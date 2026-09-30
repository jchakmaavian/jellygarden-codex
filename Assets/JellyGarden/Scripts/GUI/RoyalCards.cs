using System.Collections.Generic;
using JellyGarden.Scripts.GUI;
using JellyGarden.Scripts.Juice;
using UnityEngine;
using UnityEngine.UI;
using static RoyalThrone.Presentation.RoyalCardKit;

namespace RoyalThrone.Presentation
{
    // Every popup card in the Level Start card's style (RoyalCardKit): the same frame and crest, the title under the ribbon,
    // cream panels and dark-blue tiles for content, green primary and blue secondary buttons, the same close control, and
    // the same calm motion (RoyalCardMotion). Each card keeps its original objects, names and callbacks; only their look and
    // place change. The width is the Level Start card's 664 units; each height fits its content, with the last row ending
    // 52.5 units above the bottom edge as PLAY does, so the diamond ornament sits just below it.
    public static class RoyalCards
    {
        // Rewarded-video offers exist only in builds with an ads SDK; without one AnimationManager hides them on every
        // open, so the cards leave no room for them.
#if UNITY_ADS || GOOGLE_MOBILE_ADS
        static readonly bool VideoOffers = true;
#else
        static readonly bool VideoOffers = false;
#endif
        const float BottomGap = 52.5f;

        // ---- Shops -----------------------------------------------------------------------------------------------------

        public static void Boosters(Transform menu, Transform p)   // BoostShop
        {
            const float H = 780, top = H * .5f;
            var skin = Card(menu, p, H, "ROYAL BOOSTERS");
            var close = Close(p.Find("Close"), H);
            var icon = p.Find("Boost");
            TileBehind(p, "Royal boost tile", -170, top - 262, 164);
            RoyalMenuSkin.Place(icon, -170, top - 262, 128, 128);
            var description = Field(p.Find("BoostDescription"), 125, top - 262, 360, 23, Ink.Body);
            if (description) Wrap(description, 360, 110).alignment = TextAnchor.MiddleLeft;
            var buttons = new List<RoyalCardControl> { Feedback(close, false) };
            for (int i = 1; i <= 3; i++)
            {
                var b = Button(p.Find("BuyBoost" + i), null, 0, top - 400 - (i - 1) * 104, 480, 88, Kind.Primary);
                if (!b) continue;
                Field(b.Find("Count"), -120, 0, 180, 32, Ink.Caption);
                Price(b, 188, 30, Kind.Primary);
                buttons.Add(Feedback(b, true));
            }
            // The coin balance, on a cream pill under the offers.
            var balance = Cream(p, "Royal balance", 0, top - 692, 230, 60, 30);
            Hide(p.Find("Royal balance coin"));
            RoyalMenuSkin.Art(balance, "Royal balance icon", "coin", -58, 0, 44, 44);
            var gems = p.Find("Gems");
            if (gems) { Field(gems, 22, top - 692, 130, 28, Ink.Value); balance.SetSiblingIndex(gems.GetSiblingIndex()); }
            Ornament(skin, H);
            Motion(menu, p, H, buttons, null);
        }

        public static void Coins(Transform menu, Transform p)   // GemsShop
        {
            float H = VideoOffers ? 840 : 760, top = H * .5f;
            var skin = Card(menu, p, H, "ROYAL COINS");
            var close = Close(p.Find("Close"), H);
            // AnimationManager rewrites this line when the store is offline.
            Wrap(Words(p, "Royal subtitle", "A little extra for your next adventure", 0, top - 180, 540, 21, Ink.Body), 560, 60);
            var buttons = new List<RoyalCardControl> { Feedback(close, false) };
            for (int i = 1; i <= 4; i++)
            {
                var pack = p.Find("Pack" + i);
                if (!pack) continue;
                float x = i % 2 == 1 ? -138 : 138, y = i <= 2 ? top - 330 : top - 590;
                RoyalMenuSkin.Place(pack, x, y, 252, 236);
                var image = pack.GetComponent<Image>();
                if (image) image.color = Color.clear;
                Tile(pack, 252, 236, "Royal tile", 56);
                RoyalMenuSkin.Art(pack, "Royal coin", "coin", 0, 52, 96, 90);
                Field(pack.Find("Count"), 0, -10, 200, 36, Ink.Hero);
                var buy = Button(pack.Find("Buy"), null, 0, -76, 204, 62, Kind.Primary);
                if (buy) { Field(buy.Find("Price"), 0, 0, 186, 22, Ink.Caption); buttons.Add(Feedback(buy, true)); }
            }
            var video = p.Find("Video");
            if (VideoOffers) buttons.Add(Feedback(Button(video, "WATCH & EARN", 0, top - 760, 380, 72, Kind.Secondary), false));
            else Hide(video);
            Ornament(skin, H);
            Motion(menu, p, H, buttons, null);
        }

