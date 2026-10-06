using System.Collections.Generic;
using UnityEngine;

namespace RoyalThrone.Match3Vfx
{
    /// <summary>
    /// Breaks a piece into mesh chips that burst out, tumble and fall. Chips either come from a
    /// <see cref="Match3BurstLibrary"/> (painted pieces per colour) or are cut from the piece's own
    /// sprite with pre-generated wedge patterns that tile its silhouette, so every chip shows the
    /// real painted surface. One pool, one Tick, no Rigidbody, no colliders, no allocation per break.
    /// </summary>
    internal sealed class PieceShatterFx
    {
        public const int PatternCount = 4;
        public const int MaxWedges = 7;
        public const float MaxAuthoredChipCellFraction = 0.40f;

        sealed class Fragment
        {
            public Transform tr;
            public MeshFilter filter;
            public MeshRenderer renderer;
            public MaterialPropertyBlock props;
            public bool active, smooth;
            public float age, life, spin, spinSpeed, baseScale, exitY, gravity;
            public Vector3 velocity;
            public Color tint;
        }

        readonly Match3VfxRig rig;
        readonly Fragment[] pool;
        readonly Mesh[][] patterns = new Mesh[PatternCount][];
        readonly Vector2[][] centroids = new Vector2[PatternCount][];
        readonly List<Mesh> ownedMeshes = new List<Mesh>(64);
        readonly Dictionary<Texture, Material> materialsByTexture = new Dictionary<Texture, Material>(8);
        readonly Material baseMaterial;
        Match3BurstLibrary library;
        Mesh[][] burstMeshes;
        Vector2[][] burstOffsets;
        int cursor, spawnCounter;

        Match3VfxSettings S => rig.Settings;

        public int Capacity => pool.Length;
        public int ActiveFragments => pool.Length - FreeCount();
        public int PeakFragments { get; private set; }
        public int AuthoredColors => burstMeshes == null ? 0 : burstMeshes.Length;

        public PieceShatterFx(Match3VfxRig rig)
        {
            this.rig = rig;
            var root = new GameObject("Shatter debris").transform;
            root.SetParent(rig.transform, false);
            baseMaterial = Match3VfxMaterials.CreateFragment();
            BuildPatterns();
            BuildAuthoredBursts(S.burstLibrary);

            pool = new Fragment[Mathf.Clamp(S.shatterPoolSize, 16, 256)];
            for (int i = 0; i < pool.Length; i++)
            {
                var go = new GameObject("Chip " + i);
                go.transform.SetParent(root, false);
                var mf = go.AddComponent<MeshFilter>();
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = baseMaterial;
                mr.sortingLayerName = S.sortingLayerName;
                mr.sortingOrder = S.shatterSortingOrder;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
                mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                mr.enabled = false;
                pool[i] = new Fragment { tr = go.transform, filter = mf, renderer = mr, props = new MaterialPropertyBlock() };
            }
        }

        // ------------------------------------------------------------------ geometry

        void BuildAuthoredBursts(Match3BurstLibrary source)
        {
            library = source;
            if (!library || library.bursts == null) return;
            burstMeshes = new Mesh[library.bursts.Length][];
            burstOffsets = new Vector2[library.bursts.Length][];
            for (int color = 0; color < burstMeshes.Length; color++)
            {
                if (!library.Has(color)) { burstMeshes[color] = new Mesh[0]; burstOffsets[color] = new Vector2[0]; continue; }
                var pieces = library.bursts[color].pieces;
                burstMeshes[color] = new Mesh[pieces.Length];
                burstOffsets[color] = new Vector2[pieces.Length];
                for (int i = 0; i < pieces.Length; i++)
                {
                    var sprite = pieces[i].sprite;
                    if (!sprite) { burstMeshes[color] = new Mesh[0]; break; }
                    var rect = sprite.rect;
                    var tex = sprite.texture;
                    float x = rect.width / tex.width * 0.5f, y = rect.height / tex.height * 0.5f;
                    var mesh = new Mesh { name = sprite.name, hideFlags = HideFlags.HideAndDontSave };
                    mesh.vertices = new[] { new Vector3(-x, -y, 0), new Vector3(x, -y, 0), new Vector3(x, y, 0), new Vector3(-x, y, 0) };
                    mesh.uv = new[]
                    {
                        new Vector2(rect.xMin / tex.width, rect.yMin / tex.height), new Vector2(rect.xMax / tex.width, rect.yMin / tex.height),
                        new Vector2(rect.xMax / tex.width, rect.yMax / tex.height), new Vector2(rect.xMin / tex.width, rect.yMax / tex.height)
                    };
                    mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
                    mesh.RecalculateBounds();
                    ownedMeshes.Add(mesh);
                    burstMeshes[color][i] = mesh;
                    burstOffsets[color][i] = pieces[i].offset;
                }
            }
        }

