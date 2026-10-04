using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using JellyGarden.Scripts.Juice;
using static RoyalThrone.Presentation.RoyalCardKit;

namespace RoyalThrone.Presentation
{
    // The Leaderboard page, after the user's reference (2026-09-29), in the level-end cards' style (the So Close card: royal
    // blue, polished gold, warm cream). From the top: a royal-blue header with the title over a gold line; the Friends /
    // Players / Teams tabs; Friends List / Add Friends under Friends; one cream card with King Vanguard holding up a phone and
    // the message; the Connect button under the card, apart from it; Home's own navigation bar below, its Leaderboard tab
    // selected.
    // RoyalHomeScreen owns the page: its Leaderboard tab opens the "leagues" panel, and the page shows while that panel is
    // open; Back, Home and the other tabs close it. The page sits between Home (sorting 10) and the navigation bar (20), so the
    // bar stays live over it, and under the popups (30).
    // No leaderboard, friend or account data exists yet (the game is offline, RoyalOfflineMode), so every tab shows the
    // connect card. Connect raises ConnectRequested, the one place a social service attaches; with none attached it opens the
    // legacy Facebook login where that build is enabled, and otherwise says it is coming soon.
    // Laid out on an 800-unit column in the space between the safe area's top and the navigation bar. A screen wider than a
    // phone (a tablet) keeps the phone's column and centres it.
    public sealed class RoyalLeaderboardPage : MonoBehaviour
    {
        public const string PanelName = "leagues";
        const float Column = 800, MinHeight = 1210, Width = 736;
        const float HeaderHeight = 180, TabsTop = 206, TabsHeight = 104, SubTop = 332, SubHeight = 84, CardTop = 444;
        const float CardGap = 30, ButtonHeight = 116, BottomGap = 34, MinCard = 560, MaxCard = 980, MessageBlock = 176;
        static readonly string[] Tabs = { "Friends", "Players", "Teams" };
        static readonly string[] Views = { "Friends List", "Add Friends" };
        // The message's second line for Friends List, Add Friends, Players and Teams.
        static readonly string[] Reasons = { "to save your progress!", "to find your friends!", "to see the top players!", "to see the top teams!" };

        /// <summary>Connect was pressed. A social service handles it here; while none is attached the page says it is coming soon.</summary>
        public static event System.Action ConnectRequested;

        RoyalHomeScreen home;
        RectTransform page, content, subBar, card, connect, illustration;
        CanvasGroup group;
        Text message, connectCaption;
        readonly List<GameObject[]> tabFaces = new List<GameObject[]>(), viewFaces = new List<GameObject[]>();
        float shownAt, laidOutFor = -1, comingSoonUntil;
        int tab, view, shownTab = -1, shownView = -1;
        public int Tab => tab;
        public int View => view;
        public bool IsOpen => page && page.gameObject.activeSelf;

        // ---- Attaching to Home -----------------------------------------------------------------------------------------

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            SceneManager.sceneLoaded -= Loaded;   // Play Mode without a domain reload keeps static subscriptions
            SceneManager.sceneLoaded += Loaded;
            Attach();
        }

        static void Loaded(Scene scene, LoadSceneMode mode) => Attach();

        static void Attach()
        {
            foreach (var h in FindObjectsByType<RoyalHomeScreen>(FindObjectsInactive.Include))
                if (!h.GetComponent<RoyalLeaderboardPage>()) h.gameObject.AddComponent<RoyalLeaderboardPage>().home = h;
        }

        void Awake() { if (!home) home = GetComponent<RoyalHomeScreen>(); }

        void OnDisable() { if (page) page.gameObject.SetActive(false); }

        void OnDestroy() { if (page) Destroy(page.gameObject); }

