# rfv387 booster evidence and focused import

Source is the user's unchanged `Design/Backup/apk/royal_thron_rfv387.apk`, 211,664,697 bytes, SHA-256 `a2cbee4ecf021ee464dc6f17766ba6c0b2addb2d65007f611e5c368532aa8151`. The existing unpacked `datapack.unity3d` was compared byte-for-byte by SHA-256 with the APK entry and matches. `resources.assets` SHA-256 is `d69250c7e8619489a0babcf80ee082d4b3449f50666d59e9959cedf4894ebf3c`. Packaged Unity version: 2021.3.55f1. Package identity/version was established in the earlier `Design/Inspection-rfv387/REPORT.md`: Royal Match, `com.dreamgames.royalmatch`, 38174. The filename itself is not the app identity.

The audit reads the existing unpack and existing local UnityPy tools. It does not run reference code. No live reference playback was performed by this audit owner. Runtime scheduling claims below are explicitly limited to recoverable data.

## Clips and controllers

All **13** listed clips have identical decoded curve times, values, tangents, sprite-frame names, frame rate, loop flags, and unresolved bindings to the older `NativeVisuals.json` import. Source object IDs differ between APK versions. All are 60 fps; all source controller states have speed 1.0; all clips have zero AnimationEvents. `Clips/`, `clip-summary.json`, and `controllers.json` retain exact values and source IDs.

| Clip | rfv387 ID | Older imported ID | Duration (seconds) | Loop |
| --- | ---: | ---: | ---: | --- |
| HorizontalRocketCreationAnimation | 17025 | 15098 | 0.700000 | no |
| VerticalRocketCreationAnimation | 17026 | 15099 | 0.700000 | no |
| TntCreationAnimation | 17028 | 15101 | 1.450000 | no |
| TntBoosterAnimation | 17027 | 15100 | 1.450000 | no |
| TntLightballCreationAnimation | 17029 | 15102 | 0.500000 | no |
| TntTntComboExplodeAnimation | 17030 | 15103 | 1.583333 | no |
| PropellerCreationAnimation | 17018 | 15091 | 1.316667 | no |
| PropellerFlyAnimation | 17021 | 15094 | 0.150000 | yes |
| PropellerTapStartFlyAnimation | 17024 | 15097 | 0.066667 | no |
| PropellerSwipeStartFlyAnimation | 17023 | 15096 | 0.150000 | yes |
| PropellerStartFallAnimation | 17022 | 15095 | 0.100000 | no |
| PropellerEndFallAnimation | 17020 | 15093 | 0.550000 | no |
| PropellerEndFallAltAnimation | 17019 | 15092 | 0.550000 | no |

Controllers: Rocket 18168, TNT 18169, TNT+TNT 18170, Propeller 18167. Propeller TapStart and SwipeStart transition to Fly at normalized exit time 1 with zero transition duration/offset. The source TapStart active-object binding with path hash 3985679667 remains unresolved in the packaged hierarchy, exactly as in the old import; no substitute child is invented.

Clip lengths do not establish code-driven playback speed, when a gameplay owner interrupts a clip, when a hit is applied, or how long a moving actor travels. The original native gameplay code is compiled IL2CPP.

## Rocket launch

Ordinary Rocket has only the two creation states. `FireworksRocket*` clips belong to a different item and are not replacements for ordinary Rocket launch.

The source prefab is GameObject **56064**, root Transform **62783**. `Root` is Transform **79471**. Its separate `Part1` and `Part2` children are **77585** and **72911**, each with a SpriteRenderer. The creation hierarchy is `Root/View/Animator` Transform **86178** and is separate from those moving halves.

The current runtime catalog already maps the moving halves correctly. These are the `*_rocket_part_*` sprites, not the similarly named creation-rig frames:

