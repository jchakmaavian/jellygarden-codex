using JellyGarden.Scripts.Juice;
using JellyGarden.Scripts.Targets;
using UnityEngine;
using UnityEngine.UI;
using static RoyalThrone.Presentation.RoyalCardKit;

namespace RoyalThrone.Presentation
{
    // The Level Start card (MenuPlay), laid out to the supplied reference (Design/LevelStart/reference.png). Reference
    // pixels map to card units at 0.735 about the reference card's centre (533.5, 751.5), which makes the card 664 x 862,
    // 83% of the 800-unit canvas width as in the reference. Its frame, crest, lettering, panels and buttons are the shared
    // RoyalCardKit parts every card uses; this adds the level header, the goals, the boosters and PLAY.
    // The card's own objects (Level, Close, Play, Info, Boost1-3, Targets) keep their callbacks and remain the source of
    // truth: this only styles and places them. RoyalLevelCardMotion carries the entrance, highlights and press feedback.
    public static class RoyalLevelCard
    {
        public const float Height = 862;
        public const float GoalsY = 153.4f, BoostY = -109.9f, BoostSize = 176, BoostPitch = 194, PlayY = -318.3f;
        public static readonly float[] BoostX = { -BoostPitch, 0, BoostPitch };

        public static void Build(Transform menu, Transform card)
        {
            Hide(card.Find("Offline"));
            Hide(card.Find("TargetDescription1")); Hide(card.Find("TargetDescription2"));
            for (int s = 1; s <= 3; s++) Hide(card.Find("Star" + s));
            // The inherited entrance clip squashes the card from 1.64 x 0.53 to 1; RoyalLevelCardMotion plays a calm
            // entrance instead and fires that clip's events (its two click ticks and OnFinished) at the clip's times.
            var entrance = menu.GetComponent<Animation>();
            if (entrance) { entrance.playAutomatically = false; entrance.Stop(); }

            var skin = Card(menu, card, Height, null);
            ButtonShadow(skin, 0, PlayY, 435, 120);
            Ornament(skin, Height);

            var motion = menu.gameObject.AddComponent<RoyalLevelCardMotion>();
            motion.dim = menu.GetComponent<Image>(); motion.dimColor = DimColor;
            motion.card = (RectTransform)card;
            motion.cardGroup = card.GetComponent<CanvasGroup>();
            if (!motion.cardGroup) motion.cardGroup = card.gameObject.AddComponent<CanvasGroup>();

            // LEVEL over the number, on the ribbon. The number's dark halo is face shading, so it sits under the crest.
            var halo = Shape(skin, "Level halo", 0, 318, 160, 108, 44, C("001a5e", .5f), C("001a5e", .5f));
            halo.softness = 58;
            skin.Find("Crest").SetAsLastSibling();
            motion.halo = halo.rectTransform;
            var heading = Words(card, "Royal heading", "LEVEL", 0, 386.6f, 300, 32);
            Style(heading, C("fbf8dc"), C("f8d27c"), C("c87a28"), 1, C("0c1540"), 1.3f, C("001a5c", .85f), new Vector2(0, -2.6f));
            var number = card.Find("Level");
            if (number)
            {
                var text = number.GetComponent<Text>();
                motion.number = text;
                RoyalMenuSkin.Place(number, 0, 318.6f + 92 * CapDrop, 360, 120);
                Plain(text, 92);
                Style(text, C("fffdf0"), C("f5cf82"), C("e8901c"), 2, C("1b1030"), 1.8f, C("2a1206", .95f), new Vector2(0, -3.6f));
            }

            var panel = Cream(card, "Royal goal panel", 0, GoalsY, 589.5f, 203.2f);
            panel.SetSiblingIndex(card.Find("Royal skin").GetSiblingIndex() + 1);
            Separators(panel, 92.6f);
            var goals = Words(card, "Royal goals heading", "LEVEL GOALS", 0, 224.9f, 360, 24, Ink.HeadingOnCream);
            HeadingLines(panel, goals, 224.9f - GoalsY, 120, 232, C("cfa261"), C("fffae0", .8f));
            RoyalMenuSkin.Place(card.Find("Targets"), 0, GoalsY, 590, 203);

            Section(card, "Royal boosters heading", "START WITH A BOOST", 12.9f, 234);

            var close = Close(card.Find("Close"), Height);
            // The ? keeps an 80-unit-wide tap area; 68 tall so it stays clear of the TNT card below.
            var info = Control(card.Find("Info"), 265.3f, 12.9f, 80, 47, 68);
            if (info) HelpFace(info);

            string[] art = { "orb", "rocket", "tnt" }, names = { "COLOR BALL", "ROCKET", "TNT" };
            // The board's own Color Ball, Rocket and TNT (RoyalPieceSkin.Art): the sphere and the barrel fill their squares.
            float[] artSize = { 130, 132, 142 };
            motion.boosters = new RoyalLevelCardMotion.Booster[3];
            for (int i = 0; i < 3; i++)
            {
                Words(card, "Royal boost name " + (i + 1), names[i], BoostX[i], -213.2f, 170, 18, Ink.Label);
                motion.boosters[i] = Booster(card.Find("Boost" + (i + 1)), art[i], artSize[i], i);
            }

            var play = Control(card.Find("Play"), 0, PlayY, 435, 0, 120);
            if (play)
            {
                var face = play.Find("Royal skin");
                Face(face, 120, Kind.Primary);
                motion.playSweep = face.Find("Sweep").GetComponent<RoyalSweepGraphic>();
                motion.playGold = face.Find("Gold").GetComponent<RoyalCardShape>();
                var caption = Words(play, "Royal caption", "PLAY", 0, 1.5f, 400, 47);
                Style(caption, C("fffdf3"), C("f4e1b9"), Color.clear, 0, C("01281f"), 1.8f, C("00382c", .9f), new Vector2(0, -3.2f));
            }

            motion.close = Feedback(close, false);
            motion.help = Feedback(info, false);
            motion.play = Feedback(play, true);
            motion.reveals = new[]
            {
                Reveal(.05f, card, "Royal heading", "Level"),
                Reveal(.10f, card, "Royal goal panel", "Royal goals heading", "Targets"),
                Reveal(.15f, card, "Royal boosters heading", "Royal boosters heading lines", "Info", "Boost1", "Boost2", "Boost3",
                    "Royal boost name 1", "Royal boost name 2", "Royal boost name 3"),
                Reveal(.20f, card, "Play"),
            };
            motion.targets = card.Find("Targets");
        }

