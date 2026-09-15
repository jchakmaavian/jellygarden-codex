using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RoyalThrone.Match3Vfx.Demo
{
    public static class Match3VfxDemoMenu
    {
        [MenuItem("Tools/Royal Throne/Match-3 VFX Kit/Create Demo Scene", false, 20)]
        public static void CreateDemoScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            var demo = new GameObject("Match-3 VFX Demo").AddComponent<Match3VfxDemo>();

            // Use the art pack automatically when it has been imported next to this sample.
            foreach (string guid in AssetDatabase.FindAssets("t:Match3VfxSettings RoyalThroneArtSettings"))
            {
                demo.settings = AssetDatabase.LoadAssetAtPath<Match3VfxSettings>(AssetDatabase.GUIDToAssetPath(guid));
                break;
            }
            if (demo.settings)
            {
                string folder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(demo.settings)).Replace('\\', '/') + "/Pieces";
                var names = new[] { "block_red", "shield_blue", "crown_yellow", "leaf_green", "crystal_magenta" };
                var sprites = new Sprite[names.Length];
                for (int i = 0; i < names.Length; i++) sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(folder + "/" + names[i] + ".png");
                if (System.Array.TrueForAll(sprites, s => s)) demo.pieceSprites = sprites;
            }

            string path = AssetDatabase.GenerateUniqueAssetPath("Assets/Match3VfxDemo.unity");
            EditorSceneManager.SaveScene(scene, path);
            Selection.activeObject = demo;
            Debug.Log("[Match3Vfx] Demo scene saved to " + path + ". Press Play.");
        }
    }
}
