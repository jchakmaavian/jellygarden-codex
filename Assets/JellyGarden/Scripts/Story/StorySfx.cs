// The story's own sound effects.
//
// These are for the story's moments — a repair landing, a star arriving, an
// area finishing, a tap the player cannot afford. The kit's own sounds for
// matches, combos and boosters are left exactly as they are; the board is not
// touched.
//
// Every clip is optional. A missing one falls back to the kit's nearest
// equivalent, so the game is never silent where it should not be.

using System.Collections.Generic;
using UnityEngine;

namespace JellyGarden.Scripts.Story
{
    public static class StorySfx
    {
        private const string Root = "Story/Audio/SFX/";

        public const string RepairDone = "repair_done";
        public const string StarEarned = "star_earned";
        public const string LanternLight = "lantern_light";
        public const string Invalid = "invalid";
        public const string VineCut = "vine_cut";
        public const string TimberBreak = "timber_break";
        public const string MatchPop = "match_pop";
        public const string BoosterHammer = "booster_hammer";

        private static readonly Dictionary<string, AudioClip> _cache =
            new Dictionary<string, AudioClip>();
        private static readonly HashSet<string> _warned = new HashSet<string>();

        public static AudioClip Load(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;

            AudioClip clip;
            if (_cache.TryGetValue(key, out clip)) return clip;

            clip = Resources.Load<AudioClip>(Root + key);
            _cache[key] = clip;

            if (clip == null && _warned.Add(key))
                Debug.LogWarning("[Story] Missing effect Resources/" + Root + key +
                                 "; falling back to the kit's own.");
            return clip;
        }

        /// <summary>
        /// Plays a story effect through the kit's SoundBase, so it obeys the same
        /// volume and the same mute toggle as everything else.
        /// </summary>
        public static void Play(string key, AudioClip fallback = null)
        {
            if (SoundBase.Instance == null) return;

            AudioClip clip = Load(key) ?? fallback;
            if (clip != null) SoundBase.Instance.PlaySound(clip);
        }

        /// <summary>The sound a repair task makes when it lands.</summary>
        public static void PlayRepair(bool isFinalRepair)
        {
            // The last repair of an area lights the room; ASSETS.md gives that
            // moment its own sound.
            string key = isFinalRepair ? LanternLight : RepairDone;
            AudioClip fallback = SoundBase.Instance != null
                ? SoundBase.Instance.getStarIngr
                : null;
            Play(key, fallback);
        }

        public static void PlayStarEarned()
        {
            AudioClip fallback = SoundBase.Instance != null &&
                                 SoundBase.Instance.star != null &&
                                 SoundBase.Instance.star.Length > 0
                ? SoundBase.Instance.star[0]
                : null;
            Play(StarEarned, fallback);
        }

        public static void PlayInvalid()
        {
            AudioClip fallback = SoundBase.Instance != null
                ? SoundBase.Instance.wrongMatch
                : null;
            Play(Invalid, fallback);
        }

        public static void ClearCache()
        {
            _cache.Clear();
            _warned.Clear();
        }
    }
}
