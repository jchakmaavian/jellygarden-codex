using System;
using UnityEngine;

namespace RoyalThrone.Match3Vfx
{
    /// <summary>Four-stage timing for a piece pop: anticipation, squash, rebound, disappear.</summary>
    [Serializable]
    public struct Match3PopTiming
    {
        [Range(0f, 0.1f)] public float anticipation;
        [Range(0.01f, 0.12f)] public float squash;
        [Range(0.01f, 0.12f)] public float rebound;
        [Range(0.03f, 0.2f)] public float disappear;
        [Tooltip("0.07 => X 1.07 / Y 0.92 at the bottom of the squash.")]
        [Range(0f, 0.2f)] public float squashAmount;
        [Range(0f, 0.15f)] public float anticipationScale;
        [Range(0f, 0.3f)] public float reboundAmount;
        [Tooltip("SmoothStep easing instead of OutQuad/OutCubic. Softer, used for confirmed matches.")]
        public bool smooth;

        public float Total => anticipation + squash + rebound + disappear;

        /// <summary>The softened pop Royal Throne uses for confirmed 3+ matches.</summary>
        public static Match3PopTiming ConfirmedMatch => new Match3PopTiming
        {
            anticipation = 0.03f, squash = 0.045f, rebound = 0.045f, disappear = 0.09f,
            squashAmount = 0.045f, anticipationScale = 0.05f, reboundAmount = 0.065f, smooth = true
        };

        /// <summary>The short, snappier pop used when a power-up clears a piece.</summary>
        public static Match3PopTiming PowerClear => new Match3PopTiming
        {
            anticipation = 0.015f, squash = 0.03f, rebound = 0.02f, disappear = 0.05f,
            squashAmount = 0.07f, anticipationScale = 0.05f, reboundAmount = 0.12f, smooth = false
        };
    }

    /// <summary>
    /// Every tunable number of the kit. Defaults are the values tuned for Royal Throne on
    /// 14-15 September 2026. Sizes are expressed in board cells and multiplied by
    /// <see cref="IMatch3VfxHost.CellSize"/> at runtime, so a board of any scale works.
    /// Nothing here changes match rules, scoring or board state.
    /// </summary>
    [CreateAssetMenu(fileName = "Match3VfxSettings", menuName = "Royal Throne/Match-3 VFX Settings", order = 0)]
    public sealed class Match3VfxSettings : ScriptableObject
    {
        /// <summary>A settings asset at this Resources path is picked up automatically.</summary>
        public const string ResourcesPath = "Match3VfxSettings";

        [Header("Master")]
        [Tooltip("Off = pieces vanish instantly and no effects play. Useful for A/B comparison.")]
        public bool enableEffects = true;

        [Header("Swap")]
        [Range(0.05f, 0.4f)] public float swapDuration = 0.13f;
        [Range(0.1f, 0.5f)] public float invalidSwapDuration = 0.22f;
        [Tooltip("How far (0-1 of a cell) an invalid swap pushes before it returns.")]
        [Range(0.1f, 0.9f)] public float invalidSwapPushFraction = 0.42f;
        [Tooltip("OutBack overshoot on the return. 3 gives a small 'nope' settle.")]
        [Range(0f, 5f)] public float invalidSwapOvershoot = 3f;

        [Header("Match pop")]
        public Match3PopTiming matchPop = Match3PopTiming.ConfirmedMatch;
        public Match3PopTiming powerClearPop = Match3PopTiming.PowerClear;
        [Tooltip("Delay between neighbouring pieces of one group. Multiply by the piece index.")]
        [Range(0f, 0.06f)] public float matchStagger = 0.012f;
        [Tooltip("Delay from group confirmation to the shared centre accent.")]
        [Range(0f, 0.3f)] public float groupAccentDelay = 0.12f;
        [Range(0.03f, 0.2f)] public float shineDuration = 0.09f;
        [Range(0f, 1f)] public float shineStrength = 0.85f;

        [Header("Falling and landing")]
        [Range(1f, 30f)] public float fallSpeed = 12f;
        [Range(0f, 120f)] public float fallAcceleration = 55f;
        [Range(5f, 60f)] public float fallMaxSpeed = 32f;
        [Tooltip("Drops shorter than this (world units) snap without a landing squash.")]
        [Range(0f, 2f)] public float minimumAnimatedDrop = 0.5f;
        [Range(0f, 0.25f)] public float landingSquash = 0.075f;
        [Range(0.03f, 0.3f)] public float landingSquashDuration = 0.1f;

