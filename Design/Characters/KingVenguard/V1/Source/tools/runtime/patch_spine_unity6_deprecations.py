"""Second set of Unity 6000.5 patches for spine-unity 3.8: the APIs 6.5 only deprecates (CS0618 warnings) and two
serialization-analyzer notes. Run after patch_spine_unity6.py, on the same folder.

Same rules as the first set: every change is guarded by #if UNITY_6000_5_OR_NEWER with the original code kept in the
#else branch, and no animation, skeleton, mesh generation or rendering code is touched. The sprite-slice port also needs
the 2D Sprite package, so it is additionally guarded by SPINE_2D_SPRITE, which the editor asmdef defines only when
com.unity.2d.sprite is installed. usage: patch_spine_unity6_deprecations.py <path to the Spine folder>
(appends its table to UNITY6-COMPAT-PATCHES.md)
"""
import json, os, sys

ROOT = sys.argv[1]
LOG = []
# The script is not transactional: refuse a folder that already carries this set.
if "SPINE_2D_SPRITE" in open(os.path.join(ROOT, "Editor", "spine-unity", "Editor", "Asset Types", "SpineAtlasAssetInspector.cs"),
                             encoding="utf-8-sig").read():
    raise SystemExit("this folder already has the deprecation patches")


def patch(rel, old, new, why, count=None):
    path = os.path.join(ROOT, rel)
    text = open(path, encoding="utf-8-sig").read()
    n = text.count(old)
    if n == 0 or (count is not None and n != count):
        raise SystemExit("patch target found %d times in %s:\n%s" % (n, rel, old))
    text = text.replace(old, new)
    with open(path, "w", encoding="utf-8", newline="") as f:
        f.write(text)
    LOG.append((rel, n, why))


G = "#if UNITY_6000_5_OR_NEWER"
S = "#if UNITY_6000_5_OR_NEWER && SPINE_2D_SPRITE"

# ---- Rigidbody2D.isKinematic -> bodyType (same body type in both directions)
patch("Runtime/spine-unity/Components/SkeletonUtility/SkeletonUtility.cs",
      "\t\t\t\trb.isKinematic = isKinematic;\n",
      G + "\n\t\t\t\trb.bodyType = isKinematic ? RigidbodyType2D.Kinematic : RigidbodyType2D.Dynamic;\n#else\n"
      "\t\t\t\trb.isKinematic = isKinematic;\n#endif\n",
      "Rigidbody2D.isKinematic -> bodyType (Kinematic or Dynamic, as before).", count=1)
# Only the 2D hinge chain: the 3D chain's Rigidbody.isKinematic is not deprecated, so the target includes the 2D follower.
patch("Editor/spine-unity/Editor/Components/SkeletonUtilityBoneInspector.cs",
      "\t\t\tfollowerRigidbody.isKinematic = true;\n\t\t\tfollowerKinematicObject.AddComponent<FollowLocationRigidbody2D>()",
      G + "\n\t\t\tfollowerRigidbody.bodyType = RigidbodyType2D.Kinematic;\n#else\n\t\t\tfollowerRigidbody.isKinematic = true;\n#endif\n"
      "\t\t\tfollowerKinematicObject.AddComponent<FollowLocationRigidbody2D>()",
      "Rigidbody2D.isKinematic = true -> bodyType Kinematic (2D hinge chain only).", count=1)

# ---- scripting define symbols through NamedBuildTarget (what the ...ForGroup methods forward to)
# The define loops walk every BuildTargetGroup value. FromBuildTargetGroup throws for the retired ones (WebPlayer, PS3,
# Tizen, ...), where the ...ForGroup methods only logged a validation error, so those groups count as invalid first.
patch("Editor/spine-unity/Editor/Utility/BuildSettings.cs",
      "\t\t\tint gi = (int)group;\n\t\t\treturn\n",
      "\t\t\tint gi = (int)group;\n" + G + "\n"
      "\t\t\t// Retired groups (WebPlayer, PS3, Tizen, ...) have no NamedBuildTarget, and FromBuildTargetGroup throws for them.\n"
      "\t\t\ttry { UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(group); }\n"
      "\t\t\tcatch (System.ArgumentException) { return true; }\n"
      "#endif\n"
      "\t\t\treturn\n",
      "Groups with no NamedBuildTarget (retired platforms such as WebPlayer or PS3) count as invalid: FromBuildTargetGroup "
      "throws for them, where the ...ForGroup methods only logged a validation error.", count=1)
