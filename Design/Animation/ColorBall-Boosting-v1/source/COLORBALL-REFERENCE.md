# Color Ball (Light Ball): reference spec

Source: Royal Match screen recording, portrait 1080x1920, 941 frames at 60 fps. The frame at video frame F is `ref/full/(F+1).jpg`.
Status: final. Each number below was measured by an analyst and then checked by two independent verifiers. I re-measured every point where a verifier disagreed; my sheets and pixel tool are in `cb/measure/final/`. The resolutions are listed in section 7.

---

## 0. Conventions (read first)

| Symbol | Meaning | Value |
|---|---|---|
| f | Video frame index. Seconds = f / 60. | - |
| Cell | Board cell pitch. Column centres are at x = 74 + 116c and row centres at y = 662 + 116r. | 116 px |
| **D** | **Resting ball diameter.** Silhouette width including the protruding side studs, measured at frame 440. The height is 108-109 px (1.02 D). | **107 px = 0.922 cell** |
| k | Scale for the target sprite: 512x512 canvas with the ball about 430 px across. k = 430/107. | **4.019 sprite px per ref px** |
| 1 cell | Expressed in D and in sprite px | 1.084 D = 466 sprite px |

- **The game content runs at 30 fps.** New images only appear on odd video frames, and each even frame repeats the one before. One game update is 2 video frames (33.3 ms), so every timing here is accurate to about ±1 video frame.
- **Recording hitch.** Frames 751-754 and 757-760 each show a single held image, and 761 is a double step. From about 755 onward, video frame numbers run **2 frames (0.033 s) behind game time**. All cadence numbers below are given in game time; where video frames are listed after 755, subtract 2 to get the game-time value.
- **Ball positions.**
  - Reveal: col 4, row 1, centre (538, 782).
  - After the fall: col 4, row 4, centre (538, 1126). The measured centre at rest is (536, 1127.5).
- **Column format.** Where a table has three sizes, they read **ref px | ×D | sprite px**.
- **Canvas note.** A 512 canvas covers only ±0.595 D around the ball centre. The halo (radius about 0.92 D), the charge rings (up to about 1.7 D), the stars and the finish ring (glow radius up to 2.22 D) must be drawn as separate, larger quads, not inside the ball sprite.

---

## 1. Timeline of the ball's life

| Phase | Beat | Video frames | Time (s) | Duration |
|---|---|---|---|---|
| **Pre-reveal** | 5 leaves slide in along row 1 toward col 4 (ease-in, no scaling) | 23-42 | 0.383-0.717 | 20 f / 0.333 s |
| **Reveal** (t0 = f43) | R1 Birth: small pastel banded ball, no flash | 43-44 | 0.717-0.750 | 2 f / 0.033 s |
| | R2 White overexposed burst grows (ease-out) | 45-60 | 0.750-1.017 | 16 f / 0.267 s |
| | R3 White plateau at peak size; first stars at 61; whiteness already fading from 63 | 61-66 | 1.017-1.117 | 6 f / 0.100 s |
| | R4 White drops; near-round glassy pastel sphere with a bright rim | 67-70 | 1.117-1.183 | 4 f / 0.067 s |
| | R5 Oblate, sideways-blurred spinning disc | 71-98 | 1.183-1.650 | 28 f / 0.467 s |
| | Thin expanding ring (detached from the disc) | 71-80 (rim hugs the sphere at 67-70) | 1.183-1.350 | 10 f / 0.167 s |
| | 4-point star glints | 61 to about 86 (main burst 63-82) | 1.017-1.43 | about 26 f / 0.43 s |
| | R6 **Hard cut to the crisp resting ball** | **99** | **1.650** | **Total reveal 43→99 = 56 f = 0.933 s** |
| **Post-reveal motion** | Residual unblurred spin at 2.65 rev/s, slightly oversized (about 1.05x) | 99-121 (lower half), 99-143 (upper half) | 1.650-2.400 | - |
| | Squash (117-121), then stretch (127-139) | 115-139 | 1.917-2.317 | - |
| | Per-half spin-down: one-stud snap back, then damped back-swing. Upper half lags by 22 f. | Lower 123-189, upper 145-211 | 2.050-3.517 | about 66 f / 1.10 s each |
| | *(gameplay)* fall of 3 rows | 141-181 | 2.350-3.017 | 40 f / 0.667 s |
| | Landing squash (one update), then 2.5-3 px rebound | 183, 187-207 | 3.050-3.467 | - |
| **Idle** | Fully static, nothing moves | **213-468** | **3.550-7.817** | **256 f / 4.267 s** |
| **Activation onset** | Rocket nose enters the cell; ball unchanged | 467 | 7.783 | - |
| | A1 **First reaction**: tight white glow at full strength, sideways stretch | **469** | **7.817** | - |
| | A2 Spin-up behind the rocket (rotation about the vertical axis), then flatten and tilt | 473-482 | 7.883-8.050 | A1+A2 = 14 f / 0.233 s |
| | Charge ring #0 contracts from r ≈ 180 and merges into the halo | 477-507 | 7.950-8.450 | 30 f / 0.50 s |
| | A3 **First spinning-disc frame** | **483** | **8.050** | - |
| | Disc thickness wobble (thickest 493, thinnest 503-507) | 483-560 | 8.05-9.33 | period 20-24 f |
| | Halo peak plateau | 503-511 | 8.383-8.517 | 8 f |
| | X-shaped star glint at the centre | 507-519 | 8.450-8.650 | 12 f / 0.20 s |
| | Halo settled | 527 | 8.783 | - |
| **Steady spin** | Spinning disc (whole state) | **483-852** | **8.050-14.217** | **370 f / 6.17 s** |
| | Compact look (before the smear) | 483-507 | 8.05-8.45 | - |
| | Heavy look with a wide left smear trail | 509-669 | 8.48-11.15 | 160 f / 2.67 s |
| | Smear pulls in gradually | 669-697 | 11.15-11.62 | 28 f / 0.47 s |
| | Compact look | 697-852 | 11.62-14.22 | 156 f / 2.60 s |
| | Gem-pattern cycle | continuous | - | **8 video f = 4 updates = 0.1333 s** |
| | Charge events (glow onset → ring → merge) | onsets 565 / 655 / 745 / 837 | 9.417 / 10.917 / 12.417 / 13.950 | every about 90 f (1.50 s) |
| **Targeting** | Beam #1 launch | **541** | **9.017** | - |
| | Launch cadence (26 beams, one per crown) | 541 + 10k (video 543 + 10k after the hitch) | 9.017 to 13.217 (video 793) | **every 10 f = 0.1667 s**; window 250 f / 4.17 s |
| | First arrival / last arrival | 553 / 807 | 9.217 / 13.450 | - |
| | Last beam fully gone | 845 | 14.083 | beam life = arrival + 38 f |
| | All 26 crowns hold their charged glow | until 851 | until 14.200 | - |
| **Finish** | F1 **Hard cut: ball replaced by a white orb**; all charged crowns burst in the same frame; no screen flash | **853** | **14.217** | - |
| | F2 Orb holds, and tightens slightly at 855 | 853-856 | 14.217-14.283 | 4 f / 0.067 s |
| | F3 Orb expands; bright rim separates; lens distortion | 857-872 | 14.283-14.550 | 16 f / 0.267 s |
| | F4 Interior hollows out from the centre | 873-888 | 14.550-14.817 | 16 f / 0.267 s |
| | F5 Pure ring, still saturated | 889-900 | 14.817-15.017 | 12 f / 0.200 s |
| | F6 Ring fades (lavender) | 901-904; trace 905-906 | 15.017-15.117 | 4 f (+2 f trace) |
| | Ring gone | 907 | 15.117 | **Finish total 853→907 = 54 f = 0.90 s** |

