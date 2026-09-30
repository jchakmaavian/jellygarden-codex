# Reference baseline pass — 23 September 2026

Task: "Restore Reference Game — Import, Implement and Build". This folder holds the inspection,
verification and comparison evidence for that pass. The build and device sections are at the end.

## What the reference actually is

`Design/Backup/apk/Royal_Throne-rf.apk` is **Royal Match by Dream Games** (`com.dreamgames.royalmatch`,
versionCode 32984, Unity 2021.3.55f1 IL2CPP, built 2025-12-22), not an earlier build of this team's game.
The Android label inside the package is "Royal Match"; only the filename says Royal Throne.

Because it is a third-party commercial game, **none of its art, animation, VFX, SFX, UI, splash/logo,
characters, shaders or story content was imported into `Assets/`**, and none may be. Copying them
into Royal Throne would reproduce Dream Games' copyrighted and trademarked work in a product this
team intends to ship. This matches the project's own standing rule (`SourceReference/CLAUDE.md`:
"Do not use Royal Match as an asset-copy target … All final game art must remain original").
The APK was used read-only, as behavioural and structural reference, exactly as the previous
levels inspection did.

What *was* done instead:

| Requested | Done |
| --- | --- |
| Backup | `Design/Backup/RoyalThrone-before-ReferenceBaseline-2026-09-23_1748/` (verified, see its README) |
| Inspect APK startup and gameplay | `REFERENCE-INVENTORY.md` + 19 CSVs (read-only inventory); `device/royalmatch-38174-coldstart/` (cold start of the installed Royal Match, 3 runs, video + screenshots + timeline) |
| Copy/import reference content | **Declined** — see above. Nothing written under `Assets/` from the APK. |
| Restore splash and loading | Verified intact and working (nothing had been removed); see "Startup order" below |
| Implement reference behaviour | Existing systems verified: boosters, combinations, settings, transitions, Levels 01–50 checks. No gameplay retuning was done; differences are listed, not disguised |
| Verify and build | Editor cold-start walk-through, booster checks, 50-level campaign checks, APK build and device test (sections below) |

## Startup order — verified

### Royal Throne (this project, editor Play Mode, `main.unity`)

Captured at about 10 fps with `ScreenCapture` (`startup-editor/RoyalThrone-editor-coldstart-720x1280.mp4`,
re-timed to a constant 30 fps from the ~10 fps source frames, so it is not a genuine 30 fps recording;
`coldstart-timeline-hook-relative.tsv` has the raw per-frame states; add 1.18 s to get time since Play
Mode start).

1. Unity engine splash (`m_ShowUnitySplashScreen: 1`; not shown inside the editor).
2. Title: Royal Throne logo, castle, rotating halo, **PLAY** button, 0.55 s entrance fade.
3. PLAY tapped (scripted at 3.7 s) → 0.25 s cross-fade to the loading card: three crowns, "Your kingdom awaits",
   sapphire progress bar bound to `SceneManager.LoadSceneAsync` progress, status text
   "Opening the gates…" → "Preparing your next challenge…" → "Welcome to your kingdom". Minimum 0.9 s,
   no fake delay. The real load took ~1.8 s in the editor (one 1.5 s main-thread hitch while `game.unity` loads).
4. Game scene activates under the overlay; 0.3 s fade of the whole overlay; home (castle, resources,
   Level 23 entry, Area 4 chest, five-tab navigation). Zero console errors during the walk-through.

Nothing had been removed by the earlier pass: `main.unity` is unchanged since 13 September, git history
shows no deleted scenes or menu textures, and `RoyalTitleScreen.cs` is untouched.

### Royal Match (installed build 38174 on the Infinix X665B, no input sent)

From `device/royalmatch-38174-coldstart/royalmatch-coldstart-notes.md` (three cold starts, identical order):

1. Android launch animation (~1.7 s).
2. Static solid-blue **dream** logo splash, ~7.2 s, no indicator.
3. Hard cut to a static key-art **Loading** screen (castle, King, Butler, dog, "ROYAL MATCH!" logo,
   plain "Loading" label), ~10–12 s, no bar/percentage/spinner/tips.
4. ~0.1–0.2 s cross-fade to Home with the full HUD; home reached at ~19–24 s. No tap required, no popups.

