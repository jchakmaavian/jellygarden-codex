#!/usr/bin/env python3
"""C# type-reference closure over a git ref (royal-throne port, plan Task 1.1).

Reads every Assets/**/*.cs on a ref with `git cat-file --batch` (never the
working tree), indexes top-level type declarations per namespace, and follows
identifier references from a seed set. Files that match a --cut pattern are not
followed; each reference into them is reported as `cut` with file:line.

    python3 tools/port_code_closure.py                    # default seeds/cuts
    python3 tools/port_code_closure.py --json out.json    # machine-readable
    python3 tools/port_code_closure.py --why Assets/X.cs  # show the path to X

Heuristic, not a compiler: comments and string literals are blanked (code inside
$"{...}" kept); a reference to type T from file G counts when T's namespace is
G's own (or a parent), is imported by `using`, is global, or T is written
qualified (`Ns.T`) or aliased (`using A = Ns.T`). Nested types resolve through
their outer type. Reflection (`Type.GetType("...")`) is reported separately.
"""
import argparse
import fnmatch
import json
import re
import subprocess
import sys
import threading
from collections import defaultdict, deque

REF = "origin/royal-throne"
MAIN = "origin/main"
BASE = "a82c4df33a929614fd4d7095b59aa26cb0274a8b"   # common kit ancestor

JS = "Assets/JellyGarden/Scripts/"
DEFAULT_SEEDS = [
    JS + "LevelManager.cs", JS + "Item.cs", JS + "Square.cs", JS + "AI.cs",
    JS + "InputManager.cs", JS + "System/CombineManager.cs",
    JS + "GUI/BoostShop.cs", JS + "GUI/BoostIcon.cs", JS + "Targets/*.cs",
    JS + "Juice/*.cs",
    "Assets/Boosters/Propeller/V5/Scripts/*.cs",
    "Assets/Boosters/ColorBall/V4/Scripts/*.cs",
    "Assets/Boosters/ColorBall/V5/Scripts/*.cs",
    "Assets/ReferenceGameplay/Scripts/*.cs",
    "Assets/ReferenceGameplay/Tools/Scripts/*.cs",
    "Assets/ReferenceGameplay/Home/Scripts/ReferenceWinFinish.cs",
    JS + "RoyalSave.cs", JS + "RoyalQaService.cs",
    # import postprocessors that shape the imported art (editor only)
    "Assets/ReferenceGameplay/Editor/ReferenceSpriteImporter.cs",
    JS + "Editor/RoyalGameplayArtImporter.cs",
]
DEFAULT_TEST_SEEDS = [
    JS + "Editor/Royal*Checks.cs",
    "Assets/ReferenceGameplay/Editor/Royal*Checks.cs",
    "Assets/Boosters/Propeller/V5/Scripts/Editor/PropellerV5Checks.cs",
    "Assets/Boosters/ColorBall/V4/Scripts/Editor/ColorBallV4Checks.cs",
    "Assets/Boosters/ColorBall/V5/Scripts/Editor/ColorBallV5Checks.cs",
    JS + "Juice/RoyalRegressionRunner.cs",
]
# Excluded areas (plan §2 "Out of scope", D2, D4, D8). A reference into one of
# these is reported, not followed. Order matters only for the label.
DEFAULT_CUTS = [
    ("home/title/splash/loading", [
        JS + "GUI/RoyalHome*", JS + "GUI/RoyalTitle*", JS + "GUI/RoyalArea*",
        JS + "GUI/AreaMotion/*", JS + "GUI/RoyalLevelCard*", JS + "GUI/RoyalLogoHalo*",
        JS + "GUI/RoyalLottiePlayer*", JS + "GUI/RoyalFeaturePage*", JS + "GUI/RoyalLessonDemo*",
        JS + "GUI/RoyalScreenCover*", JS + "GUI/RoyalNotice*", JS + "GUI/RoyalWaterGlow*",
        "Assets/ReferenceGameplay/Home/Scripts/ReferenceHome*",
        JS + "Editor/RoyalHome*", JS + "Editor/RoyalTitle*", JS + "Editor/RoyalArea*",
        "Assets/ReferenceGameplay/Editor/ReferenceHome*",
    ]),
    ("win card/RoyalWinSequence", [
        JS + "Juice/RoyalWinSequence.cs", JS + "GUI/RoyalWin*", JS + "GUI/RoyalRewardCard*",
        "Assets/ReferenceGameplay/WinVFX/*",
    ]),
    ("RoyalEconomy/shop", [
        JS + "Juice/RoyalEconomy.cs", JS + "RoyalStore.cs", JS + "GUI/RoyalShopPage*",
        # coin presentation: only RoyalWinSequence (cut) uses these
        "Assets/ReferenceGameplay/Scripts/ReferenceCoinAnimation.cs",
        "Assets/ReferenceGameplay/Scripts/ReferenceCoinParticles.cs",
    ]),
    ("music/ambience", [JS + "Juice/RoyalReviewAudioCapture.cs"]),
    ("settings/profile/leaderboard/cards pages", [
        JS + "GUI/RoyalSettings*", JS + "GUI/RoyalProfile*", JS + "RoyalProfile.cs",
        JS + "GUI/RoyalLeaderboard*", JS + "GUI/RoyalCards*", JS + "GUI/RoyalCardKit*",
        JS + "GUI/RoyalCardMotion*", JS + "GUI/RoyalVolumeSlider*",
    ]),
    ("QA mod-menu UI", [
        JS + "RoyalModMenu.cs", JS + "RoyalQaMenu.cs", JS + "GUI/RoyalMenuSkin*",
        JS + "GUI/RoyalMenuPresenter*", JS + "GUI/RoyalBackButton*",
    ]),
    ("reference campaign (D8)", [
        JS + "RoyalReferenceCampaign*", JS + "RoyalCampaign*", JS + "Editor/RoyalCampaign*",
        "Assets/ReferenceGameplay/Editor/ReferenceLevelChecks.cs",
        "Assets/ReferenceGameplay/Art/Editor/ReferenceBlockerImporter.cs",
    ]),
    ("power tutorial", [JS + "GUI/Tutorial/RoyalPowerTutorial.cs"]),
    ("KingVanguard (D2)", ["Assets/Characters/*"]),
    ("Android/build tooling", [
        JS + "Editor/RoyalAndroidBuild.cs", JS + "Editor/RoyalThroneAndroidBuild.cs",
        JS + "Editor/RoyalOfflineAndroidManifest.cs", JS + "Editor/RoyalAppIconBuilder.cs",
        JS + "GUI/RoyalAndroidSystemBars.cs", JS + "Editor/RoyalHomeScreenBuilder.cs",
    ]),
    ("old booster versions", [
        "Assets/Boosters/Propeller/V2/*", "Assets/Boosters/Propeller/V3/*",
        "Assets/Boosters/Propeller/V4/*", "Assets/Boosters/ColorBall/V2/*",
        "Assets/Boosters/ColorBall/V3/*",
    ]),
]