        [Header("Match burst (cells)")]
        [Range(0.02f, 0.2f)] public float flashDuration = 0.065f;
        [Range(0.1f, 3f)] public float flashStartScale = 0.8f;
        [Range(0.1f, 3f)] public float flashEndScale = 1.25f;
        [Range(0, 24)] public int starCount = 6;
        [Range(0.5f, 12f)] public float particleVelocity = 4.8f;
        [Range(0f, 20f)] public float particleDrag = 3f;
        [Range(0.05f, 1f)] public float particleLifetime = 0.34f;
        [Range(0.05f, 1.5f)] public float starSize = 0.26f;
        [Range(0, 40)] public int microCount = 8;
        [Range(0.05f, 1f)] public float microLifetime = 0.38f;
        [Range(0.02f, 0.8f)] public float microSize = 0.09f;
        [Range(0, 20)] public int fragmentCount = 1;
        [Range(0.05f, 1f)] public float fragmentLifetime = 0.3f;
        [Range(0.05f, 1.5f)] public float fragmentSize = 0.12f;
        [Range(0f, 0.1f)] public float starDelay = 0.01f;
        [Range(0f, 0.1f)] public float microDelay = 0.035f;
        [Range(0f, 0.1f)] public float fragmentDelay = 0.02f;
        [Range(0f, 1f)] public float fragmentColorBlend = 0.8f;
        [Tooltip("Burst intensity for pieces cleared by a power-up, so large clears stay readable.")]
        [Range(0.1f, 1f)] public float powerClearBurstIntensity = 0.3f;
        public bool enableCenterAccent = true;
        [Range(0.05f, 0.5f)] public float centerRingDuration = 0.24f;
        [Range(0.1f, 3f)] public float centerRingEndScale = 1.15f;
        [Range(0, 16)] public int centerStarCount = 5;

        [Header("Intensity")]
        [Range(0f, 1f)] public float fourMatchBonus = 0.18f;
        [Range(0f, 1f)] public float fiveMatchBonus = 0.36f;
        [Range(0f, 0.5f)] public float cascadeBonusPerStep = 0.14f;
        [Range(1f, 3f)] public float intensityMax = 1.55f;
        [Tooltip("Match accent sound plays from this intensity upward (1.17 = 4-match or deeper cascade).")]
        [Range(1f, 2f)] public float accentSoundIntensity = 1.17f;

        [Header("Hit-stop")]
        public bool enableHitStop = false;
        [Range(0f, 0.12f)] public float hitStopDuration = 0.045f;
        [Range(0.01f, 1f)] public float hitStopScale = 0.12f;
        [Range(0f, 0.1f)] public float hitStopIntensityBonus = 0.025f;

        [Header("Shatter debris")]
        public bool enableShatter = true;
        [Range(16, 256)] public int shatterPoolSize = 240;
        [Range(0.5f, 8f)] public float shatterBurstSpeed = 2.2f;
        [Range(0f, 4f)] public float shatterUpwardBias = 0.65f;
        [Range(0f, 2f)] public float shatterDirectionJitter = 0.85f;
        [Range(-60f, 0f)] public float shatterGravity = -34f;
        [Range(0f, 6f)] public float shatterDrag = 1.1f;
        [Range(0f, 900f)] public float shatterSpinMin = 110f;
        [Range(0f, 900f)] public float shatterSpinMax = 460f;
        [Tooltip("Safety lifetime; solid debris normally recycles after leaving the board bottom.")]
        [Range(1f, 3f)] public float shatterLifetime = 1.8f;
        [Tooltip("Optional painted chips per colour index. Empty = chips are cut from the piece sprite.")]
        public Match3BurstLibrary burstLibrary;

        [Header("Power-up timing")]
        [Range(0.03f, 0.4f)] public float rocketAnticipation = 0.22f;
        [Range(4f, 50f)] public float rocketCellsPerSecond = 8f;
        [Range(0.1f, 2f)] public float rocketMinSweep = 0.8f;
        [Range(0.1f, 3f)] public float rocketMaxSweep = 1.2f;
        [Tooltip("The intact piece stays visible this long after the rocket head reaches it.")]
        [Range(0f, 0.2f)] public float rocketContactHold = 0.055f;
        [Range(0.05f, 0.5f)] public float tntAnticipation = 0.16f;
        [Range(0.005f, 0.1f)] public float blastWavePerCell = 0.025f;
        [Range(0.1f, 0.6f)] public float helicopterLift = 0.22f;
        [Range(0.2f, 1f)] public float helicopterFlight = 0.48f;
        [Range(0f, 0.5f)] public float orbCharge = 0.16f;
        [Range(0f, 0.5f)] public float orbConvertHold = 0.18f;
        [Range(0.1f, 0.7f)] public float smokeLifetime = 0.36f;
        [Range(0f, 0.12f)] public float impactShake = 0.035f;
        [Range(8, 64)] public int projectilePool = 32;
        [Range(100, 1000)] public int sparkleBudget = 480;

