using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static RoyalThrone.Presentation.RoyalCardKit;

namespace RoyalThrone.Presentation
{
    // The Team and Collection tabs as pages (UI polish, 2026-09-30), in the Leaderboard page's structure: the royal-blue header
    // with the title over a gold line, one cream card with the feature's own art (Home's tab icon, large, over soft gold rays)
    // and a short message saying the feature is not open yet, then one clear action under the card: PLAY the next level,
    // through Home's own OpenNextLevel. Home's navigation bar stays live below with the tab selected. RoyalHomeScreen owns the
    // pages: its tabs open the "team" and "collection" panels, and a page shows while its panel is open; Back, Home and the
    // other tabs close it. It sorts like the Leaderboard page: over Home (10), under the bar (20) and the popups (30).
    // Laid out on an 800-unit column between the safe area's top and the bar; a tablet keeps the phone's column.
    public sealed class RoyalFeaturePage : MonoBehaviour
    {
        const float Column = 800, MinHeight = 1210, Width = 736, HeaderHeight = 180, CardTop = 214;
        const float CardGap = 30, ButtonHeight = 116, BottomGap = 34, MinCard = 560, MaxCard = 1000, MessageBlock = 190;

        sealed class Feature { public string panel, title, line1, line2; public int icon; }
        // The icons are Home's own tab art (ReferenceHomeLayout: 17106 Team, 16492 Collection).
        static readonly Feature[] Features =
        {
            new Feature { panel = "team", title = "Team", line1 = "Teams are not open yet", line2 = "Win levels while you wait!", icon = 17106 },
            new Feature { panel = "collection", title = "Collection", line1 = "Collections are not open yet", line2 = "Win levels while you wait!", icon = 16492 },
        };

        RoyalHomeScreen home;
        RectTransform page, content, card, art, rays, action;
        CanvasGroup group;
        Text heading, message, actionCaption;
        Feature shown;
        float shownAt, laidOutFor = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            SceneManager.sceneLoaded -= Loaded;
            SceneManager.sceneLoaded += Loaded;
            Attach();
        }

        static void Loaded(Scene scene, LoadSceneMode mode) => Attach();

        static void Attach()
        {
            foreach (var h in FindObjectsByType<RoyalHomeScreen>(FindObjectsInactive.Include))
                if (!h.GetComponent<RoyalFeaturePage>()) h.gameObject.AddComponent<RoyalFeaturePage>().home = h;
        }

        public bool IsOpen => page && page.gameObject.activeSelf;
        public string Showing => IsOpen && shown != null ? shown.panel : null;

        void Awake() { if (!home) home = GetComponent<RoyalHomeScreen>(); }
        void OnDisable() { if (page) page.gameObject.SetActive(false); }
        void OnDestroy() { if (page) Destroy(page.gameObject); }

        void LateUpdate()
        {
            Feature want = null;
            if (home && home.IsHomeVisible && home.dialog && home.dialog.activeSelf)
                foreach (var f in Features) if (home.OpenPanelName == f.panel) want = f;
            if (want != null && !page) Build();
            if (!page) return;
            if (page.gameObject.activeSelf != (want != null)) { page.gameObject.SetActive(want != null); if (want != null) shownAt = Time.unscaledTime; }
            if (want == null) { shown = null; return; }
            if (want != shown) { shown = want; Fill(want); shownAt = Time.unscaledTime; }
            float offset = Fit();
            if (actionCaption) actionCaption.text = "PLAY LEVEL " + home.Progress.nextLevel;
            // The Settings and Leaderboard pages' entrance: a 0.2 s fade while the column settles 10 units down.
            float p = Mathf.Clamp01((Time.unscaledTime - shownAt) / .2f), e = 1 - (1 - p) * (1 - p) * (1 - p);
            group.alpha = e;
            content.anchoredPosition = new Vector2(0, offset + 10 * (1 - e));
        }

