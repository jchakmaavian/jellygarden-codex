#!/usr/bin/env python3
"""Static linter for the tuned levels.

Reads every board in `Resources/Levels/<n>.txt` together with its goal asset
`Resources/Targets/Level<n>.asset` and reports the levels that contradict
themselves: a goal the board can never serve, star thresholds out of order, a
declared size that is not the size that was written, a mode whose goal is not
in the asset.

It reads only what the kit itself reads, so every rule below is anchored to a
line of the kit rather than to taste:

  LevelManager.ProcessGameDataFromString  the header and the two-digit cells
  LevelManager.CreateSquare / CreateObstacles  which digits are understood
  LevelManager (board build)              TargetBlocks = BLOCK + 2 * DOUBLEBLOCK
  Item.GenItem                            colours are Random.Range(0, colorLimit)
  TargetObject.Done                       a goal is met when targetCount is spent
  LevelMakerEditor                        COLOR LIMIT is clamped to 3..6

Usage:
    python3 tools/lint_levels.py                 # every level
    python3 tools/lint_levels.py --levels 1-16   # one band
    python3 tools/lint_levels.py --quiet         # counts only

Prints one line per problem as `<level>: <problem>` and exits 1 when there is
at least one.
"""

import argparse
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
LEVELS = os.path.join(ROOT, "Assets/JellyGarden/Resources/Levels")
TARGETS = os.path.join(ROOT, "Assets/JellyGarden/Resources/Targets")

# InitScript.Target
SCORE, COLLECT, INGREDIENT, BLOCKS = 0, 1, 2, 3
TARGET_NAME = {SCORE: "SCORE", COLLECT: "COLLECT", INGREDIENT: "INGREDIENT", BLOCKS: "BLOCKS"}

# InitScript.LIMIT
LIMIT_NAME = {0: "moves", 1: "time"}

# Square.SquareTypes, as the two digits of a cell.
# CreateSquare only branches on NONE / EMPTY / BLOCK / DOUBLEBLOCK; anything
# else in the first digit leaves the square EMPTY and never calls
# CreateObstacles, so the cell silently loses whatever it was meant to hold.
BLOCK_DIGITS = {0: "NONE", 1: "EMPTY", 2: "BLOCK", 5: "DOUBLEBLOCK"}
# CreateObstacles only branches on WIREBLOCK / SOLIDBLOCK / UNDESTROYABLE /
# THRIVING.
OBSTACLE_DIGITS = {0: "NONE", 3: "WIREBLOCK", 4: "SOLIDBLOCK", 6: "UNDESTROYABLE", 7: "THRIVING"}

HEADER_KEYS = ("COLOR LIMIT", "COLLECT COUNT", "COLLECT ITEMS", "MODE", "SIZE", "LIMIT", "STARS")

# LevelMakerEditor clamps the slider to 3..6 and Item.items carries six colours.
COLOR_LIMIT_MIN, COLOR_LIMIT_MAX = 3, 6


class LevelFile:
    """One `Resources/Levels/<n>.txt`, parsed the way LevelManager parses it."""

    def __init__(self, number, text):
        self.number = number
        self.header = {}
        self.rows = []
        self.malformed = []
        for line in text.split("\n"):
            if not line.strip():
                continue
            key = next((k for k in HEADER_KEYS if line.startswith(k + " ")), None)
            if key is not None:
                self.header[key] = line[len(key):].strip()
            else:
                self.rows.append(line.split())

    def ints(self, key, count):
        """The `a/b/c` value of a header key, or None when it is not usable."""
        raw = self.header.get(key)
        if raw is None:
            return None
        parts = [p for p in raw.split("/") if p != ""]
        if len(parts) != count:
            return None
        try:
            return [int(p) for p in parts]
        except ValueError:
            return None


def parse_targets(path):
    """The `targets` array of a TargetLevel asset.

    `targetCount` was serialized as `count` before the rename, and both spellings
    are still in the shipped assets, so accept either.
    """
    entries = []
    current = None
    with open(path, "r", encoding="utf-8") as handle:
        for line in handle:
            line = line.rstrip("\n")
            head = re.match(r"^    - type: (\d+)\s*$", line)
            if head:
                if current is not None:
                    entries.append(current)
                current = {"type": int(head.group(1)), "color": 0, "count": None}
                continue
            if current is None:
                continue
            field = re.match(r"^      (\w+): (-?\d+)\s*$", line)
            if not field:
                continue
            name, value = field.group(1), int(field.group(2))
            if name == "color":
                current["color"] = value
            elif name in ("targetCount", "count"):
                current["count"] = value
    if current is not None:
        entries.append(current)
    return entries


