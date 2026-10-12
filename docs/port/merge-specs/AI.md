# Merge spec: `Assets/JellyGarden/Scripts/AI.cs`

Plan: §4 "`System/CombineManager.cs`, `AI.cs`", Task 3.3 Steps 3–4.

## 1. Base

`origin/royal-throne` (657 lines). `origin/main` is byte-identical to the kit ancestor `a82c4df`, so there is nothing of main's to keep.

```
git show origin/royal-throne:Assets/JellyGarden/Scripts/AI.cs > Assets/JellyGarden/Scripts/AI.cs
```

## 2. KEEP-FROM-MAIN

None. Main never changed this file.

## 3. CUT-FROM-ROYAL-THRONE

Royal Throne line numbers. There are two coupling families:
- `RoyalCampaignBlockers.IsSpecialItem`, a D8 campaign type. It is always false here, because `RoyalCampaignItem` is not imported.
- `RoyalPowerTutorial.HoldsHints`, from Royal Throne's `GUI/TutorialStuff/RoyalPowerTutorial.cs`. That lesson UI is not imported (plan §4 coupling list, Task 3.3 Step 4).

| RT line | Method | Royal Throne text (exact) | Replace with |
|---|---|---|---|
| 125 | `CheckSquare` | `if (RoyalThrone.Campaign.RoyalCampaignBlockers.IsSpecialItem(square.item)) return;` | delete the line |
| 158 | `CheckPossibleCombines` | `allowShowTip = !JellyGarden.Scripts.GUI.TutorialStuff.RoyalPowerTutorial.HoldsHints;   // a lesson keeps the hint off while it shows` | `allowShowTip = true;` (the kit's line) |
| 209 | `CheckPossibleCombines` | `if (square.type == SquareTypes.WIREBLOCK \|\| square.item == null \|\| RoyalThrone.Campaign.RoyalCampaignBlockers.IsSpecialItem(square.item))` | `if (square.type == SquareTypes.WIREBLOCK \|\| square.item == null)` |
| 458, 474, 490, 506 | `CheckPossibleCombines` (4 neighbours) | `if (square.item != null && !RoyalThrone.Campaign.RoyalCampaignBlockers.IsSpecialItem(square.item))` | `if (square.item != null)`. Same text on all four lines: replace all four. |
| 589 | `showTip` | `gotTip = true; if (JellyGarden.Scripts.GUI.TutorialStuff.RoyalPowerTutorial.HoldsHints) return;   // the lesson's own outline stays` | `gotTip = true;` |
| 638 | `showTipCor` | `if(!allowShowTip\|\|JellyGarden.Scripts.GUI.TutorialStuff.RoyalPowerTutorial.HoldsHints\|\|BoardSettling(lm))break;` | `if(!allowShowTip\|\|BoardSettling(lm))break;` |

Mechanical form:
- delete the substring ` || RoyalThrone.Campaign.RoyalCampaignBlockers.IsSpecialItem(square.item)`;
- delete the substring ` && !RoyalThrone.Campaign.RoyalCampaignBlockers.IsSpecialItem(square.item)`;
- delete the line `if (RoyalThrone.Campaign.RoyalCampaignBlockers.IsSpecialItem(square.item)) return;`;
- handle the three `HoldsHints` sites as in the table.

After the edits, check with `grep -n "RoyalCampaign\|RoyalPowerTutorial" Assets/JellyGarden/Scripts/AI.cs`. Expected: no output.

**Keep from Royal Throne** (plan §3 "Board behaviour"):
- hint state and presenter: `visibleHintItems`, `hintRoutine`, `RoyalHintPresenter`, `HintIdleSeconds`, `HintSuggestionSeconds`, `OnEnable`/`OnDisable`/`Update`, `CancelVisibleHint`;
- `GetCombine` live-candidate check;
- `BoardSettling` wait;
- `Juice.RoyalHintSolver.Find` / `FindAll` decision;
- `Juice.RoyalPatterns.ExtraHint` (RT:555–558);
- `HintCandidatesPresent`;
- the rewritten `showTip` / `showTipCor`.

## 4. D3 colour changes

None in this file. The legacy colour loop `for (int COLOR = 0; COLOR < it.items.Length; COLOR++)` (RT:202) is kit code. It no longer runs, because `royalDecided` is always true. The live hint path is `Juice.RoyalHintSolver`, which is covered by Task 4.2 Step 3.

## 5. Dropped main hooks

None. Main has no hooks in this file.

## 6. Risks / judgement calls

- **The hint solver runs with the switch off.** `royalDecided` is hard-coded `true` (RT:190), so `RoyalHintSolver.Find` decides hints and no-move shuffles even without `EffectsProfile.asset`. This is Royal Throne's own behaviour. If Task 3.8's checkpoint ("board plays with kit fallback") turns up hint or shuffle problems, look here first.
- `AI.Update` reads `InputManager.Instance.IsTouching`, and `BoardSettling` reads `level.IsResolvingMatches`, `level.InputSwaps` and `Juice.RoyalPowerupDirector.Busy`. These come from the Royal Throne `LevelManager` and `InputManager` kept by their specs, so this file compiles only together with them.
- If Task 1.3 decides to import `RoyalPowerTutorial` after all, skip the three `HoldsHints` edits.

```anchors
path: Assets/JellyGarden/Scripts/AI.cs
rt: if (RoyalThrone.Campaign.RoyalCampaignBlockers.IsSpecialItem(square.item)) return;
rt: allowShowTip = !JellyGarden.Scripts.GUI.TutorialStuff.RoyalPowerTutorial.HoldsHints;   // a lesson keeps the hint off while it shows
rt: if (square.type == SquareTypes.WIREBLOCK || square.item == null || RoyalThrone.Campaign.RoyalCampaignBlockers.IsSpecialItem(square.item))
rt: if (square.item != null && !RoyalThrone.Campaign.RoyalCampaignBlockers.IsSpecialItem(square.item))
rt: gotTip = true; if (JellyGarden.Scripts.GUI.TutorialStuff.RoyalPowerTutorial.HoldsHints) return;   // the lesson's own outline stays
rt: if(!allowShowTip||JellyGarden.Scripts.GUI.TutorialStuff.RoyalPowerTutorial.HoldsHints||BoardSettling(lm))break;
rt: void CheckSquare(Square square, int COLOR, bool moveThis = false)
rt: void showTip(List<Item> nextMoveItems)
rt: var royalPlan = Juice.RoyalHintSolver.Find(LevelManager.THIS);
main: allowShowTip = true;
absent-main: RoyalThrone
```
