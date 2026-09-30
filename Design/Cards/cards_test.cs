// All-cards verification in Play Mode (QA profile): opens every popup card, records its entrance, audits its layout (texts
// fit, content inside the face, controls take their own taps and stay on screen, no overlaps), captures it, tests press
// feedback and the real close paths, then plays level 23 for the goals card, the pause menu (HELP at timeScale 0, the leave
// question) and leaves. Checks the real profile is unchanged. Writes Temp/RoyalCards/run/log.txt and PNGs; returns immediately.
// Run in Play Mode on the map: unity command eval_file --file Design/Cards/cards_test.cs (see Design/Cards/README.md).
var dir = global::System.IO.Path.GetFullPath("Temp/RoyalCards/run");
global::System.IO.Directory.CreateDirectory(dir);
var log = new global::System.Text.StringBuilder();
var errors = new global::System.Collections.Generic.List<string>();
UnityEngine.Application.LogCallback onLog = (msg, stack, type) => { if (type == UnityEngine.LogType.Error || type == UnityEngine.LogType.Exception || type == UnityEngine.LogType.Assert) errors.Add(type + ": " + msg + " | " + (stack.Length > 300 ? stack.Substring(0, 300) : stack)); };
var lm = JellyGarden.Scripts.LevelManager.THIS;
var menus = JellyGarden.Scripts.GUI.MenuManager.Instance;
var qa = RoyalThrone.Testing.RoyalQaService.Instance;
var root = menus.GetCanvasGlobal();
var es = UnityEngine.EventSystems.EventSystem.current;
var statusPath = global::System.IO.Path.GetFullPath("Design/ReferenceGameplayImport/status.json");
string CodexCommand() { try { var s = global::System.IO.File.ReadAllText(statusPath); int i = s.IndexOf("\"commandUtc\""); return i < 0 ? "" : s.Substring(i, 50); } catch { return ""; } }
string codexAtStart = CodexCommand();
bool aborted = false;
string[] intKeys = { "OpenLevel", "Lifes", "Gems", "Colorful_bomb", "Packages", "Stripes", "Level.023.StarsCount", "Level.018.StarsCount", "Score23", "Music", "Sound" };
string[] stringKeys = { "DateOfExit", "RoyalThrone.NextLifeUtc" };
string Snapshot()
{
    var sb = new global::System.Text.StringBuilder();
    foreach (var k in intKeys) sb.Append(k).Append('=').Append(UnityEngine.PlayerPrefs.GetInt(k, -99)).Append(' ');
    sb.Append("RestLifeTimer=").Append(UnityEngine.PlayerPrefs.GetFloat("RestLifeTimer", -99).ToString("F1")).Append(' ');
    foreach (var k in stringKeys) sb.Append(k).Append('=').Append(UnityEngine.PlayerPrefs.GetString(k, "-")).Append(' ');
    return sb.ToString();
}
UnityEngine.Vector2 Centre(UnityEngine.Transform t) { var rt = (UnityEngine.RectTransform)t; return UnityEngine.RectTransformUtility.WorldToScreenPoint(null, rt.TransformPoint(rt.rect.center)); }
UnityEngine.EventSystems.PointerEventData Ped(UnityEngine.Transform t) { var pos = Centre(t); return new UnityEngine.EventSystems.PointerEventData(es) { button = UnityEngine.EventSystems.PointerEventData.InputButton.Left, position = pos, pressPosition = pos }; }
void Down(UnityEngine.Transform t, UnityEngine.EventSystems.PointerEventData p) { UnityEngine.EventSystems.ExecuteEvents.Execute(t.gameObject, p, UnityEngine.EventSystems.ExecuteEvents.pointerEnterHandler); UnityEngine.EventSystems.ExecuteEvents.Execute(t.gameObject, p, UnityEngine.EventSystems.ExecuteEvents.pointerDownHandler); }
void Up(UnityEngine.Transform t, UnityEngine.EventSystems.PointerEventData p, bool click) { UnityEngine.EventSystems.ExecuteEvents.Execute(t.gameObject, p, UnityEngine.EventSystems.ExecuteEvents.pointerUpHandler); if (click) UnityEngine.EventSystems.ExecuteEvents.Execute(t.gameObject, p, UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler); UnityEngine.EventSystems.ExecuteEvents.Execute(t.gameObject, p, UnityEngine.EventSystems.ExecuteEvents.pointerExitHandler); }
global::System.Collections.IEnumerator Wait(float seconds) { float end = UnityEngine.Time.realtimeSinceStartup + seconds; while (UnityEngine.Time.realtimeSinceStartup < end) yield return null; }
global::System.Collections.IEnumerator Click(UnityEngine.Transform t) { var p = Ped(t); Down(t, p); yield return Wait(.06f); Up(t, p, true); yield return null; }
global::System.Collections.IEnumerator Shot(string name)
{
    yield return new UnityEngine.WaitForEndOfFrame();
    var tex = UnityEngine.ScreenCapture.CaptureScreenshotAsTexture();
    global::System.IO.File.WriteAllBytes(global::System.IO.Path.Combine(dir, name + ".png"), UnityEngine.ImageConversion.EncodeToPNG(tex));
    UnityEngine.Object.Destroy(tex);
}
float Alpha(UnityEngine.Transform t)
{
    float a = 1;
    for (var x = t; x; x = x.parent) { foreach (var g in x.GetComponents<UnityEngine.CanvasGroup>()) a *= g.alpha; if (x == root) break; }
    return a;
}
// A rect in the card's local units (the card is at rest, scale 1).
UnityEngine.Rect Local(UnityEngine.Transform card, UnityEngine.RectTransform r)
{
    var c = new UnityEngine.Vector3[4]; r.GetWorldCorners(c);
    var a = card.InverseTransformPoint(c[0]); var b = card.InverseTransformPoint(c[2]);
    return UnityEngine.Rect.MinMaxRect(UnityEngine.Mathf.Min(a.x, b.x), UnityEngine.Mathf.Min(a.y, b.y), UnityEngine.Mathf.Max(a.x, b.x), UnityEngine.Mathf.Max(a.y, b.y));
}
// The drawn extent of a text: its rect for wrapped text, its preferred width about its alignment otherwise.
UnityEngine.Rect Drawn(UnityEngine.Transform card, UnityEngine.UI.Text text)
{
    var r = Local(card, text.rectTransform);
    if (text.horizontalOverflow == UnityEngine.HorizontalWrapMode.Wrap) return r;
    float w = text.preferredWidth * r.width / UnityEngine.Mathf.Max(1, text.rectTransform.rect.width), h = .81f * text.fontSize;
    float x = r.center.x - w * .5f;
    var align = text.alignment;
    if (align == UnityEngine.TextAnchor.MiddleLeft || align == UnityEngine.TextAnchor.UpperLeft || align == UnityEngine.TextAnchor.LowerLeft) x = r.xMin;
    if (align == UnityEngine.TextAnchor.MiddleRight || align == UnityEngine.TextAnchor.UpperRight || align == UnityEngine.TextAnchor.LowerRight) x = r.xMax - w;
    return new UnityEngine.Rect(x, r.center.y - .1f * text.fontSize - h * .5f, w, h);
}
bool Visible(UnityEngine.Transform t) { return t && t.gameObject.activeInHierarchy && Alpha(t) > .01f; }
bool InsideButton(UnityEngine.Transform t, UnityEngine.Transform card) { for (var x = t.parent; x && x != card; x = x.parent) if (x.GetComponent<UnityEngine.UI.Button>()) return true; return false; }
string Hit(UnityEngine.Transform t)
{
    var hits = new global::System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
    es.RaycastAll(Ped(t), hits);
    if (hits.Count == 0) return "FAIL " + t.name + " no hit";
    var handler = UnityEngine.EventSystems.ExecuteEvents.GetEventHandler<UnityEngine.EventSystems.IPointerClickHandler>(hits[0].gameObject);
    return (handler == t.gameObject ? "PASS " : "FAIL ") + t.name + " takes its own tap (hit " + hits[0].gameObject.name + ")";
}
int fails = 0, warns = 0;
void Result(bool pass, string line, bool warnOnly = false) { if (!pass) { if (warnOnly) warns++; else fails++; } log.AppendLine((pass ? "PASS " : warnOnly ? "WARN " : "FAIL ") + line); }

