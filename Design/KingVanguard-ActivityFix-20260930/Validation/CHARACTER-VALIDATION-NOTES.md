# Character framing and pupil evidence

These files are inside the independent Unity project, under `JellyGarden-Codex/Design/KingVanguard-ActivityFix-20260930/Validation`.

The original pupil reports were in **project** `Design/KingVanguard-Reference-20260929/ActivityFix`, not `outputs/Design`. They are copied here unchanged so the current delivery has one validation location:

- `pupil-containment-all-tracks.json`: before/after measurements across all 42 King/Area tracks.
- `pupil-containment-live-tracks.json`: the 11 portrait activities and five Area idle/clap tracks; 8,314 eye poses per scale sampled at 60 Hz.
- `pupil-fix-validation.json`: compilation, source-binary equality and implementation hashes recorded when the pupil change was made.
- `PUPIL-CONTAINMENT.md`: method, findings and limitations. This corrects horizontal overflow; it is not a claim of complete pixel containment during eyelid/cheek occlusion.
- `hud-aperture-framing-evidence.json`: APK hierarchy/bounds, previous framing gaps, final crop math, crown clearance and recorded maximum crown heights for all 11 live portrait clips.

Current-file readback confirms `GameplayFocus = new Rect(-184, 38, 368, 388)`, uniform scale and bottom anchoring, with zero extra top margin only for `GameplayPortrait`. Pupil scale is 1.1 in both the modifier and serialized asset. Global Bust framing stays unchanged.

At the current inner window size of 209.4 × 220.7 canvas units, the final uniform scale is 0.568814433 and the skeleton origin is (0, −131.964948). The earlier official-runtime probe sampled every crown vertex at 60 Hz across the 11 live portrait clips. Its aggregate minimum clearance from the arch is 4.485 canvas units (about 6.73 pixels at 1080 width); worst is the happy reaction at 1.2 seconds. All idle crown poses have at least 27.63 units of clearance. Separate per-clip clearance minima were not retained; the JSON explicitly distinguishes this from the per-clip maximum-height measurements that were retained.

The original APK frame spans roughly 428 source units, but its clear inner width is roughly 368. Fitting the removed outer frame into the new inner window left the cape's hard rectangle inset by approximately 13.24 / 10.27 / 20.08 canvas units at left / right / bottom. The final crop places the cape edges behind the custom arch instead. `frame_clipping` is left unchanged because it only affects the subsequent hand slots.

No animation or Unity check was rerun to assemble this evidence. Files were copied, prior measured framing results were recorded, and the current source/configuration was read to confirm the implemented values. No runtime files were modified in this evidence-finalization step.
