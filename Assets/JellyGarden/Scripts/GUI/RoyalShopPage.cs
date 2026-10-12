using System.Collections.Generic;
using JellyGarden.Scripts;
using JellyGarden.Scripts.Juice;
using UnityEngine;
using UnityEngine.UI;
using static RoyalThrone.Presentation.RoyalCardKit;

namespace RoyalThrone.Presentation
{
    // The shop (2026-10-05, the user: real money buys stars and coins through Google Play; coins buy things in the game; every
    // value equal). A full page in the Settings page's style, over everything but the level start cover: the purse at the
    // top, then two tabs.
    // - Coins: the five coin packs, bought with real money through Google Play (RoyalStore).
    // - Stars: stars bought with coins (100 each), then the three star packs bought with real money.
    // Every price follows RoyalEconomy's one rate: a coin is a US cent, a star is 100 coins. It opens from Home's coin and
    // star counters, and from every "not enough coins" moment in a level (Continue, refill, booster and tool packs).
    public sealed class RoyalShopPage : MonoBehaviour
    {
        public enum Tab { Coins, Stars }
        static RoyalShopPage instance;
        public static bool IsOpen => instance && instance.page && instance.page.gameObject.activeSelf;
        public static Tab Shown => instance ? instance.tab : Tab.Coins;

        const float Column = 800, PhoneHeight = 1422, PanelWidth = 736, RowHeight = 118, Caption = 58, PanelTop = 448;
        RectTransform page, content, panel, rows;
        CanvasGroup group;
        Text coinBalance, starBalance, message;
        readonly List<RoyalCardShape[]> tabFaces = new List<RoyalCardShape[]>();
        readonly List<Text> tabLabels = new List<Text>();
        readonly List<(RoyalEconomy.StoreItem item, Text price)> moneyPrices = new List<(RoyalEconomy.StoreItem, Text)>();
        Tab tab; int built = -1; float shownAt, nextRefresh, localAt = -1; string localMessage;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { instance = null; }

        public static void Open(Tab tab)
        {
            if (!instance)
            {
                var go = new GameObject("Royal shop", typeof(RectTransform));
                DontDestroyOnLoad(go);
                instance = go.AddComponent<RoyalShopPage>();
                instance.Build();
            }
            instance.tab = tab; instance.built = -1;
            instance.page.gameObject.SetActive(true);
            instance.shownAt = Time.unscaledTime; instance.localMessage = null;
            RoyalStore.Connect();
            instance.Refresh();
        }

        public static void Close() { if (instance && instance.page) instance.page.gameObject.SetActive(false); }

