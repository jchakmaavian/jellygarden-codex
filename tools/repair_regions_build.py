#!/usr/bin/env python3
"""Work out WHERE in each room a repair happens, so the sparkles can sit on it.

Royal Match puts its repair effect on the thing being repaired — the carpet,
the door, the chandelier — not over the whole picture. Our rooms are painted a
state at a time rather than an object at a time, so nothing in the data says
where the change is. It is in the pixels, though: state 3 differs from state 2
exactly where repair 3 landed.

So the region is measured offline, here, and shipped as a small table. It is
done offline rather than at runtime for one reason: reading a sprite's pixels
in Unity needs `isReadable` on the importer, and that keeps an uncompressed
second copy of every room in memory. 150 areas of 1500px artwork is not a cost
worth paying on a phone to learn eleven rectangles.

Areas with a single restored picture have no diff to take — one image cannot
differ from itself — and are simply absent from the table. The runtime falls
back to a room-wide effect for those, which is the honest answer: nobody has
said where that repair happens, so it does not pretend to know.

    python3 tools/repair_regions_build.py

Output: Assets/JellyGarden/Resources/Story/repair_regions.json
"""

import json
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from story_common import STORY_RES, ensure_dir, write_text_meta  # noqa: E402

try:
    from PIL import Image, ImageChops, ImageFilter
except ImportError:
    print("ERROR: needs Pillow — pip3 install Pillow", file=sys.stderr)
    raise SystemExit(1)

ART = os.path.join(STORY_RES, "Art")
OUT = os.path.join(STORY_RES, "repair_regions.json")

# The two pictures are JPEGs of the same painting with one object changed, so
# every pixel differs a little — compression noise, and a repaint is never
# byte-identical anywhere. This is how much difference counts as a change.
# Below it is noise; the repaired object itself is far above it.
THRESHOLD = 34

# Diffing is done at this width. Full resolution finds the same rectangle and
# takes twenty times as long, and the rectangle is used to place a particle
# emitter — a pixel of precision is not the thing that matters.
WORK_WIDTH = 320

# Isolated specks survive the threshold: a JPEG block that happened to land on
# an edge. A median filter over the mask drops them without eroding the object,
# which a plain erosion would.
DESPECKLE = 3

# The rectangle is grown by this fraction of the room a side, because the
# sparkles should surround the repaired object rather than be clipped to its
# silhouette.
PAD = 0.02

# A diff that covers more of the room than this is not one object — it is a
# relit or repainted room, and a "region" for it would be the whole picture
# with extra steps. Left out of the table so the runtime falls back.
MAX_COVERAGE = 0.55

# Under this and the diff found nothing but noise.
MIN_COVERAGE = 0.0004


def load(area, state):
    for ext in (".jpg", ".png"):
        path = os.path.join(ART, "area_%03d_state%d%s" % (area, state, ext))
        if os.path.exists(path):
            return Image.open(path).convert("RGB")
    return None


def changed_rect(before, after):
    """
    The normalised rect covering what differs, or None.

    Unity's convention, so the caller does no arithmetic: origin bottom-left,
    x/y/width/height each 0..1 of the room.
    """
    if before.size != after.size:
        after = after.resize(before.size)

    height = max(1, int(round(WORK_WIDTH * before.height / before.width)))
    a = before.resize((WORK_WIDTH, height), Image.LANCZOS)
    b = after.resize((WORK_WIDTH, height), Image.LANCZOS)

    diff = ImageChops.difference(a, b).convert("L")
    mask = diff.point(lambda v: 255 if v >= THRESHOLD else 0)
    mask = mask.filter(ImageFilter.MedianFilter(DESPECKLE))

    box = mask.getbbox()
    if box is None:
        return None, 0.0

    left, top, right, bottom = box
    coverage = ((right - left) * (bottom - top)) / float(WORK_WIDTH * height)

    x = left / float(WORK_WIDTH)
    w = (right - left) / float(WORK_WIDTH)
    # PIL counts down from the top; Unity counts up from the bottom.
    y = 1.0 - bottom / float(height)
    h = (bottom - top) / float(height)

    x = max(0.0, x - PAD)
    y = max(0.0, y - PAD)
    w = min(1.0 - x, w + PAD * 2)
    h = min(1.0 - y, h + PAD * 2)

    return [round(v, 4) for v in (x, y, w, h)], coverage


def main():
    ensure_dir(os.path.dirname(OUT))

    table = {}
    skipped = []

    for area in range(1, 151):
        states = {}
        state = 1
        while True:
            before = load(area, state - 1)
            after = load(area, state)
            if before is None or after is None:
                break

            rect, coverage = changed_rect(before, after)
            if rect is None or coverage < MIN_COVERAGE:
                skipped.append("area %d state %d: no change found" % (area, state))
            elif coverage > MAX_COVERAGE:
                skipped.append("area %d state %d: %.0f%% of the room changed"
                               % (area, state, coverage * 100))
            else:
                states[str(state)] = rect
                print("area %03d state %d -> %s (%.1f%% of the room)"
                      % (area, state, rect, coverage * 100))

            state += 1

        if states:
            table[str(area)] = states

    with open(OUT, "w", encoding="utf-8") as fh:
        json.dump(table, fh, indent=1, sort_keys=True)
    write_text_meta(OUT)

    for line in skipped:
        print("skipped: %s" % line)

    regions = sum(len(v) for v in table.values())
    print("areas=%d regions=%d -> %s"
          % (len(table), regions, os.path.relpath(OUT)))
    return 0


if __name__ == "__main__":
    sys.exit(main())
