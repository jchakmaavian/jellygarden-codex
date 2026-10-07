# Feedback 6 — Four gems merge into one Propeller

Corrected on 7 October 2026 following the user's clarification: the four gems must move together at their original size, without shrinking, breaking or a Propeller popping into place.

The accepted 2x2 match now uses a 0.24-second inward slide. All four gem visuals retain their original size and overlap at the existing selected creation cell. Once together, the merged silhouette crossfades over 0.14 seconds into the full-size native Propeller artwork. The resulting gameplay item starts in its settled visual state, bypassing the old pop-style creation clip. This is a visual crossfade between existing artwork, not a newly authored mesh morph.

`RoyalPropellerFormation` owns temporary visuals, parented to the source items for cleanup. Source roots and cell references do not move during the presentation. The existing destruction path still owns score/collection, host reservation, output generation and refill. Only four-item groups assigned a Propeller use this path; other special creation and square-plus-one detection remain separate QA items.

## Verification

`RoyalPropellerFormationChecks` plus isolated QA cleanup: **25 checks passed, 0 failed**, Unity 6000.5.10f1.

- 30 and 60 FPS: all four gem visuals retained their original bounds throughout movement and merge.
- Transformation started only after all four reached the same position.
- The merged shape blended into the Propeller without replaying the creation pop animation.
- Pause held movement; source roots remained fixed; original gem renderers were hidden.
- Four original items retired, exactly one Propeller was created, and temporary gem visuals were removed.
- Refill retained item/cell ownership. The isolated session restored normal player balance and lives and returned to Map with no QA runners or dirty scene.

## Evidence

- [Animation preview](propeller-merge.gif)
- [Merge sequence](merge-sequence.png)
- [Check results](checks.txt)

The preview uses actual Unity frames from the verified 60 FPS sequence. Physical-phone QA remains pending. Excel #6 is checked, with an additional fixing report; totals are 5 fixed, 1 clarified duplicate and 5 open (#7–#11).
