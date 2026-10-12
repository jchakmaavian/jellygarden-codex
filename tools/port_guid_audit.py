#!/usr/bin/env python3
"""GUID audit for the royal-throne level port (plan Task 1.2).

Compares every Unity `.meta` under `Assets/` on two git refs (default
`origin/main` and `origin/royal-throne`) and writes a Markdown report to stdout.
Everything is read from git objects (`git ls-tree -r`, `git cat-file --batch`);
the working tree is never read, so the result does not depend on the branch
that is checked out or on what an open Unity Editor has written to disk.

Lists:

  (a) same path, same GUID, identical asset blob and identical .meta blob
  (b) same path, same GUID, different asset bytes and/or different .meta bytes
  (c) same GUID at different paths: across the two refs, and duplicates inside
      the royal-throne ref (main-internal duplicates are listed for information)
  (d) same path, different GUID

Plus, for planning: main-only paths inside folders royal-throne also has (a
folder-level `git restore --source` deletes them), paths that differ only by
letter case, and the royal-throne-only paths (new GUIDs that import cleanly).

Every (b) entry is classified: "texture/meta reimport only" (asset bytes
identical, only importer settings differ), "content differs", or "content
differs (line endings only)". It then gets a side for its bytes and for its
.meta. The order of rules is: what the plan already fixes (section 4 kit
scripts, campaign levels/targets, scenes, kit prefabs); royal-throne still has
the untouched kit version -> main's; imported royal-throne C# uses public
members only royal-throne's version declares -> royal-throne's (merge if main
changed it too); an imported royal-throne file references it (GUID, asset path
or Resources key) -> royal-throne's; otherwise main's. "Changed vs kit" is
measured against the common kit ancestor (--kit-ref).

Usage:

    python3 tools/port_guid_audit.py > docs/port/GUID-AUDIT.md
    python3 tools/port_guid_audit.py --paths-file docs/port/import-paths.txt
    python3 tools/port_guid_audit.py --json audit.json
    python3 tools/test_port_guid_audit.py          # self-test on a scratch repo

`--paths-file` restricts the royal-throne side to the listed path prefixes (one
per line, `#` comments allowed, e.g. `Assets/Spine` or `Assets/Boosters/`);
it also narrows what counts as "imported" when deciding sides. The main side is
always the whole of `Assets/`, so a collision with any main asset is found.

Stdlib only. Exit code 0 when lists (c) and (d) are empty for the selected
royal-throne paths, 1 when they are not, 2 on git errors.
"""

import argparse
import collections
import json
import os
import re
import subprocess
import sys

PROJECT_ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
REPO = PROJECT_ROOT  # git repository to read; --repo (and the tests) change it
KIT_REF = "a82c4df33a929614fd4d7095b59aa26cb0274a8b"  # pristine JellyGarden kit, common ancestor

GUID_RE = re.compile(rb"^guid:\s*([0-9a-fA-F]{32})\s*$", re.M)
TEXT_EXTS = {
    ".prefab", ".unity", ".asset", ".mat", ".controller", ".overrideController",
    ".anim", ".playable", ".mask", ".physicMaterial", ".physicsMaterial2D",
    ".spriteatlas", ".spriteatlasv2", ".renderTexture", ".lighting", ".signal",
    ".guiskin", ".fontsettings", ".shadergraph", ".shadersubgraph", ".vfx",
    ".brush", ".flare", ".terrainlayer", ".mixer", ".cubemap", ".preset",
    ".inputactions", ".asmdef", ".asmref", ".uss", ".uxml", ".tss",
}

# Plan section 4: which side owns each kit script that both refs changed.
KIT_SCRIPT_RULES = {
    "Assets/JellyGarden/Scripts/LevelManager.cs": ("royal-throne", "plan §4: royal-throne base + main keep-hooks (Task 3.1)"),
    "Assets/JellyGarden/Scripts/Item.cs": ("royal-throne", "plan §4: royal-throne base, main Story hooks dropped (Task 3.2)"),
    "Assets/JellyGarden/Scripts/Square.cs": ("royal-throne", "plan §4: royal-throne base + main IngredientExitRow arrow (Task 3.3)"),
    "Assets/JellyGarden/Scripts/System/CombineManager.cs": ("royal-throne", "plan §4: royal-throne, campaign exclusions cut (Task 3.3)"),
    "Assets/JellyGarden/Scripts/AI.cs": ("royal-throne", "plan §4: royal-throne, campaign exclusions cut (Task 3.3)"),
    "Assets/JellyGarden/Scripts/InputManager.cs": ("royal-throne", "plan §4: royal-throne (Task 3.4 Step 1)"),
    "Assets/JellyGarden/Scripts/InitScript.cs": ("main", "plan §4: keep main's (StoryFreeGame/StoryFlow hooks)"),
    "Assets/JellyGarden/Scripts/GUI/AnimationManager.cs": ("main", "plan §4: keep main's (StoryFlow hooks)"),
    "Assets/JellyGarden/Scripts/GUI/MenuManager.cs": ("main", "plan §4: keep main's + royal-throne BlocksBoardInput (Task 3.4 Step 2)"),
    "Assets/JellyGarden/Scripts/SoundBase.cs": ("merge", "plan §4: union of both (Task 3.4 Step 3, git merge-file base a82c4df)"),
    "Assets/JellyGarden/Scripts/MusicBase.cs": ("merge", "plan §4: union of both (Task 3.4 Step 3, git merge-file base a82c4df)"),
    "Assets/JellyGarden/Scripts/Targets/TargetLevel.cs": ("royal-throne", "plan §4: royal-throne referenceGoal, keep main goal-art call site (Task 3.4 Step 4)"),
    "Assets/JellyGarden/Scripts/Targets/TargetIcon.cs": ("royal-throne", "plan §4: royal-throne referenceGoal, keep main goal-art call site (Task 3.4 Step 4)"),
    "Assets/JellyGarden/Scripts/Targets/TargetsGroup.cs": ("royal-throne", "plan §4: royal-throne referenceGoal (Task 3.4 Step 4)"),
    "Assets/JellyGarden/Scripts/GUI/BoostShop.cs": ("royal-throne", "plan §4: royal-throne enum additions (Task 3.4 Step 5)"),
    "Assets/JellyGarden/Scripts/GUI/BoostIcon.cs": ("royal-throne", "plan §4: royal-throne (Task 3.4 Step 5)"),
}


class GitError(RuntimeError):
    pass


def git(*args, stdin=None):
    proc = subprocess.run(
        ["git", "-C", REPO, *args],
        input=stdin,
        capture_output=True,
    )
    if proc.returncode not in (0, 1) or (proc.returncode == 1 and proc.stderr.strip()):
        raise GitError("git %s failed: %s" % (" ".join(args[:3]), proc.stderr.decode(errors="replace").strip()))
    return proc.stdout


def ls_tree(ref, root="Assets"):
    """path -> (blob oid, size) for every blob under root on ref."""
    out = git("ls-tree", "-r", "-l", "-z", ref, "--", root)
    entries = {}
    for rec in out.split(b"\0"):
        if not rec:
            continue
        meta, path = rec.split(b"\t", 1)
        mode, kind, oid, size = meta.split()
        if kind != b"blob":
            continue
        entries[path.decode("utf-8", "surrogateescape")] = (oid.decode(), int(size) if size != b"-" else -1)
    return entries


def cat_blobs(oids):
    """oid -> bytes, via one `git cat-file --batch` process."""
    oids = sorted(set(oids))
    if not oids:
        return {}
    out = git("cat-file", "--batch", stdin=("\n".join(oids) + "\n").encode())
    blobs, pos = {}, 0
    for _ in oids:
        nl = out.index(b"\n", pos)
        header = out[pos:nl].split()
        if len(header) < 3 or header[1] == b"missing":
            raise GitError("cat-file: missing object %s" % header[0].decode())
        size = int(header[2])
        start = nl + 1
        blobs[header[0].decode()] = out[start:start + size]
        pos = start + size + 1
    return blobs


def directories(paths):
    dirs = set()
    for p in paths:
        parts = p.split("/")
        for i in range(1, len(parts)):
            dirs.add("/".join(parts[:i]))
    return dirs


