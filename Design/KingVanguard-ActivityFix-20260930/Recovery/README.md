# Activity fix host-file recovery

Captured on 2026-09-30 at 05:49 UTC from the independent `JellyGarden-Codex` working copy. This package contains two alternative recovery methods for three integration scripts. It does not contain the complete Unity project or the V2 character assets and scripts they depend on.

## Complete current-file overlay

[host-snapshots-20260930-054932.zip](host-snapshots-20260930-054932.zip) contains the complete current versions of these six files, using their exact project-relative restore paths:

- `Assets/JellyGarden/Scripts/GUI/RoyalGameplayPresentation.cs`
- `Assets/JellyGarden/Scripts/GUI/RoyalGameplayPresentation.cs.meta`
- `Assets/JellyGarden/Scripts/LevelManager.cs`
- `Assets/JellyGarden/Scripts/LevelManager.cs.meta`
- `Assets/JellyGarden/Scripts/Item.cs`
- `Assets/JellyGarden/Scripts/Item.cs.meta`

These snapshots include earlier and preexisting changes already present in the working files. They are an explicit recovery overlay, not a claim that every change in these files belongs to this activity fix. Nothing else is included in the ZIP.

The capture root was `C:\Users\chakm\Documents\Codex\2026-09-08\144\outputs\JellyGarden-Codex`. Restore only into that independent working copy or another matching copy of its project baseline. Save the destination files separately before overwriting them, and close Unity while applying the overlay. From the destination project root, verify the ZIP SHA-256, then extract its contents into the root so its `Assets` paths line up:

```powershell
Get-FileHash -Algorithm SHA256 -LiteralPath 'Design/KingVanguard-ActivityFix-20260930/Recovery/host-snapshots-20260930-054932.zip'
Expand-Archive -LiteralPath 'Design/KingVanguard-ActivityFix-20260930/Recovery/host-snapshots-20260930-054932.zip' -DestinationPath . -Force
```

Expected ZIP SHA-256: `75b1cd7f51cb8b84921c921b37bb2dc5306f67bd1dec6fb8969267af6969d6a4`.

## Focused source patches

The patches compare only the saved [Before/Source](../Before/Source) copies with the final current scripts. They are not generated against the working branch's `HEAD`, which also contains unrelated pending changes.

| Patch | Included task change |
| --- | --- |
| [RoyalGameplayPresentation.cs.patch](Patches/RoyalGameplayPresentation.cs.patch) | Scoped, bounded gameplay feedback queue; real match and powerup events; accepted-reaction handling; explicit extra-move/relief priority; gameplay portrait framing. |
| [LevelManager.cs.patch](Patches/LevelManager.cs.patch) | Report the real resolved match group to the character activity event adapter. |
| [Item.cs.patch](Patches/Item.cs.patch) | Report actual powerup creation from the two existing type-change paths. |

Apply these patches only when the destination script hashes match their `baselineSha256` entries in [manifest.json](manifest.json). The exact baseline files live under `Design/KingVanguard-ActivityFix-20260930/Before/Source/` with the same `Assets/...` suffixes. Matching the patch context alone is weaker than matching these saved baseline hashes. The `.meta` files are unchanged; no meta patch is needed. Their captured bytes and baseline provenance are recorded in the manifest.

From a project copy containing the exact saved source baseline, check and then apply all three patches. The explicit Git line-ending option preserves the mixed CRLF/LF bytes in the originals:

```powershell
$activityPatches = @(
  'Design/KingVanguard-ActivityFix-20260930/Recovery/Patches/RoyalGameplayPresentation.cs.patch'
  'Design/KingVanguard-ActivityFix-20260930/Recovery/Patches/LevelManager.cs.patch'
  'Design/KingVanguard-ActivityFix-20260930/Recovery/Patches/Item.cs.patch'
)
git -c core.autocrlf=false -c core.safecrlf=false apply --check --whitespace=nowarn @activityPatches
# Proceed only if the baseline hashes and the check above match.
git -c core.autocrlf=false -c core.safecrlf=false apply --whitespace=nowarn @activityPatches
```

Use either the overlay or the patches for these scripts; applying the patches after restoring the final overlay is unnecessary. The matching V2 package and task asset changes must also be present for compilation and the final character presentation.

## Verification and provenance

[verification.txt](verification.txt) records six archive-entry checks and three patch reconstruction checks. Every ZIP entry was compared byte for byte with the source snapshot, and each current source was checked again after capture. All three patches were checked and applied in an isolated temporary directory against the saved baseline, and the resulting SHA-256 hashes exactly matched the current scripts. All three metas match their documented baseline bytes.

[manifest.json](manifest.json) provides per-file sizes, SHA-256 hashes, baseline locations, restore paths, and the working/backup branch commit IDs observed at capture. [SHA256SUMS.txt](SHA256SUMS.txt) covers the ZIP, manifest, and patches. [build_recovery.py](build_recovery.py) is the capture and verification tool; rerunning it intentionally captures a new snapshot of the named working files.

Creating this package did not edit any runtime file or Unity asset, invoke Unity, or stage, commit, push, or update a Git ref. Root-level delivery work will decide what enters the existing private `king-vanguard-v2-backup` branch; this package alone does not publish anything.
