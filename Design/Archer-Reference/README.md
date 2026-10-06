# Archer and hammer integration

The Arrow tool now plays the reference APK's arrow and target animation with its native spark, wind and smoke particles and `arrow_use` sound. The projectile sweeps left to right through the selected row, damaging each cell on arrival. It spends one Arrow and no move. The existing hammer already contains the imported animation, impact VFX and three sound cues; this task verifies that installation alongside the archer.

## Source and editable assets

- Source APK: `Design/Backup/apk/royal_thron_rfv387.apk`, SHA-256 `a2cbee4ecf021ee464dc6f17766ba6c0b2addb2d65007f611e5c368532aa8151`.
- Arrow prefab 46037; controller 18043; `ArrowAnimation` 16190, 9 seconds / 55 curves; `ArrowFallAnimation` 16191, 0.5 seconds / 34 curves. Both are recovered at 60 FPS, with source key times, values, tangents, six sprites, pivots, pixel scale and hierarchy preserved.
- Flight particles: `arrow_test` 27388 and its three children. Smoke: 57173 and its original inactive child. The six native systems are preserved in two prefabs; inactive source children remain inactive. The unused mesh references on billboard renderers are not needed for those render modes.
- Sound: original `arrow_use`, audio object 17939, decoded losslessly to WAV. No replacement synthesised sound is used.
- Editable imported assets: `Assets/ReferenceGameplay/Tools/Archer`. Runtime resources: `Resources/ReferenceGameplay/Archer`. Native VFX prefabs: `Resources/ReferenceGameplay/VFX/ArcherTrailRfv387` and `ArcherSmokeRfv387`.
- `source-verification.json` checks every archer and hammer sprite/audio hash and confirms that the local extracted datapack matches the APK payload.

This uses the APK's arrow-and-target visual, not a new archer character. The native 9-second pose timeline is sampled to fit a 1.24-second gameplay action: 0.16-second anticipation, 0.42-second row flight, 0.5-second elastic settling, and 0.16-second fade. Travel distance follows the real row geometry. This pacing is a local gameplay adaptation, not a claim of frame-for-frame matching to a separate archer video. `ArrowFallAnimation` is included as an editable source asset but is not played by the row-clear sequence.

## Runtime behavior

`ReferenceArcherSequence` owns the visuals and clock. `ReferenceBoosterBar` retains inventory, cell damage, blocker rules and board resolution. The arrow sweeps every crossed cell once, including gaps and large frame steps. The bottom bar remains visible. Pause freezes the clock, particles and owned audio; cancellation disables and destroys owned effects and stops its sound. Missing resources prevent stock being spent. All audio uses the existing sound preference and audio director.

The animation importer is available at **Royal Throne > Reference Gameplay > Import Archer Animation VFX and SFX**. `ReferenceArcherImporter.Build` is repeatable and verifies imported curves after Unity serialization. Particle IDs use offset 390000000 to avoid earlier imports. APK gameplay scripts and services are not imported.

## Verification and review

- `Review/checks.txt`: actual Unity QA board tests, including synthetic mouse input through the normal board input path, top/center/bottom rows at 30/60/15 FPS, sparse-row sweeps with large time steps, no early/duplicate hits, wire blocker clearing, stock and move accounting, refill ownership, pause, cancellation and profile restoration.
- `Review/media-checks.txt`: native archer and hammer sound playback, mute, pause/resume/cancel behavior, and hammer impact emission.
- `Review/hammer-regression-checks.txt`: fresh run of the existing hammer gameplay suite.
- `Review/Archer-Gameplay.mp4` and `Archer-Gameplay-contact.jpg`: actual Unity Game-view capture on a synthetic QA board. The video uses a separately mixed original arrow cue at release; it is not an audio loopback recording of the complete Unity mix.
- `../../../Archer-Animation-VFX-SFX.unitypackage`: reusable archer media package; game-specific runtime integration remains in the project scripts.

No Android rebuild or physical-device performance test is included in this change.
