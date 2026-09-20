using UnityEngine;

namespace JellyGarden.Scripts.Juice
{
    /// <summary>
    /// Board Color Ball drawn from the approved frame sequences (Design/Animation/ColorBall-Reveal-v1,
    /// ColorBall-Idle-v1 and ColorBall-Boosting-v1). The Reveal plays once at its authored 60 fps,
    /// opening about three cells wide and easing into the cell, then the seamless Idle loops until the
    /// ball is activated. Activation is drawn by <see cref="ColorBallBoostView"/>, because the Item
    /// itself is consumed the moment the power fires.
    /// The Item keeps its static sprite, hidden, for cell fit, target matching and hint geometry.
    /// </summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(1000)]
    public sealed class ColorBallFrameAnimator : MonoBehaviour
    {
        public enum Phase { Stopped, Reveal, Idle }

        public const int RevealFrameCount = 60;
        public const int IdleFrameCount = 120;
        public const int BoostFrameCount = 108;
        public const float SourceFps = 60f;
        /// <summary>60 frames at the authored 60 fps: one sequence frame per rendered frame.</summary>
        public const float RevealDuration = RevealFrameCount / SourceFps;
        public const float IdleDuration = IdleFrameCount / SourceFps;
        /// <summary>One board cell occupies 112 source pixels; runtime sprites use 100 PPU.</summary>
        public const float ContentScale = 1.12f; // 112 authored pixels per cell / 100 PPU.
        /// <summary>
        /// All approved frames share the same centered canvas pivot. The art grows inside that canvas.
        /// No trimming or per-frame pivot correction is applied.
        /// </summary>
        public static readonly Vector2 BallPivot = new Vector2(.5f, .5f);
        /// <summary>The Reveal opens this many times the seated board size, then eases into the cell.</summary>
        public const float RevealZoom = 3f;
        /// <summary>Extra sorting order while a sequence is larger than its cell, so nothing overlaps it.</summary>
        public const int LargeSortingBoost = 6;
        const string ResourceRoot = "RoyalColorBall/Frames/";

        static Sprite[] revealFrames, idleFrames, boostFrames;
        static bool missing;

        /// <summary>3.0 -> 2.0 -> 1.3 -> 1.0 over the tail of the Reveal, smooth and flat at both ends.</summary>
        static readonly AnimationCurve revealZoomCurve = BuildRevealZoom();

        static AnimationCurve BuildRevealZoom()
        {
            var curve = new AnimationCurve(
                new Keyframe(0f, RevealZoom), new Keyframe(.55f, RevealZoom),
                new Keyframe(.72f, 2f), new Keyframe(.87f, 1.3f), new Keyframe(1f, 1f));
            for (int i = 0; i < curve.length; i++) curve.SmoothTangents(i, 0);
            // Smoothed tangents across two equal hold keys bulge the segment between them, which would
            // push the opening size above 3x before it came down. Both hold keys stay flat.
            Flatten(curve, 0);
            Flatten(curve, 1);
            Flatten(curve, curve.length - 1);
            return curve;
        }

        static void Flatten(AnimationCurve curve, int index)
        {
            var key = curve[index];
            key.inTangent = 0;
            key.outTangent = 0;
            curve.MoveKey(index, key);
        }

        /// <summary>Size multiplier over the Reveal, where <paramref name="t"/> is 0 to 1. 1 is board size.</summary>
        public static float RevealZoomAt(float t) => Mathf.Max(1f, revealZoomCurve.Evaluate(Mathf.Clamp01(t)));

        public Phase Current { get; private set; }
        /// <summary>Zero-based index of the frame on screen, or -1 while stopped.</summary>
        public int Frame { get; private set; } = -1;
        /// <summary>Size of the frame on screen relative to the seated board ball. 1 once seated.</summary>
        public float Zoom { get; private set; } = 1f;
        public SpriteRenderer View => view;

        /// <summary>
        /// World scale of a seated (1x) Color Ball frame, and the piece sorting it draws with. The
        /// activation view is detached from the board, so it reads the size the ball had on the cell.
        /// </summary>
        public static float SeatedWorldScale { get; private set; }
        public static int SeatedSortingLayer { get; private set; }
        public static int SeatedSortingOrder { get; private set; }

        Item item;
        SpriteRenderer view;
        double age;
        int startFrame = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { missing = false; SeatedWorldScale = 0; }

        public static bool Ready => Loaded(revealFrames, RevealFrameCount) && Loaded(idleFrames, IdleFrameCount) && Loaded(boostFrames, BoostFrameCount);
        static bool Loaded(Sprite[] frames, int count) => frames != null && frames.Length == count && frames[0] && frames[count - 1];
        public static Sprite RevealFrame(int index) => Ready ? revealFrames[Mathf.Clamp(index, 0, RevealFrameCount - 1)] : null;
        public static Sprite IdleFrame(int index) => Ready ? idleFrames[Mathf.Clamp(index, 0, IdleFrameCount - 1)] : null;
        public static Sprite BoostFrame(int index) => Ready ? boostFrames[Mathf.Clamp(index, 0, BoostFrameCount - 1)] : null;

