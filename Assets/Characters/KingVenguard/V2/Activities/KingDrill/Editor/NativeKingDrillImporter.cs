#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;

namespace RoyalThrone.Characters.Activities.Editor
{
    /// <summary>Rebuilds the native KingDrill hierarchy and meshes from the exact APK export. No package changes.</summary>
    public static class NativeKingDrillImporter
    {
        public const string Base = "Assets/Characters/KingVenguard/V2/Activities/KingDrill/";
        public const string DefaultTexturePath = Base + "Textures/KingDrill_Vanguard.png";
        public const string PrefabPath = Base + "Resources/KingVanguard/KingVanguardDrill.prefab";
        public const string DataPath = "Design/KingVanguard-Reference-20260929/Logic/KingDrill/UnityImportData.json";
        const string OriginalAtlas = "Design/KingVanguard-Reference-20260929/EmbeddedKingBundles/Recovered/kingdrill_v3/Textures/sactx-0-1024x2048-ETC2-KingDrillAtlas-89d7723c__4934485327719105542.png";
        const string ReportPath = "Design/KingVanguard-Reference-20260929/Logic/KingDrill/unity-import-report.txt";
        const string FaceShapeReportPath = "Design/KingVanguard-Reference-20260929/Logic/KingDrill/native-face-shape-validation.json";

        [MenuItem("Royal Throne/Characters/Import Native KingDrill Activity")]
        public static void Build() { BuildWithTexture(DefaultTexturePath); }

