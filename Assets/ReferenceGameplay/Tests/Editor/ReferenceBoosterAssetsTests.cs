using System.Linq;
using NUnit.Framework;
using RoyalThrone.ReferenceGameplay.Preview;
using UnityEditor;
using UnityEngine;
using static RoyalThrone.ReferenceGameplay.Preview.ReferenceBoosters;

namespace RoyalThrone.ReferenceGameplay.Tests
{
    // Guards the Rocket, Propeller, Color Ball, combo, match and win assets imported from royal-throne:
    // everything ReferenceBoosterPreview loads by name must resolve with nothing missing.
    public class ReferenceBoosterAssetsTests
    {
        static readonly object[] VisualStates =
        {
            new object[] { RocketKey, new[] { RocketHorizontalState, RocketVerticalState } },
            new object[] { PropellerKey, new[] { PropellerCreationState, PropellerTakeoffState, PropellerFlyState } },
            new object[] { ColorBallKey, new[] { ColorBallCreationState, ColorBallDefaultState, ColorBallUseState } },
            new object[] { ColorBallComboKey, new[] { ColorBallComboState } },
            new object[] { "ColorBallColorBallCombo", new[] { ColorBallComboState } },
        };

        static readonly string[] ParticleVfx =
        {
            RocketTrailVfx, PropellerTakeoffVfx, PropellerImpactVfx, PropellerRocketVfx, PropellerTntFuseVfx,
            TntRocketChargeVfx, TntRocketVfx, ColorBallUseVfx, ColorBallExplodeVfx, ColorBallContactVfx,
            ColorBallComboChargeVfx, ColorBallComboExplodeVfx, MatchVfx, SwapVfx, MovesFlightVfx, MovesHitVfx,
            "RemainingMovesCoinTracer", StarHitVfx, StarIdleVfx
        };

        static readonly string[] Audio = new[]
        {
            RocketCreationAudio, RocketAudio, PropellerCreationAudio, PropellerTakeoffAudio, PropellerFlyAudio,
            PropellerHitAudio, PropellerTntAudio, RocketTntAudio, ColorBallCreationAudio, ColorBallStartAudio,
            ColorBallExplodeAudio, ColorBallComboAudio
        }.Concat(ColorBallRayAudio).Concat(MatchAudio).ToArray();

        static GameObject Load(string resource)
        {
            var prefab = Resources.Load<GameObject>(resource);
            Assert.IsNotNull(prefab, "Missing resource " + resource);
            return prefab;
        }

        static RuntimeAnimatorController Controller(GameObject prefab)
        {
            var animator = prefab.GetComponentInChildren<Animator>(true);
            Assert.IsNotNull(animator, prefab.name + " has no Animator");
            Assert.IsNotNull(animator.runtimeAnimatorController, prefab.name + " has no controller");
            return animator.runtimeAnimatorController;
        }

        static bool Has(GameObject prefab, string node) => prefab.GetComponentsInChildren<Transform>(true).Any(t => t.name == node);

        [TestCaseSource(nameof(VisualStates))]
        public void VisualHasItsClipsWithResolvedKeys(string key, string[] states)
        {
            var controller = Controller(Load("ReferenceGameplay/Visuals/" + key));
            var names = controller.animationClips.Select(c => c.name).ToArray();
            foreach (var state in states) CollectionAssert.Contains(names, state, key + " lacks " + state);
            foreach (var clip in controller.animationClips)
            {
                Assert.Greater(clip.length, 0, clip.name + " is empty");
                foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                    foreach (var frame in AnimationUtility.GetObjectReferenceCurve(clip, binding))
                        Assert.IsNotNull(frame.value, $"{clip.name} {binding.path}.{binding.propertyName} keys a missing object at {frame.time:0.###}s");
            }
        }

        [Test]
        public void RocketHasBothBranches()
        {
            var rocket = Load("ReferenceGameplay/Visuals/" + RocketKey);
            foreach (var node in new[] { RocketHorizontalBranch, RocketVerticalBranch })
                Assert.IsTrue(Has(rocket, node), "Rocket lacks " + node);
        }

        [Test]
        public void ColorBallHasItsBeamSource() =>
            Assert.IsTrue(Has(Load("ReferenceGameplay/Visuals/" + ColorBallKey), "Main"), "Color Ball lacks Animator/Main");

        [TestCaseSource(nameof(ParticleVfx))]
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

        [Test]
        public void ColorBallRayHasTexturedLines()
        {
            var lines = Load("ReferenceGameplay/VFX/" + ColorBallRayVfx).GetComponentsInChildren<LineRenderer>(true);
            Assert.IsNotEmpty(lines, "The ray prefab has no lines");
            foreach (var line in lines)
            {
                Assert.IsNotNull(line.sharedMaterial, line.name + " has no material");
                Assert.IsTrue(line.sharedMaterial.shader.isSupported, line.name + " shader unsupported");
            }
        }

        [Test]
        public void MatchExplosionHasAPartPerBoardColour()
        {
            var match = Load("ReferenceGameplay/VFX/" + MatchVfx);
            foreach (var colour in new[] { "red", "blue", "yellow", "green", "pink" })
                Assert.IsTrue(Has(match, "blast_part_" + colour), "Match explosion lacks blast_part_" + colour);
        }

        [TestCaseSource(nameof(Audio))]
        public void AudioClipLoads(string clip) =>
            Assert.IsNotNull(Resources.Load<AudioClip>("ReferenceGameplay/Audio/" + clip), "Missing audio " + clip);

        [TestCase(TntSprite), TestCase(RocketSprite)]
        [TestCase(RocketLeftSprite), TestCase(RocketRightSprite), TestCase(RocketTopSprite), TestCase(RocketBottomSprite)]
        public void SpriteLoads(string sprite) => Assert.IsNotNull(ReferenceBoosters.Sprite(sprite), "Missing sprite " + sprite);

        [Test]
        public void PreviewSceneExistsAndHoldsThePlayer()
        {
            var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ReferenceBoosterPreviewMenu.ScenePath);
            Assert.IsNotNull(scene, "Build it with Tools/Royal Throne/Boosters (original)/Open Preview Scene");
            var script = AssetDatabase.FindAssets("ReferenceBoosterPreview t:MonoScript")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Single(p => p.EndsWith("/ReferenceBoosterPreview.cs"));
            StringAssert.Contains("guid: " + AssetDatabase.AssetPathToGUID(script),
                System.IO.File.ReadAllText(ReferenceBoosterPreviewMenu.ScenePath), "Preview scene lost its ReferenceBoosterPreview player");
        }
    }
}
