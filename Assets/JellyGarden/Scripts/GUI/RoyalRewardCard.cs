using UnityEngine;
using UnityEngine.UI;

namespace RoyalThrone.Presentation
{
    // The reward card's ceremony (2026-10-05, the user: "the animation is so simple. make it feel more Royal"). Once the card
    // is in, the royal chest drops onto the card and settles with a squash, shakes as if something inside wants out, and
    // bursts open: a flash, golden rays that keep turning behind it, and a fountain of the reward (coins or hearts). The
    // reward itself rises out of the chest and hovers there, glowing; its amount is written in under the chest and a shine
    // crosses the title. Then the chest glows, sparkles twinkle and COLLECT breathes. The chest's lid is a sequence of
    // rendered frames (Resources/RoyalGameplay/RewardChest, render_chest.py). Unscaled time; with the Motion setting off the
    // chest is simply open and everything fades in.
    public sealed class RoyalRewardCard : MonoBehaviour
    {
        public Image chest, rays, icon;
        public RoyalCardShape flash, glow, shadow, aura;
        public RoyalTextStyle title, amount;
        public RectTransform sparkles;
        public Sprite[] lidFrames;
        public float[] lidAngles;
        public Vector2 chestRest, iconRest;
        public Sprite coinArt, heartArt;

        // The timeline, from the card opening (s): the drop, the shake, the burst, the lid, the reward, the words.
        const float DropAt = .12f, LandAt = .48f, SettledAt = .64f, ShakeAt = .72f, BurstAt = 1.02f, LidOpenFor = .18f, LidSettleFor = .14f;
        const float RiseAt = 1.10f, RiseFor = .45f, WordsAt = 1.45f, PerLetter = .055f, ShineAt = 1.25f, ShineEvery = 3.6f, ShineFor = .75f;
        const float Fall = 150, Hover = 6, RaysTurn = 9;
        const int FountainCount = 12, TwinkleCount = 6;
        float t, raysAngle, nextTwinkle; bool reduced, burst; int twinkleSlot;
        Image[] fountain; Vector2[] fountainVelocity; float[] fountainSpin;
        RawImage[] twinkles; float[] twinkleStart, twinkleLife; Vector2[] twinkleAt;
        string amountWords = "";

        /// <summary>The reward the card is showing: coins (true) or lives (false), as InitScript chose it.</summary>
        public bool Coins { get; private set; } = true;
        public bool Opened => burst;
        public float Elapsed => t;

        void OnEnable()
        {
            reduced = RoyalThrone.Testing.RoyalSave.GetInt("RoyalThrone.HomeMotion", 1) == 0;
            t = 0; raysAngle = 0; burst = false; nextTwinkle = 0; twinkleSlot = 0;
            var init = JellyGarden.Scripts.InitScript.Instance;
            Coins = !init || init.currentReward != JellyGarden.Scripts.RewardedAdsType.GetLifes;
            if (icon) icon.sprite = Coins ? coinArt : heartArt;
            amountWords = Coins ? "+" + (init ? init.rewardedGems : 0) + " Coins" : "Lives Refilled!";
            if (amount) { var text = amount.GetComponent<Text>(); if (text) text.text = amountWords; amount.Write(0); }
            // A card is reused: nothing may carry over from the last opening (a shine band, a turned or scaled part).
            if (title) title.Shine(-1);
            if (rays) { rays.rectTransform.localRotation = Quaternion.identity; rays.rectTransform.localScale = Vector3.one; }
            if (shadow) shadow.rectTransform.localScale = Vector3.one;
            if (glow) glow.rectTransform.anchoredPosition = iconRest;
            Build();
            Pose();
        }

        void Update()
        {
            t += Mathf.Min(Time.unscaledDeltaTime, .05f);
            Pose();
        }

