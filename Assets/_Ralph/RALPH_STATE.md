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
| R-21 | P2 | Home profile avatar and area chest ignore real taps: their only hit area is disabled (found in verification, iteration 18) | ReferenceHomeLayout.Hide :18 |
| R-22 | P3 | 4:3 tablets: the raised Home tab icon draws over the Settings dialog's bottom button (layering is intentional; needs a size or layering decision) | RoyalHomeScreen / ReferenceHomeLayout (protected) |
| R-23 | P3 | Out-of-moves CONTINUE shows a gem icon while home calls the balance coins | PreFailed prefab art; ties to R-20 |
| R-24 | P2 | Per-frame garbage from `Counter_` (`Object.name` reads) and `BoostIcon` (strings) in normal play; incremental GC is off (fixed, iteration 20; the GC setting is a decision) | Counter_.cs, BoostIcon.cs; ProjectSettings `gcIncremental` |

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

### Iteration 15 (verification V-01)
- **Date/time:** 2026-09-25 01:09-01:10 local
- **Area inspected:** lives refill timer and home lives display
- **Issue:** none found
- **Severity:** n/a
- **Reproduction:** QA profile, 2 lives. `InitScript.nextLifeUtc` was set directly to simulate three cases:
  - (a) the deadline passed 11 min ago (a long offline gap);
  - (b) the deadline is 3 min ahead (a normal wait);
  - (c) the deadline is 60 min ahead (the device clock was moved back).
- **Root cause:** n/a
- **Files changed:** none
- **Fix:** none
- **Tests executed:** `UpdateLives()` and the home HUD for each case.
- **Result:**
  - (a) lives 5/5, no deadline, HUD "5 / Full". The gap refilled and capped at the maximum.
  - (b) lives 2/5, 3.0 min left, HUD "2:58".
  - (c) lives 2/5. The countdown restarted at 5.0 min (HUD "4:58") instead of waiting 60 min.
- **Regression result:** n/a (read-only)
- **Console result:** no entries
- **Performance result:** n/a
- **Screenshots/evidence:** driver log `kv/play/ralph-010953.txt`
- **Remaining risk:** the refill uses UTC, so time-zone changes cannot move it. A device clock moved forward still refills early, as in every offline game without a server clock.
- **Next task:** settings persistence across a restart.

### Iteration 16 (verification V-02)
- **Date/time:** 2026-09-25 01:11-01:12 local
- **Area inspected:** settings persistence (home Settings dialog: Music, Sound, Motion, Hints)
- **Issue:** none found
- **Severity:** n/a
- **Reproduction:** QA profile. Pressed all four toggles, then stopped and restarted Play Mode (a full app restart from the title screen).
- **Root cause:** n/a
- **Files changed:** none
- **Fix:** none
- **Tests executed:** toggle -> read back; restart -> read back; toggle back -> read back.
- **Result:** all four went 1 -> 0, stayed 0 after the restart, and read 1 after toggling back. `ToggleSetting` calls `Save()` on each change.
- **Regression result:** the settings were restored to On in the QA namespace. The real profile was never written.
- **Console result:** no entries
- **Performance result:** n/a
- **Screenshots/evidence:** driver logs `kv/play/ralph-011106.txt`, `ralph-011146.txt`
- **Remaining risk:** Windows editor PlayerPrefs live in the registry. A device check of the Android PlayerPrefs file is still open (with R-05).
- **Next task:** missing-script, broken-reference and duplicate-manager scans.

### Iteration 17 (verification V-03)
- **Date/time:** 2026-09-25 01:13-01:22 local
- **Area inspected:** missing scripts, broken serialized references and duplicate singletons/managers on home, in a level, and back on home
- **Issue:** no defect. Three legacy Missing asset references exist, none visible (details below).
- **Severity:** n/a (P3 at most, not player-visible)
- **Reproduction:**
  - Runtime scan: every component on every loaded object (inactive included) through `SerializedObject`.
  - Offline scan: every `{fileID, guid}` in the two build scenes resolved against the 28,194 `.meta` GUIDs in Assets, Packages and PackageCache.