        /// <param name="texturePath">An atlas under Assets, same native 1024x2048 packing; may contain redesigned art.</param>
        public static void BuildWithTexture(string texturePath)
        {
            if (string.IsNullOrEmpty(texturePath) || !texturePath.Replace('\\', '/').StartsWith("Assets/", StringComparison.Ordinal))
                throw new ArgumentException("KingDrill texture path must be under this project's Assets folder.");
            texturePath = texturePath.Replace('\\', '/');
            if (!File.Exists(DataPath)) throw new FileNotFoundException("Run prepare_unity_import.py first.", DataPath);
            var data = JsonUtility.FromJson<ImportData>(File.ReadAllText(DataPath));
            // Derive setup matrices from unchanged source hierarchy before any new meshes are saved.
            var setupWorld = new Dictionary<long, Matrix4x4>();
            foreach (var node in data.nodes)
            {
                var local = Matrix4x4.TRS(node.position, node.rotation, node.scale);
                setupWorld[node.id] = node.parentId == 0 ? local : setupWorld[node.parentId] * local;
            }
            var headNode = data.nodes.Single(n => n.path == NativeKingDrillFaceShape.HeadPath);
            Vector3 facePivot = setupWorld[headNode.id].MultiplyPoint3x4(Vector3.zero);
            var faceNodes = data.nodes.Where(n => n.hasRenderer && NativeKingDrillFaceShape.IsFacialPath(n.path)).ToArray();
            if (faceNodes.Length != 13 || faceNodes.Select(n => n.renderer.spriteId).Distinct().Count() != 13)
                throw new InvalidDataException("Native KingDrill face adaptation expects all 13 distinct facial source meshes.");
            var faceBySprite = faceNodes.ToDictionary(n => n.renderer.spriteId);
            var faceValidation = new NativeKingDrillFaceShape.ValidationSet();
            foreach (var dir in new[] { "Meshes", "Sprites", "Animations", "Materials", "Resources/KingVanguard", "Textures" })
                Directory.CreateDirectory(Base + dir);
            bool copiedOriginal = false;
            if (!File.Exists(texturePath))
            {
                if (texturePath != DefaultTexturePath) throw new FileNotFoundException("Redesigned atlas does not exist.", texturePath);
                Directory.CreateDirectory(Path.GetDirectoryName(texturePath));
                File.Copy(OriginalAtlas, texturePath); copiedOriginal = true;
            }
            AssetDatabase.Refresh();
            var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
            importer.textureType = TextureImporterType.Default;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 4096;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (!texture || texture.width != data.atlasWidth || texture.height != data.atlasHeight)
                throw new InvalidDataException("Atlas dimensions must stay 1024x2048 to preserve native sprite UVs.");
            var shader = Shader.Find("Sprites/Default");
            if (!shader) throw new InvalidOperationException("Sprites/Default shader unavailable.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(Base + "Materials/KingDrill.mat");
            if (!material) { material = new Material(shader); AssetDatabase.CreateAsset(material, Base + "Materials/KingDrill.mat"); }
            material.shader = shader; material.mainTexture = texture; material.color = Color.white;
            EditorUtility.SetDirty(material);

            var meshes = new Dictionary<long, Mesh>();
            var sprites = new Dictionary<long, Sprite>();
            foreach (var record in data.sprites)
            {
                if (record.vertices.Length != record.uv.Length || record.triangles.Any(i => i < 0 || i >= record.vertices.Length))
                    throw new InvalidDataException("Invalid native mesh: " + record.name);
                var mesh = new Mesh { name = record.name + "_NativeMesh" };
                mesh.vertices = record.vertices; mesh.uv = record.uv; mesh.triangles = record.triangles;
                mesh.colors = Enumerable.Repeat(Color.white, record.vertices.Length).ToArray();
                if (record.weights != null && record.weights.Length > 0)
                {
                    if (record.weights.Length != record.vertices.Length) throw new InvalidDataException("Native skin weight count mismatch.");
                    mesh.boneWeights = record.weights.Select(w => w.Weight()).ToArray();
                    mesh.bindposes = record.bindPoses.Select(p => p.Matrix()).ToArray();
                }
                if (faceBySprite.TryGetValue(record.id, out var faceNode))
                {
                    var boneWorld = faceNode.renderer.bones.Select(id => setupWorld[id]).ToArray();
                    faceValidation.meshes.Add(NativeKingDrillFaceShape.Apply(mesh, setupWorld[faceNode.id], boneWorld, facePivot, faceNode.path));
                }
                mesh.RecalculateBounds();
                meshes[record.id] = SaveObject(mesh, Base + "Meshes/" + record.id + ".asset");
                // These proxies are native AnimationClip object-reference targets. The recovered mesh uses the actual UVs.
                var rect = new Rect(record.textureRect.x, record.textureRect.y, record.textureRect.width, record.textureRect.height);
                var sprite = Sprite.Create(texture, rect, record.pivot, record.pixelsPerUnit, 0, SpriteMeshType.FullRect);
                sprite.name = record.name;
                sprites[record.id] = SaveObject(sprite, Base + "Sprites/" + record.id + ".asset");
            }
            if (faceValidation.meshes.Count != 13) throw new InvalidDataException("Not every facial mesh received the V2 face shape.");
            File.WriteAllText(FaceShapeReportPath, JsonUtility.ToJson(faceValidation, true));

            var clips = new Dictionary<string, AnimationClip>();
            int appliedCurves = 0, unresolvedCurves = 0, objectCurves = 0;
            foreach (var record in data.clips)
            {
                var clip = new AnimationClip { name = record.name, frameRate = record.frameRate };
                foreach (var curve in record.curves)
                {
                    if (curve.path.StartsWith("__UNRESOLVED_HASH_", StringComparison.Ordinal)) { unresolvedCurves++; continue; }
                    Type type = curve.typeID == 4 ? typeof(Transform) : curve.typeID == 212 ? typeof(SpriteRenderer) : curve.typeID == 1 ? typeof(GameObject) : null;
                    if (type == null) throw new InvalidDataException("Unsupported native binding type " + curve.typeID);
                    if (curve.isPPtr)
                    {
                        var keys = curve.keys.Select(k => new ObjectReferenceKeyframe { time = k.time, value = k.objectPathId == 0 ? null : sprites[k.objectPathId] }).ToArray();
                        AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve(curve.path, type, curve.property), keys);
                        objectCurves++;
                    }
                    else
                    {
                        var keys = curve.keys.Select(k => new Keyframe(k.time, k.value, k.inSlope, k.outSlope)).ToArray();
                        var animationCurve = new AnimationCurve(keys);
                        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(curve.path, type, curve.property), animationCurve);
                    }
                    appliedCurves++;
                }
                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.startTime = 0; settings.stopTime = record.duration; settings.loopTime = record.loop;
                AnimationUtility.SetAnimationClipSettings(clip, settings);
                clips[record.name] = SaveObject(clip, Base + "Animations/" + record.name + ".anim");
            }
            string controllerPath = Base + "Animations/KingDrill.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (!controller) controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var machine = controller.layers[0].stateMachine;
            foreach (var state in machine.states) machine.RemoveState(state.state);
            var states = new Dictionary<string, AnimatorState>();
            foreach (var record in data.clips)
            {
                var state = machine.AddState(record.name);
                state.motion = clips[record.name]; state.speed = 1f; state.writeDefaultValues = true;
                states[record.name] = state;
            }
            machine.defaultState = states[NativeKingDrillActivity.IdleClip];
            var transition = states[NativeKingDrillActivity.WalkClip].AddTransition(states[NativeKingDrillActivity.AlternateIdleClip]);
            transition.hasExitTime = true; transition.exitTime = 1f; transition.hasFixedDuration = true;
            transition.duration = 0f; transition.offset = 0f; transition.interruptionSource = TransitionInterruptionSource.None;
            transition.orderedInterruption = true; transition.canTransitionToSelf = true;
            EditorUtility.SetDirty(controller);

