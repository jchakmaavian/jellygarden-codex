using UnityEngine;
using RoyalThrone.Testing;

namespace JellyGarden.Scripts.Juice
{
    /// <summary>
    /// The player's Music and SFX volumes from Settings (0-1, default full), applied on top of the game's mix in
    /// RoyalEffectsProfile (its musicVolume and effectsVolume include them). The Music and Sound switches still mute.
    /// Values are cached and re-read when the QA test save starts or ends.
    /// </summary>
    public static class RoyalAudioSettings
    {
        public const string MusicKey = "RoyalThrone.MusicVolume", SoundKey = "RoyalThrone.SoundVolume";
        static float music = -1, sound = -1; static bool cachedTesting;

        public static float Music { get { Refresh(); return music; } set { Refresh(); music = Mathf.Clamp01(value); RoyalSave.SetFloat(MusicKey, music); } }
        public static float Sound { get { Refresh(); return sound; } set { Refresh(); sound = Mathf.Clamp01(value); RoyalSave.SetFloat(SoundKey, sound); } }

        static void Refresh()
        {
            if (music >= 0 && cachedTesting == RoyalSave.Testing) return;
            cachedTesting = RoyalSave.Testing;
            music = Mathf.Clamp01(RoyalSave.GetFloat(MusicKey, 1)); sound = Mathf.Clamp01(RoyalSave.GetFloat(SoundKey, 1));
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { music = sound = -1; }
    }
}
