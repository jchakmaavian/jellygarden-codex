using System.IO;
using System.Text;
using Spine;
using Spine.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalThrone.Characters.EditorTools
{
    /// <summary>
    /// Renders the King Venguard prefab's SkeletonGraphic (official spine-unity 3.8 runtime, the same material, clipping and
    /// multi-page renderers the game uses) at the custom preview player's framing: 1080x1350, 0.5 px per Spine unit, skeleton
    /// root at (540, 1288), transparent background. Runs in an isolated preview scene, so no open scene is touched.
    /// spec: "idle:0,0.5,1.55;happy_thumbs_up:0,1.9" -> PNGs named anim_t.png in outDir.
    /// </summary>
    public static class KingVenguardRuntimeCheck
    {
        public static string Render(string outDir, string spec)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(KingVenguardPrefabBuilder.PrefabPath);
            if (!prefab) return "prefab missing: " + KingVenguardPrefabBuilder.PrefabPath;
            Directory.CreateDirectory(outDir);
            const int W = 1080, H = 1350;
            var scene = EditorSceneManager.NewPreviewScene();
            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            var log = new StringBuilder();
            try
            {
                var camGo = new GameObject("King Venguard check camera", typeof(Camera));
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(camGo, scene);
                var cam = camGo.GetComponent<Camera>();
                cam.scene = scene; cam.orthographic = true; cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0, 0, 0, 0); cam.targetTexture = rt; cam.nearClipPlane = .1f; cam.farClipPlane = 100;
                cam.transform.position = new Vector3(0, 0, -10);

                var canvasGo = new GameObject("King Venguard check canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(canvasGo, scene);
                var canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = cam; canvas.planeDistance = 10;
                var scaler = canvasGo.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize; scaler.scaleFactor = 1; scaler.referencePixelsPerUnit = 100;

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                instance.transform.SetParent(canvasGo.transform, false);
                var graphic = instance.GetComponentInChildren<SkeletonGraphic>();
                graphic.Initialize(false);
                float unitsPerSpine = graphic.SkeletonDataAsset.scale * canvas.referencePixelsPerUnit;
                var grt = graphic.rectTransform;
                grt.anchorMin = grt.anchorMax = grt.pivot = new Vector2(.5f, .5f);
                grt.localScale = new Vector3(.5f / unitsPerSpine, .5f / unitsPerSpine, 1);
                grt.anchoredPosition = new Vector2(0, (H - 1288) - H * .5f);      // root at pixel (540, 1288) from the top
                graphic.AnimationState.ClearTracks();

                var skeleton = graphic.Skeleton;
                var data = graphic.SkeletonDataAsset.GetSkeletonData(false);
                var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
                int frames = 0;
                foreach (var group in spec.Split(';'))
                {
                    var parts = group.Split(':');
                    var animation = data.FindAnimation(parts[0]);
                    if (animation == null) { log.Append("missing animation " + parts[0] + "; "); continue; }
                    foreach (var ts in parts[1].Split(','))
                    {
                        float t = float.Parse(ts, System.Globalization.CultureInfo.InvariantCulture);
                        skeleton.SetToSetupPose();
                        animation.Apply(skeleton, 0, t, false, null, 1, MixBlend.Setup, MixDirection.In);
                        skeleton.UpdateWorldTransform();
                        graphic.UpdateMesh();
                        Canvas.ForceUpdateCanvases();
                        cam.Render();
                        var prev = RenderTexture.active; RenderTexture.active = rt;
                        tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply();
                        RenderTexture.active = prev;
                        File.WriteAllBytes(Path.Combine(outDir, parts[0] + "_" + t.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + ".png"), tex.EncodeToPNG());
                        frames++;
                    }
                }
                Object.DestroyImmediate(tex);
                log.Append("rendered " + frames + " frames; submesh renderers " + graphic.transform.childCount + "; material " + graphic.material.name);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
                rt.Release(); Object.DestroyImmediate(rt);
            }
            return log.ToString();
        }
    }
}
