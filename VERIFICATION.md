# Copy verification

Completed on 8 September 2026.

| Check | Result |
| --- | --- |
| Source files copied and initially SHA-256 verified | 1,664 files; 91,505,766 bytes |
| Asset files copied | 1,611 |
| Original asset `.meta` files preserved | All 853; identical hashes |
| Source files and original setup files checked after import | 1,671 |
| Changes detected in those original files | None |
| Shared source-folder links | None |
| Package dependencies using `file:` paths | None |
| Git remotes at initial copy verification | None; a separate backup origin was configured later, as noted below |
| Git access from the user's normal Windows account | Verified after the explicitly approved ownership correction on this copy |
| Unity Editor version | 6000.5.10f1 |
| Unity import and C# compilation | Completed; process exit code 0 |
| Unity Hub registration | Added as JellyGarden-Codex |
| Visible Editor launch | Opened from Hub; copied game scene and level map verified visually |
| C# compiler errors found | None |
| Compiler warnings | Present in the inherited code; not changed as part of copying |
| Play Mode / device / executable build validation | Not performed |

The imported copy differs from the saved game source only in `ProjectSettings/ProjectSettings.asset` and `ProjectSettings/UnityConnectSettings.asset`: a separate development identity and disabled/unlinked local Unity Services settings. All copied assets, scripts, levels, package manifests, rendering settings, and metadata remained identical after import. The README, project boundary instructions, provenance records, and per-machine Git ignore entries are added development documentation/configuration.

Unity resolved packages, compiled script assemblies, completed its initial Asset Pipeline Refresh, and logged `Exiting batchmode successfully now!`. The first attempt from the restricted shell could not connect to the licensing client and was stopped. A second attempt using the existing signed-in Windows environment completed successfully. Only the copied project was passed to either import command.

The baseline commit `7194b95` preserves the verified source copy. The next commit `e84a1d2` introduces the development identity and project boundary documentation. `SourceReference/COPY-MANIFEST.json` records the copied files before those intentional changes.

During the initial copy and validation, the original Unity Editor and VS Code project were not saved, closed, reconfigured, or built. That phase did not create a remote repository, cloud project, or store submission.

## Online backup verification, 8 September 2026

The user requested online backup and created `https://github.com/jchakmaavian/jellygarden-codex`. Its GitHub page was checked: the repository is private, is owned by `jchakmaavian`, and initially contains one README commit (`4adf588dee4827e72a79b09821827998a234af55`).

- This copy's `origin` is now `https://github.com/jchakmaavian/jellygarden-codex.git`. The original project's remote/configuration was not changed.
- Local `git fsck --full --no-dangling` passed. The largest game file is approximately 15 MB; no tracked file approaches GitHub's 100 MiB limit.
- A focused scan found no common GitHub/AWS/OpenAI token or private-key signatures in the scanned text files, and no tracked files with common credential-container filenames. This is not a comprehensive credential audit.
- Git transport sign-in and an authenticated fetch succeeded. The initial GitHub README commit is preserved by merging both histories; the README combines the repository title with this copy's detailed instructions.
- The first game upload succeeded on `main`, at merge commit `791904905e6cf5e0d6243ca6f1f034d3cd082878`. The push advanced the existing GitHub branch without force-pushing. Local `main` tracks `origin/main`.
- GitHub's page was checked after upload: it still says **Private repository**, and shows `Assets`, `Packages`, `ProjectSettings`, and the project documentation.
- A fresh HTTPS clone from GitHub completed at 05:18:44 UTC. Its commit matched the uploaded development snapshot. All 1,670 tracked files and 853 asset `.meta` files were restored, including both game scenes, the package manifest/lockfile, and Unity version settings for **6000.5.10f1**.
- The restored copy passed `git fsck --full --no-dangling`, `git diff --exit-code HEAD`, and a clean working-tree check. This verified repository download, revision-history integrity, and restoration of the tracked files. Unity was not launched in the temporary recovery copy.
- Future work must be saved, committed, and successfully pushed to be backed up. No scheduled backup job is configured. See `BACKUP-AND-RESTORE.md` for recovery steps and excluded files.

## First playtest and production pass, 8 September 2026

