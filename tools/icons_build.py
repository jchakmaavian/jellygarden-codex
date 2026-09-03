#!/usr/bin/env python3
"""Cut the HUD and navigation icons out of their sheets and import them.

The ten icons are generated as two sheets of five rather than ten separate
pictures, because a sheet is the only way to be sure they share a style: one
prompt, one lighting setup, one gold. Ten separate generations drift, and a
top bar where the coin and the star were clearly lit by different suns is
exactly the "flat and assembled" look this replaces.

The sheets are painted on flat magenta — a colour none of the icons contain —
so the background comes off by distance from that colour rather than by a
model. That also gives a real alpha ramp on the antialiased edge instead of a
hard cut, which matters at 96 points on a phone: a hard cut around the dark
navy outline reads as a jagged sticker.

    python3 tools/icons_build.py

Masters: tools/art/icons/*_sheet.png
Output:  Assets/JellyGarden/Resources/Story/Art/{icon,nav}_*.png
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from asset_import import sprite_meta  # noqa: E402
from story_common import STORY_RES, ensure_dir  # noqa: E402

try:
    from PIL import Image
except ImportError:
    print("ERROR: needs Pillow — pip3 install Pillow", file=sys.stderr)
    raise SystemExit(1)

TOOLS = os.path.dirname(os.path.abspath(__file__))
MASTERS = os.path.join(TOOLS, "art", "icons")
DEST_ART = os.path.join(STORY_RES, "Art")

# Each sheet, and the keys its five icons carry left to right. The order is
# the order they were asked for, and the sheet is checked to actually hold
# five islands before anything is written — a sheet where two icons touched
# would otherwise silently import as four.
SHEETS = {
    "hud_sheet.png": [
        "icon_coin", "icon_heart", "icon_star", "icon_gear", "icon_plus",
    ],
    "nav_sheet.png": [
        "nav_areas", "nav_leaderboard", "nav_home", "nav_team", "nav_cards",
    ],
    # A sheet of one. The chest was drawn rather than painted for longer than
    # the rest and looked it, sitting on the home screen next to five painted
    # tabs; it is here so it comes out of the same key and the same trim.
    "chest_sheet.png": ["icon_chest"],
}

# The side of the square each icon is written at. 256 is four times the size
# the nav bar draws them and twice the top bar's, which leaves room for a
# retina phone without carrying a megabyte of icon into the build.
SIZE = 256

# Transparent margin left inside that square, in pixels a side. The icons are
# trimmed to their own ink, so without it the gold would touch the edge and
# the bevel would look cut off.
MARGIN = 8

# The background is not perfectly flat — it is a painted magenta with a gentle
# gradient across it, and its corners and its middle measure about 22 apart in
# RGB. So the key is a ramp between two distances rather than one threshold:
# under KEY_FLOOR is background however it drifts, over KEY_TOLERANCE is icon,
# and between them is the antialiased edge, which gets a partial alpha. A
# single threshold either keeps a halo or eats the dark navy outline.
KEY_FLOOR = 48.0
KEY_TOLERANCE = 130.0

# A column with fewer than this many icon pixels is background. A plain
# threshold of zero would let one stray keyed pixel bridge two icons into one.
COLUMN_FLOOR = 3


def background_colour(image):
    """The sheet's flat colour, taken from its four corners."""
    w, h = image.size
    corners = [(0, 0), (w - 1, 0), (0, h - 1), (w - 1, h - 1)]
    samples = [image.getpixel(p)[:3] for p in corners]
    return tuple(sum(c[i] for c in samples) // len(samples) for i in range(3))


def keyed(image, bg):
    """
    The sheet as RGBA, with the background colour lifted out.

    Alpha ramps with distance from the background, and the colour is then
    un-mixed — a half-transparent edge pixel is half background, and leaving
    that in is what puts a magenta fringe around every icon.
    """
    out = Image.new("RGBA", image.size)
    src = image.convert("RGB").load()
    dst = out.load()
    br, bgc, bb = bg

    for y in range(image.height):
        for x in range(image.width):
            r, g, b = src[x, y]
            dist = ((r - br) ** 2 + (g - bgc) ** 2 + (b - bb) ** 2) ** 0.5
            if dist >= KEY_TOLERANCE:
                dst[x, y] = (r, g, b, 255)
                continue
            if dist <= KEY_FLOOR:
                dst[x, y] = (0, 0, 0, 0)
                continue

            a = (dist - KEY_FLOOR) / (KEY_TOLERANCE - KEY_FLOOR)
            # pixel = a*icon + (1-a)*background, solved for icon.
            ur = int(min(255, max(0, (r - (1 - a) * br) / a)))
            ug = int(min(255, max(0, (g - (1 - a) * bgc) / a)))
            ub = int(min(255, max(0, (b - (1 - a) * bb) / a)))
            dst[x, y] = (ur, ug, ub, int(a * 255))

    return out


def columns_of_ink(image):
    """For each column, how many pixels in it belong to an icon."""
    alpha = image.getchannel("A")
    px = alpha.load()
    counts = []
    for x in range(image.width):
        n = 0
        for y in range(image.height):
            if px[x, y] > 24:
                n += 1
        counts.append(n)
    return counts


def islands(counts):
    """The runs of inked columns, as (start, end) pairs."""
    runs = []
    start = None
    for x, n in enumerate(counts):
        if n >= COLUMN_FLOOR:
            if start is None:
                start = x
        elif start is not None:
            runs.append((start, x - 1))
            start = None
    if start is not None:
        runs.append((start, len(counts) - 1))
    return runs


def square(icon):
    """One trimmed icon, centred in a transparent square of SIZE a side."""
    box = icon.getbbox()
    if box is not None:
        icon = icon.crop(box)

    inner = SIZE - MARGIN * 2
    scale = min(inner / icon.width, inner / icon.height)
    icon = icon.resize(
        (max(1, int(round(icon.width * scale))),
         max(1, int(round(icon.height * scale)))),
        Image.LANCZOS)

    out = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    out.paste(icon,
              ((SIZE - icon.width) // 2, (SIZE - icon.height) // 2))
    return out


def main():
    ensure_dir(DEST_ART)

    problems = []
    count = 0

    for sheet_name, keys in sorted(SHEETS.items()):
        source = os.path.join(MASTERS, sheet_name)
        if not os.path.exists(source):
            problems.append("missing sheet: %s" % sheet_name)
            continue

        sheet = Image.open(source)
        cut = keyed(sheet, background_colour(sheet))
        runs = islands(columns_of_ink(cut))

        if len(runs) != len(keys):
            problems.append(
                "%s: found %d icons, expected %d — the sheet's icons must not "
                "touch" % (sheet_name, len(runs), len(keys)))
            continue

        for (left, right), key in zip(runs, keys):
            icon = square(cut.crop((left, 0, right + 1, cut.height)))
            dest = os.path.join(DEST_ART, key + ".png")
            icon.save(dest)
            with open(dest + ".meta", "w", encoding="utf-8") as fh:
                fh.write(sprite_meta(dest))
            count += 1
            print("%s[%d..%d] -> %s.png" % (sheet_name, left, right, key))

    for problem in problems:
        print("ERROR: %s" % problem, file=sys.stderr)

    print("icons=%d" % count)
    return 1 if problems else 0


if __name__ == "__main__":
    sys.exit(main())
