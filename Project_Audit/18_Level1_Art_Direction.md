# Level 1 Asset Audit + Art Direction Foundation

**Project:** `J:/Unity Projects/Match3-Games` · JellyGarden / Match 3 Jelly Garden Kit v2.5.2 · Unity `6000.5.10f1`
**Task:** `PLAN.md` CURRENT TASK — Asset Audit + Art Direction Foundation (READ ONLY)
**Status:** Discovery and decision only. No asset, scene, prefab, script, material, VFX, animation or project setting was modified.
**Companion documents:** `Project_Audit/01–17` (technical & visual audit set)

---

## Scope decisions confirmed before writing

- **Lighting basis = bake everything.** No runtime lighting or post-processing is assumed. URP 2D is currently active, but `Assets/Settings/JG_2DRenderer.asset:57` has `m_PostProcessData: {fileID: 0}` (bloom cannot render), there are **zero `Light2D`** objects in either scene, and grading is LDR (`m_ColorGradingMode: 0`). Baking is the only basis that survives Phase 0 regardless of which way the URP decision goes.
- **Production scope = all 6 candy colours**, even though Level 1 renders only 4. Designing 4 in isolation risks a palette that breaks when colours 5–6 arrive.

## Correction to the task brief

Level 1 is `COLOR LIMIT 4`, and `Item.GenColor()` (`Assets/JellyGarden/Scripts/Item.cs:169-227`) takes the **first N** of the `items[]` array — not a random subset. Level 1 therefore renders:

**`item_01` red bean · `item_02` orange disc · `item_03` purple berry · `item_04` blue star**

**Green jelly (`item_05`) and yellow (`item_06`) do NOT appear in Level 1.** The brief listed green jelly as a Level 1 piece; it is index 4 and is excluded.

Level 1 (`Assets/JellyGarden/Resources/Levels/1.txt`) is a 9×5 board, 6 moves, grid codes `10` and `20` only. It uses **only the jelly blocker** — no cages, solid blocks, double blocks or spreading blockers. Objectives are SCORE 100 + BLOCKS 4.

---

# 1. Asset Classification

Priority: **P0** = blocks the Level 1 benchmark · **P1** = needed for Level 1 completeness · **P2** = outside Level 1, do later.

## 1.1 Normal pieces
All 128×128, `textureType: 8`, `spriteMode: 1`, PPU 100, `maxTextureSize: 1024`, **uncompressed**, `spriteBorder` zero, `spritePackingTag: Pack1`.

| Asset | Path | Construction | Visual-quality problem | Reusable | Action | Pri |
|---|---|---|---|---|---|---|
| `item_01` red bean | `Assets/JellyGarden/Textures/Items/item_01.png` | Flat RGBA sprite | Baked drop shadow inside the same alpha; fixed upper-left key; 128 px ceiling | Silhouette only | **REDESIGN** | P0 |
| `item_02` orange disc | `…/Items/item_02.png` | Flat RGBA | Flat oval; lowest dimensionality in the set | Silhouette only | **REDESIGN** | P0 |
| `item_03` purple berry | `…/Items/item_03.png` | Flat RGBA | 7-sphere cluster — busiest silhouette, degrades worst when shattered | Concept only | **REDESIGN** | P0 |
| `item_04` blue star | `…/Items/item_04.png` | Flat RGBA | Strongest silhouette in the set; thin points clip under the circular shatter cut | Silhouette | **REDESIGN** | P0 |
| `item_05` green jelly | `…/Items/item_05.png` | Flat RGBA | Not in Level 1; same defects | Silhouette | **REDESIGN** | P1 |
| `item_06` yellow square | `…/Items/item_06.png` | Flat RGBA | Not in Level 1; weakest identity (generic rounded square) | No | **REDESIGN** | P1 |

**Shared defect (measured, not inferred).** Every candy bakes four things into **one flat RGBA layer**: a white upper-left specular, a lower-right core shadow, a **soft black drop shadow offset down-right past the silhouette**, and a dark keyline. Opaque bounding boxes measured at 105–121 px of the 128 px canvas (~92 % fill), with the bbox centre sitting **1–2 px right/down of the pivot** because of that drop shadow. No PSD/AI/SVG/XCF source exists anywhere in the project.

## 1.2 Special pieces

