#!/usr/bin/env python3
"""Write a playable board for every level the story plan asks for.

Levels 1-100 are the kit's hand-tuned originals and are re-emitted unchanged.
Levels 101-1209 are derived: pick the shipped template sitting at the planned
difficulty rank, then apply seeded, deterministic mutations. Every mutation is
shape-preserving — a mirrored board is still the board a designer balanced, and
a board with one more move or one fewer colour is still solvable by the same
route. Nothing here invents a layout from scratch.

Deterministic: the same plan and the same template library always produce the
same 1209 files, so regenerating is a no-op rather than a diff.

    python3 tools/level_templates.py && python3 tools/level_plan.py
    python3 tools/level_gen.py
"""

import json
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from level_templates import (  # noqa: E402
    block_target_count,
    format_level,
    open_ratio,
    parse_level,
    playable_ratio,
    unmatchable_regions,
)
from story_common import LEVELS_RES, OUT, ensure_dir, write_text_meta  # noqa: E402

PRESERVED_THROUGH = 100

# How many levels back a template stays off the table.
#
# The number is the guarantee: no board comes back inside RECENT_TEMPLATES + 1
# levels, measured over the finished grids and not just the template numbers.
# A wider ban list needs a wider rank window to draw from, and that is the
# trade. Measured over all 1208 levels, window half-width against ban length:
#
#     window  recent  fell back  fewest candidates  closest repeat  rank offset
#     +-3     4       0          2                  5 levels        3
#     +-4     6       0          1                  7               4
#     +-5     8       15         1                  9               5
#     +-6     10      28         1                  10              6
#
# "fell back" is the count of levels where the ban list emptied the window and
# `or window` had to hand back a banned template — where the guarantee is not a
# guarantee. +-3 with four is the only row that holds everywhere AND still
# leaves every level a choice between two boards; every wider setting buys a
# bigger number by breaking the rule at fifteen levels or more, and pulls the
# board further from the difficulty the plan asked for. So the window stays as
# it was and this is four.
RECENT_TEMPLATES = 4

# The two digits of a cell take disjoint sets of SquareTypes; see the note in
# level_templates.py. Getting this wrong does not make a harder board, it makes
# a cell LevelManager silently treats as empty.
NONE = "0"
EMPTY = "1"
BLOCK = "2"          # block digit
DOUBLEBLOCK = "5"    # block digit
WIREBLOCK = "3"      # obstacle digit
THRIVING = "7"       # obstacle digit


def load(path):
    with open(path, encoding="utf-8") as fh:
        return json.load(fh)


def pick_template(templates_by_rank, difficulty, rng, recent=()):
    """
    The template whose difficulty rank is closest to the target, chosen from a
    small window so consecutive levels do not all land on the same board.

    The window on its own did not achieve that. Each level drew from it
    independently, so the same template came up twice in a row 107 times over
    the 1108 generated levels — four times in a row at 240-243, 356-359,
    945-948 and 1145-1148 — and 16 of those pairs came out BYTE-IDENTICAL,
    because the mutations are a coin flip each (mirror at 1/2, flip at 1/4) and
    can land the same way twice. Levels 111 and 112 are one such pair: same
    board, same mode, same colour limit, one more move and a pass mark eight
    points higher. A player reads that as the game repeating itself.

    `recent` is the templates the levels immediately before this one used. They
    are dropped from the window, so a board cannot come back until
    RECENT_TEMPLATES levels have gone by. The window is seven wide, so at least
    two candidates are left and the pick stays inside the same difficulty band;
    `or window` is the guard for the truncated windows at either end.
    """
    index = int(round(difficulty * (len(templates_by_rank) - 1)))
    low = max(0, index - 3)
    high = min(len(templates_by_rank) - 1, index + 3)
    window = templates_by_rank[low:high + 1]
    banned = set(recent)
    candidates = [t for t in window if t["number"] not in banned] or window
    return candidates[rng.randrange(len(candidates))]


def mirror_grid(grid):
    return [list(reversed(row)) for row in grid]