class Side:
    """One ref's view of Assets/: blobs, folders, and the GUID of every .meta."""

    def __init__(self, ref):
        self.ref = ref
        self.blobs = ls_tree(ref)
        self.dirs = directories(self.blobs)
        self.metas = {p[:-5]: oid for p, (oid, _) in self.blobs.items() if p.endswith(".meta")}
        self.meta_bytes = {}
        self.guid = {}          # asset path -> guid ('' when the .meta has none)
        self.by_guid = collections.defaultdict(list)

    def load(self, blobs):
        for asset, oid in self.metas.items():
            data = blobs[oid]
            self.meta_bytes[asset] = data
            m = GUID_RE.search(data)
            g = m.group(1).decode().lower() if m else ""
            self.guid[asset] = g
            if g:
                self.by_guid[g].append(asset)

    def kind(self, asset):
        if asset in self.blobs:
            return "file"
        if asset in self.dirs:
            return "folder"
        return "missing"

    def asset_oid(self, asset):
        entry = self.blobs.get(asset)
        return entry[0] if entry else None

    def asset_size(self, asset):
        entry = self.blobs.get(asset)
        return entry[1] if entry else None


def load_prefixes(path):
    prefixes = []
    with open(path, encoding="utf-8") as fh:
        for line in fh:
            line = line.split("#", 1)[0].strip()
            if line:
                prefixes.append(line.rstrip("/"))
    return prefixes


def selected(asset, prefixes):
    if prefixes is None:
        return True
    for pre in prefixes:
        # the prefix itself, anything under it, and its parent folders (their .meta comes along)
        if asset == pre or asset.startswith(pre + "/") or pre.startswith(asset + "/"):
            return True
    return False


# ---------------------------------------------------------------- meta diffs

def _tokens(text):
    """(indent, key, value) tokens of a Unity .meta; '-' marks a list item."""
    toks = []
    for raw in text.splitlines():
        if not raw.strip():
            continue
        ind = len(raw) - len(raw.lstrip(" "))
        body = raw.strip()
        while body == "-" or body.startswith("- "):
            toks.append((ind, "-", None))
            body = body[1:].strip()
            ind += 2
        if not body:
            continue
        if body[0] in "{[\"'" or ":" not in body:
            toks.append((ind, None, body))
        else:
            k, v = body.split(":", 1)
            toks.append((ind, k.strip(), v.strip()))
    return toks


def _build(toks, i, ind):
    if i < len(toks) and toks[i][0] == ind and toks[i][1] == "-":
        items = []
        while i < len(toks) and toks[i][0] == ind and toks[i][1] == "-":
            i += 1
            if i < len(toks) and toks[i][0] > ind:
                if toks[i][1] is None:
                    item, i = toks[i][2], i + 1
                else:
                    item, i = _build(toks, i, toks[i][0])
            else:
                item = None
            items.append(item)
        return items, i
    node = {}
    while i < len(toks) and toks[i][0] == ind and toks[i][1] != "-":
        _, k, v = toks[i]
        i += 1
        if k is None:
            continue
        if v == "" and i < len(toks) and (toks[i][0] > ind or (toks[i][0] == ind and toks[i][1] == "-")):
            node[k], i = _build(toks, i, toks[i][0])
        else:
            node[k] = v
        while i < len(toks) and toks[i][0] > ind:   # unparsed continuation lines
            i += 1
    return node, i


def parse_meta(data):
    """Unity .meta (block YAML subset) -> nested dict/list/str."""
    toks = _tokens(data.decode("utf-8", "replace"))
    if not toks:
        return {}
    node, _ = _build(toks, 0, toks[0][0])
    return node if isinstance(node, dict) else {}


def _num(v):
    if isinstance(v, str):
        try:
            f = float(v)
            return str(int(f)) if f.is_integer() and abs(f) < 1e18 else repr(f)
        except ValueError:
            return v.replace(" .", " 0.").replace(":.", ":0.").replace("{x: .", "{x: 0.")
    return v


def importer_of(meta):
    for k, v in meta.items():
        if k.endswith("Importer") and isinstance(v, dict):
            return k, v
    return None, {}


def _list(v):
    return v if isinstance(v, list) else []


def _dict(v):
    return v if isinstance(v, dict) else {}


def _platform(imp, target):
    for item in _list(imp.get("platformSettings")):
        if isinstance(item, dict) and item.get("buildTarget") == target:
            return item
    return None


def texture_settings(imp):
    """The importer settings that mean the same thing in the 2019 kit format and the Unity 6 format."""
    s = {}
    for k in ("textureType", "spriteMode", "spritePixelsToUnits", "spriteMeshType", "spriteExtrude",
              "alignment", "spritePivot", "spriteBorder", "alphaIsTransparency", "isReadable", "nPOTScale",
              "spritePackingTag", "textureShape"):
        if k in imp:
            s[k] = _num(imp[k])
    if s.get("textureType") == "5":        # kit-era "Advanced" is Unity 6 "Default"
        s["textureType"] = "0"
    mip = _dict(imp.get("mipmaps"))
    if "enableMipMap" in mip:
        s["mipmaps.enableMipMap"] = _num(mip["enableMipMap"])
    ts = _dict(imp.get("textureSettings"))
    for k in ("filterMode", "aniso"):
        if k in ts and _num(ts[k]) != "-1":
            s["textureSettings." + k] = _num(ts[k])
    wrap = ts.get("wrapU", ts.get("wrapMode"))
    if wrap is not None and _num(wrap) != "-1":
        s["textureSettings.wrap"] = _num(wrap)
    default = _platform(imp, "DefaultTexturePlatform")
    size = default.get("maxTextureSize") if default else imp.get("maxTextureSize")
    if size is not None:
        s["maxTextureSize"] = _num(size)
    if default and "textureCompression" in default:
        s["textureCompression"] = _num(default["textureCompression"])
    overrides = []
    for item in _list(imp.get("platformSettings")) + _list(imp.get("buildTargetSettings")):
        if isinstance(item, dict) and item.get("buildTarget") != "DefaultTexturePlatform" \
                and _num(item.get("overridden", "1")) == "1":
            overrides.append("%s:%s/%s" % (item.get("buildTarget"), _num(item.get("maxTextureSize")),
                                           _num(item.get("textureFormat"))))
    s["platformOverrides"] = ",".join(sorted(o for o in overrides if not o.startswith("Android:"))) or "none"
    s["androidOverrides"] = ",".join(sorted(o for o in overrides if o.startswith("Android:"))) or "none"
    sheet = _dict(imp.get("spriteSheet"))
    sprites = _list(sheet.get("sprites"))
    if True:
        s["spriteSheet.sprites"] = str(len(sprites))
        for sp in sprites:
            if isinstance(sp, dict) and sp.get("name"):
                rect = _dict(sp.get("rect"))
                if rect:
                    s["sprite[%s].rect" % sp["name"]] = ",".join(_num(rect.get(k, "")) for k in ("x", "y", "width", "height"))
                if "border" in sp:
                    s["sprite[%s].border" % sp["name"]] = _num(sp["border"])
    return s


def _flatten(node, prefix, out):
    if isinstance(node, dict):
        for k, v in node.items():
            _flatten(v, prefix + "." + k if prefix else k, out)
    elif isinstance(node, list):
        for i, v in enumerate(node):
            label = i
            if isinstance(v, dict):
                label = v.get("buildTarget") or v.get("name") or i
            _flatten(v, "%s[%s]" % (prefix, label), out)
    else:
        out[prefix] = _num(node)


IGNORED_KEYS = re.compile(r"(^|\.)(serializedVersion|AssetOrigin.*|fileIDToRecycleName.*|internalIDToNameTable.*|"
                          r"externalObjects.*|assetBundleName|assetBundleVariant|userData)$")


def compare_metas(a, b):
    """Describe how two .meta files differ.

    Returns (reserialised, importer change or None, [setting differences]).
    `reserialised` is True when the importer's serializedVersion or field set
    differs, i.e. one side was written by a newer Unity.
    """
    ma, mb = parse_meta(a), parse_meta(b)
    na, ia = importer_of(ma)
    nb, ib = importer_of(mb)
    change = None if na == nb else "%s -> %s" % (na, nb)
    fa, fb = {}, {}
    _flatten(ia, "", fa)
    _flatten(ib, "", fb)
    reserialised = (ia.get("serializedVersion") != ib.get("serializedVersion")
                    or set(fa) != set(fb) or ("AssetOrigin" in ma) != ("AssetOrigin" in mb))
    diffs = []
    if na == nb == "TextureImporter":
        sa, sb = texture_settings(ia), texture_settings(ib)
        for k in sorted(set(sa) & set(sb)):
            if sa[k] != sb[k]:
                diffs.append("%s %s -> %s" % (k, sa.get(k, "—"), sb.get(k, "—")))
    else:
        for k in sorted(set(fa) & set(fb)):
            if fa[k] != fb[k] and not IGNORED_KEYS.search(k):
                diffs.append("%s %s -> %s" % (k, fa[k] or "''", fb[k] or "''"))
    return reserialised, change, diffs


