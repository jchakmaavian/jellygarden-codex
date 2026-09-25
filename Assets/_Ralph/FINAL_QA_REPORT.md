# Royal Throne - QA loop final report

The loop ran from 2026-09-24 23:40 to 2026-09-25 08:20 (local, UTC+6) on branch `visual-rebuild-v2`, in the shared Unity 6000.5.10f1
editor. The pre-loop state is preserved on `backup/pre-ralph-2026-09-24` (`d6fe15ce`). `Assets/_Ralph/RALPH_STATE.md` is the
iteration-by-iteration record, with the reproduction, root cause, tests and evidence for every item.

## Scope

- **Done:** a QA and fix loop on Royal Throne's own non-gameplay systems, then verification:
  - layout at four sizes;
  - performance;
  - a texture import audit;
  - compile warnings;
  - the project's own check suites;
  - the shared VFX package's tests;
  - a Levels 1-50 regression;
  - an evidence run for the Royal Match art decision, with the imports switched off.
- **Declined, with the reason:**
  - **Importing or reproducing features or assets from `Royal_Throne-rf.apk`.** It is Dream Games' Royal Match, and
    `SourceReference/CLAUDE.md` makes it a quality reference only.
  - **Brand scrubbing.**
  - **PNG -> WebP conversion.** Unity re-encodes every texture at import, so the source format changes neither build size nor
    memory, and Unity has no native WebP importer. The real texture saving is in the compression settings (see Open items).
- **Protected areas** (Levels 1-50, the match-3 board, level design and balance, home layout, splash, loading screen, logo) were
  not redesigned. "Protected areas" below lists the minimal integration fixes that touched them.

## What changed for players

Each item was reproduced first and fixed with the smallest change. It was then retested in Play Mode, with a regression check and
a clean console. The IDs match the backlog in `RALPH_STATE.md`.

**Navigation and Back**
- **R-01:** CONTINUE after the last level (100) returns home. It used to try to load level 101, throw, and leave the win card stuck.
- **R-03:** Android Back on home closes an open dialog or popup without also quitting the game. It quits only from the bare home
  screen.
- **R-10:** Back in a level resumes from pause, cancels a selected tool and closes the tool shop.
- **R-14:** HOME in the pause menu asks "Leave level? You will lose the life used for this level." before leaving.
- **R-21:** the home profile avatar and the area chest respond to taps again. The home layout had switched off their only tap area.

**Levels**
- **R-04:** the pause button works only while playing, not during the win celebration. The HUD no longer takes taps meant for a
  popup.
- **R-11:** timed levels show "Time 0:56" instead of seconds under "Moves", and the Hammer stays on the booster rail.
- **R-12:** the "out of moves" offer accepts CONTINUE or END LEVEL as soon as the buttons show. It used to ignore taps for about
  2 s.
- **R-13:** a level interrupted by the phone (app sent to the background) comes back in the pause menu.
- **R-09:** leaving level 1 during the hand tutorial and coming back no longer throws.
- **R-02:** a rare endless loop, when a booster needed more plain pieces than the board had, can no longer freeze the game.

**Saving and data**
- **R-05:** a win's stars and best score are written to disk at once. They were lost if the app was killed on the win card.
- **R-06:** saved dates survive a change of device language or region. Before, one could throw on every launch.
- **Review fix:** the rewarded-ad daily timers measure elapsed time correctly. They compared clock components, so 1 h 0 min counted
  as "no minute passed". This is latent, because ads are off in the offline build.

**Shop and copy**
- **R-08:** the offline gem shop shows its packs as "Unavailable" instead of real-money buttons that did nothing.
- **R-17:** the counts read "1 coin", "1 star" and "1 level completed" instead of "1 coins", and the fail card reads "Best score:
  3800". Two debug lines no longer log on every launch.

**Performance**
- **R-24:** about 3.3 KB less garbage per frame from the HUD counters and booster icons in release builds. That means fewer
  collection pauses, which matters because incremental GC is off in this project.