See [PLAYTEST-AND-AUDIT-2026-09-08.md](PLAYTEST-AND-AUDIT-2026-09-08.md) for current evidence; earlier rows above describe the initial copy phase.

- Checked all 19 inherited audit/inventory files against the original with SHA-256: all matched. Original reports were not edited.
- Played the title/map/Level 1 flow and the Level 4 pause/map abandonment path through native UI.
- Native drag validation remains open. F8 tracing showed the automated press and release at the same destination position, with input enabled and the board unblocked.
- Added two opt-in Editor-only playtest controls; Unity recompiled `Assembly-CSharp-Editor.dll` successfully. A new deprecated API warning during the first iteration was removed by using `FindAnyObjectByType`; the final tool compile produced no new tool warnings/errors in the checked log segment.
- F9 exercised six suggested swaps through the existing game rule path: moves 6→0, score 0→1050, jelly targets 4→1, cascades and a striped piece, then a natural out-of-moves failure. It did not set score/goals/outcome directly and is not a real-input pass.
- Failure summary and Again→map navigation were observed. A natural win, final-move win, invalid swap and full special-combination testing remain outstanding.
- Revised Level 1 instruction rendered as two centred, unclipped lines at 720×1280. The scene diff changes only its text field. Other aspect ratios and audio were not tested.
- Final observed Play Mode Console: 0 errors, 7 inherited/integration warnings.
- Document links and `git diff --check` passed. No renderer/package/mobile-export configuration changes were made.

## Final Royal Throne branding, 8 September 2026

The user explicitly locked the generated logo as final and requested replacing the JellyGarden logo and title in this copied game.

- Installed the approved PNG at the existing runtime logo path, preserving its sprite GUID and entrance-animation binding. SHA-256 matches the approved design master exactly: `7397cba6758c49fc1cc5bf13f64919edc3a33bb97d0515c460623575b298ea22`.
- The opening scene uses a 600 × 600 image with preserved aspect ratio and no decorative raycast interception. Texture import retains alpha, uses full-rectangle sprite geometry, bilinear/clamp sampling, no mipmaps, and a 2048 maximum so the 1254-pixel source is not downscaled on import.
- Updated Unity product name and tile display name to Royal Throne. The optional Facebook share title now reads the product name; Facebook sharing was not enabled or exercised. Company, application identifiers, product GUID, project folder, and repository identity remain the copy's existing values.
- The copied game's existing Windows Editor preferences (22 values) and LocalLow data were copied to the Royal Throne location. Existing source data was retained. A standard registry-provider copy hit an integer-type conversion error; the Windows registry API completed the missing values, and the old/new level stars and scores were read back and matched. No original-game save locations were accessed or changed.
- Unity imported the replacement sprite and compiled successfully (`Tundra build success`, 23.94 seconds); no compiler errors were found in the checked compilation segment. Existing inherited warnings remain.
- Visually checked the finished opening state at the 720 × 1280 portrait Game view: full crown and lettering visible, transparent background intact, no distortion, and clear space above the Play button. The entrance animation reaches this state. Clicking Play opens the game level map.
- Play Mode Console after the title-to-map flow showed 0 errors and 7 existing warnings. This focused branding check does not claim full gameplay, other aspect ratios, device exports, or optional social integration validation.
- Saved the actual Unity-window preview in [Design/Branding/royal-throne-in-game-720x1280.jpg](Design/Branding/royal-throne-in-game-720x1280.jpg). The screenshot includes Editor chrome around the 720 × 1280 Game view.
- No remaining JellyGarden/Jelly Garden player-title text was found in scanned scene/prefab text and share-title fields. Internal kit namespaces, paths, integration identifiers, menus, and third-party attribution were retained.
- Diff whitespace checks pass with CRLF endings recognized. No package, renderer, mobile SDK, or original-project changes were made.
- Restarted only this copied Unity project and repeated the opening-screen → Play → map flow. The approved logo still renders correctly; Levels 1–3 retain three stars each, Level 4 is unlocked, and the copied balance loads as 4 lives and 5 gems. Restarting refreshed Unity's native preferences cache after the product-name change.

## Castle splash, redesigned Play CTA and animated loading, 8 September 2026

