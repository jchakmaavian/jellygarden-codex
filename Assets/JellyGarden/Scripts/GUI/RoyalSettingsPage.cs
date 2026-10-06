using PlayerPrefs = RoyalThrone.Testing.RoyalSave;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using JellyGarden.Scripts.Juice;
using static RoyalThrone.Presentation.RoyalCardKit;

namespace RoyalThrone.Presentation
{
    // The Home screen's settings as a full page, after the user's reference (2026-09-29): a header with the title and the red
    // close, one panel of switches (Music, Sound, Motion, Hint), then How to Play and any other entry the settings group holds
    // (the Mod Menu, when mods are on). RoyalHomeScreen still owns the settings: a switch presses its settings button, the
    // close closes its panel, and this page shows only while that panel is open, over everything else on Home. The page is
    // laid out on an 800-unit column from the screen's top; a screen wider than 9:16 (a tablet) scales the column down.
    public sealed class RoyalSettingsPage : MonoBehaviour
    {
        const float Column = 800, PhoneHeight = 1422, PanelWidth = 736, RowHeight = 124, PanelTop = 282, ButtonHeight = 116, Gap = 28;
        static readonly string[] Labels = { "Music", "Sound", "Motion", "Hint" };
        static readonly string[] Keys = { "Music", "Sound", "RoyalThrone.HomeMotion", RoyalHintSettings.PreferenceKey };

        RoyalHomeScreen home;
        RectTransform page, content;
        CanvasGroup group;
        readonly List<Switch> switches = new List<Switch>();
        readonly List<Button> extras = new List<Button>();
        readonly List<GameObject> extraRows = new List<GameObject>();
        float shownAt;
        // How to Play was opened from this page: when the lessons close, the page comes back (UI polish, 2026-09-30).
        bool helpOpen;

        sealed class Switch { public int index; public RoyalCardShape rim, face; public RectTransform knob; public Text on, off; public int state = -1; }

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
                if (!h.GetComponent<RoyalSettingsPage>()) h.gameObject.AddComponent<RoyalSettingsPage>().home = h;
        }

        public bool IsOpen => page && page.gameObject.activeSelf;

        void Awake() { if (!home) home = GetComponent<RoyalHomeScreen>(); }

        void OnDisable() { if (page) page.gameObject.SetActive(false); }

        void OnDestroy() { if (page) Destroy(page.gameObject); }

        void LateUpdate()
        {
            bool open = home && home.IsHomeVisible && home.dialog && home.dialog.activeSelf && home.settingsGroup && home.settingsGroup.activeSelf;
            if (open && !page) Build();
            if (!page) return;
            if (page.gameObject.activeSelf != open)
            {
                page.gameObject.SetActive(open);
                if (open) shownAt = Time.unscaledTime;
            }
            if (helpOpen && !HelpShowing())
            {
                helpOpen = false;
                if (!open && home && home.IsHomeVisible && home.OpenPanelName == null) home.OpenPanel("settings");
            }
            if (!open) return;
            Fit();
            SyncExtras();
            foreach (var s in switches) Show(s, Enabled(s.index));
            // A calm entrance: fade in over 0.2 s while the column settles 10 units down into place.
            float p = Mathf.Clamp01((Time.unscaledTime - shownAt) / .2f), e = 1 - (1 - p) * (1 - p) * (1 - p);
            group.alpha = e;
            content.anchoredPosition = new Vector2(0, 10 * (1 - e));
        }

        static bool Enabled(int index) => PlayerPrefs.GetInt(Keys[index], 1) != 0;

        static bool HelpShowing()
        {
            var menus = JellyGarden.Scripts.GUI.MenuManager.Instance;
            var help = menus ? menus.GetCanvasGlobal().Find("Tutorial") : null;
            return help && help.gameObject.activeSelf;
        }

        void Fit()
        {
            var r = page.rect;
            if (r.width <= 0 || r.height <= 0) return;
            float s = Mathf.Min(r.width / Column, r.height / PhoneHeight);
            if (Mathf.Abs(content.localScale.x - s) > .0005f) content.localScale = new Vector3(s, s, 1);
        }

        // ---- Building --------------------------------------------------------------------------------------------------

