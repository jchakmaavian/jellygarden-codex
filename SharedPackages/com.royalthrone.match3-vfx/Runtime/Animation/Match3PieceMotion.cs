using System;
using System.Collections;
using UnityEngine;

namespace RoyalThrone.Match3Vfx
{
    /// <summary>Options for <see cref="Match3PieceMotion.Pop"/>.</summary>
    public sealed class Match3PopOptions
    {
        public Match3PopKind kind = Match3PopKind.Match;
        /// <summary>Tint mixed into the flash, stars and fragments.</summary>
        public Color burstColor = Color.white;
        /// <summary>Colour index for painted chips in the burst library; -1 cuts chips from the sprite.</summary>
        public int colorIndex = -1;
        /// <summary>From <see cref="Match3Vfx.PlayMatchGroup"/> or <see cref="Match3Vfx.ComputeIntensity"/>.</summary>
        public float intensity = 1f;
        /// <summary>Delay before this piece starts. Use <see cref="Match3Vfx.StaggerFor"/>.</summary>
        public float stagger;
        /// <summary>Scale to animate around. Null = the root's scale when the pop starts.</summary>
        public Vector3? restScale;
        /// <summary>Renderers hidden when the piece breaks. Null = every SpriteRenderer under the root.</summary>
        public SpriteRenderer[] renderers;
        public bool burst = true;
        public bool shine = true;
        public bool shatter = true;
        /// <summary>Called once the piece is visually gone (particles keep playing). Unblock your cascade here.</summary>
        public Action onFinished;
    }

    /// <summary>
    /// Code-driven piece motion. Each method is a coroutine body: start it from your own
    /// MonoBehaviour with StartCoroutine so its lifetime follows your piece. Motion holds while
    /// <see cref="IMatch3VfxHost.IsPaused"/> is true.
    /// </summary>
    public static class Match3PieceMotion
    {
        static float Dt => Match3Vfx.Host.IsPaused ? 0f : Time.deltaTime;

        static float Advance(float t, float duration) => t + Dt / Mathf.Max(0.0001f, duration);

        /// <summary>Both pieces travel together with an OutCubic ease. Either transform may be null.</summary>
        public static IEnumerator Swap(Transform a, Transform b, Vector3 aTarget, Vector3 bTarget)
        {
            var s = Match3Vfx.Settings;
            Vector3 aStart = a ? a.position : aTarget;
            Vector3 bStart = b ? b.position : bTarget;
            float duration = s.enableEffects ? s.swapDuration : 0.2f;
            float t = 0f;
            while (t < 1f)
            {
                t = Advance(t, duration);
                float e = s.enableEffects ? Match3Easing.OutCubic(t) : Mathf.Clamp01(t);
                if (a) a.position = Vector3.LerpUnclamped(aStart, aTarget, e);
                if (b) b.position = Vector3.LerpUnclamped(bStart, bTarget, e);
                yield return null;
            }
            if (a) a.position = aTarget;
            if (b) b.position = bTarget;
        }

        /// <summary>
        /// Rejected swap: both pieces push part of the way toward each other, stop, and settle home
        /// with a small overshoot. Evaluate the rule first so the invalid case never completes a swap.
        /// </summary>
        public static IEnumerator InvalidSwap(Transform a, Transform b, Vector3 aHome, Vector3 bHome)
        {
            var s = Match3Vfx.Settings;
            if (!s.enableEffects)
            {
                if (a) a.position = aHome;
                if (b) b.position = bHome;
                yield break;
            }

            float t = 0f;
            float push = s.invalidSwapDuration * 0.42f;
            while (t < 1f)
            {
                t = Advance(t, push);
                float e = Match3Easing.OutQuad(t) * s.invalidSwapPushFraction;
                if (a) a.position = Vector3.LerpUnclamped(aHome, bHome, e);
                if (b) b.position = Vector3.LerpUnclamped(bHome, aHome, e);
                yield return null;
            }

            Vector3 aFrom = a ? a.position : aHome;
            Vector3 bFrom = b ? b.position : bHome;
            float back = s.invalidSwapDuration * 0.58f;
            t = 0f;
            while (t < 1f)
            {
                t = Advance(t, back);
                float e = Match3Easing.OutBack(t, s.invalidSwapOvershoot);
                if (a) a.position = Vector3.LerpUnclamped(aFrom, aHome, e);
                if (b) b.position = Vector3.LerpUnclamped(bFrom, bHome, e);
                yield return null;
            }
            if (a) a.position = aHome;
            if (b) b.position = bHome;
        }