        // A goal column on the goals panel: the icon over its amount. RoyalMenuPresenter calls this every few frames,
        // because the goals are created each time the card opens.
        public static void Goal(TargetIcon target, int index, int count)
        {
            if (!target) return;
            float pitch = Mathf.Min(175, 560f / Mathf.Max(1, count));
            float x = (index - (count - 1) * .5f) * pitch;
            RoyalMenuSkin.Place(target.transform, x, 0, pitch - 8, 203);
            var icon = target.imageRenderer;
            if (icon)
            {
                // The score star fills only 86% x 81% of its square: give it a larger box so it matches the other icons.
                float box = icon.sprite && icon.sprite == RoyalPieceSkin.Art("star") ? 98 : 84;
                RoyalMenuSkin.Place(icon.transform, 0, 1.3f, box, box);
                icon.preserveAspect = true;
            }
            if (target.textObj)
            {
                var text = target.textObj;
                RoyalMenuSkin.Place(text.transform, 0, -60.8f + 30 * CapDrop, pitch - 14, 36);
                if (!text.GetComponent<RoyalTextStyle>())
                {
                    RoyalMenuSkin.Typography(text, 30);
                    Paint(text, Ink.Value, 30);
                }
            }
            if (target.checkObject) RoyalMenuSkin.Place(target.checkObject.transform, 0, -60.8f, 34, 34);
            // Separators between the columns, on the panel.
            var card = target.transform.parent ? target.transform.parent.parent : null;
            ShowSeparators(card ? card.Find("Royal goal panel/Separators") : null, count, 590, -17.8f);
        }

