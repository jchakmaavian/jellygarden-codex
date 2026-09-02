#!/usr/bin/env python3
"""Reskin the boosters from candy to the king's siege gear.

The board is jewels now; the buttons under it were still lollipops and candy
bombs. This replaces the artwork *inside* the kit's button plates and badges
and leaves the plates themselves alone, because those plates are what the
kit's button animations scale, tint and pulse. Same files, same sizes, same
GUIDs — only the pixels in the middle change.

The plate background behind the old candy is rebuilt by sampling the plate's
own gradient from a clean strip near its edge, so the new icon sits on the
button the artist drew rather than on a flat patch.

    python3 tools/booster_skin.py [--preview]
"""

import argparse
import os
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from story_common import PROJECT_ROOT  # noqa: E402
import item_skin  # noqa: E402

TEXTURES = os.path.join(PROJECT_ROOT, "Assets", "JellyGarden", "Textures")

# path (under Textures, no extension), plate shape, inset from each edge,
# art key, how much of the inner box the art fills.
#
# "rect" plates are the rounded square buttons; "disc" are the serrated shop
# badges; "none" means the file is the loose item with no plate behind it.
PLATES = [
    ("Boosts/boost_bomb", "rect", 15, "bomb", 0.88),
    ("Boosts/boost_random_color", "rect", 15, "hammer", 0.92),
    ("menu/boosts_items/bomb_01", "none", 0, "bomb", 1.0),
    ("menu/boosts_items/bomb_02", "none", 0, "bomb", 1.0),
    ("menu/boosts_items/random_color_item", "none", 0, "orb", 1.0),
    ("menu/booster_shop/booster_bomb", "disc", 22, "bomb", 0.86),
    ("menu/booster_shop/booster_colorful_bomb", "disc", 22, "orb", 0.86),
    ("menu/booster_shop/booster_randome_color", "disc", 22, "hammer", 0.9),
    ("menu/booster_shop/booster_5_pakages", "disc", 22, "keg", 0.86),
    ("menu/booster_shop/booster_5_stripes_jelly", "disc", 22, "striped", 0.86),
    ("menu/play_menu/play_booster_01", "rect", 20, "orb", 0.9),
    ("menu/play_menu/play_booster_02", "rect", 20, "striped", 0.94),
    ("menu/play_menu/play_booster_03", "rect", 20, "keg", 0.9),
    ("Boosts/lollipop", "none", 0, "hammer", 1.0),
]


def art():
    """The icons this reskin draws with, at 512px on alpha."""
    pieces = {}
    for key in ("bomb", "hammer", "orb", "keg"):
        pieces[key] = item_skin.load(key)
    # The line-clear jewel, taken straight from the board reskin so the shop
    # icon and the piece on the board are literally the same picture.
    pieces["striped"] = item_skin.build()["item_01_stripes_horiz"]
    return pieces