        /// <summary>
        /// Carves a unit circle into an off-centre core plus 4-6 ring-band chunks with jittered rims,
        /// so the break reads as broken material rather than clean pie slices.
        /// </summary>
        void BuildPatterns()
        {
            for (int p = 0; p < PatternCount; p++)
            {
                int wedges = 4 + (p % 3);
                var meshes = new Mesh[wedges + 1];
                var centres = new Vector2[wedges + 1];
                Vector2 core = new Vector2((Hash(p, 91) - 0.5f) * 0.18f, (Hash(p, 137) - 0.5f) * 0.18f);

                float[] bounds = new float[wedges + 1];
                float acc = Hash(p, 11) * Mathf.PI * 2f;
                for (int w = 0; w < wedges; w++)
                {
                    bounds[w] = acc;
                    acc += (Mathf.PI * 2f / wedges) * Mathf.Lerp(0.72f, 1.28f, Hash(p * 31 + w, 53));
                }
                float correction = (Mathf.PI * 2f) / (acc - bounds[0]);
                bounds[wedges] = acc;
                for (int w = 1; w <= wedges; w++) bounds[w] = bounds[0] + (bounds[w] - bounds[0]) * correction;

                float innerBase = Mathf.Lerp(0.17f, 0.24f, Hash(p, 211));
                const int rimSteps = 3;
                int ring = rimSteps + 1;
                for (int w = 0; w < wedges; w++)
                {
                    var verts = new Vector3[ring * 2];
                    for (int r = 0; r <= rimSteps; r++)
                    {
                        float angle = Mathf.Lerp(bounds[w], bounds[w + 1], r / (float)rimSteps);
                        float cos = Mathf.Cos(angle), sin = Mathf.Sin(angle);
                        float outer = 0.5f * Mathf.Lerp(0.88f, 1f, Hash(p * 977 + w * 61 + r, 17));
                        float inner = innerBase * Mathf.Lerp(0.75f, 1.25f, Hash(p * 613 + w * 47 + r, 29));
                        verts[r] = new Vector3(core.x + cos * inner, core.y + sin * inner, 0f);
                        verts[ring + r] = new Vector3(cos * outer, sin * outer, 0f);
                    }
                    var tris = new int[rimSteps * 6];
                    for (int r = 0; r < rimSteps; r++)
                    {
                        int i0 = r, i1 = r + 1, o0 = ring + r, o1 = ring + r + 1;
                        tris[r * 6] = i0; tris[r * 6 + 1] = o0; tris[r * 6 + 2] = o1;
                        tris[r * 6 + 3] = i0; tris[r * 6 + 4] = o1; tris[r * 6 + 5] = i1;
                    }
                    meshes[w] = BuildMesh("Chip p" + p + " w" + w, verts, tris, out centres[w]);
                }

                const int coreSteps = 7;
                var coreVerts = new Vector3[coreSteps];
                for (int r = 0; r < coreSteps; r++)
                {
                    float angle = r / (float)coreSteps * Mathf.PI * 2f + bounds[0];
                    float radius = innerBase * Mathf.Lerp(0.95f, 1.35f, Hash(p * 331 + r, 37));
                    coreVerts[r] = new Vector3(core.x + Mathf.Cos(angle) * radius, core.y + Mathf.Sin(angle) * radius, 0f);
                }
                var coreTris = new int[(coreSteps - 2) * 3];
                for (int r = 0; r < coreSteps - 2; r++) { coreTris[r * 3] = 0; coreTris[r * 3 + 1] = r + 1; coreTris[r * 3 + 2] = r + 2; }
                meshes[wedges] = BuildMesh("Chip p" + p + " core", coreVerts, coreTris, out centres[wedges]);

                patterns[p] = meshes;
                centroids[p] = centres;
            }
        }

        Mesh BuildMesh(string name, Vector3[] verts, int[] tris, out Vector2 centroid)
        {
            Vector3 c = Vector3.zero;
            foreach (var v in verts) c += v;
            c /= verts.Length;
            var uvs = new Vector2[verts.Length];
            for (int v = 0; v < verts.Length; v++)
            {
                uvs[v] = new Vector2(verts[v].x + 0.5f, verts[v].y + 0.5f);
                verts[v] -= c;
            }
            var mesh = new Mesh { name = name, hideFlags = HideFlags.HideAndDontSave, vertices = verts, uv = uvs, triangles = tris };
            mesh.RecalculateBounds();
            ownedMeshes.Add(mesh);
            centroid = c;
            return mesh;
        }

