using System.Collections.Generic;
using UnityEngine;

namespace RoyalThrone.Match3Vfx
{
    /// <summary>A pooled projectile: rocket head, TNT fuse or helicopter, with trail and optional rotor.</summary>
    internal sealed class Flyer
    {
        public GameObject go;
        public Transform Root;
        public Transform art;
        public SpriteRenderer sprite;
        public TrailRenderer trail, core;
        public Transform rotor;
        public bool used, rocket;
        public int generation;
        public float artFit;

        /// <summary>Scales the art around its fitted size (anticipation squash, fuse pulse).</summary>
        public void SetPunch(float factor) => art.localScale = Vector3.one * artFit * factor;

        public void ConfigureRocketTrail(float pitch)
        {
            rocket = true;
            trail.emitting = false;
            trail.time = 0.32f;
            trail.widthMultiplier = 0.38f * pitch;
            trail.startColor = new Color(1f, 0.67f, 0.15f, 0.7f);
            trail.endColor = new Color(1f, 0.3f, 0.025f, 0f);
            core.time = 0.28f;
            core.widthMultiplier = 0.09f * pitch;
        }

        public void StartTrails()
        {
            trail.Clear();
            core.Clear();
            trail.emitting = true;
            core.emitting = rocket;
        }
    }

    /// <summary>
    /// Native particle rig for power-ups (smoke, aura rays, sparkles, embers, core flash,
    /// shockwave, rocket contact ring/glow/stars) plus the projectile pool. Emission goes through
    /// EmitParams into shared world-space systems, so no object is created during play.
    /// </summary>
    internal sealed class PowerupFx
    {
        readonly Match3VfxRig rig;
        readonly List<Material> materials = new List<Material>(10);
        readonly List<ParticleSystem> systems = new List<ParticleSystem>(9);
        readonly ParticleSystem smoke, rays, sparkles, embers, impact, shockwave, rocketRing, rocketGlow, rocketStars;
        readonly Flyer[] flyers;
        readonly System.Random random = new System.Random(72813);
        float lastCoreFlash = -10f, lastAccentSound = -10f;

        Match3VfxSettings S => rig.Settings;

        public int Blasts { get; private set; }
        public int PeakProjectiles { get; private set; }
        public int ProjectileCapacity => flyers.Length;
        public int ActiveProjectiles
        {
            get { int n = 0; foreach (var f in flyers) if (f.used) n++; return n; }
        }
        public int ParticleCount
        {
            get { int n = 0; foreach (var ps in systems) n += ps.particleCount; return n; }
        }

        public PowerupFx(Match3VfxRig rig)
        {
            this.rig = rig;
            var s = S;
            int order = s.powerupSortingOrder;
            var root = new GameObject("Power-up particles").transform;
            root.SetParent(rig.transform, false);

            var smokeTexture = s.smokeTexture ? s.smokeTexture : Match3Shapes.SmokePuff;
            smoke = System(root, "Smoke", Alpha(smokeTexture), 96, order, 0f,
                new AnimationCurve(new Keyframe(0f, 0.45f), new Keyframe(0.28f, 1f), new Keyframe(1f, 1.25f)));
            shockwave = System(root, "Shockwave", Additive(Match3Shapes.Ring, 1.25f), 24, order + 3, 0f, AnimationCurve.EaseInOut(0f, 0.2f, 1f, 3.3f));
            embers = System(root, "Embers", Additive(Match3Shapes.Diamond, 1.25f), 256, order + 4, 0.65f, AnimationCurve.Linear(0f, 1f, 1f, 0.3f));
            rays = System(root, "Aura rays", Additive(Match3Shapes.Diamond, 1.25f), 160, order + 7, 0f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));
            var rayRenderer = rays.GetComponent<ParticleSystemRenderer>();
            rayRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            rayRenderer.lengthScale = 7.2f;
            rayRenderer.velocityScale = 0.04f;
            rayRenderer.cameraVelocityScale = 0f;
            impact = System(root, "Core flash", Additive(Match3Shapes.SoftGlow, 1.25f), 24, order + 8, 0f, AnimationCurve.EaseInOut(0f, 0.4f, 1f, 1.4f));
            sparkles = System(root, "Sparkles", Additive(Match3Shapes.FourPointSparkle, 1.25f), Mathf.Clamp(s.sparkleBudget, 100, 1000), order + 9, 0.2f,
                new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(0.15f, 1f), new Keyframe(0.6f, 0.7f), new Keyframe(1f, 0f)));

