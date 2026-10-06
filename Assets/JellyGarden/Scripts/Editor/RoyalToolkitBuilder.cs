using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JellyGarden.Scripts.Juice;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RoyalThrone.Editor
{
    public static class RoyalToolkitBuilder
    {
        const string Vendor = "Assets/Plugins/AllIn1VfxToolkit/Demo & Assets/Demo/Prefabs/";
        const string Output = "Assets/JellyGarden/Resources/RoyalGameplay/Toolkit";
        const string Art = "Assets/JellyGarden/Resources/RoyalGameplayV4/05_effects/bursts/";
        [Serializable] sealed class Audit { public string pipeline = "Built-in, orthographic 2D; no camera depth required"; public string[] dependencies; public string[] changes; public int[] burstPieceCounts; }
        [MenuItem("Tools/Royal Throne/Effects/Build integrated toolkit impacts")]
        public static void Build()
        {
            RoyalHomeScreenBuilder.RequireCopy();
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before rebuilding effects.");
            Directory.CreateDirectory(Output + "/Materials"); AssetDatabase.Refresh();
            var library = AssetDatabase.LoadAssetAtPath<RoyalToolkitLibrary>(Output + "/Library.asset");
            if (!library) { library = ScriptableObject.CreateInstance<RoyalToolkitLibrary>(); AssetDatabase.CreateAsset(library, Output + "/Library.asset"); }
            library.ice = BuildImpact("Ice Impact", false); library.fire = BuildImpact("Fire Impact", true);
            library.iceScale = 1.1f; library.fireScale = 3.2f;
            library.iceLifetime = .9f; library.fireLifetime = 1.5f; library.icePool = 4; library.firePool = 2;
            EditorUtility.SetDirty(library);
            var bursts = BuildBursts();
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Design/Effects/Toolkit-review");
            var audit = new Audit {
                dependencies = AssetDatabase.GetDependencies(new[] { Vendor + "Ice Impact.prefab", Vendor + "Fire Impact.prefab" }, true).OrderBy(p => p).ToArray(),
                changes = new[] { "Vendor files unchanged; production wrappers retain nested prefab references", "Removed demo Destroy timers and point lights; fixed six-instance pool", "Disabled SOFTPART_ON and DEPTHGLOW_ON in derived materials; original shaders and vertex streams retained", "Normalized authored parent scale 3 to 1; finite planar bursts with gravity and explicit caps", "Fire starts on the existing TNT clear beat; maximum tail 1.35s, pool retirement at 1.5s", "Ice once per confirmed match group; pool retirement 0.9s", "Burst atlas alpha components become independently moving opaque pieces; no whole-card fade" },
                burstPieceCounts = bursts.bursts.Select(b => b.pieces.Length).ToArray()
            };
            File.WriteAllText("Design/Effects/Toolkit-review/dependency-audit.json", JsonUtility.ToJson(audit, true));
            Debug.Log("[Royal Toolkit] Built derived Ice/Fire impacts and five sliced burst libraries.");
        }
        static GameObject BuildImpact(string name, bool fire)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Vendor + name + ".prefab");
            if (!source) throw new FileNotFoundException("Missing user toolkit prefab: " + name);
            var root = new GameObject("Royal " + name);
            try
            {
                var nested = (GameObject)PrefabUtility.InstantiatePrefab(source);
                nested.transform.SetParent(root.transform, false);
                foreach (var script in nested.GetComponentsInChildren<MonoBehaviour>(true)) if (script) Object.DestroyImmediate(script);
                foreach (var light in nested.GetComponentsInChildren<Light>(true)) Object.DestroyImmediate(light);
                // This scale is authored for the demo's 3D world, not a board cell.
                foreach (Transform child in nested.transform) child.localScale = Vector3.one;
                foreach (var ps in nested.GetComponentsInChildren<ParticleSystem>(true)) Configure(ps, fire);
                root.SetActive(false);
                return PrefabUtility.SaveAsPrefabAsset(root, Output + "/" + name + ".prefab");
            }
            finally { Object.DestroyImmediate(root); }
        }
        static void Configure(ParticleSystem ps, bool fire)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.playOnAwake = false; main.loop = false; main.duration = fire ? 1.5f : .9f;
            main.stopAction = ParticleSystemStopAction.None; main.simulationSpeed = 1; main.useUnscaledTime = false;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy; main.simulationSpace = ParticleSystemSimulationSpace.Local;
            ps.useAutoRandomSeed = false; ps.randomSeed = 1531;
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (!renderer || !renderer.enabled) { main.maxParticles = 1; var empty = ps.emission; empty.enabled = false; return; }
            renderer.sortingLayerID = SortingLayer.NameToID("Default"); renderer.sortingOrder = 38;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false;
            if (renderer.renderMode == ParticleSystemRenderMode.HorizontalBillboard) renderer.renderMode = ParticleSystemRenderMode.Billboard;
            var materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++) materials[i] = DerivedMaterial(materials[i]);
            renderer.sharedMaterials = materials;
            string name = ps.name;
            bool flakes = name.Contains("Flakes"), sparks = name.Contains("Sparks");
            int count = flakes ? 9 : sparks ? (fire ? (name.Contains("Big") ? 12 : 16) : 12) : 1;
            main.maxParticles = count;
            var emission = ps.emission; emission.enabled = true; emission.rateOverTime = 0; emission.rateOverDistance = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0, (short)count) });
            if (sparks || flakes)
            {
                main.startLifetime = fire ? new ParticleSystem.MinMaxCurve(.75f, name.Contains("Big") ? 1.1f : 1.35f) : new ParticleSystem.MinMaxCurve(.45f, flakes ? .78f : .65f);
                // The larger Fire envelope needs a slower local spark launch so
                // its tail stays near the blast instead of spanning the whole board.
                main.startSpeed = new ParticleSystem.MinMaxCurve(fire ? .65f : .8f, fire ? 1.3f : 1.8f);
                main.gravityModifier = fire ? .18f : .20f;
                var shape = ps.shape; shape.enabled = true; shape.shapeType = ParticleSystemShapeType.Circle; shape.radius = .10f;
                shape.rotation = Vector3.zero; shape.scale = Vector3.one;
                if (!fire && sparks) main.startSize = new ParticleSystem.MinMaxCurve(.12f, .28f);
                // Keep physical movement on the board plane rather than toward the lens.
                var force = ps.forceOverLifetime; force.enabled = true; force.space = ParticleSystemSimulationSpace.Local; force.z = 0;
                var limit = ps.limitVelocityOverLifetime; limit.enabled = true; limit.separateAxes = true; limit.space = ParticleSystemSimulationSpace.Local;
                limit.limitX = 20; limit.limitY = 20; limit.limitZ = 0; limit.dampen = 1;
            }
            else
            {
                main.startSpeed = 0; main.gravityModifier = 0;
                main.startLifetime = name.Contains("GlowLight") ? .10f : name.Contains("Sphere") ? .24f : name.Contains("Mark") ? .14f : name.Contains("Flare") ? .22f : fire ? .48f : .32f;
            }
            // Preserve shader-specific Custom1 alpha streams and their original curves.
            PrefabUtility.RecordPrefabInstancePropertyModifications(ps);
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
        }
        static Material DerivedMaterial(Material source)
        {
            if (!source) return null;
            string path = Output + "/Materials/" + source.name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material) { material = new Material(source); AssetDatabase.CreateAsset(material, path); }
            else EditorUtility.CopySerialized(source, material);
            material.name = source.name; material.DisableKeyword("SOFTPART_ON"); material.DisableKeyword("DEPTHGLOW_ON");
            EditorUtility.SetDirty(material); return material;
        }
        static RoyalBurstLibrary BuildBursts()
        {
            string path = Output + "/BurstPieces.asset";
            var library = AssetDatabase.LoadAssetAtPath<RoyalBurstLibrary>(path);
            if (!library) { library = ScriptableObject.CreateInstance<RoyalBurstLibrary>(); AssetDatabase.CreateAsset(library, path); }
            // Only generated subassets belong to this builder; source PNGs stay untouched.
            foreach (var old in AssetDatabase.LoadAllAssetsAtPath(path)) if (old != library) Object.DestroyImmediate(old, true);
            string[] colors = { "red", "blue", "gold", "leaf", "magenta" };
            library.bursts = new RoyalBurstLibrary.Burst[colors.Length];
            for (int color = 0; color < colors.Length; color++)
            {
                string png = Art + "burst_" + colors[color] + ".png";
                var source = AssetDatabase.LoadAssetAtPath<Texture2D>(png);
                if (!source) throw new FileNotFoundException(png);
                var readable = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                try
                {
                    readable.LoadImage(File.ReadAllBytes(png));
                    var pixels = readable.GetPixels32(); int width = readable.width, height = readable.height;
                    var visited = new bool[pixels.Length]; var queue = new Queue<int>();
                    var found = new List<(Rect rect, int area)>();
                    for (int index = 0; index < pixels.Length; index++)
                    {
                        if (visited[index] || pixels[index].a < 40) continue;
                        visited[index] = true; queue.Enqueue(index);
                        int minX = width, minY = height, maxX = 0, maxY = 0, area = 0, colored = 0;
                        while (queue.Count > 0)
                        {
                            int p = queue.Dequeue(), x = p % width, y = p / width; var c = pixels[p];
                            minX = Math.Min(minX, x); minY = Math.Min(minY, y); maxX = Math.Max(maxX, x); maxY = Math.Max(maxY, y); area++;
                            if (Math.Max(c.r, Math.Max(c.g, c.b)) - Math.Min(c.r, Math.Min(c.g, c.b)) > 35) colored++;
                            for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
                            {
                                int nx = x + dx, ny = y + dy;
                                if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
                                int n = ny * width + nx; if (visited[n] || pixels[n].a < 40) continue;
                                visited[n] = true; queue.Enqueue(n);
                            }
                        }
                        // Exclude central white light/sparkle and tiny compression flecks.
                        if (area < 80 || colored < area * .28f || maxX - minX > width * .45f || maxY - minY > height * .45f) continue;
                        found.Add((Rect.MinMaxRect(Math.Max(0, minX - 2), Math.Max(0, minY - 2), Math.Min(width, maxX + 3), Math.Min(height, maxY + 3)), area));
                    }
                    found = found.OrderByDescending(p => p.area).Take(24).OrderBy(p => Mathf.Atan2(p.rect.center.y - height * .5f, p.rect.center.x - width * .5f)).ToList();
                    if (found.Count < 4) throw new InvalidOperationException("Burst atlas " + colors[color] + " has only " + found.Count + " separated colored pieces; inspect alpha.");
                    var pieces = new RoyalBurstLibrary.Piece[found.Count];
                    for (int i = 0; i < pieces.Length; i++)
                    {
                        var rect = found[i].rect;
                        var importedRect = new Rect(rect.x * source.width / width, rect.y * source.height / height, rect.width * source.width / width, rect.height * source.height / height);
                        var sprite = Sprite.Create(source, importedRect, new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect);
                        sprite.name = "burst-" + colors[color] + "-piece-" + i; AssetDatabase.AddObjectToAsset(sprite, library);
                        pieces[i] = new RoyalBurstLibrary.Piece { sprite = sprite, offset = new Vector2(rect.center.x / width - .5f, rect.center.y / height - .5f) };
                    }
                    library.bursts[color] = new RoyalBurstLibrary.Burst { color = colors[color], pieces = pieces };
                }
                finally { Object.DestroyImmediate(readable); }
            }
            EditorUtility.SetDirty(library); return library;
        }
    }
}
