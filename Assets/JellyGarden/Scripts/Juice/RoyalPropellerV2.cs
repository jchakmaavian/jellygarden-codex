using System.Collections.Generic;
using UnityEngine;

namespace JellyGarden.Scripts.Juice
{
    /// <summary>
    /// Propeller V2: a live rig rather than a pre-rendered sprite sequence. The body and the rotor
    /// are separate transforms, so scale response, torque reaction, tilt and rotor speed are driven
    /// per frame and the flight path stays entirely under gameplay control.
    ///
    /// The rotor turns inside its own tilted plane through a transform chain that mirrors the
    /// authored artwork: roll out, flatten, turn, restore. At zero it is the identity, so the seated
    /// pose is the untouched painted propeller. Each blade flips its sorting either side of the body
    /// as it passes the far half of the sweep, which is what keeps the sphere reading as a sphere.
    ///
    /// V1 (Design/Animation/Propeller-*-v1) stays untouched for comparison; nothing here reads it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoyalPropellerV2 : MonoBehaviour
    {
        public enum Phase { Free, Reveal, Seated, Takeoff, Flight, Retire }

        // Measured from the artwork (512 px canvas, 100 pixels per unit).
        public const float Canvas = 512f, Ppu = 100f;
        const float HubArtX = 259.67f, HubArtY = 166.70f;
        const float SphereArtX = 251.17f, SphereArtY = 293.69f, SphereArtR = 145.99f;
        /// <summary>Foreshortening of the rotor plane and its roll, solved from the painted blades.</summary>
        public const float RotorK = .585f, RotorRollDeg = -4f;
        static readonly float[] Azimuth = { 35.8f, 155.8f, 275.8f };

        // Authored timings, seconds.
        public const float RevealDuration = .55f;
        public const float TakeoffDuration = .42f;
        /// <summary>The takeoff flash, the neighbour shock pulse and the star burst all land here.</summary>
        public const float TakeoffBlastAt = .13f;
        public const float RetireDuration = .18f;

        // Rotor speed, degrees per second.
        const float IdleSpin = 9f, RevealEntrySpin = 1500f, FlightSpin = 2600f;

        /// <summary>
        /// Seated size against the cell, measured on the body rather than the blade span: the
        /// sphere is 292 px across, and the blades are meant to overhang so a booster outweighs
        /// an ordinary piece.
        /// </summary>
        public const float CellFit = .88f / 2.92f;
        /// <summary>A booster in the air reads bigger than one sitting in its cell.</summary>
        const float FlightScale = 1.38f;

        static readonly List<RoyalPropellerV2> pool = new List<RoyalPropellerV2>();
        public static int PoolSize => pool.Count;
        public static int Active { get { int n = 0; foreach (var p in pool) if (p && p.Current != Phase.Free) n++; return n; } }

        public Phase Current { get; private set; } = Phase.Free;
        public float Age { get; private set; }
        public float Spin { get; private set; }
        public float BodyScale { get; private set; } = 1;
        /// <summary>Set once the takeoff has finished; gameplay may drive <see cref="Place"/> from here.</summary>
        public bool FlightReady { get; private set; }

        /// <summary>
        /// Bumped every time the rig leaves the pool. A holder that kept a reference across a
        /// <see cref="ClearAll"/> compares it, so a recycled rig is never driven by its old owner.
        /// </summary>
        public int Serial { get; private set; }

        static int serials;

        Transform rig, rotorRoot, rotorSquash, rotorTurn, rotorFlat;
        SpriteRenderer body, hub, blurBack, blurFront;
        readonly SpriteRenderer[] blades = new SpriteRenderer[3];
        float cell = 1, baseOrder, flightNoise, spinRate;

        /// <summary>Degrees per second above which the rotor reads purely as a blurred disc.</summary>
        const float BlurFull = 1250f, BlurStart = 260f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { pool.Clear(); }

        static Sprite Art(string name) => Resources.Load<Sprite>("RoyalPropellerV2/PropellerV2_" + name);
        public static bool Ready => Art("Body") && Art("Hub") && Art("Blade0");

        /// <summary>Takes a rig from the pool, seated on <paramref name="position"/> as its contact point.</summary>
        /// <param name="spinSeed">Rotor phase to start from, so a takeoff can continue the seated blade angle.</param>
        public static RoyalPropellerV2 Take(Vector3 position, float cellSize, int sortingLayer, int sortingOrder, float spinSeed = 0f)
        {
            if (!Application.isPlaying || !Ready) return null;
            RoyalPropellerV2 rig = null;
            foreach (var candidate in pool) if (candidate && candidate.Current == Phase.Free) { rig = candidate; break; }
            if (!rig)
            {
                var host = new GameObject("Propeller V2");
                rig = host.AddComponent<RoyalPropellerV2>();
                rig.Build();
                pool.Add(rig);
            }
            rig.Serial = ++serials;
            rig.Reset(position, cellSize, sortingLayer, sortingOrder, spinSeed);
            return rig;
        }

        public static void ClearAll() { foreach (var p in pool) if (p) p.Release(); }

        void Build()
        {
            rig = new GameObject("Rig").transform;
            rig.SetParent(transform, false);

            body = NewRenderer(rig, "Body", Art("Body"));
            // The root is the board contact point, so the art sits above it by the sphere's radius.
            body.transform.localPosition = ToLocal(Canvas * .5f - SphereArtX, SphereArtY + SphereArtR - Canvas * .5f);

            rotorRoot = new GameObject("Rotor").transform;
            rotorRoot.SetParent(rig, false);
            rotorRoot.localPosition = ToLocal(HubArtX - SphereArtX, SphereArtY + SphereArtR - HubArtY);
            rotorRoot.localRotation = Quaternion.Euler(0, 0, -RotorRollDeg);

            rotorSquash = new GameObject("Plane").transform;
            rotorSquash.SetParent(rotorRoot, false);
            rotorSquash.localScale = new Vector3(1, RotorK, 1);

            rotorTurn = new GameObject("Turn").transform;
            rotorTurn.SetParent(rotorSquash, false);

            rotorFlat = new GameObject("Flat").transform;
            rotorFlat.SetParent(rotorTurn, false);
            rotorFlat.localScale = new Vector3(1, 1 / RotorK, 1);

            var unroll = new GameObject("Unroll").transform;
            unroll.SetParent(rotorFlat, false);
            unroll.localRotation = Quaternion.Euler(0, 0, RotorRollDeg);

            // Blade canvases share the body's 512 px frame, so they are offset back onto the hub.
            var bladeOffset = ToLocal(Canvas * .5f - HubArtX, HubArtY - Canvas * .5f);
            for (int i = 0; i < 3; i++)
            {
                blades[i] = NewRenderer(unroll, "Blade" + i, Art("Blade" + i));
                blades[i].transform.localPosition = bladeOffset;
            }
            hub = NewRenderer(unroll, "Hub", Art("Hub"));
            hub.transform.localPosition = bladeOffset;

            // A fast rotor reads as a disc, not as three crisp blades. This is the real painted
            // rotor swept through a full turn, split either side of the body so the sphere still
            // occludes the far half. It crossfades in with speed; the crisp blades fade out.
            var blur = Art("RotorBlur");
            blurBack = NewRenderer(rig, "Rotor blur (far)", blur);
            blurFront = NewRenderer(rig, "Rotor blur (near)", blur);
            blurBack.transform.localPosition = blurFront.transform.localPosition = body.transform.localPosition;
            gameObject.SetActive(false);
        }

        static Vector2 ToLocal(float artDx, float artDy) => new Vector2(artDx / Ppu, artDy / Ppu);

        static SpriteRenderer NewRenderer(Transform parent, string name, Sprite sprite)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var view = go.AddComponent<SpriteRenderer>();
            view.sprite = sprite;
            // An enlarged booster must never be cut by a board mask.
            view.maskInteraction = SpriteMaskInteraction.None;
            return view;
        }

