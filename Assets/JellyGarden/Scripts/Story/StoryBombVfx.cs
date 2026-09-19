// What a bomb draws when it goes off.
//
// WHY SPRITE RENDERERS AND NOT A PARTICLE SYSTEM
//
// The same reason StoryShatterVfx gives, and it is not a preference: every
// ParticleSystem this project has spawned on the board has rendered nothing —
// the kit's own pooled flipbook included — and that cause is still unfound.
// StoryExplosionProbe exists to chase it. What provably renders on this board
// is a SpriteRenderer carrying a generated texture, so that is what this uses.
//
// WHERE IT IS CALLED FROM, AND WHY NOT DestroyCor
//
// Item.DestroyPackage sets `currentType = ItemsTypes.NONE` BEFORE it calls
// DestroyItem, so by the time DestroyCor runs the bomb no longer identifies as
// a package and a hook in there cannot tell a bomb from an ordinary gem.
//
// The colour bomb makes the point sharper. Item.DestroyColor calls DestroyItem
// on every gem of one colour — up to about 25 of them on a nine-wide board, in
// a single frame. A hook in the destroy path would therefore fire twenty-five
// explosions at once. Hooking DestroyPackage and DestroyColor directly gives
// exactly ONE blast per bomb, at the bomb's own cell, which is both correct and
// affordable.
//
// THE FOUR BEATS
//
// Every variant is the same four things in different proportions, so the
// drawing code has no per-variant branch — only the numbers in
// StoryBombVariant.BlastOf differ:
//
//   GATHER   dust dragged INWARD before anything expands. This is anticipation,
//            the same principle that makes a cleared gem's squash work, and it
//            is the whole of what separates Implosion from the other four.
//   CORE     the bright centre, growing and fading.
//   RING     one or two shockwaves travelling outward.
//   DEBRIS   pieces thrown out and falling.
//
// The board's recoil is NOT here. StoryFinishVfx.Punchit already punches
// GameField on a damped spring and the rocket already uses it; every variant
// calls it with its own strength rather than carrying a second copy. See that
// file's header for why it is GameField and never the camera.

using System.Collections.Generic;
using UnityEngine;

namespace JellyGarden.Scripts.Story
{
    public static class StoryBombVfx
    {
        /// <summary>
        /// Off unless a launch asks for it. Flipped by <c>-storybomb</c>, by the
        /// <c>StoryVfx.Bomb</c> pref, or on by default in a development build.
        /// </summary>
        public static bool Enabled
        {
            get
            {
                if (_enabled.HasValue) return _enabled.Value;
                _enabled = StoryVfxFlags.On("-storybomb", "StoryVfx.Bomb");
                return _enabled.Value;
            }
            set { _enabled = value; }
        }
        private static bool? _enabled;

        // Above the gems (2) and the blockers (4), like every other board
        // effect here. The core sits above its own ring and debris.
        /// <summary>
        /// The kit's package bomb clears GetItemsAround — a three by three — so
        /// the RoyalTNTVFX blast that stands in for it is radius 1. The package
        /// covers (2 * radius + 1) squared cells.
        /// </summary>
        private const int PackageRadius = 1;

        /// <summary>
        /// Board recoil for a royal blast, on StoryFinishVfx's damped spring.
        /// The same 1.4 the sprite blast used, so swapping the picture did not
        /// quietly change how hard the board kicks.
        /// </summary>
        private const float RoyalPunch = 1.4f;

        private const int DebrisOrder = 5;
        private const int RingOrder = 6;
        private const int CoreOrder = 7;

        // The worst case is a colour bomb, which is ONE blast — the per-gem
        // clears are Glass Burst's problem, not this layer's. A blast is at
        // most 22 debris + 2 rings + 1 core + 16 gather motes, so 64 covers
        // three bombs overlapping in a cascade with room to spare.
        private const int PoolSize = 256;

        private sealed class Piece
        {
            public GameObject Go;
            public Transform T;
            public SpriteRenderer R;
            public bool Alive;
        }

        /// <summary>
        /// One explosion in flight. Everything it needs is captured when the
        /// bomb goes off, so a variant chosen while this is still on screen
        /// cannot re-physics what is already drawn.
        /// </summary>
        private sealed class Blast
        {
            public Vector3 At;
            public float Cell;
            public float Born;
            public Color Tint;
            public BombBlast Spec;

