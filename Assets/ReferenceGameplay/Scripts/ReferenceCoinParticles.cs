using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalThrone.ReferenceGameplay
{
    /// <summary>
    /// Presents the recovered coin ParticleSystems on an overlay Canvas. Simulation stays in
    /// native cell units; baked particle vertices are mapped to UI units, including travelled
    /// distance, sprite UVs, native size/colour curves and each renderer's sorting order.
    /// This adapter never changes rewards or sends gameplay callbacks.
    /// </summary>
    public sealed class ReferenceCoinParticles : MonoBehaviour
    {
        const int Capacity = 48;
        const float MaxLeaseSeconds = 12;
        sealed class Entry
        {
            public string key;
            public GameObject simulation;
            public RectTransform presentation;
            public ParticleSystem[] systems;
            public ReferenceCoinParticleGraphic[] graphics;
            public bool active, stopped, sustained;
            public int lease;
            public float units, age, emissionEnd;
        }

        public sealed class Handle
        {
            readonly Entry entry;
            readonly int lease;
            internal Handle(object value) { entry = (Entry)value; lease = entry.lease; }
            public bool Active => entry != null && entry.active && entry.lease == lease && entry.simulation && entry.presentation;
            public void SetPosition(Vector2 position)
            {
                if (Active) entry.simulation.transform.localPosition = new Vector3(position.x / entry.units, position.y / entry.units, 0);
            }
            /// <summary>End emission and allow particles already alive to fade.</summary>
            public void Stop() { if (Active) StopEmission(entry); }
            /// <summary>Cancel this lease immediately, including its remaining particles.</summary>
            public void Clear() { if (Active) Retire(entry); }
        }

        static readonly List<ReferenceCoinParticles> owners = new List<ReferenceCoinParticles>();
        static readonly Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();
        readonly List<Entry> pool = new List<Entry>();
        RectTransform canvasRoot;
        bool behindCoins;
        GameObject simulationHost;
        Camera bakeCamera;
        Material additive;

        public static int ActiveCount
        {
            get { int n = 0; foreach (var owner in owners) if (owner) foreach (var e in owner.pool) if (e.active) n++; return n; }
        }
        public static int ParticleCount
        {
            get { int n = 0; foreach (var owner in owners) if (owner) foreach (var e in owner.pool) if (e.active) foreach (var ps in e.systems) if (ps) n += ps.particleCount; return n; }
        }
        public static int BakedVertexCount
        {
            get { int n = 0; foreach (var owner in owners) if (owner) foreach (var e in owner.pool) if (e.active) foreach (var graphic in e.graphics) if (graphic) n += graphic.BakedVertexCount; return n; }
        }

        /// <param name="position">Position in canvasRoot local coordinates.</param>
        /// <param name="cellUnits">Canvas units per native particle unit (normally the coin/board cell pitch).</param>
        public static Handle Play(string key, RectTransform canvasRoot, Vector2 position, float cellUnits)
        {
            if (!Application.isPlaying || !canvasRoot || !canvasRoot.gameObject.activeInHierarchy || cellUnits <= 0) return null;
            if (!prefabs.TryGetValue(key, out var prefab) || !prefab)
            {
                prefab = Resources.Load<GameObject>("ReferenceGameplay/VFX/" + key);
                if (!prefab) return null;
                prefabs[key] = prefab;
            }
            ReferenceCoinParticles owner = null;
            bool background = key == "RemainingMovesCoinTracer";
            for (int i = owners.Count - 1; i >= 0; i--)
            {
                if (!owners[i]) owners.RemoveAt(i);
                else if (owners[i].canvasRoot == canvasRoot && owners[i].behindCoins == background) owner = owners[i];
            }
            if (!owner)
            {
                var host = FullRect(background ? "Native coin trails" : "Native coin impacts", canvasRoot);
                owner = host.gameObject.AddComponent<ReferenceCoinParticles>();
                owner.Initialize(canvasRoot, background);
                owners.Add(owner);
            }
            return owner.Acquire(key, prefab, position, cellUnits);
        }

        static RectTransform FullRect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(.5f, .5f); rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }
        void Initialize(RectTransform target, bool background)
        {
            canvasRoot = target; behindCoins = background;
            // Independent of CanvasScaler: one simulation world unit always means one native cell.
            simulationHost = new GameObject("Coin particle simulation") { hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave };
            var cameraHost = new GameObject("Coin particle bake camera") { hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave };
            cameraHost.transform.SetParent(simulationHost.transform, false);
            cameraHost.transform.localPosition = new Vector3(0, 0, -100);
            bakeCamera = cameraHost.AddComponent<Camera>();
            bakeCamera.enabled = false; bakeCamera.orthographic = true; bakeCamera.cullingMask = 0;
            bakeCamera.nearClipPlane = .01f; bakeCamera.farClipPlane = 200;
            var shader = Resources.Load<Shader>("RoyalGameplay/UIAdditive");
            if (shader) additive = new Material(shader) { name = "Coin native additive UI", hideFlags = HideFlags.DontSave };
        }
        Handle Acquire(string key, GameObject prefab, Vector2 position, float units)
        {
            // Coin Images are also pooled and are moved to the end when reused. Reassert the
            // explicit native effect layer on every lease, independent of creation/reuse order.
            if (behindCoins) transform.SetAsFirstSibling();
            else transform.SetAsLastSibling();
            Entry entry = null;
            foreach (var e in pool) if (!e.active && e.key == key && e.simulation && e.presentation) { entry = e; break; }
            if (entry == null)
            {
                if (pool.Count >= Capacity)
                {
                    // A previous effect type must not occupy the whole pool after it has finished.
                    Entry idle = null;
                    foreach (var e in pool) if (!e.active) { idle = e; break; }
                    if (idle == null) return null;
                    if (idle.simulation) Destroy(idle.simulation);
                    if (idle.presentation) Destroy(idle.presentation.gameObject);
                    pool.Remove(idle);
                }
                var simulation = Instantiate(prefab, simulationHost.transform, false);
                simulation.SetActive(false);
                entry = new Entry { key = key, simulation = simulation, presentation = FullRect(key, transform) };
                entry.systems = simulation.GetComponentsInChildren<ParticleSystem>(true);
                var graphics = new List<ReferenceCoinParticleGraphic>();
                var renderers = simulation.GetComponentsInChildren<ParticleSystemRenderer>(true);
                System.Array.Sort(renderers, (a, b) => a.sortingOrder.CompareTo(b.sortingOrder));
                foreach (var renderer in renderers)
                {
                    // BakeMesh reads the renderer while normal world cameras must not draw it.
                    renderer.forceRenderingOff = true;
                    var ps = renderer.GetComponent<ParticleSystem>();
                    if (!ps || !renderer.enabled) continue;
                    var rect = FullRect(renderer.name, entry.presentation);
                    rect.gameObject.AddComponent<CanvasRenderer>();
                    var graphic = rect.gameObject.AddComponent<ReferenceCoinParticleGraphic>();
                    var source = renderer.sharedMaterial;
                    bool isAdditive = source && source.shader && source.shader.name.Contains("Additive");
                    if (isAdditive && !additive)
                    {
                        Debug.LogError("Coin particles require RoyalGameplay/UIAdditive.shader.");
                        Destroy(rect.gameObject);
                        continue;
                    }
                    Texture texture = source ? source.mainTexture : null;
                    var sheet = ps.textureSheetAnimation;
                    if (sheet.enabled && sheet.mode == ParticleSystemAnimationMode.Sprites)
                        for (int s = 0; s < sheet.spriteCount; s++)
                            if (sheet.GetSprite(s)) { texture = sheet.GetSprite(s).texture; break; }
                    graphic.Initialize(renderer, texture, isAdditive ? additive : null);
                    if (source && source.HasProperty("_Color")) graphic.color = source.GetColor("_Color");
                    graphics.Add(graphic);
                }
                entry.graphics = graphics.ToArray();
                pool.Add(entry);
            }
            entry.lease++; entry.active = true; entry.stopped = false; entry.sustained = false;
            entry.units = Mathf.Max(.001f, units); entry.age = 0; entry.emissionEnd = 0;
            entry.presentation.SetAsLastSibling(); entry.presentation.gameObject.SetActive(true);
            entry.simulation.transform.localPosition = new Vector3(position.x / entry.units, position.y / entry.units, 0);
            // Both native CoinHitParticles templates serialize zero scale; their original runtime
            // supplies a scale when playing. The caller supplies that size through cellUnits here.
            entry.simulation.transform.localScale = prefab.transform.localScale == Vector3.zero ? Vector3.one : prefab.transform.localScale;
            entry.simulation.SetActive(true);
            foreach (var ps in entry.systems)
            {
                ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = ps.main;
                main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
                main.useUnscaledTime = true; main.stopAction = ParticleSystemStopAction.None;
                if (!ps.gameObject.activeInHierarchy || !ps.emission.enabled) continue;
                entry.sustained |= main.loop;
                entry.emissionEnd = Mathf.Max(entry.emissionEnd, main.duration + main.startDelay.constantMax);
                ps.Play(false);
            }
            return new Handle(entry);
        }
        static void StopEmission(Entry entry)
        {
            entry.stopped = true; entry.sustained = false;
            foreach (var ps in entry.systems) if (ps) ps.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }
        static void Retire(Entry entry)
        {
            foreach (var ps in entry.systems) if (ps) ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            foreach (var graphic in entry.graphics) if (graphic) graphic.ClearMesh();
            if (entry.simulation) entry.simulation.SetActive(false);
            if (entry.presentation) entry.presentation.gameObject.SetActive(false);
            entry.active = false; entry.stopped = true; entry.sustained = false;
        }
        void LateUpdate()
        {
            if (!canvasRoot) { Destroy(gameObject); return; }
            // The whole presentation hierarchy has the target's local origin, including noncentral pivots.
            var origin = canvasRoot.rect.center;
            foreach (var entry in pool)
            {
                if (!entry.active) continue;
                entry.age += Time.unscaledDeltaTime;
                if (entry.age >= MaxLeaseSeconds || !entry.simulation || !entry.presentation) { Retire(entry); continue; }
                if (!entry.sustained && !entry.stopped && entry.age >= entry.emissionEnd) StopEmission(entry);
                bool alive = false;
                foreach (var ps in entry.systems)
                    if (ps && ps.gameObject.activeInHierarchy && ps.emission.enabled && ps.IsAlive(false)) { alive = true; break; }
                if (!alive && entry.age > .05f) { Retire(entry); continue; }
                bakeCamera.orthographicSize = Mathf.Max(1, canvasRoot.rect.height / entry.units * .5f);
                bakeCamera.aspect = Mathf.Max(.01f, canvasRoot.rect.width / Mathf.Max(1, canvasRoot.rect.height));
                foreach (var graphic in entry.graphics) if (graphic) graphic.Bake(bakeCamera, entry.units, origin);
            }
        }
        /// <summary>Cancel all leases, or just those belonging to the specified canvas root.</summary>
        public static void Clear(RectTransform target = null)
        {
            foreach (var owner in owners)
                if (owner && (!target || owner.canvasRoot == target))
                    foreach (var entry in owner.pool) if (entry.active) Retire(entry);
        }
        void OnDisable() { foreach (var entry in pool) if (entry.active) Retire(entry); }
        void OnDestroy()
        {
            owners.Remove(this);
            if (simulationHost) Destroy(simulationHost);
            if (additive) Destroy(additive);
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { owners.Clear(); prefabs.Clear(); }
    }

    /// <summary>A native baked particle mesh rendered by the existing Canvas stencil/clip path.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ReferenceCoinParticleGraphic : MaskableGraphic
    {
        ParticleSystemRenderer source;
        Mesh baked;
        Texture atlas;
        float units;
        Vector2 origin;
        readonly List<Vector3> vertices = new List<Vector3>(256);
        readonly List<Color32> colors = new List<Color32>(256);
        readonly List<Vector4> uvs = new List<Vector4>(256);
        readonly List<int> indices = new List<int>(384);
        public override Texture mainTexture => atlas ? atlas : Texture2D.whiteTexture;
        public int BakedVertexCount => baked ? baked.vertexCount : 0;
        internal void Initialize(ParticleSystemRenderer renderer, Texture texture, Material uiMaterial)
        {
            source = renderer; atlas = texture; material = uiMaterial; raycastTarget = false;
            baked = new Mesh { name = "Native coin UI particles", hideFlags = HideFlags.DontSave };
            baked.MarkDynamic();
        }
        internal void Bake(Camera camera, float canvasUnits, Vector2 canvasOrigin)
        {
            units = canvasUnits; origin = canvasOrigin;
            if (!source || !source.gameObject.activeInHierarchy) { ClearMesh(); return; }
            source.BakeMesh(baked, camera, ParticleSystemBakeMeshOptions.BakeRotationAndScale | ParticleSystemBakeMeshOptions.BakePosition);
            SetVerticesDirty();
        }
        internal void ClearMesh() { if (baked) baked.Clear(); if (canvasRenderer) canvasRenderer.Clear(); SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (!baked || baked.vertexCount == 0 || baked.vertexCount >= 65000) return;
            baked.GetVertices(vertices); baked.GetColors(colors); baked.GetUVs(0, uvs); baked.GetTriangles(indices, 0);
            for (int i = 0; i < vertices.Count; i++)
            {
                var v = UIVertex.simpleVert;
                v.position = new Vector3(vertices[i].x * units - origin.x, vertices[i].y * units - origin.y, 0);
                v.color = (i < colors.Count ? (Color)colors[i] : Color.white) * color;
                v.uv0 = i < uvs.Count ? uvs[i] : Vector4.zero;
                vh.AddVert(v);
            }
            for (int i = 0; i + 2 < indices.Count; i += 3) vh.AddTriangle(indices[i], indices[i + 1], indices[i + 2]);
        }
        protected override void OnDestroy() { if (baked) Destroy(baked); base.OnDestroy(); }
    }
}
