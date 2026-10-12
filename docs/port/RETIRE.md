# Story retirement map (plan Task 3.7, §5 Retired / Reduced)

Source of truth for retiring main's in-level Story layer during the royal-throne level port.
Plan: `docs/superpowers/plans/2026-10-12-royal-throne-level-port.md`. Decisions D1–D8 use their DEFAULT column.

- **Measured on:** branch `port/royal-throne-level` at `b47ca0fa`, which is main plus the plan doc. All line numbers are against that commit.
- **Kit-core files are left out.** `LevelManager.cs`, `Item.cs`, `Square.cs`, `System/CombineManager.cs`, `AI.cs` and `InputManager.cs` are replaced wholesale by royal-throne's (§4). Their hooks are listed only in §6, as a cross-check.
- **Checker:** `tools/port_retire_check.py` checks the tree against this map (§7).

---

## 0. Summary

| | Count |
|---|---|
| Files retired by plan §5 (all present, tracked, each with a tracked `.meta`) | 37 |
| Files this map adds to the retired list (§3) | 4: `KingLeoVfxHost`, `StoryVfxFlags`, `StorybookBoardReview`, `Story/Editor/StoryBoardBorderTests` |
| Kept files that need a code change to compile | 8: `StorySceneHooks`, `StoryBoardTheme`, `StoryLevelHud`, `StorybookBoardArt`, `StoryOriginalWinStars`, `StoryShellTest`, `Story/Editor/StoryScreenTests`, `Story/Editor/StorybookBoardTests` |
| Compile errors in kept files after deleting the 41 files | 31, at 30 sites. All are listed in §1 and §2. |
| Scenes, prefabs or assets that serialise a retired script | **0** (GUID scan of `.unity/.prefab/.asset/.controller/.anim/.mat/.playable/.preset` and `ProjectSettings/`) |
| Retired scripts with a non-zero `executionOrder` in their `.meta` | 0 |
| Sprites, prefabs, animations, materials or shaders that must be deleted | **0** (§5) |
| `StoryBoot.cs` changes | **None.** It references no retired or reduced type, though the plan expected a hit. |

**Verified by typecheck.** `tools/port_retire_check.py --typecheck` runs on a copy of `HEAD`, outside the project:

| Run | Errors |
|---|---|
| `HEAD` as it is | 0 |
| The 41 files deleted | 110: 79 in the three kit-core files, 31 in kept files |
| The 41 files deleted and every fix in §1–§3 applied (scratch copy) | 80, all in `Item.cs` (60), `LevelManager.cs` (19) and `Square.cs` (1), which Tasks 3.1–3.3 replace with royal-throne's |

---

## 1. Retired scripts: paths, types, and every outside reference

All paths are under `Assets/JellyGarden/Scripts/Story/` unless shown otherwise. Each `.cs` goes together with its `.meta` (`git rm`). "Nested" lists types declared inside the class. They are reached only as `Outer.Nested`, so a search for the outer name covers them.

### 1.1 Path and type inventory

