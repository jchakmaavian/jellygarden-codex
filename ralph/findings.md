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


---

## Iteration note — US-010 (levels 17-32), 2026-09-07

Three faults, all found by opening the pictures or by the harness disagreeing
with the game. Evidence: `sweeps/20260907-223332` and `20260907-224051` (before),
`20260907-230313` and `20260907-233909` (after).

## F-011 — the Time plate wrapped, on every timed level  [FIXED in US-010]
The Moves card is sized for "19" and a TIME level puts "0:59" in it, so Unity
broke the line and drew "0:5" over "9" — two lines of a four-character clock, in
the one number a timed level is played against. US-008 had already seen it on
arc 6 and left it as "a HUD fault, not a theme one"; levels 20, 27 and 32 are
where it showed here. `StoryUi.FitOneLine` already existed for exactly this and
the plate simply never called it.

**The target chips keep wrapping ON, and that is not an oversight.** The same
change applied to a target slot made it worse, not better: a slot is a third of
a plate 393 points wide, and with wrapping off best-fit stops shrinking and the
number runs into its neighbour — level 20's chips read `25 350/1000` instead of
`25 35 0/1000`. Wide box: turn wrapping off. Narrow box beside other boxes:
leave it on. Both are in `StoryLevelHud` with the reason written down.

## F-012 — the blocks target chip looked like an empty box  [FIXED in US-010]
`Textures/Blocks/block.png` is the icon on all 40 shipped BLOCKS levels and every
generated one. It is a pale ice tile — 0.77 mean luminance — drawn on
`RoyalCream` at 0.92, so it separated from the card it sits on by 0.15 and read
as a blank square with a number beside it. On the board it works because it
frames a coloured gem; alone in a chip it does not. Levels 18, 19, 25, 28, 29
and 31 all showed it.

Fixed without touching the kit's art: every chip icon now casts a silhouette of
its own sprite one step down and right, in navy at 0.55. A pale icon gains an
edge; the dark ones gain the depth the reference panel has.

## F-013 — a dead board that the kit never reshuffled  [FIXED in US-010]
Level 31, with three moves left and twenty-one ice tiles still to break, reached
a state where no swap on the board could match — and the kit sat there. Twelve
seconds, no "no more matches", no regeneration. A player who reaches that has no
button to press: the level cannot be won and cannot be lost.

The kit's recovery is `AI.CheckPossibleCombines` → `LevelManager.NoMatches()`.
That scan is a heuristic list of L, T and line shapes, one colour at a time; the
coroutine that runs it is started from six places, never stopped, gates both its
work AND its own re-arming on `DragBlocked`, and shares one `gotTip` field
between however many copies are in flight. Pinning which of those lost the race
would have cost more than covering all of them.

`StoryDeadBoard` is the cover. Once a second, on an idle board, it asks whether
ANY neighbouring swap would score — using the kit's own `Square.FindMatchesAround`,
which is what judges a real swap — and after three such answers in a row it calls
the kit's own `NoMatches()`. Nothing is replaced: the kit's AI still runs, still
hints, and still reshuffles whenever it gets there first.

The harness had to learn the same word. `StoryPlaytest` treated any state but
Playing as an ending, so the first reshuffle it ever saw came back as "ended in
state Playing, which is neither won nor lost". It waits `RegenLevel` out now, and
a reshuffle that never ends is its own finding.

## Standing, after US-010
- **F-003 (dark backdrops) is still the only OPEN visual one, and only new art
  closes it.** `check_themes.py` still prints ten NOTE lines. Level 24's two
  `00` holes read as black rectangles cut out of the board for the same reason:
  an empty cell shows the backdrop, and the backdrop is nearly black.
- F-005 stands. Both batches of this band printed `0 finding(s)` over the
  pictures that carried F-011 and F-012.
- **New, OPEN, and not a US-010 fault:** level 23 asks for 20 ingredients in 19
  moves. Ingredients spawn at `Random.Range(0, Limit) == 0` per generated gem
  (`Item.GenItem`), so at Limit 19 a move's three-to-six new gems yield about a
  fifth of an ingredient — call it five in a level, against a goal of twenty —
  and the bottom rows are wooden blocks the ingredient has to fall through to
  reach its exit. Across every sweep on record, levels 22, 23, 24 and 26
  collected zero ingredients between them; only levels 14 and 15 ever collected
  any. Level 23 is a SHIPPED kit level, so this is the kit's own number, and
  proving it needs the spawn instrumented rather than argued. It belongs in the
  linter (`tools/lint_levels.py`), which is where a goal that cannot be reached
  is supposed to be caught.


---

## Iteration note — US-011 (levels 33-48), 2026-09-08

The band swept `0 finding(s), 0 error(s)` in all four batches, twice — before
and after the fix. Everything below came out of opening the pictures.
Evidence: `sweeps/20260908-000414`, `-000729`, `-001254`, `-001740` (before);
`-003048`, `-003456`, `-004002`, `-004503` (after).

## F-014 — the blocks target chip was an empty frosted square  [FIXED in US-011]
F-012 fixed the chip's *contrast* by giving every icon a silhouette. It did not
fix what the chip is a picture of, and levels 33, 35, 37, 38, 40, 41, 45, 46
and 48 all showed the result: a pale square with a number beside it, carrying
no information about what the level is asking for.

`Textures/Blocks/block.png` is not a picture of a block. Its centre is **55%
opaque** (alpha 141/255, measured) — it is frost, and `CreateSquare` parents it
to a square that still holds an item, at z -0.01 in front of it. On the board
it is therefore always a gem seen through ice. Alone on `RoyalCream` it
composites to almost the card colour.

`StoryLevelHud.HudTarget.Backing` puts the kit's own first item sprite behind
it, so the chip is the same picture the board draws. Which gem is measured, not
chosen: composited under the block on cream, item_01 separates from the card by
0.274 of luminance against 0.113 for the bare block and 0.145..0.201 for the
other five. On screen at 393 points that came out 0.288 against 0.175, off
level 33's own PNG before and after.

