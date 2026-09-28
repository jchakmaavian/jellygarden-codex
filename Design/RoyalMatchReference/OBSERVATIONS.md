# Royal Match reference: observed journey (official Play build 38174)

Captured on 2026-09-28 on the dedicated emulator `RoyalThrone_Reference_4K`: Android 14 x86_64, 4 KB pages, ARM64 through
`libndk_translation`, one CPU core visible to Unity. The game runs under translation, so frame pacing is slower than on a
phone. Timings below are measured from the continuous recordings; treat them as approximate (about ±0.5 s at 2 fps
sampling unless stated).

Recordings are in `recordings/journey_NNN_HHMMSS.mp4`: 180 s clips with game audio, 540x1200, 30 fps, re-encoded H.264 and
AAC from the emulator's own recorder. Clip start times are listed in `recorder.log` in the session scratchpad and below.

## Startup (clip 000 onward)
Sequence: Dream Games splash (about 60 s under translation) → key art with a "Loading" label → Terms of Service notice
(Agree) → **straight into Level 1, with no Home screen first**.

## Level 1: observed
- **Board:** 9 columns x 8 rows. The top 4 rows hold pieces; the bottom 4 rows are all boxes, 36 boxes in all.
- **Goal:** Target Box 36. **Moves:** 30.
- **Colours:** red book, blue shield, green leaf, yellow crown (4).
- **Tutorial:**
  - The first move is highlighted: the red at (row 2, col 5) swaps down with the green below.
  - The bubble reads "Make a match next to a BOX to break it!" and a hand points at the move.
  - After that move, no further forced tutorial steps were seen.
- **Boosters:** all 4 in-level booster slots show padlocks (locked).
- **Box rule:** a match next to a box breaks it (1 hit). The piece above falls into its cell.
- **Win (bot):** 14 moves, 16 left. Bot log: `scratchpad/refapk/bot_L1.log`.
- **Finale timeline** (clip 001; t = seconds into the clip):
  1. 128.0-130.0: the last move's power chain clears the final boxes; Moves still shows 15.
  2. 130.5: **the board dims and the celebration starts before the leftover moves are used.**
     - 131.0: a blue top banner.
     - 131.5-133.0: the game logo lockup drops in, with blue pennants hanging from the top, gold trumpets rising at the
       lower left and right, and firework bursts behind the logo.
     - Total about 3.0 s.
  3. 133.5: the board returns. The HUD's left panel title changes from **"Target" to "Reward"** and shows a coin counter.
  4. 134.0-135.5: **the leftover moves launch**. White streaks fly from the Moves panel to random board cells, one per move.
     The Moves counter drops 15 → 7 → 1 → 0 in about 1.5 s (about 0.1 s per move). Each hit cell becomes a rocket.
  5. 135.5-147.5 (about 12 s): **the booster finale**. The planted rockets fire in sequence across the board, with chained
     explosions and gold coin bursts. Coins fly to the Reward counter, which ends at **64**.
  6. 148.0: **the results card appears**, titled "Level 1".
     - A large star flies in over the card and shrinks onto a red cushion.
     - 148.5: "Well Done!" is written in.
     - 149.0: the card is complete, with a Continue button and a red close X.
     - No score is shown, and there are no 3 stars: **one star per level**.
- **Continue** (clip 004, about 02:52:40):
  1. A "Loading" screen with a tip card: "Rocket", four in a line → rocket.
  2. Home: the coin counter goes 2000 → 2064 (the +64 is shown flying from the lawn to the counter). The star flies from the
     middle of the scene to the star counter, 0 → 1.
  3. First time only: the scene dims with a bubble, "You can open your task list from here.", and a bouncing arrow over the
     "Area 1 0/6" button.
- **Starting balances:** 2000 coins, 5 lives (Full).

## Areas: observed
- **Home:** the background is the area scene. Bottom left: a green "Level N" button. Bottom right: an orange "Area 1 n/6"
  button with a chest. It shows a red badge "1" when a task is affordable. The bottom tab bar has a Home tab in the middle.
- **Task list** (title "Castle", the area name):
  - An "Area Progress" bar (0/6) with a purple chest (the area reward).
  - **One task row at a time** with an icon, a name, and "Do It" plus a star cost.
