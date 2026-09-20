using System.Collections;
using UnityEngine;

namespace RoyalRocketVFX
{
    /// <summary>
    /// Self-contained rocket effect: pooled flying heads, a bounded flame plume, star sparkles
    /// and a soft smoke tail. This component is purely visual - it never destroys anything.
    /// Subscribe to <see cref="CellReached"/> to run your own gameplay destruction in sync with
    /// the nozzle.
    ///
    /// Drop a RoyalRocketVFX prefab into a scene, then call
    /// <see cref="RocketVFX.Play(Vector3, RocketDirection, float, float)"/>.
    /// </summary>
    [AddComponentMenu("Royal Rocket VFX/Rocket VFX Player")]
    [DisallowMultipleComponent]
    public sealed class RocketVFXPlayer : MonoBehaviour
    {
        // Emission is driven by distance travelled, not by frame count, so plume density is
        // identical at 30fps, 144fps and at Time.timeScale 0.5.
        const float ExhaustSampleCells = .075f;

        [Header("Tuning")]
        [Tooltip("Optional shared profile. When set it replaces the inline values below.")]
        [SerializeField] RocketVFXSettings profile;
        [SerializeField] RocketVFXTuning tuning = new RocketVFXTuning();

        [Header("Directional artwork")]
        [SerializeField] RocketDirectionalArt left;
        [SerializeField] RocketDirectionalArt right;
        [SerializeField] RocketDirectionalArt down;
        [SerializeField] RocketDirectionalArt up;

        [Header("Materials (all use RoyalRocketVFX/RocketFire)")]
        [SerializeField] Material coreTrailMaterial;
        [SerializeField] Material glowTrailMaterial;
        [SerializeField] Material flameRibbonMaterial;
        [SerializeField] Material nozzleFlameMaterial;
        [SerializeField] Material starMaterial;
        [SerializeField] Material smokeMaterial;
        [SerializeField] Material flashMaterial;
        [SerializeField] Material ringMaterial;

        [Header("Behaviour")]
        [Tooltip("Camera used to decide where the rocket leaves the screen. Empty uses Camera.main.")]
        [SerializeField] Camera viewCamera;
        [Tooltip("Axis used by PlayDefaultPair(), so a prefab can be preset as the horizontal or vertical rocket.")]
        [SerializeField] RocketAxis defaultAxis = RocketAxis.Horizontal;
        [Tooltip("Register as the player used by the static RocketVFX.Play API.")]
        [SerializeField] bool registerAsDefault = true;

        /// <summary>Raised when the rocket nose reaches a whole cell: world position and cell index.
        /// Hook your own destruction here - the package never touches your board.</summary>
        public event System.Action<Vector3, int> CellReached;
        /// <summary>Raised the moment a rocket ignites, after the anticipation squash.</summary>
        public event System.Action<Vector3, RocketDirection> Launched;
        /// <summary>Raised when a rocket leaves the screen and starts returning to the pool.</summary>
        public event System.Action<Vector3, RocketDirection> Exited;

        /// <summary>Freeze the effect without touching Time.timeScale.</summary>
        public bool Paused { get; set; }

        /// <summary>True while any rocket or its trailing particles are still visible.</summary>
        public bool IsBusy
        {
            get
            {
                if (ParticleCount > 0) return true;
                if (flyers != null) foreach (var f in flyers) if (f.used) return true;
                return false;
            }
        }

        /// <summary>Live particle count across every rocket system. Zero when fully cleaned up.</summary>
        public int ParticleCount
        {
            get { int n = 0; if (systems != null) foreach (var ps in systems) if (ps) n += ps.particleCount; return n; }
        }

        /// <summary>How many pooled rockets are currently in flight.</summary>
        public int ActiveRockets
        {
            get { int n = 0; if (flyers != null) foreach (var f in flyers) if (f.used) n++; return n; }
        }

