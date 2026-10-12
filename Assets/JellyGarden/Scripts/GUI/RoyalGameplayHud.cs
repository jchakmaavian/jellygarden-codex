using UnityEngine;
using UnityEngine.UI;

namespace RoyalThrone.Presentation
{
    // The gameplay screen's HUD, drawn to the approved reference (Design/GameplayHud/reference.png): a royal-blue backdrop;
    // the Goal panel, the king's arched portrait and the Moves panel on one line at the top; the board inside a thin gold
    // frame (RoyalBoardSurface); one rounded bar of round buttons at the bottom (the four tools and settings). Only the
    // containers are drawn here. The goal icons and numbers, the king, the tool art and the settings gear are the game's own,
    // placed by RoyalGameplayPresentation. Units are the HUD canvas's (720 x 1280 reference, 1.5 px each at 1080 x 1920);
    // the sizes are the reference's, measured at 1080 x 1920.
    public static class RoyalGameplayHud
    {
        // The top row, in the header box (720 x 270 at the top of the safe area; y from the box's centre).
        public const float PanelX = 227.7f, PanelY = -34, PanelWidth = 203.3f, PanelHeight = 194, PanelBulge = 13;
        // The cream body (its tan border included) and the lower ends of the header ribbon, from the panel's centre.
        public const float BodyY = -21.3f, BodyWidth = 187, BodyHeight = 135.4f, HeaderBottom = 20, HeadingY = 70;
        // The cream's top rises like the ribbon over it, and its top corners are tall, so the ribbon's ends wrap down round them.
        const float BodyRise = 5, BodyCornerX = 15, BodyCornerY = 22, BodyCornerBottom = 13.5f;
        public const float PortraitY = -16.65f, PortraitWidth = 224, PortraitHeight = 235.3f, PortraitRise = 77, Ring = 7.3f;
        // The bottom bar: its centre sits RailY above the bottom of the safe area; the buttons sit on its centre line.
        public const float RailY = 98.5f, BarWidth = 692, BarHeight = 143, ButtonSize = 116, ButtonStride = 137.5f;
        public const float BadgeSize = 45, BadgeX = 40.7f, BadgeY = -35.7f;

        public struct Panel { public RectTransform root, body; public Text heading; }
        public struct Portrait { public RectTransform root, slot; }

        // ---- Top row ---------------------------------------------------------------------------------------------------

