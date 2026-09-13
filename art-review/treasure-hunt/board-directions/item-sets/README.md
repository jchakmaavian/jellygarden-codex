# Adventure of King Leo — Storybook Magic

Open [index.html](index.html) in Chrome. The page works directly from the repository without a running server. It is a browser design preview; no Unity assets, levels or gameplay scripts were changed.

The owner selected **Storybook Magic**, in the **Bold Cartoon** art style, and chose **Adventure of King Leo** as the working game title. The selected set appears first and is the default board collection. The other four collections remain available for comparison.

The chosen pieces are red potion, orange mushroom, purple wizard hat, blue crystal, green clover and yellow star. Their exports are in `art/storybook-magic/`. The board layout remains Soft Storybook. This records the design choice; it does not rename the Unity project, change application identifiers or install the sprites in the game.

## Collections

Every collection contains six separate transparent sprites, ordered red, orange, purple, blue, green, yellow:

| Collection | Red | Orange | Purple | Blue | Green | Yellow |
| --- | --- | --- | --- | --- | --- | --- |
| Royal Keepsakes | Wax seal | Copper coin | Gem | Shield | Feather | Bell |
| Storybook Magic | Potion | Mushroom | Wizard hat | Crystal | Clover | Star |
| Castle Garden | Strawberry | Carrot | Mushroom | Water drop | Leaf | Flower |
| Royal Sweets | Heart candy | Gummy bear | Macaron | Wrapped candy | Lollipop | Cookie |
| Playful Classics | Heart | Gift | Diamond | Water drop | Leaf | Star |

The gallery has 30 labelled item cards. Tap any item to enlarge it, or use **Try on board** to show that collection in the Soft Storybook layout. On smaller browser windows this opens an enlarged board. **32 px check** displays all gallery sprites at 32 pixels for a quick readability comparison.

The board preview includes the existing score, collect, treasure, ice and mixed-goal examples, two phone sizes and a tall-board option. Target slots reuse each collection’s matching sprite and colour index. Power-ups and the locket/ring treasure goals use the approved Bold Cartoon assets. Board content is illustrative, using actual level outlines and target counts; the preview does not simulate gameplay.

## Artwork and provenance

Four six-piece sheets were generated with **built-in image_gen** in the selected Bold Cartoon direction. Each master is 1536 × 1024 RGBA. Exact prompts are in [art/prompts.json](art/prompts.json), and source paths, dimensions and SHA-256 hashes are in [art/provenance.json](art/provenance.json).

- Masters: `art/royal-keepsakes-sheet.png`, `art/storybook-magic-sheet.png`, `art/castle-garden-sheet.png`, `art/royal-sweets-sheet.png`.
- Individual exports: `art/<collection-id>/<colour>.png`, 256 × 256 RGBA.
- Playful Classics reuses the five approved pieces from `../asset-styles/art/bold/`, plus the new Royal Keepsakes purple gem. Its red, orange, blue, green and yellow pieces were not regenerated.
- [art/sprite-layout.json](art/sprite-layout.json) records all crop rectangles and reused sources.
- `pack-icons.cjs` reproduces the crops and exports through browser canvas, retaining the generated alpha. It only crops, scales and copies; it does not redraw the artwork or remove backgrounds.

The new sets contain neither crowns nor butterflies. The previous crown exploration is preserved in its own folder and is not referenced by this preview.

## Verification

Chrome checks confirmed five collections and 30 item cards, all images loaded, each collection used the correct board sprites, and boards fitted the reserved space for normal and compact tall-board layouts. The remaining target families loaded correctly. The 32 px toggle, enlarged asset, mobile gallery and mobile board dialog worked, with no horizontal page overflow or JavaScript exceptions.

Evidence: [browser-check.json](verified/browser-check.json), [gallery top](verified/gallery-top.png), [gallery bottom](verified/gallery-bottom.png), [32 px comparison](verified/small-icons-and-classics.png), [purple gem](verified/purple-gem.png), [mobile gallery](verified/mobile-gallery.png), [mobile board](verified/mobile-board.png).