def flip_grid(grid):
    return [list(row) for row in reversed(grid)]


# The board the story plays on. The kit ships eleven sizes, 9x11 being 862 of
# the 1208 levels, and every one of them is cropped to this on the way out.
#
# Seven columns rather than nine is the whole point: the width is what sets the
# cell size on screen (StoryBoardScale puts nine columns at 98% of a 393pt
# phone), so only dropping columns makes a cell bigger. Nine rows rather than
# eleven keeps the board from crowding the HUD.
BOARD_COLS = 7
BOARD_ROWS = 9

# MODE_NAMES in level_templates: 0 SCORE, 1 COLLECT, 2 INGREDIENT, 3 BLOCKS.
# Only BLOCKS reads its goal off the board's own contents, so only BLOCKS can
# have that goal cropped away.
MODE_BLOCKS = 3


def crop_window(grid, top, left, cols=BOARD_COLS, rows=BOARD_ROWS):
    """One window out of a board, clamped to what the board actually has."""
    take_rows = min(rows, len(grid))
    take_cols = min(cols, len(grid[0]) if grid else 0)
    return [list(row[left:left + take_cols]) for row in grid[top:top + take_rows]]


def crop_grid(grid, cols=BOARD_COLS, rows=BOARD_ROWS):
    """
    Cut a board down to cols x rows, keeping as much of the design as the new
    frame can hold.

    Centred is the DEFAULT, and for most boards it is also the answer: these
    layouts are hand drawn and mirrored about their middle -- level 500 is a
    plus sign, level 7 a frame with notched sides -- so a centred window keeps
    the symmetry that a corner window would slice off one side of.

    But centred is not always best, and on two of the kit's hundred it is
    actively wrong. A match is three in a line, so a region only two cells long
    can never host one: `seal_unmatchable_cells` fills such a region with holes.
    Level 31 is a comb whose teeth are single columns exactly three cells tall,
    and a centred crop leaves the upper teeth two tall -- six cells that the
    sealer then turns into holes, taking the board from 51% open to 41%.

    So every window is scored and the best one wins:

      1. fewest cells the sealer would kill, because those become holes;
      2. then most open, because an open cell is a cell the player can use;
      3. then closest to centred, so a board with nothing to gain keeps its
         symmetry and the rule stays predictable.

    A board already at or under the target in a dimension keeps it: eleven of
    the kit's sizes are narrower than seven or shorter than nine, and widening
    one would mean inventing cells no designer drew.
    """
    have_rows = len(grid)
    have_cols = len(grid[0]) if grid else 0

    take_rows = min(rows, have_rows)
    take_cols = min(cols, have_cols)

    mid_top = (have_rows - take_rows) / 2.0
    mid_left = (have_cols - take_cols) / 2.0

    best = None
    for top in range(have_rows - take_rows + 1):
        for left in range(have_cols - take_cols + 1):
            window = crop_window(grid, top, left, cols, rows)
            dead = sum(len(region) for region in unmatchable_regions(window))
            key = (
                dead,
                -open_ratio(window),
                abs(top - mid_top) + abs(left - mid_left),
                top,
                left,
            )
            if best is None or key < best[0]:
                best = (key, window)

    return best[1]


