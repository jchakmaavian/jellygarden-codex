using System.Collections.Generic;
using JellyGarden.Scripts;
using UnityEngine;
using UnityEngine.Rendering;

namespace RoyalThrone.ReferenceGameplay
{
    // Presentation only: the existing Item still owns colour/type, hits and movement.
    // Every curve and child transform in these prefabs comes from the packaged data.
    [DisallowMultipleComponent]
    public sealed class ReferencePieceVisual : MonoBehaviour
    {
        static readonly Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();
        static readonly Dictionary<string, Stack<GameObject>> pools = new Dictionary<string, Stack<GameObject>>();
        struct PendingReturn { public string key; public GameObject view; }
        static readonly List<PendingReturn> pendingReturns = new List<PendingReturn>();
        static Transform poolRoot;
        static bool quitting;
        static bool CanPool => Application.isPlaying && !quitting;
        Item item;
        GameObject view;
        Animator animator;
        string key;
        ItemsTypes visualType;
        SpriteRenderer legacyShadow;
        bool nativeHasShadow, shadowMasked, previousShadowMask;
        bool revealing;
        bool fallAnimating, wasFalling;
        float remaining;
        public bool Revealing => revealing;
        public static int ActiveViews { get; private set; }
        public static string Key(ItemsTypes type)
        {
            switch (type)
            {
                case ItemsTypes.HORIZONTAL_STRIPPED: case ItemsTypes.VERTICAL_STRIPPED: return "Rocket";
                case ItemsTypes.PACKAGE: return "TNT";
                case ItemsTypes.BOMB: return "ColorBall";
                case ItemsTypes.HELICOPTER: return "Propeller";
                default: return null;
            }
        }
        public static bool Has(ItemsTypes type)
        {
            string name = Key(type);
            if (name == null) return false;
            if (!prefabs.TryGetValue(name, out var prefab))
            { prefab = Resources.Load<GameObject>("ReferenceGameplay/Visuals/" + name); prefabs[name] = prefab; }
            return prefab;
        }
        public static bool Attach(Item owner, bool reveal = false)
        {
            if (!owner || owner.destroying || !Has(owner.currentType)) return false;
            var seat = owner.GetComponent<ReferencePieceVisual>();
            if (!seat) seat = owner.gameObject.AddComponent<ReferencePieceVisual>();
            seat.enabled = true; seat.item = owner;
            string name = Key(owner.currentType);
            if (seat.key != name || !seat.view) { seat.Release(); seat.Acquire(name); }
            else seat.RefreshType();
            if (reveal) seat.Reveal();
            seat.Pose();
            return true;
        }
        void Acquire(string name)
        {
            key = name;
            wasFalling = false; fallAnimating = false;
            if (!pools.TryGetValue(name, out var pool)) pools[name] = pool = new Stack<GameObject>();
            while (pool.Count > 0 && !view) view = pool.Pop();
            if (!view) view = Instantiate(prefabs[name]);
            view.transform.SetParent(transform, false);
            view.transform.localPosition = Vector3.zero; view.transform.localRotation = Quaternion.identity;
            view.SetActive(true); ActiveViews++;
            animator = view.GetComponentInChildren<Animator>(true);
            if (animator) { animator.enabled = true; animator.Rebind(); animator.Update(0); }
            nativeHasShadow = false;
            foreach (var sprite in view.GetComponentsInChildren<SpriteRenderer>(true))
                if (sprite.name.IndexOf("shadow", System.StringComparison.OrdinalIgnoreCase) >= 0) { nativeHasShadow = true; break; }
            visualType = item.currentType;
            SetIdle();
        }
        void RocketBranches()
        {
            if (key == "Rocket" && animator)
            {
                var horizontal = animator.transform.Find("Horizontal");
                var vertical = animator.transform.Find("Vertical");
                if (horizontal) horizontal.gameObject.SetActive(item.currentType == ItemsTypes.HORIZONTAL_STRIPPED);
                if (vertical) vertical.gameObject.SetActive(item.currentType == ItemsTypes.VERTICAL_STRIPPED);
            }
        }
        void RefreshType()
        {
            // Both orientations share a native prefab key, but not an animation state.
            if (visualType == item.currentType) return;
            visualType = item.currentType;
            SetIdle();
        }
        string RevealState => item.currentType == ItemsTypes.HORIZONTAL_STRIPPED ? "HorizontalRocketCreationAnimation" :
            item.currentType == ItemsTypes.VERTICAL_STRIPPED ? "VerticalRocketCreationAnimation" :
            item.currentType == ItemsTypes.PACKAGE ? "TntCreationAnimation" :
            item.currentType == ItemsTypes.BOMB ? "LightballCreationAnimation" : "PropellerCreationAnimation";
        float Play(string state, float normalizedTime = 0)
        {
            if (!animator || !animator.runtimeAnimatorController) return 0;
            animator.speed = 1; animator.Play(state, 0, normalizedTime); animator.Update(0);
            foreach (var clip in animator.runtimeAnimatorController.animationClips) if (clip.name == state) return clip.length;
            return 0;
        }
        void Reveal()
        {
            fallAnimating = false;
            remaining = Play(RevealState); revealing = remaining > 0;
            RocketBranches();
            ReferenceGameplayAudio.Reveal(item.currentType);
        }
        public float RevealFromColorBall()
        {
            if (!item || item.currentType != ItemsTypes.PACKAGE || !animator) return 0;
            // This dedicated recovered clip is different from ordinary TNT creation.
            // ChangeType already owns the creation sound; do not play it twice.
            remaining = Play("TntLightballCreationAnimation");
            revealing = remaining > 0;
            return remaining;
        }
        void SetIdle()
        {
            revealing = false; fallAnimating = false;
            if (item.currentType == ItemsTypes.BOMB) Play("LightballDefaultAnimation");
            else { Play(RevealState, 1); if (animator) animator.speed = 0; }
            RocketBranches();
        }
        void LateUpdate()
        {
            if (!item || item.destroying || Key(item.currentType) != key) { Release(); return; }
            RefreshType();
            if (revealing && (!LevelManager.THIS || LevelManager.THIS.gameStatus != GameState.Pause))
            { remaining -= Time.deltaTime; if (remaining <= 0) SetIdle(); }
            if (key == "Propeller" && (!revealing || fallAnimating) && item.falling != wasFalling)
            {
                // Recovered mechanical secondary motion follows the existing fall
                // owner. Creation keeps priority; a new fall can interrupt settling.
                wasFalling = item.falling;
                remaining = Play(wasFalling ? "PropellerStartFallAnimation" : "PropellerEndFallAnimation");
                fallAnimating = remaining > 0; revealing = fallAnimating;
            }
            if (animator) animator.speed = LevelManager.THIS && LevelManager.THIS.gameStatus == GameState.Pause ? 0 : revealing || item.currentType == ItemsTypes.BOMB ? 1 : 0;
            Pose();
        }
        void Pose()
        {
            if (!view || !item || !item.sprRenderer) return;
            // Item landing/contact juice scales X and Y independently. Keep that legacy
            // root animation from deforming the recovered native hierarchy or its clips.
            var inherited = transform.lossyScale;
            float worldCell = ReferenceGameplayCatalog.CellScale(LevelManager.THIS, transform) * Mathf.Abs(inherited.x);
            view.transform.localScale = new Vector3(
                worldCell / Mathf.Max(.0001f, Mathf.Abs(inherited.x)),
                worldCell / Mathf.Max(.0001f, Mathf.Abs(inherited.y)),
                worldCell / Mathf.Max(.0001f, Mathf.Abs(inherited.z)));
            var sort = view.GetComponent<SortingGroup>();
            if (sort) { sort.sortingLayerID = item.sprRenderer.sortingLayerID; sort.sortingOrder = item.sprRenderer.sortingOrder; }
            item.sprRenderer.enabled = false;
            // RoyalPieceSkin also creates a procedural contact ellipse. Native prefabs
            // already contain their own authored shadow, so conceal only that extra one.
            if (nativeHasShadow)
            {
                if (!legacyShadow) legacyShadow = transform.Find("Royal contact shadow")?.GetComponent<SpriteRenderer>();
                if (legacyShadow && !shadowMasked) { previousShadowMask = legacyShadow.forceRenderingOff; shadowMasked = true; }
                if (legacyShadow) legacyShadow.forceRenderingOff = true;
            }
        }
        static void EnsurePool()
        {
            if (poolRoot || !CanPool) return;
            var pool = new GameObject("Reference gameplay visual pool");
            DontDestroyOnLoad(pool); poolRoot = pool.transform;
            pool.AddComponent<ReferencePieceVisualPool>();
        }
        /// <summary>
        /// Loads a power-up's view and parks up to <paramref name="count"/> idle copies in the pool, so the first Rocket, TNT,
        /// Color Ball or Propeller a level makes does not load its art and instantiate mid-cascade. Builds at most one copy
        /// per call; returns true while more are wanted.
        /// </summary>
        public static bool Prewarm(ItemsTypes type, int count)
        {
            if (!CanPool || !Has(type)) return false;
            string name = Key(type);
            if (!pools.TryGetValue(name, out var stack)) pools[name] = stack = new Stack<GameObject>();
            if (stack.Count >= Mathf.Min(count, 24)) return false;
            EnsurePool();
            var view = Instantiate(prefabs[name], poolRoot, false);
            foreach (var ps in view.GetComponentsInChildren<ParticleSystem>(true)) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            view.SetActive(false);
            stack.Push(view);
            return stack.Count < count;
        }
        static void ReturnView(string name, GameObject released)
        {
            if (!released || !CanPool) return;
            EnsurePool(); released.transform.SetParent(poolRoot, false);
            if (!pools.TryGetValue(name, out var stack)) pools[name] = stack = new Stack<GameObject>();
            if (stack.Count < 24) stack.Push(released); else Destroy(released);
        }
        internal static void FlushReturns()
        {
            if (!CanPool) { pendingReturns.Clear(); return; }
            foreach (var pending in pendingReturns) ReturnView(pending.key, pending.view);
            pendingReturns.Clear();
        }
        public void Release() { Release(false); }
        void Release(bool deferReparent)
        {
            revealing = false; fallAnimating = false; wasFalling = false;
            if (shadowMasked && legacyShadow) legacyShadow.forceRenderingOff = previousShadowMask;
            shadowMasked = false;
            if (!view) return;
            if (animator) { animator.speed = 0; animator.enabled = false; }
            foreach (var ps in view.GetComponentsInChildren<ParticleSystem>(true)) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            view.SetActive(false);
            // Unity forbids reparenting a child from the parent's SetActive callback.
            // Release ownership now, then detach on the independent pool's LateUpdate.
            if (CanPool)
            {
                if (deferReparent) { EnsurePool(); pendingReturns.Add(new PendingReturn { key = key, view = view }); }
                else ReturnView(key, view);
            }
            view = null; animator = null; ActiveViews = Mathf.Max(0, ActiveViews - 1);
            if (item && item.sprRenderer && !item.destroying) item.sprRenderer.enabled = true;
        }
        void OnDisable() { Release(true); }
        void OnDestroy() { Release(true); }
        static void Quitting() { quitting = true; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { prefabs.Clear(); pools.Clear(); pendingReturns.Clear(); poolRoot = null; ActiveViews = 0; quitting = false; Application.quitting -= Quitting; Application.quitting += Quitting; }
    }
    // Runtime-only host; the board can be inactive while deferred releases drain.
    sealed class ReferencePieceVisualPool : MonoBehaviour
    {
        void LateUpdate() { ReferencePieceVisual.FlushReturns(); }
    }
}
