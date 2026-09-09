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

from check_themes import (  # noqa: E402
    art_path,
    composite,
    item_colours,
    kit_gem_separation_floor,
    mean_colour,
    worst_gem_separation,
)
from story_common import ARC_TITLES, STORY_RES, dump_json  # noqa: E402

# arc -> (theme key, which of the kit's five shipped backdrops suits it,
#         tint applied to the board's own cells, the arc's own colour for the
#         board's ground, the arc's setting)
#
# The kit ships five backdrops and there are six arcs, so the siege reuses the
# crypt backdrop and separates itself on tint — it is the same castle, burning.
# That sharing only ever shows if the story art goes missing, which is why
# check_themes.py reports it only when both arcs actually need the fallback.
#
# About the board fill: the kit's cell sprite is flat black at alpha 0.50, and
# its alternate at 0.65, so a cell has never had a ground of its own — it showed
# the backdrop picture through a dark checker, which reads as an outline around
# a hole. The fill replaces that sprite with a white square, so the colour here
# is the board's ground exactly.
#
# The fourth component is its alpha. It has been 0.55 and then 0.85 — the board
# as tinted glass over the arc's picture, then as a near-solid pale slab once
# the shipped backdrops turned out to be dark (castle 78, village 53, siege 50
# mean luminance against 152 for the project's own reference room art) and were
# dragging a light ground to a muddy 41-68.
#
# Neither is what the board wants. The ground is now a SHADE rather than a
# surface: part transparent, and what it does is darken. The backdrop reads
# through it as itself, the cells read as a dimmed pane over the room, and the
# darkness that used to be a problem is the point — a dark ground is what the
# kit's own board was (flat black at alpha 0.50), only carrying the arc's hue.
#
# 0.55 is past the kit's own 0.50: the room is still legible behind the board
# and the gems have a genuinely dark field to sit on, which is what the six
# high-chroma kit gems want under them.
BOARD_ALPHA = 0.55

# Where a ground goes when its own colour will not do.
#
# Six gems cover the whole hue wheel at high chroma, so there is no hue a board
# can be that is far from all of them; what separates a ground from a gem is
# being light and low-chroma. This is StoryUi.RoyalCream, the surface every card
# in the game is already made of, so a board that has to give up its own colour
# gives it up to a colour the game already owns rather than to a new one.
STONE = (250 / 255.0, 234 / 255.0, 208 / 255.0)

# What the arc's own colour is taken down to before anything else happens. A
# ground is a shade now, so the hue is kept and the light is not: 0.30 is dark
# enough to read as shadow at alpha 0.35 and far enough off black that the six
# arcs are still six different boards.
GROUND_DARKEN = 0.30

# Where a ground goes when even the darkened arc colour will not separate from
# a gem. Not black — a hair of blue in it keeps it from reading as a hole, and
# it is the same near-neutral the kit's own black-at-half-alpha cell composites
# to over a lit backdrop.
SHADOW = (0.07, 0.06, 0.09)

# How finely the blend toward STONE is searched. An arc keeps as much of its own
# colour as it can, so the step is small enough that "as much as it can" means
# something and coarse enough that the number in themes.json stays readable.
BLEND_STEP = 0.05

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


def ground_fill(arc, key, colour, gems, floor):
    """The arc's board ground: its own colour, darkened, moved only as far as it must.

    A gem is drawn on this, and a ground the gem merges into is a ground the
    player cannot read the board on. The bar is not invented — it is the worst
    gem separation the kit's own five shipped boards already accept, computed by
    check_themes.kit_gem_separation_floor. So this walks the arc's colour toward
    SHADOW in BLEND_STEP steps and stops at the first one that clears it, which
    leaves an arc that was already fine exactly as it was authored.

    Returns (fill, blend, separation). With no art to measure — a fresh tree
    where asset_import.py has not run — it returns SHADOW, because the safe
    answer when the backdrop is unknown is the one that does not depend on it.
    """
    picture = art_path(key)
    backdrop = mean_colour(picture) if picture else None

    if backdrop is None or not gems or floor is None:
        return list(SHADOW), 1.0, None

    shade = [c * GROUND_DARKEN for c in colour]

    blend = 0.0
    while blend <= 1.0 + 1e-9:
        fill = [shade[i] * (1.0 - blend) + SHADOW[i] * blend for i in range(3)]
        separation = worst_gem_separation(
            composite(tuple(fill) + (BOARD_ALPHA,), backdrop), gems)
        if separation >= floor:
            return [round(c, 3) for c in fill], round(blend, 2), separation
        blend += BLEND_STEP

    # Nothing on the line clears it. SHADOW is the end of that line and the best
    # this can do; check_themes.py is the gate that says so out loud.
    fill = list(SHADOW)
    separation = worst_gem_separation(
        composite(tuple(fill) + (BOARD_ALPHA,), backdrop), gems)
    return [round(c, 3) for c in fill], 1.0, separation


def main():
    gems = item_colours()
    floor = kit_gem_separation_floor(gems)
    if floor is None:
        print("WARNING: no gem sprites to measure; every ground falls back to SHADOW.",
              file=sys.stderr)

    themes = []
    for arc, (key, kit_index, tint, colour, setting) in sorted(SPEC.items()):
        fill, blend, separation = ground_fill(arc, "backdrop_" + key, colour, gems, floor)
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
            # The board's own ground, RGB then alpha. Computed, not authored:
            # the arc's colour taken down to a shade and moved toward SHADOW by
            # the least amount that keeps every one of the kit's six gems
            # readable on it.
            "boardFill": fill + [BOARD_ALPHA],
        })
        print("arc %d %-8s blend %.2f toward shadow -> %s%s" % (
            arc, key, blend, fill,
            "" if separation is None else "  (gem dE %.1f)" % separation))

    dump_json(os.path.join(STORY_RES, "themes.json"), {"themes": themes})

    keys = set(t["themeKey"] for t in themes)
    if len(keys) != len(themes):
        print("ERROR: two arcs share a theme key", file=sys.stderr)
        return 1

    grounds = set(tuple(t["boardFill"]) for t in themes)
    if len(grounds) != len(themes):
        print("ERROR: two arcs share a board ground", file=sys.stderr)
        return 1

    print("themes=%d  gem separation floor %s" % (
        len(themes), "unknown" if floor is None else "%.1f" % floor))
    return 0


if __name__ == "__main__":
    sys.exit(main())
