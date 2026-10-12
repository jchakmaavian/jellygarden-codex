# Merge spec: `Assets/JellyGarden/Scripts/LevelManager.cs`

Plan: §4 "`LevelManager.cs`", Task 3.1 (Steps 1–4), Appendix B row `LevelManager.cs`, D3, D4, D8, Task 4.7, Task 4.10.

## 1. Base

`origin/royal-throne` (2,656 lines).

```
git show origin/royal-throne:Assets/JellyGarden/Scripts/LevelManager.cs > Assets/JellyGarden/Scripts/LevelManager.cs
```

- **Keep line 1:** `using PlayerPrefs = RoyalThrone.Testing.RoyalSave;` (Task 3.1 Step 4 default; `RoyalSave.cs` is imported in Task 3.5).
- **Namespace note:** the file is `namespace JellyGarden.Scripts` with `using JellyGarden.Scripts.System;`. In code, `Story.X` resolves to `JellyGarden.Scripts.Story.X` and `Juice.X` to `JellyGarden.Scripts.Juice.X`, and `System.X` would resolve to `JellyGarden.Scripts.System` (Royal Throne writes `global::System` for that reason). Main's blocks below already use these forms.

**Edit order.** Line numbers are Royal Throne's (`RT:n`). Apply edits bottom-up (highest line first), or match by the quoted anchor text. Each anchor is verified by `tools/port_merge_spec_check.py`.

---

## 2. KEEP-FROM-MAIN (re-apply onto the Royal Throne base)

### K1. `StoryCampaign.ApplyLevelTuning`, in `LoadLevel`

Main (`origin/main` LevelManager.cs:402–411):

```csharp
        public void LoadLevel()
        {
            currentLevel = PlayerPrefs.GetInt("OpenLevel");// TargetHolder.level;
            if (currentLevel == 0)
                currentLevel = 1;
            LoadDataFromLocal(currentLevel);
            // Apply campaign tuning after every load, including PrepareGame's
            // second load. The shipped board files remain the source layout.
            Story.StoryCampaign.ApplyLevelTuning(this);
        }
```

**Anchor:** RT `LoadLevel` (RT:437–444). After RT:442 `LoadDataFromLocal(currentLevel);`, replace the following blank line (RT:443) with main's two comment lines and `Story.StoryCampaign.ApplyLevelTuning(this);`. The method then equals main's verbatim.

### K2. `Hide()` helper

Main (main:526–534), inserted verbatim **before** RT:555 `void PrepareGame()`:

```csharp
        /// <summary>
        /// STORY: hides a piece of the kit's header, tolerating the story HUD
        /// having removed it. Unity reads a destroyed object as null, so this is
        /// the whole of the difference from a plain SetActive(false).
        /// </summary>
        private static void Hide(GameObject part)
        {
            if (part != null) part.SetActive(false);
        }

```

### K3. `PrepareGame` header null guards

**Anchor:** RT:573–579 inside `PrepareGame`:

```csharp
            blocksObject.SetActive(false);
            ingrObject.SetActive(false);
            scoreTargetObject.SetActive(false);

            star1Anim.SetActive(false);
            star2Anim.SetActive(false);
            star3Anim.SetActive(false);
```

Replace with main's (main:544–558), verbatim:

```csharp
            // STORY: all six belong to the kit's header, and StoryLevelHud
            // destroys that header once the first level of a session has
            // loaded. Every level after the first therefore ran PrepareGame
            // against the wreckage: SetActive on a destroyed GameObject throws,
            // PrepareGame stopped on this line, and everything below it never
            // ran — LoadLevel, the board, EnableMap, ShowPrePlay, OnEnterGame.
            // That is why level 1 played and level 2 was a black screen.
            // Replace Hide with SetActive(false) and the kit behaves as shipped.
            Hide(blocksObject);
            Hide(ingrObject);
            Hide(scoreTargetObject);

            Hide(star1Anim);
            Hide(star2Anim);
            Hide(star3Anim);
```

### K4. `StoryPrePlay.Open` (and with it `StoryPrePlay.Enabled`), in `PrepareGame`

**Anchor:** RT:611 `MenuManager.Instance.ShowPrePlay(true);` (in `PrepareGame`, after the `TargetBlocks` loop, before `if (limitType == LIMIT.MOVES)`). Replace that one line with main's (main:588–593), verbatim:

```csharp
            // STORY: the power plate stands in for the kit's striped goal
            // card — see StoryPrePlay. It answers whether it took the
            // screen, so StoryPrePlay.Enabled = false falls straight back
            // to the kit's own card on the next line.
            if (!JellyGarden.Scripts.Story.StoryPrePlay.Open())
                MenuManager.Instance.ShowPrePlay(true);
```

`StoryPrePlay.Enabled` is consulted inside `StoryPrePlay.Open()`. `LevelManager` does not reference it directly; main's mention at main:590 is a comment.

### K5. `IngredientExitRow(int col)`

Main (main:2252–2276), inserted verbatim **before** RT:2429 `public List<Square> GetBottomRow()`, i.e. after the closing `}` of `CheckIngredient` (RT:2427) and its blank line:

```csharp
        /// <summary>
        /// The row of the lowest cell of `col` that is not a hole, or -1 when
        /// the whole column is holes.
        ///
        /// This is the square GetBottomRow() returns for that column, and so
        /// the one square an ingredient has to reach to be collected. The
        /// ingredient arrow is drawn under it — see Square.Start. Read off the
        /// parsed level rather than off squaresArray, because Start() runs on
        /// a square before the game has any reason to promise the rest of the
        /// grid exists, and the hole pattern is fixed the moment the file is
        /// read and never changes afterwards: DestroyBlock only ever moves a
        /// square towards EMPTY, never to NONE.
        /// </summary>
        public int IngredientExitRow(int col)
        {
            if (levelSquaresFile == null || col < 0 || col >= maxCols)
                return -1;

            for (int row = maxRows - 1; row >= 0; row--)
            {
                if (levelSquaresFile[row * maxCols + col].block != SquareTypes.NONE)
                    return row;
            }
            return -1;
        }

```

