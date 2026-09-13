# Adventure of King Leo — Treasure Hunt Development Plan

**Created:** 2026-09-12  
**Working game title:** Adventure of King Leo (provisional name chosen by the owner).  
**Chosen board item set:** Storybook Magic in the Bold Cartoon style: red potion, orange mushroom, purple wizard hat, blue crystal, green clover and yellow star. The Soft Storybook board layout remains selected. See the [approved design preview](../../art-review/treasure-hunt/board-directions/item-sets/index.html) and [artwork notes](../../art-review/treasure-hunt/board-directions/item-sets/README.md). This selection is recorded in the browser preview; Unity integration is pending.  
**Story:** [King Leo and the Great Royal Treasure Hunt](GREAT-ROYAL-TREASURE-HUNT.md)  
**Goal:** Develop the new adventure one complete, playable area at a time, in order from 001 to 150.  
**Current implementation status:** Foundation and Area 001 are complete. All four repairs use the approved native transitions; the opening and ending contain two dialogue cards each. The selected Royal Storybook UI is implemented in the shared screens and menus, verified at 393×852 and 320×568. Four real wins fund four repairs, and Area 002 remains locked. Older repair videos remain removed.  
**Current focus:** Owner playtest of the implemented Royal Storybook UI in Area 001. Area 002 production has not started. See [area notes](treasure-hunt/areas/area-001.md) and [UI integration review](../../art-review/treasure-hunt/royal-storybook-runtime/README.md).

## Progress dashboard

Mark a box only when the work has been completed and checked in the game. Having a story outline or an existing room asset does not make an area complete.

- [x] Write the six-chapter story and screenplay.
- [x] Confirm the new story direction with the game owner.
- [x] Finish the minimum foundation for Area 001.
- [x] Complete Area 001 as the first fully playable example.
- [ ] Complete Areas 002–005 and verify the first continuous play session.
- [ ] Complete Chapter 1 — Areas 001–025.
- [ ] Complete Chapter 2 — Areas 026–050.
- [ ] Complete Chapter 3 — Areas 051–075.
- [ ] Complete Chapter 4 — Areas 076–100.
- [ ] Complete Chapter 5 — Areas 101–125.
- [ ] Complete Chapter 6 — Areas 126–150.
- [ ] Complete the full-game release checks.

