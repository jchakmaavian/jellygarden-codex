# King Vanguard: teeth, contact shadows and player attention

The gameplay portrait now keeps each tooth row coherent during the imported facial performance, uses shaded tooth artwork and the original animated contact-shadow masks, and responds to the player and visible match hints.

## Runtime behavior

- `KingVanguardFaceRig` fits each tooth row to its original setup shape in the animated face frame. It preserves the animated centroid, rotation and uniform scale, removing the worry entrance's nonrigid bending. It changes rendered vertices only; source bones, attachments, weights and animation keys remain intact.
- `KingVanguardFaceShapeModifier` restores warm contact shadows under the brows, moustache, beard and crown. Original shadow masks and deformation tracks are retained, with restrained opacity suited to Vanguard's lighter face. The obsolete upper crown shadow remains hidden.
- `KingVanguardAttention` observes the existing Input System route without consuming input. A touch/pressed pointer wins over recently moving mouse; after 1.25 seconds of mouse inactivity, the King can look toward the currently visible, valid match hint. The hint presenter supplies the actual selected pair midpoint or tap piece.
- Gaze blends smoothly with the source pose. The complete character leans at most about 1.46 degrees; a touch produces a small compression response. Original reaction clips take priority, and worry reduces attention strength. Pause, hidden HUD, QA overlay, focus loss and board replacement clear stale attention.
- The component is mounted only on the main gameplay HUD. Original blinking and eyelid occlusion remain active. Input-device filtering follows the game's action asset, including when the global current device belongs to a different input route.

## Artwork and provenance

Native reference: `Design/KingVanguard-Reference-20260929/Original-rfv387`, recovered from the user's reference APK. Vanguard's distinct face and outfit remain the installed design. The original compiled game method bodies are not claimed as recovered logic.

The built-in imagegen tool repainted a registered seven-component workboard. Prompt: `Inspection/ART-PROMPT.txt`; selected image: `FaceRedesign/TeethShadow-painted-workboard.png`. The mouth skin cover was then repainted with softer peach shading and no green matte; prompt: `Inspection/MOUTH-ART-PROMPT.txt`, selected image: `FaceRedesign/Mouth-natural-peach.png`. Technical packing retains source alpha and atlas coordinates. Full packing and matte-extraction evidence is adjacent to the images; eye, moustache and cheek edge residue is cleaned using existing generated paint, without reverting to original facial RGB.

Installed atlases: `Assets/Characters/KingVenguard/V2/Spine/King_Vanguard.png` and `AreaCompleted_Vanguard.png`.

## Verification

- Unity compilation passed.
- All 42 imported tracks: 10,729 rendered poses, 21,458 tooth-row checks and 135 blended frames passed. No tooth triangle inversion or source-data mutation. Maximum centroid difference was under 0.000061 source units.
- 1,275 mesh frames passed facial registration, bounds, original body preservation when attention is inactive, clipped-vertex mapping, repeated rendering, lifecycle and independent-instance checks.
- Live main-HUD checks used virtual Mouse and Touchscreen events through the real InputManager. Pointer/hint priorities, tap response, reaction retirement, reduced worry influence, invalid hints, pause/resume, board identity and QA gating passed. The normal player profile and prior device routing were restored.
- Actual rendered pupil triangles and installed alpha were checked for idle, native blink and worry poses, with source, centered and four cardinal targets. Both pupils moved in each direction; opaque spill did not exceed the same source pose beyond the stated antialias tolerance (1,053 checks).
- `After` contains current renders and a 60 fps motion preview. See `Validation` for exact reports and `Tools` for repeatable fixtures.

These are Unity Editor checks, including simulated touch input. No physical Android-device test or new APK build was performed for this change.

## Backup scope

The task backup stages only the listed Vanguard files, the focused hint presenter change, importer configuration, and this evidence folder. The hint presenter baseline was verified byte-for-byte against the working HEAD before editing. Unrelated working-tree changes are excluded. The private backup branch receives the exact committed task blobs without the large working-branch history.
