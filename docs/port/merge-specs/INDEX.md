# Kit-core merge specs: index

Plan: `docs/superpowers/plans/2026-10-12-royal-throne-level-port.md` §4, Tasks 3.1–3.4, with Decisions D1–D8 at their **default**.

The specs follow plan §4: start from Royal Throne's file, cut its couplings, then re-apply main's *keep* hooks. Each spec has six parts: base, KEEP-FROM-MAIN with main's exact code and the Royal Throne anchor, CUT-FROM-ROYAL-THRONE, D3, dropped main hooks, and risks. Each ends with an `anchors` block that a tool verifies.

Line numbers are written `RT:n` for `origin/royal-throne` and `main:n` for `origin/main`. They drift, so match by the quoted text.

## 1. Files and merge order

Phase 3 is **one compile unit** (plan Task 3.8). Nothing below compiles alone, so the order just keeps each step reviewable. Every path is under `Assets/JellyGarden/Scripts/`.

| # | Spec | File | Base | Plan task | Main hooks kept | Royal Throne cuts | D3 |
|---|---|---|---|---|---|---|---|
| 0 | INDEX §3 | `Juice/AvatarEvents.cs` (**new**) | — | 3.2 Step 4 | — | — | — |
| 1 | [LevelManager.md](LevelManager.md) | `LevelManager.cs` | royal-throne | 3.1 | K1–K13 (campaign tuning, PrePlay, OneStar, IngredientExitRow, null guards, CheckWinLose, PreWin structure, Win branch) | C1–C20 (campaign, CommitWin/RoyalEconomy/RoyalWinSequence, RoyalProfile, RoyalOfflineMode, RoyalBackButton, KingVanguard→AvatarEvents, DateOfExit format) | `COLOR LIMIT` clamp |
| 2 | [Item.md](Item.md) | `Item.cs` | royal-throne | 3.2 | none | campaign ×10, `RoyalWinSequence`, `RoyalProfile`, Propeller V3/V4, KingVanguard→AvatarEvents ×2 | `BoardColor` setter, `GenColor` clamp |
| 3 | [Square.md](Square.md) | `Square.cs` | royal-throne | 3.3 | ingredient arrow on `IngredientExitRow` (compact when the Royal layer is on) | `ConfigureSpawn`, `IsSpecialItem` ×2, `HandleHit` | — |
| 4 | [CombineManager.md](CombineManager.md) | `System/CombineManager.cs` | royal-throne | 3.3 | — (main = kit) | `IsSpecialItem` ×2 sites | — |
| 5 | [AI.md](AI.md) | `AI.cs` | royal-throne | 3.3 | — (main = kit) | `IsSpecialItem` ×6, `RoyalPowerTutorial.HoldsHints` ×3 | — |
| 6 | [InputManager.md](InputManager.md) | `InputManager.cs` | royal-throne | 3.4 S1 | none: `TrackPressPointer` covers main's press-position fix (optional fallback given) | — | — |
| 7 | [MenuManager.md](MenuManager.md) | `GUI/MenuManager.cs` | **main** | 3.4 S2 | all | adds `BlocksBoardInput` without its `RoyalShopPage` line | — |
| 8 | [SoundBase.md](SoundBase.md) | `SoundBase.cs` | royal-throne | 3.4 S3 | `PlaySound(clip, volumeScale)` | — | — |
| 9 | [MusicBase.md](MusicBase.md) | `MusicBase.cs` | royal-throne | 3.4 S3 | (King Leo music kept) | `RoyalAudioDirector` music gate | — |
| 10 | [TargetLevel.md](TargetLevel.md) | `Targets/TargetLevel.cs` | royal-throne | 3.4 S4 | `StorybookBoardArt.Goal` in `DeepCopy` | — | colour-5 → 1 migration |
| 11 | [TargetIcon.md](TargetIcon.md) | `Targets/TargetIcon.cs` | royal-throne | 3.4 S4 | `StorybookBoardArt.Goal` (+ lookup fix) | `RoyalVisualCatalog.Goal` call site | — |
| 12 | [TargetsGroup.md](TargetsGroup.md) | `Targets/TargetsGroup.cs` | royal-throne | 3.4 S4 | — (main = kit) | reference-campaign branch | — |
| 13 | [BoostShop.md](BoostShop.md) | `GUI/BoostShop.cs` | royal-throne | 3.4 S5 | — (main = kit) | `RoyalEconomy.Apply` | — |
| 14 | [BoostIcon.md](BoostIcon.md) | `GUI/BoostIcon.cs` | royal-throne | 3.4 S5 | — (main = kit) | — | — |
| 15 | [InitScript.md](InitScript.md) | `InitScript.cs` | **main** | §4 | all | — (no members added; the QA callers are cut, `COUPLINGS.md` §4; `RoyalSave` alias recommended) | — |
| 16 | [AnimationManager.md](AnimationManager.md) | `GUI/AnimationManager.cs` | **main** | §4 | all | — | — |

