# Area 001 — Castle Gates

**Campaign:** The Great Royal Treasure Hunt  
**Status:** Complete — 2026-09-12. Four native repair transitions; dialogue reduced to the approved two-card opening and two-card ending. See validation records below.  
**Story reference:** [Accepted screenplay](../../GREAT-ROYAL-TREASURE-HUNT.md)  
**Authoring source:** [campaign.json](../campaign.json)

The player wins a welcoming first puzzle, meets Pip, and prepares the castle entrance for the festival. Each puzzle earns one star; each star pays for one visible improvement. The smiling sun is visible from the start and sparkles after the final task, drawing attention to the first clue toward the courtyard.

**Area objective:** Prepare the gates for the royal festival!

## Ordered tasks and boards

| Order | Task | Cost | Board | Visible result | Completion speaker |
|---|---|---|---|---|---|
| 1 | Clear the vines | 1 star | 1 | Panel fades out; vines dissolve upward with falling leaves and soft glints, revealing the cream stonework. Panel returns at 1/4. | None; return to the task panel. |
| 2 | Build the wooden door | 1 star | 2 | The frame seats, eight oak sections rise into place, then brass hinges and ring handles settle with small glints. | None; return to the task panel. |
| 3 | Decorate with royal flags | 1 star | 3 | Mounts seat; two blue-and-gold banners unfurl in sequence, ripple gently, then settle. | None; return to the task panel. |
| 4 | Light the lanterns | 1 star | 4 | The left and right lamps kindle in sequence with warm glows, followed by a brief glint on the smiling sun. | Pip and Freya in the two-card discovery. |

The passage is already open and the stonework is intact in state 0. Clearing the vines reveals plain stonework, the royal flags hang from the tower poles, and the final task highlights the existing sun emblem. The second task installs a closed oak double door within the arch. The flag and lantern states retain that door. The task-2 icon shows the complete door, and its repair region covers the full doorway.

Total: **4 tasks, 4 stars, 4 unique first-win boards**. Board 4 is allocated to this area only in the new campaign. Area 002 must receive different board identities when authored.

Boards 1–4 and their target assets remain unchanged. Board 1 uses its actual ice-block/score targets, with a campaign minimum of 20 moves and further three-move top-ups until its first victory. Matching near or clearing the board's real targets funds the later ivy repair; the board tutorial must not pretend the board contains ivy. Boards 2–4 retain their existing score goals, with campaign move floors of 20, 24, and 36 respectively. The first review lost board 4 at 660/1000 points within its original 16 moves; its larger allowance removes that early difficulty spike. No automatic wins or star grants stand in for play.

## Story delivery

- `OPEN-01`: after the first first-win reward, before the first repair. Exactly two cards: Leo says “Welcome, Puzzle Partner!”; Pip says “I’m Pip! Six keys lead to one royal surprise!” Pip's line keeps the illustrated map cue and six empty key spaces.
- The task panel fades away for each native transition. Tasks 1–3 return directly to the updated task panel without a character dialogue card. The single-star costs and saved progress are unchanged.
- `A001-DISCOVERY`: after task 4. Exactly two cards: Pip says “Look! The sun is our first clue!” with the sun close-up; Freya says “Next, let’s restore the courtyard fountain!”
- Next objective: **Bring the courtyard fountain to life.** Until Area 002 is ready, the completed gates show “More adventures are being prepared!”

The map's empty slots are a foundation presentation; no key is awarded here. Persistent inventory begins with the scheduled Area 025 feature.

Skip and normal completion share the same saved continuation. Pip's home button explicitly replays the opening; the completed task sheet can replay the discovery. Replays do not change stars, tasks, or level rewards. A saved first win without a completed opening resumes the opening; a completed area without its discovery flag resumes the discovery.

## Art manifest

All new images were generated with the built-in image tool. [Generation prompts](../../../../art-review/treasure-hunt/area-001/prompts.md) record the direction and sequential edits. Campaign PNGs are the retained full-resolution masters and Unity import sources; import settings cap sprites at 2048 with no mipmaps.

| Resource key | File or source | Purpose |
|---|---|---|
| `area_001_state0` | `Assets/JellyGarden/Resources/Story/TreasureHunt/Art/area_001_state0.png` | Initial sunny gates with ivy. |
| `area_001_state1` | Same folder, `area_001_state1.png` | Ivy cleared. |
| `area_001_state2` | Same folder, `area_001_state2.png` | Arched oak double door installed within the stone arch. |
| `area_001_state3` | Same folder, `area_001_state3.png` | Royal banners installed. |
| `area_001_state4` | Same folder, `area_001_state4.png` | Lanterns lit; completed gates. |
| `portrait_pip` | Same folder, `portrait_pip.png` | New parchment dragon with blue wings. |
| `task_001_1..4` | Explicit crops in `campaign.json` | Vines, wooden door, royal flag, and lantern icons. |
| `clue_001` | Explicit crop of state 4 | Smiling-sun discovery close-up. |
| `backdrop_castle`, `splash_background` | Alias of state 0 | Matching castle palette. |

