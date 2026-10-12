#!/usr/bin/env python3
"""Story retirement guard for the royal-throne level port (plan Task 3.7).

docs/port/RETIRE.md is the map; this checks the tree against it. Two modes:

  scan (default)  Reads the WORKING TREE. Fails while any non-retired script still
                  names a retired type or a StoryLevelHud.Soft member, while a
                  scene/prefab/asset still serialises a retired MonoBehaviour (by
                  the GUID its .meta had on --base), or while the branch has
                  deleted or renamed a visual asset since --base. Retired files
                  still on disk are listed as TODO, not failures, unless --strict.

  --typecheck     Copies every Assets/**/*.cs (and the embedded match3-vfx package)
                  from --source (a git ref, default HEAD; or "worktree") into a temp
                  directory OUTSIDE the project, optionally deletes the retired set
                  (--simulate-delete), and typechecks it as one assembly with
                  `dotnet build`, using Assembly-CSharp-Editor.csproj for references
                  and defines. Prints errors grouped by file. Writes nothing into
                  the project (obj/bin live in the temp directory).

    python3 tools/port_retire_check.py
    python3 tools/port_retire_check.py --strict
    python3 tools/port_retire_check.py --typecheck --source HEAD --simulate-delete
    python3 tools/port_retire_check.py --typecheck --source worktree

Kit-core files are replaced wholesale by royal-throne's (plan section 4), so their
hits are reported for information only and never fail the scan.

Heuristic, not a compiler: comments and string literals are blanked before the
word match, so a mention in a comment is not a reference.
"""

import argparse
import collections
import os
import re
import shutil
import subprocess
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
STORY = "Assets/JellyGarden/Scripts/Story/"

# Plan section 5 "Retired".
PLAN_RETIRED = [STORY + n + ".cs" for n in (
    "StoryOriginalBoosters StoryOriginalBoostersValidation StoryOriginalTnt "
    "StoryOriginalTntPiece StoryOriginalTntValidation StoryOriginalColorBall "
    "StoryOriginalMotion StoryOriginalChargedTarget StoryOriginalPiece "
    "StoryRocketVfx StoryRocketSweep StoryRocketFlight StoryRocketVariant "
    "StoryRocketValidation StoryBombVfx StoryBombVariant StoryColorBombVfx "
    "StoryBlastVfx StoryShatterVfx StoryPunchVfx StoryPopVfx StoryRoyalVfx "
    "StoryExplosionProbe StoryVfxLab StoryVfxVariant StoryFinishVfx "
    "StoryBoardScale StoryBoardCentre StoryPixelSnap StorybookBoardLayout "
    "StoryRoyalBoardFrame StorybookRocketFacing StoryLevelHud.Soft "
    "StoryRoyalBoardValidation StoryPowerupTest").split()] + [
    STORY + "Editor/RocketValidationBridge.cs",
    "Assets/Editor/RocketFlameGapCheck.cs",
]

# Added by docs/port/RETIRE.md section 3 (each has no user left once the plan
# list is gone, or tests only what the plan list retires).
EXTRA_RETIRED = [
    STORY + "KingLeoVfxHost.cs",
    STORY + "StoryVfxFlags.cs",
    STORY + "StorybookBoardReview.cs",
    STORY + "Editor/StoryBoardBorderTests.cs",
]

KIT_CORE = {"Assets/JellyGarden/Scripts/" + p for p in (
    "LevelManager.cs", "Item.cs", "Square.cs", "System/CombineManager.cs",
    "AI.cs", "InputManager.cs")}

# Top-level types declared by the retired files (nested types are reached
# through these names). StoryLevelHud is NOT here: only its .Soft partial goes.
RETIRED_TYPES = (
    "StoryOriginalBoosters StoryOriginalBoostersHost StoryOriginalBoostersValidation "
    "StoryOriginalTnt StoryOriginalTntHost StoryOriginalTntPiece StoryOriginalTntValidation "
    "StoryOriginalColorBall StoryOriginalColorBallHost StoryOriginalMotion StoryOriginalFall "
    "StoryOriginalChargedTarget StoryOriginalPiece StoryRocketVfx StoryRocketSweep "
    "StoryRocketFlight RocketVariant StoryRocketVariant RocketPlume StoryRocketValidation "
    "StoryBombVfx BombVariant StoryBombVariant BombBlast StoryColorBombVfx StoryBlastVfx "
    "StoryShatterVfx StoryPunchVfx StoryPopVfx StoryRoyalVfx StoryExplosionProbe StoryVfxLab "
    "VfxVariant StoryVfxVariant ShatterPhysics StoryFinishVfx StoryBoardScale StoryBoardCentre "
    "StoryPixelSnap StorybookBoardLayout StoryRoyalBoardFrame StorybookRocketFacing "
    "StoryRoyalBoardValidation StoryPowerupTest RocketValidationBridge RocketFlameGapCheck").split()
