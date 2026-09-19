// The colour bomb: charge, mark, wave, then each target pops as the wave
// reaches it.
//
// THE SEQUENCE
//
//   CHARGE   0.30s  a copy of the bomb rises toward the camera and spins.
//   LINES    0.18s  a beam is drawn from the bomb to every gem of the colour,
//                   and each beam is HELD until its target pops.
//   WAVE     0.70s  a ring in the bomb's own colour expands across the board.
//   POPS            a target detonates at the moment the ring passes over it,
//                   so a cell two away goes long before one six away.
//
// About 1.3 seconds from lift to the last pop.
//
// WHY THE SPINNING BOMB IS A COPY AND NOT THE GEM
//
// Item.CheckChocoBomb calls DestroyColor(...) and then item1.DestroyItem() in
// the same synchronous block, so the real bomb gem is gone within a frame of
// this being called. Nothing can be animated on it. This draws its own sprite
// at the SQUARE's position instead — the square, not the gem, because
// StoryRocketSweep's header records that Item.Switching lerps a gem's transform
// mid-swap, and a colour bomb fires from exactly such a swap.
//
// WHY THE GEMS ARE ALREADY DEAD WHEN THEY "EXPLODE"
//
// DestroyColor clears every matching gem in one frame, and that is a contract
// rather than an accident: everything downstream of the kit's destroy path
// assumes the line is clear when the call returns. StoryRocketSweep's header
// records what happened when an earlier effect staggered those destroys — the
// cascade waited on a clear that never completed and the board hung for twenty
// seconds.
//
// So the kit clears them all at once and this paints the explosions afterwards.
// The gems vanish at frame zero while their blasts arrive over the next second.
// That mismatch is deliberate and it is the safe half of the trade; the
// alternative is touching the kit's timing, which has already cost this project
// a hung board once.

using System.Collections.Generic;
using UnityEngine;

namespace JellyGarden.Scripts.Story
{
    public static class StoryColorBombVfx
    {
        /// <summary>
        /// Off unless asked for. Flipped by <c>-storycolorbomb</c>, the
        /// <c>StoryVfx.ColorBomb</c> pref, or on by default — see StoryVfxFlags.
        /// </summary>
        public static bool Enabled
        {
            get
            {
                if (_enabled.HasValue) return _enabled.Value;
                _enabled = StoryVfxFlags.On("-storycolorbomb", "StoryVfx.ColorBomb");
                return _enabled.Value;
            }
            set { _enabled = value; }
        }
        private static bool? _enabled;

        private const float ChargeTime = 0.30f;
        // LineTime is GONE. It was 0.18s — how long every beam took to be drawn
        // when they were all drawn at once — and the bolts replaced that with
        // BoltEvery per target. It is not simply unused: a private const raises
        // no warning, so leaving it would have put a plausible-looking "how long
        // does the marking take" number in this file that nothing reads. That is
        // precisely how StoryRocketVfx.PerCellSeconds came to be tuned for a
        // build that ignored it.
        //
        // The marking now takes ChargeTime + (targets - 1) * BoltEvery, and the
        // run carries that as BoltsDoneAt.
        private const float WaveTime = 0.70f;

        /// <summary>How far the bomb lifts toward the camera while charging.</summary>
        private const float LiftZ = 0.9f;

        /// <summary>Turns per second while charging. Fast enough to read as a spin-up.</summary>
        private const float SpinSpeed = 900f;

        /// <summary>How far the ring travels, in cells, before the run ends.</summary>
        private const float WaveReach = 9f;

        /// <summary>
        /// Beam thickness as a fraction of a cell.
        ///
        /// Named rather than inline because it is the one number in this file
        /// most likely to be tuned again: a colour bomb can draw twenty-five of
        /// these at once, all converging on the same cell, so the width that
        /// reads well for one line is far too heavy for a full board.
        /// </summary>
        /// <summary>
        /// Bolt thickness at the BOMB end, as a fraction of a cell.
        ///
        /// This went 0.09, 0.035, 0.018, 0.05 across four builds while the
        /// owner kept saying the lines were wrong — and thinness was never the
        /// answer. The reference is a thick GLOWING BOLT with a white core and a
        /// coloured halo, not a thin opaque thread: a flat bar cannot look like
        /// that at any width. The sprite changed instead, and the width went up.
        /// </summary>
        private const float BoltWidth = 0.30f;