It cannot be misread as a collect chip: across all 1208 target assets, **not
one of the 508 levels carrying a BLOCKS chip carries a bare-gem chip beside
it** — their other chip is always the star. That count is also the size of the
fix: 42% of the game draws this chip.

## Uncertain — measured, not acted on
**A cell walled in by holes can never be matched.** Level 45's board is a
picture frame with one lone cell in the middle of it, four `00` holes around
it, and it is one of the 62 blocks the level asks for. No swap can ever include
it; only a stripped or a package reaching row 4 or column 4 clears it.

Counted over all 1208 boards, treating only `00` (NONE) and `x6`
(UNDESTROYABLE) as permanent walls — SOLIDBLOCK and THRIVING both go when a
match lands beside them, `Square.DestroyBlock` cascades into them — **24 levels
have a walled-in region of fewer than 3 cells: 45 and 97 of the kit's own 100,
and 22 generated ones** (111, 112, 122, 126, 143, 151, 277, 287, 307, 431, 546,
553, 567, 578, 583, 584, 598, 599, 617, 630, 654, 675 and two more).

Not acted on this iteration, deliberately: level 45 is the kit's shipped board
byte for byte, the cell is reachable with a booster, and the level ends
normally. It is a linter rule waiting for a decision about whether the
generated 22 should inherit shapes that contain one. **Do not hand-edit a level
file to fix it** — `tools/level_gen.py` would undo it on the next run.

## Standing, after US-011
- **F-003 (dark backdrops) is still the only OPEN visual one, and only new art
  closes it.** `check_themes.py` prints the same ten NOTE lines. Level 39's one
  hole and level 34's two read as black rectangles cut out of the board for
  that reason and no other.
- F-005 stands. Four batches printed `0 finding(s)` over the pictures that
  carried F-014.
- The ingredient shortfall first written down in US-010 shows here too: levels
  39 (2+2) and 42 (2+2) collected 0 and 1 across 40 and 42 moves. Still the
  linter's problem, still unproven without instrumenting `Item.GenItem`.
- **Two Unity batchmode gates are RED and were red before this iteration.**
  `StorySmokeTest.RunAll` fails on `expected 807 tasks, loaded 806` and
  `expected 1209 stars, loaded 1208`; `StoryScreenTests.RunHeadless` fails five
  cases in the same family (`areas 1-5 carry 25 repairs (24)`, `cost 29 stars
  (28)`, the rope's sign count, area 1's third repair region). Proven
  pre-existing: stashing this iteration's only source change and re-running the
  screen tests at HEAD gives the identical five failures. The constants were
  written in `22ddca8` under a previous PRD and never revisited; CLAUDE.md and
  `validate_story.py` both say 806 tasks / 1208 stars is the truth, so it is the
  gates that are stale. Left alone — changing an assertion to match reality is
  not a thing to do in passing.


---

## Iteration note — US-012 (levels 49-64), 2026-09-08

All four batches swept `0 finding(s), 0 error(s)` twice — before and after the
fix. Everything below came out of opening the 32 pictures.
Evidence: `sweeps/20260908-005720`, `-010237`, `-010905`, `-011324` (before);
`-013206`, `-013737`, `-014312`, `-014837` (after).

**Every one of levels 49-64 is the kit's shipped data, byte for byte** — board
and target asset both (`diff` against `tools/shipped_levels/` and
`tools/shipped_targets/`). So nothing about their difficulty, their pass marks
or their shapes is this project's to answer; only what the game draws over them
is.

## F-015 — the star chip was drawn twice the size of every other chip  [FIXED in US-012]
`SlotWeight` (F-010) gives a score target twice the plate width of a count,
because "0/5000" is nine characters and "25" is two. The icon box was the whole
slot and `preserveAspect` fits a sprite into whichever side binds first — so the
widened slot widened the STAR too. The star came out at the icon band's full
height on every plate; a count chip stayed capped by its own narrower slot.
Measured in design units: **133 against 58 on a three-chip plate (2.3x), 133
against 88 on a two-chip one (1.5x)**.

What that says on screen is the opposite of the truth. On level 53 the level
asks for 25 books and 30 shields and the score is the incidental third goal, and
the star was the biggest thing on the plate with the two real goals half its
size beside it. Levels 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 60, 61 and 63 all
showed it — every plate in the band with more than one chip.

`StoryLevelHud.SizeIconBox` centres the icon, its shadow and its backing in a
box the NARROWEST slot's width, on every chip. The plate still shares its width
out by number shape, so the numbers are unchanged; the icons are now one size
per plate. A single-chip plate is unaffected — there is nothing for it to be
uniform with. Before/after on level 49 and 50:
`sweeps/20260908-005720` vs `sweeps/20260908-013206`.

## Not acted on — measured, and the kit's own
- **Ingredients again collected zero.** Levels 49 (1+1 in 36 moves), 57 (2+2 in
  21) and 60 (2+2 in 35) ended with every ingredient chip untouched, which is
  the fourth band in a row to show it (22, 23, 24, 26 in US-010; 39, 42 in
  US-011). The spawn rule is `Random.Range(0, LevelManager.Limit) == 0` per
  generated gem, gated by `GetIngredientsCount(icon.name) < GetCount()` — which
  counts SpriteRenderers in the scene, i.e. ingredients already on the board. So
  a level needing one spawns one and then stops until it is collected, and the
  bot's problem is not spawning but DELIVERY: an ingredient is collected only by
  reaching `GetBottomRow()`, and these boards wall the lower rows with
  SOLIDBLOCK. Level 60's row 8 is `14 14 14 14 14 14 14 14 14` — a full-width
  wooden wall with a two-row pocket under it that nothing can refill. All three
  boards are the kit's own and all three are winnable (wood goes when a match
  lands beside it, and a vertical stripped clears a whole column through the
  wall). **This is a bot that does not aim, not a broken level** — proving
  otherwise needs a playtest that plays for the goal rather than for any legal
  swap, which is a harness story, not a level one.
