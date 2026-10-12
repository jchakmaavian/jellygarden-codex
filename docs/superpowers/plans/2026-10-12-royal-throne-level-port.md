# Royal Throne Level Port Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace King Leo's in-level game with Royal Throne's: board, pieces, powerup creation and usage, board behaviour, HUD, king avatar and bottom booster bar. Everything outside the level stays as `main` has it: home, areas, Treasure Hunt, story, repairs, level popup, win/lose panels and progress.

**Architecture:**
- Royal Throne builds its level into the kit's core scripts, plus a presentation layer created at runtime (`Scripts/Juice`, `Scripts/GUI/Royal*`, `Assets/Boosters`, `Assets/ReferenceGameplay`).
- One asset switches it on: `Resources/RoyalGameplay/EffectsProfile.asset`.
- Main builds its level as Story hooks added on top of the kit.
- The port works in four moves:
  1. Royal Throne's kit-core files become the new base.
  2. Only main's *meta* hooks (campaign, flow, free-game, pre-play) are re-applied.
  3. Royal Throne's level layer is imported.
  4. Main's in-level Story layer is retired.
- Nothing in `game.unity` has to come across, because Royal Throne builds its level UI at runtime.

**Tech Stack:**
- Unity 6000.5.10f1. Built-in render pipeline; URP is installed but inactive on both branches.
- C#: Assembly-CSharp, Input System only (`activeInputHandler: 1`).
- Embedded spine-unity 3.8 and AllIn1VfxToolkit.
- Python 3 tools in `tools/`.
- git worktrees.

**This document contains no implementation code.** It names files, members, commands and checks. Code is written task by task during execution.

---

## 0. Facts this plan rests on

Each fact was verified on 2026-10-12. Line numbers drift, so the tasks identify code by searching for a token.

