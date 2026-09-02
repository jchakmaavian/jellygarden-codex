#!/usr/bin/env python3
"""Scan the project and emit the data an asset catalogue page needs.

Two things come out, both as JSON on stdout-adjacent files in the scratch dir
the caller names:

  assets   every image in Assets/, grouped by the folder it lives in, with its
           real pixel size, its file size, whether it carries alpha, and a
           thumbnail small enough to inline into one page

  levels   all 1209 boards, parsed from Resources/Levels, joined to the story's
           level plan and to the Target asset that ships with each one, so a
           board can be drawn and labelled without shipping 1209 images

Cell codes are the kit's: two digits, block type then obstacle type. See
tools/level_templates.py, which is the authority.

    python3 tools/asset_atlas.py <out-dir>
"""

import base64
import io
import json
import os
import re
import sys

from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from story_common import PROJECT_ROOT  # noqa: E402

ASSETS = os.path.join(PROJECT_ROOT, "Assets")
LEVELS = os.path.join(PROJECT_ROOT, "Assets", "JellyGarden", "Resources", "Levels")
TARGETS = os.path.join(PROJECT_ROOT, "Assets", "JellyGarden", "Resources", "Targets")
STORY = os.path.join(PROJECT_ROOT, "Assets", "JellyGarden", "Resources", "Story")

# How big a thumbnail each family gets. Board art is small already and is worth
# showing at its real size; photographic story art and backgrounds are not.
THUMB_RULES = [
    ("Textures/Items", 128, "PNG"),
    ("Textures/Blocks", 128, "PNG"),
    ("Textures/Boosts", 128, "PNG"),
    ("Textures/effects", 160, "PNG"),
    ("Prefabs/Sprites", 128, "PNG"),
    ("Textures/text", 200, "PNG"),
    ("Textures/menu", 200, "PNG"),
    ("Textures/map", 180, "JPEG"),
    ("Textures/backgrounds", 200, "JPEG"),
    ("Textures/Icon", 128, "PNG"),
    ("Resources/Story/UI", 128, "PNG"),
    ("Resources/Story/Art", 190, "JPEG"),
]
DEFAULT_THUMB = (160, "PNG")

SQUARE_NAMES = {
    "0": "none", "1": "empty", "2": "block", "3": "wire",
    "4": "solid", "5": "double", "6": "undestroyable", "7": "thriving",
}
MODE_NAMES = {0: "Score", 1: "Collect", 2: "Ingredient", 3: "Blocks"}
LIMIT_NAMES = {0: "Moves", 1: "Time"}


def rel(path):
    return os.path.relpath(path, PROJECT_ROOT)


def thumb_rule(relpath):
    for prefix, size, fmt in THUMB_RULES:
        if prefix in relpath:
            return size, fmt
    return DEFAULT_THUMB


def data_uri(image, fmt):
    buf = io.BytesIO()
    if fmt == "JPEG":
        image.convert("RGB").save(buf, "JPEG", quality=72, optimize=True)
        mime = "image/jpeg"
    else:
        if image.mode != "RGBA":
            image = image.convert("RGBA")
        # A 128-colour palette is plenty for flat game art and roughly halves
        # the page weight. FASTOCTREE is the one quantiser that keeps alpha.
        image = image.quantize(colors=128, method=Image.FASTOCTREE)
        image.save(buf, "PNG", optimize=True)
        mime = "image/png"
    return "data:%s;base64,%s" % (mime, base64.b64encode(buf.getvalue()).decode())


def guid_map():
    """guid -> asset path, read from the .meta files that define them."""
    guids = {}
    for root, _dirs, files in os.walk(ASSETS):
        for name in files:
            if not name.endswith(".meta"):
                continue
            path = os.path.join(root, name)
            try:
                with open(path, encoding="utf-8", errors="replace") as handle:
                    head = handle.read(400)
            except OSError:
                continue
            found = re.search(r"^guid: ([0-9a-f]{32})", head, re.MULTILINE)
            if found:
                guids[found.group(1)] = rel(path[:-5])
    return guids


def scan_assets():
    groups = {}
    total_bytes = 0

    for root, dirs, files in os.walk(ASSETS):
        dirs[:] = [d for d in dirs if d not in ("Editor", "Unity-Reorderable-List-master")]
        for name in sorted(files):
            if not name.lower().endswith((".png", ".jpg", ".jpeg")):
                continue
            path = os.path.join(root, name)
            relpath = rel(path)
            try:
                image = Image.open(path)
                width, height = image.size
                image.load()
            except Exception:
                continue

            size, fmt = thumb_rule(relpath)
            preview = image.copy()
            preview.thumbnail((size, size), Image.LANCZOS)

            has_alpha = image.mode in ("RGBA", "LA") or "transparency" in image.info
            file_bytes = os.path.getsize(path)
            total_bytes += file_bytes

            folder = os.path.dirname(relpath).replace("Assets/JellyGarden/", "")
            groups.setdefault(folder, []).append({
                "name": os.path.splitext(name)[0],
                "file": name,
                "path": relpath,
                "w": width,
                "h": height,
                "kb": round(file_bytes / 1024, 1),
                "alpha": bool(has_alpha),
                "src": data_uri(preview, fmt),
            })

    return groups, total_bytes


