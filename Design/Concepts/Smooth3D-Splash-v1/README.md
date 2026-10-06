# Royal Throne smooth 3D splash study v1

Status: **CONCEPT — awaiting user visual review. Not finalized or installed.**

The user asked on 8 September 2026 to restore the former light halo and develop a consistent smooth, polygon-modeled, rendered 3D look, generating and reviewing assets before implementation. This folder is a design review package only. The existing Unity splash and loading implementation stays at commit `67117c70315d5c9552bce7c13778a53b449cfef9`.

## Review assets

- `castle-study-a.png`: initial brighter rounded castle, 941 × 1672 opaque PNG. Still contains too much fine foliage and paving detail.
- `castle-study-b-smooth.png`: recommended refinement, 941 × 1672 opaque PNG. Broad smooth roof forms, beveled ivory walls, sculpted green foliage and simplified paving.
- `play-button-study.png`: separate green-and-gold rendered button with PLAY lettering, 2172 × 724 RGBA PNG. Transparency is present. This is a concept image; production button states and edge inspection remain part of a later approved integration.
- `splash-composition-preview.png`: browser-rendered concept composition with the exact existing logo, refined castle, proposed Play artwork and the original halo sprite.
- `tall-phone-preview.png`: the same composition in 9:20 portrait framing.
- `loading-composition-preview.png`: loading-layout and motion study using the same artwork. The panel, bar and diamonds are separate browser UI elements; progress is simulated and explicitly labeled Animation preview.
- `preview-checks.json`: focused browser preview check results.

The PNGs have a 3D-rendered appearance. They are raster artwork, not editable 3D models. Original generated masters remain unchanged; smaller WebP delivery copies are embedded only in the conversation preview.

## Logo and halo

The approved master `Design/Branding/royal-throne-splash-logo-v1.png` is unchanged. No logo regeneration or identity changes occurred.

The previous scene at commit `54c9932` contains a Wheel object using `Assets/JellyGarden/Textures/menu/main_menu/wheel.png` (sprite GUID `6d8ae7857adbd0c4794cf2cedd05e523`) and `Assets/JellyGarden/Animation/rotation.anim` (GUID `a59c8170f5d4c11408cf5715954a15a0`). The legacy clip has a six-second 0-to-minus-360-degree Z rotation and Loop wrap mode. In the browser's downward-Y coordinate system this is reproduced as a clockwise 360-degree loop.

The review reuses that sprite behind the logo, with a proposed softer 30% opacity and radial falloff. Its artwork and six-second rotation are retained; the opacity, size and falloff are a proposed adaptation for the new background, not an exact reproduction of the old scene. Review controls allow halo on/off, motion pause, 6–18 second duration, strength, logo size, background comparison, and 9:16/9:20 framing. Reduced-motion preferences stop continuous decorative movement.

## Visual assessment

Study B is the recommended direction: its larger smooth surfaces have a consistent material language with the dimensional approved logo and the new button. The brighter sky separates the gold/plum title, while the main door and central courtyard remain visible below it. The first study is retained for comparison, not marked approved.

A live conversation preview lets the user compare Study B, Study A and the current castle background using the same proposed overlay. The current-background option is an art comparison, not an exact screenshot of the installed game. Selecting PLAY runs a 4.2-second simulated loading motion preview and returns to the splash; it never starts Unity or changes game progress.

## Verification and boundaries

Verified in headless installed Microsoft Edge using Playwright: all images decode, halo transforms change with time, background comparison and halo toggles work, pause works, simulated progress advances and returns to the splash, reduced-motion preferences are honored, and no page errors occur. Checked available widths 736, 360 and 320 pixels without horizontal overflow; the Play target remains larger than 44 pixels in height. Inspected rendered 9:16 splash, 9:20 splash and loading compositions.

This validates the isolated concept preview only. It is not a Unity implementation test, device performance measurement, or final user acceptance. No Unity Assets, scenes, scripts, Packages or ProjectSettings were changed for this study. No Unity Editor input was used.

## Generation provenance

All three assets used the built-in image-generation tool. No Magnific, API/CLI fallback, or third-party asset download was used. Study A and the Play button were new generations with no reference images. Study B was an edit of Study A, preserving the scene composition while simplifying shapes and materials.

Original output filenames:
- A: `exec-0b6e7d0d-b39f-4888-9769-10c131859c97.png`
- B: `exec-747a2b3f-b8c5-4f0b-8468-5bb9b955eb37.png`
- Play: `exec-3f851ecb-6a81-46ab-b925-5de3e9f80c37.png`

## Prompt: Study A

