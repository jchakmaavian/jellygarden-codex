# Adapting the game to the story

The kit is a candy game with candy rules. The story is about a king putting his
castle back together. This is the plan to make the second true — in the rules as
well as the art — without damaging the two things that make the first worth
keeping.

Scope decisions already made, which shape everything below:

- **The board plays like Royal Match.** One star per level. Universal powerups,
  not per-colour ones. Tap a powerup to fire it where it sits. Swipe it and it
  fires where it lands. See Phase 1.
- **Audio is left exactly as it is.** No new music, no new voice. See *Deferred*.
- **Cutscenes get a placeholder everywhere.** Real content is generated later, by
  you. See Phase 4.

---

## The two things that must not regress

Not preferences. They are the reason this project is worth finishing rather than
rebuilding.

### 1. The feel — animation, particles, VFX

The kit's match, cascade, explosion, swap-refusal and idle animations are the
game's whole tactile quality. **No change deletes, renames or re-authors an
animation clip, a particle system, a material or a prefab.**

Enforced, not asserted:

```bash
python3 tools/check_vfx_intact.py
```

Phase 1 is the first phase that edits the board scripts themselves, so the rule
gets sharper there. `Item.cs`, `Square.cs`, `LevelManager.cs`,
`GUI/AnimationManager.cs`, `ItemAnimEvents.cs` and `ParticleSorting.cs` are
checked for **method loss and line-count shrink**. Every change to them is an
addition: new methods, new branches, new call sites. Nothing is deleted, and a
behaviour that is being replaced is bypassed rather than cut out.

**The consequence that keeps catching people out:** the six board colours cannot
be reordered. `Textures/effects/explosion_item_01..06` are tinted per item
*index*. Move green to slot 1 and green pieces shatter into red sparks.

### 2. The levels — 1209 boards, tuned

| | |
|---|---|
| boards | 1209 (`Resources/Levels/1..1209.txt`, 9.4 MB) |
| targets | 1209 (`Resources/Targets/Level1..1209.asset`, 9.5 MB) |
| derived from | the kit's 100 shipped, hand-tuned boards |
| difficulty curve | mapped area-by-area in `Resources/Story/level_plan.json` |
| sabotaged boards | 665, from area 19 on |

**Finished data, not build output.** The generator is deterministic — every
mutation is seeded on the level number — so re-running reproduces the files
exactly. That is a safety property, not an invitation.

> **Do not re-run `level_gen.py` or `target_gen.py`.** If a level ever needs to
> change, edit the one `.txt` by hand and say so in the commit. The story bends
> to fit the levels. The levels do not move.

Phase 1 changes the *rules* the levels are played under. It does not change a
single board. See *Consequences to accept*.

---

## Where the game actually is

Built, committed, passing its suites: 150 areas, 807 repair tasks, 1209 stars,
six arcs; area screen, repair flow, clue cards, 30 arc-beat cutscenes, opening;
a working animated repair cutscene with 10 work types classified from each
task's own verbs (93% specific, 7% generic); the map grown from 100 nodes to
150 with arc banners; per-arc board backdrop and cell tint; the sabotage beat at
area 19; free-play economy; atomic save with per-area star pools.

What is not true yet: the board still plays by candy rules and looks like candy.

---

## Phases

### Phase 0 — Freeze and prove

Nothing else starts until the protected things are protected by a check rather
than by care.

1. **Level manifest.** `tools/levels.sha256`, one line per level and target
   file, plus `tools/check_levels_frozen.py` called wherever
   `check_vfx_intact.py` is.
2. **Prove the win path.** `StoryFlow.HandleWin` awards the star on
   `LevelManager.OnWin`, and it is verified **headless only** — nobody has
   played a board to a win in a real player and watched a star land. Do it by
   hand, record it. This matters more now: Phase 1 rewrites what winning means.
3. **Capture a difficulty baseline.** Play a fixed sample — say levels 1, 22,
   150, 400, 800, 1209 — and record moves used and score, so Phase 1's effect on
   difficulty is measurable rather than argued about.

**Risk:** none. This phase only adds checks.

---

### Phase 1 — Royal Match rules

The largest change in the plan, and the only one that edits the protected board
scripts. Four separate rules; each is independently shippable and independently
revertable.

#### 1.1 One star per level

**This is already half true.** `StoryFlow.HandleWin` awards exactly one star,
once, on the first win — replaying a beaten level pays nothing. The story
economy has always been one star per level, 1209 stars across 150 areas.

What contradicts it is the *kit's own* three-star score system running beside
it: `LevelManager.CheckStars()` fills `stars` to 1/2/3 as `Score` crosses
`star1`/`star2`/`star3`, writes `Level.NNN.StarsCount` to PlayerPrefs, and the
HUD draws a three-star progress bar across the top of the board. Two different
things called "star", one of which the story ignores.

The work:

