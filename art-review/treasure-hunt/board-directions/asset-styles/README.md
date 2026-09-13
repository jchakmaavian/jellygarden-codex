# Soft Storybook — asset style comparison

Open [index.html](index.html) in a browser. This is a design preview; no Unity assets or gameplay scripts are changed by this exploration.

**Current selection: the third set, Bold Cartoon.** Its purple butterfly has been replaced by a purple royal crown. The board, collection-target artwork, icon library and enlarged views all use the replacement. The other two styles keep their original artwork. The library opens on Bold Cartoon by default.

The three directions hold the Soft Storybook Compact Bar layout, board positions and goal data constant:

- **Glossy Jewels:** faceted match pieces, polished enamel tools and gold details.
- **Painted Storybook:** matte gouache brushwork, warm pigment and illustrated shapes.
- **Bold Cartoon:** strong indigo outlines, simple shading and clear silhouettes.

Each set contains 18 separate transparent PNG sprites: six match pieces, eight power-ups, two treasures, an ice overlay and a score medal. Use the target selector to see score, collect, treasure, ice and mixed objectives. Both phone sizes, the tall-board option, progress states and timed levels are available. Tap a mockup or an asset to enlarge it. The artwork library has its own style selector.

## Artwork

Generated with **built-in image_gen**, then copied into this folder. [Exact generation prompts](art/prompts.json) and [source paths and SHA-256 hashes](art/provenance.json) are saved alongside the artwork.

- Masters: `art/glossy-sheet.png`, `art/painted-sheet.png`, `art/bold-sheet.png` — 1122 × 1402 RGBA.
- Individual sprites: `art/glossy/`, `art/painted/`, `art/bold/` — 256 × 256 RGBA, 18 per directory.
- [Crop manifest](art/sprite-layout.json) records every source rectangle.
- `pack-icons.cjs` crops along the generated sheet gutters and resizes the original pixels through browser canvas. It retains transparency and does not redraw, remove backgrounds or recolour the art.

The selected crown is `art/bold/crown-v2.png`, generated with built-in imagegen. It overrides `piece-4` only in the Bold Cartoon preview; the original sheet and butterfly export remain available. `pack-crown.cjs` exports the crown from `art/bold-crown-master.png`, preserving its alpha. See [crown prompts and provenance](art/bold-crown-provenance.json), [crop details](art/bold-crown-crop.json), and the [updated preview](verified/bold-crown-preview.png).

The ice sprite has an open transparent center, so the underlying piece remains visible. The blue score medal is visually distinct from the yellow star match piece. Counts and the +5 / +30s annotations are live HTML, not text baked into artwork.

## Game meanings preserved

The target examples and board outlines come from the preceding [game audit](../soft-storybook-targets/game-audit.json). Refer to the [target preview notes](../soft-storybook-targets/README.md) for code references and target counts.

- Collect targets reuse the exact board sprite for each colour. Sprite order is heart, droplet, leaf, star, purple piece, gift. The purple piece is a crown in Bold Cartoon and a butterfly in the other two sets. Unity colour slots map to sprite indices `[0, 5, 4, 1, 2, 3]`; the crown retains the purple slot.
- Treasure goals remain blue lockets and red-gem rings, collected at column exits.
- The in-game tray remains extra moves (or extra time), swap glove, single-piece hammer and nearby recolour brush.
- Rocket, area bomb and rainbow ball remain board / pre-level specials. The hammer and area bomb are separate actions.
- Current Area 1 uses score and ice targets. Later-level examples demonstrate the other families.

Board content is illustrative, using actual level outlines and target counts. This page does not simulate matching or change saved progress.

## Verification

The crown revision was checked in Chrome: the selected board has crowns and no butterfly sprites, purple collection targets use the crown, the enlarged asset has the correct name, all images load, and the compact tall-board layout still fits. See [crown-check results](verified/bold-crown-check.json). The updated crown export is 256 × 256 RGBA.

Chrome checks passed for 20 configurations / 60 board views across five target examples, two phone sizes, normal and tall outlines, moves and time, and progress states. Boards fit their reserved region, tiles remain square, goal counts do not overflow, and all rendered gameplay icons belong to the selected style.

All three libraries loaded. Phone and icon enlargement worked. At a 393 × 852 browser viewport, the page had no horizontal overflow. No broken images or JavaScript exceptions were detected.

Evidence: [browser-check.json](verified/browser-check.json), [three styles](verified/three-styles.png), [painted artwork](verified/painted-artwork.png), [ice / tall / timed](verified/ice-tall-timed.png), [enlarged mobile](verified/mobile-enlarged.png).
