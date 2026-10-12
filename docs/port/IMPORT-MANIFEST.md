# Royal Throne level port: import manifest

Plan Task 1.1 (`docs/superpowers/plans/2026-10-12-royal-throne-level-port.md` §5), decisions D1–D8 at their DEFAULT. Written 2026-10-12.

| Ref | Commit |
|---|---|
| `origin/royal-throne` (source) | `1164c261bcad6527151cc292d2701c3025c9802a` |
| `origin/main` (compared against) | `c38b22468e759a0ad04c87ab08205e4a75a62bc6` |
| kit ancestor | `a82c4df33a929614fd4d7095b59aa26cb0274a8b` |

**Machine-readable outputs.**
- `docs/port/import-paths.txt` lists the paths to restore verbatim from `origin/royal-throne`. Each path implies its `.meta`.
- `docs/port/import-closure.json` gives evidence for every path: its group, size, status on main, liveness, and the edge that pulled it in. It also lists the cut edges, narrowed loads and the ancestor folder `.meta` files.

**Regenerate** (read-only on git; no working tree, no Unity):

```
python3 tools/port_import_manifest.py --out docs/port --tables /tmp/tables.md
python3 tools/port_code_closure.py [--why <path>]       # C# type closure + cut references
python3 tools/port_asset_closure.py [--why <path>] [--runtime-only] [--seed <path>]
```

## 1. Direct answers

### Is `Assets/Spine` needed? Yes, for the Cannon tool and nothing else in scope.
- `Assets/ReferenceGameplay/Tools/Scripts/ReferenceCannonSequence.cs` uses Spine types at compile time: `SkeletonAnimation`, `SkeletonDataAsset`, `Bone`, `Slot` and `RegionAttachment`. It also loads these resources:
  - `ReferenceGameplay/Cannon/CannonSkeleton` (a `SkeletonDataAsset`, line 38/105);
  - `Assets/ReferenceGameplay/Tools/Cannon/CannonAtlas.asset` (a `SpineAtlasAsset`).
- `ReferenceCannonSequence.Available` (line 38) only checks whether the assets are present. Without Spine the file does not compile.
- No other file in the closure uses Spine. The other Spine users on royal-throne are cut: `GUI/RoyalHomeScreen.cs` and `Assets/Characters/KingVenguard/**` (D2).
- **Import the whole `Assets/Spine/`.** It holds 542 blobs including `.meta`, 2,069,540 bytes.
  - It has two asmdefs: `Runtime/spine-unity.asmdef` and `Editor/spine-unity-editor.asmdef`.
  - It is spine-unity 3.8 at commit `8b4844bd`, patched for Unity 6000.5 as described in `Assets/Spine/UNITY6-COMPAT-PATCHES.md`.
  - Main has no `Assets/Spine`.
- **The only way to avoid Spine** is to drop the Cannon tool, or to reimplement its animation without Spine.

### Which `Assets/Plugins/AllIn1VfxToolkit` files does royal-throne need that main lacks? 31 files (2,739,466 bytes)
- **Why they are needed.** `Juice/RoyalToolkitImpact.cs:36` loads `Resources/RoyalGameplay/Toolkit/Library.asset`, and so do `RoyalMatchBreakService.cs:53` and `RoyalToolkitChecks.cs:65`. That library references `Toolkit/Fire Impact.prefab` and `Toolkit/Ice Impact.prefab`. Both are **prefab variants of the AllIn1 demo prefabs**: their source guid is `240c830bfe588ad4f9925288b65d7a18`, which is `Demo & Assets/Demo/Prefabs/Fire Impact.prefab`. Those prefabs and the Toolkit materials pull in the following files.
- **The files** (all under `Assets/Plugins/AllIn1VfxToolkit/Demo & Assets/`):
  - `Demo/Materials/`: FireSparks2, IceFlares, ImpactCenterGlow, ImpactFlareFire, ImpactFlareIce, ImpactGlow, ImpactMark, ImpactRing, ImpactRing2, RedFaintGlow, SnowFlakes (`.mat` ×11).
  - `Demo/Prefabs/Fire Impact.prefab`, `Demo/Prefabs/Ice Impact.prefab`.
  - `Demo/Scripts/AllIn1VfxAutoDestroy.cs`, `Demo/Scripts/AllIn1VfxFadeLight.cs`. Both prefabs use these through `m_Script`.
  - `Meshes/SphereVfx.mesh`.
  - `Textures/`:
    - `Distortion Normal Maps/DistortionRing6.png`, `Greyscale Gradients/Gradient2.png`;
    - `Noise/{Noise7,Noise9,PerlinNoise}.png`;
    - `Shapes/{Circle17,FireEmbers,Flare1,Glow,Glow2,Glow4,Impact13,Impact2,Ring3,SnowFlake}.png`.
- **Already on main, but different.** `Shaders/AllIn1Vfx.shader`, `Shaders/AllIn1VfxBuiltIn.shader` and `Shaders/AllIn1VfxFunctions.cginc` differ **only in line endings**: `git diff -w --ignore-cr-at-eol origin/main origin/royal-throne` is empty for all three. Keep main's.
- **Folder `.meta` conflict.** `Assets/Plugins.meta` has a different GUID on each side: main has `69576669…`, royal-throne has `6c2a0838…`. Keep main's; this goes to the GUID audit.

### Is `Resources/RoyalTNT` needed, or fallback only? Fallback only. It is not imported.
The runtime draws the imported APK TNT view and particles whenever they are present. RoyalTNT is used only when they are missing:
- `Juice/TNT3DAnimator.cs:51-54` says: "While the imported TNT is live … the 240 frames (~28 MB on phones) and the 32 actors would sit unused". The code reads `if(!ReferencePieceVisual.Has(ItemsTypes.PACKAGE)) TNTFrameAnimator.Prewarm(); if(!ReferenceVfxPool.Has("TntItemExplodeParticles")) BuildPool();`.
- `Juice/RoyalPowerupDirector.cs:256-257` reads `native = ReferenceVfxPool.Has("TntItemExplodeParticles"); visual = native ? null : TNT3DAnimator.ActivateAt(origin)`.
- `Juice/RoyalPieceSkin.cs:160-163` calls `TNTFrameAnimator.Attach` only when `ReferencePieceVisual.Attach(item)` fails. `Item.cs:1185-1186` does the same for `RevealSprite`.

RoyalTNT is still needed in three non-runtime places:
- The workshop/QA preview `RoyalPowerupDirector.Preview("TNT")`, at `RoyalPowerupDirector.cs:379`. It is called from `RoyalQaService.cs:242` and `RoyalEffectsChecks.cs:258`.
- `Editor/RoyalTNTChecks.cs`, which `RoyalEffectsChecks.cs:106` calls, so it is a compile dependency of the Task 4.0 suites.

Those checks will fail or skip without `Resources/RoyalTNT/**`: 250 files, 26,879,431 bytes. The shaders `Shaders/RoyalTNTBody.shader` and `Shaders/RoyalTNTGlow.shader` are referenced only by `Resources/RoyalTNT/*.mat`, so they are not needed either. This differs from plan §5 and Task 3.6, which list both shaders.

### Where do the ReferenceGameplay Resources live?
They are not in one folder. There are **13 separate `Resources/` roots** on royal-throne, and every key starts with `ReferenceGameplay/`. Two of them are excluded because they are out of scope: `Home/Resources` and `Levels/Resources`.

| Resources root on royal-throne | Keys | Used by the port |
|---|---|---|
| `Assets/ReferenceGameplay/Resources/` | `ReferenceGameplay/runtime-catalog.json` | yes (`ReferenceGameplayCatalog.cs:30`) |
| `Assets/ReferenceGameplay/Art/Resources/` | `ReferenceGameplay/Sprites/*` (1,243), `ReferenceGameplay/Blockers/*` | 177 sprites (catalog entries + literals + guid refs); `Blockers/` cut (D8) |
| `Assets/ReferenceGameplay/Animation/Resources/` | `ReferenceGameplay/Visuals/*` (8 prefabs) | 6 (Rocket, TNT, Propeller, ColorBall, `*Rfv387` combos); `ColorBallColorBallCombo`/`TNTTNTCombo` unused |
| `Assets/ReferenceGameplay/VFX/Resources/` | `ReferenceGameplay/VFX/*` (42 prefabs) | 27 |
| `Assets/ReferenceGameplay/Audio/Resources/` | `ReferenceGameplay/Audio/*` (50 wav) | 26 |
| `Assets/ReferenceGameplay/Layout/Resources/` | `ReferenceGameplay/Layout/*` (12) | 2 (`Game-settings_button_gear`, `Game-goal_panel_banner`) |
| `Assets/ReferenceGameplay/Tools/Resources/` | `ReferenceGameplay/Tools/*` (14 tool icons) | all |
| `Assets/ReferenceGameplay/Tools/Hammer/Resources/` | `ReferenceGameplay/Hammer/*` | all |
| `Assets/ReferenceGameplay/Tools/Archer/Resources/` | `ReferenceGameplay/Archer/*` | all |
| `Assets/ReferenceGameplay/Tools/Cannon/Resources/` | `ReferenceGameplay/Cannon/*` | all |
| `Assets/ReferenceGameplay/WinVFX/Resources/` | `ReferenceGameplay/VFX/*` (2) | no (win card only) |
| `Assets/ReferenceGameplay/Home/Resources/` | `ReferenceGameplay/Home/*` | no (home) |
| `Assets/ReferenceGameplay/Levels/Resources/` | `ReferenceGameplay/levels01-50` | no (D8) |

The plan's `Assets/ReferenceGameplay/Resources/ReferenceGameplay/{Visuals,VFX,Audio,Sprites,Tools,Layout}` does not exist as written. Only `runtime-catalog.json` is under `Assets/ReferenceGameplay/Resources/`.

Main already has 343 of the 482 needed ReferenceGameplay asset files, byte-identical: all of Animation and Audio, most of Art and VFX. Those are omitted from `import-paths.txt`.