// Layout audit of a card at rest.
void Audit(string name, UnityEngine.Transform card, float faceInset)
{
    var face = Local(card, (UnityEngine.RectTransform)card);
    var inner = new UnityEngine.Rect(face.xMin + faceInset, face.yMin + faceInset, face.width - 2 * faceInset, face.height - 2 * faceInset);
    log.AppendLine("  card " + name + " size " + face.size + " scale " + card.lossyScale.x.ToString("F4"));
    var texts = new global::System.Collections.Generic.List<UnityEngine.UI.Text>();
    foreach (var text in card.GetComponentsInChildren<UnityEngine.UI.Text>())
    {
        if (!text.enabled || !Visible(text.transform) || string.IsNullOrEmpty(text.text)) continue;
        texts.Add(text);
        var r = text.rectTransform.rect;
        bool wrap = text.horizontalOverflow == UnityEngine.HorizontalWrapMode.Wrap;
        bool fits = wrap ? text.preferredHeight <= r.height + 3 : text.preferredWidth <= r.width + 3;
        var d = Drawn(card, text);
        bool inside = inner.Contains(new UnityEngine.Vector2(d.xMin, d.yMin)) && inner.Contains(new UnityEngine.Vector2(d.xMax, d.yMax));
        // The ribbon's LEVEL and number sit above the face on the crest.
        if (text.name == "Royal level label" || text.name == "Level") inside = true;
        Result(fits && inside, name + " text '" + text.text.Replace("\n", " / ") + "' (" + text.name + ", " + text.fontSize + ") " + (wrap ? "h " + text.preferredHeight.ToString("F0") + "/" + r.height.ToString("F0") : "w " + text.preferredWidth.ToString("F0") + "/" + r.width.ToString("F0")) + (inside ? "" : " OUTSIDE face " + d), true);
    }
    var buttons = new global::System.Collections.Generic.List<UnityEngine.UI.Button>();
    foreach (var b in card.GetComponentsInChildren<UnityEngine.UI.Button>()) if (Visible(b.transform)) buttons.Add(b);
    foreach (var b in buttons)
    {
        var c = new UnityEngine.Vector3[4]; ((UnityEngine.RectTransform)b.transform).GetWorldCorners(c);
        bool on = c[0].x >= 0 && c[2].x <= UnityEngine.Screen.width && c[0].y >= 0 && c[2].y <= UnityEngine.Screen.height;
        Result(on, name + " " + b.name + " on screen");
        if (b.interactable) Result(Hit(b.transform).StartsWith("PASS"), name + " " + Hit(b.transform));
        var lr = Local(card, (UnityEngine.RectTransform)b.transform);
        log.AppendLine("  control " + b.name + " at " + lr.center.ToString("F1") + " size " + lr.size.ToString("F0") + " interactable=" + b.interactable);
    }
    for (int i = 0; i < buttons.Count; i++)
        for (int j = i + 1; j < buttons.Count; j++)
        {
            if (buttons[j].transform.IsChildOf(buttons[i].transform) || buttons[i].transform.IsChildOf(buttons[j].transform)) continue;
            var a = Local(card, (UnityEngine.RectTransform)buttons[i].transform); var b = Local(card, (UnityEngine.RectTransform)buttons[j].transform);
            Result(!a.Overlaps(b), name + " " + buttons[i].name + " and " + buttons[j].name + " do not overlap");
        }
    for (int i = 0; i < texts.Count; i++)
        for (int j = i + 1; j < texts.Count; j++)
        {
            var a = Drawn(card, texts[i]); var b = Drawn(card, texts[j]);
            Result(!a.Overlaps(b), name + " texts '" + texts[i].text.Replace("\n", " ") + "' and '" + texts[j].text.Replace("\n", " ") + "' do not overlap", true);
        }
    foreach (var text in texts)
    {
        if (InsideButton(text.transform, card)) continue;
        var d = Drawn(card, text);
        foreach (var b in buttons) if (!text.transform.IsChildOf(b.transform) && d.Overlaps(Local(card, (UnityEngine.RectTransform)b.transform))) Result(false, name + " text '" + text.text + "' overlaps " + b.name, true);
    }
    var heading = card.Find("Royal heading"); var close = card.Find("Close");
    if (heading && close && Visible(close)) Result(!Drawn(card, heading.GetComponent<UnityEngine.UI.Text>()).Overlaps(Local(card, (UnityEngine.RectTransform)close)), name + " heading clears the close control");
    foreach (var icon in card.GetComponentsInChildren<JellyGarden.Scripts.Targets.TargetIcon>())
    {
        var ir = Local(card, (UnityEngine.RectTransform)icon.transform);
        log.AppendLine("  goal " + icon.name + " at " + ir.center.ToString("F1") + " size " + ir.size.ToString("F0") + " text '" + icon.textObj.text + "' font " + icon.textObj.fontSize);
    }
}
// Opens a popup and records its entrance: scale and alpha each frame, no squash (x == y), no overshoot, settles to rest.
global::System.Collections.IEnumerator Entrance(string name, UnityEngine.Transform menu, UnityEngine.Transform card, bool shotMid)
{
    var motion = menu.GetComponent<RoyalThrone.Presentation.RoyalCardMotion>();
    var group = card.GetComponent<UnityEngine.CanvasGroup>();
    var anim = menu.GetComponent<UnityEngine.Animation>();
    float start = UnityEngine.Time.realtimeSinceStartup, maxScale = 0, squash = 0; string samples = "";
    bool midShot = false;
    while (UnityEngine.Time.realtimeSinceStartup - start < .7f)
    {
        float s = card.localScale.x; maxScale = UnityEngine.Mathf.Max(maxScale, s); squash = UnityEngine.Mathf.Max(squash, UnityEngine.Mathf.Abs(card.localScale.x - card.localScale.y));
        if (samples.Length < 900) samples += (motion ? motion.Elapsed.ToString("F2") : "?") + ":" + s.ToString("F3") + "/" + (group ? group.alpha.ToString("F2") : "-") + " ";
        if (shotMid && !midShot && motion && motion.Elapsed >= .10f) { midShot = true; yield return Shot(name + "-entrance-100ms"); }
        yield return null;
    }
    log.AppendLine("  entrance " + samples);
    Result(motion != null, name + " has the calm card motion");
    Result(maxScale <= 1.0001f && squash < .0001f, name + " entrance has no overshoot or squash (max scale " + maxScale.ToString("F4") + ", squash " + squash.ToString("F4") + ")");
    Result(UnityEngine.Mathf.Abs(card.localScale.x - 1) < .0001f && (!group || group.alpha > .999f), name + " settles at scale 1 and full opacity");
    Result(!anim || !anim.isPlaying, name + " inherited squash clip does not play");
}
// Press feedback on a control without clicking (the UI polish of 2026-09-30): every control, the primary ones included,
// eases to 0.98 and darkens 8% while held, and returns to 1 when released. `sink` only picks how the log line reads.
global::System.Collections.IEnumerator Press(string name, UnityEngine.Transform t, bool sink)
{
    if (!t) { log.AppendLine("  (no control for press test on " + name + ")"); yield break; }
    var skin = (UnityEngine.RectTransform)t.Find("Royal skin");
    var rest = skin.anchoredPosition; var face = t.Find("Royal skin/Green"); if (!face) face = t.Find("Royal skin/Face");
    var p = Ped(t); Down(t, p); yield return Wait(.2f);
    yield return Shot(name + "-press-" + t.name);
    float tint = face ? face.GetComponent<UnityEngine.CanvasRenderer>().GetColor().r : -1;
    float pressedScale = skin.localScale.x, pressedSink = skin.anchoredPosition.y - rest.y;
    string pressed = "scale " + pressedScale.ToString("F3") + " sink " + pressedSink.ToString("F2");
    Up(t, p, false); yield return Wait(.25f);
    string released = "scale " + skin.localScale.x.ToString("F3") + " sink " + (skin.anchoredPosition.y - rest.y).ToString("F2");
    bool held = UnityEngine.Mathf.Abs(pressedScale - .98f) < .005f && UnityEngine.Mathf.Abs(pressedSink) < .01f && tint > .9f && tint < .94f;
    bool back = UnityEngine.Mathf.Abs(skin.localScale.x - 1) < .001f && UnityEngine.Mathf.Abs(skin.anchoredPosition.y - rest.y) < .01f;
    Result(held && back, name + " " + t.name + " press feedback: pressed " + pressed + " tint " + tint.ToString("F2") + ", released " + released);
}
bool Changed() { if (CodexCommand() != codexAtStart) { aborted = true; log.AppendLine("ABORT: the Codex editor channel sent a command"); } return aborted; }