Square.cs calls it (Square.md K1). `levelSquaresFile` is declared at RT:168.

### K6. Main's whole `CheckWinLose` (D4; `StoryCampaign.ProtectFirstPuzzle`; a score goal is a real goal)

**Anchor:** RT `public void CheckWinLose()` (RT:783–811). Replace the entire method with main's (main:706–735), verbatim:

```csharp
        public void CheckWinLose()
        {
            // The first Treasure Hunt puzzle is a guided introduction. Keep a
            // few moves available until its real targets are met; never auto-win.
            if (Limit <= 0 && Story.StoryCampaign.ProtectFirstPuzzle(currentLevel) &&
                targetObject.Any(i => !i.Done())) Limit = 3;
            //		print ("check win lose");
            if (Limit <= 0)
            {
                bool lose = false;
                Limit = 0;
                if (targetObject.Any(i => !i.Done())) lose = true;
                if (lose)
                    gameStatus = GameState.PreFailed;
                else if (Score >= THIS.star1 && targetObject.All(i => i.Done()))
                {
                    gameStatus = GameState.PreWinAnimations;
                }
            }
            else
            {
                bool win = targetObject.All(i => i.Done());
                if (win)
                {
                    gameStatus = GameState.PreWinAnimations;
                }


            }
        }
```

This removes Royal Throne's `if(RoyalThrone.Campaign.RoyalReferenceCampaign.CheckBonusOutcome(this))return;` (RT:785, D8) and Royal Throne's score-free last-move win (RT:794–796).

### K7. `CheckCollectedTarget`: null guard on the `guiObj` flight

Main guards `objectTarget.guiObj` (main:631–644, comment quoted there). Royal Throne restructured `AnimateCollectedItem(TargetObject objectTarget)`: it first tries the HUD flight (`RoyalGameplayPresentation.GoalIcon` → `FlyToGoal`, RT:636–640) and then falls back to the kit flight, which dereferences `guiObj` unguarded:

RT:641 `Vector2 pos = objectTarget.guiObj.transform.position;`

**Insert before RT:641:**

```csharp
                // main: guiObj is set by TargetsGroup.OnEnable on the Targets/Level<n> asset's
                // TargetObjects, not on these DeepCopy()s, so it can be null, and the throw would
                // land inside Item.DestroyCor and truncate the cascade. The target is already
                // counted by SetOff; only the flight needs an icon. The renderers are hidden above.
                if (objectTarget.guiObj == null) return;
```

**Deviation from main's literal code:** main's `else` branch did `_item.SetActive(false)`. Do **not** copy that. Royal Throne deliberately keeps `_item` active ("Keep the destruction coroutine alive while its visual clone flies to the goal", RT:633) and hides only its renderers (RT:634). Deactivating it would stop `Item.DestroyCor` mid-way, which is the very bug main's comment describes.

### K8. `StartAnimateIngredient`: `ingrObject` / `blocksObject` null guards

**Anchor:** RT:742–749 inside `IEnumerator StartAnimateIngredient(GameObject item, Vector2 pos)`:

```csharp
            GameObject[] ingr = new GameObject[2];
            ingr[0] = ingrObject.transform.Find("Ingr1").gameObject;
            ingr[1] = ingrObject.transform.Find("Ingr2").gameObject;
            if (targetBlocks > 0)
            {
                ingr[0] = blocksObject.transform.gameObject;
                ingr[1] = blocksObject.transform.gameObject;
            }
```

Replace with main's (main:666–684), verbatim:

```csharp
            // STORY: ingrObject and blocksObject are the kit's header counters,
            // which StoryLevelHud destroys. Nothing below reads `ingr` — the kit
            // fills this array and never looks at it again — so once the header
            // was gone these four lines did nothing but throw, and they threw
            // from inside a coroutine, which killed the collect animation and
            // the cascade that started it. The destination of the flight is
            // TargetIcon, which the story HUD adopts rather than destroys, so
            // the animation itself is unaffected.
            GameObject[] ingr = new GameObject[2];
            if (ingrObject != null)
            {
                ingr[0] = ingrObject.transform.Find("Ingr1").gameObject;
                ingr[1] = ingrObject.transform.Find("Ingr2").gameObject;
            }
            if (targetBlocks > 0 && blocksObject != null)
            {
                ingr[0] = blocksObject.transform.gameObject;
                ingr[1] = blocksObject.transform.gameObject;
            }
```

Royal Throne's new flight body below (pop scale, `yield return null`, `!BoardResolving()`) stays.

### K9. `UpdateBar` null guard

**Anchor:** RT:2498 `void UpdateBar()`. Insert main's guard (main:2349–2358) as the first statement, before RT:2500 `ProgressBarScript.Instance.UpdateDisplay(...)`:

```csharp
            // STORY: the score bar is chrome StoryLevelHud destroys — the King
            // Leo header carries Target and Moves and shows no score at all.
            // Without this the destroyed Image is written on every score popup,
            // and the MissingReferenceException that throws kills the coroutine
            // it is thrown inside: Item.DestroyCor never reaches DestroyBlock,
            // the cascade never settles, DragBlocked is never released, and
            // PreWinAnimationsCor waits on it forever — a won level that never
            // shows its win. Delete these two lines and the kit's own bar is
            // driven exactly as it shipped.
            if (ProgressBarScript.Instance == null) return;

```

### K10. `CheckStars`: `StoryOneStar.CapStars` and star-marker null guards