**Derived durations**

| Interval | Frames | Seconds |
|---|---|---|
| Hit → first spin | 469→483 = 14 f | 0.233 s |
| Hit → first beam | 469→541 = 72 f | 1.200 s |
| First spin → first beam | 58 f | 0.967 s |
| Last arrival → detonation | 46 video f = 44 game f | 0.733 s |
| Last beam gone → detonation | 8 f | 0.133 s |
| Hit → ring gone (whole activation) | 438 f | 7.30 s |

---

## 2. REVEAL spec

t = (f − 43) / 60, so t = 0 is the first ball frame. The ball is centred on its cell throughout. Disc widths include the blur and colour fringe. The squashed disc is **anchored at the top**: its top edge stays at the resting top (−57..−61 px), and the bottom is pulled up.

### 2.0 Pre-beat: leaf merge (t −0.333 to 0, f 23-42)
- The five leaves slide in toward col 4 with ease-in. Outer leaf displacement per update, out of 232 px: +2, +8, +18, +31, +48, +68, +91, +116, +143, +171.
- The last leaf frame is 41/42, with the leaves spanning 215-219 px (2.0 D) and the outer leaves 74% of the way in.
- Leaves keep constant size (99-102 px) and saturation. On frame 43 they vanish in a single step, with **no flash, ring or particles**.

### 2.1 Per-beat poses

| Frame(s) | t (s) | Pose W×H: ref px / ×D / sprite px | Aspect | Colour state | Blur | Halo / ring | Stars |
|---|---|---|---|---|---|---|---|
| 43-44 | 0.000 | 70×58 / 0.65×0.54 / 281×233 | 1.21 | Pastel banded mini-ball, **not white** (r25 L 187, 5-13% white). Cyan, pink-violet and blue blobs; orange equator; white-mint band below it. | Heavy horizontal smear | none | none |
| 45-46 | 0.033 | 72×62 / 0.67×0.58 / 289×249 | 1.16 | **White from here**: core (254,255,231), L 243-245 | - | none | none |
| 47-48 | 0.067 | 82×70 / 0.77×0.65 / 330×281 | 1.17 | White; chromatic fringe (magenta/violet bottom, cyan/green top) | - | faint haze | - |
| 49-50 | 0.100 | 96×82 / 0.90×0.77 / 386×330 | 1.15 | White | - | - | - |
| 51-52 | 0.133 | 109×97 / 1.02×0.91 / 438×390 | 1.12 | White | - | - | - |
| 53-54 | 0.167 | 124×116 / 1.16×1.08 / 498×466 | 1.07 | White, pale yellow equator band (252,255,116) | - | Soft haze 10-20 px beyond the silhouette (+5..+40 L) | - |
| 55-56 | 0.200 | 134×128 / 1.25×1.20 / 538×514 | 1.05 | White | - | haze | - |
| 57-58 | 0.233 | 142×140 / 1.33×1.31 / 571×563 | 1.02 | White | - | haze; glare on neighbour edges | - |
| 59-60 | 0.267 | 151×150 / 1.41×1.40 / 607×603 | 1.00 | White | - | haze | - |
| 61-62 | 0.300 | 154×160 / 1.44×1.50 / 619×643 (height includes knob glint) | 0.96 | White. r25 white share 47-51%. | - | haze | First star, r about 73-83 |
| 63-64 | 0.333 | 157×160 / 1.47×1.50 / 631×643 | 0.98 | White fading (r25 white 26-34%) | - | haze | 4-6 stars, 5-10 px, r 82-88 |
| 65-66 | 0.367 | 157×157 / 1.47 / 631 | 1.00 | White fading, pastel starts to show | - | haze | stars growing |
| 67-68 | 0.400 | about 138×140 / 1.29×1.31 / 555×563 | 0.99 | **White drops** (r25 L 244→206): glassy pastel stud blobs, orange equator, white streak below | Blobs 38-48 px long | Bright rim hugging the silhouette, r 74-78 (0.71 D) | about 8 stars, r 84-91, 9-29 px |
| 69-70 | 0.433 | about 135×122 (±8) / 1.26×1.14 / 543×490 | 1.10 | Pastel | - | Rim r 64-66 | r 85-96, 19-30 px |
| 71-72 | 0.467 | **130×112** / 1.21×1.05 / 522×450 | **1.17** | Pastel disc (see 2.2) | Stud ovals 28-36×16-18 px | **Ring detaches at r 68-70 (0.64 D), +90 L, FWHM about 8 px** | peak 20-32 px at r 88-103 |
| 73-74 | 0.500 | 136×107 / 1.27×1.00 / 547×430 | 1.27 | Pastel | Smear 15-20 px | Ring r 72-75, +85, FWHM about 10 | r 92-108 |
| 75-76 | 0.533 | 142×104 / 1.33×0.97 / 571×418 | 1.37 | Pastel | same | Ring r 76-80, +62..+70 | r 97-114, shrinking |
| 77-78 | 0.567 | 142×100 / 1.33×0.93 / 571×402 | 1.42 | Pastel | same | Ring r 80-85, +45, FWHM about 16 | r 102-120 |
| 79-80 | 0.600 | **148×99** / 1.38×0.93 / 595×398 (widest) | **1.50** | Pastel | same | Ring r 86-88, +14..+25 (last frame) | r 107-124, 9-19 px |
| 81-86 | 0.633-0.700 | 143×96 / 1.34×0.90 / 575×386 | 1.49 | Pastel | same | **Ring gone** | Last small stars; one at r 136-143 until 85 |
| 87-92 | 0.733-0.800 | 135×98 / 1.26×0.92 / 543×394 | 1.38 | Pastel | same | - | - |
| 93-98 | 0.833-0.900 | 127×102 / 1.19×0.95 / 510×410 | 1.25 | Pastel | same | - | - |
| **99** | **0.933** | Crisp ball, about 1.00-1.08 D (99-113 read about 1.05x rest) | about 1.0 | Final navy art | **none (hard cut)** | none | none |