        void Reset(Vector3 position, float cellSize, int sortingLayer, int sortingOrder, float spinSeed)
        {
            cell = Mathf.Max(.01f, cellSize);
            baseOrder = sortingOrder;
            gameObject.SetActive(true);
            transform.position = position;
            transform.rotation = Quaternion.identity;
            Current = Phase.Reveal;
            Age = 0;
            Spin = spinSeed;
            FlightReady = false;
            flightNoise = Random.value * 10f;
            body.sortingLayerID = hub.sortingLayerID = sortingLayer;
            foreach (var blade in blades) blade.sortingLayerID = sortingLayer;
            SetTint(Color.white);
            Pose(Spin, 1, 0, 0, Vector2.zero);
        }

        public void SetTint(Color tint)
        {
            body.color = hub.color = tint;
            foreach (var blade in blades) blade.color = tint;
        }

        /// <summary>Hides the rig and returns it to the pool. Nothing is left drawing.</summary>
        public void Release()
        {
            Current = Phase.Free;
            Age = 0;
            Spin = 0;
            FlightReady = false;
            if (rig) rig.localScale = Vector3.one;
            gameObject.SetActive(false);
        }

        /// <summary>Gameplay drives the rig here during flight; the rig keeps owning its own pose.</summary>
        public void Place(Vector3 position) { transform.position = position; }