        /// <summary>How much narrower a bolt is at the target end. The taper.</summary>
        private const float BoltTaper = 0.35f;

        /// <summary>How long one bolt lives once fired.</summary>
        private const float BoltLife = 0.26f;

        /// <summary>
        /// Seconds between one bolt firing and the next.
        ///
        /// The first version drew every bolt at once and held them all until the
        /// wave arrived — twenty-five permanent threads, a static fan. The
        /// reference frames show three to six alive at any moment, appearing and
        /// fading while others start. At 0.055s with a 0.26s life, about five
        /// overlap, which is what makes it read as crackling rather than as a
        /// diagram.
        /// </summary>
        private const float BoltEvery = 0.055f;

        private const int WaveOrder = 5;
        private const int LineOrder = 6;
        private const int BombOrder = 8;

        // A colour bomb can mark 25 targets on a nine-wide board: one line each,
        // plus the bomb and the ring. Sixty-four leaves room for a second bomb
        // fired in the same cascade.
        private const int PoolSize = 64;

        private sealed class Piece
        {
            public GameObject Go;
            public Transform T;
            public SpriteRenderer R;
            public bool Alive;
        }

        private sealed class Target
        {
            public Vector3 At;
            public Piece Line;
            /// <summary>Seconds from the wave starting until the ring reaches it.</summary>
            public float ReachedAt;
            public bool Popped;

            /// <summary>When this target's bolt fires, measured from the run's start.</summary>
            public float BoltAt;

            /// <summary>Leased on firing and released when the bolt has faded.</summary>
            public bool BoltDone;
        }

        private sealed class Run
        {
            public Vector3 At;
            public float Cell;
            public float Born;
            public Color Tint;
            public Piece Bomb;
            public Piece Wave;
            public readonly List<Target> Targets = new List<Target>(32);

            /// <summary>
            /// When the last bolt has been fired, measured from the run's start.
            /// The wave is released here rather than at a fixed time, because
            /// the marking now takes as long as there are targets to mark.
            /// </summary>
            public float BoltsDoneAt;
        }

        private static readonly List<Piece> Pool = new List<Piece>(PoolSize);
        private static readonly List<Run> Runs = new List<Run>(4);
        private static Transform _parent;
        private static Driver _driver;
        private static int _next;
        private static int _said;

