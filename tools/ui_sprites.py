#!/usr/bin/env python3
"""Copy the kit sprites the story screens need into Resources.

The story screens are built in code at runtime, so they cannot hold a serialised
reference to a sprite the way a prefab does — they have to Resources.Load one.
The kit's UI sprites live outside Resources, where a built player cannot reach
them, so the handful the story uses are copied in.

Each copy takes the original's .meta verbatim apart from the GUID, so it keeps
the sprite mode, pivot, pixels-per-unit and compression the artist set. Copies,
not moves: every existing prefab keeps pointing at the original.

    python3 tools/ui_sprites.py
"""

import os
import re
import shutil
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from story_common import (  # noqa: E402
    JELLY,
    STORY_RES,
    ensure_dir,
    guid_for,
    rel_asset_path,
)

DEST = os.path.join(STORY_RES, "UI")

# name in Resources/Story/UI  ->  path under Assets/JellyGarden
WANTED = {
    "panel": "Textures/menu/play_menu/play_menu_01.png",
    "button": "Textures/menu/play_menu/play_button.png",
    "star": "Textures/menu/play_menu/star_01.png",
    "banner": "Textures/menu/map_menu/setting_banner.png",
    "close": "Textures/menu/map_menu/setting_button.png",
    "next": "Textures/menu/complete_menu/next_button.png",
    "cloud": "Textures/menu/play_menu/cloud.png",
    "lock": "Textures/map/map_overlay_02.png",

    # The three pre-level boosters the level popup offers, plus the kit's own
    # locked-slot plate. These are the kit's booster-shop icons, which are the
    # only art it ships for a booster outside a board.
    "booster_stripes": "Textures/menu/booster_shop/booster_5_stripes_jelly.png",
    "booster_packages": "Textures/menu/booster_shop/booster_5_pakages.png",
    "booster_colorbomb": "Textures/menu/booster_shop/booster_colorful_bomb.png",
    "booster_locked": "Textures/menu/play_menu/play_booster_locked.png",
}


def copy_sprite(dest_name, relative_source):
    source = os.path.join(JELLY, relative_source)
    if not os.path.exists(source):
        return "missing source: %s" % relative_source

    source_meta = source + ".meta"
    if not os.path.exists(source_meta):
        return "source has no .meta: %s" % relative_source

    extension = os.path.splitext(source)[1]
    dest = os.path.join(DEST, dest_name + extension)
    shutil.copyfile(source, dest)

    with open(source_meta, encoding="utf-8") as fh:
        meta = fh.read()

    # Same import settings, new identity. Anything else would either collide with
    # the original's GUID or silently reimport at different settings.
    meta = re.sub(
        r"^guid: [0-9a-f]+$",
        "guid: " + guid_for(rel_asset_path(dest)),
        meta,
        count=1,
        flags=re.MULTILINE,
    )
    meta = re.sub(r"^AssetOrigin:\n(?:  .*\n)*", "", meta, flags=re.MULTILINE)

    with open(dest + ".meta", "w", encoding="utf-8") as fh:
        fh.write(meta)

    return None


def main():
    ensure_dir(DEST)

    failures = []
    copied = 0
    for name, relative in sorted(WANTED.items()):
        problem = copy_sprite(name, relative)
        if problem:
            failures.append(problem)
        else:
            copied += 1

    for problem in failures:
        print("ERROR: " + problem, file=sys.stderr)

    print("ui sprites copied=%d of %d" % (copied, len(WANTED)))
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(main())