- Collapse the HUD to a **single goal bar with one star** at its end.
- Stop the three-star PlayerPrefs from being a parallel currency.
- **Keep `star1`.** It is not just a display threshold — for SCORE-mode levels
  it is the pass mark. Removing all three would leave those levels with no win
  condition. `star2` and `star3` stop being shown and stop being stored.
- The end-of-level panel says *one* star earned, matching what the area screen
  then shows.

**Acceptance:** every level that can be won awards exactly one star; no screen
shows a 2-of-3 or 3-of-3 rating; `validate_story.py` still reports 1209 stars.

#### 1.2 Universal powerups

Today a special is per colour. `Item.cs` picks
`stripedItems[color].horizontal`, `stripedItems[color].vertical` and
`packageItems[color]` — six variants of each. The colour bomb is the exception
and is already universal: `bombItems[0]`, no index.

**Follow the colour bomb.** A rocket is a rocket whatever cleared it.

Two layers, and they can land separately:

- **Art (cheap, no code).** Fill all six entries of each array with the same
  sprite. The board immediately reads as universal with the protected scripts
  untouched. Do this first.
- **Semantics (code).** A universal powerup should not still behave as a
  coloured piece for matching — in Royal Match a rocket is not part of a
  three-in-a-row. This is an additive branch in the match check, not a rewrite
  of it.

**Acceptance:** a rocket formed by a green match is pixel-identical to one
formed by a red match; a powerup is not consumed by an ordinary colour match.

#### 1.3 Tap to fire, in place

Today `Item.OnMouseDown` only begins a drag; a tap that does not become a drag
does nothing. Royal Match fires the powerup where it sits.

Add — not replace — a tap path: on mouse-up with no drag distance travelled and
the item being a special, activate it on its own square. The existing activation
routines already do the work; this only gives them a new trigger.

**Acceptance:** tapping a rocket, bomb or colour bomb fires it on its own cell,
with the kit's existing explosion VFX; tapping a plain piece still does nothing.

#### 1.4 Swipe to land, then fire

Swiping a powerup moves it one cell and it detonates **at the destination**,
rather than needing to complete a match there.

This is the subtlest of the four, because it changes what a swap *means* when a
special is involved. Today a special-to-special swap is special-cased
(`CheckChocoBomb`, `SetStrippedExtra`); a special-to-plain swap is an ordinary
swap that only pays off if it makes a match. That second case is the one that
changes: the swap completes, the powerup arrives, and it fires there.

Keep the existing special-to-special combinations — they are good, they are
already animated, and Royal Match has them too.

**Acceptance:** swiping a rocket into a neighbouring cell fires it from that
cell; the existing two-special combinations still trigger; an illegal swap still
plays the kit's refusal animation.

#### Consequences to accept

**These rules make the 1209 levels easier.** Tap-to-fire and swipe-to-land both
remove the cost of setting a powerup up. The layouts do not change and must not;
what changes is how much a player gets for a move.

The response is to measure, not to regenerate:

- Compare against the Phase 0 baseline on the same sample of levels.
- If levels are landing far under their move limit, the lever is
  `level_plan.json`'s difficulty curve and the move limits, tuned by hand for
  the areas that need it — **not** `level_gen.py`.
- Boosters are currently granted free on every win (`StoryFreeGame`). Combined
  with tap-to-fire that may be too generous; the grant rate is a one-line
  change and a good first dial to turn.

**Risk:** high, relative to everything else here. It is the only phase touching
`Item.cs` and `LevelManager.cs`, and the only one whose result cannot be judged
from a screenshot — it has to be played. Land the four rules as four commits.

---

### Phase 2 — The board skin

- Apply one board skin. **The one open decision, below.**
- Six colours stay in kit index order.
- Every filename, size and GUID unchanged; a pixel-level rewrite in place.
- Universal powerup art (1.2) is part of this: one rocket, one bomb, one colour
  bomb, drawn once rather than six times.
- Blockers need no new art to make narrative sense — wire is a cage, wood a
  rotted timber, stone fallen masonry, the thriving block rot spreading.

**Acceptance:** `check_vfx_intact.py` and `check_levels_frozen.py` clean; a
screenshot at real tile size where all six pieces are distinguishable.

**Risk:** low. `git checkout Assets/JellyGarden/Textures` undoes it.

---

### Phase 3 — The frame

Title, logo, loading screen, world map tiles, spreading blocker. `brand_skin.py`
already does all of it; the map tiles were restyled *from the kit's own tiles as
the reference image*, so the painted road keeps its shape and the level nodes
still stand on it.

**Risk:** low. The one trap is handled: the main menu draws `logo.png` as its
own object over the banner, so the banner must **not** carry the wordmark, or
the title renders twice, offset.

---

### Phase 4 — Cutscene placeholders and the drop-in contract

The goal is not to make cutscenes look good. It is to make every cutscene site
exist, be obviously a placeholder, be counted, and accept its real replacement
by dropping a file in.

