# Animated facial registration correction

The previous modifier reshaped bind vertices and deform keys in the setup pose. That affine transform does not commute with subsequent independent rotations and translations of the moustache, eyebrow, lip and eye bones. Source animation remained present, but facial parts moved through inconsistent transformed spaces.

`animated-affine-drift.json` records the BEFORE comparison at 60 Hz against a coherent transform applied after the original skeleton's skinning, using the animated `on_yuz` axes and `yuz` anchor. It measures mathematical registration, not texture alpha quality or an aesthetic target.

| Portrait attachment | Maximum vertex discrepancy, source units | Animation | Time (s) |
| --- | ---: | --- | ---: |
| Bıyık | 26.621191 | k-1_uzulme | 0.400 |
| Sağ Kaş | 9.170577 | k-1_uzulme | 0.383333 |
| Sol Kaş | 6.891053 | k-1sevinme_6 | 0.216667 |
| Ağız | 5.143841 | k-1_sevinme_NEW | 1.866667 |
| Alt Diş | 3.067319 | k-1 animation | 10.266666 |
| Ust Diş | 3.067316 | k-1 animation | 10.266666 |
| Dil | 3.067305 | k-1 animation | 10.266666 |

AreaCompleted moustache discrepancy reaches 8.794684 source units at `king_drill_win_clap_start` 0.116667 s. The archived `AuditAnimatedFace.exe` contains the former setup-space modifier and is BEFORE evidence, not a validation of the new component. Its SHA256 is `CAEDCB710B676C22977737D71C248261419F99492CD1D423A66251021E6C107E`. Recompiling the audit against the replaced modifier would change what it measures.

## Implementation

`KingVanguardFaceShapeModifier` retains its asset identity and serialized proportions, but now only registers configuration and sets attachment alpha to zero for the obsolete `Taç Gölge Ust`, `Sağ Kaş Gölge`, and `Sol Kaş Gölge` overlays. It never edits source bind vertices, region offsets, weights, triangle indices, UVs, bones, constraints, deform frame arrays, timelines or key times. The default mouth scale is 1; existing serialized assets control the actual value.

`KingVanguardFaceRig` applies the shape in `OnPostProcessVertices`, after Spine has evaluated the original bones, constraints, deforms and animation mixing. Every frame starts from fresh original skinned vertices. No presentation result is written into `Slot.Deform` or reused as the next frame's source.

The global face transform is `anchor + F * Scale(face) * inverse(F) * (point - anchor)`, where `F` is the current `on_yuz` 2x2 world transform and `anchor` is the current `yuz` world position. The nose and pupils receive their extra scale around their respective current skinned centroids. Mouth, upper/lower teeth and tongue share the current skinned `Ağız` centroid and one mouth transform, preserving their original relative assembly. With mouthScale 1 they receive only the common face map.

The component supports `SkeletonAnimation` and `SkeletonGraphic`, including the latter's referencePixelsPerUnit scaling and multiple CanvasRenderers. `KingVanguardCharacter.Ready` ensures it is attached to the UI skeleton. Reviews and direct previews use `KingVanguardFaceRig.Ensure(skeletonAnimation)` or `Ensure(skeletonGraphic)`.

## Ownership, clipping and bounds

Atlas rectangle membership alone is unsafe: packed transparent corners overlap. Cached exact UV candidates are therefore matched to each active attachment's current ORIGINAL skinned XY position. Both face and nonface owners participate. For interpolated vertices, UV triangle barycentric coordinates must also reconstruct the original XY. Ambiguous different feature mappings leave the vertex untouched and increment a diagnostic. The mapper uses the buffer's live vertexCount, not backing-array capacity.

Shipped King facial slots remain before source clipping slot 32, including all three draw-order tracks. AreaCompleted facial slots remain before clipping slots 58/67; all ten draw-order tracks move earlier nonfacial slots. Thus current facial geometry is not cut by internal source masks. The barycentric fallback identifies clipped/interpolated vertices, but a future mask crossing the face would also need its clipping boundary transformed in the same space; this change does not redesign source masks.

Spine computes cached bounds before its vertex callback and assigns them later in FillVertexData. The component therefore measures the final used vertex buffer, then assigns the matching mesh bounds in OnMeshAndMaterialsUpdated. It also resubmits the UI mesh to CanvasRenderer. Multiple UI meshes use Unity 6's parameterless CanvasRenderer.GetMesh. It never calls MatchRectTransformWithBounds or changes framing/layout. Reviews should frame from the rendered mesh bounds after LateUpdate, because the source attachments intentionally retain their original geometry.

All ownership lists, source world arrays and UV lookup tables are cached per skeleton instance. Steady frames reuse them. Rebuilding/replacing the skeleton invalidates registration; disabling unsubscribes all callbacks. No shared source geometry is mutated, and repeated mesh builds start from original source positions.

## Diagnostics and validation scope

Public counters are finalized when a mesh update completes: `LastMappedVertices`, `LastInterpolatedVertices`, `LastAmbiguousVertices`, `LastUnattributedVertices`, and `ActiveFaceAttachments`. Unattributed vertices can be nonfacial clipped geometry and alone do not prove a face error. `IsRegistered` reports whether the loaded skeleton carries a matching registered modifier.

This implementation agent performed source/API review and whitespace checks, and corrected the multi-renderer GetMesh signature after the parent editor reported its compile error. Unity import, final compile, deterministic visual review, shared-source fingerprinting, mixed-animation playback, disable/re-enable checks and bounds checks are performed by the parent and the independent validation fixture. Do not treat this note as evidence those runtime checks passed. Freshly reload SkeletonDataAsset data when replacing the old modifier: previously loaded data may still contain its former bind edits.
