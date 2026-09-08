# Screenshots

Only screenshots that genuinely explain something are kept here.

## 01_gameplay_composition.png

The gameplay screen as composed today (Level 5, 720 x 1280). It shows the three
independently-scaled regions described in section 7.3 of the main report:

- **Top HUD bar** - UGUI Canvas `Level/Canvas`, reference resolution 720 x 1280,
  width-matched. Carries the only `SafeArea.cs` in the project.
- **The board** - NOT UI. World-space SpriteRenderers inside `Level/GameField`,
  sized by the camera's orthographic size, which is chosen from three hard-coded
  aspect-ratio buckets.
- **Bottom booster bar** - the same Canvas as the HUD.
- **Background** - a separate Canvas (`CanvasBack`) that swaps sprite every 20 levels.

The HUD scales with screen **width**; the board scales with screen **height**. On any
device that is not exactly 2.06 or 2.17 tall-to-wide, the gaps between these regions
differ from this image.

Note the board's irregular outline: holes are cell code `00` in the level text file,
where nothing is drawn and pieces fall straight past.
