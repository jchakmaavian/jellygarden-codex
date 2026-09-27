using PlayerPrefs = RoyalThrone.Testing.RoyalSave;
using System.Collections;
using System.Collections.Generic;
using JellyGarden.Scripts.Juice;
using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.GUI.TutorialStuff
{
    /// <summary>
    /// Levels 2 to 5 each teach one power piece, the way level 1 teaches the swap. While the opening board fills, a few
    /// pieces are recoloured so that one swap makes the piece; once the board has settled the in-game hint outline marks that
    /// swap, a hand shows it and a caption names it. When the piece is made the hand taps it. Any other move, or leaving the
    /// level, ends the lesson. A lesson plays on the level's first attempt only (no stars yet) and once per power piece
    /// (saved); never in the reference campaign. Nothing here blocks input: the player can always just play.
    /// </summary>
    public sealed class RoyalPowerTutorial : MonoBehaviour
    {
        enum Kind { Rocket, Tnt, Propeller, ColorBall }
        sealed class Lesson { public int level; public Kind kind; public string make, use; }
        static readonly Lesson[] Lessons =
        {
            new Lesson { level = 2, kind = Kind.Rocket, make = "Match 4 in a row to make a ROCKET!", use = "Tap the Rocket to launch it!" },
            new Lesson { level = 3, kind = Kind.Tnt, make = "Match 5 in an L or T to make TNT!", use = "Tap the TNT to blast it!" },
            new Lesson { level = 4, kind = Kind.Propeller, make = "Match 4 in a square to make a PROPELLER!", use = "Tap the Propeller to send it flying!" },
            new Lesson { level = 5, kind = Kind.ColorBall, make = "Match 5 in a row to make a COLOR BALL!", use = "Swap the Color Ball with a piece to clear its colour!" },
        };

        /// <summary>The caption of the lesson showing now, or null (for checks).</summary>
        public static string Showing { get; private set; }
        /// <summary>How the latest lesson went: where it planted, why it stopped, or that it was shown (for checks).</summary>
        public static string Outcome { get; private set; }
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

        // ---------------------------------------------------------------- planting the move

        struct Cell { public int dr, dc; public Cell(int r, int c) { dr = r; dc = c; } }
        sealed class Shape { public Cell[] same; public Cell gap, mover; }

        static IEnumerable<Shape> Shapes(Kind kind)
        {
            // Offsets are (row, column); rows grow downwards. "same" are the pieces of the target colour already in place, the
            // mover (also that colour) is swapped into the gap. Each shape is tried in its four turns.
            var bases = new List<Shape>();
            if (kind == Kind.Rocket) bases.Add(new Shape { same = new[] { C(0, 0), C(0, 1), C(0, 3) }, gap = C(0, 2), mover = C(1, 2) });
            if (kind == Kind.ColorBall) bases.Add(new Shape { same = new[] { C(0, 0), C(0, 1), C(0, 3), C(0, 4) }, gap = C(0, 2), mover = C(1, 2) });
            if (kind == Kind.Tnt)
            {
                bases.Add(new Shape { same = new[] { C(0, 1), C(0, 2), C(1, 0), C(2, 0) }, gap = C(0, 0), mover = C(-1, 0) });   // L
                bases.Add(new Shape { same = new[] { C(0, -1), C(0, 1), C(1, 0), C(2, 0) }, gap = C(0, 0), mover = C(-1, 0) });  // T
            }
            if (kind == Kind.Propeller) bases.Add(new Shape { same = new[] { C(0, 0), C(0, 1), C(1, 0) }, gap = C(1, 1), mover = C(1, 2) });
            foreach (var b in bases)
                for (int turn = 0; turn < 4; turn++)
                {
                    Cell T(Cell c) { for (int k = 0; k < turn; k++) c = new Cell(c.dc, -c.dr); return c; }
                    var s = new Shape { gap = T(b.gap), mover = T(b.mover), same = new Cell[b.same.Length] };
                    for (int i = 0; i < b.same.Length; i++) s.same[i] = T(b.same[i]);
                    yield return s;
                }
        }
        static Cell C(int r, int c) => new Cell(r, c);

        static bool Plain(Item it) => it && !it.destroying && it.currentType == ItemsTypes.NONE && it.square && it.square.CanGoOut() &&
                                      !RoyalThrone.Campaign.RoyalCampaignBlockers.IsSpecialItem(it);

        // True when some line of three or 2x2 square of one colour stands on the board (it would resolve by itself).
        static bool AnyMatch(LevelManager lm)
        {
            int Colour(int c, int r) { var sq = lm.GetSquare(c, r); var it = sq ? sq.item : null; return Plain(it) ? it.color : -1 - (c + r * 100); }
            for (int r = 0; r < lm.maxRows; r++)
                for (int c = 0; c < lm.maxCols; c++)
                {
                    int k = Colour(c, r); if (k < 0) continue;
                    if (Colour(c + 1, r) == k && Colour(c + 2, r) == k) return true;
                    if (Colour(c, r + 1) == k && Colour(c, r + 2) == k) return true;
                    if (RoyalEffectsProfile.Get && Colour(c + 1, r) == k && Colour(c, r + 1) == k && Colour(c + 1, r + 1) == k) return true;
                }
            return false;
        }

        static bool IsKind(RoyalHintPlan p, Kind kind) => p != null && IsKind(p.LongestRun, p.MatchCells.Count, kind);
        static bool IsKind(int longest, int n, Kind kind)
        {
            switch (kind)
            {
                case Kind.Rocket: return longest == 4 && n == 4;
                case Kind.ColorBall: return longest >= 5 && n == 5;
                case Kind.Tnt: return longest == 3 && n == 5;
                default: return longest < 3 && n == 4;
            }
        }

        // What swapping cells a and b would match, read from the pieces' colours alone: the hint solver refuses pieces that
        // are still falling or hidden, which is every piece while the opening board drops in. Same rules as
        // RoyalHintSolver.EvaluateSwap: lines of three or more through either cell, then one 2x2 square through either
        // cell that shares no line cell.
        static void Evaluate(LevelManager lm, Vector2Int a, Vector2Int b, out int longest, out int cells)
        {
            int At(int c, int r)
            {
                var p = new Vector2Int(c, r); if (p == a) p = b; else if (p == b) p = a;
                var sq = lm.GetSquare(p.x, p.y); var it = sq ? sq.item : null;
                return Plain(it) && sq.CanGoInto() ? it.color : -1 - (c + 100 * r);
            }
            var set = new HashSet<Vector2Int>(); longest = 0;
            foreach (var o in new[] { a, b })
            {
                int k = At(o.x, o.y); if (k < 0) continue;
                foreach (var axis in new[] { Vector2Int.right, Vector2Int.up })
                {
                    var run = new List<Vector2Int> { o };
                    for (int s = -1; s <= 1; s += 2)
                        for (int n = 1; n < 12; n++) { var q = o + axis * (n * s); if (At(q.x, q.y) != k) break; run.Add(q); }
                    if (run.Count >= 3) { longest = Mathf.Max(longest, run.Count); foreach (var q in run) set.Add(q); }
                }
            }
            if (RoyalEffectsProfile.Get)
                foreach (var o in new[] { a, b })
                {
                    int k = At(o.x, o.y); if (k < 0) continue; bool found = false;
                    for (int dy = -1; dy <= 0 && !found; dy++)
                        for (int dx = -1; dx <= 0 && !found; dx++)
                        {
                            var box = new[] { new Vector2Int(o.x + dx, o.y + dy), new Vector2Int(o.x + dx + 1, o.y + dy), new Vector2Int(o.x + dx, o.y + dy + 1), new Vector2Int(o.x + dx + 1, o.y + dy + 1) };
                            bool same = true; foreach (var q in box) if (At(q.x, q.y) != k) same = false;
                            if (!same) continue;
                            found = true; bool shared = false; foreach (var q in box) if (set.Contains(q)) shared = true;
                            if (!shared) foreach (var q in box) set.Add(q);
                        }
                }
            cells = set.Count;
        }

        // Recolours the pieces of one shape near the middle of the board; returns the swap as (mover, gap piece), or false.
        static bool Plant(LevelManager lm, Kind kind, out Item moverOut, out Item gapOut)
        {
            moverOut = gapOut = null;
            int colours = Mathf.Clamp(lm.colorLimit, 3, RoyalGameplayArt.BoardColorCount);
            var anchors = new List<Vector2Int>();
            for (int r = 1; r < lm.maxRows - 1; r++) for (int c = 0; c < lm.maxCols; c++) anchors.Add(new Vector2Int(c, r));
            Vector2 mid = new Vector2((lm.maxCols - 1) * .5f, (lm.maxRows - 1) * .55f);
            anchors.Sort((a, b) => ((Vector2)a - mid).sqrMagnitude.CompareTo(((Vector2)b - mid).sqrMagnitude));
            foreach (var a in anchors)
                foreach (var s in Shapes(kind))
                {
                    Item At(Cell k) { var sq = lm.GetSquare(a.x + k.dc, a.y + k.dr); return sq ? sq.item : null; }
                    var same = new Item[s.same.Length]; bool ok = true;
                    for (int i = 0; i < same.Length && ok; i++) { same[i] = At(s.same[i]); ok = Plain(same[i]); }
                    var gap = At(s.gap); var mover = At(s.mover);
                    if (!ok || !Plain(gap) || !Plain(mover)) continue;
                    // Remember the colours to put everything back if no colour works here.
                    var touched = new List<Item>(same) { gap, mover }; var before = new int[touched.Count];
                    for (int i = 0; i < touched.Count; i++) before[i] = touched[i].color;
                    for (int x = 0; x < colours; x++)
                        for (int y = 0; y < colours; y++)
                        {
                            if (y == x) continue;
                            foreach (var it in same) it.SetColor(x);
                            mover.SetColor(x); gap.SetColor(y);
                            if (AnyMatch(lm)) continue;
                            Evaluate(lm, new Vector2Int(mover.square.col, mover.square.row), new Vector2Int(gap.square.col, gap.square.row), out int longest, out int n);
                            if (IsKind(longest, n, kind)) { moverOut = mover; gapOut = gap; return true; }
                        }
                    for (int i = 0; i < touched.Count; i++) touched[i].SetColor(before[i]);
                }
            return false;
        }

        // ---------------------------------------------------------------- the lesson

        IEnumerator Run(LevelManager lm, Lesson lesson)
        {
            // Plant while the opening board fills, so the recoloured pieces simply arrive in their colours.
            float give = Time.unscaledTime + 8; Item first = null, second = null; bool planted = false;
            while (!planted && Time.unscaledTime < give && lm && lm.currentLevel == lesson.level)
            {
                bool full = lm.maxRows > 0; int pieces = 0;
                for (int r = 0; r < lm.maxRows && full; r++)
                    for (int c = 0; c < lm.maxCols && full; c++) { var sq = lm.GetSquare(c, r); if (sq && sq.CanGoInto() && !sq.item) full = false; if (sq && sq.item) pieces++; }
                if (full && pieces > 0 && lm.gameStatus != GameState.Map)
                {
                    planted = Plant(lm, lesson.kind, out first, out second);
                    if (!planted) { float retry = Time.unscaledTime + .25f; while (Time.unscaledTime < retry) yield return null; }
                    continue;
                }
                yield return null;
            }
            if (!planted) { Outcome = "level " + lesson.level + ": no place to plant the " + lesson.kind; running = null; yield break; }
            Outcome = "level " + lesson.level + ": planted " + lesson.kind + " swap " + first.square.col + "," + first.square.row + " <-> " + second.square.col + "," + second.square.row;
            RoyalHintPlan plan;

            // Wait for play and a still board (the opening fall, the level's start banners).
            float quiet = 0; give = Time.unscaledTime + 20;
            while (quiet < .6f && Time.unscaledTime < give)
            {
                if (!lm || lm.currentLevel != lesson.level || !first || !second || first.destroying || second.destroying) { Outcome += " | lost its pieces"; running = null; yield break; }
                bool still = lm.gameStatus == GameState.Playing && !lm.IsResolvingMatches && !lm.DragBlocked && !first.falling && !second.falling;
                quiet = still ? quiet + Time.unscaledDeltaTime : 0; yield return null;
            }
            if (quiet < .6f || lm.moveID != 0) { Outcome += quiet < .6f ? " | board never settled" : " | a move came first"; running = null; yield break; }
            plan = RoyalHintSolver.EvaluateSwap(lm, first, second);
            if (!IsKind(plan, lesson.kind)) { Outcome += " | the settled board disagrees (" + (plan == null ? "no match" : plan.LongestRun + "/" + plan.MatchCells.Count) + ")"; running = null; yield break; }
            Outcome += " | shown";

            PlayerPrefs.SetInt(SaveKey(lesson.level), 1); PlayerPrefs.Save();
            var ai = AI.THIS; bool tips = ai && ai.allowShowTip; if (ai) ai.allowShowTip = false;
            var presenter = ai ? (ai.GetComponent<RoyalHintPresenter>() ?? ai.gameObject.AddComponent<RoyalHintPresenter>()) : null;
            try
            {
                Build();
                focus = first.transform.position; Caption(lesson.make, lesson.kind);
                int move = lm.moveID; float shown = 0;
                while (lm && Live(lm) && lm.moveID == move && first && second)
                {
                    if (Covered(lm)) { if (presenter) presenter.Hide(); yield return null; continue; }
                    if (presenter && !presenter.IsVisible && !lm.IsResolvingMatches) presenter.Show(RoyalHintSolver.EvaluateSwap(lm, first, second) ?? plan);
                    shown += Time.unscaledDeltaTime;
                    Swipe(first.transform.position, second.transform.position, shown);
                    yield return null;
                }
                if (presenter) presenter.Hide();
                // The move is made: the first caption goes at once, the next one comes when the new piece has landed.
                Hand(false); if (panel) panel.gameObject.SetActive(false); Showing = null;
                if (!lm || lm.moveID == move) yield break;
                // Was it the lesson's swap? Look for the new piece around the gap for a moment.
                var gapCell = new Vector2Int(second.square ? second.square.col : -99, second.square ? second.square.row : -99);
                Item made = null; float wait = Time.unscaledTime + 2.5f; int after = lm.moveID;
                while (!made && Time.unscaledTime < wait && lm.moveID == after && lm.gameStatus == GameState.Playing)
                {
                    foreach (var it in lm.GetItems())
                        if (it && !it.destroying && Matches(it.currentType, lesson.kind) && it.square &&
                            Mathf.Abs(it.square.col - gapCell.x) + Mathf.Abs(it.square.row - gapCell.y) <= 3) { made = it; break; }
                    yield return null;
                }
                if (!made) yield break;
                // Let it land and play its reveal, then show how to use it.
                float still2 = 0; wait = Time.unscaledTime + 4;
                while (still2 < .35f && Time.unscaledTime < wait && made && !made.destroying)
                { still2 = !made.falling && !lm.IsResolvingMatches ? still2 + Time.unscaledDeltaTime : 0; yield return null; }
                if (!made || made.destroying) yield break;
                focus = made.transform.position; Caption(lesson.use, lesson.kind);
                float t = 0; move = lm.moveID;
                while (made && !made.destroying && lm.moveID == move && Live(lm) && t < 15)
                {
                    if (Covered(lm)) { yield return null; continue; }
                    t += Time.unscaledDeltaTime;
                    if (lesson.kind == Kind.ColorBall)
                    {
                        // A Color Ball is swapped: show it going to a neighbouring piece.
                        var n = Neighbour(lm, made); if (n) Swipe(made.transform.position, n.transform.position, t); else Tap(made.transform.position, t);
                    }
                    else Tap(made.transform.position, t);
                    yield return null;
                }
            }
            finally
            {
                if (presenter) presenter.Hide();
                if (ai && tips) ai.allowShowTip = true;
                End(); running = null;
            }
        }

        // Playing, or paused over the level: a pause or a popup hides the lesson for a while instead of ending it.
        static bool Live(LevelManager lm) => lm.gameStatus == GameState.Playing || lm.gameStatus == GameState.Pause;
        bool Covered(LevelManager lm)
        {
            bool covered = lm.gameStatus != GameState.Playing || (MenuManager.Instance && MenuManager.Instance.BlocksBoardInput);
            if (root && root.gameObject.activeSelf == covered) root.gameObject.SetActive(!covered);
            return covered;
        }

        static bool Matches(ItemsTypes t, Kind k) =>
            k == Kind.Rocket ? t == ItemsTypes.HORIZONTAL_STRIPPED || t == ItemsTypes.VERTICAL_STRIPPED :
            k == Kind.Tnt ? t == ItemsTypes.PACKAGE : k == Kind.Propeller ? t == ItemsTypes.HELICOPTER : t == ItemsTypes.BOMB;

        static Item Neighbour(LevelManager lm, Item it)
        {
            foreach (var d in new[] { new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, -1) })
            { var sq = lm.GetSquare(it.square.col + d.x, it.square.row + d.y); if (sq && Plain(sq.item)) return sq.item; }
            return null;
        }

        // ---------------------------------------------------------------- overlay: caption and hand (no input blocking)

        RectTransform root, panel, hand; Text caption; Image icon, handImage; Font font;

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

            hand = Node("Hand", root, new Vector2(150, 150)); handImage = hand.gameObject.AddComponent<Image>(); handImage.raycastTarget = false; handImage.preserveAspect = true;
            // The level 1 tutorial's own hand, so both lessons look alike.
            var authored = FindAnyObjectByType<HandTutorial>(FindObjectsInactive.Include);
            var sr = authored ? authored.GetComponent<SpriteRenderer>() : null; var img = authored ? authored.GetComponent<Image>() : null;
            handImage.sprite = sr && sr.sprite ? sr.sprite : img ? img.sprite : null;
            if (hand.pivot == new Vector2(.5f, .5f)) hand.pivot = new Vector2(.30f, .88f);   // the fingertip sits on the point
            Hand(false);
        }

        RectTransform Node(string name, Transform parent, Vector2 size)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(parent, false);
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(.5f, .5f); r.sizeDelta = size; return r;
        }

        void Caption(string text, Kind kind)
        {
            Showing = text; caption.text = text;
            var type = kind == Kind.Rocket ? ItemsTypes.HORIZONTAL_STRIPPED : kind == Kind.Tnt ? ItemsTypes.PACKAGE : kind == Kind.Propeller ? ItemsTypes.HELICOPTER : ItemsTypes.BOMB;
            icon.sprite = RoyalPieceSkin.For(type, 0); icon.enabled = icon.sprite;
            // A cascade banner ("BRAVO!") from the move before would sit on top of the caption.
            var banner = LevelManager.THIS ? LevelManager.THIS.GetComponent<RoyalCelebration>() : null; if (banner && banner.IsShowing) banner.Cancel();
            panel.anchoredPosition = CaptionPlace(); panel.gameObject.SetActive(true);
        }

        Vector3? focus;   // where the lesson points (the swap's first piece, then the new piece)

        // Over the board's top rows when the lesson's pieces are in its lower half, otherwise over its bottom rows: the goals
        // bar above and the booster bar below stay visible (there is no room between them and a 9-row board), and the
        // pieces the lesson is about are never covered.
        Vector2 CaptionPlace()
        {
            var lm = LevelManager.THIS; var cam = Camera.main;
            var first = lm && lm.maxRows > 1 ? lm.GetSquare(0, 0) : null; var last = lm && lm.maxRows > 1 ? lm.GetSquare(0, lm.maxRows - 1) : null;
            if (!cam || !first || !last) return new Vector2(0, -Half().y + 260);
            // From the squares themselves: the layout moves and scales the field, so its origin alone is not the board.
            float cell = Mathf.Abs(first.transform.position.y - last.transform.position.y) / (lm.maxRows - 1);
            var top = Local(cam.WorldToScreenPoint(first.transform.position + Vector3.up * cell * .5f));
            var bottom = Local(cam.WorldToScreenPoint(last.transform.position + Vector3.down * cell * .5f));
            float middle = (top.y + bottom.y) * .5f, at = focus.HasValue ? WorldToLocal(focus.Value).y : middle;
            return new Vector2(0, at < middle ? top.y - 62 : bottom.y + 62);
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

        // The hand presses the piece, twice a second or so.
        void Tap(Vector3 at, float t)
        {
            focus = at; Follow(); Hand(true); if (!hand.gameObject.activeSelf) return;
            float u = t % 1.1f / 1.1f, press = u < .25f ? u / .25f : u < .45f ? 1 - (u - .25f) / .2f : 0;
            hand.anchoredPosition = WorldToLocal(at) + new Vector2(0, -10 * press);
            hand.localScale = Vector3.one * (1 - .12f * press); handImage.color = Color.white;
        }

        void End()
        {
            focus = null;
            Showing = null;
            if (root) root.gameObject.SetActive(false);
        }
    }
}
