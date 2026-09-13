# Area 1 vine transition — 2026-09-12

Implemented the approved [standalone preview](../vine-transition-preview/index.html) for **Castle Gates / Clear the vines only**. The existing task panel fades out, the unchanged room paintings transition with an upward organic dissolve, leaves fall, and soft glints settle. The panel returns at 1/4 without a dialogue card. No video is imported or played.

The native effect uses a UI shader and a single leaf/glint mesh attached to the actual room. Its 301 particle entries are measured offline from the existing before/after artwork. Room textures remain non-readable. The star is spent and the repair saved before playback; skipping lands directly on the paid state. Other task effects and the Area 002 readiness boundary remain unchanged.

## Player checks

All three reviews ended with **one real win, one completed task, zero stars, Area 002 locked, and no runtime errors**:

- [Normal playback, 393 × 852](normal/runtime-check.json): fresh isolated save; board 1 won with legal swaps; opening delivered; normal UI purchase; duplicate repair attempt rejected during playback; task panel restored; save reloaded and room reopened.
- [Normal playback, 320 × 568](small-phone/runtime-check.json): replayed the first review's earned, unspent-star checkpoint; inspected the native transition on the smaller phone.
- [Early skip](skip/runtime-check.json): replayed the same earned checkpoint; skipped 0.15 seconds after purchase. The [capture about 0.65 seconds after purchase](skip/repair-1.png) already shows the cleared room and completed task panel. Reload and reopen preserve the result.

Inspected [lower-wall clearing](normal/vines-clearing-lower.png), [arch clearing](normal/vines-clearing-arch.png), [small-phone arch](small-phone/vines-clearing-arch.png), and [completed panel](normal/vines-completed-panel.png). The camera and stonework stay aligned throughout the effect.

No player save was reset or modified by the reviews. The small-phone and skip runs copied the checkpoint earned in the normal run into their own isolated save directories.

## Build and content checks

- C# typecheck: 0 errors, 86 warnings.
- Unity `TreasureHuntTests.RunHeadless`: 0 failures; checks transition resources, unchanged other task selection, economy, final discovery, campaign boundaries and legacy save preservation.
- Campaign compiler tests: 13 passed, including rejected unknown transitions, simultaneous instant/animated configuration, and missing task-specific leaf anchors.
- Campaign compiler `--check`: passed; one ready area, four tasks, four funded stars.
- Protected kit VFX check: passed; 34 prefabs, 42 animation clips and two particle prefabs preserved.
- Launch-argument check: passed.
- Particle generation: deterministic; original Treasure Hunt artwork bytes unchanged; zero installed area MP4 files.
- macOS development build: succeeded with 0 errors, 379 warnings; 582.2 MB. `Builds/Mac/KingLeo.app` refreshed from the tested scratch build.

The focused player mode is `tools/run_player.sh --exec -treasure-review -treasure-review-vines -treasure-review-dir=<absolute-output-directory>`. For an earned checkpoint, add `-treasure-review-save=<path>`; add `-treasure-review-small-vines` or `-treasure-review-skip-vines` for the other cases. All review saves remain isolated.
