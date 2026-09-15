// The staged flourish that plays over the board when a level is won.
//
// WHAT THE KIT GIVES US, AND WHY THAT IS THE WHOLE BUDGET
//
// LevelManager.PreWinAnimationsCor shows one object for three seconds and then
// hides it:
//
//     MenuManager.Instance.ShowPreCompleteBanner(true);
//     yield return new WaitForSeconds(3);
//     MenuManager.Instance.ShowPreCompleteBanner(false);
//
// That object is `PreCompleteBanner`, and StoryWinBanner already dresses it
// with the story's own ribbon. This adds the rest of the celebration AROUND
// that ribbon -- banner drops, trumpets, light and confetti -- as children of
// the same object, so the kit's own show/hide is what starts and ends it and
// no kit coroutine is touched.
//
// WHY EVERYTHING IS DRIVEN FROM ONE CLOCK AND NOT FROM COROUTINES
//
// A coroutine per element would need a MonoBehaviour that survives the object
// being switched off mid-flight -- and this object IS switched off, every
// level, by a kit coroutine that knows nothing about us. Unity stops
// coroutines on a disabled object and does not resume them, so a staged
// sequence built that way loses whichever pieces were still animating when the
// player tapped through.
//
// So there are no coroutines. One Update reads `StoryClock.Now - _began` and
// writes every transform from that elapsed time. Switching the object off
// stops the sequence wherever it is; switching it on calls OnEnable, which
// resets `_began`, and the whole thing replays from zero. That is also what
// makes it testable: StoryClock.Override lets a test step the sequence without
// waiting on frames.
//
// THE STAGES, AND WHY THEY OVERLAP
//
//   0.00  the ground dims
//   0.15  two banner drops unroll from the top edge, staggered
//   0.35  the ribbon punches in, overshoots, settles
//   0.55  light and confetti -- StoryWinStarVfx, already written and tuned
//   0.80  four trumpets fan out from behind the ribbon, pennants trailing
//   1.20  everything idles: a slow sway, the glints keep twinkling
//   2.70  the lot lifts away together
//
// Nothing waits for the thing before it to finish. Overlapping is what makes a
// sequence read as one event rather than as a list -- the ribbon is already
// arriving while the drops are still unrolling, and the trumpets leave their
// marks while the confetti is still in the air.

