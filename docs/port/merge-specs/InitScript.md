# Merge spec: `Assets/JellyGarden/Scripts/InitScript.cs`

Plan: §4 "`InitScript.cs`, `GUI/AnimationManager.cs`, `GUI/MenuManager.cs`", Appendix B row `InitScript.cs`, Task 3.5 (`RoyalQaService`).

## 1. Base

**`origin/main`** (883 lines). Keep main's file. It is already in the working tree on `port/royal-throne-level`.

## 2. KEEP-FROM-MAIN

Everything, including the three Appendix B hooks:
- `CheckAdsEvents`: `if (!JellyGarden.Scripts.Story.StoryFreeGame.AdsAllowed) return;`
- `SpendLife`: `if (!JellyGarden.Scripts.Story.StoryFreeGame.ShouldSpendLife) return;`
- the map-node click handler: `if (JellyGarden.Scripts.Story.StoryFlow.HandleMapNodeClicked(args.Number)) return;`

Royal Throne's InitScript changes are **not** taken. That includes:
- the UTC life schedule (`NextLifeKey`, `UpdateLives`, `SaveLifeSchedule`, `LifeIntervalSeconds`);
- `TryOpenLevel`;
- `RoyalEconomy.StartingCoins`;
- `RoyalOfflineMode.ServicesEnabled` gates;
- the `DateOfExit` ISO-8601 format.

## 3. ROYAL-THRONE-ONLY MEMBERS THAT IMPORTED CODE CALLS

These public or internal members exist only in Royal Throne's `InitScript`: `LifeIntervalSeconds`, `LifeRefillDeadline`, `NextLifeKey`, `RestoreLifeScheduleForTesting`, `SaveLifeSchedule`, `SetRefillSeconds`, `TryOpenLevel` and `UpdateLives`. Main's file lacks them.

The import set searched was: `Juice/**`, the §5 GUI subset, `RoyalSave.cs`, `RoyalQaService.cs`, `ReferenceGameplay/{Scripts,Tools/Scripts,Editor}`, `ReferenceWinFinish.cs`, and `Boosters/{Propeller/V5,ColorBall/V4,ColorBall/V5}`. Only these three are called, all from `RoyalQaService.cs`, which is QA-only and wrapped in `#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !ROYAL_DISABLE_QA`:

| Member | Caller (`origin/royal-throne`) | Royal Throne definition |
|---|---|---|
| `internal DateTime LifeRefillDeadline` | `RoyalQaService.cs:72` `normalLifeDeadline=InitScript.Instance.LifeRefillDeadline;` | `InitScript.cs:95` `internal DateTime LifeRefillDeadline => nextLifeUtc;` (inside the QA `#if`) |
| `internal void RestoreLifeScheduleForTesting(DateTime, float)` | `RoyalQaService.cs:98` `InitScript.Instance.RestoreLifeScheduleForTesting(normalLifeDeadline,normalTimer);` | `InitScript.cs:97–101` (inside the QA `#if`) |
| `public void SetRefillSeconds(float)` | `RoyalQaService.cs:136` `InitScript.Instance.SetRefillSeconds(seconds);` | `InitScript.cs:669–683` (uses `nextLifeUtc`, `SaveLifeSchedule`) |

Every other `InitScript` member the import set uses exists on main with the same signature (verified): `Gems`, `lifes`, `Instance`, `SetGems(int)`, `AddGems(int)`, `AddLife(int)`, `SpendLife(int)`, `BuyBoost(BoostType,int,int)`, `SpendBoost(BoostType)`, `SaveLevelStarsCount(int,int)`, `CapOfLife`, `RestLifeTimer`, `DateOfExit`, and `TotalTimeForRestLife{Hours,Min,Sec}`.

**Default: add QA shims to main's file.** Royal Throne's real implementations depend on its life schedule (`nextLifeUtc`), which main does not have. Main keeps the kit's `RestLifeTimer` / `DateOfExit` model, and lives are never spent (`StoryFreeGame.ShouldSpendLife`). **Anchor:** main `InitScript.cs`, directly after `public static string DateOfExit;` (main:87). Insert:

