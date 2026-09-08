# HUD art staging — King Leo

Generated with Magnific, cut and assembled here. **Not imported into Unity yet**
(folder sits outside `Assets/` on purpose).

## Folders

- `raw/` — as generated, opaque white background, 1536px
- `cut/` — background removed, cropped to content, transparent PNG. These are the
  ones that become Unity sprites.
- `contact_sheet.png` — all 11 generations, both variants where two were made
- `mockup_hud.png` — the assembled preview: 9-sliced pieces + real `avatar_leo_00`
  + text, on `area_001_state3` for context
- `compose.py` — cutout + 9-slice + assembly. Re-run to re-preview after swapping
  a variant.

## Pieces

| cut/ file | role | 9-slice |
|---|---|---|
| `frame.png` | wooden top bar | yes, 11% x / 20% y |
| `panel.png` | blue plate under TARGETS and MOVES | yes, 11% x / 13% y |
| `pill.png` | cream counter box | yes, 14% x / 34% y |
| `medallion.png` | gold oval + crown, hollow centre | no |
| `rays.png` | sunburst behind the avatar | no |
| `icon_star.png` | score goal icon | no |
| `icon_book.png` | story goal icon | no |

Variants not chosen: `raw/frame_a`, `raw/panel_a`, `raw/pill_a`, `raw/medallion_a`.
Swap the filename in `compose.py` and re-run to see the alternative.

## Text is not art

TARGETS / MOVES / the numbers are text objects in the mockup, drawn in Arial
Rounded Bold as a stand-in. Real HUD should use a TMP asset (Baloo 2 ExtraBold
or Luckiest Guy) with a cream outline and a soft navy underlay.
