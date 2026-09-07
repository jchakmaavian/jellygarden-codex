# Story layer — King Leo

Everything in this folder is **additive** to the Jelly Garden kit. The kit is a
purchased asset with hand-made art, animation and particle effects; the story
sits on top of it and must never cost it any of those.

## The hard rule

**No existing prefab, Animator, Animation clip, ParticleSystem or sprite may be
deleted, renamed or restructured.** `tools/check_vfx_intact.py` enforces this
against the branch point and fails the build if it is broken.

The board simulation (`LevelManager`, `Item`, `Square`) is off limits beyond
additive hooks — with one exception already taken, and it is worth knowing about
because the same shape will recur: `LevelManager.FallingDown` enumerated the
`combinedItems` FIELD across its own `yield`s, so a second pass through the
coroutine reached that field's `Clear()` mid-enumeration and .NET threw
`InvalidOperationException: Collection was modified` straight out of the
`foreach`. That kills the coroutine where it stands and leaves the cascade half
finished on a board that looks like it is thinking. It is walked over a snapshot
now — same lists, same items, same order, so every destroy, popup and particle
still fires identically. The playtest caught it on level 9.

In particular, do not swap gem sprites: `Item` derives its own
colour by looking its sprite up in its array (`color = Array.IndexOf(items,
sprite)`), and items spawned mid-cascade come from the prefab, so a themed gem
can end up a colour that does not match how it looks.

## How the kit and the story meet

Marked hooks, all early-returns, all a two-line deletion from the kit's
original behaviour:

| File | Hook | What the story does instead |
|---|---|---|
| `InitScript.OnLevelClicked` | `StoryFlow.HandleMapNodeClicked` | opens the area screen, not the pre-play popup |
| `AnimationManager.CloseMenu` | `StoryFlow.HandleLevelComplete` / `HandleLevelFailed` | returns to the area, not the next level |
| `InitScript.SpendLife` | `StoryFreeGame.ShouldSpendLife` | never takes a life |
| `InitScript.CheckAdsEvents` | `StoryFreeGame.AdsAllowed` | never requests an ad |
| `MenuManager.ShowGemsShop` / `ShowLiveShop` | `StoryFreeGame.ShopsAllowed` | never opens a shop |
| `MenuManager.ShowRate` | `StoryFreeGame.RatePromptAllowed` | never asks for a rating |
| `MenuManager.ShowPreFailed` | `StoryFreeGame.PreFailOfferAllowed` | never offers five more moves for gems |

Star awarding rides on `LevelManager.OnWin`, and the fail panel on
`LevelManager.OnLose` — both events the kit already raises, so nothing in the
board is touched at all.

**`ShowPreFailed` is the one hook that cannot just return.** That banner's own
animation event is what sets `GameState.GameOver`, and GameOver is what raises
`OnLose` and puts the end panel up, so skipping the banner without moving the
state leaves a lost level with no way to end. The hook starts a one-frame
coroutine that does what the animation event would have done. Any future kit
hook sitting on a screen the kit uses as a state machine needs the same care:
check what the thing you are hiding is *driving* before you hide it.

## The data contract

Nothing here hardcodes an area id. Everything comes from `Resources/Story`:

| File | Written by | Read by |
|---|---|---|
| `arc1..arc6.json` | `tools/story_sync.py` | `StoryDatabase` |
| `index.json` | `tools/story_sync.py` | `StoryDatabase.Arcs` |
| `level_plan.json` | `tools/level_plan.py` | `StoryLevelPlan` |
| `cutscenes.json` | `tools/cutscene_build.py` | `CutsceneRunner` |
| `themes.json` | `tools/themes_build.py` | `StoryTheme` |
| `Art/*`, `Audio/*` | `tools/asset_import.py` | `StoryArt`, `StoryAudio`, `StorySfx` |

Art is resolved **by convention**, not by inspector reference:
`area_{id:000}_state{n}`, `clue_{id:000}`, `backdrop_{theme}`,
`portrait_{name}`. Dropping a correctly-named file into `Resources/Story/Art`
wires it up with no code change. A miss warns **once per key** and falls back to
the arc backdrop — never per frame, never blank.

`StoryFlow.LaunchLevel(levelNumber, areaId)` takes the area because the theme,
the obstacle set and the repair the level pays for all come off the area, not
off the level number. **Never pass a literal.** `StoryLevelPlan.AreaOf(level)`
is the map; it returns 0 for a level nothing owns, and 0 is a fault to report,
not a reason to fall back to area 1. `StoryPlaytest` passed `1` for four
commits, which meant 1205 of the 1208 levels were being played wearing area 1's
theme and reported as fine.

## The repair animation

Pressing Do It plays one of two things, and which one depends on whether a room
is already on screen.

**On a room** — every repair bought from the area page or from home. Gold
sparkles fall over the part of the room this repair changes, the room's own view
crossfades to the repaired state under them, and the spent star flies to the
counter. No black screen, no words, no camera move, and it is over in 1.85
seconds. This is Royal Match's own shape; see
`docs/royal-match/03-repair-in-place.jpg`, which is one caught mid-play with the
room, the sparkles and the buttons all still on screen.

**On its own screen** — the map, the kit's own scenes, anywhere with no room to
play over. Still the four-second cutscene: the room fills a black screen, the
task's own words appear, the work happens, and the repaired state wipes across
with the story's line for it. It is kept because there is nothing else on those
screens to carry the beat, and the story's line for that repair has nowhere else
to be said.

The class is still called `RepairCutscene`. Half wrong, kept: renaming it would
churn every call site to describe one of its two paths.

Three things the room path does differently, and each one had to be found by
running it rather than by reading it:

- **The black was not the cutscene's.** `AreaScreen.HideForRepair` painted its
  own backdrop black, and the room on that page is letterboxed — so the black
  showed as bars top and bottom and the repair read as a cut to another screen.
  The backdrop stays clear now and the room is switched to enveloping
  (`AreaSceneView.SetCovering`) for the length of the repair.
- **The sparkles were invisible at first.** Small glints at a twentieth of the
  room across, 18% of them large, vanish entirely into a bright room. They are
  30% large, up to a seventh of the room across, at 260 a second, and the pool
  had to go from 90 to 220 — a pool that runs dry does not look sparser, it
  looks like the effect stuttering.
- **The landing flash is halved over a room** (0.24 against 0.55, 0.22s against
  0.45s). At the cutscene's value the whole home screen blinks white, which
  reads as a bug rather than as a repair.

### Where the sparkles go

`StoryRepairRegions.For(areaId, state)` is the rect a repair changed, in the
room's own space. It comes from a **pixel diff of the room's own artwork**,
taken offline by `tools/repair_regions_build.py` and shipped as
`Resources/Story/repair_regions.json`.

