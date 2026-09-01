"""Shared helpers for the King Leo story generators.

Every generator in this folder writes into the Unity project, so they all need
the same two things: the project root, and a way to mint a stable .meta file for
a generated asset. Unity refuses to keep references across a regeneration if the
GUID moves, so GUIDs are derived from the asset path rather than randomised.
"""

import hashlib
import json
import os

# tools/ lives at the Unity project root.
PROJECT_ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
STORY_ROOT = "/Users/avian/Desktop/story"

ASSETS = os.path.join(PROJECT_ROOT, "Assets")
JELLY = os.path.join(ASSETS, "JellyGarden")
RESOURCES = os.path.join(JELLY, "Resources")
STORY_RES = os.path.join(RESOURCES, "Story")
LEVELS_RES = os.path.join(RESOURCES, "Levels")
TARGETS_RES = os.path.join(RESOURCES, "Targets")
OUT = os.path.join(PROJECT_ROOT, "tools", "out")

ARC_TITLES = {
    1: "The Missing Queen",
    2: "Grimm's Trail",
    3: "The Empty Treasury",
    4: "The Rival Crown",
    5: "The Old King's Secret",
    6: "The Reckoning",
}

ARC_DIRS = {
    1: "arc-1-the-missing-queen",
    2: "arc-2-grimms-trail",
    3: "arc-3-the-empty-treasury",
    4: "arc-4-the-rival-crown",
    5: "arc-5-the-old-kings-secret",
    6: "arc-6-the-reckoning",
}

# The six arcs move through the kingdom; the board dresses itself to match.
ARC_THEMES = {
    1: "castle",
    2: "village",
    3: "mines",
    4: "border",
    5: "crypts",
    6: "siege",
}


def guid_for(asset_path):
    """Deterministic Unity GUID for a project-relative asset path."""
    return hashlib.md5(asset_path.replace(os.sep, "/").encode("utf-8")).hexdigest()


def rel_asset_path(abs_path):
    return os.path.relpath(abs_path, PROJECT_ROOT).replace(os.sep, "/")


def write_text_meta(abs_path):
    """.meta for a TextAsset (json/txt) so Unity imports it as one."""
    meta = abs_path + ".meta"
    if os.path.exists(meta):
        return False
    guid = guid_for(rel_asset_path(abs_path))
    with open(meta, "w", encoding="utf-8") as fh:
        fh.write(
            "fileFormatVersion: 2\n"
            "guid: %s\n"
            "TextScriptImporter:\n"
            "  externalObjects: {}\n"
            "  userData: \n"
            "  assetBundleName: \n"
            "  assetBundleVariant: \n" % guid
        )
    return True


def write_native_meta(abs_path):
    """.meta for a ScriptableObject .asset."""
    meta = abs_path + ".meta"
    if os.path.exists(meta):
        return False
    guid = guid_for(rel_asset_path(abs_path))
    with open(meta, "w", encoding="utf-8") as fh:
        fh.write(
            "fileFormatVersion: 2\n"
            "guid: %s\n"
            "NativeFormatImporter:\n"
            "  externalObjects: {}\n"
            "  mainObjectFileID: 11400000\n"
            "  userData: \n"
            "  assetBundleName: \n"
            "  assetBundleVariant: \n" % guid
        )
    return True


def write_folder_meta(abs_dir):
    meta = abs_dir.rstrip(os.sep) + ".meta"
    if os.path.exists(meta):
        return False
    guid = guid_for(rel_asset_path(abs_dir))
    with open(meta, "w", encoding="utf-8") as fh:
        fh.write(
            "fileFormatVersion: 2\n"
            "guid: %s\n"
            "folderAsset: yes\n"
            "DefaultImporter:\n"
            "  externalObjects: {}\n"
            "  userData: \n"
            "  assetBundleName: \n"
            "  assetBundleVariant: \n" % guid
        )
    return True


def ensure_dir(abs_dir):
    """mkdir -p, minting folder .meta files for every new level."""
    parts = []
    cur = abs_dir
    while not os.path.isdir(cur):
        parts.append(cur)
        cur = os.path.dirname(cur)
    for path in reversed(parts):
        os.makedirs(path, exist_ok=True)
        if path.startswith(ASSETS):
            write_folder_meta(path)


def load_arcs(story_json_dir=None):
    """Return {arc_number: parsed arc file} for all six arcs."""
    if story_json_dir is None:
        story_json_dir = STORY_RES
        if not os.path.isdir(story_json_dir):
            story_json_dir = os.path.join(
                STORY_ROOT, "game", "unity", "Assets", "StreamingAssets", "story"
            )
    arcs = {}
    for n in range(1, 7):
        with open(os.path.join(story_json_dir, "arc%d.json" % n), encoding="utf-8") as fh:
            arcs[n] = json.load(fh)
    return arcs


def all_areas(arcs=None):
    """Every area across all six arcs, ordered by id."""
    if arcs is None:
        arcs = load_arcs()
    areas = []
    for n in range(1, 7):
        areas.extend(arcs[n]["areas"])
    areas.sort(key=lambda a: a["id"])
    return areas


def area_star_cost(area):
    """Stars needed to finish an area. Trust the tasks over the summary field."""
    return sum(int(t.get("stars", 1)) for t in area["tasks"])


def dump_json(abs_path, payload, meta=True):
    ensure_dir(os.path.dirname(abs_path))
    with open(abs_path, "w", encoding="utf-8") as fh:
        json.dump(payload, fh, indent=1, ensure_ascii=False)
        fh.write("\n")
    if meta and abs_path.startswith(ASSETS):
        write_text_meta(abs_path)
