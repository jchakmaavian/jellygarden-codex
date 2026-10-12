#!/usr/bin/env python3
"""Asset closure of the royal-throne in-level layer (plan Task 1.1, §5).

Reads only git objects (`git ls-tree` / `git cat-file --batch` on a ref), never
the working tree. Starting from seed paths it follows, transitively:

  (i)   Resources.Load / LoadAll / LoadAsync path arguments in .cs files.
        String concatenation is folded with `const`/`static readonly` string
        fields of the closure files; a dynamic tail turns the literal head into
        a prefix ("RoyalGameplay/Audio/" + name -> every key under that folder).
        Keys resolve against every */Resources/ folder in the tree, any extension.
        Any other string literal that is exactly an existing Resources key is
        followed too (catalog tables, name arrays). Text assets that are loaded
        (.json/.txt) are scanned the same way.
  (ii)  every `guid:` inside YAML assets (.prefab .mat .asset .controller .anim
        .overrideController .mask .spriteatlas .playable .fontsettings ...; .unity
        scenes are never followed) and inside the .meta of every included file
        (MonoImporter default references, ModelImporter external objects),
        mapped guid -> path through every .meta on the ref.
  (iii) Shader.Find("Name") in code, plus `UsePass`/`Fallback`/`#include` inside
        shaders, mapped through each .shader's `Shader "Name"` line.

Paths matching a --cut rule are not entered; every edge into them is reported
with its source. Scripts referenced by m_Script that are not in the code closure
are reported as well.

    python3 tools/port_asset_closure.py                       # code closure as seeds
    python3 tools/port_asset_closure.py --seed Assets/X.prefab --no-code
    python3 tools/port_asset_closure.py --json out.json --list out.txt
    python3 tools/port_asset_closure.py --why Assets/Some/File.png
"""
import argparse
import fnmatch
import json
import os
import re
import subprocess
import sys
import threading
from collections import defaultdict, deque

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import port_code_closure as pcc  # noqa: E402

REF = pcc.REF
YAML_EXT = {".prefab", ".mat", ".asset", ".controller", ".anim", ".overrideController",
            ".mask", ".spriteatlas", ".spriteatlasv2", ".playable", ".fontsettings",
            ".guiskin", ".renderTexture", ".flare", ".physicMaterial", ".physicsMaterial2D",
            ".lighting", ".signal", ".preset", ".terrainlayer", ".brush", ".cubemap"}
SHADER_EXT = {".shader", ".cginc", ".hlsl", ".glslinc", ".compute", ".shadergraph", ".shadersubgraph"}
TEXT_RES_EXT = {".json", ".txt", ".csv", ".xml", ".bytes", ".yaml"}
BUILTIN_GUIDS = {"0000000000000000f000000000000000", "0000000000000000e000000000000000",
                 "0000000000000000d000000000000000"}

JG_RES = "Assets/JellyGarden/Resources/"
DEFAULT_CUTS = [
    ("KingVanguard/avatar art (D2)", ["Assets/Characters/*", JG_RES + "RoyalGameplay/king-*"]),
    ("music/ambience", [JG_RES + "RoyalGameplay/Music/*", JG_RES + "RoyalGameplay/Ambience/*",
                        JG_RES + "RoyalGameplay/Audio/castle-theme.*"]),
    ("win card", [JG_RES + "RoyalGameplay/Win/*", JG_RES + "RoyalGameplay/win-*",
                  JG_RES + "RoyalGameplay/Audio/royal-fanfare.*", "Assets/ReferenceGameplay/WinVFX/*"]),
    ("economy (coins)", [JG_RES + "RoyalGameplay/coin.*", JG_RES + "RoyalGameplay/RewardChest/*"]),
    ("dead code path (plan 6.1)", [JG_RES + "RoyalColorBall/Frames/*", JG_RES + "RoyalPropellerV2/*"]),
    ("home/areas/title", [JG_RES + "RoyalAreas/*", JG_RES + "RoyalLessons/*",
                          "Assets/ReferenceGameplay/Home/Resources/*", "Assets/ReferenceGameplay/Home/Art/*",
                          "Assets/ReferenceGameplay/Home/Prefabs/*"]),
    ("profile/leaderboard pages", [JG_RES + "RoyalProfile/*", JG_RES + "RoyalLeaderboard/*"]),
    ("reference campaign (D8)", [JG_RES + "RoyalCampaign/*", JG_RES + "RoyalCampaignArt/*",
                                 "Assets/ReferenceGameplay/Art/Resources/ReferenceGameplay/Blockers/*",
                                 "Assets/ReferenceGameplay/Levels/*"]),
    ("kit level data (main's kept)", [JG_RES + "Levels/*", JG_RES + "Targets/*"]),
    ("kit prefabs (main's kept, plan 3.6 step 2)", [JG_RES + "Prefabs/*"]),
    ("TNT 3D fallback", [JG_RES + "RoyalTNT/*"]),
    ("old booster versions", ["Assets/Boosters/Propeller/V2/*", "Assets/Boosters/Propeller/V3/*",
                              "Assets/Boosters/Propeller/V4/*", "Assets/Boosters/ColorBall/V2/*",
                              "Assets/Boosters/ColorBall/V3/*"]),
    ("scene", ["*.unity"]),
]