- **Castle** (Area 1, 6 tasks). Observed so far:
  1. "Build castle" ★1.
  2. "Build the fountain" ★1.
  3. "Install gazebos" ★2.
  - Tasks 4-6 are not reached yet.
- **Doing a task:**
  1. The star flies from the Do It button to the star counter, which counts down.
  2. The panel closes and the scene shows the task spot. The new object appears with a white sparkle beam; the castle
     rises out of a bare lawn.
  3. About 2 s later the task list reopens by itself, showing the next task and the progress count (1/6, then 2/6).
- **After a win with a star in hand**, the task list opens by itself on arriving Home. The "+coins" flight plays first.

## Level 2: observed
- 9x9. Boxes fill a staircase in the lower left (36). Moves 27.
- The level card shows "Level 2", Goal Box 36, "Select Boosters:" with 3 padlocked slots, and Play.
- Tutorial: a highlighted 4-in-a-row move (crowns) that makes a Rocket.
- Won by the bot in 17 moves. Reward counter 50. The results card is the same one-star card.

## Level 3: observed
- 9x9. The goal is 25 two-layer boxes, drawn as wooden chests; one hit turns a chest into a normal box. Moves 27.
- Tutorial: a highlighted T-shaped move, a dotted outline with no caption. Crowns at (4,6), (4,8), (5,7) and (6,7); the
  shield at (4,7) swaps up and makes **TNT** at the stem.
- Won by the bot with about 1 move left. Reward 44.

## Level 4: observed
- 9x9 with a new indoor background (hall with pillars). The goal is **Grass 49**, a 7x7 grass square under the pieces in
  the middle. Moves 30. Five colours: the pink gem joins.
- Tutorial: the board outside the grass is dimmed, with the caption "Make a match on the GRASS to remove it!". There is no
  forced move; the first match dismisses it.
- A match on grass removes the grass under the matched pieces.
- Won on the last move. Reward 13.

## Level 5: observed
- 9x9 with holes (a cross-shaped gap), indoor. The goal is **Grass 73**. Moves 30. Four colours: blue, red, crown, gem;
  no leaf.
- After 3 moves, a **combo tutorial**: a TNT and a Propeller side by side are highlighted with a hand, captioned "You can
  swap two power-ups to make a more powerful explosion!". Swapping them fired the combined blast; grass went 36 → 20 in
  one move.
- Won. Reward 63.

## Level-to-Home tips (loading screen after each win)
| After | Tip |
| --- | --- |
| Level 1 | Rocket (4 in a column → rocket) |
| Level 2 | Rocket |
| Level 3 | TNT (L shape → TNT) |
| Level 4 | Propeller (2x2 → propeller) |
| Level 5 | Light Ball (5 in a row → light ball) |

## Rewards and balances
| Level | Moves (start) | Won with | Reward coins | Coins after | Stars after |
| --- | --- | --- | --- | --- | --- |
| 1 | 30 | 16 left | 64 | 2064 | 1 (spent on the castle) |
| 2 | 27 | about 10 left | 50 | 2114 | 1 (spent on the fountain) |
| 3 | 27 | about 1 left | 44 | 2158 | 1 |
| 4 | 30 | 1 left | 13 | 2171 | 2 (spent on the gazebos) |
| 5 | 30 | several left | 63 | 2234 | 1 |

The coin amount is not a simple function of moves left; the formula is **not inferred**. One star per win, always.

## Areas: Castle (Area 1)
| # | Task | Cost | Observed |
| --- | --- | --- | --- |
| 1 | Build castle | ★1 | built after Level 1 |
| 2 | Build the fountain | ★1 | built after Level 2 |
| 3 | Install gazebos | ★2 | built after Level 4 |
| 4 | Plant the bushes | ★2 | shown, not built (1 star in hand) |
| 5 | not reached | | |
| 6 | not reached | | |

- The area reward is a purple chest at 6/6, not reached.
- The task list opens by itself on arriving Home only when the current task is affordable (after Levels 2 and 4). After
  Levels 3 and 5 it did not open and showed no badge.

## Not observed
Castle tasks 5-6, the chest reward, the next area, any story or dialogue (none appeared in Levels 1-5 or at Home), and the
fail flow (no level was lost).