EXTRA_TYPES = "KingLeoVfxHost StoryVfxFlags StorybookBoardReview StoryBoardBorderTests".split()

# Members that live only in StoryLevelHud.Soft.cs (checked inside StoryLevelHud.cs too).
SOFT_MEMBERS = (
    "BuildSoftHud PlaceSoft LayoutSoft RefreshSoft UseSoftBoost _softRoot _softCard "
    "_softTray _softCrest _royalPause _royalMovesInset _royalExtra _softProgress _softBoosts "
    "_softBoostArt _softBoostCounts _softTypes GoalRowWidth GoalRowHeight _softUnit "
    "_softInset").split()

VISUAL_EXTENSIONS = (".anim", ".controller", ".overrideController", ".prefab", ".png",
                     ".jpg", ".jpeg", ".mat", ".shader", ".shadergraph")
SERIALISED = (".unity", ".prefab", ".asset", ".controller", ".playable")


def git(*args):
    return subprocess.run(["git", "-C", ROOT] + list(args), capture_output=True,
                          text=True).stdout


def blank(src):
    """Comments and string/char literals replaced by spaces; newlines kept."""
    out, i, n = [], 0, len(src)
    while i < n:
        c = src[i]
        if src.startswith("//", i):
            j = src.find("\n", i)
            j = n if j < 0 else j
            out.append(" " * (j - i)); i = j
        elif src.startswith("/*", i):
            j = src.find("*/", i + 2)
            j = n if j < 0 else j + 2
            out.append(re.sub(r"[^\n]", " ", src[i:j])); i = j
        elif c == '"' or (c == "@" and i + 1 < n and src[i + 1] == '"') or \
                (c == "$" and i + 1 < n and src[i + 1] == '"'):
            verbatim = c == "@"
            j = i + (2 if c in "@$" else 1)
            while j < n:
                if verbatim and src.startswith('""', j):
                    j += 2; continue
                if not verbatim and src[j] == "\\":
                    j += 2; continue
                if src[j] == '"':
                    j += 1; break
                if src[j] == "\n" and not verbatim:
                    break
                j += 1
            out.append(re.sub(r"[^\n]", " ", src[i:j])); i = j
        elif c == "'" and i + 2 < n and (src[i + 2] == "'" or src[i + 1] == "\\"):
            # 'x' or an escape such as '\n', '\'', 'é'.
            j = (src.find("'", i + 3) + 1) if src[i + 1] == "\\" else i + 3
            j = n if j <= i else j
            out.append(" " * (j - i)); i = j
        else:
            out.append(c); i += 1
    return "".join(out)


def cs_files():
    for base in ("Assets",):
        for dp, dn, fn in os.walk(os.path.join(ROOT, base)):
            for f in fn:
                if f.endswith(".cs"):
                    p = os.path.join(dp, f)
                    yield os.path.relpath(p, ROOT), p


def meta_guid(ref, path):
    text = git("show", "%s:%s.meta" % (ref, path))
    m = re.search(r"guid: ([0-9a-f]{32})", text)
    return m.group(1) if m else None


