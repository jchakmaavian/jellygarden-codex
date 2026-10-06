// Run with unity command --project-path <project> eval_file --file Design/Cards/LevelCompleted-v5/verify_card_motion.cs
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
var dir = global::System.IO.Path.GetFullPath("Design/Cards/LevelCompleted-v5/Verification/CardMotion");
global::System.IO.Directory.CreateDirectory(dir);
var log = new global::System.Text.StringBuilder();
var samples = new global::System.Text.StringBuilder("aspect\twall_s\tstar_s\tstar_scale\trotation_deg\tcenter_offset\tcushion_y\tflash\tglow\tpillow_glow\ttitle_written\tfront_sparkles\thalo_sparkles\tfloating_reward\tcontinue_alpha\tfloating_alpha\tfloating_path\n");
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
    Check(!piece.rewardBadge && !piece.rewardAmount && !piece.coinOrigin && (!badge || !badge.gameObject.activeInHierarchy), label + " star ceremony has no reward badge or coin origin");
    Check(Rect(piece.cushion).yMin >= Rect(next).yMax - 1, label + " cushion tassels clear Continue");
    bool inputs = true; foreach (var graphic in piece.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
        if ((graphic.transform == piece.star || graphic.transform == piece.cushion || graphic.transform == MemberTransform("flash") || graphic.transform.IsChildOf(piece.sparkles) || (MemberTransform("haloSparkles") && graphic.transform.IsChildOf(MemberTransform("haloSparkles"))) || graphic.name.Contains("glow") || graphic.name.Contains("rays") || graphic.name.IndexOf("shadow", global::System.StringComparison.OrdinalIgnoreCase) >= 0) && graphic.raycastTarget) inputs = false;
    Check(inputs, label + " decorative lights, shadows and sparkles do not take taps");
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
string floatingPath = "none"; float floatingAlpha = 0;
string HierarchyPath(UnityEngine.Transform target)
{
    string path = target ? target.name : "missing";
    for (var t = target ? target.parent : null; t; t = t.parent) path = t.name + "/" + path;
    return path;
}
bool FloatingRewardVisible()
{
    floatingPath = "none"; floatingAlpha = 0;
    foreach (var graphic in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Graphic>())
    {
        float alpha = VisibleAlpha(graphic); if (alpha <= .01f) continue;
        bool match = graphic.name == "Flying reward" || graphic.name == "+coins" || graphic.name == "Coins won";
        for (var t = graphic.transform; !match && t; t = t.parent) if (t.name == "Royal coin balance") match = true;
        if (match && alpha > floatingAlpha) { floatingAlpha = alpha; floatingPath = HierarchyPath(graphic.transform); }
    }
    return floatingAlpha > .01f;
}
float ContinueAlpha()
{
    var next = card ? card.Find("Next") : null; float alpha = 0;
    foreach (var graphic in Graphics(next)) alpha = UnityEngine.Mathf.Max(alpha, VisibleAlpha(graphic));
    return alpha;
}
void AuditTexture()
{
    var image = piece.star ? piece.star.GetComponent<UnityEngine.UI.Image>() : null;
    var art = UnityEngine.Resources.Load<UnityEngine.Sprite>("RoyalGameplay/win-star");
    Check(image && art && image.sprite == art && art.texture && UnityEngine.Mathf.Max(art.texture.width, art.texture.height) >= 1000,
        "hero uses the high-resolution win-star import (" + (art && art.texture ? art.texture.width + "x" + art.texture.height : "missing") + ")");
    Check(image && !image.raycastTarget, "large star image cannot steal Continue or Close taps");
}
global::System.Collections.IEnumerator ObserveMotion(string aspect, float duration)
{
    string economy = Economy(); float begin = UnityEngine.Time.unscaledTime;
    float firstT = -1, firstWall = -1, maxScale = 0, minScale = 9, rebound = 0, settled = -1, landed = -1, ready = -1;
    float minCushion = 9, maxCushion = 0, cushionSettled = -1, fixedCenterError = 0, maxLift = 0;
    float rotMin = 90, rotMax = -90, flashPeak = 0, idleGlowMin = 9, idleGlowMax = 0, pillowPeak = 0;
    float titleFirst = -1, titleFull = -1, titlePrevious = -1; int titleSteps = 0, temporalSamples = 0, entrySamples = 0;
    int burst = 0, idle = 0, haloIdle = 0, haloFrames = 0; bool floats = false, haloMoved = false, continueHidden = false, titleEarly = false;
    float continueMin = 1, floatAlphaMax = 0, firstFloatAt = -1; string firstFloatPath = "none", brightestFloatPath = "none";
    var haloPositions = new global::System.Collections.Generic.Dictionary<string, UnityEngine.Vector2>();
    while (UnityEngine.Time.unscaledTime - begin < 25f)
    {
        yield return new UnityEngine.WaitForEndOfFrame();
        float t = piece.Elapsed;
        bool currentFloat = FloatingRewardVisible(); floats |= currentFloat;
        float continueAlpha = ContinueAlpha(); continueMin = UnityEngine.Mathf.Min(continueMin, continueAlpha); continueHidden |= continueAlpha < .95f;
        if (currentFloat && firstFloatAt < 0) { firstFloatAt = t; firstFloatPath = floatingPath; }
        if (floatingAlpha > floatAlphaMax) { floatAlphaMax = floatingAlpha; brightestFloatPath = floatingPath; }
        bool visible = piece.star && piece.star.gameObject.activeInHierarchy && Alpha(piece.star) > .9f;
        if (!visible) continue;
        if (firstT < 0) { firstT = t; firstWall = UnityEngine.Time.unscaledTime - begin; }
        temporalSamples++; if (t < .65f) entrySamples++;
        float size = piece.star.localScale.x, offset = (piece.star.anchoredPosition - piece.StarRest).magnitude;
        float rotation = UnityEngine.Mathf.DeltaAngle(0, piece.star.localEulerAngles.z);
        maxScale = UnityEngine.Mathf.Max(maxScale, size);
        if (t <= .30f) minScale = UnityEngine.Mathf.Min(minScale, size);
        if (t >= .30f && t < .65f) rebound = UnityEngine.Mathf.Max(rebound, size);
        if (t < .195f) fixedCenterError = UnityEngine.Mathf.Max(fixedCenterError, offset);
        maxLift = UnityEngine.Mathf.Max(maxLift, offset);
        rotMin = UnityEngine.Mathf.Min(rotMin, rotation); rotMax = UnityEngine.Mathf.Max(rotMax, rotation);
        if (t >= .60f && settled < 0 && UnityEngine.Mathf.Abs(size - 1) < .001f && offset < .1f && UnityEngine.Mathf.Abs(rotation) < .1f) settled = t;
        float pillowY = piece.cushion.localScale.y;
        minCushion = UnityEngine.Mathf.Min(minCushion, pillowY); maxCushion = UnityEngine.Mathf.Max(maxCushion, pillowY);
        if (t >= .90f && cushionSettled < 0 && (piece.cushion.localScale - UnityEngine.Vector3.one).magnitude < .001f) cushionSettled = t;
        flashPeak = UnityEngine.Mathf.Max(flashPeak, MemberAlpha("flash")); pillowPeak = UnityEngine.Mathf.Max(pillowPeak, MemberAlpha("pillowGlow"));
        int front = SparklesVisible(), halo = HaloVisible(); if (t < 1.4f) burst = global::System.Math.Max(burst, front);
        if (t > 1.5f)
        {
            float glow = MemberAlpha("glow"); idleGlowMin = UnityEngine.Mathf.Min(idleGlowMin, glow); idleGlowMax = UnityEngine.Mathf.Max(idleGlowMax, glow);
            idle = global::System.Math.Max(idle, front); haloIdle = global::System.Math.Max(haloIdle, halo); if (halo > 0) haloFrames++;
            foreach (var graphic in Graphics(Member(piece, "haloSparkles")))
            {
                if (VisibleAlpha(graphic) <= .10f) continue;
                string id = graphic.GetEntityId().ToString(); UnityEngine.Vector2 initial;
                if (!haloPositions.TryGetValue(id, out initial)) haloPositions[id] = graphic.rectTransform.anchoredPosition;
                else if ((graphic.rectTransform.anchoredPosition - initial).magnitude > 4f) haloMoved = true;
            }
        }
        float written = piece.title ? piece.title.written : -1;
        if (t < .16f && written > 0) titleEarly = true;
        if (written > 0 && titleFirst < 0) titleFirst = t;
        if (float.IsPositiveInfinity(written) && titleFull < 0) titleFull = t;
        if (written > 0 && !float.IsInfinity(written) && written > titlePrevious + .05f) { titleSteps++; titlePrevious = written; }
        if (piece.Landed && landed < 0) landed = t;
        if (RewardsReady() && ready < 0) ready = t;
        samples.Append(aspect).Append('\t').Append(F(UnityEngine.Time.unscaledTime - begin)).Append('\t').Append(F(t)).Append('\t').Append(F(size)).Append('\t').Append(F(rotation)).Append('\t').Append(F(offset)).Append('\t').Append(F(pillowY)).Append('\t').Append(F(MemberAlpha("flash"))).Append('\t').Append(F(MemberAlpha("glow"))).Append('\t').Append(F(MemberAlpha("pillowGlow"))).Append('\t').Append(F(written)).Append('\t').Append(front).Append('\t').Append(halo).Append('\t').Append(currentFloat).Append('\t').Append(F(continueAlpha)).Append('\t').Append(F(floatingAlpha)).Append('\t').Append(floatingPath).Append('\n');
        if (t >= duration) break;
    }
    Check(UnityEngine.Screen.width + "x" + UnityEngine.Screen.height == aspect, aspect + " renders at requested dimensions");
    Check(temporalSamples >= 35 && entrySamples >= 12 && piece.Elapsed >= duration, aspect + " sampled the full motion (" + temporalSamples + "; entrance " + entrySamples + "; first t " + F(firstT) + ")");
    Check(firstWall >= 0 && firstWall <= .18f, aspect + " star starts with the instant shell rather than after the former generic entrance (" + F(firstWall) + " wall seconds)");
    Check(firstT <= .05f && maxScale >= 2.20f && maxScale <= 2.45f, aspect + " oversized hero begins the reference zoom (peak " + F(maxScale) + ")");
    Check(minScale >= .70f && minScale < .95f && rebound >= 1.04f && rebound <= 1.12f, aspect + " hero shrinks below rest then rebounds (min " + F(minScale) + ", rebound " + F(rebound) + ")");
    Check(fixedCenterError < .25f && maxLift <= 8f, aspect + " zoom retracts around a fixed centre with only a short rebound (" + F(fixedCenterError) + "/" + F(maxLift) + ")");
    Check(rotMax >= 20f && rotMax <= 32f && rotMin < -3f, aspect + " entrance rotates to both sides then damps (" + F(rotMin) + " to " + F(rotMax) + " degrees)");
    Check(landed >= .195f && landed <= .26f && settled >= .60f && settled <= .73f, aspect + " impact and star settle follow the fast reference timing (" + F(landed) + "/" + F(settled) + "s)");
    Check(minCushion >= .85f && minCushion < .98f && maxCushion > 1.23f && maxCushion <= 1.33f && cushionSettled >= .90f && cushionSettled <= 1.08f,
        aspect + " velvet compresses, stretches and settles (" + F(minCushion) + "/" + F(maxCushion) + "; " + F(cushionSettled) + "s)");
    Check(flashPeak >= .35f && flashPeak <= .54f && pillowPeak >= .18f, aspect + " impact lights the rear halo and velvet (" + F(flashPeak) + "/" + F(pillowPeak) + ")");
    Check(idleGlowMin >= .245f && idleGlowMax <= .315f, aspect + " idle halo stays warm and restrained (" + F(idleGlowMin) + " to " + F(idleGlowMax) + ")");
    Check(!titleEarly && titleFirst >= .17f && titleFirst <= .26f && titleSteps >= 4 && titleFull >= .50f && titleFull <= .68f, aspect + " title writes progressively around zoom impact (" + F(titleFirst) + " to " + F(titleFull) + "s; " + titleSteps + " steps)");
    Check(burst >= 3 && idle >= 3 && haloIdle >= 2 && haloFrames >= 10 && haloMoved, aspect + " burst transitions into moving rear halo and idle glints (" + burst + "/" + idle + "/" + haloIdle + ")");
    Check(ready >= .95f && ready <= 1.08f, aspect + " celebration becomes ready after velvet settles (" + F(ready) + "s)");
    Check(!continueHidden, aspect + " Continue stays visible throughout the ceremony (minimum alpha " + F(continueMin) + ")");
    Check(!floats && !CoinVisible(), aspect + " popup has no floating coins, count pill or reward label (first " + F(firstFloatAt) + "s: " + firstFloatPath + "; max alpha " + F(floatAlphaMax) + ": " + brightestFloatPath + ")");
    Check(Economy() == economy, aspect + " card presentation leaves coins, stars, lives and scores unchanged");
}
global::System.Collections.IEnumerator Capture()
{
    menu.gameObject.SetActive(false); yield return Wait(.2f); yield return OpenCard(true);
    string folder = "frames-" + global::System.DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");
    string frameDir = global::System.IO.Path.Combine(dir, folder); global::System.IO.Directory.CreateDirectory(frameDir);
    var times = new global::System.Collections.Generic.List<float>(); var paths = new global::System.Collections.Generic.List<string>();
    var frameLog = new global::System.Text.StringBuilder("frame\twall_s\tstar_s\tstar_scale\tcushion_y\n");
    float recordStart = UnityEngine.Time.unscaledTime, nextSample = recordStart;
    while (UnityEngine.Time.unscaledTime - recordStart < 18f)
    {
        yield return new UnityEngine.WaitForEndOfFrame();
        float now = UnityEngine.Time.unscaledTime; if (now < nextSample) continue;
        float stamp = now - recordStart; var tex = UnityEngine.ScreenCapture.CaptureScreenshotAsTexture();
        string filename = times.Count.ToString("D5") + ".jpg";
        try { global::System.IO.File.WriteAllBytes(global::System.IO.Path.Combine(frameDir, filename), UnityEngine.ImageConversion.EncodeToJPG(tex, 90)); }
        finally { UnityEngine.Object.Destroy(tex); }
        paths.Add(folder + "/" + filename); times.Add(stamp);
        frameLog.Append(times.Count - 1).Append('\t').Append(F(stamp)).Append('\t').Append(F(piece.Elapsed)).Append('\t').Append(F(piece.star.localScale.x)).Append('\t').Append(F(piece.cushion.localScale.y)).Append('\n');
        nextSample = now + (piece.Elapsed < 1.4f ? 1f / 60f : 1f / 15f);
        if (piece.Elapsed >= 3.5f) break;
    }
    var concat = new global::System.Text.StringBuilder("ffconcat version 1.0\n");
    for (int i = 0; i < times.Count; i++) { concat.Append("file '").Append(paths[i]).Append("'\n"); concat.Append("duration ").Append(F(i + 1 < times.Count ? times[i + 1] - times[i] : 1f / 30f)).Append('\n'); }
    if (paths.Count > 0) concat.Append("file '").Append(paths[paths.Count - 1]).Append("'\n");
    global::System.IO.File.WriteAllText(global::System.IO.Path.Combine(dir, "capture.ffconcat"), concat.ToString());
    global::System.IO.File.WriteAllText(global::System.IO.Path.Combine(dir, "capture-timestamps.tsv"), frameLog.ToString());
    Check(times.Count >= 24 && piece.Elapsed >= 3.5f, "separate capture includes full zoom, recoil and idle with actual wall and animation timestamps (" + times.Count + " frames)");
    Note("CAPTURE JPEG encoding is separate from assertion runs; ffconcat preserves actual wall-time playback, with per-frame animation times logged. No audio.");
}
global::System.Collections.IEnumerator Body()
{
    qa.StartSession(); ownsQa = RoyalThrone.Testing.RoyalSave.Testing;
    if (!ownsQa) throw new global::System.Exception("QA profile failed to start.");
    qa.Visible = false; qa.Counters = false; yield return null; yield return null;
    motionBefore = RoyalThrone.Testing.RoyalSave.GetInt("RoyalThrone.HomeMotion", 1);
    RoyalThrone.Testing.RoyalSave.SetInt("RoyalThrone.HomeMotion", 1);
    UnityEngine.Application.runInBackground = true; UnityEngine.Time.captureDeltaTime = 0;
    menu = menus.GetCanvasGlobal().Find("MenuComplete"); card = menu ? menu.Find("Image") : null;
    if (!menu || !card) throw new global::System.Exception("Win menu not found.");
    int frontPool = -1, haloPool = -1, childPool = -1;
    foreach (var size in new[] { "1080x1920", "720x1280", "1536x2048" })
    {
        menu.gameObject.SetActive(false); yield return Wait(.2f);
        bool selected = SelectSize(size); Check(selected, size + " required Game view preset exists"); if (!selected) continue;
        yield return Wait(.4f); yield return OpenCard(true);
        var shell = menu.GetComponent<RoyalThrone.Presentation.RoyalCardMotion>();
        Check(shell && shell.instantRewardEntrance && Alpha(card) > .99f && ContinueAlpha() > .99f && (card.localScale - UnityEngine.Vector3.one).magnitude < .001f,
            size + " shell and Continue are visible immediately on opening");
        yield return ObserveMotion(size, 4.5f);
        AuditTexture(); AuditArtwork(); AuditSharedShell(); AuditLayout(size); AuditLevelNumber(23);
        if (frontPool < 0) { frontPool = SparkleCount(); haloPool = HaloCount(); childPool = card.GetComponentsInChildren<UnityEngine.Transform>(true).Length; }
        Check(SparkleCount() == 38 && HaloCount() == 12, size + " front and rear sparkle pools have 38 and 12 reusable images");
        Check(SparkleCount() == frontPool && HaloCount() == haloPool && card.GetComponentsInChildren<UnityEngine.Transform>(true).Length == childPool, size + " reopen preserves complete effect pools");
        yield return Shot("settled-" + size);
    }
    SelectSize("1080x1920"); yield return Wait(.4f);
    for (int i = 0; i < 3; i++)
    {
        menu.gameObject.SetActive(false); yield return Wait(.2f); yield return OpenCard(true, i % 2 == 0 ? 24 : 23); yield return Wait(1.5f);
        AuditLevelNumber(i % 2 == 0 ? 24 : 23);
        Check(SparkleCount() == frontPool && HaloCount() == haloPool && card.GetComponentsInChildren<UnityEngine.Transform>(true).Length == childPool, "additional reopen " + (i + 1) + " reuses both particle pools");
        Check(!FloatingRewardVisible(), "additional reopen " + (i + 1) + " has no popup coin graphics (" + floatingPath + "; alpha " + F(floatingAlpha) + ")");
    }
    yield return Capture();
    menu.gameObject.SetActive(false); yield return Wait(.2f); RoyalThrone.Testing.RoyalSave.SetInt("RoyalThrone.HomeMotion", 0);
    yield return OpenCard(true); string calmEconomy = Economy(); float begin = UnityEngine.Time.unscaledTime;
    float starError = 0, cushionError = 0, rotationError = 0, flash = 0, rays = 0, shadowError = 0; int frontMax = 0, haloMax = 0; bool floating = false;
    while (UnityEngine.Time.unscaledTime - begin < 2.2f)
    {
        yield return new UnityEngine.WaitForEndOfFrame();
        starError = UnityEngine.Mathf.Max(starError, (piece.star.localScale - UnityEngine.Vector3.one).magnitude, (piece.star.anchoredPosition - piece.StarRest).magnitude);
        cushionError = UnityEngine.Mathf.Max(cushionError, (piece.cushion.localScale - UnityEngine.Vector3.one).magnitude);
        rotationError = UnityEngine.Mathf.Max(rotationError, UnityEngine.Mathf.Abs(UnityEngine.Mathf.DeltaAngle(0, piece.star.localEulerAngles.z)));
        flash = UnityEngine.Mathf.Max(flash, MemberAlpha("flash")); rays = UnityEngine.Mathf.Max(rays, MemberAlpha("rays"));
        frontMax = global::System.Math.Max(frontMax, SparklesVisible()); haloMax = global::System.Math.Max(haloMax, HaloVisible()); floating |= FloatingRewardVisible();
        if (piece.Elapsed > .4f) shadowError = UnityEngine.Mathf.Max(shadowError, UnityEngine.Mathf.Abs(ShadowGroupAlpha() - .28f), UnityEngine.Mathf.Abs(MemberAlpha("contactShadow") - .25f));
    }
    Check(starError < .001f && cushionError < .001f && rotationError < .001f, "Motion off: star and velvet remain still at rest");
    Check(flash < .001f && rays < .001f && frontMax == 0 && haloMax == 0 && !floating, "Motion off: no flash, rays, front or halo particles, or floating rewards");
    Check(shadowError < .002f && ShadowCompositeAlpha() > .15f, "Motion off: soft star/contact shadows remain visible and steady");
    Check(RewardsReady() && piece.title && float.IsPositiveInfinity(piece.title.written) && ContinueAlpha() > .99f, "Motion off: final title and Continue are readable and celebration becomes ready");
    Check(Economy() == calmEconomy, "Motion off: presentation leaves economy unchanged"); yield return Shot("motion-off");
    RoyalThrone.Testing.RoyalSave.SetInt("RoyalThrone.HomeMotion", 1);
    foreach (var control in new[] { "Next", "Royal close" })
    {
        menu.gameObject.SetActive(false); yield return Wait(.2f); yield return OpenCard(false); yield return Wait(1.5f);
        string economy = Economy(); yield return Tap(card.Find(control)); yield return Wait(.8f);
        Check(!menu.gameObject.activeSelf && lm.gameStatus.ToString() == "Map", control + " follows Continue to Home");
        Check(Economy() == economy, control + " does not award coins or stars again"); menus.menuPlay.SetActive(false);
    }
    Check(Snapshot(false) == realBefore, "real PlayerPrefs unchanged while QA is active");
}
global::System.Collections.IEnumerator Run()
{
    // Catch errors while manually flattening child iterators, then await cleanup outside catch/finally.
    var stack = new global::System.Collections.Generic.Stack<global::System.Collections.IEnumerator>();
    UnityEngine.Application.logMessageReceived += onLog; listening = true;
    Note("Started " + global::System.DateTime.UtcNow.ToString("o") + "; v5 presentation-only level 23; star ceremony and deferred 65-coin amount; no reward commit.");
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
return "Win-card verification started; poll Design/Cards/LevelCompleted-v5/Verification/CardMotion/checks.txt for DONE.";
