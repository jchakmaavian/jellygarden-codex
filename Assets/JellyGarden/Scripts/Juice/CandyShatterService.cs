using System.Collections.Generic;
using UnityEngine;

namespace JellyGarden.Scripts.Juice
{
    /// <summary>
    /// Breaks a destroyed candy into chunky mesh fragments that burst outward, tumble,
    /// fall under gravity and fade out.
    ///
    /// How the fragments keep the candy's own look:
    ///  - A small library of shatter patterns is generated ONCE at startup. Each pattern
    ///    carves a unit circle into 5-7 irregular wedges. Together the wedges of one
    ///    pattern reproduce the candy's silhouette exactly, so at the moment of the break
    ///    the fragments sit precisely where the intact sprite was.
    ///  - Every wedge's UVs are the candy's own sprite UVs, so each chunk shows the real
    ///    painted colour, gloss highlight and shading from that part of the candy. Nothing
    ///    is recoloured or substituted, and the same code works for every candy type -
    ///    it reads whatever sprite the SpriteRenderer is currently showing.
    ///
    /// Everything is pooled and driven from one Update loop. No Rigidbody, no colliders,
    /// no coroutines per fragment, and no allocation on the destruction path.
    /// Fragments are purely visual: they carry no collider, so they cannot block input,
    /// participate in matching, or affect the grid.
    /// </summary>
    public class CandyShatterService : MonoBehaviour
    {
        private static CandyShatterService _instance;
        private static bool _quitting;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _quitting = false;
            _instance = null;
        }

        public static CandyShatterService Instance
        {
            get
            {
                if (_quitting) return null;
                if (_instance == null)
                {
                    var go = new GameObject("~CandyShatterService");
                    _instance = go.AddComponent<CandyShatterService>();
                    DontDestroyOnLoad(go);
                    _instance.Build();
                }
                return _instance;
            }
        }

        /// <summary>Built during scene load so the first match never hitches.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Prewarm()
        {
            var warm = Instance;
            if (warm != null) warm.enabled = true;
        }

        private void OnApplicationQuit() { _quitting = true; }

        // ------------------------------------------------------------------
        private const int PatternCount = 4;   // shatter layouts, cycled so repeats are not obvious
        private const int MaxWedges = 7;

        private class Fragment
        {
            public Transform Tr;
            public MeshFilter Filter;
            public MeshRenderer Renderer;
            public MaterialPropertyBlock Props;
            public bool Active;
            public float Age;
            public float Life;
            public Vector3 Velocity;
            public float SpinSpeed;
            public float Spin;
            public float BaseScale;
            public Color Tint;
        }

        private Fragment[] _pool;
        private int _cursor;

        // One pattern = one set of wedge meshes that together tile the candy silhouette.
        private Mesh[][] _patterns;
        private Vector2[][] _patternCentroids;

        private Material _baseMaterial;
        private readonly Dictionary<Texture, Material> _materialsByTexture = new Dictionary<Texture, Material>(8);

        private int _spawnCounter;

        private JuiceConfig Cfg { get { return JuiceConfig.Instance; } }

        // ------------------------------------------------------------------
        private void Build()
        {
            var cfg = Cfg;

            // Load through a Resources material, not Shader.Find: a shader referenced by
            // no asset is stripped from player builds.
            Material loaded = Resources.Load<Material>("CandyFragment");
            if (loaded != null && loaded.shader != null)
            {
                _baseMaterial = loaded;
            }
            else
            {
                var shader = Shader.Find("JellyGarden/CandyFragment") ?? Shader.Find("Sprites/Default");
                _baseMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                Debug.LogWarning("[CandyShatter] Resources/CandyFragment.mat missing; fell back to Shader.Find. " +
                                 "This will not survive shader stripping in a player build.");
            }

            BuildPatterns();

            int poolSize = Mathf.Clamp(cfg.ShatterPoolSize, 16, 256);
            _pool = new Fragment[poolSize];
            for (int i = 0; i < poolSize; i++)
            {
                var go = new GameObject("Frag" + i);
                go.transform.SetParent(transform, false);

                var mf = go.AddComponent<MeshFilter>();
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = _baseMaterial;
                mr.sortingLayerName = cfg.VfxSortingLayer;
                mr.sortingOrder = cfg.ShatterSortingOrder;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
                mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                mr.enabled = false;

                _pool[i] = new Fragment
                {
                    Tr = go.transform,
                    Filter = mf,
                    Renderer = mr,
                    Props = new MaterialPropertyBlock()
                };
            }
        }