| Catalog key | Existing sprite ID | rfv387 source ID | Source suffix |
| --- | ---: | ---: | --- |
| rocket-left | 16907 | 18921 | horizontal_rocket_part_left |
| rocket-right | 21917 | 24450 | horizontal_rocket_part_right |
| rocket-up | 16348 | 18362 | vertical_rocket_part_top |
| rocket-down | 21611 | 24144 | vertical_rocket_part_bottom |

`RocketItemParticles` GameObject **37221** has four systems, each nonlooping, duration 10 seconds, start delay zero: `fire_bg` **99844** (lifetime .45), `add_fire` **100941** (.35), `add_fire_core` **104070** (.35), `sparks` **101504** (.30). These settings and hierarchy match the currently imported older source. A 10-second particle-system duration is not a 10-second Rocket travel time. The native owner moves the two parts and controls effect lifetime.

Metadata literals: `RocketItemView.RocketSpeed = 13.5`, `RocketAcceleration = .18`. Horizontal/Vertical strategies have `elapsedTime` and `waitDuration`, with no stored literal defaults. Their methods include `MoveToGlobalAndDestroy`, `ShouldCallExplode`, and horizontal `TryUnfreezeAll`. Units/formula and actual wait duration are not established by these symbols.

## TNT activation and TNT+TNT

TNT+TNT prefab GameObject **54127**, Transform **86982**, contains `Root/View/Animator` **72954** with `GovdeUst`, `GovdeAlt`, `GovdeGolge`, `Fitil`, `FitilGolge`, `FitilMask`, `FitilPartikul`, `TntKemer`, and `TntKemerGolge`. The fuse attachment Transform `Animator/FitilPartikul` is **67706**.

The 1.583333-second combo clip enlarges and wobbles its body, reaching approximately 2.7 times its initial width. It contains **no** whole-view hide/active curve, sprite switch, or detonation event. Only two alpha tracks exist: fuse shadow `FitilGolge` fades from 1 at .516667 to 0 at .85; body shadow `GovdeGolge` remains partly visible. Thus clip data alone cannot establish the exact gameplay detonation time. `TntTntComboView` exposes `StartFuseParticles`, `IsAnimationPlaying`, `PlayParticles`, and an `onAnimationCompleted` field: its native owner controls completion.

Verified metadata: normal TNT `DefaultExplodeRadius = 2`; default blast/smoke/glare counts 25/15/2, reduced counts 5/3/1. `TntExplosionConfig` has fields `explodeRadius`, `ringExplodeInterval`, `tntExplodeDelay`, `shakeConfig`, but their non-literal runtime values are **not recovered**. No exact combo radius or ring delay is asserted here.

### Recorded Level 3 chain activation

The existing same-version reference recording `Design/RoyalMatchReference/recordings/journey_008_030132.mp4` was inspected in a bounded window around its first TNT blast. This is a **Rocket-triggered TNT chain**, not a clean TNT tap: the adjacent horizontal Rocket visibly splits before the TNT flash. The 30 fps sample sheet `journey008-tnt-1176-1182667.jpg` begins at recording time 117.600 seconds, proceeds left-to-right/top-to-bottom, and contains 20 frames at 1/30-second intervals. Visible stages are:

- Sample 5 (about 117.767): Rocket halves separate while the TNT body is still visible.
- Sample 6 (about 117.800): TNT body is replaced by a bright flash at its cell.
- Samples 8–9 (about 117.867–117.900): blast expands and multiple nearby boxes emit debris.
- Samples 12–19 (about 118.000–118.233): debris and smoke persist while the horizontal Rocket continues across the row.

Times are approximate recording timestamps, not native gameplay-clock measurements. The reference emulator recording repeats frames, and its documented CPU-limited pacing differs from device playback. This sequence supports a fast chain-trigger response and expanding blast presentation; it does not establish the isolated TNT tap anticipation, exact hit-ring interval, or whether every visible debris frame corresponds to a distinct logical hit. No 0.12-second normal-TNT delay is asserted as verified source behavior.

