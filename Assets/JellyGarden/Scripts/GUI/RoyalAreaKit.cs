using System;
using UnityEngine;
using UnityEngine.UI;
using CK = RoyalThrone.Presentation.RoyalCardKit;

namespace RoyalThrone.Presentation
{
    /// <summary>
    /// The area dialogs' pieces (2026-10-06, the user: the area cards and their unlock pop-up in the same design as every
    /// other card). The frame, ribbon, close, cream cards, buttons and bar are drawn in the card kit's look (RoyalCardKit:
    /// the result cards' gold border and royal-blue face, the blue ribbon under the crown, the red close with a gold ring,
    /// cream panels with one thin gold border, green pills); their names, sizes and placement are unchanged, so the area
    /// dialogs' layout code is the same. Before, they copied the reference's arched banner and layered orange frames.
    /// Drawn (no image assets), sized as below. Every size is in design
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
        /// The dialog's card: a soft shadow, one gold border (a bronze edge, the gold, a light along its top), a navy line and
        /// the royal-blue face with a light over its upper part and a faint rim inside the border (RoyalCardKit.CleanSurface,
        /// without the crest: the ribbon carries the crown). Returns the body, inside the border.
        /// </summary>
        public static RectTransform BluePanel(Transform parent)
        {
            const float r = CK.CardRadius, b = CK.CleanBorder;
            var root = Node("Area panel frame", parent);
            var shadow = CK.Inset(root, "Shadow", -8, r + 8, CK.C("000514", .5f), CK.C("000514", .5f));
            shadow.softness = 30; shadow.rectTransform.anchoredPosition = new Vector2(0, -8);
            CK.Inset(root, "Edge", 0, r, CK.C("b86c1e"), CK.C("8a4812"));
            CK.Inset(root, "Gold", 1.2f, r - 1.2f, CK.C("fbd65e"), CK.C("eca628"));
            CK.Strip(root, "Gold light", true, 1.2f, 70, r - 1.2f, CK.C("fff5c8", .9f), CK.C("fff5c8", 0));
            CK.Inset(root, "Navy line", b - 1.5f, r - b + 1.5f, CK.C("0a2c7c"), CK.C("061f5c"));
            CK.Inset(root, "Face", b, r - b, CK.C("1446a9"), CK.C("0b3386"));
            CK.Strip(root, "Face light", true, b, 300, r - b, CK.C("2a62cc", .7f), CK.C("2a62cc", 0));
            var glow = CK.Inset(root, "Face glow", b, r - b, CK.C("5b8ae6", .32f), CK.C("5b8ae6", .05f));
            glow.band = .5f; glow.innerSoftness = 30;
            var body = Node("Body", root); Inset(body, b + 3, b + 3, b + 3, b + 3);
            return body;
        }

        /// <summary>
        /// The title ribbon (RoyalCardKit.Ribbon): a blue pill with a gold rim and a navy line, the crest's crown resting on
        /// it, and the title in the cards' display lettering. `y` is the pill's top below the parent's top; it is at most 520
        /// wide, as on the cards.
        /// </summary>
        public static Text Ribbon(RectTransform parent, float width, float y, string title, float titleSize)
        {
            float w = Mathf.Min(CK.RibbonWidth, width), h = RibbonHeight, r = 26;
            var root = Node("Ribbon", parent);
            root.anchorMin = root.anchorMax = new Vector2(.5f, 1); root.pivot = new Vector2(.5f, 1);
            root.anchoredPosition = new Vector2(0, -y); root.sizeDelta = new Vector2(w, h);
            var shadow = CK.Inset(root, "Shadow", -4, r + 4, CK.C("000a30", .55f), CK.C("000a30", .55f));
            shadow.softness = 14; shadow.rectTransform.anchoredPosition = new Vector2(0, -7);
            CK.Inset(root, "Edge", 0, r, CK.C("8c4a14"), CK.C("6a3410"));
            CK.Inset(root, "Gold", 1.2f, r - 1.2f, CK.C("ffe27a"), CK.C("e6a22a"));
            CK.Inset(root, "Gold light", 1.2f, r - 1.2f, CK.C("fffbe0", .65f), CK.C("fffbe0", 0));
            CK.Inset(root, "Navy line", 6.5f, r - 6.5f, CK.C("0a2c7c"), CK.C("061f5c"));
            CK.Inset(root, "Face", 8, r - 8, CK.C("2e66d4"), CK.C("1440a2"));
            var gloss = CK.Strip(root, "Gloss", true, 10, h * .34f, h * .17f, CK.C("ffffff", .2f), CK.C("ffffff", .02f));
            gloss.rectTransform.sizeDelta = new Vector2(-2 * 30, h * .34f);
            // The crown, smaller than on the cards so it stays clear of Home's counters above the dialog.
            var crownArt = CK.CrownSprite();
            if (crownArt)
            {
                var crown = Node("Crown", root).gameObject.AddComponent<Image>(); crown.sprite = crownArt; crown.preserveAspect = true; crown.raycastTarget = false;
                float cw = CrownWidth, ch = cw * crownArt.rect.height / crownArt.rect.width;
                var cr = crown.rectTransform; cr.anchorMin = cr.anchorMax = new Vector2(.5f, 1); cr.pivot = new Vector2(.5f, 0);
                cr.sizeDelta = new Vector2(cw, ch); cr.anchoredPosition = new Vector2(0, -4);
            }
            var label = Words("Title", root, title, Mathf.Min(titleSize, 50), Cream, TextAnchor.MiddleCenter);
            Inset(label.rectTransform, 34, 10, 34, 12);
            Title(label);
            return label;
        }
        public const float RibbonHeight = 96, CrownWidth = 330;

