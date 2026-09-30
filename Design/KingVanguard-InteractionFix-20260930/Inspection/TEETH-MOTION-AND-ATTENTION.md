# Teeth motion and attention integration

The untouched source teeth do not invert. The source audit sampled all 42 King/Area tracks at 60 Hz: 16,428 portrait tooth-row samples and 3,710 Area tooth-row samples, with zero triangle inversions. Most rows are already almost rigid. Making every new tooth conspicuously white exposed movements originally painted as subdued brown mouth bands; geometry alone cannot repair that artwork interpretation.

The exact source data, method and every sampled pose are recorded in `teeth-motion-audit.json` and `teeth-motion-60hz.csv`. `AuditTeethMotion.cs` and its response file/executable reproduce the analysis using the project's official spine-csharp runtime without Unity.

| Source pose | Maximum deviation from one rigid row with uniform scale | Smallest signed triangle area ratio |
| --- | ---: | ---: |
| Portrait lower row, worry entrance 0.383333 s | 6.409027 source units | 0.450693 |
| Portrait upper row, worry/relief | about 0.80 source units | about 0.42 |
| Portrait lower row, idle_loop_2 | about 0.134 source units | about 0.99 |
| Portrait upper row, idle/happy/sad | less than 0.00003 source units | approximately 1 |
| Every Area row | less than 0.000115 source units | approximately 1 |

At the worst lower-row worry pose, source tooth bounds in `on_yuz` coordinates are `[-149.365646, -117.050964, -80.403801, -77.945969]`. The fitted row is `[-150.169281, -114.402573, -77.947884, -81.021980]`. Mouth outer bounds are `[-194.279343, -128.115204, -44.354855, -47.007767]`. Fitted/source tooth bounding-box intersection-over-union is 0.820509; the fitted row stays within the outer mouth box with at least 13.712631 units clearance. This is an outer-bounds check, not proof of the alpha slit's visible occlusion. The parent must inspect that worry pose with the installed new textures.

## Implemented correction

Only the two tooth rows receive stabilization. `KingVanguardFaceRig` caches their original setup vertices in face coordinates. Each pose fits an orientation-preserving similarity to the current originally animated row: translation equals its current centroid, rotation and one uniform scale minimize vertex error. The resulting row then receives the existing common face transform. This retains the authored travel, opening timing, draw order and overall scaling, while removing nonrigid within-row bending that is unsuitable for visible enamel. The lower row remains behind the foreground lip cover; the upper row remains behind the moustache.

No source attachment, bone, constraint, Slot.Deform, FFD key or animation state is rewritten. Original skinned positions still identify vertex ownership; tooth correction maps only final emitted points, including interpolated UV points. `StabilizeTeeth` is a public bool property backed by the default-true serialized `stabilizeTeeth` field for A/B review. `MaximumToothCorrection` reports the current largest correction in skeleton units; divide by SkeletonDataAsset.scale to obtain original source units.

`Tools/VerifyTeethGeometry.cs` is a separate Unity eval_file fixture for the parent to execute. It tests the actual emitted meshes across all 42 tracks and mixed transitions, verifies pairwise tooth distances share one scale, preserves current source centroids and triangle orientation, and fingerprints original shared geometry/weights/timelines before and after. It is not a second implementation of the similarity fitter. Its results belong under `Validation`; the presence of the authored fixture alone is not a passed test.

## Attention

The mapper consumes the independent gameplay-only `KingVanguardAttention` component through `Ensure(graphic)`. Its source priorities, target smoothing and lifecycle gates belong to that component.

Pupil attention replaces the current source gaze center toward a target measured inside the current eye whites, blended by `Influence`. It does not add unrestricted mouse offsets to the existing source gaze. Each full scaled pupil quad is conservatively fitted inside an ellipse whose radii are 46% of the white's face-space bounding-box dimensions. Source eyelids retain their original drawing order and alpha, so a blink still occludes the pupil. Numerical rectangle/ellipse containment is conservative geometry, not independent pixel-alpha proof; the separate rendered-pixel fixture checks installed art and nonzero cardinal displacement.

The complete rendered portrait follows `BodyLean` by at most 1.4625 degrees about the source `pelerin` torso/cape pivot. TapResponse adds at most 0.4% width and -0.8% height scaling about that pivot. All changes happen after original skinning; no source root/bone motion changes. Source reaction priority and fade-out come from the attention component. Mesh bounds are recalculated from these final vertices. Public `GazeInfluence` and `BodyLeanDegrees` support review diagnostics.

This agent authored code and ran the offline original-motion audit only. Parent Unity compilation, rendered checks and the runtime fixture results must be reported separately.

## Parent-executed geometry result

The subsequently produced `Validation/teeth-geometry-checks.txt` reports PASS for all 42 tracks: 10,729 actual rendered poses, 21,458 row checks and 135 mixed frames. Maximum row-centroid error is 0.00006078505 original source units; maximum pairwise scale spread is 0.00005662441. Triangle orientation stayed positive and both shared-source fingerprints remained unchanged. This confirms the fitted teeth move as coherent rows while retaining their source centroids. It does not replace the separate mouth-alpha occlusion and gaze-pixel checks.
