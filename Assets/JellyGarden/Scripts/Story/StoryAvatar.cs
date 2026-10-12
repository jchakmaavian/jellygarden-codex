// The animated character avatar: a head-and-shoulders portrait that breathes.
//
// The HUD used to draw the whole standing figure of King Leo shrunk into a
// hundred-pixel frame, where a face two pixels wide reads as a brown smudge.
// Royal Match draws a head-and-shoulders medallion instead, and it moves.
//
// Two ways of getting there, and the caller does not have to care which it got:
//
//   Frames — Resources/Story/Art/avatar_{name}_00, _01, … are played there and
//   back on a loop. This is the real animation: the king breathes, blinks and
//   turns his head a few degrees.
//
//   The frames run FORWARD THEN BACK rather than wrapping, because a generated
//   clip does not end where it began and a wrap is a visible cut. Running back
//   down them costs nothing — the same sprites, indexed in reverse — and makes
//   any clip loop, whatever it was cut from.
//
//   Reactions — avatar_{name}_{clip}_00, _01, … is a named clip: cheer, pop,
//   worry, slump. StoryAvatar.React plays one ONCE and hands back to the idle.
//   Because a clip ping-pongs, it ends on the pose the idle starts from, so the
//   hand-back is not a cut either. In a level the Royal HUD
//   (RoyalGameplayPresentation) plays them from AvatarEvents; StoryLevelHud
//   fires React off its own numbers only while the Royal layer is off (no
//   RoyalEffectsProfile), and StoryLosePopup fires the slump when a level is lost.
//
//   Holds — StoryAvatar.Hold loops a clip in the idle's place (the worry while
//   few moves are left) until Release, which lets the current cycle finish on
//   the idle's opening pose. A reaction plays over a hold and hands back to it.
//   The in-level HUD (RoyalGameplayPresentation) drives these from AvatarEvents.
//
//   A crop — with no frames authored, the full-body portrait is drawn scaled up
//   and offset so that StoryArt.AvatarCrop fills the frame. Still, but framed
//   the same way, so a character who has no animation yet still reads as a face
//   rather than as a figure.
//
// The frames are held on the Image and swapped; nothing is instantiated per
// frame and nothing allocates once it is built. The clock is StoryClock, so an
// editor test can step the animation rather than sleep through it.

