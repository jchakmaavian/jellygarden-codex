#!/usr/bin/env python3
"""Bake the gem-clear explosion frames from AllIn1 masks.

The kit's own explosion is a glossy candy splat: a liquid blob that reads as
something SQUISHING. A gem breaking should read as shards flying apart, which
is what a match-3 of Royal Match's grade shows. That difference in silhouette
is most of the gap, so this replaces the art rather than the machinery.

Three layers per frame, composited in this order:

    Flare27    the pop        one bright starburst, gone by frame 2
    Impact31   the shatter    radial shards, the body of the effect
    Glitter    the residue    scattered dots that outlive the shards

The sources are greyscale masks from AllIn1VfxToolkit. They are tinted here to
the gem's own colour, read from LevelManager.scoresColors in the scene, so a
blast always matches the gem that produced it.

What this does NOT change: the prefab, the controller, the six clips, or any
code. It overwrites 24 PNGs in place, at their existing filenames, so every
sprite guid survives and check_vfx_intact.py stays green. `git checkout` on the
texture folders undoes the whole thing.

    python3 tools/explosion_bake.py            # write the frames
    python3 tools/explosion_bake.py --preview  # contact sheet, write nothing

Baked frames cannot blend additively — that needs a material, not a PNG. This
buys the silhouette, not the glow.
"""

import argparse
import os
import sys

from PIL import Image, ImageChops, ImageEnhance

HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT_ROOT = os.path.dirname(HERE)

TEX = os.path.join(PROJECT_ROOT, "Assets", "JellyGarden", "Textures", "effects")
SHAPES = os.path.join(
    PROJECT_ROOT, "Assets", "Plugins", "AllIn1VfxToolkit",
    "Demo & Assets", "Textures", "Shapes",
)

SIZE = 163  # what the kit's frames already are

# LevelManager.scoresColors, straight out of Scenes/game.unity. The `color` int
# indexes this list AND selects the clip, so the tint here is the same colour
# the score popup uses for the same gem.
# Gold and yellow are pushed apart from the scene's own values. scoresColors
# has them at (255,193,22) and (255,255,38), which are a hair apart once a
# blast is 60px on a phone — the two blasts read as the same effect. The kit's
# own art told them apart, so this does too: gold goes warmer and deeper,
# yellow goes paler and greener. Everything else is the scene value verbatim.
GEM_COLOURS = [
    (251, 80, 1),      # 0 red-orange   scene value
    (255, 164, 8),     # 1 gold         warmed from (255,193,22)
    (237, 13, 233),    # 2 magenta      scene value
    (41, 157, 255),    # 3 blue         scene value
    (247, 255, 96),    # 4 yellow       paled from (255,255,38)
    (37, 219, 0),      # 5 green        scene value
]

# color int -> the folder its clip actually reads. 4 and 5 are crossed:
# item_anim5 plays folder 06 and item_anim6 plays folder 05. Following the
# folder numbers instead of the clips would tint every green blast yellow.
COLOUR_TO_FOLDER = {0: "01", 1: "02", 2: "03", 3: "04", 4: "06", 5: "05"}

FOLDER_FILES = {
    "01": ["explosion-of-purple-candy-1.png", "explosion-of-purple-candy-2.png",
           "explosion-of-purple-candy-3.png", "explosion-of-red-candy-4.png"],
    "02": ["explosion-of-orange-candy-%d.png" % i for i in range(1, 5)],
    "03": ["explosion-of-purple-candy-%d.png" % i for i in range(1, 5)],
    "04": ["explosion-of-blue-candy-%d.png" % i for i in range(1, 5)],
    "05": ["explosion-of-green-candy-%d.png" % i for i in range(1, 5)],
    "06": ["explosion-of-yellow-candy-%d.png" % i for i in range(1, 5)],
}

