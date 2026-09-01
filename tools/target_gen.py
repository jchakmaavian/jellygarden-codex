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

A BLOCKS target is deliberately left alone. It is not the board's block count —
level 19 ships a target of 24 against a board of 56, and levels 28 and 35 ship a
target of 0 because their real objective is the score. It is how many blocks the
player must destroy, and the shipped assets set it well below what the board
offers. Since the only structural mutation is promoting single blocks to double
ones, a generated board always offers at least as many destroy events as its
template did, so the inherited target stays reachable — which is exactly what
the check below asserts.

Icon GUIDs are carried over untouched, so generated levels show the same target
icons the kit ships.

    python3 tools/target_gen.py
"""

import json
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from level_templates import block_target_count, parse_level  # noqa: E402
from story_common import (  # noqa: E402
    LEVELS_RES,
    OUT,
    TARGETS_RES,
    ensure_dir,
    write_native_meta,
)

PRESERVED_THROUGH = 100

TARGET_SCORE = 0
TARGET_COLLECT = 1
TARGET_INGREDIENT = 2
TARGET_BLOCKS = 3

TARGET_LEVEL_SCRIPT_GUID = "64dfa6917005141638ae80ab5e1a157a"

ENTRY_RE = re.compile(
    r"- type: (?P<type>\d+)\s*\n"
    r"\s+icon: \{fileID: (?P<iconFile>-?\d+), guid: (?P<iconGuid>[a-f0-9]+), type: (?P<iconType>\d+)\}\s*\n"
    r"\s+color: (?P<color>-?\d+)\s*\n"
    r"\s+(?:targetCount|count): (?P<count>-?\d+)"
)

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


def read_target_asset(number):
    """Parse an existing LevelN.asset into a list of target entries."""
    path = os.path.join(TARGETS_RES, "Level%d.asset" % number)
    if not os.path.exists(path):
        return None

    with open(path, encoding="utf-8") as fh:
        text = fh.read()

    entries = []
    for match in ENTRY_RE.finditer(text):
        entries.append(
            {
                "type": int(match.group("type")),
                "iconFile": match.group("iconFile"),
                "iconGuid": match.group("iconGuid"),
                "iconType": match.group("iconType"),
                "color": int(match.group("color")),
                "count": int(match.group("count")),
            }
        )
    return entries


def write_target_asset(number, entries):
    path = os.path.join(TARGETS_RES, "Level%d.asset" % number)
    body = HEADER.replace("__NAME__", "Level%d" % number)
    for entry in entries:
        body += ENTRY % entry
    with open(path, "w", encoding="utf-8") as fh:
        fh.write(body)
    write_native_meta(path)


def retarget(entries, level):
    """Rewrite only the count that has to track the generated board."""
    out = []
    for entry in entries:
        entry = dict(entry)
        if entry["type"] == TARGET_SCORE:
            entry["count"] = level["stars"][0]
        out.append(entry)
    return out


def main():
    plan = json.load(open(os.path.join(OUT, "level_plan.json"), encoding="utf-8"))
    provenance = json.load(
        open(os.path.join(OUT, "level_provenance.json"), encoding="utf-8")
    )

    ensure_dir(TARGETS_RES)

    written = 0
    preserved = 0
    failures = []

    for entry in plan["levels"]:
        number = entry["levelNumber"]

        if number <= PRESERVED_THROUGH and read_target_asset(number) is not None:
            preserved += 1
            continue

        source_number = provenance[str(number)]["template"]
        source = read_target_asset(source_number)
        if not source:
            failures.append(
                "level %d: template %d has no target asset to copy"
                % (number, source_number)
            )
            continue

        level = parse_level(
            open(os.path.join(LEVELS_RES, "%d.txt" % number), encoding="utf-8").read(),
            name="%d.txt" % number,
        )
        write_target_asset(number, retarget(source, level))
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
    print("every planned level has a board, a target asset and a .meta")
    return 0


if __name__ == "__main__":
    sys.exit(main())