| Asset | Path | Construction | Problem | Action | Pri |
|---|---|---|---|---|---|
| `item_0N_stripes_horiz/vert` ×12 | `…/Textures/Items/` | Base candy **repainted** with fused white bands | Stripe is fused into the art, so any base change forces 12 repaints | **REDESIGN** | P1 |
| `item_0N_extra` ×6 (wrapped) | `…/Textures/Items/` | Wrapped bag + painted "Boom!" wordmark | Different silhouette family from the base candy; wordmark is un-localisable baked text | **REDESIGN** | P1 |
| `candy10_choco` (colour bomb) | `…/Textures/Items/candy10_choco.png` | Single shared rainbow ball; colour-agnostic by design (`bombItems[0]`, `Item.cs:235/981`) | Reads as a chocolate ball, not as the top of the hierarchy | **REDESIGN** | P1 |
| `ingredient_01/02` | `…/Textures/Items/` | Cherries / nut | Unreachable in Level 1 (`MODE 3`); array largely unused at runtime | **KEEP** | P2 |

## 1.3 Board surface — the weakest area in the project

| Asset | Path | Construction | Problem | Action | Pri |
|---|---|---|---|---|---|
| `square.png` | `…/Textures/Blocks/square.png` | 82×82 — **uniform `RGBA(0,0,0,128)`** | **Not artwork.** A constant 50 % black fill, every pixel including corners. No bevel, no rounded corners, no texture, no rim | **REPLACE** | P0 |
| `square_2.png` | `…/Textures/Blocks/square_2.png` | 82×82 — **uniform `RGBA(0,0,0,166)`** | Same; the "checkerboard" is two alpha values | **REPLACE** | P0 |
| `border_01.png` | `…/Textures/Blocks/border_01.png` | 3×83, **24bpp — no alpha channel**, flat `#FFD800` | Hard 3 px yellow line; cannot anti-alias against the background | **REPLACE** | P0 |
| `border_02/03.png` | `…/Textures/Blocks/` | 82×82 / 85×85 corner strokes | Same flat yellow vector line | **REPLACE** | P0 |

> **The board today is tinted darkness over `Textures/backgrounds/background_0X.png`, plus a yellow outline.** There is no board substrate, tray, inner shadow or vignette. This is the single largest gap between the current build and the quality benchmark, and it is invisible in an asset list because the files exist and import cleanly.

## 1.4 Blockers
All 82×82, PPU 100 → exactly one cell. All use built-in `Sprites-Default`.

| Asset | Path | Problem | Action | Pri |
|---|---|---|---|---|
| `block.png` (jelly) | `…/Textures/Blocks/block.png` | Real art (pale lavender frosted, centre `RGBA(208,219,238,133)`). **The only blocker in Level 1.** Must match the new material language | **REDESIGN** | P0 |
| `block_02.png` (2-hit) | `…/Textures/Blocks/` | Magenta panel stacked over `block` at sorting order 1. Not in Level 1 | **REDESIGN** | P1 |
| `wireBlock.png` | `…/Textures/Blocks/` | Cyan cracked-ice cage. Not in Level 1 | **REDESIGN** | P2 |
| `solidBlock.png` | `…/Textures/Blocks/` | Wood plank. Not in Level 1 | **REDESIGN** | P2 |
| `thriving_block.png` | `…/Textures/Blocks/` | Baked "Choco" wordmark — un-localisable | **REDESIGN** | P2 |
| `undestroyable.png` | `…/Textures/Blocks/` | Grey slab. **Used by none of the 100 shipped levels** | **KEEP** (dormant) | P2 |

## 1.5 Level 1 HUD

