# Adapting the game to the story

The kit is a candy game. The story is about a king putting his castle back
together. This is the plan to make the second true without damaging the two
things that make the first worth keeping.

---

## The two things that must not regress

Everything below is shaped by these. They are not preferences; they are the
reason this project is worth finishing rather than rebuilding.

### 1. The feel — animation, particles, VFX

The kit's match, cascade, explosion, swap-refusal and idle animations are the
game's whole tactile quality. **No change deletes, renames or re-authors an
animation clip, a particle system, a material or a prefab.** Everything is
additive or a pixel-level texture rewrite under the same filename.

Enforced, not asserted:

```bash
python3 tools/check_vfx_intact.py
```

It fails on a deleted or renamed visual asset, on a prefab that lost a
`ParticleSystem` / `Animator` / `Animation` / `SpriteRenderer` component, and on
a board script that lost a method or shrank.

**Consequence that keeps catching people out:** the six board colours cannot be
reordered. `Textures/effects/explosion_item_01..06` are tinted per item *index*.
Move green to slot 1 and green pieces shatter into red sparks. Any reskin keeps
red, orange, purple, blue, green, yellow in that order.

### 2. The levels — 1209 boards, tuned

| | |
|---|---|
| boards | 1209 (`Resources/Levels/1..1209.txt`, 9.4 MB) |
| targets | 1209 (`Resources/Targets/Level1..1209.asset`, 9.5 MB) |
| derived from | the kit's 100 shipped, hand-tuned boards |
| difficulty curve | mapped area-by-area in `Resources/Story/level_plan.json` |
| sabotaged boards | 665, starting at area 19 |

These are **finished data, not build output.** The generator that made them is
deterministic — every mutation is seeded on the level number
(`random.Random("kingleo/%d")`) — so re-running it reproduces the same files.
That is a safety property, not an invitation. From here on:

> **Do not re-run `level_gen.py` or `target_gen.py` as part of story work.**
> If a level ever needs to change, edit the one `.txt` by hand and say so in the
> commit. The story bends to fit the levels. The levels do not move.

Phase 0 adds a manifest so this is checked rather than remembered.

---

## Where the game actually is

Already built, committed and passing its suites:

- 150 areas, 807 repair tasks, 1209 stars, six arcs, parsed from the markdown
  in `/Users/avian/Desktop/story` and shipped as JSON in `Resources/Story`
- area screen, repair flow, clue cards, 30 arc-beat cutscenes
- repair cutscenes that animate the specific work — 10 work types classified
  from the task's own verbs, **93% specific, 7% (58 of 807) falling back to
  generic**
- world map extended from the kit's 100 nodes to 150, with arc banners and area
  names
- per-arc board backdrop and cell tint (`StoryBoardTheme`) — the kit picked a
  backdrop by `level / 20`, which with 1209 levels meant one picture for 90% of
  the game
- the sabotage beat at area 19, using the kit's own thriving block
- free-play economy: no lives spent, no ads, no shop
- save system with atomic writes and per-area star pools

What is **not** true yet: the game still *looks* like a candy game with a story
laid on top of it. That gap is what this plan closes.

---

## Inventory of the gap

| | have | need |
|---|---|---|
| board pieces | candy (jelly bean, star, choco bomb) | reads as the story |
| blockers | jelly, wire, wood, stone, choco | already fit — ivy, cages, timbers, rubble, rot |
| boosters | lollipop, candy bomb | fit the world |
| title / logo / loading | "Jelly Garden" | "King Leo" |
| world map | 5 candy-land tiles | the kingdom's road |
| area artwork | **25 of 150 areas** (all of arc 1) | 125 areas across arcs 2–6 |
| music | 2 beds (ruin, level) | per-arc, or at least per-act |
| voice | 5 lines, all arc 1 | the clue cards at minimum |
| SFX | 8 | fine as is |

Two pieces of work are already built and sitting unapplied in `tools/`:

- `item_skin.py` — painted jewels (rejected: three of six were rectangular gems
  in neighbouring hues and blurred together at tile size)
- `item_skin_flat.py` — flat minimal set, six distinct silhouettes
- `booster_skin.py`, `brand_skin.py` — boosters, logo, title, loading, map tiles

None is applied. `tools/README.md` says so.

---

## Phases

Ordered so the safety net comes first, then visible impact per unit of risk,
then the expensive long tail.

### Phase 0 — Freeze and prove

Nothing else starts until the two protected things are protected by a check
rather than by care.

1. **Level manifest.** Write `tools/levels.sha256` — one line per level and
   target file. Add `tools/check_levels_frozen.py`, and call it from the same
   place `check_vfx_intact.py` is called.
2. **Prove the win path.** `StoryFlow.HandleWin` awards the star on
   `LevelManager.OnWin`. It is verified **headless only** — no one has ever
   played a board to a win in a real player and watched a star land. Do that,
   by hand, and record it.
3. Re-run the full verification set and record the numbers in the commit.

**Acceptance:** level manifest passes on a clean tree; a played win produces a
star, the star can be spent on a repair, and the repair cutscene fires.

**Risk:** none. This phase only adds checks.

---

### Phase 1 — The board

The single highest-impact change: it is what the player looks at for the entire
game.

- Apply one board skin. **Decision needed** — see below.
- Keep the six colours in kit index order.
- Keep every filename, size and GUID; the rewrite is pixel-level and in place.
- Blockers need no new art to make narrative sense — wire is a cage, wood is a
  rotted timber, stone is fallen masonry, the thriving block is rot spreading.
  Renaming them in the UI is copy, not art.

