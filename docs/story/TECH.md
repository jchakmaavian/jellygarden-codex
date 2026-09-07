# TECH STACK — decisions

Mobile. Unity. **Completely free — no ads, no IAP, no revenue.** Local save only.

---

## STACK

| Layer | Pick | Note |
|---|---|---|
| **Engine** | Unity 6 LTS, 2D URP | Genre standard |
| **Language** | C# | |
| **Board logic** | Pure C# library, **zero Unity references** | The key architectural call — see below |
| **UI** | UI Toolkit (map, dialogue, clue cards) + sprites for the board | |
| **Save** | Single local JSON at `Application.persistentDataPath` | No cloud, no accounts, no backend |
| **Story data** | Markdown → JSON → ScriptableObjects | The `.md` files stay the source of truth |
| **Assets** | Addressables | 150 areas of art will blow the store size cap otherwise |
| **Art** | Magnific MCP | Check commercial-use terms before publishing |
| **VO** | ElevenLabs MCP | Same |
| **Analytics** | None, or Unity Analytics free tier | Optional |
| **Device testing** | mobile-mcp | |

## NOT USING

RevenueCat · Unity IAP · ad mediation · ATT prompt · GDPR consent SDK · lives/energy · LiveOps events · battle pass · server-authoritative progression · Firebase · cloud save.

No revenue means no SDK to defend it. Roughly 40MB and every privacy integration, deleted. **The app ships with no network permission.**

---

## THE ONE ARCHITECTURAL DECISION

Board simulation is pure C# — no `MonoBehaviour`, no `using UnityEngine`.

```
Match3.Core/     pure C#. Board, Tile, Vine, MatchResolver, MoveSolver
Match3.Unity/    MonoBehaviours that render Core and feed it input
Match3.Tests/    NUnit against Core
Match3.Sim/      headless console runner
```

Not over-engineering. The Area 001 spec commits to win rates of **100 / 98 / 94 / 88%**. Those are unreachable by hand-play — you need a bot running thousands of attempts per level:

```bash
dotnet run --project Match3.Sim -- --level arc1/001-04 --trials 10000
# → win rate 0.81 · avg moves left 1.2 · vine overrun 14%
# → recommendation: +1 move
```

A greedy bot is ~200 lines. It's what makes 255 tuned levels possible instead of 12. Build it before any sprite work.

---

## WHAT "FREE" CHANGES IN THE DESIGN

**No lives gate.** The 5-lives system exists to sell refills. Without revenue it is pure friction. Retry is instant and unlimited.

That cascades:

| | Monetised match-3 | This game |
|---|---|---|
| Retry cost | A life, or gold | Instant, unlimited |
| Area 001 win rates | 100/98/94/88% | **Same** — onboarding still matters |
| Late Arc 1 (019–025) | ~75% | **55–65% is fine** |
| Boosters | Sold | Earned only, granted generously |
| Fixing a hard level | Sell a booster | Just tune it |

Instant retry means levels can be genuinely harder and more interesting. Grimm's sabotage at Area 019 can bite — nobody is paying to get past it, so nobody resents it.

**Story is the entire retention mechanic.** No daily-reward treadmill, no FOMO events. People open the app tomorrow because they want to know where Freya is.

---

## GENERATED ASSETS

Art, voice, music and effects are all generated — see `ASSETS.md` for the full
inventory, how each was made, and the two open items (casting Alfred's voice,
and checking commercial-use terms before publishing).

Import settings are applied automatically by an `AssetPostprocessor`, and every
asset path lives in one file (`Assets.cs`) so a missing file is one warning
rather than a silent blank.

---

## BOOSTERS

Four, all **earned, never sold** — the free-game rule applies here too.

| Booster | Effect |
|---|---|
| Hammer | Removes one tile, or takes a hit off one block |
| Rocket | Drops a rocket on a tile and fires it — clears the row |
| Bomb | Drops a bomb on a tile and fires it — clears 3×3 |
| Shuffle | Rearranges the board, no target needed |

Boosters cost **no move** — the player already paid by winning a level — but they
**do advance the turn counter**, so vines still creep. Without that, a stack of
boosters would trivialise every spreading obstacle in the arc.

One random booster is granted per level won. Two Hammers and a Shuffle are
granted on first run so the mechanic is discoverable.

---

## SAVE FILE

One JSON, local, rewritten on every area completion and level win.

```json
{
  "version": 1,
  "currentArea": 1,
  "stars": 2,
  "areasCompleted": [],
  "tasksCompleted": { "1": [1, 2] },
  "levelsBeaten": ["arc1/001-01", "arc1/001-02"],
  "cluesSeen": [],
  "cinematicSeen": true,
  "boosters": { "Hammer": 3, "Shuffle": 2, "Rocket": 1 },
  "settings": { "music": true, "sfx": true, "vo": true }
}
```

Write to a temp file and atomically rename, so a kill mid-write cannot corrupt a save. That is the entire persistence layer.

---

## SCOPE WARNING

**255 levels for Arc 1, solo and hand-tuned, is a six-month job.** Two ways out:

1. **Generate + validate.** A generator emits level candidates against a difficulty target; the sim bot checks the win rate; keep what lands in range. Turns 255 levels into a weekend.
2. **Cut star costs.** Drop Arc 1 to ~100 levels (4 per area, 6 on beats). Story pacing survives — the clues are what people came for.

Recommended: **option 1**, with the story beats staying exactly where they are.

---

## PUBLISHING

- **Google Play** — $25 one time.
- **Apple** — $99/year even for free apps. Ship Android first if budget matters.
- Verify Magnific and ElevenLabs commercial-use terms. Free distribution is still distribution.

---

## BUILD ORDER

**Logic → tuning → pixels.** If the board doesn't feel good grey, it won't feel good gilded.

1. `Match3.Core` — 8×8 grid, 5 colours, swap, match-3/4/5, cascade, gravity
2. Vine obstacle — anchor, spread-per-turn, cut-back (all Area 001 needs)
3. NUnit tests — vines spread on schedule, level 1 spread disabled, seeded cascades deterministic
4. `Match3.Sim` — greedy bot, win-rate report
5. Tune Area 001's four levels to spec
6. Unity — render the board, wire 4 tasks → 4 stars, one clue card

---

[Story Bible](STORY.md) · [Arc 1 board spec](arc-1-the-missing-queen/GAMEPLAY.md) · [Opening](arc-1-the-missing-queen/00-OPENING.md)