# Load arguments computed by a method call that the folder cannot follow:
# (file, method) -> the string expression its result starts with.
MANUAL_METHOD_PREFIX = {
    ("Assets/JellyGarden/Scripts/Juice/RoyalPieceSkin.cs", "CampaignResourcePath"): "CampaignResourceRoot",
}
# Prefix loads always taken whole (frame sequences, numbered/directional sets).
FORCE_FULL_PREFIX = ("RoyalColorBall/Frames/", "RoyalTNT/Frames/",
                     "RoyalGameplayV4/02_powerups/rocket_4_direction/")

LOAD_RE = re.compile(r"\bResources\s*\.\s*(Load|LoadAll|LoadAsync)\s*(?:<[^>()]*>)?\s*\(")
SHADER_FIND_RE = re.compile(r"\bShader\s*\.\s*Find\s*\(\s*\"([^\"]+)\"")
STR_LIT_RE = re.compile(r'(?<![\w@$])"((?:[^"\\\n]|\\.)*)"')
CONST_RE = re.compile(r"\b(?:const|static\s+readonly|readonly\s+static|static)\s+string\s+(\w+)\s*=\s*([^;]+);")
PROP_RE = re.compile(r"\bstring\s+(\w+)\s*=>\s*([^;]+);")
GUID_RE = re.compile(r"guid:\s*([0-9a-f]{32})")
SHADER_NAME_RE = re.compile(r'^\s*Shader\s+"([^"]+)"', re.M)
USEPASS_RE = re.compile(r'\b(?:UsePass|Fallback|FallBack)\s+"([^"]+)"')
INCLUDE_RE = re.compile(r'#\s*include(?:_with_pragmas)?\s+"([^"]+)"')
SCRIPT_RE = re.compile(r"m_Script:\s*\{fileID:\s*-?\d+,\s*guid:\s*([0-9a-f]{32})")


def git(*args):
    return subprocess.run(["git", *args], check=True, capture_output=True).stdout


class Repo:
    def __init__(self, ref):
        self.ref = ref
        self.files = {}   # path -> (oid, size)
        for line in git("ls-tree", "-r", "-l", ref).decode("utf-8", "replace").splitlines():
            meta, path = line.split("\t", 1)
            _, kind, oid, size = meta.split()
            if kind == "blob":
                self.files[path] = (oid, int(size) if size != "-" else 0)
        self._cache = {}
        self.guid = {}
        metas = [p for p in self.files if p.endswith(".meta")]
        for p, txt in self.read_many(metas).items():
            m = re.search(r"^guid:\s*([0-9a-f]{32})", txt, re.M)
            if m:
                self.guid[m.group(1)] = p[:-5]
        # Resources index: key (path under Resources/, no extension) -> [paths]
        self.res = defaultdict(list)
        self.res_dirs = defaultdict(list)
        for p in self.files:
            if p.endswith(".meta") or "/Resources/" not in p:
                continue
            key = p.rsplit("/Resources/", 1)[1]
            stem = os.path.splitext(key)[0]
            self.res[stem].append(p)
            parts = stem.split("/")
            for k in range(1, len(parts)):
                self.res_dirs["/".join(parts[:k])].append(p)
        self.shader_names = {}
        shaders = [p for p in self.files if p.endswith(".shader")]
        for p, txt in self.read_many(shaders).items():
            m = SHADER_NAME_RE.search(txt)
            if m:
                self.shader_names.setdefault(m.group(1), p)

    def read_many(self, paths):
        todo = [p for p in paths if p not in self._cache and p in self.files]
        if todo:
            proc = subprocess.Popen(["git", "cat-file", "--batch"], stdin=subprocess.PIPE, stdout=subprocess.PIPE)
            req = "".join(self.files[p][0] + "\n" for p in todo).encode()

            def feed():
                proc.stdin.write(req)
                proc.stdin.close()
            threading.Thread(target=feed, daemon=True).start()
            for p in todo:
                header = proc.stdout.readline().split()
                size = int(header[2])
                data = proc.stdout.read(size)
                proc.stdout.read(1)
                self._cache[p] = data.decode("utf-8", "replace")
            proc.stdout.close()
            proc.wait()
        return {p: self._cache[p] for p in paths if p in self._cache}

    def read(self, path):
        return self.read_many([path]).get(path, "")


