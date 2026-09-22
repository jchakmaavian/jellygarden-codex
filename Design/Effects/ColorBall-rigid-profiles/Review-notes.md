# Color Ball: rigid ReferenceMatch and Enhanced

Both profiles use the existing V5 prefab, two artwork layers, shared pivot, pools, gameplay events and audio. Enhanced is the runtime default. In Play Mode, use **Tools → Royal Throne → Color Ball V5 → Presentation** to select either profile for subsequent pooled activations.

**ReferenceMatch:** fixed metal sphere with mounted colored details, shared rigid orientation, bounded continuous local vibration and rotational exposure. Changing outer colored light is optical blur, not shell deformation. Finite curved near/far wisps replace the continuous neon orbit. Warm beam shoulders, travelling highlights and compact direction-linked contacts replace uniform connections.

**Enhanced:** the same physical motion and sequence clock, with richer wisp cross-sections, concentrated travelling beam energy, warm curved contact expansion and detail in the brief final release. No particle-count increase, global bloom change or audio rebuild.

## Comparison and remaining differences

The comparison uses the original Royal Match source, equal board-cell scale, 1× playback, unchanged exposure and separate activation/discharge alignments. All panels receive the same crop scale in each chapter. The closeup is explicitly a source closeup; normal scale shows target contacts. Royal Match is encoded at 60 FPS; our gameplay captures contain 30 distinct frames per second. Comparison audio is the Enhanced game's audio only.

The rigid Royal Throne shell remains more visible, and peripheral color traces are more separated than Royal Match's broad optical blur. The retained gameplay charge gate is 0.62 s, so the first link starts later than the reference's approximately 0.20 s after spin-up. Beam travel also follows existing distance-based gameplay timing; the first test contact takes approximately 0.27 s versus the reference's estimated 0.08–0.13 s. These differences are visible, not hidden by retiming. Our complete fixture converts 15 targets; the reference visibly converts about 18. No exact RPM or quantified visual improvement is claimed.

## Validation

Focused Play Mode checks passed for both profiles: rigid shared transforms, attached source, distinct contacts/conversions, reveal/idle, final release, existing Rocket handoff, repeated pooled use and cleanup. Normal player data was restored. Completed Color Ball sessions produced no new console errors or clipped audio samples.

| Profile | Actual 30 FPS mean / median | Actual 60 FPS mean / median | 60 FPS p95 frame time |
|---|---|---|---|
| ReferenceMatch | 30.01 / 29.93 | 60.00 / 59.44 | 18.40 ms |
| Enhanced | 30.00 / 30.05 | 60.03 / 58.44 | 24.15 ms |

These are uncaptured Editor measurements, not a mobile performance guarantee. Recording uses fixed 30 FPS game steps and is excluded from performance results. An initial 60 FPS pacing check narrowly failed; the focused Game-view rerun passed the unchanged threshold. The short-test cancellation race was corrected by cancelling through the existing game service before asserting empty visual/audio pools. Editor mode transitions also showed Home navigation exceptions outside the isolated test sessions; Home/UI code was not modified.

## Changed files

Under `Assets/Boosters/ColorBall/`:

- `V4/Scripts/ColorBallVisualV4.cs`: V5-only rigid presentation support and seconds-based spin integration.
- `V4/Scripts/ColorBallVfxV4.cs`: attached beam layers and connected local contact rendering.
- `V5/Scripts/ColorBallV5Presentation.cs`, new `ColorBallPresentationSettings.cs`: rigid pose, local energy and two presentation profiles.
- `V5/Shaders/ColorBallSphere.shader`, `ColorBallLight.shader`: fixed spherical surface/blur and flowing light.
- V5 profile assets, existing prefab references, local material properties and builder revision.
- `V5/Scripts/Editor/ColorBallV5Checks.cs`: focused capture, actual 30/60 FPS and cleanup checks; review reports.

Pre-task source and the Phase A baseline are checkpointed in [Before-rigid-reference-pass.zip](C:/Users/chakm/Documents/Codex/2026-09-08/144/colorball-rigid-work/Before-rigid-reference-pass.zip) and [ReferenceMatch-baseline.zip](C:/Users/chakm/Documents/Codex/2026-09-08/144/colorball-rigid-work/ReferenceMatch-baseline.zip). Existing unrelated edits were preserved. Rocket, TNT, Propeller, targeting, conversion rules, UI and global settings were not edited.
