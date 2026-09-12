#!/usr/bin/env python3
"""Bring the already-generated King Leo art and audio into this project.

The story project generated a full set of Arc 1 assets — five progressive gate
states, the castle reveal, Leo, Mimi, the gatekeeper's logbook, Alfred's five
narration lines, two music beds and eight sound effects — plus a parked set of
Area 002 courtyard art. They are paid for and correct; this copies them in and
names them the way the runtime looks them up.

Naming is the whole job. StoryArt resolves art by convention
(area_001_state0..state4, clue_001, portrait_leo, backdrop_castle), so a file
placed under the right name is wired with no code change and no inspector.

    python3 tools/asset_import.py
"""

import os
import re
import shutil
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from story_common import (  # noqa: E402
    STORY_RES,
    STORY_ROOT,
    ensure_dir,
    guid_for,
    rel_asset_path,
)

SOURCE_RESOURCES = os.path.join(STORY_ROOT, "game", "unity", "Assets", "Resources")
SOURCE_ART = os.path.join(SOURCE_RESOURCES, "Art")
SOURCE_AUDIO = os.path.join(SOURCE_RESOURCES, "Audio")
SOURCE_AREA002 = os.path.join(STORY_ROOT, "game", "_area002_parked")

DEST_ART = os.path.join(STORY_RES, "Art")
DEST_AUDIO = os.path.join(STORY_RES, "Audio")

# --- art ---------------------------------------------------------------------
#
# The gate states are the important ones: each was generated from the state
# before it, so they read as one gate being repaired rather than five different
# gates. They map straight onto area 001's four repair tasks.
ART = {
    # Area 001 — Castle Gates. state0 is the ruin; each repair reveals the next.
    "gate_state0_ruined.jpg": "area_001_state0",
    "gate_state1_ivycut.jpg": "area_001_state1",
    "gate_state2_timbers.jpg": "area_001_state2",
    "gate_state3_banner.jpg": "area_001_state3",
    "gate_state4_lanterns.jpg": "area_001_state4",

    # The clue that closes area 001.
    "clue_logbook.jpg": "clue_001",

    # Portraits are no longer imported from here. The whole cast was redrawn —
    # eight characters in four moods each, bright and warm all the way through,
    # written straight into Resources/Story/Art as portrait_{name}.png and
    # portrait_{name}_{mood}.png. Importing leo.jpg and mimi.jpg again would put
    # a portrait_leo.jpg beside the portrait_leo.png that now owns that key, and
    # Resources.Load would pick between them by luck.

    # The castle reveal is the opening's centrepiece and doubles as arc 1's
    # backdrop, so every castle area has something behind it.
    "castle_reveal.jpg": "backdrop_castle",
}

# Area 002's courtyard states, generated during a misread of scope and parked.
#
# ASSETS.md flags these: all six were generated against the ruined courtyard
# rather than each against the state before it, so a later state can silently
# undo an earlier repair. They are imported because a real courtyard beats a
# flat colour, and the flaw is recorded here rather than discovered later.
AREA002 = {
    "courtyard_state0_ruined.jpg": "area_002_state0",
    "courtyard_state1_scaffolding.jpg": "area_002_state1",
    "courtyard_state2_repaved.jpg": "area_002_state2",
    "courtyard_state3_statue.jpg": "area_002_state3",
    "courtyard_state4_trough.jpg": "area_002_state4",
    "courtyard_state5_clean.jpg": "area_002_state5",
    "clue_statue_base.jpg": "clue_002",
}

# --- audio -------------------------------------------------------------------

VOICE = {
    "alfred_open_01.mp3": "alfred_open_01",
    "alfred_open_02.mp3": "alfred_open_02",
    "alfred_open_03.mp3": "alfred_open_03",
    "alfred_open_04.mp3": "alfred_open_04",
    "alfred_clue_001.mp3": "alfred_clue_001",
}

MUSIC = {
    "theme_ruin.mp3": "theme_ruin",
    "theme_level.mp3": "theme_level",
}

SFX = {
    "match_pop.mp3": "match_pop",
    "vine_cut.mp3": "vine_cut",
    "timber_break.mp3": "timber_break",
    "lantern_light.mp3": "lantern_light",
    "star_earned.mp3": "star_earned",
    "repair_done.mp3": "repair_done",
    "booster_hammer.mp3": "booster_hammer",
    "invalid.mp3": "invalid",
}


