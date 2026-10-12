# Assets left unused by the Story retirement (plan Task 6.3 input; Task 3.7)

This is an inventory, **not a deletion list**. Main's rule forbids deleting any sprite, prefab, animation, material or shader (`tools/check_vfx_intact.py`); the owner decides on these later. The retired set is the one in `docs/port/RETIRE.md` §3: the 37 files from plan §5 plus `KingLeoVfxHost`, `StoryVfxFlags`, `StorybookBoardReview` and `StoryBoardBorderTests`.

**Method:**
1. I collected every `Resources.Load`, `ReferenceTnt.Visual/VfxPrefab/Clip/SpawnVisual`, `ReferenceBoosters.Sprite` and `Shader.Find` call in the retired scripts, resolving the constants in `Assets/ReferenceGameplay/Preview/Reference{Tnt,Boosters}.cs`.
2. I matched each key against every `Resources/` asset in the project.
3. I then searched for any other user:
   - kept main code;
   - royal-throne code that the port imports (`origin/royal-throne`, `*.cs` and `*.json`);
   - GUID references from scenes, prefabs, materials, controllers and assets, on main and on `origin/royal-throne`.

Everything was measured at `b47ca0fa`. Anything I could not verify is marked UNVERIFIED.

## A. Unused once the retirement lands

No kept main script, no imported royal-throne script and no asset references these. Anything under a `Resources/` folder still ships in every build until it is removed, so these entries also count toward build size.

| Asset (repo path) | Size | Only users (all retired) | Notes |
|---|---|---|---|
| `Assets/RoyalRocketVFX/**`: 7 `.cs` (own asmdef `RoyalRocketVFX`), 8 `.mat`, 3 `.prefab`, 1 `.shader` (`RoyalRocketVFX/RocketFire`), 4 `.png` (`Sprites/Rocket{Up,Down,Left,Right}`), `Profiles/DefaultRocketProfile.asset`, `Demo/RocketVFX_Demo.unity`, READMEs | 2.32 MB, 40 GUIDs | `StoryRoyalVfx.cs:11,129` loads Resources key **`RoyalVfx/RocketVFX_Horizontal`** (`Assets/RoyalRocketVFX/Resources/RoyalVfx/RocketVFX_Horizontal.prefab`). Types `RoyalRocketVFX.*` are used by `StoryRoyalVfx.cs:4,16,81–90` and `StoryRocketVariant.cs:105`. `StoryRocketValidation.cs:115` calls `Shader.Find("RoyalRocketVFX/RocketFire")`. | 0 GUID users outside the folder on main or on royal-throne. Royal-throne carries its own copy of this folder, whose tree differs from main's, and never references it either. Task 1.2's GUID audit decides which bytes land. |
| `Assets/RoyalTNTVFX/**`: 4 `.cs` (own asmdef `RoyalTNTVFX`), 4 `.mat`, 3 `.prefab`, 1 `.shader` (`RoyalTNTVFX/TNTGlow`), `Textures/TNT.png`, `Demo/TNTExplosionVFX_Demo.unity`, READMEs | 0.41 MB, 26 GUIDs | `StoryRoyalVfx.cs:12,129` loads Resources key **`RoyalVfx/TNTExplosionVFX`** (`Assets/RoyalTNTVFX/Resources/RoyalVfx/TNTExplosionVFX.prefab`). Types `RoyalTNTVFX.*` are used by `StoryRoyalVfx.cs:5,17` and `StoryOriginalTnt.cs:1,73,146`. `StoryRocketValidation.cs:116` calls `Shader.Find("RoyalTNTVFX/TNTGlow")`. | Same situation as `RoyalRocketVFX`: 0 GUID users outside the folder on either branch; royal-throne has its own differing copy. |
| `Packages/com.royalthrone.match3-vfx/**`, an embedded package (runtime, editor, tests, samples) | 2.9 MB, 96 GUIDs | `StoryRocketFlight.cs:101` loads Resources key **`Match3Vfx/Additive`** (`…/Runtime/Resources/Match3Vfx/Additive.mat`) and uses `Match3Shapes.GlowSprite/SparkleSprite` (117–128). `KingLeoVfxHost.cs:26–27` calls `Match3Vfx.Configure/Prewarm`. Main's `Item.cs:1350` calls `Match3PieceMotion.Pop`, which §4 drops. | 0 GUID references from `Assets/`. Royal-throne's `Packages/manifest.json` does not list it; that branch keeps it under `SharedPackages/`, and its `Assets/` code does not use it. `Packages/` is outside this task's write scope. Removing the package is a separate decision. |
| `Assets/JellyGarden/Resources/Story/RoyalBoard/courtyard.png` | 2.02 MB | Resources key `Story/RoyalBoard/courtyard`, through `StorybookBoardArt.Get("courtyard")` in `StoryBoardTheme.cs:254` (`ApplyBackdrop`). That method no longer runs, because `StoryBoardTheme` is not installed after D6. The retired `StorybookBoardLayout.DressBackdrop` also dressed it. | No other key, script or asset names it. It becomes fully unreferenced once `StoryBoardTheme`'s in-level code is reduced (RETIRE.md §2.2, full reduction). |