Offline, not at runtime: reading a sprite's pixels needs `isReadable` on the
importer, which keeps an uncompressed second copy of every room in memory, and
150 areas of 1500px artwork is not a price worth paying to learn four
rectangles.

**Most repairs are not in the table and that is the design.** An area with one
restored picture has no diff to take, and a state that repaints the room
wholesale has a "region" that is the whole picture with extra steps — the build
refuses anything over 55% of the room. Those report `Unknown`, and the sparkles
fall over the whole room. Guessing a corner would put them where nothing
happened.

### The spent star

`StoryStarFlight` throws it from the pressed row to the counter. Two rules, both
learned the hard way:

- **Buy first, fly second.** The first version bought the repair when the star
  landed, and the shell self-test failed on all five areas at once — it presses
  Do It and immediately asks whether the repair was bought, and for 0.38 seconds
  it had not been. That is the same complaint from the player's side.
- **It cannot live on the screen that spent it.** The repair takes the area page
  down, counter included, in the same frame. It flies on its own canvas at
  sorting order 800 and takes both ends as SCREEN POINTS read before anything is
  hidden.

Nothing about it is authored per task. The 807 tasks use 83 verbs but describe
ten kinds of work, so `RepairWork.Classify` reads the label and
`RepairVfx` picks the effect from that — dust for clearing, sparks for
rebuilding, embers for lighting, a gold sweep for polishing. 93% of tasks
classify specifically; the rest get a generic effect. The classifier reads the
WHOLE label, not the first word: "Clear the flooded corridor" is water, "Clear
the last rubble" is stone.

Two things worth knowing before changing it:

**The particles are UI quads, not a ParticleSystem.** A ParticleSystem does not
render inside a ScreenSpaceOverlay canvas at all — it is a mesh renderer and the
overlay draws after everything in the scene, so the effect is simply invisible.

**Layer order in the cutscene is the whole trick**: Before, then the neglect
wash, then After on top. Putting the wash above the clean layer dirties the
thing the repair just cleaned. Below it, an area with only ONE picture still
reads as before-and-after — which is why 23 of the 25 Arc 1 areas ship a single
`area_NNN_restored` image rather than a picture per repair.

Outside play mode `RepairCutscene.Play` calls straight back: there is no frame
loop for its coroutine, so it would otherwise hang. Editor tests cover the
economy and the classifier; the cutscene itself is verified by running the
built player with `-storydemo`.

### Playing it OVER the room

`RepairCutscene.Play(areaId, task, stateAfter, room, onDone)` plays on a room
that is already on screen instead of on a black screen of its own — which is
what Royal Match does and what a repair bought from the area page gets.
`StoryRepair` decides, and it looks in two places: the area page when it is open
on this area (passing `AreaScreen.RoomRect` and calling
`AreaScreen.HideForRepair()` first), and the home screen's
`StoryHomeScreen.RoomRect` when it is not. Only a caller with NEITHER — the map,
the kit's own scenes, a test — passes null and gets the black screen.

Three things that mode changes, all of them for the same reason — the room the
player is looking at is the real one, not a copy:

- The cutscene's own Before/Damage/After images are **disabled**, and its
  backdrop cleared. What animates is the live `AreaSceneView` underneath.
- **The reveal belongs to the cutscene, not to the purchase.**
  `AreaSceneView.NotifyTaskCompleted` is fired at the reveal beat (and on a
  skip, and on the no-frame-loop early return) rather than by `StoryRepair`
  before the cutscene starts. Fired at the purchase, the room changed while the
  player was still looking at the "before".
- The stage is matched to the room through **world corners**, not by copying
  anchors: the room lives on another canvas with its own scaler.

`AreaScreen.HideForRepair()` also takes down the top bar and the captions, which
is what the reference does: `03-repair-in-place.jpg` has the room running to the
top edge with no counters over it. Whatever hides has to come back —
`ShowAfterRepair` is called from the cutscene's callback, and again from
`AreaScreen.Open`, because a repair that finishes an area closes the screen
while it is still hidden.

The neglect wash on a live view was only ever computed when the view rebuilt,
which happens on an area change and on nothing else — so a repair watched on
screen left the room exactly as grimy as it found it. `AreaSceneView.Reveal`
now animates it (`LiftDamage`) and `SyncDamage()` re-reads it for a screen
refreshing after a change it did not watch. `AreaScreen.Refresh` calls it.

## The economy

A level win pays **one star into that area's pool**. Repairs are bought with
those stars, in the order the story wrote them. The clue fires on the **last
repair of an area, never on a level win** — that rule is in `StoryRepair` and it
is the one thing not to move. Replaying a beaten level pays nothing.

Stars do not travel between areas, so each area's own levels fund its own
repairs exactly. `StorySmokeTest` walks all 150 areas proving that balances.

## Testing

There are no asmdefs in this project, so its scripts live in the predefined
`Assembly-CSharp`. Unity gives that assembly NUnit but **not**
`UnityEngine.TestTools`, and an asmdef test assembly cannot reference a
predefined assembly — so `[UnityTest]` and the play-mode runner are unavailable.
Story screens are therefore built and driven directly from editor tests, with
`StoryClock` standing in for the frame loop so a three-second clue card is
verified deterministically instead of slept through.

Two seams exist only for this and should be preserved:

- `StoryObjects.Persist` — `DontDestroyOnLoad`, no-op outside play mode
- `StoryClock` — Unity's unscaled time, overridable by a test

**Never run the screen tests while the Editor is in Play mode.** Several cases
depend on a callback that only fires synchronously outside it — `RepairCutscene.
Play` returns immediately when `!Application.isPlaying`, and in Play mode it
starts a coroutine that the overridden `StoryClock` never advances. So the last
repair of an area never reaches `StoryRepair.OnAreaFinished`, and
`SpendingStarsCompletesTheAreaAndUnlocksTheNext` fails on *"the clue fired on
the last repair"* alone while every other assertion in it passes. Nothing is
wrong with the code.

The tell is in the log, and it is reliable: `AreaScreen.Open` guards its
`[Story] area screen N ...` line with `Application.isPlaying`. **A screen-test
run whose log carries those lines was run in Play mode** — stop Play, then run
it again before believing any failure. A correct run logs
`[Story] Area 1 (Castle Gates) restored.` before it passes.

**Ask the toolbar whether Play mode is on, not the menu.** The log tell above
only helps *after* a void run. Before running the tests, screenshot the Editor
toolbar and look at the Play button: a blue fill means the Editor is playing.
AppleScript is no use here — `AXMenuItemMarkChar` of `Edit > Play Mode > Play`
reads `missing value` whether or not the Editor is playing, so a script that
trusts it will happily run the tests into a void result.

`StoryPowerupTest` launches level 1 on every play session and never finishes it,
so a session that has run it is still "in a level" as far as the story is
concerned. Anything that hides itself during a level — the nav bar first among
them — then stays hidden over a shell screen opened on top of it. **JellyGarden >
Story > Preview > Close The Level** is the way out, and it goes through
`StoryFlow.HandleLevelComplete` rather than inventing a second one.

