#!/usr/bin/env python3
"""Check a board skin before it goes anywhere near the game.

The failure that matters in a match-3 is not ugliness, it is two pieces the
player cannot tell apart in the half second they have to look. That is measured
here rather than eyeballed, at the size a tile actually renders.

Six checks:

  format        every one of the 27 kit filenames present, 128x128, RGBA, with
                enough opaque pixels to be visible and a clean alpha edge
  universal     the six _stripes_horiz files are byte-identical to each other,
                and so are the six _stripes_vert and the six _extra — that is
                what "universal powerup" means on disk
  distinct      every pair of the six base pieces is far enough apart when
                shrunk to tile size; both by colour and by silhouette
  powerups      every powerup is far enough from every base piece, and from
                every other powerup — a bomb that reads like a cannon, or a ram
                that reads like a key, is the same failure one level up
  blockers      nothing on the board reads like the board's own furniture —
                the wooden solidBlock, the jellies, the ice, the stone and the
                spreading choco tile. A wooden battering ram next to a row of
                wooden timbers is exactly the trap this catches
  colour        each base piece still matches the hue its explosion particles
                are tinted with, since those are keyed to the item index
  contrast      each piece stands off the board's own dark cell texture
  fringe        no white halo left by a sloppy background cut

    python3 tools/verify_pieces.py <dir-of-27-pngs>

Exits non-zero if anything fails. Warnings do not fail the run.
"""

import os
import sys

import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

BASE = ["item_01", "item_02", "item_03", "item_04", "item_05", "item_06"]
SUFFIXES = ["", "_stripes_horiz", "_stripes_vert", "_extra"]
SINGLES = ["candy10_choco", "ingredient_01", "ingredient_02"]

# The hue each item index is expected to read as, because
# Textures/effects/explosion_item_NN is tinted to match it.
EXPECTED_HUE = {
    "item_01": (0, "red"),
    "item_02": (32, "orange"),
    "item_03": (285, "purple"),
    "item_04": (210, "blue"),
    "item_05": (130, "green"),
    "item_06": (48, "yellow"),
}

# How the eye is modelled: the piece is judged at the size a tile renders on a
# phone, not at 128px.
TILE_PX = 34
MIN_COLOUR_DISTANCE = 42.0    # mean per-pixel RGB distance over the union mask
MIN_SHAPE_DISTANCE = 0.16     # fraction of pixels where one is opaque, other not
MIN_HUE_SEPARATION = 22       # degrees
MIN_COVERAGE = 0.10           # of the 128x128 frame
MAX_COVERAGE = 0.92
MIN_BOARD_CONTRAST = 55.0     # against the kit's dark cell


def names():
    out = []
    for base in BASE:
        for suffix in SUFFIXES:
            out.append(base + suffix)
    return out + SINGLES


def load(directory, name):
    path = os.path.join(directory, name + ".png")
    if not os.path.exists(path):
        return None, path
    return Image.open(path).convert("RGBA"), path


def dominant_hue(image):
    """Hue of the piece's own pixels, weighted by saturation and opacity."""
    small = image.resize((64, 64), Image.LANCZOS)
    rgba = np.asarray(small).astype(np.float32)
    alpha = rgba[:, :, 3] / 255.0
    hsv = np.asarray(small.convert("RGB").convert("HSV")).astype(np.float32)
    hue, sat, val = hsv[:, :, 0], hsv[:, :, 1], hsv[:, :, 2]
    weight = alpha * (sat / 255.0) * (val / 255.0)
    if weight.sum() < 1e-3:
        return None
    angles = hue / 255.0 * 2 * np.pi
    x = float((np.cos(angles) * weight).sum())
    y = float((np.sin(angles) * weight).sum())
    return (np.degrees(np.arctan2(y, x)) + 360) % 360


def hue_gap(a, b):
    d = abs(a - b) % 360
    return min(d, 360 - d)


def at_tile(image):
    tile = image.resize((TILE_PX, TILE_PX), Image.LANCZOS)
    return np.asarray(tile).astype(np.float32)


def colour_distance(a, b):
    """Mean RGB distance where either piece has ink."""
    mask = np.maximum(a[:, :, 3], b[:, :, 3]) / 255.0
    if mask.sum() < 1e-3:
        return 0.0
    diff = np.sqrt(((a[:, :, :3] - b[:, :, :3]) ** 2).sum(axis=2))
    return float((diff * mask).sum() / mask.sum())


def shape_distance(a, b):
    """Fraction of the frame where exactly one of the two is opaque."""
    pa = a[:, :, 3] > 96
    pb = b[:, :, 3] > 96
    union = np.logical_or(pa, pb).sum()
    if union == 0:
        return 0.0
    return float(np.logical_xor(pa, pb).sum()) / float(union)


