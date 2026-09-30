# Royal Match (installed build 38174) cold-start observation

Observation only. No taps, keys or text were sent to the game. Nothing was installed, uninstalled or cleared.

## Device / version facts

- Device: Infinix X665B, Android 11, 720x1612, serial 088083728H004391, USB powered (battery ~14-15 %), Wi-Fi connected.
- App: com.dreamgames.royalmatch, installed versionCode 38174 (Play Store install, 2026-09-20). Launch activity: com.dreamgames.library.DreamUnityPlayerActivity.
- Reference APK on disk (Design/Backup/apk/Royal_Throne-rf.apk) is versionCode 32984 - an OLDER build than the one observed here. It was not installed.
- Account state at launch: logged-in progress at Level 79 (Super Hard), Area 5 (7/9), 693 coins, 5/5 lives, 4 stars. App was on the Shop screen before the first force-stop (pre-launch-state.png), on the Home screen before runs 2 and 3.
- Date/time: 2026-09-23, 17:54-18:03 local (BST on the PC clock).

## Method

- Three cold starts were performed (adb shell am force-stop, 2 s pause, adb shell am start). All three showed the same screen sequence and ended on the Home screen with no popup.
  - run1-no-video/: screenshots only (screenrecord failed: Git Bash rewrote the /sdcard path).
  - run2-no-video/: screenshots only (recording succeeded but the pull to a /c/... path failed and the device copy was removed before that was noticed).
  - run 3 (this folder): shot-<elapsed>.png screenshots + rm-coldstart.mp4 (90.06 s, 720x1612, h264, variable frame rate ~27 fps).
- Elapsed times are measured on the PC clock from T0 = the moment 'am start' was issued (T0-epoch.txt, capture-log.txt). Each screencap took 0.7-3 s under load, so a shot's filename is the time the capture STARTED (the frame was grabbed at or shortly after that).
- Video alignment: recording started 4.47 s before T0 on the PC clock; the force-stop (home screen -> one black frame -> launcher) is visible at video t = 1.9 s and the force-stop command returned at REC_START + 2.40 s, so video t0 = REC_START + ~0.45 s and T0 = video t ~4.0 s (+/- 0.2 s). Fine timings below are video times minus 4.0 s.

## Timeline (run 3, elapsed from T0; video time in brackets)

| Elapsed (s) | Video t (s) | State |
|---|---|---|
| -2.1 | 1.9 | Force-stop takes effect: Home screen -> single black frame -> Android launcher (hard cut). |
| 0.0 | ~4.0 | 'am start' issued; launcher still visible. |
| ~1.7 | 5.7 | App window appears. Android system launch animation (launcher scales/fades into the blue window), ~0.2 s. |
| ~1.9 | 5.9 | Dream Games splash: solid blue #2e31fe (RGB sampled from screenshots; corners and centre identical) full screen, white lowercase "dream" wordmark with a 4-point sparkle at the top-right of the m, centred (~48 % of screen height). Completely static for its whole duration (identical at 7.0 s and 11.5 s video time). No loading indicator. |
| ~9.05 | 13.05 | Hard cut (no fade) to the Royal Match loading screen: static key art (purple/gold castle, King eating at a table, Butler holding a parasol, dog on a chair, lawn foreground), "ROYAL MATCH!" logo top-right, and a static "Loading" label in cream/light-yellow bold rounded font with dark outline at ~y=1480/1612, centred. NO progress bar, NO percentage, NO spinner, NO animated dots, NO tips (checked at 4 fps for 10 s: pixel-identical label). |
| ~19.3 | 23.3 | Short cross-fade (~0.1-0.2 s, one intermediate frame at 10 fps) to the Home screen. The full HUD (avatar, coins 693, lives 5 Full, stars 4, settings gear, 48/100 event bar, side event buttons, Level 79 Super Hard button, Area 5 7/9 button, bottom tab bar with Home selected) is present immediately - no slide-in visible at 10 fps. |
| 19.4 -> 77 | 23.4 -> 81 | Home screen idle (Area 5 study/library room). Only motion: countdown timers ticking, the left-column "Invite" button toggling in/out. No popup, no offer, no update prompt. |

Screenshot cross-check (run 3): shot-0.2 launcher; shot-3.2 to shot-7.9 dream splash; shot-9.2, 12.1, 14.9, 17.8 Royal Match loading; shot-22.3 onward Home. Runs 1 and 2 (screenshots only): dream splash from 3.5 s / 3.6 s, Royal Match loading first captured at 9.2 s / 10.9 s, still loading at 18.3 s / 20.3 s, Home first captured at 22.3 s / 24.6 s. So on this device the Home screen is reached roughly 19-24 s after launch, of which ~7.2 s is the dream splash and ~10-12 s is the "Loading" screen.

## Popups seen

None in any of the three runs during 75+ s on the Home screen: no daily offer, no update prompt, no "tap to continue", no rate-us. (Caveat: this account had already been through its session earlier today, on the Shop screen at 17:48; a first launch of the day, or a different account/level, may show popups that did not appear here.)

## Was a tap required to reach Home?

No. The app went splash -> loading -> Home on its own. BACK was never pressed and no input was sent.

## Caveats

- Installed build 38174 differs from the 32984 reference APK; the startup order and assets of 32984 may differ (not verified on device).
- Startup is network-dependent (config/profile fetch during the "Loading" screen); durations will vary with Wi-Fi and device load. This device was also servicing adb screencaps every ~3 s, which slows it.
- Timings are PC-clock based; T0-to-video alignment is good to about +/-0.2 s. Screenshot names are capture-start times.
- The screen recording has no audio; SFX/BGM at startup were not observed.
- Runs 1 and 2 have no video; their timings are screenshot-only and coarser (~3 s).

## Files

- rm-coldstart.mp4 - run 3 screen recording (90 s), starts ~4.0 s before launch.
- shot-*.png - run 3 screenshots (elapsed seconds from T0 in the name).
- capture-log.txt, T0-epoch.txt - run 3 timing log.
- pre-launch-state.png (Shop screen before run 1), pre-run3-state.png (Home before run 3).
- run1-no-video/, run2-no-video/ - earlier runs (screenshots + logs).
- derived/ - contact sheets of the screenshots and of video frames (0.5 s and 0.1 s steps) used to build the timeline.
