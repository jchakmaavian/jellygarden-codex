# King Venguard V1 - rig guide

Status: **awaiting design approval.** Nothing here is imported into the game. No live character, scene,
UI, gameplay code or music was changed.

## Format

- **Spine 3.8 JSON** skeleton with a libGDX-format atlas, matching the reference's actual format
  (Spine 3.8, 2D skeletal). There is no 3D pipeline.
- Attachments are **rigid region attachments** plus two clipping polygons that keep the irises inside
  the eye openings. There are no meshes, weights or deform keys.
- Expressions and hands change by **attachment swaps**.
- **Units:** 1 Spine unit = 1 pixel of `Design/03_KingVenguard_Neutral_Rig_Pose.png` (1696 x 2528).
- **Origin:** the root sits at ground centre, pixel (848, 2320). Y points up.
- **Size:** the character is about 2,135 units tall, crown tip to soles. At 360 px tall in the game,
  use a skeleton scale of about 0.17.
- **Pose:** a single front view, like the reference. The side and back turnaround is a design
  reference only, not a rig view.

## Files

| Path | Contents |
|---|---|
| `RigReady/spine/King_Venguard.json` | Skeleton: 32 bones, 33 slots, 42 attachments, animations `idle`, `happy_thumbs_up`, `range_test` (test only) |
| `RigReady/spine/King_Venguard.atlas` + `King_Venguard.png`, `King_Venguard2.png` | Packed atlas, two 2048 x 2048 RGBA pages, 2 px padding, no rotation |
| `RigReady/spine/images/` | The 40 attachment PNGs, for re-packing in the Spine Editor |
| `RigReady/parts/*.png` + `parts.json` | The same 40 parts, straight alpha, cropped to content, with rig-pose position, bone and fill notes |
| `Source/KingVenguard_V1_RigParts.psd` | Layered editable source. 40 named RGBA layers at rig-pose positions in draw order. Swap attachments are hidden layers above their slot. Written by script and re-read with Pillow: 0 name, bounds or pixel mismatches. Not opened in Photoshop. |
| `Source/magnific/` | Every Magnific output, cutouts, region edits, masks and `PROMPTS.md` (prompts, credits, used or unused) |
| `Source/tools/` | Scripts that rebuild everything: `python run_all.py` (Pillow, NumPy and ffmpeg needed) |

## Hierarchy and pivots

Pivots are pixel positions in the rig-pose PNG.

- **Legs:** the feet and shins hang from `root`, so they stay planted. The thighs hang from `hips`.
  The animations rotate and scale each thigh so its knee end stays on the shin while the hips move.
- **Torso:** `torso` carries the coat. `belly` scales for breathing about the hip-line pivot.
- **Arms:** they chain from `chest`.

| Bone | Parent | Pivot (x, y px) | Spine world (x, y) |
|---|---|---|---|
| root | - | (848, 2320) ground centre | (0, 0) |
| hips | root | (848, 1700) | (0, 620) |
| skirt | hips | (848, 1560) | (0, 760) |
| thigh_R | hips | (650, 1740) | (-198, 580) |
| thigh_L | hips | (1045, 1740) | (197, 580) |
| shin_R | root | (680, 1995) knee | (-168, 325) |
| shin_L | root | (1010, 1995) knee | (162, 325) |
| foot_R | root | (690, 2182) ankle | (-158, 138) |
| foot_L | root | (1005, 2182) ankle | (157, 138) |
| torso | hips | (848, 1640) | (0, 680) |
| belly | torso | (848, 1640) | (0, 680) |
| chest | torso | (848, 1100) | (0, 1220) |
| cape_R | chest | (640, 890) | (-208, 1430) |
| cape_L | chest | (1056, 890) | (208, 1430) |
| upperarm_R | chest | (400, 1040) shoulder | (-448, 1280) |
| forearm_R | upperarm_R | (288, 1235) elbow | (-560, 1085) |
| hand_R | forearm_R | (178, 1432) wrist | (-670, 888) |
| upperarm_L | chest | (1296, 1040) shoulder | (448, 1280) |
| forearm_L | upperarm_L | (1408, 1235) elbow | (560, 1085) |
| hand_L | forearm_L | (1518, 1432) wrist | (670, 888) |
| neck | chest | (848, 830) | (0, 1490) |
| head | neck | (848, 790) | (0, 1530) |
| crown | head | (848, 395) | (0, 1925) |
| gaze | head | (848, 512) between the eyes | (0, 1808) |
| iris_R / iris_L | gaze | (778, 512) / (918, 512) | (-70, 1808) / (70, 1808) |
| brow_R / brow_L | head | (765, 440) / (931, 440) | (-83, 1880) / (83, 1880) |
| mustache | head | (848, 600) | (0, 1720) |
| chop_R / chop_L | head | (690, 640) / (1006, 640) | (-158, 1680) / (158, 1680) |
| mouth | head | (852, 668) | (4, 1652) |

