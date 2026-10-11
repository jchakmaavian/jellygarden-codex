using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RoyalThrone.ReferenceGameplay.Preview
{
    // The TNT imported from royal-throne, by the names and rules the game used there
    // (ReferencePieceVisual, ReferenceVfxPool, RoyalPowerupDirector). Shared by the
    // preview scene and King Leo's board so both draw it the same way.
    public static class ReferenceTnt
    {
        public const string PieceKey = "TNT", ComboKey = "TNTTNTComboRfv387";
        public const string CreationState = "TntCreationAnimation", ComboState = "TntTntComboExplodeAnimation";
        // ReferencePieceVisual.RevealFromColorBall: a TNT a Color Ball made has its own creation clip.
        public const string ColorBallCreationState = "TntLightballCreationAnimation";
        public const string FuseVfx = "TntItemFuseParticles", ExplodeVfx = "TntItemExplodeParticles";
        public const string ComboFuseVfx = "TntTntComboFuseParticles", ComboExplodeVfx = "TntTntComboExplodeParticles";
        public const string CreationAudio = "tnt_creation__15882", ExplodeAudio = "tnt_explode__15838", ComboAudio = "tnt_tnt_combo__rfv387_17741";
        public const string ComboFuseSocket = "FitilPartikul";
        // TNT3DAnimator.IgnitionDuration on royal-throne.
        public const float IgnitionDuration = .12f;
        // Sorting orders the game uses: VFX 35, combo body 36, its fuse 37.
        public const int VfxOrder = 35, ComboBodyOrder = 36, ComboFuseOrder = 37;

        static readonly Dictionary<string, Object> cache = new Dictionary<string, Object>();

        static T Load<T>(string path) where T : Object
        {
            if (!cache.TryGetValue(path, out var asset)) cache[path] = asset = Resources.Load<T>(path);
            return asset as T;
        }

        public static GameObject Visual(string key) => Load<GameObject>("ReferenceGameplay/Visuals/" + key);
        public static GameObject VfxPrefab(string key) => Load<GameObject>("ReferenceGameplay/VFX/" + key);
        public static AudioClip Clip(string name) => Load<AudioClip>("ReferenceGameplay/Audio/" + name);

        /// <summary>Instantiates a visual prefab at one cell's size under a sorting group.</summary>
        public static GameObject SpawnVisual(string key, Transform parent, Vector3 position, float cell, int sortingLayerId, int order)
        {
            var prefab = Visual(key);
            if (!prefab) { Debug.LogError("[Reference TNT] Missing visual " + key); return null; }
            var go = Object.Instantiate(prefab, position, Quaternion.identity, parent);
            go.transform.localScale = Vector3.one * cell;
            Sort(go, sortingLayerId, order);
            return go;
        }

        // ReferenceVfxPool's names for the blast_part_* children, by board colour index.
        static readonly string[] colorNames = { "red", "blue", "yellow", "green", "pink" };

        /// <summary>ReferenceVfxPool.Play: prefab scale × cell, sorting group, only the <paramref name="color"/> blast_part_* on (none for -1).</summary>
        public static GameObject PlayVfx(string key, Transform parent, Vector3 position, float cell, int sortingLayerId, int order, int color = -1)
        {
            var prefab = VfxPrefab(key);
            if (!prefab) { Debug.LogError("[Reference TNT] Missing VFX " + key); return null; }
            var go = Object.Instantiate(prefab, position, Quaternion.identity, parent);
            go.transform.localScale = prefab.transform.localScale * cell;
            Sort(go, sortingLayerId, order);
            foreach (var ps in go.GetComponentsInChildren<ParticleSystem>(true))
            {
                ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                if (ps.name.StartsWith("blast_part_"))
                    ps.gameObject.SetActive(color >= 0 && color < colorNames.Length && ps.name == "blast_part_" + colorNames[color]);
                if (ps.gameObject.activeInHierarchy) ps.Play(false);
            }
            return go;
        }

        public static void StopEmission(GameObject vfx)
        {
            if (vfx) foreach (var ps in vfx.GetComponentsInChildren<ParticleSystem>()) ps.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }

        public static void Pause(GameObject vfx, bool paused)
        {
            if (!vfx) return;
            foreach (var ps in vfx.GetComponentsInChildren<ParticleSystem>())
                if (paused) ps.Pause(false); else if (ps.isPaused) ps.Play(false);
        }

        public static bool Alive(GameObject vfx)
        {
            if (!vfx) return false;
            foreach (var ps in vfx.GetComponentsInChildren<ParticleSystem>()) if (ps.IsAlive(false)) return true;
            return false;
        }

        /// <summary>Plays a state from its start (or <paramref name="normalizedTime"/>); returns the clip length.</summary>
        public static float Play(Animator animator, string state, float normalizedTime = 0)
        {
            if (!animator || !animator.runtimeAnimatorController) return 0;
            animator.enabled = true; animator.speed = 1;
            animator.Play(state, 0, normalizedTime); animator.Update(0);
            foreach (var clip in animator.runtimeAnimatorController.animationClips) if (clip.name == state) return clip.length;
            Debug.LogWarning("[Reference TNT] Clip unavailable: " + state);
            return 0;
        }

        /// <summary>ReferencePieceVisual.SetIdle: the creation clip's last frame, frozen.</summary>
        public static void Idle(Animator animator)
        {
            if (!animator) return;
            Play(animator, CreationState, 1);
            animator.speed = 0;
        }

        public static Transform Find(Transform root, string name)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true)) if (child.name == name) return child;
            return null;
        }

        static void Sort(GameObject go, int sortingLayerId, int order)
        {
            var sorting = go.GetComponent<SortingGroup>();
            if (!sorting) sorting = go.AddComponent<SortingGroup>();
            sorting.sortingLayerID = sortingLayerId; sorting.sortingOrder = order;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() => cache.Clear();
    }
}