def scan(args):
    retired = PLAN_RETIRED + ([] if args.plan_only else EXTRA_RETIRED)
    types = RETIRED_TYPES + ([] if args.plan_only else EXTRA_TYPES)
    retired_set = set(retired)
    word = re.compile(r"\b(" + "|".join(types) + r")\b")
    soft = re.compile(r"\b(" + "|".join(SOFT_MEMBERS) + r")\b")
    failures, info, todo = [], [], []

    present = [p for p in retired if os.path.exists(os.path.join(ROOT, p))]
    for p in present:
        todo.append("still on disk: " + p)

    for rel, full in cs_files():
        if rel in retired_set:
            continue
        with open(full, encoding="utf-8-sig", errors="replace") as fh:
            raw = fh.read()
        code = blank(raw).split("\n")
        lines = raw.split("\n")
        for i, line in enumerate(code, 1):
            hits = set(word.findall(line))
            if rel == STORY + "StoryLevelHud.cs" or rel.endswith("/StoryLevelHud.cs"):
                hits |= set(soft.findall(line))
            if not hits:
                continue
            msg = "%s:%d [%s] %s" % (rel, i, ",".join(sorted(hits)), lines[i - 1].strip()[:140])
            (info if rel in KIT_CORE else failures).append(msg)

    guids = {}
    for p in retired:
        g = meta_guid(args.base, p) or meta_guid("HEAD", p)
        if g:
            guids[g] = p
    if guids:
        pat = re.compile("|".join(guids))
        for dp, dn, fn in os.walk(os.path.join(ROOT, "Assets")):
            for f in fn:
                if not f.endswith(SERIALISED):
                    continue
                full = os.path.join(dp, f)
                try:
                    with open(full, encoding="utf-8", errors="replace") as fh:
                        text = fh.read()
                except OSError:
                    continue
                for g in set(pat.findall(text)):
                    failures.append("%s serialises retired script %s (guid %s)"
                                    % (os.path.relpath(full, ROOT), guids[g], g))

    for line in git("diff", "--name-status", args.base, "--").splitlines():
        parts = line.split("\t")
        if len(parts) >= 2 and parts[-1].endswith(VISUAL_EXTENSIONS) and parts[0][:1] in "DR":
            failures.append("visual asset %s since %s: %s" % (
                "deleted" if parts[0].startswith("D") else "renamed", args.base, parts[-1]))
    for line in git("status", "--porcelain").splitlines():
        if line[:2].strip().startswith("D") and line[3:].endswith(VISUAL_EXTENSIONS):
            failures.append("visual asset deleted in the working tree: " + line[3:])

    for msg in todo:
        print("TODO  " + msg)
    for msg in info:
        print("INFO  kit-core (replaced by royal-throne's): " + msg)
    for msg in failures:
        print("FAIL  " + msg)
    bad = len(failures) + (len(todo) if args.strict else 0)
    print("\n%d failure(s), %d retired file(s) still present, %d kit-core hit(s) (info)."
          % (len(failures), len(todo), len(info)))
    return 1 if bad else 0


