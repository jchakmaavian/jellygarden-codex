using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace RoyalThrone.Match3Vfx.Tests
{
    public sealed class Match3VfxPlayModeTests
    {
        TestBoard board;
        Match3VfxHost host;
        Match3VfxSettings settings;
        CoroutineRunner runner;
        GameObject sceneCamera;

        [SetUp]
        public void SetUp()
        {
            Match3Vfx.Shutdown();
            // A real game scene always has a camera with an AudioListener.
            sceneCamera = new GameObject("Test camera", typeof(Camera), typeof(AudioListener));
            settings = Match3VfxSettings.CreateDefaults();
            host = new Match3VfxHost { cellSize = 1f, soundEnabled = false, useMainCameraForShake = false };
            board = new TestBoard(8, 8, 1f);
            Match3Vfx.Configure(settings, host, board);
            runner = new GameObject("Test coroutine runner").AddComponent<CoroutineRunner>();
        }

        [TearDown]
        public void TearDown()
        {
            Match3Vfx.Shutdown();
            Match3Vfx.Board = null;
            if (runner) Object.Destroy(runner.gameObject);
            if (sceneCamera) Object.Destroy(sceneCamera);
            Time.timeScale = 1f;
        }

        static IEnumerator Until(Func<bool> condition, float timeoutSeconds)
        {
            float end = Time.realtimeSinceStartup + timeoutSeconds;
            while (!condition() && Time.realtimeSinceStartup < end) yield return null;
        }

        static void AssertTravelOrder(IList<TestBoard.Hit> hits, Func<Vector2Int, int> distance)
        {
            for (int i = 0; i < hits.Count; i++)
                for (int j = i + 1; j < hits.Count; j++)
                    if (hits[i].time < hits[j].time)
                        Assert.LessOrEqual(distance(hits[i].cell), distance(hits[j].cell),
                            "cell " + hits[j].cell + " was hit before nearer cell " + hits[i].cell);
        }

        // ------------------------------------------------------------------ rig

        [UnityTest]
        public IEnumerator RigBuildsFromDefaultsWithFixedPools()
        {
            Match3Vfx.Prewarm();
            yield return null;
            var rig = Match3Vfx.ExistingRig;
            Assert.IsTrue(rig, "rig was not created");
            Assert.AreEqual(settings.shatterPoolSize, rig.Shatter.Capacity);
            Assert.AreEqual(settings.projectilePool, rig.Powerups.ProjectileCapacity);
            Assert.AreEqual(settings.soundVoices, rig.Sfx.Capacity);
            Assert.IsFalse(rig.Banner.Built, "banner canvas should be built lazily");
        }

        [UnityTest]
        public IEnumerator PreviewsRunWithoutBoardAndReturnProjectiles()
        {
            Match3Vfx.Board = null;
            foreach (Match3PreviewEffect effect in Enum.GetValues(typeof(Match3PreviewEffect)))
                Match3Vfx.Preview(effect, Vector3.zero);
            yield return new WaitForSeconds(1.6f);
            Assert.AreEqual(0, Match3Vfx.ExistingRig.Powerups.ActiveProjectiles);
            LogAssert.NoUnexpectedReceived();
        }

        // ------------------------------------------------------------------ rocket

        [UnityTest]
        public IEnumerator RocketHitsEveryCellOfItsRowOnceInTravelOrder()
        {
            var origin = new Vector2Int(2, 3);
            float start = Time.time;
            var handle = Match3Vfx.PlayRocket(origin, true);
            Assert.IsTrue(Match3Vfx.Busy);
            yield return Until(() => handle.IsDone, 6f);

            Assert.IsTrue(handle.IsDone, "rocket did not finish");
            Assert.IsFalse(Match3Vfx.Busy);
            Assert.AreEqual(8, board.Hits.Count);
            Assert.AreEqual(8, board.UniqueCells.Count());
            Assert.IsTrue(board.Hits.All(h => h.cell.y == 3 && h.source == Match3HitSource.Rocket));
            AssertTravelOrder(board.Hits, c => Mathf.Abs(c.x - origin.x));
            Assert.GreaterOrEqual(board.Hits[0].time - start, settings.rocketAnticipation * 0.9f, "anticipation skipped");
            Assert.LessOrEqual(board.Hits.Last().time - board.Hits[0].time, settings.rocketMaxSweep * 1.5f + 0.1f, "sweep too slow");
        }

        [UnityTest]
        public IEnumerator VerticalRocketSkipsHoles()
        {
            board.Holes.Add(new Vector2Int(4, 6));
            var handle = Match3Vfx.PlayRocket(new Vector2Int(4, 1), false);
            yield return Until(() => handle.IsDone, 6f);
            Assert.AreEqual(7, board.Hits.Count);
            Assert.IsTrue(board.Hits.All(h => h.cell.x == 4));
            Assert.IsFalse(board.UniqueCells.Contains(new Vector2Int(4, 6)));
        }

        [UnityTest]
        public IEnumerator PauseHoldsARocketUntilResumed()
        {
            host.isPaused = true;
            var handle = Match3Vfx.PlayRocket(new Vector2Int(3, 3), true);
            yield return new WaitForSeconds(1.2f);
            Assert.AreEqual(0, board.Hits.Count, "hits were delivered while paused");
            Assert.IsTrue(Match3Vfx.Busy);
            host.isPaused = false;
            yield return Until(() => handle.IsDone, 6f);
            Assert.AreEqual(8, board.Hits.Count);
        }

        // ------------------------------------------------------------------ TNT, helicopter, orb

        [UnityTest]
        public IEnumerator ExplosionHitsRadiusInOutwardWaves()
        {
            var origin = new Vector2Int(4, 4);
            var handle = Match3Vfx.PlayExplosion(origin, 2);
            yield return Until(() => handle.IsDone, 4f);
            Assert.AreEqual(25, board.Hits.Count);
            Assert.IsTrue(board.Hits.All(h => h.source == Match3HitSource.Blast));
            AssertTravelOrder(board.Hits, c => Mathf.Max(Mathf.Abs(c.x - origin.x), Mathf.Abs(c.y - origin.y)));
        }

        [UnityTest]
        public IEnumerator ExplosionIsClippedAtTheBoardCorner()
        {
            var handle = Match3Vfx.PlayExplosion(Vector2Int.zero, 2);
            yield return Until(() => handle.IsDone, 4f);
            Assert.AreEqual(9, board.Hits.Count);
        }

        [UnityTest]
        public IEnumerator HelicopterHitsNeighboursThenItsTarget()
        {
            var handle = Match3Vfx.PlayHelicopter(new Vector2Int(3, 3));
            yield return Until(() => handle.IsDone, 5f);
            Assert.AreEqual(5, board.Hits.Count);
            var cells = board.UniqueCells.ToList();
            foreach (var n in new[] { new Vector2Int(3, 4), new Vector2Int(3, 2), new Vector2Int(2, 3), new Vector2Int(4, 3) })
                Assert.Contains(n, cells);
            Assert.AreEqual(new Vector2Int(7, 7), board.Hits.Last().cell);
            Assert.IsTrue(board.Hits.All(h => h.source == Match3HitSource.Helicopter));
        }

        [UnityTest]
        public IEnumerator HelicopterCarriesARocketToItsTarget()
        {
            var handle = Match3Vfx.PlayCombo(Match3Power.Helicopter, Match3Power.RocketHorizontal, new Vector2Int(3, 3));
            yield return Until(() => handle.IsDone, 8f);
            Assert.AreEqual(4, board.Hits.Count(h => h.source == Match3HitSource.Helicopter));
            var rocketHits = board.Hits.Where(h => h.source == Match3HitSource.Rocket).ToList();
            Assert.AreEqual(8, rocketHits.Count);
            Assert.IsTrue(rocketHits.All(h => h.cell.y == 7));
        }

        [UnityTest]
        public IEnumerator ColorOrbConvertsEveryTargetBeforeHitting()
        {
            var targets = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 1), new Vector2Int(2, 2), new Vector2Int(5, 5), new Vector2Int(7, 7) };
            var converted = new List<(Vector2Int cell, Match3Power power, float time)>();
            var handle = Match3Vfx.PlayColorOrb(new Vector2Int(3, 3), targets, Match3Power.RocketHorizontal, (c, p) => converted.Add((c, p, Time.time)));
            yield return Until(() => handle.IsDone, 4f);
            Assert.AreEqual(5, converted.Count);
            Assert.IsTrue(converted.All(c => c.power == Match3Power.RocketHorizontal));
            Assert.AreEqual(5, board.Hits.Count);
            Assert.IsTrue(board.Hits.All(h => h.source == Match3HitSource.ColorOrb));
            Assert.LessOrEqual(converted.Max(c => c.time), board.Hits.Min(h => h.time));
        }

        IEnumerator Combo(Match3Power a, Match3Power b, int expectedUniqueCells)
        {
            var handle = Match3Vfx.PlayCombo(a, b, new Vector2Int(3, 3));
            yield return Until(() => handle.IsDone, 8f);
            Assert.IsTrue(handle.IsDone, a + " + " + b + " did not finish");
            Assert.AreEqual(expectedUniqueCells, board.UniqueCells.Count(), a + " + " + b);
        }

        [UnityTest] public IEnumerator RocketPlusRocketClearsACross() => Combo(Match3Power.RocketHorizontal, Match3Power.RocketVertical, 15);
        [UnityTest] public IEnumerator RocketPlusTntClearsAThreeLaneCross() => Combo(Match3Power.RocketVertical, Match3Power.Tnt, 39);
        [UnityTest] public IEnumerator TntPlusTntClearsRadiusFour() => Combo(Match3Power.Tnt, Match3Power.Tnt, 64);

        [UnityTest]
        public IEnumerator ClearCancelsSequencesAndReturnsProjectiles()
        {
            // A long fuse keeps the blast running when Clear arrives.
            settings.tntAnticipation = 0.5f;
            var blast = Match3Vfx.PlayExplosion(new Vector2Int(3, 3), 4);
            var rocket = Match3Vfx.PlayRocket(new Vector2Int(0, 0), true);
            yield return new WaitForSeconds(0.3f);
            Assert.IsFalse(blast.IsDone || rocket.IsDone, "sequences finished before Clear");
            Match3Vfx.Clear();
            int hits = board.Hits.Count;
            Assert.IsFalse(Match3Vfx.Busy);
            Assert.IsTrue(blast.Cancelled && rocket.Cancelled);
            Assert.IsTrue(blast.IsDone && rocket.IsDone);
            Assert.AreEqual(0, Match3Vfx.ExistingRig.Powerups.ActiveProjectiles);
            yield return new WaitForSeconds(0.8f);
            Assert.AreEqual(hits, board.Hits.Count, "a cancelled sequence kept hitting cells");
        }

        // ------------------------------------------------------------------ pieces

        static SpriteRenderer MakePiece(string name, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Match3Shapes.GemSprite;
            sr.color = new Color(0.3f, 0.6f, 1f);
            return sr;
        }

        [UnityTest]
        public IEnumerator MatchPopHidesPieceRestoresScaleAndBreaksIntoChips()
        {
            var piece = MakePiece("Piece", Vector3.zero);
            bool finished = false;
            float start = Time.time;
            runner.StartCoroutine(Match3PieceMotion.Pop(piece.transform, piece, new Match3PopOptions
            {
                burstColor = piece.color,
                onFinished = () => finished = true
            }));
            yield return Until(() => finished, 2f);
            Assert.IsTrue(finished);
            Assert.GreaterOrEqual(Time.time - start, settings.matchPop.Total * 0.9f);
            Assert.IsFalse(piece.enabled);
            Assert.AreEqual(Vector3.one, piece.transform.localScale);
            Assert.Greater(Match3Vfx.ExistingRig.Shatter.ActiveFragments, 0);
            Object.Destroy(piece.gameObject);
        }

        [UnityTest]
        public IEnumerator ShatterNeverExceedsItsPoolAndRecycles()
        {
            settings.shatterPoolSize = 32;
            var piece = MakePiece("Piece", Vector3.zero);
            for (int i = 0; i < 100; i++) Match3Vfx.Shatter(piece, 1f, Match3ShatterStyle.PowerClear);
            var rig = Match3Vfx.ExistingRig;
            Assert.AreEqual(32, rig.Shatter.Capacity);
            Assert.LessOrEqual(rig.Shatter.ActiveFragments, 32);
            yield return new WaitForSeconds(2.6f);
            Assert.AreEqual(0, rig.Shatter.ActiveFragments);
            Object.Destroy(piece.gameObject);
        }

        [UnityTest]
        public IEnumerator FallLandsExactlyAndSquashesBackToRest()
        {
            var piece = MakePiece("Faller", new Vector3(0f, 5f, 0f));
            bool done = false;
            runner.StartCoroutine(Run(Match3PieceMotion.FallAndLand(piece.transform, Vector3.zero, Vector3.one), () => done = true));
            yield return Until(() => done, 3f);
            Assert.IsTrue(done);
            Assert.AreEqual(Vector3.zero, piece.transform.position);
            Assert.AreEqual(Vector3.one, piece.transform.localScale);
            Object.Destroy(piece.gameObject);
        }

        [UnityTest]
        public IEnumerator InvalidSwapReturnsBothPiecesHome()
        {
            var a = MakePiece("A", Vector3.zero);
            var b = MakePiece("B", Vector3.right);
            bool done = false;
            runner.StartCoroutine(Run(Match3PieceMotion.InvalidSwap(a.transform, b.transform, Vector3.zero, Vector3.right), () => done = true));
            yield return Until(() => done, 2f);
            Assert.AreEqual(Vector3.zero, a.transform.position);
            Assert.AreEqual(Vector3.right, b.transform.position);
            Object.Destroy(a.gameObject);
            Object.Destroy(b.gameObject);
        }

        static IEnumerator Run(IEnumerator body, Action done)
        {
            yield return body;
            done();
        }

        // ------------------------------------------------------------------ feedback

        [UnityTest]
        public IEnumerator MatchGroupReturnsIntensityAndPlaysCentreAccent()
        {
            var positions = new[] { Vector3.zero, Vector3.right, Vector3.right * 2f, Vector3.right * 3f };
            float intensity = Match3Vfx.PlayMatchGroup(positions, Color.red, 4, 2);
            Assert.AreEqual(1f + settings.fourMatchBonus + settings.cascadeBonusPerStep, intensity, 0.0001f);
            yield return new WaitForSeconds(settings.groupAccentDelay + 0.05f);
            Assert.Greater(Match3Vfx.ExistingRig.Burst.ActiveOverlays, 0, "centre ring did not play");
        }

        [UnityTest]
        public IEnumerator HitStopRestoresTimeScale()
        {
            Match3Vfx.RequestHitStop(0f, 0.1f, 0.1f);
            yield return null;
            yield return null;
            Assert.AreEqual(0.1f, Time.timeScale, 0.001f);
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.AreEqual(1f, Time.timeScale, 0.001f);
        }

        [UnityTest]
        public IEnumerator ComboBannerUsesThresholdCooldownAndNeverBlocksInput()
        {
            Assert.IsFalse(Match3Vfx.ShowComboBanner(2));
            Assert.IsTrue(Match3Vfx.ShowComboBanner(3));
            Assert.IsFalse(Match3Vfx.ShowComboBanner(5), "cooldown ignored");
            var rig = Match3Vfx.ExistingRig;
            Assert.AreEqual("GREAT!", rig.Banner.CurrentText);
            Assert.IsFalse(rig.Banner.Group.blocksRaycasts);
            Assert.AreEqual(0, rig.GetComponentsInChildren<GraphicRaycaster>(true).Length);
            yield return new WaitForSeconds(ComboBanner.Lifetime + 0.1f);
            Assert.IsFalse(rig.Banner.IsShowing);
        }

        [UnityTest]
        public IEnumerator SoundPoolStaysWithinItsVoiceCap()
        {
            settings.soundVoices = 4;
            host.soundEnabled = true;
            var clip = AudioClip.Create("silence", 44100, 1, 44100, false);
            Match3Vfx.Prewarm();
            var rig = Match3Vfx.ExistingRig;
            for (int i = 0; i < 10; i++) Match3Vfx.PlayClip(clip, 1f, 150);
            yield return null;
            Assert.AreEqual(4, rig.Sfx.Capacity);
            Assert.LessOrEqual(rig.Sfx.ActiveVoices, 4);
            Object.Destroy(clip);
        }
    }
}
