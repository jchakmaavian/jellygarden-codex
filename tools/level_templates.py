#!/usr/bin/env python3
"""Parse the 100 shipped Jelly Garden levels into a reusable template library.

The story needs 1209 boards and only 100 exist. Rather than invent boards from
nothing, the generator remixes these — they are hand-tuned and known to be
playable, so anything derived from them starts from a board a designer signed
off on. This script is the parser half; level_gen.py is the remixer.

    python3 tools/level_templates.py
"""

import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from story_common import LEVELS_RES, OUT, dump_json, ensure_dir  # noqa: E402

# Mirrors JellyGarden.Scripts.InitScript.Target
MODE_NAMES = {0: "SCORE", 1: "COLLECT", 2: "INGREDIENT", 3: "BLOCKS"}
# Mirrors JellyGarden.Scripts.InitScript.LIMIT
LIMIT_NAMES = {0: "MOVES", 1: "TIME"}
# Mirrors JellyGarden.Scripts.Square.SquareTypes
SQUARE_NAMES = {
    0: "NONE",
    1: "EMPTY",
    2: "BLOCK",
    3: "WIREBLOCK",
    4: "SOLIDBLOCK",
    5: "DOUBLEBLOCK",
    6: "UNDESTROYABLE",
    7: "THRIVING",
}

# A cell is two digits: block type then obstacle type. "10" is a plain playable
# cell; "00" is a hole in the board.
CELL_RE = re.compile(r"^\d\d$")


class LevelParseError(ValueError):
    pass


def parse_level(text, name="<level>"):
    """Parse one level .txt into a dict. Raises LevelParseError on bad input."""
    header = {
        "mode": None,
        "cols": None,
        "rows": None,
        "limitType": None,
        "limit": None,
        "colorLimit": None,
        "stars": None,
        "collectCount": [],
        "collectItems": [],
    }
    grid = []

    for raw in text.split("\n"):
        line = raw.strip()
        if not line:
            continue

        if line.startswith("MODE"):
            header["mode"] = int(line[4:].strip())
        elif line.startswith("SIZE "):
            cols, rows = line[5:].strip().split("/")
            header["cols"], header["rows"] = int(cols), int(rows)
        elif line.startswith("LIMIT"):
            kind, amount = line[5:].strip().split("/")
            header["limitType"], header["limit"] = int(kind), int(amount)
        elif line.startswith("COLOR LIMIT "):
            header["colorLimit"] = int(line[12:].strip())
        elif line.startswith("STARS"):
            header["stars"] = [int(v) for v in line[5:].strip().split("/")]
        elif line.startswith("COLLECT COUNT "):
            header["collectCount"] = [int(v) for v in line[14:].strip().split("/")]
        elif line.startswith("COLLECT ITEMS "):
            header["collectItems"] = [int(v) for v in line[14:].strip().split("/")]
        else:
            cells = line.split()
            if not cells or not all(CELL_RE.match(c) for c in cells):
                raise LevelParseError("%s: unrecognised line %r" % (name, raw))
            grid.append(cells)

    for key in ("mode", "cols", "rows", "limitType", "limit", "colorLimit", "stars"):
        if header[key] is None:
            raise LevelParseError("%s: missing %s" % (name, key))

    if len(grid) != header["rows"]:
        raise LevelParseError(
            "%s: declared %d rows, found %d" % (name, header["rows"], len(grid))
        )
    for r, row in enumerate(grid):
        if len(row) != header["cols"]:
            raise LevelParseError(
                "%s: row %d has %d cells, expected %d"
                % (name, r, len(row), header["cols"])
            )

    header["grid"] = grid
    return header


def format_level(level):
    """Inverse of parse_level. Byte-identical round trip for the shipped files."""
    lines = [
        "MODE %d" % level["mode"],
        "SIZE %d/%d" % (level["cols"], level["rows"]),
        "LIMIT %d/%d" % (level["limitType"], level["limit"]),
        "COLOR LIMIT %d" % level["colorLimit"],
        "STARS %s" % "/".join(str(v) for v in level["stars"]),
        "COLLECT COUNT %s" % "/".join(str(v) for v in (level["collectCount"] or [0, 0])),
        "COLLECT ITEMS %s" % "/".join(str(v) for v in (level["collectItems"] or [0, 0])),
    ]
    lines.extend(" ".join(row) for row in level["grid"])
    # The shipped files have no trailing newline; match them exactly so the
    # library can reproduce its own input byte-for-byte.
    return "\n".join(lines)


