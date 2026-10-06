using UnityEngine;

namespace RoyalThrone.Match3Vfx
{
    /// <summary>
    /// Capped 2D voice pool for effect sounds: per-cue cooldowns, priorities, restrained pitch
    /// variation on repeated matches and a summed-gain limit that keeps headroom for music.
    /// Music is not handled here; listen to <see cref="Match3Vfx.MusicDuckRequested"/> to duck it.
    /// </summary>
    internal sealed class Match3Sfx
    {
        static readonly int CueCount = System.Enum.GetValues(typeof(Match3SfxCue)).Length;

        readonly Match3VfxRig rig;
        readonly int capacity;
        readonly float[] lastPlayed = new float[CueCount];
        AudioSource[] voices;
        float[] gains;
        int variation;

        public int Capacity => capacity;
        public bool Built => voices != null;
        public int PeakVoices { get; private set; }
        public int ActiveVoices
        {
            get
            {
                int n = 0;
                if (voices != null) foreach (var v in voices) if (v.isPlaying) n++;
                return n;
            }
        }

        public Match3Sfx(Match3VfxRig rig)
        {
            this.rig = rig;
            capacity = Mathf.Clamp(rig.Settings.soundVoices, 1, 32);
            for (int i = 0; i < lastPlayed.Length; i++) lastPlayed[i] = -10f;
        }

        // Voices are created on the first sound, so projects that keep their own audio pay nothing.
        void EnsureVoices()
        {
            if (voices != null) return;
            voices = new AudioSource[capacity];
            gains = new float[capacity];
            for (int i = 0; i < capacity; i++)
            {
                var v = rig.gameObject.AddComponent<AudioSource>();
                v.playOnAwake = false;
                v.spatialBlend = 0f;
                v.priority = 160;
                voices[i] = v;
            }
        }

        public AudioClip ClipFor(Match3SfxCue cue, bool advance)
        {
            var s = rig.Settings;
            switch (cue)
            {
                case Match3SfxCue.Match:
                    if (s.matchClips == null || s.matchClips.Length == 0) return null;
                    var clip = s.matchClips[variation % s.matchClips.Length];
                    if (advance) variation++;
                    return clip;
                case Match3SfxCue.Land: return s.landClip;
                case Match3SfxCue.Rocket: return s.rocketClip;
                case Match3SfxCue.Tnt: return s.tntClip;
                case Match3SfxCue.Helicopter: return s.helicopterClip;
                case Match3SfxCue.Create: return s.createClip;
                case Match3SfxCue.Click: return s.clickClip;
                case Match3SfxCue.Firework: return s.fireworkClip;
                case Match3SfxCue.Celebrate: return s.celebrateClip;
            }
            return null;
        }

        public bool Play(Match3SfxCue cue, float strength = 1f)
        {
            if (!rig.Host.SoundEnabled) return false;
            int k = (int)cue;
            float interval = cue == Match3SfxCue.Celebrate ? 1.2f : cue == Match3SfxCue.Firework ? 0.28f
                : cue == Match3SfxCue.Match ? 0.065f : cue == Match3SfxCue.Land ? 0.09f : 0.07f;
            if (Time.unscaledTime - lastPlayed[k] < interval) return false;

            var clip = ClipFor(cue, true);
            if (!clip) return false;
            lastPlayed[k] = Time.unscaledTime;

            float gain = cue == Match3SfxCue.Land ? 0.16f : cue == Match3SfxCue.Match ? 0.58f : cue == Match3SfxCue.Tnt ? 0.95f
                : cue == Match3SfxCue.Click ? 0.42f : cue == Match3SfxCue.Firework ? 0.5f : cue == Match3SfxCue.Celebrate ? 0.7f : 0.72f;
            int priority = cue == Match3SfxCue.Land ? 200 : cue == Match3SfxCue.Match ? 160 : cue == Match3SfxCue.Firework ? 90 : 60;
            float pitch = cue == Match3SfxCue.Match ? 1f + ((variation % 5) - 2) * 0.012f : 1f;
            bool played = PlayClip(clip, gain * Mathf.Clamp(strength, 0.6f, 1.2f), priority, pitch);
            if (played && (cue == Match3SfxCue.Tnt || cue == Match3SfxCue.Rocket || cue == Match3SfxCue.Celebrate))
                Match3Vfx.RaiseMusicDuck(0.42f);
            return played;
        }

        public bool PlayClip(AudioClip clip, float gain = 1f, int priority = 150, float pitch = 1f)
        {
            if (!clip || !rig.Host.SoundEnabled) return false;
            EnsureVoices();
            int chosen = -1;
            for (int i = 0; i < voices.Length; i++) if (!voices[i].isPlaying) { chosen = i; break; }
            // Steal only a voice of lower importance (higher number = lower priority).
            if (chosen < 0) for (int i = 0; i < voices.Length; i++) if (voices[i].priority > priority) { chosen = i; break; }
            if (chosen < 0) return false;

            var v = voices[chosen];
            v.Stop();
            v.clip = clip;
            v.priority = priority;
            v.pitch = pitch;
            gains[chosen] = gain * rig.Settings.effectsVolume;
            v.Play();
            Balance();
            PeakVoices = Mathf.Max(PeakVoices, ActiveVoices);
            return true;
        }

        void Balance()
        {
            if (voices == null) return;
            float total = 0f;
            for (int i = 0; i < voices.Length; i++) if (voices[i].isPlaying) total += gains[i];
            float headroom = Mathf.Min(1f, rig.Settings.mixHeadroom / Mathf.Max(0.001f, total));
            for (int i = 0; i < voices.Length; i++) voices[i].volume = gains[i] * headroom;
        }

        public void Tick()
        {
            if (voices == null) return;
            if (!rig.Host.SoundEnabled)
                foreach (var v in voices) if (v.isPlaying) v.Stop();
            Balance();
        }
    }
}