def sub_asset_ids(data):
    """name -> fileID of the sub-assets (sprites) a .meta declares, in either format."""
    meta = parse_meta(data)
    _, imp = importer_of(meta)
    ids = {}
    old = imp.get("fileIDToRecycleName")
    if isinstance(old, dict):
        for fid, name in old.items():
            ids[name] = _num(fid)
    for item in _list(imp.get("internalIDToNameTable")):
        if isinstance(item, dict):
            first = item.get("first")
            fid = list(first.values())[0] if isinstance(first, dict) and first else None
            if fid is None:
                m = re.search(r":\s*(-?\d+)", str(first or ""))
                fid = m.group(1) if m else None
            if fid is not None and item.get("second"):
                ids[item["second"]] = _num(fid)
    sheet = _dict(imp.get("spriteSheet"))
    for sp in _list(sheet.get("sprites")):
        if isinstance(sp, dict) and sp.get("name") and sp.get("internalID") not in (None, "0"):
            ids[sp["name"]] = _num(sp["internalID"])
    nft = sheet.get("nameFileIdTable")
    if isinstance(nft, dict):
        for name, fid in nft.items():
            ids[name] = _num(fid)
    return ids


# ---------------------------------------------------------------- references

HEX32_RE = re.compile(rb"[0-9a-fA-F]{32}")
LITERAL_RE = re.compile(rb'"([^"\\\r\n]{4,300})"')
CODE_EXTS = {".cs", ".json"}


def resources_key(asset):
    marker = "/Resources/"
    if marker not in asset:
        return None
    key = asset.split(marker)[-1]
    return os.path.splitext(key)[0]


IDENT_RE = re.compile(rb"[A-Za-z_][A-Za-z0-9_]*")
STRIP_CS_RE = re.compile(rb'//[^\n]*|/\*.*?\*/|@?"(?:[^"\\\n]|\\.)*"', re.S)


def cs_tokens(data):
    """Identifiers in C# source, comments and string literals removed."""
    return {t.decode() for t in IDENT_RE.findall(STRIP_CS_RE.sub(b" ", data))}


PUBLIC_DECL_RE = re.compile(
    rb"^[ \t]*(?:\[[^\]\n]*\][ \t]*)*(?:(?:public|internal|protected|static|readonly|const|override|virtual|"
    rb"abstract|sealed|partial|async|extern|new|unsafe|volatile|event)\s+)*?(?:public|internal)\s+"
    rb"(?:(?:static|readonly|const|override|virtual|abstract|sealed|partial|async|extern|new|unsafe|volatile|event)\s+)*"
    rb"(?:(?:class|struct|enum|interface|record)\s+(\w+)|[\w<>\[\],.?]+(?:\s*<[^>\n]*>)?\s+(\w+)\s*(?:\(|\{|=>|=|;|<))",
    re.M)
UNITY_MESSAGES = {"Awake", "Start", "Update", "LateUpdate", "FixedUpdate", "OnEnable", "OnDisable", "OnDestroy",
                  "OnValidate", "Reset", "OnGUI", "OnApplicationPause", "OnApplicationQuit", "OnApplicationFocus"}


def public_members(src):
    """Names of public/internal types and members a C# file declares (comments and strings removed)."""
    if not src:
        return set()
    code = STRIP_CS_RE.sub(b" ", src)
    names = set()
    for m in PUBLIC_DECL_RE.finditer(code):
        name = (m.group(1) or m.group(2) or b"").decode()
        if name and name not in UNITY_MESSAGES:
            names.add(name)
    return names


def added_members(main_src, rt_src, kit_src):
    """Public members royal-throne's version declares that neither the kit's nor main's version has."""
    return public_members(rt_src) - public_members(kit_src) - public_members(main_src)


class RefIndex:
    """Who on one ref points at an asset.

    GUID users: YAML-serialised files (prefab, scene, asset, material, ...)
    under Assets/ and ProjectSettings/ that contain the GUID. Code users: .cs
    and .json files with a string literal equal to the asset path, its
    Resources key, or a parent folder of either (Resources.LoadAll / "dir/" +
    name). Blobs are read through `git cat-file --batch`; `git grep -o` with
    hundreds of fixed patterns is far too slow on these trees.
    """

    def __init__(self, side):
        entries = dict(side.blobs)
        entries.update(ls_tree(side.ref, "ProjectSettings"))
        yaml = [p for p in entries if os.path.splitext(p)[1] in TEXT_EXTS]
        code = [p for p in entries if os.path.splitext(p)[1] in CODE_EXTS]
        blobs = cat_blobs([entries[p][0] for p in yaml + code])
        self.guid_users = collections.defaultdict(set)
        for p in yaml:
            for g in set(HEX32_RE.findall(blobs[entries[p][0]])):
                self.guid_users[g.decode().lower()].add(p)
        self.literals = collections.defaultdict(set)
        self.cs_tokens = {}
        for p in code:
            data = blobs[entries[p][0]]
            for lit in set(LITERAL_RE.findall(data)):
                self.literals[lit.decode("utf-8", "replace").rstrip("/")].add(p)
            if p.endswith(".cs"):
                self.cs_tokens[p] = cs_tokens(data)

    def users_of_guid(self, guid, exclude=()):
        return sorted(u for u in self.guid_users.get(guid, ()) if u not in exclude)

    def code_users(self, asset):
        """(exact users, folder-prefix users) among .cs/.json files."""
        exact, prefix = set(), set()
        names = [asset]
        key = resources_key(asset)
        if key:
            names += [key, key + os.path.splitext(asset)[1]]
        for n in names:
            exact |= self.literals.get(n, set())
            parts = n.split("/")
            for i in range(1, len(parts)):
                parent = "/".join(parts[:i])
                if len(parent) >= 4 and parent not in ("Assets", "Assets/Resources"):
                    prefix |= self.literals.get(parent, set())
        return sorted(exact), sorted(prefix - exact)


# ---------------------------------------------------------------- audit

def top_folder(asset, depth=2):
    return "/".join(asset.split("/")[:depth])


def audit(main_ref, rt_ref, prefixes):
    main, rt = Side(main_ref), Side(rt_ref)
    blobs = cat_blobs(list(main.metas.values()) + list(rt.metas.values()))
    main.load(blobs)
    rt.load(blobs)

    rt_sel = {a for a in rt.metas if selected(a, prefixes)}
    shared = sorted(rt_sel & set(main.metas))

    res = {"a": [], "b": [], "c": [], "c_rt_dup": [], "c_main_dup": [], "d": [],
           "case": [], "rt_only": [], "no_guid": [], "main_only_in_rt_dirs": []}

    for asset in shared:
        gm, gr = main.guid[asset], rt.guid[asset]
        if not gm or not gr:
            res["no_guid"].append({"path": asset, "main_guid": gm, "rt_guid": gr})
            continue
        if gm != gr:
            res["d"].append({"path": asset, "main_guid": gm, "rt_guid": gr,
                             "main_kind": main.kind(asset), "rt_kind": rt.kind(asset)})
            continue
        km, kr = main.kind(asset), rt.kind(asset)
        meta_same = main.metas[asset] == rt.metas[asset]
        asset_same = km == kr and (km != "file" or main.asset_oid(asset) == rt.asset_oid(asset))
        entry = {"path": asset, "guid": gm, "main_kind": km, "rt_kind": kr,
                 "asset_same": asset_same, "meta_same": meta_same}
        (res["a"] if meta_same and asset_same else res["b"]).append(entry)

    # (c) GUID at different paths across refs.
    for g in sorted(set(main.by_guid) & set(rt.by_guid)):
        mpaths, rpaths = set(main.by_guid[g]), set(rt.by_guid[g])
        if mpaths == rpaths:
            continue
        rsel = {p for p in rpaths if p in rt_sel}
        if not rsel:
            continue
        only_rt = sorted(rsel - mpaths)
        only_main = sorted(mpaths - rpaths)
        if not only_rt and not only_main:
            continue
        res["c"].append({"guid": g, "main_paths": sorted(mpaths), "rt_paths": sorted(rpaths),
                         "rt_only_paths": only_rt, "main_only_paths": only_main,
                         "kinds": {p: main.kind(p) for p in mpaths} | {p: rt.kind(p) for p in rpaths},
                         "oids": {("main", p): main.asset_oid(p) for p in mpaths}
                         | {("rt", p): rt.asset_oid(p) for p in rpaths}})

    for side, key in ((rt, "c_rt_dup"), (main, "c_main_dup")):
        for g, paths in sorted(side.by_guid.items()):
            if len(paths) < 2:
                continue
            if side is rt and not any(p in rt_sel for p in paths):
                continue
            res[key].append({"guid": g, "paths": sorted(paths),
                             "selected": sorted(p for p in paths if p in rt_sel) if side is rt else sorted(paths)})

    main_lower = collections.defaultdict(list)
    for a in main.metas:
        main_lower[a.lower()].append(a)
    for a in sorted(rt_sel):
        if a in main.metas:
            continue
        others = main_lower.get(a.lower())
        if others:
            res["case"].append({"rt_path": a, "main_paths": others,
                                "rt_guid": rt.guid[a], "main_guids": [main.guid[o] for o in others]})
        elif not (rt.guid[a] and rt.guid[a] in main.by_guid):
            res["rt_only"].append(a)

    # main-only paths whose folder also exists on royal-throne: a folder-level
    # `git restore --source=<rt> -- <folder>` (no-overlay by default) deletes them.
    for a in sorted(main.metas):
        if a in rt.metas:
            continue
        parent = a.rsplit("/", 1)[0]
        under = prefixes is None or any(a.startswith(pre + "/") for pre in prefixes)
        if parent in rt.dirs and under:
            res["main_only_in_rt_dirs"].append(a)

    return main, rt, rt_sel, res