def sprite_meta(dest, border=None):
    """
    Texture import settings for a story picture.

    Sprite, single, full rect, no mip maps — these are full-screen backdrops and
    portraits, not atlased UI, so mip maps would only soften them.

    `border` is (left, bottom, right, top) in pixels for a nine-sliced sprite —
    a panel whose ornate corners must not stretch. Omitted, the sprite has no
    border and Image.Type.Sliced falls back to drawing it whole, so passing
    nothing is the same sprite the callers before this always got.
    """
    left, bottom, right, top = border if border else (0, 0, 0, 0)
    return """fileFormatVersion: 2
guid: %s
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {x: 0.5, y: 0.5}
  spritePixelsToUnits: 100
  spriteBorder: {x: BORDER_X, y: BORDER_Y, z: BORDER_Z, w: BORDER_W}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    customData:
    physicsShape: []
    bones: []
    spriteID: 5e97eb03825dee720800000000000000
    internalID: 0
    vertices: []
    indices:
    edges: []
    weights: []
    secondaryTextures: []
  spritePackingTag:
  pSDRemoveMatte: 0
  userData:
  assetBundleName:
  assetBundleVariant:
""".replace("BORDER_X", str(left)).replace("BORDER_Y", str(bottom)) \
       .replace("BORDER_Z", str(right)).replace("BORDER_W", str(top)) \
       % guid_for(rel_asset_path(dest))


def audio_meta(dest, streaming):
    """
    Audio import settings.

    Music streams — a 45-second bed does not belong decompressed in memory.
    Narration and effects decompress on load so a line never stutters on the
    first play.
    """
    load_type = 2 if streaming else 0  # 2 = Streaming, 0 = DecompressOnLoad
    return """fileFormatVersion: 2
guid: %s
AudioImporter:
  externalObjects: {}
  serializedVersion: 7
  defaultSettings:
    serializedVersion: 2
    loadType: %d
    sampleRateSetting: 0
    sampleRateOverride: 44100
    compressionFormat: 1
    quality: 1
    conversionMode: 0
    preloadAudioData: 1
  platformSettingOverrides: {}
  forceToMono: 0
  normalize: 1
  preloadAudioData: 0
  loadInBackground: %d
  ambisonic: 0
  3D: 0
  userData:
  assetBundleName:
  assetBundleVariant:
""" % (guid_for(rel_asset_path(dest)), load_type, 1 if streaming else 0)


def copy(source_dir, source_name, dest_dir, dest_stem, meta_fn):
    source = os.path.join(source_dir, source_name)
    if not os.path.exists(source):
        return "missing: %s" % os.path.join(os.path.basename(source_dir), source_name)

    extension = os.path.splitext(source_name)[1]
    dest = os.path.join(dest_dir, dest_stem + extension)

    # Never overwrite an existing Jelly Garden asset. Every destination here is
    # inside Resources/Story, which the kit does not use, but the check costs
    # nothing and the rule is worth enforcing.
    if os.path.exists(dest) and os.path.getsize(dest) == os.path.getsize(source):
        return None

    ensure_dir(dest_dir)
    shutil.copyfile(source, dest)
    with open(dest + ".meta", "w", encoding="utf-8") as fh:
        fh.write(meta_fn(dest))
    return None


def main():
    ensure_dir(DEST_ART)
    ensure_dir(os.path.join(DEST_AUDIO, "VO"))
    ensure_dir(os.path.join(DEST_AUDIO, "Music"))
    ensure_dir(os.path.join(DEST_AUDIO, "SFX"))

    problems = []
    counts = {"art": 0, "vo": 0, "music": 0, "sfx": 0}

    for source_name, stem in ART.items():
        problem = copy(SOURCE_ART, source_name, DEST_ART, stem, sprite_meta)
        problems.append(problem) if problem else counts.__setitem__("art", counts["art"] + 1)

    for source_name, stem in AREA002.items():
        problem = copy(SOURCE_AREA002, source_name, DEST_ART, stem, sprite_meta)
        problems.append(problem) if problem else counts.__setitem__("art", counts["art"] + 1)

    for source_name, stem in VOICE.items():
        problem = copy(os.path.join(SOURCE_AUDIO, "VO"), source_name,
                       os.path.join(DEST_AUDIO, "VO"), stem,
                       lambda d: audio_meta(d, streaming=False))
        problems.append(problem) if problem else counts.__setitem__("vo", counts["vo"] + 1)

    for source_name, stem in MUSIC.items():
        problem = copy(os.path.join(SOURCE_AUDIO, "Music"), source_name,
                       os.path.join(DEST_AUDIO, "Music"), stem,
                       lambda d: audio_meta(d, streaming=True))
        problems.append(problem) if problem else counts.__setitem__("music", counts["music"] + 1)

    for source_name, stem in SFX.items():
        problem = copy(os.path.join(SOURCE_AUDIO, "SFX"), source_name,
                       os.path.join(DEST_AUDIO, "SFX"), stem,
                       lambda d: audio_meta(d, streaming=False))
        problems.append(problem) if problem else counts.__setitem__("sfx", counts["sfx"] + 1)

    for problem in problems:
        print("WARNING: " + problem, file=sys.stderr)

    print("art=%d vo=%d music=%d sfx=%d" % (
        counts["art"], counts["vo"], counts["music"], counts["sfx"]))
    if problems:
        print("%d source files were not found; the story falls back for those."
              % len(problems))
    return 0


if __name__ == "__main__":
    sys.exit(main())
