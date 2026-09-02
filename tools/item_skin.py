#!/usr/bin/env python3
"""Reskin the match-3 board from candy to King Leo's crown jewels.

The kit ships a candy game: jelly beans, lollipops, a chocolate bomb. The story
is about a king putting his castle back together, so the pieces the player
actually swaps are the castle's treasure — a ruby, a sapphire, an emerald, a
topaz, the queen's amethyst ring and the crown itself.

Nothing is deleted and nothing is renamed. Every kit PNG is overwritten in
place at its original size, so the sprite GUIDs, the sprite *names*
(LevelManager matches target icons by name), the prefab wiring, the animation
clips and every particle system keep working untouched. That is the whole
reason this is a pixel rewrite rather than an asset re-import.

    python3 tools/item_skin.py [--preview]

--preview writes a contact sheet to /tmp instead of touching the project.

Masters live in tools/art/pieces/ at 512px with alpha. Five were generated for
this pass; the rest are the story prototype's own art, already paid for.
"""

import argparse
import os
import sys

from PIL import Image, ImageFilter

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from story_common import PROJECT_ROOT  # noqa: E402

MASTERS = os.path.join(os.path.dirname(os.path.abspath(__file__)), "art", "pieces")
ITEMS = os.path.join(PROJECT_ROOT, "Assets", "JellyGarden", "Textures", "Items")

# The kit's six colours, in kit order, and what each becomes. The colour is
# kept because the explosion particle textures are tinted per item index — a
# green item that shattered into red sparks would be the VFX regressing.
#
# The last field crops the master before it is fitted. The queen's amethyst ring
# is mostly silver band, and at tile size that read as a grey blob rather than
# as the purple piece, so it is cropped down onto its stone.
PIECES = [
    ("item_01", "ruby", (196, 30, 46), None),                     # red
    ("item_02", "topaz", (232, 126, 12), None),                   # orange
    ("item_03", "amethyst", (146, 60, 190), (0.04, 0.0, 0.96, 0.70)),  # purple
    ("item_04", "sapphire", (32, 132, 220), None),                # blue
    ("item_05", "emerald", (46, 168, 58), None),                  # green
    ("item_06", "crown", (240, 190, 30), None),                   # yellow
]

SIZE = 128


def load(name, crop=None):
    image = Image.open(os.path.join(MASTERS, name + ".png")).convert("RGBA")
    if crop is None:
        return image
    bbox = image.getbbox()
    left, top, right, bottom = bbox
    box_w, box_h = right - left, bottom - top
    return image.crop((left + int(box_w * crop[0]), top + int(box_h * crop[1]),
                       left + int(box_w * crop[2]), top + int(box_h * crop[3])))


def fit(image, box, size=SIZE):
    """The master, centred in a size x size frame, occupying `box` pixels."""
    trimmed = image.crop(image.getbbox())
    trimmed.thumbnail((box, box), Image.LANCZOS)
    canvas = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    canvas.paste(trimmed,
                 ((size - trimmed.width) // 2, (size - trimmed.height) // 2),
                 trimmed)
    return canvas


def glow(colour, radius, size=SIZE):
    """A soft disc of colour, so a piece reads at board scale."""
    disc = Image.new("L", (size, size), 0)
    pixels = disc.load()
    for y in range(size):
        for x in range(size):
            dx, dy = x - size / 2.0, y - size / 2.0
            if dx * dx + dy * dy <= radius * radius:
                pixels[x, y] = 255
    solid = Image.new("RGBA", (size, size), colour + (255,))
    solid.putalpha(disc.filter(ImageFilter.GaussianBlur(radius / 2.5)))
    return solid


def striped(base, rocket, vertical):
    """A line-clear piece: the jewel with a gilded lance driven through it.

    The kit paints white stripes on the candy. The story's equivalent is the
    rocket the prototype already drew, laid across the jewel — same read at a
    glance, and it names the effect instead of decorating it.
    """
    bar = rocket.crop(rocket.getbbox())
    if vertical:
        bar = bar.rotate(90, expand=True, resample=Image.BICUBIC)
        bar.thumbnail((52, 122), Image.LANCZOS)
    else:
        bar.thumbnail((122, 52), Image.LANCZOS)

    out = base.copy()
    shadow = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    shadow.paste(bar, ((SIZE - bar.width) // 2 + 2, (SIZE - bar.height) // 2 + 3), bar)
    dark = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    dark.putalpha(shadow.getchannel("A").point(lambda a: int(a * 0.45)))
    out.alpha_composite(dark.filter(ImageFilter.GaussianBlur(1.4)))

    layer = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    layer.paste(bar, ((SIZE - bar.width) // 2, (SIZE - bar.height) // 2), bar)
    out.alpha_composite(layer)
    return out


def packaged(master, keg, colour):
    """An area-clear piece: the jewel lashed to a powder keg, fuse lit.

    The kit's version is the candy in a "Boom!" wrapper. Same idea, and the
    jewel stays large enough that the colour still reads at board scale.
    """
    out = fit(keg, 126)

    jewel = master.crop(master.getbbox())
    jewel.thumbnail((66, 66), Image.LANCZOS)

    halo = glow(colour, 26)
    halo.putalpha(halo.getchannel("A").point(lambda a: int(a * 0.55)))
    out.alpha_composite(halo)

    layer = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    layer.paste(jewel, ((SIZE - jewel.width) // 2, (SIZE - jewel.height) // 2 + 8),
                jewel)
    out.alpha_composite(layer)
    return out


def build():
    """Every sprite this reskin produces, keyed by its kit filename."""
    rocket = load("rocket")
    keg = load("keg")
    sprites = {}

    for kit_name, master_name, colour, crop in PIECES:
        master = load(master_name, crop)
        base = fit(master, 118)
        sprites[kit_name] = base
        sprites[kit_name + "_stripes_horiz"] = striped(base, rocket, vertical=False)
        sprites[kit_name + "_stripes_vert"] = striped(base, rocket, vertical=True)
        sprites[kit_name + "_extra"] = packaged(master, keg, colour)

    # The colour bomb, and the two collectibles that fall to the board floor.
    sprites["candy10_choco"] = fit(load("orb"), 124)
    sprites["ingredient_01"] = fit(load("locket"), 122)
    sprites["ingredient_02"] = fit(load("signet"), 122)
    return sprites


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--preview", action="store_true",
                        help="write a contact sheet instead of the project files")
    args = parser.parse_args()

    sprites = build()

    if args.preview:
        names = sorted(sprites)
        cols = 7
        rows = (len(names) + cols - 1) // cols
        sheet = Image.new("RGBA", (cols * SIZE, rows * SIZE), (34, 34, 40, 255))
        for index, name in enumerate(names):
            sheet.alpha_composite(sprites[name],
                                  ((index % cols) * SIZE, (index // cols) * SIZE))
        out = "/tmp/item_skin_preview.png"
        sheet.save(out)
        print("preview: %s (%d sprites)" % (out, len(names)))
        return 0

    written = 0
    for name, image in sorted(sprites.items()):
        path = os.path.join(ITEMS, name + ".png")
        if not os.path.exists(path):
            print("ERROR: %s is not a kit sprite; refusing to create it" % path,
                  file=sys.stderr)
            return 1
        before = Image.open(path).size
        if before != (SIZE, SIZE):
            print("ERROR: %s is %s, expected 128x128" % (name, before),
                  file=sys.stderr)
            return 1
        image.save(path)
        written += 1

    print("reskinned %d board sprites in place (same names, same size, same GUIDs)"
          % written)
    return 0


if __name__ == "__main__":
    sys.exit(main())
