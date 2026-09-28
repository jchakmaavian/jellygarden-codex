using PlayerPrefs = RoyalThrone.Testing.RoyalSave;
using System.Collections;
using System.Collections.Generic;
using JellyGarden.Scripts.Juice;
using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.GUI.TutorialStuff
{
    /// <summary>
    /// The lessons of levels 2 to 5, staged as the reference build shows them (Design/RoyalMatchReference/OBSERVATIONS.md);
    /// level 1's swap lesson is TutorialManager's.
    /// - Level 2 (Rocket) and level 3 (TNT): the opening board already holds a move that makes the piece. The in-game hint
    ///   outline marks that move and a hand shows it, with no caption. The lesson ends when a move is made.
    /// - Level 4 (grass): a caption over the board until the first move.
    /// - Level 5 (combining): once two power pieces stand side by side, a caption and a hand invite swapping them together;
    ///   the lesson ends when a move is made.
    /// Captions are our own wording. The pieces are never recoloured, so the verified opening layouts stay as they are. A
    /// lesson plays once (saved), only while its level has not been won, and never in the QA reference campaign. Nothing here
    /// blocks input; a pause or a popup hides a lesson for a while instead of ending it.
    /// </summary>
    public sealed class RoyalPowerTutorial : MonoBehaviour
    {
        enum Kind { Rocket, Tnt, Grass, Combo }
        sealed class Lesson { public int level; public Kind kind; public string caption; }
        static readonly Lesson[] Lessons =
        {
            new Lesson { level = 2, kind = Kind.Rocket },
            new Lesson { level = 3, kind = Kind.Tnt },
            new Lesson { level = 4, kind = Kind.Grass, caption = "Match on the grass to clear it away!" },
            new Lesson { level = 5, kind = Kind.Combo, caption = "Swap two power-ups together for a bigger blast!" },
        };

        /// <summary>The caption of the lesson showing now, or null (for checks).</summary>
        public static string Showing { get; private set; }
        /// <summary>How the latest lesson went: what it found, why it stopped, or that it was shown (for checks).</summary>
        public static string Outcome { get; private set; }
        /// <summary>True while a lesson's hand or caption is on screen (for checks).</summary>
        public static bool Visible => instance && instance.root && instance.root.gameObject.activeInHierarchy && (instance.hand.gameObject.activeSelf || instance.panel.gameObject.activeSelf);
        public static string SaveKey(int level) => "RoyalThrone.Lesson.Level" + level;

        static RoyalPowerTutorial instance;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetStatics() { instance = null; Showing = Outcome = null; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (instance) return;
            var go = new GameObject("Royal power tutorial"); DontDestroyOnLoad(go); instance = go.AddComponent<RoyalPowerTutorial>();
        }

        void OnEnable() { LevelManager.OnLevelLoaded += LevelLoaded; }
        void OnDisable() { LevelManager.OnLevelLoaded -= LevelLoaded; End(); }
        void OnDestroy() { if (instance == this) instance = null; if (root) Destroy(root.gameObject); }

        Coroutine running;
        void LevelLoaded()
        {
            if (running != null) { StopCoroutine(running); End(); }
            var lm = LevelManager.THIS; if (!lm) return;
            Lesson lesson = null; foreach (var l in Lessons) if (l.level == lm.currentLevel) lesson = l;
            if (lesson == null || RoyalThrone.Campaign.RoyalReferenceCampaign.Enabled) return;
            if (PlayerPrefs.GetInt(SaveKey(lesson.level), 0) != 0) return;
            if (PlayerPrefs.GetInt(string.Format("Level.{0:000}.StarsCount", lesson.level), 0) > 0) return;   // replaying a won level
            running = StartCoroutine(Run(lm, lesson));
        }

        // ---------------------------------------------------------------- the lessons

        static bool Plain(Item it) => it && !it.destroying && it.currentType == ItemsTypes.NONE && it.square && it.square.CanGoOut() &&
                                      !RoyalThrone.Campaign.RoyalCampaignBlockers.IsSpecialItem(it);
        static bool Power(Item it) => it && !it.destroying && !it.falling && it.square && RoyalPowerupDirector.IsPower(it.currentType) && it.currentType != ItemsTypes.INGREDIENT;

        static bool IsKind(RoyalHintPlan p, Kind kind)
        {
            if (p == null) return false; int n = p.MatchCells.Count;
            return kind == Kind.Rocket ? p.LongestRun == 4 && n == 4 : kind == Kind.Tnt && p.LongestRun == 3 && n == 5;
        }

        // The move on the settled board that makes the lesson's piece, nearest the middle; null when there is none.
        static RoyalHintPlan FindMove(LevelManager lm, Kind kind)
        {
            RoyalHintPlan best = null; float bestD = float.MaxValue; var mid = new Vector2((lm.maxCols - 1) * .5f, (lm.maxRows - 1) * .5f);
            for (int r = 0; r < lm.maxRows; r++)
                for (int c = 0; c < lm.maxCols; c++)
                {
                    var a = lm.GetSquare(c, r); if (!a || !Plain(a.item)) continue;
                    foreach (var d in new[] { new Vector2Int(1, 0), new Vector2Int(0, 1) })
                    {
                        var b = lm.GetSquare(c + d.x, r + d.y); if (!b || !Plain(b.item)) continue;
                        foreach (var pair in new[] { (a.item, b.item), (b.item, a.item) })
                        {
                            var p = RoyalHintSolver.EvaluateSwap(lm, pair.Item1, pair.Item2);
                            if (!IsKind(p, kind)) continue;
                            float dist = (new Vector2(c, r) - mid).sqrMagnitude; if (dist < bestD) { bestD = dist; best = p; }
                        }
                    }
                }
            return best;
        }

        // Two power pieces side by side, or nulls.
        static bool FindPair(LevelManager lm, out Item a, out Item b)
        {
            a = b = null;
            for (int r = 0; r < lm.maxRows; r++)
                for (int c = 0; c < lm.maxCols; c++)
                {
                    var s = lm.GetSquare(c, r); if (!s || !Power(s.item) || !s.CanGoOut()) continue;
                    foreach (var d in new[] { new Vector2Int(1, 0), new Vector2Int(0, 1) })
                    {
                        var t = lm.GetSquare(c + d.x, r + d.y);
                        if (t && Power(t.item) && t.CanGoOut()) { a = s.item; b = t.item; return true; }
                    }
                }
            return false;
        }

        // Waits for play and a still board (the opening fall, the level's start banners). False when the level was left.
        IEnumerator Settle(LevelManager lm, int level, float seconds, global::System.Action<bool> done)
        {
            float quiet = 0, give = Time.unscaledTime + 30; bool ok = false;
            while (Time.unscaledTime < give)
            {
                if (!lm || lm.currentLevel != level || lm.gameStatus == GameState.Map || lm.gameStatus == GameState.Win) break;
                bool still = lm.gameStatus == GameState.Playing && !lm.IsResolvingMatches && !lm.DragBlocked && !RoyalPowerupDirector.Busy;
                if (still) foreach (var it in lm.GetItems()) if (it && (it.falling || it.destroying)) { still = false; break; }
                quiet = still ? quiet + Time.unscaledDeltaTime : 0;
                if (quiet >= seconds) { ok = true; break; }
                yield return null;
            }
            done(ok);
        }

        IEnumerator Run(LevelManager lm, Lesson lesson)
        {
            var ai = AI.THIS; bool tips = ai && ai.allowShowTip;
            var presenter = ai ? (ai.GetComponent<RoyalHintPresenter>() ?? ai.gameObject.AddComponent<RoyalHintPresenter>()) : null;
            try
            {
                bool ready = false; yield return Settle(lm, lesson.level, .6f, v => ready = v);
                if (!ready) { Outcome = "level " + lesson.level + ": board never settled"; yield break; }
                if (lesson.kind == Kind.Rocket || lesson.kind == Kind.Tnt)
                {
                    if (lm.moveID != 0) { Outcome = "level " + lesson.level + ": a move came first"; yield break; }
                    var plan = FindMove(lm, lesson.kind);
                    if (plan == null) { Outcome = "level " + lesson.level + ": no " + lesson.kind + " move on the board"; yield break; }
                    Outcome = "level " + lesson.level + ": " + lesson.kind + " move " + plan.First.square.col + "," + plan.First.square.row + " <-> " + plan.Second.square.col + "," + plan.Second.square.row + " | shown";
                    Save(lesson.level); if (ai) ai.allowShowTip = false; Build(); Showing = lesson.kind + " move";
                    var first = plan.First; var second = plan.Second; int move = lm.moveID; float shown = 0;
                    while (lm && Live(lm) && lm.moveID == move && first && second)
                    {
                        if (Covered(lm)) { if (presenter) presenter.Hide(); yield return null; continue; }
                        if (presenter && !presenter.IsVisible && !lm.IsResolvingMatches) presenter.Show(RoyalHintSolver.EvaluateSwap(lm, first, second) ?? plan);
                        shown += Time.unscaledDeltaTime; Swipe(first.transform.position, second.transform.position, shown);
                        yield return null;
                    }
                }
                else if (lesson.kind == Kind.Grass)
                {
                    if (lm.moveID != 0) { Outcome = "level " + lesson.level + ": a move came first"; yield break; }
                    Outcome = "level " + lesson.level + ": grass caption | shown"; Save(lesson.level); Build();
                    focus = null; Caption(lesson.caption, RoyalThrone.Campaign.RoyalCampaignBlockers.Art("grass"), true);
                    int move = lm.moveID;
                    while (lm && Live(lm) && lm.moveID == move) { Covered(lm); Follow(); yield return null; }
                }
                else
                {
                    // Watch the level for two power pieces side by side, on a still board, for up to 15 moves.
                    Item a = null, b = null; bool found = false;
                    while (lm && Live(lm) && lm.currentLevel == lesson.level && lm.moveID < 15)
                    {
                        bool still = false; yield return Settle(lm, lesson.level, .35f, v => still = v);
                        if (!still) break;
                        if (FindPair(lm, out a, out b)) { found = true; break; }
                        int m = lm.moveID; while (lm && Live(lm) && lm.moveID == m) yield return null;
                    }
                    if (!found) { Outcome = "level " + lesson.level + ": no two power pieces met"; yield break; }
                    Outcome = "level " + lesson.level + ": combo " + a.square.col + "," + a.square.row + " + " + b.square.col + "," + b.square.row + " | shown";
                    Save(lesson.level); if (ai) ai.allowShowTip = false; Build();
                    focus = a.transform.position; Caption(lesson.caption, RoyalPieceSkin.For(ItemsTypes.PACKAGE, 0), false);
                    int move = lm.moveID; float shown = 0;
                    while (lm && Live(lm) && lm.moveID == move && a && b && !a.destroying && !b.destroying)
                    {
                        if (Covered(lm)) { yield return null; continue; }
                        shown += Time.unscaledDeltaTime; Swipe(a.transform.position, b.transform.position, shown);
                        yield return null;
                    }
                }
            }
            finally
            {
                if (presenter) presenter.Hide();
                if (ai && tips) ai.allowShowTip = true;
                End(); running = null;
            }
        }

        static void Save(int level) { PlayerPrefs.SetInt(SaveKey(level), 1); PlayerPrefs.Save(); }

        // Playing, or paused over the level: a pause or a popup hides the lesson for a while instead of ending it.
        static bool Live(LevelManager lm) => lm.gameStatus == GameState.Playing || lm.gameStatus == GameState.Pause;
        bool Covered(LevelManager lm)
        {
            bool covered = lm.gameStatus != GameState.Playing || (MenuManager.Instance && MenuManager.Instance.BlocksBoardInput);
            if (root && root.gameObject.activeSelf == covered) root.gameObject.SetActive(!covered);
            return covered;
        }

        // ---------------------------------------------------------------- overlay: caption and hand (no input blocking)

        RectTransform root, panel, hand; Text caption; Image icon, handImage; Font font; bool captionTop;

        void Build()
        {
            if (root) { root.gameObject.SetActive(true); return; }
            var host = new GameObject("Royal power tutorial overlay", typeof(RectTransform));
            host.transform.SetParent(transform, false); root = (RectTransform)host.transform;
            var canvas = host.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 30;
            var scaler = host.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(800, 1280); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; scaler.matchWidthOrHeight = 0;
            var group = host.AddComponent<CanvasGroup>(); group.blocksRaycasts = false; group.interactable = false;
            font = Resources.Load<Font>("RoyalGameplay/Display") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            panel = Node("Caption", root, new Vector2(680, 112));
            var rim = panel.gameObject.AddComponent<RoyalThrone.Presentation.RoyalPanelGraphic>();
            rim.topColor = new Color(1, .88f, .50f); rim.bottomColor = new Color(.80f, .52f, .16f); rim.cornerRadius = 30; rim.raycastTarget = false;
            var face = Node("Face", panel, new Vector2(670, 102)).gameObject.AddComponent<RoyalThrone.Presentation.RoyalPanelGraphic>();
            face.topColor = new Color(.16f, .30f, .62f); face.bottomColor = new Color(.07f, .15f, .38f); face.cornerRadius = 26; face.raycastTarget = false;
            icon = Node("Piece", panel, new Vector2(92, 92)).gameObject.AddComponent<Image>(); icon.raycastTarget = false; icon.preserveAspect = true;
            icon.rectTransform.anchoredPosition = new Vector2(-280, 0);
            caption = Node("Text", panel, new Vector2(540, 96)).gameObject.AddComponent<Text>();
            caption.rectTransform.anchoredPosition = new Vector2(42, 0);
            caption.font = font; caption.fontSize = 34; caption.alignment = TextAnchor.MiddleCenter; caption.color = new Color(1, .97f, .88f);
            caption.horizontalOverflow = HorizontalWrapMode.Wrap; caption.verticalOverflow = VerticalWrapMode.Truncate; caption.raycastTarget = false;
            caption.resizeTextForBestFit = true; caption.resizeTextMinSize = 22; caption.resizeTextMaxSize = 34;
            var outline = caption.gameObject.AddComponent<Outline>(); outline.effectColor = new Color(.03f, .07f, .2f, .9f); outline.effectDistance = new Vector2(2, -2);
            panel.gameObject.SetActive(false);

            hand = Node("Hand", root, new Vector2(150, 150)); handImage = hand.gameObject.AddComponent<Image>(); handImage.raycastTarget = false; handImage.preserveAspect = true;
            // The level 1 tutorial's own hand, so every lesson looks alike.
            var authored = FindAnyObjectByType<HandTutorial>(FindObjectsInactive.Include);
            var sr = authored ? authored.GetComponent<SpriteRenderer>() : null; var img = authored ? authored.GetComponent<Image>() : null;
            handImage.sprite = sr && sr.sprite ? sr.sprite : img ? img.sprite : null;
            hand.pivot = new Vector2(.30f, .88f);   // the fingertip sits on the point
            Hand(false);
        }

        RectTransform Node(string name, Transform parent, Vector2 size)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(parent, false);
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(.5f, .5f); r.sizeDelta = size; return r;
        }

        void Caption(string text, Sprite art, bool top)
        {
            Showing = text; caption.text = text; captionTop = top;
            icon.sprite = art; icon.enabled = art;
            // A cascade banner ("BRAVO!") from the move before would sit on top of the caption.
            var banner = LevelManager.THIS ? LevelManager.THIS.GetComponent<RoyalCelebration>() : null; if (banner && banner.IsShowing) banner.Cancel();
            panel.anchoredPosition = CaptionPlace(); panel.gameObject.SetActive(true);
        }

        Vector3? focus;   // where the lesson points

        // Over the board's top rows (as the reference places its lesson captions), or over its bottom rows when the pieces the
        // lesson points at are in the top half: the goals bar and the booster bar stay visible and those pieces are never covered.
        Vector2 CaptionPlace()
        {
            var lm = LevelManager.THIS; var cam = Camera.main;
            var first = lm && lm.maxRows > 1 ? lm.GetSquare(0, 0) : null; var last = lm && lm.maxRows > 1 ? lm.GetSquare(0, lm.maxRows - 1) : null;
            if (!cam || !first || !last) return new Vector2(0, -Half().y + 260);
            // From the squares themselves: the layout moves and scales the field, so its origin alone is not the board.
            float cell = Mathf.Abs(first.transform.position.y - last.transform.position.y) / (lm.maxRows - 1);
            var top = Local(cam.WorldToScreenPoint(first.transform.position + Vector3.up * cell * .5f));
            var bottom = Local(cam.WorldToScreenPoint(last.transform.position + Vector3.down * cell * .5f));
            float middle = (top.y + bottom.y) * .5f;
            bool upper = captionTop || !focus.HasValue || WorldToLocal(focus.Value).y < middle;
            return new Vector2(0, upper ? top.y - 62 : bottom.y + 62);
        }

        Vector2 Half() => root.rect.size * .5f;
        Vector2 Local(Vector3 screen) { RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, null, out var p); return p; }
        Vector2 WorldToLocal(Vector3 world) => Camera.main ? Local(Camera.main.WorldToScreenPoint(world)) : Vector2.zero;

        void Hand(bool on) { if (hand) hand.gameObject.SetActive(on && handImage.sprite); }
        // The canvas lays itself out a frame after it is made, and the board can move with the layout: place the caption each frame.
        void Follow() { if (panel && panel.gameObject.activeSelf) panel.anchoredPosition = CaptionPlace(); }

        // The hand drags from one piece to the other, lifts, and starts again.
        void Swipe(Vector3 from, Vector3 to, float t)
        {
            focus = from; Follow(); Hand(true); if (!hand.gameObject.activeSelf) return;
            float cycle = 1.5f, u = t % cycle / cycle;
            float move = Mathf.Clamp01((u - .15f) / .45f); move = move * move * (3 - 2 * move);
            hand.anchoredPosition = Vector2.Lerp(WorldToLocal(from), WorldToLocal(to), move);
            float alpha = u < .12f ? u / .12f : u > .85f ? (1 - u) / .15f : 1;
            handImage.color = new Color(1, 1, 1, alpha); hand.localScale = Vector3.one * (u < .15f ? Mathf.Lerp(1.08f, 1, u / .15f) : 1);
        }

        void End()
        {
            focus = null; Showing = null; captionTop = false;
            if (panel) panel.gameObject.SetActive(false);
            Hand(false);
            if (root) root.gameObject.SetActive(false);
        }
    }
}