patch("Editor/spine-unity/Editor/Utility/BuildSettings.cs",
      "\t\t\t\tstring defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);\n",
      G + "\n\t\t\t\tstring defines = PlayerSettings.GetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(group));\n#else\n"
      "\t\t\t\tstring defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);\n#endif\n",
      "GetScriptingDefineSymbolsForGroup -> GetScriptingDefineSymbols(NamedBuildTarget).", count=2)
patch("Editor/spine-unity/Editor/Utility/BuildSettings.cs",
      "\t\t\t\t\tPlayerSettings.SetScriptingDefineSymbolsForGroup(group, defines);\n",
      G + "\n\t\t\t\t\tPlayerSettings.SetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(group), defines);\n#else\n"
      "\t\t\t\t\tPlayerSettings.SetScriptingDefineSymbolsForGroup(group, defines);\n#endif\n",
      "SetScriptingDefineSymbolsForGroup -> SetScriptingDefineSymbols(NamedBuildTarget).", count=2)

# ---- FindObjectsOfType -> FindObjectsByType (unsorted; the reload loops do not depend on order)
patch("Editor/spine-unity/Editor/Utility/DataReloadHandler.cs",
      "\t\t\t\tvar activeSkeletonRenderers = GameObject.FindObjectsOfType<SkeletonRenderer>();\n",
      G + "\n\t\t\t\tvar activeSkeletonRenderers = GameObject.FindObjectsByType<SkeletonRenderer>();\n#else\n"
      "\t\t\t\tvar activeSkeletonRenderers = GameObject.FindObjectsOfType<SkeletonRenderer>();\n#endif\n",
      "FindObjectsOfType<SkeletonRenderer> -> FindObjectsByType (active objects, order not used).", count=2)
patch("Editor/spine-unity/Editor/Utility/DataReloadHandler.cs",
      "\t\t\t\tvar activeSkeletonGraphics = GameObject.FindObjectsOfType<SkeletonGraphic>();\n",
      G + "\n\t\t\t\tvar activeSkeletonGraphics = GameObject.FindObjectsByType<SkeletonGraphic>();\n#else\n"
      "\t\t\t\tvar activeSkeletonGraphics = GameObject.FindObjectsOfType<SkeletonGraphic>();\n#endif\n",
      "FindObjectsOfType<SkeletonGraphic> -> FindObjectsByType (active objects, order not used).", count=2)

# ---- serialization analyzer: fields Unity never serialised say so
patch("Runtime/spine-unity/Asset Types/SkeletonDataCompatibility.cs",
      "\t\t\tpublic int[][] compatibleVersions;\n",
      G + "\n\t\t\t[System.NonSerialized]\n#endif\n\t\t\tpublic int[][] compatibleVersions;\n",
      "[NonSerialized] on an int[][] field Unity cannot serialise anyway (analyzer UAC1009).", count=1)
patch("Runtime/spine-unity/Components/SkeletonAnimation.cs",
      "\t\tpublic Spine.AnimationState state;\n",
      G + "\n\t\t[System.NonSerialized]\n#endif\n\t\tpublic Spine.AnimationState state;\n",
      "[NonSerialized] on the runtime AnimationState field, which was never serialised (analyzer UAC1001).", count=1)

# ---- TextureImporter.spritesheet -> ISpriteEditorDataProvider in "Apply Regions as Texture Sprite Slices"
A = "Editor/spine-unity/Editor/Asset Types/SpineAtlasAssetInspector.cs"
patch(A,
      "\t\t\tvar spriteSheet = t.spritesheet;\n\t\t\tvar sprites = new List<SpriteMetaData>(spriteSheet);\n",
      S + "\n"
      "\t\t\t// TextureImporter.spritesheet is deprecated in 6000.5: read and write the slices through the sprite data provider.\n"
      "\t\t\tvar spriteFactories = new UnityEditor.U2D.Sprites.SpriteDataProviderFactories();\n"
      "\t\t\tspriteFactories.Init();\n"
      "\t\t\tvar spriteDataProvider = spriteFactories.GetSpriteEditorDataProviderFromObject(t);\n"
      "\t\t\tspriteDataProvider.InitSpriteEditorDataProvider();\n"
      "\t\t\tvar sprites = new List<SpriteRect>(spriteDataProvider.GetSpriteRects());\n"
      "\t\t\tvar addedSprites = new List<SpriteRect>();\n"
      "#else\n"
      "\t\t\tvar spriteSheet = t.spritesheet;\n\t\t\tvar sprites = new List<SpriteMetaData>(spriteSheet);\n"
      "#endif\n",
      "Sprite slices read through ISpriteEditorDataProvider instead of the deprecated TextureImporter.spritesheet.", count=1)