        void LateUpdate()
        {
            bool open = home && home.IsHomeVisible && home.dialog && home.dialog.activeSelf && home.OpenPanelName == PanelName;
            if (open && !page) Build();
            if (!page) return;
            if (page.gameObject.activeSelf != open)
            {
                page.gameObject.SetActive(open);
                if (open) { shownAt = Time.unscaledTime; tab = view = 0; comingSoonUntil = 0; }
            }
            if (!open) return;
            float offset = Fit();
            if (tab != shownTab || view != shownView) ShowState();
            if (connectCaption) connectCaption.text = Time.unscaledTime < comingSoonUntil ? "Coming soon" : "Connect";
            // The Settings page's entrance: a fade over 0.2 s while the column settles 10 units down into place.
            float p = Mathf.Clamp01((Time.unscaledTime - shownAt) / .2f), e = 1 - (1 - p) * (1 - p) * (1 - p);
            group.alpha = e;
            content.anchoredPosition = new Vector2(0, offset + 10 * (1 - e));
        }

        // Scales the column into the space between the safe area's top and the navigation bar, and lays the card and the
        // button out for that height. Returns the column's offset from the page's top edge.
        float Fit()
        {
            var r = page.rect;
            if (r.width <= 0 || r.height <= 0) return 0;
            var safe = Screen.safeArea;
            float top = page.InverseTransformPoint(new Vector3(0, safe.yMax, 0)).y, bottom = page.InverseTransformPoint(new Vector3(0, safe.yMin, 0)).y;
            if (home.navigation)
            {
                var corners = new Vector3[4];
                home.navigation.GetWorldCorners(corners);
                bottom = Mathf.Max(bottom, page.InverseTransformPoint(corners[1]).y);
            }
            float room = Mathf.Max(1, top - bottom);
            float s = Mathf.Min(r.width / Column, room / MinHeight);
            if (Mathf.Abs(content.localScale.x - s) > .0005f) content.localScale = new Vector3(s, s, 1);
            float height = room / s;
            if (Mathf.Abs(height - laidOutFor) > .5f) { laidOutFor = height; Layout(height); }
            return top - r.yMax;
        }

        // Rows from the column's top: the header, the tabs, the sub-tabs (Friends only), the card, a gap, Connect.
        void Layout(float height)
        {
            float cardTop = tab == 0 ? CardTop : SubTop;
            float cardHeight = Mathf.Clamp(height - cardTop - CardGap - ButtonHeight - BottomGap, MinCard, MaxCard);
            RoyalMenuSkin.Place(card, 0, -cardTop - cardHeight * .5f, Width, cardHeight);
            float buttonY = -cardTop - cardHeight - CardGap - ButtonHeight * .5f;
            connect.anchoredPosition = new Vector2(0, buttonY);
            LayoutCard(cardHeight);
        }

        // ---- Building --------------------------------------------------------------------------------------------------

