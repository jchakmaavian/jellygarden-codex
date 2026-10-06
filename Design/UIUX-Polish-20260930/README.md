# UI/UX polish pass (2026-09-30)

A polish pass over the existing, approved screens: Home, the tabs, the Level Start card, gameplay, the level-end cards,
Areas, Settings and the shops. Nothing was redesigned. The approved Home, gameplay HUD and Level Start card keep their
structure.

## Evidence used

- **Reference motion:** measured frame by frame from `Design/RoyalMatchReference/recordings`, 30 fps at 540x1200.
  - The reference ran under ARM translation, which hides the first ~150 ms of every popup open. Scale ratios and
    frame counts are reliable.
  - `J:\Unity Projects\Match3-Games_ArtSource\Match-3-JC\00_References` was not reachable: the J: drive is not
    connected.
- **Our screens:** inspected in Play Mode in the QA profile at 1080x1920, with captures of every state.
- **Code audits:** of every UI animation, the modal/tap/Back paths and the life timer.

## What the reference does

- **Controls are still while they wait.** Play, Continue, Do It, the Level button and the Area button have no pulse,
  squash or shine.
  - The only idle motion is the tutorial arrow's bob (1.18 s) and the results star's sparkles.
- **Press:** a button shrinks to about 0.93 and holds, then comes back with no overshoot.
  - This pass uses 0.98 with a slight darkening, as the brief asks.
- **Popups:** they appear quickly, with at most a ±1% settle. Closes are instant.
- **Transitions:** screen changes are short crossfades with no black frames.
- **Rewards carry the bounce.** The results star lands big → 1.0, squashes to about 0.7 and rebounds, all in about 0.45 s.

## Issues (most important first)

"Committed" says whether the fix is in this task's commit. The fixes marked "no" sit in files that hold other sessions'
uncommitted card-system work (`RoyalCardKit`, `RoyalCards`, `RoyalCardMotion`, `RoyalLevelCardMotion`, `RoyalMenuPresenter`,
`RoyalSettingsPage`) or depend on it (`RoyalFeaturePage`). They are verified in the working tree and go in when those files
are committed.

| # | Screen | Reproduction | Actual | Expected | Fix | Status | Committed |
|---|---|---|---|---|---|---|---|
| 1 | So Close, life shop, booster shop | Not enough coins, tap CONTINUE / REFILL / buy | Opened the coin shop, whose packs all read "Unavailable" offline | Say why, stay on the card | `RoyalNotice`: "Not enough coins (you have N) / Win levels to earn more" above the button; the coin shop only when purchases exist. The notice leaves with its card | fixed, verified | yes |
| 2 | So Close, Level Failed, win card | Press Android Back | Nothing happened | Back = the quiet choice | Back = END LEVEL / close / CONTINUE (`AnimationManager`) | fixed, verified | yes |
| 3 | Gameplay, levels 1-5 | Tap a padlocked tool | Nothing | Explain the lock | The tool prompt says "Tools unlock at Level 6" (live rule) for 2.2 s | fixed, verified | yes |
| 4 | Areas list | Open the Areas tab | A locked card was a grey box with a padlock | Say why | Caption: "Complete Area N to unlock" / "Unlock it from Home" / "Coming soon" | fixed, verified | yes |
| 5 | Team, Collection | Tap the tab | A small "coming soon" dialog with small grey text | Feature page structure: title → cream card → art → message → action | `RoyalFeaturePage`: the Leaderboard page's structure, Home's tab art, "not open yet", PLAY LEVEL N | fixed, verified | no |
| 6 | Level start | Tap PLAY on the Level Start card | 3.5 s to play, including a 2 s "Your goals" reminder of what the card just showed, not skippable | Less waiting, skippable | The reminder holds 0.35 s less, and a tap skips it; its events are kept, only moved | fixed, verified (2.64 s; 2.02 s with a tap) | no |
| 7 | So Close | CONTINUE or END LEVEL | The board stayed blocked (or Level Failed waited) 0.67 s after the card had faded | Release when the card is gone | OnFinished moved to the end of the fade | fixed | no |
| 8 | Win card | Win a level | The one star sat in the left of three slots; it grew in from 3x linearly over 0.5 s | Centred; land like the reference | Star1 centred; lands 2.4 → 1 in 0.13 s, squash 0.86, rebound 1.05, settled 0.5 s | fixed, verified | no |
| 9 | Gameplay | Choose a tool | Snapped to 1.06x; hard to see which is chosen | A clear selected state | Eases to 1.06x over 0.12 s with a soft gold halo | fixed, verified (1.017 → 1.060 over 0.09 s) | yes |
| 10 | Home | Wait on Home | The + badges swelled every 7 s; the selected tab breathed; "New Area" pulsed | Still controls | All still | fixed, verified | yes |
| 11 | Level Start card | Wait on the card | The booster badges pulsed every 3.2 s; a glint every 6.5 s; PLAY's gloss every 3.4 s | Still controls | No idle pulse, glint or gloss; one sweep still answers a booster choice | fixed, verified | no |
| 12 | Every card | Press any card button | Darkened 16%; PLAY sank 2 units instead of scaling; 0.06 s in / 0.10 s out | 0.98, slight brightness, 0.12-0.18 s | 0.98 and 8% darker for every control, 0.08 s in / 0.14 s out | fixed, verified | no |
| 13 | Level Start card | Tap a booster | Its art popped up to 1.04x | A press | Eases down to 0.98 while held | fixed, verified | no |
| 14 | Settings page | Press a switch row or a button | No press feedback | The same press as elsewhere | The HUD's press (0.975, 88%) on every control | fixed, verified | no |
| 15 | Settings → How to Play | Close the lessons | Landed on Home | Back to Settings | Settings reopens | fixed, verified | no |
| 16 | Areas | Tap Unlock on a new area | The padlock wobbled ±9° for 1.1 s before anything happened | No wobble, no wait | A 0.25 s shrink and fade | fixed; Unlock verified by the Areas suite | yes |
| 17 | Areas chest | Complete an area | The chest hopped and squashed every 1.3 s until tapped | Still; the hint invites the tap | Still | fixed; chest flow verified by the Areas suite | yes |
| 18 | Home info dialogs | Tap coins, lives, stars, profile | The subtitle and details were the default font | The game's type | Display font, navy | fixed, verified | yes |
| 19 | Life shop | Out of lives, tap PLAY | "Next life in 04:59" (Home shows "4:59") | One format | Reads the one timer, m:ss | fixed, verified | yes |
| 20 | Gameplay | Start a level; open the pause menu | After the goals card the blue backdrop and the HUD popped in at once; in pause the HUD vanished at once | Short fades | The HUD and the backdrop fade over 0.25 s; taps follow the state at once | fixed, verified (no black frame: the darkest frame of the start averages 59/255) | yes |