```text
Use case: stylized-concept.
Asset type: original full-bleed portrait 9:16 splash BACKGROUND ONLY for a premium casual match-3 game, Royal Throne.
Scene: an inviting royal castle courtyard and garden. A charming substantial ivory castle with broad rounded cylindrical towers, chunky cornices, blue-violet curved conical roofs, restrained gold caps and warm glowing arched windows. A single central welcoming doorway faces a gently widening smooth stone path. Rounded clipped hedges, grouped spherical tree canopies, a few oversized simple flowers and two small warm garden lanterns frame the lower sides.
Style: unmistakably pre-rendered smooth stylized 3D mobile-game art. Simplified polygon-built forms with smooth normals, beautifully beveled silhouettes, clean satin/plastic/porcelain surfaces, gentle ambient occlusion, soft large-area highlights and rich luminous colors. Premium casual-game quality in the broad visual family of Royal Match, with an original castle design. Every object should feel modeled and softly sculpted. Large readable shapes, restrained detail. NOT a painting, NOT photographic architecture, NOT faceted low-poly triangles. No gritty stone, individual realistic leaves, ornate filigree, dense flower noise, tiny roof tiles or detailed brick textures.
Composition: portrait 9:16, centered forward-facing camera at courtyard level, slight upward view. Upper 35 percent is clear deep periwinkle-to-blue sky reserved for a separate large logo and light halo; no tall tower intrudes into that upper logo zone. Castle occupies the middle 35–70 percent of frame. Quiet open pathway in the lower central fifth reserved for a separate play button. Garden frames rather than obstructs this open center. Friendly rounded distant hills.
Lighting: bright blue-hour fairytale ambience with soft warm late-sunset rim light; luminous cream walls, readable green vegetation, gentle lilac shadows, cheerful and welcoming rather than dark. Subtle blue sky gradient and only a few soft puffy clouds near the side edges. Clean cinematic 3D rendering with crisp silhouettes, smooth surfaces.
Constraints: background art only, no words, no logo, no buttons, no lettering, no people, no characters, no watermark, no baked light rays. Entire picture in the same consistent smooth 3D art direction.
```

## Prompt: Study B refinement

Reference: Study A above.

```text
Use case: style-transfer.
Edit target: supplied castle courtyard background.
Create a second material-and-shape refinement of this SAME scene for a premium casual mobile game.
Keep the overall portrait 9:16 composition, blue open upper sky, centered ivory castle with purple-blue roofs, gold trims, welcoming entrance, garden path, lantern positions, and warm soft lighting.
Change only the shape/material rendering toward MUCH simpler, smoother, polished 3D game assets: every leafy tree and hedge becomes a sculpted collection of large inflated rounded lobes with broad soft gradients. Remove ALL tiny leaf texture from vegetation. Use a few large simple five-petal flower models. Castle roofs have broad clean smooth curved surfaces with only a few widely separated bands, not lots of individual roof tiles. Walls are satin smooth cream with soft bevels, very sparse structural seams, no brick texture. Path slabs are large soft rounded smooth untextured lavender-cream forms. Every edge is gently beveled. Soft toy-like shaded 3D geometry, rich colored soft ambient shadows, broad soft specular highlights, clean readable silhouette. This should look like high-quality rendered mobile-game assets, not painted illustration, not a realistic castle photograph and not faceted polygon art.
Aim for the appealing rounded shape language and instantly readable materials associated with polished casual match-3 games. Retain depth and professional lighting; do not make it flat or primitive. No extra ornaments. Background only: no logo, no text, no UI, no characters, no rays.
```

## Prompt: Play button

```text
Use case: ui-mockup.
Asset type: ONE standalone transparent PNG PLAY button asset for Royal Throne, an original premium casual match-3 game.
Create a front-facing, horizontally wide rounded rectangle game button, width-to-height approximately 3.3:1, centered on a transparent wide canvas with 5 percent clear padding.
Design: a thick smooth cushion-like emerald green face with a gently convex surface, a clean softly beveled polished golden rim and restrained amber lower depth. Luminous spring-green highlight along the upper face, saturated emerald middle, deeper jade toward bottom. Soft studio lighting from upper left, broad clean highlights, friendly substantial smooth 3D geometry like a rendered toy object, clean antialiased silhouette. The gold should match a cheerful yellow-gold crown, with restrained shine.
Text exactly "PLAY", four large centered uppercase ivory letters with chunky friendly rounded sans-serif forms, very easy to read at small mobile sizes. Subtle dimensional thickness and a soft dark-green contact shadow. No serif, no cursive, no punctuation. Text should occupy around 55 percent of the button width and 50 percent of face height.
Constraints: a single button only, straight-on view, horizontal and symmetrical, genuine transparent alpha background, no scenery, no other UI, no hands, no separate symbols, no logo, no crown attached to the button, no sparkles outside silhouette, no watermark, no baked checkerboard. Entire button including shadow must be visible. Professional pre-rendered smooth 3D mobile-game asset, not a flat website button.
```