            public Piece Core;
            public Piece[] Rings;
            public readonly List<Mote> Motes = new List<Mote>(32);
            public readonly List<Mote> Dust = new List<Mote>(16);
            public bool Fired;
        }

        private sealed class Mote
        {
            public Piece P;
            public Vector3 From;
            public Vector3 Velocity;
            public float Life;
            public float Scale;

            // Per shard, not per blast. The reference's aftermath frame is
            // pink, blue, red and gold pieces mixed together — a single tint
            // across all of them reads as coloured dust rather than as a board
            // full of broken gems, which is the whole look being matched.
            public Color Colour;

            // Smoke is drawn under the shards, drifts instead of falling, and
            // grows as it fades. One flag rather than a second list: the two
            // differ in four numbers, not in kind.
            public bool Smoke;
        }

        /// <summary>
        /// The kit's own gem colours, read from the live board so the shards
        /// cannot disagree with the gems they came from. These are the fallback
        /// for a test with no LevelManager in the scene.
        /// </summary>
        private static readonly Color[] GemFallback =
        {
            new Color32(251, 80, 1, 255),
            new Color32(255, 193, 22, 255),
            new Color32(237, 13, 233, 255),
            new Color32(41, 157, 255, 255),
            new Color32(255, 255, 38, 255),
            new Color32(37, 219, 0, 255),
        };

        private static Color RandomGemColour()
        {
            LevelManager manager = LevelManager.THIS;
            Color[] table = manager != null && manager.scoresColors != null &&
                            manager.scoresColors.Length > 0
                ? manager.scoresColors
                : GemFallback;

            Color c = table[Random.Range(0, table.Length)];
            // Lifted a little, because a shard is small and the raw gem colour
            // reads muddy at a tenth of a cell.
            return Color.Lerp(c, Color.white, 0.18f);
        }

        private static readonly List<Piece> Pool = new List<Piece>(PoolSize);
        private static readonly List<Blast> Blasts = new List<Blast>(8);
        private static Transform _parent;
        private static Driver _driver;
        private static int _next;
        private static int _said;