        static RoyalLevelCardMotion.Booster Booster(Transform boost, string artName, float artSize, int index)
        {
            var b = new RoyalLevelCardMotion.Booster();
            if (!boost) return b;
            b.root = (RectTransform)boost;
            var animator = boost.GetComponent<Animator>();
            if (animator) animator.enabled = false;
            var button = boost.GetComponent<Button>();
            if (button) button.transition = Selectable.Transition.None;
            // The booster's own Image stays as the tap area (and keeps its sprite for anything that reads it); the card
            // and the artwork are drawn by children underneath the badge.
            var image = boost.GetComponent<Image>();
            image.sprite = RoyalPieceSkin.Art(artName);
            image.color = Color.clear;
            RoyalMenuSkin.Place(boost, BoostX[index], BoostY, BoostSize, BoostSize);
            var tile = Tile(boost, BoostSize, BoostSize, "Royal booster card");
            const float half = BoostSize * .5f;
            b.glow = Shape(tile, "Glow", 0, 0, BoostSize + 12, BoostSize + 12, half + 6, C("ffd24a"), C("ffb830"), Squircle);
            b.glow.softness = 30; b.glow.color = new Color(1, 1, 1, 0);
            b.glow.transform.SetSiblingIndex(0);
            b.rimOuter = tile.Find("Rim outer").GetComponent<RoyalCardShape>();
            b.rim = tile.Find("Rim").GetComponent<RoyalCardShape>();
            b.rimFrom = b.rim.from; b.rimTo = b.rim.to;
            b.sweep = Container(tile, "Sweep").gameObject.AddComponent<RoyalSweepGraphic>();
            Stretch(b.sweep.rectTransform, 7.4f);
            b.sweep.radius = half - 7.4f; b.sweep.exponent = Squircle; b.sweep.raycastTarget = false; b.sweep.width = .2f;
            var art = RoyalMenuSkin.Art(boost, "Royal booster art", artName, 0, 0, artSize, artSize);
            art.transform.SetSiblingIndex(1);
            b.art = art.rectTransform;

            // The badge sits at the same point of every card: the +, the count, or the tick once the booster is chosen.
            var indicator = boost.Find("Indicator");
            if (indicator)
            {
                RoyalMenuSkin.Place(indicator, 0, 0, BoostSize, BoostSize);
                var plus = indicator.Find("Plus");
                if (plus)
                {
                    Badge(plus, true);
                    Glyph(plus, "Royal plus", false);
                    b.plus = (RectTransform)plus;
                }
                var plate = indicator.Find("Image");
                if (plate)
                {
                    Badge(plate, false);
                    b.plateFace = plate.Find("Royal badge/Face").GetComponent<RoyalCardShape>();
                    var count = plate.Find("Count");
                    if (count)
                    {
                        var text = count.GetComponent<Text>();
                        text.enabled = true;
                        RoyalMenuSkin.Place(count, 0, 24 * CapDrop, 50, 34);
                        Plain(text, 24);
                        Style(text, C("ffffff"), C("e9e2cc"), Color.clear, 0, C("0a1a4a"), 1.2f, C("000a30", .8f), new Vector2(0, -1.2f));
                        count.SetAsLastSibling();
                    }
                    var check = plate.Find("Check");
                    if (check)
                    {
                        RoyalMenuSkin.Place(check, 0, 0, 56, 56);
                        var mark = check.GetComponent<Image>();
                        if (mark) mark.color = Color.clear;
                        foreach (var text in check.GetComponentsInChildren<Text>(true)) text.enabled = false;
                        Glyph(check, "Royal tick", true);
                        b.check = (RectTransform)check;
                    }
                    b.plate = (RectTransform)plate;
                }
            }
            // While another booster is active the others are set aside under a dim; it follows the card's outline.
            var locked = boost.Find("Lock");
            if (locked)
            {
                RoyalMenuSkin.Place(locked, 0, 0, BoostSize, BoostSize);
                var dim = locked.GetComponent<Image>();
                if (dim) dim.color = Color.clear;
                foreach (var text in locked.GetComponentsInChildren<Text>(true)) text.enabled = false;
                if (!locked.Find("Royal dim")) Inset(locked, "Royal dim", 0, half, C("020a24", .72f), C("020a24", .72f), Squircle);
            }
            b.press = boost.gameObject.GetComponent<RoyalPressTracker>();
            if (!b.press) b.press = boost.gameObject.AddComponent<RoyalPressTracker>();
            return b;
        }
    }
}
