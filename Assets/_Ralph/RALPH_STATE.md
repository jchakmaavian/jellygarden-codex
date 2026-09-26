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
| R-15 | P2 | How-to-play popup unreachable from a level (fixed, iteration 37, by the user's decision: HELP beside HOME in the pause menu opens it). The recorded "HELP label without a button" was wrong: the pause menu showed no HELP at all | RoyalMenuSkin, AnimationManager, RoyalBackButton |
| R-16 | P2 | Hidden legacy map may still accept taps (checked in iteration 40: it does not. At four sizes every on-screen map level lies under UI, and a delivered click on one did nothing) | MapLevel, LevelsMap |
| R-17 | P3 | Sound/Music default 0 vs 1 mismatch; `print(DateOfExit)` every launch; plurals ("1 coins"); timer formats | various |
| R-18 | P3 | Project-code compile warnings (about 30 runtime, 40 editor); obsolete Find APIs | see compile log |
| R-19 | P3 | Hidden HUD and home kings keep animating; AI hint coroutine pile-up (Kings fixed in iteration 27, in the untracked King Venguard work; the hint coroutines measured bounded, no defect) | RoyalGameplayPresentation, AI |
| R-20 | design | Offline economy has no coin income (5 coins lifetime). Decided on 2026-09-25: restore the template's 1 coin per win (done, iteration 37) | LevelManager :370 |
| R-21 | P2 | Home profile avatar and area chest ignore real taps: their only hit area is disabled (found in verification, iteration 18) | ReferenceHomeLayout.Hide :18 |
| R-22 | P3 | 4:3 tablets: the raised Home tab icon draws over the Settings dialog's bottom button (fixed, iteration 38, by the user's decision: a tall home dialog that does not fit is shortened and moved; 16:9 and 20:9 unchanged; iteration 48: its 60% minimum now holds) | ReferenceHomeLayout.FitDialog |
| R-23 | P3 | Out-of-moves CONTINUE shows a gem icon while home calls the balance coins (fixed, iteration 37, by the user's decision: it shows the coin; iteration 38, also decided: the shop, its packs, the lives refill and the tool shop show the coin too) | RoyalMenuSkin |
| R-24 | P2 | Per-frame garbage from `Counter_` (`Object.name` reads) and `BoostIcon` (strings) in normal play; incremental GC was off (fixed, iteration 20; incremental GC turned on in iteration 37, by the user's decision) | Counter_.cs, BoostIcon.cs; ProjectSettings `gcIncremental` |
| R-25 | decided | The board, HUD, tool bar and home controls draw sprites and effects extracted from the Royal Match APK (reference-asset integration of 2026-09-24: `cdd68361`, then the imported `ReferencePieceVisual` prefabs and `ReferenceVfxPool`). Royal Throne's own piece art, piece animators and effects are bypassed, and 9 effect and booster suites fail for that reason. Iteration 28 measured it: with the imports off, cell breaks, rockets, TNT, Color Ball V5 and Propeller V5 pass, and effects passes 369 checks before failing on the imported HUD's smaller playable area (360x640: 30.9 px per cell, floor 32). Iteration 29 ran the rest of effects: with the imports off, 493 of its 495 checks pass (only the two 360x640 layout checks fail); as shipped, the imported leaf and lily sprites also fill under 88% of a cell. Only 6 of the 9 fail because of the imports; the other 3 are R-28. The imports also switch in through `ReferenceActivationVisual.Attach`, `RoyalBoardSurface.Edge` (imported border instead of the own `frame-edge-*` art), `PlayableViewport` and the HUD, tool bar and home art, none of which has a runtime switch. Legal and product decision; the loop changed neither the art nor the tests (found in review, iteration 25) **Decided by the user on 2026-09-25: keep the imported art**, accepting the legal exposure. The 6 suites stay red and are not edited to accept the copied art. | `RoyalGameplayArt` :55 -> `ReferenceGameplayCatalog.For`; `RoyalPieceSkin` :159; `ReferenceVfxPool.Has` early returns in `MatchVFXService` :380/:447/:494, `CandyShatterService` :354, `ColorBallVfxV4` :49, `RoyalPowerupDirector` :252, `RoyalRocketSequence` :147/:318; `ReferenceActivationVisual.Attach` from `ColorBallVisualV4` :71 and `PropellerVisualV5` :63; `RoyalBoardSurface.Edge` :69; `RoyalGameplayPresentation.PlayableViewport` :204 |
| R-26 | decided | The QA-only reference campaign logs three "[Reference Gameplay incomplete]" warnings: cupboard door feedback, coin-collection particles and the mailbox lid animation were not recovered and have no substitute. Tied to R-25. Decided on 2026-09-26: leave the warnings | `RoyalCampaignBlockers` :380 |
| R-27 | P1 (editor) | A script reload in the middle of Play, which Unity does by default when a script changes during Play, resets every static. `RoyalSave.Testing` then falls back to the player's own profile, the static `InitScript.lifes` reads 0 and is saved on exit, and the Input System, uGUI `Selectable` and QA menu references break. It zeroed the real save's lives on 2026-09-25 and killed a campaign run. Editor only: builds never reload. Fixed in iteration 26 with `RoyalPlayModeReloadGuard`, which holds reloads until Play stops | `InitScript` :103/:824 (static lives, quit save); `RoyalSave.Testing`; new `Editor/RoyalPlayModeReloadGuard.cs` |
| R-28 | P3 (tests) | Three Color Ball suites test code the game no longer runs, and fail with every import off (iteration 28): `RunColorBall` (live v1) waits for `ColorBallFrameAnimator`, which no runtime code attaches; `RoyalColorBallQaChecks` expects `ColorBallBoostView` for a single Color Ball, which has run the V4 charge-and-beam sequence since the 09-21/22 rework; `ColorBallV4Checks` holds the V5 prefab that ships to V4 rules (V5 lifts the 1.75 rad per-frame cap) and never selects `UseV4Baseline`. Retire or re-target them: Color Ball V5, the version that ships, has its own suite, which passes with the imports off. **Decided by the user on 2026-09-25: leave them as they are**; they stay in the suite list and stay red. | `RoyalColorBallChecks` :41; `RoyalColorBallQaChecks` :109; `ColorBallV4Checks` :63 |
| R-29 | P3 | Back on a popup opened over another one (the booster guide or a shop over the pre-level card, the coin shop over a shop) closed the card underneath and left the top popup over home: each popup reads Back in its own `Update`, and iteration 37's one-Back-one-action return let whichever ran first answer, in the editor always the card (before it, one Back closed both). Found by a review copy of this session; fixed in iteration 46: a popup covered by a later open one ignores Back | AnimationManager.Update |

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

### Iteration 31 (verification V-10: resource growth over repeated levels)
- **Date/time:** 2026-09-25 08:35-08:55 local
- **Area inspected:** whether a long session accumulates objects or memory: pooled effects, board pieces, the HUD, the Kings' Spine graphics, audio. No earlier check looked for growth across levels.
- **Method:** in the QA test profile, the same level (12) is cycled: open, settle, sample, then win it through the QA "Win instantly" command. Each sample first runs `GC.Collect()` and `Resources.UnloadUnusedAssets()`, so anything counted is still referenced. It counts:
  - GameObjects, textures, sprites, materials, meshes, audio clips, particle systems, audio sources, Spine `SkeletonGraphic`s and board `Item`s;
  - managed and native memory.
  1. **CLI-driven, 30 cycles** (`kv/leak_run.py`, `kv/leak_sample.cs`). Every object count is identical from cycle 2 to cycle 30: 7180 GameObjects, 2269 textures, 1598 sprites, 3110 materials, 12618 meshes (+2 once, after cycle 1), 41 clips, 64 particle systems, 16 audio sources, 4 Spine graphics, 70 pieces. Back on the map the board's objects are gone (6822 GameObjects). The rise from the first map sample (3239) to the first level is the one-off creation of pools and cached art. Managed memory rose about 0.12 MB per cycle and native about 0.2 MB.
  2. **Calibration.** Each CLI eval loads one more assembly into the editor (776 -> 807 after 30 trivial evals), but that costs only about 2 KB of managed memory each. This does not explain 0.12 MB per cycle, so the drift was measured again without the CLI.
  3. **In-game loop, 20 cycles, no CLI traffic while it runs** (`kv/leak_inloop.cs`: one coroutine drives the QA service and writes its samples at the end). Managed memory 1,491,792 KB -> 1,491,820 KB (+28 KB over 20 cycles, with a drop back at cycle 19). Every object count is again constant, apart from +2 meshes once at cycle 2.
- **Result:** no leak. The object counts are flat across 50 cycles, and without CLI traffic so is managed memory. The drift in run 1 came from the tooling.
- **Severity:** none found.
- **Files changed:** none in the project.
- **Console result:** no entries in either run. The real profile was unchanged, and all level results went to the QA profile.
- **Remaining risk:** editor (Mono) figures. An IL2CPP build on a phone has a different allocator; the object-count evidence carries over, the byte figures do not.
- **Next task:** a sweep of levels 51-100, which no check had opened.

### Iteration 32 (verification V-11: levels 51-100)
- **Date/time:** 2026-09-25 08:55-09:05 local
- **Area inspected:** levels 51-100. The Story grid shows 100 levels and 100 level files ship, but every earlier sweep covered only the protected 1-50.
- **Method:** the V-08 sweep, unchanged, in the QA test profile. Each level is opened through the QA service, and the sweep checks:
  - the level number and the Playing state;
  - squares, pieces and goals present;
  - the HUD's moves or time label equal to the level's limit, allowing the HUD's 0.1 s refresh lag on timed levels.
- **Result:** **50/50 OK.** Boards have 81-99 squares and 43-99 pieces, with 1-3 goals each. The timed levels are 54, 56, 59, 69, 80, 84, 91 and 94. Level 69 was read inside the HUD's 0.1 s refresh window ("1:17" against 76 s) and was accepted by that rule. There were no console entries, and the real profile was unchanged.
- **Severity:** none found.
- **Files changed:** none.
- **Next task:** the fourth review requested by the user: re-read everything since `d844e783`, then the clean rebuild, every suite, both sweeps and the package tests.

### Iteration 33 (fourth review: re-read, type check, lint, every suite, both sweeps, package tests)
- **Date/time:** 2026-09-25 09:05-09:52 local
- **Trigger:** the user's review instruction. It arrived four times during this iteration: treat the output as unverified, re-read every change since the last verified checkpoint, run the full test suite, linter and type checker, read the real output, and fix every failure.
- **Re-read:** every change since the last fully verified checkpoint, `d844e783`.
  - The three test files of `127c54f8` were read against the code they check. The rocket fill matches `RocketIdleCellFill`. The board-surface orders and vertex counts match `RoyalBoardSurface.RebuildIfNeeded` line for line. The TNT preview wait cannot miss the Explosion state (about 0.25 s, sampled every frame), and `Cancel()` clears pooled actors first. The seated tolerance covers the 0.00093 maximum.
  - The three documentation commits were re-read for their claims.
  - No defect found.
  - The working tree holds nothing else from the loop. Of the files modified since 07:00, 1,939 are the evidence folders that the runs restored byte-identical, the three committed test files, and six git-ignored `UserSettings/AI.*` files that Unity's AI packages rewrote during a domain reload.
- **Type checker:** first a refresh, which had nothing pending ("No scripts needed recompilation"), then a clean rebuild (`CleanBuildCache`) with every compiler message captured per assembly (`kv/cs_warnings_final4.tsv`): **165 assemblies, 0 errors, 0 warnings**.
- **Lint:** the repository still configures no linter: no .editorconfig, rulesets, analyzer settings, CI or package scripts. The compiler reports 0 warnings for the project, and the package test logs 0 as well.
- **Suites:** all 23, each in a fresh Play session. **14 pass, 9 fail.**
  - Every suite's result lines are identical to iteration 27's run, apart from the campaign's timestamp: the same 14 passes, and the 9 failures at the same checks with the same messages.
  - The campaign passed "ALL FIVE BATCHES PASSED" in 395 s.
  - The 9 failures are 6 from the imports (R-25) and the 3 superseded Color Ball suites (R-28).
  - The real profile was unchanged after every suite, and the booster evidence was restored byte-identical.
- **Levels 1-100 sweep** (QA profile): **100/100 OK**, with no console entries and the real profile identical before and after. Timed levels 32 and 56 were read inside the HUD's 0.1 s refresh window and accepted by that rule.
- **Shared package tests:** EditMode **17/17**, PlayMode **22/22**, with 0 failed, inconclusive or skipped.
- **Failures fixed:** none were fixable without a decision.
  - The 6 R-25 failures cannot pass unless the tests certify the copied art, which is not allowed, or the import is reversed, which is the user's call.
  - The 3 R-28 suites need retiring or re-targeting, and as shipped they would still fail because of R-25.
- **Next task:** the TNT-cancel check ("Leaving gameplay cancels finished TNT particles and delayed corona") may pass vacuously. Measure it; if it cannot fail, make it meaningful.

### Iteration 34 (the TNT-cancel check is not vacuous: a correction)
- **Date/time:** 2026-09-25 09:55-09:55 local
- **Area inspected:** two statements in iteration 28.
  - Its remaining risk calls "Leaving gameplay cancels finished TNT particles and delayed corona" vacuous ("Nothing emits particle-system particles there any more").
  - Its fix 4 says "Neither `Burst` nor the preview emits a single particle-system particle".
- **Method:** on an open board, `kv/burst_probe.cs` replays the check's `Burst` and counts, every frame, what the check counts: particles under the director and under `RoyalToolkitImpact`. It ran twice, once with the imports switched off and once as shipped:
  - **A:** 30 frames, staying in gameplay;
  - **B:** the backing state switched to Map one frame after the Burst, as the check does.
- **Result** (identical in both modes):
  - **A:** 0 particles on the Burst frame, 29 from the next frame, and still 18-23 after 30 frames.
  - **B:** 33 one frame after the Burst, then 0 from the first frame on Map.

  So the check is meaningful. Without the cancel on leaving gameplay, the particles would still be alive when it samples 0.14 s later. No test change is needed.
- **Correction:** both iteration 28 statements are wrong about `Burst`. The earlier probe sampled only the Burst frame, before emission starts, and then the Map, after the cancel. The same wrong sentence is in the message of commit `127c54f8`. What holds is that the TNT preview itself emits none: 45 frames through Explosion and Debris, and `TNT3DAnimator` has no particle code at all. So the rewritten preview check stands.
- **Severity:** none; the records are corrected.
- **Files changed:** the report's wording for the preview check, which now states what was measured.
- **Console result:** none. The real profile was unchanged after both sessions, and the editor's statics were reset afterwards.
- **Next task:** none the loop can act on alone. The 9 red suites wait on R-25 (6) and R-28 (3).

### Iteration 35 (the user's decisions on R-25 and R-28)
- **Date/time:** 2026-09-25 14:10-14:15 local
- **What happened:** every review asks for every failure to be fixed, and the 9 red suites could not be fixed without two decisions. So the loop asked for them, with the evidence of iterations 28-29.
  - **R-25:** the user chose **keep the imported art**, accepting the legal exposure. The alternatives offered were restoring Royal Throne's own art behind a switch (recommended), or adding the switch but leaving the imports on.
  - **R-28:** the user chose **leave the three superseded Color Ball suites as they are**. The alternatives offered were retiring them (recommended) or retargeting them.
- **Consequence:** the 9 red suites are now expected failures by decision:
  - effects, cell breaks, TNT, rockets, Color Ball V5 and Propeller V5, because of R-25;
  - Color Ball live v1, Color Ball QA and Color Ball V4, because of R-28.

  A later review should expect exactly these, at exactly the checks recorded in iteration 33, and treat any other failure as new. The tests are not edited to accept the copied art.
- **Files changed:** these records only.
- **Next task:** the remaining decisions (R-26; the 2026-09-23 offline and teal changes; R-20, R-23, R-22, R-15; texture compression; incremental GC; the King's name) and the device checks.

### Iteration 36 (incident: my tooling stopped a Play session it had not started)
- **Date/time:** 2026-09-25 14:18-14:25 local
- **What happened:** the loop waited about four hours for the user's answers (iteration 35), and during that wait a Play
  session was entered in the shared editor. The editor log shows it was entered after the loop's last session ended (09:57) and
  was still running when the loop's notes were imported (14:15). At 14:18 the loop's probe for R-15 found the editor playing.
  Its Play helper correctly refused to enter Play, but the script's cleanup (`finally: if playing: stop`) then stopped that
  session. The log shows the exit right after the refusal, preceded by command time-outs ("Main thread operation timed out
  after 5000ms").
- **The real profile afterwards:**
  - `OpenLevel` 14 -> 8 and `Lifes` 5 -> 4: a life was spent at 14:15:32, with the refill due at 14:20:32.
  - Coins, stars, scores and settings are unchanged.

  The life and `OpenLevel` were written by that session's own level start, three minutes before the stop, not by the stop.
  If a level was in progress, the stop ended that attempt. The loop has not changed the save; whether to restore it is the
  user's call.
- **Root cause:** the scratchpad drivers' cleanup stopped any running Play session, not only one the script had entered, and
  `suites.fresh_play()` also stopped a running session before each suite.
- **Fix** (scratchpad tooling, not the project):
  - `ralph_run.STARTED` is set by `play()`, and `stop()` only stops a session this process entered;
  - `suites.fresh_play()` refuses when the editor is playing instead of stopping it;
  - `r25_parked.py` waits for Edit Mode instead of stopping Play;
  - the old `run_tests.py` is retired.
- **Severity:** P1 (process): it can end the user's own play.
- **Next task:** ask the user whether to restore the save, and whether the editor is free for the four changes approved in
  iteration 35 (R-20, R-15, R-23, incremental GC).

### Iteration 37 (the approved changes: R-20, R-15, R-23 and incremental GC)
- **Date/time:** 2026-09-25 14:32-16:07 local
- **Decisions** (the questions of iterations 35 and 36, answered by the user, who also confirmed the editor was free):
  - R-20: restore 1 coin per win;
  - R-15: make HELP open How-to-play;
  - R-23: show the coin;
  - incremental GC: turn it on;
  - the real save after the 14:18 incident: leave it as it is. The life spent by that session has since refilled
    (`Lifes` 5); `OpenLevel` stays 8.
- **R-20 (done):** the win branch's own reward had been commented out: `//InitScript.THIS.AddGems(1);//Add coins after win`,
  which also named a `THIS` that `InitScript` does not have. It now calls `InitScript.Instance.AddGems(1)`, so each win
  earns one coin. `RoyalQaChecks` now checks that a win credits exactly one coin, and that a rejected repeat credits none.
- **R-15 (done; the recorded premise was wrong):**
  - The pause menu never showed a HELP label. The skin's HELP line targets an `Info` object that only the legacy Settings
    dialog has (`CanvasGlobal/Settings/Image/Info`, which calls `AnimationManager.Info`). That dialog opens only from the
    hidden legacy map (`LevelsMap.prefab`). So How-to-play was unreachable, as recorded, but no dangling label was shown.
  - The pause menu now has HELP beside HOME (the old HELP slot above HOME overlaps the Hints row). It opens the existing
    How-to-play lessons over the pause menu, and the game stays paused.
  - Close, or Back, on How-to-play returns to the paused menu; a second Back resumes, as before. In `AnimationManager`,
    Back closes How-to-play first, and closing it over the pause menu keeps the pause. One Back release is one action
    (`RoyalBackButton.ConsumedThisFrame`), whichever menu's `Update` runs first.
  - **Found in testing:** How-to-play opened from the pause menu held the first, squashed frame of its entrance (card scale
    1.64 x 0.53, `help_tutorial_over_pause.png` before the fix). The pause stops the clock, and a legacy `Animation` does
    not advance at `timeScale` 0. `RoyalMenuSkin.PlayEntranceUnscaled` now plays that entrance on the unscaled clock
    (`AnimationClip.SampleAnimation`) and fires its events, the two click ticks.
  - **Measured** against the unpaused entrance, frame by frame (`kv/help_entrance_*.cs`): both settle at full size within
    a frame of each other (0.347 and 0.350 s), with the same squash and stretch (0.811 x 1.342 and 0.811 x 1.341) and both
    click ticks. HELP tapped while the pause menu is still arriving, before its entrance stops the clock, also completes:
    the clock stops midway through How-to-play's entrance.
- **R-23 (done):** the out-of-moves CONTINUE shows the coin that home shows. The same balance still shows a gem on the gem
  shop's packs, the lives refill price and the tool shop's prices and balance (`RoyalMenuSkin` :147, :155, :197-198);
  they were outside the decision.
- **Incremental GC (done):** `gcIncremental: 1` in `ProjectSettings.asset`. The file's other uncommitted lines, from
  2026-09-23, stay out of the commit.
- **Tests:**
  - **Approved changes** (`kv/approved_test.py`, Play Mode, QA profile): **17/17**, including the unpaused baseline and the
    early tap. The real profile was unchanged. The console had no entry from the game; the only new entries were Unity
    editor warnings: the AI Assistant package's "Account API did not become accessible within 30 seconds"
    (`com.unity.ai.assistant`, `ApiAccessibleState.cs`) and the CLI's "Editor is not in automated mode".
  - **Type check:** a clean rebuild of every assembly, with each compiler message captured: **0 errors, 0 warnings**.
  - **All 23 suites**, each in a fresh Play session: **14 pass, and the 9 decided failures fail at the same checks as in iteration 33**: the 11 failure lines of the two runs are identical (`diff` of the `!` lines in `kv/play/final4_suites.log` and `final5_suites.log`). QA controls now writes 330 lines, including the two coin checks, both PASS. The dialog review wrote 18 lines where iteration 33 wrote 21, with no FAIL either time: it reviews the real profile's `OpenLevel`, which has been 8 since the 14:18 incident (it was 14), and level 8 has one goal fewer (2 instead of 3) on each of its three goal dialogs. The console showed the same warnings and errors as in iteration 33, plus the AI Assistant package's editor warning. The real profile was unchanged by every suite, and the booster suites restored their tracked evidence byte-identical.
  - **Levels 1-100:** **100/100**: every level opens to live play with a populated board, its goals and the correct HUD limit. No batch had a problem, the console had no entries, and the real profile was unchanged.
  - **Layout at four sizes** (V-04's nine screens plus How-to-play over the pause menu, `kv/layout4_run.py`): **40 of 40** screens at 1080x1920, 1080x2400, 1536x2048 and 720x1280. Every visible control is on screen and takes a tap at its centre, and no text is clipped. The pause menu now has 7 controls, HELP included, and How-to-play over it has 3 of 3. The console had no entries, the Game view preset listing was restored exactly, and the real profile was unchanged.
  - **Shared Match-3 VFX package** (scratch project): EditMode 17/17, PlayMode 22/22, 0 compiler messages.
  - **Not executable in this pass:** incremental GC takes effect in a player build, and this pass builds no APK; the running editor was started on 2026-09-24 09:57 and reports `GarbageCollector.isIncremental=False` although the setting is on, so every test in this pass ran under the old GC mode. Measuring collection pauses needs an editor restart (the user's call) or a device build.
- **Process slip (no effect):** during the full run, a dry run of the script that writes this entry wrote into the real
  `RALPH_STATE.md` and `FINAL_QA_REPORT.md` instead of copies, because the step that should have pointed it at the copies
  had failed. The two files held placeholder text for about a minute. Both were restored byte-identical to HEAD, and the
  editor log shows no import or refresh in that window. The script now names the folder it writes to, and dry runs check
  afterwards that the real files still match HEAD.
- **Files changed:** `LevelManager.cs`, `RoyalQaChecks.cs`, `RoyalMenuSkin.cs`, `AnimationManager.cs`, `RoyalBackButton.cs`,
  `ProjectSettings/ProjectSettings.asset`.
- **Commits:** `d7d55667` (R-20), `b309dc5b` (R-23), `e6c639b4` (R-15), `0d392864` (incremental GC), and the commit of these records. The trailer now appears once per message; the earlier loop commits carry it twice, because their messages included it and `ralph_git` appends it.
- **Next task:** the remaining decisions: R-26; the 2026-09-23 offline and teal changes; R-22; the gem icons that remain
  (gem shop, lives refill, tool shop); texture compression; the King's internal file names; and whether the editor may be
  restarted so incremental GC can be measured. Then the device checks.

### Iteration 38 (second round of decisions: coins everywhere, R-22, the editor restart)
- **Date/time:** 2026-09-25 16:10 to 2026-09-26 00:16 local. The loop waited 16:40-18:56 for the user's answers and 19:12-22:52 for a
  Play session of the user's to end.
- **Decisions** (asked at 16:20):
  - the 2026-09-23 changes: the user answered "continue" instead of choosing. Nothing was reverted; the item stays open.
  - R-22: fit the dialog on 4:3.
  - The currency: coins everywhere.
  - The editor restart for incremental GC: yes.
- **A Play session the loop had not started** ran in the shared editor from before 18:56. The loop did not touch it and saved
  no script while it ran. Asked, the user said it was theirs and they were done; by 22:52 they had left Play themselves, so
  nothing was stopped. The real profile's `OpenLevel` moved from 8 to 23 during it. No project file changed (checked against
  the 16:09 rebuild: only `ProjectAuditorSettings.asset`, rewritten with identical content, and the other agent's channel file).
- **R-22 (fixed):**
  - Measured first, in the home root's space (`kv/home_dialog_probe.cs`, four sizes). At 4:3 the Settings and Story
    dialogs (991.6 tall, centred) had their bottom button 52.6 units inside the raised tab art and their top 25.5 units over
    the resource bar. At 16:9, 20:9 and 720x1280 they fit.
  - `ReferenceHomeLayout` raises the selected tab's art and places the resource bar every frame, so the fit is there
    (`FitDialog`, in its `LateUpdate`). A dialog that does not fit is shortened and moved so that its button clears the art
    by 8 units and the dialog, close button included, stays 8 below the bar. A dialog that fits is not touched.
  - After: at 4:3 the dialogs are 860.9 tall, the button clears the art by 8.0 and the close button clears the bar by 7.5.
    The settings rows stay 89.9 above the button and 12.4 below the copy, and the Story grid 111.9 and 8.6. At 16:9, 20:9 and
    720x1280 every value is identical to before. Screenshots `kv/play/r38_1536x2048_{settings,story}.png`.
- **Coins everywhere (done):** the shop is titled "ROYAL COINS", its offline subtitle says "Coin packs are not available in
  this version", and its 4 packs, the lives refill price and the tool shop's 3 prices and balance show the coin. Probe:
  9 coin icons, all the coin sprite, 0 gem icons (screenshots `kv/play/r38_{gemsshop,liveshop,boostshop}.png`).
- **Correction to iteration 37:** it said CONTINUE "shows the coin that home shows". Both are coins, but home's resource bar
  draws the imported reference coin (sprite 22081), while CONTINUE and the shops draw the game's own `RoyalGameplay/coin`.
  The imported art is not spread further (R-25).
- **Editor restart (done):**
  - Checked first: Edit Mode, idle, no prefab stage, no window with unsaved changes, no asset with user edits (only fonts
    and shaders the editor marks itself). `main.unity` was marked unsaved. A copy of the in-memory scene, written to `Temp/`,
    differed from the saved file only in 42 empty-string formatting lines, 25 defaults of fields that other sessions' script
    edits added, and 4 sizes recalculated by the `[ExecuteAlways]` title and home layout code. So nothing was saved, and the
    saved file is unchanged.
  - Quit through `EditorApplication.Exit` at 23:05:33 (the process exited at 23:05:46). Relaunched with
    `-projectPath ... -automated` at 23:05:54, ready at 23:06:27, and licensing validated. The new session has no Unity Hub
    sign-in; the old one had come from the Hub.
  - After: `GarbageCollector.isIncremental=True`, `main.unity` clean, and the CLI's automated-mode warning gone.
  - A "Ralph temp" Game view preset came back from disk: removing presets through reflection had changed only the in-memory
    list. It was removed and the list saved (`SaveToHDD`), identical to before; `gv_restore.cs` now always saves.
  - Startup logged 414 shader-compile warnings from Unity's own `com.unity.render-pipelines.core` package (light-transport
    and probe shaders), not from project code.
- **Incremental GC, measured in the editor:**
  - 20 level cycles with a forced collection each (`kv/gc_inloop.cs`): object counts constant (7185 GameObjects, 1394
    textures, 563 sprites, 115 materials, 123 meshes), so no leak. Its GC-pause figure was vacuous: between forced
    collections nothing reached the collector's own trigger, so no collection ran in the measured windows.
  - 150 s of continuous play with no forced collection (`kv/gc_long.cs`): 4 collections; 868 MB allocated, mostly by the
    editor; mono heap 1781 MB before and after; frame time median 16.7 ms, p99 30.3 ms, max 91.2 ms. 8 frames spent time in
    `GC.Collect`, 4 of them 17.9-20.1 ms: in the editor each collection still costs one frame of about 20 ms.
  - That heap is almost all the editor's, and the GC mode is fixed per process, so there is no before/after and no guide to a
    phone. What pauses a player build sees remains a device check.
- **Tests:**
  - **Type check:** clean rebuild of all 165 assemblies in the restarted editor (23:13-23:15): 0 errors, 0 warnings.
  - **All 23 suites:** **14 pass, and the 9 decided failures fail at the same checks as in iteration 33**: the 11 failure lines are identical, and so are the console's warnings and errors (13 entries). QA controls wrote 330 lines, all PASS. The dialog review wrote 21 lines: it reviews the real profile's `OpenLevel`, now 23, a level with 3 goals. The real profile was unchanged by every suite, and the booster suites restored their tracked evidence byte-identical.
  - **Levels 1-100:** 98 of 100 in the full run; levels 83 and 84 went unchecked. While level 83 was checked, the editor's main thread was blocked for more than 5 s during a licensing burst (an access-token refresh, then 9 entitlement requests answered with 404, "Found 0 entitlement groups"). Three CLI requests timed out, and level 84 then found the game still on level 83. A re-sweep of levels 80-90 passed all 11, and levels 83 and 84 gave exactly iteration 37's values. The 404s come from the AI Assistant package (`com.unity.ai.assistant`, `EditorConnectionLimitProvider`), which checks for the `com.unity.editor.ai` entitlement that this account does not have. The previous, Hub-launched session logged 1512 of them and 21 main-thread time-outs, so they predate the restart.
  - **Layout at four sizes:** **40 of 40** screens at 1080x1920, 1080x2400, 1536x2048 and 720x1280, the refitted Settings dialog included. The console had no entries, the Game view was restored exactly, and the real profile was unchanged.
  - **Approved changes** (`kv/approved_test.py`): **17/17**, with an empty console and the real profile unchanged. Its R-23 label now says what it checks: the game's own coin icon.
  - **Shared VFX package:** EditMode 17/17, PlayMode 22/22, 0 compiler messages.
- **Files changed:** `ReferenceHomeLayout.cs`, `RoyalMenuSkin.cs`, `AnimationManager.cs`.
- **Commits:** `d01918ad` (R-22), `84fdacb7` (coins everywhere), and the commit of these records.
- **Next task:** the remaining decisions: R-26; the 2026-09-23 offline and teal changes; texture compression; the King's
  internal file names. Then the device checks, including collection pauses in a player build.

### Iteration 39 (third round of decisions: the 2026-09-23 changes, R-26, the King's names, the AI Assistant package)
- **Date/time:** 2026-09-26 00:19-00:25 local
- **Decisions** (asked with the evidence of iterations 37-38; each answered with the recommended option):
  - **The 2026-09-23 changes** (offline mode and the teal buttons): keep both as they are. Every test in this pass ran with
    them. They stay uncommitted in the working tree: they are another session's work, and the user did not ask for a commit.
  - **R-26:** leave the three "[Reference Gameplay incomplete]" warnings of the QA-only reference campaign.
  - **The King's name:** keep the internal "Venguard" folders, files and identifiers. Players see "King Vanguard"
    (`RoyalOfflineMode.KingName`).
  - **The AI Assistant package** (`com.unity.ai.assistant` 2.18.0-pre.2): leave it installed. Its checks for the
    `com.unity.editor.ai` entitlement, which this account lacks, can stall the editor for over 5 s. A check they interrupt is
    re-run, as levels 83 and 84 were in iteration 38.
- **Files changed:** these records only.
- **Still open, and not possible in this pass (no Android, by instruction):**
  - texture compression, which needs an art sign-off and a device check;
  - the device checks: R-16, notch insets, the hardware Back button, memory, frame time and collection pauses on a phone, and
    the launcher icons.
- **Next task:** none the loop can act on in this pass. Every remaining item needs a device or the art sign-off.

### Iteration 40 (the device checks the editor can do; the round icon; texture step 1)
- **Date/time:** 2026-09-26 00:40-03:52 local
- **Why:** the user asked for every unexecuted part to be finished. What needs a phone stays open (the user kept the no-Android
  rule), but several "device checks" had parts the editor can do.
- **R-16 (checked, no defect):**
  - `MapLevel.Update` polls the game's `InputManager` and hit-tests its own collider. It returns early when map clicks are off
    or `EventSystem.IsPointerOverGameObject(-1)` is true.
  - Coverage (`kv/r16_probe.cs`), home screen: map clicks are enabled, and every on-screen map level lies under a UI raycast
    target, 0 exposed: 21 of 21 (1080x1920), 25 of 25 (1080x2400), 17 of 17 (1536x2048), 21 of 21 (720x1280).
  - A delivered click (`kv/r16_click.cs`, with the user's OK to bring the editor to the front for about two seconds): the game's
    touch position reached level 28 at (70, 1130), under the non-control courtyard image; press and release registered
    (frames 605 and 607); the over-UI guard was true; the level did not scale, no popup opened and `OpenLevel` did not change.
  - Two earlier attempts proved nothing and are not counted: while the editor is in the background the Input System keeps
    every mouse disabled (even an added virtual one), and a virtual mouse loses the Position action to the real one.
- **Safe-area insets (checked, no defect):** Unity's Device Simulator (`kv/notch_run.py`) on "Wide Pill Cutout" (Android,
  1200x2640, the game gets 1200x2499 because it does not draw into cutouts) and "iOS Notch Device" (1170x2532, insets 141 at
  the top and 102 at the bottom). On V-04's nine screens plus How-to-play over the pause menu, every visible control and text is
  inside the safe area, on screen, tappable at its centre and not clipped: 20 of 20. The simulator's device choice and the Game
  view were restored.
- **Hardware Back (narrowed):** the Cancel action that every Back handler reads is bound only to `<Keyboard>/escape`; the
  editor tests drive Escape. Whether a phone's Back key arrives as Escape is the one part left for a device.
- **Launcher icons (checked; one defect fixed):** Adaptive (8+) uses the 1024 foreground, whose art spans 55.7% x 62.5% of the
  canvas, inside the 66.7% safe zone, over the navy background. Round (7.1) used the opaque square icon, so round launchers
  showed a square. The user chose the adaptive look: `RoyalAppIconBuilder` now builds `royal-throne-icon-round.png` from the
  adaptive layers (the central 72 of 108 dp, in a circle) and puts it in the six Round slots; 1.87/255 from the approved preview.
- **Texture compression, step 1 (done, decided by the user):**
  - Visibility across 30 screens (`kv/tex_visible.cs`): 162 of the audit's 184 uncompressed scene textures are drawn by nothing
    visible, 13 more only by legacy-map sprites on home, and 9 are visible (the home dialog icons, the tutorial finger, the fail cross).
  - The legacy map on home: switching its 340 renderers off changed only animated pixels (cloud edges, the King's portrait):
    it is fully covered.
  - Transient art: the combo words are dead code (`LevelManager.gratzWords` is never used) and the legacy explosion is never
    spawned (its load is commented out), but `CompleteLabel` and `NoMoreMatches` still show on every win and shuffle, at alpha 1,
    so their two textures were excluded. `gold.png` (NPOT with mipmaps) would not take ASTC and was left as it was.
  - 166 textures got an Android override: ASTC 4x4, normal quality, their own max size. About 74.6 MB -> 20.4 MB on a device.
  - Unity 6.5 rewrote their `.meta` files in its current format. Checked field by field against HEAD: no GUID changed, and every
    other difference is an old default sentinel written as the value it stood for (checked on an untouched old-format sprite).
- **Tests:**
  - **Type check:** a clean rebuild of all 165 assemblies after the last change (03:48-03:50): 0 errors, 0 warnings.
  - **All 23 suites:** **14 pass, and the 9 decided failures fail at the same checks as in iteration 33**: the 11 failure lines and the console's 13 warning and error entries are identical. The real profile was unchanged by every suite, and the booster suites restored their tracked evidence byte-identical.
  - **Levels 1-100:** **100/100**. No batch had a problem, the console had no entries, and the real profile was unchanged.
  - **Layout at four sizes:** **40 of 40** screens at 1080x1920, 1080x2400, 1536x2048 and 720x1280. The console had no entries, the Game view was restored exactly, and the real profile was unchanged.
  - **Approved changes:** first 12/17: all five Back steps failed. After the R-16 focus switch, the editor's Input System had seen a real focus loss, so it dropped injected events and kept devices disabled under an internal scope the public API does not clear; the game was not at fault. The harness's Back probe now re-enables those devices the way a focus gain does, and routes input to the game for the few frames of each gesture only. Then **17/17**, with an empty console and the real profile unchanged.
  - **Shared VFX package:** EditMode 17/17, PlayMode 22/22, 0 compiler messages.
- **Files changed:** `RoyalAppIconBuilder.cs`, `royal-throne-icon-round.png` (new), `ProjectSettings.asset` (the six Round
  slots), `Design/Branding/AppIcon-v1/checks.txt`, 166 texture `.meta` files, `TEXTURE_AUDIT.md`.
- **Commits:** `9bbc35d4` (round launcher icon), `7d8bb85a` (the 166 texture overrides), and the commit of these records.
- **Still open (a phone, which the user kept out of this pass):** a phone's Back key as Escape; memory, frame time and collection
  pauses on a phone; how launchers render the icons; texture steps 2-3 (visible art: a device comparison; protected art: the
  owner's approval).
- **Next task:** none the loop can act on in this pass.

### Iteration 41 (texture steps 2-3, decided by the user)
- **Date/time:** 2026-09-26 03:52-12:47 local
- **Why:** texture steps 2 and 3 were the last open items the editor could settle, and they were the user's to decide.
- **The question, and a blocked night:**
  - At 03:55 the loop asked the user about both steps, with the evidence below; the call was interrupted when the session ended.
  - From 04:01 to 11:32 every shell command and every write into the project needed an approval this session could not get.
    Nothing ran and nothing in the project changed (the branch stayed at `4539b0a4`); the loop only prepared scripts in its
    scratchpad. Its interim plan (compress 12, leave the protected art) was never applied.
  - At 11:32 the user's answers to that question arrived: "Compress the 11" (step 2) and "Compress them" (step 3: the
    protected art plus `fb_logout-button`). At 11:33 the shell worked again.
- **Evidence given with the question** (`kv/tex_astc_compare.cs`, `kv/play/astc/`): each source compressed to ASTC 4x4 in memory
  and decoded back; ASTC decoding is deterministic, so these are the texels a phone shows. Alpha-weighted PSNR 36.9-48.9 dB on
  the 11 visible textures and 42.7-61.9 dB on the protected art; differences only at edges and as fine grain, invisible at display
  size; on the logo the largest errors sit in its anti-aliased outline.
- **Applied at 11:33 (`kv/tex_compress23.cs`):** the step-1 override (ASTC 4x4, normal quality, each texture's own max size) on
  17 textures:
  - step 2, the 11 visible ones that are not protected art: the seven home dialog icons, `level_completed_text` and
    `no_match_text` (the win and shuffle banners), `finger_for_tutorial` and `x_failed`;
  - step 3, the protected art: `logo`, `royal_play_crown`, `royal_castle_blue`, `home-castle` and `wheel`, plus
    `fb_logout-button`, which nothing visible draws and which the audit flagged only because its name contains "logo".
  - Importer read-back: 17 of 17 overridden, and every imported texture is ASTC 4x4. About 31.1 MB -> 8.5 MB on a device
    (visible 7.8 -> 2.0 MB, protected 23.3 -> 6.6 MB), counting whole 4x4 blocks; mipmaps are off on all 17.
  - With step 1, 183 of the audit's 184 uncompressed scene textures now use ASTC on Android: about 105.8 MB -> 30.9 MB, 74.9 MB
    less. Only `gold.png` (0.11 MB, NPOT with mipmaps) is left.
  - Editor side effect: `game.unity` now loads in about 1.35 s instead of 1.2 s in Play Mode, most likely the editor decoding
    ASTC for display on this PC, whose GPU cannot sample it. A phone's GPU reads ASTC directly.
- **In context, after the change (`kv/shot_compare41.py`):** the layout run's screenshots from iteration 40, taken before the 17
  overrides and kept in `kv/play/res_before41/`, against this run's: 24 pairs at 1536x2048, 720x1280 and 1080x2400 (sheets in
  `kv/play/res_compare41/`).
  - Where the 17 are drawn, the difference is small: the Settings dialog's gear icon (170x167 px at 1536x2048) by 1.3/255 on
    average, 0.8% of its pixels by more than 8; the fail screen's crosses (40x40 px at 1080x2400, 27x27 at 720x1280) by 1.2 and
    1.1/255, 2.3% and 1.8% by more than 8.
  - Every other difference is something that moves between any two runs: the random level-12 board, drifting clouds, the King,
    the drop arrows, and the pre-level popup's pulsing boosts, PLAY and close buttons (three captures of today's build 0.35 s
    apart differ there by 5-24/255 on average). How-to-play over the pause menu is pixel-identical at both sizes compared.
- **The `.meta` rewrites (checked):** Unity 6.5 rewrote the 7 old-format files among the 17 in its current format.
  - `tex2_meta_check.py`, field by field against HEAD: 17 of 17 pass. No GUID changed; the Android entry is as intended; every
    other changed value is an old-format default sentinel written as the value it imports as; the rest are version numbers,
    three obsolete keys that held empty defaults, and 45 keys the current format adds, none of them an override.
  - The sentinels' meaning was measured in the editor (`kv/sentinel_probe.cs`): untouched old-format textures import wrap -1 as
    Repeat (8 of 8 sprites, 13 of 13 others), filter -1 as Bilinear, aniso -1 as 1 and mip bias -100 as 0. The rewritten files
    hold 0, 1, 1 and 0 and import the same: 12 of 12 wrap-0 sprites Repeat, 25 of 25 Bilinear with bias 0.
  - A correction drafted during the blocked night said the wrap sentinel meant Clamp for sprites, so that commit `7d8bb85a`'s
    "wrap -1 -> Repeat" was wrong. The measurement shows the draft was wrong: its only evidence, `failed_banner_01`, already had
    an explicit Clamp before step 1. `7d8bb85a`'s message stands; the draft was never recorded.
- **Corrections to iteration 40 (its record and commit `7d8bb85a` stay as written):**
  - **Size:** step 1's 166 textures take 22.25 MB as ASTC 4x4, not about 20.4 MB. The 20.4 was the audit's 8-bpp estimate,
    which ignores ASTC's 4x4 blocks, and small textures pad heavily (mipmaps are off on all 166). Step 1 saves about 52.4 MB,
    not 54.
  - **Its `.meta` check compared only the keys both versions have.** A strict re-check of the 166 (every key, the rules of
    `tex2_meta_check.py`) finds no GUID change and the Android entry as intended, and three differences it had not reported:
    - 10 `iPhone` platform entries are now keyed `iOS`, with identical settings, and 4 sprites' `fileIDToRecycleName` names
      moved to `internalIDToNameTable` with the same IDs: renames by the format upgrade.
    - 68 legacy sprite-packing tags were dropped (Pack1 50, Pack2 8, Game 6, Pack3 4). This changes nothing: Unity 6.5 no
      longer supports them (`TextureImporter.spritePackingTag` is obsolete, "support for packing sprites through
      spritePackingTag has been removed", and the legacy `Packer` class is gone), an untouched sprite whose `.meta` still says
      Pack1 reads back an empty tag (`kv/packer_probe.cs`), and the project has no Sprite Atlas.
- **A latent limit in R-22's `FitDialog` (found re-reading iteration 38's code; no change):** it runs every frame on the height
  it last set, so its 60% minimum is measured from the shortened height, not the design height. It cannot take effect:
  - the home canvas scales from 720x1280 with "Expand", so every screen squatter than that is 1280 units tall, and the layout
    width is capped at 0.68 x the safe height: every screen squatter than 0.68 (4:3 tablets, square, landscape) has the same
    geometry, where the fit needs 86.8% of the design height (iteration 38: 991.6 -> 860.9). Insets shrink the free space and,
    below 1180 units, the dialog with it, so by the same formulas the fit never needs less than about 80%;
  - `RoyalHomeScreen.RefreshLayout` restores the design height whenever the screen, the safe area or the open panel changes, so a
    dialog shortened for one shape never stays short on another.
  - Keeping the design height apart would make the minimum hold if those proportions ever change. The home layout is protected,
    and it gets no change without a reachable defect.
- **Iteration 40's safe-area result, checked after the fact:** it was recorded before its log had been read.
  `kv/play/notch_run.log` agrees with every figure: 20 of 20 screens, 0 issues, every control tappable, 0 new console entries,
  the Game view and the simulator's device restored, the real profile unchanged.
- **Tests:**
  - **Type check:** a clean rebuild of all 165 assemblies after the change (11:45-11:47): 0 errors, 0 warnings.
  - **All 23 suites:** **14 pass, and the 9 decided failures fail at the same checks as in iteration 33**: the 11 failure lines are identical. The console had 15 warning and error entries against the baseline's 13: an "Account API did not become accessible" warning in `ref-home-sky`, and five "/api/exec ... timed out after 5000ms" errors in `colorball` while the editor was blocked for about 30 s entering Play Mode (the log shows no import or compile then; the known licensing stalls). Re-run at 12:39, both matched the baseline exactly: `ref-home-sky` passed with no entries, and `colorball` failed at its known check with only its known exception, in 19 s instead of 63 s. The real profile was unchanged by every suite, and the booster suites restored their tracked evidence byte-identical.
  - **Levels 1-100:** **100/100**. No batch had a problem, the console had no entries, and the real profile was unchanged.
  - **Layout at four sizes:** **40 of 40** screens at 1080x1920, 1080x2400, 1536x2048 and 720x1280. The console had no entries, the Game view was restored exactly, and the real profile was unchanged.
  - **Approved changes:** **17/17**, with an empty console and the real profile unchanged.
  - **Shared VFX package:** EditMode 17/17, PlayMode 22/22, 0 compiler messages.
- **Files changed:** 17 texture `.meta` files, `TEXTURE_AUDIT.md`, `FINAL_QA_REPORT.md` (which also gains the files iteration 40
  left out of its "Changed files" list: the texture `.meta` files, the round icon and its checks, and the Round icon slots).
- **Commits:** `cfea8e56` (the 17 texture overrides), and the commit of these records.
- **Still open (a phone, which the user kept out of this pass):** a phone's Back key as Escape; memory, frame time and collection
  pauses on a phone; how launchers render the icons; how the compressed art looks on a phone's screen.
- **Next task:** none the loop can act on in this pass.

### Iteration 42 (iteration 41's open parts that need no phone)
- **Date/time:** 2026-09-26 12:53-13:07 local
- **Why:** the user asked for the plan to continue. Two of iteration 41's open parts needed no phone: the cause of the editor
  stall in its `colorball` suite, and the in-context comparison of the 16 screens whose "before" captures had been overwritten.
- **The stall's cause (found):** Windows installed a Microsoft Defender security intelligence update in exactly that window.
  Windows Update started it at 12:04:08 and finished at 12:04:17; Defender reported the new version at 12:04:17, a
  configuration change at 12:04:18 and its health report at 12:04:34 (Windows' System, Application and Defender event logs).
  Unity's own logs are silent then: the licensing client's last request was at 12:03:39 and the Hub's next entry at 12:05:36.
  The scene load that ended the stall spent 1.2 s deserializing (0.14 s normally). An operating-system event, not the game.
  - **Correction to iteration 41 (its record and commit `e1c1a0bb` stay as written):** its suites line calls the stall "the
    known licensing stalls". The licensing client was idle; the cause was the Defender update.
  - The other extra entry, the "Account API did not become accessible" warning, comes from the AI Assistant package, which has
    had no account since the editor was relaunched without the Hub in iteration 38. It is in this editor session's log 6 times;
    this one came in the first Play session after the clean rebuild's domain reload.
- **The other 16 screens (in context):** all 40 compared (`kv/shot_compare42.py`, sheets in `kv/play/res_compare42/`). "Before" was captured at 12:56-13:06 with the 17 textures' pre-change `.meta` files written back and reimported (uncompressed again: RGBA32 x15, RGB24 x2); "after" is iteration 41's layout run. Where the 17 are drawn, the difference stays small at every size: the Settings gear icon by 1.29-1.34/255 on average (114x111 px at 720x1280, 170x167 px at the other sizes; 0.8-0.9% of its pixels by more than 8), the fail cross by 1.07-1.44/255 (27x27 to 58x57 px; 1.8-2.9% by more than 8). Every other difference is something that moves between any two runs: the random level-12 board, drifting clouds, the King, the drop arrows, the pre-level popup's pulsing buttons, and the QA test profile's coin and star counts. How-to-play over the pause menu is pixel-identical at all four sizes.
- **Tests:** the in-context capture ran the layout harness once more in the pre-change state: 40 of 40 screens pass, the Game view was restored exactly and the real profile was unchanged.
- **Files changed:** these records only. The 17 texture `.meta` files were written to their pre-change versions for the
  capture and restored from HEAD afterwards: all 17 match HEAD again and import as ASTC 4x4 (17 of 17, 0 errors); the editor ended stopped, with the real profile unchanged.
- **Commits:** the commit of these records.
- **Still open (a phone, which the user kept out of this pass):** a phone's Back key as Escape; memory, frame time and collection
  pauses on a phone; how launchers render the icons; how the compressed art looks on a phone's screen.
- **Next task:** none the loop can act on in this pass.

### Iteration 43 (the device checks as far as they go without a phone; two decisions)
- **Date/time:** 2026-09-26 13:10-13:26 local
- **Why:** the user asked again for the blocker to be solved. What was left looked phone-only, but three items had parts that
  are not.
- **A phone's Back key (narrowed as far as the sources go):**
  - The project builds with GameActivity (`androidApplicationEntry: 2`). Its input code, which Unity ships as source
    (`PlaybackEngines/AndroidPlayer/Source/GameActivity/UGAInput.cpp`), hands every key event to the engine with its raw
    Android key code, `AKEYCODE_BACK` included; it treats Back like Escape only to hide the soft keyboard.
  - Unity 6.5's manual (`Input.backButtonLeavesApp`) says the Back button arrives as `KeyCode.Escape`, but for the legacy
    Input Manager. This project runs the Input System only (`activeInputHandler: 1`), whose Cancel action is bound to
    `<Keyboard>/escape`.
  - The Input System 1.20 package's source, documentation and changelog say nothing about Android's Back key; the engine
    maps it in closed native code. Only a phone can confirm that it reaches `<Keyboard>/escape`.
- **How launchers render the icons (measured; one correction):**
  - Iteration 40 said the crown lies "inside the 66.7% safe zone". 66.7% is the 72 dp viewport, not the safe zone; Android's
    safe zone is a 66 dp circle (61.1% of the 108 dp layer). Measured now: the farthest visible pixel is 35.92 dp from the
    centre, and 1.3% of the crown's ink lies outside the 66 dp circle. The circle, squircle, rounded-square and teardrop
    masks cut none of it (`scratchpad/icon_masks41.png`).
  - `RoyalAppIconBuilder` had written "fits within 62.5% safe square" as fixed text since 2026-09-14. It now measures the
    foreground against both zones, writes the figures to `checks.txt` and fails if any pixel reaches past 36 dp, where a
    circular mask cuts (`2390d246`). Re-running it left every icon PNG and `ProjectSettings.asset` byte-identical; the three
    icon `.meta` files it re-saved with trailing spaces only were restored.
  - Not changed: shrinking the crown by about 8% would bring it inside the safe circle, but it is the logo (protected art)
    and no common mask cuts it. The owner's call.
- **Decisions:**
  - `gold.png` stays uncompressed. ASTC needs its mipmaps off, a change for every platform beyond the approved Android
    override, for about 90 KB; only the hidden legacy map uses it.
  - R-22's `FitDialog` stays as it is (iteration 41: its latent limit cannot take effect).
- **Tests:** a clean rebuild of all 165 assemblies after the change (13:23-13:25): 0 errors, 0 warnings. The builder's run
  verified its 18 slots and produced byte-identical icons. Only this editor tool changed, no runtime code, so the suites
  were not re-run.
- **Files changed:** `RoyalAppIconBuilder.cs`, `Design/Branding/AppIcon-v1/checks.txt`, these records.
- **Commits:** `2390d246` (the builder), and the commit of these records.
- **Still open (a phone, which the user kept out of this pass):** a phone's Back key as Escape; memory, frame time and collection
  pauses on a phone; launchers other than the four common mask shapes; how the compressed art looks on a phone's screen.
- **Next task:** none the loop can act on in this pass.

### Iteration 44 (the crown's margin decided; the report's stale statements)
- **Date/time:** 2026-09-26 13:28-13:34 local
- **Why:** the user asked again for the blocker to be solved. Iteration 43 had left the crown's safe-zone margin as "the owner's
  call", and `FINAL_QA_REPORT.md` still called items open that were decided long ago.
- **The crown's safe-zone margin (decided: no change):** none of the four common mask shapes cuts it, and the logo is protected
  art, which gets integration fixes for defects only; 1.3% of its ink past Android's guideline, with nothing clipped, is not a
  defect. `RoyalAppIconBuilder` now fails if the crown ever reaches a circular mask's edge. Scaling the adaptive foreground
  by 0.919 (33 / 35.92 dp) would bring it inside the safe circle, if the owner wants that.
- **Four stale statements in `FINAL_QA_REPORT.md`, corrected:**
  - the open-items header said items 1 and 2 were decided and "the others are still open": all but item 12 were decided by the
    user on 2026-09-25 and 2026-09-26, and the decided changes are done;
  - "Navigation and Back" said Android Back closes dialogs: R-03, R-10 and R-15 were verified with Escape in the editor, and
    whether a phone's Back key arrives as Escape is item 12;
  - the incremental-GC line sent its phone measurement to item 10, which itself defers it to item 12;
  - the WebP line sent the texture saving to "Open items" in general: it is item 9, done.
- **Tests:** none; only these records changed.
- **Files changed:** `RALPH_STATE.md`, `FINAL_QA_REPORT.md`.
- **Commits:** the commit of these records.
- **Still open (a phone, which the user kept out of this pass):** a phone's Back key as Escape; memory, frame time and collection
  pauses on a phone; launchers other than the four common mask shapes; how the compressed art looks on a phone's screen.
- **Next task:** none the loop can act on in this pass.

### Iteration 45 (a runbook for the phone checks; nothing run)
- **Date/time:** 2026-09-26 13:37-13:39 local
- **Why:** the user asked for the plan to continue. Everything left needs a phone, which this pass excludes (kept by the user on
  2026-09-26), so no APK was built and nothing ran on Android. These steps let the pass that allows a phone run the four checks
  at once.
- **Build and install (the development build, so the QA tools are in it):**
  - With the editor stopped, `RoyalThrone.Editor.RoyalAndroidBuild.BuildAndroid()` (Development, LZ4) writes
    `Builds/Android/RoyalThrone-Test.apk`; poll `Builds/Android/last-build.log` until `STATUS succeeded`. It turns
    `EditorUserBuildSettings.development` on and script debugging and profiler autoconnect off, and (unlike the Levels 01-50
    test build) does not restore them: note their values first and put them back afterwards.
  - adb is `<Unity 6000.5.10f1>/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb.exe`. First `adb devices`,
    then a screenshot (`adb exec-out screencap -p > s.png`): if someone is playing on the phone, observe only. In Git Bash,
    prefix adb calls that take device paths with `MSYS_NO_PATHCONV=1`.
  - `adb install -r` the APK (package `com.codexsandbox.jellygarden`, arm64, minSdk 26), then start it with
    `adb shell monkey -p com.codexsandbox.jellygarden -c android.intent.category.LAUNCHER 1`.
- **1. The Back key as Escape (R-03, R-10, R-15):** on home, open Settings, then `adb shell input keyevent KEYCODE_BACK`: the
  dialog must close and the app stay (`adb shell pidof com.codexsandbox.jellygarden`); a second Back on the bare home quits. In a
  level: Back pauses, Back again resumes; HELP in the pause menu, then Back, returns to the pause menu. A screenshot before and
  after each Back.
- **2. Memory, frame time, collection pauses:** `adb shell dumpsys meminfo com.codexsandbox.jellygarden` on home and in level 12
  (TOTAL PSS and Graphics). The Unity Profiler connected to the development build over ADB, 60 s on home and 60 s in a level:
  CPU frame time (median and 99th percentile) and the frames that hold a garbage collection.
- **3. The launcher icon:** `adb shell input keyevent KEYCODE_HOME`, open the app list, screenshot: the crown must be whole. Note
  the launcher's mask shape; `Design/Branding/AppIcon-v1/checks.txt` has the crown's measured margins.
- **4. The compressed art on screen:** screenshots of the title screen (logo, crown, castle), home Settings (the gear icon) and
  the fail screen (the crosses), looked at 1:1 beside the editor's captures of the same screens.
- **Afterwards:** put the three build settings back, and record the results here and in `FINAL_QA_REPORT.md` item 12.
- **Tests:** none; only these records changed.
- **Files changed:** `RALPH_STATE.md`, `FINAL_QA_REPORT.md`.
- **Commits:** the commit of these records.
- **Still open (a phone, which the user kept out of this pass):** the four checks above.
- **Next task:** none the loop can act on in this pass.

### Iteration 46 (shown, not told; R-29 fixed; the other copies of this session)
- **Date/time:** 2026-09-26 13:45-18:09 local
- **Why:** the user asked to be shown, not told: every command, what it reported, and every part never executed. Then, several
  times, for a review: re-read every change since the last verified checkpoint, run the full suite, lint and type check, and
  fix every failure.
- **The log:** every tool call of this copy since the session resumed at 11:35, with its command and what it reported, is in
  `scratchpad/command_log_session.md`, one line per call in `scratchpad/command_digest.md` (571 calls up to these
  records). It follows this copy's own message chain (`scratchpad/cmdlog_chain.py`), because other copies of the session write
  to the same transcript.
- **Other copies of this session were running.** The session was also resumed at 14:37 and at 17:18 (both rooted at `outputs`,
  sharing this copy's scratchpad) and at 15:08 (rooted at `JellyGarden-Codex`); earlier review copies had written notes on
  2026-09-25 19:15 and 2026-09-26 00:54. They could only read files and write memory and scratchpad notes: every shell command
  they tried (git, unity, python), their `exit-play` request to the editor (14:38) and every write inside the project were
  refused. So none of them changed the project, ran Unity or used git. Their 112 calls in this window are not in the
  log. They left three findings, checked below, and a loop-state memory note (14:40) that was stale within the hour: it said
  `final_run9` had stopped mid-layout and left the editor in Play; the run finished (layout 40 of 40), and that Play session
  was its own. The 17:18 copy took itself for this one: it wrote `scratchpad/apply_iter46.py` for "a session allowed to run
  git", rewrote the loop-state memory note and added a line to these records' script saying this copy had lost those
  permissions. It had not: this copy read `final_run12` itself and made the commits.
- **R-29, found by a review copy: reproduced, fixed.**
  - Found by reading: on 2026-09-25 at 10:50, before `e6c639b4`, a review copy noted that one Back closed every stacked popup
    at once; at 19:15 it noted how `e6c639b4` changed that. Both notes stayed in that copy's scratchpad until now.
  - Reproduced (`kv/r29_repro.py before 3`, QA profile, a real Escape press and release): in all 15 stacked runs (the booster
    guide, the lives shop, the coin shop over the lives shop, the booster shop and the coin shop over the booster shop, each over
    the level card, three rounds), Back closed the card underneath and left the top popup open over home; the 3 card-alone
    controls closed the card. No quit request, and the real profile was unchanged.
  - Cause: each popup reads Back in its own `AnimationManager.Update`. Iteration 37's `ConsumedThisFrame` return
    (`e6c639b4`) made one Back one action, but the action went to whichever popup's `Update` ran first, which in the editor was
    always the card. Before `e6c639b4`, one Back closed both. **Correction to iteration 37:** its "whichever menu's `Update`
    runs first" described this defect as intended.
  - Fix: a popup covered by a later open sibling with an `AnimationManager` ignores Back. The `ConsumedThisFrame` return stays:
    when the top popup closes first, the one under it is uncovered in the same frame. Measured in Play first: `CanvasGlobal` is
    one overlay canvas (sorting order 30) whose 13 children are exactly the 13 popups, and none has a canvas of its own, so
    sibling order is draw order. A popup that ignores Back never covers one that answers it: the pause, win, fail and
    out-of-moves cards open only in a level, above no popup that answers Back, and the rate card is off in the offline build.
    With any popup open, home never turns an unanswered Back into a quit (`BlocksBoardInput`).
  - After (`kv/r29_repro.py after 3`): 18 of 18: each Back closed only the top popup and left the ones under it open. No quit
    request, and the real profile was unchanged.
  - The other Back paths (`ralph_run.py r03 r03d r10`): Back closed the home profile dialog, and separately the level card,
    without a quit request; on the bare home it requested a quit, as designed (the editor ignores `Application.Quit`); in level
    12 it opened the pause menu, and on the pause menu it resumed; it cancelled a chosen tool and closed the tool shop without
    pausing. The console had no entries.
- **Checked and found wrong: "the round-2 decisions were never answered".** Two review copies wrote that the second-round
  question (2026-09-25 16:11) came back without an answer, and prepared corrections to iteration 38's decisions. The transcript
  shows the answer arriving at 18:48: "Fit the dialog on 4:3", "Coins everywhere" and "Yes, restart it", and "continue" for
  the 2026-09-23 changes. The loop's work on those changes, commits included, follows that answer in the transcript; those
  copies' part of it holds only an earlier "interrupted" result of the same call. Iteration 38's record is right and nothing
  was changed. The corrections they prepared (their `iteration40_corrections.md`, section 1) must not be applied.
- **Correction (the round launcher icon, iteration 40): no player sees it.** Unity uses the Round icon only on Android 7.1
  (API 25), as its own slot description says; the game requires Android 8.0 (`AndroidMinSdkVersion: 26`); and on Android 8.0
  and later Unity's own Android template points the round icon at the adaptive layers (`mipmap-anydpi-v26/app_icon_round.xml`).
  It stays: it does no harm, and removing it changes the APK's icon resources, which only a device build could check.
- **In context, 15 of the 17 textures, never compared until now (correction to iterations 41 and 42):** their layout screens
  show only the Settings gear icon and the fail cross, so "where the 17 are drawn" was true of those two only. Done now, each
  "before" captured with the 17 textures' pre-change imports, restored afterwards (ASTC 4x4 again, 17 of 17, matching HEAD):
  - the title screen (`kv/before_after42.py`): the blue castle's area differs by 1.88/255 on average (3.4% of its pixels by
    more than 8), the logo's lettering by 1.37 (0.27%), the PLAY button and its crown by 0.92 (0.11%). The halo's rays differ
    by 6.2 because the halo turns between captures; its texture decodes at 61.9 dB.
  - the home dialogs' header icons, in boxes that include some dialog background: star 0.30/255, chest 0.84, trophy 0.64, team
    0.66, cards 0.79, settings gear 1.30, and the heart in the lives dialog 0.25 (`kv/before_after42b.py`); no box has more than
    0.9% of its pixels changed by more than 8.
  - level 1's tutorial finger, frozen at the screen centre (its animation only moves it): 0.49/255 on its pixels, 0.39% by more
    than 8.
  - the win and shuffle banners, which play a short animation and hide themselves at its end (so single shots missed them): shown
    by the game's own `MenuManager` calls in level 12, with the clock fixed at 1/30 s a frame and every frame captured
    (`kv/before_after42c.py`); on phase-matched frames (the second run was exactly one frame ahead), 1.04/255 and 0.70/255 on
    their own pixels, 0.61% and 0.02% by more than 8.
  - Never drawn, so nothing to compare: `home-castle` (`RoyalHomeScreen.ApplyCampaignArtwork` swaps in the campaign palace art at
    runtime) and `fb_logout-button`. The home bar's heart, star and gear are other sprites: pixel-identical before and after.
  - Everything else that differed on those screens moves between runs: the King, clouds, the tutorial hand, the matching-hint
    highlight, the drop arrows.
- **Re-read, and a defect in iteration 43's change fixed (`37ddd33c`):** `RoyalAppIconBuilder.SafeZone` used the image width for
  both axes (a taller foreground was measured only in part, a wider one threw an index error) and divided by zero for a fully
  transparent one. It now refuses a non-square or empty foreground and releases its texture on every path. Its result is
  unchanged: the same figures, and `checks.txt`, every icon PNG and `ProjectSettings.asset` match HEAD.
- **Re-read again, and three more defects fixed (`e37b015b`):** neither reader checked `LoadImage`'s result, so an unreadable
  foreground left the 2x2 placeholder texture, which `SafeZone` would have measured and `WriteRoundIcon` would have written to
  disk as the round icon; and `Apply` measured the safe zone last, after writing the icons and assigning the slots, so a failing
  foreground left the project half-updated. `Apply` now measures first, both readers throw on an unreadable file, and
  `WriteRoundIcon` releases its textures on every path. Tested directly on scratch files (`kv/icon_edge_tests.cs`): an
  unreadable, a non-square (100x50) and an empty image each fail with their own message, `WriteRoundIcon` writes nothing for the
  unreadable one, and the real foreground gives the same figures. A full builder run left `checks.txt`, every icon PNG and
  `ProjectSettings.asset` identical to HEAD.
- **Tests:** four full runs (`kv/final_run9.sh` to `final_run12.sh`), on `dc7bbb76` (before the fixes), `37ddd33c`,
  `e37b015b` and `5cee04fe` (R-29):
  - **Type check:** each run starts with a clean rebuild of all 165 assemblies with every compiler message captured: 0 errors, 0
    warnings and no other message in each of the four runs (the last took 71 s).
  - **Lint:** Unity's serialization analyzer reports through the compiler, so the same rebuilds are the lint: no warning or
    info message of any kind.
  - **The 23 suites:** in each run 14 pass and the 9 decided failures fail (6 from the Royal Match imports, 3 superseded Color
    Ball suites: items 1 and 2), with the same 11 failure lines as iteration 33. The console held iteration 33's 13 entries plus
    the one below; the real profile never changed, and the evidence was restored byte-identical 3 of 3 times.
  - **Levels 1-100:** no problem in any level, in each run, and the real profile unchanged. **Layout at four sizes:** 40 of 40
    screens each time, the Game view restored exactly. **The approved changes:** 17 of 17 each time. **The shared VFX
    kit:** EditMode and PlayMode exit 0 in each run; the last run's result files: 17 of 17 and 22 of 22.
  - **R-29 on the final code** (in `final_run12`): 18 of 18, only the top popup closed; no quit request, no console entry, the
    real profile unchanged.
  - **The console's extra entry:** the AI Assistant package's "Account API did not become accessible" warning, in the first
    Play session after each run's clean rebuild (a domain reload). It comes from that package having no account since the editor
    was relaunched without the Hub in iteration 38. Removing it means removing the package, which the user decided to keep, or
    relaunching the editor through the Hub; both are the user's call, so it is recorded, not worked around.
- **Files changed:** `RoyalAppIconBuilder.cs`, `AnimationManager.cs`, these records.
- **Commits:** `37ddd33c` and `e37b015b` (the builder's fixes), `5cee04fe` (R-29), and the commit of these records.
- **Still open (a phone, which the user kept out of this pass):** a phone's Back key as Escape; memory, frame time and collection
  pauses on a phone; launchers other than the four common mask shapes; how the compressed art looks on a phone's screen.
- **Next task:** none the loop can act on in this pass.

### Iteration 47 (the other Back handlers, after R-29; no change)
- **Date/time:** 2026-09-26 18:17-18:20 local
- **Why:** R-29 showed that the loop's Back tests had stacked one pair of popups only. Before the loop ends, every other place
  where one Back could reach two handlers was checked. The user asked to continue with the plan.
- **The handlers:** `RoyalHomeScreen` closes its dialog on the press (in `LateUpdate`); `AnimationManager` closes the top popup
  on the release; in a level, `ReferenceBoosterBar` closes its shop or cancels a chosen tool on the release; `LevelManager`
  pauses (in a level) or quits (on the bare home) on the release, only if nothing consumed the gesture.
- **A home dialog and a popup are never open together.** Opening a level from a dialog closes the dialog (`OpenLevel`). While a
  popup is open, the home controls and the tab bar take no taps (`controls` and `navigationInput` are not interactable), so no
  dialog opens over or under one. The level card that opens without a tap, after CONTINUE on the win card, finds no dialog
  either: leaving home for the level closed it.
- **In a level the order is fixed.** `LevelManager` has execution order 1000; every popup and the tool bar have 0 (the tool bar's
  meta sets none). They answer the release first and consume it, so the pause branch sees it consumed: a Back that closes a
  popup or the tool shop, or cancels a tool, never also pauses. Iteration 46's `r10` run showed the tool cases.
- **The out-of-moves offer:** only the coin shop opened over it answers Back. The offer has no Back branch, and the game is not
  in `Playing` there, so nothing pauses.
- **Result:** no other case of one Back doing two things. Nothing changed in the code.
- **Tests:** none run. This is a reading of the code; iteration 46's runs cover the cases that exist (the stacks, the home
  dialog, the level card, the bare home, pause, a chosen tool and the tool shop).
- **Files changed:** `RALPH_STATE.md`, `FINAL_QA_REPORT.md`.
- **Commits:** the commit of these records.
- **Still open (a phone, which the user kept out of this pass):** the four phone checks of iteration 45.
- **Next task:** none the loop can act on in this pass.

### Iteration 48 (the open calls decided; the Back key traced into Unity's player; FitDialog's 60% minimum fixed)
- **Date/time:** 2026-09-26 18:25-19:34 local
- **Why:** the user said that stopping was a failure: state the blocker, decide what can be decided, find what is missing, and
  keep going. The blocker: the four phone checks need Android, which the user's rule keeps out of this pass. The other items
  had been left as the user's call.
- **Decided by the loop:**
  - the round launcher icon stays: no player sees it, and removing it changes the APK's icon resources, which only a device
    build could check;
  - no push: the branch holds the Royal Match import (`cdd68361`), and a push needs the user's explicit go-ahead;
  - the AI Assistant package's console warning stays: removing the package would undo the user's decision, and relaunching the
    shared editor through the Hub would interrupt its other users over a warning;
  - the 9 failing suites follow the user's decisions on the Royal Match art and the superseded Color Ball suites (items 1, 2).
- **Found: how a phone's Back reaches the game on Android 13 and later.** The project has predictive back on
  (`androidPredictiveBackSupport: 1`; Unity's tooltip: "If set, Android's OnBackInvokedCallback will be used to handle back
  events on Android 13 and above"). Unity's player (`classes.jar`, disassembled with the JDK that ships with the editor)
  registers that callback in `UnityPlayer.initialize`, and its `Runnable` (`com.unity3d.player.m0`) turns each back event into a
  `KEYCODE_BACK` press and release (`KeyEvent` down, then up a millisecond later, from a keyboard source) sent through
  `Activity.dispatchKeyEvent`. So with predictive back on, Back still arrives as the Back key, as on older Android. The phone
  check is left with the native step that turns `KEYCODE_BACK` into the Input System's Escape key: the Input System's managed
  Android code maps only gamepad keys, and its `Keyboard` layout gives Escape the `Back` usage.
- **Fixed: FitDialog's 60% minimum (R-22, latent).**
  - Reproduced (`kv/fit_forced.py pre47`, QA profile): the Settings dialog, given a 3000-unit design height, went to 1800 (60%),
    then 1080, then 931.8 (31%, what fits) and stayed there. The minimum was taken from the dialog's current height, the one
    FitDialog had set the frame before. **Correction to iterations 41 and 43:** they called this a collapse; the dialog shrinks
    to whatever fits, and to nothing only when nothing fits.
  - Fix (`ReferenceHomeLayout.FitDialog`): it remembers the layout's height (any height it did not set) and the one it set,
    takes the 60% minimum and the upper limit from the layout's height, and skips the write when the dialog already sits there.
  - After (`kv/fit_forced.py post47`): 1800 (60%) in all 30 frames. Reopening restores the normal 937.7, before and after.
  - Nothing moves on the screens that exist (`kv/r22_measure.py` at four sizes before and after the fix, `kv/r22_compare.py`):
    every dialog edge, button, row, grid, title and copy is identical. Only the close button's top differs, by up to 0.2 units,
    as it does between any two runs (iteration 38's run against today's, both before the fix).
- **Tests:** the full run on the fix (`kv/final_run13.sh`, 18:36-19:33): a clean rebuild of all 165 assemblies with 0 messages of
  any kind (the type check, and the lint, since Unity's serialization analyzer reports through the compiler); the 23 suites, 14
  passing and the 9 decided failures failing with iteration 33's 11 failure lines, the console holding iteration 33's 13 entries
  plus the AI Assistant account warning, the real profile never changing and the evidence restored byte-identical 3 of 3 times;
  Levels 1-100 with no problem; layout 40 of 40 at four sizes, the Game view restored exactly; the approved changes 17 of 17;
  R-29 18 of 18; the forced fit at 1800 in all 30 frames and 937.7 on reopening; the VFX kit 17 of 17 and 22 of 22.
- **Files changed:** `ReferenceHomeLayout.cs`, these records.
- **Commits:** `419a51e2` (the fix), and the commit of these records.
- **Still open (a phone, which the user kept out of this pass):** the four phone checks of iteration 45; for the Back key, only
  the native step above.
- **Next task:** none the loop can act on in this pass.
