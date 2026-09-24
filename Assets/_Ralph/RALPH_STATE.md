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
