#!/usr/bin/env python3
"""Copy the authored story JSON into Unity Resources and build the arc index.

Source of truth stays in /Users/avian/Desktop/story. Nothing here edits the
story; it only moves it somewhere Unity can Resources.Load it and adds the small
index file the map's chapter banners need.

    python3 tools/story_sync.py
"""

import json
import os
import shutil
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from story_common import (  # noqa: E402
    ARC_TITLES,
    STORY_RES,
    STORY_ROOT,
    area_star_cost,
    dump_json,
    ensure_dir,
    write_text_meta,
)

SOURCE_DIR = os.path.join(
    STORY_ROOT, "game", "unity", "Assets", "StreamingAssets", "story"
)


def main():
    ensure_dir(STORY_RES)

    arcs = {}
    for n in range(1, 7):
        src = os.path.join(SOURCE_DIR, "arc%d.json" % n)
        dst = os.path.join(STORY_RES, "arc%d.json" % n)
        shutil.copyfile(src, dst)
        write_text_meta(dst)
        with open(dst, encoding="utf-8") as fh:
            arcs[n] = json.load(fh)

    index = {"totalAreas": 0, "totalTasks": 0, "totalStars": 0, "arcs": []}
    for n in range(1, 7):
        areas = arcs[n]["areas"]
        tasks = sum(len(a["tasks"]) for a in areas)
        stars = sum(area_star_cost(a) for a in areas)
        index["arcs"].append(
            {
                "arc": n,
                "title": ARC_TITLES[n],
                "firstArea": areas[0]["id"],
                "lastArea": areas[-1]["id"],
                "areaCount": len(areas),
                "taskCount": tasks,
                "starCount": stars,
            }
        )
        index["totalAreas"] += len(areas)
        index["totalTasks"] += tasks
        index["totalStars"] += stars

    dump_json(os.path.join(STORY_RES, "index.json"), index)

    print(
        "areas=%d tasks=%d stars=%d"
        % (index["totalAreas"], index["totalTasks"], index["totalStars"])
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())
