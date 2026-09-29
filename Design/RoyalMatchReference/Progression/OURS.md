# Royal Throne progression as implemented (audit 2026-09-29, working tree on `dd0cef99`)

This comes from a read-only code audit. Paths are under `Assets/`. All saves go through `RoyalSave`, which keeps a separate
QA profile under `RoyalThrone.QA.v1/`.

## Levels
- **Data:**
  - Levels 1-5 come from `Resources/ReferenceGameplay/levels01-50.json` (the verified reference layouts).
  - Levels 6-100 come from `Resources/Levels/N.txt` plus `Resources/Targets/LevelN`.
  - The QA reference campaign plays levels 1-50 from the JSON.
- **Keys:**
  - `OpenLevel` is the level being opened.
  - `Level.NNN.StarsCount` holds the best stars.
  - `Score<n>` holds the best score.
- **Unlock:** level n is locked while level n-1 has 0 stars (`LevelsMap.IsLevelLocked`). The Home "Level N" button is the
  last listed level whose predecessor has stars.
- **Cap:** 100.

## Win
The order of events:
1. `CheckWinLose`.
2. `PreWinAnimations`: the celebration → the Reward counter plus the leftover moves as rockets (`RoyalWinSequence`,
   `ReferenceWinFinish`).
3. The `Win` state.

The `Win` setter then:
1. adds the coins (`Gems`, saved);
2. shows the card;
3. refunds the life spent on Play;
4. saves the best stars and score.

**Stars:**
- Levels 1-5 always get 1.
- Levels 6-100 get 1-3 from score thresholds.
- A win with moves left but a score under the first threshold gets **0 stars**: 15 coins are paid, no star is saved, the
  next level stays locked, and yet CONTINUE opens its card.

**Coins:** `10 + 5 x stars`, which is the user's rule, so 15, 20 or 25.

**Card and Home:**
- On levels 6+, the coins fly on the card and CONTINUE opens the next level's card directly (no Home).
- On levels 1-5, CONTINUE goes Home: a tip card, the star and coin flights, then the task list if it is affordable.

**Guards:** the setter blocks Win→Win; the finale can't mint coins; cancelled presentations don't pay.

## Fail
- **Out of moves:** "SO CLOSE!" with CONTINUE, which costs 12 coins for +5 moves (+30 s on timed levels). The offer
  repeats at the same price.
- **END LEVEL:** the fail card, then Again, which reopens the level's card.
- **Lives:**
  - A life is spent on Play and refunded on a win; a fail or leaving keeps it spent.
  - Refill: 12 coins.
  - 5 max, 5 min each (`RoyalThrone.NextLifeUtc`).

## Currencies
- **Coins** are the `Gems` key. New players get 5 (the scene's `FirstGems`).
- **Area stars** are not stored as a counter. Available = Σ clamp(best stars per level, 0..3) − `RoyalThrone.Area.Spent`.

## Areas
- **Data:** `Resources/RoyalAreas/areas.json` has 10 areas and 87 tasks. Each area's total stars and chest:

  | Area | Tasks | Total stars | Chest |
  | --- | --- | --- | --- |
  | 1 | 6 | 10 | 20 coins + Stripes |
  | 2 | 6 | 12 | 25 coins + Packages |
  | 3 | 7 | 15 | 30 coins + Colorful_bomb |
  | 4 | 8 | 18 | 35 coins + Stripes |
  | 5 | 8 | 19 | 40 coins + Packages |
  | 6 | 9 | 22 | 45 coins + Colorful_bomb |
  | 7 | 10 | 25 | 50 coins + Stripes |
  | 8 | 10 | 26 | 55 coins + Packages |
  | 9 | 11 | 29 | 60 coins + Colorful_bomb |
  | 10 | 12 | 32 | 65 coins + Stripes |

  The total is 208 stars. Tasks cost 1-3 stars each.
- **Art:** only Area 1 has art (its before-art is a placeholder). **Areas 2-10 route to "Coming Soon"**, so the chain
  stops after Area 1's chest.
- **Do It:** runs in strict order. It checks the stars, sets the Done bit, adds the cost to Spent, saves, then animates.
- **Chest:** paid at "Tap to Open" (coins plus a booster), then Current+1, Done reset, Entered, saved. The "New Area" and
  Unlock step is reachable only when the next area has art.
- **Keys:**
  - `RoyalThrone.Area.Current`, `.Done` (a bitmask), `.Spent`, `.Entered`;
  - `RoyalThrone.Area.Guided`.

## Story
None exists. The Home "story" panel is a level grid that runtime code never opens.

## Persistence gaps
- **The whole finale window is in memory.** Killing the app between the goals completing and the `Win` state loses the
  win: no stars, coins, unlock or life refund.
- Moves bought with CONTINUE are lost on a kill (the life stays spent).

## Other findings
- **BUG:** `InitScript.BuyBoost` overwrites the booster stock (`SetInt(type, count)`), so a booster-shop purchase replaces
  what the player owns instead of adding to it.
- `CloseMenu` sets `gameStatus = Map` twice, so `EnableMap` and `OnMapState` run twice.
- The debug Win key (editor and development builds only) can pay a second win from the card or Map.
- `Resources/RoyalCampaign/levels01-50.json` is an unused duplicate of the reference levels file, and it still ships.
