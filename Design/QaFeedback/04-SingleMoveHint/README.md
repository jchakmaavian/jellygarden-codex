# QA feedback 4: one clear move suggestion

Implemented and verified on 7 October 2026 in the independent Royal Throne project.

## Reproduction

The move finder already selected one exchange. A swap that produced two different-colour matches combined both results into its visual outline, making the marking look like multiple suggestions. The isolated regression board swaps (3,3) with (3,4), producing two three-gem lines. Before the fix, all six destination cells were outlined together. `before.png` and `before.txt` record the reproduction.

## Change

`RoyalHintPlan.MatchCells` continues to describe every actual result, preserving existing move ranking and gameplay. A separate `PreviewCells` list selects one connected, same-colour matching group after the virtual swap. The strongest pattern receives priority: Color Ball, TNT, Rocket, Propeller, then a normal match. Equal choices use stable board order, including when exchange partners are supplied in reverse order.

The presenter outlines only that group. Its gems and both exchange partners retain feedback 3's brighter glow, 1.0x–1.06x pulse and protected spacing; only the two exchange partners move. Secondary results are not also outlined or pulsed. A presenter handoff hides the previous presenter before showing a new hint.

## Verification

Unity 6000.5.10f1 compiled the runtime and editor code without errors. The focused suite passed **186 checks with zero failures**. It covers:

- Horizontal and vertical 3+3 and 3+4 results, plus a 3+5 result.
- Full match results against the production resolver, while the visible preview contains one group.
- Stable preview selection with reversed swap arguments and stronger-result priority.
- Several independent legal swaps, three concurrent idle finders, stable selection, and presenter handoff.
- Feedback 3 regression coverage: five gem designs, six match shapes, 6% pulse, glow, no overlap, unchanged roots/colliders/cell ownership, cancellation, preferences and reduced motion.
- Isolated QA routing and restoration of normal balance and lives. The test runner retired and the scene remained clean.

The first run exposed a test-fixture omission: the multiple-choice board only contained the two-result exchange. An independent alternative legal swap was added before the final successful run. Production behavior did not change for that correction.

`raw/single-group-after.png` records the corrected native Game view. `before-after.png` compares the same board region in the two captures. `checks.txt` retains all final assertions locally. No Android rebuild or physical-phone test was performed. Other original QA items remain separate.
