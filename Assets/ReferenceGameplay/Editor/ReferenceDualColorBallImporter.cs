using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class ReferenceDualColorBallImporter
{
    const string Folder = "Design/DualColorBall-Reference/";
    const string Material = "Assets/ReferenceGameplay/VFX/Materials/387000011.mat";
    public static void Build()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Dual Colorball import requires Edit Mode.");
        string Hash(string path) { using (var sha = SHA256.Create()) return Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(path))); }
        var retained = new[] { Material, Material + ".meta", "Assets/ReferenceGameplay/Animation/Clips/BallBallExplodeAnimation.anim", "Assets/ReferenceGameplay/Animation/Clips/BallBallExplodeAnimation.anim.meta", "Assets/ReferenceGameplay/Animation/Resources/ReferenceGameplay/Visuals/ColorBallColorBallCombo.prefab", "Assets/ReferenceGameplay/Animation/Resources/ReferenceGameplay/Visuals/ColorBallColorBallCombo.prefab.meta" };
        var before = retained.ToDictionary(p => p, Hash);
        var references = new Dictionary<long, UnityEngine.Object> { { 387000011, AssetDatabase.LoadAssetAtPath<Material>(Material) } };
        var input = JsonUtility.FromJson<ReferenceGameplayVfxImporter.Document>(File.ReadAllText(Folder + "NativeDualColorBallParticles.json"));
        foreach (var texture in input.textures)
        {
            var ti = (TextureImporter)AssetImporter.GetAtPath(texture.assetPath);
            ti.npotScale = TextureImporterNPOTScale.None; ti.SaveAndReimport();
        }
        ReferenceGameplayVfxImporter.BuildFile(Folder + "NativeDualColorBallParticles.json", references);
        var dump = new ReferenceGameplayVfxImporter.VerifiedData();
        var source = JsonUtility.FromJson<ReferenceGameplayVfxImporter.Document>(File.ReadAllText(Folder + "NativeDualColorBallParticles.json"));
        int systems = 0;
        foreach (var effect in source.prefabs)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ReferenceGameplay/VFX/Resources/ReferenceGameplay/VFX/" + effect.name + ".prefab");
            var nodes = effect.nodes.ToDictionary(n => n.transformId);
            foreach (var node in effect.nodes)
            {
                var names = new List<string>(); var current = node;
                while (current.parentTransformId != 0) { names.Insert(0, current.name); current = nodes[current.parentTransformId]; }
                var path = string.Join("/", names);
                var transform = path.Length == 0 ? prefab.transform : prefab.transform.Find(path);
                foreach (var component in node.components)
                {
                    if (component.type != "ParticleSystem" && component.type != "ParticleSystemRenderer") continue;
                    var actual = transform.GetComponent(component.type);
                    if (!actual) throw new InvalidOperationException("Missing native component " + path);
                    if (actual is ParticleSystem ps)
                    {
                        systems++;
                        var sheet = ps.textureSheetAnimation;
                        if (sheet.enabled && sheet.mode == ParticleSystemAnimationMode.Sprites)
                            for (int i = 0; i < sheet.spriteCount; i++) if (!sheet.GetSprite(i)) throw new InvalidOperationException("Missing source particle sprite " + path);
                    }
                    if (actual is ParticleSystemRenderer renderer)
                    {
                        if (!renderer.sharedMaterial || !renderer.sharedMaterial.shader || !renderer.sharedMaterial.shader.isSupported) throw new InvalidOperationException("Unsupported material " + path);
                        if (renderer.renderMode == ParticleSystemRenderMode.Mesh && !renderer.mesh) throw new InvalidOperationException("Missing native particle mesh " + path);
                    }
                    dump.components.Add(new ReferenceGameplayVfxImporter.VerifiedComponent { prefab = effect.name, path = path, type = component.type, sourcePathId = component.pathId, json = EditorJsonUtility.ToJson(actual) });
                }
            }
        }
        File.WriteAllText(Folder + "imported-modules.json", JsonUtility.ToJson(dump));
        // Nine source body frames are pixel-identical; one blurred lower frame differs.
        // Build a combo-only clip/controller/view while retaining the existing shared assets.
        string animation = "Assets/ReferenceGameplay/Animation/";
        var originalClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(animation + "Clips/BallBallExplodeAnimation.anim");
        string clipPath = animation + "Clips/BallBallExplodeAnimationRfv387.anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (!clip) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, clipPath); }
        EditorUtility.CopySerialized(originalClip, clip); clip.name = "BallBallExplodeAnimation";
        var exactBlur = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ReferenceGameplay/VFX/AtlasSprites/987018576.asset");
        if (!exactBlur) throw new InvalidOperationException("Missing exact rfv387 blurred body frame.");
        foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
        {
            var keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
            for (int i = 0; i < keys.Length; i++) if (keys[i].value && System.Text.RegularExpressions.Regex.Replace(keys[i].value.name, @"__\d+$", "") == "light_ball-bot_blur_2") keys[i].value = exactBlur;
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        }
        EditorUtility.SetDirty(clip);
        string controllerPath = animation + "Controllers/BallBallComboRfv387.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (!controller) controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        var machine = controller.layers[0].stateMachine;
        foreach (var state in machine.states) machine.RemoveState(state.state);
        var play = machine.AddState("BallBallExplodeAnimation"); play.motion = clip; play.speed = 1; machine.defaultState = play;
        var originalView = AssetDatabase.LoadAssetAtPath<GameObject>(animation + "Resources/ReferenceGameplay/Visuals/ColorBallColorBallCombo.prefab");
        var view = UnityEngine.Object.Instantiate(originalView);
        try
        {
            view.name = "ColorBallColorBallComboRfv387";
            view.GetComponentInChildren<Animator>(true).runtimeAnimatorController = controller;
            PrefabUtility.SaveAsPrefabAsset(view, animation + "Resources/ReferenceGameplay/Visuals/ColorBallColorBallComboRfv387.prefab");
        }
        finally { UnityEngine.Object.DestroyImmediate(view); }
        AssetDatabase.SaveAssets();
        if (systems != 8 || before.Any(p => Hash(p.Key) != p.Value)) throw new InvalidOperationException("Native system count or retained assets changed.");
        string sound = "Assets/ReferenceGameplay/Audio/Resources/ReferenceGameplay/Audio/lightball_lightball_combo__rfv387_17531.wav";
        var importer = (AudioImporter)AssetImporter.GetAtPath(sound);
        var settings = importer.defaultSampleSettings; settings.loadType = AudioClipLoadType.DecompressOnLoad; settings.compressionFormat = AudioCompressionFormat.PCM; settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate; settings.preloadAudioData = true;
        importer.defaultSampleSettings = settings; importer.forceToMono = false; importer.SaveAndReimport();
        var audio = AssetDatabase.LoadAssetAtPath<AudioClip>(sound);
        if (!audio || audio.frequency != 44100 || audio.channels != 1 || Mathf.Abs(audio.length - 4.396077f) > .002f) throw new InvalidOperationException("Combo sound descriptor changed.");
        File.WriteAllText(Folder + "import-result.txt", "PASS: 2 native VFX prefabs, 8 particle systems, 8 exact-source sprites, native combo sound; existing clip, view and shared material unchanged.\n");
        Debug.Log("DUAL_COLORBALL_IMPORT_PASSED");
    }
}