# Royal-throne files that the plan does not import (plan §2 out of scope, §5, Task 3.6 Step 1,
# Fact 7: no scene comes across). A reference from one of these does not make
# royal-throne's version of an asset necessary.
NOT_IMPORTED = [
    re.compile(r"^ProjectSettings/"),
    re.compile(r"\.unity$"),
    re.compile(r"^Assets/ReferenceGameplay/Home/(?!Scripts/ReferenceWinFinish\.cs$)"),
    re.compile(r"^Assets/ReferenceGameplay/Blockers/"),
    re.compile(r"levels01-50"),
    re.compile(r"RoyalCampaignArt"),
    re.compile(r"^Assets/Characters/"),
    # AllIn1 demo prefabs/materials/scenes: Task 2.2 imports only the textures royal-throne's toolkit materials use.
    re.compile(r"^Assets/Plugins/AllIn1VfxToolkit/Demo & Assets/.*\.(prefab|mat|unity|asset|cs|controller|anim)$"),
    re.compile(r"^Assets/JellyGarden/Resources/RoyalTNT/"),
    re.compile(r"^Assets/JellyGarden/Resources/RoyalGameplay/(Music|Win)/"),
    re.compile(r"RoyalHomeScreen|RoyalTitleScreen|RoyalReferenceCampaign|RoyalCampaign|RoyalWinSequence|RoyalEconomy"),
    # Booster generations the plan does not port (§3, §5: Propeller V5, ColorBall V4 and V5 only).
    re.compile(r"^Assets/Boosters/Propeller/V[1-4]/"),
    re.compile(r"^Assets/Boosters/ColorBall/V[1-3]/"),
]


# Paths whose side the plan already fixes, whoever references them.
PATH_RULES = [
    (re.compile(r"^Assets/JellyGarden/Resources/Levels/"), "main",
     "King Leo's campaign level (plan Fact 5, D3, D8): keep main's"),
    (re.compile(r"^Assets/JellyGarden/Resources/Targets/"), "main",
     "goal asset paired with main's level file (plan Fact 4, D8): keep main's"),
    (re.compile(r"\.unity$"), "main",
     "scene stays main's; royal-throne builds its level UI at runtime (plan Fact 7)"),
    (re.compile(r"^Assets/JellyGarden/Resources/Prefabs/(Item|Block|SolidBlock|ThrivingBlock|UndestrBlock|WireBlock)\.prefab$"),
     "main", "kit prefab stays main's; RoyalPieceSkin/RoyalBlockSkin are added at runtime instead (plan Task 3.6 Step 2)"),
]
# Main scripts the plan retires (§5 "Retired", Task 3.7).
RETIRED_SCRIPTS = set("""
StoryOriginalBoosters StoryOriginalBoostersValidation StoryOriginalTnt StoryOriginalTntPiece StoryOriginalTntValidation
StoryOriginalColorBall StoryOriginalMotion StoryOriginalChargedTarget StoryOriginalPiece StoryRocketVfx StoryRocketSweep
StoryRocketFlight StoryRocketVariant StoryRocketValidation StoryBombVfx StoryBombVariant StoryColorBombVfx StoryBlastVfx
StoryShatterVfx StoryPunchVfx StoryPopVfx StoryRoyalVfx StoryExplosionProbe StoryVfxLab StoryVfxVariant StoryFinishVfx
StoryBoardScale StoryBoardCentre StoryPixelSnap StorybookBoardLayout StoryRoyalBoardFrame StorybookRocketFacing
StoryRoyalBoardValidation StoryPowerupTest RocketValidationBridge RocketFlameGapCheck
""".split())
# Importer fields that do not change what King Leo ships (StoryBuild targets iOS and macOS only).
INERT_SETTINGS = ("androidOverrides",)


def path_rule(path):
    rule = KIT_SCRIPT_RULES.get(path)
    if rule:
        return rule
    for rx, side, why in PATH_RULES:
        if rx.search(path):
            return side, why
    return None


def imported(path, prefixes=None):
    """Would the port bring this royal-throne file across?"""
    if any(rx.search(path) for rx in NOT_IMPORTED):
        return False
    return selected(path, prefixes) if prefixes is not None else True


def kit_state(kit, side, p):
    """(asset, meta) each 'kit' (unchanged from a82c4df), 'changed', or 'new' (path not in the kit)."""
    if p not in kit.metas:
        return "new", "new"
    meta = "kit" if side.metas.get(p) == kit.metas[p] else "changed"
    if side.kind(p) == "folder" and kit.kind(p) == "folder":
        asset = "kit"
    else:
        asset = "kit" if side.asset_oid(p) and side.asset_oid(p) == kit.asset_oid(p) else "changed"
    return asset, meta


def changed_by(km, kr):
    """Who moved away from the kit, for the asset and for the .meta."""
    if km[0] == "new":
        return "not in kit"
    parts = []
    for label, a, b in (("asset", km[0], kr[0]), ("meta", km[1], kr[1])):
        who = [n for n, st in (("main", a), ("rt", b)) if st == "changed"]
        if who:
            parts.append("%s: %s" % (label, "+".join(who)))
    return "; ".join(parts) or "—"


def users_of(idx, guid, path, own_paths=()):
    own = set(own_paths)
    if path:
        own |= {path, path + ".meta"}
    refs = idx.users_of_guid(guid, own)
    code, code_dir = idx.code_users(path) if path else ([], [])
    return {"guid": refs, "code": code, "code_dir": code_dir}


def flat_users(u):
    return (u["guid"] + ["(code) " + x for x in u["code"]] + ["(code, folder load) " + x for x in u["code_dir"]])


def normalise_text(data):
    """Bytes with BOM and line-ending differences removed, or None for binary data."""
    if b"\0" in data[:8192]:
        return None
    if data.startswith(b"\xef\xbb\xbf"):
        data = data[3:]
    return b"\n".join(line.rstrip() for line in data.replace(b"\r\n", b"\n").replace(b"\r", b"\n").split(b"\n")).rstrip()


def strip_tag(u):
    return u.split(") ", 1)[1] if u.startswith("(") else u


