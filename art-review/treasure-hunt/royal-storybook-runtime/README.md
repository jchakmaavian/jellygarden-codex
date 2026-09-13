# Royal Storybook — Unity integration review

The selected direction is [Royal Storybook](../ui-directions/v1/01-royal-storybook.png). The implementation uses native Unity text, paper panels, curved blue ribbons, gold edges and blue buttons over the existing Castle Gates paintings.

## Scope

- Home: centred title, saved progress, next-task card, Play Level action, Pip, and Home / Map / Collection dock.
- Tasks: compact bottom sheet, completed-task summary, ordered purchases and readable one-star costs.
- Pre-level: actual goals, inventory-backed boosters, and Play / Close actions.
- Victory and replay: Pip, one animated star, truthful reward text and Back to Castle.
- Gameplay: ivory target/moves panels on the existing baked HUD, booster tray and pause menu.
- Secondary screens: kingdom map, discovery album, dialogue, settings, loss, pre-play instruction cards and loading label.

Only Area 001 is playable. The four native repair transitions and the approved two opening / two ending dialogue cards are preserved. Collection reads the earned discovery scene; opening the album cannot grant a clue, key or star.

## Reproducing the review

Build with Unity 6000.5.10f1 and JellyGarden.Scripts.Story.Editor.StoryBuild.BuildMac, then use the shared launcher:

    tools/run_player.sh --exec -treasure-review -treasure-review-ui -treasure-review-dir=<absolute-output-directory>

The driver creates an isolated save, makes legal swaps, checks purchases and scene delivery, visits the screens and captures the real back buffer at 393×852 and 320×568. It never resets the player's campaign save.

Run the focused Unity checks with JellyGarden.Scripts.Story.Editor.StorybookTests.RunHeadless. They include the Treasure Hunt campaign checks, actual goal counts, booster availability and selection, navigation, one-shot callbacks, animated goal placement, collection discovery binding, victory/replay behaviour, and the Area 2 gate.

## Review history

- v1: all four boards and repairs completed, with four wins, four tasks and zero remaining stars. Visual inspection found the baked HUD still carried its old skin, goal-star motion retained an old position, a corner fold extended outside its card, and the map used descending chapter order. These were corrected.
- The v1 driver also advanced the short opening's first line after it had already finished typing. The driver now checks IsTyping before its reveal tap; dialogue content and timing are unchanged.
- v2: the complete four-level run passed with zero errors. Font fitting in the baked HUD was then refined and the completed-area sheet gained its sun-clue content.
- v3: focused first-task and ending runs passed at both phone sizes. The final completion action label was enlarged for consistency with Play.
- v4: the final completion-card capture passed with four saved tasks, zero stars, the discovery earned, and Area 2 locked. The tested app is installed in Builds/Mac/KingLeo.app.
- Final captures and validation are recorded in [validation.md](validation.md); [browse the screenshots](index.html).