        void Build()
        {
            var go = new GameObject("Royal shop page", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(CanvasGroup));
            page = (RectTransform)go.transform;
            page.SetParent(transform, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 90;   // over popups and pages; under the level start cover (100)
            group = go.GetComponent<CanvasGroup>();
            var backdrop = Inset(page, "Backdrop", 0, 0, C("2256c6"), C("0a2a78"));
            backdrop.raycastTarget = true;   // takes every tap the page does not
            Inset(page, "Veil", 0, 0, C("0d3596", .25f), C("061f5c", .55f));

            content = (RectTransform)new GameObject("Content", typeof(RectTransform)).transform;
            content.SetParent(page, false);
            content.anchorMin = content.anchorMax = content.pivot = new Vector2(.5f, 1);
            content.sizeDelta = new Vector2(Column, 0);

            // The header, as on the Settings page.
            Shape(content, "Header", 0, -116, 3000, 232, 0, C("2f6bd9"), C("1747ad"));
            Shape(content, "Header light", 0, -40, 3000, 80, 0, C("ffffff", .08f), C("ffffff", 0));
            Shape(content, "Header shadow", 0, -250, 3000, 30, 0, C("000a30", .45f), C("000a30", 0));
            Shape(content, "Header gold", 0, -233, 3000, 6, 0, C("ffe27a"), C("e3a02a"));
            Shape(content, "Header gold edge", 0, -236.5f, 3000, 1.2f, 0, C("7a3a12"), C("7a3a12"));
            Words(content, "Royal heading", "Shop", 0, -128, 520, 66, Ink.Display);
            var close = Control(content, "Close", Close);
            RedClose(close, 0);
            RoyalMenuSkin.Place(close, 322, -128, 100, 100);

            // The purse: coins and stars to spend.
            coinBalance = Purse(content, "Coins purse", -180, RoyalPieceSkin.Art("coin"));
            starBalance = Purse(content, "Stars purse", 180, RoyalPieceSkin.Art("star"));

            // Coins | Stars.
            var bar = Container(content, "Royal tabs");
            RoyalMenuSkin.Place(bar, 0, -378, PanelWidth, 92);
            Inset(bar, "Recess", 0, 30, C("06205e", .85f), C("0a2a78", .85f));
            string[] names = { "Coins", "Stars" };
            for (int i = 0; i < 2; i++)
            {
                var chosen = (Tab)i;
                var t = Control(bar, "Tab " + names[i], () => { tab = chosen; built = -1; Refresh(); });
                RoyalMenuSkin.Place(t, (i - .5f) * (PanelWidth * .5f - 4), 0, PanelWidth * .5f - 14, 76);
                var faces = new[]
                {
                    Inset(t, "Gold", 0, 24, C("ffe27a"), C("e6a22a")),
                    Inset(t, "Face", 4.5f, 20, C("3d8df6"), C("1b54cc")),
                };
                tabFaces.Add(faces);
                tabLabels.Add(Words(t, "Label", names[i], 0, 1, 300, 36, Ink.OnBlue));
            }

            panel = (RectTransform)Container(content, "Royal shop panel");
            rows = (RectTransform)Container(content, "Royal shop rows");
            message = Words(content, "Royal shop message", "", 0, 0, PanelWidth, 30, Ink.Light);
            var note = Words(content, "Royal payment note", "Payments are made securely through Google Play", 0, -1384, PanelWidth - 80, 24, Ink.Light);
            note.color = new Color(1, 1, 1, .7f);
            page.gameObject.SetActive(false);
            RoyalStore.Changed += OnStoreChanged;
        }

        void OnDestroy() { RoyalStore.Changed -= OnStoreChanged; if (instance == this) instance = null; }
        void OnStoreChanged() { if (IsOpen) Refresh(); }

        // A pill with an icon and a number.
        static Text Purse(Transform parent, string name, float x, Sprite icon)
        {
            var pill = Container(parent, name);
            RoyalMenuSkin.Place(pill, x, -292, 300, 66);
            Inset(pill, "Rim", 0, 33, C("86aaf2", .55f), C("4a76d6", .4f));
            Inset(pill, "Fill", 2.4f, 30.6f, C("12399a", .95f), C("0b2a78", .95f));
            Picture(pill, "Icon", icon, -112, 0, 62);
            var value = Words(pill, "Value", "0", 22, 1, 200, 36, Ink.Light);
            return value;
        }

        static Image Picture(Transform parent, string name, Sprite sprite, float x, float y, float size)
        {
            var r = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            r.SetParent(parent, false);
            RoyalMenuSkin.Place(r, x, y, size, size);
            var image = r.gameObject.AddComponent<Image>(); image.sprite = sprite; image.preserveAspect = true; image.raycastTarget = false;
            return image;
        }

        // A tappable child with a Button, a clear tap area and the HUD's press feedback.
        static Transform Control(Transform parent, string name, UnityEngine.Events.UnityAction action)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = Color.clear;
            var button = go.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            if (action != null) button.onClick.AddListener(action);
            button.onClick.AddListener(() => { if (SoundBase.Instance) SoundBase.Instance.PlaySound(SoundBase.Instance.click); });
            go.AddComponent<RoyalHudPress>().parts = new[] { (RectTransform)go.transform };
            return go.transform;
        }

