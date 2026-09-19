# Rocket and TNT ZIP integration

`RoyalRocketVFX.zip` and `RoyalTNTVFX.zip` supply the gameplay effects. Their scripts, shaders, materials and textures are imported under `Assets/RoyalRocketVFX` and `Assets/RoyalTNTVFX`.

- Rockets use the ZIP's four directional sprites, gold flame ribbons, white smoke, embers and contact stars. Default timing is 0.12 seconds of anticipation followed by 15 cells per second. Both heads use their own distance to the board edge, then continue off screen.
- Package bombs use the ZIP's TNT ignition sprite, expanding blast, shockwave and star debris. Ignition lasts 0.12 seconds. Damage remains the game's existing 3×3 area.
- `StoryRoyalVfx` installs both players from Resources and prewarms their pools. It handles board scale, camera depth, pause, scene changes and same-scene level resets.
- Tiles are reserved immediately and clear when the rocket nose reaches them. Timing comes from the loaded ZIP player's tuning; the integration check compares clears with its actual `CellReached` events.
- The default rocket style (`RoyalStreak`, index 5) uses the ZIP. The older lab styles remain selectable. Color-bomb effects retain their existing behavior.

The rocket player prefab was moved to `Assets/RoyalRocketVFX/Resources/RoyalVfx/RocketVFX_Horizontal.prefab` with its original GUID. Its saved runtime pool was removed because the package rebuilds that pool in `Awake`; capacity is 32 heads for chained activations. Both axes use this one player. The TNT player is at `Assets/RoyalTNTVFX/Resources/RoyalVfx/TNTExplosionVFX.prefab`.

Run **JellyGarden → Story → Preview → Validate Rocket Animation** in Unity Play Mode to exercise both packages. Captures and the check result are written to `Temp/royal-vfx-validation`.

## Verification

Unity 6000.5.10f1: **87 live checks passed** for package loading, shader support, actual contact timing, both rocket axes, board-edge launches, pause/resume, rocket chains, rockets triggering TNT, bomb taps, refill, pool reuse and level-reset cleanup. The procedural rocket fallback did not run. C# typecheck passed with zero errors (existing project warnings remain). Asset-preservation and launch-size checks passed; package GUIDs and prefab references are intact.

Captured from the 393×852 Game view:

- [Horizontal rocket](rocket-horizontal.png)
- [Vertical rocket](rocket-vertical.png)
- [TNT blast](tnt-blast.png)
- [TNT debris](tnt-debris.png)
- [Validation result](validation.txt)
