# Color Ball V3

V3 is the active Color Ball visual in the independent JellyGarden-Codex project. V2 remains intact. This is a focused visual/SFX revision; target selection and the existing Rocket activation, travel and destruction systems are unchanged.

## Motion and event staging

| Stage | Timing / behavior |
|---|---|
| Reveal | 0.76 s: seed, compressed body, delayed surface, 1.11 overshoot, counter-settle, exact idle |
| Idle | Stable 0.84-cell sphere; surface rotates at 0.065 radians/s; faint breathing core |
| Charge | 0.62 s smooth acceleration to 7.8 radians/s surface and 8.6 radians/s orbit |
| Source | 2–3% pulse and directional center glint precede each moving beam |
| Beam | Existing selected cells and order; 17 cells/s head, short 1.75-cell tail |
| Target | Contact, 0.033 s to flash, another 0.033 s to conversion; at 30/60 FPS this is 1/2 frames per beat |
| Volley | Medium interval 0.124 s; high-count middle 0.121 s, ends 0.132 s; these produce approximately 20–23% longer complete volleys than measured V2 |
| Resolve | 0.30 s accelerated surface/orbit contraction with compact final flash |
| Handoff | Ball hidden for 0.16 s before the existing Rocket system receives the generated Rockets |

The surface shader inverse-projects dots onto a sphere and rotates about a tilted axis. The dark body, silhouette and main lighting stay stable. Six short shutter samples soften high-speed surface motion. Two separately sorted mesh ribbons and a travelling glint provide real front/back occlusion; the rear arc is behind the opaque sphere. The front arc crosses its surface. The orbit contracts rather than emitting a particle cloud.

The required root hierarchy is built in `ColorBallV3Builder`: BodyLayer, ColorSurfaceLayer, OrbitBack, OrbitFront, CoreGlow, BeamOrigin and VFXRoot. Assets are native generated meshes/materials. They are detached from cell masks and use the existing gameplay rendering layer below the HUD.

## Ownership and cleanup

Twelve visual rigs, 32 beams, 192 accents and 24 audio voices are prewarmed. Additional seated rigs are cached only if the board requires them. Beam events allocate no GameObjects. The ball keeps its exact visual and phase when the consumed gameplay Item releases it. A lease guards ownership; an unclaimed consumed visual expires after 0.30 s. Release resets phases and transforms on the next Take and stops owned loop audio. Board/session cancellation clears the V3 pools.

The Color Ball owns conversion. `ColorBallV3Sequence` accepts the existing target list and payload, calls the existing Item.ChangeType, waits for contact/conversion, resolves the ball, then calls the existing Hit handoff. It does not modify Rocket assets, choreography or timing. Non-Rocket special combo fallback logic remains as before.

## Audio

Seven original project sounds: Reveal, Charge, Orbit, Beam, TargetHit, Convert and FinalPulse. Idle is silent. Orbit is a quiet filtered variation of the original project Charge recording. Beam/contact/convert voices have bounded pitch and volume variation; the sum of voice gains is capped at 0.44. Charge and orbit loops follow live state and stop at the final pulse or cancellation. No Royal Match audio is copied. See `Audio/PROVENANCE.md`.

## Focused verification and review

Open the game in Play Mode, then **Tools > Royal Throne > Color Ball V3 > Run focused checks with audio** (Ctrl+Alt+Shift+F10). The disposable QA session restores normal saved player data afterward. It records at TimeScale 1.0 using Unity's actual audio mix. The editor-only deterministic capture clock samples 30 and 60 FPS, then restores its previous value.

The suite covers reveal, 15-second idle, 15-target horizontal combo, 31-target vertical combo, edge/corner targets, 60 FPS pooled reuse, event order, no premature Rocket launch, source ownership, sorting, beam cleanup, sound cues and clipping. `Animation/Review/checks.txt`, `events.csv`, `sfx-events.csv` and `protected-verification.json` contain results. The protected snapshot covers 7,514 V2, Propeller, TNT and Rocket files.

Previews are under `Animation/Previews/`: gameplay with sound, reveal/idle/activation/finish closeups, high-count and 60 FPS runs, and Royal Match/V2/V3 comparison. The comparison aligns activation starts and plays each clip at original speed; the Royal Match board and target count differ. Only the V3 audio track is used.

No Android/device performance claim or full-game QA is implied by these focused Editor tests.

## Integration changes

All new implementation, shaders, materials, meshes, sounds, prefabs, QA and recordings are under `Assets/Boosters/ColorBall/V3/`. Four existing scripts only redirect Color Ball entry points from V2 to V3:

- `Assets/JellyGarden/Scripts/Item.cs`
- `Assets/JellyGarden/Scripts/Juice/RoyalPieceSkin.cs`
- `Assets/JellyGarden/Scripts/Juice/RoyalPowerupDirector.cs`
- `Assets/JellyGarden/Scripts/RoyalQaService.cs`

To restore the old visual, redirect those Color Ball-only references and sequence call to V2. Do not modify the unchanged Rocket/TNT/Propeller code around them.