- **Level 55's bottom half cannot be swapped in at all.** Row 5 is `00` across
  all nine columns and rows 6-10 are a `23`/`10` checkerboard, so every free
  cell's four neighbours are ice and no two free items are ever adjacent. Its
  whole goal — 23 blocks — lives down there and is reachable only by a vertical
  stripped or a colour bomb fired from the top half. Kit level, kit design, and
  it is why the bot broke 0 of 23 in 28 moves.

## Standing, after US-012
- **F-003 (dark backdrops) is still the only OPEN visual one, and only new art
  closes it.** `check_themes.py` prints the same ten NOTE lines. Levels 50, 53,
  54, 55, 60, 61 and 63 all draw their `00` holes as black rectangles cut out of
  the board for that reason and no other.
- F-005 stands. Eight batches printed `0 finding(s)` over the pictures that
  carried F-015.
- The two Unity batchmode gates (`StorySmokeTest.RunAll`,
  `StoryScreenTests.RunHeadless`) are still RED on the stale 807-task / 1209-star
  constants US-011 proved pre-existing. Unchanged by this iteration.


---

## Iteration note — US-013 (levels 65-80), 2026-09-08

All four batches swept `0 finding(s), 0 error(s)` twice — before and after the
fix. The one fault came out of opening the 32 pictures.
Evidence: `sweeps/20260908-015705`, `-020159`, `-021108`, `-021604` (before);
`-023246`, `-023749`, `-024609`, `-025055` (after).

All sixteen carry the kit's shipped target asset, and eight of them — 66, 67,
68, 69, 71, 75, 76 and 78 — are the kit's shipped board byte for byte. The other
eight (65, 70, 72, 73, 74, 77, 79, 80) differ in exactly one way and nowhere
else: every changed cell is a `17` that `CreateSquare` does not understand,
replaced with `10` by US-007's linter fix.

## F-016 — two target chips read as one number  [FIXED in US-013]
Level 65 asks for 25 of one gem and 15 of another and the plate read **`2515`**.
Measured off its own 393-point screenshot: the gap between the two chips was 6
pixels while the gap between the digits INSIDE `25` was 3 — so nothing on the
card said where one number ended and the next began. Level 69 (`15 35`) was
worse at 5 against 5, level 78 (`20 25`) the same at 5 against 3, and levels 68
and 75 were marginal at 7 against 5.

The cause is that a number is best-fit and centred in its own slot, so the only
thing keeping two of them apart is whatever slack each has left over — and a
two-digit count in a slot a quarter of a 100-pixel card wide has almost none.
`SlotWeight` (F-010) and `SizeIconBox` (F-015) had both already been here; both
are about the slot and neither is about what the slot's number does at its
edges.

`StoryLevelHud.SizeCountBox` keeps a `CountGutter` of the slot clear on each
side of the number whenever the plate carries more than one chip. Two of those
— one from each side of a boundary — are 26 design units, 9.5 points at 393
wide, which is about one digit's advance (11 points, measured off the same
shot). A gap a digit wide is the typographic minimum for two numbers that are
not one number.

Both sides or neither, never one. The first version gutted only the sides that
faced a neighbour, which left the first chip a wider box than the second and
best-fit then drew `25` visibly larger than the `15` beside it — the same "one
plate, two sizes" fault `SizeIconBox` exists to prevent. A single-chip plate
(levels 73, 79, 80 here) has nothing to run into and keeps its full width, and
its numbers are unchanged.

Measured, before → after, in pixels off the levels' own screenshots:

| level | chip boundary gaps | digit gaps inside a number |
|---|---|---|
| 65 | 6, 5 → **12, 12** | 3, 5 → 2, 3 |
| 68 | 7 → **14** | 5, 4 → 4, 3 |
| 69 | 5, 5 → **12, 12** | 5, 3 → 3, 2 |
| 78 | 5, 5 → **12, 12** | 3, 3 → 2, 2 |

Across all sixteen after-shots every multi-chip plate now separates its chips by
12 to 19 pixels against 1 to 5 inside a number.

## Standing, after US-013
- **F-003 (dark backdrops) is still the only OPEN visual one, and only new art
  closes it.** `check_themes.py` prints the same ten NOTE lines. Levels 65, 73,
  74, 76, 78 and 79 draw their `00` holes as black rectangles cut out of the
  board for that reason and no other.
- F-005 stands. Eight batches printed `0 finding(s)` over the pictures that
  carried F-016.
- **Not a fault, worth knowing:** level 80's end panel says `Already beaten`
  with a bronze star, because the bot won it in the before sweep and the after
  sweep replayed it. A replay paying no star is the economy working.
- The two Unity batchmode gates (`StorySmokeTest.RunAll`,
  `StoryScreenTests.RunHeadless`) are still RED on the stale 807-task /
  1209-star constants US-011 proved pre-existing. Unchanged by this iteration.


---

## Iteration note — US-014 (levels 81-96), 2026-09-08

All four batches swept `0 finding(s), 0 error(s)` twice — before and after the
fix. The one fault came out of opening the 32 pictures and then measuring them.
Evidence: `sweeps/20260908-030115`, `-030723`, `-031713`, `-032350` (before);
`-034022`, `-034641`, `-035202`, `-035726` (after).

All sixteen carry the kit's shipped target asset byte for byte, and thirteen
carry the kit's shipped board. The other three (85, 89, 94) differ in exactly
one way and nowhere else: every changed cell is a `17` that `CreateSquare` does
not understand, replaced with `10` by US-007's linter fix.