        void Build()
        {
            if (fountain != null && fountain.Length > 0 && fountain[0]) { foreach (var f in fountain) f.enabled = false; foreach (var s in twinkles) s.enabled = false; return; }
            fountain = new Image[FountainCount]; fountainVelocity = new Vector2[FountainCount]; fountainSpin = new float[FountainCount];
            for (int i = 0; i < FountainCount; i++)
            {
                var go = new GameObject("Fountain " + i, typeof(RectTransform), typeof(Image)); go.transform.SetParent(sparkles, false);
                var image = go.GetComponent<Image>(); image.raycastTarget = false; image.preserveAspect = true; image.enabled = false;
                fountain[i] = image;
                float side = (i % 2 == 0 ? -1 : 1) * (.25f + .75f * Hash(i * 7 + 1));
                // A spray out to the sides, below the title.
                fountainVelocity[i] = new Vector2(side * 380, 420 + 170 * Hash(i * 13 + 2));
                fountainSpin[i] = (Hash(i * 5 + 3) - .5f) * 540;
            }
            twinkles = new RawImage[TwinkleCount]; twinkleStart = new float[TwinkleCount]; twinkleLife = new float[TwinkleCount]; twinkleAt = new Vector2[TwinkleCount];
            var star = JellyGarden.Scripts.Juice.SparkleTextureFactory.FourPointSparkle;
            for (int i = 0; i < TwinkleCount; i++)
            {
                var go = new GameObject("Twinkle " + i, typeof(RectTransform), typeof(RawImage)); go.transform.SetParent(sparkles, false);
                var image = go.GetComponent<RawImage>(); image.texture = star; image.raycastTarget = false; image.enabled = false;
                twinkles[i] = image; twinkleStart[i] = -10;
            }
        }

        void Pose()
        {
            if (reduced) { Calm(); return; }
            PoseChest();
            PoseLight();
            PoseReward();
            PoseFountain();
            PoseWords();
            PoseTwinkles();
        }

        // Motion off: the chest open, the reward over it, the rays still; everything fades in together.
        void Calm()
        {
            float a = Mathf.Clamp01((t - .15f) / .3f);
            burst = t >= .15f;
            SetFrame(lidAngles[lidAngles.Length - 2]);
            Place(chest.rectTransform, chestRest, Vector2.one, 0); Alpha(chest, a);
            if (shadow) Fade(shadow, .45f * a);
            if (rays) { Alpha(rays, .45f * a); rays.rectTransform.localRotation = Quaternion.identity; }
            if (flash) Fade(flash, 0);
            Place(icon.rectTransform, iconRest, Vector2.one, 0); Alpha(icon, a);
            if (glow) Fade(glow, .35f * a);
            if (amount && burst) amount.Write(float.PositiveInfinity);
            if (aura) Fade(aura, .22f * a);
            foreach (var f in fountain) f.enabled = false;
        }

        void PoseChest()
        {
            var r = chest.rectTransform; float y = 0, a = 1; Vector2 scale = Vector2.one; float turn = 0;
            if (t < DropAt) { a = 0; y = Fall; }
            else if (t < LandAt) { float u = (t - DropAt) / (LandAt - DropAt); y = Fall * (1 - u * u); a = Mathf.Clamp01(u * 4); }
            else if (t < SettledAt)
            {
                // The landing: wider and lower, a little taller, then at rest.
                float u = (t - LandAt) / (SettledAt - LandAt);
                float squash = u < .45f ? Mathf.Sin(u / .45f * Mathf.PI * .5f) : Mathf.Cos((u - .45f) / .55f * Mathf.PI * .5f);
                float sign = u < .45f ? 1 : -.35f;
                scale = new Vector2(1 + .08f * squash * sign, 1 - .10f * squash * sign);
            }
            else if (t < BurstAt && t >= ShakeAt)
            {
                // Something inside wants out: three quick rattles, growing.
                float u = (t - ShakeAt) / (BurstAt - ShakeAt);
                turn = Mathf.Sin(u * Mathf.PI * 6) * 4.5f * u;
                y = Mathf.Abs(Mathf.Sin(u * Mathf.PI * 6)) * 6 * u;
            }
            else if (t >= BurstAt && t < BurstAt + .2f)
            {
                // The burst lifts it a touch and lets it settle.
                float u = (t - BurstAt) / .2f;
                scale = new Vector2(1 - .04f * Mathf.Sin(u * Mathf.PI), 1 + .06f * Mathf.Sin(u * Mathf.PI));
            }
            Place(r, chestRest + new Vector2(0, y), scale, turn); Alpha(chest, a);
            // The lid: rattling while it shakes, then open past its rest and back.
            float angle = 0;
            if (t >= ShakeAt && t < BurstAt) angle = Mathf.Repeat(t, .12f) < .06f ? lidAngles[1] : 0;
            else if (t >= BurstAt)
            {
                float u = (t - BurstAt) / LidOpenFor, last = lidAngles[lidAngles.Length - 1], rest = lidAngles[lidAngles.Length - 2];
                angle = u < 1 ? last * (1 - (1 - u) * (1 - u) * (1 - u)) : Mathf.Lerp(last, rest, Ease((t - BurstAt - LidOpenFor) / LidSettleFor));
            }
            SetFrame(angle);
            if (!burst && t >= BurstAt) { burst = true; Burst(); }
            if (shadow)
            {
                float k = Mathf.Clamp01(1 - y / Fall);
                Fade(shadow, .45f * k * k * a);
                shadow.rectTransform.localScale = new Vector3(.6f + .4f * k, .6f + .4f * k, 1);
            }
        }

