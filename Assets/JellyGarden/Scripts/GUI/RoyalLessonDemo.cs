using System;
using System.Collections;
using System.Collections.Generic;
using JellyGarden.Scripts;
using JellyGarden.Scripts.GUI.TutorialStuff;
using JellyGarden.Scripts.Juice;
using RoyalThrone.ReferenceGameplay;
using UnityEngine;
using UnityEngine.UI;
using static RoyalThrone.Presentation.RoyalCardKit;

namespace RoyalThrone.Presentation
{
    // How to Play's lessons (2026-09-30): each page is a small board drawn with the live board's own art (its pieces, power-ups
    // and light sprites) that plays the lesson on a loop. The hand makes the match, the power-up forms where the dragged piece
    // landed, then the hand sets it off. What happens follows the game's rules (CombineManager, RoyalPowerupDirector):
    // - a horizontal four makes a Rocket that clears its row;
    // - a T or L of five makes TNT, which clears 5x5, ring by ring;
    // - a square makes a Propeller, which hits its four neighbours, then flies to a target (a blocker first);
    // - a line of five makes a Color Ball which, swapped with a piece, clears that piece's colour;
    // - a Rocket swapped onto TNT fires three rows and three columns.
    // Everything runs on the unscaled clock (the lessons also open from the pause menu), and a page starts over when it shows.
    public sealed class RoyalLessonDemo : MonoBehaviour
    {
        public const int Count = 6;
        public const float PageWidth = 560, PageHeight = 600, StageWidth = 520, StageHeight = 410, MaxPitch = 80;
        const float TitleY = 250, StageY = 8, RuleY = -252, TitleSize = 34, RuleSize = 23;
        public static readonly string[] Titles = { "MATCH THREE", "MAKE A ROCKET", "MAKE TNT", "MAKE A PROPELLER", "MAKE A COLOR BALL", "COMBINE POWER-UPS" };
        public static readonly string[] Rules =
        {
            "Swap neighbouring pieces to match three.\nEvery valid move brings you closer.",
            "Match four in a line to make a Rocket.\nTap it to clear its row or column.",
            "Match five in a T or L shape for TNT.\nTap it to blast the pieces around it.",
            "Match a square of four for a Propeller.\nIt hits its neighbours, then flies to a target.",
            "Match five in a line for a Color Ball.\nSwap it with a piece to clear that colour.",
            "Swap two power-ups together\nfor a bigger blast!",
        };

        // Each lesson's board, top row first: 0 book, 1 shield, 2 crown, 3 leaf (the four colours of a normal board), R a
        // Rocket, T TNT, X a crate. No layout holds a match before its move; the move makes exactly the lesson's shape; and
        // with SafeColour's refills no loop of the eight it repeats leaves a cascade (checked by simulating them).
        static readonly string[][] Maps =
        {
            new[] { "23123", "31031", "00122", "12310" },
            new[] { "123223", "310231", "001032", "232113" },
            new[] { "1231231", "2320312", "3120123", "1201031", "2310212", "3123123" },
            new[] { "21123X", "212312", "103023", "200101", "321212" },
            new[] { "132132", "210321", "001003", "321210", "213121" },
            new[] { "1231231", "3123123", "2312312", "12RT231", "3121323", "2313122" },
        };
        const int Colours = 4;
        const float Frame = 9, RocketSpeed = 12;   // cells per second (the board flies 15; a little slower reads better here)

        enum Power { None, Rocket, Tnt, Propeller, ColorBall, Crate }
        sealed class Piece
        {
            public RectTransform rt, art;
            public CanvasGroup group;
            public Image image;
            public int colour = -1;
            public Power power;
            public bool horizontal = true;
            public Image fan;
            public bool plain => power == Power.None;
        }

        int lesson, cols, rows, loop;
        float pitch;
        bool built;
        RectTransform field, pieces, lights, air, hand;
        Image handImage;
        Piece[,] grid;
        CanvasGroup board;

        /// <summary>True while the page's loop runs (for checks).</summary>
        public bool Playing { get; private set; }
        /// <summary>Completed loops since the page last showed, and leftover matches found after a refill (both for checks).</summary>
        public int Loops { get; private set; }
        public int Leftovers { get; private set; }
        public int Lesson => lesson;

        // ---- Building ------------------------------------------------------------------------------------------------

        /// <summary>Builds lesson `lesson`'s page on the How to Play card: its title, the board and the rule under it.</summary>
        public static Transform Build(Transform card, int lesson, float pageY)
        {
            var page = new GameObject("Royal lesson " + lesson, typeof(RectTransform)).transform;
            page.SetParent(card, false);
            RoyalMenuSkin.Place(page, 0, pageY, PageWidth, PageHeight);
            new GameObject("Royal styled", typeof(RectTransform)).transform.SetParent(page, false);   // RoyalCards.Lesson leaves it be
            Fitted(page, "Title", Titles[lesson], TitleY, PageWidth - 40, TitleSize, Ink.Navy);
            var rule = Fitted(page, "Rule", Rules[lesson], RuleY, PageWidth - 24, RuleSize, Ink.NavySoft);
            rule.lineSpacing = 1.12f;
            var stage = new GameObject("Stage", typeof(RectTransform)).transform;
            stage.SetParent(page, false);
            RoyalMenuSkin.Place(stage, 0, StageY, StageWidth, StageHeight);
            // Its own canvas: the moving pieces rebuild only the stage's mesh, not the card's.
            stage.gameObject.AddComponent<Canvas>();
            var demo = stage.gameObject.AddComponent<RoyalLessonDemo>();
            demo.lesson = lesson;
            demo.BuildBoard();
            return page;
        }

        // A line (or two) in one ink, shrunk to fit `width` when its natural width is wider.
        static Text Fitted(Transform parent, string name, string value, float capY, float width, float size, Ink ink)
        {
            var text = Words(parent, name, value, 0, capY, width, size, ink);
            float natural = text.preferredWidth;
            if (natural > width)
            {
                size = Mathf.Floor(size * width / natural);
                Paint(text, ink, size);
                RoyalMenuSkin.Place(text.transform, 0, capY + size * CapDrop, width, size * 1.4f);
            }
            // Its rect holds every line (the layout check measures it).
            var rt = (RectTransform)text.transform;
            rt.sizeDelta = new Vector2(width, Mathf.Max(rt.sizeDelta.y, text.preferredHeight));
            return text;
        }

