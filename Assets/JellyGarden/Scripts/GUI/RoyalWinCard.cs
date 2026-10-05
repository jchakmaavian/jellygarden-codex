using UnityEngine;
using UnityEngine.UI;

namespace RoyalThrone.Presentation
{
    // Marks a card part that shows by itself after the card is in, so RoyalCardMotion's entrance leaves it alone.
    public sealed class RoyalAppearsLater : MonoBehaviour { }

    // The win card's centre piece, after the reference's results card (its motion measured frame by frame from the 2026-09-28
    // recording, 2026-10-05). When the level's star shows (AnimationManager switches Star1 on once the card is in) it starts
    // very large over the card and shrinks onto the cushion in 0.2 s, where a flash lights it and the cushion gives under it;
    // the star rebounds to 1.08 and settles by 0.42 s. "Well Done!" is then written in, a letter every 0.075 s, while sparkles
    // swirl round the star for about a second; after that the star glows softly and the odd twinkle shows. Unscaled time.
    // With Home motion off the star and the title simply fade in.
    public sealed class RoyalWinCard : MonoBehaviour
    {
        public RectTransform star, cushion;
        public RoyalTextStyle title;
        Text titleText;
        public RoyalCardShape glow, flash;
        public RectTransform sparkles;
        public Vector2 starRest;

        const float Drop = .20f, Rebound = .30f, Settle = .42f, WriteStart = .24f, PerLetter = .075f, FallbackTitle = 1.6f;
        const int SwirlCount = 26;
        float t, open;
        bool started, reduced;
        RawImage[] sparkle;
        float[] sparkleStart, sparkleLife, sparkleAngle, sparkleRadius, sparkleSize, sparkleSpin;
        float nextTwinkle;
        int twinkleSlot;
        CanvasGroup starGroup;

        // True once the star has reached the cushion (RoyalWinSequence starts the coin flights from it then).
        public bool Landed => started && (reduced || t >= Drop);
        public Vector2 StarRest => starRest;

        public static RoyalWinCard Of(Transform menu)
        {
            var card = menu ? menu.Find("Image") : null;
            return card ? card.GetComponent<RoyalWinCard>() : null;
        }

        void OnEnable()
        {
            reduced = RoyalThrone.Testing.RoyalSave.GetInt("RoyalThrone.HomeMotion", 1) == 0;   // the Motion setting, as Home reads it
            started = false; t = 0; open = 0; nextTwinkle = 0; twinkleSlot = 0;
            if (title) title.Write(0);
            if (glow) glow.color = Fade(glow.color, 0);
            if (flash) flash.color = Fade(flash.color, 0);
            if (cushion) cushion.localScale = Vector3.one;
            BuildSparkles();
            foreach (var s in sparkle) s.enabled = false;
        }