        internal Mesh[] PatternMeshes(int pattern) => patterns[pattern];

        static float Hash(int a, int b)
        {
            unchecked
            {
                int h = a * 73856093 ^ b * 19349663;
                h = (h ^ (h >> 13)) * 1274126177;
                return ((h ^ (h >> 16)) & 0x7fffff) / (float)0x7fffff;
            }
        }

        Material MaterialFor(Texture tex)
        {
            if (!tex) return baseMaterial;
            if (materialsByTexture.TryGetValue(tex, out var m) && m) return m;
            m = new Material(baseMaterial) { hideFlags = HideFlags.HideAndDontSave, mainTexture = tex };
            materialsByTexture[tex] = m;
            return m;
        }

        // ------------------------------------------------------------------ play

        public void Shatter(SpriteRenderer source, float intensity, Match3ShatterStyle style, int colorIndex)
        {
            if (!source || !source.sprite || !source.sprite.texture) return;
            if (library != S.burstLibrary) BuildAuthoredBursts(S.burstLibrary);

            var s = S;
            var sprite = source.sprite;
            Texture tex = sprite.texture;
            int pattern = spawnCounter % PatternCount;
            spawnCounter++;

            Mesh[] meshes = patterns[pattern];
            Vector2[] centres = centroids[pattern];
            bool authored = burstMeshes != null && colorIndex >= 0 && colorIndex < burstMeshes.Length && burstMeshes[colorIndex].Length > 0;
            if (authored)
            {
                meshes = burstMeshes[colorIndex];
                centres = burstOffsets[colorIndex];
                tex = library.bursts[colorIndex].pieces[0].sprite.texture;
            }

            bool rocket = style == Match3ShatterStyle.Rocket;
            bool power = style == Match3ShatterStyle.PowerClear;
            bool reduced = rig.ReducedMotion;
            int chips = rocket
                ? (reduced ? 3 : Mathf.Min(meshes.Length, 3) * 2)
                : Mathf.Min(meshes.Length, reduced ? 2 : power ? 3 : MaxWedges);

            // Near the pool limit, break into fewer chips instead of dropping the event.
            // A chip that is still falling is never recycled.
            int free = FreeCount();
            if (free == 0) return;
            if (free < MaxWedges * 2) chips = Mathf.Min(chips, 3);
            chips = Mathf.Min(chips, free);

            Bounds b = source.bounds;
            Vector3 centre = b.center;
            centre.z -= 0.45f;
            float worldSize = Mathf.Max(b.size.x, b.size.y);
            // Chip size follows the stable grid pitch, never a pop-enlarged renderer.
            float cell = Mathf.Max(0.01f, rig.Cell);
            if (authored) worldSize = Mathf.Min(worldSize, cell);
            float bottom = rig.Host.BoardBottomY;
            float exitY = float.IsNegativeInfinity(bottom) || float.IsNaN(bottom) ? centre.y - worldSize * 9f : bottom - worldSize * 1.1f;

            Rect tr = sprite.textureRect;
            Vector4 st = authored ? new Vector4(1, 1, 0, 0)
                : new Vector4(tr.width / tex.width, tr.height / tex.height, tr.x / tex.width, tr.y / tex.height);
            int seed = spawnCounter * 397;

            for (int w = 0; w < chips; w++)
            {
                var f = Take();
                if (f == null) break;

                int index = authored ? (w * meshes.Length / chips + spawnCounter) % meshes.Length : w % meshes.Length;
                f.filter.sharedMesh = meshes[index];
                f.renderer.sharedMaterial = MaterialFor(tex);

                Vector2 local = centres[index];
                Vector2 birth = authored ? Vector2.ClampMagnitude(local, 0.35f) * cell : local * worldSize;
                f.tr.position = centre + new Vector3(birth.x, birth.y, 0f);
                f.tr.rotation = Quaternion.identity;

                Vector2 outward = local.sqrMagnitude > 0.0001f ? local.normalized : Vector2.up;
                float skew = (Hash(seed + w, 7) - 0.5f) * s.shatterDirectionJitter;
                float cos = Mathf.Cos(skew), sin = Mathf.Sin(skew);
                Vector2 dir = new Vector2(outward.x * cos - outward.y * sin, outward.x * sin + outward.y * cos);
                float speed = s.shatterBurstSpeed * worldSize * Mathf.Lerp(0.62f, 1.38f, Hash(seed + w, 23)) * intensity;
                f.velocity = new Vector3(dir.x * speed * (reduced ? 0.4f : 0.72f),
                    Mathf.Clamp(dir.y * speed, -worldSize * 2f, worldSize * 1.25f) + s.shatterUpwardBias * worldSize, 0f);

                f.spin = Hash(seed + w, 41) * 360f;
                f.spinSpeed = Mathf.Lerp(s.shatterSpinMin, s.shatterSpinMax, Hash(seed + w, 59)) * (Hash(seed + w, 71) < 0.5f ? -1f : 1f);

                // Confirmed matches and rocket hits fade and shrink; blast clears fall off the board solid.
                f.smooth = rocket || style == Match3ShatterStyle.Match;
                f.life = f.smooth ? (rocket ? Mathf.Lerp(0.65f, 0.85f, Hash(seed + w, 151)) : 0.78f) : Mathf.Max(1f, s.shatterLifetime);
                f.exitY = exitY;
                f.gravity = Mathf.Min(-30f, s.shatterGravity) * worldSize;
                if (f.smooth)
                {
                    f.gravity = -9f * worldSize;
                    f.velocity *= 0.55f;
                    f.spinSpeed *= 0.55f;
                }
                f.age = 0f;

                float variation = Mathf.Lerp(0.92f, 1.10f, Hash(seed + w, 101));
                f.baseScale = worldSize * variation;
                if (authored)
                {
                    Vector3 size = meshes[index].bounds.size;
                    float diagonal = Mathf.Max(0.001f, new Vector2(size.x, size.y).magnitude);
                    // A diagonal bound stays valid through the whole tumble.
                    f.baseScale = Mathf.Min(cell * 0.9f * variation, cell * MaxAuthoredChipCellFraction / diagonal);
                }
                if (rocket) f.baseScale *= w % 2 == 0 ? 0.55f : 0.85f;
                f.tr.localScale = new Vector3(f.baseScale, f.baseScale, 1f);

                f.tint = source.color;
                f.tint.a = 1f;
                f.props.Clear();
                f.props.SetVector("_MainTex_ST", st);
                f.props.SetColor("_Color", f.tint);
                f.renderer.SetPropertyBlock(f.props);
                f.active = true;
                f.renderer.enabled = true;
            }
            PeakFragments = Mathf.Max(PeakFragments, ActiveFragments);
        }

