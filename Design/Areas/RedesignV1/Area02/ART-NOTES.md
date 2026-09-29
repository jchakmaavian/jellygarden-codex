# Royal Throne Area 02 art

Created with the built-in image generation tool, 2026-09-29. Original blue-and-gold royal furniture designs.

- `before.png`: empty ivory throne hall, portrait 9:16 composition.
- `objects.png`: 1024 x 1536 PNG with genuine alpha. Six groups, two columns and three rows: throne/dais, carpet, banners, chandelier, knight statues, benches.

The model did not honor the requested 2048 x 3072 atlas size. Preserve original source pixels; engine may scale sprites. Hidden RGB contains diffuse colors under zero alpha and should not be displayed without alpha blending.

Exact alpha > 8 bounds, exclusive right/bottom, in atlas pixels:

| Sprite | x0 | y0 | x1 | y1 |
| --- | ---: | ---: | ---: | ---: |
| Throne with dais | 60 | 13 | 481 | 458 |
| Carpet | 527 | 61 | 979 | 453 |
| Banner pair | 44 | 514 | 493 | 970 |
| Chandelier | 563 | 491 | 979 | 992 |
| Knight statue pair | 62 | 1017 | 472 | 1491 |
| Bench pair | 500 | 1227 | 1013 | 1379 |

Use row boundaries 0/480/992/1536 rather than exact thirds; use x=480 as the column cut for the last row. Pair sprites can be split at their clear central alpha gap and restored simultaneously.

Prompt set: polished beveled stylized 3D original royal designs; sapphire upholstery, warm yellow gold, ivory marble, elevated frontal 15-degree-down camera; bright upper-left illumination. Background requested empty grand ivory throne hall, architectural blue/gold inlays, no furniture, unobstructed floor lower half and central rear niche. Atlas requested exact six groups in reading order with transparent alpha, generous gutters, no scenery, text, labels, grid, or watermark.

Source images are retained under `C:/Users/chakm/.codex/generated_images/01a0ed1f-620f-7e80-a0ef-325bf7bbee09/`:

- Background: `exec-1240eb79-e571-4da8-b447-61f52f7bc419.png`
- Atlas: `exec-b285be2b-c91c-4ee0-a113-f079ce205fe7.png`
