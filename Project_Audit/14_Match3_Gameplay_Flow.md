# Match-3 Gameplay Flow — Designer Reference

How a move actually resolves in this project, and where the time goes.

---

## The level file

Every level is a text file at `Assets/JellyGarden/Resources/Levels/<n>.txt`. There are 100.

```
MODE 3                       <- objective type: 0 SCORE, 1 COLLECT, 2 INGREDIENT, 3 BLOCKS
SIZE 9/5                     <- COLUMNS first, then rows
LIMIT 0/6                    <- 0 = moves, 1 = seconds; then the amount
COLOR LIMIT 4                <- how many fruit colours are in play (3-6)
STARS 100/200/300            <- score thresholds for 1, 2, 3 stars
COLLECT COUNT 0/0
COLLECT ITEMS 0/0
10 10 10 10 10 10 10 10 10   <- the grid, two digits per cell
10 10 20 20 10 10 10 10 10      first digit  = cell type
10 10 20 20 10 10 10 10 10      second digit = obstacle
10 10 10 10 10 10 10 10 10
10 10 10 10 10 10 10 10 10
```

**Cell codes actually used across all 100 levels:**

| Code | Meaning | Count |
|---|---|---|
| `10` | plain playable cell | 5,608 |
| `14` | solid block | 916 |
| `00` | hole — nothing drawn, pieces fall past it | 889 |
| `20` | jelly, 1 hit | 698 |
| `50` | double jelly, 2 hits | 428 |
| `13` | cage — the fruit inside cannot be dragged out | 354 |
| `17` | thriving blocker — spreads one cell per move | 199 |
| `23` | jelly + cage | 160 |

An `UNDESTROYABLE` type exists in code and is used by **no** shipped level.

**Level mix:** BLOCKS 40, INGREDIENT 24, COLLECT 21, SCORE 15. Moves-limited 89, time-limited 11. **9 x 11 board in 67 of the 100 levels.**

---

## One move, end to end

```
FINGER DOWN
   |
   |  Item.Update -> Physics2D.OverlapPoint  (every fruit does this, per frame)
   |  NO VISUAL OR AUDIO RESPONSE            <-- gap
   v
DRAG PAST THRESHOLD (0.1 world units, about a tenth of a cell)
   |
   |  Item.SwitchDirection -> resolves which neighbour
   |  The piece does NOT follow the finger    <-- gap
   v
SWAP
   |
   |  DragBlocked = true  (the whole board locks)
   |  The model is swapped SPECULATIVELY, then both cells are asked
   |  "would you have a match here?"
   |
   +--> NO MATCH  -> push part-way and bounce back, ~220 ms, Wrong_match.wav
   |
   +--> MATCH     -> both pieces travel, eased, 130 ms
                     Limit-- (moves), moveID++
   v
LevelManager.FindMatches() -> StartCoroutine(FallingDown())
   |
   v
+---------------------------- CASCADE WAVE (repeats) ----------------------------+
|                                                                                |
|  1. WaitForSeconds(0.1)                                    <-- 100 ms, HARD    |
|                                                                                |
|  2. CombineManager.GetCombine()   the authoritative match finder                |
|                                                                                |
|  3. combo++                                                                    |
|                                                                                |
|  4. For each matched piece:                                                    |
|        if it is a SPECIAL -> WaitForSeconds(0.1) first  <-- +100 ms EACH       |
|        DestroyItem()                                                           |
|                                                                                |
|  5. If the player just earned a special -> WaitForSeconds(0.5)  <-- +500 ms    |
|                                                                                |
|  6. while (!IsAllDestoyFinished()) WaitForSeconds(0.1)     <-- THE GATE        |
|        Polls EVERY tagged fruit, 10x/second, until every dying piece           |
|        reports that its animation is done.                                     |
|                                                                                |
|  7. Square.FallOut() x 20 full board passes, in ONE frame  (model only)        |
|        No diagonal falling - straight down the column only.                    |
|                                                                                |
|  8. WaitForSeconds(0.2)                                    <-- 200 ms          |
|                                                                                |
|  9. Every displaced fruit animates its fall simultaneously                     |
|                                                                                |
| 10. WaitForSeconds(0.2)                                    <-- 200 ms          |
|                                                                                |
| 11. GenerateNewItems()  new fruit spawns 3 units above its cell                |
|     RegenMatches()      recolours any accidental pre-made match                |
|                                                                                |
| 12. WaitForSeconds(0.1) then poll IsAllItemsFallDown()     <-- 100 ms+         |
|                                                                                |
| 13. Any matches left, or anything still able to fall? -> repeat                |
+--------------------------------------------------------------------------------+
   |
   v
Thriving blockers spread by one cell
CheckWinLose()
combo > 2  ->  a praise word appears ("Sweet!", "Delicious!")
CheckItemsPositions()  snaps every fruit back to its cell (a brute-force desync fix)
DragBlocked = false        <-- the player can move again
   |
   v
AI.CheckPossibleCombines()  waits 1 s, scans, then waits another 1 s -> hint
```

