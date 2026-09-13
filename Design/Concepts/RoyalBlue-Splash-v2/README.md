# Royal Throne royal-blue splash study v2

Status: **APPROVED AND INSTALLED — 13 September 2026.**

The user requested less green, royal-blue accents, and a more regal Play button. This revision keeps the smooth 3D direction, locked Royal Throne logo, and rotating halo from the previous concept. The initial study was isolated from Unity. The user's subsequent request, “add this in main game in unity,” approved this revision for integration in the independent Codex copy. The exact background and button PNGs are now installed in the main scene; see [current implementation](../../Branding/SPLASH-AND-LOADING.md) and [Unity screenshots](../../Branding/RoyalBlue-Unity/README.md).

## Revised artwork

- `castle-royal-blue.png`: 941 × 1672 opaque PNG. The smooth castle composition remains, with sapphire roofs and banners, stronger cobalt sky, cooler ivory shadows, quieter sage/evergreen foliage and blue-violet/ivory flowers.
- `play-royal-blue-crown.png`: 2172 × 724 RGBA PNG. Royal-blue enamel face, sculpted gold frame, a small crown with sapphire jewel, restrained gold scroll ends and ivory PLAY lettering.
- The loading concept now has a royal-blue panel and blue progress fill, removing the former green UI accent. Gold diamonds, trim and ivory text retain contrast.
- The original approved logo PNG and original halo sprite are reused unchanged. The halo remains the six-second rotation study with softer opacity/falloff documented in v1.

These are raster images with a smooth 3D-rendered appearance. They are not editable 3D models. PNG masters are preserved without post-generation recoloring. Smaller WebP copies are used only for delivering the review preview.

## Review and checks

`splash-composition-preview.png`, `tall-phone-preview.png` and `loading-composition-preview.png` are screenshots rendered from the isolated browser concept, not Unity screenshots. The conversation preview allows comparison with the earlier green castle/button. The earlier option compares those two artwork assets; the revised blue loading-panel styling stays in this v2 preview.

PLAY starts a 4.2-second simulated loading preview and returns to the title. It never launches the game or changes saved progress. Background/button comparison, halo visibility, halo pause, strength/speed, logo size and portrait framing can be explored through the review controls.

Focused checks passed in headless installed Microsoft Edge: images decode, the halo moves, background comparison and halo controls work, loading preview progresses and returns to the splash, reduced-motion preferences stop continuous decorative animation, and no page errors occur. Widths 736, 360 and 320 pixels fit without horizontal overflow; the button target remains over 44 pixels high. See `preview-checks.json` and `asset-metadata.json`.

The v1 concept is retained. The checks in this section describe the original concept-only phase, which did not change Unity Assets, scenes, scripts, Packages or ProjectSettings. Later Unity integration and Play Mode checks are recorded separately in the implementation links above. No device export was performed.

## Generation provenance

Both revised PNGs were created with the built-in image-generation tool using the corresponding v1 asset as an edit target. No Magnific or API/CLI fallback was used.

- Castle source: `../Smooth3D-Splash-v1/castle-study-b-smooth.png`; generated output `exec-2dfd1cbf-9bdb-44a3-a39d-fd90075111a4.png`.
- Button source: `../Smooth3D-Splash-v1/play-button-study.png`; generated output `exec-851f3b98-5d7f-475a-a773-36ab683cf9bd.png`.

Original generated outputs remain in the task's generated-images directory. Copies in this folder are the versioned review assets.

## Castle prompt

```text
Use case: style-transfer.
Edit target: the supplied smooth 3D castle courtyard background. Refine its COLOR ART DIRECTION to royal blue, ivory and gold because the current design feels too green.
Keep the same castle architecture, object positions, portrait 9:16 composition, clear upper sky for a separate logo, open central pathway for a separate button, smooth sculpted polygon-modeled 3D style, rounded shapes, and welcoming atmosphere.
Change the roofs, flags and banners to strong saturated ROYAL BLUE and sapphire blue with restrained gold edging. Shift the upper sky to a slightly deeper elegant cobalt/periwinkle gradient. Give ivory castle and paving soft cool blue/lavender shadow tones while preserving warm cream highlights and golden glowing windows/lanterns.
Reduce the visual dominance of greenery: remove luminous lime/yellow-green tones. Use quieter, lower-saturation sage and deep cool evergreen foliage with gentle blue ambient shadows. Preserve natural green plants, do not turn trees bright blue. In existing foreground and planter flower clusters, use predominantly blue-violet, soft periwinkle and ivory petals with small gold centers rather than bright pink. Keep all foliage surfaces smooth and sculpted, with no leaf texture.
Overall hierarchy: royal blue sky/roofs/banners are the defining accent, ivory palace is the focal environment, restrained gold gives prestige, subdued greenery supports the scene. Still bright enough to feel like a friendly premium casual match-3 game. No gloomy desaturation, no strong monochromatic blue wash, no realism or grit.
Background only. No logo, no PLAY button, no text, no UI, no characters, no light rays, no watermark. Full scene with the existing clean composition preserved.
```

## Play button prompt

```text
Use case: precise-object-edit.
Edit target: the supplied transparent green PLAY button. Create a refined royal-blue version for Royal Throne.
Preserve the straight-on readable horizontal mobile-game CTA, exact centered word "PLAY", chunky ivory letters, smooth polished pre-rendered 3D quality and genuine transparent background.
Replace ALL green with rich sapphire / royal-blue enamel: luminous cobalt upper face, saturated royal-blue center, deep navy lower bevel; soft broad specular highlight, no cyan or turquoise cast, no neon colors.
Give the silhouette a more regal crafted feel: a substantial gently rounded rectangular royal plaque, a sculpted stepped warm-gold frame, restrained symmetrical curved gold end details, and a SMALL integrated gold crown crest at the center of the upper rim, about 12 percent of button width. Crown is a simple three-tip embossed emblem with one small sapphire jewel, connected to the rim. No floating icons or excess ornaments. Preserve a clean uncluttered main blue face and generously readable ivory PLAY text. Subtle gold depth beneath the letters, precise bevels, local contact shadow.
The button should feel prestigious, inviting and tactile, coordinating with a gold-and-purple royal logo. Premium casual-game art, not casino decoration, no filigree clutter. Width-to-height of the full button including crown approximately 3.0:1. Entire silhouette and shadow visible with modest clear margin. Genuine transparent alpha, no checkerboard, no scenery, no background color, no extra text, no watermark. Single button only.
```