def fit_board(grid, floor, mode=None):
    """
    The board as it ships: cropped to 7x9 where that survives, left alone where
    it does not.

    Some boards cannot be made smaller without being made worse, and there are
    two ways a crop ruins one.

    IT THINS THE BOARD OUT. Level 31's comb is the case that forced this: its
    teeth are single columns three cells tall, three being the match length, so
    ANY window that shortens them strands them -- the best 7x9 crop of it is 44%
    open against the kit's 55% floor, and the centred one is 41%.

    IT TAKES THE GOAL AWAY. Level 63 is a BLOCKS level whose only blocks are
    four double blocks in the four CORNERS of a 9x10 board. No 7x9 window holds
    two opposite corners of that board, so every crop of it scores zero blocks
    and the level's whole objective -- eight of them -- has nothing to stand on.
    Eleven levels draw on that template.

    So the crop is offered, not imposed. A board that would lose either way
    keeps the size its designer drew. That costs the game a second board size on
    a handful of levels, which is a smaller price than a level that cannot be
    played or cannot be won.

    Both tests read the SEALED window, because sealing is what the emitted board
    goes through: an unsealed grid would promise open cells that are about to
    become holes.
    """
    cropped = seal_unmatchable_cells(crop_grid(grid))

    if open_ratio(cropped) < floor - 1e-9:
        return [list(row) for row in grid]

    # A BLOCKS level (MODE 3) whose crop keeps no blocks has no goal left. The
    # count itself is clamped downstream in target_gen; what cannot be repaired
    # there is a board with nothing to clear at all.
    if mode == MODE_BLOCKS and block_target_count(cropped) <= 0:
        if block_target_count(grid) > 0:
            return [list(row) for row in grid]

    return crop_grid(grid)


# A LIMIT is a number in a unit, and the unit decides the clamp. Type 0 counts
# moves, type 1 counts seconds, and the two shipped sets do not overlap: the
# kit's 89 move levels run 6..61 and its 11 time levels are 60 (nine of them),
# 80 and 90. One 8..60 clamp served both for 1108 levels, and in seconds it was
# wrong at both ends — it capped every clock at 60, so kit level 69's 80 seconds
# and kit level 80's 90 could not survive being used as a template, and it
# floored nothing, so 63 of the 113 generated time levels ran SHORTER than any
# clock the kit ships.
LIMIT_MOVES, LIMIT_TIME = 0, 1
LIMIT_BOUNDS = {
    # (floor, ceiling), in the row's own unit. The moves row is what the single
    # clamp always was and no generated level reaches either end of it; the
    # seconds row is the shipped time set's own smallest and largest.
    LIMIT_MOVES: (8, 60),
    LIMIT_TIME: (60, 90),
}


def scale_limit(limit, limit_type, difficulty, rng):
    """
    Harder levels get less of whatever they are limited by — fewer moves, or
    fewer seconds — but never less than the kit's own floor for that unit, and
    never more than its ceiling. See LIMIT_BOUNDS for where the two rows come
    from; a limit type the kit does not ship is measured in moves.
    """
    # 1.15x at the easy end down to 0.80x at the hard end.
    factor = 1.15 - 0.35 * difficulty
    scaled = int(round(limit * factor))
    scaled += rng.choice((-1, 0, 0, 1))
    floor, ceiling = LIMIT_BOUNDS.get(limit_type, LIMIT_BOUNDS[LIMIT_MOVES])
    return max(floor, min(ceiling, scaled))


def scale_stars(stars, difficulty):
    """Score thresholds rise with difficulty so a hard board still asks for more."""
    factor = 0.95 + 0.45 * difficulty
    scaled = [int(round(v * factor)) for v in stars]
    # Keep them strictly increasing; a flat threshold shows as a stuck star bar.
    for i in range(1, len(scaled)):
        if scaled[i] <= scaled[i - 1]:
            scaled[i] = scaled[i - 1] + 100
    return scaled


def scale_colours(colour_limit, difficulty, rng, collect_colours=()):
    """
    More colours is harder. Stay inside 4..6: the kit ships 4-6 and a 7th colour
    would need art that does not exist.

    The floor is not 4 unconditionally. A level inherits its template's COLLECT
    targets verbatim, and TargetObject.SetOff matches one by `item.color ==
    target.color` while Item.GenItem only ever rolls `Random.Range(0,
    colorLimit)`. Dropping the colour limit to 4 under a target for colour 4
    therefore names an item that can never spawn: the goal cannot be met and the
    playtest can only report "ran out of moves". So the limit never falls below
    one past the highest colour any inherited COLLECT target asks for.
    """
    floor = 4
    if collect_colours:
        floor = max(floor, max(collect_colours) + 1)
    target = 4 + int(round(difficulty * 2))
    if rng.random() < 0.25:
        target += rng.choice((-1, 1))
    return max(floor, min(6, max(target, colour_limit - 1)))


