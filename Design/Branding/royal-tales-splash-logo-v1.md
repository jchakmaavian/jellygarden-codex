# Royal Tales logo and animation

The user finalized **Royal Tales** on 13 October 2026 and requested backup of the Royal Throne logo, a new Royal Tales logo and animation, and replacement in the current game.

## Artwork

- Master: `royal-tales-splash-logo-v1.png`, 1254 x 1254 RGBA PNG with transparent corners.
- Runtime: `Assets/JellyGarden/Textures/menu/main_menu/logo.png`, byte-identical to the master.
- SHA-256: `c3e4781eea430c95a7df3acef05d4f7d2c35fef1d178a4513e3916ca90158b5f`.
- Existing sprite GUID retained: `6c0b96a44d47a0846b80777f8bb71d51`.
- Generated with the built-in imagegen tool, using the approved Royal Throne master as the edit reference. Gold crown, ruby, purple velvet backing and dimensional royal typography are retained; the lower word is now **TALES**.
- Original tool output: `C:/Users/User/.codex/generated_images/01a13475-14fd-7230-abad-9f8138c374dd/exec-775f5514-27c5-454f-82b4-b88d4f2b3cf0.png`.
- Raster master; no vector artwork or font source is claimed.

## Backup

`Archive/Royal-Throne-2026-10-13/` preserves the old runtime PNG, import metadata, approved master, provenance document, naming brief and seven win-logo layers. The original approved master at `royal-throne-splash-logo-v1.png` is also retained unchanged. Its SHA-256 is `7397cba6758c49fc1cc5bf13f64919edc3a33bb97d0515c460623575b298ea22`.

## In-game animation

- The current studio splash continues before the game's automatic loading screen.
- Loading uses the new logo with a 0.65-second scale settle, subtle breathing and three staggered warm-gold jewel gleams. The existing rotating halo remains behind it. Decoration does not intercept input and uses unscaled time.
- The existing reduced-motion preference keeps the logo at its final scale and hides the gleams.
- The level-win celebration uses three sprite regions of the new master: crown, ROYAL and TALES. They reveal on separate beats, then follow the existing celebration swell/exit, trumpets and fireworks. Old THRONE letter layers are not loaded into this animation.
- Homepage copy and the scene title now use Royal Tales.
- Technical namespaces, save keys, application identifiers and the Unity product setting remain unchanged in this artwork task so local progress retains its storage identity. A future build-label change requires a separate save migration. No APK rebuilt.

## Verification

The focused Unity Play Mode command is `Tools/Royal Tales/Verify branding in Play Mode`. Results and actual loading/home/win captures are in `RoyalTales-Verification/`. Consult `checks.txt` for the actual completed verdict; artwork generation alone is not gameplay verification.

## Generation prompt

```text
Use case: text-localization and logo-brand. Edit the supplied Royal Throne transparent game logo into the new Royal Tales logo for the same premium casual match-3 game. Exact text: ROYAL on the upper line and TALES on the lower line. Spell R-O-Y-A-L and T-A-L-E-S. Replace the entire lower word THRONE with beautifully spaced, large TALES lettering; remove every trace of the old lower word. Keep the unmistakable royal character: luminous rounded sculpted gold serif lettering, rich purple velvet throne backing, ornate but clean gold frame, five-point gold crown with red ruby, bottom purple jewel, warm polished 3D shading, deep purple edge outline. Preserve the existing crown, backing, upper word ROYAL, palette, lighting, framing and quality as closely as possible. Balance the five letters TALES to span the lower word area naturally, with generous open counters and readable at phone size. Single standalone full logo, centered, unclipped, matching approximately square 1280x1280 canvas and existing transparent padding. Truly transparent background with alpha, no checkerboard baked in, no scenery, no tagline, no extra words, no device mockup. Production quality crisp edges. This is the authorized replacement brand Royal Tales.
```
