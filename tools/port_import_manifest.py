#!/usr/bin/env python3
"""Build docs/port/import-paths.txt and the IMPORT-MANIFEST tables (plan Task 1.1).

Combines tools/port_code_closure.py (C# type closure) and
tools/port_asset_closure.py (Resources / guid / shader closure), classifies each
path against origin/main and the kit ancestor, and writes:

  docs/port/import-paths.txt     one repo-relative path per line (dir prefixes
                                 end with "/"), each with its .meta implied,
                                 grouped by "# code", "# editor-tests",
                                 "# third-party", "# assets". Only paths to be
                                 restored verbatim from origin/royal-throne.
  docs/port/import-closure.json  per-path evidence (group, size, main status,
                                 liveness, why it is in the closure)
  stdout                         markdown tables used in IMPORT-MANIFEST.md

Read-only on git; never touches Assets/.

    python3 tools/port_import_manifest.py [--out docs/port] [--tables tables.md]
"""
import argparse
import json
import os
import sys
from collections import defaultdict

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import port_code_closure as pcc  # noqa: E402
import port_asset_closure as pac  # noqa: E402

JS = pcc.JS
# plan §4: restore royal-throne's file, then cut couplings / re-apply main hooks
KIT_MERGE = {JS + p for p in ("LevelManager.cs", "Item.cs", "Square.cs", "AI.cs", "InputManager.cs",
                              "System/CombineManager.cs", "Targets/TargetLevel.cs", "Targets/TargetIcon.cs",
                              "Targets/TargetsGroup.cs", "GUI/BoostShop.cs", "GUI/BoostIcon.cs")}
# plan §4: keep main's file and hand-merge named royal-throne members
KIT_MANUAL = {JS + p for p in ("InitScript.cs", "GUI/AnimationManager.cs", "GUI/MenuManager.cs",
                               "SoundBase.cs", "MusicBase.cs")}
DEAD = {JS + "Juice/" + p for p in ("RoyalColorBallSequence.cs", "ColorBallBoostView.cs", "RoyalMatchBreakService.cs",
                                    "RoyalPropellerV2.cs", "RoyalPropellerPieceV2.cs", "RoyalPropellerV2Sequence.cs",
                                    "ColorBallFrameAnimator.cs", "UrpBloomSetup.cs")}
FALLBACK = {JS + "Juice/TNT3DAnimator.cs", JS + "Juice/TNTFrameAnimator.cs"}
QA_ONLY = {JS + "RoyalQaService.cs", JS + "Juice/RoyalRegressionRunner.cs"}
# Task 4.0's suite list; everything else in the test closure is optional
PLAN_SUITES = {"RoyalEffectsChecks", "RoyalHintPresentationChecks", "RoyalOverlapChecks",
               "RoyalPropellerFormationChecks", "RoyalSpecialFormationChecks", "RoyalFormationReferenceChecks",
               "RoyalMatchResponseChecks", "RoyalSquarePlusOneChecks", "RoyalBlockedRefillChecks",
               "RoyalSuperBombFuseChecks", "RoyalAllDualComboChecks", "RoyalDualColorBallChecks", "RoyalQaChecks",
               "RoyalColorBallQaChecks", "PropellerV5Checks", "ColorBallV4Checks", "ColorBallV5Checks",
               "RoyalInputRegression"}
OPTIONAL_TOOL_SUITES = ["Assets/ReferenceGameplay/Editor/ReferenceBoosterChecks.cs",
                        "Assets/ReferenceGameplay/Editor/ResponsiveInputChecks.cs",
                        "Assets/ReferenceGameplay/Editor/ReferenceLayoutChecks.cs",
                        "Assets/ReferenceGameplay/Editor/ReferenceHammerChecks.cs",
                        "Assets/ReferenceGameplay/Editor/ReferenceArcherChecks.cs",
                        "Assets/ReferenceGameplay/Editor/ReferenceCannonChecks.cs"]