        /// <summary>Active tuning: the shared profile when assigned, otherwise the inline values.</summary>
        public RocketVFXTuning Tuning => profile ? profile.tuning : tuning;

        sealed class Flyer
        {
            public GameObject go;
            public SpriteRenderer sprite;
            public TrailRenderer glow, core;
            public LineRenderer flame;
            public MeshRenderer nozzle;
            public readonly MaterialPropertyBlock flameProperties = new MaterialPropertyBlock();
            public bool used;
        }

        Flyer[] flyers;
        Transform root;
        Material fireInstance, ribbonInstance;
        Mesh nozzleMesh;
        Sprite[] heads;
        ParticleSystem impactRings, glowFlash, impactStars, embers, exhaustStars, smoke;
        ParticleSystem[] systems;
        float clock;
        int launchCount;

        // Deterministic sequence: the same launch looks the same in every capture and replay.
        readonly System.Random random = new System.Random(72813);
        float Range(float min, float max) => min + (float)random.NextDouble() * (max - min);

        // Index order matches the art fields and the sprite array: Left, Right, Down, Up.
        static readonly Vector3[] AxisOf = { Vector3.up, Vector3.down, Vector3.left, Vector3.right };
        static readonly float[] NozzleAngle = { 90f, -90f, 180f, 0f };
        static int HeadIndex(RocketDirection d) =>
            d == RocketDirection.Left ? 0 : d == RocketDirection.Right ? 1 : d == RocketDirection.Down ? 2 : 3;

        void Awake()
        {
            Build();
            if (registerAsDefault) RocketVFX.Register(this);
        }

        void OnDestroy()
        {
            RocketVFX.Unregister(this);
            if (fireInstance) Destroy(fireInstance);
            if (ribbonInstance) Destroy(ribbonInstance);
            if (nozzleMesh) Destroy(nozzleMesh);
            if (heads != null) foreach (var s in heads) if (s) Destroy(s);
        }

        void OnDisable() => Clear();

        void Update()
        {
            if (!Paused)
            {
                clock += Time.deltaTime;
                if (fireInstance) fireInstance.SetFloat("_FxTime", clock);
                if (ribbonInstance) ribbonInstance.SetFloat("_FxTime", clock);
            }
            if (systems == null) return;
            foreach (var ps in systems)
            {
                if (!ps) continue;
                if (Paused && !ps.isPaused) ps.Pause(false);
                else if (!Paused && ps.isPaused) ps.Play(false);
            }
        }

        // ------------------------------------------------------------------ public API

        /// <summary>
        /// Fire one rocket. <paramref name="distanceInCells"/> is how many cells it should sweep
        /// (cell events are raised for those); it then keeps flying off screen at the same speed.
        /// <paramref name="cellSize"/> is world units per cell - pass 0 to use the tuning default.
        /// </summary>
        public void Play(Vector3 position, RocketDirection direction, float distanceInCells, float cellSize = 0f)
        {
            if (!isActiveAndEnabled) return;
            var t = Tuning;
            float pitch = cellSize > 0f ? cellSize : t.cellSize;
            StartCoroutine(Fly(position, direction, Mathf.Max(0f, distanceInCells), Mathf.Max(.01f, pitch)));
        }

        /// <summary>Axis this prefab is preset for.</summary>
        public RocketAxis DefaultAxis => defaultAxis;

        /// <summary>Fire the preset pair for this prefab (Horizontal or Vertical).</summary>
        public void PlayDefaultPair(Vector3 position, float distanceInCells, float cellSize = 0f) =>
            PlayPair(position, defaultAxis, distanceInCells, cellSize);