- **Root cause:**
  - The three Missing references are original JellyGarden assets that are not in the project (`game.unity`):
    - the `Offline/Slot/Image` and `Offline/Slot/Button` sprites in MenuPlay. This is the old offline leaderboard, which is inactive, and `RoyalMenuSkin.HideVisual` sets it to alpha 0;
    - the material of `CanvasTutorial/Label`. It is inactive until the tutorial runs, and UI then falls back to the default material, which is how it rendered in iteration 7.
  - After leaving a level, `LevelManager.squaresArray` holds 81 references to the destroyed board squares. That is runtime state, rebuilt at the next level (`InitLevel` :585), and nothing reads it on the map.
- **Files changed:** none. Clearing the three references would mean editing `game.unity`, which holds the protected board setup, for no visible gain.
- **Fix:** none
- **Tests executed:** the scan on home, in level 12, and on home again after leaving (pause -> HOME -> LEAVE). A detail probe gave the path, visibility and target of each dead reference. The offline scan covered `main.unity` and `game.unity`.
- **Result:**
  - Missing scripts: 0 in all three states.
  - Managers: LevelManager, InitScript, MenuManager, SoundBase, MusicBase, InputManager, RoyalHomeScreen, RoyalGameplayPresentation, ReferenceBoosterBar, EventSystem and AudioListener are exactly 1 each in all three states.
  - `main.unity`: 0 missing references. `game.unity`: the 3 above.
- **Regression result:** n/a (read-only). The leave-confirm path from iteration 13 worked inside the scan run.
- **Console result:** no entries
- **Performance result:** n/a
- **Resources prefabs (loaded at runtime, so not all instantiated during the scan):**
  - All 85 prefabs under `Resources/` were loaded in the editor and checked with `GetMonoBehavioursWithMissingScriptCount` plus the same reference check: 0 missing scripts, 0 dead references.
  - The offline scanner flagged `Particles/PopupScores.prefab`. It is a Unity 5-format prefab that references the pre-package `UnityEngine.UI.dll` GUID for `Text`/`Shadow`, which Unity remaps on load, so this was a false positive.
- **Screenshots/evidence:**
  - driver logs `kv/play/ralph-011606.txt`, `ralph-011733.txt`;
  - scanners `kv/deadref_probe.cs`, `kv/scene_missing.py`, `kv/prefab_check.py`.
- **Scene-dependency prefabs:** the 7 prefabs outside `Resources/` that the build scenes depend on (from `AssetDatabase.GetDependencies`, 377 dependencies) got the same editor check: 0 missing scripts, 0 dead references.
- **Remaining risk:** none found for references. Assets loaded by path through Addressables or AssetBundles would not be covered, but the project uses neither for gameplay.
- **Next task:** resolution/aspect check, then a performance sample.

### Iteration 18 (verification V-04, fix)
- **Date/time:** 2026-09-25 01:30-01:49 local
- **Area inspected:** tap targets and layout at 1080x1920. Home, home Settings, pre-level card, level HUD, pause, leave confirm, win card, out-of-moves offer and fail card.
- **Issue:** on home, the profile avatar and the area chest button did nothing when tapped. Calling `onClick` directly works, which is why earlier checks (and iteration 3) passed; a finger tap did not.
- **Severity:** P2 (two visible home buttons dead to touch; the profile and area panels unreachable)
- **Reproduction:** real taps queued through the Input System (Mouse state, in the player loop) at each button's centre:
  - Coins opens the coins panel and Level 13 opens the pre-level card (controls);
  - the avatar and the chest do nothing;
  - `EventSystem.RaycastAll` at those centres returns `Castle framing/Royal blue courtyard`, the home background.
- **Root cause:**
  - `ReferenceHomeLayout.Hide` disables every non-Text graphic of a button it restyles, then re-enables only an `Image` on the button as a clear hit area.
  - The profile and area buttons draw with a `RoyalPanelGraphic` rather than an `Image`, so they were left with no enabled graphic. `Graphic.Raycast` returns false while `isActiveAndEnabled` is false.
  - The other restyled buttons (coins, lives, stars, settings, level, the five tabs) have `Image` hit areas and were unaffected.
- **Files changed:** `Assets/ReferenceGameplay/Home/Scripts/ReferenceHomeLayout.cs` (the protected home layout: smallest fix only)
- **Fix:** a restyled selectable without an `Image` keeps its own graphic enabled with a clear colour, the same treatment the `Image` hit areas get. Layout, artwork and callbacks are unchanged.
- **Tests executed:**
  - recompile (no errors); fresh Play Mode;
  - real taps on the avatar, chest, coins and level;
  - the layout probe on all nine screens (every visible control must be on screen and take a tap at its centre; text is checked for clipping and for spilling off screen);
  - before/after home capture comparison.