        /// <summary>
        /// Accelerating fall with a capped speed, so long drops take longer than short ones.
        /// <paramref name="onLanded"/> runs only for drops long enough to deserve a landing.
        /// </summary>
        public static IEnumerator Fall(Transform piece, Vector3 target, Action onLanded = null)
        {
            if (!piece) yield break;
            var s = Match3Vfx.Settings;
            Vector3 start = piece.position;
            float distance = Vector3.Distance(start, target);
            bool animated = distance > s.minimumAnimatedDrop;
            if (animated && s.enableEffects)
            {
                float travelled = 0f;
                float velocity = s.fallSpeed;
                while (travelled < distance)
                {
                    if (!piece) yield break;
                    float dt = Dt;
                    velocity = Mathf.Min(velocity + s.fallAcceleration * dt, s.fallMaxSpeed);
                    travelled += velocity * dt;
                    piece.position = Vector3.LerpUnclamped(start, target, Mathf.Clamp01(travelled / distance));
                    yield return null;
                }
            }
            if (!piece) yield break;
            piece.position = target;
            if (animated) onLanded?.Invoke();
        }

        /// <summary>Y dips, overshoots slightly and settles (about 1.00 -> 0.94 -> 1.02 -> 1.00).</summary>
        public static IEnumerator LandingSquash(Transform piece, Vector3 restScale, Func<bool> cancel = null)
        {
            var s = Match3Vfx.Settings;
            if (!piece || !s.enableEffects || s.landingSquash <= 0f || s.landingSquashDuration <= 0f) yield break;
            float t = 0f;
            while (t < 1f)
            {
                if (!piece || (cancel != null && cancel())) yield break;
                t = Advance(t, s.landingSquashDuration);
                float k = Mathf.Clamp01(t);
                float dip = -Mathf.Sin(k * Mathf.PI * 1.5f) * Mathf.Pow(1f - k, 0.6f);
                float sy = 1f + dip * s.landingSquash;
                float sx = 1f - dip * s.landingSquash * 0.75f;
                piece.localScale = new Vector3(restScale.x * sx, restScale.y * sy, restScale.z);
                yield return null;
            }
            if (piece) piece.localScale = restScale;
        }

        /// <summary>Fall, then land with sound and squash.</summary>
        public static IEnumerator FallAndLand(Transform piece, Vector3 target, Vector3 restScale, bool playLandSound = true)
        {
            bool landed = false;
            yield return Fall(piece, target, () => landed = true);
            if (!landed || !piece) yield break;
            if (playLandSound) Match3Vfx.PlaySfx(Match3SfxCue.Land);
            yield return LandingSquash(piece, restScale);
        }

        /// <summary>A newly formed power-up grows in with a small overshoot and the creation effect.</summary>
        public static IEnumerator Creation(Transform piece, Vector3 restScale)
        {
            if (!piece) yield break;
            var s = Match3Vfx.Settings;
            Match3Vfx.PlayCreation(piece.position);
            if (!s.enableEffects) { piece.localScale = restScale; yield break; }
            float t = 0f;
            while (t < 1f)
            {
                if (!piece) yield break;
                t = Advance(t, 0.2f);
                piece.localScale = restScale * Mathf.LerpUnclamped(0.6f, 1f, Match3Easing.OutBack(t, 2.2f));
                yield return null;
            }
            if (piece) piece.localScale = restScale;
        }

        /// <summary>
        /// Staged pop: anticipation shrink, squash, rebound with shine, then the sprite breaks into
        /// chips on the same frame it hides. Shine and burst are scheduled before the first yield so
        /// they still play if the piece is disabled early. The root's scale and renderer colours are
        /// restored after hiding, so pooled pieces can be reused.
        /// </summary>
        public static IEnumerator Pop(Transform root, SpriteRenderer renderer, Match3PopOptions options = null)
        {
            options = options ?? new Match3PopOptions();
            if (!root)
            {
                options.onFinished?.Invoke();
                yield break;
            }

            var s = Match3Vfx.Settings;
            Vector3 rest = options.restScale ?? root.localScale;
            var renderers = options.renderers ?? root.GetComponentsInChildren<SpriteRenderer>(true);
            var colors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) colors[i] = renderers[i] ? renderers[i].color : Color.white;

            if (!s.enableEffects)
            {
                Hide(renderers, colors);
                options.onFinished?.Invoke();
                yield break;
            }

