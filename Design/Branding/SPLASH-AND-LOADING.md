# Royal Throne splash and loading design

Implemented 8 September 2026 in the independent Codex copy, following the user's request for a new splash background, redesigned Play CTA, and redesigned loading page with animation.

## Visual design

- The locked Royal Throne logo is unchanged. The exact approved sprite is used on both screens.
- A twilight castle, illuminated windows, garden and stone approach replace the candy landscape on the opening screen. The castle art is a separate opaque 941 × 1672 PNG, imported at native resolution with bilinear sampling and no mipmaps.
- The Play CTA is an editable Unity UI button: emerald gradient, gold rim and depth, ivory PLAY lettering, hover/focus highlight and quick pressed-scale feedback. Its hit target is a single Button with one persistent callback.
- The loading page combines the same castle with a darker purple veil, the approved logo, a gold-edged purple panel, animated jewels, a moving glint, and a progress bar. It does not reference the legacy flattened loading image containing the JellyGarden title.
- Responsive safe-area layout protects the logo and controls; the background covers the viewport without stretching. Text, button surfaces, progress elements and animation remain separate scene objects.

## Loading behavior

Play latches input before the first coroutine yield, disables the CTA and presents the loading UI before starting `SceneManager.LoadSceneAsync`. The bar follows Unity's scene-load progress, normalized over its pre-activation 0–0.9 range and smoothed forward. There is no random or timed fake percentage. A short 0.9-second minimum provides a readable transition on fast loads. Three jewels and the progress glint animate with unscaled time.

The title Canvas stays alive through scene activation, blocks input, then fades out over 0.3 seconds and is destroyed. A missing destination or load-start exception restores an actionable title screen instead of leaving a disabled button. No gameplay, balance, saved-progress identity, package, renderer or platform export settings change.

## Editing and previewing

Open `Assets/JellyGarden/Scenes/main.unity` and inspect **Royal Throne UI**. Its children contain **Castle background**, **Title content**, **Floating golden lights**, and **Loading**. The controller exposes layout and loading references; artwork and text remain ordinary Unity components.

The explicit **Tools → Royal Throne → Build splash and loading screens** command regenerates this scene's entire title Canvas from the authoring code. It does not run on import. Save scene edits before invoking it, and change the builder as well if those edits must survive regeneration. It preserves the main camera, input and audio roots and is restricted to this copied project. **Validate title screen** checks required components, logo identity, the single Play callback, legacy-loading-image removal and destination availability.

**Preview loading animation (Play Mode)** (F7) is an Editor-only fifteen-second motion preview in the main scene. Its status explicitly reads “Animation preview”; it returns to the title without loading the game. Actual Play uses the real asynchronous operation. The preview code is excluded from players.

## Asset and generation record

- Runtime art: `Assets/JellyGarden/Textures/menu/main_menu/royal_castle_twilight.png`.
- Castle SHA-256: `085f7de54b6669929291522ef1bba9846ec6924a2404992fd37f85ce51b7aa3a`.
- Unchanged logo SHA-256: `7397cba6758c49fc1cc5bf13f64919edc3a33bb97d0515c460623575b298ea22`.
- Generated with the built-in image-generation tool, without reference-image inputs. No Magnific or API/CLI fallback was used. Two initial image attempts were rejected by the generation service; the third produced the installed background.
- Original generated file: `exec-883886cd-9798-40de-a07f-193baa3b6d81.png` in this task's generated-images directory; it was copied into the project and preserved at its original location.

### Successful generation prompt

```text
Create a beautiful original vertical illustration of a storybook castle and garden at twilight. Ivory stone castle with blue and purple roofs, warm golden windows, trimmed green hedges, flowerbeds and a stone path. Soft, polished, colorful 3D animated-film art direction. Welcoming and peaceful. Dark blue sky occupies the upper third, castle occupies the middle, courtyard path at the bottom. Portrait 9:16 composition. No words, no logos, no UI elements, no people. Full scene background art.
```
