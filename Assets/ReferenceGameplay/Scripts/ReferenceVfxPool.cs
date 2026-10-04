using System.Collections.Generic;
using JellyGarden.Scripts;
using UnityEngine;
using UnityEngine.Rendering;

namespace RoyalThrone.ReferenceGameplay
{
    // Native particle modules stay on their prefabs. This adapter owns leases only;
    // it never chooses targets or sends gameplay hit/conversion callbacks.
    public sealed class ReferenceVfxPool : MonoBehaviour
    {
        sealed class Entry
        {
            public string key; public GameObject root; public ParticleSystem[] particles; public bool[] pausedByOwner;
            public bool active, sustained, stopped; public int lease; public float age, emissionEnd;
        }
        public sealed class Handle
        {
            Entry entry; int lease;
            internal Handle(object value) { entry = (Entry)value; lease = entry.lease; }
            public bool Valid => entry != null && entry.active && entry.lease == lease && entry.root;
            public GameObject GameObject => Valid ? entry.root : null;
            public void Stop() { if (Valid) StopEmission(entry); }
        }
        static ReferenceVfxPool instance;
        static readonly Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();
        readonly List<Entry> pool = new List<Entry>();
        static readonly string[] colorNames = { "red", "blue", "yellow", "green", "pink" };
        const int Capacity = 96;
        public static double LastInstantiationMilliseconds { get; private set; }
        public static int Active { get { int n=0; if(instance) foreach(var e in instance.pool) if(e.active)n++; return n; } }
        public static int ParticleCount { get { int n=0; if(instance)foreach(var e in instance.pool)if(e.active)foreach(var ps in e.particles)if(ps)n+=ps.particleCount;return n; } }
        public static bool Has(string key)
        {
            if (!prefabs.TryGetValue(key, out var prefab)) { prefab = Resources.Load<GameObject>("ReferenceGameplay/VFX/" + key); prefabs[key] = prefab; }
            return prefab;
        }
        public static Handle Play(string key, Vector3 position, int color = -1, float worldCell = 0, bool sustained = false)
        {
            if (!Application.isPlaying || !Has(key)) return null;
            if (!instance) { var go = new GameObject("Imported gameplay VFX pool"); instance = go.AddComponent<ReferenceVfxPool>(); DontDestroyOnLoad(go); }
            Entry selected = null;
            foreach (var e in instance.pool) if (!e.active && e.key == key && e.root) { selected = e; break; }
            if (selected == null)
            {
                if (instance.pool.Count >= Capacity) return null;
                var buildClock = System.Diagnostics.Stopwatch.StartNew();
                var root = Instantiate(prefabs[key], instance.transform);
                selected = new Entry { key = key, root = root, particles = root.GetComponentsInChildren<ParticleSystem>(true) };
                selected.pausedByOwner = new bool[selected.particles.Length];
                instance.pool.Add(selected);
                LastInstantiationMilliseconds = buildClock.Elapsed.TotalMilliseconds;
            }
            var item = selected; item.lease++; item.active = true; item.sustained = sustained; item.age = 0; item.stopped = false; item.emissionEnd = 0;
            System.Array.Clear(item.pausedByOwner, 0, item.pausedByOwner.Length);
            var lm = LevelManager.THIS;
            if (worldCell <= 0) worldCell = lm && lm.GameField ? lm.squareWidth * Mathf.Abs(lm.GameField.lossyScale.x) : 1;
            item.root.transform.SetParent(instance.transform, false);
            item.root.transform.SetPositionAndRotation(position, Quaternion.identity);
            item.root.transform.localScale = prefabs[key].transform.localScale * worldCell;
            item.root.SetActive(true);
            var sorting = item.root.GetComponent<SortingGroup>(); if (!sorting) sorting = item.root.AddComponent<SortingGroup>();
            sorting.sortingLayerID = 0; sorting.sortingOrder = 35;
            foreach (var ps in item.particles)
            {
                ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                if (ps.name.StartsWith("blast_part_")) ps.gameObject.SetActive(color >= 0 && color < colorNames.Length && ps.name == "blast_part_" + colorNames[color]);
                if (!ps.gameObject.activeInHierarchy) continue;
                // The packaged Potion template has an explicit null Sprite awaiting
                // native runtime variant assignment. Keep its recovered child effects,
                // but do not emit an invented/default billboard for that template.
                if (key == "PotionItemSingleExplodeParticles" && ps.name == "Potion" && ps.transform.parent == item.root.transform) continue;
                var main = ps.main;
                item.emissionEnd = Mathf.Max(item.emissionEnd, main.duration + main.startDelay.constantMax);
                ps.Play(false);
            }
            return new Handle(item);
        }
        /// <summary>
        /// Loads an effect and parks up to <paramref name="count"/> idle copies in the pool, with their materials, textures and
        /// meshes touched, so the first Rocket, match or blast of a session does not load or instantiate in the middle of play.
        /// Builds at most one copy per call; returns true while more are wanted.
        /// </summary>
        public static bool Prewarm(string key, int count)
        {
            if (!Application.isPlaying || !Has(key)) return false;
            if (!instance) { var go = new GameObject("Imported gameplay VFX pool"); instance = go.AddComponent<ReferenceVfxPool>(); DontDestroyOnLoad(go); }
            int have = 0; foreach (var e in instance.pool) if (e.key == key && e.root) have++;
            if (have >= count || instance.pool.Count >= Capacity) return false;
            var root = Instantiate(prefabs[key], instance.transform);
            var entry = new Entry { key = key, root = root, particles = root.GetComponentsInChildren<ParticleSystem>(true) };
            entry.pausedByOwner = new bool[entry.particles.Length];
            foreach (var ps in entry.particles) ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            foreach (var renderer in root.GetComponentsInChildren<ParticleSystemRenderer>(true))
            {
                foreach (var material in renderer.sharedMaterials) Touch(material);
                Touch(renderer.trailMaterial);
                if (renderer.mesh) _ = renderer.mesh.vertexCount;
            }
            foreach (var ps in entry.particles)
            {
                var sheet = ps.textureSheetAnimation;
                if (sheet.enabled && sheet.mode == ParticleSystemAnimationMode.Sprites)
                    for (int i = 0; i < sheet.spriteCount; i++) { var sprite = sheet.GetSprite(i); if (sprite) Touch(sprite.texture); }
            }
            root.SetActive(false);
            instance.pool.Add(entry);
            return have + 1 < count;
        }
        static void Touch(Material material)
        {
            if (!material) return;
            foreach (int id in material.GetTexturePropertyNameIDs()) Touch(material.GetTexture(id));
        }
        // Reading a property makes the editor load an asset it would otherwise load on the effect's first render.
        static void Touch(Texture texture) { if (texture) _ = texture.width; }
        static void StopEmission(Entry item)
        {
            item.stopped = true; item.sustained = false;
            System.Array.Clear(item.pausedByOwner, 0, item.pausedByOwner.Length);
            foreach (var ps in item.particles) if (ps) ps.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }
        void Update()
        {
            var lm=LevelManager.THIS;
            if (lm && (lm.gameStatus == GameState.Map || lm.gameStatus == GameState.PrepareGame)) { Clear(); return; }
            bool paused = lm && lm.gameStatus == GameState.Pause;
            foreach (var item in pool)
            {
                if (!item.active) continue;
                if (!item.root) { item.active = false; item.sustained = false; continue; }
                for (int i = 0; i < item.particles.Length; i++)
                {
                    var ps = item.particles[i]; if (!ps || !ps.gameObject.activeInHierarchy) continue;
                    if (paused && ps.isPlaying) { ps.Pause(false); item.pausedByOwner[i] = true; }
                    else if (!paused && item.pausedByOwner[i])
                    { if (ps.isPaused) ps.Play(false); item.pausedByOwner[i] = false; }
                }
                if (paused) continue;
                item.age += Time.deltaTime;
                if (!item.sustained && !item.stopped && item.age >= item.emissionEnd) StopEmission(item);
                bool alive=false;foreach(var ps in item.particles)if(ps&&ps.gameObject.activeInHierarchy&&ps.IsAlive(false)){alive=true;break;}
                if (!item.sustained && !alive && item.age > .05f) Retire(item);
            }
        }
        static void Retire(Entry item)
        {
            foreach (var ps in item.particles) if (ps) ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (item.root) item.root.SetActive(false); item.active=false; item.sustained=false;
        }
        public static void Clear() { if(instance)foreach(var e in instance.pool)if(e.active)Retire(e); }
        void OnDestroy() { if(instance==this)instance=null; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { instance=null;prefabs.Clear(); }
    }
}
