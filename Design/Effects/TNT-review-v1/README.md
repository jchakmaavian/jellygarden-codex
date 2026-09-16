# Royal Throne — TNT visual implementation

Implemented in the independent JellyGarden-Codex project on 16 September 2026. Original J: Unity project untouched.

## Result

The board TNT now uses the pooled `RoyalTNT/TNT_3D` mesh prefab instead of its static sprite. Its red barrel, gold bands/crown badge, separate lid and curved fuse use simple Unity geometry and a dedicated shaded material. The orthographic board and HUD hierarchy are unchanged.

`TNT3DAnimator` owns the cosmetic state sequence: Reveal, Idle, Activated, Ignition, Explosion, Debris, Complete. Body stretch, lid lift/tilt, delayed fuse wobble, attached white/yellow rim flame and four-point sparkles settle in 0.50 seconds. Activation takes 0.15 seconds; the blast starts immediately afterward, and existing TNT damage begins at approximately 0.22 seconds. Decorative tails finish separately, without holding gameplay until debris cleanup.

The explosion contains a white-hot center, yellow/gold glow, radial rays, shockwave, local cell flashes, sparkles and pale expanding light/smoke. The existing colored-fragment service supplies eight small and two medium chips per cell when capacity allows. Outward/upward motion transitions into gravity, rotation and smooth fade/shrink over 0.62–0.85 seconds.

TNT actors use a fixed 32-slot pool; fragments retain the existing 240-slot pool. Heavy overlapping blasts degrade within the existing fragment budget rather than allocating more objects. No packages, gameplay rules, damage radius, levels, UI layout or Rocket visuals were changed. The VFX preview's TNT action uses the new visual sequence.

## Verification

Unity 6000.5.10f1 compiled the runtime and editor scripts. The isolated TNT fixture passed 20 checks at actual TimeScale 0.5 and 1.0, including real T-match creation, reveal timing, independent lid/fuse motion, head flame, clean idle, ignition/explosion, dense fragments, cleanup, same-object pool reuse, cancellation and unchanged coins/lives. No runtime errors were logged by the fixture. See `checks.txt`.

Ten screenshots record reveal peak, idle, ignition, explosion peak and falling debris at both speeds. Reference inspected as a compact 10 fps frame sheet from `TNT-animation-vfx.mp4`, focusing on its reveal and explosion sections. Final captures were visually reviewed. Unity was left out of Play Mode after the host board was restored.

This was TNT-only validation, not full-game QA or a device performance benchmark. Dedicated blocker/chained-booster regression was not run. The existing blocker damage path remains in use, with a TNT fragment snapshot before destruction.

Rocket sequence, Rocket fire shader and EffectsProfile hashes match their pre-task values (`rocket-unchanged-baseline.json`).

## Developer handoff

- Runtime controller: `Assets/JellyGarden/Scripts/Juice/TNT3DAnimator.cs`.
- Mesh prefab/materials: `Assets/JellyGarden/Resources/RoyalTNT/`.
- Dedicated shaders: `Assets/JellyGarden/Shaders/RoyalTNTBody.shader` and `RoyalTNTGlow.shader`.
- Integration: PACKAGE-only branches in Item/RoyalPieceSkin and TNT explosion/preview branches in RoyalPowerupDirector; optional TNT debris branch in CandyShatterService.
- Test menu: Tools → Royal Throne → TNT → Verify TNT only at half and full speed. Use the menu directly because Ctrl+Shift+F8 conflicts with an existing capture shortcut.
- `verified-source/` contains exact files and Unity metas for this implementation. Shared files include earlier project work; `tnt-only-integration.patch` isolates this task's changes against its starting snapshot. Do not replace an independently modified developer checkout blindly.

The backup commit is deliberately scoped to this review directory because the working project contains substantial pre-existing uncommitted development.
