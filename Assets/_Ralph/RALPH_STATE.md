# Royal Throne - Ralph loop state

Maintained by the QA/fix loop. Earlier iteration entries are never erased; new entries are appended at the end.

## Scope (agreed 2026-09-24)

- **In scope:** Royal Throne's own NON-GAMEPLAY systems. That means a bug hunt with reproduce -> root cause
  -> fix -> compile -> Play Mode retest -> edge case -> connected flow -> console -> regression, plus
  performance, texture import optimisation, genuinely missing standard features built in Royal Throne's
  own design, and a player-facing brand check (only "Royal Throne" is ever shown).
- **Out of scope:**
  - importing or reproducing anything from the reference APK (it is a competitor's game: quality
    reference only, per `SourceReference/CLAUDE.md`);
  - PNG -> WebP conversion. Unity re-compresses every texture at import, so the source format does not
    change build size or memory, and Unity has no native WebP importer.
- **Protected, never redesigned or rebalanced:** Levels 1-50, the match-3 board implementation, level
  designs and balance, the home layout, the splash, the loading screen and the logo/branding art. An
  integration bug in these may get the smallest possible fix, followed by a Levels 1-50 regression.
- **Shared editor:** another agent (Codex) and the user also use this Unity editor.
  - Check `editor_status` and `Design/ReferenceGameplayImport/status.json` before any recompile, Play
    Mode start or stop.
  - Never stop or recompile a Play session this loop did not start without asking.
- **Level tests** run in the QA test profile (`RoyalQaService.StartSession`), whose saves go to QA keys
  only.
- **Commits:** small path-limited checkpoints on `visual-rebuild-v2` containing only the loop's own
  hunks. Other uncommitted work in the tree is never staged. The pre-loop state is preserved on branch
  `backup/pre-ralph-2026-09-24`.

## Baseline (2026-09-24 19:15 local)

- Branch `visual-rebuild-v2` at `b76295af`. About 15,200 uncommitted paths from earlier work, all
  snapshotted in `backup/pre-ralph-2026-09-24` (`d6fe15ce`).
- Unity 6000.5.10f1. Scripts compile (`recompile_status: completed, failed=false`). Compile warnings
  are pre-existing: obsolete-API CS0618, unused variables and serialization analyzer notes.
- Console during the user's hour-long Play session (level 14, normal profile): no game errors.
- Last full King Venguard Play Mode run: 0 errors or warnings from game code.

## Feature inventory (static code pass, 2026-09-24)

Genre-standard non-gameplay systems. The checklist comes from the structural notes in
`Design/Inspection-ReferenceBaseline/REFERENCE-INVENTORY.md` (screen names only). Status is for Royal
Throne's own implementation.

| System | Royal Throne implementation | Status |
|---|---|---|
| Startup | Unity splash -> `main.unity` title (RoyalTitleScreen) -> async loading card -> `game.unity` home | Works (protected) |
| First launch | InitScript `Lauched` flag seeds lives 5, coins 5, music and sound on | Works; no booster seed |
| Home and navigation | RoyalHomeScreen with ReferenceHomeLayout; one reusable dialog; Story grid for levels 1-100 | Works (layout protected). Leagues, Team, Collection and area chests are "coming soon" text |
| Back / Android back | Cancel = `<Keyboard>/escape`; handlers in LevelManager, AnimationManager, RoyalHomeScreen, RoyalHintPresenter | **Defects:** quits the app after closing a popup; cannot resume from pause |
| Settings | Home dialog and pause menu: Music, Sound, Motion, Hints; saved at once | Works. No haptics, notifications or language (not implemented anywhere) |
| Audio | RoyalAudioDirector (one castle theme, 12 SFX voices); SoundBase and MusicBase forward to it | Works; minor mapping gaps |
| Lives | InitScript UTC-deadline refill, 5 max, 5 min each; LifeShop refill for 12 coins | Works; 0-lives flow ends in the dead gem shop |
| Economy | One balance (`Gems`, shown as coins); boosters bought with coins; no income offline | **Design gap:** 5 coins lifetime supply. Gem shop shows $ packs that do nothing |
| Pre-level | MenuPlay with 3 boosters; BoostInfo; BoostShop | **Defect:** pack purchase overwrites stock |
| Win flow | PreWinAnimations -> Win -> MenuComplete -> CONTINUE -> next MenuPlay | **Defects:** crash after level 100; stars not flushed to disk |
| Fail flow | PreFailed (continue for 12 or give up) -> MenuFailed -> TRY AGAIN | Works; 2 s dead-tap window |
| Pause | HUD gear or Back -> MenuPause: Continue, Music, Sound, Hints, Home | **Defect:** gear works during the win celebration. Home has no confirmation. HELP has a label but no button |
| HUD | RoyalGameplayPresentation overlay, sort 20 | **Defect:** stays tappable above in-level popups (camera canvas, sort 2) |
| Tutorials | Level-1 hand tutorial; BoostInfo; unreachable "How to play" popup | **Defect (suspected):** re-entry after leaving level 1 untouched throws |
| Persistence | PlayerPrefs through RoyalSave; saves on currency, lives and settings changes, app pause and quit | **Defects:** win progress not flushed; locale-dependent DateTime.Parse at startup |
| Pause/resume (OS) | InitScript saves lives on pause and quit | No auto-pause of a running level |
| Offline | RoyalOfflineMode: ads, IAP, social and analytics off; INTERNET permission stripped | Works; ad-quota code still runs |
| Safe area | Home, title and HUD read `Screen.safeArea`; legacy popups do not | Suspected: home tab-bar background ignores the bottom inset (protected layout) |
| Characters | King Venguard in 5 slots | Verified 2026-09-24 |
| Brand (player-facing) | The competitor's name appears in no scene, prefab, project setting or UI string | **Pass** (static). The only hits are code identifiers such as `royalMatchSize` and dev docs |

