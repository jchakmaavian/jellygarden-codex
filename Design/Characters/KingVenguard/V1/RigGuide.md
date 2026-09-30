# King Venguard V1 - rig guide

Status: **integrated into Royal Throne on the official Spine 3.8 runtime (24 September 2026).**
See **Unity integration** below. The corrected V1 art and its two authored animations are used as they
are: nothing was redesigned, regenerated or upscaled.
- No APK was built.
- Neither the integration nor the correction pass before it spent Magnific credits.
- The first-pass state is kept in full in `_superseded/pass1_2026-09-24/`.

## Hand and ring arrangement (decided in the correction pass)

- **Signet ring:** the character's **LEFT** hand. In front views that is the viewer's right.
- **Thumbs-up:** the character's **RIGHT** hand. The right hand never wears a ring.

| Source | Ring | Thumbs-up | Status |
|---|---|---|---|
| Master hero, 3/4 (`Design/01`) | left hand | - | consistency source |
| Turnaround front / side / back (`Design/02`) | left hand in all three (the side view shows his left side) | - | consistent |
| Neutral rig pose (`Design/03`) and rig parts | `hand_L_open`, `hand_L_fist` | `hand_R_thumbsup` | consistent |
| Hand sheet (`Design/06`) | left fist | right thumbs-up | consistent |
| Action poses 2-5 (`Design/05`) | left hand | - | consistent |
| Action pose 1, as generated | two rings on the thumbs-up hand | **left** hand | **replaced** |

Generated pose 1 was the only file that broke the rule. In `Design/05_KingVenguard_Action_Poses.png`,
its `_alpha` version and `Design/cutouts/pose_1_happy_thumbs_up.png`, it is replaced by a rig render of
`happy_thumbs_up` at 2.30 s, captioned on the sheet. The generated original is unchanged in
`Source/magnific/06-action-poses.png` and in `_superseded/pass1_2026-09-24/Design/`.

A matching three-quarter-view pose 1 would need one Magnific generation (about 78 credits). It was
identified but not spent. The first-pass note that the turnaround had the ring on the other hand was
wrong; the turnaround was always consistent.

Evidence: `Design/10_KingVenguard_Hand_And_Ring_Arrangement.png` and
`Design/09_KingVenguard_Consistency_Contact_Sheet.png`.

## What else the correction pass changed

- **Rest pose matches the master.** The rule now is that wherever the setup pose shows a pixel, the
  part carries the master's pixel. Painted fills and region edits are used only where a pixel is
  hidden. This fixed several visible first-pass differences:
  - **Under the chin:** the F4 region edit had its own chin, a stubble strip and a different shirt
    collar.
  - **Coat opening:** a pale rectangle showed between the coat panels, from a hips fill seeded by the
    white studio background.
  - **Eyes:** the eyeball behind the irises was greyish-pink and the lash line was thinned.
  - **Brows:** their soft shadow was painted out.
  - **Skirt:** it had pinholes and was missing the dark underside of the hem.
  - **Right fist swap:** it had blotchy crease speckles from colour matching.
- **Joint coverage:**
  - The breeches now continue up to the hip line under the coat skirt; the first pass opened gaps at
    a 3-degree skirt twist.
  - **Hidden upper arm:** it is now a capsule along the shoulder-elbow axis under the cape, and it
    continues 16 px behind the belly's side edge.
    - The first pass followed a hand-drawn outline, which poked out above the cape as a dark tab
      during the thumbs-up lift.
    - It also opened a light hairline between arm and belly when the arm swung 2-3 degrees.
  - Near the shoulder, the hidden arm keeps only pixels that stay inside the silhouette over ±24
    degrees of arm turn.
  - The cape's light fabric edge under its gold trim now belongs to the cape instead of riding away
    with the arm.
  - The part drawn under each elbow and knee owns the wedge outside the bend, and reaches 12 px
    (elbow) and 8 px (knee) under its joint partner.
  - The sad and worried mouths no longer show the stubble Magnific painted under the mustache.
- **New facial parts, from existing art only:**
  - Worried and sad brows.
  - Wide-eyed upper lids with a matching iris clip.
  - Cheek bones for the laugh's cheek and whisker response.