def parse_level(text):
    header = {}
    grid = []
    for line in text.splitlines():
        line = line.strip()
        if not line:
            continue
        cells = line.split()
        if all(re.fullmatch(r"\d\d", cell) for cell in cells) and len(cells) > 1:
            grid.append(cells)
            continue
        for key in ("MODE", "SIZE", "LIMIT", "COLOR LIMIT", "STARS",
                    "COLLECT COUNT", "COLLECT ITEMS"):
            if line.startswith(key):
                header[key] = line[len(key):].strip()
                break
    return header, grid


def split_pair(value, cast=int):
    parts = (value or "").split("/")
    return [cast(p) for p in parts if p != ""]


def scan_levels(guids):
    plan_path = os.path.join(STORY, "level_plan.json")
    plan = json.load(open(plan_path))
    by_number = {entry["levelNumber"]: entry for entry in plan["levels"]}

    areas = {}
    for arc in range(1, 7):
        data = json.load(open(os.path.join(STORY, "arc%d.json" % arc)))
        for area in data["areas"]:
            areas[area["id"]] = {"name": area["name"], "goal": area["goal"],
                                 "arc": data["arc"]["id"],
                                 "arcTitle": data["arc"]["title"]}

    target_re = re.compile(
        r"- type: (\d+)\s+icon: \{fileID: \d+, guid: ([0-9a-f]{32})[^}]*\}"
        r"\s+color: (-?\d+)\s+count: (\d+)")

    levels = []
    for number in range(1, plan["levelCount"] + 1):
        path = os.path.join(LEVELS, "%d.txt" % number)
        if not os.path.exists(path):
            continue
        header, grid = parse_level(open(path, encoding="utf-8").read())

        size = split_pair(header.get("SIZE"))
        limit = split_pair(header.get("LIMIT"))
        stars = split_pair(header.get("STARS"))
        collect_count = split_pair(header.get("COLLECT COUNT"))
        collect_items = split_pair(header.get("COLLECT ITEMS"))

        targets = []
        target_path = os.path.join(TARGETS, "Level%d.asset" % number)
        if os.path.exists(target_path):
            body = open(target_path, encoding="utf-8", errors="replace").read()
            for kind, guid, _colour, count in target_re.findall(body):
                icon = guids.get(guid, "")
                targets.append({
                    "mode": MODE_NAMES.get(int(kind), kind),
                    "count": int(count),
                    "icon": os.path.splitext(os.path.basename(icon))[0],
                })

        entry = by_number.get(number, {})
        area = areas.get(entry.get("areaId"), {})

        levels.append({
            "n": number,
            "area": entry.get("areaId"),
            "areaName": area.get("name", ""),
            "arc": entry.get("arc"),
            "arcTitle": area.get("arcTitle", ""),
            "theme": entry.get("themeKey", ""),
            "sabotage": bool(entry.get("sabotage")),
            "difficulty": round(entry.get("difficulty", 0), 3),
            "cols": size[0] if size else (len(grid[0]) if grid else 0),
            "rows": size[1] if len(size) > 1 else len(grid),
            "mode": MODE_NAMES.get(int(header.get("MODE", 0)), "Score"),
            "limitType": LIMIT_NAMES.get(0 if (limit and limit[0] == 0) else 1,
                                         "Moves"),
            "moves": limit[1] if len(limit) > 1 else 0,
            "time": limit[0] if limit else 0,
            "colors": int(header.get("COLOR LIMIT", 6)),
            "stars": stars,
            "collect": collect_count[1] if len(collect_count) > 1 else 0,
            "collectKinds": collect_items[1] if len(collect_items) > 1 else 0,
            "targets": targets,
            "grid": ["".join(row) for row in grid],
        })

    return levels, areas


def summarise(levels):
    counts = {}
    for level in levels:
        for row in level["grid"]:
            for index in range(0, len(row), 2):
                block, obstacle = row[index], row[index + 1]
                counts[SQUARE_NAMES[block]] = counts.get(SQUARE_NAMES[block], 0) + 1
                if obstacle != "0":
                    key = SQUARE_NAMES[obstacle]
                    counts[key] = counts.get(key, 0) + 1
    return counts


def main():
    out_dir = sys.argv[1] if len(sys.argv) > 1 else "/tmp/atlas"
    os.makedirs(out_dir, exist_ok=True)

    guids = guid_map()
    groups, total_bytes = scan_assets()
    levels, areas = scan_levels(guids)

    json.dump({"groups": groups,
               "totalBytes": total_bytes,
               "totalImages": sum(len(v) for v in groups.values())},
              open(os.path.join(out_dir, "assets.json"), "w"))
    json.dump({"levels": levels,
               "areas": {str(k): v for k, v in areas.items()},
               "cellCounts": summarise(levels)},
              open(os.path.join(out_dir, "levels.json"), "w"))

    print("images: %d in %d folders, %.1f MB on disk"
          % (sum(len(v) for v in groups.values()), len(groups),
             total_bytes / 1024 / 1024))
    print("levels: %d" % len(levels))
    print("cells:", summarise(levels))
    return 0


if __name__ == "__main__":
    sys.exit(main())
