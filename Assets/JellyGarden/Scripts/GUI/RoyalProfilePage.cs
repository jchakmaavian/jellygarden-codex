using System.Collections.Generic;
using JellyGarden.Scripts;
using JellyGarden.Scripts.Juice;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static RoyalThrone.Presentation.RoyalCardKit;
using Art = RoyalThrone.Presentation.RoyalProfileArt;

namespace RoyalThrone.Presentation
{
    // The Profile page (2026-09-30), after the reference's profile, in Royal Throne's page style (the Settings page).
    // From the top:
    // - the royal-blue header with "Profile" and the red close;
    // - the player's card: the framed avatar with its green edit badge, the name in its style, the team ("No Team": teams
    //   are not open), the month play began, and the current level on a pennant hanging from the card's top edge;
    // - "General Stats" and a cream panel of six real counts: levels completed, first-try wins, power-ups made, areas
    //   completed, total score and best score (RoyalProfile);
    // - Edit Profile, which opens the Edit card (RoyalProfileEditor).
    // RoyalHomeScreen owns it: the portrait button opens its "profile" panel, and the page shows while that panel is open, over
    // Home's navigation bar, as the Settings page does. Back closes the Edit card first, then the page. Home's portrait shows
    // the chosen avatar (King Vanguard's live portrait when he is the choice).
    public sealed class RoyalProfilePage : MonoBehaviour
    {
        public const string PanelName = "profile";
        const float Column = 800, PhoneHeight = 1422, Width = 736;
        const float CardTop = 282, CardHeight = 330, StatsTitle = 672, StatsTop = 712, StatsHeight = 470, ButtonHeight = 116;
        public static readonly string[] StatNames = { "Levels Completed", "First-Try Wins", "Power-ups Made", "Areas Completed", "Total Score", "Best Score" };

        RoyalHomeScreen home;
        RectTransform page, content, avatarTile, portraitOverlay;
        CanvasGroup group;
        Text nameText, sinceText, levelText;
        readonly Text[] statValues = new Text[6];
        RoyalProfileEditor editor;
        float shownAt;
        bool dirty = true, portraitDirty = true;
        string portraitShown;

        public bool IsOpen => page && page.gameObject.activeSelf;
        public bool Editing => editor && editor.IsOpen;
        public RoyalProfileEditor Editor => editor;
        public RectTransform Root => page;

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
                if (!h.GetComponent<RoyalProfilePage>()) h.gameObject.AddComponent<RoyalProfilePage>().home = h;
        }

        void Awake() { if (!home) home = GetComponent<RoyalHomeScreen>(); }

        void OnEnable() { RoyalProfile.Changed += OnChanged; }

        void OnDisable()
        {
            RoyalProfile.Changed -= OnChanged;
            if (page) page.gameObject.SetActive(false);
        }

        void OnDestroy() { if (page) Destroy(page.gameObject); }

        void OnChanged() { dirty = true; portraitDirty = true; }

        void LateUpdate()
        {
            SyncHomePortrait();
            bool open = home && home.IsHomeVisible && home.dialog && home.dialog.activeSelf && home.OpenPanelName == PanelName;
            // Back closes the Edit card first: Home's Update has closed the panel this frame for the Back press; the card
            // closes instead and the panel opens again before anything is drawn.
            if (!open && Editing && IsOpen && RoyalBackButton.ConsumedThisFrame && home && home.IsHomeVisible)
            {
                editor.Close();
                home.OpenPanel(PanelName);
                open = home.OpenPanelName == PanelName;
            }
            if (open && !page) Build();
            if (!page) return;
            if (page.gameObject.activeSelf != open)
            {
                page.gameObject.SetActive(open);
                if (open) { shownAt = Time.unscaledTime; dirty = true; }
                else if (editor) editor.Close();
            }
            if (!open) return;
            FitColumn();
            if (dirty) Refresh();
            // The Settings page's entrance: a fade over 0.2 s while the column settles 10 units down into place.
            float p = Mathf.Clamp01((Time.unscaledTime - shownAt) / .2f), e = 1 - (1 - p) * (1 - p) * (1 - p);
            group.alpha = e;
            content.anchoredPosition = new Vector2(0, 10 * (1 - e));
        }

        void FitColumn()
        {
            var r = page.rect;
            if (r.width <= 0 || r.height <= 0) return;
            float s = Mathf.Min(r.width / Column, r.height / PhoneHeight);
            if (Mathf.Abs(content.localScale.x - s) > .0005f) content.localScale = new Vector3(s, s, 1);
        }

