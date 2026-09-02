# Adapting the game to the story

The kit is a candy game. The story is about a king putting his castle back
together. This is the plan to make the second true without damaging the two
things that make the first worth keeping.

Two scope decisions are already made and shape everything below:

- **Audio is left exactly as it is.** No new music, no new voice, no
  re-recording. See *Deferred by decision*.
- **Cutscenes get a placeholder everywhere.** Real cutscene content is
  generated later, by you. This plan's job is to make every cutscene site
  present, wired and countable, so dropping the real ones in needs no code
  change. See Phase 3.

---

## The two things that must not regress

These are not preferences. They are the reason this project is worth finishing
rather than rebuilding.

### 1. The feel — animation, particles, VFX

The kit's match, cascade, explosion, swap-refusal and idle animations are the
game's whole tactile quality. **No change deletes, renames or re-authors an
animation clip, a particle system, a material or a prefab.** Everything is
additive, or a pixel-level texture rewrite under the same filename.

Enforced, not asserted:

```bash
python3 tools/check_vfx_intact.py
```

It fails on a deleted or renamed visual asset, on a prefab that lost a
`ParticleSystem` / `Animator` / `Animation` / `SpriteRenderer`, and on a board
script that lost a method or shrank.

**The consequence that keeps catching people out:** the six board colours cannot
be reordered. `Textures/effects/explosion_item_01..06` are tinted per item
*index*. Move green to slot 1 and green pieces shatter into red sparks. Any
reskin keeps red, orange, purple, blue, green, yellow in that order.

### 2. The levels — 1209 boards, tuned

| | |
|---|---|
| boards | 1209 (`Resources/Levels/1..1209.txt`, 9.4 MB) |
| targets | 1209 (`Resources/Targets/Level1..1209.asset`, 9.5 MB) |
| derived from | the kit's 100 shipped, hand-tuned boards |
| difficulty curve | mapped area-by-area in `Resources/Story/level_plan.json` |
| sabotaged boards | 665, from area 19 on |

These are **finished data, not build output.** The generator that made them is
deterministic — every mutation is seeded on the level number
(`random.Random("kingleo/%d")`) — so re-running reproduces the same files
exactly. That is a safety property, not an invitation.

> **Do not re-run `level_gen.py` or `target_gen.py` as part of story work.**
> If a level ever needs to change, edit the one `.txt` by hand and say so in the
> commit. The story bends to fit the levels. The levels do not move.

Phase 0 turns that from something to remember into something checked.

---

## Where the game actually is

Built, committed, and passing its suites:

- 150 areas, 807 repair tasks, 1209 stars, six arcs, parsed from the markdown in
  `/Users/avian/Desktop/story` and shipped as JSON in `Resources/Story`
- area screen, repair flow, clue cards, 30 arc-beat cutscenes, opening sequence
- a working animated repair cutscene — 10 work types classified from each task's
  own verbs, **93% specific, 7% (58 of 807) falling back to generic**
- world map grown from the kit's 100 nodes to 150, with arc banners and area names
- per-arc board backdrop and cell tint (`StoryBoardTheme`); the kit picked a
  backdrop by `level / 20`, which across 1209 levels meant one picture for 90%
  of the game
- the sabotage beat at area 19, using the kit's own thriving block
- free-play economy: no lives spent, no ads, no shop
- save system with atomic writes and per-area star pools

What is **not** true yet: the game still *looks* like a candy game with a story
laid on top. That gap is what this plan closes.

---

## Inventory of the gap

| | have | need |
|---|---|---|
| board pieces | candy — jelly bean, star, choco bomb | reads as the story |
| blockers | jelly, wire, wood, stone, choco | already fit: ivy, cages, timbers, rubble, rot |
| boosters | lollipop, candy bomb | fit the world |
| title / logo / loading | "Jelly Garden" | "King Leo" |
| world map | 5 candy-land tiles | the kingdom's road |
| area artwork | **25 of 150 areas** (all of arc 1) | placeholder for the other 125 |
| cutscene visuals | arc 1 only | placeholder everywhere, real ones later |
| audio | 2 music beds, 8 SFX, 5 voice lines | **nothing — left as is** |