| Retired file | Lines | Top-level types (nested) | Self-entry point |
|---|---|---|---|
| `StoryOriginalBoosters.cs` | 674 | `StoryOriginalBoosters` (`Count`), `StoryOriginalBoostersHost` | `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` |
| `StoryOriginalBoostersValidation.cs` | 601 | `StoryOriginalBoostersValidation` (ns `…Story.Editor`, `#if UNITY_EDITOR`) | menu `JellyGarden/Story/Preview/Validate Original Boosters` |
| `StoryOriginalTnt.cs` | 468 | `StoryOriginalTnt`, `StoryOriginalTntHost` | RIOLM SubsystemRegistration |
| `StoryOriginalTntPiece.cs` | 107 | `StoryOriginalTntPiece` | — |
| `StoryOriginalTntValidation.cs` | 386 | `StoryOriginalTntValidation` (ns `…Story.Editor`, `#if UNITY_EDITOR`) | menu `JellyGarden/Story/Preview/Validate Original TNT` |
| `StoryOriginalColorBall.cs` | 538 | `StoryOriginalColorBall`, `StoryOriginalColorBallHost` (`Run`, `Beam`) | RIOLM SubsystemRegistration |
| `StoryOriginalMotion.cs` | 437 | `StoryOriginalMotion` (`Match`), `StoryOriginalFall` | RIOLM SubsystemRegistration |
| `StoryOriginalChargedTarget.cs` | 255 | `StoryOriginalChargedTarget` (`Sparkle`) | RIOLM SubsystemRegistration |
| `StoryOriginalPiece.cs` | 151 | `StoryOriginalPiece` | — |
| `StoryRocketVfx.cs` | 785 | `StoryRocketVfx` (`Piece`, `Puff`, `Head`, `Driver`) | — |
| `StoryRocketSweep.cs` | 245 | `StoryRocketSweep` | — |
| `StoryRocketFlight.cs` | 330 | `StoryRocketFlight` (`Head`, `Mote`) | — |
| `StoryRocketVariant.cs` | 240 | `enum RocketVariant`, `StoryRocketVariant`, `struct RocketPlume` | — |
| `StoryRocketValidation.cs` | 276 | `StoryRocketValidation` (ns `…Story.Editor`, `#if UNITY_EDITOR`) | menu `JellyGarden/Story/Preview/Validate Rocket Animation` |
| `StoryBombVfx.cs` | 859 | `StoryBombVfx` (`Piece`, `Blast`, `Mote`, `Driver`) | — |
| `StoryBombVariant.cs` | 253 | `enum BombVariant`, `StoryBombVariant`, `struct BombBlast` | — |
| `StoryColorBombVfx.cs` | 703 | `StoryColorBombVfx` (`Piece`, `Target`, `Run`, `Driver`) | — |
| `StoryBlastVfx.cs` | 589 | `StoryBlastVfx` (`Rig`, `Ticker`, `Ticker.Waiting`) | — |
| `StoryShatterVfx.cs` | 499 | `StoryShatterVfx` (`Shard`, `Driver`, `Driver.Waiting`) | — |
| `StoryPunchVfx.cs` | 236 | `StoryPunchVfx` | — |
| `StoryPopVfx.cs` | 196 | `StoryPopVfx` (`Ease`) | — |
| `StoryRoyalVfx.cs` | 148 | `StoryRoyalVfx` | RIOLM SubsystemRegistration |
| `StoryExplosionProbe.cs` | 300 | `StoryExplosionProbe` | installed by `StorySceneHooks` |
| `StoryVfxLab.cs` | 619 | `StoryVfxLab` | RIOLM AfterSceneLoad, and `StorySceneHooks` |
| `StoryVfxVariant.cs` | 275 | `enum VfxVariant`, `StoryVfxVariant`, `struct ShatterPhysics` | — |
| `StoryFinishVfx.cs` | 277 | `StoryFinishVfx` (`Driver`, `Driver.Pending`) | — |
| `StoryBoardScale.cs` | 130 | `StoryBoardScale` | — |
| `StoryBoardCentre.cs` | 129 | `StoryBoardCentre` | installed by `StorySceneHooks` |
| `StoryPixelSnap.cs` | 202 | `StoryPixelSnap` | installed by `StorySceneHooks` |
| `StorybookBoardLayout.cs` | 148 | `StorybookBoardLayout` | — |
| `StoryRoyalBoardFrame.cs` | 176 | `StoryRoyalBoardFrame` (`Edge`) | — |
| `StorybookRocketFacing.cs` | 40 | `StorybookRocketFacing` | added by `StorybookBoardArt.Apply` |
| `StoryLevelHud.Soft.cs` | 228 | **partial** `StoryLevelHud`. Only the members listed under §2.4 go; the class stays. | — |
| `StoryRoyalBoardValidation.cs` | 148 | `StoryRoyalBoardValidation` (ns `…Story.Editor`, `#if UNITY_EDITOR`) | — |
| `StoryPowerupTest.cs` | 500 | `StoryPowerupTest` (`Made`) | installed by `StorySceneHooks` |
| `Editor/RocketValidationBridge.cs` | 120 | `RocketValidationBridge` | `[InitializeOnLoad]` |
| `Assets/Editor/RocketFlameGapCheck.cs` | 85 | `RocketFlameGapCheck` (global namespace) | — |

The type names used for the search are the 46 top-level names above. No retired file declares an extension method. No `using static` of a retired type exists. No reflection names a retired type by string. The only partial class is `StoryLevelHud` (`StoryLevelHud.cs` + `StoryLevelHud.Soft.cs`).

Nothing under `tools/`, the shell scripts or the `docs/` tree launches a retired entry point. That covers menu items, `-executeMethod` targets and launch flags (`-storyrocket`, `-storyvfxlab`, `-storyexplosionprobe`, `-original*`, `-story*variant=`, `-storybook-board-review=`).

### 1.2 Every reference from a kept, non-kit-core file

Comments are listed separately (§1.3), because they do not affect compiling. "CS" marks a line that fails to compile once the retired files are gone; each was confirmed by the typecheck.