- **Result:**
  - The avatar opens the "King Vanguard" profile panel and the chest opens the "Area 3" panel.
  - Home: all 12 visible controls take a tap at their centre.
  - HUD: 5/5 (four boosters and pause). Settings 7/7, pre-level 6/6, pause 6/6, leave confirm 2/2, win 1/1, out-of-moves offer 2/2, fail card 1/1.
  - No clipped or off-screen text.
- **Regression result:**
  - Coins and Level taps are unchanged.
  - The home action-button strip is pixel-identical before and after. The resource bar differs only by the animated king and the lives timer, with no panel showing behind the avatar.
- **Console result:** no errors or warnings (info logs only)
- **Performance result:** n/a (two clear graphics)
- **Screenshots/evidence:**
  - `kv/play/tap_*.txt`;
  - `kv/play/res/1080x1920_*.png`, `res/_fix_compare.png` (pre-fix capture `res/prefix_1080x1920_1home.png`);
  - probes `kv/layout_probe_loop.cs`, `kv/tap_loop.cs`, `kv/raywalk_loop.cs`.
- **Method notes:**
  - Probes that read `Screen.*` or use `GraphicRaycaster` must run inside the player loop. From the eval callback, `Screen.width/height` can report the editor window (846x1227).
  - A forced `GameState.PreFailed` with moves left never reaches the fail card. The real trigger is `Limit=0` plus `CheckWinLose()`, the only place the game sets PreFailed.
- **Remaining risk:** none known for the home. Other screens' buttons are covered by the probe above.
- **Next task:** the same layout pass at 20:9 (1080x2400), 4:3 (1536x2048) and 720x1280.

### Iteration 19 (verification V-05: other resolutions)
- **Date/time:** 2026-09-25 01:49-01:58 local
- **Area inspected:** the iteration-18 layout pass at 1080x2400 (20:9, a temporary Game view preset), 1536x2048 (4:3, the "QA tablet" preset) and 720x1280 (the "Portrait 720x1280" preset). The game is portrait-only (`defaultScreenOrientation: 0`, no landscape autorotation).
- **Issue:**
  - Probe: none.
  - Visual review of the side-by-side sheets found three things:
    - At 4:3 only, the raised Home tab icon (the castle) draws over the bottom of the home Settings dialog, across "Back to home". The button still takes taps, because the icon is not a raycast target.
    - The out-of-moves CONTINUE button shows a blue gem, while home calls the same balance coins and shows a coin.
    - The QA Mod Menu button sits below the pause card.
- **Severity:**
  - The tablet overlap is P3 (visual only, 4:3).
  - The currency icon is P3 (consistency).
  - The Mod Menu is n/a (QA tool, compiled only into editor and development builds, `#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !ROYAL_DISABLE_QA`).
- **Reproduction:** `kv/play/res/<size>_<screen>.png` and the sheets `res/_sheet0-4.png`. The overlap is `res/_overlap_zoom.png`: at 16:9 the dialog clears the tab bar.
- **Root cause (overlap):**
  - `RoyalHomeScreen` deliberately keeps the navigation above the dialog veil ("navigation remains available above it so a tab can be changed directly without a second close tap").
  - `ReferenceHomeLayout` raises the selected tab's art to 1.40x the bar height.
  - On 4:3 the dialog is tall enough to reach it.
- **Files changed:** none. The overlap sits in the protected home layout, and the fix is a layering or size decision. It is recorded for the user as R-22.
- **Tests executed:** the full nine-screen probe at each size (fresh Play Mode per size, QA profile).
- **Result:**
  - 20:9, 4:3 and 720x1280: every visible control on all nine screens is on screen and takes a tap at its centre. No clipped or off-screen text.
  - Smallest target: the pre-level "?" booster info button, 62x62 px at 1080 wide (41 px at 720). That is about half the usual 48 dp guideline (P3, noted).
- **Regression result:** n/a (read-only)
- **Console result:** no errors or warnings in any run
- **Performance result:** n/a
- **Screenshots/evidence:** `kv/play/res/`
- **Remaining risk:**
  - Safe-area insets (notches, cutouts) cannot be simulated in the Game view (`Screen.safeArea` is the full screen). Device check (with R-16).
  - The Game view was restored to the user's "RoyalThrone 1080x1920" preset and the temporary 20:9 preset removed (22 presets, as before).
