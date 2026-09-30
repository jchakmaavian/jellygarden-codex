#if UNITY_EDITOR && !ROYAL_DISABLE_QA
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JellyGarden.Scripts;
using JellyGarden.Scripts.Juice;
using RoyalThrone.Testing;
using UnityEditor;
using UnityEngine;

namespace RoyalThrone.ReferenceGameplay.Editor
{
    [InitializeOnLoad]
    public static class ReferenceRfv387Checks
    {
        const string Folder = "Design/Gameplay-rfv387/Validation";
        const string Inbox = "Design/Gameplay-rfv387/editor-request.txt";
        static double next;
        static bool running, recording;
        static RoyalQaService qa;
        static int errors, frame;
        static float largestFrame;
        static string captureCase;
        static float captureStart;
        static readonly List<string> lines = new List<string>();
        static ReferenceRfv387Checks() { EditorApplication.update += Poll; }
        static void Poll()
        {
            if (running || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.timeSinceStartup < next) return;
            next = EditorApplication.timeSinceStartup + .5;
            if (!File.Exists(Inbox)) return;
            string command = File.ReadAllText(Inbox).Trim(); File.Delete(Inbox);
            Directory.CreateDirectory(Folder);
            try
            {
                if (command == "import-fuse")
                {
                    if (EditorApplication.isPlaying) throw new InvalidOperationException("Import requires Edit Mode.");
                    ReferenceRfv387FuseImporter.Build();
                    File.WriteAllText(Folder + "/command.txt", "PASS import-fuse");
                }
                else if (command == "propeller") RoyalThrone.Boosters.Propeller.V5.Editor.PropellerReferenceChecks.Run();
                else if (command == "checks" || command == "capture" || command == "fall-color")
                {
                    qa = RoyalQaService.Instance;
                    if (!EditorApplication.isPlaying || !qa || !qa.Ready || qa.Busy || RoyalSave.Testing)
                        throw new InvalidOperationException("Ready Home and normal profile required before checks.");
                    running = true; lines.Clear(); errors = 0;
                    RoyalThrone.Editor.RoyalRegressionRunner.Run(command == "fall-color" ? FallColor() : Run(command == "capture"));
                }
                else throw new InvalidOperationException("Unknown rfv387 check command: " + command);
            }
            catch (Exception error) { File.WriteAllText(Folder + "/command.txt", "FAIL " + error); Debug.LogException(error); }
        }
        static void Note(string text) { lines.Add(text); File.WriteAllLines(Folder + "/rocket-tnt-checks.txt", lines); }
        static void Check(bool ok, string text) { Note((ok ? "PASS " : "FAIL ") + text); if (!ok) throw new InvalidOperationException(text); }
        static void Log(string message, string trace, LogType type)
        { if (type == LogType.Error || type == LogType.Exception) { errors++; Note("ERROR " + message); } }
        static IEnumerator Observe()
        {
            while (running)
            {
                largestFrame = Mathf.Max(largestFrame, Time.unscaledDeltaTime);
                yield return null;
            }
        }
        static IEnumerator Capture()
        {
            float last = -1;
            while (recording)
            {
                yield return new WaitForEndOfFrame();
                if (Time.unscaledTime - last < 1f / 30) continue;
                last = Time.unscaledTime;
                var source = ScreenCapture.CaptureScreenshotAsTexture();
                int width = 540, height = Mathf.RoundToInt(source.height * (540f / source.width));
                var rt = RenderTexture.GetTemporary(width, height, 0);
                Graphics.Blit(source, rt); var previous = RenderTexture.active; RenderTexture.active = rt;
                var image = new Texture2D(width, height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply();
                string dir = Folder + "/Frames"; Directory.CreateDirectory(dir);
                File.WriteAllBytes(dir + "/" + frame.ToString("D5") + ".jpg", image.EncodeToJPG(85));
                File.AppendAllText(Folder + "/capture.csv", frame + "," + (Time.unscaledTime - captureStart).ToString("F5", System.Globalization.CultureInfo.InvariantCulture) + "," + captureCase + "\n");
                frame++; RenderTexture.active = previous; RenderTexture.ReleaseTemporary(rt);
                UnityEngine.Object.Destroy(image); UnityEngine.Object.Destroy(source);
            }
        }
        static IEnumerator Fresh()
        {
            yield return qa.Practice(); qa.Visible = false;
            qa.Column = qa.Row = 3;
            Check(LevelManager.THIS.GetItems().All(i => !i || !i.falling), "Fixture settled");
        }
        static IEnumerator FallColor()
        {
            try
            {
                qa.StartSession(); qa.Level = 6;
                yield return RoyalThrone.Editor.ReferenceFallColorChecks.Run();
            }
            finally
            {
                running = false;
                if (RoyalSave.Testing) RoyalThrone.Editor.RoyalRegressionRunner.Run(qa.EndSession());
            }
        }
        static IEnumerator Run(bool capture)
        {
            int target = Application.targetFrameRate, vsync = QualitySettings.vSyncCount;
            int coins = InitScript.Gems, lives = InitScript.lifes;
            bool passed = false;
            Application.logMessageReceived += Log;
            try
            {
                Note("UTC " + DateTime.UtcNow.ToString("o"));
                qa.StartSession(); qa.Level = 6; qa.Visible = false;
                Check(RoyalSave.Testing, "Isolated save active");
                qa.StartCoroutine(Observe());
                if (capture)
                {
                    frame = 0; captureStart = Time.unscaledTime;
                    File.WriteAllText(Folder + "/capture.csv", "frame,time,case\n");
                    recording = true; qa.StartCoroutine(Capture());
                }
                foreach (int fps in capture ? new[] { 60 } : new[] { 30, 60 })
                {
                    QualitySettings.vSyncCount = 0; Application.targetFrameRate = fps;
                    foreach (bool horizontal in new[] { true, false })
                    {
                        yield return Fresh(); captureCase = "Rocket " + (horizontal ? "H" : "V") + " " + fps;
                        yield return qa.Execute(horizontal ? "Spawn rocket H" : "Spawn rocket V");
                        var director = RoyalPowerupDirector.Get;
                        int impacts = director.RocketCellImpacts, exits = director.RocketScreenExits;
                        largestFrame = 0; float started = Time.unscaledTime; int startFrame = Time.frameCount;
                        yield return qa.Execute("Activate selected");
                        float seconds = Time.unscaledTime - started;
                        Check(director.RocketCellImpacts - impacts >= 8 && director.RocketScreenExits - exits >= 2, captureCase + " sequential cells and two screen exits");
                        Note(captureCase + " meanFPS=" + ((Time.frameCount-startFrame)/seconds).ToString("F1") + " worstFrameMs=" + (largestFrame*1000).ToString("F1") + " flightWorstMs=" + (director.LastRocketMaxFlightFrame*1000).ToString("F1") + " lastVfxInstantiateMs=" + ReferenceVfxPool.LastInstantiationMilliseconds.ToString("F2") + " anticipation=" + director.LastRocketAnticipation + " sweep=" + director.LastRocketSweepDuration);
                        Check(LevelManager.THIS.BoardInputAllowed && !RoyalPowerupDirector.Busy, "Rocket refill and input recovered");
                    }
                    yield return Fresh(); captureCase = "TNT creation " + fps;
                    var item = LevelManager.THIS.GetSquare(3, 3).item;
                    item.NextType = ItemsTypes.PACKAGE; item.ChangeType(); yield return null;
                    var native = item.GetComponent<ReferencePieceVisual>();
                    Check(native && native.Revealing, "TNT plays native creation");
                    yield return new WaitForSeconds(.5f);
                    Check(native && native.Revealing, "TNT creation is not truncated to a short pop");
                    yield return new WaitForSeconds(1.05f);
                    Check(native && !native.Revealing, "TNT settles to native Idle");
                    captureCase = "TNT explosion " + fps;
                    yield return qa.Execute("Activate selected");
                    Check(LevelManager.THIS.BoardInputAllowed, "TNT damage/refill and input recovered");
                    yield return Fresh(); captureCase = "TNT TNT " + fps;
                    int combo = RoyalPowerupDirector.Get.NativeTntComboPlays;
                    qa.PowerA = qa.PowerB = 0;
                    yield return qa.Execute("Combine selected");
                    Check(RoyalPowerupDirector.Get.NativeTntComboPlays == combo + 1, "One dedicated TNT+TNT native animation");
                    Check(Mathf.Abs(RoyalPowerupDirector.Get.LastTntComboPreparation - 1.583333f) < .002f, "Combo keeps source clip timing");
                    Check(LevelManager.THIS.BoardInputAllowed, "TNT+TNT cascades/refill/input recovered");
                    yield return new WaitForSeconds(2);
                    Check(ReferenceActivationVisual.ActiveCount == 0, "Native activation visuals released");
                }
                Check(errors == 0, "No new runtime errors"); passed = true;
            }
            finally
            {
                if (!passed && LevelManager.THIS)
                {
                    var lm = LevelManager.THIS;
                    Note("UNSETTLED " + captureCase + " state=" + lm.gameStatus + " resolve=" + lm.IsResolvingMatches + " drag=" + lm.DragBlocked + " powers=" + RoyalPowerupDirector.Get.ActiveOperations + " falling=" + lm.GetItems().Count(i=>i&&i.falling) + " destroying=" + lm.GetItems().Count(i=>i&&i.destroying));
                    Note("DETAIL inputSwaps="+lm.InputSwaps+" level="+lm.currentLevel+" campaign="+RoyalThrone.Campaign.RoyalReferenceCampaign.Active+" matches="+lm.GetMatches().Count+" colours="+lm.colorLimit+" rand="+lm.lastRandColor+" scale="+Time.timeScale+" time="+Time.time+" fixedTime="+Time.fixedTime+" fixedDelta="+Time.fixedDeltaTime);
                    foreach(var group in lm.GetMatches().Take(3)) Note("matched " + string.Join(" / ",group.Where(i=>i).Select(i=>i.color+":"+i.currentType+":"+i.NextType+"@"+i.square.col+","+i.square.row)));
                    foreach(var i in lm.GetItems().Where(i=>i&&(i.falling||i.destroying))) Note("pending " + i.name + " " + i.currentType + " cell=" + (i.square ? i.square.col+","+i.square.row : "none") + " falling=" + i.falling + " destroying=" + i.destroying);
                }
                recording = false; running = false; Application.logMessageReceived -= Log;
                Application.targetFrameRate = target; QualitySettings.vSyncCount = vsync;
                if (RoyalSave.Testing) RoyalThrone.Editor.RoyalRegressionRunner.Run(Restore(coins, lives, passed));
            }
        }
        static IEnumerator Restore(int coins, int lives, bool passed)
        {
            yield return qa.EndSession();
            Check(!RoyalSave.Testing && InitScript.Gems == coins && InitScript.lifes == lives, "Normal save restored");
            Note(passed ? "ALL CHECKS PASSED" : "INCOMPLETE — inspect first failure");
        }
    }
}
#endif
