// The spray a cleared gem throws, on top of the kit's own flipbook.
//
// The kit clears a gem with ONE thing: a 4-frame sprite flipbook at 15fps,
// 267ms, a single flat quad (`ItemExpl`, leased from a pool of 20 and driven by
// `ItemDestroyController`). That container is the ceiling on how the clear can
// feel, and no amount of better art lifts it — baked frames cannot blend
// additively, cannot outlive their own 267ms, and cannot move independently of
// one another. A Royal Match clear is four or five layers of particles doing
// exactly those three things.
//
// So this does not replace the flipbook. It fires ALONGSIDE it, from the same
// moment, and adds what a flipbook cannot do:
//
//   SHARDS   8-16 gem-coloured pieces thrown outward with their own velocity,
//            spin and gravity, so the cell scatters instead of blinking.
//   SPARKS   ~24 small bright motes that outlive the flipbook by a third of a
//            second, so the effect trails off rather than stopping dead.
//   FLASH    one additive burst at the centre for the first ~70ms, which is
//            what makes a blast read as LIGHT rather than as a sticker.
//
// Particles work here because the BOARD IS WORLD SPACE. They would not work on
// the story's screens: a ParticleSystem is a mesh renderer and a
// ScreenSpaceOverlay canvas draws after everything in the scene, which is why
// `RepairVfx` draws UI quads by hand instead. Different surface, different
// tool; see that file's header.
//
// Nothing here edits the kit. `Item.cs` gains one line that calls `Blast`, and
// this owns everything else — its own pool, its own material, its own
// lifetime. Deleting the file and that line restores the kit exactly.

using System.Collections.Generic;
using UnityEngine;

namespace JellyGarden.Scripts.Story
{
    public static class StoryBlastVfx
    {
        /// <summary>
        /// Off until it is asked for, so the board behaves exactly as the kit
        /// does unless a launch opts in. Flipped by <c>-storyblastvfx</c>, by the <c>StoryVfx.Blast</c> pref,
        /// or on by default in a development build. See StoryVfxFlags.
        /// </summary>
        public static bool Enabled
        {
            get
            {
                // Cached, because every clear reads this and the answer
                // cannot change inside one run.
                if (_enabled.HasValue) return _enabled.Value;
                _enabled = StoryVfxFlags.On("-storyblastvfx", "StoryVfx.Blast");
                return _enabled.Value;
            }
            set { _enabled = value; }
        }
        private static bool? _enabled;

        // The kit's own gem colours, from LevelManager.scoresColors in
        // game.unity. Read from the live manager when there is one so a themed
        // board cannot disagree with its own score popups; these are the
        // fallback for a test with no board in the scene.
        private static readonly Color[] Fallback =
        {
            new Color32(251, 80, 1, 255),
            new Color32(255, 193, 22, 255),
            new Color32(237, 13, 233, 255),
            new Color32(41, 157, 255, 255),
            new Color32(255, 255, 38, 255),
            new Color32(37, 219, 0, 255),
        };

        // A cleared gem is about one cell across, and the spray has to read as
        // coming OUT of that cell without covering its neighbours. Measured
        // against StoryBoardScale.Pitch rather than hardcoded, so a change to
        // the board's pitch carries the effect with it.
        // Speeds are raised as the lives are cut, because what reads as "thrown
        // hard" is DISTANCE COVERED, not velocity: a shard living 100ms at the
        // old 3.2 would travel a quarter of what it used to and read as the
        // burst having lost its force.
        private const float ShardSpeed = 5.4f;
        private const float SparkSpeed = 3.4f;

        // Stage 4 (POP) and stage 6 (secondary), at the durations chosen on the
        // browser prototype: 100ms and 260ms.
        //
        // These were 0.42 and 0.62 — the values from before the seven-stage
        // sequence existed, when the spray was the WHOLE effect and had to
        // carry the clear on its own. It no longer does: stages 1-3 now spend
        // 90ms winding up before any of this fires, so a 420ms shard throw
        // pushed the visible end of a clear past half a second and the board
        // read as sluggish during a cascade.
        private const float ShardLife = 0.10f;
        private const float SparkLife = 0.26f;

