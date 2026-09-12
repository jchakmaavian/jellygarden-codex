#!/usr/bin/env python3
"""Install the victory card's painted art into Resources/Story/Art.

The current card uses the existing parchment plate and three new independent
sprites: a crowned victory ribbon, its curved headline, a rounded gold star
and a green capsule button. Unity supplies dynamic text and animation. The earlier sprites remain
available as fallbacks; no existing artwork is removed or overwritten by the
new masters. See tools/art/win/VICTORY.md for the generation briefs.

The masters live in tools/art/win/ and are what a repaint replaces; this copies
them in under the names StoryWinPopup asks for and writes each one's .meta.

    python3 tools/win_art.py

Nine-slicing is why several pieces carry a border. Their ends are ornament and
must not stretch; their middles are plain and must. The borders are fractions of
the master rather than pixel counts, so replacing a master with a differently
sized painting keeps the same proportion of ornament.

The plate slices on all four edges. The ribbon and the button slice only left
and right: each is drawn at one height and stretched across the panel, and
giving them a vertical border would let a caller squash a painted bevel.
"""

import os
import shutil
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from asset_import import sprite_meta  # noqa: E402
from story_common import STORY_RES, ensure_dir  # noqa: E402

TOOLS = os.path.dirname(os.path.abspath(__file__))
MASTERS = os.path.join(TOOLS, "art", "win")
DEST = os.path.join(STORY_RES, "Art")

# master file -> (key StoryArt.Load asks for, nine-slice border).
#
# A border is (left, bottom, right, top) as a FRACTION of the master's own
# width and height. None means the sprite is drawn whole — a star, a crest and
# a medallion are shapes, not frames, and slicing one would smear it.
#
# The fractions were measured on the masters themselves: the plate's gold frame
# and corner filigree end 22% in from each side and 14% down from each end; the
# ribbon's folded tails take the outer 15%; the button's rounded caps 14%.
#
# New victory sprites stay whole; the earlier plate/ribbon/button retain their
# original slicing and GUIDs for existing consumers and fallbacks.
WANTED = {
    "victory_banner.png": ("win_victory_banner", None),
    "victory_headline.png": ("win_victory_headline", None),
    "victory_button.png": ("win_victory_button", None),
    "victory_star.png": ("win_victory_star", None),
    "panel.png": ("win_panel", (0.22, 0.14, 0.22, 0.14)),
    "caption.png": ("win_caption", (0.15, 0.0, 0.15, 0.0)),
    "button.png": ("win_button", (0.14, 0.0, 0.14, 0.0)),
    "star.png": ("win_star", None),
}


def size_of(path):
    """Pixel size of a PNG, read out of its IHDR — no Pillow in this repo."""
    with open(path, "rb") as fh:
        head = fh.read(24)
    if head[:8] != b"\x89PNG\r\n\x1a\n":
        raise ValueError("not a PNG: %s" % path)
    width = int.from_bytes(head[16:20], "big")
    height = int.from_bytes(head[20:24], "big")
    return width, height


def border_for(fractions, path):
    """Fractions of the master turned into the pixel border Unity stores."""
    if fractions is None:
        return None

    width, height = size_of(path)
    left, bottom, right, top = fractions
    return (int(round(width * left)), int(round(height * bottom)),
            int(round(width * right)), int(round(height * top)))


def main():
    ensure_dir(DEST)

    problems = []
    copied = 0

    for name, (key, fractions) in sorted(WANTED.items()):
        source = os.path.join(MASTERS, name)
        if not os.path.exists(source):
            problems.append("missing master: %s" % name)
            continue

        dest = os.path.join(DEST, key + ".png")
        shutil.copyfile(source, dest)

        # An existing .meta is left alone: it holds the GUID every scene and
        # prefab already points at, and rewriting it would hand the sprite a
        # new identity on every run.
        if not os.path.exists(dest + ".meta"):
            with open(dest + ".meta", "w", encoding="utf-8") as fh:
                fh.write(sprite_meta(dest, border_for(fractions, source)))

        copied += 1

    for problem in problems:
        print("ERROR: " + problem, file=sys.stderr)

    print("win art=%d of %d" % (copied, len(WANTED)))
    return 1 if problems else 0


if __name__ == "__main__":
    sys.exit(main())