            var outer = new GameObject("KingVanguardDrill");
            try
            {
                var transforms = new Dictionary<long, Transform>();
                foreach (var node in data.nodes)
                {
                    var go = new GameObject(node.name);
                    var tr = go.transform; tr.SetParent(node.parentId == 0 ? outer.transform : transforms[node.parentId], false);
                    tr.localPosition = node.position; tr.localRotation = node.rotation; tr.localScale = node.scale;
                    transforms[node.id] = tr;
                }
                var allSprites = data.sprites.Select(s => sprites[s.id]).ToArray();
                var allMeshes = data.sprites.Select(s => meshes[s.id]).ToArray();
                var parts = new List<NativeKingDrillPart>();
                foreach (var node in data.nodes)
                {
                    if (!node.hasRenderer || node.renderer == null) continue;
                    var r = node.renderer; var tr = transforms[node.id];
                    var proxy = tr.gameObject.AddComponent<SpriteRenderer>();
                    proxy.sprite = r.spriteId == 0 ? null : sprites[r.spriteId];
                    proxy.enabled = r.enabled; proxy.color = r.color; proxy.flipX = r.flipX; proxy.flipY = r.flipY;
                    proxy.sortingLayerID = r.sortingLayerId; proxy.sortingOrder = r.sortingOrder; proxy.forceRenderingOff = true;
                    var meshGo = new GameObject("__NativeMesh"); meshGo.transform.SetParent(tr, false);
                    Renderer output;
                    if (r.bones != null && r.bones.Length > 0)
                    {
                        var skin = meshGo.AddComponent<SkinnedMeshRenderer>();
                        skin.bones = r.bones.Select(id => transforms[id]).ToArray();
                        skin.rootBone = transforms[r.rootBone]; skin.quality = SkinQuality.Bone4;
                        skin.updateWhenOffscreen = true; skin.skinnedMotionVectors = false;
                        skin.localBounds = new Bounds(Vector3.zero, new Vector3(40, 40, 4));
                        output = skin;
                    }
                    else { meshGo.AddComponent<MeshFilter>(); output = meshGo.AddComponent<MeshRenderer>(); }
                    output.sharedMaterial = material; output.shadowCastingMode = ShadowCastingMode.Off;
                    output.receiveShadows = false; output.lightProbeUsage = LightProbeUsage.Off; output.reflectionProbeUsage = ReflectionProbeUsage.Off;
                    // The original room's tall brown wall-shadow is not a standalone character part. Preserve its
                    // native sprite, geometry and alpha animation, but suppress its output outside the source room.
                    bool sceneOverlay = node.path == "NewKing/00190_body_G";
                    var part = tr.gameObject.AddComponent<NativeKingDrillPart>(); part.Configure(proxy, output, allSprites, allMeshes, sceneOverlay); parts.Add(part);
                }
                var animator = transforms[data.rootId].gameObject.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller; animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var activity = outer.AddComponent<NativeKingDrillActivity>();
                activity.Configure(animator, data.clips.Select(c => clips[c.name]).ToArray(), parts.ToArray());
                foreach (var node in data.nodes) transforms[node.id].gameObject.SetActive(node.active);
                PrefabUtility.SaveAsPrefabAsset(outer, PrefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(outer); }
            AssetDatabase.SaveAssets();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (!prefab || prefab.GetComponentsInChildren<NativeKingDrillPart>(true).Length != 28)
                throw new InvalidDataException("Native KingDrill prefab has incomplete renderer parts.");
            File.WriteAllText(ReportPath,
                "Native KingDrill import completed; runtime/visual QA remains required.\n" +
                "Atlas: " + texturePath + (copiedOriginal ? " (original copied for initial preview)" : "") + "\n" +
                "Transforms: " + data.nodes.Length + "; sprites: " + sprites.Count + "; skinned parts: 11\n" +
                "Vanguard face shape: all 13 facial meshes; common source-head pivot; setup-world x1.15 y0.92, then both pupils x1.55 y1.55 around their individual post-affine centroids; skin bindings/clips unchanged.\n" +
                "Standalone adaptation: NewKing/00190_body_G scene wall-shadow output hidden; source sprite, mesh and curves preserved.\n" +
                "Applied curves: " + appliedCurves + "; object-reference curves: " + objectCurves + "; original unmatched curves retained in source and unapplied: " + unresolvedCurves + "\n" +
                string.Join("\n", data.clips.Select(c => c.name + ": " + c.duration + "s; " + c.frameRate + "fps; loop=" + c.loop)) + "\n");
            Debug.Log("Native KingDrill imported: " + PrefabPath);
        }