        // Unchanged. The flash is stage 3's partner at the pop instant and 70ms
        // is already about two frames; cutting it further loses it entirely.
        private const float FlashLife = 0.07f;

        // Above the gems (2) and the blockers (4). The kit's own
        // ParticleSorting uses 3 for board particles; this sits above both so
        // a blast is never drawn under the pieces it came from.
        private const int SortingOrder = 6;

        // Deep enough that a full cascade never runs dry. A pool that empties
        // does not look sparser, it looks like the effect stuttering — the
        // same lesson RepairVfx records for its 220 quads. A cascade can clear
        // a dozen cells in a frame and each holds its systems for ~0.6s.
        private const int PoolSize = 24;

        private sealed class Rig
        {
            public GameObject Root;
            public ParticleSystem Shards;
            public ParticleSystem Sparks;
            public ParticleSystem Flash;
            public float FreeAt;
        }

        // A few lines, not one per clear: a cascade fires a dozen of these and
        // a log line per particle burst is its own kind of blindness.
        private static int _said;

        private static readonly List<Rig> Pool = new List<Rig>(PoolSize);
        private static Material _additive;
        private static Material _soft;
        private static Transform _parent;
        private static Ticker _ticker;

        private static void EnsureTicker()
        {
            if (_ticker != null) return;
            EnsureParent();
            if (_parent == null) return;
            _ticker = _parent.gameObject.AddComponent<Ticker>();
        }

        /// <summary>
        /// Holds the sprays that are waiting for a rocket's head to reach them.
        ///
        /// This class is otherwise entirely static — the particle rigs are
        /// leased and play themselves, so nothing here needed a frame hook
        /// until the stagger did. It lives on the same holder the rigs are
        /// parented to rather than on one of its own.
        /// </summary>
        private sealed class Ticker : MonoBehaviour
        {
            private struct Waiting
            {
                public float At;
                public Vector3 Where;
                public int Colour;
            }

            private readonly List<Waiting> _waiting = new List<Waiting>(16);

            internal void Defer(float at, Vector3 where, int colour)
            {
                _waiting.Add(new Waiting { At = at, Where = where, Colour = colour });
            }

            private void Update()
            {
                if (_waiting.Count == 0) return;

                float now = Time.time;
                // Backwards, so removing one cannot skip its neighbour.
                for (int i = _waiting.Count - 1; i >= 0; i--)
                {
                    if (now < _waiting[i].At) continue;
                    Waiting due = _waiting[i];
                    _waiting.RemoveAt(i);
                    Fire(due.Where, due.Colour);
                }
            }
        }

        /// <summary>
        /// Throw the spray for one cleared gem.
        ///
        /// `colour` is the kit's colour index (0-5), the same int the flipbook's
        /// Animator is given, so the spray always matches the gem that produced
        /// it and the score popup that follows it.
        /// </summary>
        public static void Blast(Vector3 at, int colour)
        {
            if (!Enabled) return;
            if (!Application.isPlaying) return;

            // One effect per clear, not four. Shatter, the pop wind-up, this
            // spray and the neighbour spring all fire from DestroyCor and every
            // one of them defaults ON in a development build, so a cleared gem
            // was drawing all four at once and no single variant could be
            // judged. The variant registry is the arbiter: these five are
            // fall-out-the-board looks, and the spray is not part of them.
            if (StoryVfxVariant.Current != VfxVariant.Kit) return;

            // A gem cleared by a rocket sprays when the HEAD reaches it, not
            // when the kit clears the line. The kit clears the whole row on one
            // frame and cannot be made to wait — see StoryRocketSweep's header
            // for what happens when it is — so the picture waits instead.
            // Zero for an ordinary match, which then behaves exactly as before.
            float delay = StoryRocketSweep.DelayFor(at);
            if (delay > 0f)
            {
                EnsureTicker();
                if (_ticker != null)
                {
                    _ticker.Defer(Time.time + delay, at, colour);
                    return;
                }
            }

            Fire(at, colour);
        }

