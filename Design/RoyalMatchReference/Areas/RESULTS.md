# Areas: validation results (2026-09-28)

## The main project
The suites ran in the shared editor (JellyGarden-Codex, Unity 6000.5.10f1), in the QA test profile. The editor was taken only
after the user's handoff: the startup session and the Codex coin agent were idle, and the user decided "Areas first" at 16:17.
Result files are in `tests/`.

| Suite | 720x1612 (the reference phone) | 1080x1920 | 1536x2048 (tablet) |
| --- | --- | --- | --- |
| RoyalAreaChecks (the Areas loop) | 84/84 passed | 84/84 passed | 84/84 passed |
| RoyalHomeScreenChecks | 177/177 passed | 177/177 passed | 177/177 passed |
| ReferenceHomeFinishChecks.Run(false) | - | 16/16 passed | - |
| ReferenceHomeSkyChecks | - | 8/8 passed | - |
| Persistence (two tasks built, Play Mode restarted) | 4/4 passed | - | - |
| First-visit guide (level 1 and 2 won in the QA profile) | 9/9 passed | - | - |

What the runs found and fixed:
- The object check ran during the reference's deliberate empty moment before the rise. It had passed in the clone only because
  its screenshot write to the slow external drive delayed it. It now checks that the scene is empty, then that the object
  rises between 1.6 and 2.8 s after the tap (measured 2.13 s; the reference's puff is at 2.4 s).
- On 16:9 and tablet screens, Unlock, Continue and the Coming Soon note sat on or under the tab bar (A-24). Four new
  checks now require every panel to sit above the tab bar, at all three sizes (82-326 px clear).
- The guide's tap opened the ordinary list; its caption was unreadable; the Area card was dimmed (A-23).
- The chest's rocket would have turned into a coin under the new coin flights of 50a4d1e3 (A-26).
- The phone found one more (A-27): the guide's dim did not cover the tab bar there. Fixed; the guide flow checks it, and
  the phone was re-checked on the rebuilt APK (18:21). The area suite (84/84) and the home suite (177/177) were run again
  at 720x1612 on the final code.

Compile: the working tree compiles in the editor (recompile_status: no errors). The commit's own tree also compiles on its
own, with none of the other sessions' uncommitted or untracked files: Unity's response files, HEAD + the Areas files, 0 errors
in Assembly-CSharp and Assembly-CSharp-Editor.

## The real save
`tests/real-save-comparison.txt`: the real save was exported before the first run (16:17) and again at the end.
- Every player key is identical: areas, levels, stars, coins, boosters, lives, OpenLevel, settings.
- The QA test profile was also put back as it was at the start.
- Four keys differ. Each is written on every entry to or exit from Play Mode, by any session:
  - the game's `DateOfExit` exit timestamp (it already held another session's 16:16 value before this work);
  - Unity's `unity.player_session_count`, `unity.player_sessionid` and `unity_connect.session_id`.

## Timing against the reference (from the Do It tap; `compare/`)
| Moment | Reference | Ours |
| --- | --- | --- |
| Counter drops as the star leaves | 0.13 s | 0.03 s |
| List closes | 1.2 s | 1.15 s (suite timer; 1.20 s in the recorded frames) |
| Controls away | 1.5-1.9 s | 1.15-1.5 s |
| Puff and the object | 2.4 s, built by 3.9 s | 2.13 s, risen by 3.4 s |
| Controls back | 5.4-5.7 s | 4.9-5.3 s |
| Streak lands, card moves on | 5.7-6.6 s | 5.3-6.1 s |
| List reopens on the done row | 6.9 s | 6.6 s (recorded frames) |
| Next task in | 8.0 s | 8.0 s |

## Shared result files
The suites write into shared folders that other sessions also use:
- `Design/Branding/Home-Unity`, `Design/ReferenceHomeFinish` and `Design/HomeSky` were put back byte for byte as they were
  before the runs, including another session's uncommitted `checks-1080x1920.txt`.
