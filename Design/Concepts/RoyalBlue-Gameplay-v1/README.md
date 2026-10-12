# Royal Throne — gameplay prototype v1

Status: **design review only, 13 September 2026**. The user's latest request explicitly excludes Unity implementation. This folder is isolated from Assets, scenes, packages, save data, and the original project.

Open `prototype.html` in a browser. It works offline and contains its images and font. The same prototype is presented inline in the Codex conversation.

## What to review

- The board has first priority: eight columns, royal objects, a calm blue surface and a gold frame. Goals and moves remain visible beside the king. Score and legacy star-meter chrome are removed from this proposed gameplay layout.
- Match by dragging to a neighbor, or clicking two neighboring normal pieces. Tap a power-up to activate it. Drag two neighboring power-ups together to combine them.
- Use **Try** to stage any power-up or one of the ten pairings. Highlighted tiles show where to interact. These are explicit review scenarios; normal free play creates power-ups through matches.
- Use **Screen** to review the pre-level panel, goal introduction, pause, victory blasts, success, failure and power-up guide. Asset library shows the complete new object family.
- The king celebrates cascades/combos, thinks when moves are low, leans toward pointer activity, and responds when tapped. Motion uses three WebP portraits and transforms. This is a sprite-based acting study, not a rigged 3D character or finished facial animation.
- Hammer, row clear, column clear and mix are usable tools with session inventory. They do not consume moves. Pre-level selections place power-ups on the opening board. These prototype stocks reset locally.
- Sound effects are optional temporary synthesized cues, disabled initially. Gentle motion respects the operating system preference and can be changed in Pause. Production audio, haptics and device profiling remain later work.

## Design decisions

The earlier home/splash visual language is retained: cobalt blue, warm ivory, polished gold, the existing display font and the established king. New ruby, shield, crown, leaf and lily objects replace the candy family in this prototype. The board tiles and UI surfaces are resolution-independent browser geometry; effects use a single canvas with generated particle geometry. No video is embedded into the game.

The king sits beside the goals without covering playable cells. Combo captions appear above the board; the flight path and targets stay visible. Dialogs use one consistent title, ivory content panel and blue primary action. A short, skippable celebration leads to a star reveal and home reward-flight study. The reward flyout is a presentation prototype, not a connected economy or restoration service.

## Verification

See `review/engine-checks.json` and `review/browser-checks.json` for executable results. The engine suite covers match patterns, invalid moves, blockers, gravity, all pairings, tools, win/lose precedence, 100 opening seeds and 50 automated games. Browser checks cover real pointer interactions, six viewport widths, eight screens per width, cancellation, pause, reduced motion, and assets decoding.

The performance report describes the local desktop browser only. It does not establish Unity/mobile frame rate, installed size, memory residency, load speed on a phone, level difficulty balance, or quality superiority to Royal Match. The board is one authored prototype arrangement; it does not migrate the game's 100 levels or implement every obstacle from the reference games.

## Files

- `prototype.html`: self-contained offline playable review.
- `assets/*.webp`: 19 optimized WebP assets; 412,642 bytes total (403.0 KiB).
- `fonts/BorisBlackBloxx.ttf`: existing project display font, 53,152 bytes.
- `asset-manifest.json`: exact sizes, dimensions, alpha flags and hashes; approximately 8.78 MiB of theoretical RGBA pixels across the images, before caches/canvas/rendering overhead.
- `RULES-AND-MOTION.md`: proposed mechanics, VFX timings and implementation gates.
- `REFERENCE-REVIEW.md`: source video observations and verified rules sources.
- `generation-record.json`: built-in image-generation prompts, output provenance and reference roles.
- `engine.js`, `view.js`, `prototype.template.html`: editable prototype source.
- `build-preview.cjs`, `prepare-assets.cjs`: assembly/export recipes.
- `test-engine.cjs`, `check-preview.cjs`: logic and interaction checks.
- `review/`: rendered screen captures and verification reports, not shipping game assets.

## Asset and Unity handoff

All prototype raster assets are WebP with retained alpha. Keep this review pack outside Unity Assets until the design is approved. WebP file size is an interchange/download measurement, not GPU texture memory. Unity 6's documented built-in texture importer does not list WebP, so a future integration must choose and validate an editor-only WebP decode/import pipeline, then platform texture compression and sprite atlases. Do not simply rename the files or assume a WebP will become a usable Sprite automatically.

After visual review, the next implementation milestone should be one Unity test level with the same rules, responsive HUD, controller/VFX separation, pooled effects, matching input and reduced-motion behavior. Only after that passes should we migrate level data and expand obstacles. No Android/iOS export is part of this milestone.

Completed design work is committed locally. No GitHub push is attempted in this review turn; the earlier automatic-review restriction on that push remains unresolved.
