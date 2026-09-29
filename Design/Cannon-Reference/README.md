# Cannon reference integration

The column tool now rises below its target, idles with a lit fuse, plays the recovered skeletal firing/recoil animation, and sends the recovered cannonball and flame trail up the column. Damage advances with the ball. The existing cell damage, blocker, refill, inventory and move-count rules remain authoritative.

Source files inspected:

- Video: `G:/Unity Projects/Match3-Games_ArtSource/Match-3-JC/00_References/animation-VFX/Cannon.mp4` (12.98 seconds). The firing section is around 6.6–7.9 seconds.
- APK: `Design/Backup/apk/royal_thron_rfv387.apk`.
- Recovered Spine 3.8.84 skeleton: path ID 1715; atlas 14089; texture 885. Native clips: `gercek_gelis_loop` (0.333 s), `gercek_patlama` (0.9 s). The installed Spine 3.8 runtime plays the original bones, attachments and curves.
- Native VFX hierarchies: source IDs 41043 (muzzle), 38903 (fuse), 56812 (ball/trail). Source particle modules are imported through the existing native VFX importer. The 388000000 ID offset prevents collisions with the earlier APK's assets.
- Native audio: `cannon_move`, `cannon_use`, `cannon_explode`. The existing audio mixer and sound preference control playback.

`extract_cannon.py` extracts the source dependencies. `ReferenceCannonImporter` creates persistent Spine and particle assets; its editor menu is **Royal Throne → Reference Gameplay → Import Cannon Animation**. Runtime assets live in `Assets/ReferenceGameplay/Tools/Cannon` and the three `Cannon*Rfv387` prefabs in the existing VFX Resources folder.

`ReferenceCannonSequence` owns entrance placement, source animation playback, projectile travel, pause, and cleanup. `ReferenceBoosterBar` calls it only for the cannon. Entrance is 0.32 s, aim is 0.28 s, launch occurs 0.20 s into the source firing clip, and column travel lasts 0.42 s. The cannon uses the source prefab's 0.8 scale. Each crossed cell receives at most one hit, including when a frame crosses multiple rows.

The reference's bottom-bar choreography is included: the complete tool/settings bar slides below the physical screen bottom in 0.22 s as the cannon rises. It stays hidden through firing/recoil, then slides back in 0.24 s once the source firing clip ends (1.50 s into the sequence). Its original position and input state are restored on completion or cancellation; pause freezes the slide with the cannon. The off-screen distance includes the safe-area inset. `nav-reference.jpg` records reference frames from 6.0 to 8.2 seconds for this comparison.

Review artifacts are in `Review/`: `Cannon-Gameplay-v2.mp4` is the corrected silent capture of the actual Unity Game view, `Cannon-Gameplay-v2-sheet.jpg` shows successive phases including the bar's return, and `checks.txt` records the Play Mode assertions. The earlier unversioned preview predates the bottom-bar correction. The frame capture uses a fixed simulated timestep; it is visual evidence, not a device performance benchmark. No Android APK was rebuilt for this change.

All assertions in `Review/checks.txt` passed: center/left/right columns, exactly eight hits, stock and move accounting, refill ownership, pause, cancellation, cleanup, and normal-profile restoration. The synthetic 8×8 QA board explicitly clears the reference tutorial feed and uses a five-color palette after setup. This prevents the tutorial's fixed refill stamps from leaking into the synthetic board. Earlier fixture attempts are not counted as passing runs.

The existing `Assets/Spine` runtime (including its documented Unity 6 compatibility patches) is a required dependency. It is included in this change's backup because it was present locally but not yet versioned. No runtime upgrade was performed.