        void BuildBoard()
        {
            var map = Maps[lesson];
            rows = map.Length; cols = map[0].Length;
            // Each board fills the stage, up to 80 units a cell: 65 on the 7 x 6 boards, 78-80 on the smaller ones.
            pitch = Mathf.Floor(Mathf.Min((StageWidth - 2 * Frame) / cols, (StageHeight - 2 * Frame) / rows, MaxPitch));
            float w = cols * pitch, h = rows * pitch;
            var boardRoot = Container(transform, "Board");
            RoyalMenuSkin.Place(boardRoot, 0, 0, w + 2 * Frame, h + 2 * Frame);
            // The live board's frame: a soft shadow, a gold rim with a light along its top, a thin dark line, then the cells.
            var shadow = Shape(boardRoot, "Shadow", 0, -4, w + 2 * Frame + 8, h + 2 * Frame + 8, 20, C("2a1a08", .35f), C("2a1a08", .35f));
            shadow.softness = 10;
            Inset(boardRoot, "Edge", 0, 16, C("b06a14"), C("8e5210"));
            Inset(boardRoot, "Gold", 1.3f, 14.7f, C("ffdc6a"), C("f0ac2c"));
            Strip(boardRoot, "Gold light", true, 1.3f, 12, 10, C("fff4c4", .9f), C("fff4c4", 0));
            Inset(boardRoot, "Dark line", Frame - 1.6f, 9.6f, C("4a5aa6"), C("3c4c98"));
            Inset(boardRoot, "Cells", Frame, 8, C("adbcef"), C("9fb0e7"));
            var lines = Container(boardRoot, "Grid");
            for (int c = 1; c < cols; c++) Shape(lines, "Column line " + c, -w * .5f + c * pitch, 0, 1.6f, h, .8f, C("8194d6", .9f), C("8194d6", .9f));
            for (int r = 1; r < rows; r++) Shape(lines, "Row line " + r, 0, h * .5f - r * pitch, w, 1.6f, .8f, C("8194d6", .9f), C("8194d6", .9f));
            // The pieces and their light are clipped to the cells (new pieces fall in from above); what flies above the board
            // (a lifted Propeller, the hand) is not.
            field = Layer(boardRoot, "Field", w, h);
            field.gameObject.AddComponent<RectMask2D>();
            board = field.gameObject.AddComponent<CanvasGroup>();
            board.blocksRaycasts = false; board.interactable = false;
            pieces = Layer(field, "Pieces", w, h);
            lights = Layer(field, "Light", w, h);
            air = Layer(boardRoot, "Air", w, h);
            var go = new GameObject("Hand", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            hand = (RectTransform)go.transform; hand.SetParent(air, false);
            handImage = go.GetComponent<Image>(); handImage.raycastTarget = false; handImage.preserveAspect = true;
            handImage.sprite = HandSprite();
            hand.pivot = new Vector2(.30f, .88f);   // the fingertip is the point, as in the level lessons
            hand.sizeDelta = new Vector2(pitch * 1.45f, pitch * 1.45f);
            go.GetComponent<CanvasGroup>().alpha = 0;
            built = true;
            if (isActiveAndEnabled) Begin();
        }

        static RectTransform Layer(Transform parent, string name, float w, float h)
        {
            var rt = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(.5f, .5f);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        // The level 1 tutorial's hand (RoyalPowerTutorial uses the same one).
        static Sprite hand1;
        static Sprite HandSprite()
        {
            if (hand1) return hand1;
            var authored = FindAnyObjectByType<HandTutorial>(FindObjectsInactive.Include);
            var sr = authored ? authored.GetComponent<SpriteRenderer>() : null;
            var img = authored ? authored.GetComponent<Image>() : null;
            hand1 = sr && sr.sprite ? sr.sprite : img ? img.sprite : null;
            return hand1;
        }

        // ---- The loop ------------------------------------------------------------------------------------------------

        void OnEnable() { if (built) Begin(); }
        void OnDisable() { StopAllCoroutines(); Playing = false; }

        void Begin()
        {
            StopAllCoroutines();
            Loops = 0; Leftovers = 0; loop = 0;
            Setup();
            StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            Playing = true;
            while (true)
            {
                yield return Tween(.25f, t => board.alpha = t);
                yield return Wait(.45f);
                switch (lesson)
                {
                    case 0: yield return MatchThree(); break;
                    case 1: yield return MakeRocket(); break;
                    case 2: yield return MakeTnt(); break;
                    case 3: yield return MakePropeller(); break;
                    case 4: yield return MakeColorBall(); break;
                    default: yield return Combine(); break;
                }
                yield return Wait(1.1f);
                yield return Tween(.22f, t => board.alpha = 1 - t);
                Loops++; loop = (loop + 1) % 8;   // the eight refills the layouts were checked against
                Setup();
            }
        }

        // The lesson's board, as its map sets it out.
        void Setup()
        {
            for (int i = pieces.childCount - 1; i >= 0; i--) Destroy(pieces.GetChild(i).gameObject);
            for (int i = lights.childCount - 1; i >= 0; i--) Destroy(lights.GetChild(i).gameObject);
            for (int i = air.childCount - 1; i >= 0; i--) if (air.GetChild(i) != hand) Destroy(air.GetChild(i).gameObject);
            grid = new Piece[cols, rows];
            var map = Maps[lesson];
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    grid[c, r] = Make(map[r][c], c, r);
            board.alpha = 0;
            HandVisible(0);
        }

        IEnumerator MatchThree()
        {
            yield return Drag(2, 1, 2, 2);
            yield return Match(new[] { new Vector2Int(0, 2), new Vector2Int(1, 2), new Vector2Int(2, 2) });
            yield return Settle();
        }

        IEnumerator MakeRocket()
        {
            yield return Drag(2, 1, 2, 2);
            var rocket = Merge(new[] { new Vector2Int(0, 2), new Vector2Int(1, 2), new Vector2Int(3, 2) }, new Vector2Int(2, 2), Power.Rocket);
            yield return Wait(.55f);
            yield return Settle();
            yield return Wait(.35f);
            var at = Where(rocket);
            yield return Tap(at);
            yield return Rocket(at, rocket.horizontal, rocket);
            yield return Settle();
        }

        IEnumerator MakeTnt()
        {
            yield return Drag(3, 4, 3, 3);
            var tnt = Merge(new[] { new Vector2Int(2, 3), new Vector2Int(4, 3), new Vector2Int(3, 1), new Vector2Int(3, 2) }, new Vector2Int(3, 3), Power.Tnt);
            yield return Wait(.55f);
            yield return Settle();
            yield return Wait(.35f);
            var at = Where(tnt);
            yield return Tap(at);
            yield return Blast(at, 2);
            yield return Settle();
        }

        IEnumerator MakePropeller()
        {
            yield return Drag(3, 2, 2, 2);
            var propeller = Merge(new[] { new Vector2Int(1, 2), new Vector2Int(1, 3), new Vector2Int(2, 3) }, new Vector2Int(2, 2), Power.Propeller);
            yield return Wait(.55f);
            yield return Settle();
            yield return Wait(.35f);
            var at = Where(propeller);
            yield return Tap(at);
            yield return Propel(at, Target());
            yield return Settle();
        }

        IEnumerator MakeColorBall()
        {
            yield return Drag(2, 1, 2, 2);
            var ball = Merge(new[] { new Vector2Int(0, 2), new Vector2Int(1, 2), new Vector2Int(3, 2), new Vector2Int(4, 2) }, new Vector2Int(2, 2), Power.ColorBall);
            yield return Wait(.55f);
            yield return Settle();
            yield return Wait(.35f);
            // Swapped with the piece below it: the ball fires from where it lands, at that piece's colour.
            var at = Where(ball);
            var partner = new Vector2Int(at.x, at.y + 1);
            int colour = grid[partner.x, partner.y].colour;
            yield return Drag(at.x, at.y, partner.x, partner.y);
            yield return Zap(partner, colour);
            yield return Settle();
        }

        IEnumerator Combine()
        {
            // The Rocket dragged onto the TNT: the pair goes off where the Rocket lands.
            yield return Drag(2, 3, 3, 3);
            var origin = new Vector2Int(3, 3);
            var a = grid[3, 3]; var b = grid[2, 3];
            grid[3, 3] = null; grid[2, 3] = null;
            var fuse = new List<Piece> { a, b };
            Vector2 at = Cell(origin.x, origin.y);
            yield return Tween(.3f, t =>
            {
                foreach (var p in fuse)
                {
                    p.rt.anchoredPosition = Vector2.Lerp(p.rt.anchoredPosition, at, t);
                    p.rt.localScale = Vector3.one * (1 + .25f * Mathf.Sin(t * Mathf.PI * .5f));
                    p.art.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * Mathf.PI * 8) * 7 * (1 - t));
                }
            });
            foreach (var p in fuse) Destroy(p.rt.gameObject);
            Flash(at, 3.2f, C("fff0c0"), .4f);
            Ring(at, 4.5f, C("ffd070"), .45f);
            // Three rows and three columns through the origin, all at once.
            for (int d = -1; d <= 1; d++)
            {
                StartCoroutine(Rocket(new Vector2Int(origin.x, origin.y + d), true, null));
                StartCoroutine(Rocket(new Vector2Int(origin.x + d, origin.y), false, null));
            }
            yield return Wait(Mathf.Max(cols, rows) / RocketSpeed + .35f);
            yield return Settle();
        }

