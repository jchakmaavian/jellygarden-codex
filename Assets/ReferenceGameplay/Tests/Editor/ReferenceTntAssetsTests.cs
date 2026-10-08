using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RoyalThrone.ReferenceGameplay.Preview;
using UnityEditor;
using UnityEngine;

namespace RoyalThrone.ReferenceGameplay.Tests
{
    // Guards the TNT imported from royal-throne: every prefab, clip, socket, material and
    // sound the game (and ReferenceTntPreview) loads by name must resolve with nothing missing.
    public class ReferenceTntAssetsTests
    {
        static readonly string[] Visuals = { ReferenceTnt.PieceKey, ReferenceTnt.ComboKey, "TNTTNTCombo" };
        static readonly string[] Vfx =
        {
            ReferenceTnt.FuseVfx, ReferenceTnt.ExplodeVfx,
            ReferenceTnt.ComboFuseVfx, ReferenceTnt.ComboExplodeVfx
        };
        static readonly string[] Audio =
        {
            ReferenceTnt.CreationAudio, ReferenceTnt.ExplodeAudio,
            ReferenceTnt.ComboAudio, "tnt_tnt_combo__15766"
        };

        static GameObject Load(string resource)
        {
            var prefab = Resources.Load<GameObject>(resource);
            Assert.IsNotNull(prefab, "Missing resource " + resource);
            return prefab;
        }

        static string[] ClipNames(GameObject prefab)
        {
            var animator = prefab.GetComponentInChildren<Animator>(true);
            Assert.IsNotNull(animator, prefab.name + " has no Animator");
            Assert.IsNotNull(animator.runtimeAnimatorController, prefab.name + " has no controller");
            return animator.runtimeAnimatorController.animationClips.Select(c => c.name).ToArray();
        }

        [TestCaseSource(nameof(Visuals))]
        public void VisualPrefabLoadsWithAnimator(string key) => ClipNames(Load("ReferenceGameplay/Visuals/" + key));

        [Test]
        public void PieceHasCreationClip()
        {
            var clips = ClipNames(Load("ReferenceGameplay/Visuals/" + ReferenceTnt.PieceKey));
            CollectionAssert.Contains(clips, ReferenceTnt.CreationState);
            CollectionAssert.Contains(clips, "TntLightballCreationAnimation");
            CollectionAssert.Contains(clips, "TntBoosterAnimation");
        }

        [Test]
        public void ComboHasExplodeClipAndFuseSocket()
        {
            var combo = Load("ReferenceGameplay/Visuals/" + ReferenceTnt.ComboKey);
            CollectionAssert.Contains(ClipNames(combo), ReferenceTnt.ComboState);
            Assert.IsTrue(combo.GetComponentsInChildren<Transform>(true).Any(t => t.name == ReferenceTnt.ComboFuseSocket),
                "Combo fuse socket " + ReferenceTnt.ComboFuseSocket + " missing");
        }

        [Test]
        public void ClipsHavePositiveLengthAndResolvedSprites()
        {
            foreach (var key in Visuals)
            {
                var controller = Load("ReferenceGameplay/Visuals/" + key).GetComponentInChildren<Animator>(true).runtimeAnimatorController;
                foreach (var clip in controller.animationClips)
                {
                    Assert.Greater(clip.length, 0, clip.name + " is empty");
                    foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                        foreach (var frame in AnimationUtility.GetObjectReferenceCurve(clip, binding))
                            Assert.IsNotNull(frame.value, $"{clip.name} {binding.path}.{binding.propertyName} keys a missing object at {frame.time:0.###}s");
                }
            }
        }

        [TestCaseSource(nameof(Vfx))]
        public void VfxPrefabHasRenderableParticles(string key)
        {
            var vfx = Load("ReferenceGameplay/VFX/" + key);
            var renderers = vfx.GetComponentsInChildren<ParticleSystemRenderer>(true);
            Assert.IsNotEmpty(renderers, key + " has no particle systems");
            foreach (var r in renderers)
                if (r.enabled && r.renderMode != ParticleSystemRenderMode.None)
                {
                    Assert.IsNotNull(r.sharedMaterial, $"{key}/{r.name} has no material");
                    Assert.IsTrue(r.sharedMaterial.shader && r.sharedMaterial.shader.isSupported, $"{key}/{r.name} shader unsupported");
                }
        }

        [TestCaseSource(nameof(Audio))]
        public void AudioClipLoads(string clip) =>
            Assert.IsNotNull(Resources.Load<AudioClip>("ReferenceGameplay/Audio/" + clip), "Missing audio " + clip);

        [Test]
        public void ImportedPrefabsHaveNoMissingScriptsOrReferences()
        {
            var problems = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/ReferenceGameplay" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) > 0)
                        problems.Add($"{path}: {t.name} has a missing script");
                    foreach (var component in t.GetComponents<Component>())
                    {
                        if (!component) continue;
                        var property = new SerializedObject(component).GetIterator();
                        while (property.NextVisible(true))
                            if (property.propertyType == SerializedPropertyType.ObjectReference &&
                                !property.objectReferenceValue && property.objectReferenceEntityIdValue.IsValid())
                                problems.Add($"{path}: {t.name}/{component.GetType().Name}.{property.propertyPath} is missing");
                    }
                }
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test]
        public void PreviewSceneExistsAndHoldsThePlayer()
        {
            var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ReferenceTntPreviewMenu.ScenePath);
            Assert.IsNotNull(scene, "Build it with Tools/Royal Throne/TNT (original)/Open Preview Scene");
            var script = AssetDatabase.FindAssets("ReferenceTntPreview t:MonoScript")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Single(p => p.EndsWith("/ReferenceTntPreview.cs"));
            StringAssert.Contains("guid: " + AssetDatabase.AssetPathToGUID(script),
                System.IO.File.ReadAllText(ReferenceTntPreviewMenu.ScenePath), "Preview scene lost its ReferenceTntPreview player");
        }
    }
}
