using System.Collections.Generic;
using UnityEngine;

namespace JellyGarden.Scripts.Juice
{
    /// <summary>
    /// Draws ColorBall-Boosting-v1 while an activated Color Ball marks its targets. It is a detached
    /// renderer because the Item is consumed the moment the power fires, so nothing on the board is
    /// left to carry the animation. The sequence is authored as intro 001-018, a seamless 019-078 loop
    /// held while gameplay converts targets, and a 079-108 finish entered at the loop seam.
    /// Frame 108 is fully transparent.
    /// </summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(1000)]
    public sealed class ColorBallBoostView : MonoBehaviour
    {
        public enum Phase { Idle, Intro, Loop, Finish }

        public const int IntroLast = 18, LoopFirst = 19, LoopLast = 78, FinishFirst = 79, FinishLast = 108;
        public const float SourceFps = ColorBallFrameAnimator.SourceFps;
        public const float IntroDuration = IntroLast / SourceFps;
        public const float LoopDuration = (LoopLast - LoopFirst + 1) / SourceFps;
        public const float FinishDuration = (FinishLast - FinishFirst + 1) / SourceFps;
        /// <summary>The activation grows to this many times the seated board size and stays there.</summary>
        public const float BoostZoom = 3f;

        static readonly List<ColorBallBoostView> pool = new List<ColorBallBoostView>();

        /// <summary>1.0 -> 1.5 -> 2.2 -> 3.0 across the intro, smooth and flat at both ends.</summary>
        static readonly AnimationCurve growth = BuildGrowth();

        static AnimationCurve BuildGrowth()
        {
            var curve = new AnimationCurve(
                new Keyframe(0f, 1f), new Keyframe(.4f, 1.5f), new Keyframe(.72f, 2.2f), new Keyframe(1f, BoostZoom));
            for (int i = 0; i < curve.length; i++) curve.SmoothTangents(i, 0);
            for (int i = 0; i < curve.length; i += curve.length - 1)
            {
                var key = curve[i];
                key.inTangent = 0;
                key.outTangent = 0;
                curve.MoveKey(i, key);
            }
            return curve;
        }

        /// <summary>Size multiplier over the intro, where <paramref name="t"/> is 0 to 1.</summary>
        public static float GrowthAt(float t) => growth.Evaluate(Mathf.Clamp01(t));

        public Phase Current { get; private set; }
        /// <summary>Zero-based index into the Boosting sequence, or -1 while unused.</summary>
        public int Frame { get; private set; } = -1;
        public float Zoom { get; private set; } = 1f;
        public SpriteRenderer View => view;
        public static int Active { get { int n = 0; foreach (var v in pool) if (v && v.Current != Phase.Idle) n++; return n; } }
        public static int PoolSize => pool.Count;
        public static void ClearAll() { foreach (var v in pool) if (v) v.Stop(); }

        SpriteRenderer view;
        double age, finishAt = -1;
        float seatedScale = 1f;
        bool holding;
        float exitRate=1f, chargeClock;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { pool.Clear(); }

        /// <summary>
        /// Starts the activation animation on the cell the ball fired from. Returns null when the
        /// sequences are unavailable, in which case the existing effects play on their own.
        /// </summary>
        public static ColorBallBoostView Begin(Vector3 position)
        {
            if (!Application.isPlaying || !RoyalEffectsProfile.Get || !ColorBallFrameAnimator.Prewarm()) return null;
            ColorBallBoostView view = null;
            foreach (var candidate in pool) if (candidate && candidate.Current == Phase.Idle) { view = candidate; break; }
            if (!view)
            {
                var host = new GameObject("Color Ball boosting");
                view = host.AddComponent<ColorBallBoostView>();
                pool.Add(view);
            }
            view.Play(position);
            return view;
        }

