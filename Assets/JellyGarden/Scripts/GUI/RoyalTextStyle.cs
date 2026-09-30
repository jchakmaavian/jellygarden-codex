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
            float yMin = float.MaxValue, yMax = float.MinValue;
            foreach (var v in source) { yMin = Mathf.Min(yMin, v.position.y); yMax = Mathf.Max(yMax, v.position.y); }
            float outer = rimWidth + outlineWidth;
            if (shadow.a > 0) Copies(shadow, shadowOffset, outer);
            if (outline.a > 0 && outlineWidth > 0) Copies(outline, Vector2.zero, outer);
            if (rim.a > 0 && rimWidth > 0) Copies(rim, Vector2.zero, rimWidth);
            foreach (var v in source)
            {
                var w = v;
                float t = yMax > yMin ? (v.position.y - yMin) / (yMax - yMin) : 1;
                w.color = Color.Lerp(bottom, top, t) * (Color)v.color;
                result.Add(w);
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

        void Copies(Color tint, Vector2 offset, float width)
        {
            if (width <= 0) { Copy(tint, offset); return; }
            foreach (var d in directions) Copy(tint, offset + d * width);
        }

        void Copy(Color tint, Vector2 offset)
        {
            foreach (var v in source)
            {
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