```csharp
#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !ROYAL_DISABLE_QA
        // Royal Throne QA hooks (RoyalQaService snapshots and restores the life timer). King Leo keeps the
        // kit's RestLifeTimer model and never spends lives (StoryFreeGame), so there is no UTC deadline.
        internal DateTime LifeRefillDeadline => DateTime.MinValue;
        internal void RestoreLifeScheduleForTesting(DateTime deadline, float remaining) { RestLifeTimer = remaining; }
        /// <summary>Test hook: makes the next life arrive in <paramref name="seconds"/>.</summary>
        public void SetRefillSeconds(float seconds) { RestLifeTimer = Mathf.Max(0, seconds); }
#endif
```

`DateTime` resolves through main's `using System;` (main:13). `Mathf` resolves through `using UnityEngine;` (main:20).

**Alternative:** do not touch `InitScript`. Instead delete `RoyalQaService.cs` lines 72, 98 and 136 and the `normalLifeDeadline` field. Record that in `COUPLINGS.md`.

### Recommended one-line addition: `RoyalSave` alias

Royal Throne's `InitScript.cs:1` is `using PlayerPrefs = RoyalThrone.Testing.RoyalSave;`. Main's `InitScript` writes `UnityEngine.PlayerPrefs` directly. The imported `RoyalQaService` and `ReferenceBoosterBar` call `InitScript.Instance.SetGems/AddGems/AddLife/BuyBoost/SpendBoost/SaveLevelStarsCount`, so during a QA session (`RoyalSave.Testing`) those writes would hit the **player's real save** and break the isolation `RoyalQaService` promises (plan Risks: "RoyalSave aliasing PlayerPrefs").

Add `using PlayerPrefs = RoyalThrone.Testing.RoyalSave;` as the first line of main's file. Main's `InitScript` uses only `GetInt`, `GetFloat`, `GetString`, `SetInt`, `SetFloat`, `SetString` and `Save`, and `RoyalSave` implements all of them with identical behaviour outside QA. This is a judgement call; main's Story scripts (`StoryProgress`, `StoryBoosters`…) still write real `PlayerPrefs` during QA either way.

## 4. D3 colour changes

None.

## 5. Dropped main hooks

None.

## 6. Risks / judgement calls

- **`DateOfExit` format.** Royal Throne writes `DateTime.UtcNow.ToString("o", InvariantCulture)` from `LevelManager.EnableMap(false)`. Main's `InitScript` and `GUI/LIFESAddCounter.cs:44` parse with `DateTime.Parse(InitScript.DateOfExit)` and write `DateTime.Now.ToString()`. The LevelManager spec therefore reverts that one line to main's `DateTime.Now.ToString()` (LevelManager.md C14). Keep the two files consistent.
- `RoyalQaService.cs` also calls `RoyalHomeScreen.RefreshProgress` (RoyalQaService.cs:131) and `RoyalReferenceCampaign.Select` (:96). Those are coupling cuts for `COUPLINGS.md`, outside this file.

```anchors
path: Assets/JellyGarden/Scripts/InitScript.cs
main: public static string DateOfExit;
main: using System;
main: if (!JellyGarden.Scripts.Story.StoryFreeGame.AdsAllowed)
main: if (!JellyGarden.Scripts.Story.StoryFreeGame.ShouldSpendLife)
main: if (JellyGarden.Scripts.Story.StoryFlow.HandleMapNodeClicked(args.Number))
absent-main: LifeRefillDeadline
absent-main: SetRefillSeconds
absent-main: RoyalSave
rt: using PlayerPrefs = RoyalThrone.Testing.RoyalSave;
rt: internal DateTime LifeRefillDeadline => nextLifeUtc;
rt: internal void RestoreLifeScheduleForTesting(DateTime deadline, float remaining)
rt: public void SetRefillSeconds(float seconds)
path: Assets/JellyGarden/Scripts/RoyalQaService.cs
rt: normalLifeDeadline=InitScript.Instance.LifeRefillDeadline;
rt: InitScript.Instance.RestoreLifeScheduleForTesting(normalLifeDeadline,normalTimer);
rt: InitScript.Instance.SetRefillSeconds(seconds);
```