- `Design/Areas/Unity` is back to HEAD, except its `checks.txt`, which is this work's result.

## The phone (17:33-18:01)
Setup:
- The phone: Infinix X665B, Android 11, 720x1612.
- The build: `RoyalThrone-Areas-Dev.apk`, the main tree at 17:16, a development build with the Mod Menu.
- Installed with `adb install -r`, over the existing app and with its data kept. This is the persistence test, not a
  clean-install startup test.
- Everything that spends ran in the game's own isolated Test profile (Mod Menu).

Video: `device/phone_areas_run.mp4`, 23 minutes, downscaled. Stills: `device/stills/`. Sheet: `device/sheet_phone_run.jpg`. Two
heads-up notifications from other apps appeared during the run; they are blurred in the video and in the two stills.

| Check | Result |
| --- | --- |
| Touch: every control by real taps (Do It, the red close, a tap outside, Back, the Area card, the chest stage, the tabs, View, the red X) | passed |
| Safe areas: the HUD under the status bar; every panel above the tab bar | passed |
| First win in the Test profile (level 1): the guide over the Area card, the guided list without close, the arrow at Do It | passed. A-27 (the tab bar stayed bright) was fixed; re-checked at 18:23 on the rebuilt APK: the tab bar dimmed, and a tab tap blocked (`device/phone_guide_fix_recheck.mp4`) |
| Construction: the star, the close, the controls away, the rise with its puff, the streak, the reopen on the next task | passed (tasks 1-6, in order; stars 14 -> 13 -> 12 -> 10 -> 8 -> 6 -> 4) |
| The closes: X, a tap outside, Back | passed; Back did not leave the game |
| Restart: force-stop and relaunch | passed. The real profile still read 0/6, 11 stars and Level 6. The Test profile read 1/6 with the keep built. |
| The last task: the chest (Tap to Open), 20 coins + a rocket (it keeps its face), Area Completed! with King Venguard, the restored area with Continue | passed; coins 50 -> 70, paid once |
| Home after it: "Area 2 Soon"; the Coming Soon panel with its note above the tab bar | passed |
| Scrolling: the Areas tab, swiped to Area 1's "Completed!" card; View replays the area task by task; the red X returns to the tab; Home tab | passed |
| Return to the real profile (Test profile off): Level 6, 0 coins, 11 stars, 5 lives, Area 1 0/6 | passed |
| Crashes, ANRs | none (the system's DropBox has no crash, ANR or tombstone entry; the game ran from 17:47 to the end) |

The phone's real save:
- **Before:** the file was backed up at 15:23 and checked again at 15:48 and at 17:33. `shared_prefs/…v2.playerprefs.xml`:
  1509 bytes, md5 07bd4eeab7614ba51d92eb3a41318b63, sha256 d29e50bb…4a92, OpenLevel 6, stars 3/3/2/2/1 (11), no area keys.
- **After the test:** every Area key, the coins and the level star were under the Test profile's prefix. The only real keys
  that changed were Unity's two analytics session ids. No real `RoyalThrone.Area.*` key existed.
- **Restored:** the game was force-stopped and the original file written back. It is byte-identical (size, md5, sha256,
  `cmp`), with its owner, mode 660 and even its mtime (2026-09-23 18:58:14) back. It is alone in `shared_prefs`, with no .bak.
  The game was not launched again afterwards.
- **After the re-check (18:21-18:25):** the fixed APK went on with `install -r` again. Before it, the save was byte-identical
  to the original. The Test profile ran one level-1 win and the guide. Again only Unity's two session ids changed among the
  real keys. The original was restored again, byte-identical (md5 07bd4eea…, `cmp`).

Observed on the phone, outside the Areas code: after a win on level 6 or later, CONTINUE loads the next level and opens its
card (AnimationManager.CloseMenu: HEAD, plus another session's uncommitted opening rule for levels 1-5). With that card over
Home, the task list's auto-open and the first-visit guide cannot show after those wins. The level flow was not changed here
(unrelated level rules).
