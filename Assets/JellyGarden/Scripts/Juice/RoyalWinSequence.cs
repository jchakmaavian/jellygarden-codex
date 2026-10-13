using PlayerPrefs = RoyalThrone.Testing.RoyalSave;
using System.Collections;
using System.Collections.Generic;
using JellyGarden.Scripts.GUI;
using UnityEngine;
using UnityEngine.UI;
using RoyalThrone.ReferenceGameplay;

namespace JellyGarden.Scripts.Juice
{
    /// <summary>
    /// The end of a won level, staged after the user's Royal Match Level 13 clip (Design/RoyalMatchReference/OBSERVATIONS.md,
    /// "Level 13 completion"; the user, 2026-10-06: "I want this game experience"), in Royal Throne's own art:
    /// 1. <see cref="Celebrate"/> as soon as the goals are done, before the moves left are used: the board dims below the HUD,
    ///    the logo is built (emblem, ROYAL letter by letter, THRONE), pennants unroll, trumpets swing in and fireworks rise
    ///    and burst; the logo swells and shrinks to a point and the dim is cut (about 3 s).
    /// 2. <see cref="BeginFinale"/> on that frame: the HUD's goal panel turns into a Reward coin counter; the booster
    ///    session (ReferenceWinFinish) turns the moves left into rockets and fires them, the board refills, and coins pop out
    ///    of each booster into the counter; <see cref="EndFinale"/> lands the rest and lets the board rest a moment.
    /// 3. <see cref="Reward"/> on the win card: one large star lands on its pillow; the committed award waits for Home.
    /// 4. After CONTINUE, on Home: the scene alone, the controls slide in, the star curves into its counter and the coins
    ///    stream into theirs, and the Area task list opens if its next task is affordable.
    /// The level music stops with the celebration and comes back softly on the card (RoyalAudioDirector).
    /// Presentation only: LevelManager credits the coins and stars before the card opens. Nothing here takes input. A tap
    /// shortens the celebration and the boosters (LevelManager.SkipWinCelebration).
    /// </summary>
    public sealed class RoyalWinSequence : MonoBehaviour
    {
        // The user's decision (2026-09-27): a win pays 10 coins plus 5 per star. (The reference's own amounts vary with
        // the finale and were not inferred.)
        // The reward is decided once, when the goals are met (LevelManager.CommitWin, RoyalEconomy.WinCoins: 50 plus 5 per
        // move left, up to 100). The finale spends the moves left, so its coin flights read the committed amount.
        public static int Coins(LevelManager lm) => lm && lm.CommittedCoins > 0 ? lm.CommittedCoins : RoyalEconomy.WinCoins(lm);

        static RoyalWinSequence instance;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetStatics() { instance = null; }
        static RoyalWinSequence Get(LevelManager lm)
        {
            if (!instance) instance = lm.gameObject.AddComponent<RoyalWinSequence>();
            instance.Build(); return instance;
        }
        /// <summary>For checks: the Reward counter's value during the finale, and whether Home rewards are waiting or running.</summary>
        public static int RewardShown => instance ? instance.rewardShown : 0;
        public static bool HomeRewardsPending => instance && (instance.pending != null || instance.homeRunning);
        public static int HomeCoinsShown => instance ? instance.homeCoins : 0;
        public static int HomeRewardStarsInFlight => instance && instance.homeRunning ? instance.flights.FindAll(f => !f.turning && !f.landed).Count : 0;
        public static int HomeRewardCoinsInFlight => instance && instance.homeRunning ? instance.flights.FindAll(f => f.turning && !f.landed).Count : 0;
        // Ten representative coins, equal integer increments, remainder on the last arrival.
        // This changes the displayed balance only; LevelManager saves the award once.
        public static int CoinAmountAtArrival(int total, int arrived, int count)
        {
            total = Mathf.Max(0, total);
            if (count <= 0 || arrived <= 0) return 0;
            return arrived >= count ? total : (total / count) * arrived;
        }

        // ---------------------------------------------------------------- overlay

        RectTransform root; Font font; Sprite sparkle, star, glow, ring, piece, soft, coin, pennant, logo;
        // The celebration's art (Resources/RoyalGameplay/Win; Design/RoyalMatchReference/level13_completion/art).
        Sprite[] logoPart; Sprite trumpet, flagArt, flagRoll;
        bool built, reduced;

        static readonly Color[] Confetti =
        {
            new Color(1f, .83f, .30f), new Color(.30f, .52f, 1f), new Color(1f, .96f, .84f), new Color(.96f, .32f, .36f),
            new Color(.22f, .80f, .70f), new Color(.98f, .62f, .18f)
        };
        static readonly Color Gold = new Color(1f, .86f, .42f), Navy = new Color(.06f, .13f, .32f);

        void Build()
        {
            if (built && root) return; built = true;
            var host = new GameObject("Royal win sequence - no input blocking", typeof(RectTransform));
            host.transform.SetParent(transform, false); root = (RectTransform)host.transform;
            var canvas = host.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 35;
            // The popup canvas's units (800 wide, width-matched), so the coins line up with the win card.
            var scaler = host.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(800, 1280); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; scaler.matchWidthOrHeight = 0;
            var group = host.AddComponent<CanvasGroup>(); group.blocksRaycasts = false; group.interactable = false;
            font = Resources.Load<Font>("RoyalGameplay/Display") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            sparkle = Shape(SparkleTextureFactory.FourPointSparkle); star = Shape(SparkleTextureFactory.SixPointStar);
            glow = Shape(SparkleTextureFactory.SoftGlow); ring = Shape(SparkleTextureFactory.Ring);
            var white = new Texture2D(4, 4); var px = new Color[16]; for (int i = 0; i < 16; i++) px[i] = Color.white; white.SetPixels(px); white.Apply();
            piece = Sprite.Create(white, new Rect(0, 0, 4, 4), new Vector2(.5f, .5f), 4);
            // A soft round spot, opaque at the centre (the shared SoftGlow is mostly transparent): the logo's halo and the
            // flash at a firework's heart.
            const int N = 64; var spot = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var sp = new Color[N * N];
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
            {
                float dx = (x + .5f) / N * 2 - 1, dy = (y + .5f) / N * 2 - 1, r = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                sp[y * N + x] = new Color(1, 1, 1, Mathf.Pow(1 - r * r, 1.6f));
            }
            spot.SetPixels(sp); spot.Apply(); soft = Sprite.Create(spot, new Rect(0, 0, N, N), new Vector2(.5f, .5f), N);
            pennant = MakePennant();
            // The approved Royal Throne logo (Design/Branding): the game scene references it, so it is loaded with the scene.
            foreach (var s in Resources.FindObjectsOfTypeAll<Sprite>())
                if (s && s.name == "logo" && s.texture && s.texture.width >= 512) { logo = s; break; }
            logoPart = new Sprite[LogoParts.Length];
            for (int i = 0; i < LogoParts.Length; i++) logoPart[i] = Resources.Load<Sprite>("RoyalGameplay/Win/win-logo-" + LogoParts[i]);
            trumpet = Resources.Load<Sprite>("RoyalGameplay/Win/win-trumpet");
            flagArt = Resources.Load<Sprite>("RoyalGameplay/Win/win-pennant");
            flagRoll = Resources.Load<Sprite>("RoyalGameplay/Win/win-pennant-roll");
        }
        static Sprite Shape(Texture2D t) => Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(.5f, .5f), t.width);

