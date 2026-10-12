# Merge spec: `Assets/JellyGarden/Scripts/GUI/AnimationManager.cs`

Plan: §4 "`InitScript.cs`, `GUI/AnimationManager.cs`, `GUI/MenuManager.cs`", Appendix B row `GUI/AnimationManager.cs`.

## 1. Base

**`origin/main`** (642 lines). Keep main's file. It is already in the working tree on `port/royal-throne-level`.

## 2. KEEP-FROM-MAIN

Everything. That includes the Appendix B hook in `CloseMenu`:

```csharp
            if (gameObject.name == "MenuComplete" &&
                JellyGarden.Scripts.Story.StoryFlow.HandleLevelComplete())
            {
                gameObject.SetActive(false);
                return;
            }
            if (gameObject.name == "MenuFailed" &&
                JellyGarden.Scripts.Story.StoryFlow.HandleLevelFailed())
            {
                gameObject.SetActive(false);
                return;
            }
```

## 3. ROYAL-THRONE-ONLY MEMBERS THAT IMPORTED CODE CALLS

**None.** Royal Throne's `AnimationManager` adds one public member that main lacks, `RetryLevel`. A search of the import set finds no caller:
- `Juice/**`;
- the GUI subset (`RoyalGameplayPresentation`, `RoyalGameplayHud`, `RoyalBoardSurface`, `RoyalArchShape`, `RoyalCardShape`, `RoyalPanelGraphic`, `RoyalVisualCatalog`);
- `RoyalSave.cs`, `RoyalQaService.cs`;
- `ReferenceGameplay/{Scripts,Tools/Scripts,Editor}`, `ReferenceWinFinish.cs`;
- `Boosters/{Propeller/V5,ColorBall/V4,ColorBall/V5}`.

The only imported use of `AnimationManager` is `RoyalQaService.cs:114`, `foreach(var animation in MenuManager.Instance.GetComponentsInChildren<AnimationManager>(true))animation.StopAllCoroutines();`. That is plain `MonoBehaviour` API.

Royal Throne's other changes in this file are home, shop and back-button couplings that are out of scope:
- `RoyalMenuSkin.LeaveConfirmName`;
- `RoyalBackButton`;
- `RoyalShopPage`;
- `RoyalOfflineMode`;
- `RoyalReferenceCampaign.Enabled`.

They are not taken.

### Optional additions (judgement calls, not required to compile)

1. **Avatar trigger "ExtraMoves" on a continue.** Royal Throne's `GoOnFailed` raises `RoyalThrone.Characters.KingVanguardActivityEvents.NotifyExtraMovesGranted(LevelManager.THIS);` as its last line (RT AnimationManager.cs:712). In King Leo the PreFailed offer is disabled (`StoryFreeGame.PreFailOfferAllowed`), so `GoOnFailed` is not reached in normal play. If Task 4.7 wants parity, add `JellyGarden.Scripts.Juice.AvatarEvents.NotifyExtraMovesGranted(LevelManager.THIS);` after main's `LevelManager.THIS.gameStatus = GameState.Playing;` in `GoOnFailed` (main:577).
2. **`RoyalSave` alias** `using PlayerPrefs = RoyalThrone.Testing.RoyalSave;` as the first line (Royal Throne has it at RT:1). This keeps QA sessions from writing the player's real `Rated`, `OpenLevel` and boost keys. Main's file uses only `GetInt`, `SetInt` and `Save` from `PlayerPrefs`, which `RoyalSave` supports. Same reasoning as InitScript.md §3.

## 4. D3 colour changes

None.

## 5. Dropped main hooks

None.

## 6. Risks / judgement calls

- Royal Throne's `RoyalGameplayPresentation` rail opens pause through `MenuManager.Instance.ShowMenuPause(true)` (imported code, 3 call sites). That method is kit code present on main, and `MenuPause`'s buttons run main's `AnimationManager`, so `StoryFlow` routing stays intact.
- `KingVanguardActivityEvents` must not appear in this file. Main's version has no such reference; check with `grep -n KingVanguard Assets/JellyGarden/Scripts/GUI/AnimationManager.cs` (expected: none).

```anchors
path: Assets/JellyGarden/Scripts/GUI/AnimationManager.cs
main: JellyGarden.Scripts.Story.StoryFlow.HandleLevelComplete())
main: JellyGarden.Scripts.Story.StoryFlow.HandleLevelFailed())
main: public void GoOnFailed()
main: LevelManager.THIS.gameStatus = GameState.Playing;
absent-main: KingVanguard
absent-main: RetryLevel
rt: RoyalThrone.Characters.KingVanguardActivityEvents.NotifyExtraMovesGranted(LevelManager.THIS);
rt: using PlayerPrefs = RoyalThrone.Testing.RoyalSave;
path: Assets/JellyGarden/Scripts/RoyalQaService.cs
rt: foreach(var animation in MenuManager.Instance.GetComponentsInChildren<AnimationManager>(true))animation.StopAllCoroutines();
```
