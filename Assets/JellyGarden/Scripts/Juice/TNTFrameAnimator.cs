using UnityEngine;

namespace JellyGarden.Scripts.Juice
{
    /// <summary>
    /// Board TNT drawn from the approved frame sequences (Design/Animation/TNT-Reveal-v1 and
    /// TNT-Idle-v1): the Reveal plays once at the approved gameplay speed, then the seamless Idle
    /// loops until the TNT is activated. The Item keeps its static sprite, hidden, for cell fit,
    /// target matching and hint geometry; the explosion flow still starts from that static artwork.
    /// </summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(1000)]
    public sealed class TNTFrameAnimator : MonoBehaviour
    {
        public enum Phase { Stopped, Reveal, Idle }

        public const int FrameCount = 120;
        public const float SourceFps = 60f;
        public const float IdleDuration = FrameCount / SourceFps;
        /// <summary>
        /// The Reveal runs at its authored 60 fps, so every rendered frame at 60 fps shows the next
        /// sequence frame and each beat (fuse, ignition, overshoot, rebound, settle) is legible.
        /// </summary>
        public const float RevealDuration = FrameCount / SourceFps;
        /// <summary>
        /// Peak size of the Reveal overshoot, as a multiple of the settled board size. Measured off the
        /// Royal Match reference: its barrel settles at 106 px and peaks at 125 px, so 1.18. The TNT
        /// never covers more than its own cell. This used to be 3, which towered over the board.
        /// </summary>
        public const float RevealZoom = 1.18f;
        /// <summary>
        /// Height of the settled TNT's visual centre above the barrel-base pivot, in sprite units:
        /// frames 117-120 span rows 139-484 of the 512 canvas, so the centre is 168.5 px up at 100 PPU.
        /// </summary>
        const float PivotToCentre = 1.685f;
        /// <summary>
        /// How much of the enlargement grows around that centre instead of straight up off the barrel
        /// base. Zero towers three cells above the cell; one hangs as far below it.
        /// </summary>
        const float CentreBias = .55f;
        /// <summary>Extra sorting order while the Reveal is larger than the cell, so nothing overlaps it.</summary>
        const int RevealSortingBoost = 6;

        /// <summary>
        /// The reference overshoot, sampled from its frames and mapped onto this Reveal clock: the art
        /// reaches full size, pops to 1.18, compresses to 0.88, rebounds once and is settled by 0.43 s.
        /// The authored sequence already plays its own appearance in the first frames, so this only
        /// adds the elastic response on top rather than a second growth.
        /// </summary>
        static readonly AnimationCurve zoomCurve = BuildZoomCurve();

        static AnimationCurve BuildZoomCurve()
        {
            var curve = new AnimationCurve(
                new Keyframe(0f, 1f), new Keyframe(.050f, RevealZoom), new Keyframe(.075f, 1.09f),
                new Keyframe(.100f, .88f), new Keyframe(.130f, 1.05f), new Keyframe(.165f, .99f),
                new Keyframe(.215f, 1f), new Keyframe(1f, 1f));
            for (int i = 0; i < curve.length; i++) curve.SmoothTangents(i, 0);
            // Smoothed tangents across two equal values bulge the segment between them, so both keys
            // of the settled hold have to be flat or the TNT swells again after it has landed.
            Flatten(curve, curve.length - 2, true, true);
            Flatten(curve, curve.length - 1, true, true);
            Flatten(curve, 0, true, false);
            return curve;
        }

        static void Flatten(AnimationCurve curve, int index, bool inTangent, bool outTangent)
        {
            var key = curve[index];
            if (inTangent) key.inTangent = 0;
            if (outTangent) key.outTangent = 0;
            curve.MoveKey(index, key);
        }