| kind | count | fires when |
|---|---|---|
| opening | 1 | first launch, before the map |
| arc beat | 30 | entering a beat area (`cutscenes.json`) |
| clue card | 150 | last repair of an area completes |
| repair | 807 | a repair is bought with a star |
| **total** | **988** | |

**The words already exist for all 988** — every line is authored in the story
markdown and shipped in `Resources/Story`. So a placeholder is not an empty box;
it plays the real authored text on a neutral, clearly-marked card.

**The animated repair cutscene is kept.** It wipes ruined art to restored art
with per-work-type particles and is one of the better things in the build. What
it lacks is artwork for **125 of the 150 areas** — those fall back to the arc
backdrop plus the damage wash, so they animate but never visibly transform.
Those 125 get the placeholder, not the animation.

> If you did mean to strip that animation back too, say so — I read it the other
> way deliberately, because it is existing work with VFX in it.

Resolution is by filename convention, the way `StoryArt` already works: drop a
correctly-named file into `Resources/Story` and it is picked up, no code change,
no inspector wiring:

```
Cutscenes/opening.<ext>
Cutscenes/arc_beat_<areaId>.<ext>        e.g. arc_beat_005
Cutscenes/clue_<areaId>.<ext>            e.g. clue_017
Art/area_<areaId>_restored.<ext>         the repair cutscene's target state
Art/area_<areaId>_state<n>.<ext>         optional, per-repair states
```

Deliverables: one placeholder presentation; `tools/cutscene_manifest.py`
producing the work order for all 988 sites with the exact filename each real
asset needs; and a **coverage check** so shipping with placeholders is a
decision rather than an accident.

**Risk:** low. No generation spend, no art decisions, no audio.

---

### Phase 5 — Per-arc board dressing

Obstacle sprites tinted per arc through the existing `StoryBoardTheme` path — a
colour on existing sprites, so every animation and particle stays untouched.
Mines read colder, the treasury gilded, the siege scorched, with no new art.

Do it last; it is invisible next to Phases 1 and 4.

---

### Phase 6 — Ship-readiness

- Full verification set, recorded.
- A real playthrough of arc 1 end to end in a release player, under the new rules.
- **Licensing, which cannot be settled in code.** Generated art comes from
  Magnific, existing audio from ElevenLabs. Both services' terms turn on
  *distribution*, not revenue — a free public release is still distribution.

---

## Deferred by decision

**Audio — untouched.** 2 music beds (`theme_ruin`, `theme_level`), 8 SFX, 5
voice lines covering arc 1's opening plus one clue. Everything else narrates
silently, which the code handles: a missing line logs
`[Story] No narration for '...'` once and plays nothing.

Before any future audio pass: **Alfred was never cast.** The ElevenLabs key in
use lacks `voices_read`, so all five existing lines were rendered with whatever
the account default voice is. Fix the key first, or every new line inherits a
voice nobody chose.

**Real cutscene content — yours to generate**, against the Phase 4 manifest.

---

## The one open decision

**Which board skin.** Flat minimal (shield, coin, gem, key, pennant, crown), the
painted jewels, or stay on candy.

Recommendation: **flat.** It is the only one of the three where all six pieces
stay distinguishable at the roughly 60 px a tile actually renders at — and it
suits universal powerups, which want a bold readable silhouette rather than a
painted gem with a lance through it.

---

## Verification, every phase

```bash
UNITY=/Applications/Unity/Hub/Editor/6000.5.10f1/Unity.app/Contents/MacOS/Unity

python3 tools/check_vfx_intact.py        # the feel did not regress
python3 tools/check_levels_frozen.py     # Phase 0 adds this
python3 tools/validate_story.py          # 150 areas / 807 tasks / 1209 stars / 1209 boards

python3 tools/make_typecheck_csproj.py
dotnet build Story.Typecheck.csproj -v q --nologo      # 0 errors

"$UNITY" -batchmode -quit -nographics -projectPath . -logFile - \
  -executeMethod JellyGarden.Scripts.Story.Editor.StorySmokeTest.RunAll
"$UNITY" -batchmode -quit -nographics -projectPath . -logFile - \
  -executeMethod JellyGarden.Scripts.Story.Editor.StoryScreenTests.RunHeadless
```

From Phase 1 on, **none of that is sufficient on its own.** The rule changes can
only be judged by playing: no headless suite can tell you whether tap-to-fire
made level 400 trivial. Every Phase 1 commit needs a played sample against the
Phase 0 baseline.

Batchmode takes an exclusive project lock. **Close the Unity Editor before
running any `$UNITY` line** — with it open they fail, and killing the Editor to
get past that loses unsaved work.

---

## Reference

- Story source of truth: `/Users/avian/Desktop/story` (markdown)
- Story layer: [Assets/JellyGarden/Scripts/Story/](Assets/JellyGarden/Scripts/Story/) — its [CLAUDE.md](Assets/JellyGarden/Scripts/Story/CLAUDE.md)
- Generators and their gotchas: [tools/README.md](tools/README.md)
- Asset and level inventory: `python3 tools/asset_atlas.py <out-dir>`
