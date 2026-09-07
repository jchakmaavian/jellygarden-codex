// The shapes every Royal Match-style story screen is built from.
//
// The reference is docs/royal-match/ — screenshots pulled off a real device,
// not memory. Three shapes account for nearly all of it:
//
//   RoyalPanel   a blue card with a gold rim, a title plate and a red close X
//   GreenButton  the one affirmative action on a screen
//   TopBar       coins, lives, stars, settings — the same strip on every screen
//
// Every colour below was sampled out of docs/royal-match/02-area-tasks.jpg and
// 10-level-start-popup.jpg with PIL rather than eyeballed, and the proportions
// come from the same two images scaled from the capture's 828x1472 onto the
// canvas's 1080x1920 reference resolution.
//
// Nothing here loads a kit panel or button texture. The kit's UI art has its
// labels painted into the artwork — play_menu_01 says "Level" and "Target:",
// play_button says "Play" — so a borrowed frame puts someone else's words
// behind the story's own. These are drawn instead.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    public static partial class StoryUi
    {
        #region Palette

        // Sampled from docs/royal-match/02-area-tasks.jpg.
        public static readonly Color RoyalBlue = Rgb(25, 95, 167);      // panel body
        public static readonly Color RoyalBlueDark = Rgb(16, 66, 139);  // title plate, shading
        public static readonly Color RoyalBlueDeep = Rgb(1, 85, 131);   // sunken progress track
        public static readonly Color RoyalGold = Rgb(255, 213, 100);    // inner rim highlight
        public static readonly Color RoyalGoldDeep = Rgb(240, 138, 1);  // outer rim
        public static readonly Color RoyalCream = Rgb(250, 234, 208);   // row and card fill
        public static readonly Color RoyalCreamDeep = Rgb(236, 222, 177);
        public static readonly Color RoyalNavy = Rgb(26, 48, 112);      // row label text
        public static readonly Color RoyalTitle = Rgb(250, 247, 205);   // title plate text
        public static readonly Color RoyalGreen = Rgb(101, 163, 2);     // affirmative button
        public static readonly Color RoyalGreenLight = Rgb(166, 211, 84);
        public static readonly Color RoyalGreenDark = Rgb(40, 90, 1);
        public static readonly Color RoyalRed = Rgb(239, 45, 53);       // close X
        public static readonly Color RoyalRedDark = Rgb(176, 26, 34);
        public static readonly Color RoyalGrey = Rgb(150, 148, 142);    // a disabled button
        public static readonly Color RoyalPurple = Rgb(123, 41, 187);     // the area chest
        public static readonly Color RoyalPurpleLight = Rgb(190, 130, 254);
        public static readonly Color RoyalPurpleDark = Rgb(78, 20, 124);
        public static readonly Color RoyalChestGold = Rgb(252, 214, 6);   // chest panels
        public static readonly Color RoyalTealInk = Rgb(4, 55, 74);       // heading outline

        // The area card, sampled pixel by pixel out of
        // docs/royal-match/09-home-level-button.jpg. The card there is 348x157
        // at x446,y1110 of an 828x1472 screenshot, and every colour below is a
        // reading off that image rather than a choice.
        public static readonly Color CardRim = Rgb(240, 120, 10);      // the outer band
        public static readonly Color CardRimDeep = Rgb(200, 69, 0);    // its thickness, below
        public static readonly Color CardRimLit = Rgb(255, 178, 45);   // its lit inner edge
        public static readonly Color CardFace = Rgb(255, 190, 10);     // the face, mid height
        public static readonly Color CardTrack = Rgb(0, 70, 115);      // the bar's well
        public static readonly Color CardFill = Rgb(62, 136, 0);       // the bar's green
        public static readonly Color CardInk = Rgb(253, 247, 225);     // "Area 2" and "1/7"
        public static readonly Color CardInkEdge = Rgb(146, 58, 0);    // the outline under it

        // The areas list. Its ground is a dark slate rather than the royal blue
        // every other screen sits on, because the cards on it are themselves red
        // and gold and would have nothing to sit against. Sampled off
        // docs/royal-match/04-areas-list.jpg.
        public static readonly Color RoyalSlate = Rgb(56, 74, 96);        // the list's ground
        public static readonly Color RoyalSlateLine = Rgb(35, 53, 73);    // between two cards
        public static readonly Color RoyalHeaderBlue = Rgb(1, 70, 147);   // the "Areas" strip
        public static readonly Color RoyalBanner = Rgb(184, 33, 51);      // a card's name ribbon
        public static readonly Color RoyalBannerDeep = Rgb(124, 15, 38);  // its shadowed edge
        public static readonly Color RoyalCardRed = Rgb(181, 13, 28);     // the card frame

        // The task-row icons. Royal Match paints one object per repair; these are
        // sampled off the two it shows in 02-area-tasks.jpg — the rolled carpet
        // and the stained glass — so a drawn glyph belongs to the same picture.
        public static readonly Color IconGold = Rgb(252, 192, 12);      // the carpet's trim
        public static readonly Color IconWood = Rgb(144, 66, 12);       // the glass's frame
        public static readonly Color IconDeepRed = Rgb(108, 12, 0);     // its dark panes
        public static readonly Color IconMagenta = Rgb(192, 24, 144);   // the carpet itself
        public static readonly Color IconGlass = Rgb(40, 235, 234);     // its lit panes

        private static Color Rgb(int r, int g, int b)
        {
            return new Color(r / 255f, g / 255f, b / 255f, 1f);
        }

        #endregion

        #region Drawn sprites

        // RoundedRect in StoryUi.cs caches exactly two radii, which is enough for
        // the older screens and not enough here: a panel, a row, a pill and a
        // badge all want a different corner. One cache, keyed by radius.
        private static readonly Dictionary<int, Sprite> _rounded =
            new Dictionary<int, Sprite>();

        /// <summary>A white 9-sliced rounded rect of the given corner radius.</summary>
        public static Sprite RoundedSprite(int radius)
        {
            Sprite sprite;
            if (_rounded.TryGetValue(radius, out sprite) && sprite != null) return sprite;

            sprite = RoundedRect(radius, Color.white);
            _rounded[radius] = sprite;
            return sprite;
        }

        private static Sprite _circleSprite;
        private static Sprite _gearSprite;
        private static Sprite _sheenSprite;

        /// <summary>
        /// A vertical ramp: white and part-opaque at the top, gone by the middle.
        ///
        /// uGUI cannot fill a rect with a gradient, so the way a flat button
        /// stops looking flat has been to lay a second, lighter rect over its top
        /// half — and that second rect has a hard bottom edge, which is the
        /// two-tone banding this replaces. A ramp fades out instead of stopping,
        /// so there is no edge anywhere on it.
        ///
        /// Laid over a rounded shape it needs that shape to clip it, or its own
        /// square top corners show; the caller puts a Mask on the field beneath.
        /// </summary>
        public static Sprite SheenSprite
        {
            get
            {
                if (_sheenSprite == null) _sheenSprite = DrawSheen(64);
                return _sheenSprite;
            }
        }

        /// <summary>
        /// The ramp itself. Four pixels wide because nothing varies across it;
        /// the height is what carries the falloff, and it is squared so the
        /// brightest part stays near the top edge rather than washing the middle.
        /// </summary>
        private static Sprite DrawSheen(int height)
        {
            const int width = 4;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.hideFlags = HideFlags.HideAndDontSave;

            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                // Texture rows run bottom-up; the sheen belongs at the top.
                float down = 1f - (float)y / (height - 1);
                float fall = Mathf.Clamp01(1f - down * 2.2f);
                byte alpha = (byte)Mathf.RoundToInt(fall * fall * 255f);
                for (int x = 0; x < width; x++)
                    pixels[y * width + x] = new Color32(255, 255, 255, alpha);
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height),
                new Vector2(0.5f, 0.5f), 100f);
            sprite.name = "StorySheen";
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        /// <summary>A white filled circle, tinted by the caller.</summary>
        public static Sprite CircleSprite
        {
            get
            {
                if (_circleSprite == null) _circleSprite = DrawCircle(64);
                return _circleSprite;
            }
        }

        /// <summary>The settings gear, drawn rather than borrowed — the kit has no loose one.</summary>
        public static Sprite GearSprite
        {
            get
            {
                if (_gearSprite == null) _gearSprite = DrawGear(64, 8);
                return _gearSprite;
            }
        }

        private static Texture2D NewTexture(int size)
        {
            return new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        private static Sprite Finish(Texture2D texture, int size)
        {
            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f), 100f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        private static Sprite DrawCircle(int radius)
        {
            int size = radius * 2;
            Texture2D texture = NewTexture(size);
            var pixels = new Color32[size * size];
            float centre = (size - 1) * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Mathf.Sqrt((x - centre) * (x - centre) +
                                                (y - centre) * (y - centre));
                    // One pixel of feather, so the rim is not stair-stepped.
                    float alpha = Mathf.Clamp01(radius - 0.5f - distance);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels32(pixels);
            return Finish(texture, size);
        }

        private static Sprite _bannerSprite;

        /// <summary>
        /// The curved title banner every Royal panel wears.
        ///
        /// Measured off docs/royal-match/05-earn-stars.jpg (828x1472): the
        /// plate's top edge sits at y=252 at the left end and rises to y=195 in
        /// the middle — a 57px lift over a 390px half-width, on a plate 170px
        /// tall. That is a third of the plate's height, and it is the single
        /// thing that most makes their popups read as theirs rather than as a
        /// generic blue card. A flat slab with two gold stripes across it, which
        /// is what this replaces, gets every colour right and still looks like a
        /// different game.
        ///
        /// It is one alpha shape, stacked three times at decreasing insets to
        /// make the blue shell, the gold band and the dark field — so the
        /// colours stay named in code rather than baked into a texture, and the
        /// gold reads as a band top AND bottom without drawing either.
        /// </summary>
        public static Sprite BannerSprite
        {
            get
            {
                if (_bannerSprite == null) _bannerSprite = DrawBanner(256, 128);
                return _bannerSprite;
            }
        }

        // How far the banner's ends hang below its middle, as a fraction of its
        // height. Off the reference: the top edge runs 195 at the centre to 252
        // at the end and the BOTTOM edge runs 360 to 405 — both bow the same
        // way, by about 50 on a plate 210 tall.
        //
        // Both edges bowing the same way is the whole shape. Bowed apart, which
        // is the obvious reading of "curved banner" and what this was first
        // written as, gives a lens — a shape with no thickness at its ends that
        // looks like a rugby ball, not like a ribbon draped over a card.
        public const float BannerArc = 0.24f;

        // The corner rounding at the banner's ends, in texture pixels.
        private const float BannerCorner = 22f;

        private static Sprite DrawBanner(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color32[width * height];
            float arc = height * BannerArc;

            // Constant thickness: the band is the rect's height less the travel,
            // and the whole band slides DOWN the parabola rather than opening up
            // around it.
            float thickness = height - arc;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // A parabola that is 0 in the middle and `arc` at either end.
                    float u = (x + 0.5f) / width * 2f - 1f;
                    float drop = u * u * arc;

                    float top = height - drop;
                    float bottom = top - thickness;

                    // Distance inside the shape on each axis, in pixels. The
                    // corners are rounded by treating the two distances as a
                    // radius once both are inside the corner box — the same
                    // trick RoundedSprite uses, on curved edges.
                    float dy = Mathf.Min(top - y, y - bottom);
                    float dx = Mathf.Min(x + 0.5f, width - x - 0.5f);

                    float a;
                    if (dx >= BannerCorner || dy >= BannerCorner)
                    {
                        a = Mathf.Clamp01(Mathf.Min(dx, dy));
                    }
                    else
                    {
                        float cx = BannerCorner - dx;
                        float cy = BannerCorner - dy;
                        a = Mathf.Clamp01(BannerCorner - Mathf.Sqrt(cx * cx + cy * cy));
                    }

                    pixels[y * width + x] = new Color(1f, 1f, 1f, a);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            // No border: the arc cannot be sliced, so the sprite is stretched
            // whole. Every panel is the same width, so the curve never distorts
            // between one screen and the next.
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height),
                new Vector2(0.5f, 0.5f), 100f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        /// <summary>
        /// A gear: a disc with <paramref name="teeth"/> square teeth around it and
        /// a hole through the middle. Drawn in polar coordinates so the tooth
        /// count is a parameter rather than a shape someone has to redraw.
        /// </summary>
        private static Sprite DrawGear(int size, int teeth)
        {
            Texture2D texture = NewTexture(size);
            var pixels = new Color32[size * size];
            float centre = (size - 1) * 0.5f;

            float outer = centre;
            float root = centre * 0.74f;
            float hole = centre * 0.30f;
            float toothHalfWidth = Mathf.PI / teeth * 0.34f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - centre;
                    float dy = y - centre;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);

                    float angle = Mathf.Atan2(dy, dx);
                    float toothPhase = Mathf.Repeat(angle, 2f * Mathf.PI / teeth);
                    float fromToothCentre = Mathf.Abs(toothPhase - Mathf.PI / teeth);

                    float edge = fromToothCentre < toothHalfWidth ? outer : root;

                    float alpha = Mathf.Clamp01(edge - 0.5f - distance) *
                                  Mathf.Clamp01(distance - hole);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels32(pixels);
            return Finish(texture, size);
        }

        #endregion

        #region Panel

        /// <summary>
        /// The parts of a RoyalPanel a caller actually needs. Handed back as an
        /// object rather than found by name later, so a renamed child cannot
        /// silently break a screen.
        /// </summary>
        public sealed class RoyalPanelParts
        {
            /// <summary>The whole panel, title plate included.</summary>
            public RectTransform Root;

            /// <summary>The blue body below the title plate.</summary>
            public RectTransform Body;

            /// <summary>Where a screen puts its rows. Inset inside the gold rim.</summary>
            public RectTransform Content;

            public Text Title;

            /// <summary>The red X. Null when the panel was built without a close action.</summary>
            public Button Close;
        }

        // Reference-resolution metrics, from the capture scaled 828 -> 1080.
        private const float PanelSideMargin = 26f;
        private const float PanelTitleHeight = 210f;
        private const float PanelRim = 14f;

        // The blue shell outside the gold, and the dark hairline inside it.
        // Both measured off docs/royal-match/05-earn-stars.jpg and scaled from
        // its 828 to this canvas's 1080.
        private const float PanelShell = 21f;
        private const float PanelHairline = 8f;
        private const float PanelContentPad = 26f;

        /// <summary>
        /// A blue panel with a gold rim, a title plate across its top and a red
        /// round close X sitting on the plate's right-hand corner.
        ///
        /// The panel stretches to fill <paramref name="parent"/> inside the
        /// margins Royal Match uses; a caller wanting a shorter card moves
        /// <see cref="RoyalPanelParts.Root"/> afterwards.
        /// </summary>
        public static RoyalPanelParts RoyalPanel(string name, Transform parent, string title,
            UnityAction onClose)
        {
            var parts = new RoyalPanelParts();

            RectTransform root = CreateRect(name, parent);
            Stretch(root, PanelSideMargin);
            parts.Root = root;

            // The body starts half-way down the title plate, so the plate reads as
            // sitting on top of the card rather than floating above it.
            RectTransform body = CreateRect("Body", root);
            body.anchorMin = Vector2.zero;
            body.anchorMax = Vector2.one;
            body.offsetMin = Vector2.zero;
            body.offsetMax = new Vector2(0f, -PanelTitleHeight * 0.55f);
            parts.Body = body;

            // Four rects, outside in, and the order came off a pixel scan across
            // docs/royal-match/05-earn-stars.jpg at y=800: blue shell for 16px,
            // gold for 24, a dark blue hairline for 6, then the body's own blue.
            // Scaled by 1080/828 that is 21, 31 and 8 reference points.
            //
            // The outer BLUE is the part that was missing. A gold rim running
            // straight to the edge of the card reads as a picture frame; the
            // blue outside it is what makes the gold read as inlaid into a blue
            // shell, which is the same move the splash's Play button makes.
            //
            // Nested rects rather than an Outline component, because Outline
            // offsets the whole quad and leaves the corners square.
            Image bodyShell = CreateImage("BodyShell", body, RoundedSprite(48), RoyalBlue);
            Stretch(bodyShell.rectTransform);
            bodyShell.type = Image.Type.Sliced;

            Image bodyRim = CreateImage("BodyRim", body, RoundedSprite(44), RoyalGoldDeep);
            Stretch(bodyRim.rectTransform, PanelShell);
            bodyRim.type = Image.Type.Sliced;

            Image bodyHighlight = CreateImage("BodyRimHighlight", body, RoundedSprite(42), RoyalGold);
            Stretch(bodyHighlight.rectTransform, PanelShell + PanelRim * 0.35f);
            bodyHighlight.type = Image.Type.Sliced;

            Image bodyInk = CreateImage("BodyInk", body, RoundedSprite(40), RoyalBlueDark);
            Stretch(bodyInk.rectTransform, PanelShell + PanelRim);
            bodyInk.type = Image.Type.Sliced;

            Image bodyFill = CreateImage("BodyFill", body, RoundedSprite(38), RoyalBlue);
            Stretch(bodyFill.rectTransform, PanelShell + PanelRim + PanelHairline);
            bodyFill.type = Image.Type.Sliced;
            // The panel eats taps so a click that misses a row does not fall
            // through to whatever screen is behind it.
            bodyFill.raycastTarget = true;

            RectTransform content = CreateRect("Content", body);
            content.anchorMin = Vector2.zero;
            content.anchorMax = Vector2.one;
            content.offsetMin = new Vector2(PanelContentPad, PanelContentPad);
            content.offsetMax = new Vector2(-PanelContentPad,
                -(PanelTitleHeight * 0.45f + PanelContentPad));
            parts.Content = content;

            // The title plate: a curved banner, not a slab.
            //
            // The same three layers as the body, on the arced shape — blue
            // shell, gold, dark blue field — so the gold reads as a band along
            // the top AND the bottom of the banner without either being drawn.
            // Two straight gold stripes across a rounded rect is what this
            // replaces, and it had every colour right and the wrong silhouette.
            RectTransform plate = CreateRect("TitlePlate", root);
            TopAnchored(plate, 0f, PanelTitleHeight, -18f);

            Image plateShell = CreateImage("PlateShell", plate, BannerSprite, RoyalBlue);
            Stretch(plateShell.rectTransform);

            Image plateRim = CreateImage("PlateRim", plate, BannerSprite, RoyalGoldDeep);
            Stretch(plateRim.rectTransform, PanelShell);

            Image plateGold = CreateImage("PlateGold", plate, BannerSprite, RoyalGold);
            Stretch(plateGold.rectTransform, PanelShell + PanelRim * 0.35f);

            Image plateFill = CreateImage("PlateFill", plate, BannerSprite, RoyalBlueDark);
            Stretch(plateFill.rectTransform, PanelShell + PanelRim + PanelHairline);
            plateFill.raycastTarget = true;

            // Inset wide but not tall: 110 clears the close X on the right, and
            // the plate is only PanelTitleHeight tall, so the same inset applied
            // vertically leaves the text a rect of negative height — which draws
            // nothing at all, and reads as a panel that forgot its own name.
            Text titleText = CreateText("Title", plate, title, 76, RoyalTitle,
                TextAnchor.MiddleCenter);
            RectTransform titleRect = titleText.rectTransform;

            // The banner's field sits in the TOP of the plate rect — the band
            // slides down the arc, so the bottom BannerArc of the rect is empty
            // at the middle and only filled towards the ends. Text centred in
            // the whole rect sits below the gold.
            titleRect.anchorMin = new Vector2(0f, BannerArc);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = new Vector2(110f, 20f);
            titleRect.offsetMax = new Vector2(-110f, -24f);
            titleText.fontStyle = FontStyle.Bold;

            // The 150 area names run from "The Mint" to "Grimm's Childhood
            // Room", and the arc has taken height out of the box they sit in.
            // One line, shrunk to fit: best-fit on its own wraps first.
            FitOneLine(titleText, 34, 76);
            var titleOutline = titleText.gameObject.AddComponent<Outline>();
            titleOutline.effectColor = RoyalBlueDark;
            titleOutline.effectDistance = new Vector2(3f, -3f);
            parts.Title = titleText;

            if (onClose != null)
            {
                parts.Close = CloseButton(plate, onClose);

                // Down onto the banner's right end, which the arc has dropped.
                // Anchored to the plate's corner the X floated above the gold
                // with nothing under it.
                RectTransform closeRect = parts.Close.GetComponent<RectTransform>();
                closeRect.anchoredPosition = new Vector2(-6f,
                    34f - PanelTitleHeight * BannerArc);
            }

            return parts;
        }

        /// <summary>The red round X, anchored to the top-right corner of its parent.</summary>
        public static Button CloseButton(Transform parent, UnityAction onClose)
        {
            RectTransform rect = CreateRect("Close", parent);
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(128f, 128f);
            rect.anchoredPosition = new Vector2(-6f, 34f);

            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = CircleSprite;
            image.color = RoyalRedDark;

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClose);

            Image face = CreateImage("Face", rect, CircleSprite, RoyalRed);
            Stretch(face.rectTransform, 9f);

            // The X: two bars crossed, rather than a glyph the kit's font may not
            // carry. A missing glyph renders as nothing at all, which reads as a
            // dead button.
            for (int i = 0; i < 2; i++)
            {
                Image bar = CreateImage("Bar" + i, rect, RoundedSprite(8), RoyalTitle);
                bar.type = Image.Type.Sliced;
                RectTransform barRect = bar.rectTransform;
                barRect.anchorMin = new Vector2(0.5f, 0.5f);
                barRect.anchorMax = new Vector2(0.5f, 0.5f);
                barRect.pivot = new Vector2(0.5f, 0.5f);
                barRect.sizeDelta = new Vector2(62f, 16f);
                barRect.anchoredPosition = Vector2.zero;
                barRect.localRotation = Quaternion.Euler(0f, 0f, i == 0 ? 45f : -45f);
            }

            return button;
        }

        /// <summary>
        /// A cream sub-card — the "Area Progress" box, the Goal box, a task row.
        /// Cream on blue is how Royal Match separates content from chrome.
        /// </summary>
        public static Image CreamCard(string name, Transform parent, int radius = 28)
        {
            Image edge = CreateImage(name, parent, RoundedSprite(radius), RoyalCreamDeep);
            edge.type = Image.Type.Sliced;
            edge.raycastTarget = true;

            Image fill = CreateImage("Fill", edge.rectTransform, RoundedSprite(radius), RoyalCream);
            Stretch(fill.rectTransform, 5f);
            fill.type = Image.Type.Sliced;

            return edge;
        }

        #endregion

        #region Progress bar and chest

        /// <summary>The parts of an area-progress bar a screen has to update.</summary>
        public sealed class ProgressBarParts
        {
            /// <summary>The whole track, orange rim included.</summary>
            public RectTransform Root;

            /// <summary>The green bar inside it. Its width is driven by SetProgress.</summary>
            public RectTransform Fill;

            /// <summary>The "3/7" written across the middle of the track.</summary>
            public Text Label;

            /// <summary>What the bar is currently reading, 0..1.</summary>
            public float Fraction;
        }

        /// <summary>
        /// A plain progress track: a dark well, a green bar growing out of its
        /// left end, and the count across the middle.
        ///
        /// It had an orange rim around the well and a lighter green gloss inside
        /// the fill — four nested rounded rects for a bar 20 points tall. At that
        /// size none of it read as depth; it read as a fussy edge round a small
        /// object, and it fought the card it sits on. The green is still the
        /// reference's green.
        /// </summary>
        public static ProgressBarParts ProgressBar(string name, Transform parent)
        {
            var parts = new ProgressBarParts();

            RectTransform root = CreateRect(name, parent);
            parts.Root = root;

            // A small radius, not a capsule. At 40 the corners were half the
            // track's own height, so a thin bar came out a lozenge; the fill
            // inside it keeps a slightly tighter one so it sits in the well
            // rather than tracing it.
            Image well = CreateImage("Well", root, RoundedSprite(14), CardTrack);
            Stretch(well.rectTransform);
            well.type = Image.Type.Sliced;

            // The fill is anchored rather than sized: a fraction of the track's
            // width survives whatever width the screen ends up handing the track.
            Image fill = CreateImage("Fill", well.rectTransform, RoundedSprite(10), CardFill);
            fill.type = Image.Type.Sliced;
            RectTransform fillRect = fill.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.offsetMin = new Vector2(4f, 4f);
            fillRect.offsetMax = new Vector2(-4f, -4f);
            parts.Fill = fillRect;

            // The green in the reference is lit across its top third. One
            // rectangle, clipped by the fill so its square corners stay inside
            // the rounded end.
            fill.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            Image lit = CreateImage("Lit", fillRect, RoundedSprite(8), RoyalGreenLight);
            lit.type = Image.Type.Sliced;
            RectTransform litRect = lit.rectTransform;
            litRect.anchorMin = new Vector2(0f, 0.52f);
            litRect.anchorMax = new Vector2(1f, 0.94f);
            litRect.offsetMin = new Vector2(3f, 0f);
            litRect.offsetMax = new Vector2(-3f, 0f);
            lit.raycastTarget = false;

            // Over the bar, not inside it: the count has to stay legible whether
            // the green has reached it or not.
            // Best fit, but capped below the track's own height: left at 52 the
            // count grew until it touched the well's top and bottom edges and
            // the bar read as a label with a bar behind it.
            Text label = CreateText("Value", root, string.Empty, 40, CardInk,
                TextAnchor.MiddleCenter);
            Stretch(label.rectTransform, 4f);
            label.fontStyle = FontStyle.Bold;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 16;
            label.resizeTextMaxSize = 40;
            parts.Label = label;

            SetProgress(parts, 0, 0);
            return parts;
        }

        /// <summary>Points a progress bar at a count. Total 0 reads as an empty bar.</summary>
        public static void SetProgress(ProgressBarParts parts, int done, int total)
        {
            if (parts == null) return;

            float fraction = total > 0 ? Mathf.Clamp01((float)done / total) : 0f;
            parts.Fraction = fraction;

            if (parts.Fill != null)
            {
                parts.Fill.anchorMax = new Vector2(fraction, 1f);
                // A rounded fill of zero width still draws both of its end caps,
                // which reads as one step done that has not been done.
                parts.Fill.gameObject.SetActive(fraction > 0.001f);
            }

            if (parts.Label != null) parts.Label.text = done + "/" + total;
        }

        /// <summary>
        /// The area chest that sits on the end of the progress bar: a purple chest
        /// with gold panels and a lock, composed from rounded rects. The kit ships
        /// no chest and the reference's is a painted 3D asset, so this is the flat
        /// reading of it — colours sampled from docs/royal-match/02-area-tasks.jpg.
        /// </summary>
        public static RectTransform ChestIcon(string name, Transform parent)
        {
            return ChestIcon(name, parent, RoyalChestGold);
        }

        /// <summary>
        /// The chest with its panels in a named colour. The default gold is the
        /// reference's, and it is right on the cream card of the area page — but
        /// on the home screen's gold card the same gold is the card's own fill,
        /// and the chest collapses to a pair of purple rings with nothing in
        /// them. A caller drawing it on gold passes something else.
        /// </summary>
        public static RectTransform ChestIcon(string name, Transform parent, Color panel)
        {
            RectTransform root = CreateRect(name, parent);

            // A painted chest where one exists. The drawn one below is six
            // rounded rectangles and a dot, which was fine when everything
            // around it was drawn too and is not fine sitting next to a painted
            // nav bar — it is kept so that deleting the art gives it back, and
            // so `panel` still means something on a build with no art in it.
            Sprite art = StoryArt.Load("icon_chest");
            if (art != null)
            {
                Image painted = CreateImage("Art", root, art, Color.white);
                painted.preserveAspect = true;
                Stretch(painted.rectTransform);
                return root;
            }

            Image lid = CreateImage("Lid", root, RoundedSprite(44), RoyalPurple);
            lid.type = Image.Type.Sliced;
            FrameRect(lid.rectTransform, 0f, 0.44f, 1f, 1f);

            Image lidFace = CreateImage("LidFace", lid.rectTransform, RoundedSprite(36),
                panel);
            lidFace.type = Image.Type.Sliced;
            FrameRect(lidFace.rectTransform, 0.14f, 0.12f, 0.86f, 0.84f);

            Image shine = CreateImage("Shine", lid.rectTransform, RoundedSprite(16),
                RoyalPurpleLight);
            shine.type = Image.Type.Sliced;
            FrameRect(shine.rectTransform, 0.04f, 0.62f, 0.11f, 0.9f);

            Image body = CreateImage("Body", root, RoundedSprite(20), RoyalPurple);
            body.type = Image.Type.Sliced;
            FrameRect(body.rectTransform, 0.03f, 0f, 0.97f, 0.46f);

            Image bodyFace = CreateImage("BodyFace", body.rectTransform, RoundedSprite(16),
                panel);
            bodyFace.type = Image.Type.Sliced;
            FrameRect(bodyFace.rectTransform, 0.07f, 0.2f, 0.93f, 0.82f);

            Image plate = CreateImage("Lock", root, RoundedSprite(18), RoyalPurple);
            plate.type = Image.Type.Sliced;
            FrameRect(plate.rectTransform, 0.37f, 0.24f, 0.63f, 0.62f);

            Image keyPlate = CreateImage("Keyhole", plate.rectTransform, CircleSprite,
                panel);
            FrameRect(keyPlate.rectTransform, 0.16f, 0.16f, 0.84f, 0.84f);

            Image key = CreateImage("Key", keyPlate.rectTransform, CircleSprite, RoyalPurpleDark);
            FrameRect(key.rectTransform, 0.3f, 0.3f, 0.7f, 0.7f);

            return root;
        }

        /// <summary>Anchors a rect to a fraction-box of its parent, with no offsets.</summary>
        private static void FrameRect(RectTransform rect, float minX, float minY,
            float maxX, float maxY)
        {
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        #endregion

        #region Buttons and badges

        /// <summary>
        /// The green affirmative button — "Do It", "Play". Built as a dark base
        /// with a lighter face inset from its bottom edge, which is the whole of
        /// what makes the reference art look pressable.
        /// </summary>
        public static Button GreenButton(string name, Transform parent, string label,
            int fontSize, UnityAction onClick)
        {
            RectTransform rect = CreateRect(name, parent);

            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = RoundedSprite(28);
            image.type = Image.Type.Sliced;
            image.color = RoyalGreenDark;

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            if (onClick != null) button.onClick.AddListener(onClick);

            Image face = CreateImage("Face", rect, RoundedSprite(28), RoyalGreen);
            face.type = Image.Type.Sliced;
            RectTransform faceRect = face.rectTransform;
            faceRect.anchorMin = Vector2.zero;
            faceRect.anchorMax = Vector2.one;
            faceRect.offsetMin = new Vector2(7f, 13f);
            faceRect.offsetMax = new Vector2(-7f, -7f);

            // The light on the face is a ramp that fades out, not a lighter
            // rectangle laid over the top half.
            //
            // It was the rectangle, and it stopped dead across the middle of the
            // button — exactly where the eye lands to read the word on it — so
            // the button read as two slabs stacked rather than as one lit
            // object. Same fault, same fix as the splash's Play button, which is
            // where SheenSprite came from.
            //
            // The ramp is a rectangle and the face is rounded, so the face clips
            // it: without the mask its square top corners show past the curve.
            // showMaskGraphic keeps the green itself drawn.
            face.gameObject.AddComponent<Mask>().showMaskGraphic = true;

            Image sheen = CreateImage("Sheen", faceRect, SheenSprite,
                new Color(1f, 1f, 1f, 0.30f));
            Stretch(sheen.rectTransform);
            sheen.raycastTarget = false;

            // The label sits in its own rect so a star badge can be laid beside it.
            Text text = CreateText("Label", rect, label, fontSize, RoyalTitle,
                TextAnchor.MiddleCenter);
            Stretch(text.rectTransform, 10f);
            text.fontStyle = FontStyle.Bold;
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = RoyalGreenDark;
            outline.effectDistance = new Vector2(2f, -2f);

            return button;
        }

        /// <summary>
        /// Greys a green button out without hiding it. Royal Match leaves a task
        /// you cannot afford on screen and dulls it, so the price is still legible.
        /// </summary>
        public static void SetGreenInteractable(Button button, bool interactable)
        {
            if (button == null) return;
            button.interactable = interactable;

            var baseImage = button.targetGraphic as Image;
            Transform face = button.transform.Find("Face");

            // A PAINTED button has no Face child: it is one sprite, and its
            // colours are in the drawing. Repainting its graphic green — which
            // is what this did to every button it was handed — turned the gold
            // Level button into a green blob the moment the screen refreshed.
            // A painted button greys by tint instead, which keeps its own light
            // and shade.
            if (face == null)
            {
                if (baseImage != null)
                    baseImage.color = interactable ? Color.white : RoyalGrey;
                return;
            }

            if (baseImage != null)
                baseImage.color = interactable ? RoyalGreenDark : Rgb(90, 88, 84);

            var faceImage = face.GetComponent<Image>();
            if (faceImage != null) faceImage.color = interactable ? RoyalGreen : RoyalGrey;

            // The old "Gloss" child was a lighter GREEN rectangle and had to be
            // recoloured with the rest of the button. The sheen that replaced it
            // is white at a fixed alpha, so it lights a grey face exactly as
            // well as a green one and is deliberately left alone here.
        }

        /// <summary>
        /// The star-cost badge: the kit's star sprite with the cost beside it, as
        /// on "Do It ★1". Returned as a rect so the caller places it; the star and
        /// the number are laid out inside a fixed 96x64 box.
        /// </summary>
        public static RectTransform StarBadge(string name, Transform parent, int cost,
            int fontSize = 44)
        {
            RectTransform rect = CreateRect(name, parent);
            rect.sizeDelta = new Vector2(96f, 64f);

            Image star = CreateImage("Star", rect, StarSprite, Color.white);
            RectTransform starRect = star.rectTransform;
            starRect.anchorMin = new Vector2(0f, 0.5f);
            starRect.anchorMax = new Vector2(0f, 0.5f);
            starRect.pivot = new Vector2(0f, 0.5f);
            starRect.sizeDelta = new Vector2(58f, 58f);
            starRect.anchoredPosition = Vector2.zero;
            // No star sprite in Resources yet is a warning, not a blank badge.
            if (star.sprite == null) star.color = RoyalGold;

            Text count = CreateText("Count", rect, cost.ToString(), fontSize, RoyalTitle,
                TextAnchor.MiddleLeft);
            RectTransform countRect = count.rectTransform;
            countRect.anchorMin = new Vector2(0f, 0f);
            countRect.anchorMax = new Vector2(1f, 1f);
            countRect.offsetMin = new Vector2(62f, 0f);
            countRect.offsetMax = Vector2.zero;
            count.fontStyle = FontStyle.Bold;
            var outline = count.gameObject.AddComponent<Outline>();
            outline.effectColor = RoyalGreenDark;
            outline.effectDistance = new Vector2(2f, -2f);

            return rect;
        }

        /// <summary>A "Do It" button carrying the task's own star price.</summary>
        public static Button DoItButton(string name, Transform parent, string label,
            int cost, UnityAction onClick)
        {
            Button button = GreenButton(name, parent, "", 46, onClick);

            // Label and badge share the button, so the two are laid out together
            // rather than the label being centred and the badge overlapping it.
            Text text = button.GetComponentInChildren<Text>();
            text.text = label;
            text.alignment = TextAnchor.MiddleLeft;
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(30f, 0f);
            textRect.offsetMax = new Vector2(-104f, 0f);

            RectTransform badge = StarBadge("Cost", button.transform, cost);
            badge.anchorMin = new Vector2(1f, 0.5f);
            badge.anchorMax = new Vector2(1f, 0.5f);
            badge.pivot = new Vector2(1f, 0.5f);
            badge.anchoredPosition = new Vector2(-18f, 0f);

            return button;
        }

        /// <summary>
        /// The area card's plate, built to docs/royal-match/09-home-level-button.jpg.
        ///
        /// Three bands, outside in, each one a colour read off that screenshot:
        /// a deep-orange rim, a lit edge inside it, and the golden face. The
        /// face carries the same fading sheen every other lit thing on this
        /// screen wears, which is what the reference's paler top is — the card
        /// is one gold that catches more light at the top, not two golds.
        ///
        /// Its own builder rather than GoldButton, because GoldButton is the
        /// generic shape a caller stacks things on and this is one specific
        /// card whose every measurement comes from one image.
        /// </summary>
        /// <summary>How much of the card's dark side shows below it, in points.</summary>
        private const float RimThickness = 7f;

        public static Button CardPlate(string name, Transform parent, UnityAction onClick)
        {
            RectTransform rect = CreateRect(name, parent);

            // The card is a slab seen slightly from above, and this is the side
            // of it: the deepest orange, showing only along the bottom because
            // that is the only side of a slab you can see from up here. It is
            // the whole of what lifts the card off the screen — the reference's
            // bottom edge is a full 7 points darker than its rim.
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = RoundedSprite(30);
            image.type = Image.Type.Sliced;
            image.color = CardRimDeep;

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            if (onClick != null) button.onClick.AddListener(onClick);

            // The top face of the slab, sitting on that side.
            Image rim = CreateImage("Rim", rect, RoundedSprite(30), CardRim);
            rim.type = Image.Type.Sliced;
            rim.raycastTarget = false;
            RectTransform rimRect = rim.rectTransform;
            rimRect.anchorMin = Vector2.zero;
            rimRect.anchorMax = Vector2.one;
            rimRect.offsetMin = new Vector2(0f, RimThickness);
            rimRect.offsetMax = Vector2.zero;

            Image lit = CreateImage("RimLit", rimRect, RoundedSprite(28), CardRimLit);
            lit.type = Image.Type.Sliced;
            Stretch(lit.rectTransform, 5f);
            lit.raycastTarget = false;

            Image face = CreateImage("Face", rimRect, RoundedSprite(26), CardFace);
            face.type = Image.Type.Sliced;
            Stretch(face.rectTransform, 11f);
            face.raycastTarget = false;

            // The ramp is a rectangle and the face is rounded, so the face clips
            // it; showMaskGraphic keeps the gold itself drawn.
            face.gameObject.AddComponent<Mask>().showMaskGraphic = true;

            Image sheen = CreateImage("Sheen", face.rectTransform, SheenSprite,
                new Color(1f, 1f, 1f, 0.30f));
            Stretch(sheen.rectTransform);
            sheen.raycastTarget = false;

            return button;
        }

        /// <summary>
        /// A button that is one painted sprite, with a word laid on it.
        ///
        /// GreenButton and GoldButton below assemble a button out of primitives,
        /// which is right where no art exists for it. This one is the splash's
        /// PLAY button with the word taken out — the same gold face, gloss and
        /// bottom band — sliced so the ends keep their curve and only the flat
        /// middle stretches to the width the caller gives it.
        ///
        /// Returns null where the sprite is not on disk, so a caller can fall
        /// back to the drawn button rather than shipping an invisible one.
        /// </summary>
        public static Button PaintedButton(string name, Transform parent, string label,
            int fontSize, UnityAction onClick)
        {
            return PaintedButton(name, parent, label, fontSize, onClick, PaintedButtonSprite);
        }

        /// <summary>
        /// The same button on a named plate: <c>button_gold</c> is the splash's
        /// PLAY pill, <c>card_gold</c> the amber card the area slot wears.
        /// </summary>
        public static Button PaintedButton(string name, Transform parent, string label,
            int fontSize, UnityAction onClick, string spriteKey)
        {
            Sprite art = LoadSprite(spriteKey);
            if (art == null) return null;

            RectTransform rect = CreateRect(name, parent);

            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = art;
            image.type = Image.Type.Sliced;
            image.color = Color.white;

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            if (onClick != null) button.onClick.AddListener(onClick);

            // Cream in the display face with a brown edge under it — the same
            // treatment the word PLAY wears on the button this was cut from, so
            // the two read as the same object with different words on it.
            Text text = CreateText("Label", rect, label, fontSize, RoyalTitle,
                TextAnchor.MiddleCenter);
            text.font = DisplayFont;
            text.raycastTarget = false;
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            // Clear of the rounded ends, and off the bottom band.
            textRect.offsetMin = new Vector2(28f, 16f);
            textRect.offsetMax = new Vector2(-28f, -6f);
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = IconWood;
            outline.effectDistance = new Vector2(3f, -3f);

            return button;
        }

        /// <summary>The painted button's art in Resources/Story/UI.</summary>
        private const string PaintedButtonSprite = "button_gold";

        /// <summary>The amber card plate the area slot wears.</summary>
        public const string PaintedCardSprite = "card_gold";

        /// <summary>
        /// The gold secondary button from docs/royal-match/09-home-level-button.jpg
        /// — the one carrying "Area 2", its progress bar and its chest. Built like
        /// GreenButton (dark base, brighter face inset from the bottom edge, gloss
        /// across the top) so the two sit beside each other as one pair, and left
        /// empty: what goes on it is the caller's, because the only thing this
        /// shape is ever asked to hold is a small stack of other primitives.
        /// </summary>
        public static Button GoldButton(string name, Transform parent, UnityAction onClick)
        {
            RectTransform rect = CreateRect(name, parent);

            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = RoundedSprite(28);
            image.type = Image.Type.Sliced;
            image.color = RoyalGoldDeep;

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            if (onClick != null) button.onClick.AddListener(onClick);

            Image face = CreateImage("Face", rect, RoundedSprite(28), RoyalChestGold);
            face.type = Image.Type.Sliced;
            RectTransform faceRect = face.rectTransform;
            faceRect.anchorMin = Vector2.zero;
            faceRect.anchorMax = Vector2.one;
            faceRect.offsetMin = new Vector2(7f, 13f);
            faceRect.offsetMax = new Vector2(-7f, -7f);

            // A fading ramp, not a lighter rectangle over the top half — the
            // same fault and the same fix as GreenButton above. The area card on
            // the home screen is this button, so the seam was running across the
            // widest thing on that screen.
            face.gameObject.AddComponent<Mask>().showMaskGraphic = true;

            Image sheen = CreateImage("Sheen", faceRect, SheenSprite,
                new Color(1f, 1f, 1f, 0.26f));
            Stretch(sheen.rectTransform);
            sheen.raycastTarget = false;

            // Content goes in its own rect inset from the rim, so a caller laying
            // children out in fractions is laying them out inside the face rather
            // than over the button's edges.
            RectTransform content = CreateRect("Content", rect);
            Stretch(content, 0f);
            content.offsetMin = new Vector2(16f, 20f);
            content.offsetMax = new Vector2(-16f, -14f);

            return button;
        }

        /// <summary>The content rect of a <see cref="GoldButton"/>, or its own rect.</summary>
        public static RectTransform ContentOf(Button button)
        {
            if (button == null) return null;
            Transform content = button.transform.Find("Content");
            return content != null
                ? (RectTransform)content
                : (RectTransform)button.transform;
        }

        /// <summary>
        /// The red count badge that hangs off a button's top-right corner — the
        /// "2" on the area button in the reference. Sized square by the caller;
        /// SetCountBadge writes the number and hides the badge at zero, because a
        /// call-to-action badge reading nothing to do is worse than no badge.
        /// </summary>
        public static RectTransform CountBadge(string name, Transform parent, int count)
        {
            RectTransform root = CreateRect(name, parent);
            root.sizeDelta = new Vector2(76f, 76f);

            Image rim = CreateImage("Rim", root, CircleSprite, RoyalRedDark);
            Stretch(rim.rectTransform);

            Image face = CreateImage("Face", root, CircleSprite, RoyalRed);
            Stretch(face.rectTransform, 7f);

            Text label = CreateText("Count", root, count.ToString(), 46, Color.white,
                TextAnchor.MiddleCenter);
            Stretch(label.rectTransform, 6f);
            label.fontStyle = FontStyle.Bold;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 18;
            label.resizeTextMaxSize = 46;

            SetCountBadge(root, count);
            return root;
        }

        /// <summary>Writes a count badge's number; zero hides the whole badge.</summary>
        public static void SetCountBadge(RectTransform badge, int count)
        {
            if (badge == null) return;

            badge.gameObject.SetActive(count > 0);

            Transform label = badge.Find("Count");
            if (label == null) return;

            var text = label.GetComponent<Text>();
            if (text != null) text.text = count.ToString();
        }

        #endregion

        #region Booster slots

        /// <summary>
        /// The parts of a booster tile a screen has to change after it is built:
        /// the icon, the owned count, the locked plate and the selected ring.
        /// </summary>
        public sealed class BoosterSlotParts
        {
            public RectTransform Root;
            public Button Button;
            public Image Tile;
            public Image Face;
            public Image Icon;
            public Image Locked;
            public Image Ring;
            public Text Count;
        }

        /// <summary>
        /// One tile of the "Select Boosters:" row: a gold plate with the
        /// booster's icon on it and a red count badge on its lower-right corner,
        /// as in docs/royal-match/10-level-start-popup.jpg. The caller sizes the
        /// root; everything inside is anchored to it.
        /// </summary>
        public static BoosterSlotParts BoosterSlot(string name, Transform parent,
            UnityAction onClick)
        {
            var parts = new BoosterSlotParts();

            parts.Root = CreateRect(name, parent);
            parts.Root.sizeDelta = new Vector2(150f, 150f);

            // The rim is the base image so the whole tile is one hit target.
            parts.Tile = CreateImage("Tile", parts.Root, RoundedSprite(28), RoyalGoldDeep);
            parts.Tile.type = Image.Type.Sliced;
            Stretch(parts.Tile.rectTransform);

            parts.Button = parts.Tile.gameObject.AddComponent<Button>();
            parts.Button.targetGraphic = parts.Tile;
            parts.Button.transition = Selectable.Transition.None;
            if (onClick != null) parts.Button.onClick.AddListener(onClick);

            parts.Face = CreateImage("Face", parts.Tile.rectTransform, RoundedSprite(24), RoyalGold);
            parts.Face.type = Image.Type.Sliced;
            parts.Face.raycastTarget = false;
            Stretch(parts.Face.rectTransform, 9f);

            parts.Icon = CreateImage("Icon", parts.Face.rectTransform, null, Color.white);
            parts.Icon.preserveAspect = true;
            parts.Icon.raycastTarget = false;
            Stretch(parts.Icon.rectTransform, 12f);

            // The locked plate covers the tile rather than replacing it, so a
            // locked slot still reads as the same booster the player will own.
            parts.Locked = CreateImage("Locked", parts.Tile.rectTransform, LoadSprite("booster_locked"),
                Color.white);
            parts.Locked.preserveAspect = true;
            parts.Locked.raycastTarget = false;
            Stretch(parts.Locked.rectTransform);
            if (parts.Locked.sprite == null) parts.Locked.color = new Color(0f, 0f, 0f, 0.55f);
            parts.Locked.gameObject.SetActive(false);

            // The selected ring sits outside the tile: it must not cover the
            // icon, because the icon is how the player knows what they picked.
            parts.Ring = CreateImage("Ring", parts.Root, RoundedSprite(32), RoyalGreenLight);
            parts.Ring.type = Image.Type.Sliced;
            parts.Ring.raycastTarget = false;
            RectTransform ring = parts.Ring.rectTransform;
            ring.anchorMin = Vector2.zero;
            ring.anchorMax = Vector2.one;
            ring.offsetMin = new Vector2(-10f, -10f);
            ring.offsetMax = new Vector2(10f, 10f);
            ring.SetAsFirstSibling();
            parts.Ring.gameObject.SetActive(false);

            // The count badge: a red disc on the lower-right corner.
            Image badge = CreateImage("Badge", parts.Root, CircleSprite, RoyalRed);
            badge.raycastTarget = false;
            RectTransform badgeRect = badge.rectTransform;
            badgeRect.anchorMin = new Vector2(1f, 0f);
            badgeRect.anchorMax = new Vector2(1f, 0f);
            badgeRect.pivot = new Vector2(0.5f, 0.5f);
            badgeRect.sizeDelta = new Vector2(62f, 62f);
            badgeRect.anchoredPosition = new Vector2(-4f, 6f);

            Image badgeFace = CreateImage("Face", badgeRect, CircleSprite, RoyalRed);
            badgeFace.raycastTarget = false;
            Stretch(badgeFace.rectTransform, 5f);
            badge.color = RoyalRedDark;

            parts.Count = CreateText("Count", badgeRect, "0", 40, Color.white,
                TextAnchor.MiddleCenter);
            Stretch(parts.Count.rectTransform, 4f);
            parts.Count.fontStyle = FontStyle.Bold;
            parts.Count.resizeTextForBestFit = true;
            parts.Count.resizeTextMinSize = 18;
            parts.Count.resizeTextMaxSize = 40;
            parts.Count.raycastTarget = false;

            return parts;
        }

        /// <summary>Rings a chosen booster, and un-rings it when it is unchosen.</summary>
        public static void SetBoosterSelected(BoosterSlotParts parts, bool selected)
        {
            if (parts == null) return;
            if (parts.Ring != null) parts.Ring.gameObject.SetActive(selected);
            if (parts.Tile != null) parts.Tile.color = selected ? RoyalGreenDark : RoyalGoldDeep;
        }

        /// <summary>
        /// A booster the player has none of: the kit's locked plate over a dulled
        /// tile, and the tile stops taking taps. Locked rather than blank, so the
        /// row is always three slots wide and the player can see what they are
        /// missing.
        /// </summary>
        public static void SetBoosterLocked(BoosterSlotParts parts, bool locked)
        {
            if (parts == null) return;
            if (parts.Locked != null) parts.Locked.gameObject.SetActive(locked);
            if (parts.Button != null) parts.Button.interactable = !locked;
            if (parts.Face != null) parts.Face.color = locked ? RoyalGrey : RoyalGold;
            if (parts.Icon != null)
                parts.Icon.color = locked ? new Color(1f, 1f, 1f, 0.45f) : Color.white;
        }

        #endregion

        #region Task icons

        /// <summary>
        /// The object on the left of a task row. Royal Match paints one per
        /// repair — a rolled carpet, a stained-glass window — and this story has
        /// 807 repairs and no art for any of them, so the icon is drawn from the
        /// kind of work the label describes. Ten glyphs cover all 807 because
        /// RepairWork.Classify already collapses the story's 83 verbs into ten
        /// kinds; "Relight the gate lanterns" gets a lantern for the same reason
        /// its cutscene gets a flame.
        ///
        /// Authored art always wins: pass a sprite and it is drawn instead, so
        /// dropping task_001_1.png into Resources/Story/Art needs no code change.
        /// </summary>
        public static RectTransform TaskIcon(string name, Transform parent,
            RepairWorkType work, Sprite authored = null)
        {
            RectTransform root = CreateRect(name, parent);
            root.sizeDelta = new Vector2(102f, 102f);

            if (authored != null)
            {
                Image painted = CreateImage("Art", root, authored, Color.white);
                painted.preserveAspect = true;
                Stretch(painted.rectTransform);
                return root;
            }

            switch (work)
            {
                case RepairWorkType.Clear: DrawRubble(root); break;
                case RepairWorkType.Water: DrawDroplet(root); break;
                case RepairWorkType.Rebuild: DrawBrickwork(root); break;
                case RepairWorkType.Light: DrawLantern(root); break;
                case RepairWorkType.Hang: DrawBanner(root); break;
                case RepairWorkType.Polish: DrawShine(root); break;
                case RepairWorkType.Restock: DrawBarrel(root); break;
                case RepairWorkType.Force: DrawMallet(root); break;
                case RepairWorkType.Search: DrawMagnifier(root); break;
                default: DrawCog(root); break;
            }

            return root;
        }

        /// <summary>The icon for a task, art-first and classified second.</summary>
        public static RectTransform TaskIcon(string name, Transform parent, int areaId,
            StoryTask task)
        {
            if (task == null) return TaskIcon(name, parent, RepairWorkType.General);
            return TaskIcon(name, parent, RepairWork.Classify(task.Label),
                StoryArt.TaskIcon(areaId, task.Index));
        }

        private static Image Block(Transform parent, string name, Color colour, int radius,
            float minX, float minY, float maxX, float maxY)
        {
            Image image = CreateImage(name, parent, RoundedSprite(radius), colour);
            image.type = Image.Type.Sliced;
            FrameRect(image.rectTransform, minX, minY, maxX, maxY);
            return image;
        }

        private static Image Disc(Transform parent, string name, Color colour,
            float minX, float minY, float maxX, float maxY)
        {
            Image image = CreateImage(name, parent, CircleSprite, colour);
            FrameRect(image.rectTransform, minX, minY, maxX, maxY);
            return image;
        }

        // Clear — a pile of stone that is about to be carted out.
        private static void DrawRubble(Transform root)
        {
            Block(root, "Stone1", RoyalGrey, 10, 0.02f, 0.04f, 0.54f, 0.36f);
            Block(root, "Stone2", RoyalGrey, 10, 0.50f, 0.04f, 0.98f, 0.30f);
            Block(root, "Stone3", Rgb(112, 110, 105), 10, 0.16f, 0.34f, 0.70f, 0.66f);
            Block(root, "Chip", Rgb(112, 110, 105), 8, 0.66f, 0.32f, 0.94f, 0.54f);
        }

        // Water — the drop that is leaving, not arriving.
        private static void DrawDroplet(Transform root)
        {
            Disc(root, "Bowl", RoyalBlueDeep, 0.14f, 0.02f, 0.86f, 0.70f);
            Block(root, "Point", RoyalBlueDeep, 22, 0.37f, 0.40f, 0.63f, 0.98f);
            Disc(root, "Shine", RoyalBlue, 0.28f, 0.16f, 0.44f, 0.34f);
        }

        // Rebuild — courses of brick, offset the way a wall is laid.
        private static void DrawBrickwork(Transform root)
        {
            Block(root, "BrickA", IconWood, 8, 0.02f, 0.06f, 0.48f, 0.34f);
            Block(root, "BrickB", IconWood, 8, 0.52f, 0.06f, 0.98f, 0.34f);
            Block(root, "BrickC", IconDeepRed, 8, 0.02f, 0.38f, 0.26f, 0.64f);
            Block(root, "BrickD", IconDeepRed, 8, 0.30f, 0.38f, 0.74f, 0.64f);
            Block(root, "BrickE", IconDeepRed, 8, 0.78f, 0.38f, 0.98f, 0.64f);
            Block(root, "BrickF", IconWood, 8, 0.02f, 0.68f, 0.48f, 0.94f);
            Block(root, "BrickG", IconWood, 8, 0.52f, 0.68f, 0.98f, 0.94f);
        }

        // Light — a lantern with the wick already caught.
        private static void DrawLantern(Transform root)
        {
            Block(root, "Ring", IconWood, 10, 0.44f, 0.90f, 0.56f, 1f);
            Block(root, "Cap", IconWood, 10, 0.26f, 0.78f, 0.74f, 0.92f);
            Block(root, "Body", IconWood, 14, 0.18f, 0.06f, 0.82f, 0.82f);
            Block(root, "Glass", IconGold, 12, 0.28f, 0.18f, 0.72f, 0.72f);
            Disc(root, "Flame", RoyalGoldDeep, 0.40f, 0.30f, 0.60f, 0.58f);
        }

        // Hang — a banner back on its rod.
        private static void DrawBanner(Transform root)
        {
            Block(root, "Cloth", IconMagenta, 12, 0.16f, 0.04f, 0.84f, 0.88f);
            Block(root, "Stripe", IconGold, 8, 0.42f, 0.04f, 0.58f, 0.88f);
            Block(root, "Rod", IconWood, 8, 0.02f, 0.86f, 0.98f, 0.98f);
        }

        // Polish — the shine that was under the grime.
        private static void DrawShine(Transform root)
        {
            Image star = CreateImage("Star", root, StarSprite, IconGold);
            FrameRect(star.rectTransform, 0.02f, 0.16f, 0.70f, 0.92f);
            if (star.sprite == null) star.color = RoyalGold;

            Disc(root, "Spark1", IconGold, 0.70f, 0.04f, 0.92f, 0.28f);
            Disc(root, "Spark2", IconGold, 0.80f, 0.40f, 0.94f, 0.56f);
        }

        // Restock — a barrel filled back up.
        private static void DrawBarrel(Transform root)
        {
            Block(root, "Body", IconWood, 20, 0.16f, 0.04f, 0.84f, 0.96f);
            Block(root, "HoopLow", IconGold, 6, 0.08f, 0.16f, 0.92f, 0.28f);
            Block(root, "HoopHigh", IconGold, 6, 0.08f, 0.72f, 0.92f, 0.84f);
        }

        // Force — the mallet that makes something give.
        private static void DrawMallet(Transform root)
        {
            Block(root, "Handle", IconWood, 10, 0.42f, 0.02f, 0.58f, 0.68f);
            Block(root, "Head", RoyalGrey, 12, 0.10f, 0.62f, 0.90f, 0.96f);
            Block(root, "Band", Rgb(112, 110, 105), 8, 0.44f, 0.62f, 0.56f, 0.96f);
        }

        // Search — quieter work: looking for what is there.
        private static void DrawMagnifier(Transform root)
        {
            Block(root, "Grip", IconWood, 10, 0.06f, 0.04f, 0.24f, 0.44f);
            Disc(root, "Ring", IconWood, 0.18f, 0.28f, 0.98f, 1f);
            Disc(root, "Lens", IconGlass, 0.27f, 0.37f, 0.89f, 0.91f);
        }

        // General — a cog, for work the story did not name more precisely.
        private static void DrawCog(Transform root)
        {
            Image cog = CreateImage("Cog", root, GearSprite, RoyalGoldDeep);
            FrameRect(cog.rectTransform, 0.02f, 0.02f, 0.98f, 0.98f);
            Disc(cog.rectTransform, "Hub", IconGold, 0.34f, 0.34f, 0.66f, 0.66f);
        }

        #endregion

        #region Top bar

        /// <summary>How tall the whole strip is, in reference points.</summary>
        private const float BarHeight = 140f;

        /// <summary>A ribbon's height. The gear is a disc of the same size.</summary>
        private const float PillHeight = 84f;

        /// <summary>
        /// A ribbon's width: its own drawing is 861 by 260, and this is that
        /// ratio at PillHeight. Measured rather than chosen, because the whole
        /// point of the change is that the art is never stretched — so this
        /// constant moves whenever the drawing does.
        /// </summary>
        private const float RibbonWidth = PillHeight * (861f / 260f);

        /// <summary>
        /// The gap between the two ribbons. Wide, because the ribbons carry
        /// their own gold outline: two of them close together read as one bar
        /// broken in half, and the air between is what makes them two counters.
        /// </summary>
        private const float PillGap = 64f;

        /// <summary>
        /// The parts of the top bar that carry a number, so a screen can refresh
        /// them without rebuilding the strip.
        /// </summary>
        public sealed class TopBarParts
        {
            public RectTransform Root;
            public Text Coins;
            public Text Stars;
            public Button Settings;
        }

        /// <summary>
        /// Coins, lives, stars and a settings gear, in the proportions of
        /// docs/royal-match/01-home-area.jpg. Values are read live where the game
        /// has them: gems are the kit's own currency, lives come from InitScript,
        /// and stars are the current area's unspent pool — a star being a currency
        /// here rather than a grade.
        /// </summary>
        public static TopBarParts TopBar(Transform parent, UnityAction onSettings)
        {
            var parts = new TopBarParts();

            RectTransform root = CreateRect("TopBar", parent);
            TopAnchored(root, 42f, BarHeight, 30f);
            parts.Root = root;

            // Two ribbons, not three.
            //
            // Lives is gone: StoryFreeGame never spends one, so the heart read
            // "Full" on every screen for the whole game — a counter that cannot
            // count. Coins and stars both move, so both are worth the width, and
            // dropping the third gives them room to sit at their true size.
            float x = 0f;
            parts.Coins = Pill(root, "Coins", x, RoyalGold, CoinsText(),
                "icon_coin", "ribbon_coins");
            x += RibbonWidth + PillGap;
            parts.Stars = Pill(root, "Stars", x, RoyalGold, StarsText(),
                "icon_star", "ribbon_stars");

            RectTransform gear = CreateRect("Settings", root);
            gear.anchorMin = new Vector2(1f, 0.5f);
            gear.anchorMax = new Vector2(1f, 0.5f);
            gear.pivot = new Vector2(1f, 0.5f);
            gear.sizeDelta = new Vector2(PillHeight, PillHeight);
            gear.anchoredPosition = Vector2.zero;

            // Brown rim, gold face — the gear is a coin-sized object in the same
            // metal as the wordmark, not a navy blob. The face is a second disc
            // rather than a tint on this one, so the rim stays a rim.
            var gearRim = gear.gameObject.AddComponent<Image>();
            gearRim.sprite = CircleSprite;
            gearRim.color = RoyalGoldDeep;

            var settings = gear.gameObject.AddComponent<Button>();
            settings.targetGraphic = gearRim;
            if (onSettings != null) settings.onClick.AddListener(onSettings);
            parts.Settings = settings;

            Image gearWell = CreateImage("Face", gear, CircleSprite, RoyalGold);
            Stretch(gearWell.rectTransform, 7f);
            gearWell.raycastTarget = false;

            Sprite gearArt = StoryArt.Load("icon_gear");
            Image gearFace = CreateImage("Gear", gear,
                gearArt != null ? gearArt : GearSprite,
                gearArt != null ? Color.white : RoyalCream);
            gearFace.preserveAspect = gearArt != null;
            Stretch(gearFace.rectTransform, gearArt != null ? 14f : 26f);

            return parts;
        }

        /// <summary>
        /// One rounded cream pill with an icon on its left end.
        ///
        /// The icon is a painted object where one has been made for that key,
        /// and a flat coloured disc where one has not — which is what the coin
        /// and the heart were until now, and is why the bar read as placeholder
        /// UI next to the reference. The fallback is kept rather than removed so
        /// that a missing file is a plainer bar, never an empty one.
        /// </summary>
        private static Text Pill(RectTransform parent, string name, float left,
            Color iconColour, string value, string iconKey, string ribbonKey)
        {
            RectTransform rect = CreateRect(name, parent);
            // Placed at its own size from the bar's left edge, rather than
            // anchored to a slice of the bar's width. Anchoring stretched the
            // ribbon to whatever slice it was given: the rounded end went oval
            // and the notch flattened, which is exactly the distortion the
            // painted shape exists to avoid.
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(RibbonWidth, PillHeight);
            rect.anchoredPosition = new Vector2(left, 0f);

            // A painted ribbon, not two rounded rectangles: gold outline, flat
            // saturated field, a notch cut out of the tail. Drawn Simple at the
            // rect it was measured for, so every proportion in the drawing
            // survives.
            Sprite ribbon = LoadSprite(ribbonKey);
            if (ribbon != null)
            {
                Image art = CreateImage("Ribbon", rect, ribbon, Color.white);
                art.type = Image.Type.Simple;
                art.preserveAspect = true;
                Stretch(art.rectTransform);
                art.raycastTarget = false;
            }
            else
            {
                // No ribbon on disk: a plain cream pill, so a missing file is a
                // duller bar rather than two numbers floating on the sky.
                // LoadSprite has already warned.
                Image rim = CreateImage("Rim", rect, RoundedSprite(28), IconWood);
                Stretch(rim.rectTransform);
                rim.type = Image.Type.Sliced;

                Image fill = CreateImage("Fill", rect, RoundedSprite(28), RoyalCream);
                Stretch(fill.rectTransform, 6f);
                fill.type = Image.Type.Sliced;
            }

            // Cream on a saturated field, with a dark edge under it: the ribbons
            // are different colours and one text colour has to read on all.
            Text text = CreateText("Value", rect, value, 46,
                ribbon != null ? RoyalTitle : IconWood, TextAnchor.MiddleCenter);
            // The display face, not the kit's UI font. CreateText takes whatever
            // Text happens to be in the scene, which is body copy; these are two
            // numbers on painted ribbons and they belong in the same chunky face
            // as the wordmark and the PLAY button.
            text.font = DisplayFont;
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            // Clear of the coin on the left and of the notch on the right.
            textRect.offsetMin = new Vector2(PillHeight * 0.95f, 0f);
            textRect.offsetMax = new Vector2(ribbon != null ? -PillHeight * 0.5f : -14f, 0f);
            text.fontStyle = FontStyle.Bold;
            if (ribbon != null)
            {
                var edge = text.gameObject.AddComponent<Outline>();
                edge.effectColor = new Color(0f, 0f, 0f, 0.55f);
                edge.effectDistance = new Vector2(2f, -2f);
            }

            // The icon overhangs the ribbon's left end, as in the reference, and
            // is measured off the pill so it cannot outgrow it.
            Sprite iconArt = StoryArt.Load(iconKey);
            Image icon = CreateImage("Icon", rect,
                iconArt != null ? iconArt : CircleSprite,
                iconArt != null ? Color.white : iconColour);
            icon.preserveAspect = iconArt != null;
            RectTransform iconRect = icon.rectTransform;
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(PillHeight, PillHeight);
            iconRect.anchoredPosition = new Vector2(PillHeight * 0.30f, 0f);

            return text;
        }

        /// <summary>The kit's own soft currency. It has no coin, so gems are the coins.</summary>
        public static string CoinsText()
        {
            return InitScript.Gems.ToString();
        }

        /// <summary>
        /// Lives read "Full" while the game is free, because StoryFreeGame never
        /// spends one — a number that can only ever be five is noise.
        /// </summary>
        public static string LivesText()
        {
            return StoryFreeGame.Enabled ? "Full" : InitScript.lifes.ToString();
        }

        /// <summary>Unspent stars in the area the player is in. Stars do not travel.</summary>
        public static string StarsText()
        {
            return StoryProgress.StarsIn(StoryProgress.CurrentArea).ToString();
        }

        /// <summary>Re-reads every live value. Cheap enough to call on any refresh.</summary>
        public static void RefreshTopBar(TopBarParts parts)
        {
            if (parts == null) return;
            if (parts.Coins != null) parts.Coins.text = CoinsText();
            if (parts.Stars != null) parts.Stars.text = StarsText();
        }

        #endregion

        #region Bottom navigation

        // Kept for anything still asking, and for the flat fallback: these were
        // the blue bar's own two blues, sampled off
        // docs/royal-match/01-home-area.jpg.
        public static readonly Color RoyalNavBright = Rgb(33, 127, 225);
        public static readonly Color RoyalNavSeam = Rgb(0, 55, 133);

        // The signs. Wood at rest, gold when it is the screen you are on — the
        // same gold as every other affirmative thing in the game, so selection
        // is said in a colour the player has already learned.
        public static readonly Color PlankFace = Rgb(139, 98, 55);
        public static readonly Color PlankDeep = Rgb(94, 63, 32);
        public static readonly Color PlankEdge = Rgb(62, 41, 19);
        public static readonly Color RopeLight = Rgb(138, 107, 62);
        public static readonly Color RopeShadow = Rgb(78, 58, 30);

        /// <summary>
        /// How much of the screen's height the navigation takes.
        ///
        /// Half again as much as the blue bar it replaces. The bar was one flat
        /// strip and 145 of 1472 was enough for it; a rope with boards hung off
        /// it is four things stacked — rope, hook, board, word — and at the old
        /// height the rope came out three pixels tall and vanished into the
        /// room. Every screen reserves this band by reading this constant, so
        /// they all give way together.
        ///
        /// It costs less than it looks: the boards do not fill the band and the
        /// room is drawn behind them, so the artwork is still visible where the
        /// bar used to be opaque.
        /// </summary>
        public const float NavBarHeightFrac = 218f / 1472f;

        // Where the rope is strung, as a fraction up that band, and how thick it
        // is in reference points. The signs hang from it, so it sits near the top
        // and everything else is measured down from there.
        private const float RopeHeightFrac = 0.88f;
        private const float RopeThickness = 13f;

        // The hook between the rope and the board, and how far the chosen board
        // swings up. Both in fractions of the band.
        private const float HookHeightFrac = 0.12f;
        private const float SignRiseFrac = 0.11f;

        // How much of its slot a board fills. Under one so the boards read as
        // separate objects hung side by side rather than as a fence.
        private const float SignWidthFrac = 0.72f;

        // How tall a board is, as a fraction of the band. What is left under it
        // is the gap to the bottom of the screen, which the room fills.
        private const float SignHeightFrac = 0.60f;

        /// <summary>The five things the bottom bar can be.</summary>
        public enum NavIcon
        {
            Areas,
            Leaderboard,
            Home,
            Team,
            Cards
        }

        /// <summary>One tab: its button, the face that carries the selection
        /// colour, its glyph and the label under a raised one.</summary>
        public sealed class NavTabParts
        {
            public NavIcon Icon;
            public Button Button;
            public Image Face;
            public RectTransform Glyph;
            public Text Label;

            /// <summary>Whether this sign is the one hanging high RIGHT NOW.</summary>
            public bool Raised;

            /// <summary>The board itself — the thing that rises and turns gold.</summary>
            public RectTransform Board;

            /// <summary>The line from the rope down to the board.</summary>
            public RectTransform Hook;

            /// <summary>The darker band across the board's foot.</summary>
            public Image Grain;

            /// <summary>
            /// The angle this sign hangs at while it is not the current screen,
            /// in degrees. Fixed per slot rather than random, so the bar looks
            /// the same every time it is built.
            /// </summary>
            public float Tilt;

            public AspectRatioFitter Fitter;
        }

        /// <summary>The bar and its five tabs, in left-to-right order.</summary>
        public sealed class NavBarParts
        {
            public RectTransform Root;
            public Image Body;
            public RectTransform Rim;
            public NavTabParts[] Tabs;
        }

        /// <summary>
        /// The five-tab strip across the bottom of every shell screen, built from
        /// docs/royal-match/01-home-area.jpg. The middle tab is double width, its
        /// glyph breaks up through the gold rim and it alone carries a label —
        /// that is what "raised" means here, and it is why the glyphs are drawn in
        /// their own layer ABOVE the rim while the faces that take the taps stay
        /// below it. The glyph layer does not block raycasts, so a tap on the
        /// castle still reaches the tab underneath it.
        ///
        /// The bar owns no navigation of its own: <paramref name="onTab"/> is
        /// called with the index that was pressed.
        /// </summary>
        public static NavBarParts NavBar(string name, Transform parent,
            NavIcon[] icons, string[] labels, int raised, UnityAction<int> onTab)
        {
            var parts = new NavBarParts();

            RectTransform root = CreateRect(name, parent);
            root.anchorMin = Vector2.zero;
            root.anchorMax = new Vector2(1f, NavBarHeightFrac);
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
            parts.Root = root;

            // No body. There is no bar any more — the room runs all the way to
            // the bottom of the screen and the signs hang on it. That is the
            // point of the shape: a bar paints over a tenth of the artwork on
            // every frame, and this paints over three small planks of it.
            //
            // It also means a tap between two signs falls through to the screen
            // behind, which is correct: there is nothing there to press.
            parts.Body = null;

            RectTransform faces = CreateRect("Signs", root);
            Stretch(faces);

            RectTransform glyphs = CreateRect("Glyphs", root);
            Stretch(glyphs);

            // The rope, strung across the whole width at the height the hooks
            // reach. Two bands, light over dark, which is the least a rope needs
            // to stop reading as a drawn line.
            RectTransform rope = CreateRect("Rope", root);
            rope.anchorMin = new Vector2(0f, RopeHeightFrac);
            rope.anchorMax = new Vector2(1f, RopeHeightFrac);
            rope.pivot = new Vector2(0.5f, 0.5f);
            rope.offsetMin = new Vector2(0f, -RopeThickness * 0.5f);
            rope.offsetMax = new Vector2(0f, RopeThickness * 0.5f);
            parts.Rim = rope;

            // Three bands: an outline, the rope, and a highlight along its top.
            // The outline is what lets it survive a bright room — the first pass
            // had none and the rope disappeared into a dark one instead, which
            // is the same failure from the other end.
            Image ropeEdge = CreateImage("RopeEdge", rope, null, PlankEdge);
            Stretch(ropeEdge.rectTransform);
            ropeEdge.raycastTarget = false;

            Image ropeDark = CreateImage("RopeDark", rope, null, RopeShadow);
            Stretch(ropeDark.rectTransform, 2f);
            ropeDark.raycastTarget = false;

            Image ropeLit = CreateImage("RopeLit", rope, null, RopeLight);
            RectTransform ropeLitRect = ropeLit.rectTransform;
            ropeLitRect.anchorMin = new Vector2(0f, 0.46f);
            ropeLitRect.anchorMax = Vector2.one;
            ropeLitRect.offsetMin = new Vector2(0f, 0f);
            ropeLitRect.offsetMax = new Vector2(0f, -2f);
            ropeLit.raycastTarget = false;

            int count = icons == null ? 0 : icons.Length;
            parts.Tabs = new NavTabParts[count];

            // Signs share the width evenly. There is no double-width slot any
            // more: every destination is a board of the same size, and which one
            // the player is on is said by the board hanging higher and turning
            // gold, not by it being bigger than its neighbours.
            for (int i = 0; i < count; i++)
            {
                float min = count == 0 ? 0f : (float)i / count;
                float max = count == 0 ? 1f : (float)(i + 1) / count;
                string label = labels != null && i < labels.Length ? labels[i] : null;
                parts.Tabs[i] = NavTab(faces, glyphs, icons[i], label, min, max,
                    i == raised, i, onTab);
            }

            return parts;
        }

        /// <summary>
        /// One hanging sign: a hook down from the rope, a wooden board on the
        /// end of it, and the icon and word on the board.
        ///
        /// The board is the tab's own hit target, so a tap has to land on wood.
        /// The gap between two boards is not a button, which is why the bar has
        /// no body behind them any more.
        /// </summary>
        private static NavTabParts NavTab(RectTransform faces, RectTransform glyphs,
            NavIcon icon, string label, float min, float max, bool raised, int index,
            UnityAction<int> onTab)
        {
            var parts = new NavTabParts { Icon = icon };

            float mid = (min + max) * 0.5f;
            float half = (max - min) * 0.5f * SignWidthFrac;

            // The hook. Drawn first so the board hangs over the bottom of it.
            Image hook = CreateImage("Hook" + index, faces, null, RopeShadow);
            RectTransform hookRect = hook.rectTransform;
            hookRect.anchorMin = new Vector2(mid, SignTopFrac(false));
            hookRect.anchorMax = new Vector2(mid, RopeHeightFrac);
            hookRect.pivot = new Vector2(0.5f, 0.5f);
            hookRect.offsetMin = new Vector2(-4f, 0f);
            hookRect.offsetMax = new Vector2(4f, 0f);
            hook.raycastTarget = false;
            parts.Hook = hookRect;

            // The board: a dark edge with the face inset inside it, the same two
            // nested rounded rects every other button in this game is made of.
            RectTransform board = CreateRect("Sign" + index, faces);
            board.anchorMin = new Vector2(mid - half, 0f);
            board.anchorMax = new Vector2(mid + half, 0f);
            // The pivot is the top edge, where the hook holds it: a hanging sign
            // swings from its rope, not around its own middle.
            board.pivot = new Vector2(0.5f, 1f);
            parts.Board = board;
            parts.Tilt = TiltFor(index);

            var edge = board.gameObject.AddComponent<Image>();
            edge.sprite = RoundedSprite(18);
            edge.type = Image.Type.Sliced;
            edge.color = PlankEdge;
            edge.raycastTarget = true;

            var button = board.gameObject.AddComponent<Button>();
            button.targetGraphic = edge;
            button.transition = Selectable.Transition.None;
            if (onTab != null)
            {
                int captured = index;
                button.onClick.AddListener(() => onTab(captured));
            }
            parts.Button = button;

            Image face = CreateImage("Face", board, RoundedSprite(16), PlankFace);
            face.type = Image.Type.Sliced;
            Stretch(face.rectTransform, 4f);
            face.raycastTarget = false;
            parts.Face = face;

            // Grain: one darker band across the lower half, so the board reads as
            // sawn wood rather than as a brown rectangle.
            Image grain = CreateImage("Grain", board, RoundedSprite(12), PlankDeep);
            RectTransform grainRect = grain.rectTransform;
            grainRect.anchorMin = new Vector2(0f, 0f);
            grainRect.anchorMax = new Vector2(1f, 0.34f);
            grainRect.offsetMin = new Vector2(7f, 5f);
            grainRect.offsetMax = new Vector2(-7f, 0f);
            grain.type = Image.Type.Sliced;
            grain.raycastTarget = false;
            parts.Grain = grain;

            RectTransform glyph = CreateRect("Glyph" + index, board);
            glyph.anchorMin = new Vector2(0.5f, 0.36f);
            glyph.anchorMax = new Vector2(0.5f, 0.94f);
            glyph.pivot = new Vector2(0.5f, 0.5f);
            glyph.offsetMin = Vector2.zero;
            glyph.offsetMax = Vector2.zero;

            var fitter = glyph.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
            fitter.aspectRatio = 1f;
            parts.Fitter = fitter;

            DrawNavIcon(glyph, icon);
            SetRaycastsOff(glyph);
            parts.Glyph = glyph;

            if (!string.IsNullOrEmpty(label))
            {
                Text text = CreateText("Label" + index, board, label, 34, RoyalCream,
                    TextAnchor.MiddleCenter);
                RectTransform textRect = text.rectTransform;
                textRect.anchorMin = new Vector2(0f, 0.02f);
                textRect.anchorMax = new Vector2(1f, 0.34f);
                textRect.offsetMin = new Vector2(6f, 0f);
                textRect.offsetMax = new Vector2(-6f, 0f);
                text.fontStyle = FontStyle.Bold;
                text.raycastTarget = false;
                FitOneLine(text, 12, 34);
                parts.Label = text;
            }

            SetNavTabRaised(parts, raised);
            return parts;
        }

        /// <summary>
        /// How far off true a sign hangs, by slot.
        ///
        /// Four fixed angles rather than one, because four boards at the same
        /// angle read as a deliberate slant on the whole bar; four at different
        /// ones read as four signs hung by hand. None is more than three
        /// degrees — enough to see, not enough to make the words hard to read.
        /// </summary>
        private static float TiltFor(int index)
        {
            switch (index % 4)
            {
                case 0: return -2.5f;
                case 1: return 1.8f;
                case 2: return -1.2f;
                default: return 2.6f;
            }
        }

        /// <summary>Where a board's top edge sits, as a fraction of the band.</summary>
        private static float SignTopFrac(bool raised)
        {
            return RopeHeightFrac - HookHeightFrac + (raised ? SignRiseFrac : 0f);
        }

        /// <summary>
        /// Lights the tab the player is on, and only that one.
        ///
        /// RAISED AND SELECTED ARE NOT THE SAME THING. This used to light the
        /// raised tab as well as the current one, on the reasoning that being
        /// raised is what that tab IS. The side effect was that every screen
        /// looked like Home: Home is raised, so Home was always lit, and it is
        /// the biggest, brightest thing on the bar with a word under it — so on
        /// the areas list, which correctly lit its own tab too, the eye still
        /// read Home. The bar was reporting the wrong screen on every screen
        /// except one.
        ///
        /// Raised is still permanent — it is the landmark, and the shape of the
        /// bar does not move as screens change. Only the face colour follows the
        /// player now.
        /// </summary>
        public static void SetNavSelected(NavBarParts parts, int selected)
        {
            if (parts == null || parts.Tabs == null) return;

            for (int i = 0; i < parts.Tabs.Length; i++)
            {
                NavTabParts tab = parts.Tabs[i];
                if (tab == null) continue;

                SetNavTabRaised(tab, i == selected);
            }
        }

        /// <summary>
        /// Lifts one tab's glyph out of the bar, or drops it back in, and shows
        /// or hides its word.
        ///
        /// The lift is the loudest thing on the bar — it is the one glyph that
        /// breaks the gold rim — so it has to mean the screen the player is on.
        /// It used to be nailed to Home, which is why the bar read as Home
        /// everywhere. The tab WIDTHS do not move: the middle slot stays double,
        /// so the bar keeps its shape and only the glyph and the word travel.
        /// </summary>
        private static void SetNavTabRaised(NavTabParts tab, bool raised)
        {
            if (tab == null || tab.Board == null) return;

            tab.Raised = raised;

            // The board swings up and the hook shortens by the same amount, so
            // the rope stays where it is and only the sign moves on it.
            float top = SignTopFrac(raised);
            tab.Board.anchorMin = new Vector2(tab.Board.anchorMin.x, top - SignHeightFrac);
            tab.Board.anchorMax = new Vector2(tab.Board.anchorMax.x, top);
            tab.Board.offsetMin = Vector2.zero;
            tab.Board.offsetMax = Vector2.zero;

            if (tab.Hook != null)
            {
                tab.Hook.anchorMin = new Vector2(tab.Hook.anchorMin.x, top);
                tab.Hook.anchorMax = new Vector2(tab.Hook.anchorMax.x, RopeHeightFrac);
            }

            // The current screen's sign is the one hanging straight. The others
            // stay as they were hung, which is what makes straightening read as
            // somebody having just touched this one.
            tab.Board.localRotation = Quaternion.Euler(0f, 0f, raised ? 0f : tab.Tilt);

            var edge = tab.Button == null ? null : tab.Button.targetGraphic as Image;
            if (edge != null) edge.color = raised ? RoyalGoldDeep : PlankEdge;
            if (tab.Face != null) tab.Face.color = raised ? RoyalGold : PlankFace;
            if (tab.Grain != null)
                tab.Grain.color = raised ? RoyalGoldDeep : PlankDeep;
            if (tab.Label != null)
                tab.Label.color = raised ? RoyalTealInk : RoyalCream;
        }

        private static void SetRaycastsOff(Transform root)
        {
            Graphic[] graphics = root.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++) graphics[i].raycastTarget = false;
        }

        /// <summary>
        /// The painted icon for a tab, or null where nobody has painted one.
        ///
        /// Resolved by name from Resources/Story/Art like the rest of the story's
        /// art, so a tab is re-skinned by dropping a file in — and deleting the
        /// files gives the drawn glyphs straight back.
        /// </summary>
        private static Sprite NavArt(NavIcon icon)
        {
            switch (icon)
            {
                case NavIcon.Areas: return StoryArt.Load("nav_areas");
                case NavIcon.Leaderboard: return StoryArt.Load("nav_leaderboard");
                case NavIcon.Home: return StoryArt.Load("nav_home");
                case NavIcon.Team: return StoryArt.Load("nav_team");
                default: return StoryArt.Load("nav_cards");
            }
        }

        private static void DrawNavIcon(RectTransform root, NavIcon icon)
        {
            Sprite art = NavArt(icon);
            if (art != null)
            {
                Image painted = CreateImage("Art", root, art, Color.white);
                painted.preserveAspect = true;
                Stretch(painted.rectTransform);
                return;
            }

            switch (icon)
            {
                case NavIcon.Areas: DrawNavBook(root); break;
                case NavIcon.Leaderboard: DrawNavTrophy(root); break;
                case NavIcon.Home: DrawNavCastle(root); break;
                case NavIcon.Team: DrawNavShield(root); break;
                default: DrawNavCards(root); break;
            }
        }

        // Areas — the book of rooms, with the star the game spends on them.
        private static void DrawNavBook(Transform root)
        {
            Block(root, "Ribbon", RoyalRed, 4, 0.30f, 0.00f, 0.44f, 0.30f);
            Block(root, "Cover", RoyalGoldDeep, 14, 0.06f, 0.12f, 0.94f, 0.96f);
            Block(root, "Page", RoyalPurple, 10, 0.17f, 0.24f, 0.83f, 0.88f);

            Image star = CreateImage("Star", root, StarSprite, IconGold);
            FrameRect(star.rectTransform, 0.24f, 0.31f, 0.76f, 0.81f);
            if (star.sprite == null) star.color = RoyalGold;
        }

        // Leaderboard — a cup with two handles and a plinth.
        private static void DrawNavTrophy(Transform root)
        {
            Disc(root, "HandleLeft", RoyalGoldDeep, 0.02f, 0.54f, 0.32f, 0.92f);
            Disc(root, "HandleRight", RoyalGoldDeep, 0.68f, 0.54f, 0.98f, 0.92f);
            Block(root, "Stem", RoyalGoldDeep, 6, 0.42f, 0.14f, 0.58f, 0.52f);
            Block(root, "Base", IconGold, 8, 0.22f, 0.02f, 0.78f, 0.20f);
            Block(root, "Cup", IconGold, 26, 0.22f, 0.42f, 0.78f, 0.98f);
            Block(root, "Shine", RoyalGold, 8, 0.31f, 0.56f, 0.41f, 0.90f);
        }

        // Home — the castle. Three towers, purple caps, a door in the middle.
        private static void DrawNavCastle(Transform root)
        {
            Disc(root, "CapLeft", RoyalPurple, 0.02f, 0.52f, 0.38f, 0.80f);
            Disc(root, "CapRight", RoyalPurple, 0.62f, 0.52f, 0.98f, 0.80f);
            Disc(root, "CapKeep", RoyalPurple, 0.26f, 0.70f, 0.74f, 0.96f);
            Disc(root, "Finial", RoyalChestGold, 0.45f, 0.93f, 0.55f, 1.00f);

            Block(root, "TowerLeft", RoyalCream, 8, 0.06f, 0.08f, 0.34f, 0.60f);
            Block(root, "TowerRight", RoyalCream, 8, 0.66f, 0.08f, 0.94f, 0.60f);
            Block(root, "Keep", RoyalCream, 8, 0.30f, 0.08f, 0.70f, 0.78f);

            Block(root, "WindowLeft", IconWood, 4, 0.13f, 0.32f, 0.27f, 0.48f);
            Block(root, "WindowRight", IconWood, 4, 0.73f, 0.32f, 0.87f, 0.48f);
            Block(root, "WindowKeep", IconWood, 4, 0.43f, 0.50f, 0.57f, 0.66f);

            Block(root, "Band", IconGold, 4, 0.02f, 0.10f, 0.98f, 0.20f);
            Block(root, "Door", IconWood, 8, 0.42f, 0.10f, 0.58f, 0.42f);
        }

        // Team — a shield with three of them behind it.
        private static void DrawNavShield(Transform root)
        {
            Disc(root, "PointGold", RoyalChestGold, 0.06f, 0.02f, 0.94f, 0.64f);
            Block(root, "BodyGold", RoyalChestGold, 22, 0.06f, 0.30f, 0.94f, 0.96f);
            Disc(root, "PointFace", RoyalPurple, 0.17f, 0.13f, 0.83f, 0.62f);
            Block(root, "BodyFace", RoyalPurple, 16, 0.17f, 0.36f, 0.83f, 0.86f);

            Disc(root, "HeadLeft", IconGold, 0.21f, 0.50f, 0.41f, 0.70f);
            Disc(root, "HeadRight", IconGold, 0.59f, 0.50f, 0.79f, 0.70f);
            Block(root, "BodyLeft", IconGold, 10, 0.19f, 0.26f, 0.43f, 0.50f);
            Block(root, "BodyRight", IconGold, 10, 0.57f, 0.26f, 0.81f, 0.50f);
            Disc(root, "HeadMid", IconGold, 0.38f, 0.58f, 0.62f, 0.82f);
            Block(root, "BodyMid", IconGold, 12, 0.32f, 0.28f, 0.68f, 0.60f);
        }

        // Cards — a fanned hand with a crown on the face of the front one.
        private static void DrawNavCards(Transform root)
        {
            Block(root, "Back", RoyalGoldDeep, 10, 0.02f, 0.22f, 0.52f, 0.94f);
            Block(root, "Middle", RoyalGoldDeep, 10, 0.20f, 0.14f, 0.70f, 0.90f);
            Block(root, "Front", RoyalChestGold, 10, 0.40f, 0.06f, 0.98f, 0.86f);
            Block(root, "Face", RoyalPurple, 8, 0.47f, 0.13f, 0.91f, 0.79f);

            Block(root, "CrownBand", IconGold, 4, 0.53f, 0.30f, 0.85f, 0.42f);
            Block(root, "CrownLeft", IconGold, 4, 0.53f, 0.38f, 0.61f, 0.54f);
            Block(root, "CrownMid", IconGold, 4, 0.65f, 0.38f, 0.73f, 0.60f);
            Block(root, "CrownRight", IconGold, 4, 0.77f, 0.38f, 0.85f, 0.54f);
        }

        #endregion
    }
}
