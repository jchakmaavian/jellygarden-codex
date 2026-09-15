// The rocket that actually travels — a port of the browser prototype.
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
// WHY THIS FILE IS A PORT AND NOT A DESIGN
//
// The effect was authored and approved in a browser prototype
// (scratchpad/rocket-lab.html). Four attempts to reproduce it here by choosing
// Unity values that "looked equivalent" all failed, and the last of them failed
// invisibly: every lobe was placed 0.35 units BEHIND the camera and clipped by
// the near plane, so four rounds of tuning size, speed, colour and density were
// spent on an effect that could not have appeared at any setting.
//
// So nothing here is chosen. Every constant is the prototype's, and the two
// drawing models are matched rather than approximated:
//
//   CANVAS                                  HERE
//   createRadialGradient, 3 stops           the stops baked into the sprite
//   3 lobes per puff, offset around a       3 Pieces per Puff, same offsets
//     circle at seed + lb * 2.094
//   r0 = CELL * (0.11 + rand * 0.07)        identical, CELL = the board pitch
//   grow = 2.5 + rand * 1.9                 identical
//   life = 620 + rand * 380 ms              identical
//   opacity 0.5, held to 55% of life        identical
//   a puff emitted every 7ms, 2-3 at once   identical
//   drift applied as dx * age * 0.35        identical
//
// The one thing that CANNOT be copied is absolute size: the prototype's cell is
// about 75 canvas pixels and this board's is about 39 points on a 393-point
// phone. Every length here is therefore expressed as a fraction of the cell,
// exactly as the prototype expresses it, so the picture is the same picture at
// half the physical size.
//
// WHY SPRITE RENDERERS
//
// Every ParticleSystem spawned on this board renders nothing, the kit's own
// pooled flipbook included; that cause is still unfound. A SpriteRenderer
// carrying a generated texture provably renders — StoryShatterVfx's motes were
// captured mid-flight on a real board. Same approach here, same reason.
//
// SORTING AND DEPTH
//
// Gems draw at order 2 and blockers at 4 (Item.prefab, Square.cs). Smoke draws
// at 5 and the flame at 6, so the plume is over the board and the flame over
// its own plume.
//
// Depth is +z, NOT -z. The board camera sits at z -10 looking toward +z, so
// subtracting z moves a piece away from the viewer and past the lens. That is
// the fault described above; it is written down because every instinct says
// "towards the camera" means a smaller z.

using System.Collections.Generic;
using UnityEngine;

namespace JellyGarden.Scripts.Story
{
    public static class StoryRocketVfx
    {
        /// <summary>Off unless a launch asks for it. Flipped by <c>-storyrocket</c>, by the <c>StoryVfx.Rocket</c> pref, or on by
        /// default in a development build. See StoryVfxFlags.</summary>
        public static bool Enabled
        {
            get
            {
                // Cached, because every clear reads this and the answer
                // cannot change inside one run.
                if (_enabled.HasValue) return _enabled.Value;
                _enabled = StoryVfxFlags.On("-storyrocket", "StoryVfx.Rocket");
                return _enabled.Value;
            }
            set { _enabled = value; }
        }
        private static bool? _enabled;

        // ---- the prototype's constants, unchanged ----------------------------
        //
        // Named as the prototype names them so the two can be read side by side.

        /// <summary>
        /// How long the head takes to cross ONE cell. The prototype's `perCell`,
        /// left where it was approved: 18ms.
        ///
        /// This is far faster than anything tried here before — the previous
        /// attempts ran 26, 55, 90, 56 and 105ms, each chosen to make the trail
        /// legible. None of them were the prototype's value, and the trail was
        /// not legible at any of them because it was being drawn behind the
        /// camera. The prototype is legible at 18ms because the PLUME carries
        /// the effect and outlives the head by most of a second; the head is
        /// only the bright tip that starts it.
        /// </summary>
        /// <summary>
        /// REMOVED as a speed control — kept only as the value the five trails
        /// were originally written against.
        ///
        /// Head speed now lives on RocketPlume.PerCellSeconds, per variant, and
        /// both LaunchHead and ReachesAt read it from there. This const is not
        /// referenced by either: it is here so the numbers in
        /// StoryRocketVariant.PlumeOf can be read against the baseline they were
        /// chosen from, and nothing else. Editing it changes nothing on screen.
        /// </summary>
        public const float BaselinePerCellSeconds = 0.018f;