# Kit files whose main version is kept (plan §4): their royal-throne-only
# references are reported, not followed.
KEEP_MAIN = {JS + "InitScript.cs", JS + "GUI/AnimationManager.cs", JS + "GUI/MenuManager.cs"}

DECL = re.compile(r"\b(?:class|struct|interface|enum|record)\s+([A-Za-z_]\w*)|\bdelegate\s+[\w<>\[\],.\s]+?\s+([A-Za-z_]\w*)\s*[<(]")
NS = re.compile(r"\bnamespace\s+([\w.]+)\s*(;|\{)")
USING = re.compile(r"^\s*(?:global\s+)?using\s+(static\s+)?(?:(\w+)\s*=\s*)?([\w.]+(?:<[^;]*>)?)\s*;", re.M)
IDENT = re.compile(r"[A-Za-z_]\w*")
REFLECT = re.compile(r"(?:Type\.GetType|GetType|FindType|AddComponent|GetComponent)\s*\(\s*\"([\w.]+)")


def git(*args):
    return subprocess.run(["git", *args], check=True, capture_output=True).stdout


def read_blobs(ref, paths):
    """Return {path: text} using one git cat-file --batch process."""
    proc = subprocess.Popen(["git", "cat-file", "--batch"], stdin=subprocess.PIPE, stdout=subprocess.PIPE)
    req = "".join(f"{ref}:{p}\n" for p in paths).encode()

    def feed():  # a writer thread: a full stdout pipe must not block our stdin write
        proc.stdin.write(req)
        proc.stdin.close()
    threading.Thread(target=feed, daemon=True).start()
    out = {}
    for p in paths:
        header = proc.stdout.readline().split()
        if len(header) < 3 or header[1] == b"missing":
            continue
        size = int(header[2])
        data = proc.stdout.read(size)
        proc.stdout.read(1)
        out[p] = data.decode("utf-8", "replace")
    proc.stdout.close()
    proc.wait()
    return out


