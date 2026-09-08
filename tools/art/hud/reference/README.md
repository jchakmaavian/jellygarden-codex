# Reference HUD

The active in-game HUD uses `hud_reference_frame`, `hud_reference_moves`,
`hud_reference_pill`, and `hud_reference_badge` in
`Assets/JellyGarden/Resources/Story/UI`. The previous HUD assets remain available.

The frame was edited with the built-in imagegen tool from the user's HUD
reference. `frame-compact-source.png` is the active, shorter frame without the
decorative crown; `frame-source.png` preserves the previous generation. The Unity
importer converts its exterior preview matte to alpha without removing interior
highlights. Counter plates and badges are reusable rounded UI sprites authored
by the same importer. Text, numbers, target icons, and Leo's animation remain
live Unity UI elements.

To regenerate and install, use **JellyGarden > Story > Import Reference HUD Art**,
then **Bake Level HUD into the scene**. The headless equivalent is
`JellyGarden.Scripts.Story.Editor.StoryHudReferenceArt.BakeHeadless`.
Verify with **JellyGarden > Story > Verify the baked HUD**.

Geometry is measured against the 2172 × 724 compact source, in `StoryLevelHud.cs`.
The strip is 360 design units high. Both the baker and runtime include inactive
ancestors when finding the canvas scale: the 720-wide game canvas must bake it
at 240 units, not 360. The previous bake incorrectly used scale 1, making its
473-unit strip and portrait opening 50% taller than intended. Animated portraits
now cover their masked opening with an `AspectRatioFitter` in `EnvelopeParent`
mode. One or two targets stay stacked; three or four use two columns to retain
the same badge and row height on the shorter bar.
The HUD supports one to four targets, score-only levels, and timed levels.
Shared objectives show remaining counts; score-only levels show have/need.
The baker preserves the kit's TargetsGroup across repeated bakes so collection
animations continue to land on the visible target icons.

## Compact frame edit prompt (built-in imagegen)

Use case: precise-object-edit. Asset type: production Unity mobile match-3 HUD frame sprite. Edit target: the provided blue-and-copper HUD background frame. Primary request: make this SAME frame much shorter and remove the decorative crown above the central circle. Keep the existing polished 3D cartoon art style, deep royal-blue side panels, copper bevels, dark walnut lower edge, thin orange-gold header dividers, gold circular portrait rim and warm cream sunburst. NO redesign. Output a wide 1920 x 640 landscape image. The whole frame silhouette should span x35..1885 and y35..610. The central medallion is a perfectly ROUND circle centred at (960,320), outer radius 282, inner radius 244. Remove the crown completely and continue the smooth round gold rim through the top where it was. The side panels now span x60..748 and x1172..1860, y80..565, with their inner edges concealed behind the circle as in input. Header divider y206; leave EMPTY blue space below the divider for dynamic counters. Shorten unused vertical space while keeping side panel widths generous. Actual alpha transparency outside the silhouette, no white/gray/checkerboard pixels or background. The portrait opening remains empty cream sunburst, no king: a live king animation will fill it in the game. No text, numbers, target icons, jewels, extra ornaments, or crown anywhere on the frame.

## Original generation prompt

Use case: precise-object-edit. Asset type: transparent production sprite for a Unity match-3 game's HUD. Input images: the FIRST image (user upload, TARGETS 100 / 4 and MOVES 6, brown-haired king in simple gold medallion, dark copper borders) is the edit target. Other two images are old HUD examples to IGNORE completely. Recreate the FIRST uploaded HUD as a clean isolated transparent UI background plate, preserving its proportions, sculpted shapes, copper/bronze bevels, dark walnut bottom edge, deep royal/navy-blue inset panels, thin orange-gold horizontal header rules, and very large central smooth gold oval medallion topped with a simple three-point crown. The HUD fills a 1536x672 landscape canvas with 2% clear safety margin on all sides. The broad oval medallion takes 36% of total width, centered at x50%, y54%, with simple smooth polished gold concentric rims, pale warm cream sunburst inside. Left/right blue panels extend x5..39% and x61..95%, y18..88%, with upper inner corners and lower inner corners tapering behind the medallion just like original. Preserve the original high quality softly shaded 3D cartoon mobile game look. EDITS: remove ALL text (TARGETS, MOVES), ALL digits, ALL target icons, ALL cream counter pills and the cream moves card, and remove the human king entirely, revealing uninterrupted pale cream sunburst behind him. Leave both side blue panels completely empty below their header rules so dynamic counters can be added in Unity. Remove puzzle/stone room background completely; outside the silhouette is actual alpha transparency, not black, not white, not a checkerboard pattern. No letters or numbers anywhere, no jewels or filigree on frame, no extra elements. Crisp clean silhouettes, exact original dark copper rather than bright yellow wood, warm soft highlights. This is only the static frame art, with blank areas ready for live UI.