Notes:
- Frames 85-91 and 99 carry a board-shake translation from an unrelated TNT: 85 (+22,+15), 87 (+31,+22), 91 (−9,−11), 95 (+2,+2), 99 (+5,−2). Do not reproduce it.
- White-phase centroid drifts 3-5 px left (the left edge smears further). There is no vertical motion.
- The disc's orange equator sits at y +0..+2 (rest +5..+11). The disc's vertical centre is about 8 px above the resting centre.

### 2.2 Colour references (RGB)
- **Birth (43):** cyan 167,244,236; pink-violet 134,95,190; blue 95,91,202; orange equator 213,128,107; white band 226,254,234.
- **White burst (55):** core 254,255,231; equator band 252,255,116; bottom fringe magenta 232,42,243; top-knob glint cyan 121,197,223.
- **Disc (79):**
  - Stud ovals: cyan 150,255,252 / green 148,255,188 / yellow 250,255,95 / red 240,125,119 / magenta 240,91,191 and 218,126,241.
  - Violet bottom rim 201,48,233; orange equator 225,134,109; white streak under the equator 243,255,189; body is blurred blue-violet.
- **Stars and ring:** cream white 254,255,235, drawn over pieces (they read yellow over crowns).

### 2.3 Stars (R3-R5)
- **Shape:** about 8 axis-aligned 4-point (concave diamond) glints.
- **Spawn:** at r 72-88 px (0.67-0.82 D), staggered over 61-73.
- **Angles:** mostly diagonals, about 25°, 49°, 123°, 145°, 216°, 238°, 311° and 330°.
- **Motion:** radial drift of about 2.3 px per video frame (140 px/s = 1.3 D/s).
- **Size:** 5-10 px at birth → **20-32 px peak (0.19-0.30 D) at 69-73** → shrinking to 7-17 px, then gone.
- **Lifetime:** 16-20 f (0.27-0.33 s) each.

### 2.4 Post-reveal motion (99-213; optional in-game, measured in the reference)
- **Residual spin:**
  - Rate: **2.65 rev/s** (about 32° per 30 fps update, 0.71 stud spacing). The front surface moves to screen right, which is a decreasing Y rotation in Unity.
  - Stud layout: 8 studs per ring, 4 colours ×2. The rate is the lowest-alias reading, since the pattern repeats every 180°.
- **Scale wobble in row 1** (x/y vs rest):
  - 99-113: 1.00-1.05 / 1.06-1.09
  - 117-121 (squash): 1.13-1.16 / 1.00-1.05
  - 127-135 (stretch): 0.95-1.00 / 1.12-1.135
  - 137-139: about 1.0 / 1.08
- **Spin-down, per half:** the lower half (lower stud ring) starts at 123 and the upper half 22 f later, at 145.
  1. **Snap back one stud slot (−45°)** in one update.
  2. Creep forward at about 16° per update for 4 updates, then one fast step of about 22°, then about 8° per update for 4 updates.
  3. Stop at **+40°** past the final pose and hold for 2 updates.
  4. Swing back to **−16°**, hold, then forward to **+8°**, hold, then back to 0.
  5. The lower half is settled at 189 and the upper at 211 (its last step comes in two parts with a hold). Each swing is about 0.45 of the one before.
  - Final pose: red stud facing front on the upper ring, green on the lower ring.
- **Landing:**
  - One update at 183 of 0.97 height and 1.02 width, with the bottom edge fixed.
  - Rebound lift of 2.5-3 px (0.025 D) over 187-207, peaking at 193-201.

---

## 3. IDLE spec

**Measured: there is no idle animation.** From 213 (clean from 215) to 468, the ball is a frozen sprite:
- Stud-shift tracking gives 0.00 ± 0.01 px of rotation.
- The gold band y (1139) and pole y (1075.2) are constant, so there is no bob or breathing.
- Luminance range is 0.14 levels, so there is no pulse.
- There are no glints and no halo.
- All frame-difference spikes also appear in the static neighbouring pieces, so they are codec refreshes.
- Detection limit: a pulse under about 0.3 levels, or a highlight under about 5-10 levels on a few pixels, would be invisible.

**Resting look (frame 440):**

