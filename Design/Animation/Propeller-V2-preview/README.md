# Propeller V2

A live Unity rig that poses the approved painted propeller, replacing the V1 pre-rendered
sequence. `Design/Animation/Propeller-{Reveal,Idle,Activation}-v1` is untouched and still
available for comparison.

## Contents

| File | What it is |
| --- | --- |
| `PropellerV2_Cycle.mp4` | One full cycle: reveal, idle, takeoff, flight, impact. Recorded in Play Mode at 1/25 speed and replayed at 12 fps, so the motion can be read frame by frame. |
| `PropellerV2_Cycle.webp` | The same clip, looping. |
| `PropellerV2_ContactSheet.png` | Every fifth frame of the clip. |
| `states/01-create.png` | Reveal at age 0.100 s, at the overshoot peak. |
| `states/02-idle.png` | Seated at age 0.707 s. |
| `states/03-takeoff.png` | Takeoff at age 0.333 s, rotor fully blurred. |
| `states/04-flight.png` | Flight at age 0.200 s, held lean, no exhaust. |
| `states/05-impact.png` | Impact at the target cell while the rig retires above it. |
| `states/board-before.png` | Board before the visual pass: a dark speck in every cell corner, pieces flat on the tiles. |
| `states/board-after.png` | Board after: grout seams, no specks, pieces grounded on contact shadows. |

The clip was recorded through the real activation path (`RoyalPowerupDirector.Tap`), so the
moves counter ticks down inside it and the board collapses and refills afterwards.

## V1 versus V2

| | V1 | V2 |
| --- | --- | --- |
| Form | 280 pre-rendered 512 px WebP frames in three fixed clips | A pooled rig of 7 sprite renderers posed per frame |
| Scale | Baked into the frames | `CellFit = .88 / 2.92`, measured on the sphere, so it follows `squareWidth` |
| Rotor | Baked blade positions | Body and rotor are separate transforms; the rotor is a tilted disc the blades sweep |
| Occlusion | Baked | Each blade flips either side of the body by `sin(psi)`, every frame |
| Motion blur | None | Crisp blades crossfade into a blurred disc from 260 deg/s, full by 1250 deg/s |
| Flight | Not covered | Driven by the director's Bezier; nothing about the path is baked |
| Impact | Not covered | Plays on the target cell, never inside the flying frames |
| Events | None | Six named cosmetic events |
| Cost | Three texture sets resident | Six small sprites, one pooled rig per live propeller |

## Timings

Every number below was measured in Play Mode, not read off the curves.

| Phase | Length | Shape |
| --- | --- | --- |
| Reveal | 0.55 s | Anticipation at 0.43x on the first frame, full size by 95 ms, overshoot to 1.32x at 95 ms, counter-settle to 0.93x, second overshoot 1.07x, settled from 463 ms and held on the exact idle pose until 550 ms. Rotor enters at 1500 deg/s and eases to the idle 9 deg/s by 86% through. Body tilt swings -6.7, +5.8, -3.4, +1.4, -0.5, 0 deg. |
| Idle | Seamless, no period | Breath of +-0.6% scale and +-0.6% of a cell on a 3.0 s sine, centred on zero. Rotor turns at 9 deg/s, so the three-blade silhouette repeats every 13.3 s. |
| Takeoff | 0.42 s | Compress to 0.93x and dip 0.14 of a cell by 16%, rotor torque tilts the body +7 deg then -5.5, climb to 1.28x and -11 deg lean by the end. Rotor 9 -> 420 -> 1500 -> 2300 -> 2600 deg/s. |
| Flight | `RoyalEffectsProfile.helicopterFlight` (0.47 s as shipped) | Held -11 deg lean with a +-1.6 deg sway, rotor at 2600 deg/s, hover vibration of about 1.2% of a cell. |
| Retire | 0.18 s | Scale to 0.45x and fade out while the rotor spins down. |

## Event timing

| Event | Fires at |
| --- | --- |
| `RoyalPropellerPieceV2.RevealStarted` | The frame the piece becomes a propeller |
| `RoyalPropellerPieceV2.RevealFinished` | Reveal + 0.55 s, as the rig reaches Seated |
| `PropellerActivated` | Activation frame, before the takeoff pose runs |
| `NeighborBlast` | Takeoff + 0.13 s |
| `FlightReady` | Takeoff + 0.42 s, when gameplay may drive `Place` |
| `TargetReached` | End of the flight, at the target cell centre |
| `ImpactStarted` | Immediately after, before the payload resolves |
| `ImpactFinished` | After the payload's board work returns |

None of these carry board destruction; they are cosmetic hooks. The payload still runs through
the director's existing `Explosion` / `Rocket` / `Hit`.

## Seams

Measured frame by frame in Play Mode.

- **Reveal to Idle**: the last reveal frame and the first seated frame report identical body
  centre, bounds and rotation to five decimals. No jump.
- **Idle to Takeoff**: the rig root holds at the same world position across the boundary, and the
  first takeoff step is smaller than the next in-phase step, so the motion accelerates out of rest.
  The rotor phase continues rather than resetting.
- **Takeoff to Flight** and **Flight to Retire**: continuous in position and scale.
