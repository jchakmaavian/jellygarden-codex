#!/usr/bin/env python3
"""Prove the story did not cost the game its animation or its visual effects.

That was the owner's one hard requirement, so it gets its own check rather than
being asserted in a commit message. Everything here compares the working tree
against the commit the story branch started from.

Four things are checked:

  no animation, animator or particle asset was deleted or renamed
  no prefab lost a ParticleSystem, Animator or Animation component
  the board scripts changed additively — no method was removed
  no sprite or texture the game already had disappeared

    python3 tools/check_vfx_intact.py [--base <ref>]
"""

import argparse
import os
import re
import subprocess
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from story_common import PROJECT_ROOT  # noqa: E402

# The commit this branch started from. Everything before it is the kit as it
# shipped; everything after is the story.
DEFAULT_BASE = "a82c4df"

VISUAL_EXTENSIONS = (".anim", ".controller", ".overrideController", ".prefab",
                     ".png", ".jpg", ".jpeg", ".mat", ".shader", ".shadergraph")

# The scripts that drive the board and its feel. These may gain hooks; they may
# not lose behaviour.
BOARD_SCRIPTS = [
    "Assets/JellyGarden/Scripts/LevelManager.cs",
    "Assets/JellyGarden/Scripts/Item.cs",
    "Assets/JellyGarden/Scripts/Square.cs",
    "Assets/JellyGarden/Scripts/GUI/AnimationManager.cs",
    "Assets/JellyGarden/Scripts/ItemAnimEvents.cs",
    "Assets/JellyGarden/Scripts/ParticleSorting.cs",
]

# Unity component type ids that carry the game's feel.
VISUAL_COMPONENTS = {
    "198": "ParticleSystem",
    "199": "ParticleSystemRenderer",
    "95": "Animator",
    "111": "Animation",
    "212": "SpriteRenderer",
    "1001": "PrefabInstance",
}

METHOD_RE = re.compile(
    r"^\s*(?:public|private|protected|internal|static|virtual|override|sealed|async|\s)*"
    r"(?:IEnumerator|void|bool|int|float|string|Vector[23]|GameObject|Sprite|[A-Z]\w*)"
    r"\s+(\w+)\s*\(",
    re.MULTILINE,
)


def git(*args):
    return subprocess.run(
        ["git"] + list(args), cwd=PROJECT_ROOT,
        capture_output=True, text=True, check=False).stdout


def file_at(ref, path):
    result = subprocess.run(
        ["git", "show", "%s:%s" % (ref, path)], cwd=PROJECT_ROOT,
        capture_output=True, text=True, check=False)
    return result.stdout if result.returncode == 0 else None


def changed_files(base):
    out = git("diff", "--name-status", base, "--")
    entries = []
    for line in out.splitlines():
        parts = line.split("\t")
        if len(parts) >= 2:
            entries.append((parts[0], parts[-1]))
    return entries


def check_no_visual_deletions(base, failures):
    for status, path in changed_files(base):
        if not path.endswith(VISUAL_EXTENSIONS):
            continue
        if status.startswith("D"):
            failures.append("deleted visual asset: %s" % path)
        elif status.startswith("R"):
            failures.append("renamed visual asset: %s" % path)


def count_components(text):
    """How many of each feel-carrying component a Unity YAML file declares."""
    counts = {}
    for match in re.finditer(r"^--- !u!(\d+) &", text, flags=re.MULTILINE):
        type_id = match.group(1)
        if type_id in VISUAL_COMPONENTS:
            name = VISUAL_COMPONENTS[type_id]
            counts[name] = counts.get(name, 0) + 1
    return counts


def check_components_not_lost(base, failures):
    for status, path in changed_files(base):
        if status.startswith("D") or not path.endswith((".prefab", ".unity")):
            continue

        before = file_at(base, path)
        if before is None:
            continue  # new file; nothing to lose

        full = os.path.join(PROJECT_ROOT, path)
        if not os.path.exists(full):
            failures.append("missing from the working tree: %s" % path)
            continue

        with open(full, encoding="utf-8", errors="replace") as fh:
            after = fh.read()

        old_counts = count_components(before)
        new_counts = count_components(after)

        for name, old in old_counts.items():
            new = new_counts.get(name, 0)
            if new < old:
                failures.append(
                    "%s lost %d of its %d %s components (now %d)"
                    % (path, old - new, old, name, new))


def check_board_scripts_additive(base, failures):
    for path in BOARD_SCRIPTS:
        before = file_at(base, path)
        full = os.path.join(PROJECT_ROOT, path)

        if before is None:
            continue
        if not os.path.exists(full):
            failures.append("board script deleted: %s" % path)
            continue

        with open(full, encoding="utf-8", errors="replace") as fh:
            after = fh.read()

        old_methods = set(METHOD_RE.findall(before))
        new_methods = set(METHOD_RE.findall(after))
        removed = old_methods - new_methods
        if removed:
            failures.append("%s lost methods: %s"
                            % (path, ", ".join(sorted(removed))))

        # A hook adds lines. Losing a meaningful number of them means something
        # was taken out rather than added to.
        old_lines = len([line for line in before.splitlines() if line.strip()])
        new_lines = len([line for line in after.splitlines() if line.strip()])
        if new_lines < old_lines:
            failures.append("%s shrank by %d lines; changes here must be additive"
                            % (path, old_lines - new_lines))


def summarise(base):
    prefabs = 0
    particles = 0
    animations = 0
    for root, _dirs, files in os.walk(os.path.join(PROJECT_ROOT, "Assets", "JellyGarden")):
        for name in files:
            if name.endswith(".prefab"):
                prefabs += 1
            elif name.endswith(".anim"):
                animations += 1

    particle_dir = os.path.join(PROJECT_ROOT, "Assets", "JellyGarden",
                                "Resources", "Particles")
    if os.path.isdir(particle_dir):
        particles = len([n for n in os.listdir(particle_dir) if n.endswith(".prefab")])

    print("since %s: %d prefabs, %d animation clips, %d particle prefabs present"
          % (base, prefabs, animations, particles))


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--base", default=DEFAULT_BASE,
                        help="the commit the story branch started from")
    args = parser.parse_args()

    if not git("cat-file", "-t", args.base).strip():
        print("ERROR: unknown base ref %s" % args.base, file=sys.stderr)
        return 2

    failures = []
    check_no_visual_deletions(args.base, failures)
    check_components_not_lost(args.base, failures)
    check_board_scripts_additive(args.base, failures)

    summarise(args.base)

    if failures:
        print()
        for failure in failures:
            print("REGRESSION: " + failure, file=sys.stderr)
        print("\n%d visual regressions." % len(failures), file=sys.stderr)
        return 1

    print("No animation, particle or VFX regressions.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