        /// <summary>
        /// Start the sequence for one colour bomb.
        ///
        /// Called from Item.DestroyColor BEFORE it clears, so the target
        /// positions are read while the gems are still on the board.
        /// </summary>
        public static void Fire(Vector3 at, Color tint, List<Vector3> targets, Sprite bombSprite)
        {
            if (!Enabled || !Application.isPlaying) return;
            if (targets == null || targets.Count == 0) return;

            EnsureRig();

            float cell = StoryBoardScale.Pitch;

            var run = new Run
            {
                At = at,
                Cell = cell,
                Born = Time.time,
                Tint = tint,
            };

            run.Bomb = Lease();
            if (run.Bomb != null)
            {
                Dress(run.Bomb, bombSprite != null ? bombSprite : Dot(), BombOrder);
                run.Bomb.R.color = Color.white;
            }

            run.Wave = Lease();
            if (run.Wave != null)
            {
                Dress(run.Wave, Ring(), WaveOrder);
                run.Wave.R.enabled = false;
            }

            // Nearest first, so the bolts genuinely sweep outward from the bomb.
            // FindGameObjectsWithTag returns them in scene order, which is
            // arbitrary — without this the stagger below fires at random cells
            // and reads as noise rather than as the bomb reaching further and
            // further out.
            targets.Sort((a, b) =>
                (a - at).sqrMagnitude.CompareTo((b - at).sqrMagnitude));

            for (int i = 0; i < targets.Count; i++)
            {
                Piece line = Lease();
                if (line == null) break;

                DressGlowing(line, Beam(), LineOrder);
                line.R.enabled = false;

                // When the ring gets there. The ring travels WaveReach cells in
                // WaveTime, so a target's arrival is its distance over that
                // speed — which is what makes a near cell pop long before a far
                // one rather than everything going at once.
                float cells = Vector3.Distance(at, targets[i]) / Mathf.Max(0.0001f, cell);
                float speed = WaveReach / WaveTime;

                run.Targets.Add(new Target
                {
                    At = targets[i],
                    Line = line,
                    ReachedAt = cells / speed,
                    // Staggered, nearest first, so the bolts sweep outward from
                    // the bomb rather than all appearing together.
                    // From ZERO, not after the charge.
                    //
                    // The bolts used to wait out ChargeTime, so the first third
                    // of a second was a spinning bomb and nothing else — the
                    // lines arrived last when they should arrive first. The bomb
                    // still rises and spins; it now does so WHILE the bolts
                    // strike rather than before them.
                    BoltAt = i * BoltEvery,
                });
            }

            // The last bolt fires at ChargeTime + (count-1) * BoltEvery, and the
            // wave waits for it to strike out before it is released.
            run.BoltsDoneAt = run.Targets.Count > 0
                ? (run.Targets.Count - 1) * BoltEvery + BoltLife * 0.3f
                : ChargeTime;

            Runs.Add(run);

            _said++;
            if (_said <= 5)
                Debug.Log(string.Format(
                    "[ColorBomb] #{0} at {1} | {2} target(s), bolts done {3:F0}ms, " +
                    "wave {4:F0}ms, last pop {5:F0}ms",
                    _said, at.ToString("F2"), run.Targets.Count,
                    run.BoltsDoneAt * 1000f, WaveTime * 1000f,
                    (run.BoltsDoneAt + LastReach(run)) * 1000f));
        }

        private static float LastReach(Run run)
        {
            float last = 0f;
            for (int i = 0; i < run.Targets.Count; i++)
                if (run.Targets[i].ReachedAt > last) last = run.Targets[i].ReachedAt;
            return last;
        }