global::System.Collections.IEnumerator Run()
{
    yield return null;
    UnityEngine.Application.logMessageReceivedThreaded += onLog;
    string before = Snapshot();
    try
    {
        log.AppendLine("screen " + UnityEngine.Screen.width + "x" + UnityEngine.Screen.height + " status=" + lm.gameStatus);
        log.AppendLine("real before: " + before);
        qa.StartSession(); qa.Visible = false; yield return null;
        qa.Request("Refill lives"); while (qa.Busy) yield return null; qa.Visible = false;
        JellyGarden.Scripts.InitScript.Instance.TryOpenLevel(23); yield return null; menus.menuPlay.SetActive(false); yield return Wait(.3f);

        // ---- Map: every card by itself ----
        string[] names = { "GemsShop", "LiveShop", "BoostShop", "BoostInfo", "Settings", "Tutorial", "Reward", "MenuComplete", "MenuFailed", "PreFailed", "MenuPause" };
        foreach (var name in names)
        {
            if (Changed()) yield break;
            var menu = root.Find(name);
            if (!menu) { Result(false, "missing " + name); continue; }
            var card = name == "PreFailed" ? menu.Find("Banner") : menu.Find("Image");
            log.AppendLine("== " + name);
            if (name == "MenuComplete") { lm.stars = 3; JellyGarden.Scripts.LevelManager.Score = 12450; }
            if (name == "BoostShop") menu.GetComponent<JellyGarden.Scripts.GUI.BoostShop>().SetBoost(JellyGarden.Scripts.GUI.BoostType.Stripes);
            else menu.gameObject.SetActive(true);
            if (name == "PreFailed")
            {
                // Its own clip: fade/scale in, hold at WaitForGiveUp.
                var anim = menu.GetComponent<UnityEngine.Animation>(); var state = anim["bannerFailed"];
                float maxScale = 0; string samples = "";
                float t0 = UnityEngine.Time.realtimeSinceStartup;
                while (UnityEngine.Time.realtimeSinceStartup - t0 < .6f) { maxScale = UnityEngine.Mathf.Max(maxScale, card.localScale.x); if (samples.Length < 700) samples += state.time.ToString("F2") + ":" + card.localScale.x.ToString("F3") + "/" + card.GetComponent<UnityEngine.CanvasGroup>().alpha.ToString("F2") + " "; yield return null; }
                log.AppendLine("  entrance " + samples);
                Result(maxScale <= 1.0001f, name + " entrance has no overshoot (max " + maxScale.ToString("F4") + ")");
                yield return Shot(name);
                Audit(name, card, 17.6f);
                foreach (var n in new[] { "Buy", "GiveUp", "Video" }) { var b = menu.Find(n); if (b) log.AppendLine("  " + n + " active=" + b.gameObject.activeSelf + " alpha=" + Alpha(b).ToString("F2") + " at " + Local(card, (UnityEngine.RectTransform)b).center.ToString("F1") + " interactable=" + b.GetComponent<UnityEngine.UI.Button>().interactable); }
                yield return Wait(1.7f);
                Result(state.speed == 0 && UnityEngine.Mathf.Abs(state.time - 2.05f) < .1f, name + " holds for the player's choice (time " + state.time.ToString("F2") + ", speed " + state.speed + ")");
                yield return Shot(name + "-hold");
                yield return Press(name, menu.Find("Buy"), true);
                // END LEVEL on the map: the card fades out and closes (OnFinished) without leaving the map.
                yield return Click(menu.Find("GiveUp"));
                yield return Wait(.2f);
                log.AppendLine("  after END LEVEL: time " + state.time.ToString("F2") + " card alpha " + card.GetComponent<UnityEngine.CanvasGroup>().alpha.ToString("F2") + " scale " + card.localScale.x.ToString("F3"));
                yield return Shot(name + "-leaving");
                yield return Wait(1.2f);
                Result(!menu.gameObject.activeSelf && lm.gameStatus.ToString() == "Map", name + " END LEVEL fades the card and closes it (status " + lm.gameStatus + ")");
                continue;
            }
            yield return Entrance(name, menu, card, name == "GemsShop" || name == "MenuComplete");
            if (name == "MenuComplete") yield return Wait(1.6f);   // the stars arrive one by one
            if (name == "LiveShop") yield return Wait(.5f);
            yield return Shot(name);
            Audit(name, card, 17.6f);
            var motion = menu.GetComponent<RoyalThrone.Presentation.RoyalCardMotion>();
            if (name == "MenuComplete")
                for (int i = 1; i <= 3; i++) { var star = card.Find("Star" + i); log.AppendLine("  star" + i + " active=" + star.gameObject.activeSelf + " alpha=" + Alpha(star).ToString("F2") + " scale=" + star.localScale.x.ToString("F3")); }
            if (name == "GemsShop")
                for (int i = 1; i <= 4; i++)
                {
                    var buy = card.Find("Pack" + i + "/Buy");
                    float tint = buy.Find("Royal skin/Green").GetComponent<UnityEngine.CanvasRenderer>().GetColor().r;
                    bool interactable = buy.GetComponent<UnityEngine.UI.Button>().interactable;
                    Result(interactable || UnityEngine.Mathf.Abs(tint - .6f) < .02f, name + " pack " + i + " " + (interactable ? "available" : "unavailable, dimmed " + tint.ToString("F2")) + " price '" + buy.Find("Price").GetComponent<UnityEngine.UI.Text>().text + "'");
                }
            if (name == "Tutorial")
            {
                var tutorial = menu.GetComponent<JellyGarden.Scripts.GUI.Tutorial>();
                // The first lesson's BACK rests dimmed and takes no press (RoyalCards.LessonProgress).
                var backButton = card.Find("Back"); var backFace = backButton.Find("Royal skin/Green");
                float backTint = backFace ? backFace.GetComponent<UnityEngine.CanvasRenderer>().GetColor().r : -1;
                Result(!backButton.GetComponent<UnityEngine.UI.Button>().interactable && UnityEngine.Mathf.Abs(backTint - .6f) < .02f, name + " BACK is off and dimmed on the first lesson (" + backTint.ToString("F2") + ")");
                for (int page = 1; page < RoyalThrone.Presentation.RoyalLessonDemo.Count; page++)
                {
                    tutorial.Next(); yield return null; yield return null;
                    var lesson = card.Find("Royal lesson " + page);
                    Result(lesson && lesson.Find("Royal styled"), name + " page " + page + " styled the frame it is built");
                    yield return Wait(.1f);
                    yield return Shot(name + "-" + page);
                    Audit(name + "-" + page, card, 17.6f);
                }
                for (int page = 0; page < RoyalThrone.Presentation.RoyalLessonDemo.Count - 2; page++) tutorial.Back();
                yield return null; yield return null;
                // On the second lesson both buttons are live.
                yield return Press(name, card.Find("Next"), true);
                yield return Press(name, card.Find("Back"), false);
                tutorial.Back(); yield return null;
            }
            if (name == "Settings") { yield return Press(name, card.Find("Music"), false); yield return Press(name, card.Find("Hints"), false); }
            if (name == "LiveShop") yield return Press(name, card.Find("BuyLife"), true);
            if (name == "BoostShop") yield return Press(name, card.Find("BuyBoost1"), true);
            if (name == "Reward") yield return Press(name, card.Find("Ok"), true);
            if (name == "MenuComplete") yield return Press(name, card.Find("Next"), true);
            if (name == "MenuFailed") yield return Press(name, card.Find("Again"), true);
            if (name == "MenuPause")
            {
                yield return Press(name, card.Find("Continue"), true);
                // HOME asks first; the question has the same card and motion.
                yield return Click(card.Find("Map"));
                var confirm = card.Find(RoyalThrone.Presentation.RoyalMenuSkin.LeaveConfirmName);
                Result(confirm && confirm.gameObject.activeSelf, name + " HOME opens the leave question");
                if (confirm)
                {
                    var qcard = confirm.Find("Card");
                    float t0 = UnityEngine.Time.realtimeSinceStartup, maxScale = 0;
                    while (UnityEngine.Time.realtimeSinceStartup - t0 < .6f) { maxScale = UnityEngine.Mathf.Max(maxScale, qcard.localScale.x); yield return null; }
                    Result(maxScale <= 1.0001f && UnityEngine.Mathf.Abs(qcard.localScale.x - 1) < .0001f, name + " leave question enters calmly (max " + maxScale.ToString("F4") + ")");
                    yield return Shot(name + "-leave");
                    Audit(name + "-leave", qcard, 17.6f);
                    yield return Press(name, qcard.Find("Stay"), true);
                    yield return Click(qcard.Find("Stay"));
                    Result(!confirm.gameObject.activeSelf, name + " STAY dismisses the question");
                }
                menu.gameObject.SetActive(false); yield return Wait(.3f);
                continue;
            }
            // Close through the real paths where they are safe on the map.
            var close = card.Find("Close");
            if (close && Visible(close) && name != "MenuComplete" && name != "MenuFailed")
            {
                yield return Press(name, close, false);
                yield return Click(close); yield return Wait(.2f);
                Result(!menu.gameObject.activeSelf, name + " x closes the card");
            }
            if (menu.gameObject.activeSelf) menu.gameObject.SetActive(false);
            yield return Wait(.3f);
        }

        // ---- Level 23: the Level Start card, the goals card, the pause menu ----
        if (Changed()) yield break;
        log.AppendLine("== MenuPlay");
        JellyGarden.Scripts.InitScript.Instance.TryOpenLevel(23);
        yield return Wait(.8f);
        yield return Shot("MenuPlay");
        Audit("MenuPlay", menus.menuPlay.transform.Find("Image"), 17.6f);
        yield return Click(menus.menuPlay.transform.Find("Image/Play"));
        var prePlay = lm.GameField.parent.Find("Canvas/PrePlay");
        float pt = UnityEngine.Time.realtimeSinceStartup; bool sawPrePlay = false; float preMax = 0; string preSamples = "";
        while (UnityEngine.Time.realtimeSinceStartup - pt < 6 && !prePlay.gameObject.activeSelf) yield return null;
        if (prePlay.gameObject.activeSelf)
        {
            sawPrePlay = true;
            var banner = prePlay.Find("Banner"); var bg = banner.GetComponent<UnityEngine.CanvasGroup>();
            float t0 = UnityEngine.Time.realtimeSinceStartup; bool s1 = false, s2 = false, s3 = false;
            while (prePlay.gameObject.activeSelf && UnityEngine.Time.realtimeSinceStartup - t0 < 3)
            {
                float e = UnityEngine.Time.realtimeSinceStartup - t0;
                preMax = UnityEngine.Mathf.Max(preMax, banner.localScale.x);
                if (preSamples.Length < 900) preSamples += e.ToString("F2") + ":" + banner.localScale.x.ToString("F3") + "/" + (bg ? bg.alpha.ToString("F2") : "-") + " ";
                if (!s1 && e > .1f) { s1 = true; yield return Shot("PrePlay-100ms"); }
                if (!s2 && e > .8f) { s2 = true; yield return Shot("PrePlay"); Audit("PrePlay", banner, 17.6f); }
                if (!s3 && e > 1.85f) { s3 = true; yield return Shot("PrePlay-leaving"); }
                yield return null;
            }
            log.AppendLine("  PrePlay " + preSamples);
            Result(preMax <= 1.0001f, "PrePlay goals card enters calmly (max scale " + preMax.ToString("F4") + ")");
        }
        Result(sawPrePlay, "PLAY shows the goals card");
        float wt = UnityEngine.Time.realtimeSinceStartup;
        while (UnityEngine.Time.realtimeSinceStartup - wt < 8 && lm.gameStatus.ToString() != "Playing") yield return null;
        Result(lm.gameStatus.ToString() == "Playing", "level 23 is playing after the goals card (status " + lm.gameStatus + ")");
        yield return Wait(1.0f);
        if (Changed()) yield break;

        log.AppendLine("== MenuPause in the level");
        menus.ShowMenuPause(true);
        var pause = root.Find("MenuPause"); var pauseCard = pause.Find("Image");
        yield return Entrance("MenuPause-level", pause, pauseCard, false);
        yield return Wait(.3f);
        log.AppendLine("  timeScale " + UnityEngine.Time.timeScale + " status " + lm.gameStatus);
        yield return Shot("MenuPause-level");
        // HELP at timeScale 0: How to play must still enter (unscaled clock).
        yield return Click(pauseCard.Find("Info"));
        var tutorialMenu = root.Find("Tutorial");
        yield return Entrance("Tutorial-over-pause", tutorialMenu, tutorialMenu.Find("Image"), false);
        yield return Shot("Tutorial-over-pause");
        yield return Click(tutorialMenu.Find("Image/Close")); yield return Wait(.3f);
        Result(!tutorialMenu.gameObject.activeSelf && pause.gameObject.activeSelf, "closing How to play returns to the pause menu (status " + lm.gameStatus + ")");
        // HOME -> the question -> LEAVE goes to the map (QA profile).
        yield return Click(pauseCard.Find("Map")); yield return Wait(.6f);
        var leaveQ = pauseCard.Find(RoyalThrone.Presentation.RoyalMenuSkin.LeaveConfirmName);
        yield return Shot("MenuPause-leave-level");
        yield return Click(leaveQ.Find("Card/Leave"));
        float lt = UnityEngine.Time.realtimeSinceStartup;
        while (UnityEngine.Time.realtimeSinceStartup - lt < 6 && lm.gameStatus.ToString() != "Map") yield return null;
        Result(lm.gameStatus.ToString() == "Map" && !pause.gameObject.activeSelf, "LEAVE returns to the map (status " + lm.gameStatus + ", timeScale " + UnityEngine.Time.timeScale + ")");
        yield return Wait(1.0f);
    }
    finally
    {
        UnityEngine.Application.logMessageReceivedThreaded -= onLog;
        string nl = global::System.Environment.NewLine;
        global::System.IO.File.WriteAllText(global::System.IO.Path.Combine(dir, "log.txt"), log.ToString() + nl + "(partial; errors so far: " + errors.Count + ")" + nl + string.Join(nl, errors));
    }
    yield return qa.EndSession();
    yield return Wait(.5f);
    string after = Snapshot();
    log.AppendLine("real after:  " + after);
    Result(after == before, "real profile unchanged");
    log.AppendLine("errors during run: " + errors.Count);
    foreach (var e in errors) log.AppendLine("  " + e);
    log.AppendLine("SUMMARY fails=" + fails + " warns=" + warns + (aborted ? " ABORTED" : ""));
    log.AppendLine("DONE");
    global::System.IO.File.WriteAllText(global::System.IO.Path.Combine(dir, "log.txt"), log.ToString());
}
RoyalThrone.Editor.RoyalRegressionRunner.Run(Run());
return "started";
