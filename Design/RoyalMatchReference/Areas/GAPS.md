# Areas: reference versus Royal Throne, gap list

Reference evidence is in REFERENCE.md and `reference/`. "Ours before" is the committed state before this work, captured at the
phone's 720x1612 viewport in the QA profile (`ours_before/`). Priority: P1 = the core loop, seen every task; P2 = completion,
navigation and secondary states; P3 = polish.

Status:
- **VERIFIED**: built, and checked in the main project by the named acceptance test at 720x1612, 1080x1920 and 1536x2048 (the
  tablet). The results are in `tests/`.
- **VERIFIED, differs**: works, with a remaining difference that is noted.
- **NOT DONE**: deliberately left; see section 5.
- **MISSING ART**: needs artwork that does not exist; see section 4.

## 1. UI and layout
| ID | Reference evidence | Ours before | Missing / different | Pri | Fix | Acceptance test | Status |
| --- | --- | --- | --- | --- | --- | --- | --- |
| A-01 | p02: tall blue panel with a 45-unit gold and orange rim and a shadow; arched blue ribbon with the area name; round red close, 100 units, at its right end | ivory card, chest icon, navy title and subtitle, square blue close | look and hierarchy | P1 | RoyalAreaKit.BluePanel, Ribbon (RoyalArchGraphic), RedClose, drawn in design units measured at 720 wide | "The red close receives its tap"; "The Home card gives way to the area panel" | VERIFIED |
| A-02 | p02: cream Area Progress card: heading, green n/N bar, purple chest | navy bar, own chest | card, heading, bar | P1 | CreamCard, ProgressBar, the Home card's chest (16593) | "The Area card still shows the old count"; "The streak lands and the Area card moves on (1/6)" | VERIFIED |
| A-03 | p02, p24, e01: exactly one row, the next task: icon, name, Do It with the cost | 3x4 grid of every task | one row, Do It | P1 | one row bound to RoyalAreas.NextTask; the task's object as its icon | "The list shows one row: the first task"; "Do It receives its tap" | VERIFIED |
| A-04 | p02: nothing else in the panel | All areas and Back to home buttons | extra buttons | P1 | removed (the area list is the Areas tab, A-20) | no Home-card controls under an area dialog | VERIFIED |
| A-05 | p06: a tap outside closes; the tab bar is dimmed | the veil only blocks; bright tabs | tap-outside close, dim over the tabs | P2 | Outside button in the area layer; a visual-only dim over the tab bar (our tabs stay usable: the Home's existing rule) | "A tap outside closes the panel" | VERIFIED |
| A-06 | p02: the star counter stays bright above the dim | counter dimmed | counter emphasis | P3 | not done (section 5) | - | NOT DONE |
| A-07 | p03: tapping the chest shows its rewards in a bubble | nothing | tooltip | P2 | bubble with the area's own rewardCoins + rewardBoost (amounts unchanged) | "Tapping the chest shows its rewards"; "Tapping it again puts them away" | VERIFIED |
| A-08 | open1, close1: opens with a 0.2 s settle; closes in 1-3 frames | instant open, instant close | open settle | P3 | 0.96 to 1 over 0.2 s on open; close unchanged (instant) | recorded frames (`ours_after/`, `compare/`) | VERIFIED |
| A-16 | e02, e03: "Not Enough Stars!" (board-to-star picture, "Beat levels to earn stars", Continue to the level card) | "1 more star needed", "Play level N" | presentation | P2 | Short panel rebuilt as the reference; Continue = OpenNextLevel | "Short of stars opens Not Enough Stars!"; "No task is built without enough stars, and the balance stays at zero"; "Continue receives its tap" | VERIFIED |
| A-20 | p08-p11, p25-p29: the first tab is Areas: full-screen list, newest first; locked (grey, padlock), current (red ribbon, bar), restored (Completed!, View) | tab "Events" opened the level list; a small 2-column list behind "All areas" | Areas tab | P1 (user's decision 12:47) | the left tab opens the Areas page (RoyalAreaView.List); every catalogue area as a card; opens on the current one | "The first tab is Areas"; "It lists the eight areas to come (locked), the current one and the restored one (10)"; "View on restored Area 1 receives its tap" | VERIFIED |
| A-21 | p12, p13, p30: View shows the area full screen, replays its restoration (~6.5 s), red X bottom right, Back closes | static restored scene, title banner, "Back to areas" | replay, red X, no banner | P2 | RoyalAreaScene.Replay; red close bottom right; Back; the HUD and tabs slide away | "View shows Area 1 full screen and replays it"; "Its close returns to the Areas tab"; `compare/view_replay.jpg` | VERIFIED |
| A-24 | (found in the main-project runs) the panels fit on the reference phone; on 16:9 and 3:4 screens nothing may sit under the tab bar | (new UI) Unlock crossed the panel's rim at 16:9; Unlock, Continue and the Coming Soon note went under the tab bar on a tablet | short-screen layout | P1 | LayoutPanel: each panel's content height; on a short screen the top margin goes first, then the panel scales to fit. 720x1612 unchanged | new "... sits above the tab bar" checks (4 panels, 3 sizes); "Unlock receives its tap" on the tablet | VERIFIED |

## 2. Rules and state transitions
| ID | Reference evidence | Ours before | Missing / different | Pri | Fix | Acceptance test | Status |
| --- | --- | --- | --- | --- | --- | --- | --- |
| A-15 | only the next task is ever offered (p02, p24, emu builds 1-3 in order) | any task in any order | order | P1 | RoyalAreas.Build accepts only NextTask | "A later task cannot be built before the next one" (nothing spent); "Task N is the one on offer" | VERIFIED |
| A-17 | a task's stars go once | Build + Building guard | re-test under the new flow | P1 | Do It disabled at the first tap; Build refuses a done task | "Do It spends one star, once, however often it is tapped" (5 taps in one frame); "Six tasks spend exactly the area's ten stars" | VERIFIED |
| A-19 | p15-p23: chest Tap to Open, rewards Tap to Continue, (card pack), Area Completed! with the King, restored area + Continue, Home card "New Area", New Area! + Unlock, the new area; no auto-open | "restored!" dialog with Collect, then "New area" or "coming soon" | the completion sequence | P2 | chest ceremony (pays once at the open), banner with King Venguard's thumbs-up, showcase, RoyalAreas.Entered + Enter() for New Area and Unlock; without art the next area reads Coming Soon | "Opening the chest pays 20 coins and a rocket"; "The chest cannot pay twice"; "Repeated taps on the chest pay nothing more"; "Unlock enters Area 2, once"; persistence across a restart | VERIFIED (King Venguard: working tree only, section 5) |
| A-23 | this morning's run: the first visit points at the Area card; the first list has no close and an arrow at Do It; later wins open the list by itself when affordable | the list opened by itself every time | first-visit guidance | P3 | RoyalAreaView.GuideFirstTask (once, RoyalThrone.Area.Guided, per profile). Fixed in the main runs: OpenPanel's Hide() cleared the guided flag (the list opened with its close); the caption's edge drew over its fill (unreadable); the dim covered the card | guide flow (`tests/guide-flow.txt`): L1 win shows the guide, its tap opens the list without close, a real outside tap does not close it, Do It ends it; L2 win opens the list | VERIFIED |
| A-27 | (found on the phone) the guide dims everything but the Area card, the tab bar included | (new UI) on the phone the tab bar stayed bright and tappable during the guide; the editor dimmed it. The guide's own canvas did not sort above the tab bar on the device | tab bar dim | P3 | the tab bar gets its own dim inside its canvas while the guide shows (as the task list's navDim), which also takes the tabs' taps; the guide's lower dim stops at the tab bar, so nothing is dimmed twice | guide flow: "The tab bar is dimmed too, and a tap on a tab lands on its dim"; the phone at 18:23: the tab bar at 43 (95 before the fix), and a tap on Areas left the guide up (`device/guide_tab_bar_before_after_fix.jpg`) | VERIFIED |
| A-26 | (found in the main-project runs) a reward keeps its own face while it flies | (new UI) since 50a4d1e3 every turning flight swaps its image for coin frames, so the chest's rocket would have become a coin | booster flight | P2 | AreaReward: coins turn, a booster flies flat | chest checks; `ours_after/07_chest_rewards_tap_to_continue.png` | VERIFIED |

## 3. Animation and VFX (times from the Do It tap; `compare/`)
| ID | Reference | Ours before | Ours after | Status |
| --- | --- | --- | --- | --- |
| A-09 | the counter drops at 0.13 s as a star leaves it; arcs to Do It by 0.8 s, sparkle trail, one per cost | a star from the tile to the counter | RoyalWinSequence.AreaStar: counter to Do It, 0.14 s stagger, 0.66 s flight; the counter counts down on each departure (0.03 s after the tap) | VERIFIED |
| A-10 | Do It flashes and shrinks; the panel goes in one frame at 1.2 s | list closed after the flight | flash 0.1 s + shrink 0.2 s + 0.15 s bare row; instant close at 1.15 s | VERIFIED |
| A-11 | the HUD slides off (~0.35 s) and back | HUD stayed | ReferenceHomeLayout.HudAway, 0.35 s each way | VERIFIED |
| A-12 | an empty moment, a puff at 2.4 s, the object builds in parts (~1.2-1.5 s), holds ~1.5 s | 0.35 s fade + beam | the scene waits empty, a puff, the object rises from its base with a small overshoot (1.3 s) at 2.13 s, holds 1.5 s | VERIFIED, differs: one picture rises where the reference assembles parts (MISSING ART) |
| A-13 | a bright streak to the Area card, whose count moves on as it lands | count moved at once | RoyalWinSequence.AreaStreak (0.8 s); RefreshProgress on landing + card pulse | VERIFIED |
| A-14 | reopens on the done row; tick pops; row leaves; next row (8.0 s after the tap) | grid reopened | reopen, tick pop (overshoot), row out, next row in (8.0 s after the tap) | VERIFIED |
| A-14b | last task: no star flight, tick before the close, the list reopens with "Area Completed!" over the ticked row, the row leaves, the bar fills, the chest pops to the middle | chest dialog | as observed, except the order at the reopen (`compare/last_task.jpg`) | VERIFIED, differs: ours reopens 0.4 s later and shows "Area Completed!" after the row leaves, about 1.6 s later than the reference (P3, open) |

## 4. Missing artwork (no credits approved; placeholders are not finished art)
- Areas 2-10: scenes, task objects, thumbnails (about 1,410 Magnific credits, see the areas memory). Area 1's unrestored scene
  is still a PLACEHOLDER.
- An opened-chest sprite: we keep the closed purple chest with light and a burst.
- Multi-part construction pieces: the reference assembles each object from parts; ours raises the single task picture.
- Trumpets for the Area Completed! banner: our banner is a ribbon, confetti and King Venguard.
- Area-specific task icons: the task's own object picture is used as its icon.
- Locked-area previews on the Areas tab: the reference shows each locked area's art in grey; ours shows a plain grey card for
  areas without art.

## 5. Observed but not applicable, not done, or not observed
- Card packs from the chest (p17, p18): our game has no card collection; not applicable.
- King Venguard in the Area Completed! banner: his character files have never been committed (another session's work), so the
  commit's banner has the ribbon and confetti only; his lines stay in the working tree until his files are committed.
- Red "1" badge on the Area card: rule NOT VERIFIED (seen once on the emulator, not on the phone); not implemented.
- Number of locked cards on the Areas tab: 2 at Area 5, 10 at Area 6 in the reference; ours lists every catalogue area.
- "Top"/"Bottom" scroll pills on the Areas tab: observed, not implemented (P3).
- A-06 star counter above the dim: not done (P3).
- A-14b order at the last task's reopen: see section 3 (P3).
- After a win on level 6 or later, CONTINUE opens the next level's card. That is the level flow, which was not changed here.
  So the task list's auto-open and the first-visit guide only follow wins that stay on Home (now levels 1-5).
- NOT OBSERVED: Area 1's chest contents, a next area without art, story/dialogue, a tab tap while the list is open, Back on the
  guided first list.
