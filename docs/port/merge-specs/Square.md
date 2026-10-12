# Merge spec: `Assets/JellyGarden/Scripts/Square.cs`

Plan: §4 "`Square.cs`", Task 3.3 Steps 2–3, Appendix B row `Square.cs`.

## 1. Base

`origin/royal-throne` (399 lines).

```
git show origin/royal-throne:Assets/JellyGarden/Scripts/Square.cs > Assets/JellyGarden/Scripts/Square.cs
```

## 2. KEEP-FROM-MAIN

### K1. Ingredient exit arrow on `LevelManager.IngredientExitRow` (in `Start`)

Main's block (`git show origin/main:Assets/JellyGarden/Scripts/Square.cs`, lines 52–79):

```csharp
        void Start()
        {
            // GenItem();
            // The ingredient arrow marks where an ingredient has to arrive, so
            // it belongs under the square CheckIngredient actually watches —
            // the lowest cell of this column that is not a hole. The kit hung
            // it on the last row of the GRID instead, which is the same square
            // only on a board whose bottom row is solid all the way across. On
            // the other 90 ingredient levels that drew 199 arrows onto holes
            // (21 of them under a column with no playable cell at all) and left
            // 178 real collection cells unmarked.
            if (row == LevelManager.THIS.IngredientExitRow(col))
            {
                if(LevelManager.Instance.targetObject.Any(i=>i.type==Target.INGREDIENT))
                {
                    GameObject obj = Instantiate(Resources.Load("Prefabs/arrow_ingredients")) as GameObject;
                    obj.transform.SetParent(transform);
                    obj.transform.localPosition = Vector3.zero + Vector3.down * 0.8f;
                    if (Story.StorybookBoardArt.Enabled)
                    {
                        // Compact exit markers leave room for the lower controls.
                        obj.transform.localScale *= .35f;
                        obj.transform.localPosition = Vector3.down * .55f;
                    }
                }
            }

        }
```

**Anchor.** Royal Throne method `Start` (RT:62–75) is still the kit body. Replace the RT lines from `// GenItem();` (RT:64) through the closing `}` of the outer `if` (RT:73) with the block below. That covers RT:65, `if (row == LevelManager.THIS.maxRows - 1)`, and the `obj.transform.localPosition = Vector3.zero + Vector3.down * 0.8f;` line before the two closing braces. Main's block goes in **with one change**: the compact-scale condition becomes "Royal layer active" (Task 3.3 Step 2).

```csharp
            // GenItem();
            // The ingredient arrow marks where an ingredient has to arrive, so
            // it belongs under the square CheckIngredient actually watches —
            // the lowest cell of this column that is not a hole. The kit hung
            // it on the last row of the GRID instead, which is the same square
            // only on a board whose bottom row is solid all the way across. On
            // the other 90 ingredient levels that drew 199 arrows onto holes
            // (21 of them under a column with no playable cell at all) and left
            // 178 real collection cells unmarked.
            if (row == LevelManager.THIS.IngredientExitRow(col))
            {
                if(LevelManager.Instance.targetObject.Any(i=>i.type==Target.INGREDIENT))
                {
                    GameObject obj = Instantiate(Resources.Load("Prefabs/arrow_ingredients")) as GameObject;
                    obj.transform.SetParent(transform);
                    obj.transform.localPosition = Vector3.zero + Vector3.down * 0.8f;
                    if (Juice.RoyalEffectsProfile.Get != null)   // was Story.StorybookBoardArt.Enabled (plan Task 3.3 Step 2)
                    {
                        // Compact exit markers leave room for the lower controls.
                        obj.transform.localScale *= .35f;
                        obj.transform.localPosition = Vector3.down * .55f;
                    }
                }
            }
```

This needs `LevelManager.IngredientExitRow(int)`, which the LevelManager spec re-adds (K5 there). `Juice.` resolves to `JellyGarden.Scripts.Juice`, because Square lives in `namespace JellyGarden.Scripts` (RT:18). Royal Throne already writes `Juice.RoyalEffectsProfile.Get` in this file (RT:149).

## 3. CUT-FROM-ROYAL-THRONE

All campaign couplings are D8. `IsSpecialItem` is always false here, because `RoyalCampaignItem` is not imported.

