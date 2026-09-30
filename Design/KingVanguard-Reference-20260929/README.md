# King Vanguard — fresh APK import and redesign

This V2 delivery starts from **`Design/Backup/apk/royal_thron_rfv387.apk`**, independently of the previous Vanguard artwork and rig. The requested APK, both core packs and all 22 packaged Android bundles were inspected. Source hashes, structures and extraction limits are in [SOURCE-VERIFICATION.md](SOURCE-VERIFICATION.md).

| Imported structure | Animation inventory | Review |
|---|---:|---|
| King portrait, Spine 3.8.84 | 31 playable tracks + empty source `_3` | [Portrait sheet 1](AnimationReview/King_Vanguard-page-01.png), [2](AnimationReview/King_Vanguard-page-02.png), [3](AnimationReview/King_Vanguard-page-03.png), [4](AnimationReview/King_Vanguard-page-04.png) |
| AreaCompleted, Spine 3.8.99 | 10 tracks; articulated upper body | [Area sheet 1](AnimationReview/AreaCompleted_Vanguard-page-01.png), [2](AnimationReview/AreaCompleted_Vanguard-page-02.png) |
| Nightmare/bedtime, Spine 3.8.99 | 6 tracks; king, dog, book and bedding | [Bedtime sheet](AnimationReview/Nightmare_Vanguard-page-01.png) |
| Native KingDrill, Unity hierarchy | 6 clips; genuine full body and legs | [Full-body sheet](AnimationReview/Native_KingDrill-page-01.png) |

**53 nonempty animations** were rendered at four times each; the empty source track is retained and labelled. The [manifest](AnimationReview/contact-sheet-manifest.json) records exact sample times and bounds, with zero render errors in the recorded run.

The final redesign replaces the original facial artwork: the new King has a rounder face, short pink button nose, larger brown eyes, thick dark brows, a full white beard and moustache, and a blue-velvet gold crown. The visible face parts use newly generated paintings; they do not retain the earlier Vanguard artwork or fall back to the reference King's facial RGB. The blue-and-gold outfits remain consistent with the supplied character direction.

[Final expression preview](FaceRedesign/Review/New-face-expressions.png), left to right: **Idle, Happy, Worried, Sad**.

The [distinct-face master](FaceRedesign/King-Vanguard-distinct-face-master.png) and [master prompt](FaceRedesign/master-prompt.txt) record the new identity. Built-in image generation repainted the isolated source-part workboards; technical C# tools then registered the new paintings to the existing UV rectangles:

| Face workboard | New painted board | Generation prompt | Packed atlas proof |
|---|---|---|---|
| [Portrait: 27 shared parts](FaceRedesign/Portrait/Portrait-source-workboard.png) | [Portrait painting](FaceRedesign/Portrait/Portrait-painted-workboard.png) | [Prompt](FaceRedesign/Portrait/paint-prompt.txt) | [Portrait report](FaceRedesign/Packed/King_Vanguard.pack-report.json), [Area report](FaceRedesign/Packed/AreaCompleted_Vanguard.pack-report.json) |
| [Native full body: 13 parts](FaceRedesign/Native/Native-source-workboard.png) | [Native painting](FaceRedesign/Native/Native-painted-workboard.png) | [Prompt](FaceRedesign/Native/paint-prompt.txt) | [Native report](FaceRedesign/Packed/KingDrill_Vanguard.pack-report.json) |
| [Bedtime: 8 parts](FaceRedesign/Nightmare/Nightmare-source-workboard.png) | [Bedtime painting](FaceRedesign/Nightmare/Nightmare-painted-workboard.png) | [Prompt](FaceRedesign/Nightmare/paint-prompt.txt) | [Bedtime report](FaceRedesign/Packed/Nightmare_Vanguard.pack-report.json) |

The pack reports verify zero original facial RGB fallback, zero changes outside the selected face islands, and exact saved-PNG bytes. Transparent edges use colors from the new painting. [Registration checks](FaceRedesign/registration-validation.json) cover rotation and shared portrait-to-area coordinates. [SurfacePaint](SurfacePaint/) retains the earlier outfit painting stage; the current facial stage is in `FaceRedesign/`.