        // The rows of the shown tab.
        void Lay()
        {
            built = (int)tab;
            // Detached first: Destroy waits for the end of the frame, and RoyalCardKit.Container reuses a child of the same
            // name, so new rows would otherwise be built into the old ones and vanish with them.
            foreach (var holder in new[] { rows, panel })
                for (int i = holder.childCount - 1; i >= 0; i--) { var old = holder.GetChild(i); old.SetParent(null, false); Destroy(old.gameObject); }
            moneyPrices.Clear();
            float y = -PanelTop - 8;
            var coin = RoyalPieceSkin.Art("coin"); var star = RoyalPieceSkin.Art("star");
            if (tab == Tab.Coins)
            {
                foreach (var item in RoyalEconomy.StoreItems)
                    if (item.Coins > 0) { MoneyRow(item, coin, y, item.Coins >= 2000 ? 1.18f : item.Coins >= 500 ? 1.08f : 1); y -= RowHeight; }
            }
            else
            {
                y = SectionCaption("With coins: " + RoyalEconomy.StarPriceCoins + " coins a star", y);
                foreach (int stars in RoyalEconomy.StarCoinPacks) { CoinRow(stars, star, coin, y); y -= RowHeight; }
                y = SectionCaption("With money", y);
                foreach (var item in RoyalEconomy.StoreItems)
                    if (item.Stars > 0) { MoneyRow(item, star, y, item.Stars >= 25 ? 1.18f : item.Stars >= 10 ? 1.08f : 1); y -= RowHeight; }
            }
            float height = -PanelTop - y + 8;
            RoyalMenuSkin.Place(panel, 0, -PanelTop - height * .5f, PanelWidth, height);
            var shade = Shape(panel, "Shadow", 0, -5, PanelWidth + 10, height + 10, 34, C("000a30", .35f), C("000a30", .35f));
            shade.softness = 14;
            Inset(panel, "Rim", 0, 30, C("86aaf2", .55f), C("4a76d6", .4f));
            Inset(panel, "Fill", 2.4f, 27.6f, C("12399a", .92f), C("0b2a78", .92f));
            message.rectTransform.anchoredPosition = new Vector2(0, y - 40);
        }

        float SectionCaption(string text, float y)
        {
            var label = Words(rows, "Section " + text, text, 0, y - Caption * .5f, PanelWidth - 60, 28, Ink.Light);   // unique: Words reuses a same-named child
            label.color = new Color(1, .93f, .7f);
            return y - Caption;
        }

        // Each row is named for how it is paid ("Row coins: 5 Stars", "Row money: 5 Stars"), so no two share a name.
        void Row(string paidWith, string title, string detail, Sprite icon, float y, float iconScale, out Transform button)
        {
            var row = Container(rows, "Row " + paidWith + ": " + title);
            RoyalMenuSkin.Place(row, 0, y - RowHeight * .5f, PanelWidth, RowHeight);
            Shape(row, "Line", 0, RowHeight * .5f, PanelWidth - 56, 2, 1, C("6f98ee", .25f), C("6f98ee", .25f));
            Picture(row, "Icon", icon, -296, 0, 76 * iconScale);
            var name = Words(row, "Title", title, -66, 12, 330, 36, Ink.Light); name.alignment = TextAnchor.MiddleLeft;
            var sub = Words(row, "Detail", detail, -66, -24, 330, 22, Ink.Light); sub.alignment = TextAnchor.MiddleLeft; sub.color = new Color(1, 1, 1, .7f);
            button = Control(row, "Buy", null);
        }