| Asset | Path | Problem | Action | Pri |
|---|---|---|---|---|
| `gui.png` (top plank) | `…/Textures/menu/game_menu/gui.png` | 800×182 art on a canvas that is always exactly **720 units wide → 40 px cropped each end**; score and target recesses sit flush against the crop boundary | **REDESIGN** | P1 |
| `gui_bottom.png` | same folder | 800×171, same 800/720 crop | **REDESIGN** | P1 |
| `score / moves / target / level / timer_txt.png` | same folder | **Baked word-art.** Cannot be localised or restyled without repainting | **REPLACE** (→ live text, Phase 11) | P2 |
| `star_bar.png` + `gui_for_star_bar_01.png` | same folder | Fill renders **278×66 instead of 278×30** (split Y anchors) then is ~72 % hidden by an 18 px slot in `gui.png`. **This is a scene bug, not an art bug** | **REFINE** (fix RectTransform, keep the PNG) | P1 |
| `star_for_bar_01/02.png` | same folder | 41×40 / 41×39, drawn 1:1 | **REFINE** | P2 |
| `pause_button.png` | same folder | 69×69 1:1; consistent glossy-chip idiom | **KEEP** | P2 |
| Booster icons ×5 | `…/Textures/Boosts/` | 125×124 drawn 1:1; coherent glossy-chip idiom | **REDESIGN** (Phase 5) | P2 |
| `boost_over_button.png` (lock) | `…/Textures/Boosts/` | 125×124 art in a **100×100 rect** — does not fully cover the icon it locks | **REFINE** (rect fix) | P2 |
| `boost_nomber` / `boost_plus_button` | `…/Textures/Boosts/` | 59×62 drawn 1:1 | **KEEP** | P2 |

## 1.6 Objectives / targets (Level 1)

| Asset | Path | Problem | Action | Pri |
|---|---|---|---|---|
| SCORE target icon | `…/Textures/map/star2.png` (**36×36**) | **World-map star reused as a HUD icon.** 36 px source forced into a 128 px rect at 0.3 scale → net upscale; the lowest-resolution asset on screen | **REPLACE** (create a dedicated score/star icon) | P1 |
| BLOCKS target icon | `…/Textures/Blocks/block.png` (82×82) | Correctly reuses the board sprite — good practice. Must be redesigned in lockstep with the jelly | **REDESIGN** (with `block.png`) | P0 |
| `TargetBlocks.prefab` | `…/Resources/Targets/TargetBlocks.prefab` | Icons drawn at 38.4 px after a 0.3 scale chain; **`m_PreserveAspect: 0`** → any non-square icon added later will distort | **REFINE** | P1 |

## 1.7 `Assets/GameRedesign/` — 52 files, 38 MB, **100 % orphaned**

Verified: all 65 GUIDs extracted and grepped across every `.unity`, `.prefab`, `.asset`, `.mat`, `.cs`, `.controller`, `.anim`, `.shader`, `.json` and `ProjectSettings/` file. **Zero matches.** No `Resources.Load` call targets any of it. No Addressables package exists to hide an indirection.

| Asset group | Path | Value | Action | Pri |
|---|---|---|---|---|
| King expressions ×6 — neutral, pleased, celebrate, impressed, concerned, sad | `GameRedesign/Character/Runtime/King_*.png`, 1536² RGBA | Production-grade mascot art with a usable emotional range | **KEEP as source** → **REFINE** for use | P2 |
| King concepts + `king_v1..v4` | `Character/Expressions/`, `Character/Source/` | `king_v1.jpg` is byte-identical to `king_neutral.jpg` — v1 was the selected design. v2–v4 unused | **KEEP** (archive) | P2 |
| `Character/Turnaround/` | — | **EMPTY.** No side or 3-quarter views; the king can only face camera | **CREATE** if animation is wanted | P2 |
| HUD kit — `HUD_Card / Medallion / Portrait / Rail` | `GameRedesign/UI/` + `/Resources/` | Coherent heraldic direction. **`Resources/HUD_Card.png` carries `spriteBorder {165,140,165,185}` — the ONLY 9-slice in the entire project** | **KEEP as reference**; preserve that border | P2 |
| `Coin_Gold.png` | `Reward/` + `Resources/` | Reward art, unused | **KEEP** (archive) | P2 |
| `REF_*` ×11 | `GameRedesign/Resources/` | Small crops sliced from a reference screenshot to define a target HUD layout | **REFERENCE ONLY — must not ship** | P1 |
| `RefSource/RefGame.png`, `RefKing.png` | `GameRedesign/RefSource/` | **`textureType: 0` (Default), not Sprite** — moodboard screenshots. `RefGame.png` is 941×1672, a phone-screen aspect | **REFERENCE ONLY — must never be traced, sampled or shipped** | P1 |

> **Two flags on this folder.**
>
> **Build cost.** `GameRedesign/Resources/` is a magic `Resources` folder: Unity packs all 20 files into every build *regardless of references*. 15.56 MB of source PNG, 8 of them `textureCompression: 0`. Estimated order of **~12 MB of texture data shipping for content that is provably never loaded** (estimate — depends on target compression format, not measured against a build). The other subfolders are stripped by the dependency walker and cost nothing.
>
> **IP.** `RefGame.png` is a screenshot of a reference title. Per `CLAUDE.md`, reference titles are a quality benchmark only — for responsiveness, hierarchy, timing, weight and polish. This image must not be traced, sampled, or included in any build.

