// The anticipation a cleared gem gets before it is allowed to die.
//
// The kit clears a gem the instant the match resolves: PlayDestroyAnimation
// fires and the `destroy` clip shrinks the gem's Sprite child from 1 to 0 over
// 250ms. There is no wind-up at all, and that is the whole of what makes the
// kit's clear read as a gem being switched off rather than as a gem popping.
//
// This adds the wind-up, and nothing else. Three stages, 143ms in total, all of
// them on the gem's own transform before the kit's clip is ever triggered:
//
//   SQUASH  60ms   the gem flattens to 90% — the tell, the frame that says
//                  something is about to happen to this piece
//   SNAP    50ms   it stretches hard the other way, to 120%
//   FLASH   33ms   it pulls thin and tall (0.80 x 1.24) for two frames at 60fps
//
// Then the kit's own destroy runs, unchanged and on time.
//
// WHY THE ROOT TRANSFORM AND NOT THE SPRITE CHILD
//
// `destroy.anim` animates m_LocalScale on the child path `Sprite`, not on the
// gem root. So the root is free: the pre-roll scales the root, the kit's clip
// scales the child, and the two compose rather than fight. Writing the child
// instead would have the Animator overwrite every value on the next evaluation.
//
// WHY IT MULTIPLIES xScale/yScale RATHER THAN WRITING localScale
//
// This is the part that cannot be guessed from reading the pre-roll alone.
//
// Item.AnimIdleStart is an infinite coroutine that writes
//
//     transform.localScale = new Vector3(xScale, yScale, 1);
//
// every WaitForFixedUpdate, forever, as the gem's idle breathing. DestroyItem
// tries to stop it with
//
//     StopCoroutine(AnimIdleStart());
//
// and that call stops NOTHING. Unity's StopCoroutine(IEnumerator) matches the
// running coroutine by reference, and this passes a brand-new enumerator from a
// fresh call to the method — a different object than the one StartCoroutine was
// given. Item has no OnDisable and no StopAllCoroutines, so the idle loop is
// still running while the gem is being destroyed, and it is still assigning
// localScale from its own two fields every 20ms.
//
// Any pre-roll that wrote transform.localScale directly would therefore be
// erased on the very next fixed update — a squash that flickers for one frame
// and vanishes, which reads as a rendering glitch rather than as anticipation.
// So this drives the idle loop's OWN fields through Item.PopScale, and the idle
// loop then applies them for us. The breathing and the squash multiply together
// instead of one clobbering the other.
//
// Nothing here edits the kit's timing contract. DestroyCor is synchronous by
// contract downstream of square.DestroyBlock() — see StoryRocketSweep's header
// for what happens when that is broken — and this yields only BEFORE the clip
// is triggered, well above that line. The cascade, the refill and the win check
// are reached exactly as they were, 143ms later.

using System.Collections;
using UnityEngine;

namespace JellyGarden.Scripts.Story
{
    public static class StoryPopVfx
    {
        /// <summary>
        /// Off until a launch asks for it, so the board behaves exactly as the
        /// kit does by default. Flipped by <c>-storypop</c>, by the <c>StoryVfx.Pop</c> pref,
        /// or on by default in a development build. See StoryVfxFlags.
        /// </summary>
        public static bool Enabled
        {
            get
            {
                // Cached, because every clear reads this and the answer
                // cannot change inside one run.
                if (_enabled.HasValue) return _enabled.Value;
                _enabled = StoryVfxFlags.On("-storypop", "StoryVfx.Pop");
                return _enabled.Value;
            }
            set { _enabled = value; }
        }
        private static bool? _enabled;

        // The three stages, in seconds, totalling the 90ms the browser
        // prototype's Anticipation slider was left at.
        //
        // The cut from the original 143ms is NOT proportional, and that is the
        // whole point of writing them out separately. Scaling all three by
        // 90/143 would put the flash at 21ms, which is one frame at 60fps —
        // and a one-frame stage does not read as a beat, it reads as a dropped
        // frame. So the flash keeps its two-frame floor at 33ms and the whole
        // of the reduction is taken out of the squash and the snap, which are
        // ramps rather than beats and survive being shortened.
        private const float SquashTime = 0.032f;
        private const float SnapTime = 0.025f;
        private const float FlashTime = 0.033f;

