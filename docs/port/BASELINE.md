# Baseline before the Royal Throne level port

Plan Task 0.2, for `docs/superpowers/plans/2026-10-12-royal-throne-level-port.md`. Task 5.3 adds an "After" column.

The baseline runs (Task 0.2 Steps 2-5) are not recorded here yet: they need Unity. Add them above the "Retired" section.

## Retired

These checks are removed by the port and are not expected in the "After" counts.

| Check | Where it was | Why |
|---|---|---|
| `StoryScreenTests.BoardsAreCentredWhateverTheirWidth` | `Story/Editor/StoryScreenTests.cs`, with `Story/StoryBoardCentre.cs` (deleted) | Royal Throne's board layout replaces `StoryBoardCentre`. |
| `StoryBoardBorderTests` (whole suite) and its menu item "Run Board Border Tests" | `Story/Editor/StoryBoardBorderTests.cs` (deleted) | Royal Throne's board backdrop replaces the Story border (D6). |
| `RocketValidationBridge` (the `Temp/rocket-validation-command` bridge) | `Story/Editor/RocketValidationBridge.cs` (deleted) | It ran main's rocket and TNT integration checks. Royal Throne's rocket and TNT replace those boosters. |
| `StorybookBoardTests` piece-colour, goal-mapping and `StorybookBoardLayout` fit checks | `Story/Editor/StorybookBoardTests.cs` | Royal Throne's piece skin, goal art and board layout replace them (D6). |
| `StoryThemeTest` backdrop and cell-fill findings | `Story/StoryThemeTest.cs` | `StoryBoardTheme` is not installed in levels (D6). |
