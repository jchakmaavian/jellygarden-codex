#!/usr/bin/env python3
"""Retitle the game and rebuild its world: Jelly Garden becomes King Leo.

The first thing a player saw was still a candy game — a "Jelly Garden"
wordmark over a rainbow sweetshop, a chocolate tile on the board, and a map
that wandered past lollipop beaches. This overwrites those textures in place at
their original sizes, so the scene wiring, the logo's bounce animation and the
map's scrolling all keep working.

    python3 tools/brand_skin.py [--preview]
"""

import argparse
import os
import sys

from PIL import Image, ImageDraw, ImageFilter

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from story_common import PROJECT_ROOT  # noqa: E402
import item_skin  # noqa: E402

TEXTURES = os.path.join(PROJECT_ROOT, "Assets", "JellyGarden", "Textures")
BRAND = os.path.join(os.path.dirname(os.path.abspath(__file__)), "art", "brand")
MAP = os.path.join(os.path.dirname(os.path.abspath(__file__)), "art", "map")


def cover(image, width, height):
    """Fill width x height, cropping the overflow rather than squashing."""
    scale = max(width / image.width, height / image.height)
    resized = image.resize((max(1, int(image.width * scale)),
                            max(1, int(image.height * scale))), Image.LANCZOS)
    left = (resized.width - width) // 2
    top = (resized.height - height) // 2
    return resized.crop((left, top, left + width, top + height))


def contain(image, width, height):
    """Centre the whole image inside width x height on alpha."""
    fitted = image.crop(image.getbbox()) if image.mode == "RGBA" else image.copy()
    fitted.thumbnail((width, height), Image.LANCZOS)
    canvas = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    canvas.paste(fitted, ((width - fitted.width) // 2, (height - fitted.height) // 2),
                 fitted if fitted.mode == "RGBA" else None)
    return canvas


def vignette(image, strength=0.55):
    """Darken the edges so a wordmark laid over the art stays readable."""
    width, height = image.size
    mask = Image.new("L", (width, height), 0)
    ImageDraw.Draw(mask).ellipse(
        (-width * 0.25, -height * 0.15, width * 1.25, height * 1.15), fill=255)
    mask = mask.filter(ImageFilter.GaussianBlur(min(width, height) * 0.12))
    dark = Image.new("RGB", (width, height), (16, 12, 20))
    return Image.composite(image, Image.blend(image, dark, strength), mask)


def title_screen(width, height, wordmark):
    """The castle courtyard, optionally with the King Leo wordmark over it.

    The main menu draws `logo.png` as its own object on top of the banner, so
    the banner must NOT carry the wordmark or the title appears twice, offset.
    The loading screen has no such object and does carry it.
    """
    castle = Image.open(os.path.join(BRAND, "castle.jpg")).convert("RGB")
    out = vignette(cover(castle, width, height)).convert("RGBA")

    if not wordmark:
        return out

    logo = Image.open(os.path.join(BRAND, "logo_kingleo.png")).convert("RGBA")
    plate = contain(logo, int(width * 0.82), int(height * 0.34))
    layer = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    layer.alpha_composite(plate, (0, int(height * 0.10)))
    out.alpha_composite(layer)
    return out


def build():
    logo = Image.open(os.path.join(BRAND, "logo_kingleo.png")).convert("RGBA")
    vine = item_skin.load("vine")

    images = {
        "menu/main_menu/logo": contain(logo, 508, 393),
        "menu/main_menu/main_menu_banner": title_screen(800, 1280, wordmark=False),
        "menu/main_menu/loading": title_screen(800, 1280, wordmark=True),
        # The kit's "Choco" tile is the self-spreading blocker. Ivy is what
        # actually spreads over a castle nobody has been maintaining.
        "Blocks/thriving_block": contain(vine, 82, 82),
    }

    # The five map tiles the journey scrolls through. Each was restyled from
    # the kit's own tile with the tile as the reference image, so the winding
    # road keeps its shape — the level nodes are placed against that road, and
    # a freshly invented road would leave them standing in a field.
    for index in range(1, 6):
        road = Image.open(os.path.join(MAP, "road_%02d.jpg" % index)).convert("RGB")
        images["map/map_background_%02d" % index] = \
            cover(road, 800, 1280).convert("RGBA")

    return images


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--preview", action="store_true")
    args = parser.parse_args()

    images = build()

    if args.preview:
        thumbs = []
        for name in sorted(images):
            thumb = images[name].copy()
            thumb.thumbnail((260, 260), Image.LANCZOS)
            thumbs.append(thumb)
        sheet = Image.new("RGBA", (len(thumbs) * 270, 280), (34, 34, 40, 255))
        for index, thumb in enumerate(thumbs):
            sheet.alpha_composite(thumb, (index * 270 + (270 - thumb.width) // 2,
                                          (280 - thumb.height) // 2))
        sheet.save("/tmp/brand_skin_preview.png")
        print("preview: /tmp/brand_skin_preview.png -> %s" % sorted(images))
        return 0

    for name, image in sorted(images.items()):
        path = os.path.join(TEXTURES, name + ".png")
        before = Image.open(path)
        if image.size != before.size:
            raise SystemExit("ERROR: %s changed size %s -> %s"
                             % (name, before.size, image.size))
        image.save(path)

    print("rebranded %d textures in place" % len(images))
    return 0


if __name__ == "__main__":
    sys.exit(main())
