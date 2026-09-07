# STORY BIBLE — *King Leo* (working title)

Match-3 kingdom-restoration game. 150 areas. Royal Match-style loop.

---

## LOGLINE

A king returns from a long hunt to find his castle in ruins and his queen gone. His advisor says she left. Every room he rebuilds says otherwise.

---

## THE SETUP

**Act 1 — The Hunt.** King Leo rides out on a long royal hunt. He leaves the castle in the hands of his advisor, **Grimm**. Weeks pass. Grimm rules. Grimm likes it.

**Act 2 — The Return.** Leo comes home to a rotted castle — dust, weeds, broken stone, no staff. And **Queen Freya is missing**. Grimm's story: *"She left, my lord. She waited. You did not return."*

Leo does not believe him. He starts rebuilding — because every room he restores brings back a servant, and every servant remembers something.

**Act 3 — The Truth.** Freya never left. She found Grimm's forged decrees while Leo was away, and Grimm locked her in the old dungeon below the castle, then buried the path under rubble. The castle was not neglected. It was **collapsed on purpose**, to seal her in.

Every room Leo rebuilds clears one more inch toward her.

---

## CAST

| Character | Role |
|---|---|
| **King Leo** | Protagonist. Blunt, warm, not clever — but he does not stop. |
| **Queen Freya** | Imprisoned in Arc 1; voice via notes and objects only. Rescued at Area 25, then co-lead for Arcs 2–6. Sharp, patient, keeps receipts. |
| **Grimm** | Royal Advisor. Villain. Escalates across all six arcs: advisor → fugitive → paymaster → warlord's man → Leo's half-brother. |
| **Mimi** | The castle cat. Comic relief. Finds the first clue in almost every arc. |
| **Cook** | First staff member recovered. Openly contradicts Grimm. |
| **Gardener** | First eyewitness. Terrified, then loyal. |
| **Butler Alfred** | Narrator voice. Frames each area. |
| **Captain Roderick** | Guard captain, dismissed by forged order. Muscle for later arcs. |

---

## STRUCTURE — 6 ARCS × 25 AREAS = 150

One mystery cannot carry 150 areas. Six can. Each arc has its own question, its own pressure, and its own payoff.

| Arc | Areas | Question | Payoff |
|---|---|---|---|
| **1. The Missing Queen** | 1–25 | Where is Freya? | Rescued. Grimm flees. |
| **2. Grimm's Trail** | 26–50 | Who helped him? | Traitor noble exposed. |
| **3. The Empty Treasury** | 51–75 | Where did the gold go? | It bought an army. |
| **4. The Rival Crown** | 76–100 | Who is paying Grimm now? | A neighbouring crown. |
| **5. The Old King's Secret** | 101–125 | Why does Grimm hate Leo? | He is Leo's half-brother. |
| **6. The Reckoning** | 126–150 | Can the kingdom survive? | The siege broken by evidence. Double coronation. |

**All 150 areas are written** — goal, repair tasks, clue and staging note each. 807 tasks, 1,209 stars. Levels are built for Area 001 only; the rest are locked by data until their level configs exist.

---

## THE SPINE

Each arc answers its own question and asks the next one. Nothing is a monster-of-the-week.

1. **Freya is missing** → she was buried under the castle. Grimm flees.
2. **Grimm had help** → House Vane. Exposed publicly — and Vane says Grimm was working for someone else.
3. **The treasury is empty** → the gold was traded abroad for an army. Someone foreign is paying.
4. **Who is paying** → King Haldor of the north, who has annexed three kingdoms by arranged marriage and wants Freya as the fourth. Beaten at a table, not on a field. Haldor disowns Grimm, who says: *ask your father's confessor what he did with his second son.*
5. **Why Grimm hates Leo** → Grimm is Aldous, Leo's elder half-brother, hidden in a monastery, his mother burned in a cottage. The forged will did not steal Leo's throne — it protected a theft Leo's father committed first. The old king repented and wrote to call him home. The letter was never delivered.
6. **Can the kingdom survive** → the siege is broken by publishing the truth rather than winning a duel. Leo enters the true will into the record and lets the council rule. Crowned by consent. Aldous is named, pardoned, and not restored.

**The rule the ending turns on** is planted in Area 119, carved above the first king's tomb: *the crown passes to the elder, and the kingdom to whoever tends it.* Leo has spent 150 areas tending it.

---

## AREA TEMPLATE (repeat 150×)

```
Area opens  → Leo (and Freya, from Arc 2) state a small, concrete goal
Levels      → player earns stars
Stars spent → 4 repair tasks complete the area
Final task  → CLUE lands (an object, a letter, a witness line)
Area closes → the clue points at the next area
```

**One clue per area.** Most are texture — a missing ledger page, a turned portrait. Every **5th area** is a real **ARC BEAT** that moves the plot.

---

## RHYTHM INSIDE EVERY ARC OF 25

| Areas | Function |
|---|---|
| 1–5 | Setup. Small goals, small clues. |
| 6–10 | First hard evidence. |
| 11–15 | Villain pushes back — sabotage, flood, fire, rubble. |
| 16–20 | Allies gathered. |
| 21–24 | Dead end or betrayal. |
| 25 | **Arc payoff.** Big cutscene. |

Sabotage beats double as the in-fiction excuse for the difficulty ramp.

---

## PRODUCTION RULES

1. **Freya must join the cast by Area 25.** A rescued character beats an absent one. She carries Arcs 2–6.
2. **Rotate locations.** 150 rooms in one castle is impossible. Expand: castle → village → mines and trade roads → border → ruins and crypts → siege.
3. **Grimm cannot simply lose six times.** He escalates in kind, not just in scale.
4. **Ship Arc 1 fully written.** Sketch Arcs 2–6. Watch where players drop. Write the next arc against real data.

---

## FOLDER STRUCTURE

```
story/
├── STORY.md                     ← this file
├── TECH.md                      ← stack, architecture, save format, build order
├── PLAN-TODAY.md                ← today's build plan: Arc 1 vertical slice
├── arc-1-the-missing-queen/     areas 001–025  (fully written + GAMEPLAY.md board spec)
├── arc-2-grimms-trail/          areas 026–050  (story written)
├── arc-3-the-empty-treasury/    areas 051–075  (story written)
├── arc-4-the-rival-crown/       areas 076–100  (story written)
├── arc-5-the-old-kings-secret/  areas 101–125  (story written)
└── arc-6-the-reckoning/         areas 126–150  (story written)
```

Each arc folder holds an `ARC.md` overview and one `area-NNN-*.md` file per area. Arc 1 also holds `GAMEPLAY.md` — the match-3 spec.

---

## MATCH-3 CONNECTION (see `arc-1-the-missing-queen/GAMEPLAY.md`)

**The board is the room.** Obstacles are never generic — they are the room's actual problem. Ivy on the gates becomes vines on the board. Ash in the hearth becomes ash tiles. The player learns the room by playing it.

**Grimm authors the difficulty.** Areas 001–018 are static neglect. At Area 019 Grimm opens the sluice gates and obstacles start acting on their own. Every hard level after that reads as the villain fighting back, not the designer being cruel.

**One clue per area, fired on the last repair task** — never on a level win. Beat areas get a full cutscene; the rest get a three-second card.

**Arc 1 economy:** 162 tasks, 255 levels, ~3 weeks of play. Level types debut one per five areas: Standard (001), Rescue (003), Evidence (009), Race (019), Boss (025).
