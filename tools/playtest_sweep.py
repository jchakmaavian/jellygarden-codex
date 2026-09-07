#!/usr/bin/env python3
"""Play a band of levels and print what went wrong.

There are 1208 levels. Playing them means running the playtest over and over,
and a run is a Unity invocation with a report file to find afterwards, a
screenshot directory that the next run overwrites, and a hang that will sit
there forever if nobody is watching. Doing that by hand is where a sweep dies.

This is the one command:

    python3 tools/playtest_sweep.py --levels 1-16
    python3 tools/playtest_sweep.py --levels 1-16 --batch 4
    python3 tools/playtest_sweep.py --levels 1,5,9

It runs the built macOS player when `Builds/Mac/KingLeo.app` is there, because
the player is the honest one: it raises no Editor-only ads errors, it never
reloads its domain mid-run, and it is the 393x852 window the game ships in.
With no player it falls back to a batchmode Editor running
`StoryPlaytestRunner.RunHeadless`, which gets the text and no pictures.

Either way the launch goes through `tools/run_player.sh`, which is the only
thing that holds the window flags — see `tools/check_launch_args.py`.

What it does per batch:

  arms the run with -playtestLevels=..., having deleted the stale report
  waits for the report to land, killing the batch at --timeout and calling it
    hung rather than blocking the sweep behind one bad level
  moves that batch's PNGs out of the way before the next batch renumbers them
  keeps every batch report under one run directory

and at the end prints every FINDING, grouped by level, and exits non-zero if
there were any. A hung or crashed batch is itself a finding: the levels in it
are named, because a level that hangs the game is exactly what the sweep is for.
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
# KINGLEO_APP is the launcher's own override, for a build kept elsewhere; this
# has to read the same one or the sweep decides "no player" about a bundle
# run_player.sh would have launched happily.
APP = os.environ.get(
    "KINGLEO_APP", os.path.join(PROJECT_ROOT, "Builds", "Mac", "KingLeo.app"))

# Where the player and the Editor both write. Application.persistentDataPath.
PERSIST = os.path.expanduser(
    "~/Library/Application Support/DefaultCompany/King Leo")
REPORT = os.path.join(PERSIST, "playtest.txt")
SHOTS = os.path.join(PERSIST, "playtest")
SWEEPS = os.path.join(PERSIST, "sweeps")

UNITY = os.environ.get(
    "UNITY",
    "/Applications/Unity/Hub/Editor/6000.5.10f1/Unity.app/Contents/MacOS/Unity")

# Seconds a batch gets before it is killed: a fixed cost to open the game, plus
# a generous allowance per level. A level that plays normally takes well under
# this; one that needs all of it is already suspicious.
STARTUP_BUDGET = 90
PER_LEVEL_BUDGET = 75

FINDING_LINE = re.compile(r"^FINDING:\s*(.*)$")
LEVEL_PREFIX = re.compile(r"^level (\d+):\s*(.*)$")
SUMMARY_LINE = re.compile(r"^SUMMARY:.*$", re.M)


# --------------------------------------------------------------------------
# The band


def parse_levels(text):
    """`1-16`, `1,5,9` and `1-4,20` all mean what they look like."""
    levels = []
    for part in text.split(","):
        part = part.strip()
        if not part:
            continue

        if "-" in part.lstrip("-"):
            low, _, high = part.partition("-")
            try:
                low, high = int(low), int(high)
            except ValueError:
                raise ValueError("not a level range: %s" % part)
            if high < low:
                raise ValueError("range runs backwards: %s" % part)
            levels.extend(range(low, high + 1))
        else:
            try:
                levels.append(int(part))
            except ValueError:
                raise ValueError("not a level: %s" % part)

    seen, unique = set(), []
    for level in levels:
        if level <= 0:
            raise ValueError("levels start at 1, got %d" % level)
        if level not in seen:
            seen.add(level)
            unique.append(level)
    return unique


def batches(levels, size):
    for start in range(0, len(levels), size):
        yield levels[start:start + size]


# --------------------------------------------------------------------------
# Running one batch


def command(levels, headless):
    """The command line for one batch, and how it will be described."""
    arg = "-playtestLevels=" + ",".join(str(level) for level in levels)

    if not headless:
        # --exec runs the binary in the foreground so this process owns it and
        # can kill it; the flags themselves still come from the launcher.
        return [LAUNCHER, "--exec", "-storyplaytest", arg]

    # No -quit: entering play mode reloads the domain, so the call that armed
    # the run is gone before the run starts. The runner exits the Editor itself
    # once the report is written.
    return [
        UNITY, "-batchmode", "-projectPath", PROJECT_ROOT, "-logFile", "-",
        "-executeMethod",
        "JellyGarden.Scripts.Story.Editor.StoryPlaytestRunner.RunHeadless",
        arg,
    ]


def run_batch(levels, headless, timeout, log_path):
    """Plays one batch. Returns (report text or None, why it ended)."""
    if os.path.exists(REPORT):
        os.remove(REPORT)
    if os.path.isdir(SHOTS):
        shutil.rmtree(SHOTS, ignore_errors=True)

    log = open(log_path, "wb")
    try:
        # Its own process group: a killed Unity leaves children behind, and the
        # next batch then fights the one before it for the project lock.
        process = subprocess.Popen(
            command(levels, headless),
            cwd=PROJECT_ROOT, stdout=log, stderr=subprocess.STDOUT,
            start_new_session=True)

        deadline = time.time() + timeout
        report = None
        why = "hung"

        while time.time() < deadline:
            if os.path.exists(REPORT):
                # The report is written in one call, but give the write a beat
                # to land before reading it.
                time.sleep(1.0)
                report = read(REPORT)
                why = "reported"
                break

            if process.poll() is not None:
                # It stopped without a report. Wait a moment in case the report
                # landed as it exited, then call it what it is.
                time.sleep(1.5)
                if os.path.exists(REPORT):
                    report = read(REPORT)
                    why = "reported"
                else:
                    why = "exited without a report (code %d)" % process.returncode
                break

            time.sleep(1.0)

        stop(process)
        return report, why
    finally:
        log.close()


def stop(process):
    """Ends the run and everything it started."""
    if process.poll() is not None:
        return

    for sig in (signal.SIGTERM, signal.SIGKILL):
        try:
            os.killpg(os.getpgid(process.pid), sig)
        except OSError:
            return
        for _ in range(20):
            if process.poll() is not None:
                return
            time.sleep(0.25)


def read(path):
    with open(path, encoding="utf-8", errors="replace") as handle:
        return handle.read()


def keep_shots(run_dir, index):
    """Moves this batch's PNGs somewhere the next batch will not overwrite.

    Every run numbers its shots from 0001, so batch 2 lands on batch 1's names.
    Returns how many were kept.
    """
    if not os.path.isdir(SHOTS):
        return 0

    kept = os.path.join(run_dir, "shots")
    os.makedirs(kept, exist_ok=True)

    count = 0
    for name in sorted(os.listdir(SHOTS)):
        if not name.endswith(".png"):
            continue
        shutil.move(os.path.join(SHOTS, name),
                    os.path.join(kept, "b%03d_%s" % (index, name)))
        count += 1
    return count


# --------------------------------------------------------------------------
# Reading a report


def findings_of(report):
    """Every FINDING line, as (level or 0, what)."""
    found = []
    for line in report.splitlines():
        match = FINDING_LINE.match(line.strip())
        if not match:
            continue

        what = match.group(1).strip()
        named = LEVEL_PREFIX.match(what)
        if named:
            found.append((int(named.group(1)), named.group(2).strip()))
        else:
            found.append((0, what))
    return found


def errors_of(report):
    """The error count off the SUMMARY line, or 0 when there is none."""
    match = re.search(r"(\d+) error\(s\)", report)
    return int(match.group(1)) if match else 0


def summary_of(report):
    match = SUMMARY_LINE.search(report)
    return match.group(0) if match else "(no SUMMARY line in the report)"


# --------------------------------------------------------------------------


def main():
    parser = argparse.ArgumentParser(
        description="Play a band of levels and print the findings.")
    parser.add_argument("--levels", default="1-16",
                        help="a band: 1-16, or 1,5,9, or 1-4,20 (default 1-16)")
    parser.add_argument("--batch", type=int, default=8,
                        help="levels per run (default 8)")
    parser.add_argument("--timeout", type=int, default=0,
                        help="seconds a batch gets before it is killed "
                             "(default %d + %d per level)"
                             % (STARTUP_BUDGET, PER_LEVEL_BUDGET))
    parser.add_argument("--headless", action="store_true",
                        help="force the batchmode Editor even when a player is built")
    args = parser.parse_args()

    try:
        levels = parse_levels(args.levels)
    except ValueError as problem:
        print("SWEEP: %s" % problem, file=sys.stderr)
        return 2

    if not levels:
        print("SWEEP: no levels to play", file=sys.stderr)
        return 2
    if args.batch < 1:
        print("SWEEP: --batch must be at least 1", file=sys.stderr)
        return 2

    headless = args.headless or not os.path.isdir(APP)
    if headless and not args.headless:
        print("no player at %s; falling back to a batchmode Editor "
              "(text only, no screenshots)." % APP)
    if headless and not os.path.exists(UNITY):
        print("SWEEP: no Unity at %s; set UNITY or build a player" % UNITY,
              file=sys.stderr)
        return 2

    stamp = datetime.datetime.now().strftime("%Y%m%d-%H%M%S")
    run_dir = os.path.join(SWEEPS, stamp)
    os.makedirs(run_dir, exist_ok=True)

    plan = list(batches(levels, args.batch))
    print("sweeping %d level(s) in %d batch(es) of up to %d, %s."
          % (len(levels), len(plan), args.batch,
             "batchmode Editor" if headless else "the built player"))
    print("run directory: %s" % run_dir)
    print("")

    found = []       # (level, what)
    hung = []        # (batch levels, why)
    errors = 0
    shots = 0

    for index, band in enumerate(plan, 1):
        timeout = args.timeout or (
            STARTUP_BUDGET + PER_LEVEL_BUDGET * len(band))
        span = "%d-%d" % (band[0], band[-1]) if len(band) > 1 else str(band[0])

        print("[batch %d/%d] levels %s, up to %ds"
              % (index, len(plan), span, timeout))
        sys.stdout.flush()

        log_path = os.path.join(run_dir, "batch_%03d.log" % index)
        started = time.time()
        report, why = run_batch(band, headless, timeout, log_path)
        took = time.time() - started

        shots += keep_shots(run_dir, index)

        if report is None:
            hung.append((band, why))
            print("           %s after %ds — log: %s"
                  % (why, int(took), os.path.relpath(log_path, PERSIST)))
            continue

        with open(os.path.join(run_dir, "batch_%03d.txt" % index), "w",
                  encoding="utf-8") as handle:
            handle.write(report)

        batch_found = findings_of(report)
        found.extend(batch_found)
        errors += errors_of(report)
        print("           %s (%ds)" % (summary_of(report), int(took)))

    # ---- the report ------------------------------------------------------

    print("")
    print("==== sweep ====")
    print("levels %s, %d batch(es), %d screenshot(s)"
          % (args.levels, len(plan), shots))
    print("screenshots: %s" % os.path.join(run_dir, "shots"))
    print("reports:     %s" % run_dir)
    print("")

    if hung:
        print("---- batches that never reported: %d ----" % len(hung))
        for band, why in hung:
            print("- levels %s: %s"
                  % (",".join(str(level) for level in band), why))
        print("")

    by_level = {}
    for level, what in found:
        by_level.setdefault(level, []).append(what)

    print("---- findings: %d ----" % len(found))
    for level in sorted(by_level):
        print("level %s:" % (level if level else "?"))
        for what in by_level[level]:
            print("  - %s" % what)
    if not found:
        print("(none)")
    print("")
    print("errors raised during play: %d" % errors)
    print("==== end ====")

    # A hung batch is a finding the report could not write, so it counts.
    if found or hung:
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
