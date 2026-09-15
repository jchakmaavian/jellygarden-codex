using System.Linq;
using NUnit.Framework;
using RoyalThrone.Match3Vfx.Editor;
using UnityEngine;

namespace RoyalThrone.Match3Vfx.Tests
{
    public sealed class Match3VfxEditModeTests
    {
        Match3VfxSettings settings;

        [SetUp]
        public void SetUp()
        {
            settings = Match3VfxSettings.CreateDefaults();
            Match3Vfx.Settings = settings;
        }

        [TearDown]
        public void TearDown()
        {
            Match3Vfx.Settings = null;
            Object.DestroyImmediate(settings);
        }

        [Test]
        public void DefaultsMatchRoyalThroneTuning()
        {
            Assert.AreEqual(0.13f, settings.swapDuration);
            Assert.AreEqual(0.22f, settings.rocketAnticipation);
            Assert.AreEqual(8f, settings.rocketCellsPerSecond);
            Assert.AreEqual(0.21f, settings.matchPop.Total, 0.0001f);
            Assert.AreEqual(0.115f, settings.powerClearPop.Total, 0.0001f);
            Assert.IsFalse(settings.enableHitStop);
            Assert.AreEqual(240, settings.shatterPoolSize);
        }

        [TestCase(3, 1, 1f)]
        [TestCase(4, 1, 1.18f)]
        [TestCase(5, 1, 1.36f)]
        [TestCase(3, 3, 1.28f)]
        [TestCase(5, 6, 1.55f)]
        public void IntensityStacksMatchSizeAndCascadeUnderACeiling(int size, int cascade, float expected)
        {
            Assert.AreEqual(expected, Match3Vfx.ComputeIntensity(size, cascade), 0.0001f);
        }

        [TestCase(2, null)]
        [TestCase(3, "GREAT!")]
        [TestCase(4, "BRAVO!")]
        [TestCase(5, "FANTASTIC!")]
        [TestCase(6, "BRILLIANT!")]
        [TestCase(12, "BRILLIANT!")]
        public void BannerTextFollowsCascadeThresholds(int cascade, string expected)
        {
            Assert.AreEqual(expected, settings.BannerTextFor(cascade));
        }

        [Test]
        public void StaggerGrowsPerPieceIndex()
        {
            Assert.AreEqual(0f, Match3Vfx.StaggerFor(0));
            Assert.AreEqual(settings.matchStagger * 3f, Match3Vfx.StaggerFor(3), 0.00001f);
        }

        [Test]
        public void EasingsStartAtZeroAndEndAtOne()
        {
            System.Func<float, float>[] curves =
            {
                Match3Easing.OutQuad, Match3Easing.OutCubic, Match3Easing.InQuad, Match3Easing.InCubic,
                Match3Easing.InOutQuad, t => Match3Easing.OutBack(t), t => Match3Easing.InBack(t),
                t => Match3Easing.OutElastic(t), Match3Easing.Smooth
            };
            foreach (var curve in curves)
            {
                Assert.AreEqual(0f, curve(0f), 0.0001f);
                Assert.AreEqual(1f, curve(1f), 0.0001f);
            }
            Assert.AreEqual(1f, Match3Easing.Hump(0.5f), 0.0001f);
        }

        [Test]
        public void GeneratedShapesFadeToTransparentCorners()
        {
            foreach (var texture in new[] { Match3Shapes.FourPointSparkle, Match3Shapes.SixPointStar, Match3Shapes.Diamond, Match3Shapes.SoftGlow, Match3Shapes.Ring, Match3Shapes.SmokePuff, Match3Shapes.Gem })
            {
                Assert.AreEqual(0f, texture.GetPixel(0, 0).a, 0.01f, texture.name);
                Assert.AreEqual(0f, texture.GetPixel(texture.width - 1, texture.height - 1).a, 0.01f, texture.name);
            }
            Assert.Greater(Match3Shapes.SoftGlow.GetPixel(32, 32).a, 0.9f);
        }

        [Test]
        public void DefaultMaterialsShipInResourcesWithKitShaders()
        {
            Assert.AreEqual(Match3VfxMaterials.AdditiveShader, Resources.Load<Material>(Match3VfxMaterials.AdditivePath)?.shader.name);
            Assert.AreEqual(Match3VfxMaterials.FragmentShader, Resources.Load<Material>(Match3VfxMaterials.FragmentPath)?.shader.name);
            Assert.AreEqual(Match3VfxMaterials.ParticleAlphaShader, Resources.Load<Material>(Match3VfxMaterials.ParticleAlphaPath)?.shader.name);
        }

        [Test]
        public void BurstSlicerFindsColouredChipsAndSkipsWhiteGlow()
        {
            const int size = 96;
            var pixels = new Color32[size * size];
            void Fill(int x0, int y0, int w, int h, Color32 c)
            {
                for (int y = y0; y < y0 + h; y++)
                    for (int x = x0; x < x0 + w; x++)
                        pixels[y * size + x] = c;
            }
            var ruby = new Color32(220, 30, 50, 255);
            Fill(5, 5, 12, 12, ruby);
            Fill(70, 8, 14, 10, ruby);
            Fill(8, 70, 10, 14, ruby);
            Fill(72, 72, 12, 12, ruby);
            Fill(40, 40, 16, 16, new Color32(255, 255, 250, 255)); // centre flash
            Fill(30, 80, 3, 3, ruby);                               // speck

            var islands = Match3BurstLibraryBuilder.FindIslands(pixels, size, size);
            Assert.AreEqual(4, islands.Count);
            Assert.IsTrue(islands.All(i => i.area >= 100));
        }
    }
}
