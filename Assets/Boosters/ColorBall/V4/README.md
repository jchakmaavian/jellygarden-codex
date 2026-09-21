# Color Ball V4

V4 is a motion/VFX/SFX quality pass over V3. V3 is untouched and still complete under
`Assets/Boosters/ColorBall/V3/`; V2 is untouched. Target selection, conversion rules and the
existing Rocket activation/travel/destruction are unchanged.

## Reference used

The path in the brief, `00_References/ColorBall-rocket-combo-full.mp4`, does not exist. Searching
`00_References/` as instructed found exactly one Royal Match Color Ball video, and that is what was
measured: `00_References/ColorBall/Colorball+finishing up.mp4` (2160x2700, 60 fps, 12.48 s, audio).
The Color Ball activation runs frames 347-470.

Note: a file with the brief's exact name does exist at
`Design/Effects/ColorBall-live-v5/ColorBall-rocket-combo-full.mp4`, but it is a capture of **this
game** (the QA menu is on screen), not a Royal Match reference.

## What actually changed, and why

Every number below was measured, either off the reference frames or out of the V3 source.

| | V3 | V4 | Why |
|---|---|---|---|
| Charged surface rate | 7.8 rad/s (1.24 rev/s) | **37 rad/s (5.9 rev/s)** | In the reference each dot stretches to about 2.5x its own length per frame, which is ~35 deg of arc. At 60 fps that is 37 rad/s. |
| Finish rate | 10.2 rad/s | **52 rad/s** | Keeps the collapse winding up harder than the hold. |
| Shutter | `max(0, Speed-0.3) * 0.019` = **8.2 deg**, fixed | **`Speed * deltaTime`**, capped at 300 deg | A full shutter: the smear covers exactly the rotation of the frame being drawn, so consecutive frames tile with no gap. This is what lets the ball spin fast without strobing, and it is correct at 30 and 60 fps without tuning because the arc comes from real delta time. |
| Shutter samples | 6, fixed | **6-24, scaled at one sample per 7 deg** | 6 samples across a wide arc ghosts; the count now follows the arc. |
| Horizontal widen | none | **1.0 -> 1.32 with charge** | The reference's charged form is a wide horizontal ellipse. Applied to the quad, so BodyLayer and ColorSpinLayer keep one silhouette and one pivot. |
| White core | small centre glow | **core term in the surface shader**, driven by charge + source pulse | The reference has a bright white core inside the colour. |
| Saturation | plain average | **chroma pushed back out by the amount averaged** | Averaging across an arc pulls neighbouring dots towards grey. Without this the fast ball reads as a wash. |
| Bead layout | 6 rows, up to 12 columns, radius 0.172 | **5 rows, up to 8.5 columns, radius 0.225** | A dense grid closes into thin stripes once smeared. The reference keeps fat blobs with lighter gaps. |
| Body brightness | dark sphere | **glows with charge** | Otherwise the gaps between colour rows read as black bands. |
| Beam white core | 0.027 cell | **0.095 cell** | Measured off the reference: the saturated white core is ~24-30 px perpendicular against a ~230 px cell, about 0.10 cell. V3's core is a hairline over a bright board. |
| Beam glow | 0.110 cell, alpha 0.64 | **0.225 cell, alpha 0.82** | Same measurement; the glow reads at roughly a fifth of a cell. |
| Beam head | 0.23 cell | **0.30 cell** | Keeps the travelling head readable against the thicker tail. |
| Spin audio | loop pitch follows `Charge` | **follows `SpinEnergy` (= Speed/ChargedSpin)**, range widened to 0.72-1.34 | The brief requires the spin sound to follow the actual visual spin speed. Orbit pitch now divides by the real `FinishOrbit` instead of V3's stale 13. |

Deliberately **unchanged** from V3, because they were already right: the 0.62 s charge (the brief says
not to make V4 faster), the 0.124 s volley interval (the reference's measured mean sub-event spacing
is 0.125 s), the 17 cells/s beam head, and the 1-frame-to-flash / 2-frame-to-conversion contact
staging at 30 fps.

## Anti-strobing

V3's QA guarded strobing with "per-frame rotation < 0.36 rad", which only works because V3 had
almost no shutter. V4 turns much further per frame, so that check was replaced with the invariant
that actually matters: **the shutter arc must cover the per-frame rotation step**. If it does,
consecutive frames tile and the motion is continuous however fast the ball spins. The suite holds
this across 1259 samples at both 30 and 60 fps.

## Audio

The same seven original project sounds as V3 (copied into `Audio/Resources/ColorBallV4/`, byte
identical, see `Audio/PROVENANCE.md`). No Royal Match audio was copied or extracted.

A measurement worth recording: the reference has **no spin-up sound and no spin loop**. Its >6 kHz
band is at its quietest of the whole clip through the visual spin-up; it places a single bright
broadband accent at the moment the blur completes (f359-f363) and then the beam volley. The game
keeps its charge and orbit loops because the brief asks for SpinStart and SpinLoop, but they are the
game's own design choice, not something the reference does. (The reference's music bed makes
anything below 6 kHz unmeasurable, so no claim is made there.)

## Verification

`Tools > Royal Throne > Color Ball V4 > Run focused checks with audio` (Ctrl+Alt+Shift+F10) in Play
Mode. Latest run: **376 checks, 0 failures**, TimeScale 1.0 throughout, audio peak 0.244 with zero
clipped samples, 7 cues played and Idle silent, pools clean, no new console errors.

Artifacts in `Animation/`:
- `Previews/ColorBall-V4-Gameplay.mp4` - 7.47 s, 1080x1920, 30 fps, with Unity's real audio mix, 1.0x
- `Previews/RoyalMatch-V3-V4-Comparison.mp4` - reference / V3 / V4 aligned at activation
- `Review/RoyalMatch-V3-V4-charged-comparison.png` - the charged ball, three ways, at matched cell scale
- `Review/checks.txt`, `events.csv`, `sfx-events.csv`

## Integration

All V4 code, shaders, materials, meshes, prefabs, audio and QA live under this folder. The same four
existing scripts V3 used are redirected from V3 to V4:

- `Assets/JellyGarden/Scripts/Item.cs`
- `Assets/JellyGarden/Scripts/Juice/RoyalPieceSkin.cs`
- `Assets/JellyGarden/Scripts/Juice/RoyalPowerupDirector.cs` (including the sequence call, which is
  `AnimateColorBallV4Targets` at line 301 - a partial-class call that carries no namespace, so it is
  invisible to a `ColorBall.V3` grep; miss it and the old sequence keeps running)
- `Assets/JellyGarden/Scripts/RoyalQaService.cs`

To restore V3, point those four files back at `RoyalThrone.Boosters.ColorBall.V3` and at
`AnimateColorBallV3Targets`. Do not touch the Rocket, TNT or Propeller code around them.

## Known gaps against the reference

- The reference ball is larger relative to its cell and carries a wider white bloom than V4 does.
- V4's bands still read slightly more striped, and darker between bands, than the reference's.
- Beam travel, contact staging and the collapse were inherited from V3 rather than re-authored; they
  measure close to the reference already, but they were not independently re-tuned in this pass.
