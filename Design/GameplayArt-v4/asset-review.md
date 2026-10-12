# Asset mapping and visual review

Read-only audit completed on 14 September 2026. Every source file was inventoried, and all 79 bitmaps were viewed through the five numbered contact sheets. Production assets, scenes, importers and code were not changed.

## Findings

- 81 files total: **77 WebP sprites, 1 PNG preview, 1 JPEG preview, README and manifest**. Total source size: **4,515,299 bytes**; WebP sprites alone: **3,960,470 bytes**.
- All 77 sprites decode with alpha and contain transparent pixels. Both previews are opaque. No animated images were found.
- Measured dimensions, alpha-visible bounds, byte counts and SHA256 checksums match all 77 manifest entries. The manifest's source-resolution statement says the 512px exports were normalized/upscaled from approximately 180–256px generated atlas objects; that statement is provenance metadata, not independently verified generation history.
- **No prefabs, materials, shaders, scenes, Unity `.meta` files, 3D models, sound files or animation clips are supplied.** This is a static art pack.
- [Full inventory](inventory.json) includes every file and subfolder, dimensions, alpha statistics, bounds at three thresholds, source metadata, checksums and contact-sheet membership. [Readable inventory](inventory.md) lists every measured bitmap.

## Best gameplay mapping

This mapping keeps the current game's six color IDs, power types and mechanics while replacing the more ornate artwork with the pack's simpler silhouettes.

| Existing role / color ID | Pack asset | Contact ID | Decision |
| --- | --- | --- | --- |
| Ruby / 0 | `01_match_pieces/block_red.webp` | 001 | Broad red block; clear shape and highlight at small size |
| Shield / 1 | `01_match_pieces/shield_blue.webp` | 005 | Broad blue shield with minimal trim |
| Crown / 2 | `01_match_pieces/crown_yellow.webp` | 002 | Simple yellow crown, visibly distinct from red and blue |
| Leaf / 3 | `01_match_pieces/leaf_green.webp` | 004 | Keep diagonal leaf silhouette |
| Lily / 4 | `01_match_pieces/crystal_magenta.webp` | 003 | Magenta crystal removes small ornamental details |
| Vase / 5 | `06_optional_extras/porcelain_vase.webp` | 072 | Available sixth-color fallback; preserve six-color levels rather than silently reducing their palette |
| Horizontal rocket | `02_powerups/rocket_horizontal.webp` | 009 | Use its authored horizontal orientation |
| Vertical rocket | `02_powerups/rocket_vertical.webp` | 010 | Use its authored vertical orientation; avoid applying the old extra 90° rotation |
| TNT / PACKAGE | `02_powerups/bomb_barrel.webp` | 006 | Prefer barrel over round alternative: more distinct from color ball |
| Color orb / BOMB | `02_powerups/color_ball.webp` | 007 | Prefer dotted ball over pinwheel: clearer distinction from propeller |
| Helicopter | `02_powerups/propeller.webp` | 008 | Static combined object; existing flight/spin behavior still needs runtime animation |
| Ingredient key | `06_optional_extras/royal_key.webp` | 074 | Large simple gold silhouette |
| Ingredient chest | `06_optional_extras/treasure_chest.webp` | 076 | Bold blue/gold chest with visible lock |
| Crate / solid block | `06_optional_extras/wooden_crate.webp` | 077 | Use only on the matching existing obstacle behavior |
| Ice | `06_optional_extras/ice_block.webp` | 070 | More solid silhouette; keep existing layer/health logic |
| Stone | `06_optional_extras/stone_block.webp` | 075 | Existing indestructible stone role |
| Ivy/spreading obstacle | `06_optional_extras/ivy_stone_block.webp` | 071 | Appropriate candidate if its existing mechanics represent the same obstacle |
| Reward coin | `06_optional_extras/reward_coin.webp` | 073 | Gameplay reward effect; home resource art is outside this audit's mapping |
| Board | `04_board/cell_light.webp`, `cell_dark.webp` | 014–015 | Quiet lavender cells let the pieces dominate |

The vase is narrower and visually more detailed than the five primary pieces. If a later design chooses only five colors, that requires an intentional level/difficulty change; it is not an art-import consequence.

## Sizing and composition

The basic pieces and powers use 512 × 512 canvases, but their largest visible dimension is usually **430px (83.98%)**. Fitting the full texture rectangle to 94% of a cell would produce only **about 79% visible occupancy** (`0.94 × 430 / 512`). Fit the opaque sprite bounds instead, preserving the original centered pivot and the cell's input area. Otherwise this import can repeat the user's small-piece complaint despite a nominally large sprite size.

Core bounds are nearly centered. Crown and crystal are intentionally wider than tall, while the vase, key and vertical rocket are intentionally narrow. Preserve these proportions; avoid filling both axes independently.

The supplied 64px preview shows clean differentiation among the five primary shapes. Its filled grass competes with the green leaf in occupied cells, so keep grass underneath the piece and verify real playable-cell contrast. The flat board cells are a stronger visual foundation than a saturated cyan checkerboard behind every gem.

## Effects and frame decisions

- The 42 effects comprise **28 individual fragments, 4 individual sparkles, 5 static burst groups and 5 other effect sprites** (ring, flash, two trails and sparkle cluster).
- Use the individual fragments for independent rotation, gravity and downward travel. The five burst-group images are static arrangements; moving one as a whole would keep every chip locked together.
- Prefer the sharp fragment variants for prominent chunks. `leaf_06` (045) and `red_04` (056) are particularly soft; `leaf_02`, `leaf_04` and `magenta_03` are also softer than neighboring variants. They can be secondary flecks, but are weaker choices for the main falling fragments.
- `clear_ring` (030), `hit_flash` (031), `sparkle_01`–`sparkle_04` (060–063) and line trails (064–065) are suitable textures for the existing pooled native effects. They do not replace timing, emission, sound, blend-mode, impact or gravity logic.
- The hollow nine-slice frame (016) is suitable for rectangular boards. Four 40 × 40 outer corners and four straight edges are supplied. **No dedicated inner/concave corners are included**; preserve or extend the existing contour renderer for irregular boards rather than stretching one rectangle over the whole shape.
- Grass has three static variants, not an authored animation or proven damage-state sequence. Do not stack all three states simultaneously or invent new durability rules from filenames.
- `bomb_round_alternative`, `color_pinwheel_alternative` and `bubble` are alternatives/optional content. Importing them does not require adding new power types or mechanics.
- Both files in `previews/` are reference sheets and should remain outside runtime sprite resources.

## Numbered contact sheets

1. [Core pieces, powers and grass](contact-01-pieces-powerups-grass.webp)
2. [Board tiles and every frame part](contact-02-board.webp)
3. [Effect groups, trails and optional extras](contact-03-effects-extras.webp)
4. [Every individual fragment and sparkle](contact-04-particles.webp)
5. [Both supplied previews](contact-05-previews.webp)

All bitmaps appear exactly once; global IDs link the sheets to the inventory. The checkerboard is audit presentation only and is not baked into the source sprites.