### Where do the booster prefabs live? Under `Assets/Boosters/*/V*/Prefabs/Resources/`. They arrive with the whole-folder imports.
- Propeller: `Assets/Boosters/Propeller/V5/Prefabs/Resources/Propeller-V5.prefab` and `Propeller-V5-VFX.prefab`. They are loaded at `PropellerVisualV5.cs:62` and `PropellerVfxV5.cs:18`.
- Color Ball V4: `Assets/Boosters/ColorBall/V4/Prefabs/Resources/ColorBall-V4.prefab` and `ColorBall-V4-VFX.prefab`.
- Color Ball V5: `Assets/Boosters/ColorBall/V5/Prefabs/Resources/ColorBall-V5.prefab` and `ColorBall-V5-VFX.prefab`.
- Both Color Ball versions are loaded at `ColorBallVisualV4.cs:30` and `ColorBallVfxV4.cs:40`. V5 is preferred when present (`Assets/Boosters/ColorBall/V5/README.md`).
- Other booster resources:
  - `Assets/Boosters/ColorBall/V5/Assets/Resources/ColorBall-{Enhanced,ReferenceMatch}.asset`;
  - `Audio/Resources/PropellerV5/*.wav` (`PropellerAudioV5.cs:26`);
  - `Audio/Resources/ColorBallV4/*.wav` (`ColorBallAudioV4.cs:17`).
- The three folders are self-contained. The closure covers all of their runtime files. Only docs, sources, `.gitignore` and two unreferenced paddle meshes in `Propeller/V5/Assets/` are outside it, so each folder is imported whole as a `dir/` line.

## 2. How the closure was computed

**Code closure** (`tools/port_code_closure.py`).
- **Method.** Comments and strings are blanked. Top-level types are indexed per namespace. An identifier counts as a reference when its namespace is visible: own or parent namespace, `using`, `using static`, an alias such as `using PlayerPrefs = RoyalThrone.Testing.RoyalSave`, or a qualified `Ns.Type`.
- **Seeds:**
  - the kit core: `LevelManager`, `Item`, `Square`, `AI`, `InputManager`, `System/CombineManager`, `GUI/BoostShop`, `GUI/BoostIcon`, `Targets/*`;
  - `Juice/*`;
  - `Boosters/Propeller/V5`, `ColorBall/V4`, `ColorBall/V5`;
  - `ReferenceGameplay/Scripts/*`, `ReferenceGameplay/Tools/Scripts/*`, `ReferenceWinFinish.cs`;
  - `RoyalSave`, `RoyalQaService`;
  - the two import postprocessors that shape the imported art: `ReferenceGameplay/Editor/ReferenceSpriteImporter.cs` and `Scripts/Editor/RoyalGameplayArtImporter.cs`.
- **Rules:**
  - A file in an excluded area is reported as a cut and not followed.
  - A kit file whose royal-throne version equals the kit is not followed.
  - `InitScript`, `GUI/AnimationManager` and `GUI/MenuManager` keep main's version (plan §4). Their royal-throne-only references are listed in §5 and not followed.
  - `partial class RoyalPowerupDirector` parts that live in the excluded `Boosters/*/V2|V3|V4` folders are left out. Nothing in the closure calls their members, except the sites listed in §5.

**Asset closure** (`tools/port_asset_closure.py`) follows four kinds of reference:
- `Resources.Load`, `LoadAll` and `LoadAsync` arguments, folded through `const` and `static readonly` strings and traced back through locals. A dynamic tail becomes a prefix. That prefix is narrowed to the keys that some code literal completes; the drops are recorded in `import-closure.json` under `narrowed_loads`.
- Catalog JSON entries whose `key` is named by code.
- Every `guid:` in YAML assets and in the `.meta` of every included file. This is how `RoyalWebPImporter.cs` was found: every `.webp.meta` names it as its ScriptedImporter.
- `Shader.Find` names, plus shader `UsePass`, `Fallback` and `#include`.

It is run twice, with and without the editor tests, to label assets that only tests need.

Limits:
- **Reflection is not followed.** Only `UrpBloomSetup.cs` uses `Type.GetType`, for URP types, and that file is dead.
- **Scenes are not followed.** Plan Fact 7 says no in-level object is serialized in `game.unity`.
- **Narrowing assumes keys are code literals.** No animation clip in scope carries event strings; the scan of `.anim` files found none. Every narrowed site was reviewed by hand:
  - The Layout keys are passed as literals into `RoyalGameplayPresentation.Source/Pair`.
  - The VFX keys and audio names are literals in the callers.
  - The catalog keys dropped (`propeller-*`, `glow`, `beam`, `cell-front-effect`, `box-2`, `box-3`) are used only by the cut `GUI/RoyalLessonDemo.cs`.

## 3. Summary by group

| Group | Files | Bytes (incl. .meta) | Bytes to import |
|---|---:|---:|---:|
| code | 203 | 2,855,722 | 2,855,722 |
| editor-tests | 30 | 379,059 | 379,059 |
| third-party | 280 | 4,955,000 | 4,850,679 |
| assets | 597 | 52,775,177 | 30,617,801 |
| kit-merge | 11 | 288,305 | 0 |
| kit-manual | 5 | 83,764 | 0 |
| kit-keep-main | 12 | 43,786 | 0 |
| kit-unchanged | 13 | 36,829 | 0 |

### code

