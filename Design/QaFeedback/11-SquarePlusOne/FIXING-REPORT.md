# QA #11 - Square plus one Propeller fix

Implemented 7 October 2026.

A matching 2x2 square with one additional gem beside it now creates one Propeller. The overlapping ordinary line and square become one match group, so all five source gems merge using the existing full-size movement and native Propeller reveal. The extra gem no longer suppresses the square.

[Watch the verified formation](fixed/Square-plus-one-Propeller.mp4)

## Match rules

RoyalPatterns.AddSquares now absorbs overlapping ordinary line groups, with each gem included once. An overlapping group already assigned a Rocket, TNT, Color Ball or earlier Propeller is preserved. Target selection and the shared special-creation animation are otherwise unchanged.

## Verification

The original 24 player-swap cases were rerun across all eight extra-gem positions at 15, 30 and 60 FPS. Eight additional direct-resolver cases cover the same final patterns as cascade matches at 60 FPS. Each case checks one playable Propeller, retirement of all five source items, merge presentation, reciprocal unique cell ownership, no integrity repair and input recovery after settling.

Additional checks cover overlapping square/long-line and square/T-shape priority, plus existing TNT-L, TNT-T, horizontal/vertical Rocket, Color Ball and plain-square Propeller formation suites.

The 60 FPS review clip was decoded successfully (180 frames, 720x1280); swap, full-size merge, reveal and settled Propeller were visually inspected. The clip contains two seconds of live capture and a one-second ending hold, without audio. Physical-phone QA remains pending.

Detailed local results: fixed/checks.txt. The pre-fix report and evidence remain in VERIFICATION-REPORT.md and checks.txt. A reusable isolated-session regression check is in Assets/JellyGarden/Scripts/Editor/RoyalSquarePlusOneChecks.cs. No spreadsheet was edited in this step.

Final result: **340 checks passed, zero failures**. Normal balances and lives were restored after the isolated session.
