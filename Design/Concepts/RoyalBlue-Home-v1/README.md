# Royal Throne — home screen concept v1

13 September 2026. **Design review only. Not implemented or imported into Unity.**

The user's homepage reference sets the information hierarchy: resources at the top, a central castle, level and area actions above a five-item navigation bar. The visual direction continues the approved royal-blue splash: ivory architecture, sapphire roofs and enamel, warm gold trim, rounded 3D forms, and subdued greenery. The existing approved logo is reused unchanged.

## Review images

- `review/home-1080x2400.png` — tall phone composition.
- `review/home-1080x1920.png` — shorter phone composition.
- `review/area-reward-preview.png` — proposed area reward panel.
- `review/asset-contact-sheet.png` — artwork and separate interface surfaces.
- `preview-checks.json` — results and limits of the browser preview checks.

The interactive concept opens local review panels from the resource bar, level button, area card and navigation. Settings can pause the ambient glow and particles. These are preview interactions with sample values from the reference; they do not load the board, spend currency, claim rewards, or change a game save.

## Asset pack

**24 separate PNG assets plus the existing project font.**

| Group | Included files |
| --- | --- |
| Environment | `assets/home-castle.png` |
| Main action | `assets/button-crowned-blank.png` |
| Profile | `assets/avatar-king.png` |
| Resources | Coin, heart and star icons |
| Utility and reward | Settings icon and royal chest |
| Navigation | Story book, trophy, home castle, team shield and cards |
| Branding | `assets/logo-locked.png`, byte-identical to the approved master |
| Interface surfaces | Ivory resource plate, profile frame, gold area panel, blue/gold navigation background, progress track and fill, ivory dialog, blue dialog button, close and add buttons |

Original artwork PNGs are retained at their generated resolution. `assets/preview/` contains smaller WebP delivery copies for the interactive review. The button delivery copy trims transparent margins; it does not repaint the artwork. `assets/surfaces/` contains PNG renders of the editable UI surfaces, with 12 logical pixels of transparent padding and 2.5× scale. Dynamic resource values, level labels and progress labels remain separate text.

`asset-manifest.json` and `surface-manifest.json` record dimensions, transparency, hashes, provenance and delivery paths. `generation-record.json` contains the selected prompts and source paths. Artwork was generated with the **built-in image_gen tool**; interface surfaces were rendered from the concept's CSS. `assets/BorisBlackBloxx.ttf` comes from the existing project and retains its original licensing requirements.

The environment is one rendered background plate. The castle, fountain, plants and scenery are baked into that image; this pack does not contain separate 3D meshes, rigging or a water simulation. The browser glow and particles are a small motion study, not a completed Unity VFX system.

## Design decisions

- Keep one clear next action: the crowned blue **Level 12** button. Use warm gold for the secondary **Area 1** reward card.
- Keep the reference's coin, life and star shapes recognizable. Limit small-icon ornamentation to preserve readability.
- Use gold trim and warm ivory labels consistently across both action panels and navigation.
- Keep Home visibly selected, with a raised castle icon. Other tabs open explanatory review panels; their features and economies are proposals.
- Keep text editable for future localization. The sample 2,423 coins, 5 lives, 3 stars, Level 12 and 5/6 progress do not define progression or balancing rules.
- The proposed profile king is an avatar study, not approval of final character design or story canon.

## Verification scope

Review checks cover image decoding; four available widths (320, 360, 432 and 736 pixels); tall 9:20 and shorter 9:16 compositions; control bounds and minimum tap areas; resource, level, area and navigation dialogs; return to Home; keyboard focus and Escape; ambient motion and reduced-motion preference; browser script errors. Geometry checks and interaction checks run in separate sandbox loads.

Screenshots are browser renders. They are not Unity captures, device performance tests, usability research or evidence that players prefer this design. The user should review the composition, character style, icon readability and action hierarchy before a separate implementation task.

## Editable sources

`home-preview.template.html` is the literal interface source. `prepare-preview.cjs` packages the selected artwork into a compact embedded preview; pass an absolute fragment destination. It uses the included PNG masters and font first, so restoring the pack does not require the original generation folder. `check-preview.cjs` tests the sandbox-wrapped preview. `export-assets.cjs` exports native UI surfaces and the contact sheet. These are design tools and are outside Unity's `Assets` folder. Rebuilding requires Node with Sharp; testing/exporting also requires Playwright and a Chromium browser. The sandbox wrapper is supplied by the visualize skill's `render.py`.

All work for this concept stays in `Design/Concepts/RoyalBlue-Home-v1` inside the independent copy. No scene, prefab, runtime script, package, project setting, editor state or original/boss project was changed. Online backup is not claimed; the prior GitHub push approval remains pending.
