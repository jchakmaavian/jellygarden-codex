// The story's own audio: Alfred's narration, and the ducking that keeps the
// music out of his way.
//
// Clips are resolved by convention from Resources/Story/Audio, and every one is
// optional — the story plays silently rather than not at all.

using System.Collections.Generic;
using JellyGarden.Scripts;
using UnityEngine;

namespace JellyGarden.Scripts.Story
{
    public sealed class StoryAudio : MonoBehaviour
    {
        private const string VoiceRoot = "Story/Audio/VO/";

        /// <summary>How far the music drops while a line is playing.</summary>
        private const float DuckedMusicScale = 0.3f;
        private const float DuckFadeSeconds = 0.25f;

        private static StoryAudio _instance;

        private AudioSource _voice;
        private float _musicVolumeBeforeDuck = -1f;
        private bool _ducked;

        private static readonly Dictionary<string, AudioClip> _cache =
            new Dictionary<string, AudioClip>();
        private static readonly HashSet<string> _warned = new HashSet<string>();

        public static bool IsSpeaking
        {
            get { return _instance != null && _instance._voice != null && _instance._voice.isPlaying; }
        }

        private static StoryAudio Ensure()
        {
            if (_instance != null) return _instance;

            var go = new GameObject("~StoryAudio");
            go.hideFlags = HideFlags.HideAndDontSave;
            StoryObjects.Persist(go);
            _instance = go.AddComponent<StoryAudio>();
            _instance._voice = go.AddComponent<AudioSource>();
            _instance._voice.playOnAwake = false;
            _instance._voice.spatialBlend = 0f;
            return _instance;
        }

        public static AudioClip LoadVoice(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;

            AudioClip clip;
            if (_cache.TryGetValue(key, out clip)) return clip;

            clip = Resources.Load<AudioClip>(VoiceRoot + key);
            _cache[key] = clip;

            if (clip == null && _warned.Add(key))
                Debug.Log("[Story] No narration for '" + key + "'; the line plays silently.");
            return clip;
        }

        /// <summary>Plays a narration clip, ducking the music for its duration.</summary>
        public static void PlayVoice(AudioClip clip)
        {
            if (clip == null) return;
            if (!VoiceEnabled) return;

            StoryAudio audio = Ensure();
            audio._voice.Stop();
            audio._voice.clip = clip;
            audio.Duck();
            audio._voice.Play();
        }

        public static void PlayVoice(string key)
        {
            PlayVoice(LoadVoice(key));
        }

        /// <summary>Alfred's line over an area's clue card, when one was recorded.</summary>
        public static void PlayClueVoice(int areaId)
        {
            PlayVoice(string.Format("alfred_clue_{0:000}", areaId));
        }

        public static void StopVoice()
        {
            if (_instance == null || _instance._voice == null) return;
            _instance._voice.Stop();
            _instance.Unduck();
        }

        /// <summary>
        /// Narration follows the kit's own sound toggle. There is no separate VO
        /// setting to forget to respect.
        /// </summary>
        private static bool VoiceEnabled
        {
            get { return PlayerPrefs.GetInt("Sound", 1) != 0; }
        }

        private void Duck()
        {
            if (_ducked || MusicBase.Instance == null) return;

            AudioSource music = MusicBase.Instance.GetComponent<AudioSource>();
            if (music == null) return;

            _musicVolumeBeforeDuck = music.volume;
            _ducked = true;
        }

        private void Unduck()
        {
            if (!_ducked) return;
            _ducked = false;

            if (MusicBase.Instance == null) return;
            AudioSource music = MusicBase.Instance.GetComponent<AudioSource>();
            if (music != null && _musicVolumeBeforeDuck >= 0f)
                music.volume = _musicVolumeBeforeDuck;
        }

        private void Update()
        {
            if (!_ducked || MusicBase.Instance == null) return;

            AudioSource music = MusicBase.Instance.GetComponent<AudioSource>();
            if (music == null) return;

            float target = _voice != null && _voice.isPlaying
                ? _musicVolumeBeforeDuck * DuckedMusicScale
                : _musicVolumeBeforeDuck;

            music.volume = Mathf.MoveTowards(music.volume, target,
                Time.unscaledDeltaTime * (_musicVolumeBeforeDuck / Mathf.Max(0.01f, DuckFadeSeconds)));

            if (!_voice.isPlaying && Mathf.Approximately(music.volume, _musicVolumeBeforeDuck))
                Unduck();
        }
    }
}
