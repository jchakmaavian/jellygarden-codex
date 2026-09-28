using System;
using System.Collections;
using JellyGarden.Scripts;
using JellyGarden.Scripts.GUI;
using JellyGarden.Scripts.Juice;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalThrone.Presentation
{
    // The area dialogs, drawn inside the Home dialog in its royal style: the current area's tasks, the list of every area,
    // the chest of a restored area, the next area's welcome, a note when stars run short, and "coming soon" past the last
    // area with art. A restored area can be viewed full screen from the list.
    [DisallowMultipleComponent]
    public sealed class RoyalAreaView : MonoBehaviour
    {
        static readonly Color Navy = new Color(.03f, .13f, .32f), Ivory = new Color(1, .97f, .86f), Muted = new Color(.30f, .38f, .52f);
        public const string Tasks = "area", List = "areas", Chest = "area-complete", Unlocked = "area-unlocked", Short = "area-stars", Soon = "area-soon";
        const int MaxTasks = 12;

        sealed class Tile { public Button button; public Image art; public Text name, cost; public GameObject pill, tick; public RoyalPanelGraphic face, pillFace; }
        sealed class Card { public Button button; public Image art; public Text title, status; public RoyalPanelGraphic face; }

        RoyalHomeScreen home;
        RoyalAreaScene scene;
        Font display, body;
        Sprite star;
        RectTransform tasksGroup, listGroup, bar, overlay;
        RectTransform barFill;
        Text barText, secondaryLabel, overlayTitle;
        Button secondary, overlayBack;
        Tile[] tiles;
        Card[] cards;
        Vector2 buttonMin, buttonMax;
        Action primary;
        RoyalAreas.Reward lastReward;
        int shortTask = -1;
        RoyalAreaInfo viewing;
        Coroutine pendingChest;
        readonly System.Collections.Generic.Dictionary<Button, RoyalPanelGraphic> faces = new System.Collections.Generic.Dictionary<Button, RoyalPanelGraphic>();

        public bool Viewing => viewing != null;
        public bool TasksVisible => tasksGroup && tasksGroup.gameObject.activeSelf;
        public bool ListVisible => listGroup && listGroup.gameObject.activeSelf;
        public int VisibleTiles { get { int n = 0; if (tiles != null) foreach (var t in tiles) if (t.button.gameObject.activeSelf) n++; return n; } }
        public Button TaskButton(int task) => tiles != null && task >= 0 && task < tiles.Length ? tiles[task].button : null;
        public Button AreaButton(int number) => cards != null && number >= 1 && number <= cards.Length ? cards[number - 1].button : null;
        public Button SecondaryButton => secondary;
        public Button ViewBackButton => overlayBack;
        public RoyalAreaScene Scene => scene;

        public static RoyalAreaView Attach(RoyalHomeScreen home, RoyalAreaScene scene)
        {
            var view = home.GetComponent<RoyalAreaView>();
            if (!view) view = home.gameObject.AddComponent<RoyalAreaView>();
            view.home = home; view.scene = scene; view.Build();
            return view;
        }

        public static bool Handles(string panel) => panel == Tasks || panel == List || panel == Chest || panel == Unlocked || panel == Short || panel == Soon;
        public static bool Tall(string panel) => panel == Tasks || panel == List;

        /// <summary>Sends the Area card to the dialog the area's state calls for.</summary>
        public string Route(string panel)
        {
            if (panel != Tasks) return panel;
            if (RoyalAreas.Complete) return Chest;
            var area = RoyalAreas.CurrentArea;
            return area == null || !area.Ready ? Soon : Tasks;
        }

        void Build()
        {
            if (tiles != null) return;
            display = home.dialogTitle.font; body = home.dialogDetail.font;
            star = Resources.Load<Sprite>("ReferenceGameplay/Home/16674");
            var dialog = home.dialogRect;
            var buttonRect = (RectTransform)home.dialogButton.transform;
            buttonMin = buttonRect.anchorMin; buttonMax = buttonRect.anchorMax;
            // Collect, Play and Let's go run after the dialog's own close.
            home.dialogButton.onClick.AddListener(() => { var action = primary; primary = null; action?.Invoke(); });

            bar = Rect("Area progress", dialog, .12f, .557f, .82f, .603f);
            Surface("Track", bar, new Color(.05f, .16f, .38f), new Color(.02f, .09f, .24f), 1000);
            barFill = Rect("Fill", bar, 0, 0, .5f, 1);
            var fill = Surface("Fill face", barFill, new Color(.52f, .86f, .18f), new Color(.20f, .56f, .05f), 1000);
            ((RectTransform)fill.transform).offsetMin = new Vector2(3, 3); ((RectTransform)fill.transform).offsetMax = new Vector2(-3, -3);
            barText = Label("Progress", bar, "0/0", display, 24, Ivory); Shade(barText);
            var chest = new GameObject("Area chest", typeof(RectTransform)).AddComponent<Image>();
            chest.transform.SetParent(bar, false); chest.sprite = home.icons != null && home.icons.Length > 5 ? home.icons[5] : null; chest.preserveAspect = true; chest.raycastTarget = false;
            Bounds(chest.rectTransform, .93f, -.7f, 1.22f, 1.7f);

            tasksGroup = Rect("Area tasks", dialog, .06f, .165f, .94f, .545f);
            tiles = new Tile[MaxTasks];
            for (int i = 0; i < MaxTasks; i++) tiles[i] = NewTile(i);

            listGroup = Rect("Areas", dialog, .06f, .165f, .94f, .605f);
            int count = Mathf.Max(1, RoyalAreas.All.Length);
            cards = new Card[count];
            for (int i = 0; i < count; i++) cards[i] = NewCard(i, count);

            secondary = FramedButton("All areas", dialog);
            Bounds((RectTransform)secondary.transform, .08f, .035f, .47f, .14f);
            secondaryLabel = Label("Label", secondary.transform, "All areas", display, 28, Ivory); Shade(secondaryLabel);
            secondary.onClick.AddListener(() => home.OpenPanel(home.OpenPanelName == List ? Tasks : List, secondary.gameObject));

            overlay = Rect("Area viewing", home.safeArea, 0, 0, 1, 1);
            var banner = Panel("Area name", overlay, Ivory, new Color(.96f, .89f, .74f));
            Bounds(banner, .14f, .865f, .86f, .935f);
            overlayTitle = Label("Title", banner, "", display, 32, Navy);
            overlayBack = FramedButton("Back to areas", overlay);
            Bounds((RectTransform)overlayBack.transform, .22f, .07f, .78f, .135f);
            var backLabel = Label("Label", overlayBack.transform, "Back to areas", display, 30, Ivory); Shade(backLabel);
            overlayBack.onClick.AddListener(EndView);
            overlay.gameObject.SetActive(false);
            Hide();
        }

        Tile NewTile(int i)
        {
            int col = i % 3, row = i / 3;
            var tile = new Tile();
            tile.button = FramedButton("Task " + (i + 1), tasksGroup);
            tile.face = faces[tile.button];
            Bounds((RectTransform)tile.button.transform, col / 3f + .008f, 1 - (row + 1) / 4f + .012f, (col + 1) / 3f - .008f, 1 - row / 4f - .012f);
            tile.art = new GameObject("Object", typeof(RectTransform)).AddComponent<Image>();
            tile.art.transform.SetParent(tile.button.transform, false); tile.art.preserveAspect = true; tile.art.raycastTarget = false;
            Bounds(tile.art.rectTransform, .04f, .10f, .40f, .90f);
            tile.name = Label("Name", tile.button.transform, "", body, 17, Navy);
            tile.name.fontStyle = FontStyle.Bold; tile.name.alignment = TextAnchor.MiddleLeft; tile.name.resizeTextMinSize = 11;
            Bounds(tile.name.rectTransform, .43f, .44f, .97f, .94f);
            var pill = Rect("Cost", tile.button.transform, .43f, .09f, .93f, .42f);
            tile.pill = pill.gameObject;
            tile.pillFace = Surface("Pill", pill, new Color(.25f, .56f, .98f), new Color(.05f, .27f, .70f), 1000);
            var starIcon = new GameObject("Star", typeof(RectTransform)).AddComponent<Image>();
            starIcon.transform.SetParent(pill, false); starIcon.sprite = star; starIcon.preserveAspect = true; starIcon.raycastTarget = false;
            Bounds(starIcon.rectTransform, .02f, -.12f, .42f, 1.12f);
            tile.cost = Label("Stars", pill, "1", display, 22, Ivory); Bounds(tile.cost.rectTransform, .40f, 0, .96f, 1); Shade(tile.cost);
            tile.tick = Tick(tile.button.transform);
            int index = i;
            tile.button.onClick.AddListener(() => Choose(index));
            return tile;
        }

        Card NewCard(int i, int count)
        {
            int col = i % 2, row = i / 2, rows = Mathf.CeilToInt(count / 2f);
            var card = new Card();
            card.button = FramedButton("Area " + (i + 1), listGroup);
            card.face = faces[card.button];
            Bounds((RectTransform)card.button.transform, col / 2f + .008f, 1 - (row + 1f) / rows + .01f, (col + 1) / 2f - .008f, 1 - (float)row / rows - .01f);
            card.art = new GameObject("Scene", typeof(RectTransform)).AddComponent<Image>();
            card.art.transform.SetParent(card.button.transform, false); card.art.preserveAspect = true; card.art.raycastTarget = false;
            Bounds(card.art.rectTransform, .03f, .07f, .26f, .93f);
            card.title = Label("Name", card.button.transform, "", body, 17, Navy);
            card.title.fontStyle = FontStyle.Bold; card.title.alignment = TextAnchor.MiddleLeft; card.title.resizeTextMinSize = 11;
            Bounds(card.title.rectTransform, .29f, .46f, .97f, .94f);
            card.status = Label("Status", card.button.transform, "", body, 15, Muted);
            card.status.alignment = TextAnchor.MiddleLeft; card.status.resizeTextMinSize = 11;
            Bounds(card.status.rectTransform, .29f, .08f, .97f, .46f);
            int number = i + 1;
            card.button.onClick.AddListener(() => ChooseArea(number));
            return card;
        }

        /// <summary>Hides every area control; the Home dialog calls this before it opens any panel.</summary>
        public void Hide()
        {
            primary = null;
            if (tasksGroup) tasksGroup.gameObject.SetActive(false);
            if (listGroup) listGroup.gameObject.SetActive(false);
            if (bar) bar.gameObject.SetActive(false);
            if (secondary) secondary.gameObject.SetActive(false);
            var buttonRect = (RectTransform)home.dialogButton.transform;
            buttonRect.anchorMin = new Vector2(buttonMin.x, buttonRect.anchorMin.y); buttonRect.anchorMax = new Vector2(buttonMax.x, buttonRect.anchorMax.y);
        }

        /// <summary>Fills the dialog for one of the area panels.</summary>
        public void Fill(string panel, ref string title, ref string copy, ref string detail, ref int icon)
        {
            var area = RoyalAreas.CurrentArea;
            var p = home.Progress;
            switch (panel)
            {
                case Tasks:
                    title = area.name; copy = "Area " + area.number + " · " + Count(p.available, "star", "stars") + " to spend"; icon = 5;
                    ShowTasks(area, p.available); SplitButtons("All areas");
                    break;
                case List:
                    int restored = Mathf.Clamp(RoyalAreas.Current - 1 + (RoyalAreas.Complete ? 1 : 0), 0, RoyalAreas.All.Length);
                    title = "The Royal Realm"; copy = restored + " of " + RoyalAreas.All.Length + " areas restored"; icon = 8;
                    ShowList(); SplitButtons(RoyalAreas.CurrentArea != null && RoyalAreas.CurrentArea.Ready ? "Tasks" : null);
                    break;
                case Chest:
                    title = area.name + " restored!"; copy = "Area " + area.number + " is complete"; icon = 5;
                    detail = "Your chest holds " + Count(area.rewardCoins, "coin", "coins") + BoostText(area.rewardBoost) + ".";
                    home.dialogButtonLabel.text = "Collect"; primary = CollectAndContinue;
                    break;
                case Unlocked:
                    title = "New area: " + area.name; copy = area.subtitle; icon = 8;
                    detail = "You received " + Count(lastReward.coins, "coin", "coins") + BoostText(lastReward.boost.ToString()) + ".\n" +
                             Count(area.tasks.Length, "task", "tasks") + " to restore here with your stars.";
                    home.dialogButtonLabel.text = "Let's go";
                    break;
                case Short:
                    var task = area != null && shortTask >= 0 && shortTask < area.tasks.Length ? area.tasks[shortTask] : null;
                    int missing = task != null ? Mathf.Max(1, task.cost - p.available) : 1;
                    title = Count(missing, "more star", "more stars") + " needed"; copy = task != null ? task.name : "Restore the area"; icon = 3;
                    detail = "Win levels to earn up to three stars each, then come back to build.";
                    home.dialogButtonLabel.text = "Play level " + p.nextLevel; primary = home.OpenNextLevel;
                    break;
                case Soon:
                    bool all = RoyalAreas.AllRestored;
                    title = all ? "Every area restored!" : "More areas coming soon"; icon = 8;
                    copy = all ? "The whole kingdom shines again" : "The next part of the castle is being prepared";
                    detail = "Keep winning levels: the stars you earn are saved for the next area.";
                    break;
            }
        }

        void SplitButtons(string secondaryCaption)
        {
            home.dialogDetail.gameObject.SetActive(false);
            bool two = !string.IsNullOrEmpty(secondaryCaption);
            secondary.gameObject.SetActive(two);
            if (two) secondaryLabel.text = secondaryCaption;
            var buttonRect = (RectTransform)home.dialogButton.transform;
            buttonRect.anchorMin = new Vector2(two ? .53f : buttonMin.x, buttonRect.anchorMin.y);
            buttonRect.anchorMax = new Vector2(two ? .92f : buttonMax.x, buttonRect.anchorMax.y);
            buttonRect.offsetMin = new Vector2(0, buttonRect.offsetMin.y); buttonRect.offsetMax = new Vector2(0, buttonRect.offsetMax.y);
        }

        void ShowTasks(RoyalAreaInfo area, int available)
        {
            tasksGroup.gameObject.SetActive(true); bar.gameObject.SetActive(true);
            int done = RoyalAreas.DoneCount, n = area.tasks.Length;
            barFill.anchorMax = new Vector2(n > 0 ? (float)done / n : 0, 1);
            barText.text = done + "/" + n;
            for (int i = 0; i < tiles.Length; i++)
            {
                var tile = tiles[i];
                bool used = i < n;
                tile.button.gameObject.SetActive(used);
                if (!used) continue;
                var task = area.tasks[i];
                bool built = RoyalAreas.IsDone(i), affordable = available >= task.cost;
                tile.art.sprite = RoyalAreas.Art(task.patch);
                tile.name.text = task.name;
                tile.cost.text = task.cost.ToString();
                tile.pill.SetActive(!built); tile.tick.SetActive(built);
                tile.button.interactable = !built;
                tile.face.topColor = built ? new Color(.90f, .98f, .82f) : Ivory;
                tile.face.bottomColor = built ? new Color(.74f, .90f, .62f) : new Color(.96f, .89f, .74f);
                tile.face.SetVerticesDirty();
                tile.pillFace.color = affordable ? Color.white : new Color(.62f, .66f, .74f);
                tile.art.color = built ? Color.white : new Color(1, 1, 1, .92f);
            }
        }

        void ShowList()
        {
            listGroup.gameObject.SetActive(true);
            var all = RoyalAreas.All;
            for (int i = 0; i < cards.Length; i++)
            {
                var card = cards[i];
                if (i >= all.Length) { card.button.gameObject.SetActive(false); continue; }
                var area = all[i];
                bool restored = area.number < RoyalAreas.Current || area.number == RoyalAreas.Current && RoyalAreas.Complete;
                bool current = area.number == RoyalAreas.Current && !restored;
                card.title.text = "Area " + area.number + " · " + area.name;
                card.art.sprite = area.Ready ? RoyalAreas.Art(restored ? area.thumbAfter : area.thumbBefore) : null;
                card.art.enabled = card.art.sprite;
                card.art.color = restored || current ? Color.white : new Color(.45f, .5f, .6f);
                card.status.text = restored ? "Restored · tap to visit" : current ? (area.Ready ? RoyalAreas.DoneCount + " of " + area.tasks.Length + " tasks · in progress" : "Coming soon")
                    : area.Ready ? "Locked · " + Count(area.TotalCost, "star", "stars") : "Coming soon";
                card.button.interactable = area.Ready && (restored || current);
                card.face.topColor = restored ? new Color(.90f, .98f, .82f) : current ? Ivory : new Color(.84f, .86f, .90f);
                card.face.bottomColor = restored ? new Color(.74f, .90f, .62f) : current ? new Color(.96f, .89f, .74f) : new Color(.70f, .73f, .80f);
                card.face.SetVerticesDirty();
            }
        }

        /// <summary>While a built task's star flight, reveal and the list's return play (the reference's order).</summary>
        public bool Building => building != null;
        Coroutine building;
        void OnDisable() { building = null; }

        void Choose(int task)
        {
            if (building != null) return;
            var result = RoyalAreas.Build(task, home.Progress.stars);
            if (result == RoyalAreas.BuildResult.NotEnoughStars) { shortTask = task; home.OpenPanel(Short, tiles[task].button.gameObject); return; }
            if (result != RoyalAreas.BuildResult.Built) return;
            building = StartCoroutine(BuildMotion(task));
        }

        // As the reference stages a task (Design/RoyalMatchReference/OBSERVATIONS.md): the star flies from Do It to the star
        // counter, which then counts down; the list closes and the object appears under a sparkle beam; a moment later the
        // list opens again on the next task. The star is already spent (RoyalAreas.Build); this is presentation only.
        IEnumerator BuildMotion(int task)
        {
            bool landed = false;
            if (!RoyalWinSequence.FlyStar(tiles[task].button.transform, home.starLabel ? home.starLabel.transform : null, () => landed = true)) landed = true;
            float deadline = Time.unscaledTime + 1.2f;
            while (!landed && Time.unscaledTime < deadline) yield return null;
            home.ClosePanel();
            home.RefreshProgress();
            scene.Reveal(task);
            RoyalWinSequence.Beam(scene.Patch(task) ? scene.Patch(task).transform : null);
            if (RoyalAreas.Complete)
            {
                building = null;
                if (pendingChest != null) StopCoroutine(pendingChest);
                pendingChest = StartCoroutine(ChestWhenRevealed());
                yield break;
            }
            while (scene.Revealing) yield return null;
            yield return new WaitForSecondsRealtime(1.3f);
            building = null;
            if (home.IsHomeVisible && !home.dialog.activeSelf) home.OpenPanel(Tasks, home.areaButton.gameObject);
        }

        IEnumerator ChestWhenRevealed()
        {
            while (scene.Revealing) yield return null;
            yield return new WaitForSecondsRealtime(.45f);
            pendingChest = null;
            if (home.IsHomeVisible && !home.dialog.activeSelf && RoyalAreas.Complete) home.OpenPanel(Chest, home.areaButton.gameObject);
        }

        void CollectAndContinue()
        {
            if (!RoyalAreas.CollectChest(out lastReward)) return;
            home.RefreshProgress();
            scene.ShowCurrent();
            var next = RoyalAreas.CurrentArea;
            home.OpenPanel(next != null && next.Ready ? Unlocked : Soon, home.areaButton.gameObject);
        }

        void ChooseArea(int number)
        {
            var area = RoyalAreas.All[number - 1];
            bool restored = number < RoyalAreas.Current || number == RoyalAreas.Current && RoyalAreas.Complete;
            if (restored) View(area);
            else if (number == RoyalAreas.Current) home.OpenPanel(Tasks, cards[number - 1].button.gameObject);
        }

        /// <summary>Shows a restored area full screen, with the Home controls put away until Back.</summary>
        public void View(RoyalAreaInfo area)
        {
            if (area == null || !area.Ready) return;
            home.ClosePanel();
            viewing = area;
            scene.Hold = true;
            scene.Show(area, true);
            overlayTitle.text = "Area " + area.number + ": " + area.name;
            overlay.gameObject.SetActive(true); overlay.SetAsLastSibling();
            home.SceneViewOpen = true;
        }

        public void EndView()
        {
            if (viewing == null) return;
            viewing = null;
            overlay.gameObject.SetActive(false);
            home.SceneViewOpen = false;
            scene.Hold = false;
            scene.ShowCurrent();
            home.OpenPanel(List, home.areaButton.gameObject);
        }

        void Update()
        {
            if (viewing == null) return;
            // Back leaves the view before LevelManager (execution order 1000) can read the same press as quit.
            if (!home.IsHomeVisible) { viewing = null; overlay.gameObject.SetActive(false); home.SceneViewOpen = false; scene.Hold = false; scene.ShowCurrent(); return; }
            if (InputManager.Instance != null && InputManager.Instance.Gameplay.Cancel.WasPressedThisFrame()) { EndView(); RoyalBackButton.Consume(); }
        }

        static string BoostText(string boost)
        {
            switch (boost)
            {
                case "Stripes": return " and a rocket";
                case "Packages": return " and a TNT";
                case "Colorful_bomb": return " and a color ball";
                default: return "";
            }
        }
        static string Count(int n, string one, string many) => n.ToString("N0") + " " + (n == 1 ? one : many);

        // ---- Royal UI pieces, matching RoyalHomeScreenBuilder's dialog style ----
        static RectTransform Rect(string name, Transform parent, float x0, float y0, float x1, float y1)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            r.gameObject.layer = 5; r.SetParent(parent, false); Bounds(r, x0, y0, x1, y1);
            return r;
        }
        static void Bounds(RectTransform r, float x0, float y0, float x1, float y1)
        { r.anchorMin = new Vector2(x0, y0); r.anchorMax = new Vector2(x1, y1); r.offsetMin = r.offsetMax = Vector2.zero; }
        static RoyalPanelGraphic Surface(string name, Transform parent, Color top, Color bottom, float radius)
        {
            var r = Rect(name, parent, 0, 0, 1, 1);
            var g = r.gameObject.AddComponent<RoyalPanelGraphic>(); g.topColor = top; g.bottomColor = bottom; g.cornerRadius = radius; g.raycastTarget = false;
            return g;
        }
        static RectTransform Panel(string name, Transform parent, Color top, Color bottom)
        {
            var outer = Surface(name, parent, new Color(1, .88f, .54f), new Color(.72f, .48f, .14f), 22);
            var trim = Surface("Trim", outer.transform, new Color(1, .96f, .73f), new Color(.87f, .65f, .29f), 20);
            trim.rectTransform.offsetMin = new Vector2(3, 4); trim.rectTransform.offsetMax = new Vector2(-3, -3);
            var face = Surface("Face", trim.transform, top, bottom, 18);
            face.rectTransform.offsetMin = new Vector2(2, 3); face.rectTransform.offsetMax = new Vector2(-2, -2);
            return outer.rectTransform;
        }
        // A framed button: gold rim, trim and face; the face (kept in faces) carries the tile's state colours.
        Button FramedButton(string name, Transform parent)
        {
            bool blue = name == "All areas" || name == "Back to areas";
            var panel = Panel(name, parent, blue ? new Color(.33f, .65f, 1) : Ivory, blue ? new Color(.02f, .27f, .62f) : new Color(.96f, .89f, .74f));
            var outer = panel.GetComponent<RoyalPanelGraphic>(); outer.raycastTarget = true;
            var face = panel.Find("Trim/Face").GetComponent<RoyalPanelGraphic>();
            // The button's own graphic is its hit area; the face carries the state colours.
            var button = panel.gameObject.AddComponent<Button>(); button.targetGraphic = outer; button.transition = Selectable.Transition.None;
            var motion = panel.gameObject.AddComponent<RoyalButtonMotion>(); motion.pressScale = .975f; motion.hoverScale = 1; motion.response = 20;
            faces[button] = face;
            return button;
        }
        Text Label(string name, Transform parent, string value, Font font, int size, Color color)
        {
            var r = Rect(name, parent, 0, 0, 1, 1);
            var t = r.gameObject.AddComponent<Text>(); t.font = font; t.text = value; t.fontSize = size; t.color = color; t.alignment = TextAnchor.MiddleCenter;
            t.raycastTarget = false; t.resizeTextForBestFit = true; t.resizeTextMaxSize = size; t.resizeTextMinSize = Mathf.Max(10, size - 10);
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate;
            return t;
        }
        static void Shade(Text t) { var s = t.gameObject.AddComponent<Shadow>(); s.effectColor = new Color(.02f, .08f, .25f, .6f); s.effectDistance = new Vector2(0, -1.5f); }
        // A green seal with a tick, where the cost pill sits: a round disc (kept square by its fitter) and two rotated bars.
        static GameObject Tick(Transform parent)
        {
            var r = Rect("Built", parent, .43f, .06f, .93f, .46f);
            var seal = Rect("Seal", r, 0, 0, 1, 1);
            var fit = seal.gameObject.AddComponent<AspectRatioFitter>(); fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent; fit.aspectRatio = 1;
            var disc = Surface("Disc", seal, new Color(.46f, .84f, .20f), new Color(.16f, .52f, .06f), 1000);
            Bar(disc.transform, new Vector2(.40f, .44f), new Vector2(.30f, .12f), -45);
            Bar(disc.transform, new Vector2(.59f, .52f), new Vector2(.52f, .12f), 50);
            r.gameObject.SetActive(false);
            return r.gameObject;
        }
        // A bar of the given fractional size around a centre in the square disc, rotated about its middle.
        static void Bar(Transform parent, Vector2 centre, Vector2 size, float angle)
        {
            var g = Surface("Tick", parent, Color.white, new Color(.93f, .97f, .9f), 1000);
            var r = g.rectTransform; r.anchorMin = centre - size * .5f; r.anchorMax = centre + size * .5f;
            r.offsetMin = r.offsetMax = Vector2.zero; r.localRotation = Quaternion.Euler(0, 0, angle);
        }
    }
}