**Anchor:** RT `void CheckStars()` (RT:2504–2531). Replace the whole method with main's (main:2364–2409), verbatim:

```csharp
        void CheckStars()
        {
            if (Score >= star1 && stars <= 0)
            {
                stars = 1;
            }
            if (Score >= star2 && stars <= 1)
            {
                stars = 2;
            }
            if (Score >= star3 && stars <= 2)
            {
                stars = 3;
            }

            // STORY: a level pays one star, never three. Additive — delete this
            // line and the kit's three-star scoring is exactly as it shipped.
            stars = Story.StoryOneStar.CapStars(stars);

            // STORY: the star markers live under Stars, a child of the kit's
            // header strip, which StoryLevelHud destroys along with the bar they
            // sit on. Only the marking is skipped — the scoring above it still
            // runs, so stars are still earned. Same reason as UpdateBar: an
            // exception here is thrown inside Item.DestroyCor and takes the
            // cascade down with it.
            if (star1Anim == null || star2Anim == null || star3Anim == null) return;

            if (Score >= star1)
            {
                if (!star1Anim.activeSelf)
                    SoundBase.Instance.PlaySound(SoundBase.Instance.getStarIngr);
                star1Anim.SetActive(true);
            }
            if (Score >= star2)
            {
                if (!star2Anim.activeSelf)
                    SoundBase.Instance.PlaySound(SoundBase.Instance.getStarIngr);
                star2Anim.SetActive(true);
            }
            if (Score >= star3)
            {
                if (!star3Anim.activeSelf)
                    SoundBase.Instance.PlaySound(SoundBase.Instance.getStarIngr);
                star3Anim.SetActive(true);
            }
        }
```

The result equals Royal Throne's one-star rule (RT:2510–2511), because `CapStars` clamps to 1.

### K11. `PreWinAnimationsCor`: main's structure with `ReferenceWinFinish` in place of `StoryOriginalBoosters.SendMove` (D4, Task 4.10)

Main's method (main:737–815) runs:
1. `ShowCompleteLabel(true)`, then wait 1 s;
2. `GetRandomItems` → `NextType` → `StoryOriginalBoosters.SendMove(item)` or `ChangeType()`, 0.2 s apart;
3. wait for `StoryOriginalBoosters.MovesInFlight`, then 0.3 s;
4. the volley loop over `GetAllExtaItems()`;
5. wait 1 s, then `while (dragBlocked || GetMatches().Count > 0)`;
6. `ShowCompleteLabel(false)`, `complete[0]` sound, no `PreCompleteBanner`, then `gameStatus = GameState.Win`.

Royal Throne's `ReferenceWinFinish.Play(this)` (`Assets/ReferenceGameplay/Home/Scripts/ReferenceWinFinish.cs`) does steps 2–4 itself. It sends each move left as a comet that becomes a rocket, fires every power piece, settles (`FindMatches` + wait ≤ 6 s) and repeats, then sets `Limit = 0`.

**Anchor:** RT `IEnumerator PreWinAnimationsCor()` (RT:855–888). Replace the whole method with:

```csharp
        IEnumerator PreWinAnimationsCor()
        {
            if (preWinAnimationRunning)
                yield break;

            preWinAnimationRunning = true;
            SkipWinCelebration = false;
            JellyGarden.Scripts.Juice.AvatarEvents.NotifyGoalsCompleted(this);   // was KingVanguardActivityEvents (RT:862)

            MenuManager.Instance.ShowCompleteLabel(true);
            yield return new WaitForSeconds(1);

            // Royal Throne: a goal is met when its last piece lands in the HUD's Goal panel (at most 1 s).
            float landed = Time.unscaledTime + 1f;
            while (gameStatus == GameState.PreWinAnimations && Time.unscaledTime < landed && RoyalThrone.Presentation.RoyalGameplayPresentation.GoalPiecesInFlight > 0)
                yield return null;
            if (gameStatus != GameState.PreWinAnimations) { preWinAnimationRunning = false; yield break; }

            // D4: the leftover moves finish with Royal Throne's ReferenceWinFinish, in place of main's
            // GetRandomItems -> StoryOriginalBoosters.SendMove/ChangeType loop, its MovesInFlight wait and
            // the volley loop. Each move left becomes a rocket where it lands, every power piece fires, the
            // board refills, and it repeats while moves or power pieces are left (it ends with Limit = 0).
            yield return RoyalThrone.ReferenceGameplay.ReferenceWinFinish.Play(this);
            if (gameStatus != GameState.PreWinAnimations) { preWinAnimationRunning = false; yield break; }

            // main: let the last cascade settle before the panel (bounded; the finish settles every round).
            float settleUntil = Time.unscaledTime + 6f;
            while (gameStatus == GameState.PreWinAnimations && Time.unscaledTime < settleUntil &&
                   (IsResolvingMatches || Juice.RoyalPowerupDirector.Busy || DragBlocked))
                yield return null;

            MenuManager.Instance.ShowCompleteLabel(false);
            SoundBase.Instance.PlaySound(SoundBase.Instance.complete[0]);

            // The three-second "Wonderful!" beat used to sit here:
            //
            //     ShowPreCompleteBanner(true);
            //     yield return new WaitForSeconds(3);
            //     ShowPreCompleteBanner(false);
            //
            // Removed deliberately. A level now goes straight from its last
            // settle to the win panel. MenuManager.ShowPreCompleteBanner and
            // the PreCompleteBanner object in the scene are both left alone, so
            // putting those three lines back restores it exactly.
            gameStatus = GameState.Win;
            preWinAnimationRunning = false;
            SkipWinCelebration = false;
        }
```

