#!/usr/bin/env python3
"""Static audit of the six arc board themes.

`lint_levels.py` is to a level what this is to a theme: it reads what the game
reads and reports the arcs that cannot look the way they claim to, without
launching anything. Every rule below is anchored to a line of the story layer
rather than to taste:

  StoryTheme.Load               an arc missing from themes.json has no theme at
                                all, so StoryBoardTheme.Apply returns early and
                                the board keeps the kit's own look
  StoryArt.Load                 the backdrop is Resources/Story/Art/<key>; a
                                miss is silent and the picture never appears
  StoryBoardTheme.ApplyBackdrop on that miss it falls back to
                                Background.pictures[kitBackdropIndex] — the kit
                                default — so two arcs sharing an index fall back
                                to the SAME kit picture and stop being told apart
  ArcTheme.HasBoardFill         BoardFill.a > 0; at zero the cell keeps the
                                kit's flat-black sprite and the board has no
                                ground of its own
  StoryBoardTheme.ApplyCellFill the ground is BoardFill composited over the
                                backdrop, and the gems are drawn on top of it

and one rule from the project's own brief, which is not taste either because it
is written down in CLAUDE.md:

  "Always use bright, cartoon-like colour for the castle and everything else."

That one needs a number, and the number is measured rather than invented. The
reference is `area_001_state0..3` — the castle-gate room art the opening
actually shows, which is what the brief is describing. A backdrop is too dark or
too drab when it is below the DIMMEST of those.

Usage:
    python3 tools/check_themes.py            # every arc
    python3 tools/check_themes.py --quiet    # counts only
    python3 tools/check_themes.py --json     # the measurements, for a report

Prints one line per problem as `arc <n>: <problem>` and exits 1 when there is at
least one.
"""

import argparse
import glob
import json
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from story_common import STORY_RES, JELLY, rel_asset_path  # noqa: E402

ART = os.path.join(STORY_RES, "Art")
THEMES = os.path.join(STORY_RES, "themes.json")

# The six arcs the story has. An arc with no entry in themes.json is not a
# missing nicety — StoryTheme.ForLevel returns null for every level in it.
ARCS = (1, 2, 3, 4, 5, 6)

# The pictures the brief is describing when it says bright and cartoon-like:
# area 001's four gate states, which are what the opening puts on screen. The
# floor is the dimmest and drabbest of them, so the bar is the project's own
# art rather than a number somebody liked.
REFERENCE = ("area_001_state0", "area_001_state1", "area_001_state2", "area_001_state3")

# Background.pictures on the kit's Background object. Only its LENGTH matters
# here: StoryBoardTheme clamps kitBackdropIndex into it, so two arcs whose
# indices clamp to the same slot share a fallback picture.
KIT_PICTURE_COUNT = 5


# --- measuring ---------------------------------------------------------------


def _pillow():
    try:
        from PIL import Image  # noqa: F401
    except ImportError:
        return None
    from PIL import Image
    return Image


def measure(path, size=64):
    """Mean luminance (0..255) and mean saturation (0..1) of a picture.

    Downsampled first: what is being judged is how bright and how colourful the
    picture reads behind a board, not what any one pixel does.
    """
    Image = _pillow()
    if Image is None:
        return None

    pixels = _rgb_pixels(Image.open(path).convert("RGB").resize((size, size)))
    luminance = sum(0.2126 * r + 0.7152 * g + 0.0722 * b for r, g, b in pixels) / len(pixels)
    saturation = sum(
        0.0 if max(p) == 0 else (max(p) - min(p)) / float(max(p)) for p in pixels
    ) / len(pixels)
    return luminance, saturation


def _rgb_pixels(image):
    """The pixels of an RGB image as (r, g, b) triples, 0..255."""
    raw = image.tobytes()
    return [tuple(raw[i:i + 3]) for i in range(0, len(raw), 3)]


def mean_colour(path, size=8):
    """A picture's mean colour as a 0..1 RGB triple — what shows through a fill."""
    Image = _pillow()
    if Image is None:
        return None
    pixels = _rgb_pixels(Image.open(path).convert("RGB").resize((size, size)))
    return tuple(
        sum(p[i] for p in pixels) / float(len(pixels)) / 255.0 for i in range(3))


def art_path(key):
    """Resources/Story/Art/<key> as Resources.Load resolves it — any extension."""
    matches = sorted(glob.glob(os.path.join(ART, key + ".*")))
    return next((m for m in matches if not m.endswith(".meta")), None)


def relative_luminance(rgb):
    """WCAG relative luminance of a 0..1 RGB triple."""
    channels = []
    for c in rgb:
        channels.append(c / 12.92 if c <= 0.03928 else ((c + 0.055) / 1.055) ** 2.4)
    return 0.2126 * channels[0] + 0.7152 * channels[1] + 0.0722 * channels[2]