1. **No shared history.** `origin/main` and `origin/royal-throne` share no git history.
   - Their common kit ancestor is `a82c4df` (pristine JellyGarden kit, 2026-08-31, main's second commit).
   - Royal Throne's first commit `3fc0aeb8` is one snapshot of a month of work done elsewhere (`jchakmaavian/jellygarden-codex`).
   - So the port goes feature by feature, not commit by commit.
2. **Main already has approximations of Royal Throne.** Between 2026-10-08 and 10-11, main added Story layers that copy it: `StoryOriginalBoosters`, `StoryOriginalTnt`, `StoryOriginalColorBall`, `StoryOriginalMotion`, plus a 34 MB subset of `Assets/ReferenceGameplay`. Royal Throne's real implementation replaces them.
3. **Royal Throne fixes main's overlap bug.** On main, a powerup can be spawned into a cell that gravity has already refilled. Royal Throne prevents this in two ways:
   - The creation cell is reserved: `Item.DestroyItem` calls `Square.Reserve`, and `Square.Release` runs in the same frame as `GenItem`.
   - A watchdog, `Juice/RoyalBoardIntegrity`, finds and repairs ownership errors at runtime.
4. **Enums only grew at the end.** `ItemsTypes.HELICOPTER` (the Propeller) and the four tool `BoostType` values (`RoyalHammer`, `RoyalArrow`, `RoyalCannon`, `RoyalJester`) were added at the end of their enums. Existing level files, Targets assets and saved booster counts keep their meaning.
5. **Colour count conflicts.** 403 of main's 1,208 levels use `COLOR LIMIT 6` (541 use 5, 262 use 4, 2 use 3). Royal Throne clamps the board to 5 colours and remaps colour 5 to colour 1 (`RoyalGameplayArt.BoardColor`, used by the `Item.color` setter). This must not come across unchanged; see D3.
6. **Most live assets come from a Royal Match APK.** This covers most of Royal Throne's live in-level art, animation, VFX, audio and the King rig. Sources: `Design/KingVanguard-Reference-20260929/SOURCE-VERIFICATION.md` and `Design/ReferenceGameplayImport/INTEGRATION.md` on `origin/royal-throne`. Main already ships part of the same material through its `StoryOriginal*` layers.
7. **No level objects live in the scene.** Royal Throne serializes no Juice, Reference or Booster component in `game.unity`.
   - The only scene additions belong to its home screen, which is out of scope.
   - Its in-level systems install themselves through `RuntimeInitializeOnLoadMethod`, `LevelManager.Start` (`AddComponent<RoyalGameplayPresentation>`), or `Resources.Load("RoyalGameplay/EffectsRig")`.
8. **Royal Throne's tests have limits.**
   - They are Play-mode coroutine suites driven by `Juice/RoyalRegressionRunner`, not Unity Test Framework tests.
   - Several suites have no committed driver.
   - Many entry points call `RoyalHomeScreenBuilder.RequireCopy()`, which throws unless the project folder is named `JellyGarden-Codex`.
9. **Main's "feel" rule blocks the port as written.** `tools/check_vfx_intact.py` enforces "board scripts may gain hooks, may not lose methods" against `a82c4df` for `LevelManager.cs`, `Item.cs`, `Square.cs`, `GUI/AnimationManager.cs`, `ItemAnimEvents.cs` and `ParticleSorting.cs`. Royal Throne replaces kit methods, so the port cannot pass the rule as it stands; see D7.
10. **Unity rewrites settings when branches change under it.** Never switch branches in a folder Unity has open: it writes the previous branch's in-memory `ProjectSettings` back to disk. That is what caused the `UNITY_ADS` compile errors on 2026-10-12.

## 1. Decisions to confirm before Task 0.1

Tasks below are written for the **default**. Where an alternative changes a task, the task says so.

| # | Decision | Default (recommended) | Alternatives |
|---|---|---|---|
| D1 | **APK-derived assets** | Port code and behaviour. APK-derived assets may be used only as internal placeholders, listed in `docs/port/PROVENANCE.md`. A release check (Task 6.4) fails any store build that contains them. Replacement art is a separate track. | (a) Treat APK assets as final: legal risk, needs the owner's written sign-off. (b) Code only, King Leo art from day one: slower, because Propeller, tool icons and activation VFX need new art first. |
| D2 | **King avatar** | Royal Throne's avatar *behaviour* (arched HUD portrait, reaction states and their triggers) drives King Leo's own art: `StoryAvatar` frames `Resources/Story/Art/avatar_leo_*` plus the cheer, pop, worry and slump clips. | Royal Throne's KingVanguard rig: an APK-derived Royal Match king, adding 37.5 MB plus Spine on the HUD. |
| D3 | **Gem colours** | Keep the level's own `COLOR LIMIT` (3–6). Royal Throne's clamp and its colour-5→1 remap are removed, and a sixth gem skin is added. | Clamp to 5: changes difficulty and matching on 403 levels, which then need re-tuning. |
| D4 | **Win/lose rules** | Keep main's: `CheckWinLose` (a score goal is a real goal), the star awarded by `StoryFlow`, and `StoryWinPopup`/`StoryLosePopup`. Use Royal Throne's `ReferenceWinFinish` for the leftover-moves finish. Do not port `RoyalWinSequence`, `CommitWin` or `RoyalEconomy`. | Take Royal Throne's win rules, sequence and card. |
| D5 | **Tool economy** | Main's free model: tool counts live in `StoryBoosters.Owned` and are granted by `StoryFreeGame`. Royal Throne's coin purchase panel stays off; `StoryFreeGame.ShopsAllowed` is already false. | Royal Throne's coin shop. |
| D6 | **Board backdrop** | Royal Throne's navy board scene, as requested. King Leo's area painting behind the board (`StoryBoardTheme`) is retired from the level. | Keep the area painting behind Royal Throne's board. |
| D7 | **Main's "feel must not regress" rule** (`PLAN.md`, `Assets/JellyGarden/Scripts/Story/CLAUDE.md`, `tools/check_vfx_intact.py`) | The owner signs off that the in-level feel is now Royal Throne's. `check_vfx_intact.py` is rebased onto the port's merge commit and stays in force from there. | None: the port replaces kit methods. |
| D8 | **Royal Throne reference campaign** (levels 01–50; box, mailbox, cupboard, potion, grass, egg and coin blockers) | Out of scope. Campaign hooks are cut during the kit merge. | Port these blockers as new obstacle types in a later plan. |

## 2. Scope

**In scope.**
- The requested features: board visuals, pieces, powerup creation, powerup usage and combos, board behaviour (swap, cascade, fall, refill, hints, shuffle, input during cascades), the in-level HUD, the king avatar and the bottom booster bar.
- Also required:
  - Pre-level boosters keep working.
  - Win and lose still end the level through main's flow.
  - The overlap bug is gone.
  - Tests cover all of the above.

**Out of scope.** Royal Throne's home, title, splash and loading screens; music and ambience; win card; settings, profile and leaderboard pages; cards; QA mod-menu UI; the reference campaign (D8); its Android build tooling; its branding.

## 3. Feature map

| Feature | Royal Throne source (`origin/royal-throne`) | Main today (retired by this port) | After the port |
|---|---|---|---|
| Board visuals | `GUI/RoyalGameplayPresentation` (`StyleBoard`, `FrameBoard`, `PlayableViewport`), `GUI/RoyalBoardSurface`, `GUI/RoyalGameplayHud.Backdrop`, `Juice/RoyalGameplayArt`, `Resources/RoyalGameplayV4/04_board` | `StoryBoardTheme`, `StorybookBoardLayout`, `StoryRoyalBoardFrame`, `StoryBoardScale`, `StoryBoardCentre`, `StoryPixelSnap` | Royal Throne's board, frame and camera fit |
| Pieces (gems and specials) | `Juice/RoyalPieceSkin`, `Juice/RoyalBlockSkin`, `ReferenceGameplay` `ReferenceGameplayCatalog` + `runtime-catalog.json`, `ReferencePieceVisual` | `StorybookBoardArt.Apply` (`Item.Awake`), `StorybookRocketFacing`, `StoryOriginalPiece`, `StoryOriginalTntPiece` | Royal Throne's skins, with 6 colours (D3) |
| Powerup creation | `System/CombineManager` (`SetNextItemType`, rocket orientation), `Juice/RoyalPatterns` (2×2 Propeller), `LevelManager.PreferredMatchHost`, `Juice/RoyalSpecialFormation`, `Square.Reserve/Release`, `Item.ChangeTypeCor` royal seat, `Juice/RoyalSpecialCreationFlash` | Kit rules plus `StoryOriginalBoosters.ShowPiece` / `StoryOriginalTnt.Show` | Royal Throne's rules, including the Propeller and the merge gather |
| Powerup usage and combos | `Juice/RoyalPowerupDirector` (partial: `RoyalRocketSequence`, `RoyalRocketTntCombo`, `RoyalTntNativeSequence`, `RoyalDualColorBallSequence`, `Boosters/Propeller/V5/Scripts/PropellerGameplayV5`, `Boosters/ColorBall/V4/Scripts/ColorBallV4Sequence`) | King Leo tap/landing rules in `Item` (`TapActivate`, `LandActivate`, `IsUniversalPowerup`), `StoryOriginal*`, `StoryRocket*`, `StoryBomb*`, `StoryColorBombVfx`, `StoryRoyalVfx` | Royal Throne's director |
| Board behaviour | Royal Throne's `Item` swap pipeline and `CanAcceptInput`; `LevelManager` `BoardInputAllowed`, resolver loop and `GenerateNewItems(fillBlockedCells)`; `AI` + `Juice/RoyalHintSolver` / `RoyalHintPresenter`; `Juice/RoyalShuffle`; `Juice/RoyalBoardIntegrity`; `Resources/JuiceConfig` | Kit loop plus `StoryOriginalMotion`, `StoryDeadBoard` watchdog | Royal Throne's loop. `StoryDeadBoard` is kept only if Task 4.5 shows no double shuffle |
| HUD | `GUI/RoyalGameplayPresentation.Build`, `GUI/RoyalGameplayHud`, `GUI/RoyalArchShape`, `RoyalCardShape`, `RoyalPanelGraphic`, `RoyalVisualCatalog`, `LevelManager.FlyToGoal` | `StoryLevelHud.Soft` (soft HUD) and the baked `StoryLevelHud` strip | Royal Throne's HUD. `StoryLevelHud.ReadLevel` stays as the level popup's goal model |
| King avatar | `Characters/KingVenguard/V2` (`KingVanguardSlot`, `KingVanguardCharacter` states, `KingVanguardFeedbackQueue`), triggers in `RoyalGameplayPresentation`, `Item`, `LevelManager` | `StoryAvatar` (invisible in Treasure Hunt mode) | Royal Throne's slot and state machine driving `StoryAvatar` art (D2) |
| Bottom booster bar | `ReferenceGameplay/Tools/Scripts/ReferenceBoosterBar` (Hammer, Arrow, Cannon, Jester) + `Reference{Hammer,Archer,Cannon}Sequence`; rail built in `RoyalGameplayPresentation`; `GUI/BoostShop` enum; `GUI/BoostIcon` | Soft-HUD 4-slot tray (`StoryLevelHud.Soft.UseSoftBoost`) over kit boosts | Royal Throne's rail and tools, counted by `StoryBoosters.Owned` (D5) |
| Pre-level boosters | — (kit `SetPreBoosts` → `NextType` + `ChangeType`) | `StoryLevelPopup`, `StoryBoosters`, `StoryPrePlay` | Unchanged. Pieces are seated by Royal Throne's `ChangeTypeCor` |
| Win/lose | `CheckWinLose`, `CommitWin`, `RoyalWinSequence`, `ReferenceWinFinish` | Main's `CheckWinLose`, `StoryFlow`, `StoryOriginalBoosters.SendMove` finish | Main's rules with `ReferenceWinFinish` (D4) |

## 4. Kit-core merge rules

**Method:**
1. Start from Royal Throne's version of each kit file: `git restore --source=origin/royal-throne -- <path>`.
2. Cut the Royal Throne couplings listed in `docs/port/COUPLINGS.md` (Task 1.3).
3. Re-apply main's *keep* hooks, found by searching for the token in `git show origin/main:<path>`.

**Royal Throne couplings to cut, in every file:** `RoyalWinSequence`, `CommitWin`, `RoyalEconomy`, `RoyalReferenceCampaign`, `RoyalCampaignBlockers`, `RoyalCampaignItem`/`IsSpecialItem`, campaign `TryLoad`/`TryColor`, `RoyalTitleScreen.QueueStartupWork`, home-screen types, `RoyalPowerTutorial`, `Boosters.*.V2|V3|V4` types, the 5-colour clamp and the `BoardColor` remap (D3). `KingVanguardActivityEvents` is retargeted to an avatar event API (Task 4.7).

### `Assets/JellyGarden/Scripts/LevelManager.cs`
- **Keep from main:**
  - `StoryCampaign.ApplyLevelTuning` (in `LoadLevel`) and `StoryCampaign.ProtectFirstPuzzle`.
  - `StoryPrePlay.Enabled` / `StoryPrePlay.Open`, plus the `Hide()` null guards in `PrepareGame`.
  - `StoryOneStar.CapStars`.
  - The `IngredientExitRow` method.
  - Null guards on `guiObj`, ingredient flights, `UpdateBar` and star markers.
  - Main's whole `CheckWinLose` (D4).
- **Drop from main:**
  - Board and motion hooks: `StoryBoardScale.*`, `StoryBoardTheme.DrawKitOutline`, `StoryOriginalMotion.*`.
  - Refill-barrier gates: `StoryRocketSweep.HasPendingHits`, `StoryOriginalTnt.Anticipating`, `StoryOriginalColorBall.Busy`.
  - VFX and finish hooks: `StoryRoyalVfx.StateChanged`, `StoryOriginalBoosters.SendMove` / `MovesInFlight` (replaced by `ReferenceWinFinish`), `KingLeoVfxHost.Configure` (first confirm nothing outside the level needs it; Task 3.7).
  - Main's input and debug code: the `BeginDragFromCurrentTouch` call and the temporary `[OverlapProbe]` block.
- **Keep from Royal Throne:**
  - Input: `BoardInputAllowed`, `InputSwaps`, `Begin/EndInputSwap`, `PreferredMatchHost`.
  - Resolver: `IsResolvingMatches`, `BoardResolving`, `royalResolving`, and the `FindMatches` / `FallingDown` revision loop with gravity waiting for swaps.
  - Fill and shuffle: `GenerateNewItems(falling, fillBlockedCells)` and the `RoyalShuffle` call in `NoMatchesCor`.
  - Goals and win: `FlyToGoal`, `ReferenceWinFinish` inside main's `PreWinAnimationsCor`.
  - Guards: `GetSquare` bounds, the `GetRandomItems` fix, `TimeTick` popup hold.
  - Setup: `AddComponent<RoyalGameplayPresentation>` in `Start`.

### `Assets/JellyGarden/Scripts/Item.cs`
- **Keep from main:** nothing in-level. Main's removal of the `OnMouse*` handlers does not matter, because input runs through the Input System.
- **Drop from main:**
  - Every `Story*` hook (Appendix A), plus `Match3PieceMotion.Pop` and `StorybookBoardArt.Apply` in `Awake`.
  - The universal-powerup rules (`IsUniversalPowerup`, `LandingPowerup`, `CanTapActivate`, `TapActivate`, `LandActivate`).
  - `DestroyByRocket` and `PopScale`/`PopRelease`/`PopClaimForKit`.
- **Keep from Royal Throne:** everything else.
  - Swap pipeline: `SwitchDirection`, `Switching`, `SwitchingCore`, `TrySwap`, `CanAcceptInput`, tap → `RoyalPowerupDirector.Tap`.
  - `DestroyItem` (`Reserve`, deferral) and `DestroyCor` (`Queue`, `Gather`, `Release` + `GenItem`, flash).
  - `ChangeTypeCor` royal seat; `FallingCor`, `FallDistanceAt`, `LandingSquashCor`, `JuiceDestroyCor`, `StopIdleAnim`.

### `Assets/JellyGarden/Scripts/Square.cs`
- **Keep from main:** the ingredient exit arrow placed on `LevelManager.IngredientExitRow` in `Start`. Its compact scale now applies when the Royal layer is active, not when `StorybookBoardArt.Enabled` is set.
- **Drop from main:** `StoryBoardScale.ItemScale` and `[OverlapProbe]`.
- **Keep from Royal Throne:** `reservedFor` / `Reserved` / `Reserve` / `Release`, `GenItem(falling, campaignRefill)` checks, squares in `FindMatchesAround`, special exclusions in `FindMoreMatches`, `FallOut` guards, and `DestroyBlock(rocketClear, wave, matched)` without its `RoyalCampaignBlockers` call (D8).

### `System/CombineManager.cs`, `AI.cs`
Main never changed these. Take Royal Throne's and cut the campaign exclusions (D8).

### `InputManager.cs`
Take Royal Throne's: `TrackPressPointer`, the per-instance action asset, and debug actions only in Editor or debug builds. Task 3.4 confirms it covers main's press-position fix.

### `InitScript.cs`, `GUI/AnimationManager.cs`, `GUI/MenuManager.cs`
Keep main's versions; they carry the `StoryFreeGame` and `StoryFlow` hooks. Add only `MenuManager.BlocksBoardInput` from Royal Throne, which `BoardInputAllowed` needs.

### `SoundBase.cs`, `MusicBase.cs`
Union of both: main's `PlaySound(clip, volumeScale)` plus Royal Throne's additions.

### `Targets/TargetLevel.cs`, `Targets/TargetIcon.cs`, `Targets/TargetsGroup.cs`
Royal Throne's `TargetObject.referenceGoal`, plus one goal-art source shared by the board, HUD and level popup (Task 4.6). This replaces main's `StorybookBoardArt.Goal` calls.

### `GUI/BoostShop.cs`, `GUI/BoostIcon.cs`
Royal Throne's enum additions, and `ActivateBoost` routed to the bar.

## 5. File structure after the port

**Added from Royal Throne:**
- `Assets/JellyGarden/Scripts/Juice/` (live files; dead files removed in Task 6.1).
- From `Assets/JellyGarden/Scripts/GUI/`, the in-level subset only: `RoyalGameplayPresentation`, `RoyalGameplayHud`, `RoyalBoardSurface`, `RoyalArchShape`, `RoyalCardShape`, `RoyalPanelGraphic`, `RoyalVisualCatalog`, plus whatever Task 1.3 proves they need.
- `Assets/JellyGarden/Scripts/RoyalSave.cs`, `RoyalQaService.cs` (QA only).
- `Assets/Boosters/Propeller/V5/`, `Assets/Boosters/ColorBall/V4/`, `Assets/Boosters/ColorBall/V5/`.
- `Assets/ReferenceGameplay/` (superset of main's; `Blockers` and `levels01-50` excluded per D8).
- `Assets/Spine/`, which the Cannon tool needs.
- `Assets/JellyGarden/Shaders/` Royal shaders.
- Resources folders listed in Task 3.6.
- Editor QA suites (Task 4.0).

**Retired:** scripts are deleted; their sprites, prefabs, animations and materials stay, per main's no-asset-deletion rule.
- `Story/StoryOriginalBoosters`, `StoryOriginalBoostersValidation`, `StoryOriginalTnt`, `StoryOriginalTntPiece`, `StoryOriginalTntValidation`, `StoryOriginalColorBall`, `StoryOriginalMotion`, `StoryOriginalChargedTarget`, `StoryOriginalPiece`.
- `StoryRocketVfx`, `StoryRocketSweep`, `StoryRocketFlight`, `StoryRocketVariant`, `StoryRocketValidation`.
- `StoryBombVfx`, `StoryBombVariant`, `StoryColorBombVfx`, `StoryBlastVfx`, `StoryShatterVfx`, `StoryPunchVfx`, `StoryPopVfx`, `StoryRoyalVfx`, `StoryExplosionProbe`, `StoryVfxLab`, `StoryVfxVariant`, `StoryFinishVfx`.
- `StoryBoardScale`, `StoryBoardCentre`, `StoryPixelSnap`, `StorybookBoardLayout`, `StoryRoyalBoardFrame`, `StorybookRocketFacing`, `StoryLevelHud.Soft`, `StoryRoyalBoardValidation`, `StoryPowerupTest`.
- `Story/Editor/RocketValidationBridge`, `Assets/Editor/RocketFlameGapCheck.cs`.

**Reduced** (file stays, level parts removed):
- `StoryBoardTheme` (D6) and `StorybookBoardArt` (keeps goal-art delegation only).
- `StoryVfxFlags`: retired flags removed.
- `StoryLevelHud`: keeps `ReadLevel`, `ReadLevelManager` and the `HudTarget` model; installs no in-level HUD.
- `KingLeoVfxHost`, if Task 3.7 finds meta users.

**Kept unchanged:** the rest of `Story/`: meta screens, flow, progress, campaign, popups, `StoryBoosters`, `StoryPrePlay`, `StoryFreeGame`, `StoryAvatar`, `StoryOriginalWinStars` (used by `StoryWinPopup`) and `StoryPlaytest` (adapted in Task 5.1).

---

## Phase 0 — Setup and baseline

### Task 0.1: Isolated worktree

**Files:** none in the repo.

- [ ] **Step 1:** `git fetch origin --prune`
- [ ] **Step 2:** `git worktree add ../king-leo-port -b port/royal-throne-level origin/main`
- [ ] **Step 3:** In Unity Hub, Add > Add project from disk > `../king-leo-port`.
  - It gets its own `Library/`.
  - `king-leo/` stays on `main` for side-by-side comparison.
  - Never switch branches inside either folder while Unity has it open (Fact 10).
- [ ] **Step 4:** Let the first import finish. Expected: Console shows 0 compile errors.
- [ ] **Step 5:** Preferences > External Tools > Regenerate project files. This creates `Assembly-CSharp-Editor.csproj`, which the typecheck needs.
- [ ] **Step 6:** Verify with `python3 tools/make_typecheck_csproj.py && dotnet build Story.Typecheck.csproj -v q --nologo`. Expected: `0 Error(s)`.

### Task 0.2: Baseline evidence

**Files:** create `docs/port/BASELINE.md`.

- [ ] **Step 1:** Close Unity on the worktree; batchmode needs the project lock. Set `UNITY=/Applications/Unity/Hub/Editor/6000.5.10f1/Unity.app/Contents/MacOS/Unity`.
- [ ] **Step 2:** Run `python3 tools/check_vfx_intact.py`, `python3 tools/check_launch_args.py`, `python3 tools/validate_story.py`. Record the results.
- [ ] **Step 3:** Run each headless suite. The command is `"$UNITY" -batchmode -projectPath . -logFile - -executeMethod JellyGarden.Scripts.Story.Editor.<Suite>`, with no `-quit` (they exit themselves). `<Suite>` is each of: `StoryScreenTests.RunHeadless`, `StoryScreenTests.RunLevelPopupHeadless`, `StorySmokeTest.RunAll`, `StorybookTests.RunHeadless`, `TreasureHuntTests.RunHeadless`, `StoryWinPopupTests.RunHeadless`, `StoryProgressTests.RunHeadless`. Record PASS/FAIL counts.
- [ ] **Step 4:** Run `python3 tools/playtest_sweep.py --levels 1-16 --headless`. Record:
  - the findings;
  - the number of `[OverlapProbe]` lines in the batch logs, as the baseline for the overlap bug.
- [ ] **Step 5:** Build the player with `"$UNITY" -batchmode -quit -nographics -projectPath . -logFile - -executeMethod JellyGarden.Scripts.Story.Editor.StoryBuild.BuildMac`. Then take 393×852 shots with `tools/run_player.sh` of levels 1, 9 and 31 and Treasure Hunt boards 1–4, into `docs/port/shots/baseline/`.
- [ ] **Step 6:** `git add docs/port && git commit -m "docs: record baseline before royal-throne level port"`

---

## Phase 1 — Audits (no game change)

### Task 1.1: Import manifest and provenance

**Files:** create `docs/port/IMPORT-MANIFEST.md` and `docs/port/PROVENANCE.md`.

- [ ] **Step 1:** List every path to import, grouped by the rows of §3. For each path record:
  - size (`git ls-tree -r -l origin/royal-throne -- <dir>`);
  - whether its code is live or dead;
  - whether `main` already has it (byte-identical, different, or absent).
- [ ] **Step 2:** Mark the source of each path: APK-derived, Royal Throne-authored, kit, or third-party. Use:
  - the two Royal Throne provenance docs from Fact 6;
  - files whose comments contain `APK` or `rfv387`;
  - `Assets/ReferenceGameplay/**`, `Characters/KingVenguard/**`, `Resources/ReferenceGameplay/**`.
- [ ] **Step 3:** In `PROVENANCE.md`, record the third-party licences to clear:
  - spine-unity 3.8: the Spine Runtimes License requires a Spine Editor licence.
  - AllIn1VfxToolkit: Asset Store EULA, per seat.
  - `Resources/RoyalGameplay/Display.ttf` ("BorisBlackBloxx", © Manfred Klein): licence unknown.
- [ ] **Step 4:** `git add docs/port && git commit -m "docs: royal-throne import manifest and provenance"`

### Task 1.2: GUID audit

**Files:** create `tools/port_guid_audit.py` and `docs/port/GUID-AUDIT.md`.

- [ ] **Step 1:** Write `tools/port_guid_audit.py`. It reads every `.meta` file in `origin/main:Assets` and in the manifest paths on `origin/royal-throne`, using `git ls-tree` / `git cat-file` and never the working tree. It writes four lists:
  - (a) same path, same GUID, same bytes;
  - (b) same path, same GUID, different bytes;
  - (c) same GUID at different paths;
  - (d) same path, different GUID.
- [ ] **Step 2:** Run `python3 tools/port_guid_audit.py > docs/port/GUID-AUDIT.md`. Expected, from the research: about 1,057 shared paths, about 982 of them identical.
- [ ] **Step 3:** For every entry in (b), pick a side and record why. Default: Royal Throne's, if its code loads the file.
- [ ] **Step 4:** Lists (c) and (d) must be empty before Phase 3, or each entry resolved by choosing one asset and remapping references.
- [ ] **Step 5:** `git add tools/port_guid_audit.py docs/port/GUID-AUDIT.md && git commit -m "tools: GUID audit for the royal-throne port"`

### Task 1.3: Coupling cut list

**Files:** create `docs/port/COUPLINGS.md`.

- [ ] **Step 1:** In the manifest's Royal Throne scripts, search for: `RoyalHomeScreen`, `RoyalTitleScreen`, `RoyalEconomy`, `RoyalWinSequence`, `CommitWin`, `RoyalReferenceCampaign`, `RoyalCampaignBlockers`, `RoyalCampaignItem`, `IsSpecialItem`, `TryLoad`, `TryColor`, `RoyalModMenu`, `RoyalMenuSkin`, `RoyalBackButton`, `RoyalPowerTutorial`, `KingVanguard`, `RequireCopy`, `RoyalOfflineBuildPolicy`, `ROYAL_REFERENCE_CAMPAIGN`, `ROYAL_DISABLE_QA`, `BoardColorCount`, `BoardColor(`, `Propeller.V2`, `Propeller.V3`, `Propeller.V4`, `ColorBall.V2`, `ColorBall.V3`.
- [ ] **Step 2:** For each hit, record `file:line` and one resolution: remove, guard, replace with main's equivalent (name it), or import (add to the manifest).
- [ ] **Step 3:** Work out the import closure of the GUI subset in §5. Any GUI type the kit or Juice references that is not on the list gets either a resolution or a manifest entry.
- [ ] **Step 4:** `git add docs/port && git commit -m "docs: royal-throne coupling cut list"`

---

## Phase 2 — Third-party code

### Task 2.1: spine-unity runtime

**Files:** add `Assets/Spine/**`.

- [ ] **Step 1:** `git restore --source=origin/royal-throne --staged --worktree -- Assets/Spine Assets/Spine.meta`
- [ ] **Step 2:** Read `Assets/Spine/UNITY6-COMPAT-PATCHES.md`. Open Unity. Expected: 0 compile errors; the `spine-unity` asmdef is listed.
- [ ] **Step 3:** `git commit -m "chore: import spine-unity 3.8 runtime from royal-throne"`

### Task 2.2: AllIn1VfxToolkit superset

**Files:** modify `Assets/Plugins/AllIn1VfxToolkit/**`.

- [ ] **Step 1:** From `GUID-AUDIT.md`, take the AllIn1 paths that are on Royal Throne but absent on main, mainly the demo textures used by `Resources/RoyalGameplay/Toolkit` materials. Restore only those, with `git restore --source=origin/royal-throne --staged --worktree -- <paths>`.
- [ ] **Step 2:** Leave main's differing files as they are unless the audit chose Royal Throne's. `tools/explosion_bake.py` reads masks from this folder; run it with `--help` to confirm it still resolves its paths.
- [ ] **Step 3:** Expected: Unity compiles with 0 errors. Then `git commit -m "chore: add royal-throne AllIn1 assets used by its toolkit materials"`

---

## Phase 3 — Level layer swap

Phase 3 is **one compile unit**: Tasks 3.1–3.7 do not compile on their own. Use `git add` after each task and commit once, in Task 3.8, when everything compiles.

### Task 3.1: `LevelManager.cs`

**Files:** modify `Assets/JellyGarden/Scripts/LevelManager.cs`.

- [ ] **Step 1:** `git restore --source=origin/royal-throne --worktree -- Assets/JellyGarden/Scripts/LevelManager.cs`
- [ ] **Step 2:** Apply §4 "LevelManager.cs": cut the couplings, re-apply main's keep-list by token from `git show origin/main:Assets/JellyGarden/Scripts/LevelManager.cs`, and keep Royal Throne's list.
- [ ] **Step 3:** Make the colour count come from the level's `COLOR LIMIT`, not a constant 5 (D3).
- [ ] **Step 4:** Decide what `using PlayerPrefs = RoyalThrone.Testing.RoyalSave` does. Default: keep it, and import `RoyalSave.cs` in Task 3.5. Outside a QA session it behaves exactly like `PlayerPrefs`.
- [ ] **Step 5:** `git add Assets/JellyGarden/Scripts/LevelManager.cs`

### Task 3.2: `Item.cs`

**Files:** modify `Assets/JellyGarden/Scripts/Item.cs`.

- [ ] **Step 1:** `git restore --source=origin/royal-throne --worktree -- Assets/JellyGarden/Scripts/Item.cs`
- [ ] **Step 2:** Apply §4 "Item.cs".
- [ ] **Step 3:** Remove the `BoardColor` remap from the `color` setter and the 5-colour clamp in `GenColor` (D3).
- [ ] **Step 4:** Create `Assets/JellyGarden/Scripts/Juice/AvatarEvents.cs`: a static class with one event per avatar trigger (`Worried`, `Matched`, `PowerupCreated`, `Reaction(float strength)`, `ToolUsed`, `ToolStruck`, `GoalsCompleted`, `ExtraMoves`, `Lost`), and nothing subscribed yet. Replace each `KingVanguardActivityEvents.*` call in `Item.cs` with the matching `AvatarEvents` raise, and record each replaced call site in `COUPLINGS.md`. It has to exist now so Phase 3 compiles; Task 4.7 subscribes the avatar to it.
- [ ] **Step 5:** Remove references to `Boosters.*.V2|V3|V4` types in `Cancel()` and the clear flags.
- [ ] **Step 6:** `git add Assets/JellyGarden/Scripts/Item.cs`

### Task 3.3: `Square.cs`, `CombineManager.cs`, `AI.cs`

**Files:** modify `Assets/JellyGarden/Scripts/Square.cs`, `Assets/JellyGarden/Scripts/System/CombineManager.cs`, `Assets/JellyGarden/Scripts/AI.cs`.

- [ ] **Step 1:** `git restore --source=origin/royal-throne --worktree --` each of the three paths.
- [ ] **Step 2:** `Square.cs`: re-apply the ingredient-arrow block from main's `Start` (token `IngredientExitRow`). Its compact-scale condition becomes "Royal layer active", meaning `RoyalEffectsProfile.Get != null`.
- [ ] **Step 3:** All three files: cut the campaign exclusions (D8).
- [ ] **Step 4:** `AI.cs`: cut `RoyalPowerTutorial.HoldsHints`. If Task 1.3 chose to keep the tutorial, keep it instead.
- [ ] **Step 5:** `git add` the three paths.

### Task 3.4: Other kit files

**Files:** modify `InputManager.cs`, `GUI/MenuManager.cs`, `SoundBase.cs`, `MusicBase.cs`, `Targets/TargetLevel.cs`, `Targets/TargetIcon.cs`, `Targets/TargetsGroup.cs`, `GUI/BoostShop.cs`, `GUI/BoostIcon.cs`, all under `Assets/JellyGarden/Scripts/`.

- [ ] **Step 1:** `InputManager.cs`: take Royal Throne's. Compare its press handling with main's press-position change (`git diff a82c4df origin/main -- Assets/JellyGarden/Scripts/InputManager.cs`). If main's change is not covered, re-apply it.
- [ ] **Step 2:** `MenuManager.cs`: keep main's file and add only the `BlocksBoardInput` member from Royal Throne.
- [ ] **Step 3:** `SoundBase.cs` / `MusicBase.cs`: merge both sides' additions by hand: `git merge-file` with base `a82c4df`, ours main, theirs Royal Throne.
- [ ] **Step 4:** `Targets/*`: take Royal Throne's `referenceGoal`. Keep main's goal-art call site; it is rerouted in Task 4.6.
- [ ] **Step 5:** `BoostShop.cs` / `BoostIcon.cs`: take Royal Throne's. Confirm the enum values are appended (Fact 4).
- [ ] **Step 6:** `git add` all paths.

### Task 3.5: Royal Throne level scripts

**Files:** add from `origin/royal-throne`:
- `Assets/JellyGarden/Scripts/Juice/**`
- the GUI subset (§5 + Task 1.3)
- `Assets/JellyGarden/Scripts/RoyalSave.cs`, `Assets/JellyGarden/Scripts/RoyalQaService.cs`
- `Assets/ReferenceGameplay/{Scripts,Tools/Scripts,Editor}/**`
- `Assets/ReferenceGameplay/Home/Scripts/ReferenceWinFinish.cs` (that file only; the rest of `Home/` is Royal Throne's home screen)
- `Assets/Boosters/Propeller/V5/**`, `Assets/Boosters/ColorBall/V4/**`, `Assets/Boosters/ColorBall/V5/**`

- [ ] **Step 1:** Restore each path with `git restore --source=origin/royal-throne --staged --worktree -- <path>`. `.meta` files come along, so GUIDs are preserved.
- [ ] **Step 2:** Apply `COUPLINGS.md` resolutions to these files: cut, guard or replace.
- [ ] **Step 3:** In `RoyalQaService.cs` and `Assets/JellyGarden/Scripts/Editor/RoyalOfflineBuildPolicy.cs`, change the refusal on `UNITY_ADS` / `UNITY_INAPPS` so it applies only to a release player build. In the Editor and development builds, `StoryFreeGame.AdsAllowed` already blocks ads at runtime.
- [ ] **Step 4:** Remove every call to `RoyalHomeScreenBuilder.RequireCopy()` in the imported files, and do not import the builder.
- [ ] **Step 5:** `git add` everything.

### Task 3.6: Royal Throne level assets (master switch held back)

**Files:** modify `Assets/JellyGarden/Scripts/Item.cs` and `LevelManager.cs` (runtime skin components, Step 2). Add from `origin/royal-throne`:
- `Assets/JellyGarden/Shaders/` Royal shaders: `JuiceAdditive`, `CandyFragment`, `RoyalMatchFragment`, `RoyalParticle`, `RoyalRocketFire`, `RoyalTNTBody`, `RoyalTNTGlow`.
- `Assets/JellyGarden/Resources/`:
  - `RoyalGameplay/**` **except `EffectsProfile.asset`**, `Music/**` and `Win/**`;
  - `RoyalGameplayV4/**`, `RoyalRocketFX/**`, `RoyalColorBall/**`;
  - `JuiceConfig.asset`.
- `Assets/ReferenceGameplay/Resources/ReferenceGameplay/{Visuals,VFX,Audio,Sprites,Tools,Layout}/**` and `Assets/ReferenceGameplay/Resources/ReferenceGameplay/runtime-catalog.json`.
- The `Assets/ReferenceGameplay/{Animation,Art,Audio,VFX}/**` that those prefabs reference.
- The booster prefabs (`Propeller-V5`, `ColorBall-V5`, `ColorBall-V4` and their `-VFX`) live under `Assets/Boosters/*/V*/Prefabs/Resources/`. They already arrived with the Boosters folders in Task 3.5; confirm that, and do not copy them again.

Exact paths come from `IMPORT-MANIFEST.md`.

- [ ] **Step 1:** Restore the listed paths. Skip:
  - `Resources/RoyalTNT/**` (27 MB, a fallback only);
  - `ReferenceGameplay/Blockers`, `levels01-50`, `RoyalCampaignArt` (D8);
  - `Characters/**` (D2 default).
- [ ] **Step 2:** Do **not** edit the kit prefabs `Resources/Prefabs/Item.prefab` and the five block prefabs. Instead, add `RoyalPieceSkin` at runtime in `Item.Awake`, and `RoyalBlockSkin` where `LevelManager.CreateObstacles` instantiates a block, both only when `RoyalEffectsProfile.Get != null`. Main's rule forbids restructuring kit prefabs.
- [ ] **Step 3:** Hold back `Resources/RoyalGameplay/EffectsProfile.asset`. Without it, the Royal code falls back to kit behaviour; that is this phase's checkpoint.
- [ ] **Step 4:** `git add` everything.

### Task 3.7: Retire main's in-level Story layer

**Files:** delete the "Retired" scripts in §5. Modify `StoryBoardTheme.cs`, `StorybookBoardArt.cs`, `StoryVfxFlags.cs`, `StoryLevelHud.cs`, `StorySceneHooks.cs` and `StoryBoot.cs` where they install retired systems.

- [ ] **Step 1:** Delete each retired `.cs` file and its `.meta` with `git rm`. Do not delete any sprite, prefab, animation, material or shader.
- [ ] **Step 2:** Search `Assets/` for every retired class name and remove the remaining call sites. Expected hits include `StoryVfxFlags`, `StorySceneHooks`, `StoryBoot`, `StoryLevelHud`, `StoryPlaytest`, `StoryThemeTest` and `StoryWinPopup`.
- [ ] **Step 3:** Search for `KingLeoVfxHost`. If only retired code used it, retire it too. Otherwise keep it and record which meta screen uses it.
- [ ] **Step 4:** `StoryBoardTheme`: stop installing on `OnLevelLoaded` / `OnStartPlay` (D6).
- [ ] **Step 5:** `StoryLevelHud`: stop installing its in-level HUD. `ReadLevel`, `ReadLevelManager` and `HudTarget` stay, because `StoryLevelPopup` uses them.
- [ ] **Step 6:** `git add -A Assets/JellyGarden/Scripts/Story Assets/Editor`

### Task 3.8: Compile green, meta regression, commit

**Files:** none new.

- [ ] **Step 1:** `python3 tools/make_typecheck_csproj.py && dotnet build Story.Typecheck.csproj -v q --nologo`. Expected: `0 Error(s)`.
- [ ] **Step 2:** Open the worktree in Unity. Expected: Console has 0 compile errors.
- [ ] **Step 3:** Play from `main.unity`. Expected:
  - home opens;
  - level 1 opens from the level popup;
  - the board plays with kit fallback visuals (master switch still off);
  - win and lose reach `StoryWinPopup` / `StoryLosePopup`.
- [ ] **Step 4:** Close Unity. Re-run the Task 0.2 Step 3 suites. Expected: the same PASS counts as the baseline, except level-HUD cases (`LevelHudIsTargetAndMoves`, `ScoreLevelsShowTheirPassMark`, `PortraitBreathesAndReacts`). List those in `docs/port/BASELINE.md` as "retargeted in Task 5.1".
- [ ] **Step 5:** `git commit -m "feat: swap kit core to royal-throne's level layer (switch off)"`

---

## Phase 4 — Turn on and fit each feature

### Task 4.0: Make Royal Throne's Play-mode checks runnable here

**Files:**
- Add from `origin/royal-throne`: `Assets/JellyGarden/Scripts/Juice/RoyalRegressionRunner.cs` (if not already imported), plus these `Assets/JellyGarden/Scripts/Editor/` suites: `RoyalEffectsChecks`, `RoyalHintPresentationChecks`, `RoyalOverlapChecks`, `RoyalPropellerFormationChecks`, `RoyalSpecialFormationChecks`, `RoyalFormationReferenceChecks`, `RoyalMatchResponseChecks`, `RoyalSquarePlusOneChecks`, `RoyalBlockedRefillChecks`, `RoyalSuperBombFuseChecks`, `RoyalAllDualComboChecks`, `RoyalDualColorBallChecks`, `RoyalQaChecks`, `RoyalColorBallQaChecks`, `PropellerV5Checks`, `ColorBallV4Checks`, `ColorBallV5Checks`.
- Create `Assets/JellyGarden/Scripts/Story/Editor/PortChecks.cs`.

- [ ] **Step 1:** Restore the listed suites. Remove every `RequireCopy()` call and campaign-only branch (D8). Results go to `Logs/port-checks/<suite>.txt`, not `Design/`.
- [ ] **Step 2:** Write `PortChecks.cs`:
  - one menu item per suite, under **King Leo > Port checks**;
  - each item enters Play mode on level 1, waits for a settled board, wraps the suite in `RoyalQaService.StartSession()` / `EndSession()`, and runs it through `RoyalRegressionRunner.Run`;
  - plus **Run all**;
  - plus a file bridge: a line written to `Temp/port-check-command.txt` runs that suite in an already-open Editor, and the reply goes to `Temp/port-check-response.txt`.
- [ ] **Step 3:** Run **King Leo > Port checks > Run all** with the master switch still off. Expected: suites report that the Royal layer is inactive, or skip. None crash.
- [ ] **Step 4:** `git add` and commit with `test: run royal-throne play-mode checks under King Leo > Port checks`.

### Task 4.1: Master switch and board visuals

**Files:** add `Assets/JellyGarden/Resources/RoyalGameplay/EffectsProfile.asset` (+`.meta`) from `origin/royal-throne`.

- [ ] **Step 1:** Restore `EffectsProfile.asset`.
- [ ] **Step 2:** Play these levels:
  - level 1 (7×5);
  - a 9-wide board;
  - a board with holes;
  - an ingredient level, checking the exit arrows are on `IngredientExitRow` cells;
  - a TIME level;
  - Treasure Hunt boards 1–4.

  Expected on each:
  - the gold stroke follows the silhouette, including holes;
  - the board fits between the HUD and the rail at 393×852 and in the Editor's 4:3 view;
  - no kit outline;
  - ice and blocker skins fill their cells.
- [ ] **Step 3:** Take player shots of the same levels into `docs/port/shots/4.1/`.
- [ ] **Step 4:** Run **King Leo > Port checks > RoyalEffectsChecks (cell breaks)**. Expected: all PASS.
- [ ] **Step 5:** Commit `feat: turn on royal-throne board presentation`.

### Task 4.2: Gem art and six colours (D3)

**Files:** modify `Assets/JellyGarden/Scripts/Juice/RoyalGameplayArt.cs`, `Assets/ReferenceGameplay/Scripts/ReferenceGameplayCatalog.cs` and `Assets/ReferenceGameplay/Resources/ReferenceGameplay/runtime-catalog.json`. Add a sixth gem sprite under `Resources/RoyalGameplayV4/` or reuse King Leo's crown from `Resources/Story/RoyalBoard` (D1).

- [ ] **Step 1:** Make `BoardColorCount` read the loaded level's colour limit, and remove the colour-5→1 remap.
- [ ] **Step 2:** Add a sixth skin key and map colour 5 to it.
- [ ] **Step 3:** Check every consumer of `BoardColorCount` handles 6: `MostCommonColorBallColour`, Color Ball eligibility, `RoyalHintSolver`, `RoyalShuffle`.
- [ ] **Step 4:** Play a `COLOR LIMIT 6` level. Expected: 6 distinct gems; no two colours share a sprite; matches only join same-colour gems; a Color Ball can pick colour 5.
- [ ] **Step 5:** Commit `feat: keep each level's colour limit on royal-throne art`.

### Task 4.3: Powerup creation and cell ownership

**Files:** none expected; fixes go where the checks point.

- [ ] **Step 1:** Run the **Port checks** for `RoyalPropellerFormationChecks` (QA6), `RoyalSpecialFormationChecks` and `RoyalFormationReferenceChecks` (QA7), `RoyalSquarePlusOneChecks` (QA11), `RoyalSuperBombFuseChecks` (QA5). Expected: all PASS.
- [ ] **Step 2:** Run `RoyalOverlapChecks` (QA9, 99 rapid swaps). Expected: PASS, and `RoyalBoardIntegrity.Repairs` unchanged.
- [ ] **Step 3:** By hand on level 1, check each shape:

  | Shape | Expected result |
  |---|---|
  | Swipe-made line of 4 | Rocket oriented by the swap axis |
  | 2×2 square | Propeller |
  | L or T | TNT |
  | Line of 5 | Color Ball |
  | Square plus one extra gem | Propeller, with the extra gem clearing separately |
  | Cascade-made powerup | Appears in its own cell; nothing overlaps |
- [ ] **Step 4:** Commit any fixes with `fix: <what>`.

### Task 4.4: Powerup usage and combos

**Files:** none expected.

- [ ] **Step 1:** Run `RoyalAllDualComboChecks`: 15 pairs at 15, 30 and 60 FPS. Expected: all PASS.
- [ ] **Step 2:** Run `RoyalDualColorBallChecks`, `PropellerV5Checks`, `ColorBallV4Checks`, `ColorBallV5Checks`, `RoyalColorBallQaChecks`. Expected: all PASS.
- [ ] **Step 3:** By hand:
  - a tap costs exactly 1 move;
  - swiping a special into a gem fires it from its new cell and is never rejected;
  - TNT clears 5×5; TNT+TNT clears 9×9;
  - Rocket+TNT fires 3 rows and 3 columns; Rocket+Rocket fires a cross;
  - Color Ball + Rocket/TNT/Propeller converts the most common colour;
  - Color Ball + Color Ball clears every cell.
- [ ] **Step 4:** Check Propeller targeting on main's goal types (`COLLECT`, `BLOCKS`, `INGREDIENT`, `SCORE`). Expected: it targets unfinished goal cells and never targets an ingredient.
- [ ] **Step 5:** Commit any fixes.

### Task 4.5: Board behaviour

**Files:** possibly `Assets/JellyGarden/Scripts/Story/StoryDeadBoard.cs`.

- [ ] **Step 1:** Run `RoyalMatchResponseChecks` (QA8), `RoyalBlockedRefillChecks` (QA12), `RoyalQaChecks`, and `RoyalEffectsChecks` input (`RunInput`). Expected: all PASS.
- [ ] **Step 2:** Check input during a cascade by hand:
  - a settled pair can be swapped while another column falls;
  - a swap whose cell changes mid-animation is dropped, and its move refunded.
- [ ] **Step 3:** Hints: shown after 1 s idle, rotate every 3.5 s, cancelled by any touch.
- [ ] **Step 4:** Force a dead board with the QA practice board. Expected: `RoyalShuffle` runs once with its banner.
  - If `StoryDeadBoard` also fires `NoMatches`, retire `StoryDeadBoard` and record why.
  - Otherwise keep it as a last-resort watchdog.
- [ ] **Step 5:** On an ingredient level, ingredients reach `IngredientExitRow` and are collected. On a TIME level, the clock holds while a popup is open.
- [ ] **Step 6:** Commit.

### Task 4.6: HUD

**Files:**
- Modify `GUI/RoyalGameplayPresentation.cs`, `GUI/RoyalVisualCatalog.cs`, `Story/StorybookBoardArt.cs`, `Targets/TargetIcon.cs`, `Targets/TargetLevel.cs`.
- Modify `Story/StoryLevelPopup.cs` and `Story/StoryLevelHud.cs` (`HudTarget` icon source).

- [ ] **Step 1:** Make one goal-art source: `RoyalVisualCatalog.Goal`. `StorybookBoardArt.Goal`, `TargetIcon`, `TargetLevel`, the level popup and the HUD all use it, so the popup, HUD and board show the same gems.
- [ ] **Step 2:** Score goals: main treats a score goal as a real win condition, so the HUD shows it as a goal with its pass mark (`have/need` alone, the remaining amount when it shares the panel), matching the `HudTarget.DisplayShared` rule.
- [ ] **Step 3:** Collected pieces fly to the Royal Throne goal slots (`LevelManager.FlyToGoal`). Confirm `StoryLevelHud.AimKitIconsAtThisHud` is no longer installed.
- [ ] **Step 4:** Pause: the rail's Settings button opens the kit `MenuPause`, still restyled by `StorybookKitMenus`. The Story nav bar stays hidden in the level.
- [ ] **Step 5:** Layout at 393×852, a 4.7" phone and an iPad aspect. Expected: nothing clipped, and the safe area respected.
- [ ] **Step 6:** Retarget `StoryScreenTests` cases `LevelHudIsTargetAndMoves` and `ScoreLevelsShowTheirPassMark` to the Royal Throne HUD, or retire them with a note in `BASELINE.md`.
- [ ] **Step 7:** Commit `feat: royal-throne level HUD with King Leo goal rules`.

### Task 4.7: King avatar (D2)

**Files:**
- Modify `Assets/JellyGarden/Scripts/Juice/AvatarEvents.cs` (created in Task 3.2).
- Modify `GUI/RoyalGameplayPresentation.cs` (portrait mount and the remaining raise sites), `LevelManager.cs` and `Story/StoryAvatar.cs`.

- [ ] **Step 1:** Raise every `AvatarEvents` trigger. Task 3.2 covered `Item.cs`; add the raises in `RoyalGameplayPresentation` (moves watch, reactions, tools) and `LevelManager` (goals completed, extra moves, lost), at the places Royal Throne raised its `KingVanguard*` calls:

  | Event | Raised when |
  |---|---|
  | `Worried` | moves ≤ 3 |
  | `Matched` | a match resolves |
  | `PowerupCreated` | a powerup is created |
  | `Reaction(strength)` | an effect reports its strength |
  | `ToolUsed` | a tool is used |
  | `ToolStruck` | a tool hits |
  | `GoalsCompleted` | all goals are met |
  | `ExtraMoves` | extra moves are added |
  | `Lost` | the level is lost |

- [ ] **Step 2:** Mount `StoryAvatar` in the HUD's arched portrait, where `KingVanguardSlot.Mount(..., GameplayPortrait)` was, inside `RoyalGameplayHud.WindowMask`.
- [ ] **Step 3:** Map events to King Leo clips, using Royal Throne's throttling (4 s cooldown for strong reactions):

  | Event | Clip |
  |---|---|
  | idle | ping-pong |
  | `Worried` | worry, held |
  | `Matched` | pop |
  | `PowerupCreated`, `Reaction` 0.5–0.75 | cheer |
  | `Reaction` ≥ 0.75, `ToolStruck` | cheer (strong) |
  | `ToolUsed` | pop |
  | `GoalsCompleted`, `ExtraMoves` | cheer |
  | `Lost` | slump |

- [ ] **Step 4:** Verify by hand that each trigger plays its clip once and the avatar never restarts every frame. Re-point `StoryScreenTests.PortraitBreathesAndReacts` at the new mount.
- [ ] **Step 5 (alternative D2 only):** Import `Assets/Characters/KingVenguard/V2/**` with its shaders and `Resources/KingVanguard/**`, then subscribe `KingVanguardCharacter` to `AvatarEvents`.
- [ ] **Step 6:** Commit `feat: royal-throne avatar behaviour with King Leo art`.

### Task 4.8: Bottom booster bar

**Files:** modify `Assets/ReferenceGameplay/Tools/Scripts/ReferenceBoosterBar.cs`, `Story/StoryBoosters.cs`, `Story/StoryFreeGame.cs`.

- [ ] **Step 1:** The rail shows 4 tools plus Settings. The kit boost buttons re-parented by `RoyalGameplayPresentation` keep their `BoostIcon` badge logic. The soft-HUD tray is gone (Task 3.7).
- [ ] **Step 2:** Stock (D5):
  - `ReferenceBoosterBar` reads and writes counts through `StoryBoosters.Owned` (keys are `BoostType` names, the same as its own `RoyalSave` keys).
  - The zero-stock purchase panel does not open while `StoryFreeGame.ShopsAllowed` is false.
- [ ] **Step 3:** Grants:
  - Extend `StoryFreeGame`'s starter grant (once) and per-win random grant to include `RoyalHammer`, `RoyalArrow`, `RoyalCannon` and `RoyalJester`.
  - Keep Royal Throne's lock on levels 1–5, keyed to main's level number.
- [ ] **Step 4:** Check each tool by hand:
  - Hammer clears 1 cell; Arrow clears the tapped row; Cannon clears the tapped column (Spine animation); Jester shuffles to a layout with moves;
  - none costs a move;
  - none can fire while the board is resolving;
  - the badge count drops by 1.
- [ ] **Step 5:** Commit `feat: royal-throne in-level tools on King Leo's free economy`.

### Task 4.9: Pre-level boosters

**Files:** none expected.

- [ ] **Step 1:** Pick each pre-level booster on `StoryLevelPopup`, then press Play. Expected:
  - `StoryBoosters.Apply` → `SetPreBoosts` → `ChangeTypeCor` seats Royal Throne pieces with their creation reveal;
  - the `StoryPrePlay` plates show.
- [ ] **Step 2:** The popup's booster icons match Royal Throne's piece art (shared source from Task 4.6).
- [ ] **Step 3:** Run `StoryScreenTests.RunLevelPopupHeadless`. Expected: PASS.
- [ ] **Step 4:** Commit any fixes.

### Task 4.10: Win, lose and finish (D4)

**Files:** modify `LevelManager.cs` (`PreWinAnimationsCor`).

- [ ] **Step 1:** Win path. When goals are met:
  1. main's `CheckWinLose` runs;
  2. `PreWinAnimationsCor` runs `ReferenceWinFinish`, which flies leftover moves out as rockets and fires them;
  3. `StoryFlow` awards exactly one star;
  4. `StoryWinPopup` opens.

  No coins change.
- [ ] **Step 2:** Lose path: `PreFailed` hook → `StoryLosePopup`.
- [ ] **Step 3:** A Treasure Hunt board win returns to its area task flow.
- [ ] **Step 4:** Run `StoryWinPopupTests.RunHeadless`. Expected: PASS.
- [ ] **Step 5:** Commit.

---

## Phase 5 — Harnesses and full regression

### Task 5.1: Adapt main's harnesses

**Files:** modify `Story/StoryPlaytest.cs`, `Story/StoryThemeTest.cs`, `Story/Editor/StorybookBoardTests.cs`, `Story/Editor/StoryScreenTests.cs`.

- [ ] **Step 1:** `StoryPlaytest`:
  - make moves through `Item.SwitchDirection`;
  - only move pieces where `CanAcceptInput` is true;
  - treat the board as settled when `!LevelManager.IsResolvingMatches && !RoyalPowerupDirector.Busy`;
  - add `RoyalBoardIntegrity.Repairs` to the per-level report.
- [ ] **Step 2:** `StoryThemeTest` (D6): drop the backdrop and cell-fill assertions. Keep the "screen is not black" luminance check.
- [ ] **Step 3:** `StorybookBoardTests`: remove the board-art expectations that Royal Throne art replaces. Keep the layout checks across all 1,208 levels.
- [ ] **Step 4:** Commit `test: adapt King Leo harnesses to the royal-throne level`.

### Task 5.2: Rebase the "feel" guard (D7)

**Files:** modify `tools/check_vfx_intact.py`, `PLAN.md`.

- [ ] **Step 1:** After the owner signs off on D7, set `DEFAULT_BASE` in `tools/check_vfx_intact.py` to the Task 3.8 commit hash. Record why in the docstring.
- [ ] **Step 2:** Update `PLAN.md` "The two things that must not regress" to name Royal Throne's level feel as the protected baseline.
- [ ] **Step 3:** Run `python3 tools/check_vfx_intact.py`. Expected: PASS.
- [ ] **Step 4:** Commit.

### Task 5.3: Full regression

**Files:** update `docs/port/BASELINE.md` with an "After" column.

- [ ] **Step 1:** Run the typecheck, `check_vfx_intact.py`, `check_launch_args.py` and `validate_story.py`. Expected: all pass.
- [ ] **Step 2:** Run the Task 0.2 headless suites. Expected: baseline PASS counts, apart from the retargeted cases.
- [ ] **Step 3:** Run **King Leo > Port checks > Run all**. Expected: all PASS.
- [ ] **Step 4:** Run `python3 tools/playtest_sweep.py --levels 1-64`, then Treasure Hunt boards 1–4. Expected:
  - 0 findings;
  - `RoyalBoardIntegrity.Repairs == 0` on every level;
  - no `GenItem` live-occupant warning in any log.
- [ ] **Step 5:** Take player shots of every §3 row into `docs/port/shots/after/`. Compare them side by side with `shots/baseline/`, and with Royal Throne's own captures under `origin/royal-throne:Design/`.
- [ ] **Step 6:** Commit.

---

## Phase 6 — Cleanup, documentation, merge

### Task 6.1: Remove dead imported code
- [ ] **Step 1:** Delete the imported files Royal Throne itself never runs, once nothing references them: `RoyalColorBallSequence` (move `ColorBallPhase` into `ColorBallVisualV4` first), `ColorBallBoostView`, `RoyalMatchBreakService`, `RoyalPropellerV2*`, `ColorBallFrameAnimator` (move its static `Cell` first), and `UrpBloomSetup`.
- [ ] **Step 2:** Typecheck, then commit.

### Task 6.2: Documentation
- [ ] **Step 1:** Rewrite the in-level sections of `Assets/JellyGarden/Scripts/Story/CLAUDE.md` ("The in-level HUD", "Dressing the board", "The board can run out of moves") to cover:
  - Royal Throne's layer and its master switch;
  - `AvatarEvents`;
  - the tool economy;
  - the Port checks;
  - the reservation rule ("never write a cell without checking `Reserved`").
- [ ] **Step 2:** Update `tools/README.md` with `port_guid_audit.py` and the Port checks.
- [ ] **Step 3:** Commit.

### Task 6.3: Old Story assets inventory
- [ ] **Step 1:** List the sprites, prefabs and materials that are now unreferenced because of the retired Story scripts, in `docs/port/UNUSED-ASSETS.md`. Do not delete them; main's rule forbids it. The owner decides later.
- [ ] **Step 2:** Commit.

### Task 6.4: Provenance release gate (D1)
**Files:** create `tools/check_provenance.py`; modify `Assets/JellyGarden/Scripts/Story/Editor/StoryBuild.cs`.
- [ ] **Step 1:** Write `tools/check_provenance.py`. It reads `docs/port/PROVENANCE.md` and exits non-zero if any APK-derived path is in a build: present under `Assets/` and referenced from a build scene or from `Resources`.
- [ ] **Step 2:** Call it from `StoryBuild.BuildMacRelease` and `StoryBuild.BuildIos`; a failure aborts the build. Development builds only warn.
- [ ] **Step 3:** Run it. Expected while placeholders remain: it fails and lists them; that is the point.
- [ ] **Step 4:** Commit.

### Task 6.5: Merge
- [ ] **Step 1:** `git push -u origin port/royal-throne-level`, then open a PR into `main`. The PR includes:
  - the decision record (§1 with the answers);
  - links to `docs/port/*`;
  - test evidence;
  - before and after shots.
- [ ] **Step 2:** After review and merge, run `git worktree remove ../king-leo-port`.

---

## Acceptance criteria

| Feature | Done when |
|---|---|
| Board | Royal Throne frame, cells and camera fit on every level shape, the 4:3 view and 393×852. Exit arrows sit on `IngredientExitRow`. |
| Pieces | Royal Throne skins; each level's own colour limit (3–6), with 6 distinct gems. |
| Creation | Rocket, Propeller, TNT and Color Ball from the right shapes, with the gather and reveal. QA5/6/7/11 checks PASS. |
| Ownership | QA9 PASS. `RoyalBoardIntegrity.Repairs == 0` across levels 1–64 and Treasure Hunt boards. No `GenItem` occupant warning. |
| Usage and combos | All 15 pairs PASS at 15/30/60 FPS. A tap costs 1 move. A powerup swap is never rejected. |
| Behaviour | Swapping during cascades works. Hints and shuffle work. QA8 and QA12 PASS. Ingredient and TIME levels work. |
| HUD | Goals (including score with its pass mark), moves or time, portrait and gear. Collected pieces fly to the slots. Safe area respected. |
| Avatar | Each trigger plays its King Leo clip once, throttled. |
| Booster bar | 4 tools work without spending a move. Counts come from `StoryBoosters.Owned`. No shop. Locked on levels 1–5. |
| Meta intact | Baseline headless suites PASS. A level popup → play → win/lose → area/home round trip works. One star per first win. |
| Guards | Typecheck, `check_vfx_intact` (rebased), `check_launch_args` and `validate_story` all pass. The provenance gate lists every remaining APK asset. |

## Risks

| Risk | Impact | Mitigation |
|---|---|---|
| APK-derived assets ship | Copyright claim, store removal | D1, `PROVENANCE.md`, release gate (Task 6.4), replacement-art track |
| Third-party licences (Spine runtime, AllIn1 per seat, BorisBlackBloxx font) | Licence breach | Clear them in Task 1.1 before any release |
| Colour clamp slips through | 403 levels change difficulty | D3, Task 4.2, 6-colour level in the playtest band |
| GUID collisions on import | Broken references, silent wrong assets | Task 1.2: lists (c) and (d) must be empty before Phase 3 |
| Branch switch under an open Editor | Settings overwritten, false compile errors | Separate worktree folders; never switch inside an open project |
| `RoyalSave` aliasing `PlayerPrefs` | QA sessions write to an isolated key prefix; a crash mid-session could strand real progress | `RoyalQaService.EndSession` in `finally` (`RoyalRegressionRunner`); never start sessions in player builds (`ROYAL_DISABLE_QA`) |
| Royal Throne skins update every frame (`RoyalPieceSkin.LateUpdate`, `StyleBoard` every 0.3 s) | Frame time on older phones | Profile level 1 and a 9×9 board on the oldest target device before merge |
| Play-mode-only suites | Agents cannot run them headless | Task 4.0 file bridge in an open Editor; `StoryPlaytest` headless for broad coverage |
| Main's meta code reading retired names (`MovesValue`, soft-HUD objects) | Null references at runtime | Task 3.7 Step 2 search, and the Task 3.8 round trip |

## Appendix A — main's `Story*` hooks in `Item.cs` (all dropped)

`StoryOriginalColorBall.Fire`, `.Tappable` · `StoryOriginalTnt.OwnsBlast`, `.ClearArea`, `.ComboAnticipation`, `.Show`, `.Ignite`, `.BeginWave`, `.EndWave`, `.WaveDelay`, `.SingleRadius`, `.DualRadius`, `.Ready`, `.InCombo` · `StoryOriginalBoosters.Ready`, `.ShowPiece`, `.OwnsRocket`, `.OwnsColorBall`, `.ColorBallConvert`, `.MatchBreak`, `.TntTnt`, `.RocketTnt`, `.DualColorBall`, `.ColorBall` · `StoryOriginalMotion.Ready`, `.Fall`, `.Break`, `.Silence` · `StoryRocketVfx.Enabled`, `.Contact` · `StoryRocketSweep.Show`, `.Hit`, `.HitObstacle` · `StoryBombVfx.Blow`, `.TintFor` · `StoryColorBombVfx.Fire` · `StoryRoyalVfx.TntReady` · `StoryPunchVfx.Punch` · `StorybookBoardArt.Apply` · `Match3PieceMotion.Pop`.

## Appendix B — main's hooks outside `Item.cs`

| File | Kept | Dropped |
|---|---|---|
| `LevelManager.cs` | `StoryPrePlay.Open`, `StoryPrePlay.Enabled`, `StoryOneStar.CapStars`, `StoryCampaign.ProtectFirstPuzzle`, `StoryCampaign.ApplyLevelTuning`, `IngredientExitRow` | `StoryBoardScale.FitToCell` ×7, `.ItemScale`, `.Factor`, `.Apply`; `StoryOriginalMotion.Enter` ×2, `.MarkMatch`, `.BeginRefill`; `StoryRoyalVfx.StateChanged`; `StoryRocketSweep.HasPendingHits`; `StoryOriginalTnt.Anticipating`; `StoryOriginalColorBall.Busy`; `StoryOriginalBoosters.SendMove`, `.MovesInFlight`; `StoryBoardTheme.DrawKitOutline`; `KingLeoVfxHost.Configure` |
| `Square.cs` | `IngredientExitRow` arrow | `StorybookBoardArt.Enabled` (condition replaced), `StoryBoardScale.ItemScale` |
| `InitScript.cs` | `StoryFreeGame.ShouldSpendLife`, `.AdsAllowed`, `StoryFlow.HandleMapNodeClicked` | — |
| `GUI/MenuManager.cs` | `StoryFreeGame.ShopsAllowed` ×2, `.RatePromptAllowed`, `.PreFailOfferAllowed` | — |
| `GUI/AnimationManager.cs` | `StoryFlow.HandleLevelFailed`, `.HandleLevelComplete` | — |
| `Targets/TargetLevel.cs`, `Targets/TargetIcon.cs` | goal-art call site (rerouted, Task 4.6) | `StorybookBoardArt.Goal` as the art source |
