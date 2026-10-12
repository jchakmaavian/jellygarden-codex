# QA #11 - Four-gem Propeller formation correction

Corrected 7 October 2026 after user review.

A matching 2x2 square plus one adjacent matching gem creates one Propeller, but **only the four square gems merge into it**. The fifth gem clears separately through ordinary destruction. The earlier five-gem merge was incorrect and is superseded by this revision.

[Watch the corrected four-gem merge](four-gem-merge/Square-plus-one-Propeller.mp4)

## Implementation

RoyalPatterns.AddSquares preserves the four-item square as its own special-creation group. Extra gems from overlapping ordinary line matches are placed in a separate clear group, with duplicate entries removed. The Propeller host is selected exclusively from the four square gems. Existing Rocket, TNT, Color Ball and already-selected Propeller priorities remain unchanged.

The existing full-size merge, native Propeller reveal and ordinary gem break animations are reused without retiming.

## Verification coverage

- All eight extra-gem positions tested through real player swaps at 15, 30 and 60 FPS, plus eight direct-resolver cases representing cascade-created patterns.
- Checks explicitly require exactly four gathering visuals, no gathering visual belonging to the extra gem, and ordinary destruction of the extra gem.
- One playable Propeller remains; all five source gems retire; unique cell ownership, input recovery and zero integrity repairs are checked.
- Additional overlap-priority checks and existing TNT-L/TNT-T, horizontal/vertical Rocket, Color Ball and plain-square Propeller formation suites are rerun.
- Normal profile restoration is checked at the end. Physical-phone QA remains pending.

Local evidence: four-gem-merge/checks.txt, formation-frames.jpg and the captured MP4. The clip uses 60 FPS simulation footage at normal speed with a one-second ending hold and no audio. Earlier five-gem-merge media in fixed/ is historical and does not represent the corrected behavior. No spreadsheet was edited in this step.

Final verification: **372 checks passed, zero failures**. All 32 square-plus-one cases passed the explicit four-gem-only merge and separate extra-gem clear checks. Normal balances and lives were restored. The review MP4 decoded successfully: 145 frames at 60 FPS, 720x1280.
