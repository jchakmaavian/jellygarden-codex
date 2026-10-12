# Gameplay presentation integration

This revision changes gameplay presentation in the existing JellyGarden-Codex Unity project. Level files, matching rules, navigation, scenes, the approved home screen and splash artwork are outside its scope.

## Supplied art

The complete source pack was inventoried and visually reviewed before integration: 77 gameplay sprites, two previews, README and manifest. It contains no prefabs, materials or animation clips. The source directory was read only. The lossless import and individual asset records are in [README.md](README.md) and [import-manifest.json](import-manifest.json).

The gameplay catalog maps existing color and power identifiers to the supplied blocks, shields, crowns, leaves, crystals, vases, rockets, barrel, propeller and color ball. Existing item objects and colliders remain in place. Visible pieces fit 94% of a cell; a dedicated vertical rocket asset stays upright. Gameplay goal icons preserve their target identifiers. Home and pre-level UI continue using the established catalog.

The board uses restrained supplied cell textures, a gold silhouette following occupied cells and a small depth shadow. Frame geometry is cached in five render layers. Returning to a level recreates destroyed decorative renderers while retaining their cached meshes and materials.

## Impacts and falling pieces

The installed AllIn1VfxToolkit supplies Ice Impact and Fire Impact. Derived prefabs and materials live under `Resources/RoyalGameplay/Toolkit`; vendor files are not edited. Demo self-destruction, camera scripts and realtime lights are removed from the derived versions. Their materials do not rely on soft-particle camera depth, which is unsuitable for this orthographic board.

One Ice Impact plays at the center of each confirmed ordinary match group. TNT uses Fire Impact at its impact beat after the existing anticipation. Decorative tails run independently of matching and input ownership. Four Ice and two Fire instances are prewarmed, with 48 particle systems and a total capacity of 176 particles. A full pool skips another decorative burst instead of allocating or cutting an existing tail short. Pause freezes particle simulation and lifetime; leaving gameplay clears the pool.

The five supplied burst atlases provide 28 independently sliced chips: red 7, blue 4, gold 4, leaf 8 and magenta 5. These reuse the existing 240-fragment pool, launch outward, rotate and fall under gravity. Their visible opacity remains intact during the fall. Materials without authored chips retain the existing physical fracture fallback.

## Matching hints

The old hint animation changed sprite scale that the piece skin subsequently replaced. The repaired presenter applies its subtle pair pose after skin and Animator updates without moving item roots or colliders. A pure board snapshot finds a valid exchange and the complete future matched shape. A closed, rounded gold aura follows only the shape's outside boundary; its brighter flowing highlight does not draw internal cell edges. The outlined cells can form straight matches, L, T or a valid square.

Primary mouse/touch input, Escape and the Hints setting cancel the presentation immediately. Pause, a resolving board and changed occupants invalidate it. Reduced motion keeps a steady outline. Contours are pooled and reuse their renderers and geometry buffers.

## Verification entry points

- `Tools > Royal Throne > Gameplay > Import and verify gameplay art v4` validates all imported texture resources and sprite views.
- `Shift+F12` opens the current level from Home without spending a life.
- `Shift+F10` runs the disposable gameplay regression board, restores the original board and records results under `Design/Effects/Unity-review-v4`.
- `Ctrl+Shift+F9` uses the existing VFX Lab's production Fire path at Game Speed 0.5 and captures its full tail under `Library/RoyalToolkitLabCapture`, then restores the previous Lab settings and speed.

Final Play Mode results and visual evidence are recorded with the v4 review, separately from asset preparation checks. Editor captures are not a physical-phone performance benchmark.

## Completed Play Mode review — 2026-09-15

The final disposable regression passed 495 checks with an empty runtime-error report. Mouse and touch swaps, enlarged edge hit areas, invalid swaps, all match sizes, power combinations, cascades, full-shape hint outlines and natural idle hint cancellation passed. Pool-cap and original coin/life preservation checks passed. See [checks.txt](../Effects/Unity-review-v4/checks.txt).

The captured gameplay video contains 578 frames over 56.72 seconds with Unity audio: [gameplay review](../Effects/Unity-review-v4/royal-throne-unity-effects.mp4). The existing VFX Lab's integrated Fire path was separately reviewed at speed 0.5 over 3.6 seconds, including its bright onset, outward streaks and completed tail: [contact sheet](../Effects/Unity-review-v4/fire-half-speed-contact.jpg), [capture metadata](../Effects/Unity-review-v4/fire-half-speed.json). Paired hint captures in the review's `raw` folder show complete 3/4/5, L, T and square contours plus natural idle/input cancellation.
