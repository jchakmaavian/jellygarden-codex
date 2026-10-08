using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RoyalThrone.ReferenceGameplay.Preview
{
    public static class ReferenceTntPreviewMenu
    {
        public const string ScenePath = "Assets/ReferenceGameplay/Preview/ReferenceTntPreview.unity";

        [MenuItem("Tools/Royal Throne/TNT (original)/Open Preview Scene")]
        public static void Open()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath)) EditorSceneManager.OpenScene(ScenePath);
            else Build();
        }

        [MenuItem("Tools/Royal Throne/TNT (original)/Play Preview")]
        public static void Play()
        {
            Open();
            EditorApplication.isPlaying = true;
        }

        // Board-coloured backdrop, a 5x5 cell grid (the single TNT's blast area) and the player.
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            camera.tag = "MainCamera";
            // Fits the 5x5 cells plus the combo blast on a 9:16 portrait screen.
            camera.orthographic = true; camera.orthographicSize = 8;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.11f, .16f, .30f);
            camera.transform.position = new Vector3(0, 0, -10);

            var player = new GameObject("TNT Preview").AddComponent<ReferenceTntPreview>();
            float cell = player.cell;
            var grid = new GameObject("Board cells (5x5)").transform;
            var tile = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            for (int y = -2; y <= 2; y++)
                for (int x = -2; x <= 2; x++)
                {
                    var r = new GameObject($"Cell {x},{y}", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                    r.transform.SetParent(grid, false);
                    r.transform.localPosition = new Vector3(x * cell, y * cell, 0);
                    r.sprite = tile; r.drawMode = SpriteDrawMode.Sliced; r.size = Vector2.one * cell * .94f;
                    r.color = (x + y & 1) == 0 ? new Color(.24f, .38f, .66f) : new Color(.20f, .33f, .60f);
                    r.sortingOrder = -10;
                }
            EditorSceneManager.SaveScene(scene, ScenePath);
            Selection.activeGameObject = player.gameObject;
        }
    }
}