        void Build()
        {
            var canvas = home.GetComponentInParent<Canvas>();
            if (!canvas) return;
            var go = new GameObject("Royal leaderboard page", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(CanvasGroup));
            page = (RectTransform)go.transform;
            page.SetParent(canvas.rootCanvas.transform, false);
            Stretch(page, 0);
            var own = go.GetComponent<Canvas>();
            own.overrideSorting = true; own.sortingOrder = 15;   // over Home (10), under its navigation bar (20) and the popups (30)
            group = go.GetComponent<CanvasGroup>();

            // The backdrop, as on the Settings page: a royal-blue fall; it takes every
            // tap the page does not, so nothing on Home underneath reacts.
            var backdrop = Inset(page, "Backdrop", 0, 0, C("2256c6"), C("0a2a78"));
            backdrop.raycastTarget = true;
            // The palace picture that showed faintly here was removed from the project (2026-10-04).
            Inset(page, "Hall veil", 0, 0, C("0d3596", .25f), C("061f5c", .55f));

            // The column: anchored at the top edge, 0 tall, so every row is placed by its distance from the top.
            content = (RectTransform)new GameObject("Content", typeof(RectTransform)).transform;
            content.SetParent(page, false);
            content.anchorMin = content.anchorMax = content.pivot = new Vector2(.5f, 1);
            content.sizeDelta = new Vector2(Column, 0);

            // The header: the Settings page's band (wider than any screen), its gold line and the soft shadow under it.
            float h = HeaderHeight;
            Shape(content, "Header", 0, -h * .5f, 3000, h, 0, C("2f6bd9"), C("1747ad"));
            Shape(content, "Header light", 0, -40, 3000, 80, 0, C("ffffff", .08f), C("ffffff", 0));
            Shape(content, "Header shadow", 0, -h - 17, 3000, 30, 0, C("000a30", .45f), C("000a30", 0));
            Shape(content, "Header gold", 0, -h - 1, 3000, 6, 0, C("ffe27a"), C("e3a02a"));
            Shape(content, "Header gold edge", 0, -h - 4.5f, 3000, 1.2f, 0, C("7a3a12"), C("7a3a12"));
            Words(content, "Royal heading", "Leaderboard", 0, -h * .5f - 2, 700, 72, Ink.Display);

            var controls = new List<RoyalCardControl>();
            // Friends | Players | Teams: one segmented control of three equal tabs in a recessed blue bar.
            var tabBar = Bar(content, "Royal tabs", -TabsTop - TabsHeight * .5f, TabsHeight);
            float tabW = (Width - 16 - 12) / 3, tabH = TabsHeight - 16;
            for (int i = 0; i < Tabs.Length; i++)
            {
                int index = i;
                var t = Segment(tabBar, "Tab " + Tabs[i], Tabs[i], (i - 1) * (tabW + 6), 0, tabW, tabH, 36, false, () => { tab = index; view = 0; });
                tabFaces.Add(Faces(t)); controls.Add(Feedback(t, false));
            }
            // Friends List | Add Friends: the smaller control under Friends, its choice in royal purple.
            subBar = (RectTransform)Bar(content, "Royal friend tabs", -SubTop - SubHeight * .5f, SubHeight);
            float viewW = (Width - 14 - 6) / 2, viewH = SubHeight - 14;
            for (int i = 0; i < Views.Length; i++)
            {
                int index = i;
                var t = Segment(subBar, "View " + Views[i], Views[i], (i - .5f) * (viewW + 6), 0, viewW, viewH, 32, true, () => view = index);
                viewFaces.Add(Faces(t)); controls.Add(Feedback(t, false));
            }

            // The cream card (the level-end cards' cream panel, with a larger radius) and what it shows.
            card = (RectTransform)RoyalCardKit.Panel(content, "Royal leaderboard card", 0, -CardTop - 300, Width, 600, 40);
            illustration = (RectTransform)Container(card, "Royal illustration");
            BuildIllustration(illustration);
            Shape(card, "Royal separator", 0, 0, Width - 88, 2, 1, C("c9ae7c", .75f), C("c9ae7c", .75f));
            Shape(card, "Royal separator light", 0, 0, Width - 88, 1.4f, .7f, C("ffffff", .8f), C("ffffff", .8f));
            message = Words(card, "Royal message", "", 0, 0, Width - 60, 40, Ink.Navy);
            message.lineSpacing = 1.05f;

            // Connect: the full-width blue button with the gold rim, apart from the card; its gloss crosses it now and then.
            var button = new GameObject("Connect", typeof(RectTransform), typeof(Image), typeof(Button));
            button.transform.SetParent(content, false);
            button.GetComponent<Button>().onClick.AddListener(OnConnect);
            connect = (RectTransform)CleanButton(button.transform, "Connect", 0, -900, Width, ButtonHeight, Kind.Secondary, 46);
            var face = connect.Find("Royal skin/Green").GetComponent<RoyalCardShape>();   // brighter than the secondary blue
            face.from = C("3d8df6"); face.to = C("1b54cc");
            connectCaption = connect.Find("Royal caption").GetComponent<Text>();
            controls.Add(Feedback(connect, true));

            var motion = go.AddComponent<RoyalCardMotion>();
            motion.entrance = false;
            motion.controls = controls.ToArray();
            motion.gloss = connect.Find("Royal skin/Sweep").GetComponent<RoyalSweepGraphic>();
            foreach (var b in go.GetComponentsInChildren<Button>(true))
                b.onClick.AddListener(() => { if (JellyGarden.Scripts.SoundBase.Instance) JellyGarden.Scripts.SoundBase.Instance.PlaySound(JellyGarden.Scripts.SoundBase.Instance.click); });
            page.gameObject.SetActive(false);
        }