## F-017 — two target icons touched, and the shadow closed what was left  [FIXED in US-014]
Level 81 asks for two ingredients and a score, and its two ingredient icons —
both gold-rimmed jewels — sat **3 pixels apart** on its own 393-point
screenshot, against **16 between the numbers under them**. The pair read as one
clump. Levels 82, 85, 87, 90, 92, 94 and 95 showed the same 3, and level 84
(book, shield, star) 5.

The cause is the third of the same family. `SlotWeight` (F-010) sized the slot,
`SizeIconBox` (F-015) made every icon on a plate one size, `SizeCountBox`
(F-016) owned the boundary between two NUMBERS — and nothing owned the boundary
between two ICONS. `SizeIconBox` anchored the icon box to the whole slot, so all
that separated two icons was whatever transparent margin each sprite carries.

Worse, the icon casts its own silhouette one step down and **right** (F-012's
`IconShadowStep`, 6 design units = 2.2 points), so on a full-slot box that
shadow falls into the 3-point gap and closes most of it.

`StoryLevelHud.SizeIconBox` now also holds an `IconGutter` clear on each side of
the icon, its shadow and its backing, on every slot of a multi-chip plate.
`IconGutter` is **one shadow step** — the least an icon can be inset by and
still keep its own shadow inside its own slot. It is deliberately NOT
`CountGutter`: that one is measured against a digit's advance because two
numbers butt-joined read as one number, and applying it to the icon was built
and photographed first — it shrank every icon on a shared plate from 24 points
to 14, about the height of the digit under it, and left the card visibly empty
above the numbers (`sweeps/20260908-033201` is that version).

Measured, before → after, in pixels off the levels' own screenshots:

| level | chips | icon gaps | icon widths |
|---|---|---|---|
| 81, 82, 85, 87, 92, 95 | ingredient, ingredient, score | 3, 13 → **7, 17** | 25, 22, 26 → 16, 18, 22 |
| 84, 90, 94 | count, count, score | 5, 14 → **8, 17** | 24, 20, 26 → 15, 18, 22 |
| 86, 88, 89, 93 | score, blocks | 15 → **19** | 34, 31 → 30, 23 |
| 83, 91, 96 | score | — (single chip, untouched) | 50 → 50 |

Every multi-chip plate in the band now separates its icons by 7 to 19 pixels
where the widest was 5, at a cost of 2 to 4 pixels of icon.

## Standing, after US-014
- **F-003 (dark backdrops) is still the only OPEN visual one, and only new art
  closes it.** `check_themes.py` prints the same ten NOTE lines. Levels 89, 91,
  92 and 95 draw their `00` holes as black rectangles cut out of the board for
  that reason and no other — level 89's is a single cell in the middle of a full
  region and reads as a hole punched in the board.
- F-005 stands. Eight batches printed `0 finding(s)` over the pictures that
  carried F-017.
- **The ingredient shortfall did NOT repeat.** Level 92 collected 1 of 2 and 1
  of 1 under random play, the first band since US-010 where any ingredient
  reached its exit. The five other ingredient levels here (81, 82, 85, 87, 95)
  still ended at 0, and all five wall their lower rows the way US-012 described.
- The two Unity batchmode gates (`StorySmokeTest.RunAll`,
  `StoryScreenTests.RunHeadless`) are still RED on the stale 807-task /
  1209-star constants US-011 proved pre-existing. Unchanged by this iteration.


---

## Iteration note — US-015 (levels 97-112), 2026-09-08

**This band straddles the seam.** 97-100 are the last of the kit's own hundred;
101-112 are the first generated levels anyone has played. Levels 97-100 carry
the kit's shipped target asset AND its shipped board byte for byte, except
level 100, which differs in exactly one way: two cells that were the digit `17`
`CreateSquare` does not understand, replaced with `10` by US-007's linter fix.
Nothing in 97-100 needed changing, and nothing in them was changed.

Everything below is about the generated twelve.

## F-018 — the same board twice in a row  [FIXED in US-015]
**Levels 111 and 112 were the same level.** Byte-identical boards, same MODE 0,
same 9x11, same COLOR LIMIT 4, one more move (27 against 28) and a pass mark
eight points apart (1495 against 1503). Both drew template 97. Levels 108 and
109 were the same board with its 2x2 block region mirrored: same size 9/5, same
LIMIT 0/8, same mode, stars 98 and 99. A player reads that as the game
repeating itself, and the sweep reported `0 finding(s)` over both pairs.

`level_gen.pick_template` chose from a seven-wide window of templates around the
level's difficulty rank, and its own docstring said the window was there "so
consecutive levels do not all land on the same board". **The window never did
that.** Each level drew from it independently with its own seeded RNG, so
nothing stopped two levels in a row drawing the same number. Over the 1108
generated levels that happened **107 times** — four in a row at 240-243,
356-359, 945-948 and 1145-1148 — and **16 of those pairs came out
byte-identical**, because the mutations are coin flips (mirror at 1/2, flip at
1/4) and can land the same way twice.

The fix is the rule the docstring already claimed: `pick_template` now takes
`recent`, the templates the last `RECENT_TEMPLATES` levels used, and drops them
from the window. A preserved level goes into that window too — level 100's
board IS template 100's board, so level 101 must not draw it.

`RECENT_TEMPLATES = 4` is measured, not chosen. A longer ban list needs a wider
rank window to draw from, and that is the whole trade:

| window | recent | fell back | fewest candidates | closest repeat | rank offset |
|---|---|---|---|---|---|
| ±3 | 4 | **0** | **2** | 5 levels | 3 |
| ±4 | 6 | 0 | 1 | 7 | 4 |
| ±5 | 8 | 15 | 1 | 9 | 5 |
| ±6 | 10 | 28 | 1 | 10 | 6 |

"Fell back" counts the levels where the ban list emptied the window and the
guard had to hand back a banned template — where the guarantee is not a
guarantee. ±3 with four is the only row that holds at every level *and* still
leaves every level a choice between two boards. Every wider setting buys a
bigger headline number by breaking the rule at fifteen levels or more, and
pulls the board further from the difficulty the plan asked for.

