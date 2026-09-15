# Area 002 — Outer Courtyard

**Campaign:** The Great Royal Treasure Hunt  
**Status:** Authored — 2026-09-14. Four tasks, four boards, no native repair transitions.  
**Story reference:** [Accepted screenplay](../../GREAT-ROYAL-TREASURE-HUNT.md)  
**Authoring source:** [campaign.json](../campaign.json)

The player passes through the restored gates into a neglected courtyard. Four puzzles pay for four visible improvements, ending with the fountain running — and the water leaping into the shape of a horse, which is the clue pointing on to the stables.

**Area objective:** Prepare a welcoming fountain and path.

## Ordered tasks and boards

| Order | Task | Cost | Board | Visible result | Completion speaker |
|---|---|---|---|---|---|
| 1 | Clear the weeds | 1 star | 5 | Tall weeds, wild grass tufts and scattered dry leaves lift away from the courtyard ground and the fountain base, revealing tidy green lawn. | None; return to the task panel. |
| 2 | Start the fountain | 1 star | 6 | The dry basin fills with clear turquoise water and the central spout gushes a cheerful arc, with soft droplets and sparkles. | None; return to the task panel. |
| 3 | Lay the stone path | 1 star | 7 | Rounded cream flagstones are laid over the bare earth, winding from the foreground to the fountain and ringing its base. | None; return to the task panel. |
| 4 | Plant the flower beds | 1 star | 8 | The two empty stone planters fill with coral, red, pink and yellow blossoms; the fountain's spray shapes itself into a small rearing horse. | Pip and Freya in the two-card discovery. |

Total: **4 tasks, 4 stars, 4 unique first-win boards**. Boards 5–8 are allocated to this area only; Area 001 holds boards 1–4.

Boards 5–8 and their target assets are unchanged. Campaign move floors are 24, 20, 38 and 36, each set a little above the board's own shipped `LIMIT` (21, 15, 35, 33) following the Area 001 pattern, so an early difficulty spike cannot gate the area's story.

## Story delivery

- No opening scene. `OPEN-01` belongs to Area 001 and fires once per campaign.
- The task panel fades for each repair. Tasks 1–3 return directly to the updated task panel with no dialogue card, matching Area 001.
- `A002-DISCOVERY`: after task 4. Exactly two cards: Pip says "Look at the water — it's shaped like a horse!" with the horse close-up; Freya says "A horse means the stables. That's where we go next!"
- Next objective: **Ready the parade ponies in the stables.**

## Repair presentation

This area authors **no native repair transition**. Every task uses the standard in-place repair: gold sparkles over the room, the painting crossfading from state N to state N+1 beneath them, and the spent star flying to the counter. `RepairWork.Classify` picks the effect from each task label — clearing, building, and planting all classify specifically.

`repair_regions.json` carries no entry for this area, so `StoryRepairRegions.For` returns `Unknown` and the sparkles fall over the whole room rather than a measured rectangle. That is the documented fallback, not a gap to fix: the four repairs here each change a broad region of the courtyard rather than one small object.

## Art manifest

All five room states were generated with Magnific, chained image-to-image from state 0 so that each repair changes exactly one thing and the room, camera and palette stay identical between states. Campaign PNGs are 1152×2048 masters; import settings are cloned from Area 001 (sprite, 2048 cap, no mipmaps).

| Resource key | File or source | Purpose |
|---|---|---|
| `area_002_state0` | `Assets/JellyGarden/Resources/Story/TreasureHunt/Art/area_002_state0.png` | Weedy courtyard, dry fountain, bare earth, empty planters. |
| `area_002_state1` | Same folder, `area_002_state1.png` | Weeds and dead leaves cleared. |
| `area_002_state2` | Same folder, `area_002_state2.png` | Fountain running with turquoise water. |
| `area_002_state3` | Same folder, `area_002_state3.png` | Cream flagstone path laid and ringing the fountain. |
| `area_002_state4` | Same folder, `area_002_state4.png` | Flower beds planted; water shaped into a horse. |
| `task_002_1..4` | Explicit crops in `campaign.json` | Weedy ground, running fountain, laid path, planted bed. |
| `clue_002` | Explicit crop of state 4 | The water horse above the spout. |

Characters reuse the existing Pip portrait and the shared cartoon portraits. No new character art, no repair videos, and no new transition atlases are introduced.

## Validation record

- [x] Ordered tasks, exact star budget, and campaign-local board mapping (5–8, no overlap with Area 001).
- [x] Five room states chained from one master; room, camera and palette identical across states.
- [x] Five detail-art crops defined as aliases; no separate task or clue PNGs required.
- [x] `treasure_hunt_build.py` compiles and `--check` verifies.