| File:line | Line (trimmed) | Retired type | Fix |
|---|---|---|---|
| `StorySceneHooks.cs:49` | `StoryPunchVfx.Reset();` | StoryPunchVfx | CS. Delete lines 42–49: the comment block "Not an Install — a clear…" and the call. |
| `StorySceneHooks.cs:56` | `StoryBombVfx.Reset();` | StoryBombVfx | CS. Delete lines 51–57: the comment "Same reason: …" and both resets. |
| `StorySceneHooks.cs:57` | `StoryColorBombVfx.Reset();` | StoryColorBombVfx | CS. Same block as line 56. |
| `StorySceneHooks.cs:63` | `StoryBoardCentre.Install();` | StoryBoardCentre | CS. Delete the line. |
| `StorySceneHooks.cs:64` | `StoryPixelSnap.Install();` | StoryPixelSnap | CS. Delete the line. |
| `StorySceneHooks.cs:77` | `StoryPowerupTest.Install();` | StoryPowerupTest | CS. Delete the line. |
| `StorySceneHooks.cs:78` | `StoryExplosionProbe.Install();` | StoryExplosionProbe | CS. Delete the line. |
| `StorySceneHooks.cs:79` | `StoryVfxLab.Install();` | StoryVfxLab | CS. Delete the line. |
| `StoryBoardTheme.cs:295` | `if (StorybookBoardArt.Enabled) StorybookBoardLayout.DressBackdrop(image);` | StorybookBoardLayout | CS. Delete the line. |
| `StoryBoardTheme.cs:446` | `float size = StoryBoardScale.KitPitch * (1f - CellGap);` | StoryBoardScale | CS. Replace `StoryBoardScale.KitPitch` with a local `private const float KitPitch = 0.82f;`, the value at `StoryBoardScale.cs:40`. |
| `StoryBoardTheme.cs:473` | `new Vector2(0.5f, 0.5f), 8f / StoryBoardScale.KitPitch);` | StoryBoardScale | CS. Use `KitPitch`. |
| `StoryBoardTheme.cs:568` | `if (StorybookBoardArt.Enabled) { StorybookBoardLayout.DressCells(LevelManager.THIS); return; }` | StorybookBoardLayout | CS. Replace with `if (StorybookBoardArt.Enabled) return;`, which mirrors line 650. |
| `StoryBoardTheme.cs:752` | `float cell = StoryBoardScale.KitPitch;` | StoryBoardScale | CS. Use `KitPitch`. |
| `StoryBoardTheme.cs:799` | `float cell = StoryBoardScale.KitPitch;` | StoryBoardScale | CS. Use `KitPitch`. |
| `StoryBoardTheme.cs:834` | `float cell = StoryBoardScale.KitPitch;` | StoryBoardScale | CS. Use `KitPitch`. |
| `StorybookBoardArt.cs:81` | `item.gameObject.AddComponent<StorybookRocketFacing>();` | StorybookRocketFacing | CS. Delete the whole `Apply(Item)` method, lines 61–82. Its only caller is `Item.cs:132`, which §4 drops. |
| `StoryLevelHud.cs:400` | `authored.BuildSoftHud(strip);` | Soft partial | CS. Delete the block at lines 398–403, `if (StorybookBoardArt.Enabled) { authored.BuildSoftHud(strip); authored.Refresh(); return; }`. |
| `StoryLevelHud.cs:429` | `hud.BuildSoftHud(strip);` | Soft partial | CS. Delete the block at lines 427–432. |
| `StoryLevelHud.cs:852` | `if (_softRoot != null) { RefreshSoft(model); return; }` | Soft partial | CS (2 errors). Delete the line. |
| `StoryLevelHud.cs:1540` | `if (_softRoot != null) Destroy(_softRoot.gameObject);` | Soft partial | CS. Delete the line. |
| `StoryOriginalWinStars.cs:38` | `if (!Application.isPlaying \|\| effects == null \|\| !StoryOriginalBoosters.Ready) return null;` | StoryOriginalBoosters | CS. Drop the `\|\| !StoryOriginalBoosters.Ready` term. `Build()` at lines 56–59 already returns false when `StarHitParticles`, `StarIdleParticles` or `RoyalGameplay/UIWinGlow` is missing, so `Attach` still returns null when they are absent. Behaviour change: the switch `-nooriginalboosters` / pref `StoryVfx.OriginalBoosters=0` no longer turns the win stars off. |
| `StoryShellTest.cs:431` | `if (StoryPowerupTest.IsFinished && !StoryNavBar.LevelIsPlaying &&` | StoryPowerupTest | CS. Drop `StoryPowerupTest.IsFinished && `. `IsFinished` was already true when the test was not installed (`StoryPowerupTest.cs:57-60`), so behaviour is unchanged. Optionally reword the doc comment at lines 421–425. |
| `Editor/StoryScreenTests.cs:881` | `Expect(Mathf.Abs(StoryBoardCentre.OffsetFor(9, squareWidth)) < 0.01f,` | StoryBoardCentre | CS. Delete the whole case `BoardsAreCentredWhateverTheirWidth`: its doc comment and body (lines 864–904) and its call at line 128. Royal Throne's `PlayableViewport` replaces the centring. This removes 1 case from `StoryScreenTests.RunHeadless`; record it in `BASELINE.md` as "retired with StoryBoardCentre". |
| `Editor/StoryScreenTests.cs:883` | `StoryBoardCentre.OffsetFor(9, squareWidth) + ")");` | StoryBoardCentre | CS. Same case as line 881. |
| `Editor/StoryScreenTests.cs:884` | `Expect(StoryBoardCentre.OffsetFor(5, squareWidth) < -0.15f,` | StoryBoardCentre | CS. Same case. |
| `Editor/StoryScreenTests.cs:886` | `StoryBoardCentre.OffsetFor(5, squareWidth) + ")");` | StoryBoardCentre | CS. Same case. |
| `Editor/StoryScreenTests.cs:895` | `float aimedAt = StoryBoardCentre.OffsetFor(columns, squareWidth);` | StoryBoardCentre | CS. Same case. |
| `Editor/StorybookBoardTests.cs:47` | `Rect field = StorybookBoardLayout.FieldRect(safe);` | StorybookBoardLayout | CS. Delete the layout loop, lines 30–54 (`int layouts = 0;` through the closing brace of the `for (level…)` loop). Change the PASS message at line 55 so it no longer prints `layouts`. **Plan conflict:** see §8.2. |
| `Editor/StorybookBoardTests.cs:48` | `float pitch = StorybookBoardLayout.FitPitch(safe, cols, rows);` | StorybookBoardLayout | CS. Same loop as line 47. |
| `Editor/StorybookBoardTests.cs:49` | `float allow = StorybookBoardLayout.Allowance;` | StorybookBoardLayout | CS. Same loop as line 47. |
| `StorybookBoardReview.cs:115` | `Rect field = StorybookBoardLayout.FieldRect(StorybookBoardLayout.Safe);` | StorybookBoardLayout | CS. **Retire the file** (§3). |
| `Editor/StoryBoardBorderTests.cs:61, 62, 74` | `StoryBoardScale.Pitch` / `.Factor` | StoryBoardScale | CS. **Retire the file** (§3). |
| `KingLeoVfxHost.cs:28` | `StoryRoyalVfx.Prewarm();` | StoryRoyalVfx | CS. **Retire the file** (§3). |

