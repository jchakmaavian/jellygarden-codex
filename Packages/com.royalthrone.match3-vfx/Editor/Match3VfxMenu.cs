using System.IO;
using UnityEditor;
using UnityEngine;

namespace RoyalThrone.Match3Vfx.Editor
{
    public static class Match3VfxMenu
    {
        public const string PackageName = "com.royalthrone.match3-vfx";
        public const string PackageRoot = "Packages/" + PackageName;
        public const string MaterialFolder = PackageRoot + "/Runtime/Resources/Match3Vfx";
        public const string SettingsAssetPath = "Assets/Resources/" + Match3VfxSettings.ResourcesPath + ".asset";

        [MenuItem("Tools/Royal Throne/Match-3 VFX Kit/Create Settings Asset", false, 1)]
        public static void CreateSettingsAsset()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Match3VfxSettings>(SettingsAssetPath);
            if (!existing)
            {
                Directory.CreateDirectory("Assets/Resources");
                existing = ScriptableObject.CreateInstance<Match3VfxSettings>();
                AssetDatabase.CreateAsset(existing, SettingsAssetPath);
                AssetDatabase.SaveAssets();
                Debug.Log("[Match3Vfx] Created " + SettingsAssetPath + ". It is loaded automatically at runtime.");
            }
            Selection.activeObject = existing;
            EditorGUIUtility.PingObject(existing);
        }

        [MenuItem("Tools/Royal Throne/Match-3 VFX Kit/Workshop", false, 2)]
        public static void OpenWorkshop() => Match3VfxWorkshop.Open();

        /// <summary>
        /// Recreates the three base materials that keep the kit's shaders in player builds.
        /// Only needed when working on the package itself; the shipped package already contains them.
        /// </summary>
        [MenuItem("Tools/Royal Throne/Match-3 VFX Kit/Maintainers/Rebuild Default Materials", false, 100)]
        public static void RebuildDefaultMaterials()
        {
            Directory.CreateDirectory(Path.GetFullPath(MaterialFolder));
            AssetDatabase.Refresh();
            Save("Additive", Match3VfxMaterials.AdditiveShader, m => { m.SetFloat("_Boost", 1f); m.DisableKeyword("SPRITE_RENDERER_COLOR"); });
            Save("Fragment", Match3VfxMaterials.FragmentShader, null);
            Save("ParticleAlpha", Match3VfxMaterials.ParticleAlphaShader, null);
            AssetDatabase.SaveAssets();
            Debug.Log("[Match3Vfx] Default materials written to " + MaterialFolder);
        }

        static void Save(string name, string shaderName, System.Action<Material> configure)
        {
            var shader = Shader.Find(shaderName);
            if (!shader) throw new FileNotFoundException("Shader not found: " + shaderName);
            string path = MaterialFolder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            configure?.Invoke(material);
            EditorUtility.SetDirty(material);
        }
    }
}