        /// <summary>A puff every 7ms of travel, as in the prototype.</summary>
        private const float PuffEvery = 0.007f;

        /// <summary>Puff life, seconds: the prototype's 620 + rand * 380 ms.</summary>
        private const float LifeMin = 0.620f;
        private const float LifeSpan = 0.380f;

        /// <summary>Lobe radius at birth, as a fraction of a cell: 0.11 + rand * 0.07.</summary>
        private const float R0Min = 0.11f;
        private const float R0Span = 0.07f;

        /// <summary>Final size multiple: 2.5 + rand * 1.9.</summary>
        private const float GrowMin = 2.5f;
        private const float GrowSpan = 1.9f;

        /// <summary>Peak opacity, held to 55% of life then falling to nothing.</summary>
        private const float PuffAlpha = 0.5f;
        private const float HoldUntil = 0.55f;

        /// <summary>Drift is multiplied by the puff's age in seconds and this.</summary>
        private const float DriftScale = 0.35f;

        /// <summary>The three lobes sit this far from the puff's centre, times the radius.</summary>
        private const float LobeOffset = 0.42f;

        /// <summary>2 * PI / 3 — the lobes are evenly spaced around the circle.</summary>
        private const float LobeStep = 2.094f;

        /// <summary>Head radius and flame length, as fractions of a cell.</summary>
        private const float HeadRadius = 0.30f;
        private const float FlameLength = 0.34f;

        /// <summary>The flame cone is squashed across its axis by this.</summary>
        private const float FlameFlatten = 0.42f;

        private const int SmokeOrder = 5;
        private const int FlameOrder = 6;

        /// <summary>
        /// Three Pieces per puff, two or three puffs every 7ms, each living up
        /// to a second: about 1,200 lobes alive per head at the peak, and a
        /// rocket fires two heads.
        ///
        /// A pool that runs dry does not look sparser, it looks like the effect
        /// stuttering — the lesson RepairVfx records for its 220 quads and
        /// StoryShatterVfx for its 900 motes.
        /// </summary>
        private const int PoolSize = 2600;

        /// <summary>
        /// When the head reaches a cell `distance` cells from the origin.
        ///
        /// StoryRocketSweep uses this to stagger the per-tile bursts so a gem
        /// pops as the head arrives. Both sides reading the same function is
        /// the point: a separate constant in each would drift apart the first
        /// time one was tuned.
        /// </summary>
        public static float ReachesAt(int distance)
        {
            // The CURRENT variant's speed, not the file's const.
            //
            // The five callers in StoryRocketSweep stagger their bursts against
            // this, so the two sides have to agree about how fast the head is
            // travelling. When the trails gained their own speeds, this kept
            // answering at the old 0.018 while HeavyExhaust flew at 0.055 —
            // every burst would have fired at a third of the real flight time,
            // landing far ahead of the head, and _window would have closed
            // before the sweep finished. That is precisely the drift the
            // comment above warns a second constant would cause; a stale
            // constant does it just as well.
            return Mathf.Abs(distance) *
                   StoryRocketVariant.PlumeOf(StoryRocketVariant.Current).PerCellSeconds;
        }

        // ---- state ------------------------------------------------------------

        private sealed class Piece
        {
            public GameObject Go;
            public Transform T;
            public SpriteRenderer R;
            public bool Alive;
        }

        /// <summary>
        /// One puff of the prototype's plume. It owns THREE lobes, because that
        /// is what gives the column its cauliflower edge — a single circle per
        /// step reads as a smooth tube, which is exactly what the last attempt
        /// here produced.
        /// </summary>
        private sealed class Puff
        {
            public Vector3 At;
            public Vector3 Drift;
            public float R0;
            public float Grow;
            public float Born;
            public float Life;
            public float Seed;

            // Per puff, not per file. Two heads fire per rocket and their smoke
            // outlives them by most of a second, so a variant chosen while a
            // plume is still in the air would otherwise re-opacity the puffs
            // already drawn behind a rocket that has left the board.
            public float Alpha;

            public Piece A, B, C;
        }

        private sealed class Head
        {
            public Vector3 From;
            public Vector3 Dir;
            public Vector3 Across;
            public float Distance;
            public float CellSize;
            public Color Tint;
            public float Born;
            public float Duration;
            public float NextPuff;

