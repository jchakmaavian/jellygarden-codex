"""Static source/dependency audit; does not claim Unity or visual validation."""
import json
import re
from datetime import datetime, timezone
from pathlib import Path

project = Path(__file__).resolve().parents[3]
assets = project / "Assets"
v1 = assets / "Characters/KingVenguard/V1"
v2 = assets / "Characters/KingVenguard/V2"
out = project / "Design/KingVanguard-Reference-20260929/source-consistency.json"
findings = []
mounts = []
scanned = 0

patterns = {
    "legacy_character_class": re.compile(r"\bKingVenguard(?:Character|Slot)\b"),
    "legacy_prefab_resource": re.compile(r'"KingVenguard/KingVenguard"'),
    "legacy_static_king_load": re.compile(r'(?:CampaignArt|Art|Load(?:<[^>]+>)?)\s*\(\s*"(?:[^"\n]*/)?(?:king[-_](?:idle|cheer|think)|king-venguard-excited|avatar-king)"', re.I),
    "legacy_king_still_literal": re.compile(r'"(?:RoyalLeaderboard/)?king-venguard-excited"', re.I),
}
for path in assets.rglob("*.cs"):
    if "Editor" in path.parts or "Spine" in path.parts or path.is_relative_to(v1):
        continue
    scanned += 1
    source = path.read_text(encoding="utf-8-sig", errors="replace")
    for line_no, line in enumerate(source.splitlines(), 1):
        # Remove comments, while retaining string literals relevant to the load patterns.
        code = line.split("//", 1)[0]
        for rule, pattern in patterns.items():
            if pattern.search(code):
                findings.append({"rule": rule, "file": path.relative_to(project).as_posix(), "line": line_no})
        if "KingVanguardSlot.Mount(" in code or "NativeKingDrillUiSlot.Mount(" in code:
            mounts.append({"file": path.relative_to(project).as_posix(), "line": line_no})

legacy_guids = {}
for path in v1.rglob("*.meta"):
    match = re.search(r"^guid:\s*([a-f0-9]{32})", path.read_text(encoding="utf-8", errors="replace"), re.M)
    if match:
        legacy_guids[match[1]] = path.relative_to(project).as_posix()
serialized_files = 0
for path in assets.rglob("*"):
    if path.suffix not in (".prefab", ".unity", ".asset") or path.is_relative_to(v1):
        continue
    serialized_files += 1
    content = path.read_text(encoding="utf-8", errors="replace")
    for guid in set(re.findall(r"guid:\s*([a-f0-9]{32})", content)) & legacy_guids.keys():
        findings.append({"rule": "serialized_v1_dependency", "file": path.relative_to(project).as_posix(), "target": legacy_guids[guid]})

required = {
    "RoyalGameplayPresentation.cs", "RoyalHomeScreen.cs", "RoyalMenuSkin.cs", "RoyalAreaView.cs", "RoyalLeaderboardPage.cs"
}
present = {Path(m["file"]).name for m in mounts}
for name in sorted(required - present):
    findings.append({"rule": "missing_v2_live_mount", "file": name})

result = {
    "generated_utc": datetime.now(timezone.utc).isoformat(),
    "status": "PASS" if not findings else "FAIL",
    "scope": "Runtime C# outside retained V1/Spine/Editor, plus serialized assets outside V1",
    "runtime_scripts_scanned": scanned,
    "serialized_files_scanned": serialized_files,
    "v2_mount_call_sites": mounts,
    "findings": findings,
    "limits": ["Static source and serialized-reference audit only; Unity lifecycle and visual checks are separate.",
               "Unreferenced legacy catalog keys/assets can remain archived; this audit rejects live direct loads and V1 dependencies."]
}
out.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
print(json.dumps({"status": result["status"], "runtime_scripts": scanned, "serialized_files": serialized_files,
                  "mounts": len(mounts), "findings": len(findings), "report": str(out)}))
raise SystemExit(0 if not findings else 1)
