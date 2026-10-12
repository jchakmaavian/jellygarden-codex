using System.Collections.Generic;
using UnityEngine;

namespace JellyGarden.Scripts.Juice
{
    // The ice tile's break (2026-10-04). The tile cracks and bursts in the same frame as the hit that breaks it: the gem on
    // it popping (Item.JuiceDestroyCor calls Early at the gem's own shatter), or a booster blast (Square.DestroyBlock). The
    // ice art splits into wedge shards around an impact point; they open along the cracks for a few frames, then fly out
    // with spin and gravity and fade, over a white flash, a frost puff and small glints. Presentation only: the block's
    // removal, goal count and score still happen in Square.DestroyBlock at their usual time. Each break is one pooled object
    // with two meshes (the shards in the ice texture, the frost in a soft white dot), so a cascade of breaks stays cheap.
    public sealed class RoyalIceBreak : MonoBehaviour
    {
        // Its own random numbers: UnityEngine.Random is the board's (it picks the refill colours), and a decoration must not move
        // it (bug review 2026-10-05; RoyalCellBreakChecks asserts this for the cell breaks).
        static readonly global::System.Random dice = new global::System.Random(1405);
        static float Dice(float min, float max) => min + (float)dice.NextDouble() * (max - min);
        static Vector2 DiceCircle() { float a = Dice(0, Mathf.PI * 2), r = Mathf.Sqrt((float)dice.NextDouble()); return new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r; }
        const int ShardCount = 8, PuffCount = 7, GlintCount = 12;
        const float Life = .55f, CrackTime = .045f;
        const int ShardOrder = 6, FrostOrder = 7;

        static readonly Stack<RoyalIceBreak> pool = new Stack<RoyalIceBreak>();
        static Material shardMaterial, frostMaterial;
        static Texture2D dot;
        static int lastSoundFrame = -1;

        sealed class Shard { public Vector2[] local; public Vector2[] uv; public Vector2 centroid, velocity, offset, outward; public float angle, spin, scale, shrink; }
        readonly List<Shard> shards = new List<Shard>();
        struct Mote { public Vector2 dir; public float speed, size, delay; }
        readonly Mote[] puffs = new Mote[PuffCount], glints = new Mote[GlintCount];
        Mesh shardMesh, frostMesh; MeshRenderer shardRenderer, frostRenderer; Material shardInstance;
        readonly List<Vector3> v = new List<Vector3>(); readonly List<Vector2> u = new List<Vector2>(); readonly List<Color> c = new List<Color>(); readonly List<int> tri = new List<int>();
        float age, cell, power; Vector3 centre;

        /// <summary>True for the ice tile's art (the legacy "block" and "block_02" sprites, drawn with the ice art).</summary>
        public static bool IsIce(SpriteRenderer r) => r && r.sprite && (r.sprite.name == "block" || r.sprite.name == "block_02");

        /// <summary>Breaks the ice on `square` now, ahead of its logical removal (the gem on it is popping). Hides that layer.</summary>
        public static void Early(Square square)
        {
            if (!square || square.type != SquareTypes.BLOCK || square.block == null || square.block.Count == 0) return;
            var top = square.block[square.block.Count - 1]; if (!top) return;
            var r = top.GetComponent<SpriteRenderer>();
            if (!IsIce(r) || !r.enabled || top.GetComponent<Marker>()) return;
            top.AddComponent<Marker>();
            Play(r, 1);
            r.enabled = false;
        }

        /// <summary>Square.DestroyBlock: plays the break unless Early already did. Returns true when this is ice.</summary>
        public static bool OnDestroyed(GameObject block, float intensity)
        {
            var r = block ? block.GetComponent<SpriteRenderer>() : null;
            if (!IsIce(r)) return false;
            if (!block.GetComponent<Marker>()) { block.AddComponent<Marker>(); Play(r, intensity); }
            r.enabled = false;
            return true;
        }

        sealed class Marker : MonoBehaviour { }

        public static void Play(SpriteRenderer source, float intensity)
        {
            if (!source || !source.sprite) return;
            var lm = LevelManager.THIS;
            var fx = pool.Count > 0 ? pool.Pop() : Create();
            if (!fx) return;
            fx.Begin(source, lm ? Mathf.Min(lm.squareWidth, lm.squareHeight) : source.bounds.size.x, Mathf.Clamp(intensity, .5f, 1.5f));
            if (Time.frameCount != lastSoundFrame && SoundBase.Instance) { lastSoundFrame = Time.frameCount; SoundBase.Instance.PlaySound(SoundBase.Instance.block_destroy); }
        }

