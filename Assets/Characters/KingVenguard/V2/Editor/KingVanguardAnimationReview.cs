#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using RoyalThrone.Characters.Activities;
using Spine;
using Spine.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Event = UnityEngine.Event;

namespace RoyalThrone.Characters.EditorTools
{
    /// <summary>Deterministic inspection of recovered V2 assets in a private preview scene.</summary>
    public sealed class KingVanguardAnimationReview : EditorWindow
    {
        const string SpineRoot = "Assets/Characters/KingVenguard/V2/Spine/";
        const string NativePrefab = "Assets/Characters/KingVenguard/V2/Activities/KingDrill/Resources/KingVanguard/KingVanguardDrill.prefab";
        const string DefaultOutput = "Design/KingVanguard-Reference-20260929/AnimationReview";
        static readonly string[] Rigs = { "King_Vanguard", "AreaCompleted_Vanguard", "Nightmare_Vanguard", "Native KingDrill" };
        static readonly HashSet<string> PortraitDecoration = new HashSet<string> { "Frame Background3", "sol frame", "sag frame" };
        static readonly HashSet<string> AreaDecoration = new HashSet<string> {
            "ipad_background", "background", "sol", "borazan_mask", "borazan", "borazan2", "borazan3", "borazan4",
            "new_borazan_golge", "new_borazan_golge2", "new_borazan_golge3", "new_borazan_golge4", "flama_mask_1",
            "new_flama_golge", "new_flama_kose", "new_flama_kose2", "new_flama_kivrik", "new_flama_kivrik2",
            "new_flama_curve", "new_flama_rulo", "Group 1 copy"
        };
        Preview preview;
        int rigIndex, animationIndex;
        float time, speed = 1;
        bool playing, includeScenery;
        double lastTick;
        Bounds clipBounds;
        string error;

        [MenuItem("Royal Throne/King Vanguard/Animation Review")]
        public static void Open() { GetWindow<KingVanguardAnimationReview>("King Vanguard Review").Show(); }

