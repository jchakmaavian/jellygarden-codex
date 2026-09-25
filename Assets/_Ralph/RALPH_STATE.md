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
| Safe area | Home, title and HUD read `Screen.safeArea`; legacy popups do not | Suspected: home tab-bar background ignores the bottom inset (protected layout). Not a defect (iteration 30): the backdrop extends through the inset, and the edge controls follow the safe area |
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
| R-19 | P3 | Hidden HUD and home kings keep animating; AI hint coroutine pile-up (Kings fixed in iteration 27, in the untracked King Venguard work; the hint coroutines measured bounded, no defect) | RoyalGameplayPresentation, AI |
| R-20 | design | Offline economy has no coin income (5 coins lifetime). Product decision, not changed by the loop | LevelManager :371 (commented out) |
| R-21 | P2 | Home profile avatar and area chest ignore real taps: their only hit area is disabled (found in verification, iteration 18) | ReferenceHomeLayout.Hide :18 |
| R-22 | P3 | 4:3 tablets: the raised Home tab icon draws over the Settings dialog's bottom button (layering is intentional; needs a size or layering decision) | RoyalHomeScreen / ReferenceHomeLayout (protected) |
| R-23 | P3 | Out-of-moves CONTINUE shows a gem icon while home calls the balance coins | PreFailed prefab art; ties to R-20 |
| R-24 | P2 | Per-frame garbage from `Counter_` (`Object.name` reads) and `BoostIcon` (strings) in normal play; incremental GC is off (fixed, iteration 20; the GC setting is a decision) | Counter_.cs, BoostIcon.cs; ProjectSettings `gcIncremental` |
| R-25 | decision | The board, HUD, tool bar and home controls draw sprites and effects extracted from the Royal Match APK (reference-asset integration of 2026-09-24: `cdd68361`, then the imported `ReferencePieceVisual` prefabs and `ReferenceVfxPool`). Royal Throne's own piece art, piece animators and effects are bypassed, and 9 effect and booster suites fail for that reason. Iteration 28 measured it: with the imports off, cell breaks, rockets, TNT, Color Ball V5 and Propeller V5 pass, and effects passes 369 checks before failing on the imported HUD's smaller playable area (360x640: 30.9 px per cell, floor 32). Iteration 29 ran the rest of effects: with the imports off, 493 of its 495 checks pass (only the two 360x640 layout checks fail); as shipped, the imported leaf and lily sprites also fill under 88% of a cell. Only 6 of the 9 fail because of the imports; the other 3 are R-28. The imports also switch in through `ReferenceActivationVisual.Attach`, `RoyalBoardSurface.Edge` (imported border instead of the own `frame-edge-*` art), `PlayableViewport` and the HUD, tool bar and home art, none of which has a runtime switch. Legal and product decision; the loop changed neither the art nor the tests (found in review, iteration 25) | `RoyalGameplayArt` :55 -> `ReferenceGameplayCatalog.For`; `RoyalPieceSkin` :159; `ReferenceVfxPool.Has` early returns in `MatchVFXService` :380/:447/:494, `CandyShatterService` :354, `ColorBallVfxV4` :49, `RoyalPowerupDirector` :252, `RoyalRocketSequence` :147/:318; `ReferenceActivationVisual.Attach` from `ColorBallVisualV4` :71 and `PropellerVisualV5` :63; `RoyalBoardSurface.Edge` :69; `RoyalGameplayPresentation.PlayableViewport` :204 |
| R-26 | decision | The QA-only reference campaign logs three "[Reference Gameplay incomplete]" warnings: cupboard door feedback, coin-collection particles and the mailbox lid animation were not recovered and have no substitute. Tied to R-25 | `RoyalCampaignBlockers` :380 |
| R-27 | P1 (editor) | A script reload in the middle of Play, which Unity does by default when a script changes during Play, resets every static. `RoyalSave.Testing` then falls back to the player's own profile, the static `InitScript.lifes` reads 0 and is saved on exit, and the Input System, uGUI `Selectable` and QA menu references break. It zeroed the real save's lives on 2026-09-25 and killed a campaign run. Editor only: builds never reload. Fixed in iteration 26 with `RoyalPlayModeReloadGuard`, which holds reloads until Play stops | `InitScript` :103/:824 (static lives, quit save); `RoyalSave.Testing`; new `Editor/RoyalPlayModeReloadGuard.cs` |
| R-28 | P3 (tests) | Three Color Ball suites test code the game no longer runs, and fail with every import off (iteration 28): `RunColorBall` (live v1) waits for `ColorBallFrameAnimator`, which no runtime code attaches; `RoyalColorBallQaChecks` expects `ColorBallBoostView` for a single Color Ball, which has run the V4 charge-and-beam sequence since the 09-21/22 rework; `ColorBallV4Checks` holds the V5 prefab that ships to V4 rules (V5 lifts the 1.75 rad per-frame cap) and never selects `UseV4Baseline`. Retire or re-target them: Color Ball V5, the version that ships, has its own suite, which passes with the imports off | `RoyalColorBallChecks` :41; `RoyalColorBallQaChecks` :109; `ColorBallV4Checks` :63 |

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

### Iteration 24 (verification V-08: Levels 1-50 regression)
- **Date/time:** 2026-09-25 02:28-02:38 local
- **Area inspected:** every protected level, after the loop's fixes that touch protected code:
  - `LevelManager` R-02, R-03, R-05;
  - `MatchVFXService` and `InitScript` R-18;
  - the home layout R-21.
- **Issue:** none found
- **Severity:** n/a
- **Reproduction:** QA profile. Each of levels 1-50 was opened with the QA "Open level" command (the real entry path without spending a real life) and checked:
  - state Playing on the requested level;
  - squares and pieces present;
  - at least one target;
  - the HUD limit equals `LevelManager.Limit` (moves, or m:ss on timed levels).
- **Root cause:** n/a
- **Files changed:** none
- **Fix:** none
- **Tests executed:** the sweep above (`kv/ralph_run.py` phases `v12a`/`v12b`).
- **Result:**
  - 50/50 OK.
  - The timed levels 20, 27 and 32 show "0:57", "0:57" and "0:56" (the R-11 fix). Board sizes run from 45 to 99 squares.
