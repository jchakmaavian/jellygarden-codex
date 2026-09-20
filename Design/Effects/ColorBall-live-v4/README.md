# Color Ball: compact spin and sharp conversion

Compared the creation and activation portions of `Royal-Match-booster-animation.mp4` using consecutive 60 fps frames. The reference resolves quickly into a readable sphere; activation uses a compact coloured disc with white horizontal light and an intense centre. The previous live presentation was too opaque, wide and slow, with round target bubbles.

## Installed changes

- Reveal retimes the existing 60 frames into 0.38 seconds, interpolates adjacent frames, starts with a small flattened pose and white stars, then resolves into the original 1x sphere. Its slight downward settle ends at the cell centre.
- Idle retains the approved artwork and subtle authored highlights at 1x, with no added spin, breathing, orbit or glow pulse.
- Activation takes 0.14 seconds. The charged loop is 0.72 seconds per cycle, with continuous interpolation at the authored seam. Runtime presentation narrows/flattens the coloured body within the original canvas, adds horizontal blur, soft white halo, white centre flare and thin white ribbons. The locked source frames are unchanged.
- Thin, straight white/gold beams connect the centre to each existing selected target. Target feedback now traces the actual gem silhouette and uses a compact flash/four-point star instead of an expanding bubble/ring.
- Finish takes 0.24 seconds after reaching the existing loop seam. Beam contact/conversion cadence, target rules, deferred Rocket queue and Rocket handoff are unchanged.

The only sequence integration change supplies the target sprite to the outline effect. No Rocket, TNT, UI, level or gameplay-rule code is changed. The VFX still uses the existing fixed pool of 16 beams and 192 light sprites; reused outline sprites restore their material on the next emission.

## Focused verification

`checks.txt` records the final Unity Play Mode run: Color Ball at 0.5x, standalone activation/reuse at 1x, and Color Ball + Rocket at 1x. Checks cover reveal timing, original-size idle, charged loop, one impact per target, no early Rocket launch or refill, pooled cleanup, input restored, console errors and normal player save restoration. This is not full-game QA or a device performance benchmark.

`effect-half.mp4` and `rocket-combo-full.mp4` are live captures using measured frame timestamps. Synchronous screenshot recording can introduce capture stutters. Phase PNGs and reference contact sheets support the visual comparison; the artwork intentionally remains Royal Throne rather than a pixel-identical copy.

`package_review.py` verifies unchanged approved frames and protected Rocket/TNT source hashes, then packages the installed source and metadata. `ColorBall-Polish.zip` and `incremental.patch` apply on top of the existing v3 integration. The actual Unity source files are installed in this project, not only in the review archive.
