# Connected board border and tile spacing

The gold outline now leaves a gap of 4.5% of a cell pitch around the blue tiles (about 1.5–2 pixels on a phone). Straight segments share their endpoints, rounded outer corners meet both bars, and inward corners join at the perpendicular stroke. The same rules cover holes and stepped silhouettes.

`StoryPixelSnap` reserves room for the outline outside the grid, keeping wide boards inside the screen. Board art, item scale, game rules, animation clips, and particle prefabs are unchanged.

Validation:

- Typecheck: zero errors.
- Native Unity build: zero errors (381 existing project warnings).
- Unity border tests: 2,860 samples across straight seams, outer corners, inward turns, holes, narrow necks, diagonal contacts, tile clearance, and repeated builds.
- Animation/VFX and window-launch guards pass.
- Nine native previews cover levels 3 (the reported silhouette) and 12 (a wide board) at 393 × 852, 329 × 646, 320 × 568, and 768 × 1024; level 149 (a narrow board with holes) at 393 × 852. All outline bounds stay inside the screen.

`board-preview.png` is the board cropped from the native 329 × 646 capture. Full Unity screenshots are alongside it. The preview runs in a separate save profile in an isolated project copy.