All three original Spine skeleton binaries remain unchanged. A [face-shape adapter](FACE-SHAPE-ADAPTATION.md) changes loaded portrait/area attachment geometry and the corresponding deformation values while preserving source UVs, weights, animation times and bone performance. The native full-body rig also uses adapted face geometry with its original clips. Bedtime receives the new artwork with its original geometry. Runtime assets, scripts and prefabs are under [`Assets/Characters/KingVenguard/V2`](../../Assets/Characters/KingVenguard/V2/).

## Main-game integration

- The new portrait replaces gameplay HUD, home avatar/dialog and result-card character slots. The leaderboard's existing full-body illustration uses the original native KingDrill rig, playing its source Win then Idle through a transparent UI renderer.
- Live moves trigger worry at three or fewer moves; real extra-move grants, power-up reactions, goal completion and result cards trigger their corresponding original clips. Queues are bounded and cleared when a board attempt changes or a screen closes.
- Area completion uses the separate original drill/clap animation family. Source entrance/exit clips are retained. Legacy `king_area 1` and `king_area_2` remain review-only because their raw setup needs the original initialization; they are not used for the live celebration. See [area QA](AREA-STANDALONE-QA.md).
- Full-body KingDrill and bedtime are also delivered as reusable prefabs. Bedtime remains available for review and future placement; this does not introduce the source game's drill/nightmare minigames or claim every activity has a gameplay trigger.

Exact animation-role evidence and the Royal Throne event adapters are separated in [binding evidence](Logic/ACTIVITY-AND-ANIMATION-EVIDENCE.md) and [activity adapter](Logic/ACTIVITY-ADAPTER.md). **Original IL2CPP method bodies were not recovered.** Native KingDrill applies 3,707 matched curves; **27 unresolved native transform curves** are retained as evidence but remain unapplied. See [native implementation](Logic/KingDrill/IMPLEMENTATION.md) and [import report](Logic/KingDrill/unity-import-report.txt).

## Review and verification

In Unity 6000.5.10f1, open **Royal Throne → King Vanguard → Animation Review**. Select any of the four structures, scrub a source track or play it. The viewer uses an isolated preview scene. [Review instructions](ANIMATION-REVIEW.md) include contact-sheet regeneration.

Recorded evidence:

- [Runtime state checks](Validation/runtime-checks.txt), [activity prefab checks](Validation/build-activities.txt), [all-animation render report](Validation/render-all-status.txt), and [live source/dependency audit](source-consistency.json).
- [Live gameplay activity checks](Validation/live-activity-checks.txt): assisted fixtures use the real low-move, continuation-purchase and goal-completion paths through the win result. This is not a claim of natural player input.
- [Live full-body UI checks](Validation/live-native-ui-checks.txt): source Win returns to Idle; hiding releases the render target and suspends the actor; reopening reuses the same actor and disabled camera.
- [Live area-completion checks](Validation/live-area-checks.txt): the chest ceremony and reward stage reach the new King playing the imported clap family.

Captured integration flows: [home](Validation/Home-avatar.png), [gameplay idle](Validation/Gameplay-idle.png), [worry](Validation/Gameplay-worry.png), [extra moves](Validation/Extra-moves.png), [goals complete](Validation/Goals-complete.png), [win result](Validation/Win-result.png), [failure](Validation/Failure.png), [area celebration](Validation/Area-celebration.png), and [full-body leaderboard](Validation/Leaderboard-fullbody.png). All captures were refreshed with the final facial artwork on 30 September 2026. Live reports verify event timing and UI lifecycle; the newly rendered animation sheets assess the redesigned face across source activities. The final Unity error-console query returned zero errors, and the isolated QA session restored the normal player save before leaving Play mode. These checks cover Unity editor/runtime assets and rendering; they are not an Android device test or a claim of complete source-game reconstruction.

## Recovery scope

The [backup preparation script](Tools/Create-DeliveryBackup.ps1) defaults to a read-only plan. After final verification, `-Finalize` creates a verified ZIP and SHA-256 manifest under `Delivery/`, containing the eight complete current host source files and their metas. Those snapshots include preexisting work; their live diffs are excluded from the task-only staging allowlist. The allowlist includes the new V2 package, parent metas and this evidence folder, excluding V1, caches, pending files and compiler scratch outputs. The script never stages, commits or pushes.

This recovery overlay requires the matching current project baseline; it is **not a self-contained restoration onto the base commit**. In particular, the existing `RoyalCardKit.cs` dependency is untracked and `RoyalGameplayHud.cs` has preexisting changes. The Spine package is already tracked and clean (542 files) and is not duplicated into the archive.