Measured over the finished grids of all 1208 levels, before → after:

| | before | after |
|---|---|---|
| same template back to back | 107 | **0** |
| same template two apart | (unmeasured) | **0** |
| same template three or four apart | — | **0** |
| byte-identical boards back to back | 16 | **0** |
| closest byte-identical pair anywhere | 1 level | **5 levels** |

646 level files and 643 target files were rewritten, **all of them above 100**.
Levels 1-100 are untouched, so every band US-009 through US-014 swept is
unaffected by this change. The three generators are still idempotent: running
`level_templates.py && level_gen.py && target_gen.py` twice leaves the tree
byte-for-byte identical (checked by hashing every file under `Resources/Levels`,
`Resources/Targets` and `tools/out`, not just the file list).

### Still open after the fix, and honestly
**Two identical-grid pairs remain inside this one band: 101/111 and 102/112,
ten levels apart each.** Same board, same collect goals, pass marks 101/100 and
506/501, one move apart. That is the residual the table above prices: closing it
means either breaking the guarantee at fifteen other levels or drawing the board
from further off its planned difficulty. With 100 templates and 1108 generated
levels each template is used about eleven times, so repeats at *some* distance
are arithmetic, not a bug. Recorded here rather than fixed, because the fix has
a cost that has to be decided rather than assumed.

## Measured but NOT a fault — checked and cleared
Three things looked wrong in the shots and are not. Written down so the next
iteration does not re-open them:

- **The lose panel's star is gold to the eye and bronze to the meter.** Same hue
  as the win star (38° against 39°), same saturation (0.90), but value 0.54
  against 0.85 — a 0.63x multiply. F-009's claim holds; the panels differ by
  that, by the win's pale blue socket disc behind the star, and by the caption.
- **"Already beaten" on levels 101 and 110 is correct.** Both were won (the
  report says so: level 101 "RESULT: won. Score 2000, stars 1"), and both had
  been beaten in an earlier sweep of the same install, so no new star is
  awarded and the star is drawn dull. It is the win panel's replay caption, not
  a congratulation on a loss.
- **A count chip shows what is LEFT, a score chip shows progress.** Level 111's
  end HUD reads ring `1` and `100/100`: the score was made, the ingredient was
  not, and the panel correctly says "Out of moves".

## NOTE — one plate, two number sizes  [OPEN, not fixed in US-015]
Measured off the band's own 393-point screenshots, by glyph run over the number
band of the target card. The numbers on ONE card are drawn at heights that
differ by up to 1.75x, and the direction is not consistent:

| level | chips | number heights | score is |
|---|---|---|---|
| 103 | 0/1018, 3, 1 | **8**, 14, 13 | smaller |
| 107 | 0/975, 3, 1 | **10**, 14, 13 | smaller |
| 106 | 1, 1, 0/485 | 13, 13, 11 | smaller |
| 110 | 25, 35, 0/991 | 9, 9, **11** | bigger |
| 98 | 30, 30, 0/2600 | 9, 9, 8 | matched |

Each chip's number is its own best-fit Text, so the size is whatever fits that
string in that box: a six-character score is width-limited to h8 while a
one-character count beside it stays h14. This is the fourth member of the
`SlotWeight` / `SizeIconBox` / `SizeCountBox` family — nothing owns the
relationship between the SIZES of two numbers on one plate — but the obvious
rule (one plate, one size, the smallest that fits) would drag every count down
to h8, which is worse than the fault. It wants a real answer, and it did not get
one this iteration.

## Standing, after US-015
- **F-003 (dark backdrops) is still the only OPEN visual finding that new art
  alone can close.** `check_themes.py` prints the same 10 NOTE lines. Levels
  102, 103, 104, 110 and 112 draw their `00` holes as black rectangles cut out
  of the board for that reason and no other.
- F-005 stands. Seven batches across three sweeps printed `0 finding(s),
  0 error(s)` over the pictures that carried F-018.
- The two Unity batchmode gates (`StorySmokeTest.RunAll`,
  `StoryScreenTests.RunHeadless`) are still RED on the stale constants US-011
  proved pre-existing, and this iteration re-confirmed the exact wording:
  `[Story] expected 807 tasks, loaded 806` and `[Story] expected 1209 stars,
  loaded 1208`. `tools/validate_story.py` passes on 150 / 806 / 1208 / 1208.


---

## Iteration note — US-016 (levels 113-128), 2026-09-08

Two faults, one in the game and one in the thing watching it. Both were found by
opening the pictures; all eight sweep batches printed `0 finding(s), 0 error(s)`
over the shots that carried the first one.

## F-019 — the score chip was three pixels tall  [FIXED in US-016]
On a plate that carries more than one chip the score read `have/need` — nine
characters at its longest — beside a count of one or two. Measured by glyph run
over the number band of the band's own 393-point screenshots:

| level | plate at the end | digit heights |
|---|---|---|
| 113 | `0  0  1511/1511` | 14, 14, **3** |
| 117 | `0  0  1750/2438` | 14, 14, **3** |
| 122 | `0  0  1002/1002` | 14, 14, **3** |
| 120 | `3  3  496/496` | 14, 14, **4** |
| 125 | `1  1  460/485` | 14, 14, **5** |
| 128 | `0  19  420/493` | 8, 8, **5** |

Level 122's slash was lost between two digits at that size and the chip read as
one eight-digit number, `10021002`. This is not a fault that a different share
of the plate can fix — nine characters and two counts do not fit on a 118-pixel
card at one legible size — so the STRING is what gives.

`HudTarget.DisplayShared` is the plain number still owed, and a shared plate
draws it on every chip, score included. It is what every other chip already
says, so the plate now reads one way throughout: how much is left, green when it
is none. **Alone on the plate the score chip is unchanged** and still reads
`0/970` at full size — the fault is the sharing, so the seam is `count > 1`, the
same one `SizeCountBox` and `SizeIconBox` already turn on. 1062 of the 1208
levels carry more than one chip.