## Backlog (priority order; "latent" = not reachable with current data)

| ID | P | Issue | Where |
|---|---|---|---|
| R-01 | P0 | CONTINUE after winning level 100 loads missing level 101: NullReferenceException, win card stuck | AnimationManager.CloseMenu :329; LevelManager.LoadLevel :2346 |
| R-02 | P0? | `GetRandomItems` can loop forever (pre-level boosters, or a colour bomb during the win finish); reproduce first | LevelManager :1442-1455 (gameplay: smallest fix only) |
| R-03 | P1 | Back on home closes a dialog or popup **and** quits the app | LevelManager.Update :827-833 |
| R-04 | P1 | HUD pause gear works in any state (win celebration, other popups); HUD above popups takes taps | RoyalGameplayPresentation :107, :151 |
| R-05 | P1 | Win stars, score and OpenLevel written without `Save()` (lost on kill) | LevelManager Win branch :376-381; AnimationManager :329 |
| R-06 | P1 | Locale-dependent `DateTime.Parse` of ad-quota dates in InitScript.Awake can throw on every launch | DateTimeManager :26/:42/:59/:92; InitScript :202; RewardedButton |
| R-07 | P1 | Pre-level booster pack purchase sets stock instead of adding | AnimationManager.BuyBoost :596 -> InitScript.BuyBoost |
| R-08 | P1 | Offline gem shop: $ packs whose Buy does nothing, no feedback | AnimationManager.Buy :468, GemsShop |
| R-09 | P1? | Level-1 tutorial hand throws when re-entering after leaving untouched; reproduce | HandTutorial :42, TutorialManager |
| R-10 | P2 | Back cannot resume from pause, cancel a selected tool or dismiss result cards | AnimationManager :150, LevelManager :829 |
| R-11 | P2 | Timed levels: HUD shows seconds under "Moves"; the Royal Hammer slot is hidden | RoyalGameplayPresentation :85/:157; LevelManager :602 |
| R-12 | P2 | PreFailed CONTINUE / END LEVEL ignore taps for about 2 s | AnimationManager :558 |
| R-13 | P2 | No auto-pause when the app goes to background during a level | InitScript.OnApplicationPause |
| R-14 | P2 | Pause HOME abandons the level (life lost) with no confirmation | AnimationManager.BackToMap |
| R-15 | P2 | HELP label in pause menu without a button; How-to-play popup unreachable | RoyalMenuSkin :127 |
| R-16 | P2 | Hidden legacy map may still accept taps (device check) | MapLevel, LevelsMap |
| R-17 | P3 | Sound/Music default 0 vs 1 mismatch; `print(DateOfExit)` every launch; plurals ("1 coins"); timer formats | various |
| R-18 | P3 | Project-code compile warnings (about 30 runtime, 40 editor); obsolete Find APIs | see compile log |
| R-19 | P3 | Hidden HUD and home kings keep animating; AI hint coroutine pile-up | RoyalGameplayPresentation, AI |
| R-20 | design | Offline economy has no coin income (5 coins lifetime). Product decision, not changed by the loop | LevelManager :371 (commented out) |

## Iteration log

