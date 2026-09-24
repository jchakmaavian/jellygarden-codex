# Reference gameplay tool bar — 24 September 2026

Implemented in the existing gameplay HUD. Imported packaged Hammer, Arrow, Cannon and Jester Hat icons, count/plus badges, coin, panel and button artwork. Source names, path IDs and slicing data are recorded in `import-inventory.json`. The APK was inspected offline and remains unchanged.

## Working actions

| Button | Action |
|---|---|
| Royal Hammer | Select one object; apply existing cell damage. |
| Arrow | Select a cell; clear its row through existing damage and refill. |
| Cannon | Select a cell; clear its column through existing damage and refill. |
| Jester Hat | Confirm shuffle; preserve movable item instances and boosters. |

Packaged English descriptions establish these four actions; see `resources.assets.strings.txt`. Selecting or cancelling spends nothing. A valid action consumes one tool and no move. An empty stock opens the local purchase popup. Buying updates the existing balance and stock once, then selects the tool. Invalid targets and insufficient balance do not charge. Shuffle accepts only a changed arrangement with no automatic match and an available move; if no safe arrangement is found, the hat is returned.

The adapter reuses the original four UI buttons and existing damage, blocker, powerup, cascade and save systems. New enum values are appended after existing values. New inventories have separate keys; previous booster inventories and normal player progress are preserved. QA grants stock only inside the isolated test profile.

## Accuracy boundary

Artwork is imported from packaged data. Tool actions are reconstructed from verified descriptions using Royal Throne code; compiled APK logic was not imported. Exact unlock levels, purchase prices and tool animation timing were not recovered. Purchases retain the current project's first-pack quantities and prices, using its existing shared currency balance with the coin presentation. Hammer swing and sequential row/column presentation are local implementation, not claimed as recovered reference animation clips. No reference service, credentials, purchase integration or network endpoint was added.

## Verification

Unity Play Mode: `validation.txt` records the focused checks, all passing. Tested four live button bindings and visible stock labels; selection/cancellation; empty stock and insufficient balance; purchase and duplicate callback protection; actual pointer targeting of a wire blocker with Hammer; row/column hits; refill ownership; shuffle with an existing Rocket; cleanup; and restoration of normal coins/inventory after QA. `verified-status.json` records zero new console errors and the test profile released. Runtime/editor code compilation passed with existing warnings. No full-game regression or Android execution/build was performed.

Reviewed captures: `Bottom-bar.png`, `Hammer-selected.png`, `Hammer-shop.png`, `After-shuffle.png`. These are isolated practice-board captures; displayed practice moves/objectives are not changes to campaign levels.

## Changed files

- `Assets/ReferenceGameplay/Tools/`: native sprite resources, metadata and focused `ReferenceBoosterBar` component.
- `Assets/JellyGarden/Scripts/GUI/RoyalGameplayPresentation.cs`: bind existing bar buttons to imported tools.
- `Assets/JellyGarden/Scripts/GUI/BoostIcon.cs` and `BoostShop.cs`: route new tool actions and purchases without changing old enum values.
- `Assets/JellyGarden/Scripts/LevelManager.cs`: respect temporary tool-selection ownership of board input.
- `Assets/ReferenceGameplay/Editor/ReferenceBoosterChecks.cs` and `ReferenceGameplayChecks.cs`: isolated checks and command hook.

Pre-edit checkpoint: `Design/Backup/Reference-BoosterBar-20260924-153323/`. Existing scene edits were not saved or discarded. Startup/home, music, characters, level definitions and other booster implementations were not edited by this task.
