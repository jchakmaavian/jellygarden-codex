# TNT sprite rollback and explosion color update

Restored the original imported TNT sprite for creation, idle and activation. The rejected mesh reveal is no longer connected to board items; its pooled explosion renderer remains in use with the model hidden.

Only explosion color progression changed: brief white-hot center, saturated yellow core with gold outer glow/rays, then the existing warm-white smoke/sparkle fade. Shapes, sizes, opacity, timing, damage, fragments and pooling remain unchanged.

Verified in Unity 6000.5.10f1: compilation and seven focused Play Mode checks at 0.5 speed passed. Captures show the restored sprite and early expanded blast. The first cleanup assertion was premature; extending the test wait to include the final fragments resolved it without changing runtime effects. No runtime errors during the successful test; coins and lives preserved.

Rocket sequence, Rocket fire shader and EffectsProfile hashes match the pre-TNT baseline. Original J: project untouched.

verified-source contains exact changed scripts and their existing metadata. incremental.patch records this task's runtime changes against the pre-task working files, separate from earlier uncommitted project changes.
