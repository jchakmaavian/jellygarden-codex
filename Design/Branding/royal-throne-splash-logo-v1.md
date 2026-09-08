# Royal Throne — final approved splash logo

Created 8 September 2026 for the user's request: generate a splash-screen logo for Royal Throne with the appeal of Royal Match's game logo.

- Deliverable: `royal-throne-splash-logo-v1.png`.
- Format: 1254 × 1254 RGBA PNG, 1,510,395 bytes. Transparency verified from the alpha channel; the generated alpha is preserved.
- Status: FINAL. The user explicitly approved and locked this exact artwork on 8 September 2026: "lock this logo.. final it. add this in the game. remover jellygarden logo and title." Keep the v1 filename as the approved master; further logo design changes require a new user request.
- Generation: built-in image generation, not Magnific and not the CLI fallback. Generated as a new image without reference-image inputs.
- Art direction: dimensional gold display lettering, rich purple throne backing, five-point crown, central ruby accent, transparent background.
- Visual check: both words are correctly spelled, the full crown and lettering are visible, and the logo is isolated without a tagline or scene background.
- Reproduction record: the generation prompt below. This is raster artwork; the prompt is not a layered editable source, and the delivery does not include vector paths, a font file, or separated animation layers.
- Integration: the identical PNG replaces `Assets/JellyGarden/Textures/menu/main_menu/logo.png`, preserving sprite GUID `6c0b96a44d47a0846b80777f8bb71d51`. The opening scene uses a 600 × 600 aspect-preserving image and retains its existing entrance animation. Unity product title is Royal Throne. SHA-256 of both master and runtime image: `7397cba6758c49fc1cc5bf13f64919edc3a33bb97d0515c460623575b298ea22`. The former kit artwork provenance was removed from this replaced image metadata; other kit attribution remains intact.

## Final generation prompt

```text
Use case: logo-brand.
Asset type: high-resolution standalone transparent PNG title logo for the splash screen of a premium casual match-3 mobile game.
Primary request: Create an original "Royal Throne" logo with the confident, joyful, highly polished 3D appeal of leading royal match-3 games such as Royal Match. Translate the broad qualities—bold readable lettering, rich sculpted gold, inviting royal character—into a distinct Royal Throne design.
Text (verbatim, the ONLY text): "ROYAL" on the first line, "THRONE" on the second line. Spell exactly R-O-Y-A-L and T-H-R-O-N-E.
Composition: One large centered compact emblem, approximately 1.35:1 width to height, on a square transparent canvas with 10% clear padding all around. "THRONE" is the slightly wider, dominant word; "ROYAL" nests above it. A single elegant, friendly five-point gold crown with a central ruby jewel crowns the composition. Integrate a very simple plush royal-purple throne-back silhouette behind the upper lettering, as a subtle supporting shape rather than a detailed illustration. Text should dominate and read immediately at small game-screen size.
Typography: custom heavy rounded display serif lettering, broad clean strokes, charming and prestigious, confident upright shapes with slight lively variation, open counters, generous enough spacing that every letter remains distinct. No script lettering. Smooth dimensional bevels and substantial sculpted depth; precise typographic spacing rather than generic font pasted over a scene.
Materials and palette: luminous golden-yellow front faces, warm amber beveled edges, orange-gold depth, restrained ivory highlights from upper left. Rich royal-purple recessed backing and a clean deep-plum outer keyline separate the logo from any splash-screen background. The crown ruby is a small rich crimson accent. Consistent professionally art-directed lighting and soft ambient shading.
Finish: exceptionally polished stylized 3D game branding, clean silhouettes, crisp production-quality antialiased edges, strong value hierarchy, soft rounded premium surfaces, a few tiny purposeful gleams only. Strong readable solid geometry, not noisy metallic photorealism.
Background: genuinely transparent alpha. No coloured rectangular background, no baked checkerboard, no scenery or full splash screen. Shadows should stay local to the emblem. Request 2048x2048 or the highest practical native resolution.
Avoid: extra words, subtitle, "match 3", tagline, watermark, mockup/device frame, characters/faces, castles or landscape, swords, shields, laurels, ornate unreadable filigree, excessive particles, thin letter strokes, cropped crown, copied Royal Match letterforms or arrangement. Deliver a single logo, not a contact sheet.
```