using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    [DisallowMultipleComponent]
    public sealed class StoryAvatar : MonoBehaviour
    {
        /// <summary>The whole feature, in one switch: false draws the still crop.</summary>
        public static bool Animate = true;

        /// <summary>
        /// Frames a second for the IDLE loop. The clip these are cut from is a
        /// slow idle — a breath, a blink, a small turn — so it reads correctly
        /// well under cinema rates, and every frame is a texture in memory.
        ///
        /// 6, halved from 12. At 12 the 25 frames ping-pong in 48 steps, four
        /// seconds a cycle, and a breath and a blink that quick is a fidget:
        /// the face never settles and it pulls the eye off the board. At 6 the
        /// same cycle takes eight seconds, which is a breath.
        /// </summary>
        public const float DefaultFps = 6f;

        /// <summary>
        /// Frames a second for a REACTION — the cheer, the pop, the worry.
        /// Kept at the old 12: a reaction answers something that just happened
        /// on the board and has to land while the player is still looking at
        /// what caused it. Only the idle wanted slowing.
        /// </summary>
        public const float ClipFps = 12f;

        /// <summary>
        /// Frames a second for a HELD clip. Between the two: a worry looped at
        /// the reaction rate is the fidget the idle was slowed down to avoid.
        /// </summary>
        public const float HoldFps = 8f;

        private Image _image;
        private Sprite[] _frames;
        private Sprite[] _idle;
        private Sprite[] _held;
        private string _heldClip;
        private string _clip;
        private bool _once;
        private float _fps = DefaultFps;

        /// <summary>The rate the idle runs at, kept so a reaction can hand back to it.</summary>
        private float _idleFps = DefaultFps;
        private float _startedAt;
        private int _shown = -1;

        /// <summary>Whose frames these are, so a reaction can be looked up by name.</summary>
        public string Character;

        /// <summary>
        /// The avatar on screen, or null. One HUD carries one face, and a
        /// reaction is fired by whatever notices the event rather than by
        /// something holding a reference to the portrait.
        /// </summary>
        public static StoryAvatar Active { get; private set; }

        /// <summary>The clip playing now, or null while the idle loops. Read by tests.</summary>
        public string Playing { get { return _once ? _clip : null; } }

        /// <summary>The clip looping in the idle's place, or null.</summary>
        public string Held { get { return _heldClip; } }

        /// <summary>How many frames are playing. Zero means the still crop. Read by tests.</summary>
        public int FrameCount { get { return _frames == null ? 0 : _frames.Length; } }

        /// <summary>The frame currently on screen, or -1 before the first tick.</summary>
        public int ShownFrame { get { return _shown; } }

        /// <summary>
        /// Fills <paramref name="window"/> with the character's avatar and
        /// returns the Image drawing it, animated when frames exist.
        ///
        /// The window is expected to carry a Mask — the crop path deliberately
        /// draws an image several times the size of its frame, and without a
        /// mask the rest of the figure spills across the HUD.
        /// </summary>
        public static Image Build(string character, RectTransform window)
        {
            if (window == null) return null;

            Sprite[] frames = StoryArt.AvatarFrames(character);

            Image image = StoryUi.CreateImage("Face", window,
                frames.Length > 0 ? frames[0] : StoryArt.Portrait(character), Color.white);
            image.preserveAspect = true;

            if (frames.Length > 0)
            {
                // Cover the masked opening at the frame's native aspect ratio.
                // preserveAspect alone fits INSIDE it and leaves empty bands
                // whenever the portrait and opening have different proportions.
                StoryUi.Stretch(image.rectTransform);
                var cover = image.gameObject.AddComponent<AspectRatioFitter>();
                cover.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                cover.aspectRatio = frames[0].rect.width / frames[0].rect.height;

                if (Animate)
                {
                    var player = image.gameObject.AddComponent<StoryAvatar>();
                    player.Character = character;
                    player.Play(image, frames, DefaultFps);
                }
            }
            else
            {
                ApplyCrop(image.rectTransform, StoryArt.AvatarCrop);
            }

            // No art at all is not a reason to draw a hole: the slot stays empty
            // and the frame around it still reads.
            image.enabled = image.sprite != null;
            return image;
        }

        /// <summary>
        /// Places a rect inside its parent so that <paramref name="crop"/> — a
        /// sub-rectangle of the image in 0..1 texture coordinates — is what the
        /// parent shows.
        ///
        /// Done with anchors rather than a scaled sizeDelta, so it holds at any
        /// frame size with no arithmetic to redo when the HUD is re-measured:
        /// the parent's 0..1 range is mapped onto the crop's range, which puts
        /// the rest of the image outside the parent, where the mask cuts it.
        /// </summary>
        public static void ApplyCrop(RectTransform rect, Rect crop)
        {
            if (rect == null) return;
            if (crop.width <= 0f || crop.height <= 0f)
            {
                StoryUi.Stretch(rect);
                return;
            }

            rect.anchorMin = new Vector2(-crop.x / crop.width, -crop.y / crop.height);
            rect.anchorMax = new Vector2((1f - crop.x) / crop.width, (1f - crop.y) / crop.height);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// Starts itself when the scene brings it back.
        ///
        /// The portrait is baked into game.unity, and `Character` is public so it
        /// survives with it — but the frames are a private array that does not
        /// serialise, so on a scene load there is a StoryAvatar sitting on an
        /// Image with nothing to play. It reloads them from the name it kept.
        /// </summary>
        private void Awake()
        {
            if (_frames != null && _frames.Length > 0) return;
            if (string.IsNullOrEmpty(Character)) return;

            var image = GetComponent<Image>();
            if (image == null) return;

            Sprite[] frames = StoryArt.AvatarFrames(Character);
            if (frames.Length == 0) return;

            Play(image, frames, DefaultFps);
        }

        /// <summary>Starts the loop. Safe to call again to re-point it.</summary>
        public void Play(Image image, Sprite[] frames, float fps)
        {
            _image = image;
            _frames = frames;
            _idle = frames;
            _held = null;
            _heldClip = null;
            _clip = null;
            _once = false;
            _fps = fps <= 0f ? DefaultFps : fps;
            _idleFps = _fps;
            _startedAt = StoryClock.Now;
            _shown = -1;
            Active = this;
            Tick();
        }

        /// <summary>
        /// Plays a named reaction on the face that is on screen, if there is one
        /// and if that character has the clip. Silent when either is missing —
        /// a reaction is decoration, and nothing that fires one should have to
        /// check first.
        /// </summary>
        public static void React(string clip)
        {
            if (Active != null) Active.PlayClip(clip);
        }

        /// <summary>
        /// Plays <paramref name="clip"/> once and hands back to the idle loop.
        ///
        /// Every clip is authored as a ping-pong — it runs to its last frame and
        /// back — so the reaction ENDS on the same pose the idle starts from and
        /// the hand-back is not a cut. Re-firing the clip already playing is
        /// ignored rather than restarted, so a condition that stays true (three
        /// moves left, and still three) does not freeze the face on frame zero.
        /// The clip being held is likewise left looping.
        ///
        /// Returns whether the clip is on screen now: false only when the
        /// character has no such clip.
        /// </summary>
        public bool PlayClip(string clip)
        {
            if (string.IsNullOrEmpty(clip)) return false;
            if (_once && _clip == clip) return true;
            if (!_once && _heldClip == clip) return true;

            Sprite[] frames = StoryArt.AvatarFrames(Character, clip);
            if (frames.Length == 0) return false;

            _frames = frames;
            _clip = clip;
            _once = true;
            _fps = ClipFps;
            _startedAt = StoryClock.Now;
            _shown = -1;
            Tick();
            return true;
        }

        /// <summary>
        /// Loops <paramref name="clip"/> in the idle's place until
        /// <see cref="Release"/>. A reaction already playing finishes first and
        /// then hands back to the hold. Returns false when there is no such clip.
        /// </summary>
        public bool Hold(string clip)
        {
            if (string.IsNullOrEmpty(clip)) return false;
            if (_heldClip == clip) return true;

            Sprite[] frames = StoryArt.AvatarFrames(Character, clip);
            if (frames.Length == 0) return false;

            _held = frames;
            _heldClip = clip;
            if (!_once)
            {
                StartLoop();
                Tick();
            }
            return true;
        }

        /// <summary>
        /// Ends the hold. The cycle on screen runs out as a one-shot, so the face
        /// comes back to the idle on the pose the idle starts from, not on a cut.
        /// </summary>
        public void Release()
        {
            if (_held == null) return;

            if (!_once && _frames == _held && _fps > 0f)
            {
                float cycle = Period(_frames) / _fps;
                float into = Mathf.Max(0f, StoryClock.Now - _startedAt) % cycle;
                _startedAt = StoryClock.Now - into;
                _clip = _heldClip;
                _once = true;
            }

            _held = null;
            _heldClip = null;
        }

        /// <summary>Stops a reaction where it is and goes back to the loop (the hold, else the idle).</summary>
        public void EndReaction()
        {
            if (!_once) return;
            StartLoop();
            Tick();
        }

        private void StartLoop()
        {
            _frames = _held ?? _idle;
            _clip = null;
            _once = false;
            _fps = _held != null ? HoldFps : _idleFps;
            _startedAt = StoryClock.Now;
            _shown = -1;
        }

        private void OnDestroy()
        {
            if (Active == this) Active = null;
        }

        private void Update()
        {
            Tick();
        }

        /// <summary>
        /// Advances to whichever frame the clock is on. Driven by elapsed time
        /// rather than by counting Updates, so the loop runs at the same speed
        /// whatever the frame rate — and so a test can jump it forward.
        /// </summary>
        public void Tick()
        {
            if (_image == null || _frames == null || _frames.Length == 0) return;
            if (!Animate)
            {
                Show(0);
                return;
            }

            float elapsed = StoryClock.Now - _startedAt;
            if (elapsed < 0f) elapsed = 0f;

            int step = Mathf.FloorToInt(elapsed * _fps);

            // A one-shot that has run its whole ping-pong is over: it ended on
            // the pose the idle begins from, so handing back is a no-op on
            // screen and the next Tick simply continues the loop (the hold, if
            // one is set).
            if (_once && step >= Period(_frames))
            {
                StartLoop();
                step = 0;
                if (_frames == null || _frames.Length == 0) return;
            }

            Show(PingPong(step, _frames.Length));
        }

        /// <summary>
        /// Frames in one there-and-back cycle: the last frame is not played
        /// twice, so a 25-frame clip is a 48-step loop rather than a 50-step one
        /// that stutters at each end.
        /// </summary>
        private static int Period(Sprite[] frames)
        {
            return frames == null || frames.Length <= 1 ? 1 : frames.Length * 2 - 2;
        }

        /// <summary>Maps a step onto a frame, running forward then back.</summary>
        private static int PingPong(int step, int count)
        {
            if (count <= 1) return 0;
            int period = count * 2 - 2;
            int i = step % period;
            return i < count ? i : period - i;
        }

        private void Show(int index)
        {
            if (index == _shown) return;
            _shown = index;
            _image.sprite = _frames[index];
        }
    }
}