This drops from Royal Throne: `Juice.RoyalWinSequence.Celebrate(this)` (RT:874), `Juice.RoyalWinSequence.BeginFinale(this)` (RT:879) and `Juice.RoyalWinSequence.EndFinale(this)` (RT:882), all D4.

Keep RT:843–853 (`SkipWinCelebration`, `RequestSkipWinCelebration`, `CelebrationWait`). `ReferenceWinFinish` reads `lm.SkipWinCelebration` (ReferenceWinFinish.cs:20,61), and the tap-to-skip in `Update` (RT:962–964) stays.

### K12. `FallingDown`: iterate a snapshot of `combinedItems` (main robustness fix)

**Anchor:** RT:1832 `foreach (List<Item> desrtoyItems in combinedItems)` (in `FallingDown`, right after RT:1831, the campaign line cut in C17). Replace the line with main's (main:1719–1733: comment plus line):

```csharp
                // Walked over a snapshot, not the field.
                //
                // combinedItems is a field of this class and this loop yields
                // inside itself, so a second pass through FallingDown reaches
                // the Clear() above while this one is still enumerating — and
                // .NET throws "Collection was modified" straight out of the
                // foreach. That kills the coroutine where it stands, which
                // leaves the cascade half finished and the board looking like it
                // is thinking. It is what the playtest caught on level 9.
                //
                // Nothing else changes: the inner lists and the items in them
                // are the same objects, in the same order, so every destroy,
                // every score popup and every particle still fires exactly as
                // it did.
                foreach (List<Item> desrtoyItems in new List<List<Item>>(combinedItems))
```

Royal Throne's `royalResolving` guard in `FindMatches` (RT:1694–1696) already prevents a second concurrent `FallingDown`, so this is defence in depth. It is cheap and keeps main's playtest fix.

### K13. `gameStatus` setter: main's `Win` branch (D4: star from `StoryFlow`, no coins)

The **`PreWinAnimations` branch.** RT:376–380:

```csharp
                else if (value == GameState.PreWinAnimations)
                {
                    CommitWin();
                    StartCoroutine(PreWinAnimationsCor());
                }
```

Delete the line `CommitWin();`. The branch then equals main's (main:344–347).

The **`Win` branch.** Replace RT:381–405, from `else if (value == GameState.Win)` through its closing `}` before the setter's two blank lines, with main's (main:348–370), verbatim:

```csharp
                else if (value == GameState.Win)
                {
                    //InitScript.THIS.AddGems(1);//Add coins after win
                    MenuManager.Instance.ShowMenuComplete();
                    if (!InitScript.Instance.losingLifeEveryGame)
                        InitScript.Instance.AddLife(1);
                    OnMenuComplete();
                    if (PlayerPrefs.GetInt(string.Format("Level.{0:000}.StarsCount", currentLevel), 0) < stars)
                        PlayerPrefs.SetInt(string.Format("Level.{0:000}.StarsCount", currentLevel), stars);
                    if (Score > PlayerPrefs.GetInt("Score" + currentLevel))
                    {
                        PlayerPrefs.SetInt("Score" + currentLevel, Score);
                    }

                    #if PLAYFAB || GAMESPARKS
                NetworkManager.dataManager.SetPlayerScore(currentLevel, Score);
                NetworkManager.dataManager.SetPlayerLevel(currentLevel + 1);
                NetworkManager.dataManager.SetStars();
                    #endif
                    MenuManager.Instance.ShowCompleteLabel(true);
                    SoundBase.Instance.PlaySound(SoundBase.Instance.complete[1]);
                    OnWin();
                }
```

**Keep** Royal Throne's duplicate-win guard at the top of the setter (RT:313–314):

```csharp
                // Repeated completion notifications must not pay the same win twice.
                if (value == GameState.Win && GameStatus == GameState.Win) return;
```

It stops a second `OnWin` (which `StoryFlow` listens to) from awarding twice.

---

## 3. CUT-FROM-ROYAL-THRONE

Each row is either deleted or replaced by the named main equivalent. `IsSpecialItem` / `RoyalReferenceCampaign.Active` are always false here (D8, campaign not imported), so removing those terms does not change behaviour.