def strip_code(src):
    """Blank comments/strings, keep newlines and code inside $"{...}"."""
    out = []
    i, n = 0, len(src)
    while i < n:
        c = src[i]
        nxt = src[i + 1] if i + 1 < n else ""
        if c == "/" and nxt == "/":
            j = src.find("\n", i)
            j = n if j < 0 else j
            out.append(" " * (j - i)); i = j; continue
        if c == "/" and nxt == "*":
            j = src.find("*/", i + 2)
            j = n if j < 0 else j + 2
            out.append(re.sub(r"[^\n]", " ", src[i:j])); i = j; continue
        if c == "'" :
            j = i + 1
            while j < n and src[j] != "'" and src[j] != "\n":
                j += 2 if src[j] == "\\" else 1
            j = min(n, j + 1)
            out.append(" " * (j - i)); i = j; continue
        if c in "$@" or c == '"':
            m = re.match(r'(\$@|@\$|\$|@)?"', src[i:i + 3])
            if m:
                prefix = m.group(1) or ""
                verbatim = "@" in prefix
                interp = "$" in prefix
                j = i + len(m.group(0))
                buf = [" " * (j - i)]
                depth = 0
                while j < n:
                    ch = src[j]
                    if depth == 0:
                        if ch == '"':
                            if verbatim and j + 1 < n and src[j + 1] == '"':
                                buf.append("  "); j += 2; continue
                            buf.append(" "); j += 1; break
                        if ch == "\\" and not verbatim:
                            buf.append("  "); j += 2; continue
                        if interp and ch == "{":
                            if j + 1 < n and src[j + 1] == "{":
                                buf.append("  "); j += 2; continue
                            depth = 1; buf.append(" "); j += 1; continue
                        buf.append("\n" if ch == "\n" else " "); j += 1
                    else:
                        if ch == "{": depth += 1
                        elif ch == "}":
                            depth -= 1
                            if depth == 0:
                                buf.append(" "); j += 1; continue
                        elif ch == '"':
                            # nested string inside interpolation: skip it
                            k = j + 1
                            while k < n and src[k] != '"':
                                k += 2 if src[k] == "\\" else 1
                            buf.append(" " * (k + 1 - j)); j = k + 1; continue
                        buf.append(ch); j += 1
                out.append("".join(buf)); i = j; continue
        out.append(c); i += 1
    return "".join(out)


def strip_code_keep_strings(src):
    """Blank comments only (string literals kept); newlines preserved."""
    out = []
    i, n = 0, len(src)
    while i < n:
        c = src[i]
        nxt = src[i + 1] if i + 1 < n else ""
        if c == "/" and nxt == "/":
            j = src.find("\n", i)
            j = n if j < 0 else j
            out.append(" " * (j - i)); i = j; continue
        if c == "/" and nxt == "*":
            j = src.find("*/", i + 2)
            j = n if j < 0 else j + 2
            out.append(re.sub(r"[^\n]", " ", src[i:j])); i = j; continue
        if c == "'":
            j = i + 1
            while j < n and src[j] != "'" and src[j] != "\n":
                j += 2 if src[j] == "\\" else 1
            j = min(n, j + 1)
            out.append(src[i:j]); i = j; continue
        if c == '"' or (c in "$@" and nxt in '"$@'):
            m = re.match(r'(\$@|@\$|\$|@)?"', src[i:i + 3])
            if m:
                verbatim = "@" in (m.group(1) or "")
                j = i + len(m.group(0))
                while j < n:
                    if src[j] == "\\" and not verbatim:
                        j += 2; continue
                    if src[j] == '"':
                        if verbatim and j + 1 < n and src[j + 1] == '"':
                            j += 2; continue
                        j += 1; break
                    if src[j] == "\n" and not verbatim:
                        break
                    j += 1
                out.append(src[i:j]); i = j; continue
        out.append(c); i += 1
    return "".join(out)