        private sealed class Driver : MonoBehaviour
        {
            private void LateUpdate()
            {
                float now = Time.time;

                for (int r = Runs.Count - 1; r >= 0; r--)
                {
                    Run run = Runs[r];
                    float age = now - run.Born;

                    // --- CHARGE: the bomb lifts toward the camera and spins ---
                    if (run.Bomb != null)
                    {
                        float charge = Mathf.Clamp01(age / ChargeTime);

                        // Depth is +z toward the lens on this board — the camera
                        // sits at -10 looking toward +z, so SUBTRACTING z moves a
                        // piece away and past it. StoryRocketVfx's header records
                        // that trap; every instinct says the opposite.
                        float lift = -LiftZ * Mathf.Pow(charge, 0.6f);

                        run.Bomb.T.position = run.At + new Vector3(0f, 0f, lift);
                        run.Bomb.T.localRotation =
                            Quaternion.Euler(0f, 0f, age * SpinSpeed);

                        // Grows a little as it winds up, then goes when the wave
                        // is released.
                        float grow = run.Cell * (0.55f + 0.30f * charge);
                        run.Bomb.T.localScale = Vector3.one * grow;

                        // The bomb holds until its last bolt is away, then goes.
                        float fade = age < run.BoltsDoneAt
                            ? 1f
                            : Mathf.Clamp01(1f - (age - run.BoltsDoneAt) / 0.18f);
                        run.Bomb.R.color = new Color(1f, 1f, 1f, fade);
                    }

                    // --- BOLTS: each fires, lives briefly, fades ---
                    //
                    // Not twenty-five threads held until the wave arrives, which
                    // is what this drew first and what read as a static fan. The
                    // reference has a handful alive at any instant, appearing and
                    // going while others start.
                    {
                        for (int i = 0; i < run.Targets.Count; i++)
                        {
                            Target target = run.Targets[i];
                            if (target.Line == null || target.Popped || target.BoltDone) continue;

                            float since = age - target.BoltAt;
                            if (since < 0f) { target.Line.R.enabled = false; continue; }

                            if (since >= BoltLife)
                            {
                                // Hand the piece back the moment the bolt is
                                // spent: a colour bomb can have twenty-five
                                // targets against a pool of sixty-four, and
                                // holding them all would starve the wave and the
                                // bomb body of pieces.
                                Release(target.Line);
                                target.Line = null;
                                target.BoltDone = true;
                                continue;
                            }

                            // Strikes out fast, then holds while it fades.
                            float strike = Mathf.Clamp01(since / (BoltLife * 0.28f));
                            float drawn = 1f - (1f - strike) * (1f - strike);
                            float fade = 1f - Mathf.Clamp01(
                                (since - BoltLife * 0.35f) / (BoltLife * 0.65f));

                            // FROM THE BOMB, not from the square under it.
                            //
                            // run.At is the cell on the board plane, but the
                            // bomb copy is lifted toward the camera in -z while
                            // it charges. Starting the beam at run.At therefore
                            // drew it from the board while the bomb floated
                            // above — the two visibly came apart, which is the
                            // "not connected" fault. The origin now tracks the
                            // bomb's live transform, so the line stays welded to
                            // it for as long as it is lifted.
                            Vector3 from = run.Bomb != null
                                ? run.Bomb.T.position
                                : run.At;

                            Vector3 delta = target.At - from;
                            float length = delta.magnitude * drawn;
                            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

                            target.Line.R.enabled = true;
                            // Toward the camera, which is -z on this board: the
                            // lens sits at z -10 looking toward +z, so a positive
                            // nudge pushes a piece AWAY and behind the board.
                            // StoryRocketVfx's header records the same trap.
                            target.Line.T.position = from
                                + delta.normalized * (length * 0.5f)
                                + new Vector3(0f, 0f, -0.05f);
                            target.Line.T.localRotation = Quaternion.Euler(0f, 0f, angle);
                            // Tapered: the sprite's own gradient narrows toward
                            // its right-hand end, and the quad is scaled to the
                            // bomb-end width. x is the exact bomb-to-gem
                            // distance, so the bolt still ENDS on its target.
                            target.Line.T.localScale =
                                new Vector3(length, run.Cell * BoltWidth, 1f);

                            // White core, gem-coloured halo — the sprite carries
                            // the shape, this carries the light. Kept bright so
                            // an additive material blooms rather than tinting.
                            Color glow = Color.Lerp(run.Tint, Color.white, 0.55f);
                            target.Line.R.color = new Color(
                                glow.r, glow.g, glow.b, fade);
                        }
                    }

                    // --- WAVE: a ring in the bomb's colour, and the pops ---
                    // The wave follows the BOLTS, not a fixed line time.
                    //
                    // This was ChargeTime + LineTime — 0.48s — which was right
                    // while every beam was drawn at once over 180ms. The bolts
                    // now fire one at a time, so twenty-five targets take
                    // 25 * BoltEvery = 1.375s to mark. Leaving the wave on the
                    // old constant sent it out while bolts were still firing:
                    // it would reach and pop a gem whose bolt had not been drawn
                    // yet, which is the sequence backwards.
                    float waveStart = run.BoltsDoneAt;
                    if (age >= waveStart)
                    {
                        float since = age - waveStart;

                        if (run.Wave != null)
                        {
                            float t = Mathf.Clamp01(since / WaveTime);
                            float reach = run.Cell * WaveReach * t;

                            run.Wave.R.enabled = true;
                            run.Wave.T.position = run.At + new Vector3(0f, 0f, -0.3f);
                            run.Wave.T.localScale = Vector3.one * Mathf.Max(0.001f, reach);
                            run.Wave.R.color = new Color(
                                run.Tint.r, run.Tint.g, run.Tint.b, (1f - t) * 0.7f);
                        }

                        for (int i = 0; i < run.Targets.Count; i++)
                        {
                            Target target = run.Targets[i];
                            if (target.Popped || since < target.ReachedAt) continue;

                            target.Popped = true;
                            Release(target.Line);
                            target.Line = null;

                            // The explosion is the one already tuned for the
                            // package bomb, so a colour bomb's pops and an
                            // ordinary bomb look like the same game.
                            //
                            // WITHOUT the board recoil. A colour bomb pops up to
                            // twenty-five targets across a second, and a punch
                            // each compounds into the board visibly zooming —
                            // the recoil is a full stop for one explosion, not a
                            // per-gem garnish.
                            StoryBombVfx.Blow(target.At, run.Tint,
                                punchBoard: false, royal: false);
                        }
                    }

                    // Done when the ring has run and every target has popped.
                    if (age >= waveStart + WaveTime + 0.1f)
                    {
                        Retire(run);
                        Runs.RemoveAt(r);
                    }
                }
            }

