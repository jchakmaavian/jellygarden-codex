#!/usr/bin/env python3
"""Write the per-arc board themes.

Six arcs move through the kingdom and the board should look like the place the
player is in. This exists as a script rather than a hand-written file so every
generated asset in the project has a generator behind it.

The gems are deliberately not themed; see the note at the top of StoryTheme.cs.

    python3 tools/themes_build.py
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from story_common import ARC_TITLES, STORY_RES, dump_json  # noqa: E402

# arc -> (theme key, which of the kit's five shipped backdrops suits it,
#         tint applied to the board's own cells, the colour the board is drawn
#         on, the arc's setting)
#
# The kit ships five backdrops and there are six arcs, so the siege reuses the
# crypt backdrop and separates itself on tint — it is the same castle, burning.
#
# About the board fill: the kit's cell sprite is flat black at alpha 0.50, and
# its alternate at 0.65, so a cell has never had a ground of its own — it showed
# the backdrop picture through a dark checker, which reads as an outline around
# a hole. The fill is that ground, laid under the cells. Roughly half of it
# reaches the eye through the cell above it, so these values are deliberately
# lighter than the board is meant to look, and the checker still reads on top.
SPEC = {
    1: ("castle",  0, [0.86, 0.84, 0.80], [0.85, 0.74, 0.55],
        "The main castle: gates, halls, kitchens, gardens, library, private chambers."),
    2: ("village", 1, [0.92, 0.88, 0.74], [0.70, 0.82, 0.50],
        "The bridge, the road and the village beyond it."),
    3: ("mines",   2, [0.72, 0.70, 0.78], [0.66, 0.60, 0.78],
        "Mines, trade roads and the places the gold went."),
    4: ("border",  3, [0.78, 0.86, 0.92], [0.56, 0.76, 0.92],
        "The border, the crossing and a rival crown."),
    5: ("crypts",  4, [0.66, 0.64, 0.70], [0.60, 0.56, 0.68],
        "Ruins, monastery and crypt — the old king's secret."),
    6: ("siege",   4, [0.95, 0.74, 0.62], [0.92, 0.58, 0.38],
        "The siege: repairing under fire, room by room."),
}


def main():
    themes = []
    for arc, (key, kit_index, tint, fill, setting) in sorted(SPEC.items()):
        themes.append({
            "arc": arc,
            "title": ARC_TITLES[arc],
            "themeKey": key,
            "setting": setting,
            # The generated arc backdrops are 9:16 — the shape of a portrait
            # mobile board — so they serve as the board backdrop too.
            "boardBackdrop": "backdrop_" + key,
            # Fallback when that art is absent.
            "kitBackdropIndex": kit_index,
            # Applied to the board's own cells only, never to a gem.
            "squareTint": tint,
            # Drawn behind those cells, so the board has a ground and not a hole.
            "boardFill": fill,
        })

    dump_json(os.path.join(STORY_RES, "themes.json"), {"themes": themes})

    keys = set(t["themeKey"] for t in themes)
    if len(keys) != len(themes):
        print("ERROR: two arcs share a theme key", file=sys.stderr)
        return 1

    print("themes=%d" % len(themes))
    return 0


if __name__ == "__main__":
    sys.exit(main())
