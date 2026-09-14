// A cleared gem bursts into a cloud of glowing motes that drift and fall.
//
// The kit clears a gem by playing a 4-frame flipbook over it and switching the
// gem off: it vanishes in place. This dissolves it instead — the gem becomes a
// cloud of soft round motes in its own colour, thrown outward, slowed by drag,
// then carried down by gravity and off the board.
//
// WHY SOFT MOTES AND NOT SHARDS
//
// The first version cut the gem's sprite into a 5x5 grid and threw the pieces.
// It worked, and it read as SHARP — every piece was a literal rectangle with
// hard corners and flat colour, which is what a grid cut produces and nothing
// about it suggests a solid object breaking. Five variants were mocked up and
// compared side by side (canvas, real physics, same gravity and spin as here);
// this is the one that was chosen. A mote has no edge at all, so it cannot
// look sharp however small it is drawn.
//
// WHY SPRITE RENDERERS AND NOT A PARTICLE SYSTEM
//
// Every ParticleSystem this project has spawned on the board renders nothing —
// the kit's own pooled flipbook included, which is why a cleared cell just
// empties today. Twelve consecutive captured frames across two bursts show no
// trace of either, while the logs report both as positioned, enabled and
// drawing. That cause is still unfound.
//
// What DOES provably render is a SpriteRenderer on the board: the shard
// version of this file was captured mid-fall on a real board. So a mote is a
// SpriteRenderer carrying a generated radial-glow sprite, on the gem's own
// sorting layer — the same kind of object the board already draws
// successfully, rather than a kind that has not.
//
// WHAT THE NUMBERS COME FROM
//
//   POOL     The worst case is not a cascade, it is a colour bomb:
//            Item.DestroyColor finds every gem of one colour by tag and
//            destroys the lot in one frame. The largest board is 9x11 = 99
//            cells (862 of the 1208 levels are exactly that), and at
//            COLOR LIMIT 4 — 275 levels — a third of the board can be one
//            colour. 25 gems x 34 motes is about 850 in a single frame.
//
//   LIFE     Refill gems fall at speed 10 accelerating 0.2 per fixed update,
//            roughly 100-150ms per cell (Item.FallingCor). A cloud living
//            900ms is crossed by six or more incoming gems and reads as
//            noise. Motes are lighter than shards and drift rather than
//            plummet, so they can live a little longer without cluttering.
//
//   ORDER    Gems draw at sorting order 2 and blockers at 4 (Item.prefab,
//            Square.cs). Motes draw at 5, in front of the board.
//
// Nothing is clipped on the way out: the board is world-space sprites and
// game.unity contains no RectMask2D at all, so a mote keeps falling once it
// leaves the grid.

using System.Collections.Generic;
using UnityEngine;

namespace JellyGarden.Scripts.Story
{
    public static class StoryShatterVfx
    {
        /// <summary>
        /// Off unless a launch asks for it, so the board behaves exactly as the
        /// kit does by default. Flipped by <c>-storyshatter</c>.
        /// </summary>
        public static bool Enabled
        {
            get
            {
                if (_enabled.HasValue) return _enabled.Value;
                _enabled = false;
                foreach (string arg in global::System.Environment.GetCommandLineArgs())
                    if (arg == "-storyshatter") { _enabled = true; break; }
                return _enabled.Value;
            }
            set { _enabled = value; }
        }
        private static bool? _enabled;

        // A cloud, not a handful. Below about twenty the motes read as separate
        // dots rather than as one dissolving gem; the mockup settled on this.
        private const int PerGem = 34;

        // 900 covers the colour-bomb case above with room to spare. A pool that
        // runs dry does not look sparser, it looks like the effect stuttering —
        // the lesson RepairVfx records for its own 220 quads.
        private const int PoolSize = 900;

        // Motes are light: they are thrown out fast, drag kills most of that in
        // the first fifth of their life, and gravity then carries what is left
        // downward. That is the difference between a cloud and a handful of
        // gravel, and it is why they can live longer than shards without
        // colliding with the gems falling in behind them.
        private const float Life = 0.78f;
        private const float Gravity = -7.5f;
        private const float BurstSpeed = 3.4f;
        private const float Drag = 3.6f;

        // Each mote is a fraction of the GEM across, and the spread keeps the
        // cloud from reading as one uniform texture.
        //
        // Doubled from 0.055-0.185, which was measured too small to see. The
        // arithmetic: the gem sprite is 128px at 100 PPU = 1.28 world units,
        // drawn at ItemScale (0.64 * Factor 1.427), so it spans 1.169 units.
        // AspectCamera puts ortho at 12.92 for a nine-wide board at pitch
        // 1.17, which is 33 points per world unit on an 852-point window — so
        // the gem itself is only about 39 points across. At the old values a
        // mote was 2.1 to 7.1 points, and more than half of a mote's alpha is
        // a soft skirt that falls to nothing at its rim, so the part that
        // actually reads was around a third of that. These give 4.2 to 14.2
        // points, average about 9.
        private const float MoteMin = 0.11f;
        private const float MoteMax = 0.37f;

