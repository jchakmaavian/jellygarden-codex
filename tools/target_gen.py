#!/usr/bin/env python3
"""Write a Targets/LevelN.asset for every level the story plan asks for.

LevelManager.ProcessGameDataFromString does an unguarded
Resources.Load<TargetLevel>("Targets/Level" + n) and dereferences the result, so
a level without a target asset is not a missing objective — it is a null
reference on load.

Generated assets are the source template's asset with one count rewritten:

  type 0 (SCORE)      -> the generated level's first star threshold
  type 1 (COLLECT)    -> unchanged; the generated .txt keeps the template's
  type 2 (INGREDIENT)    COLLECT COUNT and COLLECT ITEMS lines verbatim
  type 3 (BLOCKS)     -> unchanged

A non-zero BLOCKS target is deliberately left alone. It is not the board's block
count — level 19 ships a target of 24 against a board of 56. It is how many
blocks the player must destroy, and the shipped assets set it at or below what
the board offers. Since the only structural mutation is promoting single blocks
to double ones, a generated board always offers at least as many destroy events
as its template did, so the inherited target stays reachable — which is exactly
what the check below asserts.

A target of COUNT ZERO is a different thing entirely, and is repaired. Done() is
`targetCount - countCollected <= 0`, so a zero target is met before the first
move: it draws a permanently ticked chip in the HUD and contributes nothing to
CheckWinLose. Two kit assets carry one (Level28 and Level35 ship BLOCKS 0;
Level15 ships a second INGREDIENT entry at 0), and every level generated from
those templates inherited it. See repair_dead_targets.

Icon GUIDs are carried over untouched, so generated levels show the same target
icons the kit ships.

    python3 tools/target_gen.py
"""

import json
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from level_templates import block_target_count, parse_level  # noqa: E402
from story_common import (  # noqa: E402
    LEVELS_RES,
    OUT,
    SHIPPED_LEVELS,
    SHIPPED_TARGETS,
    TARGET_BLOCKS,
    TARGET_COLLECT,
    TARGET_INGREDIENT,
    TARGET_SCORE,
    TARGETS_RES,
    ensure_dir,
    read_target_entries,
    write_native_meta,
)

PRESERVED_THROUGH = 100

TARGET_LEVEL_SCRIPT_GUID = "64dfa6917005141638ae80ab5e1a157a"

# LIMIT's first field: 0 counts moves, 1 counts seconds.
LIMIT_MOVES = 0

HEADER = """%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: __SCRIPT_GUID__, type: 3}
  m_Name: __NAME__
  m_EditorClassIdentifier: 
  targets:
    array:
""".replace("__SCRIPT_GUID__", TARGET_LEVEL_SCRIPT_GUID)

ENTRY = """    - type: %(type)d
      icon: {fileID: %(iconFile)s, guid: %(iconGuid)s, type: %(iconType)s}
      color: %(color)d
      targetCount: %(count)d
      countCollected: 0
      guiObj: {fileID: 0}
      blockSprites:
        array: []
"""


def read_target_asset(number, directory=None):
    """Parse a LevelN.asset into a list of target entries."""
    return read_target_entries(
        os.path.join(directory or TARGETS_RES, "Level%d.asset" % number)
    )


def write_target_asset(number, entries):
    path = os.path.join(TARGETS_RES, "Level%d.asset" % number)
    body = HEADER.replace("__NAME__", "Level%d" % number)
    for entry in entries:
        body += ENTRY % entry
    with open(path, "w", encoding="utf-8") as fh:
        fh.write(body)
    write_native_meta(path)