KEEP_MAIN_NOTES = {JS + k: v for k, v in {
    "GUI/BoostAnimation.cs": "rt: ParticleSystem.main deprecation fix only (optional cherry-pick)",
    "GUI/LIFESAddCounter.cs": "rt: lives refill rewrite on RoyalSave (economy); keep main's",
    "GUI/Level.cs": "rt: PlayerPrefs -> RoyalSave alias only",
    "GUI/LifeShop.cs": "rt: RoyalEconomy hook (cut)",
    "GUI/PreFailed.cs": "rt: RoyalEconomy hook (cut)",
    "GUI/Tutorial.cs": "rt: RoyalCards / RoyalLessonDemo hooks (cut)",
    "GUI/Tutorial/HandTutorial.cs": "rt: null/NaN guards; main has its own edits here (+19/-3)",
    "GUI/Tutorial/TutorialManager.cs": "rt: RoyalReferenceCampaign hook (cut, D8)",
    "MapScripts/LevelsMap.cs": "rt: FindObjectsByType + teardown null guards (optional cherry-pick)",
    "MapScripts/MapCamera.cs": "rt: removed an unused field only",
    "MapScripts/PlayerPrefsMapProgressManager.cs": "rt: PlayerPrefs -> RoyalSave alias only",
    "UnityInAppsIntegration.cs": "rt: RoyalOfflineMode guard (offline build policy); keep main's",
}.items()}
HOLD_BACK = {
    "Assets/JellyGarden/Resources/RoyalGameplay/EffectsProfile.asset":
        "master switch; plan Task 3.6 step 3 holds it back, Task 4.1 restores it",
}
NOT_RECOMMENDED = {
    "Assets/JellyGarden/Scripts/Editor/RoyalPresentationChecks.cs":
        "needs RoyalLessonDemo (home lesson demo, cut) at RoyalPresentationChecks.cs:46,49",
}
WHOLE_FOLDERS = ["Assets/Boosters/Propeller/V5/", "Assets/Boosters/ColorBall/V4/", "Assets/Boosters/ColorBall/V5/",
                 "Assets/Spine/"]
THIRD_PARTY = ("Assets/Spine/", "Assets/Plugins/")


def blobs(ref):
    out = {}
    for line in pcc.git("ls-tree", "-r", "-l", ref).decode("utf-8", "replace").splitlines():
        meta, path = line.split("\t", 1)
        _, kind, oid, size = meta.split()
        if kind == "blob":
            out[path] = (oid, int(size) if size != "-" else 0)
    return out


def main_status(p, rt, mn):
    if p not in mn:
        return "absent"
    return "identical" if mn[p][0] == rt[p][0] else "different"