## F-020 — one plate, three number sizes  [FIXED in US-016]
The NOTE US-015 left open, and the fourth member of the `SlotWeight` /
`SizeIconBox` / `SizeCountBox` family: those three own how much ROOM a chip
gets, and nothing owned the relationship between the SIZES that room produces.
Best-fit solves each chip alone, so level 113 read `20 2 1511` at three
different heights — the two-digit count smallest, the single digit half again
as tall as it.

`StoryLevelHud.MatchNumberSizes` measures each string against its own box at the
cap size and draws every chip on a shared plate at the smallest of those. It
takes NOTHING down: the size chosen is the one the tightest chip already draws
at. Over all 1208 levels it settles at the cap on 695, at 17 on 229 and at 14 on
284, and never reaches the floor.

That floor of 14 is what F-019 bought. While the score chip still read
`have/need` the tightest chip on a shared plate was a nine-character string at
the minimum, and matching to it would have taken every number down with it —
which is exactly why US-015 recorded "one plate, one size" as a rule that could
not be applied yet. Fix the string first, then the sizes.

## F-021 — the sweep called a slow win a broken one  [FIXED in US-016]
`StoryPlaytest.Ending` waited a flat 60 seconds for `PreWinAnimations` and then
reported "the level was won but never showed its win". Level 118 crossed it and
was reported as a level that never showed its win, having shown it moments
later — the same level had finished the same animation in 17.5s and 21.5s on the
two runs before.

Over the 71 wins in this machine's sweep reports the animation runs from half a
second to **48.5**, and it does not scale with what it has to spend: level 111
took 33.5s with no moves left to convert and level 80 took 30s with 57. A
60-second bound is 1.24x the worst real win, so it was going to false-alarm.

`WaitOut` gives up on PROGRESS instead of on the clock: it waits while the score
or the move counter is still moving and calls the level stuck only after
`StallWindow` (15s) of complete quiet, with a 120s ceiling that stays under the
sweep's per-level budget. 15 is measured — the longest quiet gap inside a win
that DID finish, across this band, is 5.5s, and every fail banner moved the
state within 0.5s. A stuck level is now reported four times sooner than before,
and a slow one is not reported at all.

**Ask whether a new finding is about the game or about the thing watching it.**
This one arrived in the same run as a HUD fix and looked like its fallout.

