# Royal Throne — independent Propeller V2

This implementation lives entirely in this folder, apart from the three existing gameplay entry points listed below. It does not load the old Propeller rig, V1 frame sequences, TNT art, explosion materials, smoke, shockwaves, or Rocket effects.

## Presentation

The supplied Propeller artwork is preserved as `Assets/RoyalThrone-Propeller-reference.webp`. V2 is a new lightweight mesh interpretation of its blue body, gold belt, three gold/red paddles, and ruby hub. Body and rotor are independently posed. This is not a pixel-identical reproduction of the WebP.

The rotor uses real rounded paddle meshes with depth, a centered tilted axis, and four low-opacity samples of its actual swept blades. It never substitutes a generic circular blur. The three blades are combined by material to reduce draw calls. The rig has 14 mesh renderers; the eight sweep renderers are transparent in Idle.

| Phase | Timing in game seconds | Behavior |
|---|---:|---|
| Reveal | 0.54 | 0.20 scale compression, fast rise to 1.16, rebound, independent blade response, exact rest pose |
| Idle | indefinite | 0.25% breathing, 0.003-cell float, 1.8-degree rotor sway, 0.25-degree counter-tilt |
| Activation | 0.34 | compression and torque; rotor accelerates to 400 RPM; release starts at 0.14; lift reaches 0.62 cell |
| Flight | existing `helicopterFlight` setting | gameplay supplies the existing curved path; body leans into travel and rotor holds speed |
| Target contact | 0.12 | compact white/gold flash, tiny scale shock, fade and return to pool |
| Star cleanup | at most 0.38 after emission | small outward stars fade and shrink; no smoke, fire, exhaust or debris from this VFX system |

Idle fits within an actual board cell. Flight uses 1.10 times the rest pose. World-space meshes avoid board/cell masks. The existing HUD remains above gameplay rendering.

## Events and ownership

Subscribe to `PropellerVisualV2.Event`. Arguments are the named `PropellerEvent`, the visual instance, and world position. `Lease` identifies each pooled use; do not retain it as ownership after release.

| Event | When |
|---|---|
| PropellerCreated | first Reveal pose |
| PropellerRevealComplete | Reveal reaches 0.54 seconds and enters Idle |
| PropellerActivationStarted | activation begins, retaining the current pose and rotor angle |
| PropellerTakeoff | at/after 0.14 activation seconds |
| PropellerNeighborHit | each gameplay-selected neighbor, at takeoff |
| PropellerFlightReady | activation reaches 0.34 seconds |
| PropellerTargetReached | gameplay reaches the selected target |
| PropellerImpact | same contact frame, after the local impact is emitted |
| PropellerCompleted | after the 0.12-second contact fade, before release |

Actual dispatch is quantized to a rendered frame. `Animation/Review/events.csv` records measured times. `PropellerSeatV2.Claim` transfers the same live rig from the source item to gameplay; there is no replacement artwork or new rotor phase at launch. `BeginFlightTranslation` absorbs the lift into world position without a jump. The visual never selects targets or clears cells.

`PropellerGameplayV2.cs` is a partial adapter to the existing director. It calls existing `Target`, `Hit`, `StartOperation`, pause handling and payload handlers. TNT and Rocket payload behavior remains owned by those existing systems. Start-cell neighbor hits use the same existing neighbor set, synchronized with V2 takeoff.

## Integration changes outside this folder

1. `Assets/JellyGarden/Scripts/Item.cs`: seat newly created Propellers in their cells; route their reveal to V2; omit the shared generic creation burst only for Propeller.
2. `Assets/JellyGarden/Scripts/Juice/RoyalPieceSkin.cs`: attach the independent V2 seat for Propeller items.
3. `Assets/JellyGarden/Scripts/Juice/RoyalPowerupDirector.cs`: route only `Helicopter` to the new adapter; clear V2's own pool when the existing cancellation entry point runs.

No scene, level, HUD, TNT/Bomb or Rocket files are changed. V1 and the older `RoyalPropellerV2` implementation remain available unchanged. The latter has a historical V2 name but is not this implementation and is no longer selected by these three hooks.

## V1 versus this V2

