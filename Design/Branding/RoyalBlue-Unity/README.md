# Royal-blue splash integration — 13 September 2026

The user approved the v2 study for integration with “add this in main game in unity.” The saved `main.unity` scene in JellyGarden-Codex now uses the exact approved blue castle and crowned Play PNGs, the locked Royal Throne logo, the original six-second rotating light-wheel appearance, and a blue loading card with animated jewels and progress glint.

The original project at `J:\Unity Projects\Match3-Games` was not edited or run for this integration. The map, board, gameplay rules, save identity, packages, renderer and platform-export settings were not changed.

## Actual Unity captures

These are unedited native Unity Editor window captures from Play Mode in Unity 6000.5.10f1. They include Editor chrome; the named dimensions identify the Game view resolution, not the full screenshot dimensions.

| View | Splash | Loading motion preview |
| --- | --- | --- |
| 720 × 1280 | [Splash](splash-720x1280.jpg) | [Loading](loading-preview-720x1280.jpg) |
| 720 × 1600 | [Splash](splash-720x1600.jpg) | [Loading](loading-preview-720x1600.jpg) |
| 768 × 1024 | [Splash](splash-768x1024.jpg) | [Loading](loading-preview-768x1024.jpg) |

The loading motion captures use the existing F7 Editor-only fifteen-second preview, explicitly labelled “Animation preview.” Separate observed frames showed halo rotation, jewel motion and fill/glint advancement; the preview returned to Play. They do not measure actual loading time.

[Real loading](loading-real-720x1280.jpg) captures the actual Play transition with “Preparing your game...” before scene activation. A rapid double-click reached the existing map at 720 × 1280, and a normal click reached it at 768 × 1024. [Map Settings](map-input-after-loading.jpg) confirms the map accepts input after the overlay fades away. The final [Console](console-after-real-loading.jpg) showed 0 errors and 7 inherited/integration warnings.

The full logo, Play button and loading text fit all three inspected portrait sizes. The castle crops to cover the viewport without stretching. No physical-device cutout, mobile export, missing-scene fault injection or full gameplay regression test was performed.

See [splash and loading design](../SPLASH-AND-LOADING.md) for runtime paths, exact asset hashes, editing and rebuilding instructions, and the preserved asynchronous loading behavior.