using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    public sealed class StoryWinCelebration : MonoBehaviour
    {
        /// <summary>The whole feature, in one switch. False gives back the plain ribbon.</summary>
        public static bool Enabled = true;

        // ---- the schedule, in seconds from the moment the banner is shown ----

        private const float DimAt = 0.00f, DimFor = 0.30f;
        private const float DropAt = 0.15f, DropFor = 0.52f, DropStagger = 0.08f;
        private const float RibbonAt = 0.35f, RibbonFor = 0.62f;
        private const float SparkleAt = 0.55f;
        private const float HornAt = 0.80f, HornFor = 0.50f, HornStagger = 0.06f;
        private const float IdleAt = 1.20f;
        private const float ExitAt = 2.70f, ExitFor = 0.30f;

        /// <summary>How far the drops hang, and how wide, in the kit canvas's units.</summary>
        private const float DropWidth = 132f, DropHeight = 430f, DropInset = 34f;

        /// <summary>Trumpet size, and how far the fan opens.</summary>
        private const float HornSize = 232f, HornSpread = 250f, HornRise = 40f;

        private RectTransform _ribbon;
        private RectTransform[] _drops = new RectTransform[2];
        private RectTransform[] _horns = new RectTransform[4];
        private Vector2[] _hornRest = new Vector2[4];
        private float[] _hornAngle = new float[4];
        private Image _dim;
        private StoryWinStarVfx _light;
        private float _began;

        /// <summary>
        /// Build the celebration under the kit's banner object.
        ///
        /// Called by StoryWinBanner.Install once the ribbon exists, so the
        /// ribbon can be handed in rather than found -- the banner object is
        /// inactive at install time and Find would skip its children.
        /// </summary>
        public static StoryWinCelebration Install(Transform banner, RectTransform ribbon)
        {
            if (!Enabled || banner == null) return null;
            if (banner.GetComponent<StoryWinCelebration>() != null) return null;

            var celebration = banner.gameObject.AddComponent<StoryWinCelebration>();
            celebration.Build(banner, ribbon);
            return celebration;
        }

        private void Build(Transform banner, RectTransform ribbon)
        {
            _ribbon = ribbon;

            // The dim sits behind everything, including the ribbon, so it must
            // be the first child. It is deliberately NOT full black: the board
            // underneath is the thing the player just beat, and hiding it
            // entirely makes the flourish feel like a scene change.
            _dim = StoryUi.CreateImage("~WinDim", banner, null, new Color(0f, 0f, 0f, 0f));
            _dim.raycastTarget = false;
            RectTransform dim = _dim.rectTransform;
            dim.anchorMin = Vector2.zero;
            dim.anchorMax = Vector2.one;
            dim.offsetMin = new Vector2(-600f, -1400f);
            dim.offsetMax = new Vector2(600f, 1400f);
            dim.SetSiblingIndex(0);

            Sprite dropArt = StoryUi.LoadSprite("win_drop");
            for (int i = 0; i < _drops.Length; i++)
            {
                Image drop = StoryUi.CreateImage("~WinDrop" + i, banner, dropArt, Color.white);
                drop.raycastTarget = false;
                drop.preserveAspect = true;
                RectTransform rect = drop.rectTransform;
                // Anchored to the TOP of the canvas and pivoted at its top
                // edge, so scaling y from 0 unrolls it downward from the rail
                // rather than growing it from its middle.
                rect.anchorMin = rect.anchorMax = new Vector2(i == 0 ? 0f : 1f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.sizeDelta = new Vector2(DropWidth, DropHeight);
                rect.anchoredPosition = new Vector2(
                    i == 0 ? DropInset + DropWidth * 0.5f : -(DropInset + DropWidth * 0.5f), 0f);
                rect.SetSiblingIndex(1);
                _drops[i] = rect;
            }

            Sprite hornArt = StoryUi.LoadSprite("win_horn");
            for (int i = 0; i < _horns.Length; i++)
            {
                Image horn = StoryUi.CreateImage("~WinHorn" + i, banner, hornArt, Color.white);
                horn.raycastTarget = false;
                horn.preserveAspect = true;
                RectTransform rect = horn.rectTransform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(HornSize, HornSize);

                // Two a side, fanned. The art points up-and-right, so the right
                // pair keeps it and the left pair is mirrored by a negative x
                // scale -- cheaper and sharper than a second sprite, and the
                // pennant mirrors with it.
                bool right = i < 2;
                float tier = (i % 2 == 0) ? 0f : 1f;
                _hornRest[i] = new Vector2(
                    (right ? 1f : -1f) * (HornSpread * (0.62f + tier * 0.38f)),
                    -HornRise - tier * 54f);
                _hornAngle[i] = (right ? -1f : 1f) * (12f + tier * 16f);
                rect.localScale = new Vector3(right ? 1f : -1f, 1f, 1f);
                rect.SetSiblingIndex(1);
                _horns[i] = rect;
            }

            // The light, rays, sparkles and confetti are already written and
            // tuned in StoryWinStarVfx -- it takes any RectTransform as its
            // subject, so the ribbon stands in for the star it was built for.
            var effects = StoryUi.CreateRect("~WinLight", banner);
            effects.anchorMin = effects.anchorMax = effects.pivot = new Vector2(0.5f, 0.5f);
            effects.sizeDelta = new Vector2(760f, 760f);
            effects.anchoredPosition = Vector2.zero;
            effects.SetSiblingIndex(1);
            _light = effects.gameObject.AddComponent<StoryWinStarVfx>();
            _light.raycastTarget = false;

            Apply(0f);
        }

        private void OnEnable()
        {
            // The kit switches this object on for its three seconds and off
            // again. Restarting here is what makes the sequence play once per
            // level rather than once per session.
            _began = StoryClock.Now;
            if (_light != null && _ribbon != null)
            {
                _light.SetStar(_ribbon);
                _light.Play(_began);
            }
            Apply(0f);
        }

        private void OnDisable()
        {
            if (_light != null) _light.Settle();
        }

        private void Update()
        {
            Apply(Mathf.Max(0f, StoryClock.Now - _began));
        }

        /// <summary>
        /// Every transform in the sequence, written from one elapsed time.
        ///
        /// Deliberately a pure function of `elapsed`: nothing accumulates, so a
        /// dropped frame cannot desync one element from another and a test can
        /// jump straight to any moment.
        /// </summary>
        private void Apply(float elapsed)
        {
            float exit = ExitFor <= 0f ? 0f : Mathf.Clamp01((elapsed - ExitAt) / ExitFor);
            float leaving = 1f - Ease(exit);
            float idle = Mathf.Max(0f, elapsed - IdleAt);

            if (_dim != null)
            {
                float t = Mathf.Clamp01((elapsed - DimAt) / DimFor);
                _dim.color = new Color(0f, 0f, 0f, 0.42f * Ease(t) * leaving);
            }

            for (int i = 0; i < _drops.Length; i++)
            {
                if (_drops[i] == null) continue;
                float t = Mathf.Clamp01((elapsed - DropAt - i * DropStagger) / DropFor);
                // Overshoot on the way down, then a fabric wobble that decays --
                // a banner that stops dead at full length reads as a sprite
                // being scaled, which is what it is.
                float unroll = Overshoot(t, 1.06f);
                float wobble = Mathf.Sin(idle * 2.1f + i * 0.7f) * 0.012f * Mathf.Clamp01(idle / 0.8f);
                _drops[i].localScale = new Vector3(1f, (unroll + wobble) * leaving, 1f);
                _drops[i].localRotation = Quaternion.Euler(0f, 0f,
                    Mathf.Sin(idle * 1.5f + i * 1.1f) * 0.9f * Mathf.Clamp01(idle / 0.8f));
            }

            if (_ribbon != null)
            {
                float t = Mathf.Clamp01((elapsed - RibbonAt) / RibbonFor);
                float punch = Overshoot(t, 1.12f);
                float breathe = 1f + Mathf.Sin(idle * 1.7f) * 0.016f * Mathf.Clamp01(idle / 0.7f);
                _ribbon.localScale = Vector3.one * (punch * breathe * leaving);
                _ribbon.localRotation = Quaternion.Euler(0f, 0f,
                    -7f * (1f - t) * (1f - t) + Mathf.Sin(idle * 1.3f) * 1.4f * Mathf.Clamp01(idle / 0.7f));
            }

            for (int i = 0; i < _horns.Length; i++)
            {
                if (_horns[i] == null) continue;
                float t = Mathf.Clamp01((elapsed - HornAt - i * HornStagger) / HornFor);
                float out_ = Overshoot(t, 1.08f);
                bool right = i < 2;
                // They fan OUT from behind the ribbon, so they start at the
                // centre and travel to their rest position.
                Vector2 at = Vector2.Lerp(Vector2.zero, _hornRest[i], out_);
                at.y += Mathf.Sin(idle * 1.9f + i * 0.9f) * 5f * Mathf.Clamp01(idle / 0.9f);
                _horns[i].anchoredPosition = at;
                float swing = Mathf.Sin(idle * 2.3f + i * 0.8f) * 2.2f * Mathf.Clamp01(idle / 0.9f);
                _horns[i].localRotation = Quaternion.Euler(0f, 0f,
                    _hornAngle[i] * out_ + swing);
                float size = out_ * leaving;
                _horns[i].localScale = new Vector3((right ? 1f : -1f) * size, size, 1f);
            }
        }

        /// <summary>Smoothstep. Used where a thing fades rather than arrives.</summary>
        private static float Ease(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        /// <summary>
        /// Back-out: overshoots past 1 and settles. The same curve
        /// StoryWinStarVfx uses for the star, so the pieces of the celebration
        /// share one sense of weight instead of each easing its own way.
        /// </summary>
        private static float Overshoot(float t, float peak)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            float back = t - 1f;
            float s = (peak - 1f) * 9.8f;
            return 1f + (s + 1f) * back * back * back + s * back * back;
        }
    }
}
