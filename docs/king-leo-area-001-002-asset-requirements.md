# Areas 001–002 — kid-friendly asset requirements

**Superseded — 2026-09-12:** This is a historical proposal for the original campaign. All older area repair videos have been removed at the owner's request. Its video requirements are withdrawn; use the [Treasure Hunt development plan](story/TREASURE-HUNT-DEVELOPMENT-PLAN.md) for current production.

Date: 2026-09-10
Status: **requirement only.** Nothing here is designed or implemented. No art
has been made, no prompt written, no file replaced.

Scope: Area 001 (Castle Gates, 3 tasks, levels 1–3) and Area 002 (Outer
Courtyard, 5 tasks, levels 4–8). Their story text was rewritten for young
readers on 2026-09-10; the art and animation behind that text has not been
touched, and in Area 002 it contradicts the new copy outright.

---

## Headline count

**24 new files**, plus 1 shared backdrop that both areas sit on, plus one tool
re-run that produces no new art.

| Area | New files | Of which art | Of which video | Of which audio |
|---|---|---|---|---|
| 001 Castle Gates | 3 | 3 | 0 | 0 |
| 002 Outer Courtyard | 21 | 12 | 5 | 4 |
| Shared (both) | 1 | 1 | 0 | 0 |
| **Total** | **25** | **16** | **5** | **4** |

Area 001 is nearly done. Area 002 is effectively unstarted — its six paintings
exist but are the wrong shape, the wrong style, and tell the wrong story.

---

## What already exists

Verified by reading the files, not from memory.

### Area 001 — keep

| Asset | Files | Spec | Verdict |
|---|---|---|---|
| Room states `area_001_state0..3.jpg` | 4 | 941×1672 portrait | **Keep.** Bright, warm, cartoon-lit castle gate. This is the project's own brightness benchmark — `check_themes.py` measures every other picture against the dimmest of these four. |
| Repair clips `area_001_task1..3.mp4` | 3 | 720×1280, 24 fps, 6.0 s | **Keep, pending review.** Nobody has watched these against the new copy. |
| Clue card `clue_001.jpg` | 1 | — | Keep. |
| VO `alfred_clue_001`, `alfred_open_01..04` | 5 | mp3 | Keep. Copy changed; see open question 3. |
| SFX `timber_break`, `vine_cut`, `lantern_light`, `repair_done`, `star_earned` | 5 | mp3 | Keep. |

### Area 002 — replace

| Asset | Files | Spec | Verdict |
|---|---|---|---|
| Room states `area_002_state0..5.jpg` | 6 | 1760×1328 **landscape** | **Replace all six.** Three separate faults, below. |
| Clue card `clue_002.jpg` | 1 | 1760×1328 landscape | **Replace.** Same style and shape problem. |
| Repair clips | 0 | — | **Five missing.** |

Why all six Area 002 paintings fail:

1. **Wrong shape.** 1760×1328 is landscape. The game runs portrait at 393×852
   and Area 001's approved art is 941×1672. A landscape painting either crops
   its own subject away or letterboxes.
2. **Wrong style.** Dark, desaturated, near-photographic oil painting — grey
   stone, overcast sky, no saturated colour anywhere. The project brief is
   *bright, cartoon-like colour for the castle and everything else.*
3. **Wrong story.** `state0` shows a crowned stone king face-down in the weeds,
   read at a glance as a body in a graveyard. `state5` — the fully repaired,
   all-five-stars payoff — is still a grey empty yard under a cold sky. A child
   who spends five stars gets no visible reward.

### Shared

| Asset | Files | Verdict |
|---|---|---|
| `backdrop_castle.jpg` | 1 | **Replace.** 1152×2048, dark and near-photographic. It is the board backdrop behind levels 1–8, so it is on screen for every level of both areas. Already one of the ten `NOTE` lines `check_themes.py` reports. |
| Leo avatar frames (25 idle + 17 each cheer/pop/slump/worry) | 93 | Keep. Area-agnostic, already in style. |
| Character portraits (`portrait_*`, 7 characters × 4 moods) | 28 | Keep. Areas 001–002 have no cutscene — first one is Area 005 — so only the clue card and HUD use them here. |

---

## What must be made

### 1. Room state paintings — 6 files

`Resources/Story/Art/area_002_state0.jpg` … `area_002_state5.jpg`

- **941×1672 portrait**, matching Area 001's approved artwork exactly.
- Bright cartoon colour. Brightness at or above the dimmest of
  `area_001_state0..3` — that is the measured floor `check_themes.py` enforces.
- State *n* = the courtyard with *n* repairs finished, in task order:
  0 messy → 1 poles gone → 2 path fixed → 3 statue standing → 4 trough full →
  5 drains clear.
- Camera locked. Every state must be the same viewpoint at the same scale, or
  the repair wipe between them reads as a scene change rather than a repair.
- Each repair must be **visible at 393×852 on a phone**, and must be the *only*
  thing that changed between consecutive states — this is what
  `repair_regions_build.py` diffs to find the sparkle target.