**Pairs that must be merged together:**
- **1 + 3:** `Square.Start` calls `LevelManager.IngredientExitRow`.
- **1 + 7:** `LevelManager.BoardInputAllowed` and `TimerHeld` read `MenuManager.BlocksBoardInput`.
- **10 + 11:** `TargetIcon`'s lookup depends on what `TargetObject.DeepCopy` puts in `icon`.
- **1 + 2 + 3:** `Square.GenItem(..., campaignRefill)` and `Square.DestroyBlock(rocketClear, wave, matched)` keep their signatures because `Item.cs` passes those arguments.
- **0 before 1 and 2:** `AvatarEvents` must exist.
- **15:** only the `RoyalSave` alias (recommended). The `InitScript` members that imported QA code calls are cut at the callers (`COUPLINGS.md` §4), not added here.

## 2. How to apply and test

```
# 1. Anchors: every quoted Royal Throne and main line still exists (read-only, git show only)
python3 tools/port_merge_spec_check.py            # expected: TOTAL 295 anchors, 0 missing

# 2. Dry run: applies all 16 specs to a temp folder, never Assets/, then checks brace balance,
#    leftover couplings, and C# syntax with the .NET SDK's Roslyn csc (parse errors only)
python3 tools/port_merge_dryrun.py [OUT_DIR]      # expected: PASS: 16 files merged ...

# 3. After the real merge (Phase 3), diff each file against the dry-run reference
diff -u OUT_DIR/LevelManager.cs Assets/JellyGarden/Scripts/LevelManager.cs
```

Results on 2026-10-12, against `origin/main` and `origin/royal-throne` at the time of writing:
- `port_merge_spec_check.py`: 295 anchors, 0 missing.
- `port_merge_dryrun.py`: PASS. All 16 files merged, every edit anchor matched its expected count, brace and paren balance equal to the base file, no leftover coupling tokens outside comments, 0 C# parse errors (Roslyn 8.0.424).
- Negative controls: an injected missing `)` is reported as CS1026, and the coupling regex finds 37/22/9 hits in the unmerged Royal Throne `LevelManager`/`Item`/`AI`.

The dry run does **not** type-check. Unity references and the Juice/ReferenceGameplay imports come only in Task 3.5, and Task 3.8 Step 1's `dotnet build Story.Typecheck.csproj` is the real compile gate.

## 3. Shared contract: `Juice/AvatarEvents.cs` (create before Phase 3 compiles)

The specs replace every `RoyalThrone.Characters.KingVanguardActivityEvents.NotifyX(...)` with `JellyGarden.Scripts.Juice.AvatarEvents.NotifyX(...)`, keeping method names and arguments. That keeps the edit a mechanical prefix swap. Required shape (plan Task 3.2 Step 4 / Task 4.7):

```csharp
namespace JellyGarden.Scripts.Juice
{
    public static class AvatarEvents
    {
        // Plan Task 4.7 events. Nothing subscribes until Task 4.7.
        public static event global::System.Action<LevelManager> Worried, GoalsCompleted, ExtraMoves, Lost, ToolUsed, ToolStruck;
        public static event global::System.Action<LevelManager, int> Matched;          // matched pieces
        public static event global::System.Action<LevelManager, Item> PowerupCreated;
        public static event global::System.Action<float> Reaction;                     // strength

        // Raise methods called by the kit files. Copy the bodies (the gating) from
        // origin/royal-throne:Assets/Characters/KingVenguard/V2/Scripts/KingVanguardActivityEvents.cs
        public static void NotifyMatchResolved(LevelManager level, int matchedPieces)  { /* gate: Playing && n >= 3 -> Matched */ }
        public static void NotifyPowerupCreated(LevelManager level, Item item)         { /* gate: live board piece of a power type -> PowerupCreated */ }
        public static void NotifyExtraMovesGranted(LevelManager level)                  { /* gate: Playing && MOVES -> ExtraMoves */ }
        public static void NotifyGoalsCompleted(LevelManager level)                     { /* gate: PreWinAnimations -> GoalsCompleted */ }
        // + a [RuntimeInitializeOnLoadMethod(SubsystemRegistration)] reset of all events, as in the original.
    }
}
```