        // ---- Pieces --------------------------------------------------------------------------------------------------

        Vector2 Cell(int c, int r) => new Vector2((c - (cols - 1) * .5f) * pitch, ((rows - 1) * .5f - r) * pitch);
        Vector2 Cell(Vector2Int at) => Cell(at.x, at.y);
        bool Inside(int c, int r) => c >= 0 && r >= 0 && c < cols && r < rows;

        Vector2Int Where(Piece p)
        {
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    if (grid[c, r] == p) return new Vector2Int(c, r);
            return new Vector2Int(-1, -1);
        }

        Piece Make(char code, int c, int r)
        {
            var p = new Piece();
            if (code >= '0' && code <= '9') p.colour = code - '0';
            else p.power = code == 'R' ? Power.Rocket : code == 'T' ? Power.Tnt : code == 'P' ? Power.Propeller : code == 'C' ? Power.ColorBall : code == 'X' ? Power.Crate : Power.None;
            var go = new GameObject("Piece " + code, typeof(RectTransform), typeof(CanvasGroup));
            p.rt = (RectTransform)go.transform; p.rt.SetParent(pieces, false);
            p.rt.anchorMin = p.rt.anchorMax = p.rt.pivot = new Vector2(.5f, .5f);
            p.rt.sizeDelta = new Vector2(pitch, pitch);
            p.rt.anchoredPosition = Cell(c, r);
            p.group = go.GetComponent<CanvasGroup>(); p.group.blocksRaycasts = false; p.group.interactable = false;
            // The board's contact shadow (RoyalPieceSkin): the same ellipse under every piece.
            if (p.power != Power.Crate)
            {
                var shadow = Shape(p.rt, "Shadow", 0, -pitch * .40f, pitch * .66f, pitch * .22f, pitch * .11f, new Color(.02f, .05f, .16f, .30f), new Color(.02f, .05f, .16f, .30f));
                shadow.softness = pitch * .08f;
            }
            p.art = Layer(p.rt, "Art", pitch, pitch);
            switch (p.power)
            {
                case Power.None: Sprite(p, RoyalGameplayArt.For(ItemsTypes.NONE, p.colour)); break;
                case Power.Rocket: Sprite(p, RoyalGameplayArt.For(ItemsTypes.HORIZONTAL_STRIPPED, 0)); break;
                case Power.Tnt: Sprite(p, RoyalGameplayArt.For(ItemsTypes.PACKAGE, 0)); break;
                case Power.ColorBall: Sprite(p, RoyalGameplayArt.For(ItemsTypes.BOMB, 0)); break;
                case Power.Crate: Sprite(p, ReferenceGameplayCatalog.Art("crate") ?? RoyalPieceSkin.Art("crate")); break;
                case Power.Propeller: PropellerArt(p); break;
            }
            return p;
        }