- **Previews:** the two approval previews were re-rendered from the corrected rig and labelled
  "Custom-player preview — official Spine compatibility untested." The first-pass videos are in
  `Previews/superseded/`.

## Format and intended Spine target

- **Data:** Spine **3.8** JSON (`"spine": "3.8.99"`) with a libGDX-format text atlas, in the same
  format family as the inspected reference (Spine 3.8.84). It is 2D skeletal; there is no 3D pipeline.
- **Attachments:** only **region attachments** (48) and **clipping** attachments (4). There are no
  meshes, weights, deform keys, IK, constraints, events, paths or draw-order keys.
- **Changes:** expressions and hands change by attachment swaps. The limited facial response moves
  and scales dedicated bones.
- **Intended target:** Spine Editor 3.8.x import (`King_Venguard.json` with `images/`) and a Spine
  3.8 runtime.
- **Units:** 1 unit = 1 pixel of `Design/03_KingVenguard_Neutral_Rig_Pose.png` (1696 x 2528). The root
  sits at ground centre, pixel (848, 2320), with Y up.
- **Size:** about 2,135 units from crown tip to soles. For a 360 px tall character use a skeleton
  scale of about 0.17.
- **View:** front view only. The side and back turnaround is a design reference, not a rig view.

### Compatibility checks (Unity integration pass, 2026-09-24)

The rig now runs on the **official** Spine 3.8 runtime: spine-csharp and spine-unity from
EsotericSoftware/spine-runtimes branch 3.8, commit `8b4844bd4b193ba9e54487ed397a777993cbad56`. The JSON
version string is unchanged (`3.8.99`).

1. **Official loader - passed after one data fix.** spine-csharp 3.8 (`SkeletonJson`) threw
   `IndexOutOfRangeException` in `SetStepped` on `range_test`. A 3.8 timeline stores one curve fewer than
   it has keys, so the **last key must not carry `"curve"`**. `build_rig.py` now writes stepped keys only
   on non-last keys. No other data changed.
2. **Pose sampling - passed.** The official runtime and the custom player were compared at every preview
   frame:
   - `idle` matches within 0.03 units.
   - `happy_thumbs_up` matches within 4.05 units, with identical attachments at every frame.
   - The residual is official 3.8's 10-segment linear Bezier approximation. It peaks on the fast right
     hand at 1.47 s.
3. **Clipping and attachment timelines - passed.** The `eyeclip_*` swaps and every `hand_R` / eyelid /
   brow / mouth swap land on the same frames as in the custom player.
4. **Atlas alpha - handled.** The pages are straight alpha. Every Spine material and the UI material
   use `_StraightAlphaInput`, and the texture imports as Default with `alphaIsTransparency`, no mipmaps,
   uncompressed and Clamp.
5. **Unity 6000.5.10f1 - runs with 16 compile-only patches.** Unity 6.5 turned the int instance-ID API
   into compile errors (CS0619). The patches are listed in `Assets/Spine/UNITY6-COMPAT-PATCHES.md`.
   Each one is behind `#if UNITY_6000_5_OR_NEWER` with the original kept in `#else`. None touches
   animation, mesh generation or rendering.
6. **Mixing - set.** The idle<->happy mix is 0.2 s on the SkeletonDataAsset. `happy_thumbs_up` starts
   and ends on idle's first pose.
7. **Unity rendering - passed.** SkeletonGraphic renders were compared with the custom player at 22
   times across both animations, with the same framing:
   - Mean difference 2.4-3.0 levels inside the character.
   - More than 80 levels only at 1.47 s, 3.20 s and 3.50 s (908, 81 and 19 px), from the Bezier
     approximation above.
8. **Still untested:**
   - Spine Editor import. No editor was installed; the data was never opened in it.
   - Readability on a device. Android was out of scope for this pass.

## Unity integration (Royal Throne)

**Runtime:**
- **Source:** official spine-unity 3.8 and spine-csharp 3.8, embedded in `Assets/Spine/`.
- **Licence:** the Spine Runtimes License, which requires a Spine Editor licence; the project owner
  confirmed one.
