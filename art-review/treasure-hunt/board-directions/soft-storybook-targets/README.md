# Soft Storybook — real targets and new power-up icons

Open [index.html](index.html) directly in a browser. This is a design preview. No Unity scripts, scenes, target assets, installed sprites or save data were changed.

## Targets found in the game

`InitScript.Target` defines four families. The current target assets contain these combinations:

| Family | Actual behaviour | Example used |
| --- | --- | --- |
| SCORE | Reach the required points. Every existing level target asset includes a score goal. | Level 3: 500 points |
| COLLECT | Match pieces with the target's colour index. | Level 17: 20 red, 25 orange, 1,000 points |
| INGREDIENT | Bring a specified treasure to the lowest playable cell in its column. The current art is a blue locket and red ring. | Level 12: 3 lockets, 1 ring, 1,000 points |
| BLOCKS | Clear the specified block target. Current target art represents ice over a piece. | Level 1: 4 frozen cells, 100 points |

Mixed goals are real: level 23 asks for 20 red pieces, 20 lockets and 4,500 points. The largest existing target list has three entries. The runtime HUD accepts four, so a separately labelled four-goal capacity sample is included; that sample is not an actual level.

Moves and time are two limit modes, not additional target families. The preview's Timed level switch demonstrates a timer and the extra-time tool without changing the objectives.

The active Treasure Hunt area 1 currently uses score and ice goals: level 1 has both, while levels 2–4 have score only. Later preserved levels supply collection and treasure examples. Nothing was unlocked or added to the active campaign.

The audit found 1,208 target assets: 1,208 with score, 269 with collection, 318 with ingredient/treasure goals and 502 with block goals. Counts overlap because levels combine families. There are 146 one-goal, 532 two-goal and 530 three-goal assets. The parser handles both `count` and `targetCount` fields.

See [game-audit.json](game-audit.json) for evidence and [target-examples.js](target-examples.js) for the exact target entries and board grids read from the project.

## Correct power-up roles

The earlier visual exploration showed pre-level boosters in the gameplay tray. This preview corrects the tray to the actual in-game tools and redraws all eight relevant icons in the Soft Storybook style.

| New icon | Runtime identity | Behaviour / location |
| --- | --- | --- |
| Extra moves | `BoostType.ExtraMoves` | Adds 5 moves during play |
| Swap glove | `BoostType.Hand` | Allows an adjacent swap without consuming a move |
| Royal hammer | `BoostType.Bomb` | Removes the selected piece; this enum is the single-piece tool |
| Colour brush | `BoostType.Random_color` | Recolours nearby normal pieces, not a whole-board shuffle |
| Rocket | `BoostType.Stripes` / striped item | Row/column special; also selected before a level |
| Area bomb | `BoostType.Packages` / package item | Area-clearing special; also selected before a level |
| Rainbow ball | `BoostType.Colorful_bomb` / BOMB item | Colour-clearing special; also selected before a level |
| Extra time | `BoostType.ExtraTime` | Adds 30 seconds on timed levels; replaces extra moves in the illustrated tray |

Runtime logic and the scene's shop descriptions were used to resolve these names. Some older story comments call Hand a hammer and Random_color a shuffle; the actual code has different behaviour. This preview does not modify that behaviour.

Sources:

- [Target enum](../../../../Assets/JellyGarden/Scripts/InitScript.cs)
- [Target completion logic](../../../../Assets/JellyGarden/Scripts/Targets/TargetLevel.cs)
- [Board limits, ingredient exits and extra moves/time](../../../../Assets/JellyGarden/Scripts/LevelManager.cs)
- [HUD target slots and score formatting](../../../../Assets/JellyGarden/Scripts/Story/StoryLevelHud.cs)
- [Hammer and recolour tool effects](../../../../Assets/JellyGarden/Scripts/GUI/BoostAnimation.cs)
- [Swap and board-special logic](../../../../Assets/JellyGarden/Scripts/Item.cs)
- [Pre-level boosters](../../../../Assets/JellyGarden/Scripts/Story/StoryBoosters.cs)
- [Configured booster descriptions](../../../../Assets/JellyGarden/Scenes/game.unity)

## Mockup controls

- Choose Classic Cards, Compact Bar or Floating Controls.
- Compare standard and small phones.
- Show progress: example counts decrease and completed collection goals gain checkmarks.
- Tall board test: apply the actual level 4 outline, 9 × 11, to all target examples.
- Timed level: show a sample timer and replace +5 moves with +30 seconds.
- Enlarge any target mockup or new power-up icon.

Normal views use the source board shapes. Token positions, progress, inventory counts, power-up placements and timed values are illustrative. The score goal shows earned/needed points when alone and remaining points when sharing the header, matching the current HUD's distinction. Small `pts` labels and a circular badge distinguish score stars from yellow collectible pieces.

The six new piece sprites follow the existing colour identity: red→heart, orange→gift, purple→butterfly, blue→droplet, green→leaf, yellow→star. Collection targets use the same mapping as the board. The existing locket, ring and ice sprites are retained so their target and board representations agree.

The header, board and tray occupy separate layout rows. The board is fitted into the measured middle region with square cells and a size cap. Extra target slots stay inside the header, preserving the reserved board area.

## New artwork

Eight separate transparent 256 × 256 icons are saved in [art/](art/). Two generated collection sheets are also retained there. The original generation files were left in place.

The built-in imagegen tool generated the icons from the Soft Storybook piece sheet as a style reference. Initial outputs had printed checkerboards; additional imagegen background-extraction passes produced the selected RGBA sheets. Browser canvas then cropped grid cells and fitted them into individual sprites without recolouring.

- [Generation prompts and selected source files](art/provenance.json)
- [Exact background-extraction prompts](art/edit-provenance.json)
- [Sprite crop metadata](art/sprite-layout.json)
- [Reproducible sprite packaging](pack-icons.cjs), using the local server on 8766 and a dedicated Chrome debug instance on 9236

Power-up artwork is installed only in this review page. Integration into Unity would require preserving sprite and colour identities, reusing the appropriate artwork in every consumer, and validating the existing animations and VFX.

## Verification

[verified/browser-check.json](verified/browser-check.json) covers 24 combinations of three layouts, two phone sizes, normal/tall boards and moves/time limits, for 96 rendered board views. Board containment, square cells, clear separation from the header/tray, target-count fit, image loading and JavaScript errors were checked. The mixed and four-goal headers were included. Screenshots are in `verified/`.