        /// <summary>
        /// The cards' red close (RoyalCardKit.RedClose): a gold ring around a red face with a white X, centred on (x, y);
        /// `size` is the tap area, the drawing is 84% of it.
        /// </summary>
        public static Button RedClose(RectTransform parent, float x, float y, float size)
        {
            var r = Centre(Node("Close", parent), x, y, size, size);
            var hit = r.gameObject.AddComponent<Image>(); hit.color = Color.clear;
            var skin = Node("Skin", r); float d = size * .84f, k = d / 84f, rad = d * .5f, gap = (size - d) * .5f;
            Inset(skin, gap, gap, gap, gap);
            var shadow = CK.Inset(skin, "Shadow", -2, rad + 2, CK.C("1a0008", .55f), CK.C("1a0008", .55f));
            shadow.softness = 9; shadow.rectTransform.anchoredPosition = new Vector2(0, -3);
            CK.Inset(skin, "Edge", 0, rad, CK.C("7a3a12"), CK.C("5a2a0e"));
            CK.Inset(skin, "Gold", 1.2f * k, rad - 1.2f * k, CK.C("ffe27a"), CK.C("e3a02a"));
            CK.Inset(skin, "Dark line", 6.4f * k, rad - 6.4f * k, CK.C("6a0c0c"), CK.C("4e0808"));
            CK.Inset(skin, "Face", 7.6f * k, rad - 7.6f * k, CK.C("f4513f"), CK.C("b3171d"));
            var shine = CK.Strip(skin, "Shine", true, 10 * k, 26 * k, 13 * k, CK.C("ffffff", .32f), CK.C("ffffff", .03f));
            shine.rectTransform.sizeDelta = new Vector2(-2 * 20 * k, 26 * k);
            for (int pass = 0; pass < 2; pass++)
                for (int arm = 0; arm < 2; arm++)
                {
                    bool shade = pass == 0;
                    var bar = CK.Shape(skin, (shade ? "Cross edge " : "Cross ") + arm, 0, shade ? -2.2f * k : 0, 44 * k, 11 * k, 5.5f * k,
                        shade ? CK.C("5a0505", .7f) : CK.C("ffffff"), shade ? CK.C("5a0505", .7f) : CK.C("f6e8e2"));
                    bar.rectTransform.localRotation = Quaternion.Euler(0, 0, arm == 0 ? 45 : -45);
                }
            return Clickable(r.gameObject, hit);
        }

        /// <summary>The cards' cream panel (RoyalCardKit.Panel): one thin gold border and a soft light over the cream's upper
        /// half. Returns the cream face, inside the border, for the content.</summary>
        public static RectTransform CreamCard(string name, RectTransform parent, float x, float y, float w, float h)
        {
            var r = At(Node(name, parent), x, y, w, h);
            float rad = Mathf.Min(CK.PanelRadius, h * .3f), border = CK.PanelBorder;
            var shadow = CK.Inset(r, "Shadow", -2, rad + 2, CK.C("00103a", .35f), CK.C("00103a", .35f));
            shadow.softness = 10; shadow.rectTransform.anchoredPosition = new Vector2(0, -3);
            CK.Inset(r, "Edge", 0, rad, CK.C("c47a26"), CK.C("a8641c"));
            CK.Inset(r, "Gold", 1, rad - 1, CK.C("fde58a"), CK.C("e9ae44"));
            CK.Inset(r, "Cream", border, rad - border, CK.C("fff6e1"), CK.C("f5e2bc"));
            CK.Strip(r, "Cream shine", true, border, h * .45f, rad - border, CK.C("ffffff", .55f), CK.C("ffffff", 0));
            var face = Node("Face", r); Inset(face, border, border, border, border);
            return face;
        }