---

# 2. Level 1 Art Direction Specification

**Governing constraint: everything is baked.** No runtime lights, no post-processing, no normal maps. All dimensionality is painted into the sprite and must be internally consistent, because nothing can be re-lit later.

## 2.1 Rendering direction — 2D with baked 2.5D
Flat sprites carrying **painted volume**. Pieces should read as moulded objects seen from directly above, not as illustrations. No perspective, no foreshortening, no implied camera tilt — the camera is orthographic and pieces tumble during destruction, so any baked perspective breaks the moment a piece rotates.

## 2.2 Shape language
- **One primitive per colour**, separable **in greyscale at 60 px**. This is the real test; colour-blind readability and motion readability both follow from it.
- Compact, convex, radially balanced. **Avoid multi-lobe clusters** (current purple berry) and **thin protrusions** (current star points) — both disintegrate under the circular shatter cut.
- Consistent corner radius across the set; no sharp vertices.
- Occupy **85–92 % of the frame**. Below 85 % the piece looks lost in its cell; above 92 % neighbours visually touch.

## 2.3 Candy / jelly material language
Exactly two materials, applied consistently:
- **Jelly** — translucent, soft internal falloff, light appears to penetrate the body, slightly darker at the silhouette edge.
- **Hard candy / glaze** — opaque core, tight high-contrast specular, crisp edge.

Do not introduce a third. The contrast between these two is what separates pieces at speed.

## 2.4 Lighting direction
- **Single fixed key light, upper-left, ~135°**, identical on every piece, blocker, cell and HUD chip.
- Soft ambient fill from the lower-right at ~25 % key intensity, so the shadow side never goes dead.
- **No second key and no coloured rim from a second direction.** With baked lighting, any inconsistency reads immediately as a wrong asset.

## 2.5 Specular / highlight treatment
- **One dominant highlight**, upper-left, covering 12–18 % of the piece area.
- Optional **single thin crescent** along the upper edge, glaze material only.
- **Never place a highlight in the lower-right quadrant.** That is the shadow side, and the shatter cuts its centre chunk from the middle — a stray highlight there becomes an orphaned bright dot on a shard.
- Highlights must be **soft-edged, not hard white discs.** The current hard specular is what shimmers under the idle breathing animation, worsened because mipmaps are disabled project-wide.

## 2.6 Bevel / depth treatment
- **Soft inner bevel** at 6–10 % of the piece radius: brighter along the upper-left arc, darker along the lower-right arc.
- **No hard black keyline.** Separation from the board comes from value contrast (§2.10), not outline. The current keyline flattens the pieces and fights the bevel.
- Interior form must still read when a piece is cropped to ~30 % of its area. This is what makes a shard look like *part of a candy* rather than a coloured chip.

## 2.7 Saturation and colour rules
Anchor every piece to the serialized palette at `Assets/JellyGarden/Scenes/game.unity:6844` (`scoresColors`), because it already drives score popups and VFX tint:

| idx | Colour | Hex |
|---|---|---|
| 0 | red-orange | `#FB5001` |
| 1 | golden yellow | `#FFC116` |
| 2 | magenta | `#ED0DE9` |
| 3 | blue | `#299DFF` |
| 4 | yellow | `#FFFF26` |
| 5 | green | `#25DB00` |

- **Indices 1 and 4 are dangerously close** (golden yellow vs yellow). If both ship, separate them by material and silhouette, not hue alone.
- Pieces: high chroma. **Board: ≤25 % of piece chroma.** Background: ≤15 %.
- Value: pieces occupy the upper 60 % of the value range; the board sits in a narrow mid-dark band so no piece is ever darker than the surface behind it.

## 2.8 Shadows / contact shadows
- **The contact shadow must NOT be baked into the piece sprite.** This is the single most important technical rule in this document.
- Current art bakes a drop shadow into the same alpha as the candy. Two measured consequences: the opaque bbox sits off-pivot, and **`CandyShatterService` tears the shadow into the shards**, so debris flies with shadow fragments attached.
- The contact shadow belongs on the **board cell**, or as a separate pooled sprite beneath the piece — never inside the piece texture.

