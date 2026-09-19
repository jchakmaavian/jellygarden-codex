// Which trail a rocket leaves behind it, and where that choice lives.
//
// WHY A SECOND REGISTRY RATHER THAN MORE ENTRIES IN StoryVfxVariant
//
// The clear effect and the rocket trail are answers to DIFFERENT questions. A
// player picks one look for a cleared gem and, independently, one look for a
// rocket; folding both into one enum would make the two choices exclusive, so
// selecting a rocket trail would silently change the clear effect back from
// GlassBurst to whatever shared the index.
//
// So this is its own enum and its own pref, read by StoryRocketVfx alone.
// StoryVfxVariant is untouched and GlassBurst stays the clear.
//
// WHAT THE NUMBERS ARE
//
// Every field below is a knob StoryRocketVfx already had as a private const,
// tuned from the same prototype: how often a puff is laid, how long it lives,
// how far it grows, how opaque it is and how far it drifts sideways. The five
// variants differ in those alone — the head, the flame cone, the three-lobed
// puff shape and the warm tint on young smoke are shared, because they are what
// makes a plume read as a ROCKET rather than as fog, and none of the five wants
// to be anything else.
//
// The reference is a launch photograph: a hot white tip and a thick white plume
// that is still dense a long way behind it. Persistence (Life) and opacity
// (Alpha) are what carry that, more than size does.

using UnityEngine;

namespace JellyGarden.Scripts.Story
{
    /// <summary>
    /// The five rocket trails. Persisted by index, so entries may be APPENDED
    /// but never reordered — a saved choice would silently become another look.
    /// </summary>
    public enum RocketVariant
    {
        /// <summary>Dense, slow to fade, wide billow. Closest to the reference photo.</summary>
        LaunchPlume = 0,

        /// <summary>Narrow and clean, hugging the flight line. Least noise on a busy board.</summary>
        TightContrail = 1,

        /// <summary>Fewer puffs, each expanding hard. Lumpy cauliflower smoke.</summary>
        HeavyExhaust = 2,

        /// <summary>Short-lived; the bright tip does the work. Best in a long cascade.</summary>
        QuickStreak = 3,

        /// <summary>Thin, pale and lingering. Hangs rather than billows.</summary>
        SmokeTrail = 4,

        /// <summary>
        /// The imported RoyalRocketVFX package: directional rocket art, a short
        /// ignition squeeze, gold flame ribbons, white smoke and contact stars.
        /// </summary>
        RoyalStreak = 5,
    }

    public static class StoryRocketVariant
    {
        public const int Count = 6;

        private const string Pref = "StoryVfx.Rocket.Variant";
        private const string Flag = "-storyrocketvariant=";

        /// <summary>
        /// The trail in force. Cached like every other layer's switch: each
        /// rocket reads it and the answer cannot change inside one flight.
        /// </summary>
        public static RocketVariant Current
        {
            get
            {
                if (_current.HasValue) return _current.Value;
                _current = Resolve();
                return _current.Value;
            }
            set { _current = value; }
        }
        private static RocketVariant? _current;

        private static RocketVariant Resolve()
        {
            // 1. The command line wins, so a sweep can pin a trail without
            //    disturbing whatever was chosen in the lab.
            foreach (string arg in global::System.Environment.GetCommandLineArgs())
            {
                if (arg == null || !arg.StartsWith(Flag)) continue;

                int parsed;
                if (int.TryParse(arg.Substring(Flag.Length), out parsed) &&
                    parsed >= 0 && parsed < Count)
                    return (RocketVariant)parsed;
            }

            // 2. The lab's saved choice.
            if (PlayerPrefs.HasKey(Pref))
            {
                int saved = PlayerPrefs.GetInt(Pref, 0);
                if (saved >= 0 && saved < Count) return (RocketVariant)saved;
            }

            // 3. Nothing chosen yet. RoyalStreak on every build: it is the
            //    shipping rocket, drawn by RoyalRocketVFX. The five plume
            //    trails above stay reachable through -storyrocketvariant=0..4,
            //    which is what art-review/rocket-animation/README.md records.
            return RocketVariant.RoyalStreak;
        }

        public static void Save(RocketVariant variant)
        {
            PlayerPrefs.SetInt(Pref, (int)variant);
            PlayerPrefs.Save();
            _current = variant;
        }

        public static string NameOf(RocketVariant variant)
        {
            switch (variant)
            {
                case RocketVariant.LaunchPlume: return "Launch Plume";
                case RocketVariant.TightContrail: return "Tight Contrail";
                case RocketVariant.HeavyExhaust: return "Heavy Exhaust";
                case RocketVariant.QuickStreak: return "Quick Streak";
                case RocketVariant.SmokeTrail: return "Smoke Trail";
                case RocketVariant.RoyalStreak: return "Royal Streak";
                default: return variant.ToString();
            }
        }