        // A sprite at the size and pivot the board gives it: imported art by its own pixels per cell, older art fitted.
        Image Sprite(Piece p, Sprite sprite) => p.image = Art(p.art, "Image", sprite, Vector2.zero);

        Image Art(RectTransform parent, string name, Sprite sprite, Vector2 cellOffset)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform; rt.SetParent(parent, false);
            var image = go.GetComponent<Image>(); image.raycastTarget = false;
            Fit(image, sprite);
            rt.anchoredPosition = cellOffset * pitch;
            return image;
        }

        // `native`: the sprite is imported board art (drawn by its own pixels per cell) even when it was loaded by path.
        void Fit(Image image, Sprite sprite, bool native = false)
        {
            image.sprite = sprite; image.enabled = sprite;
            if (!sprite) return;
            var rt = image.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
            if (native || ReferenceGameplayCatalog.IsNative(sprite))
            {
                rt.sizeDelta = sprite.rect.size / sprite.pixelsPerUnit * pitch;
                rt.pivot = new Vector2(sprite.pivot.x / sprite.rect.width, sprite.pivot.y / sprite.rect.height);
            }
            else
            {
                float k = pitch * RoyalPieceSkin.CellFill / Mathf.Max(sprite.rect.width, sprite.rect.height);
                rt.sizeDelta = sprite.rect.size * k;
                rt.pivot = new Vector2(.5f, .5f);
            }
        }

        // The board's Propeller (ReferenceGameplay/Visuals/Propeller): the body, the pin and the fan above them, as the prefab
        // places them (in cells); a single sprite when the imported parts are missing.
        void PropellerArt(Piece p)
        {
            var body = ReferenceGameplayCatalog.Art("propeller-body");
            var fan = ReferenceGameplayCatalog.Art("propeller-blades-idle");
            if (!body || !fan) { Sprite(p, RoyalGameplayArt.For(ItemsTypes.HELICOPTER, 0)); return; }
            Art(p.art, "Body", body, new Vector2(-.01f, -.095f));
            var pin = ReferenceGameplayCatalog.Art("propeller-pin");
            if (pin) Art(p.art, "Pin", pin, new Vector2(.022f, .066f));
            p.fan = Art(p.art, "Fan", fan, new Vector2(.008f, .116f));
        }

        // The Propeller's spin frames: its sharp turn (01-07), then the blurred loop, then the blur disc.
        static Sprite[] turn, blur, disc;
        static Sprite[] Frames(params string[] names)
        {
            var list = new List<Sprite>();
            foreach (var n in names) { var s = Resources.Load<Sprite>("ReferenceGameplay/Sprites/Items-SpecialItems-PropellerItem-" + n); if (s) list.Add(s); }
            return list.ToArray();
        }
        static void LoadSpin()
        {
            if (turn != null) return;
            turn = Frames("01__17013", "02__21800", "03__16929", "04__16744", "05__22175", "06__22109", "07__16374");
            blur = Frames("donme_loop_00000__21225", "donme_loop_00001__21186", "donme_loop_00002__21824", "donme_loop_00003__21703");
            disc = Frames("2d_pervane__00000__21239", "2d_pervane__00001__21492", "2d_pervane__00002__21409");
        }

        // ---- Light (the board's additive sprites) --------------------------------------------------------------------

        static Material additive;
        static Material Additive()
        {
            if (additive) return additive;
            var shader = Resources.Load<Shader>("RoyalLessons/UIAdditive");
            if (shader) additive = new Material(shader) { name = "Royal lesson light" };
            return additive;
        }

        Image Light(string key, Vector2 at, float size, Color tint, Transform parent = null)
        {
            var sprite = ReferenceGameplayCatalog.Art(key);
            var material = Additive();
            if (!sprite || !material) return null;
            var go = new GameObject("Light " + key, typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform; rt.SetParent(parent ? parent : lights, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(.5f, .5f);
            rt.sizeDelta = new Vector2(size, size); rt.anchoredPosition = at;
            var image = go.GetComponent<Image>(); image.sprite = sprite; image.material = material; image.color = tint; image.raycastTarget = false;
            return image;
        }

        // A soft flash that swells and fades (`size` in cells).
        void Flash(Vector2 at, float size, Color tint, float time, string key = "glow")
        {
            var light = Light(key, at, pitch * size, tint);
            if (light) StartCoroutine(Fade(light, time, .55f, 1.15f));
        }

        // A ring that opens out to `size` cells.
        void Ring(Vector2 at, float size, Color tint, float time)
        {
            var light = Light("clear-ring", at, pitch * size, tint);
            if (light) StartCoroutine(Fade(light, time, .25f, 1));
        }

        IEnumerator Fade(Image light, float time, float from, float to)
        {
            var rt = light.rectTransform; var tint = light.color;
            yield return Tween(time, t =>
            {
                if (!light) return;
                rt.localScale = Vector3.one * Mathf.Lerp(from, to, OutCubic(t));
                light.color = new Color(tint.r, tint.g, tint.b, tint.a * (1 - t * t));
            });
            if (light) Destroy(light.gameObject);
        }

        // Five stars thrown out of a cleared cell, in the piece's colour.
        void Sparks(Vector2 at, Color tint, int count = 5)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = (i + .5f) / count * Mathf.PI * 2 + (loop * 1.3f + at.x * .01f);
                var star = Light("sparkle-01", at, pitch * .26f, tint);
                if (star) StartCoroutine(Spark(star, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * pitch * (.6f + .25f * ((i * 7) % 3) / 2f)));
            }
        }

