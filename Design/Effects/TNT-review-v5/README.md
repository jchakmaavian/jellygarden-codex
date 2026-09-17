# TNT explosion refinement and continuous replay

The v4 comparison against Royal-Match-TNT-2.mp4 showed a flat white disk, an empty band inside the wave, a continuous yellow ring, sharp spokes, repeating circular cell impacts, and debris concentrated at the perimeter. This revision changes only TNT explosion rendering and the TNT-specific fragment impulse.

- The compact white flash breaks up earlier into illuminated gold/white patches. Broader irregular rays and a textured light field overlap the expanding wave.
- The wave is discontinuous and softer, with pale gold and white highlights.
- Cell impacts briefly flash, then retain six varied local sparkles rather than persistent identical circles. Their positions and twinkle differ between cells.
- Explosion stars spread through the interior. TNT fragment impulses vary so some chips linger locally while others burst outward, before existing gravity/fade takes over.
- Soft warm residual light overlaps the colored falling debris. Cleanup remains at 0.86 seconds.

Original TNT artwork, reveal, ignition, damage order, damage radius, pool sizes, fragment count, gravity and lifetime are preserved. Rocket, other boosters, HUD, levels and serialized metadata were not edited for this revision. Existing fixed renderer and fragment pools are reused; shader variation adds no particle objects. This is a closer interpretation of the reference, not a verified frame-identical reproduction.

## Verification

Unity 6000.5.10f1 compiled the changed scripts/shader and ran the disposable TNT fixture at 0.25x and 1x. All 17 checks passed: original sprite reveal/idle, layered blast colors, overlapping residual effects/debris, pooled reuse/cleanup, preserved coins/lives, and no runtime errors during the checks. The original board was restored, then Play Mode was stopped. No full-game QA or device GPU benchmark was performed. The zero editor p95 field in checks.txt is not a performance measurement for this short-circuit TNT suite.

- `TNT-normal.mp4`: 78 consecutive rendered frames of activation through cleanup at 60 simulation frames/second (1.3 seconds). Capture uses a temporary fixed capture delta to avoid screenshot encoding overhead changing the simulation step.
- `TNT-quarter-playback.mp4`: the same recorded sequence played at one-quarter speed for inspection; distinct quarter-speed runtime checks and phase PNGs also passed.
- `blast-sequence.jpg`: eight sequential samples from the continuous replay. Reference samples were inspected separately at comparable board-cell scale.
- Phase PNGs show reveal, idle, ignition, blast, rays and falling debris at both tested speeds.

## Source backup

`verified-source` stores the five changed source files and their original metadata. Runtime edits are limited to TNT3DAnimator.cs, RoyalTNTGlow.shader, and the TNT-only velocity branch in CandyShatterService.cs. The two editor files update the review output and add bounded replay capture, restoring capture timing in finally.

`incremental.patch` compares against the v4 verified snapshot; the fragment-service baseline is included in `baseline`. This review directory is backed up independently because the project contains earlier unrelated uncommitted changes. It is an incremental review package, not a standalone Unity project.
