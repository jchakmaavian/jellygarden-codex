// The board's recoil after a rocket has finished crossing it.
//
// The last of the five layers the browser prototype settled: SHAKE, LAUNCH,
// TRAIL, DESTRUCTION, FINISH. The first four all happen to the pieces; this one
// happens to the BOARD, and that is what makes a blast feel like it landed
// rather than like it played.
//
// It is one gesture: the whole grid is punched a few percent larger at the
// moment the head lands, then settles back over 430ms on a damped spring. No
// particles, no textures, no pool — a single transform written once a frame.
//
// WHY GameField AND NOT THE CAMERA
//
// The camera is not free. StoryPixelSnap.LateUpdate writes
// camera.transform.position every frame, nudging it by the sub-pixel residual
// needed to keep cell edges on whole pixels. A punch on the camera would be
// measured by the snap as misalignment on the very next frame and corrected
// away — and worse, the snap's own header records what happens when that
// feedback loop goes wrong: the board walks off the left of the screen for as
// long as the level is up.
//
// LevelManager.GameField has no such owner. It is written twice, both times
// while a level is being laid out (LevelManager.cs:566 and :989), and never
// again while the board is live. Nothing writes its localScale at all. So it
// can be scaled freely and put back.
//
// WHAT PUNCHES AND WHAT DOES NOT
//
// Square.CreateItem parents each gem to `transform.parent` — the square's
// parent, which is GameField — so gems are siblings of squares rather than
// children of them, and both are children of GameField. Blockers and outlines
// parent to their square. So the entire playable board scales as one piece.
//
// What does NOT move with it: the kit's own stripesEffect (instantiated
// unparented at LevelManager.cs:2276), and the Story VFX pools, which live on
// their own DontDestroyOnLoad holders outside the scene graph. That is
// deliberate rather than overlooked. Smoke and motes hanging still in world
// space while the board recoils underneath them is what reads as the BOARD
// being hit; dragging the smoke along with it would read as the camera being
// bumped, which is a different and much cheaper-looking effect.
//
// WHY A SPRING AND NOT A CURVE
//
// A punch that eases back monotonically reads as a zoom. What reads as impact
// is overshoot — the board springs past its resting size, comes back under it,
// and settles. That needs a second-order response, which is four lines of
// maths and no keyframes.

using UnityEngine;

namespace JellyGarden.Scripts.Story
{
    public static class StoryFinishVfx
    {
        /// <summary>Off unless a launch asks for it. Flipped by <c>-storyfinish</c>, by the <c>StoryVfx.Finish</c> pref, or on by
        /// default in a development build. See StoryVfxFlags.</summary>
        public static bool Enabled
        {
            get
            {
                // Cached, because every clear reads this and the answer
                // cannot change inside one run.
                if (_enabled.HasValue) return _enabled.Value;
                _enabled = StoryVfxFlags.On("-storyfinish", "StoryVfx.Finish");
                return _enabled.Value;
            }
            set { _enabled = value; }
        }
        private static bool? _enabled;

        /// <summary>
        /// How long the board takes to settle, in seconds.
        ///
        /// 430ms, read off the browser prototype's phase timeline — the Finish
        /// bar starts when the head lands and runs to the end of the blast. It
        /// is the longest of the five layers because it is the only one that
        /// has to come to REST: the others can be cut off by their own fade,
        /// but a board still visibly moving when the player takes their next
        /// turn is a board that feels unstable.
        /// </summary>
        public const float Settle = 0.430f;

        /// <summary>
        /// How far the board is punched, as a fraction of its own size.
        ///
        /// Deliberately tiny. The grid fills nearly the whole width of a
        /// 393pt phone screen, so at 2.2% a nine-wide board's outer edge moves
        /// about four points — enough to register as a thump, small enough
        /// that no cell appears to leave its column. Anything past about 4%
        /// and the board reads as bouncing rather than as being struck.
        /// </summary>
        private const float Punch = 0.022f;

        // A spring stiff enough to cross back through its rest size twice
        // inside Settle, damped enough that the third crossing is invisible.
        // These two are a pair: raising Stiffness without raising Damping
        // leaves the board ringing past the end of the window.
        private const float Stiffness = 190f;
        private const float Damping = 17f;

        private static Driver _driver;

        /// <summary>
        /// Punch the board.
        ///
        /// Called when a rocket's head reaches the end of its run. Safe to call
        /// again while a punch is still settling — a second rocket re-energises
        /// the spring rather than restarting it, so a rocket-plus-rocket combo
        /// hits harder instead of visibly cutting the first punch short.
        /// </summary>
        public static void Punchit(float strength = 1f)
        {
            if (!Enabled || !Application.isPlaying) return;

            LevelManager manager = LevelManager.THIS;
            if (manager == null || manager.GameField == null) return;

            EnsureRig();
            if (_driver == null) return;

            _driver.Hit(manager.GameField, Punch * Mathf.Max(0f, strength));
        }

