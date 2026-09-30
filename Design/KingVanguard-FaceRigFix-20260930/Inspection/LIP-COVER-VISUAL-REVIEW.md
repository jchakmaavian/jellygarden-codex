# Lip-cover correction: visual review

Reviewed the regenerated `After` idle, happy, sad and worry close-ups, plus all six deterministic samples in the idle and sad sheets. This is a visual review of stills, not an independent execution of the animation/mix tests.

The corrected warm skin/lip surround removes the false large burgundy cavity. The teeth now read within the mouth opening, and the brows remain attached after suppression of their obsolete external shadows. These views do not justify moving the teeth or moustache again, or regenerating the full face.

One concrete artifact remains: the separate `Bıyık Gölge` attachment forms an olive/brown ribbon outside the visible moustache. In sad and worry poses, small curved strokes float beyond both curl ends; in idle and happy it creates a broad greenish underside, especially on the right. The new moustache already has its own shaded hair volume. Its repainted alpha contour also occupies only about 84% of the original source alpha coverage, making the separate old contour unsuitable.

The minimal next step is to suppress **only `Bıyık Gölge`**, at attachment alpha level so source slot-color keys cannot restore it. Preserve the existing painted moustache, teeth, mouth cover, source motion and geometry. Recheck the same expressions afterward. No other clear geometric displacement is established by the reviewed stills.

Stable copies of the reviewed state, before that proposed suppression:

- [Sad](lip-cover-before-shadow-suppression-sad.png)
- [Idle](lip-cover-before-shadow-suppression-idle.png)

No runtime assets or code were changed by this review.
