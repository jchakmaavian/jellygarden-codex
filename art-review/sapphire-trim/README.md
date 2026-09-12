# Sapphire Trim — implemented level-start popup

Implements the selected **02 Sapphire Trim** from `../soft-royal-frames/index.html`.

- Blue enamel frame with gold rails, cut corners, gemstone clasps, and a floating level badge.
- Soft Royal cream interior, optional booster tiles with owned counts and selection checks, and a green Play pill.
- A gently moving score star, soft halo, two glints, and a cached blurred home backdrop.
- A complete layout fitted to the safe area, with room for multiple targets and longer level numbers.

Goals use `StoryLevelHud.ReadLevel`. Score targets use the same star artwork as the mockup; collect targets retain their actual icons and backing. Opening or backing out clears booster choices. Play hands the selection to the existing board flow, where inventory is spent. Repeated taps after close do nothing.

## Implementation

- `StoryLevelPopup.cs`: layout and existing goal/booster/launch wiring.
- `StorySapphireFrame.cs`: native UI mesh for the frame and gold clasps.
- `StoryLevelGoalVfx.cs`: unscaled score-star motion and glints.
- `StoryModalBackdrop.cs`: GPU backdrop capture, blur, orientation, and texture cleanup.
- `StoryScreenTests.cs`: focused popup checks and repeated-tap coverage.

## Validation

Unity preview uses a separate save profile in an isolated project copy. The production workspace contains no preview driver.

- Typecheck: zero errors.
- Focused Unity checks: target values, booster inventory, disabled slots, multiple selection, Play handoff, close/reopen, and repeated actions.
- Native preview: 360 × 720, 393 × 852, 320 × 568, 346 × 641, 768 × 1024, and a simulated notched safe area.
- Checked button bounds and raycasts, visible text, score and collection targets, the largest authored target count, and star animation while time scale is zero.
- Real level launch builds the board and spends exactly one of each selected booster.
- Kit animation/VFX and window-launch guard checks pass.

The PNG files in this folder are native Unity captures. `sapphire-trim.png` is
the main preview, and `star-animation.gif` shows the live score-star motion.
The final macOS preview build completed with zero errors and 381 existing
project warnings. The HTML exploration remains in `../soft-royal-frames/`.