def typecheck(args):
    csproj = os.path.join(ROOT, "Assembly-CSharp-Editor.csproj")
    if not os.path.exists(csproj):
        print("Assembly-CSharp-Editor.csproj is missing: regenerate project files in Unity.")
        return 2
    work = tempfile.mkdtemp(prefix="retire-typecheck-")
    src = os.path.join(work, "src")
    roots = ["Assets", "Packages/com.royalthrone.match3-vfx/Runtime",
             "Packages/com.royalthrone.match3-vfx/Editor"]
    count = 0
    if args.source == "worktree":
        for base in roots:
            for dp, dn, fn in os.walk(os.path.join(ROOT, base)):
                if "~" in dp:
                    continue
                for f in fn:
                    if f.endswith(".cs"):
                        s = os.path.join(dp, f)
                        d = os.path.join(src, os.path.relpath(s, ROOT))
                        os.makedirs(os.path.dirname(d), exist_ok=True)
                        shutil.copyfile(s, d)
                        count += 1
    else:
        items = []
        for line in git("ls-tree", "-r", args.source, "--", *roots).splitlines():
            meta, path = line.split("\t", 1)
            if path.endswith(".cs") and "~" not in path:
                items.append((meta.split()[2], path))
        proc = subprocess.Popen(["git", "-C", ROOT, "cat-file", "--batch"],
                                stdin=subprocess.PIPE, stdout=subprocess.PIPE)
        for sha, path in items:
            proc.stdin.write((sha + "\n").encode()); proc.stdin.flush()
            size = int(proc.stdout.readline().split()[2])
            data = proc.stdout.read(size); proc.stdout.read(1)
            d = os.path.join(src, path)
            os.makedirs(os.path.dirname(d), exist_ok=True)
            with open(d, "wb") as fh:
                fh.write(data)
            count += 1
        proc.stdin.close(); proc.wait()
    if args.simulate_delete:
        for p in PLAN_RETIRED + ([] if args.plan_only else EXTRA_RETIRED):
            target = os.path.join(src, p)
            if os.path.exists(target):
                os.remove(target)

    with open(csproj, encoding="utf-8-sig") as fh:
        text = fh.read()
    text = re.sub(r'[ \t]*<Compile Include="[^"]*" />\r?\n', "", text)
    text = re.sub(r'[ \t]*<ProjectReference Include="[^"]*">.*?</ProjectReference>\r?\n', "",
                  text, flags=re.S)
    text = re.sub(r'[ \t]*<ProjectReference Include="[^"]*" />\r?\n', "", text)
    text = re.sub(r"<HintPath>(?!/)([^<]+)</HintPath>",
                  lambda m: "<HintPath>" + os.path.join(ROOT, m.group(1)) + "</HintPath>", text)
    text = text.replace("<AssemblyName>Assembly-CSharp-Editor</AssemblyName>",
                        "<AssemblyName>RetireCheck</AssemblyName>")
    text = re.sub(r"<OutputPath>[^<]*</OutputPath>", "<OutputPath>%s/bin/</OutputPath>" % work, text)
    text = re.sub(r"<BaseIntermediateOutputPath>[^<]*</BaseIntermediateOutputPath>",
                  "<BaseIntermediateOutputPath>%s/obj/</BaseIntermediateOutputPath>" % work, text)
    text = text.replace("<NoWarn>0169;USG0001</NoWarn>",
                        "<NoWarn>0169;USG0001;CS0414;CS0649;CS0618;CS0162;CS0219;CS0168;CS0067"
                        "</NoWarn><AllowUnsafeBlocks>true</AllowUnsafeBlocks>")
    end = text.rindex("</Project>")
    text = text[:end] + '  <ItemGroup>\n    <Compile Include="src/**/*.cs" />\n  </ItemGroup>\n' \
        + text[end:]
    with open(os.path.join(work, "RetireCheck.csproj"), "w", encoding="utf-8") as fh:
        fh.write(text)

    out = subprocess.run(["dotnet", "build", "RetireCheck.csproj", "-v", "q", "--nologo",
                          "--no-incremental"], cwd=work, capture_output=True, text=True)
    errors = collections.OrderedDict()
    for line in (out.stdout + out.stderr).splitlines():
        m = re.search(r"/src/(.+?)\((\d+),\d+\): error (CS\d+): (.*?) \[", line)
        if m:
            errors[(m.group(1), int(m.group(2)), m.group(3), m.group(4))] = True
    by_file = collections.defaultdict(list)
    for (path, line, code, msg) in errors:
        by_file[path].append((line, code, msg))
    print("typecheck: %d files from %s%s; %d error(s)" % (
        count, args.source, " minus the retired set" if args.simulate_delete else "", len(errors)))
    for path in sorted(by_file, key=lambda p: (p not in KIT_CORE, p)):
        tag = "kit-core" if path in KIT_CORE else "KEPT"
        print("  [%s] %s: %d" % (tag, path, len(by_file[path])))
        if path not in KIT_CORE or args.verbose:
            for line, code, msg in sorted(by_file[path]):
                print("      %d %s %s" % (line, code, msg[:150]))
    if not args.keep:
        shutil.rmtree(work, ignore_errors=True)
    else:
        print("kept " + work)
    kept_errors = [p for p in by_file if p not in KIT_CORE]
    return 1 if kept_errors else 0


def main():
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("--base", default="HEAD",
                    help="ref whose .meta GUIDs and visual assets are the baseline (default HEAD)")
    ap.add_argument("--strict", action="store_true", help="retired files still present fail")
    ap.add_argument("--plan-only", action="store_true",
                    help="retired set = plan section 5 only (without RETIRE.md additions)")
    ap.add_argument("--typecheck", action="store_true")
    ap.add_argument("--source", default="HEAD", help="git ref or 'worktree' (typecheck)")
    ap.add_argument("--simulate-delete", action="store_true",
                    help="typecheck: drop the retired set from the copy first")
    ap.add_argument("--verbose", action="store_true", help="typecheck: list kit-core errors too")
    ap.add_argument("--keep", action="store_true", help="typecheck: keep the temp directory")
    args = ap.parse_args()
    return typecheck(args) if args.typecheck else scan(args)


if __name__ == "__main__":
    sys.exit(main())