To look at a screen in Play mode, use **JellyGarden > Story > Preview** rather
than a temporary hook in a runtime script: it opens a screen, grants stars and
resets progress, and it is inert outside Play mode. The temporary hooks that
used to serve this purpose kept riding along into commits — and one did: a
`// TEMP` `yield break` sat at the top of `StoryPowerupTest.Drive` from the
US-007 commit, dead-coding the whole powerup test. Grep for `TEMP` before every
commit. Preview is a **sub-sub-menu**: the AppleScript path is
`JellyGarden > Story > Preview > <item>`, three levels, each clicked open with a
delay before the next.

**Preview > Buy The Next Repair** drives a repair in Play mode without a
synthetic click. It presses the row's actual Do It `Button` — the order guard
and the price check live behind `AreaScreen.OnRepair`, and a preview that
reaches past them into `StoryRepair` is previewing something the player cannot
do. Same rule as pressing the X rather than calling `Close()`.

### Auditing what actually ships

`StoryContentAudit` answers a question neither `validate_story.py` (the files
agree) nor `StorySmokeTest` (all 150 open without throwing) asks: for the areas
that ship, does *every* thing the player looks at resolve — name, goal, each
task with a price and an icon, the room's picture, the clue's picture — and did
anything warn about missing art on the way past.

It logs **one line per area** and a summary, and it is run from three places:
`JellyGarden > Story > Audit Shell Areas`, inside `StorySmokeTest.Check`, and as
the `ShellAreasHaveEverythingTheyNeed` screen-test case. The band is a
parameter; `ShellFirstArea..ShellLastArea` is only today's default.

Two things it relies on, both worth keeping:

- **A missing asset warns and falls back rather than failing**, so the screen
  still draws and nobody notices the arc backdrop standing in for a room. The
  only way to prove art is complete is to read what warned, so `StoryArt` and
  `StoryUi` both expose their warned-key sets (`StoryArt.WarnedKeys`,
  `StoryUi.WarnedSprites`) and the audit clears them first and reports whatever
  the run provoked.
- **A task with no authored art still draws** — `StoryUi.TaskIcon` falls through
  to the glyph for `RepairWorkType.General`, a cog. That is a content gap in a
  shipping area even though every assertion about the row passes, so the audit
  treats General as a problem inside its band.

**A new `RepairWork.Classify` needle must be checked against all 807 labels
before it is added.** `"oil"` would have caught "Fix the boiler" and "Repair the
boilers" and sent them to Polish; the needle that works is `"re-oil"`. The
clauses are ordered, so a needle also has to be checked against every clause
above the one it goes in.

### Walking the band in Play mode

`StoryContentAudit` asks whether the content is there; `StoryShellTest` asks
whether the band can be *played through*. It runs itself once per play session —
no input, like `StoryPowerupTest` — and for each of areas 1-5 it opens the area
page, grants exactly `StoryArea.StarCost`, presses every Do It button in turn,
waits each cutscene out, and checks the area reports restored, the wallet ends
at zero and the next area unlocks. One PASS/FAIL line per area, then a summary.

Four things it has to do that no editor test does, all of them Play-mode facts:

- **It waits for `StoryPowerupTest`.** That test loads the `game` scene and plays
  level 1 on every session. An area screen opened over that measures the
  collision, not the shell. `StoryPowerupTest.IsFinished` is the seam, and it is
  true when nothing is installed so a build without it is not waited on.
- **It presses buttons, it does not call the code behind them.**
  `AreaScreen.RepairButton(i).onClick.Invoke()` — the repair-order guard and the
  price check live in `AreaScreen.OnRepair`, and `onClick` fires whether or not
  the button is interactable, so the row's own `RowCanRepair` is checked first.
- **It advances the dialogue itself.** `DialogueSystem` never auto-advances: one
  tap finishes the line, the next moves on. Every fifth area is an arc beat with
  a scene, so a Play-mode walk of areas 1-5 hits one on area 5 and would hang
  there forever. The walk presses the dialogue's own full-screen `Advance`
  button while `StoryShell.StoryIsPlaying`. `ClueCard` needs none of this — it
  dismisses itself after three seconds.
- **It puts the save back.** The walk needs a fresh save to prove "buys every
  repair", so it copies `StoryProgress.SavePath` aside, `ResetAll()`s, and
  restores the copy in a `finally` — the clue and cutscene flags it set on the
  way through go with it. `yield return` inside `try`/`finally` is legal in a C#
  iterator; `try`/`catch` is not. Restoring the *file* is not the whole job:
  `AreaScreen.Open` and `StoryHomeScreen.Open` both write their area back to
  `StoryProgress.CurrentArea`, so the walk has to come home to
  `StoryProgress.CurrentArea` rather than to `StoryHomeScreen.CurrentRoom` — the
  first run of this moved a bookmark from area 3 to area 2 on its way out.

`StoryShell.StoryIsPlaying` is the one predicate for "a beat is on screen" —
repair cutscene, clue card, arc scene, opening sequence, dialogue. Anything
waiting out a beat should read it rather than assembling its own list.

Re-run it with **JellyGarden > Story > Preview > Run Shell Self-Test**. It takes
around three minutes: 25 repairs at roughly four seconds of cutscene each, plus
five payoffs. A good run ends:

```
[Story] shell test area 1 'Castle Gates': 4 rows, 4 repairs, 4 stars spent, restored, area 2 unlocked. PASS
[Story] shell test area 2 'Outer Courtyard': 5 rows, 5 repairs, 5 stars spent, restored, area 3 unlocked. PASS
[Story] shell test area 3 'The Stables': 5 rows, 5 repairs, 6 stars spent, restored, area 4 unlocked. PASS
[Story] shell test area 4 'Guard House': 5 rows, 5 repairs, 6 stars spent, restored, area 5 unlocked. PASS
[Story] shell test area 5 'Entrance Hall': 6 rows, 6 repairs, 8 stars spent, restored, area 6 unlocked. PASS
[Story] Shell self-test, areas 1-5: 5 area(s), 25 repairs, 29 stars. 5 PASS, 0 FAIL.
```

The 25 repairs and 29 stars are the same two numbers `StoryContentAudit` reports,
which is the point: the audit counts what the story *says* the band costs, and
the walk counts what the screens actually charged. They disagreeing is the
failure worth catching.

## The Royal Match screen kit

`StoryUi` is a **partial class**. The generic builders — canvas, rect, text,
button — stay in `StoryUi.cs`; the Royal Match shapes are in
`StoryUi.Royal.cs`. Add a new screen primitive to the Royal file, not the base
one, so the kit-neutral half stays kit-neutral.

These primitives carry every screen:

