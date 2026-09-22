# Color Ball flow correction

This pass corrects the existing Color Ball presentation in the independent Unity project. Enhanced remains the default; ReferenceMatch and Enhanced still share one booster system. Checkpoint: commit `2852a8f` plus `colorball-rigid-work/Before-flow-correction.zip` outside the project. Unrelated working edits were preserved.

## Visible changes

- The metal sphere stays rigid. Its mounted color exposure now integrates changing rigid orientation, with filtered optical streaks outside the shell. Bright metal replaces the dark separators between bands; optical tails fade before their quad boundary.
- Curved, unequal streams extend outward instead of following a tight circular contour. Near/far layers preserve the colored body. The discharge pulse points toward its actual outgoing beam.
- Warm beam volume, a concentrated moving head and a narrower hot core replace the dominant white-wire appearance. Existing endpoints, travel/contact events and short link overlap remain authoritative.
- Incoming energy opens into a compact directional contact flare. Smaller standalone stars leave the beam/contact connection clearer.

The first capture exposed excess white wash; that was reduced. The next exposed repeated glossy echoes in the optical blur; fine specular detail was filtered and sample spacing reduced. The delivered capture includes those corrections.

## Verification

Focused Play Mode proof and one complete Color Ball + Rocket combo passed. All 15 selected targets converted exactly once; the existing Rocket handoff, final 0.46-second envelope and restored board input passed. Repeated proof runs, cancellation, attached beam origins, rigid shell transforms and empty VFX/audio pools passed. Normal player data was restored. No new errors occurred during these test sessions. The existing Home navigation exception can still occur during Editor transitions; unrelated Home code was not changed.

Uncaptured Editor checks: 30 FPS mean **29.96**, median **29.99**; 60 FPS mean **59.98**, median **54.49**, p95 frame time **23.77 ms**. The existing acceptance threshold passed; this is not a locked-60 or mobile performance claim. Videos contain 30 distinct captured game frames per second, with real game audio and no clipping. Capture performance is excluded from those FPS measurements.

## Comparison limits

`RoyalMatch-Before-Corrected-Comparison.mp4` compares the original Royal Match source, the previous Enhanced recording and the corrected short proof. Activation and first discharge each have normal board-scale and equally scaled closeup chapters. Playback is 1x; no exposure adjustment or retiming. Audio is the corrected game capture only.

The corrected ball retains a more spherical silhouette and narrower optical envelope than the reference. The existing 0.62-second conversion-ready gate and distance-based beam travel remain slower than Royal Match; they were preserved to avoid changing conversion timing and Rocket scheduling. This pass improves the rendered shapes and connections; it does not claim an exact reference match or a numerical quality increase.

## Changed runtime files

- `Assets/Boosters/ColorBall/V5/Shaders/ColorBallSphere.shader`
- `Assets/Boosters/ColorBall/V5/Shaders/ColorBallLight.shader`
- `Assets/Boosters/ColorBall/V5/Scripts/ColorBallV5Presentation.cs`
- `Assets/Boosters/ColorBall/V4/Scripts/ColorBallVfxV4.cs` — V5-only presentation branches.

The focused check output folder and raw-capture ignore rule also changed. No targeting, conversion results, Rocket, TNT, Propeller, UI, global lighting, audio settings or Time.timeScale changes.