**Navigation:** [Workflow](#how-we-will-work) · [Foundation](#minimum-foundation-before-area-001) · [Definition of done](#definition-of-done-for-every-area) · [Chapter 1](#chapter-1--the-castle-of-surprises) · [Chapter 2](#chapter-2--the-runaway-parade) · [Chapter 3](#chapter-3--the-crystal-express) · [Chapter 4](#chapter-4--the-royal-adventure-games) · [Chapter 5](#chapter-5--the-upside-down-tower) · [Chapter 6](#chapter-6--the-biggest-surprise) · [Release checks](#full-game-release-checks)

## How we will work

Finish the active area before starting production on the next one. An area includes its script, room art, tasks, playable levels, discovery scene, and saved progress. Shared features are built when the first area that needs them is reached.

1. Read the area's entry in this plan and its story references.
2. Write or adapt its short production brief, including exact task order and star cost.
3. Create the needed room states, task icons, character expressions, and discovery artwork.
4. Connect the content to the game's existing level, task, dialogue, and repair flow.
5. Play the area through normally, test its saved state, and fix the issues found.
6. Save evidence, mark its checklist, and advance to the next area.

Keep the next unfinished area unavailable to ordinary play. A completed area can end with its next story objective and a clear “More adventures are being prepared” message until the next area is ready. Development preview tools may open unfinished areas explicitly. Once the next area is completed, recheck the handoff between the two.

At every fifth area, check the preceding five-area story sequence as well as its milestone scene. At each chapter ending, check its key reward and the next chapter's entry. These checks happen as the content becomes available; they do not require developing a whole chapter before reviewing the first area.

Use the existing match-3 controls, animation, particles, and powerups. Parade chases, train journeys, games, and inventions can initially use short scripted animations or illustrated reveals. New playable minigames are outside this first implementation plan.

The current soundtrack and sound effects are the starting audio set. The story must be understandable through pictures and readable dialogue. New voice recordings or a replacement soundtrack can be a later production pass; visually demonstrate the musical clues even without bespoke music.

### What to save for each area

Create these artifacts when work on that area begins, rather than creating 150 empty folders now:

- An authoring brief under the proposed `docs/story/treasure-hunt/areas/area-NNN.md`, linked to the accepted screenplay.
- Structured task and dialogue records in the new campaign's authoring package; runtime JSON is compiled from that package.
- Art masters, export names, and a manifest of every room state and task icon used.
- A completion record at the end of the brief: date, task/star/level totals, changed assets, checks run, and any follow-up polish.
- Initial, intermediate, and completed room screenshots plus a screenshot of the discovery scene, saved under the proposed `art-review/treasure-hunt/area-NNN/` directory.

The screenplay stays the narrative reference. The per-area package holds the implementation details. After an intentional story change, update both so that future areas do not inherit conflicting instructions.

## Existing game baseline and decisions

These observations were read from the repository on 2026-09-12. They are a starting point, not completion claims about the new adventure.

| Topic | Observed state | Development decision |
|---|---|---|
| Current campaign | Generated index lists 150 areas, 806 tasks, and 1,208 stars; the level plan lists 1,208 entries. | Reuse the working systems and suitable boards. Derive the new campaign's totals from its actual task budget. |
| Area 001 | Runtime has three tasks and three assigned levels. The new script specifies four tasks. | Implement the new four-task sequence and provide four first-win stars. Reconcile its level assignments before declaring the area playable. |
| Areas 002–005 | Runtime task counts are 5, 5, 5, and 6; star budgets are 5, 6, 6, and 8. | Use these as initial budgets while checking the new scripts and pacing. |
| Story source | `tools/story_common.py` points to `/Users/avian/Desktop/story`; `story_sync.py` copies authored JSON from there. | Add an explicit repository-local authoring package for the new campaign and make compiler input/output selection unambiguous. |
| Opening | `OpeningSequence.cs` contains the old dialogue directly. | Route the new campaign to the approved two-card `OPEN-01`; Freya speaks in Area 001's two-card ending. |
| Cutscenes | `cutscene_build.py` synthesizes dialogue from older prose and delays Freya until Area 026. | Import the new screenplay's explicit speakers and lines; keep staging notes out of spoken dialogue. |
| Saved progress | `StoryProgress.cs` uses one `story_save.json`, with task, level, area, and seen-scene state. | Give the new campaign a distinct progress identity and save file. Preserve the old save without deleting or silently reinterpreting it. |
| Unlocking | Completing an area currently unlocks its successor without checking content readiness. | Require both player progress and completed campaign content before an ordinary player can enter an area. |
| Board themes | Existing chapter keys include `crypts` and `siege`; the level flow also carries a sabotage story beat. | Adopt the new chapter presentation and remove the old plot from the new campaign. Keep any useful obstacle behavior only with a fitting, friendly explanation. |
| Old planning documents | Some older notes quote 1,209 boards and give different regeneration instructions. | Use the current data snapshot for baselines; reconcile the authoring workflow documents when the new compiler is added. |

Board allocation must remain consistent: one first-win star per rewardable level, total available stars matching the area's task cost, and unique level identities within the campaign. Replaying a beaten level cannot fund a missing star. Moving a board to Area 001 must not silently leave another area short of stars.

Preserve the kit's shipped Levels 1–100 and protected VFX assets. If a new or retuned board is needed, add it through the intended authoring/generation workflow and validate its specific gameplay. Do not patch generated story files as the lasting source or regenerate unrelated board sets just to change dialogue.

## Minimum foundation before Area 001

Build only the shared work required to run the new opening and finish one area. Later chapter features have their own checkpoints below.

- [ ] **Capture the baseline:** Record the current story/level totals, the first area's task and board assignments, relevant asset names, and a short playthrough of the existing win-to-repair loop.
- [x] **Create a new campaign package:** Establish a repository-local authoring location, a campaign identifier, and explicit source/output configuration for the new story. Keep the existing campaign's source available as a reference.
- [x] **Add a small, explicit content schema:** Support stable area/task IDs, task cost, completion line, speaker, scene ID, trigger, stage cue, and next-area reference. Separate display text from production instructions. Compile Area 001 first.
- [x] **Select the campaign consistently:** Make story loading, level mappings, dialogue, opening, area names, art, and themes resolve through the same selected campaign. Campaign selection is startup-only; restart the player or reload the Unity domain to change campaigns and their caches together.
- [x] **Separate saved progress:** Store the new campaign's task, level, opening, and scene state independently. Check the kit's PlayerPrefs interactions so an old win cannot suppress a new campaign reward. Keep user audio preferences intact.
- [x] **Gate incomplete content:** Add readiness information and enforce it in home, areas list, map, next-level launch, and post-area continuation. Show only new-story text in new-campaign previews; unfinished scenes must not fall back to the old plot.
- [x] **Connect explicit dialogue:** Make the current dialogue presentation consume authored speakers/lines. Support skippable scenes and an explicit replay path with no gameplay rewards. Establish scene IDs and recovery behavior for interrupted scenes.
- [x] **Prepare opening characters:** Reuse suitable Leo, Freya, Alfred, and Mimi art; create Pip's dragon/map presentation. Verify readable portraits and names on the current phone layout. No speaking dialogue for Mimi.
- [x] **Set the new Area 001 budget:** Allocate four one-star tasks and four suitable first-win level entries. Confirm actual goals match the tutorial's instructions. Keep the extra task from causing a duplicate level reward or starving Area 002's eventual allocation.
- [x] **Protect the existing feel:** Keep match, cascade, powerup, repair, and victory effects working. Add focused checks for new progress and readiness behavior; update content validation to understand partial campaign development without treating missing required assets in a ready area as acceptable.
- [x] **Document the new build command:** Record the exact compiler command and its outputs, demonstrate a reproducible Area 001 build, and update stale source-path instructions. Do not assume the old sync command imports this Markdown directly.

**Foundation exit condition:** A developer can select the new campaign, enter its first real board, earn a star once, view the correct opening, spend that star on the correct task, and reload without losing the result. Area 002 remains inaccessible until its new content is ready.

## Definition of done for every area

Copy these checks into the active area's completion note and leave this master template unchanged. Pass every item before checking that area's “Complete area” box. This does not require running the entire game suite after every copy edit.

- [ ] **Story:** Correct new title, visible goal, arrival line, task labels, completion lines, discovery, and next objective. Characters know only what has been revealed so far.
- [ ] **Budget:** Task order and star cost are explicit. Assigned first-win levels supply exactly the required stars. A retry or replay cannot duplicate stars or rewards.
- [ ] **Art:** Every task produces its intended visible change. Initial, intermediate, and completed states, task icons, portraits, repair regions, and discovery art are present at the required resolution.
- [ ] **Gameplay:** Play the area's assigned boards and purchase its tasks normally. Goals match what the board can actually do; the difficulty and instructions fit the area. Use the existing VFX without regression.
- [ ] **Scene delivery:** The payoff follows the final task exactly once. A milestone replaces the ordinary discovery card. Skip, explicit replay, and interrupted-session recovery work without duplicate grants or missing required progress.
- [ ] **Saved progress:** Reload midway through the area and after completion. Task state, room state, stars, scene flags, and any earned key persist. No previous area improvement is lost.
- [ ] **Navigation:** Home, the task panel, area list, map, and Play show the correct area. The successor opens only when both completed-player-progress and ready-content conditions are true.
- [ ] **Presentation:** Inspect the target portrait window and the smallest supported layout. Text, buttons, task markers, faces, and clue objects remain readable; there is no old-story copy or missing-asset fallback in the playable area.
- [ ] **Evidence:** Save screenshots and a brief normal-play result, fix blocking issues, and update the area's completion record. Log optional polish separately without marking a broken area complete.

### Verification scope

Use read-only content and asset checks for each area. Run targeted runtime checks when scripts, saving, rewards, triggers, or navigation change. A story rewrite does not need to re-prove every existing board after every line of dialogue.

The existing tools provide starting points:

| Check | When it is relevant |
|---|---|
| `python3 tools/validate_story.py` | After adapting it to the new campaign's schema/readiness rules, validate task totals, references, scene coverage, and level-plan consistency. The current validator alone cannot prove the new campaign is correct. |
| `python3 tools/check_vfx_intact.py` | After changes touching gameplay scripts, art integration, or protected effects, and at chapter completion. |
| `python3 tools/lint_levels.py --levels <range>` | For the actual board numbers assigned or changed; area IDs are not level numbers. |
| `python3 tools/make_typecheck_csproj.py` then `dotnet build Story.Typecheck.csproj -v q --nologo` | When adding or changing C# scripts. |
| Story screen, content-audit, and smoke checks | Adapt to the new campaign; run focused coverage while building an area and broader checks at chapter boundaries. |
| `tools/run_player.sh` | Use for real-player checks in the established portrait window. A screenshot or headless pass alone does not prove the play-and-spend loop. |
| `tools/playtest_sweep.py --levels <range>` | When board difficulty, objectives, or gameplay behavior changed. Run one foreground batch and inspect its actual output. |

For generators, validate the output, inspect the diff, and verify reproducibility. For unchanged gameplay, reuse existing validation evidence until a new change or failure calls it into question.

## Shared features introduced when needed

This table gives dependencies for future areas. It is not a second list of features that must all be built before Area 001.

| First needed | Feature | Required behavior |
|---|---|---|
| Area 001 | Pip and map display | Show the guide and six empty key spaces; keep keys visually distinct from level stars. |
| Area 005 | Milestone playback | Play `C1-005` after the last task; suppress the ordinary discovery card and remember the scene. |
| Area 006 | Persistent discovery flags | Record that the mystery tower was revealed so revisits remain consistent. |
| Area 010 | Visual musical clue sequence | Show the tune's first phrase, add the next pieces at 015 and 020, and reuse the combined pattern at 024–025. |
| Area 025 | Key awards and map progress | Award the Melody Key once, including on skip/recovery, and persist it independently from stars. Support explicit reward-free scene replay. |
| Area 026 | Chapter transition | Switch title, theme, map route, and available cast without resetting completed areas. |
| Area 030 | Moving story props | Remember the float's current stop; animate progress only after the relevant task/area completion. |
| Area 051 | Railway progress | Display the route segments actually completed by the player. |
| Area 055 | Pebble | Add dragon art and nonverbal reactions; show the Crystal Key without awarding it early. |
| Area 080 | Shared games pennants | Record five specific pennants and explain their rules before awarding them. Keep pennants separate from keys and level stars. |
| Area 105 | Invention staging | Reuse the area's scene system for humming lifts, floating furniture, and visible controls. |
| Area 126 | Return visits | Reference the completed castle improvements while adding new festival state. |
| Area 142 | Non-milestone story scene | Dispatch `S142` explicitly. Five keys open the outer door and remain owned; do not rely only on every-fifth-area logic. |
| Area 145 | Combined stations | Persist the music/motion/light/wind/invention completion and grant the Sunburst Key exactly once. |
| Area 149 | Finale setup scene | Dispatch `S149` after all six keys are present and remember that the main console is ready. |
| Area 150 | Completed campaign state | Show the full celebration, replay it safely, and return to the completed festival. The teaser must not launch missing content. |

## Reading the area checklists

Each area below has its own production checkboxes. “Complete area” means the shared definition of done has passed and evidence is recorded. The listed baseline budget comes from the existing runtime; it is not a fixed budget for the rewrite. Preserve it where the new tasks fit, and explicitly revise the campaign's mapping when it changes.

“Author and integrate” includes the area's arrival line, exact ordered tasks and costs, one completion line per task, and next objective. Areas 001–005 already have detailed screenplay text to adapt; later areas require those task-level details to be written from the accepted outline.

“Create and integrate art” includes the initial room, every task state, final room, task icons, repair regions, relevant character expressions, and discovery staging. Reuse existing art when it fits the new story and document any edits.

## Chapter 1 — The Castle of Surprises
**Order:** Areas 001–025. **Payoff:** Melody Key at Area 025.  
**Story reference:** [Chapter 1 outline and screenplay](GREAT-ROYAL-TREASURE-HUNT.md#6-chapter-1--the-castle-of-surprises).

### Chapter entry and feature checkpoints

- [x] Finish the minimum foundation and work on Area 001 first.
- [x] Prepare the bright castle palette and reuse compatible castle artwork; introduce new character art only as needed.
- [ ] By Area 005, integrate milestone scenes; by 010, implement the musical clue display; by 025, implement persistent key awards.

### Area 001 — Castle Gates

**Implemented budget:** 4 tasks · 4 stars · boards 1–4 (new campaign mapping). [Brief and checks](treasure-hunt/areas/area-001.md).

- [x] **Tasks and script:** All four Area 001 tasks use their approved native transitions: vine clearing, door assembly, flag unfurling, and sequential lantern lighting. Tasks 1–3 return to the panel without dialogue. Only four cards remain: Leo/Pip at the opening and Pip/Freya at the final sun discovery.
- [x] **Art:** Updated states 2–4, the task-2 icon, and its repair region for the wooden door; the door persists through the flag and lantern tasks.
- [x] **Discovery:** Reviewed the sparkle on the existing smiling sun emblem and Pip's clue dialogue after the lanterns light.
- [x] **Area-specific check:** Implement OPEN-01 after the first win: introduce Pip from the decoration cupboard, show six empty key spaces, then return to the first repair.
- [x] **Area-specific check:** Build four one-star tasks in order: clear the vines, build the wooden door, decorate with royal flags, light the lanterns. Supply four unique first-win rewards and match the tutorial copy to the real board goals.
- [x] **Area-specific check:** Prove opening skip/replay, a reload after the first star, and the completed-area boundary while Area 002 is still unavailable.
- [x] **Complete area 001:** Passed the shared definition of done, saved updated evidence, and verified the Area 002 readiness boundary.

### Area 002 — Outer Courtyard

**Existing budget:** 5 tasks · 5 stars · 5 assigned levels.

- [ ] **Tasks and script:** Prepare a welcoming fountain and path.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** Water forms a horse shape and points to the stables. Deliver as the final-task reveal and short dialogue card.
- [ ] **Area-specific check:** After this area becomes ready, replay the 001-to-002 handoff without developer-granted stars or lost task progress.
- [ ] **Complete area 002:** Pass the shared definition of done, save evidence, and verify the Area 003 readiness boundary.

### Area 003 — The Stables

**Existing budget:** 5 tasks · 6 stars · 6 assigned levels.

- [ ] **Tasks and script:** Prepare parade ponies and a small cart.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** Mimi finds a toy trumpet with a guard-house badge. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 003:** Pass the shared definition of done, save evidence, and verify the Area 004 readiness boundary.

### Area 004 — Guard House

**Existing budget:** 5 tasks · 6 stars · 6 assigned levels.

- [ ] **Tasks and script:** Organize the welcome band and signal flags.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** A musical seal on a banner matches the entrance-hall floor. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 004:** Pass the shared definition of done, save evidence, and verify the Area 005 readiness boundary.

### Area 005 — Entrance Hall

**Existing budget:** 6 tasks · 8 stars · 8 assigned levels.

- [ ] **Tasks and script:** Uncover a colorful floor pattern and display the map.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** Grimm joins the hunt; the floor points toward the throne. Play [C1-005](GREAT-ROYAL-TREASURE-HUNT.md#c1-005--a-very-proper-treasure-hunter) after the final task.
- [ ] **Area-specific check:** Play the continuous 001–005 session; show Freya from the opening and introduce Grimm here as a confident, friendly rival.
- [ ] **Complete area 005:** Pass the shared definition of done, save evidence, and verify the Area 006 readiness boundary.

### Area 006 — Throne Room

**Existing budget:** 7 tasks · 10 stars · 10 assigned levels.

- [ ] **Tasks and script:** Polish the throne platform and restore its carved sun.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** A viewing door reveals the mystery tower's five door locks and sun-shaped window. Deliver as the final-task reveal and short dialogue card.
- [ ] **Area-specific check:** Persist the mystery-tower discovery; show five door locks and a sun-shaped window, matching Areas 124 and 142.
- [ ] **Complete area 006:** Pass the shared definition of done, save evidence, and verify the Area 007 readiness boundary.

### Area 007 — Cloak Room

**Existing budget:** 6 tasks · 8 stars · 8 assigned levels.

- [ ] **Tasks and script:** Set up costume rails for festival visitors.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** A tiny conductor's coat contains a dinner invitation. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 007:** Pass the shared definition of done, save evidence, and verify the Area 008 readiness boundary.

### Area 008 — Dining Room

**Existing budget:** 6 tasks · 8 stars · 8 assigned levels.

- [ ] **Tasks and script:** Restore a table that lays out its own settings.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** A spoon taps the map's first musical rhythm. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 008:** Pass the shared definition of done, save evidence, and verify the Area 009 readiness boundary.

### Area 009 — Royal Kitchen

**Existing budget:** 7 tasks · 10 stars · 10 assigned levels.

- [ ] **Tasks and script:** Prepare the festival ovens and cake station.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** A cake mold carries the same musical-note symbol. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 009:** Pass the shared definition of done, save evidence, and verify the Area 010 readiness boundary.

### Area 010 — The Pantry

**Existing budget:** 6 tasks · 9 stars · 9 assigned levels.

- [ ] **Tasks and script:** Sort ingredients and tune a musical cupboard.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** The cupboard plays the first part of the key's tune. Play [C1-010](GREAT-ROYAL-TREASURE-HUNT.md#c1-010--the-singing-cupboard) after the final task.
- [ ] **Area-specific check:** Introduce the first visual musical phrase and save it for the later fountain, bubble, and staircase clues.
- [ ] **Complete area 010:** Pass the shared definition of done, save evidence, and verify the Area 011 readiness boundary.

### Area 011 — Helpers' Lounge

**Existing budget:** 6 tasks · 9 stars · 9 assigned levels.

- [ ] **Tasks and script:** Build a comfortable break room for the festival team.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** A cushion springs up to reveal a laundry-room ticket. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 011:** Pass the shared definition of done, save evidence, and verify the Area 012 readiness boundary.

### Area 012 — Bubble Laundry

**Existing budget:** 6 tasks · 9 stars · 9 assigned levels.

- [ ] **Tasks and script:** Restore a gentle bubble-powered washing machine.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** Clean fabric reveals a rose-shaped diagram. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 012:** Pass the shared definition of done, save evidence, and verify the Area 013 readiness boundary.

### Area 013 — Rose Garden

**Existing budget:** 8 tasks · 12 stars · 12 assigned levels.

- [ ] **Tasks and script:** Shape hedges and open a flower path.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** The hedges form an arrow toward the greenhouse. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 013:** Pass the shared definition of done, save evidence, and verify the Area 014 readiness boundary.

### Area 014 — The Greenhouse

**Existing budget:** 6 tasks · 10 stars · 10 assigned levels.

- [ ] **Tasks and script:** Grow bright flowers and arrange hanging planters.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** A flower opens into the shape of a fountain spout. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 014:** Pass the shared definition of done, save evidence, and verify the Area 015 readiness boundary.

### Area 015 — Fountain Plaza

**Existing budget:** 6 tasks · 10 stars · 10 assigned levels.

- [ ] **Tasks and script:** Tune the water jets to the map's rhythm.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** The fountain plays the second part of the tune. Play [C1-015](GREAT-ROYAL-TREASURE-HUNT.md#c1-015--a-fountain-with-rhythm) after the final task.
- [ ] **Area-specific check:** Add the second musical phrase while retaining the first phrase from Area 010.
- [ ] **Complete area 015:** Pass the shared definition of done, save evidence, and verify the Area 016 readiness boundary.

### Area 016 — The Library

**Existing budget:** 8 tasks · 13 stars · 13 assigned levels.

- [ ] **Tasks and script:** Sort books and restore a rolling reading ladder.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** A picture book shows a staircase made of piano keys. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 016:** Pass the shared definition of done, save evidence, and verify the Area 017 readiness boundary.

### Area 017 — Leo's Study

**Existing budget:** 6 tasks · 10 stars · 10 assigned levels.

- [ ] **Tasks and script:** Set up an adventure desk and drawing board.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** Leo's childhood sketch shows the stairs beside the map room. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 017:** Pass the shared definition of done, save evidence, and verify the Area 018 readiness boundary.

### Area 018 — The Map Room

**Existing budget:** 6 tasks · 10 stars · 10 assigned levels.

- [ ] **Tasks and script:** Repair a model of the castle.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** A miniature staircase lights up beside a pool symbol. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 018:** Pass the shared definition of done, save evidence, and verify the Area 019 readiness boundary.

### Area 019 — Royal Pool

**Existing budget:** 8 tasks · 14 stars · 14 assigned levels.

- [ ] **Tasks and script:** Add floating toy boats and a safe play edge.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** A boat carries a bath-house tile marked with a musical note. Deliver as the final-task reveal and short dialogue card.
- [ ] **Area-specific check:** Replace the old sabotage introduction with the toy-boat discovery. Audit board instructions and any automatic sabotage dialogue before the first assigned board launches.
- [ ] **Complete area 019:** Pass the shared definition of done, save evidence, and verify the Area 020 readiness boundary.

### Area 020 — Bubble Bath House

**Existing budget:** 7 tasks · 12 stars · 12 assigned levels.

- [ ] **Tasks and script:** Tune the bubble pumps and polish the tiled floor.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** Musical bubbles complete the tune and reveal its order. Play [C1-020](GREAT-ROYAL-TREASURE-HUNT.md#c1-020--a-tune-made-of-bubbles) after the final task.
- [ ] **Area-specific check:** Complete the visual tune with the third phrase; preserve the order and shapes used at Areas 010 and 015.
- [ ] **Complete area 020:** Pass the shared definition of done, save evidence, and verify the Area 021 readiness boundary.

### Area 021 — Royal Rest Room

**Existing budget:** 8 tasks · 14 stars · 14 assigned levels.

- [ ] **Tasks and script:** Prepare a cozy picnic-and-reading stop.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** A music-box dancer points toward Freya's costume collection. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 021:** Pass the shared definition of done, save evidence, and verify the Area 022 readiness boundary.

### Area 022 — Freya's Costume Room

**Existing budget:** 7 tasks · 12 stars · 12 assigned levels.

- [ ] **Tasks and script:** Build a dress-up stage and hang festival costumes.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** A dancer's shoe opens a small door toward the ballroom. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 022:** Pass the shared definition of done, save evidence, and verify the Area 023 readiness boundary.

### Area 023 — The Ballroom

**Existing budget:** 8 tasks · 15 stars · 15 assigned levels.

- [ ] **Tasks and script:** Restore the dance floor and its music controls.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** The floor repeats the tune and lights a route onward. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 023:** Pass the shared definition of done, save evidence, and verify the Area 024 readiness boundary.

### Area 024 — The Great Hall

**Existing budget:** 7 tasks · 13 stars · 13 assigned levels.

- [ ] **Tasks and script:** Restore the musical staircase and its railings.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** Walking the tune opens the secret music room. Deliver as the final-task reveal and short dialogue card.
- [ ] **Area-specific check:** Use the already-discovered tune to open the music room. Show the staircase sequence as a task payoff, without requiring an extra rhythm minigame.
- [ ] **Complete area 024:** Pass the shared definition of done, save evidence, and verify the Area 025 readiness boundary.

### Area 025 — The Secret Music Room

**Existing budget:** 8 tasks · 18 stars · 18 assigned levels.

- [ ] **Tasks and script:** Restore the instruments and play the complete tune.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** The Melody Key appears; Pip reveals the village parade. Play [C1-025](GREAT-ROYAL-TREASURE-HUNT.md#c1-025--the-melody-key) after the final task.
- [ ] **Area-specific check:** Grant the Melody Key exactly once. Recover the grant after interruption or scene skip, show 1 of 6, and keep explicit replay reward-free.
- [ ] **Complete area 025:** Pass the shared definition of done, save evidence, and verify the Area 026 readiness boundary.

### Chapter 1 completion check

- [ ] All 25 area completion boxes for 001–025 are checked, with evidence in their briefs.
- [ ] All five milestone scenes are in the correct order and play once in normal progression.
- [ ] Verify the chapter reward: Melody Key at Area 025. Reload and replay without duplicate grants.
- [ ] Review the chapter’s ordinary discoveries, character continuity, theme, task pacing, and real-player screenshots.
- [ ] Recheck the complete first session and all three tune phrases before the Melody Key reveal.
- [ ] Check the 025-to-026 transition when the next area is ready; until then, check the locked-content message.

## Chapter 2 — The Runaway Parade
**Order:** Areas 026–050. **Payoff:** Ribbon Key at Area 050.  
**Story reference:** [Chapter 2 outline and screenplay](GREAT-ROYAL-TREASURE-HUNT.md#7-chapter-2--the-runaway-parade).

### Chapter entry and feature checkpoints

- [ ] Verify the 025-to-026 handoff, retaining the Melody Key and completed castle progress.
- [ ] Prepare village art and the reusable parade-float prop before its introduction at 028–030.
- [ ] Add float-stop state and the finishing-song sequence when needed; keep the parade independent of wall-clock time.

### Area 026 — Castle Bridge

**Existing budget:** 5 tasks · 5 stars · 5 assigned levels.

- [ ] **Tasks and script:** Decorate the crossing and prepare a float-width lane.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** Wheel-shaped map marks point down the village road. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 026:** Pass the shared definition of done, save evidence, and verify the Area 027 readiness boundary.

### Area 027 — Village Road

**Existing budget:** 5 tasks · 6 stars · 6 assigned levels.

- [ ] **Tasks and script:** Add bunting, benches, and route markers.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** The village's drumbeat becomes audible around the bend. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 027:** Pass the shared definition of done, save evidence, and verify the Area 028 readiness boundary.

### Area 028 — Village Square

**Existing budget:** 5 tasks · 6 stars · 6 assigned levels.

- [ ] **Tasks and script:** Build the parade assembly area.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** An unfinished dragon float has the map's ribbon emblem. Deliver as the final-task reveal and short dialogue card.
- [ ] **Area-specific check:** Introduce the dragon float using the same design that will appear through Chapter 2 and in the finale.
- [ ] **Complete area 028:** Pass the shared definition of done, save evidence, and verify the Area 029 readiness boundary.

### Area 029 — The Ribbon Mill

**Existing budget:** 5 tasks · 6 stars · 6 assigned levels.

- [ ] **Tasks and script:** Restore ribbon spools and the float's fabric wings.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** An old instruction tag describes a musical parade engine. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 029:** Pass the shared definition of done, save evidence, and verify the Area 030 readiness boundary.

### Area 030 — The Float Workshop

**Existing budget:** 5 tasks · 7 stars · 7 assigned levels.

- [ ] **Tasks and script:** Complete the float and fit Grimm's parade engine.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** The Ribbon Key lights inside its nose; the float starts early. Play [C2-030](GREAT-ROYAL-TREASURE-HUNT.md#c2-030--the-parade-starts-itself) after the final task.
- [ ] **Area-specific check:** Reveal the Ribbon Key inside the float, start its first route segment, and save its stop. Do not add the key to the owned-key inventory yet.
- [ ] **Complete area 030:** Pass the shared definition of done, save evidence, and verify the Area 031 readiness boundary.

### Area 031 — The Bakery

**Existing budget:** 6 tasks · 8 stars · 8 assigned levels.

- [ ] **Tasks and script:** Prepare snack carts along the float's first stop.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** The float pauses for a bun, then follows a bell-shaped sign. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 031:** Pass the shared definition of done, save evidence, and verify the Area 032 readiness boundary.

### Area 032 — The Bell Pavilion

**Existing budget:** 5 tasks · 7 stars · 7 assigned levels.

- [ ] **Tasks and script:** Restore the parade bells and their rope wheels.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** One bell makes the float turn toward the orchard. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 032:** Pass the shared definition of done, save evidence, and verify the Area 033 readiness boundary.

### Area 033 — The Orchard

**Existing budget:** 5 tasks · 7 stars · 7 assigned levels.

- [ ] **Tasks and script:** Clear a winding route and add apple stalls.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** Painted wheel tracks lead to the riverside. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 033:** Pass the shared definition of done, save evidence, and verify the Area 034 readiness boundary.

### Area 034 — Fisherman's Pier

**Existing budget:** 5 tasks · 6 stars · 6 assigned levels.

- [ ] **Tasks and script:** Build a viewing deck and a broad turning platform.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** The float waits at the unfinished harbor entrance. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 034:** Pass the shared definition of done, save evidence, and verify the Area 035 readiness boundary.

### Area 035 — The Harbor

**Existing budget:** 5 tasks · 7 stars · 7 assigned levels.

- [ ] **Tasks and script:** Prepare the waterfront parade stop and a soft landing haystack.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** Grimm's flying carriage lands in the hay; a musical control is spotted. Play [C2-035](GREAT-ROYAL-TREASURE-HUNT.md#c2-035--an-excellent-landing) after the final task.
- [ ] **Area-specific check:** Stage Grimm’s gentle haystack landing and the team helping him; maintain the float’s correct route position.
- [ ] **Complete area 035:** Pass the shared definition of done, save evidence, and verify the Area 036 readiness boundary.

### Area 036 — The Boat Workshop

**Existing budget:** 5 tasks · 7 stars · 7 assigned levels.

- [ ] **Tasks and script:** Build a little escort boat with a band platform.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** A carved music symbol matches the float's engine. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 036:** Pass the shared definition of done, save evidence, and verify the Area 037 readiness boundary.

### Area 037 — The Costume Store

**Existing budget:** 5 tasks · 7 stars · 7 assigned levels.

- [ ] **Tasks and script:** Unpack animal costumes and percussion instruments.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** Mimi finds a conductor's baton inside a giant rabbit ear. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 037:** Pass the shared definition of done, save evidence, and verify the Area 038 readiness boundary.

### Area 038 — Market Street

**Existing budget:** 6 tasks · 7 stars · 7 assigned levels.

- [ ] **Tasks and script:** Arrange stalls and string festival lights.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** Stall awnings reveal the first notes of a painted parade song. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 038:** Pass the shared definition of done, save evidence, and verify the Area 039 readiness boundary.

### Area 039 — The Picnic Inn

**Existing budget:** 5 tasks · 7 stars · 7 assigned levels.

- [ ] **Tasks and script:** Set up picnic tables and a musician's rest stop.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** The old parade leader remembers where the song is stored. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 039:** Pass the shared definition of done, save evidence, and verify the Area 040 readiness boundary.

### Area 040 — Weaver's Hall

**Existing budget:** 5 tasks · 7 stars · 7 assigned levels.

- [ ] **Tasks and script:** Weave the finishing banner and unfold the parade song.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** Freya learns the finishing tune makes the float bow and stop. Play [C2-040](GREAT-ROYAL-TREASURE-HUNT.md#c2-040--the-song-at-the-finish) after the final task.
- [ ] **Area-specific check:** Reveal the finishing-song rule clearly enough that Area 050 feels like the player’s plan succeeding.
- [ ] **Complete area 040:** Pass the shared definition of done, save evidence, and verify the Area 041 readiness boundary.

### Area 041 — The Schoolhouse

**Existing budget:** 5 tasks · 6 stars · 6 assigned levels.

- [ ] **Tasks and script:** Prepare costumes and practice spaces for the children's band.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** The band learns the first musical phrase. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 041:** Pass the shared definition of done, save evidence, and verify the Area 042 readiness boundary.

### Area 042 — Village Well

**Existing budget:** 5 tasks · 7 stars · 7 assigned levels.

- [ ] **Tasks and script:** Restore a musical water wheel beside the well.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** Its rhythm supplies the song's second phrase. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 042:** Pass the shared definition of done, save evidence, and verify the Area 043 readiness boundary.

### Area 043 — The Welcome House

**Existing budget:** 5 tasks · 7 stars · 7 assigned levels.

- [ ] **Tasks and script:** Prepare a float-viewing garden and accessible seating.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** More villagers join the chorus and finish the song. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 043:** Pass the shared definition of done, save evidence, and verify the Area 044 readiness boundary.

### Area 044 — The Lookout Post

**Existing budget:** 5 tasks · 8 stars · 8 assigned levels.

- [ ] **Tasks and script:** Add route flags and a large parade map.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** The final route leads through the celebration gate. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 044:** Pass the shared definition of done, save evidence, and verify the Area 045 readiness boundary.

### Area 045 — Celebration Gate

**Existing budget:** 5 tasks · 6 stars · 6 assigned levels.

- [ ] **Tasks and script:** Build a wide arch and a clear path to the manor gardens.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** Grimm helps steer the float through; the finishing plan is ready. Play [C2-045](GREAT-ROYAL-TREASURE-HUNT.md#c2-045--room-for-everyone) after the final task.
- [ ] **Complete area 045:** Pass the shared definition of done, save evidence, and verify the Area 046 readiness boundary.

### Area 046 — The Invitation Station

**Existing budget:** 5 tasks · 7 stars · 7 assigned levels.

- [ ] **Tasks and script:** Stamp and deliver invitations to the final parade stop.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** Hometown performers begin arriving with their instruments. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 046:** Pass the shared definition of done, save evidence, and verify the Area 047 readiness boundary.

### Area 047 — The Party House

**Existing budget:** 5 tasks · 8 stars · 8 assigned levels.

- [ ] **Tasks and script:** Assemble a rolling stage and dressing rooms.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** The stage's wheels fit the final stretch of the parade route. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 047:** Pass the shared definition of done, save evidence, and verify the Area 048 readiness boundary.

### Area 048 — Party Gardens

**Existing budget:** 5 tasks · 8 stars · 8 assigned levels.

- [ ] **Tasks and script:** Prepare flower arches and the band's rehearsal lawn.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** The finishing tune makes the float's nose ornament blink. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 048:** Pass the shared definition of done, save evidence, and verify the Area 049 readiness boundary.

### Area 049 — The Music Pavilion

**Existing budget:** 5 tasks · 8 stars · 8 assigned levels.

- [ ] **Tasks and script:** Set up the band, conductor's stand, and sound bells.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** A successful rehearsal points everyone toward Harvest Court. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 049:** Pass the shared definition of done, save evidence, and verify the Area 050 readiness boundary.

### Area 050 — Harvest Court

**Existing budget:** 8 tasks · 16 stars · 16 assigned levels.

- [ ] **Tasks and script:** Complete the grandstand and perform the finishing song.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** The float bows, awards the Ribbon Key, and reveals a train ticket. Play [C2-050](GREAT-ROYAL-TREASURE-HUNT.md#c2-050--the-ribbon-key) after the final task.
- [ ] **Area-specific check:** Stop the float with the finishing song, grant the Ribbon Key once, show 2 of 6, and reveal the train ticket.
- [ ] **Complete area 050:** Pass the shared definition of done, save evidence, and verify the Area 051 readiness boundary.

### Chapter 2 completion check

- [ ] All 25 area completion boxes for 026–050 are checked, with evidence in their briefs.
- [ ] All five milestone scenes are in the correct order and play once in normal progression.
- [ ] Verify the chapter reward: Ribbon Key at Area 050. Reload and replay without duplicate grants.
- [ ] Review the chapter’s ordinary discoveries, character continuity, theme, task pacing, and real-player screenshots.
- [ ] Check the 050-to-051 transition when the next area is ready; until then, check the locked-content message.

## Chapter 3 — The Crystal Express
**Order:** Areas 051–075. **Payoff:** Crystal Key at Area 075.  
**Story reference:** [Chapter 3 outline and screenplay](GREAT-ROYAL-TREASURE-HUNT.md#8-chapter-3--the-crystal-express).

### Chapter entry and feature checkpoints

- [ ] Verify the 050-to-051 handoff and retained Melody/Ribbon Keys.
- [ ] Prepare the railway route display, train/carriage props, and cave palette for Area 051 onward.
- [ ] Prepare Pebble and the toy train before Area 055; reserve the key grant for the station opening at 075.

### Area 051 — The Ticket House

**Existing budget:** 5 tasks · 6 stars · 6 assigned levels.

- [ ] **Tasks and script:** Restore ticket windows and a cheerful station sign.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** An old route map shows a crystal-shaped key marker. Deliver as the final-task reveal and short dialogue card.
- [ ] **Area-specific check:** Initialize the railway display from completed track sections; avoid showing unfinished destinations as reachable.
- [ ] **Complete area 051:** Pass the shared definition of done, save evidence, and verify the Area 052 readiness boundary.

### Area 052 — Station Steps

**Existing budget:** 5 tasks · 5 stars · 5 assigned levels.

- [ ] **Tasks and script:** Build a wide arrival ramp and luggage benches.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** Small round dragon footprints lead onto the platform. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 052:** Pass the shared definition of done, save evidence, and verify the Area 053 readiness boundary.

### Area 053 — The Engine Shed

**Existing budget:** 5 tasks · 7 stars · 7 assigned levels.

- [ ] **Tasks and script:** Restore the little train and its passenger carriage.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** A bubble floats from the cave entrance and pops over Leo's cap. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 053:** Pass the shared definition of done, save evidence, and verify the Area 054 readiness boundary.

### Area 054 — The Badge Workshop

**Existing budget:** 5 tasks · 7 stars · 7 assigned levels.

- [ ] **Tasks and script:** Make train badges and polish the train's bell.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** A tiny toy wheel rolls in from the crystal inspection room. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 054:** Pass the shared definition of done, save evidence, and verify the Area 055 readiness boundary.

### Area 055 — The Crystal Room

**Existing budget:** 5 tasks · 7 stars · 7 assigned levels.

- [ ] **Tasks and script:** Open display windows and prepare a toy-train table.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** Meet Pebble; the Crystal Key is the flag on the little train. Play [C3-055](GREAT-ROYAL-TREASURE-HUNT.md#c3-055--a-very-small-passenger) after the final task.
- [ ] **Area-specific check:** Introduce Pebble with chirps and gestures, show the key as the toy-train flag, and record the promise to return for the station opening.
- [ ] **Complete area 055:** Pass the shared definition of done, save evidence, and verify the Area 056 readiness boundary.

### Area 056 — The Route Office

**Existing budget:** 5 tasks · 6 stars · 6 assigned levels.

- [ ] **Tasks and script:** Plan the sightseeing line and Pebble's play station.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** A drawing identifies the parts needed for a proper toy flag. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 056:** Pass the shared definition of done, save evidence, and verify the Area 057 readiness boundary.

### Area 057 — The Supply Barn

**Existing budget:** 5 tasks · 6 stars · 6 assigned levels.

- [ ] **Tasks and script:** Sort timber, ribbons, and rounded track pieces.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** A supply crate reveals the castle's familiar sun-and-gear mark. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 057:** Pass the shared definition of done, save evidence, and verify the Area 058 readiness boundary.

### Area 058 — The Lantern Store

**Existing budget:** 5 tasks · 7 stars · 7 assigned levels.

- [ ] **Tasks and script:** Build lantern strings for the cave route.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** Lit lanterns reveal the next stretch of track. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 058:** Pass the shared definition of done, save evidence, and verify the Area 059 readiness boundary.

### Area 059 — The Meadow Stop

**Existing budget:** 5 tasks · 6 stars · 6 assigned levels.

- [ ] **Tasks and script:** Create a picnic stop with room for animal visitors.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** Pebble demonstrates a carriage that needs a softer seat. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 059:** Pass the shared definition of done, save evidence, and verify the Area 060 readiness boundary.

### Area 060 — The Northbound Track

**Existing budget:** 5 tasks · 7 stars · 7 assigned levels.

- [ ] **Tasks and script:** Fit the first long section of sightseeing railway.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** Grimm's shortcut gadget unfolds into a very long spring. Play [C3-060](GREAT-ROYAL-TREASURE-HUNT.md#c3-060--the-spring-surprise) after the final task.
- [ ] **Area-specific check:** Keep Grimm’s long-spring prop/design available for its useful return at Area 065.
- [ ] **Complete area 060:** Pass the shared definition of done, save evidence, and verify the Area 061 readiness boundary.

### Area 061 — The Whistle Stop

**Existing budget:** 5 tasks · 6 stars · 6 assigned levels.

- [ ] **Tasks and script:** Install a water pump and a musical station whistle.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** The whistle answers one of the castle's musical notes. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 061:** Pass the shared definition of done, save evidence, and verify the Area 062 readiness boundary.

### Area 062 — The Stone Workshop

**Existing budget:** 5 tasks · 7 stars · 7 assigned levels.

- [ ] **Tasks and script:** Shape smooth stepping stones and station signs.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** A stone panel illustrates connected railway and castle gears. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 062:** Pass the shared definition of done, save evidence, and verify the Area 063 readiness boundary.

### Area 063 — The Glittering Cave

**Existing budget:** 5 tasks · 8 stars · 8 assigned levels.

- [ ] **Tasks and script:** Illuminate crystal walls and finish the viewing path.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** Reflections reveal the flag pattern Pip has been tracing. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 063:** Pass the shared definition of done, save evidence, and verify the Area 064 readiness boundary.

### Area 064 — The Explorer Lodge

**Existing budget:** 5 tasks · 6 stars · 6 assigned levels.

- [ ] **Tasks and script:** Prepare benches, blankets, and a craft table.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** Freya sketches a carriage spring that would suit Pebble. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 064:** Pass the shared definition of done, save evidence, and verify the Area 065 readiness boundary.

### Area 065 — The Gear Workshop

**Existing budget:** 5 tasks · 7 stars · 7 assigned levels.

- [ ] **Tasks and script:** Turn Grimm's long spring into a cushioned carriage seat.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** Pebble tests the new seat and gives Grimm an enormous bubble. Play [C3-065](GREAT-ROYAL-TREASURE-HUNT.md#c3-065--grimm-gets-it-right) after the final task.
- [ ] **Area-specific check:** Pay off the earlier spring with the comfortable carriage seat and show Grimm sharing his design.
- [ ] **Complete area 065:** Pass the shared definition of done, save evidence, and verify the Area 066 readiness boundary.

### Area 066 — The Rainbow Yard

**Existing budget:** 5 tasks · 8 stars · 8 assigned levels.

- [ ] **Tasks and script:** Paint the carriages and assemble the toy flagpole.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** The finished flagpole needs the ribbon from the riverside mill. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 066:** Pass the shared definition of done, save evidence, and verify the Area 067 readiness boundary.

### Area 067 — The Water Bridge

**Existing budget:** 5 tasks · 7 stars · 7 assigned levels.

- [ ] **Tasks and script:** Restore the bridge's water wheel and viewing balcony.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** Its movement powers the next railway signal. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 067:** Pass the shared definition of done, save evidence, and verify the Area 068 readiness boundary.

### Area 068 — The Ribbon Wheel

**Existing budget:** 5 tasks · 7 stars · 7 assigned levels.

- [ ] **Tasks and script:** Restore a small loom and weave Pebble's train flag.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** The flag is ready; the remaining track leads over the river. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 068:** Pass the shared definition of done, save evidence, and verify the Area 069 readiness boundary.

### Area 069 — The Picnic Junction

**Existing budget:** 5 tasks · 6 stars · 6 assigned levels.

- [ ] **Tasks and script:** Set up food tables and a train-turning platform.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** Friends gather for the first full practice journey. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 069:** Pass the shared definition of done, save evidence, and verify the Area 070 readiness boundary.

### Area 070 — The River Crossing

**Existing budget:** 5 tasks · 7 stars · 7 assigned levels.

- [ ] **Tasks and script:** Finish the rail bridge and set its signals.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** The train completes its first river crossing with everyone's help. Play [C3-070](GREAT-ROYAL-TREASURE-HUNT.md#c3-070--across-the-sparkling-river) after the final task.
- [ ] **Area-specific check:** Animate the crossing only after the bridge and route are complete; keep every completed railway segment visible on reload.
- [ ] **Complete area 070:** Pass the shared definition of done, save evidence, and verify the Area 071 readiness boundary.

### Area 071 — The Echo Cove

**Existing budget:** 5 tasks · 7 stars · 7 assigned levels.

- [ ] **Tasks and script:** Add an echo-listening deck and glowing route signs.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** The train whistle's echo reveals an opening behind a crystal screen. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 071:** Pass the shared definition of done, save evidence, and verify the Area 072 readiness boundary.

### Area 072 — The Hidden Platform

**Existing budget:** 5 tasks · 7 stars · 7 assigned levels.

- [ ] **Tasks and script:** Restore the little station beyond the screen.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** An old diagram connects its track to the Wonderworks symbol. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 072:** Pass the shared definition of done, save evidence, and verify the Area 073 readiness boundary.

### Area 073 — The Ticket Tent

**Existing budget:** 5 tasks · 8 stars · 8 assigned levels.

- [ ] **Tasks and script:** Prepare invitations for Pebble's station opening.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** A northern courier asks to meet the train at its final stop. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 073:** Pass the shared definition of done, save evidence, and verify the Area 074 readiness boundary.

### Area 074 — Pebble's Playground

**Existing budget:** 5 tasks · 7 stars · 7 assigned levels.

- [ ] **Tasks and script:** Build toy tracks, bubble hoops, and a dragon-sized seat.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** Everything is ready for the new flag and the opening ceremony. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 074:** Pass the shared definition of done, save evidence, and verify the Area 075 readiness boundary.

### Area 075 — Crystal Grand Station

**Existing budget:** 8 tasks · 16 stars · 16 assigned levels.

- [ ] **Tasks and script:** Finish the station canopy and open the full railway.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** Pebble gives the Crystal Key; Haldor's invitation arrives. Play [C3-075](GREAT-ROYAL-TREASURE-HUNT.md#c3-075--the-crystal-key) after the final task.
- [ ] **Area-specific check:** Swap the toy flag for the Crystal Key, grant it once, show 3 of 6, and deliver Haldor’s invitation.
- [ ] **Complete area 075:** Pass the shared definition of done, save evidence, and verify the Area 076 readiness boundary.

### Chapter 3 completion check

- [ ] All 25 area completion boxes for 051–075 are checked, with evidence in their briefs.
- [ ] All five milestone scenes are in the correct order and play once in normal progression.
- [ ] Verify the chapter reward: Crystal Key at Area 075. Reload and replay without duplicate grants.
- [ ] Review the chapter’s ordinary discoveries, character continuity, theme, task pacing, and real-player screenshots.
- [ ] Check the 075-to-076 transition when the next area is ready; until then, check the locked-content message.

## Chapter 4 — The Royal Adventure Games
**Order:** Areas 076–100. **Payoff:** Snowflake Key at Area 100.  
**Story reference:** [Chapter 4 outline and screenplay](GREAT-ROYAL-TREASURE-HUNT.md#9-chapter-4--the-royal-adventure-games).

### Chapter entry and feature checkpoints

- [ ] Verify the 075-to-076 handoff and retained three-key progress.
- [ ] Prepare the bright northern setting and friendly Haldor presentation before Area 080.
- [ ] By Area 080, implement five distinct team-pennant flags, their visible rules, and the shared finish-arch display.

### Area 076 — The Snowflake Road

**Existing budget:** 5 tasks · 6 stars · 6 assigned levels.

- [ ] **Tasks and script:** Prepare cheerful travel signs and warm rest stops.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** Games flags lead toward Haldor's welcome tower. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 076:** Pass the shared definition of done, save evidence, and verify the Area 077 readiness boundary.

### Area 077 — Welcome Watchtower

**Existing budget:** 5 tasks · 6 stars · 6 assigned levels.

- [ ] **Tasks and script:** Hang team banners and restore the viewing telescope.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** The telescope reveals a huge field of unfinished games. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 077:** Pass the shared definition of done, save evidence, and verify the Area 078 readiness boundary.

### Area 078 — The Snow Clubhouse

**Existing budget:** 5 tasks · 7 stars · 7 assigned levels.

- [ ] **Tasks and script:** Prepare team tables and equipment racks.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** Each visitor receives a team ribbon and illustrated rule card. Deliver as the final-task reveal and short dialogue card.
- [ ] **Area-specific check:** Keep the early illustrated rule card consistent with Haldor’s explanation at Area 080.
- [ ] **Complete area 078:** Pass the shared definition of done, save evidence, and verify the Area 079 readiness boundary.

### Area 079 — The Signal Hill

**Existing budget:** 5 tasks · 7 stars · 7 assigned levels.

- [ ] **Tasks and script:** Set up colored lanterns and a welcome horn.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** The opening signal calls everyone to the snow pavilion. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 079:** Pass the shared definition of done, save evidence, and verify the Area 080 readiness boundary.

### Area 080 — Snow Pavilion

**Existing budget:** 5 tasks · 6 stars · 6 assigned levels.

- [ ] **Tasks and script:** Complete the welcome stage and team display.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** Haldor explains the games and awards the welcome pennant. Play [C4-080](GREAT-ROYAL-TREASURE-HUNT.md#c4-080--let-the-games-begin) after the final task.
- [ ] **Area-specific check:** Explain the five shared challenges before awarding the welcome pennant. Show pennant progress separately from key progress.
- [ ] **Complete area 080:** Pass the shared definition of done, save evidence, and verify the Area 081 readiness boundary.

### Area 081 — The Sled Path

**Existing budget:** 5 tasks · 7 stars · 7 assigned levels.

- [ ] **Tasks and script:** Build a gentle sled track with broad stopping areas.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** A wind-up mascot carries the next event's sign. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 081:** Pass the shared definition of done, save evidence, and verify the Area 082 readiness boundary.

### Area 082 — The Snowball Gate

**Existing budget:** 5 tasks · 8 stars · 8 assigned levels.

- [ ] **Tasks and script:** Arrange soft snowball targets and a bright arch.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** The targets point toward the skaters' clubhouse. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 082:** Pass the shared definition of done, save evidence, and verify the Area 083 readiness boundary.

### Area 083 — The Cocoa Cabin

**Existing budget:** 5 tasks · 6 stars · 6 assigned levels.

- [ ] **Tasks and script:** Prepare warm drinks, spare mittens, and team benches.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** Grimm offers a sketch for adjustable skate handles. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 083:** Pass the shared definition of done, save evidence, and verify the Area 084 readiness boundary.

### Area 084 — The Skating Pond

**Existing budget:** 5 tasks · 8 stars · 8 assigned levels.

- [ ] **Tasks and script:** Finish the marked skating circuit and viewing seats.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** Everyone practices the shared route toward the ice harbor. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 084:** Pass the shared definition of done, save evidence, and verify the Area 085 readiness boundary.

### Area 085 — Ice Harbor

**Existing budget:** 5 tasks · 7 stars · 7 assigned levels.

- [ ] **Tasks and script:** Complete the skating parade's finish deck.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** Both teams finish together and earn the skating pennant. Play [C4-085](GREAT-ROYAL-TREASURE-HUNT.md#c4-085--the-skating-team) after the final task.
- [ ] **Area-specific check:** Award the skating pennant once and retain the welcome pennant.
- [ ] **Complete area 085:** Pass the shared definition of done, save evidence, and verify the Area 086 readiness boundary.

### Area 086 — Winter Palace Gates

**Existing budget:** 5 tasks · 7 stars · 7 assigned levels.

- [ ] **Tasks and script:** Decorate the palace entrance for the next events.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** A giant kite-shaped banner points into the hall. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 086:** Pass the shared definition of done, save evidence, and verify the Area 087 readiness boundary.

### Area 087 — The Frost Hall

**Existing budget:** 5 tasks · 7 stars · 7 assigned levels.

- [ ] **Tasks and script:** Assemble kite frames and ribbon tails.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** A wind diagram shows the route to the garden. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 087:** Pass the shared definition of done, save evidence, and verify the Area 088 readiness boundary.

### Area 088 — The Guest Wing

**Existing budget:** 5 tasks · 7 stars · 7 assigned levels.

- [ ] **Tasks and script:** Prepare rooms and a kite-decorating corner.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** Leo's unusually wide kite needs an adjustable handle. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 088:** Pass the shared definition of done, save evidence, and verify the Area 089 readiness boundary.

### Area 089 — The Ice Garden

**Existing budget:** 5 tasks · 7 stars · 7 assigned levels.

- [ ] **Tasks and script:** Restore gentle fans and the kite-course markers.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** Grimm's handle design lets visitors share the big kite. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 089:** Pass the shared definition of done, save evidence, and verify the Area 090 readiness boundary.

### Area 090 — The Kite Ballroom

**Existing budget:** 5 tasks · 8 stars · 8 assigned levels.

- [ ] **Tasks and script:** Open the covered kite exhibition and flying courtyard.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** Freya leads the shared flight; the kite pennant is awarded. Play [C4-090](GREAT-ROYAL-TREASURE-HUNT.md#c4-090--freya-flies-the-enormous-kite) after the final task.
- [ ] **Area-specific check:** Award the kite pennant once; stage Freya leading the flight and both teams helping.
- [ ] **Complete area 090:** Pass the shared definition of done, save evidence, and verify the Area 091 readiness boundary.

### Area 091 — The Puzzle Lodge

**Existing budget:** 5 tasks · 7 stars · 7 assigned levels.

- [ ] **Tasks and script:** Build the first stations of a cooperative puzzle trail.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** An illustrated banner challenge begins. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 091:** Pass the shared definition of done, save evidence, and verify the Area 092 readiness boundary.

### Area 092 — The Team Workshop

**Existing budget:** 5 tasks · 7 stars · 7 assigned levels.

- [ ] **Tasks and script:** Make shape flags and repair the wind-up mascot's wheels.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** The mascot demonstrates the order of the next flags. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 092:** Pass the shared definition of done, save evidence, and verify the Area 093 readiness boundary.

### Area 093 — The Podium Steps

**Existing budget:** 5 tasks · 7 stars · 7 assigned levels.

- [ ] **Tasks and script:** Build a broad podium and group-photo platform.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** Several small winners' stands connect into one long team platform. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 093:** Pass the shared definition of done, save evidence, and verify the Area 094 readiness boundary.

### Area 094 — The Trophy Room

**Existing budget:** 5 tasks · 8 stars · 8 assigned levels.

- [ ] **Tasks and script:** Prepare the prize display and polish the key case.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** The Snowflake Key's case shows a tiny upside-down tower. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 094:** Pass the shared definition of done, save evidence, and verify the Area 095 readiness boundary.

### Area 095 — Hall of Banners

**Existing budget:** 5 tasks · 7 stars · 7 assigned levels.

- [ ] **Tasks and script:** Complete the teams' shared picture banner.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** The banner pennant is awarded; the final relay is explained. Play [C4-095](GREAT-ROYAL-TREASURE-HUNT.md#c4-095--a-place-for-every-flag) after the final task.
- [ ] **Area-specific check:** Award the banner pennant once; the teams should now have four of five pennants.
- [ ] **Complete area 095:** Pass the shared definition of done, save evidence, and verify the Area 096 readiness boundary.

### Area 096 — The Planning Table

**Existing budget:** 5 tasks · 7 stars · 7 assigned levels.

- [ ] **Tasks and script:** Arrange the relay map and choose stations together.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** Every teammate finds a job that suits them. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 096:** Pass the shared definition of done, save evidence, and verify the Area 097 readiness boundary.

### Area 097 — The Snowball Yard

**Existing budget:** 5 tasks · 7 stars · 7 assigned levels.

- [ ] **Tasks and script:** Prepare the relay's soft target station.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** Its final target opens the route to the lookout. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 097:** Pass the shared definition of done, save evidence, and verify the Area 098 readiness boundary.

### Area 098 — The Kite Lookout

**Existing budget:** 5 tasks · 9 stars · 9 assigned levels.

- [ ] **Tasks and script:** Set up a clear signal kite and safe viewing rails.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** The kite shows the mascot's route to the field. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 098:** Pass the shared definition of done, save evidence, and verify the Area 099 readiness boundary.

### Area 099 — The Relay Field

**Existing budget:** 5 tasks · 8 stars · 8 assigned levels.

- [ ] **Tasks and script:** Build the final relay stations and finish arch.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** Four pennants are mounted; one space awaits the relay pennant. Deliver as the final-task reveal and short dialogue card.
- [ ] **Area-specific check:** Show four mounted pennants and one empty slot. Do not grant the Snowflake Key before the final relay.
- [ ] **Complete area 099:** Pass the shared definition of done, save evidence, and verify the Area 100 readiness boundary.

### Area 100 — The Grand Finish

**Existing budget:** 8 tasks · 16 stars · 16 assigned levels.

- [ ] **Tasks and script:** Complete the shared relay and guide the mascot home.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** The fifth pennant opens the prize; the Snowflake Key reveals the tower route. Play [C4-100](GREAT-ROYAL-TREASURE-HUNT.md#c4-100--the-snowflake-key) after the final task.
- [ ] **Area-specific check:** Award the relay pennant, verify all five, then grant the Snowflake Key once and show 4 of 6. Keep earlier pennants across reloads.
- [ ] **Complete area 100:** Pass the shared definition of done, save evidence, and verify the Area 101 readiness boundary.

### Chapter 4 completion check

- [ ] All 25 area completion boxes for 076–100 are checked, with evidence in their briefs.
- [ ] All five milestone scenes are in the correct order and play once in normal progression.
- [ ] Verify the chapter reward: Snowflake Key at Area 100. Reload and replay without duplicate grants.
- [ ] Review the chapter’s ordinary discoveries, character continuity, theme, task pacing, and real-player screenshots.
- [ ] Verify all five team pennants and the shared games rules before the Snowflake Key award.
- [ ] Check the 100-to-101 transition when the next area is ready; until then, check the locked-content message.

## Chapter 5 — The Upside-Down Tower
**Order:** Areas 101–125. **Payoff:** Gear Key at Area 125.  
**Story reference:** [Chapter 5 outline and screenplay](GREAT-ROYAL-TREASURE-HUNT.md#10-chapter-5--the-upside-down-tower).

### Chapter entry and feature checkpoints

- [ ] Verify the 100-to-101 handoff and retained four-key progress.
- [ ] Prepare workshop art and reusable lift/floating-furniture staging by Area 105.
- [ ] Plan the Wonderworks model from the player’s actual earlier improvements; keep this workshop distinct from the tower at home.

### Area 101 — The Inventors' Gardens

**Existing budget:** 5 tasks · 6 stars · 6 assigned levels.

- [ ] **Tasks and script:** Clear paths and restore playful topiary machines.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** A sign points toward a tower with windows under its balconies. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 101:** Pass the shared definition of done, save evidence, and verify the Area 102 readiness boundary.

### Area 102 — The Winding Walkway

**Existing budget:** 5 tasks · 6 stars · 6 assigned levels.

- [ ] **Tasks and script:** Repair curved footbridges and direction markers.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** Arrows bend upward toward an unusual front gate. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 102:** Pass the shared definition of done, save evidence, and verify the Area 103 readiness boundary.

### Area 103 — The Workshop Gate

**Existing budget:** 5 tasks · 6 stars · 6 assigned levels.

- [ ] **Tasks and script:** Restore the visitor bell and colorful gear emblem.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** The gate welcomes visitors with a polite mechanical bow. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 103:** Pass the shared definition of done, save evidence, and verify the Area 104 readiness boundary.

### Area 104 — The Sketch Library

**Existing budget:** 5 tasks · 7 stars · 7 assigned levels.

- [ ] **Tasks and script:** Organize picture plans and rolling display boards.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** A bell-shaped plan marks the tower's first lift. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 104:** Pass the shared definition of done, save evidence, and verify the Area 105 readiness boundary.

### Area 105 — The Humming Lift

**Existing budget:** 5 tasks · 7 stars · 7 assigned levels.

- [ ] **Tasks and script:** Restore a lift that responds to a simple tune.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** The team rises into the tower and sees its upside-down furniture. Play [C5-105](GREAT-ROYAL-TREASURE-HUNT.md#c5-105--the-lift-that-likes-a-tune) after the final task.
- [ ] **Area-specific check:** Demonstrate the lift’s music cue visually; show the upside-down decorations without making navigation confusing.
- [ ] **Complete area 105:** Pass the shared definition of done, save evidence, and verify the Area 106 readiness boundary.

### Area 106 — The Floating Garden

**Existing budget:** 5 tasks · 6 stars · 6 assigned levels.

- [ ] **Tasks and script:** Arrange planters on low, gently hovering platforms.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** Roots curl around a label for the bubble cellar. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 106:** Pass the shared definition of done, save evidence, and verify the Area 107 readiness boundary.

### Area 107 — The Bubble Cellar

**Existing budget:** 5 tasks · 7 stars · 7 assigned levels.

- [ ] **Tasks and script:** Restore bubble pipes and colored observation windows.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** The bubbles carry the same sun symbol as Pebble's railway. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 107:** Pass the shared definition of done, save evidence, and verify the Area 108 readiness boundary.

### Area 108 — The Curiosity Gallery

**Existing budget:** 5 tasks · 7 stars · 7 assigned levels.

- [ ] **Tasks and script:** Display miniature inventions and their picture labels.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** Five familiar places appear together on one display shelf. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 108:** Pass the shared definition of done, save evidence, and verify the Area 109 readiness boundary.

### Area 109 — The Ribbon Walk

**Existing budget:** 5 tasks · 7 stars · 7 assigned levels.

- [ ] **Tasks and script:** Repair a moving walkway and its stop buttons.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** A gear-shaped sign leads to the tower's control garden. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 109:** Pass the shared definition of done, save evidence, and verify the Area 110 readiness boundary.

### Area 110 — The Gear Garden

**Existing budget:** 5 tasks · 7 stars · 7 assigned levels.

- [ ] **Tasks and script:** Connect large, slow-turning gears and a display board.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** A blueprint names the connected invention: the Wonderworks. Play [C5-110](GREAT-ROYAL-TREASURE-HUNT.md#c5-110--what-they-have-been-building) after the final task.
- [ ] **Area-specific check:** Reveal the Wonderworks connections using recognizable places from the first four chapters, while keeping the final transformation hidden.
- [ ] **Complete area 110:** Pass the shared definition of done, save evidence, and verify the Area 111 readiness boundary.

### Area 111 — The Reflection Pool

**Existing budget:** 5 tasks · 8 stars · 8 assigned levels.

- [ ] **Tasks and script:** Restore a shallow mirror pool and stepping platforms.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** Reflections make an upside-down label readable. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 111:** Pass the shared definition of done, save evidence, and verify the Area 112 readiness boundary.

### Area 112 — The Ceiling Hall

**Existing budget:** 5 tasks · 7 stars · 7 assigned levels.

- [ ] **Tasks and script:** Arrange furniture on visible overhead tracks.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** Grimm spots a control panel beside a floating sofa. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 112:** Pass the shared definition of done, save evidence, and verify the Area 113 readiness boundary.

### Area 113 — The Toy Room

**Existing budget:** 5 tasks · 8 stars · 8 assigned levels.

- [ ] **Tasks and script:** Repair wind-up toys and add soft play mats.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** A toy demonstrates the sofa's rise-and-lower mechanism. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 113:** Pass the shared definition of done, save evidence, and verify the Area 114 readiness boundary.

### Area 114 — The Portrait Gallery

**Existing budget:** 5 tasks · 7 stars · 7 assigned levels.

- [ ] **Tasks and script:** Hang animated inventor portraits and restore their buttons.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** One portrait demonstrates taking turns at the controls. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 114:** Pass the shared definition of done, save evidence, and verify the Area 115 readiness boundary.

### Area 115 — The Floating Lounge

**Existing budget:** 5 tasks · 8 stars · 8 assigned levels.

- [ ] **Tasks and script:** Finish the sofa's landing platform and control station.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** Grimm tries the wrong lever and asks the team to guide him down. Play [C5-115](GREAT-ROYAL-TREASURE-HUNT.md#c5-115--a-sofa-above-the-floor) after the final task.
- [ ] **Area-specific check:** Use the previously demonstrated lift controls to bring Grimm’s sofa down gently; frame the moment around asking for help.
- [ ] **Complete area 115:** Pass the shared definition of done, save evidence, and verify the Area 116 readiness boundary.

### Area 116 — The Pattern Room

**Existing budget:** 5 tasks · 7 stars · 7 assigned levels.

- [ ] **Tasks and script:** Sort picture cards into the Wonderworks' operating sequence.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** The correct sequence needs several controls used together. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 116:** Pass the shared definition of done, save evidence, and verify the Area 117 readiness boundary.

### Area 117 — The Lantern Studio

**Existing budget:** 5 tasks · 7 stars · 7 assigned levels.

- [ ] **Tasks and script:** Create light patterns and colored projection screens.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** Projected lines connect the railway, parade, and castle. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 117:** Pass the shared definition of done, save evidence, and verify the Area 118 readiness boundary.

### Area 118 — The Echo Chamber

**Existing budget:** 5 tasks · 8 stars · 8 assigned levels.

- [ ] **Tasks and script:** Tune friendly echo pipes and a speaking trumpet.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** The pipes repeat the music-room tune in a new rhythm. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 118:** Pass the shared definition of done, save evidence, and verify the Area 119 readiness boundary.

### Area 119 — The Great Gear Hall

**Existing budget:** 5 tasks · 7 stars · 7 assigned levels.

- [ ] **Tasks and script:** Restore a large transparent gear assembly.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** An empty gear space matches one of Grimm's notebook sketches. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 119:** Pass the shared definition of done, save evidence, and verify the Area 120 readiness boundary.

### Area 120 — The Hidden Workshop

**Existing budget:** 5 tasks · 8 stars · 8 assigned levels.

- [ ] **Tasks and script:** Build the missing gear assembly using Grimm's shared design.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** The model begins working; Grimm accepts a place on the team. Play [C5-120](GREAT-ROYAL-TREASURE-HUNT.md#c5-120--the-notebook-opens) after the final task.
- [ ] **Area-specific check:** Record Grimm’s accepted teammate role so subsequent dialogue and shared inventions reflect it.
- [ ] **Complete area 120:** Pass the shared definition of done, save evidence, and verify the Area 121 readiness boundary.

### Area 121 — The Little Inventors' Room

**Existing budget:** 5 tasks · 7 stars · 7 assigned levels.

- [ ] **Tasks and script:** Set up a hands-on model-building table for visitors.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** Simple models explain how the final stations work together. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 121:** Pass the shared definition of done, save evidence, and verify the Area 122 readiness boundary.

### Area 122 — The Rainbow Paint House

**Existing budget:** 5 tasks · 7 stars · 7 assigned levels.

- [ ] **Tasks and script:** Restore paint mixers and color the kingdom model.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** The model's castle matches the player's restored rooms. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 122:** Pass the shared definition of done, save evidence, and verify the Area 123 readiness boundary.

### Area 123 — The Windmill Balcony

**Existing budget:** 5 tasks · 8 stars · 8 assigned levels.

- [ ] **Tasks and script:** Restore a wind wheel that powers the model's final section.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** A beam of light points toward the blueprint gallery. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 123:** Pass the shared definition of done, save evidence, and verify the Area 124 readiness boundary.

### Area 124 — The Blueprint Gallery

**Existing budget:** 5 tasks · 7 stars · 7 assigned levels.

- [ ] **Tasks and script:** Assemble the full illustrated Wonderworks plan.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** The plan shows five keys opening the mystery tower's outer door. Deliver as the final-task reveal and short dialogue card.
- [ ] **Area-specific check:** Show the five-key outer-door diagram and the separate sixth-key workshop before the team returns home.
- [ ] **Complete area 124:** Pass the shared definition of done, save evidence, and verify the Area 125 readiness boundary.

### Area 125 — The Inventors' Observatory

**Existing budget:** 8 tasks · 16 stars · 16 assigned levels.

- [ ] **Tasks and script:** Complete the kingdom model and aim its viewing lens home.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** The Gear Key appears; the final route says to bring their friends. Play [C5-125](GREAT-ROYAL-TREASURE-HUNT.md#c5-125--the-gear-key) after the final task.
- [ ] **Area-specific check:** Grant the Gear Key once, show 5 of 6, and display the instruction to bring their friends back to the castle.
- [ ] **Complete area 125:** Pass the shared definition of done, save evidence, and verify the Area 126 readiness boundary.

### Chapter 5 completion check

- [ ] All 25 area completion boxes for 101–125 are checked, with evidence in their briefs.
- [ ] All five milestone scenes are in the correct order and play once in normal progression.
- [ ] Verify the chapter reward: Gear Key at Area 125. Reload and replay without duplicate grants.
- [ ] Review the chapter’s ordinary discoveries, character continuity, theme, task pacing, and real-player screenshots.
- [ ] Check the 125-to-126 transition when the next area is ready; until then, check the locked-content message.

## Chapter 6 — The Biggest Surprise
**Order:** Areas 126–150. **Payoff:** Sunburst Key at Area 145; full festival at Area 150.  
**Story reference:** [Chapter 6 outline and screenplay](GREAT-ROYAL-TREASURE-HUNT.md#11-chapter-6--the-biggest-surprise).

### Chapter entry and feature checkpoints

- [ ] Verify the 125-to-126 handoff and retained five-key progress.
- [ ] Map each return location to its earlier completed room state; create additions without erasing the original improvements.
- [ ] Stage the returning cast as they arrive; prepare explicit scenes S142 and S149 as their areas approach.

### Area 126 — The Festival Approach

**Existing budget:** 5 tasks · 7 stars · 7 assigned levels.

- [ ] **Tasks and script:** Add welcome arches outside the completed castle walls.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** The first invitation replies arrive on Pip's map. Deliver as the final-task reveal and short dialogue card.
- [ ] **Area-specific check:** Start the festival expansion from the restored castle baseline. Opening the new chapter must not reset any earlier area state.
- [ ] **Complete area 126:** Pass the shared definition of done, save evidence, and verify the Area 127 readiness boundary.

### Area 127 — The Guest Meadow

**Existing budget:** 5 tasks · 8 stars · 8 assigned levels.

- [ ] **Tasks and script:** Build picnic spaces and a campsite for visiting performers.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** The village band arrives with its instruments. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 127:** Pass the shared definition of done, save evidence, and verify the Area 128 readiness boundary.

### Area 128 — The Lantern Gatehouse

**Existing budget:** 5 tasks · 9 stars · 9 assigned levels.

- [ ] **Tasks and script:** Add a new lantern canopy to the restored entrance.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** The parade float appears beneath the lights. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 128:** Pass the shared definition of done, save evidence, and verify the Area 129 readiness boundary.

### Area 129 — The Parade Lane

**Existing budget:** 5 tasks · 7 stars · 7 assigned levels.

- [ ] **Tasks and script:** Connect the castle entrance to the expanded procession route.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** The float's nose ornament becomes a festival lamp. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 129:** Pass the shared definition of done, save evidence, and verify the Area 130 readiness boundary.

### Area 130 — The Confetti Workshop

**Existing budget:** 5 tasks · 8 stars · 8 assigned levels.

- [ ] **Tasks and script:** Prepare paper decorations and gentle confetti fans.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** The village friends reunite with Leo; more guests are on their way. Play [C6-130](GREAT-ROYAL-TREASURE-HUNT.md#c6-130--the-parade-comes-home) after the final task.
- [ ] **Area-specific check:** Bring the float and village band home with their Chapter 2 designs and introduce Grimm’s hat-shaped float.
- [ ] **Complete area 130:** Pass the shared definition of done, save evidence, and verify the Area 131 readiness boundary.

### Area 131 — The Rest-and-Refresh Tent

**Existing budget:** 6 tasks · 9 stars · 9 assigned levels.

- [ ] **Tasks and script:** Build shaded seating, drinking-water stations, and a quiet corner.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** Alfred has a welcoming place for visitors who need a break. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 131:** Pass the shared definition of done, save evidence, and verify the Area 132 readiness boundary.

### Area 132 — The Rainbow Walk

**Existing budget:** 5 tasks · 7 stars · 7 assigned levels.

- [ ] **Tasks and script:** Add viewing rails and colorful ribbons to the wall walk.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** Guests see the Crystal Express approaching below. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 132:** Pass the shared definition of done, save evidence, and verify the Area 133 readiness boundary.

### Area 133 — The Pony Parade Yard

**Existing budget:** 5 tasks · 8 stars · 8 assigned levels.

- [ ] **Tasks and script:** Expand the restored stables with a grooming and ribbon station.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** The original parade ponies join the growing procession. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 133:** Pass the shared definition of done, save evidence, and verify the Area 134 readiness boundary.

### Area 134 — Carousel Courtyard

**Existing budget:** 5 tasks · 9 stars · 9 assigned levels.

- [ ] **Tasks and script:** Prepare the circular carousel platform and surrounding paths.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** The music-room carousel model matches the new platform. Deliver as the final-task reveal and short dialogue card.
- [ ] **Area-specific check:** Prepare the carousel base now so the final animation can raise the carousel from the exact location the player built.
- [ ] **Complete area 134:** Pass the shared definition of done, save evidence, and verify the Area 135 readiness boundary.

### Area 135 — The Welcome Hall

**Existing budget:** 5 tasks · 7 stars · 7 assigned levels.

- [ ] **Tasks and script:** Expand the entrance hall's guest displays and map stand.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** Pebble and Haldor arrive; the adventure team is together. Play [C6-135](GREAT-ROYAL-TREASURE-HUNT.md#c6-135--every-friend-has-a-place) after the final task.
- [ ] **Area-specific check:** Bring Pebble and Haldor into the active festival cast and map their roles to the final stations.
- [ ] **Complete area 135:** Pass the shared definition of done, save evidence, and verify the Area 136 readiness boundary.

### Area 136 — The Festival Kitchen

**Existing budget:** 5 tasks · 7 stars · 7 assigned levels.

- [ ] **Tasks and script:** Add serving windows and a cake-decorating table.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** Cook unveils a six-key cake topped with a tiny Mimi. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 136:** Pass the shared definition of done, save evidence, and verify the Area 137 readiness boundary.

### Area 137 — The Rainbow Garden

**Existing budget:** 5 tasks · 7 stars · 7 assigned levels.

- [ ] **Tasks and script:** Add flower tunnels and the festival's bubble-fountain pipes.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** A test bubble reflects the mystery tower's balcony. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 137:** Pass the shared definition of done, save evidence, and verify the Area 138 readiness boundary.

### Area 138 — The Story Library

**Existing budget:** 6 tasks · 10 stars · 10 assigned levels.

- [ ] **Tasks and script:** Add a picture-book stage and a display of adventure keepsakes.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** Visitors can see the journey's completed clues together. Deliver as the final-task reveal and short dialogue card.
- [ ] **Area-specific check:** Display the actual adventure keepsakes and completed clues, with no remnants of the old evidence/abduction story.
- [ ] **Complete area 138:** Pass the shared definition of done, save evidence, and verify the Area 139 readiness boundary.

### Area 139 — The Bubble Pool

**Existing budget:** 5 tasks · 8 stars · 8 assigned levels.

- [ ] **Tasks and script:** Prepare toy-boat routes and the Wonderworks preview outlet.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** The preview's operating diagram points toward the costume lounge. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 139:** Pass the shared definition of done, save evidence, and verify the Area 140 readiness boundary.

### Area 140 — The Festival Costume Lounge

**Existing budget:** 5 tasks · 8 stars · 8 assigned levels.

- [ ] **Tasks and script:** Prepare dress-up rails and the preview control station.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** Grimm's hat floats away in a bubble; he asks Freya to share the controls. Play [C6-140](GREAT-ROYAL-TREASURE-HUNT.md#c6-140--the-floating-hat) after the final task.
- [ ] **Area-specific check:** Stage the floating hat and Grimm sharing controls with Freya. Keep the hat’s location consistent with its recovery at 142.
- [ ] **Complete area 140:** Pass the shared definition of done, save evidence, and verify the Area 141 readiness boundary.

### Area 141 — The Tower Garden

**Existing budget:** 5 tasks · 8 stars · 8 assigned levels.

- [ ] **Tasks and script:** Open the garden route seen behind the throne in Chapter 1.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** The mystery tower's outer door is finally within reach. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 141:** Pass the shared definition of done, save evidence, and verify the Area 142 readiness boundary.

### Area 142 — The Five-Key Door

**Existing budget:** 5 tasks · 9 stars · 9 assigned levels.

- [ ] **Tasks and script:** Restore the entrance platform and fit the first five keys.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** The outer door opens onto the shared challenge stations. Play [S142](GREAT-ROYAL-TREASURE-HUNT.md#s142--through-the-five-key-door) after the final task.
- [ ] **Area-specific check:** Run S142 even though this is not a fifth area. Verify the first five keys are owned, unlock the outer door, then retain all five keys for the main console.
- [ ] **Complete area 142:** Pass the shared definition of done, save evidence, and verify the Area 143 readiness boundary.

### Area 143 — The Music-and-Motion Room

**Existing budget:** 5 tasks · 9 stars · 9 assigned levels.

- [ ] **Tasks and script:** Prepare the music pipes and parade-powered motion wheel.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** The band and Roderick's helpers make two station symbols glow. Deliver as the final-task reveal and short dialogue card.
- [ ] **Area-specific check:** Persist the music and movement stations; show the band and Roderick’s helpers operating them.
- [ ] **Complete area 143:** Pass the shared definition of done, save evidence, and verify the Area 144 readiness boundary.

### Area 144 — The Light-and-Wind Room

**Existing budget:** 5 tasks · 9 stars · 9 assigned levels.

- [ ] **Tasks and script:** Align lantern mirrors, bubble lenses, and Haldor's wind wheel.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** Pebble and Haldor light two more station symbols. Deliver as the final-task reveal and short dialogue card.
- [ ] **Area-specific check:** Persist the light and wind stations; show Pebble and Haldor contributing and retain the two earlier stations.
- [ ] **Complete area 144:** Pass the shared definition of done, save evidence, and verify the Area 145 readiness boundary.

### Area 145 — The Sunburst Workshop

**Existing budget:** 5 tasks · 10 stars · 10 assigned levels.

- [ ] **Tasks and script:** Connect Grimm's final gear and coordinate all five stations.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** The Sunburst Key appears; all six keys have been found. Play [C6-145](GREAT-ROYAL-TREASURE-HUNT.md#c6-145--the-sunburst-key) after the final task.
- [ ] **Area-specific check:** Connect the invention station, verify all five stations, and grant the Sunburst Key once. Show 6 of 6 and keep the final festival closed until Area 150.
- [ ] **Complete area 145:** Pass the shared definition of done, save evidence, and verify the Area 146 readiness boundary.

### Area 146 — The Grand Welcome Gates

**Existing budget:** 5 tasks · 10 stars · 10 assigned levels.

- [ ] **Tasks and script:** Add the final festival sign and open every visitor lane.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** All guest groups are ready for the main celebration. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 146:** Pass the shared definition of done, save evidence, and verify the Area 147 readiness boundary.

### Area 147 — The Wonder Garden

**Existing budget:** 5 tasks · 8 stars · 8 assigned levels.

- [ ] **Tasks and script:** Finish soft slide landings and fountain viewing paths.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** A closed tower panel shows the outline of a curled slide. Deliver as the final-task reveal and short dialogue card.
- [ ] **Area-specific check:** Create the slide landing artwork and geometry required by the finale’s tower-slide reveal.
- [ ] **Complete area 147:** Pass the shared definition of done, save evidence, and verify the Area 148 readiness boundary.

### Area 148 — The Celebration Ballroom

**Existing budget:** 6 tasks · 12 stars · 12 assigned levels.

- [ ] **Tasks and script:** Prepare the band platform and everyone’s festival costumes.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** The familiar castle tune becomes the festival's opening song. Deliver as the final-task reveal and short dialogue card.
- [ ] **Complete area 148:** Pass the shared definition of done, save evidence, and verify the Area 149 readiness boundary.

### Area 149 — The Wonderworks Console

**Existing budget:** 5 tasks · 10 stars · 10 assigned levels.

- [ ] **Tasks and script:** Install all six keys and connect the final controls.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** The golden button rises in the Grand Courtyard. Play [S149](GREAT-ROYAL-TREASURE-HUNT.md#s149--press-for-wonder) after the final task.
- [ ] **Area-specific check:** Run S149 explicitly, verify all six keys, persist the ready console, and show the golden button. Its appearance does not finish Area 150.
- [ ] **Complete area 149:** Pass the shared definition of done, save evidence, and verify the Area 150 readiness boundary.

### Area 150 — The Grand Courtyard

**Existing budget:** 8 tasks · 18 stars · 18 assigned levels.

- [ ] **Tasks and script:** Unveil the celebration space and open the Wonderworks.
- [ ] **Art:** Integrate room states, task icons, repair regions, and the required character/prop staging.
- [ ] **Discovery:** Mimi presses the button; the full festival opens; Pip reveals a future adventure. Play [C6-150](GREAT-ROYAL-TREASURE-HUNT.md#c6-150--the-biggest-surprise) after the final task.
- [ ] **Area-specific check:** Integrate the button, transformation, celebration, and epilogue segments. Match the seven transformation reveals to the areas the player built.
- [ ] **Area-specific check:** Persist campaign completion and the proposed “Our Grand Adventure” keepsake if included. Skipping or replaying the ending must preserve a single completed outcome.
- [ ] **Area-specific check:** Return to a usable completed festival scene. Keep the flying island as a teaser until its own playable content exists.
- [ ] **Complete area 150:** Pass the shared definition of done, save evidence, and verify the completed-festival return path.

### Chapter 6 completion check

- [ ] All 25 area completion boxes for 126–150 are checked, with evidence in their briefs.
- [ ] All five milestone scenes are in the correct order and play once in normal progression.
- [ ] Verify the chapter reward: Sunburst Key at Area 145; full festival at Area 150. Reload and replay without duplicate grants.
- [ ] Review the chapter’s ordinary discoveries, character continuity, theme, task pacing, and real-player screenshots.
- [ ] Verify S142 and S149, all five final stations, the seven transformation reveals, and the epilogue after completion.
- [ ] Return to the completed festival without unlocking an unfinished flying-island area.

## Full-game release checks

Start this pass after the 150 individual areas are complete. It checks the combined campaign and its final build; it does not replace the area-by-area checks.

- [ ] **Complete content:** All 150 new area names, goals, tasks, task responses, room states, discoveries, and navigation destinations are present. Campaign totals match the authored task and level assignments.
- [ ] **Complete scripts:** Verify `OPEN-01`, all 30 milestone scenes, `S142`, `S149`, the Area 150 finale segments, and the epilogue. Check ordinary area discovery cards as well as major scenes.
- [ ] **Correct keys:** Verify grants at 025, 050, 075, 100, 125, and 145. The visible count progresses from zero to six without early awards, lost keys, or replay duplication.
- [ ] **Correct final sequence:** Five keys open the outer door at 142; all five stations unlock the Sunburst Key at 145; six keys activate the console at 149; the festival opens at 150.
- [ ] **Safe progress recovery:** Test a fresh campaign, midway saves, an interrupted task reveal, an interrupted key scene, and a completed-campaign save. Preserve legacy story progress and shared preferences when switching development campaigns.
- [ ] **No old-story leakage:** Inspect the generated new-campaign content and presentation paths for old opening lines, missing-queen text, hostile Grimm/Haldor dialogue, sabotage narration, old chapter names, and inappropriate fallback art. Old source files may remain as references without appearing in the new campaign.
- [ ] **Consistent gameplay:** Validate every assigned board and target; verify star budgets across every area. Run focused difficulty checks on changed boards and representative chapter transitions, then inspect the actual playthrough output.
- [ ] **Protected effects:** Run the VFX integrity check and relevant C# and Unity checks. Verify match, cascade, powerup, repair, and victory presentation in a real player.
- [ ] **Readable presentation:** Inspect the supported phone layouts, portrait composition, scene skip controls, text wrapping, task markers, key shapes, and final celebration. Confirm visual musical clues can be understood without new voice or soundtrack assets.
- [ ] **Reliable content loading:** Check missing-asset reporting, campaign caches, scene cleanup, and memory behavior when moving between chapters with different large backgrounds and characters.
- [ ] **Completed-game navigation:** Confirm the player can return home and revisit completed areas without spending stars twice or replaying mandatory scenes. The flying island remains a resolved ending's optional teaser.
- [ ] **Reproducible build:** Compile the authored campaign into a clean output, validate it, and confirm a second compile produces the same content. Record the exact commands and build version.
- [ ] **Reviewable delivery:** Create the final playable build and short release notes covering the new story, known limitations, and validation evidence. Update this dashboard only after these checks pass.

## Optional polish after the playable campaign

These items are separate from the completion requirements above. Keep essential plot information in the initial implementation, even if its first presentation uses illustrated scenes and existing effects.

- [ ] Add bespoke chapter music and a recorded version of the recurring musical clue.
- [ ] Add recorded character voices while retaining readable dialogue.
- [ ] Expand illustrated set pieces into richer animation without changing their saved outcomes.
- [ ] Add optional keepsake browsing and extra character idle reactions.
- [ ] Plan the flying-island adventure as a separate campaign with its own area checklist.

**Execution order:** Minimum foundation → Area 001 → Area 002 → continue in numerical order → chapter checks as each chapter finishes → full-game release checks.

### Royal Storybook visual-fidelity correction — 2026-09-12

The owner requested the four implemented screens match the approved HTML art,
not just its palette. The decorative controls now use native cutouts from that
exact artwork: title ribbons, Pip, victory art, blue buttons, task/booster/nav
icons and paper folds. Dynamic text uses serif display lettering; task rows match
the pictured full-row action and star price. Real goals, inventory, repair state,
board simulation and rewards remain authoritative.

The paired review is [Royal Storybook — Design and Game](../../art-review/treasure-hunt/royal-storybook-fidelity/index.html).
Area 1 remains the only playable area. Owner review of the revised build is next.

### Royal Storybook separated artwork — 2026-09-12

The approved artwork has been packaged into 33 transparent UI sprites. Complete
paper surfaces, blank buttons/ribbons, wallet badges, task containers, goal well,
booster tiles and dock now replace the pieced-together approximations. Original
Pip, fixed title art, star, buttons and icons are separate source cutouts. Values
and interactions remain live. Body text uses the existing semibold face without
an extra synthetic bold weight.

[Layered-art comparison and asset gallery](../../art-review/treasure-hunt/royal-storybook-layers/index.html)
records the implementation and its remaining content/aspect-ratio differences.
Area 1 transitions, dialogue, task costs and Area 2 gating remain unchanged.

The owner subsequently requested removal of the outer phone frame. The shared
gold screen-border overlay has been removed from the game.
