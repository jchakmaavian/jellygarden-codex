# Royal Throne level port: provenance

Plan Task 1.1 steps 2–3, under decision D1 at its default:
- APK-derived assets may be used **only as internal placeholders**.
- The release gate in Task 6.4 (`tools/check_provenance.py`) reads this file. It fails any store build that still contains a path from the `APK-DERIVED` block below.

The paths are the ones in `docs/port/import-paths.txt`. Sizes and per-file detail are in `docs/port/IMPORT-MANIFEST.md` and `docs/port/import-closure.json`. The ref is `origin/royal-throne` `1164c261`. Written 2026-10-12.

**Source categories.**
- **APK-derived**: extracted, decoded or recovered from the Royal Match Android package `royal_thron_rfv387.apk`. Its SHA-256 is `a2cbee4e…8151`, recorded in `Design/KingVanguard-Reference-20260929/SOURCE-VERIFICATION.md`.
- **royal-throne-authored**: made in the royal-throne project. This covers code, procedural or generated art, and generated audio.
- **kit**: the JellyGarden Match-3 kit at `a82c4df`.
- **third-party**: an external package or font under its own licence.
- **UNVERIFIED**: no provenance record was found. The release gate should treat these as APK-derived until the owner confirms otherwise.

## 1. Evidence sources

1. `Design/ReferenceGameplayImport/INTEGRATION.md` on `origin/royal-throne`:
   - "Source APK retained unchanged."
   - "Imported assets: 1,243 exact sprite crops; 7 shared source atlases; 38 prefabs (6 booster hierarchies, 6 blocker composites, 26 VFX hierarchies); 18 editable clips and 6 controllers; 44 gameplay WAVs."
   - Its "Runtime connections" table maps those assets onto `ReferenceGameplayCatalog`, `ReferencePieceVisual`, `ReferenceActivationVisual`, `ReferenceVfxPool` and the "recovered PCM clips".
2. `Design/ReferenceGameplayImport/SOURCE-PACKAGE.md`: "the imported gameplay assets, modified adapters … level-source evidence". The original implementation commit is `cdd68361`.
3. `Design/KingVanguard-Reference-20260929/SOURCE-VERIFICATION.md`:
   - The APK's identity and hash.
   - The King Spine rigs (`AreaCompleted.skel`, `King.skel`, `nightmare_popup.skel`) and KingDrill, which are "Recovered" from the APK's Unity packs and bundles.
   - It covers `Assets/Characters/KingVenguard/**`, which is **not imported** (D2).
4. Code comments that cite the APK. The authored code says it plays APK data:

| File:line (royal-throne) | Comment |
|---|---|
| `Assets/ReferenceGameplay/Scripts/ReferenceGameplayAudio.cs:8` | "Original PCM decoded from the APK." |
| `Assets/ReferenceGameplay/Scripts/ReferencePieceVisual.cs:9` | "Every curve and child transform in these prefabs comes from the packaged data." |
| `Assets/ReferenceGameplay/Scripts/ReferenceVfxPool.cs:8` | "Native particle modules stay on their prefabs." |
| `Assets/ReferenceGameplay/Scripts/ReferenceGameplayCatalog.cs:9` | "Board-only native asset adapter." |
| `Assets/ReferenceGameplay/Tools/Scripts/ReferenceBoosterBar.cs:16` | "Native icon artwork + verified packaged tool descriptions. The APK's IL2CPP …" |
| `Assets/JellyGarden/Scripts/GUI/RoyalGameplayPresentation.cs:49` | "APK level2 UI sprites are authored as mirrored halves." |
| `Assets/JellyGarden/Scripts/Juice/RoyalTntNativeSequence.cs:12` | "The APK supplies a dedicated body/roof/fuse animation for the double TNT." |
| `Assets/JellyGarden/Scripts/Juice/RoyalRocketTntCombo.cs:18` | plays `rocket_tnt_combo__rfv387_17431` |
| `Assets/Boosters/Propeller/V5/Scripts/PropellerVisualV5.cs:127` | plays `propeller_tnt_combo__rfv387_17641` |
| `Assets/ReferenceGameplay/Tools/Scripts/ReferenceCannonSequence.cs:38,113` | `CannonBallRfv387`, `CannonFuseRfv387` |
| `Assets/JellyGarden/Scripts/Juice/RoyalPowerupDirector.cs:334` | "the APK's tie breaker is unverified" |
| `Assets/Boosters/Propeller/V5/Scripts/PropellerGameplayV5.cs:14` | "Reconstructed objective-first selection. The APK exposes separate combo pilots …" |