| Path | Bytes | Live | Main | Note |
|---|---:|---|---|---|
| `Assets/JellyGarden/Scripts/Editor/RoyalGameplayArtImporter.cs` | 4,811 | editor | absent |  |
| `Assets/JellyGarden/Scripts/Editor/RoyalWebPImporter.cs` | 5,536 | live | absent | script of a closure asset (ScriptedImporter for .webp; needs FFmpeg) |
| `Assets/JellyGarden/Scripts/GUI/RoyalArchShape.cs` | 7,097 | live | absent |  |
| `Assets/JellyGarden/Scripts/GUI/RoyalBoardSurface.cs` | 12,722 | live | absent |  |
| `Assets/JellyGarden/Scripts/GUI/RoyalCardShape.cs` | 5,980 | live | absent |  |
| `Assets/JellyGarden/Scripts/GUI/RoyalGameplayHud.cs` | 24,725 | live | absent |  |
| `Assets/JellyGarden/Scripts/GUI/RoyalGameplayPresentation.cs` | 45,086 | live | absent |  |
| `Assets/JellyGarden/Scripts/GUI/RoyalPanelGraphic.cs` | 2,984 | live | absent |  |
| `Assets/JellyGarden/Scripts/GUI/RoyalPressTracker.cs` | 1,159 | live | absent |  |
| `Assets/JellyGarden/Scripts/GUI/RoyalTextStyle.cs` | 16,616 | live | absent |  |
| `Assets/JellyGarden/Scripts/GUI/RoyalVisualCatalog.cs` | 2,943 | live | absent |  |
| `Assets/JellyGarden/Scripts/Juice/CandyShatterService.cs` | 28,828 | live | absent |  |
| `Assets/JellyGarden/Scripts/Juice/ColorBallBoostView.cs` | 9,587 | dead | absent |  |
| `Assets/JellyGarden/Scripts/Juice/ColorBallFrameAnimator.cs` | 14,634 | dead | absent |  |
| `Assets/JellyGarden/Scripts/Juice/ColorBallPresentation.cs` | 1,028 | live | absent |  |
| `Assets/JellyGarden/Scripts/Juice/ColorBallTargetVfx.cs` | 13,901 | live | absent |  |
| `Assets/JellyGarden/Scripts/Juice/Easing.cs` | 2,564 | live | absent |  |
| `Assets/JellyGarden/Scripts/Juice/JuiceConfig.cs` | 12,785 | live | absent |  |
| `Assets/JellyGarden/Scripts/Juice/MatchVFXService.cs` | 34,426 | live | absent |  |
| `Assets/JellyGarden/Scripts/Juice/RoyalAudioDirector.cs` | 12,009 | live | absent |  |
| `Assets/JellyGarden/Scripts/Juice/RoyalAudioSettings.cs` | 1,424 | live | absent |  |
| `Assets/JellyGarden/Scripts/Juice/RoyalBlockSkin.cs` | 363 | live | absent |  |
| `Assets/JellyGarden/Scripts/Juice/RoyalBoardIntegrity.cs` | 7,004 | live | absent |  |
| `Assets/JellyGarden/Scripts/Juice/RoyalBurstLibrary.cs` | 373 | live | absent |  |
| `Assets/JellyGarden/Scripts/Juice/RoyalCelebration.cs` | 9,577 | live | absent |  |
| `Assets/JellyGarden/Scripts/Juice/RoyalColorBallSequence.cs` | 4,954 | dead | absent |  |
| `Assets/JellyGarden/Scripts/Juice/RoyalContactShadow.cs` | 2,823 | live | absent |  |
| `Assets/JellyGarden/Scripts/Juice/RoyalDualColorBallSequence.cs` | 1,842 | live | absent |  |
| `Assets/JellyGarden/Scripts/Juice/RoyalEffectsProfile.cs` | 2,116 | live | absent |  |
| `Assets/JellyGarden/Scripts/Juice/RoyalGameplayArt.cs` | 11,452 | live | absent |  |
| `Assets/JellyGarden/Scripts/Juice/RoyalGameplayWarmup.cs` | 5,227 | live | absent |  |
| `Assets/JellyGarden/Scripts/Juice/RoyalHintContour.cs` | 2,608 | live | absent |  |
| `Assets/JellyGarden/Scripts/Juice/RoyalHintPresenter.cs` | 28,994 | live | absent |  |
| `Assets/JellyGarden/Scripts/Juice/RoyalHintSettings.cs` | 779 | live | absent |  |
| `Assets/JellyGarden/Scripts/Juice/RoyalHintSolver.cs` | 11,433 | live | absent |  |
| `Assets/JellyGarden/Scripts/Juice/RoyalIceBreak.cs` | 15,135 | live | absent |  |
| `Assets/JellyGarden/Scripts/Juice/RoyalMatchBreakService.cs` | 16,055 | dead | absent |  |
| `Assets/JellyGarden/Scripts/Juice/RoyalPatterns.cs` | 3,067 | live | absent |  |
| `Assets/JellyGarden/Scripts/Juice/RoyalPieceSkin.cs` | 11,565 | live | absent |  |
| `Assets/JellyGarden/Scripts/Juice/RoyalPowerupDirector.cs` | 30,449 | live | absent |  |
| `Assets/JellyGarden/Scripts/Juice/RoyalPropellerPieceV2.cs` | 5,108 | dead | absent |  |
| `Assets/JellyGarden/Scripts/Juice/RoyalPropellerV2.cs` | 17,149 | dead | absent |  |
| `Assets/JellyGarden/Scripts/Juice/RoyalPropellerV2Sequence.cs` | 6,428 | dead | absent |  |
| `Assets/JellyGarden/Scripts/Juice/RoyalRegressionRunner.cs` | 1,854 | qa-only | absent |  |
| `Assets/JellyGarden/Scripts/Juice/RoyalRocketLaunch.cs` | 5,575 | live | absent |  |
| `Assets/JellyGarden/Scripts/Juice/RoyalRocketPieceAnimator.cs` | 16,218 | live | absent |  |
| `Assets/JellyGarden/Scripts/Juice/RoyalRocketSequence.cs` | 29,771 | live | absent |  |
| `Assets/JellyGarden/Scripts/Juice/RoyalRocketTntCombo.cs` | 1,700 | live | absent |  |
| `Assets/JellyGarden/Scripts/Juice/RoyalShuffle.cs` | 6,568 | live | absent |  |
| `Assets/JellyGarden/Scripts/Juice/RoyalSpecialCreationFlash.cs` | 4,112 | live | absent |  |
| `Assets/JellyGarden/Scripts/Juice/RoyalSpecialFormation.cs` | 4,403 | live | absent |  |
| `Assets/JellyGarden/Scripts/Juice/RoyalTntNativeSequence.cs` | 2,229 | live | absent |  |
| `Assets/JellyGarden/Scripts/Juice/RoyalToolkitImpact.cs` | 6,126 | live | absent |  |
| `Assets/JellyGarden/Scripts/Juice/RoyalToolkitLibrary.cs` | 426 | live | absent |  |
| `Assets/JellyGarden/Scripts/Juice/SparkleTextureFactory.cs` | 6,804 | live | absent |  |
| `Assets/JellyGarden/Scripts/Juice/TNT3DAnimator.cs` | 13,693 | fallback | absent |  |
| `Assets/JellyGarden/Scripts/Juice/TNTFrameAnimator.cs` | 13,100 | fallback | absent |  |
| `Assets/JellyGarden/Scripts/Juice/UrpBloomSetup.cs` | 6,275 | dead | absent |  |
| `Assets/JellyGarden/Scripts/RoyalOfflineMode.cs` | 393 | live | absent |  |
| `Assets/JellyGarden/Scripts/RoyalQaService.cs` | 29,985 | qa-only | absent |  |
| `Assets/JellyGarden/Scripts/RoyalSave.cs` | 3,455 | live | absent |  |
| `Assets/ReferenceGameplay/Editor/ReferenceSpriteImporter.cs` | 4,930 | editor | absent |  |
| `Assets/ReferenceGameplay/Home/Scripts/ReferenceWinFinish.cs` | 7,583 | live | absent |  |
| `Assets/ReferenceGameplay/Scripts/ReferenceActivationVisual.cs` | 8,258 | live | absent |  |
| `Assets/ReferenceGameplay/Scripts/ReferenceDualColorBallVisual.cs` | 3,588 | live | absent |  |
| `Assets/ReferenceGameplay/Scripts/ReferenceGameplayAudio.cs` | 3,148 | live | absent |  |
| `Assets/ReferenceGameplay/Scripts/ReferenceGameplayCatalog.cs` | 3,795 | live | absent |  |
| `Assets/ReferenceGameplay/Scripts/ReferencePieceVisual.cs` | 12,790 | live | absent |  |
| `Assets/ReferenceGameplay/Scripts/ReferenceVfxPool.cs` | 10,547 | live | absent |  |
| `Assets/ReferenceGameplay/Tools/Scripts/ReferenceArcherSequence.cs` | 8,572 | live | absent |  |
| `Assets/ReferenceGameplay/Tools/Scripts/ReferenceBoosterBar.cs` | 29,845 | live | absent |  |
| `Assets/ReferenceGameplay/Tools/Scripts/ReferenceCannonSequence.cs` | 9,880 | live | absent |  |
| `Assets/ReferenceGameplay/Tools/Scripts/ReferenceHammerSequence.cs` | 8,360 | live | absent |  |
| `Assets/Boosters/Propeller/V5/` (whole folder, 71 files) | 1,438,907 | docs/source 8, editor 4, live 56, unreferenced 3 | absent | scripts, prefabs, audio, docs |
| `Assets/Boosters/ColorBall/V4/` (whole folder, 34 files) | 570,142 | docs/source 5, editor 3, live 26 | absent | scripts, prefabs, audio, docs |
| `Assets/Boosters/ColorBall/V5/` (whole folder, 25 files) | 82,842 | docs/source 6, editor 2, live 17 | absent | scripts, prefabs, audio, docs |

### editor-tests

| Path | Bytes | Live | Main | Note |
|---|---:|---|---|---|
| `Assets/JellyGarden/Scripts/Editor/RoyalBlockedRefillChecks.cs` | 3,381 | test | absent | plan Task 4.0 |
| `Assets/JellyGarden/Scripts/Editor/RoyalBoardPresentationChecks.cs` | 5,950 | test | absent | plan dep |
| `Assets/JellyGarden/Scripts/Editor/RoyalBoardSurfaceChecks.cs` | 10,446 | test | absent | plan dep |
| `Assets/JellyGarden/Scripts/Editor/RoyalCellBreakChecks.cs` | 13,786 | test | absent | plan dep |
| `Assets/JellyGarden/Scripts/Editor/RoyalColorBallChecks.cs` | 8,985 | test | absent | plan dep |
| `Assets/JellyGarden/Scripts/Editor/RoyalColorBallQaChecks.cs` | 10,901 | test | absent | plan Task 4.0 |
| `Assets/JellyGarden/Scripts/Editor/RoyalEffectsChecks.cs` | 28,650 | test | absent | plan Task 4.0 |
| `Assets/JellyGarden/Scripts/Editor/RoyalFormationReferenceChecks.cs` | 7,904 | test | absent | plan Task 4.0 |
| `Assets/JellyGarden/Scripts/Editor/RoyalHintPresentationChecks.cs` | 35,792 | test | absent | plan Task 4.0 |
| `Assets/JellyGarden/Scripts/Editor/RoyalInputRegression.cs` | 13,634 | test | absent | plan Task 4.0 |
| `Assets/JellyGarden/Scripts/Editor/RoyalMatchResponseChecks.cs` | 4,552 | test | absent | plan Task 4.0 |
| `Assets/JellyGarden/Scripts/Editor/RoyalOverlapChecks.cs` | 8,843 | test | absent | plan Task 4.0 |
| `Assets/JellyGarden/Scripts/Editor/RoyalPresentationChecks.cs` | 5,625 | test | absent | optional |
| `Assets/JellyGarden/Scripts/Editor/RoyalPropellerFormationChecks.cs` | 6,295 | test | absent | plan Task 4.0 |
| `Assets/JellyGarden/Scripts/Editor/RoyalQaChecks.cs` | 15,696 | test | absent | plan Task 4.0 |
| `Assets/JellyGarden/Scripts/Editor/RoyalRocketChecks.cs` | 45,349 | test | absent | plan dep |
| `Assets/JellyGarden/Scripts/Editor/RoyalSpecialFormationChecks.cs` | 7,130 | test | absent | plan Task 4.0 |
| `Assets/JellyGarden/Scripts/Editor/RoyalSquarePlusOneChecks.cs` | 5,309 | test | absent | plan Task 4.0 |
| `Assets/JellyGarden/Scripts/Editor/RoyalSuperBombFuseChecks.cs` | 6,351 | test | absent | plan Task 4.0 |
| `Assets/JellyGarden/Scripts/Editor/RoyalTNTChecks.cs` | 17,572 | test | absent | plan dep |
| `Assets/JellyGarden/Scripts/Editor/RoyalToolkitChecks.cs` | 13,420 | test | absent | plan dep |
| `Assets/JellyGarden/Scripts/Editor/RoyalVfxLab.cs` | 20,059 | test | absent | plan dep |
| `Assets/ReferenceGameplay/Editor/ReferenceArcherChecks.cs` | 10,535 | test | absent | optional |
| `Assets/ReferenceGameplay/Editor/ReferenceBoosterChecks.cs` | 7,320 | test | absent | optional |
| `Assets/ReferenceGameplay/Editor/ReferenceCannonChecks.cs` | 13,598 | test | absent | optional |
| `Assets/ReferenceGameplay/Editor/ReferenceHammerChecks.cs` | 9,423 | test | absent | optional |
| `Assets/ReferenceGameplay/Editor/ReferenceLayoutChecks.cs` | 6,017 | test | absent | optional |
| `Assets/ReferenceGameplay/Editor/ResponsiveInputChecks.cs` | 12,966 | test | absent | optional |
| `Assets/ReferenceGameplay/Editor/RoyalAllDualComboChecks.cs` | 10,965 | test | absent | plan Task 4.0 |
| `Assets/ReferenceGameplay/Editor/RoyalDualColorBallChecks.cs` | 10,823 | test | absent | plan Task 4.0 |

### kit-merge

