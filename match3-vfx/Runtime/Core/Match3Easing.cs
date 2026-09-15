using UnityEngine;

namespace RoyalThrone.Match3Vfx
{
    /// <summary>Allocation-free easing used by every motion in the kit. Inputs are clamped to 0-1.</summary>
    public static class Match3Easing
    {
        public static float OutQuad(float t)
        {
            t = Mathf.Clamp01(t);
            return 1f - (1f - t) * (1f - t);
        }

        public static float OutCubic(float t)
        {
            t = Mathf.Clamp01(t);
            float f = 1f - t;
            return 1f - f * f * f;
        }

        public static float InQuad(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t;
        }

        public static float InCubic(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * t;
        }

        public static float InOutQuad(float t)
        {
            t = Mathf.Clamp01(t);
            return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) * 0.5f;
        }

        /// <summary>Overshoot on the way out. 1.70158 is the classic amount.</summary>
        public static float OutBack(float t, float overshoot = 1.70158f)
        {
            t = Mathf.Clamp01(t);
            float c3 = overshoot + 1f;
            float f = t - 1f;
            return 1f + c3 * f * f * f + overshoot * f * f;
        }

        /// <summary>Pull back slightly before moving forward.</summary>
        public static float InBack(float t, float overshoot = 1.70158f)
        {
            t = Mathf.Clamp01(t);
            float c3 = overshoot + 1f;
            return c3 * t * t * t - overshoot * t * t;
        }

        public static float OutElastic(float t, float period = 0.35f)
        {
            t = Mathf.Clamp01(t);
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            float c4 = (2f * Mathf.PI) / period;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
        }

        /// <summary>0 -> 1 -> 0, peaking at the midpoint.</summary>
        public static float Hump(float t)
        {
            t = Mathf.Clamp01(t);
            return Mathf.Sin(t * Mathf.PI);
        }

        public static float Smooth(float t) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
    }
}
