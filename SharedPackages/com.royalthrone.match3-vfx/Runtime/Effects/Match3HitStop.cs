using UnityEngine;

namespace RoyalThrone.Match3Vfx
{
    /// <summary>
    /// Micro-freeze on impact, driven on unscaled time because it is what moves timeScale.
    /// Requests coalesce (longest wins). If anything else changes timeScale during the freeze
    /// (a pause menu, a debug slow-motion) the hit-stop surrenders instead of restoring over it.
    /// </summary>
    internal sealed class Match3HitStop
    {
        bool active, queued;
        float remaining, restore = 1f, applied;
        float pendingDelay, pendingDuration, pendingScale = 1f;

        public bool Active => active;
        public bool Queued => queued;

        public void Request(float delay, float duration, float scale)
        {
            if (duration <= 0f) return;
            if (queued)
            {
                pendingDelay = Mathf.Min(pendingDelay, delay);
                pendingDuration = Mathf.Max(pendingDuration, duration);
                pendingScale = Mathf.Min(pendingScale, Mathf.Clamp(scale, 0.01f, 1f));
                return;
            }
            queued = true;
            pendingDelay = delay;
            pendingDuration = duration;
            pendingScale = Mathf.Clamp(scale, 0.01f, 1f);
        }

        public void Tick(float unscaledDt)
        {
            if (queued && !active)
            {
                pendingDelay -= unscaledDt;
                if (pendingDelay > 0f) return;
                queued = false;
                // Only engage from normal play; never override a pause at 0 or a slow-motion.
                if (Time.timeScale > 0.5f)
                {
                    restore = Time.timeScale;
                    applied = restore * pendingScale;
                    Time.timeScale = applied;
                    remaining = pendingDuration;
                    active = true;
                }
            }
            else if (active)
            {
                if (!Mathf.Approximately(Time.timeScale, applied)) { active = false; return; }
                remaining -= unscaledDt;
                if (remaining <= 0f)
                {
                    Time.timeScale = restore;
                    active = false;
                }
            }
        }

        /// <summary>Restores timeScale if a freeze is running and drops any queued request.</summary>
        public void Release()
        {
            if (active && Mathf.Approximately(Time.timeScale, applied)) Time.timeScale = restore;
            active = false;
            queued = false;
        }
    }
}
