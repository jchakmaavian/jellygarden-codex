using System.Collections.Generic;
using JellyGarden.Scripts;
using JellyGarden.Scripts.Juice;
using UnityEngine;

namespace RoyalThrone.ReferenceGameplay
{
    // Original PCM decoded from the APK. This only selects short gameplay voices;
    // the existing voice cap, sound setting and music mixer retain their ownership.
    public static class ReferenceGameplayAudio
    {
        static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
        static int match;
        static readonly string[] matches = { "match_explode1__15971", "match_explode2__15612", "match_explode3__15652", "match_explode4__15914", "match_explode5__15888", "match_explode6__15850" };
        public static AudioClip Load(string nativeName)
        {
            if (!cache.TryGetValue(nativeName, out var clip))
            {
                clip = Resources.Load<AudioClip>("ReferenceGameplay/Audio/" + nativeName);
                cache[nativeName] = clip;
            }
            return clip;
        }
        public static AudioClip For(RoyalCue cue)
        {
            if (!ReferenceGameplayCatalog.Available) return null;
            switch (cue)
            {
                case RoyalCue.Match: return Load(matches[match++ % matches.Length]);
                case RoyalCue.Land: return Load("fall_hit_1__15531");
                case RoyalCue.Rocket: return Load("rocket_explode__15712");
                case RoyalCue.Tnt: return Load("tnt_explode__15838");
                case RoyalCue.Helicopter: return Load("propeller_take_off__15745");
                default: return null;
            }
        }
        public static void Reveal(ItemsTypes type)
        {
            string name = type == ItemsTypes.BOMB ? "lightball_creation__15586" : type == ItemsTypes.PACKAGE ? "tnt_creation__15882" :
                type == ItemsTypes.HELICOPTER ? "propeller_creation__15500" : "rocket_creation__15581";
            RoyalAudioDirector.Get?.PlayClip(Load(name), .72f, 60, 1);
        }
        public static void Play(string nativeName, float gain = .72f) => RoyalAudioDirector.Get?.PlayClip(Load(nativeName), gain, 90, 1);
        /// <summary>Every gameplay voice For() and Reveal() can pick, for the level-start warm-up.</summary>
        public static IEnumerable<AudioClip> GameplayClips()
        {
            if (!ReferenceGameplayCatalog.Available) yield break;
            foreach (var name in matches) yield return Load(name);
            foreach (var name in new[] { "fall_hit_1__15531", "rocket_explode__15712", "tnt_explode__15838", "propeller_take_off__15745",
                "lightball_creation__15586", "tnt_creation__15882", "propeller_creation__15500", "rocket_creation__15581" })
                yield return Load(name);
        }
        public static bool Owns(AudioClip clip)
        {
            if (!clip) return false;
            foreach (var value in cache.Values) if (value == clip) return true;
            return false;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { cache.Clear(); match = 0; }
    }
}
