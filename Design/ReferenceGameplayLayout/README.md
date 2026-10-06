# Gameplay layout imported from the reference APK

Implemented in the existing `RoyalGameplayPresentation` and `RoyalBoardSurface`.

## Provenance

- Reference kept unchanged: `Design/Backup/apk/Royal_Throne-rf.apk`.
- Reused offline extraction: `Design/Inspection-Levels01-50/ReferencePackage/datapack.unity3d_data/level2` and `sharedassets2.assets`.
- Source level2 hierarchy supplies the equal goal/move panels, central portrait frame, four booster slots and fifth settings slot. `scene-layout.json` retains the inspected transforms and sprite references.
- Twelve decoded source sprites imported under `Assets/ReferenceGameplay/Layout/Resources/ReferenceGameplay/Layout`. Mirrored halves, transparency and panel slicing are retained. `import-inventory.json` identifies their source objects/atlas regions.
- Existing imported `Border-side__17165` supplies the board edge artwork, with orientation per exposed cell edge. No change to board occupancy or colliders.

## Runtime changes

- Replaced generated top panels with the packaged curved banner, cream panel and blue/gold header artwork.
- Framed the existing Royal Throne portrait with the recovered blue portrait frame and backdrop; kept character identity and reactions.
- Replaced the lower rail with packaged green booster circles, gold frames and blue rail. Retained the original four Button/BoostIcon objects, callbacks and inventory counts.
- Moved the existing pause action into the packaged blue settings button in slot five.
- Removed the extra level heading and unrelated castle illustration from gameplay. Uses the original packaged gameplay camera clear color behind the board.
- Board fitting reserves space for the fixed-size header and bottom controls and retains irregular board layouts/ingredient exits.

## Accuracy / scope

Imported UI sprite artwork is source data. Screen anchoring is an adaptation to the existing Canvas and safe-area system; APK dynamic layout code is compiled and was not recovered as editable Unity source. This is not claimed as a pixel-exact APK scene reconstruction.

Royal Throne portrait, readable font, functional booster icons/count badges, board cell fill and ingredient arrows remain the current game's assets. Reference bottom-booster actions differ from this game's actions, so they are not falsely relabelled with incompatible icons. Startup, home, branding, BGM, settings dialogs, saves, level definitions, booster effects and input rules are not changed.

## Recovery

Before-edit checkpoint: `Design/Backup/Gameplay-Layout-20260924-150839`. Includes the already-edited gameplay presentation file exactly as it existed before this pass. Old artwork remains recoverable.

## Verification

See `validation.txt` and the live `Gameplay-Level14.png`, `Gameplay-Pause.png`, `Gameplay-SquareBoard.png` captures. Tests run through the existing isolated QA profile; the normal profile is restored afterwards.

Final result: Unity runtime/editor assemblies compile. Twelve focused Play Mode assertions passed, including actual hand-button selection/cancellation, pause callback, irregular-board bounds, match/refill and normal-profile restoration. Final error tracker: zero new errors. Captures are from the live game at 1080×1920; no scene files were saved by the runner.