        void Fill(Feature f)
        {
            heading.text = f.title;
            message.text = f.line1 + "\n" + f.line2;
            var sprite = Resources.Load<Sprite>("ReferenceGameplay/Home/" + f.icon);
            var image = art.GetComponent<Image>();
            image.sprite = sprite; image.enabled = sprite;   // an Image without a sprite draws a white box
            laidOutFor = -1;
        }

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
            float room = Mathf.Max(1, top - bottom), s = Mathf.Min(r.width / Column, room / MinHeight);
            if (Mathf.Abs(content.localScale.x - s) > .0005f) content.localScale = new Vector3(s, s, 1);
            float height = room / s;
            if (Mathf.Abs(height - laidOutFor) > .5f) { laidOutFor = height; Layout(height); }
            return top - r.yMax;
        }

        void Layout(float height)
        {
            float cardHeight = Mathf.Clamp(height - CardTop - CardGap - ButtonHeight - BottomGap, MinCard, MaxCard);
            RoyalMenuSkin.Place(card, 0, -CardTop - cardHeight * .5f, Width, cardHeight);
            action.anchoredPosition = new Vector2(0, -CardTop - cardHeight - CardGap - ButtonHeight * .5f);
            float cardTop = cardHeight * .5f, lineY = -cardTop + MessageBlock;
            RoyalMenuSkin.Place(card.Find("Royal separator"), 0, lineY, Width - 88, 2);
            RoyalMenuSkin.Place(card.Find("Royal separator light"), 0, lineY - 1.7f, Width - 88, 1.4f);
            RoyalMenuSkin.Place(message.transform, 0, (-cardTop + lineY) * .5f + 40 * CapDrop, Width - 60, 40 * 1.4f);
            float space = cardTop - 22 - lineY - 18, size = Mathf.Min(space - 40, 420), centre = lineY + 18 + space * .5f;
            RoyalMenuSkin.Place(art, 0, centre, size, size);
            RoyalMenuSkin.Place(card.Find("Royal glow"), 0, centre, size * 1.25f, size * 1.25f);
            rays.anchoredPosition = new Vector2(0, centre);
            rays.localScale = Vector3.one * Mathf.Min(size * .72f, Width * .5f - 30) / 100;
        }

        void Build()
        {
            var canvas = home.GetComponentInParent<Canvas>();
            if (!canvas) return;
            var go = new GameObject("Royal feature page", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(CanvasGroup));
            page = (RectTransform)go.transform;
            page.SetParent(canvas.rootCanvas.transform, false);
            Stretch(page, 0);
            var own = go.GetComponent<Canvas>();
            own.overrideSorting = true; own.sortingOrder = 15;
            group = go.GetComponent<CanvasGroup>();

            // The backdrop, as on the Settings and Leaderboard pages.
            var backdrop = Inset(page, "Backdrop", 0, 0, C("2256c6"), C("0a2a78"));
            backdrop.raycastTarget = true;
            // The palace picture that showed faintly here was removed from the project (2026-10-04).
            Inset(page, "Hall veil", 0, 0, C("0d3596", .25f), C("061f5c", .55f));

            content = (RectTransform)new GameObject("Content", typeof(RectTransform)).transform;
            content.SetParent(page, false);
            content.anchorMin = content.anchorMax = content.pivot = new Vector2(.5f, 1);
            content.sizeDelta = new Vector2(Column, 0);

            float h = HeaderHeight;
            Shape(content, "Header", 0, -h * .5f, 3000, h, 0, C("2f6bd9"), C("1747ad"));
            Shape(content, "Header light", 0, -40, 3000, 80, 0, C("ffffff", .08f), C("ffffff", 0));
            Shape(content, "Header shadow", 0, -h - 17, 3000, 30, 0, C("000a30", .45f), C("000a30", 0));
            Shape(content, "Header gold", 0, -h - 1, 3000, 6, 0, C("ffe27a"), C("e3a02a"));
            Shape(content, "Header gold edge", 0, -h - 4.5f, 3000, 1.2f, 0, C("7a3a12"), C("7a3a12"));
            heading = Words(content, "Royal heading", "", 0, -h * .5f - 2, 700, 72, Ink.Display);

            card = (RectTransform)RoyalCardKit.Panel(content, "Royal feature card", 0, -CardTop - 300, Width, 600, 40);
            rays = (RectTransform)Container(card, "Royal rays");
            RoyalMenuSkin.Place(rays, 0, 0, 0, 0);
            for (int i = 0; i < 16; i++)
            {
                var ray = Shape(rays, "Ray " + i, 0, 0, 13, 100, 6.5f, C("ffd24a", 0), C("ffd24a", .28f));
                ray.softness = 5;
                ray.rectTransform.pivot = new Vector2(.5f, 0); ray.rectTransform.anchoredPosition = Vector2.zero;
                ray.rectTransform.localRotation = Quaternion.Euler(0, 0, i * 22.5f + 11.25f);
            }
            var glow = Shape(card, "Royal glow", 0, 0, 400, 400, 200, C("ffffff", .6f), C("fff2d0", .45f));
            glow.softness = 150;
            var artGo = new GameObject("Royal feature art", typeof(RectTransform), typeof(Image));
            artGo.transform.SetParent(card, false);
            art = (RectTransform)artGo.transform;
            var image = artGo.GetComponent<Image>(); image.preserveAspect = true; image.raycastTarget = false;
            Shape(card, "Royal separator", 0, 0, Width - 88, 2, 1, C("c9ae7c", .75f), C("c9ae7c", .75f));
            Shape(card, "Royal separator light", 0, 0, Width - 88, 1.4f, .7f, C("ffffff", .8f), C("ffffff", .8f));
            message = Words(card, "Royal message", "", 0, 0, Width - 60, 40, Ink.Navy);
            message.lineSpacing = 1.05f;

            // The next step, apart from the card: the green primary button (PLAY is always the primary action).
            var button = new GameObject("Play next level", typeof(RectTransform), typeof(Image), typeof(Button));
            button.transform.SetParent(content, false);
            button.GetComponent<Button>().onClick.AddListener(() => { if (home) home.OpenNextLevel(); });
            action = (RectTransform)CleanButton(button.transform, "PLAY", 0, -900, Width, ButtonHeight, Kind.Primary, 46);
            actionCaption = action.Find("Royal caption").GetComponent<Text>();
            var motion = go.AddComponent<RoyalCardMotion>();
            motion.entrance = false;
            motion.controls = new[] { Feedback(action, true) };
            page.gameObject.SetActive(false);
        }
    }
}