| Primitive | What it gives back |
|---|---|
| `RoyalPanel(name, parent, title, onClose)` | `RoyalPanelParts` — `Root`, `Body`, `Content`, `Title`, `Close` |
| `GreenButton` / `DoItButton` | the one affirmative action, with an optional star price |
| `StarBadge(name, parent, cost)` | the ★N a task costs |
| `TopBar(parent, onSettings)` | `TopBarParts`, refreshed by `RefreshTopBar` |
| `CreamCard(name, parent, radius)` | the cream sub-card every section sits on |
| `ProgressBar(name, parent)` | `ProgressBarParts` — `Root`, `Fill`, `Label`, `Fraction`; pointed at a count by `SetProgress(parts, done, total)` |
| `ChestIcon(name, parent[, panel])` | the chest that ends a progress bar — the painted `icon_chest` where it is on disk, else a drawn purple one whose inner faces `panel` recolours |
| `GoldButton(name, parent, onClick)` | the gold secondary card; fill it via `ContentOf(button)` |
| `CountBadge(name, parent, count)` | the red call-to-action badge, written by `SetCountBadge` |
| `TaskIcon(name, parent, areaId, task)` | the object on the left of a task row |
| `NavBar(name, parent, icons, labels, raised, onTab)` | `NavBarParts` — the bottom strip and its `NavTabParts[]`; lit by `SetNavSelected` |

Rules that are easy to get wrong:

- **Colours are sampled, not chosen.** Every `Royal*` colour came out of
  `docs/royal-match/*.jpg` with PIL. Match a new one the same way rather than
  eyeballing it, and say which file it came from.
- **A builder hands its parts back as an object.** Nothing looks a child up by
  name afterwards, so renaming a child cannot silently break a screen. Keep it
  that way.
- **A rim is two nested rects, not an `Outline`.** `Outline` offsets the whole
  quad and leaves the corners square; a gold rim needs a rounded rect behind a
  smaller rounded rect.
- **A close X is two rotated bars, not a glyph.** The kit's font may not carry
  ✕, and a missing glyph renders as nothing — which reads as a dead button.
- **`SetGreenInteractable`, not `SetInteractable`.** The green button is three
  layers (base, face, gloss); the base-class helper only recolours one of them.
- Lives read **"Full"** while `StoryFreeGame.Enabled`; coins are the kit's
  `InitScript.Gems`, since the kit has no coin currency at all.
- **A progress bar's fill is anchored, not sized.** `SetProgress` moves
  `Fill.anchorMax.x` and hides the fill entirely at zero — a rounded rect of
  zero width still draws both end caps, which reads as one step already done.
- **Text drawn over the room artwork needs an `Outline`.** The art behind it is
  of any brightness, so the letters carry their own contrast.
- **Anything built before `AreaSceneView.Attach` is drawn under the artwork.**
  The view is a later sibling. `SetAsLastSibling()` the captions after every
  attach, or they vanish behind the room.
- **`AreaSceneView` fits its parent two ways, and more than one may be alive.**
  `Attach(parent, areaId)` letterboxes — the whole painting, bars where it does
  not reach, which is what the area screen and the cutscene stage want.
  `Attach(parent, areaId, true)` covers: an `AspectRatioFitter` in
  `EnvelopeParent` scales the stack until it fills both directions and the
  overflow is cropped, so the room bleeds to every edge. **A covering parent
  must carry a `RectMask2D`** or the painting draws outside the screen it
  belongs to. Views are held in a list, not a single static, because home and
  the area screen can show the same room at once and a repair has to reveal on
  both.
- **A flag is not a geometry proof.** `IsCovering` and `Aspect` being right says
  nothing about whether anything actually filled the screen, and an editor test
  asserting them passes over a view that letterboxes. Prove a full-bleed claim
  off the pixels: screenshot the Game view, measure the painted rect, and
  compare it against the centre crop the cover *should* produce (compute it from
  the source jpg with PIL). Same trap as the negative-height rect.
- **Per-task art does not exist, so an icon is classified, not looked up.**
  `StoryUi.TaskIcon` draws one of ten glyphs chosen by
  `RepairWork.Classify(label)` — the same classifier the repair cutscene uses,
  so a lantern task gets a lantern here and a flame there. It checks
  `StoryArt.TaskIcon(areaId, taskIndex)` first, so dropping
  `Resources/Story/Art/task_012_3.png` in replaces the glyph with no code
  change. Add a glyph by adding a `RepairWorkType`, never by special-casing a
  label. That lookup is the one in `StoryArt` that does **not** warn on a miss:
  807 tasks with no art would be 807 lines of log.
- **A value baked in at build time must be rebuilt when the area changes, not
  only when the row count does.** A row's star price and icon are fixed when the
  row is created, so `AreaScreen` keys its rebuild on `_rowsBuiltFor`; two areas
  of equal length would otherwise wear each other's prices.
- **A greyed button still fires `onClick.Invoke()`.** `interactable` is a UI
  state, not a guard. Now that every unfinished row keeps its Do It button, the
  story's repair order is enforced inside `AreaScreen.OnRepair` against
  `StoryProgress.NextTask`, not by which buttons happen to exist.
- **A drawn icon needs its interior to differ from what it is drawn on.**
  (Historical: `ChestIcon` now prefers painted art, and this bit it before.)
  The drawn chest is a purple frame around gold panels, which reads on the area
  page's cream card and collapses to two empty purple rings on the home
  screen's gold one — the panels *were* the card. That is why it takes a
  `panel` colour. Any primitive made of nested flat rects has this failure
  mode, and the editor tests cannot see it: every rect is present, sized and
  coloured exactly as asked.
- **A best-fit `Text` wraps before it shrinks.** Narrow a label's rect and a
  two-word name becomes two lines that overflow the top of its card rather than
  a smaller one line. Give a variable-length label the full width it can have —
  and remember the 150 area names run from "Castle Gates" to far longer.

The primitives are proven by `StoryScreenTests.RoyalPrimitivesConstruct`, which
builds each one for real and logs a `[Story] primitive ...` line per shape. With
the Editor open, batchmode is locked out — run it from the menu instead:
**JellyGarden > Story > Run Screen Tests**, then read `Logs/Editor.log`.

## The home screen

`StoryHomeScreen` is the room, the top bar over it, and one band of buttons over
the floor. Its layout constants are worth understanding before moving anything:

- **Vertical metrics are fractions of the canvas, horizontal ones are reference
  points.** The band is `164f / 1472f` of the canvas height because the capture
  is a 9:19.5 phone and the Editor's Game view is 4:3 — a canvas at match 0.5
  hands the band ~1663 reference units of height there rather than 1920. Every
  fixed-pixel column that fits one overlaps on the other, and every editor test
  still passes.
- **The two buttons are `GreenButton` + `GoldButton`, split by measured
  fractions of the band** (`LevelRightFrac`, `AreaLeftFrac`) — the gold card is
  the wider of the two, as in the reference.