            // Where the head was when it last laid smoke. An emission covers
            // the SEGMENT from here to the head's current point rather than a
            // single dot under the nose: PuffEvery is seconds of TRAVEL, and on
            // a slow variant one interval can outlast a frame, so spawning only
            // at the nose leaves gaps the eye reads as the trail lagging.
            public Vector3 LastPuffAt;
            public bool HasLastPuff;

            // The trail this head is laying, read once when it launched. See
            // Puff.Alpha for why it is carried rather than looked up per frame.
            public RocketPlume Plume;

            public Piece Tip;
            public Piece Flame;
        }

        private static readonly List<Piece> Pool = new List<Piece>(PoolSize);
        private static readonly List<Puff> Puffs = new List<Puff>(512);
        private static readonly List<Head> Heads = new List<Head>(8);
        private static Transform _parent;
        private static Driver _driver;
        private static int _next;

        // ---- firing -----------------------------------------------------------

        /// <summary>
        /// Fire the two heads for one rocket.
        ///
        /// `cellsEachWay` is how far the line runs — the board's width or
        /// height — so the head leaves the board rather than stopping short on
        /// a narrow level.
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
            // Resolved into a local BEFORE the initialiser, because Duration is
            // computed from it and an object initialiser cannot read a field it
            // is still assigning.
            RocketPlume plume = StoryRocketVariant.PlumeOf(StoryRocketVariant.Current);

            Heads.Add(new Head
            {
                From = from,
                Dir = direction,
                Across = new Vector3(-direction.y, direction.x, 0f),
                Distance = distance,
                CellSize = cellSize,
                Tint = tint,
                Born = Time.time,
                // The VARIANT's speed, not the file's const. This line read
                // PerCellSeconds until the trails needed their own speeds, and
                // leaving it would have made every plume tuning look like it had
                // no effect on how fast the rocket crossed the board.
                Duration = (distance / Mathf.Max(0.0001f, cellSize)) * plume.PerCellSeconds,
                NextPuff = 0f,
                Plume = plume,
            });
        }

        /// <summary>
        /// Lay one puff at the head, exactly as the prototype does: the centre
        /// jittered a tenth of a cell along the axis and a third across it, and
        /// the drift set mostly ACROSS the line of travel so the plume spreads
        /// sideways as it is left behind rather than chasing the head.
        /// </summary>
        private static void Spawn(Vector3 at, Head head, float now)
        {
            var puff = new Puff
            {
                At = at
                    + head.Across * ((Random.value - 0.5f) * head.CellSize * 0.34f)
                    + head.Dir * ((Random.value - 0.5f) * head.CellSize * 0.10f),
                // Scaled by the variant's Drift against the shipped 0.35, so a
                // tight contrail hugs the flight line and a launch plume spreads.
                Drift = (head.Across * ((Random.value - 0.5f) * 0.34f)
                      + head.Dir * ((Random.value - 0.5f) * 0.10f - 0.06f))
                      * (head.Plume.Drift / DriftScale),
                R0 = head.CellSize * (R0Min + Random.value * R0Span),
                Grow = head.Plume.GrowMin + Random.value * head.Plume.GrowSpan,
                Born = now,
                Life = head.Plume.LifeMin + Random.value * head.Plume.LifeSpan,
                Seed = Random.value * 6.28f,
                Alpha = head.Plume.Alpha,
            };

            puff.A = Lease();
            puff.B = Lease();
            puff.C = Lease();
            if (puff.A == null || puff.B == null || puff.C == null)
            {
                Release(puff.A); Release(puff.B); Release(puff.C);
                return;
            }

            Dress(puff.A); Dress(puff.B); Dress(puff.C);
            Puffs.Add(puff);
        }

        private static void Dress(Piece piece)
        {
            piece.R.sprite = Smoke();
            piece.R.sortingOrder = SmokeOrder;
            piece.R.enabled = true;
            piece.T.localRotation = Quaternion.identity;
            piece.Go.SetActive(true);
        }

        // ---- the three sprites, baked from the prototype's gradients ----------

