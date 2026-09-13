# Area 001 art generation

**Wooden-door revision:** States 2–4 were replaced on 2026-09-12. See the [new prompts and masters](wooden-door/prompts.md) and [asset manifest](wooden-door/assets.json). The arch-only originals described below are retained in `wooden-door/previous-art/`.

Generated with built-in `image_gen` in this session. No CLI or API fallback.
The first draft had front banners; a corrective edit removed those banners before deriving the repair sequence.

## treasure_gate_prompt

Use case: illustration-story. Asset type: final full-screen background for Area 001 of King Leo, a polished family match-3 mobile game. Generate a brand-new beautiful portrait 9:16 castle gate scene, no UI and no text. Art direction: premium charming hand-painted 3D storybook game illustration, boldly rounded and appealing shapes, soft painterly shading, saturated sky blue and turquoise, warm cream stone, coral flowers, sunlit fresh green grass, gold highlights; an inviting magical toy kingdom. Avoid photographic textures, hyperrealism, brown grimness, abandoned ruins, generic realistic medieval castle. Fixed camera, front-facing slightly elevated view. Two friendly rounded small gate towers with glossy cobalt-blue conical roofs frame an arched open passage; through it glimpse an inviting pastel courtyard and distant castle. Main gate architecture centered within middle 65% of width, between 23% and 70% of height so it survives portrait phone cropping. Top 20% peaceful blue sky and soft clouds. Bottom 25% uncluttered curving cream-paved approach with flower beds at edges, usable negative space for game buttons. This is initial state 0 before a festival: A lush but pretty ivy vine covers the arch and partly trails across the passage, to be cleared as task 1; the gate stone itself is sound. Two short decorative wooden arch supports beside the passage have no connecting curved top beam yet, to be added as task 2; no broken wood. Banner rods are empty, to receive royal blue-and-gold pennants in task 3. Exactly two decorative brass lanterns on the front flanking the passage are currently unlit, to glow warmly in task 4. A small smiling sun medallion centered above the stone arch, partly covered by ivy. Lower right at the edge of the gate stands a small charming cart of rolled festival decorations and an old cream cupboard with a golden ribbon peeping out. No characters, no lettering, no numbers, no watermark. Finish as clean publication-quality actual game artwork, not a concept sketch.

## treasure_pip_prompt

Use case: illustration-story. Asset type: a game character portrait sprite on a genuinely transparent background. Create Pip, a cute tiny friendly paper dragon who is a living treasure map for the family mobile game King Leo. Full little dragon centered with generous transparent margins, large expressive navy-blue eyes, round friendly snout, cream-and-gold parchment body with neat origami folds, sky-blue folded wing interiors, curled ribbon-like tail, small gold crown-shaped folded head crest, delicate dotted map route and one tiny compass illustration printed on one wing. Charming polished hand-painted 3D cartoon game style, bold readable silhouette, soft warm highlights, rounded folded paper shapes, delighted welcoming smile. Pip floats in a three-quarter pose, wings lifted and one little forepaw waving. No teeth, no fire, no scary features, no words, no actual lettering, no UI, no frame, no opaque backdrop, no ground shadow, no watermark. Square image, clean alpha edges. Actual production sprite, not a character sheet.

## treasure_state1_prompt

Edit this exact portrait game background to show completion of task 1, Clear the ivy. Remove all ivy vines and climbing leaves from the FRONT stone gate arch, both front towers, and doorway supports, revealing clean warm cream stone and the existing smiling sun medallion. Preserve the flowerpots, flowerbeds, bushes beside the path, every distant plant, wooden brackets, unlit brass lanterns, EMPTY front banner rods, decoration cart, cupboard, pavement, castle beyond and sky. Do not add banners or a wooden top beam. Keep camera, framing, colors, lighting, all architecture and all object positions precisely identical to the reference, same resolution. Only remove the climbing ivy on the front gate, no other edits, no text.

## treasure_state2_prompt

Edit this exact game background into repair state 2. Preserve the image, camera, geometry, lighting, colors, sun medallion, empty banner poles, unlit lanterns, surroundings, and all objects pixel-aligned as much as possible. The two existing wooden brackets at the sides of the opening now support a newly repaired beautiful curved warm oak arch beam following the underside of the stone arch, spanning between the brackets. Add ONLY that graceful solid wooden arch beam with gold end bolts. Do not obstruct the passage or cover the sun. No banners yet. No ivy. No text, no characters. High polish cheerful kids' royal treasure hunt mobile game environment.

## treasure_state3_prompt

Edit this EXACT image into repair state 3 for a mobile game: preserve all architecture, camera, geometry, position, blue roofs, sunshine, wooden curved arch beam, flowers, unlit lanterns, cart, and smiling sun medallion perfectly. Add exactly two beautiful royal blue cloth pennant banners with bright gold borders and a simple stylized gold sun symbol (not lettering), one suspended down from EACH existing horizontal brass pole on the front gate towers. The new banners should hang to around the height of the top of the wooden brackets, not over the lanterns. The cloth is slightly waving gently, beautifully textured. No other changes. Keep the lanterns unlit. No people, no animals, no text. Same polished cheerful 3D storybook environment.

## treasure_state4_prompt

Edit this EXACT image into the final repair state 4 for a mobile game. Only change the two foreground brass lanterns: fill each glass chamber with a clearly visible warm luminous golden magical light, glowing gently onto the neighboring stone, with a small luminous star-like sparkle at each lantern. Add a tiny glint at the tip of the smiling sun medallion above the gate to draw attention to the clue. Keep bright cheerful daytime, do NOT turn the scene into dusk or night. Preserve camera, geometry, wooden arch, banners, clouds, landscape, all other objects and their positions EXACTLY. No extra lamps, characters, text or props. Polished joyful royal treasure hunt background. Lanterns are the only major change.

## Corrected initial state

The accepted state 0 is the initial gate scene edited to remove the two front blue-and-gold banners while retaining the empty horizontal rods, climbing ivy, wooden brackets, and unlit lanterns. The original draft with banners was not shipped.

## Master provenance

| Export | Generated master |
|---|---|
| state 0 | exec-fb940a99-3bf5-4917-81ea-5d32989a3ccf.png |
| state 1 | exec-c05b5a0d-bbbd-4279-9cde-5a37a06bc9ab.png |
| state 2 | exec-fa590a3d-1681-4dbf-be5b-3ce234ee837a.png |
| state 3 | exec-7f9a0fd6-76dc-447b-8de3-012e7689b8a0.png |
| state 4 | exec-c7962004-b237-442a-966e-672bb43599ce.png |
| Pip | exec-4cfa26c9-ca99-4b3c-9ba7-4e4787fefdb0.png |

Masters were copied without pixel modification into `Assets/JellyGarden/Resources/Story/TreasureHunt/Art/`. `assets.json` records dimensions and SHA-256 hashes. Task icons and the clue are Unity sprite crops, defined in the authoring JSON.