- Rebuilt only the main scene's title Canvas as editable Unity UI. Camera, EventSystem, InputManager and SoundBase roots were retained. The game scene, gameplay rules, saved-progress identity, renderer, packages and platform settings were not changed.
- The approved logo remains byte-identical to its master. The old candy background, blue baked-text Play button, flattened JellyGarden loading artwork and legacy entrance clip are no longer referenced by the main scene. The new castle image is stored in Assets and included in the source backup.
- A first authoring check exposed missing CanvasRenderer components on the custom panel graphics. The component requirement and builder were corrected; the scene was rebuilt, and the validation command now checks every UI Graphic has its renderer. No errors/exceptions were found in the project log after the corrected title validation.
- Unity compiled the new runtime and Editor scripts successfully. The final title validation passed: approved logo identity/aspect, required components/bindings, one persistent Play callback, no legacy loading-art reference, and an enabled destination scene.
- Visually checked the splash at 720 × 1280, 768 × 1024 and 720 × 1600. The full logo and Play CTA remain readable and inside the viewport. The full-bleed castle crops to the viewport rather than stretching. Physical device cutouts were not tested.
- In Play Mode at 720 × 1280 and 720 × 1600, the F7 Editor-only loading preview displayed its dedicated Royal Throne logo, purple loading panel and readable text. Separate observed frames showed the progress fill advancing and the jewels changing height/scale. The preview returned to an actionable title screen.
- A native rapid double-click on Play completed the real asynchronous title-to-game flow and reached the existing map. The loading Canvas was removed; opening the map's Settings confirmed input was released. Levels 1–3 still displayed their earned stars, and Level 4 remained unlocked. No new gameplay result was generated for this branding check.
- Console after the real title-to-map flow: 0 errors, 7 inherited/integration warnings. Missing-scene and load-start exception recovery are implemented but were not fault-injected in this visual test. No Android/iOS export was performed.
- Actual Unity-window screenshots are saved as [splash preview](Design/Branding/splash-castle-720x1280.jpg) and [loading motion preview](Design/Branding/loading-motion-preview-720x1280.jpg). The latter is explicitly labelled “Animation preview”; it is not a claim that preview progress measured a real load. Both files include Editor chrome.

## Approved royal-blue splash integration, 13 September 2026

- The user approved the v2 visual study for Unity integration. Installed its exact smooth 3D blue-castle and crowned sapphire Play PNGs in the main scene. SHA-256 comparisons match both approved masters. The locked logo and original light-wheel texture/rotation asset are unchanged.
- Restored the original six-second halo appearance using the original wheel sprite with a soft alpha edge and unscaled-time rotation. Title and loading halos stay aligned during crossfade and do not block input. The blue loading panel, animated jewels, glint and real asynchronous scene progress retain the existing loading lifecycle.
- Unity imported the artwork and compiled the runtime/Editor scripts. The explicit main-scene builder and title validation passed. The checked project log has no C# compiler errors or exception entries. Existing legacy compiler warnings remain.
- In Unity Play Mode, visually inspected splash and F7 loading-preview layouts at 720 × 1280, 720 × 1600 and 768 × 1024. The full logo, Play CTA and loading text fit; castle artwork covers the view without stretching. Observed separate animation frames with changing halo orientation, jewel motion and advancing progress/glint, and automatic return to Play.
- Rapid double-click on Play at 720 × 1280 and single click at 768 × 1024 both reached the existing game map. Opening map Settings confirmed input was released after the loading overlay faded away. No level was played or game result generated during these checks.
- Console after the real loading flow: 0 errors and 7 inherited/integration warnings. Physical cutouts, missing-scene/load-exception fault injection, mobile exports and full gameplay regression were not tested.
- The game scene, gameplay logic, save identity, packages, renderer and platform settings were not changed. Work was limited to the independent Codex copy.
- Actual Unity captures and their scope are recorded in [RoyalBlue-Unity](Design/Branding/RoyalBlue-Unity/README.md). The separate browser-concept screenshots remain historical review evidence.

## Android QA build on a physical phone, 13–14 September 2026

The user requested running the game on their USB-connected Android phone so QA can test it. See [QA-ANDROID.md](QA-ANDROID.md).

