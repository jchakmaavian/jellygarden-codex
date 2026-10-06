#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace RoyalThrone.ReferenceGameplay.Editor
{
    /// <summary>Imports recovered static renderer hierarchies; does not implement blocker rules.</summary>
    public static class ReferenceBlockerImporter
    {
        const string Root = "Assets/ReferenceGameplay/Art/";
        public const string PrefabRoot = Root + "Resources/ReferenceGameplay/Blockers/";
        [Serializable] sealed class Catalog { public SpriteEntry[] sprites; }
        [Serializable] sealed class SpriteEntry { public long sourcePathId; public string assetPath; }
        [Serializable] sealed class Data { public PrefabData[] prefabs; public Goal[] goals; public string[] limitations; }
        [Serializable] sealed class Goal { public string key; public long spritePathId; }
        [Serializable] sealed class PrefabData
        {
            public string key, meshTextureAssetPath;
            public long sourceRootTransformId, meshTransformId;
            public Vector3 centerOffset;
            public Node[] nodes;
            public MeshLayer[] meshLayers;
        }
        [Serializable] sealed class Node
        {
            public long transformId, parentTransformId;
            public string name;
            public bool active;
            public Vector3 localPosition, localScale;
            public Quaternion localRotation;
            public RendererData[] renderers;
        }
        [Serializable] sealed class RendererData
        {
            public long sourcePathId, spritePathId;
            public bool enabled, flipX, flipY;
            public int sortingOrder, drawMode, maskInteraction, spriteSortPoint;
            public Color color;
            public Vector2 size;
        }
        [Serializable] sealed class MeshLayer
        {
            public string name;
            public Vector3[] vertices;
            public Vector2[] uv;
            public int[] triangles;
            public Color color;
        }

        [MenuItem("Tools/Royal Throne/Reference Gameplay/Import native static blocker views")]
        public static void Import()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Import native blocker prefabs outside Play Mode.");
            var catalog = JsonUtility.FromJson<Catalog>(File.ReadAllText(Root + "NativeSpriteCatalog.json"));
            var sprites = new Dictionary<long, Sprite>();
            foreach (var e in catalog.sprites) sprites[e.sourcePathId] = AssetDatabase.LoadAssetAtPath<Sprite>(e.assetPath);
            var data = JsonUtility.FromJson<Data>(File.ReadAllText(Root + "BlockerNativeData.json"));
            Directory.CreateDirectory(PrefabRoot);
            Directory.CreateDirectory(Root + "Meshes");
            Directory.CreateDirectory(Root + "Materials");
            AssetDatabase.Refresh();
            var defaultSprite = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
            var mailbox = MailboxMaterial();
            int renderers = 0;
            foreach (var p in data.prefabs)
            {
                var wrapper = new GameObject(p.key);
                wrapper.AddComponent<SortingGroup>();
                var transforms = new Dictionary<long, Transform>();
                try
                {
                    foreach (var n in p.nodes)
                    {
                        var go = new GameObject(n.name);
                        var tr = go.transform;
                        tr.SetParent(n.parentTransformId == 0 ? wrapper.transform : transforms[n.parentTransformId], false);
                        // Native root authoring placement is not a visual animation offset.
                        // Center the native 2x2 lower-left anchor inside the existing board footprint.
                        tr.localPosition = n.parentTransformId == 0 ? -p.centerOffset : n.localPosition;
                        tr.localRotation = n.localRotation;
                        tr.localScale = n.localScale;
                        transforms[n.transformId] = tr;
                        foreach (var r in n.renderers)
                        {
                            var sr = go.AddComponent<SpriteRenderer>();
                            if (r.spritePathId != 0)
                            {
                                if (!sprites.TryGetValue(r.spritePathId, out var sprite) || !sprite)
                                    throw new InvalidOperationException("Missing native blocker Sprite " + r.spritePathId);
                                sr.sprite = sprite;
                            }
                            sr.sharedMaterial = defaultSprite;
                            sr.enabled = r.enabled;
                            sr.sortingOrder = r.sortingOrder;
                            sr.flipX = r.flipX;
                            sr.flipY = r.flipY;
                            sr.color = r.color;
                            sr.drawMode = (SpriteDrawMode)r.drawMode;
                            sr.size = r.size;
                            sr.maskInteraction = (SpriteMaskInteraction)r.maskInteraction;
                            sr.spriteSortPoint = (SpriteSortPoint)r.spriteSortPoint;
                            renderers++;
                        }
                        go.SetActive(n.active);
                    }
                    for (int i = 0; i < p.meshLayers.Length; i++)
                    {
                        var layer = p.meshLayers[i];
                        var go = new GameObject(layer.name);
                        go.transform.SetParent(transforms[p.meshTransformId], false);
                        var meshPath = Root + "Meshes/" + p.key + "-slot-" + i + ".asset";
                        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                        if (!mesh) { mesh = new Mesh(); AssetDatabase.CreateAsset(mesh, meshPath); }
                        mesh.Clear(); mesh.name = layer.name;
                        mesh.vertices = layer.vertices; mesh.uv = layer.uv; mesh.triangles = layer.triangles;
                        var colors = new Color[layer.vertices.Length];
                        for (int k = 0; k < colors.Length; k++) colors[k] = layer.color;
                        mesh.colors = colors; mesh.RecalculateBounds();
                        EditorUtility.SetDirty(mesh);
                        go.AddComponent<MeshFilter>().sharedMesh = mesh;
                        var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = mailbox;
                        mr.sortingOrder = i; mr.shadowCastingMode = ShadowCastingMode.Off; mr.receiveShadows = false;
                        renderers++;
                    }
                    PrefabUtility.SaveAsPrefabAsset(wrapper, PrefabRoot + p.key + ".prefab");
                }
                finally { UnityEngine.Object.DestroyImmediate(wrapper); }
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Reference Gameplay] Imported " + data.prefabs.Length + " native static blocker views / " + renderers + " renderers. Potion placement and grass2 state remain explicitly incomplete in blocker-import-status.json.");
        }

        static Material MailboxMaterial()
        {
            const string path = Root + "Materials/NativeMailboxPMA.mat";
            var shader = Shader.Find("RoyalThrone/ReferenceGameplay/NativeSpinePMA");
            if (!shader) throw new InvalidOperationException("Native PMA compatibility shader missing.");
            var texImporter = AssetImporter.GetAtPath(Root + "Atlases/mailbox.png") as TextureImporter;
            if (!texImporter) throw new InvalidOperationException("Native mailbox atlas missing.");
            texImporter.textureType = TextureImporterType.Default;
            texImporter.sRGBTexture = true; texImporter.alphaIsTransparency = false;
            texImporter.mipmapEnabled = false; texImporter.npotScale = TextureImporterNPOTScale.None;
            texImporter.textureCompression = TextureImporterCompression.Uncompressed;
            texImporter.maxTextureSize = 512; texImporter.filterMode = FilterMode.Bilinear;
            texImporter.wrapMode = TextureWrapMode.Clamp; texImporter.SaveAndReimport();
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
            material.shader = shader;
            material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "Atlases/mailbox.png");
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
#endif