5. Name markers: `rfv387` / `Rfv387` in file names (for example `VFX/Textures/rfv387-*.png`, `VFX/Resources/ReferenceGameplay/VFX/*Rfv387.prefab`, `Audio/*__rfv387_*.wav`) and the numeric `__<pathId>` suffixes. These match `sourcePathId` in `runtime-catalog.json`.
6. Per-folder provenance files:
   - `Assets/Boosters/Propeller/V5/README.md` and `Audio/provenance.json`;
   - `Assets/Boosters/ColorBall/V4/README.md` and `Audio/PROVENANCE.md`;
   - `Assets/Boosters/ColorBall/V5/README.md`;
   - `Design/GameplayArt-v4/README.md` and `asset-review.md` (for `RoyalGameplayV4`).
7. The font's own `name` table, and blob identity against the kit (§4).

## 2. Per path or folder

| Path (on royal-throne; `/` = folder) | Source | Evidence |
|---|---|---|
| `Assets/ReferenceGameplay/Art/` (`Resources/ReferenceGameplay/Sprites/*`, `Atlases/*`) | **APK-derived** | INTEGRATION.md "1,243 exact sprite crops; 7 shared source atlases". `__<pathId>` names = `sourcePathId`. `ReferenceSpriteImporter.cs` "Recovered Sprite metadata" |
| `Assets/ReferenceGameplay/Animation/` (`Clips/`, `Controllers/`, `Resources/ReferenceGameplay/Visuals/`) | **APK-derived** | INTEGRATION.md "18 editable clips and 6 controllers", "Rocket, TNT, Propeller and Color Ball render hierarchies/creation clips". `ReferencePieceVisual.cs:9` |
| `Assets/ReferenceGameplay/VFX/` (`Resources/`, `AtlasSprites/`, `Materials/`, `Textures/`) | **APK-derived** | INTEGRATION.md "26 VFX hierarchies … 189 ParticleSystems". `*Rfv387` prefabs. `rfv387-*` textures |
| `Assets/ReferenceGameplay/Audio/` | **APK-derived** | INTEGRATION.md "44 gameplay WAVs". `ReferenceGameplayAudio.cs:8`. `__rfv387_` names |
| `Assets/ReferenceGameplay/Layout/` | **APK-derived** | `RoyalGameplayPresentation.cs:49` "APK level2 UI sprites" |
| `Assets/ReferenceGameplay/Tools/Resources/` (tool icons) | **APK-derived** | `ReferenceBoosterBar.cs:16` "Native icon artwork" |
| `Assets/ReferenceGameplay/Tools/Hammer/` | **APK-derived** | `HammerSource.json` (native clip curves). Built by `ReferenceHammerImporter.cs:29` from `Design/Hammer-Reference/NativeHammerParticles.json`. `HammerImpactRfv387` |
| `Assets/ReferenceGameplay/Tools/Archer/` | **APK-derived** | `ArcherSource.json`. `ReferenceArcherImporter.cs:78` from `Design/Archer-Reference/NativeArcherParticles.json` |
| `Assets/ReferenceGameplay/Tools/Cannon/` | **APK-derived** | Spine export `export_anim_cannon.{skel.bytes,atlas.txt,png}`. `ReferenceCannonImporter.cs:33` "The packaged material uses premultiplied alpha", `:40` `Design/Cannon-Reference/NativeCannonParticles.json` |
| `Assets/ReferenceGameplay/Resources/ReferenceGameplay/runtime-catalog.json` | **APK-derived** (index of APK sprites) | each entry has `sourceName` and `sourcePathId` |
| `Assets/ReferenceGameplay/Scripts/`, `Assets/ReferenceGameplay/Tools/Scripts/`, `Assets/ReferenceGameplay/Home/Scripts/ReferenceWinFinish.cs`, `Assets/ReferenceGameplay/Editor/*.cs` | royal-throne-authored (adapters that play APK data) | INTEGRATION.md "modified adapters". Behaviour is "reconstructed", not copied: INTEGRATION.md "Incomplete reference equivalence" and `PropellerGameplayV5.cs:14`. Code only; holds no APK data |
| `Assets/JellyGarden/Scripts/Juice/`, `Assets/JellyGarden/Scripts/GUI/Royal*.cs`, `RoyalSave.cs`, `RoyalQaService.cs`, `RoyalOfflineMode.cs`, `Assets/JellyGarden/Scripts/Editor/Royal*.cs` | royal-throne-authored | royal-throne first commit `3fc0aeb8` (snapshot of `jchakmaavian/jellygarden-codex`, plan Fact 1). Comments in §1 item 4 show that some files *play* APK assets; the code itself is authored |
| `Assets/Boosters/Propeller/V5/` | royal-throne-authored. Audio is AI-generated | README:51 "Three original sounds were generated for this assignment with ElevenLabs through Magnific. No Royal Match audio was copied or extracted." README:73 "new runtime, editor checks, generated meshes, prefabs, materials, shaders, SFX". At runtime it also drives the APK `ReferenceGameplay/Visuals/Propeller` prefab (`PropellerVisualV5.cs:74`) |
| `Assets/Boosters/ColorBall/V4/` | royal-throne-authored. Audio is a project original | README:59-60 "The same seven original project sounds as V3 … No Royal Match audio was copied or extracted". `Audio/PROVENANCE.md` "exact copies of the original Royal Throne Color Ball V2 sounds, created for this project". The visual target was a Royal Match video (README:10), used as reference only. At runtime it drives the APK `Visuals/ColorBall` prefab (`ColorBallVisualV4.cs:72`) |
| `Assets/Boosters/ColorBall/V5/` | royal-throne-authored | README: "Both prefabs are native variants of V4"; "original V4 cues are reused byte-for-byte" |
| `Assets/JellyGarden/Shaders/{CandyFragment,JuiceAdditive,RoyalMatchFragment,RoyalParticle,RoyalRocketFire}.shader` | royal-throne-authored | `Shader "JellyGarden/CandyFragment"`, `"JellyGarden/JuiceAdditive"`, `"JellyGarden/RoyalMatchFragment"`, `"RoyalThrone/Particle Alpha"`, `"RoyalThrone/RocketFire"`. None of them is in the kit at `a82c4df` or on main. No external source is cited, so the authorship itself is **UNVERIFIED** (no provenance doc) |
| `Assets/JellyGarden/Resources/{CandyFragment,JuiceAdditive,RoyalMatchFragment}.mat`, `JuiceConfig.asset`, `RoyalRocketFX/`, `RoyalColorBall/*.shader` | royal-throne-authored | materials and settings for the shaders above; scripts' own ScriptableObjects |
| `Assets/JellyGarden/Resources/RoyalGameplay/{EffectsProfile.asset,EffectsRig.prefab,*.mat,*Shape.asset}` | royal-throne-authored | settings and particle rig for `RoyalPowerupDirector` / `RoyalEffectsProfile`. **UNVERIFIED**: `EffectsRig.prefab` (704 KB) textures were not traced to a source |
| `Assets/JellyGarden/Resources/RoyalGameplay/Toolkit/` | **third-party-derived** (AllIn1VfxToolkit) | `Toolkit/Fire Impact.prefab` and `Ice Impact.prefab` are prefab variants of `Assets/Plugins/AllIn1VfxToolkit/Demo & Assets/Demo/Prefabs/Fire Impact.prefab` (source guid `240c830bfe588ad4f9925288b65d7a18`). `Toolkit/Materials/*.mat` share their names with the AllIn1 demo materials (FireSparks2, IceFlares, Impact*, RedFaintGlow, SnowFlakes) and use AllIn1 demo textures |
| `Assets/JellyGarden/Resources/RoyalGameplay/Audio/{celebrate,click,create,firework,helicopter,land,match-a,match-b,rocket,tnt}.wav` | **UNVERIFIED** | no provenance record found on royal-throne (`Design/Audio/` documents only the win fanfare and level music, which are not imported) |
| `Assets/JellyGarden/Resources/RoyalGameplay/*.webp` (19 loose sprites: chains, chest, crate, crown, gem, hammer, heart, helicopter, ice, key, leaf, lily, ruby, shield, smoke, star, stone, tnt, vase) | **UNVERIFIED** | no per-file record. Later sprites in the same folder are documented as generated in-project (e.g. `Design/Cards/*`: win-cushion "generated with the built-in image tool"), but those are not imported |
| `Assets/JellyGarden/Resources/RoyalGameplayV4/` | royal-throne-supplied art pack. **UNVERIFIED** generation history | `Design/GameplayArt-v4/README.md`: "The supplied Royal Throne gameplay pack …". `asset-review.md`: "the 512px exports were normalized/upscaled from approximately 180–256px generated atlas objects", and README: "supplied provenance, not independently verified generation history" |
| `Assets/JellyGarden/Resources/RoyalGameplay/Display.ttf` | **third-party** (font), also **kit** | §4 below |
| `Assets/Spine/` | **third-party** | spine-unity 3.8, Esoteric Software. `Assets/Spine/LICENSE.txt`, `package.json` (`com.esotericsoftware.spine.spine-unity` 3.8.0), `UNITY6-COMPAT-PATCHES.md` (commit `8b4844bd`, 6000.5 patches) |
| `Assets/Plugins/AllIn1VfxToolkit/Demo & Assets/…` (31 files, IMPORT-MANIFEST §1) | **third-party** | AllIn1VfxToolkit demo content (Unity Asset Store). Main already ships the toolkit with `README.txt` and `Documentation.pdf` |
| Kit-core files (`LevelManager.cs`, `Item.cs`, `Square.cs`, `AI.cs`, `InputManager.cs`, `System/CombineManager.cs`, `Targets/*`, `GUI/BoostShop.cs`, `GUI/BoostIcon.cs`, …) | **kit** + royal-throne-authored edits | present at `a82c4df`; royal-throne diffs in `scratchpad/diffs/*.diff` / `git diff a82c4df origin/royal-throne` |