        IEnumerator Spark(Image star, Vector2 travel)
        {
            var rt = star.rectTransform; Vector2 from = rt.anchoredPosition; var tint = star.color;
            float spin = (travel.x >= 0 ? 1 : -1) * 160;
            yield return Tween(.42f, t =>
            {
                if (!star) return;
                float e = OutCubic(t);
                rt.anchoredPosition = from + travel * e;
                rt.localRotation = Quaternion.Euler(0, 0, spin * t);
                rt.localScale = Vector3.one * Mathf.Lerp(1.1f, .35f, t);
                star.color = new Color(tint.r, tint.g, tint.b, 1 - t);
            });
            if (star) Destroy(star.gameObject);
        }

        static readonly Color[] Tints = { C("ff6a55"), C("5aa8ff"), C("ffd445"), C("6fe46a") };
        static Color Tint(Piece p) => p.plain && p.colour >= 0 && p.colour < Tints.Length ? Tints[p.colour] : p.power == Power.Crate ? C("ffb45a") : C("fff0b0");

        // ---- Moves ---------------------------------------------------------------------------------------------------

        // The hand drags the piece at (c0, r0) onto its neighbour (c1, r1); the two trade places.
        IEnumerator Drag(int c0, int r0, int c1, int r1)
        {
            Vector2 a = Cell(c0, r0), b = Cell(c1, r1);
            yield return HandIn(a);
            yield return HandPress(true);
            var dragged = grid[c0, r0]; var other = grid[c1, r1];
            dragged.rt.SetAsLastSibling();
            yield return Tween(.24f, t =>
            {
                float e = InOutQuad(t);
                dragged.rt.anchoredPosition = Vector2.Lerp(a, b, e);
                if (other != null) other.rt.anchoredPosition = Vector2.Lerp(b, a, e);
                hand.anchoredPosition = Vector2.Lerp(a, b, e);
            });
            grid[c1, r1] = dragged; grid[c0, r0] = other;
            StartCoroutine(HandOut());
        }

        IEnumerator Tap(Vector2Int at)
        {
            yield return HandIn(Cell(at));
            yield return HandPress(true);
            StartCoroutine(HandOut());
        }

        IEnumerator HandIn(Vector2 at)
        {
            if (!handImage.sprite) yield break;
            Vector2 from = at + new Vector2(pitch * .9f, -pitch * 1.1f);
            hand.localScale = Vector3.one;
            yield return Tween(.3f, t => { hand.anchoredPosition = Vector2.Lerp(from, at, OutCubic(t)); HandVisible(t); });
        }

        IEnumerator HandPress(bool down)
        {
            if (!handImage.sprite) yield break;
            yield return Tween(.1f, t => hand.localScale = Vector3.one * Mathf.Lerp(1, .86f, down ? t : 1 - t));
        }

        IEnumerator HandOut()
        {
            if (!handImage.sprite) yield break;
            yield return HandPress(false);
            Vector2 from = hand.anchoredPosition, to = from + new Vector2(pitch * .6f, -pitch * .8f);
            yield return Tween(.22f, t => { hand.anchoredPosition = Vector2.Lerp(from, to, t); HandVisible(1 - t); });
        }

        void HandVisible(float alpha) { var g = hand.GetComponent<CanvasGroup>(); if (g) g.alpha = alpha; }

        // ---- Clears --------------------------------------------------------------------------------------------------

        IEnumerator Match(IEnumerable<Vector2Int> cells)
        {
            foreach (var at in cells) Hit(at.x, at.y, 0);
            yield return Wait(.35f);
        }

        // The matched pieces run into `into` and the power-up forms there.
        Piece Merge(IEnumerable<Vector2Int> from, Vector2Int into, Power power)
        {
            var target = Cell(into);
            var group = new List<Piece>();
            foreach (var at in from) { var p = grid[at.x, at.y]; if (p != null) group.Add(p); grid[at.x, at.y] = null; }
            var host = grid[into.x, into.y]; if (host != null) group.Add(host);
            var made = Make(power == Power.Rocket ? 'R' : power == Power.Tnt ? 'T' : power == Power.Propeller ? 'P' : 'C', into.x, into.y);
            grid[into.x, into.y] = made;
            made.rt.localScale = Vector3.zero;
            StartCoroutine(MergeRoutine(group, target, made));
            return made;
        }

        IEnumerator MergeRoutine(List<Piece> group, Vector2 target, Piece made)
        {
            var starts = new List<Vector2>(); foreach (var p in group) starts.Add(p.rt.anchoredPosition);
            yield return Tween(.18f, t =>
            {
                float e = t * t;
                for (int i = 0; i < group.Count; i++)
                {
                    group[i].rt.anchoredPosition = Vector2.Lerp(starts[i], target, e);
                    group[i].rt.localScale = Vector3.one * Mathf.Lerp(1, .7f, e);
                }
            });
            foreach (var p in group) Destroy(p.rt.gameObject);
            Flash(target, 2.2f, C("fff4d0", .95f), .35f);
            Ring(target, 2.6f, C("ffd36a"), .4f);
            Sparks(target, C("ffe07a"), 6);
            yield return Tween(.32f, t => made.rt.localScale = Vector3.one * OutBack(t));
        }

        // Clears the piece at (c, r) after `delay` (a blocker breaks the same way).
        void Hit(int c, int r, float delay)
        {
            if (!Inside(c, r)) return;
            var p = grid[c, r];
            if (p == null) return;
            grid[c, r] = null;
            StartCoroutine(Pop(p, delay));
        }

        IEnumerator Pop(Piece p, float delay)
        {
            if (delay > 0) yield return Wait(delay);
            Vector2 at = p.rt.anchoredPosition;
            var tint = Tint(p);
            Flash(at, 1.7f, new Color(1, 1, 1, .8f), .28f);
            Sparks(at, tint);
            if (p.power == Power.Crate) Flash(at, 1.5f, C("d9b894", .7f), .45f, "smoke");
            yield return Tween(.07f, t => p.rt.localScale = Vector3.one * Mathf.Lerp(1, 1.14f, t));
            yield return Tween(.13f, t => { p.rt.localScale = Vector3.one * Mathf.Lerp(1.14f, .3f, t * t); p.group.alpha = 1 - t; });
            Destroy(p.rt.gameObject);
        }

