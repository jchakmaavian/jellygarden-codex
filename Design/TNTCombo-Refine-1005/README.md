# TNT+TNT combo refinement (2026-10-05)

The TNT+TNT combo enlarges its body to about 3.4 cells, but its art was authored at board size (one cell is about 140 px).
This pass sharpens that art, rebuilds the sun band, fixes a rendering bug and adds a merge beat. The animation clip,
the prefab transforms and the explosion are unchanged.

## Assets (GUIDs kept, replaced in place)
| Sprite | Before | After |
| --- | --- | --- |
| Combo-govde_ust (barrel top) | 182x120 @ 140 PPU | 728x480 @ 560 PPU |
| Combo-govde_alt (barrel bottom) | 182x94 @ 140 | 728x376 @ 560 |
| Combo-fitil (fuse) | 73x69 @ 140 | 292x276 @ 560 |
| Combo-fitil_golge (fuse shadow) | 76x66 @ 140 | 304x264 @ 560 |
| Combo-kemer_golge (band shadow) | 170x51 @ 140 | 680x204 @ 560 |
| Combo-kemer (sun band) | 792x396 @ 560, rebuilt | 792x396 @ 560, rebuilt again |

- World sizes and centred pivots are unchanged, so no transform or clip needed editing. Size caps were raised from 256 to
  1024 on every platform; compression formats are unchanged (ASTC 4x4 on Android and iOS).
- The parts are Lanczos-3 enlargements in premultiplied alpha, with a sharpened outline and a gentle unsharp mask. This
  sharpens edges and grooves; it cannot add painted detail that the 182 px source never had.
- **Sun band.** The 09-24 sun band was the board TNT's small 80x64 front plate, stretched about 2.5x to the band's
  footprint. That made it blurry, gave it a red smear along the top, and lost the band's shape. The new band starts from the
  original pre-sun band (`originals/Combo-kemer__21603-pre-sun-original.png`) and keeps its outline: curled ends and a raised
  central plate. The "TNT" letters are painted out with a harmonic fill of the surrounding gold. The band is enlarged 4x and
  carries the supplied sun badge (`Design/TNTSun/Sun-Badge.png`) at its native 256 px.

## Prefab fix
`TNTTNTCombo.prefab` `FitilMask` (the SpriteMask that burns the fuse down) had no material (`m_Materials: {fileID: 0}`).
It now uses Unity's built-in `Sprites-Mask` material.

## Code
`Item.SwitchingCore`: when two TNTs are swapped, the dragged TNT slides onto its partner, drawn above it, instead of the two
trading places. The combined TNT then grows out of the cell they met in. Other swaps are unchanged.

## Files
- `originals/` holds the six PNGs and their .meta files, the prefab, and the pre-sun band, all as they were before this pass.
- `refined/` holds the PNGs as installed.
- `build/` holds the generators (`Img.cs`, `Band.cs`, `build_band.ps1`, `build_parts.ps1`). They run in Windows PowerShell 5.1
  with `Add-Type`; no Python or Unity is needed. `$S` at the top of each script points at the session scratchpad where they
  were written; set it to a folder holding `kemer_original.png` (the pre-sun band), `art\Img.cs` and `art\Band.cs`.
