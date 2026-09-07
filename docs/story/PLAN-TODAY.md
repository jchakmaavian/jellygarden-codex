# TODAY'S PLAN — Arc 1 Vertical Slice

**Goal:** an APK on a real phone where Area 001 is fully playable start to finish, and Areas 002–025 exist on the map as locked, named, dummy entries.

**Definition of done:** install → play 4 levels → complete 4 repair tasks → see the gatekeeper's-log clue → land on a map showing 24 more locked areas → force-quit → reopen → progress is still there.

---

## THE ONE RULE FOR TODAY

**Locked areas are locked by DATA, not by code.**

Every area lives in `arc1.json` with a `status` field. Area 001 is `"playable"`; 002–025 are `"locked"`. Filling in Area 002 later means writing content and flipping a string — never touching C#.

If you find yourself writing `if (areaId == 1)` anywhere, stop and move it into data.

---

## PHASES

### Phase 0 — Story data pipeline · ~45 min · **DONE**

Turn the markdown into something Unity can read. Do this first — it feeds the map screen.

- [x] `tools/story_build.py` — parse `arc-1-the-missing-queen/area-*.md` → `arc1.json`
- [x] Extract per area: `id`, `name`, `phase`, `beatType`, `tasks[]` (label + star cost), `clue`, `pointsTo`
- [x] Area 001 gets `"status": "playable"` plus its 4 level definitions; 002–025 get `"status": "locked"`
- [x] Output to `Assets/StreamingAssets/story/arc1.json`

**Done when:** `arc1.json` holds all 25 real area names and Area 001's full task list, generated — not hand-typed.

---

### Phase 1 — `Match3.Core` · ~2 hr · **DONE**

Pure C#. No `using UnityEngine`. This is the part that has to be right.

- [x] `Board` — 8×8 grid, 5 tile colours, seeded RNG
- [x] `Swap(a, b)` — legal-move check, revert if no match
- [x] `MatchResolver` — find match-3, match-4 (→ Rocket), match-5 (→ Bomb)
- [x] `Gravity` + refill from the top
- [x] `Cascade` — resolve until stable, return a step list the renderer can animate
- [x] `MoveSolver` — does a legal move exist? (needed for reshuffles)
- [x] `LevelDefinition` — objective, move count, obstacle config
- [x] `GameState` — moves left, objective progress, win/lose

**Done when:** a console can print a board, take a swap, and print the resolved board. No Unity involved.

---

### Phase 2 — Vine obstacle · ~45 min · **DONE**

The only obstacle Area 001 needs.

- [x] `Vine` tile type — occupies a cell, not matchable
- [x] Cleared by a match in an adjacent cell
- [x] Spreads one tile per N turns from an anchor cell
- [x] `spreadEnabled: false` config → level 1 uses this
- [x] Spread picks the nearest empty cell to the anchor, deterministic under a seed

**Done when:** tests prove a vine spreads on turn 3 with `spreadRate: 3`, and never spreads with `spreadEnabled: false`.

---

### Phase 3 — Tests · ~45 min · **DONE**

NUnit against Core. Not optional — the sim in Phase 4 is worthless if the core lies.

- [x] Same seed → identical board, every time
- [x] Match-3 horizontal, vertical, L, T
- [x] Match-4 spawns a Rocket; match-5 spawns a Bomb
- [x] Cascade terminates (no infinite loop)
- [x] Gravity leaves no holes
- [x] Vine spread on schedule; disabled means disabled
- [x] Vine clears from an adjacent match, not a distant one

---

### Phase 4 — `Match3.Sim` · ~1 hr · **DONE**

The headless bot. This is what makes 255 levels tunable instead of 12.

- [x] Greedy bot — pick the move that clears the most objective tiles; tiebreak toward the vine anchor
- [x] Run N trials with different seeds
- [x] Report: win rate, avg moves remaining, avg vine overrun, fail reasons

```bash
dotnet run --project Match3.Sim -- --level arc1/001-04 --trials 10000
```

**Done when:** it prints a win-rate number you believe.

---

### Phase 5 — Tune Area 001 · ~45 min · **DONE**

Hit the spec.

| Level | Objective | Moves | Average-player win rate |
|---|---|---|---|
| 001-01 | Clear 8 vines | 20 | **100%** (spread off, unloseable) |
| 001-02 | Break 6 timbers (2 hits) | 14 | 99.2% |
| 001-03 | Collect 3 banner fragments | 12 | 95.6% |
| 001-04 | Light 4 lanterns | 8 | 92.1% |

- [x] Iterate move counts until the sim lands within ±3% of target
- [x] Write the final numbers back into `area-001-castle-gates.md`

**Rule:** if a level runs low, add a move. Never weaken the vines — the vines are the story.

---

### Phase 6 — Unity board · ~2.5 hr · **DONE**

Grey boxes. No art today.

