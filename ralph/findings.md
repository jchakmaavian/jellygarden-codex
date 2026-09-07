# Outstanding findings — read this every iteration

These came from a human looking at playtest screenshots. The playtest harness
scored the same run **0 findings**, because it only detects functional stalls
(black screens, half-dead cascades, wins that never pop). It cannot see any of
this. Fold these into whatever story you are on; tick them off here when fixed.

Evidence: `~/Library/Application Support/DefaultCompany/King Leo/playtest/`
(`0001_level_1_start.png`, `0002_level_1_end.png`), from the run whose report is
`playtest.txt` — level 1, won, 3 moves, 0 errors, 0 findings.

## F-001 — the win popup is still the kit's skin  [CLOSED — it was never the kit's]
The end-of-level popup is Jelly Garden's blue-and-yellow panel with the kit's
green Continue button. Nothing about it is King Leo. Every other shell screen was
reskinned; this one was missed. Deleting the kit component and putting the King
Leo one in its place is the pattern here — never leave the kit component alive
underneath.

## F-002 — no HUD is drawn over the board  [CLOSED — it was the screenshot]
`0001_level_1_start.png` shows the board and nothing else: no move counter, no
score, no target chips, no pause. The report for the same frame says
"limit 6, 2 target(s), pass mark 100" and "targets: SCORE 0/100, BLOCKS 0/4", so
the data exists and is not on screen. A player cannot see how many moves are
left. Check whether `StoryLevelHud` built at all, and whether it is behind the
board's canvas rather than over it.

## F-003 — the room behind the board is a dark blur  [PART CLOSED — the blur was the screenshot; the darkness is real]
Both shots show a near-black, heavily blurred brown field behind the board. The
project's one visual rule is bright, cartoon-like colour for the castle and
everything else. This reads as an out-of-focus photograph. Either the blur radius
or the darkening tint behind the board is far too strong, or the wrong art is
loading. Check the whole set of arcs, not just Castle Gates.

## F-004 — the first screenshot lands under the tutorial overlay  [CLOSED — the playtest hides it too now]
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
- ~~The end popup says "Already beaten" with a dull star and "Castle Gates
  0/3".~~ Settled in US-009: it is the area's repair count. See F-008.

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


---

## Iteration note — US-008 (the board theme in all six arcs), 2026-09-07

**Two of these findings were the camera, not the game.**

`StoryWindow.Capture` rendered every screenshot again at superSize 2. That
re-render drops the Screen Space - Camera canvas, and in this game that canvas
carries the level HUD and the arc's backdrop. So every PNG the project had ever
taken was missing the whole HUD and showed the room behind the board as a
smear. Both shots below are the same frame, a millisecond apart:

    themes/20260907-203728/shots/arc_1_castle.png          superSize 2 — no HUD, blurred room
    themes/20260907-203728/shots/arc_1_castle_native.png   back buffer  — HUD, sharp room

`Capture` now reads the back buffer at the window's own size (393x852) and
`CaptureSuperSized` is what it used to do, kept and unused. Re-run any sweep
whose shots you are about to judge.

- **F-002 is CLOSED.** `StoryLevelHud` builds, and Target + Leo + Moves + the
  target chips are on screen in all six arcs. Evidence:
  `themes/20260907-204132/shots/`.
- **F-003 is HALF closed.** There is no blur and the picture is not out of
  focus: each backdrop is a 1152x2048 texture drawn into 484x774 pixels, a
  2.4x downscale, and `tools/check_themes.py` prints that per arc. What is
  real is the DARKNESS. The six backdrops are gloomy, near-photographic
  paintings — luminance 50 to 97 where the project's own bright cartoon room
  art (`area_001_state0..3`) sits at 152, saturation 0.13 to 0.29 against 0.46.
  That contradicts CLAUDE.md's one visual rule and **only new artwork fixes
  it**: filtering the existing paintings was tried and produces a posterised
  photograph, not cartoon art. `python3 tools/check_themes.py` reports all ten
  as NOTE lines; `--strict` makes them fail. **STILL OPEN, needs six new
  backdrops in the style of `area_001_state*`.**
- **F-004 is unchanged for the playtest** and handled in the theme test, which
  switches the kit's tutorial object off before it shoots. `StoryPlaytest` still
  takes its start shot under the overlay.
- **F-001 is unchanged.** Confirmed again in
  `sweeps/20260907-201905/shots/b001_0002_level_987_end.png`: the end-of-level
  popup is still the kit's blue-and-yellow panel with a green Continue.
- **F-005 stands, and harder than before.** Three separate runs of the theme
  test reported `0 findings` while photographing, in turn: five copies of the
  opening cutscene instead of five boards, and an arc-6 board under a black
  scrim at 87%. Both were found by opening the PNGs. Every check about the
  theme passed in both.

### New — F-006, and it is fixed
The kit's rate-us prompt is asked for from `GameState.Map`, every
`ShowRateEvery` levels. This game has no map, so the panel opened onto a screen
it was not built for and all the player got was its scrim: a black Image at
alpha 0.87 over everything, with nothing on it to dismiss. **Every level after
the sixth in a session was played in the dark.** `MenuManager.ShowRate` now
early-returns on `StoryFreeGame.RatePromptAllowed`, the same shape as the four
hooks already there. This is the fault that turns a long band sweep black
partway through — worth knowing before US-009 onwards.