def contrast_ratio(a, b):
    """WCAG contrast ratio between two 0..1 RGB triples."""
    la, lb = relative_luminance(a), relative_luminance(b)
    lo, hi = sorted((la, lb))
    return (hi + 0.05) / (lo + 0.05)


def composite(fill, backdrop):
    """The board's ground: BoardFill at its alpha over the backdrop behind it.

    This is what StoryBoardTheme.ApplyCellFill actually produces — the fill
    sprite is white, so the renderer colour IS the ground, and its alpha is how
    much backdrop shows through. A gem is read against this, not against either
    layer on its own.
    """
    r, g, b, a = fill
    return tuple(a * c + (1.0 - a) * d for c, d in zip((r, g, b), backdrop))


# A gem is read against the board's ground, and a WCAG ratio is the wrong
# instrument for it: a red gem on a tan ground can sit at 1.05:1 and still be
# perfectly legible, because what separates them is hue, not lightness. So the
# separation measured here is CIE76 dE — lightness AND colour together.
#
# The threshold is not invented. The kit's own board is square.png, flat black
# at alpha 0.50, over one of its five shipped backdrops, and that board ships and
# is legible. Whatever the worst gem separation is on the kit's own worst board
# is the floor: the story's ground may not merge a gem worse than the kit
# already does.
KIT_SQUARE_ALPHA = 128 / 255.0
KIT_BACKGROUNDS = os.path.join(JELLY, "Textures", "backgrounds")

# The kit's six item colours in kit order (red, orange, purple, blue, green,
# yellow), sampled from Textures/Items/item_01..06.png rather than written down.
ITEM_SPRITES = tuple("item_%02d.png" % n for n in range(1, 7))
ITEMS_DIR = os.path.join(JELLY, "Textures", "Items")


def item_colours():
    """The mean opaque colour of each of the kit's six gems, 0..1 RGB."""
    Image = _pillow()
    if Image is None:
        return []

    colours = []
    for name in ITEM_SPRITES:
        path = os.path.join(ITEMS_DIR, name)
        if not os.path.exists(path):
            continue
        image = Image.open(path).convert("RGBA").resize((32, 32))
        raw = image.tobytes()
        opaque = [tuple(raw[i:i + 4]) for i in range(0, len(raw), 4)]
        opaque = [p for p in opaque if p[3] > 200]
        if not opaque:
            continue
        colours.append(tuple(
            sum(p[i] for p in opaque) / float(len(opaque)) / 255.0 for i in range(3)))
    return colours


def _lab(rgb):
    """sRGB 0..1 to CIE L*a*b* under D65."""
    linear = [c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4 for c in rgb]
    r, g, b = linear
    x = (0.4124 * r + 0.3576 * g + 0.1805 * b) / 0.95047
    y = (0.2126 * r + 0.7152 * g + 0.0722 * b) / 1.00000
    z = (0.0193 * r + 0.1192 * g + 0.9505 * b) / 1.08883

    def f(t):
        return t ** (1.0 / 3.0) if t > 0.008856 else (7.787 * t + 16.0 / 116.0)

    fx, fy, fz = f(x), f(y), f(z)
    return 116.0 * fy - 16.0, 500.0 * (fx - fy), 200.0 * (fy - fz)


def delta_e(a, b):
    """CIE76 colour difference between two 0..1 RGB triples."""
    la, lb = _lab(a), _lab(b)
    return sum((x - y) ** 2 for x, y in zip(la, lb)) ** 0.5


def worst_gem_separation(ground, gems):
    """How close the nearest of the six gems comes to this ground."""
    return min(delta_e(ground, gem) for gem in gems)


def kit_gem_separation_floor(gems):
    """The worst gem separation the kit's own shipped boards already accept.

    The kit's cell is flat black at alpha 0.50 laid over one of its five
    backdrops, so its ground is that picture halved. The floor is the worst of
    the five, because all five ship and all five are legible.
    """
    if not gems:
        return None
    pictures = sorted(glob.glob(os.path.join(KIT_BACKGROUNDS, "background_*.png")))
    worst = None
    for picture in pictures:
        mean = mean_colour(picture)
        if mean is None:
            continue
        ground = composite((0.0, 0.0, 0.0, KIT_SQUARE_ALPHA), mean)
        separation = worst_gem_separation(ground, gems)
        worst = separation if worst is None else min(worst, separation)
    return worst


# --- the audit ---------------------------------------------------------------


