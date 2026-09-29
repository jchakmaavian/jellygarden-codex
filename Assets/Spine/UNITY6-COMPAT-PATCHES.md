# spine-unity 3.8 - Unity 6000.5 compatibility patches

Runtime: official spine-unity 3.8 (spine-runtimes branch 3.8, commit 8b4844bd4b193ba9e54487ed397a777993cbad56),
with spine-csharp 3.8 from the same commit in `Runtime/spine-csharp`.

Unity 6.5 made the int instance-ID API a compile error (CS0619). Every patch below is guarded by
`#if UNITY_6000_5_OR_NEWER` and keeps the original code in the `#else` branch. No animation, skeleton, mesh generation or
rendering code was changed. Applied by `Design/Characters/KingVenguard/V1/Source/tools/runtime/patch_spine_unity6.py`
(kept outside Assets; run it on a fresh 3.8 checkout of the same commit to reproduce this folder).

| File | Sites | Change |
|---|---|---|
| `Runtime/spine-unity/Components/SkeletonMecanim.cs` | 1 | AnimationClip equality/hash for the clip dictionary: EntityId instead of the removed instance id. |
| `Editor/spine-unity/Editor/Asset Types/SpineAtlasAssetInspector.cs` | 2 | GetAssetPath(Object) overload, identical result, available in every Unity version. |
| `Editor/spine-unity/Editor/Utility/DataReloadHandler.cs` | 1 | In-memory key for the SkeletonGraphic whose data asset is being reloaded. |
| `Editor/spine-unity/Editor/Utility/AssetUtility.cs` | 1 | Same key as DataReloadHandler, looked up after the reload. |
| `Editor/spine-unity/Editor/Utility/AssetUtility.cs` | 1 | Re-fetch of a 'fake null' atlas reference after reimport: validity test on EntityId. |
| `Editor/spine-unity/Editor/Utility/AssetUtility.cs` | 1 | Same re-fetch through EntityIdToObject. |
| `Editor/spine-unity/Editor/Utility/SpineEditorUtilities.cs` | 1 | Drag-and-drop callback registered on the EntityId hierarchy event. |
| `Editor/spine-unity/Editor/Utility/SpineEditorUtilities.cs` | 1 | Icon callback unregistered from the EntityId hierarchy event. |
| `Editor/spine-unity/Editor/Utility/SpineEditorUtilities.cs` | 1 | Icon callback registered on the EntityId hierarchy event. |
| `Editor/spine-unity/Editor/Utility/SpineEditorUtilities.cs` | 1 | Icon table key (skeletonRendererTable): EntityId hash, matched by the EntityId callback below. |
| `Editor/spine-unity/Editor/Utility/SpineEditorUtilities.cs` | 1 | Icon table key (skeletonUtilityBoneTable): EntityId hash, matched by the EntityId callback below. |
| `Editor/spine-unity/Editor/Utility/SpineEditorUtilities.cs` | 1 | Icon table key (boundingBoxFollowerTable): EntityId hash, matched by the EntityId callback below. |
| `Editor/spine-unity/Editor/Utility/SpineEditorUtilities.cs` | 1 | Icon table key (boundingBoxFollowerGraphicTable): EntityId hash, matched by the EntityId callback below. |
| `Editor/spine-unity/Editor/Utility/SpineEditorUtilities.cs` | 1 | EntityId overload of the icon callback, forwarding the same key the tables use. |
| `Editor/spine-unity/Editor/Utility/SpineEditorUtilities.cs` | 1 | EntityId overload of the drag-and-drop callback; remembers the hovered EntityId for the look-up. |
| `Editor/spine-unity/Editor/Utility/SpineEditorUtilities.cs` | 1 | Hovered object resolved through EntityIdToObject. |

## Deprecations (warnings only)

APIs that 6000.5 deprecates without removing, plus two serialization-analyzer notes. Applied by
`patch_spine_unity6_deprecations.py` after the first set, with the same `#if UNITY_6000_5_OR_NEWER` / `#else` guards.
The sprite-slice port is additionally guarded by `SPINE_2D_SPRITE` (the 2D Sprite package).

| File | Sites | Change |
|---|---|---|
| `Runtime/spine-unity/Components/SkeletonUtility/SkeletonUtility.cs` | 1 | Rigidbody2D.isKinematic -> bodyType (Kinematic or Dynamic, as before). |
| `Editor/spine-unity/Editor/Components/SkeletonUtilityBoneInspector.cs` | 1 | Rigidbody2D.isKinematic = true -> bodyType Kinematic (2D hinge chain only). |
| `Editor/spine-unity/Editor/Utility/BuildSettings.cs` | 1 | Groups with no NamedBuildTarget (retired platforms such as WebPlayer or PS3) count as invalid: FromBuildTargetGroup throws for them, where the ...ForGroup methods only logged a validation error. |
| `Editor/spine-unity/Editor/Utility/BuildSettings.cs` | 2 | GetScriptingDefineSymbolsForGroup -> GetScriptingDefineSymbols(NamedBuildTarget). |
| `Editor/spine-unity/Editor/Utility/BuildSettings.cs` | 2 | SetScriptingDefineSymbolsForGroup -> SetScriptingDefineSymbols(NamedBuildTarget). |
| `Editor/spine-unity/Editor/Utility/DataReloadHandler.cs` | 2 | FindObjectsOfType<SkeletonRenderer> -> FindObjectsByType (active objects, order not used). |
| `Editor/spine-unity/Editor/Utility/DataReloadHandler.cs` | 2 | FindObjectsOfType<SkeletonGraphic> -> FindObjectsByType (active objects, order not used). |
| `Runtime/spine-unity/Asset Types/SkeletonDataCompatibility.cs` | 1 | [NonSerialized] on an int[][] field Unity cannot serialise anyway (analyzer UAC1009). |
| `Runtime/spine-unity/Components/SkeletonAnimation.cs` | 1 | [NonSerialized] on the runtime AnimationState field, which was never serialised (analyzer UAC1001). |
| `Editor/spine-unity/Editor/Asset Types/SpineAtlasAssetInspector.cs` | 1 | Sprite slices read through ISpriteEditorDataProvider instead of the deprecated TextureImporter.spritesheet. |
| `Editor/spine-unity/Editor/Asset Types/SpineAtlasAssetInspector.cs` | 1 | New slices created as SpriteRect (centre pivot, as before) with a fresh sprite id. |
| `Editor/spine-unity/Editor/Asset Types/SpineAtlasAssetInspector.cs` | 1 | Slices written back through the provider, with name/file-id pairs for the new ones (stable references). |
| `Editor/spine-unity-editor.asmdef` | 1 | References Unity.2D.Sprite.Editor; defines SPINE_2D_SPRITE when com.unity.2d.sprite is installed. |