Installed build 38174 is newer than the 32984 APK on disk; the APK itself was not installed (it would be
a downgrade of the user's Play Store copy and was not needed for the observation).

### Differences (reported, not changed)

| Aspect | Royal Throne | Royal Match 38174 | Note |
| --- | --- | --- | --- |
| Gate before loading | PLAY tap required | none (auto-advance) | Product decision; a one-line change in `RoyalTitleScreen` could auto-call `BeginPlay()` after the entrance. Not applied. |
| Loading indicator | real progress bar + status text | static label, no indicator | Ours already meets "loading must reflect actual initialization". |
| Time to home | ~5.8 s from Play Mode start, of which 3.7 s idle on the title | ~19–24 s | The reference spends most of it on network config/bundle fetch (inventory §4–5). |
| Publisher splash | Unity engine splash only | Dream Games logo | Royal Match's logos are trademarks and were not reproduced. |

`Startup-Comparison-RoyalThrone-vs-RoyalMatch-30s.mp4` shows both side by side, aligned at launch,
with elapsed-time burn-ins. Left = our editor capture (black lead of 1.18 s before the capture hook
attached), right = the device recording scaled to the same height. No speed, zoom or exposure changes.
The device recording has no audio; the editor capture has none either.

## Gameplay verification (editor, isolated QA profile)

All runs used `RoyalQaService.StartSession()` (saves redirected to `RoyalThrone.QA.v1/` keys) and ended
with `EndSession()`; real prefs were checked before and after (`OpenLevel=23 Music=1 Sound=1
HomeMotion=1 ShowHints=1 ModMenu=1 Gems=3 Lifes=5`, unchanged).

- Settings dialog: Music / Sound / Motion / Hints toggles plus the Mod Menu entry
  (`startup-editor/stills/06-settings-dialog.png`).
- Boosters on the disposable 8×8 practice board (`boosters-editor/`): TNT, rocket H, propeller and
  color ball each spawned and activated; combinations TNT+rocket, color ball+TNT, propeller+rocket,
  color ball+rocket. Every command completed, the board settled back to 64/64 filled cells, and moves
  decremented by exactly one per action. The only console error was a pre-existing `TutorialManager`
  hand-animation `MissingReferenceException` raised when the practice board replaced level 1's items
  mid-tutorial (a QA fixture artefact, not a player path).
- Levels 01–50: `RoyalCampaignChecks.RunAll()` plus a rerun of the failed batch and the final batch —
  results in `Design/Inspection-Levels01-50/Validation/`. Data checks: 294 PASS (all 50 levels).
  Open-check batches: **01–10, 11–20, 21–30 and 41–50 PASS** (each level opens, settles, matches its
  authored layout/goals, performs a real move, and each batch exercises fail/retry/win/save/unlock).
  **Batch 31–40 FAILS at level 40**, reproducibly (three runs): "production celebration reaches win screen"
  never becomes true. Root cause, observed live: level 40 is the coin bonus board (70 coins + 2 color
  balls). The legacy `LevelManager.PreWinAnimationsCor` loop destroys leftover power-ups until none
  remain; here each color ball clears the whole coin board, the campaign refill spawns new coins and
  cascades create new specials (`PACKAGE`, `HORIZONTAL_STRIPPED` appeared within 10 s), so the
  "no extra items left" condition is never met and the celebration loops indefinitely. Level 20, the
  other bonus board, carries TNTs instead and passed. This is confined to the QA-only reference
  campaign (the earlier pass's adaptation); the shipped levels are unaffected. Not fixed in this pass:
  the fix belongs in the campaign's bonus-level end rule (for example stop campaign spawn feeding and
  skip the leftover-special loop once `PreWinAnimations` starts on a bonus level), which is a design call.

## Review of the earlier pass (uncommitted working tree)

An independent read-only review of the 79 uncommitted changes, each finding adversarially verified by two
further readers. Confirmed items and what was done:

| # | Severity | Finding | Action |
| --- | --- | --- | --- |
| F1 | medium | `RoyalHomeScreen.UpdateNavigation` threw `IndexOutOfRangeException` on every domain reload / Play entry (editor only; `[ExecuteAlways]` plus domain-reload-disabled resurrects null arrays as empty). Pre-existing at HEAD. | **Fixed**: the guard now checks all four tab arrays against `tabButtons.Length` (one line, `RoyalHomeScreen.cs:423`). |
| F2 | medium | `RoyalOfflineBuildPolicy.ServiceDefines` listed `SENTIS_ANALYTICS_ENABLED`, an editor-analytics define the AI Inference package re-adds, so builds not started from the two Royal Throne menu items would throw. | **Fixed**: removed from the list (`RoyalOfflineBuildPolicy.cs`). |
| F3 | low | Ungated: every level's gem/coin sprites now come from `Resources/RoyalCampaignArt` (generated art) when present. | Left as the earlier pass designed it; product decision. |
| F4 | low | Ungated home-screen art/copy: palace, King avatar, coin, Home-tab crown, dog/steward portraits, rosette, "King Vanguard". | Left; product decision. |
| F5 | low | Ungated recolours: navigation bar, Play button green→teal, `RoyalMenuSkin` buttons blue→teal. | Left; the exact HEAD values are the `-` side of `git diff HEAD` for four lines. |
| F6 | low | Offline mode makes the gem-shop Buy button a silent no-op. | Left; cosmetic. |
| F9 | low | `RoyalOfflineAndroidManifest` strips INTERNET from development builds too, so Unity Profiler/Console attach over adb will not work. | Left (intended offline policy); noted. |
| F10 | low | `RoyalCampaignBlockers.IsSpecialItem` does a `GetComponent` per cell in match/hint scans on all levels. | Left; perf-only. The fix is prefixing `RoyalReferenceCampaign.Active &&`. |
| F7 | — | Refuted (the code is not compiled). F8/F11: disputed hygiene items (stray untracked recovery scene and preview mp4 under Assets; `RequireCopy` path guard). | No action. |

Non-issues checked and cleared: the `InitScript` offline early return skips only ad/social init;
`config.json` → `{}` has no runtime reader; all reference-campaign hooks are no-ops unless the campaign is
selected through the QA menu; no `[RuntimeInitializeOnLoadMethod]` touches PlayerPrefs; the
`ROYAL_REFERENCE_CAMPAIGN` bootstrap is not compiled into normal builds.

## Build

`Design/Backup/apk/RoyalThrone-ReferenceBaseline-2026-09-23_1840.apk` (248,672,847 bytes), built with
Unity 6000.5.10f1 through `RoyalAndroidBuild.BuildAndroid()` (development build, Mod Menu included),
3 min 43 s, 0 compile errors. Byte-identical copy of `Builds/Android/RoyalThrone-Test.apk`; the previous
build in that slot was already preserved as `RoyalThrone-Dev-2026-09-23_1328.apk` (hash-verified before
overwrite). Log and config summary sit next to the APK. `Royal_Throne-rf.apk` and previous builds were
not overwritten. `EditorUserBuildSettings.development` was restored to false afterwards.

The APK contains no reference-game content. It is the current Royal Throne working tree: HEAD b3bb51d,
the earlier pass's uncommitted offline/levels-01-50/campaign-art changes, and this pass's two fixes.
Package `com.codexsandbox.jellygarden`, arm64, portrait, IL2CPP; INTERNET permission removed by the
earlier pass's offline manifest step.

## Device test (Infinix X665B, Android 11, 720×1612)

`device/royalthrone-referencebaseline-coldstart/`. Installed with `adb install -r` over the existing QA
app (same package, saved data kept: the device profile was at level 6, 4 lives). Cold start via
`am force-stop` + `am start`, passive screenshots every ~3 s (screencap latency on this phone), no input:

| Elapsed | State |
| --- | --- |
| 0.4 s – 3.8 s | dark engine background |
| 6.1 s | "Made with Unity" engine splash |
| 8.0 s | Royal Throne title: logo, castle, PLAY (Development Build watermark) — stable through 80 s |

So on device the order is engine splash → title → (PLAY) → loading → home, matching the editor.
The APK launches, no Unity exceptions in logcat during startup.

PLAY → loading → home was **not** captured on device by this pass: when the interactive phase began
(~1 min after the cold-start run) the phone was already inside **level 6** with the move counter
decreasing and ice blockers breaking, i.e. the user had picked the phone up and was playing the new
build. To avoid interfering, no further input was sent (the one tap issued landed on the board frame
while the level was already open). Those frames (`p2-*.png`, `rt-play-to-home.mp4`) still show a real
level running on the device build: HUD, King avatar, ice-block goals, moves, booster bar, matches
resolving. Settings dialog, Mod Menu and booster activation were therefore **not exercised on the
device** in this pass; they were verified in the editor (above). The device recording from the first
cold-start run failed (Git Bash path rewrite of `/sdcard`); the screenshots stand in for it.

## Files in this folder

- `REFERENCE-INVENTORY.md` and `*.csv` — read-only inventory of the reference APK (names, counts, structure).
- `device/royalmatch-38174-coldstart/` — installed Royal Match cold start: video, screenshots, contact sheets, notes.
- `startup-editor/` — our editor cold start: MP4, stills, per-frame timeline, settings dialog.
- `boosters-editor/` — booster/combination frames and the QA command logs.
- `Startup-Comparison-RoyalThrone-vs-RoyalMatch-30s.mp4` — side-by-side startup comparison (internal review only; contains competitor footage, do not redistribute).
