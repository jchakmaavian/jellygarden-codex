using UnityEngine;
using UnityEngine.UI;

namespace RoyalThrone.Presentation
{
    // Excludes a part with its own timing from RoyalCardMotion's entrance bands.
    public sealed class RoyalAppearsLater : MonoBehaviour { }

    // Presentation of the already committed win: a soft star landing, warm light on the velvet,
    // a short sparkle burst, then a five-tip glint and a traveling sweep over the cushion braid.
    // AnimationManager activates Star1 after the card enters. This component owns no reward writes.
    public sealed class RoyalWinCard : MonoBehaviour
    {
        public RectTransform star, cushion, starShadow;
        public RoyalTextStyle title;
        public RoyalCardShape glow, flash, pillowGlow, contactShadow;
        public RectTransform sparkles, rays, rewardBadge, coinOrigin;
        public Text rewardAmount;
        public Vector2 starRest;

        const float Drop = .28f, Rebound = .42f, Settle = .60f;
        const float WriteStart = .32f, PerLetter = .042f, FallbackTitle = 1.6f;
        const float RewardStart = .69f, RewardCountTime = .55f, RewardReady = .72f;
        const int BurstCount = 24, IdleCount = 14, RewardCountSteps = 64;

        float t, open, nextTwinkle;
        bool started, reduced, initialized;
        int twinkleSlot, rewardCoins = -1, shownRewardStep = -1;
        Text titleText;
        Material lightMaterial;
        CanvasGroup starGroup, shadowGroup, titleGroup, raysGroup, badgeGroup;
        Vector2 badgeRest;
        RawImage[] sparkle;
        float[] sparkleStart, sparkleLife, sparkleAngle, sparkleRadius, sparkleSize, sparkleSpin;
        Vector2[] sparklePosition, sparkleEnd;
        bool[] sparkleHero;
        readonly string[] rewardLabels = new string[RewardCountSteps + 1];

        // Impact and collection are separate: coins wait until the complete star/cushion settle.
        public bool Landed => started && (reduced || t >= Drop);
        public bool RewardsReady => started && t >= (reduced ? .20f : RewardReady);
        public float Elapsed => t;
        public float OpenElapsed => open;
        public int RewardCoins => Mathf.Max(0, rewardCoins);
        public Vector2 StarRest => starRest;
        public int ActiveSparkles
        {
            get
            {
                int count = 0;
                if (sparkle != null) foreach (var image in sparkle) if (image && image.enabled) count++;
                return count;
            }
        }

        public static RoyalWinCard Of(Transform menu)
        {
            var card = menu ? menu.Find("Image") : null;
            return card ? card.GetComponent<RoyalWinCard>() : null;
        }

        // Reward passes the committed amount after opening the popup. The fallback below also
        // makes directly opened QA cards readable, without awarding anything.
        public void SetReward(int coins)
        {
            coins = Mathf.Max(0, coins);
            if (coins != rewardCoins || rewardLabels[0] == null)
            {
                rewardCoins = coins;
                for (int i = 0; i <= RewardCountSteps; i++)
                    rewardLabels[i] = "+" + Mathf.RoundToInt(coins * Ease(i / (float)RewardCountSteps));
            }
            shownRewardStep = -1;
            ShowRewardCount(RewardProgress());
        }

        void OnEnable()
        {
            reduced = RoyalThrone.Testing.RoyalSave.GetInt("RoyalThrone.HomeMotion", 1) == 0;
            started = false; t = 0; open = 0; nextTwinkle = .91f; twinkleSlot = 0;
            var level = JellyGarden.Scripts.LevelManager.THIS;
            SetReward(level ? JellyGarden.Scripts.Juice.RoyalWinSequence.Coins(level) : 0);
            ResetVisuals();
            // AddComponent can call OnEnable before the card builder assigns our references.
            // BuildSparkles is intentionally deferred until Update has a real parent to use.
        }

        void OnDisable()
        {
            ResetVisuals();
            started = false; t = 0; open = 0;
        }

        void OnDestroy()
        {
            if (lightMaterial) Destroy(lightMaterial);
        }