- **Setup.** Installed Android Build Support, Android SDK & NDK Tools (NDK r27c) and OpenJDK 17 for Unity 6000.5.10f1 with the Unity CLI. The first attempt failed with access denied under `C:\Program Files`; the retry succeeded after the user approved the Windows administrator prompt.
- **Device.** Infinix X665B, Android 11 (API 30), arm64-v8a, 720 × 1612.
- **Ads define blocker.** Android had `UNITY_ADS`, but no Unity Ads package is installed; the built-in Connect module only provides `UnityEngine.Advertisements.UnityAdsSettings`, so the ads code cannot compile for Android. The inherited ad IDs belong to the original game. The inherited `PostImporting` postprocessor re-added `UNITY_ADS` to Android, iOS and Windows Store after every import, which stopped the first headless build at the new build tool's guard. The forced add was removed; the build tool now strips ad, purchase, Facebook and backend defines for Android and logs any it removes. The iOS and Windows Store entries still contain `UNITY_ADS`; those targets were not built.
- **Defines after the switch.** Android now has `SENTIS_ANALYTICS_ENABLED;APP_UI_EDITOR_ONLY`, written by installed packages. Standalone is unchanged.
- **First build.** Headless IL2CPP ARM64 build: `Build Finished, Result: Success`, 76.6 MB APK, 11 min 31 s including the first Android asset import. `adb install -r` succeeded and the app opened to the Royal Throne splash at full layout. The crash buffer stayed empty.
- **Launch log.** A `ClassNotFoundException` for the Play Asset Delivery `AssetPackManager` (not used). An `ArgumentException: 'InputUpdateType.None' is not a valid update mask` from `com.unity.dt.app-ui` enabling EnhancedTouch, which game input does not use; it did not appear in later builds after the package define above. Inherited `DontDestroyOnLoad` warnings.
- **Real touch play.** Observed through screenshots and logs while the user played on the phone; these were not injected inputs. The home screen showed Level 4 next with 8 stars, Levels 1–3 having been completed on the phone. On Level 4, moves decreased from 11 to 8 with matches, effects, the "Fantastic" banner and goal counter updates. No Unity errors were logged. An overnight gap produced a normal process restart, not a crash.
- **Navigation bar finding.** Player Settings request a visible status bar, a hidden navigation bar and swipe-revealed transient bars. At launch the navigation bar was hidden. On this Android 11 phone a touch revealed it: `APP_CMD_WINDOW_INSETS_CHANGED` followed an injected tap on empty artwork, a Play tap made while the bar was hidden did not start loading, and the bar then stayed over the bottom of the home tab bar and the in-level booster bar.
- **Navigation bar fix.** Added `RoyalAndroidSystemBars`, compiled only for Android and skipped in the Editor. At startup and whenever the game regains focus, it applies `ShowTransientBarsBySwipe` and hides the navigation bar. It deliberately does not react to every insets change, so a swipe can still reveal Back and Home. A first version that only re-hid the bar on focus did not stop taps revealing it; the tap test exposed this, and the behaviour call was added.
- **Build from the open Editor.** **Tools → Royal Throne → Build → Android QA APK** succeeded twice from the running Editor: 4 min 21 s, then 55 s incremental, each producing a 75.5 MB APK with no compiler errors. The automation's menu request timed out after 30 s while the build continued; the Pipeline package logged that timeout as a console error, which is unrelated to the project.
- **Re-test after reinstall, progress kept.** Injected adb taps and swipes, with the navigation bar state read from Android's window insets. The bar stayed hidden at launch, 1 s and 3 s after a tap on empty artwork, after a single Play tap (the game scene loaded on that first tap), after Home and return, on the Level 4 popup and on the Level 4 board. The home tab bar (Story, Leagues, Home, Team, Collection) and the in-level boosters (crown, hammer, magic orb, TNT) were fully visible. A swipe up from the bottom edge showed a translucent navigation bar at 0.7 s, hidden again by 5 s. No Unity errors apart from the Play Asset Delivery lookup, and the earlier App UI input error did not return.
- **Not tested.** Phones older than Android 11, where Unity documents that the swipe-only behaviour cannot be set; other screen sizes and camera cutouts; a level-by-level regression; audio; performance, heat and battery; calls and other interruptions; iOS. The test left the phone on the Level 4 board it started.
