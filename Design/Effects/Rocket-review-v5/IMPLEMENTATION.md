# Royal Rocket FireExhaust v5

This review contains the verified Rocket-only source snapshot, focused Play Mode results, and representative mid-flight captures for the seven-cell FireExhaust pass.

## Production changes

- Rocket motion remains `10` board cells per game second.
- Flight duration remains `travelDistance / rocketSpeed`; no duration clamp was added.
- FireExhaust lifetime is `7 cells / actualRocketSpeed` for both axes.
- The continuous plume now uses a compact gold nozzle, a narrower white core, retained yellow/gold flame through the body, spatially sampled four-point stars, and a late soft white smoke fade.
- Spatial sampling is every `0.075` cell so 10-cells-per-second travel cannot open frame-dependent gaps.
- Smoke particles are emitted from the travelled path in world space, remain transparent through the flame body, expand in the final two cells, and fade without a detached cap.
- Match-4 Rocket creation uses the existing `Item.ChangeTypeCor` connection and animates the existing item for 0.48 seconds with scale overshoot, orientation-aware squash/stretch, a small rotation settle, and the existing sparkle pop.

No Rocket speed, board rule, UI, level, navigation, or other booster behavior was changed.

## Review contents

- `checks.txt`: focused half-speed and full-speed results.
- `previews/`: one representative horizontal and vertical mid-flight frame at each required speed.
- `verified-source/`: exact source and material snapshot used by the passing Unity run.

