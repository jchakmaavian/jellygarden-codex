using UnityEngine;
using UnityEngine.UI;

namespace RoyalThrone.Presentation
{
    // Excludes a part with its own timing from RoyalCardMotion's entrance bands.
    public sealed class RoyalAppearsLater : MonoBehaviour { }

    // Presentation of the already committed win, measured from game completed experience.mp4:
    // a fast star zoom/wobble, impact flash and velvet recoil, then rear halo and braid glints.
    // AnimationManager activates Star1 with the card. This component owns no reward writes.
    public sealed class RoyalWinCard : MonoBehaviour
    {
        public RectTransform star, cushion, starShadow;
        public RoyalTextStyle title;
        public RoyalWinTitleLetters titleArtwork;
        public RoyalWinConfetti confetti;
        public RoyalCardShape glow, flash, pillowGlow, contactShadow;
        public RectTransform sparkles, haloSparkles, rays, rewardBadge, coinOrigin;
        public Text rewardAmount, levelNumber;
        public Vector2 starRest;

        const float Drop = .20f, Rebound = .34f, Settle = .64f, CushionSettle = .98f;
        const float WriteStart = .10f, PerLetter = .04f, LetterPop = .40f, FallbackTitle = 1.6f;
        const float RewardStart = .70f, RewardCountTime = .35f, RewardReady = .98f;
        const int BurstCount = 24, IdleCount = 14, HaloCount = 12, RewardCountSteps = 64;