            private static void Retire(Run run)
            {
                Release(run.Bomb);
                Release(run.Wave);
                for (int i = 0; i < run.Targets.Count; i++) Release(run.Targets[i].Line);
            }
        }

        private static void Dress(Piece piece, Sprite sprite, int order)
        {
            piece.R.sprite = sprite;
            piece.R.sortingOrder = order;
            piece.R.enabled = true;
            piece.T.localRotation = Quaternion.identity;
            piece.Go.SetActive(true);
        }

        /// <summary>
        /// Dress a piece so it GLOWS: the same as Dress, plus an additive
        /// material so the bolt brightens the board under it instead of
        /// covering it.
        ///
        /// Additive is what makes the reference's bolts look like light. The
        /// fallback matters though: nothing in this project has put an additive
        /// material on a SpriteRenderer before — StoryBlastVfx uses it on
        /// ParticleSystems, and its own header records that particles may never
        /// have rendered on this board at all. So a missing shader leaves the
        /// renderer's default material in place and the bolt still draws, just
        /// without the bloom.
        /// </summary>
        private static void DressGlowing(Piece piece, Sprite sprite, int order)
        {
            Dress(piece, sprite, order);

            Material additive = Additive();
            if (additive != null) piece.R.material = additive;
        }

        private static Material Additive()
        {
            if (_additiveTried) return _additive;
            _additiveTried = true;

            // The same shader the kit's own board particles use, so this
            // inherits whatever render pipeline the project is on.
            Shader shader = Shader.Find("Particles/Additive");
            if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Additive");
            if (shader == null) return null;

            _additive = new Material(shader) { name = "StoryColorBombAdditive" };
            return _additive;
        }
        private static Material _additive;
        private static bool _additiveTried;

