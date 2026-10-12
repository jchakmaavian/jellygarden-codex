# Feedback 9: stuck gems and overlapping occupants

Current-build verification on 7 October 2026: **242 checks passed, 0 failed**. The reported stuck matched gem / incoming gem overlap was not reproduced. No gameplay code was changed for this verification.

## Coverage

At each of 15, 30 and 60 simulated FPS:

- Horizontal and vertical normal three-gem matches.
- Horizontal and vertical four-gem matches creating rockets.
- Square creating a Propeller, L creating TNT and five-in-a-row creating Color Ball.
- Two concurrent valid swaps in separate board regions.
- A continuous rapid sequence of 24 newly started swaps, without repainting the board between moves.

Total: 24 seeded scenarios plus three rapid sequences, **99 started swaps**. Of the 72 rapid-sequence swaps, **68 started while prior resolution was active** (23 at 15 FPS, 22 at 30 FPS, 23 at 60 FPS). This counts newly acquired swap operations, not successful match outcomes or attempted calls rejected before starting.

## What was observed

Across **2,309 rendered frames**, the suite enumerated all live Item objects under the board, rather than just the pieces reachable from cell records. This includes the orphaned gems that can cause the reported visible overlap.

- Every live piece retained one reciprocal cell owner throughout sampled motion.
- No refill occupied a cell reserved for special creation.
- Every expected matched source gem retired.
- No destruction remained stuck beyond the three-second observation threshold.
- Every scenario settled with exactly 64 live pieces in 64 cells and no remaining swap, falling, destruction or resolver state.
- Settled piece positions matched their owned cells. No two settled piece roots occupied the same cell-sized region.
- The automatic board-integrity repair count did not increase in any scenario. A repaired failure was not treated as a pass.
- Normal balance and lives were restored after the isolated QA session. The final session returned to Map with testing disabled. Final Unity console errors: zero.

The settled 60 FPS screenshot was reviewed visually. Expected transient gem overlap during a special-piece merge is intentional and is not counted as duplicate settled occupants.

## Status and limits

**Pass in the tested current Unity build; original QA incident not reproduced.** Existing swap/gravity ownership protections and special-cell reservations held under these tests. The original QA report did not identify a level, exact input sequence or device, so this does not establish reproduction-and-fix closure for that specific incident. A physical-phone check of the affected QA build remains outstanding.

The first run was repeated after tightening the rapid-swap counter to count an increase in active swaps, rather than merely observing another active swap. Both runs passed; `checks.txt` contains the final corrected run.

## Evidence

- `checks.txt`: authoritative final results.
- `settled-15.png`, `settled-30.png`, `settled-60.png`: captured settled boards.
- `Assets/JellyGarden/Scripts/Editor/RoyalOverlapChecks.cs`: reusable editor-only verification suite.

Run the suite only inside `RoyalQaService.StartSession()` / `EndSession()`, with moves-based Level 1 selected, via `RoyalRegressionRunner`. The suite restores capture timing in `finally`; the driver must restore the QA session even if verification fails.