def lint_level(number):
    """Every problem with one level, as a list of strings."""
    problems = []
    say = problems.append

    level_path = os.path.join(LEVELS, "%d.txt" % number)
    target_path = os.path.join(TARGETS, "Level%d.asset" % number)
    if not os.path.exists(level_path):
        return ["no board file at Resources/Levels/%d.txt" % number]
    with open(level_path, "r", encoding="utf-8") as handle:
        level = LevelFile(number, handle.read())

    for key in ("MODE", "SIZE", "LIMIT", "COLOR LIMIT", "STARS"):
        if key not in level.header:
            say("board has no %s line" % key)
    if len(problems):
        return problems

    # --- SIZE against the grid that was actually written -------------------
    size = level.ints("SIZE", 2)
    if size is None:
        say("SIZE %r is not cols/rows" % level.header["SIZE"])
        return problems
    cols, rows = size
    if cols < 1 or rows < 1:
        say("SIZE %d/%d is not a board" % (cols, rows))
        return problems
    if len(level.rows) != rows:
        say("SIZE says %d rows, %d rows are written" % (rows, len(level.rows)))
    for index, row in enumerate(level.rows):
        if len(row) != cols:
            say("SIZE says %d columns, row %d has %d" % (cols, index + 1, len(row)))

    # --- the two digits of every cell --------------------------------------
    board_blocks = 0     # what TargetBlocks counts at board build
    open_cells = 0       # squares that exist and can hold an item
    seen_bad_block = set()
    seen_bad_obstacle = set()
    for index, row in enumerate(level.rows):
        for column, cell in enumerate(row):
            if len(cell) != 2 or not cell.isdigit():
                say("cell %r at row %d column %d is not two digits"
                    % (cell, index + 1, column + 1))
                continue
            block, obstacle = int(cell[0]), int(cell[1])
            if block not in BLOCK_DIGITS and block not in seen_bad_block:
                seen_bad_block.add(block)
                say("block digit %d is not one of %s, so the kit draws an empty "
                    "square and skips the obstacle"
                    % (block, "/".join(str(d) for d in sorted(BLOCK_DIGITS))))
            if obstacle not in OBSTACLE_DIGITS and obstacle not in seen_bad_obstacle:
                seen_bad_obstacle.add(obstacle)
                say("obstacle digit %d is not one of %s, so nothing is placed"
                    % (obstacle, "/".join(str(d) for d in sorted(OBSTACLE_DIGITS))))
            if block == 2:
                board_blocks += 1
            elif block == 5:
                board_blocks += 2
            if block != 0:
                open_cells += 1
    if open_cells == 0:
        say("board has no open cell, nothing can be played")

    # --- LIMIT --------------------------------------------------------------
    limit = level.ints("LIMIT", 2)
    if limit is None:
        say("LIMIT %r is not type/amount" % level.header["LIMIT"])
    else:
        limit_type, limit_amount = limit
        if limit_type not in LIMIT_NAME:
            say("LIMIT type %d is neither moves (0) nor time (1)" % limit_type)
        if limit_amount <= 0:
            say("LIMIT is %d %s, so the level is over before a move is made"
                % (limit_amount, LIMIT_NAME.get(limit_type, "units")))

    # --- COLOR LIMIT --------------------------------------------------------
    color_limit = None
    raw_color_limit = level.header["COLOR LIMIT"].strip()
    try:
        color_limit = int(raw_color_limit)
    except ValueError:
        say("COLOR LIMIT %r is not a number" % raw_color_limit)
    if color_limit is not None and not (COLOR_LIMIT_MIN <= color_limit <= COLOR_LIMIT_MAX):
        say("COLOR LIMIT %d is outside the %d..%d the kit supports"
            % (color_limit, COLOR_LIMIT_MIN, COLOR_LIMIT_MAX))

    # --- STARS --------------------------------------------------------------
    stars = level.ints("STARS", 3)
    if stars is None:
        say("STARS %r is not three thresholds" % level.header["STARS"])
    else:
        if stars[0] <= 0:
            say("STARS first threshold is %d, so the pass mark is free" % stars[0])
        if not (stars[0] < stars[1] < stars[2]):
            say("STARS %d/%d/%d are not strictly ascending" % tuple(stars))

    # --- the goal asset -----------------------------------------------------
    if not os.path.exists(target_path):
        say("no goal asset at Resources/Targets/Level%d.asset" % number)
        return problems
    targets = parse_targets(target_path)
    if not targets:
        say("goal asset has no targets")
        return problems

    mode = None
    try:
        mode = int(level.header["MODE"].strip())
    except ValueError:
        say("MODE %r is not a number" % level.header["MODE"])
    if mode is not None and mode not in TARGET_NAME:
        say("MODE %d is not one of %s"
            % (mode, "/".join("%d %s" % (k, v) for k, v in sorted(TARGET_NAME.items()))))
        mode = None

    kinds = set()
    for entry in targets:
        kind, count, color = entry["type"], entry["count"], entry["color"]
        name = TARGET_NAME.get(kind, "target %d" % kind)
        kinds.add(kind)
        if kind not in TARGET_NAME:
            say("goal asset has a target of unknown kind %d" % kind)
            continue
        if count is None:
            say("%s target has no count" % name)
            continue
        if count < 0:
            say("%s target asks for %d" % (name, count))
            continue
        if kind == SCORE:
            if stars is not None and count != stars[0]:
                say("SCORE target is %d but the first star is %d; the pass mark "
                    "disagrees with itself" % (count, stars[0]))
        elif kind == BLOCKS:
            if count == 0:
                say("BLOCKS target is 0 while the board carries %d block%s; the "
                    "goal is met before a move is made"
                    % (board_blocks, "" if board_blocks == 1 else "s"))
            elif count > board_blocks:
                say("BLOCKS target is %d but the board carries only %d; the goal "
                    "cannot be met" % (count, board_blocks))
        elif kind == COLLECT:
            if count == 0:
                say("COLLECT target for colour %d asks for 0" % color)
            if color_limit is not None and color >= color_limit:
                say("COLLECT target is colour %d but COLOR LIMIT is %d, so that "
                    "item never spawns and the goal cannot be met"
                    % (color, color_limit))
            if color < 0:
                say("COLLECT target is colour %d" % color)
        elif kind == INGREDIENT:
            if count == 0:
                say("INGREDIENT target asks for 0")
            elif open_cells == 0:
                say("INGREDIENT target asks for %d but the board has no open "
                    "cell to drop one through" % count)

    if SCORE not in kinds:
        say("goal asset has no SCORE target, so a level run out of moves has no "
            "pass mark to check")
    if mode is not None and mode not in kinds:
        say("MODE %d (%s) but the goal asset holds only %s"
            % (mode, TARGET_NAME[mode],
               ", ".join(TARGET_NAME.get(k, str(k)) for k in sorted(kinds))))

    return problems