- **R-19:** the King on a hidden screen no longer animates in the background. Home and the HUD hide by fading to
  alpha 0, so one invisible King always kept animating and rebuilding his mesh: 70-79 us per frame in the editor, likely
  more on a phone. He now freezes while his screen is fully transparent and is refreshed on the frame it shows again. The
  change is in the untracked King Venguard work (see Risks).

**Engineering, no visible change**
- **R-18 and the reviews:** the project compiles with **0 warnings** (was 69). Obsolete Unity APIs were moved to their current
  forms, dead code was removed, and the vendored Spine runtime got a second guarded patch set.
- **R-27, editor safety:** a script reload can no longer happen in the middle of Play. Before, one reset every static: the QA test
  profile's save routing fell back to the player's own save, and the game's lives read 0 and were saved on exit. The guard makes
  scripts edited during Play take effect when Play stops.
- **Test suites:** three of the project's check suites had drifted from the game and were fixed as tests: the home screen suite
  (twice), HUD layout and QA controls. Iteration 28 fixed four more stale checks, which the evidence run exposed:
  - the board check expected idle rockets at 94% of the cell, instead of their designed 78%;
  - the same check predated the board's grout layer;
  - the TNT frame check was 0.0009 too strict just above zoom 1;
  - the TNT preview check counted particles that the preview no longer uses.

**Not changed:**
- **R-07** (a booster pack purchase sets the stock instead of adding): unreachable with the current buttons.
- **R-19's second half** (a suspected pile-up of hint coroutines): measured with profiler markers across idle play and
  real moves, it is bounded, with one hint display and one scan per ~7 s cycle. No change to the protected AI.

## Verification

