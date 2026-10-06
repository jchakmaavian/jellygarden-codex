# Royal Throne Propeller V4

Independent V4 motion, VFX and original audio. V1/V2/V3 remain unchanged. The established body/blade design is retained.

## Timing

Reveal: **0.52 s**. Motor starts at **0.055 s**; dip/compression occupies the first **0.20 s**. Takeoff and neighbor hit: **0.285 s**. Flight handoff: **0.42 s**. Contact/squash/flash: **0.18 s**. Concurrent launches reserve **0.07 s** spacing.

Flight duration is `clamp(0.40 + 0.06 * distanceInCells, 0.45, 0.90)`.

| Cells | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | Longer |
|---|---|---|---|---|---|---|---|---|---|
| Seconds | .46 | .52 | .58 | .64 | .70 | .76 | .82 | .88 | .90 max |

A cubic path continues the upward lift before turning toward the target. Hermite timing matches its entry tangent to the lift's 1.2-cell/second exit velocity, then accelerates and eases into contact. The arc is small and varies with distance. Bank is up to 10 degrees; scale changes only 1.065 to 1.02 to .97. Separate rotor/body transforms retain blade identity during acceleration and use tapered actual-blade sweeps at high speed. No global speed setting or Time.timeScale assignment is added.

## VFX and SFX

Takeoff uses a compact white pulse, air ellipse, directional pressure, stars and tiny painted gem chips. Flight emits two short air/spark accents. Contact adds a small 0.18-second pressure pulse with white/gold stars and squash. No TNT explosion, fire, smoke, exhaust or large shockwave is used for Propeller hits.

Six original mono 48 kHz WAVs are synthesized from oscillators and seeded noise. No Royal Match audio was copied or extracted. The generator is included.

| Sound | Animation event |
|---|---|
| Propeller_Create | RevealStarted |
| Propeller_RotorStart | RotorAccelerating |
| Propeller_Takeoff | Takeoff |
| Propeller_Flight | FlightReady; quiet loop fades on approach and stops at TargetReached |
| Propeller_NeighborHit | NeighborHit; simultaneous neighboring hits share one accent |
| Propeller_Impact | ImpactStarted |

The 24-voice pool respects the existing Sound setting. Aggregate Propeller gain is capped at .22 to preserve headroom; rotor/flight are quieter than launch/impact. Pause, cancellation, release and reuse manage audio explicitly. TNT audio and the shared audio director are unchanged.

## Files and integration

All new scripts, prefabs, meshes, materials, textures, shaders, audio and review files are inside V4. Prefab/material GUID references are independent. Only three existing files change:

- `Assets/JellyGarden/Scripts/Item.cs`: choose V4 seating and route V4 normal-gem hits to its tiny chips, below the protected TNT/Rocket branches.
- `Assets/JellyGarden/Scripts/Juice/RoyalPieceSkin.cs`: choose V4 for seated Propellers.
- `Assets/JellyGarden/Scripts/Juice/RoyalPowerupDirector.cs`: select the V4 coroutine and include V4 in cancellation.

Existing targeting and neighboring cells are unchanged. Cosmetic code does not select targets or apply board damage.

## Review and QA

The Royal Match reference and V3 gameplay were inspected in sequential frames. First-pass V4 review found an abrupt direction change on downward flights immediately after lift. The corrected cubic entry continues the lift before banking; the destination and duration remain unchanged.

TNT was recorded before integration and after V4. Comparison covers its complete activation/explosion/debris state timing and all 25 affected cells through TNT cleanup. Later randomized refill is excluded: the initial overbroad assertion detected an extra ordinary gem clear after TNT had already finished. TNT source and assets remain unchanged.

**Tools > Royal Throne > Propeller V4 > Run focused checks with audio** (Ctrl+Alt+F10) uses the existing disposable QA profile. Tests cover real 2x2 creation, Idle, short/medium/long flight, edge/corner targets, three/five concurrent Propellers, concurrent TNT, cancellation and reuse. Player data is restored afterward. Full-game QA is outside this task.

Recordings contain Unity AudioRenderer's actual game mix synchronized with a 30 Hz capture clock at **1.0x**. No sound effects were added to the video afterward. Capture overhead means the videos demonstrate animation pacing, not device performance.

- [Gameplay with audio](Animation/Previews/Propeller-V4-Gameplay.mp4)
- [Same-target V3 left / V4 right](Animation/Previews/V3-left-V4-right.mp4)
- [Consecutive takeoff frames](Animation/Previews/Takeoff-Consecutive-Frames.jpg)
- [Flight frames](Animation/Previews/Flight-Frames.jpg)
- [QA results](Animation/Review/checks.txt)
- [Animation events](Animation/Review/events.csv) / [sound events](Animation/Review/sfx-events.csv)
- [TNT before](Animation/Previews/tnt-before.mp4) / [TNT after](Animation/Previews/tnt-after.mp4)
- [Protected file verification](Animation/Review/locked-verification.json)

Final corrected run: **102 checks passed**, including TNT before/after, three/five Propellers, cancellation and pooled reuse. No new console errors, no clipped audio samples and no VFX/audio leftovers. Peak: 100/240 effect entries, 12/24 voices, 5 live rigs. All 1,674 protected V1/V2/V3 and TNT/Bomb files remain unchanged.

[Developer source package](Animation/Delivery/Propeller-V4-Source.zip) includes original WAVs, independent native assets, scripts and the focused integration patch.