        // ------------------------------------------------------------------
        /// <summary>
        /// Carves a unit circle (radius 0.5, matching a sprite's local bounds) into
        /// irregular wedges. Deliberately NOT a clean radial pie: the centre is offset and
        /// every rim point is jittered, so chunks read as broken candy rather than slices.
        /// </summary>
        private void BuildPatterns()
        {
            _patterns = new Mesh[PatternCount][];
            _patternCentroids = new Vector2[PatternCount][];

            for (int p = 0; p < PatternCount; p++)
            {
                int wedges = 4 + (p % 3);   // 4,5,6,4 ring chunks; +1 core = 5-7 fragments per candy
                var meshes = new Mesh[wedges];
                var centroids = new Vector2[wedges];

                // Break centre, pushed off-axis so the wedges have unequal mass.
                Vector2 core = new Vector2(
                    (Hash(p, 91) - 0.5f) * 0.18f,
                    (Hash(p, 137) - 0.5f) * 0.18f);

                // Uneven angular boundaries.
                float[] bounds = new float[wedges + 1];
                float acc = Hash(p, 11) * Mathf.PI * 2f;
                for (int w = 0; w < wedges; w++)
                {
                    bounds[w] = acc;
                    float share = (Mathf.PI * 2f / wedges) * Mathf.Lerp(0.72f, 1.28f, Hash(p * 31 + w, 53));
                    acc += share;
                }
                bounds[wedges] = bounds[0] + Mathf.PI * 2f;
                // Normalise so the wedges exactly close the circle.
                float span = acc - bounds[0];
                float correction = (Mathf.PI * 2f) / span;
                for (int w = 1; w <= wedges; w++)
                    bounds[w] = bounds[0] + (bounds[w] - bounds[0]) * correction;

                // Outer chunks are a RING band, not pie slices. A slice that converges to a
                // point reads as a thin sliver; a band between an inner and an outer arc
                // reads as a chunky broken piece, which is what we want.
                float innerBase = Mathf.Lerp(0.17f, 0.24f, Hash(p, 211));

                for (int w = 0; w < wedges; w++)
                {
                    float a0 = bounds[w];
                    float a1 = bounds[w + 1];

                    const int rimSteps = 3;                 // 4 samples along each arc
                    int ring = rimSteps + 1;
                    var verts = new Vector3[ring * 2];
                    var uvs = new Vector2[ring * 2];

                    for (int r = 0; r <= rimSteps; r++)
                    {
                        float t = r / (float)rimSteps;
                        float ang = Mathf.Lerp(a0, a1, t);
                        float cosA = Mathf.Cos(ang), sinA = Mathf.Sin(ang);

                        float rOuter = 0.5f * Mathf.Lerp(0.88f, 1.0f, Hash(p * 977 + w * 61 + r, 17));
                        float rInner = innerBase * Mathf.Lerp(0.75f, 1.25f, Hash(p * 613 + w * 47 + r, 29));

                        verts[r] = new Vector3(core.x + cosA * rInner, core.y + sinA * rInner, 0f);
                        verts[ring + r] = new Vector3(cosA * rOuter, sinA * rOuter, 0f);
                    }

                    var tris = new int[rimSteps * 6];
                    for (int r = 0; r < rimSteps; r++)
                    {
                        int i0 = r, i1 = r + 1, o0 = ring + r, o1 = ring + r + 1;
                        tris[r * 6 + 0] = i0; tris[r * 6 + 1] = o0; tris[r * 6 + 2] = o1;
                        tris[r * 6 + 3] = i0; tris[r * 6 + 4] = o1; tris[r * 6 + 5] = i1;
                    }

                    meshes[w] = BuildMesh("CandyFrag_p" + p + "_w" + w, verts, uvs, tris, out centroids[w]);
                }

                // One more chunk for the middle, so the candy's centre - usually where the
                // gloss highlight sits - breaks off as its own recognisable piece.
                {
                    const int coreSteps = 7;
                    var cv = new Vector3[coreSteps];
                    var cu = new Vector2[coreSteps];
                    for (int r = 0; r < coreSteps; r++)
                    {
                        float ang = (r / (float)coreSteps) * Mathf.PI * 2f + bounds[0];
                        float rad = innerBase * Mathf.Lerp(0.95f, 1.35f, Hash(p * 331 + r, 37));
                        cv[r] = new Vector3(core.x + Mathf.Cos(ang) * rad, core.y + Mathf.Sin(ang) * rad, 0f);
                    }
                    var ct = new int[(coreSteps - 2) * 3];
                    for (int r = 0; r < coreSteps - 2; r++)
                    {
                        ct[r * 3 + 0] = 0; ct[r * 3 + 1] = r + 1; ct[r * 3 + 2] = r + 2;
                    }
                    Vector2 cc;
                    var coreMesh = BuildMesh("CandyFrag_p" + p + "_core", cv, cu, ct, out cc);

                    var m2 = new Mesh[wedges + 1];
                    var c2 = new Vector2[wedges + 1];
                    for (int w = 0; w < wedges; w++) { m2[w] = meshes[w]; c2[w] = centroids[w]; }
                    m2[wedges] = coreMesh; c2[wedges] = cc;
                    meshes = m2; centroids = c2;
                }

                _patterns[p] = meshes;
                _patternCentroids[p] = centroids;
            }
        }

