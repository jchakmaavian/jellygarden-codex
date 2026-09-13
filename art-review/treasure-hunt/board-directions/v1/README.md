# King Leo — five board directions

Open [index.html](index.html) directly in a browser. No server or internet connection is required. This is a visual design review; no Unity game files were changed for this exploration.

## The five options

| Direction | Board treatment | New piece family |
| --- | --- | --- |
| Royal Gems | Blue enamel, cream cards, royal blue controls | Heart, shield, clover, crown, amethyst, sun |
| Soft Storybook | Cream paper, pale blue tiles, lilac booster dock | Heart, droplet, leaf, star, butterfly, gift |
| Enchanted Jewels | Midnight jewel box, circular moves medallion | Ruby triangle, sapphire diamond, emerald rectangle, star, moon, amber |
| Royal Keepsakes | Wooden tray, parchment header, crimson dock | Bow, letter, clover, bell, potion, acorn |
| Garden Magic | Pale jade tiles, leafy cards, garden palette | Tulip, dewdrop, leaf, flower, butterfly, mandarin |

Royal Gems is the recommended starting point for its bright silhouettes and connection to the approved Royal Storybook menus. This is a design judgment, not a playtest result. Any board and piece family can be paired in a later implementation.

## Preview controls

- Explore each design at a larger size; Previous and Next cycle through all five.
- Try a match swaps two pieces, clears three green pieces, refills the row and updates the sample score and moves. This is an animation demonstration, not a working match-3 engine.
- Switch between 393 × 852 and 320 × 568 phone proportions.
- Check silhouettes removes colour from the proposed pieces.
- Inspect the six-piece collections enlarged or at approximately 43 px, and compare with current pieces.

All variants show the same sample arrangement in an eight-column, seven-row board with missing corners, matching the level 3 board shape. Level 3, 24 moves, and a 500-point goal are sample display values. Pause, settings and booster controls inside the phone are visual examples.

## Assets and implementation fidelity

The boards are composed from HTML/CSS, separate transparent game pieces, and existing project art. They are not flattened screen mockups. Text, counters, progress, tiles, pieces and controls remain separate elements. The five new collections contain 30 generated sprites; existing castle, Pip, star and booster art provide story context. No outer gold screen border is added.

- `art/<direction>-sheet.png`: original selected generated sheet, copied from its generation location.
- `art/<direction>/0.png` through `5.png`: separate 256 × 256 transparent pieces, cropped and fitted from the selected sheet without recolouring.
- [sprite-layout.json](sprite-layout.json): source grid cells and crop rectangles.
- [provenance.json](provenance.json): exact selected generation/edit prompts, tool, source locations and packaging notes. `prompts.json` retains the initial five requests; the selected prompts in provenance take precedence.
- [pack-pieces.cjs](pack-pieces.cjs): the browser canvas packaging step; requires the local review server on port 8766 and a dedicated Chrome debugging instance on port 9236. This is only for reproducing sprites, not for viewing the preview.
- `styles.css`: board surfaces, layouts and transition styling.

Original generated files were retained. Art was generated with the built-in imagegen tool; no external art search was used. The selected source PNGs contain transparency even though some viewers show their underlying RGB background.

If a collection is approved, retain the game's six existing colour identities when installing it. The preview file order differs from the current Unity item order:

| Preview sprite | Colour | Current Unity sprite slot |
| --- | --- | --- |
| 0.png | Red | item_01.png |
| 1.png | Blue | item_04.png |
| 2.png | Green | item_05.png |
| 3.png | Yellow | item_06.png |
| 4.png | Purple | item_03.png |
| 5.png | Orange | item_02.png |

Unity integration has not been done. The CSS surfaces and layout would still need to be recreated or exported as separate UI elements after a direction is chosen; the new piece sprites can be reused directly.

## Verification

Chrome checks covered asset/font loading, gallery and detail views, the match demo, collection and size controls, grayscale mode, and compact phone layout. All five compact boards keep their HUD, board and boosters separated. The page fits a 393 px mobile viewport without horizontal overflow. Screenshots and the browser check report are in [verified](verified/).
