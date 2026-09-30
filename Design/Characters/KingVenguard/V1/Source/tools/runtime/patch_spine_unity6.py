"""Minimal Unity 6000.5 compatibility patches for the official spine-unity 3.8 runtime (EsotericSoftware/spine-runtimes,
branch 3.8, commit 8b4844bd4b193ba9e54487ed397a777993cbad56).

Unity 6.5 turned the int instance-ID API into compile errors (CS0619): Object.GetInstanceID, AssetDatabase.GetAssetPath(int),
EditorUtility.InstanceIDToObject(int) and EditorApplication.hierarchyWindowItemOnGUI. spine-unity 3.8 uses those ids only as
in-memory dictionary keys, for editor object look-ups and for Hierarchy-window icon callbacks. Every change is guarded by
#if UNITY_6000_5_OR_NEWER with the original code kept in the #else branch, and nothing touches animation, mesh generation or
rendering. usage: patch_spine_unity6.py <path to the Spine folder>   (writes UNITY6-COMPAT-PATCHES.md next to it)
"""
import os, sys

ROOT = sys.argv[1]
LOG = []


def patch(rel, old, new, why):
    path = os.path.join(ROOT, rel)
    text = open(path, encoding="utf-8-sig").read()
    n = text.count(old)
    if n == 0:
        raise SystemExit("patch target not found in %s:\n%s" % (rel, old))
    text = text.replace(old, new)
    with open(path, "w", encoding="utf-8", newline="") as f:
        f.write(text)
    LOG.append((rel, n, why))


G = "#if UNITY_6000_5_OR_NEWER"

# ---- runtime: SkeletonMecanim's AnimationClip comparer (used as a dictionary key)
patch("Runtime/spine-unity/Components/SkeletonMecanim.cs",
      "\t\t\t\tpublic bool Equals (AnimationClip x, AnimationClip y) { return x.GetInstanceID() == y.GetInstanceID(); }\n"
      "\t\t\t\tpublic int GetHashCode (AnimationClip o) { return o.GetInstanceID(); }\n",
      G + "\n"
      "\t\t\t\tpublic bool Equals (AnimationClip x, AnimationClip y) { return x.GetEntityId().Equals(y.GetEntityId()); }\n"
      "\t\t\t\tpublic int GetHashCode (AnimationClip o) { return o.GetEntityId().GetHashCode(); }\n"
      "#else\n"
      "\t\t\t\tpublic bool Equals (AnimationClip x, AnimationClip y) { return x.GetInstanceID() == y.GetInstanceID(); }\n"
      "\t\t\t\tpublic int GetHashCode (AnimationClip o) { return o.GetInstanceID(); }\n"
      "#endif\n",
      "AnimationClip equality/hash for the clip dictionary: EntityId instead of the removed instance id.")

# ---- editor: texture asset paths in the atlas inspector
patch("Editor/spine-unity/Editor/Asset Types/SpineAtlasAssetInspector.cs",
      "AssetDatabase.GetAssetPath(texture.GetInstanceID())",
      "AssetDatabase.GetAssetPath(texture)",
      "GetAssetPath(Object) overload, identical result, available in every Unity version.")

# ---- editor: SkeletonGraphic -> SkeletonDataAsset path bookkeeping across a data reload (int dictionary keys)
patch("Editor/spine-unity/Editor/Utility/DataReloadHandler.cs",
      "\t\t\t\t\t\tvar sgID = sg.GetInstanceID();\n",
      G + "\n\t\t\t\t\t\tvar sgID = sg.GetEntityId().GetHashCode();\n#else\n\t\t\t\t\t\tvar sgID = sg.GetInstanceID();\n#endif\n",
      "In-memory key for the SkeletonGraphic whose data asset is being reloaded.")
