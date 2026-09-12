# King Leo — 10 home screen HTML concepts

Open **index.html** in a browser. The entire gallery works offline, including
fonts and artwork. No package installation, server, or build step is needed.

## The collection

| # | Direction | Main idea |
|---|---|---|
| 01 | Royal Welcome | Blue and gold, a compact repair card, a prominent green Play button |
| 02 | The Storybook | An ivory storybook and a chapter-led welcome |
| 03 | Castle First | An open scene, floating controls, a repair marker on the gate |
| 04 | Little Kingdom | Bright clouds, rounded buttons, and a cheerful repair sticker |
| 05 | A King’s Welcome | Leo’s portrait and a personal welcome |
| 06 | The Royal Workshop | The next repair is the primary action |
| 07 | Crown & Velvet | Crimson banners, a crest, and an ornate action panel |
| 08 | The Garden Postcard | A framed castle illustration with cream and sage controls |
| 09 | The Adventure Trail | Gate, flag, and lamps become three visible milestones |
| 10 | Golden Hour | A small title, a slim repair row, and warm amber controls |

## Using the gallery

- **Open preview** enlarges a concept and explains the layout and tradeoff.
- Use **Previous / Next**, or the keyboard arrow keys, to compare concepts.
- **Escape** closes the enlarged preview.
- Heart buttons save a **shortlist** locally in the browser; no data is sent.
- Royal / Playful / Simple filters narrow the collection.
- **Motion** toggles the small decorative animations. Reduced motion is
  respected on first load.
- **Reset demos** resets repair state without clearing the shortlist.
- Direct links use `index.html#concept-01` through `#concept-10`.

Each phone is an independent interactive HTML mockup. The repair button opens
the three actual Area 1 tasks. Spending the available star updates that
concept's castle image, star counter, and repair progress. Play opens a level
preview; navigation and settings open local demonstration panels. There is no
puzzle engine or connection to the Unity save data.

## Files and artwork

- `index.html`: the gallery and enlarged-preview shell.
- `app.js`: 10 distinct compositions and local interactions.
- `styles.css`: shared controls, the ten visual treatments, and responsive layout.
- `assets/`: copies of the existing castle states, icons, portrait, and fonts.
- `overview-desktop.png`: all 10 concepts in one contact sheet.
- `preview-01.png`: enlarged Royal Welcome concept.
- `overview-mobile.png`, `preview-mobile-08.png`: mobile browser captures.

No new artwork was generated. The only project changes for this request are
inside this folder. Unity implementation follows the selected direction.

## Verification

Inspected in headless Chrome at 1800×1100 and 390×844. All 10 concepts render;
all images and fonts load; neither viewport has horizontal overflow. Tested
repair/star/progress changes, Play and Cards panels in every variant, preview
navigation, Escape dismissal, shortlist filtering and persistence, and reset.
No JavaScript exceptions were reported. The enlarged views and overview were
also inspected visually.
