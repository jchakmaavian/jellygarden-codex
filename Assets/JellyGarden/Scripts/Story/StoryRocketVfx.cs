// The rocket that actually travels.
//
// WHAT THE KIT DOES
//
// LevelManager.StrippedShow drops two sprite halves at the rocket's own cell,
// rotates them for a vertical, and destroys them a second later. Meanwhile
// Item.DestroyHorizontal kills every gem in the row inside one foreach with no
// delay at all. So the whole row dies on a single frame while a stripe flashes
// in place: no direction, no speed, no duration. It reads as a flash, not a
// shot.
//
// WHAT THIS ADDS
//
//   HEAD    A bright core flies from the rocket's cell to each end of the line,
//           reaching the far edge in SweepSeconds. Two heads, opposite ways.
//   TRAIL   Each head lays down stretched streaks behind it that fade over
//           TrailLife, so the travel is legible at speed rather than being a
//           blur the eye cannot follow.
//
// It is purely visual: nothing here destroys anything. The stagger — gems dying
// as the head reaches them rather than all at once — is a separate change in
// Item.cs, and this exposes ReachesAt() so the two agree about when the head
// arrives at a given cell rather than each keeping its own idea of the timing.
//
// WHY SPRITE RENDERERS
//
// Every ParticleSystem spawned on this board renders nothing, the kit's own
// pooled flipbook included; that cause is still unfound. A SpriteRenderer
// carrying a generated texture provably renders — StoryShatterVfx's motes were
// captured mid-flight on a real board. Same approach here, same reason.
//
// SORTING
//
// Gems draw at order 2 and blockers at 4 (Item.prefab, Square.cs), and the
// kit's own StripesEffect sits at 0, which is part of why it reads so weakly —
// it is drawn UNDER the board it is supposed to be cutting through. The head
// and its trail draw at 6.

using System.Collections.Generic;
using UnityEngine;

namespace JellyGarden.Scripts.Story
{
    public static class StoryRocketVfx
    {
        /// <summary>Off unless a launch asks for it. Flipped by <c>-storyrocket</c>.</summary>
        public static bool Enabled
        {
            get
            {
                if (_enabled.HasValue) return _enabled.Value;
                _enabled = false;
                foreach (string arg in global::System.Environment.GetCommandLineArgs())
                    if (arg == "-storyrocket") { _enabled = true; break; }
                return _enabled.Value;
            }
            set { _enabled = value; }
        }
        private static bool? _enabled;

        /// <summary>
        /// How long the head takes to cross ONE cell.
        ///
        /// A nine-wide board is four or five cells from the middle to an edge,
        /// so at 90ms a sweep finishes in about 400ms from a centre cell and
        /// 720ms from a corner.
        ///
        /// This has been slowed twice: 26ms, then 55ms, both still read as too
        /// fast. The ceiling is the refill — the kit clears the whole line on
        /// one frame and gems start dropping into the gaps immediately, falling
        /// a cell every 100-150ms (Item.FallingCor) — so a head slower than
        /// about a cell per 100ms is travelling over cells that have already
        /// refilled, which reads as the effect lagging the board rather than
        /// driving it. 90ms is close to that limit and is as slow as this can
        /// honestly go while the kit's clear stays synchronous.
        /// </summary>
        public const float PerCellSeconds = 0.090f;

        /// <summary>
        /// How long a streak lives after the head has passed it.
        ///
        /// Roughly four cells of travel at the current speed, so the trail is
        /// about half a board long behind the head. Raised with the speed: at
        /// the old 220ms the streaks died almost as fast as they were laid and
        /// the slower head dragged a stub rather than a tail.
        /// </summary>
        private const float TrailLife = 0.30f;

        /// <summary>How long the head itself lives past the end of its run.</summary>
        private const float HeadFade = 0.10f;

        // One streak laid per this many seconds of travel. Dense enough to read
        // as a continuous line, sparse enough that a nine-cell sweep is about
        // twenty objects rather than one per frame.
        private const float TrailEvery = 0.012f;

        private const int SortingOrder = 6;

        // Two heads per rocket, and a rocket-plus-rocket combo fires two of
        // those at once, so four heads and their streaks. 160 covers a colour
        // bomb turning several stripes at the same moment.
        private const int PoolSize = 160;

        /// <summary>
        /// When the head reaches a cell `distance` cells from the origin.
        ///
        /// Item.cs uses this to delay its destroys so a gem dies as the head
        /// arrives. Both sides reading the same function is the point: a
        /// separate constant in each would drift apart the first time one was
        /// tuned.
        /// </summary>
        public static float ReachesAt(int distance)
        {
            return Mathf.Abs(distance) * PerCellSeconds;
        }

