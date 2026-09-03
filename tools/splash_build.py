#!/usr/bin/env python3
"""Import the King Leo brand art the splash screen wears.

The kit opens on its own Jelly Garden splash — `Textures/menu/main_menu/logo.png`
over `main_menu_banner.png`. Neither is touched: StorySplash swaps the sprites at
runtime, the same way StoryBoardTheme swaps the board's backdrop, so the kit's
own art is still there and turning the feature off gives it straight back.

This puts the master into Resources/Story/Art under the name StorySplash looks
for. The master lives in tools/art/brand/ with the rest of the brand art.

    python3 tools/splash_build.py
"""

import os
import shutil
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from asset_import import sprite_meta  # noqa: E402
from story_common import STORY_RES, ensure_dir  # noqa: E402

TOOLS = os.path.dirname(os.path.abspath(__file__))
MASTERS = os.path.join(TOOLS, "art", "brand")
DEST_ART = os.path.join(STORY_RES, "Art")

# master file -> the key StoryArt.Load resolves
BRAND = {
    "king_leo_logo.png": "splash_logo",
}


def main():
    ensure_dir(DEST_ART)

    problems = []
    count = 0

    for source_name, stem in sorted(BRAND.items()):
        source = os.path.join(MASTERS, source_name)
        if not os.path.exists(source):
            problems.append("missing master: %s" % source_name)
            continue

        dest = os.path.join(DEST_ART, stem + os.path.splitext(source_name)[1])
        shutil.copyfile(source, dest)
        with open(dest + ".meta", "w", encoding="utf-8") as fh:
            fh.write(sprite_meta(dest))
        count += 1
        print("%s -> %s" % (source_name, os.path.basename(dest)))

    for problem in problems:
        print("ERROR: %s" % problem, file=sys.stderr)

    print("brand art=%d" % count)
    return 1 if problems else 0


if __name__ == "__main__":
    sys.exit(main())
