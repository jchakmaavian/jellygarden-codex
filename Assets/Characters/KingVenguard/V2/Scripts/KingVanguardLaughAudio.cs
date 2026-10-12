using UnityEngine;
using Spine.Unity;
using JellyGarden.Scripts.Juice;
using PlayerPrefs = RoyalThrone.Testing.RoyalSave;

namespace RoyalThrone.Characters
{
    [DisallowMultipleComponent]
    public sealed class KingVanguardLaughAudio : MonoBehaviour
    {
        static KingVanguardLaughAudio speaking;
        KingVanguardLaughter laugh;
        SkeletonGraphic graphic;
        AudioSource voice;
        bool retired, started, paused;
        public bool IsPlaying => voice && voice.isPlaying;
        public float PlaybackTime => voice ? voice.time : 0;
        public void Begin(KingVanguardLaughter performance)
        {
            StopVoice();
            if (speaking && speaking != this) speaking.StopVoice();
            speaking = this; laugh = performance; graphic = GetComponent<SkeletonGraphic>();
            if (!voice)
            {
                voice = gameObject.AddComponent<AudioSource>();
                voice.playOnAwake = false; voice.loop = false; voice.spatialBlend = 0;
                voice.priority = 45; voice.clip = Resources.Load<AudioClip>("KingVanguard/HoHoHo");
            }
            retired = !voice.clip || !Application.isFocused || PlayerPrefs.GetInt("Sound",1) == 0;
            started = paused = false;
        }
        void Update()
        {
            if (retired || !laugh) return;
            if (!laugh.Active || !laugh.IsVisible() || PlayerPrefs.GetInt("Sound",1) == 0)
            { StopVoice(); return; }
            float at = laugh.Time - KingVanguardLaughTiming.LeadIn;
            if (at < 0) return;
            if (at >= voice.clip.length) { StopVoice(); return; }
            bool frozen = Time.timeScale <= 0 || (graphic && (graphic.freeze || graphic.timeScale <= 0));
            if (frozen) { if (started && !paused) voice.Pause(); paused = true; return; }
            voice.volume = .9f * RoyalEffectsProfile.Get.effectsVolume;
            float speed = graphic ? graphic.timeScale * (graphic.unscaledTime ? 1 : Time.timeScale) : Time.timeScale;
            voice.pitch = Mathf.Clamp(speed, .1f, 3f);
            if (!started) { voice.time = at; voice.Play(); started = true; paused = false; }
            else if (paused) { voice.time = at; voice.UnPause(); paused = false; }
            else if (Mathf.Abs(voice.time-at) > .09f) voice.time = at;
        }
        public void StopVoice()
        {
            if (voice) voice.Stop(); retired = true; started = paused = false;
            if (speaking == this) speaking = null;
        }
        void OnDisable() { StopVoice(); }
        void OnApplicationFocus(bool focused) { if (!focused) StopVoice(); }
        void OnApplicationPause(bool pause) { if (pause) StopVoice(); }
    }
}
