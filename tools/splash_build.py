#!/usr/bin/env python3
"""Import the King Leo brand art the splash screen wears.

The kit opens on its own Jelly Garden splash — `Textures/menu/main_menu/logo.png`
over `main_menu_banner.png`. Neither is touched: StorySplash swaps the sprites at
runtime, the same way StoryBoardTheme swaps the board's backdrop, so the kit's
own art is still there and turning the feature off gives it straight back.

This puts the master into Resources/Story/Art under the name StorySplash looks
for. The master lives in tools/art/brand/ with the rest of the brand art.

    python3 tools/splash_build.py
"""

import os
import shutil
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from asset_import import sprite_meta  # noqa: E402
from story_common import STORY_RES, ensure_dir, guid_for, rel_asset_path  # noqa: E402

TOOLS = os.path.dirname(os.path.abspath(__file__))
MASTERS = os.path.join(TOOLS, "art", "brand")
DEST_ART = os.path.join(STORY_RES, "Art")

# master file -> the key StoryArt.Load resolves
BRAND = {
    "king_leo_logo.png": "splash_logo",
    "king_leo_splash.jpg": "splash_background",
}

# The kit ships its display font in Assets/JellyGarden/Fonts, which a build only
# carries if something references it — and StoryUi finds its font by looking at
# whichever Text happens to be in the scene, which on the splash is one wearing
# Unity's default. A copy under Resources is what makes it loadable by name.
DEST_FONTS = os.path.join(STORY_RES, "Fonts")
KIT_FONTS = os.path.join(
    os.path.dirname(TOOLS), "Assets", "JellyGarden", "Fonts")
FONTS = {
    "BorisBlackBloxx.ttf": "story_display",
}


def font_meta(dest, face):
    """TrueType import settings, matching how the kit imports its own faces."""
    return """fileFormatVersion: 2
guid: %s
TrueTypeFontImporter:
  externalObjects: {}
  serializedVersion: 4
  fontSize: 16
  forceTextureCase: -2
  characterSpacing: 0
  characterPadding: 1
  includeFontData: 1
  fontNames:
  - %s
  fallbackFontReferences: []
  customCharacters:\x20
  fontRenderingMode: 0
  ascentCalculationMode: 1
  useLegacyBoundsCalculation: 0
  shouldRoundAdvanceValue: 1
  userData:\x20
  assetBundleName:\x20
  assetBundleVariant:\x20
""" % (guid_for(rel_asset_path(dest)), face)


def main():
    ensure_dir(DEST_ART)

    problems = []
    count = 0

    for source_name, stem in sorted(BRAND.items()):
        source = os.path.join(MASTERS, source_name)
        if not os.path.exists(source):
            problems.append("missing master: %s" % source_name)
            continue

        dest = os.path.join(DEST_ART, stem + os.path.splitext(source_name)[1])
        shutil.copyfile(source, dest)
        with open(dest + ".meta", "w", encoding="utf-8") as fh:
            fh.write(sprite_meta(dest))
        count += 1
        print("%s -> %s" % (source_name, os.path.basename(dest)))

    ensure_dir(DEST_FONTS)
    for source_name, stem in sorted(FONTS.items()):
        source = os.path.join(KIT_FONTS, source_name)
        if not os.path.exists(source):
            problems.append("missing font: %s" % source_name)
            continue

        dest = os.path.join(DEST_FONTS, stem + ".ttf")
        shutil.copyfile(source, dest)
        face = os.path.splitext(source_name)[0]
        with open(dest + ".meta", "w", encoding="utf-8") as fh:
            fh.write(font_meta(dest, face))
        count += 1
        print("%s -> Fonts/%s.ttf" % (source_name, stem))

    for problem in problems:
        print("ERROR: %s" % problem, file=sys.stderr)

    print("brand art=%d" % count)
    return 1 if problems else 0


if __name__ == "__main__":
    sys.exit(main())
