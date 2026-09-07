# Area 001 — Castle Gates

**Arc:** 1 — The Missing Queen
**Phase:** Setup — small goals, texture clues
**Beat type:** Texture
**Tasks:** 4 · **Stars to complete:** 4 · **Levels:** 4

> First area in the game. Everything here is doing double duty: it tells the opening of the story and it teaches the entire loop. See **[00-OPENING.md](00-OPENING.md)** for the first three minutes.

---

## Goal

Get the gates open. Nobody has cleared this road in months.

Leo's stated reason is practical — he cannot ride into his own castle. The real function is orientation: the player sees the state of the kingdom before anyone explains it.

---

## Repair tasks

| # | Task | Stars | What changes on screen |
|---|---|---|---|
| 1 | Cut back the ivy choking the portcullis | 1 | Ivy peels off. Stone and iron underneath, sound. |
| 2 | Replace the rotted gate timbers | 1 | Fresh oak beams. The gate stops sagging. |
| 3 | Rehang the royal banner | 1 | Leo's colours drop down the wall. First colour in the scene. |
| 4 | Relight the gate lanterns | 1 | Warm light. The castle stops looking abandoned. |

**Total: 4 stars = 4 levels.**

Task order is deliberate: **decay → structure → identity → life.** By task 4 the gate looks cared for, which makes the ruined courtyard in Area 002 land harder by contrast.

---

## Board

**New obstacle: Vines** — creep one tile per turn from a fixed anchor. Match adjacent to cut back. Teaches the arc's first lesson: *deal with it now, or it grows.*

**Vines never move**, and as of now they never regrow either. Blockers are excluded from gravity and from shuffles, so a vine stays in its cell until it is cleared.

**Spread is switched off for the whole of Area 001.** It was originally on from level 2, to teach *deal with it now or it grows*. In play it did not read that way: ivy reappearing a few cells from where you cleared it looked like the blocker had slid across the board, and on levels whose objective is timbers or lanterns it was noise the player had no reason to track. Vines here are now purely static obstacles.

The mechanic itself is intact — it is one flag per level in `game/data/levels/area-001.json` (`obstacles.vine.spreadEnabled`), and every spread now unfurls with a scale-in, a rustle and a line of copy. If it comes back, it should come back in an area where **the ivy is the objective**, so growing back is the challenge rather than background churn.

Anything that appears on the board by itself needs an arrival animation, or players read it as a bug. Area 019's rising water and Grimm's Meddling tile are the same shape.

**Level types:** Standard only.

**Grid:** 8×8. Five tile colours (not six — colour-matching is easier and level 1 must not be lost).

### Level-by-level

Move counts below are **simulator output, not guesses** — 5,000 trials per level per skill band. Config lives in `game/data/levels/area-001.json`; re-verify with:

```bash
dotnet run --project Match3.Sim -c Release -- --all --trials 5000
```

| Lv | Objective | Moves | Teaches | Skilled | Average | Casual |
|---|---|---|---|---|---|---|
| **1** | Clear 8 vine tiles | 20 | Swap, match-3, the vine obstacle | 100% | 100% | **100% — cannot be lost** |
| **2** | Break 10 rotted timbers (2 hits each) | 23 | Multi-hit blockers | 99.6% | 98.3% | 94.0% |
| **3** | Collect 3 banner fragments buried under vines | 11 | 4-in-a-row → Rocket; clearing *down* to something | 95.8% | 94.5% | 91.9% |
| **4** | Light 4 lanterns (match adjacent to each) | 9 | All three together | 95.3% | 92.8% | 88.3% |

**On level 4's target.** This was specced at 88%. The simulator can hit that exactly at 7 moves, but casual players then fall to ~80% — which breaks the opening sequence's own rule of *zero losses in session one*. The target was a guess; 8 moves is the answer. Corrected to 92%.

