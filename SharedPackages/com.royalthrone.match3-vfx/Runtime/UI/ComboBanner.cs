using UnityEngine;
using UnityEngine.UI;

namespace RoyalThrone.Match3Vfx
{
    /// <summary>
    /// One reusable cascade banner (GREAT!, BRAVO!...) with a fixed sparkle pool. It has no
    /// GraphicRaycaster and its CanvasGroup never blocks raycasts, so it cannot eat board input.
    /// Built lazily on first use.
    /// </summary>
    internal sealed class ComboBanner
    {
        public const int SparkleCount = 32;
        public const float Lifetime = 1.25f;

        sealed class Sparkle
        {
            public Image image;
            public RectTransform rect;
            public Vector2 origin, velocity;
            public float spin, size;
        }

        readonly Match3VfxRig rig;
        readonly Sparkle[] sparkles = new Sparkle[SparkleCount];
        GameObject rootObject;
        CanvasGroup group;
        RectTransform safe, banner, glow;
        Image halo;
        Text title;
        float age = 10f, nextAllowed;
        bool reduced;

        public bool Built => rootObject;
        public bool IsShowing => age < Lifetime;
        public int Presentations { get; private set; }
        public string CurrentText => title ? title.text : null;
        public CanvasGroup Group => group;

        public ComboBanner(Match3VfxRig rig) { this.rig = rig; }

        static RectTransform Rect(string name, Transform parent, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            return rect;
        }

        void Build()
        {
            var s = rig.Settings;
            var root = Rect("Combo banner (never blocks input)", rig.transform, Vector2.zero);
            rootObject = root.gameObject;
            var canvas = rootObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = s.bannerCanvasOrder;
            var scaler = rootObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720f, 1280f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            group = rootObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            group.alpha = 0f;

            safe = Rect("Safe area", root, Vector2.zero);
            banner = Rect("Banner", safe, new Vector2(650f, 150f));
            banner.anchorMin = banner.anchorMax = new Vector2(0.5f, s.bannerHeight);

            glow = Rect("Warm halo", banner, new Vector2(550f, 145f));
            halo = glow.gameObject.AddComponent<Image>();
            halo.sprite = Match3Shapes.GlowSprite;
            halo.raycastTarget = false;

            for (int i = 0; i < SparkleCount; i++)
            {
                var rect = Rect("Sparkle " + i, banner, Vector2.one * 12f);
                var image = rect.gameObject.AddComponent<Image>();
                image.sprite = Match3Shapes.SparkleSprite;
                image.raycastTarget = false;
                sparkles[i] = new Sparkle { image = image, rect = rect };
            }

            var textRect = Rect("Text", banner, new Vector2(620f, 112f));
            title = textRect.gameObject.AddComponent<Text>();
            title.font = s.bannerFont ? s.bannerFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            title.fontSize = 78;
            title.fontStyle = s.bannerFont ? FontStyle.Normal : FontStyle.Bold;
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 42;
            title.resizeTextMaxSize = 78;
            title.alignment = TextAnchor.MiddleCenter;
            title.horizontalOverflow = HorizontalWrapMode.Overflow;
            title.color = new Color(1f, 0.92f, 0.58f);
            title.raycastTarget = false;
            var shadow = textRect.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.06f, 0.13f, 0.32f, 0.95f);
            shadow.effectDistance = new Vector2(0f, -6f);
            var outline = textRect.gameObject.AddComponent<Outline>();
            outline.effectColor = s.bannerOutlineColor;
            outline.effectDistance = new Vector2(2.5f, -2.5f);
            var gradient = textRect.gameObject.AddComponent<ComboBannerGradient>();
            gradient.top = s.bannerTopColor;
            gradient.bottom = s.bannerBottomColor;
        }

        public bool Show(int cascade, bool force = false)
        {
            var s = rig.Settings;
            string text = s.BannerTextFor(cascade);
            if (text == null)
            {
                if (!force) return false;
                text = s.bannerTexts != null && s.bannerTexts.Length > 0 ? s.bannerTexts[0] : "GREAT!";
            }
            if (!force && Time.unscaledTime < nextAllowed) return false;
            if (!rootObject) Build();

            nextAllowed = Time.unscaledTime + s.bannerCooldown;
            age = 0f;
            Presentations++;
            reduced = rig.ReducedMotion;
            title.text = text;
            // Deterministic decoration never consumes the game's random sequence.
            for (int i = 0; i < SparkleCount; i++)
            {
                var spark = sparkles[i];
                float a = (i + 0.3f) * Mathf.PI * 2f / SparkleCount;
                float speed = 95f + (i * 47 % 140);
                spark.origin = new Vector2(Mathf.Cos(a) * 220f, Mathf.Sin(a) * 48f);
                spark.velocity = new Vector2(Mathf.Cos(a) * speed, Mathf.Sin(a) * speed * 0.7f + 35f);
                spark.spin = (i % 2 == 0 ? 1f : -1f) * (55f + i * 11f);
                spark.size = 10f + i % 4 * 5f;
                spark.image.enabled = !reduced || i % 4 == 0;
            }
            rig.Sfx.Play(Match3SfxCue.Celebrate);
            Render();
            return true;
        }

        public void Tick(float dt, bool paused)
        {
            if (!rootObject) return;
            if (paused || !IsShowing) { group.alpha = 0f; return; }
            age += dt;
            Render();
        }

        void Render()
        {
            var area = Screen.safeArea;
            var screen = new Vector2(Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height));
            safe.anchorMin = area.min / screen;
            safe.anchorMax = area.max / screen;
            safe.offsetMin = safe.offsetMax = Vector2.zero;

            float entrance = Mathf.Clamp01(age / 0.18f);
            float exit = Mathf.Clamp01((age - 0.93f) / 0.32f);
            group.alpha = (reduced ? entrance : 1f) * (1f - exit);
            float spring = 1f + Mathf.Sin(entrance * Mathf.PI) * 0.12f;
            banner.localScale = Vector3.one * (reduced ? 1f : Mathf.Lerp(0.65f, spring, 1f - Mathf.Pow(1f - entrance, 3f)));
            banner.anchoredPosition = new Vector2(0f, reduced ? 0f : Mathf.Lerp(-12f, 9f, entrance) + exit * 22f);
            banner.localRotation = Quaternion.Euler(0f, 0f, reduced ? 0f : Mathf.Sin(entrance * Mathf.PI) * -2f);
            halo.color = new Color(1f, 0.75f, 0.23f, (reduced ? 0.13f : 0.24f) * (1f - exit));
            glow.localScale = Vector3.one * (1f + entrance * 0.14f);

            for (int i = 0; i < SparkleCount; i++)
            {
                var spark = sparkles[i];
                if (!spark.image.enabled) continue;
                float t = Mathf.Max(0f, age - 0.055f);
                float amount = reduced ? 0.25f : 1f;
                spark.rect.anchoredPosition = spark.origin + spark.velocity * t * amount + Vector2.down * (95f * t * t * amount);
                spark.rect.localRotation = Quaternion.Euler(0f, 0f, spark.spin * t * amount);
                float pulse = 0.6f + 0.4f * Mathf.Sin(t * 12f + i);
                spark.rect.sizeDelta = Vector2.one * spark.size * pulse;
                spark.image.color = i % 3 == 0 ? new Color(0.65f, 0.88f, 1f, 1f - exit) : new Color(1f, 0.9f, 0.55f, 1f - exit);
            }
        }

        public void Cancel()
        {
            age = 10f;
            if (group) group.alpha = 0f;
        }

        public void Dispose()
        {
            if (rootObject) Object.Destroy(rootObject);
        }
    }
}