| Path | Bytes | Live | Main | Note |
|---|---:|---|---|---|
| `Assets/JellyGarden/Scripts/AI.cs` | 21,942 | live | different | plan §4: restore royal-throne's, cut couplings, re-apply main hooks |
| `Assets/JellyGarden/Scripts/GUI/BoostIcon.cs` | 5,252 | live | different | plan §4: restore royal-throne's, cut couplings, re-apply main hooks |
| `Assets/JellyGarden/Scripts/GUI/BoostShop.cs` | 3,653 | live | different | plan §4: restore royal-throne's, cut couplings, re-apply main hooks |
| `Assets/JellyGarden/Scripts/InputManager.cs` | 10,224 | live | different | plan §4: restore royal-throne's, cut couplings, re-apply main hooks |
| `Assets/JellyGarden/Scripts/Item.cs` | 87,438 | live | different | plan §4: restore royal-throne's, cut couplings, re-apply main hooks |
| `Assets/JellyGarden/Scripts/LevelManager.cs` | 118,475 | live | different | plan §4: restore royal-throne's, cut couplings, re-apply main hooks |
| `Assets/JellyGarden/Scripts/Square.cs` | 17,087 | live | different | plan §4: restore royal-throne's, cut couplings, re-apply main hooks |
| `Assets/JellyGarden/Scripts/System/CombineManager.cs` | 10,092 | live | different | plan §4: restore royal-throne's, cut couplings, re-apply main hooks |
| `Assets/JellyGarden/Scripts/Targets/TargetIcon.cs` | 4,107 | live | different | plan §4: restore royal-throne's, cut couplings, re-apply main hooks |
| `Assets/JellyGarden/Scripts/Targets/TargetLevel.cs` | 3,824 | live | different | plan §4: restore royal-throne's, cut couplings, re-apply main hooks |
| `Assets/JellyGarden/Scripts/Targets/TargetsGroup.cs` | 2,074 | live | different | plan §4: restore royal-throne's, cut couplings, re-apply main hooks |

### kit-manual

| Path | Bytes | Live | Main | Note |
|---|---:|---|---|---|
| `Assets/JellyGarden/Scripts/GUI/AnimationManager.cs` | 32,243 | live | different | plan §4: keep main's, add the royal-throne members the closure uses |
| `Assets/JellyGarden/Scripts/GUI/MenuManager.cs` | 8,782 | live | different | plan §4: keep main's, add the royal-throne members the closure uses |
| `Assets/JellyGarden/Scripts/InitScript.cs` | 35,175 | live | different | plan §4: keep main's, add the royal-throne members the closure uses |
| `Assets/JellyGarden/Scripts/MusicBase.cs` | 1,960 | live | different | plan §4: keep main's, add the royal-throne members the closure uses |
| `Assets/JellyGarden/Scripts/SoundBase.cs` | 3,674 | live | different | plan §4: keep main's, add the royal-throne members the closure uses |

### kit-keep-main

| Path | Bytes | Live | Main | Note |
|---|---:|---|---|---|
| `Assets/JellyGarden/Scripts/GUI/BoostAnimation.cs` | 2,129 | live | different | rt: ParticleSystem.main deprecation fix only (optional cherry-pick) |
| `Assets/JellyGarden/Scripts/GUI/LIFESAddCounter.cs` | 2,419 | live | different | rt: lives refill rewrite on RoyalSave (economy); keep main's |
| `Assets/JellyGarden/Scripts/GUI/Level.cs` | 1,753 | live | different | rt: PlayerPrefs -> RoyalSave alias only |
| `Assets/JellyGarden/Scripts/GUI/LifeShop.cs` | 2,166 | live | different | rt: RoyalEconomy hook (cut) |
| `Assets/JellyGarden/Scripts/GUI/PreFailed.cs` | 3,428 | live | different | rt: RoyalEconomy hook (cut) |
| `Assets/JellyGarden/Scripts/GUI/Tutorial.cs` | 2,624 | live | different | rt: RoyalCards / RoyalLessonDemo hooks (cut) |
| `Assets/JellyGarden/Scripts/GUI/Tutorial/HandTutorial.cs` | 2,352 | live | different | rt: null/NaN guards; main has its own edits here (+19/-3) |
| `Assets/JellyGarden/Scripts/GUI/Tutorial/TutorialManager.cs` | 5,202 | live | different | rt: RoyalReferenceCampaign hook (cut, D8) |
| `Assets/JellyGarden/Scripts/MapScripts/LevelsMap.cs` | 7,147 | live | different | rt: FindObjectsByType + teardown null guards (optional cherry-pick) |
| `Assets/JellyGarden/Scripts/MapScripts/MapCamera.cs` | 4,047 | live | different | rt: removed an unused field only |
| `Assets/JellyGarden/Scripts/MapScripts/PlayerPrefsMapProgressManager.cs` | 1,444 | live | different | rt: PlayerPrefs -> RoyalSave alias only |
| `Assets/JellyGarden/Scripts/UnityInAppsIntegration.cs` | 4,199 | live | different | rt: RoyalOfflineMode guard (offline build policy); keep main's |

### kit-unchanged

| Path | Bytes | Live | Main | Note |
|---|---:|---|---|---|
| `Assets/JellyGarden/Scripts/GUI/ProgressBarScript.cs` | 2,419 | live | identical | main already identical |
| `Assets/JellyGarden/Scripts/Integrations/UnityAdsID.cs` | 1,067 | live | identical | main already identical (script of a closure asset) |
| `Assets/JellyGarden/Scripts/ItemAnimEvents.cs` | 1,965 | live | identical | main already identical |
| `Assets/JellyGarden/Scripts/LevelDebugger.cs` | 2,423 | live | identical | main already identical |
| `Assets/JellyGarden/Scripts/MapScripts/IMapProgressManager.cs` | 949 | live | identical | main already identical |
| `Assets/JellyGarden/Scripts/MapScripts/LevelReachedEventArgs.cs` | 1,000 | live | identical | main already identical |
| `Assets/JellyGarden/Scripts/MapScripts/MapLevel.cs` | 4,156 | live | identical | main already identical |
| `Assets/JellyGarden/Scripts/MapScripts/StarsType.cs` | 818 | live | identical | main already identical |
| `Assets/JellyGarden/Scripts/MapScripts/TranslationType.cs` | 827 | live | identical | main already identical |
| `Assets/JellyGarden/Scripts/MapScripts/WaypointsMover.cs` | 4,552 | live | identical | main already identical |
| `Assets/JellyGarden/Scripts/Targets/Editor/TargetLevelEditor.cs` | 8,987 | live | identical | main already identical |
| `Assets/JellyGarden/Scripts/Targets/TargetText.cs` | 1,113 | live | identical | main already identical |
| `Assets/JellyGarden/Unity-Reorderable-List-master/List/ReorderableArray.cs` | 1,632 | live | identical | main already identical |

### third-party (by folder)

| Folder | Files | Bytes | Live | Test-only | Main identical | Main different | Main absent |
|---|---:|---:|---:|---:|---:|---:|---:|
| `Assets/Plugins/AllIn1VfxToolkit/Demo & Assets/Demo/` | 15 | 1,938,167 | 15 | 0 | 0 | 0 | 15 |
| `Assets/Plugins/AllIn1VfxToolkit/Demo & Assets/Meshes/` | 1 | 68,263 | 1 | 0 | 0 | 0 | 1 |
| `Assets/Plugins/AllIn1VfxToolkit/Demo & Assets/Textures/` | 15 | 733,036 | 15 | 0 | 0 | 0 | 15 |
| `Assets/Plugins/AllIn1VfxToolkit/Shaders/` | 3 | 101,064 | 3 | 0 | 0 | 3 | 0 |
| `Assets/Spine/` | 4 | 7,554 | 0 | 0 | 0 | 0 | 4 |
| `Assets/Spine/Editor/` | 83 | 726,386 | 1 | 0 | 0 | 0 | 83 |
| `Assets/Spine/Runtime/` | 159 | 1,236,611 | 71 | 0 | 0 | 0 | 159 |

### assets (by folder)