        float t, open, nextTwinkle, sparkleCeiling;
        bool started, reduced, initialized;
        int twinkleSlot, rewardCoins = -1, shownRewardStep = -1;
        Text titleText;
        Material lightMaterial;
        CanvasGroup starGroup, shadowGroup, titleGroup, raysGroup, badgeGroup;
        Vector2 badgeRest;
        RawImage[] sparkle;
        RawImage[] haloSparkle;
        float[] haloPhase, haloSpeed, haloRadius, haloAspect, haloSize;
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
        public int ActiveHaloSparkles
        {
            get
            {
                int count = 0;
                if (haloSparkle != null) foreach (var image in haloSparkle) if (image && image.enabled && image.color.a > .01f) count++;
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
            started = false; t = 0; open = 0; nextTwinkle = .92f; twinkleSlot = 0;
            var level = JellyGarden.Scripts.LevelManager.THIS;
            RefreshLevelNumber();
            SetReward(level ? JellyGarden.Scripts.Juice.RoyalWinSequence.Coins(level) : 0);
            ResetVisuals();
            // AddComponent can call OnEnable before the card builder assigns our references.
            // BuildSparkles is intentionally deferred until the first pose has a real parent to use.
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
            RefreshLevelNumber();
            starGroup = Group(star);
            shadowGroup = Group(starShadow);
            titleGroup = Group(titleArtwork ? titleArtwork.transform : title ? title.transform : null);
            raysGroup = Group(rays);
            badgeGroup = Group(rewardBadge);
            if (rewardBadge) badgeRest = rewardBadge.anchoredPosition;
            if (title)
            {
                titleText = title.GetComponent<Text>();
                title.rewardWave = true; title.rewardDepth = true; title.revealSpan = LetterPop / PerLetter;
            }
            sparkleCeiling = ((RectTransform)transform).rect.yMax - 190;
            if (titleText)
            {
                var textRect = titleArtwork ? titleArtwork.RectTransform : titleText.rectTransform;
                sparkleCeiling = transform.InverseTransformPoint(textRect.TransformPoint(new Vector3(0, textRect.rect.yMin, 0))).y - 8;
            }
            // Light adds warmth over the navy rather than mixing a dull opaque veil into it.
            // This card owns one shared UI material; its pillow, star and shadows retain normal blending.
            var lightShader = Resources.Load<Shader>("RoyalGameplay/UIAdditive");
            if (lightShader)
            {
                lightMaterial = new Material(lightShader) { name = "Royal win additive light", hideFlags = HideFlags.HideAndDontSave };
                if (glow) glow.material = lightMaterial;
                if (flash) flash.material = lightMaterial;
                if (pillowGlow) pillowGlow.material = lightMaterial;

                // Normal alpha preserves the warm gold beam color against the deep blue face.
            }
            BuildSparkles();
            BuildHaloSparkles();
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

        void RefreshLevelNumber()
        {
            var level = JellyGarden.Scripts.LevelManager.THIS;
            if (levelNumber && level) levelNumber.text = level.currentLevel.ToString();
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
            SetTitle(0);
            if (confetti) confetti.Pose(0, reduced);
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
            if (haloSparkle != null)
                foreach (var image in haloSparkle) if (image) image.enabled = false;
        }

        // Menu activation events run in Update. Pose afterward so the first rendered card frame includes its star.
        void LateUpdate()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, .05f);
            open += dt;
            if (!Initialize()) return;
            if (!started)
            {
                if (star && star.gameObject.activeInHierarchy) { started = true; t = 0; }
                else if (open > FallbackTitle)
                {
                    SetTitle(float.PositiveInfinity);
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
            PoseHalo();
            if (confetti) confetti.Pose(t, false);
            Write(t - WriteStart);
            Burst();
            Twinkle();
            PoseReward();
        }

        void Calm()
        {
            if (confetti) confetti.Pose(t, true);
            float a = Mathf.Clamp01(t / .20f);
            if (starGroup) starGroup.alpha = a;
            star.anchoredPosition = starRest; star.localScale = Vector3.one; star.localRotation = Quaternion.identity;
            if (cushion) cushion.localScale = Vector3.one;
            if (shadowGroup) shadowGroup.alpha = .28f * a;
            if (starShadow) starShadow.localScale = Vector3.one;
            if (contactShadow) contactShadow.color = Fade(contactShadow.color, .25f * a);
            if (glow) glow.color = Fade(glow.color, .28f * a);
            if (pillowGlow) pillowGlow.color = Fade(pillowGlow.color, .12f * a);
            if (flash) flash.color = Fade(flash.color, 0);
            if (raysGroup) raysGroup.alpha = .55f * a;
            SetTitle(float.PositiveInfinity);
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
                // The reference retracts toward a nearly fixed centre; it does not fall down a long path.
                // Its peak is normalized for our already 1.5x larger reward and shared card frame.
                float u = Mathf.Clamp01(t / Drop);
                size = t < .06f ? Mathf.Lerp(2.4f, 1.50f, Ease(t / .06f))
                    : t < .15f ? Mathf.Lerp(1.50f, 1.30f, (t - .06f) / .09f)
                    : Mathf.Lerp(1.30f, .76f, (t - .15f) / .05f);
                lift = 0;
                rotation = 28 * Mathf.Cos(u * Mathf.PI * 3) * Mathf.Pow(1 - u, 1.2f);
            }
            else if (t < Rebound)
            {
                float u = Ease((t - Drop) / (Rebound - Drop));
                size = Mathf.Lerp(.76f, 1.08f, u);
                lift = Mathf.Lerp(0, 7, u); rotation = Mathf.Lerp(0, -2, u);
            }
            else
            {
                float u = Ease((t - Rebound) / (Settle - Rebound));
                size = Mathf.Lerp(1.08f, 1, u);
                lift = Mathf.Lerp(7, 0, u); rotation = Mathf.Lerp(-2, 0, u);
            }
            star.localScale = Vector3.one * size;
            star.anchoredPosition = starRest + new Vector2(0, lift);
            star.localRotation = Quaternion.Euler(0, 0, rotation);
            if (starGroup) starGroup.alpha = 1;
        }

        void PoseCushion()
        {
            if (!cushion) return;
            float x = 1, y = 1;
            if (t >= Drop && t < Drop + .04f)
            {
                float u = Ease((t - Drop) / .04f); x = Mathf.Lerp(1, 1.055f, u); y = Mathf.Lerp(1, .88f, u);
            }
            else if (t >= Drop + .04f && t < Drop + .16f)
            {
                float u = Ease((t - Drop - .04f) / .12f); x = Mathf.Lerp(1.055f, .96f, u); y = Mathf.Lerp(.88f, 1.30f, u);
            }
            else if (t >= Drop + .16f && t < Drop + .40f)
            {
                float u = Ease((t - Drop - .16f) / .24f); x = Mathf.Lerp(.96f, 1.035f, u); y = Mathf.Lerp(1.30f, .965f, u);
            }
            else if (t >= Drop + .40f && t < CushionSettle)
            {
                float u = Ease((t - Drop - .40f) / (CushionSettle - Drop - .40f)); x = Mathf.Lerp(1.035f, 1, u); y = Mathf.Lerp(.965f, 1, u);
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
                float width = t < Drop ? Mathf.Lerp(1.15f, 1.035f, approach) : Mathf.Lerp(1.035f, 1, settle);
                float height = t < Drop ? Mathf.Lerp(1.10f, .96f, approach) : Mathf.Lerp(.96f, 1, settle);
                starShadow.localScale = new Vector3(width, height, 1);
            }
            if (contactShadow)
                contactShadow.color = Fade(contactShadow.color, .25f * Ease((t - .12f) / (Drop - .12f)));
        }

        void PoseLight()
        {
            float rise = Ease((t - Drop) / .34f);
            float breath = Mathf.Sin((t - Drop) * 1.30f);
            float impact = t < Drop ? Mathf.Clamp01((t - Drop + .025f) / .025f) : 1 - Mathf.Clamp01((t - Drop) / .30f);
            if (glow) glow.color = Fade(glow.color, rise * (.22f + .025f * breath));
            if (pillowGlow) pillowGlow.color = Fade(pillowGlow.color, rise * (.12f + .018f * breath) + .26f * impact * impact);
            if (flash)
            {
                flash.color = Fade(flash.color, .52f * impact * impact);
                flash.rectTransform.localScale = Vector3.one * Mathf.Lerp(.70f, 1.08f, Mathf.Clamp01((t - Drop + .025f) / .325f));
            }
            if (raysGroup) raysGroup.alpha = (.88f + .08f * breath) * rise;
            if (rays) rays.localRotation = Quaternion.Euler(0, 0, Mathf.Max(0, t - Drop) * 1.5f);
        }

        void SetTitle(float letters) { if (titleArtwork) titleArtwork.Write(letters); else if (title) title.Write(letters); }

        void Write(float since)
        {
            if (!title) return;
            float letters = since < 0 ? 0 : since / PerLetter;
            int count = titleText ? titleText.text.Length : 20;
            // The final exclamation gets the same full envelope; reaching its onset must not snap it to rest.
            SetTitle(letters >= Mathf.Max(0, count - 1) + title.revealSpan ? float.PositiveInfinity : letters);
        }

        float RewardProgress() => reduced ? 1 : Mathf.Clamp01((t - RewardStart) / RewardCountTime);

        void PoseReward()
        {
            float reveal = Ease((t - RewardStart) / .32f);
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
            float starSize = star ? star.sizeDelta.x : 414;
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("Sparkle " + i, typeof(RectTransform), typeof(RawImage));
                go.transform.SetParent(sparkles, false);
                var image = go.GetComponent<RawImage>();
                image.texture = texture; image.raycastTarget = false; image.enabled = false; sparkle[i] = image;
                if (lightMaterial) image.material = lightMaterial;
                sparkleStart[i] = i < BurstCount ? Drop + .18f * Hash(i * 3 + 1) : float.PositiveInfinity;
                sparkleLife[i] = .70f + .30f * Hash(i * 3 + 2);
                sparkleAngle[i] = i * 2.399963f;
                sparkleRadius[i] = Mathf.Min(235, starSize * (.43f + .13f * Hash(i * 7 + 5)));
                sparkleSize[i] = (i % 4 == 0 ? 35 : 20) + 15 * Hash(i * 5 + 7);
                sparkleSpin[i] = .22f + .24f * Hash(i * 11 + 3);
            }
        }