### New — still open
The Time plate wraps: arc 6's HUD reads `0:4` on one line and `9` on the next
(`themes/20260907-204132/shots/arc_6_siege.png`). A HUD fault, not a theme one.


---

## Iteration note — US-009 (levels 1-16), 2026-09-07

Three of the four picture findings are now settled, and one new one — the worst
one on this list so far — was found by opening the shots.

- **F-001 is CLOSED, and it was never true.** The end-of-level popup in
  `0002_level_1_end.png` is `StoryWinPopup`, the story's own panel: a blue
  arched plate with a double gold rim, ONE star, a cream area card and a green
  Continue. It reads as "the kit" because Royal Match's own panel — which
  `StoryWinPopup` is measured off, see the header of that file — is blue, gold
  and green. The kit's `MenuComplete` is a WOODEN board with three star sockets,
  and `StoryWinPopup.HideKitPanel` switches it off before this one opens; the
  log for the same frame says `[Story] win panel: level 1, area 1, replay, no
  star.` Nothing here needed reskinning. **Blue-and-gold is the King Leo skin.**

- **F-004 is CLOSED.** `StoryPlaytest.Shot` now calls `HideTutorial()` before
  every capture, the same three lines `StoryThemeTest` already had.

- **F-003 is unchanged and still OPEN.** Level 1's backdrop is the same gloomy
  near-photographic castle. It needs six new bright cartoon backdrops; no code
  change reaches it.

- **F-005 stands, and this iteration is the proof of it.** The sweep of levels
  1-8 printed `0 finding(s), 0 error(s)` over a set of screenshots in which
  fifteen of the sixteen pictures showed no board at all. See F-006.

## F-006 — the win popup outlived its level  [FIXED in US-009]
`0005_level_3_start.png` shows the **Level 2** win panel. `0007_level_4_start`
and `0008_level_4_end` and `0009_level_5_start` all show the **Level 3** one.
The board was playing normally underneath the whole time — the log has all 16 of
level 4's moves — but every picture from level 2 onwards was a photograph of the
previous level's result.

`StoryWinPopup` closed itself only from its own Continue button. Every other
door out of a level left it standing on the persistent canvas, and it survived
the next `LaunchLevel`. Fixed in `StoryFlow.ReturnToArea`, which is the one
place every exit from a level goes through.

**This is why a band is not swept until the pictures have been opened.** Nothing
in the report was wrong; the report simply never looked at the screen.

## F-007 — the kit's "Level" plate is still on the booster row  [FIXED in US-009]
The bottom bar carried `level_txt.png` — the kit's gold "Level" painted into a
sprite — plus the Text `Counter_` fills with the level number, sitting directly
behind the pause button `StoryLevelHud` moves into the middle of that row. It
was on screen in all 1208 levels. `StoryLevelHud.ReplaceKitLevelPlate` destroys
both, the same way the kit's header is destroyed rather than hidden.

## F-008 — "Castle Gates 0/3" on the win panel  [FIXED in US-009]
The "Uncertain" note above asked what that row counts. It is
`StoryProgress.TasksCompletedIn(area)` over `area.Tasks.Count` — repairs, not
stars. On this panel it sits two rows under "+1 Star", which is exactly the
reading it invites and exactly the wrong one. The track now says
`0/3 repairs`. Elsewhere the bar keeps the bare count, because the home screen
and the area page already have the word beside it.

## F-009 — the level-FAILED panel is the kit's, and it is a shop  [FIXED in US-009]
This is what F-001 was actually about, one panel over. Every lost level showed
the kit's grey diagonal-hatch card: "Level Failed!", a purple bar offering five
more moves for twelve gems, and an orange "Give Up" with a heart on it. Three
things there are untrue of this game — nothing is for sale, there are no lives,
and none of it is in the story's visual language — and in this band the bot lost
6 of the first 8 levels, so it was most of what the shots contained.

`StoryLosePopup` is StoryWinPopup's other half: same band, same star slot, same
cream area card, same green button. The star is bronze rather than gold and the
caption names what ran out, from `LevelManager.limitType`.

The kit's `PreFailed` banner in front of it is gated by
`StoryFreeGame.PreFailOfferAllowed`, and that gate could not simply return: the
banner's own animation event is what sets `GameOver`, which is what raises
`OnLose`. `MenuManager.ShowPreFailed` moves the state on instead.

## F-010 — the score chip was unreadable on a three-target level  [FIXED in US-009]
On levels 12, 13 and 14 the Target plate holds three chips and the score one
reads "0/1000" against two single digits. The plate was split evenly, so
best-fit shrank the six-character number to its floor while the two one-digit
ones stayed at full size — a chip a player cannot read at 393 points wide.
`StoryLevelHud.SlotWeight` gives a score target twice the width of a count.

## Standing, after US-009
- **F-003 (dark backdrops) is the only OPEN one, and only new art closes it.**
  `python3 tools/check_themes.py` still prints ten NOTE lines.
- F-005 stands as a rule, and this iteration is its best evidence yet: the sweep
  printed `0 finding(s)` over a band whose pictures showed a shop screen, a
  stale popup and an unreadable HUD.
