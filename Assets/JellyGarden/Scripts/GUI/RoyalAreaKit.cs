using System;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalThrone.Presentation
{
    /// <summary>
    /// The area dialogs' pieces, drawn (no image assets) to the reference's proportions and colours. Every size is in design
    /// units: the reference's pixels on a 720-wide phone (Design/RoyalMatchReference/Areas/GAPS.md), which are also the Home
    /// canvas's units at 720 wide. Positions are given y down from the top of their parent, as they were measured.
    /// </summary>
    public static class RoyalAreaKit
    {
        public static Font Display;
        public static Sprite Star, Chest;
        public static readonly Color Cream = C(251, 248, 209), Straw = C(242, 233, 168), Navy = C(7, 55, 92), Ink = C(42, 68, 116),
            Gold = C(251, 205, 0), Orange = C(241, 135, 0), Deep = C(0, 60, 124);

        public static Color C(int r, int g, int b, int a = 255) => new Color(r / 255f, g / 255f, b / 255f, a / 255f);

        public static void Init(Font display)
        {
            if (!Display) Display = display;
            if (!Star) Star = Resources.Load<Sprite>("ReferenceGameplay/Home/16674");
            if (!Chest) Chest = Resources.Load<Sprite>("ReferenceGameplay/Home/16593");
        }

        // ---------------------------------------------------------------- placement

        public static RectTransform Node(string name, Transform parent)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            r.gameObject.layer = 5; r.SetParent(parent, false); Fill(r); return r;
        }
        public static void Fill(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }
        /// <summary>A child placed by its top-left corner and size, y down from the parent's top, x from its left.</summary>
        public static RectTransform At(RectTransform r, float x, float y, float w, float h)
        {
            r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(w, h); return r;
        }
        /// <summary>A child centred on (x, y) of its parent's top-left, with the given size.</summary>
        public static RectTransform Centre(RectTransform r, float x, float y, float w, float h)
        {
            r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(.5f, .5f);
            r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(w, h); return r;
        }
        public static RectTransform Inset(RectTransform r, float left, float top, float right, float bottom)
        { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = new Vector2(left, bottom); r.offsetMax = new Vector2(-right, -top); return r; }

        // ---------------------------------------------------------------- surfaces

        public static RoyalPanelGraphic Surface(string name, Transform parent, Color top, Color bottom, float radius)
        {
            var g = Node(name, parent).gameObject.AddComponent<RoyalPanelGraphic>();
            g.topColor = top; g.bottomColor = bottom; g.cornerRadius = radius; g.raycastTarget = false; return g;
        }
        public static RoyalPanelGraphic Layer(string name, Transform parent, float inset, Color top, Color bottom, float radius)
        { var g = Surface(name, parent, top, bottom, radius); Inset(g.rectTransform, inset, inset, inset, inset); return g; }

        /// <summary>
        /// The tall blue dialog: a soft shadow under it, then from the outside in a blue edge, a dark line, orange, a straw
        /// line, gold, a dark line and a pale line around the blue body (45 units in all, as measured). Returns the body.
        /// </summary>
        public static RectTransform BluePanel(Transform parent)
        {
            var root = Node("Area panel frame", parent);
            var shadow = Surface("Shadow", root, C(10, 25, 45, 150), C(10, 25, 45, 150), 44);
            Inset(shadow.rectTransform, 0, 16, 0, -22);
            var edge = Surface("Blue edge", root, C(28, 113, 187), C(24, 95, 167), 44);
            var dark = Layer("Dark line", edge.transform, 11, C(6, 62, 122), C(6, 62, 122), 36);
            var orange = Layer("Orange", dark.transform, 5, C(241, 138, 7), C(233, 124, 0), 32);
            var straw = Layer("Straw", orange.transform, 9, C(255, 215, 99), C(255, 215, 99), 26);
            var gold = Layer("Gold", straw.transform, 3, C(252, 206, 0), C(250, 190, 0), 24);
            var line = Layer("Inner dark line", gold.transform, 8, C(0, 60, 124), C(0, 60, 124), 18);
            var pale = Layer("Pale line", line.transform, 5, C(31, 115, 190), C(29, 108, 182), 14);
            var body = Layer("Body", pale.transform, 3, C(25, 92, 167), C(21, 85, 160), 12);
            return body.rectTransform;
        }

        /// <summary>The arched blue ribbon with gold and orange trims (156 units at its middle) and its title.</summary>
        public static Text Ribbon(RectTransform parent, float width, float y, string title, float titleSize)
        {
            var root = Node("Ribbon", parent);
            root.anchorMin = root.anchorMax = new Vector2(.5f, 1); root.pivot = new Vector2(.5f, 1);
            root.anchoredPosition = new Vector2(0, -y); root.sizeDelta = new Vector2(width, 156);
            // Bands from the top: blue edge 11, gold 10, orange 8, dark 4, body 101, gold 10, orange 9, dark 3.
            float[] h = { 11, 10, 8, 4, 101, 10, 9, 3 };
            Color[] top = { C(40, 120, 183), C(252, 214, 60), C(240, 136, 0), C(0, 58, 123), C(25, 88, 168), C(252, 212, 40), C(240, 134, 0), C(9, 62, 119) };
            Color[] bottom = { C(25, 84, 167), C(249, 203, 0), C(236, 128, 0), C(0, 58, 123), C(20, 78, 155), C(249, 204, 0), C(232, 124, 0), C(9, 62, 119) };
            // A soft shadow cast on the panel under the ribbon.
            Band(root, "Ribbon shadow", 156, 18, C(0, 25, 60, 110), C(0, 25, 60, 0));
            float at = 0;
            for (int i = 0; i < h.Length; i++) { Band(root, "Ribbon band " + i, at, h[i], top[i], bottom[i]); at += h[i]; }
            // The title's letters sit in the body band, 33 to 134 units from the top at the middle.
            var label = Words("Title", root, title, titleSize, Cream, TextAnchor.MiddleCenter);
            var lr = label.rectTransform; lr.anchorMin = lr.anchorMax = new Vector2(.5f, 1); lr.pivot = new Vector2(.5f, .5f);
            lr.sizeDelta = new Vector2(width * .74f, 84); lr.anchoredPosition = new Vector2(0, -84);
            Title(label);
            return label;
        }
        static void Band(RectTransform root, string name, float y, float h, Color top, Color bottom)
        {
            var g = new GameObject(name, typeof(RectTransform)).AddComponent<RoyalArchGraphic>();
            g.gameObject.layer = 5; g.transform.SetParent(root, false); g.raycastTarget = false; g.topColor = top; g.bottomColor = bottom;
            var r = g.rectTransform; r.anchorMin = new Vector2(0, 1); r.anchorMax = new Vector2(1, 1); r.pivot = new Vector2(.5f, 1);
            r.anchoredPosition = new Vector2(0, -y); r.sizeDelta = new Vector2(0, h);
        }

        /// <summary>The round red close control: a dark red rim, a glossy red face and a cream cross with a dark red edge.</summary>
        public static Button RedClose(RectTransform parent, float x, float y, float size)
        {
            var r = Centre(Node("Close", parent), x, y, size, size);
            var rim = Surface("Rim", r, C(150, 12, 20), C(110, 5, 12), 1000);
            rim.raycastTarget = true;
            var face = Layer("Face", rim.transform, size * .06f, C(250, 70, 72), C(200, 18, 30), 1000);
            var shine = Surface("Shine", face.transform, new Color(1, 1, 1, .45f), new Color(1, 1, 1, 0), 1000);
            Inset(shine.rectTransform, size * .16f, size * .06f, size * .16f, size * .48f);
            foreach (var angle in new[] { 45f, -45f })
            {
                var edge = Surface("Cross edge", face.transform, C(150, 20, 25), C(150, 20, 25), 1000);
                Centre(edge.rectTransform, 0, 0, size * .56f, size * .22f); edge.rectTransform.anchorMin = edge.rectTransform.anchorMax = new Vector2(.5f, .5f);
                edge.rectTransform.anchoredPosition = Vector2.zero; edge.rectTransform.localRotation = Quaternion.Euler(0, 0, angle);
            }
            foreach (var angle in new[] { 45f, -45f })
            {
                var bar = Surface("Cross", face.transform, C(255, 250, 225), C(248, 230, 190), 1000);
                Centre(bar.rectTransform, 0, 0, size * .47f, size * .13f); bar.rectTransform.anchorMin = bar.rectTransform.anchorMax = new Vector2(.5f, .5f);
                bar.rectTransform.anchoredPosition = Vector2.zero; bar.rectTransform.localRotation = Quaternion.Euler(0, 0, angle);
            }
            return Clickable(r.gameObject, rim);
        }

        /// <summary>A cream card with a dark blue outline, a thin tan border and a grey lip under it (Area Progress, a task row).</summary>
        public static RectTransform CreamCard(string name, RectTransform parent, float x, float y, float w, float h)
        {
            var r = At(Node(name, parent), x, y, w, h);
            var outline = Surface("Outline", r, C(0, 60, 128), C(0, 60, 128), 18);
            var lip = Layer("Lip", outline.transform, 4, C(150, 146, 134), C(128, 126, 120), 15);
            var border = Surface("Border", lip.transform, C(176, 166, 132), C(160, 153, 126), 14);
            Inset(border.rectTransform, 0, 0, 0, 6);
            var face = Layer("Face", border.transform, 3, C(251, 239, 218), C(245, 226, 187), 12);
            return face.rectTransform;
        }

        /// <summary>The green action button (Do It, Continue, Unlock, View): dark green edge, a pale inner line, a glossy face.</summary>
        public static Button GreenButton(string name, RectTransform parent, float x, float y, float w, float h, out RectTransform face)
        {
            var r = At(Node(name, parent), x, y, w, h);
            var edge = Surface("Edge", r, C(92, 150, 21), C(66, 120, 6), 16);
            edge.raycastTarget = true;
            var pale = Layer("Pale line", edge.transform, 5, C(200, 238, 100), C(170, 220, 60), 12);
            var body = Layer("Face", pale.transform, 3, C(165, 214, 20), C(118, 178, 0), 10);
            var gloss = Surface("Gloss", body.transform, new Color(1, 1, 1, .30f), new Color(1, 1, 1, .04f), 9);
            Inset(gloss.rectTransform, 5, 3, 5, h * .52f);
            face = body.rectTransform;
            return Clickable(r.gameObject, edge);
        }

        static Button Clickable(GameObject go, Graphic target)
        {
            var b = go.AddComponent<Button>(); b.targetGraphic = target; b.transition = Selectable.Transition.None;
            var motion = go.AddComponent<RoyalButtonMotion>(); motion.pressScale = .95f; motion.hoverScale = 1; motion.response = 22;
            return b;
        }

        /// <summary>The progress bar: an orange frame, a dark track, a glossy green fill and the count over it.</summary>
        public static RectTransform ProgressBar(RectTransform parent, float x, float y, float w, float h, out RectTransform fill, out Text value, float textSize)
        {
            var r = At(Node("Progress bar", parent), x, y, w, h);
            var frame = Surface("Frame", r, C(254, 178, 20), C(240, 132, 0), h * .5f);
            var track = Layer("Track", frame.transform, 5, C(0, 70, 110), C(0, 88, 125), h * .5f - 5);
            fill = Node("Fill", track.transform); fill.anchorMax = new Vector2(.5f, 1);
            var green = Surface("Fill face", fill, C(141, 203, 0), C(56, 136, 0), h * .5f - 5);
            var gloss = Surface("Fill gloss", green.transform, new Color(1, 1, 1, .28f), new Color(1, 1, 1, 0), h * .3f);
            Inset(gloss.rectTransform, 4, 2, 4, (h - 10) * .5f);
            value = Words("Count", r, "0/0", textSize, C(250, 246, 204), TextAnchor.MiddleCenter);
            Outline(value, C(20, 60, 0), 2.4f);
            return r;
        }

        /// <summary>A green tick with a dark green edge (a done task).</summary>
        public static RectTransform Check(RectTransform parent, float x, float y, float size)
        {
            var r = Centre(Node("Done tick", parent), x, y, size, size);
            var edge = new GameObject("Edge", typeof(RectTransform)).AddComponent<RoyalCheckGraphic>();
            edge.transform.SetParent(r, false); Fill(edge.rectTransform); edge.thickness = .34f; edge.topColor = C(40, 110, 10); edge.bottomColor = C(30, 90, 5); edge.raycastTarget = false;
            var face = new GameObject("Face", typeof(RectTransform)).AddComponent<RoyalCheckGraphic>();
            face.transform.SetParent(r, false); Fill(face.rectTransform); face.thickness = .22f; face.topColor = C(170, 240, 60); face.bottomColor = C(70, 180, 10); face.raycastTarget = false;
            return r;
        }

        /// <summary>A steel padlock: a shackle arc over a rounded body with a keyhole.</summary>
        public static RectTransform Padlock(RectTransform parent, float x, float y, float size)
        {
            var r = Centre(Node("Padlock", parent), x, y, size * .8f, size);
            var shackle = new GameObject("Shackle", typeof(RectTransform)).AddComponent<RoyalArcGraphic>();
            shackle.transform.SetParent(r, false); shackle.raycastTarget = false; shackle.from = 0; shackle.to = 180; shackle.thickness = .30f;
            shackle.topColor = C(235, 240, 245); shackle.bottomColor = C(150, 160, 175);
            var s = shackle.rectTransform; s.anchorMin = new Vector2(.14f, .40f); s.anchorMax = new Vector2(.86f, 1.02f); s.offsetMin = s.offsetMax = Vector2.zero;
            var body = Surface("Body", r, C(222, 228, 236), C(140, 150, 166), size * .12f);
            body.rectTransform.anchorMin = new Vector2(0, 0); body.rectTransform.anchorMax = new Vector2(1, .62f);
            var inner = Layer("Face", body.transform, size * .05f, C(200, 208, 220), C(158, 168, 184), size * .09f);
            var hole = Surface("Keyhole", inner.transform, C(60, 66, 80), C(40, 44, 55), 1000);
            hole.rectTransform.anchorMin = new Vector2(.40f, .42f); hole.rectTransform.anchorMax = new Vector2(.60f, .74f);
            var slot = Surface("Keyhole slot", inner.transform, C(60, 66, 80), C(40, 44, 55), 3);
            slot.rectTransform.anchorMin = new Vector2(.455f, .20f); slot.rectTransform.anchorMax = new Vector2(.545f, .55f);
            return r;
        }

        public static Image Picture(string name, RectTransform parent, Sprite sprite, float x, float y, float w, float h)
        {
            var i = Centre(Node(name, parent), x, y, w, h).gameObject.AddComponent<Image>();
            i.sprite = sprite; i.preserveAspect = true; i.raycastTarget = false; return i;
        }

        // ---------------------------------------------------------------- lettering

        public static Text Words(string name, Transform parent, string value, float size, Color color, TextAnchor align)
        {
            var t = Node(name, parent).gameObject.AddComponent<Text>();
            t.font = Display; t.text = value; t.fontSize = Mathf.RoundToInt(size); t.color = color; t.alignment = align;
            t.raycastTarget = false; t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate;
            t.resizeTextForBestFit = true; t.resizeTextMaxSize = Mathf.RoundToInt(size); t.resizeTextMinSize = Mathf.Max(10, Mathf.RoundToInt(size * .55f));
            return t;
        }
        public static void Outline(Text t, Color color, float distance)
        { var o = t.gameObject.AddComponent<Outline>(); o.effectColor = color; o.effectDistance = new Vector2(distance, -distance); }
        /// <summary>The ribbon titles: cream to straw letters, a navy edge and a gold rim outside it.</summary>
        public static void Title(Text t)
        {
            var g = t.gameObject.AddComponent<RoyalTextGradient>(); g.top = Color.white; g.bottom = new Color(Straw.r / Cream.r, Straw.g / Cream.g, Straw.b / Cream.b);
            Outline(t, Navy, 3.2f); Outline(t, C(245, 182, 12), 2.6f);
            var s = t.gameObject.AddComponent<Shadow>(); s.effectColor = C(106, 51, 40, 200); s.effectDistance = new Vector2(0, -3);
        }
        /// <summary>Button captions and counts: cream letters with a dark green edge.</summary>
        public static void ButtonCaption(Text t) { Outline(t, C(30, 80, 5), 2.6f); }
    }
}
