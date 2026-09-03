// The animated character avatar: a head-and-shoulders portrait that breathes.
//
// The HUD used to draw the whole standing figure of King Leo shrunk into a
// hundred-pixel frame, where a face two pixels wide reads as a brown smudge.
// Royal Match draws a head-and-shoulders medallion instead, and it moves.
//
// Two ways of getting there, and the caller does not have to care which it got:
//
//   Frames — Resources/Story/Art/avatar_{name}_00, _01, … are played in order
//   on a loop. This is the real animation: the king breathes, blinks and turns
//   his head a few degrees.
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
        /// Frames a second. The clip these are cut from is a slow idle — a
        /// breath, a blink, a small turn — so it reads correctly well under
        /// cinema rates, and every frame is a texture in memory.
        /// </summary>
        public const float DefaultFps = 12f;

        private Image _image;
        private Sprite[] _frames;
        private float _fps = DefaultFps;
        private float _startedAt;
        private int _shown = -1;

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
                // A generated frame is already the head-and-shoulders crop, so
                // it simply fills the window.
                StoryUi.Stretch(image.rectTransform);

                if (Animate)
                {
                    var player = image.gameObject.AddComponent<StoryAvatar>();
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

        /// <summary>Starts the loop. Safe to call again to re-point it.</summary>
        public void Play(Image image, Sprite[] frames, float fps)
        {
            _image = image;
            _frames = frames;
            _fps = fps <= 0f ? DefaultFps : fps;
            _startedAt = StoryClock.Now;
            _shown = -1;
            Tick();
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

            int index = Mathf.FloorToInt(elapsed * _fps) % _frames.Length;
            Show(index);
        }

        private void Show(int index)
        {
            if (index == _shown) return;
            _shown = index;
            _image.sprite = _frames[index];
        }
    }
}
