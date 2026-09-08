#!/usr/bin/env python3
"""Reskin the in-level booster bar from Jelly Garden candy to King Leo's court.

The bar under the board is the last kit surface a player reads on every level:
five tiles — extra moves, the hand, pause, the lollipop and the candy bomb —
plus the two little badges that sit on their corners. `booster_skin.py` only
ever replaced the artwork *inside* two of those plates; the plate itself, the
hand, the arrows, the badges and the pause button were still the kit's.

This rebuilds each of those files whole: a royal plate with the icon composed
on top, at the kit's own filename, pixel size and GUID, so every prefab,
animation clip and Button reference keeps working untouched. Nothing in the
scene or in a script changes — only the pixels.

Masters live in tools/art/bar (this reskin's own) and tools/art/pieces (shared
with the board reskin, so the bomb on the bar is the bomb on the board).

    python3 tools/bar_skin.py --preview        # contact sheet in /tmp
    python3 tools/bar_skin.py --stage <dir>    # write nothing to the game
    python3 tools/bar_skin.py                  # apply to the project
"""

import argparse
import os
import sys

from PIL import Image, ImageDraw, ImageFilter, ImageFont

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from story_common import PROJECT_ROOT  # noqa: E402

ART = os.path.join(os.path.dirname(os.path.abspath(__file__)), "art")
BAR = os.path.join(ART, "bar")
PIECES = os.path.join(ART, "pieces")
TEXTURES = os.path.join(PROJECT_ROOT, "Assets", "JellyGarden", "Textures")
FONT = os.path.join(PROJECT_ROOT, "Assets", "JellyGarden", "Resources", "Story",
                    "Fonts", "story_display.ttf")

# The number painted on a tile is part of the artwork in this kit — the moves
# booster says "+5" and the timer says "+30" in the texture, not in a Text.
# Both are the kit's own amounts (BoostIcon: 5 moves, 30 seconds), so they are
# repainted rather than reworded.
GOLD = (255, 214, 92)
GOLD_DEEP = (226, 152, 30)
INK = (74, 38, 12)

# kit file (under Textures, no extension), master, how much of the tile the
# icon fills, where its centre sits as a fraction of the tile, and the number
# painted on it as (text, centre).
#
# The bar renders at 69px on a 393pt screen, so an icon that is merely present
# at 125px can still be unreadable in the hand. The fills below were set by
# looking at that size, not at the master.
TILES = [
    ("Boosts/boost_5_mooves", ("pieces", "rocket"), 0.74, (0.50, 0.62), ("+5", (0.50, 0.30))),
    ("Boosts/boost_hand", ("bar", "gauntlet"), 0.74, (0.50, 0.50), None),
    ("Boosts/boost_bomb", ("pieces", "bomb"), 0.74, (0.50, 0.52), None),
    ("Boosts/boost_random_color", ("pieces", "hammer"), 0.76, (0.50, 0.50), None),
    # The hourglass is tall, so its amount sits beside it rather than over it —
    # which is what the kit's own +30 tile does with the same shape.
    ("Boosts/boost_5_time", ("bar", "hourglass"), 0.80, (0.38, 0.50), ("+30", (0.60, 0.58))),
]

# Files that are one master, edge to edge: the locked plate, the two corner
# badges and the pause button.
WHOLE = [
    ("Boosts/boost_over_button", ("bar", "locked"), 1.0),
    ("Boosts/boost_nomber", ("bar", "count_badge"), 1.0),
    ("Boosts/boost_plus_button", ("bar", "plus_badge"), 1.0),
    ("menu/game_menu/pause_button", ("bar", "pause"), 1.0),
]


def master(where, name):
    root = BAR if where == "bar" else PIECES
    image = Image.open(os.path.join(root, name + ".png")).convert("RGBA")
    return image.crop(image.getbbox())


def fitted(image, box_w, box_h):
    out = image.copy()
    out.thumbnail((max(1, box_w), max(1, box_h)), Image.LANCZOS)
    return out


