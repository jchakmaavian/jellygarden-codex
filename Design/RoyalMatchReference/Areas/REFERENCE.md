# Royal Match Areas: what the reference does (observed 2026-09-28)

**Sources.** The official Play build of Royal Match (com.dreamgames.royalmatch 38174):
- the user's phone (Infinix X665B, Android 11, 720x1612): profile "test 1", Level 85. It started at Area 5 "Library" 8/9
  with 7 stars;
- the dedicated reference emulator (Android 14, 1080x2400, ARM translation): Area 1 "Castle" 3/6 with 1 star, plus this
  morning's recordings of its first three builds.

The backup APK `Design/Backup/apk/royal_thron_rfv387.apk` was not installed or run: it is tampered (see PROGRESS.md in the
parent folder).

**What was spent.** Exactly 3 stars on the phone, approved by the user at 12:47. That was one "Do It ★3", which completed
Area 5, opened its chest and unlocked Area 6. No level was played on the phone. On the emulator, one short-of-stars tap was
made, which spends nothing by design. An age gate appeared there, and the year chosen is not known because a scroll picked it.

**Timings.** Phone timings are native, at 33-60 fps. Timings marked "emu" run under translation and are slower. Times are
seconds from the tap.

## Home
- **Background:** the current area's scene.
- **Buttons:** bottom left is Level N; bottom right is an orange "Area N" card with a green n/N bar and a purple chest
  (`p01`).
- **Red "1" badge:** seen on the emulator's Area card with ★2 in hand for a ★2 task. Not seen on the phone with ★7 for a ★3
  task. The rule is not verified.
- **Swiping:** a horizontal swipe on Home pages between the tabs.
- **Built objects:** tapping one visibly does nothing (`a10`).

## The task list (`p02`)
**Layout:**
- A tall blue panel with a 45-unit gold and orange rim and a drop shadow.
- An arched blue ribbon carries the area name in cream letters with a navy and gold edge. Its edges drop about 61 units at
  the ends: `38.9 v^2 + 23.9 v^4`.
- A red round close button (100 units) sits at the ribbon's right end.
- The Area Progress card shows a heading, a green bar with n/N, and the purple chest.
- **Exactly one task row:** the next task in order. It has an icon, a name, and a green "Do It ★N" button.
- The rest of the panel is empty.

**Opening and closing:**
- It opens at full size and settles in about 0.2 s (`open1`).
- X, Back and a tap outside each close it within 1-3 frames (`close1`, `p04`-`p06`).

**While it is open:** the star counter stays bright above the dim. The tab bar is dimmed.

**The chest:** tapping it shows a white bubble listing the area's rewards, joined by "+" (`p03`). A tap elsewhere hides it.

## Building an ordinary task (emu `b1`, `b1star`, `b3`; Area 1 tasks 1 and 3)
| t | What happens |
| --- | --- |
| 0.13 | The star counter drops as a star leaves it. The star arcs left and down past the title, then in to Do It, trailing sparkles. For ★2 there are two stars. |
| 0.8 | It lands. Do It flashes, then shrinks away in about 0.2 s. |
| 1.2 | The panel is gone in one frame. |
| 1.5-1.9 | The top bar slides up. The Level and Area cards and the tab bar slide down. |
| 2.4 | The scene is empty for a moment. A puff appears at the spot. |
| 2.7-3.9 | The object builds: the castle's parts rise and drop in. |
| 3.9-5.4 | It holds. |
| 5.4-5.7 | The HUD comes back. |
| 5.7-6.6 | A bright yellow streak runs from the object to the Area card, whose count moves on. |
| 6.9 | The list reopens on the done row, which has no button. |
| 7.2 | A green tick pops, overshooting. |
| 8.0 | The row fades out. |
| 8.4 | The next task's row appears. |

On the phone at native speed the list was back 6.25 s after the tap. On the emulator it took 7.0 s.

## The area's last task (phone `pb`, `pstar`, `ctr`; Area 5 "Set up train set" ★3)
- **At the tap:** no star flies, and the counter stays at 7. Do It shrinks (7.8-8.0), and a big green tick pops over the
  row (8.1), then sparkles.
- **The build:** the panel goes at +1.3 s and the HUD leaves. The object is built. The counter reads 4 when the HUD
  returns; the stars were taken while it was away.
- **The list reopens with "Area Completed!"** in place of a task. The tick pops again and the row leaves. The bar fills 8/9
  to 9/9.
- **The chest:** it pops from the list to the middle of the screen and hops about every 1.3 s. "Tap to Open" appears after
  about 1.2 s (`p15`).
- **Opening it:** the chest opens with light, and the rewards burst out in an arc (Area 5: 250 coins, 4 cards and 8
  boosters). "Tap to Continue" (`p16`) sends the rewards to their counters: coins went 706 to 956.
- **A card pack follows** ("Your Cards", Tap to Open, Tap to Claim; `p17`, `p18`). This is the Collection feature.
- **"Area Completed!":** a purple ribbon under gold trumpets, with confetti. The King rises from the bottom giving two
  thumbs up. The HUD is gone (`p19`).
- **The restored area** is shown alone, with "Continue" (`p20`). Home follows, with the Area card reading "New Area"
  (`p21`).
- **"New Area!":** the next area's name on a gold header, over its scene under a padlock, and a green Unlock (`p22`).
- **Unlock:** the padlock wobbles for about 1.1 s. The new area appears with a gold sparkle ring (`unl`). The Area card
  reads "Area 6 0/8" (`p23`). The list does not open by itself.

## Short of stars (emulator `e01`-`e04`)
- The list looks the same as when the task is affordable.
- Tapping Do It swaps the list for "Not Enough Stars!" in about 0.37 s (emu). It shows a board-to-star picture, "Beat levels
  to earn stars" and Continue.
- Continue opens the next level's card (goal, padlocked boosters, Play).

## The Areas tab (`p08`-`p13`, `p25`-`p30`)
- **The first tab is "Areas".** It is a full-screen list with a blue title bar and cards 602 wide on a 653-unit pitch,
  newest first:
  - **locked:** a grey tab, a grey ribbon, a grey scene and a steel padlock;
  - **current:** a red ribbon, the unrestored scene and a progress bar;
  - **restored:** "✓ Completed!" and a green View button.
- **How many locked cards:** at Area 5 the list showed two locked areas above the current one. At Area 6 it showed ten
  (up to Area 16), probably as their previews downloaded. There is no fixed rule.
- **Scroll pills:** translucent "Top" and "Bottom" pills appear while scrolling.
- **View** replays the area's restoration, task by task over about 6.5 s. A red X sits at the bottom right. X and Back return
  to the tab.
- **Profile:** it shows an "Areas Completed" count (4). Tapping it does nothing.

## First visit (this morning's emulator run, after Level 1)
- The Home dims, with a bubble and a bouncing arrow on the Area card.
- The first list has an arrow at Do It and no close.
- After Levels 2 and 4 the list opened by itself: the next task was affordable. After Levels 3 and 5 it did not.

## Not observed
- Area 1's chest contents;
- a next area without art (the reference always has one);
- any story or dialogue;
- what a tab tap does while the list is open;
- the badge's rule.