def is_text_yaml(txt):
    return txt.startswith("%YAML") or txt.startswith("--- !u!")


def split_args(src, start):
    """Return the first argument text of a call whose '(' is at src[start-1]."""
    depth, i, out = 0, start, []
    in_str = False
    while i < len(src):
        c = src[i]
        if in_str:
            out.append(c)
            if c == "\\":
                out.append(src[i + 1]); i += 2; continue
            if c == '"':
                in_str = False
        elif c == '"':
            in_str = True; out.append(c)
        elif c in "([{":
            depth += 1; out.append(c)
        elif c in ")]}":
            if depth == 0:
                break
            depth -= 1; out.append(c)
        elif c == "," and depth == 0:
            break
        else:
            out.append(c)
        i += 1
    return "".join(out).strip()


def split_plus(expr):
    parts, depth, cur, in_str = [], 0, [], False
    i = 0
    while i < len(expr):
        c = expr[i]
        if in_str:
            cur.append(c)
            if c == "\\":
                cur.append(expr[i + 1]); i += 2; continue
            if c == '"':
                in_str = False
        elif c == '"':
            in_str = True; cur.append(c)
        elif c in "([{":
            depth += 1; cur.append(c)
        elif c in ")]}":
            depth -= 1; cur.append(c)
        elif c == "+" and depth == 0:
            parts.append("".join(cur).strip()); cur = []
        else:
            cur.append(c)
        i += 1
    parts.append("".join(cur).strip())
    return parts


class Consts:
    """name -> list of (file, string value) from const/static string fields."""
    def __init__(self, texts):
        self.by_name = defaultdict(list)
        raw = []
        for path, txt in texts.items():
            for rx in (CONST_RE, PROP_RE):
                for m in rx.finditer(txt):
                    raw.append((path, m.group(1), m.group(2).strip()))
        # fold up to 4 rounds so consts built from consts resolve
        known = {}
        for _ in range(4):
            for path, name, expr in raw:
                val = self.fold(expr, path, known)
                if val is not None and val[1]:
                    known[(path, name)] = val[0]
        for (path, name), v in known.items():
            self.by_name[name].append((path, v))

    @staticmethod
    def fold(expr, path, known):
        out = []
        for part in split_plus(expr):
            m = re.fullmatch(r'@?"((?:[^"\\]|\\.)*)"', part)
            if m:
                out.append(m.group(1)); continue
            name = part.split(".")[-1].strip()
            hit = known.get((path, name)) or next((v for (p, n), v in known.items() if n == name), None)
            if hit is None:
                return ("".join(out), False)
            out.append(hit)
        return ("".join(out), True)

    def lookup(self, name, path, qualifier=None, class_files=None):
        c = self.by_name.get(name, [])
        if qualifier and class_files:
            for p, v in c:
                if p in class_files.get(qualifier, ()):
                    return v
        for p, v in c:
            if p == path:
                return v
        return c[0][1] if len(c) == 1 else None


