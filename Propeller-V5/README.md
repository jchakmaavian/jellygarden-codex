# Propeller V5

Independent rotor presentation and mechanical audio for Royal Throne. V1–V4 and TNT/Bomb assets remain unchanged. Live Propeller creation, skin attachment and activation select V5 through three small integration hooks.

## Review

- [Gameplay with captured Unity audio](Animation/Previews/Propeller-V5-Gameplay.mp4)
- [V4 left / V5 right, same target and normal speed](Animation/Previews/V4-left-V5-right.mp4)
- [30 FPS rotor close-up](Animation/Previews/Rotor-30FPS.mp4) · [quarter-speed review](Animation/Previews/Rotor-30FPS-Slow-Review.mp4)
- [60 FPS rotor close-up](Animation/Previews/Rotor-60FPS.mp4) · [quarter-speed review](Animation/Previews/Rotor-60FPS-Slow-Review.mp4)
- [Consecutive startup frames](Animation/Previews/corner-target-Activation.jpg) · [consecutive flight frames](Animation/Previews/corner-target-Flight.jpg)
- [Source and integration package](Animation/Delivery/Propeller-V5-Source.zip)

Close-ups track the body in the exported recording. Slow reviews change playback speed only. Gameplay recordings run at 1×; no global speed or Time.timeScale changes are used. The recorded audio is Unity's actual mix, not an added soundtrack.

## V4 versus V5

| Aspect | V4 | V5 |
|---|---|---|
| High-speed blades | Solid blades at 3,300°/s plus six ghost copies; 110° steps alias against 120° symmetry at 30 Hz | Continuous gold/ruby swept-paddle shutter, crisp blades fade out, strictly forward visible phase |
| Visible phase | Raw motor angle | Smooth motor-dependent presentation rate capped asymptotically below 780°/s: about 26° per 30 Hz frame / 13° per 60 Hz frame |
| Startup | Ghosts added around fully opaque blades | Readable blades → gradual sweep → high-speed colored directional blur |
| Depth | Solid and ghost meshes | Same tilted 3D rotor plane/body/hub; fading blades do not write invisible depth into the sweep |
| Lift | Earlier release | Stationary rotor startup, dip, visible vertical lift, pressure/neighbor event, then banking and translation |
| Flight | Distance-based path | Same distance path and target selection; decorative flight sparks removed |
| Audio | Procedural tones, separate startup and flight | Original generated motor/air/contact sources; one uninterrupted motor loop, speed-driven pitch, contact fade/stop |

The motor still reaches 3,300°/s. `VisibleRate` is explicitly a presentation filter, not a claim that the rendered blade angle is the physical motor angle. At high speed the shutter representation carries the blur; the readable phase stays below the three-blade sampling limit at both requested rates. The body is never used to fake rotor spin.

## Timings and events

| Phase/event | Relative time |
|---|---:|
| Reveal → Idle | 0.52 s |
| Activation started | 0.00 s |
| Rotor audio/motion startup event | 0.055 s, quantized to the next rendered frame |
| Full rotor power | 0.32 s |
| Body dip | 0.14–0.25 s |
| Vertical rise | 0.25–0.46 s, ends 0.58 cell above the origin |
| Takeoff pressure + existing neighbor hits | 0.355 s |
| Flight ready / translation begins | 0.46 s |
| Flight | Existing `clamp(0.40 + 0.06 × distanceInCells, 0.45, 0.90)` |
| Contact compression → impact | 0.022 s after contact |
| Impact visual complete | 0.18 s after contact |
| Local fragments | 0.30–0.38 s |

`PropellerVisualV5` owns presentation/events; the existing director chooses targets and affected cells. The V5 bridge preserves V4's flight curve, distance timing, reservations, payload handoff and destruction calls.

## Audio assets

Three original sounds were generated for this assignment with ElevenLabs through Magnific. No Royal Match audio was copied or extracted. These are generated sound effects, not microphone field recordings or user-approved final recordings.

- `Audio/Resources/PropellerV5/RotorLoop.wav`: small motor/blade-air source; 2.4 s loop, overlap crossfade. The same AudioSource spans RotorStart and Flight. Pitch follows motor acceleration from 0.42× to 1×; gain fades during approach and stops at contact.
- `AirRelease.wav`: 0.25 s aerodynamic takeoff accent.
- `PhysicalHit.wav`: 0.22 s small mechanical/ceramic contact and chips. Lower-gain variants provide creation and neighbor contact.

Source MP3s and processing provenance are under `Audio/`. Only trimming, filtering, gain normalization and fades/crossfades were applied; no oscillator beeps or musical chimes were synthesized. Sources remain replaceable through the three resource clip slots. Five simultaneous rotor loops maximum; the independent 20-voice pool applies combined gain headroom without changing shared audio or TNT.

## Verification

`Animation/Review/checks.txt`, `final-summary.json`, `recording-report.json` and `events.csv` contain the final run. Focused cases cover real 2×2 creation, reveal/idle, short/medium/long flights, edge/corner targets, three/five Propellers, Propeller plus TNT, interrupted activation and pooled reuse, and a separate 60 Hz capture. Phase progress is checked modulo the three-blade symmetry; audio samples are checked for clipping. The disposable QA profile is restored afterward.

The first pass passed runtime checks but exposed invisible-blade depth holes in visual review. Those were fixed in V5, the sweep was narrowed, and final captures were re-recorded. `first-pass-checks.txt` is retained as history, not final visual approval.

Captured 30/60 Hz tests verify motion at those sampling intervals; they are not a device performance benchmark. Visual and audio taste remains reviewable in the recordings.

Protected file hashes and prefab/material references are checked in `locked-verification.json` and `asset-reference-check.json`. V5 runtime prefabs/materials reference only V5 assets. There is no reference to TNT art, shaders, audio or particles.

Run in Unity Play Mode after the homepage is open: **Tools → Royal Throne → Propeller V5 → Run focused checks with audio**. Shortcut: Ctrl+Alt+Shift+F12. The builder runs once in Edit Mode and can be rerun from the adjacent Build menu.

## Changed files

All new runtime, editor checks, generated meshes, prefabs, materials, shaders, SFX and review artifacts are under `Assets/Boosters/Propeller/V5/` with new GUIDs. Main runtime files:

- `Scripts/PropellerVisualV5.cs` — three rotor stages, independent body/plane/rotor motion, phase filter, reveal/lift/impact and pooling.
- `Materials/PropellerRotorShutter.shader` — directional exposure integration in the tilted rotor plane.
- `Scripts/PropellerAudioV5.cs` — continuous motor, speed-driven pitch, event accents, headroom and cleanup.
- `Scripts/PropellerGameplayV5.cs`, `PropellerSeatV5.cs`, `PropellerMotionV5.cs` — independent bridge preserving existing target/path ownership.
- `Scripts/PropellerVfxV5.cs`, `PropellerGeometry.cs` — isolated compact accents/chips and unchanged body mesh construction.
- `Scripts/Editor/PropellerV5Builder.cs`, `PropellerV5Checks.cs`, `PropellerCapture.cs` — asset builder and focused recording/checks.

Only three existing files have integration edits: `Assets/JellyGarden/Scripts/Item.cs`, `Assets/JellyGarden/Scripts/Juice/RoyalPieceSkin.cs`, and `Assets/JellyGarden/Scripts/Juice/RoyalPowerupDirector.cs`. `Animation/Delivery/Integration.patch` records the exact changes against commit `130abc7`. No UI, level, targeting, Rocket, TNT or Bomb implementation is changed.
