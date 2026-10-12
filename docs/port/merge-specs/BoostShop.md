# Merge spec: `Assets/JellyGarden/Scripts/GUI/BoostShop.cs`

Plan: §4 "`GUI/BoostShop.cs`, `GUI/BoostIcon.cs`", Task 3.4 Step 5, D5, Fact 4.

## 1. Base

`origin/royal-throne` (94 lines). `origin/main` is byte-identical to the kit ancestor `a82c4df` for this file.

```
git show origin/royal-throne:Assets/JellyGarden/Scripts/GUI/BoostShop.cs > Assets/JellyGarden/Scripts/GUI/BoostShop.cs
```

## 2. KEEP-FROM-MAIN

None. Main never changed this file.

## 3. CUT-FROM-ROYAL-THRONE

| RT line | Method | Royal Throne text (exact) | Action |
|---|---|---|---|
| 61 | `SetBoost` | `Juice.RoyalEconomy.Apply(boostProducts);   // pack sizes and prices come from the coin rules` | **Delete the line.** `RoyalEconomy` is Royal Throne's coin rules (plan §4 coupling list, D4/D5). Without it, `boostProducts` keeps the inspector's pack sizes and prices from main's scene, as on main. |

**Keep from Royal Throne:**
- **Enum additions (Fact 4).** `RoyalHammer`, `RoyalArrow`, `RoyalCannon` and `RoyalJester` are appended *after* `None` (RT:29–33). The existing values `ExtraMoves=0 … Random_color=7, None=8` keep their integers, so saved `PlayerPrefs` keys (named by `BoostType` string), level files and `Targets` assets keep their meaning. Verified: the RT enum order is `ExtraMoves, Packages, Stripes, ExtraTime, Bomb, Colorful_bomb, Hand, Random_color, None, RoyalHammer, RoyalArrow, RoyalCannon, RoyalJester`. The base and main order is identical up to `None`.
- **Tool route** at the top of `SetBoost` (RT:59): `if(RoyalThrone.ReferenceGameplay.ReferenceBoosterBar.IsTool(_boostType)){RoyalThrone.ReferenceGameplay.ReferenceBoosterBar.Instance.OpenShop(_boostType);return;}`. Plan: "`ActivateBoost` routed to the bar". Task 4.8 Step 2 makes `ReferenceBoosterBar`'s purchase panel respect `StoryFreeGame.ShopsAllowed`.
- **Per-booster art** when the Royal layer is on (RT:64–69, `Juice.RoyalPieceSkin.Art`).

## 4. D3 colour changes

None.

## 5. Dropped main hooks

None.

## 6. Risks / judgement calls

- **The kit gem shop can still open (D5).** On main, `BoostShop` is not gated by `StoryFreeGame.ShopsAllowed`; only `ShowGemsShop` and `ShowLiveShop` are. Once Task 4.8 re-parents the kit boost buttons onto the Royal rail, a zero-count kit boost opens this gem purchase panel through `BoostIcon.OpenBoostShop` → `SetBoost`. D5 wants no shop. **Recommended (not a plain Royal Throne take; confirm in Task 4.8):** insert after the RT:59 tool route:

  ```csharp
              if (!JellyGarden.Scripts.Story.StoryFreeGame.ShopsAllowed) return;   // D5: nothing is for sale
  ```

  Without this line the merge equals "take Royal Throne's" exactly. Main's `StoryScreenTests.cs:1007` already expects `!StoryFreeGame.ShopsAllowed`.
- **Null `ReferenceBoosterBar.Instance`.** The tool route dereferences `Instance` without a check. `IsTool` is only true for the four Royal tool types, whose buttons exist only on the Royal rail, so `Instance` is set whenever the route runs. For robustness, the merge agent may write `var bar = RoyalThrone.ReferenceGameplay.ReferenceBoosterBar.Instance; if (bar) bar.OpenShop(_boostType); return;` instead. This is optional.
- **Index bounds.** `boostProducts[(int)_boostType]` would go out of range for the new enum values (9–12), but the tool route returns first, so it is never indexed with them.

```anchors
path: Assets/JellyGarden/Scripts/GUI/BoostShop.cs
rt: Juice.RoyalEconomy.Apply(boostProducts);   // pack sizes and prices come from the coin rules
rt: if(RoyalThrone.ReferenceGameplay.ReferenceBoosterBar.IsTool(_boostType)){RoyalThrone.ReferenceGameplay.ReferenceBoosterBar.Instance.OpenShop(_boostType);return;}
rt: public void SetBoost (BoostType _boostType )
rt: None,
rt: RoyalHammer,
rt: RoyalJester
main: None
absent-main: RoyalHammer
```