Already built and sitting unapplied in `tools/`, none of it live:

- `item_skin_flat.py` — flat minimal board set, six distinct silhouettes
- `item_skin.py` — painted jewels (rejected: three of six were rectangular gems
  in neighbouring hues and blurred together at tile size)
- `booster_skin.py`, `brand_skin.py` — boosters, logo, title, loading, map tiles

`tools/README.md` says plainly that none is applied.

---

## Phases

Safety net first, then visible impact per unit of risk.

### Phase 0 — Freeze and prove

Nothing else starts until the two protected things are protected by a check
rather than by care.

1. **Level manifest.** Write `tools/levels.sha256`, one line per level and
   target file. Add `tools/check_levels_frozen.py` and call it wherever
   `check_vfx_intact.py` is called.
2. **Prove the win path.** `StoryFlow.HandleWin` awards the star on
   `LevelManager.OnWin`. It is verified **headless only** — nobody has played a
   board to a win in a real player and watched a star land. Do that by hand and
   record it.
3. Re-run the full verification set; put the numbers in the commit.

**Acceptance:** manifest passes on a clean tree; a played win produces a star,
the star spends on a repair, the repair cutscene fires.

**Risk:** none. This phase only adds checks.

---

### Phase 1 — The board

The highest-impact single change: it is what the player looks at for the whole
game.

- Apply one board skin — **the one open decision, see below**.
- Six colours stay in kit index order.
- Every filename, size and GUID unchanged; the rewrite is pixel-level, in place.
- Blockers need no new art to make narrative sense. Wire is a cage, wood is a
  rotted timber, stone is fallen masonry, the thriving block is rot spreading.
  Renaming them in the UI is copy, not art.

**Acceptance:** `check_vfx_intact.py` clean, `check_levels_frozen.py` clean, and
a board screenshot at real tile size where all six pieces are distinguishable.

**Risk:** low. One `git checkout Assets/JellyGarden/Textures` undoes it.

---

### Phase 2 — The frame

Title, logo, loading screen, world map tiles, the spreading blocker.
`brand_skin.py` already does all of it. The map tiles were restyled *from the
kit's own tiles as the reference image*, so the painted road keeps its shape and
the level nodes still stand on it.

**Acceptance:** the first screen names the game correctly; no map node stands in
a field where the road used to be.

**Risk:** low. The one real trap is already handled — the main menu draws
`logo.png` as its own object over the banner, so the banner must **not** carry
the wordmark or the title renders twice, offset.

---

### Phase 3 — Cutscene placeholders and the drop-in contract

This is the phase the cutscene decision reshapes. The goal is **not** to make
cutscenes look good. It is to make every cutscene site exist, be obviously a
placeholder, be counted, and accept its real replacement by dropping a file in.

#### What a "cutscene site" is

| kind | count | fires when |
|---|---|---|
| opening | 1 | first launch, before the map |
| arc beat | 30 | entering a beat area — `cutscenes.json` |
| clue card | 150 | last repair of an area completes |
| repair | 807 | a repair is bought with a star |
| **total** | **988** | |

**The words already exist for all 988.** Every line is authored in the story
markdown and shipped in `Resources/Story`. What is missing is the *visual*.
The placeholder therefore is not an empty box — it plays the authored text on a
neutral, clearly-marked card.

#### The repair cutscene is not replaced

The animated repair cutscene already works: it wipes the ruined art to the
restored art with per-work-type particles, and it is one of the better things in
the build. It stays. What it is missing is **artwork for 125 of the 150 areas** —
those fall back to the arc backdrop plus the damage wash, so they animate but
never visibly transform. Those 125 get the placeholder, not the animation.