### Iteration 1
- **Date/time:** 2026-09-24 23:40-23:45 local
- **Area inspected:** win flow, MenuComplete CONTINUE (R-01)
- **Issue:** CONTINUE after winning the last authored level (100) tries to open level 101. That throws, and the win card stays on screen for good.
- **Severity:** P0 (blocking navigation)
- **Reproduction:** QA profile: open level 100, Win instantly, press CONTINUE. Result: state Map, `level=101`, `OpenLevel=101`, `MenuComplete` still open over the home screen; every further tap throws again (evidence `play/r01_after_continue.png`, pre-fix).
- **Root cause:** `AnimationManager.CloseMenu` (MenuComplete branch) always sets `OpenLevel` to the current level + 1 and calls `LevelManager.LoadLevel()`. `LoadDataFromLocal` dereferences `Resources.Load("Levels/101")`, which is null, before `SetActive(false)` runs.
- **Files changed:** `Assets/JellyGarden/Scripts/GUI/AnimationManager.cs`
- **Fix:** open the next level only if its level file exists. After the last level, CONTINUE returns to the home screen. Nothing else in the branch changed.
- **Tests executed:** recompile; fresh Play Mode. Level 100 -> Win -> CONTINUE. Regression: level 12 -> Win -> CONTINUE.
- **Result:** level 100: state Map, level stays 100, `OpenLevel=100`, no popup open, home visible (screenshot checked).
- **Regression result:** level 12 -> CONTINUE opens level 13's pre-level card (`OpenLevel=13`). The QA session end restored the real profile (`OpenLevel=14`).
- **Console result:** no errors. Only the Unity AI package's "Account API" warning, which is not project code.
- **Performance result:** n/a (one `Resources.Load` per CONTINUE tap).
- **Screenshots/evidence:** scratchpad `kv/play/r01_after_continue.png` (post-fix), driver log `kv/play/ralph-*.txt`
- **Remaining risk:** the reference-campaign QA mode caps at 50 and reopens level 50 (unchanged, QA only).
- **Next task:** R-03 (Back on home quits the app after closing a popup).

### Iteration 2
- **Date/time:** 2026-09-24 23:49-23:54 local
- **Area inspected:** Back / Android back on the home screen (R-03)
- **Issue:** one Back gesture that closed a home dialog or a popup also quit the app.
- **Severity:** P1 (unexpected app exit)
- **Reproduction:** real Escape press and release queued through the Input System (probe `kv/back_probe.cs`), QA profile.
  - (A) Profile dialog open: the dialog closed on the press frame; on the release frame the state was Map, so `LevelManager` reached `Application.Quit()`.
  - (B) Level popup (MenuPlay) open: AnimationManager closed it on the release frame, then LevelManager quit in the same frame.
- **Root cause:** three independent handlers. `RoyalHomeScreen` (execution order 0, on press) and `AnimationManager` (order 0, on release) close things before `LevelManager` (order 1000, on release) runs. LevelManager only checked `gameStatus == Map`, so it always quit.
- **Files changed:**
  - new `Assets/JellyGarden/Scripts/GUI/RoyalBackButton.cs`
  - `RoyalHomeScreen.cs` (1 line)
  - `AnimationManager.cs` (Back branch)
  - `LevelManager.cs` (Back branch only)
- **Fix:** `RoyalBackButton`. Handlers that close something because of Back call `Consume()`. LevelManager records the press frame and quits only if the gesture was not consumed and no popup is open. Quit goes through `RoyalBackButton.Quit()`, which counts requests so tests can observe them.
- **Tests executed:** recompile; fresh Play Mode; the same probe for A, B and C (nothing open); regression D (Back while playing).
- **Result:**
  - A: dialog closed, `QuitRequests` 0.
  - B: popup closed, `QuitRequests` 0.
  - C: nothing open, `QuitRequests` 1 (intended exit).
