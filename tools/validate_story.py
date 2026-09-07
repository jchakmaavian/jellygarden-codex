#!/usr/bin/env python3
"""One command that proves all 150 areas are real, playable content.

Everything the runtime assumes is checked here against the files on disk: the
story is whole, the economy adds up, and every star the story charges has a
board and a target asset behind it.

    python3 tools/validate_story.py
"""

import json
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from level_templates import block_target_count, open_ratio, parse_level  # noqa: E402
from story_common import (  # noqa: E402
    ARC_TITLES,
    LEVELS_RES,
    STORY_RES,
    TARGETS_RES,
    all_areas,
    area_star_cost,
    load_arcs,
)
from target_gen import TARGET_BLOCKS, read_target_asset  # noqa: E402

EXPECTED_AREAS = 150
EXPECTED_TASKS = 806
EXPECTED_STARS = 1208
BEAT_EVERY = 5


class Report:
    def __init__(self):
        self.failures = []

    def check(self, condition, message):
        if not condition:
            self.failures.append(message)
        return condition

    def fail(self, message):
        self.failures.append(message)


def check_story(report):
    areas = all_areas()

    report.check(len(areas) == EXPECTED_AREAS,
                 "expected %d areas, found %d" % (EXPECTED_AREAS, len(areas)))

    tasks = stars = 0
    for index, area in enumerate(areas):
        where = "area %d" % area["id"]

        report.check(area["id"] == index + 1,
                     "area ids are not contiguous at position %d (id %d)" % (index, area["id"]))
        report.check(bool(area.get("name")), "%s has no name" % where)
        report.check(bool(area.get("goal")), "%s has no goal" % where)
        report.check(bool(area.get("clue")), "%s has no clue" % where)
        report.check(len(area["tasks"]) >= 1, "%s has no repair tasks" % where)

        for task in area["tasks"]:
            report.check(int(task.get("stars", 0)) >= 1,
                         "%s task %s costs no stars" % (where, task.get("index")))
            report.check(bool(task.get("label")),
                         "%s task %s has no label" % (where, task.get("index")))

        tasks += len(area["tasks"])
        stars += area_star_cost(area)

    report.check(tasks == EXPECTED_TASKS,
                 "expected %d tasks, found %d" % (EXPECTED_TASKS, tasks))
    report.check(stars == EXPECTED_STARS,
                 "expected %d stars, found %d" % (EXPECTED_STARS, stars))

    return areas, stars


def check_plan(report, areas, total_stars):
    plan_path = os.path.join(STORY_RES, "level_plan.json")
    if not os.path.exists(plan_path):
        report.fail("no level_plan.json — run tools/level_plan.py")
        return None

    with open(plan_path, encoding="utf-8") as fh:
        plan = json.load(fh)

    levels = plan["levels"]

    # The economy's central identity: one level per star the story charges.
    report.check(len(levels) == total_stars,
                 "the story costs %d stars but the plan has %d levels"
                 % (total_stars, len(levels)))

    numbers = [entry["levelNumber"] for entry in levels]
    report.check(numbers == list(range(1, len(levels) + 1)),
                 "level numbers are not contiguous from 1")

    by_area = {}
    for entry in levels:
        by_area.setdefault(entry["areaId"], []).append(entry)

    for area in areas:
        planned = len(by_area.get(area["id"], []))
        cost = area_star_cost(area)
        report.check(planned == cost,
                     "area %d costs %d stars but has %d planned levels"
                     % (area["id"], cost, planned))

    return plan