- **`NavBand` is laid out and empty**, waiting for US-010. Fill the rect; do not
  re-cut the screen.
- The Level button opens `StoryLevelPopup` and the area card opens
  `AreaScreen`, both of which sort **above** home (600 and 400 against 300), so
  home stays open underneath and the popup's `onClose` only has to refresh it.
- `StoryHomeScreen.AffordableRepairs(areaId)` is the red badge's number: it
  walks the area's tasks **in the story's order** spending one pool of stars and
  stops at the first it cannot pay for. Counting every affordable task instead
  would point the player at buttons that grey out when pressed, because
  `AreaScreen` only ever offers `StoryProgress.NextTask`.

## The shell

`StoryShell` is what makes the screens a game rather than five screens: it
decides what is on screen whenever a level is not, and it is the only thing that
knows the kit's map exists.

- **`StoryShell.UseMap` is the whole switch.** False — the default — and the game
  opens on the room, with the map put away. True and the kit's map is the entry
  point again, `StoryShell` stops touching anything, and a level ends on the area
  page as it did before the shell existed. That flag is what "the map is left in
  place" has to mean: the map is not deleted, not emptied and not edited, and one
  field puts it back.
- **The map is deactivated, never left running underneath.** Each of its 150
  nodes polls `Physics2D.OverlapPoint` in `Update`, and a node tapped through a
  gap in the shell would launch a level from a screen the player cannot see.
- **Hiding it is a per-frame assertion, not a one-off.** `LevelManager.ShowMap`
  turns the map back on at the end of every level, so the shell puts it away
  again — the play log carries `kit map put away` once per level, which is right.
- **Nothing is hidden before the kit's own `Start` has run.** `InitScript` reads a
  sprite off the map with `FindObjectOfType`, which skips inactive objects and
  would throw. `Install` runs at `sceneLoaded`, when `LevelManager.THIS` is still
  null, so the first pass is deliberately a no-op and `Update` does the work.
  `StoryMapRefresh.Request` is guarded for the same reason: `LevelsMap.Reset`
  takes `Max()` of `FindObjectsOfType<MapLevel>()`, which is an exception rather
  than a no-op on a map that is off.
- **The shell comes down for a level.** Every shell canvas sorts at 300..600 and
  the kit's level canvas does not, so a screen left open is a screen drawn over
  the board. `StoryFlow.LaunchLevel` calls `CloseForLevel` directly rather than
  waiting for `Update` to notice a frame later.
- **A level ends on home.** `StoryFlow.OpenAfterLevel` asks
  `StoryShell.ShowAfterLevel` rather than opening the area page itself. The star
  just earned is already counted on home's red badge and the area page is one tap
  away on the gold card.
- **`EnsureFloor` is called on every close path, not trusted to the caller.**
  Each screen closes onto whatever opened it, which is right while the player
  walks forwards — but the area page can also be opened by a level ending, by the
  preview menu and by a map node, and a red X with nothing underneath it is a
  black screen with a nav bar. It is a no-op when anything is open, and it
  deliberately does nothing while a cutscene is up: the beat IS the screen.
- **`InGameSceneSource` is the test seam**, in the shape of
  `StoryNavBar.LevelIsPlayingSource`: an editor test has neither a board nor a map
  in the scene and has to say the shell is home. It also keeps `EnsureFloor` out
  of the cases that are not about the shell — null it in a `finally`, because
  domain reload is off.

## The signs on the rope

`StoryNavBar` is the one piece of the shell that is not a screen. It used to be
Royal Match's five-tab blue bar; it is now a rope strung across the bottom of
the screen with a wooden board hung from it per destination.

**There are two boards: Areas and Home.** That is the whole of what the game
has. The bar it replaced had five slots and three of them answered a tap with an
apology — Team is not in this game at all, Leaderboard and Cards are not built —
and three dead buttons made the two real ones harder to find, not easier.
Adding a third is adding a line to `Icons` and `Labels`; the boards share the
width evenly, so nothing else moves.

- **There is no bar body any more.** The room runs to the bottom of the screen
  and the boards hang on it. A tap between two boards falls through, which is
  correct — there is nothing there to press. It also means the artwork is
  visible where the bar used to be opaque.
- **Selection is height and colour, not size.** The chosen board hangs higher
  (its hook shortens by the same amount, so the rope does not move) and turns
  gold; the others are wood. Every board is the same size. `SetNavSelected`
  drives all of it through `SetNavTabRaised`.
- **The band is 218 of 1472, not 145.** A flat strip needed 145; a rope with
  hook, board and word stacked on it does not. At the old height the rope came
  out three pixels tall and vanished into a dark room. Every screen reserves the
  band by reading `StoryUi.NavBarHeightFrac`, so they all gave way together.
- **The rope carries its own outline.** Without one it disappears into a dark
  room; with only an outline it would disappear into a bright one. Three bands:
  edge, rope, highlight.
- It still lives on its own persistent canvas at sorting order 500 — above home
  (300) and the area page (400), below the level popup (600) — because the area
  page shows the same rope and there must not be two of them.
  `StoryHomeScreen.NavBand` is a RESERVATION and takes its height from the same
  constant.
- **`LevelIsPlaying` reads `LevelManager.THIS.gameStatus`, not
  `MenuManager.menuPlay`.** `menuPlay` is the kit's PRE-play popup, shown from
  the map before a level starts, so a bar that hides on it hides in the wrong
  place and shows in the wrong one. `StoryFlow.PlayingArea` is checked first.
- **`StoryNavBar.AreasDestination` is the seam `StoryAreasList.Install` sets** to
  point Areas at the list of all 150 rooms; null is the area the player is
  restoring, which is what a test that has not installed the list gets.
  `LevelIsPlayingSource` is the other seam, so a test can put the shell "in a
  level" with no board in the scene.
- The rope is driven by `Update`, because a level starts and ends without
  telling it. An editor test has no frame loop, so it calls `StoryNavBar.Sync()`.
  Hiding turns off the CANVAS, never the component's own object — deactivating
  the host would stop `Update` and it could never come back.

## The areas list

`StoryAreasList` is the Areas tab's destination: 150 room cards in a vertical
scroll on a dark slate ground, per `docs/royal-match/04-areas-list.jpg`. Sorting
order 350 — above home (300), below the area page (400), so a card opens the area
page ON TOP of the list and `AreaScreen`'s close X drops the player back here.

- **A card is built from `StoryDatabase` and `StoryProgress` alone.** No prefab
  per area, no per-area code path: area 150 renders exactly as area 1 does, and
  adding an area to the story adds a card.
- **The cards are built ONCE and only refreshed.** A card's shape never changes —
  only its numbers, whether it is locked, and whether it is the current room. 150
  cards is the most expensive thing the shell builds; rebuilding them on every
  repair is not affordable.