        static RoyalIceBreak Create()
        {
            if (!shardMaterial) shardMaterial = new Material(Shader.Find("Sprites/Default")) { name = "Royal ice shards" };
            if (!frostMaterial) frostMaterial = new Material(Shader.Find("Sprites/Default")) { name = "Royal ice frost", mainTexture = Dot() };
            var go = new GameObject("Royal ice break"); DontDestroyOnLoad(go);
            var fx = go.AddComponent<RoyalIceBreak>();
            MeshRenderer Part(string name, out Mesh mesh, Material material, int order)
            {
                var child = new GameObject(name); child.transform.SetParent(go.transform, false);
                mesh = new Mesh { name = name }; mesh.MarkDynamic();
                child.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = child.AddComponent<MeshRenderer>(); r.sharedMaterial = material; r.sortingOrder = order; return r;
            }
            fx.shardRenderer = Part("shards", out fx.shardMesh, shardMaterial, ShardOrder);
            fx.frostRenderer = Part("frost", out fx.frostMesh, frostMaterial, FrostOrder);
            fx.shardInstance = new Material(shardMaterial); fx.shardRenderer.sharedMaterial = fx.shardInstance;
            return fx;
        }

        static Texture2D Dot()
        {
            if (dot) return dot;
            const int n = 64; dot = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "Royal ice frost dot", wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            var px = new Color[n * n];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float dx = (x + .5f) / n * 2 - 1, dy = (y + .5f) / n * 2 - 1, d = Mathf.Sqrt(dx * dx + dy * dy);
                px[y * n + x] = new Color(1, 1, 1, Mathf.Clamp01(1 - d) * Mathf.Clamp01(1 - d));
            }
            dot.SetPixels(px); dot.Apply(false, true); return dot;
        }