        /// <summary>Begins the takeoff. Reveal and idle end here.</summary>
        /// <summary>Skips the reveal and holds the seated idle pose.</summary>
        public void Seat() { Current = Phase.Seated; Age = 0; Spin = 0; }

        public void Launch()
        {
            if (Current == Phase.Free) return;
            Current = Phase.Takeoff;
            Age = 0;
            FlightReady = false;
        }

        /// <summary>Plays the short retire fade once gameplay has finished with the rig.</summary>
        public void Retire()
        {
            if (Current == Phase.Free || Current == Phase.Retire) return;
            Current = Phase.Retire;
            Age = 0;
        }

        void LateUpdate()
        {
            var level = LevelManager.THIS;
            bool paused = level && level.gameStatus == GameState.Pause;
            if (!paused) Age += Time.deltaTime;

            switch (Current)
            {
                case Phase.Reveal: RevealPose(); break;
                case Phase.Seated: SeatedPose(); break;
                case Phase.Takeoff: TakeoffPose(); break;
                case Phase.Flight: FlightPose(); break;
                case Phase.Retire: RetirePose(); break;
            }
            // The rotor angle is periodic, so keep it small: a long idle would otherwise
            // erode float precision on a value that only ever feeds a rotation and a sine.
            if (Spin < 0f || Spin >= 360f) Spin = Mathf.Repeat(Spin, 360f);
            ApplySorting();
        }

        void RevealPose()
        {
            float t = Mathf.Clamp01(Age / RevealDuration);
            // Compact anticipation, a fast arrival, one overshoot and a controlled counter-settle.
            float scale = Curve(t, 0f, .22f, .07f, .70f, .16f, 1.16f, .28f, .93f, .42f, 1.04f, .62f, .99f, .82f, 1f, 1f, 1f);
            float squash = Curve(t, 0f, .26f, .08f, -.12f, .18f, .10f, .30f, -.05f, .48f, .02f, .70f, 0f, 1f, 0f);
            float tilt = Curve(t, 0f, -8f, .12f, 6f, .26f, -3.4f, .40f, 1.5f, .58f, -.5f, .80f, 0f, 1f, 0f);
            float rise = Curve(t, 0f, .30f, .14f, -.06f, .30f, .03f, .52f, -.01f, .80f, 0f, 1f, 0f) * cell;
            // The rotor keeps its own timing: it arrives fast and stops before the body finishes settling.
            spinRate = Curve(t, 0f, RevealEntrySpin, .30f, RevealEntrySpin * .55f, .62f, 160f, .86f, IdleSpin, 1f, IdleSpin);
            Spin += spinRate * Time.deltaTime;
            Pose(Spin, scale, squash, tilt, new Vector2(0, rise));
            if (t >= 1f) { Current = Phase.Seated; Age = 0; }
        }

        void SeatedPose()
        {
            // Nearly static so the board stays readable: a slow breath, a slow blade drift, no drift in place.
            float breath = Mathf.Sin(Age * 2.1f);
            spinRate = IdleSpin;
            Spin += IdleSpin * Time.deltaTime;
            Pose(Spin, 1f + .006f * breath, .004f * breath, 0f, new Vector2(0, .006f * cell * breath));
        }