        // A hanging swallow-tail pennant: royal blue, with a gold hem and a gold diamond, drawn here (no image asset).
        static Sprite MakePennant()
        {
            const int W = 64, H = 160; var t = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp }; var c = new Color[W * H];
            Color blueTop = new Color(.18f, .38f, .92f), blueBottom = new Color(.08f, .20f, .62f), gold = new Color(1f, .84f, .36f), dark = new Color(.05f, .12f, .40f);
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float u = (x + .5f) / W, v = 1 - (y + .5f) / H;            // v = 0 at the top edge, 1 at the tail
                    float notch = v > .78f ? (v - .78f) / .22f * .5f : 0;         // the swallow tail's V
                    bool inside = Mathf.Abs(u - .5f) >= notch && u > .04f && u < .96f;
                    if (!inside) { c[y * W + x] = new Color(0, 0, 0, 0); continue; }
                    var col = Color.Lerp(blueTop, blueBottom, v);
                    float edge = Mathf.Min(u - .04f, .96f - u);
                    if (edge < .07f || v < .06f) col = gold; else if (edge < .10f) col = dark;
                    float dy = Mathf.Abs(v - .42f) / .11f, dx = Mathf.Abs(u - .5f) / .18f;
                    if (dx + dy < 1) col = Color.Lerp(gold, Color.white, (1 - dx - dy) * .35f);
                    c[y * W + x] = col;
                }
            t.SetPixels(c); t.Apply();
            return Sprite.Create(t, new Rect(0, 0, W, H), new Vector2(.5f, 1f), 100);   // pivot at the top: it hangs
        }

        RectTransform Node(string name, Transform parent, Vector2 pos, Vector2 size)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(parent, false); r.anchorMin = r.anchorMax = r.pivot = new Vector2(.5f, .5f); r.anchoredPosition = pos; r.sizeDelta = size; return r;
        }
        Image Picture(string name, Transform parent, Sprite s, Vector2 pos, Vector2 size, Color c)
        { var i = Node(name, parent, pos, size).gameObject.AddComponent<Image>(); i.sprite = s; i.color = c; i.raycastTarget = false; i.preserveAspect = true; return i; }
        Text Words(string name, Transform parent, string text, int size, Vector2 pos, Vector2 box, Color c, float outline)
        {
            var t = Node(name, parent, pos, box).gameObject.AddComponent<Text>();
            t.font = font; t.fontSize = size; t.text = text; t.alignment = TextAnchor.MiddleCenter; t.color = c; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
            var sh = t.gameObject.AddComponent<Shadow>(); sh.effectColor = new Color(.03f, .07f, .20f, .85f); sh.effectDistance = new Vector2(0, -outline * 1.6f);
            if (outline > 0) { var o = t.gameObject.AddComponent<Outline>(); o.effectColor = Navy; o.effectDistance = new Vector2(outline, -outline); }
            return t;
        }
        // A UI element's position in overlay units. The popup canvas (CanvasGlobal) renders through a camera, so its
        // elements go to the screen through that camera; an overlay canvas's positions are already in pixels.
        Vector2 Local(Transform t)
        {
            return LocalPoint(t, t.position);
        }
        Vector2 LocalPoint(Transform owner, Vector3 world)
        {
            var canvas = owner.GetComponentInParent<Canvas>(); canvas = canvas ? canvas.rootCanvas : null;
            Camera cam = null;
            if (canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay) cam = canvas.worldCamera ? canvas.worldCamera : Camera.main;
            var screen = RectTransformUtility.WorldToScreenPoint(cam, world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, null, out var local); return local;
        }
        Vector2 World(Vector3 world)
        {
            var cam = Camera.main; if (!cam) return Vector2.zero;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, cam.WorldToScreenPoint(world), null, out var local); return local;
        }
        // From the screen, not root.rect: the canvas is always 800 units wide (width-matched), and an overlay made this frame
        // has not been scaled yet (its rect is still the screen's pixels), which put the pennants 1,300 px above the screen.
        Vector2 Half => new Vector2(400, 400f * Screen.height / Mathf.Max(1, Screen.width));
        Vector2 Size => Half * 2;
        // Waits (at most half a second) until the CanvasScaler has applied its scale, so screen-to-overlay positions are right.
        IEnumerator Scaled()
        {
            float want = Screen.width / 800f, until = Time.unscaledTime + .5f;
            while (Mathf.Abs(root.localScale.x - want) > .01f && Time.unscaledTime < until) yield return null;
        }
        static float EaseOut(float t) { t = Mathf.Clamp01(t); return 1 - (1 - t) * (1 - t) * (1 - t); }
        static float EaseInOut(float t) { t = Mathf.Clamp01(t); return t * t * (3 - 2 * t); }

        // ---------------------------------------------------------------- particles (confetti, sparkles, rings)

        sealed class Bit
        {
            public Image image; public RectTransform rect; public Vector2 pos, vel; public Color color;
            public float age, life, spin, angle, flip, flipSpeed, gravity, drag, size, grow, fade; public int kind;
        }
        readonly List<Bit> bits = new List<Bit>();
        const int MaxBits = 360;

        Bit Spawn(int kind, Vector2 pos, Vector2 vel, float life, float size, Color color)
        {
            Bit b = null;
            foreach (var x in bits) if (x.age >= x.life) { b = x; break; }
            if (b == null)
            {
                if (bits.Count >= MaxBits) return null;
                b = new Bit(); b.image = Picture("Win bit", root, piece, Vector2.zero, Vector2.one, Color.white); b.rect = b.image.rectTransform; bits.Add(b);
            }
            b.kind = kind; b.pos = pos; b.vel = vel; b.life = life; b.age = 0; b.size = size; b.color = color; b.angle = Random.Range(0, 360f);
            b.spin = Random.Range(-540f, 540f); b.flip = Random.Range(0, 6.3f); b.flipSpeed = Random.Range(5f, 11f); b.grow = 0; b.fade = 2;
            b.gravity = kind == 0 ? 1350 : kind == 1 ? 180 : 0; b.drag = kind == 0 ? .55f : 1.8f;
            b.image.sprite = kind == 0 ? piece : kind == 1 ? (Random.value < .35f ? star : sparkle) : kind == 2 ? ring : soft;
            b.image.preserveAspect = kind != 0; b.image.enabled = true; b.rect.SetAsLastSibling();
            b.rect.sizeDelta = kind == 0 ? new Vector2(size * .62f, size) : Vector2.one * size;
            return b;
        }

        void ConfettiBurst(Vector2 from, float angle, float spread, int count)
        {
            if (reduced) count = Mathf.Max(6, count / 4);
            for (int i = 0; i < count; i++)
            {
                float a = (angle + Random.Range(-spread, spread)) * Mathf.Deg2Rad, speed = Random.Range(900f, 1650f);
                Spawn(0, from, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * speed, Random.Range(1.7f, 2.5f), Random.Range(20f, 34f), Confetti[i % Confetti.Length]);
            }
        }
        // A firework on the overlay: a bright flash, a ring, and a wide spray of sparks that slow, sink and linger.
        // `shower`: the celebration's kind, a spray of sparks with no ring, as the reference's fireworks are.
        void FireworkBurst(Vector2 at, Color color, float scale = 1, bool shower = false)
        {
            var core = Spawn(3, at, Vector2.zero, .34f, 120 * scale * (shower ? .6f : 1), new Color(1, .97f, .86f)); if (core != null) core.grow = 170 * scale * (shower ? .6f : 1);
            if (!shower) RingFlash(at, 220 * scale, color);
            else if (!reduced)
                for (int i = 0; i < 14; i++)
                {
                    // An inner shell of slower sparks fills the burst's heart.
                    float a = Random.value * Mathf.PI * 2, v = 380 * scale * Random.Range(.2f, .7f);
                    var b = Spawn(1, at, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * v, Random.Range(.6f, .95f), Random.Range(18f, 30f), Color.Lerp(color, Color.white, .4f));
                    if (b != null) { b.fade = .9f; b.gravity = 220; b.drag = 1.8f; }
                }
            int count = reduced ? 8 : Mathf.RoundToInt(26 * scale * (shower ? 1.4f : 1));
            for (int i = 0; i < count; i++)
            {
                float a = (i + Random.value * .5f) * Mathf.PI * 2 / count, v = 700 * scale * Random.Range(.6f, 1f);
                var b = Spawn(1, at, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * v, Random.Range(.75f, 1.05f), Random.Range(30f, 42f) * (shower ? 1.4f : 1), i % 3 == 0 ? Color.white : color);
                if (b != null) { b.fade = .7f; b.gravity = 300; b.drag = 1.5f; }
            }
        }
        void SparkleBurst(Vector2 at, int count, float speed, Color color, float size = 22)
        {
            if (reduced) count = Mathf.Max(2, count / 3);
            for (int i = 0; i < count; i++)
            {
                float a = (i + Random.value * .6f) * Mathf.PI * 2 / count, v = speed * Random.Range(.55f, 1f);
                Spawn(1, at, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * v, Random.Range(.45f, .75f), size * Random.Range(.7f, 1.2f), color);
            }
        }
        void RingFlash(Vector2 at, float size, Color color)
        { var b = Spawn(2, at, Vector2.zero, .38f, size * .4f, color); if (b != null) b.grow = size * 1.6f; }

        void Update()
        {
            if (!root) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, .05f);
            foreach (var b in bits)
            {
                if (b.age >= b.life) { if (b.image.enabled) b.image.enabled = false; continue; }
                b.age += dt; float u = b.age / b.life;
                b.vel.y -= b.gravity * dt; b.vel *= 1 - Mathf.Min(1, b.drag * dt); b.pos += b.vel * dt;
                if (b.kind == 0)
                {
                    // Tumbling paper: spin in the plane and flip about its own axis, drifting as it falls.
                    b.angle += b.spin * dt; b.flip += b.flipSpeed * dt; b.pos.x += Mathf.Sin(b.flip * .7f) * 38 * dt;
                    b.rect.localScale = new Vector3(Mathf.Cos(b.flip), 1, 1);
                    b.image.color = new Color(b.color.r * (.78f + .22f * Mathf.Abs(Mathf.Cos(b.flip))), b.color.g * (.78f + .22f * Mathf.Abs(Mathf.Cos(b.flip))), b.color.b, 1 - Mathf.Clamp01((u - .78f) / .22f));
                }
                else if (b.kind == 1)
                {
                    b.angle += b.spin * .25f * dt; b.rect.localScale = Vector3.one * (1 - u * .5f);
                    b.image.color = new Color(b.color.r, b.color.g, b.color.b, Mathf.Pow(1 - u, b.fade));
                }
                else
                {
                    // A ring (2) or a flash (3) opening out as it fades.
                    b.rect.sizeDelta = Vector2.one * (b.size + b.grow * EaseOut(u)); b.rect.localScale = Vector3.one;
                    b.image.color = new Color(b.color.r, b.color.g, b.color.b, b.kind == 2 ? .8f * (1 - u) : .95f * (1 - u) * (1 - u));
                }
                b.rect.anchoredPosition = b.pos; b.rect.localRotation = Quaternion.Euler(0, 0, b.angle);
                if (b.age >= b.life) b.image.enabled = false;
            }
            UpdateFlights(dt);
            UpdateCoinPulses(dt);
            if (pillFlash) { flash = Mathf.Max(0, flash - dt * 3.2f); pillFlash.color = new Color(1, .92f, .6f, .55f * flash); if (pill) pill.localScale = Vector3.one * (1 + .035f * flash); }
            UpdateFinale();
            var lm = LevelManager.THIS;
            if (pending != null && !homeRunning && lm && lm.gameStatus == GameState.Map)
            {
                // Home's first frame already shows the area scene alone; the controls slide in when the payout starts.
                HideHomeControls();
                // A directly opened/reopened win card can cover Home. Collection starts only after that card closes.
                var complete = MenuManager.Instance ? MenuManager.Instance.GetCanvasGlobal().Find("MenuComplete") : null;
                if (!complete || !complete.gameObject.activeInHierarchy) homeReward = StartCoroutine(HomeRewards(lm));
            }
        }

        // ---------------------------------------------------------------- coin (and star) flights

        sealed class Flight
        {
            public Image image; public Vector2 from, burst, control, to;
            public float start, fly, size, hold; public global::System.Action land;
            public Transform receiver;
            public RectTransform cardFrame;
            public bool landed, turning, started, trail;
            public ReferenceCoinParticles.Handle tracer;
        }
        readonly List<Flight> flights = new List<Flight>();
        readonly Stack<Image> coinImages = new Stack<Image>();
        readonly global::System.Random coinRandom = new global::System.Random(6019);
        float nextCoinContactSound, nextCoinAppearSound;
        int previewGeneration;
        sealed class CoinPulse { public RectTransform rect; public Vector3 rest; public float age; }
        readonly List<CoinPulse> coinPulses = new List<CoinPulse>();
        void PulseCoin(RectTransform rect)
        {
            if (!rect) return;
            var pulse = coinPulses.Find(p => p.rect == rect);
            if (pulse == null) { pulse = new CoinPulse { rect = rect, rest = rect.localScale }; coinPulses.Add(pulse); }
            pulse.age = 0;
        }
        void UpdateCoinPulses(float dt)
        {
            for (int i = coinPulses.Count - 1; i >= 0; i--)
            {
                var p = coinPulses[i]; p.age += dt;
                // Serialized CoinInfoView counter response: 1 -> 1.05 (.08s) -> .98 (.13s) -> 1 (.06s).
                float factor = p.age < .08f ? Mathf.Lerp(1, 1.05f, EaseOut(p.age / .08f)) : p.age < .21f ? Mathf.Lerp(1.05f, .98f, EaseInOut((p.age - .08f) / .13f)) : Mathf.Lerp(.98f, 1, EaseOut((p.age - .21f) / .06f));
                if (p.rect) p.rect.localScale = p.rest * factor;
                if (!p.rect || p.age >= .27f) { if (p.rect) p.rect.localScale = p.rest; coinPulses.RemoveAt(i); }
            }
        }
        public static int ActiveCoinFlights => instance ? instance.flights.FindAll(f => f.turning && !f.landed).Count : 0;

        void Fly(Sprite art, Vector2 from, Vector2 to, float delay, float size, float fly, global::System.Action land, bool turning = true, float spread = 120, float hold = 0, bool trail = true, Transform receiver = null)
        {
            // Presentation must not consume the board's random sequence during its refill.
            float a = turning ? (float)coinRandom.NextDouble() * Mathf.PI * 2 : Random.Range(0, Mathf.PI * 2);
            float r = turning ? Mathf.Lerp(spread * .4f, spread, (float)coinRandom.NextDouble()) : Random.Range(spread * .4f, spread);
            var f = new Flight { from = from, burst = from + new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r * .75f), to = to, start = Time.unscaledTime + delay, fly = Mathf.Max(.01f, fly), land = land, turning = turning, size = size, hold = hold, trail = trail, receiver = receiver };
            // Native metadata verifies the final .6s accelerating vector; control points are reconstructed
            // from the observed outward pop / curved collection, not recovered IL2CPP method bodies.
            f.control = turning
                ? new Vector2(Mathf.Lerp(f.burst.x, to.x, .24f) - Mathf.Sign(f.burst.x - to.x) * 55, Mathf.Lerp(f.burst.y, to.y, .35f))
                : new Vector2(Mathf.Lerp(f.burst.x, to.x, .2f) + Random.Range(-160f, 160f), Mathf.Lerp(f.burst.y, to.y, .55f));
            while (coinImages.Count > 0 && !f.image) f.image = coinImages.Pop();
            if (!f.image) f.image = Picture("Flying reward", root, art, from, new Vector2(size, size), Color.clear);
            f.image.gameObject.SetActive(true); f.image.sprite = art; f.image.color = Color.clear;
            f.image.rectTransform.localScale = Vector3.one; f.image.rectTransform.localRotation = Quaternion.identity;
            f.image.rectTransform.anchoredPosition = from; f.image.rectTransform.sizeDelta = Vector2.one * size;
            f.image.rectTransform.SetAsLastSibling(); flights.Add(f);
        }

        void UpdateFlights(float dt)
        {
            if (flights.Count == 0) return; float now = Time.unscaledTime;
            for (int k = flights.Count - 1; k >= 0; k--)
            {
                var f = flights[k];
                if (f.landed || !f.image) { RetireFlight(f); flights.RemoveAt(k); continue; }
                if (f.cardFrame && !f.cardFrame.gameObject.activeInHierarchy)
                { RetireFlight(f); flights.RemoveAt(k); continue; }
                if (f.receiver) f.to = Local(f.receiver);
                float e = now - f.start; if (e < 0) continue;
                float burstTime = (f.turning ? .55f : .2f) + f.hold;
                if (!f.started)
                {
                    f.started = true;
                    if (f.turning && now >= nextCoinAppearSound)
                    { ReferenceGameplayAudio.Play("coin_appear__rfv387_17675", .48f); nextCoinAppearSound = now + .75f; }
                }
                Vector2 p; float scale;
                if (e < burstTime) { float u = EaseOut(e / (f.turning ? .45f : .2f)); p = Vector2.Lerp(f.from, f.burst, u); scale = Mathf.Lerp(.55f, 1, u); f.image.color = new Color(1, 1, 1, Mathf.Clamp01(e / .06f)); }
                else
                {
                    float u = Mathf.Clamp01((e - burstTime) / f.fly), w = u * u;   // accelerates into its counter
                    p = (1 - w) * (1 - w) * f.burst + 2 * (1 - w) * w * f.control + w * w * f.to; scale = Mathf.Lerp(1, .72f, u);
                    if (f.turning)
                    {
                        if (!reduced && f.trail && f.tracer == null) f.tracer = ReferenceCoinParticles.Play("RemainingMovesCoinTracer", root, f.burst, f.size);
                        f.tracer?.SetPosition(p);
                    }
                    else if (!reduced && Random.value < .45f) Spawn(1, p, Vector2.zero, .32f, 15, new Color(1, .9f, .5f));
                    if (u >= 1)
                    {
                        f.landed = true; f.tracer?.Stop(); f.land?.Invoke();
                        if (f.turning)
                        {
                            ReferenceCoinParticles.Play(f.trail ? "CoinHitParticles" : "CoinCollectHitParticles", root, f.to, f.size);
                            if (now >= nextCoinContactSound) { ReferenceGameplayAudio.Play("coin_collect__15714", .68f); nextCoinContactSound = now + .07f; }
                        }
                        else SparkleBurst(f.to, 4, 170, Gold, 18);
                        f.image.gameObject.SetActive(false); continue;
                    }
                }
                f.image.rectTransform.localScale = Vector3.one * scale;
                if (!f.turning && f.hold > 0)
                {
                    // The Home star pauses and gently rocks before collection. Other star flights retain their zero hold.
                    float rock = e < burstTime ? Mathf.Sin(Mathf.Clamp01((e - .2f) / f.hold) * Mathf.PI * 2) * 5 : 0;
                    f.image.rectTransform.localRotation = Quaternion.Euler(0, 0, rock);
                }
                if (f.turning) ReferenceCoinAnimation.Apply(f.image, reduced ? 0 : e, f.size);
                f.image.rectTransform.anchoredPosition = f.cardFrame ? ContainCardCoin(f, p) : p;
            }
        }

        readonly Vector3[] cardCoinCorners = new Vector3[4];
        Rect CardCoinBounds(RectTransform frame)
        {
            // The popup and reward overlay use different canvases. Convert through screen space rather than assuming
            // equal scales, including while the popup is fitting to a phone/tablet or completing its entrance.
            frame.GetWorldCorners(cardCoinCorners);
            Vector2 lo = LocalPoint(frame, cardCoinCorners[0]), hi = lo;
            for (int i = 1; i < cardCoinCorners.Length; i++)
            {
                var point = LocalPoint(frame, cardCoinCorners[i]);
                lo = Vector2.Min(lo, point); hi = Vector2.Max(hi, point);
            }
            return Rect.MinMaxRect(lo.x, lo.y, hi.x, hi.y);
        }
        Vector2 ContainCardCoin(Flight flight, Vector2 position)
        {
            var bounds = CardCoinBounds(flight.cardFrame);
            float inset = 24 * bounds.width / Mathf.Max(1, flight.cardFrame.rect.width);
            var rect = flight.image.rectTransform;
            // Apply the recovered flip frame first: its aspect ratio and negative X scale can change every frame.
            float halfW = Mathf.Abs(rect.rect.width * rect.localScale.x) * .5f;
            float halfH = Mathf.Abs(rect.rect.height * rect.localScale.y) * .5f;
            position.x = Mathf.Clamp(position.x, bounds.xMin + inset + halfW, bounds.xMax - inset - halfW);
            position.y = Mathf.Clamp(position.y, bounds.yMin + inset + halfH, bounds.yMax - inset - halfH);
            return position;
        }
        void RetireFlight(Flight f)
        {
            f.tracer?.Stop(); f.tracer = null; f.land = null;
            if (!f.image) return;
            f.image.gameObject.SetActive(false); f.image.rectTransform.localScale = Vector3.one;
            if (coinImages.Count < 40) coinImages.Push(f.image); else Destroy(f.image.gameObject);
            f.image = null;
        }
        void ClearFlights()
        {
            previewGeneration++;
            foreach (var f in flights) RetireFlight(f); flights.Clear();
            ReferenceCoinParticles.Clear(root); nextCoinContactSound = nextCoinAppearSound = 0;
            foreach (var p in coinPulses) if (p.rect) p.rect.localScale = p.rest; coinPulses.Clear();
            // During scene shutdown Get would create a new audio object merely to stop it.
            // Retire only already-existing coin voices, leaving music and other cues alone.
            var audio = FindAnyObjectByType<RoyalAudioDirector>();
            if (audio) foreach (var name in new[] { "coin_appear__rfv387_17675", "coin_counter__rfv387_17638", "coin_collect__15714" })
                audio.StopClip(ReferenceGameplayAudio.Load(name));
        }

        /// <summary>QA uses the actual reward renderer. Currency is never awarded by this presentation.</summary>
        public static IEnumerator PreviewCoins(Vector2 from, Vector2 to, bool boardTrail, global::System.Action<int> onArrival = null)
        {
            var lm = LevelManager.THIS; if (!lm) yield break;
            var s = Get(lm); s.ClearFlights(); int generation = s.previewGeneration; yield return s.Scaled();
            if (!s || generation != s.previewGeneration) yield break;
            s.reduced = false; int landed = 0; s.coin = s.coin ? s.coin : RoyalPieceSkin.Art("coin");
            for (int i = 0; i < 10; i++)
                s.Fly(s.coin, from, to, .25f + i * .02f, 70, .6f, () =>
                {
                    landed++; onArrival?.Invoke(landed);
                    if (landed == 1) ReferenceGameplayAudio.Play("coin_counter__rfv387_17638", .55f);
                }, true, boardTrail ? 100 : 48, i * .09f, boardTrail);
            float until = Time.unscaledTime + 4.8f;
            while (s.flights.Count > 0 && Time.unscaledTime < until) yield return null;
        }
        public static void CancelCoinPreview() { if (instance) instance.ClearFlights(); }

        // ---------------------------------------------------------------- 1. the celebration, when the goals are done

        // The celebration plays on by itself. The level's coroutine moves on to the booster session at the hand-over: the frame
        // the dim is cut, which is the frame the reference turns its Target panel into Reward.
        bool celebrationHandover;
        public static IEnumerator Celebrate(LevelManager lm)
        {
            var s = Get(lm); s.celebrationHandover = false;
            s.StartCoroutine(s.RunCelebration(lm));
            float until = Time.unscaledTime + 7;
            while (!s.celebrationHandover && s && s.isActiveAndEnabled && lm.gameStatus == GameState.PreWinAnimations && Time.unscaledTime < until) yield return null;
        }

        // The logo's layers (Resources/RoyalGameplay/Win), cut from the approved logo by
        // Design/RoyalMatchReference/level13_completion/art/logo_layers.py: the emblem (crown, throne back and frame, with an
        // empty velvet band), the letters of ROYAL, and the second line THRONE with its ornament. Centre x, y and size w, h are
        // fractions of the logo's width, y up. Together they recompose the logo exactly.
        static readonly string[] LogoParts = { "emblem", "r", "o", "y", "a", "l", "throne" };
        static readonly Vector4[] LogoLayout =
        {
            new Vector4(-.0008f, .18341f, .89155f, .50558f), new Vector4(-.25478f, .02073f, .20335f, .21372f),
            new Vector4(-.11284f, .02113f, .17624f, .19219f), new Vector4(.01754f, .02512f, .17544f, .19378f),
            new Vector4(.12839f, .01715f, .17544f, .19856f), new Vector4(.25758f, .01994f, .18501f, .2201f),
            new Vector4(.0004f, -.21691f, .94498f, .32217f)
        };
        // When each layer pops in (s after the dim starts): the emblem widens in, ROYAL is written a letter every 0.06 s, and
        // THRONE lands as one word with the larger overshoot, as the reference writes "ROYAL" and then lands "MATCH!".
        static readonly float[] LogoIn = { .20f, .30f, .36f, .42f, .48f, .54f, .62f };
        // The four herald trumpets: the bell's centre (fractions of the half screen), the angle the bell points (degrees up
        // from the side it comes from) and a size. Two come from each lower edge, bells up and in, as in the reference.
        static readonly Vector4[] Horns =
        {
            new Vector4(-.51f, -.23f, 37, 1f), new Vector4(-.26f, -.385f, 31, .94f),
            new Vector4(.51f, -.23f, 37, 1f), new Vector4(.26f, -.385f, 31, .94f)
        };
        // Firework shells: when, where (fractions of the half screen) and the colour. The first three are seen rising from the
        // bottom centre first, as the reference's are; the rest burst on their own around the logo.
        static readonly Vector4[] Shells =
        {
            new Vector4(1.55f, -.45f, .40f, 0), new Vector4(1.70f, .50f, .34f, 1), new Vector4(1.86f, -.08f, .52f, 2),
            new Vector4(2.02f, .62f, .04f, 3), new Vector4(2.18f, -.62f, .10f, 0), new Vector4(2.36f, .24f, .46f, 1),
            new Vector4(2.52f, -.30f, .60f, 4)
        };
        static readonly Color[] ShellColors =
        {
            new Color(1f, .82f, .32f), new Color(1f, .42f, .52f), new Color(.72f, .46f, 1f), new Color(.46f, .70f, 1f),
            new Color(1f, .96f, .86f)
        };
        const float CelebrationExit = 2.70f, CelebrationShrink = 2.90f, CelebrationCut = 3.05f;

        static float Overshoot(float t, float s) { t = Mathf.Clamp01(t) - 1; return 1 + (s + 1) * t * t * t + s * t * t; }
        // A pop: from nothing up to `peak` in `rise` seconds, then back to 1 over `settle` seconds.
        static float PopScale(float e, float peak, float rise, float settle)
        {
            if (e <= 0) return 0;
            return e < rise ? peak * EaseOut(e / rise) : Mathf.Lerp(peak, 1, EaseInOut((e - rise) / settle));
        }
        // The bottom edge of the gameplay HUD (Target, King, Moves) in overlay units: the reference dims only the board and the
        // booster bar, so the HUD and the cheering King stay bright. Null when the HUD is not there.
        float? HudBottom()
        {
            var header = GameObject.Find("Imported gameplay header"); var rect = header ? header.transform as RectTransform : null;
            if (!rect || !rect.gameObject.activeInHierarchy) return null;
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            return LocalPoint(rect, corners[0]).y;
        }

        // Staged after the user's Royal Match Level 13 clip (Design/RoyalMatchReference/OBSERVATIONS.md, "Level 13
        // completion"; the user, 2026-10-06: "I want this game experience"), in Royal Throne's own art: the board dims below
        // the HUD; the emblem widens in, ROYAL is written a letter at a time and THRONE lands with an overshoot; two pennants
        // unroll from under the HUD; four herald trumpets swing in from the lower edges; firework shells rise and burst behind
        // the logo; then the logo swells, shrinks to a point, and the dim is cut in a single frame (3.05 s). A tap jumps to the
        // swell. The fanfare starts with the pennants; the level music has already faded out (RoyalAudioDirector).
        IEnumerator RunCelebration(LevelManager lm)
        {
            reduced = PlayerPrefs.GetInt("RoyalThrone.HomeMotion", 1) == 0;
            // As in the reference, the move that met the goals plays out (its chain and the refill) before the screen dims;
            // at most 2.5 s, and a tap cuts it short.
            float settle = Time.unscaledTime + 2.5f;
            while (Time.unscaledTime < settle && lm.gameStatus == GameState.PreWinAnimations && !lm.SkipWinCelebration &&
                   (lm.IsResolvingMatches || RoyalPowerupDirector.Busy || lm.GetItems().Exists(i => i && (i.destroying || i.falling))))
                yield return null;
            if (lm.gameStatus != GameState.PreWinAnimations) { celebrationHandover = true; yield break; }
            // An overlay made this frame may not have its screen size yet; everything below is laid out from it.
            yield return Scaled();
            var h = Half;
            float dimTop = Mathf.Min(HudBottom() ?? h.y + 20, h.y + 20);
            // Back to front: the dim (with the firework sparks drawn over it on this canvas), then a canvas of its own for the
            // pennants, the logo, the trumpets and the label, so the fireworks burst behind them.
            var stage = Node("Level won", root, Vector2.zero, Size);
            var dim = Picture("Dim", stage, piece, new Vector2(0, (dimTop - h.y - 20) * .5f), new Vector2(Size.x + 40, dimTop + h.y + 20), Color.clear);
            dim.preserveAspect = false;
            var front = Node("Celebration front", root, Vector2.zero, Size);
            var frontCanvas = front.gameObject.AddComponent<Canvas>(); frontCanvas.overrideSorting = true; frontCanvas.sortingOrder = 36;

            // Two pennants unroll downward from under the HUD at 17% and 83% of the width: a clip grows over the cloth and the
            // rolled end rides its lower edge.
            float flagW = 120, flagH = flagW * 420 / 200f;
            var clips = new RectTransform[2]; var rolls = new Image[2];
            for (int i = 0; i < 2; i++)
            {
                float fx = (i == 0 ? -1 : 1) * h.x * .70f;
                clips[i] = Node("Pennant", front, new Vector2(fx, dimTop + 2), new Vector2(flagW + 12, 0)); clips[i].pivot = new Vector2(.5f, 1);
                clips[i].gameObject.AddComponent<RectMask2D>();
                var cloth = Picture("Cloth", clips[i], flagArt ? flagArt : pennant, Vector2.zero, new Vector2(flagW, flagH), Color.white);
                cloth.preserveAspect = false; var c = cloth.rectTransform; c.anchorMin = c.anchorMax = c.pivot = new Vector2(.5f, 1); c.anchoredPosition = Vector2.zero;
                rolls[i] = Picture("Pennant roll", front, flagRoll ? flagRoll : piece, new Vector2(fx, dimTop), new Vector2(flagW * 1.16f, flagW * 1.16f * 60 / 228f), flagRoll ? Color.white : Color.clear);
            }

            // The lockup: the logo's layers, or the whole logo (or the name in letters) if they are missing.
            float logoW = 470, logoY = h.y * .08f;
            var lockup = Node("Royal Throne lockup", front, new Vector2(0, logoY), new Vector2(logoW, logoW));
            bool layered = logoPart != null && global::System.Array.TrueForAll(logoPart, s => s);
            RectTransform[] parts;
            float[] partIn;
            if (layered)
            {
                parts = new RectTransform[LogoParts.Length]; partIn = LogoIn;
                for (int i = 0; i < parts.Length; i++)
                {
                    var l = LogoLayout[i];
                    var img = Picture("Logo " + LogoParts[i], lockup, logoPart[i], new Vector2(l.x, l.y) * logoW, new Vector2(l.z, l.w) * logoW, Color.white);
                    img.preserveAspect = false; parts[i] = img.rectTransform; parts[i].localScale = Vector3.zero;
                }
            }
            else
            {
                var whole = logo ? Picture("Royal Throne logo", lockup, logo, Vector2.zero, new Vector2(logoW, logoW * logo.rect.height / Mathf.Max(1, logo.rect.width)), Color.white).rectTransform
                                 : Words("Royal Throne", lockup, "ROYAL THRONE", 96, Vector2.zero, new Vector2(700, 160), Gold, 4).rectTransform;
                whole.localScale = Vector3.zero; parts = new[] { whole }; partIn = new[] { LogoIn[0] };
            }

            // Four herald trumpets from the lower edges, bells up and in; they slide in along their own line and bob.
            var horns = new List<RectTransform>(); var hornRest = new List<Vector2>(); var hornDir = new List<Vector2>(); var hornAngle = new List<float>();
            if (trumpet)
                foreach (var spec in Horns)
                {
                    bool right = spec.x > 0; float len = 300 * spec.w;
                    var img = Picture("Trumpet", front, trumpet, Vector2.zero, new Vector2(len, len * 360 / 600f), Color.white);
                    var r = img.rectTransform; r.pivot = new Vector2(.90f, 1 - 120 / 360f);   // the bell's rim, on the tube's line
                    float angle = right ? 180 - spec.z : spec.z;                                 // the way the bell points
                    r.localScale = new Vector3(right ? -1 : 1, 1, 1); r.localRotation = Quaternion.Euler(0, 0, right ? -spec.z : spec.z);
                    var rest = new Vector2(spec.x * h.x, spec.y * h.y);
                    horns.Add(r); hornRest.Add(rest); hornAngle.Add(right ? -spec.z : spec.z);
                    hornDir.Add(new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)));
                    r.anchoredPosition = rest - hornDir[hornDir.Count - 1] * 420;
                }

            // "Tap to Skip", where the booster session's label (ReferenceWinFinish) will carry on.
            var skip = Words("Tap to Skip", front, "Tap to Skip", 31, new Vector2(0, -h.y + Size.y * .14f), new Vector2(400, 60), new Color(1, .97f, .8f, 0), 0);
            skip.GetComponent<Shadow>().effectColor = new Color(.02f, .10f, .22f); skip.GetComponent<Shadow>().effectDistance = new Vector2(1, -2);

            // Its reaction makes the king in the HUD portrait cheer.
            if (RoyalPowerupDirector.Get && lm.GameField) RoyalPowerupDirector.Get.Creation(lm.GameField.position);

            float t = 0; int fired = 0; bool fanfare = false, abandoned = false;
            var fanfareClip = Resources.Load<AudioClip>("RoyalGameplay/Audio/royal-fanfare");
            var rising = new Image[3];
            while (true)
            {
                bool won = lm.gameStatus == GameState.Win;
                if (lm.gameStatus != GameState.PreWinAnimations && !won) { abandoned = true; break; }
                t += Mathf.Min(Time.unscaledDeltaTime, .05f);
                // A tap, or a card opening early (a last-move win with nothing left to fire), goes straight to the swell.
                if ((lm.SkipWinCelebration || won) && t < CelebrationExit) t = CelebrationExit;
                if (reduced && t < CelebrationExit && t > 1.2f) t = CelebrationExit;
                if (t >= CelebrationCut) break;
                if (!fanfare && t >= .70f)
                {
                    fanfare = true;
                    // Royal Throne's herald fanfare (Design/Audio/WinFanfare), scored to this timeline: pickups with the
                    // pennants, a held chord as the trumpets land, the final chord on the swell. The inherited "complete"
                    // jingle stands in if the clip is missing.
                    if (fanfareClip && RoyalAudioDirector.Get) RoyalAudioDirector.Get.PlayClip(fanfareClip, .95f, 0);
                    else if (SoundBase.Instance && SoundBase.Instance.complete != null && SoundBase.Instance.complete.Length > 0) SoundBase.Instance.PlaySound(SoundBase.Instance.complete[0]);
                }
                dim.color = new Color(Navy.r * .45f, Navy.g * .45f, Navy.b * .45f, .72f * EaseOut(t / .2f));

                // The logo: each layer pops in on its beat; at the end the whole lockup swells and then shrinks to a point.
                for (int i = 0; i < parts.Length; i++)
                {
                    float e = t - partIn[i];
                    if (reduced) { parts[i].localScale = Vector3.one * Mathf.Clamp01(e / .15f); continue; }
                    if (parts.Length > 1 && i == 0)
                        parts[i].localScale = e <= 0 ? Vector3.zero : new Vector3(Mathf.Lerp(.25f, 1, Overshoot(e / .16f, 1.4f)), Mathf.Lerp(.55f, 1, Overshoot(e / .16f, 1.4f)), 1);
                    else parts[i].localScale = Vector3.one * (i == parts.Length - 1 && parts.Length > 1 ? PopScale(e, 1.32f, .09f, .14f) : PopScale(e, 1.35f, .07f, .10f));
                }
                float swell = reduced ? 1 : t < CelebrationShrink ? Mathf.Lerp(1, 1.18f, EaseOut((t - CelebrationExit) / .2f)) : 1.18f * (1 - Mathf.Pow(Mathf.Clamp01((t - CelebrationShrink) / (CelebrationCut - CelebrationShrink)), 1.6f));
                lockup.localScale = Vector3.one * Mathf.Max(0, swell);

                // The pennants unroll over 0.4 s and roll back up at the end.
                float unroll = EaseOut((t - .85f) / .35f) * (1 - EaseInOut((t - CelebrationExit - .05f) / .2f));
                if (reduced) unroll = 1;
                for (int i = 0; i < 2; i++)
                {
                    float len = flagH * unroll;
                    clips[i].sizeDelta = new Vector2(flagW + 12, len);
                    rolls[i].rectTransform.anchoredPosition = new Vector2(clips[i].anchoredPosition.x, dimTop + 2 - Mathf.Max(0, len - 14));
                    rolls[i].color = new Color(1, 1, 1, flagRoll && !reduced ? Mathf.Clamp01((t - .8f) / .06f) * (1 - Mathf.Clamp01((unroll - .9f) / .1f)) : 0);
                    clips[i].localRotation = Quaternion.Euler(0, 0, reduced ? 0 : Mathf.Sin(t * 2.1f + i * 1.7f) * 1.5f * unroll);
                }

                // The trumpets swing in over 0.3 s (the lower pair a beat later), bob, and leave the way they came.
                for (int i = 0; i < horns.Count; i++)
                {
                    float start = 1.05f + (i % 2) * .08f;
                    float arrive = reduced ? 1 : EaseOut((t - start) / .3f), leave = reduced ? 0 : EaseInOut((t - CelebrationExit + .05f) / .25f);
                    horns[i].anchoredPosition = hornRest[i] - hornDir[i] * 420 * ((1 - arrive) + leave);
                    float bob = reduced ? 0 : Mathf.Sin((t - start) * 7f + i) * 3 * arrive;
                    horns[i].localRotation = Quaternion.Euler(0, 0, hornAngle[i] + bob);
                }

                skip.color = new Color(1, .97f, .8f, Mathf.Clamp01((t - .9f) / .2f));

                // Fireworks: shells rise from the bottom centre and burst around the logo, behind it.
                if (!reduced)
                {
                    for (int k = 0; k < 3; k++)
                    {
                        var s = Shells[k]; float launch = s.x - .38f, u = (t - launch) / .38f;
                        if (u < 0 || u >= 1) { if (rising[k]) rising[k].enabled = false; continue; }

                        if (!rising[k]) { rising[k] = Picture("Shell trail", stage, piece, Vector2.zero, new Vector2(6, 120), Color.white); rising[k].preserveAspect = false; }
                        var a = new Vector2((k - 1) * 30, -h.y * .82f); var b = new Vector2(s.y * h.x, s.z * h.y); var p = Vector2.Lerp(a, b, EaseOut(u));
                        var d = (b - a).normalized; rising[k].enabled = true; rising[k].rectTransform.anchoredPosition = p - d * 60;
                        rising[k].rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90);
                        rising[k].color = new Color(1, .96f, .84f, .85f * (1 - u * u));
                    }
                    while (fired < Shells.Length && t >= Shells[fired].x && t < CelebrationExit + .1f)
                    {
                        var s = Shells[fired]; FireworkBurst(new Vector2(s.y * h.x, s.z * h.y), ShellColors[(int)s.w], 1.5f, true); fired++;
                        RoyalAudioDirector.Get?.Play(RoyalCue.Firework, .8f);
                    }
                }
                yield return null;
            }
            // The cut: the dim, the lockup and every spark go in one frame, and the booster session takes over.
            foreach (var b in bits) b.age = b.life;
            // A level left mid-celebration takes its fanfare with it; a won one lets it ring into the booster session.
            if (abandoned && fanfareClip) { var audio = FindAnyObjectByType<RoyalAudioDirector>(); if (audio) audio.StopClip(fanfareClip); }
            celebrationHandover = true;
            if (stage) Destroy(stage.gameObject);
            if (front) Destroy(front.gameObject);
        }

        // ---------------------------------------------------------------- 2. the finale: a Reward counter in the HUD

        RectTransform rewardContents; Text rewardCount; Image rewardIcon; Text goalHeading; GameObject goalContents; string goalWords;
        bool finale, finaleDraining; int rewardShown, rewardTarget, finaleBase; LevelManager finaleLevel;
        float finaleReserved, finaleArrived;
        Vector2 lastFinaleSource;
        readonly HashSet<Item> finaleSources = new HashSet<Item>();
        public static int FinaleBurstCount => instance ? instance.finaleSources.Count : 0;
        public static float FinalePendingCoinValue => instance && instance.finale && instance.finaleReserved - instance.finaleArrived > .0001f ? instance.finaleReserved - instance.finaleArrived : 0;

        /// <summary>The goal panel becomes a Reward coin counter for the booster session.</summary>
        public static void BeginFinale(LevelManager lm) { Get(lm).StartFinale(lm); }
        /// <summary>After the booster session: the coins still due fly in and the counter settles on the win's amount.</summary>
        public static IEnumerator EndFinale(LevelManager lm) { var s = Get(lm); yield return s.FinishFinale(lm); }

        void StartFinale(LevelManager lm)
        {
            RestoreHud();
            var panel = GameObject.Find("Royal HUD goal panel");
            goalHeading = null; goalContents = null;
            if (panel)
            {
                foreach (var t in panel.GetComponentsInChildren<Text>(true)) if (t.name == "Goal heading") { goalHeading = t; break; }
                foreach (var t in panel.GetComponentsInChildren<RectTransform>(true)) if (t.name == "Goal contents") { goalContents = t.gameObject; break; }
            }
            if (goalHeading) { goalWords = goalHeading.text; goalHeading.text = "Reward"; }
            if (goalContents)
            {
                goalContents.SetActive(false);
                var parent = (RectTransform)goalContents.transform.parent;
                rewardContents = new GameObject("Royal reward contents", typeof(RectTransform)).GetComponent<RectTransform>();
                rewardContents.SetParent(parent, false); rewardContents.anchorMin = Vector2.zero; rewardContents.anchorMax = Vector2.one;
                rewardContents.offsetMin = new Vector2(5, 8); rewardContents.offsetMax = new Vector2(-5, -16);
                var iconRect = new GameObject("Reward coin", typeof(RectTransform)).GetComponent<RectTransform>(); iconRect.SetParent(rewardContents, false);
                iconRect.anchorMin = new Vector2(.18f, .12f); iconRect.anchorMax = new Vector2(.82f, .96f); iconRect.offsetMin = iconRect.offsetMax = Vector2.zero;
                rewardIcon = iconRect.gameObject.AddComponent<Image>(); coin = coin ? coin : RoyalPieceSkin.Art("coin"); rewardIcon.sprite = coin; rewardIcon.preserveAspect = true; rewardIcon.raycastTarget = false;
                var numRect = new GameObject("Reward count", typeof(RectTransform)).GetComponent<RectTransform>(); numRect.SetParent(rewardContents, false);
                // Over the coin's lower right, clear of the panel's frame, as the reference's counter sits.
                numRect.anchorMin = new Vector2(.34f, -.02f); numRect.anchorMax = new Vector2(.90f, .44f); numRect.offsetMin = numRect.offsetMax = Vector2.zero;
                rewardCount = numRect.gameObject.AddComponent<Text>(); rewardCount.font = font; rewardCount.fontSize = 30; rewardCount.alignment = TextAnchor.MiddleRight;
                rewardCount.color = Color.white; rewardCount.raycastTarget = false; rewardCount.text = "0";
                var o = rewardCount.gameObject.AddComponent<Outline>(); o.effectColor = Navy; o.effectDistance = new Vector2(2, -2);
            }
            rewardTarget = Coins(lm); finaleBase = Mathf.Min(10, rewardTarget);
            rewardShown = finaleBase; if (rewardCount) rewardCount.text = rewardShown.ToString();
            finale = true; finaleDraining = false; finaleLevel = lm;
            finaleReserved = finaleArrived = 0; finaleSources.Clear();
            // The coin pops into the emptied panel, as the reference's does on the frame its dim is cut.
            if (rewardContents && !reduced) StartCoroutine(PopRewardIn(rewardContents));
        }
        IEnumerator PopRewardIn(RectTransform contents)
        {
            float t = 0; contents.localScale = Vector3.zero;
            while (contents && t < .14f) { t += Mathf.Min(Time.unscaledDeltaTime, .05f); contents.localScale = Vector3.one * PopScale(t, 1.12f, .08f, .06f); yield return null; }
            if (contents) contents.localScale = Vector3.one;
        }

        // Only accepted booster activations can emit finale coins. No random live-piece timer.
        void UpdateFinale()
        {
            if (!finale) return;
            var lm = finaleLevel;
            if (!lm || (lm.gameStatus != GameState.PreWinAnimations && lm.gameStatus != GameState.Win)) { finale = false; RestoreHud(); return; }
        }

        public static void FinaleBoosterActivated(Item item)
        {
            if (!instance || !item) return;
            var s = instance; var lm = s.finaleLevel;
            if (!s.finale || s.finaleDraining || !lm || lm.gameStatus != GameState.PreWinAnimations || !s.rewardIcon ||
                !RoyalPowerupDirector.IsPower(item.currentType) || !s.finaleSources.Add(item)) return;
            s.rewardTarget = Mathf.Max(s.rewardTarget, Coins(lm));
            s.lastFinaleSource = s.World(item.transform.position);
            int remaining = lm.GetItems().FindAll(i => i && !i.destroying && RoyalPowerupDirector.IsPower(i.currentType)).Count;
            if (lm.limitType == LIMIT.MOVES) remaining += Mathf.Max(0, lm.Limit);
            // Allocate the EXISTING award across actual booster bursts. The reference's full
            // per-booster economy is not recovered; this must never manufacture currency.
            float unassigned = Mathf.Max(0, s.rewardTarget - s.finaleBase - s.finaleReserved);
            // As in the user's Level 13 clip, a few coins pop out of each booster as it goes off (two from a rocket, one from
            // anything else), hang there a moment and then arc into the Reward coin. Each carries its share of our award
            // (the user's choice, 2026-10-06: keep 50 + 5 per move left), so the counter climbs by that share per coin.
            bool rocket = item.currentType == ItemsTypes.HORIZONTAL_STRIPPED || item.currentType == ItemsTypes.VERTICAL_STRIPPED;
            s.EmitFinaleCoins(s.lastFinaleSource, unassigned / (remaining + 1), rocket ? 2 : 1);
        }

        void EmitFinaleCoins(Vector2 source, float amount, int count)
        {
            if (amount <= .00001f || !rewardIcon || count <= 0) return;
            finaleReserved += amount;
            int generation = previewGeneration;
            for (int i = 0; i < count; i++)
                Fly(coin, source, Local(rewardIcon.transform), i * .05f, 58, .5f, () =>
                {
                    if (!finale || generation != previewGeneration) return;
                    finaleArrived += amount / count;
                    rewardShown = Mathf.Min(rewardTarget, finaleBase + Mathf.FloorToInt(finaleArrived + .0001f));
                    if (rewardCount) rewardCount.text = rewardShown.ToString();
                    if (rewardIcon) PulseCoin(rewardIcon.rectTransform);
                }, true, 70, 0, true, rewardIcon.transform);
        }

        IEnumerator FinishFinale(LevelManager lm)
        {
            if (!finale || !rewardIcon) yield break;
            finaleDraining = true;
            rewardTarget = Mathf.Max(rewardTarget, Coins(lm));
            // The card opens as soon as the last booster has cleared its cells (the user, 2026-10-13, replacing the
            // 2026-10-06 refill and rest): no refill, no wait for coins still in flight, no rest. The counter shows its total.
            ClearFlights();
            rewardShown = rewardTarget; if (rewardCount) rewardCount.text = rewardShown.ToString();
            yield break;
        }

        void RestoreHud()
        {
            finale = false;
            if (goalHeading && goalWords != null) goalHeading.text = goalWords;
            if (goalContents) goalContents.SetActive(true);
            if (rewardContents) Destroy(rewardContents.gameObject);
            rewardContents = null; rewardCount = null; rewardIcon = null; goalHeading = null; goalContents = null; goalWords = null;
        }

        void OnEnable() { LevelManager.OnLevelLoaded += LevelLoaded; }
        void OnDisable() { LevelManager.OnLevelLoaded -= LevelLoaded; CancelRewardPresentation(); RestoreHud(); }
        void LevelLoaded() { CancelRewardPresentation(); RestoreHud(); }

        // ---------------------------------------------------------------- 3. the win card

        sealed class Pending { public int coins, before, stars, level; }
        Pending pending; bool homeRunning; int homeCoins;
        Coroutine reward, homeReward, risingReward;
        Canvas.WillRenderCanvases homeHold;
        RoyalThrone.Presentation.RoyalHomeScreen heldHome;
        readonly List<GameObject> rewardObjects = new List<GameObject>();

        void TrackRewardObject(GameObject value) { rewardObjects.Add(value); }
        void ReleaseHomeHold()
        {
            if (homeHold != null) Canvas.willRenderCanvases -= homeHold;
            homeHold = null;
            if (heldHome) heldHome.RefreshProgress();
            heldHome = null;
        }
        void CancelRewardPresentation()
        {
            // Rewards are already saved by LevelManager. Retire presentation ownership without
            // invoking arrival callbacks or carrying an old balance into another level/profile.
            pending = null;
            finaleReserved = finaleArrived = 0;
            ReleaseHomeHold();
            if (reward != null) StopCoroutine(reward);
            if (homeReward != null) StopCoroutine(homeReward);
            if (risingReward != null) StopCoroutine(risingReward);
            reward = homeReward = risingReward = null;
            homeRunning = false;
            ReleaseHomeControls();
            homeCoins = InitScript.Gems;
            ClearFlights();
            ClearPill();
            // Unity may stop a disabled host without disposing every nested enumerator.
            // Keep explicit ownership of card, tip and floating-label objects for that case.
            foreach (var obj in rewardObjects) if (obj) Destroy(obj);
            rewardObjects.Clear();
        }

        public static void Reward(LevelManager lm, int coins, int balanceBefore)
        {
            var s = Get(lm); s.CancelRewardPresentation();
            var menu = MenuManager.Instance ? MenuManager.Instance.GetCanvasGlobal().Find("MenuComplete") : null;
            var winCard = RoyalThrone.Presentation.RoyalWinCard.Of(menu);
            if (winCard) winCard.SetReward(coins); // retain the committed amount; never credit currency here
            // The supplied completed-level recording confirms this beyond the opening levels: CONTINUE returns Home,
            // where the star leads the overlapping coin collection. The popup itself celebrates the star.
            s.pending = new Pending { coins = coins, before = balanceBefore, stars = lm.StarsGained, level = lm.currentLevel };
        }

        // ---- legacy card-coin presentation; normal wins now collect on Home.

        RectTransform pill; Text pillValue; Image pillFlash; float flash;
        float PillY => Half.y - (Screen.height - Screen.safeArea.yMax) * root.rect.width / Mathf.Max(1, Screen.width) - 76;
        void BuildPill(int value)
        {
            pill = Node("Royal coin balance", root, new Vector2(0, PillY), new Vector2(270, 80));
            var rim = pill.gameObject.AddComponent<RoyalThrone.Presentation.RoyalPanelGraphic>();
            rim.topColor = new Color(1, .90f, .52f); rim.bottomColor = new Color(.80f, .50f, .15f); rim.cornerRadius = 40; rim.raycastTarget = false;
            var face = Node("Cream", pill, Vector2.zero, new Vector2(262, 72)).gameObject.AddComponent<RoyalThrone.Presentation.RoyalPanelGraphic>();
            face.topColor = new Color(1, .97f, .88f); face.bottomColor = new Color(.97f, .88f, .66f); face.cornerRadius = 36; face.raycastTarget = false;
            pillFlash = Picture("Flash", pill, glow, Vector2.zero, new Vector2(330, 150), new Color(1, .92f, .6f, 0));
            coin = coin ? coin : RoyalPieceSkin.Art("coin");
            Picture("Coin", pill, coin, new Vector2(-118, 0), new Vector2(92, 92), Color.white);
            pillValue = Words("Balance", pill, value.ToString(), 42, new Vector2(24, 1), new Vector2(190, 70), Navy, 0);
            pillValue.GetComponent<Shadow>().effectColor = new Color(1, 1, 1, .5f); pillValue.GetComponent<Shadow>().effectDistance = new Vector2(0, -1.5f);
        }
        void ClearPill() { if (pill) Destroy(pill.gameObject); pill = null; pillValue = null; pillFlash = null; }

        IEnumerator RunCardCoins(LevelManager lm, int coins, int before)
        {
            reduced = PlayerPrefs.GetInt("RoyalThrone.HomeMotion", 1) == 0;
            var menus = MenuManager.Instance; var card = menus ? menus.GetCanvasGlobal().Find("MenuComplete") : null;
            if (!card) yield break;
            BuildPill(before); var pillGroup = pill.gameObject.AddComponent<CanvasGroup>(); pillGroup.alpha = 0;
            // Keep the glowing star clear: wait for its full settle and reward reveal, then collect below the pillow.
            float t0 = Time.unscaledTime; var piece = RoyalThrone.Presentation.RoyalWinCard.Of(card); var star1 = card.Find("Image/Star1");
            while (card.gameObject.activeInHierarchy && Time.unscaledTime - t0 < 4.5f)
            {
                float e = Time.unscaledTime - t0;
                pillGroup.alpha = EaseOut((e - .15f) / .25f); pill.anchoredPosition = new Vector2(0, PillY + (reduced ? 0 : 16 * (1 - EaseOut((e - .15f) / .25f))));
                bool landed = piece ? piece.RewardsReady : star1 && star1.gameObject.activeInHierarchy;
                if (landed && e > .5f) break;
                yield return null;
            }
            if (!card.gameObject.activeInHierarchy) { ClearPill(); yield break; }
            float wait = Time.unscaledTime + .12f;
            while (card.gameObject.activeInHierarchy && Time.unscaledTime < wait) yield return null;
            if (!card.gameObject.activeInHierarchy) { ClearPill(); yield break; }
            pillGroup.alpha = 1; pill.anchoredPosition = new Vector2(0, PillY);
            if (reduced)
            {
                if (pillValue) pillValue.text = (before + coins).ToString();
                while (card.gameObject.activeInHierarchy) yield return null;
                ClearPill(); yield break;
            }
            int flightsN = 10, landedN = 0; coin = coin ? coin : RoyalPieceSkin.Art("coin");
            var frame = piece ? piece.transform as RectTransform : card.Find("Image") as RectTransform;
            if (!frame) { if (pillValue) pillValue.text = (before + coins).ToString(); ClearPill(); yield break; }
            var receiver = piece ? piece.coinOrigin : null;
            Vector2 receiverInCard = receiver ? (Vector2)frame.InverseTransformPoint(receiver.position) : new Vector2(-67, -214);
            Vector2 At(Vector2 position) => LocalPoint(frame, frame.TransformPoint(position));
            float cardScale = CardCoinBounds(frame).width / Mathf.Max(1, frame.rect.width);
            Vector2 collect = At(receiverInCard), origin = At(receiverInCard + new Vector2(-50, 123));
            for (int i = 0; i < flightsN; i++)
            {
                Fly(coin, origin, collect, .055f * i, 46 * cardScale, .6f, () =>
                {
                    landedN++; int shown = before + CoinAmountAtArrival(coins, landedN, flightsN);
                    if (pillValue) pillValue.text = shown.ToString(); flash = 1;
                    PulseCoin(receiver);
                    if (landedN == 1) risingReward = StartCoroutine(RisePlus(coins));
                    if (landedN == 1) ReferenceGameplayAudio.Play("coin_counter__rfv387_17638", .55f);
                }, true, 0, 0, false, receiver);
                // A restrained fan off the pillow's lower-left edge, then a curved collection into the badge. All three
                // Bezier points stay in the card, and a live full-sprite bound also guards resolution/layout changes.
                // Home, finale and area rewards retain their own authored paths and never receive this bound.
                var flight = flights[flights.Count - 1];
                flight.cardFrame = frame;
                flight.burst = At(receiverInCard + new Vector2(-127 + (i % 5 - 2) * 12, 112 + i % 3 * 14));
                flight.control = At(receiverInCard + new Vector2(-113 - i % 3 * 7, -10 + i % 2 * 12));
            }
            while (card.gameObject.activeInHierarchy && landedN < flightsN) yield return null;
            if (!card.gameObject.activeInHierarchy) ClearFlights();
            if (pillValue) pillValue.text = (before + coins).ToString();
            while (card.gameObject.activeInHierarchy) yield return null;
            float fade = 0; var g = pill ? pill.GetComponent<CanvasGroup>() : null;
            while (g && fade < .2f) { fade += Time.unscaledDeltaTime; g.alpha = 1 - fade / .2f; yield return null; }
            ClearPill();
        }

        IEnumerator RisePlus(int coins)
        {
            if (!pill) yield break;
            var label = Words("+coins", root, "+" + coins, 44, pill.anchoredPosition + new Vector2(178, 0), new Vector2(160, 60), new Color(.62f, 1f, .52f), 2.5f);
            TrackRewardObject(label.gameObject);
            float t = 0;
            while (t < 1 && label) { t += Time.unscaledDeltaTime; label.rectTransform.anchoredPosition += Vector2.up * 34 * Time.unscaledDeltaTime; label.color = new Color(.62f, 1f, .52f, 1 - EaseInOut((t - .45f) / .55f)); yield return null; }
            if (label) Destroy(label.gameObject);
        }

        // ---------------------------------------------------------------- 4. Home: star, coins, tasks

        // On Home, after the user's Royal Match Level 13 clip (Design/RoyalMatchReference/OBSERVATIONS.md): the area scene is
        // shown alone for a moment, then the top bar and the Level row slide in and settle with a small overshoot. The star pops
        // in over the scene, glows, dips a little and curves out to the right and up into the star counter; while it is in the
        // air a pile of coins appears at the same spot under "+N", churns, and then streams up the left side into the coin
        // counter one coin at a time. The counters count up as they are reached. The task list then opens if its next task
        // can be paid for. Presentation only: the coins and the star were saved when the goals were met.
        RoyalThrone.ReferenceGameplay.ReferenceHomeLayout entranceLayout;
        void HideHomeControls()
        {
            if (entranceLayout || reduced || PlayerPrefs.GetInt("RoyalThrone.HomeMotion", 1) == 0) return;
            var home = FindAnyObjectByType<RoyalThrone.Presentation.RoyalHomeScreen>();
            entranceLayout = home ? home.GetComponent<RoyalThrone.ReferenceGameplay.ReferenceHomeLayout>() : null;
            if (entranceLayout) entranceLayout.EntranceAway = 1;
        }
        void ReleaseHomeControls() { if (entranceLayout) entranceLayout.EntranceAway = 0; entranceLayout = null; }

        // Where the Home star and coins appear: over the area scene, above the Level button.
        Vector2 HomeRewardOrigin(RoyalThrone.Presentation.RoyalHomeScreen home)
        {
            var level = home.levelButton ? home.levelButton.transform as RectTransform : null;
            if (!level) return new Vector2(0, -Half.y * .3f);
            var corners = new Vector3[4]; level.GetWorldCorners(corners);
            float top = LocalPoint(level, corners[1]).y;
            return new Vector2(0, Mathf.Min(top + 150, -Half.y * .1f));
        }

        IEnumerator HomeRewards(LevelManager lm)
        {
            homeRunning = true; var p = pending; pending = null;
            bool completed = false;
            try
            {
                reduced = PlayerPrefs.GetInt("RoyalThrone.HomeMotion", 1) == 0;
                HideHomeControls();
                var home = FindAnyObjectByType<RoyalThrone.Presentation.RoyalHomeScreen>();
                float wait = Time.unscaledTime + 3; while ((!home || !home.coinLabel || !home.coinLabel.gameObject.activeInHierarchy) && Time.unscaledTime < wait) { yield return null; home = home ? home : FindAnyObjectByType<RoyalThrone.Presentation.RoyalHomeScreen>(); }
                if (!home || !home.coinLabel || lm.gameStatus != GameState.Map) yield break;
                if (reduced)
                {
                    // Currency and stars are already saved. Motion off shows those balances directly, with no flights,
                    // count-up, floating amount or temporary hold on the Home counters.
                    ReleaseHomeControls();
                    homeCoins = p.before + p.coins;
                    home.RefreshProgress();
                    completed = true;
                    int starsAvailable = home.Progress.available;
                    if (Affordable(starsAvailable) && home.areaButton && !(home.AreaView && home.AreaView.GuideFirstTask())) home.OpenPanel("area", RoyalThrone.Presentation.RoyalAreaView.Entry(home).gameObject);
                    yield break;
                }
                // Hold the counters at their old values while the rewards fly.
                homeCoins = p.before; home.RefreshProgress(); int starsNow = home.Progress.available; int starsShown = Mathf.Max(0, starsNow - p.stars);
                void Hold() { if (home.coinLabel) home.coinLabel.text = homeCoins.ToString("N0"); if (home.starLabel) home.starLabel.text = starsShown.ToString(); }
                heldHome = home; homeHold = Hold; Canvas.willRenderCanvases += homeHold;
                try
                {
                    // The scene alone, then the controls slide in (0.32 s, a small overshoot), then a short beat.
                    float alone = Time.unscaledTime + .6f;
                    while (Time.unscaledTime < alone && lm.gameStatus == GameState.Map) yield return null;
                    if (entranceLayout)
                    {
                        float e = 0;
                        while (e < .32f && lm.gameStatus == GameState.Map && entranceLayout)
                        { e += Mathf.Min(Time.unscaledDeltaTime, .05f); entranceLayout.EntranceAway = 1 - Overshoot(e / .32f, 1.2f); yield return null; }
                        ReleaseHomeControls();
                    }
                    float beat = Time.unscaledTime + .25f;
                    while (Time.unscaledTime < beat && lm.gameStatus == GameState.Map) yield return null;
                    if (lm.gameStatus != GameState.Map) yield break;
                    var origin = HomeRewardOrigin(home);

                    bool starLanded = p.stars <= 0 || !home.starLabel;
                    if (!starLanded)
                    {
                        var starParent = home.starLabel.transform.parent;
                        var starReceiver = starParent.Find("Imported 16674") ?? starParent.Find("Stars icon");
                        var starArt = Resources.Load<Sprite>("RoyalGameplay/win-star");
                        if (!starArt) starArt = RoyalThrone.Presentation.RoyalAreaKit.Star ? RoyalThrone.Presentation.RoyalAreaKit.Star : star;
                        StartCoroutine(HomeStar(starArt, origin, starReceiver ? starReceiver : home.starLabel.transform,
                            () => { starLanded = true; starsShown = starsNow; RoyalAudioDirector.Get?.Play(RoyalCue.Create); if (starReceiver) PulseCoin(starReceiver as RectTransform); }));
                        // The coins appear under it while it is still in the air.
                        float coinStart = Time.unscaledTime + .95f;
                        while (Time.unscaledTime < coinStart && lm.gameStatus == GameState.Map) yield return null;
                    }
                    if (lm.gameStatus != GameState.Map) yield break;
                    var receiver = (home.coinLabel.transform.parent.Find("Imported 22081") ?? home.coinLabel.transform.parent.Find("Coins icon")) as RectTransform;
                    bool coinsLanded = false;
                    StartCoroutine(HomeCoins(p.coins, origin, receiver ? receiver : home.coinLabel.rectTransform,
                        (landed, count) => homeCoins = p.before + CoinAmountAtArrival(p.coins, landed, count), () => coinsLanded = true));
                    float t = 0;
                    while ((!coinsLanded || !starLanded) && t < 6f && lm.gameStatus == GameState.Map) { t += Time.unscaledDeltaTime; yield return null; }
                    if (lm.gameStatus != GameState.Map) yield break;
                    homeCoins = p.before + p.coins; starsShown = starsNow;
                    yield return new WaitForSecondsRealtime(.25f);
                    completed = coinsLanded && starLanded;
                }
                finally { ReleaseHomeHold(); }
                // The task list opens by itself when the current area's next task can be paid for.
                // The very first time, the reference points at the Area card instead (RoyalAreaView.GuideFirstTask).
                if (lm.gameStatus == GameState.Map && Affordable(starsNow) && home.areaButton && !(home.AreaView && home.AreaView.GuideFirstTask())) home.OpenPanel("area", RoyalThrone.Presentation.RoyalAreaView.Entry(home).gameObject);
            }
            finally
            {
                ReleaseHomeControls();
                if (!completed)
                {
                    ClearFlights();
                    foreach (var obj in rewardObjects) if (obj) Destroy(obj);
                    rewardObjects.Clear();
                }
                homeRunning = false; homeReward = null; ReleaseHomeHold();
            }
        }

        // The Home star: it pops in (0.13 s), glows for a moment, dips a little, then curves out to the right and up into the
        // star counter, gathering speed and tilting, with sparkles left behind it.
        IEnumerator HomeStar(Sprite art, Vector2 origin, Transform receiver, global::System.Action land)
        {
            var halo = Picture("Home star glow", root, soft, origin, new Vector2(320, 320), new Color(1, .86f, .4f, 0));
            var img = Picture("Home star", root, art, origin, new Vector2(165, 165), Color.white);
            TrackRewardObject(halo.gameObject); TrackRewardObject(img.gameObject);
            var r = img.rectTransform; r.localScale = Vector3.zero;
            const float pop = .13f, hold = .35f, dip = .18f, fly = .62f;
            var dipTo = origin + new Vector2(-14, -16);
            float t = 0;
            while (img && LevelManager.THIS && LevelManager.THIS.gameStatus == GameState.Map)
            {
                t += Mathf.Min(Time.unscaledDeltaTime, .05f);
                var to = receiver ? Local(receiver) : origin + new Vector2(200, 600);
                float shine = Mathf.Clamp01(t / pop) * (1 - Mathf.Clamp01((t - pop - hold - dip) / .25f));
                halo.color = new Color(1, .86f, .4f, .7f * shine * (.8f + .2f * Mathf.Sin(t * 14)));
                halo.rectTransform.localScale = Vector3.one * (1 + .12f * Mathf.Sin(t * 9));
                if (t < pop) { r.localScale = Vector3.one * PopScale(t, 1.12f, pop * .7f, pop * .3f); r.anchoredPosition = origin; }
                else if (t < pop + hold) r.localScale = Vector3.one;
                else if (t < pop + hold + dip)
                {
                    float u = EaseInOut((t - pop - hold) / dip);
                    r.anchoredPosition = Vector2.Lerp(origin, dipTo, u); r.localRotation = Quaternion.Euler(0, 0, 8 * u);
                }
                else
                {
                    float u = Mathf.Clamp01((t - pop - hold - dip) / fly), w = u * u;
                    var control = new Vector2(Mathf.Max(dipTo.x, to.x) + 170, Mathf.Lerp(dipTo.y, to.y, .25f));
                    var pos = (1 - w) * (1 - w) * dipTo + 2 * (1 - w) * w * control + w * w * to;
                    r.anchoredPosition = pos; halo.rectTransform.anchoredPosition = pos;
                    r.localScale = Vector3.one * Mathf.Lerp(1, .5f, w); r.localRotation = Quaternion.Euler(0, 0, 8 - 32 * w);
                    if (Random.value < .75f)
                    {
                        var b = Spawn(1, pos + Random.insideUnitCircle * 20, Random.insideUnitCircle * 30, Random.Range(.45f, .75f), Random.Range(14f, 24f), Random.value < .5f ? Gold : Color.white);
                        if (b != null) b.gravity = 20;
                    }
                    if (u >= 1) { SparkleBurst(to, 10, 260, Gold, 22); RingFlash(to, 90, Gold); land?.Invoke(); break; }
                }
                yield return null;
            }
            if (img) Destroy(img.gameObject);
            if (halo) Destroy(halo.gameObject);
        }

        // The Home coins: a pile pops in at the star's spot with "+N" over it, churns (each coin hops on its own beat), then the
        // coins leave one at a time, 0.07 s apart, and curve up the left side into the coin counter. Twelve coins at most;
        // each carries its share of the award.
        IEnumerator HomeCoins(int amount, Vector2 origin, RectTransform receiver, global::System.Action<int, int> arrived, global::System.Action done)
        {
            int n = Mathf.Clamp(amount, 1, 12);
            coin = coin ? coin : RoyalPieceSkin.Art("coin");
            var pile = new Image[n]; var off = new Vector2[n]; var phase = new float[n]; var from = new Vector2[n]; var gone = new bool[n];
            for (int i = 0; i < n; i++)
            {
                float a = i * 2.4f, rad = 14 + 34 * Mathf.Sqrt((i + .5f) / n);
                off[i] = new Vector2(Mathf.Cos(a) * rad * 1.3f, Mathf.Sin(a) * rad * .55f); phase[i] = i * 1.7f;
                pile[i] = Picture("Home coin", root, coin, origin + off[i], new Vector2(64, 64), Color.white);
                pile[i].rectTransform.localScale = Vector3.zero; TrackRewardObject(pile[i].gameObject);
            }
            var plus = Words("Coins won", root, "+" + amount, 60, origin + new Vector2(0, 66), new Vector2(300, 90), Gold, 3);
            TrackRewardObject(plus.gameObject); plus.rectTransform.SetAsLastSibling(); plus.rectTransform.localScale = Vector3.zero;
            const float churn = .62f, gap = .07f, fly = .55f;
            float t = 0; int landed = 0;
            while (landed < n && t < 6 && LevelManager.THIS && LevelManager.THIS.gameStatus == GameState.Map)
            {
                t += Mathf.Min(Time.unscaledDeltaTime, .05f);
                var to = receiver ? Local(receiver) : origin + new Vector2(-300, 700);
                plus.rectTransform.localScale = Vector3.one * PopScale(t - .05f, 1.15f, .1f, .1f);
                plus.rectTransform.anchoredPosition = origin + new Vector2(0, 66 + 12 * EaseOut(t / .8f));
                plus.color = new Color(Gold.r, Gold.g, Gold.b, 1 - Mathf.Clamp01((t - churn - n * gap - .2f) / .3f));
                for (int i = 0; i < n; i++)
                {
                    if (gone[i]) continue;
                    var r = pile[i].rectTransform; float leave = churn + i * gap;
                    if (t < leave)
                    {
                        float hop = Mathf.Abs(Mathf.Sin(t * 7 + phase[i])) * 22 * Mathf.Clamp01((t - .1f) / .15f);
                        r.localScale = Vector3.one * PopScale(t - i * .015f, 1.15f, .08f, .08f);
                        r.anchoredPosition = from[i] = origin + off[i] + new Vector2(0, hop);
                    }
                    else
                    {
                        float u = Mathf.Clamp01((t - leave) / fly), w = Mathf.Pow(u, 1.6f);
                        var control = new Vector2(Mathf.Min(from[i].x, to.x) - 140, Mathf.Lerp(from[i].y, to.y, .55f));
                        r.anchoredPosition = (1 - w) * (1 - w) * from[i] + 2 * (1 - w) * w * control + w * w * to;
                        r.localScale = Vector3.one * Mathf.Lerp(1, .7f, w);
                        if (u >= 1)
                        {
                            gone[i] = true; landed++; pile[i].gameObject.SetActive(false);
                            arrived?.Invoke(landed, n);
                            ReferenceCoinParticles.Play("CoinCollectHitParticles", root, to, 64);
                            if (receiver) PulseCoin(receiver);
                            if (landed == 1) ReferenceGameplayAudio.Play("coin_counter__rfv387_17638", .55f);
                            if (Time.unscaledTime >= nextCoinContactSound) { ReferenceGameplayAudio.Play("coin_collect__15714", .68f); nextCoinContactSound = Time.unscaledTime + .07f; }
                        }
                    }
                    ReferenceCoinAnimation.Apply(pile[i], t + phase[i], 64);
                }
                yield return null;
            }
            foreach (var c in pile) if (c) Destroy(c.gameObject);
            if (plus) Destroy(plus.gameObject);
            done?.Invoke();
        }

        static bool Affordable(int starsLeft)
        {
            var area = RoyalThrone.Presentation.RoyalAreas.CurrentArea; if (area == null || area.tasks == null) return false;
            for (int i = 0; i < area.tasks.Length; i++) if (!RoyalThrone.Presentation.RoyalAreas.IsDone(i)) return area.tasks[i].cost <= starsLeft;
            return false;
        }

        // ---------------------------------------------------------------- Areas (RoyalAreaView): stars, streaks, puffs, the chest

        static RoyalWinSequence Area()
        {
            var lm = LevelManager.THIS; if (!lm) return null;
            var s = Get(lm); s.reduced = PlayerPrefs.GetInt("RoyalThrone.HomeMotion", 1) == 0; return s;
        }

        /// <summary>
        /// A star leaves the counter (<paramref name="depart"/> as it goes) and arcs out to the left and down, then in to Do It
        /// (<paramref name="land"/>), trailing sparkles: the reference's spend (GAPS.md A-09). False when there is no overlay.
        /// </summary>
        public static bool AreaStar(Transform from, Transform to, float delay, float flight, global::System.Action depart, global::System.Action land)
        {
            var s = Area(); if (!s || !from || !to) return false;
            s.StartCoroutine(s.RunAreaStar(from, to, delay, flight, depart, land)); return true;
        }
        IEnumerator RunAreaStar(Transform from, Transform to, float delay, float flight, global::System.Action depart, global::System.Action land)
        {
            yield return Scaled();
            float end = Time.unscaledTime + delay; while (Time.unscaledTime < end) yield return null;
            if (!from || !to) { depart?.Invoke(); land?.Invoke(); yield break; }
            Vector2 a = Local(from), b = Local(to), c = new Vector2(Mathf.Min(a.x, b.x) - 330, Mathf.Lerp(a.y, b.y, .45f));
            var art = RoyalThrone.Presentation.RoyalAreaKit.Star ? RoyalThrone.Presentation.RoyalAreaKit.Star : star;
            var img = Picture("Area star", root, art, a, new Vector2(80, 80), Color.white);
            depart?.Invoke();
            float t = 0;
            while (t < flight)
            {
                t += Time.unscaledDeltaTime; float u = Mathf.Clamp01(t / flight), w = u * u * (3 - 2 * u);
                Vector2 p = (1 - w) * (1 - w) * a + 2 * (1 - w) * w * c + w * w * b;
                img.rectTransform.anchoredPosition = p; img.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.1f, .72f, u);
                if (!reduced && Random.value < .75f) Spawn(1, p + Random.insideUnitCircle * 10, Random.insideUnitCircle * 40, .45f, Random.Range(12f, 20f), new Color(1, .92f, .6f));
                yield return null;
            }
            Destroy(img.gameObject);
            if (to) { var p2 = Local(to); SparkleBurst(p2, 6, 200, Gold, 20); RingFlash(p2, 110, new Color(1, .95f, .7f)); }
            RoyalAudioDirector.Get?.Play(RoyalCue.Create);
            land?.Invoke();
        }

        /// <summary>A light streak from a new object to the Area card, landing with a ring (A-13).</summary>
        public static bool AreaStreak(Transform from, Transform to, float duration, global::System.Action land)
        {
            var s = Area(); if (!s || !from || !to) return false;
            s.StartCoroutine(s.RunAreaStreak(from, to, duration, land)); return true;
        }
        IEnumerator RunAreaStreak(Transform from, Transform to, float duration, global::System.Action land)
        {
            yield return Scaled();
            Vector2 a = Local(from), b = Local(to), c = new Vector2(Mathf.Lerp(a.x, b.x, .25f), Mathf.Max(a.y, b.y) + 160);
            var halo = Picture("Area streak", root, soft, a, new Vector2(130, 130), new Color(1, .93f, .55f, 1)); halo.preserveAspect = false;
            var core = Picture("Area streak core", root, sparkle, a, new Vector2(84, 84), Color.white);
            float t = 0;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime; float u = Mathf.Clamp01(t / duration), w = u * u;
                Vector2 p = (1 - w) * (1 - w) * a + 2 * (1 - w) * w * c + w * w * b;
                halo.rectTransform.anchoredPosition = p; core.rectTransform.anchoredPosition = p; core.rectTransform.localRotation = Quaternion.Euler(0, 0, t * 360);
                int n = reduced ? 2 : 5;
                for (int i = 0; i < n; i++) Spawn(1, p + Random.insideUnitCircle * 14, Random.insideUnitCircle * 26, Random.Range(.5f, .85f), Random.Range(18f, 30f), i % 2 == 0 ? Color.white : new Color(1, .86f, .38f));
                var trail = Spawn(3, p, Vector2.zero, .45f, 70, new Color(1, .9f, .5f, .75f)); if (trail != null) trail.grow = 10;
                yield return null;
            }
            Destroy(halo.gameObject); Destroy(core.gameObject);
            RingFlash(b, 190, new Color(1, .92f, .6f)); SparkleBurst(b, 10, 260, Gold, 22);
            RoyalAudioDirector.Get?.Play(RoyalCue.Click);
            land?.Invoke();
        }

        /// <summary>Dust and sparkles at a new object's foot as it rises (A-12). <paramref name="width"/> in Home canvas units.</summary>
        public static void AreaPuff(Transform at, float width)
        {
            var s = Area(); if (!s || !at) return;
            s.StartCoroutine(s.RunAreaPuff(at, width));
        }
        IEnumerator RunAreaPuff(Transform at, float width)
        {
            yield return Scaled();
            if (!at) yield break;
            var p = Local(at); float w = Mathf.Clamp(width * 1.1f, 120, 700);
            int puffs = reduced ? 3 : 9;
            for (int i = 0; i < puffs; i++)
            {
                var b = Spawn(3, p + new Vector2(Random.Range(-.45f, .45f) * w, Random.Range(-10f, 30f)), new Vector2(Random.Range(-.5f, .5f) * w, Random.Range(40f, 120f)),
                              Random.Range(.6f, .9f), Random.Range(.25f, .4f) * w, new Color(1, .97f, .92f, .8f));
                if (b != null) { b.grow = w * .3f; b.drag = 2.5f; }
            }
            SparkleBurst(p + new Vector2(0, 40), 12, 320, Color.white, 22);
            RoyalAudioDirector.Get?.Play(RoyalCue.Create);
        }

        /// <summary>The chest opening: a warm flash, a ring and a spray of sparkles (A-19).</summary>
        public static void AreaBurst(Transform at, float scale)
        {
            var s = Area(); if (!s || !at) return;
            var p = s.Local(at);
            var core = s.Spawn(3, p, Vector2.zero, .5f, 220 * scale, new Color(1, .95f, .75f)); if (core != null) core.grow = 320 * scale;
            s.RingFlash(p, 320 * scale, s.GoldTint); s.SparkleBurst(p, 26, 560, Gold, 28);
            RoyalAudioDirector.Get?.Play(RoyalCue.Create);
        }
        Color GoldTint => new Color(1, .9f, .55f);

        /// <summary>Confetti over the whole screen for "Area Completed!".</summary>
        public static void AreaConfetti()
        {
            var s = Area(); if (!s) return;
            s.StartCoroutine(s.RunAreaConfetti());
        }
        IEnumerator RunAreaConfetti()
        {
            yield return Scaled();
            ConfettiBurst(new Vector2(-Half.x, Half.y * .85f), -35, 28, 46);
            ConfettiBurst(new Vector2(Half.x, Half.y * .85f), 215, 28, 46);
            float t = 0;
            while (t < 2.4f)
            {
                t += Time.unscaledDeltaTime;
                if (!reduced && Random.value < .6f)
                    Spawn(0, new Vector2(Random.Range(-Half.x, Half.x), Half.y + 30), new Vector2(Random.Range(-60f, 60f), Random.Range(-260f, -120f)), Random.Range(2.2f, 3f), Random.Range(20f, 30f), Confetti[Random.Range(0, Confetti.Length)]);
                yield return null;
            }
        }

        /// <summary>A ring of gold sparkles opening over a newly unlocked area.</summary>
        public static void AreaUnlockRing(Transform at)
        {
            var s = Area(); if (!s || !at) return;
            var p = s.Local(at);
            s.RingFlash(p, 520, s.GoldTint); s.SparkleBurst(p, 36, 700, Gold, 26);
        }

        /// <summary>A reward's pieces fly from where it is shown to a counter; <paramref name="land"/> runs once per piece. Coins
        /// turn as every coin flight does; anything else (a booster) keeps its own face on the way.</summary>
        public static bool AreaReward(Transform from, Transform to, int pieces, bool coins, global::System.Action land)
        {
            var s = Area(); if (!s || !from || !to) return false;
            var img = from.GetComponent<Image>();
            var art = img && img.sprite ? img.sprite : s.coin ? s.coin : RoyalPieceSkin.Art("coin");
            s.StartCoroutine(s.RunAreaReward(from, to, pieces, art, coins, land)); return true;
        }
        IEnumerator RunAreaReward(Transform from, Transform to, int pieces, Sprite art, bool coins, global::System.Action land)
        {
            int generation = previewGeneration;
            yield return Scaled();
            if (generation != previewGeneration) yield break;
            if (!from || !to) { for (int i = 0; i < pieces; i++) land?.Invoke(); yield break; }
            Vector2 a = Local(from), b = Local(to) + new Vector2(-60, 0);
            for (int i = 0; i < pieces; i++) Fly(art, a, b, i * .05f, 76, .6f, () => { RoyalAudioDirector.Get?.Play(RoyalCue.Click); land?.Invoke(); }, coins);
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
            if (root) Destroy(root.gameObject);
            // The factory's textures are shared and cached: destroy only the sprites, and the textures made here.
            foreach (var s in new[] { piece, soft, pennant }) if (s && s.texture) Destroy(s.texture);
            foreach (var s in new[] { sparkle, star, glow, ring, piece, soft, pennant }) if (s) Destroy(s);
        }
    }
}