        /// <summary>Fire the two rockets of a match-3 line clear: Left+Right, or Up+Down.</summary>
        public void PlayPair(Vector3 position, RocketAxis axis, float distanceInCells, float cellSize = 0f)
        {
            if (axis == RocketAxis.Horizontal)
            {
                Play(position, RocketDirection.Left, distanceInCells, cellSize);
                Play(position, RocketDirection.Right, distanceInCells, cellSize);
            }
            else
            {
                Play(position, RocketDirection.Up, distanceInCells, cellSize);
                Play(position, RocketDirection.Down, distanceInCells, cellSize);
            }
        }

        /// <summary>Stop everything immediately and return all rockets and particles to the pool.</summary>
        public void Clear()
        {
            StopAllCoroutines();
            if (systems != null)
                foreach (var ps in systems)
                    if (ps) ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (flyers == null) return;
            foreach (var f in flyers)
            {
                f.used = false;
                f.nozzle.enabled = false;
                f.flame.enabled = false;
                f.flame.positionCount = 0;
                f.glow.emitting = false;
                f.core.emitting = false;
                f.glow.Clear();
                f.core.Clear();
                f.sprite.enabled = true;
                f.go.SetActive(false);
            }
        }

        // ------------------------------------------------------------------ construction

        void Build()
        {
            var t = Tuning;
            root = new GameObject("RocketVFX - runtime effects").transform;
            root.SetParent(transform, false);

            // Only the time and brightness uniforms are mutable; the authored materials on disk
            // are never written to.
            fireInstance = nozzleFlameMaterial ? new Material(nozzleFlameMaterial) { name = "Nozzle flame (instance)" } : null;
            ribbonInstance = flameRibbonMaterial ? new Material(flameRibbonMaterial) { name = "Flame ribbon (instance)" } : null;
            ApplyFlameIntensity(t.flameIntensity);

            impactRings = CreateParticles("Contact rings", ringMaterial, 128, true, false, t);
            glowFlash = CreateParticles("Launch and contact light", flashMaterial, 128, false, false, t);
            impactStars = CreateParticles("Impact stars", starMaterial, 2048, false, true, t);
            embers = CreateParticles("Gold embers", flashMaterial, 1024, false, false, t);
            exhaustStars = CreateParticles("Exhaust star sparkles", starMaterial, 2048, false, true, t);
            smoke = CreateParticles("White smoke tail", smokeMaterial, 512, false, false, t);

            // Smoke is born with every spatial sample but stays transparent through the flame
            // body. It appears only in the final cells, grows, then dissolves - so the plume has
            // no hard cutoff and no detached bulb behind the ribbon.
            var smokeColour = smoke.colorOverLifetime;
            var smokeFade = new Gradient();
            smokeFade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[]
                {
                    new GradientAlphaKey(0, 0), new GradientAlphaKey(0, .72f), new GradientAlphaKey(.14f, .82f),
                    new GradientAlphaKey(.09f, .92f), new GradientAlphaKey(0, 1)
                });
            smokeColour.color = smokeFade;
            var smokeSize = smoke.sizeOverLifetime;
            smokeSize.size = new ParticleSystem.MinMaxCurve(1, new AnimationCurve(
                new Keyframe(0, .42f), new Keyframe(.72f, .62f), new Keyframe(1, 1.85f)));