| RT line | Method | Royal Throne text (exact) | Action |
|---|---|---|---|
| 77 | `GenItem` | `public Item GenItem(bool falling = true, bool campaignRefill = true)` | **Keep the signature.** Item.cs calls `GenItem(..., campaignRefill: false)` (RT Item:1436). The parameter is now unused. |
| 101–104 | `GenItem` | `// An earned power-up replaces its matched piece; it is not a refill and` / `// must not consume a queued egg/coin or inherit that item's identity.` / `if (campaignRefill)` / `RoyalThrone.Campaign.RoyalReferenceCampaign.ConfigureSpawn(itemInstance, falling);` | **Delete the 4 lines.** `return this.item;` follows directly after `this.item = itemInstance;`. |
| 145–146 | `FindMoreMatches` | `if (RoyalThrone.Campaign.RoyalCampaignBlockers.IsSpecialItem(this.item))` / `return countedSquares;` | **Delete both lines.** |
| 277 | `CanGoOut` | `if (RoyalThrone.Campaign.RoyalCampaignBlockers.IsSpecialItem(item)) return false;` | **Delete the line.** `CanGoOut` becomes the kit's `return type != SquareTypes.WIREBLOCK;`. |
| 286 | `DestroyBlock` | `public void DestroyBlock(bool rocketClear = false, int campaignWave = 0, bool campaignMatchedItem = false)` | **Keep the signature** (plan §4: "`DestroyBlock(rocketClear, wave, matched)` without its `RoyalCampaignBlockers` call"). Item.cs passes all three. |
| 288 | `DestroyBlock` | `if (RoyalThrone.Campaign.RoyalCampaignBlockers.HandleHit(this, rocketClear, campaignWave, campaignMatchedItem)) return;` | **Delete the line.** |

**Keep from Royal Throne** (plan §4):
- `reservedFor` / `Reserved` / `Reserve` / `Release` (RT:44–53);
- the `GenItem` reservation and live-occupant checks (RT:81–84);
- `Juice.RoyalEffectsProfile.Get` special exclusion in `FindMoreMatches` (RT:149);
- `RoyalPatterns.SquareAt` in `FindMatchesAround` (RT:220–221);
- `FallOut` guards `!item.inputSwapLocked` and `!nextSquare.Reserved` (RT:234, RT:255);
- `DestroyBlock`'s `RoyalPowerupDirector.Chip`, `RoyalIceBreak`, `CandyShatterService` and `blockSprites` null-guard (RT:302–345).

## 4. D3 colour changes

None. Square has no colour clamp.

## 5. Dropped main hooks

- `StoryBoardScale.ItemScale` in `GenItem` (main:101–102: `itemInstance.transform.localScale = Vector2.one * JellyGarden.Scripts.Story.StoryBoardScale.ItemScale;`). Royal Throne's `Vector2.one * 0.6f` (RT:86) stays.
- `[OverlapProbe]` warning in `GenItem` (main:85–89). Royal Throne's `[Royal board]` warning (RT:83–84) replaces it.
- `!Item.IsUniversalPowerup(this.item)` in `FindMoreMatches` (main:165). Royal Throne's `if (Juice.RoyalEffectsProfile.Get && item.currentType != ItemsTypes.NONE) return countedSquares;` covers it.
- `Story.StorybookBoardArt.Enabled` as the arrow-scale condition (replaced, see K1).

## 6. Risks / judgement calls

- **The compact arrow is for the Royal rail.** With the switch off (Phase 3), arrows use the kit size and position. That is expected: Task 4.1 Step 2 checks the arrows with the switch on.
- **`IngredientExitRow` reads `levelSquaresFile`.** Royal Throne's `LoadDataFromLocal` writes `levelSquaresFile` from `RoyalReferenceCampaign.TryLoad`. That branch is cut by the LevelManager spec, so the parsed level file is always the source, as on main.
- Keeping the unused `campaignRefill`, `campaignWave` and `campaignMatchedItem` parameters is deliberate. It avoids touching every Royal Throne caller. A later cleanup can drop them together with Item.cs's `royalCampaignHitWave` / `campaignAdjacentMatch`.

```anchors
path: Assets/JellyGarden/Scripts/Square.cs
rt: void Start()
rt: // GenItem();
rt: if (row == LevelManager.THIS.maxRows - 1)
rt: obj.transform.localPosition = Vector3.zero + Vector3.down * 0.8f;
rt: public Item GenItem(bool falling = true, bool campaignRefill = true)
rt: if (campaignRefill)
rt: RoyalThrone.Campaign.RoyalReferenceCampaign.ConfigureSpawn(itemInstance, falling);
rt: if (RoyalThrone.Campaign.RoyalCampaignBlockers.IsSpecialItem(this.item))
rt: if (RoyalThrone.Campaign.RoyalCampaignBlockers.IsSpecialItem(item)) return false;
rt: public void DestroyBlock(bool rocketClear = false, int campaignWave = 0, bool campaignMatchedItem = false)
rt: if (RoyalThrone.Campaign.RoyalCampaignBlockers.HandleHit(this, rocketClear, campaignWave, campaignMatchedItem)) return;
rt: if (Juice.RoyalEffectsProfile.Get && item.currentType != ItemsTypes.NONE) return countedSquares;
rt: public bool Reserved => reservedFor && reservedFor.destroying;
rt: itemInstance.transform.localScale = Vector2.one * 0.6f;
rt: namespace JellyGarden.Scripts
main: if (row == LevelManager.THIS.IngredientExitRow(col))
main: if (Story.StorybookBoardArt.Enabled)
main: obj.transform.localScale *= .35f;
main: obj.transform.localPosition = Vector3.down * .55f;
main: Vector2.one * JellyGarden.Scripts.Story.StoryBoardScale.ItemScale;
```
