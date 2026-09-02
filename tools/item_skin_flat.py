#!/usr/bin/env python3
"""The flat, minimal board skin: six shapes a player can tell apart at a glance.

The first reskin used painted jewels. They looked expensive and read badly —
three of the six were rectangular gems in adjacent hues, and at the size a tile
actually renders that is one blur. This set trades detail for legibility: six
different silhouettes, six flat colours, one dark outline weight, no gradients.

    shield  red      coin   orange    gem    purple
    key     blue     flag   green     crown  yellow

Specials are the base shape plus one marker, so a special never stops being
recognisably its own colour:

    line clear    a white double-arrow bar laid across the shape
    area clear    a small bomb badge in the corner
    colour bomb   the rainbow wheel
    collectibles  the locket and the ring

Writes the same 27 kit filenames in place at 128x128, for the same reasons as
`item_skin.py` — see that file and tools/README.md.

    python3 tools/item_skin_flat.py [--preview] [--mockup]
"""

import argparse
import os
import sys

from PIL import Image, ImageDraw

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from story_common import PROJECT_ROOT  # noqa: E402

MASTERS = os.path.join(os.path.dirname(os.path.abspath(__file__)), "art", "flat")
ITEMS = os.path.join(PROJECT_ROOT, "Assets", "JellyGarden", "Textures", "Items")

SIZE = 128
OUTLINE = (45, 52, 61, 255)

# Kit index, master shape, the colour the explosion particles are tinted with.
# The colour order is the kit's and must not move.
PIECES = [
    ("item_01", "shield", (214, 48, 49)),
    ("item_02", "coin", (243, 146, 26)),
    ("item_03", "gem", (162, 47, 199)),
    ("item_04", "key", (32, 121, 214)),
    ("item_05", "flag", (38, 178, 62)),
    ("item_06", "crown", (243, 196, 26)),
]


def load(name):
    return Image.open(os.path.join(MASTERS, name + ".png")).convert("RGBA")


def fit(image, box, size=SIZE, dy=0):
    trimmed = image.crop(image.getbbox())
    trimmed.thumbnail((box, box), Image.LANCZOS)
    canvas = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    canvas.paste(trimmed, ((size - trimmed.width) // 2,
                           (size - trimmed.height) // 2 + dy), trimmed)
    return canvas


def arrow_bar(vertical):
    """A flat white double-arrow, outlined to match the icons."""
    length, thickness, head = 116, 26, 22
    bar = Image.new("RGBA", (length, thickness + 16), (0, 0, 0, 0))
    draw = ImageDraw.Draw(bar)
    mid = bar.height // 2

    draw.rounded_rectangle(
        (head - 4, mid - thickness // 2, length - head + 4, mid + thickness // 2),
        radius=thickness // 2, fill=(255, 255, 255, 255), outline=OUTLINE, width=5)
    draw.polygon([(2, mid), (head + 6, mid - thickness), (head + 6, mid + thickness)],
                 fill=(255, 255, 255, 255), outline=OUTLINE)
    draw.polygon([(length - 2, mid), (length - head - 6, mid - thickness),
                  (length - head - 6, mid + thickness)],
                 fill=(255, 255, 255, 255), outline=OUTLINE)

    return bar.rotate(90, expand=True) if vertical else bar


def bomb_badge(size=52):
    """The bomb, small, for the corner of an area-clear piece."""
    badge = load("bomb")
    badge = badge.crop(badge.getbbox())
    badge.thumbnail((size, size), Image.LANCZOS)
    return badge


def striped(base, vertical):
    out = base.copy()
    bar = arrow_bar(vertical)
    layer = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    layer.paste(bar, ((SIZE - bar.width) // 2, (SIZE - bar.height) // 2), bar)
    out.alpha_composite(layer)
    return out


def packaged(base, badge):
    out = base.copy()
    layer = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    layer.paste(badge, (SIZE - badge.width - 2, SIZE - badge.height - 2), badge)
    out.alpha_composite(layer)
    return out


def build():
    badge = bomb_badge()
    sprites = {}

    for kit_name, master, _colour in PIECES:
        # The flag and the key are tall and thin; the crown is wide and short.
        # Fitting them all to one box makes the crown look tiny, so the box is
        # the larger dimension and the shapes are allowed to differ in mass.
        base = fit(load(master), 112)
        sprites[kit_name] = base
        sprites[kit_name + "_stripes_horiz"] = striped(base, vertical=False)
        sprites[kit_name + "_stripes_vert"] = striped(base, vertical=True)
        sprites[kit_name + "_extra"] = packaged(base, badge)

    sprites["candy10_choco"] = fit(load("wheel"), 118)
    sprites["ingredient_01"] = fit(load("locket"), 116)
    sprites["ingredient_02"] = fit(load("ring"), 116)
    return sprites


def contact_sheet(sprites, cols=7, cell=SIZE, background=(34, 34, 40, 255)):
    names = sorted(sprites)
    rows = (len(names) + cols - 1) // cols
    sheet = Image.new("RGBA", (cols * cell, rows * cell), background)
    for index, name in enumerate(names):
        sheet.alpha_composite(sprites[name],
                              ((index % cols) * cell, (index // cols) * cell))
    return sheet


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--preview", action="store_true")
    args = parser.parse_args()

    sprites = build()

    if args.preview:
        contact_sheet(sprites).save("/tmp/item_skin_flat_preview.png")
        print("preview: /tmp/item_skin_flat_preview.png (%d sprites)" % len(sprites))
        return 0

    for name, image in sorted(sprites.items()):
        path = os.path.join(ITEMS, name + ".png")
        if not os.path.exists(path):
            print("ERROR: %s is not a kit sprite" % path, file=sys.stderr)
            return 1
        if Image.open(path).size != (SIZE, SIZE):
            print("ERROR: %s is not 128x128" % name, file=sys.stderr)
            return 1
        image.save(path)

    print("reskinned %d board sprites in place (flat set)" % len(sprites))
    return 0


if __name__ == "__main__":
    sys.exit(main())
