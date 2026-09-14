// Which explosion a bomb draws, and where that choice lives.
//
// A third registry beside StoryVfxVariant (the clear) and StoryRocketVariant
// (the trail), for the reason given in that file's header: these are answers to
// DIFFERENT questions, and folding them into one enum would make the choices
// exclusive — picking an explosion would silently change the clear effect back
// from GlassBurst.
//
// WHAT A VARIANT CONTROLS
//
// Every one of the five is the same four beats in different proportions:
//
//   GATHER   an optional wind-up before anything expands. Implosion spends the
//            most time here and it is the whole point of that variant; Energy
//            Pulse spends a little; the rest spend none.
//   CORE     the bright centre, how big it gets and how long it lives.
//   RING     the shockwave, how far it travels and how thick it stays.
//   DEBRIS   how many pieces are thrown and how far.
//
// So the struct below is those four groups, and StoryBombVfx draws them. A
// variant is a row of numbers rather than a branch in the drawing code, which
// is what made the five rocket trails cheap to compare and re-tune.
//
// WHAT A VARIANT DOES NOT CONTROL
//
// The board recoil. StoryFinishVfx.Punchit already punches GameField on a
// damped spring and the rocket already uses it, so every variant calls it with
// its own strength rather than carrying its own copy. That file's header
// records why it is GameField and never the camera: StoryPixelSnap owns the
// camera every frame and would correct a punch away, or worse, walk the board
// off the screen.

using UnityEngine;

namespace JellyGarden.Scripts.Story
{
    /// <summary>
    /// The five explosions. Persisted by index, so entries may be APPENDED but
    /// never reordered — a saved choice would silently become another look.
    /// </summary>
    public enum BombVariant
    {
        /// <summary>Flash, chunky star burst, shockwave, debris, smoke.</summary>
        CartoonBoom = 0,

        /// <summary>White-hot core into an orange fireball, flame ring, embers.</summary>
        Fireball = 1,

        /// <summary>Gathers inward, then a clean spherical burst and two rings.</summary>
        EnergyPulse = 2,

        /// <summary>Flash, branches to each cleared cell, electric ring, sparks.</summary>
        ElectricDetonation = 3,

        /// <summary>Dust sucked in, a held compression, then the biggest blast.</summary>
        Implosion = 4,
    }

    public static class StoryBombVariant
    {
        public const int Count = 5;

        private const string Pref = "StoryVfx.Bomb.Variant";
        private const string Flag = "-storybombvariant=";

        public static BombVariant Current
        {
            get
            {
                if (_current.HasValue) return _current.Value;
                _current = Resolve();
                return _current.Value;
            }
            set { _current = value; }
        }
        private static BombVariant? _current;

        private static BombVariant Resolve()
        {
            foreach (string arg in global::System.Environment.GetCommandLineArgs())
            {
                if (arg == null || !arg.StartsWith(Flag)) continue;

                int parsed;
                if (int.TryParse(arg.Substring(Flag.Length), out parsed) &&
                    parsed >= 0 && parsed < Count)
                    return (BombVariant)parsed;
            }

            if (PlayerPrefs.HasKey(Pref))
            {
                int saved = PlayerPrefs.GetInt(Pref, 0);
                if (saved >= 0 && saved < Count) return (BombVariant)saved;
            }

            // Nothing chosen yet. Implosion, because it is the one the owner
            // named as their own first choice and so the sensible thing to see
            // before comparing.
            return BombVariant.Implosion;
        }

        public static void Save(BombVariant variant)
        {
            PlayerPrefs.SetInt(Pref, (int)variant);
            PlayerPrefs.Save();
            _current = variant;
        }

        public static string NameOf(BombVariant variant)
        {
            switch (variant)
            {
                case BombVariant.CartoonBoom: return "Cartoon Boom";
                case BombVariant.Fireball: return "Fireball";
                case BombVariant.EnergyPulse: return "Energy Pulse";
                case BombVariant.ElectricDetonation: return "Electric";
                case BombVariant.Implosion: return "Implosion";
                default: return variant.ToString();
            }
        }

        public static string DescriptionOf(BombVariant variant)
        {
            switch (variant)
            {
                case BombVariant.CartoonBoom: return "Flash, star burst, shockwave, debris";
                case BombVariant.Fireball: return "White core into an orange fireball";
                case BombVariant.EnergyPulse: return "Gathers in, clean burst, two rings";
                case BombVariant.ElectricDetonation: return "Branches to each cleared cell";
                case BombVariant.Implosion: return "Sucks in, holds, then the big blast";
                default: return string.Empty;
            }
        }

