using PlayerPrefs = RoyalThrone.Testing.RoyalSave;
using System.Collections;
using System.Collections.Generic;
using JellyGarden.Scripts.GUI;
using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Juice
{
    /// <summary>
    /// The end of a won level, after the booster session: a short celebration over the board (SPLENDID! lettering,
    /// confetti, fireworks, the king's cheer), then, on the win card, a sparkle burst as each star appears and the coins
    /// earned flying up into a coin counter that ticks up as they land. Presentation only: LevelManager credits the coins
    /// before the card opens. Nothing here takes input. The celebration shortens on a tap (LevelManager.SkipWinCelebration);
    /// the coin flight finishes at once if the card closes early. No squash or overshoot, like the cards around it.
    /// </summary>
    public sealed class RoyalWinSequence : MonoBehaviour
    {
        // The user's decision (2026-09-27): a win pays 10 coins plus 5 per star.
        public static int Coins(int stars) => 10 + 5 * Mathf.Clamp(stars, 1, 3);

        static RoyalWinSequence instance;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetStatics() { instance = null; }
        static RoyalWinSequence Get(LevelManager lm)
        {
            if (!instance) instance = lm.gameObject.AddComponent<RoyalWinSequence>();
            instance.Build(); return instance;
        }

        // ---------------------------------------------------------------- overlay

        RectTransform root; Font font; Sprite sparkle, star, glow, ring, piece, soft, coin;
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
            // A soft round spot, opaque at the centre (the shared SoftGlow is mostly transparent): the word's shade and halo,
            // and the flash at a firework's heart.
            const int N = 64; var spot = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var sp = new Color[N * N];
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
            {
                float dx = (x + .5f) / N * 2 - 1, dy = (y + .5f) / N * 2 - 1, r = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                sp[y * N + x] = new Color(1, 1, 1, Mathf.Pow(1 - r * r, 1.6f));
            }
            spot.SetPixels(sp); spot.Apply(); soft = Sprite.Create(spot, new Rect(0, 0, N, N), new Vector2(.5f, .5f), N);
        }
        static Sprite Shape(Texture2D t) => Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(.5f, .5f), t.width);

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
        Vector2 Half => root.rect.size * .5f;
        // The pill's height below the top edge, clear of a notch: the safe area's top inset in overlay units.
        float PillY => Half.y - (Screen.height - Screen.safeArea.yMax) * root.rect.width / Mathf.Max(1, Screen.width) - 76;
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
        // Confetti drifting down from the top edge, slower than the bursts, still falling as the win card opens.
        void ConfettiRain(int count)
        {
            if (reduced) count = Mathf.Max(4, count / 4);
            var h = Half;
            for (int i = 0; i < count; i++)
            {
                var b = Spawn(0, new Vector2(Random.Range(-h.x * .95f, h.x * .95f), h.y + Random.Range(10f, 160f)),
                              new Vector2(Random.Range(-60f, 60f), Random.Range(-260f, -120f)), Random.Range(2.4f, 3.2f), Random.Range(18f, 30f), Confetti[i % Confetti.Length]);
                if (b != null) { b.gravity = 520; b.drag = 1f; }
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
            UpdateCoins(dt);
        }

        // ---------------------------------------------------------------- the celebration, after the booster session

        public static IEnumerator Celebrate(LevelManager lm)
        {
            var s = Get(lm); yield return s.RunCelebration(lm);
        }

        IEnumerator RunCelebration(LevelManager lm)
        {
            reduced = PlayerPrefs.GetInt("RoyalThrone.HomeMotion", 1) == 0;
            // One word, not a second "level complete": the inherited label said that before the booster session.
            float y = Half.y * .30f;
            var lockup = Node("Splendid", root, new Vector2(0, y), new Vector2(760, 300));
            var group = lockup.gameObject.AddComponent<CanvasGroup>(); group.alpha = 0;
            // A soft navy shade keeps the lettering readable over the busy board; the warm halo sits on it.
            var shade = Picture("Shade", lockup, soft, new Vector2(0, -8), new Vector2(980, 300), new Color(Navy.r, Navy.g, Navy.b, 0));
            shade.preserveAspect = false;
            var halo = Picture("Warm halo", lockup, soft, new Vector2(0, 4), new Vector2(760, 200), new Color(1, .80f, .36f, 0));
            halo.preserveAspect = false;
            // The display font is wide: best fit keeps the word inside 84% of the screen's width (672 of 800 units).
            float halfWord = 336;
            var word = Words("SPLENDID!", lockup, "SPLENDID!", 124, Vector2.zero, new Vector2(halfWord * 2, 176), Gold, 4f);
            word.horizontalOverflow = HorizontalWrapMode.Wrap; word.verticalOverflow = VerticalWrapMode.Truncate;
            word.resizeTextForBestFit = true; word.resizeTextMinSize = 48; word.resizeTextMaxSize = 124;
            word.gameObject.AddComponent<RoyalCelebrationGradient>();
            var flankL = Picture("Sparkle left", lockup, sparkle, new Vector2(-halfWord - 14, 48), new Vector2(64, 64), Gold);
            var flankR = Picture("Sparkle right", lockup, sparkle, new Vector2(halfWord + 10, -44), new Vector2(48, 48), Gold);

            var h = Half;
            ConfettiBurst(new Vector2(-h.x + 30, -h.y * .55f), 68, 14, 56);
            ConfettiBurst(new Vector2(h.x - 30, -h.y * .55f), 112, 14, 56);
            bool rained = false;
            // Fireworks on the overlay around the word; the director's board fireworks under them add their sparks and sound.
            var center = lm.GameField ? lm.GameField.position + new Vector3((lm.maxCols - 1) * lm.squareWidth * .5f, -(lm.maxRows - 1) * lm.squareHeight * .5f, -.2f) : Vector3.zero;
            var bursts = new[] { new Vector3(-1.6f, 1.2f, 0), new Vector3(1.7f, .6f, 0), new Vector3(-.2f, -1.4f, 0) };
            var shells = new[] { new Vector2(-240, y + 170), new Vector2(250, y + 60), new Vector2(-20, y - 240) };
            var colors = new[] { Gold, new Color(.62f, .80f, 1f), new Color(1f, .62f, .68f) };
            var at = new[] { .10f, .38f, .66f }; int fired = 0;
            // A creation burst at the board's centre: its ring and sparkles open the celebration, and its reaction makes the
            // king in the HUD portrait cheer.
            if (RoyalPowerupDirector.Get) RoyalPowerupDirector.Get.Creation(center);
            // A cascade banner still showing from the booster session gives way to the celebration's word.
            var banner = lm.GetComponent<RoyalCelebration>(); if (banner && banner.IsShowing) banner.Cancel();

            float t = 0, hold = 1.45f, exit = .28f;
            while (true)
            {
                // The level was left or restarted mid-celebration (QA tools can): stop at once and clear the overlay.
                if (lm.gameStatus != GameState.PreWinAnimations) { foreach (var b in bits) b.age = b.life; break; }
                t += Time.unscaledDeltaTime;
                // A tap (the same one that sped up the booster session) jumps to the exit.
                if (lm.SkipWinCelebration && t < hold) hold = Mathf.Max(.25f, t);
                while (fired < at.Length && t >= at[fired] && t < hold)
                {
                    var director = RoyalPowerupDirector.Get; if (director) director.Firework(center + bursts[fired] * lm.squareWidth, 1.35f);
                    FireworkBurst(shells[fired], colors[fired]);
                    fired++;
                }
                if (!rained && t >= .15f) { rained = true; ConfettiRain(34); }
                float inU = EaseOut(t / .34f), outU = Mathf.Clamp01((t - hold) / exit), shown = inU * (1 - outU);
                group.alpha = shown;
                lockup.anchoredPosition = new Vector2(0, y + (reduced ? 0 : Mathf.Lerp(-36, 0, inU) + outU * 28));
                lockup.localScale = Vector3.one * (reduced ? 1 : Mathf.Lerp(.9f, 1, inU));
                shade.color = new Color(Navy.r, Navy.g, Navy.b, .62f * shown);
                halo.color = new Color(1, .80f, .36f, .22f * shown); halo.rectTransform.localScale = Vector3.one * (1 + .06f * Mathf.Sin(t * 2.2f));
                // The flanking sparkles twinkle out of step with each other.
                flankL.rectTransform.localScale = Vector3.one * (reduced ? 1 : .75f + .35f * Mathf.Abs(Mathf.Sin(t * 5.2f)));
                flankR.rectTransform.localScale = Vector3.one * (reduced ? 1 : .75f + .35f * Mathf.Abs(Mathf.Sin(t * 5.2f + 1.7f)));
                flankL.rectTransform.localRotation = Quaternion.Euler(0, 0, t * 40); flankR.rectTransform.localRotation = Quaternion.Euler(0, 0, -t * 55);
                if (!reduced && t < hold && Random.value < .35f)
                    SparkleBurst(new Vector2(Random.Range(-halfWord, halfWord), Random.Range(-60f, 90f)) + lockup.anchoredPosition, 1, 60, Gold, 20);
                if (t >= hold + exit) break;
                yield return null;
            }
            Destroy(lockup.gameObject);
        }

        // ---------------------------------------------------------------- the reward, on the win card

        sealed class Flight { public Image image; public Vector2 from, burst, control, to; public float start; public bool landed; }
        readonly List<Flight> flights = new List<Flight>();
        RectTransform pill; Text pillValue; Image pillFlash; float flash;
        int shownBefore, coinsWon, landed, flightCount; bool rewarding;
        Coroutine reward;

        public static void Reward(LevelManager lm, int coins, int balanceBefore)
        {
            var s = Get(lm); if (s.reward != null) s.StopCoroutine(s.reward); s.Clear();
            s.reward = s.StartCoroutine(s.RunReward(lm, coins, balanceBefore));
        }

        void Clear()
        {
            foreach (var f in flights) if (f.image) Destroy(f.image.gameObject);
            flights.Clear(); if (pill) Destroy(pill.gameObject); pill = null; rewarding = false;
        }

        void BuildPill(int value)
        {
            // The coin balance on a cream pill at the top of the screen, above the card's crest.
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
        Vector2 PillCoin => pill ? pill.anchoredPosition + new Vector2(-118, 0) : Vector2.zero;

        IEnumerator RunReward(LevelManager lm, int coins, int before)
        {
            reduced = PlayerPrefs.GetInt("RoyalThrone.HomeMotion", 1) == 0;
            var menus = MenuManager.Instance; var card = menus ? menus.GetCanvasGlobal().Find("MenuComplete") : null;
            if (!card) yield break;
            rewarding = true; shownBefore = before; coinsWon = coins; landed = 0;
            BuildPill(before); var pillGroup = pill.gameObject.AddComponent<CanvasGroup>(); pillGroup.alpha = 0;
            float t0 = Time.unscaledTime; int expected = Mathf.Clamp(lm.stars, 1, 3); var seen = new bool[3]; int count = 0;
            Vector2 starRow = Vector2.zero;
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
            if (!card.gameObject.activeInHierarchy) { Clear(); yield break; }   // closed before the stars: nothing to fly
            if (starRow == Vector2.zero) { var s2 = card.Find("Image/Star2"); starRow = s2 ? Local(s2) : Vector2.zero; }
            float wait = Time.unscaledTime + .32f;
            while (card.gameObject.activeInHierarchy && Time.unscaledTime < wait) { pillGroup.alpha = 1; yield return null; }
            pillGroup.alpha = 1; pill.anchoredPosition = new Vector2(0, PillY);

            // The coins burst from the stars and fly up to the counter; each landing ticks the balance up.
            flightCount = Mathf.Clamp(reduced ? Mathf.Min(coins, 6) : coins, 1, 16);
            var to = PillCoin; float now = Time.unscaledTime;
            for (int i = 0; i < flightCount; i++)
            {
                float a = Random.Range(0, Mathf.PI * 2), r = Random.Range(60f, 170f);
                var f = new Flight { from = starRow, burst = starRow + new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r * .75f), to = to, start = now + i * .045f };
                f.control = new Vector2(Mathf.Lerp(f.burst.x, to.x, .2f) + Random.Range(-190f, 190f), Mathf.Lerp(f.burst.y, to.y, .55f));
                f.image = Picture("Coin", root, coin, starRow, new Vector2(66, 66), new Color(1, 1, 1, 0)); flights.Add(f);
            }
            RoyalAudioDirector.Get?.Play(RoyalCue.Create);
            while (rewarding && card.gameObject.activeInHierarchy && landed < flightCount) yield return null;
            if (landed < flightCount) FinishNow();
            while (card.gameObject.activeInHierarchy) yield return null;
            // The card closed: the counter fades out with it.
            float fade = 0; var g = pill ? pill.GetComponent<CanvasGroup>() : null;
            while (g && fade < .2f) { fade += Time.unscaledDeltaTime; g.alpha = 1 - fade / .2f; yield return null; }
            Clear();
        }

        void UpdateCoins(float dt)
        {
            if (pillFlash) { flash = Mathf.Max(0, flash - dt * 3.2f); pillFlash.color = new Color(1, .92f, .6f, .55f * flash); if (pill) pill.localScale = Vector3.one * (1 + .035f * flash); }
            if (flights.Count == 0) return;
            float now = Time.unscaledTime;
            foreach (var f in flights)
            {
                if (f.landed || !f.image) continue;
                float e = now - f.start; if (e < 0) continue;
                const float BurstTime = .2f, FlyTime = .58f;
                Vector2 p; float scale;
                if (e < BurstTime) { float u = EaseOut(e / BurstTime); p = Vector2.Lerp(f.from, f.burst, u); scale = Mathf.Lerp(.55f, 1, u); f.image.color = new Color(1, 1, 1, Mathf.Clamp01(e / .06f)); }
                else
                {
                    float u = Mathf.Clamp01((e - BurstTime) / FlyTime), w = u * u;   // accelerates into the counter
                    p = (1 - w) * (1 - w) * f.burst + 2 * (1 - w) * w * f.control + w * w * f.to; scale = Mathf.Lerp(1, .72f, u);
                    if (!reduced && Random.value < .5f) Spawn(1, p, Vector2.zero, .32f, 15, new Color(1, .9f, .5f));
                    if (u >= 1) { Land(f); continue; }
                }
                // A coin turning over in flight.
                f.image.rectTransform.anchoredPosition = p; f.image.rectTransform.localScale = new Vector3(scale * (.55f + .45f * Mathf.Abs(Mathf.Cos(e * 9 + f.start * 3))), scale, 1);
            }
        }

        void Land(Flight f)
        {
            f.landed = true; landed++; if (f.image) Destroy(f.image.gameObject);
            int shown = landed >= flightCount ? shownBefore + coinsWon : shownBefore + Mathf.RoundToInt(coinsWon * (float)landed / flightCount);
            if (pillValue) pillValue.text = shown.ToString();
            flash = 1; SparkleBurst(PillCoin, 5, 190, Gold, 20);
            if (landed == 1) PlusLabel();
            if (landed % 3 == 1) RoyalAudioDirector.Get?.Play(RoyalCue.Click);
        }

        void FinishNow()
        {
            foreach (var f in flights) if (!f.landed) { f.landed = true; if (f.image) Destroy(f.image.gameObject); }
            landed = flightCount; if (pillValue) pillValue.text = (shownBefore + coinsWon).ToString();
        }

        void PlusLabel() { StartCoroutine(RisePlus()); }
        IEnumerator RisePlus()
        {
            if (!pill) yield break;
            var label = Words("+coins", root, "+" + coinsWon, 44, pill.anchoredPosition + new Vector2(178, 0), new Vector2(160, 60), new Color(.62f, 1f, .52f), 2.5f);
            float t = 0;
            while (t < 1 && label) { t += Time.unscaledDeltaTime; var r = label.rectTransform; r.anchoredPosition += Vector2.up * 34 * Time.unscaledDeltaTime; label.color = new Color(.62f, 1f, .52f, 1 - EaseInOut((t - .45f) / .55f)); yield return null; }
            if (label) Destroy(label.gameObject);
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
            if (root) Destroy(root.gameObject);
            // The factory's textures are shared and cached: destroy only the sprites, and the one texture made here.
            if (piece && piece.texture) Destroy(piece.texture);
            if (soft && soft.texture) Destroy(soft.texture);
            foreach (var s in new[] { sparkle, star, glow, ring, piece, soft }) if (s) Destroy(s);
        }
    }
}