        /// <summary>
        /// Throw the spray now. Split from <see cref="Blast"/> so a staggered
        /// burst can reach the same code once its delay has run out.
        /// </summary>
        private static void Fire(Vector3 at, int colour)
        {
            Rig rig = Lease();
            if (rig == null)
            {
                if (_said < 3) { _said++; Debug.Log("[Blast] pool empty, nothing thrown"); }
                return;
            }

            // Say what was actually built and where, ONCE. Every wrong
            // conclusion about the kit's own flipbook survived a log line that
            // reported intent rather than result, so this reports the material
            // that was resolved and the renderer's real state.
            if (_said < 3)
            {
                _said++;
                var pr = rig.Shards.GetComponent<ParticleSystemRenderer>();
                Debug.Log(string.Format(
                    "[Blast] #{0} at {1} colour {2} | shader '{3}' mat '{4}' tex {5} " +
                    "| order {6} layer {7} enabled {8} | pool {9}",
                    _said, at.ToString("F2"), colour,
                    _additive == null || _additive.shader == null ? "NULL" : _additive.shader.name,
                    _additive == null ? "NULL" : _additive.name,
                    _additive == null || _additive.mainTexture == null ? "NULL" : "ok",
                    pr.sortingOrder, pr.sortingLayerID, pr.enabled, Pool.Count));
            }

            Color tint = ColourFor(colour);

            rig.Root.transform.position = at;
            Paint(rig.Shards, tint);
            Paint(rig.Sparks, tint);
            Paint(rig.Flash, tint);

            rig.Shards.Play(false);
            rig.Sparks.Play(false);
            rig.Flash.Play(false);

            rig.FreeAt = Time.time + SparkLife + 0.1f;
        }

        private static Color ColourFor(int colour)
        {
            LevelManager manager = LevelManager.THIS;
            if (manager != null && manager.scoresColors != null &&
                colour >= 0 && colour < manager.scoresColors.Length)
                return manager.scoresColors[colour];
            return Fallback[Mathf.Clamp(colour, 0, Fallback.Length - 1)];
        }

        private static void Paint(ParticleSystem system, Color tint)
        {
            ParticleSystem.MainModule main = system.main;
            main.startColor = tint;
        }

        private static Rig Lease()
        {
            float now = Time.time;
            for (int i = 0; i < Pool.Count; i++)
                if (Pool[i].Root != null && now >= Pool[i].FreeAt) return Pool[i];

            if (Pool.Count >= PoolSize) return null;

            Rig made = Build();
            if (made != null) Pool.Add(made);
            return made;
        }

        private static Rig Build()
        {
            EnsureParent();
            EnsureMaterials();

            var root = new GameObject("StoryBlast" + Pool.Count);
            root.transform.SetParent(_parent, false);

            var rig = new Rig
            {
                Root = root,
                Shards = MakeShards(root.transform),
                Sparks = MakeSparks(root.transform),
                Flash = MakeFlash(root.transform),
                FreeAt = 0f,
            };
            return rig;
        }

        private static void EnsureParent()
        {
            if (_parent != null) return;
            var holder = new GameObject("~StoryBlastVfx");
            Object.DontDestroyOnLoad(holder);
            _parent = holder.transform;
        }

        private static void EnsureMaterials()
        {
            if (_additive != null) return;

            // The kit's own board particles use the builtin Particles/Additive
            // shader (package_particle.mat, shader fileID 10721) and they do
            // render on this board — Firework fires on every package clear. So
            // this uses the same shader rather than inventing one, which also
            // means it inherits whatever render pipeline the project is on.
            Shader additive = Shader.Find("Particles/Additive");
            if (additive == null) additive = Shader.Find("Legacy Shaders/Particles/Additive");
            if (additive == null) additive = Shader.Find("Sprites/Default");
            _additive = new Material(additive) { name = "StoryBlastAdditive" };

            Shader soft = Shader.Find("Particles/Alpha Blended Premultiply");
            if (soft == null) soft = Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply");
            if (soft == null) soft = additive;
            _soft = new Material(soft) { name = "StoryBlastSoft" };

            Texture2D dot = Dot();
            _additive.mainTexture = dot;
            _soft.mainTexture = dot;
        }

