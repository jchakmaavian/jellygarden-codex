# Tooth bands and authentic animated shadows

The smallest art correction is to soften the two tooth textures and restore the original technical shadow masks with suitable warm-neutral color and restrained opacity. Preserve Vanguard's face, moustache, beard, brows, crown, corrected lip surround, source meshes, animation tracks and UVs.

## Evidence and workboards

- [Source/current components on a skin-colored background](teeth-and-shadows-source-current.png)
- [Original-source workboard](FaceRedesign/Source/TeethShadow-source-workboard.png) and [exact layout/masks](FaceRedesign/Source/TeethShadow-layout.json)
- [Current stored-artwork workboard](FaceRedesign/Current/TeethShadowCurrent-source-workboard.png)
- [Hashes, source attachment topology, colors and alpha measurements](teeth-shadow-audit.json)
- [Board extraction tool](Build-TeethShadowBoards.ps1) and [comparison/audit script](inspect_teeth_shadows.py)

The boards contain seven cells at 384 × 384, arranged in two columns. Cell 1 is lower teeth, 2 upper teeth, 3 moustache shadow, 4 right brow shadow, 5 left brow shadow, 6 beard shadow and 7 crown-underside shadow. Cell numbers differ from the old 27-component workboard; use this layout's region names and coordinates when packing. `FaceRedesign/Source/Parts/*-mask.png` contains the original masks. All board operations are technical extraction/inspection, not replacement art.

The current board includes stored shadow textures that are disabled in the current runtime. It must not be read as a claim that every shown shadow currently renders. Brow and moustache shadows are suppressed by the current `KingVanguardFaceShapeModifier` configuration; beard and crown-underside shadows are not.

## Teeth: change the paint, preserve the mouth assembly

The APK lower and upper tooth components are smooth shaded brown bands, not complete individually drawn white dental arches. Both slots and attachment tints are white, so the brown appearance is in the actual source texture. Their source setup draw order is tongue → lower teeth → mouth surround → upper teeth → moustache shadow → moustache → nose. They are intentionally occluded by the surrounding mouth and hair layers.

The upper tooth mesh has only eight vertices and six triangles, with deform keys in 21 clips. The lower has 16 vertices and 14 triangles, with deform keys in 23 clips. New sharp vertical tooth divisions and large bright enamel blocks emphasize changes in this low-resolution deform surface. This is a plausible contributor to the user's impression of peculiar tooth motion; the art audit alone cannot establish a runtime animation fault.

Retexture only these two cells into restrained, smoothly shaded warm-ivory tooth bands. Keep the original asymmetric/slanted registered silhouettes, darker contact/root shading, and softly suggested tooth divisions. Avoid tall square tooth blocks, thick standalone dental arches, strong vertical black/brown lines, bright white at the deep root edge, or extra gums/tongue. The upper band's source slope must remain, rather than painting a level row and letting the atlas clip it. Preserve the exact source alpha masks when packing, and keep the corrected `Ağız` foreground lip/skin surround unchanged.

The generated paint should retain the new character's material style and ivory teeth. It need not restore original facial RGB or make all teeth brown. The goal is source-compatible shading and deformation readability.

## Shadows: retain the original animated technical masks

The source shadows already carry authored shape, edge softness and animation:

| Mask | Vertices | Source slot bone | Clips with deform timelines |
| --- | ---: | --- | ---: |
| Right brow shadow | 9 | `on_yuz` | 30 |
| Left brow shadow | 10 | `on_yuz` | 30 |
| Moustache shadow | 18 | `biyik` | 8 |
| Beard shadow | 19 | `yuz` | 27 |
| Crown underside | 10 | `yuz` | 6 |

These are unweighted meshes with expression-specific deform coordinates. In particular, the brow shadows are **not static** just because their attachment bone is `on_yuz`; their 30 deform-bearing clips follow the changing expressions. Reusing those source technical masks with the coherent face mapping better fits the request for original-style moving shadows than permanent suppression.

The current shadow RGB has shifted from warm reddish-brown to olive. This is visible color, not just hidden transparent texels. The source/current comparison shows the shift across all five masks. Recover each original source alpha mask directly, then use new warm-neutral brown RGB and calibrated opacity. Preserve the source soft edge and deformation silhouette; do not derive a fresh alpha mask from a newly generated shadow on green. That creates avoidable matte-estimation errors and discards the authored edge softness. If AI supplies shadow color/material suggestions, keep the source mask authoritative when packing.

Start with reduced opacity for the brow and moustache masks and judge them over the current new features. Their original mask outlines can extend beyond the thinner new brow and moustache artwork; opacity must keep the resulting contact shadows soft rather than creating a separate dark ribbon. A single fixed crop or displacement is inappropriate because the masks deform across expressions. Restore one family at a time and compare the full idle, worried, sad and happy cycles, including blends. Keep the obsolete upper-crown shadow suppressed unless separately redesigned for the new crown silhouette; it is not included in this workboard.

Re-enabling the currently suppressed families requires their configuration flags to change and fresh skeleton data to load. The current modifier sets attachment alpha to zero when suppression is enabled; simply flipping the flag on an already modified cached `SkeletonData` object does not restore that original alpha.

## Proposed bounded sequence

1. Retexture and repack the two tooth regions only; validate that every other atlas texel remains unchanged.
2. Restore the five original technical alpha masks using newly chosen warm-neutral shadow RGB. Preserve the corrected face identity, mouth-cover paint and all rig motion.
3. Re-enable brow/moustache shadow families on clean skeleton data and tune only their intensity if needed. Check original animated outlines against the narrower new features throughout the clips.
4. Inspect sad/worry tooth containment, happy tooth-band stretching, brow lift/closure contact shadows, and moustache curl-tip shadows. Compare transitions as well as isolated poses.

No Unity calls, runtime edits or atlas replacements were performed by this inspection. The existing final face renders and earlier source-face render were inspected alongside the actual atlas components.
