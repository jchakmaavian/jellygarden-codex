#!/usr/bin/env python3
"""The board skin for the Royal Match rules: six pieces, four universal powerups.

The kit's specials are per colour — `stripedItems[color]` and
`packageItems[color]`, six variants of each. Under the new rules a powerup is
universal: a rocket is a rocket whatever cleared it. The colour bomb already
works this way (`bombItems[0]`, no index), so this follows a precedent the kit
set rather than inventing one.

That means **twelve distinct images fill twenty-seven filenames**:

    six pieces        book red, coin orange, gem purple, shield blue,
                      leaf green, crown yellow — kit colour order, which is
                      load-bearing because the explosion particle textures are
                      tinted per item index
    two rams          the line clear is a battering ram, drawn twice — lying
                      down for the row, standing up for the column. The vertical
                      one is drawn, not rotated: a rotated ram puts its iron
                      head where the light does not fall. item_01..06_stripes_horiz
                      are all the same file, and so are all six _stripes_vert
    one bomb          all six _extra are the same file
    one colour bomb   candy10_choco
    two collectibles  ingredient_01, ingredient_02

Every kit filename, size and GUID is preserved, so prefabs, animation clips,
particle systems and Target assets keep working untouched.

    python3 tools/item_skin_royal.py --stage <dir>   # write nothing to the game
    python3 tools/item_skin_royal.py --preview       # contact sheet in /tmp
    python3 tools/item_skin_royal.py                 # apply to the project
"""

import argparse
import os
import sys

from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from story_common import PROJECT_ROOT  # noqa: E402

MASTERS = os.path.join(os.path.dirname(os.path.abspath(__file__)), "art", "royal")
ITEMS = os.path.join(PROJECT_ROOT, "Assets", "JellyGarden", "Textures", "Items")

SIZE = 128

# Kit index, master, the colour its explosion particles are tinted with. The
# order is the kit's and must not move.
PIECES = [
    ("item_01", "book", (214, 48, 49)),
    ("item_02", "coin", (243, 156, 26)),
    ("item_03", "gem", (162, 47, 199)),
    ("item_04", "shield", (32, 132, 220)),
    ("item_05", "leaf", (38, 178, 62)),
    ("item_06", "crown", (243, 196, 26)),
]

# A powerup is one image, repeated across all six colour slots.
UNIVERSAL = [
    ("_stripes_horiz", "ram_h", 120),
    ("_stripes_vert", "ram_v", 120),
    ("_extra", "bomb", 112),
]

SINGLES = [
    ("candy10_choco", "wheel", 118),
    ("ingredient_01", "locket", 114),
    ("ingredient_02", "ring", 116),
]


def load(name):
    return Image.open(os.path.join(MASTERS, name + ".png")).convert("RGBA")


def fit(image, box):
    trimmed = image.crop(image.getbbox())
    trimmed.thumbnail((box, box), Image.LANCZOS)
    canvas = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    canvas.paste(trimmed, ((SIZE - trimmed.width) // 2,
                           (SIZE - trimmed.height) // 2), trimmed)
    return canvas


def build():
    sprites = {}

    for kit_name, master, _colour in PIECES:
        sprites[kit_name] = fit(load(master), 112)

    for suffix, master, box in UNIVERSAL:
        art = fit(load(master), box)
        for kit_name, _master, _colour in PIECES:
            # The same pixels under six names. That is the point.
            sprites[kit_name + suffix] = art

    for kit_name, master, box in SINGLES:
        sprites[kit_name] = fit(load(master), box)

    return sprites


def contact_sheet(sprites, cols=7):
    names = sorted(sprites)
    rows = (len(names) + cols - 1) // cols
    sheet = Image.new("RGBA", (cols * SIZE, rows * SIZE), (34, 34, 40, 255))
    for index, name in enumerate(names):
        sheet.alpha_composite(sprites[name],
                              ((index % cols) * SIZE, (index // cols) * SIZE))
    return sheet


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--preview", action="store_true",
                        help="write a contact sheet to /tmp and touch nothing")
    parser.add_argument("--stage", metavar="DIR",
                        help="write the 27 sprites to DIR instead of the project")
    args = parser.parse_args()

    sprites = build()

    if args.preview:
        contact_sheet(sprites).save("/tmp/item_skin_royal_preview.png")
        print("preview: /tmp/item_skin_royal_preview.png (%d sprites)" % len(sprites))
        return 0

    target_dir = args.stage or ITEMS
    if args.stage:
        os.makedirs(target_dir, exist_ok=True)

    for name, image in sorted(sprites.items()):
        kit_path = os.path.join(ITEMS, name + ".png")
        if not os.path.exists(kit_path):
            print("ERROR: %s is not a kit sprite" % name, file=sys.stderr)
            return 1
        if Image.open(kit_path).size != (SIZE, SIZE):
            print("ERROR: %s is not 128x128 in the kit" % name, file=sys.stderr)
            return 1
        image.save(os.path.join(target_dir, name + ".png"))

    where = "staged in %s" % target_dir if args.stage else "written into the project"
    print("%d sprites %s" % (len(sprites), where))
    return 0


if __name__ == "__main__":
    sys.exit(main())
