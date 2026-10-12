using System.Collections.Generic;
using JellyGarden.Scripts;
using JellyGarden.Scripts.Juice;
using RoyalThrone.Testing;
using UnityEngine;
using UnityEngine.Rendering;

namespace RoyalThrone.ReferenceGameplay
{
    // A retained child of an existing pooled booster actor. The actor owns gameplay,
    // its native child owns artwork/clip curves; neither changes the other's timing.
    [DisallowMultipleComponent]
    public sealed class ReferenceActivationVisual : MonoBehaviour
    {
        static readonly List<ReferenceActivationVisual> active = new List<ReferenceActivationVisual>();
        GameObject view;
        Animator animator;
        Transform source;
        Renderer[] previous;
        bool[] previousMask;
        Behaviour suppressed;
        bool suppressedWasEnabled, audioPaused, held;
        AudioSource voice;
        float gain;
        string queuedState;
        string viewKind;
        float stateRemaining;
        public bool Active { get; private set; }
        public static int ActiveCount => active.Count;
        public static int PlayingVoices { get { int count = 0; foreach (var actor in active) if (actor && actor.voice && (actor.voice.isPlaying || actor.audioPaused)) count++; return count; } }
        public Vector3 SourceOrigin => source ? source.position : transform.position;

        public static ReferenceActivationVisual Attach(GameObject owner, string kind, int sortingOrder, Behaviour suppress = null)
        {
            var prefab = Resources.Load<GameObject>("ReferenceGameplay/Visuals/" + kind);
            if (!prefab) return null;
            var seat = owner.GetComponent<ReferenceActivationVisual>();
            if (!seat) seat = owner.AddComponent<ReferenceActivationVisual>();
            if (seat.Active) seat.Release();
            // Projectile actors are shared by boosters. A cached TNT view must not
            // be reused as the distinct TNT+TNT animation (or another booster).
            if (seat.view && seat.viewKind != kind)
            {
                seat.view.SetActive(false);
                Destroy(seat.view);
                seat.view = null; seat.animator = null; seat.source = null;
            }
            if (!seat.view)
            {
                seat.previous = owner.GetComponentsInChildren<Renderer>(true);
                seat.previousMask = new bool[seat.previous.Length];
                seat.view = Instantiate(prefab, owner.transform, false);
                seat.viewKind = kind;
                seat.view.name = "Imported " + kind + " activation";
                seat.animator = seat.view.GetComponentInChildren<Animator>(true);
                seat.source = seat.view.transform.Find("Animator/Main") ?? seat.view.transform;
            }
            seat.view.transform.localPosition = Vector3.zero;
            seat.view.transform.localRotation = Quaternion.identity;
            seat.view.transform.localScale = Vector3.one * ReferenceGameplayCatalog.CellScale(LevelManager.THIS, owner.transform);
            foreach (var t in seat.view.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = owner.layer;
            var sorting = seat.view.GetComponent<SortingGroup>();
            if (sorting) { sorting.sortingLayerID = SortingLayer.NameToID("Default"); sorting.sortingOrder = sortingOrder; }
            for (int i = 0; i < seat.previous.Length; i++)
                if (seat.previous[i]) { seat.previousMask[i] = seat.previous[i].forceRenderingOff; seat.previous[i].forceRenderingOff = true; }
            seat.suppressed = suppress;
            if (suppress) { seat.suppressedWasEnabled = suppress.enabled; suppress.enabled = false; }
            seat.view.SetActive(true);
            if (seat.animator) { seat.animator.enabled = true; seat.animator.Rebind(); seat.animator.Update(0); seat.animator.speed = 1; }
            seat.queuedState = null; seat.held = false; seat.Active = true;
            if (!active.Contains(seat)) active.Add(seat);
            return seat;
        }

        public float Play(string state, string then = null, float normalizedTime = 0)
        {
            queuedState = null;
            if (!Active || !animator || !animator.runtimeAnimatorController) return 0;
            foreach (var clip in animator.runtimeAnimatorController.animationClips)
                if (clip.name == state)
                {
                    held = false; animator.speed = 1; animator.Play(state, 0, normalizedTime); animator.Update(0);
                    stateRemaining = clip.length * (1 - Mathf.Clamp01(normalizedTime)); queuedState = then;
                    return clip.length;
                }
            Debug.LogWarning("[Reference Gameplay] Native activation clip unavailable: " + state);
            return 0;
        }
        public void Hold() { held = true; queuedState = null; if (animator) animator.speed = 0; }
        public void Offset(Vector3 localOffset) { if (view) view.transform.localPosition = localOffset; }
        public void Hide() { if (view) view.SetActive(false); }
        public Transform Socket(string name)
        {
            if (view) foreach (var child in view.GetComponentsInChildren<Transform>(true))
                if (child.name == name) return child;
            return source ? source : transform;
        }

        // Only sustained native cues need owner lifetime. Short accents use the shared
        // ReferenceGameplayAudio capped mixer. No imported cue is synthesized or looped.
        public void PlayOwned(string nativeName, float volume = .45f)
        {
            StopOwned();
            var clip = ReferenceGameplayAudio.Load(nativeName);
            if (!clip || RoyalSave.GetInt("Sound", 1) == 0) return;
            int voices = 0;
            foreach (var actor in active) if (actor && actor.voice && (actor.voice.isPlaying || actor.audioPaused)) voices++;
            if (voices >= 8) return;
            if (!voice) { voice = gameObject.AddComponent<AudioSource>(); voice.playOnAwake = false; voice.spatialBlend = 0; voice.priority = 150; }
            voice.clip = clip; voice.pitch = 1; voice.loop = false; gain = volume;
            voice.volume = volume * (RoyalEffectsProfile.Get ? RoyalEffectsProfile.Get.effectsVolume : 1);
            voice.Play();
        }
        public void StopOwned() { if (voice) { voice.Stop(); voice.clip = null; } audioPaused = false; }

        void Update()
        {
            if (!Active) return;
            bool pause = LevelManager.THIS && LevelManager.THIS.gameStatus == GameState.Pause;
            if (animator) animator.speed = pause || held ? 0 : 1;
            if (!pause && queuedState != null && (stateRemaining -= Time.deltaTime) <= 0) Play(queuedState);
            if (!voice) return;
            if (RoyalSave.GetInt("Sound", 1) == 0) { StopOwned(); return; }
            if (pause && voice.isPlaying) { voice.Pause(); audioPaused = true; }
            else if (!pause && audioPaused) { voice.UnPause(); audioPaused = false; }
            float total = 0;
            foreach (var actor in active) if (actor && actor.voice && actor.voice.isPlaying) total += actor.gain;
            voice.volume = gain * Mathf.Min(1, .60f / Mathf.Max(.001f, total)) * (RoyalEffectsProfile.Get ? RoyalEffectsProfile.Get.effectsVolume : 1);
        }
        public void Release()
        {
            if (!Active) return;
            Active = false; queuedState = null; StopOwned(); active.Remove(this);
            if (animator) { animator.speed = 0; animator.enabled = false; }
            if (view)
            {
                foreach (var ps in view.GetComponentsInChildren<ParticleSystem>(true)) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                view.SetActive(false); view.transform.localPosition = Vector3.zero;
            }
            for (int i = 0; i < previous.Length; i++) if (previous[i]) previous[i].forceRenderingOff = previousMask[i];
            if (suppressed) suppressed.enabled = suppressedWasEnabled;
        }
        void OnDisable() { Release(); }
        void OnDestroy() { active.Remove(this); }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Reset() { active.Clear(); }
    }
}