def plate_background(original, shape, inset):
    """The button with the old artwork painted out, using its own gradient.

    The inner region is inpainted by diffusion: whatever plate survives around
    the hole is blurred inward, repeatedly, until it closes over. Sampling a
    row or a single flat colour instead produced streaks, because the shop
    badges have a white serrated rim that a row sample walks straight into.
    """
    width, height = original.size
    box = (inset, inset, width - inset, height - inset)

    hole = Image.new("L", (width, height), 0)
    hole_draw = ImageDraw.Draw(hole)
    if shape == "disc":
        hole_draw.ellipse(box, fill=255)
    else:
        radius = max(4, (box[2] - box[0]) // 5)
        hole_draw.rounded_rectangle(box, radius=radius, fill=255)

    plate = original.getchannel("A").point(lambda a: 255 if a > 128 else 0)
    hole = Image.composite(hole, Image.new("L", (width, height), 0), plate)

    rgb = np.asarray(original.convert("RGB"), dtype=np.float32)
    known = ((np.asarray(plate, dtype=np.float32) > 0) &
             (np.asarray(hole, dtype=np.float32) == 0)).astype(np.float32)
    known = known[:, :, None]

    colour = rgb * known
    weight = known.copy()
    for _ in range(60):
        colour = _box_blur(colour, 2)
        weight = _box_blur(weight, 2)
        colour = np.where(known > 0, rgb, colour / np.maximum(weight, 1e-5))
        weight = np.where(known > 0, 1.0, np.minimum(weight, 1.0))
        colour = colour * weight

    patched = np.clip(colour / np.maximum(weight, 1e-5), 0, 255).astype(np.uint8)

    patch = Image.fromarray(patched, "RGB").convert("RGBA")
    patch.putalpha(hole.filter(ImageFilter.GaussianBlur(1.2)))

    out = original.copy()
    out.alpha_composite(patch)
    return out


def _box_blur(array, radius):
    """Separable box blur on an HxWxC float array. Pillow cannot blur mode F."""
    size = 2 * radius + 1
    padded = np.pad(array, ((radius, radius), (radius, radius), (0, 0)), mode="edge")
    sums = np.cumsum(padded, axis=0)
    sums = np.vstack([np.zeros((1,) + sums.shape[1:], dtype=np.float32), sums])
    array = (sums[size:, :, :] - sums[:-size, :, :]) / size
    sums = np.cumsum(array, axis=1)
    sums = np.hstack([np.zeros((sums.shape[0], 1, sums.shape[2]), dtype=np.float32),
                      sums])
    return (sums[:, size:, :] - sums[:, :-size, :]) / size


def compose(path, shape, inset, icon, fill_ratio):
    original = Image.open(path).convert("RGBA")
    width, height = original.size

    if shape == "none":
        base = Image.new("RGBA", (width, height), (0, 0, 0, 0))
        box_w, box_h = width, height
    else:
        base = plate_background(original, shape, inset)
        box_w = int((width - 2 * inset) * fill_ratio)
        box_h = int((height - 2 * inset) * fill_ratio)

    art_image = icon.crop(icon.getbbox())
    art_image.thumbnail((int(box_w * (fill_ratio if shape == "none" else 1)),
                         int(box_h * (fill_ratio if shape == "none" else 1))),
                        Image.LANCZOS)

    layer = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    layer.paste(art_image,
                ((width - art_image.width) // 2, (height - art_image.height) // 2),
                art_image)
    base.alpha_composite(layer)
    return base


def build():
    pieces = art()
    out = {}
    for name, shape, inset, key, ratio in PLATES:
        path = os.path.join(TEXTURES, name + ".png")
        if not os.path.exists(path):
            raise SystemExit("ERROR: no such kit texture: %s" % path)
        out[name] = compose(path, shape, inset, pieces[key], ratio)
    return out


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--preview", action="store_true")
    args = parser.parse_args()

    sprites = build()

    if args.preview:
        names = sorted(sprites)
        cell = 180
        cols = 7
        rows = (len(names) + cols - 1) // cols
        sheet = Image.new("RGBA", (cols * cell, rows * cell), (34, 34, 40, 255))
        for index, name in enumerate(names):
            thumb = sprites[name].copy()
            thumb.thumbnail((cell - 8, cell - 8), Image.LANCZOS)
            sheet.alpha_composite(
                thumb,
                ((index % cols) * cell + (cell - thumb.width) // 2,
                 (index // cols) * cell + (cell - thumb.height) // 2))
        sheet.save("/tmp/booster_skin_preview.png")
        print("preview: /tmp/booster_skin_preview.png (%d icons)" % len(names))
        return 0

    for name, image in sorted(sprites.items()):
        path = os.path.join(TEXTURES, name + ".png")
        before = Image.open(path).size
        if image.size != before:
            raise SystemExit("ERROR: %s changed size %s -> %s"
                             % (name, before, image.size))
        image.save(path)

    print("reskinned %d booster icons in place" % len(sprites))
    return 0


if __name__ == "__main__":
    sys.exit(main())