        // A round mote, drawn once. The AllIn1 library has shapes that would do,
        // but every one of them is a 512px file loaded through Resources, and a
        // particle at a tenth of a cell resolves none of that detail. A 32px
        // radial falloff is what actually reaches the screen.
        private static Texture2D _dot;
        private static Texture2D Dot()
        {
            if (_dot != null) return _dot;
            const int N = 32;
            _dot = new Texture2D(N, N, TextureFormat.RGBA32, false) { name = "StoryBlastDot" };
            _dot.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[N * N];
            float half = (N - 1) * 0.5f;
            for (int y = 0; y < N; y++)
            {
                for (int x = 0; x < N; x++)
                {
                    float dx = (x - half) / half;
                    float dy = (y - half) / half;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    // Squared falloff keeps a bright core with a soft skirt,
                    // which is what reads as a spark rather than as a disc.
                    float a = Mathf.Clamp01(1f - d);
                    a *= a;
                    byte v = (byte)(a * 255f);
                    px[y * N + x] = new Color32(255, 255, 255, v);
                }
            }
            _dot.SetPixels32(px);
            _dot.Apply(false, true);
            return _dot;
        }

        private static ParticleSystem Attach(Transform parent, string name, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var system = go.AddComponent<ParticleSystem>();

            ParticleSystem.MainModule main = system.main;
            main.playOnAwake = false;
            main.loop = false;
            // Local, so the whole rig can be moved to the next cell and the
            // particles already in flight are not dragged along with it.
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Local;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 0f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = material;
            renderer.sortingLayerID = 0;
            renderer.sortingOrder = SortingOrder;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;

            return system;
        }

        private static ParticleSystem MakeShards(Transform parent)
        {
            ParticleSystem system = Attach(parent, "Shards", _soft);
            float cell = StoryBoardScale.Pitch;

            ParticleSystem.MainModule main = system.main;
            main.duration = ShardLife;
            main.startLifetime = new ParticleSystem.MinMaxCurve(ShardLife * 0.7f, ShardLife);
            main.startSpeed = new ParticleSystem.MinMaxCurve(ShardSpeed * 0.55f, ShardSpeed);

            // CHUNKY, which is a count-and-size decision rather than a texture
            // one. The dot these draw has no edge at any size, so the only way a
            // piece reads as a PIECE is for it to be big enough to have visible
            // area — roughly a third of a cell at the top end, up from 0.30.
            // Fewer and larger also stops the burst reading as a puff of dust,
            // which is what 14 small motes at 100ms looked like.
            main.startSize = new ParticleSystem.MinMaxCurve(cell * 0.24f, cell * 0.44f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

            // Gravity raised with the speed. At 100ms a shard barely falls at
            // all, and a burst with no droop reads as a flat ring rather than as
            // pieces being thrown off something.
            main.gravityModifier = 2.4f;
            main.maxParticles = 24;

            ParticleSystem.EmissionModule emission = system.emission;

            // 6-9, down from 8-14. The pieces are now nearly twice the area, so
            // the old count at the new size covers the neighbouring cells.
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 6, 9) });

            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = cell * 0.10f;
            shape.radiusThickness = 1f;

            // Spin, so a shard reads as a piece of something rather than a dot.
            ParticleSystem.RotationOverLifetimeModule spin = system.rotationOverLifetime;
            spin.enabled = true;
            spin.z = new ParticleSystem.MinMaxCurve(-6f, 6f);