| V1 | New V2 |
|---|---|
| Authored WebP frame sequences | Independent body/rotor meshes and transform curves |
| Fixed frame-based intro/loop/finish | Gameplay-length flight with continuous rotor angle |
| 0.40-second activation intro and 0.60-second finish in its README | 0.34-second mechanical takeoff and 0.12-second precise contact |
| Rotor sweep baked into frames | Gradual, speed-dependent sweeps of the actual blades |
| Sequence files remain untouched | Separate prefab, pool, shaders, materials, geometry and VFX |

The provided `Royal-Match-booster-animation.mp4` was inspected through its overview and selected frame sequences. It clearly shows Color Ball, Rocket and TNT effects; no distinct Propeller creation/flight cycle was identifiable in that clip. V2 follows the user's detailed Propeller motion brief and the Royal Throne source silhouette, without claiming a frame-identical Propeller reference match.

## Developer use

The runtime prefabs are `Prefabs/Resources/Propeller-V2.prefab` and `Propeller-V2-VFX.prefab`. They reference only assets in this folder. Native Unity shaders and meshes require no added packages. The pool prewarms 12 rigs and 320 air/star particles. Five simultaneous activations fit within that allocation. Native material property blocks are initialized after instantiation. VFX randomness uses its own generator, preserving gameplay random state.

Use **Tools → Royal Throne → Propeller V2 → Run focused live checks** in Play Mode (Ctrl+Alt+F12). Tests use the existing isolated QA profile and practice board, then restore the normal profile. They do not run full-game QA. The recorded capture clock is temporary and restored afterward.

Use **Build independent prefab** only in Edit Mode to regenerate this folder's native meshes/materials/prefabs. `.meta` GUIDs are preserved when rebuilding existing assets. `Animation/Review/locked-verification.json` checks protected existing files; `checks.txt` and `events.csv` contain the live test evidence. Temporary recording frames are in ignored `.Capture/`.

## Delivery and verified results — 21 September 2026

- [Final live gameplay recording](Animation/Previews/Propeller-V2-Gameplay.mp4): normal-speed cycle, five concurrent corner/center Propellers, then Propeller and unchanged TNT together.
- Half-speed phase previews: [Reveal](Animation/Previews/Reveal.mp4), [Idle](Animation/Previews/Idle.mp4), [Takeoff](Animation/Previews/Takeoff.mp4), [Flight](Animation/Previews/Flight.mp4), [Impact](Animation/Previews/Impact.mp4).
- [Full 0.5x cycle](Animation/Previews/half-speed.mp4), [actual 2x2 creation](Animation/Previews/square-match-create.mp4), [cancellation and reuse](Animation/Previews/cancel-and-reuse.mp4).
- [Phase screenshots](Animation/Previews/Phase-Screenshots.jpg), [sequential motion review](Animation/Previews/Motion-Review.jpg), [V1 versus V2 artwork](Animation/Previews/V1-vs-V2.jpg).
- [Detailed test results](Animation/Review/checks.txt), [measured event dispatch](Animation/Review/events.csv), [protected-file verification](Animation/Review/locked-verification.json).

The final focused Play Mode run passed actual 2x2 creation, Reveal/Idle/Activation/Flight/Impact at 0.5x and 1x, all nine event types, one target impact per activation, exact idle cell bounds, no anchor drift, edge/corner launches, five simultaneous rigs, TNT concurrency, cancellation and reuse. It recorded zero new console errors/exceptions. Normal player profile was restored. Peak V2 particles were 137 of 320; five rigs remained within the prewarmed pool. All V2 rigs/particles were inactive after final cleanup. Compilation completed without C# or shader errors.

Measured 0.5x sequence, in game seconds relative to creation: Created 0.000; RevealComplete 0.550; ActivationStarted 1.383; Takeoff and NeighborHit 1.533; FlightReady 1.733; TargetReached and Impact 2.217; Completed 2.350. The Idle hold here is intentional test setup, not a gameplay wait. Frame quantization explains the small difference from authored phase durations.

After the first visual pass, paddles were rebuilt with rounded domed surfaces, a thicker gold belt and swept blade echoes. The final pass was re-captured and reviewed. V2 deliberately uses simpler native geometry than the painted V1 artwork; the comparison shows that difference openly. The recording uses a temporary 30 Hz capture clock to show animation timing consistently; it is not a device performance benchmark.

All 1,206 protected existing V1/TNT/Bomb files match their pre-task hashes. New assets are confined to this V2 folder and its required parent `.meta` files. The only existing source edits are the three integration hooks listed above. No external package was added.