**Event names.** The plan (Task 3.2 Step 4, Task 4.7) names the events `Worried`, `Matched`, `ExtraMoves`. The `AvatarEvents.cs` committed on `port/royal-throne-level` (`5f71ea88`) names them `WorriedChanged`, `MatchResolved`, `ExtraMovesGranted`, and types `ToolUsed`/`ToolStruck` as `Action<int>`. The `Notify*` methods the kit files call are the same in both, so Phase 3 compiles either way; only Task 4.7's subscribers depend on the event names. Task 4.7 should use the committed names and treat the plan's table as the event-to-clip mapping (audit review).

Inside `namespace JellyGarden.Scripts.Juice`, a qualified `System.Action` resolves to the kit namespace `JellyGarden.Scripts.System` first. Write `global::System.Action`, as above, or add `using System;` at file level and write `Action<...>`, as `Juice/RoyalHintSettings.cs` does.

Call sites the specs convert. Record them in `COUPLINGS.md`:

| File | Royal Throne line | Call |
|---|---|---|
| `Item.cs` | RT:1184 | `NotifyPowerupCreated(LevelManager.THIS, this)` (royal seat) |
| `Item.cs` | RT:1243 | `NotifyPowerupCreated(LevelManager.THIS, this)` (legacy branch) |
| `LevelManager.cs` | RT:159 | `NotifyExtraMovesGranted(this)` (ExtraMoves/ExtraTime boost) |
| `LevelManager.cs` | RT:862 | `NotifyGoalsCompleted(this)` (now inside main-structured `PreWinAnimationsCor`, LevelManager.md K11) |
| `LevelManager.cs` | RT:1839 | `NotifyMatchResolved(this, matchedPieces)` |
| `GUI/AnimationManager.cs` | RT:712 | *optional* `NotifyExtraMovesGranted` in `GoOnFailed` (main keeps its own file; AnimationManager.md §3) |

## 4. Cross-file items for `COUPLINGS.md` / Task 3.5 (outside these 16 files, but the merged kit depends on them)

1. **`Juice/RoyalWinSequence.cs` must not be imported** (D4). The merged `LevelManager` drops `CommittedCoins`, `StarsGained` and `CommitWin`, which only `RoyalWinSequence` reads (`RoyalWinSequence.cs:33,894`). It is referenced only by home and win-card files that are not imported, and by a comment in `ReferenceWinFinish.cs:30`. If it is imported anyway, keep `CommittedCoins`/`StarsGained` in `LevelManager`.
2. **`ReferenceWinFinish.Regular()`** (`Assets/ReferenceGameplay/Home/Scripts/ReferenceWinFinish.cs:17`) calls `RoyalThrone.Campaign.RoyalCampaignBlockers.IsSpecialItem(i)`. Cut that term (D8), or the K11 finish does not compile.
3. **`RoyalAudioDirector` plays its own music** (`Build()` → `music[0].Play()`, castle theme, then level music). With `MusicBase`'s gate removed (MusicBase.md), both tracks would play once the switch is on. Cut the director's music or set its volume to 0.
4. **`RoyalQaService.cs`** calls `InitScript.LifeRefillDeadline`, `RestoreLifeScheduleForTesting` and `SetRefillSeconds`; `RoyalQaChecks.cs:34` reads `InitScript.NextLifeKey` and `RoyalEffectsChecks.cs:23` calls `InitScript.Instance.TryOpenLevel`. Main's `InitScript` has none of them. Default: cut those callers (`COUPLINGS.md` §4; InitScript.md §3 gives shims only as the alternative). `RoyalQaService` also calls `RoyalHomeScreen.RefreshProgress` and `RoyalReferenceCampaign.Select`, which must be cut.
5. **`Juice/RoyalGameplayArt.BoardColor` / `BoardColorCount`** still remap and clamp visuals until Task 4.2. Their callers in Juice (`RoyalPowerupDirector.cs:323,330,342`, `RoyalGameplayArt.For`) and `RoyalQaService.cs:223,311` are Task 4.2's.
6. **Item prefab skin (Task 3.6).** The one-line runtime `AddComponent<RoyalPieceSkin>()` change for `Item.Awake` is given in Item.md §6.