## B. Loaded by retired scripts but still in use (do not list as unused)

| Resources key(s) | Retired loader(s) | Remaining user(s) |
|---|---|---|
| 44 `ReferenceGameplay/{Visuals,VFX,Audio,Sprites}/…` keys. Examples: `Visuals/Rocket`, `Visuals/TNT`, `Visuals/ColorBall`, `Visuals/ColorBallColorBallComboRfv387`, `Visuals/TNTTNTComboRfv387`, `VFX/RocketItemParticles`, `VFX/MatchItemExplodeParticles`, `VFX/Remaining Moves(Hit)Particles`, `VFX/Lightball*`, `VFX/BallBallCombo*`, `VFX/Tnt*`, `Audio/match_explode1–6__*`, `Audio/lightball_*`, `Audio/rocket_*`, `Audio/tnt_*`, `Sprites/Items-SpecialItems-RocketItem-*_part_*` | `StoryOriginalBoosters`, `StoryOriginalTnt`, `StoryOriginalTntPiece`, `StoryOriginalColorBall`, `StoryOriginalPiece`, `StoryOriginalBoostersValidation`, `StoryOriginalTntValidation`, `StoryRocketFlight`, `RocketFlameGapCheck` | Main's kept `Assets/ReferenceGameplay/Preview/{ReferenceTnt,ReferenceBoosters,ReferenceTntPreview,ReferenceBoosterPreview}.cs` and `Assets/ReferenceGameplay/Tests/Editor/*`, plus royal-throne's imported code at the **same Resources paths**. All 234 of main's `Assets/ReferenceGameplay/**/Resources/**` files exist at identical paths on royal-throne. Spot check of 11 keys: each is named in 1–2 royal-throne scripts. |
| `ReferenceGameplay/Audio/fall_hit_1__15531` | `StoryOriginalMotion.cs:293` (only main user) | royal-throne `Assets/ReferenceGameplay/Scripts/ReferenceGameplayAudio.cs` |
| `RoyalColorBall/ColorBallGemGlow` (`Assets/JellyGarden/Resources/RoyalColorBall/ColorBallGemGlow.shader`) | `StoryOriginalChargedTarget.cs:70` (only main user) | royal-throne `Assets/Boosters/ColorBall/V4/Scripts/ColorBallChargedTargetV4.cs` (imported in Task 3.5) |
| `Story/TreasureHunt/UI/Layers/rocket` | `StoryRocketFlight.cs:86` | `StorybookArt.Crop("rocket", …)` at `StorybookArt.cs:124`, through `StorybookLayers.Get` |
| `Story/RoyalBoard/{book,coin,gem,shield,leaf,crown,tnt,orb,bomb,rocket,hammer,glove}`, `Story/TreasureHunt/Board/{ice,locket,ring,score,moves,time}` | `StorybookBoardArt.Apply` (removed), `StorybookBoardLayout` (`ice`), `StoryLevelHud.Soft` (booster art) | `StorybookBoardArt.Piece/Booster/Goal/Get` from `StoryPrePlay`, `StoryBoosters` (level popup), `StoryLevelHud.ReadLevel` (level popup), `Targets/TargetIcon`, `Targets/TargetLevel` |
| `ReferenceGameplay/VFX/StarHitParticles`, `ReferenceGameplay/VFX/StarIdleParticles`, `RoyalGameplay/UIWinGlow` | none (listed because `StoryOriginalWinStars` lost its `StoryOriginalBoosters.Ready` gate) | `StoryOriginalWinStars.cs:56–58` (kept; win card). Royal-throne has the same files at the same paths (`UIWinGlow.shader` blob and `.meta` are identical). |
| `Story/Art/backdrop_*` | none retired; `StoryBoardTheme.ApplyBackdrop` used them as the in-level fallback | `StoryArt.ArcBackdrop` (area and home screens, `StoryContentAudit`, `StorySmokeTest`, `StoryScreenTests`); `backdrop_castle` is also aliased in `Story/TreasureHunt/campaign.json` |

