using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalThrone.Presentation
{
    /// <summary>
    /// Plays a Lottie (Bodymovin) JSON animation on a uGUI canvas, once, then holds its last frame.
    /// Supports the subset the Royal Throne splash (Splash.json) uses: shape layers whose groups hold static Bezier paths
    /// with solid fills (nonzero or even-odd), static group transforms, and animated layer transforms (anchor, joined or
    /// split position, scale, rotation, opacity) with eased or hold keyframes. Layers are drawn in the file's order.
    /// Each layer's artwork is rasterised once into its own texture at the on-screen scale (analytic horizontal coverage,
    /// four sub-scanlines per pixel) and drawn as one transformed quad; only the transforms change per frame.
    /// Unsupported features (masks, mattes, effects, images, text, trim paths, strokes, expressions) are reported once.
    /// This RectTransform is the composition: its size is the composition size in canvas units.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Royal Throne/Lottie Player")]
    public sealed class RoyalLottiePlayer : MonoBehaviour
    {
        public TextAsset animationJson;

        public float FrameRate { get; private set; }
        public float InPoint { get; private set; }
        public float OutPoint { get; private set; }
        public Vector2 CompositionSize { get; private set; }
        /// <summary>Length of one play-through in seconds (out point - in point, at the file's frame rate).</summary>
        public float Duration => FrameRate > 0 ? (OutPoint - InPoint) / FrameRate : 0;
        public float Frame { get; private set; }
        public bool Playing { get; private set; }
        public bool Finished { get; private set; }
        public bool Loaded => layers != null;
        public int LayerCount => layers != null ? layers.Count : 0;
        public event Action Completed;

        sealed class Keyframe { public float t; public float[] s, e; public float[] ox, oy, ix, iy; public bool hold; }
        sealed class Property
        {
            public float[] constant; public List<Keyframe> keys;
            public float[] Value(float frame)
            {
                if (keys == null || keys.Count == 0) return constant;
                if (frame <= keys[0].t) return keys[0].s;
                for (int i = 0; i < keys.Count - 1; i++)
                {
                    var a = keys[i]; var b = keys[i + 1];
                    if (frame >= b.t) continue;
                    var from = a.s; var to = a.e ?? b.s ?? a.s;
                    if (a.hold || to == null || from == null) return from;
                    float u = (frame - a.t) / Mathf.Max(1e-5f, b.t - a.t);
                    var v = new float[from.Length];
                    for (int d = 0; d < from.Length; d++)
                    {
                        float eased = a.ox == null ? u : Ease(Pick(a.ox, d), Pick(a.oy, d), Pick(a.ix, d), Pick(a.iy, d), u);
                        v[d] = from[d] + ((d < to.Length ? to[d] : from[d]) - from[d]) * eased;
                    }
                    return v;
                }
                var last = keys[keys.Count - 1];
                return last.s ?? (keys.Count > 1 ? keys[keys.Count - 2].e : null) ?? constant;
            }
            static float Pick(float[] a, int d) => a == null || a.Length == 0 ? 0 : a[Mathf.Min(d, a.Length - 1)];
        }
        sealed class Path { public List<Vector2> points = new List<Vector2>(); }
        sealed class Fill { public Color color; public bool evenOdd; public List<Path> paths = new List<Path>(); }
        sealed class Layer
        {
            public string name; public float ip, op;
            public Property anchor, position, positionX, positionY, scale, rotation, opacity;
            public List<Fill> fills = new List<Fill>();
            public Rect bounds;                     // layer space (Lottie: y down)
            public RawImage image; public Texture2D texture; public float rasterScale;
        }

        List<Layer> layers;
        float elapsed; float rasterPixelsPerUnit;
        readonly HashSet<string> unsupported = new HashSet<string>();

        void Awake() { if (animationJson && layers == null) Load(); }

        /// <summary>Parses the JSON. Returns false (and logs) when the file cannot be read.</summary>
        public bool Load()
        {
            Clear();
            if (!animationJson) { Debug.LogError("Royal Throne: the splash animation JSON is not assigned.", this); return false; }
            Dictionary<string, object> root;
            try { root = MiniJson.Parse(animationJson.text) as Dictionary<string, object>; }
            catch (Exception exception) { Debug.LogError("Royal Throne: the splash animation JSON could not be read: " + exception.Message, this); return false; }
            if (root == null) { Debug.LogError("Royal Throne: the splash animation JSON is empty.", this); return false; }
            FrameRate = Num(root, "fr", 30); InPoint = Num(root, "ip", 0); OutPoint = Num(root, "op", InPoint + FrameRate);
            CompositionSize = new Vector2(Num(root, "w", 512), Num(root, "h", 512));
            layers = new List<Layer>();
            if (root.TryGetValue("assets", out var assets) && assets is List<object> assetList && assetList.Count > 0) Unsupported("assets/precomps");
            if (root.TryGetValue("layers", out var ls) && ls is List<object> list)
                foreach (var entry in list)
                {
                    var l = entry as Dictionary<string, object>; if (l == null) continue;
                    if ((int)Num(l, "ty", -1) != 4) { Unsupported("layer type " + Num(l, "ty", -1)); continue; }
                    if (l.ContainsKey("masksProperties") || l.ContainsKey("tt") || l.ContainsKey("ef")) Unsupported("masks, mattes or effects");
                    var layer = new Layer { name = Str(l, "nm"), ip = Num(l, "ip", InPoint), op = Num(l, "op", OutPoint) };
                    var ks = Obj(l, "ks");
                    layer.anchor = Prop(Obj(ks, "a"), new float[] { 0, 0, 0 });
                    var p = Obj(ks, "p");
                    if (p != null && p.TryGetValue("s", out var split) && split is bool b && b)
                    { layer.positionX = Prop(Obj(p, "x"), new float[] { 0 }); layer.positionY = Prop(Obj(p, "y"), new float[] { 0 }); }
                    else layer.position = Prop(p, new float[] { 0, 0, 0 });
                    layer.scale = Prop(Obj(ks, "s"), new float[] { 100, 100, 100 });
                    layer.rotation = Prop(Obj(ks, "r") ?? Obj(ks, "rz"), new float[] { 0 });
                    layer.opacity = Prop(Obj(ks, "o"), new float[] { 100 });
                    if (l.TryGetValue("shapes", out var shapes) && shapes is List<object> shapeList) ReadShapes(shapeList, Matrix4x4.identity, layer);
                    layer.bounds = Bounds(layer);
                    layers.Add(layer);
                }
            layers.Reverse();   // the first layer in the file is drawn on top
            Frame = InPoint; Finished = false; Playing = false;
            return true;
        }

        void ReadShapes(List<object> items, Matrix4x4 parent, Layer layer)
        {
            // A group's own transform ("tr") applies to everything in it; its fill paints the paths gathered before it.
            Matrix4x4 local = Matrix4x4.identity;
            foreach (var entry in items)
            {
                var it = entry as Dictionary<string, object>;
                if (it != null && Str(it, "ty") == "tr") local = StaticTransform(it);
            }
            var matrix = parent * local;
            var pending = new List<Path>();
            foreach (var entry in items)
            {
                var it = entry as Dictionary<string, object>; if (it == null) continue;
                if (it.TryGetValue("hd", out var hidden) && hidden is bool h && h) continue;
                switch (Str(it, "ty"))
                {
                    case "gr": if (it.TryGetValue("it", out var inner) && inner is List<object> innerList) ReadShapes(innerList, matrix, layer); break;
                    case "sh": { var path = ReadPath(Obj(it, "ks"), matrix); if (path != null) pending.Add(path); break; }
                    case "fl":
                    {
                        var c = Prop(Obj(it, "c"), new float[] { 1, 1, 1, 1 }).Value(InPoint);
                        float o = Prop(Obj(it, "o"), new float[] { 100 }).Value(InPoint)[0] / 100;
                        var fill = new Fill { color = new Color(c[0], c[1], c[2], (c.Length > 3 ? c[3] : 1) * o), evenOdd = (int)Num(it, "r", 1) == 2 };
                        fill.paths.AddRange(pending); layer.fills.Add(fill);
                        break;
                    }
                    case "tr": break;
                    default: Unsupported("shape item " + Str(it, "ty")); break;
                }
            }
        }

        Path ReadPath(Dictionary<string, object> ks, Matrix4x4 matrix)
        {
            if (ks == null) return null;
            if (ks.TryGetValue("a", out var animated) && Convert.ToDouble(animated, CultureInfo.InvariantCulture) != 0) { Unsupported("animated paths"); return null; }
            var k = Obj(ks, "k"); if (k == null) return null;
            var v = Points(k, "v"); var inT = Points(k, "i"); var outT = Points(k, "o");
            bool closed = !k.TryGetValue("c", out var c) || (c is bool cb && cb);
            if (v.Count < 2) return null;
            var path = new Path();
            int segments = closed ? v.Count : v.Count - 1;
            for (int i = 0; i < segments; i++)
            {
                int j = (i + 1) % v.Count;
                Vector2 p0 = v[i], p3 = v[j], p1 = p0 + (i < outT.Count ? outT[i] : Vector2.zero), p2 = p3 + (j < inT.Count ? inT[j] : Vector2.zero);
                int steps = Mathf.Clamp(Mathf.CeilToInt(((p1 - p0).magnitude + (p2 - p1).magnitude + (p3 - p2).magnitude) / 3f), 1, 48);
                for (int s = 0; s < steps; s++)
                {
                    float t = s / (float)steps, u = 1 - t;
                    var q = u * u * u * p0 + 3 * u * u * t * p1 + 3 * u * t * t * p2 + t * t * t * p3;
                    path.points.Add(matrix.MultiplyPoint3x4(q));
                }
            }
            return path;
        }

        static Matrix4x4 StaticTransform(Dictionary<string, object> tr)
        {
            var a = Prop(Obj(tr, "a"), new float[] { 0, 0 }).Value(0); var p = Prop(Obj(tr, "p"), new float[] { 0, 0 }).Value(0);
            var s = Prop(Obj(tr, "s"), new float[] { 100, 100 }).Value(0); float r = Prop(Obj(tr, "r"), new float[] { 0 }).Value(0)[0];
            return Matrix4x4.Translate(new Vector3(p[0], p[1])) * Matrix4x4.Rotate(Quaternion.Euler(0, 0, r)) *
                   Matrix4x4.Scale(new Vector3(s[0] / 100, s[1] / 100, 1)) * Matrix4x4.Translate(new Vector3(-a[0], -a[1]));
        }

        static Rect Bounds(Layer layer)
        {
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            foreach (var f in layer.fills) foreach (var p in f.paths) foreach (var q in p.points)
                    { x0 = Mathf.Min(x0, q.x); y0 = Mathf.Min(y0, q.y); x1 = Mathf.Max(x1, q.x); y1 = Mathf.Max(y1, q.y); }
            return x0 > x1 ? new Rect(0, 0, 0, 0) : Rect.MinMaxRect(x0 - 1, y0 - 1, x1 + 1, y1 + 1);
        }

        /// <summary>
        /// Builds (or rebuilds, when the on-screen scale changed by more than 8%) each layer's texture for the given number of
        /// screen pixels per composition unit, and shows frame 0.
        /// </summary>
        public void Prepare(float pixelsPerUnit)
        {
            if (layers == null && !Load()) return;
            pixelsPerUnit = Mathf.Clamp(pixelsPerUnit, .05f, 8f);
            bool rebuild = rasterPixelsPerUnit <= 0 || Mathf.Abs(pixelsPerUnit / rasterPixelsPerUnit - 1) > .08f;
            if (rebuild)
            {
                rasterPixelsPerUnit = pixelsPerUnit;
                // The largest scale any layer reaches in the file keeps its edges crisp at the peak of a bounce.
                foreach (var layer in layers) Rasterise(layer, pixelsPerUnit * Mathf.Max(1, PeakScale(layer.scale)));
            }
            Apply(Frame);
        }

        static float PeakScale(Property scale)
        {
            float peak = 1;
            if (scale.keys != null) foreach (var k in scale.keys) { if (k.s != null) foreach (var v in k.s) peak = Mathf.Max(peak, v / 100); if (k.e != null) foreach (var v in k.e) peak = Mathf.Max(peak, v / 100); }
            else if (scale.constant != null) foreach (var v in scale.constant) peak = Mathf.Max(peak, v / 100);
            return Mathf.Min(peak, 3);
        }

        void Rasterise(Layer layer, float ppu)
        {
            if (!layer.image)
            {
                var go = new GameObject(string.IsNullOrEmpty(layer.name) ? "Lottie layer" : layer.name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
                go.layer = gameObject.layer; go.hideFlags = HideFlags.DontSave;
                var rt = (RectTransform)go.transform; rt.SetParent(transform, false); rt.SetAsFirstSibling();
                rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
                layer.image = go.GetComponent<RawImage>(); layer.image.raycastTarget = false;
            }
            int w = Mathf.Clamp(Mathf.CeilToInt(layer.bounds.width * ppu), 1, 2048), h = Mathf.Clamp(Mathf.CeilToInt(layer.bounds.height * ppu), 1, 2048);
            if (layer.texture && (layer.texture.width != w || layer.texture.height != h)) { Destroy(layer.texture); layer.texture = null; }
            if (!layer.texture) layer.texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "Lottie " + layer.name, hideFlags = HideFlags.DontSave };
            var pixels = new Color32[w * h];
            float sx = w / layer.bounds.width, sy = h / layer.bounds.height;
            var coverage = new float[w];
            foreach (var fill in layer.fills)
            {
                for (int row = 0; row < h; row++)
                {
                    Array.Clear(coverage, 0, w);
                    for (int sub = 0; sub < 4; sub++)
                    {
                        // texture row 0 is the bottom; Lottie y grows downward
                        float y = layer.bounds.yMax - (row + (sub + .5f) / 4f) / sy;
                        Scanline(fill, y, layer.bounds.xMin, sx, w, coverage, .25f);
                    }
                    for (int x = 0; x < w; x++)
                    {
                        float a = Mathf.Clamp01(coverage[x]) * fill.color.a; if (a <= 0) continue;
                        int i = row * w + x; var dst = pixels[i]; float da = dst.a / 255f, outA = a + da * (1 - a);
                        Color c = outA > 0 ? (fill.color * a + (Color)dst * da * (1 - a)) / outA : Color.clear;
                        pixels[i] = new Color32((byte)(c.r * 255 + .5f), (byte)(c.g * 255 + .5f), (byte)(c.b * 255 + .5f), (byte)(outA * 255 + .5f));
                    }
                }
            }
            layer.texture.SetPixels32(pixels); layer.texture.Apply(false, false);
            layer.image.texture = layer.texture; layer.rasterScale = ppu;
        }

        readonly List<KeyValuePair<float, int>> crossings = new List<KeyValuePair<float, int>>();
        void Scanline(Fill fill, float y, float x0, float sx, int w, float[] coverage, float weight)
        {
            crossings.Clear();
            foreach (var path in fill.paths)
            {
                var pts = path.points; int n = pts.Count;
                for (int i = 0; i < n; i++)
                {
                    Vector2 a = pts[i], b = pts[(i + 1) % n];
                    if ((a.y <= y && b.y > y) || (b.y <= y && a.y > y))
                    {
                        float x = a.x + (y - a.y) / (b.y - a.y) * (b.x - a.x);
                        crossings.Add(new KeyValuePair<float, int>(x, b.y > a.y ? 1 : -1));
                    }
                }
            }
            crossings.Sort((p, q) => p.Key.CompareTo(q.Key));
            int winding = 0;
            for (int i = 0; i < crossings.Count - 1; i++)
            {
                winding += crossings[i].Value;
                bool inside = fill.evenOdd ? (i % 2 == 0) : winding != 0;
                if (!inside) continue;
                float a = (crossings[i].Key - x0) * sx, b = (crossings[i + 1].Key - x0) * sx;
                if (b <= 0 || a >= w) continue;
                a = Mathf.Max(a, 0); b = Mathf.Min(b, w);
                int ia = (int)a, ib = (int)b;
                if (ia == ib) { coverage[Mathf.Min(ia, w - 1)] += (b - a) * weight; continue; }
                coverage[ia] += (ia + 1 - a) * weight;
                for (int x = ia + 1; x < ib && x < w; x++) coverage[x] += weight;
                if (ib < w) coverage[ib] += (b - ib) * weight;
            }
        }

        /// <summary>For checks: stops playback and shows one frame.</summary>
        public void Seek(float frame) { Playing = false; Frame = Mathf.Clamp(frame, InPoint, OutPoint - 1); elapsed = (Frame - InPoint) / Mathf.Max(1, FrameRate); Apply(Frame); }

        /// <summary>Starts (or restarts) the play-through from the in point.</summary>
        public void Play() { elapsed = 0; Frame = InPoint; Finished = false; Playing = layers != null; Apply(Frame); }

        void Update()
        {
            if (!Playing) return;
            // A long frame (asset loading elsewhere) slows the animation instead of skipping part of it.
            elapsed += Mathf.Min(Time.unscaledDeltaTime, 1f / 20);
            float frame = InPoint + elapsed * FrameRate;
            if (frame >= OutPoint - 1)
            {
                Frame = OutPoint - 1; Apply(Frame); Playing = false; Finished = true;   // play once and hold the final frame
                Completed?.Invoke();
                return;
            }
            Frame = frame; Apply(frame);
        }

        void Apply(float frame)
        {
            if (layers == null) return;
            var rect = ((RectTransform)transform).rect;
            float unit = CompositionSize.x > 0 ? rect.width / CompositionSize.x : 1;   // canvas units per composition unit
            foreach (var layer in layers)
            {
                if (!layer.image) continue;
                bool visible = frame >= layer.ip && frame < layer.op;
                float opacity = visible ? layer.opacity.Value(frame)[0] / 100 : 0;
                if (layer.image.enabled != opacity > 0) layer.image.enabled = opacity > 0;
                if (opacity <= 0) continue;
                var a = layer.anchor.Value(frame);
                Vector2 p = layer.position != null ? new Vector2(layer.position.Value(frame)[0], layer.position.Value(frame)[1])
                                                   : new Vector2(layer.positionX.Value(frame)[0], layer.positionY.Value(frame)[0]);
                var s = layer.scale.Value(frame); float r = layer.rotation.Value(frame)[0];
                var rt = layer.image.rectTransform;
                rt.sizeDelta = layer.bounds.size * unit;
                // The layer's anchor point, inside its artwork's bounds, is the pivot the transform scales and rotates about.
                rt.pivot = new Vector2((a[0] - layer.bounds.xMin) / Mathf.Max(1e-4f, layer.bounds.width), 1 - (a[1] - layer.bounds.yMin) / Mathf.Max(1e-4f, layer.bounds.height));
                rt.anchoredPosition = new Vector2(p.x, -p.y) * unit;
                rt.localScale = new Vector3(s[0] / 100, (s.Length > 1 ? s[1] : s[0]) / 100, 1);
                rt.localRotation = Quaternion.Euler(0, 0, -r);                       // Lottie rotates clockwise, y down
                layer.image.color = new Color(1, 1, 1, opacity);
            }
        }

        void Clear()
        {
            if (layers != null)
                foreach (var layer in layers)
                {
                    if (layer.texture) Destroy(layer.texture);
                    if (layer.image) Destroy(layer.image.gameObject);
                }
            layers = null; rasterPixelsPerUnit = 0;
        }

        void OnDestroy() => Clear();

        void Unsupported(string what)
        {
            if (unsupported.Add(what)) Debug.LogWarning("Royal Throne Lottie player: " + what + " is not supported and is skipped (" + (animationJson ? animationJson.name : "?") + ").", this);
        }

        // ---------------------------------------------------------------- JSON helpers

        static Dictionary<string, object> Obj(Dictionary<string, object> d, string key) => d != null && d.TryGetValue(key, out var v) ? v as Dictionary<string, object> : null;
        static string Str(Dictionary<string, object> d, string key) => d != null && d.TryGetValue(key, out var v) ? v as string : null;
        static float Num(Dictionary<string, object> d, string key, float fallback) => d != null && d.TryGetValue(key, out var v) && v is double n ? (float)n : fallback;
        static float[] Floats(object v)
        {
            if (v is double n) return new[] { (float)n };
            if (v is List<object> list) { var a = new float[list.Count]; for (int i = 0; i < a.Length; i++) a[i] = list[i] is double x ? (float)x : 0; return a; }
            return null;
        }
        static List<Vector2> Points(Dictionary<string, object> k, string key)
        {
            var result = new List<Vector2>();
            if (k.TryGetValue(key, out var v) && v is List<object> list)
                foreach (var item in list) { var f = Floats(item); if (f != null && f.Length >= 2) result.Add(new Vector2(f[0], f[1])); }
            return result;
        }
        static Property Prop(Dictionary<string, object> d, float[] fallback)
        {
            var prop = new Property { constant = fallback };
            if (d == null || !d.TryGetValue("k", out var k)) return prop;
            bool animated = d.TryGetValue("a", out var a) && a is double ad && ad != 0;
            if (!animated) { prop.constant = Floats(k) ?? fallback; return prop; }
            prop.keys = new List<Keyframe>();
            if (k is List<object> list)
                foreach (var entry in list)
                {
                    var kf = entry as Dictionary<string, object>; if (kf == null) continue;
                    var key = new Keyframe { t = Num(kf, "t", 0), s = kf.TryGetValue("s", out var s) ? Floats(s) : null, e = kf.TryGetValue("e", out var e) ? Floats(e) : null,
                                             hold = kf.TryGetValue("h", out var h) && h is double hd && hd != 0 };
                    var o = Obj(kf, "o"); var i = Obj(kf, "i");
                    if (o != null && i != null) { key.ox = Floats(o.TryGetValue("x", out var ox) ? ox : null); key.oy = Floats(o.TryGetValue("y", out var oy) ? oy : null); key.ix = Floats(i.TryGetValue("x", out var ix) ? ix : null); key.iy = Floats(i.TryGetValue("y", out var iy) ? iy : null); }
                    prop.keys.Add(key);
                }
            if (prop.keys.Count > 0 && prop.keys[0].s != null) prop.constant = prop.keys[0].s;
            return prop;
        }

        // Cubic Bezier easing through (0,0), (x1,y1), (x2,y2), (1,1): solve x(u) = t, return y(u).
        static float Ease(float x1, float y1, float x2, float y2, float t)
        {
            float lo = 0, hi = 1, u = t;
            for (int i = 0; i < 24; i++)
            {
                u = (lo + hi) * .5f;
                float x = 3 * (1 - u) * (1 - u) * u * x1 + 3 * (1 - u) * u * u * x2 + u * u * u;
                if (x < t) lo = u; else hi = u;
            }
            return 3 * (1 - u) * (1 - u) * u * y1 + 3 * (1 - u) * u * u * y2 + u * u * u;
        }

        /// <summary>A small JSON reader (objects, arrays, numbers, strings, true/false/null) for animation files.</summary>
        static class MiniJson
        {
            public static object Parse(string text) { int i = 0; var v = Value(text, ref i); return v; }
            static void Skip(string s, ref int i) { while (i < s.Length && char.IsWhiteSpace(s[i])) i++; }
            static object Value(string s, ref int i)
            {
                Skip(s, ref i); if (i >= s.Length) throw new FormatException("unexpected end");
                char c = s[i];
                if (c == '{')
                {
                    var d = new Dictionary<string, object>(); i++;
                    while (true)
                    {
                        Skip(s, ref i); if (s[i] == '}') { i++; return d; }
                        var key = (string)Value(s, ref i); Skip(s, ref i); if (s[i] != ':') throw new FormatException("':' expected at " + i); i++;
                        d[key] = Value(s, ref i); Skip(s, ref i);
                        if (s[i] == ',') { i++; continue; } if (s[i] == '}') { i++; return d; } throw new FormatException("',' or '}' expected at " + i);
                    }
                }
                if (c == '[')
                {
                    var list = new List<object>(); i++;
                    while (true)
                    {
                        Skip(s, ref i); if (s[i] == ']') { i++; return list; }
                        list.Add(Value(s, ref i)); Skip(s, ref i);
                        if (s[i] == ',') { i++; continue; } if (s[i] == ']') { i++; return list; } throw new FormatException("',' or ']' expected at " + i);
                    }
                }
                if (c == '"')
                {
                    var sb = new System.Text.StringBuilder(); i++;
                    while (s[i] != '"')
                    {
                        if (s[i] == '\\')
                        {
                            i++; char e = s[i];
                            if (e == 'u') { sb.Append((char)Convert.ToInt32(s.Substring(i + 1, 4), 16)); i += 4; }
                            else sb.Append(e == 'n' ? '\n' : e == 't' ? '\t' : e == 'r' ? '\r' : e == 'b' ? '\b' : e == 'f' ? '\f' : e);
                        }
                        else sb.Append(s[i]);
                        i++;
                    }
                    i++; return sb.ToString();
                }
                if (s.Length - i >= 4 && string.CompareOrdinal(s, i, "true", 0, 4) == 0) { i += 4; return true; }
                if (s.Length - i >= 5 && string.CompareOrdinal(s, i, "false", 0, 5) == 0) { i += 5; return false; }
                if (s.Length - i >= 4 && string.CompareOrdinal(s, i, "null", 0, 4) == 0) { i += 4; return null; }
                int start = i; while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
                if (start == i) throw new FormatException("unexpected '" + c + "' at " + i);
                return double.Parse(s.Substring(start, i - start), NumberStyles.Float, CultureInfo.InvariantCulture);
            }
        }
    }
}
