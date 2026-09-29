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
    /// The end of a won level, staged as the reference build shows it (Design/RoyalMatchReference/OBSERVATIONS.md):
    /// 1. <see cref="Celebrate"/> as soon as the goals are done, before the moves left are used: the screen dims, the
    ///    Royal Throne logo drops in under hanging pennants, with fireworks and confetti (about 2.7 s).
    /// 2. <see cref="BeginFinale"/>: the HUD's goal panel turns into a Reward coin counter; while the booster session
    ///    (ReferenceWinFinish) turns the moves left into rockets and fires them, coins fly from the board into it, and
    ///    <see cref="EndFinale"/> lands the rest.
    /// 3. <see cref="Reward"/> on the win card: a sparkle as each star appears; on the verified opening levels (1-5), which
    ///    award one star, a single large star drops onto the card in place of the star row and the score.
    /// 4. After CONTINUE, on Home: a tip card for the piece the next levels teach, then the coins and the star fly into the
    ///    Home counters, which count up as they land, and the Area task list opens if its next task is affordable.
    /// Presentation only: LevelManager credits the coins and stars before the card opens. Nothing here takes input. A tap
    /// shortens the celebration (LevelManager.SkipWinCelebration). No squash or overshoot, like the cards around it.
    /// </summary>
    public sealed class RoyalWinSequence : MonoBehaviour
    {
        // The user's decision (2026-09-27): a win pays 10 coins plus 5 per star. (The reference's own amounts vary with
        // the finale and were not inferred.)
        public static int Coins(int stars) => 10 + 5 * Mathf.Clamp(stars, 1, 3);

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

        // ---------------------------------------------------------------- overlay

        RectTransform root; Font font; Sprite sparkle, star, glow, ring, piece, soft, coin, pennant, logo;
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
            var canvas = t.GetComponentInParent<Canvas>(); canvas = canvas ? canvas.rootCanvas : null;
            Camera cam = null;
            if (canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay) cam = canvas.worldCamera ? canvas.worldCamera : Camera.main;
            var screen = RectTransformUtility.WorldToScreenPoint(cam, t.position);
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
        void FireworkBurst(Vector2 at, Color color)
        {
            var core = Spawn(3, at, Vector2.zero, .34f, 120, new Color(1, .97f, .86f)); if (core != null) core.grow = 170;
            RingFlash(at, 220, color);
            int count = reduced ? 8 : 26;
            for (int i = 0; i < count; i++)
            {
                float a = (i + Random.value * .5f) * Mathf.PI * 2 / count, v = 700 * Random.Range(.6f, 1f);
                var b = Spawn(1, at, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * v, Random.Range(.75f, 1.05f), Random.Range(30f, 42f), i % 3 == 0 ? Color.white : color);
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
            if (pending != null && !homeRunning && lm && lm.gameStatus == GameState.Map) homeReward = StartCoroutine(HomeRewards(lm));
        }

        // ---------------------------------------------------------------- coin (and star) flights

        sealed class Flight
        {
            public Image image; public Vector2 from, burst, control, to;
            public float start, fly, size, hold; public global::System.Action land;
            public bool landed, turning, started, trail;
            public ReferenceCoinParticles.Handle tracer;
        }
        readonly List<Flight> flights = new List<Flight>();
        readonly Stack<Image> coinImages = new Stack<Image>();
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

        void Fly(Sprite art, Vector2 from, Vector2 to, float delay, float size, float fly, global::System.Action land, bool turning = true, float spread = 120, float hold = 0, bool trail = true)
        {
            float a = Random.Range(0, Mathf.PI * 2), r = Random.Range(spread * .4f, spread);
            var f = new Flight { from = from, burst = from + new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r * .75f), to = to, start = Time.unscaledTime + delay, fly = turning ? .6f : fly, land = land, turning = turning, size = size, hold = hold, trail = trail };
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
                float e = now - f.start; if (e < 0) continue;
                float burstTime = f.turning ? .55f + f.hold : .2f;
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
                f.image.rectTransform.anchoredPosition = p;
                f.image.rectTransform.localScale = Vector3.one * scale;
                if (f.turning) ReferenceCoinAnimation.Apply(f.image, reduced ? 0 : e, f.size);
            }
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
            foreach (var name in new[] { "coin_appear__rfv387_17675", "coin_counter__rfv387_17638", "coin_collect__15714" })
                RoyalAudioDirector.Get?.StopClip(ReferenceGameplayAudio.Load(name));
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

        public static IEnumerator Celebrate(LevelManager lm)
        {
            var s = Get(lm); yield return s.RunCelebration(lm);
        }

        IEnumerator RunCelebration(LevelManager lm)
        {
            reduced = PlayerPrefs.GetInt("RoyalThrone.HomeMotion", 1) == 0;
            // As in the reference, the move that met the goals plays out (its chain and the refill) before the screen dims;
            // at most 2.5 s, and a tap cuts it short.
            float settle = Time.unscaledTime + 2.5f;
            while (Time.unscaledTime < settle && lm.gameStatus == GameState.PreWinAnimations && !lm.SkipWinCelebration &&
                   (lm.IsResolvingMatches || RoyalPowerupDirector.Busy || lm.GetItems().Exists(i => i && (i.destroying || i.falling))))
                yield return null;
            if (lm.gameStatus != GameState.PreWinAnimations) yield break;
            // An overlay made this frame may not have its screen size yet; everything below is laid out from it.
            yield return Scaled();
            // The existing "complete" cue (the celebration fanfare through RoyalAudioDirector); music is untouched.
            if (SoundBase.Instance && SoundBase.Instance.complete != null && SoundBase.Instance.complete.Length > 0) SoundBase.Instance.PlaySound(SoundBase.Instance.complete[0]);
            var h = Half; float y = h.y * .22f;
            var stage = Node("Level won", root, Vector2.zero, Size);
            var group = stage.gameObject.AddComponent<CanvasGroup>(); group.alpha = 1;
            var dim = Picture("Dim", stage, piece, Vector2.zero, Size + new Vector2(40, 40), new Color(Navy.r, Navy.g, Navy.b, 0)); dim.preserveAspect = false;
            // Pennants hanging from the top edge, dropping in one after another and swaying.
            var flags = new List<RectTransform>(); float[] xs = { -.84f, -.54f, -.25f, .25f, .54f, .84f };
            foreach (var fx in xs)
            {
                var f = Picture("Pennant", stage, pennant, new Vector2(fx * h.x, h.y + 20), new Vector2(92, 230), Color.white).rectTransform;
                f.pivot = new Vector2(.5f, 1); f.localScale = Vector3.one * (Mathf.Abs(fx) > .7f ? 1.1f : .9f); flags.Add(f);
            }
            var halo = Picture("Logo halo", stage, soft, new Vector2(0, y), new Vector2(760, 520), new Color(1, .82f, .40f, 0)); halo.preserveAspect = false;
            RectTransform lockup;
            if (logo)
            {
                var img = Picture("Royal Throne logo", stage, logo, new Vector2(0, y), new Vector2(560, 560 * logo.rect.height / Mathf.Max(1, logo.rect.width)), new Color(1, 1, 1, 0));
                lockup = img.rectTransform;
            }
            else lockup = Words("Royal Throne", stage, "ROYAL THRONE", 96, new Vector2(0, y), new Vector2(700, 160), new Color(Gold.r, Gold.g, Gold.b, 0), 4).rectTransform;
            var graphic = lockup.GetComponent<Graphic>();

            ConfettiBurst(new Vector2(-h.x + 30, -h.y * .55f), 68, 14, 48);
            ConfettiBurst(new Vector2(h.x - 30, -h.y * .55f), 112, 14, 48);
            var shells = new[] { new Vector2(-250, y + 150), new Vector2(260, y + 90), new Vector2(-30, y - 230), new Vector2(210, y - 190) };
            var colors = new[] { Gold, new Color(.62f, .80f, 1f), new Color(1f, .62f, .68f), Gold };
            var at = new[] { .25f, .55f, .85f, 1.15f }; int fired = 0;
            // Its reaction makes the king in the HUD portrait cheer.
            if (RoyalPowerupDirector.Get && lm.GameField) RoyalPowerupDirector.Get.Creation(lm.GameField.position);

            float t = 0, hold = 2.2f, exit = .45f;
            while (true)
            {
                if (lm.gameStatus != GameState.PreWinAnimations) { foreach (var b in bits) b.age = b.life; break; }
                t += Time.unscaledDeltaTime;
                // A tap (the same one that sped up the booster session) jumps to the exit.
                if (lm.SkipWinCelebration && t < hold) hold = Mathf.Max(.35f, t);
                while (fired < at.Length && t >= at[fired] && t < hold) { FireworkBurst(shells[fired], colors[fired]); fired++; }
                float outU = Mathf.Clamp01((t - hold) / exit), dimIn = EaseOut(t / .3f);
                dim.color = new Color(Navy.r * .5f, Navy.g * .5f, Navy.b * .5f, .74f * dimIn * (1 - outU));
                // The logo drops from above the screen, settles without a bounce, and lifts away at the end.
                float drop = EaseOut((t - .12f) / .5f), show = Mathf.Clamp01((t - .12f) / .2f) * (1 - outU);
                lockup.anchoredPosition = new Vector2(0, y + (reduced ? 0 : (1 - drop) * 380 + outU * 40));
                lockup.localScale = Vector3.one * (reduced ? 1 : Mathf.Lerp(1.12f, 1, drop));
                if (graphic) graphic.color = new Color(graphic.color.r, graphic.color.g, graphic.color.b, show);
                halo.color = new Color(1, .82f, .40f, .30f * show); halo.rectTransform.localScale = Vector3.one * (1 + .05f * Mathf.Sin(t * 2.4f));
                for (int i = 0; i < flags.Count; i++)
                {
                    float fin = EaseOut((t - .05f - i % 3 * .07f) / .45f);
                    flags[i].anchoredPosition = new Vector2(xs[i] * h.x, h.y + 20 + (1 - fin) * 260 + outU * 260);
                    flags[i].localRotation = Quaternion.Euler(0, 0, reduced ? 0 : Mathf.Sin(t * 2.1f + i * 1.3f) * 4);
                }
                if (!reduced && t < hold && Random.value < .3f)
                    SparkleBurst(new Vector2(Random.Range(-250f, 250f), Random.Range(-120f, 120f)) + lockup.anchoredPosition, 1, 60, Gold, 20);
                if (t >= hold + exit) break;
                yield return null;
            }
            Destroy(stage.gameObject);
        }

        // ---------------------------------------------------------------- 2. the finale: a Reward counter in the HUD

        RectTransform rewardContents; Text rewardCount; Image rewardIcon; Text goalHeading; GameObject goalContents; string goalWords;
        bool finale; int rewardShown, rewardTarget; float nextCoin; LevelManager finaleLevel;

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
            rewardShown = 0; rewardTarget = Coins(lm.stars); finale = true; finaleLevel = lm; nextCoin = Time.unscaledTime + .4f;
            // The booster session takes about 0.3 s per move left (a flight every 0.12 s, then each rocket 0.18 s apart), so
            // the coins are spread over it rather than all leaving in its first seconds.
            coinGap = Mathf.Clamp(.3f * Mathf.Max(1, lm.Limit) / Mathf.Max(1, rewardTarget - 3), .15f, .8f);
        }
        float coinGap = .25f;

        // While the booster session runs, a coin leaves a random board piece for the Reward counter every coinGap seconds,
        // keeping a few in hand for the end; each adds one to the counter as it lands.
        void UpdateFinale()
        {
            if (!finale) return;
            var lm = finaleLevel;
            if (!lm || (lm.gameStatus != GameState.PreWinAnimations && lm.gameStatus != GameState.Win)) { finale = false; RestoreHud(); return; }
            if (lm.gameStatus != GameState.PreWinAnimations) return;
            rewardTarget = Mathf.Max(rewardTarget, Coins(lm.stars));
            if (Time.unscaledTime < nextCoin || !rewardIcon) return;
            nextCoin = Time.unscaledTime + (lm.SkipWinCelebration ? .08f : coinGap);
            int inFlight = 0; foreach (var f in flights) if (!f.landed) inFlight++;
            if (rewardShown + inFlight >= rewardTarget - 3) return;
            var items = lm.GetItems(); if (items == null || items.Count == 0) return;
            var it = items[Random.Range(0, items.Count)]; if (!it) return;
            Fly(coin, World(it.transform.position), Local(rewardIcon.transform), 0, 54, .6f, () => { rewardShown = Mathf.Min(rewardTarget, rewardShown + 1); if (rewardCount) rewardCount.text = rewardShown.ToString(); if (rewardIcon) PulseCoin(rewardIcon.rectTransform); });
        }

        IEnumerator FinishFinale(LevelManager lm)
        {
            if (!finale || !rewardIcon) yield break;
            rewardTarget = Mathf.Max(rewardTarget, Coins(lm.stars));
            int inFlight = 0; foreach (var f in flights) if (!f.landed) inFlight++;
            int left = rewardTarget - rewardShown - inFlight;
            var from = lm.GameField ? World(lm.GameField.position) : Vector2.zero;
            if (lm.GameField && lm.maxRows > 0) from = World(lm.GameField.position + new Vector3((lm.maxCols - 1) * lm.squareWidth * .5f, -(lm.maxRows - 1) * lm.squareHeight * .5f, 0));
            int n = Mathf.Clamp(left, 0, 8), each = n > 0 ? left / n : 0, extra = n > 0 ? left - each * n : 0;
            for (int i = 0; i < n; i++)
            {
                int amount = each + (i < extra ? 1 : 0);
                Fly(coin, from, Local(rewardIcon.transform), i * .05f, 54, .5f, () => { rewardShown = Mathf.Min(rewardTarget, rewardShown + amount); if (rewardCount) rewardCount.text = rewardShown.ToString(); });
            }
            float end = Time.unscaledTime + 2.4f;
            while (Time.unscaledTime < end && rewardShown < rewardTarget && lm.gameStatus == GameState.PreWinAnimations) yield return null;
            rewardShown = rewardTarget; if (rewardCount) rewardCount.text = rewardShown.ToString();
            yield return new WaitForSecondsRealtime(.25f);
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

        sealed class Pending { public int coins, before, stars, level; public bool opening; }
        Pending pending; bool homeRunning; int homeCoins;
        Coroutine reward, homeReward, risingReward;
        Canvas.WillRenderCanvases homeHold;
        RoyalThrone.Presentation.RoyalHomeScreen heldHome;
        readonly List<GameObject> rewardObjects = new List<GameObject>();
        readonly List<Graphic> hiddenRewardGraphics = new List<Graphic>();

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
            ReleaseHomeHold();
            if (reward != null) StopCoroutine(reward);
            if (homeReward != null) StopCoroutine(homeReward);
            if (risingReward != null) StopCoroutine(risingReward);
            reward = homeReward = risingReward = null;
            homeRunning = false;
            homeCoins = InitScript.Gems;
            ClearFlights();
            ClearPill();
            foreach (var graphic in hiddenRewardGraphics) if (graphic) graphic.enabled = true;
            hiddenRewardGraphics.Clear();
            // Unity may stop a disabled host without disposing every nested enumerator.
            // Keep explicit ownership of card, tip and floating-label objects for that case.
            foreach (var obj in rewardObjects) if (obj) Destroy(obj);
            rewardObjects.Clear();
        }

        public static void Reward(LevelManager lm, int coins, int balanceBefore)
        {
            var s = Get(lm); s.CancelRewardPresentation();
            bool opening = RoyalThrone.Campaign.RoyalReferenceCampaign.Opening;
            // Home plays the rewards only after the verified opening levels, whose CONTINUE goes Home (AnimationManager).
            // Later levels go on to the next level's card, so their coins fly on the win card as before.
            s.pending = opening ? new Pending { coins = coins, before = balanceBefore, stars = lm.StarsGained, level = lm.currentLevel, opening = true } : null;
            s.reward = s.StartCoroutine(opening ? s.RunCard() : s.RunCardCoins(lm, coins, balanceBefore));
        }

        // ---- later levels: coins from the card's stars into a coin balance pill above the card (unchanged from v4)

        RectTransform pill; Text pillValue; Image pillFlash; float flash;
        float PillY => Half.y - (Screen.height - Screen.safeArea.yMax) * root.rect.width / Mathf.Max(1, Screen.width) - 76;
        Vector2 PillCoin => pill ? pill.anchoredPosition + new Vector2(-118, 0) : Vector2.zero;

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
            float t0 = Time.unscaledTime; int expected = Mathf.Clamp(lm.stars, 1, 3), count = 0; var seen = new bool[3]; Vector2 starRow = Vector2.zero;
            while (card.gameObject.activeInHierarchy && Time.unscaledTime - t0 < 4.5f)
            {
                float e = Time.unscaledTime - t0;
                pillGroup.alpha = EaseOut((e - .15f) / .25f); pill.anchoredPosition = new Vector2(0, PillY + 16 * (1 - EaseOut((e - .15f) / .25f)));
                for (int i = 0; i < 3; i++)
                {
                    var s = card.Find("Image/Star" + (i + 1));
                    if (!s || seen[i] || !s.gameObject.activeInHierarchy) continue;
                    seen[i] = true; count++; var p = Local(s); if (i == 1 || starRow == Vector2.zero) starRow = p;
                    SparkleBurst(p, 16, 330, Gold, 26); RingFlash(p, 150, new Color(1, .9f, .55f));
                }
                if (count >= expected && e > .5f) break;
                yield return null;
            }
            if (!card.gameObject.activeInHierarchy) { ClearPill(); yield break; }
            if (starRow == Vector2.zero) { var s2 = card.Find("Image/Star2"); starRow = s2 ? Local(s2) : Vector2.zero; }
            float wait = Time.unscaledTime + .32f;
            while (card.gameObject.activeInHierarchy && Time.unscaledTime < wait) yield return null;
            pillGroup.alpha = 1; pill.anchoredPosition = new Vector2(0, PillY);
            int flightsN = reduced ? 6 : 10, landedN = 0; coin = coin ? coin : RoyalPieceSkin.Art("coin");
            for (int i = 0; i < flightsN; i++)
                Fly(coin, starRow, PillCoin, .02f * i, 66, .6f, () =>
                {
                    landedN++; int shown = landedN >= flightsN ? before + coins : before + Mathf.RoundToInt(coins * (float)landedN / flightsN);
                    if (pillValue) pillValue.text = shown.ToString(); flash = 1;
                    if (landedN == 1) risingReward = StartCoroutine(RisePlus(coins));
                    if (landedN == 1) ReferenceGameplayAudio.Play("coin_counter__rfv387_17638", .55f);
                }, true, 100, i * .09f, false);
            while (card.gameObject.activeInHierarchy && landedN < flightsN) yield return null;
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

        // Levels 1-5: one progression star per win, as on the reference card. The row of three and the score give way to a
        // single large star that drops onto the card; they are switched back on when the card closes (it is reused).
        IEnumerator RunCard()
        {
            reduced = PlayerPrefs.GetInt("RoyalThrone.HomeMotion", 1) == 0;
            var menus = MenuManager.Instance; var card = menus ? menus.GetCanvasGlobal().Find("MenuComplete") : null;
            if (!card) yield break;
            float t0 = Time.unscaledTime; var hidden = hiddenRewardGraphics;
            try
            {
                var slot = card.Find("Image/Star2"); if (!slot) slot = card.Find("Image/Star1");
                Vector2 at = slot ? Local(slot) + new Vector2(0, -10) : new Vector2(0, 120);
                var big = Picture("Level star", root, star, at + new Vector2(0, 420), new Vector2(200, 200), new Color(1, 1, 1, 0));
                var bigGlow = Picture("Level star glow", root, soft, at, new Vector2(330, 330), new Color(1, .86f, .45f, 0)); bigGlow.preserveAspect = false;
                TrackRewardObject(big.gameObject); TrackRewardObject(bigGlow.gameObject);
                big.rectTransform.SetAsLastSibling();
                Sprite starArt = null; var s1 = card.Find("Image/Star1"); var si = s1 ? s1.GetComponent<Image>() : null; if (si && si.sprite) starArt = si.sprite; if (starArt) big.sprite = starArt;
                bool landed = false; float fall = 0;
                while (card && card.gameObject.activeInHierarchy)
                {
                    HideRowAndScore(card, hidden);
                    float e = Time.unscaledTime - t0;
                    if (e > .35f)
                    {
                        fall += Time.unscaledDeltaTime; float u = EaseOut(fall / .5f);
                        big.rectTransform.anchoredPosition = at + new Vector2(0, (1 - u) * 420);
                        big.rectTransform.localScale = Vector3.one * (reduced ? 1 : Mathf.Lerp(1.6f, 1, u));
                        big.rectTransform.localRotation = Quaternion.Euler(0, 0, (1 - u) * -18);
                        big.color = new Color(1, 1, 1, Mathf.Clamp01(fall / .12f));
                        if (!landed && u >= 1)
                        {
                            landed = true; SparkleBurst(at, 18, 360, Gold, 26); RingFlash(at, 220, new Color(1, .9f, .55f));
                            RoyalAudioDirector.Get?.Play(RoyalCue.Create);
                        }
                        if (landed) { float g = Time.unscaledTime - t0; bigGlow.color = new Color(1, .86f, .45f, .35f + .1f * Mathf.Sin(g * 2.5f)); if (!reduced && Random.value < .06f) SparkleBurst(at + Random.insideUnitCircle * 90, 1, 40, Gold, 18); }
                    }
                    yield return null;
                }
                Destroy(big.gameObject); Destroy(bigGlow.gameObject);
            }
            finally { foreach (var g in hidden) if (g) g.enabled = true; }
        }

        // Hides the star row and the score, remembering each graphic it switched off (the animator may re-enable them).
        static void HideRowAndScore(Transform card, List<Graphic> hidden)
        {
            void Hide(Graphic g) { if (!g.enabled) return; g.enabled = false; if (!hidden.Contains(g)) hidden.Add(g); }
            for (int i = 1; i <= 3; i++) { var s = card.Find("Image/Star" + i); if (s) foreach (var g in s.GetComponentsInChildren<Graphic>(true)) Hide(g); }
            foreach (var g in card.GetComponentsInChildren<Graphic>(true))
                if (g.name.IndexOf("Score", global::System.StringComparison.OrdinalIgnoreCase) >= 0 || g.transform.parent && g.transform.parent.name.IndexOf("Score", global::System.StringComparison.OrdinalIgnoreCase) >= 0) Hide(g);
        }

        // ---------------------------------------------------------------- 4. Home: tip, coins, star, tasks

        // The tip shown on the way home, per level just won, as the reference build does (Rocket, Rocket, TNT, Propeller,
        // then the five-in-a-row piece). Our own wording; our pieces.
        static bool Tip(int level, out string title, out ItemsTypes power, out int length, out bool square, out bool corner)
        {
            title = null; power = ItemsTypes.NONE; length = 4; square = false; corner = false;
            switch (level)
            {
                case 1: case 2: title = "Rocket"; power = ItemsTypes.VERTICAL_STRIPPED; length = 4; return true;
                case 3: title = "TNT"; power = ItemsTypes.PACKAGE; corner = true; return true;
                case 4: title = "Propeller"; power = ItemsTypes.HELICOPTER; square = true; return true;
                case 5: title = "Color Ball"; power = ItemsTypes.BOMB; length = 5; return true;
            }
            return false;
        }

        IEnumerator HomeRewards(LevelManager lm)
        {
            homeRunning = true; var p = pending; pending = null;
            try
            {
                reduced = PlayerPrefs.GetInt("RoyalThrone.HomeMotion", 1) == 0;
                var home = FindAnyObjectByType<RoyalThrone.Presentation.RoyalHomeScreen>();
                float wait = Time.unscaledTime + 3; while ((!home || !home.coinLabel || !home.coinLabel.gameObject.activeInHierarchy) && Time.unscaledTime < wait) { yield return null; home = home ? home : FindAnyObjectByType<RoyalThrone.Presentation.RoyalHomeScreen>(); }
                if (!home || !home.coinLabel) yield break;
                // Hold the counters at their old values while the tip shows and the rewards fly.
                homeCoins = p.before; home.RefreshProgress(); int starsNow = home.Progress.available; int starsShown = Mathf.Max(0, starsNow - p.stars);
                void Hold() { if (home.coinLabel) home.coinLabel.text = homeCoins.ToString("N0"); if (home.starLabel) home.starLabel.text = starsShown.ToString(); }
                heldHome = home; homeHold = Hold; Canvas.willRenderCanvases += homeHold;
                try
                {
                    if (Tip(p.level, out var title, out var power, out var length, out var square, out var corner)) yield return ShowTip(title, power, length, square, corner);
                    // The coins: "+N" over the scene, then the coins fly to the coin counter, which counts up as they land.
                    var receiver = (home.coinLabel.transform.parent.Find("Imported 22081") ?? home.coinLabel.transform.parent.Find("Coins icon")) as RectTransform;
                    var coinTo = receiver ? Local(receiver) : Local(home.coinLabel.transform) + new Vector2(-70, 0); var from = new Vector2(0, -Half.y * .18f);
                    var plus = Words("Coins won", root, "+" + p.coins, 64, from, new Vector2(300, 90), Gold, 3);
                    TrackRewardObject(plus.gameObject);
                    int flightsN = reduced ? 6 : 10, landedN = 0; coin = coin ? coin : RoyalPieceSkin.Art("coin");
                    for (int i = 0; i < flightsN; i++)
                    {
                        Fly(coin, from, coinTo, .25f + i * .02f, 70, .6f, () =>
                        {
                            landedN++; homeCoins = landedN >= flightsN ? p.before + p.coins : p.before + Mathf.RoundToInt(p.coins * (float)landedN / flightsN);
                            PulseCoin(receiver);
                            if (landedN == 1) ReferenceGameplayAudio.Play("coin_counter__rfv387_17638", .55f);
                        }, true, 48, i * .09f, false);
                    }
                    float t = 0; while (landedN < flightsN && t < 4) { t += Time.unscaledDeltaTime; if (plus) { plus.rectTransform.anchoredPosition = from + new Vector2(0, 40 * EaseOut(t / .8f)); plus.color = new Color(Gold.r, Gold.g, Gold.b, 1 - Mathf.Clamp01((t - .9f) / .4f)); } yield return null; }
                    homeCoins = p.before + p.coins; if (plus) Destroy(plus.gameObject);
                    // The star: from the middle of the scene to the star counter.
                    if (p.stars > 0 && home.starLabel)
                    {
                        bool starLanded = false; var starTo = Local(home.starLabel.transform) + new Vector2(-60, 0);
                        Fly(star, new Vector2(0, -Half.y * .05f), starTo, .1f, 110, .7f, () => { starLanded = true; starsShown = starsNow; RoyalAudioDirector.Get?.Play(RoyalCue.Create); }, false, 60);
                        t = 0; while (!starLanded && t < 3) { t += Time.unscaledDeltaTime; yield return null; }
                        starsShown = starsNow;
                    }
                    yield return new WaitForSecondsRealtime(.25f);
                }
                finally { ReleaseHomeHold(); }
                // The task list opens by itself when the current area's next task can be paid for.
                // The very first time, the reference points at the Area card instead (RoyalAreaView.GuideFirstTask).
                if (lm.gameStatus == GameState.Map && Affordable(starsNow) && home.areaButton && !(home.AreaView && home.AreaView.GuideFirstTask())) home.OpenPanel("area", home.areaButton.gameObject);
            }
            finally { homeRunning = false; homeReward = null; ReleaseHomeHold(); }
        }

        static bool Affordable(int starsLeft)
        {
            var area = RoyalThrone.Presentation.RoyalAreas.CurrentArea; if (area == null || area.tasks == null) return false;
            for (int i = 0; i < area.tasks.Length; i++) if (!RoyalThrone.Presentation.RoyalAreas.IsDone(i)) return area.tasks[i].cost <= starsLeft;
            return false;
        }

        IEnumerator ShowTip(string title, ItemsTypes power, int length, bool square, bool corner)
        {
            var stage = Node("Tip", root, Vector2.zero, Size);
            TrackRewardObject(stage.gameObject);
            var group = stage.gameObject.AddComponent<CanvasGroup>(); group.alpha = 0;
            var back = Picture("Tip backdrop", stage, piece, Vector2.zero, Size + new Vector2(40, 40), new Color(.07f, .16f, .45f, .96f)); back.preserveAspect = false;
            Words("Tip title", stage, title, 76, new Vector2(0, 260), new Vector2(700, 110), Gold, 4);
            // The pieces that make it, an arrow, and the piece itself.
            var tile = RoyalPieceSkin.For(ItemsTypes.NONE, corner ? 1 : square ? 2 : length == 5 ? 4 : 0);
            var cells = new List<Vector2>();
            if (square) cells.AddRange(new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) });
            else if (corner) cells.AddRange(new[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 2), new Vector2(1, 0), new Vector2(2, 0) });
            else for (int i = 0; i < length; i++) cells.Add(new Vector2(0, i));
            float cell = 76; var origin = new Vector2(-200, -40);
            float maxY = 0; foreach (var c in cells) maxY = Mathf.Max(maxY, c.y);
            foreach (var c in cells) Picture("Tip piece", stage, tile, origin + new Vector2(c.x * cell, (c.y - maxY * .5f) * cell), new Vector2(cell - 6, cell - 6), Color.white);
            // The arrow is drawn (a shaft and a two-stroke head), not a text glyph the display font may lack.
            var arrow = Node("Tip arrow", stage, new Vector2(20, -40), new Vector2(120, 60));
            var arrowColor = new Color(1, 1, 1, .92f);
            Picture("Shaft", arrow, piece, new Vector2(-8, 0), new Vector2(84, 14), arrowColor).preserveAspect = false;
            foreach (var sign in new[] { 1f, -1f })
            {
                var stroke = Picture("Head", arrow, piece, new Vector2(24, sign * 13), new Vector2(46, 14), arrowColor);
                stroke.preserveAspect = false; stroke.rectTransform.localRotation = Quaternion.Euler(0, 0, -sign * 40);
            }
            Picture("Tip power", stage, RoyalPieceSkin.For(power, 0), new Vector2(200, -40), new Vector2(170, 170), Color.white);
            float t = 0, hold = 1.3f;
            while (t < hold + .5f)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = Mathf.Clamp01(t / .2f) * (1 - Mathf.Clamp01((t - hold - .2f) / .3f));
                arrow.anchoredPosition = new Vector2(20 + (reduced ? 0 : Mathf.Sin(t * 5) * 8), -40);
                yield return null;
            }
            Destroy(stage.gameObject);
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
            yield return Scaled();
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