| Feature | Size and position |
|---|---|
| Silhouette | 107 × 109 px (1.00 × 1.02 D; 430 × 438 sprite px) |
| Gold equator band | 92-93 px wide (0.86 D), 12 px tall (0.11 D); centre about 11 px (0.10 D) below the ball centre; dark upper tier and bright lower tier |
| Studs | 15-20 px (0.14-0.19 D); upper-middle and lower rings of 8 each, ring radius 42-43 px (0.40 D); blue pole stud on top |

- **Lighting:** a soft key light from the upper left. The navy dome is 104-122 luma on the left and 74-91 on the right; the lower bowl is 45-77.
- **Band gradient:** the band is brightest (250,235,150) at its left third and darkens to luma 59-76 at the right end.
- **No specular dots and no drop shadow.**

**Recommended subtle life, consistent with the measurement:**
1. **Default: keep the idle ball fully static.** This matches the reference.
2. Put the "life" into event beats that the reference actually has: the post-reveal residual spin and damped per-half settle (2.4), and the one-update landing squash with its tiny rebound whenever the ball lands after a fall.
3. If product still wants idle life, it is **not in the reference**. Keep it below the detection limits above:
   - no transform change;
   - at most a rare (every ≥3 s), ≤0.2 s soft highlight sweep on the upper-left yellow stud or gold band, ≤10 levels (≤4%) additive.

---

## 4. ACTIVATION spec

### 4.1 Onset beats (after the rocket hit)

| Frame | t from hit (s) | Beat | Numbers (ref px / ×D) |
|---|---|---|---|
| 469 | 0.000 | Tight white glow appears **at full strength**, no fade-in. Ball stretches sideways. | Glow is pure white at r 60-79 (0.56-0.74 D), +45 at r 80-89, gone by r 95-105 (0.89-0.98 D); it grows slightly over 469-481. Width 115-120 (1.07-1.12 D, +10-13%, stronger on the right); height unchanged. |
| 473-479 | 0.067-0.167 | Studs shift sideways: **spin-up about the vertical axis**, not a tumble. The rocket is drawn in front. | Lower-row studs turned about 55-60° by 477. Height 116-119 at 473. |
| 477 | 0.133 | **Charge ring #0** appears | r 170-190 (about 1.68 D), +5..+13; contracts about 2.4 px/f (145 at 491, 128 at 497, 110-116 at 505); merges into the halo at 507 |
| 481 | 0.200 | Last sharp sphere: flattened and tilted | 127×95 (1.19×0.89 D), aspect 1.34, tilted 5-8° anticlockwise |
| **483** | **0.233** | **Instant switch to the spinning disc.** Body lightens to periwinkle and studs become pastel ovals. Blur length jumps from 5 to 12 px in one step. | about 155×88 (1.45×0.82 D), aspect about 1.8; equator stripe at y 1131 (+5) |
| 493 | 0.400 | Disc at its thickest; first thin vertical flare line on the upper half | H 102-105 (0.97 D), W about 135-146 (1.3 D) |
| 503-507 | 0.567-0.633 | Disc at its thinnest; **halo peak plateau** | H 73-78 (0.70 D), W 150-160 (1.45 D), aspect about 2.0-2.1. Halo white to r 100-106 on the diagonals (0.95 D); downward (open column) white to about 136-156 |
| 507-519 | 0.633-0.833 | **X-shaped star glint** over the disc centre (sharp, not blurred) | Arms at about ±45-60°, span about 60-90 px (0.6-0.8 D), 3-5 px thick; clearest 509-513 |
| 509 → 521 | 0.667-0.867 | Wide left smear trail starts and ramps up over about 3-6 updates (not one step) | Left edge −85..−100 at 509-515, −100..−120 at 517-521 |
| 517-519 | 0.800-0.833 | Disc at its highest (+10 px) and tilted about 3° right side up | Equator y 1121 |
| 527 | 0.967 | Halo settles | White to r 96-98 (0.91 D) |
| **541** | **1.200** | **First beam launch** (see section 5) | - |

### 4.2 Steady spin look (483-852)

**Shape.** An oblate "burger" disc with horizontal motion blur only, soft left and right ends fading over 8-15 px, and a **static** orange equator stripe.

| Look | Frames | W × H ref px | ×D | sprite px | Aspect |
|---|---|---|---|---|---|
| Compact (start) | 483-507 | 140-160 × 75-105 | 1.3-1.5 × 0.7-0.98 | 563-643 × 301-422 | 1.5-2.1 |
| Heavy, left trail | 509-669 | body about 140 plus a **left-only** trail of 40-60 px, so 170-212 × 88-125 | 1.6-2.0 × 0.82-1.17 | 683-852 × 354-502 | 1.5-2.2 |
| Compact (end) | 697-852 | **133-150 × 98-110** | **1.24-1.40 × 0.92-1.03** | **535-603 × 394-442** | **1.35-1.5** |

- **Centre.** About 5 px (0.05 D) right of the cell centre in the compact look. In the heavy look the trail pulls the apparent centre about 15-25 px left.
- **Recommended implementation.** Use a single symmetric disc of **1.35 × 0.95 D** (aspect 1.42), with a 10-15 px (0.1-0.14 D) horizontal blur. The heavy left trail is an optional early flourish.
- **Height breathing, slow and not tied to the spin cycle** (H px):

| Frames | Height (px) |
|---|---|
| 509-555 | 88-105 |
| 563-601 | 104-121 |
| 603-621 | 90-100 |
| **623-651** | **118-128** (stretch; coincides with a dim halo) |
| 659-667 | 85-97 |
| 673-745 | 95-117 |
| 761-795 | 89-104 |
| 797-851 | 93-110 |

  Overall range is 0.8-1.2 D, with a period of roughly 50-100 f.
- **Tilt rocking.** About ±3-5° in the heavy look and 0-3° later. There is no squash or pulse on the fast cycle.

