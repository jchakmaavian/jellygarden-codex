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
        /// does unless a launch opts in. Flipped by <c>-storyblastvfx</c>.
        /// </summary>
        public static bool Enabled
        {
            get
            {
                if (_enabled.HasValue) return _enabled.Value;
                _enabled = false;
                foreach (string arg in global::System.Environment.GetCommandLineArgs())
                    if (arg == "-storyblastvfx") { _enabled = true; break; }
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
        private const float ShardSpeed = 3.2f;
        private const float SparkSpeed = 2.1f;
        private const float ShardLife = 0.42f;
        private const float SparkLife = 0.62f;
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
            main.startSize = new ParticleSystem.MinMaxCurve(cell * 0.16f, cell * 0.30f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = 1.15f;
            main.maxParticles = 24;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 8, 14) });

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
