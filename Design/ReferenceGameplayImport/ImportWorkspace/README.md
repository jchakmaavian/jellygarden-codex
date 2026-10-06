# Isolated native import workspace

Disposable Unity 6000.5.10f1 workspace containing only the recovered gameplay asset sources and focused Editor importers. It imports and validates native materials, sprites, animation clips/controllers, particle modules and static blocker render hierarchies independently of the running main game Editor.

No original gameplay code, player saves, Android build configuration or unrelated game assets are copied here. No APK is built or run. Generated Unity-native assets are copied back to their matching `Assets/ReferenceGameplay/` paths only after validation. Reports retained outside this disposable workspace distinguish asset compatibility checks from actual gameplay verification.

Unity caches and duplicate source copies are intentionally excluded from version control.