- **Unchanged:** the Unity version, `Packages/manifest.json` and every other package.
- **Editor defaults:** `Assets/Editor/SpineSettings.asset` sets scale 0.01, mix 0.2 s and shader
  `Spine/Skeleton`.

**Runtime content**, all in `Assets/Characters/KingVenguard/V1/`. Source art, the PSD, generation logs and
superseded versions stay here in `Design/`.

| Path | Contents |
|---|---|
| `Spine/King_Venguard.json` | Byte-identical to `RigReady/spine/King_Venguard.json`. |
| `Spine/King_Venguard.atlas.txt`, `King_Venguard.png` | The single-page runtime export from `RigReady/spine-unity/`. |
| `Spine/King_Venguard_Atlas.asset`, `_Material.mat`, `_SkeletonData.asset` | Generated by spine-unity. The data asset carries the 0.2 s idle<->happy mix. |
| `Materials/KingVenguard_SkeletonGraphic_StraightAlpha.mat` | UI material: SkeletonGraphicDefault with straight-alpha input. |
| `Resources/KingVenguard/KingVenguard.prefab` | The one reusable prefab: a root with `KingVenguardCharacter` and a `Skeleton` child with SkeletonGraphic. |
| `Scripts/KingVenguardCharacter.cs` | Plays `idle` and `happy_thumbs_up` on track 0. Frames the rig in a slot with one uniform scale. |
| `Scripts/KingVenguardSlot.cs` | Puts the prefab into an existing king image slot, keeping that slot's placement and its old sprite. |
| `Editor/KingVenguardPrefabBuilder.cs` | Rebuilds the prefab (menu **Royal Throne > King Venguard > Rebuild prefab**). |
| `Editor/KingVenguardRuntimeCheck.cs` | Renders official-runtime frames for comparison with the previews. |

**Why a single atlas page at texture scale 0.9:**
- **The bug:** spine-unity 3.8 draws a multi-page atlas through one child RawImage per page. uGUI can
  overwrite a freshly enabled child with a white quad for a frame; this is fixed only in 4.0. In the
  first Unity renders, idle at 0.00 s was blank and happy at 2.00 s lost the crown and mouth.
- **The fix:** one page keeps SkeletonGraphic on its single-renderer path.
- **The cost:** attachment sizes are unchanged; only texel density drops by 10%. Even the largest slot
  draws about 0.27 screen px per Spine unit on a 1080 px wide screen, so the page is still more than
  3x denser than any slot needs.

**Prefab settings:**
- Starts `idle`, looping, with a single canvas renderer.
- `unscaledTime` keeps the authored timing when gameplay changes `Time.timeScale`.
- `raycastTarget` is off, so the king never blocks input.
- Maskable.

**Connections.** All are existing king slots. Each is mounted at runtime by the code that already
builds the slot, and no scene file was changed.

| Slot | Built in | Framing and mask | Plays |
|---|---|---|---|
| HUD portrait `King reactions` | `RoyalGameplayPresentation.Build` | Bust, stencil `Mask` in the King-Frame arch, under the frame edge | idle; happy once per positive reaction |
| Home profile portrait | `RoyalHomeScreen.InitializeRuntime`, after `ReferenceHomeLayout` | Avatar (head), `RectMask2D` | idle |
| Home dialog icon (profile and story panels) | `RoyalHomeScreen.ShowRoyalCompanions` | Bust, `RectMask2D` | idle; the other panels get their own icons back |
| Win card `Royal king` | `RoyalMenuSkin` (MenuComplete) | Bust, `RectMask2D` | happy once each time the card opens, then idle |
| Fail card `Royal king` | `RoyalMenuSkin` (MenuFailed) | Bust, `RectMask2D` | neutral idle |

Each previous king sprite stays assigned to its image and is only hidden, so it is recoverable. If the
prefab cannot load, the old sprite stays visible and one error is logged. Mounting is idempotent per slot.

**Animation mapping:**
- **Idle:** the authored `idle` (4.0 s), looping. This is not the 8 s preview recording.
- **Happy:** `happy_thumbs_up` (3.8 s) once, with the thumbs-up at the authored 1.90 s. `idle` is
  queued on the same track with `AddAnimation(..., delay 0)`, which Spine schedules at the animation's
  end minus the mix. Idle therefore starts at 3.6 s and blends in over 0.2 s, while happy finishes its
  return to idle's first pose underneath.
