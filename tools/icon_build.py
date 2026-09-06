#!/usr/bin/env python3
"""Put the King Leo app icon where the kit's candy one was.

The game opened on King Leo everywhere except the one place a player sees
first: the icon on their home screen was still the kit's "MATCH 3" candy
balls. StorySplash swaps the in-game wordmark at runtime; the app icon is a
build setting and cannot be swapped that way, so this overwrites the file
PlayerSettings points at.

The name `icon_192.png` is the kit's and is KEPT even though the file is now
1024 square. Unity references it by the GUID in its .meta and nothing else in
the project reads it, so the name is only a label — and renaming it would
churn the ProjectSettings reference for nothing. 1024 rather than 192 because
that is what the App Store asks for and Unity downsamples per platform; the
old file was 192 and every platform was upscaling it.

    python3 tools/icon_build.py

Master: tools/art/brand/king_leo_icon.png
Target: Assets/JellyGarden/Textures/Icon/icon_192.png
"""

import os
import sys

try:
    from PIL import Image
except ImportError:
    print("ERROR: needs Pillow — pip3 install Pillow", file=sys.stderr)
    raise SystemExit(1)

TOOLS = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.dirname(TOOLS)
MASTER = os.path.join(TOOLS, "art", "brand", "king_leo_icon.png")
TARGET = os.path.join(PROJECT, "Assets", "JellyGarden", "Textures",
                      "Icon", "icon_192.png")

SIZE = 1024


def main():
    if not os.path.exists(MASTER):
        print("ERROR: missing master %s" % MASTER, file=sys.stderr)
        return 1

    icon = Image.open(MASTER).convert("RGBA")

    # Square, and no transparency: iOS rejects an icon with an alpha channel
    # and every platform lays its own rounded mask over the top, so a corner
    # left transparent here is a corner that renders black there.
    if icon.width != icon.height:
        side = min(icon.width, icon.height)
        left = (icon.width - side) // 2
        top = (icon.height - side) // 2
        icon = icon.crop((left, top, left + side, top + side))

    flat = Image.new("RGB", icon.size, (7, 20, 64))
    flat.paste(icon, (0, 0), icon)
    flat = flat.resize((SIZE, SIZE), Image.LANCZOS)
    flat.save(TARGET)

    print("%s -> %s (%dx%d)"
          % (os.path.basename(MASTER),
             os.path.relpath(TARGET, PROJECT), SIZE, SIZE))
    return 0


if __name__ == "__main__":
    sys.exit(main())
