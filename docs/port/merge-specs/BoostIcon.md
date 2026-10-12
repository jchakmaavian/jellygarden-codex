# Merge spec: `Assets/JellyGarden/Scripts/GUI/BoostIcon.cs`

Plan: §4 "`GUI/BoostShop.cs`, `GUI/BoostIcon.cs`", Task 3.4 Step 5, D5.

## 1. Base

`origin/royal-throne` (174 lines). `origin/main` is byte-identical to the kit ancestor `a82c4df` for this file.

```
git show origin/royal-throne:Assets/JellyGarden/Scripts/GUI/BoostIcon.cs > Assets/JellyGarden/Scripts/GUI/BoostIcon.cs
```

## 2. KEEP-FROM-MAIN

None. Main never changed this file.

## 3. CUT-FROM-ROYAL-THRONE

None. Every Royal Throne addition is either imported or self-contained:

| RT line | Royal Throne text | Status |
|---|---|---|
| 1 | `using PlayerPrefs = RoyalThrone.Testing.RoyalSave;` | Keep. `RoyalSave.cs` is imported (Task 3.5) and forwards to `UnityEngine.PlayerPrefs` with the same keys outside QA, so `StoryBoosters.Owned` and `StoryFreeGame` grants (main, real `PlayerPrefs`) are read back unchanged. |
| 29–32 | null-safe `Indicator/Image/Check` lookup in `OnEnable` | Keep (robustness). |
| 42 | `if(RoyalThrone.ReferenceGameplay.ReferenceBoosterBar.IsTool(type)){RoyalThrone.ReferenceGameplay.ReferenceBoosterBar.Instance.Choose(type);return;}` | Keep. This is the plan's "`ActivateBoost` routed to the bar". |
| 155–164 | cached count key and label in `Update` | Keep (performance). |

The file also carries Royal Throne's BOM change: the copyright line is no longer the first line, because the `using` alias is. That is harmless.

## 4. D3 colour changes

None.

## 5. Dropped main hooks

None.

## 6. Risks / judgement calls

- **The kit badge reads counts through `RoyalSave`.** `PlayerPrefs.GetInt("" + type)` now goes through `RoyalSave.GetInt`. During a Royal QA session (`RoyalSave.Testing == true`, Editor and development builds only), the badge shows the QA profile's counts, not the player's. That is intended.
- **A zero count opens the gem shop.** `ActivateBoost` → `OpenBoostShop` → `BoostShop.SetBoost` for non-tool kit boosts. See BoostShop.md §6 for the recommended `StoryFreeGame.ShopsAllowed` guard (D5). That guard lives in `BoostShop.SetBoost`, so this file stays a plain Royal Throne take.
- **`ReferenceBoosterBar.Instance.Choose` is unguarded** (same reasoning as BoostShop.md §6). It only runs for the four Royal tool types, whose buttons are created by the bar itself.

```anchors
path: Assets/JellyGarden/Scripts/GUI/BoostIcon.cs
rt: using PlayerPrefs = RoyalThrone.Testing.RoyalSave;
rt: if(RoyalThrone.ReferenceGameplay.ReferenceBoosterBar.IsTool(type)){RoyalThrone.ReferenceGameplay.ReferenceBoosterBar.Instance.Choose(type);return;}
rt: public void ActivateBoost ()
rt: int count = PlayerPrefs.GetInt (countKey);
rt: GameObject.Find ("CanvasGlobal").transform.Find ("BoostShop").gameObject.GetComponent<BoostShop> ().SetBoost (boosType);
absent-main: RoyalThrone
```