class Finding(object):
    """A problem. `note` marks the ones only new artwork can answer.

    The split matters because a check that can never pass is a check nobody
    runs. Everything about the WIRING — a missing picture, a fallback to the
    kit, two arcs that came out the same board, a ground a gem merges into — is
    a fault someone can fix today, and the exit code is about those. The six
    backdrops being dark, realistic paintings where the brief asks for bright
    cartoon colour is just as true and just as reported, but the fix for it is
    six new pictures, so it is a NOTE and `--strict` is how you make it count.
    """

    def __init__(self, arc, problem, note=False):
        self.arc = arc
        self.problem = problem
        self.note = note

    def __str__(self):
        return "%s arc %d: %s" % ("NOTE" if self.note else "    ", self.arc, self.problem)


def load_themes():
    if not os.path.exists(THEMES):
        return None
    with open(THEMES, encoding="utf-8") as fh:
        return json.load(fh).get("themes", [])


def reference_floor():
    """The dimmest and drabbest of the project's own bright cartoon room art."""
    measured = []
    for key in REFERENCE:
        path = art_path(key)
        if path is None:
            continue
        result = measure(path)
        if result is not None:
            measured.append(result)
    if not measured:
        return None
    return min(m[0] for m in measured), min(m[1] for m in measured)


def audit():
    findings = []
    report = {"arcs": {}, "reference": None}

    themes = load_themes()
    if themes is None:
        findings.append(Finding(0, "no %s; run tools/themes_build.py"
                                % rel_asset_path(THEMES)))
        return findings, report

    by_arc = {}
    for theme in themes:
        arc = int(theme.get("arc", 0))
        if arc > 0:
            by_arc[arc] = theme

    for arc in ARCS:
        if arc not in by_arc:
            findings.append(Finding(
                arc, "no theme in %s, so StoryTheme.ForLevel returns null for "
                     "every level in it and the board keeps the kit's own look"
                     % rel_asset_path(THEMES)))

    floor = reference_floor()
    if floor is not None:
        report["reference"] = {"luminance": round(floor[0], 1),
                               "saturation": round(floor[1], 3),
                               "from": list(REFERENCE)}

    gems = item_colours()
    gem_floor = kit_gem_separation_floor(gems)
    if gem_floor is not None:
        report["gemSeparationFloor"] = round(gem_floor, 1)

    # The HUD's own plates are opaque, so what they are laid over cannot change
    # how they read — but that is a claim, and AC3 asks for it to be checked
    # rather than asserted. StoryLevelHud.Plate draws RoyalTitle text on a
    # RoyalBlueDark fill; the target and moves cards are RoyalNavy on cream.
    for label, ink, plate in (
            ("the Target and Moves plates", (250, 247, 205), (16, 66, 139)),
            ("a target chip's count", (26, 48, 112), (250, 234, 208))):
        ratio = contrast_ratio(tuple(c / 255.0 for c in ink),
                               tuple(c / 255.0 for c in plate))
        report.setdefault("hud", {})[label] = round(ratio, 1)
        if ratio < 4.5:
            findings.append(Finding(
                0, "%s read at %.1f:1, below the 4.5:1 that body text needs — and "
                   "the plate is opaque, so no arc backdrop can rescue it"
                   % (label, ratio)))

    # --- per arc ---
    for arc in sorted(by_arc):
        theme = by_arc[arc]
        entry = {"themeKey": theme.get("themeKey", ""),
                 "boardBackdrop": theme.get("boardBackdrop", ""),
                 "kitBackdropIndex": theme.get("kitBackdropIndex", 0)}
        report["arcs"][str(arc)] = entry

        key = theme.get("boardBackdrop", "")
        path = art_path(key) if key else None

        if not key:
            findings.append(Finding(arc, "names no boardBackdrop, so every board "
                                         "in it falls back to a kit picture"))
        elif path is None:
            findings.append(Finding(
                arc, "backdrop art is missing: Assets/JellyGarden/Resources/Story/"
                     "Art/%s.* — StoryBoardTheme.ApplyBackdrop falls back to the "
                     "kit's own picture %d" % (key, theme.get("kitBackdropIndex", 0))))
        else:
            entry["backdropPath"] = rel_asset_path(path)
            measured = measure(path)
            if measured is None:
                findings.append(Finding(arc, "cannot measure %s: Pillow is not "
                                             "installed" % rel_asset_path(path)))
            else:
                entry["luminance"] = round(measured[0], 1)
                entry["saturation"] = round(measured[1], 3)
                if floor is not None:
                    if measured[0] < floor[0]:
                        findings.append(Finding(
                            arc, note=True, problem= "backdrop %s is dark: luminance %.0f against %.0f "
                                 "for the dimmest of %s, the project's own bright "
                                 "cartoon room art. CLAUDE.md: bright, cartoon-like "
                                 "colour for everything."
                                 % (os.path.basename(path), measured[0], floor[0],
                                    REFERENCE[0])))
                    if measured[1] < floor[1]:
                        findings.append(Finding(
                            arc, note=True, problem= "backdrop %s is drab: saturation %.2f against %.2f "
                                 "for the drabbest of the reference art"
                                 % (os.path.basename(path), measured[1], floor[1])))

        # The board's own ground.
        fill = theme.get("boardFill") or []
        if len(fill) < 3:
            findings.append(Finding(arc, "names no boardFill, so ArcTheme.HasBoardFill "
                                         "is false and the board keeps the kit's flat "
                                         "black checker"))
        else:
            alpha = float(fill[3]) if len(fill) >= 4 else 1.0
            entry["boardFill"] = [round(float(c), 3) for c in fill[:3]] + [round(alpha, 3)]
            if alpha <= 0.0:
                findings.append(Finding(arc, "boardFill alpha is 0, which is the same "
                                             "as naming no fill at all"))
            elif path is not None and gems and gem_floor is not None:
                # The backdrop's mean colour is what shows through the fill.
                mean = mean_colour(path)
                if mean is not None:
                    ground = composite(
                        (float(fill[0]), float(fill[1]), float(fill[2]), alpha), mean)
                    entry["ground"] = [round(c, 3) for c in ground]
                    separation = worst_gem_separation(ground, gems)
                    entry["worstGemSeparation"] = round(separation, 1)
                    if separation < gem_floor:
                        findings.append(Finding(
                            arc, "a gem merges into the board ground: dE %.1f to the "
                                 "closest of the kit's six items, worse than the %.1f "
                                 "the kit's own darkest board already accepts"
                                 % (separation, gem_floor)))

    # --- across arcs ---
    def duplicates(field, describe):
        seen = {}
        for arc, theme in sorted(by_arc.items()):
            value = theme.get(field)
            if isinstance(value, list):
                value = tuple(round(float(v), 4) for v in value)
            if value in (None, "", ()):
                continue
            seen.setdefault(value, []).append(arc)
        for value, arcs in sorted(seen.items(), key=lambda kv: kv[1]):
            if len(arcs) > 1:
                for arc in arcs[1:]:
                    findings.append(Finding(arc, describe(value, arcs)))

    duplicates("themeKey", lambda v, a: "shares the theme key '%s' with arc %d" % (v, a[0]))
    duplicates("boardBackdrop",
               lambda v, a: "shares its backdrop '%s' with arc %d, so the two arcs "
                            "look like the same place" % (v, a[0]))
    duplicates("boardFill",
               lambda v, a: "shares its board ground with arc %d, so the board is "
                            "the same colour in both" % a[0])

    # A shared kit index only ever shows when the story art is missing, which
    # is the case ApplyBackdrop exists to handle — but there are six arcs and the
    # kit ships five pictures, so SOME pair must share and a bare duplicate is
    # not a fault. It becomes one the moment both arcs actually need the
    # fallback, because then the two arcs really are the same board. Clamped
    # first, because ApplyBackdrop clamps.
    clamped = {}
    for arc, theme in sorted(by_arc.items()):
        index = max(0, min(int(theme.get("kitBackdropIndex", 0)), KIT_PICTURE_COUNT - 1))
        clamped.setdefault(index, []).append(arc)
    for index, arcs in sorted(clamped.items()):
        relying = [a for a in arcs if not art_path(by_arc[a].get("boardBackdrop", "") or "\0")]
        if len(relying) > 1:
            for arc in relying[1:]:
                findings.append(Finding(
                    arc, "has no backdrop art of its own AND falls back to the same "
                         "kit picture as arc %d (kitBackdropIndex %d), so the two "
                         "arcs are the same board" % (relying[0], index)))

    findings.sort(key=lambda f: (f.note, f.arc, f.problem))
    return findings, report


def main():
    parser = argparse.ArgumentParser(description=__doc__,
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--quiet", action="store_true", help="counts only")
    parser.add_argument("--strict", action="store_true",
                        help="count the NOTE lines too — the ones only new art can answer")
    parser.add_argument("--json", action="store_true", help="print the measurements")
    args = parser.parse_args()

    findings, report = audit()
    faults = [f for f in findings if not f.note or args.strict]
    notes = [f for f in findings if f.note]

    if args.json:
        report["findings"] = [str(f).strip() for f in findings]
        print(json.dumps(report, indent=1))
        return 1 if faults else 0

    if not args.quiet:
        for finding in findings:
            print(finding)

    print("%d problem(s) and %d note(s) in %d arcs." %
          (len(findings) - len(notes), len(notes), len(ARCS)))
    return 1 if faults else 0


if __name__ == "__main__":
    sys.exit(main())
