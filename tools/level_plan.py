#!/usr/bin/env python3
"""Map every story star to a concrete playable level number.

The economy is the one from the story bible: a level win pays one star, and
repair tasks are bought with stars. So the number of levels the game needs is
exactly the number of stars the 150 areas cost — 1209.

Levels are numbered 1..1209 in area order, then task order. That keeps level 1
as area 1's first level, which is also the kit's tutorial-shaped level, and it
means a level number alone tells you where in the story you are.

    python3 tools/level_plan.py
"""

import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from story_common import (  # noqa: E402
    ARC_THEMES,
    OUT,
    STORY_RES,
    all_areas,
    dump_json,
    ensure_dir,
)

# GAMEPLAY.md: areas 001-018 are static neglect. At area 019 Grimm opens the
# sluice gates and obstacles start acting on their own.
FIRST_SABOTAGE_AREA = 19

# Sabotage is Grimm pushing back, not a permanent tax. It starts occasional and
# becomes the norm — flagging every level from area 19 onward would put a
# spreading blocker on three quarters of the game, which reads as the designer
# being cruel rather than the villain being active.
SABOTAGE_START_CHANCE = 0.30
SABOTAGE_END_CHANCE = 0.95


def sabotage_chance(area_id):
    if area_id < FIRST_SABOTAGE_AREA:
        return 0.0
    span = max(1, 150 - FIRST_SABOTAGE_AREA)
    t = (area_id - FIRST_SABOTAGE_AREA) / span
    return SABOTAGE_START_CHANCE + (SABOTAGE_END_CHANCE - SABOTAGE_START_CHANCE) * t


def is_sabotaged(area_id, level_number):
    """Deterministic per level, so the plan is stable across regenerations."""
    if area_id < FIRST_SABOTAGE_AREA:
        return False
    # The very first sabotage level is always sabotaged — it is the beat where
    # Grimm opens the sluice gates, and it has a line of dialogue attached.
    rng = random.Random("kingleo/sabotage/%d" % level_number)
    return rng.random() < sabotage_chance(area_id)

# Difficulty floor and ceiling as a fraction of the template library's spread.
# The floor is not 0: area 1 aside, even early boards should have some shape.
DIFFICULTY_FLOOR = 0.02
DIFFICULTY_CEILING = 0.98


def area_difficulty(area_id, arc, index_in_arc, areas_in_arc):
    """
    Difficulty as a 0..1 position in the template library's spread.

    Two curves stacked: a slow global rise across all 150 areas so the game gets
    harder overall, and a per-arc reset dip so entering a new arc feels like a
    new chapter rather than a wall. The dip shrinks as the arcs go on — by arc 6
    there is nowhere soft left to land.
    """
    global_progress = (area_id - 1) / 149.0

    arc_progress = index_in_arc / max(1, areas_in_arc - 1)
    dip_depth = 0.18 * (1.0 - (arc - 1) / 5.0)  # 0.18 in arc 1, 0.0 in arc 6
    dip = dip_depth * (1.0 - arc_progress)

    value = global_progress - dip

    # Area 1 is the onboarding area and must stay unloseable-easy.
    if area_id == 1:
        value = 0.0

    return round(min(DIFFICULTY_CEILING, max(DIFFICULTY_FLOOR if area_id > 1 else 0.0, value)), 4)


def build_plan():
    areas = all_areas()

    by_arc = {}
    for area in areas:
        by_arc.setdefault(area["arc"], []).append(area)

    entries = []
    level_number = 0

    for area in areas:
        arc = area["arc"]
        siblings = by_arc[arc]
        index_in_arc = siblings.index(area)
        difficulty = area_difficulty(area["id"], arc, index_in_arc, len(siblings))

        star_index = 0
        for task in area["tasks"]:
            for _ in range(int(task.get("stars", 1))):
                level_number += 1
                star_index += 1
                entries.append(
                    {
                        "levelNumber": level_number,
                        "areaId": area["id"],
                        "arc": arc,
                        "taskIndex": int(task["index"]),
                        "starIndexWithinArea": star_index,
                        # Nudge difficulty slightly upward within an area so the
                        # last repair is the hardest one you pay for.
                        "difficulty": round(
                            min(
                                DIFFICULTY_CEILING,
                                difficulty + 0.012 * (star_index - 1),
                            ),
                            4,
                        ),
                        "themeKey": ARC_THEMES[arc],
                        "sabotage": is_sabotaged(area["id"], level_number),
                    }
                )

    return entries


def main():
    entries = build_plan()

    # A level plan that does not cover exactly the authored star cost means the
    # economy is wrong somewhere; fail loudly rather than shipping a gap.
    areas = all_areas()
    expected = sum(
        int(t.get("stars", 1)) for area in areas for t in area["tasks"]
    )
    if len(entries) != expected:
        print(
            "ERROR: plan has %d levels but the story costs %d stars"
            % (len(entries), expected),
            file=sys.stderr,
        )
        return 1

    numbers = [e["levelNumber"] for e in entries]
    if numbers != list(range(1, len(entries) + 1)):
        print("ERROR: level numbers are not contiguous from 1", file=sys.stderr)
        return 1

    # The first level of the first sabotage area carries the story beat, so it
    # is forced on regardless of the dice.
    for entry in entries:
        if entry["areaId"] == FIRST_SABOTAGE_AREA:
            entry["sabotage"] = True
            entry["sabotageBeat"] = True
            break

    # Per-area index so the runtime can go from an area to its levels in O(1).
    by_area = {}
    for entry in entries:
        by_area.setdefault(str(entry["areaId"]), []).append(entry["levelNumber"])

    payload = {
        "levelCount": len(entries),
        "firstSabotageArea": FIRST_SABOTAGE_AREA,
        "levels": entries,
        "byArea": by_area,
    }

    ensure_dir(OUT)
    dump_json(os.path.join(OUT, "level_plan.json"), payload, meta=False)
    dump_json(os.path.join(STORY_RES, "level_plan.json"), payload)

    first = entries[0]
    last = entries[-1]
    print("levels=%d" % len(entries))
    sabotaged = sum(1 for e in entries if e["sabotage"])
    print("sabotaged=%d (%.0f%% of levels, from area %d)"
          % (sabotaged, 100.0 * sabotaged / len(entries), FIRST_SABOTAGE_AREA))
    print(
        "difficulty: level 1 (area %d) @ %.3f .. level %d (area %d) @ %.3f"
        % (
            first["areaId"],
            first["difficulty"],
            last["levelNumber"],
            last["areaId"],
            last["difficulty"],
        )
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())