        /// <summary>The scale the page's 800-unit column is drawn at (the Edit card uses it too).</summary>
        public float ColumnScale => content ? content.localScale.x : 1;

        // ---- Filling -----------------------------------------------------------------------------------------------------

        public void Refresh()
        {
            dirty = false;
            var avatar = RoyalProfileArt.Find(RoyalProfile.Avatar);
            Art.SetAvatar(avatarTile, avatar);
            Art.SetFrame(avatarTile, RoyalProfile.Frame);
            nameText.text = RoyalProfile.Name;
            Art.Fit(nameText, NameWidth, NameSize, Ink.Display, NameLeft);
            Art.StyleName(nameText, RoyalProfile.NameStyle, nameText.fontSize);
            sinceText.text = RoyalProfile.Since;
            var progress = home ? home.Progress : default(RoyalHomeProgress);
            levelText.text = home ? progress.nextLevel.ToString() : "1";
            RoyalProfile.Scores(home ? home.levelNumbers : null, out long total, out int best);
            int[] counts = { home ? progress.completed : 0, RoyalProfile.FirstTryWins, RoyalProfile.PowerupsMade, RoyalProfile.AreasCompleted };
            for (int i = 0; i < 4; i++) statValues[i].text = counts[i].ToString("N0");
            statValues[4].text = total.ToString("N0");
            statValues[5].text = best.ToString("N0");
        }

        const float NameLeft = -84, NameWidth = 280, NameSize = 54, Avatar = 210;

        // ---- Building ----------------------------------------------------------------------------------------------------

        void Build()
        {
            var canvas = home.GetComponentInParent<Canvas>();
            if (!canvas) return;
            var go = new GameObject("Royal profile page", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(CanvasGroup));
            page = (RectTransform)go.transform;
            page.SetParent(canvas.rootCanvas.transform, false);
            Stretch(page, 0);
            var own = go.GetComponent<Canvas>();
            own.overrideSorting = true; own.sortingOrder = 60;   // over Home's navigation bar (20), as the Settings page
            group = go.GetComponent<CanvasGroup>();

            // The Settings page's backdrop: a royal-blue fall with the castle hall faintly behind it; it takes every tap.
            var backdrop = Inset(page, "Backdrop", 0, 0, C("2256c6"), C("0a2a78"));
            backdrop.raycastTarget = true;
            var hall = RoyalMenuSkin.Art(page, "Castle hall", "home-castle", 0, 0, Column, PhoneHeight);
            Stretch(hall.rectTransform, 0);
            hall.preserveAspect = false; hall.color = C("9dbcff", .14f);
            var fitter = hall.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            if (hall.sprite) fitter.aspectRatio = hall.sprite.rect.width / hall.sprite.rect.height;
            Inset(page, "Hall veil", 0, 0, C("0d3596", .25f), C("061f5c", .55f));

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
            Words(content, "Royal heading", "Profile", 0, -128, 520, 66, Ink.Display);
            var controls = new List<RoyalCardControl>();
            var close = Control(content, "Close", () => { if (home) home.ClosePanel(); });
            RedClose(close, 0);
            RoyalMenuSkin.Place(close, 322, -128, 100, 100);
            controls.Add(Feedback(close, false));

            BuildCard(controls);
            BuildStats();

            var edit = Control(content, "Royal edit profile", OpenEditor);
            CleanButton(edit, "Edit Profile", 0, -StatsTop - StatsHeight - 30 - ButtonHeight * .5f, Width, ButtonHeight, Kind.Secondary, 44);
            controls.Add(Feedback(edit, false));

            var motion = go.AddComponent<RoyalCardMotion>();
            motion.entrance = false;
            motion.controls = controls.ToArray();
            editor = RoyalProfileEditor.Build(page, this);
            page.gameObject.SetActive(false);
        }

