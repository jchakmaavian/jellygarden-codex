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

            // Gold rim, then blue fill inset inside it: two rects rather than an
            // outline component, because Outline offsets the whole quad and
            // leaves the corners square.
            Image bodyRim = CreateImage("BodyRim", body, RoundedSprite(48), RoyalGoldDeep);
            Stretch(bodyRim.rectTransform);
            bodyRim.type = Image.Type.Sliced;

            Image bodyHighlight = CreateImage("BodyRimHighlight", body, RoundedSprite(48), RoyalGold);
            Stretch(bodyHighlight.rectTransform, PanelRim * 0.45f);
            bodyHighlight.type = Image.Type.Sliced;

            Image bodyFill = CreateImage("BodyFill", body, RoundedSprite(48), RoyalBlue);
            Stretch(bodyFill.rectTransform, PanelRim);
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

            // The title plate: a rounded blue slab with two gold bands across it.
            RectTransform plate = CreateRect("TitlePlate", root);
            TopAnchored(plate, 0f, PanelTitleHeight, -18f);

            Image plateRim = CreateImage("PlateRim", plate, RoundedSprite(48), RoyalGoldDeep);
            Stretch(plateRim.rectTransform);
            plateRim.type = Image.Type.Sliced;

            Image plateFill = CreateImage("PlateFill", plate, RoundedSprite(48), RoyalBlueDark);
            Stretch(plateFill.rectTransform, PanelRim);
            plateFill.type = Image.Type.Sliced;
            plateFill.raycastTarget = true;

            Image bandTop = CreateImage("BandTop", plate, null, RoyalGold);
            TopAnchored(bandTop.rectTransform, 26f, 8f, 26f);
            Image bandBottom = CreateImage("BandBottom", plate, null, RoyalGold);
            BottomAnchored(bandBottom.rectTransform, 26f, 8f, 26f);

            // Inset wide but not tall: 110 clears the close X on the right, and
            // the plate is only PanelTitleHeight tall, so the same inset applied
            // vertically leaves the text a rect of negative height — which draws
            // nothing at all, and reads as a panel that forgot its own name.
            Text titleText = CreateText("Title", plate, title, 76, RoyalTitle,
                TextAnchor.MiddleCenter);
            RectTransform titleRect = titleText.rectTransform;
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
            titleRect.offsetMin = new Vector2(110f, 22f);
            titleRect.offsetMax = new Vector2(-110f, -22f);
            titleText.fontStyle = FontStyle.Bold;
            var titleOutline = titleText.gameObject.AddComponent<Outline>();
            titleOutline.effectColor = RoyalBlueDark;
            titleOutline.effectDistance = new Vector2(3f, -3f);
            parts.Title = titleText;

            if (onClose != null) parts.Close = CloseButton(plate, onClose);

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
        /// The sunken progress track from docs/royal-match/02-area-tasks.jpg: an
        /// orange-rimmed well of RoyalBlueDeep with a green bar growing out of its
        /// left end and the count written across the middle. The green is the same
        /// green the home screen's area bar uses in 09-home-level-button.jpg.
        /// </summary>
        public static ProgressBarParts ProgressBar(string name, Transform parent)
        {
            var parts = new ProgressBarParts();

            RectTransform root = CreateRect(name, parent);
            parts.Root = root;

            Image rim = CreateImage("Rim", root, RoundedSprite(40), RoyalGoldDeep);
            Stretch(rim.rectTransform);
            rim.type = Image.Type.Sliced;

            Image well = CreateImage("Well", root, RoundedSprite(40), RoyalBlueDeep);
            Stretch(well.rectTransform, 8f);
            well.type = Image.Type.Sliced;

            // The fill is anchored rather than sized: a fraction of the track's
            // width survives whatever width the screen ends up handing the track.
            Image fill = CreateImage("Fill", well.rectTransform, RoundedSprite(36), RoyalGreen);
            fill.type = Image.Type.Sliced;
            RectTransform fillRect = fill.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.offsetMin = new Vector2(4f, 4f);
            fillRect.offsetMax = new Vector2(-4f, -4f);
            parts.Fill = fillRect;

            Image gloss = CreateImage("Gloss", fillRect, RoundedSprite(24), RoyalGreenLight);
            gloss.type = Image.Type.Sliced;
            RectTransform glossRect = gloss.rectTransform;
            glossRect.anchorMin = new Vector2(0f, 0.52f);
            glossRect.anchorMax = new Vector2(1f, 0.9f);
            glossRect.offsetMin = new Vector2(10f, 0f);
            glossRect.offsetMax = new Vector2(-10f, 0f);

            // Over the bar, not inside it: the count has to stay legible whether
            // the green has reached it or not.
            Text label = CreateText("Value", root, string.Empty, 52, RoyalTitle,
                TextAnchor.MiddleCenter);
            Stretch(label.rectTransform);
            label.fontStyle = FontStyle.Bold;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 18;
            label.resizeTextMaxSize = 52;
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
            RectTransform root = CreateRect(name, parent);

            Image lid = CreateImage("Lid", root, RoundedSprite(44), RoyalPurple);
            lid.type = Image.Type.Sliced;
            FrameRect(lid.rectTransform, 0f, 0.44f, 1f, 1f);

            Image lidFace = CreateImage("LidFace", lid.rectTransform, RoundedSprite(36),
                RoyalChestGold);
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
                RoyalChestGold);
            bodyFace.type = Image.Type.Sliced;
            FrameRect(bodyFace.rectTransform, 0.07f, 0.2f, 0.93f, 0.82f);

            Image plate = CreateImage("Lock", root, RoundedSprite(18), RoyalPurple);
            plate.type = Image.Type.Sliced;
            FrameRect(plate.rectTransform, 0.37f, 0.24f, 0.63f, 0.62f);

            Image keyPlate = CreateImage("Keyhole", plate.rectTransform, CircleSprite,
                RoyalChestGold);
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

            // The glossy top third.
            Image gloss = CreateImage("Gloss", rect, RoundedSprite(28), RoyalGreenLight);
            gloss.type = Image.Type.Sliced;
            RectTransform glossRect = gloss.rectTransform;
            glossRect.anchorMin = new Vector2(0f, 0.55f);
            glossRect.anchorMax = new Vector2(1f, 1f);
            glossRect.offsetMin = new Vector2(18f, 0f);
            glossRect.offsetMax = new Vector2(-18f, -14f);

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
            if (baseImage != null)
                baseImage.color = interactable ? RoyalGreenDark : Rgb(90, 88, 84);

            Transform face = button.transform.Find("Face");
            if (face != null)
            {
                var faceImage = face.GetComponent<Image>();
                if (faceImage != null) faceImage.color = interactable ? RoyalGreen : RoyalGrey;
            }

            Transform gloss = button.transform.Find("Gloss");
            if (gloss != null)
            {
                var glossImage = gloss.GetComponent<Image>();
                if (glossImage != null)
                    glossImage.color = interactable ? RoyalGreenLight : Rgb(178, 176, 170);
            }
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

        /// <summary>
        /// The parts of the top bar that carry a number, so a screen can refresh
        /// them without rebuilding the strip.
        /// </summary>
        public sealed class TopBarParts
        {
            public RectTransform Root;
            public Text Coins;
            public Text Lives;
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
            TopAnchored(root, 42f, 140f, 30f);
            parts.Root = root;

            parts.Coins = Pill(root, "Coins", 0f, 0.34f, RoyalGold, CoinsText());
            parts.Lives = Pill(root, "Lives", 0.355f, 0.685f, RoyalRed, LivesText());
            parts.Stars = Pill(root, "Stars", 0.70f, 0.885f, RoyalGold, StarsText());

            RectTransform gear = CreateRect("Settings", root);
            gear.anchorMin = new Vector2(0.905f, 0f);
            gear.anchorMax = new Vector2(1f, 1f);
            gear.offsetMin = Vector2.zero;
            gear.offsetMax = Vector2.zero;

            var gearRim = gear.gameObject.AddComponent<Image>();
            gearRim.sprite = CircleSprite;
            gearRim.color = RoyalBlueDark;

            var settings = gear.gameObject.AddComponent<Button>();
            settings.targetGraphic = gearRim;
            if (onSettings != null) settings.onClick.AddListener(onSettings);
            parts.Settings = settings;

            Image gearFace = CreateImage("Gear", gear, GearSprite, RoyalCream);
            Stretch(gearFace.rectTransform, 26f);

            return parts;
        }

        /// <summary>One rounded cream pill with an icon disc on its left end.</summary>
        private static Text Pill(RectTransform parent, string name, float minX, float maxX,
            Color iconColour, string value)
        {
            RectTransform rect = CreateRect(name, parent);
            rect.anchorMin = new Vector2(minX, 0.06f);
            rect.anchorMax = new Vector2(maxX, 0.94f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image rim = CreateImage("Rim", rect, RoundedSprite(28), RoyalBlueDark);
            Stretch(rim.rectTransform);
            rim.type = Image.Type.Sliced;

            Image fill = CreateImage("Fill", rect, RoundedSprite(28), RoyalCream);
            Stretch(fill.rectTransform, 6f);
            fill.type = Image.Type.Sliced;

            Text text = CreateText("Value", rect, value, 46, RoyalNavy, TextAnchor.MiddleCenter);
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(96f, 0f);
            textRect.offsetMax = new Vector2(-14f, 0f);
            text.fontStyle = FontStyle.Bold;

            // The icon overhangs the pill's left end, as in the reference.
            Image icon = CreateImage("Icon", rect, CircleSprite, iconColour);
            RectTransform iconRect = icon.rectTransform;
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(118f, 118f);
            iconRect.anchoredPosition = new Vector2(6f, 0f);

            if (name == "Stars")
            {
                icon.sprite = StarSprite != null ? StarSprite : CircleSprite;
                if (StarSprite != null) icon.color = Color.white;
            }

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
            if (parts.Lives != null) parts.Lives.text = LivesText();
            if (parts.Stars != null) parts.Stars.text = StarsText();
        }

        #endregion
    }
}
