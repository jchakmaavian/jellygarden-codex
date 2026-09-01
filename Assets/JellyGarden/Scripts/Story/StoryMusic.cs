// Swaps the kit's music bed for the story's, without touching a prefab.
//
// MusicBase plays by index: the game asks for track 0 on the map and track 1 in
// a level. So the story's two themes are written into those two slots at boot —
// theme_ruin for the map and the area screens, theme_level for play — and every
// existing PlayMusic call keeps working unchanged.
//
// The kit hard-cuts between tracks. This watches for the cut and rides the
// volume through it, so a screen change is a cross-fade rather than a snap.

using UnityEngine;

namespace JellyGarden.Scripts.Story
{
    [DisallowMultipleComponent]
    public sealed class StoryMusic : MonoBehaviour
    {
        private const string MusicRoot = "Story/Audio/Music/";

        /// <summary>MusicBase's own indices: 0 is the map, 1 is a level.</summary>
        private const int MapTrack = 0;
        private const int LevelTrack = 1;

        private const float CrossFadeSeconds = 0.5f;

        private static StoryMusic _instance;

        private AudioSource _source;
        private AudioClip _lastClip;
        private float _fadeStartedAt;
        private float _targetVolume = 1f;

        /// <summary>Called by StorySceneHooks on every scene load; a no-op when this scene has no map or board.</summary>
        public static void Install()
        {
            // No one-shot flag: MusicBase does not exist until the scene that
            // owns it loads, so a flag set on the menu would block it forever.
            if (MusicBase.Instance == null) return;
            if (_instance != null) return;

            ReplaceTracks();

            _instance = MusicBase.Instance.gameObject.GetComponent<StoryMusic>();
            if (_instance == null)
                _instance = MusicBase.Instance.gameObject.AddComponent<StoryMusic>();
        }

        /// <summary>
        /// Writes the story themes into MusicBase's array. A theme that is not
        /// present leaves the kit's own track in place rather than silencing it.
        /// </summary>
        private static void ReplaceTracks()
        {
            AudioClip[] tracks = MusicBase.Instance.music;
            if (tracks == null || tracks.Length <= LevelTrack)
            {
                Debug.LogWarning("[Story] MusicBase has fewer than two tracks; " +
                                 "story themes were not installed.");
                return;
            }

            AudioClip ruin = Resources.Load<AudioClip>(MusicRoot + "theme_ruin");
            AudioClip level = Resources.Load<AudioClip>(MusicRoot + "theme_level");

            if (ruin != null) tracks[MapTrack] = ruin;
            if (level != null) tracks[LevelTrack] = level;

            if (ruin == null || level == null)
                Debug.LogWarning("[Story] A story music theme is missing from " +
                                 "Resources/" + MusicRoot + "; the kit's own track stands in.");
        }

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            if (_source != null)
            {
                _targetVolume = _source.volume;
                _lastClip = _source.clip;
            }
        }

        private void Update()
        {
            if (_source == null) return;

            // The kit swaps the clip and calls Play(); there is no event for it,
            // so the swap itself is the signal to fade.
            if (_source.clip != _lastClip)
            {
                _lastClip = _source.clip;
                _fadeStartedAt = Time.unscaledTime;
                _targetVolume = PlayerPrefs.GetInt("Music", 1) != 0 ? 1f : 0f;
                _source.volume = 0f;
            }

            if (_fadeStartedAt <= 0f) return;

            // StoryAudio owns the volume while narration is playing; stepping on
            // it here would undo the duck mid-line.
            if (StoryAudio.IsSpeaking) return;

            float t = (Time.unscaledTime - _fadeStartedAt) / CrossFadeSeconds;
            if (t >= 1f)
            {
                _source.volume = _targetVolume;
                _fadeStartedAt = 0f;
                return;
            }

            _source.volume = Mathf.Lerp(0f, _targetVolume, t);
        }
    }
}
