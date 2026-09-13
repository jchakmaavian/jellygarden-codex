# Area 001 validation — 2026-09-12

## Automated checks

| Check | Result |
|---|---|
| New campaign compiler `--check` | Pass: 150 catalogue areas, 1 ready area, 4 tasks, 4 stars, 4 boards. |
| Authoring regression tests | Pass: 10 cases, including broken budgets, duplicate board rewards, task order, non-speaking characters, invalid crops, missing required task art, and tuning an unassigned board. |
| Unity `TreasureHuntTests.RunHeadless` | Pass: 0 failures, including save round-trips, reward deduplication, ordered purchases, skip/replay/recovery, portraits, navigation sprites, and readiness. |
| C# typecheck | Pass: 0 errors, 74 compiler warnings. |
| macOS player build | Pass: 0 errors; Unity reported 379 warnings. |
| Protected VFX check | Pass: 34 prefabs, 42 animation clips, and 2 particle prefabs preserved. |
| Launch configuration | Pass: shared 393 × 852 portrait window settings and 2× captures. |
| Boards 1–4 lint | Pass: 0 problems. |
| Legacy content validation | Pass: original 150 areas, 806 tasks, and 1,208 boards remain consistent. |

## Real-player review

The review uses `StoryPlaytest`'s legal-swap player and the real Continue and repair button handlers, with a separate timestamped save. No forced wins, injected stars, or direct task-completion calls fund this playthrough. It reloads the story save after every repair.

The initial review completed three boards/repairs, then lost board 4 at 660/1000 points with its original 16-move limit. `initial-difficulty-check.json` records that finding. Campaign move floors are now 20, 20, 24, and 36; the actual board and target files were not changed.

Visual review identified missing shared navigation sprites, undersized map labels, and overlap between Pip's home card and the progress badge. Those were corrected. The task list now scrolls to its next unfinished repair on opening.

Final playthrough: **PASS — four real wins, four purchased repairs, zero remaining stars, Area 002 locked, and zero runtime errors.** See `runtime-check.json`. The score results were 1230, 990, 940, and 1940. The saved campaign contains both completed scene flags and all four task and level records. Screenshots use actual player rendering at 393 × 852 and 320 × 568, captured through `StoryWindow.Capture`.

## Scope

Area 001 is the only playable new area. The next 149 areas have the new catalogue text and stay unavailable until authored and reviewed. Existing compatible cast portraits, UI art, audio, and match-3 effects are reused. Pip and all five gate states are newly generated artwork; see `assets.json` and `prompts.md`.
