# Cannon reference integration

The column tool now rises below its target, idles with a lit fuse, plays the recovered skeletal firing/recoil animation, and sends the recovered cannonball and flame trail up the column. Damage advances with the ball. The existing cell damage, blocker, refill, inventory and move-count rules remain authoritative.

Source files inspected:

- Video: `G:/Unity Projects/Match3-Games_ArtSource/Match-3-JC/00_References/animation-VFX/Cannon.mp4` (12.98 seconds). The firing section is around 6.6–7.9 seconds.
- APK: `Design/Backup/apk/royal_thron_rfv387.apk`.
- Recovered Spine 3.8.84 skeleton: path ID 1715; atlas 14089; texture 885. Native clips: `gercek_gelis_loop` (0.333 s), `gercek_patlama` (0.9 s). The installed Spine 3.8 runtime plays the original bones, attachments and curves.
- Native VFX hierarchies: source IDs 41043 (muzzle), 38903 (fuse), 56812 (ball/trail). Source particle modules are imported through the existing native VFX importer. The 388000000 ID offset prevents collisions with the earlier APK's assets.
- Native audio: `cannon_move`, `cannon_use`, `cannon_explode`. The existing audio mixer and sound preference control playback.

`extract_cannon.py` extracts the source dependencies. `ReferenceCannonImporter` creates persistent Spine and particle assets; its editor menu is **Royal Throne → Reference Gameplay → Import Cannon Animation**. Runtime assets live in `Assets/ReferenceGameplay/Tools/Cannon` and the three `Cannon*Rfv387` prefabs in the existing VFX Resources folder.

`ReferenceCannonSequence` owns entrance placement, source animation playback, projectile travel, pause, and cleanup. `ReferenceBoosterBar` calls it only for the cannon. Entrance eases in and out over 0.50 s; the native firing clip starts at 0.40 s, overlapping the last part of the rise. It blends from the idle clip over 0.05 s. Launch occurs at the firing clip's 0.60 s extension/discharge pose (1.00 s into the sequence). Source pose renders and reference video crops (`source-poses.jpg`, `fire-cue.jpg`, `source-keyframes.txt`) were used to choose that cue. The previous 0.20 s launch happened during anticipation.

The ball and muzzle flash start at the animated rim's aperture. Animation updates split exactly at the firing cue even if a frame crosses it. Projectile travel lasts 0.38 s, using the visually fitted accelerating distance curve `0.3u + 0.7u*u`, rather than the old constant speed. VFX hierarchies are instantiated before timed motion, avoiding Instantiate/Resources.Load at launch. Cannon hits reuse the existing immediate projectile shatter presentation so the target does not play a delayed match anticipation after the ball passes. The source prefab's 0.8 scale is retained. Each crossed cell receives at most one hit, including when a frame crosses multiple rows.

The reference's bottom-bar choreography is included: the complete tool/settings bar slides below the physical screen bottom in 0.22 s as the cannon rises. It stays hidden through firing/recoil, then slides back in 0.24 s once the source firing clip ends (1.30 s into the sequence). Its original position and input state are restored on completion or cancellation; pause freezes the slide with the cannon. The off-screen distance includes the safe-area inset. `nav-reference.jpg` records reference frames from 6.0 to 8.2 seconds for this comparison.

Review artifacts are in `Review/`: `Cannon-Gameplay-v3.mp4` is the revised silent capture of the actual Unity Game view at 60 FPS, `Cannon-Gameplay-v3-sheet.jpg` shows successive phases, and `checks.txt` records the Play Mode assertions. V2 is the earlier bar correction; the unversioned preview predates both corrections. The frame capture uses a fixed simulated timestep; it is visual evidence, not a device performance benchmark. No Android APK was rebuilt for this change.

`Cannon-Reference-vs-v3.mp4` places the reference on the left and the new Unity capture on the right, both at normal speed. The reference starts at 6.30 s to approximately align the discharge around 1.00 s. Their original aspect ratios are preserved, so the shorter Unity view has padding below it. `compare_cannon.py` reproduces this comparison. The Play Mode checks exercise the source firing cue at 60/30/15 FPS, including frames that cross the cue and multiple cells; results are in `Review/timing.txt`.

All assertions in `Review/checks.txt` passed: center/left/right columns, exactly eight hits, stock and move accounting, refill ownership, pause, cancellation, cleanup, and normal-profile restoration. The synthetic 8×8 QA board explicitly clears the reference tutorial feed and uses a five-color palette after setup. This prevents the tutorial's fixed refill stamps from leaking into the synthetic board. Earlier fixture attempts are not counted as passing runs.

The existing `Assets/Spine` runtime (including its documented Unity 6 compatibility patches) is a required dependency. It is included in this change's backup because it was present locally but not yet versioned. No runtime upgrade was performed.