        int FreeCount()
        {
            int n = 0;
            for (int i = 0; i < pool.Length; i++) if (!pool[i].active) n++;
            return n;
        }

        Fragment Take()
        {
            for (int i = 0; i < pool.Length; i++)
            {
                int index = (cursor + i) % pool.Length;
                if (pool[index].active) continue;
                cursor = (index + 1) % pool.Length;
                return pool[index];
            }
            return null;
        }

        public void Tick(float dt)
        {
            float drag = Mathf.Clamp01(S.shatterDrag * dt);
            for (int i = 0; i < pool.Length; i++)
            {
                var f = pool[i];
                if (!f.active) continue;
                f.age += dt;
                if (f.age >= f.life || f.tr.position.y < f.exitY)
                {
                    f.active = false;
                    f.renderer.enabled = false;
                    continue;
                }

                if (f.smooth) f.tr.position += new Vector3(0f, -0.5f * f.gravity * dt * dt, 0f);
                f.velocity.y += f.gravity * dt;
                f.velocity.x *= 1f - drag;
                f.tr.position += f.velocity * dt;
                f.spin += f.spinSpeed * dt;
                f.tr.rotation = Quaternion.Euler(0f, 0f, f.spin);

                if (!f.smooth) continue;
                float fade = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.5f, f.life, f.age));
                float scale = f.baseScale * Mathf.Lerp(1f, 0.65f, fade);
                f.tr.localScale = new Vector3(scale, scale, 1f);
                var tint = f.tint;
                tint.a *= 1f - fade;
                f.props.SetColor("_Color", tint);
                f.renderer.SetPropertyBlock(f.props);
            }
        }

        public void Clear()
        {
            foreach (var f in pool) { f.active = false; f.renderer.enabled = false; }
        }

        public void Dispose()
        {
            foreach (var mesh in ownedMeshes) if (mesh) Object.Destroy(mesh);
            foreach (var m in materialsByTexture.Values) if (m) Object.Destroy(m);
            if (baseMaterial) Object.Destroy(baseMaterial);
            ownedMeshes.Clear();
            materialsByTexture.Clear();
        }
    }
}
