#!/usr/bin/env python3
"""Compile the repository-owned Treasure Hunt campaign; never rewrite kit boards.

    python3 tools/treasure_hunt_build.py
    python3 tools/treasure_hunt_build.py --check

Only authored areas marked ready can be played. The remaining catalogue is
new-story metadata, not a fallback to the legacy campaign's playable content.
"""
import argparse
import hashlib
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "docs/story/treasure-hunt/campaign.json"
SCREENPLAY = ROOT / "docs/story/GREAT-ROYAL-TREASURE-HUNT.md"
OUTPUT = ROOT / "Assets/JellyGarden/Resources/Story/TreasureHunt"
CHAPTERS = ["The Castle of Surprises", "The Runaway Parade", "The Crystal Express",
            "The Royal Adventure Games", "The Upside-Down Tower", "The Biggest Surprise"]
THEMES = ["castle", "village", "crystal", "games", "inventions", "festival"]


def encoded(value):
    return json.dumps(value, ensure_ascii=False, indent=2) + "\n"


def meta(path, folder=False):
    relative = path.relative_to(ROOT).as_posix()
    guid = hashlib.md5(relative.encode()).hexdigest()
    return (f"fileFormatVersion: 2\nguid: {guid}\n" +
            ("folderAsset: yes\nDefaultImporter:\n" if folder else "TextScriptImporter:\n") +
            "  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")