def white_fringe(image):
    """Fraction of edge pixels that are near-white — the mark of a bad cut."""
    rgba = np.asarray(image).astype(np.int16)
    alpha = rgba[:, :, 3]
    edge = (alpha > 40) & (alpha < 205)
    if edge.sum() == 0:
        return 0.0
    rgb = rgba[:, :, :3][edge]
    near_white = (rgb > 226).all(axis=1)
    return float(near_white.sum()) / float(edge.sum())


def main():
    if len(sys.argv) < 2:
        print(__doc__)
        return 2
    directory = sys.argv[1]

    failures = []
    warnings = []
    images = {}

    # ---- format -------------------------------------------------------
    for name in names():
        image, path = load(directory, name)
        if image is None:
            failures.append("missing: %s" % path)
            continue
        if image.size != (128, 128):
            failures.append("%s is %dx%d, expected 128x128" % (name,) + image.size)
            continue
        images[name] = image

        alpha = np.asarray(image)[:, :, 3]
        coverage = float((alpha > 96).sum()) / (128 * 128)
        if coverage < MIN_COVERAGE:
            failures.append("%s covers only %.0f%% of its frame" % (name, coverage * 100))
        elif coverage > MAX_COVERAGE:
            warnings.append("%s covers %.0f%% of its frame; it may crowd the cell"
                            % (name, coverage * 100))

        fringe = white_fringe(image)
        if fringe > 0.22:
            failures.append("%s has a white halo on %.0f%% of its edge"
                            % (name, fringe * 100))
        elif fringe > 0.10:
            warnings.append("%s: %.0f%% of its edge is near-white"
                            % (name, fringe * 100))

    if failures:
        report(failures, warnings)
        return 1

    # ---- universal powerups -------------------------------------------
    for suffix, label in ((("_stripes_horiz"), "horizontal rocket"),
                          (("_stripes_vert"), "vertical rocket"),
                          (("_extra"), "bomb")):
        blobs = set()
        for base in BASE:
            with open(os.path.join(directory, base + suffix + ".png"), "rb") as fh:
                blobs.add(fh.read())
        if len(blobs) != 1:
            failures.append("%s is not universal: %d different images across the "
                            "six colour slots" % (label, len(blobs)))

    # ---- distinctness of the six -------------------------------------
    tiles = {name: at_tile(images[name]) for name in BASE}
    worst = []
    for i in range(len(BASE)):
        for j in range(i + 1, len(BASE)):
            a, b = BASE[i], BASE[j]
            cd = colour_distance(tiles[a], tiles[b])
            sd = shape_distance(tiles[a], tiles[b])
            worst.append((cd, sd, a, b))
            if cd < MIN_COLOUR_DISTANCE and sd < MIN_SHAPE_DISTANCE:
                failures.append(
                    "%s and %s are too alike at %dpx: colour %.0f (need %.0f) "
                    "and shape %.2f (need %.2f)"
                    % (a, b, TILE_PX, cd, MIN_COLOUR_DISTANCE, sd, MIN_SHAPE_DISTANCE))
            elif cd < MIN_COLOUR_DISTANCE:
                warnings.append("%s and %s are close in colour (%.0f); they are "
                                "told apart by silhouette alone (%.2f)" % (a, b, cd, sd))

    # ---- powerups against pieces, and against each other --------------
    POWERUPS = [("item_01_stripes_horiz", "ram across"),
                ("item_01_stripes_vert", "ram up"),
                ("item_01_extra", "bomb"),
                ("candy10_choco", "colour bomb"),
                ("ingredient_01", "locket"),
                ("ingredient_02", "ring")]

    ptiles = {label: at_tile(images[name]) for name, label in POWERUPS
              if name in images}

    for label, ptile in ptiles.items():
        for base in BASE:
            cd = colour_distance(ptile, tiles[base])
            sd = shape_distance(ptile, tiles[base])
            if cd < MIN_COLOUR_DISTANCE and sd < MIN_SHAPE_DISTANCE:
                failures.append(
                    "the %s reads like %s at %dpx: colour %.0f, shape %.2f"
                    % (label, base, TILE_PX, cd, sd))

    pworst = []
    for label, ptile in ptiles.items():
        near = min((colour_distance(ptile, tiles[b]), shape_distance(ptile, tiles[b]), b)
                   for b in BASE)
        pworst.append((near[0], near[1], label, near[2]))

    plabels = list(ptiles)
    for i in range(len(plabels)):
        for j in range(i + 1, len(plabels)):
            a, b = plabels[i], plabels[j]
            cd = colour_distance(ptiles[a], ptiles[b])
            sd = shape_distance(ptiles[a], ptiles[b])
            if cd < MIN_COLOUR_DISTANCE and sd < MIN_SHAPE_DISTANCE:
                failures.append(
                    "the %s and the %s are too alike at %dpx: colour %.0f, shape %.2f"
                    % (a, b, TILE_PX, cd, sd))
            elif cd < MIN_COLOUR_DISTANCE:
                warnings.append("the %s and the %s are close in colour (%.0f); "
                                "silhouette separates them (%.2f)" % (a, b, cd, sd))

    # ---- hue matches the particle tint --------------------------------
    hues = {}
    for name in BASE:
        hue = dominant_hue(images[name])
        hues[name] = hue
        expected, label = EXPECTED_HUE[name]
        if hue is None:
            warnings.append("%s has no measurable hue; it reads as neutral" % name)
            continue
        gap = hue_gap(hue, expected)
        if gap > 55:
            failures.append("%s reads at hue %.0f°, but its explosion particles are "
                            "%s (%d°) — %.0f° apart" % (name, hue, label, expected, gap))
        elif gap > 32:
            warnings.append("%s is %.0f° off the %s its particles are tinted with"
                            % (name, gap, label))

    ordered = sorted((h, n) for n, h in hues.items() if h is not None)
    for k in range(len(ordered) - 1):
        gap = hue_gap(ordered[k][0], ordered[k + 1][0])
        if gap < MIN_HUE_SEPARATION:
            warnings.append("%s and %s sit %.0f° apart in hue"
                            % (ordered[k][1], ordered[k + 1][1], gap))

    # ---- against the board's own furniture ----------------------------
    blocks_dir = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..",
                              "Assets", "JellyGarden", "Textures", "Blocks")
    BLOCKERS = ["solidBlock", "block", "block_02", "wireBlock",
                "undestroyable", "thriving_block"]
    blockers = {}
    for name in BLOCKERS:
        path = os.path.join(blocks_dir, name + ".png")
        if os.path.exists(path):
            blockers[name] = at_tile(Image.open(path).convert("RGBA"))

    bworst = []
    if blockers:
        for label, tile_data in list(ptiles.items()) + [(n, tiles[n]) for n in BASE]:
            near = min((colour_distance(tile_data, b), shape_distance(tile_data, b), n)
                       for n, b in blockers.items())
            bworst.append((near[0], near[1], label, near[2]))
            if near[0] < MIN_COLOUR_DISTANCE and near[1] < MIN_SHAPE_DISTANCE:
                failures.append(
                    "the %s reads like the %s blocker at %dpx: colour %.0f, shape %.2f"
                    % (label, near[2], TILE_PX, near[0], near[1]))

    # ---- contrast against the board -----------------------------------
    cell_path = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..",
                             "Assets", "JellyGarden", "Textures", "Blocks",
                             "square.png")
    if os.path.exists(cell_path):
        cell = np.asarray(Image.open(cell_path).convert("RGB").resize(
            (TILE_PX, TILE_PX), Image.LANCZOS)).astype(np.float32)
        for name in BASE + SINGLES:
            tile = at_tile(images[name])
            mask = tile[:, :, 3] / 255.0
            if mask.sum() < 1e-3:
                continue
            diff = np.sqrt(((tile[:, :, :3] - cell) ** 2).sum(axis=2))
            contrast = float((diff * mask).sum() / mask.sum())
            if contrast < MIN_BOARD_CONTRAST:
                failures.append("%s only stands %.0f off the board cell (need %.0f)"
                                % (name, contrast, MIN_BOARD_CONTRAST))

    report(failures, warnings, worst, hues, pworst, bworst)
    return 1 if failures else 0