        // The Goal or Moves panel, as the reference draws it: a deep-blue frame; over its top a ribbon, a little wider than
        // the frame, with a gold rim along its arch that wraps round the ribbon's ends; and the cream body set into the
        // ribbon, whose ends hang down round the cream's tall top corners. Flat, saturated colours with one-unit bevel lines,
        // no gloss. `body` is where the goals or the number go; it is drawn above the panel's layers.
        public static Panel BuildPanel(Transform header, string name, float x, string title, Font font)
        {
            var root = Box(header, name, x, PanelY, PanelWidth, PanelHeight);
            float w = PanelWidth, h = PanelHeight, top = h * .5f, bottom = -top;
            const float corner = 12, rim = 6.3f;   // the ribbon's rounded top ends; the gold rim's depth at the middle
            float rw = w + 3, rb = HeaderBottom;
            Arch(root, "Shadow", 0, -5, rw + 6, h + 6, corner + 3, PanelBulge, 18, C("021648", .55f), C("021648", .55f)).softness = 14;

            // The frame: a navy outline, deep blue that is darker under the ribbon, and a light line round the cream.
            float frameTop = rb + 20, fy = (frameTop + bottom) * .5f, fh = frameTop - bottom;
            Shape(root, "Outline", 0, fy, w + 2, fh + 2, 16, C("0a2a6c"), C("08235c"));
            Shape(root, "Frame", 0, fy, w, fh, 15, C("0a4aac"), C("0a63d0"));
            Body(root, "Frame light", 2.6f, 4.6f, 2.6f, C("3d95f2", .75f), C("3d95f2", .75f)).band = 1.1f;

            // The ribbon. Its navy outline shows below its ends as their underside, and a darker blue down their sides.
            Arch(root, "Header outline", 0, (top + 1 + rb - 1) * .5f, rw + 2, top - rb + 2, corner + 1, PanelBulge, 5, C("0a2a6c"), C("0a2a6c"));
            float blueBottom = rb + 2.5f;
            Arch(root, "Header side", 0, (top + blueBottom) * .5f, rw, top - blueBottom, corner, PanelBulge, 3, C("0848aa"), C("053c94"));
            // The gold: a pale top line, polished gold and a deeper orange line where the blue starts. The gold reaches a short
            // way down the ribbon's ends, round the corners, and tapers away there (its rounded lower corners).
            float goldBottom = top - PanelBulge - corner - 16;
            Arch(root, "Header gold outline", 0, (top + 1 + goldBottom) * .5f, rw + 2, top + 1 - goldBottom, corner + 1, PanelBulge, 9, C("9a5a0c"), C("9a5a0c"));
            Arch(root, "Header gold", 0, (top + goldBottom) * .5f, rw, top - goldBottom, corner, PanelBulge, 8, C("ffd84a"), C("e89a18"));
            var glint = Arch(root, "Header gold light", 0, (top - .6f + goldBottom) * .5f, rw - 1.2f, top - .6f - goldBottom, corner - .6f, PanelBulge, 7.4f, C("fff7c0", .95f), C("fff7c0", 0));
            glint.band = 1.2f;
            // The blue sits `rim` under the gold and 1.2 inside it at the ends, so the rim runs the length of the arch.
            float blueTop = top - rim, bw = rw - 2.4f;
            Arch(root, "Header gold shade", 0, (blueTop + 1.3f + goldBottom) * .5f, bw + .6f, blueTop + 1.3f - goldBottom, corner - 1, PanelBulge, 0, C("b8650e"), C("b8650e"));
            Arch(root, "Header", 0, (blueTop + blueBottom) * .5f, bw, blueTop - blueBottom, corner - 1.2f, PanelBulge, 3, C("0a5ccb"), C("0350b7"));
            var sheen = Arch(root, "Header light", 0, blueTop - .7f - 12, bw - 1.4f, 24, corner - 1.9f, PanelBulge, 0, C("3a9cff", .85f), C("3a9cff", 0));
            sheen.band = 1.1f;

            // The cream, set into the ribbon: a navy lip where they meet (a thin line down the sides), a grey-tan bevel that is
            // deeper along the bottom, a light line inside it, the flat cream and the ribbon's soft shade along its top.
            Body(root, "Cream lip", 1.5f, 3.5f, 1.5f, C("0a2a6c"), C("0a2a6c"));
            Body(root, "Cream edge", 0, 0, 0, C("c4b193"), C("a08a72"));
            Body(root, "Cream light", -4, -1.6f, -4.5f, C("fffbe8"), C("fff6dc"));
            Body(root, "Cream", -5, -2.4f, -5.6f, C("f9f0d3"), C("f6eacb"));
            var shade = Body(root, "Cream shade", -5, -2.4f, -5.6f, C("8b6d44", .3f), C("8b6d44", 0));
            shade.band = .5f; shade.innerSoftness = 9;

            var heading = Words(root, "Heading", title, 0, HeadingY, 180, 27, font);
            Style(heading, C("ffffff"), C("e6eefa"), Color.clear, 0, C("0a2a6c", .45f), .6f, C("031a52", .75f), new Vector2(0, -2.4f));
            var body = Box(root, "Body", 0, BodyY, BodyWidth, BodyHeight);
            return new Panel { root = root, body = body, heading = heading };
        }

        // A shape with the cream body's outline, grown by `side` at the sides, `up` at the top and `down` at the bottom
        // (negative: inset), its corners grown with it.
        static RoyalArchShape Body(Transform root, string name, float side, float up, float down, Color from, Color to)
        {
            float t = BodyY + BodyHeight * .5f + up, b = BodyY - BodyHeight * .5f - down;
            var shape = Arch(root, name, 0, (t + b) * .5f, BodyWidth + 2 * side, t - b, 0, BodyRise, Mathf.Max(0, BodyCornerBottom + side), from, to);
            shape.topRadius = Mathf.Max(0, BodyCornerX + side); shape.topRise = Mathf.Max(0, BodyCornerY + up);
            return shape;
        }

