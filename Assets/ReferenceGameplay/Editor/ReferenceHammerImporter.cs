#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RoyalThrone.ReferenceGameplay.Editor
{
    public static class ReferenceHammerImporter
    {
        const string Base="Assets/ReferenceGameplay/Tools/Hammer/";
        const string ResourcesFolder=Base+"Resources/ReferenceGameplay/Hammer/";
        [MenuItem("Royal Throne/Reference Gameplay/Import Hammer Animation VFX and SFX")]
        public static void Build()
        {
            AssetDatabase.Refresh();Directory.CreateDirectory(ResourcesFolder);AssetDatabase.Refresh();
            foreach(string name in new[]{"HammerAnimation.prefab","HammerExplodeAnimation2.anim"})Move(Base+name,ResourcesFolder+name);
            foreach(string name in new[]{"hammer_use_appear","hammer_use_hit","hammer_use_disappear"})
            {
                string path=ResourcesFolder+name+".wav";Move(Base+"Audio/"+name+".wav",path);
                var ai=(AudioImporter)AssetImporter.GetAtPath(path);var settings=ai.defaultSampleSettings;
                settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.compressionFormat=AudioCompressionFormat.PCM;settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;ai.defaultSampleSettings=settings;ai.SaveAndReimport();
            }
            foreach(string path in Directory.GetFiles(Base+"VFXTextures","*.png"))
            {
                var ti=(TextureImporter)AssetImporter.GetAtPath(path.Replace('\\','/'));ti.textureCompression=TextureImporterCompression.Uncompressed;ti.mipmapEnabled=false;ti.alphaIsTransparency=false;ti.maxTextureSize=4096;ti.SaveAndReimport();
            }
            ReferenceGameplayVfxImporter.BuildFile("Design/Hammer-Reference/NativeHammerParticles.json");
            AssetDatabase.SaveAssets();
            if(!ReferenceHammerSequence.Available)throw new Exception("Hammer import has missing runtime dependencies.");
            var clip=UnityEngine.Resources.Load<AnimationClip>("ReferenceGameplay/Hammer/HammerExplodeAnimation2");
            var prefab=UnityEngine.Resources.Load<GameObject>("ReferenceGameplay/VFX/HammerImpactRfv387");
            var renderers=prefab.GetComponentsInChildren<ParticleSystemRenderer>(true);
            if(renderers.Any(r=>!r.sharedMaterial||!r.sharedMaterial.shader))throw new Exception("Hammer VFX has missing materials.");
            File.WriteAllText("Design/Hammer-Reference/game-import-report.txt",$"PASS runtime dependencies\nClip: {clip.name}; duration {clip.length}; FPS {clip.frameRate}\nImpact particle systems: {prefab.GetComponentsInChildren<ParticleSystem>(true).Length}\nOriginal particle shaders: {string.Join(", ",renderers.Select(r=>r.sharedMaterial.shader.name).Distinct())}\n");
        }
        static void Move(string from,string to){if(AssetDatabase.LoadMainAssetAtPath(to))return;string error=AssetDatabase.MoveAsset(from,to);if(!string.IsNullOrEmpty(error))throw new Exception(error);}
    }
}
#endif