        void ShowState()
        {
            bool friends = tab == 0;
            for (int i = 0; i < tabFaces.Count; i++) { tabFaces[i][0].SetActive(i == tab); tabFaces[i][1].SetActive(i != tab); }
            for (int i = 0; i < viewFaces.Count; i++) { viewFaces[i][0].SetActive(i == view); viewFaces[i][1].SetActive(i != view); }
            subBar.gameObject.SetActive(friends);
            message.text = "Connect with Facebook\n" + Reasons[friends ? view : tab + 1];
            if (shownTab < 0 || (shownTab == 0) != friends) laidOutFor = -1;   // the card takes the sub-tabs' row on Players and Teams
            shownTab = tab; shownView = view;
            if (laidOutFor < 0) Fit();
        }

        void OnConnect()
        {
            if (ConnectRequested != null) { ConnectRequested(); return; }
#if FACEBOOK
            if (RoyalThrone.RoyalOfflineMode.ServicesEnabled && JellyGarden.Scripts.Integrations.FacebookManager.THIS)
            { JellyGarden.Scripts.Integrations.FacebookManager.THIS.CallFBLogin(); return; }
#endif
            comingSoonUntil = Time.unscaledTime + 1.6f;
        }

        // ---- The card --------------------------------------------------------------------------------------------------

        // The illustration fills the card above the message block; the King stands centred on a soft floor shadow.
        void LayoutCard(float cardHeight)
        {
            float top = cardHeight * .5f, lineY = -top + MessageBlock;
            RoyalMenuSkin.Place(card.Find("Royal separator"), 0, lineY, Width - 88, 2);
            RoyalMenuSkin.Place(card.Find("Royal separator light"), 0, lineY - 1.7f, Width - 88, 1.4f);
            RoyalMenuSkin.Place(message.transform, 0, (-top + lineY) * .5f + 40 * CapDrop, Width - 60, 40 * 1.4f);
            float space = top - 22 - lineY - 18;
            RoyalMenuSkin.Place(illustration, 0, lineY + 18 + space * .5f, Width - 40, space);
            float kingHeight = Mathf.Min(space - 12, 560);
            var king = illustration.Find("Royal king") as RectTransform;
            float kingWidth = kingHeight * kingAspect;
            RoyalMenuSkin.Place(king, 0, -space * .5f + 6 + kingHeight * .5f, kingWidth, kingHeight);
            RoyalMenuSkin.Place(illustration.Find("Royal floor shadow"), 0, -space * .5f + 6 + kingHeight * RoyalThrone.Characters.Activities.NativeKingDrillUiSlot.GroundFraction, kingWidth * .82f, kingWidth * .13f);
            float chest = -space * .5f + 6 + kingHeight * .55f;
            RoyalMenuSkin.Place(illustration.Find("Royal glow"), 0, chest, kingHeight * .95f, kingHeight * .95f);
            var rays = illustration.Find("Royal rays");
            ((RectTransform)rays).anchoredPosition = new Vector2(0, chest);
            rays.localScale = Vector3.one * Mathf.Min(kingHeight * .66f, Width * .5f - 30) / 100;
        }

        float kingAspect = RoyalThrone.Characters.Activities.NativeKingDrillUiSlot.Aspect;