        // Above the gems (2) and the blockers (4), so a falling shard crosses
        // in front of the board rather than disappearing behind it.
        private const int SortingOrder = 5;

        private sealed class Shard
        {
            public GameObject Go;
            public Transform T;
            public SpriteRenderer R;
            public Vector3 Velocity;
            public float Born;
            // Per mote, not one constant for the cloud: motes that all die on
            // the same frame read as the effect being switched off.
            public float Life;
            // The scale it was born at. Kept so the shrink is measured from
            // the mote's own starting size rather than from last frame's,
            // which would compound and collapse it to nothing.
            public float BaseScale;
            public bool Alive;
        }

        private static readonly List<Shard> Pool = new List<Shard>(PoolSize);
        private static Transform _parent;
        private static Driver _driver;
        private static int _next;

        /// <summary>
        /// Break one cleared gem into falling pieces.
        ///
        /// Called with the gem still intact — before the kit switches it off —
        /// so its sprite, size and sorting can be copied off the live renderer.
        /// </summary>
        public static void Shatter(Item item)
        {
            if (!Enabled || !Application.isPlaying) return;
            if (item == null || item.sprRenderer == null) return;

            Sprite source = item.sprRenderer.sprite;
            if (source == null) return;

            EnsureRig();

            Transform gem = item.transform;
            Vector3 centre = gem.position;
            // The gem's own on-screen size, so the cloud is a real fraction of
            // the gem rather than a guess at the cell.
            float span = source.bounds.size.x * gem.lossyScale.x;

            // The gem's colour, taken from the board rather than from a table:
            // Item.color indexes LevelManager.scoresColors, the same list the
            // score popup uses, so a mote can never disagree with the gem it
            // came from.
            Color tint = ColourOf(item);

            Sprite glow = Glow();
            int layer = item.sprRenderer.gameObject.layer;
            int sortingLayer = item.sprRenderer.sortingLayerID;

            for (int i = 0; i < PerGem; i++)
            {
                Shard mote = Lease();
                if (mote == null) return;   // pool exhausted; drop the rest

                // Start scattered through the gem's own footprint rather than
                // all at its centre, so the first frame already looks like the
                // gem coming apart instead of a point emitting.
                Vector2 inside = Random.insideUnitCircle * 0.42f;
                Vector3 at = centre + new Vector3(inside.x * span, inside.y * span, -0.3f);

                float size = span * Random.Range(MoteMin, MoteMax);
                float scale = size / Mathf.Max(0.0001f, glow.bounds.size.x);

                mote.T.position = at;
                mote.T.localScale = Vector3.one * scale;
                mote.BaseScale = scale;
                mote.T.localRotation = Quaternion.identity;

                mote.R.sprite = glow;
                mote.R.color = tint;
                mote.R.sortingLayerID = sortingLayer;
                mote.R.sortingOrder = SortingOrder;
                mote.R.enabled = true;
                mote.Go.layer = layer;
                mote.Go.SetActive(true);

                // Outward from where the mote started, so the cloud expands
                // from the gem rather than being blown one way, with a little
                // lift so the fall has a top to it.
                Vector2 dir = inside.sqrMagnitude < 0.0001f
                    ? Random.insideUnitCircle.normalized
                    : inside.normalized;

                mote.Velocity = new Vector3(
                    dir.x * BurstSpeed * Random.Range(0.35f, 1.15f),
                    dir.y * BurstSpeed * Random.Range(0.3f, 0.95f) + Random.Range(0.6f, 2.1f),
                    0f);
                mote.Born = Time.time;
                mote.Life = Life * Random.Range(0.72f, 1.15f);
                mote.Alive = true;
            }
        }

        /// <summary>
        /// The colour of the gem being cleared, from the board's own table.
        /// Falls back to the renderer's tint when there is no manager, which is
        /// the case in an editor test with no board in the scene.
        /// </summary>
        private static Color ColourOf(Item item)
        {
            LevelManager manager = LevelManager.THIS;
            if (manager != null && manager.scoresColors != null &&
                item.color >= 0 && item.color < manager.scoresColors.Length)
            {
                Color c = manager.scoresColors[item.color];
                // Lifted towards white a little: a mote is a light, and the
                // raw gem colour reads muddy once it is drawn soft and small.
                return Color.Lerp(c, Color.white, 0.22f);
            }
            return item.sprRenderer.color;
        }