        /// <summary>
        /// Set a bomb off at one cell.
        ///
        /// Called from Item.DestroyPackage and Item.DestroyColor — once per
        /// bomb, never per cleared gem. See the header.
        /// </summary>
        /// <param name="punchBoard">
        /// Whether this blast also recoils the board. True for a bomb that goes
        /// off on its own; FALSE for each target of a colour bomb, which would
        /// otherwise punch once per cleared gem. See the call site below.
        /// </param>
        /// <param name="royal">
        /// Whether this blast is drawn by the RoyalTNTVFX package. True for a
        /// bomb that goes off on its own; FALSE for each target of a colour
        /// bomb — see the branch below.
        /// </param>
        public static void Blow(Vector3 at, Color tint, bool punchBoard = true, bool royal = true)
        {
            if (!Enabled || !Application.isPlaying) return;

            // The package bomb's explosion is RoyalTNTVFX's now: the ignition
            // squash on the TNT art, a white-hot core into gold rays, a broken
            // shockwave, and a spark wave that rolls out ring by ring over the
            // blast area. It is 58 quads and no ParticleSystem at all, which is
            // why it is safe on a board that has never rendered one.
            //
            // PackageRadius is 1, not the package README's 2: the kit's
            // DestroyPackage clears GetItemsAround — a three by three — and the
            // package sizes its blast as (2 * radius + 1) squared.
            //
            // A COLOUR BOMB DOES NOT COME THROUGH HERE. It calls this once per
            // cleared gem, up to twenty-five times across one second, and each
            // of those is a single cell rather than a bomb going off. Twenty-five
            // TNT actors against a pool of thirty-two would starve the pool and
            // read as the board detonating rather than as gems being picked off,
            // so those keep the sprite blast below until they are judged on a
            // real board. StoryColorBombVfx passes royal: false.
            if (royal)
            {
                if (StoryRoyalVfx.Blast(at, PackageRadius))
                {
                    // The package draws no board recoil — deliberately, it is a
                    // pure VFX package. The punch is still this layer's, on the
                    // same damped spring every other blast uses.
                    if (punchBoard) StoryFinishVfx.Punchit(RoyalPunch);

                    _said++;
                    Debug.Log(string.Format("[Bomb] #{0} royal TNT at {1} | radius {2}",
                        _said, at.ToString("F2"), PackageRadius));
                    return;
                }

                // The player prefab is missing or Resources could not find it.
                // StoryRoyalVfx has already said so once; falling through to the
                // sprite blast means a missing package costs the look, not the
                // explosion.
            }

            EnsureRig();

            BombBlast spec = StoryBombVariant.BlastOf(StoryBombVariant.Current);
            float cell = StoryBoardScale.Pitch;

            var blast = new Blast
            {
                At = at,
                Cell = cell,
                Born = Time.time,
                Tint = tint,
                Spec = spec,
                Rings = new Piece[Mathf.Max(1, spec.Rings)],
            };

            // The gather's dust exists only while there IS a gather, so a
            // variant with none costs nothing for it.
            if (spec.Gather > 0f)
            {
                for (int i = 0; i < 16; i++)
                {
                    Piece piece = Lease();
                    if (piece == null) break;

                    float angle = Random.value * Mathf.PI * 2f;
                    float away = cell * (0.9f + Random.value * 1.4f);

                    blast.Dust.Add(new Mote
                    {
                        P = piece,
                        From = at + new Vector3(Mathf.Cos(angle) * away,
                                                Mathf.Sin(angle) * away, -0.3f),
                        Scale = cell * (0.06f + Random.value * 0.09f),
                    });

                    Dress(piece, Glow(), DebrisOrder);
                }
            }

            Blasts.Add(blast);

            // The board's recoil, at the moment the blast actually expands
            // rather than when the bomb was struck — so on Implosion it lands
            // after the gather, which is what makes the wind-up pay off.
            // The board recoil belongs to the CALLER, not to every blast.
            //
            // This used to punch unconditionally, and a colour bomb calls Blow
            // once per target — twenty-five times on a nine-wide board, spread
            // over the wave's second. Each punch is only a couple of percent of
            // GameField.localScale, but they overlap and compound, and the board
            // visibly zoomed. A package bomb never showed it because one punch
            // settles on its damped spring before anything else arrives.
            //
            // So Blow draws and nothing else. Item.DestroyPackage punches once
            // for its own explosion; StoryColorBombVfx deliberately does not,
            // because twenty-five recoils are not twenty-five times as
            // satisfying, they are a camera fault.
            if (punchBoard)
            {
                if (spec.Gather > 0f) StoryFinishVfx.PunchAfter(spec.Gather, spec.Punch);
                else StoryFinishVfx.Punchit(spec.Punch);
            }

            // EVERY blast, not the first three.
            //
            // The cap was three, and it hid exactly the thing that mattered: a
            // comparison of five variants produces five blasts, so a three-line
            // log reported only the taps made before any button was pressed and
            // said nothing at all about whether the selection was working. Two
            // wrong conclusions were drawn from that silence.
            //
            // A bomb is a rare event — one per tap in the lab, a handful per
            // level in play — so a line each is affordable in a way that a line
            // per cleared gem never was.
            _said++;
            Debug.Log(string.Format(
                "[Bomb] #{0} {1} at {2} | gather {3:F0}ms core {4:F1} ring {5:F1} debris {6} punch {7:F2}",
                _said, StoryBombVariant.NameOf(StoryBombVariant.Current),
                at.ToString("F2"), spec.Gather * 1000f, spec.CoreSize,
                spec.RingReach, spec.Debris, spec.Punch));
        }

        /// <summary>
        /// The colour a blast is drawn in: the gem's own, lifted towards white
        /// and then towards fire by the variant's Warm. A cold variant keeps the
        /// gem's colour; Fireball is orange whatever set it off.
        /// </summary>
        public static Color TintFor(int colour, float warm)
        {
            LevelManager manager = LevelManager.THIS;
            Color c = manager != null && manager.scoresColors != null &&
                      colour >= 0 && colour < manager.scoresColors.Length
                ? manager.scoresColors[colour]
                : Color.white;

            c = Color.Lerp(c, Color.white, 0.35f);
            return Color.Lerp(c, new Color(1f, 0.58f, 0.16f), warm);
        }

