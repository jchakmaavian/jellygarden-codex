#if UNITY_EDITOR && !ROYAL_DISABLE_QA
using System;
using System.Collections;
using System.IO;
using System.Linq;
using JellyGarden.Scripts;
using JellyGarden.Scripts.Juice;
using RoyalThrone.Testing;
using UnityEditor;
using UnityEngine;

namespace RoyalThrone.Editor
{
    public static class RoyalTwinTntArtChecks
    {
        const string Folder = "Design/TwinTNT-Art-Fix/";
        static void Check(bool ok, string message)
        {
            File.AppendAllText(Folder + "checks.txt", (ok ? "PASS " : "FAIL ") + message + "\n");
            if (!ok) throw new InvalidOperationException(message);
        }
        [MenuItem("Tools/Royal Throne/QA/Verify twin TNT Royal artwork")]
        public static void Run() => RoyalRegressionRunner.Run(Suite());
        static IEnumerator Suite()
        {
            Directory.CreateDirectory(Folder);
            File.WriteAllText(Folder + "checks.txt", "Royal Throne twin TNT artwork verification\n");
            var qa = RoyalQaService.Instance;
            Check(Application.isPlaying && qa && qa.Ready && !qa.Busy && !RoyalSave.Testing, "Ready normal profile");
            int coins = InitScript.Gems, lives = InitScript.lifes;
            try
            {
                qa.StartSession(); qa.Level = 6;
                var source = Resources.Load<GameObject>("ReferenceGameplay/Visuals/TNTTNTCombo");
                var expected = source.GetComponentsInChildren<SpriteRenderer>(true).ToDictionary(r => r.name, r => r.sprite);
                for (int pass = 0; pass < 2; pass++)
                {
                    yield return qa.Practice(); qa.Visible = false;
                    var lm = LevelManager.THIS;
                    var a = lm.GetSquare(3, 3).item; var b = lm.GetSquare(4, 3).item;
                    a.NextType = b.NextType = ItemsTypes.PACKAGE; a.ChangeType(); b.ChangeType();
                    yield return new WaitForSeconds(1.5f);
                    var director = RoyalPowerupDirector.Get; int plays = director.NativeTntComboPlays;
                    Check(RoyalPowerupDirector.TrySwap(a, b), "Real twin TNT swap starts, pass " + pass);
                    int visible = 0; bool art = true; float deadline = Time.realtimeSinceStartup + 20;
                    while ((RoyalPowerupDirector.Busy || visible == 0) && Time.realtimeSinceStartup < deadline)
                    {
                        yield return new WaitForEndOfFrame();
                        var view = UnityEngine.Object.FindObjectsByType<RoyalThrone.ReferenceGameplay.ReferenceActivationVisual>()
                            .FirstOrDefault(v => v.Active && v.Socket("TntKemer").name == "TntKemer");
                        if (!view) continue;
                        var root = view.Socket("TntKemer").parent;
                        art &= root.GetComponentsInChildren<SpriteRenderer>(true).All(r => expected.TryGetValue(r.name, out var sprite) && r.sprite == sprite);
                        if (++visible == 25)
                        {
                            var image = ScreenCapture.CaptureScreenshotAsTexture();
                            File.WriteAllBytes(Folder + "combo-" + pass + ".png", image.EncodeToPNG());
                            UnityEngine.Object.Destroy(image);
                        }
                    }
                    Check(visible > 25 && art, "Sun band and Royal barrel/fuse artwork retained throughout animation, pass " + pass);
                    Check(director.NativeTntComboPlays == plays + 1 && !RoyalPowerupDirector.Busy, "Dedicated combo completes once, pass " + pass);
                    Check(Mathf.Abs(director.LastTntComboPreparation - 1.583333f) < .0001f, "Authored fuse duration preserved");
                }
                yield return qa.EndSession();
                Check(!RoyalSave.Testing && InitScript.Gems == coins && InitScript.lifes == lives, "Normal player data restored");
                File.AppendAllText(Folder + "checks.txt", "ALL CHECKS PASSED\n");
            }
            finally { RoyalPowerupDirector.Get?.Cancel(); if (RoyalSave.Testing) RoyalRegressionRunner.Run(qa.EndSession()); }
        }
    }
}
#endif