def steepest_shipped_blocks_objective():
    """
    The hardest BLOCKS objective the kit itself ships, as (blocks, moves).

    Used as the ceiling when a missing BLOCKS count has to be filled in: no
    repaired level may ask for more blocks per move than a level a designer
    signed off on. Derived from the shipped set rather than written down, so it
    stays true if that set ever changes. Today it is level 75 — 112 blocks in 37
    moves. Timed levels are excluded: their LIMIT counts seconds, not moves.
    """
    best = None
    for number in range(1, PRESERVED_THROUGH + 1):
        path = os.path.join(SHIPPED_LEVELS, "%d.txt" % number)
        if not os.path.exists(path):
            continue
        level = parse_level(open(path, encoding="utf-8").read(), name="%d.txt" % number)
        if level["limitType"] != LIMIT_MOVES or level["limit"] <= 0:
            continue
        for entry in read_target_asset(number, SHIPPED_TARGETS) or []:
            if entry["type"] != TARGET_BLOCKS or entry["count"] <= 0:
                continue
            if best is None or entry["count"] * best[1] > best[0] * level["limit"]:
                best = (entry["count"], level["limit"])
    return best


def repair_dead_targets(entries, level, steepest):
    """
    Drop or fill any target that is already met before the first move.

    TargetObject.Done() is `targetCount - countCollected <= 0`, so an entry with
    a count of 0 is satisfied on load. CheckWinLose asks every entry whether it
    is Done, so such an entry adds nothing to the win condition — but TargetIcon
    still builds a chip for it and Update() ticks it immediately, so the player
    is shown a goal that was never a goal. Worse for INGREDIENT: Item.GenItem
    iterates every ingredient target when it rolls a new item, so a dead second
    entry starts a second FallingCor for every gem the board spawns.

      SCORE       never dead (its count is the level's first star threshold)
      COLLECT     dropped; the colour it named is simply not part of the goal
      INGREDIENT  dropped; Item.GenItem never spawns it (it gates on !Done())
      BLOCKS      filled from the board, since LevelMakerEditor writes the board's
                  own block count and 0 means the field was never set — 32 of the
                  38 shipped BLOCKS levels ask for exactly what their board holds.
                  Capped at the steepest blocks-per-move the kit ships, so a wall
                  of double blocks under a short move limit does not become
                  unclearable. Dropped if the board holds no blocks at all.
    """
    out = []
    for entry in entries:
        entry = dict(entry)
        if entry["type"] == TARGET_SCORE:
            out.append(entry)
            continue

        if entry["type"] != TARGET_BLOCKS:
            if entry["count"] > 0:
                out.append(entry)
            continue

        available = block_target_count(level["grid"])

        # A count that is already set is kept -- UNLESS the board cannot meet
        # it. It always could until the boards were cropped to 7x9: a target
        # inherited its template's count and the template's board came with it,
        # so the two agreed by construction. A cropped board holds fewer blocks
        # than the board the count was written for, and 193 levels came out of
        # the crop asking for blocks that are no longer there -- level 136 asks
        # 23 of a board holding 14. Unwinnable, and `validate_story` says so.
        #
        # Clamping DOWN is the only honest repair here: the goal becomes "clear
        # what this board has", which is what the kit's own BLOCKS levels ask
        # (32 of its 38 ask for exactly their board's count).
        if entry["count"] > 0:
            # A board with no blocks left cannot carry a BLOCKS goal at all.
            # `fit_board` keeps such a level uncropped precisely so this does
            # not happen, so reaching here means a board genuinely ships with no
            # blocks and the inherited goal is a lie either way -- dropping it
            # is caught downstream as "MODE 3 with no type-3 entry", which is
            # the honest error rather than an unwinnable level.
            if available <= 0:
                continue
            if entry["count"] > available:
                entry["count"] = available
            out.append(entry)
            continue
        if available <= 0:
            continue
        if steepest and level["limitType"] == LIMIT_MOVES and level["limit"] > 0:
            available = min(available, steepest[0] * level["limit"] // steepest[1])
        if available <= 0:
            continue
        entry["count"] = available
        out.append(entry)
    return out


def retarget(entries, level, steepest=None):
    """Rewrite only the count that has to track the generated board."""
    out = []
    for entry in entries:
        entry = dict(entry)
        if entry["type"] == TARGET_SCORE:
            entry["count"] = level["stars"][0]
        out.append(entry)
    return repair_dead_targets(out, level, steepest)


def main():
    plan = json.load(open(os.path.join(OUT, "level_plan.json"), encoding="utf-8"))
    provenance = json.load(
        open(os.path.join(OUT, "level_provenance.json"), encoding="utf-8")
    )

    ensure_dir(TARGETS_RES)

    steepest = steepest_shipped_blocks_objective()

    written = 0
    preserved = 0
    repaired = []
    failures = []

    for entry in plan["levels"]:
        number = entry["levelNumber"]

        # Every template's asset is read from tools/shipped_targets, never from
        # Resources/Targets. Resources holds this script's own output, so
        # sourcing from it would make the second run differ from the first.
        if number <= PRESERVED_THROUGH:
            source = read_target_asset(number, SHIPPED_TARGETS)
        else:
            source = read_target_asset(
                provenance[str(number)]["template"], SHIPPED_TARGETS
            )
        if not source:
            failures.append("level %d has no template target asset to copy" % number)
            continue

        level = parse_level(
            open(os.path.join(LEVELS_RES, "%d.txt" % number), encoding="utf-8").read(),
            name="%d.txt" % number,
        )

        if number <= PRESERVED_THROUGH:
            # The kit's own assets are kept as they are, with one exception:
            # a target that is already met on load is not a goal. Only the three
            # that carry one are rewritten; the other 97 files are left untouched
            # on disk so the kit's formatting survives.
            fixed = repair_dead_targets(source, level, steepest)
            if fixed != source:
                write_target_asset(number, fixed)
                repaired.append(number)
            preserved += 1
            continue

        write_target_asset(number, retarget(source, level, steepest))
        written += 1

    # Nothing may reach the game without both halves present, and a BLOCKS level
    # that asks for more blocks than its board holds is unwinnable.
    unreachable = 0
    for entry in plan["levels"]:
        number = entry["levelNumber"]
        txt = os.path.join(LEVELS_RES, "%d.txt" % number)
        asset = os.path.join(TARGETS_RES, "Level%d.asset" % number)
        if not os.path.exists(txt):
            failures.append("level %d has no board" % number)
            continue
        if not os.path.exists(asset):
            failures.append("level %d has no target asset" % number)
            continue
        if not os.path.exists(asset + ".meta"):
            failures.append("level %d target asset has no .meta" % number)
            continue

        level = parse_level(open(txt, encoding="utf-8").read(), name="%d.txt" % number)
        targets = read_target_asset(number)
        if not targets:
            failures.append("level %d target asset has no entries" % number)
            continue

        # The level's MODE and its target entries have to describe the same
        # objective. They agree by construction — a generated level keeps its
        # template's mode and its template's targets — so a failure here means a
        # template and its asset were mismatched to begin with.
        required_type = {
            0: TARGET_SCORE,
            1: TARGET_COLLECT,
            2: TARGET_INGREDIENT,
            3: TARGET_BLOCKS,
        }[level["mode"]]
        if not any(t["type"] == required_type for t in targets):
            failures.append(
                "level %d is MODE %d but its target asset has no type-%d entry"
                % (number, level["mode"], required_type)
            )

        available_blocks = block_target_count(level["grid"])
        for target in targets:
            if target["type"] == TARGET_BLOCKS and target["count"] > available_blocks:
                unreachable += 1
                failures.append(
                    "level %d asks the player to destroy %d blocks but its board "
                    "only holds %d" % (number, target["count"], available_blocks)
                )
            if target["type"] == TARGET_SCORE and target["count"] != level["stars"][0]:
                failures.append(
                    "level %d scores its first star at %d but its target asks for %d"
                    % (number, level["stars"][0], target["count"])
                )

    if failures:
        for failure in failures[:40]:
            print("ERROR: " + failure, file=sys.stderr)
        if len(failures) > 40:
            print("... and %d more" % (len(failures) - 40), file=sys.stderr)
        return 1

    print("targets written=%d (%d preserved)" % (written + preserved, preserved))
    if steepest:
        print(
            "steepest shipped BLOCKS objective: %d blocks in %d moves"
            % (steepest[0], steepest[1])
        )
    if repaired:
        print(
            "%d shipped assets had a target that was already met on load: %s"
            % (len(repaired), ", ".join("Level%d" % n for n in repaired))
        )
    print("every planned level has a board, a target asset and a .meta")
    return 0


if __name__ == "__main__":
    sys.exit(main())
