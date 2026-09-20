# Color Ball live QA flow correction — 20 September 2026

The user's gameplay recording exposed two missed paths in v1: ordinary Color Ball activation used the legacy clear timing, and QA spawning assigned a normal gem color instead of the Color Ball sentinel. That allowed unintended color matching/chains. The previous isolated Rocket-combo test did not cover these QA entry points.

## Installed correction

- Ordinary activation now uses the approved Boosting spin-up, charged loop, separate beam/glow/impact effects, and authored finish. Each ordinary target clears after its beam arrival. Refill waits for completion.
- Color Ball + Rocket continues to convert targets through Item.ChangeType, holds generated Rockets until finish, then hands them to the existing Rocket activation system.
- QA spawning uses the real Color Ball creation path and color 555. QA preview/activation controls allow Reveal to settle into Idle before activating.
- All 378 approved animation/VFX source frames remain byte-identical to v1. No Rocket artwork, Rocket VFX, TNT, UI layout, level data, or other booster logic changed.

The live files are installed in this independent Unity project's Assets folder. `ColorBall-QA-Flow-Fix.zip` contains the four tested source files and their metadata. `incremental.patch` records this task's changes against the included pre-edit baseline. This is a follow-up to the v1 integration package, not a standalone Unity project.

## Verification

Unity 6000.5.10f1 compiled the changed sources and completed the focused Play Mode suite through actual RoyalQaService command entry points, with the live match resolver/refill enabled:

1. **Color-ball effect**, 0.5×: Reveal, Idle, charged loop, beams, finish, cleanup, refill.
2. **Spawn color ball → Activate selected**, 1×: three spawned balls; activation leaves the two unselected Color Balls intact. No false color matching/chaining.
3. **Combine selected**, Color Ball + horizontal Rocket, 1×: reveal/idle, conversion, finish, existing Rocket launches, cleanup, refill.

All assertions passed in `checks.txt`, including no new runtime errors, no falling/refill underneath the active Color Ball, released Color Ball/target VFX pools, and restored normal player data. The gameplay board accepted input after refill. No full-game QA was run; unrelated combos were not retested.

Reproduce in Play Mode using **Tools → Royal Throne → Color Ball → Verify QA activation and Rocket combo** (Ctrl+Shift+F5). The suite temporarily uses the existing isolated QA save session and restores it afterward.

## Evidence

- `spawn-activate-full.mp4`: recorded live standalone QA activation at 1×.
- `rocket-combo-full.mp4`: recorded live creation/conversion/Rocket handoff at 1×.
- PNGs capture Reveal, Idle, beams, finish, handoff and settled board. `effect-half-*` is the 0.5× case.
- Movies encode captured Game-view frames using recorded wall-clock timestamps; they are not animation mockups. Still-image capture can cause brief recording stutters.

The historical dirty workspace contains other ongoing work. Only this focused source snapshot, patch and evidence are included in this review backup.