        private sealed class Piece
        {
            public GameObject Go;
            public Transform T;
            public SpriteRenderer R;
            public float Born;
            public float Life;
            public float BaseScale;
            public bool IsHead;
            public bool Alive;
        }

        private static readonly List<Piece> Pool = new List<Piece>(PoolSize);
        private static Transform _parent;
        private static Driver _driver;
        private static int _next;

        /// <summary>
        /// Fire the two heads for one rocket.
        ///
        /// `horizontal` picks the axis. `cells` is how far the line runs each
        /// way — the board's width or height — so the head leaves the board
        /// rather than stopping short on a narrow level.
        /// </summary>
        public static void Fire(Vector3 at, bool horizontal, int cellsEachWay, float cellSize, Color tint)
        {
            if (!Enabled || !Application.isPlaying) return;

            EnsureRig();

            Vector3 axis = horizontal ? Vector3.right : Vector3.up;
            float distance = (cellsEachWay + 1) * cellSize;

            LaunchHead(at, axis, distance, cellSize, tint);
            LaunchHead(at, -axis, distance, cellSize, tint);
        }

        private static void LaunchHead(Vector3 from, Vector3 direction, float distance,
                                       float cellSize, Color tint)
        {
            var head = new Head
            {
                From = from,
                Dir = direction,
                Distance = distance,
                CellSize = cellSize,
                Tint = tint,
                Born = Time.time,
                Duration = (distance / Mathf.Max(0.0001f, cellSize)) * PerCellSeconds,
                NextTrail = 0f,
            };
            Heads.Add(head);
        }

        private sealed class Head
        {
            public Vector3 From;
            public Vector3 Dir;
            public float Distance;
            public float CellSize;
            public Color Tint;
            public float Born;
            public float Duration;
            public float NextTrail;
        }

        private static readonly List<Head> Heads = new List<Head>(8);

        // ---- the two textures -------------------------------------------------

