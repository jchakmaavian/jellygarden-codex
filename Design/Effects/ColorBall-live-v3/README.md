# Color Ball visual polish — 20 September 2026

The previous live sequence was functional but read as a slow, serial target scan with box-shaped flashes and a long wait before finishing. This pass changes only Color Ball presentation and event pacing in the independent JellyGarden-Codex project.

## Visible changes

- Beams launch approximately 0.105 game seconds apart and travel for 0.12 seconds. A narrow warm-white core, soft gold halo and moving four-point star show direction and arrival. Gentle curvature keeps the paths from looking like rigid wires.
- Target feedback uses a round gold glow, a bright white star, a short expanding ring and several drifting white/gold sparkles. It no longer uses the box-shaped target flash.
- Small white/gold stars orbit the charged ball and pull inward during its finish. The approved ball artwork, pivot and authored size transitions remain unchanged.
- The final impact fades over the finish instead of holding the loop. Playback accelerates the remaining fraction of the charged loop to reach its original seam within 0.16 seconds, then plays the authored finish normally. There is no arbitrary frame cut.

## Integration and scope

The director retains existing target selection/order and Rocket orientation rules. A small in-flight queue allows overlap, but each target changes only after its own beam arrives. Generated Rockets stay deferred until the Color Ball finishes. Refill remains blocked through conversion and finish.

The VFX renderer uses fixed pools of 16 beams and 192 light sprites, with three 64-pixel analytic masks generated once. It does not create/destroy objects or materials per beam/particle. Random decorative offsets use their own generator. ClearAll resets the pool; runtime materials/textures are released with the component. Everything is unmasked, above board pieces and below the existing HUD.

All 378 approved source animation/VFX frames remain unchanged. The old target VFX assets are retained; their display is replaced by the focused live renderer. Rocket, TNT, other booster implementation, UI layout and levels are untouched.

## Verification and evidence

`checks.txt` is the focused Unity Play Mode report. The existing QA entry-point suite covers Color-ball effect at 0.5×, three spawned balls with one activation at 1×, and Color Ball + Rocket at 1×. It also checks overlapping beams, one arrival per selected target, bounded pool reuse, no early Rocket launch/refill, cleanup, input availability and normal save restoration.

`spawn-activate-full.mp4` and `rocket-combo-full.mp4` are live Game-view captures encoded using recorded timestamps. PNGs show individual phases. These are runtime captures, not mockups; synchronous capture can cause brief recording stutters. No full-game QA or phone performance benchmark is claimed.

`ColorBall-Polish.zip` contains the four installed source files with metadata. `incremental.patch` compares them with this folder's pre-edit baseline. Apply only on top of the existing v2 integration. The review archive provides a focused backup without committing unrelated pre-existing workspace changes.