        /// <summary>
        /// Punch the board <paramref name="seconds"/> from now.
        ///
        /// A rocket clears its line on one frame but its head keeps travelling
        /// for a third of a second afterwards, and the recoil belongs to the
        /// moment the head LANDS, not the moment the kit finished clearing. So
        /// the call site fires this the instant the rocket goes off and names
        /// the delay, rather than something having to watch the head.
        ///
        /// The wait is unscaled and holds no reference to any board object, so
        /// a level unloaded mid-flight simply finds no GameField and does
        /// nothing when it lands.
        /// </summary>
        public static void PunchAfter(float seconds, float strength = 1f)
        {
            if (!Enabled || !Application.isPlaying) return;
            if (seconds <= 0f) { Punchit(strength); return; }

            EnsureRig();
            if (_driver == null) return;

            _driver.Schedule(Time.unscaledTime + seconds, strength);
        }

        private static void EnsureRig()
        {
            if (_driver != null) return;

            var holder = new GameObject("~StoryFinishVfx");
            holder.hideFlags = HideFlags.HideAndDontSave;
            StoryObjects.Persist(holder);
            _driver = holder.AddComponent<Driver>();
        }

        /// <summary>
        /// Drives the spring and owns the board's scale while a punch is live.
        /// </summary>
        private sealed class Driver : MonoBehaviour
        {
            private Transform _field;
            private float _offset;      // current displacement from rest
            private float _velocity;
            private float _quietFor;

            // Punches waiting for their head to land. A list rather than one
            // pending time: a rocket-plus-rocket combo fires two sweeps of
            // different lengths in the same frame, and the shorter one must not
            // overwrite the longer one's landing.
            private readonly global::System.Collections.Generic.List<Pending> _pending =
                new global::System.Collections.Generic.List<Pending>(4);

            private struct Pending
            {
                public float At;
                public float Strength;
            }

            internal void Schedule(float at, float strength)
            {
                _pending.Add(new Pending { At = at, Strength = strength });
            }

            // The scale the board had before the first punch touched it. Taken
            // once and restored exactly, so a punch interrupted by a level load
            // cannot leave the grid permanently a fraction too large.
            private Vector3 _rest = Vector3.one;
            private bool _restTaken;

            internal void Hit(Transform field, float amount)
            {
                if (field == null) return;

                // A new board is a new transform: re-take the rest scale rather
                // than carrying the previous level's.
                if (_field != field)
                {
                    Release();
                    _field = field;
                    _rest = field.localScale;
                    _restTaken = true;
                }

                // Velocity, not position. Setting the offset directly would
                // make a second punch during the first one snap the board to a
                // size it was not at; adding energy lets them compose.
                _velocity += amount * Stiffness * 0.5f;
                _quietFor = 0f;
            }

            private void LateUpdate()
            {
                // Land anything whose head has arrived. Done BEFORE the early
                // return below, because until the first punch lands there is no
                // field to spring and the driver is otherwise idle.
                if (_pending.Count > 0)
                {
                    float now = Time.unscaledTime;
                    for (int i = _pending.Count - 1; i >= 0; i--)
                    {
                        if (now < _pending[i].At) continue;
                        float strength = _pending[i].Strength;
                        _pending.RemoveAt(i);
                        Punchit(strength);
                    }
                }

                if (_field == null || !_restTaken) return;

                float dt = Time.unscaledDeltaTime;
                if (dt <= 0f) return;
                // A long frame — a level load, a stall — would make an explicit
                // integrator diverge and blow the board up to nothing.
                if (dt > 0.05f) dt = 0.05f;

                // Damped spring, integrated semi-implicitly: acceleration is
                // applied to velocity first and the NEW velocity moves the
                // offset. The explicit order is what rings forever at this
                // stiffness.
                float acceleration = -Stiffness * _offset - Damping * _velocity;
                _velocity += acceleration * dt;
                _offset += _velocity * dt;

                _field.localScale = _rest * (1f + _offset);

                // Put it back exactly once it has stopped, rather than leaving
                // the board at rest-times-0.9998 forever.
                if (Mathf.Abs(_offset) < 0.0002f && Mathf.Abs(_velocity) < 0.002f)
                {
                    _quietFor += dt;
                    if (_quietFor > 0.05f) Release();
                }
                else
                {
                    _quietFor = 0f;
                }
            }

            private void Release()
            {
                if (_field != null && _restTaken) _field.localScale = _rest;
                _offset = 0f;
                _velocity = 0f;
                _quietFor = 0f;
                _restTaken = false;
                _field = null;
            }

            private void OnDisable()
            {
                Release();
            }
        }
    }
}