        void BuildHaloSparkles()
        {
            if (!haloSparkles || (haloSparkle != null && haloSparkle.Length > 0 && haloSparkle[0])) return;
            haloSparkle = new RawImage[HaloCount];
            haloPhase = new float[HaloCount]; haloSpeed = new float[HaloCount]; haloRadius = new float[HaloCount];
            haloAspect = new float[HaloCount]; haloSize = new float[HaloCount];
            var texture = JellyGarden.Scripts.Juice.SparkleTextureFactory.FourPointSparkle;
            float starSize = star ? star.sizeDelta.x : 414;
            float outerRadius = Mathf.Min(235, starSize * .57f), innerRadius = Mathf.Min(outerRadius - 8, starSize * .48f);
            for (int i = 0; i < HaloCount; i++)
            {
                var go = new GameObject("Halo sparkle " + i, typeof(RectTransform), typeof(RawImage));
                go.transform.SetParent(haloSparkles, false);
                var image = go.GetComponent<RawImage>();
                image.texture = texture; image.raycastTarget = false; image.enabled = false;
                if (lightMaterial) image.material = lightMaterial;
                haloSparkle[i] = image;
                haloPhase[i] = i * Mathf.PI * 2 / HaloCount;
                haloSpeed[i] = .18f + .04f * Hash(i * 11 + 7);
                haloRadius[i] = Mathf.Lerp(innerRadius, outerRadius, Hash(i * 13 + 8));
                haloAspect[i] = .82f + .08f * Hash(i * 7 + 2);
                haloSize[i] = i % 4 == 0 ? 38 : 15 + 16 * Hash(i * 5 + 9);
            }
        }

