# Royal Rocket: faster flight and separate visual states

Implemented and verified in the independent JellyGarden-Codex Unity project on 16 September 2026. The original J: Unity project was not changed.

- Flight is 15 cells/second, exactly 1.5 times the previous 10. Launch anticipation is 0.12 seconds instead of 0.18. Travel still uses actual distance and constant velocity, without duration clamps or shortened paths.
- Idle horizontal and vertical Rockets retain their original two-headed artwork, fitted to 78% of the actual cell. Passing Rockets use four dedicated directional textures at 0.64 cell length, with their pivots at the trailing nozzle. Flight objects and scale are independent of board Items.
- Match-4 replacements now spawn in their own cell. Their first-frame reveal cannot be overwritten by Item.Start randomization, falling initialization or caching an animated scale. The pop, small rotation, squash and settle take approximately 0.4 seconds.
- FireExhaust design is preserved. The shader, materials and textures have identical hashes to the pre-task files. Particle density, gradients, widths, spatial sampling and seven-cell target are unchanged. Existing speed-derived lifetimes automatically change from 7/10 to 7/15 seconds.

Directional source files were inspected individually in `J:/Unity Projects/Match3-Games_ArtSource/Match-3-JC/Royal_Throne_Gameplay_Assets/02_powerups/rocket_4_direction`. Their WebP images were decoded to lossless PNG without resizing, repainting or changing decoded RGBA pixels. Existing idle source art and prefab references were preserved.

## Verification

Unity 6000.5.10f1 compiled the changes. Only RoyalRocketChecks was run, using the existing disposable board and the VFX Lab at time scales 0.5 and 1.0. The test uses the real Match-4 detector, item destruction/replacement path and Rocket activation path; it does not run the full game regression.

All 92 checks passed, with no runtime errors during the final run:

- Horizontal and vertical Match-4 creation, first visible frame, in-cell position and original scale restoration.
- Reveal durations: 0.404 / 0.394 seconds at half speed and 0.432 / 0.426 seconds at normal speed, measured in game time.
- Both idle orientations fit 78% of their cell; reveal overshoot remains inside the cell.
- Left, right, up and down use their corresponding artwork without rotating a shared sprite.
- Observed flight speed is 14.99–15.01 cells/second; nozzle attachment and seven-cell exhaust length pass in both axes.
- Rockets, trails, particles and colored debris finish and release, with no pooled VFX object growth.
- Coins and lives are preserved; the original gameplay board is restored after the disposable fixture.

`checks.txt` contains the complete results. `*-reveal-*.png` and `*-mid-*.png` are actual Game view captures. The level-one host's tutorial hand/dimming remains visible in these captures; it is not part of the Rocket visuals. No tutorial or production UI code was changed.

The first run's cleanup assertion was premature at the faster flight speed: the Rocket tail had ended while debris was still falling. The fixture now waits for debris to finish within its bounded timeout before asserting that its pool is empty. Debris behavior was not changed.

## Backup contents

`rocket-only.patch` records this task's text changes against the existing working files at task start. `verified-source/` contains the seven edited files, their existing metas, and four new directional PNGs with metas. `source-hashes.txt` identifies the verified live files. These snapshots follow the existing Rocket review backup convention and do not stage unrelated pending project work.
