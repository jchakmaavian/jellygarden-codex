# Rocket line-clear revision 3

Scope: rocket presentation, screen exit paths, and the existing QA rocket replay. Source rocket images and metadata, EffectsRig prefab, Item/Square/LevelManager, generic fracture service, gameplay scene, HUD source/layout, and sorting-layer settings remain unchanged from this revision's starting point.

## Rendering and motion

Active heads and custom particle emitters live in a dedicated, unmasked GameplayVFX container at scene root. World VFX remain on Default; the existing royal HUD is ScreenSpaceOverlay order 20 and the existing pause/menu canvas uses the higher UI sorting layer. No manual HUD avoidance or projectile hiding is used.

After 0.22-second anticipation, each head travels at constant speed. Camera viewport edges determine offscreen retirement, with 2.2 extra cell widths to carry the nozzle flame completely out. The board portion retains its 0.8–1.2-second tuning; total visible flight depends on distance to the screen edge. Cell contact, the 0.055-second local flash lead-in, and existing gem/blocker destruction calls are preserved.

## Fire layers

- Original two-headed Royal Throne art split into cached sprite views; pixels unchanged.
- Animated nozzle exhaust and a continuous gold/white flame ribbon, approximately one cell wide.
- The ribbon spans launch point to moving head and stays fed until exit, preventing the launch area from going empty. It fades smoothly over 0.68 seconds after exit.
- White core trail: 0.32 seconds, 0.30-cell width.
- Additive orange glow trail: 0.70 seconds, 1.22-cell width.
- White/gold stars: 0.45–0.68 seconds, emitted by travelled distance at 0.1-cell intervals.
- Lingering white/gold stars and burning flecks are replenished along the older travelled section while the head remains active.
- Burning embers: 0.52 seconds. Existing per-cell debris remains color/type matched.
- Gold flame body uses premultiplied blending to preserve color; core, glow, nozzle, impacts and sparks remain additive. No AllIn1 effect is called by the rocket sequence.

All 32 projectiles prewarm their renderers, with four fixed custom particle emitters. Retirement retains the pooled slot until its tails expire. QA replay waits for the rocket visuals to finish before another loop.

## Verification

Final Play Mode regression: 722 checks passed at 0.5× followed by 1×, with no runtime errors during the run. Verified horizontal/vertical, blockers, chained rockets, cascades, left-edge launches, starts under the HUD and above the bottom bar, simultaneous opposite rockets, and repeated visual previews. Actual speed remained correct through each flight. HUD/rail pixel checks stayed unchanged while vertical rockets passed underneath. The original board, coins and lives were restored. Test runner uses a disposable 8×8 board, restores the original board/state, and disables the QA menu's competing speed update only during the Lab review. It checks actual time scale throughout each flight. Captures are visual evidence, not a phone GPU/FPS benchmark.

Release-style C# compilation with QA disabled passed; inherited deprecation/serialization warnings remain in the project. No mobile GPU benchmark or device build was performed.

Artifacts: `checks.txt` contains the full assertions; `capture-manifest.json` identifies the final-run images; `rocket-v3.patch` contains only this revision relative to `before/`; preservation and changed-file SHA-256 manifests accompany it.
