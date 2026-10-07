using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using RoyalThrone.ReferenceGameplay;
using UnityEngine;

namespace JellyGarden.Scripts.Juice
{
    /// <summary>
    /// Moves gameplay's first-use loading to the level start, while the level card is up. Without it the first match, Rocket,
    /// power-up or booster of a session loaded its prefab, textures and sounds mid-play: 90-175 ms frames in the editor
    /// profile, in the middle of the Rocket's flight. Covered: the imported effect and power-up pools, the Color Ball and
    /// Propeller sequences' own pools, and the gameplay sounds' audio data. One small step per frame; a warm step costs a lookup.
    /// </summary>
    public static class RoyalGameplayWarmup
    {
        static readonly (string key, int count)[] Effects =
        {
            ("MatchItemExplodeParticles", 16), ("RocketItemParticles", 6), ("TntItemFuseParticles", 2), ("TntItemExplodeParticles", 2),
            ("TntTntComboFuseParticles", 1), ("TntTntComboExplodeParticles", 1), ("PropellerItemExplodeParticles", 2),
            ("PropellerTargetExplodeParticles", 2), ("PropellerRocketComboParticles", 1), ("TntPropellerComboFuseParticles", 1),
            ("LightballUseParticles", 1), ("LightballItemExplodeParticles", 1), ("LightballRayPrefab", 4), ("LightballEffectedItemExplodeParticles", 8)
        };
        static readonly (ItemsTypes type, int count)[] Pieces =
            { (ItemsTypes.HORIZONTAL_STRIPPED, 3), (ItemsTypes.PACKAGE, 2), (ItemsTypes.HELICOPTER, 2), (ItemsTypes.BOMB, 1) };
        static readonly string[] DirectorClips = { "match-a", "match-b", "rocket", "tnt", "helicopter", "create", "land", "click", "firework", "celebrate", "royal-fanfare" };
        static float lastStep = -10;
        // The editor keeps statics between Play sessions (domain reload is off), while the clock restarts.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetStatics() { lastStep = -10; }

        /// <summary>Starts the warm-up on <paramref name="host"/> unless one is already running.</summary>
        public static void Begin(MonoBehaviour host)
        {
            if (!Application.isPlaying || !host || !host.isActiveAndEnabled) return;
            // A run that its host stopped leaves no flag behind: a live run steps every frame.
            float since = Time.unscaledTime - lastStep;
            if (since >= 0 && since < .5f) return;
            lastStep = Time.unscaledTime;
            host.StartCoroutine(Run());
        }

        static IEnumerator Run()
        {
            Safe(() => { _ = RoyalPowerupDirector.Get; return false; });   // builds the effects rig before the first match
            yield return null;
            foreach (var clip in Clips()) { lastStep = Time.unscaledTime; Safe(() => Prepare(clip)); yield return null; }
            foreach (var piece in Pieces)
                while (Safe(() => ReferencePieceVisual.Prewarm(piece.type, piece.count))) { lastStep = Time.unscaledTime; yield return null; }
            foreach (var effect in Effects)
                while (Safe(() => ReferenceVfxPool.Prewarm(effect.key, effect.count))) { lastStep = Time.unscaledTime; yield return null; }
            // The booster sequences build their pools on first use; these are their own prewarms, called earlier.
            // One visual per frame: their pools hold 12 each, which in one frame stalled the level card.
            for (int count = 1; count <= 12; count++)
            {
                Safe(() => { RoyalThrone.Boosters.ColorBall.V4.ColorBallVisualV4.Prewarm(count); return false; }); lastStep = Time.unscaledTime; yield return null;
                Safe(() => { RoyalThrone.Boosters.Propeller.V5.PropellerVisualV5.Prewarm(count); return false; }); lastStep = Time.unscaledTime; yield return null;
            }
            Safe(() => { ColorBallTargetVfx.Prewarm(); return false; }); lastStep = Time.unscaledTime;
        }

        static IEnumerable<AudioClip> Clips()
        {
            foreach (var name in DirectorClips) yield return Resources.Load<AudioClip>("RoyalGameplay/Audio/" + name);
            foreach (var clip in ReferenceGameplayAudio.GameplayClips()) yield return clip;
            var sounds = SoundBase.Instance;
            if (!sounds) yield break;
            foreach (var field in typeof(SoundBase).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (field.FieldType == typeof(AudioClip)) yield return field.GetValue(sounds) as AudioClip;
                else if (field.FieldType == typeof(AudioClip[]) && field.GetValue(sounds) is AudioClip[] clips)
                    foreach (var clip in clips) yield return clip;
            }
        }

        static bool Prepare(AudioClip clip)
        {
            if (clip && clip.loadState == AudioDataLoadState.Unloaded) clip.LoadAudioData();
            return false;
        }

        static bool Safe(Func<bool> step)
        {
            try { return step(); }
            catch (Exception e) { Debug.LogWarning("[Gameplay warm-up] skipped a step: " + e.Message); return false; }
        }
    }
}