def enrich(main, rt, kit, res, prefixes=None):
    """Reference users for (b), (c), (d); class and recommended side for (b)."""
    rt_idx, main_idx = RefIndex(rt), RefIndex(main)
    b = res["b"]
    wanted = []
    for e in b:
        if not e["asset_same"] and e["main_kind"] == e["rt_kind"] == "file":
            if max(main.asset_size(e["path"]), rt.asset_size(e["path"])) <= 8 * 1024 * 1024:
                wanted += [main.asset_oid(e["path"]), rt.asset_oid(e["path"])]
    contents = cat_blobs(wanted)
    for e in b:
        p = e["path"]
        e["eol_only"] = False
        if not e["asset_same"] and main.asset_oid(p) in contents and rt.asset_oid(p) in contents:
            na, nb = normalise_text(contents[main.asset_oid(p)]), normalise_text(contents[rt.asset_oid(p)])
            e["eol_only"] = na is not None and na == nb
        e["rt_users"] = users_of(rt_idx, e["guid"], p)
        e["main_users"] = users_of(main_idx, e["guid"], p)
        km, kr = kit_state(kit, main, p), kit_state(kit, rt, p)
        e["kit_main"], e["kit_rt"] = km, kr
        e["changed_by"] = changed_by(km, kr)
        e["class"] = "texture/meta reimport only" if e["asset_same"] else (
            "content differs (line endings only)" if e["eol_only"] else "content differs")
        what = []
        if not e["asset_same"]:
            if e["eol_only"]:
                what.append("asset line endings / BOM / trailing whitespace only (text identical)")
            elif e["main_kind"] == e["rt_kind"] == "file":
                what.append("asset bytes (main %s B, royal-throne %s B)" % (main.asset_size(p), rt.asset_size(p)))
            else:
                what.append("asset kind %s -> %s" % (e["main_kind"], e["rt_kind"]))
        e["meta_settings"], e["sub_ids_lost"], e["meaningful"] = [], [], []
        e["reserialised"], e["importer_change"] = False, None
        if not e["meta_same"]:
            reser, change, diffs = compare_metas(main.meta_bytes[p], rt.meta_bytes[p])
            e["reserialised"], e["importer_change"], e["meta_settings"] = reser, change, diffs
            e["meaningful"] = [d for d in diffs if d.split(" ", 1)[0] not in INERT_SETTINGS]
            if change:
                what.append("importer %s" % change)
            if reser:
                what.append(".meta re-serialised (%s)" % ("main = kit format, royal-throne rewritten by Unity 6"
                                                          if km[1] == "kit" else "both rewritten"))
            if diffs:
                what.append("settings main -> royal-throne: " + "; ".join(diffs[:5])
                            + (" (+%d more)" % (len(diffs) - 5) if len(diffs) > 5 else ""))
            elif not change and not reser:
                what.append(".meta bytes only (no field value differs)")
            ids_m, ids_r = sub_asset_ids(main.meta_bytes[p]), sub_asset_ids(rt.meta_bytes[p])
            e["sub_ids_lost"] = sorted(n for n, fid in ids_m.items() if ids_r.get(n) != fid)
            if e["sub_ids_lost"]:
                what.append("sprite fileIDs differ for %d of main's sub-assets (%s)"
                            % (len(e["sub_ids_lost"]), ", ".join(e["sub_ids_lost"][:3])))
        e["what"] = what

    # C#: members royal-throne added to a shared script, and royal-throne code that uses them.
    cs_entries = [e for e in b if e["path"].endswith(".cs") and not e["asset_same"] and not e["eol_only"]
                  and e["kit_rt"][0] != "kit"]
    srcs = cat_blobs([x for e in cs_entries for x in (main.asset_oid(e["path"]), rt.asset_oid(e["path"]),
                                                      kit.asset_oid(e["path"])) if x])
    for e in b:
        e["rt_added_used"] = {}
    for e in b:
        e["main_added_used"] = {}
    for e in cs_entries:
        p = e["path"]
        kit_src = srcs.get(kit.asset_oid(p)) if kit.asset_oid(p) else None
        added = added_members(srcs[main.asset_oid(p)], srcs[rt.asset_oid(p)], kit_src)
        e["rt_added"] = sorted(added)
        e["_added"] = added
        if e["kit_main"][0] != "kit":
            # main's side: public members main added that main-only code uses (plan §4 keep-lists).
            m_added = added_members(srcs[rt.asset_oid(p)], srcs[main.asset_oid(p)], kit_src)
            hits = collections.defaultdict(list)
            for u, toks in main_idx.cs_tokens.items():
                if u != p and u not in rt.blobs:
                    for t in m_added & toks:
                        hits[t].append(u)
            e["main_added_used"] = {t: sorted(v) for t, v in sorted(hits.items())}

    # Decide sides. A royal-throne user only counts when the port brings that user's
    # royal-throne version across: a royal-throne-only file, or a shared file whose
    # royal-throne bytes are taken. Iterate, since users are themselves list (b) entries.
    by_path = {e["path"]: e for e in b}
    side_of = {}
    for e in b:
        rule = path_rule(e["path"])
        side_of[e["path"]] = rule[0] if rule else "main"

    def live(e):
        out = []
        for u in e["rt_users"]["guid"] + e["rt_users"]["code"]:
            if not imported(u, prefixes):
                continue
            if u not in main.blobs:
                out.append(u)
            elif main.blobs[u][0] == rt.blobs.get(u, (None,))[0]:
                continue                              # identical on main: not royal-throne-specific
            elif side_of.get(u) == "royal-throne":
                out.append(u)
        return out

    def code_taken(u):
        if u.endswith(".cs") and imported(u, prefixes):
            if u not in main.blobs:
                return True
            return side_of.get(u) in ("royal-throne", "merge")
        return False

    for _ in range(10):
        changed = False
        for e in b:
            if e.get("_added"):
                hits = collections.defaultdict(list)
                for u, toks in rt_idx.cs_tokens.items():
                    if u != e["path"] and code_taken(u):
                        for t in e["_added"] & toks:
                            hits[t].append(u)
                e["rt_added_used"] = {t: sorted(v) for t, v in sorted(hits.items())}
            decide(e, live(e))
            asset_side = e["asset_side"] or e["meta_side"]
            if path_rule(e["path"]) is None and side_of[e["path"]] != asset_side:
                side_of[e["path"]] = asset_side
                changed = True
        if not changed:
            break
    for e in b:
        e["rt_live_users"] = live(e)
        e.pop("_added", None)

    for e in res["c"]:
        own = e["main_paths"] + e["rt_paths"]
        e["main_users"] = users_of(main_idx, e["guid"], None, own)
        e["rt_users"] = users_of(rt_idx, e["guid"], None, own)
        e["code_users"] = {}
        for p in e["main_paths"]:
            e["code_users"]["main:" + p] = main_idx.code_users(p)
        for p in e["rt_paths"]:
            e["code_users"]["rt:" + p] = rt_idx.code_users(p)
    for e in res["d"]:
        e["main_users"] = users_of(main_idx, e["main_guid"], e["path"])
        e["rt_users"] = users_of(rt_idx, e["rt_guid"], e["path"])
    for e in res["c_rt_dup"]:
        e["rt_users"] = users_of(rt_idx, e["guid"], None, e["paths"])


