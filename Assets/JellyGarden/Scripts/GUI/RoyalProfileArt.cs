using System.Collections.Generic;
using JellyGarden.Scripts;
using JellyGarden.Scripts.Juice;
using UnityEngine;
using UnityEngine.UI;
using static RoyalThrone.Presentation.RoyalCardKit;

namespace RoyalThrone.Presentation
{
    // The profile's art (2026-09-30), drawn with the card kit. It holds:
    // - the avatars: King Vanguard (his approved "excited" pose), the corgi and the steward, cropped to their heads, plus
    //   every sprite placed in Resources/RoyalProfile/Avatars (square portraits, shown whole) — new avatars need no code;
    // - the five frames and four name styles the player can choose;
    // - the framed avatar tile, the level pennant, the drawn icons and the tabs the Profile page and its Edit card share.
    public sealed class RoyalAvatar
    {
        public string id, name;
        public Sprite sprite;
        public Vector2 centre = new Vector2(.5f, .5f);   // the crop's centre, from the image's top-left, in image fractions
        public float size = 1;                          // the crop's side, as a fraction of the image's width
        public Color from, to;                          // the tile's background behind the art
    }

    public sealed class RoyalFrame { public string name; public Color edge, from, to, light; }
    public sealed class RoyalNameStyle { public string name; public Color top, bottom, edge; }

    public static class RoyalProfileArt
    {
        public const string AvatarFolder = "RoyalProfile/Avatars";

        public static readonly RoyalFrame[] Frames =
        {
            new RoyalFrame { name = "Gold", edge = C("7a3a12"), from = C("ffe27a"), to = C("d99a22"), light = C("fffbe0", .7f) },
            new RoyalFrame { name = "Sapphire", edge = C("0a2466"), from = C("8ccfff"), to = C("2a6be0"), light = C("e6f4ff", .7f) },
            new RoyalFrame { name = "Emerald", edge = C("0b4a1c"), from = C("8ef59a"), to = C("1f9a3c"), light = C("eaffe8", .7f) },
            new RoyalFrame { name = "Amethyst", edge = C("3a0d6a"), from = C("e4b4ff"), to = C("8a3ad6"), light = C("f6ebff", .7f) },
            new RoyalFrame { name = "Ruby", edge = C("5a0a14"), from = C("ff9a8a"), to = C("c8232e"), light = C("fff0ec", .7f) },
        };

        public static readonly RoyalNameStyle[] NameStyles =
        {
            new RoyalNameStyle { name = "Royal Gold", top = C("fffbf0"), bottom = C("ffe4a8"), edge = C("7a3a0c") },
            new RoyalNameStyle { name = "Ivory", top = C("ffffff"), bottom = C("e9eef8"), edge = C("0b2766") },
            new RoyalNameStyle { name = "Sapphire", top = C("e2f2ff"), bottom = C("86c8ff"), edge = C("0a2466") },
            new RoyalNameStyle { name = "Rose", top = C("fff0f4"), bottom = C("ffa6c2"), edge = C("6a0c2e") },
        };

        static List<RoyalAvatar> avatars;

        /// <summary>Every avatar the player can choose, the built-in three first.</summary>
        public static List<RoyalAvatar> Avatars
        {
            get
            {
                if (avatars != null && (avatars.Count == 0 || avatars[0].sprite)) return avatars;
                avatars = new List<RoyalAvatar>();
                Add("king", "King Vanguard", Resources.Load<Sprite>("RoyalLeaderboard/king-venguard-excited"), new Vector2(.45f, .165f), .55f, C("4a8cf0"), C("163f9e"));
                Add("corgi", "Royal Corgi", RoyalPieceSkin.CampaignArt("dog"), new Vector2(.43f, .28f), .56f, C("ffd76a"), C("ef8a2a"));
                Add("steward", "The Steward", RoyalPieceSkin.CampaignArt("steward"), new Vector2(.39f, .12f), .36f, C("4fd0bd"), C("137a82"));
                var extra = new List<Sprite>(Resources.LoadAll<Sprite>(AvatarFolder));
                extra.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
                foreach (var s in extra) Add(s.name, Pretty(s.name), s, new Vector2(.5f, .5f), 1, C("5a8ee8"), C("1a3f9a"));
                return avatars;
            }
        }