class CsFile:
    def __init__(self, path, text):
        self.path = path
        self.text = text
        self.code = strip_code(text)
        self.lines = self.code.split("\n")
        self.namespaces = set()
        self.usings = set()
        self.aliases = {}
        self.static_usings = set()
        self.top_types = []   # (name, namespace)
        self.parse()
        self.idents = set(IDENT.findall(self.code))
        self.editor = "/Editor/" in path

    def parse(self):
        code = self.code
        for m in USING.finditer(code):
            static, alias, target = m.groups()
            target = re.sub(r"<.*", "", target)
            if alias:
                self.aliases[alias] = target
            elif static:
                self.static_usings.add(target)
            else:
                self.usings.add(target)
        # walk braces to know the depth of each declaration and its namespace
        ns_stack = []  # (name, depth at which it opened)
        file_ns = None
        depth = 0
        type_depth_marks = []  # depth at which a type body opens
        pending_ns = None
        pending_type = False
        tokens = re.finditer(r"\bnamespace\s+[\w.]+\s*[;{]|\b(?:class|struct|interface|enum|record)\s+[A-Za-z_]\w*|\bdelegate\b[^;{(]*\(|[{};]", code)
        for t in tokens:
            s = t.group(0)
            if s.startswith("namespace"):
                name = re.match(r"namespace\s+([\w.]+)", s).group(1)
                if s.endswith(";"):
                    file_ns = name
                else:
                    depth += 1
                    ns_stack.append((name, depth))
                self.namespaces.add(name)
                continue
            if s == "{":
                depth += 1
                if pending_type:
                    type_depth_marks.append(depth)
                    pending_type = False
                continue
            if s == "}":
                if type_depth_marks and type_depth_marks[-1] == depth:
                    type_depth_marks.pop()
                if ns_stack and ns_stack[-1][1] == depth:
                    ns_stack.pop()
                depth -= 1
                continue
            if s == ";":
                pending_type = False
                continue
            if s.startswith("delegate"):
                m = re.search(r"([A-Za-z_]\w*)\s*(?:<[^>]*>)?\s*\($", s.strip())
                if m and not type_depth_marks and not re.search(r"delegate\s*\($", s):
                    ns = ".".join(n for n, _ in ns_stack) or file_ns or ""
                    self.top_types.append((m.group(1), ns))
                continue
            name = s.split()[-1]
            if not type_depth_marks:
                ns = ".".join(n for n, _ in ns_stack) or file_ns or ""
                self.top_types.append((name, ns))
            pending_type = True
        if file_ns:
            self.namespaces.add(file_ns)

    def visible_namespaces(self):
        vis = {""} | self.usings
        for ns in self.namespaces:
            parts = ns.split(".")
            for k in range(1, len(parts) + 1):
                vis.add(".".join(parts[:k]))
            # nested namespace blocks: "A" then "B" -> "A.B" handled by parse joining
        return vis

    def lines_with(self, ident, limit=3):
        pat = re.compile(r"\b" + re.escape(ident) + r"\b")
        hits = [i + 1 for i, ln in enumerate(self.lines) if pat.search(ln)]
        return hits[:limit], len(hits)


def match_any(path, patterns):
    return any(fnmatch.fnmatchcase(path, p) for p in patterns)


def cut_label(path, cuts):
    for label, pats in cuts:
        if match_any(path, pats):
            return label
    return None


def blob_map(ref, root="Assets"):
    out = {}
    for line in git("ls-tree", "-r", "-l", ref, "--", root).decode("utf-8", "replace").splitlines():
        meta, path = line.split("\t", 1)
        _, kind, oid, size = meta.split()
        if kind == "blob":
            out[path] = (oid, int(size) if size != "-" else 0)
    return out


def kit_status(path, rt, base):
    if path not in base:
        return "new"
    return "kit-same" if rt.get(path, (None,))[0] == base[path][0] else "kit-changed"


def main_status(path, rt, main):
    if path not in main:
        return "absent"
    return "identical" if main[path][0] == rt.get(path, (None,))[0] else "different"


def build(ref):
    names = git("ls-tree", "-r", "--name-only", ref, "--", "Assets").decode().splitlines()
    cs = [p for p in names if p.endswith(".cs")]
    blobs = read_blobs(ref, cs)
    files = {p: CsFile(p, t) for p, t in blobs.items()}
    index = defaultdict(list)  # name -> [(path, ns)]
    for f in files.values():
        for name, ns in f.top_types:
            index[name].append((f.path, ns))
    return files, index


