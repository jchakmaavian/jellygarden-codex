# Cell-local match destruction

The ordinary-match resolver now marks each confirmed gem with its match size. `Item.JuiceDestroyCor` hands those gems to `RoyalMatchBreakService` before target collection can hide their original renderers. The original matching, special creation, target, scoring and refill systems still own the outcome.

Every cell emits the AllIn1 Ice Impact rings and crystal sparks through a shared world-space copy of the existing derived prefab. Large glow/flare layers are omitted to keep each impact inside approximately one cell. The old group-center Ice call is removed from ordinary match resolution. The original Ice/Fire preview pool and powerup destruction path are retained.

A fixed pool of 256 sprite snapshots keeps the gem visible through its pop. Ice starts at 0.025 seconds; the gem compresses and fades between 0.105 and 0.20 seconds. Fragments begin at 0.09 seconds and arrive in small batches through 0.22 seconds. Cell staggering is capped at 0.04 seconds. All fragments expire at 0.80 seconds, fading and shrinking over approximately the last third of their life. Board refill can proceed after the gem pop while the separate visual tail continues.

The existing `RoyalGameplay/Toolkit/BurstPieces` library supplies the individually sliced, painted red/blue/gold/leaf/magenta fragments from `RoyalGameplayV4/05_effects/bursts`. The sixth, porcelain gem uses small sections of its own texture. Base counts are 14/8/8/14/10/8 per gem; Match-4 adds two and Match-5+ adds four. Small and medium fragments launch radially, tumble, and fall under gravity scaled to the grid pitch. Horizontal travel is bounded by the initial velocity and short lifespan. Reduced motion uses four fragments.

Forty particle systems are built at scene load and reused: eight toolkit layers plus 32 painted-fragment emitters. Only the two ring and two crystal layers emit for each Ice impact. Fragment meshes/materials are cached. There are no per-match prefab instantiations, per-fragment GameObjects, colliders or rigidbodies. Decoration uses a private deterministic hash, preserving gameplay random state. Zero game speed and pause freeze playback; replacing or leaving the board clears it.

## Verification

Unity 6000.5.10f1 Play Mode: **107 checks passed, zero runtime errors** on 15 September 2026. Exact horizontal/vertical Match-3, Match-4, Match-5, five-cell L/T shapes, simultaneous matches and a constructed second-wave cascade passed. The particle checks verified visible gem retention, eight blue chips, downward velocity, local spread, pause, fade/shrink, cleanup and 64 simultaneous cell impacts. Rendered sequences were reviewed for local shape coverage and readable falling fragments.

The runtime sources also compile successfully with `ROYAL_DISABLE_QA` using Unity's bundled compiler (exit 0). Existing unrelated compiler warnings remain; no new compile error was introduced. Focused diff whitespace checks passed.

Run `Tools > Royal Throne > Effects > Verify per-cell match breaks` in a settled Play Mode board (Ctrl+Shift+F10). It uses the existing disposable regression board and restores the original board, resources, targets and random state in `finally`. See `checks.txt` for the latest actual result and the adjacent PNG sequence for rendered frames.

This is an Editor Play Mode review, not an Android device performance benchmark. Existing broad QA and Fire review evidence remains in the v4 and QA-Mode folders; it is not represented as a new run for this change.
