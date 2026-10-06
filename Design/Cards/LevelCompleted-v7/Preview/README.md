# Editable dimensional title preview - v7

Open `preview.html` locally. This self-contained browser reconstruction uses project artwork and no network requests. The native Unity captures one directory above show the implemented game; CSS rasterization and lighting differ from Unity.

The title now uses eight gold depth planes, gold/ivory bevels, a dark keyline and a cast shadow. Each glyph tilts in perspective, grows to 1.62 scale, compresses to 0.92 and settles over 0.48 seconds. The stagger is 0.045 seconds, with the word-space beat preserved. Type is 62 units at row 190. Reduced motion keeps the dimensional title still.

`config.js` controls sizes and timing; `preview.js` controls motion and depth planes; `preview.css` styles bevels and the card. The existing emerald button and ten-second card-to-Home review remain. Continue/Close switch to Home at 4.5 seconds in the automated timeline; its resource values and scenery are illustrative and do not read a game save. V6 remains preserved.

Automation: await `window.previewReady`; use `preview.seek(seconds)`, `preview.play(seconds)`, `preview.continue()`, `preview.setReduced(true)` and `preview.state()`. `letterScales` reports the pose of every character. `preview.html?capture&t=0.30` hides review controls. The persistent OFFLINE MOTION PREVIEW label distinguishes this artifact from native capture.