> If you actually meant to strip the repair animation back to a placeholder too,
> say so — I read it the other way on purpose, because that animation is
> existing work with VFX in it.

#### The contract, so later is cheap

Resolution is by filename convention, the way `StoryArt` already resolves area
art — drop a correctly-named file into `Resources/Story` and it is picked up
with no code change and no inspector wiring:

```
Cutscenes/opening.<ext>
Cutscenes/arc_beat_<areaId>.<ext>          e.g. arc_beat_005
Cutscenes/clue_<areaId>.<ext>              e.g. clue_017
Art/area_<areaId>_restored.<ext>           the repair cutscene's target state
Art/area_<areaId>_state<n>.<ext>           optional, per-repair states
```

Deliverables:

1. **One placeholder presentation** used at every site with no real asset —
   visibly a placeholder, carrying the site's real authored text plus its id.
2. **`tools/cutscene_manifest.py`** → a manifest of all 988 sites: id, kind,
   area, trigger, the text it already carries, and the exact filename its real
   asset must have. This is the work order for generating them later.
3. **A coverage check** that reports how many sites are still placeholder, so
   the number is visible and a placeholder can never ship unnoticed.

**Acceptance:** every one of the 988 sites plays something and none renders
blank; the manifest lists all 988 with resolvable filenames; the coverage check
reports the real number, and it is allowed to be high.

**Risk:** low. No generation spend, no art decisions, no audio.

---

### Phase 4 — Per-arc board dressing

Cheap polish once 1–3 land. Obstacle sprites tint per arc through the existing
`StoryBoardTheme` path — a colour on existing sprites, so every animation and
particle stays untouched. Mines read colder, the treasury gilded, the siege
scorched, with no new art at all.

**Risk:** low. Do it last; it is invisible next to Phases 1 and 3.

---

### Phase 5 — Ship-readiness

- Full verification set, recorded.
- A real playthrough of arc 1, end to end, in a release player.
- **Licensing, and this one cannot be settled in code.** The generated art comes
  from Magnific and the existing audio from ElevenLabs. Both services' terms turn
  on *distribution*, not revenue — a free public release is still distribution.
  Check both before any store listing.

Shipping with placeholder cutscenes is a real option, and the coverage check
makes it a decision rather than an accident.

---

## Deferred by decision

Recorded so it is deliberate rather than forgotten.

**Audio — untouched.** Currently 2 music beds (`theme_ruin`, `theme_level`),
8 SFX, and 5 voice lines. The voice lines cover arc 1's opening plus one clue.
Everything else narrates silently, which the code handles: a missing line logs
`[Story] No narration for '...'` once and plays nothing.

One thing worth knowing before any future audio pass: **Alfred was never cast.**
The ElevenLabs key in use lacks the `voices_read` permission, so all five
existing lines were rendered with whatever the account default voice is. Fix the
key before generating more, or every new line inherits a voice nobody chose.

**Real cutscene content — yours to generate.** Phase 3 delivers the sites, the
manifest and the naming contract; the content arrives later against that.

---

## The one open decision

**Which board skin.** Flat minimal (shield, coin, gem, key, pennant, crown),
the painted jewels, or stay on candy.

My recommendation is **flat** — it is the only one of the three where all six
pieces stay distinguishable at the roughly 60 px a tile actually renders at.
The painted set looked more expensive and read worse.

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

Batchmode takes an exclusive project lock. **Close the Unity Editor before
running any `$UNITY` line** — with it open they fail, and killing the Editor to
get past that loses unsaved work.

---

## Reference

- Story source of truth: `/Users/avian/Desktop/story` (markdown)
- Story layer: [Assets/JellyGarden/Scripts/Story/](Assets/JellyGarden/Scripts/Story/) — its [CLAUDE.md](Assets/JellyGarden/Scripts/Story/CLAUDE.md)
- Generators and their gotchas: [tools/README.md](tools/README.md)
- Asset and level inventory: `python3 tools/asset_atlas.py <out-dir>`