`_R` is the character's right, on the viewer's left.

## Draw order and attachments (back to front)

| # | Slot | Bone | Setup attachment | Swaps |
|---|---|---|---|---|
| 1 | pelvis | hips | pelvis | - |
| 2-5 | shin_R, shin_L, foot_R, foot_L | own bones | same names | - |
| 6-7 | thigh_R, thigh_L | own bones | same names | - |
| 8 | skirt | skirt | skirt | - |
| 9-10 | upperarm_R, upperarm_L | own bones | same names | - |
| 11 | torso | belly | torso | - |
| 12-13 | cape_R, cape_L | own bones | same names | - |
| 14 | neck | neck | neck | - |
| 15 | collar | chest | collar | - |
| 16 | hand_R | hand_R | hand_R_open | hand_R_thumbsup, hand_R_fist, hand_R_palm |
| 17 | forearm_R | forearm_R | forearm_R (the cuff covers the wrist) | - |
| 18 | hand_L | hand_L | hand_L_open | hand_L_fist |
| 19 | forearm_L | forearm_L | forearm_L | - |
| 20 | head | head | head (face, ears, hair, eye whites, neutral mouth) | - |
| 21-24 | eyeclip_R, iris_R, eyeclip_L, iris_L | head / iris | clipping polygon, then the iris clipped to the eye opening | - |
| 25-26 | eyelid_R, eyelid_L | head | none (eyes open) | eyes_closed_*, eyes_happy_* |
| 27 | mouth | mouth | none (the head holds the neutral mouth) | mouth_neutral, mouth_laugh, mouth_worried, mouth_sad |
| 28-30 | chop_R, chop_L, mustache | own bones | same names | - |
| 31-32 | brow_R, brow_L | own bones | same names | - |
| 33 | crown | crown | crown | - |

**Attachment points.** Swap hands are positioned by their wrist point. They come from
`Design/06_KingVenguard_Hand_Replacements.png`, scaled 0.41 to match the body hands. Fists and the
palm sit at -90 degrees to the hand bone, and the thumbs-up at 0 degrees. `gaze` moves both irises
together; the clip polygons stop them leaving the eye whites. Eyelid and mouth swaps are pixel-aligned
to the head, because they are region edits of the same rig-pose image.

## Parts and hidden-area coverage

Every part keeps its full visible pixels from the rig pose. Hidden areas were filled so movement does
not reveal holes. How each was painted:

- **Magnific edits:** the hair dome under the crown, the skin and thin hair under the whiskers, and
  the neck, shirt collar and fur behind the chin.
- **Reflection** across the covering edge: the forehead under the crown band, the coat under the cape,
  the sleeves under the cape, the wrists under the cuffs, and the breeches under the coat skirt.
- **Smooth diffusion:** the forehead under the brows, the sclera under the irises, and the hips under
  the skirt.
- **Seam pass:** hairline gaps between neighbouring parts, 13,419 px in total, were given to the
  nearest part.

Per-part detail is in `RigReady/parts/parts.json`, including the `fill` and `seams_closed_px` fields.

Check results:

| Check | Result |
|---|---|
| Setup-pose reassembly vs source | Mean difference 1.33 / 255 inside the silhouette. The residue is at anti-aliased part edges. |
| Source-opaque pixels left uncovered | 0 |
| Joint coverage on magenta, six stepped poses | No holes inside the silhouette. See `Previews/KingVenguard_Joint_Coverage_Test.png`. |
| Structural validation of the JSON against atlas and images | PASS. Bones declared parent-first, slots, attachments and swap keys all resolve. |

**Tested motion range, beyond which the painted fills may show:**

- **Right arm:** upper arm -25 to +23 degrees, forearm up to +128 degrees (the thumbs-up pose).
- **Left arm:** upper arm -28 to +25 degrees, forearm -70 to -45 degrees.
- **Head:** ±8 degrees.
- **Crown:** ±3 degrees, lifting 6 units.
- **Cape:** ±5 degrees.
- **Skirt and torso:** ±3 degrees.
- **Hips:** dropping 16 units, shifting 8 units sideways.
- **Brows:** -6 to +14 units vertically.
- **Irises:** ±10 units.

Raising an arm above the shoulder has not been tested. The reflected sleeve fill under the cape will
show there.

## Expression swaps for the observed reference activities

| Reference activity | Venguard parts |
|---|---|
| Idle, blink (idle_loop_*) | eyes_closed_* for 0.14 s, gaze glance, brow lift |
| Joy, happy, thumbs-up (sevinme, Happy_2) | eyes_happy_*, mouth_laugh, hand_R_fist then hand_R_thumbsup |
| Relax (Relax_1) | eyes_closed_* held |
| Worry (kaygi) | mouth_worried, brows up, gaze darting, hands to hand_*_fist |
| Sadness (uzulme) | mouth_sad, eyes_closed_* or lowered gaze, head down |
| Excitement (Heyecan) | mouth_laugh, brows up, bounce. There is no wide-eyed swap; see Missing. |
| Dance (dans) | Body sway with head counter-rotation. There is no dedicated swap. |