## 2.9 Silhouette / readability rules
- Distinguishable at **60 px** (the smallest measured on-screen size), in greyscale, and under motion blur.
- No two pieces share a silhouette family.
- The silhouette must survive being cut into 5–7 chunks. The shatter cuts a **circle** from the sprite bounds, so art that does not fill that circle produces chunks of empty space.

## 2.10 Board-vs-piece contrast
- The board reads as a **recessed tray**: subtle inner shadow at the top edge, faint vertical gradient, low-chroma neutral.
- Checkerboard delta: **6–10 % value difference** between alternating cells. The current 128 vs 166 alpha (~15 %) is too strong and competes with the pieces.
- Board frame: a soft rounded rim with a 1–2 px light catch upper-left, not a flat line.
- **Rule of thumb: squint at the screen. If the board is as visible as the pieces, the board is wrong.**

## 2.11 Normal-piece hierarchy
All six normal pieces are **peers** — equal visual weight, equal highlight area, equal bevel depth. No colour may read as "the important one".

## 2.12 Special-piece hierarchy
Escalate by **added energy, never by added size**. Every piece must stay inside one cell.

| Tier | Treatment |
|---|---|
| Normal | Base material, one highlight |
| **Striped** | Base silhouette + directional banding + soft internal glow along the stripe axis. Must read *which axis* at 60 px |
| **Wrapped** | Base silhouette + enclosing shell + tighter, brighter specular. **Drop the baked "Boom!" wordmark** |
| **Colour bomb** | Distinct silhouette (correctly colour-agnostic today) + highest specular energy in the game + subtle internal iridescence |

Because bloom is unavailable, "glow" must be painted into the sprite or delivered as an additive pooled sprite through the existing `MatchVFXService` — never assumed from post-processing.

## 2.13 Compatibility requirements for animation and VFX
Non-negotiable, derived from systems already in place:

1. **No baked drop shadow** (§2.8) — `CandyShatterService` shreds it.
2. **Radially compact, 85–92 % frame fill** — the shatter cuts a circle from sprite bounds.
3. **Interior colour defined across the whole body.** Shard cut faces show cropped albedo; a piece detailed only at its rim produces hollow-looking chunks.
4. **Soft-edged highlights** — hard speculars shimmer under the idle breathing and squash animations, worsened because `enableMipMap: 0` everywhere.
5. **Pivot stays centred (0.5, 0.5)** — squash, spin and tumble all rotate about it.
6. **Preserve the `Item` root/child split.** Authored clips target the child `Sprite`; code juice animates the root. Art changes must not require re-parenting.
7. **Keep the world footprint at 0.768 units.** Author at **256×256 and import at PPU 200** — `256 / 200 × 0.6 = 0.768`, identical to today, so **zero code or prefab changes**. Board and blockers: **164×164 @ PPU 200** = 0.82 units.
8. **Enable mipmaps** on the new set — pieces are minified ~2× at 720×1280 and shimmer during motion.

---

# 3. Recommended Asset Production Order

Follows `PLAN.md`'s Asset Review Order, resequenced so Level 1 closes first.

1. **Board cell + checkerboard + frame** (P0). Counter-intuitive but correct: the board is currently a flat black fill, so pieces cannot be judged against it. Everything downstream is evaluated on top of this.
2. **The 4 Level 1 candies** — red bean, orange disc, purple berry, blue star (P0).
3. **Jelly blocker `block.png`** and its target-icon reuse (P0). Completes the Level 1 board.
4. **→ LEVEL 1 VISUAL GATE.** Approve before continuing.
5. Candies 5–6 (green, yellow) to complete the palette (P1).
6. Striped ×12, wrapped ×6, colour bomb (P1).
7. Destruction fragment treatment — validate shatter identity against the new art (P1).
8. HUD planks + the dedicated score-target icon (P1).
9. Remaining blockers: double, cage, solid, spreading (P2).
10. Booster icons (P2).
11. King/mascot integration + reward art (P2).

---

# 4. Assets to Reuse