## Propeller behavior evidence

The creation, tap/swipe launch, looped flying and fall/landing clips above are exact recovered data. Metadata literals for `DefaultPropellerPilot`: `OutDuration=.75`, `OutLimit=.7`, `MaxScale=1.5`, `MinOutDegree=20`, `MaxOutDegree=100`, `StartOutSpeedConst=.2`, `GoRatio=.7`, `GoStartSpeed=0`, `DownLimit=.3`, `MaxDeltaTime=.5`, `ForceExplodeFrameCount=45`. Getters for further flight speeds and minimum fly time exist but their bodies are not recovered here. These literals do not on their own define an equivalent trajectory.

`PropellerPropellerComboPilot.ComboExpandDistance=1`. `PropellerSpecialComboPilot.ComboExpandDistance=1.3`, root speed limits .5/4, root scale limits .9/1.1. `PropellerRocketStrategy.TargetExplodeDelay=.07`; Propeller+Rocket and TNT+Propeller combo `ExtraComboDelay=.02`. Full type/field/method evidence and literal data offsets are in `booster-metadata.json` and `booster-constants.json`.

## VFX and dependencies

All ten previously imported requested particle hierarchies are normalized-data-identical to the newer source, including ordering and duplicate-named nodes: RocketItemParticles, TntItemExplodeParticles, TntItemFuseParticles, PropellerItemExplodeParticles, PropellerTargetExplodeParticles, TntTntComboExplodeParticles, TntRocketComboParticles, PropellerRocketComboParticles, TntPropellerComboFuseParticles, TntRocketComboUseParticles.

The missing hierarchy is **TntTntComboFuseParticles, GameObject 51481: seven objects, six particle systems**. All six loop. The root `sparks` emitter has emission disabled; five children provide glow, colored/white sparks and spark lines. Owner code must stop it at detonation/cancellation/recycle. It uses built-in `Particles/Standard Unlit` and `Mobile/Particles/Additive`; no new shader is needed.

The dependency audit compared decoded source pixels, not just names. Of 119 referenced sprites, 22 are pixel-identical and 97 differ; all sizes match. Most changed booster sprites have small channel deltas (recorded per sprite) and should not be described as identical artwork. The fuse's `additive_round_02` and `additive_round_01` exactly match existing sprite IDs16582/21650. Its `Effects-color_particle` has identical RGB and alpha differs by one on four pixels; an exact new 122×122 extraction is provided rather than claiming byte equality.

Eleven selected audio cues were compared by encoded source payload; four match exactly, seven differ. `audio-comparison.json` records this. Different encoded bytes alone do not establish an audible change; no new audio is imported in this focused task.

## Focused import artifact

`prepare_missing_fuse.py` produces `TntTntComboFuseParticles-import.json` and the single new exact sprite PNG under `Assets/ReferenceGameplay/VFX/Textures`. `ReferenceRfv387FuseImporter.Build()` invokes the established VFX importer for this one hierarchy. The importer has a backward-compatible optional reused-reference overload, so existing materials and sprites are loaded directly and are not regenerated. It verifies the reused files and their `.meta` hashes remain unchanged.

Run the Unity menu **Royal Throne / Reference Gameplay / Import rfv387 TNT Combo Fuse Only**, or invoke `ReferenceRfv387FuseImporter.Build()`. It creates `Resources/ReferenceGameplay/VFX/TntTntComboFuseParticles.prefab`, validates seven objects/six systems, shader support and nonnull sprite dependencies, and writes `missing-fuse-import-result.json` with the actual imported module dump. That result, not the existence of this preparation script, is the evidence that Unity import passed.

No entire media pack, existing animation clip, runtime booster logic, player progression, scene, or imported source GUID is regenerated by this entry point. No reference MonoBehaviour code is imported. The existing importer retains its documented sorting-layer remapping and owner-managed ParticleSystem stop-action compatibility adjustments.