        private static Piece Lease()
        {
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
            var go = new GameObject("ColorBomb" + index);
            go.transform.SetParent(_parent, false);
            go.SetActive(false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = LineOrder;
            return new Piece { Go = go, T = go.transform, R = renderer, Alive = false };
        }

        private static void EnsureRig()
        {
            if (_parent != null && _driver != null) return;

            var holder = new GameObject("~StoryColorBombVfx");
            Object.DontDestroyOnLoad(holder);
            _parent = holder.transform;
            _driver = holder.AddComponent<Driver>();
        }

        /// <summary>Drop every run. Called on a scene load.</summary>
        public static void Reset()
        {
            for (int i = 0; i < Runs.Count; i++)
            {
                Release(Runs[i].Bomb);
                Release(Runs[i].Wave);
                for (int t = 0; t < Runs[i].Targets.Count; t++)
                    Release(Runs[i].Targets[t].Line);
            }
            Runs.Clear();
        }

        // ---- sprites ----------------------------------------------------------

        /// <summary>A flat white bar, stretched into a targeting beam.</summary>
        private static Sprite Beam()
        {
            if (_beam != null) return _beam;

            const int N = 16;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false)
            {
                name = "StoryColorBombBeam",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };

            var px = new Color32[N * N];
            float half = (N - 1) * 0.5f;

            for (int y = 0; y < N; y++)
            {
                // Across the bolt: a hard white core with a soft skirt. The
                // core is what reads as the lightning itself and the skirt is
                // the halo around it — a single linear falloff gives neither,
                // which is why the flat bar this replaced looked like a drawn
                // rectangle however thin it was made.
                float dy = Mathf.Abs(y - half) / half;
                float core = Mathf.Clamp01(1f - dy / 0.28f);
                float skirt = Mathf.Clamp01(1f - dy);
                float across = Mathf.Clamp01(core + skirt * skirt * 0.55f);

                for (int x = 0; x < N; x++)
                {
                    // Along the bolt: full width at the bomb end (x = 0),
                    // narrowing toward the target. Baking the taper into the
                    // texture rather than the transform means one quad still
                    // draws it — a tapered quad is not something a
                    // SpriteRenderer can express.
                    float along = x / (float)(N - 1);
                    float width = Mathf.Lerp(1f, BoltTaper, along);

                    float a = dy <= width ? across * (1f - dy / Mathf.Max(0.0001f, width)) : 0f;

                    // The head is brightest, so a bolt reads as coming FROM the
                    // bomb rather than as a symmetric stick.
                    a *= Mathf.Lerp(1f, 0.72f, along);

                    px[y * N + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            // PixelsPerUnit = N, so the sprite is exactly ONE world unit across.
            //
            // This was 1f, which made a 16-pixel texture SIXTEEN units wide —
            // and since the beam is positioned by setting localScale.x to the
            // distance it must cover, every line was drawn sixteen times too
            // long and shot far past the gem it was meant to mark. The same
            // factor applied to the width, which is why cutting BeamWidth from
            // 0.09 to 0.035 to 0.018 never made the beams look thin: the value
            // on screen was sixteen times each of those.
            //
            // With one unit per sprite, localScale is the size in world units
            // and both axes mean what they say.
            _beam = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), N);
            return _beam;
        }
        private static Sprite _beam;

        /// <summary>The expanding shockwave: a hollow annulus.</summary>
        private static Sprite Ring()
        {
            if (_ring != null) return _ring;

            const int N = 128;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false)
            {
                name = "StoryColorBombRing",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };

            var px = new Color32[N * N];
            float half = (N - 1) * 0.5f;
            const float Mid = 0.84f, Band = 0.11f;

            for (int y = 0; y < N; y++)
            {
                for (int x = 0; x < N; x++)
                {
                    float dx = (x - half) / half, dy = (y - half) / half;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float off = Mathf.Abs(d - Mid) / Band;
                    float a = off >= 1f ? 0f : Mathf.Pow(1f - off, 1.7f);
                    px[y * N + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            _ring = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), N);
            return _ring;
        }
        private static Sprite _ring;

        /// <summary>Fallback for the bomb body when the gem's sprite is missing.</summary>
        private static Sprite Dot()
        {
            if (_dot != null) return _dot;

            const int N = 48;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false)
            {
                name = "StoryColorBombDot",
                wrapMode = TextureWrapMode.Clamp,
            };

            var px = new Color32[N * N];
            float half = (N - 1) * 0.5f;
            for (int y = 0; y < N; y++)
            {
                for (int x = 0; x < N; x++)
                {
                    float dx = (x - half) / half, dy = (y - half) / half;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    px[y * N + x] = new Color32(255, 255, 255,
                        (byte)(d >= 1f ? 0f : Mathf.Pow(1f - d, 0.8f) * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            _dot = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), N);
            return _dot;
        }
        private static Sprite _dot;
    }
}