**Not imported, for the record.** These are excluded by D2, D4 or D8, or cut as fallback or dead content:
- `Assets/Characters/KingVenguard/**`: APK-derived, per SOURCE-VERIFICATION.md.
- `Resources/RoyalTNT/**` and `Resources/RoyalColorBall/{Frames,VFX}/**`: **UNVERIFIED**.
- `Resources/RoyalCampaignArt/**`: **UNVERIFIED**.
- `ReferenceGameplay/{Home,Levels,WinVFX}/**`, `ReferenceGameplay/Art/Resources/ReferenceGameplay/Blockers/**`: APK-derived, per INTEGRATION.md "6 blocker composites" and "Levels 01–50 … data resource `ReferenceGameplay/levels01-50`".

**Already on main.** Main already ships 343 of the needed `Assets/ReferenceGameplay/**` asset files, byte-identical, through its `StoryOriginal*` layers (plan Fact 2). They are APK-derived under the same evidence. The release gate must cover them too.

## 3. Paths for the release gate (Task 6.4)

`tools/check_provenance.py` should read the two fenced blocks below. Each line is a path prefix on main after the port. Every line under `APK-DERIVED` fails a store build. Every line under `UNVERIFIED` fails one too, until the owner reclassifies it in this file.

```APK-DERIVED
Assets/ReferenceGameplay/Art/
Assets/ReferenceGameplay/Animation/
Assets/ReferenceGameplay/VFX/
Assets/ReferenceGameplay/WinVFX/
Assets/ReferenceGameplay/Audio/
Assets/ReferenceGameplay/Layout/
Assets/ReferenceGameplay/Tools/Resources/
Assets/ReferenceGameplay/Tools/Hammer/
Assets/ReferenceGameplay/Tools/Archer/
Assets/ReferenceGameplay/Tools/Cannon/
Assets/ReferenceGameplay/Resources/ReferenceGameplay/runtime-catalog.json
Assets/Characters/KingVenguard/
```