def check_levels(report, plan):
    if plan is None:
        return {}

    stats = {"levels": 0, "sabotaged": 0, "min_open": 1.0}

    for entry in plan["levels"]:
        number = entry["levelNumber"]
        txt = os.path.join(LEVELS_RES, "%d.txt" % number)
        asset = os.path.join(TARGETS_RES, "Level%d.asset" % number)

        if not report.check(os.path.exists(txt), "level %d has no board" % number):
            continue
        if not report.check(os.path.exists(txt + ".meta"),
                            "level %d board has no .meta" % number):
            continue
        if not report.check(os.path.exists(asset), "level %d has no target asset" % number):
            continue
        if not report.check(os.path.exists(asset + ".meta"),
                            "level %d target asset has no .meta" % number):
            continue

        with open(txt, encoding="utf-8") as fh:
            text = fh.read()

        try:
            level = parse_level(text, name="%d.txt" % number)
        except Exception as exc:  # noqa: BLE001 - reported, not swallowed
            report.fail("level %d does not parse: %s" % (number, exc))
            continue

        stats["levels"] += 1
        stats["min_open"] = min(stats["min_open"], open_ratio(level["grid"]))

        has_thriving = any(cell[1] == "7" for row in level["grid"] for cell in row)
        if has_thriving:
            stats["sabotaged"] += 1

        # The board and the plan must agree about whether this level is sabotaged,
        # or the plan is describing a game that is not the one being played.
        report.check(has_thriving == bool(entry["sabotage"]),
                     "level %d: plan says sabotage=%s but the board says %s"
                     % (number, entry["sabotage"], has_thriving))

        # Nothing acts on its own before Grimm opens the sluice gates.
        if entry["areaId"] < plan["firstSabotageArea"]:
            report.check(not has_thriving,
                         "level %d is in area %d, before sabotage begins, but has a "
                         "self-acting obstacle" % (number, entry["areaId"]))

        targets = read_target_asset(number)
        if not report.check(bool(targets), "level %d target asset has no entries" % number):
            continue

        available = block_target_count(level["grid"])
        for target in targets:
            if target["type"] == TARGET_BLOCKS:
                report.check(target["count"] <= available,
                             "level %d asks for %d blocks but its board holds %d"
                             % (number, target["count"], available))

    return stats


def check_presentation(report, areas):
    art = os.path.join(STORY_RES, "Art")

    for theme in ("castle", "village", "mines", "border", "crypts", "siege"):
        report.check(
            any(os.path.exists(os.path.join(art, "backdrop_%s%s" % (theme, ext)))
                for ext in (".jpg", ".png")),
            "arc theme '%s' has no backdrop" % theme)

    for character in ("leo", "freya", "grimm", "mimi", "cook", "gardener",
                      "alfred", "roderick"):
        report.check(
            any(os.path.exists(os.path.join(art, "portrait_%s%s" % (character, ext)))
                for ext in (".jpg", ".png")),
            "%s has no portrait" % character)

    cutscenes_path = os.path.join(STORY_RES, "cutscenes.json")
    if not report.check(os.path.exists(cutscenes_path), "no cutscenes.json"):
        return

    with open(cutscenes_path, encoding="utf-8") as fh:
        cutscenes = json.load(fh)["cutscenes"]

    by_area = {scene["areaId"]: scene for scene in cutscenes}
    for area in areas:
        if area["id"] % BEAT_EVERY != 0:
            continue
        scene = by_area.get(area["id"])
        if not report.check(scene is not None, "arc beat area %d has no scene" % area["id"]):
            continue

        minimum = 6 if area["id"] % 25 == 0 else 3
        report.check(len(scene["lines"]) >= minimum,
                     "area %d has %d lines, needs %d"
                     % (area["id"], len(scene["lines"]), minimum))


def print_summary(areas, plan, stats):
    arcs = load_arcs()
    print()
    print("%-4s %-24s %6s %6s %6s %8s" % ("ARC", "TITLE", "AREAS", "TASKS", "STARS", "LEVELS"))
    print("-" * 60)

    by_area_levels = {}
    if plan:
        for entry in plan["levels"]:
            by_area_levels[entry["areaId"]] = by_area_levels.get(entry["areaId"], 0) + 1

    for arc in range(1, 7):
        arc_areas = arcs[arc]["areas"]
        tasks = sum(len(a["tasks"]) for a in arc_areas)
        stars = sum(area_star_cost(a) for a in arc_areas)
        levels = sum(by_area_levels.get(a["id"], 0) for a in arc_areas)
        print("%-4d %-24s %6d %6d %6d %8d"
              % (arc, ARC_TITLES[arc], len(arc_areas), tasks, stars, levels))

    print("-" * 60)
    print("%-4s %-24s %6d %6d %6d %8d"
          % ("", "TOTAL", len(areas),
             sum(len(a["tasks"]) for a in areas),
             sum(area_star_cost(a) for a in areas),
             sum(by_area_levels.values())))
    print()
    print("boards parsed: %d   sabotaged: %d   least open board: %.0f%%"
          % (stats.get("levels", 0), stats.get("sabotaged", 0),
             stats.get("min_open", 0) * 100))


def main():
    report = Report()

    areas, total_stars = check_story(report)
    plan = check_plan(report, areas, total_stars)
    stats = check_levels(report, plan)
    check_presentation(report, areas)

    print_summary(areas, plan, stats)

    if report.failures:
        print()
        for failure in report.failures[:40]:
            print("FAIL: " + failure, file=sys.stderr)
        if len(report.failures) > 40:
            print("... and %d more" % (len(report.failures) - 40), file=sys.stderr)
        print("\n%d checks failed." % len(report.failures), file=sys.stderr)
        return 1

    print("All checks passed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