        /// <summary>The cards' primary button (RoyalCardKit.CleanFace): a green pill with a gold border, a gloss and the passing
        /// sweep, over a soft shadow. `face` is the area inside the border, for the caption.</summary>
        public static Button GreenButton(string name, RectTransform parent, float x, float y, float w, float h, out RectTransform face)
        {
            var r = At(Node(name, parent), x, y, w, h);
            var hit = r.gameObject.AddComponent<Image>(); hit.color = Color.clear;
            float k = Mathf.Clamp(h / 116, .6f, 1);
            var shadow = CK.Inset(r, "Shadow", 0, h * .5f, CK.C("011140", .55f), CK.C("011140", .55f));
            shadow.softness = 14 * k; shadow.rectTransform.anchoredPosition = new Vector2(0, -6 * k);
            var skin = Node("Skin", r);
            CK.CleanFace(skin, h, CK.Kind.Primary);
            face = Node("Face", r); Inset(face, 9, 9, 9, 9);
            return Clickable(r.gameObject, hit);
        }

        static Button Clickable(GameObject go, Graphic target)
        {
            var b = go.AddComponent<Button>(); b.targetGraphic = target; b.transition = Selectable.Transition.None;
            var motion = go.AddComponent<RoyalButtonMotion>(); motion.pressScale = .95f; motion.hoverScale = 1; motion.response = 22;
            return b;
        }

        /// <summary>The progress bar in the cards' colours: a gold rim around a navy track, the primary button's green fill
        /// with its gloss, and the count over it in white.</summary>
        public static RectTransform ProgressBar(RectTransform parent, float x, float y, float w, float h, out RectTransform fill, out Text value, float textSize)
        {
            var r = At(Node("Progress bar", parent), x, y, w, h);
            float rad = h * .5f;
            CK.Inset(r, "Edge", 0, rad, CK.C("8c4a14"), CK.C("6a3410"));
            CK.Inset(r, "Gold", 1.1f, rad - 1.1f, CK.C("ffe27a"), CK.C("e6a22a"));
            var track = Layer("Track", r, 5, CK.C("0a2466"), CK.C("16409a"), rad - 5);
            fill = Node("Fill", track.transform); fill.anchorMax = new Vector2(.5f, 1);
            var green = Surface("Fill face", fill, CK.C("3ec255"), CK.C("148a38"), rad - 5);
            var gloss = Surface("Fill gloss", green.transform, new Color(1, 1, 1, .28f), new Color(1, 1, 1, 0), h * .3f);
            Inset(gloss.rectTransform, 4, 2, 4, (h - 10) * .5f);
            value = Words("Count", r, "0/0", textSize, Color.white, TextAnchor.MiddleCenter);
            Lettering(value, CK.Ink.OnBlue);
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
        /// <summary>Titles (the ribbons, the area names): the cards' display lettering, ivory warming to cream with a thin
        /// warm-brown edge.</summary>
        public static void Title(Text t) { Lettering(t, CK.Ink.Display); }
        /// <summary>Button captions and costs: the cards' white on green, its edge in the button's dark green; never larger
        /// than the cards' captions (about a third of the button's height).</summary>
        public static void ButtonCaption(Text t)
        {
            var box = t.transform.parent as RectTransform;
            if (box && box.rect.height > 0) t.resizeTextMaxSize = Mathf.Min(t.resizeTextMaxSize, Mathf.RoundToInt((box.rect.height + 18) * .36f));
            Lettering(t, CK.Ink.OnGreen);
        }
        /// <summary>One of the cards' inks on a best-fit label: area and task names change at run time, so the label keeps
        /// fitting its box (RoyalCardKit.Paint alone sets one exact size).</summary>
        public static void Lettering(Text t, CK.Ink ink)
        {
            bool fit = t.resizeTextForBestFit; int max = t.resizeTextMaxSize, min = t.resizeTextMinSize, size = t.fontSize;
            var align = t.alignment; var wrap = t.horizontalOverflow; var cut = t.verticalOverflow;
            CK.Paint(t, ink, size);
            t.resizeTextForBestFit = fit; t.resizeTextMaxSize = max; t.resizeTextMinSize = min;
            t.alignment = align; t.horizontalOverflow = wrap; t.verticalOverflow = cut;
        }
    }
}