```UNVERIFIED
Assets/JellyGarden/Resources/RoyalGameplay/Audio/
Assets/JellyGarden/Resources/RoyalGameplay/chains.webp
Assets/JellyGarden/Resources/RoyalGameplay/chest.webp
Assets/JellyGarden/Resources/RoyalGameplay/crate.webp
Assets/JellyGarden/Resources/RoyalGameplay/crown.webp
Assets/JellyGarden/Resources/RoyalGameplay/gem.webp
Assets/JellyGarden/Resources/RoyalGameplay/hammer.webp
Assets/JellyGarden/Resources/RoyalGameplay/heart.webp
Assets/JellyGarden/Resources/RoyalGameplay/helicopter.webp
Assets/JellyGarden/Resources/RoyalGameplay/ice.webp
Assets/JellyGarden/Resources/RoyalGameplay/key.webp
Assets/JellyGarden/Resources/RoyalGameplay/leaf.webp
Assets/JellyGarden/Resources/RoyalGameplay/lily.webp
Assets/JellyGarden/Resources/RoyalGameplay/ruby.webp
Assets/JellyGarden/Resources/RoyalGameplay/shield.webp
Assets/JellyGarden/Resources/RoyalGameplay/smoke.webp
Assets/JellyGarden/Resources/RoyalGameplay/star.webp
Assets/JellyGarden/Resources/RoyalGameplay/stone.webp
Assets/JellyGarden/Resources/RoyalGameplay/tnt.webp
Assets/JellyGarden/Resources/RoyalGameplay/vase.webp
Assets/JellyGarden/Resources/RoyalGameplay/EffectsRig.prefab
Assets/JellyGarden/Resources/RoyalGameplayV4/
```