        void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, .05f);
            open += dt;
            if (!started)
            {
                if (star && star.gameObject.activeInHierarchy) { started = true; t = 0; }
                // No star (a card opened some other way): the title still shows.
                else if (open > FallbackTitle && title) title.Write(float.PositiveInfinity);
                if (!started) return;
            }
            else t += dt;
            if (!starGroup && star) { starGroup = star.GetComponent<CanvasGroup>(); if (!starGroup) starGroup = star.gameObject.AddComponent<CanvasGroup>(); }
            if (reduced) { Calm(); return; }
            PoseStar();
            PoseCushion();
            PoseLight();
            Write(t - WriteStart);
            Swirl();
            Twinkle();
        }

        void Calm()
        {
            float a = Mathf.Clamp01(t / .2f);
            if (starGroup) starGroup.alpha = a;
            star.anchoredPosition = starRest; star.localScale = Vector3.one; star.localRotation = Quaternion.identity;
            if (glow) glow.color = Fade(glow.color, .30f * a);
            if (title && t >= Drop) title.Write(float.PositiveInfinity);
        }

        void PoseStar()
        {
            float s, lift;
            if (t < Drop) { float u = t / Drop; s = Mathf.Lerp(2.7f, .80f, u * u); lift = 70 * (1 - u * u); }
            else if (t < Rebound) { s = Mathf.Lerp(.80f, 1.08f, RoyalLevelCardMotion.EaseOutCubic((t - Drop) / (Rebound - Drop))); lift = 0; }
            else { s = Mathf.Lerp(1.08f, 1, RoyalLevelCardMotion.EaseOutCubic((t - Rebound) / (Settle - Rebound))); lift = 0; }
            star.localScale = Vector3.one * s;
            star.anchoredPosition = starRest + new Vector2(0, lift);
            star.localRotation = Quaternion.Euler(0, 0, t < Drop ? -10 * (1 - t / Drop) : 0);
            if (starGroup) starGroup.alpha = Mathf.Clamp01(t / .06f);
        }

        // The cushion gives under the star and springs back: lower and wider, then a little taller, then at rest.
        void PoseCushion()
        {
            if (!cushion) return;
            float y = 1, x = 1;
            if (t >= Drop && t < Drop + .06f) { float u = RoyalLevelCardMotion.EaseOutCubic((t - Drop) / .06f); y = Mathf.Lerp(1, .90f, u); x = Mathf.Lerp(1, 1.04f, u); }
            else if (t >= Drop + .06f && t < Drop + .16f) { float u = RoyalLevelCardMotion.EaseOutCubic((t - Drop - .06f) / .10f); y = Mathf.Lerp(.90f, 1.03f, u); x = Mathf.Lerp(1.04f, .99f, u); }
            else if (t >= Drop + .16f && t < Drop + .30f) { float u = RoyalLevelCardMotion.EaseOutCubic((t - Drop - .16f) / .14f); y = Mathf.Lerp(1.03f, 1, u); x = Mathf.Lerp(.99f, 1, u); }
            cushion.localScale = new Vector3(x, y, 1);
        }

        // The flash as the star lands (peak at 0.17 s, gone by 0.4 s), then the star's own soft glow, breathing.
        void PoseLight()
        {
            if (flash)
            {
                float f = t < .12f ? 0 : t < .17f ? (t - .12f) / .05f : 1 - Mathf.Clamp01((t - .17f) / .23f);
                flash.color = Fade(flash.color, .85f * f * f);
                flash.rectTransform.localScale = Vector3.one * Mathf.Lerp(.7f, 1.15f, Mathf.Clamp01((t - .12f) / .3f));
            }
            if (glow)
            {
                float rise = Mathf.Clamp01((t - .2f) / .3f);
                glow.color = Fade(glow.color, rise * (.30f + .07f * Mathf.Sin((t - .2f) * 2.4f)));
            }
        }

        void Write(float since)
        {
            if (!title) return;
            float letters = since < 0 ? 0 : since / PerLetter;
            if (!titleText) titleText = title.GetComponent<Text>();
            // Each glyph pops over one letter's time; once the last has settled the title is drawn as it is.
            int count = titleText ? titleText.text.Length : 20;
            title.Write(letters >= count + 1 ? float.PositiveInfinity : letters);
        }

        void BuildSparkles()
        {
            if (sparkle != null && sparkle.Length > 0 && sparkle[0]) return;
            int n = SwirlCount + 6;
            sparkle = new RawImage[n];
            sparkleStart = new float[n]; sparkleLife = new float[n]; sparkleAngle = new float[n]; sparkleRadius = new float[n]; sparkleSize = new float[n]; sparkleSpin = new float[n];
            var texture = JellyGarden.Scripts.Juice.SparkleTextureFactory.FourPointSparkle;
            for (int i = 0; i < n; i++)
            {
                var go = new GameObject("Sparkle " + i, typeof(RectTransform), typeof(RawImage));
                go.transform.SetParent(sparkles, false);
                var image = go.GetComponent<RawImage>(); image.texture = texture; image.raycastTarget = false; image.enabled = false;
                sparkle[i] = image;
                // The swirl: each sparkle starts somewhere on a ring round the star, at its own time in the first 0.5 s.
                float k = i / (float)SwirlCount;
                sparkleStart[i] = .22f + .5f * Hash(i * 3 + 1);
                sparkleLife[i] = .55f + .45f * Hash(i * 3 + 2);
                sparkleAngle[i] = k * Mathf.PI * 2 + Hash(i * 3 + 3);
                sparkleRadius[i] = 90 + 110 * Hash(i * 7 + 5);
                sparkleSize[i] = 16 + 20 * Hash(i * 5 + 7);
                sparkleSpin[i] = 2.2f + 1.6f * Hash(i * 11 + 3);
            }
        }

        // Sparkles spiralling in round the star and the cushion as the title is written.
        void Swirl()
        {
            for (int i = 0; i < SwirlCount; i++)
            {
                float u = (t - sparkleStart[i]) / sparkleLife[i];
                var image = sparkle[i];
                if (u < 0 || u > 1) { if (image.enabled) image.enabled = false; continue; }
                image.enabled = true;
                float a = sparkleAngle[i] + sparkleSpin[i] * u, r = sparkleRadius[i] * (1 - .45f * u);
                var p = starRest + new Vector2(Mathf.Cos(a) * r * 1.25f, Mathf.Sin(a) * r * .8f - 30 + 50 * u);
                float alpha = Mathf.Sin(Mathf.PI * u);
                Place(image, p, sparkleSize[i] * (.6f + .6f * alpha), a * 40, new Color(1, .97f, .86f, alpha));
            }
        }

        // Once settled, a twinkle every 0.3-0.5 s somewhere round the star.
        void Twinkle()
        {
            if (t < 1.2f) { nextTwinkle = 1.2f; return; }
            for (int i = SwirlCount; i < sparkle.Length; i++)
            {
                float u = (t - sparkleStart[i]) / sparkleLife[i];
                var image = sparkle[i];
                if (u < 0 || u > 1) { if (image.enabled) image.enabled = false; continue; }
                image.enabled = true;
                float alpha = Mathf.Sin(Mathf.PI * u);
                Place(image, starRest + new Vector2(Mathf.Cos(sparkleAngle[i]), Mathf.Sin(sparkleAngle[i])) * sparkleRadius[i], sparkleSize[i] * (.4f + .8f * alpha), 0, new Color(1, .96f, .82f, alpha));
            }
            if (t < nextTwinkle) return;
            int slot = SwirlCount + (twinkleSlot++ % (sparkle.Length - SwirlCount));
            float h = Hash(twinkleSlot * 13 + 1);
            sparkleStart[slot] = t; sparkleLife[slot] = .5f + .25f * h;
            sparkleAngle[slot] = Hash(twinkleSlot * 17 + 2) * Mathf.PI * 2;
            sparkleRadius[slot] = 70 + 70 * Hash(twinkleSlot * 19 + 3);
            sparkleSize[slot] = 18 + 14 * h;
            nextTwinkle = t + .3f + .2f * Hash(twinkleSlot * 23 + 4);
        }

        static void Place(RawImage image, Vector2 p, float size, float angle, Color color)
        {
            var r = image.rectTransform;
            r.anchoredPosition = p; r.sizeDelta = new Vector2(size, size); r.localRotation = Quaternion.Euler(0, 0, angle);
            image.color = color;
        }

        static float Hash(int n) { uint x = (uint)n * 2654435761u; x ^= x >> 15; x *= 2246822519u; x ^= x >> 13; return (x & 0xffffff) / (float)0x1000000; }
        static Color Fade(Color c, float a) { c.a = a; return c; }
    }
}
