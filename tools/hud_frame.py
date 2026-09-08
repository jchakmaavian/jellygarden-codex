#!/usr/bin/env python3
"""Paint the level HUD's single plate from its parts.

StoryLevelHud draws ONE painted plate — `Resources/Story/UI/hud_frame` — and
places live text over it against measured constants. That plate is composed
here from `tools/art/hud/`, so re-cutting a part is a re-run rather than a
hand-edit in an image editor.

Everything the plate paints never changes at runtime: the wood bar, both blue
panels, their gold rules, the sunburst and the crowned medallion ring. The
avatar, the labels and every number are drawn over it by the HUD.

The geometry below IS the contract StoryLevelHud's constants are measured
against. Change a number here and the matching constant in StoryLevelHud.cs
goes stale — the block comment beside them names each one.

    python3 tools/hud_frame.py
"""

import os
import sys

from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
SRC = os.path.join(HERE, "art", "hud")
OUT = os.path.join(HERE, os.pardir, "Assets", "JellyGarden", "Resources",
                   "Story", "UI", "hud_frame.png")

# The plate. Authored at 1536 wide to match every other sprite in
# Resources/Story/UI; its height IS StoryLevelHud.StripHeight once scaled to
# the 1080-unit design width, so shortening it here shortens the HUD.
W, H = 1536, 520

# The wood bar: full width, inset top and bottom so the crown clears it.
BAR = (0, 70, W, 500)

# The two blue panels, mirrored about the centre line.
PANEL_L = (77, 100, 522, 470)

# Where the gold rule crosses a panel, as a fraction of the panel's height.
# StoryLevelHud.TitleFrac must equal this: the label sits above the rule.
RULE_FRAC = 0.30

# The medallion, centred, tall enough that the crown rises over the bar.
MED_W = 400
MED_TOP = 6

# The hole in the ring is not a number here: `hole_of` reads it off the part's
# own alpha, so re-cutting the ring moves the sunburst and the avatar with it.

GOLD = (214, 168, 62)


def nine(im, w, h, fx, fy, dbx, dby):
    """Nine-slice `im` to w x h. fx/fy border as a fraction of the SOURCE,
    dbx/dby the border in DESTINATION pixels."""
    sw, sh = im.size
    bx, by = int(sw * fx), int(sh * fy)
    dbx, dby = min(dbx, w // 2 - 1), min(dby, h // 2 - 1)
    out = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    cols = [(0, bx), (bx, sw - bx), (sw - bx, sw)]
    rows = [(0, by), (by, sh - by), (sh - by, sh)]
    dcols = [(0, dbx), (dbx, w - dbx), (w - dbx, w)]
    drows = [(0, dby), (dby, h - dby), (h - dby, h)]
    for r, (y0, y1) in enumerate(rows):
        for c, (x0, x1) in enumerate(cols):
            dx0, dx1 = dcols[c]
            dy0, dy1 = drows[r]
            tile = im.crop((x0, y0, x1, y1)).resize(
                (max(1, dx1 - dx0), max(1, dy1 - dy0)), Image.LANCZOS)
            out.alpha_composite(tile, (dx0, dy0))
    return out


def hole_of(ring):
    """The opening in the ring, as a box in the ring's own pixels.

    The cut-out ring is transparent through its middle, so the hole is the
    transparent region the ring's centre falls in — flood-filled from there,
    which stops at the gold rather than at the picture's edge.
    """
    alpha = ring.split()[3].point(lambda v: 0 if v > 40 else 255)
    probe = Image.merge("RGB", (alpha, alpha, alpha))
    ImageDraw.floodfill(probe, (ring.width // 2, int(ring.height * 0.62)),
                        (255, 0, 0), thresh=10)
    inside = Image.new("L", ring.size, 0)
    src, dst = probe.load(), inside.load()
    for y in range(ring.height):
        for x in range(ring.width):
            if src[x, y] == (255, 0, 0):
                dst[x, y] = 255
    box = inside.getbbox()
    if box is None:
        sys.exit("medallion.png has no hole — is its centre still painted in?")
    return box


def load(name):
    path = os.path.join(SRC, name + ".png")
    if not os.path.exists(path):
        sys.exit("missing part: " + path)
    return Image.open(path).convert("RGBA")


def rule(draw, x0, x1, y):
    """A gold hairline that fades out at both ends, the way the art paints it."""
    span = x1 - x0
    for x in range(x0, x1):
        t = (x - x0) / float(span)
        a = int(210 * min(1.0, min(t, 1.0 - t) * 6.0))
        draw.line([(x, y), (x, y + 2)], fill=GOLD + (a,))


def main():
    plate = Image.new("RGBA", (W, H), (0, 0, 0, 0))

    bar = load("frame")
    bx0, by0, bx1, by1 = BAR
    plate.alpha_composite(nine(bar, bx1 - bx0, by1 - by0, 0.11, 0.20, 163, 96),
                          (bx0, by0))

    panel = load("panel")
    px0, py0, px1, py1 = PANEL_L
    pw, ph = px1 - px0, py1 - py0
    tile = nine(panel, pw, ph, 0.11, 0.13, 60, 55)
    plate.alpha_composite(tile, (px0, py0))
    plate.alpha_composite(tile.transpose(Image.FLIP_LEFT_RIGHT), (W - px1, py0))

    d = ImageDraw.Draw(plate)
    ry = py0 + int(ph * RULE_FRAC)
    rule(d, px0 + 34, px1 - 34, ry)
    rule(d, W - px1 + 34, W - px0 - 34, ry)

    ring = load("medallion")
    mh = int(ring.height * MED_W / float(ring.width))
    ring = ring.resize((MED_W, mh), Image.LANCZOS)
    mx = (W - MED_W) // 2

    # The sunburst fills the opening exactly and is clipped to it, so no ray
    # crosses the gold. The opening is an oval, so the clip is one too.
    hx0, hy0, hx1, hy1 = hole_of(ring)
    hw, hh = hx1 - hx0, hy1 - hy0
    sun = load("rays").resize((hw, hh), Image.LANCZOS)
    clip = Image.new("L", (hw, hh), 0)
    ImageDraw.Draw(clip).ellipse((0, 0, hw - 1, hh - 1), fill=255)
    sun.putalpha(clip)
    plate.alpha_composite(sun, (mx + hx0, MED_TOP + hy0))
    plate.alpha_composite(ring, (mx, MED_TOP))

    plate.save(os.path.normpath(OUT), optimize=True)

    # The numbers StoryLevelHud.cs has to agree with, printed so a re-run that
    # moves something says so out loud.
    design_h = 1080.0 * H / W
    sy = design_h / H
    print("wrote", os.path.normpath(OUT), plate.size)
    print("  StripHeight    %.0f" % design_h)
    print("  PanelLeftMin   %.4f" % (px0 / float(W)))
    print("  PanelLeftMax   %.4f" % (px1 / float(W)))
    print("  PanelTop       %.0f" % (py0 * sy))
    print("  PanelHeight    %.0f" % (ph * sy))
    print("  TitleFrac      %.4f" % RULE_FRAC)
    print("  WindowMin      %.4f" % ((mx + hx0) / float(W)))
    print("  WindowMax      %.4f" % ((mx + hx1) / float(W)))
    print("  WindowTop      %.0f" % ((MED_TOP + hy0) * sy))
    print("  WindowHeight   %.0f" % (hh * sy))


if __name__ == "__main__":
    main()
