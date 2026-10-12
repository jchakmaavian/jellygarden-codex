# Merge spec: `Assets/JellyGarden/Scripts/System/CombineManager.cs`

Plan: §4 "`System/CombineManager.cs`, `AI.cs`", Task 3.3.

## 1. Base

`origin/royal-throne` (335 lines). `origin/main` is byte-identical to the kit ancestor `a82c4df` for this file, so there is nothing of main's to keep.

```
git show origin/royal-throne:Assets/JellyGarden/Scripts/System/CombineManager.cs > Assets/JellyGarden/Scripts/System/CombineManager.cs
```

## 2. KEEP-FROM-MAIN

None. Main never changed this file.

## 3. CUT-FROM-ROYAL-THRONE

Royal Throne line numbers.

| RT line | Royal Throne text | Action |
|---|---|---|
| 223–224 | `if (RoyalThrone.Campaign.RoyalCampaignBlockers.IsSpecialItem(square.item))` / `return false;` (in `IsSquareNotNull`) | **Delete both lines.** |
| 257 | `if (RoyalThrone.Campaign.RoyalCampaignBlockers.IsSpecialItem(item) \|\| RoyalThrone.Campaign.RoyalCampaignBlockers.IsSpecialItem(nextItem)) return false;` (in `CheckColor`) | **Delete the line.** |

Why removal is behaviour-neutral: `RoyalCampaignBlockers.IsSpecialItem(item)` is `item && item.TryGetComponent(out RoyalCampaignItem _)` (`RoyalCampaignBlockers.cs:294`). `RoyalCampaignItem` is a D8 type that is not imported, so no piece can carry it and the term is always false.

After the cut, `IsSquareNotNull` ends:

```csharp
			if (square.item == null)
				return false;
			return true;
		}
```

and `CheckColor` starts:

```csharp
		bool CheckColor (Item item, Item nextItem)
		{
			if (Juice.RoyalEffectsProfile.Get && (item.currentType != ItemsTypes.NONE || nextItem && nextItem.currentType != ItemsTypes.NONE)) return false;
			if (nextItem) {
```

**Keep from Royal Throne:** the rest of the file, including:
- `LevelManager.THIS.PreferredMatchHost(cmb.items)` (RT:77);
- `Juice.RoyalPatterns.AddSquares(combinedItems)` (RT:93, the 2×2 Propeller);
- the swap-axis rocket orientation block in `GetTypeOfCombine` (RT:161–172);
- the "specials never colour-match" guard in `CheckColor` (RT:258).

## 4. D3 colour changes

None. This file has no colour clamp or colour remap.

## 5. Dropped main hooks

None. Main has no hooks in this file.

## 6. Risks / judgement calls

- `PreferredMatchHost` is a Royal Throne `LevelManager` member. It is kept by the LevelManager spec, so this file only compiles together with it (Phase 3 is one compile unit).
- `RoyalPatterns.AddSquares` and the Royal orientation rule only run when `RoyalEffectsProfile.Get` is non-null, so with the master switch held back (Task 3.6) the kit rules apply.

```anchors
path: Assets/JellyGarden/Scripts/System/CombineManager.cs
rt: if (RoyalThrone.Campaign.RoyalCampaignBlockers.IsSpecialItem(square.item))
rt: if (RoyalThrone.Campaign.RoyalCampaignBlockers.IsSpecialItem(item) || RoyalThrone.Campaign.RoyalCampaignBlockers.IsSpecialItem(nextItem)) return false;
rt: if (Juice.RoyalEffectsProfile.Get && (item.currentType != ItemsTypes.NONE || nextItem && nextItem.currentType != ItemsTypes.NONE)) return false;
rt: Item draggedItem = LevelManager.THIS.PreferredMatchHost(cmb.items);
rt: if (Juice.RoyalEffectsProfile.Get) Juice.RoyalPatterns.AddSquares(combinedItems);
rt: bool CheckColor (Item item, Item nextItem)
absent-main: RoyalThrone
absent-main: Juice.
```
