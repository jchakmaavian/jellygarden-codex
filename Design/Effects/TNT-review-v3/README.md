# TNT reference timing update

Reference: J:/Unity Projects/Match3-Games_ArtSource/Match-3-JC/00_References/animation-VFX/TNT.mp4 (13.58 seconds, 60 fps, supplied at 0.25x). Inspected overview, consecutive creation/impact frames and the full relevant phase sheets. Recorded intervals are divided by four for normal-speed animation. Artwork stays Royal Throne; the mesh model remains disabled.

Implemented in the existing TNT visual pool:

- Sprite reveal: immediate appearance, growth/overshoot, hot animated head-rim, contraction, rebound and settle in 0.40 seconds. The replacement item skips legacy randomize/fall initialization during this reveal.
- Activation: 0.12-second compression and ignition flash using the original sprite.
- Blast: white-hot expanding ball through 0.15 seconds; warm gold rim and irregular rays emerge as the center fades through 0.25 seconds; shockwave/rays fade through 0.34 seconds.
- Per-cell impacts and fragment release occur while the central ball expands. TNT fragments use two medium and eight small painted chips per cell within the existing fixed pool; gravity and lifetime are preserved. Chips are sized to remain readable around the blast.
- Pooled stars and soft residual light finish by 0.74 seconds. Gold layers use slight background coverage to prevent additive clipping to white on the lavender board.

Verification is limited to TNT at 0.25x and 1x: same-frame replacement creation, reveal/rim, idle, activation, blast phases, falling debris, cleanup, pooled reuse, preserved coins/lives, and runtime errors. See checks.txt and the phase captures. No full-game regression or device performance benchmark. The captures document visual correspondence, not pixel-identical artwork or a pixel-difference certification.

Runtime changes are isolated in incremental.patch against the pre-task working files. verified-source retains exact scripts/shader and original metadata; apply to the existing project architecture, not as standalone replacement gameplay logic. No new packages, prefab GUID changes, or Rocket edits.