- **Artwork is loaded only for the cards near the viewport.** There are 31 room
  paintings on disk at roughly half a megabyte each, and resolving all 150 cards
  at once would decompress every one of them to open a menu. `RefreshArt` runs on
  `ScrollRect.onValueChanged` and gives one entry of slack in each direction.
  `StoryArt`'s own cache never releases, so this bounds what a single OPEN costs,
  not what a long scroll eventually costs.
- **A card's proportions come out of its WIDTH.** The list scrolls, so an entry is
  a fixed block of reference points (983 tall, an 888-point card in it) rather
  than a fraction of the screen — which is the opposite of the rule for a
  full-screen column, and for the same reason: the thing that must not distort is
  the card, and the viewport simply shows however many fit.
- **The list runs from area 150 down to area 1, and opens scrolled to the
  player's own room.** Royal Match reads downward into the past and can put the
  newest card at the top because it does not draw locked areas at all. This one
  does draw them, so opening at the top would be 145 padlocks before the first
  pressable card; `ScrollTo` puts the frontier at the top of the viewport
  instead, which restores the reference's reading without hiding the road ahead.
- **A locked card is drawn, greyed and padlocked, never hidden**, and `OnCard`
  enforces that itself — `onClick` fires on a greyed button. Grey the ribbon's
  FACE as well as the ribbon: the rim covers only its outer few points, so tinting
  the outer image alone leaves a locked area wearing a bright red name plate.
- `StoryAreasList.Destination` is the test seam — where a card goes — in the shape
  of `StoryLevelPopup.Launcher`. `StoryAreasList.Install()`, called once per scene
  by `StorySceneHooks`, is what points the nav bar's Areas tab here.

## The in-level HUD

`StoryLevelHud` draws Royal Match's Target + portrait + Moves strip over the
kit's score bar. Four things about it are not obvious:

- **The board does not exist when a scene finishes loading.** `LevelManager`
  assigns `THIS` in `Start`, and the level UI is inactive until a level opens,
  so a `StorySceneHooks` pass finds nothing. Anything that needs the board
  attaches on `LevelManager.OnLevelLoaded` / `OnStartPlay` instead — the HUD and
  `StoryBoardTheme` both do.
- **Never deactivate the kit's `Targets` group.** `TargetsGroup.OnDisable`
  destroys the icons it made, and `LevelManager.CheckCollectedTarget` flies a
  matched piece to `TargetObject.guiObj` — one of those icons. Deactivating it
  animates to a destroyed transform. Hide it with a `CanvasGroup` at alpha 0
  instead, which is also why the HUD moves those invisible icons onto its own
  slots: the kit's collect animation then lands where the player is reading.
- **`Level/Canvas/BottomPanel` draws over `Level/Canvas/Panel`.** A screen built
  under `Panel` cannot put anything on top of the booster row, which is why the
  kit's pause button is moved *into* that row rather than drawn over it.
- The HUD is a child of the kit's canvas rather than an overlay canvas of its
  own, so it inherits the kit's sort order and visibility for free. That canvas
  is authored at 720x1280 and the Royal metrics are in 1080, so every vertical
  measurement is multiplied by `referenceResolution.x / 1080`; horizontal ones
  are normalised anchors and need no scaling at all.
- **Every level carries a `Target.SCORE` entry, and it is a real win
  condition.** `LevelManager.CheckWinLose` requires `targetObject.All(Done)`,
  and the score entry's `Done()` is `Score >= star1`. So a score slot is never
  chrome to be hidden — on all 1209 levels it is a goal the player must reach,
  and on the 176 `MODE 0` levels it is the *only* goal. The HUD draws it as a
  target that reads `have/need` against `star1`; the strip still never uses the
  word "score". `StoryLevelHud.ReadLevel(level, score)` builds the same model
  from `Resources/Levels/N.txt` plus `Resources/Targets/LevelN.asset` with no
  board in the scene — use it for any screen that has to state a level's goal.
- **The target plate is shared out by weight, not evenly.** `SlotWeight` gives a
  score target 2 and everything else 1, because a score target reads
  `940/1000` and a collect one reads `3`. Split evenly, a three-target level gave
  the score an eighth of the width it needed and `resizeTextForBestFit` shrank it
  to its floor — a chip nobody can read at 393 points wide, sitting beside two
  single digits at full size. Levels 12, 13 and 14 are where it showed.
- **Never reorder the target slots.** `AimKitIconsAtThisHud` matches the kit's
  `TargetIcon`s to this HUD's slots by index, so the model must keep
  `LevelManager.targetObject` order or the collect animation flies to the wrong
  icon. The asset order varies by level (`('0','3')` and `('3','0')` both occur).
- **Wrapping off in a wide box, on in a narrow one.** `StoryUi.FitOneLine` is
  what makes best-fit solve for WIDTH, and the Moves/Time card needed it: sized
  for "19", a TIME level's "0:59" wrapped to "0:5" over "9". The same call on a
  target chip made it worse — a chip is a third of a 393-point plate, and with
  wrapping off best-fit stops shrinking and the number runs into its neighbour
  (`25 350/1000` where `25 35 0/1000` belongs). Both are in `StoryLevelHud` and
  both say why.
- **A target icon is the kit's artwork and some of it is nearly white.**
  `Textures/Blocks/block.png`, the icon on every BLOCKS level, is a pale ice tile
  at 0.77 luminance on a `RoyalCream` card at 0.92 — a blank box with a number
  beside it. Every chip icon therefore casts a silhouette of its own sprite one
  step down and right (`IconShadowStep`, navy at 0.55), which gives a pale icon
  an edge and a dark one depth, without editing a single kit texture. Any new
  chip artwork gets this for free; do not solve it per icon.

## The board can run out of moves and the kit can miss it

`StoryDeadBoard` is the last resort behind the kit's reshuffle, and it is worth
knowing why it has to exist.

`AI.CheckPossibleCombines` is the kit's own answer: it scans for a hint and, if
it finds none, calls `LevelManager.NoMatches()`, which shows "no more matches"
and regenerates. That scan is a heuristic — a fixed list of L, T and line shapes,
one colour at a time — and the coroutine running it is **started from six places,
never stopped, gates both its work and its own re-arming on `DragBlocked`, and
shares one `gotTip` field between however many copies are in flight.** When it
loses that race nothing else asks the question, and the player is left with a
board that has no move on it and no way to end the level. Level 31 sat like that
for twelve seconds with its score goal already met.

- **Ask the question with the kit's own matcher.** `Square.FindMatchesAround` is
  what decides whether a real swap scores, so the watchdog swaps two items, asks
  it, and swaps them back — the same shape `StoryPlaytest.TryDirection` uses.
  Anything that invents its own definition of a legal move can disagree with the
  game about whether the board is dead.
- **Answer it three times before acting.** A board mid-cascade has falling items,
  and a falling item is not a legal swap partner, so a settling board reads as a
  dead one for a frame or two.
