using System.Collections.Generic;
using UnityEngine;

namespace RoyalThrone.Match3Vfx
{
    /// <summary>
    /// Layered burst for matched pieces. Four shared world-space ParticleSystems (stars, micro
    /// sparkles, tinted fragments, centre stars) are moved and emitted into, so a match creates
    /// no objects and no extra draw calls. Flash, ring and shine are pooled sprites advanced from
    /// one Tick. Staggered layers use a fixed pending pool instead of coroutines.
    /// </summary>
    internal sealed class MatchBurstFx
    {
        public const int OverlayPoolSize = 96;
        public const int PendingPoolSize = 128;

        enum OverlayMode { Flash, Ring, Shine }

        sealed class Overlay
        {
            public SpriteRenderer renderer;
            public Transform tr;
            public bool active;
            public float time, duration, startScale, endScale, delay;
            public Color tint;
            public OverlayMode mode;
        }

        sealed class Pending
        {
            public ParticleSystem ps;
            public Vector3 position;
            public Color tint;
            public float speed, lifetime, size, remaining;
            public int count;
            public bool active;
        }

        readonly Match3VfxRig rig;
        readonly List<Material> materials = new List<Material>(6);
        readonly Overlay[] overlays = new Overlay[OverlayPoolSize];
        readonly Pending[] pending = new Pending[PendingPoolSize];
        readonly ParticleSystem stars, micro, fragments, centerStars;
        int overlayCursor, pendingCursor;

        Match3VfxSettings S => rig.Settings;

        public int ActiveOverlays
        {
            get { int n = 0; foreach (var o in overlays) if (o.active) n++; return n; }
        }

        public int ParticleCount => stars.particleCount + micro.particleCount + fragments.particleCount + centerStars.particleCount;

        public MatchBurstFx(Match3VfxRig rig)
        {
            this.rig = rig;
            var root = new GameObject("Match bursts").transform;
            root.SetParent(rig.transform, false);

            var overlayMaterial = Match3VfxMaterials.CreateAdditiveSprite(S.hdrBoost);
            materials.Add(overlayMaterial);

            stars = BuildSystem(root, "Stars", Match3Shapes.FourPointSparkle, 144, 0.14f);
            micro = BuildSystem(root, "Micro sparkles", Match3Shapes.Diamond, 192, 0.22f);
            fragments = BuildSystem(root, "Tinted fragments", Match3Shapes.Diamond, 64, 0.9f);
            centerStars = BuildSystem(root, "Centre stars", Match3Shapes.SixPointStar, 80, 0.12f);

            for (int i = 0; i < pending.Length; i++) pending[i] = new Pending();
            for (int i = 0; i < overlays.Length; i++)
            {
                var go = new GameObject("Overlay " + i);
                go.transform.SetParent(root, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sharedMaterial = overlayMaterial;
                sr.sortingLayerName = S.sortingLayerName;
                sr.sortingOrder = S.burstSortingOrder;
                sr.enabled = false;
                overlays[i] = new Overlay { renderer = sr, tr = go.transform };
            }
        }

        ParticleSystem BuildSystem(Transform parent, string label, Texture2D texture, int maxParticles, float gravity)
        {
            var go = new GameObject(label);
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = maxParticles;
            main.gravityModifier = gravity;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.04f;
            shape.radiusThickness = 1f;
            shape.arcMode = ParticleSystemShapeMultiModeValue.Random;

            // Drag, not dampen: dampen runs per rendered frame and would widen bursts at low frame rates.
            var limit = ps.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.limit = new ParticleSystem.MinMaxCurve(0.9f);
            limit.dampen = 0f;
            limit.drag = new ParticleSystem.MinMaxCurve(S.particleDrag);
            limit.multiplyDragByParticleSize = false;
            limit.multiplyDragByParticleVelocity = true;

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            var sizeCurve = new AnimationCurve(new Keyframe(0f, 0.85f), new Keyframe(0.12f, 1.12f), new Keyframe(1f, 0.3f));
            size.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            var color = ps.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.55f), new GradientAlphaKey(0f, 1f) });
            color.color = new ParticleSystem.MinMaxGradient(gradient);