        // ---- Power-ups -----------------------------------------------------------------------------------------------

        // A Rocket at `at` (or a line fired from there) splits in two, the heads fly apart along its row or column and clear
        // each cell as they pass it, the origin first.
        IEnumerator Rocket(Vector2Int at, bool horizontal, Piece rocket)
        {
            if (rocket != null)
            {
                grid[at.x, at.y] = null;
                yield return Tween(.12f, t => rocket.rt.localScale = Vector3.one * (1 + .12f * Mathf.Sin(t * Mathf.PI)));   // the wind-up
                Destroy(rocket.rt.gameObject);
            }
            else Hit(at.x, at.y, 0);
            Vector2 origin = Cell(at);
            Flash(origin, 1.8f, C("fff0c8", .9f), .3f, "hit-flash");
            var heads = new List<RectTransform>(); var trails = new List<Image>(); var dirs = new List<Vector2Int>();
            for (int side = -1; side <= 1; side += 2)
            {
                var dir = horizontal ? new Vector2Int(side, 0) : new Vector2Int(0, side);
                string key = horizontal ? (side < 0 ? "rocket-left" : "rocket-right") : (side < 0 ? "rocket-up" : "rocket-down");
                var sprite = ReferenceGameplayCatalog.Art(key) ?? RoyalGameplayArt.For(horizontal ? ItemsTypes.HORIZONTAL_STRIPPED : ItemsTypes.VERTICAL_STRIPPED, 0);
                var head = Layer(lights, "Rocket head", pitch, pitch);
                head.anchoredPosition = origin;
                Art(head, "Image", sprite, Vector2.zero);
                heads.Add(head); dirs.Add(dir);
                var trail = Light("beam", origin, pitch, C("ffc070", .9f));
                if (trail)
                {
                    var tr = trail.rectTransform; tr.pivot = new Vector2(0, .5f);
                    float angle = Mathf.Atan2(-dir.y, dir.x) * Mathf.Rad2Deg;   // grid rows grow downwards
                    tr.localRotation = Quaternion.Euler(0, 0, angle - 180);
                    trails.Add(trail);
                }
                else trails.Add(null);
            }
            float reach = (horizontal ? cols : rows) + 1.5f, time = reach / RocketSpeed;
            var cleared = new HashSet<int>();
            yield return Tween(time, t =>
            {
                float dist = t * reach;
                for (int i = 0; i < heads.Count; i++)
                {
                    var d = dirs[i];
                    Vector2 step = new Vector2(d.x, -d.y) * pitch;
                    heads[i].anchoredPosition = origin + step * dist;
                    if (trails[i])
                    {
                        var tr = trails[i].rectTransform;
                        tr.anchoredPosition = heads[i].anchoredPosition - step * .25f;
                        tr.sizeDelta = new Vector2(Mathf.Min(dist, 2.6f) * pitch, pitch * .55f);
                    }
                    for (int k = 1; k <= Mathf.FloorToInt(dist + .45f); k++)
                    {
                        int c = at.x + d.x * k, r = at.y + d.y * k;
                        if (!Inside(c, r) || !cleared.Add(c * 100 + r)) continue;
                        Hit(c, r, 0);
                    }
                }
            });
            foreach (var h in heads) Destroy(h.gameObject);
            foreach (var tr in trails) if (tr) StartCoroutine(Fade(tr, .15f, 1, 1));
        }

