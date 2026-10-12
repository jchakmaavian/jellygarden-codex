#if UNITY_EDITOR && !ROYAL_DISABLE_QA
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JellyGarden.Scripts;
using JellyGarden.Scripts.Juice;
using RoyalThrone.ReferenceGameplay;
using RoyalThrone.Testing;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RoyalThrone.Editor
{
    public static class RoyalDualColorBallChecks
    {
        const string Folder = "Logs/port-checks/DualColorBall-Reference/Verification/";
        static readonly List<string> notes = new List<string>();
        static readonly List<string> errors = new List<string>();
        public static bool Running { get; private set; }
        static void Check(bool ok, string message)
        {
            notes.Add((ok ? "PASS " : "FAIL ") + message);
            File.WriteAllLines(Folder + "checks.txt", notes);
            if (!ok) throw new InvalidOperationException(message);
        }
        static void Log(string message, string trace, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert) errors.Add(message);
        }
        [MenuItem("Tools/Royal Throne/QA/Verify native dual Colorball")]
        public static void Run()
        {
            var qa = RoyalQaService.Instance;
            if (Running || !Application.isPlaying || !qa || !qa.Ready || qa.Busy || RoyalSave.Testing) throw new InvalidOperationException("Ready game and normal player profile required.");
            Running = true; notes.Clear(); errors.Clear(); Directory.CreateDirectory(Folder);
            RoyalRegressionRunner.Run(Suite(qa));
        }
        static IEnumerator Suite(RoyalQaService qa)
        {
            float capture = Time.captureDeltaTime; int fpsLimit = Application.targetFrameRate, vsync = QualitySettings.vSyncCount;
            int coins = InitScript.Gems, lives = InitScript.lifes;
            int sound = RoyalSave.GetInt("Sound", 1);
            bool complete = false;
            Application.logMessageReceived += Log;
            try
            {
                qa.StartSession(); qa.Level = 6; qa.Visible = false;
                Check(RoyalSave.Testing, "Player profile isolated before fixture changes");
                foreach (int fps in new[] { 15, 30, 60 })
                {
                    Time.captureDeltaTime = 1f / fps; Application.targetFrameRate = fps; QualitySettings.vSyncCount = 0;
                    yield return qa.Practice(); qa.Visible = false;
                    var lm = LevelManager.THIS; var director = RoyalPowerupDirector.Get;
                    var a = lm.GetSquare(3, 3).item; var b = lm.GetSquare(4, 3).item;
                    a.NextType = b.NextType = ItemsTypes.BOMB; a.ChangeType(); b.ChangeType();
                    yield return new WaitForSeconds(1);
                    var original = lm.GetItems().Where(i => i && i != a && i != b).ToArray();
                    int plays = director.NativeDualColorBallPlays; int oldLimit = lm.Limit;
                    Check(RoyalPowerupDirector.TrySwap(a, b), fps + " FPS: real power-up pair starts");
                    int frame = 0; bool paused = false, rendered = false, chargeVisible = false, blastVisible = false;
                    float deadline = Time.realtimeSinceStartup + 25;
                    while ((RoyalPowerupDirector.Busy || frame == 0) && Time.realtimeSinceStartup < deadline)
                    {
                        yield return (UnityEngine.Application.isBatchMode?null:new WaitForEndOfFrame());
                        var actor = Object.FindObjectsByType<ReferenceDualColorBallVisual>().FirstOrDefault(v => v.Active);
                        if (actor && director.DualColorBallClearFrame < 0)
                        {
                            Check(original.All(i => i && !i.destroying), fps + " FPS frame " + frame + ": no board damage during authored preparation");
                            rendered |= actor.GetComponentsInChildren<SpriteRenderer>().Any(r => r.enabled && !r.forceRenderingOff && r.sprite && r.gameObject.activeInHierarchy);
                            chargeVisible |= Object.FindObjectsByType<ParticleSystem>().Any(p => p.name == "add_energy_lines" && p.particleCount > 0);
                            if (!paused && actor.Elapsed > .6f)
                            {
                                lm.gameStatus = GameState.Pause;
                                yield return null; yield return (UnityEngine.Application.isBatchMode?null:new WaitForEndOfFrame());
                                var pos = actor.GetComponentsInChildren<SpriteRenderer>().Select(r => r.transform.position).ToArray(); float time = actor.Elapsed;
                                yield return null; yield return (UnityEngine.Application.isBatchMode?null:new WaitForEndOfFrame()); yield return null; yield return (UnityEngine.Application.isBatchMode?null:new WaitForEndOfFrame());
                                Check(Mathf.Abs(actor.Elapsed - time) < .00001f && pos.SequenceEqual(actor.GetComponentsInChildren<SpriteRenderer>().Select(r => r.transform.position)), fps + " FPS: body and sequence pause together");
                                Check(Object.FindObjectsByType<ParticleSystem>().Where(p => p.name == "add_energy_lines" || p.name == "add_energy_dots").All(p => !p.isPlaying), fps + " FPS: native charging VFX pause");
                                lm.gameStatus = GameState.Playing; paused = true;
                            }
                        }
                        blastVisible |= Object.FindObjectsByType<ParticleSystem>().Any(p => p.name == "add_ring" && p.particleCount > 0);
                        if (fps == 60 && frame % 2 == 0 && !UnityEngine.Application.isBatchMode)
                        {
                            string frames = Folder + "Frames/"; Directory.CreateDirectory(frames);
                            var image = ScreenCapture.CaptureScreenshotAsTexture();
                            File.WriteAllBytes(frames + (frame / 2).ToString("D4") + ".png", image.EncodeToPNG()); Object.Destroy(image);
                        }
                        frame++;
                    }
                    Check(!RoyalPowerupDirector.Busy && director.NativeDualColorBallPlays == plays + 1, fps + " FPS: native sequence completes once");
                    Check(Mathf.Abs(director.DualColorBallPreparation - 2.883333f) < .0001f, fps + " FPS: recovered clip duration retained");
                    Check(rendered && chargeVisible && blastVisible, fps + " FPS: native bodies, charging particles and blast render");
                    Check(director.DualColorBallHits == 64 && director.DualColorBallClearFrame >= 0, fps + " FPS: every fixture cell hit on one blast frame");
                    Check(original.All(i => !i || i.destroying), fps + " FPS: all original ordinary gems cleared");
                    Check(lm.Limit == oldLimit, fps + " FPS: presentation adds no move debit (swap owns accounting)");
                    float tailDeadline = Time.realtimeSinceStartup + 10;
                    while ((ReferenceDualColorBallVisual.ActiveCount > 0 || ReferenceVfxPool.Active > 0) && Time.realtimeSinceStartup < tailDeadline) yield return null;
                    Check(ReferenceDualColorBallVisual.ActiveCount == 0 && ReferenceActivationVisual.PlayingVoices == 0 && ReferenceVfxPool.Active == 0, fps + " FPS: native sound and all particle tails retire");
                    director.Cancel();
                }
                Time.captureDeltaTime = 1f / 60;
                yield return qa.Practice(); qa.Visible = false;
                var level = LevelManager.THIS; var power = RoyalPowerupDirector.Get;
                // Leave one ingredient and one unbreakable cell in a sparse fixture.
                var ingredient = level.GetSquare(0, 0).item; ingredient.currentType = ItemsTypes.INGREDIENT;
                level.GetSquare(7, 7).type = SquareTypes.UNDESTROYABLE;
                var protectedItem = level.GetSquare(7, 7).item;
                for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++)
                    if (y == 6 && x < 4) level.GetSquare(x, y).type = SquareTypes.NONE;
                var first = level.GetSquare(3, 3).item; var second = level.GetSquare(3, 4).item;
                first.NextType = second.NextType = ItemsTypes.BOMB; first.ChangeType(); second.ChangeType();
                yield return new WaitForSeconds(1);
                Check(RoyalPowerupDirector.TrySwap(first, second), "Vertical pair starts on sparse board");
                float end = Time.realtimeSinceStartup + 20;
                while (RoyalPowerupDirector.Busy && Time.realtimeSinceStartup < end) yield return null;
                Check(!RoyalPowerupDirector.Busy && ingredient && !ingredient.destroying && protectedItem && !protectedItem.destroying, "Sparse board keeps ingredient and unbreakable rules");
                power.Cancel();
                yield return qa.Practice(); qa.Visible = false;
                first = level.GetSquare(3, 3).item; second = level.GetSquare(4, 3).item;
                first.NextType = second.NextType = ItemsTypes.BOMB; first.ChangeType(); second.ChangeType();
                yield return new WaitForSeconds(1);
                RoyalSave.SetInt("Sound", 0);
                Check(RoyalPowerupDirector.TrySwap(first, second), "Muted dual pair starts");
                yield return null; yield return null;
                Check(ReferenceActivationVisual.PlayingVoices == 0, "Native combo obeys muted sound preference");
                var remaining = level.GetItems().Where(i => i).ToArray(); power.Cancel(); yield return null;
                Check(!RoyalPowerupDirector.Busy && ReferenceDualColorBallVisual.ActiveCount == 0 && ReferenceVfxPool.Active == 0 && ReferenceActivationVisual.ActiveCount == 0, "Mid-charge cancellation clears actors, VFX and voice leases");
                Check(remaining.All(i => !i || i.inputConversionLocks == 0), "Mid-charge cancellation releases every target input lock");
                RoyalSave.SetInt("Sound", sound);
                yield return qa.EndSession();
                Check(!RoyalSave.Testing && InitScript.Gems == coins && InitScript.lifes == lives, "Player progress, coins and lives restored");
                Check(errors.Count == 0, "No new runtime errors");
                complete = true;
            }
            finally
            {
                RoyalPowerupDirector.Get?.Cancel();
                Time.captureDeltaTime = capture; Application.targetFrameRate = fpsLimit; QualitySettings.vSyncCount = vsync;
                Application.logMessageReceived -= Log; Running = false;
                if (RoyalSave.Testing) RoyalRegressionRunner.Run(qa.EndSession());
                notes.Add(complete ? "ALL DUAL COLORBALL CHECKS PASSED" : "DUAL COLORBALL CHECKS INCOMPLETE");
                File.WriteAllLines(Folder + "checks.txt", notes);
            }
        }
    }
}
#endif