        void BuildIllustration(Transform parent)
        {
            // Soft gold rays behind the King (a still glory, no motion), fading outwards; built 100 long, sized by LayoutCard.
            var rays = Container(parent, "Royal rays");
            RoyalMenuSkin.Place(rays, 0, 0, 0, 0);
            for (int i = 0; i < 16; i++)
            {
                var ray = Shape(rays, "Ray " + i, 0, 0, 13, 100, 6.5f, C("ffd24a", 0), C("ffd24a", .28f));
                ray.softness = 5;
                ray.rectTransform.pivot = new Vector2(.5f, 0); ray.rectTransform.anchoredPosition = Vector2.zero;
                ray.rectTransform.localRotation = Quaternion.Euler(0, 0, i * 22.5f + 11.25f);
            }
            var glow = Shape(parent, "Royal glow", 0, 0, 400, 400, 200, C("ffffff", .6f), C("fff2d0", .45f));
            glow.softness = 150;
            var floor = Shape(parent, "Royal floor shadow", 0, 0, 240, 36, 18, C("7a5a2e", .3f), C("7a5a2e", .3f));
            floor.softness = 22;
            var kingGo = new GameObject("Royal king", typeof(RectTransform), typeof(Image));
            kingGo.transform.SetParent(parent, false);
            var king = kingGo.GetComponent<Image>();
            king.raycastTarget = false;
            // Preserve this existing full-body composition with the independently imported native KingDrill rig.
            RoyalThrone.Characters.Activities.NativeKingDrillUiSlot.Mount(king);
        }

        void BuildPhone(Transform phone)
        {
            var r = (RectTransform)phone;
            r.gameObject.AddComponent<RoyalLeaderboardPhoneFit>();
            // Built at 230 x 130 (the art's pixels), then scaled with the King by RoyalLeaderboardPhoneFit.
            const float w = 230, h = 130, radius = 20, bezel = 11;
            r.sizeDelta = new Vector2(w, h);
            var shadow = Shape(phone, "Shadow", 0, -7, w + 8, h + 8, radius + 4, C("0a1640", .4f), C("0a1640", .4f));
            shadow.softness = 18;
            Inset(phone, "Edge", 0, radius, C("8c4a14"), C("6a3410"));
            Inset(phone, "Gold", 1.4f, radius - 1.4f, C("ffe27a"), C("e6a22a"));
            Inset(phone, "Gold light", 1.4f, radius - 1.4f, C("fffbe0", .6f), C("fffbe0", 0));
            Inset(phone, "Bezel", 5.5f, radius - 5.5f, C("10265e"), C("081a46"));
            var screen = Container(phone, "Screen");
            Stretch((RectTransform)screen, bezel);
            float sw = w - 2 * bezel, sh = h - 2 * bezel, sr = radius - bezel;
            Inset(screen, "Glass", 0, sr, C("4aa0ff"), C("1552c9"));
            Strip(screen, "Glass light", true, 0, sh * .48f, sr, C("ffffff", .24f), C("ffffff", 0));
            var crown = new GameObject("Crown", typeof(RectTransform), typeof(Image));
            crown.transform.SetParent(screen, false);
            var crownImage = crown.GetComponent<Image>();
            crownImage.sprite = RoyalPieceSkin.CampaignArt("crown"); crownImage.preserveAspect = true; crownImage.raycastTarget = false;
            RoyalMenuSkin.Place(crown.transform, -sw * .17f, -1, sh * .78f, sh * .78f);
            // A signal: three rising bars, cream on the blue glass.
            var signal = Container(screen, "Signal");
            RoyalMenuSkin.Place(signal, sw * .24f, 0, 60, sh * .62f);
            for (int i = 0; i < 3; i++)
            {
                float bar = sh * (.26f + .18f * i);
                Shape(signal, "Bar " + i, (i - 1) * 17, -sh * .31f + bar * .5f, 11, bar, 3, C("fffaf0"), C("f1e3c4"));
            }
        }

        // ---- Tabs ------------------------------------------------------------------------------------------------------

        // A recessed bar for a segmented control: a soft shadow, a light rim and the deep blue fill.
        static Transform Bar(Transform parent, string name, float y, float height)
        {
            var bar = Container(parent, name);
            RoyalMenuSkin.Place(bar, 0, y, Width, height);
            float r = height * .31f;
            var shadow = Shape(bar, "Shadow", 0, -4, Width + 8, height + 8, r + 4, C("000a30", .35f), C("000a30", .35f));
            shadow.softness = 12;
            Inset(bar, "Rim", 0, r, C("6a96ea", .6f), C("2a58c0", .5f));
            Inset(bar, "Fill", 2.2f, r - 2.2f, C("0a2a78"), C("08225f"));
            Strip(bar, "Fill shade", true, 2.2f, height * .3f, r - 2.2f, C("000a30", .3f), C("000a30", 0));
            return bar;
        }

