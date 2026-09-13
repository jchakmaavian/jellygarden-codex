# Soft Storybook — adaptive board layouts

Open [index.html](index.html) directly in a browser. This is a new preview alongside the original five directions; Unity files and the running game are unchanged.

The user selected Soft Storybook and identified that the character strip could collide with larger level boards. These three alternatives remove the character strip and reserve independent regions for the top controls, board and boosters.

| Variant | Layout | Tradeoff |
| --- | --- | --- |
| Classic Cards | Original target card, round moves counter and lilac booster tray | Closest to the approved look; tall boards shrink more on small phones |
| Compact Bar | Combined score/moves panel and a shorter booster dock | Recommended balance of the storybook look and room for tall boards |
| Floating Controls | Small status pills, separate booster buttons and tiles without an outer tray | Most open presentation; less framing around the board |

## Controls

- Short: 9 × 5 outline from level 1.
- Medium: 8 × 7 outline from level 3, including missing corners.
- Tall: 9 × 11 outline from level 4, including missing cells.
- Standard phone: 393 × 852. Small phone: 320 × 568.
- Show reserved board space: overlays the exact middle region used for sizing.
- Enlarge design: opens the selected layout in a larger view.

The board geometry comes from `Assets/JellyGarden/Resources/Levels/{1,3,4}.txt`. A scan of all current level files found a maximum of 9 columns and 11 rows. Token arrangement and obstacles are illustrative. Counters use sample targets and the campaign's minimum moves for those levels; this is not a gameplay simulation.

## Sizing

The screen uses three grid rows: `auto minmax(0, 1fr) auto`. The board region contains no character or story banner. A ResizeObserver measures the remaining width and height. Square cell size is the minimum of:

1. The proposed maximum cell size, scaled to phone width.
2. Available board width divided across its columns, after padding and gaps.
3. Available board height divided across its rows, after padding and gaps.

The complete board is centred inside the region. Neither the header nor the booster dock moves into that region. Shadows and outer margins have extra space. The displayed tile size is converted to the selected phone's logical pixels, independent of browser preview scaling.

For the 9 × 11 board on the small phone, measured tile sizes were approximately 25 px (Classic), 28.5 px (Compact) and 29.2 px (Floating). Small boards use a size cap instead of expanding indefinitely. These are browser measurements, not Unity runtime guarantees.

## Art and reuse

All three options use the same six approved-for-review Soft Storybook pieces from [the first exploration](../v1/index.html). No new art was generated for this layout pass. The existing assets are linked directly:

- `../v1/art/soft-storybook/{0..5}.png`: six transparent generated pieces.
- Project castle painting, victory star, rocket, bomb, orb and glove.
- Existing Storybook fonts and shared styles from `../v1/styles.css`.

Piece prompts and generation provenance remain in [../v1/provenance.json](../v1/provenance.json). Layout changes are in [layouts.css](layouts.css) and [layouts.js](layouts.js). Runtime integration would still need to apply the same fitting rules to the actual Unity board.

## Browser verification

[verified/layout-check.json](verified/layout-check.json) covers all three layouts × three board shapes × two phone sizes. Header separation, footer separation, board containment, square cells and footer containment passed all 18 combinations. No character strip is rendered. Screenshots of the tall board at both phone proportions are in `verified/`.
