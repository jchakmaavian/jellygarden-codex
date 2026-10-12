# Delivery and private-backup audit

Audit only. No staging, commit, push, Unity command, asset edit or player-save change was performed. Root owns the ongoing Play Mode checks.

## Backup boundary

`AGENTS.md` authorizes verified work to the private `jchakmaavian/jellygarden-codex` repository. Current origin matches that repository. HEAD at audit time is `b3bb51d`. The index was empty. Preserve the existing working tree; never use `git add -A`, reset, clean or a broad Design/Backup add.

The task checkpoint is `Design/Backup/ReferenceGameplay-Checkpoint-20260924-121611` (504 files, approximately 6.7 MB, no project caches). Its `status-before.txt` and `working-edits-before.patch` distinguish inherited unfinished work from this import. No second full-project backup is needed.

`ChangedFiles.txt` lists this task's edits, not a self-contained commit. Several required campaign adapters were already uncommitted at the checkpoint. Committing only the 13 listed files plus ReferenceGameplay would omit required types/fields and runtime hooks.

## Recommended explicit source scope

After final verification, include:

1. `Assets/ReferenceGameplay.meta` and all `Assets/ReferenceGameplay/`, including every existing `.meta`. Approximately 49.1 MB / 3,016 files at audit time. Generated dependency check found no unresolved GUIDs inside this tree.
2. The 13 existing files at the start of `ChangedFiles.txt`, with their existing metadata. Two of those, `RoyalReferenceCampaign.cs` and `RoyalCampaignBlockers.cs`, are currently untracked, so include their `.meta` files explicitly.
3. The following **inherited prerequisite changes**, identified separately in the commit description. These are already present and tested by the current task; do not rewrite them merely to make the commit look smaller:
   - `Assets/JellyGarden/Scripts/AI.cs`
   - `Assets/JellyGarden/Scripts/Juice/RoyalHintSolver.cs`
   - `Assets/JellyGarden/Scripts/LevelManager.cs`
   - `Assets/JellyGarden/Scripts/RoyalQaService.cs`
   - `Assets/JellyGarden/Scripts/RoyalSave.cs`
   - `Assets/JellyGarden/Scripts/Square.cs`
   - `Assets/JellyGarden/Scripts/System/CombineManager.cs`
   - `Assets/JellyGarden/Scripts/Targets/TargetLevel.cs`
   - `Assets/JellyGarden/Scripts/Targets/TargetsGroup.cs`
   - `Assets/JellyGarden/Scripts/RoyalCampaignItem.cs` and `.meta`
   - `Assets/JellyGarden/Scripts/RoyalCampaignPiece.cs` and `.meta`
   - `Assets/JellyGarden/Scripts/RoyalOfflineMode.cs` and `.meta` (existing LevelManager dependency; no services are enabled)
4. Required existing verification helpers, with `.meta`: `Assets/JellyGarden/Scripts/Editor/RoyalCampaignChecks.cs`, `RoyalCampaignBlockerChecks.cs`, and `RoyalHandoffCapture.cs`.

Do not stage the conditional `RoyalReferenceCampaignBootstrap` merely to back up this import: current Editor checks select the existing QA profile explicitly. That file is for a separate test build, and this task does not build an APK.

The preceding scope preserves unrelated local edits without claiming the committed subset reproduces those edits. HEAD already exposes the homepage fields/methods used by RoyalReferenceCampaign, so the pre-existing homepage redesign is not a compilation dependency. The `RoyalCampaignArt` folder is a retained fallback, not a required native gameplay dependency. Do not sweep either into the task commit.

## Required evidence and editable-import inputs

Include selected files rather than adding the whole inspection/output tree:

- `Design/ReferenceGameplayImport/INTEGRATION.md`, `ChangedFiles.txt`, `vfx-connections.csv`, checkpoint pointer, APK integrity and protected-file reports.
- `Art/` source catalogs, extraction/import/validation tools and notes, including the mailbox native skeleton/atlas data; reviewed appearance sheets are useful delivery artifacts.
- `Media/NativeVisuals.json` and `Media/NativeParticles.json` are direct inputs to the included Unity importers. Preserve them. Include NativeBlockerParticles, original animation data, manifests, validation reports, tool sources and incompleteness notes for provenance. The recovered `resources.resource` and original bundles are offline source dependencies, not runtime Assets; keep them locally and avoid duplicating the APK package in the source commit.
- `Levels/` importer, source-parity report and RULE-PROVENANCE.md.
- `Design/Inspection-Levels01-50/reference-levels01-50-decoded.json`, `reference-schema-evidence.json` and `ReferenceLevelsRaw/level01.bytes` through `level50.bytes` are direct inputs to ReferenceLevelChecks. Include these explicit paths so data checks remain reproducible.
- Final Unity reports, reviewed screenshots and the compact final gameplay MP4. Keep original failed-pass reports distinctly named; never present them as the final pass.
- The task checkpoint's scoped source snapshots and baseline patch may be backed up explicitly if desired. Do not add sibling backups, APKs or old inspection folders wholesale.

Exclude ImportWorkspace caches/project copy, raw frame sequences, temporary capture audio, compiled DLL/RSP/PDB files, live inbox/status files, `__pycache__`, generated checkpoint duplicates and unrelated recordings. ImportWorkspace's own `.gitignore` already excludes everything except its README and ignore file; the parent output directory has no blanket ignore, so explicit file staging is essential. No file in the proposed runtime tree requires LFS by size; check the final MP4 size before upload.

After staging, review the staged path list/stat and ensure every new Unity asset has its metadata. Verify the staged source snapshot's compile/dependencies before calling it a reproducible source backup. Current working-tree compilation alone also sees unstaged prerequisites.

## Scope and delivery cautions

- Levels 01–50 contain source-checked authored data (3,933 cells), but are selected in the isolated reference QA campaign. Normal campaign/progression are retained. Say this explicitly.
- Matching, swaps, refill probabilities/order, blocker hit rules, combo targeting/order, bonus completion and rewards are existing reconstructed behavior, not verified native logic. Authored-data parity and a passing playable batch do not prove reference-rule equivalence or level solvability.
- Native particle/render import verification does not establish native runtime choreography, which depended on unavailable IL2CPP scripts and timing overrides.
- Seven imported VFX roots lack native sequence hooks: MatchItemSwapParticles, TntRocketComboParticles, PropellerRocketComboParticles, TntPropellerComboFuseParticles, TntRocketComboUseParticles, PurpleGrassItemTapParticles and PurpleGrassItemExplodeParticles.
- Other acknowledged gaps: potion bottle population/color assignment, cupboard door shader/state/debris behavior, mailbox activity playback, Grass2 composite state, coin reward flight/particle choreography, native board border generation and native ordinary swap/fall staging. One Propeller animation path references a missing source hierarchy child.
- `INTEGRATION.md` and `vfx-connections.csv` still said live verification pending at audit time. Replace this only with the actual new test result and recording paths once root completes the tests.
- Protected-file hashes are relative to this task's checkpoint, not Git HEAD. Existing unrelated homepage/services/settings edits predate the task and remain in the working tree.

This audit is not gameplay-test evidence and does not mark the import complete.