def resolve_refs(f, files, index):
    """Return {target_path: [type names]} referenced by file f."""
    vis = f.visible_namespaces()
    out = defaultdict(set)
    for ident in sorted(f.idents):
        cands = index.get(ident)
        if not cands:
            continue
        for path, ns in cands:
            if path == f.path:
                continue
            ok = ns in vis
            if not ok and ns:
                last = ns.split(".")[-1]
                if re.search(r"\b" + re.escape(last) + r"\s*\.\s*" + re.escape(ident) + r"\b", f.code):
                    ok = True
                elif any(a == ns + "." + ident or a == ns for a in f.aliases.values()):
                    ok = True
                elif ns + "." + ident in f.static_usings:
                    ok = True
            if ok:
                out[path].add(ident)
    # aliases like `using PlayerPrefs = RoyalThrone.Testing.RoyalSave;`
    for alias, target in f.aliases.items():
        tname = target.split(".")[-1]
        tns = ".".join(target.split(".")[:-1])
        for path, ns in index.get(tname, []):
            if ns == tns and path != f.path:
                out[path].add(tname)
    return out


def partial_names(files):
    """Type names declared `partial` in more than one file."""
    seen = defaultdict(set)
    for f in files.values():
        for m in re.finditer(r"\bpartial\s+(?:class|struct|interface)\s+(\w+)", f.code):
            seen[m.group(1)].add(f.path)
    return {k: v for k, v in seen.items() if len(v) > 1}


def closure(files, index, seeds, cuts, follow, stop_at=None):
    """BFS. `follow(path)` says whether a non-seed file's own references are followed."""
    stop_at = stop_at or set()
    partials = partial_names(files)
    included = {}
    parent = {}
    cut_hits = []
    not_followed = []   # (file, target, types) edges from files we do not follow
    q = deque()
    for s in seeds:
        if s in files and s not in included:
            included[s] = "seed"
            q.append(s)
    while q:
        g = q.popleft()
        refs = resolve_refs(files[g], files, index)
        if included[g] != "seed" and not follow(g):
            for tgt, types in sorted(refs.items()):
                if tgt not in included and tgt not in stop_at and not follow_is_kit(tgt):
                    not_followed.append((g, tgt, sorted(types)))
            continue
        for tgt, types in sorted(refs.items()):
            if tgt in included or tgt in stop_at:
                continue
            # a partial part in another file is not a dependency of the referrer;
            # it is compiled with the type wherever it lives
            only_partial = all(t in partials and tgt in partials[t] for t in types)
            label = cut_label(tgt, cuts)
            if label:
                if only_partial:
                    continue
                for t in sorted(types):
                    lines, total = files[g].lines_with(t)
                    cut_hits.append({"from": g, "lines": lines, "count": total, "type": t, "to": tgt, "area": label})
                continue
            included[tgt] = "ref"
            parent[tgt] = (g, sorted(types))
            q.append(tgt)
    return included, parent, cut_hits, not_followed


follow_is_kit = lambda p: False


def expand(patterns, files):
    out = []
    for p in patterns:
        out.extend(sorted(f for f in files if fnmatch.fnmatchcase(f, p)))
    seen = set()
    return [x for x in out if not (x in seen or seen.add(x))]


