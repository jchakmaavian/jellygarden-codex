# Five level-start popup designs

Open [index.html](index.html) in a browser to compare five interactive concepts
for the Level 3 / goal / booster popup. Everything works offline, using the
game's own castle, fonts, portrait, goal star, and three booster icons.

| # | Design | Direction |
|---|---|---|
| 01 | Soft Royal | Cream centered card, floating level badge, green Play capsule. |
| 02 | Castle Drawer | Bottom sheet with more visible castle and an inline goal. |
| 03 | The Royal Ticket | Blue-and-cream ticket with a perforated booster section. |
| 04 | Emerald & Gold | Dark emerald panel, crown medallion, amber Play button. |
| 05 | A Little Leo Luck | Leo portrait, encouragement, and colorful booster tiles. |

Each design shows the same **Level 3**, **500-point goal**, and starting
inventory: **1 rocket, 2 bombs, 3 color balls**. This is sample state for visual
comparison; the mockups do not read or modify the game save.

Click a booster to select or deselect it. Play displays a local preview
response. Closing and reopening a popup clears its booster selection, matching
the current game flow. The gallery and enlarged view share each concept's
selection. Hearts save favorites in this browser; the Shortlist filter shows
them together. Motion can be disabled, and reduced-motion preferences are
respected. Escape closes the enlarged preview; arrow keys switch designs.

## Captures

- [All five designs](five-designs.png)
- [Full desktop gallery](overview-desktop.png)
- Individual phones: [01](design-01.png), [02](design-02.png),
  [03](design-03.png), [04](design-04.png), [05](design-05.png)
- [Mobile enlarged preview](preview-mobile-02.png)

Checked in Chrome at desktop, 390px phone, and 320px compact-phone widths:
images and fonts load, controls stay inside the game canvas, each concept's
interactions work, favorites survive reload, and the inspector supports mobile
scrolling and keyboard dismissal.

These are HTML mockups for choosing a direction. No Unity scripts, gameplay,
prefabs, or original assets were changed.