        void TakeoffPose()
        {
            float t = Mathf.Clamp01(Age / TakeoffDuration);
            // Compress and dip, then the rotor tears it off the board.
            float scale = Curve(t, 0f, 1f, .14f, .93f, .30f, 1.16f, .52f, 1.28f, 1f, FlightScale);
            float squash = Curve(t, 0f, 0f, .14f, .13f, .30f, -.08f, .60f, -.02f, 1f, -.015f);
            // The body reacts against the rotor's torque before it leans into the climb.
            float tilt = Curve(t, 0f, 0f, .16f, 7f, .34f, -5.5f, .68f, -9f, 1f, -11f);
            float rise = Curve(t, 0f, 0f, .16f, -.14f, .34f, .18f, .66f, .52f, 1f, .72f) * cell;
            spinRate = Curve(t, 0f, IdleSpin, .10f, 420f, .26f, 1500f, .55f, 2300f, 1f, FlightSpin);
            Spin += spinRate * Time.deltaTime;
            Pose(Spin, scale, squash, tilt, new Vector2(0, rise));
            if (t >= 1f) { Current = Phase.Flight; Age = 0; FlightReady = true; }
        }

        void FlightPose()
        {
            // Fast rotor, a held aerodynamic lean and a small hover vibration. No flame, no exhaust.
            spinRate = FlightSpin;
            Spin += FlightSpin * Time.deltaTime;
            float hover = Mathf.Sin((Age + flightNoise) * 17f) * .012f + Mathf.Sin((Age + flightNoise) * 29f) * .005f;
            float lean = -11f + Mathf.Sin((Age + flightNoise) * 6f) * 1.6f;
            Pose(Spin, FlightScale, -.015f, lean, new Vector2(0, (.72f + hover) * cell));
        }

        void RetirePose()
        {
            float t = Mathf.Clamp01(Age / RetireDuration);
            spinRate = FlightSpin * (1 - t);
            Spin += spinRate * Time.deltaTime;
            float scale = Mathf.Lerp(FlightScale, FlightScale * .45f, t * t);
            SetTint(new Color(1, 1, 1, 1 - t));
            Pose(Spin, scale, 0, -11f, new Vector2(0, .72f * cell));
            if (t >= 1f) Release();
        }

        void Pose(float spinDeg, float scale, float squash, float tiltDeg, Vector2 offset)
        {
            BodyScale = scale;
            // The painted propeller is 421 px wide inside its 512 px canvas, so this lands the
            // seated booster on the same cell fill the other board pieces use.
            float fit = cell * CellFit;
            rig.localPosition = offset;
            rig.localRotation = Quaternion.Euler(0, 0, tiltDeg);
            rig.localScale = new Vector3(fit * scale * (1 + squash), fit * scale / (1 + squash), 1);
            rotorTurn.localRotation = Quaternion.Euler(0, 0, -spinDeg);
        }

        void ApplySorting()
        {
            int order = Mathf.RoundToInt(baseOrder);
            body.sortingOrder = order;
            hub.sortingOrder = order + 2;
            blurBack.sortingOrder = order - 1;
            blurFront.sortingOrder = order + 1;
            float blur = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(BlurStart, BlurFull, spinRate));
            var tint = body.color;
            // Half the disc either side of the body keeps the near/far read while it spins.
            blurBack.color = blurFront.color = new Color(tint.r, tint.g, tint.b, tint.a * blur * .62f);
            blurBack.enabled = blurFront.enabled = blur > .01f;
            for (int i = 0; i < 3; i++)
            {
                float psi = (Azimuth[i] + Spin) * Mathf.Deg2Rad;
                // The far half of the sweep passes behind the sphere.
                blades[i].sortingOrder = Mathf.Sin(psi) > 0 ? order - 1 : order + 1;
                blades[i].color = new Color(tint.r, tint.g, tint.b, tint.a * (1 - blur));
                blades[i].enabled = blur < .99f;
            }
        }

        /// <summary>Piecewise monotone interpolation through (t, value) pairs.</summary>
        static float Curve(float t, params float[] keys)
        {
            int n = keys.Length / 2;
            if (t <= keys[0]) return keys[1];
            if (t >= keys[(n - 1) * 2]) return keys[(n - 1) * 2 + 1];
            for (int i = 0; i < n - 1; i++)
            {
                float x0 = keys[i * 2], x1 = keys[(i + 1) * 2];
                if (t > x1) continue;
                float u = Mathf.InverseLerp(x0, x1, t);
                return Mathf.Lerp(keys[i * 2 + 1], keys[(i + 1) * 2 + 1], u * u * (3 - 2 * u));
            }
            return keys[(n - 1) * 2 + 1];
        }
    }
}