def compress(paths, rt_files, group_of, group):
    """Replace a set of files by the largest directories it covers completely."""
    chosen = set(paths)
    out = []
    dirs = defaultdict(set)
    for p in rt_files:
        if p.endswith(".meta"):
            continue
        parts = p.split("/")
        for k in range(2, len(parts)):
            dirs["/".join(parts[:k]) + "/"].add(p)
    covered = set()
    for d in sorted(dirs, key=len):
        members = dirs[d]
        if len(members) > 1 and members <= chosen and not (members & covered):
            if all(group_of.get(m) == group for m in members):
                out.append(d)
                covered |= members
    out += sorted(chosen - covered)
    return sorted(out)


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--out", default="docs/port")
    ap.add_argument("--tables", help="write markdown tables here instead of stdout")
    args = ap.parse_args()

    files, index = pcc.build(pcc.REF)
    code = pcc.compute(files, index)
    rt, mn = blobs(pcc.REF), blobs(pcc.MAIN)
    repo = pac.Repo(pcc.REF)
    runtime_cs = set(code["runtime"])
    test_cs = set(code["tests"])
    full = pac.run(repo, sorted(runtime_cs | test_cs), runtime_cs | test_cs, pac.DEFAULT_CUTS)
    rto = pac.run(repo, sorted(runtime_cs), set(runtime_cs), pac.DEFAULT_CUTS)
    assets_all = {p for p in full["included"] if not p.endswith(".cs")}
    assets_rt = {p for p in rto["included"] if not p.endswith(".cs")}
    extra_cs = {p for p in full["included"] if p.endswith(".cs")} - runtime_cs - test_cs  # e.g. AllIn1 demo scripts
    extra_cs |= set(full["scripts_outside"])

    rows = {}

    def put(p, group, live, note=""):
        if p.endswith(".meta") or p not in rt:
            return
        rows[p] = {"group": group, "live": live, "note": note, "size": rt[p][1],
                   "meta_size": rt.get(p + ".meta", ("", 0))[1], "main": main_status(p, rt, mn)}

    base = pcc.blob_map(pcc.BASE)
    for p in sorted(runtime_cs):
        k = pcc.kit_status(p, rt, base)
        if p.startswith(THIRD_PARTY):
            put(p, "third-party", "live")
        elif p in KIT_MERGE:
            put(p, "kit-merge", "live", "plan §4: restore royal-throne's, cut couplings, re-apply main hooks")
        elif p in KIT_MANUAL:
            put(p, "kit-manual", "live", "plan §4: keep main's, add the royal-throne members the closure uses")
        elif k == "kit-same" or (k == "kit-changed" and main_status(p, rt, mn) == "identical"):
            put(p, "kit-unchanged", "live", "main already identical")
        elif k == "kit-changed":
            put(p, "kit-keep-main", "live", KEEP_MAIN_NOTES.get(p, "keep main's"))
        else:
            live = "dead" if p in DEAD else "fallback" if p in FALLBACK else "qa-only" if p in QA_ONLY else \
                "editor" if "/Editor/" in p else "live"
            put(p, "code", live)
    # plan suites and what they need to compile
    plan_seeds = [p for p in sorted(test_cs | runtime_cs) if os.path.basename(p)[:-3] in PLAN_SUITES]
    rt_b, base_b = pcc.blob_map(pcc.REF), pcc.blob_map(pcc.BASE)
    pcc.follow_is_kit = lambda q: pcc.kit_status(q, rt_b, base_b) == "kit-same"
    plan_inc, _, _, _ = pcc.closure(files, index, plan_seeds, pcc.DEFAULT_CUTS,
                                    lambda q: q not in pcc.KEEP_MAIN and pcc.kit_status(q, rt_b, base_b) != "kit-same",
                                    stop_at=runtime_cs - set(plan_seeds))
    for p in sorted(test_cs) + OPTIONAL_TOOL_SUITES + sorted(NOT_RECOMMENDED):
        name = os.path.basename(p)[:-3]
        if p in runtime_cs and "/Editor/" in p and not p.startswith(("Assets/Boosters/",)):
            pass
        note = "plan Task 4.0" if name in PLAN_SUITES else "plan dep" if p in plan_inc else "optional"
        if p.startswith("Assets/Boosters/"):
            continue  # booster checks travel with their whole folder
        put(p, "editor-tests", "test", note)
    for p in sorted(extra_cs):
        if pcc.kit_status(p, rt, base) == "kit-same" and main_status(p, rt, mn) == "identical":
            put(p, "kit-unchanged", "live", "main already identical (script of a closure asset)")
        else:
            put(p, "third-party" if p.startswith(THIRD_PARTY) else "code", "live",
                "script of a closure asset" + (" (ScriptedImporter for .webp; needs FFmpeg)" if "WebP" in p else ""))
    for p in sorted(assets_all):
        g = "third-party" if p.startswith(THIRD_PARTY) else "assets"
        put(p, g, "live" if p in assets_rt else "test-only")
    # whole folders: everything under them, same group as their scripts
    for d in WHOLE_FOLDERS:
        g = "third-party" if d.startswith(THIRD_PARTY) else "code"
        for p in rt:
            if p.startswith(d) and not p.endswith(".meta") and p not in rows:
                put(p, g, "docs/source" if not p.endswith((".cs", ".prefab", ".asset", ".mat", ".shader", ".wav", ".png", ".anim", ".controller", ".mesh", ".asmdef", ".json")) else "unreferenced", "whole-folder import")
        for p in list(rows):
            if p.startswith(d) and rows[p]["group"] != "editor-tests":
                rows[p]["group"] = g

    # what goes into import-paths.txt: restore-verbatim groups, not identical on main,
    # third-party files main already has differently are left to the GUID audit
    def importable(p, r):
        if r["group"] not in ("code", "editor-tests", "third-party", "assets"):
            return False
        if r["main"] == "identical":
            return False
        if r["main"] == "different" and p.startswith("Assets/Plugins/"):
            return False   # AllIn1 shaders: CRLF-only differences; keep main's
        if r["main"] == "different" and r["group"] == "assets" and p.startswith("Assets/JellyGarden/Resources/") \
                and "/Royal" not in p and "/Juice" not in p:
            return False   # kit resources such as UnityAdsID.asset: keep main's
        return True

    group_of = {p: r["group"] for p, r in rows.items() if importable(p, r) and p not in HOLD_BACK}
    lines = []
    for g in ("code", "editor-tests", "third-party", "assets"):
        sel = [p for p, gg in group_of.items() if gg == g]
        if g in ("code", "third-party"):
            sel_set = set(sel)
            for d in WHOLE_FOLDERS:
                if ("third-party" if d.startswith(THIRD_PARTY) else "code") == g:
                    sel_set = {p for p in sel_set if not p.startswith(d)}
                    sel_set.add(d)
            out = sorted(x for x in sel_set if x.endswith("/")) + \
                compress([x for x in sel_set if not x.endswith("/")], rt, group_of, g)
        elif g == "editor-tests":
            lines.append("# editor-tests")
            lines.append("# plan Task 4.0 suites + their compile deps (RoyalEffectsChecks calls the Board*/CellBreak/ColorBall/")
            lines.append("# Rocket/TNT/Toolkit suites; RoyalRocketChecks needs RoyalVfxLab)")
            lines += sorted(x for x in sel if rows[x]["note"] in ("plan Task 4.0", "plan dep"))
            lines.append("# recommended extra: booster-bar tools (Task 4.8), input during cascades (Task 4.5), HUD layout (Task 4.6)")
            lines += sorted(x for x in sel if rows[x]["note"] == "optional" and x not in NOT_RECOMMENDED)
            for x in sorted(NOT_RECOMMENDED):
                lines.append(f"# not imported: {x}  ({NOT_RECOMMENDED[x]})")
            lines.append("")
            continue
        else:
            out = compress(sel, rt, group_of, g)
        lines.append(f"# {g}")
        lines += sorted(set(out))
        if g == "assets":
            for x, why_held in sorted(HOLD_BACK.items()):
                lines.append(f"# held back: {x}  ({why_held})")
        lines.append("")
    os.makedirs(args.out, exist_ok=True)
    with open(os.path.join(args.out, "import-paths.txt"), "w") as fh:
        fh.write("# royal-throne -> main import list (plan Task 1.1). Generated by tools/port_import_manifest.py.\n"
                 "# One repo-relative path per line on origin/royal-throne; a trailing / means the whole folder.\n"
                 "# Each path implies its .meta. Kit-core merge files (plan §4) are NOT listed: see IMPORT-MANIFEST.md.\n"
                 "# Paths byte-identical on origin/main are omitted (restoring them is a no-op).\n"
                 "# Also restore the ancestor folder .meta files listed in import-closure.json \"folder_metas\"\n"
                 "# (new folders keep their GUIDs). Booster folders contain a .gitignore with no .meta (expected).\n\n")
        fh.write("\n".join(lines))
    # ancestor folder .meta files a restore must also bring (new folders keep their GUIDs);
    # a "dir/" line already implies dir.meta and everything inside it
    imported, prefixes = set(), [ln for ln in lines if ln.endswith("/") and not ln.startswith("#")]
    for ln in lines:
        if ln and not ln.startswith("#"):
            imported |= {q for q in rt if q.startswith(ln) and not q.endswith(".meta")} if ln.endswith("/") else {ln}
    folder_metas, folder_meta_conflicts = set(), set()
    for q in imported:
        parts = q.split("/")
        for k in range(2, len(parts)):
            d = "/".join(parts[:k])
            m = d + ".meta"
            if m not in rt or any(d + "/" == pre or d.startswith(pre) for pre in prefixes):
                continue
            if m not in mn:
                folder_metas.add(m)
            elif mn[m][0] != rt[m][0]:
                folder_meta_conflicts.add(m)
    why = {**{k: v for k, v in full["parent"].items()}}
    with open(os.path.join(args.out, "import-closure.json"), "w") as fh:
        json.dump({"ref": pcc.REF, "main": pcc.MAIN, "base": pcc.BASE,
                   "paths": {p: {**r, "why": why.get(p, ["seed", "seed"])[0] if p in why else ""}
                             for p, r in sorted(rows.items())},
                   "asset_cuts": sorted(full["cuts"], key=lambda c: (c["to"], c["from"])), "code_cuts": code["runtime_cuts"] + code["test_cuts"],
                   "partial_parts_cut": code["partial_parts_cut"], "not_followed": code["not_followed"],
                   "narrowed_loads": sorted(list(x[:4]) for x in full["narrowed"]),
                   "catalog_dropped": sorted(full["catalog_dropped"]),
                   "folder_metas": sorted(folder_metas),
                   "folder_meta_conflicts": sorted(folder_meta_conflicts)}, fh, indent=1)

    # markdown tables
    out = []
    w = out.append

    def fmt(n):
        return f"{n:,}"
    tot = defaultdict(lambda: [0, 0, 0])
    for p, r in rows.items():
        t = tot[r["group"]]
        t[0] += 1
        t[1] += r["size"] + r["meta_size"]
        t[2] += (r["size"] + r["meta_size"]) if importable(p, r) else 0
    w("| Group | Files | Bytes (incl. .meta) | Bytes to import |\n|---|---:|---:|---:|")
    for g in ("code", "editor-tests", "third-party", "assets", "kit-merge", "kit-manual", "kit-keep-main", "kit-unchanged"):
        if g in tot:
            w(f"| {g} | {tot[g][0]} | {fmt(tot[g][1])} | {fmt(tot[g][2])} |")
    for g in ("code", "editor-tests", "kit-merge", "kit-manual", "kit-keep-main", "kit-unchanged"):
        w(f"\n#### {g}\n\n| Path | Bytes | Live | Main | Note |\n|---|---:|---|---|---|")
        for p, r in sorted(rows.items()):
            if r["group"] == g and not (g == "code" and any(p.startswith(d) for d in WHOLE_FOLDERS)):
                w(f"| `{p}` | {fmt(r['size'])} | {r['live']} | {r['main']} | {r['note']} |")
        if g == "code":
            for d in WHOLE_FOLDERS:
                if not d.startswith(THIRD_PARTY):
                    ms = [r for p, r in rows.items() if p.startswith(d)]
                    st = defaultdict(int)
                    for r in ms:
                        st[r["live"]] += 1
                    w(f"| `{d}` (whole folder, {len(ms)} files) | {fmt(sum(r['size'] for r in ms))} | "
                      f"{', '.join(f'{k} {v}' for k, v in sorted(st.items()))} | absent | scripts, prefabs, audio, docs |")
    for g in ("third-party", "assets"):
        w(f"\n#### {g} (by folder)\n\n| Folder | Files | Bytes | Live | Test-only | Main identical | Main different | Main absent |\n|---|---:|---:|---:|---:|---:|---:|---:|")
        agg = defaultdict(lambda: defaultdict(int))
        for p, r in rows.items():
            if r["group"] != g:
                continue
            parts = p.split("/")
            depth = 5 if p.startswith(("Assets/ReferenceGameplay/", "Assets/JellyGarden/Resources/",
                                       "Assets/Plugins/AllIn1VfxToolkit/")) else 3
            d = "/".join(parts[:min(depth, len(parts) - 1)]) + "/"
            a = agg[d]
            a["n"] += 1
            a["b"] += r["size"]
            a[r["live"]] += 1
            a[r["main"]] += 1
        for d, a in sorted(agg.items()):
            w(f"| `{d}` | {a['n']} | {fmt(a['b'])} | {a['live']} | {a['test-only']} | {a['identical']} | {a['different']} | {a['absent']} |")
    w("\n#### Ancestor folder .meta files to restore (absent on main)\n")
    w("```\n" + "\n".join(sorted(folder_metas)) + "\n```")
    if folder_meta_conflicts:
        w("\nFolder .meta present on both sides with different bytes (keep main's; GUID-AUDIT decides): " +
          ", ".join(f"`{m}`" for m in sorted(folder_meta_conflicts)))
    text = "\n".join(out) + "\n"
    if args.tables:
        with open(args.tables, "w") as fh:
            fh.write(text)
    else:
        print(text)
    n = sum(1 for ln in lines if ln and not ln.startswith("#"))
    print(f"wrote {args.out}/import-paths.txt ({n} entries) and import-closure.json", file=sys.stderr)


if __name__ == "__main__":
    sys.exit(main())
