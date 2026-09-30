using System;
using System.Collections;
using System.Collections.Generic;
using JellyGarden.Scripts;
using JellyGarden.Scripts.GUI;
using JellyGarden.Scripts.Juice;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using K = RoyalThrone.Presentation.RoyalAreaKit;

namespace RoyalThrone.Presentation
{
    /// <summary>
    /// The area dialogs, staged as the reference does (Design/RoyalMatchReference/Areas/GAPS.md), in Royal Throne's own drawn
    /// pieces (RoyalAreaKit) inside the Home dialog's layer, so Back, the veil and the Home controls behave as for every Home
    /// dialog:
    /// - the task list: the area's name on an arched ribbon, its Area Progress (tap the chest for its rewards) and one row, the
    ///   next task, with Do It and its cost;
    /// - building: the stars leave the counter for Do It, the list goes, the Home controls slide away, the object rises into
    ///   the scene, the controls come back, a streak carries the progress to the Area card, the list returns on the next task;
    /// - short of stars, the chest ceremony (Tap to Open, Tap to Continue, Area Completed!, the restored area), New Area!;
    /// - the Areas tab: every area as a card; a restored one is replayed full screen.
    /// The saved state changes only in RoyalAreas (Build, CollectChest, Enter); everything here is presentation.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoyalAreaView : MonoBehaviour
    {
        public const string Tasks = "area", List = "areas", Chest = "area-complete", Unlocked = "area-unlocked", Short = "area-stars", Soon = "area-soon";
        public static bool Handles(string panel) => panel == Tasks || panel == List || panel == Chest || panel == Unlocked || panel == Short || panel == Soon;
        /// <summary>The area dialogs draw their own panels and hide the Home card, so none of them uses its tall layout.</summary>
        public static bool Tall(string panel) => false;

        // Reference timings (GAPS.md A-08 .. A-14): the phone (native) where it showed them, else the emulator's Area 1 builds.
        // From the tap: the star leaves at 0.13 s and lands at 0.8 s; the row stands without Do It 0.15 s; the list goes at 1.2 s;
        // the empty scene waits 0.6 s after the controls leave; the object rises in about 1 s and holds 1.5 s; the list is back
        // at about 6.2 s (phone) and on the next task about 1.4 s later.
        const float OpenSettle = .2f, StarStagger = .14f, StarFlight = .66f, Shrink = .2f, RowBare = .15f, HudSlide = .35f, PauseBeforeRise = .6f,
            RiseTime = 1.3f, HoldAfterRise = 1.5f, StreakTime = .8f, TickPop = .3f, RowOut = .3f, RowIn = .3f, LockRelease = .25f;

        RoyalHomeScreen home;
        RoyalAreaScene scene;
        RoyalThrone.ReferenceGameplay.ReferenceHomeLayout homeLayout;
        RectTransform root;
        Image outside, navDim;
        Button outsideButton;
        Color veilColor; bool veilKept;
        string showing;
        bool Motion => home && home.MotionEnabled;

        public RoyalAreaScene Scene => scene;
        /// <summary>The area dialog on screen (Tasks, Short, Chest, Unlocked, Soon or List), null when none is.</summary>
        public string Showing => home && home.dialog && home.dialog.activeSelf && root && root.gameObject.activeSelf ? showing : null;
        public bool TasksVisible => Showing == Tasks;
        public bool ListVisible => Showing == List;
        /// <summary>Task rows shown: the reference lists one, the next task in order.</summary>
        public int VisibleTiles => Showing == Tasks && row && row.gameObject.activeSelf ? 1 : 0;
        public int RowTask => rowTask;
        public Button DoItButton => doIt;
        public Button CloseButton => closeFor.TryGetValue(Showing ?? "", out var b) ? b : null;
        public Button OutsideButton => outsideButton;
        public Button ChestButton => chestButton;
        public bool TooltipVisible => tooltip && tooltip.gameObject.activeSelf;
        public Button ShortContinueButton => shortContinue;
        public Button UnlockButton => unlock;
        /// <summary>For the checks: how far the open area panel's lowest part (its drop shadow aside) is above the tab bar, in
        /// screen pixels; negative when something sits under the bar.</summary>
        public float TabClearance
        {
            get
            {
                var panel = showing == Tasks ? taskPanel : showing == Short ? shortPanel : showing == Unlocked || showing == Soon ? newPanel : null;
                var bar = home.navigationBackground ? home.navigationBackground : home.navigation;
                if (Showing == null || !panel || !bar) return float.NaN;
                var corners = new Vector3[4]; float lowest = float.MaxValue;
                foreach (var g in panel.GetComponentsInChildren<Graphic>())
                {
                    if (!g.enabled || g.color.a <= 0 || g.name == "Shadow") continue;
                    var mask = g.GetComponentInParent<RectMask2D>();   // clipped art counts as far as its mask
                    (mask && mask.transform.IsChildOf(panel) ? mask.rectTransform : g.rectTransform).GetWorldCorners(corners);
                    lowest = Mathf.Min(lowest, corners[0].y);
                }
                bar.GetWorldCorners(corners);
                return lowest - corners[1].y;
            }
        }
        public Button StageButton => stageTap;
        public int StageStep => stageStep;
        public bool Viewing => viewing != null;
        public bool Replaying => scene && scene.Replaying;
        public Button ViewBackButton => viewClose;
        /// <summary>The Areas tab's card for an area (null when that area is not listed).</summary>
        public Button AreaButton(int number) { foreach (var c in cards) if (c.number == number && c.rect.gameObject.activeSelf) return c.view; return null; }
        public string CardState(int number) { foreach (var c in cards) if (c.number == number && c.rect.gameObject.activeSelf) return c.state; return null; }
        public int ListedAreas { get { int n = 0; foreach (var c in cards) if (c.rect.gameObject.activeSelf) n++; return n; } }
        /// <summary>For the checks: the row's button for the given task, when that task is the one listed.</summary>
        public Button TaskButton(int task) => task == rowTask && doIt && doIt.gameObject.activeSelf ? doIt : null;
        public bool Building => building != null;
        public bool Ceremony => ceremony != null;

        readonly Dictionary<string, Button> closeFor = new Dictionary<string, Button>();

        public static RoyalAreaView Attach(RoyalHomeScreen home, RoyalAreaScene scene)
        {
            var view = home.GetComponent<RoyalAreaView>();
            if (!view) view = home.gameObject.AddComponent<RoyalAreaView>();
            view.home = home; view.scene = scene; view.Build();
            return view;
        }

        /// <summary>Sends the Area card to the dialog the area's state calls for.</summary>
        public string Route(string panel)
        {
            if (panel != Tasks) return panel;
            if (forceTasks) return Tasks;
            if (RoyalAreas.Complete) return Chest;
            var area = RoyalAreas.CurrentArea;
            if (area != null && area.Ready && RoyalAreas.AwaitingEntry) return Unlocked;
            return area == null || !area.Ready ? Soon : Tasks;
        }

        // ================================================================ building the dialogs

        void Build()
        {
            if (root) return;
            K.Init(home.dialogTitle.font);
            homeLayout = home.GetComponent<RoyalThrone.ReferenceGameplay.ReferenceHomeLayout>();
            root = K.Node("Area dialogs", home.dialog.transform);
            // The dim and tap-outside layer (A-05). The Home dialog's own veil is cleared while an area dialog shows.
            outside = K.Node("Outside", root).gameObject.AddComponent<Image>();
            outside.color = new Color(.02f, .04f, .08f, .70f);
            outsideButton = outside.gameObject.AddComponent<Button>(); outsideButton.transition = Selectable.Transition.None;
            outsideButton.onClick.AddListener(OutsideTap);
            navDim = K.Node("Area dim over the tabs", home.navigation).gameObject.AddComponent<Image>();
            navDim.color = new Color(.02f, .04f, .08f, .62f); navDim.raycastTarget = false; navDim.gameObject.SetActive(false);
            BuildTaskPanel(); BuildShortPanel(); BuildNewPanel(); BuildStage(); BuildBanner(); BuildShowcase(); BuildPage(); BuildViewer(); BuildGuide();
            root.gameObject.SetActive(false);
            Canvas.willRenderCanvases += HoldLabels;
        }
        void OnDestroy() { Canvas.willRenderCanvases -= HoldLabels; }

        // A design-size panel (720 wide) pinned under the Home counters and scaled to the layout width.
        RectTransform DesignPanel(string name, float height)
        {
            var p = K.Node(name, root); p.anchorMin = p.anchorMax = new Vector2(.5f, 1); p.pivot = new Vector2(.5f, 1);
            p.sizeDelta = new Vector2(720, height); p.gameObject.SetActive(false);
            return p;
        }

        RectTransform taskPanel; Text taskTitle; RectTransform barFill; Text barCount; Button chestButton;
        RectTransform row, rowSlide; Image rowArt; Text rowName; Button doIt; RectTransform doItRect; Text doItWord, doItCost; Image doItStar;
        RectTransform tick; Text completedLine; RectTransform tooltip; Text tooltipCoins; Image tooltipBoost, tooltipDismiss; Text tooltipPlus;
        int rowTask = -1;

        void BuildTaskPanel()
        {
            taskPanel = DesignPanel("Area task list", 942);
            K.Inset(FrameOf(K.BluePanel(taskPanel)), 27, 91, 27, 22);
            taskTitle = K.Ribbon(taskPanel, 720, 0, "", 64);
            closeFor[Tasks] = K.RedClose(taskPanel, 668, 58, 100); closeFor[Tasks].onClick.AddListener(home.ClosePanel);
            // Area Progress: heading, bar and the chest (tap: its rewards, A-07).
            var card = K.CreamCard("Area progress", taskPanel, 76, 221, 567, 156);
            var heading = K.Words("Heading", card, "Area Progress", 31, K.Cream, TextAnchor.MiddleCenter);
            K.Centre(heading.rectTransform, 239, 31, 360, 44); K.Outline(heading, K.Navy, 2.4f);
            K.ProgressBar(card, 35, 64, 409, 56, out barFill, out barCount, 30);
            var chest = K.Picture("Area chest", card, K.Chest, 471, 75, 163, 130);
            chest.raycastTarget = true; chestButton = chest.gameObject.AddComponent<Button>(); chestButton.transition = Selectable.Transition.None;
            chestButton.onClick.AddListener(() => ShowTooltip(!TooltipVisible));
            var divider = K.Surface("Divider", taskPanel, K.C(0, 60, 128), K.C(0, 60, 128), 2); K.At(divider.rectTransform, 71, 399, 578, 4);
            // The one task row: its object as the icon, the name, and Do It with the cost (A-03).
            row = K.At(K.Node("Task row", taskPanel), 76, 424, 567, 115);
            rowSlide = K.Node("Slide", row);
            var face = K.CreamCard("Row card", rowSlide, 0, 0, 567, 115);
            rowArt = K.Picture("Task object", face, null, 50, 52, 76, 76);
            rowName = K.Words("Task name", face, "", 31, K.Ink, TextAnchor.MiddleLeft); K.At(rowName.rectTransform, 96, 8, 280, 94);
            rowName.resizeTextMinSize = 18;
            doIt = K.GreenButton("Do It", rowSlide, 385, 12, 163, 90, out var doItFace); doItRect = (RectTransform)doIt.transform;
            doItWord = K.Words("Caption", doItFace, "Do It", 27, K.Cream, TextAnchor.MiddleRight); K.At(doItWord.rectTransform, 4, 2, 76, 70); K.ButtonCaption(doItWord);
            doItStar = K.Picture("Star", doItFace, K.Star, 97, 37, 31, 31);
            doItCost = K.Words("Cost", doItFace, "1", 30, K.Cream, TextAnchor.MiddleLeft); K.At(doItCost.rectTransform, 114, 2, 32, 70); K.ButtonCaption(doItCost);
            doIt.onClick.AddListener(DoIt);
            tick = K.Check(rowSlide, 466, 57, 92); tick.gameObject.SetActive(false);
            completedLine = K.Words("Area completed", taskPanel, "Area Completed!", 34, K.Cream, TextAnchor.MiddleCenter);
            K.At(completedLine.rectTransform, 76, 440, 567, 80); K.Outline(completedLine, K.Navy, 2.6f); completedLine.gameObject.SetActive(false);
            // The chest's rewards, in a bubble under the progress card (A-07); a tap anywhere else puts it away.
            tooltipDismiss = K.Node("Chest rewards dismiss", taskPanel).gameObject.AddComponent<Image>(); tooltipDismiss.color = Color.clear;
            var dismiss = tooltipDismiss.gameObject.AddComponent<Button>(); dismiss.transition = Selectable.Transition.None;
            dismiss.onClick.AddListener(() => ShowTooltip(false));
            var reach = tooltipDismiss.rectTransform; reach.offsetMin = new Vector2(-2000, -3000); reach.offsetMax = new Vector2(2000, 3000);
            tooltipDismiss.gameObject.SetActive(false);
            tooltip = K.At(K.Node("Chest rewards", taskPanel), 44, 368, 632, 150);
            var bubble = K.Surface("Bubble", tooltip, Color.white, K.C(236, 240, 248), 22);
            var tip = K.Surface("Pointer", tooltip, Color.white, Color.white, 4); K.Centre(tip.rectTransform, 503, 0, 34, 34);
            tip.rectTransform.localRotation = Quaternion.Euler(0, 0, 45); tip.transform.SetAsFirstSibling();
            var coinArt = RoyalPieceSkin.CampaignArt("coin");
            K.Picture("Coin", tooltip, coinArt ? coinArt : K.Star, 220, 64, 86, 86);
            tooltipCoins = K.Words("Coins", tooltip, "0", 30, K.Cream, TextAnchor.MiddleCenter); K.Centre(tooltipCoins.rectTransform, 220, 116, 120, 40); K.Outline(tooltipCoins, K.Navy, 2.4f);
            tooltipPlus = K.Words("Plus", tooltip, "+", 40, K.Ink, TextAnchor.MiddleCenter); K.Centre(tooltipPlus.rectTransform, 318, 72, 50, 60);
            tooltipBoost = K.Picture("Booster", tooltip, null, 412, 72, 96, 96);
            tooltip.gameObject.SetActive(false);
        }

        RectTransform shortPanel; Button shortContinue;
        void BuildShortPanel()
        {
            // "Not Enough Stars!" (A-16), measured on the reference emulator and scaled to 720 wide.
            shortPanel = DesignPanel("Area short of stars", 800);
            var body = K.BluePanel(shortPanel); var frame = FrameOf(body); K.Inset(frame, 40, 90, 40, 22);
            K.Ribbon(shortPanel, 690, 0, "Not Enough Stars!", 52);
            closeFor[Short] = K.RedClose(shortPanel, 650, 47, 100); closeFor[Short].onClick.AddListener(home.ClosePanel);
            // A little board of our pieces, an arrow, and the star it earns.
            var board = K.Surface("Board", shortPanel, K.C(250, 244, 226), K.C(226, 214, 186), 16); K.At(board.rectTransform, 120, 247, 200, 200);
            var tray = K.Layer("Tray", board.transform, 8, K.C(214, 224, 236), K.C(190, 204, 222), 10);
            int[] colours = { 3, 4, 0, 0, 0, 3, 4, 3, 4 };
            for (int i = 0; i < 9; i++)
            {
                var art = RoyalPieceSkin.For(ItemsTypes.NONE, colours[i]);
                var piece = K.Picture("Piece", (RectTransform)tray.transform, art, 0, 0, 56, 56);
                var r = piece.rectTransform; r.anchorMin = r.anchorMax = new Vector2((i % 3 + .5f) / 3f, 1 - (i / 3 + .5f) / 3f); r.anchoredPosition = Vector2.zero;
            }
            var arrow = K.Node("Arrow", shortPanel); K.Centre(arrow, 390, 222, 150, 60);
            var shaft = K.Surface("Shaft", arrow, K.C(252, 250, 235), K.C(232, 226, 205), 8); K.Centre(shaft.rectTransform, 66, 30, 104, 16);
            foreach (var s in new[] { -1f, 1f })
            {
                var head = K.Surface("Head", arrow, K.C(252, 250, 235), K.C(232, 226, 205), 8); K.Centre(head.rectTransform, 112, 30 + s * 13, 46, 16);
                head.rectTransform.localRotation = Quaternion.Euler(0, 0, s * 40);
            }
            K.Picture("Star", shortPanel, K.Star, 503, 348, 200, 190);
            var strip = K.Surface("Caption strip", shortPanel, K.C(9, 50, 104), K.C(14, 58, 116), 14); K.At(strip.rectTransform, 100, 483, 520, 70);
            var caption = K.Words("Caption", strip.transform, "Beat levels to earn stars", 33, K.Cream, TextAnchor.MiddleCenter); K.Inset(caption.rectTransform, 12, 6, 12, 6);
            shortContinue = K.GreenButton("Continue", shortPanel, 163, 580, 394, 160, out var face);
            var word = K.Words("Caption", face, "Continue", 62, K.Cream, TextAnchor.MiddleCenter); K.ButtonCaption(word);
            shortContinue.onClick.AddListener(() => { home.ClosePanel(); home.OpenNextLevel(); });
        }
        static RectTransform FrameOf(RectTransform body) { var t = body; while (t.parent && t.name != "Area panel frame") t = (RectTransform)t.parent; return t; }

        RectTransform newPanel; Text newTitle, newName, newNote; Button unlock; RectTransform newThumbMask, newLock, newNoteStrip; Image newThumb; RectTransform newGrey;
        void BuildNewPanel()
        {
            // "New Area!" (A-19): the next area's name on a gold header over its scene under a padlock, and Unlock.
            newPanel = DesignPanel("Area new area", 972);
            var body = K.BluePanel(newPanel); var frame = FrameOf(body); K.Inset(frame, 36, 90, 40, 22);
            newTitle = K.Ribbon(newPanel, 720, 0, "New Area!", 60);
            closeFor[Unlocked] = K.RedClose(newPanel, 646, 64, 100); closeFor[Unlocked].onClick.AddListener(home.ClosePanel);
            closeFor[Soon] = closeFor[Unlocked];
            var inner = K.Surface("Picture frame", newPanel, K.C(252, 206, 0), K.C(233, 124, 0), 20); K.At(inner.rectTransform, 96, 214, 528, 470);
            var header = K.Layer("Header", inner.transform, 6, K.C(255, 214, 40), K.C(250, 180, 0), 14);
            header.rectTransform.anchorMin = new Vector2(0, 1); header.rectTransform.anchorMax = new Vector2(1, 1); header.rectTransform.pivot = new Vector2(.5f, 1);
            header.rectTransform.sizeDelta = new Vector2(-12, 104); header.rectTransform.anchoredPosition = new Vector2(0, -6);
            newName = K.Words("Name", header.transform, "", 50, K.Cream, TextAnchor.MiddleCenter); K.Inset(newName.rectTransform, 16, 6, 16, 6); K.Title(newName);
            newThumbMask = K.Node("Scene", inner.transform); K.Inset(newThumbMask, 6, 112, 6, 6);
            newThumbMask.gameObject.AddComponent<RectMask2D>();
            newGrey = K.Surface("Locked scene", newThumbMask, K.C(150, 164, 184), K.C(96, 110, 132), 0).rectTransform;
            newThumb = K.Node("Scene art", newThumbMask).gameObject.AddComponent<Image>(); newThumb.raycastTarget = false;
            newLock = K.Padlock(inner.GetComponent<RectTransform>(), 264, 300, 230);
            unlock = K.GreenButton("Unlock", newPanel, 166, 710, 390, 170, out var face);
            var word = K.Words("Caption", face, "Unlock", 66, K.Cream, TextAnchor.MiddleCenter); K.ButtonCaption(word);
            unlock.onClick.AddListener(UnlockNext);
            newNoteStrip = K.Surface("Note strip", newPanel, K.C(9, 50, 104), K.C(14, 58, 116), 14).rectTransform; K.At(newNoteStrip, 96, 716, 528, 150);
            newNote = K.Words("Note", newNoteStrip, "", 32, K.Cream, TextAnchor.MiddleCenter); K.Inset(newNote.rectTransform, 18, 10, 18, 10);
        }

        RectTransform stage; Image stageChest; RectTransform stageChestRect; Text stageHint; Button stageTap; RectTransform stageItems; Text stageCoins; Image stageCoin, stageBoost; int stageStep;
        void BuildStage()
        {
            // The chest ceremony (A-19): the chest centre stage, Tap to Open, the rewards over it, Tap to Continue.
            stage = K.Node("Area chest", root); stage.gameObject.SetActive(false);
            stageTap = stage.gameObject.AddComponent<Image>().gameObject.AddComponent<Button>(); stage.GetComponent<Image>().color = Color.clear;
            stageTap.transition = Selectable.Transition.None; stageTap.onClick.AddListener(() => stageTapped = true);
            var mid = K.Node("Middle", stage); mid.anchorMin = mid.anchorMax = new Vector2(.5f, .5f); mid.sizeDelta = new Vector2(720, 1612); mids.Add(mid);
            stageChest =K.Picture("Chest", mid, K.Chest, 369, 956, 290, 230); stageChestRect = stageChest.rectTransform;
            stageItems = K.Node("Rewards", mid);
            var coinArt = RoyalPieceSkin.CampaignArt("coin");
            stageCoin = K.Picture("Coin", stageItems, coinArt ? coinArt : K.Star, 290, 690, 120, 120);
            stageCoins = K.Words("Coins", stageItems, "", 40, K.Cream, TextAnchor.MiddleCenter); K.Centre(stageCoins.rectTransform, 290, 768, 200, 50); K.Outline(stageCoins, K.Navy, 3);
            stageBoost = K.Picture("Booster", stageItems, null, 450, 700, 130, 130);
            stageHint = K.Words("Hint", mid, "Tap to Open", 46, K.Cream, TextAnchor.MiddleCenter); K.Centre(stageHint.rectTransform, 360, 1168, 600, 70); K.Outline(stageHint, K.Navy, 3);
        }

        RectTransform banner; RectTransform bannerRibbon; Text bannerWord;
        void BuildBanner()
        {
            // "Area Completed!" (A-19): a ribbon across the top half, over confetti.
            banner = K.Node("Area completed", root); banner.gameObject.SetActive(false);
            var dim = K.Node("Dim", banner).gameObject.AddComponent<Image>(); dim.color = new Color(.01f, .03f, .08f, .45f);
            var skip = dim.gameObject.AddComponent<Button>(); skip.transition = Selectable.Transition.None; skip.onClick.AddListener(() => stageTapped = true);
            var mid = K.Node("Middle", banner); mid.anchorMin = mid.anchorMax = new Vector2(.5f, .5f); mid.sizeDelta = new Vector2(720, 1612); mids.Add(mid);
            bannerRibbon = K.At(K.Node("Ribbon", mid), 0, 330, 720, 160);
            foreach (var side in new[] { 0, 1 })
            {
                var tail = K.Surface("Tail", bannerRibbon, K.C(70, 60, 170), K.C(46, 38, 128), 6); K.At(tail.rectTransform, side == 0 ? -6 : 616, 44, 110, 100);
                tail.rectTransform.localRotation = Quaternion.Euler(0, 0, side == 0 ? 8 : -8);
            }
            var band = K.Surface("Band", bannerRibbon, K.C(252, 206, 0), K.C(233, 124, 0), 14); K.At(band.rectTransform, 40, 24, 640, 112);
            K.Layer("Face", band.transform, 7, K.C(96, 84, 214), K.C(62, 50, 168), 10);
            bannerWord = K.Words("Words", band.transform, "Area Completed!", 58, K.Cream, TextAnchor.MiddleCenter); K.Inset(bannerWord.rectTransform, 20, 8, 20, 8); K.Title(bannerWord);
        }

        RectTransform showcase; Text showcaseHint; Button showcaseTap;
        void BuildShowcase()
        {
            // The restored area on its own, with Continue (A-19).
            showcase = K.Node("Area showcase", root); showcase.gameObject.SetActive(false);
            var hit = showcase.gameObject.AddComponent<Image>(); hit.color = Color.clear;
            showcaseTap = showcase.gameObject.AddComponent<Button>(); showcaseTap.transition = Selectable.Transition.None; showcaseTap.onClick.AddListener(() => stageTapped = true);
            var mid = K.Node("Bottom", showcase); mid.anchorMin = mid.anchorMax = new Vector2(.5f, 0); mid.pivot = new Vector2(.5f, 0); mid.sizeDelta = new Vector2(720, 260); mids.Add(mid);
            showcaseHint = K.Words("Continue", mid, "Continue", 52, K.Cream, TextAnchor.MiddleCenter); K.At(showcaseHint.rectTransform, 60, 96, 600, 80); K.Outline(showcaseHint, K.Navy, 3.2f);
        }

        // ---- the Areas tab (A-20)
        sealed class Card { public int number; public string state; public RectTransform rect; public Image scene; public Text tab, name; public RectTransform bar, barFill, done, lockBadge; public Text barCount; public Button view; public RoyalPanelGraphic tabFace, ribbonFace, frameFace; public RectTransform grey, lockNote; public Text lockWords; }
        readonly List<Card> cards = new List<Card>();
        RectTransform page; ScrollRect pageScroll; RectTransform pageContent;
        const float CardPitch = 653, CardTop = 54;

        void BuildPage()
        {
            page = K.Node("Areas tab", root); page.gameObject.SetActive(false);
            var back = K.Surface("Background", page, K.C(49, 65, 84), K.C(56, 74, 96), 0); back.raycastTarget = true;
            // The list scrolls under the title bar and above the tab bar.
            var viewport = K.Node("Viewport", page); viewport.gameObject.AddComponent<RectMask2D>();
            pageScroll = page.gameObject.AddComponent<ScrollRect>(); pageScroll.horizontal = false; pageScroll.vertical = true;
            pageScroll.movementType = ScrollRect.MovementType.Elastic; pageScroll.scrollSensitivity = 30; pageScroll.viewport = viewport;
            pageContent = K.Node("Content", viewport); pageContent.anchorMin = new Vector2(.5f, 1); pageContent.anchorMax = new Vector2(.5f, 1); pageContent.pivot = new Vector2(.5f, 1);
            pageContent.sizeDelta = new Vector2(720, 1000);
            pageScroll.content = pageContent;
            // The title bar: a blue band down to the orange trim, "Areas" on it.
            var bar = K.Node("Title bar", page); bar.anchorMin = new Vector2(0, 1); bar.anchorMax = Vector2.one; bar.pivot = new Vector2(.5f, 1);
            bar.sizeDelta = new Vector2(0, 211); bar.anchoredPosition = Vector2.zero;
            K.Surface("Blue", bar, K.C(0, 64, 130), K.C(0, 74, 158), 0);
            var trim = K.Surface("Trim", bar, K.C(255, 167, 0), K.C(255, 88, 0), 0); trim.rectTransform.anchorMax = new Vector2(1, 0); trim.rectTransform.sizeDelta = new Vector2(0, 9);
            trim.rectTransform.anchorMin = Vector2.zero; trim.rectTransform.pivot = new Vector2(.5f, 0);
            var title = K.Words("Title", bar, "Areas", 64, K.Cream, TextAnchor.MiddleCenter); K.Inset(title.rectTransform, 100, 104, 100, 20); K.Title(title);
            titleBar = bar; pageViewport = viewport;
        }
        RectTransform titleBar, pageViewport;

        Card NewCard(int number)
        {
            var c = new Card { number = number };
            c.rect = K.Node("Area card " + number, pageContent); c.rect.anchorMin = c.rect.anchorMax = new Vector2(.5f, 1); c.rect.pivot = new Vector2(.5f, 1); c.rect.sizeDelta = new Vector2(720, CardPitch);
            var divider = K.Surface("Divider", c.rect, K.C(27, 50, 61), K.C(27, 50, 61), 1); K.At(divider.rectTransform, 68, CardPitch - 58, 586, 3);
            // The picture: a gold and red frame around a landscape window onto the area's scene.
            var frame = K.Surface("Frame", c.rect, K.C(193, 0, 47), K.C(132, 31, 47), 26); K.At(frame.rectTransform, 59, 134, 602, 402);
            c.frameFace = K.Layer("Gold", frame.transform, 4, K.C(253, 176, 2), K.C(252, 160, 2), 22);
            var window = K.Node("Window", c.frameFace.transform); K.Inset(window, 6, 6, 6, 18); window.gameObject.AddComponent<RectMask2D>();
            c.grey = K.Surface("Locked scene", window, K.C(160, 172, 190), K.C(104, 116, 136), 0).rectTransform;
            c.scene = K.Node("Scene", window).gameObject.AddComponent<Image>(); c.scene.raycastTarget = false; c.scene.preserveAspect = false;
            // The ribbon with the name, and the "Area N" tab over it.
            var ribbon = K.Surface("Ribbon", c.rect, K.C(132, 30, 46), K.C(113, 25, 40), 18); K.At(ribbon.rectTransform, 59, 57, 602, 118);
            var trims = K.Layer("Trims", ribbon.transform, 4, K.C(253, 176, 2), K.C(239, 151, 2), 14);
            c.ribbonFace = K.Layer("Face", trims.transform, 6, K.C(185, 44, 60), K.C(176, 34, 52), 10);
            c.name = K.Words("Name", c.ribbonFace.transform, "", 52, K.Cream, TextAnchor.MiddleCenter); K.Inset(c.name.rectTransform, 30, 6, 30, 8); K.Title(c.name);
            var tab = K.Surface("Tab", c.rect, K.C(122, 14, 44), K.C(115, 42, 49), 16); K.At(tab.rectTransform, 239, 0, 242, 70);
            c.tabFace = K.Layer("Face", tab.transform, 3, K.C(246, 234, 196), K.C(226, 208, 160), 14);
            c.tab = K.Words("Area", c.tabFace.transform, "Area " + number, 34, K.C(122, 30, 40), TextAnchor.MiddleCenter); K.Inset(c.tab.rectTransform, 8, 4, 8, 24);
            // Current: its progress bar. Done: Completed! and View. Locked: a padlock.
            // Measured on the reference cards (a18, a19): the bar 185-535 x 454-514, Completed! 90-350 x 420-484, View 430-622 x 403-487.
            c.bar = K.ProgressBar(c.rect, 185, 454, 350, 60, out c.barFill, out c.barCount, 32);
            c.done = K.At(K.Node("Completed", c.rect), 90, 420, 262, 64);
            var strip = K.Surface("Strip", c.done, new Color(0, 0, 0, .30f), new Color(0, 0, 0, .20f), 12); K.Inset(strip.rectTransform, 0, 6, 0, 6);
            K.Check(c.done, 30, 32, 50);
            var doneWord = K.Words("Words", c.done, "Completed!", 32, K.Cream, TextAnchor.MiddleLeft); K.Inset(doneWord.rectTransform, 58, 4, 4, 4); K.Outline(doneWord, K.C(20, 50, 10), 2.4f);
            c.view = K.GreenButton("View", c.rect, 430, 403, 192, 84, out var face);
            var viewWord = K.Words("Caption", face, "View", 40, K.Cream, TextAnchor.MiddleCenter); K.ButtonCaption(viewWord);
            int n = number; c.view.onClick.AddListener(() => ChooseArea(n));
            c.lockBadge = K.Padlock(c.rect, 360, 350, 220);
            // Locked: why, in words, on a dark strip across the grey window (UI polish, 2026-09-30; it was a padlock alone).
            c.lockNote = K.At(K.Node("Lock note", c.rect), 110, 456, 500, 58);
            var noteStrip = K.Surface("Strip", c.lockNote, new Color(0, .05f, .16f, .55f), new Color(0, .05f, .16f, .45f), 14); K.Inset(noteStrip.rectTransform, 0, 0, 0, 0);
            c.lockWords = K.Words("Words", c.lockNote, "", 30, K.Cream, TextAnchor.MiddleCenter); K.Inset(c.lockWords.rectTransform, 12, 4, 12, 4); K.Outline(c.lockWords, K.C(10, 20, 45), 2);
            cards.Add(c);
            return c;
        }

        RectTransform viewer; Button viewClose; RoyalAreaInfo viewing;
        void BuildViewer()
        {
            // A restored area full screen, replaying its restoration, with the red close at the bottom right (A-21).
            viewer = K.Node("Area view", home.safeArea); viewer.gameObject.SetActive(false);
            var corner = K.Node("Corner", viewer); corner.anchorMin = corner.anchorMax = new Vector2(1, 0); corner.pivot = new Vector2(1, 0); corner.sizeDelta = new Vector2(200, 200);
            viewClose = K.RedClose(corner, 112, 90, 100);
            viewClose.onClick.AddListener(EndView);
        }

        // ================================================================ opening and filling

        /// <summary>Hides every area dialog; the Home dialog calls this before it opens any panel.</summary>
        public void Hide()
        {
            if (!root) return;
            StopCeremony(); guided = false; if (doItArrow) doItArrow.gameObject.SetActive(false);
            showing = null; root.gameObject.SetActive(false);
            if (navDim) navDim.gameObject.SetActive(false);
            if (home.dialogRect) home.dialogRect.gameObject.SetActive(true);
            var veil = home.dialog ? home.dialog.GetComponent<Image>() : null;
            if (veil && veilKept) { veil.color = veilColor; veilKept = false; }
        }

        /// <summary>Fills the dialog for one of the area panels (called by RoyalHomeScreen.OpenPanel).</summary>
        public void Fill(string panel, ref string title, ref string copy, ref string detail, ref int icon)
        {
            // The Home card and its veil give way to the area dialog's own panel and dim.
            showing = panel; root.gameObject.SetActive(true); root.SetAsLastSibling();
            if (home.dialogRect) home.dialogRect.gameObject.SetActive(false);
            var veil = home.dialog.GetComponent<Image>();
            if (veil && !veilKept) { veilColor = veil.color; veilKept = true; veil.color = Color.clear; }
            bool dialogLike = panel == Tasks || panel == Short || panel == Unlocked || panel == Soon;
            outside.gameObject.SetActive(dialogLike || panel == Chest);
            outsideButton.interactable = dialogLike;
            navDim.gameObject.SetActive(dialogLike || panel == Chest); navDim.transform.SetAsLastSibling();
            foreach (var p in new[] { taskPanel, shortPanel, newPanel }) p.gameObject.SetActive(false);
            stage.gameObject.SetActive(false); banner.gameObject.SetActive(false); showcase.gameObject.SetActive(false); page.gameObject.SetActive(false);
            ShowTooltip(false);
            title = panel; icon = 5;
            switch (panel)
            {
                case Tasks: guided = openGuided; ShowTasks(); Opened(taskPanel); break;
                case Short: Opened(shortPanel); break;
                case Unlocked: case Soon: ShowNewArea(panel == Unlocked); Opened(newPanel); break;
                case Chest: ceremony = StartCoroutine(ChestCeremony()); break;
                case List: ShowPage(); break;
            }
        }

        // Pins a design panel under the Home counters, scales it to the layout width and settles it in (A-08).
        void Opened(RectTransform panel)
        {
            panel.gameObject.SetActive(true); panel.SetAsLastSibling();
            LayoutPanel(panel);
            openedPanel = panel; openedAt = Time.unscaledTime;
        }
        RectTransform openedPanel; float openedAt = -10;
        void LayoutPanel(RectTransform panel)
        {
            var safe = home.safeArea.rect; float w = home.layout.rect.width, k = w / 720f;
            float designHeight = panel == taskPanel ? 942 : panel == shortPanel ? 800 : 972;
            // The least height the panel's content needs (its lowest control and the frame under it), in design units.
            float needed = panel == taskPanel ? 600 : panel == shortPanel ? 790 : 940;
            float fraction = panel == taskPanel ? .179f : panel == shortPanel ? .212f : .205f;
            float below = w * .182f + 14, above = safe.height - w * .2652f - 16;   // under the top bar, over the tab bar
            float top = Mathf.Max(safe.height * fraction, below);
            // A screen shorter than the reference phone's (16:9, a tablet) first gives up the panel's top margin, then scales the
            // whole panel down, so nothing ever sits under the tab bar.
            if ((above - top) / k < needed) top = Mathf.Max(below, above - needed * k);
            float room = (above - top) / k, fit = Mathf.Clamp(room / needed, .3f, 1);
            float height = Mathf.Clamp(room / fit, needed, designHeight);
            panel.sizeDelta = new Vector2(720, height); panel.anchoredPosition = new Vector2(0, -top);
            float settle = Motion ? Mathf.Clamp01((Time.unscaledTime - openedAt) / OpenSettle) : 1;
            float s = k * fit * (panel == openedPanel ? Mathf.Lerp(.96f, 1, 1 - (1 - settle) * (1 - settle)) : 1);
            panel.localScale = new Vector3(s, s, 1);
        }

        int reopenDone = -1; bool forceTasks, reopenComplete;
        void ShowTasks()
        {
            var area = RoyalAreas.CurrentArea; if (area == null) return;
            taskTitle.text = area.name;
            int done = RoyalAreas.DoneCount, n = area.tasks.Length;
            bool reopening = reopenDone >= 0;
            // Right after a build the bar still shows the count before it: it moves on in the reopen sequence.
            SetBar(barFill, barCount, reopening && !reopenComplete ? done : reopening ? done - 1 : done, n);
            completedLine.gameObject.SetActive(false);
            int task = reopening ? reopenDone : RoyalAreas.NextTask;
            ShowRow(task, reopening);
            // The first list, when the guide brought the player here, has no close (the reference's first list has none).
            closeFor[Tasks].gameObject.SetActive(!guided);
            outsideButton.interactable = !guided;
            doItArrow.gameObject.SetActive(guided && !reopening);
        }
        void ShowRow(int task, bool done)
        {
            var area = RoyalAreas.CurrentArea;
            rowTask = task;
            bool has = area != null && task >= 0 && task < area.tasks.Length;
            row.gameObject.SetActive(has); rowSlide.anchoredPosition = Vector2.zero; SetAlpha(rowSlide, 1);
            if (!has) return;
            var t = area.tasks[task];
            rowArt.sprite = RoyalAreas.Art(string.IsNullOrEmpty(t.icon) ? t.patch : t.icon); rowArt.enabled = rowArt.sprite;
            rowName.text = t.name; doItCost.text = t.cost.ToString();
            doIt.gameObject.SetActive(!done); doIt.interactable = !done && building == null; doItRect.localScale = Vector3.one;
            tick.gameObject.SetActive(false);
        }
        static void SetBar(RectTransform fill, Text count, int done, int total)
        {
            fill.anchorMax = new Vector2(total > 0 ? Mathf.Clamp01((float)done / total) : 0, 1);
            count.text = done + "/" + total;
        }

        int shortTask = -1;

        void ShowNewArea(bool ready)
        {
            var area = RoyalAreas.CurrentArea;
            newTitle.text = ready ? "New Area!" : RoyalAreas.AllRestored ? "All Restored!" : "Coming Soon";
            newName.text = area != null ? area.name : "The Royal Realm";
            var art = ready && area != null ? RoyalAreas.Art(area.before) : null;
            newThumb.sprite = art; newThumb.enabled = art; newGrey.gameObject.SetActive(!art);
            if (art) Window(newThumb, art, newThumbMask);
            newLock.gameObject.SetActive(!RoyalAreas.AllRestored); newLock.localRotation = Quaternion.identity; newLock.localScale = Vector3.one;
            if (newLock.GetComponent<CanvasGroup>()) newLock.GetComponent<CanvasGroup>().alpha = 1;
            unlock.gameObject.SetActive(ready); unlock.interactable = ready;
            newNoteStrip.gameObject.SetActive(!ready);
            newNote.text = RoyalAreas.AllRestored ? "The whole kingdom shines again." : "This area is being prepared. Keep winning levels: your stars are saved for it.";
        }

        // A landscape window onto a portrait scene, centred where the scene's subject stands (40% down).
        static void Window(Image image, Sprite art, RectTransform window)
        {
            var r = image.rectTransform; r.anchorMin = r.anchorMax = new Vector2(.5f, .5f); r.pivot = new Vector2(.5f, .5f);
            float w = Mathf.Max(1, window.rect.width), aspect = art.rect.height / art.rect.width, h = w * aspect;
            r.sizeDelta = new Vector2(w, h); r.anchoredPosition = new Vector2(0, h * (.40f - .5f) * -1);
        }

        // ================================================================ taps

        void OutsideTap()
        {
            if (building != null || unlocking) return;
            if (showing == Tasks || showing == Short || showing == Unlocked || showing == Soon) home.ClosePanel();
        }

        void ShowTooltip(bool show)
        {
            if (building != null) show = false;
            var area = RoyalAreas.CurrentArea;
            if (show && area != null)
            {
                tooltipCoins.text = area.rewardCoins.ToString("N0");
                var boost = BoostArt(area.rewardBoost);
                tooltipBoost.sprite = boost; tooltipBoost.enabled = boost; tooltipPlus.enabled = boost;
            }
            // Under the bubble, a clear layer over everything else takes the next tap and puts the bubble away.
            tooltipDismiss.gameObject.SetActive(show); tooltipDismiss.transform.SetAsLastSibling();
            tooltip.gameObject.SetActive(show); tooltip.SetAsLastSibling();
        }

        static Sprite BoostArt(string boost)
        {
            switch (boost)
            {
                case "Stripes": return RoyalPieceSkin.For(ItemsTypes.HORIZONTAL_STRIPPED, 0);
                case "Packages": return RoyalPieceSkin.For(ItemsTypes.PACKAGE, 0);
                case "Colorful_bomb": return RoyalPieceSkin.For(ItemsTypes.BOMB, 0);
                default: return null;
            }
        }

        void Update()
        {
            if (!root) return;
            // Back ends a view; while a task builds or the new area unlocks it does nothing (the Home is not in a dialog then,
            // so without this the quit prompt would open).
            if (InputManager.Instance != null && InputManager.Instance.Gameplay.Cancel.WasPressedThisFrame())
            {
                if (viewing != null) { EndView(); RoyalBackButton.Consume(); }
                else if ((building != null || unlocking) && !home.dialog.activeSelf) RoyalBackButton.Consume();
            }
            if (viewing != null && !home.IsHomeVisible) EndViewQuietly();
            PulseNewArea();
            PlaceGuide();
            if (doItArrow && doItArrow.gameObject.activeInHierarchy) doItArrow.anchoredPosition = new Vector2(466, -(250 + (Motion ? Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4.2f)) * 20 : 0)));
        }

        void LateUpdate()
        {
            if (!root || !root.gameObject.activeSelf) return;
            foreach (var p in new[] { taskPanel, shortPanel, newPanel }) if (p.gameObject.activeSelf) LayoutPanel(p);
            if (page.gameObject.activeSelf) LayoutPage();
            float k = home.layout.rect.width / 720f;
            foreach (var m in mids) if (m.gameObject.activeInHierarchy) m.localScale = new Vector3(k, k, 1);
            // Keyboard focus follows the area dialog's own close control, not the hidden Home card's.
            var es = EventSystem.current; var close = CloseButton;
            if (es && close && es.currentSelectedGameObject == home.closeButton.gameObject) es.SetSelectedGameObject(close.gameObject);
        }

        // The star counter's value while stars are in flight (the counter counts down as each leaves), and the Area card's
        // "New Area" dress, applied last before the canvases draw.
        int heldStars = -1, heldCoins = -1;
        readonly List<RectTransform> mids = new List<RectTransform>();
        void HoldLabels()
        {
            if (!home || !home.starLabel) return;
            if (heldStars >= 0) home.starLabel.text = heldStars.ToString();
            if (heldCoins >= 0 && home.coinLabel) home.coinLabel.text = heldCoins.ToString("N0");
        }

        // ================================================================ building a task (A-09 .. A-14)

        Coroutine building;
        void OnDisable() { if (building != null) { building = null; FinishBuild(); } StopCeremony(); }

        void DoIt()
        {
            if (building != null || ceremony != null || rowTask < 0) return;
            var area = RoyalAreas.CurrentArea; int task = rowTask;
            if (area == null || task != RoyalAreas.NextTask) return;
            int before = home.Progress.available, cost = area.tasks[task].cost;
            // The scene must not redraw from the save while the build stages the new object.
            scene.Hold = true;
            var result = RoyalAreas.Build(task, home.Progress.stars);
            if (result == RoyalAreas.BuildResult.NotEnoughStars) { scene.Hold = false; shortTask = task; home.OpenPanel(Short, doIt.gameObject); return; }
            if (result != RoyalAreas.BuildResult.Built) { scene.Hold = false; return; }
            doIt.interactable = false;
            if (guided) { guided = false; RoyalThrone.Testing.RoyalSave.SetInt(GuidedKey, 1); RoyalThrone.Testing.RoyalSave.Save(); doItArrow.gameObject.SetActive(false); }
            building = StartCoroutine(BuildMotion(area, task, before, cost, RoyalAreas.Complete));
        }

        IEnumerator BuildMotion(RoyalAreaInfo area, int task, int before, int cost, bool last)
        {
            try
            {
                heldStars = before;
                // 1. The stars leave the counter one after another and arc into Do It (an area's last task skips this: its
                //    stars go while the Home is away, as observed).
                if (!last)
                {
                    int landed = 0;
                    // From the counter's star icon (the reference's stars leave from there).
                    Transform from = home.resourceButtons != null && home.resourceButtons.Length > 3 ? home.resourceButtons[3].transform.Find("Imported 16674") : null;
                    if (!from) from = home.starLabel.transform;
                    for (int i = 0; i < cost; i++)
                    {
                        int k = i;
                        if (!RoyalWinSequence.AreaStar(from, doIt.transform, k * StarStagger, StarFlight, () => heldStars = before - k - 1, () => landed++))
                        { heldStars = before - k - 1; landed++; }
                    }
                    float until = Time.unscaledTime + 3; while (landed < cost && Time.unscaledTime < until) yield return null;
                }
                // 2. Do It flashes and shrinks away; a last task shows its tick before the list goes.
                yield return ShrinkDoIt();
                if (last) { tick.gameObject.SetActive(true); yield return Pop(tick, TickPop); yield return Wait(.3f); }
                else yield return Wait(Motion ? RowBare : 0);
                // 3. The list goes in one frame, and the Home controls slide off; the scene waits a moment, empty.
                home.ClosePanel();
                yield return SlideHud(1);
                if (last) heldStars = before - cost;
                yield return Wait(Motion ? PauseBeforeRise : .1f);
                // 4. The object rises into its place with a puff; the scene holds it a moment.
                yield return scene.Rise(task, Motion ? RiseTime : .3f);
                yield return Wait(Motion ? HoldAfterRise : .4f);
                // 5. The controls come back; a streak runs from the object to the Area card, whose count moves on as it lands.
                yield return SlideHud(0);
                bool arrived = false;
                var patch = scene.Patch(task);
                if (!RoyalWinSequence.AreaStreak(patch ? patch.transform : scene.transform, home.areaButton.transform, StreakTime, () => arrived = true)) arrived = true;
                float t1 = Time.unscaledTime + 2.5f; while (!arrived && Time.unscaledTime < t1) yield return null;
                home.RefreshProgress(); heldStars = -1;
                StartCoroutine(Pulse((RectTransform)home.areaButton.transform, .06f, .3f));
                yield return Wait(.25f);
                if (!home.IsHomeVisible || !isActiveAndEnabled) yield break;
                // 6. The list comes back on the task just done: its tick pops, the row leaves and the next task comes in.
                reopenDone = task; reopenComplete = last; forceTasks = true;
                home.OpenPanel(Tasks, home.areaButton.gameObject);
                forceTasks = false; reopenDone = -1;
                if (showing != Tasks) yield break;
                if (last)
                {
                    // The last one: the list says the area is complete and its bar fills; then the chest comes out.
                    tick.gameObject.SetActive(true);
                    yield return Wait(.2f); yield return Pop(tick, TickPop); yield return Wait(.35f);
                    yield return SlideRow(0, -1, RowOut);
                    row.gameObject.SetActive(false); completedLine.gameObject.SetActive(true);
                    yield return Pop(completedLine.rectTransform, .25f);
                    yield return FillBar(barFill, barCount, area.tasks.Length - 1, area.tasks.Length, .6f);
                    yield return Wait(.5f);
                    chestFrom = chestButton.transform.position; chestFromSet = true;
                    building = null; FinishBuild();
                    home.OpenPanel(Chest, home.areaButton.gameObject);
                    yield break;
                }
                tick.gameObject.SetActive(true);
                yield return Wait(.2f); yield return Pop(tick, TickPop); yield return Wait(.45f);
                yield return SlideRow(0, -1, RowOut);
                SetBar(barFill, barCount, RoyalAreas.DoneCount, area.tasks.Length);
                ShowRow(RoyalAreas.NextTask, false);
                doIt.interactable = false;
                yield return SlideRow(1, 0, RowIn);
            }
            finally { building = null; FinishBuild(); }
        }
        void FinishBuild()
        {
            heldStars = -1; forceTasks = false; reopenDone = -1;
            if (homeLayout) homeLayout.HudAway = 0;
            if (scene) scene.Hold = false;
            if (doIt && showing == Tasks) doIt.interactable = true;
        }

        IEnumerator ShrinkDoIt()
        {
            var face = doItRect; float t = 0, len = Motion ? Shrink : .05f;
            var flash = K.Surface("Flash", face, new Color(1, 1, .8f, .0f), new Color(1, 1, .8f, 0), 16);
            while (t < len + .1f)
            {
                t += Time.unscaledDeltaTime;
                float f = Mathf.Clamp01(t / .1f), u = Mathf.Clamp01((t - .1f) / len);
                flash.color = new Color(1, 1, 1, .6f * f * (1 - u)); flash.SetVerticesDirty();
                face.localScale = Vector3.one * (1 - u * u);
                yield return null;
            }
            Destroy(flash.gameObject);
            doIt.gameObject.SetActive(false); face.localScale = Vector3.one;
        }

        IEnumerator SlideHud(float to)
        {
            if (!homeLayout) yield break;
            float from = homeLayout.HudAway, t = 0, len = Motion ? HudSlide : .01f;
            while (t < len) { t += Time.unscaledDeltaTime; float u = Mathf.Clamp01(t / len); homeLayout.HudAway = Mathf.Lerp(from, to, u * u * (3 - 2 * u)); yield return null; }
            homeLayout.HudAway = to;
        }

        IEnumerator SlideRow(float fromAlpha, float direction, float len)
        {
            float t = 0; len = Motion ? len : .01f;
            while (t < len)
            {
                t += Time.unscaledDeltaTime; float u = Mathf.Clamp01(t / len), e = u * u * (3 - 2 * u);
                // Out: slides left and fades; in: arrives from the right.
                float x = fromAlpha > .5f ? (1 - e) * 80 : direction * e * 120;
                rowSlide.anchoredPosition = new Vector2(x, 0); SetAlpha(rowSlide, fromAlpha > .5f ? e : 1 - e);
                yield return null;
            }
        }

        static void SetAlpha(RectTransform r, float a)
        {
            var g = r.GetComponent<CanvasGroup>(); if (!g) g = r.gameObject.AddComponent<CanvasGroup>();
            g.alpha = a; g.blocksRaycasts = a > .5f;
        }

        IEnumerator Pop(RectTransform r, float len)
        {
            float t = 0; if (!Motion) { r.localScale = Vector3.one; yield break; }
            while (t < len)
            {
                t += Time.unscaledDeltaTime; float u = Mathf.Clamp01(t / len);
                // 0 -> 1.35 at 45% -> 1: the reference's overshooting tick.
                float s = u < .45f ? Mathf.Lerp(0, 1.35f, u / .45f) : Mathf.Lerp(1.35f, 1, (u - .45f) / .55f);
                r.localScale = Vector3.one * s; yield return null;
            }
            r.localScale = Vector3.one;
        }

        IEnumerator Pulse(RectTransform r, float amount, float len)
        {
            float t = 0; var base_ = r.localScale;
            while (t < len) { t += Time.unscaledDeltaTime; float s = 1 + amount * Mathf.Sin(Mathf.Clamp01(t / len) * Mathf.PI); r.localScale = base_ * s; yield return null; }
            r.localScale = base_;
        }

        IEnumerator FillBar(RectTransform fill, Text count, int from, int to, float len)
        {
            float t = 0, total = Mathf.Max(1, to);
            while (t < len) { t += Time.unscaledDeltaTime; float u = Mathf.Clamp01(t / len); fill.anchorMax = new Vector2(Mathf.Lerp(from, to, u * u * (3 - 2 * u)) / total, 1); yield return null; }
            SetBar(fill, count, to, to);
        }

        static IEnumerator Wait(float s) { float end = Time.unscaledTime + s; while (Time.unscaledTime < end) yield return null; }

        // ================================================================ the chest ceremony (A-19)

        Coroutine ceremony; bool stageTapped; Vector3 chestFrom; bool chestFromSet;
        RoyalAreas.Reward lastReward;

        void StopCeremony()
        {
            if (ceremony != null) { StopCoroutine(ceremony); ceremony = null; }
            stageStep = 0;
            if (home) home.SceneViewOpen = viewing != null;
            if (homeLayout && building == null) homeLayout.HudAway = 0;
        }

        // The ceremony's dialog was closed under it (Back): it ends where it stands. Before the chest is opened nothing has
        // been paid, and the Area card leads back to the chest; after, only the presentation is skipped.
        bool Gone => !home.dialog.activeSelf || !root.gameObject.activeSelf;

        IEnumerator ChestCeremony()
        {
            var area = RoyalAreas.CurrentArea;
            if (area == null || !RoyalAreas.Complete) { home.ClosePanel(); ceremony = null; yield break; }
            stage.gameObject.SetActive(true); stage.SetAsLastSibling(); stageItems.gameObject.SetActive(false);
            stageHint.text = "Tap to Open"; stageHint.gameObject.SetActive(false); stageTapped = false; stageStep = 1;
            stageChestRect.localScale = Vector3.one; stageChest.enabled = true;
            // The chest comes out of the list (or the Area card) to the middle and waits there, hopping.
            var mid = (RectTransform)stageChestRect.parent;
            yield return null;   // the stage's layout (and scale) is applied before positions are read
            Vector2 target = new Vector2(369 - 360, 1612 * .5f - 956);
            Vector2 start = LocalIn(mid, chestFromSet ? chestFrom : home.areaButton.transform.position);
            chestFromSet = false;
            MidAnchor(stageChestRect, start);
            float t = 0;
            while (t < .5f)
            {
                if (Gone) { EndCeremony(false); yield break; }
                t += Time.unscaledDeltaTime; float u = Mathf.Clamp01(t / .5f), e = 1 - (1 - u) * (1 - u);
                stageChestRect.anchoredPosition = Vector2.Lerp(start, target, e) + new Vector2(0, Mathf.Sin(u * Mathf.PI) * 120);
                stageChestRect.localScale = Vector3.one * Mathf.Lerp(.5f, 1, e); yield return null;
            }
            MidAnchor(stageChestRect, target);
            float shown = Time.unscaledTime;
            while (!stageTapped)
            {
                if (Gone) { EndCeremony(false); yield break; }
                float e = Time.unscaledTime - shown;
                if (e > 1.2f && !stageHint.gameObject.activeSelf) stageHint.gameObject.SetActive(true);
                // The chest waits still; "Tap to Open" invites the tap (no idle hop or squash).
                yield return null;
            }
            // Tap to Open: the chest pays (once: CollectChest refuses a second time) and its rewards come out over it.
            stageTapped = false; stageHint.gameObject.SetActive(false);
            int coinsBefore = InitScript.Gems;
            if (!RoyalAreas.CollectChest(out lastReward)) { EndCeremony(false); yield break; }
            stageStep = 2; heldCoins = coinsBefore;
            RoyalWinSequence.AreaBurst(stageChest.transform, 1.3f);
            stageCoins.text = lastReward.coins.ToString("N0");
            var boost = BoostArt(lastReward.boost.ToString()); stageBoost.sprite = boost; stageBoost.enabled = boost;
            stageItems.gameObject.SetActive(true);
            t = 0;
            while (t < .5f)
            {
                if (Gone) { EndCeremony(true); yield break; }
                t += Time.unscaledDeltaTime; float u = Mathf.Clamp01(t / .5f), e = 1 - (1 - u) * (1 - u);
                stageChestRect.localScale = Vector3.one * (1 + .12f * Mathf.Sin(u * Mathf.PI));
                stageItems.localScale = Vector3.one * e; stageItems.anchoredPosition = new Vector2(0, -120 * (1 - e)); yield return null;
            }
            yield return Wait(.6f);
            stageHint.text = "Tap to Continue"; stageHint.gameObject.SetActive(true);
            while (!stageTapped) { if (Gone) { EndCeremony(true); yield break; } yield return null; }
            stageTapped = false; stageHint.gameObject.SetActive(false); stageStep = 3;
            // The rewards fly to their counters; the coin counter counts up as they land.
            const int Pieces = 8; int landed = 0;
            if (!RoyalWinSequence.AreaReward(stageCoin.transform, home.coinLabel.transform, Pieces, true, () => { landed++; heldCoins = coinsBefore + Mathf.RoundToInt(lastReward.coins * (float)landed / Pieces); }))
                landed = Pieces;
            if (stageBoost.enabled) RoyalWinSequence.AreaReward(stageBoost.transform, home.levelButton.transform, 1, false, null);
            stageItems.gameObject.SetActive(false);
            t = 0; while (t < .35f) { t += Time.unscaledDeltaTime; stageChestRect.localScale = Vector3.one * (1 - Mathf.Clamp01(t / .35f)); yield return null; }
            stage.gameObject.SetActive(false);
            float until = Time.unscaledTime + 2; while (landed < Pieces && Time.unscaledTime < until) yield return null;
            heldCoins = -1; home.RefreshProgress();
            // "Area Completed!", then the restored area on its own with Continue.
            yield return CompletedBanner();
            if (Gone) { EndCeremony(true); yield break; }
            yield return Showcase(area);
            EndCeremony(true);
        }

        // Ends the ceremony (finished, or cut short by Back) and gives the Home back its controls and current area.
        void EndCeremony(bool paid)
        {
            ceremony = null; stageStep = 0; heldCoins = -1;
            stage.gameObject.SetActive(false); banner.gameObject.SetActive(false); showcase.gameObject.SetActive(false);
            home.SceneViewOpen = false; scene.Hold = false; if (homeLayout) homeLayout.HudAway = 0;
            if (home.dialog.activeSelf) home.ClosePanel();
            scene.ShowCurrent(); home.RefreshProgress();
        }

        IEnumerator CompletedBanner()
        {
            stageStep = 4;
            home.SceneViewOpen = true; outside.gameObject.SetActive(false); navDim.gameObject.SetActive(false);
            StartCoroutine(SlideHud(1));
            banner.gameObject.SetActive(true); banner.SetAsLastSibling();
            RoyalWinSequence.AreaConfetti();
            float t = 0; stageTapped = false;
            while (t < 4.5f && !(stageTapped && t > 1))
            {
                if (Gone) yield break;
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / .45f), e = 1 - (1 - u) * (1 - u);
                bannerRibbon.localScale = new Vector3(Mathf.Lerp(.6f, 1, e), Mathf.Lerp(.6f, 1, e), 1);
                SetAlpha(bannerRibbon, e);
                if (stageTapped && t <= 1) stageTapped = false;
                yield return null;
            }
            stageTapped = false;
            banner.gameObject.SetActive(false);
        }

        IEnumerator Showcase(RoyalAreaInfo area)
        {
            stageStep = 5;
            scene.Hold = true; scene.Show(area, true);
            showcase.gameObject.SetActive(true); showcase.SetAsLastSibling();
            float shown = Time.unscaledTime; stageTapped = false;
            while (!stageTapped || Time.unscaledTime - shown < .6f)
            {
                if (Gone) yield break;
                if (stageTapped && Time.unscaledTime - shown < .6f) stageTapped = false;
                float p = .85f + .15f * Mathf.Sin((Time.unscaledTime - shown) * 3.2f);
                showcaseHint.color = new Color(K.Cream.r, K.Cream.g, K.Cream.b, Mathf.Clamp01((Time.unscaledTime - shown) / .4f) * p);
                yield return null;
            }
            stageTapped = false;
            showcase.gameObject.SetActive(false); scene.Hold = false;
            yield return SlideHud(0); home.SceneViewOpen = false;
        }

        static Vector2 LocalIn(RectTransform space, Vector3 world)
        {
            var canvas = space.GetComponentInParent<Canvas>(); var cam = canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(space, RectTransformUtility.WorldToScreenPoint(cam, world), cam, out var local);
            // anchoredPosition of a child anchored at the parent's centre.
            return local;
        }
        static void MidAnchor(RectTransform r, Vector2 at) { r.anchorMin = r.anchorMax = new Vector2(.5f, .5f); r.anchoredPosition = at; }

        // ================================================================ New Area! and Unlock

        bool unlocking;
        void UnlockNext()
        {
            if (unlocking || !RoyalAreas.AwaitingEntry) return;
            unlocking = true; unlock.interactable = false;
            StartCoroutine(Unlocking());
        }
        IEnumerator Unlocking()
        {
            try
            {
                // The padlock lets go (a quick shrink and fade, no wobble), then the new area is there, with a ring of sparkles.
                float t = 0, len = Motion ? LockRelease : .05f;
                var lockGroup = newLock.GetComponent<CanvasGroup>(); if (!lockGroup) lockGroup = newLock.gameObject.AddComponent<CanvasGroup>();
                while (t < len) { t += Time.unscaledDeltaTime; float u = Mathf.Clamp01(t / len), e = u * u; newLock.localScale = Vector3.one * Mathf.Lerp(1, .82f, e); lockGroup.alpha = 1 - e; yield return null; }
                newLock.localRotation = Quaternion.identity;
                if (!RoyalAreas.Enter()) yield break;
                home.ClosePanel();
                scene.ShowCurrent(); home.RefreshProgress();
                RoyalWinSequence.AreaUnlockRing(home.safeArea);
            }
            finally { unlocking = false; }
        }

        // ================================================================ the first task list (A-23)

        // The reference's first visit, after Level 1: the Home dims around the Area card with a caption and a bouncing arrow;
        // its first list has no close and an arrow at Do It. Once, until the first task is built (GuidedKey, per profile).
        public const string GuidedKey = "RoyalThrone.Area.Guided";
        public static bool NeedsGuide => RoyalThrone.Testing.RoyalSave.GetInt(GuidedKey, 0) == 0 && RoyalAreas.Current == 1 && RoyalAreas.Spent == 0 && RoyalAreas.DoneCount == 0;
        RectTransform guide, guideHit, guideArrow, doItArrow, guideBelow; Image guideTabsDim; bool guided, openGuided;
        public bool Guiding => guide && guide.gameObject.activeSelf;
        /// <summary>For the checks: the guide's dim over the tab bar (it takes the tabs' taps while the guide shows).</summary>
        public Image GuideTabsDim => guideTabsDim;
        public bool GuidedList => guided;
        public Button GuideButton => guideHit ? guideHit.GetComponent<Button>() : null;

        void BuildGuide()
        {
            // Above the Home: a canvas of its own, so the dim covers everything but the Area card. The tab bar has its own dim,
            // inside the tab bar's canvas (as navDim): on the phone the guide's canvas did not sort above the tabs.
            guide = K.Node("Area guide", home.safeArea); guide.gameObject.SetActive(false);
            guideTabsDim = K.Node("Area guide dim over the tabs", home.navigation).gameObject.AddComponent<Image>();
            guideTabsDim.color = new Color(.02f, .04f, .08f, .62f); guideTabsDim.gameObject.SetActive(false);   // takes the tabs' taps
            var canvas = guide.gameObject.AddComponent<Canvas>(); canvas.overrideSorting = true; canvas.sortingOrder = 26;
            guide.gameObject.AddComponent<GraphicRaycaster>();
            guideHit = K.Node("Area card", guide); guideHit.anchorMin = guideHit.anchorMax = new Vector2(.5f, .5f);
            var hit = guideHit.gameObject.AddComponent<Image>(); hit.color = Color.clear;
            var button = guideHit.gameObject.AddComponent<Button>(); button.transition = Selectable.Transition.None;
            // Hide() (which OpenPanel calls first) clears the guided state, so the list is asked for guided and Fill sets it.
            button.onClick.AddListener(() => { EndGuide(); openGuided = true; home.OpenPanel(Tasks, home.areaButton.gameObject); openGuided = false; });
            // The dim is four pieces around the card, anchored to its rect in multiples of its size, so the card stays bright and
            // follows it; the pieces still take every tap that misses the card.
            foreach (var (min, max) in new[] { (new Vector2(-20, 1), new Vector2(21, 40)), (new Vector2(-20, -40), new Vector2(21, 0)), (new Vector2(-20, 0), new Vector2(0, 1)), (new Vector2(1, 0), new Vector2(21, 1)) })
            {
                var piece = K.Node("Dim", guideHit); piece.anchorMin = min; piece.anchorMax = max; piece.offsetMin = piece.offsetMax = Vector2.zero;
                piece.gameObject.AddComponent<Image>().color = new Color(.02f, .04f, .08f, .62f);
                if (min.y < 0) guideBelow = piece;   // stops at the tab bar, which dims itself (PlaceGuide)
            }
            // The caption: its navy edge, then the cream bubble over it, then the words (a parent draws before its children).
            var bubble = K.Node("Caption", guideHit);
            bubble.anchorMin = bubble.anchorMax = new Vector2(.5f, 1); bubble.pivot = new Vector2(.5f, 0); bubble.sizeDelta = new Vector2(430, 110); bubble.anchoredPosition = new Vector2(-150, 150);
            var edge = K.Surface("Edge", bubble, K.C(0, 60, 128), K.C(0, 60, 128), 22); K.Inset(edge.rectTransform, -4, -4, -4, -4);
            var fill = K.Surface("Fill", bubble, K.C(252, 248, 232), K.C(236, 228, 204), 22); K.Inset(fill.rectTransform, 0, 0, 0, 0);
            var words = K.Words("Words", bubble, "Tap here to open your task list.", 30, K.Ink, TextAnchor.MiddleCenter); K.Inset(words.rectTransform, 18, 10, 18, 10);
            guideArrow = Arrow("Arrow", guideHit, true); guideArrow.anchorMin = guideArrow.anchorMax = new Vector2(.5f, 1);
            doItArrow = Arrow("Do It arrow", (RectTransform)doIt.transform.parent, false); doItArrow.gameObject.SetActive(false);
            doItArrow.anchorMin = doItArrow.anchorMax = new Vector2(0, 1); doItArrow.anchoredPosition = new Vector2(466, -250);
        }

        // A thick gold arrow, drawn: a shaft and a head, pointing down (to the Area card) or up (to Do It).
        static RectTransform Arrow(string name, RectTransform parent, bool down)
        {
            var r = K.Node(name, parent); r.sizeDelta = new Vector2(110, 130); r.pivot = new Vector2(.5f, .5f);
            var turn = K.Node("Turn", r); turn.localRotation = Quaternion.Euler(0, 0, down ? 0 : 180);
            var shaft = K.Surface("Shaft", turn, K.C(255, 214, 60), K.C(236, 150, 0), 10); K.Centre(shaft.rectTransform, 55, 42, 44, 70);
            foreach (var s in new[] { -1f, 1f })
            {
                var head = K.Surface("Head", turn, K.C(255, 214, 60), K.C(236, 150, 0), 10); K.Centre(head.rectTransform, 55 + s * 22, 88, 44, 72);
                head.rectTransform.localRotation = Quaternion.Euler(0, 0, s * 42);
            }
            return r;
        }

        /// <summary>Home after a win with the first task affordable: shows the guide instead of opening the list (the
        /// reference opens it by itself only after that). False when the guide is not due.</summary>
        public bool GuideFirstTask()
        {
            if (!NeedsGuide || !home.IsHomeVisible || home.dialog.activeSelf) return false;
            guide.gameObject.SetActive(true); guide.SetAsLastSibling();
            guideTabsDim.gameObject.SetActive(true); guideTabsDim.transform.SetAsLastSibling();
            PlaceGuide();
            return true;
        }
        void EndGuide() { if (guide) guide.gameObject.SetActive(false); if (guideTabsDim) guideTabsDim.gameObject.SetActive(false); }

        void PlaceGuide()
        {
            if (!Guiding) return;
            if (!home.IsHomeVisible || home.dialog.activeSelf) { EndGuide(); return; }
            // The opening follows the Area card; the arrow bounces over it.
            var card = (RectTransform)home.areaButton.transform; var corners = new Vector3[4]; card.GetWorldCorners(corners);
            Vector2 a = guide.InverseTransformPoint(corners[0]), c = guide.InverseTransformPoint(corners[2]);
            guideHit.sizeDelta = c - a; guideHit.anchoredPosition = (a + c) * .5f;
            // The dim under the card ends at the tab bar's top edge; below it the tab bar's own dim takes over.
            var bar = home.navigationBackground ? home.navigationBackground : home.navigation;
            if (guideBelow && bar)
            {
                var nav = new Vector3[4]; bar.GetWorldCorners(nav); float top = guide.InverseTransformPoint(nav[1]).y;
                guideBelow.anchorMin = new Vector2(-20, 0); guideBelow.anchorMax = new Vector2(21, 0);
                guideBelow.offsetMax = Vector2.zero; guideBelow.offsetMin = new Vector2(0, -Mathf.Max(0, a.y - top));
            }
            float bob = Motion ? Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4.2f)) * 22 : 0;
            guideArrow.anchoredPosition = new Vector2(0, 80 + bob);
        }

        // ================================================================ the Areas tab (A-20) and the replay (A-21)

        void ShowPage()
        {
            page.gameObject.SetActive(true); page.SetAsLastSibling();
            outside.gameObject.SetActive(false); navDim.gameObject.SetActive(false);
            var all = RoyalAreas.All;
            int current = RoyalAreas.Current, entered = RoyalAreas.Entered;
            bool complete = RoyalAreas.Complete;
            // Newest first: the areas still to come (locked) above the current one, then every restored one below it. The
            // reference listed two ahead at Area 5 and ten at Area 6 (as their previews downloaded); ours are all known.
            int top = all.Length;
            foreach (var c in cards) c.rect.gameObject.SetActive(false);
            int slot = 0;
            for (int n = top; n >= 1; n--)
            {
                var card = null as Card; foreach (var c in cards) if (c.number == n) card = c;
                if (card == null) card = NewCard(n);
                var area = all[n - 1];
                bool restored = n < current || n == current && complete;
                bool now = n == current && !complete && entered >= current && area.Ready;
                card.state = restored ? "done" : now ? "current" : "locked";
                card.rect.gameObject.SetActive(true);
                card.rect.anchoredPosition = new Vector2(0, -(CardTop + slot * CardPitch)); slot++;
                card.name.text = area.name; card.tab.text = "Area " + n;
                var art = area.Ready ? RoyalAreas.Art(restored ? area.after : area.before) : null;
                bool picture = art && card.state != "locked";
                card.scene.sprite = art; card.scene.enabled = picture; card.grey.gameObject.SetActive(!picture);
                if (picture) Window(card.scene, art, (RectTransform)card.scene.transform.parent);
                card.bar.gameObject.SetActive(now); card.done.gameObject.SetActive(restored);
                card.view.gameObject.SetActive(restored && area.Ready); card.view.interactable = restored && area.Ready;
                card.lockBadge.gameObject.SetActive(card.state == "locked");
                // The unlock rule in words: areas open one after another, each once the one before it is restored.
                card.lockNote.gameObject.SetActive(card.state == "locked");
                if (card.state == "locked")
                    card.lockWords.text = !area.Ready ? "Coming soon" : n == current && entered < current ? "Unlock it from Home" : "Complete Area " + (n - 1) + " to unlock";
                if (now) SetBar(card.barFill, card.barCount, RoyalAreas.DoneCount, area.tasks.Length);
                bool grey = card.state == "locked";
                card.ribbonFace.topColor = grey ? K.C(150, 160, 176) : K.C(185, 44, 60); card.ribbonFace.bottomColor = grey ? K.C(120, 130, 148) : K.C(176, 34, 52);
                card.tabFace.topColor = grey ? K.C(214, 220, 228) : K.C(246, 234, 196); card.tabFace.bottomColor = grey ? K.C(180, 188, 200) : K.C(226, 208, 160);
                card.frameFace.topColor = grey ? K.C(190, 198, 210) : K.C(253, 176, 2); card.frameFace.bottomColor = grey ? K.C(150, 160, 176) : K.C(252, 160, 2);
                foreach (var g in new Graphic[] { card.ribbonFace, card.tabFace, card.frameFace }) g.SetVerticesDirty();
                card.tab.color = grey ? K.C(70, 80, 96) : K.C(122, 30, 40);
            }
            pageContent.sizeDelta = new Vector2(720, CardTop + slot * CardPitch + 40);
            LayoutPage(); Canvas.ForceUpdateCanvases();
            // Opens on the current area, just under the locked ones (as the reference opens its list).
            float k = home.layout.rect.width / 720f;
            float currentY = (CardTop + Mathf.Max(0, top - current) * CardPitch - 120) * k;
            float room = Mathf.Max(1, pageContent.rect.height * k - pageViewport.rect.height);
            pageScroll.StopMovement(); pageScroll.verticalNormalizedPosition = 1 - Mathf.Clamp01(currentY / room);
        }

        /// <summary>Scrolls the Areas tab so an area's card sits in the middle of the list (what a player's swipe does).</summary>
        public void ScrollTo(int number)
        {
            Card card = null; foreach (var c in cards) if (c.number == number && c.rect.gameObject.activeSelf) card = c;
            if (card == null || !page.gameObject.activeSelf) return;
            Canvas.ForceUpdateCanvases();
            float k = home.layout.rect.width / 720f, centre = (-card.rect.anchoredPosition.y + CardPitch * .5f) * k;
            float room = Mathf.Max(1, pageContent.rect.height * k - pageViewport.rect.height);
            pageScroll.StopMovement(); pageScroll.verticalNormalizedPosition = 1 - Mathf.Clamp01((centre - pageViewport.rect.height * .5f) / room);
        }

        void LayoutPage()
        {
            float w = home.layout.rect.width, k = w / 720f, navH = w * .2652f;
            // The page reaches over the top inset (a notch) so its title bar starts at the screen's edge.
            float topInset = ((RectTransform)home.transform).rect.height * (1 - home.safeArea.anchorMax.y);
            page.offsetMax = new Vector2(0, topInset);
            titleBar.sizeDelta = new Vector2(0, 211 * k + topInset);
            foreach (Transform child in titleBar) if (child.name == "Title") { var t = (RectTransform)child; t.offsetMin = new Vector2(100 * k, 20 * k); t.offsetMax = new Vector2(-100 * k, -(104 * k + topInset)); }
            pageViewport.offsetMax = new Vector2(0, -(211 * k + topInset)); pageViewport.offsetMin = new Vector2(0, navH);
            pageContent.localScale = new Vector3(k, k, 1);
        }

        void ChooseArea(int number)
        {
            var area = RoyalAreas.All[number - 1];
            bool restored = number < RoyalAreas.Current || number == RoyalAreas.Current && RoyalAreas.Complete;
            if (restored && area.Ready) View(area);
        }

        /// <summary>Shows a restored area full screen and replays its restoration, with the Home controls put away until close.</summary>
        public void View(RoyalAreaInfo area)
        {
            if (area == null || !area.Ready) return;
            home.ClosePanel();
            viewing = area;
            viewer.gameObject.SetActive(true); viewer.SetAsLastSibling();
            home.SceneViewOpen = true; if (homeLayout) homeLayout.HudAway = 1;
            scene.Hold = true;
            scene.Replay(area, Motion);
        }

        public void EndView()
        {
            if (viewing == null) return;
            EndViewQuietly();
            home.OpenPanel(List, home.tabButtons != null && home.tabButtons.Length > 0 ? home.tabButtons[0].gameObject : null);
        }
        void EndViewQuietly()
        {
            viewing = null; viewer.gameObject.SetActive(false); home.SceneViewOpen = false; if (homeLayout && building == null) homeLayout.HudAway = 0;
            scene.StopReplay(); scene.Hold = false; scene.ShowCurrent();
        }

        /// <summary>RoyalHomeScreen.RefreshProgress calls this: the Area card reads "New Area" while the next area waits to be
        /// unlocked (the reference's card after a chest), and shows its count otherwise.</summary>
        public void DecorateCard()
        {
            var area = RoyalAreas.CurrentArea;
            bool fresh = area != null && area.Ready && RoyalAreas.AwaitingEntry;
            if (fresh == cardFresh && cardDecorated) { if (fresh) home.areaLabel.text = "New Area"; return; }
            cardDecorated = true; cardFresh = fresh;
            var button = home.areaButton.transform;
            foreach (Transform child in button)
                if (child.name == "Imported 16593" || child.name == "Imported 17034" || child == home.areaFill.parent) child.gameObject.SetActive(!fresh);
            if (home.areaProgress) home.areaProgress.gameObject.SetActive(!fresh);
            var label = home.areaLabel.rectTransform;
            if (fresh) { savedLabel = new Vector4(label.anchorMin.x, label.anchorMin.y, label.anchorMax.x, label.anchorMax.y); label.anchorMin = new Vector2(.06f, .12f); label.anchorMax = new Vector2(.94f, .88f); home.areaLabel.text = "New Area"; home.areaLabel.fontSize = home.areaLabel.resizeTextMaxSize = 40; }
            else if (savedLabel != Vector4.zero) { label.anchorMin = new Vector2(savedLabel.x, savedLabel.y); label.anchorMax = new Vector2(savedLabel.z, savedLabel.w); home.areaLabel.fontSize = home.areaLabel.resizeTextMaxSize = 32; label.localScale = Vector3.one; }
        }
        bool cardDecorated, cardFresh; Vector4 savedLabel;
        void PulseNewArea()
        {
            // "New Area" stays still: no idle pulse on a control (the label's size and the card say it is new).
            if (!cardFresh || !home || !home.IsHomeVisible || home.dialog.activeSelf) return;
            if (home.areaLabel.transform.localScale != Vector3.one) home.areaLabel.transform.localScale = Vector3.one;
        }
    }
}