## Measured but NOT a fault — checked and cleared
- **Level 113's plate says `20 2` where its level file says `COLLECT COUNT
  20/20`.** The asset is right and the file is inert: `LevelManager` parses
  COLLECT COUNT into `ingrCountTarget`, which it reads only in INGREDIENT mode,
  and 113 is COLLECT. The chip counts come from `Targets/Level113.asset`, which
  says 20 and 2. Kit data, carried verbatim by `target_gen.py`.
- **Interior `00` holes are not black rectangles that the code draws.** They are
  the arc backdrop seen through the board, and the backdrop is a near-black
  painting. Levels 115, 117, 120, 122, 124, 125 and 127 all show it. F-003, and
  only new art closes it.

## NOTE — "Out of time" over a plate of finished goals  [OPEN, not a fault yet]
Level 122 lost on the clock with `COLLECT 32/35` and the cascade that was still
settling finished it at 35/35, so the end shot is a fail panel over a strip
reading `0 0 0` in green. The loss is honest — the timer expired first, and the
report records both numbers — and it happened once in 24 level-plays across this
band. Freezing the strip at `GameOver`/`PreFailed` would fix the reading and
would also stop the win animation's score climbing, so it wants the two states
separated rather than one guard. Recorded rather than done.

## Standing, after US-016
- **F-003 (dark backdrops) is still the only OPEN visual finding that new art
  alone can close.** `check_themes.py`: same 10 NOTE lines.
- F-005 stands. Eight batches across four sweeps printed `0 finding(s),
  0 error(s)` over the pictures that carried F-019 and F-020.
- The 101/111 and 102/112 identical-grid pairs US-015 priced are unchanged.
- The two Unity batchmode gates are RED on exactly the five screen assertions
  and the two smoke constants they were red on before this iteration, checked
  line for line against `screen_base.log` and `smoke_us015.log`.


---

## Iteration note — US-017 (levels 129-144), 2026-09-08

Four batches of four, `0 finding(s), 0 error(s)` in every one of them, over 32
screenshots. One fault, found by opening the pictures — and its shape came out
of the *board data*, not the HUD, which is the first time in this sweep that it
has.

Levels 129-136 are area 16 The Library, 137-144 area 17 Leo's Study; both are
arc 1, so both draw the same backdrop. That is F-003 and nothing new.

## F-022 — cells no match can ever reach  [FIXED in US-017]
**Level 141 opens with four gems sitting alone, each in its own little frame,
off the edge of the board.** They never move for the whole level. The board is
9x11 and its top two rows are

    10 00 00 10 10 10 00 00 10
    00 10 00 10 10 10 00 10 00

so (0,0), (0,8), (1,1) and (1,7) each have four holes around them. A match is
three of a colour in a line and a swap only reaches an orthogonal neighbour, so
both live entirely inside one region of the board: a region holding no three
collinear cells cannot host either, whatever falls into it.

Counted that way over all 1208 boards — treating only `00` and `x6`
(UNDESTROYABLE) as permanent walls, because `Square.DestroyBlock` cascades into
SOLIDBLOCK and THRIVING the moment a match lands beside one — there are **55
such regions across 25 levels, and every one of them is a single cell**:

| shape | levels | what the cell is |
|---|---|---|
| the kit's level 97 | 97, and 9 generated (127, 141, 151, 160, 259, 269, 277, 286, 307) | a frozen gem, four per board |
| the kit's level 45 | 45, and 14 generated (433, 539, 547, 563, 570, 577, 583, 598, 631, 636, 643, 654, 662, 675) | a **block**, in the middle of a picture frame |

US-011 measured this and left it: "a linter rule waiting for a decision about
whether the generated 22 should inherit shapes that contain one". US-015's
template re-pick moved the list (22 → 23, different numbers), which is the
reason to compute it rather than write it down.

**None of the fourteen was unwinnable** — their BLOCKS goal asks for 62 and 64
to 72 blocks are reachable, so the level ends normally with one blocker still
iced. Checked, level by level, against the pre-change boards, because "the goal
counts a block you cannot clear" was the obvious guess and it was wrong.

The fix is `level_gen.seal_unmatchable_cells`, run last in `generate()` so it
sees the shape mirror, flip, thicken and sabotage left behind: it writes `00`
over every cell of an unmatchable region. That is the hole the board was
already behaving as if it had. `block_target_count` is read off the finished
grid into the provenance file, so the fourteen BLOCKS goals follow by
themselves; no target asset changed, since all fourteen ask for 62 and still
have 64+.

**It changes 23 files and no others.** `pick_template` is untouched, so nothing
is re-picked and no other level moves a byte — 101-128, already swept, are
unaffected except level 127, which is one of the 23 and was re-swept and
re-photographed. Idempotence re-checked by hashing the content of
`Resources/Levels`, `Resources/Targets` and `tools/out`.

`tools/lint_levels.py` gained the rule so it cannot come back, and with it a
NOTE channel. The kit's levels 45 and 97 carry the same fault and this project
may not rewrite them — the generator does not write levels 1-100 and a
hand-edit is undone by the next run — so a fault on a preserved board reports
as `<level>: NOTE ...` and fails only under `--strict`. Above 100 it is a
problem. Today: **0 problems, 5 notes**.

## Measured but NOT a fault — checked and cleared
- **Level 129's pass mark is 99 and it scored 1780.** Level 143 is the same at
  100. Both come from the kit's template 11 (`STARS 100/1800/2300`), an
  INGREDIENT level where the score is a formality and the ingredient is the
  gate. The kit's own design, inherited faithfully.
- **Level 138 gets 11 moves for two ingredients.** Its template is the kit's
  level 24, which ships `LIMIT 0/10` — the generated level is *more* generous
  than the board it came from.
- **Levels 130/142 and 132/144 are the same board twelve apart**, pass marks
  1496/1497 and 5041/5046. That is the residual US-015 priced: 100 templates
  over 1108 generated levels is eleven uses each, so repeats at some distance
  are arithmetic. The guarantee is "not inside 5 levels" and it holds.
- **Level 134's blocks look like blue ice and level 144's like lavender.**
  `CreateSquare` swaps in the `doubleBlock` sprite for DOUBLEBLOCK, and
  `thicken_blockers` promoted 144's. Two sprites, one goal, the kit's own.
- **The single-cell-wide columns on 132/144 are playable.** A vertical swap
  inside a one-wide column can make a three-run (`[B,A,B,B]` → `[A,B,B,B]`),
  and the column refills from its own top row. The bot cleared 4 of 18 blocks
  on 132 and 0 on 144 — random play, not a wall.

## Standing, after US-017
- **F-003 (dark backdrops) is still the only OPEN visual finding that new art
  alone can close.** `check_themes.py`: same 10 NOTE lines.
- F-005 stands, and this iteration is its sharpest case yet: six batches
  printed `0 finding(s), 0 error(s)` over the shots that carried F-022, and the
  linter — which reads every board in 0.2s — had no rule for it either.
- The NOTE US-016 left open ("Out of time" over a plate of finished goals) did
  not recur in this band's 24 level-plays.
- The two Unity batchmode gates are RED on exactly the five screen assertions
  and the two smoke constants of the recorded baseline, diffed line for line
  against `screen_base.log`. No C# changed this iteration.


---

## Iteration note — US-018 (levels 145-160), 2026-09-08

Four batches, `0 finding(s), 0 error(s)` in all of them once the band could be
played at all — and the first batch could not. Three faults, one of them the
first in this sweep that the harness itself was hiding.

Levels 145-146 are area 17 Leo's Study, 147-156 area 18 The Map Room, 157-160
area 19 Royal Pool. All three are arc 1, so all three draw the same backdrop.
That is F-003 and nothing new.

## F-023 — the story beat before level 157 was reported as a black screen  [FIXED in US-018]
`playtest_sweep.py --levels 157-160` played **one** level and stopped:

    level 157: never became playable — state Map, 45 square(s) on the board.
    Nothing is drawn, which on screen is a black screen.

The board was fine. Level 157 is the one level in the game carrying
`sabotageBeat`, and `StoryFlow.LaunchLevel` holds the board back until
`StorySabotage.PlayIntroIfDue` is done. A beat waits for a tap forever;
`StoryPlaytest.Reach` only slept. After 30 seconds it called the level dead, and
because that finding `break`s the run, levels 158, 159 and 160 were never played.

`Reach` presses the dialogue's own full-screen `Advance` button while it waits —
the same three lines `StoryThemeTest` and `StoryShellTest` already had — and
takes a `_beat` screenshot of the card first, after one press so the typewriter
has finished. **The card is what the player is looking at there, so it is worth
a picture**; the two faults below were both found in it.

## F-024 — the sabotage beat was spoken into a void  [FIXED in US-018]
`sweeps/20260908-072831/shots/b001_0001_level_157_start.png`: Alfred's card and
a Skip button on a **completely black screen**. The beat fires from
`StoryFlow.LaunchLevel` — the shell has come down for the level and the board
has not been loaded — so there was nothing behind it. The one moment the game
names Grimm out loud was a cream card on black.

`OpeningSequence` had already solved this and says so in its header ("without
this the dialogue card floats on nothing — which is the black screen the old
cutscene was blamed for"). It had solved it privately. That backdrop is now
`StoryDialogueRoom` — canvas 810, the area's own room covering the screen under
a `RectMask2D`, the same 0.32 dim the opening measured — and both callers use
it. The beat now plays over Royal Pool.

## F-025 — over half the game's dialogue fell out of its card  [FIXED in US-018]
The same picture, read again: the card holds four lines, `tap to continue` sits
**on top of** the fourth, and the fifth — "Opened." — is drawn on the black
below the card, in ink, clipped by the bottom of the screen.

The card was a fixed 400 units tall with the body inset 100 from its top and 70
from its bottom. Measured off the PNG that is 92 pixels of body at a 31-pixel
line pitch: **three lines.** The line that found it is 70 characters. The
**median** line in this game's dialogue is 79, and **185 of the 354 lines the
game speaks are over 70** — so this was most of the story, not one beat.

`DialogueSystem` now sizes the card to its line. It grows upward from the same
bottom edge (`MeasuredBodyHeight` wraps the real string in the real font at the
real width — a row count cannot be guessed from a length), stops where the Skip
button begins, and only then does the type shrink, 54 down to a floor of 34.
A short line draws exactly as it always did. The portrait moved from a middle
anchor to a top one so it stays beside the name plate as the card grows.

`StoryScreenTests.DialogueCardHoldsEveryLineTheGameSpeaks` reads every line out
of `cutscenes.json` plus the sabotage line and asserts each one fits — so a
longer line written into the markdown tomorrow is covered without anyone
remembering to add it.

## F-026 — 68 boards are not in the middle of the screen  [FIXED in US-018]
Level 149's board has an 85-point margin on its left and 103 on its right.
`LevelManager.GenerateLevel` puts the field at `-maxCols / 2.75f` and then lays
its squares out `squareWidth` apart, and those two numbers only agree for one
board width:

    centre = -maxCols / 2.75 + (maxCols - 1) * squareWidth / 2

At the shipped `squareWidth` of 0.82 that is zero at **nine** columns and
nowhere else. 1140 levels are nine wide and centred to within a third of a
pixel; the other 68 are 5, 6 or 8 wide and sit 9.0, 6.6 and 2.0 pixels left of
centre at 393 points. **The kit ships the same fault on eight of its own hundred
levels** (3, 5, 6, 8, 11, 12, 24, 62), so no level file and no generator can fix
it.

`StoryBoardCentre` aims the CAMERA at the middle of the grid, on
`OnLevelLoaded` / `OnStartPlay` like `StoryBoardTheme`. Not the board: three
other things hang off the field's only parent, and the field's own x is driven
every frame of the slide-in by the legacy clip `AnimateField` builds, so writing
it afterwards would land the board and then jerk it sideways. Moving the camera
moves nothing else — the level's canvases follow it and the board's effects are
children of the board. Measured after: level 149 went from -9.0 to -0.5 pixels.

It centres the GRID, not the filled cells. A board with an empty left column is
a board the level file drew that way, and re-centring on what is left would move
the 1140 boards that are already right.

**Vertical is deliberately untouched.** The same arithmetic is off in y
(`maxRows / 2.75`), but the play area is not vertically symmetric — HUD above,
booster row below — and the kit already tunes it per goal (`yOffset = 0.3` for
INGREDIENT). There is no "correct" answer to compare against the way there is
for x.

## F-027 — level 547 lost the blocker the plan says it has  [FIXED in US-018]
`validate_story.py` was RED at the branch point and the US-017 report says it
was green:

    FAIL: level 547: plan says sabotage=True but the board says False

`seal_unmatchable_cells`, added in US-017, ran last in `generate()` — after
`apply_sabotage` — and on level 547 the cell it sealed was the cell the sabotage
had just been placed on. Sealing now runs BEFORE the sabotage. It is free to
move: `unmatchable_regions` treats only holes and undestroyable blocks as walls,
so it sees the same shape either way, and it still runs after the mirror, flip
and thickening that decide that shape. 8 level files changed (269, 286, 433,
539, 547, 583, 631, 675), all above 260 and none in a band already swept; no
target asset moved.

## Measured but NOT a fault — checked and cleared
- **Levels 146 and 156 are two boards with a wall of holes between them.** The
  kit's own level 55 ships exactly that shape and 146 inherits it. Both halves
  hold three-in-a-line and both refill from their own top row.
- **Level 155's two one-wide columns.** The same shape US-017 cleared: a
  vertical swap inside a one-wide column can make a three-run, the column
  refills from its own top row, and its eleven lavender cells are DOUBLEBLOCK,
  worth two each against a goal of 18.
- **Level 148's board sits high with a lot of room under it.** Five rows on a
  screen sized for eleven. Its grid centre is 34 points above the middle of the
  play area, which is the vertical case above, left alone on purpose.
- **Levels 151, 152, 158 and 160 end on "Already beaten" with their goals
  met.** They are replays — the save carries 43 levels beaten from the earlier
  batches — and the bronze star is what a replay is supposed to draw.

## Standing, after US-018
- **F-003 (dark backdrops) is still the only OPEN visual finding that new art
  alone can close.** `check_themes.py`: same 10 NOTE lines.
- F-005 stands, and this iteration is a new shape of it: the harness did not
  merely fail to see F-024 and F-025, it **prevented** them being seen, by
  dismissing the one screen they were on as a black screen and then abandoning
  three levels. A report of `0 findings` over four levels was, in the first run,
  a report over one.
- `validate_story.py` is green again for the first time since US-017.
- `lint_levels.py`: 0 problems, 5 notes. Generators idempotent by content hash.
- The two Unity batchmode gates are RED on exactly the five screen assertions
  and the two smoke constants of the recorded baseline, diffed line for line
  against `screen_base.log` and `smoke_us017.log`. Three new screen cases were
  added this iteration and all three pass.
