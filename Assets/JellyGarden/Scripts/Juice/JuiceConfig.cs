using UnityEngine;

namespace JellyGarden.Scripts.Juice
{
    /// <summary>
    /// Every tunable number for the match presentation lives here so timings can be
    /// dialled in without touching gameplay code.
    ///
    /// Loaded from Resources/JuiceConfig.asset when present; falls back to the
    /// defaults below so the game still runs if the asset is missing. Nothing in
    /// this class affects match rules, scoring, targets or board state.
    /// </summary>
    [CreateAssetMenu(fileName = "JuiceConfig", menuName = "JellyGarden/Juice Config", order = 0)]
    public class JuiceConfig : ScriptableObject
    {
        private const string ResourcePath = "JuiceConfig";

        private static JuiceConfig _instance;

        public static JuiceConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<JuiceConfig>(ResourcePath);
                    if (_instance == null)
                    {
                        _instance = CreateInstance<JuiceConfig>();
                        _instance.name = "JuiceConfig (runtime defaults)";
                    }
                }
                return _instance;
            }
        }

        // ------------------------------------------------------------------
        [Header("Swap")]
        [Tooltip("Seconds for a successful swap. Both items travel simultaneously.")]
        [Range(0.05f, 0.4f)] public float SwapDuration = 0.13f;

        [Tooltip("Total seconds for the reject-and-return animation on an invalid swap.")]
        [Range(0.1f, 0.5f)] public float InvalidSwapDuration = 0.22f;

        [Tooltip("How far (0-1 of a cell) an invalid swap pushes before it bounces back.")]
        [Range(0.1f, 0.9f)] public float InvalidSwapPushFraction = 0.42f;

        // ------------------------------------------------------------------
        [Header("Match - timing")]
        // Stage durations sum to ~160ms: 25 + 35 + 25 anticipation/squash/rebound,
        // then a 75ms pop. The burst fires at the 85ms mark.
        [Tooltip("Anticipation hold before the squash begins.")]
        [Range(0f, 0.1f)] public float MatchAnticipation = 0.025f;

        [Tooltip("Squash stage duration.")]
        [Range(0.01f, 0.12f)] public float MatchSquashDuration = 0.035f;

        [Tooltip("Rebound stage duration before the pop.")]
        [Range(0.01f, 0.12f)] public float MatchReboundDuration = 0.025f;

        [Tooltip("How long the fruit takes to scale/fade away once it pops.")]
        [Range(0.03f, 0.2f)] public float MatchDestroyDuration = 0.075f;

        [Tooltip("Delay between neighbouring fruits in the same match group.")]
        [Range(0f, 0.06f)] public float MatchStagger = 0.02f;

        // ------------------------------------------------------------------
        [Header("Match - shape")]
        [Tooltip("Squash strength. 0.07 => scale X 1.07 / Y 0.92.")]
        [Range(0f, 0.2f)] public float MatchSquashAmount = 0.07f;

        [Tooltip("Anticipation shrink before the squash.")]
        [Range(0f, 0.15f)] public float MatchAnticipationScale = 0.05f;

        [Tooltip("Overshoot on the rebound before the fruit pops.")]
        [Range(0f, 0.3f)] public float MatchReboundAmount = 0.12f;

        // All VFX sizes below are multiplied by the board's cell size at runtime
        // (0.6 world units on the shipped levels), so they are expressed in cells.
        // ------------------------------------------------------------------
        [Header("VFX - flash")]
        [Range(0.02f, 0.2f)] public float FlashDuration = 0.08f;
        [Range(0.1f, 3f)] public float FlashStartScale = 0.8f;
        [Range(0.1f, 3f)] public float FlashEndScale = 2.3f;

        // ------------------------------------------------------------------
        [Header("VFX - primary stars")]
        [Range(0, 24)] public int StarParticleCount = 9;
        [Tooltip("Outward launch speed (world units/sec) before drag slows them.")]
        [Range(0.5f, 12f)] public float ParticleVelocity = 6f;
        [Tooltip("Time-correct deceleration. Higher = the burst tightens up faster.")]
        [Range(0f, 20f)] public float ParticleDrag = 3f;
        [Range(0.05f, 1f)] public float ParticleLifetime = 0.32f;
        [Range(0.05f, 1.5f)] public float StarSize = 0.85f;

        // ------------------------------------------------------------------
        [Header("VFX - micro sparkles")]
        [Range(0, 40)] public int MicroParticleCount = 13;
        [Range(0.05f, 1f)] public float MicroLifetime = 0.28f;
        [Range(0.02f, 0.8f)] public float MicroSize = 0.3f;

        // ------------------------------------------------------------------
        [Header("VFX - coloured fruit fragments")]
        [Range(0, 20)] public int FragmentCount = 6;
        [Range(0.05f, 1f)] public float FragmentLifetime = 0.3f;
        [Range(0.05f, 1.5f)] public float FragmentSize = 0.55f;

        // ------------------------------------------------------------------
        [Header("VFX - layer stagger")]
        [Tooltip("Delay from the flash to the primary star burst.")]
        [Range(0f, 0.1f)] public float StarDelay = 0.01f;
        [Tooltip("Delay from the flash to the micro sparkles (spec wants T=110-180ms).")]
        [Range(0f, 0.1f)] public float MicroDelay = 0.035f;
        [Tooltip("Delay from the flash to the coloured fragments.")]
        [Range(0f, 0.1f)] public float FragmentDelay = 0.02f;
        [Tooltip("How much of the fruit colour bleeds into the fragments (rest is white).")]
        [Range(0f, 1f)] public float FragmentColorBlend = 0.8f;

        // ------------------------------------------------------------------
        [Header("VFX - match centre emphasis")]
        public bool EnableCenterEmphasis = true;
        [Range(0.05f, 0.5f)] public float CenterRingDuration = 0.24f;
        [Range(0.1f, 3f)] public float CenterRingEndScale = 1.5f;
        [Range(0, 16)] public int CenterStarCount = 5;

        // ------------------------------------------------------------------
        [Header("Falling")]
        [Tooltip("Initial fall speed in world units/sec.")]
        [Range(1f, 30f)] public float FallSpeed = 9f;
        [Tooltip("Gravity applied while falling (world units/sec^2).")]
        [Range(0f, 120f)] public float FallAcceleration = 42f;
        [Tooltip("Upper bound so long drops stay readable.")]
        [Range(5f, 60f)] public float FallMaxSpeed = 26f;

        [Header("Landing")]
        // 0.075 rather than 0.06: the (1-k)^0.6 damping envelope in LandingSquashCor
        // caps the actual dip at ~79.8% of this value, so 0.075 lands the requested
        // 1.00 -> 0.94 -> 1.02 -> 1.00 shape.
        [Tooltip("Landing squash depth. 0.075 => scale Y dips to ~0.94 after damping.")]
        [Range(0f, 0.25f)] public float LandingSquash = 0.075f;
        [Tooltip("Total landing squash+rebound time.")]
        [Range(0.03f, 0.3f)] public float LandingSquashDuration = 0.1f;

        // ------------------------------------------------------------------
        [Header("Hit-stop")]
        [Tooltip("Micro-freeze on the pop. Sells impact weight better than extra animation.")]
        public bool EnableHitStop = true;

        [Tooltip("Freeze length for a normal 3-match, in real seconds. 40-80ms is the useful band.")]
        [Range(0f, 0.12f)] public float HitStopDuration = 0.045f;

        [Tooltip("timeScale during the freeze. 0.12 reads as a hard hit; 1 disables it.")]
        [Range(0.01f, 1f)] public float HitStopScale = 0.12f;

        [Tooltip("Extra freeze length per point of match intensity (bigger match = heavier hit).")]
        [Range(0f, 0.1f)] public float HitStopIntensityBonus = 0.025f;

        // ------------------------------------------------------------------
        [Header("Cascade")]
        [Tooltip("Extra emphasis added per cascade step (chain 2, 3, 4+).")]
        [Range(0f, 0.5f)] public float CascadeIntensityMultiplier = 0.14f;
        [Tooltip("Hard ceiling so long cascades never flood the board.")]
        [Range(1f, 3f)] public float CascadeIntensityMax = 1.55f;

        [Header("Larger matches")]
        [Tooltip("Extra emphasis for a 4-match (0.18 => +18%).")]
        [Range(0f, 1f)] public float FourMatchBonus = 0.18f;
        [Tooltip("Extra emphasis for a 5+ match.")]
        [Range(0f, 1f)] public float FiveMatchBonus = 0.36f;

        // ------------------------------------------------------------------
        // ------------------------------------------------------------------
        // ------------------------------------------------------------------
        [Header("Candy shatter (break-apart debris)")]
        [Tooltip("Break the candy into chunky mesh fragments cut from its own sprite.")]
        public bool EnableCandyShatter = true;

        [Tooltip("Pooled fragment objects. ~6 per candy, so this covers roughly 16 " +
                 "simultaneous candies before the effect degrades gracefully.")]
        [Range(16, 256)] public int ShatterPoolSize = 96;

        [Tooltip("Draw order for fragments. Board pieces are 2 and the sparkle burst is 25, " +
                 "so 20 puts debris above the board and under the sparkles.")]
        public int ShatterSortingOrder = 20;

        [Tooltip("Outward launch speed, in candy-widths per second.")]
        [Range(0.5f, 8f)] public float ShatterBurstSpeed = 2.35f;

        [Tooltip("Extra upward push at launch, so some chunks arc before falling.")]
        [Range(0f, 4f)] public float ShatterUpwardBias = 1.05f;

        [Tooltip("Radians of angular scatter applied to each chunk's outward direction. " +
                 "Stops the burst reading as a perfectly symmetrical radial star.")]
        [Range(0f, 2f)] public float ShatterDirectionJitter = 0.85f;

        [Tooltip("Downward acceleration (world units/sec^2). This is what sells the weight.")]
        [Range(-60f, 0f)] public float ShatterGravity = -20f;

        [Tooltip("Velocity bleed-off per second, so chunks lose momentum before gravity wins.")]
        [Range(0f, 6f)] public float ShatterDrag = 1.7f;

        [Range(0f, 900f)] public float ShatterSpinMin = 110f;
        [Range(0f, 900f)] public float ShatterSpinMax = 460f;

        [Tooltip("Fragment lifetime in seconds, varied +/-22% per chunk.")]
        [Range(0.15f, 1.2f)] public float ShatterLifetime = 0.45f;

        [Tooltip("Fraction of life before fade/shrink begins. Keeps chunks solid while they move.")]
        [Range(0.1f, 1f)] public float ShatterFadeStart = 0.5f;

        [Tooltip("Scale reached at the end of the fade.")]
        [Range(0.1f, 1f)] public float ShatterEndScale = 0.55f;

        [Header("Bloom (URP)")]
        [Tooltip("Creates the Volume and enables camera post-processing at runtime. " +
                 "No scene file is modified.")]
        public bool EnableBloom = true;

        [Tooltip("Only pixels brighter than this bloom. Keep at/above 1.0 so the board " +
                 "art (which never exceeds 1.0) stays exactly as it looks today and only " +
                 "the HDR-boosted sparkles glow.")]
        [Range(0.5f, 3f)] public float BloomThreshold = 1.05f;

        [Range(0f, 3f)] public float BloomIntensity = 0.62f;

        [Tooltip("How far the glow spreads.")]
        [Range(0f, 1f)] public float BloomScatter = 0.58f;

        [Tooltip("Multiplier applied to the VFX shader output so sparkles clear the " +
                 "bloom threshold. 1 = no glow.")]
        [Range(1f, 6f)] public float VfxHdrBoost = 1.55f;

        [Header("Rendering")]
        [Tooltip("Sorting order for VFX. Fruits render at 2, so keep this above it.")]
        public int VfxSortingOrder = 25;
        public string VfxSortingLayer = "Default";

        [Header("Master")]
        [Tooltip("Turn the whole juice layer off to compare against the original feel.")]
        public bool EnableJuice = true;
    }
}
