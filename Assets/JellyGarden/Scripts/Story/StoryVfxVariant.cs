// Which clear effect the board draws, and where that choice lives.
//
// WHY A REGISTRY AND NOT SIX FLAGS
//
// The five existing board layers each carry their own on/off switch, and that
// is right for them: rocket, shatter, blast and finish are different EFFECTS
// that can sensibly run together. The six entries here are not — they are six
// answers to one question, "what does a cleared gem look like", and exactly one
// of them is true at a time. A bool per variant would let two be on at once and
// the board would draw both.
//
// So this is an int, chosen once, read everywhere.
//
// WHY IT IS A PREF AND NOT A CONSTANT
//
// The whole point of StoryVfxLab is to compare these on a real board and keep
// the winner without a rebuild. A const would mean a Unity build per comparison
// — six builds to answer one question, which is exactly the loop that made
// choosing an effect cost an afternoon.
//
// The command line still wins when it is present (-storyvfxvariant=3), so a
// sweep or a scripted run can pin a variant without touching the player's
// saved choice. Same order as StoryVfxFlags, for the same reasons; see that
// file's header for why the command line cannot be the only source.

using UnityEngine;

namespace JellyGarden.Scripts.Story
{
    /// <summary>
    /// The six looks a cleared gem can have. The order is the order the lab
    /// lists them, and the numbers are persisted — so entries may be APPENDED
    /// but never reordered or renumbered, or a saved choice silently becomes a
    /// different effect.
    /// </summary>
    public enum VfxVariant
    {
        /// <summary>The kit untouched: 4-frame flipbook, 250ms shrink, nothing added.</summary>
        Kit = 0,

        /// <summary>
        /// Heavy chunks. Few, large, thrown out and down, almost no drag — they
        /// arc over the board's edge like masonry. The most physical of the five.
        /// </summary>
        HeavyChunks = 1,

        /// <summary>
        /// Fine gravel. Many small pieces with a wide spread, falling fast.
        /// Reads as the gem disintegrating rather than breaking.
        /// </summary>
        FineGravel = 2,

        /// <summary>
        /// Glass burst. A hard outward throw first, then gravity takes them —
        /// pieces travel sideways before they fall, like something smashed.
        /// </summary>
        GlassBurst = 3,

        /// <summary>
        /// Slow drift. Light pieces that fall lazily with a little air
        /// resistance, staying visible longest on the way down.
        /// </summary>
        SlowDrift = 4,

        /// <summary>
        /// Straight drop. Barely any outward throw — the gem simply collapses
        /// and the pieces fall out of the board under their own weight.
        /// </summary>
        StraightDrop = 5,
    }

    public static class StoryVfxVariant
    {
        /// <summary>How many entries the lab should offer. Derived, not typed twice.</summary>
        public const int Count = 6;

        private const string Pref = "StoryVfx.Variant";
        private const string Flag = "-storyvfxvariant=";

        /// <summary>
        /// The variant in force. Cached like the other layers' switches, because
        /// every cleared gem reads this and the answer cannot change inside one
        /// run — the lab writes the pref and reloads the board, which rebuilds
        /// the cache.
        /// </summary>
        public static VfxVariant Current
        {
            get
            {
                if (_current.HasValue) return _current.Value;
                _current = Resolve();
                return _current.Value;
            }
            set { _current = value; }
        }
        private static VfxVariant? _current;

        private static VfxVariant Resolve()
        {
            // 1. The command line wins, so a sweep can pin a variant without
            //    disturbing whatever the player picked in the lab.
            foreach (string arg in global::System.Environment.GetCommandLineArgs())
            {
                if (arg == null || !arg.StartsWith(Flag)) continue;

                int parsed;
                if (int.TryParse(arg.Substring(Flag.Length), out parsed) &&
                    parsed >= 0 && parsed < Count)
                    return (VfxVariant)parsed;
            }

            // 2. The lab's saved choice.
            if (PlayerPrefs.HasKey(Pref))
            {
                int saved = PlayerPrefs.GetInt(Pref, 0);
                if (saved >= 0 && saved < Count) return (VfxVariant)saved;
            }

            // 3. Nothing chosen yet. GlassBurst rather than Kit: a development
            //    build that shows the stock board teaches nothing about the six,
            //    and Kit is always one tap away in the lab.
            // GlassBurst on EVERY build, not only development.
            //
            // The five variants were compared on a real board and this one was
            // chosen, so it is the game's clear effect rather than a debug
            // default. Leaving the release arm on Kit would have shipped the
            // untouched kit flipbook to every player while the chosen effect
            // existed only in whoever's PlayerPrefs had pressed "Use this".
            return VfxVariant.GlassBurst;
        }

        /// <summary>
        /// Keep a choice. Takes effect on the next board, because every layer
        /// caches its own answer on first read.
        /// </summary>
        public static void Save(VfxVariant variant)
        {
            PlayerPrefs.SetInt(Pref, (int)variant);
            PlayerPrefs.Save();
            _current = variant;
        }