Leo, Freya, Alfred, and Mimi reuse the existing compatible cartoon portraits. Victory UI art is explicitly shared. Room art never falls back to a legacy room. Existing match, cascade, powerup, and victory effects remain in place. All four tasks use native motion from the approved previews. There are no installed area repair videos. The mood-darkening wash is disabled for the new sunny gate states.

## Previous validation record (before the objective rewrite)

- [x] Explicit scripts, ordered tasks, exact star budget, and campaign-local board mapping.
- [x] Five gate states, four task detail sprites, Pip portrait, and sun discovery artwork integrated.
- [x] Compiler consistency and malformed-authoring regression checks pass.
- [x] Unity campaign tests pass for rewards, task order, skip/replay/recovery, saves, assets, and the unavailable successor.
- [x] Protected VFX, launch configuration, legacy content, and boards 1–4 validate.
- [x] Play all four boards with legal swaps and purchase all four repairs through the UI.
- [x] Inspect initial, intermediate, completed, and discovery screens on portrait and small-phone layouts.
- [x] Save the original runtime report and screenshots.

Illustrated staging is intentional for this first playable area. A full cupboard-door animation, animated folding map, bespoke key-slot illustrations, and new voice/audio recordings remain optional polish.

Previous player result: **4 wins, 4 repairs, 0 unspent stars, Area 002 locked, 0 runtime errors**. Reviewed the 393 × 852 and 320 × 568 layouts, including the final sun clue and explicit replay. See the [runtime report](../../../../art-review/treasure-hunt/area-001/runtime-check.json), [screenshots](../../../../art-review/treasure-hunt/area-001/), and [validation record](../../../../art-review/treasure-hunt/area-001/validation.md). These captures predate the objective rewrite and do not establish completion of the revised area.

## Objective rewrite — 2026-09-12

Selected labels: **Clear the vines**, **Build the wooden door**, **Decorate with royal flags**, and **Light the lanterns**. The goal, task labels, completion dialogue, and opening instruction are authored. Keep four one-star tasks on boards 1–4.

- [x] Update states 2–4, the task-2 icon, and its repair region to show the new wooden door and preserve it through the flag and lantern tasks.
- [x] Review the revised task labels and dialogue in the player at both phone sizes.
- [x] Confirm each task's wording and final sun clue agree with its visible state change; save updated screenshots.
- [x] Finish Area 001's presentation review before marking the area complete or starting Area 002.

## Earlier instant vine clearing — 2026-09-12 (superseded)

The owner initially requested removing the animation when pressing **Clear the vines**. The task used `instantRepair: true` until the native transition preview was approved. Its authored Freya line remains in the source without a completion dialogue card. The other three tasks retain their normal repair presentation.

The player-review save directory is now installed at `AfterAssembliesLoaded`, before `StoryBoot` reads progress. A review at the old initialization stage could read the player's progress into memory before switching to the empty review directory; that produced a false lost-star failure on reload. The earlier failed report is preserved at `wooden-door/review-before-save-fix/runtime-check.json`.

## Wooden-door completion record — 2026-09-12 (before the transition update)

**Totals:** Four one-star tasks, boards 1–4, four real wins, four purchased repairs, zero remaining stars. Area 002 remains unavailable. The corrected fresh review completed tasks 1–2; a copied earned checkpoint continued through tasks 3–4, so the final runtime report explicitly records `startingTasks: 2`.

**Changed assets and behavior:** States 2–4, the door icon crop and repair region, revised task copy, instant vine clearing, campaign-specific movie lookup, and isolated review startup/checkpoint handling. Existing sprite GUIDs, kit boards, and protected animation/VFX assets are preserved. The three legacy repair videos and four preview copies were subsequently removed at the owner's request; no area repair video files remain in the project.

- [x] **Story:** Revised task wording, intended instant first task, completion lines, final sun clue, and courtyard objective agree with the authored content.
- [x] **Budget:** Four unique first-win rewards fund four one-star repairs; duplicate rewards and purchases are rejected.
- [x] **Art:** Five room states, task icons, portraits, door repair region, and sun close-up are integrated. The door persists through the flag and lantern tasks.
- [x] **Gameplay:** All four boards won using legal swaps; tasks purchased through normal UI handlers. Vine clearing updates immediately with the task panel visible.
- [x] **Scene delivery:** Opening/discovery completion, skip, replay, and recovery pass focused checks. The final discovery and explicit replay render correctly.
- [x] **Saved progress:** Fresh and resumed review saves preserve rewards, task state, room state, and scene flags after reload.
- [x] **Navigation:** Completed gates remain the active area; Area 002 stays locked until ready.
- [x] **Presentation:** Reviewed 393 × 852 and 320 × 568 captures, including the completed room and sun clue. Treasure Hunt does not load any legacy repair movie.
- [x] **Evidence:** Updated [runtime report](../../../../art-review/treasure-hunt/area-001/wooden-door/runtime-check.json), [review and checks](../../../../art-review/treasure-hunt/area-001/wooden-door/validation.md), [screenshots](../../../../art-review/treasure-hunt/area-001/wooden-door/), and [built-in imagegen prompts](../../../../art-review/treasure-hunt/area-001/wooden-door/prompts.md).