        static void Add(string id, string name, Sprite sprite, Vector2 centre, float size, Color from, Color to)
        {
            if (!sprite) return;
            avatars.Add(new RoyalAvatar { id = id, name = name, sprite = sprite, centre = centre, size = size, from = from, to = to });
        }

        // "court-knight" -> "Court Knight"
        static string Pretty(string file)
        {
            var words = file.Replace('_', ' ').Replace('-', ' ').Split(' ');
            for (int i = 0; i < words.Length; i++) if (words[i].Length > 0) words[i] = char.ToUpperInvariant(words[i][0]) + words[i].Substring(1);
            return string.Join(" ", words).Trim();
        }

        public static RoyalAvatar Find(string id)
        {
            foreach (var a in Avatars) if (a.id == id) return a;
            return null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { avatars = null; }

        // ---- The framed avatar ---------------------------------------------------------------------------------------

        /// <summary>A rounded avatar tile `size` wide: a soft shadow, the frame (edge, rim, light, dark line), and the portrait
        /// masked to the inside: the avatar's background, a soft light behind the head, the art cropped to its head.</summary>
        public static RectTransform Tile(Transform parent, string name, float x, float y, float size, RoyalAvatar avatar, int frame)
        {
            var tile = (RectTransform)Container(parent, name);
            RoyalMenuSkin.Place(tile, x, y, size, size);
            float r = size * .2f, b = size * .075f;
            var shadow = Shape(tile, "Shadow", 0, -size * .025f, size + 6, size + 6, r + 3, C("000a30", .45f), C("000a30", .45f), Squircle);
            shadow.softness = size * .06f;
            var rim = Container(tile, "Frame");
            Inset(rim, "Edge", 0, r, Color.black, Color.black, Squircle);
            Inset(rim, "Rim", 1.2f, r - 1.2f, Color.white, Color.white, Squircle);
            Inset(rim, "Rim light", 1.2f, r - 1.2f, Color.white, Color.white, Squircle);
            Inset(rim, "Dark line", b - 1.6f, r - b + 1.6f, C("041a52"), C("041a52"), Squircle);
            var portrait = Inset(tile, "Portrait", b, r - b, Color.white, Color.white, Squircle);
            portrait.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            var glow = Shape(portrait.transform, "Glow", 0, size * .12f, size * .8f, size * .8f, size * .4f, C("ffffff", .32f), C("ffffff", .2f));
            glow.softness = size * .3f;
            var artGo = new GameObject("Art", typeof(RectTransform), typeof(Image));
            artGo.transform.SetParent(portrait.transform, false);
            artGo.GetComponent<Image>().raycastTarget = false;
            // A light along the frame's inner top edge, over the portrait.
            var sheen = Strip(tile, "Sheen", true, b, size * .2f, r - b, C("ffffff", .16f), C("ffffff", 0), Squircle);
            sheen.rectTransform.SetAsLastSibling();
            SetFrame(tile, frame);
            SetAvatar(tile, avatar);
            return tile;
        }

        public static void SetFrame(RectTransform tile, int frame)
        {
            var f = Frames[Mathf.Clamp(frame, 0, Frames.Length - 1)];
            Tint(tile, "Frame/Edge", f.edge, f.edge);
            Tint(tile, "Frame/Rim", f.from, f.to);
            Tint(tile, "Frame/Rim light", f.light, new Color(f.light.r, f.light.g, f.light.b, 0));
        }

        /// <summary>Shows `avatar` in the tile (null: an empty royal-blue portrait with a faint crown, the frame swatches' look).</summary>
        public static void SetAvatar(RectTransform tile, RoyalAvatar avatar)
        {
            var portrait = tile.Find("Portrait") as RectTransform;
            var art = portrait ? portrait.Find("Art").GetComponent<Image>() : null;
            if (!art) return;
            float inner = portrait.rect.width > 0 ? portrait.rect.width : tile.sizeDelta.x * .85f;
            if (avatar == null)
            {
                Tint(tile, "Portrait", C("1a4aa8"), C("0c2a78"));
                art.sprite = RoyalGameplayArt.For(ItemsTypes.NONE, 2);   // the board's crown, as a faint emblem
                art.color = new Color(.02f, .1f, .35f, .55f); art.preserveAspect = true;
                RoyalMenuSkin.Place(art.transform, 0, 0, inner * .55f, inner * .55f);
                portrait.Find("Glow").gameObject.SetActive(false);
                return;
            }
            Tint(tile, "Portrait", avatar.from, avatar.to);
            portrait.Find("Glow").gameObject.SetActive(true);
            var s = avatar.sprite; art.sprite = s; art.color = Color.white; art.preserveAspect = false;
            float cropPx = Mathf.Max(1, avatar.size * s.rect.width), k = inner / cropPx;
            float w = s.rect.width * k, h = s.rect.height * k;
            RoyalMenuSkin.Place(art.transform, (.5f - avatar.centre.x) * w, (avatar.centre.y - .5f) * h, w, h);
        }

        static void Tint(Transform root, string path, Color from, Color to)
        {
            var t = root.Find(path); var shape = t ? t.GetComponent<RoyalCardShape>() : null;
            if (!shape) return;
            if (shape.from == from && shape.to == to) return;
            shape.from = from; shape.to = to; shape.SetVerticesDirty();
        }

        // ---- Lettering -----------------------------------------------------------------------------------------------

        /// <summary>The name in one of the four styles: one fill, a thin edge in its dark, a short soft shadow (the clean inks).</summary>
        public static void StyleName(Text text, int style, float size)
        {
            var s = NameStyles[Mathf.Clamp(style, 0, NameStyles.Length - 1)];
            Style(text, s.top, s.bottom, Color.clear, 0, s.edge, size * .036f, C("1c0a02", .45f), new Vector2(0, -Mathf.Max(1.2f, size * .045f)), size * .02f);
        }

        /// <summary>A text placed by its left edge and the centre of its capitals, shrunk to fit `width`.</summary>
        public static Text Left(Transform parent, string name, string value, float left, float capY, float width, float size, Ink ink)
        {
            var text = Words(parent, name, value, left + width * .5f, capY, width, size, ink);
            text.alignment = TextAnchor.MiddleLeft;
            Fit(text, width, size, ink, left);
            return text;
        }

        public static void Fit(Text text, float width, float size, Ink ink, float left)
        {
            var rt = (RectTransform)text.transform;
            float capY = rt.anchoredPosition.y - text.fontSize * CapDrop;
            // Measured at the full size, so a shorter name grows back after a longer one was shrunk.
            if (Mathf.Abs(text.fontSize - size) > .5f) Paint(text, ink, size);
            float natural = text.preferredWidth, used = size;
            if (natural > width) { used = Mathf.Floor(size * width / natural); Paint(text, ink, used); }
            RoyalMenuSkin.Place(rt, left + width * .5f, capY + used * CapDrop, width, used * 1.4f);
        }

        // ---- The level pennant ---------------------------------------------------------------------------------------

        /// <summary>A royal-blue pennant with a gold rim hanging from `top` (its flat top edge), "Level" over the level number;
        /// its point is a diamond whose lower half shows below the body. Returns the number's Text.</summary>
        public static Text Pennant(Transform parent, float x, float top, float w, float body, int level)
        {
            float h = body + w * .5f, rim = 6;
            var p = Container(parent, "Royal level pennant");
            RoyalMenuSkin.Place(p, x, top - h * .5f, w, h);
            float bodyY = h * .5f - body * .5f, pointY = h * .5f - body;
            var shadow = Container(p, "Shadow");
            Shape(shadow, "Body", 3, bodyY - 5, w, body, 6, C("000a30", .4f), C("000a30", .4f)).softness = 8;
            var ds = Shape(shadow, "Point", 3, pointY - 5, w * .7071f, w * .7071f, 6, C("000a30", .4f), C("000a30", .4f)); ds.softness = 8;
            ds.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
            Layer(p, "Edge", w, body, 0, bodyY, pointY, C("7a3a12"), C("5a2a0e"), 6);
            Layer(p, "Gold", w - 2.4f, body - 1.2f, 0, bodyY - .6f, pointY, C("ffe27a"), C("e3a02a"), 5);
            Layer(p, "Navy line", w - 2 * (rim - 1), body - (rim - 1), 0, bodyY - (rim - 1) * .5f, pointY, C("0a2c7c"), C("061f5c"), 4);
            Layer(p, "Face", w - 2 * rim, body - rim, 0, bodyY - rim * .5f, pointY, C("2e66d4"), C("1440a2"), 3);
            var gloss = Shape(p, "Gloss", 0, h * .5f - rim - body * .18f, w - 2 * rim - 8, body * .3f, 4, C("ffffff", .22f), C("ffffff", .02f));
            Words(p, "Royal level label", "Level", 0, h * .5f - body * .3f, w - 10, 26, Ink.Light);
            return Words(p, "Royal level number", level.ToString(), 0, h * .5f - body * .66f, w - 10, 46, Ink.Display);
        }

        // One layer of the pennant: its body and its point, the same colours, the point's side set by the body's width.
        static void Layer(Transform p, string name, float w, float body, float x, float bodyY, float pointY, Color from, Color to, float r)
        {
            var layer = Container(p, name);
            Shape(layer, "Body", x, bodyY, w, body, r, from, to);
            var d = Shape(layer, "Point", x, pointY, w * .7071f, w * .7071f, r, to, to);
            d.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
            d.rectTransform.SetAsFirstSibling();
        }

        // ---- Drawn icons (cream, like the Settings page's) -----------------------------------------------------------

        /// <summary>A pencil, pointing down-left: the Edit badge's and the name field's mark.</summary>
        public static void Pencil(Transform parent, float size, Color a, Color b)
        {
            var icon = Container(parent, "Royal pencil");
            RoyalMenuSkin.Place(icon, 0, 0, size, size);
            var body = Shape(icon, "Body", size * .06f, size * .06f, size * .26f, size * .7f, size * .05f, a, b);
            body.rectTransform.localRotation = Quaternion.Euler(0, 0, -45);
            var tip = Shape(icon, "Tip", -size * .26f, -size * .26f, size * .17f, size * .17f, size * .03f, a, b);
            tip.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
            var band = Shape(icon, "Band", size * .19f, size * .19f, size * .28f, size * .06f, size * .02f, new Color(0, 0, 0, .25f), new Color(0, 0, 0, .25f));
            band.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
        }

        /// <summary>A calendar: a frame, its top band with two rings, and a grid of days.</summary>
        public static void Calendar(Transform parent, float x, float y, float size)
        {
            var icon = Container(parent, "Royal calendar");
            RoyalMenuSkin.Place(icon, x, y, size, size);
            Color a = C("fffaf0"), b = C("ecdcb8");
            var frame = Shape(icon, "Frame", 0, -size * .04f, size * .92f, size * .8f, size * .12f, a, b); frame.band = size * .08f;
            Shape(icon, "Band", 0, size * .22f, size * .92f, size * .2f, size * .08f, a, b);
            for (int i = -1; i <= 1; i += 2) Shape(icon, "Ring " + i, i * size * .22f, size * .38f, size * .09f, size * .2f, size * .045f, a, b);
            for (int r = 0; r < 2; r++)
                for (int c = 0; c < 3; c++)
                    Shape(icon, "Day " + r + c, (c - 1) * size * .22f, -size * .1f - r * size * .2f, size * .13f, size * .12f, size * .03f, a, b);
        }

        /// <summary>A sprite in a box, aspect kept (a stat's or a row's icon).</summary>
        public static Image Picture(Transform parent, string name, Sprite sprite, float x, float y, float size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite; image.preserveAspect = true; image.raycastTarget = false; image.enabled = sprite;
            RoyalMenuSkin.Place(go.transform, x, y, size, size);
            return image;
        }

        public static Sprite HomeArt(int id) => Resources.Load<Sprite>("ReferenceGameplay/Home/" + id);

        // ---- Tabs (the Leaderboard page's segmented control) ---------------------------------------------------------

        public static Transform Bar(Transform parent, string name, float y, float width, float height)
        {
            var bar = Container(parent, name);
            RoyalMenuSkin.Place(bar, 0, y, width, height);
            float r = height * .31f;
            var shadow = Shape(bar, "Shadow", 0, -4, width + 8, height + 8, r + 4, C("000a30", .35f), C("000a30", .35f));
            shadow.softness = 12;
            Inset(bar, "Rim", 0, r, C("6a96ea", .6f), C("2a58c0", .5f));
            Inset(bar, "Fill", 2.2f, r - 2.2f, C("0a2a78"), C("08225f"));
            Strip(bar, "Fill shade", true, 2.2f, height * .3f, r - 2.2f, C("000a30", .3f), C("000a30", 0));
            return bar;
        }

        /// <summary>One tab: its "On" look (bright blue in the gold rim) and "Off" look (a darker blue, a quieter label).</summary>
        public static Transform Segment(Transform bar, string name, string caption, float x, float w, float h, float size, UnityEngine.Events.UnityAction choose)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(bar, false);
            go.GetComponent<Image>().color = Color.clear;
            var button = go.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(choose);
            RoyalMenuSkin.Place(go.transform, x, 0, w, h);
            var skin = Container(go.transform, "Royal skin");
            float r = h * .3f;
            var on = Container(skin, "On");
            Inset(on, "Edge", 0, r, C("8c4a14"), C("6a3410"));
            Inset(on, "Gold", 1.1f, r - 1.1f, C("ffe27a"), C("e6a22a"));
            Inset(on, "Gold light", 1.1f, r - 1.1f, C("fffbe0", .6f), C("fffbe0", 0));
            Inset(on, "Dark line", 4.6f, r - 4.6f, C("0a2466"), C("071a4c"));
            Inset(on, "Face", 5.9f, r - 5.9f, C("3d8df6"), C("1b54cc"));
            float lens = (h - 11.8f) * .42f;
            var gloss = Strip(on, "Gloss", true, 7.9f, lens, lens * .5f, C("ffffff", .2f), C("ffffff", .03f));
            gloss.rectTransform.sizeDelta = new Vector2(-2 * (5.9f + (r - 5.9f) * .45f), lens);
            Words(on, "Label", caption, 0, 1, w - 16, size, Ink.OnBlue);
            var off = Container(skin, "Off");
            Inset(off, "Face", 0, r, C("15398a"), C("0d2a6c"));
            var light = Inset(off, "Light", 0, r, C("7aa2f0", .35f), C("7aa2f0", 0));
            light.band = 1.2f;
            var offLabel = Words(off, "Label", caption, 0, 1, w - 16, size - 2, Ink.Light);
            Style(offLabel, C("d9e4fb"), C("bccbee"), Color.clear, 0, Color.clear, 0, C("041a52", .45f), new Vector2(0, -1.4f), size * .01f);
            return go.transform;
        }

        public static void Choose(Transform segment, bool chosen)
        {
            var on = segment.Find("Royal skin/On"); var off = segment.Find("Royal skin/Off");
            if (on && on.gameObject.activeSelf != chosen) on.gameObject.SetActive(chosen);
            if (off && off.gameObject.activeSelf == chosen) off.gameObject.SetActive(!chosen);
        }

        /// <summary>The choice mark: a green ring around a tile and a green tick badge on its lower-right corner.</summary>
        public static Transform Chosen(Transform tile, float size)
        {
            var mark = Container(tile, "Royal chosen");
            RoyalMenuSkin.Place(mark, 0, 0, size, size);
            var ring = Shape(mark, "Ring", 0, 0, size + 16, size + 16, size * .2f + 8, C("7bea4a"), C("2fa836"), Squircle);
            ring.band = 7;
            var badge = Container(mark, "Badge");
            Badge(badge, true, size * .42f, -size * .42f);
            Glyph(badge, "Royal tick", true);
            mark.SetAsLastSibling();
            return mark;
        }
    }
}