        // The player's card: the Settings page's panel (a deeper, see-through blue with a light rim).
        void BuildCard(List<RoyalCardControl> controls)
        {
            var card = Container(content, "Royal player card");
            RoyalMenuSkin.Place(card, 0, -CardTop - CardHeight * .5f, Width, CardHeight);
            var shade = Shape(card, "Shadow", 0, -5, Width + 10, CardHeight + 10, 34, C("000a30", .35f), C("000a30", .35f));
            shade.softness = 14;
            Inset(card, "Rim", 0, 30, C("86aaf2", .55f), C("4a76d6", .4f));
            Inset(card, "Fill", 2.4f, 27.6f, C("12399a", .92f), C("0b2a78", .92f));
            Strip(card, "Fill light", true, 2.4f, CardHeight * .45f, 27.6f, C("5b8ae6", .18f), C("5b8ae6", 0));

            // The avatar: tapping it (or its badge) opens the Edit card.
            float tileX = -Width * .5f + 34 + Avatar * .5f;
            avatarTile = Art.Tile(card, "Royal avatar", tileX, 0, Avatar, RoyalProfileArt.Find(RoyalProfile.Avatar), RoyalProfile.Frame);
            var hit = avatarTile.gameObject.AddComponent<Image>(); hit.color = Color.clear;
            var tap = avatarTile.gameObject.AddComponent<Button>(); tap.transition = Selectable.Transition.None; tap.onClick.AddListener(OpenEditor);
            tap.onClick.AddListener(Click);
            avatarTile.gameObject.AddComponent<RoyalHudPress>().parts = new[] { avatarTile };
            EditBadge(card, tileX + Avatar * .5f - 20, -Avatar * .5f + 20);

            float top = CardHeight * .5f;
            nameText = Art.Left(card, "Royal name", RoyalProfile.Name, NameLeft, top - 76, NameWidth, NameSize, Ink.Display);
            // The rows under the name start clear of the avatar and its badge.
            Art.Picture(card, "Royal team icon", Art.HomeArt(17106), -52, top - 158, 56);
            Art.Left(card, "Royal team", "No Team", -16, top - 158, 220, 34, Ink.Light);
            Art.Calendar(card, -52, top - 236, 44);
            sinceText = Art.Left(card, "Royal since", RoyalProfile.Since, -16, top - 236, 220, 34, Ink.Light);
            levelText = Art.Pennant(card, Width * .5f - 30 - 64, top + 8, 128, 132, home ? home.Progress.nextLevel : 1);
        }

        // The green badge with a pencil on the avatar's lower-right corner.
        void EditBadge(Transform parent, float x, float y)
        {
            var go = new GameObject("Royal edit badge", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = Color.clear;
            var button = go.GetComponent<Button>(); button.transition = Selectable.Transition.None;
            button.onClick.AddListener(OpenEditor); button.onClick.AddListener(Click);
            const float d = 72, r = 18;
            RoyalMenuSkin.Place(go.transform, x, y, d + 16, d + 16);
            var skin = Container(go.transform, "Royal skin");
            RoyalMenuSkin.Place(skin, 0, 0, d, d);
            var shadow = Shape(skin, "Shadow", 0, -3, d + 4, d + 4, r + 2, C("001a08", .5f), C("001a08", .5f));
            shadow.softness = 6;
            Inset(skin, "Edge", 0, r, C("8c4a14"), C("6a3410"));
            Inset(skin, "Gold", 1.1f, r - 1.1f, C("ffe27a"), C("e6a22a"));
            Inset(skin, "Dark line", 5, r - 5, C("0c5424"), C("093f1b"));
            Inset(skin, "Green", 6.3f, r - 6.3f, C("3ec255"), C("148a38"));
            Strip(skin, "Gloss", true, 8, 18, 9, C("ffffff", .24f), C("ffffff", .03f));
            Art.Pencil(skin, 40, C("fffdf3"), C("f2f7ea"));
            go.AddComponent<RoyalHudPress>().parts = new[] { (RectTransform)skin };
        }

        // "General Stats" over the cream panel of six counts, three to a row: the icon, the name, the value on a tan pill.
        void BuildStats()
        {
            var heading = Words(content, "Royal stats heading", "General Stats", 0, -StatsTitle, 520, 38, Ink.Display);
            HeadingLines(content, heading, -StatsTitle - 1.1f, 120, 350, C("fdf08a"), C("fffbe0", .35f));
            var panel = RoyalCardKit.Panel(content, "Royal stats panel", 0, -StatsTop - StatsHeight * .5f, Width, StatsHeight, 30);
            Sprite[] icons =
            {
                Art.HomeArt(16525),                                        // the castle: levels
                Art.HomeArt(22238),                                        // the trophy: first-try wins
                RoyalGameplayArt.For(ItemsTypes.HORIZONTAL_STRIPPED, 0),   // the board's Rocket: power-ups made
                Art.HomeArt(16927),                                        // the Areas book
                RoyalPieceSkin.Art("star"),                                // the star: score
                RoyalGameplayArt.For(ItemsTypes.NONE, 2),                  // the board's crown: best score
            };
            float top = StatsHeight * .5f;
            for (int i = 0; i < 6; i++)
            {
                float cx = (i % 3 - 1) * 238, cellTop = top - 22 - (i / 3) * 214;
                Art.Picture(panel, "Stat icon " + i, icons[i], cx, cellTop - 50, 88);
                var label = Words(panel, "Stat label " + i, StatNames[i], cx, cellTop - 112, 226, 24, Ink.Navy);
                if (label.preferredWidth > 226) { float size = Mathf.Floor(24 * 226 / label.preferredWidth); Paint(label, Ink.Navy, size); RoyalMenuSkin.Place(label.transform, cx, cellTop - 112 + size * CapDrop, 226, size * 1.4f); }
                var pill = Container(panel, "Stat pill " + i);
                RoyalMenuSkin.Place(pill, cx, cellTop - 168, 206, 60);
                Inset(pill, "Edge", 0, 18, C("c9ab76"), C("b89660"));
                Inset(pill, "Fill", 1.6f, 16.4f, C("e6d0a2"), C("dcc394"));
                Strip(pill, "Shade", true, 1.6f, 16, 16.4f, C("7a5a2e", .22f), C("7a5a2e", 0));
                statValues[i] = Words(pill, "Value", "0", 0, 0, 196, 34, Ink.Value);
            }
        }

        void OpenEditor() { if (editor) editor.Open(); }

        static void Click() { if (SoundBase.Instance) SoundBase.Instance.PlaySound(SoundBase.Instance.click); }

        // A tappable child with a Button, a clear Image as its tap area and the HUD's press (the Settings page's controls).
        static Transform Control(Transform parent, string name, UnityEngine.Events.UnityAction action)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = Color.clear;
            var button = go.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(action);
            button.onClick.AddListener(Click);
            return go.transform;
        }

