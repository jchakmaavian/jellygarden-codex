// The areas list: all 150 rooms as a vertical scroll of cards.
//
// Royal Match has no winding path map. Its Areas tab is a column of room cards
// on a dark slate ground — a cream "Area N" plate, a red name ribbon, the room
// itself behind a gold-rimmed red frame, and a progress bar across the bottom
// of the picture (docs/royal-match/04-areas-list.jpg). This is that screen, for
// a story that has 150 of them rather than forty.
//
// Two things here are not in the reference, and both follow from the length of
// this story:
//
//  - The list runs DOWNWARD from the last area to the first, as the reference
//    does, but it opens scrolled to the area the player is actually in. Royal
//    Match can put the newest card at the top because its locked areas are not
//    drawn at all; this one draws them, so opening at the top would be 145
//    padlocks before the first thing the player can press. Opening at the
//    frontier gives the reference's reading — what is next above, what is done
//    below — without hiding the road ahead.
//
//  - The artwork is loaded only for the cards near the viewport. There are 31
//    room pictures on disk at roughly half a megabyte each; resolving all 150
//    cards at once would decompress every one of them to open a menu.
//
// There is no per-area prefab and no per-area code path: a card is built from
// StoryDatabase and StoryProgress alone, so area 150 renders exactly as area 1
// does, and adding an area to the story adds a card here.
//
// Sorting order 350 is between home (300) and the area page (400): the list
// covers home, the area page covers the list, and AreaScreen's close X — which
// only deactivates itself — drops the player back onto whichever of the two
// they came from. The nav bar (500) stays above all three.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    public sealed class StoryAreasList : MonoBehaviour
    {
        public const string HostName = "StoryAreasList";

        private const int SortingOrder = 350;

        // The header strip, as a fraction of the canvas height: the reference is
        // 828x1472 and a fraction survives the Editor's 4:3 Game view, which a
        // pixel height does not.
        private const float HeaderHeightFrac = 156f / 1472f;
        private const float HeaderRimHeight = 10f;

        // A card's own proportions come out of its WIDTH, not the screen's
        // height: the list scrolls, so an entry is a fixed block of reference
        // points and the viewport shows however many of them fit. The reference
        // card runs x 74..755 of 828 and one entry is 754 tall; scaled to the
        // 1080-point canvas by 1080/828 = 1.304 that is a 888-point card in a
        // 983-point entry.
        private const float EntryHeight = 983f;
        private const float CardSideMargin = 96f;

        // Down the entry, as fractions of its height, measured off the Area 2
        // card in 04-areas-list.jpg. Stored as anchors from the BOTTOM, because
        // that is what a RectTransform wants; the reading order in the comment
        // is top-down, which is how they were measured.
        private const float PlateTop = 1f - 0.090f;      // cream "Area N" plate
        private const float PlateBottom = 1f - 0.174f;
        private const float BannerTop = 1f - 0.167f;     // red name ribbon
        private const float BannerBottom = 1f - 0.346f;
        private const float BodyTop = 1f - 0.196f;       // the framed picture
        private const float BodyBottom = 1f - 0.989f;
        private const float BarTop = 1f - 0.735f;        // progress across the picture
        private const float BarBottom = 1f - 0.841f;
        private const float BarLeftFrac = 0.20f;
        private const float BarRightFrac = 0.79f;

        // The plate is narrower than the card and the ribbon is wider: the ribbon
        // overhangs the frame on both sides, which is what makes it read as a
        // ribbon rather than as the card's top edge.
        private const float PlateWidthFrac = 0.42f;
        private const float BannerOverhang = 10f;

        // The frame around the picture, in reference points. The top inset is the
        // deep one because the ribbon lies over it.
        private const float FrameSide = 34f;
        private const float FrameTop = 149f;
        private const float FrameBottom = 130f;

        private const float SeparatorHeight = 5f;

        // The padlock over a locked room. Big enough to be the thing the eye lands
        // on: at half this it reads as a smudge on the picture.
        private const float LockSize = 230f;

        // How far outside the viewport a card still gets its picture. One entry
        // of slack in each direction, so a flick does not scroll onto a grey card.
        private const int ArtMargin = 1;

        private static StoryAreasList _instance;

        /// <summary>
        /// Where a card goes when it is pressed. Null is the area's own page,
        /// which is what the game does; a test swaps it to record the area id
        /// without building the screen behind this one. The same shape as
        /// StoryLevelPopup.Launcher and StoryLevelHud.Source.
        /// </summary>
        public static Action<int> Destination;

        /// <summary>One card. Everything a refresh or a scroll has to reach.</summary>
        public sealed class CardParts
        {
            public int AreaId;
            public RectTransform Root;
            public Button Button;
            public Text Heading;          // "Area 2"
            public Text Name;             // "Throne Room"
            public Image Plate;
            public Image Banner;
            public Image BannerFace;   // the red inside the ribbon's gold rim
            public Image Body;
            public Image Halo;            // the gold ring on the current area
            public RectTransform ArtHost; // masks the picture
            public Image Art;
            public AspectRatioFitter ArtFitter;
            public Image Scrim;           // over a locked room
            public Image Lock;
            public StoryUi.ProgressBarParts Progress;
            public bool Locked;
            public bool Current;
            public bool ArtLoaded;
        }

        private Canvas _canvas;
        private RectTransform _viewport;
        private RectTransform _content;
        private ScrollRect _scroll;
        private Text _title;
        private readonly List<CardParts> _cards = new List<CardParts>();

        public static StoryAreasList Instance
        {
            get { return _instance; }
        }

        public static bool IsOpen
        {
            get { return _instance != null && _instance.gameObject.activeSelf; }
        }

        /// <summary>How many cards the list holds — one per area in the story.</summary>
        public int CardCount
        {
            get { return _cards.Count; }
        }

        /// <summary>The list's own heading.</summary>
        public string TitleText
        {
            get { return _title == null ? null : _title.text; }
        }

        /// <summary>The scrolling column, for a test that wants to measure it.</summary>
        public RectTransform Content
        {
            get { return _content; }
        }

        private bool InRange(int i)
        {
            return i >= 0 && i < _cards.Count;
        }

        /// <summary>The card at a row, or null.</summary>
        public CardParts Card(int i)
        {
            return InRange(i) ? _cards[i] : null;
        }

        /// <summary>Which area the card at a row is for, or 0.</summary>
        public int AreaIdAt(int i)
        {
            return InRange(i) ? _cards[i].AreaId : 0;
        }

        /// <summary>Which row an area sits on, or -1. The list runs 150 down to 1.</summary>
        public int RowOf(int areaId)
        {
            for (int i = 0; i < _cards.Count; i++)
                if (_cards[i].AreaId == areaId) return i;
            return -1;
        }

        public string CardHeading(int i)
        {
            return InRange(i) && _cards[i].Heading != null ? _cards[i].Heading.text : null;
        }

        public string CardName(int i)
        {
            return InRange(i) && _cards[i].Name != null ? _cards[i].Name.text : null;
        }

        /// <summary>The card's repair count, e.g. "1/7".</summary>
        public string CardProgressText(int i)
        {
            if (!InRange(i) || _cards[i].Progress == null) return null;
            return _cards[i].Progress.Label == null ? null : _cards[i].Progress.Label.text;
        }

        public bool CardIsLocked(int i)
        {
            return InRange(i) && _cards[i].Locked;
        }

        public bool CardIsCurrent(int i)
        {
            return InRange(i) && _cards[i].Current;
        }

        /// <summary>Whether the card is showing the room rather than a flat fill.</summary>
        public bool CardHasArt(int i)
        {
            return InRange(i) && _cards[i].Art != null && _cards[i].Art.sprite != null;
        }

        public Button CardButton(int i)
        {
            return InRange(i) ? _cards[i].Button : null;
        }

        #region Open and close

        /// <summary>Opens the list, scrolled to the room the player is in.</summary>
        public static void Open()
        {
            Open(StoryHomeScreen.CurrentRoom);
        }

        /// <summary>Opens the list scrolled to a named area.</summary>
        public static void Open(int focusAreaId)
        {
            if (_instance == null)
            {
                var go = new GameObject(HostName);
                StoryObjects.Persist(go);
                _instance = go.AddComponent<StoryAreasList>();
                _instance.Build();
            }

            // The area page sorts ABOVE this one, so leaving it up would open the
            // list underneath a panel and look like the tab did nothing.
            AreaScreen.Close();

            _instance.gameObject.SetActive(true);
            _instance.Refresh();
            _instance.ScrollTo(focusAreaId);

            if (Application.isPlaying)
            {
                int row = _instance.RowOf(focusAreaId);
                Debug.Log(string.Format(
                    "[Story] areas list: {0} card(s), row {1} is area {2} '{3}' {4}, " +
                    "{5} locked, art on {6}.",
                    _instance.CardCount, row, focusAreaId,
                    _instance.CardName(row), _instance.CardProgressText(row),
                    _instance.LockedCount, _instance.ArtCount));
            }
        }

        public static void Close()
        {
            if (_instance != null) _instance.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        /// <summary>How many cards are padlocked right now.</summary>
        public int LockedCount
        {
            get
            {
                int locked = 0;
                for (int i = 0; i < _cards.Count; i++) if (_cards[i].Locked) locked++;
                return locked;
            }
        }

        /// <summary>How many cards are currently carrying a picture.</summary>
        public int ArtCount
        {
            get
            {
                int loaded = 0;
                for (int i = 0; i < _cards.Count; i++) if (CardHasArt(i)) loaded++;
                return loaded;
            }
        }

        #endregion

        #region Build

        private void Build()
        {
            _canvas = StoryUi.CreateOverlayCanvas("AreasCanvas", SortingOrder);
            _canvas.transform.SetParent(transform, false);

            Image ground = StoryUi.CreateImage("Ground", _canvas.transform, null, StoryUi.RoyalSlate);
            StoryUi.Stretch(ground.rectTransform);
            // The ground eats taps, so a press that misses a card does not fall
            // through to the map behind this canvas.
            ground.raycastTarget = true;

            BuildHeader();
            BuildScroll();
            BuildCards();
        }

        private void BuildHeader()
        {
            RectTransform header = StoryUi.CreateRect("Header", _canvas.transform);
            header.anchorMin = new Vector2(0f, 1f - HeaderHeightFrac);
            header.anchorMax = Vector2.one;
            header.offsetMin = Vector2.zero;
            header.offsetMax = Vector2.zero;

            Image strip = StoryUi.CreateImage("Strip", header, null, StoryUi.RoyalHeaderBlue);
            StoryUi.Stretch(strip.rectTransform);
            strip.raycastTarget = true;

            // The gold rule under the strip, which is what separates the header
            // from the slate rather than a drop shadow.
            Image rim = StoryUi.CreateImage("Rim", header, null, StoryUi.RoyalGoldDeep);
            rim.rectTransform.anchorMin = Vector2.zero;
            rim.rectTransform.anchorMax = new Vector2(1f, 0f);
            rim.rectTransform.pivot = new Vector2(0.5f, 1f);
            rim.rectTransform.sizeDelta = new Vector2(0f, HeaderRimHeight);
            rim.rectTransform.anchoredPosition = Vector2.zero;

            _title = StoryUi.CreateText("Title", header, "Areas", 72, StoryUi.RoyalTitle,
                TextAnchor.MiddleCenter);
            StoryUi.Stretch(_title.rectTransform);
            _title.fontStyle = FontStyle.Bold;
            var outline = _title.gameObject.AddComponent<Outline>();
            outline.effectColor = StoryUi.RoyalTealInk;
            outline.effectDistance = new Vector2(3f, -3f);
        }

        private void BuildScroll()
        {
            _viewport = StoryUi.CreateRect("Viewport", _canvas.transform);
            _viewport.anchorMin = new Vector2(0f, StoryUi.NavBarHeightFrac);
            _viewport.anchorMax = new Vector2(1f, 1f - HeaderHeightFrac);
            _viewport.offsetMin = Vector2.zero;
            _viewport.offsetMax = Vector2.zero;

            // A ScrollRect drags on its viewport's own graphic, so the viewport
            // needs one even though there is nothing to see through it.
            var catcher = _viewport.gameObject.AddComponent<Image>();
            catcher.color = new Color(0f, 0f, 0f, 0.01f);
            _viewport.gameObject.AddComponent<RectMask2D>();

            _scroll = _viewport.gameObject.AddComponent<ScrollRect>();
            _scroll.horizontal = false;
            _scroll.movementType = ScrollRect.MovementType.Elastic;
            _scroll.scrollSensitivity = 40f;

            _content = StoryUi.CreateRect("Cards", _viewport);
            _content.anchorMin = new Vector2(0f, 1f);
            _content.anchorMax = new Vector2(1f, 1f);
            _content.pivot = new Vector2(0.5f, 1f);
            _content.sizeDelta = Vector2.zero;

            _scroll.content = _content;
            _scroll.viewport = _viewport;
            _scroll.onValueChanged.AddListener(OnScrolled);
        }

        /// <summary>
        /// One card per area, newest first. Built once — a card's shape never
        /// changes, only its numbers and whether it is locked, and rebuilding 150
        /// of them on every repair would be the most expensive thing the shell
        /// does.
        /// </summary>
        private void BuildCards()
        {
            _cards.Clear();

            int count = StoryDatabase.AreaCount;
            _content.sizeDelta = new Vector2(0f, count * EntryHeight);

            for (int row = 0; row < count; row++)
            {
                // Row 0 is the LAST area: the reference reads downward into the
                // past, and the player opens on their own frontier.
                int areaId = count - row;
                _cards.Add(BuildCard(row, areaId));
            }
        }

        private CardParts BuildCard(int row, int areaId)
        {
            var card = new CardParts { AreaId = areaId };

            RectTransform entry = StoryUi.CreateRect("Area" + areaId, _content);
            entry.anchorMin = new Vector2(0f, 1f);
            entry.anchorMax = new Vector2(1f, 1f);
            entry.pivot = new Vector2(0.5f, 1f);
            entry.sizeDelta = new Vector2(0f, EntryHeight);
            entry.anchoredPosition = new Vector2(0f, -row * EntryHeight);
            card.Root = entry;

            // The hairline between two cards. It belongs to the entry above it
            // and runs the card's own width, which is what makes the column read
            // as a list rather than as cards floating on a wall.
            Image separator = StoryUi.CreateImage("Separator", entry, null,
                StoryUi.RoyalSlateLine);
            RectTransform line = separator.rectTransform;
            line.anchorMin = new Vector2(0f, 0f);
            line.anchorMax = new Vector2(1f, 0f);
            line.pivot = new Vector2(0.5f, 0.5f);
            line.offsetMin = new Vector2(CardSideMargin, -SeparatorHeight * 0.5f);
            line.offsetMax = new Vector2(-CardSideMargin, SeparatorHeight * 0.5f);
            // The last entry has nothing under it to be separated from.
            separator.gameObject.SetActive(row < StoryDatabase.AreaCount - 1);

            // The whole entry is the button. A card is a picture with a name on
            // it, and the player expects to press the picture.
            card.Button = entry.gameObject.AddComponent<Button>();
            card.Button.transition = Selectable.Transition.None;
            int captured = row;
            card.Button.onClick.AddListener(() => OnCard(captured));

            // The gold ring that says "you are here". Behind the body, one step
            // proud of it on every side, so it reads as a halo and not as a
            // second frame.
            card.Halo = StoryUi.CreateImage("Halo", entry, StoryUi.RoundedSprite(48),
                StoryUi.RoyalGold);
            card.Halo.type = Image.Type.Sliced;
            Frame(card.Halo.rectTransform, BodyBottom, BodyTop,
                CardSideMargin - 12f, -12f);
            card.Halo.gameObject.SetActive(false);

            card.Body = StoryUi.CreateImage("Body", entry, StoryUi.RoundedSprite(44),
                StoryUi.RoyalCardRed);
            card.Body.type = Image.Type.Sliced;
            Frame(card.Body.rectTransform, BodyBottom, BodyTop, CardSideMargin, 0f);

            // The gold rim is a second rounded rect inset inside the red one,
            // which is how every rim in this kit is drawn.
            Image rim = StoryUi.CreateImage("Rim", card.Body.rectTransform,
                StoryUi.RoundedSprite(40), StoryUi.RoyalGoldDeep);
            rim.type = Image.Type.Sliced;
            rim.rectTransform.anchorMin = Vector2.zero;
            rim.rectTransform.anchorMax = Vector2.one;
            rim.rectTransform.offsetMin = new Vector2(FrameSide - 8f, FrameBottom - 8f);
            rim.rectTransform.offsetMax = new Vector2(-(FrameSide - 8f), -(FrameTop - 8f));

            card.ArtHost = StoryUi.CreateRect("Art", card.Body.rectTransform);
            card.ArtHost.anchorMin = Vector2.zero;
            card.ArtHost.anchorMax = Vector2.one;
            card.ArtHost.offsetMin = new Vector2(FrameSide, FrameBottom);
            card.ArtHost.offsetMax = new Vector2(-FrameSide, -FrameTop);
            card.ArtHost.gameObject.AddComponent<RectMask2D>();

            // The picture is 4:3 and this window is 1.64:1, so it is enveloped
            // and cropped rather than letterboxed — the same trade AreaSceneView
            // makes on the home screen, and for the same reason: these rooms are
            // composed around their centre.
            card.Art = StoryUi.CreateImage("Room", card.ArtHost, null,
                StoryUi.RoyalSlateLine);
            StoryUi.Stretch(card.Art.rectTransform);
            card.ArtFitter = card.Art.gameObject.AddComponent<AspectRatioFitter>();
            card.ArtFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            card.ArtFitter.enabled = false;

            card.Scrim = StoryUi.CreateImage("Scrim", card.ArtHost, null,
                new Color(0.11f, 0.19f, 0.29f, 0.62f));
            StoryUi.Stretch(card.Scrim.rectTransform);
            card.Scrim.gameObject.SetActive(false);

            card.Lock = StoryUi.CreateImage("Lock", card.ArtHost, StoryUi.LockSprite,
                Color.white);
            card.Lock.preserveAspect = true;
            card.Lock.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            card.Lock.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            card.Lock.rectTransform.sizeDelta = new Vector2(LockSize, LockSize);
            card.Lock.rectTransform.anchoredPosition = Vector2.zero;
            card.Lock.gameObject.SetActive(false);

            card.Progress = StoryUi.ProgressBar("Progress", card.Body.rectTransform);
            card.Progress.Root.anchorMin = new Vector2(BarLeftFrac, BarBottomInBody());
            card.Progress.Root.anchorMax = new Vector2(BarRightFrac, BarTopInBody());
            card.Progress.Root.offsetMin = Vector2.zero;
            card.Progress.Root.offsetMax = Vector2.zero;

            // The ribbon last, so it lies over the frame's top edge, and wider
            // than the card, so it overhangs it.
            card.Banner = StoryUi.CreateImage("Banner", entry, StoryUi.RoundedSprite(24),
                StoryUi.RoyalBanner);
            card.Banner.type = Image.Type.Sliced;
            Frame(card.Banner.rectTransform, BannerBottom, BannerTop,
                CardSideMargin - BannerOverhang, 0f);

            Image bannerRim = StoryUi.CreateImage("Rim", card.Banner.rectTransform,
                StoryUi.RoundedSprite(20), StoryUi.RoyalGoldDeep);
            bannerRim.type = Image.Type.Sliced;
            StoryUi.Stretch(bannerRim.rectTransform, 7f);

            card.BannerFace = StoryUi.CreateImage("Face", bannerRim.rectTransform,
                StoryUi.RoundedSprite(18), StoryUi.RoyalBanner);
            card.BannerFace.type = Image.Type.Sliced;
            StoryUi.Stretch(card.BannerFace.rectTransform, 5f);

            // 150 names, and "The Hall of Broken Mirrors" is one of them. Best
            // fit across the ribbon's whole width — a best-fit Text wraps before
            // it shrinks, so a narrow rect gives two big lines, not one small one.
            card.Name = StoryUi.CreateText("Name", card.Banner.rectTransform, string.Empty,
                62, StoryUi.RoyalTitle, TextAnchor.MiddleCenter);
            StoryUi.Stretch(card.Name.rectTransform, 26f);
            card.Name.fontStyle = FontStyle.Bold;

            // One line, shrunk to fit. Best-fit on its own wraps first, and the
            // ribbon these sit on is one line tall.
            StoryUi.FitOneLine(card.Name, 20, 62);
            var nameOutline = card.Name.gameObject.AddComponent<Outline>();
            nameOutline.effectColor = StoryUi.RoyalBannerDeep;
            nameOutline.effectDistance = new Vector2(2.5f, -2.5f);

            // The plate is drawn AFTER the ribbon but tucks behind it, because
            // only its top half shows: it is the tab of a card, not a label on it.
            card.Plate = StoryUi.CreateImage("Plate", entry, StoryUi.RoundedSprite(20),
                StoryUi.RoyalCream);
            card.Plate.type = Image.Type.Sliced;
            RectTransform plate = card.Plate.rectTransform;
            plate.anchorMin = new Vector2(0.5f - PlateWidthFrac * 0.5f, PlateBottom);
            plate.anchorMax = new Vector2(0.5f + PlateWidthFrac * 0.5f, PlateTop);
            plate.offsetMin = Vector2.zero;
            plate.offsetMax = Vector2.zero;
            plate.SetSiblingIndex(card.Banner.transform.GetSiblingIndex());

            card.Heading = StoryUi.CreateText("Heading", plate, "Area " + areaId, 40,
                StoryUi.RoyalNavy, TextAnchor.UpperCenter);
            StoryUi.Stretch(card.Heading.rectTransform, 8f);
            card.Heading.fontStyle = FontStyle.Bold;
            card.Heading.resizeTextForBestFit = true;
            card.Heading.resizeTextMinSize = 16;
            card.Heading.resizeTextMaxSize = 40;

            return card;
        }

        /// <summary>
        /// Anchors a rect across the card's column: full width less a margin,
        /// between two fractions of the entry's height.
        /// </summary>
        private static void Frame(RectTransform rect, float bottom, float top,
            float sideMargin, float verticalGrow)
        {
            rect.anchorMin = new Vector2(0f, bottom);
            rect.anchorMax = new Vector2(1f, top);
            rect.offsetMin = new Vector2(sideMargin, verticalGrow);
            rect.offsetMax = new Vector2(-sideMargin, -verticalGrow);
        }

        // The progress bar is anchored inside the card BODY, not the entry, so
        // its fractions have to be re-expressed against the body's height.
        private static float BarBottomInBody()
        {
            return (BarBottom - BodyBottom) / (BodyTop - BodyBottom);
        }

        private static float BarTopInBody()
        {
            return (BarTop - BodyBottom) / (BodyTop - BodyBottom);
        }

        #endregion

        #region Refresh

        /// <summary>
        /// Brings every card up to date with progress: its repair count, whether
        /// it is locked, and whether it is the room the player is in. Cheap — no
        /// card is rebuilt and no picture is loaded here.
        /// </summary>
        public void Refresh()
        {
            int current = StoryHomeScreen.CurrentRoom;

            for (int i = 0; i < _cards.Count; i++)
            {
                CardParts card = _cards[i];
                StoryArea area = StoryDatabase.GetArea(card.AreaId);
                if (area == null) continue;

                card.Name.text = area.Name;
                StoryUi.SetProgress(card.Progress,
                    StoryProgress.TasksCompletedIn(card.AreaId), area.Tasks.Count);

                bool locked = !StoryProgress.IsAreaUnlocked(card.AreaId);
                card.Locked = locked;
                card.Scrim.gameObject.SetActive(locked);
                card.Lock.gameObject.SetActive(locked);

                // Locked reads as locked rather than as missing: the room is still
                // drawn, behind a scrim and a padlock, and the card is greyed
                // rather than hidden. onClick still fires on a greyed button, so
                // OnCard enforces the rule itself.
                card.Button.interactable = !locked;
                card.Body.color = locked ? StoryUi.RoyalGrey : StoryUi.RoyalCardRed;
                card.Banner.color = locked ? StoryUi.RoyalGrey : StoryUi.RoyalBanner;
                // The ribbon's face is what is actually seen — the rim around it
                // covers only its outer few points, so greying the outer image
                // alone leaves a locked area wearing a bright red name plate.
                card.BannerFace.color = locked ? StoryUi.RoyalGrey : StoryUi.RoyalBanner;
                card.Plate.color = locked ? StoryUi.RoyalCreamDeep : StoryUi.RoyalCream;

                card.Current = !locked && card.AreaId == current;
                card.Halo.gameObject.SetActive(card.Current);
            }

            RefreshArt();
        }

        #endregion

        #region Scrolling and artwork

        /// <summary>
        /// Puts an area at the top of the viewport. The list is long enough that
        /// this is the difference between a usable screen and 145 padlocks.
        /// </summary>
        public void ScrollTo(int areaId)
        {
            int row = RowOf(areaId);
            if (row < 0) row = 0;

            float span = Mathf.Max(0f, _content.sizeDelta.y - ViewportHeight);
            float y = Mathf.Clamp(row * EntryHeight, 0f, span);
            _content.anchoredPosition = new Vector2(_content.anchoredPosition.x, y);

            RefreshArt();
        }

        /// <summary>Which row sits at the top of the viewport right now.</summary>
        public int TopRow
        {
            get { return Mathf.FloorToInt(_content.anchoredPosition.y / EntryHeight); }
        }

        private float ViewportHeight
        {
            get
            {
                // Before the first layout pass the viewport has no rect, and a
                // zero height would load the artwork for a single card. The
                // canvas reference height is the honest guess.
                float height = _viewport == null ? 0f : _viewport.rect.height;
                return height > 1f ? height : 1920f * (1f - HeaderHeightFrac - StoryUi.NavBarHeightFrac);
            }
        }

        private void OnScrolled(Vector2 _)
        {
            RefreshArt();
        }

        /// <summary>
        /// Gives the picture to the cards near the viewport and takes it back from
        /// the rest. Thirty-one room paintings at half a megabyte each is not a
        /// thing to decompress in order to open a menu, and a card off screen is
        /// not a thing anyone is looking at.
        /// </summary>
        private void RefreshArt()
        {
            if (_cards.Count == 0) return;

            float top = _content.anchoredPosition.y;
            int first = Mathf.FloorToInt(top / EntryHeight) - ArtMargin;
            int last = Mathf.CeilToInt((top + ViewportHeight) / EntryHeight) + ArtMargin;

            for (int i = 0; i < _cards.Count; i++)
            {
                bool wanted = i >= first && i <= last;
                if (wanted == _cards[i].ArtLoaded) continue;

                if (wanted) LoadArt(_cards[i]);
                else ClearArt(_cards[i]);
            }
        }

        private static void LoadArt(CardParts card)
        {
            card.ArtLoaded = true;

            // The room as it stands: the state for the repairs already paid for,
            // falling back to the restored picture and then to the arc's backdrop,
            // which is StoryArt's own resolution order and not a second art path.
            int done = StoryProgress.TasksCompletedIn(card.AreaId);
            Sprite sprite = StoryArt.AreaState(card.AreaId, done);
            if (sprite == null) sprite = StoryArt.AreaRestored(card.AreaId);
            if (sprite == null) return;

            card.Art.sprite = sprite;
            card.Art.color = Color.white;

            Rect rect = sprite.rect;
            if (rect.height <= 0f) return;
            card.ArtFitter.aspectRatio = rect.width / rect.height;
            card.ArtFitter.enabled = true;
        }

        private static void ClearArt(CardParts card)
        {
            card.ArtLoaded = false;
            card.Art.sprite = null;
            card.Art.color = StoryUi.RoyalSlateLine;
            card.ArtFitter.enabled = false;
        }

        #endregion

        #region Actions

        /// <summary>
        /// A card: the area's own page, which sorts above this list, so backing
        /// out of it drops the player straight back here.
        ///
        /// The guard is not the button's interactable flag — onClick fires on a
        /// greyed button, and an editor test invokes it directly.
        /// </summary>
        private void OnCard(int row)
        {
            if (!InRange(row)) return;
            CardParts card = _cards[row];

            if (!StoryProgress.IsAreaUnlocked(card.AreaId))
            {
                StoryToast.Show("Area " + card.AreaId + " is still locked.");
                return;
            }

            Debug.Log("[Story] areas list: area " + card.AreaId + " pressed.");

            if (Destination != null) Destination(card.AreaId);
            else AreaScreen.Open(card.AreaId);
        }

        #endregion

        #region Install

        /// <summary>
        /// Points the nav bar's Areas tab at this list. Called once per scene by
        /// StorySceneHooks, alongside everything else the story installs — the
        /// tab itself does not know this screen exists, it only knows the seam.
        /// </summary>
        public static void Install()
        {
            StoryNavBar.AreasDestination = () => Open();
        }

        #endregion
    }
}