            // Rocket contact layers shrink as they fade and draw above the heads.
            var shrink = AnimationCurve.EaseInOut(0f, 0.9f, 1f, 0.15f);
            rocketRing = System(root, "Rocket contact ring", Additive(Match3Shapes.Ring, 1.4f), 64, order + 13, 0f, shrink, true);
            rocketGlow = System(root, "Rocket contact glow", Additive(Match3Shapes.SoftGlow, 1.4f), 64, order + 13, 0f, shrink, true);
            rocketStars = System(root, "Rocket stars", Additive(Match3Shapes.FourPointSparkle, 1.4f), 768, order + 13, 0f, shrink, true);

            flyers = BuildFlyers(Mathf.Clamp(s.projectilePool, 8, 64), order);
        }

        Material Additive(Texture2D texture, float boost)
        {
            var m = Match3VfxMaterials.CreateAdditiveParticle(texture, boost);
            materials.Add(m);
            return m;
        }

        Material Alpha(Texture2D texture)
        {
            var m = Match3VfxMaterials.CreateParticleAlpha(texture);
            materials.Add(m);
            return m;
        }

        ParticleSystem System(Transform parent, string name, Material material, int capacity, int order, float gravity, AnimationCurve sizes, bool rocketFade = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = capacity;
            main.startSpeed = 0f;
            main.startSize = 1f;
            main.startLifetime = 0.4f;
            main.gravityModifier = gravity;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            var shape = ps.shape;
            shape.enabled = false;

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, sizes);

            var color = ps.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                rocketFade
                    ? new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.85f, 0.35f), new GradientAlphaKey(0f, 1f) }
                    : new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.95f, 0.2f), new GradientAlphaKey(0f, 1f) });
            color.color = gradient;

            var drag = ps.limitVelocityOverLifetime;
            drag.enabled = true;
            drag.limit = 100f;
            drag.dampen = 0f;
            drag.drag = 2f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingLayerName = S.sortingLayerName;
            renderer.sortingOrder = order;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            ps.useAutoRandomSeed = false;
            ps.randomSeed = 37183;
            ps.Play();
            systems.Add(ps);
            return ps;
        }

        Flyer[] BuildFlyers(int count, int order)
        {
            var root = new GameObject("Projectiles").transform;
            root.SetParent(rig.transform, false);
            var trailMaterial = Match3VfxMaterials.CreateAdditiveParticle(Match3Shapes.SoftGlow, 1.25f);
            var coreMaterial = Match3VfxMaterials.CreateAdditiveParticle(Texture2D.whiteTexture, 1.7f);
            materials.Add(trailMaterial);
            materials.Add(coreMaterial);
            string layer = S.sortingLayerName;

            var pool = new Flyer[count];
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("Projectile " + i);
                go.transform.SetParent(root, false);

                var artObject = new GameObject("Art");
                artObject.transform.SetParent(go.transform, false);
                var sr = artObject.AddComponent<SpriteRenderer>();
                sr.sortingLayerName = layer;
                sr.sortingOrder = order + 10;

                var trail = go.AddComponent<TrailRenderer>();
                trail.sharedMaterial = trailMaterial;
                trail.sortingLayerName = layer;
                trail.sortingOrder = order + 6;
                trail.time = 0.16f;
                trail.minVertexDistance = 0.06f;
                trail.numCapVertices = 3;
                trail.numCornerVertices = 2;
                trail.widthCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
                trail.emitting = false;

                var coreObject = new GameObject("White-hot core");
                coreObject.transform.SetParent(go.transform, false);
                var core = coreObject.AddComponent<TrailRenderer>();
                core.sharedMaterial = coreMaterial;
                core.sortingLayerName = layer;
                core.sortingOrder = order + 9;
                core.time = 0.3f;
                core.minVertexDistance = 0.025f;
                core.numCapVertices = 4;
                core.widthCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
                core.startColor = Color.white;
                core.endColor = new Color(1f, 1f, 0.85f, 0f);
                core.emitting = false;

                var rotor = new GameObject("Rotor").transform;
                rotor.SetParent(go.transform, false);
                for (int blade = 0; blade < 2; blade++)
                {
                    var arm = new GameObject("Blade").AddComponent<LineRenderer>();
                    arm.transform.SetParent(rotor, false);
                    arm.useWorldSpace = false;
                    arm.positionCount = 2;
                    arm.SetPosition(0, blade == 0 ? Vector3.left * 0.55f : Vector3.down * 0.55f);
                    arm.SetPosition(1, blade == 0 ? Vector3.right * 0.55f : Vector3.up * 0.55f);
                    arm.numCapVertices = 3;
                    arm.sharedMaterial = trailMaterial;
                    arm.sortingLayerName = layer;
                    arm.sortingOrder = order + 12;
                    arm.startColor = arm.endColor = new Color(1f, 0.92f, 0.55f, 0.65f);
                }

                go.SetActive(false);
                pool[i] = new Flyer { go = go, Root = go.transform, art = artObject.transform, sprite = sr, trail = trail, core = core, rotor = rotor };
            }
            return pool;
        }

        Sprite SpriteFor(Match3Power power, out bool fallback)
        {
            var s = S;
            Sprite sprite = power == Match3Power.Tnt ? s.tntSprite
                : power == Match3Power.Helicopter ? s.helicopterSprite
                : power == Match3Power.RocketHorizontal || power == Match3Power.RocketVertical ? s.rocketSprite : null;
            fallback = !sprite;
            return sprite ? sprite : Match3Shapes.GemSprite;
        }

        // ------------------------------------------------------------------ projectiles

        public Flyer TakeFlyer(Match3Power power, Vector3 position, float scale)
        {
            float cell = rig.Cell;
            foreach (var f in flyers)
            {
                if (f.used) continue;
                f.used = true;
                f.rocket = false;
                f.generation++;

                var sprite = SpriteFor(power, out bool fallback);
                f.sprite.sprite = sprite;
                f.sprite.color = fallback ? new Color(1f, 0.82f, 0.36f) : Color.white;
                f.sprite.enabled = true;
                Vector3 bounds = sprite.bounds.size;
                f.artFit = cell * scale / Mathf.Max(0.0001f, Mathf.Max(bounds.x, bounds.y));
                f.art.localScale = Vector3.one * f.artFit;
                f.art.localRotation = Quaternion.identity;

                bool helicopter = power == Match3Power.Helicopter;
                f.rotor.gameObject.SetActive(helicopter);
                f.rotor.localScale = Vector3.one * cell * scale;
                f.rotor.localPosition = new Vector3(0f, 0.23f * cell * scale, -0.04f);
                foreach (var arm in f.rotor.GetComponentsInChildren<LineRenderer>(true)) arm.widthMultiplier = 0.045f * cell;

                f.Root.SetPositionAndRotation(position + Vector3.back * 0.6f, Quaternion.identity);
                f.trail.time = 0.16f;
                f.trail.widthMultiplier = 0.3f * cell;
                f.trail.startColor = new Color(1f, 0.95f, 0.6f, 1f);
                f.trail.endColor = new Color(1f, 0.45f, 0.08f, 0f);
                f.core.emitting = false;
                f.go.SetActive(true);
                f.trail.Clear();
                f.core.Clear();
                // Rockets start their trails at launch; the TNT fuse has none.
                f.trail.emitting = helicopter;
                PeakProjectiles = Mathf.Max(PeakProjectiles, ActiveProjectiles);
                return f;
            }
            return null;
        }

        /// <summary>Hides the art now and returns the projectile to the pool once its trail has faded.</summary>
        public void Retire(Flyer f)
        {
            if (f == null || !f.used) return;
            f.sprite.enabled = false;
            f.rotor.gameObject.SetActive(false);
            f.trail.emitting = false;
            f.core.emitting = false;
            int generation = f.generation;
            rig.Schedule(f.rocket ? 0.36f : 0.17f, () =>
            {
                if (f.generation != generation || !f.used) return;
                Release(f);
            });
        }

        static void Release(Flyer f)
        {
            f.trail.Clear();
            f.core.Clear();
            f.go.SetActive(false);
            f.used = false;
            f.rocket = false;
        }

        // ------------------------------------------------------------------ effects

        float Range(float min, float max) => min + (float)random.NextDouble() * (max - min);

        void Emit(ParticleSystem ps, Vector3 p, int n, float size, float speed, float life, Color tint)
        {
            if (!ps || n <= 0) return;
            float cell = rig.Cell;
            if (rig.ReducedMotion) n = Mathf.Max(1, n / 3);
            var e = new ParticleSystem.EmitParams
            {
                position = p + Vector3.back * 0.5f,
                startLifetime = life,
                startSize = size * cell,
                startColor = tint,
                applyShapeToPosition = false
            };
            for (int i = 0; i < n; i++)
            {
                float angle = i * Mathf.PI * 2f / n + Range(-0.25f, 0.25f);
                e.velocity = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * speed * cell * Range(0.65f, 1.2f);
                e.rotation = Range(0f, 360f);
                ps.Emit(e, 1);
            }
        }

        void PuffRing(Vector3 p, float strength)
        {
            float cell = rig.Cell;
            int count = rig.ReducedMotion ? 4 : 8;
            for (int i = 0; i < count; i++)
            {
                float a = (i + 0.15f) * Mathf.PI * 2f / count;
                var dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                smoke.Emit(new ParticleSystem.EmitParams
                {
                    position = p + dir * cell * 0.4f * strength + Vector3.back * 0.5f,
                    velocity = dir * cell * 4.5f * strength,
                    startLifetime = S.smokeLifetime * Range(0.85f, 1.1f),
                    startSize = cell * 0.87f * strength * Range(0.85f, 1.1f),
                    startColor = Color.white,
                    rotation = -a * Mathf.Rad2Deg,
                    applyShapeToPosition = false
                }, 1);
            }
        }

        /// <summary>Staged blast: warm core, expanding shockwave, smoke puffs, rays, sparks, embers, late corona.</summary>
        public void Blast(Vector3 p, float strength)
        {
            strength = Mathf.Clamp(strength, 0.35f, 1.45f);
            Blasts++;
            if (strength >= 0.9f && rig.Impacts.TryPlayBlast(p, strength))
            {
                Match3Vfx.RaiseReaction(strength);
                rig.Shake.Add(strength);
                return;
            }
            bool motion = !rig.ReducedMotion;
            // Closely chained blasts share one bright core beat.
            if (Time.time - lastCoreFlash > 0.11f)
            {
                Emit(impact, p, 1, (motion ? 4.3f : 2.5f) * strength, 0f, 0.10f, new Color(1f, 0.96f, 0.74f, motion ? 0.92f : 0.38f));
                lastCoreFlash = Time.time;
            }
            Emit(shockwave, p, 1, 2.1f * strength, 0f, 0.34f, new Color(1f, 0.88f, 0.43f, 0.9f));
            PuffRing(p, strength);
            Emit(rays, p, 22, 0.19f, 10f * strength, 0.31f, new Color(1f, 0.94f, 0.69f, 1f));
            Emit(sparkles, p, 24, 0.27f, 6.2f * strength, 0.58f, new Color(1f, 0.96f, 0.73f, 1f));
            Emit(embers, p, 20, 0.12f, 7f * strength, 0.61f, new Color(1f, 0.7f, 0.2f, 1f));
            rig.Schedule(0.075f, () =>
            {
                Emit(shockwave, p, 1, 1.45f * strength, 0f, 0.33f, new Color(1f, 0.72f, 0.24f, 0.5f));
                Emit(sparkles, p, 14, 0.17f, 4.6f * strength, 0.66f, new Color(1f, 0.93f, 0.67f, 1f));
            });
            Match3Vfx.RaiseReaction(strength);
            rig.Shake.Add(strength);
        }

        public void Firework(Vector3 p, float intensity)
        {
            int count = Mathf.RoundToInt(Mathf.Lerp(9f, 19f, Mathf.InverseLerp(1f, S.intensityMax, intensity)));
            Emit(sparkles, p, count, 0.18f, 3.3f, 0.44f, new Color(1f, 0.94f, 0.7f, 0.95f));
            Emit(embers, p, count / 2, 0.085f, 3.8f, 0.55f, new Color(1f, 0.75f, 0.27f, 1f));
            MatchAccentSound(intensity);
        }

        public void MatchAccentSound(float intensity)
        {
            if (intensity < S.accentSoundIntensity || Time.time - lastAccentSound <= 0.35f) return;
            if (rig.Sfx.Play(Match3SfxCue.Firework)) lastAccentSound = Time.time;
        }

        public void Creation(Vector3 p)
        {
            Emit(shockwave, p, 1, 0.6f, 0f, 0.2f, new Color(0.75f, 0.9f, 1f, 1f));
            Emit(sparkles, p, 10, 0.33f, 3f, 0.34f, new Color(1f, 0.9f, 0.3f, 1f));
            rig.Sfx.Play(Match3SfxCue.Create);
            Match3Vfx.RaiseReaction(0.6f);
        }

        public void Chip(Vector3 p)
        {
            Emit(smoke, p, 3, 0.43f, 1.8f, 0.22f, new Color(1f, 0.86f, 0.65f, 0.75f));
            Emit(embers, p, 6, 0.13f, 3f, 0.3f, new Color(1f, 0.73f, 0.2f, 1f));
        }

        public void RocketImpact(Vector3 p, bool end)
        {
            Emit(rocketRing, p, 1, end ? 0.85f : 0.62f, 0f, 0.23f, Color.white);
            Emit(rocketGlow, p, 1, end ? 1.1f : 0.78f, 0f, 0.18f, new Color(1f, 0.87f, 0.48f, 0.72f));
            Emit(rocketStars, p, end ? 12 : 7, 0.16f, 1.6f, 0.34f, new Color(1f, 0.96f, 0.76f, 1f));
        }

        public void TrailSpark(Vector3 p) => Emit(rocketStars, p, 3, 0.11f, 0.65f, 0.34f, new Color(1f, 0.95f, 0.7f, 0.9f));

        public void OrbSpark(Vector3 p) => Emit(sparkles, p, 5, 0.35f, 2f, 0.3f, new Color(0.8f, 0.9f, 1f, 1f));

        // ------------------------------------------------------------------ lifecycle

        public void Tick(float dt)
        {
            foreach (var f in flyers)
                if (f.used && f.rotor.gameObject.activeSelf) f.rotor.Rotate(0f, 0f, dt * 2400f);
        }

        public void SetPaused(bool paused)
        {
            foreach (var ps in systems)
            {
                if (paused && !ps.isPaused) ps.Pause();
                else if (!paused && ps.isPaused) ps.Play();
            }
        }

        public void Clear()
        {
            foreach (var f in flyers)
            {
                f.generation++;
                if (f.used || f.go.activeSelf) Release(f);
                f.sprite.enabled = true;
            }
            foreach (var ps in systems)
            {
                ps.Clear();
                if (!ps.isPlaying) ps.Play();
            }
        }

        public void Dispose()
        {
            foreach (var m in materials) if (m) Object.Destroy(m);
            materials.Clear();
        }
    }
}
