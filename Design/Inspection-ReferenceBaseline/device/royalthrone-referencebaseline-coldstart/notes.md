# Royal Throne ReferenceBaseline APK - device cold start (2026-09-23 18:41 local)

APK: Design/Backup/apk/RoyalThrone-ReferenceBaseline-2026-09-23_1840.apk (installed with adb install -r over the existing com.codexsandbox.jellygarden QA app; data kept).
Device: Infinix X665B, Android 11, 720x1612, USB powered, battery 12-13%.

shot-<elapsed>.png = passive screenshots from am start (T0 in T0.txt), ~3 s apart because screencap is slow on this phone.
  0.4-3.8 s dark engine background; 6.1 s Made with Unity splash; 8.0 s onward Royal Throne title with PLAY (stable to 79.7 s). No input sent.

p2-*.png / rt-play-to-home.mp4 (18:43-18:44): interactive phase. At p2-00 the app was ALREADY in level 6 (moves 8 -> 7 -> 6, ice goals 5 -> 3) with no scripted input: the user was playing on the phone. Only one tap was issued (360,1395; on the board frame, no effect observed) and the phase was aborted. These frames document a real level running on the device build.

Not tested on device: PLAY->loading->home transition timing, settings dialog, Mod Menu, booster activation (all verified in the editor instead). The first cold-start screen recording failed (Git Bash rewrote /sdcard); use MSYS_NO_PATHCONV=1.
logcat -s Unity:E showed no exceptions during startup.