| # | RT line(s) | Member | Royal Throne text (exact, first line) | Action |
|---|---|---|---|---|
| C1 | 98–99 | field | `/// <summary>Continues bought in this attempt: each one costs more (RoyalEconomy.ContinueCost).</summary>` / `[HideInInspector] public int ContinuesBought;` | delete both lines (`RoyalEconomy`; nothing in the import set reads it) |
| C2 | 159 | `ActivatedBoost` setter | `RoyalThrone.Characters.KingVanguardActivityEvents.NotifyExtraMovesGranted(this);` | `JellyGarden.Scripts.Juice.AvatarEvents.NotifyExtraMovesGranted(this);` |
| C3 | 174–177 | fields | `/// <summary>Stars this win added to the Area balance: ...` / `public int StarsGained { get; private set; }` / `// A win's lasting effects are paid once per attempt (CommitWin), ...` / `bool winCommitted; int committedCoins, committedCoinsBefore;` | delete the 4 lines (`CommitWin`, D4). Only the non-imported `RoyalWinSequence` reads `StarsGained`/`CommittedCoins`. |
| C4 | 378 | setter, `PreWinAnimations` | `CommitWin();` | delete (K13) |
| C5 | 381–405 | setter, `Win` | `CommitWin();` / `stars = 1;` / `Juice.RoyalWinSequence.Reward(this, committedCoins, committedCoinsBefore);` / `PlayerPrefs.Save();` … | replace the branch with main's (K13) |
| C6 | 467 | `EnableMap` | `InitScript.DateOfExit = DateTime.UtcNow.ToString("o", global::System.Globalization.CultureInfo.InvariantCulture);  //1.4` | `InitScript.DateOfExit = DateTime.Now.ToString();  //1.4` (main:434). Main's `InitScript` and `GUI/LIFESAddCounter.cs:44` read `DateOfExit` with `DateTime.Parse` and write it with `DateTime.Now.ToString()`, and main's `InitScript` is kept. |
| C7a | 506 | `Start`, `#if FACEBOOK` | `FacebookEnable = RoyalThrone.RoyalOfflineMode.ServicesEnabled;` | `FacebookEnable = true;//1.6.2` (main:473) |
| C7b | 515–516 | `Start`, `#if UNITY_INAPPS` | `enableInApps = RoyalThrone.RoyalOfflineMode.ServicesEnabled;` / `if (enableInApps) gameObject.AddComponent<UnityInAppsIntegration>();` | main:482–483: `gameObject.AddComponent<UnityInAppsIntegration>();` / `enableInApps = true;//1.6.1`. `RoyalOfflineMode.cs` is not imported. Main defines `UNITY_INAPPS` for Standalone, so this matches main exactly. |
| C8 | 542 | `InitLevel` | `RoyalThrone.Campaign.RoyalReferenceCampaign.Build(this);` | delete |
| C9 | 563 | `PrepareGame` | `winCommitted = false; StarsGained = 0; committedCoins = 0; ContinuesBought = 0;` | delete the line. Keep the other resets RT:557–570 (`royalResolving`, `InputSwaps`, `inputRevision`, `preWinAnimationRunning`, `SkipWinCelebration`, `ingredientFly`, `DragBlocked`). |
| C10 | 599–600 | `PrepareGame` | `// The profile's first-try wins (RoyalProfile): an attempt at this level begins here, once per attempt.` / `RoyalThrone.RoyalProfile.AttemptStarted(...)` | delete both lines (profile is out of scope) |
| C11 | 785 | `CheckWinLose` | `if(RoyalThrone.Campaign.RoyalReferenceCampaign.CheckBonusOutcome(this))return;` | gone with K6 |
| C12 | 813–841 | `CommitWin` | from `/// <summary>` (`A win's lasting effects, once per attempt…`) through the closing `}` of `void CommitWin()`, including `public int CommittedCoins => winCommitted ? committedCoins : 0;` | delete the whole block (D4). Keep RT:843–853. |
| C13 | 862, 874, 879, 882 | `PreWinAnimationsCor` | `KingVanguardActivityEvents.NotifyGoalsCompleted`, `Juice.RoyalWinSequence.Celebrate/BeginFinale/EndFinale` | gone with K11 (avatar call becomes `AvatarEvents`) |
| C14 | 948–956 | `Update` | `if (InputManager.Instance.Gameplay.Cancel.WasPressedThisFrame())` / `RoyalThrone.Presentation.RoyalBackButton.NotePress();` and the `WasReleasedThisFrame` block using `RoyalBackButton.Consumed` / `.Quit()` | replace RT:948–958 with main's block (main:875–882; below). `RoyalBackButton` is Royal Throne's home-screen back stack, which is not imported. |
| C15 | 1483–1484 | `RegenMatches` | `if(onlyFalling&&RoyalThrone.Campaign.RoyalReferenceCampaign.Active&&gameStatus!=GameState.PreWinAnimations)` / `{yield return new WaitForFixedUpdate();yield break;}` | delete both lines (the 3 comment lines RT:1480–1482 may go too) |
| C16 | 1591–1598 | `GetRandomItems` | `if(RoyalThrone.Campaign.RoyalReferenceCampaign.Active)` … `return selected;` `}` | delete the 8 lines. Royal Throne's general fix below (RT:1612–1615, "Only plain pieces can be picked") stays. |
| C17 | 1669–1670 | `DestroyItems` | `if(RoyalThrone.Campaign.RoyalReferenceCampaign.Active&&gameStatus==GameState.RegenLevel&&` / `RoyalThrone.Campaign.RoyalCampaignBlockers.IsSpecialItem(item.GetComponent<Item>()))continue;` | delete both lines |
| C18 | 1831 | `FallingDown` | `if(RoyalThrone.Campaign.RoyalReferenceCampaign.Active)RoyalThrone.Campaign.RoyalCampaignBlockers.BeginMatchWave();` | delete |
| C19 | 1839 | `FallingDown` | `RoyalThrone.Characters.KingVanguardActivityEvents.NotifyMatchResolved(this, matchedPieces);` | `JellyGarden.Scripts.Juice.AvatarEvents.NotifyMatchResolved(this, matchedPieces);` |
| C20 | 2536–2537 | `LoadDataFromLocal` | `if(RoyalThrone.Campaign.RoyalReferenceCampaign.TryLoad(this,currentLevel,out var referenceBoard))` / `{levelSquaresFile=referenceBoard;return;}` | delete both lines |

C14 replacement (main:875–882), replacing RT:948–958 up to and including the `}` that closes the `WasReleasedThisFrame` block:

```csharp
                if (InputManager.Instance.Gameplay.Cancel.WasReleasedThisFrame())
                {
                    if (LevelManager.THIS.gameStatus == GameState.Playing)
                        MenuManager.Instance.ShowMenuPause(true);
                    else if (LevelManager.THIS.gameStatus == GameState.Map)
                        Application.Quit();

                }
```

After §2 and §3, this must return nothing:

```
grep -nE "RoyalThrone\.(Campaign|Characters|RoyalProfile|RoyalOfflineMode)|RoyalBackButton|RoyalWinSequence|RoyalEconomy|CommitWin|winCommitted|StarsGained|ContinuesBought|BoardColorCount" Assets/JellyGarden/Scripts/LevelManager.cs
```

### Keep from Royal Throne (plan §4; do not touch)

