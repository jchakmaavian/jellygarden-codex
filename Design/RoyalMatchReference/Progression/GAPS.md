# Progression gaps: reference vs Royal Throne

Priorities:
- **P1:** correctness or persistence (can lose or duplicate progress or currency).
- **P2:** a missing link in the chain.
- **P3:** fidelity or polish.

Statuses: open, fixed (iteration and commit), decided, blocked (with reason).

## User decisions (2026-09-29)
- **D1: one star per win**, as in the reference. This replaces the 1-3-by-score rule on levels 6+. The coin rule
  (10 + 5 x stars) is unchanged, so it now pays 15 per win.
- **D2: area star totals follow the reference**: 10, 10, 20, 20, 20, 20, 20, 20, 20, 20 for Areas 1-10. Our own task
  names and art stay.
- **D3: Areas 2+ progress** with clearly marked placeholder scenes built from our own art kit, until real art exists.
- **D4:** DX11 is handed over for this loop.

## Open
| ID | P | Gap | Reference | Ours | Plan | Status |
| --- | --- | --- | --- | --- | --- | --- |
| G01 | P1 | A booster-shop purchase overwrites the stock | A purchase adds | `InitScript.BuyBoost` sets the stock to the pack count | The shop now passes stock + pack (the other callers already pass totals) | **fixed**: it1, `0849dc6b` |
| G02 | P1 | Stars per win (D1) | Always 1 | 1-3 by score on L6+; 0 possible | Every win saves exactly 1 star; handle saves that hold 2-3 (migration) | **fixed**: it2 (the G02/G04 commit). Every win saves 1 star; the score adds none; Home counts one star per won level, so older 2-3 saves count once; `RoyalHomeScreenChecks` updated to the rule |
| G03 | P1 | A zero-star win bypasses the lock | n/a (always 1 star) | 0 stars paid, next locked, CONTINUE opens it | Fixed by G02 | **fixed** by G02: every win is 1 star; a win at 0 moves below star1 now wins instead of hanging |
| G04 | P1 | Killing the app during the finale loses the win | Saved by the time the card shows (a kill on the Well Done card kept +16 coins, +1 star and the next level); the reference also restores a level in progress (G14) | Everything is lost if killed before the `Win` state | Commit the win's rewards (stars, coins, unlock, life) when the goals complete, before the celebration; the finale stays presentation | **fixed**: it2 (the G02/G04 commit). `LevelManager.CommitWin` at PreWinAnimations; a stop mid-finale kept the star and the unlock (T2/T3, level 35) |
| G05 | P2 | The level 6+ win skips Home | ? (observe L6: likely Continue → Home, with star and coin flights) | CONTINUE opens the next card; no star flight; the task list can't auto-open | Observe L6 | open |
| G06 | P2 | Area totals (D2) | 10, 10, 20… | 10, 12, 15, 18, 19, 22, 25, 26, 29, 32 | Re-cost `areas.json` to the reference totals, keeping our tasks | open |
| G07 | P2 | Areas 2+ blocked (D3) | Always continues | "Coming Soon" after Area 1 | Placeholder scenes; the Unlock flow for every area | open |
| G08 | P2 | Fail flow fidelity | "Continue?": +5 moves for **900 coins** (escalation ?) → X → one card "Level Failed!" with the goal ticks and crosses, the boosters and Try Again → X → Home, lives −1 | "SO CLOSE!": +5 moves for 12 coins, repeatable at the same price → END LEVEL → fail card → Again → the level card | Make the fail card the retry card (the goal status, boosters and Try Again on one card); the price is part of G12 | open |
| G14 | P2 | A level in progress survives a kill | Restored exactly (board, moves, the pending Continue? offer) | Lost; the life stays spent | Serialize the level session (a large change to the board engine); decide scope | open |
| G15 | P2 | Lives refill time | 30 min per life (5 max) | 5 min per life | Report; part of the economy (G12) | open |
| G16 | P3 | Pre-level boosters unlock | At L7 on the level card, 3 of each free (Rocket, TNT, Light Ball); in-level tools still padlocked at L7 | In-level tools unlock from L6; pre-level boosters ? | Observe the tool unlock level; compare | open |
| G17 | P3 | Task list after a relaunch | Did not open by itself on a relaunch after a win; the Area badge "1" shows when the next task is affordable | ? | Check ours | open |
| G09 | P2 | Story | Level-based episodes (not yet reached) | None | Observe where it unlocks; plan | open |
| G10 | P3 | `CloseMenu` sets Map twice | n/a | `EnableMap`/`OnMapState` run twice | Check for side effects; set it once | open |
| G11 | P3 | The debug Win key pays again | n/a | Dev builds only | Guard: pay once per attempt | **fixed** by G04: `CommitWin` pays once per attempt (T1: a forced second entry paid nothing) |
| G12 | P3 | Economy scale | 2000 starting coins; L1-5 rewards 13-64 | 5 starting coins; 15 per win; continue 12 | Report only (the user's coin rule stands) | decided: report |
| G13 | P3 | Unused duplicate `Resources/RoyalCampaign/levels01-50.json` ships | n/a | Byte-identical duplicate | Report (another session's file) | open |

## Found during testing (not fixed; the loop stopped)
- **QA harness: board settle.** `RoyalQaService.Settled` timed out ("Board did not settle within 30 seconds") while
  levels 33, 36 and 37 were opening (before any win code runs), and in the QA controls suite on "Combine selected"
  (107 PASS, then that failure). The last clean QA controls run was 2026-09-27 20:19. Not attributed: finding the cause
  needs an A/B that reverts other sessions' work, which was not allowed.
- **QA harness: `EndSession` after a nested `StartSession`.** `StartSession` returns early when the QA profile is already on,
  but `EndSession` still switches it off and restores normal values it never captured. That leaked `Lifes=1` into the
  real save during T1 (restored from the export taken before it).