def parse_range(text, highest):
    """`1-16`, `5`, or `1-16,40-48` into a sorted list of level numbers."""
    numbers = []
    for part in text.split(","):
        part = part.strip()
        if not part:
            continue
        if "-" in part:
            low, high = part.split("-", 1)
            numbers.extend(range(int(low), int(high) + 1))
        else:
            numbers.append(int(part))
    return [n for n in sorted(set(numbers)) if 1 <= n <= highest]


def highest_level():
    highest = 0
    for name in os.listdir(LEVELS):
        stem, extension = os.path.splitext(name)
        if extension == ".txt" and stem.isdigit():
            highest = max(highest, int(stem))
    return highest


def main():
    parser = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    parser.add_argument("--levels", help="a band such as 1-16, or 1-16,40-48")
    parser.add_argument("--quiet", action="store_true",
                        help="print the summary only")
    args = parser.parse_args()

    highest = highest_level()
    if highest == 0:
        print("no levels found under %s" % LEVELS, file=sys.stderr)
        return 2
    numbers = parse_range(args.levels, highest) if args.levels else list(range(1, highest + 1))

    faulty = 0
    total = 0
    for number in numbers:
        problems = lint_level(number)
        if problems:
            faulty += 1
            total += len(problems)
            if not args.quiet:
                for problem in problems:
                    print("%d: %s" % (number, problem))

    print("%d level%s checked, %d problem%s in %d level%s"
          % (len(numbers), "" if len(numbers) == 1 else "s",
             total, "" if total == 1 else "s",
             faulty, "" if faulty == 1 else "s"))
    return 1 if total else 0


if __name__ == "__main__":
    sys.exit(main())