- **Trigger:** the existing positive king reaction, `RoyalPowerupDirector.Reaction`, which power-up
  creation and activation raise.
  - It passes through the HUD's existing `excitement > 0.5` threshold, which used to show the cheer
    sprite.
  - Happy fires only on the rising edge, and `PlayHappy` is ignored while a reaction is still playing.
    A cascade or combo chain therefore gives one reaction, and no track overlaps.
- **Leaving a screen:** a slot that is disabled mid-reaction resets to idle, so a reopened card never
  resumes an old reaction.
- **Missing mappings (neutral idle; nothing was fabricated):**
  - The HUD "think" state (5 moves or fewer). The old sprite showed a thinking king.
  - The fail card. The old sprite showed a thinking king.
  - Any sad or worried reaction.

**Framing:** `KingVenguardCharacter.Frame` fits a focus box into the slot with one uniform scale, top
aligned and centred. The boxes are Bust (crown to chest), Avatar (head) and FullBody. The RectTransform
covers the focus box, with the skeleton root at the pivot (spine-unity's own bounds convention), so
`RectMask2D` culls by what is actually drawn. Before this fix, a rect centred on the feet made
`RectMask2D` cull the whole bust in the home portrait. Frames follow a slot's size, so the portrait
re-framed itself when the home layout later changed.

### Play Mode verification (Unity 6000.5.10f1 editor, 1080 x 1920 Game view, 24 September 2026)

Level checks ran inside the game's isolated QA test profile (`RoyalQaService`). Every runtime
`PlayerPrefs` call goes through `RoyalSave`, so wins, losses and moves wrote only QA keys. The session
end restored the real profile (level 23). A per-frame log of each king's track 0 (animation, track
time, mix, track count) backs the timings below. Stills are in `Previews/InGame/`.

| Check | Result |
|---|---|
| Real locations | HUD arch portrait in level 12, home profile portrait, profile dialog, win card and fail card all show King Venguard. The old sprites are hidden. |
| Idle loop | Idle ran without a break on a single track: 115 s in the HUD and 8 min in the home portrait. The cards idle for as long as they stay open. |
| Happy once, back to idle | Win card: `happy_thumbs_up` from 0.00 to 3.63 s, then `idle` mixing in from happy. HUD: a real rocket activation raised the existing reaction to 0.77, and happy played once (0.00 to 3.63 s, thumbs-up at 1.90 s), then idle mixed in. This is the recorded clip. |
| Repeated reactions | Win card: the second opening restarts happy at 0.00, because the reset on close works. HUD: three rockets. Rocket 1 started happy at 2.52 s. Rocket 2's reaction arrived at 5.52 s, while happy was at 3.00 s, and was ignored: no restart and no second track. Match-level reactions of 0.21 to 0.27 in between triggered nothing. Rocket 3, after idle had resumed, started a new happy at 0.00 (10.84 s) that returned to idle at 14.46 s. |
| Comparison with the previews | `07_InGame_vs_CustomPreview_sheet.png` puts the HUD crop next to the custom player at the same track times (idle, 0.57, 1.23, 2.00, 2.30 and 3.20 s, then idle). Pose, expression and hand swaps match; only the backgrounds differ. |
| Leave and reopen | Level to HOME to map, then the level again: still exactly 5 slots, 5 `KingVenguardCharacter` and 5 `SkeletonGraphic`. Reopening the profile panel twice keeps one dialog holder. The coins panel gets its coin icon back. |
| Pause and resume | In-game pause: the HUD hides as before while the king keeps idling on unscaled time, and Continue returns to play. Editor pause freezes the track (unchanged over 2 s) and resume continues it. |
| Parts, holes, sorting | No missing parts or joint holes at full resolution. The HUD mask follows the arch and the gold frame edge draws over the king. The thumbs-up hand is inside the win-card crop. |
| Input | `raycastTarget` is off on every SkeletonGraphic, so no input is blocked. |
| Console | Zero errors or warnings from the character over the whole run. The only new errors in the shared editor came from another agent's tools (its TNT art bake and refused refreshes). |
| TNT | The live TNT only raises match-level reactions (0.25 or less), below the existing 0.5 cheer threshold. The old cheer sprite never showed for a TNT either, so this is existing game behaviour and was left as is. |

