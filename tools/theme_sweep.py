#!/usr/bin/env python3
"""Open one board in every arc, keep the six pictures, print the findings.

`check_themes.py` settles what the theme data says; this settles what a board
actually wears. It runs the built player once with `-storythemetest`, which
launches the first level of each of the six arcs into the area that level
belongs to, verifies the backdrop and the ground that reached the cells, and
photographs each one.

    python3 tools/theme_sweep.py
    python3 tools/theme_sweep.py --timeout 400

Everything for a run lands in

    ~/Library/Application Support/DefaultCompany/King Leo/themes/<timestamp>/

as `themetest.txt`, the player log, and `shots/arc_1_castle.png` … `arc_6_siege.png`
— one 393x852 PNG per arc, read off the back buffer.

Those shots carry the HUD and a sharp room. Shots taken before this story did
not: `StoryWindow.Capture` re-rendered the frame at superSize 2, which drops the
Screen Space - Camera canvas the HUD and the backdrop live on. Any screenshot in
this project older than that is missing both, and two entries in
ralph/findings.md were written from one.

Its exit code is not a verdict. Zero findings means nothing resolved to the kit
default and no two arcs came out identical; it says nothing about whether an arc
LOOKS like the place it is meant to be. Open the six PNGs and put them side by
side — that is the half of this story no check can do.
"""

import argparse
import datetime
import os
import re
import shutil
import signal
import subprocess
import sys
import time

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from story_common import PROJECT_ROOT  # noqa: E402

LAUNCHER = os.path.join(PROJECT_ROOT, "tools", "run_player.sh")

# The launcher's own override, read here too so this does not decide "no player"
# about a bundle run_player.sh would have launched happily.
APP = os.environ.get(
    "KINGLEO_APP", os.path.join(PROJECT_ROOT, "Builds", "Mac", "KingLeo.app"))

PERSIST = os.path.expanduser("~/Library/Application Support/DefaultCompany/King Leo")
REPORT = os.path.join(PERSIST, "themetest.txt")
SHOTS = os.path.join(PERSIST, "themetest")
RUNS = os.path.join(PERSIST, "themes")

# Six levels, each opened into its own area, each waited out until the board is
# still. Well under this when it is healthy; an arc that needs all of it is
# already the finding.
DEFAULT_TIMEOUT = 420

FINDING_LINE = re.compile(r"^FINDING:\s*(.*)$")
ARC_PREFIX = re.compile(r"^arc (\d+):\s*(.*)$")


def run(timeout, run_dir):
    """Runs the player once. Returns (report text or None, why it ended)."""
    if os.path.exists(REPORT):
        os.remove(REPORT)
    if os.path.isdir(SHOTS):
        shutil.rmtree(SHOTS, ignore_errors=True)

    log_path = os.path.join(run_dir, "player.log")
    log = open(log_path, "wb")
    try:
        # Its own process group, so a kill takes the children with it — a Unity
        # left behind holds the project lock against everything after it.
        process = subprocess.Popen(
            [LAUNCHER, "--exec", "-storythemetest"],
            cwd=PROJECT_ROOT, stdout=log, stderr=subprocess.STDOUT,
            start_new_session=True)

        deadline = time.time() + timeout
        report = None
        why = "hung"

        while time.time() < deadline:
            if os.path.exists(REPORT):
                # Give the write a beat to land before reading it.
                time.sleep(1.0)
                with open(REPORT, encoding="utf-8", errors="replace") as fh:
                    report = fh.read()
                why = "reported"
                break
            if process.poll() is not None:
                # It exited without writing one. Wait a moment in case the write
                # and the exit raced, then give up on it.
                time.sleep(1.5)
                if os.path.exists(REPORT):
                    with open(REPORT, encoding="utf-8", errors="replace") as fh:
                        report = fh.read()
                    why = "reported"
                else:
                    why = "exited without a report (code %s)" % process.returncode
                break
            time.sleep(1.0)

        if process.poll() is None:
            try:
                os.killpg(os.getpgid(process.pid), signal.SIGKILL)
            except OSError:
                pass
            process.wait(timeout=30)

        return report, why
    finally:
        log.close()


def keep(run_dir, report):
    """Puts the report and the six PNGs somewhere the next run will not eat."""
    if report is not None:
        with open(os.path.join(run_dir, "themetest.txt"), "w", encoding="utf-8") as fh:
            fh.write(report)

    kept = 0
    if os.path.isdir(SHOTS):
        shots = os.path.join(run_dir, "shots")
        os.makedirs(shots, exist_ok=True)
        for name in sorted(os.listdir(SHOTS)):
            if not name.endswith(".png"):
                continue
            shutil.move(os.path.join(SHOTS, name), os.path.join(shots, name))
            kept += 1
    return kept


def findings_of(report):
    """The FINDING lines, grouped by the arc they name."""
    grouped = {}
    for line in (report or "").split("\n"):
        match = FINDING_LINE.match(line.strip())
        if not match:
            continue
        what = match.group(1)
        arc = ARC_PREFIX.match(what)
        if arc:
            grouped.setdefault(int(arc.group(1)), []).append(arc.group(2))
        else:
            grouped.setdefault(0, []).append(what)
    return grouped


def main():
    parser = argparse.ArgumentParser(
        description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--timeout", type=int, default=DEFAULT_TIMEOUT,
                        help="seconds before the run is killed (default %d)" % DEFAULT_TIMEOUT)
    args = parser.parse_args()

    if not os.path.isdir(APP):
        print("no player at %s" % APP, file=sys.stderr)
        print("build one from the open Editor: JellyGarden > Story > Build macOS Player",
              file=sys.stderr)
        return 2

    stamp = datetime.datetime.now().strftime("%Y%m%d-%H%M%S")
    run_dir = os.path.join(RUNS, stamp)
    os.makedirs(run_dir, exist_ok=True)
    print("run -> %s" % run_dir)

    started = time.time()
    report, why = run(args.timeout, run_dir)
    kept = keep(run_dir, report)
    print("%s after %.0fs; %d shot(s) kept." % (why, time.time() - started, kept))

    if report is None:
        print("\nNO REPORT. The player never wrote %s — see %s/player.log."
              % (os.path.basename(REPORT), run_dir), file=sys.stderr)
        return 1

    grouped = findings_of(report)
    total = sum(len(v) for v in grouped.values())

    print()
    if total == 0:
        print("0 findings across 6 arcs.")
    else:
        for arc in sorted(grouped):
            print("arc %d:" % arc if arc else "no arc:")
            for what in grouped[arc]:
                print("  - %s" % what)
        print("\n%d finding(s)." % total)

    print("\nZero findings is not a verdict. Open %s/shots and look at the six." % run_dir)
    return 1 if total else 0


if __name__ == "__main__":
    sys.exit(main())