        public static string DescriptionOf(RocketVariant variant)
        {
            switch (variant)
            {
                case RocketVariant.LaunchPlume: return "Dense, slow to fade, wide billow";
                case RocketVariant.TightContrail: return "Narrow and clean, hugs the line";
                case RocketVariant.HeavyExhaust: return "Big lumpy smoke, expands hard";
                case RocketVariant.QuickStreak: return "Short-lived, bright tip leads";
                case RocketVariant.SmokeTrail: return "Thin, pale, lingering";
                case RocketVariant.RoyalStreak: return "The shipping rocket: art, squeeze, gold exhaust";
                default: return string.Empty;
            }
        }

        /// <summary>
        /// One variant's plume. Read once per head in LaunchHead and carried on
        /// the Head itself, so a choice made mid-flight cannot re-physics the
        /// puffs already drawn behind a rocket still travelling.
        /// </summary>
        public static RocketPlume PlumeOf(RocketVariant variant)
        {
            switch (variant)
            {
                case RocketVariant.LaunchPlume:
                    return new RocketPlume(0.005f, 0.90f, 0.50f, 3.0f, 2.2f, 0.58f, 0.42f, 0.018f);

                case RocketVariant.TightContrail:
                    return new RocketPlume(0.006f, 0.70f, 0.30f, 1.8f, 1.0f, 0.52f, 0.18f, 0.018f);

                case RocketVariant.HeavyExhaust:
                    // 0.055s per cell — three times the 0.018 the others fly at,
                    // so a nine-wide board takes about half a second rather than
                    // 160ms. The trail was chosen for its big lumpy smoke, and
                    // at the old speed the head outran its own plume: the effect
                    // arrived as one cloud instead of a rocket laying one.
                    //
                    // The puff cadence drops with it. PuffEvery is seconds of
                    // TRAVEL, not of screen time, so leaving it at 0.009 while
                    // the head takes three times as long would lay three times
                    // the smoke over the same distance and bury the board.
                    return new RocketPlume(0.022f, 1.00f, 0.50f, 4.5f, 2.5f, 0.55f, 0.50f, 0.055f);

                case RocketVariant.QuickStreak:
                    return new RocketPlume(0.005f, 0.35f, 0.20f, 2.0f, 1.0f, 0.45f, 0.25f, 0.014f);

                case RocketVariant.SmokeTrail:
                    return new RocketPlume(0.008f, 1.20f, 0.60f, 2.2f, 1.2f, 0.34f, 0.30f, 0.018f);

                case RocketVariant.RoyalStreak:
                    // The ZIP profile owns this variant's tuning. Retain a
                    // valid legacy plume for tools that enumerate every style.
                    return new RocketPlume(0.022f, 1.00f, 0.50f, 4.5f, 2.5f, 0.55f, 0.50f, 0.055f);

                default:
                    // The values StoryRocketVfx shipped with, between
                    // TightContrail and QuickStreak.
                    return new RocketPlume(0.007f, 0.62f, 0.38f, 2.5f, 1.9f, 0.50f, 0.35f, 0.018f);
            }
        }
    }

    /// <summary>
    /// How one variant's smoke behaves. A struct rather than seven fields on the
    /// registry, so a variant is one row to read and one row to tune.
    /// </summary>
    public struct RocketPlume
    {
        /// <summary>Seconds of travel between puffs. Smaller is denser.</summary>
        public readonly float PuffEvery;

        /// <summary>Puff life in seconds, smallest and the random span above it.</summary>
        public readonly float LifeMin, LifeSpan;

        /// <summary>Final size as a multiple of birth radius, smallest and span.</summary>
        public readonly float GrowMin, GrowSpan;

        /// <summary>Peak opacity, held to HoldUntil of life then falling away.</summary>
        public readonly float Alpha;

        /// <summary>How fast a puff drifts sideways off the flight line.</summary>
        public readonly float Drift;

        /// <summary>
        /// Seconds the head takes to cross one cell. THE speed control.
        ///
        /// This was a single shared const in StoryRocketVfx at 0.018 — about
        /// 160ms for the head to cross a nine-wide board, which is faster than
        /// the eye can follow the tip. It lives here because the five trails
        /// want different speeds: a thin contrail can be quick and still read,
        /// while a heavy plume needs time for its smoke to build behind the
        /// head or the whole effect arrives as one puff.
        /// </summary>
        public readonly float PerCellSeconds;

        public RocketPlume(float puffEvery, float lifeMin, float lifeSpan,
            float growMin, float growSpan, float alpha, float drift,
            float perCellSeconds)
        {
            PuffEvery = puffEvery;
            LifeMin = lifeMin;
            LifeSpan = lifeSpan;
            GrowMin = growMin;
            GrowSpan = growSpan;
            Alpha = alpha;
            Drift = drift;
            PerCellSeconds = perCellSeconds;
        }
    }
}