**The in-game clip** (`Previews/InGame/KingVenguard_InGame_HUD_Idle-Happy-Idle_*.mp4`, 9.07 s, 30 fps,
silent) is the level-12 HUD recorded from the Game view in Play Mode:
- idle, then a real rocket activation, then `happy_thumbs_up`, then idle;
- one full-screen 720 x 1280 version and one 2x crop of the portrait.

**How it was captured:** frames were captured at a fixed 30 fps game step
(`Time.captureDeltaTime = 1/30`). That setting does not fix *unscaled* time, which the king normally
animates on. For the length of the recording only, the king graphics were therefore switched to scaled
time and switched back afterwards. `Time.timeScale` was 1 throughout, so both clocks run at the same
speed, and the king plays at his authored speed in step with the board.
All other checks ran on the normal unscaled clock in real time.

## Files

| Path | Contents |
|---|---|
| `RigReady/spine/King_Venguard.json` | Skeleton: 34 bones, 37 slots, 48 region attachments + 4 clipping. Animations: `idle`, `happy_thumbs_up`, `range_test` (test only). |
| `RigReady/spine/King_Venguard.atlas` + `King_Venguard.png`, `King_Venguard2.png` | Packed atlas: two 2048 x 2048 RGBA pages, straight alpha, 2 px padding, no rotation. |
| `RigReady/spine/images/` | The 48 attachment PNGs, for re-packing in the Spine Editor. |
| `RigReady/spine-unity/` | The runtime export used in Unity: the byte-identical JSON plus a **single** 2048 x 2048 page packed at texture scale 0.9 (MaxRects, 2 px padding, premultiplied-filtered downscale, stored straight alpha). `export.json` records the scale. Made by `Source/tools/export_unity_atlas.py`. |
| `Source/tools/runtime/patch_spine_unity6.py` | Re-applies the 16 Unity 6000.5 compile patches to a fresh spine-unity 3.8 checkout of the same commit. |
| `RigReady/parts/*.png` + `parts.json` | The same 48 parts, cropped, with rig-pose position, bone, `fill` and `derived` notes. |
| `Source/KingVenguard_V1_RigParts.psd` | Layered source: one named layer per attachment at its rig-pose position, in draw order. Swaps are hidden layers above their slot. Re-read with Pillow; not opened in Photoshop. |
| `Design/01`-`06` | Generated design art (06: hand sheet). `05` has pose 1 corrected. |
| `Design/07`, `08` | Exploded parts sheet; game-scale readability. |
| `Design/09`, `10` | New: consistency contact sheet; hand and ring arrangement. |
| `Previews/*.mp4` | The two approval previews. The pass-1 videos are in `Previews/superseded/`. |
| `Previews/checks/` | `fidelity.json`, `continuity.json`, `render-idle.json`, `render-happy.json`, difference images. |
| `Previews/InGame/` | The Unity Play Mode evidence: the idle-happy-idle HUD clips, stills of every slot, the in-game vs custom-preview sheet, and the per-frame track logs (`logs/`). |
| `Source/tools/` | `python run_all.py` rebuilds everything (Pillow, NumPy and ffmpeg needed). |

## Hierarchy and pivots

Pivots are pixel positions in the rig-pose PNG. `_R` is the character's right, on the viewer's left.