- **Recover through the kit, not around it.** It calls `NoMatches()` — the kit's
  own path, with the kit's own banner and regeneration. The AI still runs, still
  hints, and still reshuffles whenever it gets there first.
- **A reshuffle is the middle of a level, not the end of one.** `RegenLevel` is a
  legitimate state and anything watching a level has to know the word:
  `StoryPlaytest` treated every state but Playing as an ending and reported the
  first reshuffle it ever saw as "ended in state Playing, which is neither won
  nor lost", stopping the level dead.

## The two end panels

`StoryWinPopup` and `StoryLosePopup` are one design in two states, and they are
deliberately the same file twice: the same band, the same star slot, the same
cream area card, the same green button, the same fractions. A win and a loss are
the same moment in the game's shape, and reading them as two screens is how one
ends up feeling borrowed.

What differs is what the game differs in. The win panel's star is white (gold
artwork undimmed) and captioned `+1 Star`, or `RoyalGrey` and `Already beaten`
on a replay. The lose panel's is always `RoyalGrey`, captioned `Out of moves` or
`Out of time` from `LevelManager.limitType`.

- **`StarSprite` is gold artwork and `Image.color` MULTIPLIES it.** There is no
  tint that makes it blue, grey or neutral: `RoyalBlueDark` was tried, to read as
  an empty socket in the plate, and came out a dark green blob that looked like a
  bug. Grey over gold is bronze, which is what an unearned star has looked like
  in this genre for twenty years.
- **Both panels are closed by `StoryFlow.ReturnToArea`, not only by their own
  buttons.** Continue is one door out of a level; a retry, a beat that leaves
  early and the playtest are others, and a panel closed only by its button
  survived all of them — including the next `LaunchLevel`, so from the second
  level of a run the player was reading the previous level's result over a live
  board. `ReturnToArea` also switches off the kit's `MenuComplete`, `MenuFailed`
  and `PreFailed` for the same reason.
- **The area row names its unit.** "Castle Gates / 0/3" two rows under "+1 Star"
  reads as stars in that area, and it counts repairs. The track says
  `0/3 repairs` on both panels; the bar keeps the bare count on the home screen
  and the area page, where the word is already beside it.

## The level start popup

`StoryLevelPopup.Open(level, areaId, onClose)` is the "Level N / Goal / Play"
card. It is opened from the area screen's Play button, and `onClose` is what its
red X goes back to — pass the screen that opened it, or the player lands nowhere.

- **Its goal comes from `StoryLevelHud.ReadLevel(level, 0)`,** not from a second
  reader. At score zero every `HudTarget.Remaining` is the whole of what the
  level asks for: the pass mark on the score target, the count on a collect one.
  The popup and the in-level HUD therefore cannot disagree about a level.
- **No move count belongs on it.** Royal Match states the goal and nothing else
  before the board opens; a number to fear is the board's job to reveal. The
  editor test asserts the popup never says "move", "time" or "score".
- **`StoryLevelPopup.Launcher` is the test seam** — the same shape as
  `StoryLevelHud.Source`. Null means `StoryFlow.LaunchLevel`, which is the only
  thing it ever is in the game; a test swaps it to prove Play launches the right
  level for the right area with no board in the scene.
- The scrim behind it eats taps but does not close. The X is the only way out,
  so a tap that misses Play cannot lose the player their place.

### The booster row

`StoryBoosters` owns the three boosters a player may take into a level and the
choice they made on the popup.

- **The three are the kit's own pre-level boosts**, not an invention:
  `LevelManager.SetPreBoosts` reads `BoostStriped`, `BoostPackage` and
  `BoostColorfullBomb` after the board is generated and converts that many random
  items. `StoryBoosters.AmountOf` matches the kit's own `BoostIcon` (5, 5, 1) so
  a booster does the same thing wherever it was picked.
- **The popup runs in the map scene, where `LevelManager.THIS` does not exist.**
  The choice is therefore held in a static and written on
  `LevelManager.OnLevelLoaded`, which `ReGenLevel` fires before the
  `RegenMatches` coroutine reaches `SetPreBoosts`. The kit then spends the
  booster itself through `InitScript.SpendBoost` — nothing in `LevelManager` is
  edited, three public fields are written from outside.
- `Apply` clears the selection, because the "no more matches" regeneration fires
  `OnLevelLoaded` again and would otherwise hand out a second set.
- **Anything the popup offers must be earnable.** `StoryFreeGame.Grantable`
  carries all three, and `GrantStarterPreBoosters` gives one of each once —
  without that, two of the three slots would be locked forever, since nothing in
  this game is for sale.
- `StoryBoosters.OwnedSource` is the counts seam, in the shape of
  `StoryLevelHud.Source`: null is PlayerPrefs, and a test swaps it rather than
  writing the player's save.