def number(canvas, text, centre, width, height):
    """The kit's painted amount, in the story's display face and gold."""
    size = int(height * 0.42)
    try:
        font = ImageFont.truetype(FONT, size)
    except (OSError, IOError):
        raise SystemExit("ERROR: cannot open %s" % FONT)

    draw = ImageDraw.Draw(canvas)
    left, top, right, bottom = draw.textbbox((0, 0), text, font=font,
                                             stroke_width=int(size * 0.14))
    x = int(width * centre[0]) - (right - left) // 2 - left
    y = int(height * centre[1]) - (bottom - top) // 2 - top

    # Drawn twice: the deep gold underneath is what gives the flat fill an
    # edge at 40px, which is the size this tile actually renders at.
    draw.text((x, y + max(1, size // 14)), text, font=font, fill=GOLD_DEEP,
              stroke_width=int(size * 0.14), stroke_fill=INK)
    draw.text((x, y), text, font=font, fill=GOLD,
              stroke_width=int(size * 0.10), stroke_fill=INK)
    return canvas


def halo(size, alpha=110):
    """A soft warm disc behind the icon.

    The plate is deep blue and two of these icons — the black bomb, the dark
    hammer haft — are darker than it. Without this they read as a smudge at the
    69px the bar actually renders at; the kit solved the same problem by
    painting its candy in light pastels.
    """
    width, height = size
    disc = Image.new("L", (width, height), 0)
    ImageDraw.Draw(disc).ellipse(
        (width * 0.12, height * 0.12, width * 0.88, height * 0.88), fill=alpha)
    glow = Image.new("RGBA", (width, height), (255, 244, 214, 255))
    glow.putalpha(disc.filter(ImageFilter.GaussianBlur(width * 0.10)))
    return glow


def tile(size, icon, fill, centre, label):
    width, height = size
    plate = master("bar", "plate").resize((width, height), Image.LANCZOS)
    plate.alpha_composite(halo(size))

    art = fitted(icon, int(width * fill), int(height * fill))
    layer = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    layer.paste(art, (int(width * centre[0]) - art.width // 2,
                      int(height * centre[1]) - art.height // 2), art)
    plate.alpha_composite(layer)

    if label:
        plate = number(plate, label[0], label[1], width, height)
    return plate


def whole(size, icon, fill):
    width, height = size
    art = fitted(icon, int(width * fill), int(height * fill))
    canvas = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    canvas.paste(art, ((width - art.width) // 2, (height - art.height) // 2), art)
    return canvas


def kit_size(name):
    path = os.path.join(TEXTURES, name + ".png")
    if not os.path.exists(path):
        raise SystemExit("ERROR: no such kit texture: %s" % path)
    return Image.open(path).size


def build():
    out = {}
    for name, (where, art), fill, centre, label in TILES:
        out[name] = tile(kit_size(name), master(where, art), fill, centre, label)
    for name, (where, art), fill in WHOLE:
        out[name] = whole(kit_size(name), master(where, art), fill)
    return out


def preview(sprites, path):
    names = sorted(sprites)
    cell = 170
    sheet = Image.new("RGBA", (cell * len(names), cell), (34, 34, 40, 255))
    for index, name in enumerate(names):
        thumb = sprites[name].copy()
        thumb.thumbnail((cell - 20, cell - 20), Image.LANCZOS)
        sheet.alpha_composite(thumb, (index * cell + (cell - thumb.width) // 2,
                                      (cell - thumb.height) // 2))
    sheet.save(path)
    return path


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--preview", action="store_true")
    parser.add_argument("--stage")
    args = parser.parse_args()

    sprites = build()

    if args.preview:
        print("preview: %s (%d sprites)"
              % (preview(sprites, "/tmp/bar_skin_preview.png"), len(sprites)))
        return 0

    root = args.stage if args.stage else TEXTURES
    for name, image in sorted(sprites.items()):
        before = kit_size(name)
        if image.size != before:
            raise SystemExit("ERROR: %s changed size %s -> %s"
                             % (name, before, image.size))
        path = os.path.join(root, name + ".png")
        directory = os.path.dirname(path)
        if not os.path.isdir(directory):
            os.makedirs(directory)
        image.save(path)

    print("reskinned %d booster-bar sprites in %s" % (len(sprites), root))
    return 0


if __name__ == "__main__":
    sys.exit(main())