            // Shrink as they fly: a shard that keeps its size looks like it is
            // approaching the camera rather than dying.
            ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, Falloff(1f, 0.25f));

            ParticleSystem.ColorOverLifetimeModule fade = system.colorOverLifetime;
            fade.enabled = true;
            fade.color = new ParticleSystem.MinMaxGradient(Fade(1f, 0f, 0.55f));

            return system;
        }

        private static ParticleSystem MakeSparks(Transform parent)
        {
            ParticleSystem system = Attach(parent, "Sparks", _additive);
            float cell = StoryBoardScale.Pitch;

            ParticleSystem.MainModule main = system.main;
            main.duration = SparkLife;
            main.startLifetime = new ParticleSystem.MinMaxCurve(SparkLife * 0.5f, SparkLife);
            main.startSpeed = new ParticleSystem.MinMaxCurve(SparkSpeed * 0.35f, SparkSpeed);
            main.startSize = new ParticleSystem.MinMaxCurve(cell * 0.04f, cell * 0.10f);
            main.gravityModifier = 0.55f;
            main.maxParticles = 40;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 18, 26) });

            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = cell * 0.06f;
            shape.radiusThickness = 1f;

            // THE CURVE. Stage 6 asks for sparks that curl outward rather than
            // flying straight, and an orbital velocity about z is what bends a
            // world-space particle's path without touching its speed: the
            // particle keeps its outward velocity and gains a rotation about
            // the burst's own centre, so the track is an arc.
            //
            // Signed range, so half the sparks curl each way — one direction
            // for all of them reads as the whole burst being swept sideways by
            // wind, which is a different effect entirely.
            ParticleSystem.VelocityOverLifetimeModule swirl = system.velocityOverLifetime;
            swirl.enabled = true;
            swirl.space = ParticleSystemSimulationSpace.World;

            // ALL THREE ORBITAL AXES, IN THE SAME MODE.
            //
            // Unity requires orbitalX/Y/Z to share one MinMaxCurve mode and
            // logs "Particle Orbital Velocity curves must all be in the same
            // mode" every time a system plays otherwise — which, on a board
            // clearing gems, is a wall of red per cascade and a curl that never
            // applies.
            //
            // Setting only Z left X and Y at their default single-constant mode
            // against Z's TwoConstants. So X and Y are written explicitly, in
            // the same two-constant form, pinned to zero: the curl is still
            // only about Z, but the module is now internally consistent.
            swirl.orbitalX = new ParticleSystem.MinMaxCurve(0f, 0f);
            swirl.orbitalY = new ParticleSystem.MinMaxCurve(0f, 0f);
            swirl.orbitalZ = new ParticleSystem.MinMaxCurve(-2.6f, 2.6f);

            // The offset the orbit is measured from, in the same mode again for
            // the same reason. Zero is deliberate rather than omitted: the
            // system's own origin is the cell the gem died in, which is exactly
            // the centre the sparks should curl around.
            swirl.radial = new ParticleSystem.MinMaxCurve(0f, 0f);

            ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, Falloff(1f, 0f));

            ParticleSystem.ColorOverLifetimeModule fade = system.colorOverLifetime;
            fade.enabled = true;
            fade.color = new ParticleSystem.MinMaxGradient(Fade(1f, 0f, 0.35f));

            return system;
        }

        private static ParticleSystem MakeFlash(Transform parent)
        {
            ParticleSystem system = Attach(parent, "Flash", _additive);
            float cell = StoryBoardScale.Pitch;

            ParticleSystem.MainModule main = system.main;
            main.duration = FlashLife;
            main.startLifetime = FlashLife;
            main.startSpeed = 0f;
            main.startSize = cell * 0.95f;
            main.maxParticles = 2;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });

            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = false;

            // Grows a little as it dies, which reads as a pop rather than a
            // light being switched off.
            ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
            size.enabled = true;
            var grow = new AnimationCurve();
            grow.AddKey(0f, 0.55f);
            grow.AddKey(0.4f, 1f);
            grow.AddKey(1f, 1.15f);
            size.size = new ParticleSystem.MinMaxCurve(1f, grow);

            ParticleSystem.ColorOverLifetimeModule fade = system.colorOverLifetime;
            fade.enabled = true;
            fade.color = new ParticleSystem.MinMaxGradient(Fade(1f, 0f, 0.15f));

            return system;
        }

        /// <summary>A curve from `from` to `to`, easing out.</summary>
        private static AnimationCurve Falloff(float from, float to)
        {
            var curve = new AnimationCurve();
            curve.AddKey(0f, from);
            curve.AddKey(0.45f, Mathf.Lerp(to, from, 0.55f));
            curve.AddKey(1f, to);
            return curve;
        }

        /// <summary>
        /// Alpha holding at `from` until `hold` of the life is gone, then
        /// falling to `to`. Holding first is what stops a burst looking like it
        /// is fading from the instant it appears.
        /// </summary>
        private static Gradient Fade(float from, float to, float hold)
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f),
                },
                new[]
                {
                    new GradientAlphaKey(from, 0f),
                    new GradientAlphaKey(from, hold),
                    new GradientAlphaKey(to, 1f),
                });
            return gradient;
        }
    }
}
