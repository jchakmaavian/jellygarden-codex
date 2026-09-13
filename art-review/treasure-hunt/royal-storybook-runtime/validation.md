# Royal Storybook — validation

The selected UI direction is implemented as native Unity controls. Area 001 remains the only playable area.

## Checks

- Unity StorybookTests.RunHeadless: passed. Covers campaign data and economy, navigation, task affordability/order, goal counts, booster inventory and selection, double taps, close/reopen callbacks, animated goal placement, collection discovery binding, and victory/replay behaviour.
- C# typecheck: zero errors.
- Treasure Hunt compiler tests: 17 passed.
- Protected kit animation/VFX check: passed.
- Shared windowed-launch check: passed.

## Actual player runs

| Run | Result | Verified |
|---|---|---|
| [Full Area 1](v2/normal/runtime-check.json) | Passed, no errors | Four legal board wins, four repairs, zero unspent stars, both opening/ending cards, saved progress, Area 2 locked |
| [Final first task](v3/first-task/runtime-check.json) | Passed, no errors | Final HUD labels/counts, pause/resume, real first win, pre-level goals/boosters, menu navigation, vine transition, 393×852 and 320×568 |
| [Final ending](v4/ending/runtime-check.json) | Passed, no errors | Earned checkpoint, lantern transition, two ending cards, completed-area sheet, discovery album and restart persistence |

[Open the screenshot gallery](index.html). Screens show real saved progress, so counters vary by checkpoint.

The completed-area button uses the existing discovery replay action. The home clue card opens Collection. Later areas remain gated; no additional dialogue or repair videos were introduced.

The tested app is installed at Builds/Mac/KingLeo.app. An already running player continues using its loaded scripts until restarted.

[Unity check log](checks/unity-tests.log) · [Build summary](checks/build-summary.txt) · [Typecheck summary](checks/typecheck-summary.txt)
