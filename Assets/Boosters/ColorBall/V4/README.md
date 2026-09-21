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
| Charged surface rate | 7.8 rad/s (1.24 rev/s) | **94 rad/s (15 rev/s)** | The reference blurred ball has an exact four-frame repeat cycle (autocorrelation minima at k=4,8,12,16 across four base frames), so it turns 90 deg per frame = 15 rev/s = 5400 deg/s = 94 rad/s. |
| Finish rate | 10.2 rad/s | **126 rad/s** | Keeps the collapse winding up harder than the hold. |
| Shutter | `max(0, Speed-0.3) * 0.019` = **8.2 deg**, fixed | **0.60 of the frame step** (54 deg at 60 fps) | Deliberately shorter than the 90 deg step. A full shutter averages three differently coloured beads (spacing is ~36 deg) into grey - tried at 92 deg and it washed out. The reference does the same: its beads stay saturated and stretch to roughly 2.5x their length, about 50-55 deg. |
| Per-frame rotation cap | n/a | **1.75 rad (100 deg)** | At 30 fps, 94 rad/s would be 180 deg a frame, which aliases into mush whatever the shutter. The cap makes the ball spin fewer revolutions per second at 30 fps instead, so motion stays readable at either refresh rate. |
| Shutter samples | 6, fixed | **6-24, scaled at one sample per 7 deg** | 6 samples across a wide arc ghosts; the count now follows the arc. |
| Horizontal widen | none | **1.0 -> 1.80, trailing the blur** | Measured: the reference at rest is 212x190 px (aspect 1.12) on a 231 px cell, and at full speed 375x163 px (aspect 1.84) - it only widens, the height never exceeds the rest height. It is also still round about eight frames after full blur and widest around thirteen, so the widen trails the charge rather than arriving with it. Applied to the quad, so BodyLayer and ColorSpinLayer keep one silhouette and one pivot. |
| White core | small centre glow | **core term in the surface shader**, driven by charge + source pulse | The reference has a bright white core inside the colour. |
| Saturation | plain average | **chroma pushed back out by the amount averaged** | Averaging across an arc pulls neighbouring dots towards grey. Without this the fast ball reads as a wash. |
| Bead layout | 6 rows, up to 12 columns, radius 0.172 | **5 rows, up to 8.5 columns, radius 0.225** | A dense grid closes into thin stripes once smeared. The reference keeps fat blobs with lighter gaps. |
| Body brightness | dark sphere | **glows with charge** | Otherwise the gaps between colour rows read as black bands. |
| Beam white core | 0.027 cell | **0.095 cell** | Measured off the reference against its 231 px cell: pure white core 14 px (0.061 cell), full white band 21 px (**0.091 cell**). V3 drew a hairline over a bright board. |
| Beam glow | 0.110 cell, alpha 0.64 | **0.225 cell, alpha 0.82** | Reference total visual width is 50 px typ = **0.217 cell**, constant along the length with no taper. |
| Beam head | 0.23 cell | **0.30 cell** | Keeps the travelling head readable against the thicker tail. |
| Spin audio | loop pitch follows `Charge` | **follows `SpinEnergy` (= Speed/ChargedSpin)**, range widened to 0.72-1.34 | The brief requires the spin sound to follow the actual visual spin speed. Orbit pitch now divides by the real `FinishOrbit` instead of V3's stale 13. |

Deliberately **unchanged** from V3, because they were already right: the 0.62 s charge (the brief says
not to make V4 faster), the 0.124 s volley interval (the reference's measured mean sub-event spacing
is 0.125 s), the 17 cells/s beam head, and the 1-frame-to-flash / 2-frame-to-conversion contact
staging at 30 fps.

## Anti-strobing

V3 guarded strobing by keeping the per-frame rotation under 0.36 rad, which only works because V3
had almost no shutter. V4 turns 90 deg a frame like the reference, so that check was replaced with
two invariants that actually hold the design together:

1. the shutter arc is the designed fraction (0.60) of the rotation step - wide enough to elongate
   each bead and hide the aliasing, short enough that neighbouring beads do not average to grey;
2. the rotation step never exceeds the 1.75 rad per-frame cap, so a 30 fps device spins fewer
   revolutions per second rather than aliasing.

The suite holds both across 1122 samples at 30 and 60 fps.

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
Mode. Latest run: **368 passes, 0 substantive failures**, TimeScale 1.0 throughout, audio peak 0.21
with zero clipped samples, 7 cues played and Idle silent, pools clean, all selected targets contacted
and converted exactly once, existing Rockets launching after payoff.

The run does report one FAILED line, from the suite's "no new console errors" check. The errors are
`IndexOutOfRangeException` inside `UnityEngine.UI.Selectable.OnEnable`, raised from
`LevelManager.EnableMap` and `RoyalQaMenu.Update` while the harness toggles its menus. No Color Ball
code appears in the stack; it is a uGUI issue in the QA harness, not a V4 regression.

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
- Beam travel, contact staging and the collapse were inherited from V3 rather than re-authored.
- Measured differences deliberately NOT applied, because the brief overrides them:
  - the reference launches a beam every 85 ms on average (18 beams, irregular 2-9 frame gaps); V4
    keeps V3's uniform 124 ms, because the brief says keep V3 timing and do not make V4 faster;
  - the reference beam head travels 27.8 cells/s against V4's 17 cells/s, for the same reason;
  - the reference glow is saturated amber (additive R:G:B = 1 : 1.03 : 0.08), not cyan or gem-tinted.
    The brief asks for a "colored/cyan" glow, and this project's own earlier v5 notes independently
    described a "narrow white centre with gold surround". V4 still tints the glow with the gem
    colour; switching it to amber is a visible art-direction call and is left for a decision.