        void Burst()
        {
            JellyGarden.Scripts.Juice.RoyalAudioDirector.Get?.Play(JellyGarden.Scripts.Juice.RoyalCue.Create);
            // Each piece shows once its own turn in the fountain comes (PoseFountain), not all at once at the card's centre.
            for (int i = 0; i < FountainCount; i++) fountain[i].sprite = Coins ? coinArt : heartArt;
        }

        void PoseLight()
        {
            float since = t - BurstAt;
            if (flash)
            {
                float f = since < 0 ? 0 : since < .05f ? since / .05f : 1 - Mathf.Clamp01((since - .05f) / .35f);
                Fade(flash, .9f * f * f);
                flash.rectTransform.localScale = Vector3.one * Mathf.Lerp(.6f, 1.25f, Mathf.Clamp01(since / .4f));
            }
            if (rays)
            {
                float rise = Mathf.Clamp01(since / .28f);
                if (since > 0) raysAngle -= RaysTurn * Mathf.Min(Time.unscaledDeltaTime, .05f);
                Alpha(rays, since < 0 ? 0 : rise * (.58f + .08f * Mathf.Sin(t * 1.7f)));
                rays.rectTransform.localRotation = Quaternion.Euler(0, 0, raysAngle);
                rays.rectTransform.localScale = Vector3.one * Mathf.Lerp(.7f, 1, Ease(rise));
            }
            if (aura) Fade(aura, t < 1.6f ? 0 : Mathf.Clamp01((t - 1.6f) / .4f) * (.20f + .14f * Mathf.Sin((t - 1.6f) * 2.6f)));
        }

        // The reward rises out of the chest, past its size and back, and then hovers, breathing.
        void PoseReward()
        {
            var r = icon.rectTransform;
            if (t < RiseAt) { Alpha(icon, 0); if (glow) Fade(glow, 0); return; }
            float u = Mathf.Clamp01((t - RiseAt) / RiseFor);
            float s = u < .7f ? Mathf.Lerp(.3f, 1.15f, Ease(u / .7f)) : Mathf.Lerp(1.15f, 1, Ease((u - .7f) / .3f));
            var from = chestRest + new Vector2(0, 40);
            var at = Vector2.Lerp(from, iconRest, Ease(u)) + new Vector2(0, u >= 1 ? Mathf.Sin((t - RiseAt - RiseFor) * 2.2f) * Hover : 0);
            Place(r, at, Vector2.one * s, u < 1 ? (1 - u) * -12 : 0); Alpha(icon, Mathf.Clamp01(u * 3));
            if (glow) { Fade(glow, Mathf.Clamp01(u * 2) * (.42f + .08f * Mathf.Sin(t * 2.4f))); glow.rectTransform.anchoredPosition = at; }
        }

