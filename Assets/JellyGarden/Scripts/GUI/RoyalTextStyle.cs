using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalThrone.Presentation
{
    // Lettering for the Level Start card: a top-to-bottom fill gradient, a coloured rim round the letters, a dark outline
    // outside the rim and a drop shadow of the whole outlined word, built in one pass. Unity's Outline draws four copies;
    // eight keep thick outlines round on the large level number.
    [AddComponentMenu("Royal Throne/Text Style")]
    [RequireComponent(typeof(Text))]
    public sealed class RoyalTextStyle : BaseMeshEffect
    {
        public Color top = Color.white, bottom = Color.white;
        public Color rim = Color.clear;
        [Min(0)] public float rimWidth;
        public Color outline = Color.clear;
        [Min(0)] public float outlineWidth;
        public Color shadow = Color.clear;
        public Vector2 shadowOffset = new Vector2(0, -2);
        // Extra space after each letter (legacy Text has no tracking), kept centred, left or right as the Text aligns.
        public float spacing;
        // Letters reveal as `written` passes their index. Infinity (the default) shows every glyph as it is.
        // Ordinary reveals pop about the centre; the reward wave uses the longer baseline-anchored envelope below.
        public float written = float.PositiveInfinity;
        // Win-title opt-in: each glyph grows upward from its baseline while later letters start their own wave.
        // Span is the number of stagger steps occupied by one complete grow/undershoot/settle envelope.
        public bool rewardWave;
        [Min(1)] public float revealSpan = 1;
        public void Write(float letters) { if (written == letters) return; written = letters; if (graphic) graphic.SetVerticesDirty(); }
        // A band of light crossing the letters (the reward card's title): its centre across the text, 0 at the left edge and 1
        // at the right. Negative (the default) is off.
        public float shine = -1;
        public void Shine(float at) { if (shine == at) return; shine = at; if (graphic) graphic.SetVerticesDirty(); }

        static readonly List<UIVertex> source = new List<UIVertex>(), result = new List<UIVertex>();
        static readonly Vector2[] directions = BuildDirections();

        static Vector2[] BuildDirections()
        {
            var d = new Vector2[8];
            for (int i = 0; i < 8; i++) d[i] = new Vector2(Mathf.Cos(i * Mathf.PI / 4), Mathf.Sin(i * Mathf.PI / 4));
            return d;
        }

        public void Set(Color top, Color bottom, Color rim, float rimWidth, Color outline, float outlineWidth, Color shadow, Vector2 shadowOffset)
        {
            this.top = top; this.bottom = bottom; this.rim = rim; this.rimWidth = rimWidth; this.outline = outline;
            this.outlineWidth = outlineWidth; this.shadow = shadow; this.shadowOffset = shadowOffset;
            if (graphic) graphic.SetVerticesDirty();
        }

        public void Set(Color top, Color bottom, Color rim, float rimWidth, Color outline, float outlineWidth, Color shadow, Vector2 shadowOffset, float spacing)
        {
            this.spacing = spacing;
            Set(top, bottom, rim, rimWidth, outline, outlineWidth, shadow, shadowOffset);
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive()) return;
            source.Clear(); result.Clear();
            vh.GetUIVertexStream(source);
            if (source.Count == 0) return;
            if (spacing != 0) Track();
            float yMin = float.MaxValue, yMax = float.MinValue, xMin = float.MaxValue, xMax = float.MinValue;
            foreach (var v in source) { yMin = Mathf.Min(yMin, v.position.y); yMax = Mathf.Max(yMax, v.position.y); xMin = Mathf.Min(xMin, v.position.x); xMax = Mathf.Max(xMax, v.position.x); }
            if (!float.IsPositiveInfinity(written)) Reveal();
            float outer = rimWidth + outlineWidth;
            // Enlarged letters overlap in the reference. Composite each whole outlined glyph in order so their
            // pale faces cannot erase neighbouring letters' borders. Other labels retain their original batching.
            int batch = rewardWave ? 6 : source.Count;
            for (int first = 0; first < source.Count; first += batch)
            {
                int count = Mathf.Min(batch, source.Count - first);
                if (shadow.a > 0) Copies(shadow, shadowOffset, outer, first, count);
                if (outline.a > 0 && outlineWidth > 0) Copies(outline, Vector2.zero, outer, first, count);
                if (rim.a > 0 && rimWidth > 0) Copies(rim, Vector2.zero, rimWidth, first, count);
                for (int i = first; i < first + count; i++)
                {
                    var v = source[i]; var w = v;
                    float t = yMax > yMin ? (v.position.y - yMin) / (yMax - yMin) : 1;
                    w.color = Color.Lerp(bottom, top, t) * (Color)v.color;
                    if (shine >= 0 && xMax > xMin)
                    {
                        float d = ((v.position.x - xMin) / (xMax - xMin) - shine) / .13f;
                        Color c = w.color; w.color = Color.Lerp(c, new Color(1, 1, 1, c.a), Mathf.Exp(-d * d) * .75f);
                    }
                    result.Add(w);
                }
            }
            vh.Clear();
            vh.AddUIVertexTriangleStream(result);
        }

        // Letter spacing: each glyph quad (six vertices) moves right by `spacing` per glyph before it on its line; a line
        // starts where a glyph sits left of the one before. Each line then shifts back by its alignment's share.
        void Track()
        {
            int quads = source.Count / 6;
            if (quads < 2) return;
            var align = graphic is Text text ? text.alignment : TextAnchor.MiddleCenter;
            float share = align == TextAnchor.UpperLeft || align == TextAnchor.MiddleLeft || align == TextAnchor.LowerLeft ? 0
                : align == TextAnchor.UpperRight || align == TextAnchor.MiddleRight || align == TextAnchor.LowerRight ? 1 : .5f;
            int start = 0; float previous = float.MinValue;
            for (int q = 0; q <= quads; q++)
            {
                float left = q < quads ? source[q * 6].position.x : float.MinValue;
                if (q < quads) for (int k = 1; k < 6; k++) left = Mathf.Min(left, source[q * 6 + k].position.x);
                if (q == quads || left < previous - 1)
                {
                    float back = (q - start - 1) * spacing * share;
                    for (int i = start; i < q; i++)
                    {
                        float dx = (i - start) * spacing - back;
                        for (int k = 0; k < 6; k++) { var v = source[i * 6 + k]; v.position.x += dx; source[i * 6 + k] = v; }
                    }
                    start = q;
                }
                previous = left;
            }
        }

        // The written-in reveal on the glyph quads (six vertices each), before the shadow, outline and rim copy them.
        void Reveal()
        {
            int quads = source.Count / 6;
            // Legacy Text omits whitespace quads. Keep each space's beat in the plain-text reward heading.
            string characters = rewardWave && graphic is Text label ? label.text : null;
            int character = 0;
            for (int q = 0; q < quads; q++)
            {
                int letter = q;
                if (characters != null)
                {
                    while (character < characters.Length && char.IsWhiteSpace(characters[character])) character++;
                    letter = character++;
                }
                float p = Mathf.Clamp01((written - letter) / (rewardWave ? Mathf.Max(1, revealSpan) : 1));
                if (p >= 1) continue;
                Vector3 c = Vector3.zero;
                for (int k = 0; k < 6; k++) c += source[q * 6 + k].position;
                c /= 6;
                float u = 1 - p / .6f;
                float s = p < .6f ? Mathf.Lerp(1.7f, .94f, 1 - u * u * u) : Mathf.Lerp(.94f, 1, (p - .6f) / .4f);
                float a = Mathf.Clamp01(p * 2.5f);
                if (rewardWave)
                {
                    // Measured reference W: rest-size arrival, 1.72x at +.10 s, .88x at +.30 s, rest at +.40 s.
                    // Its bottom barely moves: expanding about the baseline makes the visible travelling wave.
                    for (int k = 0; k < 6; k++) c.y = Mathf.Min(c.y, source[q * 6 + k].position.y);
                    if (p < .25f) { u = p / .25f; s = Mathf.Lerp(1, 1.72f, 1 - (1 - u) * (1 - u)); }
                    else if (p < .75f) { u = (p - .25f) / .5f; s = Mathf.Lerp(1.72f, .88f, u * u); }
                    else { u = (p - .75f) / .25f; s = Mathf.Lerp(.88f, 1, u * u * (3 - 2 * u)); }
                    a = Mathf.Clamp01((written - letter) * 5);
                }
                for (int k = 0; k < 6; k++)
                {
                    var v = source[q * 6 + k];
                    v.position = c + (v.position - c) * s;
                    var col = v.color; col.a = (byte)Mathf.RoundToInt(col.a * a); v.color = col;
                    source[q * 6 + k] = v;
                }
            }
        }

        void Copies(Color tint, Vector2 offset, float width, int first, int count)
        {
            if (width <= 0) { Copy(tint, offset, first, count); return; }
            foreach (var d in directions) Copy(tint, offset + d * width, first, count);
        }

        void Copy(Color tint, Vector2 offset, int first, int count)
        {
            for (int i = first; i < first + count; i++)
            {
                var v = source[i];
                var w = v;
                w.position += (Vector3)offset;
                var c = tint;
                c.a *= ((Color)v.color).a;
                w.color = c;
                result.Add(w);
            }
        }
    }
}