        // The king's portrait: an arched window (a half-ellipse over a rounded base) in a polished gold ring, on a deep
        // royal-blue ground with a soft glow. `slot` covers the window and comes before the ring, so the king (placed there by
        // the caller) is framed by it.
        public static Portrait BuildPortrait(Transform header)
        {
            float w = PortraitWidth, h = PortraitHeight, rise = PortraitRise, rb = 14.7f;
            var root = Box(header, "Royal HUD portrait", 0, PortraitY, w, h);
            ArchTop(root, "Shadow", 0, -5, w + 6, h + 6, rise + 3, rb + 3, C("021648", .5f), C("021648", .5f)).softness = 14;
            ArchTop(root, "Outline", 0, 0, w + 3, h + 3, rise + 1.5f, rb + 1.5f, C("8f5410"), C("6e3a07"));
            float ww = w - 2 * Ring, wh = h - 2 * Ring;
            ArchTop(root, "Window", 0, 0, ww, wh, rise - Ring, rb - Ring, C("2b7ae6"), C("0c3592"));
            // A soft glow behind the king's head, kept inside the window.
            var glow = Shape(root, "Window glow", 0, 14, ww * .62f, wh * .5f, ww * .31f, C("62b0ff", .75f), C("62b0ff", .75f));
            glow.softness = ww * .3f;
            var slot = Box(root, "King slot", 0, 0, ww, wh);
            // The ring on top: polished gold with a light inner edge and a soft shade just inside the window.
            var shade = ArchTop(root, "Window shade", 0, 0, ww, wh, rise - Ring, rb - Ring, C("020d33", .5f), C("020d33", .5f));
            shade.band = 2; shade.innerSoftness = 9;
            var ring = ArchTop(root, "Ring", 0, 0, w, h, rise, rb, C("ffd94a"), C("e39418"));
            ring.band = Ring;
            var light = ArchTop(root, "Ring light", 0, 0, w - 1, h - 1, rise - .5f, rb - .5f, C("fff4b8", .7f), C("fff1b0", .2f));
            light.band = 1.6f;
            var inner = ArchTop(root, "Ring inner edge", 0, 0, ww + 2, wh + 2, rise - Ring + 1, rb - Ring + 1, C("a8650f"), C("8a4e0b"));
            inner.band = 1.4f;
            return new Portrait { root = root, slot = slot };
        }

        // The portrait window's shape as a mask (not drawn), for King Leo's avatar inside the slot.
        public static RoyalArchShape WindowMask(Transform slot, string name)
        {
            float ww = PortraitWidth - 2 * Ring, wh = PortraitHeight - 2 * Ring;
            var mask = ArchTop(slot, name, 0, 0, ww, wh, PortraitRise - Ring, 14.7f - Ring, Color.white, Color.white);
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            return mask;
        }

        // ---- Bottom bar ------------------------------------------------------------------------------------------------

        // One rounded bar behind the five buttons, with a light upper edge.
        public static void BuildBar(Transform rail)
        {
            var bar = Box(rail, "Royal HUD bar", 0, 0, BarWidth, BarHeight);
            bar.SetAsFirstSibling();
            float r = BarHeight * .5f;
            Shape(bar, "Shadow", 0, -6, BarWidth + 8, BarHeight + 8, r + 4, C("021648", .5f), C("021648", .5f)).softness = 16;
            Shape(bar, "Outline", 0, 0, BarWidth + 3, BarHeight + 3, r + 1.5f, C("0a2c72"), C("082259"));
            Shape(bar, "Rim light", 0, 0, BarWidth, BarHeight, r, C("6cc0fa"), C("2a66c8"));
            Shape(bar, "Body", 0, -1.25f, BarWidth, BarHeight - 2.5f, r - 1.25f, C("2c70d8"), C("1a53bb"));
            Shape(bar, "Gloss", 0, BarHeight * .22f, BarWidth - 40, BarHeight * .42f, BarHeight * .21f, C("ffffff", .08f), C("ffffff", 0));
        }

