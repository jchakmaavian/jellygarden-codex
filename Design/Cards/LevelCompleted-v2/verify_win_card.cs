// Run with unity command --project-path <project> eval_file --file Design/Cards/LevelCompleted-v2/verify_win_card.cs
// Editor-eval script, deliberately outside Assets. Owns one QA session; never adopts an existing session.
// Presentation-only: opening the card and RoyalWinSequence.Reward do not commit a level win or credit currency.
if (!UnityEngine.Application.isPlaying) throw new global::System.InvalidOperationException("Enter Play Mode on Home first.");
if (RoyalThrone.Testing.RoyalSave.Testing) throw new global::System.InvalidOperationException("Another QA session is active; refusing to adopt it.");
var qa = RoyalThrone.Testing.RoyalQaService.Instance;
var lm = JellyGarden.Scripts.LevelManager.THIS;
var menus = JellyGarden.Scripts.GUI.MenuManager.Instance;
if (!qa || !qa.Ready || qa.Busy || !lm || !menus) throw new global::System.InvalidOperationException("Game is not ready for an owned QA session.");
if (lm.gameStatus.ToString() != "Map") throw new global::System.InvalidOperationException("Return to Home before running this presentation check.");
var dir = global::System.IO.Path.GetFullPath("Design/Cards/LevelCompleted-v2/Verification");
global::System.IO.Directory.CreateDirectory(dir);
var log = new global::System.Text.StringBuilder();
var samples = new global::System.Text.StringBuilder("open_s\tstar_s\tstar_scale\tcushion_y\tflash\tglow\tpillow_glow\tvisible_sparkles\tlanded\trewards_ready\tcoin_visible\n");
var errors = new global::System.Collections.Generic.List<string>();
int passes = 0, fails = 0;
bool ownsQa = false, cleanupCompleted = false, listening = false;
float oldCapture = UnityEngine.Time.captureDeltaTime;
bool oldBackground = UnityEngine.Application.runInBackground;
var flags = global::System.Reflection.BindingFlags.Instance | global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.NonPublic;
var oldSizes = new global::System.Collections.Generic.Dictionary<UnityEngine.Object, int>();
global::System.Reflection.PropertyInfo sizeProperty = null;
UnityEngine.Transform menu = null, card = null;
RoyalThrone.Presentation.RoyalWinCard piece = null;
int motionBefore = 1;
string F(float value) => value.ToString("F4", global::System.Globalization.CultureInfo.InvariantCulture);
void Note(string message) { log.AppendLine(message); global::System.IO.File.WriteAllText(global::System.IO.Path.Combine(dir, "checks.txt"), log.ToString()); }
void Check(bool ok, string message) { if (ok) passes++; else fails++; Note((ok ? "PASS " : "FAIL ") + message); }
object Member(object obj, string name)
{
    if (obj == null) return null;
    var type = obj.GetType(); var f = type.GetField(name, flags);
    return f != null ? f.GetValue(obj) : type.GetProperty(name, flags)?.GetValue(obj);
}
bool RewardsReady() => piece && Member(piece, "RewardsReady") is bool b && b;
global::System.Collections.Generic.List<UnityEngine.UI.Graphic> Graphics(object value)
{
    var list = new global::System.Collections.Generic.List<UnityEngine.UI.Graphic>();
    void Add(object item)
    {
        if (item is UnityEngine.UI.Graphic graphic) { list.Add(graphic); return; }
        if (item is UnityEngine.Component component) { list.AddRange(component.GetComponentsInChildren<UnityEngine.UI.Graphic>(true)); return; }
        if (item is global::System.Collections.IEnumerable items) foreach (var child in items) Add(child);
    }
    Add(value); return list;
}
float Alpha(UnityEngine.Transform t)
{
    float a = 1;
    for (var x = t; x; x = x.parent) foreach (var g in x.GetComponents<UnityEngine.CanvasGroup>()) a *= g.alpha;
    return a;
}
float VisibleAlpha(UnityEngine.UI.Graphic g) => g && g.enabled && g.gameObject.activeInHierarchy ? g.color.a * Alpha(g.transform) : 0;
float MemberAlpha(string name) { float a = 0; foreach (var g in Graphics(Member(piece, name))) a = UnityEngine.Mathf.Max(a, VisibleAlpha(g)); return a; }
int SparklesVisible()
{
    int n = 0; if (!piece || !piece.sparkles) return n;
    foreach (var g in piece.sparkles.GetComponentsInChildren<UnityEngine.UI.RawImage>(true)) if (VisibleAlpha(g) > .025f) n++;
    return n;
}
int SparkleCount() => piece && piece.sparkles ? piece.sparkles.GetComponentsInChildren<UnityEngine.UI.RawImage>(true).Length : 0;
bool CoinVisible()
{
    foreach (var g in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Image>(UnityEngine.FindObjectsSortMode.None))
        if (g.name == "Flying reward" && VisibleAlpha(g) > .01f) return true;
    return false;
}
string Snapshot(bool routed)
{
    var b = new global::System.Text.StringBuilder();
    string[] ints = { "OpenLevel", "OpenLevelTest", "Lifes", "Gems", "Colorful_bomb", "Packages", "Stripes", "Cannon", "Hammer", "Hand", "Arrow", "Music", "Sound", "RoyalThrone.HomeMotion", "GetGems", "GetLifes", "GetGoOn", "RoyalThrone.Area", "RoyalThrone.StarsSpent" };
    foreach (var k in ints) b.Append(k).Append('=').Append(routed ? RoyalThrone.Testing.RoyalSave.GetInt(k, -99999) : UnityEngine.PlayerPrefs.GetInt(k, -99999)).Append(';');
    for (int i = 1; i <= 50; i++)
    {
        string k = "Level." + i.ToString("000") + ".StarsCount", score = "Score" + i;
        b.Append(k).Append('=').Append(routed ? RoyalThrone.Testing.RoyalSave.GetInt(k, -99999) : UnityEngine.PlayerPrefs.GetInt(k, -99999)).Append(';');
        b.Append(score).Append('=').Append(routed ? RoyalThrone.Testing.RoyalSave.GetInt(score, -99999) : UnityEngine.PlayerPrefs.GetInt(score, -99999)).Append(';');
    }
    foreach (var k in new[] { "DateOfExit", "RoyalThrone.NextLifeUtc" }) b.Append(k).Append('=').Append(routed ? RoyalThrone.Testing.RoyalSave.GetString(k, "<missing>") : UnityEngine.PlayerPrefs.GetString(k, "<missing>")).Append(';');
    b.Append("RestLifeTimer=").Append(F(routed ? RoyalThrone.Testing.RoyalSave.GetFloat("RestLifeTimer", -99999) : UnityEngine.PlayerPrefs.GetFloat("RestLifeTimer", -99999)));
    return b.ToString();
}
string Economy()
{
    var b = new global::System.Text.StringBuilder("gems=" + JellyGarden.Scripts.InitScript.Gems + ";lives=" + JellyGarden.Scripts.InitScript.lifes);
    foreach (var k in new[] { "Gems", "Lifes", "Colorful_bomb", "Packages", "Stripes", "Level.023.StarsCount", "Score23", "Level.024.StarsCount", "Score24", "RoyalThrone.StarsSpent" })
        b.Append(';').Append(k).Append('=').Append(RoyalThrone.Testing.RoyalSave.GetInt(k, -99999));
    return b.ToString();
}
string realBefore = Snapshot(false), runtimeBefore = JellyGarden.Scripts.InitScript.Gems + "/" + JellyGarden.Scripts.InitScript.lifes + "/" + lm.currentLevel;
UnityEngine.Application.LogCallback onLog = (message, stack, type) => { if (type == UnityEngine.LogType.Error || type == UnityEngine.LogType.Exception || type == UnityEngine.LogType.Assert) errors.Add(type + ": " + message); };
global::System.Collections.IEnumerator Wait(float seconds) { float until = UnityEngine.Time.unscaledTime + seconds; while (UnityEngine.Time.unscaledTime < until) yield return null; }
global::System.Collections.IEnumerator Shot(string name)
{
    yield return new UnityEngine.WaitForEndOfFrame();
    var tex = UnityEngine.ScreenCapture.CaptureScreenshotAsTexture();
    try { global::System.IO.File.WriteAllBytes(global::System.IO.Path.Combine(dir, name + ".png"), UnityEngine.ImageConversion.EncodeToPNG(tex)); }
    finally { UnityEngine.Object.Destroy(tex); }
}
UnityEngine.Rect Rect(UnityEngine.Transform t)
{
    var r = t as UnityEngine.RectTransform; if (!r) return new UnityEngine.Rect();
    var c = new UnityEngine.Vector3[4]; r.GetWorldCorners(c);
    var canvas = r.GetComponentInParent<UnityEngine.Canvas>(); var cam = canvas && canvas.renderMode != UnityEngine.RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
    var lo = UnityEngine.RectTransformUtility.WorldToScreenPoint(cam, c[0]); var hi = UnityEngine.RectTransformUtility.WorldToScreenPoint(cam, c[2]);
    return UnityEngine.Rect.MinMaxRect(lo.x, lo.y, hi.x, hi.y);
}
bool Fits(UnityEngine.Transform t) { var r = Rect(t); return t && r.width > 0 && r.xMin >= -1 && r.yMin >= -1 && r.xMax <= UnityEngine.Screen.width + 1 && r.yMax <= UnityEngine.Screen.height + 1; }
void AuditLayout(string label)
{
    var skin = card.Find("Royal skin"); var close = card.Find("Royal close"); var next = card.Find("Next"); var badge = card.Find("Royal win reward"); var heading = card.Find("Royal win heading");
    Check(Fits(skin) && Fits(close) && Fits(next), label + " card, close and Continue stay on screen " + UnityEngine.Screen.width + "x" + UnityEngine.Screen.height);
    Check(heading && piece.star && Rect(heading).yMin >= Rect(piece.star).yMax - Rect(piece.star).height * .12f, label + " heading clears the star body");
    Check(badge && Rect(badge).yMin >= Rect(next).yMax - 1, label + " reward badge clears Continue");
    Check(badge && Rect(piece.cushion).yMin >= Rect(badge).yMax - Rect(piece.cushion).height * .2f, label + " cushion tassels clear the reward badge");
    bool inputs = true; foreach (var graphic in piece.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
        if ((graphic.transform.IsChildOf(piece.sparkles) || graphic.name.Contains("glow") || graphic.name.Contains("rays")) && graphic.raycastTarget) inputs = false;
    Check(inputs, label + " decorative light and sparkles do not take taps");
}
bool SelectSize(string wanted)
{
    try
    {
        var assembly = typeof(UnityEditor.EditorWindow).Assembly; var viewType = assembly.GetType("UnityEditor.GameView");
        var sizesType = assembly.GetType("UnityEditor.GameViewSizes");
        var sizes = sizesType.GetProperty("instance", global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Static | global::System.Reflection.BindingFlags.FlattenHierarchy).GetValue(null);
        var groupType = sizesType.GetProperty("currentGroupType", flags).GetValue(sizes);
        var group = sizesType.GetMethod("GetGroup", flags).Invoke(sizes, new[] { groupType });
        var labels = (string[])group.GetType().GetMethod("GetDisplayTexts", flags).Invoke(group, null);
        int index = -1; for (int i = 0; i < labels.Length; i++) if (labels[i].Replace(" ", "").Contains(wanted)) { index = i; break; }
        if (index < 0) { Note("SKIP no existing Game view preset " + wanted); return false; }
        sizeProperty = viewType.GetProperty("selectedSizeIndex", flags);
        foreach (var view in UnityEngine.Resources.FindObjectsOfTypeAll(viewType)) { if (!oldSizes.ContainsKey(view)) oldSizes.Add(view, (int)sizeProperty.GetValue(view)); sizeProperty.SetValue(view, index); }
        return oldSizes.Count > 0;
    }
    catch (global::System.Exception ex) { Note("SKIP size preset unavailable: " + ex.Message); return false; }
}
void RestoreSettings()
{
    UnityEngine.Time.captureDeltaTime = oldCapture; UnityEngine.Application.runInBackground = oldBackground;
    if (sizeProperty != null) foreach (var entry in oldSizes) if (entry.Key) sizeProperty.SetValue(entry.Key, entry.Value);
    if (ownsQa && RoyalThrone.Testing.RoyalSave.Testing) RoyalThrone.Testing.RoyalSave.SetInt("RoyalThrone.HomeMotion", motionBefore);
}
global::System.Collections.IEnumerator OpenCard(bool coins)
{
    menus.HideAllCanvasElements();
    JellyGarden.Scripts.InitScript.Instance.TryOpenLevel(23); yield return null; menus.menuPlay.SetActive(false);
    lm.stars = 1; menus.ShowMenuComplete();
    piece = RoyalThrone.Presentation.RoyalWinCard.Of(menu);
    if (!piece) throw new global::System.Exception("RoyalWinCard missing.");
    var setter = piece.GetType().GetMethod("SetReward", flags);
    if (setter == null) throw new global::System.Exception("RoyalWinCard.SetReward is missing.");
    setter.Invoke(piece, new object[] { 65 });
    if (coins) JellyGarden.Scripts.Juice.RoyalWinSequence.Reward(lm, 65, JellyGarden.Scripts.InitScript.Gems);
}
global::System.Collections.IEnumerator Tap(UnityEngine.Transform t)
{
    if (!t) throw new global::System.Exception("Tap target missing.");
    var es = UnityEngine.EventSystems.EventSystem.current;
    var p = new UnityEngine.EventSystems.PointerEventData(es) { position = Rect(t).center, button = UnityEngine.EventSystems.PointerEventData.InputButton.Left };
    var hits = new global::System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>(); es.RaycastAll(p, hits);
    var target = hits.Count == 0 ? null : UnityEngine.EventSystems.ExecuteEvents.GetEventHandler<UnityEngine.EventSystems.IPointerClickHandler>(hits[0].gameObject);
    Check(target == t.gameObject, t.name + " owns its real raycast tap");
    if (target != t.gameObject) yield break;
    UnityEngine.EventSystems.ExecuteEvents.Execute(target, p, UnityEngine.EventSystems.ExecuteEvents.pointerDownHandler);
    yield return Wait(.08f);
    UnityEngine.EventSystems.ExecuteEvents.Execute(target, p, UnityEngine.EventSystems.ExecuteEvents.pointerUpHandler);
    UnityEngine.EventSystems.ExecuteEvents.Execute(target, p, UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
}
global::System.Collections.IEnumerator Body()
{
    qa.StartSession(); ownsQa = RoyalThrone.Testing.RoyalSave.Testing;
    if (!ownsQa) throw new global::System.Exception("QA profile failed to start.");
    qa.Visible = false; qa.Counters = false;
    yield return null; yield return null;
    motionBefore = RoyalThrone.Testing.RoyalSave.GetInt("RoyalThrone.HomeMotion", 1);
    RoyalThrone.Testing.RoyalSave.SetInt("RoyalThrone.HomeMotion", 1);
    UnityEngine.Application.runInBackground = true; UnityEngine.Time.captureDeltaTime = 0;
    menu = menus.GetCanvasGlobal().Find("MenuComplete"); card = menu ? menu.Find("Image") : null;
    if (!menu || !card) throw new global::System.Exception("Win menu not found.");
    SelectSize("1080x1920"); yield return Wait(.3f);
    yield return OpenCard(true);
    Check(Member(piece, "pillowGlow") != null && Member(piece, "rays") != null && Member(piece, "rewardBadge") != null && Member(piece, "coinOrigin") != null, "new pillow glow, rays, reward badge and coin origin are bound");
    string economyBefore = Economy();
    float start = UnityEngine.Time.unscaledTime, starAt = -1, landedAt = -1, readyAt = -1, coinAt = -1, settledAt = -1;
    float maxScale = 0, minScale = 9, rebound = 0, minCushion = 9, flashPeak = 0, glowPeak = 0, pillowPeak = 0, firstScale = 0;
    int burst = 0, idle = 0; bool earlyFlight = false;
    while (UnityEngine.Time.unscaledTime - start < 4.6f)
    {
        float now = UnityEngine.Time.unscaledTime, elapsed = now - start;
        bool on = piece.star && piece.star.gameObject.activeInHierarchy;
        if (on && starAt < 0) { starAt = now; firstScale = piece.star.localScale.x; }
        float since = starAt < 0 ? -1 : now - starAt;
        if (on)
        {
            float scale = piece.star.localScale.x; maxScale = UnityEngine.Mathf.Max(maxScale, scale);
            if (since < .42f) minScale = UnityEngine.Mathf.Min(minScale, scale);
            if (since >= .30f && since < .57f) rebound = UnityEngine.Mathf.Max(rebound, scale);
            if (since >= .50f && settledAt < 0 && UnityEngine.Mathf.Abs(scale - 1) < .002f && (piece.star.anchoredPosition - piece.StarRest).magnitude < .2f) settledAt = now;
        }
        minCushion = UnityEngine.Mathf.Min(minCushion, piece.cushion.localScale.y);
        flashPeak = UnityEngine.Mathf.Max(flashPeak, MemberAlpha("flash")); glowPeak = UnityEngine.Mathf.Max(glowPeak, MemberAlpha("glow")); pillowPeak = UnityEngine.Mathf.Max(pillowPeak, MemberAlpha("pillowGlow"));
        int visible = SparklesVisible(); if (since >= 0 && since < 1.8f) burst = global::System.Math.Max(burst, visible); if (since >= 3) idle = global::System.Math.Max(idle, visible);
        if (piece.Landed && landedAt < 0) landedAt = now;
        if (RewardsReady() && readyAt < 0) readyAt = now;
        bool flying = CoinVisible(); if (flying && coinAt < 0) coinAt = now; if (flying && !RewardsReady()) earlyFlight = true;
        samples.Append(F(elapsed)).Append('\t').Append(F(since)).Append('\t').Append(F(on ? piece.star.localScale.x : 0)).Append('\t').Append(F(piece.cushion.localScale.y)).Append('\t').Append(F(MemberAlpha("flash"))).Append('\t').Append(F(MemberAlpha("glow"))).Append('\t').Append(F(MemberAlpha("pillowGlow"))).Append('\t').Append(visible).Append('\t').Append(piece.Landed).Append('\t').Append(RewardsReady()).Append('\t').Append(flying).Append('\n');
        yield return null;
    }
    Check(starAt >= 0 && maxScale >= 1.65f && maxScale <= 1.80f, "star enters at 1.65..1.80x (max " + F(maxScale) + ", first " + F(firstScale) + ")");
    Check(landedAt >= starAt && landedAt - starAt >= .22f && landedAt - starAt <= .38f, "star lands near 0.28s (" + F(landedAt - starAt) + "s)");
    Check(minScale >= .80f && minScale <= .98f && rebound > 1.005f && rebound <= 1.12f, "star compresses and rebounds (min " + F(minScale) + ", rebound " + F(rebound) + ")");
    Check(settledAt > 0 && settledAt - starAt >= .50f && settledAt - starAt <= .74f && UnityEngine.Mathf.Abs(piece.star.localScale.x - 1) < .001f, "star settles near 0.60s (" + F(settledAt - starAt) + "s)");
    Check(minCushion < .97f && minCushion > .82f && UnityEngine.Mathf.Abs(piece.cushion.localScale.y - 1) < .001f, "pillow gives and returns to rest (min " + F(minCushion) + ")");
    Check(flashPeak > .3f, "impact flash is visible (peak " + F(flashPeak) + ")");
    Check(glowPeak > .35f && pillowPeak > .15f, "star and pillow glow visibly (" + F(glowPeak) + "/" + F(pillowPeak) + ")");
    Check(burst >= 3, "landing sparkle burst is visible (max " + burst + ")");
    Check(idle > 0, "sparkles remain observable after 3 seconds (max " + idle + ")");
    var amount = Member(piece, "rewardAmount") as UnityEngine.UI.Text;
    Check(amount && amount.text.Contains("65") && amount.text.Contains("+") && MemberAlpha("rewardBadge") > .9f, "reward badge visibly reads +65 (" + (amount ? amount.text : "missing") + ")");
    Check(readyAt > 0 && coinAt >= readyAt && !earlyFlight, "coin flights wait for RewardsReady (ready " + F(readyAt - starAt) + "s; first coin " + F(coinAt - starAt) + "s)");
    Check(Economy() == economyBefore, "the 65-coin presentation does not mutate reward balances or stars");
    AuditLayout("portrait"); yield return Shot("settled-1080x1920");
    int baselineSparkles = SparkleCount(); int baselineChildren = card.GetComponentsInChildren<UnityEngine.Transform>(true).Length;
    Check(baselineSparkles > 0, "sparkle pool is populated (" + baselineSparkles + ")");
    for (int i = 1; i <= 3; i++)
    {
        menu.gameObject.SetActive(false); yield return Wait(.25f); yield return OpenCard(false); yield return Wait(1.8f);
        Check(SparkleCount() == baselineSparkles && card.GetComponentsInChildren<UnityEngine.Transform>(true).Length == baselineChildren, "reopen " + i + " reuses effects without duplicate objects");
    }
    // Capture separately so JPEG encoding cannot distort the temporal assertions above.
    menu.gameObject.SetActive(false); yield return Wait(.25f);
    string frameFolderName = "frames-" + global::System.DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");
    string frameDir = global::System.IO.Path.Combine(dir, frameFolderName); global::System.IO.Directory.CreateDirectory(frameDir);
    var times = new global::System.Collections.Generic.List<float>(); var paths = new global::System.Collections.Generic.List<string>();
    var frameLog = new global::System.Text.StringBuilder("frame\telapsed_s\n");
    yield return OpenCard(true); float recordStart = UnityEngine.Time.unscaledTime, nextSample = recordStart;
    while (UnityEngine.Time.unscaledTime - recordStart < 4f)
    {
        if (UnityEngine.Time.unscaledTime < nextSample) { yield return null; continue; }
        yield return new UnityEngine.WaitForEndOfFrame();
        float stamp = UnityEngine.Time.unscaledTime - recordStart; var tex = UnityEngine.ScreenCapture.CaptureScreenshotAsTexture();
        string filename = times.Count.ToString("D5") + ".jpg";
        try { global::System.IO.File.WriteAllBytes(global::System.IO.Path.Combine(frameDir, filename), UnityEngine.ImageConversion.EncodeToJPG(tex, 90)); }
        finally { UnityEngine.Object.Destroy(tex); }
        paths.Add(frameFolderName + "/" + filename); times.Add(stamp); frameLog.Append(times.Count - 1).Append('\t').Append(F(stamp)).Append('\n');
        nextSample = UnityEngine.Time.unscaledTime + 1f / 30f;
    }
    var concat = new global::System.Text.StringBuilder("ffconcat version 1.0\n");
    for (int i = 0; i < times.Count; i++) { concat.Append("file '").Append(paths[i]).Append("'\n"); concat.Append("duration ").Append(F(i + 1 < times.Count ? times[i + 1] - times[i] : 1f / 30f)).Append('\n'); }
    if (paths.Count > 0) concat.Append("file '").Append(paths[paths.Count - 1]).Append("'\n");
    global::System.IO.File.WriteAllText(global::System.IO.Path.Combine(dir, "capture.ffconcat"), concat.ToString());
    global::System.IO.File.WriteAllText(global::System.IO.Path.Combine(dir, "capture-timestamps.tsv"), frameLog.ToString());
    Check(times.Count >= 12 && times[times.Count - 1] >= 3.5f, "recording covers the entrance and idle glow (" + times.Count + " samples)");
    Note("CAPTURE " + times.Count + " actual-time JPG samples over 4s; ffconcat durations preserve observed playback. No audio recorded.");
    foreach (var size in new[] { "720x1280", "1536x2048" }) if (SelectSize(size)) { yield return Wait(.6f); AuditLayout(size); yield return Shot("settled-" + size); }
    SelectSize("1080x1920"); yield return Wait(.4f);
    menu.gameObject.SetActive(false); yield return Wait(.25f); RoyalThrone.Testing.RoyalSave.SetInt("RoyalThrone.HomeMotion", 0);
    yield return OpenCard(false); start = UnityEngine.Time.unscaledTime; float calmScaleError = 0, calmCushionError = 0, calmRays = 0; int calmSparkles = 0;
    while (UnityEngine.Time.unscaledTime - start < 2.2f)
    {
        if (piece.star.gameObject.activeInHierarchy) calmScaleError = UnityEngine.Mathf.Max(calmScaleError, UnityEngine.Mathf.Abs(piece.star.localScale.x - 1));
        calmCushionError = UnityEngine.Mathf.Max(calmCushionError, (piece.cushion.localScale - UnityEngine.Vector3.one).magnitude);
        calmRays = UnityEngine.Mathf.Max(calmRays, MemberAlpha("rays")); calmSparkles = global::System.Math.Max(calmSparkles, SparklesVisible()); yield return null;
    }
    Check(calmScaleError < .001f && calmCushionError < .001f, "Motion off: star stays at 1x and pillow never compresses");
    Check(calmRays < .001f && calmSparkles == 0, "Motion off: rays and sparkles stay off");
    Check(RewardsReady(), "Motion off: reward still becomes ready"); yield return Shot("motion-off");
    RoyalThrone.Testing.RoyalSave.SetInt("RoyalThrone.HomeMotion", 1);
    foreach (var control in new[] { "Next", "Royal close" })
    {
        menu.gameObject.SetActive(false); yield return Wait(.2f); yield return OpenCard(false); yield return Wait(1.8f);
        string economy = Economy(); yield return Tap(card.Find(control)); yield return Wait(.8f);
        Check(!menu.gameObject.activeSelf && lm.gameStatus.ToString() == "Map", control + " follows Continue to Home/next-level card");
        Check(Economy() == economy, control + " does not award coins or stars again"); menus.menuPlay.SetActive(false);
    }
    Check(Snapshot(false) == realBefore, "real PlayerPrefs unchanged while QA is active");
}
global::System.Collections.IEnumerator Run()
{
    // Catch errors while manually flattening child iterators, then await cleanup outside catch/finally.
    var stack = new global::System.Collections.Generic.Stack<global::System.Collections.IEnumerator>();
    UnityEngine.Application.logMessageReceived += onLog; listening = true;
    Note("Started " + global::System.DateTime.UtcNow.ToString("o") + "; presentation-only level 23, one star, 65 coins.");
    stack.Push(Body());
    try
    {
        while (stack.Count > 0)
        {
            var current = stack.Peek(); bool moved = false; global::System.Exception error = null;
            try { moved = current.MoveNext(); } catch (global::System.Exception ex) { error = ex; }
            if (error != null) { Check(false, "Harness exception: " + error); break; }
            if (!moved) { stack.Pop(); (current as global::System.IDisposable)?.Dispose(); continue; }
            if (current.Current is global::System.Collections.IEnumerator child && !(current.Current is UnityEngine.CustomYieldInstruction)) stack.Push(child); else yield return current.Current;
        }
        while (stack.Count > 0) (stack.Pop() as global::System.IDisposable)?.Dispose();
        RestoreSettings();
        if (ownsQa && qa && RoyalThrone.Testing.RoyalSave.Testing) yield return qa.EndSession();
        ownsQa = false; cleanupCompleted = true;
        yield return Wait(.3f);
        Check(!RoyalThrone.Testing.RoyalSave.Testing, "owned QA session ended");
        Check(Snapshot(false) == realBefore, "real PlayerPrefs unchanged after restoring the player profile");
        Check(JellyGarden.Scripts.InitScript.Gems + "/" + JellyGarden.Scripts.InitScript.lifes + "/" + lm.currentLevel == runtimeBefore, "player runtime coins/lives/level restored");
        Check(errors.Count == 0, "no Unity errors during the run (" + errors.Count + ")");
        foreach (var error in errors) Note("UNITY ERROR " + error);
        Note("SUMMARY passes=" + passes + " fails=" + fails); Note("DONE");
    }
    finally
    {
        while (stack.Count > 0) { try { (stack.Pop() as global::System.IDisposable)?.Dispose(); } catch { } }
        RestoreSettings();
        if (listening) { UnityEngine.Application.logMessageReceived -= onLog; listening = false; }
        global::System.IO.File.WriteAllText(global::System.IO.Path.Combine(dir, "samples.tsv"), samples.ToString());
        if (!cleanupCompleted && ownsQa && qa && RoyalThrone.Testing.RoyalSave.Testing)
        {
            Note("INTERRUPTED: scheduled owned QA restoration; verify Testing=false before continuing."); qa.StartCoroutine(qa.EndSession());
        }
    }
}
RoyalThrone.Editor.RoyalRegressionRunner.Run(Run());
return "Win-card verification started; poll Design/Cards/LevelCompleted-v2/Verification/checks.txt for DONE.";