**Band order, top to bottom** (heights in px / ×D; RGB from frames 809, 527 and 539):
1. Halo white (251,251,255).
2. Thin pale cyan-lilac rim, 5 px.
3. Top gem row: about 3 small ovals, magenta (236,70,242), yellow, green; about 15 px tall (0.14 D); ovals 25-30 × 12 px.
4. Blue-violet body (129-150, 136-182, 236-243).
5. **Upper-equator gem row**: 4-5 ovals of 28-38 × 13-18 px (0.26-0.36 × 0.12-0.17 D).
   - cyan (169-207, 247-255, 237-254)
   - yellow (250,255,124)
   - white-yellow (249,255,222)
   - green (127-132, 255, 120-157)
   - red-orange (240,130,106)
6. Periwinkle body (158,180,251), with a thin mauve line (214,146,169).
7. **Orange equator stripe**, 8-10 px (0.08-0.09 D), (224-232, 120-180, 59-140). It sits 5-9 px below centre, is smile-curved, and **does not rotate**.
8. Thin pale white-lavender line, 4-6 px (230-243, 243, 255).
9. Lower gem row: magenta (238-247, 67-85, 202-251), pink-red (233,51,122), yellow (251,255,137), green.
10. Blue-violet / navy base (139,74,206), (87,80,203), (74,72,227), with small blue and magenta gems.
11. Pale rim, then the halo.

- **Overall colour.** The whole disc is pastel and brightened; true dark navy (65-73, 55-62, 86-122) makes up at most 3%, at the lower-right rim.
- **Stud ovals.** Elongation about 2.1 at 483, rising to 2.5-2.7 (44-48 × 18 px) from 527.

**Core.** There is **no constant white core**. A white gem crosses the centre once per 8-frame cycle, filling 0-15% of a 61×61 box.

**Halo** (white glow behind the ball, in front of the board; it overlaps neighbouring piece edges by about 15 px):

| State | White to | Half brightness | Board colour by |
|---|---|---|---|
| Calm (815-837) | r 94-98 (0.90 D; 378-394 sprite px) | r 106-110 (1.01 D) | r 125-133 (1.20 D) |
| Dim lilac dip (623-665) | r 71-81 (0.71 D) | r 84-90 | r 92-107 |
| After a ring merges | r 105-120 (1.0-1.12 D), relaxing at about 1 px/f | - | - |

- The edge tint varies: cyan (197,231,250) around swells, lilac (206,205,248), pink (236,183,249).

