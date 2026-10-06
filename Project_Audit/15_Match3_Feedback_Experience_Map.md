# Match-3 Feedback Experience Map — Designer Reference

Every feedback layer at every stage of a move, as the project actually implements it.

Legend: **EXISTS** / **PARTIAL** / **MISSING**. Items marked *(added)* are the AI agent's work, not the original kit.

---

## Sequence 1 — A normal 3-match

### Stage 1: Touch a piece

| Layer | State |
|---|---|
| Visual | **MISSING** — no highlight, no tint, no outline |
| Motion | **MISSING** — no lift, no scale, no tilt |
| VFX | **MISSING** |
| Sound | **MISSING** — no selection sound of any kind |
| Camera | **MISSING** |

**Responsible system:** `Item.Update()` sets a boolean `dragThis = true`. Nothing else happens.

> **This is the single biggest experience gap in the game.** The first moment of every interaction — the moment the player commits — has no response at all. In a premium match-3 this is where a piece lifts, brightens and clicks.

### Stage 2: Drag

| Layer | State |
|---|---|
| Visual / Motion | **MISSING** — the piece does not follow the finger |
| Sound | **MISSING** |

**Responsible system:** `Item.CheckItemDrag()` measures the drag distance and fires the swap once it exceeds **0.1 world units** (about a tenth of a cell). There is no intermediate state: the piece is either still, or already swapping.

> The threshold is in *world* units, so on a phone where the camera is zoomed out the swipe must be physically longer. Sensitivity is not consistent across devices.

### Stage 3: The swap

| Layer | Valid swap | Invalid swap |
|---|---|---|
| Visual | Pieces exchange places | Pushes ~42% of a cell, then returns |
| Motion | Eased, **130 ms** *(added; originally linear, ~200 ms)* | ~220 ms total *(added; originally a full 400 ms round trip)* |
| VFX | **MISSING** | **MISSING** |
| Sound | **MISSING** — a successful swap is silent | `Wrong_match.wav` |
| Camera | **MISSING** | **MISSING** |

> A valid swap has **no sound at all**. The only audio confirmation a player gets that a move was accepted is the destruction sound that follows about 100 ms later.

### Stage 4: Match confirmed

| Layer | State |
|---|---|
| Everything | **MISSING** — `WaitForSeconds(0.1f)` |

**100 ms of nothing happens** between the swap landing and the first sign of a match. This is a hard-coded wait in `LevelManager.FallingDown()`, not an animation.

### Stage 5: Impact and clear

| Layer | Original kit | Now |
|---|---|---|
| Anticipation | **MISSING** | EXISTS *(added)* — 25 ms pull-in |
| Squash | **MISSING** | EXISTS *(added)* — 35 ms, X 1.07 / Y 0.92 |
| Rebound | **MISSING** | EXISTS *(added)* — 25 ms |
| Impact flash | **MISSING** | EXISTS *(added)* |
| Burst VFX | PARTIAL — a 4-frame flip-book, **behind the fruit** | EXISTS *(added)* — layered particles, **in front** |
| Piece removal | 250 ms shrink | 75 ms scale-up + fade *(added)* |
| Sound | `Jelly_destroy` / `Jelly_destroy_02`, random of two | unchanged |
| Hit-stop | **MISSING** | EXISTS *(added)* — 45 ms micro-freeze |
| Camera | **MISSING** | **MISSING** |
| Per-piece stagger | **MISSING** | EXISTS *(added)* — 20 ms between neighbours |

> **Why the original read flat:** the destroy puff (`ItemExpl`) draws at sorting order **0** while the fruit draws at order **2** — the effect was *behind* the piece it was destroying, and behind every neighbouring fruit it overlapped. It was also identical every time: four hand-drawn frames, always 267 ms, no variation in size, rotation or count, and no difference between a 3-match and a 5-match.

### Stage 6: Board settles

| Layer | State |
|---|---|
| Everything | **BLOCKED** — the destroy gate |

Nothing falls until **every** dying piece has reported its animation finished, polled at 100 ms granularity. One slow animation anywhere stalls the whole board.

### Stage 7: Fall and landing

| Layer | Original | Now |
|---|---|---|
| Fall motion | A lerp ramp — long and short drops took nearly the same time | Velocity integration *(added)* — long drops genuinely take longer |
| Landing | `stop.anim`, 258 ms on the child sprite | 100 ms code squash on the root *(added)* |
| Landing VFX | **MISSING** — no dust, no impact | **MISSING** |
| Sound | `drop1` / `drop2`, random of two | unchanged |
| Camera | **MISSING** | **MISSING** |

