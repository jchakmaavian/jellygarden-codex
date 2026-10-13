# Royal Throne splash and loading design

**Branding update, 13 October 2026:** the user finalized **Royal Tales** and requested a new logo and animation. The old master is backed up; the current runtime sprite is now the Royal Tales artwork. See [Royal Tales logo and animation](royal-tales-splash-logo-v1.md). The earlier Royal Throne logo lock and hash below are historical.

Initial implementation: 8 September 2026. Current royal-blue revision: 13 September 2026, after the user approved the visual study for integration with “add this in main game in unity.” All implementation is in the independent Codex copy.

## Visual design

- The locked Royal Throne logo is unchanged. The exact approved sprite is used on both screens.
- The approved smooth 3D castle has royal-blue roofs, flags and sky, warm ivory walls, gold lights, and subdued green foliage. The separate opaque 941 × 1672 PNG is copied exactly from the approved v2 study, with bilinear sampling and no mipmaps.
- The Play CTA uses the exact 2172 × 724 transparent approved image: sapphire enamel, a sculpted gold frame, a small crown crest and ivory PLAY lettering. It remains a Unity Button with one persistent callback, hover/focus scaling and quick pressed feedback. Its text and decoration are baked into the artwork; there is no duplicate Unity text label.
- The original `wheel.png` light rays rotate behind the logo once every six seconds. `RoyalLogoHalo` uses unscaled time and a soft vertex-alpha falloff, preserving the original texture. The original rotation animation asset is retained unchanged; the new component recreates its six-second clockwise loop. The splash and loading halos share their phase through the transition and do not intercept input.
- The loading page keeps the same castle and logo, with a gold-edged royal-blue panel, animated gold jewels, a moving glint, and a sapphire progress bar. It does not reference the legacy flattened loading image containing the JellyGarden title.
- Responsive safe-area layout protects the logo and controls; the background covers the viewport without stretching. The Play PNG, logo, halo, editable loading text, panel surfaces, and progress elements remain separate scene objects. The art has a rendered 3D appearance; it is not a set of editable 3D models.

## Loading behavior

Play latches input before the first coroutine yield, disables the CTA and presents the loading UI before starting `SceneManager.LoadSceneAsync`. The bar follows Unity's scene-load progress, normalized over its pre-activation 0–0.9 range and smoothed forward. There is no random or timed fake percentage. A short 0.9-second minimum provides a readable transition on fast loads. Three jewels and the progress glint animate with unscaled time.

The title Canvas stays alive through scene activation, blocks input, then fades out over 0.3 seconds and is destroyed. A missing destination or load-start exception restores an actionable title screen instead of leaving a disabled button. No gameplay, balance, saved-progress identity, package, renderer or platform export settings change.

## Editing and previewing

Open `Assets/JellyGarden/Scenes/main.unity` and inspect **Royal Throne UI**. Its children contain **Castle background**, **Title content**, **Floating golden lights**, and **Loading**. The controller exposes layout and loading references; artwork and text remain ordinary Unity components.

The explicit **Tools → Royal Throne → Build splash and loading screens** command regenerates this scene's entire title Canvas from the authoring code. It does not run on import. Save scene edits before invoking it, and change the builder as well if those edits must survive regeneration. It preserves the main camera, input and audio roots and is restricted to this copied project. **Validate title screen** checks required components, approved logo and Play artwork, halo configuration, the single Play callback, legacy-loading-image removal and destination availability.

**Preview loading animation (Play Mode)** (F7) is an Editor-only fifteen-second motion preview in the main scene. Its status explicitly reads “Animation preview”; it returns to the title without loading the game. Actual Play uses the real asynchronous operation. The preview code is excluded from players.

## Asset and generation record

- Runtime castle: `Assets/JellyGarden/Textures/menu/main_menu/royal_castle_blue.png`.
- Castle SHA-256: `3f78e8fa7c8eec2642b68edeeb20acfa59b90197de219ea8de8520d707506729`.
- Runtime button: `Assets/JellyGarden/Textures/menu/main_menu/royal_play_crown.png`.
- Button SHA-256: `cc1bd59b7d45644e145e733026496629f8eec1fd30427dc0f8fb661770c17110`.
- Unchanged logo SHA-256: `7397cba6758c49fc1cc5bf13f64919edc3a33bb97d0515c460623575b298ea22`.
- Background and button are byte-identical to the approved PNG masters. No artwork was regenerated or recolored during Unity integration. Their built-in image-generation prompts and provenance are retained in [royal-blue splash v2](../Concepts/RoyalBlue-Splash-v2/README.md).
- Actual Unity Play Mode captures and verification notes are in [RoyalBlue-Unity](RoyalBlue-Unity/README.md).

### Historical first castle implementation

The earlier twilight background is retained at `Assets/JellyGarden/Textures/menu/main_menu/royal_castle_twilight.png` but is no longer referenced by the main scene. Its SHA-256 is `085f7de54b6669929291522ef1bba9846ec6924a2404992fd37f85ce51b7aa3a`.

- Generated with the built-in image-generation tool, without reference-image inputs. No Magnific or API/CLI fallback was used. Two initial image attempts were rejected by the generation service; the third produced the installed background.
- Original generated file: `exec-883886cd-9798-40de-a07f-193baa3b6d81.png` in this task's generated-images directory; it was copied into the project and preserved at its original location.

### Successful generation prompt

```text
Create a beautiful original vertical illustration of a storybook castle and garden at twilight. Ivory stone castle with blue and purple roofs, warm golden windows, trimmed green hedges, flowerbeds and a stone path. Soft, polished, colorful 3D animated-film art direction. Welcoming and peaceful. Dark blue sky occupies the upper third, castle occupies the middle, courtyard path at the bottom. Portrait 9:16 composition. No words, no logos, no UI elements, no people. Full scene background art.
```