        public static void Lives(Transform menu, Transform p)   // LiveShop
        {
            float H = VideoOffers ? 720 : 612, top = H * .5f;
            var skin = Card(menu, p, H, "MORE LIVES");
            var close = Close(p.Find("Close"), H);
            foreach (Transform t in p) if (t.name == "Label") Hide(t);
            RoyalMenuSkin.Art(p, "Royal heart", "heart", 0, top - 262, 176, 168);
            Wrap(Words(p, "Royal message", "Refill your hearts and keep playing", 0, top - 378, 540, 23, Ink.Body), 560, 40);
            // LifeShop writes the time to the next life here.
            Words(p, "Royal next life", "", 0, top - 418, 540, 21, Ink.Accent);
            var buttons = new List<RoyalCardControl> { Feedback(close, false) };
            var life = Button(p.Find("BuyLife"), null, 0, top - 512, 420, 96, Kind.Primary);
            if (life)
            {
                Hide(life.Find("Text"));
                Words(life, "Royal refill", "REFILL", -50, 1.2f, 230, 36, Ink.Caption);
                Price(life, 150, 32, Kind.Primary);
                buttons.Add(Feedback(life, true));
                ButtonShadow(skin, 0, top - 512, 420, 96);
            }
            var video = p.Find("Video");
            if (VideoOffers) buttons.Add(Feedback(Button(video, "WATCH FOR A LIFE", 0, top - 616, 400, 70, Kind.Secondary), false));
            else Hide(video);
            Ornament(skin, H);
            Motion(menu, p, H, buttons, life);
        }

        // ---- Information -----------------------------------------------------------------------------------------------

        public static void Guide(Transform menu, Transform p)   // BoostInfo
        {
            const float H = 722, top = H * .5f;
            var skin = Card(menu, p, H, "POWER UP YOUR PLAY");
            var close = Close(p.Find("Close"), H);
            string[] names = { "COLOR BALL", "ROCKET", "TNT" }, copy = { "Clears pieces of one colour", "Clears a full row or column", "Blasts the surrounding area" };
            string[] art = { "orb", "rocket", "tnt" };
            int ix = 0;
            foreach (var desc in p.GetComponentsInChildren<Text>(true))
            {
                if (desc.name != "BoostDescription") continue;
                int n = Mathf.Min(ix, 2);
                float y = top - 262 - n * 168;
                // The booster on its tile (as on the Level Start card), its name and what it does.
                TileBehind(p, "Royal info tile " + n, -190, y, 140);
                RoyalMenuSkin.Art(p, "Royal info art " + n, art[n], -190, y, n == 2 ? 112 : 104, n == 2 ? 112 : 104);
                Words(p, "Royal info name " + n, names[n], 78, y + 26, 330, 24, Ink.Heading).alignment = TextAnchor.MiddleLeft;
                desc.text = copy[n];
                Wrap(Field(desc.transform, 78, y - 18, 330, 21, Ink.Body), 330, 56).alignment = TextAnchor.MiddleLeft;
                ix++;
            }
            Ornament(skin, H);
            Motion(menu, p, H, new List<RoyalCardControl> { Feedback(close, false) }, null);
        }

        // How to play (redesigned 2026-09-30), on the level-end cards' ribbon system: "How to Play" on the ribbon with the red
        // close, one cream panel holding the lesson (RoyalLessonDemo: its title, a small board playing it on a loop, its rule),
        // a row of page dots, then BACK and NEXT side by side. BACK rests dimmed on the first lesson; on the last, NEXT reads
        // GOT IT and closes the card (Tutorial.Next).
        public const float LessonCardHeight = 940, LessonPanelTop = 96, LessonPanelHeight = 600, LessonDotsRow = 730, LessonButtonRow = 834;
        public const float LessonButtonWidth = 270, LessonButtonHeight = 100, LessonButtonX = 150;
        public static float LessonPageY => LessonCardHeight * .5f - LessonPanelTop - LessonPanelHeight * .5f;
        public const float LessonY = -4;   // where the older lesson pages sat
        public static void Lessons(Transform menu, Transform p)   // Tutorial
        {
            const float H = LessonCardHeight, top = H * .5f;
            var skin = RibbonCard(menu, p, H, "How to Play");
            var close = RedClose(p.Find("Close"), H);
            var panel = Panel(p, "Royal lesson panel", 0, LessonPageY, RoyalLessonDemo.PageWidth + 20, LessonPanelHeight, 26);
            panel.SetSiblingIndex(p.Find("Royal skin").GetSiblingIndex() + 1);
            var dots = Container(p, "Royal lesson dots");
            RoyalMenuSkin.Place(dots, 0, top - LessonDotsRow, 40 * RoyalLessonDemo.Count, 30);
            for (int i = 0; i < RoyalLessonDemo.Count; i++)
            {
                var dot = Container(dots, "Dot " + i);
                RoyalMenuSkin.Place(dot, (i - (RoyalLessonDemo.Count - 1) * .5f) * 30, 0, 16, 16);
                Inset(dot, "Edge", 0, 8, C("071a4c", .85f), C("071a4c", .85f));
                Inset(dot, "Face", 2, 6, C("5c7fd0"), C("3a5cb0"));
            }
            float y = top - LessonButtonRow, w = LessonButtonWidth, h = LessonButtonHeight;
            var back = CleanButton(p.Find("Back"), "BACK", -LessonButtonX, y, w, h, Kind.Secondary, 36);
            var next = CleanButton(p.Find("Next"), "NEXT", LessonButtonX, y, w, h, Kind.Primary, 40);
            if (next) ButtonShadow(skin, LessonButtonX, y, w, h);
            Motion(menu, p, H, new List<RoyalCardControl> { Feedback(close, false), Feedback(next, true), Feedback(back, false) }, next);
            int page = 0;
            for (int i = 0; i < RoyalLessonDemo.Count; i++) { var shown = p.Find("Royal lesson " + i); if (shown && shown.gameObject.activeSelf) page = i; }
            LessonProgress(p, page);
        }