patch("Editor/spine-unity/Editor/Utility/AssetUtility.cs",
      "\t\t\t\t\tvar skeletonGraphicID = skeletonGraphic.GetInstanceID();\n",
      G + "\n\t\t\t\t\tvar skeletonGraphicID = skeletonGraphic.GetEntityId().GetHashCode();\n#else\n\t\t\t\t\tvar skeletonGraphicID = skeletonGraphic.GetInstanceID();\n#endif\n",
      "Same key as DataReloadHandler, looked up after the reload.")
patch("Editor/spine-unity/Editor/Utility/AssetUtility.cs",
      "\t\t\t\t\t\t\t\t\tskeletonDataAtlasAssets[i].GetInstanceID() != 0\n",
      G + "\n\t\t\t\t\t\t\t\t\tskeletonDataAtlasAssets[i].GetEntityId().IsValid()\n#else\n\t\t\t\t\t\t\t\t\tskeletonDataAtlasAssets[i].GetInstanceID() != 0\n#endif\n",
      "Re-fetch of a 'fake null' atlas reference after reimport: validity test on EntityId.")
patch("Editor/spine-unity/Editor/Utility/AssetUtility.cs",
      "\t\t\t\t\t\t\t\t\tskeletonDataAtlasAssets[i] = EditorUtility.InstanceIDToObject(skeletonDataAtlasAssets[i].GetInstanceID()) as AtlasAssetBase;\n",
      G + "\n\t\t\t\t\t\t\t\t\tskeletonDataAtlasAssets[i] = EditorUtility.EntityIdToObject(skeletonDataAtlasAssets[i].GetEntityId()) as AtlasAssetBase;\n#else\n"
      "\t\t\t\t\t\t\t\t\tskeletonDataAtlasAssets[i] = EditorUtility.InstanceIDToObject(skeletonDataAtlasAssets[i].GetInstanceID()) as AtlasAssetBase;\n#endif\n",
      "Same re-fetch through EntityIdToObject.")

# ---- editor: Hierarchy-window callbacks (drag-and-drop of skeleton data, component icons)
SEU = "Editor/spine-unity/Editor/Utility/SpineEditorUtilities.cs"
patch(SEU,
      "\t\t\tEditorApplication.hierarchyWindowItemOnGUI -= HierarchyHandler.HandleDragAndDrop;\n"
      "\t\t\tEditorApplication.hierarchyWindowItemOnGUI += HierarchyHandler.HandleDragAndDrop;\n",
      G + "\n\t\t\tEditorApplication.hierarchyWindowItemByEntityIdOnGUI -= HierarchyHandler.HandleDragAndDrop;\n"
      "\t\t\tEditorApplication.hierarchyWindowItemByEntityIdOnGUI += HierarchyHandler.HandleDragAndDrop;\n#else\n"
      "\t\t\tEditorApplication.hierarchyWindowItemOnGUI -= HierarchyHandler.HandleDragAndDrop;\n"
      "\t\t\tEditorApplication.hierarchyWindowItemOnGUI += HierarchyHandler.HandleDragAndDrop;\n#endif\n",
      "Drag-and-drop callback registered on the EntityId hierarchy event.")
patch(SEU,
      "\t\t\t\tEditorApplication.hierarchyWindowItemOnGUI -= IconsOnGUI;\n",
      G + "\n\t\t\t\tEditorApplication.hierarchyWindowItemByEntityIdOnGUI -= IconsOnGUI;\n#else\n\t\t\t\tEditorApplication.hierarchyWindowItemOnGUI -= IconsOnGUI;\n#endif\n",
      "Icon callback unregistered from the EntityId hierarchy event.")
patch(SEU,
      "\t\t\t\t\tEditorApplication.hierarchyWindowItemOnGUI += IconsOnGUI;\n",
      G + "\n\t\t\t\t\tEditorApplication.hierarchyWindowItemByEntityIdOnGUI += IconsOnGUI;\n#else\n\t\t\t\t\tEditorApplication.hierarchyWindowItemOnGUI += IconsOnGUI;\n#endif\n",
      "Icon callback registered on the EntityId hierarchy event.")