def compute(files, index, ref=REF):
    """Run both closures; returns the result dict printed by main()."""
    rt, mainm, base = blob_map(ref), blob_map(MAIN), blob_map(BASE)
    global follow_is_kit
    follow_is_kit = lambda p: kit_status(p, rt, base) == "kit-same"

    def follow(p):
        return p not in KEEP_MAIN and kit_status(p, rt, base) != "kit-same"
    cuts = DEFAULT_CUTS
    seeds = [s for s in expand(DEFAULT_SEEDS, files) if not cut_label(s, cuts)]
    seed_cut = [s for s in expand(DEFAULT_SEEDS, files) if cut_label(s, cuts)]
    runtime, rparent, rcuts, rnf = closure(files, index, seeds, cuts, follow)
    tseeds = [s for s in expand(DEFAULT_TEST_SEEDS, files) if not cut_label(s, cuts)]
    tseed_cut = [s for s in expand(DEFAULT_TEST_SEEDS, files) if cut_label(s, cuts)]
    tests, tparent, tcuts, tnf = closure(files, index, tseeds, cuts, follow, stop_at=set(runtime))
    tests = {k: v for k, v in tests.items() if k not in runtime}

    reflect = []
    for p in list(runtime) + list(tests):
        for m in REFLECT.finditer(files[p].text):
            reflect.append((p, files[p].text[:m.start()].count("\n") + 1, m.group(1)))
    return {
        "ref": ref, "n_seeds": len(seeds),
        "runtime": sorted(runtime), "runtime_kind": runtime, "runtime_parent": rparent,
        "tests": sorted(tests), "tests_kind": tests, "tests_parent": tparent,
        "seed_excluded": seed_cut, "test_seed_excluded": tseed_cut,
        "runtime_cuts": rcuts, "test_cuts": tcuts,
        "reflection": reflect,
        "not_followed": rnf + tnf,
        "partial_parts_cut": sorted(p for t, ps in partial_names(files).items() for p in ps
                                    if cut_label(p, cuts) and any(q in runtime for q in ps)),
        "status": {p: {"kit": kit_status(p, rt, base), "main": main_status(p, rt, mainm),
                       "size": rt.get(p, (None, 0))[1]} for p in list(runtime) + list(tests)},
    }


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--ref", default=REF)
    ap.add_argument("--json", help="write full result as JSON here")
    ap.add_argument("--why", help="print the reference chain that pulls this path in")
    args = ap.parse_args()

    files, index = build(args.ref)
    res = compute(files, index, args.ref)
    runtime, rparent = res["runtime_kind"], res["runtime_parent"]
    tests, tparent = res["tests_kind"], res["tests_parent"]
    rcuts, tcuts, rnf_tnf = res["runtime_cuts"], res["test_cuts"], res["not_followed"]
    seed_cut, tseed_cut, reflect = res["seed_excluded"], res["test_seed_excluded"], res["reflection"]
    cuts = DEFAULT_CUTS
    if args.why:
        par = {**rparent, **tparent}
        p = args.why
        while p in par:
            src, types = par[p]
            print(f"{p}  <-  {src}  via {', '.join(types)}")
            p = src
        print(f"{p}  ({'seed' if p in runtime or p in tests else 'not in closure'})")
        return

    if args.json:
        with open(args.json, "w") as fh:
            json.dump({k: v for k, v in res.items() if not k.endswith("_kind")}, fh, indent=1)
    def st(p):
        return f"{res['status'][p]['kit']}/{res['status'][p]['main']}"
    print(f"# runtime closure: {len(runtime)} files ({res['n_seeds']} seeds)  [kit-status/main-status]")
    for p in sorted(runtime):
        why = "seed" if runtime[p] == "seed" else "<- " + rparent[p][0].split("/")[-1] + " (" + ",".join(rparent[p][1][:3]) + ")"
        print(f"{p}\t{st(p)}\t{why}")
    print(f"\n# editor-test closure (beyond runtime): {len(tests)} files")
    for p in sorted(tests):
        why = "seed" if tests[p] == "seed" else "<- " + tparent[p][0].split("/")[-1] + " (" + ",".join(tparent[p][1][:3]) + ")"
        print(f"{p}\t{st(p)}\t{why}")
    print("\n# partial-class parts left out (their type is in the closure)")
    for p in res["partial_parts_cut"]:
        print(p, cut_label(p, cuts), sep="\t")
    print("\n# references NOT followed (from kit files whose main version is kept)")
    for g, tgt, types in rnf_tnf:
        print(f"{g}\t-> {tgt}\t({','.join(types)})")
    print("\n# seeds excluded by cut rules")
    for p in seed_cut + tseed_cut:
        print(p, cut_label(p, cuts), sep="\t")
    print("\n# cut references (runtime)")
    for c in rcuts:
        print(f"{c['from']}:{','.join(map(str, c['lines']))} ({c['count']}x)\t{c['type']}\t-> {c['to']}\t[{c['area']}]")
    print("\n# cut references (editor tests)")
    for c in tcuts:
        print(f"{c['from']}:{','.join(map(str, c['lines']))} ({c['count']}x)\t{c['type']}\t-> {c['to']}\t[{c['area']}]")
    print("\n# reflection-style string type references")
    for p, ln, s in reflect:
        print(f"{p}:{ln}\t{s}")


if __name__ == "__main__":
    sys.exit(main())
