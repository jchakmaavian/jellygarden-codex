#!/usr/bin/env bash
#
# Launches the built macOS player. This is the only thing that should.
#
# The window is always 393x852 — an iPhone 15/16 in portrait — and never full
# screen. A full-screen launch takes over the display for however long a sweep
# runs, and every screenshot it takes comes out a 16:10 desktop rather than the
# 9:19.5 phone the game ships on, which is the wrong shape to judge a layout in.
#
# The flags live here and in StoryWindow.cs, and tools/check_launch_args.py
# fails if the two disagree or if any other file spells them out for itself.
#
# Usage:
#   tools/run_player.sh                    # open the game
#   tools/run_player.sh -storyplaytest     # anything extra is passed through
#   tools/run_player.sh -storydemo
#   tools/run_player.sh -storyselftest
#   tools/run_player.sh --print-args       # the flags, for a script that must
#                                          # build its own command line
#
# KINGLEO_APP overrides the app bundle, for a release build kept elsewhere.

set -euo pipefail

WIDTH=393
HEIGHT=852
LAUNCH_ARGS=(-screen-fullscreen 0 -screen-width "$WIDTH" -screen-height "$HEIGHT")

if [[ "${1:-}" == "--print-args" ]]; then
    echo "${LAUNCH_ARGS[@]}"
    exit 0
fi

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
APP="${KINGLEO_APP:-$ROOT/Builds/Mac/KingLeo.app}"

if [[ ! -d "$APP" ]]; then
    echo "no player at $APP" >&2
    echo "build one first:" >&2
    echo "  \"\$UNITY\" -batchmode -quit -nographics -projectPath . -logFile - \\" >&2
    echo "    -executeMethod JellyGarden.Scripts.Story.Editor.StoryBuild.BuildMac" >&2
    exit 1
fi

echo "launching $APP ${LAUNCH_ARGS[*]} $*"
exec open -a "$APP" --args "${LAUNCH_ARGS[@]}" "$@"