| Folder | Files | Bytes | Live | Test-only | Main identical | Main different | Main absent |
|---|---:|---:|---:|---:|---:|---:|---:|
| `Assets/JellyGarden/Resources/` | 7 | 16,903 | 7 | 0 | 2 | 1 | 4 |
| `Assets/JellyGarden/Resources/RoyalColorBall/` | 4 | 8,108 | 4 | 0 | 1 | 0 | 3 |
| `Assets/JellyGarden/Resources/RoyalGameplay/` | 33 | 1,198,457 | 32 | 1 | 0 | 0 | 33 |
| `Assets/JellyGarden/Resources/RoyalGameplay/Audio/` | 10 | 464,780 | 10 | 0 | 0 | 0 | 10 |
| `Assets/JellyGarden/Resources/RoyalGameplay/Toolkit/` | 15 | 275,568 | 15 | 0 | 0 | 0 | 15 |
| `Assets/JellyGarden/Resources/RoyalGameplayV4/01_match_pieces/` | 5 | 610,080 | 5 | 0 | 0 | 0 | 5 |
| `Assets/JellyGarden/Resources/RoyalGameplayV4/02_powerups/` | 9 | 1,309,345 | 9 | 0 | 0 | 0 | 9 |
| `Assets/JellyGarden/Resources/RoyalGameplayV4/03_grass/` | 3 | 595,577 | 3 | 0 | 0 | 0 | 3 |
| `Assets/JellyGarden/Resources/RoyalGameplayV4/04_board/` | 2 | 8,151 | 2 | 0 | 0 | 0 | 2 |
| `Assets/JellyGarden/Resources/RoyalGameplayV4/05_effects/` | 5 | 519,914 | 5 | 0 | 0 | 0 | 5 |
| `Assets/JellyGarden/Resources/RoyalGameplayV4/06_optional_extras/` | 9 | 1,624,361 | 9 | 0 | 0 | 0 | 9 |
| `Assets/JellyGarden/Resources/RoyalRocketFX/` | 8 | 6,413 | 8 | 0 | 0 | 0 | 8 |
| `Assets/JellyGarden/Shaders/` | 5 | 15,013 | 5 | 0 | 0 | 0 | 5 |
| `Assets/ReferenceGameplay/Animation/Clips/` | 18 | 3,275,049 | 18 | 0 | 18 | 0 | 0 |
| `Assets/ReferenceGameplay/Animation/Controllers/` | 6 | 23,403 | 6 | 0 | 6 | 0 | 0 |
| `Assets/ReferenceGameplay/Animation/Resources/ReferenceGameplay/` | 6 | 192,526 | 6 | 0 | 6 | 0 | 0 |
| `Assets/ReferenceGameplay/Art/Atlases/` | 2 | 5,281,293 | 2 | 0 | 2 | 0 | 0 |
| `Assets/ReferenceGameplay/Art/Resources/ReferenceGameplay/` | 177 | 3,414,017 | 176 | 1 | 157 | 0 | 20 |
| `Assets/ReferenceGameplay/Audio/Resources/ReferenceGameplay/` | 26 | 2,805,634 | 25 | 1 | 26 | 0 | 0 |
| `Assets/ReferenceGameplay/Layout/Resources/ReferenceGameplay/` | 2 | 15,839 | 2 | 0 | 0 | 0 | 2 |
| `Assets/ReferenceGameplay/Resources/ReferenceGameplay/` | 1 | 8,054 | 1 | 0 | 0 | 0 | 1 |
| `Assets/ReferenceGameplay/Tools/Archer/` | 1 | 2,876 | 1 | 0 | 0 | 0 | 1 |
| `Assets/ReferenceGameplay/Tools/Archer/Resources/` | 4 | 329,899 | 4 | 0 | 0 | 0 | 4 |
| `Assets/ReferenceGameplay/Tools/Archer/Sprites/` | 6 | 107,810 | 6 | 0 | 0 | 0 | 6 |
| `Assets/ReferenceGameplay/Tools/Archer/VFXTextures/` | 2 | 5,275,539 | 2 | 0 | 0 | 0 | 2 |
| `Assets/ReferenceGameplay/Tools/Cannon/` | 9 | 5,400,966 | 9 | 0 | 0 | 0 | 9 |
| `Assets/ReferenceGameplay/Tools/Cannon/Resources/` | 4 | 471,207 | 4 | 0 | 0 | 0 | 4 |
| `Assets/ReferenceGameplay/Tools/Hammer/` | 1 | 2,078 | 1 | 0 | 0 | 0 | 1 |
| `Assets/ReferenceGameplay/Tools/Hammer/Resources/` | 5 | 255,304 | 5 | 0 | 0 | 0 | 5 |
| `Assets/ReferenceGameplay/Tools/Hammer/Sprites/` | 18 | 480,972 | 18 | 0 | 0 | 0 | 18 |
| `Assets/ReferenceGameplay/Tools/Hammer/VFXTextures/` | 3 | 5,277,604 | 3 | 0 | 0 | 0 | 3 |
| `Assets/ReferenceGameplay/Tools/Resources/ReferenceGameplay/` | 14 | 197,038 | 14 | 0 | 0 | 0 | 14 |
| `Assets/ReferenceGameplay/VFX/AtlasSprites/` | 104 | 525,135 | 104 | 0 | 68 | 0 | 36 |
| `Assets/ReferenceGameplay/VFX/Materials/` | 10 | 13,252 | 10 | 0 | 5 | 0 | 5 |
| `Assets/ReferenceGameplay/VFX/Resources/ReferenceGameplay/` | 27 | 9,864,382 | 27 | 0 | 20 | 0 | 7 |
| `Assets/ReferenceGameplay/VFX/Textures/` | 36 | 468,915 | 36 | 0 | 35 | 0 | 1 |

### Ancestor folder .meta files to restore (absent on main)

```
Assets/Boosters.meta
Assets/Boosters/ColorBall.meta
Assets/Boosters/Propeller.meta
Assets/JellyGarden/Resources/RoyalGameplay/Audio.meta
Assets/JellyGarden/Scripts/Juice.meta
Assets/JellyGarden/Shaders.meta
Assets/Plugins/AllIn1VfxToolkit/Demo & Assets.meta
Assets/Plugins/AllIn1VfxToolkit/Demo & Assets/Demo.meta
Assets/Plugins/AllIn1VfxToolkit/Demo & Assets/Demo/Materials.meta
Assets/Plugins/AllIn1VfxToolkit/Demo & Assets/Demo/Prefabs.meta
Assets/Plugins/AllIn1VfxToolkit/Demo & Assets/Demo/Scripts.meta
Assets/Plugins/AllIn1VfxToolkit/Demo & Assets/Meshes.meta
Assets/Plugins/AllIn1VfxToolkit/Demo & Assets/Textures.meta
Assets/Plugins/AllIn1VfxToolkit/Demo & Assets/Textures/Distortion Normal Maps.meta
Assets/Plugins/AllIn1VfxToolkit/Demo & Assets/Textures/Greyscale Gradients.meta
Assets/Plugins/AllIn1VfxToolkit/Demo & Assets/Textures/Noise.meta
Assets/Plugins/AllIn1VfxToolkit/Demo & Assets/Textures/Shapes.meta
Assets/ReferenceGameplay/Editor.meta
Assets/ReferenceGameplay/Home.meta
Assets/ReferenceGameplay/Home/Scripts.meta
Assets/ReferenceGameplay/Layout.meta
Assets/ReferenceGameplay/Layout/Resources.meta
Assets/ReferenceGameplay/Layout/Resources/ReferenceGameplay.meta
Assets/ReferenceGameplay/Layout/Resources/ReferenceGameplay/Layout.meta
Assets/ReferenceGameplay/Resources.meta
Assets/ReferenceGameplay/Resources/ReferenceGameplay.meta
Assets/ReferenceGameplay/Scripts.meta
Assets/ReferenceGameplay/Tools.meta
Assets/ReferenceGameplay/Tools/Archer.meta
Assets/ReferenceGameplay/Tools/Cannon.meta
Assets/ReferenceGameplay/Tools/Hammer.meta
```

