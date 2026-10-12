using System.IO;
using Spine.Unity;
using UnityEditor;
using UnityEngine;

namespace RoyalThrone.Characters.EditorTools
{
    public static class KingVanguardPrefabBuilder
    {
        public const string Root = "Assets/Characters/KingVenguard/V2/";
        public const string PortraitPath = Root + "Resources/KingVanguard/KingVanguard.prefab";
        public const string CelebrationPath = Root + "Resources/KingVanguard/KingVanguardCelebration.prefab";
        [MenuItem("Royal Throne/King Vanguard/Rebuild APK-derived prefabs")]
        public static void BuildMenu() { Debug.Log(Build()); }
        public static string Build()
        {
            return BuildOne(false) + "\n" + BuildOne(true);
        }
        static string BuildOne(bool area)
        {
            string stem = area ? "AreaCompleted_Vanguard" : "King_Vanguard";
            string path = area ? CelebrationPath : PortraitPath;
            var data = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(Root + "Spine/" + stem + "_SkeletonData.asset");
            var material = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/KingVanguard_SkeletonGraphic_StraightAlpha.mat");
            if (!data || !material || data.GetSkeletonData(false) == null) throw new System.InvalidOperationException("Missing or invalid Vanguard import: " + stem);
            data.defaultMix = .18f; data.FillStateData(); EditorUtility.SetDirty(data);
            var root = new GameObject(area ? "KingVanguardCelebration" : "KingVanguard", typeof(RectTransform));
            try
            {
                var rt = (RectTransform)root.transform;
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
                var graphic = SkeletonGraphic.NewSkeletonGraphicGameObject(data, root.transform, material);
                graphic.name = "Skeleton"; graphic.initialSkinName = "default";
                graphic.startingAnimation = area ? KingVanguardCharacter.AreaIdleAnimation : KingVanguardCharacter.IdleAnimation;
                graphic.startingLoop = area;
                graphic.unscaledTime = true; graphic.raycastTarget = false; graphic.maskable = true;
                graphic.allowMultipleCanvasRenderers = data.atlasAssets[0].MaterialCount > 1;
                graphic.Initialize(true);
                graphic.gameObject.AddComponent<KingVanguardFaceRig>();
                var character = root.AddComponent<KingVanguardCharacter>();
                var so = new SerializedObject(character);
                so.FindProperty("skeletonGraphic").objectReferenceValue = graphic;
                so.FindProperty("areaRig").boolValue = area;
                so.ApplyModifiedPropertiesWithoutUndo();
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                AssetDatabase.Refresh();
                var saved = PrefabUtility.SaveAsPrefabAsset(root, path, out bool ok);
                if (!saved || !ok) throw new System.InvalidOperationException("Prefab save failed: " + path);
                AssetDatabase.SaveAssets();
                return "Saved " + path + ": " + data.GetSkeletonData(false).Animations.Count + " original tracks";
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
