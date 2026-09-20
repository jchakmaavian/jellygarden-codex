using PlayerPrefs = RoyalThrone.Testing.RoyalSave;
using UnityEngine;

namespace JellyGarden.Scripts.Juice
{
    // Presentation only. Never waits on, consumes or queries matching rules.
    public sealed class RoyalToolkitImpact : MonoBehaviour
    {
        sealed class Slot
        {
            public GameObject go; public ParticleSystem[] systems;
            public bool fire, active; public float age, life;
        }
        static RoyalToolkitImpact instance;
        static bool attempted;
        RoyalToolkitLibrary library;
        Slot[] pool;
        uint seed = 41731;
        GameState previousState;
        bool observedState;
        public int ActiveCount { get; private set; }
        public int PeakActive { get; private set; }
        public int Capacity => pool == null ? 0 : pool.Length;
        public int ParticleCapacity { get; private set; }
        public int PlayedIce { get; private set; }
        public int PlayedFire { get; private set; }
        public static RoyalToolkitImpact Existing => instance;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { instance = null; attempted = false; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Prewarm() { Get(); }
        static RoyalToolkitImpact Get()
        {
            if (!Application.isPlaying || attempted) return instance;
            attempted = true;
            var data = Resources.Load<RoyalToolkitLibrary>("RoyalGameplay/Toolkit/Library");
            if (!data || !data.ice || !data.fire) return null;
            instance = new GameObject("Royal toolkit impacts — fixed pool").AddComponent<RoyalToolkitImpact>();
            instance.library = data;
            instance.Build();
            DontDestroyOnLoad(instance.gameObject);
            return instance;
        }
        void Build()
        {
            int ice = Mathf.Clamp(library.icePool, 1, 4), fire = Mathf.Clamp(library.firePool, 1, 2);
            pool = new Slot[ice + fire];
            for (int i = 0; i < pool.Length; i++)
            {
                bool isFire = i >= ice;
                var go = Instantiate(isFire ? library.fire : library.ice, transform);
                go.SetActive(false);
                var systems = go.GetComponentsInChildren<ParticleSystem>(true);
                foreach (var ps in systems)
                {
                    ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ps.useAutoRandomSeed = false;
                    var main = ps.main; main.playOnAwake = false; main.useUnscaledTime = false;
                    ParticleCapacity += main.maxParticles;
                }
                pool[i] = new Slot { go = go, systems = systems, fire = isFire };
            }
        }
        public static bool TryPlayIce(Vector3 position, float cell, float intensity = 1)
            => TryPlay(false, position, cell, intensity);
        public static bool TryPlayFire(Vector3 position, float cell, float intensity = 1)
            => TryPlay(true, position, cell, intensity);
        static bool TryPlay(bool fire, Vector3 position, float cell, float intensity)
        {
            if (!JuiceConfig.Instance.EnableJuice || PlayerPrefs.GetInt("RoyalThrone.HomeMotion", 1) == 0) return false;
            var service = Get();
            return service && service.Play(fire, position, Mathf.Max(.01f, cell), intensity);
        }
        bool Play(bool fire, Vector3 position, float cell, float intensity)
        {
            foreach (var slot in pool)
            {
                if (slot.active || slot.fire != fire) continue;
                slot.go.transform.SetPositionAndRotation(position + Vector3.back * .65f, Quaternion.identity);
                float size = (fire ? library.fireScale : library.iceScale) * Mathf.Lerp(1, 1.22f, Mathf.InverseLerp(1, 1.55f, intensity));
                slot.go.transform.localScale = Vector3.one * cell * size;
                slot.age = 0; slot.life = fire ? library.fireLifetime : library.iceLifetime;
                slot.active = true; slot.go.SetActive(true);
                foreach (var ps in slot.systems)
                {
                    ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ps.randomSeed = ++seed;
                    ps.Play(false);
                }
                ActiveCount++; PeakActive = Mathf.Max(PeakActive, ActiveCount);
                if (fire) PlayedFire++; else PlayedIce++;
                return true;
            }
            // An already visible wave has priority. Never allocate or restart an active tail.
            return true;
        }
        void Update()
        {
            if (pool == null) return;
            var level = LevelManager.THIS;
            bool paused = level && level.gameStatus == GameState.Pause;
            if (level)
            {
                var state = level.gameStatus;
                if (observedState && previousState != state && (state == GameState.Map || state == GameState.PrepareGame)) Clear();
                previousState = state; observedState = true;
            }
            foreach (var slot in pool)
            {
                if (!slot.active) continue;
                foreach (var ps in slot.systems)
                {
                    if (paused && !ps.isPaused) ps.Pause(false);
                    else if (!paused && ps.isPaused) ps.Play(false);
                }
                if (paused) continue;
                slot.age += Time.deltaTime;
                if (slot.age >= slot.life) Release(slot);
            }
        }
        void Release(Slot slot)
        {
            foreach (var ps in slot.systems) ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            slot.go.SetActive(false); slot.active = false; ActiveCount--;
        }
        public void Clear()
        {
            if (pool != null) foreach (var slot in pool) if (slot.active) Release(slot);
        }
        void OnDisable() { Clear(); }
    }
}