Folder .meta present on both sides with different bytes (keep main's; GUID-AUDIT decides): `Assets/Plugins.meta`, `Assets/Plugins/AllIn1VfxToolkit.meta`

## 4. Notes on the groups

**`code` (restore verbatim).**
- New runtime and editor scripts: `Juice/*` except 3 cut files, the in-level GUI subset, `RoyalSave`, `RoyalQaService`, `RoyalOfflineMode`, `ReferenceGameplay/Scripts` except 2 coin scripts, `Tools/Scripts`, `ReferenceWinFinish`, and the three booster folders.
- `RoyalOfflineMode.cs` is new against plan §5. Royal-throne's `LevelManager` references it, at the `RoyalOfflineMode` token.
- `Editor/RoyalWebPImporter.cs` is a `ScriptedImporter` for `.webp`. The 19 imported `RoyalGameplay/*.webp` sprites need it. It shells out to **FFmpeg** in the Editor, looking in `ROYAL_THRONE_FFMPEG`, `/opt/homebrew/bin/ffmpeg`, `/usr/local/bin/ffmpeg` and `/usr/bin/ffmpeg`.
  - It is present on this machine at `/opt/homebrew/bin/ffmpeg`.
  - Every other machine that imports the project needs FFmpeg, or these sprites fail to import.
- Two postprocessors change how the imported art imports, so they come along:
  - `ReferenceSpriteImporter.cs` handles `Art/Resources/ReferenceGameplay/Sprites/`, `Art/Atlases/` and `VFX/Textures/`.
  - `RoyalGameplayArtImporter.cs` handles `Resources/RoyalGameplayV4/`. Its line 44 calls `RoyalHomeScreenBuilder.RequireCopy()`, which must be cut.
- Live/dead labels:
  - **dead**: the plan Task 6.1 list. `RoyalColorBallSequence` holds `ColorBallPhase`, and `ColorBallFrameAnimator` holds the static `Cell`.
  - **fallback**: `TNT3DAnimator` and `TNTFrameAnimator`. Their assets are not imported (§1).
  - **qa-only**: `RoyalQaService` (`#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !ROYAL_DISABLE_QA`) and `RoyalRegressionRunner`.
  - `RoyalBlockSkin` has no code referrer on royal-throne. Royal-throne attached it in the block prefabs; plan Task 3.6 step 2 adds it at runtime instead. `RoyalBoardIntegrity` installs itself through `RuntimeInitializeOnLoadMethod(AfterSceneLoad)`.

**`editor-tests`.**
- The 17 plan Task 4.0 suites need 8 more to compile:
  - `RoyalEffectsChecks` calls `RoyalColorBallChecks.Run` (:100), `RoyalTNTChecks.Run` (:106), `RoyalRocketChecks.Run` (:112), `RoyalCellBreakChecks.RunRestored` (:118), `RoyalBoardSurfaceChecks.Run` (:141), `RoyalToolkitChecks.Run` (:143, :151) and `RoyalBoardPresentationChecks.Run` (:173).
  - `RoyalRocketChecks` needs `RoyalVfxLab`.
  - `RoyalInputRegression` is the input half of `RoyalEffectsChecks`.
- `PropellerV5Checks`, `ColorBallV4Checks` and `ColorBallV5Checks` travel inside the whole booster folders.
- Recommended extras, all coroutine `Run()` suites that need the Task 4.0 `PortChecks` driver:
  - `ReferenceBoosterChecks` (booster bar);
  - `ReferenceHammerChecks`, `ReferenceArcherChecks`, `ReferenceCannonChecks` (Task 4.8). All three need edits, listed in §5.
  - `ResponsiveInputChecks` (Task 4.5) and `ReferenceLayoutChecks` (HUD, Task 4.6).
- Their hub `ReferenceGameplayChecks` is **not** imported. It drives home, sky, level and campaign checks.
- `RoyalPresentationChecks` is not imported, because it needs the cut `RoyalLessonDemo` (`:46,49`).
- Asset needs for tests:
  - `RoyalTNTChecks` needs `Resources/RoyalTNT/**`, which is not imported.
  - `RoyalColorBallChecks` needs `Resources/RoyalColorBall/Frames/**`, which is not imported because it is dead.
  - Without those assets, these two suites, and the cases in `RoyalEffectsChecks` that call them, must be trimmed or marked skip. **Decision needed.**
- 3 assets are test-only: `RoyalGameplay/heart.webp`, `Sprites/add_smoke_soft__21919.png` and `Audio/rocket_tnt_combo__15488.wav`.

**`third-party`.** This is `Assets/Spine/` (whole) plus the 31 AllIn1 files in §1. The "live" counts in the folder table include only the Spine files the C# closure touches. The rest of Spine comes with the package.

**`assets`.** The 597 closure assets total 52.8 MB. 346 of them are already byte-identical on main: 343 under `ReferenceGameplay/`, plus `InputActions.inputactions`, `levelstate.txt.txt` and `RoyalColorBall/ColorBallGemGlow.shader`. `UnityAdsID.asset` differs, and main's copy is kept. So 30.6 MB is actually imported. Notable points:
- `RoyalGameplay/` brings 59 of its 103 files.
  - Out: `Music/**`, `Win/**`, `Audio/castle-theme.mp3` (home music), `Audio/royal-fanfare.wav` (win), `coin.webp`, `RewardChest/**`, `king-{idle,think,cheer}.webp` (avatar, D2), `home-castle.webp`, `level-card-crest.png`, `reward-rays.webp`, `settings.webp`, `win-cushion*.webp` and `win-star.png`.
  - `UIWinGlow.shader` is already on main.
- `RoyalColorBall/` brings only its 4 shaders; `ColorBallGemGlow.shader` is already identical on main.
  - `Frames/**` (108 files, 4.77 MB) is reached only through the dead `ColorBallBoostView`/`ColorBallFrameAnimator` path. `RoyalPowerupDirector.cs:354` reaches it only for payloads other than NONE, rocket, Propeller and TNT, after `:349-350`.
  - `VFX/{Beam,Glow,Impact}/**` (90 webp) is loaded by no code on royal-throne.
- `RoyalGameplayV4/` (whole, 33 PNG) is the fallback board art pack. `RoyalGameplayArt.LegacyArt` still serves `cell-light`, the blocker and goal art (`ice`, `crate`, `stone`, `chains`, `key`, `chest`) and, as a fallback after the catalog, the Rocket direction sprites (`RoyalRocketSequence.cs:105,114`).
- `ReferenceGameplay/Art/.../Sprites`: 177 of 1,243.
  - The board pieces and powerups come from `runtime-catalog.json` entries named in code.
  - The rest are referenced by VFX and animation through guid.
  - D8 blocker sprites (`box1-3`, `coin`, `cupboard`, `egg`, `mailbox`, `potion`, `grass`) come in only because their catalog keys are literals in `RoyalPieceSkin.CampaignKeys` and `RoyalVisualCatalog`. **They can be dropped once the coupling cut removes those keys.**

**`kit-merge`.** These are the plan §4 restore-then-edit files. They are **not** in `import-paths.txt`; Tasks 3.1 to 3.4 restore them one by one.

**`kit-manual`.** For these, keep main's file and hand-merge only the royal-throne members that imported code uses:

| Kit file (keep main's) | Royal-throne member the closure needs | Used at (royal-throne lines) |
|---|---|---|
| `GUI/MenuManager.cs` | `BlocksBoardInput` | `RoyalGameplayPresentation.cs:126,167,223`; `Item.cs:1843`; `LevelManager.cs:257,955,1011`; `ReferenceBoosterBar.cs:271,296` |
| `GUI/AnimationManager.cs` | `IsOpen` | `ReferenceBoosterBar.cs:290` (shop branch, cut with D5) |
| `InitScript.cs` | `LifeRefillDeadline`, `RestoreLifeScheduleForTesting`, `SetRefillSeconds` | `RoyalQaService.cs:72,98,136` |
| `InitScript.cs` | `NextLifeKey` | `RoyalQaChecks.cs:34` |
| `InitScript.cs` | `TryOpenLevel` | `RoyalEffectsChecks.cs:23` (and `RoyalPresentationChecks.cs:26`, not imported) |
| `SoundBase.cs` | `RoyalAudioDirector` / `RoyalCue` routing | union per plan §4 |
| `MusicBase.cs` | routes music to `RoyalAudioDirector` | music is out of scope; recommend keeping main's `MusicBase` unchanged |

**`kit-keep-main`.**
- Royal-throne changed these kit files, but nothing in the closure needs those changes. The reason for each file is in the table.
- `GUI/BoostAnimation.cs` (a `ParticleSystem.main` fix) and `MapScripts/LevelsMap.cs` (`FindObjectsByType` plus teardown null guards) are optional cherry-picks.
- `GUI/Tutorial/HandTutorial.cs` was edited on both sides.

**`kit-unchanged`.** These are already identical on main, so nothing needs doing.

## 5. Cut list: references from imported code into excluded areas

These are flags for the cut plan (`docs/port/COUPLINGS.md`). Each row shows the first three lines and the total number of hits; `JS/` means `Assets/JellyGarden/Scripts/`.

The same machine-readable data is in `import-closure.json` under `code_cuts`, `partial_parts_cut` and `not_followed`.

| Referencing file:lines (count) | Type | Defined in (excluded) | Area |
|---|---|---|---|
| `JS/GUI/RoyalGameplayPresentation.cs:111,112,113` (8) | `KingVanguardActivityEvents` | `Assets/Characters/KingVenguard/V2/Scripts/KingVanguardActivityEvents.cs` | KingVanguard (D2) |
| `JS/GUI/RoyalGameplayPresentation.cs:169,170,171` (7) | `KingVanguardCharacter` | `Assets/Characters/KingVenguard/V2/Scripts/KingVanguardCharacter.cs` | KingVanguard (D2) |
| `JS/GUI/RoyalGameplayPresentation.cs:18,144,154` (9) | `KingVanguardFeedbackQueue` | `Assets/Characters/KingVenguard/V2/Scripts/KingVanguardFeedbackQueue.cs` | KingVanguard (D2) |
| `JS/GUI/RoyalGameplayPresentation.cs:17,268` (2) | `KingVanguardSlot` | `Assets/Characters/KingVenguard/V2/Scripts/KingVanguardSlot.cs` | KingVanguard (D2) |
| `JS/Item.cs:1184,1243` (2) | `KingVanguardActivityEvents` | `Assets/Characters/KingVenguard/V2/Scripts/KingVanguardActivityEvents.cs` | KingVanguard (D2) |
| `JS/LevelManager.cs:159,862,1839` (3) | `KingVanguardActivityEvents` | `Assets/Characters/KingVenguard/V2/Scripts/KingVanguardActivityEvents.cs` | KingVanguard (D2) |
| `JS/GUI/RoyalGameplayHud.cs:162,180,284` (3) | `RoyalMenuSkin` | `JS/GUI/RoyalMenuSkin.cs` | QA mod-menu UI |
| `JS/GUI/RoyalGameplayPresentation.cs:291` (1) | `RoyalMenuPresenter` | `JS/GUI/RoyalMenuPresenter.cs` | QA mod-menu UI |
| `JS/GUI/RoyalGameplayPresentation.cs:236,314` (2) | `RoyalMenuSkin` | `JS/GUI/RoyalMenuSkin.cs` | QA mod-menu UI |
| `JS/LevelManager.cs:949,952,955` (4) | `RoyalBackButton` | `JS/GUI/RoyalBackButton.cs` | QA mod-menu UI |
| `JS/RoyalQaService.cs:236` (1) | `RoyalQaMenu` | `JS/RoyalQaMenu.cs` | QA mod-menu UI |
| `Assets/ReferenceGameplay/Tools/Scripts/ReferenceBoosterBar.cs:290,292,293` (3) | `RoyalBackButton` | `JS/GUI/RoyalBackButton.cs` | QA mod-menu UI |
| `Assets/ReferenceGameplay/Tools/Scripts/ReferenceBoosterBar.cs:112,113,121` (3) | `RoyalMenuSkin` | `JS/GUI/RoyalMenuSkin.cs` | QA mod-menu UI |
| `JS/Editor/RoyalQaChecks.cs:122` (1) | `RoyalEconomy` | `JS/Juice/RoyalEconomy.cs` | RoyalEconomy/shop |
| `JS/GUI/BoostShop.cs:61` (1) | `RoyalEconomy` | `JS/Juice/RoyalEconomy.cs` | RoyalEconomy/shop |
| `JS/GUI/LifeShop.cs:25` (1) | `RoyalEconomy` | `JS/Juice/RoyalEconomy.cs` | RoyalEconomy/shop |
| `JS/GUI/PreFailed.cs:27` (1) | `RoyalEconomy` | `JS/Juice/RoyalEconomy.cs` | RoyalEconomy/shop |
| `JS/LevelManager.cs:832` (1) | `RoyalEconomy` | `JS/Juice/RoyalEconomy.cs` | RoyalEconomy/shop |
| `Assets/ReferenceGameplay/Tools/Scripts/ReferenceBoosterBar.cs:258` (1) | `RoyalEconomy` | `JS/Juice/RoyalEconomy.cs` | RoyalEconomy/shop |
| `Assets/ReferenceGameplay/Tools/Scripts/ReferenceBoosterBar.cs:264,290` (2) | `RoyalShopPage` | `JS/GUI/RoyalShopPage.cs` | RoyalEconomy/shop |
| `JS/Editor/RoyalEffectsChecks.cs:59` (1) | `RoyalHomeScreenBuilder` | `JS/Editor/RoyalHomeScreenBuilder.cs` | home/title/splash/loading |
| `JS/Editor/RoyalGameplayArtImporter.cs:44` (1) | `RoyalHomeScreenBuilder` | `JS/Editor/RoyalHomeScreenBuilder.cs` | home/title/splash/loading |
| `JS/Editor/RoyalPresentationChecks.cs:46,49` (2) | `RoyalLessonDemo` | `JS/GUI/RoyalLessonDemo.cs` | home/title/splash/loading |
| `JS/GUI/RoyalGameplayPresentation.cs:69` (1) | `RoyalTitleScreen` | `JS/GUI/RoyalTitleScreen.cs` | home/title/splash/loading |
| `JS/GUI/Tutorial.cs:23,50,51` (3) | `RoyalLessonDemo` | `JS/GUI/RoyalLessonDemo.cs` | home/title/splash/loading |
| `JS/RoyalQaService.cs:131` (1) | `RoyalHomeScreen` | `JS/GUI/RoyalHomeScreen.cs` | home/title/splash/loading |
| `Assets/ReferenceGameplay/Editor/ReferenceArcherChecks.cs:27` (1) | `RoyalTitleScreen` | `JS/GUI/RoyalTitleScreen.cs` | home/title/splash/loading |
| `Assets/ReferenceGameplay/Editor/ReferenceCannonChecks.cs:28` (1) | `RoyalTitleScreen` | `JS/GUI/RoyalTitleScreen.cs` | home/title/splash/loading |
| `Assets/ReferenceGameplay/Editor/ReferenceHammerChecks.cs:27` (1) | `RoyalTitleScreen` | `JS/GUI/RoyalTitleScreen.cs` | home/title/splash/loading |
| `JS/Editor/RoyalEffectsChecks.cs:55,145` (2) | `RoyalReviewAudioCapture` | `JS/Juice/RoyalReviewAudioCapture.cs` | music/ambience |
| `JS/Item.cs:1559` (1) | `PropellerVfxV3` | `Assets/Boosters/Propeller/V3/Scripts/PropellerVfxV3.cs` | old booster versions |
| `JS/Item.cs:1549` (1) | `PropellerVfxV4` | `Assets/Boosters/Propeller/V4/Scripts/PropellerVfxV4.cs` | old booster versions |
| `JS/Juice/RoyalPowerupDirector.cs:99` (1) | `PropellerVisualV2` | `Assets/Boosters/Propeller/V2/Scripts/PropellerVisualV2.cs` | old booster versions |
| `JS/Juice/RoyalPowerupDirector.cs:99` (1) | `PropellerVisualV3` | `Assets/Boosters/Propeller/V3/Scripts/PropellerVisualV3.cs` | old booster versions |
| `JS/Juice/RoyalPowerupDirector.cs:99` (1) | `PropellerVisualV4` | `Assets/Boosters/Propeller/V4/Scripts/PropellerVisualV4.cs` | old booster versions |
| `JS/AI.cs:158,589,638` (3) | `RoyalPowerTutorial` | `JS/GUI/Tutorial/RoyalPowerTutorial.cs` | power tutorial |
| `Assets/Boosters/Propeller/V5/Scripts/PropellerGameplayV5.cs:45,46,47` (3) | `RoyalCampaignBlockers` | `JS/RoyalCampaignBlockers.cs` | reference campaign (D8) |
| `Assets/Boosters/Propeller/V5/Scripts/PropellerGameplayV5.cs:49` (1) | `RoyalCampaignItem` | `JS/RoyalCampaignItem.cs` | reference campaign (D8) |
| `Assets/Boosters/Propeller/V5/Scripts/PropellerGameplayV5.cs:43` (1) | `RoyalReferenceCampaign` | `JS/RoyalReferenceCampaign.cs` | reference campaign (D8) |
| `JS/AI.cs:125,209,458` (6) | `RoyalCampaignBlockers` | `JS/RoyalCampaignBlockers.cs` | reference campaign (D8) |
| `JS/Editor/RoyalQaChecks.cs:88` (1) | `RoyalReferenceCampaign` | `JS/RoyalReferenceCampaign.cs` | reference campaign (D8) |
| `JS/GUI/Tutorial/TutorialManager.cs:69` (1) | `RoyalReferenceCampaign` | `JS/RoyalReferenceCampaign.cs` | reference campaign (D8) |
| `JS/Item.cs:435,470,1301` (6) | `RoyalCampaignBlockers` | `JS/RoyalCampaignBlockers.cs` | reference campaign (D8) |
| `JS/Item.cs:194` (1) | `RoyalReferenceCampaign` | `JS/RoyalReferenceCampaign.cs` | reference campaign (D8) |
| `JS/Juice/CandyShatterService.cs:360` (1) | `RoyalCampaignBlockers` | `JS/RoyalCampaignBlockers.cs` | reference campaign (D8) |
| `JS/Juice/RoyalHintSolver.cs:134` (1) | `RoyalCampaignBlockers` | `JS/RoyalCampaignBlockers.cs` | reference campaign (D8) |
| `JS/Juice/RoyalIceBreak.cs:45,46` (2) | `RoyalCampaignBlockers` | `JS/RoyalCampaignBlockers.cs` | reference campaign (D8) |
| `JS/Juice/RoyalPieceSkin.cs:129` (1) | `RoyalCampaignItem` | `JS/RoyalCampaignItem.cs` | reference campaign (D8) |
| `JS/Juice/RoyalPowerupDirector.cs:326` (1) | `RoyalCampaignBlockers` | `JS/RoyalCampaignBlockers.cs` | reference campaign (D8) |
| `JS/Juice/RoyalShuffle.cs:25,85` (2) | `RoyalCampaignBlockers` | `JS/RoyalCampaignBlockers.cs` | reference campaign (D8) |
| `JS/LevelManager.cs:1594,1670,1831` (3) | `RoyalCampaignBlockers` | `JS/RoyalCampaignBlockers.cs` | reference campaign (D8) |
| `JS/LevelManager.cs:542,785,1483` (7) | `RoyalReferenceCampaign` | `JS/RoyalReferenceCampaign.cs` | reference campaign (D8) |
| `JS/RoyalQaService.cs:96,171,172` (6) | `RoyalReferenceCampaign` | `JS/RoyalReferenceCampaign.cs` | reference campaign (D8) |
| `JS/Square.cs:145,277,288` (3) | `RoyalCampaignBlockers` | `JS/RoyalCampaignBlockers.cs` | reference campaign (D8) |
| `JS/Square.cs:104` (1) | `RoyalReferenceCampaign` | `JS/RoyalReferenceCampaign.cs` | reference campaign (D8) |
| `JS/System/CombineManager.cs:223,257` (2) | `RoyalCampaignBlockers` | `JS/RoyalCampaignBlockers.cs` | reference campaign (D8) |
| `JS/Targets/TargetsGroup.cs:28` (1) | `RoyalReferenceCampaign` | `JS/RoyalReferenceCampaign.cs` | reference campaign (D8) |
| `Assets/ReferenceGameplay/Editor/ReferenceArcherChecks.cs:50,71` (2) | `RoyalReferenceCampaign` | `JS/RoyalReferenceCampaign.cs` | reference campaign (D8) |
| `Assets/ReferenceGameplay/Editor/ReferenceCannonChecks.cs:79,80,81` (3) | `RoyalReferenceCampaign` | `JS/RoyalReferenceCampaign.cs` | reference campaign (D8) |
| `Assets/ReferenceGameplay/Editor/ReferenceHammerChecks.cs:46,66` (2) | `RoyalReferenceCampaign` | `JS/RoyalReferenceCampaign.cs` | reference campaign (D8) |
| `Assets/ReferenceGameplay/Home/Scripts/ReferenceWinFinish.cs:17` (1) | `RoyalCampaignBlockers` | `JS/RoyalCampaignBlockers.cs` | reference campaign (D8) |
| `Assets/ReferenceGameplay/Tools/Scripts/ReferenceBoosterBar.cs:317` (1) | `RoyalCampaignBlockers` | `JS/RoyalCampaignBlockers.cs` | reference campaign (D8) |
| `Assets/ReferenceGameplay/Tools/Scripts/ReferenceBoosterBar.cs:30,232` (2) | `RoyalReferenceCampaign` | `JS/RoyalReferenceCampaign.cs` | reference campaign (D8) |
| `JS/GUI/Tutorial.cs:51,52` (2) | `RoyalCards` | `JS/GUI/RoyalCards.cs` | settings/profile/leaderboard/cards pages |
| `JS/Item.cs:1442` (1) | `RoyalProfile` | `JS/RoyalProfile.cs` | settings/profile/leaderboard/cards pages |
| `JS/LevelManager.cs:600,830` (2) | `RoyalProfile` | `JS/RoyalProfile.cs` | settings/profile/leaderboard/cards pages |
| `JS/Item.cs:1333` (1) | `RoyalWinSequence` | `JS/Juice/RoyalWinSequence.cs` | win card/RoyalWinSequence |
| `JS/LevelManager.cs:389,874,879` (4) | `RoyalWinSequence` | `JS/Juice/RoyalWinSequence.cs` | win card/RoyalWinSequence |

Partial-class parts of `RoyalPowerupDirector` left out (they compile with the type wherever they live; nothing in the closure calls their members except the `Cancel()` / `Item` sites listed above):
- `Assets/Boosters/ColorBall/V2/Scripts/ColorBallV2Sequence.cs`
- `Assets/Boosters/ColorBall/V3/Scripts/ColorBallV3Sequence.cs`
- `Assets/Boosters/Propeller/V2/Scripts/PropellerGameplayV2.cs`
- `Assets/Boosters/Propeller/V3/Scripts/PropellerGameplayV3.cs`
- `Assets/Boosters/Propeller/V4/Scripts/PropellerGameplayV4.cs`

| Kit file kept as main's | royal-throne reference not followed |
|---|---|
| `JS/GUI/MenuManager.cs` | `RoyalShopPage` -> `JS/GUI/RoyalShopPage.cs` |
| `JS/InitScript.cs` | `FacebookManager` -> `JS/Integrations/FacebookManager.cs` |
| `JS/InitScript.cs` | `UnityAdsController` -> `JS/Integrations/UnityAdsController.cs` |
| `JS/InitScript.cs` | `RoyalEconomy` -> `JS/Juice/RoyalEconomy.cs` |
| `JS/InitScript.cs` | `DateTimeManager,RewardedAdsTime` -> `JS/System/DateTimeManager.cs` |
| `JS/GUI/AnimationManager.cs` | `KingVanguardActivityEvents` -> `Assets/Characters/KingVenguard/V2/Scripts/KingVanguardActivityEvents.cs` |
| `JS/GUI/AnimationManager.cs` | `RoyalBackButton` -> `JS/GUI/RoyalBackButton.cs` |
| `JS/GUI/AnimationManager.cs` | `RoyalMenuSkin` -> `JS/GUI/RoyalMenuSkin.cs` |
| `JS/GUI/AnimationManager.cs` | `RoyalNotice` -> `JS/GUI/RoyalNotice.cs` |
| `JS/GUI/AnimationManager.cs` | `RoyalScreenCover` -> `JS/GUI/RoyalScreenCover.cs` |
| `JS/GUI/AnimationManager.cs` | `RoyalShopPage` -> `JS/GUI/RoyalShopPage.cs` |
| `JS/GUI/AnimationManager.cs` | `RoyalReferenceCampaign` -> `JS/RoyalReferenceCampaign.cs` |

Other notes for the cut plan:
- `RoyalGameplayArtImporter.cs:44` and `RoyalEffectsChecks.cs:59` call `RoyalHomeScreenBuilder.RequireCopy()`.
- `RoyalPieceSkin.CampaignArt` / `CampaignResourcePath` (`RoyalPieceSkin.cs:12-51`) loads from `RoyalCampaignArt/` (D8, not imported). For in-level pieces it is only a fallback after `ReferenceGameplayCatalog`. With the folder absent, `RoyalGameplayArt.Art(key)` (`RoyalGameplayArt.cs:32-36`) and `RoyalPieceSkin.Art` fall through to `RoyalGameplayV4/` and `RoyalGameplay/*.webp`.
- **D3 note (Task 4.2).** Colour 5 (`"vase"`) has no catalog entry. Without `RoyalCampaignArt` it resolves to `RoyalGameplayV4/06_optional_extras/porcelain_vase`. With `RoyalCampaignArt` present it would have resolved to `RoyalCampaignArt/gem.png`, through `RoyalPieceSkin.cs:36`.
- `RoyalAudioDirector.cs:35-36` preloads `castle-theme` (home music) and `royal-fanfare` (win), and `:134` loads `RoyalGameplay/Music/`. `RoyalGameplayWarmup.cs:27` prewarms `royal-fanfare`. All of these are music or win, so cut or guard them.
- `Editor/RoyalOfflineBuildPolicy.cs` is **not** in the closure. Nothing in scope references it; only `RoyalAndroidBuild`, `RoyalThroneAndroidBuild` and royal-throne's `LevelMakerEditor` do. It is an `IPreprocessBuildWithReport` (`:40`). Plan Task 3.5 step 3 edits it, but it is optional. Importing it unchanged would refuse main's builds while ad or IAP defines are set. `RoyalQaService.cs:68` has its own `#if … UNITY_ADS …` guard.

## 6. Asset cuts: excluded resources that imported code loads

| Area | Excluded target (folder) | Files | Bytes | Reached from |
|---|---|---:|---:|---|
| KingVanguard/avatar art (D2) | `Assets/JellyGarden/Resources/RoyalGameplay/` | 3 | 80,516 | `JS/Juice/RoyalPieceSkin.cs:64` |
| TNT 3D fallback | `Assets/JellyGarden/Resources/RoyalTNT/` | 1 | 263,949 | `JS/Juice/TNT3DAnimator.cs:59` |
| TNT 3D fallback | `Assets/JellyGarden/Resources/RoyalTNT/Frames/` | 240 | 26,608,182 | `JS/Juice/TNTFrameAnimator.cs:126` |
| dead code path (plan 6.1) | `Assets/JellyGarden/Resources/RoyalColorBall/Frames/` | 108 | 4,770,380 | `JS/Juice/ColorBallFrameAnimator.cs:133` |
| economy (coins) | `Assets/JellyGarden/Resources/RoyalGameplay/` | 1 | 43,554 | `JS/Juice/RoyalPieceSkin.cs:64` |
| kit level data (main's kept) | `Assets/JellyGarden/Resources/Levels/` | 100 | 37,706 | `JS/Editor/RoyalQaChecks.cs:35`, `JS/GUI/AnimationManager.cs:383`, `JS/LevelManager.cs:2539`, `JS/LevelManager.cs:2542` … |
| kit level data (main's kept) | `Assets/JellyGarden/Resources/Targets/` | 2 | 11,024 | `JS/Editor/RoyalEffectsChecks.cs:138`, `JS/LevelManager.cs:2637`, `JS/Targets/TargetsGroup.cs:36` |
| kit prefabs (main's kept, plan 3.6 step 2) | `Assets/JellyGarden/Resources/Prefabs/` | 8 | 34,810 | `JS/Editor/RoyalEffectsChecks.cs:128`, `JS/Editor/RoyalEffectsChecks.cs:135`, `JS/Editor/RoyalRocketChecks.cs:374`, `JS/Item.cs:178` … |
| kit prefabs (main's kept, plan 3.6 step 2) | `Assets/JellyGarden/Resources/Prefabs/Effects/` | 5 | 226,126 | `JS/GUI/BoostAnimation.cs:24`, `JS/GUI/BoostAnimation.cs:32`, `JS/Item.cs:1350`, `JS/Item.cs:1384` … |
| music/ambience | `Assets/JellyGarden/Resources/RoyalGameplay/Audio/` | 1 | 1,628,413 | `JS/Editor/RoyalEffectsChecks.cs:125`, `JS/Juice/RoyalAudioDirector.cs:36`, `JS/Juice/RoyalGameplayWarmup.cs:64` |
| music/ambience | `Assets/JellyGarden/Resources/RoyalGameplay/Music/` | 1 | 2,665,222 | `JS/Editor/RoyalEffectsChecks.cs:126`, `JS/Juice/RoyalAudioDirector.cs:134` |
| reference campaign (D8) | `Assets/JellyGarden/Resources/RoyalCampaignArt/` | 10 | 12,472,390 | `JS/Juice/RoyalPieceSkin.cs:51` |
| win card | `Assets/JellyGarden/Resources/RoyalGameplay/Audio/` | 1 | 617,444 | `JS/Editor/RoyalEffectsChecks.cs:125`, `JS/Juice/RoyalAudioDirector.cs:36`, `JS/Juice/RoyalGameplayWarmup.cs:64` |

The `kit level data` and `kit prefabs` rows are kit resources that main already has in its own version (1,208 levels, its own `Item.prefab`). They are cut so that royal-throne's 100 levels and its modified kit prefabs are not restored (plan Task 3.6 step 2).

## 7. Where this differs from the plan (§5, Tasks 3.5, 3.6 and 4.0)

| Plan says | Closure says |
|---|---|
| `Juice/**` | All except `RoyalEconomy.cs`, `RoyalWinSequence.cs` and `RoyalReviewAudioCapture.cs` (cut areas) |
| GUI subset of 7 files "plus whatever Task 1.3 proves" | 9 files: the plan's 7 plus `RoyalPressTracker.cs` and `RoyalTextStyle.cs` (both used by `RoyalGameplayHud`) |
| — | `RoyalOfflineMode.cs`, `Editor/RoyalWebPImporter.cs` (needs FFmpeg), `Editor/RoyalGameplayArtImporter.cs`, `ReferenceGameplay/Editor/ReferenceSpriteImporter.cs` are needed |
| `Assets/ReferenceGameplay/Scripts/**` | All except `ReferenceCoinAnimation.cs` / `ReferenceCoinParticles.cs`: their only user is `RoyalWinSequence` (cut) |
| `Assets/ReferenceGameplay/Editor/**` | Only `ReferenceSpriteImporter`, `RoyalAllDualComboChecks`, `RoyalDualColorBallChecks` + 6 recommended tool/input/layout suites. The rest are one-off APK importers or home/campaign checks |
| Shaders `RoyalTNTBody`, `RoyalTNTGlow` | Not needed (only `Resources/RoyalTNT/*.mat` use them) |
| `RoyalGameplay/**` except EffectsProfile/Music/Win | 59 of 103 files (§4). `EffectsProfile.asset` is listed as a `# held back:` comment |
| `RoyalColorBall/**` | 4 shaders only; `Frames/**` dead, `VFX/**` unreferenced |
| `Assets/ReferenceGameplay/Resources/ReferenceGameplay/{Visuals,VFX,Audio,Sprites,Tools,Layout}/**` | Those keys are spread over 10 `Resources/` roots (§1); 482 asset files are needed, 343 already identical on main |
| `Assets/ReferenceGameplay/{Animation,Art,Audio,VFX}/**` | Only the guid-referenced subset (see the `assets` table) |
| Task 4.0 suite list | Plus 8 compile dependencies of `RoyalEffectsChecks`/`RoyalRocketChecks`; `RoyalTNTChecks`/`RoyalColorBallChecks` need assets this list does not import |
| AllIn1 demo textures only | 31 files including 2 demo prefabs, 11 materials, 1 mesh and 2 demo scripts |

## 8. Not verified

- Liveness labels come from static references, `RuntimeInitializeOnLoadMethod` and prefab `m_Script` use. They were not checked by running the game.
- `kit-keep-main` assumes main's version of each file compiles against royal-throne's kit-core files. That is **UNVERIFIED** until the Task 3.8 typecheck.
- The C# closure is a heuristic, not a compiler. It may report false edges where unrelated namespaces share a type name. Every `new` file it pulled in was checked by hand above.
- Sizes are bytes from `git ls-tree -l`, with `.meta` bytes counted separately. royal-throne has no `.gitattributes` and the blobs are real file content, so there is no LFS pointer to worry about. This was checked.
- These were not run:
  - No Unity import was run.
  - The webp importer's FFmpeg requirement was not tested end to end.
