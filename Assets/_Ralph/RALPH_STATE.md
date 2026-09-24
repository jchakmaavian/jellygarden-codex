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