- **Next task:** performance sample.

### Iteration 20 (verification V-06: performance, fix)
- **Date/time:** 2026-09-25 02:00-02:12 local
- **Area inspected:** performance, home idle and level 12 idle, in editor Play Mode.
  - Frame time and render counters come from `ProfilerRecorder`.
  - Per-script time and GC come from the editor profiler's own frames, counted under `PlayerLoop` only, so editor work is excluded.
  - Allocations per call come from the exact in-frame counter (`GC Allocated In Frame`). `GC.GetAllocatedBytesForCurrentThread` always reads 0 under Unity Mono.
- **Issue:** two legacy UI scripts made garbage every frame in normal play:
  - `Counter_` read `Object.name` about 12 times per counter per frame (44 B each): 2.8 KB/frame on home, 3.2 KB/frame in a level.
  - `BoostIcon` rebuilt its pref key and label strings every frame (256 B/frame).
  - Incremental GC is off (`gcIncremental: 0`), so this garbage is collected in stop-the-world pauses.
- **Severity:** P2 (steady garbage leads to periodic GC hitches on devices). The fail card's "Best score:3800" missing space is P3, fixed in the same line.
- **Reproduction:** `kv/play/perf_home.txt`, `perf_level.txt` (the profiler run was at 720x1280); `kv/alloc_bench.cs`, `kv/ui_alloc_bench.cs`.
- **Measurements (editor, indicative only; not device numbers):**
  - Home idle: main thread avg 16.7 ms (p95 23.3), PlayerLoop 5.9 ms, 28 SetPass calls, about 1.1k triangles.
  - Level idle: avg 18.9 ms (p50 16.7, p95 21.8, one 556 ms outlier), PlayerLoop 6.0 ms, 36 SetPass calls, about 1.2k triangles.
  - Top script time: `RoyalModMenu.Update` 1.1-1.3 ms (QA tool), `RoyalAudioDirector.Update` about 0.47 ms, `RoyalPieceSkin.LateUpdate` 0.46 ms (70 pieces), `RoyalHomeScreen.LateUpdate` 0.29-0.44 ms, `BoostIcon` x5 0.30 ms, `MapLevel.Update` x100 0.14 ms (the hidden legacy map).
  - Game-code GC: 9.6 KB/frame on home, 57.5 KB/frame in a level. Most of it is not real:
    - `RoyalPieceSkin` 45.7 KB/frame is `GetComponent<RoyalCampaignItem>()` missing on ordinary pieces. That is Unity's editor-only "fake null" allocation (654 B per miss; `TryGetComponent` 0; nothing in builds).
    - During QA sessions `RoyalSave` builds read-key strings (QA only).
- **Root cause:** string building in per-frame `Update`s (`Object.name` getter, `"" + enum`, `"" + int`).
- **Files changed:** `Assets/JellyGarden/Scripts/GUI/Counter_.cs`, `Assets/JellyGarden/Scripts/GUI/BoostIcon.cs`
- **Fix:**
  - `Counter_` reads its name once. The fail card now reads "Best score: 3800".
  - `BoostIcon` caches its pref key and rewrites the count label only when the count changes, or when other code has replaced the label.
  - What each label shows is unchanged.
- **Tests executed:** recompile; fresh Play Mode; per-call allocation after the fix; booster labels vs stock; label overwrite recovery; level counters vs Limit and Score; fail card.
- **Result:**
  - `Counter_` 2,798 -> 48 B/frame (home) and 3,200 -> 76 B/frame (level). The rest is the `"" + int` label text.
  - `BoostIcon` 257 -> 1 B/frame.
- **Regression result:**
  - Booster labels follow the stock (0 -> 4 after "Set booster stock"). A label overwritten with "99" goes back to the stock value the next frame.
  - Counters follow Limit (18 -> 7) and Score (0 -> 1234); the level number shows.
  - Fail card: "Best score: 3800".
