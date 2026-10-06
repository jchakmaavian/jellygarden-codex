// Run with unity command --project-path <project> eval_file --file Design/Cards/LevelCompleted-v4/verify_win_card.cs
// Editor-eval script, deliberately outside Assets. Owns one QA session; never adopts an existing session.
// Presentation-only: opening the card and RoyalWinSequence.Reward do not commit a level win or credit currency.
#pragma warning disable 0162 // eval_file's generated wrapper may append a return after this script's return.
if (!UnityEngine.Application.isPlaying) throw new global::System.InvalidOperationException("Enter Play Mode on Home first.");
if (RoyalThrone.Testing.RoyalSave.Testing) throw new global::System.InvalidOperationException("Another QA session is active; refusing to adopt it.");
var qa = RoyalThrone.Testing.RoyalQaService.Instance;
var lm = JellyGarden.Scripts.LevelManager.THIS;
var menus = JellyGarden.Scripts.GUI.MenuManager.Instance;
if (!qa || !qa.Ready || qa.Busy || !lm || !menus) throw new global::System.InvalidOperationException("Game is not ready for an owned QA session.");
if (lm.gameStatus.ToString() != "Map") throw new global::System.InvalidOperationException("Return to Home before running this presentation check.");
var dir = global::System.IO.Path.GetFullPath("Design/Cards/LevelCompleted-v4/Verification");
global::System.IO.Directory.CreateDirectory(dir);
var log = new global::System.Text.StringBuilder();
var samples = new global::System.Text.StringBuilder("open_s\tstar_s\tstar_scale\tcushion_y\tflash\tglow\tpillow_glow\tvisible_sparkles\tlanded\trewards_ready\tcoin_visible\tstar_shadow_composite_alpha\tcontact_shadow\tvisible_halo_sparkles\n");
var coinSamples = new global::System.Text.StringBuilder("aspect\topen_s\timage_id\talpha\tleft\tright\tbottom\ttop\tmin_inset_margin\tescaped_corners\n");
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
    foreach (var g in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Image>())
        if (g.name == "Flying reward" && VisibleAlpha(g) > .01f) return true;
    return false;
}
UnityEngine.Transform MemberTransform(string name) => (Member(piece, name) as UnityEngine.Component)?.transform;
int HaloVisible()
{
    int count = 0; foreach (var graphic in Graphics(Member(piece, "haloSparkles"))) if (VisibleAlpha(graphic) > .025f) count++;
    return count;
}
int HaloCount() => Graphics(Member(piece, "haloSparkles")).Count;
float ShadowCompositeAlpha()
{
    // The softened silhouette combines many translucent offset Images. The strongest single
    // Image is intentionally faint. This gives their combined opacity over overlapping opaque
    // interior texels; it is not a claim about the transparent edge pixels of the sprite.
    float transmission = 1; foreach (var graphic in Graphics(Member(piece, "starShadow"))) if (graphic is UnityEngine.UI.Image) transmission *= 1 - UnityEngine.Mathf.Clamp01(VisibleAlpha(graphic));
    return 1 - transmission;
}
float ShadowGroupAlpha()
{
    var shadow = MemberTransform("starShadow"); var group = shadow ? shadow.GetComponent<UnityEngine.CanvasGroup>() : null;
    return group ? group.alpha : 0;
}
bool Behind(UnityEngine.Transform a, UnityEngine.Transform b) => a && b && a.parent == b.parent && a.GetSiblingIndex() < b.GetSiblingIndex();
float Luma(UnityEngine.Color c) => .2126f * c.r + .7152f * c.g + .0722f * c.b;
bool SameShade(UnityEngine.Transform a, UnityEngine.Transform b)
{
    var first = a ? a.GetComponent<RoyalThrone.Presentation.RoyalCardShape>() : null;
    var second = b ? b.GetComponent<RoyalThrone.Presentation.RoyalCardShape>() : null;
    return first && second && first.from == second.from && first.to == second.to;
}
void AuditSharedShell()
{
    // Compare against the real Level Start card rather than a second hard-coded copy of its theme.
    var reference = menus.menuPlay ? menus.menuPlay.transform.Find("Image") : null;
    var skin = card.Find("Royal skin"); var referenceSkin = reference ? reference.Find("Royal skin") : null;
    bool commonBevels = skin && referenceSkin;
    foreach (var layer in new[] { "Edge", "Gold", "Inner brown", "Navy line", "Blue line" })
        commonBevels &= SameShade(skin ? skin.Find(layer) : null, referenceSkin ? referenceSkin.Find(layer) : null);
    Check(commonBevels, "win frame uses the same layered gold bevels as Level Start");
    var crest = skin ? skin.Find("Crest")?.GetComponent<UnityEngine.UI.Image>() : null;
    var referenceCrest = referenceSkin ? referenceSkin.Find("Crest")?.GetComponent<UnityEngine.UI.Image>() : null;
    var flatRibbon = card.Find("Royal ribbon");
    Check(crest && referenceCrest && VisibleAlpha(crest) > .9f && crest.sprite == referenceCrest.sprite && (!flatRibbon || !flatRibbon.gameObject.activeInHierarchy),
        "full draped crown crest matches the other cards, with no competing flat ribbon");
    Check(SameShade(card.Find("Royal close/Royal skin/Face"), reference ? reference.Find("Close/Royal skin/Face") : null),
        "close control uses the shared royal-blue face");
    Check(SameShade(card.Find("Next/Royal skin/Green"), reference ? reference.Find("Play/Royal skin/Green") : null) &&
        SameShade(card.Find("Next/Royal skin/Gold"), reference ? reference.Find("Play/Royal skin/Gold") : null),
        "Continue uses the shared emerald and gold primary-button treatment");
    Check(skin && skin.Find("Ornament diamond") && skin.Find("Ornament line left") && skin.Find("Ornament line right"),
        "shared bottom diamond and fine lines complete the card frame");
    Check(piece.star && UnityEngine.Mathf.Abs(piece.star.rect.width / 276f - 1.5f) < .001f && UnityEngine.Mathf.Abs(piece.star.rect.height / 276f - 1.5f) < .001f,
        "reward star is exactly 1.5 times the previous 276-unit star (" + (piece.star ? F(piece.star.rect.width) : "missing") + ")");
}
void AuditLevelNumber(int expected)
{
    var number = Member(piece, "levelNumber") as UnityEngine.UI.Text;
    Check(number && number.text == expected.ToString() && VisibleAlpha(number) > .9f,
        "crest shows the live level " + expected + " (" + (number ? number.text : "missing") + ")");
}
void AuditArtwork()
{
    var face = card.Find("Royal skin/Face")?.GetComponent<RoyalThrone.Presentation.RoyalCardShape>();
    var light = card.Find("Royal skin/Face light")?.GetComponent<RoyalThrone.Presentation.RoyalCardShape>();
    var rim = card.Find("Royal skin/Face glow")?.GetComponent<RoyalThrone.Presentation.RoyalCardShape>();
    Check(face && Luma(face.from) < .20f && Luma(face.to) < .12f && face.from.b > face.from.r + .20f,
        "win face is dark royal blue (top/bottom luminance " + (face ? F(Luma(face.from)) + "/" + F(Luma(face.to)) : "missing") + ")");
    Check(light && rim && light.from.a <= .25f && rim.from.a <= .17f, "face highlight and inner glow preserve the darker background");
    bool lightsBehind = true;
    foreach (var name in new[] { "glow", "rays", "flash", "haloSparkles" }) lightsBehind &= Behind(MemberTransform(name), piece.cushion) && Behind(MemberTransform(name), piece.star);
    Check(lightsBehind, "halo, animated halo sparkles, rays and impact flash render behind cushion and star");
    Check(piece.glow && piece.glow.rectTransform.sizeDelta.x < piece.star.sizeDelta.x * 1.5f &&
        (piece.glow.rectTransform.anchoredPosition - piece.StarRest).magnitude < piece.star.sizeDelta.x * .12f,
        "halo stays centred behind the star rather than washing the entire card");
    var shadow = MemberTransform("starShadow"); var contact = MemberTransform("contactShadow");
    Check(shadow && contact && Behind(piece.cushion, shadow) && Behind(shadow, contact) && Behind(contact, piece.star),
        "star silhouette shadow and contact shadow sit above velvet and below the star");
    var shadowRect = shadow as UnityEngine.RectTransform;
    Check(shadowRect && shadowRect.anchoredPosition.y < piece.StarRest.y && shadowRect.rect.width > piece.star.rect.width * .6f && shadowRect.rect.height < piece.star.rect.height * .4f,
        "star shadow is flattened onto the pillow below the star");
    Check(ShadowGroupAlpha() > .20f && ShadowCompositeAlpha() > .15f && MemberAlpha("contactShadow") > .10f,
        "soft silhouette and contact shadow remain visible after landing (group " + F(ShadowGroupAlpha()) + "; composite interior alpha " + F(ShadowCompositeAlpha()) + "; contact " + F(MemberAlpha("contactShadow")) + ")");
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
    float left = float.PositiveInfinity, bottom = float.PositiveInfinity, right = float.NegativeInfinity, top = float.NegativeInfinity;
    foreach (var world in c)
    {
        var point = UnityEngine.RectTransformUtility.WorldToScreenPoint(cam, world);
        left = UnityEngine.Mathf.Min(left, point.x); right = UnityEngine.Mathf.Max(right, point.x);
        bottom = UnityEngine.Mathf.Min(bottom, point.y); top = UnityEngine.Mathf.Max(top, point.y);
    }
    return UnityEngine.Rect.MinMaxRect(left, bottom, right, top);
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
        if ((graphic.transform.IsChildOf(piece.sparkles) || (MemberTransform("haloSparkles") && graphic.transform.IsChildOf(MemberTransform("haloSparkles"))) || graphic.name.Contains("glow") || graphic.name.Contains("rays") || graphic.name.IndexOf("shadow", global::System.StringComparison.OrdinalIgnoreCase) >= 0) && graphic.raycastTarget) inputs = false;
    Check(inputs, label + " decorative lights, shadows and sparkles do not take taps");
}
global::System.Collections.IEnumerator ObserveCoinBounds(string aspect)
{
    // Read after rendering updates. All four full Image-quad corners are checked, including flip,
    // rotation and sprite-size changes; checking only its centre would miss visible escapes.
    var frame = card as UnityEngine.RectTransform;
    var frameCanvas = frame.GetComponentInParent<UnityEngine.Canvas>();
    var frameCamera = frameCanvas && frameCanvas.renderMode != UnityEngine.RenderMode.ScreenSpaceOverlay ? frameCanvas.worldCamera : null;
    var boundary = frame.rect;
    const float inset = 22f, tolerance = .5f; // 2 units inside the implementation's 24-unit safety inset.
    var safe = UnityEngine.Rect.MinMaxRect(boundary.xMin + inset, boundary.yMin + inset, boundary.xMax - inset, boundary.yMax - inset);
    var seen = new global::System.Collections.Generic.HashSet<string>();
    var corners = new UnityEngine.Vector3[4];
    int frames = 0, visibleFrames = 0, quadSamples = 0, escapedCorners = 0, maxVisible = 0;
    float worstMargin = float.PositiveInfinity, firstCoin = -1, lastCoin = -1;
    float start = UnityEngine.Time.unscaledTime;
    string economy = Economy(); bool early = false; string firstEscape = null;
    while (UnityEngine.Time.unscaledTime - start < 5f)
    {
        yield return new UnityEngine.WaitForEndOfFrame();
        float elapsed = UnityEngine.Time.unscaledTime - start; int visible = 0; frames++;
        foreach (var coin in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Image>())
        {
            if (coin.name != "Flying reward" || VisibleAlpha(coin) <= .001f) continue;
            string coinId = coin.GetEntityId().ToString();
            visible++; quadSamples++; seen.Add(coinId);
            if (firstCoin < 0) firstCoin = elapsed; lastCoin = elapsed;
            early |= !RewardsReady();
            coin.rectTransform.GetWorldCorners(corners);
            var coinCanvas = coin.canvas;
            var coinCamera = coinCanvas && coinCanvas.renderMode != UnityEngine.RenderMode.ScreenSpaceOverlay ? coinCanvas.worldCamera : null;
            float margin = float.PositiveInfinity, left = float.PositiveInfinity, right = float.NegativeInfinity, bottom = float.PositiveInfinity, top = float.NegativeInfinity;
            int outside = 0;
            foreach (var world in corners)
            {
                var screen = UnityEngine.RectTransformUtility.WorldToScreenPoint(coinCamera, world);
                UnityEngine.Vector2 p;
                if (!UnityEngine.RectTransformUtility.ScreenPointToLocalPointInRectangle(frame, screen, frameCamera, out p))
                {
                    outside++; margin = float.NegativeInfinity; continue;
                }
                left = UnityEngine.Mathf.Min(left, p.x); right = UnityEngine.Mathf.Max(right, p.x);
                bottom = UnityEngine.Mathf.Min(bottom, p.y); top = UnityEngine.Mathf.Max(top, p.y);
                float m = UnityEngine.Mathf.Min(p.x - safe.xMin, safe.xMax - p.x, p.y - safe.yMin, safe.yMax - p.y);
                margin = UnityEngine.Mathf.Min(margin, m);
                if (m < -tolerance) outside++;
            }
            worstMargin = UnityEngine.Mathf.Min(worstMargin, margin); escapedCorners += outside;
            if (outside > 0 && firstEscape == null) firstEscape = "t=" + F(elapsed) + " id=" + coinId + " margin=" + F(margin);
            coinSamples.Append(aspect).Append('\t').Append(F(elapsed)).Append('\t').Append(coinId).Append('\t').Append(F(VisibleAlpha(coin))).Append('\t')
                .Append(F(left)).Append('\t').Append(F(right)).Append('\t').Append(F(bottom)).Append('\t').Append(F(top)).Append('\t').Append(F(margin)).Append('\t').Append(outside).Append('\n');
        }
        if (visible > 0) visibleFrames++;
        maxVisible = global::System.Math.Max(maxVisible, visible);
    }
    Check(UnityEngine.Screen.width + "x" + UnityEngine.Screen.height == aspect, aspect + " rendered at the required dimensions");
    Check(seen.Count >= 10 && visibleFrames >= 10 && quadSamples >= 30 && maxVisible >= 3 && lastCoin - firstCoin > .45f,
        aspect + " observed full coin flight: " + seen.Count + " coins, " + visibleFrames + " visible frames, " + quadSamples + " quads, " + F(lastCoin - firstCoin) + "s");
    Check(quadSamples > 0 && escapedCorners == 0, aspect + " all " + (quadSamples * 4) + " visible sprite corners stay 22 units inside the popup (worst margin " + F(worstMargin) + "; first escape " + (firstEscape ?? "none") + ")");
    Check(firstCoin >= 0 && !early && !CoinVisible(), aspect + " flights start after reward readiness and finish within observation");
    Check(Economy() == economy, aspect + " flight presentation leaves currency, stars and scores unchanged");
    Note("COIN OBSERVATION " + aspect + ": " + frames + " rendered frames; alpha threshold .001; bounds checked on the complete Image quad.");
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
global::System.Collections.IEnumerator OpenCard(bool coins, int level = 23)
{
    menus.HideAllCanvasElements();
    JellyGarden.Scripts.InitScript.Instance.TryOpenLevel(level); yield return null; menus.menuPlay.SetActive(false);
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
    Check(SelectSize("1080x1920"), "1080x1920 preset is available"); yield return Wait(.3f);
    yield return OpenCard(true);
    Check(Member(piece, "pillowGlow") != null && Member(piece, "rays") != null && Member(piece, "rewardBadge") != null && Member(piece, "coinOrigin") != null && Member(piece, "starShadow") != null && Member(piece, "contactShadow") != null && Member(piece, "haloSparkles") != null, "pillow glow, rays, halo sparkles, reward badge, coin origin and both shadows are bound");
    string economyBefore = Economy();
    float start = UnityEngine.Time.unscaledTime, starAt = -1, landedAt = -1, readyAt = -1, coinAt = -1, settledAt = -1;
    float maxScale = 0, minScale = 9, rebound = 0, minCushion = 9, flashPeak = 0, glowPeak = 0, pillowPeak = 0, firstScale = 0;
    int burst = 0, idle = 0, haloIdle = 0, haloIdleFrames = 0, haloReportedPeak = 0; float idleMaxSize = 0; bool earlyFlight = false, haloMoved = false;
    var haloInitialPositions = new global::System.Collections.Generic.Dictionary<string, UnityEngine.Vector2>();
    while (UnityEngine.Time.unscaledTime - start < 6f)
    {
        float now = UnityEngine.Time.unscaledTime, elapsed = now - start;
        bool on = piece.star && piece.star.gameObject.activeInHierarchy;
        if (on && starAt < 0) { starAt = now; firstScale = piece.star.localScale.x; }
        float since = starAt < 0 ? -1 : now - starAt;
        if (on)
        {
            float scale = piece.star.localScale.x; maxScale = UnityEngine.Mathf.Max(maxScale, scale);
            if (since < .68f) minScale = UnityEngine.Mathf.Min(minScale, scale);
            if (since >= .52f && since < .82f) rebound = UnityEngine.Mathf.Max(rebound, scale);
            if (since >= .80f && settledAt < 0 && UnityEngine.Mathf.Abs(scale - 1) < .002f && (piece.star.anchoredPosition - piece.StarRest).magnitude < .2f) settledAt = now;
        }
        minCushion = UnityEngine.Mathf.Min(minCushion, piece.cushion.localScale.y);
        flashPeak = UnityEngine.Mathf.Max(flashPeak, MemberAlpha("flash")); glowPeak = UnityEngine.Mathf.Max(glowPeak, MemberAlpha("glow")); pillowPeak = UnityEngine.Mathf.Max(pillowPeak, MemberAlpha("pillowGlow"));
        int visible = SparklesVisible(); if (since >= 0 && since < 1.8f) burst = global::System.Math.Max(burst, visible); if (since >= 3) idle = global::System.Math.Max(idle, visible);
        int haloVisible = HaloVisible();
        if (since >= 3)
        {
            haloIdle = global::System.Math.Max(haloIdle, haloVisible);
            if (haloVisible > 0) haloIdleFrames++;
            if (Member(piece, "ActiveHaloSparkles") is int activeHalo) haloReportedPeak = global::System.Math.Max(haloReportedPeak, activeHalo);
            foreach (var graphic in Graphics(Member(piece, "haloSparkles")))
            {
                if (VisibleAlpha(graphic) <= .10f) continue;
                string id = graphic.GetEntityId().ToString(); UnityEngine.Vector2 initial;
                if (!haloInitialPositions.TryGetValue(id, out initial)) haloInitialPositions[id] = graphic.rectTransform.anchoredPosition;
                else if ((graphic.rectTransform.anchoredPosition - initial).magnitude > 4f) haloMoved = true;
            }
        }
        if (since >= 3 && piece.sparkles)
            foreach (var sparkle in piece.sparkles.GetComponentsInChildren<UnityEngine.UI.RawImage>(true))
                if (VisibleAlpha(sparkle) > .15f) idleMaxSize = UnityEngine.Mathf.Max(idleMaxSize, sparkle.rectTransform.rect.width * UnityEngine.Mathf.Abs(sparkle.rectTransform.localScale.x));
        if (piece.Landed && landedAt < 0) landedAt = now;
        if (RewardsReady() && readyAt < 0) readyAt = now;
        bool flying = CoinVisible(); if (flying && coinAt < 0) coinAt = now; if (flying && !RewardsReady()) earlyFlight = true;
        samples.Append(F(elapsed)).Append('\t').Append(F(since)).Append('\t').Append(F(on ? piece.star.localScale.x : 0)).Append('\t').Append(F(piece.cushion.localScale.y)).Append('\t').Append(F(MemberAlpha("flash"))).Append('\t').Append(F(MemberAlpha("glow"))).Append('\t').Append(F(MemberAlpha("pillowGlow"))).Append('\t').Append(visible).Append('\t').Append(piece.Landed).Append('\t').Append(RewardsReady()).Append('\t').Append(flying).Append('\t').Append(F(ShadowCompositeAlpha())).Append('\t').Append(F(MemberAlpha("contactShadow"))).Append('\t').Append(haloVisible).Append('\n');
        yield return null;
    }
    Check(starAt >= 0 && maxScale >= 1.10f && maxScale <= 1.14f, "larger reward star enters with restrained scale (max " + F(maxScale) + ", first " + F(firstScale) + ")");
    Check(landedAt >= starAt && landedAt - starAt >= .40f && landedAt - starAt <= .58f, "precious-reward landing has a measured arrival near 0.48s (" + F(landedAt - starAt) + "s)");
    Check(minScale >= .935f && minScale <= .985f && rebound > 1.005f && rebound <= 1.055f, "star gives slightly and settles with a small rebound (min " + F(minScale) + ", rebound " + F(rebound) + ")");
    Check(settledAt > 0 && settledAt - starAt >= .80f && settledAt - starAt <= 1.08f && UnityEngine.Mathf.Abs(piece.star.localScale.x - 1) < .001f, "star settles near 0.90s (" + F(settledAt - starAt) + "s)");
    Check(minCushion < .98f && minCushion > .90f && UnityEngine.Mathf.Abs(piece.cushion.localScale.y - 1) < .001f, "pillow gives gently and returns to rest (min " + F(minCushion) + ")");
    Check(flashPeak > .10f && flashPeak <= .28f, "restrained impact flash remains visible without a bright wash (peak " + F(flashPeak) + ")");
    Check(glowPeak > .28f && glowPeak < .46f && pillowPeak > .08f && pillowPeak < .17f, "star and pillow carry a controlled warm glow (" + F(glowPeak) + "/" + F(pillowPeak) + ")");
    Check(burst >= 3, "landing sparkle burst is visible (max " + burst + ")");
    Check(idle >= 4, "idle star and braid sweep remains visibly populated after 3 seconds (max " + idle + ")");
    Check(idleMaxSize >= 42f, "idle hero glints are visibly larger than the former tiny twinkles (max " + F(idleMaxSize) + " card units)");
    Check(haloIdle >= 2 && haloIdleFrames >= 10 && haloReportedPeak >= haloIdle, "rear halo sparkle animation remains visible during idle (max " + haloIdle + ", " + haloIdleFrames + " frames; reported " + haloReportedPeak + ")");
    Check(haloMoved, "visible halo glints continue travelling behind the star after the landing");
    var amount = Member(piece, "rewardAmount") as UnityEngine.UI.Text;
    Check(amount && amount.text.Contains("65") && amount.text.Contains("+") && MemberAlpha("rewardBadge") > .9f, "reward badge visibly reads +65 (" + (amount ? amount.text : "missing") + ")");
    Check(readyAt > settledAt && readyAt - starAt >= 1.03f && readyAt - starAt <= 1.30f && coinAt >= readyAt && !earlyFlight, "coin flights wait until the precious-reward landing has fully settled (ready " + F(readyAt - starAt) + "s; first coin " + F(coinAt - starAt) + "s)");
    Check(Economy() == economyBefore, "the 65-coin presentation does not mutate reward balances or stars");
    AuditArtwork(); AuditSharedShell(); AuditLevelNumber(23);
    AuditLayout("portrait"); yield return Shot("settled-1080x1920");
    int baselineSparkles = SparkleCount(), baselineHalo = HaloCount(); int baselineChildren = card.GetComponentsInChildren<UnityEngine.Transform>(true).Length;
    Check(baselineSparkles == 38, "24 landing and 14 idle sparkles are pooled once (" + baselineSparkles + ")");
    Check(baselineHalo > 0, "rear halo has its own reusable sparkle pool (" + baselineHalo + ")");
    for (int i = 1; i <= 3; i++)
    {
        int checkLevel = i % 2 == 0 ? 23 : 24;
        menu.gameObject.SetActive(false); yield return Wait(.25f); yield return OpenCard(false, checkLevel); yield return Wait(2.4f);
        AuditLevelNumber(checkLevel);
        Check(SparkleCount() == baselineSparkles && HaloCount() == baselineHalo && card.GetComponentsInChildren<UnityEngine.Transform>(true).Length == baselineChildren, "reopen " + i + " reuses front and halo effects without duplicate objects");
    }
    // Capture separately so JPEG encoding cannot distort the temporal assertions above.
    menu.gameObject.SetActive(false); yield return Wait(.25f);
    string frameFolderName = "frames-" + global::System.DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");
    string frameDir = global::System.IO.Path.Combine(dir, frameFolderName); global::System.IO.Directory.CreateDirectory(frameDir);
    var times = new global::System.Collections.Generic.List<float>(); var paths = new global::System.Collections.Generic.List<string>();
    var frameLog = new global::System.Text.StringBuilder("frame\telapsed_s\n");
    yield return OpenCard(true); float recordStart = UnityEngine.Time.unscaledTime, nextSample = recordStart;
    while (UnityEngine.Time.unscaledTime - recordStart < 6f)
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
    Check(times.Count >= 18 && times[times.Count - 1] >= 5.5f, "recording covers entrance, shadows and repeated idle sparkle sweep (" + times.Count + " samples)");
    Note("CAPTURE " + times.Count + " actual-time JPG samples over 6s; ffconcat durations preserve observed playback. No audio recorded.");
    // Each aspect gets a fresh real flight. Resizing only a settled card would never exercise coin bounds.
    foreach (var size in new[] { "1080x1920", "720x1280", "1536x2048" })
    {
        menu.gameObject.SetActive(false); yield return Wait(.25f);
        bool selected = SelectSize(size); Check(selected, size + " required Game view preset is available");
        if (!selected) continue;
        yield return Wait(.6f); yield return OpenCard(true); yield return ObserveCoinBounds(size);
        AuditLayout(size); AuditArtwork(); AuditSharedShell(); AuditLevelNumber(23); yield return Shot("settled-" + size);
        Check(SparkleCount() == baselineSparkles && HaloCount() == baselineHalo && card.GetComponentsInChildren<UnityEngine.Transform>(true).Length == baselineChildren, size + " flight reopen preserves front and halo effect pools");
    }
    SelectSize("1080x1920"); yield return Wait(.4f);
    menu.gameObject.SetActive(false); yield return Wait(.25f); RoyalThrone.Testing.RoyalSave.SetInt("RoyalThrone.HomeMotion", 0);
    yield return OpenCard(true); start = UnityEngine.Time.unscaledTime; float calmScaleError = 0, calmCushionError = 0, calmRays = 0, calmFlash = 0, calmShadowError = 0; int calmSparkles = 0, calmHalo = 0; bool calmCoin = false;
    string calmEconomy = Economy();
    while (UnityEngine.Time.unscaledTime - start < 2.2f)
    {
        if (piece.star.gameObject.activeInHierarchy) calmScaleError = UnityEngine.Mathf.Max(calmScaleError, UnityEngine.Mathf.Abs(piece.star.localScale.x - 1));
        calmCushionError = UnityEngine.Mathf.Max(calmCushionError, (piece.cushion.localScale - UnityEngine.Vector3.one).magnitude);
        calmRays = UnityEngine.Mathf.Max(calmRays, MemberAlpha("rays")); calmFlash = UnityEngine.Mathf.Max(calmFlash, MemberAlpha("flash"));
        calmSparkles = global::System.Math.Max(calmSparkles, SparklesVisible()); calmHalo = global::System.Math.Max(calmHalo, HaloVisible()); calmCoin |= CoinVisible();
        if (UnityEngine.Time.unscaledTime - start > 1.2f)
        {
            var shadow = MemberTransform("starShadow"); var group = shadow ? shadow.GetComponent<UnityEngine.CanvasGroup>() : null;
            calmShadowError = UnityEngine.Mathf.Max(calmShadowError, group ? UnityEngine.Mathf.Abs(group.alpha - .28f) : 1f, UnityEngine.Mathf.Abs(MemberAlpha("contactShadow") - .25f));
        }
        yield return null;
    }
    Check(calmScaleError < .001f && calmCushionError < .001f, "Motion off: star stays at 1x and pillow never compresses");
    Check(calmRays < .001f && calmFlash < .001f && calmSparkles == 0 && calmHalo == 0 && !calmCoin, "Motion off: rays, flash, front and halo sparkles, and coin flights stay off");
    Check(calmShadowError < .002f && ShadowGroupAlpha() > .20f && ShadowCompositeAlpha() > .15f,
        "Motion off: soft silhouette and contact shadow stay visible and steady");
    Check(Economy() == calmEconomy, "Motion off: reward presentation does not mutate currency or stars");
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
        global::System.IO.File.WriteAllText(global::System.IO.Path.Combine(dir, "coin-bounds.tsv"), coinSamples.ToString());
        if (!cleanupCompleted && ownsQa && qa && RoyalThrone.Testing.RoyalSave.Testing)
        {
            Note("INTERRUPTED: scheduled owned QA restoration; verify Testing=false before continuing."); qa.StartCoroutine(qa.EndSession());
        }
    }
}
RoyalThrone.Editor.RoyalRegressionRunner.Run(Run());
return "Win-card verification started; poll Design/Cards/LevelCompleted-v4/Verification/checks.txt for DONE.";