        // Where each stage lands. The gem is widest at the end of SQUASH and
        // tallest at the end of FLASH, and the sign change between them is what
        // carries the snap — a stretch that only ever grows reads as a swell.
        private const float SquashX = 1.10f, SquashY = 0.90f;
        private const float SnapX = 0.80f, SnapY = 1.24f;
        private const float FlashX = 0.98f, FlashY = 1.34f;

        /// <summary>
        /// How long the whole pre-roll takes. Anything that needs to reason
        /// about when the kit's own clear starts should read this rather than
        /// adding the three constants up itself.
        /// </summary>
        public static float Duration
        {
            get { return SquashTime + SnapTime + FlashTime; }
        }

        /// <summary>
        /// Play the wind-up on one gem about to be cleared, then return so the
        /// kit's own destroy can run.
        ///
        /// Yields for <see cref="Duration"/> when enabled and a no-op otherwise
        /// — a disabled pre-roll costs the board not one frame, which is what
        /// keeps the kit's behaviour exactly reachable.
        /// </summary>
        public static IEnumerator PreRoll(Item item)
        {
            if (!Enabled) yield break;
            if (!Application.isPlaying) yield break;
            if (item == null) yield break;

            // Kit means untouched: no wind-up at all, so the lab's first button
            // is a true reference to compare the other five against.
            if (StoryVfxVariant.Current == VfxVariant.Kit) yield break;

            // Scale is multiplicative on top of whatever the idle loop is
            // currently breathing at, so the pre-roll reads the same on a gem
            // caught mid-breath as on one at rest.
            _frames = 0;
            float startedAt = Time.realtimeSinceStartup;

            yield return Stage(item, 1f, 1f, SquashX, SquashY, SquashTime, Ease.Out);
            yield return Stage(item, SquashX, SquashY, SnapX, SnapY, SnapTime, Ease.Out);
            yield return Stage(item, SnapX, SnapY, FlashX, FlashY, FlashTime, Ease.Linear);

            // A few lines, not one per clear. Reports what the effect ACTUALLY
            // got rather than what it asked for: a pre-roll that ran for one
            // frame is invisible however correct its arithmetic is, and that is
            // indistinguishable from "no effect" on screen.
            if (_said < 3)
            {
                _said++;
                Debug.Log(string.Format(
                    "[Pop] #{0} budget {1:F0}ms actual {2:F0}ms over {3} frame(s) | scale {4}",
                    _said, Duration * 1000f,
                    (Time.realtimeSinceStartup - startedAt) * 1000f, _frames,
                    item == null ? "(gem gone)" : item.transform.localScale.ToString("F3")));
            }
        }

        private static int _said;
        private static int _frames;

        private enum Ease { Linear, Out }

        /// <summary>
        /// Drive the gem from one scale pair to another over `seconds`.
        ///
        /// Unscaled time is deliberate: a pause mid-cascade should freeze the
        /// board, and Time.timeScale is what the kit pauses with — but a gem
        /// halfway through its wind-up when the game is paused would otherwise
        /// sit visibly squashed until play resumed. The stage is 60ms; letting
        /// it finish through a pause is the lesser of the two artefacts.
        /// </summary>
        private static IEnumerator Stage(
            Item item, float fromX, float fromY, float toX, float toY, float seconds, Ease ease)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                // The gem can be destroyed out from under the pre-roll by a
                // rocket or a colour bomb clearing the same cell on the next
                // frame. Bail rather than touching a dead transform.
                if (item == null) yield break;

                float t = Mathf.Clamp01(elapsed / seconds);
                if (ease == Ease.Out) t = 1f - (1f - t) * (1f - t) * (1f - t);

                item.PopScale(Mathf.Lerp(fromX, toX, t), Mathf.Lerp(fromY, toY, t));

                _frames++;
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (item != null) item.PopScale(toX, toY);
        }
    }
}