        /// <summary>
        /// The one sprite every mote draws: a soft radial glow, white, so the
        /// renderer's colour tints it to whichever gem produced it.
        ///
        /// Drawn once into a 48px texture rather than fetched from the art.
        /// The mockup that chose this look drew a radial gradient per mote in
        /// canvas, which Unity cannot do per-sprite at any sane cost; baking
        /// the same falloff into one texture and tinting it gives the same
        /// picture for one draw setup and no per-frame work.
        /// </summary>
        private static Sprite Glow()
        {
            if (_glow != null) return _glow;

            const int N = 48;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false)
            {
                name = "StoryShatterGlow",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };

            var px = new Color32[N * N];
            float half = (N - 1) * 0.5f;
            for (int y = 0; y < N; y++)
            {
                for (int x = 0; x < N; x++)
                {
                    float dx = (x - half) / half;
                    float dy = (y - half) / half;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);

                    // A bright core inside a wide soft skirt. The core is what
                    // carries the gem's colour at this size; the skirt is what
                    // stops the mote ever showing an edge, which is the whole
                    // point of choosing this variant.
                    float core = Mathf.Clamp01(1f - d / 0.42f);
                    float skirt = Mathf.Clamp01(1f - d);
                    float a = Mathf.Clamp01(core * 0.85f + skirt * skirt * 0.55f);

                    px[y * N + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);

            _glow = Sprite.Create(tex, new Rect(0f, 0f, N, N), new Vector2(0.5f, 0.5f), N);
            _glow.name = "mote";
            return _glow;
        }
        private static Sprite _glow;

        private static Shard Lease()
        {
            // Grow first, then look. An earlier version wrapped the cursor
            // modulo PoolSize and then indexed Pool with it, which throws the
            // moment the cursor runs past however many slots have actually been
            // built — List.Add appends at Count, not at the cursor. The pool
            // grows one slot per lease until it is full, so the cursor is only
            // ever taken modulo what EXISTS.
            if (Pool.Count < PoolSize) Pool.Add(Build(Pool.Count));

            int count = Pool.Count;
            for (int tried = 0; tried < count; tried++)
            {
                _next = (_next + 1) % count;
                Shard candidate = Pool[_next];
                if (candidate != null && !candidate.Alive) return candidate;
            }
            return null;
        }

        private static Shard Build(int index)
        {
            var go = new GameObject("Shard" + index);
            go.transform.SetParent(_parent, false);
            go.SetActive(false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = SortingOrder;
            return new Shard
            {
                Go = go,
                T = go.transform,
                R = renderer,
                Alive = false,
            };
        }

        private static void EnsureRig()
        {
            if (_parent != null && _driver != null) return;

            var holder = new GameObject("~StoryShatterVfx");
            Object.DontDestroyOnLoad(holder);
            _parent = holder.transform;
            _driver = holder.AddComponent<Driver>();
        }

        /// <summary>
        /// Moves every live shard. One component for the whole pool rather than
        /// one per shard: 640 MonoBehaviours each doing three lines of maths is
        /// 640 Update calls a frame for no benefit.
        /// </summary>
        private sealed class Driver : MonoBehaviour
        {
            private void Update()
            {
                float now = Time.time;
                float dt = Time.deltaTime;

                for (int i = 0; i < Pool.Count; i++)
                {
                    Shard mote = Pool[i];
                    if (mote == null || !mote.Alive) continue;

                    float age = (now - mote.Born) / mote.Life;
                    if (age >= 1f || mote.Go == null)
                    {
                        Retire(mote);
                        continue;
                    }

                    // Drag first, then gravity. The burst throws a mote out
                    // fast and drag eats most of that within the first fifth
                    // of its life, so what the eye sees is a cloud blooming
                    // and then sinking — not gravel on a ballistic arc. This
                    // ordering is what separates the two.
                    mote.Velocity -= mote.Velocity * Mathf.Min(1f, Drag * dt);
                    mote.Velocity.y += Gravity * dt;
                    mote.T.position += mote.Velocity * dt;

                    // Shrink as they go, so a mote thins out instead of
                    // vanishing at full size.
                    float shrink = Mathf.Lerp(1f, 0.55f, age * age);
                    mote.T.localScale = Vector3.one * (mote.BaseScale * shrink);

                    // Hold opacity through most of the life, then go. Fading
                    // from the first frame reads as the cloud evaporating
                    // rather than drifting away.
                    Color c = mote.R.color;
                    c.a = age < 0.55f ? 1f : Mathf.InverseLerp(1f, 0.55f, age);
                    mote.R.color = c;
                }
            }

            private static void Retire(Shard shard)
            {
                shard.Alive = false;
                if (shard.Go != null) shard.Go.SetActive(false);
            }
        }
    }
}