        /// <summary>Size multiplier over the Reveal, where <paramref name="t"/> is 0 to 1. 1 is board size.</summary>
        /// <summary>Not clamped to 1: the reference compresses below board size on the rebound.</summary>
        public static float ZoomAt(float t) => zoomCurve.Evaluate(Mathf.Clamp01(t));
        /// <summary>Frames draw the board art at 0.84 about its barrel base: art (256, 458) is frame (256, 480).</summary>
        public const float ContentScale = .84f;
        /// <summary>Sprite pivot on the barrel base (normalised, bottom-left origin).</summary>
        public static readonly Vector2 BasePivot = new Vector2(256f / 512f, (512f - 480f) / 512f);
        /// <summary>The same barrel base below the board sprite's centred pivot, in 100 PPU units.</summary>
        public static readonly Vector3 BaseOffset = new Vector3(0f, (256f - 458f) / 100f, 0f);
        const string ResourceRoot = "RoyalTNT/Frames/";

        static Sprite[] revealFrames, idleFrames;
        static bool missing;

        public Phase Current { get; private set; }
        /// <summary>Zero-based index of the frame on screen, or -1 while stopped.</summary>
        public int Frame { get; private set; } = -1;
        /// <summary>Size of the frame on screen relative to the settled board TNT. 1 once seated.</summary>
        public float Zoom { get; private set; } = 1f;
        public SpriteRenderer View => view;

        Item item;
        SpriteRenderer view;
        double age;
        int startFrame = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { missing = false; }

        public static bool Ready => Loaded(revealFrames) && Loaded(idleFrames);
        static bool Loaded(Sprite[] frames) => frames != null && frames[0] && frames[FrameCount - 1];
        public static Sprite RevealFrame(int index) => Ready ? revealFrames[Mathf.Clamp(index, 0, FrameCount - 1)] : null;
        public static Sprite IdleFrame(int index) => Ready ? idleFrames[Mathf.Clamp(index, 0, FrameCount - 1)] : null;

        /// <summary>Loads both sequences once. Without them, board TNT keeps the static art and procedural reveal.</summary>
        public static bool Prewarm()
        {
            if (Ready) return true;
            if (missing) return false;
            revealFrames = Load("Reveal/TNT_Reveal_");
            idleFrames = Load("Idle/TNT_Idle_");
            if (Ready) return true;
            missing = true;
            Debug.LogWarning("TNT frame sequences are missing under Resources/" + ResourceRoot + "; board TNT keeps its static artwork.");
            return false;
        }