These code files load the APK-derived content. Replacement art has to be routed through them, or the release gate will keep listing it:
- `ReferenceGameplayCatalog` (pieces and powerups);
- `ReferencePieceVisual`, `ReferenceActivationVisual` and `ReferenceDualColorBallVisual` (powerup bodies and combos);
- `ReferenceVfxPool` (blasts, trails, leftover-moves finish);
- `ReferenceGameplayAudio` (gameplay SFX);
- `ReferenceBoosterBar` with `Reference{Hammer,Archer,Cannon}Sequence` (tools and their icons);
- `RoyalSpecialCreationFlash` (`ReferenceGameplay/Sprites`);
- `RoyalGameplayPresentation` and `ReferenceBoosterBar` (`ReferenceGameplay/Layout`).

Every one of these checks for the asset and falls back when it is missing, for example `ReferenceGameplayCatalog.Available` and `ReferenceVfxPool.Has`. The fallback for TNT is `Resources/RoyalTNT`, which is not imported (IMPORT-MANIFEST §1).

## 4. Third-party licences to clear before any release

| Item | Where | Licence | What it requires | Status |
|---|---|---|---|---|
| **spine-unity 3.8 runtime** | `Assets/Spine/` (needed only for the Cannon tool) | Spine Runtimes License Agreement (`Assets/Spine/LICENSE.txt`, 2019-05-01). It permits integration "under the terms and conditions of Section 2 of the Spine Editor License Agreement" | The text says: "each user of the Products must obtain their own Spine Editor license and redistribution of the Products in any form must include this license and copyright notice." **A Spine Editor licence is required**, and the licence text must ship with the product | Not cleared. The Cannon's skeleton data is separately APK-derived. Dropping the Cannon tool, or reimplementing it without Spine, removes this item |
| **AllIn1VfxToolkit** | `Assets/Plugins/AllIn1VfxToolkit/**` (main already ships it) + 31 demo files (IMPORT-MANIFEST §1) + `Resources/RoyalGameplay/Toolkit/**` (derived prefabs and materials) | Unity Asset Store EULA (per-seat licence for editor tools). **UNVERIFIED**: the exact Asset Store licence type was not checked | Every developer seat that uses the toolkit needs a licence. Redistributing demo content inside a built game is normally allowed as part of a product; **UNVERIFIED** for the demo prefabs and textures | Not cleared |
| **"BorisBlackBloxx" font** | `Assets/JellyGarden/Resources/RoyalGameplay/Display.ttf` | Licence unknown. The font's name table says `Copyright © 2003, Manfred Klein. All rights reserved.`, version `1.0 2003-10-01` | Must be cleared with the author's terms | Not cleared. **The same font is already on main.** `Display.ttf` (blob `fbc38cbaabb6acef7cd17858348f7abb63062409`, 53,152 bytes) is byte-identical to the kit's own `Assets/JellyGarden/Fonts/BorisBlackBloxx.ttf`, present at `a82c4df`, and to main's `Assets/JellyGarden/Resources/Story/Fonts/story_display.ttf`. Importing it adds no new licence exposure. Instead of a duplicate, the three `Resources.Load<Font>("RoyalGameplay/Display")` sites (`RoyalCelebration.cs:77`, `RoyalGameplayPresentation.cs:235`, `ReferenceWinFinish.cs:66`) can point at `Story/Fonts/story_display` |
| AI-generated audio | `Assets/Boosters/Propeller/V5/Audio/**` | ElevenLabs via Magnific (README:51) | Commercial use depends on the account plan that generated the sounds. **UNVERIFIED** | Not cleared |
| JellyGarden kit | kit-core files | Asset Store kit, already the base of main | — | Same status as main today |
