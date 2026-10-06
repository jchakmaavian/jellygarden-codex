# QA isolation and board settling repair — 2026-09-29

The QA controls suite failed on `Combine selected` in the starting workspace. The board also entered `RegenLevel` while a combo was still resolving. A separate no-move fixture reproduced the hint-scan race: it started a shuffle with a piece explicitly still falling.

## Changes

- `RoyalQaService` records whether it owns a player snapshot. An externally enabled QA route cannot be ended without that snapshot; rejection leaves writes isolated. Repeated starts keep the original snapshot. Concurrent teardown is ignored and new commands are rejected during teardown.
- Teardown restores runtime resources before switching save routes. It restores the exact captured UTC life deadline without calling the persistence-writing `SetRefillSeconds` hook. It does not postpone the player's refill deadline by the duration of a test.
- Practice fixtures release the preceding campaign board's fixed opening, refill feeds and palette. The selected QA campaign mode remains available for the next real level opening.
- The hint scan waits for resolution, power-ups, swaps and moving/destroying pieces before considering a no-move shuffle. The existing responsive input behavior remains intact.
- Settle timeouts still use 30 seconds, require three quiet frames, and now report the state and outstanding gates.

## Verification

- Unity 6000.5.10f1: compilation completed with zero errors.
- Full QA controls suite: **342 PASS**, `ALL QA CONTROLS PASSED`. Includes all 15 power-up combinations, hints, cascades, rewards, win/fail controls, nested start, unowned teardown, concurrent teardown and player-key checks after teardown. The one rejected duplicate win is an expected assertion, not a failed suite.
- The hint regression fails before the gate (`RegenLevel` while falling) and passes after it. The same test verifies that a legitimate no-move shuffle resumes once the piece stops falling.
- A low-life captured-snapshot test restores its exact nonzero UTC deadline and leaves both saved timer keys unchanged. The injected snapshot is only in memory; the player's actual runtime values are restored in the same frame, before subsequent Update callbacks.
- Original levels 33, 36 and 37 already passed nine seeded openings in the starting workspace. That workspace contains another session's `RegenMatches` recolouring fix. This task did not alter `LevelManager.cs`, remove that fix, or claim to have reproduced the earlier level-opening failure.
- Final level check: **18/18 openings passed**, covering levels 33, 36 and 37 with seeds 17, 29 and 41 in both original and reference campaign modes. Reopening the reference campaign after a practice fixture also passed.
- Whole-registry comparison: no non-QA gameplay keys changed, including lives, currencies, stars, unlocks and life timers. Only Unity's session identifier changed. Editor shutdown was performed with writes still routed to QA.

The full player registry snapshot is kept under ignored `Temp/QaRepair`, not committed. Evidence in this folder contains checks and test drivers, not player save exports. Existing unrelated source changes and the earlier QA report are preserved.

## Reproduce

Enter `game.unity` in Play Mode with the manual test profile off. Run **Tools > Royal Throne > QA > Verify every testing control** for the main suite. The `*.eval.cs.txt` files are additional Unity Pipeline eval drivers; copy one to a `.cs` file under `Temp/QaRepair` and run `eval_file` against this project. Run each driver only after the preceding suite has finished. Their output paths are inside `Temp/QaRepair`.

Validation covers the current combined workspace in the Unity Editor. It does not constitute an Android cold-start test.