for comp, table in (("r", "skeletonRendererTable"), ("b", "skeletonUtilityBoneTable"), ("bbf", "boundingBoxFollowerTable"), ("bbf", "boundingBoxFollowerGraphicTable")):
    old = "\t\t\t\t\t%s[%s.gameObject.GetInstanceID()] = %s" % (table, comp, comp)
    patch(SEU, old + ("" if table != "skeletonRendererTable" else ".gameObject") + ";\n",
          G + "\n" + old.replace("GetInstanceID()", "GetEntityId().GetHashCode()") + ("" if table != "skeletonRendererTable" else ".gameObject") + ";\n#else\n"
          + old + ("" if table != "skeletonRendererTable" else ".gameObject") + ";\n#endif\n",
          "Icon table key (%s): EntityId hash, matched by the EntityId callback below." % table)
patch(SEU,
      "\t\t\tinternal static void IconsOnGUI (int instanceId, Rect selectionRect) {\n",
      G + "\n\t\t\tinternal static void IconsOnGUI (EntityId entityId, Rect selectionRect) { IconsOnGUI(entityId.GetHashCode(), selectionRect); }\n#endif\n"
      "\t\t\tinternal static void IconsOnGUI (int instanceId, Rect selectionRect) {\n",
      "EntityId overload of the icon callback, forwarding the same key the tables use.")

# the drag-and-drop handler looks the hovered object up by id
text = open(os.path.join(ROOT, SEU), encoding="utf-8-sig").read()
sig = "internal static void HandleDragAndDrop (int instanceId, Rect selectionRect) {"
if sig not in text:
    raise SystemExit("HandleDragAndDrop signature not found")
patch(SEU, "\t\t\t" + sig + "\n",
      G + "\n\t\t\tstatic EntityId dragTargetEntityId;\n"
      "\t\t\tinternal static void HandleDragAndDrop (EntityId entityId, Rect selectionRect) { dragTargetEntityId = entityId; HandleDragAndDrop(entityId.GetHashCode(), selectionRect); }\n#endif\n"
      "\t\t\t" + sig + "\n",
      "EntityId overload of the drag-and-drop callback; remembers the hovered EntityId for the look-up.")
patch(SEU,
      "\t\t\t\t\t\t\t\t\t\tvar mouseOverTarget = UnityEditor.EditorUtility.InstanceIDToObject(instanceId);\n",
      G + "\n\t\t\t\t\t\t\t\t\t\tvar mouseOverTarget = UnityEditor.EditorUtility.EntityIdToObject(dragTargetEntityId);\n#else\n"
      "\t\t\t\t\t\t\t\t\t\tvar mouseOverTarget = UnityEditor.EditorUtility.InstanceIDToObject(instanceId);\n#endif\n",
      "Hovered object resolved through EntityIdToObject.")

with open(os.path.join(ROOT, "UNITY6-COMPAT-PATCHES.md"), "w", encoding="utf-8", newline="\n") as f:
    f.write("# spine-unity 3.8 - Unity 6000.5 compatibility patches\n\n")
    f.write("Runtime: official spine-unity 3.8 (spine-runtimes branch 3.8, commit 8b4844bd4b193ba9e54487ed397a777993cbad56),\n"
            "with spine-csharp 3.8 from the same commit in `Runtime/spine-csharp`.\n\n")
    f.write("Unity 6.5 made the int instance-ID API a compile error (CS0619). Every patch below is guarded by\n"
            "`#if UNITY_6000_5_OR_NEWER` and keeps the original code in the `#else` branch. No animation, skeleton, mesh generation or\n"
            "rendering code was changed. Applied by `patch_spine_unity6.py`.\n\n| File | Sites | Change |\n|---|---|---|\n")
    for rel, n, why in LOG:
        f.write("| `%s` | %d | %s |\n" % (rel, n, why))
print("applied", len(LOG), "patches")
for rel, n, why in LOG: print("  %-70s x%d" % (rel, n))