        // One tab: a Button whose "Royal skin" holds both looks, "On" (the choice: bright blue in the gold rim, or royal purple
        // in a lilac rim) and "Off" (the others: a darker blue, a quieter label).
        Transform Segment(Transform bar, string name, string caption, float x, float y, float w, float h, float size, bool purple, UnityEngine.Events.UnityAction choose)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(bar, false);
            go.GetComponent<Image>().color = Color.clear;
            var button = go.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(choose);
            RoyalMenuSkin.Place(go.transform, x, y, w, h);
            var skin = Container(go.transform, "Royal skin");
            float r = h * .3f;

            var on = Container(skin, "On");
            if (purple)
            {
                Inset(on, "Edge", 0, r, C("3a0d6a"), C("2a0850"));
                Inset(on, "Rim", 1.1f, r - 1.1f, C("dcbcff"), C("a978f0"));
                Inset(on, "Dark line", 3.6f, r - 3.6f, C("3a0f78"), C("2c0a5e"));
                Inset(on, "Face", 4.8f, r - 4.8f, C("a352f6"), C("6c22c8"));
            }
            else
            {
                Inset(on, "Edge", 0, r, C("8c4a14"), C("6a3410"));
                Inset(on, "Gold", 1.1f, r - 1.1f, C("ffe27a"), C("e6a22a"));
                Inset(on, "Gold light", 1.1f, r - 1.1f, C("fffbe0", .6f), C("fffbe0", 0));
                Inset(on, "Dark line", 4.6f, r - 4.6f, C("0a2466"), C("071a4c"));
                Inset(on, "Face", 5.9f, r - 5.9f, C("3d8df6"), C("1b54cc"));
            }
            float face = purple ? 4.8f : 5.9f, lens = (h - 2 * face) * .42f;
            var gloss = Strip(on, "Gloss", true, face + 2, lens, lens * .5f, C("ffffff", .2f), C("ffffff", .03f));
            gloss.rectTransform.sizeDelta = new Vector2(-2 * (face + (r - face) * .45f), lens);
            var onLabel = Words(on, "Label", caption, 0, 1, w - 20, size, Ink.OnBlue);
            if (purple) Style(onLabel, C("ffffff"), C("f6eeff"), Color.clear, 0, C("3a0a6e"), size * .034f, C("24054a", .5f), new Vector2(0, -Mathf.Max(1.2f, size * .045f)), size * .02f);

            var off = Container(skin, "Off");
            Inset(off, "Face", 0, r, C("15398a"), C("0d2a6c"));
            var light = Inset(off, "Light", 0, r, C("7aa2f0", .35f), C("7aa2f0", 0));
            light.band = 1.2f;
            var offLabel = Words(off, "Label", caption, 0, 1, w - 20, size - 2, Ink.Light);
            Style(offLabel, C("d9e4fb"), C("bccbee"), Color.clear, 0, Color.clear, 0, C("041a52", .45f), new Vector2(0, -1.4f), size * .01f);
            return go.transform;
        }

        static GameObject[] Faces(Transform segment) => new[] { segment.Find("Royal skin/On").gameObject, segment.Find("Royal skin/Off").gameObject };
    }

    // Keeps the phone, built at 230 x 130 (the art's own pixels, 988 tall), in proportion to the King's rect.
    public sealed class RoyalLeaderboardPhoneFit : MonoBehaviour
    {
        void LateUpdate()
        {
            var king = transform.parent as RectTransform;
            if (!king) return;
            float s = king.rect.height / 988;
            if (Mathf.Abs(transform.localScale.x - s) > .0005f) transform.localScale = new Vector3(s, s, 1);
        }
    }
}