        void OnEnable() { EditorApplication.update += Tick; lastTick = EditorApplication.timeSinceStartup; }
        void OnDisable() { EditorApplication.update -= Tick; DisposePreview(); }
        void DisposePreview() { preview?.Dispose(); preview = null; }
        void Rebuild()
        {
            DisposePreview(); error = null;
            try
            {
                preview = new Preview(rigIndex, includeScenery);
                animationIndex = Mathf.Clamp(animationIndex, 0, preview.Clips.Count - 1);
                if (preview.Clips.Count == 0) throw new InvalidOperationException("Rig has no imported animations.");
                time = Mathf.Clamp(time, 0, preview.Clips[animationIndex].Duration);
                clipBounds = preview.MeasureClip(preview.Clips[animationIndex], 13);
                preview.Sample(preview.Clips[animationIndex], time);
            }
            catch (Exception e) { error = e.Message; DisposePreview(); }
        }
        void Tick()
        {
            double now = EditorApplication.timeSinceStartup;
            float dt = Mathf.Min(.1f, (float)(now - lastTick)); lastTick = now;
            if (!playing || preview == null || preview.Clips.Count == 0) return;
            var clip = preview.Clips[animationIndex];
            time = clip.Duration > 0 ? Mathf.Repeat(time + dt * speed, clip.Duration) : 0;
            Repaint();
        }
        void OnGUI()
        {
            EditorGUILayout.LabelField("Recovered rfv387 animation review", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            rigIndex = EditorGUILayout.Popup("Character structure", rigIndex, Rigs);
            includeScenery = EditorGUILayout.Toggle("Include source scenery", includeScenery);
            if (EditorGUI.EndChangeCheck()) { animationIndex = 0; time = 0; Rebuild(); }
            if (preview == null && error == null) Rebuild();
            if (error != null)
            {
                EditorGUILayout.HelpBox(error, MessageType.Warning);
                if (GUILayout.Button("Reload imported assets")) Rebuild();
                return;
            }
            if (preview == null) return;
            EditorGUI.BeginChangeCheck();
            animationIndex = EditorGUILayout.Popup("Animation", animationIndex, preview.Clips.Select(c => c.Name).ToArray());
            if (EditorGUI.EndChangeCheck()) { time = 0; Rebuild(); }
            if (preview == null) return;
            var clip = preview.Clips[animationIndex];
            using (new EditorGUILayout.HorizontalScope())
            {
                playing = GUILayout.Toggle(playing, playing ? "Pause" : "Play", "Button", GUILayout.Width(65));
                if (GUILayout.Button("Start", GUILayout.Width(55))) time = 0;
                speed = EditorGUILayout.Slider("Speed", speed, .1f, 2f);
            }
            time = EditorGUILayout.Slider("Time (seconds)", time, 0, Mathf.Max(0, clip.Duration));
            EditorGUILayout.LabelField($"{preview.Clips.Count} tracks  |  {clip.Duration:0.###} seconds  |  private preview scene");
            if (clip.Duration == 0) EditorGUILayout.HelpBox("This is an original empty track. The setup pose is shown.", MessageType.Info);
            string inspectionNote = InspectionNote(rigIndex, clip.Name);
            if (inspectionNote != null) EditorGUILayout.HelpBox(inspectionNote, MessageType.Info);
            Rect area = GUILayoutUtility.GetRect(200, 200, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            if (Event.current.type == EventType.Repaint && area.width > 1 && area.height > 1)
            {
                try
                {
                    preview.Sample(clip, time);
                    var target = preview.Render(Mathf.Clamp((int)area.width, 64, 1600), Mathf.Clamp((int)area.height, 64, 1600), clipBounds, null);
                    GUI.DrawTexture(area, target, ScaleMode.ScaleToFit, false);
                }
                catch (Exception e) { error = e.Message; }
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Reload assets")) Rebuild();
                if (GUILayout.Button("Render all contact sheets"))
                {
                    string output = EditorUtility.OpenFolderPanel("Contact sheet output", Path.GetFullPath(DefaultOutput), "");
                    if (!string.IsNullOrEmpty(output)) Debug.Log(RenderContactSheets(output));
                }
            }
            EditorGUILayout.HelpBox("The viewer samples the original tracks directly. Scenery can be hidden without changing source data. Nightmare retains its book, dog and bedding; its background is optional.", MessageType.None);
        }

        /// <summary>Callable from an editor command. Captures every nonzero Spine track and, if available, all six native clips.</summary>
        public static string RenderContactSheets(string outputDir)
        {
            if (string.IsNullOrWhiteSpace(outputDir)) throw new ArgumentException("An output directory is required.");
            outputDir = Path.GetFullPath(outputDir); Directory.CreateDirectory(outputDir);
            const int cellW = 340, cellH = 380, rowsPerPage = 8;
            float[] phases = { 0, .33f, .66f, .95f };
            var report = new StringBuilder("Recovered King animation contact sheets\nSamples: 0%, 33%, 66%, 95%; framing is fixed across each clip.\n");
            var manifest = new ReviewManifest { createdUtc = DateTime.UtcNow.ToString("o"), includeScenery = false };
            for (int rig = 0; rig < Rigs.Length; rig++)
            {
                Preview instance = null;
                try
                {
                    instance = new Preview(rig, false);
                    var clips = instance.Clips.Where(c => c.Duration > 0).ToArray();
                    report.AppendLine(Rigs[rig] + ": " + clips.Length + " nonzero tracks; " + (instance.Clips.Count - clips.Length) + " zero-duration tracks skipped.");
                    for (int page = 0; page * rowsPerPage < clips.Length; page++)
                    {
                        int count = Mathf.Min(rowsPerPage, clips.Length - page * rowsPerPage);
                        var sheet = new Texture2D(cellW * 4, cellH * count, TextureFormat.RGBA32, false);
                        try
                        {
                            for (int row = 0; row < count; row++)
                            {
                                var clip = clips[page * rowsPerPage + row];
                                // Native clips may not key every channel. Rebuild for a pristine prefab pose between clips.
                                if (rig == 3) { instance.Dispose(); instance = new Preview(rig, false); }
                                Bounds bounds = instance.MeasureClip(clip, 13);
                                var entry = new ReviewEntry { rig = Rigs[rig], animation = clip.Name, duration = clip.Duration, page = page + 1, row = row + 1, boundsCenter = bounds.center, boundsSize = bounds.size, inspectionNote = InspectionNote(rig, clip.Name) };
                                if (entry.inspectionNote != null) report.AppendLine("  " + clip.Name + ": " + entry.inspectionNote);
                                for (int col = 0; col < phases.Length; col++)
                                {
                                    float sampleTime = clip.Duration * phases[col];
                                    instance.Sample(clip, sampleTime);
                                    string label = clip.Name + "\n" + sampleTime.ToString("0.000", CultureInfo.InvariantCulture) + " / " + clip.Duration.ToString("0.000", CultureInfo.InvariantCulture) + " s  [" + (phases[col] * 100).ToString("0") + "%]";
                                    var rendered = instance.Capture(cellW, cellH, bounds, label);
                                    try { sheet.SetPixels32(col * cellW, (count - row - 1) * cellH, cellW, cellH, rendered.GetPixels32()); }
                                    finally { Object.DestroyImmediate(rendered); }
                                    entry.samples.Add(sampleTime);
                                }
                                manifest.animations.Add(entry);
                            }
                            sheet.Apply(false, false);
                            string name = SafeName(Rigs[rig]) + "-page-" + (page + 1).ToString("00") + ".png";
                            File.WriteAllBytes(Path.Combine(outputDir, name), sheet.EncodeToPNG());
                            report.AppendLine("  " + name);
                        }
                        finally { Object.DestroyImmediate(sheet); }
                    }
                }
                catch (Exception e) { string failure = Rigs[rig] + ": FAILED - " + e; report.AppendLine(failure); manifest.errors.Add(failure); }
                finally { instance?.Dispose(); }
            }
            File.WriteAllText(Path.Combine(outputDir, "contact-sheet-report.txt"), report.ToString());
            File.WriteAllText(Path.Combine(outputDir, "contact-sheet-manifest.json"), JsonUtility.ToJson(manifest, true));
            return report.ToString();
        }
        static string SafeName(string value) { return string.Concat(value.Select(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_')); }
        static string InspectionNote(int rig, string animation)
        {
            if (rig != 1) return null;
            if (animation == "king_area 1" || animation == "king_area_2")
                return "This original track leaves legacy arm variants active in its setup pose. The review preserves that source result. Use the complete drill/clap loops for a standalone character; the original game's initialization for these older variants is unverified.";
            if (animation == "king_drill_win_clap_start" || animation == "king_drill_win_clap_finish")
                return "The source body enters/exits vertically by about 1,535 source units. The fixed review camera includes that full trajectory. A persistent UI should fit the stable clap-loop pose and treat this as an entrance/exit.";
            if (animation == "king_area_3" || animation == "king_area_4" || animation == "king_drill_win")
                return "This source clip contains a large horizontal body trajectory. The review camera includes the complete motion; it does not recenter the character each frame.";
            return null;
        }

        [Serializable] sealed class ReviewManifest { public string createdUtc; public bool includeScenery; public List<ReviewEntry> animations = new List<ReviewEntry>(); public List<string> errors = new List<string>(); }
        [Serializable] sealed class ReviewEntry { public string rig, animation, inspectionNote; public float duration; public int page, row; public Vector3 boundsCenter, boundsSize; public List<float> samples = new List<float>(); }
        sealed class Clip { public string Name; public float Duration; public Spine.Animation SpineAnimation; }

        sealed class Preview : IDisposable
        {
            readonly Scene scene;
            readonly Camera camera;
            readonly int rig;
            readonly bool scenery;
            GameObject actor, labelCanvas;
            SkeletonAnimation spine;
            NativeKingDrillActivity native;
            RenderTexture target;
            Text caption;
            Mesh bakedMesh;
            public readonly List<Clip> Clips = new List<Clip>();

            public Preview(int rigIndex, bool includeScenery)
            {
                rig = rigIndex; scenery = includeScenery; scene = EditorSceneManager.NewPreviewScene();
                try
                {
                    var camGo = new GameObject("Vanguard review camera", typeof(Camera)); SceneManager.MoveGameObjectToScene(camGo, scene);
                    camera = camGo.GetComponent<Camera>(); camera.scene = scene; camera.orthographic = true;
                    camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.10f, .14f, .22f, 1);
                    camera.nearClipPlane = .01f; camera.farClipPlane = 1000; camera.enabled = false;
                    if (rig < 3)
                    {
                        string path = SpineRoot + Rigs[rig] + "_SkeletonData.asset";
                        var data = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(path);
                        if (!data) throw new FileNotFoundException("Imported skeleton is missing: " + path);
                        actor = new GameObject(Rigs[rig]); SceneManager.MoveGameObjectToScene(actor, scene);
                        spine = actor.AddComponent<SkeletonAnimation>(); spine.skeletonDataAsset = data; spine.Initialize(false); spine.enabled = false;
                        if (rig < 2) actor.AddComponent<KingVanguardFaceRig>();
                        if (spine.Skeleton == null) throw new InvalidOperationException("Could not initialize " + path);
                        foreach (var animation in spine.Skeleton.Data.Animations) Clips.Add(new Clip { Name = animation.Name, Duration = animation.Duration, SpineAnimation = animation });
                    }
                    else
                    {
                        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(NativePrefab);
                        if (!prefab) throw new FileNotFoundException("Native KingDrill prefab is not imported yet: " + NativePrefab);
                        actor = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene); native = actor.GetComponentInChildren<NativeKingDrillActivity>(true);
                        if (!native) throw new InvalidOperationException("Native KingDrill prefab is missing its activity component.");
                        foreach (var clip in native.Clips) if (clip) Clips.Add(new Clip { Name = clip.name, Duration = clip.length });
                        bakedMesh = new Mesh();
                    }
                }
                catch { Dispose(); throw; }
            }
            public void Sample(Clip clip, float sampleTime)
            {
                if (spine)
                {
                    spine.Skeleton.SetToSetupPose();
                    clip.SpineAnimation.Apply(spine.Skeleton, 0, Mathf.Clamp(sampleTime, 0, clip.Duration), false, null, 1, MixBlend.Setup, MixDirection.In);
                    if (!scenery)
                        foreach (var slot in spine.Skeleton.Slots)
                            if ((rig == 0 && PortraitDecoration.Contains(slot.Data.Name)) || (rig == 1 && AreaDecoration.Contains(slot.Data.Name)) || (rig == 2 && slot.Data.Name == "00100_Z_bg")) slot.Attachment = null;
                    spine.Skeleton.UpdateWorldTransform(); spine.LateUpdate();
                }
                else if (!native.Sample(clip.Name, sampleTime)) throw new InvalidOperationException("Cannot sample native clip: " + clip.Name);
            }
            public Bounds MeasureClip(Clip clip, int samples)
            {
                Bounds result = default; bool any = false;
                for (int i = 0; i < samples; i++)
                {
                    Sample(clip, clip.Duration * i / Mathf.Max(1, samples - 1));
                    Bounds bounds = CurrentBounds();
                    if (!any) { result = bounds; any = true; } else result.Encapsulate(bounds);
                }
                return result;
            }
            Bounds CurrentBounds()
            {
                if (spine)
                {
                    // Face proportions are applied after native skinning. Measure the
                    // emitted mesh so the review camera includes those final vertices.
                    var faceRig = spine.GetComponent<KingVanguardFaceRig>();
                    var filter = spine.GetComponent<MeshFilter>();
                    if (faceRig && faceRig.IsRegistered && filter && filter.sharedMesh)
                    {
                        var renderedVertices = filter.sharedMesh.vertices;
                        var colors = filter.sharedMesh.colors32;
                        bool found = false;
                        Bounds mapped = default;
                        for (int i = 0; i < renderedVertices.Length; i++)
                        {
                            if (colors.Length == renderedVertices.Length && colors[i].a == 0) continue;
                            if (!found) { mapped = new Bounds(renderedVertices[i], Vector3.zero); found = true; }
                            else mapped.Encapsulate(renderedVertices[i]);
                        }
                        if (found) return mapped;
                    }
                    // Skeleton.GetBounds includes alpha-zero attachment variants. Ignore these for useful visual framing.
                    float minX = float.PositiveInfinity, minY = float.PositiveInfinity, maxX = float.NegativeInfinity, maxY = float.NegativeInfinity;
                    float[] vertices = new float[8];
                    foreach (var slot in spine.Skeleton.DrawOrder)
                    {
                        if (!slot.Bone.Active || slot.A <= .001f) continue;
                        int length;
                        var region = slot.Attachment as RegionAttachment;
                        var mesh = slot.Attachment as MeshAttachment;
                        if (region != null && region.A > .001f) { length = 8; region.ComputeWorldVertices(slot.Bone, vertices, 0); }
                        else if (mesh != null && mesh.A > .001f)
                        {
                            length = mesh.WorldVerticesLength;
                            if (vertices.Length < length) vertices = new float[length];
                            mesh.ComputeWorldVertices(slot, 0, length, vertices, 0);
                        }
                        else continue;
                        for (int i = 0; i < length; i += 2) { minX = Mathf.Min(minX, vertices[i]); maxX = Mathf.Max(maxX, vertices[i]); minY = Mathf.Min(minY, vertices[i + 1]); maxY = Mathf.Max(maxY, vertices[i + 1]); }
                    }
                    float x = minX, y = minY, w = maxX - minX, h = maxY - minY;
                    if (!Finite(x) || !Finite(y) || !Finite(w) || !Finite(h) || w <= 0 || h <= 0) throw new InvalidOperationException("Invalid rendered bounds for " + Rigs[rig]);
                    return new Bounds(new Vector3(x + w * .5f, y + h * .5f, 0), new Vector3(w, h, .01f));
                }
                bool any = false; Bounds result = default;
                foreach (var part in native.Parts)
                {
                    if (!part || !part.Output || !part.Output.enabled || !part.Output.gameObject.activeInHierarchy) continue;
                    Renderer renderer = part.Output;
                    var skin = renderer as SkinnedMeshRenderer;
                    Bounds bounds;
                    if (skin && skin.sharedMesh)
                    {
                        skin.BakeMesh(bakedMesh); bounds = TransformBounds(skin.transform, bakedMesh.bounds);
                    }
                    else bounds = renderer.bounds;
                    if (!any) { result = bounds; any = true; } else result.Encapsulate(bounds);
                }
                if (!any || result.size.x <= 0 || result.size.y <= 0) throw new InvalidOperationException("Native KingDrill has no visible mesh at this sample.");
                return result;
            }
            static Bounds TransformBounds(Transform transform, Bounds local)
            {
                Vector3 min = local.min, max = local.max; var result = new Bounds(transform.TransformPoint(min), Vector3.zero);
                for (int i = 0; i < 8; i++) result.Encapsulate(transform.TransformPoint(new Vector3((i & 1) == 0 ? min.x : max.x, (i & 2) == 0 ? min.y : max.y, (i & 4) == 0 ? min.z : max.z)));
                return result;
            }
            static bool Finite(float n) { return !float.IsNaN(n) && !float.IsInfinity(n); }
            public RenderTexture Render(int width, int height, Bounds bounds, string label)
            {
                if (!target || target.width != width || target.height != height)
                {
                    if (target) { camera.targetTexture = null; target.Release(); Object.DestroyImmediate(target); }
                    target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32); target.Create(); camera.targetTexture = target;
                }
                const float labelHeight = 54;
                float usableHeight = label == null ? height : Mathf.Max(1, height - labelHeight);
                float halfHeight = Mathf.Max(bounds.size.y * .5f, bounds.size.x * .5f * usableHeight / width) * 1.12f;
                camera.orthographicSize = Mathf.Max(.01f, halfHeight * height / usableHeight);
                camera.aspect = (float)width / height;
                float offset = label == null ? 0 : camera.orthographicSize * labelHeight / height;
                camera.transform.position = new Vector3(bounds.center.x, bounds.center.y - offset, bounds.center.z - 100);
                if (label != null) { EnsureCaption(); labelCanvas.SetActive(true); caption.text = label; Canvas.ForceUpdateCanvases(); }
                else if (labelCanvas) labelCanvas.SetActive(false);
                camera.Render(); return target;
            }
            void EnsureCaption()
            {
                if (caption) return;
                labelCanvas = new GameObject("Contact sheet caption", typeof(RectTransform), typeof(Canvas)); SceneManager.MoveGameObjectToScene(labelCanvas, scene);
                var canvas = labelCanvas.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1; canvas.sortingOrder = 32760;
                var panel = new GameObject("Caption background", typeof(RectTransform), typeof(Image)); panel.transform.SetParent(labelCanvas.transform, false);
                var rt = panel.GetComponent<RectTransform>(); rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(1, 0); rt.pivot = new Vector2(.5f, 0); rt.sizeDelta = new Vector2(0, 54);
                panel.GetComponent<Image>().color = new Color(.035f, .05f, .09f, 1);
                var textGo = new GameObject("Caption", typeof(RectTransform), typeof(Text)); textGo.transform.SetParent(panel.transform, false);
                rt = textGo.GetComponent<RectTransform>(); rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = new Vector2(8, 3); rt.offsetMax = new Vector2(-8, -3);
                caption = textGo.GetComponent<Text>(); caption.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); caption.fontSize = 14; caption.color = new Color(.94f, .96f, 1); caption.alignment = TextAnchor.MiddleLeft;
                caption.horizontalOverflow = HorizontalWrapMode.Wrap; caption.verticalOverflow = VerticalWrapMode.Overflow;
            }
            public Texture2D Capture(int width, int height, Bounds bounds, string label)
            {
                Render(width, height, bounds, label); var previous = RenderTexture.active;
                var image = new Texture2D(width, height, TextureFormat.RGBA32, false);
                try { RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply(false, false); return image; }
                catch { Object.DestroyImmediate(image); throw; }
                finally { RenderTexture.active = previous; }
            }
            public void Dispose()
            {
                if (camera) camera.targetTexture = null;
                if (target) { target.Release(); Object.DestroyImmediate(target); target = null; }
                if (bakedMesh) Object.DestroyImmediate(bakedMesh);
                if (scene.IsValid() && scene.isLoaded) EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
#endif