        // A round button's face (the tools green, settings blue): a thin blue outer rim, a thick polished gold ring and the
        // disc with a soft light. Drawn into `face`, a container filling the button; the art goes on top.
        public static void BuildButton(Transform face, bool settings)
        {
            float d = ButtonSize;
            var shadow = Circle(face, "Shadow", 0, -3, d + 4, C("021648", .5f), C("021648", .5f)); shadow.softness = 8;
            Circle(face, "Rim", 0, 0, d, C("12449e"), C("0a2d78"));
            Circle(face, "Gold edge", 0, 0, d - 3, C("c8720f"), C("a4580a"));
            Circle(face, "Gold", 0, 0, d - 5, C("ffe25c"), C("f0a41c"));
            var gloss = Circle(face, "Gold light", 0, 0, d - 5, C("fffbe0", .75f), C("fffbe0", 0)); gloss.band = 3;
            Circle(face, "Dark line", 0, 0, d - 23, C("9a540c"), C("7a3f08"));
            Circle(face, "Disc", 0, 0, d - 25, settings ? C("2f86f2") : C("3fbf3a"), settings ? C("1248b8") : C("178a22"));
            var glow = Circle(face, "Disc glow", 0, 8, d * .5f, settings ? C("8cc4ff", .28f) : C("9af07a", .45f), settings ? C("8cc4ff", .28f) : C("9af07a", .45f));
            glow.softness = d * .22f;
        }

        // A button's badge at its lower right: the red count, or the green + when the tool is used up. `plate` is the
        // badge's object (its text stays the live count); the badge art is inserted beneath its children.
        public static void BuildBadge(RectTransform plate, bool plus)
        {
            Place(plate, BadgeX, BadgeY, BadgeSize, BadgeSize);
            var art = Box(plate, "Royal HUD badge", 0, 0, BadgeSize, BadgeSize);
            art.SetAsFirstSibling();
            var shadow = Circle(art, "Shadow", 0, -1.5f, BadgeSize + 2, C("1a0206", .45f), C("1a0206", .45f)); shadow.softness = 5;
            Circle(art, "Edge", 0, 0, BadgeSize, C("ffffff"), C("ffe8e6"));
            Circle(art, "Face", 0, 0, BadgeSize - 4, plus ? C("3cc896") : C("ff3b30"), plus ? C("067a52") : C("cc1a14"));
            var shine = Shape(art, "Shine", 0, 9, 26, 12, 6, C("ffffff", .38f), C("ffffff", 0)); shine.softness = 4;
            if (plus)
            {
                var glyph = Box(art, "Plus", 0, 0, 22, 22);
                Shape(glyph, "Across", 0, 0, 22, 6.4f, 3.2f, C("fffaea"), C("f1e0b6"));
                Shape(glyph, "Down", 0, 0, 6.4f, 22, 3.2f, C("fffaea"), C("f1e0b6"));
            }
        }

        // The live count on a badge: white, bold, centred.
        public static void StyleCount(Text count, Font font)
        {
            Place(count.transform, 0, 32 * CapDrop, BadgeSize, BadgeSize);
            count.font = font; count.fontSize = 32; count.resizeTextForBestFit = true; count.resizeTextMinSize = 14; count.resizeTextMaxSize = 32;
            count.alignment = TextAnchor.MiddleCenter; count.color = Color.white; count.raycastTarget = false;
            count.transform.SetAsLastSibling();
            Style(count, C("ffffff"), C("fff4f2"), Color.clear, 0, Color.clear, 0, C("6a0606", .55f), new Vector2(0, -1.4f));
        }

        // ---- Backdrop and board frame ----------------------------------------------------------------------------------

        static Sprite backdrop;
        static Texture2D frameEdge, frameCorners;