        /// <summary>The name the lab puts on a button.</summary>
        public static string NameOf(VfxVariant variant)
        {
            switch (variant)
            {
                case VfxVariant.Kit: return "Kit (as shipped)";
                case VfxVariant.HeavyChunks: return "Heavy Chunks";
                case VfxVariant.FineGravel: return "Fine Gravel";
                case VfxVariant.GlassBurst: return "Glass Burst";
                case VfxVariant.SlowDrift: return "Slow Drift";
                case VfxVariant.StraightDrop: return "Straight Drop";
                default: return variant.ToString();
            }
        }

        /// <summary>One line under the name, so the lab is readable without this file.</summary>
        public static string DescriptionOf(VfxVariant variant)
        {
            switch (variant)
            {
                case VfxVariant.Kit: return "No wind-up, 250ms fade, flipbook only";
                case VfxVariant.HeavyChunks: return "Few big pieces, arc out and down";
                case VfxVariant.FineGravel: return "Many small pieces, wide spread";
                case VfxVariant.GlassBurst: return "Thrown sideways, then falls";
                case VfxVariant.SlowDrift: return "Light pieces, lazy fall";
                case VfxVariant.StraightDrop: return "Collapses straight down";
                default: return string.Empty;
            }
        }

        /// <summary>
        /// The physics one variant falls with.
        ///
        /// Every number here is a departure from StoryShatterVfx's own
        /// constants, and DRAG is the one that matters most. Its shipped value
        /// of 3.6 kills a piece's velocity inside the first fifth of its life,
        /// which is deliberate there — it makes a cloud bloom and hang. That is
        /// the opposite of falling out of the board, so every variant below
        /// drops it hard and leans on gravity instead.
        ///
        /// Life is long enough for a piece to cross the bottom edge: the board
        /// is nine rows at pitch 1.17, so a piece starting mid-board has about
        /// six units to fall, and at these gravities that is well under a
        /// second.
        /// </summary>
        public static ShatterPhysics PhysicsOf(VfxVariant variant)
        {
            switch (variant)
            {
                case VfxVariant.HeavyChunks:
                    return new ShatterPhysics(10, 1.05f, -16f, 3.0f, 0.35f, 0.30f, 0.58f);

                case VfxVariant.FineGravel:
                    return new ShatterPhysics(40, 0.95f, -14f, 4.2f, 0.55f, 0.10f, 0.24f);

                case VfxVariant.GlassBurst:
                    return new ShatterPhysics(18, 1.00f, -18f, 6.5f, 0.30f, 0.16f, 0.40f);

                case VfxVariant.SlowDrift:
                    return new ShatterPhysics(26, 1.35f, -6.5f, 2.2f, 1.30f, 0.14f, 0.34f);

                case VfxVariant.StraightDrop:
                    // Pieces at 4x the original 0.20-0.44. At the shipped size a
                    // piece was a fifth of a gem and read as dust rather than as
                    // the gem coming apart — the same complaint StoryShatterVfx's
                    // own header records about its first measured values.
                    //
                    // COUNT AND SIZE ARE NOT INDEPENDENT. 4x the width is 16x
                    // the area per piece, so the 16 that read as sparse at the
                    // old size covered the cell and its neighbours at the new
                    // one. Seven large pieces is about the same total area the
                    // sixteen small ones drew, which is what "the gem came
                    // apart" looks like rather than "the gem exploded".
                    return new ShatterPhysics(7, 0.90f, -20f, 0.9f, 0.20f, 0.80f, 1.76f);

                default:
                    // Kit draws no shatter at all; the values are never read.
                    return new ShatterPhysics(0, 0.1f, 0f, 0f, 0f, 0.1f, 0.2f);
            }
        }

        /// <summary>
        /// Whether this variant wants the kit's 250ms shrink shortened.
        ///
        /// Every added variant does, and for one reason: the kit fades a gem out
        /// over 250ms, which is longer than any of the six effects, so whatever
        /// is drawn on top is followed by a quarter-second of the gem melting.
        /// That fade is what makes a clear read as soft however good the effect
        /// over it is. Kit alone keeps it, because Kit means untouched.
        /// </summary>
        public static bool WantsFastFade(VfxVariant variant)
        {
            return variant != VfxVariant.Kit;
        }
    }

    /// <summary>
    /// How one variant's pieces move. A struct rather than five fields on the
    /// registry, so a variant is one row to read and one row to tune.
    /// </summary>
    public struct ShatterPhysics
    {
        /// <summary>Pieces per gem.</summary>
        public readonly int Count;

        /// <summary>Seconds a piece lives. Long enough to leave the board.</summary>
        public readonly float Life;

        /// <summary>Downward acceleration. The thing that carries pieces out.</summary>
        public readonly float Gravity;

        /// <summary>How hard a piece is thrown from the gem's centre.</summary>
        public readonly float BurstSpeed;

        /// <summary>Air resistance. Near zero lets gravity win; high makes it hang.</summary>
        public readonly float Drag;

        /// <summary>Piece size as a fraction of the gem, smallest and largest.</summary>
        public readonly float SizeMin, SizeMax;

        public ShatterPhysics(int count, float life, float gravity, float burstSpeed,
            float drag, float sizeMin, float sizeMax)
        {
            Count = count;
            Life = life;
            Gravity = gravity;
            BurstSpeed = burstSpeed;
            Drag = drag;
            SizeMin = sizeMin;
            SizeMax = sizeMax;
        }
    }
}