        /// <summary>
        /// Finishes a fragment mesh: assigns each vertex the UV of its position inside the
        /// sprite (so the chunk shows exactly the part of the candy it was cut from), then
        /// re-centres the geometry on its own centroid so it tumbles about itself rather
        /// than orbiting the candy's centre.
        /// </summary>
        private static Mesh BuildMesh(string name, Vector3[] verts, Vector2[] uvs, int[] tris, out Vector2 centroid)
        {
            Vector3 c = Vector3.zero;
            for (int v = 0; v < verts.Length; v++) c += verts[v];
            c /= verts.Length;

            for (int v = 0; v < verts.Length; v++)
            {
                uvs[v] = new Vector2(verts[v].x + 0.5f, verts[v].y + 0.5f);
                verts[v] -= c;
            }

            var mesh = new Mesh { name = name, hideFlags = HideFlags.HideAndDontSave };
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateBounds();

            centroid = new Vector2(c.x, c.y);
            return mesh;
        }

        /// <summary>Deterministic 0-1 hash. Keeps the randomness bounded and repeatable.</summary>
        private static float Hash(int a, int b)
        {
            int h = a * 73856093 ^ b * 19349663;
            h = (h ^ (h >> 13)) * 1274126177;
            return ((h ^ (h >> 16)) & 0x7fffff) / (float)0x7fffff;
        }

        // ------------------------------------------------------------------
        private Material MaterialFor(Texture tex)
        {
            if (tex == null) return _baseMaterial;
            Material m;
            if (_materialsByTexture.TryGetValue(tex, out m) && m != null) return m;
            m = new Material(_baseMaterial) { hideFlags = HideFlags.HideAndDontSave };
            m.mainTexture = tex;
            _materialsByTexture[tex] = m;
            return m;
        }