        // The backdrop behind the board: brightest down the middle and towards the top, deep navy at the sides (measured
        // from the reference). A small bilinear texture, one unit wide and two tall, stretched over the camera's view.
        public static Sprite Backdrop()
        {
            if (backdrop) return backdrop;
            const int w = 96, h = 192;
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = "Royal HUD backdrop", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave };
            Color centreTop = C("2b79da"), centreBottom = C("1964c5"), edgeTop = C("11418e"), edgeBottom = C("0c357d");
            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                float v = y / (h - 1f);
                Color centre = Color.Lerp(centreBottom, centreTop, v), edge = Color.Lerp(edgeBottom, edgeTop, v);
                for (int x = 0; x < w; x++)
                {
                    float d = (x + .5f) / w - .5f, f = Mathf.Exp(-(d * d) / (2 * .235f * .235f));
                    pixels[y * w + x] = Color.Lerp(edge, centre, f);
                }
            }
            texture.SetPixels(pixels); texture.Apply(false, true);
            backdrop = Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(.5f, .5f), w, 0, SpriteMeshType.FullRect);
            backdrop.name = "Royal HUD backdrop"; backdrop.hideFlags = HideFlags.DontSave;
            return backdrop;
        }

        // The board frame's cross-section, from the cells (0) outwards (1): a thin dark-blue inner line, a light gold edge,
        // polished gold, a deeper orange lip and a navy outline that fades over the last few per cent.
        public static Color FrameProfile(float v)
        {
            Color c;
            if (v < .08f) c = C("2a4a9c");
            else if (v < .2f) c = Color.Lerp(C("fff6c4"), C("ffe57e"), (v - .08f) / .12f);
            else if (v < .66f) c = Color.Lerp(C("ffd64a"), C("f6b62c"), (v - .2f) / .46f);
            else if (v < .84f) c = Color.Lerp(C("eb9f22"), C("d7851a"), (v - .66f) / .18f);
            else if (v < .93f) c = C("a8620f");
            else c = C("0d2a62");
            c.a = v > .93f ? Mathf.Clamp01((1 - v) / .07f) : 1;
            return c;
        }

        // The straight frame band: 4 x 64, the cross-section along its height.
        public static Texture2D FrameEdge()
        {
            if (frameEdge) return frameEdge;
            const int w = 4, h = 64;
            frameEdge = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = "Royal board frame edge", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave };
            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++) { var c = FrameProfile((y + .5f) / h); for (int x = 0; x < w; x++) pixels[y * w + x] = c; }
            frameEdge.SetPixels(pixels); frameEdge.Apply(false, true);
            return frameEdge;
        }

        // The frame's corner pieces side by side (a gap between them keeps bilinear sampling apart). Left, an outer corner:
        // a quarter ring about the image's bottom-right, from radius 1 (the cells) to 2 (the outside), in frame-band units.
        // Right, an inner corner: the band turning a concave corner at the image's bottom-right, mitred on the diagonal.
        public const int CornerTile = 64, CornerGap = 8;
        public static Rect OuterCornerUV => TileUV(0);
        public static Rect InnerCornerUV => TileUV(1);
        static Rect TileUV(int tile)
        {
            float width = 2 * CornerTile + CornerGap, x0 = tile * (CornerTile + CornerGap);
            return new Rect((x0 + .5f) / width, .5f / CornerTile, (CornerTile - 1) / width, (CornerTile - 1f) / CornerTile);
        }
        public static Texture2D FrameCorners()
        {
            if (frameCorners) return frameCorners;
            int n = CornerTile, width = 2 * n + CornerGap;
            frameCorners = new Texture2D(width, n, TextureFormat.RGBA32, false) { name = "Royal board frame corners", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave };
            var pixels = new Color[width * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = (x + .5f) / n, v = (y + .5f) / n;
                    // Outer corner: 2 band-widths square, the ring's centre at the bottom-right.
                    float r = Mathf.Sqrt((1 - u) * (1 - u) + v * v) * 2, b = r - 1;
                    Color outer = FrameProfile(Mathf.Clamp01(b));
                    if (b < 0) outer.a *= Mathf.Clamp01(1 + b / .04f);
                    if (b > 1) outer.a = 0;
                    pixels[y * width + x] = outer;
                    // Inner corner: 1 band-width square; the band follows the nearer of the two edges.
                    pixels[y * width + n + CornerGap + x] = FrameProfile(Mathf.Min(1 - u, v));
                }
            frameCorners.SetPixels(pixels); frameCorners.Apply(false, true);
            return frameCorners;
        }

        // ---- Small builders --------------------------------------------------------------------------------------------

        // Centres `t` on its parent's centre at (x, y), w x h, unscaled and unrotated: Royal Throne's RoyalMenuSkin.Place,
        // kept under this name for the HUD's own callers (RoyalGameplayPresentation, ReferenceBoosterBar).
        public static RectTransform Place(Transform t, float x, float y, float w, float h) => RoyalMenuSkin.Place(t, x, y, w, h);

        static RectTransform Box(Transform parent, string name, float x, float y, float w, float h)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(parent, false);
            Place(r, x, y, w, h);
            return r;
        }

        static RoyalCardShape Shape(Transform parent, string name, float x, float y, float w, float h, float radius, Color from, Color to)
        {
            var shape = Box(parent, name, x, y, w, h).gameObject.AddComponent<RoyalCardShape>();
            shape.raycastTarget = false; shape.from = from; shape.to = to; shape.radius = radius;
            return shape;
        }

        static RoyalCardShape Circle(Transform parent, string name, float x, float y, float d, Color from, Color to) => Shape(parent, name, x, y, d, d, d * .5f, from, to);

        static RoyalArchShape Arch(Transform parent, string name, float x, float y, float w, float h, float corner, float bulge, float bottom, Color from, Color to)
        {
            var shape = Box(parent, name, x, y, w, h).gameObject.AddComponent<RoyalArchShape>();
            shape.raycastTarget = false; shape.from = from; shape.to = to;
            shape.topRadius = shape.topRise = corner; shape.bulge = bulge; shape.bottomRadius = bottom;
            return shape;
        }

        // An arched window: a half-ellipse `rise` tall over the full width, on a base with rounded corners.
        static RoyalArchShape ArchTop(Transform parent, string name, float x, float y, float w, float h, float rise, float bottom, Color from, Color to)
        {
            var shape = Arch(parent, name, x, y, w, h, w * .5f, 0, bottom, from, to);
            shape.topRise = rise;
            return shape;
        }

        static Text Words(Transform parent, string name, string value, float x, float capY, float width, float size, Font font)
        {
            var r = Box(parent, name, x, capY + size * CapDrop, width, size * 1.4f);
            var text = r.gameObject.AddComponent<Text>();
            text.font = font; text.text = value; text.fontSize = Mathf.RoundToInt(size); text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow; text.verticalOverflow = VerticalWrapMode.Overflow;
            text.color = Color.white; text.raycastTarget = false;
            return text;
        }

        // A centred line of the display font draws its capitals about a tenth of the font size below the rect's centre.
        public const float CapDrop = .1f;

        // A text's gradient, rim, outline and shadow (RoyalTextStyle).
        public static RoyalTextStyle Style(Text text, Color top, Color bottom, Color rim, float rimWidth, Color outline, float outlineWidth, Color shadow, Vector2 shadowOffset)
        {
            var style = text.GetComponent<RoyalTextStyle>();
            if (!style) style = text.gameObject.AddComponent<RoyalTextStyle>();
            style.Set(top, bottom, rim, rimWidth, outline, outlineWidth, shadow, shadowOffset);
            return style;
        }

        static Color C(string hex, float alpha = 1)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            c.a = alpha;
            return c;
        }
    }

    // Press feedback for the gameplay HUD's round buttons: while held the face eases to 0.975 and 88% brightness, and back
    // on release (0.08 s in, 0.12 s out, ease-out), on the unscaled clock. No squash, wobble or bounce. The button's own
    // transform is left alone (the tool bar enlarges the chosen tool on it).
    public sealed class RoyalHudPress : MonoBehaviour
    {
        // The parts that ease down together: the button's face and its badge.
        public RectTransform[] parts;
        RoyalPressTracker press;
        Graphic[] graphics;
        float blend;

        void Awake()
        {
            press = GetComponent<RoyalPressTracker>();
            if (!press) press = gameObject.AddComponent<RoyalPressTracker>();
        }

        void Update()
        {
            if (parts == null) return;
            float target = press && press.Held ? 1 : 0;
            if (blend == target) return;
            blend = Mathf.MoveTowards(blend, target, Time.unscaledDeltaTime / (target > blend ? .08f : .12f));
            float u = 1 - blend;
            Apply(1 - u * u * u);   // ease-out cubic
        }

        void OnDisable() { blend = 0; Apply(0); }

        void Apply(float e)
        {
            if (parts == null) return;
            foreach (var part in parts) if (part) part.localScale = Vector3.one * Mathf.Lerp(1, .975f, e);
            if (graphics == null) graphics = GetComponentsInChildren<Graphic>(true);
            float shade = Mathf.Lerp(1, .88f, e);
            var tint = new Color(shade, shade, shade, 1);
            foreach (var g in graphics) if (g) g.canvasRenderer.SetColor(tint);
        }
    }
}