- **Silhouette concepts** for red bean, blue star and green jelly — the three most distinct shapes in the set.
- **`scoresColors` palette** (`game.unity:6844`) — already wired to score popups and VFX tint.
- **The glossy-chip idiom** on `pause_button` and the booster icons — the most coherent visual language currently in the project.
- **`block.png` reused as its own target icon** — correct practice; keep the pattern.
- **All 6 king expressions** (`GameRedesign/Character/Runtime/`) — production-grade art with a usable emotional range.
- **`HUD_Card`'s 9-slice border** `{165,140,165,185}` — the only 9-slice authoring in the repo.
- **`Item.prefab` root/child rig, `MatchVFXService`, `CandyShatterService`, `JuiceConfig.asset`** — all sound; new art plugs into them unchanged.

# 5. Assets to Replace / Rebuild

- **`square.png`, `square_2.png`** — not artwork; uniform alpha fills.
- **`border_01/02/03.png`** — flat 3 px yellow line; `border_01` has no alpha channel at all.
- **All 27 sprites in `Textures/Items/`** — rebuilt at 256² with the contact shadow separated out.
- **`Textures/map/star2.png` used as a target icon** — a 36×36 map asset doing HUD duty.
- **`gui.png` / `gui_bottom.png`** — re-authored at the true 720 canvas width.
- **Baked word-art** (`score/moves/target/level/timer_txt.png`) — replace with live text at Phase 11.
- **`explosion_item_01–06`** (4 frames each, 163×163) — the legacy per-colour flipbook, already dead at runtime while `EnableJuice` is on.

# 6. Technical Constraints Affecting New Art

1. **No runtime lighting or post-processing.** Zero `Light2D`, `m_PostProcessData` null, LDR grading. All light is painted.
2. **No layered sources exist.** No PSD/AI/SVG/XCF anywhere — every shipped PNG is a flattened final. New art must ship with editable sources or this recurs.
3. **Author 256² @ PPU 200** (pieces) and **164² @ PPU 200** (cells/blockers) to hold the exact current world footprint with no code change.
4. **Resolution is currently borderline.** Candies draw at ~61–76 px at 720×1280, but **~150 px from a 128 px source on 1440×2960 — magnified and genuinely soft.**
5. **No mipmaps, no Sprite Atlases.** `Pack1`/`Pack2` are legacy tags that do nothing in Unity 6. Every sprite is its own uncompressed texture and its own potential draw call.
6. **Everything is uncompressed** (`textureCompression: 0`, no platform overrides) — headroom exists to raise resolution before compression becomes the constraint.
7. **Only two sorting layers** (`Default`, `UI`). Board order: cells 0 → jelly 0/1 → pieces 2 → blockers 3 → shatter 20 → sparkle 25. **Nothing on the board can ever draw over the HUD.**
8. **Recolouring candies requires a scene edit** — `scoresColors` is serialized in `game.unity` and drives score popups and VFX tint.
9. **Stripe art is fused into the base candy**, so each base change forces 12 stripe repaints. Consider authoring stripes as a separable overlay.
10. **HUD art is 800 px wide on a 720-wide canvas** — 40 px is cropped from each end today.
11. **`m_PreserveAspect: 0`** on target icons — any non-square icon added later will distort.
12. **`GameRedesign/Resources/` ships ~12 MB of never-loaded texture in every build.**

---

# 7. Recommended NEXT TASK

> **Phase 0 — Stabilize Technical Baseline**, narrowed to a single decision: **keep-and-repair, or revert, the URP 2D migration.**

**Why this rather than starting art production.** This audit locked a *bake-everything* art direction precisely because the renderer is currently ambiguous. That assumption is safe, but it is also a **ceiling**: baked-only art can never use bloom on special pieces or 2D lights for depth. Producing the full 27-sprite set under bake-only rules and *then* repairing the renderer would mean every specular and glow decision was made against the wrong target.

The decision is small and the evidence is already gathered:
- `Assets/Settings/JG_2DRenderer.asset:57` — `m_PostProcessData: {fileID: 0}`, one field currently disabling all post-processing.
- Four unused URP template assets (`Mobile_RPAsset`, `PC_RPAsset`, `Mobile_Renderer`, `PC_Renderer`) sit alongside the two live ones in `Assets/Settings/`.
- Confirm Level 1 runs clean and a mobile build is possible **before** committing to asset production.

**Then** proceed to production step 1 (board cell + frame), because pieces cannot be judged against a flat black board.

---

*Prepared by direct inspection of project files, serialized Unity YAML, `.meta` importer data, PNG header and pixel sampling, and git state. No file inside `Assets/`, `Packages/`, `ProjectSettings/` or `UserSettings/` was modified to produce this document.*
