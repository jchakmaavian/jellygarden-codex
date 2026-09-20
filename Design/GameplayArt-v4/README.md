# Gameplay art v4 import

The supplied Royal Throne gameplay pack replaces ornate gameplay pieces with clearer shapes. The source folder was read only. All 79 supplied bitmaps, including both previews, were inspected before the mapping was approved. See the [asset review](asset-review.md), [inventory](inventory.md) and numbered contact sheets.

## Preparation checks

- **77 lossless WebP sprites converted to 77 lossless PNGs**, totaling 5,498,629 bytes. Previews are excluded from runtime resources.
- All original dimensions and **every decoded RGBA byte match**, including partial alpha and RGB stored under fully transparent pixels. No resizing, recoloring, painting, alpha thresholding or PNG cropping was performed.
- The [import manifest](import-manifest.json) records source and output checksums, decoded-pixel checksums, source provenance metadata, bounds and pivot for every asset.
- Source WebP files and their manifest were not modified. The pack's statement that its source artwork was generated from roughly 180–256px atlas objects is supplied provenance, not independently verified generation history.

## Unity integration

`RoyalGameplayArt.Art(key)` loads and caches gameplay-only sprites. `For(type, color)` keeps color IDs 0–5 as red block, blue shield, yellow crown, green leaf, magenta crystal and porcelain vase. It supplies separate native horizontal and vertical rockets, TNT barrel, color ball and propeller. Home and splash continue using their existing art catalog.

The original PNG canvases remain intact. `Sprite.Create` uses measured alpha-visible rectangles and pivots that retain the original canvas center. This lets pieces fill their cells without the extra transparent margin reducing their apparent size. Board tiles, grass states and every frame part use full canvas bounds for alignment. The rectangular frame also has its supplied 40px nine-slice border.

`RoyalGameplayArtImporter` applies settings only under `Assets/JellyGarden/Resources/RoyalGameplayV4/`: explicit 2D texture shape, sRGB, preserved source alpha, bilinear filtering, clamp, no mipmaps, no power-of-two scaling, maximum size 512, uncompressed GPU textures and no CPU read/write copy. Unity's alpha-edge transparency processing is an import setting; it does not rewrite the source PNG files. Textures load on demand rather than all 77 at startup. The importer explicitly corrects the cubemap shape that Unity initially inferred from minimal new metadata; texture type alone does not reset shape.

Run **Tools → Royal Throne → Gameplay → Import and verify gameplay art v4** after Unity compiles. This verifies all 77 resources load, sprite rectangles stay inside their textures, pivots retain source centers and textures have the intended memory settings. Unity compile and playable integration results are recorded separately by the main gameplay verification pass; the conversion checks above do not claim a completed gameplay test.

## Boundaries

This pack contains static artwork only: no prefabs, materials, models, audio or animation clips. Imported grass variants and optional booster art do not add mechanics. Burst groups contain several visible fragments in one image; independent falling pieces require component extraction or individual particle sprites. Board frames include outer corners but no authored concave corners for irregular boards.
