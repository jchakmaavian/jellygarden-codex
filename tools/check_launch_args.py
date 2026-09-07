#!/usr/bin/env python3
"""Prove nothing can launch the game full screen.

The player is always windowed at 393x852 — an iPhone 15/16 in portrait. A
full-screen launch takes over the display for as long as a 1208-level sweep
runs, and every screenshot it takes comes out the shape of this machine's
desktop rather than the shape of the phone the game ships on.

That rule survives only if there is one place to get it wrong. The flags live
in `tools/run_player.sh`; the same two numbers live in `StoryWindow.cs`, which
puts a player that was opened some other way back into a window. This check
holds those two together and fails on any third copy:

  the numbers in run_player.sh and StoryWindow.cs agree
  run_player.sh actually passes -screen-fullscreen 0
  no other file spells the screen flags out for itself
  no other file opens the .app bundle directly

    python3 tools/check_launch_args.py
"""

import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from story_common import PROJECT_ROOT  # noqa: E402

LAUNCHER = "tools/run_player.sh"
WINDOW_SCRIPT = "Assets/JellyGarden/Scripts/Story/StoryWindow.cs"
CHECKER = "tools/check_launch_args.py"

# Where a launch instruction could plausibly live: the tools, the story scripts,
# the docs. `ralph/` is the agent's own PRD and log — it describes the rule
# rather than launching anything — and `Builds/` is output.
SEARCH_ROOTS = ["tools", "Assets/JellyGarden/Scripts", "docs"]
SEARCH_SUFFIXES = (".py", ".sh", ".cs", ".md", ".txt")

# The two files above are the only ones allowed to hold the flags.
ALLOWED = {LAUNCHER, WINDOW_SCRIPT, CHECKER}

SCREEN_FLAG = re.compile(r"-screen-(?:fullscreen|width|height)\b")
OPENS_APP = re.compile(r"open\s+-a\s+\S*KingLeo\.app")


def read(rel_path):
    with open(os.path.join(PROJECT_ROOT, rel_path), encoding="utf-8") as handle:
        return handle.read()


def launcher_size(failures):
    """The width and height tools/run_player.sh will actually pass."""
    text = read(LAUNCHER)

    width = re.search(r"^WIDTH=(\d+)$", text, re.M)
    height = re.search(r"^HEIGHT=(\d+)$", text, re.M)
    if not width or not height:
        failures.append("%s has no WIDTH= / HEIGHT= to read" % LAUNCHER)
        return None

    if "-screen-fullscreen 0" not in text:
        failures.append("%s does not pass -screen-fullscreen 0" % LAUNCHER)

    if "-screen-width \"$WIDTH\"" not in text or "-screen-height \"$HEIGHT\"" not in text:
        failures.append("%s does not pass its own WIDTH/HEIGHT to the player" % LAUNCHER)

    return int(width.group(1)), int(height.group(1))


def runtime_size(failures):
    """The width and height StoryWindow.cs will put a stray player back to."""
    text = read(WINDOW_SCRIPT)

    width = re.search(r"public const int Width = (\d+);", text)
    height = re.search(r"public const int Height = (\d+);", text)
    if not width or not height:
        failures.append("%s has no Width / Height constants to read" % WINDOW_SCRIPT)
        return None

    if not re.search(r"public const int ShotSuperSize = 2;", text):
        failures.append("%s must capture at superSize 2" % WINDOW_SCRIPT)

    if "FullScreenMode.Windowed" not in text:
        failures.append("%s no longer forces a windowed player" % WINDOW_SCRIPT)

    return int(width.group(1)), int(height.group(1))


def check_no_other_copies(failures):
    for root in SEARCH_ROOTS:
        for directory, _, names in os.walk(os.path.join(PROJECT_ROOT, root)):
            for name in sorted(names):
                if not name.endswith(SEARCH_SUFFIXES):
                    continue

                path = os.path.join(directory, name)
                rel = os.path.relpath(path, PROJECT_ROOT).replace(os.sep, "/")
                if rel in ALLOWED:
                    continue

                with open(path, encoding="utf-8", errors="replace") as handle:
                    for number, line in enumerate(handle, 1):
                        if SCREEN_FLAG.search(line):
                            failures.append(
                                "%s:%d repeats the screen flags; run %s instead"
                                % (rel, number, LAUNCHER))
                        if OPENS_APP.search(line):
                            failures.append(
                                "%s:%d opens the app itself; run %s instead"
                                % (rel, number, LAUNCHER))


def check_capture_goes_through_window(failures):
    """Every screenshot is StoryWindow.Capture, so none can miss superSize."""
    for directory, _, names in os.walk(
            os.path.join(PROJECT_ROOT, "Assets/JellyGarden/Scripts")):
        for name in sorted(names):
            if not name.endswith(".cs"):
                continue

            path = os.path.join(directory, name)
            rel = os.path.relpath(path, PROJECT_ROOT).replace(os.sep, "/")
            if rel == WINDOW_SCRIPT:
                continue

            with open(path, encoding="utf-8", errors="replace") as handle:
                for number, line in enumerate(handle, 1):
                    if line.lstrip().startswith("//"):
                        continue
                    if "ScreenCapture.CaptureScreenshot" in line:
                        failures.append(
                            "%s:%d captures directly; call StoryWindow.Capture"
                            % (rel, number))


def main():
    failures = []

    launcher = launcher_size(failures)
    runtime = runtime_size(failures)

    if launcher and runtime and launcher != runtime:
        failures.append(
            "%s launches %dx%d but %s forces %dx%d"
            % (LAUNCHER, launcher[0], launcher[1],
               WINDOW_SCRIPT, runtime[0], runtime[1]))

    check_no_other_copies(failures)
    check_capture_goes_through_window(failures)

    if failures:
        for failure in failures:
            print("LAUNCH: " + failure, file=sys.stderr)
        print("\n%d launch problem(s)." % len(failures), file=sys.stderr)
        return 1

    print("Launch is windowed at %dx%d everywhere, screenshots at superSize 2."
          % (launcher[0], launcher[1]))
    return 0


if __name__ == "__main__":
    sys.exit(main())