        // ------------------------------------------------------------------
        /// <summary>
        /// Breaks the candy currently shown by <paramref name="source"/> into fragments.
        /// Reads the sprite off the renderer, so it works for every candy colour and for
        /// striped / wrapped variants without any per-candy configuration.
        /// </summary>
        public void Shatter(SpriteRenderer source, float intensity,bool powerClear=false)
        {
            var cfg = Cfg;
            if (!cfg.EnableJuice || !cfg.EnableCandyShatter) return;
            if (source == null || source.sprite == null) return;

            var sprite = source.sprite;
            var tex = sprite.texture;
            if (tex == null) return;

            int pattern = _spawnCounter % PatternCount;
            _spawnCounter++;

            Mesh[] meshes = _patterns[pattern];
            Vector2[] centroids = _patternCentroids[pattern];
            int wedges = Mathf.Min(meshes.Length, powerClear?2:MaxWedges);

            // Bail out rather than allocate if the pool is nearly spent. The sparkle VFX
            // still plays, so a huge clear degrades gracefully instead of stuttering.
            if (FreeCount() < wedges) return;

            // World footprint of the intact candy, so fragments start exactly on top of it.
            Bounds b = source.bounds;
            Vector3 centre = b.center;
            centre.z -= 0.45f;                       // sit in front of the board pieces
            float worldSize = Mathf.Max(b.size.x, b.size.y);

            // Map the mesh's 0-1 UVs onto this sprite's region of its texture (atlas-safe).
            Rect tr = sprite.textureRect;
            Vector4 st = new Vector4(tr.width / tex.width, tr.height / tex.height,
                                     tr.x / tex.width, tr.y / tex.height);

            int seed = _spawnCounter * 397;

            for (int w = 0; w < wedges; w++)
            {
                var f = Take();
                if (f == null) return;

                f.Filter.sharedMesh = meshes[w];
                f.Renderer.sharedMaterial = MaterialFor(tex);

                Vector2 local = centroids[w];
                f.Tr.position = centre + new Vector3(local.x * worldSize, local.y * worldSize, 0f);
                f.Tr.rotation = Quaternion.identity;
                f.BaseScale = worldSize;
                f.Tr.localScale = new Vector3(worldSize, worldSize, 1f);

                // --- launch -------------------------------------------------
                // Outward from the break centre, then deliberately skewed so the burst is
                // never a clean radial star: a jittered angle plus an upward bias gives the
                // mix of sideways, diagonal and slightly-upward chunks.
                Vector2 outward = local.sqrMagnitude > 0.0001f ? local.normalized : new Vector2(0f, 1f);
                float skew = (Hash(seed + w, 7) - 0.5f) * cfg.ShatterDirectionJitter;
                float cos = Mathf.Cos(skew), sin = Mathf.Sin(skew);
                Vector2 dir = new Vector2(outward.x * cos - outward.y * sin,
                                          outward.x * sin + outward.y * cos);

                float speed = cfg.ShatterBurstSpeed * worldSize *
                              Mathf.Lerp(0.62f, 1.38f, Hash(seed + w, 23)) * intensity;

                f.Velocity = new Vector3(dir.x * speed,
                                         dir.y * speed + cfg.ShatterUpwardBias * worldSize,
                                         0f);

                f.Spin = Hash(seed + w, 41) * 360f;
                f.SpinSpeed = Mathf.Lerp(cfg.ShatterSpinMin, cfg.ShatterSpinMax, Hash(seed + w, 59))
                              * (Hash(seed + w, 71) < 0.5f ? -1f : 1f);

                f.Life = cfg.ShatterLifetime * (powerClear?.65f:1) * Mathf.Lerp(0.78f, 1.22f, Hash(seed + w, 83));
                f.Age = 0f;

                // Slight per-chunk size variation so they do not read as a uniform set.
                f.BaseScale = worldSize * Mathf.Lerp(0.92f, 1.10f, Hash(seed + w, 101));

                f.Tint = source.color;
                f.Tint.a = 1f;

                f.Props.Clear();
                f.Props.SetVector("_MainTex_ST", st);
                f.Props.SetColor("_Color", f.Tint);
                f.Renderer.SetPropertyBlock(f.Props);

                f.Active = true;
                f.Renderer.enabled = true;
            }
        }

        // ------------------------------------------------------------------
        private int FreeCount()
        {
            int n = 0;
            for (int i = 0; i < _pool.Length; i++) if (!_pool[i].Active) n++;
            return n;
        }

        private Fragment Take()
        {
            for (int i = 0; i < _pool.Length; i++)
            {
                int idx = (_cursor + i) % _pool.Length;
                if (!_pool[idx].Active)
                {
                    _cursor = (idx + 1) % _pool.Length;
                    return _pool[idx];
                }
            }
            return null;
        }

        // ------------------------------------------------------------------
        private void Update()
        {
            if (_pool == null) return;

            var cfg = Cfg;
            float dt = Time.deltaTime;
            float gravity = cfg.ShatterGravity;
            float drag = Mathf.Clamp01(cfg.ShatterDrag * dt);

            for (int i = 0; i < _pool.Length; i++)
            {
                var f = _pool[i];
                if (!f.Active) continue;

                f.Age += dt;
                float k = f.Life > 0f ? f.Age / f.Life : 1f;

                if (k >= 1f)
                {
                    f.Active = false;
                    f.Renderer.enabled = false;
                    continue;
                }

                // position += velocity * dt ; velocity.y += gravity * dt
                f.Velocity.y += gravity * dt;
                f.Velocity -= f.Velocity * drag;          // energy loss, so the burst settles
                f.Tr.position += f.Velocity * dt;

                f.Spin += f.SpinSpeed * dt;
                f.Tr.rotation = Quaternion.Euler(0f, 0f, f.Spin);

                // Fade and shrink only over the tail, so the chunk stays solid and readable
                // while it is doing the interesting part of the motion.
                float tail = cfg.ShatterFadeStart;
                float fade = k <= tail ? 1f : 1f - Mathf.Clamp01((k - tail) / (1f - tail));

                float s = f.BaseScale * Mathf.Lerp(1f, cfg.ShatterEndScale, 1f - fade);
                f.Tr.localScale = new Vector3(s, s, 1f);

                var c = f.Tint;
                c.a = fade;
                f.Props.SetColor("_Color", c);
                f.Renderer.SetPropertyBlock(f.Props);
            }
        }
    }
}