patch(A,
      "\t\t\t\t\t\tsprites.Add(new SpriteMetaData {\n\t\t\t\t\t\t\tname = r.name,\n\t\t\t\t\t\t\tpivot = new Vector2(0.5f, 0.5f),\n"
      "\t\t\t\t\t\t\trect = spriteRect\n\t\t\t\t\t\t});\n",
      S + "\n"
      "\t\t\t\t\t\tvar addedSprite = new SpriteRect {\n\t\t\t\t\t\t\tname = r.name,\n\t\t\t\t\t\t\tpivot = new Vector2(0.5f, 0.5f),\n"
      "\t\t\t\t\t\t\talignment = SpriteAlignment.Center,\n\t\t\t\t\t\t\trect = spriteRect,\n\t\t\t\t\t\t\tspriteID = GUID.Generate()\n\t\t\t\t\t\t};\n"
      "\t\t\t\t\t\tsprites.Add(addedSprite);\n\t\t\t\t\t\taddedSprites.Add(addedSprite);\n"
      "#else\n"
      "\t\t\t\t\t\tsprites.Add(new SpriteMetaData {\n\t\t\t\t\t\t\tname = r.name,\n\t\t\t\t\t\t\tpivot = new Vector2(0.5f, 0.5f),\n"
      "\t\t\t\t\t\t\trect = spriteRect\n\t\t\t\t\t\t});\n"
      "#endif\n",
      "New slices created as SpriteRect (centre pivot, as before) with a fresh sprite id.", count=1)
patch(A,
      "\t\t\tt.spritesheet = sprites.ToArray();\n",
      S + "\n"
      "\t\t\tspriteDataProvider.SetSpriteRects(sprites.ToArray());\n"
      "\t\t\tvar nameFileIds = spriteDataProvider.GetDataProvider<UnityEditor.U2D.Sprites.ISpriteNameFileIdDataProvider>();\n"
      "\t\t\tif (nameFileIds != null) {\n"
      "\t\t\t\tvar pairs = new List<SpriteNameFileIdPair>(nameFileIds.GetNameFileIdPairs());\n"
      "\t\t\t\tforeach (var added in addedSprites) pairs.Add(new SpriteNameFileIdPair(added.name, added.spriteID));\n"
      "\t\t\t\tnameFileIds.SetNameFileIdPairs(pairs);\n"
      "\t\t\t}\n"
      "\t\t\tspriteDataProvider.Apply();\n"
      "#else\n"
      "\t\t\tt.spritesheet = sprites.ToArray();\n"
      "#endif\n",
      "Slices written back through the provider, with name/file-id pairs for the new ones (stable references).", count=1)

# ---- the editor assembly may use the 2D Sprite editor API when the package is present
asmdef = os.path.join(ROOT, "Editor", "spine-unity-editor.asmdef")
data = json.load(open(asmdef, encoding="utf-8-sig"))
if "Unity.2D.Sprite.Editor" not in data.setdefault("references", []):
    data["references"].append("Unity.2D.Sprite.Editor")
defines = data.setdefault("versionDefines", [])
if not any(d.get("define") == "SPINE_2D_SPRITE" for d in defines):
    defines.append({"name": "com.unity.2d.sprite", "expression": "1.0.0", "define": "SPINE_2D_SPRITE"})
with open(asmdef, "w", encoding="utf-8", newline="\n") as f:
    f.write(json.dumps(data, indent=4) + "\n")
LOG.append(("Editor/spine-unity-editor.asmdef", 1, "References Unity.2D.Sprite.Editor; defines SPINE_2D_SPRITE when com.unity.2d.sprite is installed."))

doc = os.path.join(ROOT, "UNITY6-COMPAT-PATCHES.md")
with open(doc, "a", encoding="utf-8", newline="\n") as f:
    f.write("\n## Deprecations (warnings only)\n\n"
            "APIs that 6000.5 deprecates without removing, plus two serialization-analyzer notes. Applied by\n"
            "`patch_spine_unity6_deprecations.py` after the first set, with the same `#if UNITY_6000_5_OR_NEWER` / `#else` guards.\n"
            "The sprite-slice port is additionally guarded by `SPINE_2D_SPRITE` (the 2D Sprite package).\n\n"
            "| File | Sites | Change |\n|---|---|---|\n")
    for rel, n, why in LOG:
        f.write("| `%s` | %d | %s |\n" % (rel, n, why))
print("applied", len(LOG), "patches")
for rel, n, why in LOG: print("  %-72s x%d" % (rel, n))