        static Sprite[] Load(string prefix)
        {
            var frames = new Sprite[FrameCount];
            for (int i = 0; i < FrameCount; i++)
            {
                var source = Resources.Load<Sprite>(ResourceRoot + prefix + (i + 1).ToString("000"));
                if (!source) return null;
                var texture = source.texture;
                frames[i] = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), BasePivot, 100f, 0, SpriteMeshType.FullRect);
                frames[i].name = texture.name;
            }
            return frames;
        }

        /// <summary>Starts the Reveal for a TNT created by a match. False when the frames are unavailable.</summary>
        public static bool PlayReveal(Item item)
        {
            var animator = Attach(item);
            if (!animator) return false;
            animator.Begin(Phase.Reveal);
            return true;
        }

        /// <summary>
        /// Adds the animator to a board TNT. A TNT that appears without a reveal (an orb conversion,
        /// for example) joins the Idle loop at frame 001 in the same frame, so its static art never shows.
        /// </summary>
        public static TNTFrameAnimator Attach(Item item)
        {
            if (!Application.isPlaying || !item || !item.sprRenderer || item.destroying || item.currentType != ItemsTypes.PACKAGE || !RoyalEffectsProfile.Get || !Prewarm()) return null;
            var animator = item.GetComponent<TNTFrameAnimator>();
            if (!animator) animator = item.gameObject.AddComponent<TNTFrameAnimator>();
            if (animator.Current == Phase.Stopped) animator.Begin(Phase.Idle);
            return animator;
        }

        void Awake()
        {
            item = GetComponent<Item>();
            if (!item || !item.sprRenderer) { enabled = false; return; }
            var source = item.sprRenderer;
            var child = new GameObject("TNT frames");
            child.layer = source.gameObject.layer;
            child.transform.SetParent(source.transform, false);
            // Pivot on the art's barrel base; 1 / 0.84 = 1.19x restores the board size.
            child.transform.localPosition = BaseOffset;
            child.transform.localScale = Vector3.one / ContentScale;
            view = child.AddComponent<SpriteRenderer>();
            view.sharedMaterial = source.sharedMaterial;
            view.enabled = false;
        }

        void Begin(Phase phase)
        {
            if (!view) return;
            Current = phase;
            age = 0;
            // Frame 001 is on screen for the first rendered frame; the clock starts on the next one.
            startFrame = Time.frameCount;
            Apply();
        }

        void LateUpdate()
        {
            if (!item || !item.sprRenderer || item.destroying || item.currentType != ItemsTypes.PACKAGE)
            {
                // Activation hides the frames at once; the explosion flow owns the static art from here.
                Stop(item && item.sprRenderer && !item.destroying);
                return;
            }
            if (Current == Phase.Stopped)
            {
                if (Prewarm()) Begin(Phase.Idle);
                return;
            }
            var level = LevelManager.THIS;
            if (Time.frameCount != startFrame && !(level && level.gameStatus == GameState.Pause)) age += Time.deltaTime;
            Apply();
        }

        void Apply()
        {
            if (!Ready) { Stop(true); return; }
            if (Current == Phase.Reveal && age >= RevealDuration)
            {
                // Idle frame 001 repeats Reveal frame 120, so the loop continues on the same clock.
                Current = Phase.Idle;
                age -= RevealDuration;
            }
            if (Current == Phase.Idle && age >= IdleDuration) age %= IdleDuration;
            bool revealing = Current == Phase.Reveal;
            // Both sequences are authored at 60 fps, so the Reveal advances on the same clock as the Idle.
            Frame = revealing
                ? Mathf.Min(FrameCount - 1, (int)(age * SourceFps + 1e-6))
                : (int)(age * SourceFps + 1e-6) % FrameCount;
            Zoom = revealing ? ZoomAt((float)(age / RevealDuration)) : 1f;
            var source = item.sprRenderer;
            view.sprite = revealing ? revealFrames[Frame] : idleFrames[Frame];
            view.color = source.color;
            view.sortingLayerID = source.sortingLayerID;
            // One order above pieces normally, because the lift and flames cross into the neighbouring
            // cells; clearly above everything while the reveal is still larger than its cell.
            view.sortingOrder = source.sortingOrder + (Zoom > 1.001f ? RevealSortingBoost : 1);
            view.maskInteraction = source.maskInteraction;
            view.enabled = true;
            source.enabled = false;
            // The frames are drawn about the barrel base, so the base cannot drift while the size
            // changes. Part of the growth is shifted down so a big reveal sits around its cell
            // instead of towering out of it; both terms reach exactly the seated pose at Zoom 1.
            var pose = view.transform;
            pose.localScale = Vector3.one * (Zoom / ContentScale);
            // Only an enlargement needs pulling back down. A squash keeps the barrel base planted, so
            // the compression reads as weight instead of the whole TNT hopping up off the cell.
            pose.localPosition = new Vector3(BaseOffset.x, BaseOffset.y - CentreBias * PivotToCentre * Mathf.Max(0f, Zoom - 1f), BaseOffset.z);
        }

        void Stop(bool showStaticArt)
        {
            if (Current == Phase.Stopped && !(view && view.enabled)) return;
            Current = Phase.Stopped;
            Frame = -1;
            age = 0;
            Zoom = 1f;
            // A pooled piece must not inherit the previous reveal's size or lift.
            if (view)
            {
                view.enabled = false;
                view.sprite = null;
                view.transform.localScale = Vector3.one / ContentScale;
                view.transform.localPosition = BaseOffset;
            }
            if (showStaticArt) item.sprRenderer.enabled = true;
        }

        void OnDisable()
        {
            if (item && item.sprRenderer) Stop(!item.destroying);
        }

        void OnDestroy()
        {
            if (view) Destroy(view.gameObject);
        }
    }
}