        // The page dots (the current one gold and larger), NEXT or GOT IT, and BACK off on the first lesson.
        public static void LessonProgress(Transform card, int page)
        {
            if (!card) return;
            var dots = card.Find("Royal lesson dots");
            if (dots)
                for (int i = 0; i < dots.childCount; i++)
                {
                    var dot = dots.GetChild(i);
                    bool on = i == page;
                    dot.localScale = Vector3.one * (on ? 1.3f : 1);
                    var face = dot.Find("Face") ? dot.Find("Face").GetComponent<RoyalCardShape>() : null;
                    var edge = dot.Find("Edge") ? dot.Find("Edge").GetComponent<RoyalCardShape>() : null;
                    if (face) { face.from = on ? C("ffe27a") : C("5c7fd0"); face.to = on ? C("e6a22a") : C("3a5cb0"); face.SetVerticesDirty(); }
                    if (edge) { edge.from = edge.to = on ? C("7a3a12") : C("071a4c", .85f); edge.SetVerticesDirty(); }
                }
            var caption = card.Find("Next/Royal caption");
            var text = caption ? caption.GetComponent<Text>() : null;
            if (text) text.text = page >= RoyalLessonDemo.Count - 1 ? "GOT IT" : "NEXT";
            var back = card.Find("Back");
            var button = back ? back.GetComponent<UnityEngine.UI.Button>() : null;
            if (button) button.interactable = page > 0;
        }

        // The pages before the 2026-09-30 redesign (static art); RoyalLessonDemo's pages carry "Royal styled" from the start.
        public static void Lesson(Transform page)
        {
            if (!page || page.Find("Royal styled")) return;
            new GameObject("Royal styled", typeof(RectTransform)).transform.SetParent(page, false);
            RoyalMenuSkin.Place(page, 0, LessonY, 530, 430);
            var title = page.Find("Title");
            if (title) Field(title, 0, 184, 500, 26, Ink.HeadingOnCream);
            var rule = page.Find("Rule");
            if (rule) Wrap(Field(rule, 0, -165, 520, 21, Ink.BodyOnCream), 520, 70);
        }

        // ---- Pause and settings ----------------------------------------------------------------------------------------