        void MoneyRow(RoyalEconomy.StoreItem item, Sprite icon, float y, float iconScale)
        {
            string detail = item.Coins > 0 ? "1 coin = 1 cent" : item.Stars + " x " + RoyalEconomy.StarPriceCoins + " coins of value";
            Row("money", item.Title, detail, icon, y, iconScale, out var buy);
            CleanButton(buy, null, 0, 0, 200, 84, Kind.Primary);
            RoyalMenuSkin.Place(buy, 240, 0, 200, 84);
            var price = Words(buy, "Price", RoyalStore.Price(item), 0, 1, 180, 34, CaptionInk(Kind.Primary));
            moneyPrices.Add((item, price));
            buy.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (RoyalStore.Busy) return;
                RoyalStore.Buy(item, (paid, text) => Refresh());
                Refresh();
            });
        }

        void CoinRow(int stars, Sprite icon, Sprite coin, float y)
        {
            int cost = RoyalEconomy.StarsCost(stars);
            Row("coins", stars + (stars == 1 ? " Star" : " Stars"), "For area tasks", icon, y, stars >= 10 ? 1.12f : 1, out var buy);
            CleanButton(buy, null, 0, 0, 200, 84, Kind.Secondary);
            RoyalMenuSkin.Place(buy, 240, 0, 200, 84);
            Picture(buy, "Coin", coin, -56, 0, 46);
            Words(buy, "Price", cost.ToString("N0"), 20, 1, 130, 34, CaptionInk(Kind.Secondary));
            buy.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (!RoyalStore.BuyStarsWithCoins(stars)) Show("Not enough coins (you have " + InitScript.Gems.ToString("N0") + "). Buy coins in the Coins tab");
                Refresh();
            });
        }

        // A message of the page's own (not enough coins); the store's own messages come from RoyalStore.Message.
        void Show(string text) { localMessage = text; localAt = Time.unscaledTime; }

        void Refresh()
        {
            if (!page || !page.gameObject.activeSelf) return;
            if (built != (int)tab) { Lay(); localMessage = null; }
            for (int i = 0; i < tabFaces.Count; i++)
            {
                bool on = (int)tab == i;
                tabFaces[i][0].gameObject.SetActive(on);
                tabFaces[i][1].from = on ? C("3d8df6") : C("1a3f99"); tabFaces[i][1].to = on ? C("1b54cc") : C("12307a");
                tabFaces[i][1].SetVerticesDirty();
                tabLabels[i].color = on ? Color.white : new Color(1, 1, 1, .65f);
            }
            if (coinBalance) coinBalance.text = InitScript.Gems.ToString("N0");
            var home = FindAnyObjectByType<RoyalHomeScreen>();
            if (starBalance) starBalance.text = (home ? home.Progress.available : RoyalAreas.Bought).ToString("N0");
            foreach (var (item, price) in moneyPrices) if (price) price.text = RoyalStore.Busy ? "..." : RoyalStore.Price(item);
            string status = RoyalStore.Status == RoyalStore.State.Connecting ? "Connecting to Google Play..."
                : RoyalStore.Status == RoyalStore.State.Unavailable ? "Google Play is unavailable. Coins can still buy stars" : "";
            // The newest message since the shop opened (the store's or the page's), else the store's state.
            string store = RoyalStore.MessageAt >= shownAt ? RoyalStore.Message : null;
            string local = localMessage != null && localAt >= shownAt ? localMessage : null;
            string text = store != null && (local == null || RoyalStore.MessageAt >= localAt) ? store : local;
            if (message) message.text = string.IsNullOrEmpty(text) ? status : text;
        }

        void Update()
        {
            if (!IsOpen) return;
            // Fit the 800 x 1422 column to the screen, as the other pages do.
            var r = page.rect;
            if (r.width > 0 && r.height > 0)
            {
                float s = Mathf.Min(r.width / Column, r.height / PhoneHeight);
                if (Mathf.Abs(content.localScale.x - s) > .0005f) content.localScale = new Vector3(s, s, 1);
            }
            float p = Mathf.Clamp01((Time.unscaledTime - shownAt) / .2f), e = 1 - (1 - p) * (1 - p) * (1 - p);
            group.alpha = e;
            content.anchoredPosition = new Vector2(0, 10 * (1 - e));
            if (Time.unscaledTime >= nextRefresh) { nextRefresh = Time.unscaledTime + .25f; Refresh(); }
            // Back closes the shop first.
            if (InputManager.Instance != null && InputManager.Instance.Gameplay.Cancel.WasReleasedThisFrame()) { Close(); RoyalBackButton.Consume(); }
        }
    }
}
