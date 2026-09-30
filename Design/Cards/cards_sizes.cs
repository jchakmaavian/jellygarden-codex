// Every card at other screen shapes (existing Game view presets only), in the QA profile: the crest, the card and each
// control stay on screen. Captures each; restores the user's 1080x1920 preset (index 20).
// Run in Play Mode on the map: unity command eval_file --file Design/Cards/cards_sizes.cs (see Design/Cards/README.md).
var dir = global::System.IO.Path.GetFullPath("Temp/RoyalCards/sizes");
global::System.IO.Directory.CreateDirectory(dir);
var log = new global::System.Text.StringBuilder();
var lm = JellyGarden.Scripts.LevelManager.THIS;
var menus = JellyGarden.Scripts.GUI.MenuManager.Instance;
var root = menus.GetCanvasGlobal();
var qa = RoyalThrone.Testing.RoyalQaService.Instance;
var asm = typeof(UnityEditor.EditorWindow).Assembly;
var gameViewType = asm.GetType("UnityEditor.GameView");
var prop = gameViewType.GetProperty("selectedSizeIndex", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
void Select(int index) { foreach (var v in UnityEngine.Resources.FindObjectsOfTypeAll(gameViewType)) prop.SetValue(v, index); }
int fails = 0;
string Inside(string name, UnityEngine.RectTransform r)
{
    var c = new UnityEngine.Vector3[4]; r.GetWorldCorners(c);
    bool ok = c[0].x >= -1 && c[0].y >= -1 && c[2].x <= UnityEngine.Screen.width + 1 && c[2].y <= UnityEngine.Screen.height + 1;
    if (!ok) fails++;
    return (ok ? "PASS " : "FAIL ") + name + " " + c[0].x.ToString("F0") + "," + c[0].y.ToString("F0") + ".." + c[2].x.ToString("F0") + "," + c[2].y.ToString("F0");
}
global::System.Collections.IEnumerator Wait(float seconds) { float end = UnityEngine.Time.realtimeSinceStartup + seconds; while (UnityEngine.Time.realtimeSinceStartup < end) yield return null; }
global::System.Collections.IEnumerator Run()
{
    yield return null;
    string before = UnityEngine.PlayerPrefs.GetInt("OpenLevel") + "/" + UnityEngine.PlayerPrefs.GetInt("Lifes") + "/" + UnityEngine.PlayerPrefs.GetInt("Gems");
    qa.StartSession(); qa.Visible = false; yield return null;
    JellyGarden.Scripts.InitScript.Instance.TryOpenLevel(23); yield return null; menus.menuPlay.SetActive(false); yield return Wait(.3f);
    try
    {
        foreach (int preset in new[] { 19, 11, 21 })
        {
            Select(preset);
            for (int f = 0; f < 6; f++) yield return null;
            log.AppendLine("preset " + preset + " screen " + UnityEngine.Screen.width + "x" + UnityEngine.Screen.height);
            foreach (var name in new[] { "MenuPlay", "GemsShop", "LiveShop", "BoostShop", "BoostInfo", "Settings", "Tutorial", "Reward", "MenuComplete", "MenuFailed", "PreFailed", "MenuPause" })
            {
                var menu = root.Find(name);
                var card = name == "PreFailed" ? menu.Find("Banner") : menu.Find("Image");
                if (name == "MenuPlay") JellyGarden.Scripts.InitScript.Instance.TryOpenLevel(23);
                else if (name == "BoostShop") menu.GetComponent<JellyGarden.Scripts.GUI.BoostShop>().SetBoost(JellyGarden.Scripts.GUI.BoostType.Stripes);
                else menu.gameObject.SetActive(true);
                yield return Wait(name == "PreFailed" ? .9f : .7f);
                log.AppendLine("  " + name + ": " + Inside("crest", (UnityEngine.RectTransform)card.Find("Royal skin/Crest")) + " | " + Inside("card", (UnityEngine.RectTransform)card));
                var controls = new global::System.Collections.Generic.List<UnityEngine.Transform>();
                foreach (var b in card.GetComponentsInChildren<UnityEngine.UI.Button>()) controls.Add(b.transform);
                if (name == "PreFailed") foreach (var n in new[] { "Buy", "GiveUp" }) controls.Add(menu.Find(n));
                foreach (var t in controls) { var line = Inside(t.name, (UnityEngine.RectTransform)t); if (line.StartsWith("FAIL")) log.AppendLine("    " + line); }
                yield return new UnityEngine.WaitForEndOfFrame();
                var tex = UnityEngine.ScreenCapture.CaptureScreenshotAsTexture();
                global::System.IO.File.WriteAllBytes(global::System.IO.Path.Combine(dir, UnityEngine.Screen.width + "x" + UnityEngine.Screen.height + "-" + name + ".png"), UnityEngine.ImageConversion.EncodeToPNG(tex));
                UnityEngine.Object.Destroy(tex);
                if (name == "MenuPlay") menu.GetComponent<JellyGarden.Scripts.GUI.AnimationManager>().CloseMenu(); else menu.gameObject.SetActive(false);
                yield return Wait(.25f);
            }
        }
    }
    finally { Select(20); }
    yield return qa.EndSession();
    for (int f = 0; f < 4; f++) yield return null;
    string after = UnityEngine.PlayerPrefs.GetInt("OpenLevel") + "/" + UnityEngine.PlayerPrefs.GetInt("Lifes") + "/" + UnityEngine.PlayerPrefs.GetInt("Gems");
    log.AppendLine("restored screen " + UnityEngine.Screen.width + "x" + UnityEngine.Screen.height + "; real OpenLevel/Lifes/Gems " + before + " -> " + after + (before == after ? " (unchanged)" : " CHANGED"));
    log.AppendLine("SUMMARY fails=" + fails);
    log.AppendLine("DONE");
    global::System.IO.File.WriteAllText(global::System.IO.Path.Combine(dir, "log.txt"), log.ToString());
}
RoyalThrone.Editor.RoyalRegressionRunner.Run(Run());
return "started";