def eval_load_arg(expr, path, consts, code="", pos=0, class_files=None, depth=0):
    """-> (literal text, exact?). `code`/`pos` let a bare local be traced back."""
    out = []
    m = re.fullmatch(r'\$@?"([^"]*)"', expr)
    if m:  # interpolated: keep the head up to the first hole
        head = m.group(1).split("{", 1)
        return head[0], len(head) == 1
    for k, part in enumerate(split_plus(expr)):
        m = re.fullmatch(r'@?"((?:[^"\\]|\\.)*)"', part)
        if m:
            out.append(m.group(1)); continue
        m = re.fullmatch(r"[\w.]+", part)
        if m:
            bits = part.split(".")
            v = consts.lookup(bits[-1], path, bits[-2] if len(bits) > 1 else None, class_files)
            if v is not None:
                out.append(v); continue
            if k == 0 and len(bits) == 1 and code and depth < 2:
                # a local: nearest `name = <expr>;` before the call
                prev = list(re.finditer(r"\b" + re.escape(part) + r"\s*=\s*([^;=][^;]*);", code[:pos]))
                if prev:
                    sub = prev[-1].group(1).strip()
                    mm = re.fullmatch(r"(\w+)\s*\(.*\)", sub)
                    if mm and (path, mm.group(1)) in MANUAL_METHOD_PREFIX:
                        sub = MANUAL_METHOD_PREFIX[(path, mm.group(1))] + " + x"
                    lit, exact = eval_load_arg(sub, path, consts, code, prev[-1].start(), class_files, depth + 1)
                    out.append(lit)
                    if exact:
                        continue
        return "".join(out), False
    return "".join(out), True


def match_any(path, pats):
    return any(fnmatch.fnmatchcase(path, p) for p in pats)


def cut_label(path, cuts):
    for label, pats in cuts:
        if match_any(path, pats):
            return label
    return None