def compile_campaign(source):
    catalogue = re.findall(r"^\| (\d{3}) \| (.*?) \| (.*?) \| (.*?) \|$",
                           SCREENPLAY.read_text(), re.M)
    assert [int(row[0]) for row in catalogue] == list(range(1, 151)), "Incomplete catalogue"
    authored = {a["id"]: a for a in source["areas"]}
    assert len(authored) == len(source["areas"]), "Duplicate area IDs"
    assert set(authored) <= set(range(1, 151)), "Invalid area ID"
    used_levels, scenes, scene_ids = set(), [], set()
    areas, entries = [], []
    for number, name, goal, discovery in catalogue:
        number = int(number)
        area = {"id": number, "arc": (number - 1) // 25 + 1, "name": name,
                "goal": goal, "clue": "", "tasks": [], "status": "coming-soon",
                "pointsTo": number + 1 if number < 150 else 0, "levels": []}
        if number in authored:
            area.update(authored[number])
            tasks = area["tasks"]
            assert [t["index"] for t in tasks] == list(range(1, len(tasks) + 1)), "Task order"
            assert all(t["stars"] > 0 and t["label"] for t in tasks)
            assert len(area["boardNumbers"]) == sum(t["stars"] for t in tasks), "Unfunded tasks"
            assert len(set(area["boardNumbers"])) == len(area["boardNumbers"]), "Duplicate rewardable level"
            assert set(area.get("minimumMoves", {})) <= {str(n) for n in area["boardNumbers"]}, "Move tuning for unassigned board"
            assert all(isinstance(n, int) and n > 0 for n in area.get("minimumMoves", {}).values()), "Invalid move floor"
            offset = 0
            for task in tasks:
                # Empty pairs deliberately omit the post-repair dialogue card.
                # The final task still delivers its area's separate payoff scene.
                speaker, line = task["speaker"], task["onComplete"]
                assert isinstance(speaker, str) and isinstance(line, str), "Invalid task dialogue"
                assert bool(speaker.strip()) == bool(line.strip()), "Task dialogue needs both speaker and line, or neither"
                assert not line.strip() or speaker not in ("Mimi", "Pebble"), "Non-speaking character"
                transition = task.get("repairTransition", "")
                assert transition in ("", "vines", "door", "flags", "lanterns"), "Unknown repair transition"
                assert not (transition and task.get("instantRepair")), "A transition cannot be instant"
                if transition == "vines":
                    anchors = OUTPUT / f"Transitions/area_{number:03}_task{task['index']}_vines.json"
                    assert anchors.exists(), "Missing vine transition anchors"
                elif transition:
                    stem = OUTPUT / f"Transitions/area_{number:03}_parts"
                    assert stem.with_suffix('.png').exists() and Path(str(stem) + '_layout.json').exists(), "Missing gate transition parts"
                for _ in range(task["stars"]):
                    level = area["boardNumbers"][offset]
                    assert level not in used_levels, f"Duplicate rewardable level {level}"
                    assert (ROOT / f"Assets/JellyGarden/Resources/Levels/{level}.txt").exists()
                    assert (ROOT / f"Assets/JellyGarden/Resources/Targets/Level{level}.asset").exists()
                    used_levels.add(level)
                    offset += 1
                    entries.append({"levelNumber": level, "areaId": number,
                                    "arc": area["arc"], "taskIndex": task["index"],
                                    "starIndexWithinArea": offset, "difficulty": 0,
                                    "themeKey": THEMES[area["arc"] - 1], "sabotage": False,
                                    "minimumMoves": area.get("minimumMoves", {}).get(str(level), 0)})
            for scene in area.pop("scenes", []):
                assert scene["id"] not in scene_ids, "Duplicate scene ID"
                assert scene["lines"], "Empty scene"
                for line in scene["lines"]:
                    assert line["speaker"] not in ("Mimi", "Pebble"), "Non-speaking character"
                    assert line["text"] and not line["text"].startswith("STAGE"), "Staging is not dialogue"
                scene_ids.add(scene["id"])
                scenes.append(dict(scene, areaId=number))
        area["stars"] = sum(t["stars"] for t in area["tasks"])
        areas.append(area)
    ready = [a["id"] for a in areas if a["status"] == "ready"]
    assert ready and ready == list(range(1, max(ready) + 1)), "Ready areas must be contiguous"
    for area in areas:
        if area["status"] == "ready":
            assert area["tasks"] and area["payoffScene"] in scene_ids
            assert area["pointsTo"] == (area["id"] + 1 if area["id"] < 150 else 0), "Invalid next area"
            for state in range(len(area["tasks"]) + 1):
                assert (OUTPUT / f"Art/area_{area['id']:03}_state{state}.png").exists(), "Missing required room state"
        else:
            assert not area["tasks"] and not area.get("boardNumbers"), "Unfinished areas cannot own playable content"
    assert "OPEN-01" in scene_ids, "Missing opening"
    alias_keys = [a["key"] for a in source.get("artAliases", [])]
    assert len(alias_keys) == len(set(alias_keys)), "Duplicate art alias"
    for area in areas:
        if area["id"] not in ready:
            continue
        details = [f"task_{area['id']:03}_{t['index']}" for t in area["tasks"]]
        details.append(f"clue_{area['id']:03}")
        for key in details:
            assert key in alias_keys or (OUTPUT / f"Art/{key}.png").exists(), "Missing required detail art: " + key
    for alias in source.get("artAliases", []):
        assert (OUTPUT / ("Art/" + alias["source"] + ".png")).exists(), "Missing alias source"
        if "rect" in alias:
            x, y, w, h = alias["rect"]
            assert x >= 0 and y >= 0 and w > 0 and h > 0 and x + w <= 1 and y + h <= 1, "Invalid art crop"
    index = {"totalAreas": 150, "totalTasks": sum(len(a["tasks"]) for a in areas),
             "totalStars": sum(a["stars"] for a in areas), "arcs": []}
    output = {}
    for arc in range(1, 7):
        band = [a for a in areas if a["arc"] == arc]
        output[f"arc{arc}.json"] = {"schemaVersion": 2, "arc": arc, "areas": band}
        index["arcs"].append({"arc": arc, "title": CHAPTERS[arc - 1],
                              "firstArea": band[0]["id"], "lastArea": band[-1]["id"],
                              "areaCount": 25, "taskCount": sum(len(a["tasks"]) for a in band),
                              "starCount": sum(a["stars"] for a in band),
                              "themeKey": THEMES[arc - 1]})
    output["index.json"] = index
    output["level_plan.json"] = {"levelCount": len(entries), "firstSabotageArea": 2147483647,
                                 "levels": entries,
                                 "byArea": {str(a): [e["levelNumber"] for e in entries if e["areaId"] == a]
                                            for a in ready}}
    output["scenes.json"] = {"scenes": scenes}
    output["cutscenes.json"] = {"cutsceneCount": 0, "cutscenes": []}
    output["repair_regions.json"] = source["repairRegions"]
    output["themes.json"] = {"themes": [
        {"arc": n, "title": CHAPTERS[n - 1], "themeKey": THEMES[n - 1],
         "boardBackdrop": "backdrop_" + THEMES[n - 1], "kitBackdropIndex": 0,
         "squareTint": [0.86, 0.95, 1.0], "boardFill": [0.18, 0.40, 0.60, 0.92]}
        for n in range(1, 7)]}
    output["campaign.json"] = {"id": "treasure-hunt", "title": "The Great Royal Treasure Hunt",
                               "openingScene": "OPEN-01", "readyAreas": ready,
                               "artAliases": source.get("artAliases", []),
                               "sharedArt": source.get("sharedArt", [])}
    return output


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true", help="Validate source and generated output without writing")
    args = parser.parse_args()
    output = compile_campaign(json.loads(SOURCE.read_text()))
    mismatches = []
    for name, value in output.items():
        path = OUTPUT / name
        text = encoded(value)
        if args.check:
            if not path.exists() or path.read_text() != text:
                mismatches.append(str(path.relative_to(ROOT)))
        else:
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text(text)
            if not Path(str(path) + ".meta").exists():
                Path(str(path) + ".meta").write_text(meta(path))
    active = OUTPUT.parent / "active_campaign.json"
    active_text = encoded({"resourceRoot": "Story/TreasureHunt"})
    if args.check:
        if not active.exists() or active.read_text() != active_text:
            mismatches.append(str(active.relative_to(ROOT)))
    else:
        active.write_text(active_text)
        Path(str(active) + ".meta").write_text(meta(active))
        folder_meta = Path(str(OUTPUT) + ".meta")
        if not folder_meta.exists():
            folder_meta.write_text(meta(OUTPUT, folder=True))
    if mismatches:
        raise SystemExit("Stale generated content: " + ", ".join(mismatches))
    print(f"Treasure Hunt: {len(output['campaign.json']['readyAreas'])} ready area(s), "
          f"{output['index.json']['totalTasks']} tasks, {output['index.json']['totalStars']} stars; "
          "150 new-story catalogue entries. " + ("Verified." if args.check else "Compiled."))


if __name__ == "__main__":
    main()
