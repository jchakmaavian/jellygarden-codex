# King Vanguard HUD and activity correction

The top-middle King appeared as a floating rectangular cutout inside the arch. Normal matches did not reach the old reaction threshold, and imported power-up reveals returned before the old visual-effect celebration callback. This correction addresses those visible and behavioral problems while retaining Vanguard's redesigned facial identity and the imported animation performance.

## APK comparison and UI placement

The exact `royal_thron_rfv387.apk` import was inspected again. Its portrait uses a roughly 368-source-unit inner aperture; the previous 428-unit fit included the removed outer frame. The original cape ends were concealed by that frame. In the custom HUD they instead left visible gaps of about 15–20 pixels at the sides and 30 pixels below at 1080×1920.

- [Untouched APK portrait and original frame](Before/APK-original-portrait.png)
- [Before: live HUD](Before/Live-05.png)
- [After: live HUD](After/Gameplay-HUD.png)
- [After: in-game character motion](After/King-Vanguard-HUD-motion.mp4)

`KingVanguardCharacter.Framing.GameplayPortrait` uses `Rect(-184, 38, 368, 388)`, uniform scale, and bottom anchoring without the general portrait's extra top margin. The cape extends behind the UI ring; the existing arch mask contains it. The original hand-only `frame_clipping` remains intact. Goal/Moves placement and the game's blue-and-gold arch design are preserved. Other portrait slots keep their existing framing.

## Facial motion

The 1.55× pupil enlargement exceeded the eye-white bounds during source glances. The portrait/area pupil scale is now 1.1×, preserving the original gaze motion. The mouth-interior painting previously included an extra tongue beneath the separately animated tongue. Built-in image generation corrected only that mouth cell; technical packing applied only the mouth UV island in the portrait and area textures.

[Mouth correction prompt](FaceRedesign/mouth-repair-prompt.txt), [painted workboard](FaceRedesign/Portrait-mouth-repaired-workboard.png), and [pixel-preservation proof](FaceRedesign/mouth-only-validation.json). All non-mouth atlas pixels remain identical; no original facial RGB was restored. The native full-body and bedtime art are outside this HUD-specific correction.

## Activity behavior

- Committed matches of at least three pieces trigger the imported 0.7-second `k-1_sevinme_3` acknowledgment, even with visual effects disabled.
- Committed power-up formation emits a semantic event before native visual code can return. Strong feedback plays the original happy clip.
- One pending response coalesces bursts, favors stronger feedback, and waits for animation acceptance. Match spacing is 2 seconds; power-up spacing is 4 seconds. Pending feedback expires after 2 or 3 seconds respectively.
- Hiding the HUD, changing board attempts, and priority outcomes clear pending feedback. Rejected swaps do not celebrate. Worry, extra moves, goal completion, and result priorities remain in place.

## Verification

[Controller and queue checks](Validation/controller-checks.txt) exercise the actual Spine runtime, including 30 seconds of idle cycling and reaction/worry handoffs. [Live feedback checks](Validation/live-feedback-checks.txt) use real swap and match resolution on isolated fixtures; direct overlap/visibility probes are explicitly labelled. The [transition trace](Validation/live-feedback-transitions.csv) records each observed clip change.

All controller checks and live feedback checks passed. The [priority activity regression](Validation/live-activity-checks.txt) also passed: low-move worry, the actual extra-move grant, goal relief, and the win result. The [final state check](Validation/final-state.txt) confirms that the normal player profile was restored after isolated QA.

The HUD recording is a direct presentation playback review on the actual in-game UI, separate from the live gameplay-trigger checks. Its [timestamp trace](After/hud-motion-trace.csv) preserves the measured capture intervals. The final H.264 MP4 decoded without errors. This is Unity editor verification, not an Android device test. One Unity internal GUID assertion was logged at play startup before the gameplay checks; no additional error or assertion appeared during the checks.

[Eye-motion measurements](Validation/PUPIL-CONTAINMENT.md) and [aperture measurements](Validation/hud-aperture-framing-evidence.json) document the reference comparison. [Installed atlas hashes](Validation/installed-atlases.json) confirm the mouth-only output is present in the game assets.

Source APK compiled method bodies remain unrecovered; this continues to use local game-to-character adapters, as documented in the [original import report](../KingVanguard-Reference-20260929/README.md).