        // TNT: the fuse wind-up, then a blast that clears the 5 x 5 around it ring by ring (centre, 3 x 3, 5 x 5).
        IEnumerator Blast(Vector2Int at, int radius)
        {
            var tnt = grid[at.x, at.y];
            grid[at.x, at.y] = null;
            yield return Tween(.34f, t =>
            {
                tnt.rt.localScale = Vector3.one * (1 + .16f * t);
                tnt.art.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * Mathf.PI * 7) * 7 * t);
            });
            Vector2 origin = Cell(at);
            Destroy(tnt.rt.gameObject);
            Flash(origin, (2 * radius + 1) * 1.15f, C("fff0c8"), .45f, "hit-flash");
            Flash(origin, (2 * radius + 1) * .9f, C("ffb050", .9f), .5f);
            Ring(origin, (2 * radius + 1) * 1.1f, C("ffd070"), .42f);
            for (int i = 0; i < 4; i++)
                Flash(origin + new Vector2(Mathf.Cos(i * 1.57f + .6f), Mathf.Sin(i * 1.57f + .6f)) * pitch * 1.2f, 1.6f, C("cdbfae", .55f), .6f, "smoke");
            for (int ring = 1; ring <= radius; ring++)
            {
                yield return Wait(.05f);
                for (int r = at.y - ring; r <= at.y + ring; r++)
                    for (int c = at.x - ring; c <= at.x + ring; c++)
                        if (Mathf.Max(Mathf.Abs(c - at.x), Mathf.Abs(r - at.y)) == ring) Hit(c, r, 0);
            }
            yield return Wait(.3f);
        }

        // The Propeller's target: a blocker first, else the farthest piece (RoyalPowerupDirector's Propeller target rule).
        Vector2Int Target()
        {
            var best = new Vector2Int(-1, -1); float score = -1;
            var from = new Vector2Int(-1, -1);
            for (int r = 0; r < rows; r++) for (int c = 0; c < cols; c++) if (grid[c, r] != null && grid[c, r].power == Power.Propeller) from = new Vector2Int(c, r);
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    var p = grid[c, r];
                    if (p == null || p.power == Power.Propeller) continue;
                    float s = (p.power == Power.Crate ? 100 : 0) + Vector2.Distance(new Vector2(c, r), from);
                    if (s > score) { score = s; best = new Vector2Int(c, r); }
                }
            return best;
        }

        // The Propeller spins up, hits its four neighbours, lifts, flies an arc to its target and clears it.
        IEnumerator Propel(Vector2Int at, Vector2Int target)
        {
            LoadSpin();
            var propeller = grid[at.x, at.y];
            grid[at.x, at.y] = null;
            bool spinning = true;
            StartCoroutine(Spin(propeller, () => spinning));
            yield return Wait(.22f);
            Hit(at.x - 1, at.y, 0); Hit(at.x + 1, at.y, 0); Hit(at.x, at.y - 1, 0); Hit(at.x, at.y + 1, 0);
            Flash(Cell(at), 2.4f, C("fff4d8", .8f), .3f);
            // Off the board's clipping, so the lifted Propeller is never cut by the board's edge.
            propeller.rt.SetParent(air, true);
            propeller.rt.SetAsFirstSibling();
            Vector2 a = Cell(at), b = Inside(target.x, target.y) ? Cell(target) : a + new Vector2(0, pitch * 3);
            var shadow = propeller.rt.Find("Shadow");
            yield return Tween(.2f, t => { propeller.art.localScale = Vector3.one * (1 + .3f * t); propeller.art.anchoredPosition = new Vector2(0, pitch * .18f * t); if (shadow) shadow.localScale = Vector3.one * (1 - .35f * t); });
            Vector2 control = (a + b) * .5f + new Vector2(0, pitch * (1.6f + .15f * Vector2.Distance(a, b) / pitch));
            yield return Tween(.62f, t =>
            {
                float e = InOutQuad(t), u = 1 - e;
                propeller.rt.anchoredPosition = u * u * a + 2 * u * e * control + e * e * b;
            });
            yield return Tween(.1f, t => { propeller.art.localScale = Vector3.one * Mathf.Lerp(1.3f, 1f, t); propeller.art.anchoredPosition = new Vector2(0, pitch * .18f * (1 - t)); });
            spinning = false;
            if (Inside(target.x, target.y)) Hit(target.x, target.y, 0);
            Flash(b, 2.6f, C("fff0c8"), .4f, "hit-flash");
            Ring(b, 2.4f, C("ffd070"), .38f);
            yield return Tween(.14f, t => { propeller.rt.localScale = Vector3.one * (1 - t); propeller.group.alpha = 1 - t; });
            Destroy(propeller.rt.gameObject);
            yield return Wait(.2f);
        }

        IEnumerator Spin(Piece p, Func<bool> running)
        {
            if (!p.fan) yield break;
            float t = 0;
            while (running() && p.fan)
            {
                // The sharp turn for the first 0.2 s, the blurred loop while it lifts, then the blur disc in flight.
                var set = t < .2f ? turn : t < .45f ? blur : disc;
                if (set != null && set.Length > 0)
                {
                    var s = set[Mathf.FloorToInt(t * (t < .2f ? 30 : 24)) % set.Length];
                    if (p.fan.sprite != s) Fit(p.fan, s, true);
                }
                yield return null;
                t += Time.unscaledDeltaTime;
            }
        }

        // The Color Ball at `at` charges, fires a beam at every piece of `colour` one after another, the pieces light up, then
        // all go together, and the ball last.
        IEnumerator Zap(Vector2Int at, int colour)
        {
            var ball = grid[at.x, at.y];
            grid[at.x, at.y] = null;
            Vector2 origin = Cell(at);
            var targets = new List<Vector2Int>();
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    if (grid[c, r] != null && grid[c, r].plain && grid[c, r].colour == colour) targets.Add(new Vector2Int(c, r));
            targets.Sort((x, y) => Vector2Int.Distance(x, at).CompareTo(Vector2Int.Distance(y, at)));
            var tint = colour >= 0 && colour < Tints.Length ? Tints[colour] : Color.white;
            bool charging = true;
            StartCoroutine(Charge(ball, () => charging));
            var beams = new List<Image>(); var glows = new List<Image>();
            foreach (var t in targets)
            {
                Vector2 end = Cell(t);
                var beam = Light("beam", origin, pitch, new Color(tint.r, tint.g, tint.b, .95f));
                if (beam)
                {
                    var rt = beam.rectTransform; rt.pivot = new Vector2(1, .5f);
                    Vector2 d = end - origin;
                    rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg + 180);
                    StartCoroutine(Tween(.1f, k => { if (rt) rt.sizeDelta = new Vector2(d.magnitude * OutCubic(k), pitch * .42f); }));
                    beams.Add(beam);
                }
                var glow = Light("glow", end, pitch * 1.5f, new Color(tint.r, tint.g, tint.b, .9f));
                if (glow) glows.Add(glow);
                var piece = grid[t.x, t.y];
                if (piece != null) StartCoroutine(Tween(.12f, k => { if (piece.rt) piece.rt.localScale = Vector3.one * (1 + .12f * OutCubic(k)); }));
                yield return Wait(.124f);
            }
            yield return Wait(.3f);
            charging = false;
            foreach (var t in targets) Hit(t.x, t.y, 0);
            foreach (var b in beams) if (b) StartCoroutine(Fade(b, .18f, 1, 1));
            foreach (var g in glows) if (g) StartCoroutine(Fade(g, .25f, 1, 1.3f));
            Flash(origin, 2.8f, C("fff4dc"), .42f, "hit-flash");
            Ring(origin, 3, new Color(tint.r, tint.g, tint.b, 1), .4f);
            yield return Tween(.16f, t => { ball.rt.localScale = Vector3.one * Mathf.Lerp(1.12f, 1.35f, t); ball.group.alpha = 1 - t; });
            Destroy(ball.rt.gameObject);
            yield return Wait(.2f);
        }

        IEnumerator Charge(Piece ball, Func<bool> running)
        {
            var glow = Light("glow", ball.rt.anchoredPosition, pitch * 1.9f, C("ffffff", .0f));
            float t = 0;
            while (running() && ball.rt)
            {
                float k = Mathf.Min(1, t / .25f);
                ball.rt.localScale = Vector3.one * (1 + .12f * k + .03f * Mathf.Sin(t * 22));
                if (glow) glow.color = new Color(1, 1, 1, .55f * k + .1f * Mathf.Sin(t * 18));
                yield return null;
                t += Time.unscaledDeltaTime;
            }
            if (glow) StartCoroutine(Fade(glow, .2f, 1, 1.2f));
        }

        // ---- Gravity -------------------------------------------------------------------------------------------------

        // The pieces fall into the gaps and new ones drop in from above (blockers stay put); any match the refill makes goes
        // as on the board, so the lesson never leaves one standing.
        IEnumerator Settle()
        {
            for (int pass = 0; pass < 4; pass++)
            {
                yield return Fall();
                var found = Matches();
                if (found.Count == 0) yield break;
                Leftovers++;
                foreach (var at in found) Hit(at.x, at.y, 0);
                yield return Wait(.32f);
            }
        }

        IEnumerator Fall()
        {
            float longest = 0;
            var tops = new int[cols];
            // First every column settles, then the new pieces' colours are chosen against the settled board.
            for (int c = 0; c < cols; c++)
            {
                int write = rows - 1;
                for (int r = rows - 1; r >= 0; r--)
                {
                    var p = grid[c, r];
                    if (p == null) continue;
                    if (p.power == Power.Crate) { write = r - 1; continue; }
                    if (r != write)
                    {
                        grid[c, write] = p; grid[c, r] = null;
                        longest = Mathf.Max(longest, Drop(p, Cell(c, r), Cell(c, write), write - r));
                    }
                    write--;
                }
                tops[c] = write;
            }
            for (int c = 0; c < cols; c++)
            {
                int write = tops[c];
                for (int r = write; r >= 0; r--)
                {
                    if (grid[c, r] != null) continue;
                    var p = Make((char)('0' + SafeColour(c, r)), c, r);
                    grid[c, r] = p;
                    // All of a column's new pieces come down together from just above the board, in order.
                    Vector2 start = Cell(c, r - (write + 1));
                    p.rt.anchoredPosition = start;
                    longest = Mathf.Max(longest, Drop(p, start, Cell(c, r), write + 1));
                }
            }
            if (longest > 0) yield return Wait(longest + .05f);
        }

        float Drop(Piece p, Vector2 from, Vector2 to, int cells)
        {
            float time = .12f + .05f * cells;
            StartCoroutine(DropRoutine(p, from, to, time));
            return time + .1f;
        }

        IEnumerator DropRoutine(Piece p, Vector2 from, Vector2 to, float time)
        {
            yield return Tween(time, t => { if (p.rt) p.rt.anchoredPosition = Vector2.LerpUnclamped(from, to, t * t); });
            // A short squash on landing.
            yield return Tween(.1f, t => { if (p.art) p.art.localScale = new Vector3(1 + .06f * (1 - t), 1 - .1f * (1 - t), 1); });
        }

        // The first colour (in an order mixed from the cell, the lesson and the loop) that makes no line of three and no square.
        int SafeColour(int c, int r)
        {
            uint h = (uint)(c * 73856093) ^ (uint)(r * 19349663) ^ (uint)(loop * 83492791) ^ (uint)(lesson * 2654435761u);
            h ^= h >> 13; h *= 0x5bd1e995; h ^= h >> 15;
            int start = (int)(h % Colours);
            for (int k = 0; k < Colours; k++)
            {
                int colour = (start + k) % Colours;
                if (!Makes(c, r, colour)) return colour;
            }
            return start;
        }

        bool Same(int c, int r, int colour) => Inside(c, r) && grid[c, r] != null && grid[c, r].plain && grid[c, r].colour == colour;

        bool Makes(int c, int r, int colour)
        {
            if (Same(c - 1, r, colour) && (Same(c - 2, r, colour) || Same(c + 1, r, colour))) return true;
            if (Same(c + 1, r, colour) && Same(c + 2, r, colour)) return true;
            if (Same(c, r - 1, colour) && (Same(c, r - 2, colour) || Same(c, r + 1, colour))) return true;
            if (Same(c, r + 1, colour) && Same(c, r + 2, colour)) return true;
            for (int dx = -1; dx <= 0; dx++)
                for (int dy = -1; dy <= 0; dy++)
                {
                    int n = 0;
                    for (int x = 0; x < 2; x++) for (int y = 0; y < 2; y++) { int cc = c + dx + x, rr = r + dy + y; if (cc == c && rr == r) continue; if (Same(cc, rr, colour)) n++; }
                    if (n == 3) return true;
                }
            return false;
        }

        // Every plain piece standing in a line of three or more.
        List<Vector2Int> Matches()
        {
            var found = new HashSet<Vector2Int>();
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    var p = grid[c, r];
                    if (p == null || !p.plain) continue;
                    if (Same(c + 1, r, p.colour) && Same(c + 2, r, p.colour)) for (int k = 0; Same(c + k, r, p.colour); k++) found.Add(new Vector2Int(c + k, r));
                    if (Same(c, r + 1, p.colour) && Same(c, r + 2, p.colour)) for (int k = 0; Same(c, r + k, p.colour); k++) found.Add(new Vector2Int(c, r + k));
                }
            return new List<Vector2Int>(found);
        }

        // ---- Timing --------------------------------------------------------------------------------------------------

        static IEnumerator Tween(float time, Action<float> step)
        {
            float t = 0;
            while (t < time) { step(t / time); yield return null; t += Time.unscaledDeltaTime; }
            step(1);
        }

        static IEnumerator Wait(float time)
        {
            float t = 0;
            while (t < time) { yield return null; t += Time.unscaledDeltaTime; }
        }

        static float OutCubic(float x) { float u = 1 - x; return 1 - u * u * u; }
        static float InOutQuad(float x) => x < .5f ? 2 * x * x : 1 - 2 * (1 - x) * (1 - x);
        static float OutBack(float x) { const float s = 1.9f; float u = x - 1; return 1 + u * u * ((s + 1) * u + s); }
    }
}