def playable_ratio(grid):
    """Fraction of cells a gem can actually occupy. '00' is a hole."""
    total = sum(len(row) for row in grid)
    if total == 0:
        return 0.0
    playable = sum(1 for row in grid for cell in row if cell[0] != "0")
    return playable / total


def obstacle_density(grid):
    """Fraction of cells carrying a blocker or an obstacle overlay."""
    total = sum(len(row) for row in grid)
    if total == 0:
        return 0.0
    busy = sum(
        1
        for row in grid
        for cell in row
        if (cell[0] not in ("0", "1")) or cell[1] != "0"
    )
    return busy / total


def difficulty_of(level):
    """
    Normalised 0..1 difficulty estimate.

    Not a win-rate model — there is no solver in this project. It is a ranking
    signal, used only to choose which template dresses which story level, so
    what matters is that it orders the shipped levels sensibly.
    """
    grid = level["grid"]
    area = level["cols"] * level["rows"]

    # Fewer moves for a bigger board is harder.
    moves = max(1, level["limit"])
    pressure = min(1.0, (area / 40.0) / moves)

    # More colours means fewer accidental matches.
    colour = (level["colorLimit"] - 4) / 3.0  # 4..7 -> 0..1
    colour = min(1.0, max(0.0, colour))

    holes = 1.0 - playable_ratio(grid)
    obstacles = min(1.0, obstacle_density(grid) * 2.0)

    # Collect and ingredient modes ask for specific tiles, not just points.
    mode_weight = {0: 0.0, 1: 0.25, 2: 0.35, 3: 0.15}.get(level["mode"], 0.0)

    score = (
        0.34 * pressure
        + 0.20 * colour
        + 0.16 * holes
        + 0.20 * obstacles
        + 0.10 * mode_weight * 2.0
    )
    return round(min(1.0, max(0.0, score)), 4)


def load_shipped_levels():
    """Every Levels/N.txt currently on disk, parsed and classified."""
    templates = []
    for entry in sorted(os.listdir(LEVELS_RES)):
        if not entry.endswith(".txt") or entry.endswith(".txt.meta"):
            continue
        stem = entry[:-4]
        if not stem.isdigit():
            continue

        path = os.path.join(LEVELS_RES, entry)
        with open(path, encoding="utf-8") as fh:
            text = fh.read()

        level = parse_level(text, name=entry)
        level["number"] = int(stem)
        level["modeName"] = MODE_NAMES.get(level["mode"], str(level["mode"]))
        level["limitName"] = LIMIT_NAMES.get(level["limitType"], str(level["limitType"]))
        level["difficulty"] = difficulty_of(level)
        level["playableRatio"] = round(playable_ratio(level["grid"]), 4)
        level["obstacleDensity"] = round(obstacle_density(level["grid"]), 4)
        templates.append(level)

    # The raw score only spans ~0.10..0.41, which is useless as a selector.
    # What the generator actually needs is a rank: "give me the board sitting at
    # 70% of the shipped difficulty spread". Percentile-rank it.
    by_difficulty = sorted(templates, key=lambda t: t["difficulty"])
    last = max(1, len(by_difficulty) - 1)
    for rank, level in enumerate(by_difficulty):
        level["difficultyRank"] = round(rank / last, 4)

    templates.sort(key=lambda t: t["number"])
    return templates


def main():
    templates = load_shipped_levels()

    # A template library is worthless if it cannot reproduce its own input.
    for level in templates:
        path = os.path.join(LEVELS_RES, "%d.txt" % level["number"])
        with open(path, encoding="utf-8") as fh:
            original = fh.read()
        if format_level(level) != original:
            print(
                "WARNING: level %d does not round-trip byte-for-byte"
                % level["number"],
                file=sys.stderr,
            )

    ensure_dir(OUT)
    dump_json(
        os.path.join(OUT, "level_templates.json"),
        {"squareTypes": SQUARE_NAMES, "modes": MODE_NAMES, "templates": templates},
        meta=False,
    )

    easiest = min(templates, key=lambda t: t["difficulty"])
    hardest = max(templates, key=lambda t: t["difficulty"])
    print("templates=%d" % len(templates))
    print(
        "difficulty range: level %d @ %.3f .. level %d @ %.3f"
        % (
            easiest["number"],
            easiest["difficulty"],
            hardest["number"],
            hardest["difficulty"],
        )
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())