def thicken_blockers(grid, difficulty, rng):
    """
    Promote single blocks to double blocks as difficulty rises — the only
    promotion the block digit actually supports.

    A gem still lives in a blocked cell, so this changes how many hits the cell
    takes without changing the board's shape or its reachable region. It does
    change the BLOCKS-mode objective count, which is why the target asset is
    generated from the finished grid rather than copied.
    """
    if difficulty < 0.35:
        return grid

    chance = min(0.45, (difficulty - 0.35) * 0.9)

    out = []
    for row in grid:
        new_row = []
        for cell in row:
            block, obstacle = cell[0], cell[1]
            if block == BLOCK and rng.random() < chance:
                block = DOUBLEBLOCK
            new_row.append(block + obstacle)
        out.append(new_row)
    return out


def apply_sabotage(grid, rng):
    """
    Grimm's sabotage: one thriving block, which spreads to a neighbour every
    turn on its own.

    THRIVING is the kit's own self-spreading obstacle, already wired with its
    prefab and its spread animation in LevelManager — so the story beat costs no
    new mechanic. It goes in the obstacle digit on a plain cell, away from the
    edges so it has room to be cut back before it takes the board.
    """
    rows, cols = len(grid), len(grid[0])

    def free(r, c):
        # Any real cell with no obstacle on it. A cell already under a block
        # cover is fine — "50" becomes "57", a double block that also spreads —
        # and on boards made entirely of covers that is the only option there is.
        return grid[r][c][0] != NONE and grid[r][c][1] == NONE

    interior = [
        (r, c)
        for r in range(1, max(1, rows - 1))
        for c in range(1, max(1, cols - 1))
        if free(r, c)
    ]
    # Prefer a plain cell in the interior, then any interior cell, then anywhere.
    plain = [(r, c) for (r, c) in interior if grid[r][c][0] == EMPTY]
    anywhere = [
        (r, c) for r in range(rows) for c in range(cols) if free(r, c)
    ]
    candidates = plain or interior or anywhere
    if not candidates:
        return grid

    r, c = candidates[rng.randrange(len(candidates))]
    grid = [list(row) for row in grid]
    grid[r][c] = grid[r][c][0] + THRIVING
    return grid


def strip_self_acting(grid):
    """
    Remove any thriving block the template brought with it.

    Areas 001-018 are static neglect — nothing on the board may act on its own
    before Grimm opens the sluice gates at area 019 — and past that point the
    plan decides which levels are sabotaged. Either way, a spreading blocker that
    arrived by accident because a shipped template happened to have one would
    make the plan's sabotage flag a lie.
    """
    return [
        [cell[0] + (NONE if cell[1] == THRIVING else cell[1]) for cell in row]
        for row in grid
    ]


def seal_unmatchable_cells(grid):
    """
    Close every cell no match can ever reach.

    `unmatchable_regions` is the rule: a region walled in by holes and
    undestroyable blocks that holds no three collinear cells cannot host a
    match, so whatever spawns there is frozen for the whole level. Two of the
    kit's hundred boards contain one — level 45's picture frame has a single
    cell in the middle of it, level 97 has one in each corner — and a generated
    level gets one only by inheriting one of those two shapes.

    What the player sees is a gem sitting alone in its own frame, off the edge
    of the board, that never changes for the whole level — four of them at once
    on level 141. On the fourteen BLOCKS levels that inherit level 45's picture
    frame it is worse: the lone cell is a block, so a blocker stays iced no
    matter what the player does. None of those levels was unwinnable — their
    goal asks for 62 blocks and 64 to 72 are reachable — but the board is
    telling the player something untrue about itself.

    Sealing it — writing the hole the board already behaves as if it had — is
    the smallest honest change: the cell was unreachable before and is absent
    after, `block_target_count` then reads the goal off a board that can meet
    all of it, and nothing else about the shape moves. Levels 1-100 never come
    through here, so the kit's own two boards keep the cells a designer drew.
    """
    dead = [cell for region in unmatchable_regions(grid) for cell in region]
    if not dead:
        return grid
    grid = [list(row) for row in grid]
    for row, col in dead:
        grid[row][col] = NONE + NONE
    return grid