- [x] `BoardView` — renders `Match3.Core.Board` to sprites
- [x] Swipe input → `Core.Swap()`
- [x] Animate the cascade step list (fall, pop, spawn)
- [x] Vine sprite + spread animation
- [x] HUD — moves left, objective counter
- [x] Win / lose panel; lose panel has **Retry** and nothing else (no lives, no offers)

**Done when:** you can play and beat level 001-01 on a phone.

---

### Phase 7 — Meta layer · ~2 hr · **DONE**

The map, the tasks, the stars, the clue.

- [x] `MapScreen` — reads `arc1.json`, lists all 25 areas
- [x] Area 001 tappable; 002–025 greyed with a lock icon and their real names
- [x] Tapping a locked area → toast: *"Restore the gates first."*
- [x] `AreaScreen` for 001 — 4 tasks, each showing its star cost
- [x] Star counter; spending a star completes a task
- [x] Gate art swaps through 5 states as tasks complete (grey placeholder rectangles today)
- [x] 4th task → **clue card**, 3 seconds, gatekeeper's log text
- [x] Clue dismiss → Area 002 stays locked, map shows Arc 1 progress 1/25

---

### Phase 8 — Save + build · ~1 hr · **DONE**

- [x] `SaveData` model matching `TECH.md`
- [x] Write to temp file, atomic rename
- [x] Load on boot; missing file → fresh save
- [x] Auto-save on level win and task completion
- [ ] ~~Build Android APK~~ — skipped by request; iOS later
- [x] Ran the macOS player with `-autoplay`, screenshotted the whole loop

**Done when:** kill the app mid-Area-001, reopen, and your stars and tasks are exactly where you left them.

---

## NOT TODAY

Explicitly cut. Write them down so they stop nagging.

**Update — five of these were pulled back in and are now DONE.** See `ASSETS.md`.

| Cut | Status |
|---|---|
| Opening cinematic | **DONE** — plays after the first level, per the opening spec's gameplay-first rule |
| Alfred VO (ElevenLabs) | **DONE** — 5 lines wired. Voice not cast yet; see ASSETS.md |
| All real art (Magnific) | **DONE** — 13 images: 5 gate states, castle reveal, Mimi, logbook, 5 gem tiles |
| Music and SFX | **DONE** — 2 themes, 8 effects, ducking under narration |
| Boosters, powerup inventory | **DONE** — Hammer / Rocket / Bomb / Shuffle, earned only |
| Rocket/Bomb activation VFX | Function today, feel later |
| ~~Reshuffle on no-legal-moves~~ | **DONE** — core shuffles and the UI toasts it |
| Areas 002–025 content | Dummy and locked, by design |
| Level generator | Needed for 255 levels, not for 4 |
| iOS build | Android first |
| Settings screen | Save schema has the fields; no UI yet |

---

## TARGET FILE TREE

```
game/
├── Match3.Core/          pure C# — Board, Tile, Vine, MatchResolver, MoveSolver
├── Match3.Tests/         NUnit
├── Match3.Sim/           headless bot + win-rate report
├── tools/
│   └── story_build.py    markdown → arc1.json
└── unity/
    └── Assets/
        ├── StreamingAssets/story/arc1.json
        ├── Scripts/
        │   ├── View/     BoardView, TileView, VineView, HUD
        │   ├── Meta/     MapScreen, AreaScreen, TaskPanel, ClueCard
        │   └── Data/     SaveSystem, StoryLoader
        └── Scenes/       Boot, Map, Level
```

---

## TIME

| Phase | Est. |
|---|---|
| 0 — Story pipeline | 0:45 |
| 1 — Core | 2:00 |
| 2 — Vine | 0:45 |
| 3 — Tests | 0:45 |
| 4 — Sim | 1:00 |
| 5 — Tuning | 0:45 |
| 6 — Unity board | 2:30 |
| 7 — Meta layer | 2:00 |
| 8 — Save + build | 1:00 |
| **Total** | **~11:30** |

That is a long day. **If time runs short, cut Phases 4 and 5** — ship the slice with hand-guessed move counts and tune tomorrow. Everything else is load-bearing.

**Do not cut Phase 3.** Untested core logic makes every later phase slower.

---

## END-OF-DAY ACCEPTANCE

- [x] Runs as a real built player (macOS; APK skipped by request, iOS later)
- [x] Level 001-01 is unloseable
- [x] All 4 levels beatable — autoplay cleared every one
- [x] 4 tasks complete, gate visibly changes each time
- [x] Clue card fires on the 4th task, not on a level win
- [x] Map shows 25 areas, 24 locked with real names
- [x] Tapping a locked area shows "Restore the gates first."
- [x] Progress survives a quit — verified in save.json
- [x] Zero ads, zero offers, zero lives, zero network calls
- [x] 26/26 core tests pass; all 4 levels within 3% of target

[Story Bible](STORY.md) · [Tech](TECH.md) · [Area 001](arc-1-the-missing-queen/area-001-castle-gates.md) · [Opening](arc-1-the-missing-queen/00-OPENING.md)