**Charge events** (a periodic "breath" every about 90 f = 1.5 s; event #0 is at activation, 477):

| Event | Glow onset | Ring separates (r ≈ 165) | Merges with the halo |
|---|---|---|---|
| #0 | about 475 | 477 (r 180) | 507 |
| #1 | 565 | 573 | 605 |
| #2 | 655 | 663-665 | 689 |
| #3 | 745 | 755-759 (hitch) | 773-775 |
| #4 | 837 | 845-847 | cut off by the detonation at 851 (r 150) |

- **Onset.** Faint glow in the board gaps out to about 200-230 px (about 2 cells). The static board-gap line at r ≈ 176 lights up (it is not a ring).
- **Ring.** A soft, translucent white band (10-20 px, about 0.1-0.2 D) appears at **r ≈ 165 (1.54 D)** 8-10 f after onset. It **contracts at 2.4-2.7 px/f (≈150 px/s = 1.4 D/s)** and brightens from +40 to fully white by r ≈ 120 (1.12 D). It merges into the halo edge at r ≈ 95-105 (0.9-1.0 D), **24-32 f after separating**.
- **After merge.** The halo white core swells to r 105-120 and relaxes back to about 96 over 15-40 f.
- **Extra fast rings.** After event #1 only: two faint extra rings (595-615 and 611-633) contract from r about 250 to 110 at about 7 px/f, peak +30, never white.

**Vertical flare line.**
- A thin white line, 4-7 px wide (0.04-0.07 D), at x ≈ centre, drawn **over** the disc and never extending past it. Length ranges from 12-35 px early to about 100 px (full disc height, 0.93 D) later.
- Intermittent with no regular period; each showing lasts 6-18 f.
- Seen at 493-501, 541-557, 601-605, 635-643, 679-685, 795-799, 811-827 (strongest 817-819) and 843-851 (weak).

**Stars and sparks.**
- The only ball-owned stars are the X glint (507-519).
- Short tapered rim spikes (8-12, 15-40 px) show intermittently at about 701-723 and 771-811. Spike counts were not verified.
- Long white downward rays (over 150 px) at 695-727 are beam roots, not spikes.
- No free sparks. The 4-point stars seen elsewhere belong to crown impacts.

**Layering, back to front:** board-gap glow and charge ring → pieces → halo → ball disc → flare lines and beam roots.

### 4.3 Suggested seamless spin loop
- **Base loop: 0.1333 s** (4 game frames; 8 frames at 60 fps). This is the measured gem-pattern repeat, lag-8 difference 16.5 vs 37-47 at lags 2-6.
- **Inside one loop:** 2 smooth rightward texture drifts of about +14..+20 px (0.13-0.19 D) on the equator row, then 2 pattern swaps.
  - Procedural equivalent: stud texture UV scroll of **180° of the stud pattern per 0.1333 s**. Only the 180° colour period is observable, so the true rate is aliased.
  - Keep the equator stripe static.
- **Master loop, if charge events are baked in: 1.4667 s = 11 base loops** (88 frames at 60 fps), which matches the measured about 88-92 f event period. Keyframes within that loop:

| Loop time (s) | Event |
|---|---|
| 0.00 | Board-gap glow onset |
| 0.13-0.17 | Ring spawns at 1.54 D |
| 0.55-0.70 | Ring merges at about 0.95 D |
| 0.55-1.10 | Halo core swells to 1.05-1.12 D and relaxes to 0.90 D |
| Anywhere, 0.1-0.3 s | Optional vertical flare |

- **Height breathing:** add ±8-10% on a 0.8-1.6 s sine that is not locked to the base loop.

### 4.4 Finish beats (orb → ring)

Centre: (538,1126), ±5 px; the fitted centre drifts down about 7 px late in the ring phase.

| Frame | t from det. (s) | Element | Radius (ref px / ×D radius / sprite px) | Other |
|---|---|---|---|---|
| 851-852 | −0.033 | Last spin image | - | Halo and disc unchanged: **no wind-up, shrink or brightness ramp** |
| **853** | **0.000** | Solid white orb | Visible edge **r ≈ 90** on the open diagonals (0.84 D / 362); angle-averaged 50% at r 86 (it stops at r 69-73 where neighbouring pieces cover it); soft falloff to about 100-107 | **Behind** the neighbouring pieces on 853-855. All charged crowns burst this frame. No screen flash. |
| 855 | 0.033 | Orb, slightly tighter | r 84-88 (0.80 D) | Sharper edge; faint cyan tint on neighbour edges (lens distortion starts) |
| 857 | 0.067 | Orb starts to grow | r 91-99 (0.89 D) | Distinct cyan ghost arc of the shield inside the left edge; white now spreads over neighbours |
| 859 | 0.100 | Rim separates | Rim at r 90; edge 101-108 | Milky, translucent interior |
| 861 | 0.133 | First clean ring peak | **Ring r 99** (0.93 D); edge 112-122 | Ring FWHM about 18 over the inner haze |
| 863 | 0.167 | - | 109 (1.02 D) | - |
| 865 | 0.200 | - | 118-120 (1.11 D) | Radial zoom-blur streaks on the pieces seen through the orb (865 to about 883) |
| 867 | 0.233 | - | 127-128 (1.19 D) | - |
| 869 | 0.267 | - | 135-136 (1.27 D) | - |
| 871 | 0.300 | Last mostly white interior (about 95%) | 142-143 (1.33 D); edge 153 | - |
| 873 | 0.333 | **Centre hole opens** | 149-150 (1.40 D) | Hole r 12-20 |
| 875 | 0.367 | - | 156 (1.46 D) | Hole r 35-42; 100% of rays saturated |
| 877 | 0.400 | - | 162 (1.51 D) | Hole r 50; saturated band 40 px |
| 879 | 0.433 | - | 168 (1.57 D) | Hole r 60-66; band 38 |
| 881 | 0.467 | - | 174 (1.63 D) | Hole r 80-85; band 35 |
| 883 | 0.500 | - | 179-180 (1.68 D) | Band 34; 99% of rays saturated |
| 885 | 0.533 | Jagged outer spikes appear (20-50 px) | 183-186 (1.72 D) | Band 34; 97% |
| 887 | 0.567 | - | 188-189 (1.76 D) | Band 30; 94-97% |
| 889 | 0.600 | **Pure ring** (haze essentially gone; residual to about 893) | 192-195 (1.81 D) | Band 34; 85-87% |
| 891 | 0.633 | - | 198-199 (1.85 D) | Band 36; 82% |
| 893 | 0.667 | - | 202-204 (1.89 D) | Band 32; 76% |
| 895 | 0.700 | - | 204 (1.91 D) | Band 25; 73% |
| 897 | 0.733 | Thinning | 207-209 (1.94 D) | Band 22; 63%; peak min channel 251 |
| **899** | 0.767 | Last mostly saturated frame | 210 (1.96 D) | Band 12-17; 55%; peak min channel 246-249 |
| 901 | 0.800 | Saturation collapses | 211-214 (1.98 D) | Band about 5; 20%; peak 220-228; pink-lavender glow |
| 903 | 0.833 | Last clearly visible ring (lavender) | 214-216 (2.01 D / 864) | No saturated pixels; peak 192-201 |
| 905 | 0.867 | Trace | about 219 (2.05 D) | +12..+15 over background |
| 907 | 0.900 | Gone | - | - |

- **Growth is ease-out:**
  - 861-875: 4.1 px/f (0.038 D/f)
  - 875-891: 2.6 px/f
  - 891-903: 1.4 px/f
  - Orb edge r 86-90 at 853 → ring peak r 216 at 903 (×2.4).
- **Ring cross-section** (for implementation):
  - saturated white core about 35 px (0.33 D) during 877-893, thinning to 22 px at 897, 15 at 899 and 0 at 901;
  - soft inner ramp of 20-40 px into the fading haze;
  - outer falloff of 15-20 px;
  - outer glow (10% above background) reaches r 117 (861) → 185 (877) → 216 (889) → 238 (903), i.e. 2.22 D.
- **Colour:**
  - White (254,255,252) while saturated.
  - As it fades over the plain board it turns pink-lavender (244,232,236 → 218,197,238).
  - Over grass it turns yellow-lime (254,255,192 → 252,255,104), as if additive.
  - The mean colour warms as it fades: (254,255,246) at 887 → (238,228,181) at 899.
- **Layering:** the orb is behind the pieces at 853-855. From 857 the white, lens distortion and ring draw over the pieces, and the ring reads as additive over grass.

---

## 5. TARGETING VFX spec

### 5.1 Beam look
- **Construction.** A straight additive line from the **ball centre** to the **crown centre**. It is drawn over the pieces and over the spinning disc, though some falling particles draw over it. It has a pure-white core, a pale lemon band and a soft additive falloff; it is not a saturated gold border.
- **Width depends on the background.**
  - "hot" = R,G ≥ 240; "core" = R,G ≥ 245 and B ≥ 235; FWHM = width at half of the peak above the background.
  - The first three segment rows are the look over mid-light tiles. Over dark pieces the line reads about 30% narrower (hot 8-12, FWHM 13-17). Taper along the length is unclear; implement it as untapered.

| Segment | Hot (ref px / ×D / sprite) | White core | FWHM (ref / ×D / sprite) |
|---|---|---|---|
| Leading tip | 5-10 / 0.05-0.09 / 20-40 | 0-9 | 10-15 / 0.09-0.14 / 40-60 |
| **Head swell** (50-100 px long, 2-4 f behind the tip) | **19-26 / 0.18-0.24 / 76-105** | 12-17 | **23-31 / 0.21-0.29 / 92-125** |
| **Steady line** | **14-18 / 0.13-0.17 / 56-72** | **9-16 / 0.08-0.15 / 36-64** | **17-22 / 0.16-0.21 / 68-88** |
| Thin phase | 6-8 (3-5 on its last frame) / 0.06-0.07 | 4-8 | 8-13 / 0.07-0.12 / 32-52 |
| **Contact glow at the target end** | 36-50 / 0.34-0.47 | 31-40 | **45-67 / 0.42-0.63 / 181-269** |

- **Cross-section colour** (c0r4 beam over board 154,167,212):
  - white core 8-10 px (254,255,250-255);
  - pale lemon band of 2-4 px each side (253-255,255,223-237);
  - falloff over 5-8 px each side: (244,254,227) → (234,246,232) → (213,227,230) → (185,201,217).
  - Net effect is +40..+60 on R and G with B unchanged: pale yellow, lime over grass, orange-yellow over red.

### 5.2 Timings (game time; A = arrival frame)
- **Appear.** The launch frame shows a direction-specific white lobe that just exits the halo (r ≈ 112-128, 1.05-1.2 D). On the next update the bolt head is at 160-200 px (1.5-1.9 D). The line stays attached to the ball centre behind the head.
- **Head motion.** 28-40 px/f over the first 2-3 updates, easing to 18-25 px/f near the target. The last update snaps 40-110 px onto the crown centre, and **the crown charges on that same frame**.
- **Travel time** (launch → arrival) is a step function of distance:

| Distance (ref px) | ×D | Cells | Travel |
|---|---|---|---|
| 116 | 1.08 | 1.00 | 6 f (0.100 s) |
| 164 | 1.53 | 1.41 | 8 f (0.133 s) |
| 232-259 | 2.17-2.42 | 2.0-2.24 | 10 f (0.167 s) |
| 328-492 | 3.07-4.60 | 2.83-4.24 | 12 f (0.200 s) |
| 519-656 | 4.85-6.13 | 4.47-5.66 | 14 f (0.233 s) |

- **Persist.** Full width and full brightness until about A+18. A **narrowing wave** then runs from the ball end toward the target at about 18-20 px/f (≈0.17 D/f): d 100-130 at A+18, d 180 at A+22, d 300 at A+28. It thins the line to the thin phase.
- **Contact glow.** Grows until A+14..A+16, holds, and **collapses at A+34..A+38**.
- **Fade.** Brightness falls over **A+26..A+38**. It is not strictly simultaneous along the line: segments can drop out up to about 6 f apart, from either end.
- **Gone at A+38** (A+40 for beam #1). Life from launch is 44-52 f (0.73-0.87 s).
- **On screen at once:** usually 4-5 beams.

### 5.3 Stagger and cadence
- **One launch every 10 game frames (0.1667 s, 6 per second), 26 launches**, first at 541. In video frames that is 541 + 10k up to 741 and 543 + 10k from 753 (hitch).
- **Arrivals** follow at irregular intervals (2-18 f, mean 10.2) because travel time depends on distance.
- **Order.** The target order is **scattered**, not an angular sweep:
  1. c1r6 (3.91 D)
  2. c0r4 (4.34 D)
  3. c7r7
  4. c2r4
  5. c8r8
  6. c5r4
  7. c0r7
  8. c0r8
  9. c5r5
  10. c3r0
  11. c2r2
  12. c8r3
  13. c8r6
  14. c8r0
  15. c0r1
  16. c5r8
  17. c3r7
  18. c5r2
  19. c1r5
  20. c2r6
  21. c1r1
  22. c8r5
  23. c3r8
  24. c6r2
  25. c5r3
  26. c1r0 (5.42 D)
- **Crossings.** Beams to crowns on the same line are drawn exactly on top of each other. A beam passing over a crown does **not** charge it.
- **Arrival frames** (video): 553, 563, 573, 581, 595, 597, 615, 625, 629, 643, 653, 663, 675, 685, 695, 703, 713, 721, 733, 743, 755, 765, 775, 785, 791, 807.

### 5.4 Target glow / charge
- **On arrival (same frame).**
  - A cream outline appears (249-255, 255, 205-212). Its bright core is 4-7 px, **3-6 px outside the crown silhouette**, with a soft halo out to 14-18 px outside (0.13-0.17 D). That puts it about 63-64 px from the crown centre.
  - The body shifts from orange-yellow (245,204,18) to **lemon** (246-251, 224-248, 39-151), with animated white-yellow caustic streaks across it.
- **During contact (A+12..A+30).**
  - The crown's hot area within 90 px is about 15× its uncharged value (6000-7900 px vs 410-490).
  - White area peaks at A+14..A+16.
- **Held charge (after the beam, until 851).**
  - Outline plus lemon body (inner 244-249, 218-241, 32-84).
  - **Pulse every 24 video f = 12 updates = 0.40 s (2.5 Hz).** It is phase-locked to that crown's own arrival: peak at A+6+24k, dip at A+18+24k.
  - The ring luma at r 58-72 swings by 15-40 levels, and the outline outer edge breathes 3-5 px. Any size pop is ≤ about 5%, and unclear.
- **Hold durations** range from 298 f (c1r6) down to 44 f (c1r0). All crowns release together at 853.

### 5.5 Impact flash
- **There is no radial burst or shockwave at arrival.** What you see is a white-hot contact point at the crown centre where the beam ends. It reaches its largest at A+14..A+16 (a white blob about 45-67 px across, 0.42-0.63 D), with small 4-point glint rays of **about 20-40 px (0.19-0.37 D; estimated, not measured)**.
- There is no launch flash at the ball end (see the halo in 4.2).

### 5.6 Final destruction flash (853)
- **No full-screen flash**: the mean difference outside the board is 0.00-0.20.
- The ball is replaced by the white orb (4.4) on the same frame that **all 26 charged crowns burst at once**: white spiky starbursts with gold crown chunks, and the grass under crowns breaks too.
- Four-point sparkles at about 863-891 sit inside the crown burst puffs, not on the orb.
- The board refill starts at about 893-899.

---

## 6. Quick scale reference (ref px → ×D → sprite px, k = 4.019)

| Ref px | ×D | Sprite px | Used for |
|---|---|---|---|
| 12 | 0.11 | 48 | Gold band height |
| 18 | 0.17 | 72 | Steady beam FWHM (typical); stud size |
| 35 | 0.33 | 141 | Finish ring saturated core |
| 58 × 70 | 0.54 × 0.65 | 233 × 281 | Birth ball |
| 69 | 0.64 | 277 | Reveal ring start radius |
| 88 | 0.82 | 354 | Reveal ring end radius |
| 90 | 0.84 | 362 | Detonation orb radius |
| 96 | 0.90 | 386 | Calm halo white radius (diagonals) |
| 107 | 1.00 | 430 | Resting ball |
| 108 | 1.01 | 434 | Halo half-brightness radius |
| 116 | 1.08 | 466 | One cell |
| 130 × 112 | 1.21 × 1.05 | 522 × 450 | Reveal disc at 71 |
| 142 × 100 | 1.33 × 0.93 | 571 × 402 | Steady compact disc (typical) |
| 148 × 99 | 1.38 × 0.93 | 595 × 398 | Reveal disc widest (79) |
| 157 | 1.47 | 631 | White burst peak |
| 165 | 1.54 | 663 | Charge ring spawn radius |
| 216 | 2.02 | 868 | Finish ring max radius |
| 238 | 2.22 | 956 | Finish ring outer glow radius |

---

## 7. Resolved disagreements (final decisions)

1. **First beam launch: 541, not 543.** On a 3-px-wide ray at 213.5° from (538,1128), the min channel at r 96-112 is 165-221 on 539 and jumps to 246-252 on 541 (control angles unchanged). On 543 the head is at r 168. So 541 is the launch frame (a lobe just outside the halo), 543 is the first clear streak, and the cadence is a constant 10 f. The verifier who saw "nothing at 541" only looked beyond r 120.
2. **Reveal disc width at 71-77.** Checked on the zoom sheet `final/rv_disc_069_079.jpg`, the analyst's 146-154 px was the ring's outer diameter. The disc body is about 130 (71) → 136 (73) → 142 (75-77) → 148 (79, widest). Aspect peaks at about 1.5 at 79-83.
3. **Reveal white fade.** The plateau is 61-66, but whiteness already falls from 63. The largest step is 65 → 67.
4. **Frame 99** carries a (+5,−2) board shake. No pop happens at 99; frames 99-113 are uniformly about 1.05x rest.
5. **Post-reveal spin rate: 2.65 rev/s** (both idle verifiers), not 2.8-3.0 (analyst) or about 2 (reveal analyst).
6. **One-stud snap (−45°)** at the start of each half's spin-down (123 and 145). Confirmed independently by both idle verifiers and added to the spec.
7. **Idle window: 213-468.** The ball's first change is at 469; at 467 only the rocket nose enters the cell.
8. **Activation disc widths.** `final/ao_disc_499_521.jpg` shows the left edge at −70..−80 through 507 and −85..−100 from 509, reaching −100..−120 by 517-521, so the widening ramps up rather than jumping. W at 505 is about 150-160 (not 168); aspect is about 2.0-2.1 (not 2.4).
9. **X glint: 507-519** (both the slanted line at 507 and the X at 517-519 are visible on the sheet). The analyst had 509-517.
10. **The pre-spin motion (473-479) is a spin-up rotation about the vertical axis**, not a tumble. Tilt at 553 is ≤2°, not 5°.
11. **Spin-state rings were mis-measured by the analyst.**
    - My column profiles (x 530-546, below the ball) show each ring separating at r ≈ 164-165 and contracting at 2.4-2.7 px/f:
      - Ring #1: 164 (573), 152 (579), 134 (585), 116-122 (591), 104-110 (597), merged 605.
      - Ring #2: 161 (665) → 101 (687), merged 689.
      - Ring #3: 149 (761) → 107 (779); continuous white from core to ring from about 775.
    - The "ring at 176-190" is a static board gap that lights up. The "halo swells to 180-197 px" are this incoming ring glow.
12. **Halo dip and vertical stretch at 623-651: confirmed.** White to only r 74-80 at 635-649. The disc bottom reaches y 1188 (my profile) and height is about 118-128 on `final/ss_stretch_605_645.jpg`.
13. **Wide smear start and end.** The smear starts at 509 (not 499 or 501). Pull-in is gradual over 669-697 (`final/ss_change_665_697.jpg`), not a 687-693 step. The spin state starts at 483 (both verifiers).
14. **Recording hitch** at 751-760, +2 f thereafter (both beam verifiers). Travel times of 16 f and the grid shift after 751 are artefacts; the pulse phase is A+6+24k in game time.
15. **Beam end.** Beams are gone at A+38, and beam #1 at A+40 (my ray: residual +20..+35 at 591, baseline at 593). The fade runs over about A+26..A+38 and is not uniform along the line.
16. **Beam width depends on the background.** Hot 14-18 over light tiles and 8-12 over dark pieces; taper is unclear. Contact glow is 45-67 px (the analyst had 43-52).
17. **Finish orb.** Radius about 90 on the open diagonals (the angle-averaged 86 is pulled down by occluding pieces). The orb is **behind** the neighbouring pieces at 853-855 (`final/fin_orb_851_861.jpg`). Distortion starts faintly at 855. The analyst's centre offset (543,1133) is rejected; the orb is centred on (538,1126).
18. **Finish ring thickness.** The saturated band is 30-40 px (my downward profile: ≥235 from r 155-200 at 883, and r 205-217 at 899), not an 18-29 px FWHM. 899 is the last mostly saturated frame, and saturation collapses at 901. Per-ray saturated fractions are 0.1-0.15 higher than the analyst's.

Evidence produced for this final pass: `cb/measure/final/` (fx.cs/fx.exe pixel tool, rv_disc_069_079.jpg, ao_disc_499_521.jpg, ss_change_665_697.jpg, ss_stretch_605_645.jpg, fin_orb_851_861.jpg). Verifier material is in `cb/measure/verify-*/`.