def generate(template, entry, min_open=0.0):
    """Derive one level from a template. Pure given (template, entry)."""
    rng = random.Random("kingleo/%d" % entry["levelNumber"])
    difficulty = entry["difficulty"]

    grid = [list(row) for row in template["grid"]]
    if rng.random() < 0.5:
        grid = mirror_grid(grid)
    if rng.random() < 0.25:
        grid = flip_grid(grid)

    # Cropped BEFORE the blockers are thickened and before the seal, so both
    # read the shape that actually ships: thickening a blocker on a column the
    # crop is about to cut off would spend the difficulty budget on cells no
    # player sees, and sealing before the crop would miss the regions the crop
    # itself strands.
    grid = fit_board(grid, min_open, template["mode"])

    grid = thicken_blockers(grid, difficulty, rng)
    grid = strip_self_acting(grid)

    # Sealed BEFORE the sabotage, not after it. Sealing writes a hole over a
    # cell no match can reach, and on level 547 that cell was the one the
    # sabotage had just been placed on: the board came out with no spreading
    # blocker on it at all and the plan's sabotage flag became a lie, which
    # `validate_story.py` catches and did.
    #
    # The order is free to change because sealing does not depend on the
    # obstacle digit — `unmatchable_regions` treats only holes and undestroyable
    # blocks as walls, and a thriving block is neither — so it sees exactly the
    # same shape here as it saw at the end. It still runs after the mirror, the
    # flip and the thickening, which are what decide that shape.
    grid = seal_unmatchable_cells(grid)

    if entry["sabotage"]:
        # A thriving block closes its cell until it is cleared, so it never
        # reduces the board's permanently open area — but the check stays here so
        # a future obstacle that does cannot slip in unnoticed.
        sabotaged = apply_sabotage(grid, rng)
        if open_ratio(sabotaged) >= min_open - 1e-9:
            grid = sabotaged

    return {
        "mode": template["mode"],
        # Read off the CROPPED grid, never off the template: parse_level asserts
        # the header against the rows it can count, so a stale SIZE here is a
        # hard failure on the next read rather than a quiet mismatch.
        "cols": len(grid[0]) if grid else 0,
        "rows": len(grid),
        "limitType": template["limitType"],
        "limit": scale_limit(
            template["limit"], template["limitType"], difficulty, rng),
        "colorLimit": scale_colours(
            template["colorLimit"],
            difficulty,
            rng,
            template.get("collectTargetColors", ()),
        ),
        "stars": scale_stars(template["stars"], difficulty),
        "collectCount": list(template["collectCount"]) or [0, 0],
        "collectItems": list(template["collectItems"]) or [0, 0],
        "grid": grid,
        "sourceTemplate": template["number"],
    }