        private static void Spawn(Blast blast)
        {
            BombBlast spec = blast.Spec;
            Color tint = TintFor(0, spec.Warm);
            // The gem's colour where there is one; the blast was handed a tint
            // already lifted, so only the warm shift is applied here.
            tint = Color.Lerp(blast.Tint, tint, spec.Warm);

            blast.Core = Lease();
            if (blast.Core != null) Dress(blast.Core, Core(), CoreOrder);

            for (int i = 0; i < blast.Rings.Length; i++)
            {
                blast.Rings[i] = Lease();
                if (blast.Rings[i] != null) Dress(blast.Rings[i], Ring(), RingOrder);
            }

            for (int i = 0; i < spec.Debris; i++)
            {
                Piece piece = Lease();
                if (piece == null) break;

                float angle = Random.value * Mathf.PI * 2f;

                // Squared random, so most shards are slow and a few are thrown
                // far. A flat spread puts them all at one radius, which reads as
                // a ring; the reference has pieces everywhere from the centre to
                // the board's edge.
                float r = Random.value;
                float speed = spec.DebrisSpeed * (0.20f + r * r * 1.05f);

                blast.Motes.Add(new Mote
                {
                    P = piece,
                    // Scattered through the blast's own footprint rather than
                    // all from one point, so the first frame already looks like
                    // a cell coming apart instead of a fountain.
                    From = blast.At + new Vector3(
                        (Random.value - 0.5f) * blast.Cell * 0.8f,
                        (Random.value - 0.5f) * blast.Cell * 0.8f, -0.3f),
                    Velocity = new Vector3(Mathf.Cos(angle) * speed,
                                           Mathf.Sin(angle) * speed + 2.2f, 0f),
                    Life = spec.DebrisLife * (0.7f + Random.value * 0.6f),
                    // CHUNKY. The reference's pieces read as broken gem — a
                    // third of a cell at the top end, with visible facets — not
                    // as confetti. The first version drew them at 0.055 to 0.155
                    // of a cell, which is where "coloured dust" came from.
                    Scale = blast.Cell * (0.13f + Random.value * 0.21f),
                    Colour = RandomGemColour(),
                });

                Dress(piece, Chip(), DebrisOrder);
                piece.T.localRotation = Quaternion.Euler(0f, 0f, Random.value * 360f);
            }

            // The grey smoke at the blast edge. None of the five variants had
            // any, and it is what gives the reference's core an edge to sit
            // against rather than fading straight into the board.
            int puffs = Mathf.Max(6, spec.Debris / 6);
            for (int i = 0; i < puffs; i++)
            {
                Piece piece = Lease();
                if (piece == null) break;

                float angle = (i / (float)puffs) * Mathf.PI * 2f + Random.value * 0.7f;
                float speed = spec.DebrisSpeed * (0.10f + Random.value * 0.16f);

                blast.Motes.Add(new Mote
                {
                    P = piece,
                    From = blast.At + new Vector3(Mathf.Cos(angle) * blast.Cell * 0.7f,
                                                  Mathf.Sin(angle) * blast.Cell * 0.7f, -0.2f),
                    Velocity = new Vector3(Mathf.Cos(angle) * speed,
                                           Mathf.Sin(angle) * speed + 0.3f, 0f),
                    Life = spec.DebrisLife * 0.75f,
                    Scale = blast.Cell * (0.55f + Random.value * 0.45f),
                    Colour = new Color(0.82f, 0.82f, 0.86f, 1f),
                    Smoke = true,
                });

                Dress(piece, Glow(), DebrisOrder - 1);
            }

            // Electric only: straight branches out to nearby cells, drawn as
            // thin stretched glows rather than as real line geometry — at a
            // tenth of a cell wide the difference does not reach the screen.
            for (int i = 0; i < spec.Branches; i++)
            {
                Piece piece = Lease();
                if (piece == null) break;

                float angle = (i / (float)spec.Branches) * Mathf.PI * 2f
                            + Random.value * 0.4f;
                float reach = blast.Cell * (1.2f + Random.value * 1.6f);

                blast.Motes.Add(new Mote
                {
                    P = piece,
                    From = blast.At + new Vector3(Mathf.Cos(angle) * reach * 0.5f,
                                                  Mathf.Sin(angle) * reach * 0.5f, -0.4f),
                    Velocity = Vector3.zero,
                    Life = spec.CoreLife * 1.4f,
                    Scale = blast.Cell * 0.07f,
                });

                Dress(piece, Glow(), RingOrder);
                piece.T.localRotation = Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg);
                piece.T.localScale = new Vector3(reach, blast.Cell * 0.07f, 1f);
            }