        void Build()
        {
            var canvas = home.GetComponentInParent<Canvas>();
            if (!canvas) return;
            var go = new GameObject("Royal settings page", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(CanvasGroup));
            page = (RectTransform)go.transform;
            page.SetParent(canvas.rootCanvas.transform, false);
            Stretch(page, 0);
            var own = go.GetComponent<Canvas>();
            own.overrideSorting = true; own.sortingOrder = 60;   // over Home's navigation bar (20)
            group = go.GetComponent<CanvasGroup>();

            // The backdrop: a royal-blue fall; it takes every tap the page does not.
            var backdrop = Inset(page, "Backdrop", 0, 0, C("2256c6"), C("0a2a78"));
            backdrop.raycastTarget = true;
            // The palace picture that showed faintly here was removed from the project (2026-10-04).
            Inset(page, "Hall veil", 0, 0, C("0d3596", .25f), C("061f5c", .55f));

            // The column: anchored at the top edge, 0 tall, so every row below is placed by its distance from the top.
            content = (RectTransform)new GameObject("Content", typeof(RectTransform)).transform;
            content.SetParent(page, false);
            content.anchorMin = content.anchorMax = content.pivot = new Vector2(.5f, 1);
            content.sizeDelta = new Vector2(Column, 0);

            // The header: a wider band than any screen, a gold line under it and a soft shadow below that.
            Shape(content, "Header", 0, -116, 3000, 232, 0, C("2f6bd9"), C("1747ad"));
            Shape(content, "Header light", 0, -40, 3000, 80, 0, C("ffffff", .08f), C("ffffff", 0));
            Shape(content, "Header shadow", 0, -250, 3000, 30, 0, C("000a30", .45f), C("000a30", 0));
            Shape(content, "Header gold", 0, -233, 3000, 6, 0, C("ffe27a"), C("e3a02a"));
            Shape(content, "Header gold edge", 0, -236.5f, 3000, 1.2f, 0, C("7a3a12"), C("7a3a12"));
            Words(content, "Royal heading", "Settings", 0, -128, 520, 66, Ink.Display);
            var close = Control(content, "Close", () => { if (home) home.ClosePanel(); });
            RedClose(close, 0);
            RoyalMenuSkin.Place(close, 322, -128, 100, 100);

            // The switches' panel: a deeper, see-through blue with a light rim; a thin line between rows.
            int rows = Mathf.Min(home.settingButtons != null ? home.settingButtons.Length : 0, Labels.Length);
            float panelHeight = rows * RowHeight + 16, panelY = -PanelTop - panelHeight * .5f;
            var panel = Container(content, "Royal settings panel");
            RoyalMenuSkin.Place(panel, 0, panelY, PanelWidth, panelHeight);
            var shade = Shape(panel, "Shadow", 0, -5, PanelWidth + 10, panelHeight + 10, 34, C("000a30", .35f), C("000a30", .35f));
            shade.softness = 14;
            Inset(panel, "Rim", 0, 30, C("86aaf2", .55f), C("4a76d6", .4f));
            Inset(panel, "Fill", 2.4f, 27.6f, C("12399a", .92f), C("0b2a78", .92f));
            for (int i = 0; i < rows; i++)
            {
                float y = RowY(i);
                if (i > 0)
                {
                    Shape(content, "Row line " + i, 0, y + RowHeight * .5f, PanelWidth - 56, 2, 1, C("6f98ee", .35f), C("6f98ee", .35f));
                    Shape(content, "Row line shade " + i, 0, y + RowHeight * .5f - 1.8f, PanelWidth - 56, 1.2f, .6f, C("041a52", .45f), C("041a52", .45f));
                }
                int index = i;
                var row = Control(content, "Row " + Labels[i], () => { if (home && home.settingButtons[index]) home.settingButtons[index].onClick.Invoke(); });
                RoyalMenuSkin.Place(row, 0, y, PanelWidth, RowHeight);
                Icon(row, i, -296);
                var label = Words(row, "Royal label", Labels[i], -80, 0, 340, 36, Ink.Light);
                label.alignment = TextAnchor.MiddleLeft;
                switches.Add(BuildSwitch(row, i));
            }

            // How to Play, the same lessons the pause menu's HELP opens; Home's panel closes first.
            float buttonY = panelY - panelHeight * .5f - Gap - ButtonHeight * .5f - 12;
            var help = Control(content, "How to play", () =>
            {
                if (home) home.ClosePanel();
                if (JellyGarden.Scripts.GUI.MenuManager.Instance) { JellyGarden.Scripts.GUI.MenuManager.Instance.ShowTutorial(true); helpOpen = HelpShowing(); }
            });
            RowButton(help, "How to Play", buttonY, Kind.Primary, true);
            nextButtonY = buttonY - ButtonHeight - Gap * .6f;
            page.gameObject.SetActive(false);
        }