        // Coins (or hearts) thrown up out of the chest, falling back and fading.
        void PoseFountain()
        {
            float since = t - BurstAt - .02f;
            for (int i = 0; i < FountainCount; i++)
            {
                var image = fountain[i];
                float life = .95f + .2f * Hash(i * 3 + 9), at = since - i * .015f;
                if (!burst || at < 0 || at > life) { if (image.enabled) image.enabled = false; continue; }
                if (!image.enabled) image.enabled = true;
                var v = fountainVelocity[i];
                var p = chestRest + new Vector2(0, 30) + v * at + new Vector2(0, -1500 * at * at * .5f);
                float size = 58 * (1 - .3f * at / life);
                var r = image.rectTransform; r.anchoredPosition = p; r.sizeDelta = new Vector2(size, size); r.localRotation = Quaternion.Euler(0, 0, fountainSpin[i] * at);
                var c = image.color; c.a = 1 - Mathf.Clamp01((at - life * .65f) / (life * .35f)); image.color = c;
            }
        }

        void PoseWords()
        {
            if (amount) amount.Write(t < WordsAt ? 0 : (t - WordsAt) / PerLetter >= amountWords.Length + 1 ? float.PositiveInfinity : (t - WordsAt) / PerLetter);
            if (title)
            {
                float since = t - ShineAt;
                float u = since < 0 ? -1 : (since % ShineEvery) / ShineFor;
                title.Shine(u >= 0 && u <= 1 ? Mathf.Lerp(-.25f, 1.25f, u) : -1);
            }
        }

        // Once open: a twinkle every so often round the reward and the chest.
        void PoseTwinkles()
        {
            for (int i = 0; i < TwinkleCount; i++)
            {
                float u = (t - twinkleStart[i]) / twinkleLife[i];
                var image = twinkles[i];
                if (u < 0 || u > 1) { if (image.enabled) image.enabled = false; continue; }
                image.enabled = true; float a = Mathf.Sin(Mathf.PI * u);
                var r = image.rectTransform; r.anchoredPosition = twinkleAt[i]; r.sizeDelta = Vector2.one * (16 + 26 * a); r.localRotation = Quaternion.Euler(0, 0, u * 90);
                image.color = new Color(1, .96f, .8f, a);
            }
            if (t < 1.5f || t < nextTwinkle) return;
            int slot = twinkleSlot++ % TwinkleCount; float h = Hash(twinkleSlot * 17 + 5);
            twinkleStart[slot] = t; twinkleLife[slot] = .45f + .3f * h;
            var around = h < .55f ? iconRest : chestRest;
            float ang = Hash(twinkleSlot * 23 + 7) * Mathf.PI * 2, dist = (h < .55f ? 70 : 120) + 50 * Hash(twinkleSlot * 29 + 11);
            twinkleAt[slot] = around + new Vector2(Mathf.Cos(ang) * dist, Mathf.Sin(ang) * dist * .75f);
            nextTwinkle = t + .22f + .25f * Hash(twinkleSlot * 31 + 13);
        }

        void SetFrame(float angle)
        {
            if (lidFrames == null || lidFrames.Length == 0) return;
            int best = 0; float bestD = float.MaxValue;
            for (int i = 0; i < lidAngles.Length; i++) { float d = Mathf.Abs(lidAngles[i] - angle); if (d < bestD) { bestD = d; best = i; } }
            if (chest.sprite != lidFrames[best]) chest.sprite = lidFrames[best];
        }

        static void Place(RectTransform r, Vector2 at, Vector2 scale, float turn) { r.anchoredPosition = at; r.localScale = new Vector3(scale.x, scale.y, 1); r.localRotation = Quaternion.Euler(0, 0, turn); }
        static void Alpha(Graphic g, float a) { if (!g) return; var c = g.color; c.a = a; g.color = c; }
        static void Fade(RoyalCardShape s, float a) { var c = s.color; c.a = a; s.color = c; }
        static float Ease(float u) { u = Mathf.Clamp01(u); return 1 - (1 - u) * (1 - u) * (1 - u); }
        static float Hash(int n) { uint x = (uint)n * 2654435761u; x ^= x >> 15; x *= 2246822519u; x ^= x >> 13; return (x & 0xffffff) / (float)0x1000000; }
    }
}