Two string couplings exist only inside retired files: `StoryRoyalBoardValidation.cs:92` looks up `GameObject.Find("RoyalBoardHud")`, the Soft HUD root, and `StorybookBoardReview.cs:86,91` looks up buttons named `"Booster0".."Booster3"`, the Soft HUD tray. No kept file looks up a Soft HUD object by name. `StoryLevelPopup.cs:238` builds its own `"Booster"+boost` rects, which is unrelated.

### 1.3 Mentions in comments only (no compile effect; optional cleanup)

| File:line | Mentions |
|---|---|
| `StorySceneHooks.cs:42–48, 51–55` | `StoryPunchVfx` (deleted with its block, §1.2) |
| `StorySceneHooks.cs:80–81` | "it waits for the board self-test above". Delete the comment, since the board self-test is retired. |
| `StorySelfTests.cs:3` | `StoryPowerupTest` |
| `StoryPlaytest.cs:3` | `StoryPowerupTest` |
| `Editor/StoryScreenPreview.cs:100` | `StoryPowerupTest` |
| `StoryOriginalWinStars.cs:1` | "See StoryOriginalBoosters." |
| `StoryVfxFlags.cs:91–92` | `StoryVfxVariant`, `StoryRocketVariant`, `StoryBombVariant` (file retired, §3) |
| `StoryBoardTheme.cs:394, 750` | `StoryBoardScale.Factor` |
| `Assets/JellyGarden/Scripts/Story/CLAUDE.md` | 9 mentions: `StoryBoardBorderTests`, `StoryBoardCentre`, `StoryPixelSnap`, `StoryPowerupTest`. Task 6.2 rewrites this file. |
| `tools/level_gen.py:116` | `StoryBoardScale` (comment) |

---

## 2. Reduced files: what goes, what stays, and who needs it

### 2.1 `StorySceneHooks.cs` (installer)

**Remove** these lines; every one of them is verified by the typecheck except `StoryBoardTheme.Install` and `StoryLevelHud.Install`, which are removed by decision rather than because they fail to compile:
- lines 42–57, the three VFX resets and their comments;
- line 62 `StoryBoardTheme.Install();` (D6, §2.2);
- line 63 `StoryBoardCentre.Install();`;
- line 64 `StoryPixelSnap.Install();`;
- line 67 `StoryLevelHud.Install();` (§2.4);
- lines 77–79 `StoryPowerupTest.Install();`, `StoryExplosionProbe.Install();` and `StoryVfxLab.Install();`;
- lines 80–81, the comment.

**Keep** every other call, in the same order:
- `StorySplash`, `StoryMusic`, `StoryLevelFullBleed`, `StoryMapLabels`, `StoryOneStar`, `StoryWinBanner`, `StorybookKitMenus`, `StoryBoosters`, `StoryNavBar`, `StoryAreasList`, `StoryShell` and `StoryShellTest`;
- `StoryDeadBoard.Install()` until Task 4.5 decides on it.

### 2.2 `StoryBoardTheme.cs` (D6: the area painting is no longer behind the board)

**Compile fixes** (§1.2):
- add `private const float KitPitch = 0.82f;`;
- replace the 5 uses of `StoryBoardScale.KitPitch`;
- delete line 295;
- line 568 becomes `if (StorybookBoardArt.Enabled) return;`.

**Stop installing:** remove `StorySceneHooks.cs:62`. Nothing else creates the component (`Install()` at lines 107–115 is its only factory), so `OnEnable`/`OnDisable` (the `LevelManager.OnLevelLoaded` / `OnStartPlay` subscriptions at lines 117–127), `LateUpdate` (877–887) and `Apply` never run. Optionally make `Install()` an empty method with a D6 comment.

**Must stay (Phase 3):** these public statics, read by `StoryThemeTest.cs` at lines 309, 356–359, 370, 379, 387, 400–401 and 421:
- `UseStoryBackdrop`, `UseStoryCellFill`;
- `LastBackdrop`, `LastBackdropWasKitDefault`;
- `LastFill`, `LastFilledCells`.

`DrawKitOutline` is read only by main's `LevelManager.cs:513`, which §4 drops. Leaving it is harmless.

**Full reduction (Task 5.1, together with `StoryThemeTest`):** once `StoryThemeTest` drops its backdrop and cell-fill assertions (plan Task 5.1 Step 2), the remaining in-level code can go:
- `Apply`, `ApplyBackdrop`, `ShowBackdrop` and `RestoreKitBackdrop`;
- `FillSprite`, `FlatSprite` and `ApplyCellFill`;
- `DarkenKitChecker`, `ApplyBoardOutline`, `Bar`, `Elbow` and `ArcSprite`;
- `LateUpdate` and `OnEnable`/`OnDisable`.

The file then reduces to the statics, or retires. `StoryBoardBorderTests`, the only reflection user of `FillSprite` and `ApplyBoardOutline`, is already retired by §3.

