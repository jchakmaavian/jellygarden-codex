# Royal gameplay implementation

The approved mockup is implemented through the existing story gameplay renderer. The HUD displays live target, move/time and inventory values. Its controls call the existing booster and pause actions. The subsequent HUD revision removes the level label and crown badge and places pause at the top center.

- Blue enamel and gold HUD panels, a centered pause button, ivory moves counter and four-button booster tray.
- Recessed slate-blue cells and a rounded gold mesh border following each level's actual shape, including holes and disconnected sections.
- Compact ingredient exit markers leave the pause control and booster tray clear.
- Six replacement pieces in the existing color order: red book, amber coin, purple gem, blue shield, green leaf and gold crown.
- Replacement rocket, TNT and rainbow orb sprites, plus glove, hammer and bomb booster icons. Existing rocket/TNT effect players remain connected.
- The level-start popup and pre-level power-up preview share the board's rocket, TNT and color-bomb artwork; the popup no longer overrides them with crops from the old sheet.
- Sunny castle courtyard background and responsive phone/tablet layout.

The game retains its real level layouts and booster rules. The last bomb-shaped booster retains the existing recolour action. Ingredient, blocker and score icons continue using their existing story assets.

## Art provenance

Thirteen assets were generated with the built-in image-generation tool, each referencing the approved `gameplay-mockup.png`. [generation.json](generation.json) records every exact prompt, tool mode, source file and import setting. Original generated PNGs are copied unchanged into `Assets/JellyGarden/Resources/Story/RoyalBoard`; no offline image editing or cropping is applied. Unity imports transparent icons as single sprites at 512 pixels and the background at 2048 pixels. Pixels-per-unit sizing normalizes the opaque icon silhouettes for the existing board scale.

HUD panel surfaces and the board contour are procedural Unity graphics so their borders remain clean across sizes.

## Review

Validated in the open Unity editor on 2026-09-19:

- C# typecheck build passed.
- [26 live UI checks passed](result.txt) across score, collect and ingredient levels, including booster selection/cancellation, +5 consumption and pause.
- [87 rocket/TNT checks passed](vfx-result.txt), including both rocket axes, edge hits, chains, pause, pool cleanup and refill.
- VFX integrity and window-launch guards passed. All thirteen assets have valid sprite imports; twelve icons have real transparency.

In Play Mode, send `royal-board` to `Temp/rocket-validation-command.txt` to run the explicit editor review. It checks live target/piece mappings, button callbacks and viewport sizes and writes native Game-view captures here. Review inventory preferences are restored on completion or failure. `validate` runs the existing rocket/TNT integration checks.

The saved mockup is a design reference; PNG files in this directory are actual Unity captures.

- [Gameplay with power-ups, 393×852](powerups-393x852.png)
- [Compact phone, 320×568](powerups-320x568.png)
- [Tablet, 768×1024](powerups-768x1024.png)
- [Ingredient level](level-12-393x852.png)
- [Updated level-start popup](level-start-7-393x852.png)
- [Level-start popup with collectible-piece goals](level-start-17-393x852.png)