**Vertical metrics on a story screen are fractions, not pixels.** The reference
captures are a 9:19.5 phone; the Editor's Game view is 4:3. A `ScaleWithScreenSize`
canvas at `matchWidthOrHeight 0.5` hands a 4:3 view far fewer reference units of
height than of width — 1663 rather than 1920 at 768x1024 — so a column of fixed
pixel heights that fits the phone silently overlaps everywhere else. That is how
the booster row first landed underneath the Play button while every editor test
passed. Anchor vertical positions as fractions of the parent (see
`StoryLevelPopup`'s `DesignHeight` block), keep horizontal measurements in
reference points, and give any text in a box that can shrink
`resizeTextForBestFit`.

**A stretched rect can have negative height, and a negative rect draws nothing.**
`RoyalPanel`'s title was inset 110px on all four sides of a 210px-tall plate, so
every panel ever built had an invisible title — which reads as a panel that
forgot its own name, not as a bug. Inset a title horizontally (to clear the
close X) and only slightly vertically, and assert
`rectTransform.rect.height > 0` after `Canvas.ForceUpdateCanvases()` rather than
trusting that `Text.text` being right means the text is on screen.

## Two traps that have bitten more than once

**`System.*` resolves into the kit's namespace.** The kit has
`JellyGarden.Scripts.System`, so inside `JellyGarden.Scripts.Story` a bare
`System.Random` or `System.Text.StringBuilder` fails to compile. Write
`global::System.Random`, or add a `using`.

**`RuntimeInitializeOnLoadMethod` runs once, in the startup scene.** This game
opens on `main` and loads `game` afterwards, so anything needing the map or the
board found nothing and silently did nothing. Per-scene setup belongs in
`StorySceneHooks`, which subscribes to `SceneManager.sceneLoaded`; only
once-per-process work belongs in `StoryBoot`.

## Typecheck

Unity writes an explicit `<Compile Include>` per file, so a newly added script is
**not typechecked** by the generated csproj. Use the globbed one:

```bash
python3 tools/make_typecheck_csproj.py
dotnet build Story.Typecheck.csproj -v q --nologo
```

## Dressing the board (StoryBoardTheme, StoryThemeTest)

`StoryBoardTheme` puts the arc's picture behind the board and the arc's colour
under its cells. `StoryThemeTest` (`-storythemetest`, run by
`tools/theme_sweep.py`) opens the first level of each of the six arcs in its own
area and photographs it. Four things it learned the hard way, all of which cost
a run each:

- **The fallback is silent by design, so it has to be reported.**
  `ApplyBackdrop` falls back to a kit picture when the story art is missing —
  correct, a level must never fail to open — but that makes "arc 6 is wearing
  the kit's default" invisible in a screenshot of a board that looks fine.
  `StoryBoardTheme.LastBackdrop` / `LastBackdropWasKitDefault` / `LastFill` /
  `LastFilledCells` exist for a test to read, and the fallback now warns once
  per theme key naming `Story/Art/<key>`.
- **A board can be live, themed and completely hidden.** The first run of the
  theme test reported six correct themes and photographed one board and five
  copies of the opening cutscene: leaving a level drops the player on home, home
  is where the story's beats fire, and a beat waits for a tap forever. Anything
  that photographs a board must `WaitForCalm` — poll `StoryShell.StoryIsPlaying`
  and press the dialogue's own `Advance` button, the same shape as
  `StoryShellTest.PressOnThroughDialogue`.
- **Ask the screen, not the scene graph.** Every check about arc 6 passed —
  right theme, right backdrop sprite, 84 cells carrying 236,157,108 — over a
  frame that was 87% black. Only reading the pixels caught it. The cause was the
  kit's rate-us prompt (see `StoryFreeGame.RatePromptAllowed`).
- **Hide the kit tutorial before shooting level 1.** `TutorialManager` dims the
  screen and draws a hand over the board on `currentLevel == 1`, which is arc
  1's first level.

## Playing levels (StoryPlaytest)

`StoryPlaytest` opens a level, finds a legal swap, makes it, waits for the
cascade and repeats until the level ends. It is the only thing here that finds a
coroutine truncated mid-cascade, because that fault leaves the board looking
fine.

**Run it from the Editor already open**, never batchmode — batchmode wants the
project lock the Editor holds. `King Leo > Preview > Playtest the band file`
plays the comma-separated level list in `ralph/playtest_band.txt` and leaves the
Editor open. `StoryPlaytestRunner.RunHeadless` is the batchmode path and **quits
the Editor** as soon as the report lands; never wire a menu item to it.

The report is written to `Application.persistentDataPath/playtest.txt` —
`~/Library/Application Support/DefaultCompany/King Leo/` on this machine — and
ends with one `SUMMARY:` line. Poll for the file; do not read it out of the log.

**Every `FINDING:` names its level.** `Finding()` prefixes `"level N: "` from
`_level`, which `Drive` sets per level, so a call site writes only what went
wrong. Do not spell the level number out again in the message.

**A level the bot cannot beat is a result, not a fault.** GameOver/PreFailed is
`RESULT: lost`, counted, and the run goes on. Only a state that is neither won
nor lost is a finding.

**`Ending` waits out BOTH pre-states before it shoots.** PreWinAnimations was
always waited on; PreFailed was not, so every lost level's end shot was taken
while the kit's fail banner was still sliding and never showed the end panel at
all. A level that never leaves either pre-state is its own finding: it is a
level that cannot end.

**`Shot` hides the kit tutorial first**, the same three lines `StoryThemeTest`
has. Without it every shot of a level the kit decides to teach is a picture of a
dimmed screen with a hand on it.

**Two PNGs per level, in `Application.persistentDataPath/playtest/`**, named
`0001_level_1_start.png` / `..._end.png` so the directory sorts into playing
order. The start shot is taken *before* the playable check, so a level that
never draws a board still leaves a picture of what the player saw. A visual
fault — a board off centre, a HUD over it, a tile with no sprite — raises no
error and ends no level, so a text-only sweep finds only the faults that throw.

**A shot needs a frame to be the end of.** `WaitForEndOfFrame` never returns
under `-nographics`, so a capture there hangs the whole sweep. `OpenShotDir`
turns screenshots off when `SystemInfo.graphicsDeviceType` is `Null`, and the
report says so on its `screenshots` line.

## The window (StoryWindow)

**The player is always windowed at 393x852 — an iPhone 15/16 in portrait — and
never full screen.** A full-screen sweep takes over the display for as long as
it runs, and its screenshots come out the shape of this desktop rather than the
shape of the phone the game ships on, which is the wrong aspect to judge any
layout in.

`tools/run_player.sh` is the only thing that launches the game. It holds the
window flags — not full screen, 393 wide, 852 tall — and passes anything else
through, so `tools/run_player.sh -storyplaytest` is a playtest and
`tools/run_player.sh -storydemo` is the demo walk. Never paste the flags into a
new script.

`StoryWindow.Apply()`, called from `StoryBoot`, is the braces to that belt: a
desktop player opened by double-clicking it takes whatever Unity last saved in
the player prefs, so Apply puts it back into a window. It is a no-op in the
Editor and on a phone, where full screen is all there is.

**Every screenshot goes through `StoryWindow.Capture`, and it reads the back
buffer at the window's own size — 393x852.** It used to render the frame again
at superSize 2, for the good reason that 393 points is small to read a target
chip in. That cost far more than it bought: the re-render drops the Screen Space
- Camera canvas, and in this game that canvas carries **the whole level HUD and
the arc's backdrop**. Every shot the project took before 2026-09-07 has no
Target plate, no Moves counter, no target chips, and a smear where the room
should be — and two entries in `ralph/findings.md` ("no HUD is drawn over the
board", "the room behind the board is a dark blur") were written from one.
`StoryWindow.CaptureSuperSized` is the old behaviour, kept and unused.

The lesson generalises past this one call: **a screenshot is a measurement, and
this one was wrong for four commits while every check about the HUD passed.**
When a picture and a check disagree, suspect the picture too. `StoryThemeTest`
now reads the frame with `ReadPixels` and reports its mean luminance against the
ground the theme asked for, which is the check that finally caught a board drawn
under a black scrim.

The Editor's Game view is not 393x852, so measure a layout off a player run.

```bash
python3 tools/check_launch_args.py
```

fails if `run_player.sh` and `StoryWindow.cs` disagree about the size, if any
other file spells the flags out or opens the `.app` itself, or if a screenshot
is taken by anything but `StoryWindow.Capture`.

**`StoryBuild`'s menu items no longer quit the Editor.** Every method there is
both a `-executeMethod` entry point and a `[MenuItem]`; `EditorApplication.Exit`
now runs through `Finish()`, which returns instead when
`Application.isBatchMode` is false. Clicking `JellyGarden > Story > Build macOS
Player` closed the Editor the moment the build succeeded. Anything else that is
both a menu item and a batchmode entry point needs the same guard.