| Bone | Parent | Pivot (x, y px) |
|---|---|---|
| root | - | (848, 2320) ground centre |
| hips | root | (848, 1700) |
| skirt | hips | (848, 1560) |
| thigh_R / thigh_L | hips | (650, 1740) / (1045, 1740) |
| shin_R / shin_L | root | (680, 1995) / (1010, 1995) knees |
| foot_R / foot_L | root | (690, 2182) / (1005, 2182) ankles |
| torso, belly | hips, torso | (848, 1640) |
| chest | torso | (848, 1100) |
| cape_R / cape_L | chest | (640, 890) / (1056, 890) |
| upperarm_R / upperarm_L | chest | (400, 1040) / (1296, 1040) shoulders |
| forearm_R / forearm_L | upperarm | (288, 1235) / (1408, 1235) elbows |
| hand_R / hand_L | forearm | (178, 1432) / (1518, 1432) wrists |
| neck | chest | (848, 830) |
| head | neck | (848, 790) |
| crown | head | (848, 395) |
| gaze | head | (848, 512), moves both irises |
| iris_R / iris_L | gaze | (778, 512) / (918, 512) |
| brow_R / brow_L | head | (765, 440) / (931, 440) |
| mustache | head | (848, 600) |
| chop_R / chop_L (whiskers) | head | (650, 600) / (1046, 600): the top of each whisker, so scaling stretches it downward |
| mouth | head | (852, 668) |
| cheek_R / cheek_L | head | (718, 568) / (978, 568) - new |

The feet and shins hang from `root`, so they stay planted. The thighs hang from `hips`, and the
animations rotate and scale each thigh so its knee end stays on the shin while the hips move.

## Draw order and attachments (back to front)

| # | Slot | Bone | Setup | Swaps |
|---|---|---|---|---|
| 1-7 | pelvis, shin_R/L, foot_R/L, thigh_R/L | own bones | same names | - |
| 8 | skirt | skirt | skirt | - |
| 9-10 | upperarm_R/L | own bones | same names | - |
| 11 | torso | belly | torso | - |
| 12-15 | cape_R/L, neck, collar | cape_*, neck, chest | same names | - |
| 16 | hand_R | hand_R | hand_R_open | hand_R_fist, **hand_R_thumbsup**, hand_R_palm |
| 17 | forearm_R | forearm_R | forearm_R (the cuff covers the wrist) | - |
| 18 | hand_L | hand_L | hand_L_open (ring) | hand_L_fist (ring) |
| 19 | forearm_L | forearm_L | forearm_L | - |
| 20 | head | head | head (face, ears, hair, eye whites, neutral mouth) | - |
| 21-22 | cheek_R/L | cheek_* | cheek_* (identical to the head at rest) | - |
| 23-24 | eyewide_R/L | head | none | eyes_wide_* |
| 25-28 | eyeclip_R, iris_R, eyeclip_L, iris_L | head / iris_* | clip to the eye opening, then the iris | eyeclip_*_wide |
| 29-30 | eyelid_R/L | head | none | eyes_closed_*, eyes_happy_* |
| 31 | mouth | mouth | none (the head holds the neutral mouth) | mouth_neutral, mouth_laugh, mouth_worried, mouth_sad |
| 32-34 | chop_R/L, mustache | own bones | same names | - |
| 35-36 | brow_R/L | brow_* | brow_* | brow_*_worried, brow_*_sad |
| 37 | crown | crown | crown | - |

**Attachment points:**

- **Hand swaps:** positioned by their wrist point, from the hand sheet scaled 0.41. Fists and the
  palm sit at -90 degrees to the hand bone, and the thumbs-up at 0 degrees. Their skin is
  colour-matched to the rig hand of the same side; only left-hand gold is treated as ring.
- **Face swaps:** eyelid, mouth and wide-eye swaps are pixel-aligned to the head, because they are
  region edits or warps of the same rig-pose image.

## Expressions and the limited facial response