        [Header("Power-up art (optional)")]
        public Sprite rocketSprite;
        public Sprite tntSprite;
        public Sprite helicopterSprite;
        [Tooltip("Soft smoke puff. Empty = generated puff.")]
        public Texture2D smokeTexture;

        [Header("Optional impact prefabs")]
        [Tooltip("Played once per confirmed match group instead of the ring accent. Leave empty for the built-in accent.")]
        public GameObject matchImpactPrefab;
        [Range(0.1f, 5f)] public float matchImpactScale = 1.1f;
        [Range(0.2f, 3f)] public float matchImpactLifetime = 0.9f;
        [Range(1, 8)] public int matchImpactPool = 4;
        [Tooltip("Played for strong blasts (TNT) instead of the built-in corona. Leave empty for the built-in blast.")]
        public GameObject blastImpactPrefab;
        [Range(0.1f, 8f)] public float blastImpactScale = 3.2f;
        [Range(0.2f, 3f)] public float blastImpactLifetime = 1.5f;
        [Range(1, 4)] public int blastImpactPool = 2;

        [Header("Sound effects (optional)")]
        [Tooltip("Alternated on every match for variation.")]
        public AudioClip[] matchClips = new AudioClip[0];
        public AudioClip landClip;
        public AudioClip rocketClip;
        public AudioClip tntClip;
        public AudioClip helicopterClip;
        public AudioClip createClip;
        public AudioClip clickClip;
        public AudioClip fireworkClip;
        public AudioClip celebrateClip;
        [Range(0f, 1f)] public float effectsVolume = 0.68f;
        [Range(4, 24)] public int soundVoices = 12;
        [Tooltip("Summed voice gain is limited to this, leaving headroom for music.")]
        [Range(0.2f, 1f)] public float mixHeadroom = 0.68f;

        [Header("Combo banner")]
        public bool enableComboBanner = true;
        [Tooltip("Cascade count that shows the first text. Each further step advances one text.")]
        [Range(2, 10)] public int bannerMinCascade = 3;
        public string[] bannerTexts = { "GREAT!", "BRAVO!", "FANTASTIC!", "BRILLIANT!" };
        public Font bannerFont;
        [Range(1f, 10f)] public float bannerCooldown = 2.4f;
        public int bannerCanvasOrder = 24;
        [Tooltip("Normalised vertical position of the banner inside the safe area.")]
        [Range(0f, 1f)] public float bannerHeight = 0.735f;
        public Color bannerTopColor = new Color(1f, 1f, 0.9f);
        public Color bannerBottomColor = new Color(1f, 0.7f, 0.17f);
        public Color bannerOutlineColor = new Color(0.12f, 0.2f, 0.41f);

        [Header("Rendering")]
        public string sortingLayerName = "Default";
        [Tooltip("Shatter debris. Keep above the board pieces.")]
        public int shatterSortingOrder = 20;
        [Tooltip("Match flash, sparkles and rings.")]
        public int burstSortingOrder = 25;
        [Tooltip("Power-up particles use this order and up to +13 above it.")]
        public int powerupSortingOrder = 26;
        [Tooltip("Additive output multiplier so HDR bloom picks up sparkles only.")]
        [Range(1f, 6f)] public float hdrBoost = 1.55f;
        [Tooltip("URP only: create a runtime bloom volume and enable camera post-processing.")]
        public bool enableUrpBloom = false;
        [Range(0.5f, 3f)] public float bloomThreshold = 1.05f;
        [Range(0f, 3f)] public float bloomIntensity = 0.62f;
        [Range(0f, 1f)] public float bloomScatter = 0.58f;

        /// <summary>Creates an in-memory settings object with the default values.</summary>
        public static Match3VfxSettings CreateDefaults()
        {
            var settings = CreateInstance<Match3VfxSettings>();
            settings.name = "Match3VfxSettings (runtime defaults)";
            settings.hideFlags = HideFlags.DontSave;
            return settings;
        }

        /// <summary>Text for a cascade count, or null when the count is below the threshold.</summary>
        public string BannerTextFor(int cascade)
        {
            if (bannerTexts == null || bannerTexts.Length == 0 || cascade < bannerMinCascade) return null;
            return bannerTexts[Mathf.Clamp(cascade - bannerMinCascade, 0, bannerTexts.Length - 1)];
        }

        void OnValidate()
        {
            if (rocketMaxSweep < rocketMinSweep) rocketMaxSweep = rocketMinSweep;
            if (shatterSpinMax < shatterSpinMin) shatterSpinMax = shatterSpinMin;
        }
    }
}