        /// <summary>Loads the three sequences once. Without them the ball keeps its static artwork.</summary>
        public static bool Prewarm()
        {
            if (Ready) return true;
            if (missing) return false;
            revealFrames = Load("Reveal/ColorBall_Reveal_", RevealFrameCount);
            idleFrames = Load("Idle/ColorBall_Idle_", IdleFrameCount);
            boostFrames = Load("Boosting/ColorBall_Boosting_", BoostFrameCount);
            if (Ready) return true;
            missing = true;
            Debug.LogWarning("Color Ball frame sequences are missing under Resources/" + ResourceRoot + "; the board ball keeps its static artwork.");
            return false;
        }

        static Sprite[] Load(string prefix, int count)
        {
            var frames = new Sprite[count];
            for (int i = 0; i < count; i++)
            {
                // The approved Idle is intentionally static: share one texture for all 120 frames.
                if (prefix.StartsWith("Idle/") && i > 0) { frames[i] = frames[0]; continue; }
                var source = Resources.Load<Sprite>(ResourceRoot + prefix + (i + 1).ToString("000"));
                if (!source) return null;
                frames[i] = source;
            }
            return frames;
        }

        /// <summary>Starts the Reveal for a Color Ball created by a match. False when the frames are unavailable.</summary>
        public static bool PlayReveal(Item item)
        {
            var animator = Attach(item);
            if (!animator) return false;
            animator.Begin(Phase.Reveal);
            return true;
        }

        /// <summary>
        /// Adds the animator to a board Color Ball. A ball that appears without a reveal (a pre-level
        /// boost, for example) joins the Idle loop at frame 001 in the same frame, so its static art never shows.
        /// </summary>
        public static ColorBallFrameAnimator Attach(Item item)
        {
            if (!Application.isPlaying || !item || !item.sprRenderer || item.destroying || item.currentType != ItemsTypes.BOMB || !RoyalEffectsProfile.Get || !Prewarm()) return null;
            var animator = item.GetComponent<ColorBallFrameAnimator>();
            if (!animator) animator = item.gameObject.AddComponent<ColorBallFrameAnimator>();
            if (animator.Current == Phase.Stopped) animator.Begin(Phase.Idle);
            return animator;
        }

        void Awake()
        {
            item = GetComponent<Item>();
            if (!item || !item.sprRenderer) { enabled = false; return; }
            var source = item.sprRenderer;
            var child = new GameObject("Color Ball frames");
            child.layer = source.gameObject.layer;
            child.transform.SetParent(source.transform, false);
            child.transform.localPosition = Vector3.zero;
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
            if (!item || !item.sprRenderer || item.destroying || item.currentType != ItemsTypes.BOMB)
            {
                // Activation hides the frames at once; the boosting view owns the visuals from here.
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
                // Idle frame 001 repeats Reveal frame 060, so the loop continues on the same clock.
                Current = Phase.Idle;
                age -= RevealDuration;
            }
            if (Current == Phase.Idle && age >= IdleDuration) age %= IdleDuration;
            bool revealing = Current == Phase.Reveal;
            // Both sequences are authored at 60 fps, so the Reveal advances on the same clock as the Idle.
            Frame = revealing
                ? Mathf.Min(RevealFrameCount - 1, (int)(age * SourceFps + 1e-6))
                : (int)(age * SourceFps + 1e-6) % IdleFrameCount;
            // All growth/shrink is authored in the approved frames. Never scale it a second time.
            Zoom = 1f;
            var source = item.sprRenderer;
            view.sprite = revealing ? revealFrames[Frame] : idleFrames[Frame];
            view.color = source.color;
            view.sortingLayerID = source.sortingLayerID;
            view.sortingOrder = revealing ? 40 : source.sortingOrder + 1;
            // The ball must never be cut by a board mask while it is larger than its cell.
            view.maskInteraction = SpriteMaskInteraction.None;
            view.enabled = true;
            source.enabled = false;
            // The frames are pivoted on the sphere centre, so scaling keeps the ball on its cell centre
            // and the seated pose is reached exactly at Zoom 1.
            var pose = view.transform;
            pose.position = item.transform.position;
            pose.rotation = Quaternion.identity;
            float cell = Cell;
            float scale = cell / ContentScale;
            var parentScale = pose.parent.lossyScale;
            pose.localScale = new Vector3(scale / Mathf.Max(.001f, Mathf.Abs(parentScale.x)), scale / Mathf.Max(.001f, Mathf.Abs(parentScale.y)), 1f);
            // Recorded during the Reveal too, so a ball tapped before it seats still hands the
            // activation view the size it would have had on the cell.
            SeatedWorldScale = scale;
            SeatedSortingLayer = source.sortingLayerID;
            SeatedSortingOrder = source.sortingOrder;
        }

        public static float Cell => LevelManager.THIS && LevelManager.THIS.GameField
            ? Mathf.Min(LevelManager.THIS.squareWidth * Mathf.Abs(LevelManager.THIS.GameField.lossyScale.x), LevelManager.THIS.squareHeight * Mathf.Abs(LevelManager.THIS.GameField.lossyScale.y)) : 1.2f;

        public static void HideForActivation(Item item)
        {
            if (item) item.GetComponent<ColorBallFrameAnimator>()?.Stop(false);
        }

        void Stop(bool showStaticArt)
        {
            if (Current == Phase.Stopped && !(view && view.enabled)) return;
            Current = Phase.Stopped;
            Frame = -1;
            age = 0;
            Zoom = 1f;
            // A pooled piece must not inherit the previous reveal's size.
            if (view)
            {
                view.enabled = false;
                view.sprite = null;
                view.transform.localScale = Vector3.one / ContentScale;
                view.transform.localPosition = Vector3.zero;
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
