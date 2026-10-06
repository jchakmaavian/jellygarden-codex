# Throne coin v1

On 2026-09-27 the user chose to give the board's coin item an original Royal Throne coin with a throne-chair emblem
(option 2 of the coin request). The nine `Items-CoinItem-coin*` frames in
`Assets/ReferenceGameplay/Art/Resources/ReferenceGameplay/Sprites/` now hold that coin.
`throne-coin-frames.png` shows all nine.

## What changed

- **The nine frames' pixels.** Each PNG keeps its canvas size, and its `.meta` file is untouched. The GUID, sprite rect,
  centre pivot, 140 pixels per unit and import settings are therefore the same.
- **Nothing else.** The catalogs, code, prefabs and scenes are unchanged. The coin item has no animation, clip or VFX: the
  collect event only plays a sound.

## Where the coin appears

The runtime catalog key `coin` loads `Items-CoinItem-coin__16660`, the 3/4 view. It is the coin goal item on the board in
levels 20 and 40, the bonus levels of the reference campaign (a campaign that runs only in the QA test profile). The other
eight frames came with the atlas import, and nothing loads them today. They were redrawn too, so that any future spin uses
one consistent coin:

| Frame | View |
|---|---|
| `coin.10` | front, 2° turn |
| `coin.11` | front, 10° turn |
| `coin` | 3/4, 29° turn (the frame on the board) |
| `coin.02` | 3/4, 47° turn |
| `coin.03` | 3/4, 64° turn |
| `coin.04` | side, 82° turn |
| `coin.05` | 93° turn: past side-on, so the back face shows (the back carries the same throne) |
| `coin.10_g` | as `coin.10`, with the warmer grade of the frame it replaces |
| `coin.11_g` | as `coin.11`, with the warmer grade and the short left-edge motion smear of the frame it replaces |

## Provenance

- **Original art.** The coin is a 3D model rendered by `render_throne_coin.py` (numpy ray marching, 4 x 4 supersampling).
  It is not derived from the imported frames. No Magnific or other image generator was used.
- **Palette.** The gold follows the luminance-to-colour ramp of Royal Throne's own shop coin,
  `Assets/JellyGarden/Resources/RoyalGameplay/coin.webp` (`own_gold_ramp.json`).
- **Throne emblem.** Designed for this coin from simple rounded shapes: a tall arched back with one knob, small armrests,
  a seat slab and a base on two feet. Each part is embossed to its own height, with the seat highest.
- **Pose data only from the old frames.** `fit_pose.py` fitted each frame's turn angle, scale and position to the
  silhouettes of the frames it replaced (`poses.json`). This keeps the placement in each canvas the same, and it is the
  only information taken from them.
- **Names.** The file names still carry the import's asset IDs, because the catalogs load them by name.
  `Art/NativeSpriteCatalog.json` still lists these nine entries with their original APK source.
- **Re-extraction.** Re-running the reference extraction outside Unity would overwrite this art. The editor's `refresh`
  and `import` commands do not write sprite PNGs.

## Not changed

The safe item's coins are still the imported coin: `Items-SafeItem-coin_*`, `Parts-coin01`-`03` and the coin pile.

## Reproduce and check

```
python render_throne_coin.py <out dir>                    # all nine frames
python verify_and_sheet.py <out dir> <sheet.png>          # canvas, silhouette, corners, edge fringe, bled colour
```

`verify_and_sheet.py` compares against whatever is in the Sprites folder. That is now this art, so after installation it
checks canvas sizes and clean edges rather than a before/after match.

Checks on 2026-09-27, before installation, against the frames being replaced:

- canvas sizes identical;
- silhouette overlap 0.979 to 0.997;
- clear corners;
- no dark fringe at the edges;
- no black left in transparent pixels.

After installation, the same day:

- **Import:** all nine textures imported with unchanged `.meta` files, the same sizes, full-frame rects, centre pivots
  and 140 pixels per unit.
- **Loaded pixels:** a GPU readback of the loaded board coin matches the render exactly: 13,513 opaque pixels,
  mean RGB (226, 167, 40).
- **In Play Mode:** QA test profile, reference campaign, level 20. The board holds 79 coin items, and every one draws the
  new `Items-CoinItem-coin__16660`. The console stayed clean, and the real profile was unchanged.