# Per frame, per layer: (scale, alpha, erode).
#
# The clip runs 4 frames at 15fps and then holds emty_sprite, so this is 267ms
# of art. The shape of the curve is the point: the flare is over almost before
# it is seen, the shards peak on frame 2 and keep expanding after, and the
# glitter is still there on frame 4 so the effect trails off instead of
# stopping dead.
#
# The shards fade by ERODING, not by dropping alpha. Alpha alone turned frames
# 3 and 4 into a grey smear, because the soft skirt of the mask outlives its
# bright core. Eroding eats the skirt first, so what is left stays bright and
# simply gets sparser — debris breaking up rather than a blob dimming.
# Per frame, per layer: (scale, alpha, erode).
#
# The curve has to DESCEND after frame 2. An earlier table grew the shard every
# frame and ended more solid than it started — 13, 41, 47, 49 percent of the
# tile — so the blast bloomed into a fat flower and then cut to nothing. The
# kit's own frames go 9, 49, 22, 9: a burst that dies. These follow that shape.
#
# StarDiamond is a filled mask, so coverage is cut by ERODING it — which eats
# the petals inward from their edges and thins the star into spokes — as well
# as by alpha. Scale keeps growing throughout, because debris travels outward
# even as it fades.
# How the kit's own frames die, and how these have to.
#
# The kit goes 9, 49, 22, 9 percent of the tile ABOVE ALPHA 200. That last part
# is the whole trick: its late frames stay opaque and simply cover less area.
# It never dims a frame uniformly.
#
# Two failed attempts here did exactly that, in two different ways:
#
#   erode 3.2 / 7.5 — a filled mask has a flat interior, so a high exponent
#                     drops the entire petal at once rather than thinning it.
#                     Frames 3 and 4 fell to 0.4% and 0.0%.
#   alpha .62 / .30 — a uniform multiply caps every pixel at 158 and 76, both
#                     under the 200 the measurement (and the eye, on a light
#                     board) calls solid. Also 0%.
#
# So the late frames keep alpha high and shrink instead: the shard's SCALE
# comes back down after its peak, which is the same silhouette getting smaller,
# and only a mild erode breaks the star's points up as it goes.
FRAMES = [
    # flare              shard               glitter
    ((0.70, 1.00, 1.0), (0.45, 0.90, 1.0), (0.34, 0.45, 1.0)),
    ((1.00, 0.60, 1.2), (0.88, 1.00, 1.0), (0.68, 0.85, 1.0)),
    ((0.55, 0.25, 1.5), (0.62, 0.95, 1.25), (0.98, 0.80, 1.2)),
    ((0.00, 0.00, 1.0), (0.38, 0.90, 1.45), (1.15, 0.60, 1.6)),
]

# How much of the tile the kit's own frames cover above alpha 200, measured
# from the originals in git: 9.1%, 49.3%, 22.3%, 9.2%. A baked frame that
# covers far less than its counterpart is invisible on the story's light
# board however good the silhouette is, so the bake is checked against these
# rather than judged on a black contact sheet.
KIT_COVERAGE = [9.1, 49.3, 22.3, 9.2]


def load_mask(name):
    """One AllIn1 mask as a single-channel image, white = opaque."""
    path = os.path.join(SHAPES, name)
    if not os.path.exists(path):
        sys.exit("missing source texture: %s" % path)
    img = Image.open(path).convert("RGBA")
    # These masks carry their shape in RGB on black, and several have a flat
    # opaque alpha, so luminance is the honest source of coverage.
    return img.convert("L")


def tinted(mask, colour, scale, alpha, boost=1.0, erode=1.0,
           white_at=0.97, solidify=1.0):
    """A mask, tinted and scaled onto a SIZE square, ready to composite.

    `erode` is how a layer fades. Dropping alpha on a soft-edged mask over a
    dark board does not read as a shard dimming, it reads as grey sludge — the
    soft skirt of the mask survives as a smear long after the bright core has
    gone. Raising the mask to a power instead eats the skirt first and leaves
    the core bright, so the shards thin out and break up the way real debris
    does. Values above 1 erode; 1.0 leaves the mask alone.
    """
    out = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    if scale <= 0 or alpha <= 0:
        return out

    side = max(1, int(round(SIZE * scale)))
    m = mask.resize((side, side), Image.LANCZOS)
    if erode != 1.0:
        m = m.point(lambda v: int((v / 255.0) ** erode * 255))
    if boost != 1.0:
        m = ImageEnhance.Brightness(m).enhance(boost)

    # The body of the shape keeps the gem's own colour; only a thin rim of the
    # very hottest pixels goes white, as a glint.
    #
    # Two earlier attempts got this wrong in opposite directions. Mixing toward
    # white by the mask's intensity (exponent 2.6) washed everything pale, and
    # on the story's LIGHT board a pale shard is invisible. Raising the exponent
    # to 7 fixed the thin Impact mask but, on a FILLED mask like StarDiamond,
    # the interior is at full mask value everywhere, so the whole petal crossed
    # the threshold and all six gem colours came out as the same white flower.
    #
    # `white_at` is therefore a fraction of the mask's own peak rather than an
    # absolute curve: only the top slice glints, whatever the mask's fill.
    flat = Image.new("RGB", (side, side), colour)
    if white_at >= 1.0:
        # No glint: the mask keeps the gem's colour everywhere it covers.
        #
        # This is what a hard-edged mask needs. StarDiamond is effectively
        # binary — 43.3% of its tile sits at luminance 250 or above and only a
        # thin antialiased rim lies below, so a "top slice" threshold has no
        # slice to find and paints the entire petal white. Every gem colour then
        # came out as the same white flower with a coloured outline. The flare
        # layer carries the brightness instead, which is where a bright core
        # belongs anyway.
        rgb = flat
    else:
        white = Image.new("RGB", (side, side), (255, 255, 255))
        peak = max(1, m.getextrema()[1])
        cut = int(peak * white_at)
        core = m.point(lambda v: 255 if v >= cut else 0)
        rgb = Image.composite(white, flat, core)

    # Alpha is lifted toward opaque, because a baked frame cannot blend
    # additively the way the mask was authored for. `solidify` below 1 lifts;
    # 1.0 uses the mask as-is. A filled mask needs no lift and a soft one does.
    a = m.point(lambda v: int(min(255, (v / 255.0) ** solidify * 255) * alpha))
    layer = Image.merge("RGBA", (*rgb.split(), a))

    off = (SIZE - side) // 2
    out.paste(layer, (off, off))
    return out