def main():
    templates = load(os.path.join(OUT, "level_templates.json"))["templates"]
    plan = load(os.path.join(OUT, "level_plan.json"))

    by_number = {t["number"]: t for t in templates}
    by_rank = sorted(templates, key=lambda t: t["difficultyRank"])

    # The floor is derived, not invented: no generated board may be less open
    # than the least open board the kit itself ships. Measured as open_ratio —
    # cells that can ever hold a gem — because a cell under a clearable blocker
    # is a puzzle, not dead space.
    #
    # Recomputed from the grids rather than read from the stored field, which is
    # rounded to four places and would sit fractionally above the real value.
    # Measured on the templates AS THE KIT DREW THEM, uncropped. The floor says
    # "no board the story emits is less open than the worst board the kit
    # shipped", which is a fact about the kit and cannot be read off boards this
    # script has already cut down -- `fit_board` consults the floor to decide
    # whether a crop is allowed at all, so deriving the floor from cropped
    # boards would let the crop lower its own bar until anything passed.
    min_open = min(open_ratio(t["grid"]) for t in templates)

    ensure_dir(LEVELS_RES)

    written = 0
    preserved = 0
    destaticed = 0
    provenance = {}
    failures = []

    # The templates the last RECENT_TEMPLATES levels used, oldest first. The
    # plan is in level order, so this is a plain sliding window. A preserved
    # level goes in it too: level 100's board IS template 100's board, so
    # letting level 101 draw template 100 would hand the player the same shape
    # twice across the boundary between the kit's levels and the generated ones.
    recent = []

    for entry in plan["levels"]:
        number = entry["levelNumber"]
        path = os.path.join(LEVELS_RES, "%d.txt" % number)

        if number <= PRESERVED_THROUGH and number in by_number:
            level = dict(by_number[number])
            # The shipped levels are kept as they are, with one exception: the
            # story forbids a self-acting obstacle before area 019, and a few of
            # the kit's early levels ship a thriving block. Story wins.
            stripped = strip_self_acting(level["grid"])
            if stripped != level["grid"]:
                level["grid"] = stripped
                destaticed += 1

            # ...and one more, added when the board was cropped to 7x9: the
            # kit's hundred are cropped with the rest. A board that kept nine
            # columns here would be a different SIZE from every level after it
            # and would change size under the player at level 101.
            #
            # tools/shipped_levels is NOT touched -- the crop happens on the way
            # out, so the template library still round-trips byte-for-byte
            # against the originals and this is undone by deleting the call.
            level["grid"] = seal_unmatchable_cells(
                fit_board(level["grid"], min_open, level["mode"])
            )
            level["cols"] = len(level["grid"][0]) if level["grid"] else 0
            level["rows"] = len(level["grid"])

            text = format_level(level)
            provenance[str(number)] = {
                "template": number,
                "preserved": True,
                "mode": level["mode"],
                "blockTargetCount": block_target_count(level["grid"]),
            }
            preserved += 1
            used = number
        else:
            rng = random.Random("kingleo/pick/%d" % number)
            template = pick_template(by_rank, entry["difficulty"], rng, recent)
            level = generate(template, entry, min_open)
            text = format_level(level)
            provenance[str(number)] = {
                "template": template["number"],
                "preserved": False,
                "difficulty": entry["difficulty"],
                "areaId": entry["areaId"],
                "mode": level["mode"],
                # Read off the finished grid: thicken_blockers changes how many
                # blocks a BLOCKS-mode level actually contains, and a target
                # asset that disagrees makes the level unwinnable.
                "blockTargetCount": block_target_count(level["grid"]),
                "starThresholds": level["stars"],
                "moves": level["limit"],
            }
            written += 1
            used = template["number"]

        recent.append(used)
        del recent[:-RECENT_TEMPLATES]

        # Every emitted file must survive the same parser the game's own reader
        # mirrors, and must leave enough board to play on.
        try:
            reparsed = parse_level(text, name="%d.txt" % number)
        except Exception as exc:  # noqa: BLE001 - reported, not swallowed
            failures.append("level %d does not re-parse: %s" % (number, exc))
            continue

        ratio = open_ratio(reparsed["grid"])
        if ratio < min_open - 1e-6:
            failures.append(
                "level %d is only %.0f%% open, below the worst shipped board (%.0f%%)"
                % (number, ratio * 100, min_open * 100)
            )
            continue

        with open(path, "w", encoding="utf-8") as fh:
            fh.write(text)
        write_text_meta(path)

    with open(os.path.join(OUT, "level_provenance.json"), "w", encoding="utf-8") as fh:
        json.dump(provenance, fh, indent=1)
        fh.write("\n")

    if failures:
        for failure in failures:
            print("ERROR: " + failure, file=sys.stderr)
        return 1

    print("levels written=%d (%d preserved)" % (written + preserved, preserved))
    if destaticed:
        print(
            "%d shipped levels had a thriving block removed: the story has no "
            "self-acting obstacle before area 019" % destaticed
        )
    print("open-cell floor honoured: %.0f%% (worst shipped board)" % (min_open * 100))
    return 0


if __name__ == "__main__":
    sys.exit(main())
