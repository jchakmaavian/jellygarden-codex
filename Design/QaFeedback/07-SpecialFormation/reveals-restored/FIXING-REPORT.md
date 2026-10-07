# Original Reveals restored; full-size gem merge retained

Revision requested and verified on 7 October 2026.

The static transformation had replaced the authored special-piece Reveals. The user requested restoring all of them while keeping the gems moving together.

The full-size 0.24-second gem movement remains. Gems stay opaque as they meet in the existing creation cell. The result then uses the original `ReferencePieceVisual.Attach(..., true)` path, starting its native Reveal and creation sound normally. The settled preview, 0.14-second crossfade and Reveal-bypass flag have been removed.

Restored clips:

- TNT: `TntCreationAnimation`
- Horizontal Rocket: `HorizontalRocketCreationAnimation`
- Vertical Rocket: `VerticalRocketCreationAnimation`
- Color Ball: `LightballCreationAnimation`
- Propeller: `PropellerCreationAnimation`

No artwork, animation clip or controller was edited. Match detection, host choice, collection and refill remain on their existing paths.

## Verification

**145 checks passed, 0 failed** in Unity 6000.5.10f1. TNT L/T, Rocket H/V, Color Ball and Propeller ran at 30 and 60 FPS. Updated assertions verify each original Reveal starts near its beginning and advances normally, instead of asserting the previously requested bypass.

The checks also cover full-size opaque gem movement, convergence, pause, stationary source roots, retirement of matched items, exactly one resulting special, cleanup and refill ownership. The isolated QA driver restored normal balance/lives; the subsequent editor check confirmed no QA runners or dirty scene. Physical-phone testing and an exact Royal Match comparison remain pending.

## Current evidence

- [TNT preview](TNT-L/merge.gif)
- [Horizontal Rocket preview](Rocket-H/merge.gif) and [vertical Rocket preview](Rocket-V/merge.gif)
- [Color Ball preview](ColorBall/merge.gif)
- [Propeller preview](Propeller/merge.gif)
- [Merge and Reveal frames](merge-sequences.png)
- [Checks](checks.txt)

GIFs are short actual Unity capture excerpts of the merge and restored Reveal motion, not full-length reference comparisons. The Excel tracker and Fixing Report now reflect this revised behavior; earlier no-Reveal descriptions are superseded.