        /// <summary>
        /// One variant's explosion. Read once when a bomb goes off and carried
        /// on the blast, so a choice made while an explosion is still on screen
        /// cannot re-physics what is already drawn.
        /// </summary>
        /// <summary>
        /// ONE explosion, matched to the owner's reference frames.
        ///
        /// The five variants this replaced were all small — cores of 1.6 to 3.2
        /// cells and ten to twenty-two pieces — and the owner's verdict was that
        /// they looked identical. They did, and the reason was scale rather than
        /// tuning: at that size the differences between them were smaller than
        /// the effect itself, so no amount of re-tuning the five would have
        /// separated them.
        ///
        /// The reference is a different thing entirely. Frame one is a white
        /// core four or five cells across with the board blown out behind it;
        /// frame two is the board covered in small GEM-COLOURED shards, far more
        /// than twenty, scattered well past the nine cells a package clears. So
        /// the numbers here are taken from those two pictures rather than from a
        /// prototype: a core that dominates, seventy shards in the kit's own six
        /// colours, and grey smoke at the blast edge which none of the five had
        /// at all.
        ///
        /// The enum is kept, and every entry returns this. That is deliberate:
        /// the registry, the pref and the -storybombvariant flag all still work,
        /// so a future comparison can be reopened by giving the cases different
        /// bodies again, without re-plumbing anything.
        /// </summary>
        public static BombBlast BlastOf(BombVariant variant)
        {
            return new BombBlast(
                // No wind-up. Frame one is already the flash — a white pinprick
                // on an intact board — so there is no beat before it.
                gather: 0f, gatherPull: 0f,

                // The core GROWS. This is the correction that matters most: the
                // first version opened at full size and shrank, which is the
                // opposite of the reference. Across the ten frames the core is a
                // pinprick at 1, about a cell at 2, two cells at 3, and does not
                // peak until 6 — then fades through 7 and 8 and is gone by 9.
                //
                // CoreLife is therefore the whole grow-and-fade, not a hold:
                // StoryBombVfx ramps it up over the first 45% and fades it out
                // over the rest.
                coreSize: 4.2f, coreLife: 0.72f,

                // A soft wide wave, invisible on its own. The reference shows no
                // hard ring anywhere — what reads as an edge in frames 4 and 5
                // is smoke — so this only pushes the eye outward.
                ringReach: 5.2f, ringLife: 0.50f, ringWidth: 0.60f, rings: 1,

                // Ten frames from first spark to a nearly clear board is about
                // 1.4 seconds, and frames 8 to 10 are shards ALONE, still
                // falling after the core has gone. So the debris has to outlive
                // the core by a wide margin rather than dying with it.
                debris: 64, debrisSpeed: 8.5f, debrisLife: 1.35f,

                // Warm enough that the core reads as hot without washing the
                // gem colours out of the shards in front of it.
                warm: 0.22f, punch: 1.4f, branches: 0);
        }
    }

    /// <summary>
    /// How one variant's explosion behaves. Sizes are fractions of a cell;
    /// times are seconds.
    /// </summary>
    public struct BombBlast
    {
        /// <summary>Seconds spent pulling inward before anything expands. 0 for none.</summary>
        public readonly float Gather;

        /// <summary>How hard the gather drags its dust in, as a fraction of a cell.</summary>
        public readonly float GatherPull;

        /// <summary>The bright centre: final size in cells, and how long it lives.</summary>
        public readonly float CoreSize, CoreLife;

        /// <summary>The shockwave: how far it reaches, how long, how thick, how many.</summary>
        public readonly float RingReach, RingLife, RingWidth;
        public readonly int Rings;

        /// <summary>Thrown pieces: how many, how fast, how long they live.</summary>
        public readonly int Debris;
        public readonly float DebrisSpeed, DebrisLife;

        /// <summary>0 is white and cold, 1 is full orange fire.</summary>
        public readonly float Warm;

        /// <summary>Board recoil strength, passed to StoryFinishVfx.Punchit.</summary>
        public readonly float Punch;

        /// <summary>Lightning branches drawn to nearby cells. 0 for none.</summary>
        public readonly int Branches;

        public BombBlast(float gather, float gatherPull,
            float coreSize, float coreLife,
            float ringReach, float ringLife, float ringWidth, int rings,
            int debris, float debrisSpeed, float debrisLife,
            float warm, float punch, int branches)
        {
            Gather = gather;
            GatherPull = gatherPull;
            CoreSize = coreSize;
            CoreLife = coreLife;
            RingReach = ringReach;
            RingLife = ringLife;
            RingWidth = ringWidth;
            Rings = rings;
            Debris = debris;
            DebrisSpeed = debrisSpeed;
            DebrisLife = debrisLife;
            Warm = warm;
            Punch = punch;
            Branches = branches;
        }
    }
}