        // ---- Home's portrait ---------------------------------------------------------------------------------------------

        // The chosen avatar is drawn over the portrait's place (inside Home's own frame), and Home's King portrait is set aside:
        // since 2026-10-04 the King is no longer one of the avatars.
        void SyncHomePortrait()
        {
            if (!home || home.resourceButtons == null || home.resourceButtons.Length == 0 || !home.resourceButtons[0]) return;
            string id = RoyalProfile.Avatar;
            // Home's layout switches off every graphic on its resource buttons when it builds, which can come after the
            // portrait was drawn (now that an avatar is always drawn here): a portrait found switched off is drawn again.
            var shownArt = portraitOverlay ? portraitOverlay.Find("Portrait/Art") : null;
            bool switchedOff = shownArt && portraitOverlay.gameObject.activeSelf && !shownArt.GetComponent<Image>().enabled;
            if (!portraitDirty && id == portraitShown && !switchedOff) return;
            portraitDirty = false; portraitShown = id;
            var button = home.resourceButtons[0].transform;
            var king = button.Find("King portrait") as RectTransform;
            if (king && king.gameObject.activeSelf) king.gameObject.SetActive(false);
            var place = king ? king : (RectTransform)button;
            if (!portraitOverlay)
            {
                portraitOverlay = Art.Tile(place.parent, "Royal avatar portrait", 0, 0, 100, RoyalProfileArt.Find(id), 0);
                // Home draws its own frame round the portrait: only the masked portrait is kept, filling the place.
                foreach (string part in new[] { "Shadow", "Frame", "Sheen" }) { var t = portraitOverlay.Find(part); if (t) t.gameObject.SetActive(false); }
                Stretch((RectTransform)portraitOverlay.Find("Portrait"), 0);
            }
            portraitOverlay.gameObject.SetActive(true);
            portraitOverlay.anchorMin = place.anchorMin; portraitOverlay.anchorMax = place.anchorMax; portraitOverlay.pivot = place.pivot;
            portraitOverlay.anchoredPosition = place.anchoredPosition; portraitOverlay.sizeDelta = place.sizeDelta;
            portraitOverlay.SetSiblingIndex(place.GetSiblingIndex() + 1);
            Art.SetAvatar(portraitOverlay, RoyalProfileArt.Find(id));
            foreach (var g in portraitOverlay.GetComponentsInChildren<Graphic>()) g.enabled = true;   // the parts in use only
        }
    }
}
