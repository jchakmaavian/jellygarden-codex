# Imported homepage UI and finishing stage

Implemented in the existing Unity project, using the unchanged reference APK and existing offline extraction. No APK install or build.

## Homepage

Imported 25 native UI sprites: resource containers, coin/heart/star, Settings, button frames, green Level button, orange Area button, purple chest, progress bar and the five navigation icons. `home-art-inventory.json` identifies their actual source IDs, sizes, pivots and borders. Mirrored halves and native slicing are retained.

The adapter uses the recovered Home UI hierarchy as its layout reference, adapting anchors to the existing responsive canvas. The source Home profile Frame is an empty runtime-populated node; the packaged profile-frame artwork is used with our existing king portrait. Existing live resource labels, level entry, area action, navigation and Settings callbacks remain attached. The Royal Throne logo, king, castle/background and current music remain unchanged. No new background or moving clouds were imported. Existing tab destinations remain the project's destinations; this is not an import of reference online events/services.

## Finishing stage

Reconstructed orchestration follows the packaged `RemainingMovesItemReplacer` and `RemainingMovesItemExploder` structure: send remaining moves to eligible normal pieces, play arrival feedback, create specials, activate surviving boosters through the existing system, resolve/refill, then complete the existing win flow. Tap-to-skip shortens presentation gaps and move travel without cancelling conversions or awarding a second win.

The previous 15-move conversion cap is removed. Larger counts use further board batches if necessary. Only actual Rocket/TNT/Color Ball/Propeller pieces are selected for finishing activation; ingredients and blocker identities are excluded. Conversion selects distinct eligible pieces within a batch. The current Rocket/other booster implementations were not edited.

`RemainingMovesParticles` and `RemainingMovesHitParticles` are imported native particle hierarchies, with their sprite/material dependencies, through the existing importer and pool. The source renderer's unused mesh reference (file 4, ID 10202) is not needed in its Billboard rendering mode. No active particle dependency was missing.

**Accuracy limits:** Editable APK logic, exact reward constants, booster-selection probabilities and move-flight scheduling were not recovered. The orchestration is reconstructed code, not imported source. The existing game's horizontal/vertical Rocket selection policy and reward/progression system remain; no guessed per-move coin reward was added. Flight/gap timings are adapter values, not claimed as exact reference timings. A completely blocked board without eligible pieces or remaining boosters cannot display additional conversions; its counter is not silently falsified.

## Checks and captures

- Runtime and editor compilation passed (existing warnings only).
- Isolated Play Mode: 20 moves converted exactly once; zero moves still detonated existing boosters; five moves with skip converted exactly once. All three reached Win, with no remaining active booster operation.
- Homepage controls, Settings opening, unchanged background sprite and normal-save/currency restoration passed.
- Final finishing and homepage runs recorded zero new console errors. Initial checks caught a test-harness restart-state error, which was fixed; unrelated Spine import/atlas warnings observed during the initial editor refresh are not reported as gameplay fixes.
- `finishing-validation.txt`, `validation.txt`, `verified-finish-status.json`, `verified-home-status.json` contain the results.
- `Homepage.png`, `Remaining-moves.png`, `Booster-finale.png` are real Play Mode captures. Finishing captures use the isolated practice board, not changed campaign objectives.

Changed code: `ReferenceHomeLayout.cs`, `ReferenceWinFinish.cs`, one runtime hook in `RoyalHomeScreen.cs`, the existing `LevelManager.PreWinAnimationsCor`, a focused source-file overload in the VFX importer, and scoped editor checks. No scenes were saved. Earlier unrelated edits were preserved.

Pre-edit checkpoint: `Design/Backup/Reference-Home-Finish-20260924-162610/`.