### 2.3 `StorybookBoardArt.cs` (goal and booster art only)

**Remove:** `Apply(Item)`, lines 61–82. Its only caller is main's `Item.cs:132` in `Awake`, which §4 drops. It also referenced the retired `StorybookRocketFacing`.

**Must stay:**

| Member | Kept users |
|---|---|
| `Enabled` | `StoryBoosters.cs:104`; `StoryPrePlay.cs:184,194,204,213`; `StoryLevelHud.cs:1035,1053` (and `398/427` until those blocks are deleted); `StoryBoardTheme.cs:254,295,568,650` |
| `Get(string)` | `StoryLevelHud.cs:1053` (`"score"`), `StoryBoardTheme.cs:254` (`"courtyard"`) |
| `Piece(int)` | `StoryPrePlay.cs:184`, `Editor/StorybookBoardTests.cs:23` |
| `Booster(BoostType)` | `StoryBoosters.cs:104` (the level popup's booster icons), `StoryPrePlay.cs:194,204,213` |
| `Goal(TargetObject)` | `Targets/TargetLevel.cs:51`, `Targets/TargetIcon.cs:48`, `StoryLevelHud.cs:1026` |

Task 4.6 reroutes `Goal` to `RoyalVisualCatalog.Goal`. Until then this file is the single goal-art source.

### 2.4 `StoryLevelHud.cs` (keeps the model; installs no in-level HUD)

**Removed with `StoryLevelHud.Soft.cs`:**
- fields `_softRoot`, `_softCard`, `_softTray`, `_softCrest`, `_royalPause`, `_royalMovesInset`, `_royalExtra`, `_softProgress`, `_softBoosts`, `_softBoostArt`, `_softBoostCounts`, `_softTypes`, `_softUnit` and `_softInset`;
- constants `GoalRowWidth` and `GoalRowHeight`;
- methods `BuildSoftHud`, `RoyalText`, `PlaceSoft`, `FontSize`, `LayoutSoft`, `RefreshSoft` and `UseSoftBoost`;
- the Unity messages `OnEnable` and `OnDisable`.

Fix the four call sites at lines 398–403, 427–432, 852 and 1540 (§1.2).

**Stop the in-level HUD:**
1. Remove `StorySceneHooks.cs:67`. `Install`/`Attach` then never run, so the kit strip is no longer taken over at runtime.
2. **The component is also baked into `Assets/JellyGarden/Scenes/game.unity`.** It is the only reduced or retired script that any scene serialises (GUID scan). Its `Awake` (513) binds and sets `Instance`, `Update` (835–840) refreshes ten times a second and calls `React` → `StoryAvatar.React`, and `LateUpdate` (842–845) runs `AimKitIconsAtThisHud`, which moves the kit's TargetIcons. Gate the two per-frame messages:
   - `public static bool Enabled = true;` (line 61) becomes `public static bool Enabled => JellyGarden.Scripts.Juice.RoyalEffectsProfile.Get == null;`. `Enabled` is never assigned anywhere, only mentioned in comments.
   - Add `if (!Enabled) return;` as the first line of `Update()` and `LateUpdate()`.

   Do not gate `Refresh()`: the editor tests call it directly.

   This gate needs `Juice/RoyalEffectsProfile.cs`, which Task 3.5 imports in the same compile unit. The typecheck in §0 used `if (!Enabled) return;` with the current field.
3. **Do not deactivate the baked GameObject.** `StoryHudBaker.AdoptKitParts` reparented the kit's `Targets` (`TargetsGroup`) under it. `TargetsGroup.OnDisable` destroys the TargetIcons, and royal-throne's `LevelManager.CheckCollectedTarget` still falls back to `objectTarget.guiObj` when `RoyalGameplayPresentation.GoalIcon` fails (`origin/royal-throne:Assets/JellyGarden/Scripts/LevelManager.cs:636–641`).

**Why the gate rather than a constant `false`:**
- Royal-throne adds `RoyalGameplayPresentation` only when the profile exists (`origin/royal-throne:…/LevelManager.cs:524`).
- So from Task 3.8 to Task 4.1, with the master switch off, the baked strip is the level's only HUD. Unconditionally off, it would freeze on stale numbers during the Task 3.8 smoke test.
- Once the switch is on, royal-throne hides `Canvas/Panel`, the strip's parent, with a CanvasGroup (`RoyalGameplayPresentation.cs:290–294`), and the gate makes the component inert.

**Must stay, by user:**

| Member | Users |
|---|---|
| `ReadLevel(int,int)`, `HudModel` (`Targets`, `LimitTitle`, `LimitValue`, `LimitLeft`, `PassMark`), `HudTarget` (`Icon`, `Backing`, `Remaining`, `IsScore`, `Done`, `Text`, `Display`, `DisplayShared`) | `StoryLevelPopup.cs:297–308`; `Editor/StorybookTests.cs:72`; `Editor/StoryScreenTests.cs:1501–1617, 1996` |
| `ReadLevelManager()` | `Refresh()` (the fallback HUD while the switch is off); plan §5 keeps it |
| `ModeOf(int)` | `Editor/StoryScreenTests.cs:1573, 2011` |
| `AddTargets`, `Slotify`, `ScoreSlot`, `ScoreText`, `BlockBacking` | internal to `ReadLevel` / `ReadLevelManager` |
| `Build(Transform,float)`, `Source`, `HostName`, `Refresh()`, `VisibleTargetCount`, `LimitText`, `LimitTitleText`, `TargetTexts()` | `Editor/StoryScreenTests.cs` HUD cases (1316–1617, 2382); `Editor/StoryHudBaker.cs` (97–171, 253) |
| `FindBoosterRow`, `MovePauseOutOfTheHudsWay`, `EnsureBound` | `Editor/StoryHudBaker.cs:160–324` |

`StoryScreenTests` `LevelHudIsTargetAndMoves`, `PortraitBreathesAndReacts` and `ScoreLevelsShowTheirPassMark` still compile and still pass: they `Build` the strip into a test canvas and call `Refresh()` directly. Tasks 4.6 and 4.7 retarget them. `JellyGarden/Story/Bake Level HUD into the scene` (`StoryHudBaker`) must not be run after the port, because it would re-bake the in-level strip into `game.unity`. Retire it with the strip in Task 4.6.

### 2.5 `KingLeoVfxHost.cs`: retire (no meta users; §3)

- Its only caller is main's `LevelManager.cs:496` `KingLeoVfxHost.Configure()`, which §4 drops.
- Its body calls the retired `StoryRoyalVfx.Prewarm()` (line 28).
- Nothing outside the level references it, and royal-throne's code does not use the `Match3Vfx` package at all. The package is used only by its own samples and tests.

### 2.6 `StoryVfxFlags.cs`: retire (no remaining callers; §3)

The file declares no flag constants. Each flag is a string passed to `StoryVfxFlags.On(flag, pref)` by a retired caller:

| Retired caller | Flag | Pref |
|---|---|---|
| `StoryOriginalBoosters:52` | `-originalboosters` | `StoryVfx.OriginalBoosters` |
| `StoryOriginalTnt:53` | `-originaltnt` | |
| `StoryOriginalColorBall:58` | `-originalcolorball` | |
| `StoryOriginalMotion:57` | `-originalmotion` | |
| `StoryRocketVfx:75` | `-storyrocket` | |
| `StoryBombVfx:58` | `-storybomb` | |
| `StoryColorBombVfx:55` | `-storycolorbomb` | |
| `StoryBlastVfx:50` | `-storyblastvfx` | |
| `StoryShatterVfx:73` | `-storyshatter` | |
| `StoryPopVfx:77` | `-storypop` | |
| `StoryFinishVfx:64` | `-storyfinish` | |

- No kept file calls `On` or `Set`.
- No kept file or tool reads a `StoryVfx.*` PlayerPrefs key.
- No tool passes these flags.

"Retired flags removed" therefore removes everything. If the team prefers to keep the file as a utility, it compiles unchanged with zero callers; only the comment at lines 84–101 is stale.

### 2.7 `StoryBoot.cs`: no change

It references only `StoryWindow`, `StoryDatabase`, `StoryProgress`, `StoryLevelPlan`, `CutsceneRunner`, `StoryTheme`, `StoryMapProgressManager` and `StoryFreeGame`, all of which are kept. The plan's "expected hit" in `StoryBoot` does not exist.

---

## 3. Transitive breakage: kept classes

| Kept class | Uses a retired type? | Keeps compiling by | Must retire? |
|---|---|---|---|
| `StoryOriginalWinStars` | `StoryOriginalBoosters.Ready` (38) | the §1.2 fix | No. Plan §5 keeps it. It still needs `Assets/ReferenceGameplay/Preview/ReferenceBoosters.cs` (asmdef `RoyalThrone.ReferenceTntPreview`, `using static …ReferenceBoosters` at line 12, for `StarHitVfx` and `StarIdleVfx`). That folder is main-only; royal-throne has no `Preview/`, and its `ReferenceGameplay` namespaces do not collide. **Do not delete `Assets/ReferenceGameplay/Preview/`** when importing royal-throne's `ReferenceGameplay`, or else inline the two constants `"StarHitParticles"` and `"StarIdleParticles"`. |
| `StoryWinPopup` | indirectly, through `StoryOriginalWinStars.Attach` (116, 188) | nothing to change | No |
| `StoryPlaytest` | no (comment at line 3 only). Uses `LevelManager.DragBlocked` (428, 680) and `StoryDeadBoard.Reshuffles` (442); both exist on royal-throne's `LevelManager` / main's `StoryDeadBoard`. | unchanged | No. Task 5.1 adapts it. |
| `StoryThemeTest` | no, but reads `StoryBoardTheme` statics | keeping the statics (§2.2) | No. It only runs with `-storythemetest` or its pref. At runtime its backdrop and cell-fill findings will fire once `StoryBoardTheme` stops installing; Task 5.1 Step 2 drops them. |
| `StoryShellTest` | `StoryPowerupTest.IsFinished` (431) | the §1.2 fix | No |
| `StoryLevelPopup` | no; uses `StoryLevelHud.ReadLevel/HudModel/HudTarget` | §2.4 keep-list | No |
| `StoryPrePlay`, `StoryBoosters` | no; use `StorybookBoardArt.Enabled/Piece/Booster` | §2.3 keep-list | No |
| `StoryDeadBoard` | no | unchanged | Task 4.5 decides |
| `TreasureHuntReview` | no retired type, Soft member or Soft object name | unchanged | No. Its runtime behaviour against the royal-throne board is UNVERIFIED. |
| `StoryRoyalChrome` | no, but its **only** user was `StoryLevelHud.Soft` (lines 35, 44, 46, 48, 52, 62, 65) | unchanged (procedural sprites, no assets) | Optional: it becomes dead code. |
| `StorySelfTests` | no (comment) | unchanged | No. `StoryShellTest` still reads `Requested`. |
| **`StorybookBoardReview`** | `StorybookBoardLayout` (115) | — | **Yes, retire.** It is a command-line-only review (`-storybook-board-review=`; nothing in `tools/` passes it) of exactly the retired storybook board layer. It asserts that gem sprites equal `StorybookBoardArt.Piece`, which no longer holds once `Apply` goes. It reads `StoryLevelHud.Instance.VisibleTargetCount`, which stays 0 with the HUD gated, and clicks the Soft HUD buttons `Booster0..3`, which no longer exist: line 87 throws a `NullReferenceException` and line 91's `First(...)` throws. It also checks that the board fits `StorybookBoardLayout.FieldRect`, which is retired. No other file references it. |
| **`Story/Editor/StoryBoardBorderTests`** | `StoryBoardScale.Pitch/Factor` (61, 62, 74) | (alternative: inline `Pitch = 1.17f`, `Factor = 1.17f / 0.82f`) | **Yes, retire (recommended).** It is a menu-only test (`JellyGarden/Story/Run Board Border Tests`, not among the Task 0.2 suites) of `StoryBoardTheme.ApplyBoardOutline`, the gold edge that D6 retires from the level. It calls `FillSprite` and `ApplyBoardOutline` by reflection, which would fail at runtime after the §2.2 full reduction. Remove its mention in `Story/CLAUDE.md` (Task 6.2). |
| **`KingLeoVfxHost`** | `StoryRoyalVfx` (28) | — | **Yes, retire.** See §2.5. |
| **`StoryVfxFlags`** | no, but every caller is retired | — | **Yes, retire (recommended).** See §2.6. |
| `Story/Editor/StoryScreenTests` | `StoryBoardCentre` (881–895) | the §1.2 fix (−1 case) | No |
| `Story/Editor/StorybookBoardTests` | `StorybookBoardLayout` (47–49) | the §1.2 fix | No; see §8.2 |
| `Story/Editor/StoryHudBaker` | no; uses kept `StoryLevelHud` members | unchanged | Retire with the strip in Task 4.6 (not needed to compile) |
| `Story/Editor/StorybookTests`, `StoryWinPopupTests`, `StorySmokeTest`, `TreasureHuntTests`, `StoryProgressTests`, `StoryScreenPreview`, `StoryPlaytestRunner` | no | unchanged | No |
| Kit and GUI files that are not replaced (`Targets/TargetLevel.cs:51`, `Targets/TargetIcon.cs:48`) | only `StorybookBoardArt.Goal` (kept) | unchanged | No |

**Kit-core members that main added and royal-throne lacks.** I compared declarations in main's six kit-core files against royal-throne's. The only main-only members are:
- in `LevelManager`: `IngredientExitRow` (re-applied by §4) and `matchesGot`;
- in `Item`: `BeginDragFromCurrentTouch`, `CanLandFire`, `CanTapActivate`, `DestroyByRocket`, `DragThresholdPixels`, `IsUniversalPowerup`, `LandActivate`, `LandingPowerup`, `PopClaimForKit`, `PopRelease`, `PopScale`, `TapActivate`, `hasPressScreenPos` and `pressScreenPos`.

**No kept file uses any of them.** Members that kept Story code reads (`DragBlocked`, `InGameBoosts`, `OnLevelLoaded`, `OnStartPlay`, `activatedBoost`, `targetObject`, `star1`, `Limit`, `limitType`, `GameField`) all exist on royal-throne's `LevelManager`.

**Final retired list for `git rm` (41 `.cs` + 41 `.meta`):** the 37 files of §1.1, plus `KingLeoVfxHost.cs`, `StoryVfxFlags.cs`, `StorybookBoardReview.cs` and `Editor/StoryBoardBorderTests.cs`. Optional: `StoryRoyalChrome.cs`.

---

## 4. Assets and resources

The asset inventory is in `docs/port/UNUSED-ASSETS.md`. In short:
- Two whole packages become unreferenced by game code: `Assets/RoyalRocketVFX` (40 assets, 2.3 MB) and `Assets/RoyalTNTVFX` (26 assets, 0.4 MB).
- The embedded `Packages/com.royalthrone.match3-vfx` (2.9 MB) also becomes unreferenced.
- One sprite becomes unused: `Story/RoyalBoard/courtyard.png` (2.0 MB).
- Every `ReferenceGameplay/*` resource that the `StoryOriginal*` layer loads stays in use. Main's kept `Preview` code and tests load the same keys, and royal-throne's imported code loads them at the same paths.

## 5. The no-deletion rule

- **No retirement requires deleting a sprite, prefab, animation, material or shader.** The retired scripts create their VFX procedurally (`Texture2D`, `Sprite.Create`, built-in shaders found by `Shader.Find`) or load assets by `Resources` key. No asset references a retired script's GUID, so deleting the scripts leaves no "Missing Script" component behind.
- `Assets/JellyGarden/Scenes/game.unity` keeps the baked `StoryLevelHud` component (§2.4), which is not retired.
- `tools/check_vfx_intact.py` is unaffected by these deletions, because `.cs` is not in `VISUAL_EXTENSIONS` and no `.prefab` or `.unity` changes.
- `tools/port_retire_check.py` also fails on any visual asset deleted or renamed since `--base`.

## 6. Cross-check against plan Appendix A/B

These are the hooks main's kit-core files have on retired types, taken from non-comment code at `HEAD`. They disappear when royal-throne's files are restored. Notes on the appendix:

- **Item.cs**:
  - The appendix is complete except for **`StoryBoardScale.Pitch` at `Item.cs:1271`**, which Appendix A does not list.
  - `StoryOriginalTnt.WaveDelay` (Appendix A) appears only in a comment (`Item.cs:1522`).
  - `StorybookBoardArt.Apply` is at `Item.cs:132`.
- **LevelManager.cs**:
  - `StoryRoyalVfx.StateChanged` 285; `KingLeoVfxHost.Configure` 496; `StoryBoardTheme.DrawKitOutline` 513.
  - `StoryOriginalBoosters.SendMove` 755 / `.MovesInFlight` 763.
  - `StoryBoardScale.Apply` 974, `.Factor` 1029, `.FitToCell` ×7 (1048, 1060, 1068, 1297, 1308, 1320, 1331).
  - `StoryOriginalMotion.BeginRefill` 1347, `.Enter` 1361 (one call; Appendix B says ×2), `.MarkMatch` 1758.
  - `StoryRocketSweep.HasPendingHits` 2045, `StoryOriginalTnt.Anticipating` 2047, `StoryOriginalColorBall.Busy` 2049.
  - `StoryBoardScale.ItemScale` is listed under LevelManager in Appendix B, but it is used only at `Square.cs:102`.
  - `StoryPrePlay.Enabled` appears only in a comment (590); the call is `StoryPrePlay.Open()` at 592.
- **Square.cs**: `StorybookBoardArt.Enabled` 70, `StoryBoardScale.ItemScale` 102.

## 7. How to check (Task 3.7 Step 2, Task 3.8 Step 1)

```
python3 tools/port_retire_check.py                      # scan the working tree
python3 tools/port_retire_check.py --strict             # also fail while a retired file exists
python3 tools/port_retire_check.py --typecheck --source HEAD --simulate-delete
python3 tools/port_retire_check.py --typecheck --source worktree
```

- **Scan:** comments and strings are blanked before matching. It fails on references from kept files, on any scene or prefab that serialises a retired GUID, and on deleted or renamed visual assets. Kit-core hits are reported as INFO.
  - At `b47ca0fa`: 30 FAIL lines (exactly the CS rows in §1.2 for kept files), 41 TODO (retired files still present), 79 INFO (kit-core).
- **Typecheck:** copies the sources from a git ref or the working tree into a temp directory, builds them as one assembly against the references in `Assembly-CSharp-Editor.csproj`, and lists errors per file. It writes nothing into the project.
  - Results at `b47ca0fa` are in §0.
- `--plan-only` restricts both modes to the plan's 37 files.

## 8. Notes for the implementer

1. **Concurrent working-tree changes (seen during this audit).** `git status` on `port/royal-throne-level` showed 2,565 untracked royal-throne files under `Assets/`, plus a modified `ProjectSettings/TimeManager.asset`. They include `Assets/Boosters/**`, `Juice/**`, `GUI/Royal*` and `Resources/RoyalGameplay/EffectsProfile.asset`. They appeared while this audit was running, and this audit did not create them. EffectsProfile.asset is the master switch that Task 3.6 Step 3 says to hold back.

By the end of the audit, 16 tracked files had also changed in the working tree, among them `Item.cs`, `LevelManager.cs`, `Square.cs` and `Targets/TargetsGroup.cs`. On that tree the scan still reports the same 30 kept-file failures, but 0 kit-core INFO hits. The imported `Editor/Royal*Checks.cs` suites also trip `tools/check_launch_args.py` (30 "captures directly; call StoryWindow.Capture"). That matters for Task 4.0.

All measurements above come from tracked content at `b47ca0fa` (`git` objects), not from those files.
2. **Plan conflict, Task 5.1 Step 3.** The plan says to keep `StorybookBoardTests`' layout checks across all 1,208 levels, but those checks are `StorybookBoardLayout.FieldRect/FitPitch/Allowance`, which §5 retires. Task 3.7 has to remove the loop for Phase 3 to compile. Task 5.1 can re-add a per-level fit check against royal-throne's `RoyalGameplayPresentation.PlayableViewport`.
3. **Baseline deltas to record in `docs/port/BASELINE.md` (Task 3.8 Step 4):**
   - `StoryScreenTests.RunHeadless` loses `BoardsAreCentredWhateverTheirWidth`.
   - The menu suites `Validate Original Boosters`, `Validate Original TNT`, `Validate Rocket Animation` and `Run Board Border Tests` disappear.
   - The command-line reviews `-storybook-board-review=`, `-storyvfxlab` and `-storyexplosionprobe` disappear.
4. **`StoryLevelHud` gate** (§2.4). The gate depends on `JellyGarden.Scripts.Juice.RoyalEffectsProfile`, which Task 3.5 imports in the same compile unit. If Task 3.7 is typechecked before 3.5, temporarily use `Enabled = false` and accept a frozen HUD.