        bool Initialize()
        {
            if (initialized) return true;
            if (!sparkles) return false;
            starGroup = Group(star);
            shadowGroup = Group(starShadow);
            titleGroup = Group(title ? title.transform : null);
            raysGroup = Group(rays);
            badgeGroup = Group(rewardBadge);
            if (rewardBadge) badgeRest = rewardBadge.anchoredPosition;
            if (title) titleText = title.GetComponent<Text>();
            // Light adds warmth over the navy rather than mixing a dull opaque veil into it.
            // This card owns one shared UI material; its pillow, star and shadows retain normal blending.
            var lightShader = Resources.Load<Shader>("RoyalGameplay/UIAdditive");
            if (lightShader)
            {
                lightMaterial = new Material(lightShader) { name = "Royal win additive light", hideFlags = HideFlags.HideAndDontSave };
                if (glow) glow.material = lightMaterial;
                if (flash) flash.material = lightMaterial;
                if (pillowGlow) pillowGlow.material = lightMaterial;
                var rayGraphic = rays ? rays.GetComponent<Graphic>() : null;
                if (rayGraphic) rayGraphic.material = lightMaterial;
            }
            BuildSparkles();
            initialized = true;
            ResetVisuals();
            return true;
        }

        static CanvasGroup Group(Transform target)
        {
            if (!target) return null;
            var group = target.GetComponent<CanvasGroup>();
            if (!group) group = target.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false; group.interactable = false;
            return group;
        }

        void ResetVisuals()
        {
            if (star)
            {
                star.anchoredPosition = starRest;
                star.localScale = Vector3.one;
                star.localRotation = Quaternion.identity;
            }
            if (starGroup) starGroup.alpha = 0;
            if (shadowGroup) shadowGroup.alpha = 0;
            if (starShadow) starShadow.localScale = Vector3.one;
            if (contactShadow) contactShadow.color = Fade(contactShadow.color, 0);
            if (title) title.Write(0);
            if (titleGroup) titleGroup.alpha = 1;
            if (cushion) cushion.localScale = Vector3.one;
            if (glow) glow.color = Fade(glow.color, 0);
            if (pillowGlow) pillowGlow.color = Fade(pillowGlow.color, 0);
            if (flash) { flash.color = Fade(flash.color, 0); flash.rectTransform.localScale = Vector3.one; }
            if (rays) rays.localRotation = Quaternion.identity;
            if (raysGroup) raysGroup.alpha = 0;
            if (badgeGroup) badgeGroup.alpha = 0;
            if (rewardBadge && initialized) { rewardBadge.anchoredPosition = badgeRest; rewardBadge.localScale = Vector3.one; }
            shownRewardStep = -1;
            ShowRewardCount(0);
            if (sparkle != null)
                for (int i = 0; i < sparkle.Length; i++)
                {
                    if (sparkle[i]) sparkle[i].enabled = false;
                    // Only burst entries have authored start times. Idle entries must not
                    // reuse the previous opening's schedule or appear on the next first frame.
                    if (i >= BurstCount) sparkleStart[i] = float.PositiveInfinity;
                }
        }

        void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, .05f);
            open += dt;
            if (!Initialize()) return;
            if (!started)
            {
                if (star && star.gameObject.activeInHierarchy) { started = true; t = 0; }
                else if (open > FallbackTitle)
                {
                    if (title) title.Write(float.PositiveInfinity);
                    if (badgeGroup) badgeGroup.alpha = 1;
                    ShowRewardCount(1);
                }
                if (!started) return;
            }
            else t += dt;