def decide(e, live_users):
    """Pick a side for the asset bytes and for the .meta of one list (b) entry."""
    km, kr = e["kit_main"], e["kit_rt"]
    main_n = len(flat_users(e["main_users"]))
    rule = path_rule(e["path"])
    notes = []
    if rule:
        a_side = None if e["asset_same"] else rule[0]
        m_side = None if e["meta_same"] else rule[0]
        why = rule[1]
    elif e["main_kind"] == "folder":
        a_side, m_side, why = None, "main", "folder .meta, same GUID: keep main's"
    else:
        reasons = []
        if e["asset_same"]:
            a_side = None
        elif e["eol_only"]:
            a_side = "main"
            reasons.append("asset: only line endings differ, keep main's")
        elif kr[0] == "kit":
            a_side = "main"
            reasons.append("asset: royal-throne's bytes are the untouched kit's")
        elif e.get("rt_added_used"):
            used = e["rt_added_used"]
            files = sorted({f for v in used.values() for f in v})
            a_side = "merge" if km[0] == "changed" else "royal-throne"
            reasons.append("asset: imported royal-throne code uses member(s) royal-throne added here (%s; in %s)%s"
                           % (", ".join(list(used)[:4]), ", ".join(f.rsplit("/", 1)[-1] for f in files[:3])
                              + (" +%d" % (len(files) - 3) if len(files) > 3 else ""),
                              "; main changed it too, so merge" if km[0] == "changed" else ""))
        elif live_users:
            a_side = "royal-throne"
            reasons.append("asset: %d imported royal-throne file(s) use it" % len(live_users))
            if km[0] in ("changed", "new") and main_n:
                notes.append("CHECK: main has its own version and %d main file(s) use it" % main_n)
        else:
            a_side = "main"
            reasons.append("asset: no imported royal-throne file uses it")
        if e["meta_same"]:
            m_side = None
        elif a_side in ("royal-throne", "merge"):
            m_side = "royal-throne"
            reasons.append("meta: importer settings follow royal-throne's bytes")
        elif not e["meaningful"] and not e["importer_change"] and not e["sub_ids_lost"]:
            m_side = "main"
            reasons.append("meta: no setting that matters for iOS/macOS differs (re-serialisation%s)"
                           % (" + Android override" if e["meta_settings"] else ""))
        elif kr[1] == "kit":
            m_side = "main"
            reasons.append("meta: royal-throne's .meta is the kit's")
        elif km[1] == "kit" and live_users:
            m_side = "royal-throne"
            reasons.append("meta: main never changed it; %d imported royal-throne file(s) use it" % len(live_users))
        else:
            m_side = "main"
            reasons.append("meta: main's settings (%s)" % ("both changed it" if km[1] == "changed"
                                                            else "no imported royal-throne user"))
        why = "; ".join(reasons)
    if rule and e.get("rt_added_used") and rule[0] == "main":
        used = e["rt_added_used"]
        files = sorted({f.rsplit("/", 1)[-1] for v in used.values() for f in v})
        notes.append("CHECK (Task 1.3): imported royal-throne code uses member(s) only royal-throne's version declares: "
                     "%s (in %s)" % (", ".join(used), ", ".join(files[:4]) + (" +%d" % (len(files) - 4) if len(files) > 4 else "")))
    if a_side in ("royal-throne", "merge") and e.get("main_added_used"):
        used = e["main_added_used"]
        files = sorted({f.rsplit("/", 1)[-1] for v in used.values() for f in v})
        kept = [f for f in files if f[:-3] not in RETIRED_SCRIPTS]
        if kept:
            notes.append("main code the plan keeps uses member(s) main added: %s (in %s); re-apply them"
                         % (", ".join(list(used)[:8]) + (" +%d" % (len(used) - 8) if len(used) > 8 else ""),
                            ", ".join(kept[:6]) + (" +%d" % (len(kept) - 6) if len(kept) > 6 else "")))
        else:
            notes.append("info: main-added member(s) %s are used only by scripts plan §5 retires (%s)"
                         % (", ".join(used), ", ".join(files)))
    if m_side == "royal-throne" and e["sub_ids_lost"]:
        notes.append("BREAKS main references to sprite fileIDs of %s unless remapped" % ", ".join(e["sub_ids_lost"][:3]))
    e["asset_side"], e["meta_side"] = a_side, m_side
    sides = {x for x in (a_side, m_side) if x}
    if len(sides) == 1:
        e["side"] = sides.pop()
    else:
        e["side"] = "asset %s, meta %s" % (a_side, m_side)
    e["why"] = why + ("; " + "; ".join(notes) if notes else "")


# ---------------------------------------------------------------- report

def md_list(paths, limit=None):
    shown = paths if limit is None else paths[:limit]
    s = "<br>".join("`%s`" % p for p in shown)
    if limit is not None and len(paths) > limit:
        s += "<br>... +%d more" % (len(paths) - limit)
    return s or "—"


def breakdown(paths, depth=2):
    c = collections.Counter(top_folder(p, depth) for p in paths)
    return sorted(c.items(), key=lambda kv: (-kv[1], kv[0]))


def c_kind(e):
    oids = e["oids"]
    if e["rt_only_paths"] and not e["main_only_paths"]:
        return "extra copy on royal-throne (main's path kept)"
    if e["rt_only_paths"] and e["main_only_paths"]:
        rt_oids = {oids[("rt", p)] for p in e["rt_only_paths"]}
        main_oids = {oids[("main", p)] for p in e["main_only_paths"]}
        if rt_oids == main_oids:
            return "moved (same bytes)"
        return "moved (bytes differ)"
    return "main-only path"


def md_escape(text):
    return text.replace("|", "\\|")


def fmt_users(u, limit=3):
    shown = u["guid"][:limit] + ["(code) " + x for x in u["code"][:max(1, limit - len(u["guid"][:limit]))]]
    hidden = len(u["guid"]) + len(u["code"]) - len(shown)
    out = "<br>".join("`%s`" % x for x in shown)
    if hidden > 0:
        out += "<br>+%d more" % hidden
    if u["code_dir"]:
        out += ("<br>" if out else "") + "+%d folder-load" % len(u["code_dir"])
    return out or "—"


def d_users(u):
    parts = []
    if u["guid"]:
        parts.append("GUID users: " + md_list(u["guid"], 3))
    else:
        parts.append("no GUID users")
    if u["code"]:
        parts.append("path-string users: " + md_list(u["code"], 2))
    return "<br>".join(parts)


def c_advice(e):
    rt_new = ", ".join("`%s`" % p for p in e["rt_only_paths"]) or "—"
    main_old = ", ".join("`%s`" % p for p in e["main_only_paths"]) or "—"
    rt_used = bool(flat_users(e["rt_users"])) or any(v[0] for k, v in e["code_users"].items() if k.startswith("rt:"))
    main_code = sorted({x for k, v in e["code_users"].items() if k.startswith("main:") for x in v[0]})
    if not e["rt_only_paths"]:
        return ("Nothing: royal-throne no longer has this GUID at main's path.", "No action.")
    breaks = ("If %s is restored next to main's %s, Unity re-GUIDs one of them on import. If main's keeps the GUID, "
              "the royal-throne copy is orphaned; if the royal-throne copy wins (it imports first, or main's .meta is "
              "regenerated), every main reference to the GUID silently moves to royal-throne's bytes." % (rt_new, main_old))
    if main_code:
        breaks += " Main loads its copy by Resources path from %s." % ", ".join("`%s`" % x.rsplit("/", 1)[-1] for x in main_code)
    if not rt_used:
        fix = ("Nothing on royal-throne references %s by GUID or by path string, so do not import it: exclude it from any "
               "`git restore` of its folder (restore file by file, or `git rm --cached` it right after). Keep main's file "
               "and GUID." % rt_new)
    else:
        fix = ("Keep main's path and GUID. If royal-throne's bytes are wanted, copy them over main's file (GUID "
               "unchanged) instead of restoring royal-throne's path; royal-throne GUID references then resolve to it. "
               "If both copies are needed, import royal-throne's with a new GUID and rewrite its royal-throne users.")
    if e["main_only_paths"] and main_code:
        fix += " Main's Resources copy becomes unused when its loader is retired (plan §5, Task 3.7); keep it (no-deletion rule)."
    return breaks, fix


def d_advice(e):
    main_guid_users, rt_guid_users = e["main_users"]["guid"], e["rt_users"]["guid"]
    if e["main_kind"] == e["rt_kind"] == "folder":
        if not main_guid_users and not rt_guid_users:
            breaks = ("Nothing references either folder GUID. Only the folder's own `%s.meta` is affected: restoring "
                      "royal-throne's version would change the folder GUID in git (churn, no Missing references)." % e["path"])
        else:
            breaks = ("Folder GUID referenced (DefaultAsset fields): restoring royal-throne's `%s.meta` breaks main's "
                      "users; keeping main's breaks royal-throne's." % e["path"])
        fix = ("Keep main's `%s.meta`. Restore royal-throne content by sub-path (`git restore --source=origin/royal-throne "
               "-- %s/<sub-path>`), which never touches the sibling `%s.meta`; never pass `%s.meta` itself."
               % (e["path"], e["path"], e["path"], e["path"]))
        if rt_guid_users:
            fix += " Rewrite `%s` to `%s` in: %s." % (e["rt_guid"], e["main_guid"], ", ".join("`%s`" % x for x in rt_guid_users))
        return breaks, fix
    breaks = ("Taking royal-throne's .meta breaks %d main GUID user(s); keeping main's breaks %d royal-throne GUID user(s)."
              % (len(main_guid_users), len(rt_guid_users)))
    fix = ("Keep main's .meta (GUID `%s`). For every imported royal-throne file that references `%s`, rewrite the GUID to "
           "`%s` (`git grep %s origin/royal-throne` lists them)." % (e["main_guid"], e["rt_guid"], e["main_guid"], e["rt_guid"]))
    return breaks, fix


