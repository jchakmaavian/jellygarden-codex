using System.Collections.Generic;
using JellyGarden.Scripts;
using UnityEngine;

namespace RoyalThrone.ReferenceGameplay
{
    // The two-ball motion and both particle hierarchies are recovered assets.
    // This host reconstructs their lifetime; it never damages the board.
    public sealed class ReferenceDualColorBallVisual : MonoBehaviour
    {
        static readonly List<ReferenceDualColorBallVisual> pool = new List<ReferenceDualColorBallVisual>();
        ReferenceActivationVisual view;
        ReferenceVfxPool.Handle charge;
        float age, retirement;
        bool detonated;
        public bool Active { get; private set; }
        public float Duration { get; private set; }
        public float Elapsed => age;
        public bool Ready => Active && age + .00001f >= Duration;
        public static int ActiveCount { get { int count = 0; foreach (var actor in pool) if (actor && actor.Active) count++; return count; } }
        const string Sound = "lightball_lightball_combo__rfv387_17531";

        public static ReferenceDualColorBallVisual Begin(Vector3 position)
        {
            ReferenceDualColorBallVisual actor = null;
            foreach (var candidate in pool) if (candidate && !candidate.Active) { actor = candidate; break; }
            if (!actor)
            {
                var root = new GameObject("Native dual Colorball");
                actor = root.AddComponent<ReferenceDualColorBallVisual>();
                pool.Add(actor);
            }
            actor.transform.SetPositionAndRotation(position, Quaternion.identity);
            actor.transform.localScale = Vector3.one;
            actor.gameObject.SetActive(true);
            actor.view = ReferenceActivationVisual.Attach(actor.gameObject, "ColorBallColorBallComboRfv387", 36);
            actor.Duration = actor.view ? actor.view.Play("BallBallExplodeAnimation") : 0;
            if (actor.Duration <= 0) { actor.Release(); return null; }
            actor.age = 0; actor.detonated = false; actor.Active = true;
            var audio = ReferenceGameplayAudio.Load(Sound);
            actor.retirement = Mathf.Max(actor.Duration, audio ? audio.length : 0);
            actor.view.PlayOwned(Sound, .45f);
            actor.charge = ReferenceVfxPool.Play("BallBallComboUseParticles", position);
            return actor;
        }
        public void Detonate()
        {
            if (!Active || detonated) return;
            detonated = true;
            view.Hold(); view.Hide();
            charge?.Stop();
            ReferenceVfxPool.Play("BallBallComboExplodeParticles", transform.position);
        }
        void Update()
        {
            if (!Active) return;
            var lm = LevelManager.THIS;
            if (lm && (lm.gameStatus == GameState.Map || lm.gameStatus == GameState.PrepareGame)) { Release(); return; }
            if (lm && lm.gameStatus == GameState.Pause) return;
            age += Time.deltaTime;
            if (detonated && age >= retirement) Release();
        }
        public void Release()
        {
            charge?.Stop(); charge = null;
            if (view) view.Release();
            Active = false; detonated = false;
            gameObject.SetActive(false);
        }
        void OnDisable() { if (Active) Release(); }
        void OnDestroy() { pool.Remove(this); }
        public static void Clear() { foreach (var actor in pool) if (actor && actor.Active) actor.Release(); }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { pool.Clear(); }
    }
}