            blast.Fired = true;
        }

        private static void Dress(Piece piece, Sprite sprite, int order)
        {
            piece.R.sprite = sprite;
            piece.R.sortingOrder = order;
            piece.R.enabled = true;
            piece.T.localRotation = Quaternion.identity;
            piece.Go.SetActive(true);
        }

        // ---- the frame loop ---------------------------------------------------

        private sealed class Driver : MonoBehaviour
        {
            private void LateUpdate()
            {
                float now = Time.time;
                float dt = Time.deltaTime;

                for (int b = Blasts.Count - 1; b >= 0; b--)
                {
                    Blast blast = Blasts[b];
                    BombBlast spec = blast.Spec;
                    float age = now - blast.Born;

                    // --- GATHER: dust dragged inward, nothing expanding yet ---
                    if (age < spec.Gather)
                    {
                        float t = age / spec.Gather;
                        // Accelerating inward, so it reads as being pulled
                        // rather than as drifting.
                        float pulled = t * t;

                        for (int i = 0; i < blast.Dust.Count; i++)
                        {
                            Mote dust = blast.Dust[i];
                            if (dust.P == null) continue;

                            Vector3 to = Vector3.Lerp(dust.From, blast.At,
                                pulled * spec.GatherPull);
                            dust.P.T.position = to;
                            dust.P.T.localScale = Vector3.one * (dust.Scale * (1f - t * 0.4f));
                            dust.P.R.color = new Color(blast.Tint.r, blast.Tint.g,
                                                       blast.Tint.b, 0.25f + t * 0.55f);
                        }
                        continue;
                    }

                    // The gather is over: retire its dust and build the blast.
                    if (!blast.Fired)
                    {
                        for (int i = 0; i < blast.Dust.Count; i++) Release(blast.Dust[i].P);
                        blast.Dust.Clear();
                        Spawn(blast);
                    }

                    float since = age - spec.Gather;
                    float longest = Mathf.Max(spec.CoreLife,
                                    Mathf.Max(spec.RingLife, spec.DebrisLife));

                    if (since >= longest)
                    {
                        Retire(blast);
                        Blasts.RemoveAt(b);
                        continue;
                    }

                    // --- CORE: grow to a peak, then fade ---
                    //
                    // The reference's core is a pinprick in frame one and does
                    // not reach full size until frame six of ten. The first
                    // version opened at full size and only shrank, which is why
                    // it read as a flash rather than as a detonation.
                    //
                    // So the life is split: GROWTH over the first 45%, fade over
                    // the remaining 55%. Opacity holds near full through the
                    // growth so the core is bright while it is still expanding,
                    // exactly as frames two through six show.
                    if (blast.Core != null)
                    {
                        const float Peak = 0.45f;

                        float t = Mathf.Clamp01(since / spec.CoreLife);
                        float size, alpha;

                        if (t < Peak)
                        {
                            // Accelerating outward, so the first frames are a
                            // small hot point rather than an instant disc.
                            float g = t / Peak;
                            size = blast.Cell * spec.CoreSize * Mathf.Pow(g, 1.35f);
                            alpha = 0.55f + 0.45f * g;
                        }
                        else
                        {
                            float f = (t - Peak) / (1f - Peak);
                            // Keeps expanding a little as it dies, which is what
                            // stops the fade reading as a light switching off.
                            size = blast.Cell * spec.CoreSize * (1f + f * 0.18f);
                            alpha = (1f - f) * (1f - f);
                        }

                        blast.Core.T.position = blast.At + new Vector3(0f, 0f, -0.5f);
                        blast.Core.T.localScale = Vector3.one * size;
                        blast.Core.R.color = new Color(blast.Tint.r, blast.Tint.g,
                                                       blast.Tint.b, alpha);
                    }

                    // --- RINGS ---
                    for (int i = 0; i < blast.Rings.Length; i++)
                    {
                        Piece ring = blast.Rings[i];
                        if (ring == null) continue;

                        // The second ring is a third of a life behind the first,
                        // which is what reads as two waves rather than as one
                        // thick edge.
                        float offset = i * spec.RingLife * 0.33f;
                        float t = Mathf.Clamp01((since - offset) / spec.RingLife);
                        if (since < offset) { ring.R.enabled = false; continue; }

                        ring.R.enabled = true;
                        float reach = blast.Cell * spec.RingReach * (1f - Mathf.Pow(1f - t, 2.2f));
                        ring.T.position = blast.At + new Vector3(0f, 0f, -0.45f);
                        ring.T.localScale = Vector3.one * Mathf.Max(0.001f, reach);
                        ring.R.color = new Color(1f, 1f, 1f, (1f - t) * 0.6f);
                    }

                    // --- DEBRIS and BRANCHES ---
                    for (int i = 0; i < blast.Motes.Count; i++)
                    {
                        Mote mote = blast.Motes[i];
                        if (mote.P == null) continue;

                        float t = Mathf.Clamp01(since / mote.Life);
                        if (t >= 1f) { mote.P.R.enabled = false; continue; }

                        // A branch has no velocity and keeps its stretched
                        // scale; only its opacity flickers away.
                        if (mote.Velocity == Vector3.zero)
                        {
                            float flicker = 0.55f + 0.45f * Mathf.Sin(since * 60f + i);
                            mote.P.R.color = new Color(1f, 1f, 1f, (1f - t) * flicker);
                            continue;
                        }

                        if (mote.Smoke)
                        {
                            // Smoke drifts, slows hard and GROWS as it thins —
                            // the opposite of a shard, which falls and shrinks.
                            mote.Velocity -= mote.Velocity * Mathf.Min(1f, 2.6f * dt);
                            mote.From += mote.Velocity * dt;
                            mote.P.T.position = mote.From;
                            mote.P.T.localScale = Vector3.one * (mote.Scale * (1f + t * 0.8f));
                            mote.P.R.color = new Color(mote.Colour.r, mote.Colour.g,
                                                       mote.Colour.b, (1f - t) * 0.5f);
                            continue;
                        }

                        // Shards fall under gravity and keep their own gem
                        // colour to the end, so the aftermath frame is coloured
                        // pieces on the board rather than fading white dust.
                        mote.Velocity.y -= 11.5f * dt;
                        mote.From += mote.Velocity * dt;
                        mote.P.T.position = mote.From;
                        mote.P.T.localScale = Vector3.one * (mote.Scale * (1f - t * 0.35f));
                        mote.P.R.color = new Color(mote.Colour.r, mote.Colour.g,
                                                   mote.Colour.b,
                                                   t < 0.7f ? 1f : (1f - t) / 0.3f);
                    }
                }
            }