def render(main, rt, kit, rt_sel, res, prefixes):
    out = []
    w = out.append

    def short(ref):
        return git("rev-parse", "--short", ref).decode().strip()

    shared = len(res["a"]) + len(res["b"]) + len(res["d"]) + len(res["no_guid"])
    w("# GUID audit: %s vs %s" % (main.ref, rt.ref))
    w("")
    w("Generated by `python3 tools/port_guid_audit.py%s` (plan Task 1.2). Do not edit by hand; re-run the tool. "
      "Source of truth is git objects only (`git ls-tree -r`, `git cat-file --batch`); the working tree is not read."
      % (" --paths-file ..." if prefixes is not None else ""))
    w("")
    w("- main: `%s` = `%s`, %d .meta files under `Assets/`." % (main.ref, short(main.ref), len(main.metas)))
    w("- royal-throne: `%s` = `%s`, %d .meta files under `Assets/`; %d selected%s."
      % (rt.ref, short(rt.ref), len(rt.metas), len(rt_sel),
         "" if prefixes is None else " by --paths-file (%s)" % ", ".join("`%s`" % p for p in prefixes)))
    w("- kit ancestor: `%s` (used for \"changed vs kit\")." % short(kit.ref))
    w("- \"Same bytes\" compares git blob ids, for the asset and for its .meta separately. A folder compares its .meta only.")
    w("")
    w("## Counts")
    w("")
    w("| List | Meaning | Count |")
    w("|---|---|---|")
    w("| — | paths with a .meta on both refs | %d |" % shared)
    w("| (a) | same path, same GUID, identical asset + .meta | %d |" % len(res["a"]))
    nb = collections.Counter(e["class"] for e in res["b"])
    w("| (b) | same path, same GUID, different bytes | %d (%d content differs, %d line endings only, %d texture/meta reimport only) |"
      % (len(res["b"]), nb["content differs"], nb["content differs (line endings only)"], nb["texture/meta reimport only"]))
    w("| (c) | same GUID at different paths, across refs | %d |" % len(res["c"]))
    w("| (c) | duplicate GUID inside royal-throne | %d |" % len(res["c_rt_dup"]))
    w("| (c, info) | duplicate GUID inside main | %d |" % len(res["c_main_dup"]))
    w("| (d) | same path, different GUID | %d |" % len(res["d"]))
    w("| — | path differs only by letter case across refs | %d |" % len(res["case"]))
    w("| — | .meta without a GUID line on either side | %d |" % len(res["no_guid"]))
    w("| — | royal-throne-only path, GUID unknown to main (imports cleanly) | %d |" % len(res["rt_only"]))
    w("")

    w("## (a) Identical: %d" % len(res["a"]))
    w("")
    w("| Folder | Count |")
    w("|---|---|")
    for k, v in breakdown([e["path"] for e in res["a"]]):
        w("| `%s` | %d |" % (k, v))
    w("")

    b = res["b"]
    w("## Key findings")
    w("")
    sides = collections.Counter(e["side"] for e in b)
    rt_rows = [e for e in b if e["side"] != "main"]
    rule_rows = [e for e in rt_rows if path_rule(e["path"])]
    w("- (b): %d entries. Take main's for %d; royal-throne's for %d; merge/split for %d. "
      % (len(b), sides["main"], sides["royal-throne"], len(b) - sides["main"] - sides["royal-throne"])
      + ("No shared path needs royal-throne's bytes or importer settings." if not rt_rows else
         "All %d non-main entries are kit scripts that plan §4 already assigns; no royal-throne asset bytes or importer "
         "settings are needed for any other shared path." % len(rt_rows) if len(rule_rows) == len(rt_rows)
         else "%d non-main entries come from reference analysis, not plan §4 (see rows without a `plan §4` reason)."
         % (len(rt_rows) - len(rule_rows))))
    eol = [e for e in b if e["eol_only"]]
    if eol:
        w("- %d (b) entries differ only in line endings/BOM/trailing whitespace (%s); they are no-ops."
          % (len(eol), ", ".join("`%s` %d" % kv for kv in breakdown([e["path"] for e in eol], 3))))
    reser = [e for e in b if e["asset_same"] and not e["meaningful"] and not e["importer_change"]]
    if reser:
        w("- %d texture/meta-only entries differ only by Unity 6 re-serialisation and/or an Android override "
          "(King Leo builds iOS and macOS only); they are no-ops." % len(reser))
    notes = [e for e in b if "CHECK" in e["why"] or "BREAKS" in e["why"] or "re-apply them" in e["why"]
             or "info: main-added" in e["why"]]
    for e in notes:
        w("- `%s` (take **%s**): %s" % (e["path"], e["side"], e["why"].split("; ", 1)[-1]))
    if res["c"] or res["d"] or any(len(e["selected"]) > 1 for e in res["c_rt_dup"]):
        w("- (c)/(d): %d + %d entries. Plan Task 1.2 Step 4 requires each to be resolved before Phase 3; the resolution "
          "for every entry is in its row below." % (len(res["c"]), len(res["d"])))
    else:
        w("- (c)/(d): empty. Nothing blocks Phase 3.")
    w("")

    w("## (b) Same path and GUID, different bytes: %d" % len(b))
    w("")
    w("- **texture/meta reimport only**: the asset blob is identical; only the .meta differs. Unity reimports with the "
      "chosen importer settings. Nothing referencing the asset breaks unless sprite fileIDs change (flagged per row; "
      "none found when this report was generated if no row says so).")
    w("- **content differs**: the asset bytes differ (the .meta may differ too). **content differs (line endings only)**: "
      "text identical after normalising CRLF/LF, BOM and trailing whitespace.")
    w("- **Changed vs kit**: which side moved away from `%s`, for the asset and the .meta separately; `not in kit` = "
      "both refs added the path independently (same GUID, e.g. the same Asset Store package)." % short(kit.ref))
    w("- **Take**, decided separately for the asset bytes and the .meta, in this order: a rule the plan already fixes "
      "(§4 kit scripts, campaign levels/targets, scenes, kit prefabs); folders; line-ending-only text; if royal-throne's "
      "copy is the untouched kit version, main's; if imported royal-throne C# uses public members only royal-throne's "
      "version declares, royal-throne's (merge when main changed it too); if an imported royal-throne file references "
      "the asset (GUID outside its own .meta, or a C#/JSON string naming its path or Resources key), royal-throne's; "
      "otherwise main's. Importer settings follow the chosen bytes; otherwise main's, unless main never touched the .meta "
      "and royal-throne's settings matter for iOS/macOS.")
    w("- \"Imported\" = everything on royal-throne except what the plan leaves behind: scenes, ProjectSettings, home and "
      "title screens, reference campaign and blockers, the King rig, `Resources/RoyalTNT`, `RoyalGameplay/Music|Win`, "
      "`RoyalWinSequence`/`RoyalEconomy`, Propeller V1-V4, ColorBall V1-V3, AllIn1 demo prefabs/materials%s. A royal-throne "
      "file identical on main does not count as a royal-throne user."
      % ("" if prefixes is None else ", and anything outside --paths-file"))
    w("- User columns: GUID users first, then `(code)` string-literal users; `+N folder-load` counts files with a literal "
      "naming a parent Resources folder (`Resources.LoadAll`, `\"dir/\" + name`; listed for information, not used to decide).")
    w("")
    w("### (b) summary")
    w("")
    w("| Folder | Class | Take main | Take royal-throne | Merge / split |")
    w("|---|---|---|---|---|")
    summ = collections.defaultdict(collections.Counter)
    for e in b:
        summ[(top_folder(e["path"], 3), e["class"])][e["side"]] += 1
    for (folder, cls), c in sorted(summ.items(), key=lambda kv: (-sum(kv[1].values()), kv[0])):
        other = sum(c.values()) - c["main"] - c["royal-throne"]
        w("| `%s` | %s | %d | %d | %d |" % (folder, cls, c["main"], c["royal-throne"], other))
    w("")
    for cls in ("content differs", "content differs (line endings only)", "texture/meta reimport only"):
        rows = sorted((e for e in b if e["class"] == cls), key=lambda e: e["path"])
        w("### (b) %s: %d" % (cls, len(rows)))
        w("")
        w("| # | Path | What differs | Changed vs kit | royal-throne users | main users | Take | Why |")
        w("|---|---|---|---|---|---|---|---|")
        for i, e in enumerate(rows, 1):
            w("| %d | `%s` | %s | %s | %s | %s | **%s** | %s |" % (
                i, e["path"], md_escape("; ".join(e["what"])), e["changed_by"],
                fmt_users(e["rt_users"]), fmt_users(e["main_users"]), e["side"], md_escape(e["why"])))
        w("")

    w("## (c) Same GUID at different paths")
    w("")
    w("### Across refs: %d" % len(res["c"]))
    w("")
    if res["c"]:
        w("Unity allows one asset per GUID. If both paths end up in the project, the one imported second is given a fresh "
          "GUID and every reference to the shared GUID resolves to the other one, silently. Restoring the royal-throne "
          "folder that contains the royal-throne path (`git restore --source=origin/royal-throne -- <folder>`) does exactly "
          "this.")
        w("")
        w("| # | GUID | main path | royal-throne path | Kind | Users | What breaks | Resolution |")
        w("|---|---|---|---|---|---|---|---|")
        for i, e in enumerate(res["c"], 1):
            users = []
            if flat_users(e["main_users"]):
                users.append("main GUID: " + md_list(flat_users(e["main_users"]), 3))
            if flat_users(e["rt_users"]):
                users.append("royal-throne GUID: " + md_list(flat_users(e["rt_users"]), 3))
            for k, v in e["code_users"].items():
                if v[0]:
                    users.append("code on %s: %s" % (k.split(":", 1)[0], md_list(v[0], 3)))
            breaks, fix = c_advice(e)
            w("| %d | `%s` | %s | %s | %s | %s | %s | %s |" % (
                i, e["guid"], md_list(e["main_paths"]), md_list(e["rt_paths"]), c_kind(e),
                "<br>".join(users) or "none", breaks, fix))
    else:
        w("None.")
    w("")
    w("### Duplicates inside royal-throne: %d" % len(res["c_rt_dup"]))
    w("")
    if res["c_rt_dup"]:
        w("Importing more than one path of a duplicate reproduces the collision in this project: the second gets a new "
          "GUID and its royal-throne users then point at the first. Import one path, or give the other a new GUID and "
          "remap its users.")
        w("")
        w("| # | GUID | royal-throne paths | Selected for import | royal-throne users |")
        w("|---|---|---|---|---|")
        for i, e in enumerate(res["c_rt_dup"], 1):
            w("| %d | `%s` | %s | %s | %s |" % (i, e["guid"], md_list(e["paths"]), md_list(e["selected"]),
                                              md_list(flat_users(e["rt_users"]), 4)))
    else:
        w("None.")
    w("")
    w("### Duplicates inside main (information): %d" % len(res["c_main_dup"]))
    w("")
    if res["c_main_dup"]:
        for e in res["c_main_dup"]:
            w("- `%s`: %s" % (e["guid"], ", ".join("`%s`" % p for p in e["paths"])))
    else:
        w("None.")
    w("")

    w("## (d) Same path, different GUID: %d" % len(res["d"]))
    w("")
    if res["d"]:
        w("Restoring royal-throne's .meta over main's changes the GUID of main's asset: every main file that references "
          "main's GUID goes Missing. Keeping main's .meta breaks every royal-throne file that references royal-throne's "
          "GUID, unless those references are rewritten. Path-string users (code that names the path) are unaffected by a "
          "GUID change and are listed only for completeness.")
        w("")
        w("| # | Path | Kind | main GUID / users | royal-throne GUID / users | What breaks | Resolution |")
        w("|---|---|---|---|---|---|---|")
        for i, e in enumerate(res["d"], 1):
            breaks, fix = d_advice(e)
            w("| %d | `%s` | %s/%s | `%s`<br>%s | `%s`<br>%s | %s | %s |" % (
                i, e["path"], e["main_kind"], e["rt_kind"], e["main_guid"], d_users(e["main_users"]),
                e["rt_guid"], d_users(e["rt_users"]), breaks, fix))
    else:
        w("None.")
    w("")

    mo = res["main_only_in_rt_dirs"]
    w("## Main-only paths inside folders royal-throne also has: %d" % len(mo))
    w("")
    w("`git restore --source=origin/royal-throne -- <folder>` runs in no-overlay mode by default: tracked files that the "
      "source tree lacks are **deleted**. Restoring any folder below wholesale deletes main's files in it (with their .meta, "
      "so their GUIDs and every reference to them). Restore royal-throne paths file by file or by the deepest folder that "
      "has no main-only file, or pass `--overlay`.")
    w("")
    w("| Folder | main-only paths |")
    w("|---|---|")
    for k, v in breakdown(mo, 4):
        w("| `%s` | %d |" % (k, v))
    w("")
    rest = [a for a in mo if not re.match(r"Assets/JellyGarden/Resources/(Levels|Targets)/", a)]
    if rest:
        w("Outside `Resources/Levels` and `Resources/Targets` (main's campaign levels and goals beyond the %d royal-throne also has):"
          % sum(1 for x in rt.blobs if x.startswith("Assets/JellyGarden/Resources/Levels/") and not x.endswith(".meta")))
        w("")
        for a in rest:
            if main.kind(a) == "folder":
                n = sum(1 for x in main.blobs if x.startswith(a + "/") and not x.endswith(".meta"))
                w("- `%s/` (folder, %d files)" % (a, n))
            else:
                w("- `%s`" % a)
        w("")

    w("## Paths that differ only by letter case: %d" % len(res["case"]))
    w("")
    if res["case"]:
        w("macOS (case-insensitive) and Unity treat these as one path; git treats them as two.")
        w("")
        w("| royal-throne path | main path | royal-throne GUID | main GUID |")
        w("|---|---|---|---|")
        for e in res["case"]:
            w("| `%s` | %s | `%s` | %s |" % (e["rt_path"], md_list(e["main_paths"]), e["rt_guid"],
                                          ", ".join("`%s`" % g for g in e["main_guids"])))
    else:
        w("None.")
    w("")
    if res["no_guid"]:
        w("## .meta without a GUID line: %d" % len(res["no_guid"]))
        w("")
        for e in res["no_guid"]:
            w("- `%s` (main `%s`, royal-throne `%s`)" % (e["path"], e["main_guid"] or "—", e["rt_guid"] or "—"))
        w("")

    w("## Royal-throne-only paths (new GUIDs): %d" % len(res["rt_only"]))
    w("")
    w("These import with their own .meta and cannot collide with main.")
    w("")
    w("| Folder | Count |")
    w("|---|---|")
    for k, v in breakdown(res["rt_only"], 3):
        w("| `%s` | %d |" % (k, v))
    w("")
    allin1 = [p for p in res["rt_only"] if p.startswith("Assets/Plugins/AllIn1VfxToolkit/") and rt.kind(p) == "file"]
    w("### `Assets/Plugins/AllIn1VfxToolkit` files only on royal-throne (input to plan Task 2.2): %d" % len(allin1))
    w("")
    for p in allin1:
        w("- `%s`" % p)
    w("")
    return "\n".join(out) + "\n"


