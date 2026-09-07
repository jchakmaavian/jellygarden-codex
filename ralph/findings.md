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