        /// <summary>Lets the held loop run out into the 079-108 finish. Safe to call more than once.</summary>
        public void Finish()
        {
            if (!holding) return;
            holding = false;
            // Frame 079 continues at the charged loop seam. Finish at that seam, never hard-cut.
            double cycles = global::System.Math.Max(0, age - IntroDuration) / LoopDuration;
            finishAt = IntroDuration + global::System.Math.Ceiling(cycles) * LoopDuration;
            // Spin into the same authored seam promptly, without a dead extra loop or a frame cut.
            exitRate=Mathf.Max(1f,(float)(finishAt-age)/.16f);
        }

        void Awake()
        {
            view = gameObject.GetComponent<SpriteRenderer>();
            if (!view) view = gameObject.AddComponent<SpriteRenderer>();
            view.enabled = false;
        }

        void Play(Vector3 position)
        {
            // A reused view must not inherit the previous activation's size, frame or phase.
            age = 0;
            finishAt = -1;
            holding = true;
            exitRate=1;chargeClock=0;
            Current = Phase.Intro;
            Frame = 0;
            Zoom = 1f;
            seatedScale = ColorBallFrameAnimator.Cell / ColorBallFrameAnimator.ContentScale;
            var board = LevelManager.THIS;
            if (board && board.GameField) gameObject.layer = board.GameField.gameObject.layer;
            transform.position = position + Vector3.back * .25f;
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one * seatedScale;
            view.sprite = ColorBallFrameAnimator.BoostFrame(0);
            view.color = Color.white;
            view.sortingLayerID = ColorBallFrameAnimator.SeatedSortingLayer;
            view.sortingOrder = 40;
            // Never let a board mask cut the enlarged activation.
            view.maskInteraction = SpriteMaskInteraction.None;
            view.enabled = true;
        }

        void LateUpdate()
        {
            if (Current == Phase.Idle) return;
            var level = LevelManager.THIS;
            if (!(level && level.gameStatus == GameState.Pause))
            {
                double step=Time.deltaTime;
                if(finishAt>=0&&age<finishAt)
                {
                    double until=(finishAt-age)/exitRate;
                    age+=step<=until?step*exitRate:(finishAt-age)+(step-until);
                }
                else age+=step;
                chargeClock+=Time.deltaTime;
                if(chargeClock>=.045f && (finishAt<0 || age<finishAt+.16f))
                {
                    chargeClock%=.045f;
                    ColorBallTargetVfx.Charge(transform.position,Mathf.Clamp01((float)(age/IntroDuration)),finishAt>=0);
                }
            }

            int frame;
            if (age < IntroDuration)
            {
                Current = Phase.Intro;
                frame = Mathf.Min(IntroLast - 1, (int)(age * SourceFps + 1e-6));
                Zoom = GrowthAt((float)(age / IntroDuration));
            }
            else
            {
                Zoom = BoostZoom;
                // The finish is only entered once the intro has played; it begins at the approved charged-loop seam.
                if (finishAt >= 0 && age >= finishAt)
                {
                    Current = Phase.Finish;
                    double into = age - finishAt;
                    if (into >= FinishDuration) { Stop(); return; }
                    frame = FinishFirst - 1 + Mathf.Min(FinishLast - FinishFirst, (int)(into * SourceFps + 1e-6));
                }
                else
                {
                    Current = Phase.Loop;
                    double into = age - IntroDuration;
                    int span = LoopLast - LoopFirst + 1;
                    frame = LoopFirst - 1 + (int)(into * SourceFps + 1e-6) % span;
                }
            }
            Frame = frame;
            view.sprite = ColorBallFrameAnimator.BoostFrame(frame);
            // The approved 512px frames contain the entire 1x -> 3x pose change.
            transform.localScale = Vector3.one * seatedScale;
        }

        /// <summary>Hides the view and returns it to the pool, leaving nothing on the board.</summary>
        public void Stop()
        {
            Current = Phase.Idle;
            Frame = -1;
            Zoom = 1f;
            age = 0;
            finishAt = -1;
            holding = false;
            exitRate=1;chargeClock=0;
            if (view)
            {
                view.enabled = false;
                view.sprite = null;
            }
            transform.localScale = Vector3.one;
        }

        void OnDisable() { Stop(); }
    }
}