## Checked and kept

- **Life system:** one life per 5 real minutes (`game.unity`: Min 5), from one saved UTC deadline.
  - Offline catch-up is capped at the cap, and the countdown survives a restart. No runtime defect.
  - The Level Maker's "Reset to default" writes 15 minutes: an editor-only trap, reported, not changed.
- **Board input under popups and double taps:**
  - Every popup blocks the board.
  - Every buy, continue and reward closes its popup or sets a guard in the same call.
- **Kept on purpose:**
  - the task tick's overshoot and the coin counter's response (both copied from the reference);
  - the tutorial hand and arrows (guidance);
  - the level-end celebrations;
  - instant closes (the reference's).

## Tests run (editor, 1080x1920, 2026-09-30)

Results are in `checks/`.

- **This pass's driver** (`checks/uiux-driver.txt`): 24/24 pass, no runtime errors.
  - It runs in the QA profile with real clicks, the real Back key and taps.
  - It checks the real save before and after.
- **Existing suites:**

| Suite | Result | Note |
|---|---|---|
| Tool bar (`ReferenceBoosterChecks`) | 36/36 pass | |
| HUD layout (`ReferenceLayoutChecks`) | 12/12 pass | |
| Home finish (`ReferenceHomeFinishChecks`) | 16/16 pass | |
| Home (`RoyalHomeScreenChecks`) | stops at check 108 | Its hit test clicks the legacy "Setting 0" button, which the 09-29 Settings page now covers. With that page switched off at runtime, all 179 pass |
| Areas (`RoyalAreaChecks`) | stops at check 62 | "Area 2 keeps its own tasks on Area 1's placeholder scene": since 3c1a3002 Area 2 has its own art. A copy without that one check passed all 80, Unlock and the real-save check included |
| Dialog review (`RoyalPresentationChecks`) | 20 of 21 pass | "MenuFailed heading visible" looks for `Image/Royal heading`; since 09-29 the Level Failed card's title sits in its ribbon (`Image/Royal ribbon/Royal heading`) |

- **The three failures predate this pass:** each comes from other sessions' 09-29 work, and each check is out of date,
  not the game. The checks were not changed.
- **Real save:** each run compares its game values before and after (level, lives, coins, boosters, sound), and none
  changed them.
  - Correction (later on 2026-09-30): the dialog review runs in the real profile. Opening the coin shop, the life shop
    and the So Close offer turns three rewarded-video day counters (`GetGems`, `GetLifes`, `GetGoOn`). They were put
    back to their 11:14 values. This build has no ads, so they had no effect.
  - It did change between 12:02 and 13:24, while this pass was waiting for the shared editor: level 33 won, coins
    186 → 196, 2 Cannons, lives full.
  - None of this pass's runs was playing then, so the change was left as it is.

## Evidence

- `screens/`: before/after pairs (WebP, 405x720 per side): Home, Areas captions, Team, Collection, the info dialog, PLAY
  press, the chosen tool, the win star. `09-new-states.webp` shows the new states.
- `recordings/`: normal-speed clips, 30 fps, recorded in the game at the end of each frame:
  - level start and a tapped skip;
  - the Team and Collection pages;
  - choosing a tool;
  - So Close without coins, then Back;
  - the win card.

## Remaining

- **Timed levels:** the clock keeps running while the in-game tool shop is open and for the 0.35 s before the pause
  menu takes effect.
- **Tools:** after a tool is used, the board stays locked until every cascade settles.
- **Win flow:** on levels 6+, the next level's card opens by itself after the win's CONTINUE (existing behaviour).
- **Hitches:** not measured cleanly.
  - The driver logged 24 editor frames over 50 ms, most of them 110-160 ms.
  - One of them came while Home sat idle, so they are not attributable to the UI. The run was also capturing, recording
    and being driven through the editor.
  - A run without captures, and profiling on a device, are still to do.
- **Not tested:** a physical Android phone was not connected, so device performance is not measured. Editor frame times
  are not device evidence.