- **Input:** `BoardInputAllowed` (RT:254–257), `InputSwaps`, `BeginInputSwap`/`EndInputSwap`, `PreferredMatchHost`. The `Update` press path uses `BoardInputAllowed` + `CanAcceptInput` (RT:966–1006).
- **Resolver:** `royalResolving`, `IsResolvingMatches`, `BoardResolving()`, the `FindMatches` guard, and the `FallingDown` revision loop with gravity waiting for swaps (RT:1799–1810, 1962–1975, 1990–1993, 2058–2068, 2142).
- **Fill and shuffle:** `GenerateNewItems(bool falling, bool fillBlockedCells)` (RT:1393–1434) and `Juice.RoyalShuffle.Run` in `NoMatchesCor` (RT:1446–1459).
- **Goals:** `FlyToGoal` and `PlayMatchCenterDelayed`; `AnimateCollectedItem(TargetObject)`; `GetIngredientsCount` (Royal Throne's version supersedes main's sprite-null fix).
- **Guards:** `GetSquare` bounds (RT:2210); the `GetRandomItems` count fix (RT:1612–1615); `TimeTick` held by `TimerHeld()` (RT:1011–1017, uses `MenuManager.BlocksBoardInput`, see MenuManager.md); the `timeTick` handle (RT:200, 547–548).
- **Setup:** `AddComponent<RoyalThrone.Presentation.RoyalGameplayPresentation>()` in `Start` (RT:524–525, only when `Juice.RoyalEffectsProfile.Get`); `Juice.RoyalGameplayWarmup.Begin(this)` (RT:331).
- **Combo:** `Juice.RoyalCelebration.Play(combo)` (RT:2137) in place of `gratzWords`.
- **Win:** `SkipWinCelebration` / `RequestSkipWinCelebration` / tap-to-skip (RT:843–853, 962–964).

## 4. D3 colour changes (Task 3.1 Step 3)

| RT line | Method | Royal Throne text (exact) | Replace with |
|---|---|---|---|
| 2591 | `ProcessGameDataFromString`, `COLOR LIMIT` | `colorLimit = Mathf.Clamp(int.Parse(blocksString), 1, Juice.RoyalGameplayArt.BoardColorCount);` | `colorLimit = int.Parse(blocksString);` (the kit's and main's line) |

The level's `COLOR LIMIT` (3–6; 403 levels use 6) is kept as authored. `Item.GenColor` bounds it by `items.Length` (Item.md §4). This file has no colour-5 → 1 remap.

## 5. Dropped main hooks (not re-applied)

