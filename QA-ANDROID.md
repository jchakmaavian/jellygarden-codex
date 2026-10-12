# Royal Throne — Android QA build

A test build of the game for physical Android phones. The latest build was produced on **14 September 2026 at 13:26 Bangladesh time** from this independent copy.

This build includes larger board pieces and touch targets, persisted matching hints,
new sound effects and falling fragments, power-up and text celebrations, the app icon,
and the revised home and loading presentation. **452 Editor checks passed** across
gameplay/input, hints, home and dialogs. See the [screen and effects review](Design/Effects/Unity-review-v3/README.md).
Physical-phone frame rate, battery use and speaker balance remain unverified.

Unity reported a build time of 2 minutes 52 seconds. The APK is **79,592,603 bytes**
(75.9 MiB); Android's signature verifier passed with the existing Android Debug key.
SHA-256: `b8795a40da8cf20a86da49cd5424a2692e2c8a2685fb512605f7f6597b956eda`.
The [build record](Design/Effects/Unity-review-v3/android-build.json) records the package checks.

## Build facts

| Item | Value |
| --- | --- |
| APK | `Builds/Android/RoyalThrone-QA.apk`. The file is excluded from Git and the GitHub backup; share it directly. |
| App name and package | Royal Throne, `com.codexsandbox.jellygarden` |
| Version | 2.5.2 (version code 1) |
| Supported phones | Android 8.0 (API 26) or newer with a 64-bit ARM processor (arm64-v8a). Phones that only support 32-bit apps cannot install it. |
| Orientation | Portrait |
| Build type | Release player (IL2CPP) signed with the Unity debug key. It cannot be uploaded to Google Play. |

## Install

With USB debugging enabled:

```
adb install -r Builds/Android/RoyalThrone-QA.apk
```

Without a computer, copy the APK to the phone, open it from the Files app, and allow **Install unknown apps** when Android asks.

If installation fails with `INSTALL_FAILED_UPDATE_INCOMPATIBLE`, the phone has a build from another computer, which uses a different debug key. Uninstall Royal Throne first. Uninstalling deletes the progress saved on that phone.

## Disabled in this build

These are intentional. Do not report them as bugs.

- **Ads.** Rewarded-video buttons stay hidden. The inherited ad IDs belong to the original game, so the ad SDK is not enabled.
- **Purchases.** Gem packs in the shop close without charging or granting gems.
- **Facebook, leaderboards and online accounts.** Not enabled.
- **Editor playtest shortcuts.** F7–F12 tools and debug win/lose keys exist only inside Unity.

## Test session tips

- A fresh install starts a new player on the phone. Progress from the Unity Editor is not copied.
- Lives: a maximum of 5. When lives are below 5, one returns every 15 minutes.
- System bars: the status bar stays visible at the top. The navigation bar is hidden during play; swipe up from the bottom edge to show Back and Home briefly. Report any screen where the navigation bar stays visible or covers buttons, especially on phones older than Android 11, which were not tested.
- To start again as a new player: **Settings → Apps → Royal Throne → Storage → Clear data**, or `adb shell pm clear com.codexsandbox.jellygarden`. This also clears level progress.
- Screenshot from a computer: `adb exec-out screencap -p > shot.png`
- Logs for a bug: run `adb logcat -c`, reproduce the problem, then `adb logcat -d -s Unity AndroidRuntime > royal-throne-log.txt` and attach the file.

## Bug report contents

Phone model and Android version, APK date, level number, steps, expected result, actual result, how often it happens, and a screenshot or screen recording. Attach logs for crashes, freezes and stuck boards.

## Suggested coverage

This adapts the [Editor baseline checklist](PLAYTEST-TOOLS.md) for touch. Record each item as passed, failed or not tested.

- **Opening flow:** app launch, splash, Play, loading screen, castle home, Settings and information dialogs.
- **First level:** tutorial message, dismissal and first move.
- **Touch input:** valid horizontal and vertical swaps, invalid swap, fast swipes, a swipe released outside the board, taps on buttons while pieces are still falling.
- **Board rules:** one move used per accepted swap and none for an invalid swap; matches, falling, refill and cascades; special pieces and their combinations.
- **Goals:** targets and HUD counters agree with what cleared on the board.
- **Outcomes:** win with moves remaining, win on the final move, fail with goals unfinished, retry, return home.
- **Lives:** life use, refill countdown, behaviour with no lives left.
- **Progress:** fully close the app and reopen; stars and unlocked levels remain.
- **Interruptions:** Home button and return, screen lock, notifications or calls during a level, the back gesture or button.
- **Audio:** music and sound settings, phone silent mode, audio after returning to the app.
- **Presentation:** readable text, nothing hidden by the camera cutout or navigation bar, dialogs inside the screen, smooth animation, heat and battery use across a 20-minute session.

## Rebuilding the APK

The development PC needs Unity 6000.5.10f1 with Android Build Support, installed on 13 September 2026.

- **In Unity:** **Tools → Royal Throne → Build → Android QA APK**. Unity switches the project to Android, which takes several minutes the first time.
- **Headless, with Unity closed**, from the project folder:

  ```
  unity build . --target Android --execute-method RoyalThrone.Editor.RoyalThroneAndroidBuild.BuildQaApk --allow-dirty-build --log-file Logs/android-qa-build.log
  ```

Before building, the tool removes any ad, purchase, Facebook or backend SDK symbols from the Android settings and logs a warning naming them.
