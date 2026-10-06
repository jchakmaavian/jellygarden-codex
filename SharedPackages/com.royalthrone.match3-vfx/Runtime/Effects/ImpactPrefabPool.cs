using UnityEngine;

namespace RoyalThrone.Match3Vfx
{
    /// <summary>
    /// Optional authored impact prefabs (for example from a VFX toolkit your team licenses).
    /// A fixed number of instances is created up front and reused; nothing is instantiated
    /// during play. Without prefabs the built-in accents and blast play instead.
    /// </summary>
    internal sealed class ImpactPrefabPool
    {
        sealed class Slot
        {
            public GameObject go;
            public ParticleSystem[] systems;
            public bool blast, active;
            public float age, life;
        }

        readonly Match3VfxRig rig;
        readonly Slot[] slots;
        uint seed = 41731;

        public int Capacity => slots.Length;
        public int ActiveCount { get; private set; }
        public int PlayedMatch { get; private set; }
        public int PlayedBlast { get; private set; }

        public ImpactPrefabPool(Match3VfxRig rig)
        {
            this.rig = rig;
            var s = rig.Settings;
            int match = s.matchImpactPrefab ? Mathf.Clamp(s.matchImpactPool, 1, 8) : 0;
            int blast = s.blastImpactPrefab ? Mathf.Clamp(s.blastImpactPool, 1, 4) : 0;
            slots = new Slot[match + blast];
            if (slots.Length == 0) return;

            var root = new GameObject("Impact prefabs").transform;
            root.SetParent(rig.transform, false);
            for (int i = 0; i < slots.Length; i++)
            {
                bool isBlast = i >= match;
                var go = Object.Instantiate(isBlast ? s.blastImpactPrefab : s.matchImpactPrefab, root);
                go.SetActive(false);
                var systems = go.GetComponentsInChildren<ParticleSystem>(true);
                foreach (var ps in systems)
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ps.useAutoRandomSeed = false;
                    var main = ps.main;
                    main.playOnAwake = false;
                    main.useUnscaledTime = false;
                    main.stopAction = ParticleSystemStopAction.None;
                }
                slots[i] = new Slot { go = go, systems = systems, blast = isBlast };
            }
        }

        public bool TryPlayMatch(Vector3 position, float intensity) => TryPlay(false, position, intensity);
        public bool TryPlayBlast(Vector3 position, float intensity) => TryPlay(true, position, intensity);

        bool TryPlay(bool blast, Vector3 position, float intensity)
        {
            var s = rig.Settings;
            if (rig.ReducedMotion || (blast ? !s.blastImpactPrefab : !s.matchImpactPrefab)) return false;
            foreach (var slot in slots)
            {
                if (slot.active || slot.blast != blast) continue;
                slot.go.transform.SetPositionAndRotation(position + Vector3.back * 0.65f, Quaternion.identity);
                float size = (blast ? s.blastImpactScale : s.matchImpactScale) * Mathf.Lerp(1f, 1.22f, Mathf.InverseLerp(1f, 1.55f, intensity));
                slot.go.transform.localScale = Vector3.one * rig.Cell * size;
                slot.age = 0f;
                slot.life = blast ? s.blastImpactLifetime : s.matchImpactLifetime;
                slot.active = true;
                slot.go.SetActive(true);
                foreach (var ps in slot.systems)
                {
                    ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ps.randomSeed = ++seed;
                    ps.Play(false);
                }
                ActiveCount++;
                if (blast) PlayedBlast++; else PlayedMatch++;
                return true;
            }
            // Every instance is busy: the visible wave has priority. Report handled so no second effect stacks.
            return slots.Length > 0;
        }

        public void Tick(float dt)
        {
            foreach (var slot in slots)
            {
                if (!slot.active) continue;
                slot.age += dt;
                if (slot.age >= slot.life) Release(slot);
            }
        }

        public void SetPaused(bool paused)
        {
            foreach (var slot in slots)
            {
                if (!slot.active) continue;
                foreach (var ps in slot.systems)
                {
                    if (paused && !ps.isPaused) ps.Pause(false);
                    else if (!paused && ps.isPaused) ps.Play(false);
                }
            }
        }

        void Release(Slot slot)
        {
            foreach (var ps in slot.systems) ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            slot.go.SetActive(false);
            slot.active = false;
            ActiveCount--;
        }

        public void Clear()
        {
            foreach (var slot in slots) if (slot.active) Release(slot);
        }

        public void Dispose() { }
    }
}
