# QA #11 - Square plus one verification

Verified 7 October 2026. **Bug reproduced. No gameplay changes made.**

## Current behavior

A 2x2 square with one matching gem directly beside it fails to create a Propeller. The overlapping three-gem line resolves as an ordinary match instead. In the reviewed left-extension example, the other two red square gems remain on the board after resolution.

## Runtime evidence

- 24 actual player-swap cases: all eight edge-adjacent positions around a central 2x2 square, at 15, 30 and 60 FPS.
- **All 24 cases created zero Propellers**, did not retire all five source gems, and did not run the special merge animation.
- Each test started a real swap, sampled the actual resolver through settling, and monitored live cell ownership.
- Ownership and input recovery passed for all cases; no integrity repair was needed.
- Result: 73 passing checks and 72 expected-behavior failures, grouped as three failed expectations per case.
- Control: plain 2x2 with no extra gem, using the same swap setup at 60 FPS, created exactly one playable Propeller, merged correctly and retired all four source gems. Seven control checks passed.
- Both isolated QA sessions restored normal balances and lives. Unity reported no console errors. No production code or scene changes were made.

## Cause

CombineManager.GetCombine collects horizontal/vertical line groups before calling RoyalPatterns.AddSquares. AddSquares marks all line-group items as used, then rejects any square containing a used item (Assets/JellyGarden/Scripts/Juice/RoyalPatterns.cs, lines 42-46).

Every five-gem shape tested includes both a valid 2x2 square and an overlapping three-gem line. The line claims shared gems first, suppressing Propeller creation.

## Status

QA #11 remains **open, verified and reproducible**. A future fix should reconcile the overlapping line and square into the intended Propeller-producing match, preserving higher-priority special-match rules and resolving all five source gems cleanly. No fix is applied by this verification.

Local evidence: checks.txt, case-0 through case-7 before/after screenshots, and control/checks.txt. Test drivers are Temp/verify_qa11.cs and Temp/verify_qa11_control.cs. Physical-device reproduction was not part of this check.
