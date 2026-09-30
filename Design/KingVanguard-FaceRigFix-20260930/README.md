# King Vanguard facial rig registration

The teeth, eyebrows and moustache did not stay registered throughout the imported expressions. The previous face modifier changed geometry in the bind pose, then the original animated bones acted on that already-transformed geometry. Weighted moustache/brows and unweighted teeth therefore diverged during expressions even when the starting pose looked aligned.

The measured previous moustache error reached 26.62 source units during sadness, about 23 pixels in the 1080-wide HUD. The right brow reached 9.17 units. [Inspection evidence](Inspection/animated-affine-drift.json) compares the previous result against a coherent transformation of the original animated face.

The new `KingVanguardFaceRig` maps the final skinned face in the current animated head frame. Bones, weights, original deformation keys and animation state remain intact. Exact UV and animated-position ownership identify facial vertices without changing the body; interpolated vertices use triangle correspondence. Final mesh bounds are updated after generation. Each character owns its mapping state.

The mouth, upper/lower teeth and tongue retain their original relative scale. The external brow and moustache shadow overlays are hidden because they no longer match Vanguard's repainted contours; the new features already contain their own shading.

The original `Ağız` part is a foreground skin/lip cover with a small transparent opening over the lower teeth and tongue. It had been mistakenly painted as a large dark cavity, making the teeth look detached even after the geometry correction. Built-in imagegen repainted only that cover in Vanguard's peach palette. Technical packing preserves the original alpha opening and changes no other atlas island. The redesigned face, crown, beard and moustache remain installed. [Art prompt](FaceArt/lip-repair-prompt.txt), [generated workboard](FaceRedesign/Portrait-lip-cover-workboard.png), [mouth-only preservation proof](FaceRedesign/mouth-only-validation.json), and [installed asset hashes](Validation/installed-atlases.json).

The animation review tool uses the same mapper and measures the final visible mesh. `Tools/RenderFaceReview.cs` renders seven expression sequences in a private preview scene. `Tools/RecordFaceMotion.cs` records actual Spine animation-state crossfades at 60 fps, including a reaction during a blink. This recording is isolated presentation playback, not natural gameplay input.

## Verified result

- [60 fps facial motion preview](After/King-Vanguard-Face-Motion.mp4): 24 seconds, 1,440 frames; H.264 decode passed.
- [In-game HUD](After/Gameplay-HUD.png), [happy poses](After/k-1sevinme_6.png), [sad poses](After/k-1_uzulme.png), and [blink transition frames](After/blink-transition.png).
- [Face motion checks](Validation/face-motion-checks.txt): 1,275 checked mesh frames including 135 actual crossfade frames; face coverage, unchanged body/bones/deforms, final bounds, instance isolation, repeated-build stability and barycentric mapping passed.
- [Live HUD checks](Validation/live-hud.txt): actual SkeletonGraphic canvas mapping remained registered through 230 happy/return frames. [Normal player profile restored](Validation/final-state.txt).
- [Both character prefabs rebuilt](Validation/prefab-build.txt). Current scripts compile successfully. An existing Unity internal GUID assertion was logged at 06:15:39 UTC before the final checks; no additional errors were logged during the final preview and gameplay checks.

Verification used Unity 6000.5.10f1. No Android device test was performed. The recovered source animation timing and original binary files remain unchanged; this is a rendering/registration correction, not reconstructed APK method bodies.