            private static void Retire(Blast blast)
            {
                Release(blast.Core);
                for (int i = 0; i < blast.Rings.Length; i++) Release(blast.Rings[i]);
                for (int i = 0; i < blast.Motes.Count; i++) Release(blast.Motes[i].P);
                for (int i = 0; i < blast.Dust.Count; i++) Release(blast.Dust[i].P);
            }
        }

        // ---- pool -------------------------------------------------------------

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
            var go = new GameObject("Bomb" + index);
            go.transform.SetParent(_parent, false);
            go.SetActive(false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = CoreOrder;
            return new Piece { Go = go, T = go.transform, R = renderer, Alive = false };
        }

        private static void EnsureRig()
        {
            if (_parent != null && _driver != null) return;

            var holder = new GameObject("~StoryBombVfx");
            Object.DontDestroyOnLoad(holder);
            _parent = holder.transform;
            _driver = holder.AddComponent<Driver>();
        }

        /// <summary>Drop every blast. Called on a scene load.</summary>
        public static void Reset()
        {
            StoryRoyalVfx.Clear();
            for (int i = 0; i < Blasts.Count; i++) Driver_Retire(Blasts[i]);
            Blasts.Clear();
        }

        private static void Driver_Retire(Blast blast)
        {
            Release(blast.Core);
            if (blast.Rings != null)
                for (int i = 0; i < blast.Rings.Length; i++) Release(blast.Rings[i]);
            for (int i = 0; i < blast.Motes.Count; i++) Release(blast.Motes[i].P);
            for (int i = 0; i < blast.Dust.Count; i++) Release(blast.Dust[i].P);
        }