        /// <summary>
        /// The plume lobe.
        ///
        /// The prototype fills each lobe with a canvas radial gradient:
        ///
        ///     0.0  rgba(250,249,252,1)
        ///     0.5  rgba(226,224,232,0.72)
        ///     1.0  rgba(196,193,206,0)
        ///
        /// Those three stops ARE the look. The previous texture here was a flat
        /// core out to 0.45 of the radius and then a squared falloff, which has
        /// no shoulder at the half-radius — overlapping lobes merged into one
        /// flat wash instead of reading as separate billows. This bakes the
        /// prototype's stops, interpolated exactly as canvas interpolates them,
        /// so a lobe drawn here is the lobe drawn there.
        /// </summary>
        private static Sprite Smoke()
        {
            if (_smoke != null) return _smoke;

            const int N = 128;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false)
            {
                name = "StoryRocketSmoke",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };

            var px = new Color32[N * N];
            float half = (N - 1) * 0.5f;

            var inner = new Color(250f / 255f, 249f / 255f, 252f / 255f, 1f);
            var middle = new Color(226f / 255f, 224f / 255f, 232f / 255f, 0.72f);
            var outer = new Color(196f / 255f, 193f / 255f, 206f / 255f, 0f);

            for (int y = 0; y < N; y++)
            {
                for (int x = 0; x < N; x++)
                {
                    float dx = (x - half) / half, dy = (y - half) / half;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);

                    Color c;
                    if (d >= 1f) c = outer;
                    else if (d < 0.5f) c = Color.Lerp(inner, middle, d / 0.5f);
                    else c = Color.Lerp(middle, outer, (d - 0.5f) / 0.5f);

                    px[y * N + x] = c;
                }
            }

            tex.SetPixels32(px);
            tex.Apply(false, true);
            _smoke = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), N);
            _smoke.name = "rocketSmoke";
            return _smoke;
        }
        private static Sprite _smoke;

        /// <summary>
        /// The head: the prototype's four stops, white through pale gold to
        /// orange and out to nothing.
        ///
        ///     0.00  #ffffff
        ///     0.30  #fff0bd
        ///     0.62  #ff9a3c
        ///     1.00  rgba(255,70,0,0)
        ///
        /// The prototype also paints a hard white disc at 0.34 of the radius on
        /// top; that is baked in here rather than drawn as a second object.
        /// </summary>
        private static Sprite Core()
        {
            if (_core != null) return _core;

            const int N = 128;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false)
            {
                name = "StoryRocketCore",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };

            var px = new Color32[N * N];
            float half = (N - 1) * 0.5f;

            Color white = Color.white;
            var gold = new Color(1f, 240f / 255f, 189f / 255f, 1f);
            var orange = new Color(1f, 154f / 255f, 60f / 255f, 1f);
            var gone = new Color(1f, 70f / 255f, 0f, 0f);

            for (int y = 0; y < N; y++)
            {
                for (int x = 0; x < N; x++)
                {
                    float dx = (x - half) / half, dy = (y - half) / half;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);

                    Color c;
                    if (d >= 1f) c = gone;
                    else if (d < 0.30f) c = Color.Lerp(white, gold, d / 0.30f);
                    else if (d < 0.62f) c = Color.Lerp(gold, orange, (d - 0.30f) / 0.32f);
                    else c = Color.Lerp(orange, gone, (d - 0.62f) / 0.38f);

                    // The prototype's solid white centre, drawn over the top.
                    if (d < 0.34f) c = white;

                    px[y * N + x] = c;
                }
            }

            tex.SetPixels32(px);
            tex.Apply(false, true);
            _core = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), N);
            _core.name = "rocketCore";
            return _core;
        }
        private static Sprite _core;

        /// <summary>
        /// The flame cone behind the tip.
        ///
        ///     0.00  rgba(255,240,190,0.95)
        ///     0.55  rgba(255,140,50,0.55)
        ///     1.00  rgba(255,70,0,0)
        ///
        /// The prototype squashes this to 0.42 across its axis; here that is
        /// the transform's y scale, so one round texture serves both axes.
        /// </summary>
        private static Sprite Flame()
        {
            if (_flame != null) return _flame;

            const int N = 96;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false)
            {
                name = "StoryRocketFlame",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };

            var px = new Color32[N * N];
            float half = (N - 1) * 0.5f;

            var hot = new Color(1f, 240f / 255f, 190f / 255f, 0.95f);
            var mid = new Color(1f, 140f / 255f, 50f / 255f, 0.55f);
            var gone = new Color(1f, 70f / 255f, 0f, 0f);

            for (int y = 0; y < N; y++)
            {
                for (int x = 0; x < N; x++)
                {
                    float dx = (x - half) / half, dy = (y - half) / half;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);

                    Color c;
                    if (d >= 1f) c = gone;
                    else if (d < 0.55f) c = Color.Lerp(hot, mid, d / 0.55f);
                    else c = Color.Lerp(mid, gone, (d - 0.55f) / 0.45f);

                    px[y * N + x] = c;
                }
            }

            tex.SetPixels32(px);
            tex.Apply(false, true);
            _flame = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), N);
            _flame.name = "rocketFlame";
            return _flame;
        }
        private static Sprite _flame;

        // ---- pool ---------------------------------------------------------------

        private static Piece Lease()
        {
            // Grow first, then look. Wrapping the cursor modulo PoolSize and
            // then indexing Pool with it throws the moment the cursor runs past
            // however many slots have actually been built — List.Add appends at
            // Count, not at the cursor.
            if (Pool.Count < PoolSize) Pool.Add(Build(Pool.Count));

            int count = Pool.Count;
            for (int tried = 0; tried < count; tried++)
            {
                _next = (_next + 1) % count;
                Piece candidate = Pool[_next];
                if (candidate != null && !candidate.Alive)
                {
                    candidate.Alive = true;
                    return candidate;
                }
            }
            return null;
        }

        private static void Release(Piece piece)
        {
            if (piece == null) return;
            piece.Alive = false;
            if (piece.Go != null) piece.Go.SetActive(false);
        }

        private static Piece Build(int index)
        {
            var go = new GameObject("RocketPiece" + index);
            go.transform.SetParent(_parent, false);
            go.SetActive(false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = SmokeOrder;
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

        // ---- the frame loop -----------------------------------------------------

        /// <summary>
        /// Runs the prototype's own update, in its own order: advance each head
        /// and lay its puffs, then draw every live puff, then the flame and tip
        /// on top of the smoke they made.
        /// </summary>
        private sealed class Driver : MonoBehaviour
        {
            private void LateUpdate()
            {
                float now = Time.time;

                for (int h = Heads.Count - 1; h >= 0; h--)
                {
                    Head head = Heads[h];
                    float age = (now - head.Born) / head.Duration;

                    if (age >= 1f)
                    {
                        // The head is gone the moment it leaves the board; its
                        // smoke lives on by itself.
                        Release(head.Tip); Release(head.Flame);
                        head.Tip = null; head.Flame = null;
                        Heads.RemoveAt(h);
                        continue;
                    }

                    Vector3 at = head.From + head.Dir * (age * head.Distance);

                    // Two or three lobes per emission, on a fixed cadence so
                    // the plume has the same density whatever the frame rate.
                    //
                    // The lobes are laid ALONG the segment the head crossed
                    // since the last emission, not all at the nose. Same count,
                    // same density, same tuning — but a continuous line instead
                    // of dots at the sampling points. HeavyExhaust emits every
                    // 0.022s of travel, longer than a frame at 60fps, so
                    // spawning at the nose alone left the head visibly ahead of
                    // its own smoke between ticks.
                    if (now >= head.NextPuff)
                    {
                        head.NextPuff = now + head.Plume.PuffEvery;
                        int puffs = Random.value < 0.5f ? 3 : 2;

                        // The first emission has no previous point to reach back
                        // to, so it lays at the nose exactly as before.
                        Vector3 since = head.HasLastPuff ? head.LastPuffAt : at;

                        for (int i = 0; i < puffs; i++)
                        {
                            // Spread across the segment rather than bunched at
                            // either end: (i + 1) / puffs walks the lobes up to
                            // and including the nose.
                            float along = (i + 1f) / puffs;
                            Spawn(Vector3.Lerp(since, at, along), head, now);
                        }

                        head.LastPuffAt = at;
                        head.HasLastPuff = true;
                    }

                    DrawHead(head, at);
                }

                for (int p = Puffs.Count - 1; p >= 0; p--)
                {
                    Puff puff = Puffs[p];
                    float seconds = now - puff.Born;
                    float pa = seconds / puff.Life;

                    if (pa >= 1f)
                    {
                        Release(puff.A); Release(puff.B); Release(puff.C);
                        Puffs.RemoveAt(p);
                        continue;
                    }

                    // Grows fast at first, then settles — expanding gas, not a
                    // linear ramp.
                    float gr = 1f + (puff.Grow - 1f) * Mathf.Pow(pa, 0.55f);
                    float rad = puff.R0 * gr;
                    Vector3 centre = puff.At + puff.Drift * (seconds * DriftScale);

                    // Holds opacity through most of its life, then goes. The
                    // reference plume is still dense a long way behind the head.
                    float op = pa < HoldUntil
                        ? puff.Alpha
                        : puff.Alpha * (1f - (pa - HoldUntil) / (1f - HoldUntil));

                    // Young smoke is still lit by the flame; old smoke is plain.
                    float warm = Mathf.Max(0f, 1f - pa * 3.2f);
                    Color tint = warm > 0.25f
                        ? new Color(1f, (238f - warm * 30f) / 255f, (214f - warm * 80f) / 255f, op)
                        : new Color(1f, 1f, 1f, op);

                    Lobe(puff.A, puff, 0, centre, rad, tint);
                    Lobe(puff.B, puff, 1, centre, rad, tint);
                    Lobe(puff.C, puff, 2, centre, rad, tint);
                }
            }

            /// <summary>
            /// One of a puff's three lobes, placed exactly where the prototype
            /// places it: around a circle of 0.42 radii, each lobe's own radius
            /// varying with the puff's seed so no two puffs are the same shape.
            /// </summary>
            private static void Lobe(Piece piece, Puff puff, int index,
                                     Vector3 centre, float rad, Color tint)
            {
                if (piece == null || piece.Go == null) return;

                float angle = puff.Seed + index * LobeStep;
                Vector3 at = centre + new Vector3(
                    Mathf.Cos(angle) * rad * LobeOffset,
                    Mathf.Sin(angle) * rad * LobeOffset,
                    0f);

                float lr = rad * (0.62f + 0.16f * Mathf.Sin(puff.Seed + index));

                piece.T.position = new Vector3(at.x, at.y, at.z + 0.35f);
                // The sprite is one unit across at its own PPU, so the diameter
                // is the scale: a radius of lr is a scale of 2 * lr.
                piece.T.localScale = Vector3.one * (lr * 2f);
                piece.R.color = tint;

            }

            /// <summary>
            /// The tip and its flame cone, drawn over the smoke rather than
            /// under it — the prototype draws the plume first for exactly this
            /// reason.
            /// </summary>
            private static void DrawHead(Head head, Vector3 at)
            {
                if (head.Tip == null)
                {
                    head.Tip = Lease();
                    if (head.Tip != null)
                    {
                        head.Tip.R.sprite = Core();
                        head.Tip.R.sortingOrder = FlameOrder;
                        head.Tip.R.enabled = true;
                        head.Tip.Go.SetActive(true);
                    }
                }

                if (head.Flame == null)
                {
                    head.Flame = Lease();
                    if (head.Flame != null)
                    {
                        head.Flame.R.sprite = Flame();
                        head.Flame.R.sortingOrder = FlameOrder - 1;
                        head.Flame.R.enabled = true;
                        head.Flame.Go.SetActive(true);
                    }
                }

                if (head.Flame != null)
                {
                    float fl = head.CellSize * FlameLength;
                    Vector3 fx = at - head.Dir * (fl * 0.5f);
                    head.Flame.T.position = new Vector3(fx.x, fx.y, fx.z + 0.36f);
                    // Squashed across the direction of travel, so the cone is
                    // long down the line and thin across it.
                    float angle = Mathf.Atan2(head.Dir.y, head.Dir.x) * Mathf.Rad2Deg;
                    head.Flame.T.localRotation = Quaternion.Euler(0f, 0f, angle);
                    head.Flame.T.localScale = new Vector3(fl * 2f, fl * 2f * FlameFlatten, 1f);
                    head.Flame.R.color = Color.white;
                }

                if (head.Tip != null)
                {
                    float hr = head.CellSize * HeadRadius;
                    head.Tip.T.position = new Vector3(at.x, at.y, at.z + 0.4f);
                    head.Tip.T.localScale = Vector3.one * (hr * 2f);
                    head.Tip.R.color = Color.white;
                }
            }
        }
    }
}
