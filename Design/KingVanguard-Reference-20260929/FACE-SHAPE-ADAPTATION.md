# Vanguard face shape adaptation

The requested new face uses a broader, shorter facial silhouette and a smaller nose while retaining the recovered facial performance. This geometry adapter supports the newly redesigned artwork. It does not generate or replace any painted facial feature itself.

`Assets/Characters/KingVenguard/V2/Spine/KingVanguard_FaceShape.asset` configures a 1.15 horizontal / 0.92 vertical face map. Its final art polish adds a 1.05 horizontal / 0.85 vertical nose map, 1.55 iris/pupil scale, and 0.82 scale for the mouth/teeth/tongue group. `Tools/ImportVanguard.cs` assigns it to the King and AreaCompleted SkeletonData assets before loading them. Nightmare uses a different head rig and is not assigned this modifier.

## Registration

The exact per-rig bone rest transforms, slot names, attachment sizes, influence names, topology counts, and deform-animation membership are in `facial-rig-registration.json`. The two rigs share 27 facial atlas regions with identical original/trimmed dimensions and UV coordinates. King has the additional `Taç Gölge Ust` crown shadow. Their six meshes with different triangle lists remain separate; the adapter never transfers a mesh between rigs.

All facial regions participate: beard/face, beard shadow, mustache and shadow, mouth/teeth/tongue, nose, eyes/pupils/lids/lashes, brows and shadows, cheeks, ears, crown and crown shadows. This includes the lids and brow shadows attached to `on_yuz`, independently from the animated eye/brow bones. Leaving those sibling attachments unmapped would separate eyelids from eye whites during blinks.

The `yuz` head joint is the setup-space anchor. The `on_yuz` bone supplies the head's horizontal/vertical axes. Both rigs have the same facial geometry beneath these bones, although Area's head is parented to the torso and has a different overall scale. The nose and both iris/pupil attachments use their own transformed four-corner centroids. The mouth group (`Ağız`, `Alt Diş`, `Ust Diş`, `Dil`) uses the same `Ağız` setup vertex centroid, so all four parts retain their alignment. The nose's new alpha mask already makes a smaller button nose; its former extra uniform 0.78 shrink is superseded by the final 1.05 / 0.85 map.

### Crown shadow diagnosis

The brown oval floating above the shorter new portrait crown is `Taç Gölge Ust`, which exists only in the portrait rig. The lower `Taç Alt Gölge` remains at the crown/forehead junction. Review images showed the oval in the portrait and none in AreaCompleted, consistent with the attachment inventory. Modified setup-space vertical bounds corroborate this: the upper shadow spans 3.118513–3.790114 Unity units, crown spans 1.912375–3.632738, and lower shadow spans 1.884731–2.661163.

The obsolete upper shadow attachment has its own alpha set to zero by the modifier. Attachment alpha multiplies slot alpha, so animation slot-color keys cannot restore it. The attachment, source region, topology and animation keys remain present. The new shadow suppression is covered across every animation sample. The lower crown shadow remains unchanged apart from the common face geometry adaptation.

## Geometry and animation method

The SkeletonDataModifierAsset changes the freshly loaded in-memory attachment geometry. Original `.skel.bytes` files remain unchanged.

- Unweighted mesh positions are mapped from the slot bone's local coordinates into common setup space, transformed, and mapped back through the same bone.
- Every weighted vertex influence is mapped through its own setup bone and inverse. Bone indices and numeric weights remain unchanged.
- Region attachments retain the exact affine result in their four offset corners. This can represent shear caused by anisotropic scaling of an angled quad, which a region's rotation/scale fields alone cannot represent.
- Spine 3.8 unweighted deform keys hold absolute positions; these receive the full affine map. Weighted deform keys hold influence offsets; these receive the translation-free linear map. Frame arrays are cloned before changing setup arrays because empty-deform source keys may share the setup array.
- UVs, topology, durations, key times, bone timelines, constraints, draw order, events, and attachment switching remain unchanged. Repeated application to the same SkeletonData is guarded against double scaling.

This is a change to the head's bind geometry, with the original bone performance retained. It is not an exact screen-space squash at every animated pose. In particular, original bone translations are not scaled. Rendered expressions and large head rotations still require visual review with the final new artwork. Repacking/remapping skins later must preserve the adapted region corner offsets or reload the data and reapply the modifier; normal imported playback does not call region `UpdateOffset()` again.

## Verification

`Tools/VerifyFaceShapeOffline.cs` executes the actual imported Spine 3.8 C# runtime against the original King and Area binaries/atlases without launching Unity. The Unity types in the harness are minimal instantiation stubs; all skeleton loading, constraints, skinning, deformation and animation sampling use the project's official runtime. The modifier also compiled against the actual Unity and Spine reference assemblies.

- King: 28 attachments; 349 deformation timelines; 3,280 deformation keys; 87,735 compared vertex points. Maximum difference from the expected setup-space affine result: 0.000000834465 Unity units.
- AreaCompleted: 27 attachments; 110 deformation timelines; 625 deformation keys; 60,896 compared vertex points. Maximum difference: 0.00000667572 Unity units.
- All 42 tracks were sampled at 11 times with finite output geometry. Weights, bone indices, triangles, UVs and timeline key data remained unchanged. A second modifier application was stable.

Results: `Validation/face-shape-offline.txt`. These numeric checks do not substitute for the parent's Unity rendering and review of the new facial artwork.

The actual user-attached 1536×1024 turnaround sheet was not found among 11,916 searched non-V1 Design images. The two same-size matches belong to CoinClub and are unrelated. No earlier Vanguard V1 artwork was used in this adaptation.
