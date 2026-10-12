# rfv387 gameplay integration — validation pending

Updated 2026-09-29. No commit, push, APK build, normal-save edit, or reference-app launch was performed by this integration pass.

## Exact source and imported content

`Design/Backup/apk/royal_thron_rfv387.apk` was inspected offline. Its SHA-256 is `a2cbee4ecf021ee464dc6f17766ba6c0b2addb2d65007f611e5c368532aa8151`. The APK is unchanged.

- Imported missing native `TntTntComboFuseParticles` into `Assets/ReferenceGameplay/VFX/Resources/ReferenceGameplay/VFX/TntTntComboFuseParticles.prefab`: seven objects, six particle systems, one exact new sprite dependency. Unity import validation passed; reused materials/sprites were unchanged. See `Reference/missing-fuse-import-result.json`.
- Reused existing imported Rocket, TNT and Propeller native clips and particle prefabs. Thirteen clip curves/controllers and ten particle hierarchies were compared with this exact APK and match their existing imports. This is not a claim that every texture or encoded audio payload is identical.
- Ordinary Rocket launch is code-driven in the source; the unrelated Fireworks Rocket clips were not substituted.
- Detailed file IDs, durations, dependencies and provenance: `Reference/README.md`.

## Runtime changes currently in Assets

- Propeller: remaining-objective prioritization, payload-footprint scoring, destination reservations/revalidation, carried TNT/Rocket visuals with near/far order, native paired effects/audio, continuous takeoff/flight handoff, and one payload handoff on empty-target fallback.
- Falling: render-frame, elapsed-time capped acceleration; removed short-distance snapping and overshoot; cancellable settling; metal booster roots remain rigid. Recovered Propeller fall/landing clips connected.
- TNT: dedicated native TNT+TNT preparation clip and fuse socket/effect before the existing single combo blast. Cached activation actors correctly switch native view kind. Normal TNT's recovered creation clip remains full-length.
- Color Ball: eligible-majority direct targeting versus explicit ordinary-gem swap color; progressive TNT/Propeller conversions through existing handoffs; ordinary targets hold a small charge presentation and clear together after firing; arrival latching prevents missed hits when a beam is retired and reused during a long frame. Approved body/core/aura/beam rendering assets are unchanged.
- Pool shutdown no longer recreates the native visual pool during application teardown.
- Fixed a diagnosed board lock: duplicate 2×2 groups recolored the same piece twice per regeneration pass, repeatedly undoing the first recolor. Each physical item is now recolored once per pass. Logical gem color also retains its chosen palette index rather than rediscovering it from replaced artwork.
- Rocket speed, timing and appearance were not blindly retuned from metadata constants with unknown units. Flight frame and native VFX-instantiation measurements were added.

These gameplay adapters and motion calculations are reconstructed C#, not recovered editable IL2CPP method bodies. Exact native tie-breaking, Rocket acceleration formula, TNT combo radius/ring interval and Color Ball disappearance timestamps remain unverified.

## Remaining patches staged, not yet installed

`Review/fall-color-ownership.patch` and `Review/Proposed/` contain a reviewed follow-up for overlapping Color Ball target ownership, preventing another converter's booster from firing early, and explicit conversion-contact scale restoration on cancellation. The proposal includes corresponding 30/60-cap regressions. Base file hashes must be checked before applying; shared-file changes must be merged, not overwritten.

`Review/Propeller-creation-convergence.apply_patch.txt` contains the prepared Propeller creation convergence correction, with adjacent source hashes and notes. Existing Level 4 reference footage shows a five-piece cluster containing a 2×2 moving into the selected cell before native creation. The loading card verifies 2×2 creation. The footage does not establish isolated four-piece destination selection. The proposal preserves existing rule/host selection; only consumed-gem presentation converges.

## Actual validation so far

- Native missing-fuse import validation: PASS.
- Earlier diagnostic gameplay exercised horizontal/vertical Rocket, TNT reveal/activation and dedicated TNT+TNT, including cascades, refill, input return and normal-save restoration, at 30/60 caps. **That run used a manual diagnostic recolor to isolate the refill lock; its `ALL CHECKS PASSED` line is not final acceptance evidence.** A clean rerun after the real regeneration fix is required.
- Diagnostic Rocket frame observations: roughly 29.4–30.0 FPS and 59.5–59.9 FPS mean; cold flight maximum about 60 ms versus warm ~26 ms. Native VFX instantiation measured under 1 ms. This establishes frame-time spikes, not their complete profiler root cause. No speed reduction was used to mask them.
- At 07:32 UTC, all 92 tracked protected level/scene/settings/approved Color Ball files matched `protected-assets-before.json`.
- Focused fall/Color Ball and Propeller runtime suites have not completed. No final visual recording or exact same-action comparison is claimed.

## Why validation is waiting

Another session ran Home checks, re-entered Play Mode and started a test profile while this pass was preparing validation. At 07:39 UTC Unity was both playing and compiling, with the other test profile active. This pass did not stop or take over that session. A user coordination question is pending.

The exact APK also has previously verified native-code modifications and an injected downloader. Permission to use the already-installed verified same-version build for live comparison remains pending. Existing official same-version recordings were used for bounded visual observations and are identified as such; they are not represented as execution of the supplied APK.

## Resume order

1. Obtain the free Unity window; leave other sessions' saves/test profile untouched until released.
2. In Edit Mode, check current file hashes, merge the staged follow-ups, refresh and verify compilation.
3. Run `checks`, `fall-color`, and `propeller` sequentially through `Design/Gameplay-rfv387/editor-request.txt` on isolated saves. Fix genuine failures, then capture the changed live actions.
4. Recheck protected hashes, cleanup and normal-save restoration; update this report with actual final evidence. Do not commit or push.

## Main changed files

- `Assets/JellyGarden/Scripts/Item.cs`, `LevelManager.cs`
- `Assets/JellyGarden/Scripts/Juice/RoyalPowerupDirector.cs`, `RoyalTntNativeSequence.cs`, `RoyalRocketSequence.cs`
- `Assets/Boosters/Propeller/V5/Scripts/PropellerGameplayV5.cs`, `PropellerVisualV5.cs`, `Editor/PropellerReferenceChecks.cs`
- `Assets/Boosters/ColorBall/V4/Scripts/ColorBallV4Sequence.cs`, `ColorBallVfxV4.cs`, `ColorBallChargedTargetV4.cs`
- Read-only objective accessors in `RoyalCampaignBlockers.cs` and `RoyalCampaignItem.cs`
- `Assets/ReferenceGameplay/Scripts/ReferencePieceVisual.cs`, `ReferenceActivationVisual.cs`, `ReferenceVfxPool.cs`
- Focused VFX importer extension, new `ReferenceRfv387FuseImporter.cs`, imported fuse prefab/texture and metadata
- `Assets/ReferenceGameplay/Editor/ReferenceRfv387Checks.cs`, `ReferenceFallColorChecks.cs`

Earlier coin work and other sessions' existing changes remain separate; the entire working-tree diff is not attributed to this task.
