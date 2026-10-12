# Propeller integration evidence — 2026-09-29

## Source evidence
- Exact APK inspected: `Design/Backup/apk/royal_thron_rfv387.apk`; IL2CPP metadata names/constants read directly from its global-metadata.dat. Original method bodies are compiled, not recovered editable C#.
- Existing original-reference gameplay recording: `Design/RoyalMatchReference/recordings/journey_013_031638.mp4`, 98.3–99.8 s, Level5 TNT+Propeller tutorial swap. Its recorded app version is38174; its broader provenance is in `Design/RoyalMatchReference/OBSERVATIONS.md`.
- Consecutive .1s samples show both bodies sharing a moving centre, circling side by side and changing front/back order; lit fuse remains on TNT; TNT detonates at destination.
- The reference Level4 tip explicitly shows a2×2 equal-colour square creating aPropeller. Four-gem convergence timing was not established by the inspected gameplay segment. No speculative merge animation was added.

## Imported assets reused
- `Assets/ReferenceGameplay/Animation/Resources/ReferenceGameplay/Visuals/Propeller.prefab` and its existing controller/clips.
- `Assets/ReferenceGameplay/VFX/Resources/ReferenceGameplay/VFX/TntPropellerComboFuseParticles.prefab`.
- `Assets/ReferenceGameplay/VFX/Resources/ReferenceGameplay/VFX/PropellerRocketComboParticles.prefab`.
- `Assets/ReferenceGameplay/Audio/Resources/ReferenceGameplay/Audio/propeller_tnt_combo__15680.wav`.
- Existing catalog TNT and both Rocket sprites, preserving their current artwork and native pivots/PPU.
- These were already imported from the older APK. No new asset import is claimed in this pass; exact-rfv387 compatibility requires the separate asset audit.

## Reconstructed changes
- Active runtime remains `PropellerGameplayV5` → `PropellerVisualV5`; the older RoyalPropellerV2Sequence is not changed.
- Remaining objective hits take priority over legacy distance/random tie-breaks. TNT/rocket target evaluation uses its real affected footprint, and shared multi-cell blockers count once. Completed objectives get no bonus. Exact native target weighting/tie-breaks are not recovered.
- Spent takeoff destinations are released and reselected; concurrent destinations remain reserved.
- Native sprite pair, near/far sorting and smooth end convergence follow observed TNT pairing. Pair motion curves are reconstructed, not imported native pilot code. Projectile movement timing is preserved.
- Native fuse/combo particles and local SFX are attached to the pair; leased visuals and sustained particles stop at contact, cancellation and pool return.
- Rocket target delay .07s is a verified `PropellerRocketStrategy.TargetExplodeDelay` metadata constant.

## Verified metadata / boundaries
- `PropellerSpecialComboPilot`: otherItem, otherItemView, ArrangeRootPositions, ArrangeSorting; ComboExpandDistance=1.3. Supports separate paired visual system.
- `DefaultPropellerPilot`: OutDuration=.75 and MaxScale=1.5 are pilot parameters, NOT a proven full-flight duration; no total-time change based on these alone.
- `LightballPropellerStrategy`: GetReplacedItemForLightball, CreateBgForReplacedItemForLightball, SingleRayReached. Supports the progressive conversion handoff handled by the Color Ball agent.
- Rocket pairing choreography and ColorBall+Propeller special variants still need direct reference gameplay comparison. Current Rocket pair uses the shared-pilot evidence; not claimed frame-exact.
- Old recovered clips: creation1.316667s, tap start.066667s, swipe start.15s, fly loop.15s, fall start.1s, landing/end-fall.55s. Total takeoff timing was deliberately not inferred from a short start clip.

## Validation
`PropellerReferenceChecks.Run()` checks live objective selection/update, TNT+both Rocket pair launches, five concurrent destinations, repeated pooled use, cancellation, input recovery and VFX/audio cleanup in the isolated QA profile. Actual run results belong in `Runtime-checks.txt`; compilation alone is not a completed gameplay comparison.

Checkpoint: `Design/Backup/Propeller-rfv387-20260929-124102` (four affected files only).
Changed runtime files: V5 `PropellerGameplayV5.cs`, `PropellerVisualV5.cs`; readonly `RoyalCampaignBlockers.GoalAt` and `RoyalCampaignItem.GoalName` hooks. Added focused editor check file +meta. No commit/push, level changes, UI, startup, saves or BGM changes.

## Follow-up runtime fixes
- A paired takeoff now preserves both bodies' exact world positions when gameplay takes translation ownership.
- The reused TNT fuse particles follow the visible rope tip of the current Sun-TNT sprite. This anchor is mapped from its source artwork; it is not claimed to be the native animated prefab socket.
- An empty destination retains one local/last-valid payload handoff instead of silently consuming the Rocket/TNT. Tests count handoffs and reservation cleanup.
- Main-game QA found a separate startup/refill lock: duplicate 2x2 groups could recolour the same piece twice in one regeneration pass and undo the first change. Runtime instrumentation established thousands of repeated passes, with the Unity fixed loop progressing normally. `LevelManager.RegenMatches` now visits each physical piece once per pass. No input gate is forcibly cleared.
- Added an actual duplicate-2x2 regeneration regression using the existing routine, plus paired-position continuity and empty-target single-handoff checks. Temporary LevelManager diagnostic fields were removed after diagnosis. Runtime results remain the responsibility of the current Unity test pass.
