using System.Collections.Generic;
using JellyGarden.Scripts.GUI;
using JellyGarden.Scripts.Juice;
using JellyGarden.Scripts.Targets;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalThrone.Presentation
{
    // The Royal Throne card kit: the Level Start card's visual language (Design/LevelStart) as parts, so every popup is built
    // from the same frame, crest, lettering, panels and buttons. Canvas units; colours measured from the reference.
    // Card layout: the crest sits on the top edge, the title in the slot below the ribbon ends (the level number's place on
    // the Level Start card), content below it, the primary action at the bottom above the small diamond ornament.
    public static class RoyalCardKit
    {
        // Every card's centre sits 33 units below the screen's centre: the Level Start card's place when it was matched to the
        // reference (its card 40 below a root the scene had raised by 7).
        public const float CardWidth = 664, CardRadius = 37.5f, FaceInset = 17.6f, CardOffsetY = -33;
        // A centred line of the display font draws its capitals about a tenth of the font size below the rect's centre.
        public const float CapDrop = .1f;
        public const float Squircle = 2.75f;   // the booster tiles' corner curve, fitted to the reference
        public const float TitleDrop = 126, CrestDrop = 10.2f, CloseInsetX = 44.2f, CloseInsetY = 34.8f, OrnamentRise = 31.5f;
        public static readonly Color DimColor = new Color(.01f, .02f, .06f, .78f);

        // The level-end cards (the So Close offer and the result cards after it) use a lighter system: one gold border with a
        // soft inner light, one radius family, and fixed heights for the title, the panels and the two kinds of button.
        public const float CleanBorder = 9, PanelRadius = 30, PanelBorder = 4.5f, PrimaryHeight = 116, SecondaryHeight = 82;
        public const float CleanTitleDrop = 178, CleanTitleSize = 70, CleanBottom = 52, LevelLabelDrop = 33, LevelNumberDrop = 73;

        public enum Ink { Hero, Heading, HeadingOnCream, Value, Label, Body, BodyOnCream, Caption, CaptionOnBlue, Accent, Subtitle, CaptionOnRed,
            Display, OnGreen, OnBlue, OnRed, Light, Navy, NavySoft }
        public enum Kind { Primary, Secondary, Danger }

        // ---- Cards -----------------------------------------------------------------------------------------------------

        // A popup's card: the dimmed backdrop, the card placed and skinned (shadow, gold border, face, crest), and its title.
        // Returns the card's "Royal skin" (RoyalMenuSkin.Apply skips menus whose card already has one).
        public static Transform Card(Transform menu, Transform card, float height, string title, float offsetY = CardOffsetY, bool clean = false)
        {
            foreach (var clamp in menu.GetComponentsInChildren<KeepUIInScreen>(true)) clamp.enabled = false;
            // The inherited popup roots carry authoring offsets (the shops, the guide, settings and How to play sat 95 units
            // high, the Level Start card 7): centre them all, so every card shares one place on screen.
            var root = menu as RectTransform;
            if (root && root.anchoredPosition.sqrMagnitude > .0001f) root.anchoredPosition = Vector2.zero;
            var backdrop = menu.GetComponent<Image>();
            if (backdrop) backdrop.color = DimColor;
            Hide(menu.Find("Leadboard"));
            RoyalMenuSkin.Place(card, 0, offsetY, CardWidth, height);
            var image = card.GetComponent<Image>();
            if (image) image.color = Color.clear;
            var skin = Container(card, "Royal skin");
            skin.SetAsFirstSibling();
            if (clean) CleanSurface(skin, CardWidth, height); else Surface(skin, CardWidth, height);
            if (title != null)
            {
                if (clean) Title(card, title, height, 560, CleanTitleSize, CleanTitleDrop, false, Ink.Display);
                else Title(card, title, height);
            }
            return skin;
        }

        // The level-end cards' surface: a soft shadow, one gold border (a thin bronze edge, the gold, a light along its top),
        // a thin navy line and the royal-blue face with a light over its upper part and a faint rim just inside the border.
        // The crest is the same as on every card.
        public static void CleanSurface(Transform skin, float width, float height)
        {
            const float r = CardRadius, b = CleanBorder;
            var shadow = Shape(skin, "Shadow", 0, -8, width + 16, height + 16, r + 8, C("000514", .5f), C("000514", .5f));
            shadow.softness = 30;
            Inset(skin, "Edge", 0, r, C("b86c1e"), C("8a4812"));
            Inset(skin, "Gold", 1.2f, r - 1.2f, C("fbd65e"), C("eca628"));
            Strip(skin, "Gold light", true, 1.2f, 70, r - 1.2f, C("fff5c8", .9f), C("fff5c8", 0));
            Inset(skin, "Navy line", b - 1.5f, r - b + 1.5f, C("0a2c7c"), C("061f5c"));
            Inset(skin, "Face", b, r - b, C("1446a9"), C("0b3386"));
            Strip(skin, "Face light", true, b, Mathf.Min(height * .45f, 360) - b, r - b, C("2a62cc", .7f), C("2a62cc", 0));
            var glow = Inset(skin, "Face glow", b, r - b, C("5b8ae6", .32f), C("5b8ae6", .05f));
            glow.band = .5f; glow.innerSoftness = 30;
            var crest = RoyalMenuSkin.Art(skin, "Crest", "level-card-crest", .37f, height * .5f - CrestDrop, 444.7f, 213.2f);
            crest.preserveAspect = false;
        }

        // LEVEL over the level number on the crest's ribbon, the same on every level-end card.
        public static void LevelBadge(Transform card, Text number)
        {
            float top = ((RectTransform)card).sizeDelta.y * .5f;
            Words(card, "Royal level label", "LEVEL", 0, top - LevelLabelDrop, 160, 24, Ink.Display);
            if (number) Field(number.transform, 0, top - LevelNumberDrop, 160, 40, Ink.Display);
        }

        // The level-end cards' cream panel: one thin gold border and a soft light over the cream's upper half.
        public static Transform Panel(Transform parent, string name, float x, float y, float w, float h, float r = PanelRadius)
        {
            var panel = Container(parent, name);
            RoyalMenuSkin.Place(panel, x, y, w, h);
            var shadow = Shape(panel, "Shadow", 0, -3, w + 4, h + 4, r + 2, C("00103a", .35f), C("00103a", .35f));
            shadow.softness = 10;
            Inset(panel, "Edge", 0, r, C("c47a26"), C("a8641c"));
            Inset(panel, "Gold", 1, r - 1, C("fde58a"), C("e9ae44"));
            Inset(panel, "Cream", PanelBorder, r - PanelBorder, C("fff6e1"), C("f5e2bc"));
            Strip(panel, "Cream shine", true, PanelBorder, h * .45f, r - PanelBorder, C("ffffff", .55f), C("ffffff", 0));
            return panel;
        }

        // A level-end button: a pill with one gold border (8 units on the primary, 5.5 on the secondary), green or royal blue.
        public static Transform CleanButton(Transform t, string caption, float x, float y, float w, float h, Kind kind, float captionSize = 0)
        {
            if (!t) return null;
            Control(t, x, y, w, 0, h);
            CleanFace(t.Find("Royal skin"), h, kind);
            if (caption != null)
            {
                float size = captionSize > 0 ? captionSize : Mathf.Round(h * (kind == Kind.Secondary ? .32f : .34f));
                Words(t, "Royal caption", caption, 0, h * .0125f, w - 24, size, CaptionInk(kind));
            }
            return t;
        }

        public static Ink CaptionInk(Kind kind) => kind == Kind.Primary ? Ink.OnGreen : kind == Kind.Danger ? Ink.OnRed : Ink.OnBlue;

        // Green (primary), royal blue (secondary) or red (a destructive choice); the gold border is 8 units, 5.5 on the blue.
        public static void CleanFace(Transform skin, float h, Kind kind)
        {
            float r = h * .5f, border = kind == Kind.Secondary ? 5.5f : 8, face = border + 1.3f;
            Color line = C("0c5424"), lineLow = C("093f1b"), top = C("3ec255"), bottom = C("148a38");
            if (kind == Kind.Secondary) { line = C("0a2466"); lineLow = C("071a4c"); top = C("3068d6"); bottom = C("1a44a6"); }
            if (kind == Kind.Danger) { line = C("6a0c0c"); lineLow = C("4e0808"); top = C("f2493e"); bottom = C("b3171d"); }
            Inset(skin, "Edge", 0, r, C("8c4a14"), C("6a3410"));
            Inset(skin, "Gold", 1.1f, r - 1.1f, C("ffe27a"), C("e6a22a"));
            Inset(skin, "Gold light", 1.1f, r - 1.1f, C("fffbe0", .6f), C("fffbe0", 0));
            Inset(skin, "Dark line", border, r - border, line, lineLow);
            Inset(skin, "Green", face, r - face, top, bottom);   // "Green": the face layer's name for every kind
            float fr = r - face, lens = (h - 2 * face) * .42f;
            var gloss = Strip(skin, "Gloss", true, face + 2, lens, lens * .5f, C("ffffff", kind == Kind.Secondary ? .18f : .22f), C("ffffff", .03f));
            gloss.rectTransform.sizeDelta = new Vector2(-2 * (face + fr * .45f), lens);
            var sweep = Container(skin, "Sweep").gameObject.AddComponent<RoyalSweepGraphic>();
            Stretch(sweep.rectTransform, face);
            sweep.radius = fr; sweep.raycastTarget = false; sweep.color = new Color(1, 1, .96f);
        }

        // A thin vertical divider inside a button (between CONTINUE and its price): a dark groove with a light beside it.
        public static void Divider(Transform button, float x, float height, Kind kind)
        {
            var d = Container(button, "Royal divider");
            var rt = (RectTransform)d;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(.5f, .5f);
            rt.sizeDelta = new Vector2(4, height); rt.anchoredPosition = new Vector2(x, 0);
            bool primary = kind == Kind.Primary;
            Shape(d, "Groove", -.7f, 0, 1.8f, height, .9f, primary ? C("0a4a1c", .7f) : C("071a4c", .75f), primary ? C("0a4a1c", .7f) : C("071a4c", .75f));
            Shape(d, "Light", .9f, 0, 1.2f, height, .6f, primary ? C("b4f0b8", .35f) : C("a9c4ff", .35f), primary ? C("b4f0b8", .35f) : C("a9c4ff", .35f));
        }

        // On a screen wider than a 9:16 phone (a tablet) a level-end card keeps a phone's share of the screen height instead of
        // growing with the width: its menu root (backdrop included, grown to cover the screen again) scales down, to 0.72 at most.
        public static void FitToScreen(RectTransform root)
        {
            var parent = root ? root.parent as RectTransform : null;
            if (!parent) return;
            var area = parent.rect.size;
            if (area.x <= 0 || area.y <= 0) return;
            float s = Mathf.Clamp(area.y / area.x / (16f / 9f), .72f, 1);
            bool scaled = Mathf.Abs(root.localScale.x - 1) > .0005f;
            if (s > .9995f && !scaled) return;   // a phone: the root keeps its own layout
            var pad = area * (1 / s - 1) * .5f;
            if (Mathf.Abs(root.localScale.x - s) < .0005f && (root.offsetMax - pad).sqrMagnitude < .01f) return;
            root.localScale = new Vector3(s, s, 1);
            root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one; root.pivot = new Vector2(.5f, .5f);
            root.offsetMin = -pad; root.offsetMax = pad;
        }

        // The same share for a card inside a fixed-size cover (the Quit Level question): the scale FitToScreen would use.
        public static float TabletScale(Transform t)
        {
            var canvas = t ? t.GetComponentInParent<Canvas>() : null;
            if (!canvas) return 1;
            var area = ((RectTransform)canvas.rootCanvas.transform).rect.size;
            return area.x > 0 && area.y > 0 ? Mathf.Clamp(area.y / area.x / (16f / 9f), .72f, 1) : 1;
        }

        // ---- Ribbon cards (the Level Failed and Quit Level cards) ------------------------------------------------------

        // A card whose title sits on a wide blue ribbon across its top edge, under the crest's crown, instead of on the
        // crest's small tab: the level-end surface with the tab crest hidden, the ribbon, and the title ("Royal heading").
        public static Transform RibbonCard(Transform menu, Transform card, float height, string title, float offsetY = CardOffsetY)
        {
            var skin = Card(menu, card, height, null, offsetY, true);
            var crest = skin.Find("Crest");
            if (crest) crest.GetComponent<Image>().enabled = false;   // kept: ButtonShadow orders its shadows by it
            Ribbon(card, title, height);
            return skin;
        }

        public const float RibbonWidth = 520, RibbonHeight = 100, RibbonDrop = 16;

        public static Text Ribbon(Transform card, string title, float height)
        {
            float top = height * .5f, y = top - RibbonDrop, w = RibbonWidth, h = RibbonHeight, r = 26;
            var ribbon = Container(card, "Royal ribbon");
            RoyalMenuSkin.Place(ribbon, 0, y, w, h);
            var shadow = Shape(ribbon, "Shadow", 0, -7, w + 8, h + 8, r + 4, C("000a30", .55f), C("000a30", .55f));
            shadow.softness = 14;
            var body = Container(ribbon, "Body");
            Inset(body, "Edge", 0, r, C("8c4a14"), C("6a3410"));
            Inset(body, "Gold", 1.2f, r - 1.2f, C("ffe27a"), C("e6a22a"));
            Inset(body, "Gold light", 1.2f, r - 1.2f, C("fffbe0", .65f), C("fffbe0", 0));
            Inset(body, "Navy line", 6.5f, r - 6.5f, C("0a2c7c"), C("061f5c"));
            Inset(body, "Face", 8, r - 8, C("2e66d4"), C("1440a2"));
            var gloss = Strip(body, "Gloss", true, 10, h * .34f, h * .17f, C("ffffff", .2f), C("ffffff", .02f));
            gloss.rectTransform.sizeDelta = new Vector2(-2 * 30, h * .34f);
            // The crest's crown and scroll, cut from the crest art, resting on the ribbon's upper edge (4 units over its rim).
            var crownArt = CrownSprite();
            if (crownArt)
            {
                float crownHeight = CrownWidth * CrownCut.height / CrownCut.width;
                var crown = RoyalMenuSkin.Art(ribbon, "Crown", "level-card-crest", 0, h * .5f - 4 + crownHeight * .5f, CrownWidth, crownHeight);
                crown.sprite = crownArt; crown.preserveAspect = true;
            }
            var text = Words(ribbon, "Royal heading", title, 0, -2, w - 70, 50, Ink.Display);
            float natural = text.preferredWidth;
            if (natural > w - 70) { float size = Mathf.Floor(50 * (w - 70) / natural); Paint(text, Ink.Display, size); RoyalMenuSkin.Place(text.transform, 0, -2 + size * CapDrop, w - 70, size * 1.4f); }
            return text;
        }

        // The cut in the 605 x 290 crest art (from its top-left): its full width down to row 125, where the tab begins. The
        // scroll bar tapers to nothing before the art's sides, so the cut leaves no hard edge except along the bottom.
        static readonly Rect CrownCut = new Rect(0, 0, 605, 125);
        const float CrownWidth = 484;
        static Sprite crownSprite;
        static Sprite CrownSprite()
        {
            if (crownSprite) return crownSprite;
            var crest = RoyalPieceSkin.Art("level-card-crest");
            if (!crest) return null;
            var t = crest.textureRect;
            float kx = t.width / 605f, ky = t.height / 290f;
            crownSprite = Sprite.Create(crest.texture, new Rect(t.x + CrownCut.x * kx, t.yMax - (CrownCut.y + CrownCut.height) * ky, CrownCut.width * kx, CrownCut.height * ky), new Vector2(.5f, .5f), 100);
            crownSprite.name = "Royal crown ornament";
            return crownSprite;
        }

        // The red close control of the ribbon cards (a gold ring, a white X), overlapping the card's top-right corner.
        public static Transform RedClose(Transform t, float height)
        {
            if (!t) return null;
            var c = Control(t, CardWidth * .5f - 30, height * .5f - 22, 100, 84);
            var skin = c.Find("Royal skin");
            const float d = 84, r = d * .5f;
            var shadow = Shape(skin, "Shadow", 0, -3, d + 4, d + 4, r + 2, C("1a0008", .55f), C("1a0008", .55f));
            shadow.softness = 9;
            Inset(skin, "Edge", 0, r, C("7a3a12"), C("5a2a0e"));
            Inset(skin, "Gold", 1.2f, r - 1.2f, C("ffe27a"), C("e3a02a"));
            Inset(skin, "Dark line", 6.4f, r - 6.4f, C("6a0c0c"), C("4e0808"));
            Inset(skin, "Face", 7.6f, r - 7.6f, C("f4513f"), C("b3171d"));
            var shine = Strip(skin, "Shine", true, 10, 26, 13, C("ffffff", .32f), C("ffffff", .03f));
            shine.rectTransform.sizeDelta = new Vector2(-2 * 20, 26);
            var glyph = Container(c, "Royal glyph");
            for (int pass = 0; pass < 2; pass++)
                for (int arm = 0; arm < 2; arm++)
                {
                    bool shade = pass == 0;
                    var bar = Shape(glyph, (shade ? "Shadow " : "Arm ") + arm, 0, shade ? -2.2f : 0, 44, 11, 5.5f,
                        shade ? C("5a0505", .7f) : C("ffffff"), shade ? C("5a0505", .7f) : C("f6e8e2"));
                    bar.rectTransform.localRotation = Quaternion.Euler(0, 0, arm == 0 ? 45 : -45);
                }
            return c;
        }

        // A broken heart (the life heart split in two, the halves tipped apart) with short rays either side when `rays`.
        public static Transform BrokenHeart(Transform parent, string name, float x, float y, float size, bool rays)
        {
            var holder = Container(parent, name);
            RoyalMenuSkin.Place(holder, x, y, size, size);
            var art = RoyalPieceSkin.Art("heart");
            for (int side = -1; side <= 1; side += 2)
            {
                var half = RoyalMenuSkin.Art(holder, side < 0 ? "Left half" : "Right half", "heart", side * size * .035f, 0, size, size);
                half.sprite = art; half.type = Image.Type.Filled; half.fillMethod = Image.FillMethod.Horizontal;
                half.fillOrigin = side < 0 ? (int)Image.OriginHorizontal.Left : (int)Image.OriginHorizontal.Right; half.fillAmount = .5f;
                var hr = half.rectTransform;
                hr.pivot = new Vector2(.5f, .1f); hr.anchoredPosition = new Vector2(side * size * .035f, -size * .4f);
                hr.localRotation = Quaternion.Euler(0, 0, side * -7);
            }
            if (rays)
                for (int side = -1; side <= 1; side += 2)
                    for (int i = -1; i <= 1; i++)
                    {
                        float a = i * 32 * Mathf.Deg2Rad, dist = size * .66f;
                        var ray = Shape(holder, "Ray " + (side < 0 ? "left " : "right ") + (i + 2), side * dist * Mathf.Cos(a), size * .06f + dist * Mathf.Sin(a) * .9f, size * .16f, size * .055f, size * .0275f, C("f3a07c"), C("ee8e6a"));
                        ray.rectTransform.localRotation = Quaternion.Euler(0, 0, side * i * 32);
                    }
            return holder;
        }

        // The card surface under `skin`: soft shadow, the layered gold border, the blue face with its light, the crest.
        public static void Surface(Transform skin, float width, float height)
        {
            const float r = CardRadius;
            var shadow = Shape(skin, "Shadow", 0, -6, width + 20, height + 20, r + 10, C("000514", .6f), C("000514", .6f));
            shadow.softness = 28;
            Inset(skin, "Edge", 0, r, C("a0561a"), C("6c2f0c"));
            Inset(skin, "Gold", 1.5f, r - 1.5f, C("f9c643"), C("efa925"));
            Strip(skin, "Gold light", true, 1.5f, 80, r - 1.5f, C("fff6c8", .95f), C("fff6c8", 0));
            Strip(skin, "Gold low light", false, 1.5f, 80, r - 1.5f, C("ffe17a", 0), C("ffe17a", .55f));
            Inset(skin, "Inner brown", 9.6f, r - 9.6f, C("a8591a"), C("7a3913"));
            Inset(skin, "Navy line", 13.5f, r - 13.5f, C("04256e"), C("021745"));
            Inset(skin, "Blue line", 15.6f, r - 15.6f, C("0b5ac6"), C("0851b8"));
            Inset(skin, "Face", FaceInset, r - FaceInset, C("022f7c"), C("01286e"));
            // The face's light falls off over the upper 390 units, as on the 862-unit Level Start card.
            Strip(skin, "Face light", true, FaceInset, Mathf.Min(height * .45f, 388) - FaceInset, r - FaceInset, C("0749b5"), C("0749b5", 0));
            // A lighter rim inside the border, as in the reference: clear along the sides, almost gone along the bottom.
            var glow = Inset(skin, "Face glow", FaceInset, r - FaceInset, C("0c50ba", .5f), C("0c50ba", .08f));
            glow.band = .5f; glow.innerSoftness = 46;
            // Crown, scroll ornament, raised tab and ribbon ends: cut from the reference; its crown axis sits at x = 0.
            var crest = RoyalMenuSkin.Art(skin, "Crest", "level-card-crest", .37f, height * .5f - CrestDrop, 444.7f, 213.2f);
            crest.preserveAspect = false;
        }

        // The small bronze diamond and its two lines, just above the card's bottom edge.
        public static void Ornament(Transform skin, float height)
        {
            float y = -height * .5f + OrnamentRise;
            var diamond = Shape(skin, "Ornament diamond", 0, y, 15, 15, 1.5f, C("7c5f2c"), C("6a4f24"));
            diamond.band = 2.2f;
            diamond.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
            Shape(skin, "Ornament line left", -45.2f, y, 53, 1.6f, .8f, C("5c4c36"), C("5c4c36"));
            Shape(skin, "Ornament line right", 45.2f, y, 53, 1.6f, .8f, C("5c4c36"), C("5c4c36"));
        }

        // The soft dark shadow a primary button casts on the face.
        public static void ButtonShadow(Transform skin, float x, float y, float w, float h)
        {
            var s = Shape(skin, "Button shadow", x, y - 8 * Mathf.Clamp(h / 120, .6f, 1), w, h, h * .395f, C("011140", .95f), C("011140", .95f));
            s.softness = 26 * Mathf.Clamp(h / 120, .6f, 1);
            s.transform.SetSiblingIndex(skin.Find("Crest").GetSiblingIndex());
        }

        // The card title ("Royal heading", which the dialog review looks for) in the level number's lettering, fitted to width.
        public static Text Title(Transform card, string title, float height, float maxWidth = 560, float size = 46, float drop = TitleDrop, bool shade = true, Ink ink = Ink.Hero)
        {
            float y = height * .5f - drop;
            var text = Words(card, "Royal heading", title, 0, y, maxWidth, size, ink);
            float natural = text.preferredWidth;
            if (natural > maxWidth) { size = Mathf.Floor(size * maxWidth / natural); Paint(text, ink, size); RoyalMenuSkin.Place(text.transform, 0, y + size * CapDrop, maxWidth, size * 1.4f); }
            // The number's dark halo on the Level Start card, sized to the title: face shading under the crest.
            var skin = card.Find("Royal skin");
            if (shade && skin && !skin.Find("Title halo"))
            {
                var halo = Shape(skin, "Title halo", 0, y, Mathf.Min(text.preferredWidth + 40, CardWidth - 60), 70, 30, C("001a5e", .42f), C("001a5e", .42f));
                halo.softness = 44;
                var crest = skin.Find("Crest");
                if (crest) halo.transform.SetSiblingIndex(crest.GetSiblingIndex());
            }
            return text;
        }

        // The close control in the card's top-right corner: a 73.5-unit circle with a 96-unit tap area.
        public static Transform Close(Transform close, float height)
        {
            if (!close) return null;
            var c = Control(close, CardWidth * .5f - CloseInsetX, height * .5f - CloseInsetY, 96, 73.5f);
            CloseFace(c);
            return c;
        }

        // ---- Lettering -------------------------------------------------------------------------------------------------

        // A label with an exact size, placed by the centre of its capitals, in one of the card's inks.
        public static Text Words(Transform parent, string name, string value, float x, float capY, float width, float size, Ink ink)
        {
            var text = RoyalMenuSkin.Label(parent, name, value, x, capY + size * CapDrop, width, size * 1.4f, Mathf.RoundToInt(size));
            text.enabled = true;
            Paint(text, ink, size);
            return text;
        }

        // The same without an ink: the caller styles it.
        public static Text Words(Transform parent, string name, string value, float x, float capY, float width, float size)
        {
            var text = RoyalMenuSkin.Label(parent, name, value, x, capY + size * CapDrop, width, size * 1.4f, Mathf.RoundToInt(size));
            text.enabled = true;
            Plain(text, size);
            return text;
        }

        // Restyle an existing Text in place (its content is someone else's): exact size, one ink.
        public static Text Field(Transform t, float x, float capY, float width, float size, Ink ink)
        {
            var text = t ? t.GetComponent<Text>() : null;
            if (!text) return null;
            RoyalMenuSkin.Place(t, x, capY + size * CapDrop, width, size * 1.4f);
            RoyalMenuSkin.Typography(text, Mathf.RoundToInt(size));
            text.enabled = true;
            Paint(text, ink, size);
            return text;
        }

        public static void Paint(Text text, Ink ink, float size)
        {
            Plain(text, size);
            float k = size / 46f;
            switch (ink)
            {
                case Ink.Hero: Style(text, C("fffdf0"), C("f5cf82"), C("e8901c"), 1.6f * Mathf.Max(k, .6f), C("1b1030"), 1.5f * Mathf.Max(k, .6f), C("2a1206", .95f), new Vector2(0, -2.8f * Mathf.Max(k, .6f))); break;
                case Ink.Heading: Style(text, C("fdf6bd"), C("f5c658"), Color.clear, 0, C("0b1a4a"), 1.4f, C("021548", .9f), new Vector2(0, -2)); break;
                case Ink.HeadingOnCream: Style(text, C("9a5a1a"), C("8c5012"), Color.clear, 0, Color.clear, 0, C("ffffff", .7f), new Vector2(0, -1.3f)); break;
                case Ink.Value: Style(text, C("10306f"), C("0a2260"), Color.clear, 0, Color.clear, 0, C("ffffff", .55f), new Vector2(0, -1.3f)); break;
                case Ink.Label: Style(text, C("fffdf0"), C("c8ccd4"), Color.clear, 0, C("0a1a50"), 1.2f, C("001450", .9f), new Vector2(0, -1.6f)); break;
                case Ink.Body: Style(text, C("fbf3dc"), C("eadcbc"), Color.clear, 0, Color.clear, 0, C("00103a", .85f), new Vector2(0, -1.6f)); break;
                case Ink.BodyOnCream: Style(text, C("24406f"), C("1b3260"), Color.clear, 0, Color.clear, 0, C("ffffff", .5f), new Vector2(0, -1.1f)); break;
                case Ink.Caption: Style(text, C("fffdf3"), C("f4e1b9"), Color.clear, 0, C("01281f"), 1.8f * Mathf.Clamp(size / 47f, .6f, 1), C("00382c", .9f), new Vector2(0, -3.2f * Mathf.Clamp(size / 47f, .6f, 1))); break;
                case Ink.CaptionOnBlue: Style(text, C("fffdf3"), C("e3e7f2"), Color.clear, 0, C("0a1a4a"), 1.3f, C("000a30", .85f), new Vector2(0, -1.8f)); break;
                case Ink.Accent: Style(text, C("ffe9a6"), C("f3c25a"), Color.clear, 0, C("2a1a08", .9f), 1.1f, C("00103a", .8f), new Vector2(0, -1.4f)); break;
                case Ink.CaptionOnRed: Style(text, C("fffdf3"), C("f8e3bd"), Color.clear, 0, C("4a0707"), 1.8f * Mathf.Clamp(size / 47f, .6f, 1), C("3a0404", .9f), new Vector2(0, -3.2f * Mathf.Clamp(size / 47f, .6f, 1))); break;
                case Ink.Subtitle: Style(text, C("f4f7ff"), C("e6edfb"), Color.clear, 0, Color.clear, 0, C("001040", .5f), new Vector2(0, -1.4f), size * .01f); break;
                // The level-end cards' type: a title ivory warming to cream with a thin warm-brown edge; captions white with an
                // edge in the dark of their button's colour; labels on blue plain white with a soft shadow.
                case Ink.Display: Clean(text, size, C("fffbf0"), C("ffe4a8"), C("7a3a0c"), .036f, C("1c0a02", .45f), .03f); break;
                case Ink.OnGreen: Clean(text, size, C("ffffff"), C("f2f7ea"), C("0b5424"), .034f, C("063a17", .5f), .02f); break;
                case Ink.OnBlue: Clean(text, size, C("ffffff"), C("edf2ff"), C("0b2766"), .034f, C("06154a", .5f), .02f); break;
                case Ink.OnRed: Clean(text, size, C("ffffff"), C("fff0ea"), C("761010"), .034f, C("4a0606", .5f), .02f); break;
                case Ink.Light: Clean(text, size, C("ffffff"), C("f1f5ff"), Color.clear, 0, C("041a52", .55f), .01f); break;
                // On cream: a flat navy (a softer one for supporting lines) with a faint white lift below, no outline.
                case Ink.Navy: Style(text, C("12337c"), C("0c2766"), Color.clear, 0, Color.clear, 0, C("ffffff", .6f), new Vector2(0, -1.2f), size * .018f); break;
                case Ink.NavySoft: Style(text, C("2c4a86"), C("243f78"), Color.clear, 0, Color.clear, 0, C("ffffff", .5f), new Vector2(0, -1f), size * .012f); break;
            }
        }

        static void Clean(Text text, float size, Color top, Color bottom, Color edge, float edgeShare, Color shadow, float spacingShare)
        {
            Style(text, top, bottom, Color.clear, 0, edge, size * edgeShare, shadow, new Vector2(0, -Mathf.Max(1.2f, size * .045f)), size * spacingShare);
        }

        // One exact size with no best fit (a best-fit box that is too small draws nothing) and no inherited effects.
        public static void Plain(Text text, float size)
        {
            text.resizeTextForBestFit = false;
            text.fontSize = Mathf.RoundToInt(size);
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            foreach (var effect in text.GetComponents<Shadow>()) effect.enabled = false;   // Outline derives from Shadow
        }

        // Body copy that wraps inside its box instead of overflowing.
        public static Text Wrap(Text text, float width, float height)
        {
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            ((RectTransform)text.transform).sizeDelta = new Vector2(width, height);
            return text;
        }

        public static RoyalTextStyle Style(Text text, Color top, Color bottom, Color rim, float rimWidth, Color outline, float outlineWidth, Color shadow, Vector2 shadowOffset, float spacing = 0)
        {
            var style = text.GetComponent<RoyalTextStyle>();
            if (!style) style = text.gameObject.AddComponent<RoyalTextStyle>();
            style.Set(top, bottom, rim, rimWidth, outline, outlineWidth, shadow, shadowOffset, spacing);
            return style;
        }

        // Thin lines either side of a heading, clear of its text; `reach` caps how far out they go from the centre.
        public static void HeadingLines(Transform parent, Text heading, float y, float length, float reach, Color line, Color light, float x = 0)
        {
            heading.resizeTextForBestFit = false;
            float start = heading.preferredWidth * .5f + 18, end = Mathf.Min(reach, start + length);
            if (end <= start) return;
            float centre = (start + end) * .5f, w = end - start;
            for (int side = -1; side <= 1; side += 2)
            {
                string suffix = side < 0 ? " left" : " right";
                Shape(parent, "Line" + suffix, x + side * centre, y, w, 1.6f, .8f, line, line);
                if (light.a > 0) Shape(parent, "Line light" + suffix, x + side * centre, y - 1.7f, w, 1.1f, .55f, light, light);
            }
        }

        // A section heading on the blue face, with gold lines either side (the "START WITH A BOOST" line).
        public static Text Section(Transform card, string name, string value, float y, float reach = 280)
        {
            var text = Words(card, name, value, 0, y, 520, 24, Ink.Heading);
            var lines = Container(card, name + " lines");
            lines.SetSiblingIndex(text.transform.GetSiblingIndex());
            HeadingLines(lines, text, y - 1.1f, 56, reach, C("fdf08a"), Color.clear);
            return text;
        }

        // ---- Panels ----------------------------------------------------------------------------------------------------

        // The cream panel with its gold border (the Level Start card's goals panel).
        public static Transform Cream(Transform parent, string name, float x, float y, float w, float h, float r = 36)
        {
            var panel = Container(parent, name);
            RoyalMenuSkin.Place(panel, x, y, w, h);
            Inset(panel, "Outline", -.9f, r + .9f, C("2a2141", .85f), C("2a2141", .85f));
            Inset(panel, "Edge", 0, r, C("c07424"), C("a55e1a"));
            Inset(panel, "Gold", 1.2f, r - 1.2f, C("fde27c"), C("e9a52a"));
            Inset(panel, "Inner line", 7.3f, r - 7.3f, C("e6b865"), C("d09a4d"));
            Inset(panel, "Cream", 8.1f, r - 8.1f, C("fef2d4"), C("f5d8a6"));
            var shine = Inset(panel, "Cream shine", 8.1f, r - 8.1f, C("ffffff", .85f), C("ffffff", 0));
            shine.band = 1.3f;
            return panel;
        }

        // A dark-blue squircle tile with a thin gold rim (the Level Start card's booster cards), drawn under `holder`'s
        // content: it becomes holder's first child and fills a `w` x `h` box at the holder's centre. Square tiles are full
        // squircles; a larger, oblong tile passes a smaller `radius` so it stays a rounded card rather than a lozenge.
        public static Transform Tile(Transform holder, float w, float h, string name = "Royal tile", float radius = 0)
        {
            var old = holder.Find(name);
            if (old) Discard(old);
            var tile = Container(holder, name);
            tile.SetSiblingIndex(0);
            var r = (RectTransform)tile;
            r.anchorMin = r.anchorMax = new Vector2(.5f, .5f);
            r.sizeDelta = new Vector2(w, h);
            float half = radius > 0 ? radius : Mathf.Min(w, h) * .5f;
            var shadow = Shape(tile, "Shadow", 0, -4, w + 4, h + 4, half + 2, C("000a24", .55f), C("000a24", .55f), Squircle);
            shadow.softness = 12;
            Inset(tile, "Edge", -.7f, half + .7f, C("231a3b", .9f), C("231a3b", .9f), Squircle);
            Inset(tile, "Rim outer", 0, half, C("c0661e"), C("a04f10"), Squircle);
            Inset(tile, "Rim", 1.2f, half - 1.2f, C("fff0a0"), C("f6c342"), Squircle);
            Inset(tile, "Rim light", 1.2f, half - 1.2f, C("fffbe0", .85f), C("fffbe0", 0), Squircle);
            Inset(tile, "Inner line", 5.9f, half - 5.9f, C("031456"), C("031456"), Squircle);
            Inset(tile, "Face", 7.4f, half - 7.4f, C("1f3db2"), C("0c1a4a"), Squircle);
            var shine = Shape(tile, "Face shine", 0, h * .23f, w * .62f, h * .34f, h * .17f, C("3f63e6", .28f), C("3f63e6", .28f));
            shine.softness = h * .28f;
            return tile;
        }

        // A goal column on a cream panel: the icon over its amount (the Level Start card's goals). `height` is the panel's.
        public static void Goal(TargetIcon target, int index, int count, float panelWidth, float height, float iconBox = 84, float valueSize = 30, float pad = 16, bool markOnIcon = false)
        {
            if (!target) return;
            float pitch = Mathf.Min(175, (panelWidth - 30) / Mathf.Max(1, count));
            float x = (index - (count - 1) * .5f) * pitch;
            RoyalMenuSkin.Place(target.transform, x, 0, pitch - 8, height);
            float iconY = height * .5f - pad - iconBox * .5f, valueY = -height * .5f + pad + valueSize * .45f;
            var icon = target.imageRenderer;
            if (icon)
            {
                // The score star fills only 86% x 81% of its square: give it a larger box so it matches the other icons.
                float box = icon.sprite && icon.sprite == RoyalPieceSkin.Art("star") ? iconBox * 1.17f : iconBox;
                RoyalMenuSkin.Place(icon.transform, 0, iconY + 1.3f, box, box);
                icon.preserveAspect = true;
            }
            // In the goal prefabs the amount and the tick/X are children of the icon, not of the column: their rows are given
            // in the column's space, so the icon's own offset comes off (else they sit 17 units up, over the icon).
            if (target.textObj)
            {
                var text = target.textObj;
                RoyalMenuSkin.Place(text.transform, 0, valueY + valueSize * CapDrop - OffsetIn(target.transform, text.transform), pitch - 14, valueSize * 1.2f);
                if (!text.GetComponent<RoyalTextStyle>())
                {
                    RoyalMenuSkin.Typography(text, Mathf.RoundToInt(valueSize));
                    Plain(text, valueSize);
                    Style(text, C("10306f"), C("0a2260"), Color.clear, 0, Color.clear, 0, C("ffffff", .55f), new Vector2(0, -1.3f));
                }
            }
            // With `markOnIcon` the tick or X sits on the icon's lower-right corner (the fail card keeps the amount under it).
            if (target.checkObject)
            {
                float markY = markOnIcon ? iconY - iconBox * .32f : valueY;
                RoyalMenuSkin.Place(target.checkObject.transform, markOnIcon ? iconBox * .36f : 0, markY - OffsetIn(target.transform, target.checkObject.transform), markOnIcon ? 40 : 34, markOnIcon ? 40 : 34);
                if (markOnIcon) target.checkObject.transform.SetAsLastSibling();
            }
        }

        // How far up a node's parent chain sits inside `column` (the parents' anchored heights, up to the column itself).
        static float OffsetIn(Transform column, Transform node)
        {
            float y = 0;
            for (var p = node.parent; p; p = p.parent)
            {
                if (p == column) return y;
                if (p is RectTransform r) y += r.anchoredPosition.y;
            }
            return 0;   // not inside the column: leave the given row as it is
        }

        // Separators between goal columns on a cream panel (up to three); ShowSeparators() shows as many as the level needs.
        public static Transform Separators(Transform panel, float lineHeight, float alpha = .9f)
        {
            var separators = Container(panel, "Separators");
            for (int s = 0; s < 3; s++)
            {
                var line = Container(separators, "Separator " + (s + 1));
                var rt = (RectTransform)line;
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(.5f, .5f);
                rt.sizeDelta = new Vector2(3, lineHeight);
                Shape(line, "Groove", -.4f, 0, 1.4f, lineHeight, .7f, C("c8a066", alpha), C("c8a066", alpha));
                Shape(line, "Light", .9f, 0, 1.1f, lineHeight, .55f, C("fff6dc", alpha), C("fff6dc", alpha));
                line.gameObject.SetActive(false);
            }
            return separators;
        }

        public static void ShowSeparators(Transform separators, int count, float panelWidth, float y)
        {
            if (!separators) return;
            float pitch = Mathf.Min(175, (panelWidth - 30) / Mathf.Max(1, count));
            for (int s = 0; s < separators.childCount; s++)
            {
                var line = (RectTransform)separators.GetChild(s);
                bool shown = s < count - 1;
                if (line.gameObject.activeSelf != shown) line.gameObject.SetActive(shown);
                if (shown) line.anchoredPosition = new Vector2((s + 1 - count * .5f) * pitch, y);
            }
        }

        // ---- Controls --------------------------------------------------------------------------------------------------

        // Places a control (its rect is the tap area) and removes the inherited look: the squash Animator, the sprite and
        // the original caption. The Button keeps its callback; its colour transition is replaced by the card's motion.
        public static Transform Control(Transform t, float x, float y, float tapSize, float visualSize, float tapHeight = 0)
        {
            if (!t) return null;
            var animator = t.GetComponent<Animator>();
            if (animator) animator.enabled = false;
            var button = t.GetComponent<Button>();
            if (button) button.transition = Selectable.Transition.None;
            var image = t.GetComponent<Image>();
            if (image) image.color = Color.clear;
            foreach (var text in t.GetComponentsInChildren<Text>(true)) text.enabled = false;
            var old = t.Find("Royal skin");
            if (old) Discard(old);
            RoyalMenuSkin.Place(t, x, y, tapSize, tapHeight > 0 ? tapHeight : visualSize > 0 ? tapSize : 120);
            var skin = Container(t, "Royal skin");
            skin.SetAsFirstSibling();
            if (visualSize > 0)
            {
                var r = (RectTransform)skin;
                r.anchorMin = r.anchorMax = new Vector2(.5f, .5f);
                r.sizeDelta = new Vector2(visualSize, visualSize);
            }
            return t;
        }

        // A rectangular button: Primary is the green PLAY face, Secondary the blue tile face; both share the gold rim.
        // The caption is "Royal caption" (the pause menu's toggles rewrite it).
        public static Transform Button(Transform t, string caption, float x, float y, float w, float h, Kind kind, float captionSize = 0)
        {
            if (!t) return null;
            Control(t, x, y, w, 0, h);
            Face(t.Find("Royal skin"), h, kind);
            if (caption != null)
            {
                float size = captionSize > 0 ? captionSize : Mathf.Round(h * (kind == Kind.Primary ? .39f : .34f));
                Words(t, "Royal caption", caption, 0, h * .0125f, w - 24, size, kind == Kind.Primary ? Ink.Caption : Ink.CaptionOnBlue);
            }
            return t;
        }

        // The button face layers, scaled from the 120-unit PLAY button.
        public static void Face(Transform skin, float h, Kind kind)
        {
            float k = Mathf.Clamp(h / 120, .55f, 1), r = h * .395f;
            float orange = .8f * k, gold = 2.6f * k, innerOrange = 8.8f * k, dark = 10.3f * k, face = 12.3f * k;
            Inset(skin, "Edge", 0, r, C("3a2a3e"), C("5a2c10"));
            Inset(skin, "Orange", orange, r - orange, C("d0680e"), C("b85a0c"));
            Inset(skin, "Gold", gold, r - gold, C("ffe06a"), C("e19a1c"));
            Inset(skin, "Gold light", gold, r - gold, C("fffbd8", .75f), C("fffbd8", 0));
            Inset(skin, "Inner orange", innerOrange, r - innerOrange, C("c7650f"), C("a54c05"));
            if (kind == Kind.Primary)
            {
                Inset(skin, "Dark line", dark, r - dark, C("16241a"), C("0c2a1e"));
                Inset(skin, "Green", face, r - face, C("1ea77a"), C("01503f"));
            }
            else
            {
                Inset(skin, "Dark line", dark, r - dark, C("06163e"), C("041030"));
                Inset(skin, "Green", face, r - face, C("2a5cc4"), C("0c2466"));   // "Green" is the face layer's name for every kind
            }
            // The gloss: a lens inside the face's top edge, 10 in from its sides so its corners stay inside the face's.
            float lens = Mathf.Max(8, h * .28f), fr = r - face;
            var gloss = Strip(skin, "Gloss", true, face + 2.5f * k, lens, Mathf.Min(fr * .5f, lens * .5f), C("ffffff", kind == Kind.Primary ? .24f : .2f), C("ffffff", .03f));
            gloss.rectTransform.sizeDelta = new Vector2(-2 * (face + Mathf.Max(6, fr * .29f)), lens);
            var sweep = Container(skin, "Sweep").gameObject.AddComponent<RoyalSweepGraphic>();
            Stretch(sweep.rectTransform, face);
            sweep.radius = fr; sweep.raycastTarget = false; sweep.color = new Color(1, 1, .96f);
        }

        // A price inside a button: its "Price" Text right-aligned against a coin centred at `coinX` from the button's centre.
        public static void Price(Transform button, float coinX, float size, Kind kind)
        {
            if (!button) return;
            var price = button.Find("Price");
            if (price)
            {
                var text = Field(price, coinX - 2.05f * size, 0, 2.6f * size, size, kind == Kind.Primary ? Ink.Caption : Ink.CaptionOnBlue);
                if (text) text.alignment = TextAnchor.MiddleRight;
            }
            var coin = RoyalMenuSkin.Art(button, "Royal coin", "coin", coinX, 0, 1.25f * size, 1.25f * size);
            coin.transform.SetAsLastSibling();
        }

        public static void CloseFace(Transform close)
        {
            var skin = close.Find("Royal skin");
            const float d = 73.5f, r = d * .5f;
            var shadow = Shape(skin, "Shadow", 0, -2.5f, d + 2, d + 2, r + 1, C("000a24", .5f), C("000a24", .5f));
            shadow.softness = 8;
            Inset(skin, "Edge", 0, r, C("5a2e1e"), C("5a2e1e"));
            Inset(skin, "Orange", .9f, r - .9f, C("c9681a"), C("a65226"));
            Inset(skin, "Gold", 2.4f, r - 2.4f, C("ffeea0"), C("f0b43a"));
            Inset(skin, "Inner orange", 7.2f, r - 7.2f, C("c65b12"), C("a44c10"));
            Inset(skin, "Face", 8.6f, r - 8.6f, C("1d4ea8"), C("04194a"));
            Inset(skin, "Face low light", 8.6f, r - 8.6f, C("0d3184", 0), C("0d3184", .5f));
            Inset(skin, "Shine", 8.6f, r - 8.6f, C("2d62c0", .55f), C("2d62c0", 0));
            var glyph = Container(close, "Royal glyph");
            for (int pass = 0; pass < 2; pass++)
                for (int arm = 0; arm < 2; arm++)
                {
                    bool shade = pass == 0;
                    var bar = Shape(glyph, (shade ? "Shadow " : "Arm ") + arm, 0, shade ? -2 : 0, 44, 9, 4.5f,
                        shade ? C("00083a", .7f) : C("fdf3d0"), shade ? C("00083a", .7f) : C("f4d99a"));
                    bar.rectTransform.localRotation = Quaternion.Euler(0, 0, arm == 0 ? 45 : -45);
                }
        }

        public static void HelpFace(Transform info)
        {
            var skin = info.Find("Royal skin");
            const float d = 47, r = d * .5f;
            Inset(skin, "Edge", 0, r, C("5a3a18"), C("5a3a18"));
            Inset(skin, "Ring", .8f, r - .8f, C("ffe07a"), C("e6a02a"));
            Inset(skin, "Inner", 4.4f, r - 4.4f, C("06163e"), C("06163e"));
            Inset(skin, "Face", 5.2f, r - 5.2f, C("1f70c8"), C("03256e"));
            Inset(skin, "Shine", 5.2f, r - 5.2f, C("ffffff", .2f), C("ffffff", 0));
            var mark = Words(info, "Royal caption", "?", 0, 0, 40, 29, Ink.Heading);
            Style(mark, C("fbf6c0"), C("f2cd62"), Color.clear, 0, C("031340"), 1, C("040a2d", .8f), new Vector2(0, -1.5f));
        }

        // A round badge (56 units) with a gold ring: green behind the + and the tick, navy behind a count.
        public static void Badge(Transform t, bool green, float x = 56.2f, float y = -54.4f)
        {
            RoyalMenuSkin.Place(t, x, y, 56, 56);
            var image = t.GetComponent<Image>();
            if (image) image.color = Color.clear;
            foreach (var text in t.GetComponentsInChildren<Text>(true)) if (text.name != "Count") text.enabled = false;
            var old = t.Find("Royal skin");
            if (old) Discard(old);
            var badge = Container(t, "Royal badge");
            badge.SetAsFirstSibling();
            const float r = 28;
            var shadow = Shape(badge, "Shadow", 0, -2.2f, 58, 58, 29, C("001008", .55f), C("001008", .55f));
            shadow.softness = 6;
            Inset(badge, "Edge", 0, r, C("6c3a12"), C("6c3a12"));
            Inset(badge, "Ring", 1, r - 1, C("ffe56e"), C("d68f1c"));
            Inset(badge, "Inner", 4.4f, r - 4.4f, C("00301f"), C("00301f"));
            var face = Inset(badge, "Face", 5.2f, r - 5.2f, C("3cc896"), C("055a45"));
            if (!green) { face.from = C("2656b8"); face.to = C("0a2466"); }
            Inset(badge, "Shine", 5.2f, r - 5.2f, C("ffffff", .26f), C("ffffff", 0));
        }

        // The + (two bars) or the tick (a short and a long bar), in cream with a soft shadow.
        public static void Glyph(Transform parent, string name, bool tick)
        {
            var old = parent.Find(name);
            if (old) Discard(old);
            var glyph = Container(parent, name);
            for (int pass = 0; pass < 2; pass++)
            {
                bool shade = pass == 0;
                Color from = shade ? C("00301f", .6f) : C("fffaea"), to = shade ? C("00301f", .6f) : C("f1e0b6");
                float dy = shade ? -1.2f : 0;
                if (tick)
                {
                    // Strokes from (-10, -.5) down to (-3, -7.5), then up to (10, 7.5); centred on the badge.
                    var shortBar = Shape(glyph, "Short" + pass, -6.5f, -4 + dy, 16.3f, 6.4f, 3.2f, from, to);
                    shortBar.rectTransform.localRotation = Quaternion.Euler(0, 0, -45);
                    var longBar = Shape(glyph, "Long" + pass, 3.5f, 0 + dy, 26.3f, 6.4f, 3.2f, from, to);
                    longBar.rectTransform.localRotation = Quaternion.Euler(0, 0, 49);
                }
                else
                {
                    Shape(glyph, "Across" + pass, 0, dy, 22, 6.6f, 3.3f, from, to);
                    Shape(glyph, "Down" + pass, 0, dy, 6.6f, 22, 3.3f, from, to);
                }
            }
        }

        // ---- Motion wiring ---------------------------------------------------------------------------------------------

        public static RoyalCardControl Feedback(Transform t, bool sink)
        {
            if (!t) return null;
            var c = new RoyalCardControl { sink = sink, button = t.GetComponent<Selectable>() };
            c.press = t.GetComponent<RoyalPressTracker>();
            if (!c.press) c.press = t.gameObject.AddComponent<RoyalPressTracker>();
            var parts = new List<RectTransform>();
            foreach (Transform child in t)
                if (child.name == "Royal skin" || child.name == "Royal caption" || child.name == "Royal glyph" || child.name == "Price" || child.name == "Royal coin" || child.name == "Count" || child.name == "Royal more" || child.name == "Royal divider")
                    parts.Add((RectTransform)child);
            c.parts = parts.ToArray();
            c.rest = new Vector2[c.parts.Length];
            for (int i = 0; i < c.parts.Length; i++) c.rest[i] = c.parts[i].anchoredPosition;
            c.graphics = t.GetComponentsInChildren<Graphic>(true);
            return c;
        }

        public static RoyalCardReveal Reveal(float delay, Transform parent, params string[] names)
        {
            var list = new List<RectTransform>();
            foreach (var n in names) { var t = parent.Find(n) as RectTransform; if (t) list.Add(t); }
            return Reveal(delay, list);
        }

        public static RoyalCardReveal Reveal(float delay, List<RectTransform> rects)
        {
            var reveal = new RoyalCardReveal { delay = delay, rects = rects.ToArray() };
            reveal.groups = new CanvasGroup[reveal.rects.Length];
            reveal.rest = new Vector2[reveal.rects.Length];
            for (int i = 0; i < reveal.rects.Length; i++)
            {
                var g = reveal.rects[i].GetComponent<CanvasGroup>();
                if (!g) g = reveal.rects[i].gameObject.AddComponent<CanvasGroup>();
                reveal.groups[i] = g;
                reveal.rest[i] = reveal.rects[i].anchoredPosition;
            }
            return reveal;
        }

        // ---- Small builders --------------------------------------------------------------------------------------------

        public static Transform Container(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing) return existing;
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Stretch((RectTransform)go.transform, 0);
            return go.transform;
        }

        public static void Stretch(RectTransform r, float inset)
        {
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.pivot = new Vector2(.5f, .5f);
            r.offsetMin = Vector2.one * inset; r.offsetMax = -Vector2.one * inset;
            r.localScale = Vector3.one; r.localRotation = Quaternion.identity;
        }

        static RoyalCardShape NewShape(Transform parent, string name, Color from, Color to, float radius, float exponent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var shape = go.AddComponent<RoyalCardShape>();
            shape.raycastTarget = false;
            shape.from = from; shape.to = to; shape.radius = Mathf.Max(0, radius); shape.exponent = exponent;
            return shape;
        }

        public static RoyalCardShape Shape(Transform parent, string name, float x, float y, float w, float h, float radius, Color from, Color to, float exponent = 2)
        {
            var shape = NewShape(parent, name, from, to, radius, exponent);
            RoyalMenuSkin.Place(shape.transform, x, y, w, h);
            return shape;
        }

        // A layer filling its parent, `inset` in from every edge.
        public static RoyalCardShape Inset(Transform parent, string name, float inset, float radius, Color from, Color to, float exponent = 2)
        {
            var shape = NewShape(parent, name, from, to, radius, exponent);
            Stretch(shape.rectTransform, inset);
            return shape;
        }

        // A layer along the top (or bottom) of its parent, `inset` in from that edge and the sides, `height` tall.
        public static RoyalCardShape Strip(Transform parent, string name, bool top, float inset, float height, float radius, Color from, Color to, float exponent = 2)
        {
            var shape = NewShape(parent, name, from, to, radius, exponent);
            var r = shape.rectTransform;
            r.anchorMin = new Vector2(0, top ? 1 : 0); r.anchorMax = new Vector2(1, top ? 1 : 0); r.pivot = new Vector2(.5f, top ? 1 : 0);
            r.sizeDelta = new Vector2(-2 * inset, height);
            r.anchoredPosition = new Vector2(0, top ? -inset : inset);
            return shape;
        }

        // Destroy runs at the end of the frame, so rename first: a Find for the same name must not return the old object.
        public static void Discard(Transform old)
        {
            old.name += " (replaced)";
            old.gameObject.SetActive(false);
            Object.Destroy(old.gameObject);
        }

        public static void Hide(Transform t)
        {
            if (!t) return;
            var group = t.GetComponent<CanvasGroup>();
            if (!group) group = t.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0; group.blocksRaycasts = false;
        }

        public static Color C(string hex, float alpha = 1)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            c.a = alpha;
            return c;
        }
    }

    // A group of card elements that fade in and rise 8 units together, `delay` seconds into the entrance.
    public sealed class RoyalCardReveal { public float delay; public RectTransform[] rects; public CanvasGroup[] groups; public Vector2[] rest; }

    // A pressable control's feedback: the parts that move (sink) or shrink, and the graphics that darken. A control whose
    // button is not interactable (the offline coin packs) is dimmed instead and does not respond to a press.
    public sealed class RoyalCardControl
    {
        public bool sink;
        public Selectable button;
        public RoyalPressTracker press;
        public RectTransform[] parts;
        public Vector2[] rest;
        public Graphic[] graphics;
        internal float blend;
        internal bool off;
    }
}
