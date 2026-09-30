using Spine.Unity;
using UnityEditor;
using UnityEngine;

namespace RoyalThrone.Characters.EditorTools
{
    /// <summary>Builds Resources/KingVenguard/KingVenguard.prefab from the imported Spine 3.8 data (official spine-unity runtime).</summary>
    public static class KingVenguardPrefabBuilder
    {
        public const string Root = "Assets/Characters/KingVenguard/V1/";
        public const string SkeletonDataPath = Root + "Spine/King_Venguard_SkeletonData.asset";
        public const string MaterialPath = Root + "Materials/KingVenguard_SkeletonGraphic_StraightAlpha.mat";
        public const string PrefabPath = Root + "Resources/KingVenguard/KingVenguard.prefab";

        [MenuItem("Royal Throne/King Venguard/Rebuild prefab")]
        public static void BuildMenu() { Debug.Log(Build()); }

        public static string Build()
        {
            var data = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(SkeletonDataPath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (!data || !material) return "King Venguard prefab not built: missing " + (!data ? SkeletonDataPath : MaterialPath);
            if (data.GetSkeletonData(false) == null) return "King Venguard prefab not built: skeleton data failed to load";

            // Spine Editor-style mix setup on the data asset: idle <-> happy blend over 0.2 s. happy_thumbs_up starts and ends on
            // idle's first pose, so the blend only smooths a trigger that lands mid-idle.
            data.defaultMix = 0.2f;
            data.fromAnimation = new[] { KingVenguardCharacter.IdleAnimation, KingVenguardCharacter.HappyAnimation };
            data.toAnimation = new[] { KingVenguardCharacter.HappyAnimation, KingVenguardCharacter.IdleAnimation };
            data.duration = new[] { 0.2f, 0.2f };
            EditorUtility.SetDirty(data);
            // The exported atlas is straight (not premultiplied) alpha: every page material and the UI material read it that way.
            foreach (var page in data.atlasAssets[0].Materials)
            {
                page.SetFloat("_StraightAlphaInput", 1); page.EnableKeyword("_STRAIGHT_ALPHA_INPUT"); EditorUtility.SetDirty(page);
            }
            material.SetFloat("_StraightAlphaInput", 1); material.EnableKeyword("_STRAIGHT_ALPHA_INPUT"); EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            int pages = data.atlasAssets[0].MaterialCount;

            var root = new GameObject("KingVenguard", typeof(RectTransform));
            try
            {
                var rt = (RectTransform)root.transform;
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero; rt.pivot = new Vector2(.5f, .5f);
                var graphic = SkeletonGraphic.NewSkeletonGraphicGameObject(data, root.transform, material);
                graphic.gameObject.name = "Skeleton";
                graphic.initialSkinName = "default";
                graphic.startingAnimation = KingVenguardCharacter.IdleAnimation;
                graphic.startingLoop = true;
                // One page (RigReady/spine-unity) keeps SkeletonGraphic on its single-renderer path. spine-unity 3.8's multi-page
                // path draws through child RawImages that uGUI can overwrite with a white quad for a frame.
                graphic.allowMultipleCanvasRenderers = pages > 1;
                graphic.unscaledTime = true;                    // UI character: keeps its authored timing when gameplay time is scaled
                graphic.raycastTarget = false;                  // never blocks input
                graphic.maskable = true;
                graphic.Initialize(true);
                var character = root.AddComponent<KingVenguardCharacter>();
                var so = new SerializedObject(character);
                so.FindProperty("skeletonGraphic").objectReferenceValue = graphic;
                so.ApplyModifiedPropertiesWithoutUndo();
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(PrefabPath));
                AssetDatabase.Refresh();
                var saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool ok);
                return ok && saved ? "King Venguard prefab saved: " + PrefabPath + " (atlas pages " + pages + ", multiple canvas renderers " + graphic.allowMultipleCanvasRenderers + ")" : "King Venguard prefab save failed";
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