            // Gold at birth, cooling to white, held at readable size through mid-plume so the
            // individual four-point stars stay countable before they thin out.
            var starColour = exhaustStars.colorOverLifetime;
            var starFade = new Gradient();
            starFade.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1, .84f, .40f), 0), new GradientColorKey(new Color(1, .95f, .80f), .34f),
                    new GradientColorKey(Color.white, .62f), new GradientColorKey(Color.white, 1)
                },
                new[]
                {
                    new GradientAlphaKey(.78f, 0), new GradientAlphaKey(1, .18f), new GradientAlphaKey(.86f, .50f),
                    new GradientAlphaKey(.28f, .84f), new GradientAlphaKey(0, 1)
                });
            starColour.color = starFade;
            var starSize = exhaustStars.sizeOverLifetime;
            starSize.size = new ParticleSystem.MinMaxCurve(1, new AnimationCurve(
                new Keyframe(0, .80f), new Keyframe(.22f, 1), new Keyframe(.62f, .78f), new Keyframe(1, .12f)));

            systems = new[] { impactRings, glowFlash, impactStars, embers, smoke, exhaustStars };

            heads = new[]
            {
                left.CreateSprite("Rocket_Left"),
                right.CreateSprite("Rocket_Right"),
                down.CreateSprite("Rocket_Down"),
                up.CreateSprite("Rocket_Up")
            };

            nozzleMesh = new Mesh { name = "Rocket nozzle flame quad" };
            nozzleMesh.vertices = new[]
            {
                new Vector3(-1.65f, -.48f, 0), new Vector3(.035f, -.48f, 0),
                new Vector3(.035f, .48f, 0), new Vector3(-1.65f, .48f, 0)
            };
            nozzleMesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            nozzleMesh.colors = new[] { Color.white, Color.white, Color.white, Color.white };
            nozzleMesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            nozzleMesh.RecalculateBounds();

            BuildPool(t);
        }

        void ApplyFlameIntensity(float intensity)
        {
            if (fireInstance) fireInstance.SetFloat("_Boost", 1.45f * intensity);
            if (ribbonInstance) ribbonInstance.SetFloat("_Boost", 1f * intensity);
        }

        void BuildPool(RocketVFXTuning t)
        {
            flyers = new Flyer[Mathf.Max(2, t.poolSize)];
            for (int i = 0; i < flyers.Length; i++)
            {
                var go = new GameObject("Rocket " + i);
                go.transform.SetParent(root, false);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sortingLayerName = t.sortingLayer;
                sr.sortingOrder = t.sortingOrder;
                sr.maskInteraction = SpriteMaskInteraction.None;

                var glowTrail = go.AddComponent<TrailRenderer>();
                glowTrail.sharedMaterial = glowTrailMaterial;
                glowTrail.sortingLayerName = t.sortingLayer;
                glowTrail.sortingOrder = t.sortingOrder - 4;
                glowTrail.numCapVertices = 3;
                glowTrail.numCornerVertices = 2;
                glowTrail.autodestruct = false;
                glowTrail.emitting = false;

                var coreObject = new GameObject("White-hot core");
                coreObject.transform.SetParent(go.transform, false);
                var coreTrail = coreObject.AddComponent<TrailRenderer>();
                coreTrail.sharedMaterial = coreTrailMaterial;
                coreTrail.sortingLayerName = t.sortingLayer;
                coreTrail.sortingOrder = t.sortingOrder - 1;
                coreTrail.numCapVertices = 4;
                coreTrail.autodestruct = false;
                coreTrail.emitting = false;

                var flameObject = new GameObject("Flame ribbon");
                flameObject.transform.SetParent(go.transform, false);
                var flame = flameObject.AddComponent<LineRenderer>();
                flame.sharedMaterial = ribbonInstance;
                flame.sortingLayerName = t.sortingLayer;
                flame.sortingOrder = t.sortingOrder - 3;
                flame.useWorldSpace = true;
                flame.numCapVertices = 0;
                flame.numCornerVertices = 3;
                flame.textureMode = LineTextureMode.Stretch;
                flame.widthCurve = AnimationCurve.Linear(0, 1, 1, 1);
                flame.enabled = false;
                flame.positionCount = 0;

                var nozzleObject = new GameObject("Nozzle flame");
                nozzleObject.transform.SetParent(go.transform, false);
                nozzleObject.AddComponent<MeshFilter>().sharedMesh = nozzleMesh;
                var nozzleRenderer = nozzleObject.AddComponent<MeshRenderer>();
                nozzleRenderer.sharedMaterial = fireInstance;
                nozzleRenderer.sortingLayerName = t.sortingLayer;
                nozzleRenderer.sortingOrder = t.sortingOrder - 2;
                nozzleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                nozzleRenderer.receiveShadows = false;
                var props = new MaterialPropertyBlock();
                props.SetFloat("_Phase", i * .37f);
                nozzleRenderer.SetPropertyBlock(props);
                nozzleRenderer.enabled = false;

                flyers[i] = new Flyer
                {
                    go = go, sprite = sr, glow = glowTrail, core = coreTrail,
                    flame = flame, nozzle = nozzleRenderer
                };
                go.SetActive(false);
            }
        }

        ParticleSystem CreateParticles(string name, Material material, int capacity, bool expanding, bool spin, RocketVFXTuning t)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.stopAction = ParticleSystemStopAction.None;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Shape;
            main.useUnscaledTime = false;
            main.startSpeed = 0;
            main.startSize = 1;
            main.gravityModifier = 0;
            main.maxParticles = capacity;

            var emission = ps.emission; emission.enabled = false;
            var shape = ps.shape; shape.enabled = false;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.maskInteraction = SpriteMaskInteraction.None;
            renderer.sortingLayerName = t.sortingLayer;
            renderer.sortingOrder = t.sortingOrder + 2;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            var colour = ps.colorOverLifetime; colour.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(.85f, .28f), new GradientAlphaKey(0, 1) });
            colour.color = gradient;

            var size = ps.sizeOverLifetime; size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1, expanding
                ? AnimationCurve.EaseInOut(0, .45f, 1, 1.25f)
                : new AnimationCurve(new Keyframe(0, .9f), new Keyframe(.2f, 1), new Keyframe(1, .12f)));

            var rotation = ps.rotationOverLifetime;
            rotation.enabled = spin;
            rotation.z = new ParticleSystem.MinMaxCurve(-2.2f, 2.2f);

            ps.useAutoRandomSeed = false;
            ps.randomSeed = (uint)(37183 + capacity);
            return ps;
        }

        // ------------------------------------------------------------------ flight

        static float FlightScale(Sprite sprite, float pitch, float rocketScale) =>
            sprite ? pitch * rocketScale / Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y) : 1f;

        static Gradient ExhaustGradient(bool core)
        {
            var g = new Gradient();
            g.SetKeys(
                new[]
                {
                    new GradientColorKey(core ? new Color(1, .99f, .88f) : new Color(1, .68f, .13f), 0),
                    new GradientColorKey(core ? Color.white : new Color(1, .88f, .42f), .56f),
                    new GradientColorKey(Color.white, .82f), new GradientColorKey(Color.white, 1)
                },
                new[]
                {
                    new GradientAlphaKey(core ? .98f : .62f, 0), new GradientAlphaKey(core ? .90f : .48f, .56f),
                    new GradientAlphaKey(core ? .35f : .11f, .84f), new GradientAlphaKey(0, 1)
                });
            return g;
        }

        /// <summary>Distance in cells until the whole rocket is outside the camera, so it always
        /// exits cleanly instead of stopping at the last cell.</summary>
        float ExitDistance(Vector3 origin, Vector3 direction, float pitch, float sweptCells, RocketVFXTuning t)
        {
            var cam = viewCamera ? viewCamera : Camera.main;
            if (!cam) return sweptCells + 3f;
            float depth = Vector3.Dot(origin - cam.transform.position, cam.transform.forward);
            var min = cam.ViewportToWorldPoint(new Vector3(0, 0, depth));
            var max = cam.ViewportToWorldPoint(new Vector3(1, 1, depth));
            float boundary = direction.x != 0
                ? (direction.x > 0 ? max.x - origin.x : origin.x - min.x)
                : (direction.y > 0 ? max.y - origin.y : origin.y - min.y);
            return Mathf.Max(sweptCells + .8f, boundary / pitch + t.screenExitPadding);
        }

        Flyer Take()
        {
            if (flyers == null) return null;
            foreach (var f in flyers)
            {
                if (f.used) continue;
                f.used = true;
                f.go.SetActive(true);
                f.sprite.enabled = true;
                f.nozzle.enabled = false;
                f.flame.enabled = false;
                f.flame.positionCount = 0;
                f.glow.emitting = false;
                f.core.emitting = false;
                f.glow.Clear();
                f.core.Clear();
                return f;
            }
            return null;
        }

        IEnumerator Fly(Vector3 origin, RocketDirection direction, float sweptCells, float pitch)
        {
            var t = Tuning;
            int dir = (int)direction;
            Vector3 axis = AxisOf[dir];
            float speed = Mathf.Max(1f, t.travelSpeed);
            float exhaustLength = Mathf.Max(.5f, t.exhaustLengthInCells);
            // Trail lifetime is derived from length and speed, so the plume keeps its cell length
            // at any speed. Never slow the rocket down to make the trail longer.
            float exhaustLife = exhaustLength / speed;
            float exit = ExitDistance(origin, axis, pitch, sweptCells, t);

            var f = Take();
            if (f == null) yield break;

            var sprite = heads != null ? heads[HeadIndex(direction)] : null;
            f.sprite.sprite = sprite;
            float fit = FlightScale(sprite, pitch, t.rocketScale);
            f.go.transform.position = origin + Vector3.back * .6f;
            f.go.transform.rotation = Quaternion.identity;
            f.go.transform.localScale = Vector3.one * fit;

            f.nozzle.transform.localRotation = Quaternion.Euler(0, 0, NozzleAngle[dir]);
            f.nozzle.transform.localScale = Vector3.one * (pitch / Mathf.Max(.0001f, fit));

            f.glow.time = exhaustLife;
            f.glow.minVertexDistance = .035f * pitch;
            f.glow.widthMultiplier = .76f * pitch;
            f.glow.widthCurve = new AnimationCurve(
                new Keyframe(0, 1), new Keyframe(.50f, .80f), new Keyframe(.84f, .32f), new Keyframe(1, .015f));
            f.glow.colorGradient = ExhaustGradient(false);

            f.flame.widthMultiplier = .90f * pitch;
            f.flameProperties.SetFloat("_TravelFraction", 0);
            f.flameProperties.SetFloat("_Phase", launchCount * .73f + dir);
            f.flame.SetPropertyBlock(f.flameProperties);
            f.flame.startColor = f.flame.endColor = new Color(1, 1, 1, .88f);
            f.flame.positionCount = 2;
            f.flame.SetPosition(0, origin + Vector3.back * .6f);
            f.flame.SetPosition(1, origin + Vector3.back * .6f);

            f.core.time = exhaustLife;
            f.core.minVertexDistance = .02f * pitch;
            f.core.widthMultiplier = .17f * pitch;
            f.core.widthCurve = new AnimationCurve(
                new Keyframe(0, 1), new Keyframe(.60f, .90f), new Keyframe(.88f, .22f), new Keyframe(1, 0));
            f.core.colorGradient = ExhaustGradient(true);

            launchCount++;

            // Short anticipation squash, then ignition. Flight itself keeps one constant velocity.
            float anticipation = 0f, window = Mathf.Max(.0001f, t.anticipation);
            while (anticipation < t.anticipation)
            {
                if (!Paused) anticipation += Time.deltaTime;
                float compression = 1f - .055f * Mathf.Sin(Mathf.Clamp01(anticipation / window) * Mathf.PI);
                f.go.transform.localScale = Vector3.one * fit * compression;
                yield return null;
            }

            Impact(origin, true, pitch);
            Launched?.Invoke(origin, direction);
            f.go.transform.localScale = Vector3.one * fit;
            f.glow.Clear();
            f.core.Clear();
            f.glow.emitting = true;
            f.core.emitting = true;
            f.flame.enabled = true;
            f.nozzle.enabled = true;

            float distance = 0f;
            int stepDone = -1, sparkStepDone = -1;
            int lastCell = Mathf.FloorToInt(sweptCells);
            while (distance < exit)
            {
                if (Paused) { yield return null; continue; }
                distance = Mathf.Min(exit, distance + speed * Time.deltaTime);
                // Contact follows the nose of the directional flight sprite, not its centre.
                float noseDistance = distance + t.rocketScale;
                Vector3 head = origin + axis * distance * pitch;

                f.go.transform.position = head + Vector3.back * .6f;
                float visibleCells = Mathf.Min(distance, exhaustLength);
                f.flame.SetPosition(0, head - axis * visibleCells * pitch + Vector3.back * .6f);
                f.flame.SetPosition(1, head + Vector3.back * .6f);
                f.flameProperties.SetFloat("_TravelFraction", visibleCells / exhaustLength);
                f.flame.SetPropertyBlock(f.flameProperties);

                for (int sample = sparkStepDone + 1; sample <= Mathf.FloorToInt(distance / ExhaustSampleCells); sample++)
                    EmitExhaustSparks(origin + axis * (sample * ExhaustSampleCells) * pitch,
                        axis, pitch, speed, exhaustLength, sample * ExhaustSampleCells, t);

                for (int step = stepDone + 1; step <= Mathf.Min(lastCell, Mathf.FloorToInt(noseDistance)); step++)
                {
                    Vector3 cell = origin + axis * step * pitch;
                    Impact(cell, false, pitch);
                    CellReached?.Invoke(cell, step);
                }

                stepDone = Mathf.Max(stepDone, Mathf.FloorToInt(noseDistance));
                sparkStepDone = Mathf.FloorToInt(distance / ExhaustSampleCells);
                yield return null;
            }

            Exited?.Invoke(f.go.transform.position, direction);
            Impact(f.go.transform.position, true, pitch);
            yield return Retire(f);
        }

        IEnumerator Retire(Flyer f)
        {
            f.sprite.enabled = false;
            f.nozzle.enabled = false;
            f.glow.emitting = false;
            f.core.emitting = false;

            // Let the bounded plume and its speed-derived trails finish before pool reuse.
            float elapsed = 0f, lifetime = Mathf.Max(f.glow.time, f.core.time) + .08f;
            while (elapsed < lifetime)
            {
                if (!Paused) elapsed += Time.deltaTime;
                float opacity = .88f * (1 - Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / Mathf.Max(.1f, lifetime - .08f))));
                f.flame.startColor = f.flame.endColor = new Color(1, 1, 1, opacity);
                yield return null;
            }

            f.glow.Clear();
            f.core.Clear();
            f.flame.enabled = false;
            f.flame.positionCount = 0;
            f.go.SetActive(false);
            f.used = false;
            f.sprite.enabled = true;
        }

        // ------------------------------------------------------------------ emission

        void Emit(ParticleSystem ps, Vector3 p, int n, float size, float speed, float life, Color tint, float pitch)
        {
            if (!ps || n <= 0) return;
            var e = new ParticleSystem.EmitParams
            {
                position = p + Vector3.back * .5f,
                startLifetime = life,
                startSize = size * pitch,
                startColor = tint,
                applyShapeToPosition = false
            };
            for (int i = 0; i < n; i++)
            {
                float angle = i * Mathf.PI * 2 / n + Range(-.25f, .25f);
                e.velocity = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * speed * pitch * Range(.65f, 1.2f);
                e.rotation = Range(0, 360);
                ps.Emit(e, 1);
            }
            if (!ps.isPlaying) ps.Play(false);
        }

        void Impact(Vector3 position, bool end, float pitch)
        {
            Emit(glowFlash, position, 1, end ? 1.15f : .70f, 0, end ? .20f : .13f, new Color(1, .96f, .70f, end ? .85f : .72f), pitch);
            Emit(impactRings, position, 1, end ? 1.32f : .83f, 0, .28f, new Color(1, .70f, .19f, .85f), pitch);
            Emit(impactStars, position, end ? 22 : 10, end ? .22f : .17f, end ? 2.5f : 1.6f, .43f, new Color(1, .96f, .75f, 1), pitch);
        }

        void EmitExhaustSparks(Vector3 nozzlePoint, Vector3 direction, float pitch, float speed, float length, float travelled, RocketVFXTuning t)
        {
            Vector3 across = new Vector3(-direction.y, direction.x, 0);

            int count = Mathf.Max(0, Mathf.RoundToInt(4 * t.starDensity));
            for (int i = 0; i < count; i++)
            {
                float behind = Mathf.Min(travelled, Range(.05f, .30f)), drift = Range(.08f, .28f);
                // Each star draws the fraction of the plume it is allowed to reach. A uniform draw
                // keeps every star alive near the nozzle and lets progressively fewer survive
                // toward the tail, so the trail thins out instead of ending as a block.
                float reach = Range(.43f, 1f);
                float lifetime = Mathf.Max(.05f, (length * reach - behind - .10f) / (speed + drift));
                var e = new ParticleSystem.EmitParams
                {
                    position = nozzlePoint - direction * pitch * behind + across * pitch * Range(-.20f, .20f) + Vector3.back * .5f,
                    velocity = (-direction * drift + across * Range(-.08f, .08f)) * pitch,
                    startSize = pitch * (i == 0 && Range(0, 1) < .12f ? Range(.32f, .43f) : Range(.12f, .25f)),
                    startLifetime = lifetime,
                    rotation = Range(0, 360),
                    startColor = new Color(1, 1, 1, Range(.75f, 1f)),
                    applyShapeToPosition = false
                };
                exhaustStars.Emit(e, 1);
            }

            // Gold embers carry the yellow far past the first cell, thinning out by mid-plume.
            for (int i = 0; i < 2; i++)
            {
                float emberReach = Range(.30f, .66f);
                var e = new ParticleSystem.EmitParams
                {
                    position = nozzlePoint - direction * pitch * Mathf.Min(travelled, .18f) + across * pitch * Range(-.20f, .20f) + Vector3.back * .5f,
                    velocity = (-direction * .18f + across * Range(-.15f, .15f)) * pitch,
                    startSize = pitch * Range(.07f, .17f),
                    startLifetime = length * emberReach / (speed + .18f),
                    startColor = new Color(1, .82f, .28f, .85f),
                    applyShapeToPosition = false
                };
                embers.Emit(e, 1);
            }

            // One low-opacity wisp follows each spatial sample. Its lifetime matches the plume
            // distance and its alpha curve hides it until the energy tail, so smoke cannot form a
            // detached bulb or a hard cap behind the ribbon.
            if (t.smokeAmount > 0f)
            {
                float smokeDrift = Range(.05f, .12f);
                var wisp = new ParticleSystem.EmitParams
                {
                    position = nozzlePoint + across * pitch * Range(-.10f, .10f) + Vector3.back * .5f,
                    velocity = (-direction * smokeDrift + across * Range(-.06f, .06f)) * pitch,
                    startSize = pitch * Range(.30f, .47f),
                    startLifetime = length / (speed + smokeDrift) * Range(.96f, 1.04f),
                    rotation = Range(0, 360),
                    startColor = new Color(1, 1, .96f, Mathf.Clamp01(Range(.55f, .78f) * t.smokeAmount)),
                    applyShapeToPosition = false
                };
                smoke.Emit(wisp, 1);
            }

            if (!exhaustStars.isPlaying) exhaustStars.Play(false);
            if (!embers.isPlaying) embers.Play(false);
            if (!smoke.isPlaying) smoke.Play(false);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (Application.isPlaying && fireInstance) ApplyFlameIntensity(Tuning.flameIntensity);
        }
#endif
    }
}
