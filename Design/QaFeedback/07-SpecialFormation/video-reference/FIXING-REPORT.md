# Special formation: supplied video revision

Implemented from `G:/Game/matching Tranform.mp4` (1080 x 1920, 60 FPS, 168 frames). The relevant sequence is the sideways swap that completes a vertical four-gem match and creates a horizontal rocket.

## Visible behavior

- Full-size, opaque gems accelerate into a compact overlapping stack over 10 reference frames (approximately 0.167 seconds). The last visible stack is covered by the creation flash rather than shrinking into an isolated gem.
- The original native special-piece Reveal starts immediately after the merge. Its clips, controllers, art and sound remain intact.
- A white-gold flash, expanding ring and four-point sparkles follow the created piece through gravity. This overlay is reconstructed from the supplied video using existing imported sprites and the existing additive shader; it is not a recovered particle clip.
- Removed the additional half-second resolver hold for gathered special pieces. The existing destruction, swap-ownership and pause gates still determine when gravity is safe.
- Player-created rocket orientation follows the creating swap, matching the sideways swap/horizontal rocket shown in the video. Cascades without an active creating swap retain the existing match-line orientation.
- The common merge and flash presentation applies to TNT, both rockets, Color Ball and Propeller. The video directly demonstrates a rocket; the other types retain their own original Reveals.

## Timing evidence

| Measurement | Before | Final implementation | Supplied video |
| --- | --- | --- | --- |
| Merge to Reveal, 60 FPS | 15 frames | 10 frames | About 10 frames, 128–138 |
| Reveal to first downward movement, 60 FPS | 18 frames | 3 frames | About 3 frames |
| Merge to Reveal, 30 FPS | 8 frames | 5 frames | 60 FPS source only |
| Reveal to first downward movement, 30 FPS | 9 frames | 2 frames | 60 FPS source only |

Frame timings are controlled Unity simulation/capture timings, not phone performance measurements. The side-by-side view aligns merge onset and cell scale. It demonstrates the sequence and timing, not pixel-identical particle rendering or identical refill contents.

## Verification

Final combined run: **249 passed, 0 failed**.

- 56 real-swap reference checks: both directions at 30/60 FPS, full size/opacity, immediate Reveal handoff, native Reveal progressing during gravity, creation overlay following/cleanup, orientation, score, move count, ownership and resolver completion.
- 144 special-piece checks: TNT L/T, Rocket H/V, Color Ball and Propeller at 30/60 FPS, including pause, native Reveal start/progress, source retirement and refill ownership.
- 48 normal match-three checks: the QA #8 next-frame break behavior remains intact.
- One normal-profile restoration check: balance and lives restored. Final session returned to Map; no final Unity console errors.

The combined fixture explicitly selects moves-based Level 1. An earlier run inherited a timed level and failed move assertions while displaying timer bonuses. That fixture issue was corrected. The special creation assertion now counts the initial handoff, so a legitimate later refill cascade is not mistaken for duplicate creation.

## Review evidence

- [Reference versus implementation](reference-vs-implementation.gif)
- [Frame comparison](reference-vs-implementation.jpg)
- `before/checks.txt` and `before/*.csv`: measured baseline.
- `after/*.csv` and `after/*.png`: final actual-swap traces and frames.
- `final-checks/checks.txt`: authoritative final combined results.

The local Excel tracker has updated notes for feedback 6–7 and an additional fixing report section. Physical-phone QA and a rebuilt APK remain pending.
