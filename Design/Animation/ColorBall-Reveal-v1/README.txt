ColorBall-Reveal-v1 | 60 frames

Royal Throne Color Ball animation update - 20 September 2026

ACTIVE AUTHORING: source/ColorBallMotion.cs, source/render_motion.ps1 and source/package_motion.py.
Older renderer files are retained as helpers/history; their old timing comments do not describe this revision.
The source/COLORBALL-REFERENCE.md is an inherited reference measurement, not a claim of new independent verification.

All three groups: transparent 512x512 lossless WebP frames, authored at 60 FPS.
One fixed pivot (0.5, 0.5), no automatic trimming, identical pixel-to-cell scale.
112 source pixels = one board cell. Full sprite quad = 512/112 = 4.5714286 cells.
Set pixels-per-unit = 112 / actual cell world width. Do NOT fit the whole 512px texture to one cell.
The idle artwork itself occupies approximately 112px. The spinning disc grows to approximately 336px (3 cells).
The transparent margin protects the halo/stars from clipping. Render on the existing unmasked gameplay VFX layer below HUD.

Artwork preservation:
- Idle and all state handoffs use the original color_ball.webp pixels, uniformly resampled.
- No AI regeneration, new dots, recolored decals or invented back-face pattern.
- Spin bands are shutter-sampled horizontal smears of the original dot cutout over the original navy lighting.
- Intentional squash/stretch and the short reveal descent are authored animation. The canvas pivot never moves.
- Idle is deliberately static, with no scale change, rotation, dot orbit, breathing or bounce.

TIMELINE (frame numbers below are ONE-BASED):
Reveal 001-060: early sparkles, tiny flattened birth, quick prominence at 3x, decelerating descent, return to original sphere.
Reveal 054-060: exact idle pose. Last Reveal frame == every Idle frame == Boosting frame 001.
Idle 001-120: 2-second static seamless loop.
Boosting 001-018: immediate reaction and smooth 1x -> 3x spin-up (0.30 seconds).
Boosting 019-078: one-second seamless charged loop, six horizontal turns/second, saturated bands, small white core and soft halo.
Repeat 019-078 for as long as target processing requires.
Boosting 079-108: controlled finish, soft expanding light ring, stars fade; final frame fully transparent.
Request finish after pending beam arrivals, leave at the next charged-loop boundary, then play 079-108 once.
Release visual and signal rocket handoff only once when frame 108 completes.

SEPARATE GAMEPLAY-CONTROLLED VFX (retained, not baked into the ball):
ColorBall-Boosting-v1/VFX/ColorBall_Beam/SequenceBody: orient from Color Ball center to chosen target.
Beam body scales to distance; Contact clip stays unscaled and is placed at the target.
ColorBall_TargetGlow/On then /Loop: white/gold target flash and held charged outline.
ColorBall_TargetImpact: local conversion/activation feedback. Existing fragment system handles debris.
Target selection and conversion are gameplay events: mark target once on beam arrival, replace with the existing Rocket,
continue processing the remaining targets, finish the Color Ball, then call existing Rocket activation.
Do not put target coordinates into the ball frames. Do not redraw or modify Rocket FireExhaust.
The older optional CoreFlare and FinishRing_Large clips remain archived VFX assets; do not overlay them on the new baked ball clips.
The new core and finish are already present in Boosting.

OUTPUTS:
frames/: lossless individual WebPs with exact RGBA round-trip verified against rendered masters.
60FPS_WEBP.zip: complete current numbered source sequence.
Preview.webp: transparent animated preview. WebP timing uses repeating 17/16/17 ms intervals to average 60 FPS.
Static idle may be losslessly coalesced by the animated WebP encoder; all 120 individual frames remain in frames/ and the zip.
Preview.mp4: review only, composited on dark and lavender cell-grid backgrounds; Boosting repeats its loop three times.
ContactSheet.png: numbered frames with a cell grid for size comparison.
Boosting/Chain_Reveal-Idle-Boosting.mp4: uninterrupted state-transition preview.

RE-RENDER:
Decode the original WebP losslessly to PNG; pass it to:
powershell.exe -NoProfile -ExecutionPolicy Bypass -File source/render_motion.ps1 -SourceImage <original.png> -Output <masters>
Put master subfolders Reveal, Idle and Boosting in ColorBall-Reveal-v1/review/masters, then run:
python source/package_motion.py
Requires existing System.Drawing, Pillow, numpy and the existing ffmpeg path specified in package_motion.py.

SCOPE:
This update changes animation assets under Design/Animation only. It does not install a new Unity gameplay controller,
change live Color Ball targeting rules, or modify Rocket, TNT, Propeller, HUD or levels.
The event/scale contract is in AnimationContract.json; existing game integration can consume these clips independently.
