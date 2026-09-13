# Area 1 door, flags and lanterns — in-game validation

**Implemented and verified — 2026-09-12.** The owner approved the [three transition previews](../remaining-task-previews/README.md) and requested implementation for Area 1.

- **Door:** frame seats, eight oak sections rise into place, brass hinges and ring handles arrive with small glints.
- **Flags:** mounts seat, banners unfurl in sequence, ripple, and settle.
- **Lanterns:** lamps kindle left then right, warm light blooms locally, and the smiling sun glints before the existing discovery.

`StoryGateTransition` uses the current room sprites, two UI meshes, a 1024 × 512 non-readable parts atlas, and the `LanternLight` UI shader. `tools/gate_transition_build.cjs` bakes the approval preview's temporary layers. Original room-image hashes match the atlas manifest; no room painting or gameplay video was added or replaced. Each transition runs for 4.55 seconds and supports tapping to skip. The existing vine transition remains in place.

The task panel fades away during the work. Door and flag dialogue follows completion; the final task retains Pip's line and the sun-clue discovery. A purchase spends and saves its one star before animation. Repeat purchases are blocked during playback, and normal completion or skip reveals the purchased room state and removes temporary layers.

## Real-player results

The normal review continued from the previously earned, completed-vine checkpoint and won boards 2–4 using legal swaps. Focused runs copied the unspent-star checkpoints earned by that run. All saves were isolated from the player's progress; no stars or wins were granted by the review.

| Run | Resolution during transition | Final wins / tasks / stars | Result |
|---|---|---|---|
| [Normal tasks 2–4](normal/runtime-check.json) | 393 × 852 | 4 / 4 / 0 | Passed |
| [Small-screen door](small-task-2/runtime-check.json) | 320 × 568 | 2 / 2 / 0 | Passed |
| [Small-screen flags](small-task-3/runtime-check.json) | 320 × 568 | 3 / 3 / 0 | Passed |
| [Small-screen lanterns](small-task-4/runtime-check.json) | 320 × 568 | 4 / 4 / 0 | Passed |
| [Early lantern skip](skip-task-4/runtime-check.json) | 393 × 852 | 4 / 4 / 0 | Passed |

All five reports contain zero runtime errors and keep Area 2 locked. Checks cover native-effect attachment, duplicate-purchase rejection, layer cleanup, completion dialogue, save reload, and final discovery state. Early skip taps at 0.15 seconds and checks that playback and its layers are gone 0.5 seconds later. The full run also replays the final discovery.

Visually inspected working and settling captures for all three effects at both resolutions, the sun glint, task dialogue, completed room, and final clue. Example captures: [door assembly](normal/door-working.png), [flags unfurling](small-task-3/flags-working.png), [left lamp ignition](normal/lanterns-working.png), [sun glint](small-task-4/repair-4.png), and [discovery](normal/discovery-1.png).

## Build and content checks

- Treasure Hunt compiler consistency: passed; 1 ready area, 4 tasks, 4 stars.
- Compiler regression suite: 14 tests passed, including missing transition atlas/layout rejection.
- Unity campaign tests: 0 failures, including transition keys, packaged resources, non-readable atlas and part bounds, economy, dialogue, save behavior, and the locked successor.
- C# typecheck: 0 errors, 96 warnings.
- macOS development build: 0 errors, 379 warnings; Unity 6000.5.10f1.
- Protected kit VFX and launch-argument checks: passed. No imported area repair VideoClips were restored.
- The tested player replaces `Builds/Mac/KingLeo.app`; launch it through `tools/run_player.sh`.

The phone-size checks use the macOS player at the listed window resolutions; they are not physical-device performance measurements.
