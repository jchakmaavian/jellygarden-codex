# Reference celebration card - 8 October 2026

Applied to the user-confirmed active project, G:/Game/JellyGarden-Codex. The older C: checkout was not modified by this implementation.

The supplied image guides the final composition: larger joined Well Done! artwork with a stronger arch, a royal-blue backdrop hiding the board/HUD, a slightly shorter card, a lowered star/cushion group with clear title spacing, and a wider, brighter emerald Continue button with a warm gloss. The level number remains live; the preview uses QA Level 23.

Celebration VFX: 18 feathered warm gold rays sit behind the star, slowly rotate and breathe; the existing star and braid sparkles remain. A new RoyalWinConfetti graphic draws four curled ribbons (two blue, two gold), fluttering in bounded side lanes. A short 18-piece burst starts after the star impact and expires, leaving the four ribbon curls. All ribbon geometry uses one UI renderer, no generated textures, no per-frame object creation and no input interception. Motion off uses a short fade into a static title, rays and four ribbons.

Runtime changes: RoyalCards, RoyalWinCard, RoyalWinTitleLetters, RoyalWinRaysGraphic, and new RoyalWinConfetti plus its Unity meta. Existing artwork is reused. No reward, save, audio or gameplay code was changed.

Validation: Unity 6000.5.10f1 compiled successfully. verify_celebration.cs passed 53 focused checks with zero failures across 1080x1920, 720x1280 and 1536x2048. Checks cover actual title meshes/crown clearance, ribbon bounds across the full burst, static reduced motion, layer order, hierarchy reuse, Continue/close routing, and preserved normal saves/runtime resources/Home catalog. No runtime errors were recorded. QA ended and Unity was left in clean Edit Mode, main.unity. Other ongoing project changes were excluded from this commit.

Royal-Throne-Celebration-v12.mp4 is a five-second actual Unity capture, silent, resampled to 30 fps using its recorded wall-time cadence. It passed full decode validation. Native stills and the QA log are in Verification. This is a reconstructed in-game match to the supplied image, not a pixel-identical render. Physical-device/Android performance was not tested.