            if (reduced) { Calm(); return; }
            PoseStar();
            PoseCushion();
            PoseShadow();
            PoseLight();
            Write(t - WriteStart);
            Burst();
            Twinkle();
            PoseReward();
        }

        void Calm()
        {
            float a = Mathf.Clamp01(t / .20f);
            if (starGroup) starGroup.alpha = a;
            star.anchoredPosition = starRest; star.localScale = Vector3.one; star.localRotation = Quaternion.identity;
            if (cushion) cushion.localScale = Vector3.one;
            if (shadowGroup) shadowGroup.alpha = .28f * a;
            if (starShadow) starShadow.localScale = Vector3.one;
            if (contactShadow) contactShadow.color = Fade(contactShadow.color, .25f * a);
            if (glow) glow.color = Fade(glow.color, .53f * a);
            if (pillowGlow) pillowGlow.color = Fade(pillowGlow.color, .17f * a);
            if (flash) flash.color = Fade(flash.color, 0);
            if (raysGroup) raysGroup.alpha = 0;
            if (title) title.Write(float.PositiveInfinity);
            if (titleGroup) titleGroup.alpha = a;
            if (badgeGroup) badgeGroup.alpha = a;
            if (rewardBadge) rewardBadge.anchoredPosition = badgeRest;
            // Motion off presents the exact amount directly; only opacity changes.
            ShowRewardCount(1);
        }

        void PoseStar()
        {
            float size, lift, rotation;
            if (t < Drop)
            {
                float u = Mathf.Clamp01(t / Drop);
                size = Mathf.Lerp(1.70f, .90f, u * u);
                lift = Mathf.Lerp(70, -6, u * u);
                rotation = Mathf.Lerp(-15, 2, u);
            }
            else if (t < Rebound)
            {
                float u = Ease((t - Drop) / (Rebound - Drop));
                size = Mathf.Lerp(.90f, 1.055f, u);
                lift = Mathf.Lerp(-6, 4, u); rotation = Mathf.Lerp(2, -1, u);
            }
            else
            {
                float u = Ease((t - Rebound) / (Settle - Rebound));
                size = Mathf.Lerp(1.055f, 1, u);
                lift = Mathf.Lerp(4, 0, u); rotation = Mathf.Lerp(-1, 0, u);
            }
            star.localScale = Vector3.one * size;
            star.anchoredPosition = starRest + new Vector2(0, lift);
            star.localRotation = Quaternion.Euler(0, 0, rotation);
            if (starGroup) starGroup.alpha = Mathf.Clamp01(t / .065f);
        }

        void PoseCushion()
        {
            if (!cushion) return;
            float x = 1, y = 1;
            if (t >= Drop && t < Drop + .07f)
            {
                float u = Ease((t - Drop) / .07f); x = Mathf.Lerp(1, 1.055f, u); y = Mathf.Lerp(1, .84f, u);
            }
            else if (t >= Drop + .07f && t < Drop + .22f)
            {
                float u = Ease((t - Drop - .07f) / .15f); x = Mathf.Lerp(1.055f, .993f, u); y = Mathf.Lerp(.84f, 1.028f, u);
            }
            else if (t >= Drop + .22f && t < Drop + .42f)
            {
                float u = Ease((t - Drop - .22f) / .20f); x = Mathf.Lerp(.993f, 1, u); y = Mathf.Lerp(1.028f, 1, u);
            }
            cushion.localScale = new Vector3(x, y, 1);
        }

        void PoseShadow()
        {
            // The airborne star casts a broad, faint shape; it tightens and darkens as
            // the star meets the velvet. Both shadows stay on the pillow, below the star.
            float approach = Ease(t / Drop);
            float settle = Ease((t - Drop) / (Settle - Drop));
            if (shadowGroup)
                shadowGroup.alpha = t < Drop ? Mathf.Lerp(.07f, .32f, approach) * Mathf.Clamp01(t / .09f) : Mathf.Lerp(.32f, .28f, settle);
            if (starShadow)
            {
                float width = t < Drop ? Mathf.Lerp(1.27f, 1.055f, approach) : Mathf.Lerp(1.055f, 1, settle);
                float height = t < Drop ? Mathf.Lerp(1.20f, .89f, approach) : Mathf.Lerp(.89f, 1, settle);
                starShadow.localScale = new Vector3(width, height, 1);
            }
            if (contactShadow)
                contactShadow.color = Fade(contactShadow.color, .25f * Ease((t - .10f) / (Drop - .10f)));
        }

        void PoseLight()
        {
            float rise = Ease((t - Drop) / .30f);
            float breath = Mathf.Sin((t - Drop) * 2.15f);
            if (glow) glow.color = Fade(glow.color, rise * (.53f + .055f * breath));
            if (pillowGlow) pillowGlow.color = Fade(pillowGlow.color, rise * (.17f + .025f * breath));
            if (flash)
            {
                float f = t < Drop ? Mathf.Clamp01((t - Drop + .065f) / .065f) : 1 - Mathf.Clamp01((t - Drop) / .23f);
                flash.color = Fade(flash.color, .38f * f * f);
                flash.rectTransform.localScale = Vector3.one * Mathf.Lerp(.78f, 1.06f, Mathf.Clamp01((t - Drop + .065f) / .30f));
            }
            if (raysGroup) raysGroup.alpha = .27f * rise;
            if (rays) rays.localRotation = Quaternion.Euler(0, 0, Mathf.Max(0, t - Drop) * 4);
        }

        void Write(float since)
        {
            if (!title) return;
            float letters = since < 0 ? 0 : since / PerLetter;
            int count = titleText ? titleText.text.Length : 20;
            title.Write(letters >= count + 1 ? float.PositiveInfinity : letters);
        }

        float RewardProgress() => reduced ? 1 : Mathf.Clamp01((t - RewardStart) / RewardCountTime);

        void PoseReward()
        {
            float reveal = Ease((t - RewardStart) / .26f);
            if (badgeGroup) badgeGroup.alpha = reveal;
            if (rewardBadge) rewardBadge.anchoredPosition = badgeRest + new Vector2(0, -12 * (1 - reveal));
            ShowRewardCount(RewardProgress());
        }

        void ShowRewardCount(float progress)
        {
            if (!rewardAmount) return;
            int step = Mathf.Clamp(Mathf.RoundToInt(progress * RewardCountSteps), 0, RewardCountSteps);
            if (step == shownRewardStep || rewardLabels[step] == null) return;
            shownRewardStep = step;
            rewardAmount.text = rewardLabels[step];
        }

        void BuildSparkles()
        {
            if (!sparkles || (sparkle != null && sparkle.Length > 0 && sparkle[0])) return;
            int count = BurstCount + IdleCount;
            sparkle = new RawImage[count];
            sparkleStart = new float[count]; sparkleLife = new float[count]; sparkleAngle = new float[count];
            sparkleRadius = new float[count]; sparkleSize = new float[count]; sparkleSpin = new float[count];
            sparklePosition = new Vector2[count]; sparkleEnd = new Vector2[count]; sparkleHero = new bool[count];
            var texture = JellyGarden.Scripts.Juice.SparkleTextureFactory.FourPointSparkle;
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("Sparkle " + i, typeof(RectTransform), typeof(RawImage));
                go.transform.SetParent(sparkles, false);
                var image = go.GetComponent<RawImage>();
                image.texture = texture; image.raycastTarget = false; image.enabled = false; sparkle[i] = image;
                if (lightMaterial) image.material = lightMaterial;
                sparkleStart[i] = i < BurstCount ? Drop + .28f * Hash(i * 3 + 1) : float.PositiveInfinity;
                sparkleLife[i] = .55f + .25f * Hash(i * 3 + 2);
                sparkleAngle[i] = i * 2.399963f;
                sparkleRadius[i] = 128 + 60 * Hash(i * 7 + 5);
                sparkleSize[i] = (i % 4 == 0 ? 31 : 18) + 15 * Hash(i * 5 + 7);
                sparkleSpin[i] = .65f + .55f * Hash(i * 11 + 3);
            }
        }

        void Burst()
        {
            for (int i = 0; i < BurstCount; i++)
            {
                float u = (t - sparkleStart[i]) / sparkleLife[i];
                var image = sparkle[i];
                if (u < 0 || u > 1) { image.enabled = false; continue; }
                image.enabled = true;
                float angle = sparkleAngle[i] + sparkleSpin[i] * u;
                float radius = sparkleRadius[i] * (.80f + .32f * Ease(u));
                var position = starRest + new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius * .79f - 12 + 15 * u);
                float alpha = Mathf.Sin(Mathf.PI * u);
                Place(image, position, sparkleSize[i] * (.65f + .50f * alpha), angle * 20, new Color(1, .96f, .78f, alpha));
            }
        }

        void Twinkle()
        {
            for (int i = BurstCount; i < sparkle.Length; i++)
            {
                float u = (t - sparkleStart[i]) / sparkleLife[i];
                var image = sparkle[i];
                if (u < 0 || u > 1) { image.enabled = false; continue; }
                image.enabled = true;
                float envelope = Mathf.Sin(Mathf.PI * u);
                // Two highlights within each glint give the polished tips a brief shimmer.
                float alpha = envelope * (sparkleHero[i] ? .84f + .16f * Mathf.Cos(4 * Mathf.PI * u) : 1);
                Vector2 position = Vector2.Lerp(sparklePosition[i], sparkleEnd[i], Ease(u));
                float size = sparkleSize[i] * (.40f + .80f * envelope);
                Place(image, position, size, 16 * Mathf.Sin(u * Mathf.PI), new Color(1, .97f, .82f, alpha));
            }
            if (t < nextTwinkle) return;
            int index = twinkleSlot++;
            int slot = BurstCount + index % IdleCount;
            int phase = index % 15;
            bool hero = phase < 5 || phase == 14;
            sparkleHero[slot] = hero;
            sparkleStart[slot] = t;
            sparkleLife[slot] = hero ? .78f : .62f;
            sparkleSize[slot] = hero ? (phase == 0 || phase == 14 ? 48 : 40) : 24 + 6 * Hash(index * 13 + 1);
            if (hero)
            {
                sparklePosition[slot] = StarTip(phase == 14 ? 0 : phase);
                sparkleEnd[slot] = sparklePosition[slot];
            }
            else
            {
                // A continuous, left-to-right highlight catches the gold front braid.
                // It follows the actual cushion's shallow curved edge, below the star face.
                float along = (phase - 5) / 8f;
                sparklePosition[slot] = BraidPoint(along);
                sparkleEnd[slot] = BraidPoint(Mathf.Min(1, along + .07f));
            }
            nextTwinkle = t + (phase == 14 ? .95f : hero ? .14f : .09f);
        }

        Vector2 StarTip(int index)
        {
            float size = star ? star.sizeDelta.x : 256;
            switch (index % 5)
            {
                case 0: return starRest + new Vector2(0, .40f * size);
                case 1: return starRest + new Vector2(.34f, .10f) * size;
                case 2: return starRest + new Vector2(.22f, -.29f) * size;
                case 3: return starRest + new Vector2(-.22f, -.29f) * size;
                default: return starRest + new Vector2(-.34f, .10f) * size;
            }
        }

        Vector2 BraidPoint(float along)
        {
            float edge = Mathf.Abs(along * 2 - 1);
            return CushionPoint(Mathf.Lerp(-.40f, .40f, along), .345f + .085f * edge * edge);
        }

        Vector2 CushionPoint(float x, float y)
        {
            if (!cushion) return starRest + new Vector2(x * 460, -110);
            return cushion.anchoredPosition + new Vector2(x * cushion.sizeDelta.x, (y - cushion.pivot.y) * cushion.sizeDelta.y);
        }

        static void Place(RawImage image, Vector2 position, float size, float angle, Color color)
        {
            var rect = image.rectTransform;
            rect.anchoredPosition = position; rect.sizeDelta = new Vector2(size, size);
            rect.localRotation = Quaternion.Euler(0, 0, angle); image.color = color;
        }

        static float Ease(float value) => RoyalLevelCardMotion.EaseOutCubic(value);
        static float Hash(int n) { uint x = (uint)n * 2654435761u; x ^= x >> 15; x *= 2246822519u; x ^= x >> 13; return (x & 0xffffff) / (float)0x1000000; }
        static Color Fade(Color color, float alpha) { color.a = alpha; return color; }
    }
}