        void PoseHalo()
        {
            if (haloSparkle == null) return;
            float age = Mathf.Max(0, t - Drop), rise = Ease((age - .04f) / .60f);
            for (int i = 0; i < haloSparkle.Length; i++)
            {
                var image = haloSparkle[i];
                image.enabled = rise > .001f;
                if (!image.enabled) continue;
                float angle = haloPhase[i] + age * haloSpeed[i];
                float pulse = .5f + .5f * Mathf.Sin(age * 1.9f + i * 1.7f);
                float alpha = rise * (.08f + .82f * pulse * pulse * pulse);
                // The orbit sits below the heading and physically behind both the star and
                // cushion. Its hidden lower arc gives the visible rim glints a sense of depth.
                float height = Mathf.Min(haloRadius[i] * haloAspect[i], Mathf.Max(30, sparkleCeiling - starRest.y + 12 - haloSize[i] * .85f));
                var position = starRest + new Vector2(Mathf.Cos(angle) * haloRadius[i], Mathf.Sin(angle) * height - 12);
                var color = i % 4 == 0 ? new Color(1, .96f, .77f, alpha) : new Color(1, .86f, .48f, alpha);
                Place(image, position, haloSize[i] * (.65f + .50f * pulse), angle * Mathf.Rad2Deg * .15f, color);
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
                float radius = sparkleRadius[i] * (.86f + .14f * Ease(u));
                float height = Mathf.Min(radius * .84f, Mathf.Max(30, sparkleCeiling - starRest.y - sparkleSize[i] * .85f));
                var position = starRest + new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * height - 8 + 8 * u);
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
            sparkleSize[slot] = hero ? (phase == 0 || phase == 14 ? 56 : 46) : 24 + 6 * Hash(index * 13 + 1);
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
