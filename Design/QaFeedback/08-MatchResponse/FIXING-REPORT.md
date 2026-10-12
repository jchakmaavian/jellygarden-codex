# Feedback 8 — Immediate normal three-gem match response

Implemented and verified on 7 October 2026 in Unity 6000.5.10f1.

Normal triples now start breaking on the next rendered frame after a successful swap completes. All three break together using the existing imported gem particles. Special-piece gem merges and original Reveals remain intact.

## Before and after

Actual player swaps were exercised through `Item.SwitchDirection`, horizontally and vertically, using fixed 30/60 FPS simulation steps. Timing is measured in rendered frames; screenshot encoding wall time is excluded.

| Simulation | Previous first break after swap commit | Updated first break | Previous first-to-last gem spread | Updated spread |
| --- | --- | --- | --- | --- |
| 30 FPS, horizontal and vertical | 9 frames | 1 frame | 1 frame | 0 frames |
| 60 FPS, horizontal and vertical | 15 frames | 1 frame | 2 frames | 0 frames |

The previous delay corresponds to roughly 0.25–0.30 seconds at those nominal rates; the revised response is within one frame (about 17–33 ms). These are controlled Unity measurements, not physical-phone benchmarks.

## Change

- Removed the fixed 0.1-second pre-scan wait. Resolution still waits for input swaps, falling pieces and pause state before inspecting the board.
- Normal three-gem groups no longer stagger or request hit-stop. Their break path skips anticipation, squash and rebound, starting the existing shatter effect immediately.
- Retained the short destruction-completion phase, imported particle artwork, score/collection code and existing refill path.
- Special merge and Reveal code was not edited. Removing the pre-scan wait permits an already-ready special match to begin sooner, while its merge and Reveal animation sequences remain unchanged.

## Verification

- **49 focused checks passed, 0 failed:** actual horizontal/vertical swaps at 30/60 FPS, next-frame break, simultaneous clearing, no destruction during swap ownership, visible painted break particles, exact three-gem score credit, one move consumed, source retirement, refill ownership and resolver completion.
- **145 special regression checks passed, 0 failed:** TNT L/T, both Rocket orientations, Color Ball and Propeller at 30/60 FPS, including full-size merges and original Reveal start/progress.
- Isolated sessions restored normal player balance and lives. Final state: Map, no QA runners, no dirty scene, no Console errors.

The initial timing probe was corrected to count rendered frames rather than screenshot-inflated wall time, and to observe the imported particle pool used by the current game. The saved baseline results below use those corrected measurements.

## Evidence

- [Before/after animation](before-after.gif)
- [Same-frame comparison](before-after.png)
- [Baseline checks](before/checks.txt)
- [Updated normal-match checks](after/checks.txt)
- [Special regression checks](special-regression/checks.txt)

The Excel tracker marks #8 fixed and adds the timing, implementation and verification report. Totals: 7 fixed, 1 clarified duplicate, 3 open (#9–#11). Physical-phone QA and an exact Royal Match reference comparison remain pending.