Main line numbers from `origin/main`:
- `JellyGarden.Scripts.Story.StoryRoyalVfx.StateChanged(value);` (main:285, `gameStatus` setter).
- `JellyGarden.Scripts.Story.KingLeoVfxHost.Configure();` (main:496, `Start`). **Conditional:** if Task 3.7 finds a meta user and keeps `KingLeoVfxHost`, re-add it after RT:523 `Instance = this;`, with main's comment block main:492–495.
- `if (JellyGarden.Scripts.Story.StoryBoardTheme.DrawKitOutline)` before `GenerateOutline();` (main:513, `InitLevel`). Royal Throne calls `GenerateOutline()` unconditionally (RT:543); the Royal layer hides the kit outline itself (Task 4.1).
- `StoryBoardScale.Apply(this)` (main:974, `GenerateLevel`), `.Factor` (main:1028–1029, `CreateSquare`), `.FitToCell(block.transform)` ×7 (main `CreateObstacles` and friends: lines 1048, 1060, 1068, 1297, 1308, 1320, 1331).
- `StoryOriginalMotion.BeginRefill()`, `Item storyCreated =` / `.Enter(storyCreated, falling, col, row)` (main:1347, 1358–1361, `GenerateNewItems`), and `.MarkMatch(...)` (main:1758, `FallingDown`).
- `StoryOriginalBoosters.SendMove(item)`, `.MovesInFlight` and main's volley loop (main:747–798, `PreWinAnimationsCor`), replaced by `ReferenceWinFinish` (K11).
- `StoryRocketSweep.HasPendingHits`, `StoryOriginalTnt.Anticipating`, `StoryOriginalColorBall.Busy` (main:2045–2049, `IsAllDestoyFinished`). Royal Throne's `InputSwaps` / `RoyalPowerupDirector.Busy` gates (RT:2224–2225) replace them.
- `itemTouched.BeginDragFromCurrentTouch();` (main:932, `Update`). Royal Throne's `Item` owns press, drag and release.
- The `[OverlapProbe]` orphan scan (main:1891–1896; `Debug.LogWarning("[OverlapProbe] orphan ...` at main:1894; after `FallingDown`'s loop).
- Main's `GetIngredientsCount` sprite-null guard (main:1554–1555), superseded by Royal Throne's (RT:1650–1658).
- Main's `InitLevel` `StopCoroutine(TimeTick())` / `StartCoroutine(TimeTick())` pair, superseded by Royal Throne's `timeTick` handle (RT:547–548).

## 6. Risks / judgement calls

- **D4: a win can hang at 0 moves.** Main's `CheckWinLose` requires `Score >= star1` for the last-move win (K6). If all goals are met on the last move with `Score < star1`, the level neither wins nor fails. Royal Throne fixed exactly this (RT:794–795 comment). Main has it today. Under the Royal layer, `BoardInputAllowed` also needs `Limit > 0`, so the board is then simply frozen. Recommend asking the owner whether to add `|| targetObject.All(i => i.Done() && i.type != Target.SCORE)`, keeping score goals real (D4: "a score goal is a real goal"). UNVERIFIED how many shipped levels can hit this; `StoryOneStar` makes `star1` the pass mark.
- **`ShowCompleteLabel(true/false)` is main's kit label** (K11). Royal Throne replaced it with `RoyalWinSequence.Celebrate`, which D4 does not port. Task 4.10 should check that the kit label does not clash with the Royal HUD. If it does, delete those two lines; plan D4 only fixes the flow, not the label.
- **Settle wait in K11.** Main waited on `dragBlocked || GetMatches().Count > 0`, which can spin forever if a match is left with no resolver running. K11 instead waits on Royal Throne's resolver state, for at most 6 s. `ReferenceWinFinish` already settles each round (`Settle()`: `FindMatches()` then wait ≤ 6 s).
- **`ReferenceWinFinish.Regular()` calls `RoyalThrone.Campaign.RoyalCampaignBlockers.IsSpecialItem(i)`** (ReferenceWinFinish.cs:17). That is a coupling cut for `COUPLINGS.md` / Task 3.5, not part of this file, but this file's K11 depends on it compiling.
- **`StoryLevelHud` no longer destroys the kit header after Task 3.7.** The K3, K8, K9 and K10 guards then become dormant, but stay as cheap insurance. Task 3.7 Step 5 retires the in-level `StoryLevelHud`, and `RoyalGameplayPresentation` may still hide the header.
- **`GenerateOutline()` with the switch off (Phase 3)** draws the kit outline. That matches the "kit fallback" checkpoint in Task 3.8.
- **Avatar API.** C2, C19 and K11 call `JellyGarden.Scripts.Juice.AvatarEvents.Notify*`. The contract is in INDEX.md §3. The class must exist before Phase 3 compiles (Task 3.2 Step 4).
- **`RoyalGameplayPresentation.GoalIcon` / `GoalLaunched` / `GoalLanded` / `GoalPiecesInFlight`** are in the §5 GUI subset (imported). If Task 1.3 trims that subset, K11 and `CheckCollectedTarget` must change with it.

```anchors
path: Assets/JellyGarden/Scripts/LevelManager.cs
rt: using PlayerPrefs = RoyalThrone.Testing.RoyalSave;
rt: using JellyGarden.Scripts.System;
rt: namespace JellyGarden.Scripts
rt: public void LoadLevel()
rt: LoadDataFromLocal(currentLevel);
rt: void PrepareGame()
rt: blocksObject.SetActive(false);
rt: ingrObject.SetActive(false);
rt: scoreTargetObject.SetActive(false);
rt: star1Anim.SetActive(false);
rt: star3Anim.SetActive(false);
rt: MenuManager.Instance.ShowPrePlay(true);
rt: public List<Square> GetBottomRow()
rt: SquareBlocks[] levelSquaresFile = new SquareBlocks[81];
rt: public void CheckWinLose()
rt: if(RoyalThrone.Campaign.RoyalReferenceCampaign.CheckBonusOutcome(this))return;
rt: else if (targetObject.All(i => i.Done()))
rt: void AnimateCollectedItem(TargetObject objectTarget)
rt: if (spriteRenderer && RoyalThrone.Presentation.RoyalGameplayPresentation.GoalIcon(objectTarget, out var icon, out var layer))
rt: Vector2 pos = objectTarget.guiObj.transform.position;
rt: // Keep the destruction coroutine alive while its visual clone flies to the goal.
rt: IEnumerator StartAnimateIngredient(GameObject item, Vector2 pos)
rt: ingr[0] = ingrObject.transform.Find("Ingr1").gameObject;
rt: if (targetBlocks > 0)
rt: void UpdateBar()
rt: ProgressBarScript.Instance.UpdateDisplay((float)Score * 100f / ((float)star1 / ((star1 * 100f / star3)) * 100f) / 100f);
rt: void CheckStars()
rt: // A win is worth exactly one star (the reference; the user's decision, 2026-09-29): the score no longer adds
rt: IEnumerator PreWinAnimationsCor()
rt: RoyalThrone.Characters.KingVanguardActivityEvents.NotifyGoalsCompleted(this);
rt: while (gameStatus == GameState.PreWinAnimations && Time.unscaledTime < landed && RoyalThrone.Presentation.RoyalGameplayPresentation.GoalPiecesInFlight > 0)
rt: yield return Juice.RoyalWinSequence.Celebrate(this);
rt: Juice.RoyalWinSequence.BeginFinale(this);
rt: yield return RoyalThrone.ReferenceGameplay.ReferenceWinFinish.Play(this);
rt: yield return Juice.RoyalWinSequence.EndFinale(this);
rt: public bool SkipWinCelebration { get; private set; }
rt: public void RequestSkipWinCelebration()
rt: foreach (List<Item> desrtoyItems in combinedItems)
rt: if (value == GameState.Win && GameStatus == GameState.Win) return;
rt: else if (value == GameState.PreWinAnimations)
rt: CommitWin();
rt: else if (value == GameState.Win)
rt: Juice.RoyalWinSequence.Reward(this, committedCoins, committedCoinsBefore);
rt: [HideInInspector] public int ContinuesBought;
rt: RoyalThrone.Characters.KingVanguardActivityEvents.NotifyExtraMovesGranted(this);
rt: public int StarsGained { get; private set; }
rt: bool winCommitted; int committedCoins, committedCoinsBefore;
rt: InitScript.DateOfExit = DateTime.UtcNow.ToString("o", global::System.Globalization.CultureInfo.InvariantCulture);  //1.4
rt: FacebookEnable = RoyalThrone.RoyalOfflineMode.ServicesEnabled;
rt: enableInApps = RoyalThrone.RoyalOfflineMode.ServicesEnabled;
rt: if (enableInApps) gameObject.AddComponent<UnityInAppsIntegration>();
rt: RoyalThrone.Campaign.RoyalReferenceCampaign.Build(this);
rt: winCommitted = false; StarsGained = 0; committedCoins = 0; ContinuesBought = 0;
rt: RoyalThrone.RoyalProfile.AttemptStarted(currentLevel, PlayerPrefs.GetInt(string.Format("Level.{0:000}.StarsCount", currentLevel), 0) > 0);
rt: public int CommittedCoins => winCommitted ? committedCoins : 0;
rt: void CommitWin()
rt: if (InputManager.Instance.Gameplay.Cancel.WasPressedThisFrame())
rt: RoyalThrone.Presentation.RoyalBackButton.NotePress();
rt: if (LevelManager.THIS.gameStatus == GameState.Playing && !RoyalThrone.Presentation.RoyalBackButton.Consumed)
rt: else if (LevelManager.THIS.gameStatus == GameState.Map && !RoyalThrone.Presentation.RoyalBackButton.Consumed && !MenuManager.Instance.BlocksBoardInput)
rt: RoyalThrone.Presentation.RoyalBackButton.Quit();
rt: if(onlyFalling&&RoyalThrone.Campaign.RoyalReferenceCampaign.Active&&gameStatus!=GameState.PreWinAnimations)
rt: {yield return new WaitForFixedUpdate();yield break;}
rt: if(RoyalThrone.Campaign.RoyalReferenceCampaign.Active)
rt: if(RoyalThrone.Campaign.RoyalReferenceCampaign.Active&&gameStatus==GameState.RegenLevel&&
rt: RoyalThrone.Campaign.RoyalCampaignBlockers.IsSpecialItem(item.GetComponent<Item>()))continue;
rt: if(RoyalThrone.Campaign.RoyalReferenceCampaign.Active)RoyalThrone.Campaign.RoyalCampaignBlockers.BeginMatchWave();
rt: RoyalThrone.Characters.KingVanguardActivityEvents.NotifyMatchResolved(this, matchedPieces);
rt: if(RoyalThrone.Campaign.RoyalReferenceCampaign.TryLoad(this,currentLevel,out var referenceBoard))
rt: {levelSquaresFile=referenceBoard;return;}
rt: colorLimit = Mathf.Clamp(int.Parse(blocksString), 1, Juice.RoyalGameplayArt.BoardColorCount);
rt: if (Juice.RoyalEffectsProfile.Get && !GetComponent<RoyalThrone.Presentation.RoyalGameplayPresentation>())
rt: Instance = this;
rt: GenerateOutline();
rt: Juice.RoyalCelebration.Play(combo);
rt: Juice.RoyalGameplayWarmup.Begin(this);
rt: bool TimerHeld() => (MenuManager.Instance && MenuManager.Instance.BlocksBoardInput) ||
rt: bool GenerateNewItems(bool falling = true, bool fillBlockedCells = false)
rt: yield return Juice.RoyalShuffle.Run(this, ok => shuffled = ok);
rt: public bool BoardInputAllowed =>
rt: public bool IsResolvingMatches => royalResolving;
rt: if (row < 0 || col < 0 || row >= maxRows || col >= maxCols)
main: Story.StoryCampaign.ApplyLevelTuning(this);
main: private static void Hide(GameObject part)
main: Hide(blocksObject);
main: if (!JellyGarden.Scripts.Story.StoryPrePlay.Open())
main: public int IngredientExitRow(int col)
main: if (Limit <= 0 && Story.StoryCampaign.ProtectFirstPuzzle(currentLevel) &&
main: else if (Score >= THIS.star1 && targetObject.All(i => i.Done()))
main: if (objectTarget.guiObj != null)
main: if (ingrObject != null)
main: if (targetBlocks > 0 && blocksObject != null)
main: if (ProgressBarScript.Instance == null) return;
main: stars = Story.StoryOneStar.CapStars(stars);
main: if (star1Anim == null || star2Anim == null || star3Anim == null) return;
main: if (!JellyGarden.Scripts.Story.StoryOriginalBoosters.SendMove(item))
main: while (JellyGarden.Scripts.Story.StoryOriginalBoosters.MovesInFlight > 0)
main: MenuManager.Instance.ShowCompleteLabel(true);
main: MenuManager.Instance.ShowCompleteLabel(false);
main: SoundBase.Instance.PlaySound(SoundBase.Instance.complete[0]);
main: foreach (List<Item> desrtoyItems in new List<List<Item>>(combinedItems))
main: InitScript.DateOfExit = DateTime.Now.ToString();  //1.4
main: FacebookEnable = true;//1.6.2
main: enableInApps = true;//1.6.1
main: JellyGarden.Scripts.Story.StoryRoyalVfx.StateChanged(value);
main: JellyGarden.Scripts.Story.KingLeoVfxHost.Configure();
main: if (JellyGarden.Scripts.Story.StoryBoardTheme.DrawKitOutline)
main: JellyGarden.Scripts.Story.StoryBoardScale.Apply(this);
main: JellyGarden.Scripts.Story.StoryBoardScale.FitToCell(block.transform);
main: JellyGarden.Scripts.Story.StoryOriginalMotion.BeginRefill();
main: JellyGarden.Scripts.Story.StoryOriginalMotion.Enter(storyCreated, falling, col, row);
main: if (JellyGarden.Scripts.Story.StoryRocketSweep.HasPendingHits) return false;
main: if (JellyGarden.Scripts.Story.StoryOriginalTnt.Anticipating) return false;
main: if (JellyGarden.Scripts.Story.StoryOriginalColorBall.Busy) return false;
main: itemTouched.BeginDragFromCurrentTouch();
main: else if (LevelManager.THIS.gameStatus == GameState.Map)
main: Application.Quit();
path: Assets/ReferenceGameplay/Home/Scripts/ReferenceWinFinish.cs
rt: public static IEnumerator Play(LevelManager owner){var view=owner.GetComponent<ReferenceWinFinish>();if(!view)view=owner.gameObject.AddComponent<ReferenceWinFinish>();yield return view.Run(owner);}
```
