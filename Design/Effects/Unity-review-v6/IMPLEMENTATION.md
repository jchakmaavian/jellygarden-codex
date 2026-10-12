# Restored match presentation, 15 September 2026

The user requested rollback of the preceding animation/VFX revision and a smoother presentation. This restores the sprite-led match pop, one AllIn1 Ice accent per group, and the original authored fragment counts. The replaced per-cell emitter has no runtime hook or automatic prewarm; its source and v5 evidence remain for historical comparison.

Confirmed ordinary matches use 30 ms anticipation, 45 ms squash, 45 ms rebound, and 90 ms cleanup. SmoothStep easing and reduced deformation soften the pop. Group Ice is timed at 120 ms. Fragments retain the established pool and artwork, with 55% launch/spin speed, gravity of nine cell-footprints/s?, a 780 ms tail, and a smooth fade/shrink after 500 ms. TNT and other power clears retain their prior behavior.

No scene, prefab, serialized configuration, match rules, navigation, economy, or level data was changed in this revision.

Validation: runtime C# compilation passes with existing project warnings. Unity Play Mode verification pending; no v5 test result should be interpreted as verification of this revision.