## 5. Decisions and judgement calls the merge agent should know

| Where | Default chosen in the spec | Why / alternative |
|---|---|---|
| LevelManager K6 (D4) | main's `CheckWinLose`, including `Score >= star1` on the last move | Plan D4. **Owner question:** this keeps main's existing hang: goals met on the last move with `Score < star1`, so neither win nor lose. Royal Throne fixed it. |
| LevelManager K11 (D4) | main's `ShowCompleteLabel` + sound + no banner around `ReferenceWinFinish.Play`; bounded settle wait | Main's `dragBlocked \|\| GetMatches()` wait can spin forever. Task 4.10 checks the kit label against the Royal HUD. |
| LevelManager K7 | `guiObj == null → return` (not main's `_item.SetActive(false)`) | Deactivating the item would kill `Item.DestroyCor`; Royal Throne hides only the renderers. |
| LevelManager C6 | `DateOfExit = DateTime.Now.ToString()` (main) | Main's `InitScript` and `LIFESAddCounter` are kept and parse it with `DateTime.Parse`. |
| LevelManager C7 | main's `FacebookEnable` / `UnityInAppsIntegration` lines | `RoyalOfflineMode.cs` is in `import-paths.txt` (harmless, always `false`), but nothing calls it once C7 is applied. Alternative: keep RT's two lines to force services off. |
| MusicBase | delete the `RoyalAudioDirector` gate | Music is out of scope (§2); main's `StoryMusic` themes keep playing. |
| TargetIcon | main's `StorybookBoardArt.Goal` plus a lookup that also matches the art's name | Keeps the badge bound to the live `TargetObject` copy. Task 4.6 later moves both files to `RoyalVisualCatalog.Goal`. |
| InputManager | no main lines re-applied | `TrackPressPointer` reads the pressing pointer; an optional non-pointer fallback is given. |
| BoostShop | plain Royal Throne take minus `RoyalEconomy`; a `StoryFreeGame.ShopsAllowed` guard is **recommended** | D5. Zero-count kit boosts on the Royal rail would otherwise open the gem shop. |
| InitScript / AnimationManager | main's files; no members added (QA callers cut, `COUPLINGS.md` §4); `RoyalSave` alias recommended | Without the alias, QA sessions write the player's real save through `InitScript.BuyBoost/SetGems/...`. |
| Square / Item | keep the unused `campaignRefill`, `campaignWave`, `campaignMatchedItem` params and the `royalCampaignHitWave` field | Smallest diff; Royal Throne callers keep compiling. |

## 6. Not covered by these specs (verified, for completeness)

Royal Throne also modifies these kit scripts, and main keeps its versions. None of them adds a public member that imported code calls (member diff run on 2026-10-12):
- `GUI/BoostAnimation.cs` (deprecated `ParticleSystem.startColor` → `main.startColor`);
- `GUI/Counter_.cs` (string caching + `RoyalSave` alias);
- `GUI/Tutorial.cs` (`RoyalLessonDemo` pages, home-screen lessons, out of scope);
- `GUI/Tutorial/HandTutorial.cs` and `TutorialManager.cs` (main also changed `HandTutorial`);
- `GUI/PreFailed.cs` (`RoyalEconomy`);
- `GUI/LIFESAddCounter.cs`, `GUI/Level.cs`, `GUI/KeepUIInScreen.cs`, `GUI/LifeShop.cs`, `GUI/MapTargetIcon.cs`, `GUI/Offlineleadboard.cs`;
- `MapScripts/*`, `Integrations/*`, `Leadboard/*`, `System/DateTimeManager.cs`, `UnityAdsController.cs`, `UnityInAppsIntegration.cs`, `PrivacyPolicy.cs`;
- `Editor/LevelMakerEditor.cs`, `Editor/PostImporting.cs`.

`ItemAnimEvents.cs` and `ParticleSorting.cs` are identical on both branches.

UNVERIFIED: whether `BoostAnimation`'s particle API fix matters at runtime on Unity 6000.5. The deprecated setter still compiles on main today.