---

## Where the time actually goes

**Hard-coded waits per cascade wave:** `0.1 + 0.2 + 0.2 + 0.1 = 0.6 s`, plus at least one 100 ms poll at the destroy gate and one at each fall gate — **about 0.8 s per wave before any animation plays at all.**

A four-deep cascade therefore cannot resolve in under roughly 3.5 seconds no matter how fast the art is.

### The destroy gate is the key mechanism

```csharp
bool IsAllDestoyFinished() {
    GameObject[] items = GameObject.FindGameObjectsWithTag("Item");
    foreach (GameObject item in items) {
        Item c = item.GetComponent<Item>();
        if (c.destroying && !c.animationFinished) return false;
    }
    return true;
}
```

Three things follow from this, and they govern how fast the game can ever feel:

1. **The slowest death animation on screen stalls the entire board** — including parts of the board with nothing to do with that match.
2. **It is polled every 100 ms, so the real wait always rounds UP.** A 160 ms animation costs 200 ms. Shortening an animation from 260 ms to 210 ms buys nothing; shortening it to 190 ms buys a full 100 ms.
3. It calls `GameObject.FindGameObjectsWithTag` — allocating an array of about 99 objects — **ten times a second, in four different places**, during the busiest visual moments.

> **The headline for a designer: roughly 0.8 s of every cascade wave is `WaitForSeconds`, not animation.** If the brief is "make it feel snappier", the highest-leverage change is those numbers in `LevelManager.cs`, not the animation curves.

---

## Special pieces

**Creation** is decided in `CombineManager.SetNextItemType`:

| Match shape | Result |
|---|---|
| 5 or more in a line | Colour bomb |
| L / T / cross (both axes >= 3) | Wrapped package |
| Exactly 4 | Striped, oriented by the player's drag direction |
| Plain 3 | nothing |

A **second, parallel creation path** exists inside `Item.Switching()` using different counts. Two places decide "you earned a striped piece", which is a plausible reason specials sometimes look inconsistent with the visible match.

**Combinations** (swapping two specials together) — all implemented:

| Pair | Effect |
|---|---|
| Striped + Striped | Full cross — a row and a column |
| Striped + Package | The 3x3 around the package becomes alternating stripes, then all fire |
| Package + Package | A larger area clear |
| Colour bomb + plain fruit | Every fruit of that colour is destroyed |
| Colour bomb + Striped | Every fruit of that colour becomes a stripe, then all fire |
| Colour bomb + Package | Every fruit of that colour becomes a package |
| Colour bomb + Colour bomb | A dedicated outward board sweep |
| Anything + Ingredient | Blocked — ingredients cannot combine |

---

## Objectives, win and lose

- **Objectives** live in 100 ScriptableObjects at `Resources/Targets/Level<n>.asset`, each a reorderable list of `{ type, icon, color, targetCount }`.
- **Matching rules:** COLLECT matches on colour; INGREDIENT matches on a **string comparison of the sprite's name**; BLOCKS matches on the sprite name being in a list, with a fallback that counts any `Square`.
- **Win:** the instant every objective is done. If you run out of moves with all objectives done you *additionally* need `Score >= star1` — and if you do not have it, **the code sets no state at all**, a silent stall.
- **The win sequence can run 12+ seconds.** Up to 15 leftover moves each become a stripe at 0.5 s apiece, then all are cleared at about 1.1 s apiece, then a 3 s banner.

---

## Hints and the dead board

**The hint system and the dead-board detector are the same code** (`AI.cs`).

- It scans **13 hard-coded shape templates** across every colour and every cell.
- The hint appears about **2 seconds after the board settles** — 1 s in the scanner plus 1 s in the display coroutine. **It is on a fixed clock, not an idle timer**: it fires whether or not the player is still thinking.
- **The scan only understands plain colour matches.** A board whose only legal move requires a colour bomb or a stripe would be declared dead. (INFERRED from the template code.)
- **"Shuffle" is not a shuffle.** The board is destroyed and regenerated. Blockers, jelly and progress survive; the fruit does not.

---

## Constraints this flow places on design

1. Roughly 0.8 s of hard waiting per cascade wave.
2. The destroy gate rounds up to 100 ms — there are no in-between animation durations.
3. Earning a special costs a flat extra 500 ms; each special in a group costs another 100 ms.
4. No diagonal falling — pieces never slide around overhangs, which constrains level shapes.
5. The swipe threshold is in world units, so sensitivity varies by device.
6. The board is locked (`DragBlocked`) for the whole cascade; there is no move queuing or buffered input.