**Acceptance:** `check_vfx_intact.py` clean; `check_levels_frozen.py` clean; a
board screenshot at real tile size where all six pieces are distinguishable.

**Risk:** low, and one `git checkout Assets/JellyGarden/Textures` undoes it.

---

### Phase 2 — The frame

Title, logo, loading screen, world map tiles, and the spreading blocker.
`brand_skin.py` already does all of it; the map tiles were restyled *from the
kit's own tiles as the reference image* so the painted road keeps its shape and
the level nodes still stand on it.

**Acceptance:** the first thing the player sees names the game correctly, and
no map node is standing in a field where the road used to be.

**Risk:** low. The one real trap is already handled — the main menu draws
`logo.png` as its own object over the banner, so the banner must **not** carry
the wordmark or the title renders twice, offset.

---

### Phase 3 — Area artwork for arcs 2–6

The expensive one: **125 areas with no art of their own.** They currently fall
back to the arc backdrop plus the damage wash, so they animate but never
visibly transform. That is the difference between a story and a checklist.

Three options, cheapest first:

| option | images | what the player gets |
|---|---|---|
| **A. per-arc set** | ~30 (5 states × 6 arcs) | every area in an arc shares a look; repairs read as progress within the arc, not within the room |
| **B. per-area restored** | 125 | each area has its own room, one before/after pair via the damage wash |
| **C. per-area staged** | ~500 | what arc 1 has: a picture per repair state |

**Recommendation: B.** It is the smallest spend that makes each of the 150 areas
feel like a different place, and the repair cutscene's wipe already carries the
in-between states convincingly — that is exactly what arc 1's areas 3–25 do
today with a single restored image each.

**Hard-won rule from arc 1, do not lose it:** generate each state from the state
*before* it, never all states from the ruin. Fanning out from the ruin produced
a banner state with the *broken* gate restored underneath it, silently undoing a
repair the player had already paid a star for.

**Acceptance:** `StoryArt.HasAreaArt(id)` true for all 150; no generated state
regresses an earlier repair.

**Risk:** medium — it is the only phase with a real budget attached, and the
only one whose output needs eyes on every image.

---

### Phase 4 — Sound

- Music: currently two beds for the whole game. One per arc, or at minimum one
  per act (ruin / pursuit / reckoning), cross-faded the way the two already are.
- Voice: 5 lines exist, all arc 1. The clue cards are where narration earns its
  place — 150 of them.
- **Open blocker from arc 1, still unresolved:** Alfred is not cast. The
  ElevenLabs key in use lacks `voices_read`, so every existing line was rendered
  with whatever the account default is. Fix the key before generating 150 more
  lines in a voice nobody chose.

**Acceptance:** no `[Story] No narration for '...'` warnings for clue cards.

---

### Phase 5 — Board dressing per arc

Cheap polish once phases 1–3 land: the obstacle sprites can be tinted per arc
through the existing `StoryBoardTheme` path — a colour on existing sprites, so
every animation and particle stays untouched. Mines read colder, the treasury
reads gilded, the siege reads scorched, with no new art at all.

**Risk:** low, but do it last — it is invisible next to phases 1 and 3.

---

### Phase 6 — Ship

- Full verification set, recorded.
- A real playthrough of arc 1 end to end in a release player.
- **Licensing, and this one cannot be settled in code:** the generated art and
  audio come from Magnific and ElevenLabs. Both services' commercial-use terms
  turn on *distribution*, not revenue. A free public release is still
  distribution. Check both before any store listing.

---

## Decisions I need from you

1. **Which board skin.** The flat minimal set (six silhouettes — shield, coin,
   gem, key, pennant, crown), the painted jewels, or stay on candy. My
   recommendation is flat: it is the only one of the three where all six pieces
   stay distinguishable at the ~60px a tile actually renders at.
2. **Phase 3 option A, B or C** — that is where the budget goes.
3. **Whether to fix the ElevenLabs key** before phase 4, or ship arc 1's voice
   as-is and revisit.

---

## Verification, every phase

```bash
UNITY=/Applications/Unity/Hub/Editor/6000.5.10f1/Unity.app/Contents/MacOS/Unity

python3 tools/check_vfx_intact.py        # the feel did not regress
python3 tools/check_levels_frozen.py     # phase 0 adds this
python3 tools/validate_story.py          # 150 areas / 807 tasks / 1209 stars / 1209 boards

python3 tools/make_typecheck_csproj.py
dotnet build Story.Typecheck.csproj -v q --nologo      # 0 errors

"$UNITY" -batchmode -quit -nographics -projectPath . -logFile - \
  -executeMethod JellyGarden.Scripts.Story.Editor.StorySmokeTest.RunAll
"$UNITY" -batchmode -quit -nographics -projectPath . -logFile - \
  -executeMethod JellyGarden.Scripts.Story.Editor.StoryScreenTests.RunHeadless
```

Batchmode takes an exclusive project lock. **Close the Unity Editor before
running any of the `$UNITY` lines** — with it open they fail, and killing the
Editor to get past that loses unsaved work.

---

## Reference

- Story source of truth: `/Users/avian/Desktop/story` (markdown)
- Story layer: [Assets/JellyGarden/Scripts/Story/](Assets/JellyGarden/Scripts/Story/) — its [CLAUDE.md](Assets/JellyGarden/Scripts/Story/CLAUDE.md)
- Generators and their gotchas: [tools/README.md](tools/README.md)
- Asset and level inventory: `python3 tools/asset_atlas.py <out-dir>`
