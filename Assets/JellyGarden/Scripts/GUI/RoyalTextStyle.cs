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
        // The win heading alone uses projected gold depth, directional bevels and a travelling 3D pop.
        public bool rewardDepth;
        public const int RewardDepthVerticesPerGlyph = 678;
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
            if (rewardDepth)
            {
                RewardMesh();
                vh.Clear(); vh.AddUIVertexTriangleStream(result);
                return;
            }
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

        // UI-friendly extrusion: eight solid silhouette slices behind each bevelled face, projected together.
        // All layers use the font atlas and one UI material; other labels keep their original mesh path.
        void RewardMesh()
        {
            int quads = source.Count / 6, character = 0;
            string text = graphic is Text label ? label.text : "";
            for (int q = 0; q < quads; q++)
            {
                while (character < text.Length && char.IsWhiteSpace(text[character])) character++;
                int logical = character++;
                bool finale = logical < text.Length && text[logical] == '!';
                float peak = finale ? 1.82f : 1.72f;
                float p = float.IsPositiveInfinity(written) ? 1 : Mathf.Clamp01((written - logical) / Mathf.Max(1, revealSpan));
                // Reveal the whole dimensional glyph together; fading stacked side layers produces a brown flash.
                float alpha = float.IsPositiveInfinity(written) || written > logical ? 1 : 0;
                float scale, pitch, yaw;
                if (p < .25f)
                {
                    float u = p / .25f, e = 1 - (1 - u) * (1 - u);
                    scale = Mathf.Lerp(1, peak, e); pitch = Mathf.Lerp(-10, 3, e); yaw = Mathf.Lerp(7, -2, e);
                }
                else if (p < .75f)
                {
                    float u = (p - .25f) / .5f, e = u * u;
                    scale = Mathf.Lerp(peak, .88f, e); pitch = Mathf.Lerp(3, -1, e); yaw = Mathf.Lerp(-2, 1, e);
                }
                else
                {
                    float u = (p - .75f) / .25f, e = u * u * (3 - 2 * u);
                    scale = Mathf.Lerp(.88f, 1, e); pitch = Mathf.Lerp(-1, 0, e); yaw = Mathf.Lerp(1, 0, e);
                }
                if (finale) scale *= 1.10f;
                float fan = Mathf.Lerp(-5, 5, q / Mathf.Max(1f, quads - 1));
                if (finale) fan = -8;
                float direction = q < quads / 2 ? -1 : 1;
                var rotation = Quaternion.Euler(pitch, yaw * direction, fan + direction * 3 * Mathf.Sin(p * Mathf.PI));
                int first = q * 6;
                float lo = float.MaxValue, hi = float.MinValue, left = float.MaxValue, right = float.MinValue;
                for (int k = 0; k < 6; k++) { var v = source[first + k].position; lo = Mathf.Min(lo, v.y); hi = Mathf.Max(hi, v.y); left = Mathf.Min(left, v.x); right = Mathf.Max(right, v.x); }
                var pivot = new Vector3((left + right) * .5f, lo);
                int firstOutput = result.Count;
                // Cast shadow, far bronze sidewall, bright top edge, warm inner bevel, navy keyline, ivory face bevel.
                RewardRing(first, pivot, rotation, scale, alpha, lo, hi, -5, new Vector2(0, -1.4f), 5.8f,
                    new Color(.015f, .035f, .085f, .48f), new Color(.015f, .035f, .085f, .48f));
                for (int layer = 8; layer >= 1; layer--)
                {
                    float depth = layer / 8f;
                    Color sideTop = Color.Lerp(new Color(.87f, .49f, .08f), new Color(.44f, .20f, .025f), depth);
                    Color sideBottom = Color.Lerp(new Color(.64f, .28f, .015f), new Color(.23f, .075f, .01f), depth);
                    RewardRing(first, pivot, rotation, scale, alpha, lo, hi, -3.6f * depth, Vector2.zero, 5.8f, sideTop, sideBottom);
                    RewardCopy(first, pivot, rotation, scale, alpha, lo, hi, -3.6f * depth, Vector2.zero, sideTop, sideBottom);
                }
                RewardRing(first, pivot, rotation, scale, alpha, lo, hi, 0, Vector2.zero, 5.8f, new Color(1, .97f, .69f), new Color(.87f, .52f, .06f));
                RewardRing(first, pivot, rotation, scale, alpha, lo, hi, .35f, Vector2.zero, 4.8f, new Color(1, .82f, .32f), new Color(1, .73f, .20f));
                RewardRing(first, pivot, rotation, scale, alpha, lo, hi, .65f, Vector2.zero, 2.8f, new Color(.018f, .14f, .22f), new Color(.012f, .075f, .15f));
                RewardRing(first, pivot, rotation, scale, alpha, lo, hi, .9f, Vector2.zero, .45f, new Color(1, 1, .97f), new Color(.96f, .88f, .61f));
                RewardCopy(first, pivot, rotation, scale, alpha, lo, hi, 1.1f, Vector2.zero, new Color(1, .995f, .94f), new Color(1, .95f, .78f));
                // The bigger punctuation follows a shallow dip so its peak keeps breathing room under the crown.
                if (finale)
                {
                    float dip = 6 * Mathf.Sin(p * Mathf.PI);
                    for (int i = firstOutput; i < result.Count; i++) { var v = result[i]; v.position.y -= dip; result[i] = v; }
                }
            }
        }

        void RewardRing(int first, Vector3 pivot, Quaternion rotation, float scale, float alpha, float lo, float hi,
            float z, Vector2 offset, float radius, Color light, Color dark)
        {
            foreach (var d in directions)
            {
                Color tint = Color.Lerp(dark, light, Mathf.Clamp01(.5f + d.y * .4f - d.x * .18f));
                RewardCopy(first, pivot, rotation, scale, alpha, lo, hi, z, offset + d * radius, tint, tint);
            }
        }

        void RewardCopy(int first, Vector3 pivot, Quaternion rotation, float scale, float alpha, float lo, float hi,
            float z, Vector2 offset, Color light, Color dark)
        {
            for (int k = 0; k < 6; k++)
            {
                var v = source[first + k];
                float gradient = hi > lo ? Mathf.Clamp01((v.position.y - lo) / (hi - lo)) : 1;
                var point = v.position - pivot + new Vector3(offset.x - z * .32f, offset.y + z * .85f, z);
                // A slightly taller, less wide face reads as playful display lettering instead of a metal plaque.
                point.x *= .96f; point.y *= 1.08f;
                point = rotation * (point * scale);
                float perspective = 640 / (640 - point.z);
                v.position = pivot + new Vector3(point.x * perspective, point.y * perspective, 0);
                Color color = Color.Lerp(dark, light, gradient); color.a *= alpha * ((Color)v.color).a;
                v.color = color; result.Add(v);
            }
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
