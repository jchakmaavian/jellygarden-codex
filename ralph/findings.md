# Outstanding findings — read this every iteration

These came from a human looking at playtest screenshots. The playtest harness
scored the same run **0 findings**, because it only detects functional stalls
(black screens, half-dead cascades, wins that never pop). It cannot see any of
this. Fold these into whatever story you are on; tick them off here when fixed.

Evidence: `~/Library/Application Support/DefaultCompany/King Leo/playtest/`
(`0001_level_1_start.png`, `0002_level_1_end.png`), from the run whose report is
`playtest.txt` — level 1, won, 3 moves, 0 errors, 0 findings.

## F-001 — the win popup is still the kit's skin  [OPEN]
The end-of-level popup is Jelly Garden's blue-and-yellow panel with the kit's
green Continue button. Nothing about it is King Leo. Every other shell screen was
reskinned; this one was missed. Deleting the kit component and putting the King
Leo one in its place is the pattern here — never leave the kit component alive
underneath.

## F-002 — no HUD is drawn over the board  [OPEN]
`0001_level_1_start.png` shows the board and nothing else: no move counter, no
score, no target chips, no pause. The report for the same frame says
"limit 6, 2 target(s), pass mark 100" and "targets: SCORE 0/100, BLOCKS 0/4", so
the data exists and is not on screen. A player cannot see how many moves are
left. Check whether `StoryLevelHud` built at all, and whether it is behind the
board's canvas rather than over it.

## F-003 — the room behind the board is a dark blur  [OPEN]
Both shots show a near-black, heavily blurred brown field behind the board. The
project's one visual rule is bright, cartoon-like colour for the castle and
everything else. This reads as an out-of-focus photograph. Either the blur radius
or the darkening tint behind the board is far too strong, or the wrong art is
loading. Check the whole set of arcs, not just Castle Gates.

## F-004 — the first screenshot lands under the tutorial overlay  [OPEN]
On level 1 the start shot is taken while the kit tutorial dims the screen and
draws a pointing hand over the board. Any level with a tutorial or an intro beat
will screenshot the overlay instead of the board, which makes the shot useless
for visual review. Take the start shot after any overlay has cleared, or take a
third shot once play has actually begun.

## F-005 — visual review is not automated and must not be claimed as done  [OPEN]
A band is not finished when `playtest_sweep.py` prints zero findings. The
screenshots have to be opened and looked at. Do not mark a band story `passes:
true` on the strength of the report alone.

## Uncertain — check before acting
- The end popup says "Already beaten" with a dull star and "Castle Gates 0/3".
  The save already had progress, so this may be correct — "0/3" is probably the
  area's repair count, not stars. Confirm what that row means before changing it;
  if it is ambiguous to a player, that is worth fixing on its own.

---

## Iteration note — US-007 (level linter), 2026-09-07

None of F-001..F-005 is ticked off. All five are about what a screenshot shows —
the win popup's skin, the missing HUD, the dark blurred room, shots taken under
the tutorial overlay, and the rule that a report is not a verdict. US-007 is a
static data story: it reads level files and target assets and never renders a
frame, so it cannot see or fix any of them. **F-001 through F-005 stay OPEN.**

What US-007 did change that a later visual story should expect: 23 levels now
draw a real blocks chip in the HUD instead of one that was ticked before the
first move, and 2 levels (15, 105) lost a dead ingredient chip. If F-002 is
fixed and the HUD appears, those chips are the ones to look at first.
