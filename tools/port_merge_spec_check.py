#!/usr/bin/env python3
"""Verify the anchors quoted in docs/port/merge-specs/*.md against git refs.

Every merge spec ends with one or more fenced blocks tagged ``anchors``:

    ```anchors
    path: Assets/JellyGarden/Scripts/LevelManager.cs
    rt: public void CheckWinLose()
    main: if (!JellyGarden.Scripts.Story.StoryPrePlay.Open())
    absent-rt: something that must NOT be in royal-throne
    ```

* ``path:`` sets the repo path for the lines that follow (may repeat).
* ``rt:``   the stripped text must equal at least one stripped line of
            ``origin/royal-throne:<path>``.
* ``main:`` same, against ``origin/main:<path>``.
* ``absent-rt:`` / ``absent-main:`` the text must NOT occur on any line.

A line that matches more than once is reported as AMBIGUOUS (still a pass,
because anchors are always given together with their method name).

Read-only: uses ``git show`` only, never the working tree, so it is safe to
run while Unity holds the project.

Usage:
    python3 tools/port_merge_spec_check.py [spec.md ...]
Exit status 1 when any anchor is missing.
"""
import pathlib
import re
import subprocess
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent
SPECS = ROOT / "docs" / "port" / "merge-specs"
REFS = {"rt": "origin/royal-throne", "main": "origin/main"}
_cache = {}


def blob(ref, path):
    key = (ref, path)
    if key not in _cache:
        try:
            out = subprocess.run(
                ["git", "show", ref + ":" + path],
                cwd=ROOT, check=True, capture_output=True,
            ).stdout.decode("utf-8-sig", errors="replace")
        except subprocess.CalledProcessError:
            out = None
        _cache[key] = None if out is None else [l.strip() for l in out.splitlines()]
    return _cache[key]


def check(spec):
    text = spec.read_text(encoding="utf-8")
    blocks = re.findall(r"```anchors\n(.*?)```", text, flags=re.S)
    fails = ambiguous = total = 0
    for block in blocks:
        path = None
        for raw in block.splitlines():
            if not raw.strip():
                continue
            kind, _, value = raw.partition(":")
            kind, value = kind.strip(), value.strip()
            if kind == "path":
                path = value
                continue
            negate = kind.startswith("absent-")
            side = kind[len("absent-"):] if negate else kind
            if side not in REFS or path is None:
                print("  ? %s: unparsable line %r" % (spec.name, raw))
                fails += 1
                continue
            total += 1
            lines = blob(REFS[side], path)
            if lines is None:
                print("  FAIL %s: %s:%s does not exist" % (spec.name, REFS[side], path))
                fails += 1
                continue
            if negate:
                hits = sum(1 for l in lines if value in l)
                if hits:
                    print("  FAIL %s: [%s] expected absent but found %dx: %s" % (spec.name, side, hits, value))
                    fails += 1
                continue
            hits = sum(1 for l in lines if l == value)
            if hits == 0:
                print("  FAIL %s: [%s %s] anchor not found: %s" % (spec.name, side, path.split("/")[-1], value))
                fails += 1
            elif hits > 1:
                ambiguous += 1
    return total, fails, ambiguous, len(blocks)


def main(argv):
    if any(a in ("-h", "--help") for a in argv):
        print(__doc__)
        return 0
    specs = [pathlib.Path(a) for a in argv] or sorted(SPECS.glob("*.md"))
    grand_total = grand_fail = 0
    for spec in specs:
        total, fails, ambiguous, nblocks = check(spec)
        grand_total += total
        grand_fail += fails
        if nblocks == 0:
            print("%-26s no anchors block" % spec.name)
            continue
        status = "OK" if fails == 0 else "FAIL"
        print("%-26s %-4s %3d anchors, %d missing, %d ambiguous" % (spec.name, status, total, fails, ambiguous))
    print("TOTAL %d anchors, %d missing" % (grand_total, grand_fail))
    return 1 if grand_fail else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