def run(repo, seeds, code_files, cuts, extra_allowed=()):
    included = {}          # path -> reason
    parent = {}            # path -> (src, how)
    cut_hits = []
    dynamic = []           # unresolved Resources.Load args
    missing_res = []
    unresolved_guids = defaultdict(set)
    scripts_outside = defaultdict(set)
    shader_missing = []
    loads = []             # every Resources.Load site: (file, line, expr, literal, exact, n_matched)
    q = deque()
    cs_texts = repo.read_many([p for p in code_files if p.endswith(".cs")])
    stripped = {p: pcc.strip_code_keep_strings(t) for p, t in cs_texts.items()}
    consts = Consts(stripped)
    class_files = defaultdict(set)
    for p, t in stripped.items():
        for m in re.finditer(r"\b(?:class|struct)\s+(\w+)", t):
            class_files[m.group(1)].add(p)
    # every string literal in the code closure: narrows prefix loads
    literals = set()
    for t in stripped.values():
        literals.update(m.group(1) for m in STR_LIT_RE.finditer(t))
    narrowed_report = []
    catalog_dropped = []

    def add(path, src, how):
        if path in included:
            return
        if path not in repo.files:
            return
        label = cut_label(path, cuts)
        if label and path not in extra_allowed:
            cut_hits.append({"from": src, "to": path, "how": how, "area": label})
            return
        included[path] = how
        parent[path] = (src, how)
        q.append(path)

    def add_res_key(literal, exact, src, how, folder_ok=True):
        lit = literal.rstrip("/") if exact else literal  # a prefix keeps its "/"
        hits = []
        if exact:
            hits = list(repo.res.get(lit, []))
            if folder_ok:
                hits += repo.res_dirs.get(lit, [])
        else:
            if lit == "":
                return 0
            full = {k: ps for k, ps in repo.res.items() if k.startswith(lit)}
            if lit.startswith(FORCE_FULL_PREFIX):
                keep = set(full)
            else:
                keep = {k for k in full
                        if k[len(lit):] in literals
                        or any(k[len(lit):].startswith(x) for x in literals if x and x[-1] in "-_/")}
                if not keep:
                    keep = set(full)
            narrowed_report.append((src, lit, len(keep), len(full), sorted(set(full) - keep)))
            for k in sorted(keep):
                hits.extend(full[k])
        for h in hits:
            add(h, src, how)
        return len(hits)

    for s in seeds:
        if s.endswith("/"):
            for p in repo.files:
                if p.startswith(s) and not p.endswith(".meta"):
                    add(p, "seed", "seed")
        else:
            add(s, "seed", "seed")
            included[s] = "seed"

    while q:
        p = q.popleft()
        ext = os.path.splitext(p)[1]
        # .meta guid references (default references, external objects)
        meta = repo.read(p + ".meta") if p + ".meta" in repo.files else ""
        for g in sorted(set(GUID_RE.findall(meta))):
            if repo.guid.get(g) == p:
                continue
            tgt = repo.guid.get(g)
            if tgt:
                add(tgt, p, "meta-guid")
        if ext == ".cs":
            txt = repo.read(p)
            code = pcc.strip_code_keep_strings(txt)
            for m in LOAD_RE.finditer(code):
                arg = split_args(code, m.end())
                lit, exact = eval_load_arg(arg, p, consts, code, m.start(), class_files)
                line = code[:m.start()].count("\n") + 1
                n = add_res_key(lit, exact, f"{p}:{line}", "Resources." + m.group(1)) if (lit or exact) else 0
                loads.append((p, line, arg, lit, exact, n))
                if not lit and not exact:
                    dynamic.append((p, line, arg))
                elif n == 0:
                    missing_res.append((p, line, arg, lit))
            for m in SHADER_FIND_RE.finditer(code):
                line = code[:m.start()].count("\n") + 1
                tgt = repo.shader_names.get(m.group(1))
                if tgt:
                    add(tgt, f"{p}:{line}", "Shader.Find")
                else:
                    shader_missing.append((p, line, m.group(1)))
            for m in STR_LIT_RE.finditer(code):
                s = m.group(1)
                if s and s in repo.res and "/" in s:
                    line = code[:m.start()].count("\n") + 1
                    for h in repo.res[s]:
                        add(h, f"{p}:{line}", "literal-key")
        elif ext in YAML_EXT or (ext == ".asset" ):
            txt = repo.read(p)
            if not is_text_yaml(txt):
                continue
            for g in sorted(set(GUID_RE.findall(txt))):
                if g in BUILTIN_GUIDS:
                    continue
                tgt = repo.guid.get(g)
                if not tgt:
                    unresolved_guids[g].add(p)
                    continue
                if tgt.endswith(".cs") and tgt not in code_files:
                    if g in SCRIPT_RE.findall(txt):
                        scripts_outside[tgt].add(p)
                        continue
                if tgt.endswith(".cs"):
                    continue
                add(tgt, p, "guid")
        elif ext in SHADER_EXT:
            txt = repo.read(p)
            for m in USEPASS_RE.finditer(txt):
                name = m.group(1).rsplit("/", 1)[0] if m.group(0).startswith("UsePass") else m.group(1)
                tgt = repo.shader_names.get(name)
                if tgt:
                    add(tgt, p, "shader-name")
            for m in INCLUDE_RE.finditer(txt):
                inc = m.group(1)
                cands = [os.path.normpath(os.path.join(os.path.dirname(p), inc)), inc]
                for c in cands:
                    if c in repo.files:
                        add(c, p, "#include"); break
        elif ext == ".json" and "/Resources/" in p and '"resourcesPath"' in repo.read(p):
            # a key -> resourcesPath catalog: take the entries whose key the code names
            try:
                entries = json.loads(repo.read(p)).get("entries", [])
            except ValueError:
                entries = []
            for e in entries:
                key, rp = e.get("key", ""), e.get("resourcesPath", "")
                used = key in literals or any(key.startswith(x) for x in literals if x and x[-1] in "-_")
                if used:
                    for h in repo.res.get(rp, []):
                        add(h, p, "catalog-key " + key)
                else:
                    catalog_dropped.append((p, key, rp))
        elif ext in TEXT_RES_EXT and "/Resources/" in p:
            txt = repo.read(p)
            for m in re.finditer(r'"([^"\n]+)"', txt):
                s = m.group(1)
                if "/" in s:
                    s2 = os.path.splitext(s)[0]
                    if s2 in repo.res:
                        for h in repo.res[s2]:
                            add(h, p, "catalog-key")
    return {
        "included": included, "parent": parent, "cuts": cut_hits, "dynamic": dynamic,
        "missing_res": missing_res, "unresolved_guids": {k: sorted(v) for k, v in unresolved_guids.items()},
        "scripts_outside": {k: sorted(v) for k, v in scripts_outside.items()},
        "shader_missing": shader_missing, "loads": loads, "narrowed": narrowed_report,
        "catalog_dropped": catalog_dropped,
    }


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--ref", default=REF)
    ap.add_argument("--seed", action="append", default=[], help="extra seed path (dir prefixes end with /)")
    ap.add_argument("--seed-file", help="file with one seed path per line")
    ap.add_argument("--allow", action="append", default=[], help="path exempt from cut rules")
    ap.add_argument("--no-code", action="store_true", help="do not seed with the code closure")
    ap.add_argument("--runtime-only", action="store_true", help="seed with the runtime code closure only (no editor tests)")
    ap.add_argument("--json")
    ap.add_argument("--list", help="write the closure (non-.cs) one path per line")
    ap.add_argument("--why")
    args = ap.parse_args()

    repo = Repo(args.ref)
    code_files = set()
    seeds = list(args.seed)
    if args.seed_file:
        seeds += [ln.strip() for ln in open(args.seed_file) if ln.strip() and not ln.startswith("#")]
    if not args.no_code:
        files, index = pcc.build(args.ref)
        res = pcc.compute(files, index)
        code_files = set(res["runtime"]) | (set() if args.runtime_only else set(res["tests"]))
        seeds = sorted(code_files) + seeds
    code_files |= {s for s in seeds if s.endswith(".cs")}
    out = run(repo, seeds, code_files, DEFAULT_CUTS, set(args.allow))

    if args.why:
        p = args.why
        while p in out["parent"] and out["parent"][p][0] != "seed":
            src, how = out["parent"][p]
            print(f"{p}  <-[{how}]-  {src}")
            p = src.split(":")[0]
        print(f"{p}  ({'seed' if p in out['included'] else 'not in closure'})")
        return

    assets = sorted(p for p in out["included"] if not p.endswith(".cs"))
    total = sum(repo.files[p][1] + repo.files.get(p + ".meta", ("", 0))[1] for p in assets)
    if args.list:
        with open(args.list, "w") as fh:
            fh.write("\n".join(assets) + "\n")
    if args.json:
        with open(args.json, "w") as fh:
            json.dump({**out, "assets": assets,
                       "sizes": {p: repo.files[p][1] for p in out["included"]}}, fh, indent=1)
    print(f"# asset closure: {len(assets)} files, {total/1e6:.1f} MB incl. .meta "
          f"(+{len([p for p in out['included'] if p.endswith('.cs')])} .cs)")
    by_dir = defaultdict(lambda: [0, 0])
    for p in assets:
        d = "/".join(p.split("/")[:4])
        by_dir[d][0] += 1
        by_dir[d][1] += repo.files[p][1]
    for d, (n, sz) in sorted(by_dir.items()):
        print(f"{n:6d} {sz/1e6:8.2f} MB  {d}")
    print("\n# cut edges (target area <- source)")
    agg = defaultdict(set)
    for c in out["cuts"]:
        agg[(c["area"], c["from"])].add(c["to"])
    for (area, src), tos in sorted(agg.items()):
        sample = sorted(tos)[0]
        print(f"[{area}] {src} -> {len(tos)} file(s), e.g. {sample}")
    print("\n# Resources.Load sites")
    for p, line, arg, lit, exact, n in out["loads"]:
        print(f"{p}:{line}\t{'exact' if exact else 'prefix'}\t{lit!r}\t{n} hit(s)\t<- {arg[:90]}")
    print("\n# prefix loads narrowed by code literals (kept/total; dropped keys)")
    seen = set()
    for src, lit, kept, total, dropped in out["narrowed"]:
        if (lit, kept) in seen:
            continue
        seen.add((lit, kept))
        print(f"{src}\t{lit!r}\t{kept}/{total}\tdropped: {', '.join(dropped[:12])}{' ...' if len(dropped) > 12 else ''}")
    print("\n# catalog entries not named by any code literal (not followed)")
    for p, key, rp in out["catalog_dropped"]:
        print(f"{p}\t{key}\t{rp}")
    print("\n# unresolved dynamic loads (no literal head)")
    for p, line, arg in out["dynamic"]:
        print(f"{p}:{line}\t{arg[:120]}")
    print("\n# Shader.Find names with no .shader on the ref (built-in or package)")
    for p, line, n in out["shader_missing"]:
        print(f"{p}:{line}\t{n}")
    print("\n# scripts used by closure assets but outside the code closure")
    for s, users in sorted(out["scripts_outside"].items()):
        print(f"{s}\t<- {len(users)} asset(s), e.g. {users[0]}")
    print("\n# guids with no .meta on the ref (packages / built-in)")
    for g, users in sorted(out["unresolved_guids"].items(), key=lambda kv: -len(kv[1])):
        print(f"{g}\t{len(users)} asset(s), e.g. {users[0]}")


if __name__ == "__main__":
    sys.exit(main())
