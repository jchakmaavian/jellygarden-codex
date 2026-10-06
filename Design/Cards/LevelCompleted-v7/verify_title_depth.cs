// Editor eval only. Run when the editor owner permits: eval_file --file Design/Cards/LevelCompleted-v7/verify_title_buttons.cs
// Presentation-only owned QA session: no CommitWin, gameplay reward, shop purchase or permanent player edit.
#pragma warning disable 0162
if (!UnityEngine.Application.isPlaying || RoyalThrone.Testing.RoyalSave.Testing) throw new global::System.InvalidOperationException("Use idle Play Mode Home with no existing QA session.");
var qa = RoyalThrone.Testing.RoyalQaService.Instance;
var lm = JellyGarden.Scripts.LevelManager.THIS;
var menus = JellyGarden.Scripts.GUI.MenuManager.Instance;
var home = UnityEngine.Object.FindAnyObjectByType<RoyalThrone.Presentation.RoyalHomeScreen>();
if (!qa || !qa.Ready || qa.Busy || !lm || !menus || !home || lm.gameStatus != JellyGarden.Scripts.GameState.Map) throw new global::System.InvalidOperationException("Return to ready idle Home first.");
var dir = global::System.IO.Path.GetFullPath("Design/Cards/LevelCompleted-v7/Verification/TitleButtons");
global::System.IO.Directory.CreateDirectory(dir);
var checkPath = global::System.IO.Path.GetFullPath("Design/Cards/LevelCompleted-v7/Verification/title-buttons-checks.txt");
var log = new global::System.Text.StringBuilder();
var sampleLog = new global::System.Text.StringBuilder("aspect\twall_s\ttitle_s\tglyph\tlogical_index\talpha\tscale\tbaseline_error\tcrest_gap_local\n");
var errors = new global::System.Collections.Generic.List<string>();
var flags = global::System.Reflection.BindingFlags.Instance | global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.NonPublic;
var oldSizes = new global::System.Collections.Generic.Dictionary<UnityEngine.Object, int>();
global::System.Reflection.PropertyInfo sizeProperty = null;
int passes = 0, fails = 0; bool ownsQa = false, cleaned = false;
float oldCapture = UnityEngine.Time.captureDeltaTime; bool oldBackground = UnityEngine.Application.runInBackground; int oldRate = UnityEngine.Application.targetFrameRate;
var oldCatalog = home.levelNumbers; var oldCatalogValues = oldCatalog == null ? null : (int[])oldCatalog.Clone();
UnityEngine.GameObject board = null;
UnityEngine.Transform menu = null, card = null;
RoyalThrone.Presentation.RoyalWinCard piece = null;
UnityEngine.UI.Text heading = null;
var crestPoints = new global::System.Collections.Generic.List<UnityEngine.Vector2>();
string F(float n) => n.ToString("F4", global::System.Globalization.CultureInfo.InvariantCulture);
void Note(string s) { log.AppendLine(s); global::System.IO.File.WriteAllText(checkPath, log.ToString()); }
void Check(bool ok, string s) { if (ok) passes++; else fails++; Note((ok ? "PASS " : "FAIL ") + s); }
UnityEngine.Application.LogCallback onLog = (m, s, t) => { if (t == UnityEngine.LogType.Error || t == UnityEngine.LogType.Exception || t == UnityEngine.LogType.Assert) errors.Add(t + ": " + m); };
string Snapshot(bool routed)
{
    var s = new global::System.Text.StringBuilder();
    foreach (string k in new[] { "OpenLevel", "OpenLevelTest", "Gems", "Lifes", "Colorful_bomb", "Packages", "Stripes", "Cannon", "Hammer", "Hand", "Arrow", "Music", "Sound", "RoyalThrone.HomeMotion", "RoyalThrone.Area.Current", "RoyalThrone.Area.Done", "RoyalThrone.Area.Spent", "RoyalThrone.Area.BoughtStars", "RoyalThrone.Area.Entered", "RoyalThrone.Area.LayoutVersion", "RoyalThrone.Area.MigrationCredit", "RoyalThrone.Area.Guided", "RoyalThrone.Profile.FirstTryWins", "RoyalThrone.Profile.PowerupsMade" })
        s.Append(k).Append('=').Append(routed ? RoyalThrone.Testing.RoyalSave.GetInt(k, -99999) : UnityEngine.PlayerPrefs.GetInt(k, -99999)).Append(';');
    for (int n = 1; n <= 50; n++) foreach (string k in new[] { "Level." + n.ToString("000") + ".StarsCount", "Score" + n, "RoyalThrone.Profile.Tried." + n.ToString("000") })
        s.Append(k).Append('=').Append(routed ? RoyalThrone.Testing.RoyalSave.GetInt(k, -99999) : UnityEngine.PlayerPrefs.GetInt(k, -99999)).Append(';');
    foreach (string k in new[] { "DateOfExit", "RoyalThrone.NextLifeUtc" }) s.Append(k).Append('=').Append(routed ? RoyalThrone.Testing.RoyalSave.GetString(k, "<missing>") : UnityEngine.PlayerPrefs.GetString(k, "<missing>")).Append(';');
    s.Append("timer=").Append(F(routed ? RoyalThrone.Testing.RoyalSave.GetFloat("RestLifeTimer", -99999) : UnityEngine.PlayerPrefs.GetFloat("RestLifeTimer", -99999)));
    return s.ToString();
}
string Economy() => JellyGarden.Scripts.InitScript.Gems + "/" + JellyGarden.Scripts.InitScript.lifes + "/" + RoyalThrone.Testing.RoyalSave.GetInt("Gems") + "/" + RoyalThrone.Testing.RoyalSave.GetInt("Level.023.StarsCount") + "/" + RoyalThrone.Testing.RoyalSave.GetInt("Score23");
string realBefore = Snapshot(false), runtimeBefore = JellyGarden.Scripts.InitScript.Gems + "/" + JellyGarden.Scripts.InitScript.lifes + "/" + lm.currentLevel;
global::System.Collections.IEnumerator Wait(float seconds) { float end = UnityEngine.Time.unscaledTime + seconds; while (UnityEngine.Time.unscaledTime < end) yield return null; }
UnityEngine.Camera CameraFor(UnityEngine.Transform t) { var c = t.GetComponentInParent<UnityEngine.Canvas>(); c = c ? c.rootCanvas : null; return c && c.renderMode != UnityEngine.RenderMode.ScreenSpaceOverlay ? c.worldCamera : null; }
UnityEngine.Rect ScreenRect(UnityEngine.Transform t)
{
    var r = t as UnityEngine.RectTransform; var p = new UnityEngine.Vector3[4]; r.GetWorldCorners(p);
    var lo = new UnityEngine.Vector2(float.PositiveInfinity, float.PositiveInfinity); var hi = -lo;
    foreach (var v in p) { var q = UnityEngine.RectTransformUtility.WorldToScreenPoint(CameraFor(t), v); lo = UnityEngine.Vector2.Min(lo, q); hi = UnityEngine.Vector2.Max(hi, q); }
    return UnityEngine.Rect.MinMaxRect(lo.x, lo.y, hi.x, hi.y);
}
UnityEngine.Rect ToScreen(UnityEngine.Rect r, UnityEngine.Transform t)
{
    var a = UnityEngine.RectTransformUtility.WorldToScreenPoint(CameraFor(t), t.TransformPoint(new UnityEngine.Vector3(r.xMin, r.yMin)));
    var b = UnityEngine.RectTransformUtility.WorldToScreenPoint(CameraFor(t), t.TransformPoint(new UnityEngine.Vector3(r.xMax, r.yMax)));
    return UnityEngine.Rect.MinMaxRect(a.x, a.y, b.x, b.y);
}
int Copies(UnityEngine.Color color, float width) => color.a <= 0 ? 0 : width > 0 ? 8 : 1;
(UnityEngine.Rect[] boxes, float[] alpha, UnityEngine.Rect all, bool faceColors, UnityEngine.Rect[] silhouettes, float skew, float depth) ReadTitle()
{
    // Unity6 returns the renderer's current mesh. Borrow it read-only; never clear or destroy it.
    var renderMesh = heading.canvasRenderer.GetMesh();
    if (!renderMesh) throw new global::System.Exception("The title has no rendered CanvasRenderer mesh.");
    var p = renderMesh.vertices; var c = renderMesh.colors32; var triangles = renderMesh.triangles;
    var style = piece.title; float outer = style.rimWidth + style.outlineWidth;
    int blocks = 1 + Copies(style.shadow, outer) + (style.outlineWidth > 0 ? Copies(style.outline, outer) : 0) + (style.rimWidth > 0 ? Copies(style.rim, style.rimWidth) : 0);
    int stride = style.rewardDepth ? RoyalThrone.Presentation.RoyalTextStyle.RewardDepthVerticesPerGlyph : blocks * 6;
    if (triangles.Length == 0 || triangles.Length % stride != 0) throw new global::System.Exception("Rendered title mesh does not contain complete styled glyph groups.");
    int count = triangles.Length / stride; var boxes = new UnityEngine.Rect[count]; var alpha = new float[count]; bool faceColors = true; var silhouettes = new UnityEngine.Rect[count]; float skew = 0, depth = 0;
    var allLo = new UnityEngine.Vector2(float.PositiveInfinity, float.PositiveInfinity); var allHi = -allLo;
    for (int q = 0; q < count; q++)
    {
        var lo = new UnityEngine.Vector2(float.PositiveInfinity, float.PositiveInfinity); var hi = -lo;
        for (int k = 0; k < 6; k++)
        {
            int v = triangles[q * stride + stride - 6 + k]; var xy = (UnityEngine.Vector2)p[v];
            lo = UnityEngine.Vector2.Min(lo, xy); hi = UnityEngine.Vector2.Max(hi, xy); alpha[q] = UnityEngine.Mathf.Max(alpha[q], c[v].a / 255f);
            if (c[v].a > 100 && (c[v].r < 210 || c[v].g < 190)) faceColors = false;
        }
        boxes[q] = UnityEngine.Rect.MinMaxRect(lo.x, lo.y, hi.x, hi.y);
        int faceStart = q * stride + stride - 6;
        var tl = p[triangles[faceStart]]; var tr = p[triangles[faceStart + 1]];
        var br = p[triangles[faceStart + 2]]; var bl = p[triangles[faceStart + 4]];
        if (alpha[q] > .1f) skew = UnityEngine.Mathf.Max(skew, UnityEngine.Mathf.Abs(UnityEngine.Vector3.Distance(tl,tr) / UnityEngine.Mathf.Max(.001f,UnityEngine.Vector3.Distance(bl,br)) - 1));
        if (style.rewardDepth && alpha[q] > .1f) depth = UnityEngine.Mathf.Max(depth, UnityEngine.Vector3.Distance(tl, p[triangles[q * stride + 16 * 6]]));
        var glyphLo = new UnityEngine.Vector2(float.PositiveInfinity,float.PositiveInfinity); var glyphHi = -glyphLo;
        for (int k = 0; k < stride; k++) { int v = triangles[q * stride + k]; if (c[v].a <= 16) continue; var xy = (UnityEngine.Vector2)p[v]; glyphLo = UnityEngine.Vector2.Min(glyphLo,xy); glyphHi = UnityEngine.Vector2.Max(glyphHi,xy); }
        silhouettes[q] = float.IsInfinity(glyphLo.x) ? new UnityEngine.Rect() : UnityEngine.Rect.MinMaxRect(glyphLo.x,glyphLo.y,glyphHi.x,glyphHi.y);

        for (int k = 0; k < stride; k++) { int v = triangles[q * stride + k]; if (c[v].a <= 16) continue; var xy = (UnityEngine.Vector2)p[v]; allLo = UnityEngine.Vector2.Min(allLo, xy); allHi = UnityEngine.Vector2.Max(allHi, xy); }
    }
    return (boxes, alpha, float.IsInfinity(allLo.x) ? new UnityEngine.Rect() : UnityEngine.Rect.MinMaxRect(allLo.x, allLo.y, allHi.x, allHi.y), faceColors, silhouettes, skew, depth);
}
void ReadCrest()
{
    crestPoints.Clear(); var image = card.Find("Royal skin/Crest").GetComponent<UnityEngine.UI.Image>(); var sprite = image.sprite;
    var texture = sprite.texture; var rt = UnityEngine.RenderTexture.GetTemporary(texture.width, texture.height, 0, UnityEngine.RenderTextureFormat.ARGB32);
    var copy = new UnityEngine.Texture2D(texture.width, texture.height, UnityEngine.TextureFormat.RGBA32, false); var previous = UnityEngine.RenderTexture.active;
    try
    {
        UnityEngine.Graphics.Blit(texture, rt); UnityEngine.RenderTexture.active = rt; copy.ReadPixels(new UnityEngine.Rect(0, 0, texture.width, texture.height), 0, 0); copy.Apply();
        var pixels = copy.GetPixels32(); var uv = sprite.textureRect; var rect = image.rectTransform.rect;
        for (int y = 0; y < (int)uv.height; y += 2) for (int x = 0; x < (int)uv.width; x += 2)
        {
            if (pixels[((int)uv.y + y) * texture.width + (int)uv.x + x].a < 96) continue;
            var local = new UnityEngine.Vector3(rect.xMin + (x + .5f) / uv.width * rect.width, rect.yMin + (y + .5f) / uv.height * rect.height);
            crestPoints.Add(heading.transform.InverseTransformPoint(image.transform.TransformPoint(local)));
        }
    }
    finally { UnityEngine.RenderTexture.active = previous; UnityEngine.RenderTexture.ReleaseTemporary(rt); UnityEngine.Object.Destroy(copy); }
}
float CrestGap(UnityEngine.Rect[] boxes, float[] alpha)
{
    float gap = 9999; float border = 0; // Bounds already include every projected bevel, depth and shadow layer.
    for (int q = 0; q < boxes.Length; q++)
    {
        var r = boxes[q]; if (alpha[q] < .1f || r.width < .1f || r.height < .1f) continue;
        foreach (var point in crestPoints) if (point.x >= r.xMin - border && point.x <= r.xMax + border) gap = UnityEngine.Mathf.Min(gap, point.y - r.yMax - border);
    }
    return gap;
}
bool SelectSize(string wanted)
{
    var a = typeof(UnityEditor.EditorWindow).Assembly; var viewType = a.GetType("UnityEditor.GameView"); var sizesType = a.GetType("UnityEditor.GameViewSizes");
    var sizes = sizesType.GetProperty("instance", global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Static | global::System.Reflection.BindingFlags.FlattenHierarchy).GetValue(null);
    var group = sizesType.GetMethod("GetGroup", flags).Invoke(sizes, new[] { sizesType.GetProperty("currentGroupType", flags).GetValue(sizes) });
    var labels = (string[])group.GetType().GetMethod("GetDisplayTexts", flags).Invoke(group, null); int index = -1;
    for (int i = 0; i < labels.Length; i++) if (labels[i].Replace(" ", "").Contains(wanted)) { index = i; break; }
    if (index < 0) return false;
    sizeProperty = viewType.GetProperty("selectedSizeIndex", flags);
    foreach (var view in UnityEngine.Resources.FindObjectsOfTypeAll(viewType)) { if (!oldSizes.ContainsKey(view)) oldSizes.Add(view, (int)sizeProperty.GetValue(view)); sizeProperty.SetValue(view, index); }
    return oldSizes.Count > 0;
}
void Restore()
{
    UnityEngine.Time.captureDeltaTime = oldCapture; UnityEngine.Application.runInBackground = oldBackground; UnityEngine.Application.targetFrameRate = oldRate;
    if (home) home.levelNumbers = oldCatalog;
    if (sizeProperty != null) foreach (var item in oldSizes) if (item.Key) sizeProperty.SetValue(item.Key, item.Value);
    if (board) { UnityEngine.Object.Destroy(board); board = null; }
}
global::System.Collections.IEnumerator OpenCard(bool calm = false)
{
    menus.HideAllCanvasElements(); yield return null;
    RoyalThrone.Testing.RoyalSave.SetInt("RoyalThrone.HomeMotion", calm ? 0 : 1);
    JellyGarden.Scripts.InitScript.Instance.TryOpenLevel(23); yield return null; menus.menuPlay.SetActive(false);
    lm.stars = 1; menus.ShowMenuComplete(); piece = RoyalThrone.Presentation.RoyalWinCard.Of(menu); heading = piece ? piece.title.GetComponent<UnityEngine.UI.Text>() : null;
    if (!piece || !heading) throw new global::System.Exception("Win title is missing.");
}
global::System.Collections.IEnumerator Shot(string name)
{
    yield return new UnityEngine.WaitForEndOfFrame(); var tex = UnityEngine.ScreenCapture.CaptureScreenshotAsTexture();
    try { global::System.IO.File.WriteAllBytes(global::System.IO.Path.Combine(dir, name + ".png"), UnityEngine.ImageConversion.EncodeToPNG(tex)); } finally { UnityEngine.Object.Destroy(tex); }
}
global::System.Collections.IEnumerator Observe(string aspect, bool calm)
{
    string economy = Economy(); var states = new global::System.Collections.Generic.List<(float time, float wall, UnityEngine.Rect[] box, float[] alpha, float gap)>();
    float begin = UnityEngine.Time.realtimeSinceStartup, minGap = 9999; bool faces = true, bounds = true; float maxSkew = 0, maxDepth = 0; int previousVerts = -1;
    while (UnityEngine.Time.realtimeSinceStartup - begin < 15)
    {
        yield return new UnityEngine.WaitForEndOfFrame();
        if (crestPoints.Count == 0) ReadCrest();
        var data = ReadTitle(); float gap = CrestGap(data.silhouettes, data.alpha); minGap = UnityEngine.Mathf.Min(minGap, gap); faces &= data.faceColors; maxSkew = UnityEngine.Mathf.Max(maxSkew,data.skew); maxDepth = UnityEngine.Mathf.Max(maxDepth,data.depth);
        if (previousVerts < 0) previousVerts = data.boxes.Length; else if (previousVerts != data.boxes.Length) throw new global::System.Exception("Title mesh glyph count changes during reveal.");
        if (data.all.width > 0) { var r = ToScreen(data.all, heading.transform); bounds &= r.xMin >= 0 && r.yMin >= 0 && r.xMax <= UnityEngine.Screen.width && r.yMax <= UnityEngine.Screen.height; }
        states.Add((piece.Elapsed, UnityEngine.Time.realtimeSinceStartup - begin, data.boxes, data.alpha, gap));
        if (piece.Elapsed >= 1.25f) break;
    }
    if (states.Count == 0) throw new global::System.Exception("No title frames rendered.");
    var final = states[states.Count - 1]; var glyphs = new global::System.Collections.Generic.List<int>(); var logical = new global::System.Collections.Generic.List<int>();
    for (int i = 0; i < final.box.Length; i++) if (final.box[i].width > .1f && final.box[i].height > .1f) glyphs.Add(i);
    for (int i = 0; i < heading.text.Length; i++) if (!char.IsWhiteSpace(heading.text[i])) logical.Add(i);
    Check(glyphs.Count == logical.Count && glyphs.Count == 9, aspect + " rendered mesh contains all nine visible Well Done! glyphs");
    if (glyphs.Count != logical.Count) throw new global::System.Exception("Cannot map rendered title quads to its actual characters.");
    Check(faces, aspect + " each rendered glyph group ends with its ivory face, preserving per-glyph compositing");
    Check(bounds && minGap >= 0, aspect + " animated title stays on screen and its outlined glyph bounds clear opaque crest pixels (gap " + F(minGap) + ")");
    Check(final.time >= 1.2f && states.Count >= 20, aspect + " sampled the complete0–1.2s rendered title (" + states.Count + " frames)");
    Check(piece.title.rewardDepth && heading.canvasRenderer.GetMesh().vertexCount < 65000 && maxDepth > 5, aspect + " projected gold sidewall remains visibly separated from the face and within UI vertex budget (depth " + F(maxDepth) + ")");
    Check(calm ? maxSkew < .002f : maxSkew > .003f, aspect + " perspective tilt " + (calm ? "is settled in Motion off" : "changes the face projection during the pop") + " (skew " + F(maxSkew) + ")");
    float lastOnset = -1; int overlapMax = 0; bool settling = true;
    for (int g = 0; g < glyphs.Count; g++)
    {
        int q = glyphs[g], logicalIndex = logical[g]; float onset = -1, peak = 0, peakAt = -1, trough = 9, baseline = 0;
        foreach (var state in states)
        {
            float scale = state.box[q].height / final.box[q].height, drift = UnityEngine.Mathf.Abs(state.box[q].yMin - final.box[q].yMin);
            if (state.alpha[q] > .1f) { if (onset < 0) onset = state.time; if (scale > peak) { peak = scale; peakAt = state.time; } baseline = UnityEngine.Mathf.Max(baseline, drift); }
            if (state.time > .18f + logicalIndex * .045f + .30f && state.time < .18f + logicalIndex * .045f + .48f) trough = UnityEngine.Mathf.Min(trough, scale);
            if (state.time >= 1.15f) settling &= UnityEngine.Mathf.Abs(scale - 1) < .005f;
            sampleLog.Append(aspect).Append('\t').Append(F(state.wall)).Append('\t').Append(F(state.time)).Append('\t').Append(heading.text[logicalIndex]).Append('\t').Append(logicalIndex).Append('\t').Append(F(state.alpha[q])).Append('\t').Append(F(scale)).Append('\t').Append(F(drift)).Append('\t').Append(F(state.gap)).Append('\n');
        }
        if (calm) Check(peak >= .995f && peak <= 1.005f && baseline < .1f, aspect + " Motion off keeps glyph " + heading.text[logicalIndex] + logicalIndex + " at its final mesh size/baseline");
        else
        {
            float expected = .18f + logicalIndex * .045f;
            Check(onset >= expected - .012f && onset <= expected + .065f && onset >= lastOnset, aspect + " glyph " + heading.text[logicalIndex] + logicalIndex + " enters in logical sequence at " + F(onset) + "s");
            Check(peak >= 1.40f && peak <= 1.85f && peakAt >= expected + .05f && peakAt <= expected + .20f && trough >= .85f && trough <= 1.0f,
                aspect + " glyph " + heading.text[logicalIndex] + logicalIndex + " grows, undershoots and settles (" + F(peak) + "/" + F(trough) + ")");
            Check(baseline < 10f, aspect + " glyph " + heading.text[logicalIndex] + logicalIndex + " keeps the tilted pop anchored near its baseline (" + F(baseline) + ")");
            if (heading.text[logicalIndex] == 'D') Check(onset >= .385f && onset <= .46f, aspect + " D keeps the space's stagger step (onset " + F(onset) + ")");
            if (heading.text[logicalIndex] == '!') Check(onset >= .565f && onset <= .66f && peakAt > .65f && peakAt < .81f && trough < .98f, aspect + " final exclamation receives its full envelope without being cut off");
        }
        lastOnset = onset;
    }
    foreach (var state in states) { int overlapping = 0; foreach (int q in glyphs) if (state.alpha[q] > .1f && state.box[q].height / final.box[q].height > 1.1f) overlapping++; overlapMax = global::System.Math.Max(overlapMax, overlapping); }
    Check(settling && (calm || overlapMax >= 3), aspect + " title settles completely and " + (calm ? "stays static" : "overlaps several growing letters (" + overlapMax + ")"));
    Check(Economy() == economy, aspect + " title presentation does not change currency, lives, stars or score");
}
bool Shade(UnityEngine.Transform button, string layer, string top, string bottom)
{
    var shape = button.Find("Royal skin/" + layer)?.GetComponent<RoyalThrone.Presentation.RoyalCardShape>();
    return shape && shape.from == RoyalThrone.Presentation.RoyalCardKit.C(top) && shape.to == RoyalThrone.Presentation.RoyalCardKit.C(bottom);
}
string StyleSignature(UnityEngine.UI.Text text)
{
    var s = text.GetComponent<RoyalThrone.Presentation.RoyalTextStyle>();
    return s.top + "/" + s.bottom + "/" + s.rim + "/" + F(s.rimWidth) + "/" + s.outline + "/" + F(s.outlineWidth) + "/" + s.shadow + "/" + s.shadowOffset + "/" + F(s.spacing);
}
global::System.Collections.IEnumerator ButtonBoard()
{
    menus.HideAllCanvasElements(); board = new UnityEngine.GameObject("V7 owned QA button board", typeof(UnityEngine.RectTransform), typeof(UnityEngine.Canvas), typeof(UnityEngine.UI.CanvasScaler));
    var canvas = board.GetComponent<UnityEngine.Canvas>(); canvas.renderMode = UnityEngine.RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 32000;
    var scaler = board.GetComponent<UnityEngine.UI.CanvasScaler>(); scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new UnityEngine.Vector2(1080, 1920); scaler.matchWidthOrHeight = 0;
    var parent = board.transform;
    RoyalThrone.Presentation.RoyalCardKit.Shape(parent, "Background", 0, 0, 1080, 1920, 0, RoyalThrone.Presentation.RoyalCardKit.C("081c46"), RoyalThrone.Presentation.RoyalCardKit.C("04122f"));
    RoyalThrone.Presentation.RoyalCardKit.Words(parent, "Title", "Shared primary buttons", 0, 840, 1000, 42, RoyalThrone.Presentation.RoyalCardKit.Ink.Hero);
    RoyalThrone.Presentation.RoyalCardKit.Words(parent, "Columns", "Face                       CleanFace", 0, 765, 1000, 26, RoyalThrone.Presentation.RoyalCardKit.Ink.Light);
    int[] heights = { 62, 82, 116, 120 }; int[] widths = { 180, 240, 360, 435 };
    for (int i = 0; i < heights.Length; i++)
    {
        float y = 625 - i * 295; var pair = new UnityEngine.Transform[2]; float size = UnityEngine.Mathf.Round(heights[i] * .34f);
        for (int method = 0; method < 2; method++)
        {
            var go = new UnityEngine.GameObject((method == 0 ? "Face" : "CleanFace") + heights[i], typeof(UnityEngine.RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button)); go.transform.SetParent(parent, false); pair[method] = go.transform;
            if (method == 0) RoyalThrone.Presentation.RoyalCardKit.Button(go.transform, "Continue", method == 0 ? -245 : 245, y, widths[i], heights[i], RoyalThrone.Presentation.RoyalCardKit.Kind.Primary, size);
            else RoyalThrone.Presentation.RoyalCardKit.CleanButton(go.transform, "Continue", 245, y, widths[i], heights[i], RoyalThrone.Presentation.RoyalCardKit.Kind.Primary, size);
        }
        yield return new UnityEngine.WaitForEndOfFrame();
        bool same = true, positive = true, nonRaycast = true;
        var first = pair[0].Find("Royal skin"); var second = pair[1].Find("Royal skin");
        foreach (var shape in first.GetComponentsInChildren<RoyalThrone.Presentation.RoyalCardShape>())
        {
            var other = second.Find(shape.name)?.GetComponent<RoyalThrone.Presentation.RoyalCardShape>();
            same &= other && shape.from == other.from && shape.to == other.to && UnityEngine.Mathf.Abs(shape.radius - other.radius) < .001f && shape.rectTransform.rect == other.rectTransform.rect && shape.rectTransform.anchoredPosition == other.rectTransform.anchoredPosition;
            positive &= shape.rectTransform.rect.width > 0 && shape.rectTransform.rect.height > 0;
            nonRaycast &= !shape.raycastTarget;
        }
        var captionA = pair[0].Find("Royal caption").GetComponent<UnityEngine.UI.Text>(); var captionB = pair[1].Find("Royal caption").GetComponent<UnityEngine.UI.Text>();
        Check(same && positive && nonRaycast, heights[i] + "h/" + widths[i] + "w primary Face and CleanFace have identical drawable enamel/bezel geometry with passive decoration");
        Check(StyleSignature(captionA) == StyleSignature(captionB) && captionA.font == captionB.font && captionA.fontSize == captionB.fontSize, heights[i] + "h primary caption styling matches across builders");
        Check(Shade(pair[0], "Green", "18a44e", "034522") && Shade(pair[0], "Emerald bevel", "33b86b", "012f17") && Shade(pair[0], "Gold", "fff0a0", "c58923"), heights[i] + "h uses the reviewed emerald enamel and sculpted gold colors");
        Check(captionA.preferredWidth < widths[i] - 20 && !captionA.GetComponent<RoyalThrone.Presentation.RoyalTextStyle>().rewardWave, heights[i] + "h primary caption fits without acquiring the win-title wave");
    }
    var legacy = new UnityEngine.Transform[3];
    for (int i = 0; i < 3; i++)
    {
        var go = new UnityEngine.GameObject("Legacy button " + i, typeof(UnityEngine.RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button)); go.transform.SetParent(parent, false); legacy[i] = go.transform;
        if (i == 0) RoyalThrone.Presentation.RoyalCardKit.Button(go.transform, "Blue", -320, -655, 240, 82, RoyalThrone.Presentation.RoyalCardKit.Kind.Secondary);
        else RoyalThrone.Presentation.RoyalCardKit.CleanButton(go.transform, i == 1 ? "Blue" : "Red", i == 1 ? 0 : 320, -655, 240, 82, i == 1 ? RoyalThrone.Presentation.RoyalCardKit.Kind.Secondary : RoyalThrone.Presentation.RoyalCardKit.Kind.Danger);
    }
    yield return new UnityEngine.WaitForEndOfFrame();
    Check(Shade(legacy[0], "Green", "2a5cc4", "0c2466") && Shade(legacy[1], "Green", "3068d6", "1a44a6") && Shade(legacy[2], "Green", "f2493e", "b3171d"), "legacy classic-blue, clean-blue and danger-red colors remain unchanged");
    Check(!legacy[0].Find("Royal skin/Emerald bevel") && !legacy[1].Find("Royal skin/Emerald bevel") && !legacy[2].Find("Royal skin/Emerald bevel"), "secondary and danger buttons do not acquire the primary enamel layer");
    yield return Shot("shared-button-board"); UnityEngine.Object.Destroy(board); board = null; yield return null;
}
global::System.Collections.IEnumerator Route(string control)
{
    yield return OpenCard(); yield return Wait(1.3f);
    var next = card.Find("Next").GetComponent<UnityEngine.UI.Button>(); var close = card.Find("Royal close").GetComponent<UnityEngine.UI.Button>(); var target = control == "Next" ? next : close;
    int nextCalls = 0, closeCalls = 0; UnityEngine.Events.UnityAction onNext = () => nextCalls++, onClose = () => closeCalls++;
    next.onClick.AddListener(onNext); close.onClick.AddListener(onClose); string economy = Economy();
    try
    {
        var es = UnityEngine.EventSystems.EventSystem.current; var p = new UnityEngine.EventSystems.PointerEventData(es) { position = ScreenRect(target.transform).center, button = UnityEngine.EventSystems.PointerEventData.InputButton.Left };
        var hits = new global::System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>(); es.RaycastAll(p, hits);
        var hit = hits.Count == 0 ? null : UnityEngine.EventSystems.ExecuteEvents.GetEventHandler<UnityEngine.EventSystems.IPointerClickHandler>(hits[0].gameObject);
        Check(hit == target.gameObject, control + " remains the actual topmost raycast target");
        if (hit == target.gameObject) { UnityEngine.EventSystems.ExecuteEvents.Execute(hit, p, UnityEngine.EventSystems.ExecuteEvents.pointerDownHandler); yield return Wait(.05f); UnityEngine.EventSystems.ExecuteEvents.Execute(hit, p, UnityEngine.EventSystems.ExecuteEvents.pointerUpHandler); UnityEngine.EventSystems.ExecuteEvents.Execute(hit, p, UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler); }
        yield return Wait(.2f);
        Check(nextCalls == 1 && closeCalls == (control == "Next" ? 0 : 1) && !menu.gameObject.activeInHierarchy && !menus.menuPlay.activeInHierarchy && lm.gameStatus == JellyGarden.Scripts.GameState.Map && lm.currentLevel == 24, control + " routes exactly once through Continue to Home and selects Level24");
        Check(Economy() == economy, control + " does not commit or duplicate any award");
    }
    finally { next.onClick.RemoveListener(onNext); close.onClick.RemoveListener(onClose); }
}
global::System.Collections.IEnumerator Capture()
{
    yield return OpenCard(); crestPoints.Clear();
    string folder = "frames-" + global::System.DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"); var frameDir = global::System.IO.Path.Combine(dir, folder); global::System.IO.Directory.CreateDirectory(frameDir);
    var stamps = new global::System.Collections.Generic.List<float>(); var frameLog = new global::System.Text.StringBuilder("frame\twall_s\ttitle_s\theading_x\theading_y\theading_w\theading_h\tcrest_gap_local\n");
    float begin = UnityEngine.Time.realtimeSinceStartup;
    while (UnityEngine.Time.realtimeSinceStartup - begin < 15)
    {
        yield return new UnityEngine.WaitForEndOfFrame(); if (crestPoints.Count == 0) ReadCrest();
        float stamp = UnityEngine.Time.realtimeSinceStartup - begin; var data = ReadTitle(); var roi = ScreenRect(heading.transform);
        // Include upward wave and both borders in a fixed crop. Coordinates are screen pixels with bottom-left origin.
        roi.yMin -= roi.height * .15f; roi.yMax += roi.height * .65f;
        var tex = UnityEngine.ScreenCapture.CaptureScreenshotAsTexture();
        try { global::System.IO.File.WriteAllBytes(global::System.IO.Path.Combine(frameDir, stamps.Count.ToString("D5") + ".jpg"), UnityEngine.ImageConversion.EncodeToJPG(tex, 94)); } finally { UnityEngine.Object.Destroy(tex); }
        frameLog.Append(stamps.Count).Append('\t').Append(F(stamp)).Append('\t').Append(F(piece.Elapsed)).Append('\t').Append(F(roi.x)).Append('\t').Append(F(roi.y)).Append('\t').Append(F(roi.width)).Append('\t').Append(F(roi.height)).Append('\t').Append(F(CrestGap(data.silhouettes, data.alpha))).Append('\n'); stamps.Add(stamp);
        if (piece.Elapsed >= 3.05f) break;
    }
    var concat = new global::System.Text.StringBuilder("ffconcat version 1.0\n");
    for (int i = 0; i < stamps.Count; i++) concat.Append("file '").Append(folder).Append('/').Append(i.ToString("D5")).Append(".jpg'\nduration ").Append(F(i + 1 < stamps.Count ? stamps[i + 1] - stamps[i] : 1f / 30f)).Append('\n');
    if (stamps.Count > 0) concat.Append("file '").Append(folder).Append('/').Append((stamps.Count - 1).ToString("D5")).Append(".jpg'\n");
    global::System.IO.File.WriteAllText(global::System.IO.Path.Combine(dir, "capture.ffconcat"), concat.ToString()); global::System.IO.File.WriteAllText(global::System.IO.Path.Combine(dir, "capture-timestamps.tsv"), frameLog.ToString());
    Note("CAPTURE " + folder + " " + stamps.Count + " actual rendered JPEG frames; wall/title timestamps logged separately. Encoding pass is separate from motion assertions; silent capture.");
}
global::System.Collections.IEnumerator Body()
{
    qa.StartSession(); ownsQa = RoyalThrone.Testing.RoyalSave.Testing; if (!ownsQa) throw new global::System.Exception("Owned QA session did not start.");
    qa.Visible = false; qa.Counters = false; RoyalThrone.Campaign.RoyalReferenceCampaign.Select(true);
    for (int n = 1; n <= 50; n++) RoyalThrone.Testing.RoyalSave.SetInt("Level." + n.ToString("000") + ".StarsCount", n < 23 ? 1 : 0);
    yield return qa.Execute("Refill lives"); UnityEngine.Application.runInBackground = true; UnityEngine.Time.captureDeltaTime = 0; UnityEngine.Application.targetFrameRate = 60;
    menu = menus.GetCanvasGlobal().Find("MenuComplete"); card = menu ? menu.Find("Image") : null; if (!card) throw new global::System.Exception("Win card missing.");
    int children = -1;
    foreach (string size in new[] { "1080x1920", "720x1280", "1536x2048" })
    {
        menus.HideAllCanvasElements(); Check(SelectSize(size), size + " required Game view preset exists"); yield return Wait(.35f);
        yield return OpenCard(); crestPoints.Clear(); yield return Observe(size, false);
        Check(UnityEngine.Screen.width + "x" + UnityEngine.Screen.height == size, size + " uses the requested dimensions");
        int count = card.GetComponentsInChildren<UnityEngine.Transform>(true).Length; if (children < 0) children = count;
        Check(count == children, size + " reopening reuses the title and effect hierarchy without accumulating children"); yield return Shot("settled-" + size);
    }
    menus.HideAllCanvasElements(); SelectSize("1080x1920"); yield return Wait(.35f); yield return OpenCard(true); crestPoints.Clear(); yield return Observe("motion-off", true); yield return Shot("motion-off");
    Check(card.GetComponentsInChildren<UnityEngine.Transform>(true).Length == children, "Motion off reopening reuses the existing hierarchy");
    var start = menus.menuPlay.transform.Find("Image/Play"); var next = card.Find("Next");
    Check(Shade(start, "Green", "18a44e", "034522") && Shade(next, "Green", "18a44e", "034522") && Shade(start, "Gold", "fff0a0", "c58923") && Shade(next, "Gold", "fff0a0", "c58923"), "real LevelStart and Continue use the same primary emerald/gold finish");
    yield return ButtonBoard(); yield return Route("Next"); yield return Route("Royal close"); yield return Capture();
    Check(Snapshot(false) == realBefore, "normal player save keys remain untouched during presentation QA");
}
global::System.Collections.IEnumerator Run()
{
    UnityEngine.Application.logMessageReceived += onLog; var stack = new global::System.Collections.Generic.Stack<global::System.Collections.IEnumerator>(); stack.Push(Body());
    Note("Started " + global::System.DateTime.UtcNow.ToString("o") + "; v7 projected-depth title mesh/buttons presentation QA; no win/economy commit.");
    try
    {
        while (stack.Count > 0)
        {
            var step = stack.Peek(); bool moved = false; global::System.Exception error = null; try { moved = step.MoveNext(); } catch (global::System.Exception ex) { error = ex; }
            if (error != null) { Check(false, "Harness exception: " + error); break; }
            if (!moved) { stack.Pop(); (step as global::System.IDisposable)?.Dispose(); continue; }
            if (step.Current is global::System.Collections.IEnumerator child && !(step.Current is UnityEngine.CustomYieldInstruction)) stack.Push(child); else yield return step.Current;
        }
        while (stack.Count > 0) (stack.Pop() as global::System.IDisposable)?.Dispose();
        Restore(); if (ownsQa && qa && RoyalThrone.Testing.RoyalSave.Testing) yield return qa.EndSession(); ownsQa = false; cleaned = true; yield return Wait(.25f);
        bool catalog = home && global::System.Object.ReferenceEquals(home.levelNumbers, oldCatalog);
        if (catalog && oldCatalogValues != null) { catalog &= home.levelNumbers.Length == oldCatalogValues.Length; for (int i = 0; catalog && i < oldCatalogValues.Length; i++) catalog &= home.levelNumbers[i] == oldCatalogValues[i]; }
        Check(catalog && !RoyalThrone.Testing.RoyalSave.Testing, "owned QA ended and original Home catalog reference/values restored");
        Check(Snapshot(false) == realBefore && JellyGarden.Scripts.InitScript.Gems + "/" + JellyGarden.Scripts.InitScript.lifes + "/" + lm.currentLevel == runtimeBefore, "normal save keys and runtime coins/lives/level restored exactly");
        Check(errors.Count == 0, "no Unity errors or exceptions (" + errors.Count + ")"); foreach (string e in errors) Note("UNITY ERROR " + e);
        Note("SUMMARY passes=" + passes + " fails=" + fails); Note("DONE");
    }
    finally
    {
        while (stack.Count > 0) { try { (stack.Pop() as global::System.IDisposable)?.Dispose(); } catch { } }
        Restore(); UnityEngine.Application.logMessageReceived -= onLog;
        global::System.IO.File.WriteAllText(global::System.IO.Path.Combine(dir, "mesh-samples.tsv"), sampleLog.ToString());
        if (!cleaned && ownsQa && qa && RoyalThrone.Testing.RoyalSave.Testing) { Note("INTERRUPTED: scheduling owned QA restoration"); qa.StartCoroutine(qa.EndSession()); }
    }
}
qa.StartCoroutine(Run());
return "Focused title/button QA started; poll Design/Cards/LevelCompleted-v7/Verification/title-buttons-checks.txt for DONE.";
