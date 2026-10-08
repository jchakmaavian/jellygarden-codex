# Royal Throne: all dual booster animation/VFX audit

Source: G:\Game\old Backup\JellyGarden-Codex-20261005\Design\Backup\apk\royal_thron_rfv387.apk. The exact package is read directly; the older extracted media pack is only a comparison baseline. Provenance hashes are in ../DualColorBall-Reference/source-inventory.json.

Coverage includes Rocket+Rocket, TNT+TNT, Propeller+Propeller, Colorball+Colorball, Rocket+TNT, Propeller+Rocket, Propeller+TNT, Colorball+Rocket, Colorball+TNT, Colorball+Propeller. Horizontal/vertical rocket variants yield 15 test cases. The live suite runs those cases at 30, 15 and 60 FPS, then cancels each pairing mid-activation in an isolated player profile. Full-game QA runs in a separate Unity 6000.5.10f1 project copied inside this checkout because a concurrent music task uses the main editor.

## Repairs

- TNT+TNT requested a nonexistent resource spelling. It now uses the dedicated TNTTNTComboRfv387 view and the authored 1.583333-second clip, native fuse, blast and exact combo sound.
- Rocket+TNT did not use either dedicated combo particle hierarchy. Its existing launch anticipation now owns TntRocketComboUseParticles, and a zero-delay profile waits for the source emitter to actually produce a visible frame, followed by TntRocketComboParticles and the existing three-row/three-column rocket damage. Presentation adds no extra move debit.
- Propeller+Rocket incorrectly played its explosion at takeoff and followed the flying payload. The dedicated burst now plays at target impact.
- Propeller+TNT uses the exact native combo cue. The existing pair orbit, target selection and damage remain reconstructed code.
- Thirty-two exact cropped sprites are installed under an independent ID range. Both dedicated combo views and Colorball sprite-switch tracks use them. Shared particle artwork whose alpha differed from the APK now points to the exact sprite. Existing single-booster body assets and their clips stay intact.
- Legacy artwork names contain an appended source ID. The earlier dual Colorball importer failed to match that suffix, leaving its different blurred lower frame unused. Both importers now match the native name correctly. Native Sprite assets use Unity persistent references, including the correct asset reference type.
- Repeated particle child names are matched by parent and sibling ordinal during source audit and repair. Transform.Find alone selected the wrong sibling in the Propeller takeoff audit.

## Evidence

- Both dedicated clips: 147 curves and 9,372 keyframe/sprite checks. Zero unexpected differences. Forty-two documented differences are Unity's step tangents for discrete enabled/sorting-order properties; source key times and integer values remain unchanged.
- Eight dedicated combo VFX prefabs: 40 particle systems, 10,175 active module checks, zero differences.
- Eight supporting VFX prefabs used by converted boosters: 53 particle systems and two beam renderers, 13,842 checks, zero unexpected differences. Ten documented differences are obsolete lightmap fields and sorting-layer remapping in Unity 6.
- Thirty-two newly recovered sprite PNGs match the source decoded RGBA pixels exactly. All 101 particle sprite slots resolve as expected, including three intentionally empty native slots on inactive emitters. Shader/material references are supported.
- Both runtime and editor source compile with Unity 6000.5.10f1 references. Existing unrelated serialization/obsolete API warnings remain. The fresh fixture also raises a UnityEditor.Search startup exception; that exact editor stack is documented separately and excluded from gameplay-error checks.
- All 45 live pairing/orientation/FPS runs and 15 cancellation cases passed. Normal player coins, lives and progress were restored; no gameplay runtime errors were observed.
- Verification/checks.txt and the review media record the live full-game results; exported files alone are not gameplay verification. Batch capture renders the real game camera and excludes Screen Space Overlay UI. The review montage has no captured gameplay soundtrack.

## Remaining accuracy limits

These checks establish native asset data fidelity and playable presentation coverage. They do not establish frame-for-frame equivalence to every source combo in native APK playback. The APK's compiled motion owners, exact interrupt/anticipation timing, camera shake and hit scheduling were not recovered. The Colorball source wrapper's custom BlackBg mesh-mask/dimming behavior is not implemented. Propeller pair trajectories, Colorball beam pacing and Rocket+TNT preparation retain the existing reconstructed runtime. No universal timing/mask parity claim is made. Existing backup reference recordings were located and the recorded TNT/Propeller convergence evidence was reviewed, but they do not show every pairing. Android/device comparison remains pending.

No reference APK or compiled game code is included in the backup commit. Player progression, approved branding, music and the original Unity project remain outside these changes.