            var rotation = ps.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-3.5f, 3.5f);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.sortingLayerName = S.sortingLayerName;
            renderer.sortingOrder = S.burstSortingOrder;
            var material = Match3VfxMaterials.CreateAdditiveParticle(texture, S.hdrBoost);
            materials.Add(material);
            renderer.sharedMaterial = material;

            ps.Play();
            return ps;
        }

        // ------------------------------------------------------------------

        public void PlayBurst(Vector3 position, Color color, float intensity, float delay)
        {
            var s = S;
            float cell = rig.Cell;
            position.z -= 0.5f;
            // Intensity drives count and speed fully but only a third of it reaches size, so a
            // deep cascade never throws elements that cover the neighbouring pieces.
            float sizeIntensity = Mathf.Lerp(1f, intensity, 0.35f);

            var flash = rig.ReducedMotion ? null : Take();
            if (flash != null)
            {
                flash.mode = OverlayMode.Flash;
                flash.renderer.sprite = Match3Shapes.GlowSprite;
                flash.tr.position = position;
                flash.tr.localRotation = Quaternion.identity;
                flash.duration = s.flashDuration;
                flash.time = 0f;
                flash.startScale = s.flashStartScale * cell * sizeIntensity;
                flash.endScale = s.flashEndScale * cell * sizeIntensity;
                flash.tint = Color.Lerp(Color.white, color, 0.25f);
                flash.tint.a = 1f;
                flash.delay = delay;
                flash.active = true;
                flash.renderer.enabled = delay <= 0f;
            }

            Schedule(stars, position, delay + s.starDelay,
                Mathf.Clamp(Mathf.RoundToInt(s.starCount * intensity), 1, 8),
                Color.Lerp(Color.white, color, 0.18f), s.particleVelocity * cell, s.particleLifetime,
                s.starSize * cell * sizeIntensity);
            Schedule(micro, position, delay + s.microDelay,
                Mathf.Clamp(Mathf.RoundToInt(s.microCount * intensity), 2, 12),
                Color.Lerp(Color.white, color, 0.1f), s.particleVelocity * 1.35f * cell, s.microLifetime,
                s.microSize * cell * sizeIntensity);
            Schedule(fragments, position, delay + s.fragmentDelay,
                Mathf.RoundToInt(s.fragmentCount * intensity),
                Color.Lerp(Color.white, color, s.fragmentColorBlend), s.particleVelocity * 0.72f * cell, s.fragmentLifetime,
                s.fragmentSize * cell * sizeIntensity);
        }

        public void PlayCenter(Vector3 position, Color color, float intensity, float delay)
        {
            if (delay > 0f) rig.Schedule(delay, () => PlayCenterNow(position, color, intensity));
            else PlayCenterNow(position, color, intensity);
        }

        void PlayCenterNow(Vector3 position, Color color, float intensity)
        {
            var s = S;
            if (!s.enableCenterAccent) return;
            if (rig.Impacts.TryPlayMatch(position, intensity))
            {
                rig.Powerups.MatchAccentSound(intensity);
                return;
            }

            float cell = rig.Cell;
            position.z -= 0.55f;
            float sizeIntensity = Mathf.Lerp(1f, intensity, 0.35f);
            var ring = Take();
            if (ring != null)
            {
                ring.mode = OverlayMode.Ring;
                ring.renderer.sprite = Match3Shapes.RingSprite;
                ring.tr.position = position;
                ring.tr.localRotation = Quaternion.identity;
                ring.duration = s.centerRingDuration;
                ring.time = 0f;
                ring.startScale = 0.25f * cell;
                ring.endScale = s.centerRingEndScale * cell * sizeIntensity;
                ring.tint = Color.Lerp(Color.white, color, 0.35f);
                ring.tint.a = 1f;
                ring.delay = 0f;
                ring.active = true;
                ring.renderer.enabled = true;
            }

            Emit(centerStars, position, Mathf.RoundToInt(s.centerStarCount * intensity),
                Color.Lerp(Color.white, color, 0.2f), s.particleVelocity * 0.85f * cell,
                s.particleLifetime * 1.15f, s.starSize * 1.25f * cell * sizeIntensity);
            rig.Powerups.Firework(position, intensity);
        }

        public void PlayShine(SpriteRenderer source, float duration, float strength, float delay)
        {
            if (!source || !source.sprite || rig.ReducedMotion) return;
            var o = Take();
            if (o == null) return;
            o.mode = OverlayMode.Shine;
            o.renderer.sprite = source.sprite;
            o.renderer.flipX = source.flipX;
            o.renderer.flipY = source.flipY;
            // Snapshot the transform so the shine survives the piece being disabled.
            var st = source.transform;
            o.tr.position = st.position + new Vector3(0f, 0f, -0.4f);
            o.tr.rotation = st.rotation;
            o.tr.localScale = st.lossyScale;
            o.duration = duration;
            o.time = 0f;
            o.tint = new Color(1f, 1f, 1f, strength);
            o.delay = delay;
            o.active = true;
            o.renderer.enabled = delay <= 0f;
        }

        // ------------------------------------------------------------------

        void Schedule(ParticleSystem ps, Vector3 position, float delay, int count, Color tint, float speed, float lifetime, float size)
        {
            if (count <= 0) return;
            if (delay <= 0f) { Emit(ps, position, count, tint, speed, lifetime, size); return; }
            for (int i = 0; i < pending.Length; i++)
            {
                int index = (pendingCursor + i) % pending.Length;
                var p = pending[index];
                if (p.active) continue;
                p.ps = ps; p.position = position; p.count = count; p.tint = tint;
                p.speed = speed; p.lifetime = lifetime; p.size = size; p.remaining = delay; p.active = true;
                pendingCursor = (index + 1) % pending.Length;
                return;
            }
            // Queue full during a very large clear: emit now rather than drop it.
            Emit(ps, position, count, tint, speed, lifetime, size);
        }

        void Emit(ParticleSystem ps, Vector3 position, int count, Color tint, float speed, float lifetime, float size)
        {
            if (!ps || count <= 0) return;
            if (rig.ReducedMotion) count = Mathf.Max(1, count / 3);
            ps.transform.position = position;
            var main = ps.main;
            main.startColor = tint;
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.55f, speed * 1.15f);
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.7f, lifetime * 1.15f);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size * 1.3f);
            ps.Emit(count);
        }

        Overlay Take()
        {
            for (int i = 0; i < overlays.Length; i++)
            {
                int index = (overlayCursor + i) % overlays.Length;
                if (overlays[index].active) continue;
                overlayCursor = (index + 1) % overlays.Length;
                return overlays[index];
            }
            return null;
        }

        public void Tick(float dt)
        {
            for (int i = 0; i < pending.Length; i++)
            {
                var p = pending[i];
                if (!p.active) continue;
                p.remaining -= dt;
                if (p.remaining > 0f) continue;
                p.active = false;
                Emit(p.ps, p.position, p.count, p.tint, p.speed, p.lifetime, p.size);
                p.ps = null;
            }

            for (int i = 0; i < overlays.Length; i++)
            {
                var o = overlays[i];
                if (!o.active) continue;
                if (o.delay > 0f)
                {
                    o.delay -= dt;
                    if (o.delay > 0f) continue;
                    o.renderer.enabled = true;
                }

                o.time += dt;
                float t = o.duration > 0f ? Mathf.Clamp01(o.time / o.duration) : 1f;
                var c = o.tint;
                switch (o.mode)
                {
                    case OverlayMode.Flash:
                    {
                        float s = Mathf.Lerp(o.startScale, o.endScale, Match3Easing.OutCubic(t));
                        o.tr.localScale = new Vector3(s, s, 1f);
                        c.a = 1f - Match3Easing.InQuad(t);
                        break;
                    }
                    case OverlayMode.Ring:
                    {
                        float s = Mathf.Lerp(o.startScale, o.endScale, Match3Easing.OutCubic(t));
                        o.tr.localScale = new Vector3(s, s, 1f);
                        c.a = (1f - Match3Easing.InQuad(t)) * 0.9f;
                        break;
                    }
                    case OverlayMode.Shine:
                        c.a = o.tint.a * (1f - Match3Easing.InQuad(t));
                        break;
                }
                o.renderer.color = c;

                if (t >= 1f)
                {
                    o.active = false;
                    o.renderer.enabled = false;
                }
            }
        }

        public void SetPaused(bool paused)
        {
            SetPaused(stars, paused);
            SetPaused(micro, paused);
            SetPaused(fragments, paused);
            SetPaused(centerStars, paused);
        }

        static void SetPaused(ParticleSystem ps, bool paused)
        {
            if (!ps) return;
            if (paused && !ps.isPaused) ps.Pause();
            else if (!paused && ps.isPaused) ps.Play();
        }

        public void Clear()
        {
            foreach (var o in overlays) { o.active = false; o.renderer.enabled = false; }
            foreach (var p in pending) { p.active = false; p.ps = null; }
            foreach (var ps in new[] { stars, micro, fragments, centerStars })
                if (ps) { ps.Clear(); if (!ps.isPlaying) ps.Play(); }
        }

        public void Dispose()
        {
            foreach (var m in materials) if (m) Object.Destroy(m);
            materials.Clear();
        }
    }
}