        void Begin(SpriteRenderer source, float cellSize, float intensity)
        {
            gameObject.SetActive(true);
            age = 0; cell = cellSize; power = intensity;
            var b = source.bounds; centre = b.center; centre.z = source.transform.position.z - .3f;
            transform.position = centre; transform.rotation = Quaternion.identity; transform.localScale = Vector3.one;
            shardRenderer.sortingLayerID = frostRenderer.sortingLayerID = source.sortingLayerID;
            var sprite = source.sprite; var tex = sprite.texture; shardInstance.mainTexture = tex;
            var tr = sprite.textureRect; var uvRect = new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height);
            float w = b.size.x, h = b.size.y;
            Vector2 UV(Vector2 p) => new Vector2(uvRect.xMin + (p.x / w + .5f) * uvRect.width, uvRect.yMin + (p.y / h + .5f) * uvRect.height);
            // The impact point near the middle, and the square's outline cut into wedges around it.
            var impact = new Vector2(Dice(-.12f, .12f) * w, Dice(-.12f, .12f) * h);
            var cuts = new float[ShardCount];
            float start = Dice(0f, 360f);
            for (int i = 0; i < ShardCount; i++) cuts[i] = start + (i + Dice(-.3f, .3f)) * 360f / ShardCount;
            var corners = new[] { new Vector2(w * .5f, h * .5f), new Vector2(-w * .5f, h * .5f), new Vector2(-w * .5f, -h * .5f), new Vector2(w * .5f, -h * .5f) };
            Vector2 OnEdge(float degrees)
            {
                var d = new Vector2(Mathf.Cos(degrees * Mathf.Deg2Rad), Mathf.Sin(degrees * Mathf.Deg2Rad));
                float tx = d.x != 0 ? ((d.x > 0 ? w * .5f : -w * .5f) - impact.x) / d.x : float.MaxValue;
                float ty = d.y != 0 ? ((d.y > 0 ? h * .5f : -h * .5f) - impact.y) / d.y : float.MaxValue;
                return impact + d * Mathf.Min(tx, ty);
            }
            float AngleOf(Vector2 p) { var a = Mathf.Atan2(p.y - impact.y, p.x - impact.x) * Mathf.Rad2Deg; return a; }
            shards.Clear();
            void AddShard(List<Vector2> poly, float speedMin, float speedMax, float spinMax, float shrink)
            {
                var shard = new Shard { local = poly.ToArray(), uv = new Vector2[poly.Count] };
                Vector2 sum = Vector2.zero; foreach (var p in poly) sum += p; shard.centroid = sum / poly.Count;
                for (int k = 0; k < poly.Count; k++) { shard.uv[k] = UV(poly[k]); shard.local[k] = poly[k] - shard.centroid; }
                var outward = (shard.centroid - impact).normalized; if (outward == Vector2.zero) outward = DiceCircle().normalized;
                shard.outward = outward; shard.shrink = shrink;
                shard.velocity = (outward * Dice(speedMin, speedMax) + Vector2.up * Dice(1f, 2f)) * cell * power;
                shard.spin = Dice(-spinMax, spinMax) * power; shard.angle = 0; shard.offset = Vector2.zero; shard.scale = 1;
                shards.Add(shard);
            }
            // Each wedge is cut again across, at 40-60% of its length: a small inner chip that pops out slowly and fades
            // first, and an outer piece that flies fast; 16 pieces in all.
            for (int i = 0; i < ShardCount; i++)
            {
                float a0 = cuts[i], a1 = cuts[(i + 1) % ShardCount] + (i == ShardCount - 1 ? 360 : 0);
                Vector2 e0 = OnEdge(a0), e1 = OnEdge(a1);
                Vector2 m0 = impact + (e0 - impact) * Dice(.4f, .6f), m1 = impact + (e1 - impact) * Dice(.4f, .6f);
                AddShard(new List<Vector2> { impact, m0, m1 }, .8f, 1.6f, 300, .25f);
                var outer = new List<Vector2> { m0, e0 };
                var inside = new List<(float, Vector2)>();
                foreach (var corner in corners) { float a = AngleOf(corner); while (a < a0) a += 360; while (a >= a0 + 360) a -= 360; if (a < a1) inside.Add((a, corner)); }
                inside.Sort((p, q) => p.Item1.CompareTo(q.Item1));
                foreach (var item in inside) outer.Add(item.Item2);
                outer.Add(e1); outer.Add(m1);
                AddShard(outer, 2.4f, 3.8f, 540, .45f);
            }
            for (int i = 0; i < PuffCount; i++) puffs[i] = new Mote { dir = DiceCircle().normalized, speed = Dice(.25f, .6f) * cell, size = Dice(.35f, .6f) * cell, delay = Dice(0f, .05f) };
            for (int i = 0; i < GlintCount; i++) glints[i] = new Mote { dir = DiceCircle().normalized, speed = Dice(1.6f, 3f) * cell * power, size = Dice(.07f, .13f) * cell, delay = Dice(0f, .06f) };
            Draw();
        }

        void Update()
        {
            float dt = Mathf.Min(Time.deltaTime, .05f);
            age += dt;
            if (age >= Life) { gameObject.SetActive(false); pool.Push(this); return; }
            if (age > CrackTime)
                foreach (var s in shards)
                {
                    s.velocity += Vector2.down * 11f * cell * dt;
                    s.offset += s.velocity * dt; s.angle += s.spin * dt;
                    s.scale = Mathf.Lerp(1, s.shrink, (age - CrackTime) / (Life - CrackTime));
                }
            else foreach (var s in shards) s.offset = s.outward * cell * .035f * (age / CrackTime);   // the cracks open
            Draw();
        }

        void Draw()
        {
            v.Clear(); u.Clear(); c.Clear(); tri.Clear();
            float fade = age < Life * .55f ? 1 : 1 - (age - Life * .55f) / (Life * .45f);
            var tint = new Color(1, 1, 1, Mathf.Clamp01(fade));
            foreach (var s in shards)
            {
                int first = v.Count; var rot = Quaternion.Euler(0, 0, s.angle);
                for (int k = 0; k < s.local.Length; k++)
                {
                    Vector2 p = rot * (s.local[k] * s.scale);
                    v.Add(new Vector3(s.centroid.x + s.offset.x + p.x, s.centroid.y + s.offset.y + p.y, 0)); u.Add(s.uv[k]); c.Add(tint);
                }
                for (int k = 1; k < s.local.Length - 1; k++) { tri.Add(first); tri.Add(first + k); tri.Add(first + k + 1); }
            }
            shardMesh.Clear(); shardMesh.SetVertices(v); shardMesh.SetUVs(0, u); shardMesh.SetColors(c); shardMesh.SetTriangles(tri, 0); shardMesh.RecalculateBounds();

            v.Clear(); u.Clear(); c.Clear(); tri.Clear();
            // The impact flash: a bright white bloom over the tile for the first tenth of a second.
            float flash = Mathf.Clamp01(1 - age / .12f);
            if (flash > 0) Quad(Vector2.zero, cell * (1.05f + .25f * (1 - flash)), new Color(1, 1, 1, .85f * flash));
            foreach (var p in puffs)
            {
                float t = (age - p.delay) / .4f; if (t <= 0 || t >= 1) continue;
                Quad(p.dir * p.speed * t * 1.6f, p.size * (.6f + .8f * t), new Color(.88f, .97f, 1, .55f * (1 - t)));
            }
            foreach (var g in glints)
            {
                float t = (age - g.delay) / .45f; if (t <= 0 || t >= 1) continue;
                var at = g.dir * g.speed * t * (1 - .35f * t) + Vector2.down * 2.5f * cell * t * t * .3f;
                Quad(at, g.size * (1 - t * .6f), new Color(1, 1, 1, 1 - t));
            }
            frostMesh.Clear(); frostMesh.SetVertices(v); frostMesh.SetUVs(0, u); frostMesh.SetColors(c); frostMesh.SetTriangles(tri, 0); frostMesh.RecalculateBounds();
        }

        void Quad(Vector2 at, float size, Color colour)
        {
            int first = v.Count; float s = size * .5f;
            v.Add(new Vector3(at.x - s, at.y - s)); v.Add(new Vector3(at.x + s, at.y - s)); v.Add(new Vector3(at.x + s, at.y + s)); v.Add(new Vector3(at.x - s, at.y + s));
            u.Add(new Vector2(0, 0)); u.Add(new Vector2(1, 0)); u.Add(new Vector2(1, 1)); u.Add(new Vector2(0, 1));
            for (int i = 0; i < 4; i++) c.Add(colour);
            tri.Add(first); tri.Add(first + 1); tri.Add(first + 2); tri.Add(first); tri.Add(first + 2); tri.Add(first + 3);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { pool.Clear(); lastSoundFrame = -1; }
    }
}
