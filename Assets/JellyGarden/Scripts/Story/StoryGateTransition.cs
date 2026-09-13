// Native motion from the approved Area 1 door/flag/lantern previews. Pieces
// are baked offline into one atlas; full room textures remain non-readable.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    public sealed class StoryGateTransition : MonoBehaviour
    {
        public const float Duration = 4.55f;
        public const float RevealAt = 3.6f;
        public string Kind { get; private set; }
        private Image _after;
        private Material _light;
        private StoryGateLayer _pieces, _effects;

        public static bool Supports(string key)
        {
            return key == "door" || key == "flags" || key == "lanterns";
        }

        public static string DataPath(int areaId)
        {
            return StoryCampaign.ResourceRoot + string.Format("/Transitions/area_{0:000}_parts", areaId);
        }

        public static StoryGateTransition Attach(RectTransform parent, Sprite before, Sprite after,
            int areaId, string kind)
        {
            if (!Supports(kind) || parent == null || before == null || after == null) return null;
            var atlas = Resources.Load<Texture2D>(DataPath(areaId));
            var source = Resources.Load<TextAsset>(DataPath(areaId) + "_layout");
            var shader = kind == "lanterns" ? Resources.Load<Shader>("Story/UI/LanternLight") : null;
            if (atlas == null || source == null || (kind == "lanterns" && (shader == null || !shader.isSupported))) return null;
            var data = JsonUtility.FromJson<GateParts>(source.text);
            if (data == null || data.parts == null || data.width != atlas.width || data.height != atlas.height) return null;
            var parts = new Dictionary<string, GatePart>();
            foreach (GatePart part in data.parts) parts[part.key] = part;
            string[] required = kind == "door"
                ? new[] { "white", "doorFrame", "doorWood", "hardware0", "hardware1", "hardware2", "hardware3", "hardware4", "hardware5" }
                : kind == "flags" ? new[] { "white", "flag0", "flag1", "pole0", "pole1", "stone0", "stone1" }
                : new[] { "white" };
            foreach (string key in required) if (!parts.ContainsKey(key)) return null;

            RectTransform root = StoryUi.Stretch(StoryUi.CreateRect("GateTransition", parent));
            var fitter = root.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectRatio = before.rect.width / before.rect.height;
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            var transition = root.gameObject.AddComponent<StoryGateTransition>();
            transition.Kind = kind;
            Image initial = StoryUi.CreateImage("BeforeWork", root, before, Color.white);
            StoryUi.Stretch(initial.rectTransform); initial.raycastTarget = false;
            transition._pieces = StoryGateLayer.Attach(root, atlas, parts, kind, false);
            transition._after = StoryUi.CreateImage("FinishedWork", root, after, Color.white);
            StoryUi.Stretch(transition._after.rectTransform); transition._after.raycastTarget = false;
            if (shader != null)
            {
                transition._light = new Material(shader) { name = "Lantern lighting (instance)" };
                transition._light.SetVector("_SpriteUV", DataUtility.GetOuterUV(after));
                transition._after.material = transition._light;
            }
            transition._effects = StoryGateLayer.Attach(root, atlas, parts, kind, true);
            transition.SetTime(0f);
            return transition;
        }

        public void SetTime(float elapsed)
        {
            // The click already happened. Keep the preview's timing from the
            // moment the real task panel begins fading away.
            float t = elapsed + .62f;
            _pieces.Seconds = _effects.Seconds = t;
            _pieces.SetVerticesDirty(); _effects.SetVerticesDirty();
            if (_light != null)
            {
                _light.SetFloat("_Seconds", t);
                _after.materialForRendering.SetFloat("_Seconds", t);
            }
            else
            {
                float alpha = Kind == "door" ? Mathf.SmoothStep(0f, 1f, (t - 3.60f) / .28f)
                    : Mathf.SmoothStep(0f, 1f, (t - 3.85f) / .30f);
                _after.color = new Color(1f, 1f, 1f, alpha);
            }
        }

        private void OnDestroy()
        {
            if (_light == null) return;
            if (_after != null) _after.material = null;
            if (Application.isPlaying) Destroy(_light); else DestroyImmediate(_light);
        }
    }

    [Serializable] internal sealed class GateParts { public int width, height; public GatePart[] parts; }
    [Serializable] internal sealed class GatePart
    {
        public string key;
        public int x, y, width, height, atlasX, atlasY;
    }

    // A single mesh for moving pieces and another for soft glints. Coordinates
    // are the approved 720 × 1280 preview, scaled to the actual painted rect.
    internal sealed class StoryGateLayer : MaskableGraphic
    {
        internal float Seconds;
        private Texture2D _atlas;
        private Dictionary<string, GatePart> _parts;
        private string _kind;
        private bool _effects;
        private Rect _painting;
        private Vector2 _whiteUv;
        private const float S = 720f / 941f;
        private static readonly float[] FittingTimes = { 2.40f, 2.52f, 2.64f, 2.76f, 2.94f, 3.06f };
        private static readonly string[] FittingKeys = { "hardware0", "hardware1", "hardware2", "hardware3", "hardware4", "hardware5" };
        private static readonly Vector2[] DoorGlints = { new Vector2(307,1083), new Vector2(630,1083), new Vector2(468,624) };
        private static readonly Color GoldLight = new Color(1f, .94f, .73f);

        public override Texture mainTexture { get { return _atlas != null ? _atlas : Texture2D.whiteTexture; } }

        internal static StoryGateLayer Attach(RectTransform parent, Texture2D atlas,
            Dictionary<string, GatePart> parts, string kind, bool effects)
        {
            RectTransform root = StoryUi.Stretch(StoryUi.CreateRect(effects ? "WorkGlints" : "MovingPieces", parent));
            var layer = root.gameObject.AddComponent<StoryGateLayer>();
            layer._atlas = atlas; layer._parts = parts; layer._kind = kind; layer._effects = effects;
            GatePart white = parts["white"];
            layer._whiteUv = new Vector2((white.atlasX + 2f) / atlas.width, 1f - (white.atlasY + 2f) / atlas.height);
            layer.raycastTarget = false;
            layer.SetMaterialDirty();
            return layer;
        }

        private static float Smooth(float p) { return Mathf.SmoothStep(0f, 1f, p); }
        private static float Out(float p) { return 1f - Mathf.Pow(1f - Mathf.Clamp01(p), 3f); }
        private static float RandomValue(int n)
        {
            double x = Math.Sin(n * 127.1 + 311.7) * 43758.5453123;
            return (float)(x - Math.Floor(x));
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_parts == null) return;
            _painting = rectTransform.rect;
            if (_kind == "door") Door(vh);
            else if (_kind == "flags") Flags(vh);
            else if (_effects) Lanterns(vh);
        }

        private void Door(VertexHelper vh)
        {
            float t = Seconds;
            if (!_effects) Draw(vh, "doorFrame", Vector2.zero, 1f, Smooth((t - 1.03f) / .55f));
            GatePart wood = _parts["doorWood"];
            const float left = 311f * S, width = 312f * S, top = 630f * S, bottom = 1106f * S;
            Rect opening = Rect.MinMaxRect(left, top, left + width, bottom);
            for (int i = 0; i < 8; i++)
            {
                float start = 1.17f + i * .115f, p = Mathf.Clamp01((t - start) / .75f);
                if (p <= 0f) continue;
                float x = left + i * width / 8f;
                if (!_effects)
                {
                    float settled = Out(p);
                    Vector2 offset = new Vector2((i < 4 ? -1 : 1) * 7f * (1f - settled),
                        Mathf.Max(0f, 67f * (1f - settled) - 4f * Mathf.Sin(Mathf.PI * p)));
                    Region(vh, wood, Rect.MinMaxRect(x, top, x + width / 8f + .4f, bottom), offset,
                        Smooth(p / .22f), Color.white, opening);
                }
                else for (int k = 0; k < 5; k++)
                    Dust(vh, new Vector2(x + width / 16f, bottom - 3f), t - (start + .52f + k * .025f), i * 17 + k);
            }
            for (int i = 0; i < FittingKeys.Length; i++)
            {
                float p = Mathf.Clamp01((t - FittingTimes[i]) / .38f);
                int side = i >= 4 ? 0 : i % 2 == 0 ? -1 : 1;
                GatePart fitting = _parts[FittingKeys[i]];
                if (!_effects)
                {
                    float scale = side == 0 ? .66f + .34f * Out(p) + .05f * Mathf.Sin(p * Mathf.PI) : 1f;
                    Draw(vh, FittingKeys[i], new Vector2(side * 15f * (1f - Out(p)), 0f), scale, Smooth(p / .4f));
                }
                else
                {
                    float a = (t - FittingTimes[i] - .25f) / .56f;
                    if (a > 0f && a < 1f) Glint(vh, new Vector2(fitting.x + fitting.width * (.5f + (side == 0 ? 1 : side) * .3f),
                        fitting.y + fitting.height * .5f - 3f), 5f, Mathf.Sin(a * Mathf.PI) * .8f);
                }
            }
            if (_effects) for (int i = 0; i < DoorGlints.Length; i++)
            {
                float a = (t - 3.70f - i * .12f) / .65f;
                if (a > 0f && a < 1f) Glint(vh, DoorGlints[i] * S, 5f, Mathf.Sin(a * Mathf.PI) * .7f);
            }
        }

        private void Flags(VertexHelper vh)
        {
            float t = Seconds;
            for (int i = 0; i < 2; i++)
            {
                string flagKey = i == 0 ? "flag0" : "flag1";
                GatePart flag = _parts[flagKey];
                float start = i == 0 ? 1.27f : 1.50f, phase = i == 0 ? 0f : 1.4f;
                float x = (i == 0 ? 42f : 727f) * S, w = (i == 0 ? 169f : 173f) * S;
                if (!_effects)
                {
                    Draw(vh, i == 0 ? "stone0" : "stone1", Vector2.zero, 1f, Smooth((t - 1f) / .28f));
                    float mount = Smooth((t - 1.05f - i * .12f) / .30f);
                    Draw(vh, i == 0 ? "pole0" : "pole1", new Vector2(0, 4f * (1f - mount)), 1f, mount);
                    float p = Mathf.Clamp01((t - start) / 1.42f);
                    float y0 = 510f * S, y1 = (510f + 217f * Out(p)) * S;
                    float motion = 1f - Smooth((t - 2.7f) / 1.15f);
                    for (float y = y0; y < y1; y += 2f)
                    {
                        float frac = (y - y0) / (217f * S);
                        float wave = Mathf.Sin(frac * 7.5f - t * 5.6f + phase) * 4.1f * frac * motion;
                        float fold = p < .91f && y > y1 - 8f
                            ? 1f - .32f * Mathf.Sin((y - y1 + 8f) / 8f * Mathf.PI) * (1f - Smooth((p - .77f) / .14f)) : 1f;
                        Region(vh, flag, new Rect(x - 1f, y, w + 2f, Mathf.Min(2.1f, y1 - y)), new Vector2(wave, 0),
                            1f, new Color(fold, fold, fold, 1f));
                    }
                }
                else for (int k = 0; k < 4; k++)
                {
                    float a = (t - (2.8f + i * .22f + k * .07f)) / .7f;
                    if (a <= 0f || a >= 1f) continue;
                    float gx = k % 2 == 0 ? x + (k < 2 ? 13 : 30) * S : x + w - (k < 2 ? 13 : 33) * S;
                    Glint(vh, new Vector2(gx, (k < 2 ? 515 : 721) * S), 4.5f, Mathf.Sin(a * Mathf.PI) * .7f);
                }
            }
        }

        private void Lanterns(VertexHelper vh)
        {
            float t = Seconds;
            for (int i = 0; i < 2; i++)
            {
                int lx = i == 0 ? 178 : 761;
                float age = t - (i == 0 ? 1.18f : 1.72f);
                Vector2 point = new Vector2(lx, 787) * S;
                if (age > -.23f && age < .12f)
                {
                    float p = Mathf.Clamp01((age + .23f) / .35f);
                    Glint(vh, point + new Vector2(Mathf.Sin(p * Mathf.PI) * 11f, 35f * (1f - p)),
                        3f + 4f * Mathf.Sin(p * Mathf.PI), Mathf.Sin(p * Mathf.PI) * .9f);
                }
                float flare = age > 0f && age < 1.45f ? Mathf.Sin(age / 1.45f * Mathf.PI) : 0f;
                Glow(vh, point, 76f * S, new Color(1f, .75f, .22f, .22f * flare));
                Glow(vh, point, 18f * S, new Color(1f, .90f, .53f, .42f * flare));
                for (int k = 0; k < 12; k++)
                {
                    float a = age - .1f - k * .055f;
                    if (a < 0f || a > 1.1f) continue;
                    Vector2 offset = new Vector2((RandomValue(k + lx) - .5f) * 23f + Mathf.Sin(a * 3f + k) * 4f,
                        -a * (28f + RandomValue(k) * 22f));
                    Glint(vh, point + offset, 1.3f + RandomValue(k + 4) * 1.2f, Mathf.Sin(a / 1.1f * Mathf.PI) * .6f);
                }
            }
            float sun = (t - 3.13f) / 1.1f;
            if (sun > 0f && sun < 1f)
            {
                Glint(vh, new Vector2(510,445) * S, 11f, Mathf.Sin(sun * Mathf.PI));
                Glow(vh, new Vector2(475,473) * S, 42f, new Color(1f,.75f,.22f,.1f*Mathf.Sin(sun*Mathf.PI)));
            }
        }

        private void Draw(VertexHelper vh, string key, Vector2 offset, float scale, float alpha)
        {
            if (alpha <= 0f) return;
            GatePart p = _parts[key];
            Rect source = new Rect(p.x, p.y, p.width, p.height);
            Vector2 center = source.center;
            Rect destination = new Rect(center - source.size * scale * .5f + offset, source.size * scale);
            Quad(vh, p, source, destination, new Color(1,1,1,alpha));
        }

        private void Region(VertexHelper vh, GatePart part, Rect source, Vector2 offset,
            float alpha, Color tint, Rect? clip = null)
        {
            if (alpha <= 0f) return;
            float x0 = Mathf.Max(source.xMin, part.x), y0 = Mathf.Max(source.yMin, part.y);
            float x1 = Mathf.Min(source.xMax, part.x + part.width), y1 = Mathf.Min(source.yMax, part.y + part.height);
            if (clip.HasValue)
            {
                Rect r = clip.Value;
                x0 = Mathf.Max(x0, r.xMin - offset.x); x1 = Mathf.Min(x1, r.xMax - offset.x);
                y0 = Mathf.Max(y0, r.yMin - offset.y); y1 = Mathf.Min(y1, r.yMax - offset.y);
            }
            if (x1 <= x0 || y1 <= y0) return;
            source = Rect.MinMaxRect(x0, y0, x1, y1);
            tint.a *= alpha;
            Quad(vh, part, source, new Rect(source.position + offset, source.size), tint);
        }

        private void Quad(VertexHelper vh, GatePart part, Rect src, Rect dst, Color tint)
        {
            float u0 = (part.atlasX + src.xMin - part.x) / _atlas.width;
            float u1 = (part.atlasX + src.xMax - part.x) / _atlas.width;
            float v0 = 1f - (part.atlasY + src.yMin - part.y) / _atlas.height;
            float v1 = 1f - (part.atlasY + src.yMax - part.y) / _atlas.height;
            int n = vh.currentVertCount;
            Vertex(vh, new Vector2(dst.xMin,dst.yMin), tint, new Vector2(u0,v0));
            Vertex(vh, new Vector2(dst.xMax,dst.yMin), tint, new Vector2(u1,v0));
            Vertex(vh, new Vector2(dst.xMax,dst.yMax), tint, new Vector2(u1,v1));
            Vertex(vh, new Vector2(dst.xMin,dst.yMax), tint, new Vector2(u0,v1));
            vh.AddTriangle(n,n+1,n+2); vh.AddTriangle(n,n+2,n+3);
        }

        private void Dust(VertexHelper vh, Vector2 point, float age, int seed)
        {
            if (age < 0f || age > 1f) return;
            point += new Vector2((RandomValue(seed)*2f-1f)*(15f+age*35f),-age*(18f+RandomValue(seed+1)*24f));
            float r = 1f + RandomValue(seed+2)*1.9f;
            Color tint = new Color(.92f,.77f,.55f,Mathf.Sin(age*Mathf.PI)*.55f);
            int n=vh.currentVertCount;
            Vertex(vh,point+new Vector2(-r,0),tint,_whiteUv);Vertex(vh,point+new Vector2(0,-r*.45f),tint,_whiteUv);
            Vertex(vh,point+new Vector2(r,0),tint,_whiteUv);Vertex(vh,point+new Vector2(0,r*.45f),tint,_whiteUv);
            vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
        }

        private void Glint(VertexHelper vh, Vector2 point, float radius, float alpha)
        {
            Glow(vh,point,radius*3f,new Color(GoldLight.r,GoldLight.g,GoldLight.b,alpha*.45f));
            int first=vh.currentVertCount;Color tint=new Color(1f,.96f,.79f,alpha);
            Vertex(vh,point,tint,_whiteUv);
            for(int i=0;i<=8;i++)
            {
                float a=i*Mathf.PI/4f,r=(i%2==0?1f:.16f)*radius;
                Vertex(vh,point+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r,tint,_whiteUv);
                if(i>0)vh.AddTriangle(first,first+i,first+i+1);
            }
        }

        private void Glow(VertexHelper vh, Vector2 point, float radius, Color tint)
        {
            if(tint.a<=0f)return;
            int first=vh.currentVertCount;Vertex(vh,point,tint,_whiteUv);tint.a=0;
            for(int i=0;i<=20;i++)
            {
                float a=i*Mathf.PI/10f;
                Vertex(vh,point+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,tint,_whiteUv);
                if(i>0)vh.AddTriangle(first,first+i,first+i+1);
            }
        }

        private void Vertex(VertexHelper vh, Vector2 point, Color tint, Vector2 uv)
        {
            vh.AddVert(new Vector3(_painting.xMin+point.x/720f*_painting.width,
                _painting.yMax-point.y/1280f*_painting.height,0),tint,uv);
        }
    }
}