### Stage 8: Refill

| Layer | State |
|---|---|
| Visual | New fruit spawns **3 world units above** its cell and falls in |
| Motion | Same fall system |
| Scale-in / fade-in | **MISSING** |
| Sound | **MISSING** — refill is silent |

### Stage 9: Cascade

| Layer | State |
|---|---|
| Visual | Repeats the whole wave |
| VFX intensity | PARTIAL *(added)* — scales with match size and cascade depth, capped |
| **Sound** | **MISSING** — **every wave plays the identical destruction sound at the same pitch** |
| Praise word | EXISTS — after 3+ waves, one of several word-art objects appears |
| Camera | **MISSING** |

> **`.pitch` is never touched anywhere in the codebase.** There is no rising ladder. `Combo 01.wav` exists in the project and is wired to nothing.
>
> The game *does* contain one working escalation model: the win sequence plays star sounds 1, 2, 3 in ascending order, 0.5 s apart. That is the pattern to copy.

### Stage 10: Objective update

| Layer | State |
|---|---|
| Visual | EXISTS — a copy of the fruit's sprite is created on sorting layer "UI" |
| Motion | EXISTS — flies to the HUD icon along a runtime-built curve with a deliberate dip, spinning |
| Sound | EXISTS — `Get_star_ingredient.wav` |
| HUD reaction | **MISSING** — the counter changes value with no animation |

### Stage 11: Board ready

`DragBlocked = false`. Then, on a fixed clock, the hint fires about 2 seconds later — regardless of whether the player is still thinking.

---

## Sequence 2 — A special piece: the striped fruit

| Stage | Visual | Motion | VFX | Sound | Gap |
|---|---|---|---|---|---|
| Creation (4-match) | Sprite swaps to the striped variant | `appear.anim`, 333 ms, with a `SetAppeared` event | **MISSING** — no creation burst | `Create_bonus.wav` | **The board pauses a flat 500 ms here** |
| Idle | Static striped sprite | idle breathing only | **MISSING** — no shimmer or pulse to advertise it | — | Nothing signals "this piece is special" |
| Activation | Row or column clears | pieces destroy in sequence | `StripesEffect` beam prefab | `Stripes_bonus.wav` | **No directional trail; the beam does not travel** |
| Chain reaction | Each hit special fires one handshake later | — | each fires its own effect | each plays its own sound, **unlayered** | Sounds stack and can clip — there is no mixer to limit them |

**Wrapped package:** `Firework.prefab` — a real particle system, sorting order 3. Sound: `Explosion.wav`.

> **A wiring defect:** the package detonation and the generic explosion point at the **same audio clip**. A dedicated `Package_destroy.wav` (226 KB) exists on disk, referenced by nothing.

**Colour bomb:** `Replace.prefab` — two particle systems. **But only the root has the sorting fix; the child "Bubble" system stays at order 0 and therefore renders behind the fruit** — the same defect as the original destroy puff.

---

## Summary — the ten most valuable missing layers

Ordered by impact per unit of effort:

| # | Missing layer | Why it matters |
|---|---|---|
| 1 | **Piece selection feedback** | The first moment of every interaction is silent and invisible |
| 2 | **Cascade audio escalation** | A 3-match and a 7-piece cascade sound identical; the asset for this already exists unused |
| 3 | **A sound on a successful swap** | The player gets no audio confirmation that the move was accepted |
| 4 | **Landing impact** | Fruit arrives with no dust, no accent — only a generic drop sound |
| 5 | **Refill spawn accent** | New fruit simply appears and falls, silently |
| 6 | **HUD counter reaction** | Score and objective numbers change with no motion |
| 7 | **Special-piece idle advertisement** | Nothing draws the eye to a piece worth using |
| 8 | **Camera feedback** | No shake, punch or zoom anywhere in the game |
| 9 | **Match-size differentiation in the original VFX** | Partly addressed by the added layer |
| 10 | **A working score popup** | Its 1.0 s animation is destroyed at 0.3 s |

**And the constraint that limits all of them:** roughly 0.8 s of every cascade wave is hard-coded `WaitForSeconds`. Feedback polish will have a low ceiling until those numbers are revisited.