- `state0` is untidy, not grim: no body-like statue, no graveyard reading, no
  overcast sky.
- `state5` is the payoff and must look it — colour, light, life.

### 2. Repair animations — 5 files

`Resources/Story/RepairAnimations/area_002_task1.mp4` … `area_002_task5.mp4`

- **720×1280, 24 fps, 6.0 s**, no audio track — the same spec the three Area 001
  clips already ship at, and the spec the headless screen test asserts.
- One clip per repair, showing the change happening: poles carried off, stones
  laid, statue rising, water filling, leaves sweeping away.
- Must start on the previous state's frame and end on the next state's frame, or
  the cut back to the still painting jumps.
- Optional per the code — `RepairCutscene` falls back to the particle effect —
  but Area 001 has all three, so Area 002 having none is an inconsistency a
  player feels immediately at level 4.

### 3. Task icons — 8 files

`Resources/Story/Art/task_001_1..3.png` and `task_002_1..5.png`

- The small picture on a task's row in the repair list. Today **zero of the 806
  tasks have one**; the UI draws a generic glyph guessed from the label text.
- Areas 001–002 are 8 of those 806. A child reading "Fill the horses' water"
  should see a trough of water, not a generic wrench.
- Transparent PNG. Size to be set from the row height in the royal UI — not
  measured yet, see open question 1.

### 4. Clue card — 1 file

`Resources/Story/Art/clue_002.jpg` — portrait, bright, and matched to the new
clue text: *"Someone pushed the king's statue over. It did not just fall."*
The picture carries the whole clue for a pre-reader, so the push must be
legible in the image: marks at the statue's base, the statue off its plinth.
No blood, no menace — this is a mystery, not a threat.

### 5. Voice-over — 1 file

`Resources/Story/Audio/VO/alfred_clue_002.mp3`

`StoryAudio` resolves clue VO as `alfred_clue_{areaId:000}`. Only
`alfred_clue_001` exists, so Area 002's clue card plays silent today. One line,
Alfred, matching the rewritten clue.

### 6. Sound effects — 3 files

`Resources/Story/Audio/SFX/` — Area 002's obstacle is **rubble blocks** (two
hits, no spread) and nothing in the SFX folder covers it or its repairs:

- `rubble_break.mp3` — the two-hit blocker clearing.
- `water_pour.mp3` — task 4.
- `sweep.mp3` — task 5.

Tasks 1–3 can reuse `timber_break` and `repair_done`.

---

## Constraints any delivery must satisfy

**These are gates in the repo, not preferences.**

1. `StoryScreenTests.RunHeadless` asserts Area 001's four state sprites are
   **exactly 941×1672**. A repaint at any other size turns the suite red.
2. The same test asserts each Area 001 clip is **720×1280 and 6.0 s ±0.1**.
3. The same test asserts `StoryRepairVideo.Load(2, 1) == null` — it currently
   proves that an *unanimated* area falls back to the particle effect. **Adding
   Area 002's clips breaks this test**, and the fix belongs in the same change:
   the assertion has to move to an area that still has no clips.
4. `check_vfx_intact.py` must stay green. New art is additive; nothing may
   remove or replace a kit prefab, clip or particle.
5. `check_themes.py` must not gain a new `NOTE`. Replacing `backdrop_castle.jpg`
   should *remove* one of the ten it reports.
6. `repair_regions_build.py` must be re-run after the Area 002 paintings land —
   the region table is a pixel diff of consecutive states, and today it has
   entries for Area 001 tasks 2–3 and Area 002 tasks 1–2 only. **Five of this
   scope's eight repairs have no measured region**, so their sparkle falls back
   to the whole room. Regenerating is a tool run, not an asset.
7. New files under `Resources/` need their `.meta` committed, or the guid is
   lost.

---

## Open questions — answer before any art is made

1. **Task icon size.** Nothing measures the repair-list row today. Needed before
   8 icons are drawn at the wrong resolution.
2. **Restyle Area 001's three clips?** They predate the kid-friendly rewrite and
   nobody has watched them since. If they read as adult in tone, the count grows
   by 3 and the total becomes 28.
3. **Re-record Area 001's five VO lines?** `alfred_open_01..04` and
   `alfred_clue_001` were recorded against the old copy — *"Break the rotten
   timbers"*, *"He never finished the sentence."* The on-screen text is now
   simpler; the audio is not. If they must match, the count grows by 5.
4. **Do the story's own obstacles reach the board?** Area 001's JSON objectives
   (`breakTimbers`, `collectFragments`, `lightLanterns`) and Area 002's rubble
   are **not wired into levels 1–8** — those are the kit's preserved boards with
   the kit's blockers and score targets. If they are ever wired, each obstacle
   needs board-piece art of its own and this requirement roughly doubles. Out of
   scope until that decision is made.
5. **Area 002's `state0` is the ruin the story needs and the image a child
   sees.** Untidy-but-cheerful and abandoned-and-sad pull in opposite
   directions. The brief says bright cartoon; the beat says the kingdom is
   neglected. Someone has to pick which wins at state 0.