        public static void Pause(Transform menu, Transform p, bool paused)   // MenuPause, Settings
        {
            // Rows from the card's top edge: CONTINUE (the pause menu only), MUSIC and SFX, the matching hints, then HOME
            // and HELP side by side (pause) or stacked (settings, where either may be absent).
            var resume = p.Find("Continue");
            var home = p.Find("Map");
            var info = p.Find("Info");
            bool showResume = resume && (paused || resume.gameObject.activeSelf);
            bool showHome = home && (paused || home.gameObject.activeSelf), showInfo = paused || info && info.gameObject.activeSelf;
            float row = 238, resumeRow = row;
            if (showResume) row += 118;
            float soundRow = row, hintRow = row + 96, helpRow, homeRow, last;
            if (paused) { helpRow = homeRow = hintRow + 128; last = homeRow; }
            else
            {
                helpRow = hintRow + 96;
                homeRow = showInfo ? helpRow + 96 : helpRow;
                last = showHome ? homeRow : showInfo ? helpRow : hintRow;
            }
            float H = Mathf.Round(last + 38 + BottomGap), top = H * .5f;

            var skin = Card(menu, p, H, paused ? "TAKE A BREATHER" : "SETTINGS");
            var close = Close(p.Find("Close"), H);
            var buttons = new List<RoyalCardControl> { Feedback(close, false) };
            Transform primary = null;
            if (showResume)
            {
                primary = Button(resume, "CONTINUE", 0, top - resumeRow, 460, 100, Kind.Primary);
                buttons.Add(Feedback(primary, true));
                ButtonShadow(skin, 0, top - resumeRow, 460, 100);
            }
            else Hide(resume);
            // The captions are rewritten with the current state by RoyalMenuPresenter.
            buttons.Add(Feedback(Button(p.Find("Music"), "MUSIC ON", -128, top - soundRow, 244, 76, Kind.Secondary), false));
            buttons.Add(Feedback(Button(p.Find("Sound"), "SFX ON", 128, top - soundRow, 244, 76, Kind.Secondary), false));
            foreach (string n in new[] { "Music", "Sound" }) Hide(p.Find(n + "/" + n + "Off"));
            var hint = p.Find("Hints");
            if (!hint)
            {
                hint = new GameObject("Hints", typeof(RectTransform), typeof(Image), typeof(UnityEngine.UI.Button)).transform;
                hint.SetParent(p, false);
                hint.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => { RoyalHintSettings.Enabled = !RoyalHintSettings.Enabled; RoyalAudioDirector.Get?.Play(RoyalCue.Click); });
            }
            buttons.Add(Feedback(Button(hint, "MATCHING HINTS ON", 0, top - hintRow, 500, 76, Kind.Secondary), false));
            if (paused)
            {
                Wrap(Words(p, "Royal hint description", "A gentle clue after a few quiet seconds", 0, top - hintRow - 56, 520, 19, Ink.Body), 540, 30);
                // HELP opens the existing How-to-play lessons. The pause menu has no Info object, so the button is made here,
                // beside HOME. The lessons' motion runs on the unscaled clock, so it plays while the game is paused.
                if (!info)
                {
                    info = new GameObject("Info", typeof(RectTransform), typeof(Image), typeof(UnityEngine.UI.Button)).transform;
                    info.SetParent(p, false);
                    info.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => { RoyalAudioDirector.Get?.Play(RoyalCue.Click); MenuManager.Instance.ShowTutorial(true); });
                }
                buttons.Add(Feedback(Button(home, "HOME", -128, top - homeRow, 244, 76, Kind.Secondary), false));
                buttons.Add(Feedback(Button(info, "HELP", 128, top - helpRow, 244, 76, Kind.Secondary), false));
                LeaveConfirm(menu, p);
            }
            else
            {
                if (showInfo) buttons.Add(Feedback(Button(info, "HELP", 0, top - helpRow, 300, 76, Kind.Secondary), false));
                if (showHome) buttons.Add(Feedback(Button(home, "HOME", 0, top - homeRow, 300, 76, Kind.Secondary), false));
            }
            Ornament(skin, H);
            Motion(menu, p, H, buttons, primary);
        }

        // Pause -> HOME leaves the level and the life spent on it, so the pause menu asks first. The question covers the
        // screen (its dimmer blocks the pause card underneath); LEAVE runs the existing BackToMap, STAY returns.
        static void LeaveConfirm(Transform menu, Transform p)
        {
            if (p.Find(RoyalMenuSkin.LeaveConfirmName)) return;
            var dim = new GameObject(RoyalMenuSkin.LeaveConfirmName, typeof(RectTransform), typeof(Image));
            dim.transform.SetParent(p, false);
            var dr = (RectTransform)dim.transform;
            dr.anchorMin = dr.anchorMax = new Vector2(.5f, .5f); dr.sizeDelta = new Vector2(3000, 4000);
            var shade = dim.GetComponent<Image>();
            shade.color = DimColor; shade.raycastTarget = true;
            // After the reference's Quit Level card: the title on the ribbon, the red close, a cream panel with the broken heart
            // and the warning, then Quit (red, the existing BackToMap) over Cancel (blue); the close cancels too.
            const float H = 724, top = H * .5f;
            var card = new GameObject("Card", typeof(RectTransform), typeof(Image)).transform;
            card.SetParent(dim.transform, false);
            var skin = RibbonCard(dim.transform, card, H, "Quit Level?", 0);
            var manager = menu.GetComponent<AnimationManager>();
            UnityEngine.Events.UnityAction cancel = () => { dim.SetActive(false); RoyalAudioDirector.Get?.Play(RoyalCue.Click); };
            var close = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(UnityEngine.UI.Button)).transform;
            close.SetParent(card, false);
            RedClose(close, H);
            close.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(cancel);
            var panel = Panel(card, "Royal confirm panel", 0, top - 246, 566, 318, 26);
            panel.SetSiblingIndex(skin.GetSiblingIndex() + 1);
            BrokenHeart(card, "Royal broken heart", 0, top - 210, 190, true);
            Words(card, "Royal confirm text", "You will lose a life!", 0, top - 353, 520, 40, Ink.Navy);
            var leave = new GameObject("Leave", typeof(RectTransform), typeof(Image), typeof(UnityEngine.UI.Button)).transform;
            leave.SetParent(card, false);
            CleanButton(leave, "Quit", 0, top - 494, 482, PrimaryHeight, Kind.Danger, 50);
            ButtonShadow(skin, 0, top - 494, 482, PrimaryHeight);
            leave.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => { if (manager) manager.BackToMap(); });
            var stay = new GameObject("Stay", typeof(RectTransform), typeof(Image), typeof(UnityEngine.UI.Button)).transform;
            stay.SetParent(card, false);
            CleanButton(stay, "Cancel", 0, top - 619, 416, 86, Kind.Secondary, 36);
            stay.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(cancel);
            Ornament(skin, H);
            var motion = dim.AddComponent<RoyalCardMotion>();
            motion.front = true;
            motion.dim = shade; motion.dimColor = DimColor;
            motion.card = (RectTransform)card; motion.cardGroup = card.gameObject.AddComponent<CanvasGroup>();
            motion.reveals = RoyalCardMotion.Bands(card, H);
            motion.controls = new[] { Feedback(close, false), Feedback(leave, true), Feedback(stay, false) };
            motion.gloss = leave.Find("Royal skin/Sweep").GetComponent<RoyalSweepGraphic>();
            dim.SetActive(false);
        }

        // ---- Results ---------------------------------------------------------------------------------------------------

        // MenuComplete and MenuFailed. Returns the king's image, where RoyalMenuSkin mounts King Vanguard. Rows follow each
        // other from the top edge: the stars (win), the king, the goals (the fail card has them; the win card has no goals
        // group), the score, then the action; the card ends 52.5 below it.
        public static Image Result(Transform menu, Transform p, bool win)
        {
            if (!win) return Failed(menu, p);
            var targets = p.Find("Targets");
            var best = p.Find("BestScore");
            bool showBest = best && best.gameObject.activeSelf;
            // SHARE exists only with online services (GUIEvents hides it otherwise).
            var share = p.Find("Share");
            bool showShare = share && RoyalThrone.RoyalOfflineMode.ServicesEnabled;
            // The level-end system (as the So Close offer before it): the title at the offer's height, the stars 60 lower
            // than on the old card with it, the goals card at the offer's size and the primary button at its height.
            const float lower = CleanTitleDrop - TitleDrop, goalsHalf = RoyalCards.OfferGoalsHeight * .5f, actionHalf = PrimaryHeight * .5f;
            float row = (win ? 281 : 145) + lower;            // the stars' lower edge, or the title's
            float kingRow = row + 16 + 75; row = kingRow + 75;
            float goalsRow = 0;
            if (targets) { goalsRow = row + 21 + goalsHalf; row = goalsRow + goalsHalf; }
            float scoreRow = row + 38, valueRow = scoreRow + 43, bestRow = scoreRow + 85;
            row = showBest ? bestRow + 8 : valueRow + 18;
            float actionRow = row + 28 + actionHalf;
            float H = Mathf.Round(actionRow + actionHalf + CleanBottom), top = H * .5f;

            var skin = Card(menu, p, H, win ? "WELL PLAYED!" : "TRY AGAIN", CardOffsetY, true);
            // LEVEL over the level number on the ribbon ("Level" holds the number alone).
            var level = p.Find("Level");
            LevelBadge(p, level ? level.GetComponent<Text>() : null);
            // The stars earned, shown one by one by AnimationManager once the card is in.
            for (int i = 1; i <= 3; i++)
            {
                var star = p.Find("Star" + i);
                if (!win) { Hide(star); continue; }
                // A win earns one star (LevelManager.stars is 1), and it is Star1: that one sits in the middle, at the large size;
                // Star2 and Star3 keep the places beside it for a level that ever shows more.
                float size = i == 1 ? 118 : 100, x = i == 1 ? 0 : i == 2 ? -118 : 118;
                RoyalMenuSkin.Place(star, x, top - lower - (i == 1 ? 222 : 238), size, size);
                var image = star ? star.GetComponent<Image>() : null;
                if (image) { image.sprite = JellyGarden.Scripts.Juice.RoyalPieceSkin.Art("star"); image.preserveAspect = true; }
                if (star && !star.GetComponent<RoyalCalmAppear>()) star.gameObject.AddComponent<RoyalCalmAppear>();
                // The inherited star_animation (3x -> 1, linear, 0.5 s; no events) would override the calm appear every frame.
                var clip = star ? star.GetComponent<Animation>() : null;
                if (clip) { clip.Stop(); clip.playAutomatically = false; clip.enabled = false; }
            }
            var king = RoyalMenuSkin.Art(p, "Royal king", null, 0, top - kingRow, 170, 150);
            king.enabled = false; // empty mount point; V2 owns the visible character
            if (targets)
            {
                var panel = Panel(p, "Royal goals panel", 0, top - goalsRow, RoyalCards.OfferGoalsWidth, RoyalCards.OfferGoalsHeight);
                panel.SetSiblingIndex(p.Find("Royal skin").GetSiblingIndex() + 1);
                Separators(panel, 110, .5f);
                RoyalMenuSkin.Place(targets, 0, top - goalsRow, RoyalCards.OfferGoalsWidth, RoyalCards.OfferGoalsHeight);
            }
            foreach (string n in new[] { "TargetDescription1", "TargetDescription2", "TargetCheck1", "TargetCheck2", "TargetUnCheck1", "TargetUnCheck2" }) Hide(p.Find(n));
            Words(p, "Royal score label", "SCORE", 0, top - scoreRow, 350, 20, Ink.Heading);
            Field(p.Find("Score"), 0, top - valueRow, 490, 44, Ink.Hero);
            Field(best, 0, top - bestRow, 490, 20, Ink.Accent);
            var buttons = new List<RoyalCardControl>();
            float actionY = top - actionRow;
            // With SHARE the two sit side by side: a shorter action on the left, SHARE at the secondary height on the right.
            var actionButton = p.Find(win ? "Next" : "Again");
            bool pair = showShare && actionButton;
            float actionX = pair ? -78 : 0, actionW = pair ? 330 : 490;
            var action = CleanButton(actionButton, win ? "CONTINUE" : "TRY AGAIN", actionX, actionY, actionW, PrimaryHeight, Kind.Primary);
            if (action) { buttons.Add(Feedback(action, true)); ButtonShadow(skin, actionX, actionY, actionW, PrimaryHeight); }
            if (pair) buttons.Add(Feedback(CleanButton(share, "SHARE", 172, actionY, 150, SecondaryHeight, Kind.Secondary, 24), false));
            else Hide(share);
            Motion(menu, p, H, buttons, action);
            return king;
        }

        // MenuFailed, after the reference's Level Failed card: "Level n" on the ribbon, the red close, one cream panel with
        // the broken heart and "Level Failed!", a line of explanation and the goals (each open goal's X on its icon and what
        // was left under it), then Try Again (RetryLevel) and End Level. The close and End Level both leave for the map
        // through the card's own CloseMenu, as the back key does. RoyalMenuPresenter writes the level into the title.
        public const float FailGoalsWidth = 520, FailGoalsHeight = 150, FailIcon = 72, FailValue = 30, FailPad = 18;
        static Image Failed(Transform menu, Transform p)
        {
            const float panelTop = 84, panelBottom = 535, heartRow = 132, lineRow = 334, goalsRow = 436, againRow = 625, endRow = 748;
            const float H = 840, top = H * .5f;
            var skin = RibbonCard(menu, p, H, "Level");
            var manager = menu.GetComponent<AnimationManager>();
            var close = p.Find("Royal close");
            if (!close) { close = new GameObject("Royal close", typeof(RectTransform), typeof(Image), typeof(UnityEngine.UI.Button)).transform; close.SetParent(p, false); close.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => { if (manager) manager.CloseMenu(); }); }
            RedClose(close, H);
            var panel = Panel(p, "Royal fail panel", 0, top - (panelTop + panelBottom) * .5f, 560, panelBottom - panelTop, 26);
            panel.SetSiblingIndex(p.Find("Royal skin").GetSiblingIndex() + 1);
            // The heart and "Level Failed!" centred together as one line.
            var heading = Words(p, "Royal fail heading", "Level Failed!", 0, top - heartRow, 420, 48, Ink.Navy);
            float heart = 66, gap = 14, line = heart + gap + heading.preferredWidth, left = -line * .5f;
            BrokenHeart(p, "Royal broken heart", left + heart * .5f, top - heartRow + 2, heart, false);
            RoyalMenuSkin.Place(heading.transform, left + heart + gap + heading.preferredWidth * .5f, top - heartRow + 48 * CapDrop, heading.preferredWidth + 4, 48 * 1.4f);
            Words(p, "Royal fail line", "You didn't complete all the goals.", 0, top - lineRow, 520, 23, Ink.NavySoft);
            var targets = p.Find("Targets");
            var goals = Container(p, "Royal goals panel");
            RoyalMenuSkin.Place(goals, 0, top - goalsRow, FailGoalsWidth, FailGoalsHeight);
            goals.SetSiblingIndex(panel.GetSiblingIndex() + 1);
            Inset(goals, "Edge", 0, 24, C("dcc491"), C("d2b884"));
            Inset(goals, "Fill", 1.6f, 22.4f, C("f4e5c6"), C("efdcb7"));
            Separators(goals, 96, .5f);
            if (targets) RoyalMenuSkin.Place(targets, 0, top - goalsRow, FailGoalsWidth, FailGoalsHeight);
            foreach (string n in new[] { "TargetDescription1", "TargetDescription2", "TargetCheck1", "TargetCheck2", "TargetUnCheck1", "TargetUnCheck2", "Score", "BestScore" }) Hide(p.Find(n));
            for (int i = 1; i <= 3; i++) Hide(p.Find("Star" + i));
            Hide(p.Find("Level"));   // the number alone; the ribbon's title carries it
            // A dedicated portrait row keeps the new king's sad action visible without covering goals or buttons.
            var king = RoyalMenuSkin.Art(p, "Royal king", null, 0, top - 243, 170, 150);
            king.enabled = false;
            var buttons = new List<RoyalCardControl> { Feedback(close, false) };
            var again = CleanButton(p.Find("Again"), "Try Again", 0, top - againRow, 470, PrimaryHeight, Kind.Primary, 44);
            if (again) { buttons.Add(Feedback(again, true)); ButtonShadow(skin, 0, top - againRow, 470, PrimaryHeight); }
            var end = p.Find("Royal end level");
            if (!end) { end = new GameObject("Royal end level", typeof(RectTransform), typeof(Image), typeof(UnityEngine.UI.Button)).transform; end.SetParent(p, false); end.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => { if (manager) manager.CloseMenu(); }); }
            buttons.Add(Feedback(CleanButton(end, "End Level", 0, top - endRow, 420, SecondaryHeight, Kind.Secondary, 32), false));
            Motion(menu, p, H, buttons, again);
            return king;
        }

        public static void Reward(Transform menu, Transform p)
        {
            const float H = 600, top = H * .5f;
            var skin = Card(menu, p, H, "A ROYAL REWARD");
            var close = Close(p.Find("Close"), H);
            Hide(p.Find("Label")); Hide(p.Find("icon"));
            RoyalMenuSkin.Art(p, "Royal reward", "chest", 0, top - 300, 250, 240);
            float okY = -top + BottomGap + 48;
            var ok = Button(p.Find("Ok"), "COLLECT", 0, okY, 400, 96, Kind.Primary);
            if (ok) ButtonShadow(skin, 0, okY, 400, 96);
            Ornament(skin, H);
            Motion(menu, p, H, new List<RoyalCardControl> { Feedback(close, false), Feedback(ok, true) }, ok);
        }

        // PreFailed: the offer of extra moves. Its banner is the card; CONTINUE, END LEVEL and the video offer are siblings
        // of the banner under the menu root. Its own clip (RoyalMenuPresenter.ConfigureRetryMotion) times the card.
        // Built on the level-end system (CleanSurface, Panel, CleanButton): crest with LEVEL n, the title and its subtitle,
        // one cream goals card in equal columns, CONTINUE with its price, then the smaller END LEVEL; no ornament.
        public const float OfferGoalsWidth = 560, OfferGoalsHeight = 188, OfferIcon = 76, OfferValue = 36, OfferPad = 30;
        public static void SoClose(Transform retry)
        {
            const float goalsRow = 390, buyRow = 570, buyW = 520, giveUpRow = 696, giveUpW = 396;
            float H = VideoOffers ? 880 : 790, top = H * .5f, Y = CardOffsetY;
            var banner = retry.Find("Banner");
            var skin = Card(retry, banner, H, "SO CLOSE!", CardOffsetY, true);
            // LEVEL over the level number on the ribbon, as on the result cards (RoyalMenuPresenter writes the number).
            LevelBadge(banner, Words(banner, "Royal level number", "", 0, 0, 160, 40, Ink.Display));
            // RoyalMenuPresenter writes the level's extra moves (or seconds) here each time the offer opens.
            Wrap(Words(banner, "Royal retry text", "A few more moves could do it", 0, top - 250, 580, 26, Ink.Subtitle), 600, 40);
            var panel = Panel(banner, "Royal goals panel", 0, top - goalsRow, OfferGoalsWidth, OfferGoalsHeight);
            panel.SetSiblingIndex(banner.Find("Royal skin").GetSiblingIndex() + 1);
            Separators(panel, 110, .5f);
            RoyalMenuSkin.Place(banner.Find("Targets"), 0, top - goalsRow, OfferGoalsWidth, OfferGoalsHeight);
            foreach (var group in retry.GetComponentsInChildren<JellyGarden.Scripts.Targets.TargetsGroup>(true))
            {
                group.transform.SetParent(banner, false); group.name = "Royal live goals";
                RoyalMenuSkin.Place(group.transform, 0, top - goalsRow, OfferGoalsWidth, OfferGoalsHeight);
            }
            foreach (string n in new[] { "TargetCheck1", "TargetCheck2", "TargetUnCheck1", "TargetUnCheck2" }) Hide(banner.Find("Targets/" + n));
            var buttons = new List<RoyalCardControl>();
            var buy = CleanButton(retry.Find("Buy"), null, 0, Y + top - buyRow, buyW, PrimaryHeight, Kind.Primary);
            if (buy)
            {
                // CONTINUE on the left, a divider, then the price (LevelManager.FailedCost, written by PreFailed) against its coin.
                Words(buy, "Royal more", "CONTINUE", -76, 1.2f, 300, 38, Ink.OnGreen);
                Divider(buy, 72, 54, Kind.Primary);
                var price = Field(buy.Find("Price"), 90, 1.2f, 120, 36, Ink.OnGreen);
                if (price) { price.alignment = TextAnchor.MiddleRight; price.enabled = true; }
                RoyalMenuSkin.Art(buy, "Royal coin", "coin", 188, 0, 54, 54).transform.SetAsLastSibling();
                buttons.Add(Feedback(buy, true));
                ButtonShadow(skin, 0, top - buyRow, buyW, PrimaryHeight);
            }
            var giveUp = CleanButton(retry.Find("GiveUp"), "END LEVEL", 0, Y + top - giveUpRow, giveUpW, SecondaryHeight, Kind.Secondary, 27);
            if (giveUp) buttons.Add(Feedback(giveUp, false));
            var video = retry.Find("Video");
            if (VideoOffers)
            {
                video = CleanButton(video, "WATCH FOR MOVES", 0, Y + top - giveUpRow - 90, giveUpW, 70, Kind.Secondary, 22);
                if (video) buttons.Add(Feedback(video, false));
            }
            else Hide(video);
            var motion = retry.gameObject.AddComponent<RoyalCardMotion>();
            motion.entrance = false;
            motion.controls = buttons.ToArray();
            motion.gloss = buy ? buy.Find("Royal skin/Sweep").GetComponent<RoyalSweepGraphic>() : null;
        }

        // While the offer is open the level is not lost yet, so each goal shows what is left (a tick once it is done) in place
        // of the red X that TargetIcon gives every open goal in PreFailed; the final card (MenuFailed) keeps the X. Called
        // every frame from RoyalMenuPresenter.LateUpdate, after the icons' own Update (they are created after the offer
        // enables): each icon is held once and then left alone until ReleaseOfferGoals.
        public static void OfferGoals(Transform retry, List<JellyGarden.Scripts.Targets.TargetIcon> icons)
        {
            var goals = retry.Find("Banner/Royal live goals");
            if (!goals) return;
            goals.GetComponentsInChildren(false, icons);
            foreach (var icon in icons)
            {
                if (!icon.enabled || !icon.checkObject || !icon.textObj) continue;
                icon.enabled = false;
                bool done = icon.target != null && icon.target.Done();
                if (done && icon.checkUncheck != null && icon.checkUncheck.Length > 0) icon.checkObject.sprite = icon.checkUncheck[0];
                icon.checkObject.gameObject.SetActive(done);
                icon.textObj.gameObject.SetActive(!done);
            }
        }

        // The fail card: each open goal keeps its X (now on the icon's corner) and shows what was left under it; a done goal
        // shows its tick. Held like the offer's goals, until ReleaseFailedGoals.
        public static void FailedGoals(Transform failed, List<JellyGarden.Scripts.Targets.TargetIcon> icons)
        {
            var goals = failed.Find("Image/Targets");
            if (!goals) return;
            goals.GetComponentsInChildren(false, icons);
            foreach (var icon in icons)
            {
                if (!icon.enabled || !icon.checkObject || !icon.textObj) continue;
                icon.enabled = false;
                bool done = icon.target != null && icon.target.Done();
                if (icon.checkUncheck != null && icon.checkUncheck.Length > 1) icon.checkObject.sprite = icon.checkUncheck[done ? 0 : 1];
                icon.checkObject.gameObject.SetActive(true);
                icon.textObj.gameObject.SetActive(!done);
            }
        }

        public static void ReleaseFailedGoals(Transform failed)
        {
            var goals = failed.Find("Image/Targets");
            if (!goals) return;
            foreach (var icon in goals.GetComponentsInChildren<JellyGarden.Scripts.Targets.TargetIcon>(true)) icon.enabled = true;
        }

        // The offer closed (CONTINUE or END LEVEL): its goal icons follow the game again.
        public static void ReleaseOfferGoals(Transform retry)
        {
            var goals = retry.Find("Banner/Royal live goals");
            if (!goals) return;
            foreach (var icon in goals.GetComponentsInChildren<JellyGarden.Scripts.Targets.TargetIcon>(true)) icon.enabled = true;
        }

        // PrePlay: the goals reminder between the map and the board. Its own clip (RoyalMenuPresenter.ConfigurePrePlay) times it.
        public static void Journey(Transform root, Transform banner)
        {
            const float H = 430, top = H * .5f;
            RoyalMenuSkin.Place(banner, 0, 0, CardWidth, H);
            var image = banner.GetComponent<Image>();
            if (image) image.color = Color.clear;
            var skin = Container(banner, "Royal skin");
            skin.SetAsFirstSibling();
            Surface(skin, CardWidth, H);
            Title(banner, "YOUR GOALS", H);
            var panel = Cream(banner, "Royal goals panel", 0, top - 252, 480, 124, 32);
            panel.SetSiblingIndex(skin.GetSiblingIndex() + 1);
            Separators(panel, 60);
            foreach (var group in root.GetComponentsInChildren<JellyGarden.Scripts.Targets.TargetsGroup>(true))
            {
                group.transform.SetParent(banner, false); group.name = "Royal live goals";
                RoyalMenuSkin.Place(group.transform, 0, top - 252, 480, 124);
            }
            Wrap(Words(banner, "Royal journey caption", "Make every move count", 0, top - 360, 540, 22, Ink.Body), 560, 36);
            Ornament(skin, H);
        }

        // ---- Shared ----------------------------------------------------------------------------------------------------

        // A tile drawn behind a piece of content that is itself an Image (its children would draw over it).
        static void TileBehind(Transform card, string name, float x, float y, float size)
        {
            var holder = Container(card, name);
            RoyalMenuSkin.Place(holder, x, y, size, size);
            Tile(holder, size, size);
            holder.SetSiblingIndex(card.Find("Royal skin").GetSiblingIndex() + 1);
        }

        // The card's motion: the calm entrance in place of the inherited clip (whose events it fires), reveal bands, press
        // feedback for every control and the gloss on the primary action.
        static void Motion(Transform menu, Transform card, float height, List<RoyalCardControl> controls, Transform primary)
        {
            var motion = menu.gameObject.AddComponent<RoyalCardMotion>();
            motion.dim = menu.GetComponent<Image>(); motion.dimColor = DimColor;
            motion.card = (RectTransform)card;
            motion.cardGroup = card.GetComponent<CanvasGroup>();
            if (!motion.cardGroup) motion.cardGroup = card.gameObject.AddComponent<CanvasGroup>();
            motion.AdoptClip(menu.GetComponent<Animation>());
            motion.reveals = RoyalCardMotion.Bands(card, height);
            controls.RemoveAll(c => c == null);
            motion.controls = controls.ToArray();
            var sweep = primary ? primary.Find("Royal skin/Sweep") : null;
            motion.gloss = sweep ? sweep.GetComponent<RoyalSweepGraphic>() : null;
        }
    }

    // The win card's star landing, as the reference's results star does it (measured from the recordings, 2026-09-30): it drops
    // in from 2.4x to full size in 0.13 s as it fades in, gives a gentle squash to 0.86 and a rebound to 1.05, and is settled by
    // 0.5 s, on the unscaled clock. A reward, not a control, so it keeps this bounce; the old star_animation clip is off.
    public sealed class RoyalCalmAppear : MonoBehaviour
    {
        const float Land = .13f, Squash = .25f, Rebound = .40f, End = .50f;
        float t;
        CanvasGroup group;
        void OnEnable()
        {
            t = 0;
            if (!group) group = GetComponent<CanvasGroup>();
            if (!group) group = gameObject.AddComponent<CanvasGroup>();
            Pose();
        }
        void Update() { if (t > End) return; t += Mathf.Min(Time.unscaledDeltaTime, .05f); Pose(); }
        void Pose()
        {
            float s;
            if (t < Land) { float u = t / Land; s = Mathf.Lerp(2.4f, 1, u * u); }
            else if (t < Squash) s = Mathf.Lerp(1, .86f, RoyalLevelCardMotion.EaseOutCubic((t - Land) / (Squash - Land)));
            else if (t < Rebound) s = Mathf.Lerp(.86f, 1.05f, RoyalLevelCardMotion.EaseOutCubic((t - Squash) / (Rebound - Squash)));
            else s = Mathf.Lerp(1.05f, 1, RoyalLevelCardMotion.EaseOutCubic((t - Rebound) / (End - Rebound)));
            group.alpha = Mathf.Clamp01(t / .08f);
            transform.localScale = Vector3.one * s;
        }
    }
}