def report(failures, warnings, worst=None, hues=None, pworst=None,
           bworst=None):
    if hues:
        print("hue of each piece, against the tint of its explosion particles")
        for name in BASE:
            expected, label = EXPECTED_HUE[name]
            hue = hues.get(name)
            print("  %-8s %6s   expected %-7s %3d°   off by %s"
                  % (name,
                     ("%.0f°" % hue) if hue is not None else "n/a",
                     label, expected,
                     ("%.0f°" % hue_gap(hue, expected)) if hue is not None else "n/a"))
        print()

    if worst:
        worst.sort()
        print("closest pairs at %dpx (colour distance, silhouette difference)" % TILE_PX)
        for cd, sd, a, b in worst[:4]:
            print("  %-8s vs %-8s  colour %5.1f   shape %.2f" % (a, b, cd, sd))
        print()

    if pworst:
        pworst.sort()
        print("each powerup against its nearest of the six")
        for cd, sd, label, base in pworst:
            print("  %-12s nearest %-8s  colour %5.1f   shape %.2f"
                  % (label, base, cd, sd))
        print()

    if bworst:
        bworst.sort()
        print("closest thing on the board to each piece's own furniture")
        for cd, sd, label, blocker in bworst[:4]:
            print("  %-12s nearest %-14s colour %5.1f   shape %.2f"
                  % (label, blocker, cd, sd))
        print()

    for line in warnings:
        print("WARN  " + line)
    for line in failures:
        print("FAIL  " + line)

    print()
    if failures:
        print("%d failure(s), %d warning(s)" % (len(failures), len(warnings)))
    else:
        print("all checks passed  (%d warning(s))" % len(warnings))


if __name__ == "__main__":
    sys.exit(main())