- **Console result:** no entries
- **Performance result:** about 3.3 KB/frame less garbage in levels in release builds, roughly 0.2 MB/s at 60 fps.
- **Screenshots/evidence:** `kv/play/res/after_failed_bestscore.png`, driver logs `kv/play/ralph-0208*.txt`, `ralph-0210*.txt`
- **Remaining risk / not changed (recorded for the report):**
  - Enable incremental GC: a project setting and product decision.
  - `RoyalHomeScreen` and `ReferenceHomeLayout` `LateUpdate`s keep running during levels (protected home code).
  - `RoyalModMenu.AttachEntries` runs `FindObjectsByType` every frame (development builds only).
  - `RoyalAudioDirector` reads the Sound pref once per voice per frame (cheap on devices).
  - The hidden legacy map's 100 `MapLevel.Update` calls per frame.
- **Next task:** texture import audit (report plus safe fixes), then R-18 compile warnings.

### Iteration 21 (verification V-07: texture import audit, report only)
- **Date/time:** 2026-09-25 02:13-02:25 local
- **Area inspected:** import settings of every texture in the Android build: 2,353 textures.
  - 212 are dependencies of `main.unity`/`game.unity`; 2,141 are under `Resources/`. Six could not be read by PIL and were not analysed.
  - Android compression formats are `ETC` (no subtarget override).
- **Issue:**
  - 1,569 build textures are imported uncompressed (RGBA32) and 12 as RGB24.
  - Loaded with the scenes: 211 textures, about 106.5 MB estimated on Android.
  - Of those, 184 are uncompressed (105.7 MB). The 77 that are 256 px or larger hold 99.0 MB.
  - At 8 bpp (ASTC 4x4 / ETC2 RGBA8) they would take about 29 MB, a saving of roughly 72-77 MB of resident memory.
  - The largest are protected title and home art (logo 6 MB, title crown 6 MB, castle art 4.5 MB x2), legacy menu banners, tutorial art and the five level backgrounds. Several `Resources/` textures carry deliberate RGBA32 Android overrides (home sky, clouds, palace, coin).
- **Severity:** P2 (resident memory on low-RAM Android devices; not a crash in the editor)
- **Reproduction:** `kv/texture_audit.py` over `kv/build_textures.txt`. The report is `Assets/_Ralph/TEXTURE_AUDIT.md`.
- **Root cause:** the inherited JellyGarden UI art uses "None" compression, a leftover from ETC1 having no alpha, and some newer art keeps explicit RGBA32 overrides.
- **Files changed:** `Assets/_Ralph/TEXTURE_AUDIT.md` (report only). No import settings were changed.
- **Fix:** none. Every compression change alters pixels, and the largest items are protected art. The report ranks the work: hidden legacy art first (no visual cost), then visible UI with a device check, protected art only with approval, and ASTC as the Android default.
- **Tests executed:** static audit (import settings plus image headers); scene-resident vs on-demand split from `AssetDatabase.GetDependencies`.
- **Result:** as above. Only 23 UI textures have mipmaps (0.8 MB) and none has Read/Write enabled. One source is larger than 2048 (the title crown, 2172x724).
- **Regression result:** n/a
- **Console result:** n/a
- **Performance result:** estimated saving of about 72-77 MB resident on Android if the uncompressed scene art is compressed.
- **Screenshots/evidence:** `Assets/_Ralph/TEXTURE_AUDIT.md`, `kv/texture_audit.md`, `kv/texture_split.txt`
- **Remaining risk:** the estimates assume the effective formats computed from the `.meta` files. Verify with a device memory snapshot (Memory Profiler) before and after.
- **Next task:** R-18 compile warnings.

### Iteration 22 (R-18: obsolete Unity APIs in runtime code, fix)
- **Date/time:** 2026-09-25 02:14-02:24 local
- **Area inspected:** project compile warnings, captured from `CompilationPipeline.assemblyCompilationFinished` during a clean script rebuild. This editor writes no `Editor.log`.
- **Issue:** 69 warnings.
  - Runtime `Assembly-CSharp`: 22. Nine call sites (10 warnings) use APIs that Unity 6.5 has marked obsolete:
    - `FindObjectOfType`, `FindObjectsOfType`;
    - `FindFirstObjectByType`, and the `FindObjectsSortMode` overloads (both newly deprecated in 6.5);
    - `ParticleSystem.startColor`.
  - The rest are unused fields and variables (CS0414/CS0219/CS0168) and two serialization-analyzer notes.
  - `Assembly-CSharp-Editor` has 33 and Spine 14 (third-party).