**On level 2's length.** It originally ran 6 timbers in 14 moves and was over in 5.7 turns — a win-rate on target hiding a level that ended before it started. Ten timbers spread wider over 25 moves brings it to 10.7 turns. Win rate alone would never have caught this; the turns column did.

**On the skill band.** A perfect solver beats levels no human would, and a proximity-only bot fails ones everybody clears. The simulator plays three stand-ins — skilled (always best move), average (30% random), casual (55% random) — and levels are tuned so *average* sits on target and *casual* stays comfortable.

### Level 1 — the unloseable one

- Vines sit still and wait to be cut (true of every level in this area now).
- 20 moves against a 5-move solution — the simulator clears it in 5. Enormous slack, on purpose.
- If the player somehow runs low, the move counter quietly stops decrementing. Never show a fail screen in the first 60 seconds of a match-3 game.
- Hand cursor guides move 1 only. Moves 2+ are free play.

```
Level 1 layout — vines anchored bottom-left

. . . . . . . .
. . . . . . . .
. . . . . . . .
. . . . . . . .
V . . . . . . .
V V . . . . . .
V V . . . . . .
V V V . . . . .     V = vine (8 total)
```

Anchor in the corner so the player naturally clears inward. No move can make the situation worse.

### Level 4 — the first real level

First level where losing is possible. 8 moves, with an average player finishing on 4.2 spare. If live data shows casual players below 80%, add one move — do not weaken the vines. The vines are the story.

---

## Clue

> The gatekeeper's log stops mid-sentence, eleven weeks ago. The last line reads: *the Queen asked me to—*

### Delivery

Fires on the **last repair task**, never on a level win:

```
4th star spent → lanterns catch → light falls across the gatehouse window
→ camera finds the open logbook on the sill → CLUE CARD (3 seconds)
```

Three-second card, no cutscene — this is a texture area. Alfred VO, one line only:

> *"He never finished the sentence. He never got the chance."*

Then straight to the map. Area 002 unlocks.

**Do not explain the clue.** Nobody says "that's suspicious." The player gets a torn sentence and a date, and draws the line themselves. Every clue in this arc follows that rule.

---

## Direction

Cold open. Leo rides in alone. No fanfare, no guards. Establish the rot in one screen.

**Cast present:** Leo (rides in at 1:05), Mimi (appears 1:35). Alfred is **voice-over only** — he does not appear in person until Area 005. Grimm does not exist yet.

**Tone:** quiet. Not tragic, not comic. Wind, crows, one man's footsteps on stone. The music should be almost nothing — a single held string. Save the full theme for the Area 002 castle reveal.

**Colour:** desaturate hard. Greys, dead green, rust. The banner in task 3 is the first saturated red the player sees, and the lanterns in task 4 are the first warm light. Those two moments are the area's entire visual arc.

**The one line that matters:** Leo says *"...Freya?"* into an empty courtyard and gets no answer. That is the hook. Do not add a second line.

---

## Points to

Area 002 — Outer Courtyard

The logbook stops mid-sentence, so Leo goes looking for the man who wrote it. He finds the courtyard instead — and his father's statue face-down in the weeds.

---

## Production checklist

- [x] 4 levels tuned — 100 / 99 / 96 / 92% for an average player, verified over 5,000 trials each
- [ ] Vine obstacle: art, spread animation, cut-back VFX
- [ ] Gate art in 5 states (ruined → +ivy cut → +timbers → +banner → +lanterns)
- [ ] Opening sequence: hand-cursor tutorial, board-to-gate dissolve, castle reveal
- [ ] Alfred VO: 4 opening lines + 1 clue line
- [ ] Mimi intro animation
- [ ] Clue card art: gatekeeper's logbook, torn sentence legible on a phone screen
- [ ] Confirm: zero fail screens, zero offers, zero tutorial popups in this area

---

[Opening sequence](00-OPENING.md) · [Arc overview](ARC.md) · [Board spec](GAMEPLAY.md)
