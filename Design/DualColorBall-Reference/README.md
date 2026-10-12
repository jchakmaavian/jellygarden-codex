# Royal Throne dual Colorball reference import

Source APK: G:\Game\old Backup\JellyGarden-Codex-20261005\Design\Backup\apk\royal_thron_rfv387.apk. See source-inventory.json for the APK and data-pack hashes.

The old dual combo selected the full board but fell through to a generic sparkle/Boosting effect, waited 0.34 seconds, then hit cells in row order. It never used the dedicated native two-ball clip.

The dual branch now plays the recovered BallBallExplodeAnimation (2.883333 seconds at its authored 60 FPS), its charging glow/energy particles, the dedicated explosion ring/flash/splash, and the exact native lightball_lightball_combo sound. Every existing selected cell receives its existing Hit callback on the blast frame. The move, reward, blocker, ingredient and target-selection owners remain the existing game code. Ordinary Colorball and Colorball-to-booster paths keep their current presentation.

The 60 decoded animation curves exactly match the already imported clip after reference IDs are normalized. Nine of ten referenced body sprites are pixel-identical. The one differing lower blurred frame is recovered from this APK and used by a new combo-only clip, controller and view. The original clip, view, body art and shared additive material remain unchanged. Eight sprites in total (seven particle sprites plus that blurred body frame) are recovered. Non-power-of-two dimensions are preserved.

Two VFX prefabs contain eight native particle systems. All 2,103 compared active module values match the actual Unity 6 import, with zero differences. Native sprite, material, shader and audio descriptor references were verified by the importer.

Validation: runtime and editor assemblies compile using Unity 6000.5.10f1. The isolated fixture inside this checkout rendered the actual imported animation and VFX and passed 22 lifecycle checks at 15/30/60 FPS, including pause/resume, mute, reuse, cancellation and Home cleanup. Review frames show the native orbit/charge, final merge, flash and expanding rainbow ring. The MP4 uses the captured Unity fixture frames with the exact native cue added as a separate review soundtrack; it is not a recorded gameplay audio mix. Fixture board and mixer services are lightweight stubs; this does not establish full campaign gameplay parity. The focused real-board QA entry point is Tools > Royal Throne > QA > Verify native dual Colorball.

Native animation, sprites, VFX modules and sound are recovered. C# lifetime and the blast callback are reconstructed: the APK contains no animation events for this clip, and its compiled gameplay scheduling was not recovered. The source prefab also has a custom BlackBg mesh-mask component outside the recovered animation hierarchy; its dynamic dimming/mask behavior is not imported here. No claim of exact custom-mask or compiled scheduling parity is made.

The existing Unity Play Mode session was left running. Real-board QA and an Android/device comparison remain pending editor availability. No reference APK, compiled game code, services or identifiers are uploaded by this change.