        // ---- sprites ----------------------------------------------------------

        /// <summary>A soft round glow: debris, dust and branches all draw it.</summary>
        private static Sprite Glow()
        {
            if (_glow != null) return _glow;

            const int N = 48;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false)
            {
                name = "StoryBombGlow",
                wrapMode = TextureWrapMode.Clamp,
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
                    float core = Mathf.Clamp01(1f - d / 0.42f);
                    float skirt = Mathf.Clamp01(1f - d);
                    float a = Mathf.Clamp01(core * 0.85f + skirt * skirt * 0.55f);
                    px[y * N + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            _glow = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), N);
            return _glow;
        }
        private static Sprite _glow;

        /// <summary>
        /// A gem chip: a hard-edged rounded square rather than a soft glow.
        ///
        /// Glow() has no edge at any size — that is exactly why StoryShatterVfx
        /// chose it, so its motes could never look sharp. The reference wants
        /// the opposite: frame two is small angular pieces of broken gem, and a
        /// soft radial falloff at a tenth of a cell reads as coloured dust.
        /// </summary>
        private static Sprite Chip()
        {
            if (_chip != null) return _chip;

            const int N = 32;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false)
            {
                name = "StoryBombChip",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };

            var px = new Color32[N * N];
            float half = (N - 1) * 0.5f;

            for (int y = 0; y < N; y++)
            {
                for (int x = 0; x < N; x++)
                {
                    float dx = Mathf.Abs(x - half) / half;
                    float dy = Mathf.Abs(y - half) / half;

                    // A squircle: solid to the rim, then one texel of falloff so
                    // the edge is hard without aliasing.
                    float d = Mathf.Pow(Mathf.Pow(dx, 3.2f) + Mathf.Pow(dy, 3.2f), 1f / 3.2f);
                    float a = d >= 1f ? 0f : Mathf.Clamp01((1f - d) * 6f);

                    // A brighter heart, so a chip catches light like a facet
                    // rather than reading as a flat tile.
                    byte v = (byte)(d < 0.45f ? 255 : 232);
                    px[y * N + x] = new Color32(v, v, v, (byte)(a * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            _chip = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), N);
            return _chip;
        }
        private static Sprite _chip;

        /// <summary>
        /// The bright centre: white through gold to orange, so a warm variant
        /// tints towards fire and a cold one stays near white.
        /// </summary>
        private static Sprite Core()
        {
            if (_core != null) return _core;

            const int N = 96;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false)
            {
                name = "StoryBombCore",
                wrapMode = TextureWrapMode.Clamp,
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

                    float a = d >= 1f ? 0f : Mathf.Pow(1f - d, 1.6f);
                    // A hard white heart, so the centre reads as LIGHT rather
                    // than as a coloured disc.
                    if (d < 0.30f) a = 1f;
                    px[y * N + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            _core = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), N);
            return _core;
        }
        private static Sprite _core;

        /// <summary>
        /// The shockwave: a hollow annulus.
        ///
        /// Nothing else in the project bakes one — Core, Glow and the rocket's
        /// Smoke are all filled discs — so a ring cannot be borrowed from them.
        /// A filled disc scaled up reads as a growing blob covering the board,
        /// which is the opposite of a wave passing over it.
        /// </summary>
        private static Sprite Ring()
        {
            if (_ring != null) return _ring;

            const int N = 128;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false)
            {
                name = "StoryBombRing",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };

            var px = new Color32[N * N];
            float half = (N - 1) * 0.5f;

            // The band sits just inside the sprite's edge, so scaling the
            // transform to a diameter puts the wave exactly there.
            const float Mid = 0.82f, Half = 0.12f;

            for (int y = 0; y < N; y++)
            {
                for (int x = 0; x < N; x++)
                {
                    float dx = (x - half) / half, dy = (y - half) / half;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);

                    float off = Mathf.Abs(d - Mid) / Half;
                    float a = off >= 1f ? 0f : Mathf.Pow(1f - off, 1.8f);
                    px[y * N + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            _ring = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), N);
            return _ring;
        }
        private static Sprite _ring;
    }
}
