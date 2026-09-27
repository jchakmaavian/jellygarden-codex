# Gameplay HUD

`reference.png` is the approved design for the gameplay screen (2026-09-27, level 23). The UI follows it. The board,
pieces, goals, tools and the king are the game's own and are unchanged. The king artwork is kept as it is; only its
frame follows the reference.

Code, in `Assets/JellyGarden/Scripts/GUI/`:

- `RoyalGameplayHud`: the drawn parts. It builds the Goal and Moves panels, the portrait frame, the bottom bar, the
  round buttons and their badges. It also makes the backdrop gradient and the board frame's textures, and holds
  `RoyalHudPress` (press feedback).
- `RoyalArchShape`: the arched shapes. That is the headers' shallow arc and the portrait's half-ellipse window.
- `RoyalGameplayPresentation`: builds the HUD with those parts and places the live objects in them. That covers the goal
  icons and numbers, the Moves number, the king rig in its masked window, the four tools and settings. It also fits the
  board between the top row and the bar.
- `RoyalBoardSurface`: the gold frame around the board's silhouette.

## Layout

The reference is 941 x 1672 px. Scaled to 1080 x 1920 it gives these sizes, in HUD canvas units (720 x 1280 reference,
1.5 px each at 1080 x 1920).

- **Top row**, anchored to the top of the safe area:
  - Goal and Moves panels: 203 x 194 units, centred 228 units either side of the middle.
  - The portrait: 224 x 235, centred.
  - All three share one baseline, and the row ends 269 units down.
  - Each panel: a blue frame with an arched header (12-unit rise, 5-unit gold rim) over a cream body of 183 x 135.
  - The portrait: a half-ellipse window 77 units tall over a rounded base, in a 7.3-unit gold ring.
  - The whole row sits 19 units higher than in the reference. Level 23 has 11 rows plus the key exits, one more row
    than the reference shows, so this keeps its cells close to the reference's size.
- **Board**: framed by a gold band a tenth of a cell wide, outside the cells. Outer corners are rounded (radius one to
  two band widths); inner corners are mitred. The frame keeps the reference's 3.9% side margins. The board fits between
  the top row and the bar with about 11 units of gap each side. 9-column boards get the reference's 108 px cells at
  1080 x 1920. On levels with key exits the board also leaves room for the exit arrows, which hang about 0.86 of a cell
  below the bottom row, so they end above the bar; level 23 and level 14 then get about 103 px cells.
- **Bottom bar**: one pill-shaped bar, 692 x 143 units, its centre 98.5 units above the bottom of the safe area.
  - Five round buttons, 116 units across and 137.5 apart: the four tools on green and settings on blue.
  - Each button has a thick gold ring and a thin blue outer rim.
  - Badges are 45 units, at the lower right: red with the live count (32 units, white), or green with a + once a tool
    is used up.
- **Backdrop**: a royal-blue gradient, #2b79da at the top centre fading to #0c357d at the sides and bottom, with no
  scenery.
- **Lettering**: the game's display font, BorisBlackBloxx, which is close to the reference's.
  - "Goal" and "Moves" are cream with a navy outline.
  - The goal icons are 70 / 64 / 50 / 40 units for one to four goals; the numbers are dark blue, 30 units for one or two
    goals and 27 for more.
  - The Moves number is dark blue, 74 units, and the strongest text in its panel.

## Level start

After the goals card closes, the level stays in `WaitForPopup` for about a second while `LevelManager.AnimateField` slides
the board in from the right; the state turns to `Playing` when the slide ends.
- **Before:** the HUD, the backdrop and the cell styling waited for `Playing`, and the camera was framed only after the
  slide. So that second showed the raw board on the camera's black.
- **Now:** all three appear as soon as the board exists; only the goals card holds them back, because the overlay HUD
  would cover it. The camera is framed for the board's resting place from the slide's first frame.
- **Framing is held:** the legacy `AspectCamera` sets its own zoom once the pieces exist, so the framing is re-applied
  whenever anything changes it during a level.
- **No black anywhere:** the camera's own background is the backdrop's navy.

## Interaction

There is no squash or bounce. A pressed button's face and badge ease to 0.975 and 88% brightness: 0.08 s in, 0.12 s out,
ease-out, on the unscaled clock. The chosen tool keeps the tool bar's own 1.06 enlargement.

## Checks

Verified at 1080 x 1920, 720 x 1280, 1080 x 2160 and 1440 x 2960 in the QA profile: nothing clips, the top row shares
one baseline and the portrait is centred. `reference-vs-unity-1080x1920.png` shows the reference beside the game.

The final validation (2026-09-27) covered the level start on a regular rectangular board (level 36), the irregular
level 14 and level 23 with key exits. In every drawn frame from the goals card closing until play:
- the HUD, the backdrop and the styled tiles are visible;
- no frame is black;
- the camera does not move.

After the board lands:
- the frame matches each silhouette (level 14: 48 sides, 8 outer and 4 inner corners);
- the exit arrows sit fully between the board and the bar;
- the tool counts follow the stock, and the settings button opens the pause menu;
- nothing clips at 1080 x 1920.

`ReferenceLayoutChecks` passes 12/12, the console shows no errors and the real save is unchanged.

Two project checks follow the new design:
- `ReferenceLayoutChecks` checks the HUD is drawn to this reference. The settings gear is the only packaged layout art
  left.
- `RoyalBoardSurfaceChecks` expects a seventh layer for the frame's corners.