def over(base, layer):
    return Image.alpha_composite(base, layer)


def build(colour_index):
    """The four frames for one gem colour."""
    colour = GEM_COLOURS[colour_index]
    flare = load_mask("Flare27.png")
    # Impact31 was the first choice and was wrong. It is thin radial filigree:
    # measured against the bake's own curve it covers 11.7% of the tile where
    # the kit's peak frame covers 49.3%, so it read as nothing on the story's
    # light board however it was tinted. Silhouettes were being judged on a
    # black contact sheet, where bright filigree looks strong.
    #
    # StarDiamond is a FILLED eight-point burst at 46.6% — within a few points
    # of the kit's own mass, and a jewel burst rather than a candy splat, which
    # is the change that was wanted. Every mask in the library was measured
    # before this was picked; see KIT_COVERAGE and report_coverage().
    shard = load_mask("StarDiamond.png")
    grit = load_mask("Glitter.png")

    frames = []
    for (fs, fa, fe), (ss, sa, se), (gs, ga, ge) in FRAMES:
        img = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))

        # Glitter and flare are soft glows authored for additive blending, so
        # they are lifted toward opaque to survive being baked. The shard is a
        # filled mask and needs no lift — lifting it was what produced a solid
        # white flower with no gem colour left in it.
        img = over(img, tinted(grit, colour, gs, ga, boost=1.15, erode=ge,
                               solidify=0.55, white_at=0.90))
        img = over(img, tinted(shard, colour, ss, sa, erode=se,
                               solidify=1.0, white_at=1.0))
        img = over(img, tinted(flare, colour, fs, fa, boost=1.25, erode=fe,
                               solidify=0.5, white_at=0.85))
        frames.append(img)
    return frames


def write_frames():
    written = 0
    for colour_index, folder in sorted(COLOUR_TO_FOLDER.items()):
        frames = build(colour_index)
        names = FOLDER_FILES[folder]
        target = os.path.join(TEX, "explosion_item_%s" % folder)
        for img, name in zip(frames, names):
            path = os.path.join(target, name)
            if not os.path.exists(path):
                sys.exit("expected an existing frame at %s" % path)
            img.save(path)
            written += 1
    return written


def coverage(img):
    """Percent of the tile at alpha above 200 — what the eye reads as solid."""
    alpha = img.split()[3]
    hist = alpha.histogram()
    solid = sum(hist[201:])
    return 100.0 * solid / float(img.width * img.height)


def report_coverage():
    """Measure every frame against the kit's own, and say which fall short.

    This exists because the first bake was judged on a black contact sheet,
    where thin bright filigree looks punchy, and shipped frames covering 1.9%
    of the tile where the kit covered 49%. On the story's light board they were
    invisible, and the game read as having no effect at all. Numbers catch that;
    a contact sheet did not.
    """
    print("frame  kit%%   bake%%  ratio")
    worst = []
    for index, frames in enumerate(build(i) for i in range(len(GEM_COLOURS))):
        for f, img in enumerate(frames):
            got = coverage(img)
            want = KIT_COVERAGE[f]
            ratio = got / want if want else 0.0
            if index == 0:
                print("  %d   %5.1f  %5.1f  %4.2fx%s"
                      % (f + 1, want, got, ratio, "" if ratio >= 0.5 else "   <-- THIN"))
            if ratio < 0.5:
                worst.append((index, f + 1, got, want))
    if worst:
        print("\n%d frame(s) under half the kit's coverage — these read as "
              "nothing on a lit board." % len(worst))
    else:
        print("\nevery frame is at least half the kit's coverage")
    return worst


def write_preview(path):
    """One contact sheet: a row per gem colour, a column per frame."""
    pad = 8
    cols, rows = 4, len(COLOUR_TO_FOLDER)
    w = cols * SIZE + (cols + 1) * pad
    h = rows * SIZE + (rows + 1) * pad
    sheet = Image.new("RGBA", (w, h), (14, 12, 10, 255))
    for row, colour_index in enumerate(sorted(COLOUR_TO_FOLDER)):
        for col, img in enumerate(build(colour_index)):
            x = pad + col * (SIZE + pad)
            y = pad + row * (SIZE + pad)
            sheet.alpha_composite(img, (x, y))
    sheet.save(path)
    return path


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--preview", metavar="PATH", nargs="?", const="explosion_preview.png",
                    help="write a contact sheet instead of the frames")
    args = ap.parse_args()

    if args.preview:
        report_coverage()
        print("\npreview: %s" % write_preview(args.preview))
        return

    n = write_frames()
    print("wrote %d frames over 6 gem colours" % n)
    print("undo with: git checkout Assets/JellyGarden/Textures/effects/")


if __name__ == "__main__":
    main()