- **Regression result:** D: Back in level 12 opens MenuPause (Pause, timeScale 0), quits unchanged, and Continue resumes Playing.
- **Console result:** no errors (only the Unity AI package's "Account API" warning).
- **Performance result:** n/a
- **Screenshots/evidence:** `kv/play/back_A_dialog.txt`, `back_B_menuplay.txt`, `back_C_nothing.txt`, `back_D_playing.txt`, driver log `kv/play/ralph-*.txt`
- **Remaining risk:**
  - Android predictive back (`androidPredictiveBackSupport: 1`) still needs a device check.
  - A quit confirmation dialog is not implemented (Back with nothing open exits immediately, as before).
- **Next task:** R-04 (HUD pause gear usable in any state; HUD above in-level popups).

### Iteration 3
- **Date/time:** 2026-09-24 23:57 - 2026-09-25 00:01 local
- **Area inspected:** in-level HUD pause gear and HUD input over popups (R-04)
- **Issue:**
  - (A) The HUD gear opened the pause menu in any state, including the win celebration; its HOME button then forfeits a completed win.
  - (B) The HUD (overlay canvas, sort 20) kept taking taps while a popup (camera canvas, sort 2) was open during play.
- **Severity:** P1 (can forfeit progress; input through modals)
- **Reproduction:** QA profile, level 12.
  - (A) Complete targets, start `PreWinAnimations` as the debug Win key does, press the gear: MenuPause opened over the celebration.
  - (B) Open the gem shop while playing: HUD `blocksRaycasts=True`.
- **Root cause:** the Royal HUD replaced the legacy pause button but dropped its `gameStatus == Playing` check. `LateUpdate` only hid the HUD for Pause, GameOver, Win and PreFailed, never for an open popup.
- **Files changed:** `Assets/JellyGarden/Scripts/GUI/RoyalGameplayPresentation.cs` (gear listener; one line in LateUpdate)
- **Fix:** the gear opens pause only while Playing with no popup open. The HUD stops blocking raycasts whenever `MenuManager.BlocksBoardInput` is true.
- **Tests executed:** recompile; fresh Play Mode; A and B repeated; regression r04r (gear during normal play, Continue).
- **Result:** (A) the gear does nothing during the celebration and the win reaches MenuComplete. (B) the HUD ignores taps with the gem shop open and takes them again after it closes.
- **Regression result:** during play the gear opens pause (Pause, timeScale 0, HUD hidden); Continue resumes Playing with HUD taps restored.
- **Console result:** no errors
- **Performance result:** one extra property read per frame (`BlocksBoardInput` loops over about 12 popup children)
- **Screenshots/evidence:** driver log `kv/play/ralph-235925.txt`
- **Remaining risk:** the HUD is still drawn above popups visually (by design for the goal banners); only its input is gated.
- **Next task:** R-05 (win progress not flushed to disk).

### Iteration 4
- **Date/time:** 2026-09-25 00:02-00:07 local
- **Area inspected:** persistence of a win (R-05)
- **Issue:** the Win branch writes stars and best score after its only `Save()` (`AddLife` refund), then nothing flushes. A kill or crash on the win card can lose the win and the next level's unlock.
- **Severity:** P1 (data loss window)
- **Reproduction:** not reproducible in the Windows editor. Evidence: a real celebration win on QA level 37 put the new star value in the registry at once with no `Save()`, because editor PlayerPrefs write straight through to the registry. The window exists where PlayerPrefs commit only on Save, pause or quit (the Android target). Code evidence: `LevelManager` Win branch writes after `AddLife(1)`.
- **Root cause:** missing flush after the progress writes.
- **Files changed:** `Assets/JellyGarden/Scripts/LevelManager.cs` (3 lines, byte-level edit that keeps the file's line endings)
- **Fix:** `PlayerPrefs.Save()` right after the stars and best-score writes in the Win branch.
- **Tests executed:** recompile; fresh Play Mode; real celebration win on QA level 38.
- **Result:** stars 0 -> 3, level 39 unlocked, value in the registry.
- **Regression result:** the win card shows as before; the QA session restores the real profile.
- **Console result:** no errors (Unity AI "Account API" warning only).
- **Performance result:** one extra PlayerPrefs flush per win.
- **Screenshots/evidence:** `kv/play/ralph-000239.txt` (pre-fix registry read), `ralph-000506.txt`
- **Remaining risk:** **device verification pending** (Android kill on the win card). Not marked COMPLETE.
- **Next task:** R-06 (locale-dependent DateTime.Parse at startup).

### Iteration 5
- **Date/time:** 2026-09-25 00:07-00:12 local
- **Area inspected:** saved dates read at startup and by rewarded-button popups (R-06)
- **Issue:** `DateTimeManager` stored `DateTime.Now.ToString()` in the device culture and parsed it with `DateTime.Parse` in the current culture. After a language or region change the stored string no longer parses, and `InitScript.Awake` (which calls `GetLastDateTime` for every rewarded type) throws on every launch, skipping the life-schedule load and the rest of Awake. The rewarded popups (PreFailed, LiveShop, GemsShop) throw the same way. The ad-quota code runs even in the offline build.
- **Severity:** P1 (permanent broken startup after a locale change)
- **Reproduction:** probe `kv/date_probe.cs` (QA keys only). Current culture en-US. A date stored in en-GB form, `24/09/2026 23:05:00`, makes `GetLastDateTime` and `IsPeriodPassed` throw `FormatException`.
- **Root cause:** culture-dependent write and parse, with no failure handling.
- **Files changed:** `Assets/JellyGarden/Scripts/System/DateTimeManager.cs`
- **Fix:**
  - `SetDateTimeNow` writes the invariant round-trip format (`"o"`).
  - A shared `TryRead` parses the invariant form first, then the legacy current-culture form.
  - An unreadable value counts as "no date" (period passed / now) instead of throwing.
  - The period semantics are otherwise unchanged.
- **Tests executed:** recompile; fresh Play Mode; same probe; legacy-format read; LiveShop opened with en-GB dates stored in all three rewarded keys.
- **Result:**
  - The mismatched date gives no exception (`GetLastDateTime` returns now; `IsPeriodPassed` returns true).
  - New dates are stored as `2026-09-25T00:11:00.71...+06:00` and read back exactly.
  - A legacy `9/24/2026 11:05:00 PM` reads back exactly.
- **Regression result:** LiveShop opens normally; startup of the session was clean.
- **Console result:** 0 entries during the popup test.
- **Performance result:** n/a
- **Screenshots/evidence:** probe output in this entry
- **Remaining risk:**
  - The period checks still use the `Minutes`/`Hours` components rather than the totals (ad frequency only; ads are disabled offline).
  - The ad-quota counter still writes and saves on popup opens offline (P3).
- **Next task:** R-07 (booster pack purchase overwrites stock).

### Iteration 6
- **Date/time:** 2026-09-25 00:12-00:17 local
- **Area inspected:** pre-level booster purchase (R-07), offline gem shop (R-08)
- **R-07 outcome:** not reachable, downgraded to P3 (latent).
  - Every booster button in `game.unity` calls `BoostIcon.ActivateBoost`, which opens the booster shop only when the stock label reads 0. Setting the stock to the pack size is then identical to adding.
  - The other `InitScript.BuyBoost` callers (in-level tool bar, QA "Set booster stock") rely on its set semantics.
  - No change made.
- **Issue (R-08):** players short of coins (lives refill, fail-screen continue, booster shop) are sent to the gem shop, which in the offline build shows $0.99-$14.99 packs whose Buy silently does nothing.
- **Severity:** P1 (dead end with misleading UI, no feedback)
- **Reproduction:** QA profile; gem shop opened; Buy on pack 1: shop stays open, no sound, no message, coins unchanged (`kv/play/r08_gemshop.png`, pre-fix run).
- **Root cause:** `AnimationManager.Buy` returns early when store services are off, but the shop still presents live-looking prices and enabled buttons.
- **Files changed:** `Assets/JellyGarden/Scripts/GUI/AnimationManager.cs` (GemsShop OnEnable)
- **Fix:** with store services off, each pack's button is non-interactable and reads "Unavailable", and the subtitle says "Gem packs are not available in this version". With services on, the shop is unchanged. No prices or economy values changed.
- **Tests executed:** recompile; fresh Play Mode; direct open; real path (0 lives, 3 coins -> PLAY -> lives shop -> REFILL -> gem shop -> x).
- **Result:** all four packs show "Unavailable" and are disabled, the subtitle explains, and coins are unchanged. x returns to the lives shop.
- **Regression result:** MenuPlay -> LiveShop -> GemsShop stacking and closing is unchanged. The QA session restored the real profile.
- **Console result:** no errors
- **Performance result:** n/a
- **Screenshots/evidence:** `kv/play/r08b_gemshop.png`
- **Remaining risk:**
  - The disabled buttons keep their teal face; stronger disabled styling is P3.
  - The offline economy still has no coin income (R-20, product decision).
- **Next task:** R-09 (level-1 tutorial re-entry), then R-02 (booster loop reproduction).

### Iteration 7
- **Date/time:** 2026-09-25 00:18-00:24 local
- **Area inspected:** level-1 hand tutorial when the level is left and re-entered (R-09)
- **Issue:** leaving level 1 (pause -> HOME) before the first board touch, then entering level 1 again, threw `MissingReferenceException` (destroyed `Item`). The hand stayed frozen at its old position.
- **Severity:** P1 (exception in a protected level's integration)
- **Reproduction:** real path, QA profile. Home `OpenLevel(1)` -> PLAY -> hand shown -> gear -> HOME -> `OpenLevel(1)` -> PLAY. Stack: `LevelManager.EnableMap(false)` -> `Level.SetActive(true)` -> `HandTutorial.OnEnable` -> `TutorialManager.GetItemsPositions` (reads destroyed pieces).
- **Root cause:** the tutorial overlay only hides on the first touch. Leaving the level deactivated it with `items` still pointing at the old board, and re-activation re-ran the hand with them.
- **Files changed:** `Assets/JellyGarden/Scripts/GUI/Tutorial/TutorialManager.cs` (OnDisable: 3 lines)
- **Fix:** when the level is left (`Level` deactivated -> `TutorialManager.OnDisable`), hide the overlay and clear the stale pieces (the same retirement the QA harness does). `showed` is untouched, so a player who never dismissed it sees it again on the new board. Level design, rules and tutorial content are unchanged.
- **Tests executed:** recompile; fresh Play Mode; the same real path; regression r09r (first touch dismisses, re-entry shows no tutorial).
- **Result:** no exception. On re-entry the tutorial is shown fresh on the new board, with the hand animating over the new hint (`kv/play/r09_reentered.png`).
- **Regression result:** the first touch hides the tutorial (`showed=True`), and re-entering level 1 afterwards shows none. Level 1 plays normally.
- **Console result:** no errors
- **Performance result:** n/a
- **Screenshots/evidence:** `kv/play/r09_tutorial.png`, `r09_reentered.png`, `r09r_reentered.png`
- **Remaining risk:** `showed` is session-only (the tutorial replays in a new session). That is existing behaviour, left unchanged.
- **Next task:** R-02 (reproduce the `GetRandomItems` loop with pre-level boosters).

### Iteration 8
- **Date/time:** 2026-09-25 00:24-00:28 local
- **Area inspected:** `LevelManager.GetRandomItems` (pre-level boosters at level start; colour ball destroyed during the win finish) (R-02)
- **Issue:** the selection loop caps `count` by all pieces but draws only from plain ones.
  - With fewer plain pieces than requested it never finishes.
  - With none, `list[Random.Range(0,0)]` throws and the catch sets `Win` on every pass.
  - Either way the game freezes on the main thread.
- **Severity:** P0 (hang). Reachability is narrow: a crowded board, or a colour ball destroyed in the win finish with no plain piece left.
- **Reproduction:** not run in the live editor, because an infinite main-thread loop would hang the shared Unity session. The loop, copied verbatim except for Unity types, ran in an isolated .NET program (scratchpad `r02/Program.cs`) with an iteration guard:
  - "win finish, 0 plain": no termination in 5,000,000 iterations, 5,000,000 `Win` assignments;
  - "5 rockets, 3 plain": no termination;
  - normal case: fine.
- **Root cause:** a missing cap to the plain-piece count, plus a catch that re-enters Win.
- **Files changed:** `Assets/JellyGarden/Scripts/LevelManager.cs` (+4 lines, byte-level edit keeping line endings)
- **Fix:** `if (list.Count < count) count = list.Count;` before the loop. Normal cases are identical, including the random picks; degenerate cases return what exists.
- **Tests executed:** the same .NET harness against the fixed logic (all cases terminate); recompile; fresh Play Mode regression.
- **Result:** harness: fixed version terminates in every case (0 and 3 picks respectively).
- **Regression result:**
  - Level 12 with all three pre-level boosters: the board starts with 5 TNT, 5 rockets and 1 colour ball.
  - A colour ball spawned and the win finish run: Win reached normally.
- **Console result:** no errors
- **Performance result:** n/a
- **Screenshots/evidence:** `kv/play/r02_boosters.png`; harness output in this entry
- **Remaining risk:** the dead catch -> `Win` branch is kept (now unreachable) to keep the diff minimal.
- **Next task:** P2 batch. R-11 (timed levels: HUD label and hammer slot), R-10 (Back in pause and result cards), R-13 (auto-pause on app background), R-14 (quit-level confirmation), R-12 (PreFailed tap delay).

### Iteration 9
- **Date/time:** 2026-09-25 00:29-00:33 local
- **Area inspected:** in-level HUD on timed levels (R-11); levels 20, 27 and 32 are within the protected 1-50
- **Issue:** on timed levels the HUD header read "Moves" while showing the seconds left ("Moves 54"), and the first rail slot (the Royal Hammer) was empty.
- **Severity:** P2 (wrong information; a tool missing on 11 levels)
- **Reproduction:** QA profile, level 20 (`limitType=TIME`): heading 'Moves', value '56'; `BoostExtraMoves` (the rebound Hammer) hidden (`kv/play/r11_hud_level20.png`, pre-fix run). Level 12 (moves) correct.
- **Root cause:**
  - The Royal HUD formats `Limit` without checking `limitType`.
  - `LevelManager.PrepareGame` still hides the legacy extra-moves booster on timed levels. The Royal HUD rebound that object as the Hammer tool.
- **Files changed:** `Assets/JellyGarden/Scripts/GUI/RoyalGameplayPresentation.cs` (HUD only; `LevelManager` untouched)
- **Fix:**
  - On timed levels the header reads "Time" and the value shows m:ss; on moves levels "Moves" as before.
  - The HUD keeps its four rebound rail tools active in its 10 Hz refresh.
  - No gameplay rule or level data changed.
- **Tests executed:** recompile; fresh Play Mode; level 20 (timed) and level 12 (moves).
- **Result:** level 20: "Time 0:56", all four tools and the gear visible. Level 12: "Moves 18", unchanged.
- **Regression result:** moves levels unchanged; the legacy extra-time booster stays on the hidden legacy panel as before.
- **Console result:** no errors
- **Performance result:** a 4-element loop at 10 Hz
- **Screenshots/evidence:** `kv/play/r11_hud_level20.png` (post-fix), `r11_hud_level12.png`
- **Remaining risk:** the PrePlay caption still says "Make every move count" on timed levels (P3 copy).
- **Next task:** R-10 (Back in pause, result cards and tool selection).

### Iteration 10
- **Date/time:** 2026-09-25 00:33-00:42 local
- **Area inspected:** Back / Android back inside a level (R-10)
- **Issue:**
  - (a) Back on the pause menu did nothing.
  - (b) Back with a tool selected, or (c) with the tool shop open, opened the pause menu. The tool or shop was only cancelled as a side effect.
- **Severity:** P2 (navigation friction)
- **Reproduction:** real Escape press and release (probe), QA level 12.
  - (a) Pause -> Back: still Pause, timeScale 0.
  - (b) Hammer chosen -> Back: MenuPause opened.
  - (c) Arrow shop open -> Back: MenuPause opened.
- **Root cause:**
  - MenuPause was not among AnimationManager's Back-closable popups.
  - ReferenceBoosterBar never read Cancel.
  - LevelManager's Playing branch opened pause regardless of what the gesture had already done.
- **Files changed:**
  - `AnimationManager.cs`: MenuPause Back -> `Play()` (the Continue handler) + consume
  - `ReferenceBoosterBar.cs`: Back closes its shop or cancels the selection + consume
  - `LevelManager.cs`: Playing branch requires `!RoyalBackButton.Consumed` (1 line, byte-level edit)
- **Fix:** Back resolves the innermost thing first: tool shop, then tool selection, then pause resume. It opens pause only if nothing consumed the gesture.
- **Tests executed:** recompile; fresh Play Mode; a, b and c; regression D (Back while playing opens pause); home suite A, B and C.
- **Result:**
  - (a) Back resumes Playing (timeScale 1).
  - (b) the selection is cancelled and no pause opens.
  - (c) the shop is closed and no pause opens.
- **Regression result:**
  - D: pause opens, Continue resumes.
  - Home: the dialog and popup close with 0 quits; nothing open -> 1 quit.
- **Console result:** no errors
- **Performance result:** n/a
- **Screenshots/evidence:** `kv/play/back_E_pause.txt`, `back_F_tool.txt`, `back_G_shop.txt`, `ralph-003820.txt`, `ralph-004047.txt`
- **Remaining risk:** Back still does nothing on PrePlay, the win card, the fail card and the PreFailed offer. That is deliberate for now, because those need a product decision (e.g. Back = CONTINUE / TRY AGAIN).
- **Next task:** R-13 (auto-pause when the app goes to background during a level).

### Iteration 11
- **Date/time:** 2026-09-25 00:42-00:46 local
- **Area inspected:** app pause/resume during a level (R-13)
- **Issue:** a level interrupted by the OS (call, home button) resumed straight into live play, with no pause menu. On the 11 timed levels the clock resumes immediately.
- **Severity:** P2
- **Reproduction:** QA level 12. Unity's `OnApplicationPause(true)` then `(false)` delivered to every root object, which is what the engine does when the app is backgrounded. Result: still Playing, no popup.
- **Root cause:** only `InitScript.OnApplicationPause` existed, and it only saves lives.
- **Files changed:** `Assets/JellyGarden/Scripts/GUI/RoyalGameplayPresentation.cs`
- **Fix:**
  - `OnApplicationPause(true)` opens the pause menu, only while Playing with no popup open.
  - Focus loss does the same in builds only (the editor loses focus on every Inspector click).
- **Tests executed:** recompile; fresh Play Mode; the same message sequence while playing; edge case on the win card.
- **Result:** while playing: resume lands on the pause menu (Pause, timeScale 0) and Continue resumes. On the win card: unchanged.
- **Regression result:** the pause menu from the gear and from Back is unaffected (both covered in iterations 3 and 10).
- **Console result:** no errors
- **Performance result:** n/a
- **Screenshots/evidence:** driver log `kv/play/ralph-004408.txt`
- **Remaining risk:** real OS backgrounding and focus loss need a device check (Android notification shade, calls).
- **Next task:** R-12 (PreFailed buttons ignore taps for about 2 s).

### Iteration 12
- **Date/time:** 2026-09-25 00:46-00:54 local
- **Area inspected:** PreFailed ("so close") continue / end-level offer (R-12)
- **Issue:** the offer's buttons are in place about 0.4 s after it opens, but choices made before 2.05 s were lost.
  - CONTINUE required the clip to be held (`speed == 0`), which only happens at the `WaitForGiveUp` event (2.05 s).
  - END LEVEL set speed 1, but the clip then held at 2.05 s anyway and waited for a second tap.
- **Severity:** P2 (dropped input on a key decision screen)
- **Reproduction:** exact-timing in-game coroutine (`kv/prefailed_probe.cs`), QA level 12, offer opened as the game does (0 moves -> PreFailed).
  - END LEVEL at 0.6 s: the clip held at 2.05 s and the offer was still open at 6 s.
  - CONTINUE at 0.6 s: ignored; moves 0; held open at 6 s.
  - Late END LEVEL at 2.6 s: fail card at 3.6 s (works).
- **Root cause:** the Royal presenter shortened the entrance, but the input gate and the hold event kept the original clip's 2.05 s timing.
- **Files changed:** `Assets/JellyGarden/Scripts/GUI/AnimationManager.cs`
- **Fix:**
  - A per-showing `failedChoiceMade` flag, reset on open.
  - CONTINUE and END LEVEL are accepted as soon as tapped, once.
  - The hold event does not re-hold after a choice.
  - An early choice jumps the clip just past the hold point, so the exit plays at once.
  - Prices, extra moves and flow are unchanged.
- **Tests executed:** recompile; fresh Play Mode; the same probe for early END LEVEL, early CONTINUE and late END LEVEL; edge r12c (early CONTINUE with 3 coins).
- **Result:**
  - Early END LEVEL: fail card at 1.58 s (1 s after the tap).
  - Early CONTINUE: +5 moves and Playing; offer closed at 1.60 s.
  - Late END LEVEL: fail card at 3.61 s, unchanged.
- **Regression result:** with too few coins, CONTINUE opens the gem shop, the offer holds, and END LEVEL then shows the fail card.
- **Console result:** no errors
- **Performance result:** n/a
- **Screenshots/evidence:** `kv/play/prefailed_giveup.txt`, `prefailed_continue.txt`, `prefailed_giveup_late.txt`, `prefailed_continue_poor.txt`
- **Remaining risk:** taps during the first 0.38 s (buttons still sliding in) are accepted too. That is intended, since the buttons are already visible.
- **Next task:** R-14 (pause HOME abandons the level without confirmation).

### Iteration 13
- **Date/time:** 2026-09-25 00:54-01:01 local
- **Area inspected:** pause menu HOME (R-14)
- **Issue:** HOME left the level at once, and the life spent at PLAY was lost with no warning. This is a genre-standard confirmation that Royal Throne lacked.
- **Severity:** P2 (irreversible loss on one tap)
- **Reproduction:** real path, QA profile. Lives 5, PLAY -> 4, gear -> HOME: straight to the map, lives 4 (`kv/play/r14_pause.png`).
- **Root cause:** `AnimationManager.BackToMap` leaves immediately. An earlier pass deliberately stopped leaving from opening the fail card, and never added a confirm.
- **Files changed:** `RoyalMenuSkin.cs` (builds the confirm card in the pause menu); `AnimationManager.cs` (BackToMap asks first; Back and reopen handling).
- **Fix:**
  - HOME on the pause menu shows a card built with the menu's own style: "LEAVE LEVEL?", "You will lose the life used for this level.", LEAVE and STAY.
  - LEAVE runs the unchanged leave path. STAY and Back return to the pause menu.
  - A reopened pause menu never shows a stale card. The life rule is unchanged.
- **Tests executed:** recompile; fresh Play Mode; real path HOME -> card, STAY, HOME -> Back, HOME -> LEAVE, reopen pause, CONTINUE.
- **Result:**
  - The card shows while paused (screenshot checked: legible and consistent with the menu).
  - STAY and Back hide it and stay paused.
  - LEAVE goes to the map (lives 4, as the rule says).
  - Reopened pause: no card. CONTINUE resumes.
- **Regression result:** pause menu Continue, Music, SFX and Hints are unchanged. Back on the pause menu without the card still resumes (iteration 10).
- **Console result:** no errors
- **Performance result:** n/a (one small UI card built once)
- **Screenshots/evidence:** `kv/play/r14b_confirm.png`, `back_H_confirm.txt`
- **Remaining risk:** the legacy Settings popup (unreachable) shares the skin path, but the card is built only for MenuPause.
- **Next task:** P2/P3 review: R-17 small defects (Sound/Music default mismatch, `print(DateOfExit)` on every launch, plurals, timer formats), R-18 compile warnings in non-protected runtime code, then verification mode.

### Iteration 14
- **Date/time:** 2026-09-25 01:03-01:07 local
- **Area inspected:** home panel copy (R-17, plurals)
- **Issue:** the profile, coins and stars panels built counts by concatenation, so every new player saw "1 levels completed", "1 stars earned", "1 coins" and "1 stars".
- **Severity:** P3 (visible copy error early in every playthrough)
- **Reproduction:** QA profile with totals of 1 (level 1 = 1 star, all other levels 0, coins 1). Panels read exactly the strings above.
- **Root cause:** `n + " stars"` style concatenation.
- **Files changed:** `Assets/JellyGarden/Scripts/GUI/RoyalHomeScreen.cs` (a `Count` helper, used for the three panels)
- **Fix:** singular or plural by count, keeping the existing `N0` number formatting.
- **Tests executed:** recompile; fresh Play Mode; totals of 1, totals of 2, 0 coins.
- **Result:** "1 level completed / 1 star earned", "1 coin", "1 star"; "2 levels ... / 2 stars", "2 coins"; "0 coins".
- **Regression result:** panel layout and other copy unchanged. The QA session restored the real profile.
- **Console result:** no errors
- **Performance result:** n/a
- **Screenshots/evidence:** driver log `kv/play/ralph-*.txt`
- **Remaining risk:** the profile name is "King Vanguard" (`RoyalOfflineMode.KingName`) while the character assets are spelled "Venguard". This is a naming decision for the user, not changed.
- **Next task:** verification mode. Lives and refill timer, settings persistence across a restart, then missing-reference and duplicate-manager scans, resolution check, performance sample.
