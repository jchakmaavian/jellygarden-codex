# Reference progression chain (Royal Match, official Play build 38174)

Status legend:
- **O**: observed in play (emulator `RoyalThrone_Reference_4K` unless the phone is named);
- **D**: read from the build's data (read-only);
- **?**: not yet established.

The earlier evidence lives in `../OBSERVATIONS.md` (levels 1-5, 2026-09-28) and `../Areas/REFERENCE.md` (the Areas flow).

## The chain
| # | Link | Rule | Status | Evidence |
| --- | --- | --- | --- | --- |
| 1 | Level | One level per play. Home shows "Level N"; its card shows the goal, the boosters and Play. L1 starts directly from the first launch, with no Home. | O | OBSERVATIONS: L1-L5 |
| 2 | Win | Settle → celebration → the goal panel becomes "Reward" coins, while the leftover moves become rockets that fire → a one-star "Well Done!" card → Continue → a loading tip → Home. | O | OBSERVATIONS: finale timeline |
| 3 | Coins | Earned on every win (L1-5: 64, 50, 44, 13, 63). The amount isn't a simple function of moves left, and the formula is not inferred. Credited at Home, with a flight into the counter. Starting balance 2000. | O | OBSERVATIONS: rewards table |
| 4 | Stars | **Exactly one star per win**, with no score and no 3-star rating. Credited at Home (flight 0 → 1). Spent only on area tasks. | O (L1-5) | OBSERVATIONS; hard levels ? |
| 5 | Area task | See [Area tasks](#area-tasks). | O | Areas/REFERENCE |
| 6 | Story | Level-based "episodes": `story-1..239` level variants, `EpisodeDialog`/`EpisodeRow`/`EpisodeChestSpine`, `StoryLevelCompletedDialog`. **Not seen in L1-5 or in Area 1**, so where it unlocks is ? | D (names only) | Inspection-ReferenceBaseline/REFERENCE-INVENTORY.md §3 |
| 7 | Reward | See [The area reward](#the-area-reward). | O (Area 5) | Areas/REFERENCE (`p15`-`p18`) |
| 8 | Area completion | The last task shows no star flight (the stars are taken while the HUD is away). The list shows "Area Completed!" and the bar fills → chest → card pack → "Area Completed!" banner with the King → the restored area with Continue → Home shows "New Area". | O (phone) | Areas/REFERENCE |
| 9 | Next area | "New Area!" (name, padlocked scene, Unlock) → the padlock wobbles about 1.1 s → a sparkle ring → "Area N+1 0/k". The list does not open by itself. | O (phone) | Areas/REFERENCE (`p22`, `p23`) |

### Area tasks
- A list shows one task row at a time: an icon, a name, and "Do It ★cost".
- Do It: a star flight → the build → the list reopens → a tick → the next row.
- On arriving Home after a win, the list opens by itself only when the next task is affordable.
- When short of stars: "Not Enough Stars!" → Continue → the next level's card.

### The area reward
- Each area has a chest at k/k.
- Area 5's gave 250 coins, 4 cards and 8 boosters, then a card pack (the Collection feature).
- Area 1's contents: ?

## Levels per area cycle (D, verified against play)
Each win gives one star, so the levels needed to finish an area equal the sum of its task costs. These come from the
`AreaNNAreaConfig` assets (`reference/area_configs_1-12.json`).

| Area | Config name | Tasks | Star costs in task order | Stars = level wins |
| --- | --- | --- | --- | --- |
| 1 | Castle | 6 | 1, 1, 2, 2, 2, 2 | **10** |
| 2 | ThroneRoom | 7 | 1, 2, 1, 2, 1, 1, 2 | **10** |
| 3 | DiningRoom | 9 | 1, 2, 2, 2, 2, 2, 3, 3, 3 | 20 |
| 4 | Garden | 8 | 1, 2, 2, 2, 3, 3, 3, 4 | 20 |
| 5 | Library | 9 | 1, 1, 2, 2, 2, 3, 3, 3, 3 | 20 |
| 6 | Pool | 8 | 1, 4, 2, 2, 2, 4, 3, 2 | 20 |
| 7 | Bedroom | 8 | 1, 2, 3, 3, 4, 2, 3, 2 | 20 |
| 8 | MusicRoom | 8 | 1, 2, 3, 2, 4, 3, 3, 2 | 20 |
| 9 | Dock | 9 | 1, 1, 2, 1, 2, 4, 4, 3, 2 | 20 |
| 10 | Kitchen | 8 | 1, 3, 3, 3, 4, 2, 2, 2 | 20 |
| 11 | Bathroom | 8 | 1, 2, 3, 3, 3, 2, 4, 2 | 20 |
| 12 | Living Room | 9 | 2, 3, 4, 5, 4, 3, 3, 3, 3 | 30 |

**The reference has no fixed 7-level cycle.**
- The first two areas take 10 wins each, Areas 3-11 take 20, and Area 12 takes 30. Later areas rise to 40, 50 and then
  100 (partially decoded).
- Area 2 is the only one with **7 tasks**.

Checks against play:
- Area 1 tasks 1-4 cost ★1, ★1, ★2, ★2 (emulator).
- Area 5 has 9 tasks, and its last costs ★3 (phone).
- Area 6 has 8 tasks, and its first costs ★1 (phone).

Task prerequisites: every task names the task it follows. Area 1 is a strict chain. Other areas branch (Area 2: tasks 4 and
7 have none). The list still shows one row, the lowest-numbered open task, as far as observed.

## Observed 2026-09-29 (iteration 1; screens in `reference/screens/prog_*`, clips in `reference/recordings/prog_L6_*`)
- **Level 6** (grass 72, 35 moves, two 4x9 blocks):
  - won on the last move;
  - **the same one-star "Well Done!" card** as L1-5;
  - Reward **16** coins (Moves 0 at the end, so no leftover rockets). One star per win holds beyond L5.
- **The win is saved by the time the card shows.** A force-stop on the Well Done card, then a relaunch, gave coins
  2234 → 2250, stars 1 → 2, and Home "Level 7".
  - The task list did not open on the relaunch, although the ★2 task had become affordable.
  - (`prog_004`, `prog_005`)
- **Level 7:**
  - **the pre-level boosters unlock** on the level card: Rocket, TNT and Light Ball, **3 of each** (L6's card still had 3
    padlocks);
  - the in-level tool bar is **still padlocked**;
  - goals Box 17 + Grass 32, 25 moves.
- **Out of moves** (the bot lost L7):
  - **"Continue?"**, with "+5 — Add 5 moves to keep playing!" and **Play On 900 coins**;
  - a red X;
  - the coin balance is shown top-left (`prog_L7_025_fail_offer`).
- **A kill on the Continue? offer is restored.**
  - After a force-stop and a relaunch (new process id), the game came back to the same Level 7 board and the same offer.
  - **The level in progress is persisted** (`prog_009`).
- **X on the offer → one combined card** (`prog_010`):
  - "Level 7" and "💔 Level Failed!";
  - the goal status (Box ✓, Grass ✗);
  - "Select Boosters:" (3/3/3);
  - **Try Again**, and a red X.
- **X on the fail card → Home** (`prog_012`):
  - **lives 5 → 4**, with a refill timer. It read 21:28 about 8.5 min after running out of moves, so **one life = 30 min**,
    and the loss counted from the fail;
  - the Area card now shows a red **"1"** badge (★2 in hand; the next task costs ★2).

## Still to observe
- The fail flow: Try Again (does it start the level at once), Play On (does the price escalate on a second continue),
  and the empty-lives flow.
- Lives: the empty-lives flow and the refill offer.
- Area 1: tasks 4-6 in play, its chest contents, completion, and the Area 2 unlock (5 more wins on the emulator:
  ★1 held, ★6 needed).
- Hard levels (star or coin rules).
- Where the story unlocks.
- Coin sinks other than continues (boosters, shop).
- Persistence across an app restart (for example force-stop between the win and Home).