        static T SaveObject<T>(T value, string path) where T : UnityEngine.Object
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (!existing) { AssetDatabase.CreateAsset(value, path); return value; }
            EditorUtility.CopySerialized(value, existing); EditorUtility.SetDirty(existing);
            UnityEngine.Object.DestroyImmediate(value); return existing;
        }

        [Serializable] class ImportData { public int atlasWidth, atlasHeight; public long rootId; public Node[] nodes; public SpriteData[] sprites; public ClipData[] clips; }
        [Serializable] class Node { public long id, parentId; public string name, path; public bool active, hasRenderer; public Vector3 position, scale; public Quaternion rotation; public RendererData renderer; }
        [Serializable] class RendererData { public long spriteId, rootBone; public long[] bones; public bool enabled, flipX, flipY; public Color color; public int sortingOrder, sortingLayerId; }
        [Serializable] class SpriteData { public long id; public string name; public Vector3[] vertices; public Vector2[] uv; public int[] triangles; public WeightData[] weights; public MatrixData[] bindPoses; public RectData textureRect; public Vector2 pivot; public float pixelsPerUnit; }
        [Serializable] class WeightData
        {
            public int boneIndex0,boneIndex1,boneIndex2,boneIndex3;
            public float weight0,weight1,weight2,weight3;
            public BoneWeight Weight() { return new BoneWeight { boneIndex0=boneIndex0,boneIndex1=boneIndex1,boneIndex2=boneIndex2,boneIndex3=boneIndex3,weight0=weight0,weight1=weight1,weight2=weight2,weight3=weight3 }; }
        }
        [Serializable] class RectData { public float x, y, width, height; }
        [Serializable] class MatrixData
        {
            public float e00,e01,e02,e03,e10,e11,e12,e13,e20,e21,e22,e23,e30,e31,e32,e33;
            public Matrix4x4 Matrix() { return new Matrix4x4(new Vector4(e00,e10,e20,e30),new Vector4(e01,e11,e21,e31),new Vector4(e02,e12,e22,e32),new Vector4(e03,e13,e23,e33)); }
        }
        [Serializable] class ClipData { public string name; public float duration, frameRate; public bool loop; public CurveData[] curves; }
        [Serializable] class CurveData { public string path, property; public int typeID; public bool isPPtr; public KeyData[] keys; }
        [Serializable] class KeyData { public float time, value, inSlope, outSlope; public long objectPathId; }
    }
}
#endif