        /// <summary>The head: a hot white core inside a coloured halo.</summary>
        private static Sprite Core()
        {
            if (_core != null) return _core;
            const int N = 64;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false)
            {
                name = "StoryRocketCore", wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var px = new Color32[N * N];
            float half = (N - 1) * 0.5f;
            for (int y = 0; y < N; y++)
            {
                for (int x = 0; x < N; x++)
                {
                    float dx = (x - half) / half, dy = (y - half) / half;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    // A tight white centre carries the "hot" read; the wide
                    // skirt is what keeps it from having a visible edge.
                    float core = Mathf.Clamp01(1f - d / 0.30f);
                    float halo = Mathf.Clamp01(1f - d);
                    float a = Mathf.Clamp01(core + halo * halo * 0.7f);
                    byte white = (byte)(Mathf.Clamp01(core * 1.4f) * 255f);
                    px[y * N + x] = new Color32(255, (byte)(200 + white * 55 / 255),
                                                (byte)(120 + white * 135 / 255), (byte)(a * 255f));
                }
            }
            tex.SetPixels32(px); tex.Apply(false, true);
            _core = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), N);
            _core.name = "rocketCore";
            return _core;
        }
        private static Sprite _core;

        /// <summary>
        /// A streak: bright at the head end, falling to nothing at the tail.
        /// Drawn wide and short, then stretched by the transform, so one
        /// texture serves every length.
        /// </summary>
        private static Sprite Streak()
        {
            if (_streak != null) return _streak;
            const int WPX = 96, HPX = 24;
            var tex = new Texture2D(WPX, HPX, TextureFormat.RGBA32, false)
            {
                name = "StoryRocketStreak", wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var px = new Color32[WPX * HPX];
            for (int y = 0; y < HPX; y++)
            {
                // Across the streak: a soft band, thickest in the middle.
                float ny = Mathf.Abs((y - (HPX - 1) * 0.5f) / ((HPX - 1) * 0.5f));
                float across = Mathf.Clamp01(1f - ny);
                across *= across;
                for (int x = 0; x < WPX; x++)
                {
                    // Along it: full at the head (x = max), gone at the tail.
                    float along = x / (float)(WPX - 1);
                    float a = across * along * along;
                    px[y * WPX + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }
            tex.SetPixels32(px); tex.Apply(false, true);
            _streak = Sprite.Create(tex, new Rect(0, 0, WPX, HPX), new Vector2(0.5f, 0.5f), HPX);
            _streak.name = "rocketStreak";
            return _streak;
        }
        private static Sprite _streak;

        // ---- pool -------------------------------------------------------------

        private static Piece Lease()
        {
            if (Pool.Count < PoolSize) Pool.Add(Build(Pool.Count));
            int count = Pool.Count;
            for (int tried = 0; tried < count; tried++)
            {
                _next = (_next + 1) % count;
                Piece candidate = Pool[_next];
                if (candidate != null && !candidate.Alive) return candidate;
            }
            return null;
        }

        private static Piece Build(int index)
        {
            var go = new GameObject("RocketPiece" + index);
            go.transform.SetParent(_parent, false);
            go.SetActive(false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = SortingOrder;
            return new Piece { Go = go, T = go.transform, R = renderer, Alive = false };
        }

        private static void EnsureRig()
        {
            if (_parent != null && _driver != null) return;
            var holder = new GameObject("~StoryRocketVfx");
            Object.DontDestroyOnLoad(holder);
            _parent = holder.transform;
            _driver = holder.AddComponent<Driver>();
        }

        /// <summary>Advances every live head and fades every streak.</summary>
        private sealed class Driver : MonoBehaviour
        {
            private void Update()
            {
                float now = Time.time;
                float dt = Time.deltaTime;

                for (int h = Heads.Count - 1; h >= 0; h--)
                {
                    Head head = Heads[h];
                    float age = (now - head.Born) / head.Duration;

                    if (age >= 1f + HeadFade / head.Duration) { Heads.RemoveAt(h); continue; }

                    float travelled = Mathf.Min(1f, age) * head.Distance;
                    Vector3 at = head.From + head.Dir * travelled;

                    // Lay a streak behind the head at a fixed cadence rather
                    // than once per frame, so the trail looks the same at any
                    // frame rate.
                    if (age < 1f && now >= head.NextTrail)
                    {
                        head.NextTrail = now + TrailEvery;
                        Emit(at, head, now);
                    }

                    // The head itself is redrawn each frame as a short-lived
                    // piece: one object, replaced, rather than a persistent one
                    // that has to be tracked and cleaned up.
                    if (age < 1f) Emit(at, head, now, true);
                }

                for (int i = 0; i < Pool.Count; i++)
                {
                    Piece piece = Pool[i];
                    if (piece == null || !piece.Alive) continue;

                    float age = (now - piece.Born) / piece.Life;
                    if (age >= 1f || piece.Go == null) { Retire(piece); continue; }

                    Color c = piece.R.color;
                    c.a = piece.IsHead ? 1f - age * age : Mathf.Clamp01(1f - age);
                    piece.R.color = c;

                    if (!piece.IsHead)
                    {
                        // A streak thins as it dies rather than only fading,
                        // so the trail tapers instead of dissolving whole.
                        Vector3 s = piece.T.localScale;
                        s.y = piece.BaseScale * Mathf.Lerp(1f, 0.35f, age);
                        piece.T.localScale = s;
                    }
                }
            }

            private static void Emit(Vector3 at, Head head, float now, bool isHead = false)
            {
                Piece piece = Lease();
                if (piece == null) return;

                Sprite sprite = isHead ? Core() : Streak();
                piece.R.sprite = sprite;
                piece.R.sortingOrder = SortingOrder;
                piece.R.enabled = true;

                Color tint = Color.Lerp(head.Tint, Color.white, isHead ? 0.55f : 0.3f);
                piece.R.color = tint;

                piece.T.position = at + new Vector3(0f, 0f, -0.4f);

                if (isHead)
                {
                    float size = head.CellSize * 1.05f;
                    float scale = size / Mathf.Max(0.0001f, sprite.bounds.size.x);
                    piece.T.localScale = Vector3.one * scale;
                    piece.T.localRotation = Quaternion.identity;
                    piece.BaseScale = scale;
                    piece.Life = 0.05f;          // replaced next frame
                }
                else
                {
                    // Stretched back along the direction of travel.
                    float length = head.CellSize * 2.1f;
                    float thick = head.CellSize * 0.34f;
                    float sx = length / Mathf.Max(0.0001f, sprite.bounds.size.x);
                    float sy = thick / Mathf.Max(0.0001f, sprite.bounds.size.y);
                    piece.T.localScale = new Vector3(sx, sy, 1f);
                    piece.BaseScale = sy;
                    // The texture is bright at its +x end, so the streak is
                    // rotated to point its bright end at the head.
                    float angle = Mathf.Atan2(head.Dir.y, head.Dir.x) * Mathf.Rad2Deg;
                    piece.T.localRotation = Quaternion.Euler(0f, 0f, angle);
                    piece.T.position = at - head.Dir * (length * 0.5f) + new Vector3(0f, 0f, -0.4f);
                    piece.Life = TrailLife;
                }

                piece.IsHead = isHead;
                piece.Born = now;
                piece.Alive = true;
                piece.Go.SetActive(true);
            }

            private static void Retire(Piece piece)
            {
                piece.Alive = false;
                if (piece.Go != null) piece.Go.SetActive(false);
            }
        }
    }
}