            if (options.kind == Match3PopKind.Rocket)
            {
                if (options.shatter && renderer && renderer.enabled)
                    Match3Vfx.Shatter(renderer, 1f, Match3ShatterStyle.Rocket, options.colorIndex);
                Hide(renderers, colors);
                float hold = 0f;
                while (hold < 0.065f) { yield return null; hold += Dt; }
                options.onFinished?.Invoke();
                yield break;
            }

            var timing = options.kind == Match3PopKind.Match ? s.matchPop : s.powerClearPop;
            float toShine = options.stagger + timing.anticipation + timing.squash;
            float toBurst = toShine + timing.rebound;
            if (options.shine && renderer && renderer.enabled) Match3Vfx.PlayShine(renderer, toShine);
            if (options.burst)
            {
                float burstIntensity = options.kind == Match3PopKind.PowerClear ? s.powerClearBurstIntensity : options.intensity;
                Match3Vfx.PlayMatchBurst(root.position, options.burstColor, burstIntensity, toBurst);
            }

            float wait = 0f;
            while (wait < options.stagger) { yield return null; wait += Dt; }

            float squash = timing.squashAmount;
            float anticipation = timing.anticipationScale;
            float rebound = timing.reboundAmount;

            // 1. anticipation
            float t = 0f;
            while (t < 1f)
            {
                if (!root) { options.onFinished?.Invoke(); yield break; }
                t = Advance(t, timing.anticipation);
                float e = timing.smooth ? Match3Easing.Smooth(t) : Match3Easing.OutQuad(t);
                float scale = 1f - anticipation * e;
                root.localScale = new Vector3(rest.x * scale, rest.y * scale, rest.z);
                yield return null;
            }

            // 2. squash
            t = 0f;
            float from = 1f - anticipation;
            while (t < 1f)
            {
                if (!root) { options.onFinished?.Invoke(); yield break; }
                t = Advance(t, timing.squash);
                float e = timing.smooth ? Match3Easing.Smooth(t) : Match3Easing.OutCubic(t);
                root.localScale = new Vector3(rest.x * Mathf.Lerp(from, 1f + squash, e), rest.y * Mathf.Lerp(from, 1f - squash * 1.15f, e), rest.z);
                yield return null;
            }

            // 3. rebound (the shine was scheduled up front)
            t = 0f;
            while (t < 1f)
            {
                if (!root) { options.onFinished?.Invoke(); yield break; }
                t = Advance(t, timing.rebound);
                float e = timing.smooth ? Match3Easing.Smooth(t) : Match3Easing.OutQuad(t);
                root.localScale = new Vector3(rest.x * Mathf.Lerp(1f + squash, 1f - rebound * 0.35f, e), rest.y * Mathf.Lerp(1f - squash * 1.15f, 1f + rebound, e), rest.z);
                yield return null;
            }

            // 4. break apart: chips take over the silhouette on the same frame the sprite hides.
            bool shattered = false;
            if (options.shatter && s.enableShatter && renderer && renderer.enabled && renderer.sprite)
            {
                Match3Vfx.Shatter(renderer, options.intensity,
                    options.kind == Match3PopKind.Match ? Match3ShatterStyle.Match : Match3ShatterStyle.PowerClear, options.colorIndex);
                shattered = true;
                foreach (var r in renderers) if (r) r.enabled = false;
            }

            float startX = 1f - rebound * 0.35f, startY = 1f + rebound;
            t = 0f;
            while (t < 1f)
            {
                if (!root) { options.onFinished?.Invoke(); yield break; }
                t = Advance(t, timing.disappear);
                if (!shattered)
                {
                    float grow = Mathf.Lerp(1f, 1.25f, Match3Easing.InQuad(t));
                    root.localScale = new Vector3(rest.x * startX * grow, rest.y * startY * grow, rest.z);
                    float alpha = 1f - Match3Easing.OutQuad(t);
                    for (int i = 0; i < renderers.Length; i++)
                    {
                        if (!renderers[i]) continue;
                        var c = colors[i];
                        c.a *= alpha;
                        renderers[i].color = c;
                    }
                }
                yield return null;
            }

            Hide(renderers, colors);
            if (root) root.localScale = rest;
            options.onFinished?.Invoke();
        }

        static void Hide(SpriteRenderer[] renderers, Color[] colors)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (!renderers[i]) continue;
                renderers[i].enabled = false;
                renderers[i].color = colors[i];
            }
        }
    }
}