| Expression | Parts and bones |
|---|---|
| Neutral, warm closed smile | head (the master's face) |
| Blink or relaxed | eyes_closed_* |
| Laugh squint | eyes_happy_* |
| **Wide-eyed** (new) | eyes_wide_*: the upper lid raised 12 px by a warp of the head's own eye region, lower lid fixed, with the iris clip swapped to the matching shape and the irises scaled 0.96 |
| Glance | gaze bone, up to 10 units |
| Brows | brow_* move up to 15 units. **Worried** (new): inner ends lifted 22 px. **Sad** (new): inner ends lifted 16 px, outer ends dropped 12 px. Both are warps of the existing brow art. |
| Mouths | laugh, worried, sad (region edits) |
| **Cheek and whisker response** (new) | cheek_* (feathered copies of the head's cheek skin) lift up to 7 units and scale up to 1.05. The whiskers stretch up to 1.05 from their top pivots, ride up and flare outward. The mustache widens up to 1.045. Only these facial bones deform; the body stays rigid. |

Each brow part carries its own soft shadow on the forehead, taken from the master, so the shadow moves
with the brow. The first pass had no worried brows, no sad brows and no wide-eyed swap.

## Parts and hidden-area coverage

How the hidden areas were painted:

- **Magnific region edits, only where hidden:**
  - the hair dome under the crown (F1)
  - thin hair under the whiskers (F2)
  - the neck and collar under the chin outline (F4)
- **Reflection across the covering edge:**
  - the forehead under the crown band
  - the coat under the cape
  - the sleeve under the cape, within the arm capsule, with the rest of the capsule filled from the sleeve
  - the wrists under the cuffs
  - the breeches under the skirt
- **Smooth fill from chosen seed pixels only:**
  - the forehead under the brows, from skin and never from hair or crown shadow
  - the eyeball behind the irises, from clean white only
  - the hips and the thigh continuation under the skirt, from the breeches the master shows
- **Seam passes:**
  - orphan pixels are given to the nearest part, and enclosed pockets are closed
  - every lower part reaches 4 px under the parts above it, using its own edge colours
  - the elbow and knee reach further, as described above

| Check (all rendered or measured by script) | Result |
|---|---|
| Rig setup render vs master, 1:1, opaque pixels | Mean max-channel difference 1.27 / 255. 378 px differ by more than 40 and 42 px by more than 80, out of about 2 million. The largest cluster is 101 px at the right brow tail. |
| Part reassembly vs master | Mean 0.65, p99 3.4. 0 px drawn more than 2 px outside the silhouette, 0 opaque pixels uncovered. |
| Joint test (`range_test`, 6 poses, 1:1), enclosed holes | 0 / 0 / 67 / 31 / 146 / 31 px. Poses 2, 3 and 5 are specks of 25 px or less, mostly behind the crown's side finials at ±8 degrees of head turn. The 146 px in pose 4 (arms out 25 degrees) is background enclosed between the bent forearm and the belly, a real gap. The first pass had 559 and 448 px at the skirt hem in poses 3 and 5. |
| Structural validation (JSON against atlas and images) | PASS |
| Loop seam and hand-offs (`continuity.json`) | Idle 4.0 to 0.0 s: 0.000 px. Idle to happy: 0.053 px. Happy end to idle: 0.000 px. No attachment differences. |
| Idle preview, all 480 frames (`render-idle.json`) | Largest enclosed speck 16 px. The largest hairline cluster is 76 px; the crown's concave corners alone give about 60. |
| Happy preview, all 360 frames (`render-happy.json`) | The two largest enclosed regions are background between the hanging hand and the hip: 291 px at 3.37 s and 66 px at 1.57 s. Everything else is 32 px or less. The largest hairline cluster is 97 px, the slit between the lifting sleeve and the coat. Before the arm fixes, elbow and belly cracks gave 134-191 px. |
| PSD re-read with Pillow | 48 layers, 0 name, bounds or pixel mismatches. Odd-sized layers get one transparent column, because Pillow expects even-padded raw channels. |

**Tested motion range, beyond which painted fills may show:**

- **Right arm:** upper arm -25 to +23 degrees, forearm up to +128 degrees.
- **Left arm:** upper arm -28 to +25 degrees, forearm -70 to -45 degrees.
- **Arms turned out 25 degrees** (joint-test extreme): the hidden inner arm shows as a smooth darker
  band, with a faint light line where the visible sleeve edge met the belly.
- **Cape 3 degrees with the arms down** (joint-test extreme): a trace of the hidden sleeve shows at the
  cape's upper outer corner. The approval animations stay within 2.5 degrees.
- **Head:** ±8 degrees. Specks appear behind the crown's side finials at the extremes; the approval
  animations stay within ±4.
- **Crown:** ±3 degrees, lifting 6 units.
- **Cape:** ±5 degrees.
- **Skirt and torso:** ±3 degrees.
- **Hips:** dropping 16 units, shifting 8 units sideways.
- **Brows:** -3 to +15 units.
- **Irises:** ±10 units.
- **Cheeks:** up to 7 units, scale 1.05.
- **Whiskers:** ±5 degrees, scale up to 1.05.

Raising an arm above the shoulder has not been tested.

## Approval previews

Both previews are rendered from `RigReady/spine` with the custom preview player (not AI video, and not
the official runtime). They are 60 fps, 1.0x speed, silent and 1080 x 1350, with a burned-in label and
timecode. The same animations on the official runtime, in game, are in `Previews/InGame/`, compared
side by side in `07_InGame_vs_CustomPreview_sheet.png`.

- **`Previews/KingVenguard_Idle_Loop_60fps_CustomPlayerPreview.mp4`** - `idle`, two 4.0 s loops.
  - Breath: the belly scales +1.0% wide and +1.4% tall, and the chest lifts 3.5 units.
  - Weight shift: the hips sway ±6 units with the feet planted.
  - Lagged follow-through on the head, crown, whiskers, cape and arms.
  - A blink at 1.55 s, and a glance with a brow lift from 2.1 to 3.35 s.
- **`Previews/KingVenguard_Happy_ThumbsUp_60fps_CustomPlayerPreview.mp4`** - 1.0 s of idle, then
  `happy_thumbs_up` (3.8 s), then 1.2 s of idle. This shows the hand-off in and the return to idle.
  - **Laugh:** 0.18-1.5 s, with happy squint, laughing mouth, and cheeks and whiskers lifting.
  - **Arm:** the right arm lifts from 1.45 s and the fist forms at 1.58 s.
  - **Thumbs-up:** swapped in at **1.90 s**. This is the only timing taken from the reference.
  - **Hold:** wide eyes with a second laugh from 1.92 to 2.62 s, then a blink at 2.78 s.
  - **Return:** fist at 3.14 s, open hand at 3.30 s, settled by 3.8 s.
  - **Secondary motion:** the whiskers, mustache, crown, cape, skirt and left arm run on damped springs.

All body motion and all other timings are original design choices; see `ReferenceNotes.md`. The inspected
reference is a head-and-shoulders portrait rig. The fastest single-frame move is the right hand during the
lift into the thumbs-up, 41 units at 1.60 s, about 20 px per frame at preview scale; it is deliberately snappy.

## Completed / untested / missing

**Completed:**

- Hand and ring rule decided, action pose 1 corrected, contact sheet and hand sheet made.
- Rest pose reproduces the master.
- Worried brows, sad brows, wide eyes and the cheek and whisker response added from existing art.
- Parts, PSD, atlas and JSON rebuilt.
- Idle and happy previews re-rendered and labelled.
- Fidelity, joint, continuity and validation checks scripted.
- **Integration:** the official Spine 3.8 runtime was validated, and the rig is imported and live in the
  five existing king slots, verified in Unity Play Mode (see **Unity integration**).

**Untested:**

- Spine Editor import. The data has only been loaded by the official runtimes, not by the editor.
- The PSD has not been opened in Photoshop.
- Android and device readability. There has been no APK or device test, by instruction.

**Missing or limited:**

- No mesh deformation. The cheeks, whiskers and mustache move by bones only.
- No worried or sad eye shapes, and no sad, worried or thinking animation. The HUD "think" state and
  the fail card therefore show neutral idle.
- **Signet ring not visible in game:** every slot frames the head or bust, so the ringed LEFT hand is
  below the crop. The RIGHT-hand thumbs-up, with no ring, is visible.
- **Runtime texture:** the page is 0.9x the master texel density and uncompressed RGBA, 16 MB in
  memory. It stays sharper than every slot needs, but a compressed format (for example ASTC) has not
  been tried.
- **Curve approximation:** the official 3.8 curve approximation differs from the custom previews by up
  to 4 units on the fastest hand move (1.47 s). It is not visible at game size.
- The left hand has only open and fist.
- Front view only, and the cape is two rigid halves.
- Action pose 1 is a front-view rig render, not a matching 3/4 generation (needs credits).
- No upscaling or final enhancement.
- No further animations, by instruction.