def to_json(res):
    def clean(e):
        if isinstance(e, dict):
            return {k: clean(v) for k, v in e.items() if k != "oids"}
        if isinstance(e, (list, tuple, set)):
            return [clean(x) for x in e]
        return e
    return clean(res)


def main_cli(argv=None):
    ap = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    ap.add_argument("--main-ref", default="origin/main")
    ap.add_argument("--rt-ref", default="origin/royal-throne")
    ap.add_argument("--kit-ref", default=KIT_REF, help="common kit ancestor (default %(default)s)")
    ap.add_argument("--paths-file", help="file of royal-throne path prefixes (one per line) to restrict the audit to")
    ap.add_argument("--json", metavar="FILE", help="also write the raw lists as JSON to FILE")
    ap.add_argument("--repo", default=PROJECT_ROOT, help="git repository to read (default: this project)")
    args = ap.parse_args(argv)
    global REPO
    REPO = args.repo

    prefixes = load_prefixes(args.paths_file) if args.paths_file else None
    try:
        main, rt, rt_sel, res = audit(args.main_ref, args.rt_ref, prefixes)
        kit = Side(args.kit_ref)
        enrich(main, rt, kit, res, prefixes)
        report = render(main, rt, kit, rt_sel, res, prefixes)
    except GitError as exc:
        print("port_guid_audit: %s" % exc, file=sys.stderr)
        return 2
    sys.stdout.write(report)
    if args.json:
        with open(args.json, "w", encoding="utf-8") as fh:
            json.dump(to_json(res), fh, indent=1, sort_keys=True)
    blocking = len(res["c"]) + len(res["d"]) + sum(1 for e in res["c_rt_dup"] if len(e["selected"]) > 1)
    print("port_guid_audit: a=%d b=%d c=%d c_rt_dup=%d d=%d case=%d rt_only=%d"
          % (len(res["a"]), len(res["b"]), len(res["c"]), len(res["c_rt_dup"]), len(res["d"]),
             len(res["case"]), len(res["rt_only"])), file=sys.stderr)
    return 1 if blocking else 0


if __name__ == "__main__":
    sys.exit(main_cli())
