# Royal Throne — Propeller V3 animation correction

V3 is a separate runtime rig, pool, materials, shaders, meshes and animation code in this folder. V1 and V2 remain untouched. The blue/gold/red body and paddle geometry retain the V2 design; the work changes motion and local effects, rather than redesigning the booster.

## What changed compared with V2

| Feature | V2 recording | V3 |
|---|---|---|
| Reveal | Whole-rig size change dominates | 0 → 0.70 → 1.17 → 0.96 → 1, with separate body squash, blade lag and counter-settle |
| Motor startup | Rotor accelerates during a short undifferentiated lift | Visible slow blades, accelerating blade sweeps, then high-speed rotor; independent counter-torque |
| Compression | Small pose dip | Body compresses 20%, with a 0.075-cell dip (about 9.6 pixels in the 1080-wide capture) |
| Takeoff | 0.62-cell lift over 0.34 seconds | Distinct release at 0.235 seconds; continuous 0.90-cell lift; flight handoff at 0.46 seconds |
| Blade blur | Four equal-opacity echoes | Six tapered echoes of the actual blades; hidden at low speed; no generic circular disc |
| Flight | Constant 1.10 scale and nearly fixed bank | 1.10 → 1.02 → 0.94 size, smoothly tracking bank up to 13 degrees, subtle pitch and vibration |
| Neighbor hits | Shared large falling fragments distract from lift | Thin directional pressure, local stars, three small painted chips per normal gem |
| Contact | Immediate fade and flash | Short contact squash, sharp white/gold star, then shrink/fade over 0.15 seconds |
| Multiple launches | Same-frame independent activations | Shared Propeller-only scheduler reserves 60 ms launch spacing |

V3 keeps gameplay's selected target and existing curved path, including its ease-in/ease-out. Cosmetic code never selects or destroys board cells. It does not use TNT explosions, fire, smoke, shockwaves or Rocket exhaust. Ordinary gem chips are independent copies of the existing painted gem fragments, scaled to 0.09–0.15 cell diagonals, with gravity and a 0.30–0.38-second fade. Shared TNT/Rocket destruction branches have priority and remain unchanged.

## Phase and event timing

Times are authored game seconds; dispatch is quantized to the rendered frame.

| Event | Time / trigger |
|---|---|
| PropellerRevealStarted | creation, first compressed pose |
| PropellerRevealFinished | 0.52 after creation; exact Idle rest pose |
| PropellerActivationStarted | after the optional 60 ms inter-launch slot |
| PropellerRotorAccelerating | 0.055 after activation |
| PropellerTakeoff | 0.235 after activation |
| PropellerNeighborHit | gameplay-selected neighbors on the release frame |
| PropellerFlightReady | 0.46 after activation; lift is absorbed into world position |
| PropellerTargetReached | gameplay trajectory reaches the selected cell |
| PropellerImpactStarted | 0.022 after contact compression; gameplay hit follows |
| PropellerImpactFinished | 0.15 after contact |
| PropellerCompleted | same completion frame, then release to pool |

Subscribe to `PropellerVisualV3.Event`; the callback includes the instance, current pool lease and position. Reuse resets pose, speed, alpha, body/rotor transforms and local VFX. No object replacement occurs between Idle and Flight. `Place` accepts world position, tangent and normalized progress from gameplay. The renderer owns only pose and rotor response.

Idle has 0.35% size breathing, 0.005-cell vertical response, a 2.6-degree rotor adjustment and 0.55-degree body counter-motion. It emits no effects. The fixed effect pool contains 240 entries; the rig pool prewarms 12 instances. Six sweep pairs are disabled below their visibility threshold.

## Integration / changed existing files

Only these existing gameplay entry points change:

1. `Assets/JellyGarden/Scripts/Item.cs`: select the V3 seat for new Propellers; a Propeller-only normal-gem hit flag routes compact chips instead of the generic destruction animation. TNT and Rocket branches are unchanged and take priority.
2. `Assets/JellyGarden/Scripts/Juice/RoyalPieceSkin.cs`: select the V3 seat for seated Propellers.
3. `Assets/JellyGarden/Scripts/Juice/RoyalPowerupDirector.cs`: route the Propeller sequence to V3, and clear its pool through existing cancellation.

All new runtime assets are here. No scene, UI, level, Rocket, TNT/Bomb or V1/V2 file is edited. The prefab and material GUID check ensures references stay inside V3. The editor builder reads the existing gem-fragment library only to create independent copies; the runtime does not load its assets.

## Review and QA

The supplied `Propeller-V2-Gameplay.mp4` was reviewed in consecutive frames through motor start, takeoff, flight and contact. The first V3 live pass exposed an uneven lift curve and distracting inherited gem debris. The lift was simplified to one continuous release; Propeller-specific small chips replaced its generic debris. The corrected sequence was then retested and recorded.

Use **Tools → Royal Throne → Propeller V3 → Run focused live checks** (Ctrl+Alt+F11) from gameplay Play Mode. It uses the existing disposable QA profile and restores the player's profile afterward. Tests cover real 2×2 creation, 0.5× and 1× phases, a ten-second Idle, forced corner/edge targets through existing blocker priority, five concurrent Propellers, unchanged TNT concurrently, cancellation and reuse. Full-game QA is outside this task.

- [V2 left / V3 right takeoff comparison](Animation/Previews/V2-left-V3-right-Takeoff.mp4)
- [Final live gameplay recording](Animation/Previews/Propeller-V3-Gameplay.mp4)
- [Reveal](Animation/Previews/Reveal.mp4), [ten-second Idle](Animation/Previews/Idle-10-seconds.mp4), [Activation](Animation/Previews/Activation.mp4), [Flight](Animation/Previews/Flight.mp4), [Impact](Animation/Previews/Impact.mp4)
- [Consecutive motion frames](Animation/Previews/V3-Motion-Frames.jpg), [phase screenshots](Animation/Previews/Phase-Screenshots.jpg)
- [Test results](Animation/Review/checks.txt), [actual event timings](Animation/Review/events.csv), [protected-file hashes](Animation/Review/locked-verification.json)

Recordings use a temporary 30 Hz capture clock; they demonstrate animation timing, not physical-device performance. The side-by-side clip aligns activation starts and preserves each version's timing. Its left side is the original V2 recording, not a reimplementation.

Final corrected run: all focused checks passed, no new console exceptions or errors, five concurrent live rigs, peak 102/240 VFX entries, and zero active pooled objects after cleanup. Normal player data was restored. All 1,382 protected V1/V2 and TNT/Bomb file hashes are unchanged.

[Developer source package](Animation/Delivery/Propeller-V3-Source.zip) includes independent V3 runtime assets and their metadata, the three integration files, and a patch against the preceding V2 version. Preview recordings are supplied separately.
