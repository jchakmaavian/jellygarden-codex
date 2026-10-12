# Feedback 7 — TNT, Rocket and Color Ball creation

**Superseded presentation:** the user subsequently requested the original Reveals back while retaining gem merging. See [the current restoration report](reveals-restored/FIXING-REPORT.md). The crossfade/no-Reveal description below records the earlier implementation.

Implemented in the requested order on 7 October 2026: TNT first, both Rocket orientations second, Color Ball third. Each stage was tested before proceeding to the next.

Matched gems keep their original size as they slide into the existing selected creation cell over 0.24 seconds. Once together, their merged silhouette crossfades into one full-size special piece over 0.14 seconds. There is no gem shrinking, fragment burst, empty interval or replay of the pop-style creation clip. This uses a crossfade between existing sprites, not a new mesh morph.

## Implementation

- Generalized the existing Propeller sequence as `RoyalSpecialFormation`, preserving its script GUID and the verified Propeller behavior.
- TNT accepts the resolver's T/L groups, including a repeated intersection reference, without choosing a new host or changing match detection.
- Rocket previews enable the same horizontal/vertical branch as the resulting gameplay item.
- Color Ball blends into its native idle appearance.
- The merge flag is consumed once. Later conversions retain their normal reveal behavior; native creation audio plays once when the result is handed to the gameplay item.
- The existing resolver retains scoring, collection, host reservation, item generation and refill. This change does not adjust normal-match timing, Color Ball activation duration or square-plus-one detection.

## Verification

Final combined Unity 6000.5.10f1 regression: **145 passed, 0 failed**. The suite exercised TNT L/T, horizontal/vertical Rocket, Color Ball and Propeller at 30 and 60 FPS.

Checks covered constant gem size, all matched gems moving together, convergence before the blend, no replayed pop reveal, pause/resume, fixed original item roots, source retirement, exactly one resulting special item, visual cleanup and refill cell ownership. The new special cases also checked that the one-use merge flag cleared on handoff.

Captured Unity frames were visually reviewed across movement, blend and completed shape for all five new cases. One intermediate Rocket recording was obscured by the startup loading screen and was discarded; the final driver waits for that screen to finish before starting the isolated session.

The final run restored normal player balance and lives and returned to Map with no QA runners and no dirty scene. Unity Console reported no errors. Physical-phone QA and an exact side-by-side Royal Match comparison remain unverified.

## Review media

- [TNT L merge](TNT-L/merge.gif) and [TNT T merge](TNT-T/merge.gif)
- [Horizontal Rocket merge](Rocket-H/merge.gif) and [Vertical Rocket merge](Rocket-V/merge.gif)
- [Color Ball merge](ColorBall/merge.gif)
- [All creation sequences](merge-sequences.png)
- [Final check results](checks.txt)

The GIFs use every second frame from the final 60 FPS Unity captures. The Excel tracker marks #7 fixed and adds detailed rows to its Fixing Report. Totals: 6 fixed, 1 clarified duplicate, 4 open (#8–#11). The workbook and review media remain local under the repository's existing ignore rules.
