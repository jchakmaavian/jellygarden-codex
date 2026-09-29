# Royal Throne Area 03 art

Created with the built-in image generation tool, 2026-09-29. Original blue-and-gold dining furniture designs.

- `before.png`: empty ivory dining salon, portrait 9:16 composition.
- `objects.png`: 1254 x 1254 PNG with genuine alpha. Three columns and three rows, cell size 418 px. Seven groups in reading order: table, chair pair, cabinet, piano, fireplace, curtain, chandelier; final two cells are empty.
- `cabinet-replacement.png`: 1254 x 1254 PNG with genuine alpha, complete cabinet crown and feet. Use this instead of atlas cabinet. Alpha > 8 bounds are [276,71,979,1139], right and bottom exclusive.

The model did not honor the requested 3072 x 3072 atlas size. Preserve original source pixels; engine may scale sprites. Hidden RGB contains diffuse colors under zero alpha and should not be displayed without alpha blending. Two attempted padding repairs flattened a checkerboard into RGB; those rejected variants are not in the deliverable folder.

Exact alpha > 8 bounds, exclusive right/bottom, in atlas pixels:

| Sprite | x0 | y0 | x1 | y1 |
| --- | ---: | ---: | ---: | ---: |
| Dining table | 15 | 79 | 404 | 378 |
| Chair pair | 454 | 47 | 801 | 379 |
| Cabinet (superseded) | 882 | 7 | 1214 | 404 |
| Piano and stool | 27 | 424 | 387 | 834 |
| Fireplace | 441 | 501 | 815 | 782 |
| Curtain panels | 843 | 424 | 1247 | 816 |
| Chandelier | 30 | 840 | 389 | 1239 |

All nominal 418 px cell boundaries are clear of alpha > 8 pixels. Recommended composition centers in normalized background coordinates: table (.5,.73), chair pair split at (.25,.75) and (.75,.75), cabinet (.16,.48), fireplace (.5,.45), piano (.8,.57), chandelier (.5,.25). Split curtain panels may flank side windows. Parent integration should tune these against the final device crop.

Prompt set: polished beveled stylized 3D original royal furniture; rich sapphire blue, warm gold, ivory carved wood, slightly elevated frontal camera, clean silhouettes. Background requested empty grand dining salon with recessed side windows, central empty ivory wall and unobstructed marble floor; no furniture or chandelier. Atlas requested seven isolated groups, actual transparent alpha, 3x3 grid with last two cells empty, no text, labels, grid lines, scenery, or watermark. Replacement cabinet requested one complete front-facing sapphire china cabinet with gilded crown, glass doors, porcelain dishes, blue goblets, short gold feet and transparent all-side padding.

Source images are retained under `C:/Users/chakm/.codex/generated_images/01a0ed1f-620f-7e80-a0ef-325bf7bbee09/`:

- Background: `exec-31f6d764-07bf-4199-89b5-78b85961ce8c.png`
- Atlas: `exec-98a14bd0-4e31-41b7-8131-6f4ec37c786e.png`
- Cabinet replacement: `exec-c8ecf2cd-88aa-49c4-824d-20199c5369df.png`