- **Severity:** P3 (no player effect today). Unity has already turned one sibling API (`SerializedProperty.objectReferenceInstanceIDValue`) into an error in 6.5.
- **Reproduction:** `kv/cs_warnings.tsv` (clean rebuild)
- **Root cause:** code written against pre-6.5 APIs
- **Files changed:** `InitScript.cs`, `MapScripts/LevelsMap.cs`, `Juice/MatchVFXService.cs`, `Juice/UrpBloomSetup.cs`, `Juice/RoyalReviewAudioCapture.cs`, `GUI/BoostAnimation.cs` (one hunk each; other sessions' uncommitted edits in `InitScript.cs` are not included)
- **Fix:** each call moved to the current form with identical behaviour:
  - single objects (`LevelsMap`, `MapCamera`, the one `AudioListener`) use `FindAnyObjectByType`;
  - `GetMapLevels` re-sorts by level number, so the unsorted `FindObjectsByType` gives the same result;
  - the two `FindObjectsSortMode.None` calls use the unsorted overload;
  - `startColor` goes through `ParticleSystem.main`. `BoostAnimation` is not attached anywhere and nothing calls `ShowEffect`, so this path is dead code.
  - Unused variables in protected files (`LevelManager`, `Square`) were left alone.
- **Tests executed:**
  - recompile with warning capture;
  - fresh Play Mode: startup to home (the `InitScript` camera setup, the bloom camera scan);
  - level 12 win -> CONTINUE (`GetMapLevels`);
  - a TNT spawned at (4,4) and detonated (match VFX square lookup).
- **Result:**
  - Runtime warnings went from 22 to 12, with no obsolete-API warning left in runtime code.
  - CONTINUE opened level 13's card. The TNT detonated: moves 18 -> 17, score 310.
- **Regression result:** as above. The QA session restored the real profile (`OpenLevel=14`).
- **Console result:** info logs only
- **Performance result:** n/a
- **Screenshots/evidence:** `kv/cs_warnings.tsv`, `kv/cs_warnings_after.tsv`, driver log `kv/play/ralph-022225.txt`
- **Remaining risk:** the editor-only warnings (33) and Spine's (14) are unchanged. The editor tools compile and work, so these are out of scope for a player-facing pass.
- **Next task:** R-17 leftovers (Sound/Music default mismatch, `print(DateOfExit)` each launch, timer formats), then the final report.

### Iteration 23 (R-17 leftovers)
- **Date/time:** 2026-09-25 02:24-02:28 local
- **Area inspected:** the remaining R-17 items
- **Issue:**
  - Every launch logged two leftover debug lines, the last exit time (`print(DateOfExit)`) and "Leadboard disable" twice. They go to logcat in release builds too.
  - Sound/Music defaults: five reads use default 0 while the rest use 1.
  - Timer formats: home and the timed-level HUD show `m:ss`, while `LIFESAddCounter` and the legacy `Counter_` show `mm:ss`.
- **Severity:** P3
- **Reproduction:** console after a fresh Play start. Code reads (`grep`).
- **Root cause:**
  - The prints are debug leftovers.
  - The default-0 reads (`InitScript` :226-227, `AnimationManager` :87/:92) are unreachable: the first launch writes Music=1 and Sound=1 before any of them runs, and the keys then always exist.
  - The `mm:ss` counters exist only on hidden legacy UI. `LIFESAddCounter` is only in `LevelsMap.prefab`, the invisible legacy map that still drives life regeneration, and the legacy `Counter_` timer is on the hidden HUD.
- **Files changed:** `InitScript.cs` (one line removed; other sessions' edits not included), `Leadboard/LeadboardManager.cs` (one line removed)
- **Fix:** removed the two debug prints. Nothing was changed for the latent defaults or the hidden timers.
- **Tests executed:** recompile; fresh Play Mode start to home; console since entering Play; lives state.
- **Result:** 0 console entries from startup to home (before: 3 info lines). Lives 5/5, home normal.
- **Regression result:** the startup path is unchanged apart from the logs. The life-regeneration owner (`LIFESAddCounter`) was not touched.
- **Console result:** clean
- **Performance result:** n/a
- **Screenshots/evidence:** driver log `kv/play/ralph-022727.txt`
- **Remaining risk:** none
- **Next task:** final verification summary and `FINAL_QA_REPORT.md`.