Current owner scope: **Area 001 only**. Area 002 remains unavailable.

## Older-video removal — 2026-09-12

The owner requested removing older repair videos for every area. The inventory found three installed gate clips and four source/preview MP4s; all seven files and the Unity video import metadata were deleted. No other area had a video. The archived gallery no longer embeds or links those files. Character animation source clips are separate and retained.

Removal validation: zero failures in the focused Unity gate-art and video-removal checks for both Treasure Hunt and the legacy campaign; zero C# errors; protected VFX and the campaign compiler pass. The rebuilt macOS player contains no imported area VideoClip assets and replaces `Builds/Mac/KingLeo.app`. The archived gallery has no video players or broken local links.

## Approved native vine transition — 2026-09-12

After reviewing and approving the standalone transition, the owner requested implementation for Area 001 only. Task 1 now authors `repairTransition: "vines"`. The task panel fades out, the vines dissolve from the lower walls to the arch with falling leaves and soft glints, and the panel returns at 1/4. There is no completion dialogue card. This supersedes the instant behavior recorded above.

`StoryVineTransition` animates the existing non-readable room sprites with a UI shader and one particle mesh; `tools/vine_transition_build.py` generates leaf anchors from the art offline. No new room paintings or gameplay videos were added. The single star is spent and saved before playback, and an early skip still lands on the completed state.

Validated normal playback at 393 × 852 and 320 × 568, early skip, duplicate purchase rejection, restored task controls, save reload, and the locked Area 002 boundary. All three isolated player reports passed with one win, one task and zero stars. C# and the macOS build have zero errors; Unity campaign checks, all 13 compiler tests, and protected VFX checks pass. The default local player build has been refreshed. [Runtime screenshots and validation](../../../../art-review/treasure-hunt/area-001/vine-transition-runtime/validation.md).

## Approved door, flag and lantern transitions — 2026-09-12

Following approval of the three standalone previews, tasks 2–4 now author `repairTransition: "door"`, `"flags"`, and `"lanterns"`. The door assembles from timber sections and brass fittings; the flags unfurl and settle; the lamps kindle in sequence before a sun glint. Each runs natively for 4.55 seconds, with a fading task panel and tap-to-skip. Existing task dialogue and the final discovery follow the animation.

`StoryGateTransition` animates the approved preview layers from one offline-baked 1024 × 512 atlas; `LanternLight` reveals the existing lit-state painting around each lamp. The four source paintings retain their hashes and remain non-readable. The existing vine transition, board rewards, one-star costs, and Area 002 gate remain intact.

The normal player review continued from the completed-vine checkpoint, won boards 2–4 with legal swaps, and finished at four wins, four tasks and zero stars. Three focused small-phone runs and an early-skip final-task run used its earned checkpoints. All five reports passed with zero runtime errors. Visually reviewed all three transitions at 393 × 852 and 320 × 568, their dialogue, and the final sun clue. C# and macOS builds have zero errors; Unity campaign checks, all 14 compiler tests, protected VFX and launch checks pass. The tested local player build is refreshed. [Runtime screenshots and validation](../../../../art-review/treasure-hunt/area-001/remaining-task-runtime/validation.md).

## Approved dialogue reduction — 2026-09-12

The owner selected four short cards: Leo's welcome and Pip's introduction at the opening, then Pip's sun clue and Freya's courtyard objective at the ending. The exact current lines are listed under Story delivery above. This supersedes the longer opening, door/flag completion cards, and three-card ending in the historical records.

Tasks 1–3 now return directly to the updated task panel after their approved native transitions. All tasks author empty `speaker` / `onComplete` pairs; the final task still plays the two-card `A001-DISCOVERY`. Map and sun cues, existing scene IDs, save/replay behavior, and the four-star budget remain intact.

Three isolated player runs passed: opening replay plus door repair, flag repair, and lantern repair plus ending. The opening and ending each displayed exactly two cards. Reviewed dialogue and task panels at both phone sizes, verified save reload, and kept Area 002 locked. All 17 compiler tests, Unity campaign checks, protected VFX and launch checks pass; C# and macOS builds have zero errors. The local player build is refreshed. [Short-dialogue screenshots and validation](../../../../art-review/treasure-hunt/area-001/short-dialogue-runtime/validation.md).

## Royal Storybook UI

The selected whole-game direction is integrated with native ivory panels, royal-blue
ribbons and buttons, and soft gold edging. Home, the compact task sheet, pre-level
and victory cards, gameplay HUD, pause/settings, map, collection, dialogue and
completed-area screen share the theme. The sun-clue album reads the earned
Treasure Hunt discovery flag.

The full four-level run and final focused checks passed at both phone sizes.
See the [screenshots and validation](../../../../art-review/treasure-hunt/royal-storybook-runtime/validation.md).
The tested app is installed at Builds/Mac/KingLeo.app. Restart an already open
player to load the new scripts. Area 002 remains gated.
