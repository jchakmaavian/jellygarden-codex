using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using JellyGarden.Scripts;
using JellyGarden.Scripts.Juice;
using RoyalThrone.ReferenceGameplay;
using RoyalThrone.Testing;
using UnityEngine;

namespace RoyalThrone.Editor
{
    public static class RoyalRocketFlameGapChecks
    {
        const string Folder = "Design/Effects/Rocket-flame-gap-fix/";
        static int checks;
        static bool complete;
        static string failure;
        static readonly FieldInfo Flyers = typeof(RoyalPowerupDirector).GetField("flyers", BindingFlags.Instance | BindingFlags.NonPublic);
        public static void Run()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
            Directory.CreateDirectory(Folder);
            checks = 0; complete = false; failure = "Interrupted before completion";
            File.WriteAllText(Folder + "result.txt", "RUNNING");
            File.WriteAllText(Folder + "progress.txt", "");
            RoyalRegressionRunner.Run(Suite());
        }
        static void Require(bool condition, string message)
        {
            if (!condition) { failure = message; throw new Exception(message); }
            checks++;
            File.AppendAllText(Folder + "progress.txt", message + "\n");
        }
        static IEnumerator Suite()
        {
            var qa = RoyalQaService.Instance;
            bool created = !qa;
            if (!qa) qa = new GameObject("Rocket gap QA").AddComponent<RoyalQaService>();
            bool enabled = qa.enabled, visible = qa.Visible;
            float capture = Time.captureDeltaTime, clock = Time.timeScale, speed = qa.Speed;
            int money = InitScript.Gems, lives = InitScript.lifes;
            if (RoyalSave.Testing) throw new InvalidOperationException("Finish the existing QA session first.");
            try
            {
                qa.StartSession();
                Require(RoyalSave.Testing, "Isolated save profile active");
                qa.Visible = false; qa.Speed = 1;
                foreach (int fps in new[] { 15, 30, 60 })
                foreach (bool horizontal in new[] { true, false })
                {
                    Time.timeScale = 1; Time.captureDeltaTime = 1f / fps;
                    qa.enabled = true;
                    yield return qa.Practice();
                    yield return qa.Settled();
                    qa.enabled = false;
                    var lm = LevelManager.THIS;
                    var fx = RoyalPowerupDirector.Get;
                    var item = lm.GetSquare(3,3).item;
                    item.NextType = horizontal ? ItemsTypes.HORIZONTAL_STRIPPED : ItemsTypes.VERTICAL_STRIPPED;
                    item.ChangeType();
                    yield return new WaitForSeconds(.65f);
                    lm.DragBlocked = false;
                    int moves = lm.Limit, impacts = fx.RocketCellImpacts;
                    string name = (horizontal ? "horizontal" : "vertical") + "-" + fps;
                    Require(RoyalPowerupDirector.Tap(item), name + ": shipping tap accepted");
                    Require(lm.Limit == moves - 1, name + ": exactly one move charged");
                    float start = Time.time, deadline = Time.realtimeSinceStartup + 15;
                    int frames = 0; bool paused = false;
                    int flightFrames = 0;
                    do
                    {
                        yield return new WaitForEndOfFrame();
                        frames++;
                        var flyers = (Array)Flyers.GetValue(fx);
                        foreach (var flyer in flyers)
                        {
                            var type = flyer.GetType();
                            var sr = (SpriteRenderer)type.GetField("sprite").GetValue(flyer);
                            var handle = (ReferenceVfxPool.Handle)type.GetField("nativeRocket").GetValue(flyer);
                            if (!sr || !sr.enabled || !sr.gameObject.activeInHierarchy || handle == null || !handle.Valid) continue;
                            flightFrames++;
                            float pitch = Mathf.Max(sr.bounds.size.x, sr.bounds.size.y);
                            foreach (var ps in handle.GameObject.GetComponentsInChildren<ParticleSystem>())
                            {
                                if (ps.name != "add_fire" && ps.name != "add_fire_core") continue;
                                var particles = new ParticleSystem.Particle[ps.main.maxParticles];
                                int count = ps.GetParticles(particles);
                                float gap = float.PositiveInfinity;
                                for (int k = 0; k < count; k++) gap = Mathf.Min(gap, Vector3.Distance(particles[k].position, sr.transform.position));
                                Require(gap < pitch * .08f, name + ": visible native flame reaches current nozzle");
                            }
                        }
                        if (fps == 30 && frames >= 5 && frames <= 20)
                        {
                            var image = ScreenCapture.CaptureScreenshotAsTexture();
                            File.WriteAllBytes(Folder + name + "-" + frames.ToString("D2") + ".png", image.EncodeToPNG());
                            UnityEngine.Object.Destroy(image);
                        }
                        if (!paused && fps == 30 && horizontal && flightFrames > 0)
                        {
                            paused = true;
                            var transforms = fx.GetComponentsInChildren<SpriteRenderer>().Where(s => s.enabled && s.gameObject.activeInHierarchy).Select(s => s.transform).ToArray();
                            var positions = transforms.Select(t => t.position).ToArray();
                            int count = ReferenceVfxPool.ParticleCount;
                            lm.gameStatus = GameState.Pause; Time.timeScale = 0;
                            yield return new WaitForSecondsRealtime(.12f);
                            Require(transforms.Select((t,k) => !t || Vector3.Distance(t.position, positions[k]) < .001f).All(b => b), "Pause freezes rocket motion");
                            Require(ReferenceVfxPool.ParticleCount == count, "Pause does not add flame particles");
                            lm.gameStatus = GameState.Playing; Time.timeScale = 1;
                        }
                    } while ((RoyalPowerupDirector.Busy || lm.IsResolvingMatches || ReferenceVfxPool.Active > 0) && Time.realtimeSinceStartup < deadline);
                    Require(flightFrames >= 2, name + ": both native rocket heads were observed");
                    yield return qa.Settled();
                    Require(fx.RocketCellImpacts > impacts, name + ": actual row/column impacts occurred");
                    Require(lm.GetItems().Where(i => i).All(i => !i.falling && !i.destroying && i.square.item == i), name + ": refill settles with valid item ownership");
                    Require(ReferenceVfxPool.Active == 0, name + ": pooled native effects retire");
                    Require(Time.realtimeSinceStartup < deadline, name + ": no animation/board hang");
                }
                complete = true;
            }
            finally
            {
                Time.captureDeltaTime = capture; Time.timeScale = clock; qa.Speed = speed;
                RoyalPowerupDirector.Get.Cancel();
                qa.StartCoroutine(Restore(qa, enabled, visible, created, money, lives));
            }
        }
        static IEnumerator Restore(RoyalQaService qa, bool enabled, bool visible, bool created, int money, int lives)
        {
            yield return qa.EndSession();
            bool restored = !RoyalSave.Testing && InitScript.Gems == money && InitScript.lifes == lives;
            qa.Visible = visible; qa.enabled = enabled;
            File.WriteAllText(Folder + "result.txt", (complete && restored ? "PASS " : "FAIL ") + checks + " checks; player profile restored=" + restored + (complete ? "" : "; " + failure));
            if (created) UnityEngine.Object.Destroy(qa.gameObject);
        }
    }
}