## Approval animations

Both are rendered from the rig data at 60 fps, 1.0x speed, silent, 1080 x 1350.

- **`idle`, 4.0 s loop** (`Previews/KingVenguard_Idle_Loop_4s_x2_60fps.mp4`, two loops).
  - **Breath:** the belly scales +1.0% wide and +1.4% tall, and the chest lifts 3.5 units.
  - **Weight shift:** the hips sway ±6 units and dip 2.4 units, with the feet planted and the thighs
    following.
  - **Follow-through:** each link lags its parent by 0.12-0.66 s (torso, neck, head, crown, cape,
    arms, hands, whiskers).
  - **Face:** a blink at 1.55 s, and a glance with a brow lift from 2.1 to 3.35 s.
  - **Loop seam:** the frame-to-frame change across the seam is 0.37, against 0.32 typical.
- **`happy_thumbs_up`, 3.8 s** (`Previews/KingVenguard_Happy_ThumbsUp_3.8s_60fps.mp4`, with 0.4 s of
  rest before and 0.6 s after).
  - **Laugh:** the body dips 10 units, bounces up 7 units, and decays over three rebounds by 1.35 s.
    Happy squint eyes and the laughing mouth run from 0.18 to 1.5 s.
  - **Thumbs-up:** the right arm lifts from 1.45 s, the fist closes at 1.72 s, and the thumbs-up swaps
    in at **1.90 s**, as in the reference. A small pump follows, then a hold with a second laugh, a
    look toward the hand, and a blink at 2.78 s.
  - **Settle:** the hand returns through the fist at 3.22 s and the pose settles by 3.8 s.
  - **Secondary motion:** the mustache, whiskers, crown, cape, skirt and left arm are driven by damped
    springs from the body's acceleration, not hand-keyed wobble.

## How to open or integrate later

This was not done in this task.

- **Spine Editor:** Spine > Import Data > `King_Venguard.json`, with `images/` alongside. The data is
  written to the Spine 3.8 JSON specification: 3.8 curve encoding, `angle` rotate keys and array skins.
- **spine-unity:** use a 3.8-compatible runtime and rename the atlas to `King_Venguard.atlas.txt`. The
  main project has no Spine runtime package today, so integration needs one added.

## Completed / untested / missing

**Completed**

- Reference inspection and an animation list (`ReferenceNotes.md`).
- A master design, a front/side/back turnaround, a neutral rig pose, an expression sheet (8
  expressions), action poses (5) and hand replacements (4). Each comes as an opaque image, a clean
  transparent cutout and split cutouts.
- 40 separated rig parts with painted hidden areas, a layered PSD, a Spine 3.8 skeleton and atlas, two
  approval animations as MP4s, keyframe strips, a joint coverage test and a game-scale readability
  sheet.

**Untested**

- **Official Spine software:** the rig has not been opened in the Spine Editor or any official Spine
  runtime. Neither is installed on this machine or in the project, which is the exact blocker for that
  test. The previews come from `Source/tools/spine_eval.py`, a minimal evaluator of the same JSON. It
  covers bones, rotate/translate/scale, Bezier and stepped curves, attachment swaps and clipping, and
  is not the official runtime. Small differences in curve or clip evaluation are possible there.
- **PSD:** it has not been opened in Photoshop. Only an independent parser, Pillow, has read it.
- **Game use:** the rig has not been played in Unity or on a device.

**Missing or limited**

- **Face shapes:** there is no mesh deformation. The reference squashes cheeks, beard and mouth with
  mesh deforms; Venguard uses rigid parts and swaps. Brows only move and rotate, with no worried or
  sad brow shapes.
- **Swaps not made:** there is no wide-eyed excitement swap and no sad or worried eye shape.
- **Hands:** the left hand has only open and fist. There are no pointing or clasped hands, which the
  worried action pose shows.
- **Views:** the rig is front view only. There is no three-quarter or side rig.
- **Cape:** it is two rigid halves with no cloth mesh.
- **Consistency across the Magnific sheets:**
  - The signet ring sits on the left hand in the master, the rig pose and the hand sheet, but on the
    other hand in some action and turnaround views.
  - The action-pose sheet's thumbs-up uses his left hand; the rig uses his right, matching the hand
    sheet and the reference.
  - Crown finial details vary slightly between sheets.
- **Unused Magnific outputs, kept for the record:** the first master, three region edits that did not
  do what was asked, and one off-style eye edit. `Source/magnific/PROMPTS.md` lists them.
- **No final enhancement:** the design is at working resolution, with a 1792 x 2400 master, and no
  upscaling. That is reserved for after approval.