**Type check (the C# compiler).** A full clean rebuild of all 165 assemblies, with every compiler message captured per assembly:
**0 errors**. The final clean rebuild, after the last change, also gives 0 errors and 0 warnings.

**Lint.** The repository configures no linter: no .editorconfig, rulesets, analyzer settings, CI or package scripts. The lint
signal is therefore the compiler warnings plus Unity's serialization analyzer. They went from **69 to 0**:

| Assembly | Before | After |
|---|---:|---:|
| Assembly-CSharp | 22 | 0 |
| Assembly-CSharp-Editor | 33 | 0 |
| spine-unity | 3 | 0 |
| spine-unity-editor | 11 | 0 |

**Test suites.** The project has no Unity Test Framework tests of its own; its suites are editor check suites. The final run used
the final code in a clean editor. Each suite ran in a fresh Play session, and the player's real save was compared before and after
each one: it never changed.

| Suite | Result |
|---|---|
| Reference level data (50 levels, 3933 cells) | PASS |
| Campaign data (294 checks) | PASS |
| Home screen (167 checks) | PASS |
| Home sky, home finish, tool bar, HUD layout | PASS |
| Responsive input, pointer events and cancellation | PASS |
| TNT sun art, board palette, dialogs (21) | PASS |
| QA controls ("ALL QA CONTROLS PASSED") | PASS |
| Reference campaign 01-50 ("ALL FIVE BATCHES PASSED", 413 s) | PASS |
| Effects main suite, cell breaks, TNT, rockets | **FAIL** (open item 1); with the imports off, cell breaks, rockets and TNT pass |
| Booster suites: Color Ball V5, Propeller V5 | **FAIL** (open item 1); **PASS** with the imports off |
| Color Ball live v1, Color Ball QA, Color Ball V4 | **FAIL**, with or without the imports: they test superseded Color Ball code (open item 2) |

As shipped, 9 suites fail, each at the same check every run.
- **Six fail because of the imports.** They check Royal Throne's own piece art, animators and effects, and the game draws imported
  Royal Match visuals instead. A test change cannot fix them without certifying the copied assets, and a game change cannot fix
  them without reversing an import that was made on request. That choice is open item 1.
- **Iterations 28 and 29 measured those six with the imports switched off** (evidence under open item 1): five pass, and the main
  effects suite passes 493 of its 495 checks. With the imports on it stops after 48.
- **The other three test Color Ball code the game no longer runs** (open item 2).

**Shared Match-3 VFX package tests** (`SharedPackages/com.royalthrone.match3-vfx`, run in a scratch project): EditMode **17/17**,
PlayMode **22/22**. The package's runtime and editor assemblies compiled with 0 warnings.

**Levels 1-50 regression** on the final code: **50/50**. Every level opens to live play with a populated board, its goals and the
correct HUD limit. The console had no entries. The HUD refreshes its time label every 0.1 s, so for about 0.1 s after each
timer tick it still shows the previous second. This was measured and is harmless, and the sweep's check allows for it.

**Layout at four sizes** (1080x1920, 20:9 1080x2400, 4:3 1536x2048, 720x1280): 36 of 36 checks. On nine screens every visible
control is on screen and takes a tap at its centre, and no text is clipped or off screen.

**Home settings, lives timer, references:**
- settings survive a restart;
- the lives refill is correct for a long offline gap, a normal wait and a device clock moved back;
- the build scenes and every `Resources/` prefab have 0 missing scripts and 0 dead references;
- there is exactly one of each manager on home, in a level and back on home.

## What went wrong during verification

- **The player's real save lost its lives, and was restored.** During the first full suite run, a script edit of mine was still
  waiting to be picked up. The editor picked it up in the middle of a Play session and reloaded the scripts. The reload stopped the
  campaign suite and set the real save's lives from 5 to 0. The lives were restored exactly as the game writes a full set, and
  nothing else in the save changed. The reload guard (R-27) now prevents this, and every edit of mine is followed by a refresh.
- **The home suite failed intermittently.** It read the screen size from an editor callback, which reports the editor window about
  3.5% of the time. It now runs inside the game loop and passed 3 of 3 runs, then again in the final run.

## Open items that need a decision

These are product, legal or device questions. The loop recorded them and did not change them.

1. **Royal Match art and effects in the game (R-25). A legal and product decision, and the most urgent.** Since the reference-asset
   integration of 2026-09-24 (`cdd68361` "Integrate recovered gameplay assets and 50-level reference QA campaign", then the
   imported `ReferencePieceVisual` prefabs and `ReferenceVfxPool`), the game draws sprites and effects extracted from the Royal
   Match APK:
   - **Board:** every piece and booster is an APK sprite (for example `Items-MatchItem-book__22221`). The match, TNT, rocket,
     Color Ball and propeller effects use the imported particle prefabs.
   - **HUD, tool bar and home controls:** they use the imported layout sprites.

   Royal Throne's own piece art, animators and effects are still in the project, but bypassed. `RoyalPieceSkin` skips its animators
   when the reference visual attaches, and the effect services return early when `ReferenceVfxPool` has the imported effect. There
   is no switch: the imported visuals are active whenever their `Resources` exist.

   `SourceReference/CLAUDE.md` makes Royal Match a quality reference only, and an import of its assets was declined on 2026-09-23.
   The choice is between:
   - **(a)** keeping the imported art and accepting the legal exposure;
   - **(b)** restoring Royal Throne's own art, by removing the imported board visuals and effects or adding a switch, then running
     the full effect suites.

   **Evidence for (b), from iterations 28 and 29.** The imports were switched off for single Play sessions:
   - the four caches that select them were emptied at runtime;
   - the two booster prefabs that have no cache were moved out of `Resources` for the run, then moved back, byte for byte.

   With the imports off:
   - **Pass:** cell breaks, rockets, TNT, Color Ball V5 (the version that ships) and Propeller V5.
   - **Main effects suite:** 493 of its 495 checks pass; as shipped it stops after 48. The 2 failures come from the imported HUD.
     Since `9f27a636` the board's playable area leaves room for the imported HUD, so on a 360x640 phone a 9x11 or 5x11 board gets
     30.9 px per cell, under the suite's floor of 32.
   - **As shipped**, the same suite also finds the imported leaf and lily pieces filling less than 88% of their cell.

   So Royal Throne's own board art and effects still work. (b) is still a code change, not only a deletion, because five places
   load imported files with no switch:
   - the Color Ball and Propeller activation visuals;
   - the board border, which replaced the own `frame-edge-*` art;
   - the playable area and the HUD layout;
   - the tool bar;
   - the home art.
2. **Three superseded Color Ball suites (R-28).** Color Ball live v1, Color Ball QA and Color Ball V4 fail whether the imports
   are on or off. They test Color Ball code the game no longer runs:
   - live v1 waits for a frame animator that nothing attaches;
   - Color Ball QA expects the Boosting view for a single ball, which since the 09-21/22 rework runs the V4 charge-and-beam
     sequence;
   - Color Ball V4 holds the V5 prefab that ships to V4's rules.

   Retire or re-target them. The Color Ball that ships has its own suite, Color Ball V5, and it passes with the imports off.
3. **The reference QA campaign (R-26).** Its levels 01-50 were decoded from the APK; the campaign is QA-only and players cannot
   reach it. It logs three "[Reference Gameplay incomplete]" warnings: cupboard door feedback, coin-collection particles and the
   mailbox lid animation were not recovered, and nothing replaces them. Tied to item 1.
4. **Ungated changes from 2026-09-23, made before the loop.**
   - `RoyalOfflineMode` turns off ads, IAP, social and analytics, and strips the INTERNET permission from Android builds.
   - The home Play button and navigation were recoloured teal.

   Both still wait for a yes or no.
5. **Offline economy (R-20).** There is no coin income: a player has 5 coins for life, and the gem shop is unavailable offline. R-08
   made the shop honest, but it still gives nothing.
6. **Currency icon (R-23).** The out-of-moves CONTINUE shows a gem, while home calls the balance coins.
7. **4:3 tablets (R-22).** The raised Home tab icon draws over the Settings dialog's bottom button. The layering is intentional in
   the protected home layout, so this needs a size or layering decision.
8. **HELP in the pause menu (R-15).** The menu has a HELP label with no button, and the How-to-play popup is unreachable. Add the
   button or remove the label.
9. **Texture compression.** 184 scene textures (105.7 MB) are imported uncompressed. Compressing the 77 largest would save about
   72-77 MB of memory on Android (`Assets/_Ralph/TEXTURE_AUDIT.md`). This changes how the art looks, so it needs an art sign-off and
   a device check.
10. **Incremental GC** is off (`gcIncremental`). R-24 removed the steady per-frame garbage in normal play; turning incremental GC on
   would shorten the remaining collection pauses.
11. **The King's name.** The character and its files say "Venguard", while the English word is "Vanguard". Keep the spelling or
    rename it before release.
12. **Device checks, not possible in this pass (no Android, by instruction):**
    - R-16, whether the hidden legacy map still takes taps;
    - safe-area insets (notches and cutouts);
    - the hardware Back button;
    - memory and frame time on a phone;
    - the Legacy and Round launcher icons that `RoyalAppIconBuilder` still writes.

## Risks

- **Royal Throne's own effects are verified only with the imports switched off.** While item 1 stands, the game never runs them.
  With the imports off (iterations 28-29) they pass their suites, apart from the imported HUD's two layout checks. If they are
  restored, make the code changes listed under item 1 and run every effect and booster suite again before release.
- **The reload guard changes editor behaviour.** With `Assets/JellyGarden/Scripts/Editor/RoyalPlayModeReloadGuard.cs` in place, a
  script saved during Play takes effect when Play stops, not immediately. Deleting the file restores Unity's default, and with it
  the risk to the real save.
- **The evidence is editor-only.** Every result here comes from the Unity editor (Mono) at simulated resolutions. Performance and
  allocation numbers on an IL2CPP Android build will differ.
- **Uncommitted work in the tree.** About 15,000 paths from earlier sessions and other agents are uncommitted. The loop's commits
  are path-limited and contain only its own hunks, checked with a dry run before each commit. A broad `git add` would sweep the
  rest in.
- **The Spine runtime is untracked.** `Assets/Spine`, with its two Unity 6.5 patch sets (`Assets/Spine/UNITY6-COMPAT-PATCHES.md`),
  and the King Venguard content exist only in the working tree. They are not in any commit or in the backup branch.
- **Suites change tracked files.** The project's check suites rewrite their result files under `Design/`, in other agents'
  folders. The loop left those refreshed files uncommitted. It backed up and restored the per-version booster evidence byte for
  byte. In iteration 28 it restored every result folder byte for byte.
- **Latent fixes.** The rewarded-ad timer fix and R-06 only matter once ads are on, which the offline build does not do.

## Protected areas

None of these was redesigned or rebalanced. The loop's changes that touch them are small integration fixes:

| Area | Change |
|---|---|
| Levels 1-50, level design and balance | none (no level data, targets, limits or balance values changed) |
| Match-3 board | `LevelManager`: Back handling (R-03, R-10), saving a win at once (R-05), the `GetRandomItems` loop guard (R-02), and review cleanup (an unused field, an unused catch variable, two `[NonSerialized]` markers). `MatchVFXService`: one API rename (R-18). `Square`: an unused field removed. |
| Level-1 tutorial | `TutorialManager.OnDisable` (R-09, 3 lines); `HandTutorial`: an unused local removed |
| Home layout | `ReferenceHomeLayout.Hide` keeps a clear tap area (R-21); `RoyalHomeScreen`: one Back line (R-03) and singular/plural counts (R-17) |
| Splash, loading screen | `RoyalTitleScreen`: an unused field removed |
| Logo and branding art | none |

A Levels 1-50 regression ran after the fixes, again after the first review, and again on the final code.

## The loop in numbers

- **29 iterations:** 17 fix iterations (1-14, 22, 23, 27), 8 verification passes V-01 to V-08 (two with fixes; V-07 is the
  texture audit), 2 reviews, and 2 R-25 evidence runs (28, with its test fixes, and 29).
- **29 commits** (`c6b9fb97` to the commit of this report): 52 files, 45 of them outside `Assets/_Ralph`.
- **Changed files:**
  - runtime: `AnimationManager`, `BoostAnimation`, `BoostIcon`, `Counter_`, `DateTimeManager`, `HandTutorial`, `InitScript`,
    `LeadboardManager`, `LevelManager`, `LevelsMap`, `MapCamera`, `MapTargetIcon`, `MatchVFXService`, `PropellerVisualV5`,
    `ReferenceBoosterBar`, `ReferenceHomeLayout`, `RoyalBackButton` (new), `RoyalGameplayPresentation`, `RoyalHomeScreen`,
    `RoyalMenuSkin`, `RoyalReviewAudioCapture`, `RoyalTitleScreen`, `Square`, `TutorialManager`, `UrpBloomSetup`;
  - editor: `LevelMakerEditor`, `LevelsEditorBase`, `MapLevelEditor`, `PostImporting`, `ReorderableDrawer`,
    `RoyalAppIconBuilder`, `RoyalPlayModeReloadGuard` (new), and the check suites `ReferenceHomeFinishChecks`,
    `ReferenceHomeSkyChecks`, `ReferenceLayoutChecks`, `RoyalBoardSurfaceChecks`, `RoyalColorBallChecks`,
    `RoyalColorBallQaChecks`, `RoyalEffectsChecks`, `RoyalHomeScreenChecks`, `RoyalQaChecks`, `RoyalRocketChecks`, `RoyalTNTChecks`;
  - reports: `Assets/_Ralph/RALPH_STATE.md`, `TEXTURE_AUDIT.md`, this file.
- **Working tree only** (untracked King Venguard work, never committed piecemeal): `KingVenguardCharacter.cs` (R-19), seven Spine files,
  `spine-unity-editor.asmdef`, `UNITY6-COMPAT-PATCHES.md`, and `Design/Characters/KingVenguard/V1/Source/tools/runtime/patch_spine_unity6_deprecations.py`.
