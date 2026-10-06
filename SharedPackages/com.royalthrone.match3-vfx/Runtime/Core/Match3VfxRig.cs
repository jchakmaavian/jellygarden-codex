using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RoyalThrone.Match3Vfx
{
    /// <summary>
    /// Owns every pool of the kit and advances them from one Update. Created by
    /// <see cref="Match3Vfx"/>; do not add it to scenes yourself.
    /// </summary>
    [AddComponentMenu("")]
    internal sealed class Match3VfxRig : MonoBehaviour
    {
        struct Scheduled
        {
            public float remaining;
            public Action action;
        }

        readonly List<Scheduled> scheduled = new List<Scheduled>(32);
        readonly List<Action> due = new List<Action>(16);

        public Match3VfxSettings Settings { get; private set; }
        public MatchBurstFx Burst { get; private set; }
        public PieceShatterFx Shatter { get; private set; }
        public PowerupFx Powerups { get; private set; }
        public PowerupSequencer Sequences { get; private set; }
        public ImpactPrefabPool Impacts { get; private set; }
        public Match3HitStop HitStop { get; private set; }
        public Match3CameraShake Shake { get; private set; }
        public Match3Sfx Sfx { get; private set; }
        public ComboBanner Banner { get; private set; }

        public IMatch3VfxHost Host => Match3Vfx.Host;
        public float Cell => Mathf.Max(0.0001f, Host.CellSize);
        public bool Paused => Host.IsPaused;
        public bool ReducedMotion => Host.ReducedMotion;
        public int ScheduledCount => scheduled.Count;

        bool lastPaused;

        public void Build(Match3VfxSettings settings)
        {
            Settings = settings;
            HitStop = new Match3HitStop();
            Shake = new Match3CameraShake(this);
            Impacts = new ImpactPrefabPool(this);
            Burst = new MatchBurstFx(this);
            Shatter = new PieceShatterFx(this);
            Powerups = new PowerupFx(this);
            Sequences = new PowerupSequencer(this);
            Sfx = new Match3Sfx(this);
            Banner = new ComboBanner(this);
            Match3UrpBloom.Apply(settings);
        }

        /// <summary>Runs <paramref name="action"/> after <paramref name="delay"/> seconds of unpaused time.</summary>
        public void Schedule(float delay, Action action)
        {
            if (action == null) return;
            if (delay <= 0f) { action(); return; }
            scheduled.Add(new Scheduled { remaining = delay, action = action });
        }

        /// <summary>Waits the given seconds, holding while the host is paused.</summary>
        public IEnumerator Wait(float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                yield return null;
                if (!Paused) t += Time.deltaTime;
            }
        }

        void Update()
        {
            if (Burst == null) return;
            HitStop.Tick(Time.unscaledDeltaTime);

            bool paused = Paused;
            if (paused != lastPaused)
            {
                Burst.SetPaused(paused);
                Powerups.SetPaused(paused);
                Impacts.SetPaused(paused);
                lastPaused = paused;
            }

            Sfx.Tick();
            Banner.Tick(Time.deltaTime, paused);
            if (paused) return;

            float dt = Time.deltaTime;
            TickScheduled(dt);
            Burst.Tick(dt);
            Shatter.Tick(dt);
            Impacts.Tick(dt);
            Powerups.Tick(dt);
        }

        void LateUpdate()
        {
            if (Shake != null) Shake.LateTick(Time.deltaTime);
        }

        void TickScheduled(float dt)
        {
            for (int i = scheduled.Count - 1; i >= 0; i--)
            {
                var s = scheduled[i];
                s.remaining -= dt;
                if (s.remaining <= 0f)
                {
                    due.Add(s.action);
                    scheduled.RemoveAt(i);
                }
                else scheduled[i] = s;
            }
            // Removed back-to-front; run in the order they were scheduled.
            for (int i = due.Count - 1; i >= 0; i--)
            {
                try { due[i](); }
                catch (Exception e) { Debug.LogException(e); }
            }
            due.Clear();
        }

        public void ClearAll()
        {
            if (Burst == null) return;
            StopAllCoroutines();
            scheduled.Clear();
            Sequences.Cancel();
            Burst.Clear();
            Shatter.Clear();
            Powerups.Clear();
            Impacts.Clear();
            HitStop.Release();
            Shake.Reset();
            Banner.Cancel();
        }

        /// <summary>Board-free previews for editor tools and menus.</summary>
        public void Preview(Match3PreviewEffect effect, Vector3 p)
        {
            var s = Settings;
            switch (effect)
            {
                case Match3PreviewEffect.Match3:
                    for (int i = -1; i <= 1; i++)
                        Burst.PlayBurst(p + Vector3.right * i * Cell, new Color(0.2f, 0.7f, 1f), 1f, (i + 1) * s.matchStagger);
                    Burst.PlayCenter(p, Color.white, 1f, 0f);
                    Sfx.Play(Match3SfxCue.Match);
                    break;
                case Match3PreviewEffect.Match4:
                case Match3PreviewEffect.Match5:
                    Burst.PlayCenter(p, Color.white, effect == Match3PreviewEffect.Match5 ? 1f + s.fiveMatchBonus : 1f + s.fourMatchBonus, 0f);
                    Powerups.Creation(p);
                    break;
                case Match3PreviewEffect.Tnt:
                    Powerups.Blast(p, 1f);
                    Sfx.Play(Match3SfxCue.Tnt);
                    break;
                case Match3PreviewEffect.Creation:
                    Powerups.Creation(p);
                    break;
                case Match3PreviewEffect.Firework:
                    Powerups.Firework(p, s.intensityMax);
                    break;
                case Match3PreviewEffect.ComboBanner:
                    Banner.Show(s.bannerMinCascade + 1, true);
                    break;
                case Match3PreviewEffect.Rocket:
                case Match3PreviewEffect.Helicopter:
                    StartCoroutine(PreviewFlight(effect == Match3PreviewEffect.Helicopter, p));
                    break;
            }
        }

        IEnumerator PreviewFlight(bool helicopter, Vector3 p)
        {
            var f = Powerups.TakeFlyer(helicopter ? Match3Power.Helicopter : Match3Power.RocketHorizontal, p, 0.9f);
            Sfx.Play(helicopter ? Match3SfxCue.Helicopter : Match3SfxCue.Rocket);
            if (f != null && !helicopter)
            {
                f.ConfigureRocketTrail(Cell);
                f.StartTrails();
            }
            float t = 0f;
            while (t < 0.55f)
            {
                yield return null;
                if (Paused) continue;
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / 0.55f);
                if (f != null)
                {
                    f.Root.position = p + new Vector3((k - 0.5f) * Cell * 7f, helicopter ? Mathf.Sin(k * Mathf.PI) * Cell * 2f : 0f, -0.6f);
                    f.Root.rotation = Quaternion.Euler(0f, 0f, helicopter ? -15f : 0f);
                }
            }
            Powerups.Blast(p + Vector3.right * Cell * 3.5f, 0.5f);
            if (f != null) Powerups.Retire(f);
        }

        void OnDestroy()
        {
            HitStop?.Release();
            Shake?.Reset();
            Burst?.Dispose();
            Shatter?.Dispose();
            Powerups?.Dispose();
            Impacts?.Dispose();
            Banner?.Dispose();
        }
    }
}