The retired VFX layers (`StoryRocketVfx`, `StoryBombVfx`, `StoryColorBombVfx`, `StoryBlastVfx`, `StoryShatterVfx`, `StoryPunchVfx`, `StoryPopVfx`, `StoryFinishVfx`, `StoryExplosionProbe`, `StoryVfxLab`, `StoryRoyalBoardFrame`, `StoryBoardScale`, `StoryBoardCentre`, `StoryPixelSnap`) load **no** project asset. They build textures and sprites in code and use only built-in shaders (`Sprites/Default`, `Particles/Additive`, `Legacy Shaders/Particles/*`). They leave nothing behind.

## C. Still referenced, but no longer visible in a level once the Royal layer is on

| Asset | Why it stays | Note |
|---|---|---|
| The `StoryLevelHud` strip baked into `Assets/JellyGarden/Scenes/game.unity` (under `Level/Canvas/Panel/Panel`), with `Resources/Story/UI/hud_reference_frame` and `hud_reference_moves` | `StoryLevelHud.Build` (editor tests, `StoryHudBaker`), and the fallback HUD while the master switch is off | Royal-throne hides `Canvas/Panel` with a CanvasGroup when its HUD builds. The kit's `TargetsGroup` lives under this strip and must stay active (RETIRE.md §2.4). Candidate for cleanup in Task 4.6, which edits the scene, not this task. |
| `StoryAvatar` frames `Resources/Story/Art/avatar_leo_*` | D2: `AvatarEvents` drives them in Task 4.7 | They are no longer triggered by `StoryLevelHud.React` once the gate is on. |

## D. Non-asset leftovers (informational)

These PlayerPrefs keys are no longer read: `StoryVfx.Rocket`, `.Pop`, `.Shatter`, `.Blast`, `.Finish`, `.Bomb`, `.ColorBomb`, `.OriginalBoosters`, `.OriginalTnt`, `.OriginalColorBall`, `.OriginalMotion`, `.Variant`, and `StoryVfxLab.Requested`. They stay on players' devices, which is harmless.

## Open points

- For `Assets/RoyalRocketVFX` and `Assets/RoyalTNTVFX`, "0 users" rests on two searches:
  - a GUID search of `.unity/.prefab/.mat/.asset/.controller/.anim` on main and on `origin/royal-throne`;
  - a name search for `RoyalRocketVFX`, `RocketVFX`, `RoyalTNTVFX`, `TNTExplosion*` and `RoyalVfx/` in royal-throne's `*.cs` and `*.json`.

  Name lookups made at runtime in any other way (for example a shader name built from a string) are UNVERIFIED.
- Whether `Packages/com.royalthrone.match3-vfx` should leave `Packages/manifest.json` / `packages-lock.json` is UNVERIFIED and out of scope (`Packages/` is read-only for this phase).