- **Regression result:** this is the regression. The QA session restored the real profile (`OpenLevel=14`).
- **Console result:** no entries in either half
- **Performance result:** n/a
- **Screenshots/evidence:** driver logs `kv/play/ralph-022847.txt`, `ralph-023326.txt`
- **Remaining risk:** opening a level is not playing it to the end. Wins and fails are covered by the campaign and QA suites (iteration 25).
- **Next task:** review pass (re-read every change, then the project's full check suites, lint and type check).

### Iteration 25 (review: re-read everything, full check suites, lint, type check)
- **Date/time:** 2026-09-25 02:40-04:20 local
- **Trigger:** the user asked for the previous output to be treated as unverified. In this order: re-read every change, run the full test suite, linter and type checker, read the real output, and fix every failure, including pre-existing and unrelated ones.
- **Re-read:** the whole loop diff (20 commits, 19 C# files). Five risk points were checked against the code, and the committed hunks hold:
  - **(a)** `RoyalBackButton.NotePress` runs in every state (`if (InputManager.Instance != null)` in `LevelManager.Update`).
  - **(b)** `RoyalHintPresenter` only hides the hint on Back; it closes nothing.
  - **(c)** The forced-active rail tools only undo the legacy moves/time toggle (`LevelManager` :598-607).
  - **(d)** `GetRandomItems` terminates: the candidates are distinct, and the only rejection is a duplicate pick.
  - **(e)** `bannerFailed` has exactly two events, so skipping the hold loses nothing.
- **Defect found in touched code (pre-existing):** `DateTimeManager.IsMinutesPassed` / `IsHourPassed` compared the `TimeSpan.Minutes` / `.Hours` components instead of `TotalMinutes` / `TotalHours`, so 1 h 0 min read as "no minute passed". Fixed. 11 unit cases in the QA profile pass (fails=0), including 60 min 20 s and 24 h 10 min, which the old code got wrong.
- **Type checker = the C# compiler.** A full clean rebuild (`CleanBuildCache`) of all 165 assemblies: 0 errors. Two of my intermediate warning fixes failed to compile and were caught by the rebuild before anything ran:
  - `System` resolves to `JellyGarden.Scripts.System` inside `LevelManager`;
  - locals still assigned later.
- **Linter:** none is configured (no .editorconfig, ruleset, analyzer config, csc.rsp, CI, package.json or pyproject). The lint signal is compiler warnings plus Unity's serialization analyzer. **69 -> 0:**
  - `Assembly-CSharp` 22 -> 0 and `Assembly-CSharp-Editor` 33 -> 0:
    - obsolete APIs moved to their current forms;
    - unused fields and locals removed (with their assignments);
    - unreachable code after `return false;` removed;
    - `[NonSerialized]` added where Unity never serialised the field;
    - `[Obsolete]` added on the drawer override of an obsolete member.
    - The Legacy/Round Android icons are kept, with the warning suppressed locally: dropping them changes APK icon resources, which only a device build can verify.
  - Spine 14 -> 0 (vendor, untracked King Venguard work):
    - 12 guarded patches (13 after iteration 26) (`UNITY_6000_5_OR_NEWER`, original code in `#else`) applied by `patch_spine_unity6_deprecations.py`, recorded in `UNITY6-COMPAT-PATCHES.md`.
    - The sprite-slice inspector was ported to `ISpriteEditorDataProvider` (asmdef reference plus `SPINE_2D_SPRITE`). Tested on a temporary copy of the King atlas: all 48 regions exact, the second pass updates in place with stable sprite IDs, and the copy was deleted.
    - The first script run stopped on its own count guard at a 3D `Rigidbody` site, whose `isKinematic` is not deprecated. The target was narrowed to the 2D chain, the files restored, the script dry-run on copies, then applied.
- **Test suites:** the project has no Unity Test Framework tests. Its suites are editor check suites, run one per fresh Play session, with a real-profile snapshot before and after (`kv/suites.py`). Results:

  | Suite | Result | Notes |
  |---|---|---|
  | `ReferenceLevelChecks.VerifyData` (edit) | PASS | 50 levels, 3933 cells |
  | `RoyalCampaignChecks.Data` (edit) | PASS | 294 checks |
  | `ReferenceHomeSkyChecks` | PASS | includes "Settings still opens above cloud layer", which failed on 09-24 11:23 UTC |
  | `RoyalHomeScreenChecks` | FAIL -> PASS (167) | The pagination check assumed Story opens on page 1; it opens on the next level's page (page 2 on the real profile), so it is now relative. The suite also left the real `OpenLevel` at the next level (see below). |
  | `ReferenceHomeFinishChecks` (3 finishes) | PASS | |
  | `ReferenceBoosterChecks` | PASS | |
  | `ReferenceLayoutChecks` | FAIL -> PASS | Two checks went stale when the tool bar was connected (`3f5def19`, 09-24 15:54): the name selector also caught the tool bar's "Native left/right" art, and the hand tap now selects the tool in `ReferenceBoosterBar` instead of `LevelManager.ActivatedBoost`. |
  | `ResponsiveInputChecks.Run` / `InputEvents` | PASS / PASS | |
  | `TNTSunArt.Run` (checks only) | PASS | |
  | `CheckBoardPalette` | PASS | |
  | Dialog review (`RoyalPresentationChecks`) | PASS (21) | |
  | `RoyalCampaignChecks.RunAll` (reference campaign 01-50) | PASS | "ALL FIVE BATCHES PASSED" in one run (404 s); the runbook says it had never completed in one go before. Its 3 warnings are the reference import's own "[Reference Gameplay incomplete]" notices (R-26). |
  | `RoyalQaChecks` | FAIL -> PASS (328) | (1) It looked for "GAME TESTING MODE", which the Mod Menu replaced on 09-22: now "Mod Menu". (2) The refill check waited a scaled 3.2 s, from the start of a long frame, for a 3.0 s real-time deadline (measured: real 2.92 s, scaled 3.21 s): it now waits on the real clock and checks that exactly one life arrives at 2.8-4 s. The game's refill itself works (4 -> 5 at 3.0 s). (3) "Reset refill timer" was checked at full lives, where the UTC schedule correctly has no timer: now checked one life below the cap. |
  | `RoyalEffectsChecks.Run` | FAIL | "Gameplay NONE colour 0 fills 94% of its cell": every board piece is a sprite from the Royal Match APK (R-25). The suite stops at its first failure, after 52 passing checks. |
  | `RunCellBreaks` (first run ever) | FAIL | 7 pass, then "3-match emits restored Ice accent": `MatchVFXService.PlayMatchCenter` returns early when the imported "MatchItemExplodeParticles" exist (R-25) |
  | `RunTnt` / `RunRockets` / `RunColorBall` | FAIL | the imported piece visual replaces `TNTFrameAnimator`, the rocket renderer and `ColorBallFrameAnimator` (R-25) |
  | `RoyalColorBallQaChecks` | FAIL | 1 pass, then the imported "LightballRayPrefab" rays replace the in-house beams (`ColorBallVfxV4` :49, R-25) |
  | `ColorBallV4Checks` / `ColorBallV5Checks` | FAIL | NullReferenceException: `ColorBallSeatV4` is no longer attached (R-25). The V4 suite's last recorded run (09-21 18:54, before the import) had also failed, with an IndexOutOfRangeException. |
  | `PropellerV5Checks` | FAIL | 5 pass (including the loop's `PropellerVisualV5` edit), then "Created Propeller uses independent V5" (R-25) |

  The per-version booster suites write tracked evidence, so each run was bracketed by a backup and a byte-identical restore. Not run: the superseded Color Ball V2/V3 and Propeller V2-V4 suites (dead code; a run would overwrite their historical evidence), the recorders (`PropellerTntRegression.Before`, `RoyalEffectsChecks.Inspect`/`Capture*`, `KingVenguardRuntimeCheck.Render`) and the import/refresh/bake commands (they rewrite reference-derived assets). Every suite left the real profile unchanged, apart from the `OpenLevel` side effect below.
- **Console errors reviewed** (buffer seq 238015-240014, 975 errors):
  - 946 came from one 3-second burst (09-24 11:21 UTC, a recompile during Play).
  - One `ReferenceHomeSkyChecks` failure, which passes now.
  - Two `TNTSunArt.Bake` errors, logged five minutes before that tool's first commit. The committed version has neither defect.
  - The R-09 reproduction before its fix.
  - Tooling noise.
  - The 7 compile errors from my own first attempts, since fixed.
- **Side effect found and fixed:** `RoyalHomeScreenChecks` left the real `OpenLevel` at the next level (14 -> 23). The real value was restored to 14, and the suite now restores it in `Finish`.
- **Files changed:**
  - runtime: `PropellerVisualV5`, `MapTargetIcon`, `RoyalTitleScreen`, `HandTutorial`, `InitScript`, `LevelManager`, `MapCamera`, `Square`, `DateTimeManager`;
  - editor: `LevelMakerEditor`, `PostImporting`, `RoyalAppIconBuilder`, `RoyalColorBallChecks`, `RoyalColorBallQaChecks`, `RoyalEffectsChecks`, `RoyalRocketChecks`, `RoyalTNTChecks`, `LevelsEditorBase`, `MapLevelEditor`, `ReorderableDrawer`, `ReferenceHomeFinishChecks`, `ReferenceHomeSkyChecks`, `RoyalHomeScreenChecks`, `ReferenceLayoutChecks`, `RoyalQaChecks`;
  - vendor, working tree only (untracked King Venguard work): 7 Spine files, `spine-unity-editor.asmdef`, `UNITY6-COMPAT-PATCHES.md`, and the new patch script.
- **Remaining risk:** the effect and booster suites cannot pass while the imported Royal Match visuals replace the in-house ones (R-25). Royal Throne's own board art, piece animators and effects have therefore not been exercised in game since 09-24 14:41. They need a full effect-suite run if they are restored.
- **Next task:** commit the review edits, then `FINAL_QA_REPORT.md`.

### Iteration 26 (second review: re-read, type check, lint, every suite again)
- **Date/time:** 2026-09-25 04:20-05:50 local
- **Trigger:** the user asked again (three more times during this iteration) for the previous output to be treated as unverified: re-read every change since the last verified checkpoint, run the full test suite, linter and type checker, read the real output, and fix every failure.
- **Re-read:** every review edit (25 tracked files, `scratchpad/review_diff.txt`) and the Spine patch set against its pre-patch backup. This time each API migration was checked for behaviour, not only for compiling:
  - **Spine defect found in my own patch (fixed).** `NamedBuildTarget.FromBuildTargetGroup` throws `ArgumentException` for 14 retired groups (WebPlayer, PS3, XBOX360, Tizen, PSP2, PSM, SamsungTV, N3DS, WiiU, Facebook, Lumin, Stadia, plus WP8 and BlackBerry, which Spine already skips). The old `...ForGroup` methods only logged a validation error for them. So the patched `SpineBuildEnvUtility.EnableBuildDefine` / `DisableBuildDefine` would have thrown at WebPlayer, the second group, and never reached Android. Latent: only the Spine Preferences TK2D toggle calls them, and this project never shows it.
    - **Fix:** under the same `UNITY_6000_5_OR_NEWER` guard, `IsInvalidGroup` treats a group with no named target as invalid.
    - **Consistency:** the fix was added to `patch_spine_unity6_deprecations.py`, which was re-run on a pristine copy. Seven files came out byte-identical to the working tree, and `BuildSettings.cs` and `UNITY6-COMPAT-PATCHES.md` were copied from the regenerated copy. A second run refuses the patched folder.
    - **Test:** in the compiled editor, `IsInvalidGroup` classifies all 33 groups as expected (18 valid, 15 skipped), and the real `DisableBuildDefine` loop with an absent define walks every valid group without an exception and leaves the defines unchanged.
    - `PostImporting` (same API change) uses only Android, iOS and WSA, which all have named targets.
  - **`FindObjectOfType` / `FindFirstObjectByType` -> `FindAnyObjectByType`.** Unity 6.5 deprecates both older calls because they depend on instance-ID order, and names `FindAnyObjectByType` as the replacement. Every singular lookup the loop changed targets a singleton (`LevelsMap` and `Path`: one prefab; `MapCamera`, `RoyalHomeScreen`, `TutorialManager`: one each in `game.unity`). Every runtime scene load is single-mode, so there is one `AudioListener` at a time.
  - **`FindObjectsOfType` -> `FindObjectsByType`** (unsorted): the `MapLevel` lists are re-sorted by number, and the other sites already used `SortMode.None`.
  - **`[NonSerialized]`:** `CombineManager` is a plain class without `[Serializable]`, Unity cannot serialise `Hashtable`, and no scene or prefab YAML carries either field, so nothing changes.
  - **Removed code:** `InitScript.Update` only set `leftControl`, which nothing read. `HandTutorial`'s removed block read a plain field (`AI.combineType`).
  - **R-24 `Counter_` name cache:** no runtime code renames a counter object (the only rename of HUD objects, in `RoyalMenuSkin`, targets the `Targets` containers, which carry no `Counter_`).
  - **Stale comment (fixed):** `InitScript` still pointed star control to "Settings -> GAME TESTING MODE". The Mod Menu replaced that screen on 09-22 and holds the star controls: the comment now says "Mod Menu".
- **Type checker and lint:** a full clean rebuild (`CleanBuildCache`) of all 165 assemblies, with every compiler message captured per assembly (`kv/cs_warnings_final.tsv`): **0 errors, 0 warnings**. The same capture recorded the 69 warnings and 7 errors of the first review pass, so an empty file is a real pass. There is still no linter configuration in the repository.
- **Test suites (final code):** two full runs of every runnable suite, each in a fresh Play session with the real profile compared before and after.
  - **First run:** 12 pass. `RoyalHomeScreenChecks` and `RoyalCampaignChecks.RunAll` failed, both explained and fixed below. The 9 suites after the mid-Play reload are not valid results: the effects run met the corrupted `Selectable` table, and the other 8 were skipped when the runner's console query crashed.
  - **Second run (final code, clean editor):**

  | Suite | Result |
  |---|---|
  | `ReferenceLevelChecks.VerifyData` (edit) | PASS (50 levels, 3933 cells) |
  | `RoyalCampaignChecks.Data` (edit) | PASS (294) |
  | `ReferenceHomeSkyChecks` | PASS |
  | `RoyalHomeScreenChecks` | PASS (167; also 3 of 3 in a row after the fix) |
  | `ReferenceHomeFinishChecks` | PASS |
  | `ReferenceBoosterChecks` | PASS |
  | `ReferenceLayoutChecks` | PASS |
  | `ResponsiveInputChecks.Run` / `InputEvents` | PASS / PASS |
  | `TNTSunArt.Run` | PASS |
  | `CheckBoardPalette` | PASS |
  | Dialog review | PASS (21) |
  | `RoyalQaChecks` | PASS ("ALL QA CONTROLS PASSED") |
  | `RoyalCampaignChecks.RunAll` | PASS ("ALL FIVE BATCHES PASSED", 398 s) |
  | `RoyalEffectsChecks.Run`, `RunCellBreaks`, `RunTnt`, `RunRockets`, `RunColorBall`, `RoyalColorBallQaChecks` | FAIL, each at the same check as in iteration 25 (R-25) |
  | `ColorBallV4Checks`, `ColorBallV5Checks`, `PropellerV5Checks` | FAIL, same as iteration 25 (R-25); tracked evidence restored byte-identical |

  The real profile was unchanged after every suite. The 9 failures cannot be fixed in the tests without certifying the copied Royal Match assets. Nor can they be fixed in the game without reversing the 09-24 import that the user asked for on 09-23. There is no switch: the imported visuals turn on whenever their `Resources` exist. So they stay a decision (R-25).
- **Home suite failure (FAIL "Close stays on screen"):** FAIL "Close stays on screen" on the first full run (it passed with 167 checks in iteration 25).
  - **Root cause (test harness):** the suite ran from `EditorApplication.update`, where `Screen` intermittently describes the editor window. A probe sampled `Screen` for 3 s on Home: `EditorApplication.update` read 846x1227 in 28 of 806 samples (3.5%); the player loop read 1080x1920 every time after its first step. The area dialog's Close button spans x 917-1065, so on a wrong read `xMax <= Screen.width` fails.
  - **Not a game bug:** the dialog opened from either context lands in the same place, fully on screen. Its safe-area anchors stay (0,0)-(1,1), because `Screen.safeArea` and the size always come from the same context.
  - **Fix:** `RoyalHomeScreenChecks` now runs every step in the player loop through `RoyalRegressionRunner`. The first step yields once, because a coroutine's first step runs synchronously in the calling eval. A run id stops overlapping runs, and a small editor hook still reports a Play session that ends mid-suite ("Play session remains available"), as before.
  - **Test:** 3 runs in a row, each in a fresh Play session: PASS 167/167 each, real profile unchanged.
- **Campaign failure and real-save damage (caused by my own pending edit):** on the first full run, `RoyalCampaignChecks.RunAll` stopped after 3 batches, and the player's real save went from `Lifes=5` to `Lifes=0`.
  - **Chain:** at 04:32 I edited `InitScript.cs` (a comment), then requested the clean rebuild with `RequestScriptCompilation`, which compiles from disk but does not refresh the AssetDatabase. The edit therefore stayed pending. The first refresh after it was automatic (probably editor focus), at 04:50:39 in the middle of batch 31-40. `Logs/Editor.log` records: "Requested script compilation because: AssetDatabase observed changes in script compilation related files" (1 changed file), a 1-second build, then "Reloading assemblies after forced synchronous recompile" during Play.
  - **Damage from the reload:** it killed the campaign coroutine, so the runner's cleanup wrote "BATCH FAILED" with no failed check, and the QA `EndSession` threw in `ToMap`. It reset every static: `RoyalSave.Testing` became false, and the static `InitScript.lifes` read 0, so the exit save wrote `Lifes=0` and a fresh refill deadline into the real profile. It also broke the Input System maps, `RoyalQaMenu.quick` and uGUI's static `Selectable` table. The `Selectable` errors carried into the next Play session (the effects run), because entering Play does not reload here.
  - **Recovery:** I killed the job (the wrapper's children had survived TaskStop and started the sweep), ended the sweep's QA session through its normal path, and stopped Play. I then restored the real lives exactly as `InitScript.RestoreLifes` writes them (`Lifes=5`, `NextLifeUtc` empty, `RestLifeTimer=0`). The runner's snapshot (`OpenLevel`, coins, lives, audio, motion, hints, star and score totals) then matched the pre-run one. No other key can have been written: the QA cleanup died in its first step (`ToMap`) before writing anything, and after the reload the only automatic writers are the life schedule and the quit/pause save, which touch only the four life keys. Finally I ran an Edit Mode refresh (0 pending changes) and a forced script reload for clean statics.
  - **Fix (R-27):** new editor-only `RoyalPlayModeReloadGuard` locks assembly reloads on entering Play and unlocks them on exit, so a mid-Play reload waits for Play to stop. Test: a reload requested mid-Play did not run (88 reloads before and 12 s later, game alive with lives 5); it ran right after Play stopped (89). Real profile unchanged, 0 console errors.
  - **Process fix:** every edit is now followed by `unity command recompile`, which refreshes first. The suite runner's console query now retries instead of crashing: after the error flood it had failed with "list index out of range" and skipped 8 suites.
- **Shared-package tests:** the shared Match-3 VFX kit's own Unity Test Framework tests, batch `-runTests` in the scratch project (`file:` dependency plus `testables`): EditMode 17/17 and PlayMode 22/22 passed, 0 failed or skipped. The package's runtime and editor assemblies were compiled fresh in this run with 0 warnings; its test assemblies came from the build cache.
- **Levels 1-50:** 50/50 OK on the final code, in the QA profile. Every level opens to live play with squares, pieces, goals and the right HUD limit; the timed levels 20, 27 and 32 show "0:56". 0 console entries, and the real profile was identical before and after.
- **Files changed:** committed as `adea09ff` (zero warnings: 21 files), `de45bc0d` (`DateTimeManager`), `77f78097` (`RoyalHomeScreenChecks`, `RoyalQaChecks`, `ReferenceLayoutChecks`) and `51e2e6af` (new `Editor/RoyalPlayModeReloadGuard.cs`). Before committing, a dry run of the commit helper diffed each file's exact commit content against HEAD: every hunk is the loop's. `InitScript` and `LevelMakerEditor` were three-way merged, so other sessions' uncommitted edits stay out, and the committed versions reference nothing removed.
  - Working tree only (untracked King Venguard work): `Assets/Spine/.../BuildSettings.cs`, `UNITY6-COMPAT-PATCHES.md`, `Design/.../patch_spine_unity6_deprecations.py`.
  - Tooling (scratchpad): `kv/ralph_run.py` `seq()` retries.
- **Remaining risk:**
  - The 9 effect and booster suites stay red until R-25 is decided. While the imported visuals replace Royal Throne's own, its piece art, animators and effects are not exercised in game.
  - The reload guard changes editor behaviour for everyone using this editor: a script saved during Play now takes effect when Play stops. Delete the file to return to Unity's default.
  - `DateOfExit` in the real profile was rewritten by the damaged exit. It is rewritten on every exit anyway, and only feeds the life schedule, which is now full.
- **Next task:** R-19: measure whether the hidden Home and HUD Kings still animate (both screens hide with a `CanvasGroup` alpha of 0 and stay active), and the AI hint coroutine pile-up; then fix what is worth fixing.

### Iteration 27 (R-19: hidden Kings and hint coroutines)
- **Date/time:** 2026-09-25 05:48-06:50 local
- **Area inspected:** R-19, from the initial static pass: "hidden HUD and home Kings keep animating; AI hint coroutine pile-up".
- **Issue (part 1, confirmed):** Home and the gameplay HUD hide by setting a `CanvasGroup` alpha to 0 and stay active (`RoyalGameplayPresentation` :158/:165, `RoyalHomeScreen` :86/:124). So the King on the hidden screen kept animating and rebuilding his mesh:
  - on Home, the HUD King cost 70 us per frame;
  - in a level, the Home King cost 79 us per frame.

  These are editor (Mono) figures, timed with a Stopwatch around `Update(0)` + `LateUpdate()` x 200. The win and fail card Kings are inactive while their cards are closed and cost nothing.
- **Part 2 (not a defect):** counted with the profiler markers `AI.CheckPossibleCombines() [Coroutine: MoveNext]` and `AI.showTipCor() [Coroutine: MoveNext]` for 50 s, with a control marker reading 1 per frame:
  - **idle:** one hint display alive (1 resume per frame) and one scan per ~7 s cycle (4 s idle, 2.4 s hint, 1 s scan);
  - **during 5 real moves:** scans rise to 1-5 resumes per second. The display count reads 2-3 in the frame a new hint replaces the old one, because a new coroutine's first step runs inside `StartCoroutine`;
  - **after the moves:** back to one display and the same cycle, with no growth.

  Every scan ends after showing a hint; the only self-restart follows a no-moves reshuffle. No change to the protected AI. A first attempt read all zeros, because script markers are only emitted while the Profiler runs. That was not evidence, so it was re-run with the Profiler on and restored afterwards.
- **Severity:** P3 (performance)
- **Root cause:** `SkeletonGraphic` updates whenever its GameObject is active. A `CanvasGroup` alpha of 0 hides it without deactivating it.
- **Files changed:** `Assets/Characters/KingVenguard/V1/Scripts/KingVenguardCharacter.cs`. This is untracked King Venguard work, so the change is in the working tree only and not committed.
- **Fix:** `KingVenguardCharacter.LateUpdate` freezes the `SkeletonGraphic` while an enabled `CanvasGroup` above the King has alpha 0. On the frame he shows again it refreshes him once (`Update(0)`, `LateUpdate()`), so that frame never draws a stale pose. Disabled groups are ignored, as Unity ignores them; this was found when re-reading the change before the full run.
- **Tests executed:**
  - **Screen changes:** home, level, HUD reaction, pause, resume, home. The King on the hidden screen is frozen with his time held, and the visible one animates. The HUD reaction plays through into idle. Every active King keeps his 266-vertex mesh.
  - **Group states:** a disabled alpha-0 group does not freeze him (time 6.032 -> 6.565). An enabled one freezes him, with time held at 6.565. Removing it unfreezes him, with a mesh. After 200 frames, the reused list holds exactly the 2 groups of the parent chain.
  - **Regression suites:** home, ref-home-sky, ref-home-finish, ref-layout, ref-tools, dialogs and qa-controls pass, followed by the full run below.
- **Result:** the invisible King's cost (70-79 us per frame in the editor) is gone. The visible King is unchanged.
- **Regression result:** a full verification run on the final code, after a clean rebuild (165 assemblies: 0 errors, 0 warnings):
  - **Suites:** 14 pass (campaign-all "ALL FIVE BATCHES PASSED" in 413 s), and the same 9 fail at the same checks as before (R-25). The real profile was unchanged after every suite, and the booster evidence was restored byte-identical.
  - **Levels 1-50:** 50/50, 0 console entries, real profile identical before and after. The timed levels 20, 27 and 32 passed only on the sweep's 1.5 s re-check. Measured per frame, the HUD time label trails `Limit` by 63-105 ms after each timer tick, and every lagging frame shows exactly the previous second. This is the HUD's deliberate 0.1 s label refresh, not a defect. The sweep's check (`kv/ralph_run.py` `LEVELCHECK`) now accepts the previous second on timed levels and labels it, instead of relying on the re-check.
  - **Shared package:** 17/17 EditMode and 22/22 PlayMode tests pass, 0 compiler warnings.
- **Console result:** 0 errors during the tests (2 routine warnings after the recompile).
- **Performance result:** about 70-80 us per frame saved in the editor, likely more on a phone.
- **Remaining risk:** a King on a screen that fades in from alpha 0 resumes his idle from the pose where he froze. He is refreshed on the first visible frame, so that frame shows no stale pose, but his idle does not jump ahead to where it would have been.
- **Next task:** none left that the loop can act on alone. Every open backlog item needs a decision or a device: R-25 (most urgent), R-26, R-20, R-22, R-23, R-15, R-16, plus texture compression, incremental GC and the King's name. The next step is to gather evidence for the R-25 decision: whether Royal Throne's own board art and effects still work if the imports are switched off. That can be done at runtime without editing any file.

### Iteration 28 (R-25 evidence: Royal Throne's own art and effects with the imports off)
- **Date/time:** 2026-09-25 07:00-07:58 local
- **Area inspected:** R-25. Do Royal Throne's own board art, animators and effects still pass the 9 red suites when the imported Royal Match visuals are off? The answer sizes option (b) of the decision.
- **Method:**
  - **Runtime switch, no file edited** (scratchpad `kv/imports_off.cs`). In each fresh Play session, before any board exists, the four caches that decide whether an import is used are set to "missing":
    - `ReferenceGameplayCatalog.entries` is emptied. That turns off the board sprites and the catalog's gate on imported sound effects.
    - `ReferencePieceVisual.prefabs` is set to null for the 6 imported booster visuals.
    - `ReferenceVfxPool.prefabs` is set to null for all 28 imported particle prefabs.
    - `ReferenceGameplayAudio.cache` is set to null for all 44 imported clips.

    Unity resets all four at the next Play start. The editor was also reset after every run with the classes' own `Reset` methods (`kv/imports_reset.cs`). It was checked at the start and end of every suite: the first board piece draws `ruby` instead of `Items-MatchItem-book__22221`, and no imported pool or piece view was created.
  - **Imports without a cache stay on under that switch:**
    - the Color Ball and Propeller activation visuals (`ReferenceActivationVisual.Attach` loads its prefab on every call);
    - the board border, the HUD layout, the tool bar and the home art.
  - **Parked run for the activation visuals.** For the 5 Color Ball and Propeller suites, `Visuals/ColorBall.prefab` and `Visuals/Propeller.prefab` were moved out of `Resources` with `AssetDatabase.MoveAsset` (GUIDs kept) and moved back afterwards (`kv/park.cs`, `kv/unpark.cs`, `kv/r25_parked.py`). Afterwards the SHA-256 of both prefabs and both metas was identical, the folder's git status was unchanged and the parking folder was gone. This was the one step that touched project files, and it left no change.
  - **Evidence and profile.** Every result folder a suite writes (tracked evidence under `Design/Effects/` and `Assets/Boosters/*/Animation/Review`) was backed up first and restored byte-identical afterwards. The new results are kept in scratchpad `r25/`, `r25-parked/` and `r25-on/`. The player's real profile was compared before and after every suite and never changed.
- **Results:**

  | Suite | Imports on (as shipped) | Imports off |
  |---|---|---|
  | Cell breaks | FAIL (imported match particles) | **PASS** (78 checks) |
  | Rockets | FAIL (imported rocket visual) | **PASS** (168 checks) |
  | TNT | FAIL (imported TNT visual) | **PASS** (37 checks) after a test fix (3 below) |
  | Color Ball V5, the version that ships | FAIL | **PASS** with the activation prefab parked |
  | Propeller V5 | FAIL | **PASS** with the prefab parked ("ALL PROPELLER V5 CHECKS PASSED") |
  | Effects (main suite) | FAIL after 48 passing checks | 369 checks pass after the test fixes (was 54), then it fails at a consequence of the imported HUD (below) |
  | Color Ball live v1 (`RunColorBall`) | FAIL | FAIL, also when parked: it tests `ColorBallFrameAnimator`, which no runtime code attaches any more |
  | Color Ball QA (`RoyalColorBallQaChecks`) | FAIL | FAIL, also when parked: it expects `ColorBallBoostView` for a single Color Ball. A single ball has run the V4 charge-and-beam sequence since the 09-21/22 rework |
  | Color Ball V4 | FAIL | FAIL, also when parked: it holds the V5 prefab that ships to V4 rules. V5 deliberately lifts V4's 1.75 rad per-frame cap (`if(!referenceV5)`), and the suite never selects `UseV4Baseline` |

- **Test defects found and fixed** (test code only, in Royal Throne's own art path; none involves imported art):
  1. **Rocket fill (`RoyalBoardSurfaceChecks`).** The check expected idle rockets at 94% of the cell. Rocket review v6 made them 78% on purpose (`RoyalPieceSkin.RocketIdleCellFill`, "fitted to 78% of the actual cell"), and the rocket suite checks 78%. The check now expects 78% for rockets and 94% for every other piece.
  2. **Board surface (`RoyalBoardSurfaceChecks`).** The check expected 5 layers (a shadow and 4 edges) and a shadow under all 64 cells. `8aca59e4` (09-20 19:35, an hour after the checkpoint whose recorded run passed all 495 checks) added the grout layer and limited the drop shadow to the bottom-right silhouette. The expectations were derived from `RoyalBoardSurface.RebuildIfNeeded` for the complete 8x8 fixture:
     - 6 layers;
     - grout under 64 cells (256 vertices, order -2);
     - shadow under 15 cells, the bottom row and the right column (60 vertices, order -1);
     - edges of 8 segments each (32 vertices, order 2).
  3. **TNT seated tolerance (`RoyalTNTChecks`).** 1 of 6387 frames failed: Reveal frame 14 at zoom 1.000689, with the base 0.000639 below the seated pose (under 0.07 source px). The check counts zoom up to 1.001 as seated and allows 0.0001. The animator pulls the frame down for any zoom above 1: 0.93 times the excess, at most 0.00093. The seated band now allows 0.001, and the enlarged band stays strict. A frame observer beside the suite (`kv/tnt_observer.cs`) showed every other term holding on all 6387 frames: sprite, scale, sorting, hidden static art, pose and single renderer.
  4. **TNT preview on Map (`RoyalEffectsChecks`).** "Effects Workshop preview still plays while already on Map" counted particle-system particles one frame after `Preview("TNT")`. The check was replayed step by step in both modes (`kv/preview_probe.cs`). Neither `Burst` nor the preview emits a single particle-system particle. The preview's TNT actor does play while on Map: Activated, Ignition, Explosion at frame 8, then Debris. The check now waits, for up to 2 s, for the preview actor to reach Explosion while the game stays on Map. The file's mixed line endings were kept, so the diff is those 5 lines.

  With the imports on, effects and TNT still fail at the same checks as before these fixes:
  - effects: "Gameplay NONE colour 0 fills 94% of its cell", after 48 passing checks;
  - TNT: "quarter: the created TNT carries the frame animator".

  So the as-shipped verdicts do not change.
- **Found, not fixed (part of R-25):**
  - **The imported HUD shrinks the board on small phones.** With the imports off, effects stops at "9x11 board fits safe area above boosters with readable cells at 360x640 (30.9 px/cell)", against a floor of 32.
    - `9f27a636` (09-24, "Import packaged gameplay HUD layout") changed `RoyalGameplayPresentation.PlayableViewport`. It used to reserve fixed fractions of the safe area (x 2-98%, y 14.5-80.8%). It now reserves the imported HUD's 155 px at the bottom and 285 px at the top, at the 720x1280 reference.
    - The check is right. This is a consequence of the import, and it cannot be switched off at runtime.
  - **R-25 missed some switch points:**
    - **`ReferenceActivationVisual.Attach`** (`ColorBallVisualV4` :71, `PropellerVisualV5` :63). It has no cache, and while it is attached the V4 and V5 code skip their own visuals through `UsesNative`.
    - **`RoyalBoardSurface.Edge`.** In `9f27a636` the imported `Border-side__17165` replaced Royal Throne's own `frame-edge-*` art. Without it the board has no gold edge at all.
    - **`PlayableViewport`**, described above.
    - **`ReferenceGameplayAudio`.** The imported sound effects are gated by the catalog, except on the activation paths.
    - **The HUD, tool bar and home art.**
- **Corrections to iterations 25-27:** iteration 25 says the effects suite stops "after 52 passing checks"; its result file, like every run since, says 48. They also attributed all 9 failures to R-25. Three of them do not come from the imports: Color Ball live v1, Color Ball QA and Color Ball V4 fail with every import off, the parked prefabs included. They test Color Ball implementations the game no longer runs (R-28).
- **Severity:** R-25 is a decision; R-28 is P3 test maintenance.
- **Files changed:**
  - `Assets/JellyGarden/Scripts/Editor/RoyalBoardSurfaceChecks.cs`
  - `Assets/JellyGarden/Scripts/Editor/RoyalTNTChecks.cs`
  - `Assets/JellyGarden/Scripts/Editor/RoyalEffectsChecks.cs`

  All three were identical to HEAD before this iteration, so the commit carries only these hunks. The two prefabs were moved and restored, with no net change. The tooling is in the scratchpad (`kv/`).
- **Tests executed:**
  - the 9 suites with the imports off (623 s);
  - effects and TNT again after each fix;
  - the 5 Color Ball and Propeller suites with the prefabs parked (414 s);
  - effects and TNT with the imports on;
  - the TNT frame observer, and the preview replay in both modes;
  - a final confirmation of effects and TNT on the committed bytes, in both modes.
- **Regression result:** on the committed bytes, with the imports off, TNT passes (37 checks) and effects passes 369 checks before the HUD failure. With the imports on, both fail at their R-25 checks as before. The other 14 suites do not call the three changed files: `RoyalBoardSurfaceChecks` and `RoyalTNTChecks` are called only from `RoyalEffectsChecks`.
- **Console result:** only the suites' own failure exceptions, plus the CLI's routine "not in automated mode" warning after each recompile.
- **Compile:** `Assembly-CSharp-Editor` was rebuilt four times, each with 0 errors and 0 warnings.
- **Remaining risk:**
  - **Option (b) still needs code changes.** It is not only a matter of deleting the imports, because five places load imported files with no switch:
    - the activation visuals (without them the V4 and V5 visuals are already correct);
    - the board border, whose `frame-edge-*` art must be restored;
    - `PlayableViewport` and the HUD layout, reverted or re-sized;
    - the tool bar;
    - the home art.
  - **The main effects suite may hold more stale checks after check 369.** It stops at its first failure, and the 09-20 version had 495 checks.
  - **"Leaving gameplay cancels finished TNT particles and delayed corona" passes vacuously.** Nothing emits particle-system particles there any more.
- **Next task:** none the loop can act on alone. The decisions are R-25 (now with evidence), R-28, and the items already open.

### Iteration 29 (R-25 evidence, part 2: the rest of the main effects suite)
- **Date/time:** 2026-09-25 08:00-08:20 local
- **Area inspected:** the last 201 checks of the main effects suite (`RoyalEffectsChecks.Run`), from `RoyalBoardPresentationChecks` on. They had not run since 09-20 in either mode, because the suite aborts at its first failure: check 49 as shipped, and check 370, the imported-HUD layout, with the imports off (iteration 28). Iteration 28 flagged this as a remaining risk.
- **Method** (no project file edited; scratchpad `kv/effects_tail.cs` and `kv/effects_tail.py`):
  1. `RoyalEffectsChecks.Run()` builds the disposable workshop board exactly as the suite does and runs its first synchronous step.
  2. The suite's coroutine is detached. Its runner's stack is cleared first, so the suite's own teardown does not run early.
  3. `RoyalBoardPresentationChecks` and `RoyalHintPresentationChecks` run with a check callback that records instead of throwing, so every failure is listed, not only the first. Then come the suite's closing lines (`Suite()` :166-180), copied unchanged apart from that callback.
  4. The suite's own `Finish()` restores the level. It runs from a `finally` block, even if a section throws.

  Each mode ran in a fresh Play session. The suite's result folder (including the hint captures under `raw/`) was backed up and restored byte-identical. The real profile was unchanged after both.
- **Results:**
  - **Imports off** (iteration 28's runtime switch): **199 of 201 pass.** The 2 failures are the imported HUD at 360x640 (9x11 and 5x11 boards, 30.9 px per cell against a floor of 32).
    - This section starts at the same position as in the 09-20 run (its first check is check 295 in both).
    - Adding iteration 28's 294 checks before it, the whole suite has now been measured with the imports off: **493 of 495 pass**. The only failures are the two layout checks caused by the HUD import.
  - **As shipped: 176 of 201 pass.**
    - 23 fail "Piece fills its cell without crossing adjacent hit areas". The failing cells are exactly the cells holding colours 3 and 4, allowing for the suite's own recolouring of (1,1) and (2,1), which held colours 3 and 4 before it set them to 0. In the imported catalog those are "leaf" and "lily": at their recovered native size they fill less than 88% of a cell. With the imports off, all 64 cells pass.
    - The other 2 are the same HUD layout failures.
    - Everything else passes as shipped: hints on and off, hint rendering and cancellation, input, invalid swap, tap activation and refill, the audio, projectile and fragment caps, and "No runtime errors during regression run".
- **Not measured as shipped:** checks 49-294. They run inline with the suite's throwing check, so they cannot be run this way without copying the suite's own code. The reference campaign suite (all 50 levels, as shipped) covers gameplay.
- **Observation:** in the first run the driver also found two other `RoyalRegressionRunner` objects besides the suite's own. None exists in Edit Mode, and the second run found only the suite's own. They were removed without running their pending iterators, and the run's results and the real profile were clean. Origin unknown; editor tooling only.
- **Severity:** evidence for the R-25 decision. No new defect outside R-25.
- **Files changed:** none in the project.
- **Tests executed:** the tail in both modes; before each, a fresh Play session with a settled board.
- **Console result:** one routine log entry per run, no errors.
- **Remaining risk:** checks 49-294 are unmeasured as shipped.
- **Next task:** none the loop can act on alone. The decisions are unchanged: R-25, now with complete evidence for the main effects suite, R-28, and the items already open.

### Iteration 30 (verification V-09: safe-area bands at 20:9)
- **Date/time:** 2026-09-25 08:20-08:29 local
- **Area inspected:** the open safe-area question from the inventory ("legacy popups do not read `Screen.safeArea`"; "Suspected: home tab-bar background ignores the bottom inset") and from V-05, which recorded insets as "cannot be simulated in the Game view". The approach: measure where the controls are instead of simulating a device.
- **Method:**
  - The nine screens of V-05 at 1080x2400 (20:9), in the QA test profile: home, settings, pre-level, HUD, pause, leave confirmation, win, out of moves, fail.
  - The same layout probe as V-05, plus one band check: every visible, interactable control whose rect reaches into the top 130 px (notch, punch hole, status bar) or the bottom 100 px (gesture bar). These are conservative bands: an iPhone-style inset is about 130 / 95 px at this width, and a punch-hole Android status bar about 65-130 px. Scratchpad `kv/layout_inset_probe_loop.cs` and `kv/inset_run.py`.
  - A Play-session check that the controls in the bands are children of the RectTransforms their screens anchor to `Screen.safeArea` (`kv/safe_hierarchy.cs`).
  - The Android player settings.
- **Results:**
  - **Legacy popups:** settings, pre-level, pause, leave confirmation, win, out of moves and fail have **no control in either band** at 20:9. They do not read the safe area and do not need to. All nine screens also passed the base checks again: every control on screen and tappable at its centre, no clipped text.
  - **Home:** the resource bar (Profile, Coins, Lives, Stars, Settings) reaches 92 px into the top band, and the main navigation sits at the bottom edge (100 px into the band). All of them are children of `RoyalHomeScreen.safeArea`, which `RefreshLayout` anchors to `Screen.safeArea`, so on a device that reports insets they move inside it.
    - The only home element outside that rect is `navigationBackground`, deliberately: its height is `navHeight + root.rect.height * min.y`, so the tab bar's backdrop extends down through the bottom inset.
    - The inventory's suspicion is therefore **not a defect**.
  - **HUD:** the booster rail and pause button reach 73 px into the bottom band. They are children of the HUD's `content` ("Safe area"), which is anchored to `Screen.safeArea` every frame.
  - **Android:** `androidRenderOutsideSafeArea: 0`, so the game never draws into a display cutout. The system keeps the window clear of the notch or punch hole.
- **Severity:** none found.
- **Files changed:** none in the project. The Game view was switched to a temporary "Ralph temp" 1080x2400 preset for the run and restored: the user's "RoyalThrone 1080x1920" preset is selected again, and the preset list (22 entries) is identical to before.
  - The first removal attempt failed. In Unity 6.5 `GameViewSizeGroup.RemoveCustomSize` takes the overall index; a custom-list index is rejected. That logged 2 console errors ("Invalid index 4 18 5") from my tooling, not from the game. The script is fixed.
- **Tests executed:** the nine-screen probe at 1080x2400, the hierarchy check in a fresh Play session, and the player settings.
- **Console result:** no game entries. The 2 errors above came from the preset removal.
- **Remaining risk:**
  - The editor always reports a full-screen safe area. Seeing home and the HUD actually move out of the insets, and the gesture bar's behaviour in fullscreen, still needs a notched phone (R-16's device session).
- **Also checked:** `ProjectSettings.asset` shows uncommitted define changes: the offline build policy strips `UNITY_ADS` and `UNITY_INAPPS` from Standalone, iOS and Windows Store, and `submitAnalytics` is 0. The working-tree file is identical to the pre-loop backup (`d6fe15ce`), so this is part of the 2026-09-23 offline change awaiting a decision, not a loop change. It is now noted in the report under that item.
- **Next task:** none the loop can act on alone; the decisions and device checks are unchanged.
