# Royal Match reference run: progress

Assignment (2026-09-28): inspect the reference APK, play Levels 1-5 and the end-of-level / Home / Areas journey, and
reproduce the observed behaviour in Royal Throne through its existing systems. Evidence: `OBSERVATIONS.md` and
`recordings/`.

## Checkpoint
`Design/Backup/Checkpoint-2026-09-28_0230-RoyalMatchReference` (744 files: levels, targets, areas, gameplay, GUI, juice,
editor checks, game scene, area specs; git HEAD 171b1e49, status and uncommitted diff). Every installer below also
snapshots the files it overwrites (`scratchpad/kv/play/batch*_before/`).

## Completed
- **APK identity** (`Design/Backup/apk/royal_thron_rfv387.apk`, SHA-256 a2cbee4e...8151):
  - package `com.dreamgames.royalmatch`, version 38174, minSdk 24 (Android 7.0), targetSdk 35, native code arm64-v8a only;
  - it is a merged single APK (the Play bundle's base, UnityDataAssetPack and arm64 splits in one file).
- **Signature and integrity: FAILED, not run.**
  - Signed by an unknown party (`CN=A1, O=A1, L=Lazyland, C=RU`), not by Google Play.
  - Compared file by file with the official Play-signed 38174 install: 1358 files are identical and 4 differ.
  - **`lib/arm64-v8a/libil2cpp.so` is binary-patched** (5,683 regions, 22,652 bytes; the game logic).
  - **`classes6.dex` has injected classes**: `com.android.unityengine.UnityPIayerNativeActivity` (a look-alike name, capital
    I for l) and `com.unity3d.player.services.Event` (a downloader/executor).
  - Decision: this file is not executed. The official Play-signed build of the same version is used instead.
- **Dedicated emulator** `RoyalThrone_Reference_4K` (emulator-5556): Android 14 x86_64, 4 KB pages, ARM64 translation, no
  Play Store, no account. No other emulator's data was touched. The USB phone was never used.
- **Reference journey recorded** with game audio, Levels 1-5 plus Home and Areas: 15 clips, `recordings/journey_000-014`.
- **Evidence record**: `OBSERVATIONS.md` (triggers, timings, motion, VFX, sound, balances, tasks).
- **Level data verified** against the reference screens: the decoded layouts in `Resources/ReferenceGameplay/levels01-50.json`
  match cell by cell (L1 40/40 readable cells, L2 81/81, L3 81/81, L4 49/49 plus the grass flags). Goals and moves match
  (36 boxes/30, 36 boxes/27, 25 chests/27, 49 grass/30, 73 grass/30).
- **Artwork generation (Magnific Nano Banana 2 unlimited): BLOCKED.** The account has no unlimited plan (131 credits). No
  paid generation was used; no other model was substituted.

## Batches
| Batch | Scope | State |
| --- | --- | --- |
| A | Levels 1-5 run on the verified reference layouts for players (not only QA); lessons staged like the reference (L1 crate caption, L2 Rocket and L3 TNT outline + hand on a real move, L4 grass caption, L5 combo caption + hand after the first power pair); in-level tools padlocked on L1-5 | verified in the test clone |
| B | Win flow in the reference order: settle, then the celebration (dark dim, Royal Throne logo under pennants, fireworks, confetti, complete fanfare) → goal panel becomes a Reward coin counter while the moves left fire as rockets → one-star card for L1-5 | verified in the test clone |
| C | CONTINUE → Home (no auto level card for L1-5); tip card (Rocket, Rocket, TNT, Propeller, Color Ball); coin and star flights into the Home counters; task list opens by itself only when the next task is affordable | verified in the test clone |
| D | Area build staged like the reference: star flight from Do It → list closes → object appears under a sparkle beam → list reopens | verified in the test clone; area check updated |

## Verification (2026-09-28, test clone)
The shared editor was in a Play session on the player's real save from 02:23, so every test ran in a second editor on a
full copy of the project (`D:\RoyalThroneQA\outputs\JellyGarden-Codex`, own company name, so its saves are separate from
the player's). The home and title builders accept only a path ending in `outputs\JellyGarden-Codex`, hence the folder.
- **Type check:** the clone's full Unity compile: 0 errors. The 7 warnings (UAC1001) are all in `RoyalCardMotion.cs` and
  `RoyalLevelCardMotion.cs`, another session's uncommitted files. The project has no linter (see the check-suites notes).
- **Levels 1-5:** the sweep opens each level with the reference moves (30, 27, 27, 30, 30) and one goal. The lesson test
  shows each lesson and ends it on the first move. The bot wins 14 of 15 games (L3 is the tightest, 2 of 3).
- **Win flow:** on each of L1-5: the celebration, the Reward counter reaching 15, the one-star card with the star row and
  score hidden, CONTINUE staying Home, the tip card, the +15 coins counting up, and the list opening when affordable.
- **Padlocks:** L1 shows four padlocks with no stock badges, and a tap does nothing; L6 has none, and a tap selects.
- **Suites:** 16 pass (ref-levels, campaign-data, areas, home, dialogs, qa-controls, ref-tools, ref-layout, ref-input,
  ref-input-events, ref-home-finish, ref-home-sky, tnt-sun, palette, campaign-all; plus the padlock check). The 9 known
  failures (effects, cell-breaks, tnt, rockets, colorball, colorball-qa, cb-v4, cb-v5, prop-v5) fail on exactly the same
  checks as the 2026-09-25 baseline, the user's R-25/R-28 decisions.
- **Fixed during verification:**
  - the home and dialog suites wrote an `OpenLevel` key into a save that had none;
  - the celebration read the overlay's size before its CanvasScaler applied, so the pennants were drawn 1,300 px above the
    screen and the logo sat too high;
  - the padlock pass fought `BoostIcon.UnLockBoost`, which shows the stock badge again after every move.

## Areas mapping (structure kept, names and art are Royal Throne's own)
| # | Reference "Castle" task (cost) | Royal Throne "Castle Approach" task (cost) |
| --- | --- | --- |
| 1 | Build castle (★1) | Raise the castle keep (★1) |
| 2 | Build the fountain (★1) | Rebuild the grand stairway (★1) |
| 3 | Install gazebos (★2) | Restore the twin waterfalls (★2) |
| 4 | Plant the bushes (★2) | Set the golden gate pillars (★2) |
| 5 | not observed | Pave the sunlit plaza (★2) |
| 6 | not observed | Plant the courtyard gardens (★2) |
| chest | purple chest at 6/6 (not reached) | area chest, 20 coins + a rocket |

## Verified limitations
- Timings from the reference are approximate (ARM translation on one core; ±0.5 s).
- The coin reward keeps the user's rule (10 + 5 per star). The reference's amounts (64, 50, 44, 13, 63) follow a formula
  that could not be inferred.
- The reference's gold trumpets and red cushion have no Royal Throne art; the celebration uses pennants, the logo,
  fireworks and confetti, and the card star sits on the existing card.
- Not observed in the reference: Castle tasks 5-6, the chest, the next area, any story, the fail flow.
- The imported board art (boxes, pieces) is the existing R-25 state, unchanged by this run.
- BGM: no audio asset, mixer or music code was changed (the celebration uses the existing `complete` cue).

## Files changed (batches A-D)
- `Assets/JellyGarden/Scripts/RoyalReferenceCampaign.cs` (A)
- `Assets/JellyGarden/Scripts/GUI/Tutorial/RoyalPowerTutorial.cs` (A)
- `Assets/JellyGarden/Scripts/GUI/Tutorial/TutorialManager.cs` (A)
- `Assets/JellyGarden/Scripts/Juice/RoyalWinSequence.cs` (B, C, D)
- `Assets/JellyGarden/Scripts/LevelManager.cs` (B)
- `Assets/JellyGarden/Scripts/GUI/AnimationManager.cs` (C; one hunk, the other session's hunks left alone)
- `Assets/JellyGarden/Scripts/GUI/RoyalAreaView.cs` (D)
- `Assets/JellyGarden/Scripts/Editor/RoyalAreaChecks.cs` (D)
- `Assets/ReferenceGameplay/Tools/Scripts/ReferenceBoosterBar.cs` (A: the padlocks)
- `Assets/JellyGarden/Scripts/RoyalQaService.cs` (A: `Practising`, so the QA practice board keeps the tools unlocked)
- `Assets/JellyGarden/Scripts/Editor/RoyalHomeScreenChecks.cs`, `RoyalPresentationChecks.cs` (no `OpenLevel` key left
  behind on a save that had none)

## Integration into the main project (DX11 editor, 2026-09-28 11:55)
Merged into `C:\Users\chakm\Documents\Codex\2026-09-08\144\outputs\JellyGarden-Codex` (the `<DX11>` editor) once it had
been stopped for a minute: five anchor-checked patches (other sessions' uncommitted hunks kept) and six whole-file copies
of files that still matched HEAD. Validated there before the commit:
- compile: 0 errors; the merged code loaded (live probe);
- the 12 merged files equal the tested copy on D: (line endings aside);
- QA-profile runs in DX11: Levels 1-5 open, the padlock check (8/8), the level 1 win flow; the real save unchanged each
  time; no console errors.

Committed path-limited on `visual-rebuild-v2` (AnimationManager: this hunk only). Not pushed. The recording and the
comparison sheets are in `implementation/` (not committed: large media).