        float nextButtonY;

        float RowY(int i) => -PanelTop - 8 - RowHeight * (i + .5f);

        // A tappable child of the column with the Button (and a clear Image as its tap area).
        static Transform Control(Transform parent, string name, UnityEngine.Events.UnityAction action)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = Color.clear;
            var button = go.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(action);
            button.onClick.AddListener(() => { if (JellyGarden.Scripts.SoundBase.Instance) JellyGarden.Scripts.SoundBase.Instance.PlaySound(JellyGarden.Scripts.SoundBase.Instance.click); });
            // The same press as the HUD's buttons: the control eases to 0.975 and 88% brightness while held, and back.
            go.AddComponent<RoyalHudPress>().parts = new[] { (RectTransform)go.transform };
            return go.transform;
        }

        // A full-width button in the reference's style: an icon on the left, a left-aligned caption, a chevron on the right.
        void RowButton(Transform t, string caption, float y, Kind kind, bool helpIcon)
        {
            CleanButton(t, null, 0, y, PanelWidth, ButtonHeight, kind);
            if (helpIcon)
            {
                var ring = Shape(t, "Royal icon ring", -296, 0, 46, 46, 23, C("fffaf0"), C("f1e3c4"));
                ring.band = 4.5f;
                Words(t, "Royal icon mark", "?", -295, 0, 40, 32, Ink.Light);
            }
            var text = Words(t, "Royal label", caption, -80, 1, 360, 38, CaptionInk(kind));
            text.alignment = TextAnchor.MiddleLeft;
            for (int arm = -1; arm <= 1; arm += 2)
            {
                var bar = Shape(t, "Royal chevron " + (arm < 0 ? "upper" : "lower"), 316, arm * -7.5f, 24, 7, 3.5f, C("fffaf0"), C("f1e3c4"));
                bar.rectTransform.localRotation = Quaternion.Euler(0, 0, arm * 45);
            }
        }

        // Any other button in Home's settings group (the Mod Menu entry) becomes a blue row button under How to Play.
        void SyncExtras()
        {
            if (!home.settingsGroup) return;
            var found = new List<Button>();
            foreach (Transform child in home.settingsGroup.transform)
            {
                var b = child.GetComponent<Button>();
                if (b && child.gameObject.activeSelf && System.Array.IndexOf(home.settingButtons, b) < 0) found.Add(b);
            }
            if (found.Count == extras.Count && found.TrueForAll(extras.Contains)) return;
            foreach (var row in extraRows) if (row) Destroy(row);
            extraRows.Clear(); extras.Clear();
            float y = nextButtonY;
            foreach (var b in found)
            {
                var source = b;
                var label = b.GetComponentInChildren<Text>();
                var t = Control(content, "Extra " + b.name, () => { if (source) source.onClick.Invoke(); });
                RowButton(t, label ? label.text : b.name, y, Kind.Secondary, false);
                extras.Add(b); extraRows.Add(t.gameObject);
                y -= ButtonHeight + Gap * .6f;
            }
        }

        // ---- Switches --------------------------------------------------------------------------------------------------

        static Switch BuildSwitch(Transform row, int index)
        {
            const float w = 204, h = 78, r = h * .5f;
            var holder = Container(row, "Royal switch");
            RoyalMenuSkin.Place(holder, 238, 0, w, h);
            var s = new Switch { index = index };
            var shadow = Shape(holder, "Shadow", 0, -3, w + 4, h + 4, r + 2, C("000a30", .45f), C("000a30", .45f));
            shadow.softness = 8;
            Inset(holder, "Edge", 0, r, C("5a3212", .9f), C("40220c", .9f));
            s.rim = Inset(holder, "Rim", 1.2f, r - 1.2f, C("ffe27a"), C("e3a02a"));
            s.face = Inset(holder, "Face", 5.5f, r - 5.5f, C("33b64d"), C("138536"));
            var gloss = Strip(holder, "Gloss", true, 8, 22, 11, C("ffffff", .2f), C("ffffff", .02f));
            gloss.rectTransform.sizeDelta = new Vector2(-2 * 24, 22);
            s.on = Words(holder, "On", "ON", -36, 0, 90, 28, Ink.OnGreen);
            s.off = Words(holder, "Off", "OFF", 34, 0, 100, 28, Ink.Light);
            var knob = Container(holder, "Knob");
            s.knob = (RectTransform)knob;
            RoyalMenuSkin.Place(knob, 63, 0, 62, 62);
            var knobShadow = Shape(knob, "Shadow", 0, -3, 64, 64, 32, C("00103a", .5f), C("00103a", .5f));
            knobShadow.softness = 6;
            Inset(knob, "Face", 0, 31, C("fffcf2"), C("e8dcc4"));
            Inset(knob, "Shine", 3, 28, C("ffffff", .7f), C("ffffff", 0));
            return s;
        }

        static void Show(Switch s, bool on)
        {
            int state = on ? 1 : 0;
            if (s.state == state) return;
            s.state = state;
            s.rim.from = on ? C("ffe27a") : C("6f98ee"); s.rim.to = on ? C("e3a02a") : C("3f6cd0");
            s.face.from = on ? C("33b64d") : C("0d2a6e"); s.face.to = on ? C("138536") : C("081d52");
            s.rim.SetVerticesDirty(); s.face.SetVerticesDirty();
            s.knob.anchoredPosition = new Vector2(on ? 63 : -63, 0);
            s.on.enabled = on; s.off.enabled = !on;
        }

        // ---- Icons (drawn, cream) --------------------------------------------------------------------------------------

        static void Icon(Transform row, int index, float x)
        {
            var icon = Container(row, "Royal icon");
            RoyalMenuSkin.Place(icon, x, 0, 64, 64);
            Color a = C("fffaf0"), b = C("ecdcb8");
            switch (index)
            {
                case 0:   // a pair of beamed notes
                    for (int n = 0; n < 2; n++)
                    {
                        var head = Shape(icon, "Head " + n, -10 + n * 24, -14 + n * 6, 21, 15, 7.5f, a, b);
                        head.rectTransform.localRotation = Quaternion.Euler(0, 0, 20);
                        Shape(icon, "Stem " + n, -1 + n * 24, 5 + n * 6, 4.5f, 36, 2, a, b);
                    }
                    var beam = Shape(icon, "Beam", 11, 24, 29, 8, 2, a, b);
                    beam.rectTransform.localRotation = Quaternion.Euler(0, 0, 14);
                    break;
                case 1:   // a speaker with two sound waves
                    Shape(icon, "Body", -17, 0, 12, 20, 2.5f, a, b);
                    var cone = Clip(icon, "Cone", -6, 0, 16, 44);
                    var diamond = Shape(cone, "Diamond", 8, 0, 28, 28, 3, a, b);
                    diamond.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
                    var waves = Clip(icon, "Waves", 20, 0, 22, 56);
                    for (int w = 0; w < 2; w++)
                    {
                        float d = 26 + w * 18;
                        var ring = Shape(waves, "Wave " + w, -18, 0, d, d, d * .5f, a, b);
                        ring.band = 4;
                    }
                    break;
                case 2:   // sparkles (Home's motion)
                    Shape(icon, "Spark long", -4, -2, 8, 44, 4, a, b);
                    Shape(icon, "Spark wide", -4, -2, 44, 8, 4, a, b);
                    var core = Shape(icon, "Spark core", -4, -2, 16, 16, 3, a, b);
                    core.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
                    Shape(icon, "Small long", 17, 17, 4.5f, 18, 2.25f, a, b);
                    Shape(icon, "Small wide", 17, 17, 18, 4.5f, 2.25f, a, b);
                    break;
                default:   // a light bulb
                    Shape(icon, "Glass", 0, 7, 34, 34, 17, a, b);
                    Shape(icon, "Neck", 0, -12, 18, 10, 3, a, b);
                    Shape(icon, "Base", 0, -21, 12, 6, 3, a, b);
                    Shape(icon, "Band", 0, -16.5f, 18, 1.6f, .8f, C("0b2a78", .8f), C("0b2a78", .8f));
                    break;
            }
        }

        // A container that clips what is drawn inside it to its rect.
        static Transform Clip(Transform parent, string name, float x, float y, float w, float h)
        {
            var c = Container(parent, name);
            RoyalMenuSkin.Place(c, x, y, w, h);
            c.gameObject.AddComponent<RectMask2D>();
            return c;
        }
    }
}
