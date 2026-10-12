#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

/// <summary>Imports the one missing exact-source TNT+TNT fuse, retaining shared assets.</summary>
public static class ReferenceRfv387FuseImporter
{
    const string Design = "Design/Gameplay-rfv387/Reference/";
    const string Vfx = "Assets/ReferenceGameplay/VFX/";
    const string PrefabPath = Vfx + "Resources/ReferenceGameplay/VFX/TntTntComboFuseParticles.prefab";
    static readonly Dictionary<long,string> Reused = new Dictionary<long,string>
    {
        {387000003, Vfx + "Materials/387000003.mat"},
        {387000011, Vfx + "Materials/387000011.mat"},
        {16582, Vfx + "AtlasSprites/16582.asset"},
        {21650, Vfx + "AtlasSprites/21650.asset"}
    };
    [Serializable] public class Result
    {
        public bool ok;
        public string error, prefab;
        public int gameObjects, particleSystems;
        public bool reusedAssetsUnchanged;
        public List<ReferenceGameplayVfxImporter.VerifiedComponent> components = new List<ReferenceGameplayVfxImporter.VerifiedComponent>();
    }
    static string Hash(string path)
    {
        using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "");
    }
    [MenuItem("Royal Throne/Reference Gameplay/Import rfv387 TNT Combo Fuse Only")]
    public static void Build()
    {
        string project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        var result = new Result {prefab = PrefabPath};
        try
        {
            var before = Reused.Values.SelectMany(p => new[]{p,p+".meta"}).ToDictionary(p=>p,p=>Hash(Path.Combine(project,p)));
            var refs = new Dictionary<long,UnityEngine.Object>();
            foreach (var pair in Reused)
            {
                var asset = AssetDatabase.LoadMainAssetAtPath(pair.Value);
                if (!asset) throw new InvalidOperationException("Missing verified shared dependency: " + pair.Value);
                refs[pair.Key] = asset;
            }
            string source = Design + "TntTntComboFuseParticles-import.json";
            var document = JsonUtility.FromJson<ReferenceGameplayVfxImporter.Document>(File.ReadAllText(Path.Combine(project,source)));
            if (document.prefabs.Length != 1 || document.prefabs[0].name != "TntTntComboFuseParticles" || document.materials.Length != 0)
                throw new InvalidOperationException("Expected only the missing source fuse with reused materials.");
            ReferenceGameplayVfxImporter.BuildFile(source, refs);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            result.gameObjects = prefab.GetComponentsInChildren<Transform>(true).Length;
            var systems = prefab.GetComponentsInChildren<ParticleSystem>(true);
            result.particleSystems = systems.Length;
            if (result.gameObjects != 7 || systems.Length != 6) throw new InvalidOperationException("Native fuse hierarchy count differs from source.");
            foreach (var system in systems)
            {
                var renderer = system.GetComponent<ParticleSystemRenderer>();
                if (!renderer || !renderer.sharedMaterial || !renderer.sharedMaterial.shader || !renderer.sharedMaterial.shader.isSupported)
                    throw new InvalidOperationException("Unusable source fuse material: " + system.name);
                var sheet = system.textureSheetAnimation;
                if (sheet.enabled && sheet.mode == ParticleSystemAnimationMode.Sprites)
                    for (int i=0;i<sheet.spriteCount;i++) if (!sheet.GetSprite(i)) throw new InvalidOperationException("Unresolved fuse sprite: " + system.name);
                result.components.Add(new ReferenceGameplayVfxImporter.VerifiedComponent {
                    prefab="TntTntComboFuseParticles", path=AnimationUtility.CalculateTransformPath(system.transform,prefab.transform),
                    type="ParticleSystem", json=EditorJsonUtility.ToJson(system)
                });
            }
            result.reusedAssetsUnchanged = before.All(p=>Hash(Path.Combine(project,p.Key))==p.Value);
            if (!result.reusedAssetsUnchanged) throw new InvalidOperationException("An existing shared asset or GUID changed.");
            result.ok = true;
            Debug.Log("REFERENCE_RFV387_FUSE_IMPORTED: 7 objects, 6 native particle systems; existing dependencies unchanged.");
        }
        catch (Exception error) {result.error=error.ToString();throw;}
        finally {File.WriteAllText(Path.Combine(project,Design+"missing-fuse-import-result.json"),JsonUtility.ToJson(result,true));}
    }
}
#endif
