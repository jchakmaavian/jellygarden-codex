using UnityEngine;

namespace JellyGarden.Scripts.Juice
{
    /// <summary>
    /// Central, pooled VFX service for match feedback.
    ///
    /// Design notes:
    ///  - Four shared ParticleSystems (stars / micro sparkles / coloured fragments /
    ///    centre stars) simulate in WORLD space. To play a burst we move the system's
    ///    transform to the hit position, set the tint, and call Emit(). Nothing is
    ///    instantiated or destroyed per match, so a match costs no GC allocations
    ///    and adds no draw calls beyond the four persistent systems.
    ///  - The impact flash, the fruit shine and the centre ring are flat sprites taken
    ///    from a small fixed-size pool and advanced by one Update loop (no per-effect
    ///    coroutines).
    ///  - Nothing here touches gameplay state, the Canvas, or any existing GameObject.
    ///    The service builds its own hidden root object on first use.
    /// </summary>
    public class MatchVFXService : MonoBehaviour
    {
        private static MatchVFXService _instance;
        private static bool _quitting;

        /// <summary>
        /// Statics survive a play-mode stop when "Reload Domain" is disabled, so
        /// _quitting would stay true and permanently return a null service for the
        /// rest of the Editor session. Reset explicitly on every play start.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _quitting = false;
            _instance = null;
        }

        public static MatchVFXService Instance
        {
            get
            {
                if (_quitting) return null;
                if (_instance == null)
                {
                    var go = new GameObject("~MatchVFXService");
                    _instance = go.AddComponent<MatchVFXService>();
                    DontDestroyOnLoad(go);
                    _instance.Build();
                }
                return _instance;
            }
        }

        /// <summary>
        /// Builds the pools and generates the textures during scene load. Previously
        /// this happened lazily inside the first match, producing a multi-frame hitch
        /// at precisely the moment the feedback was supposed to feel good.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Prewarm()
        {
            var warm = Instance;
            if (warm != null) warm.enabled = true;
        }

        // ------------------------------------------------------------------
        // Each destroyed fruit consumes two overlays (impact flash + fruit shine) and
        // each match group one more for the centre ring, so a booster clear can ask for
        // dozens at once. Sized so Take() does not silently drop flashes on the biggest,
        // most rewarding clears.
        private const int OverlayPoolSize = 96;

        // Three staggered layers per destroyed fruit.
        private const int PendingPoolSize = 128;

        private enum OverlayMode { Flash, Ring, Shine }

        private class Overlay
        {
            public SpriteRenderer Renderer;
            public Transform Tr;
            public bool Active;
            public float Time;
            public float Duration;
            public float StartScale;
            public float EndScale;
            public Color Tint;
            public OverlayMode Mode;
            /// <summary>Seconds still to wait before this overlay becomes visible.</summary>
            public float Delay;
        }

        private ParticleSystem _stars;
        private ParticleSystem _micro;
        private ParticleSystem _fragments;
        private ParticleSystem _centerStars;

        private Overlay[] _overlays;
        private int _overlayCursor;

        /// <summary>A queued particle emission, used to stagger the burst layers.</summary>
        private class Pending
        {
            public ParticleSystem Ps;
            public Vector3 Pos;
            public Color Tint;
            public float Speed;
            public float Lifetime;
            public float Size;
            public float Remaining;
            public int Count;
            public bool Active;
        }

        private Pending[] _pending;
        private int _pendingCursor;

        private Sprite _glowSprite;
        private Sprite _ringSprite;
        private Material _additiveMat;   // particles: vertex colour only
        private Material _overlayMat;    // sprites: also multiplies SpriteRenderer.color

        private float _cellSize = -1f;

        // --- hit-stop ------------------------------------------------------
        // A micro-freeze on impact. Cheaper and more convincing than any amount of
        // extra animation, and it is what separates a good match-3 pop from a
        // premium one. Driven on UNSCALED time so it can restore itself.
        private bool _hitStopActive;
        private float _hitStopRemaining;
        private float _hitStopRestore = 1f;
        private float _hitStopApplied;      // what we set timeScale to
        private float _hitStopPendingDelay;
        private float _hitStopPendingDuration;
        private float _hitStopPendingScale = 1f;
        private bool _hitStopQueued;

        private JuiceConfig Cfg { get { return JuiceConfig.Instance; } }

        // ------------------------------------------------------------------
        private void OnApplicationQuit()
        {
            _quitting = true;
        }

        private void Build()
        {
            var cfg = Cfg;

            // Load through a Resources material asset, NOT Shader.Find. A shader that no
            // asset references is stripped from player builds, so Shader.Find would return
            // null in a build and silently drop the whole layer to alpha-blended
            // Sprites/Default. A material under Resources/ is always included.
            Material baseMat = Resources.Load<Material>("JuiceAdditive");
            if (baseMat != null && baseMat.shader != null)
            {
                _additiveMat = new Material(baseMat) { hideFlags = HideFlags.HideAndDontSave };
            }
            else
            {
                var shader = Shader.Find("JellyGarden/JuiceAdditive") ?? Shader.Find("Sprites/Default");
                _additiveMat = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                Debug.LogWarning("[MatchVFX] Resources/JuiceAdditive.mat missing; fell back to Shader.Find. " +
                                 "This will not survive shader stripping in a player build.");
            }

            // Particles carry their colour in the vertex stream; SpriteRenderers deliver
            // SpriteRenderer.color through the _RendererColor uniform instead. That uniform
            // lives in the UnityPerDrawSprite constant buffer, which Unity populates ONLY
            // for sprite draws - reading it on a particle draw multiplies the colour by an
            // unset value and the particles disappear. Keep the keyword off on the shared
            // particle material and on for the sprite-overlay material.
            _additiveMat.DisableKeyword("SPRITE_RENDERER_COLOR");
            _additiveMat.SetFloat("_UseRendererColor", 0f);

            _overlayMat = new Material(_additiveMat) { hideFlags = HideFlags.HideAndDontSave };
            _overlayMat.EnableKeyword("SPRITE_RENDERER_COLOR");
            _overlayMat.SetFloat("_UseRendererColor", 1f);

            // Push the additive output above 1.0 so URP bloom picks up ONLY the sparkles.
            // Harmless on Built-In, where values simply clamp.
            _additiveMat.SetFloat("_Boost", cfg.VfxHdrBoost);
            _overlayMat.SetFloat("_Boost", cfg.VfxHdrBoost);

            _glowSprite = MakeSprite(SparkleTextureFactory.SoftGlow);
            _ringSprite = MakeSprite(SparkleTextureFactory.Ring);

            _stars = BuildSystem("Stars", SparkleTextureFactory.FourPointSparkle, 320, 0f);
            _micro = BuildSystem("Micro", SparkleTextureFactory.Diamond, 420, 0f);
            _fragments = BuildSystem("Fragments", SparkleTextureFactory.SoftGlow, 260, 0.9f);
            _centerStars = BuildSystem("CenterStars", SparkleTextureFactory.SixPointStar, 160, 0f);

            _pending = new Pending[PendingPoolSize];
            for (int i = 0; i < PendingPoolSize; i++) _pending[i] = new Pending();

            UrpBloomSetup.Apply(cfg);

            _overlays = new Overlay[OverlayPoolSize];
            for (int i = 0; i < OverlayPoolSize; i++)
            {
                var go = new GameObject("Overlay" + i);
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sharedMaterial = _overlayMat;
                sr.sortingLayerName = cfg.VfxSortingLayer;
                sr.sortingOrder = cfg.VfxSortingOrder;
                sr.enabled = false;
                _overlays[i] = new Overlay { Renderer = sr, Tr = go.transform };
            }
        }

        private static Sprite MakeSprite(Texture2D tex)
        {
            // pixelsPerUnit == texture width means scale 1 == 1 world unit.
            var s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), tex.width, 0, SpriteMeshType.FullRect);
            s.hideFlags = HideFlags.HideAndDontSave;
            return s;
        }

        private ParticleSystem BuildSystem(string label, Texture2D tex, int maxParticles, float gravity)
        {
            var cfg = Cfg;

            var go = new GameObject("PS_" + label);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();

            // A freshly added ParticleSystem auto-plays and emits; shut that off first.
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = maxParticles;
            main.gravityModifier = gravity;
            main.startLifetime = 0.3f;
            main.startSpeed = 3f;
            main.startSize = 0.3f;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = Color.white;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;   // we drive everything through Emit()

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.04f;
            shape.radiusThickness = 1f;
            shape.arc = 360f;
            shape.arcMode = ParticleSystemShapeMultiModeValue.Random;
            shape.alignToDirection = false;

            // Fast launch that bleeds off - the "decelerate naturally" requirement.
            // Uses drag rather than dampen: dampen is applied once per RENDERED FRAME,
            // so it makes the burst radius frame-rate dependent (a 30fps device would
            // get a visibly wider burst than a 60fps one). drag is time-correct.
            var limit = ps.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.separateAxes = false;
            limit.limit = new ParticleSystem.MinMaxCurve(0.9f);
            limit.dampen = 0f;
            limit.drag = new ParticleSystem.MinMaxCurve(Cfg.ParticleDrag);
            limit.multiplyDragByParticleSize = false;
            limit.multiplyDragByParticleVelocity = true;

            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            var sizeCurve = new AnimationCurve();
            // Born near full size so the particle is legible on frame one, a brief
            // overshoot for the pop, then holds at 0.30 and lets the alpha ramp do the
            // dying. The old 0.55 -> 1.0 -> 0.05 curve halved the effective size.
            sizeCurve.AddKey(new Keyframe(0f, 0.85f));
            sizeCurve.AddKey(new Keyframe(0.12f, 1.12f));
            sizeCurve.AddKey(new Keyframe(1f, 0.3f));
            sol.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 0.55f),
                    new GradientAlphaKey(0f, 1f)
                });
            col.color = new ParticleSystem.MinMaxGradient(grad);

            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-3.5f, 3.5f);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingLayerName = cfg.VfxSortingLayer;
            renderer.sortingOrder = cfg.VfxSortingOrder;
            renderer.alignment = ParticleSystemRenderSpace.View;

            var mat = new Material(_additiveMat) { hideFlags = HideFlags.HideAndDontSave };
            mat.mainTexture = tex;
            renderer.sharedMaterial = mat;

            ps.Play();   // running but emitting nothing until Emit() is called
            return ps;
        }

        // ------------------------------------------------------------------
        /// <summary>
        /// World-space size of one board cell, used so the effect scales with the
        /// board instead of being hard-coded. Measured once from a live item.
        /// </summary>
        private float CellSize()
        {
            if (_cellSize > 0f) return _cellSize;

            // Measure the spacing between two adjacent board squares. Squares are laid
            // out once and never animated, so this is the true cell pitch. Deliberately
            // NOT measured from an Item: an item's localScale carries the board scale but
            // is also continuously animated by the idle breathing and the destroy pop, so
            // sampling it would cache a value that is a few percent wrong - and its parent
            // is unscaled, so that is not a substitute either.
            var squares = FindObjectsByType<Square>(FindObjectsSortMode.None);
            if (squares != null && squares.Length >= 2)
            {
                Vector3 p0 = squares[0].transform.position;
                float best = float.MaxValue;
                for (int i = 1; i < squares.Length; i++)
                {
                    float d = Vector2.Distance(p0, squares[i].transform.position);
                    if (d > 0.01f && d < best) best = d;
                }
                if (best < float.MaxValue)
                {
                    _cellSize = best;
                    return _cellSize;
                }
            }

            // Fallback: the item's own world size, which includes the board scale.
            var item = FindAnyObjectByType<Item>();
            if (item != null)
            {
                var box = item.GetComponent<BoxCollider2D>();
                if (box != null)
                {
                    float s = box.size.x * Mathf.Abs(item.transform.lossyScale.x);
                    if (s > 0.01f)
                    {
                        _cellSize = s;
                        return _cellSize;
                    }
                }
            }
            return 1f;   // sensible default; not cached, so a later call can still measure
        }

        // ------------------------------------------------------------------
        /// <summary>
        /// The full layered burst for one destroyed fruit: impact flash, primary
        /// star burst, micro sparkles and a small amount of fruit-coloured debris.
        /// </summary>
        /// <param name="delay">
        /// Seconds to wait before the burst starts. Callers schedule the burst UP FRONT
        /// with the delay rather than calling this at the right moment, so the effect
        /// survives the requesting object being destroyed or deactivated in the meantime
        /// (LevelManager.AnimateCollectedItem deactivates collected fruits mid-destroy).
        /// </param>
        public void PlayMatchBurst(Vector3 position, Color fruitColor, float intensity, float delay = 0f)
        {
            var cfg = Cfg;
            if (!cfg.EnableJuice) return;

            float cell = CellSize();
            position.z -= 0.5f;   // sit in front of the fruits

            // Intensity drives COUNT and velocity at full strength, but only a third of
            // it reaches SIZE. Scaling size linearly would make a max-intensity cascade
            // throw single elements ~2.7 cells wide that swallow the neighbouring fruits.
            float sizeIntensity = Mathf.Lerp(1f, intensity, 0.35f);

            // Layer 1 - impact flash.
            var flash = Take();
            if (flash != null)
            {
                flash.Mode = OverlayMode.Flash;
                flash.Renderer.sprite = _glowSprite;
                flash.Tr.position = position;
                flash.Tr.localRotation = Quaternion.identity;
                flash.Duration = cfg.FlashDuration;
                flash.Time = 0f;
                flash.StartScale = cfg.FlashStartScale * cell * sizeIntensity;
                flash.EndScale = cfg.FlashEndScale * cell * sizeIntensity;
                flash.Tint = Color.Lerp(Color.white, fruitColor, 0.25f);
                flash.Tint.a = 1f;
                flash.Delay = delay;
                flash.Active = true;
                flash.Renderer.enabled = delay <= 0f;
            }

            // Layers 2-4 are scheduled a few milliseconds apart so the burst has
            // temporal layering instead of firing as one flat simultaneous event.
            // The spec asks for stars at T=90-160ms and micro sparkles at T=110-180ms,
            // measured from match confirmation; the caller invokes this at ~T=85ms.

            // Layer 2 - primary stars. Mostly white/gold so every match reads alike.
            Schedule(_stars, position, delay + cfg.StarDelay,
                Mathf.Clamp(Mathf.RoundToInt(cfg.StarParticleCount * intensity), 6, 12),
                Color.Lerp(Color.white, fruitColor, 0.18f),
                cfg.ParticleVelocity * cell,
                cfg.ParticleLifetime,
                cfg.StarSize * cell * sizeIntensity);

            // Layer 3 - micro sparkles: faster, smaller, shorter.
            Schedule(_micro, position, delay + cfg.MicroDelay,
                Mathf.Clamp(Mathf.RoundToInt(cfg.MicroParticleCount * intensity), 8, 20),
                Color.Lerp(Color.white, fruitColor, 0.1f),
                cfg.ParticleVelocity * 1.35f * cell,
                cfg.MicroLifetime,
                cfg.MicroSize * cell * sizeIntensity);

            // Layer 4 - a restrained amount of fruit-coloured debris.
            Schedule(_fragments, position, delay + cfg.FragmentDelay,
                Mathf.RoundToInt(cfg.FragmentCount * intensity),
                Color.Lerp(Color.white, fruitColor, cfg.FragmentColorBlend),
                cfg.ParticleVelocity * 0.72f * cell,
                cfg.FragmentLifetime,
                cfg.FragmentSize * cell * sizeIntensity);
        }

        /// <summary>
        /// One shared accent at the centre of a match group so the individual pops
        /// read as a single event. Deliberately small.
        /// </summary>
        public void PlayMatchCenter(Vector3 position, Color fruitColor, float intensity)
        {
            var cfg = Cfg;
            if (!cfg.EnableJuice || !cfg.EnableCenterEmphasis) return;

            float cell = CellSize();
            position.z -= 0.55f;
            float sizeIntensity = Mathf.Lerp(1f, intensity, 0.35f);

            var ring = Take();
            if (ring != null)
            {
                ring.Mode = OverlayMode.Ring;
                ring.Renderer.sprite = _ringSprite;
                ring.Tr.position = position;
                ring.Tr.localRotation = Quaternion.identity;
                ring.Duration = cfg.CenterRingDuration;
                ring.Time = 0f;
                ring.StartScale = 0.25f * cell;
                ring.EndScale = cfg.CenterRingEndScale * cell * sizeIntensity;
                ring.Tint = Color.Lerp(Color.white, fruitColor, 0.35f);
                ring.Tint.a = 1f;
                ring.Delay = 0f;   // pooled: never inherit a previous entry's delay
                ring.Active = true;
                ring.Renderer.enabled = true;
            }

            EmitBurst(_centerStars, position,
                Mathf.RoundToInt(cfg.CenterStarCount * intensity),
                Color.Lerp(Color.white, fruitColor, 0.2f),
                cfg.ParticleVelocity * 0.85f * cell,
                cfg.ParticleLifetime * 1.15f,
                cfg.StarSize * 1.25f * cell * sizeIntensity);
        }

        /// <summary>
        /// Very short additive re-draw of the fruit's own sprite, giving a bright
        /// highlight just before it pops. The transform is snapshotted so the
        /// overlay stays valid after the fruit object goes away.
        /// </summary>
        public void PlayFruitShine(SpriteRenderer source, float duration, float strength, float delay = 0f)
        {
            var cfg = Cfg;
            if (!cfg.EnableJuice || source == null || source.sprite == null) return;

            var o = Take();
            if (o == null) return;

            o.Delay = delay;

            o.Mode = OverlayMode.Shine;
            o.Renderer.sprite = source.sprite;
            var st = source.transform;
            o.Tr.position = st.position + new Vector3(0f, 0f, -0.4f);
            o.Tr.rotation = st.rotation;
            o.Tr.localScale = st.lossyScale;
            o.Duration = duration;
            o.Time = 0f;
            o.StartScale = 1f;   // unused for Shine - scale is fixed to the snapshot
            o.EndScale = 1f;
            o.Tint = new Color(1f, 1f, 1f, strength);
            o.Active = true;
            o.Renderer.enabled = delay <= 0f;
        }

        // ------------------------------------------------------------------
        /// <summary>
        /// Queues an emission for <paramref name="delay"/> seconds from now. A delay of
        /// zero or less emits immediately. Backed by a fixed-size pool so staggering the
        /// layers costs no coroutines and no allocation.
        /// </summary>
        private void Schedule(ParticleSystem ps, Vector3 pos, float delay, int count,
                              Color tint, float speed, float lifetime, float size)
        {
            if (count <= 0) return;

            if (delay <= 0f)
            {
                EmitBurst(ps, pos, count, tint, speed, lifetime, size);
                return;
            }

            for (int i = 0; i < _pending.Length; i++)
            {
                int idx = (_pendingCursor + i) % _pending.Length;
                var p = _pending[idx];
                if (p.Active) continue;

                p.Ps = ps;
                p.Pos = pos;
                p.Count = count;
                p.Tint = tint;
                p.Speed = speed;
                p.Lifetime = lifetime;
                p.Size = size;
                p.Remaining = delay;
                p.Active = true;
                _pendingCursor = (idx + 1) % _pending.Length;
                return;
            }

            // Queue full (a very large simultaneous clear): emit now rather than drop it.
            EmitBurst(ps, pos, count, tint, speed, lifetime, size);
        }

        private void EmitBurst(ParticleSystem ps, Vector3 pos, int count, Color tint,
                               float speed, float lifetime, float size)
        {
            if (ps == null || count <= 0) return;

            ps.transform.position = pos;

            var main = ps.main;
            main.startColor = tint;
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.55f, speed * 1.15f);
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.7f, lifetime * 1.15f);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size * 1.3f);

            ps.Emit(count);
        }

        private Overlay Take()
        {
            for (int i = 0; i < _overlays.Length; i++)
            {
                int idx = (_overlayCursor + i) % _overlays.Length;
                if (!_overlays[idx].Active)
                {
                    _overlayCursor = (idx + 1) % _overlays.Length;
                    return _overlays[idx];
                }
            }
            return null;   // pool exhausted: drop the effect rather than allocate
        }

        private void Update()
        {
            if (_overlays == null) return;

            // Hit-stop runs on unscaled time - it is what MOVES timeScale, so it can
            // never depend on it.
            TickHitStop(Time.unscaledDeltaTime);

            float dt = Time.deltaTime;

            // Release any layer emissions whose stagger delay has elapsed.
            if (_pending != null)
            {
                for (int i = 0; i < _pending.Length; i++)
                {
                    var p = _pending[i];
                    if (!p.Active) continue;
                    p.Remaining -= dt;
                    if (p.Remaining > 0f) continue;
                    p.Active = false;
                    EmitBurst(p.Ps, p.Pos, p.Count, p.Tint, p.Speed, p.Lifetime, p.Size);
                    p.Ps = null;   // drop the reference so a dead system can be collected
                }
            }

            for (int i = 0; i < _overlays.Length; i++)
            {
                var o = _overlays[i];
                if (!o.Active) continue;

                if (o.Delay > 0f)
                {
                    o.Delay -= dt;
                    if (o.Delay > 0f) continue;
                    o.Renderer.enabled = true;   // held hidden until its beat arrives
                }

                o.Time += dt;
                float t = o.Duration > 0f ? Mathf.Clamp01(o.Time / o.Duration) : 1f;

                switch (o.Mode)
                {
                    case OverlayMode.Flash:
                    {
                        // Snap out fast, fade faster - should read as an instant pop.
                        float s = Mathf.Lerp(o.StartScale, o.EndScale, Easing.OutCubic(t));
                        o.Tr.localScale = new Vector3(s, s, 1f);
                        var c = o.Tint;
                        c.a = 1f - Easing.InQuad(t);
                        o.Renderer.color = c;
                        break;
                    }
                    case OverlayMode.Ring:
                    {
                        float s = Mathf.Lerp(o.StartScale, o.EndScale, Easing.OutCubic(t));
                        o.Tr.localScale = new Vector3(s, s, 1f);
                        var c = o.Tint;
                        // Hold bright, then drop late - the same shape as Flash. The old
                        // OutQuad fade was half gone 70ms into a 240ms life, which made
                        // the shared centre accent effectively invisible.
                        c.a = (1f - Easing.InQuad(t)) * 0.9f;
                        o.Renderer.color = c;
                        break;
                    }
                    case OverlayMode.Shine:
                    {
                        var c = o.Tint;
                        // Hold-then-fall so the highlight beat actually lands instead of
                        // being gone within two frames.
                        c.a = o.Tint.a * (1f - Easing.InQuad(t));
                        o.Renderer.color = c;
                        break;
                    }
                }

                if (t >= 1f)
                {
                    o.Active = false;
                    o.Renderer.enabled = false;
                }
            }
        }

        // ------------------------------------------------------------------
        /// <summary>
        /// Requests a micro-freeze <paramref name="delay"/> seconds from now, landing on
        /// the pop. Fired ONCE PER MATCH GROUP, never per fruit - a hit-stop per fruit in
        /// a long cascade would read as lag rather than impact.
        /// </summary>
        public void RequestHitStop(float delay, float duration, float scale)
        {
            var cfg = Cfg;
            if (!cfg.EnableJuice || !cfg.EnableHitStop) return;
            if (duration <= 0f) return;

            // Coalesce rather than stack: overlapping requests take the longer one.
            if (_hitStopQueued)
            {
                _hitStopPendingDelay = Mathf.Min(_hitStopPendingDelay, delay);
                _hitStopPendingDuration = Mathf.Max(_hitStopPendingDuration, duration);
                _hitStopPendingScale = Mathf.Min(_hitStopPendingScale, scale);
                return;
            }

            _hitStopQueued = true;
            _hitStopPendingDelay = delay;
            _hitStopPendingDuration = duration;
            _hitStopPendingScale = Mathf.Clamp(scale, 0.01f, 1f);
        }

        private void TickHitStop(float unscaledDt)
        {
            if (_hitStopQueued && !_hitStopActive)
            {
                _hitStopPendingDelay -= unscaledDt;
                if (_hitStopPendingDelay <= 0f)
                {
                    _hitStopQueued = false;
                    // Only engage from normal play. If something else already owns
                    // timeScale (pause menu at 0, or a debug slow-mo), leave it alone.
                    if (Time.timeScale > 0.5f)
                    {
                        _hitStopRestore = Time.timeScale;
                        _hitStopApplied = _hitStopRestore * _hitStopPendingScale;
                        Time.timeScale = _hitStopApplied;
                        _hitStopRemaining = _hitStopPendingDuration;
                        _hitStopActive = true;
                    }
                }
            }
            else if (_hitStopActive)
            {
                // If anything else changed timeScale meanwhile (e.g. the pause menu),
                // surrender ownership instead of stomping it on restore.
                if (!Mathf.Approximately(Time.timeScale, _hitStopApplied))
                {
                    _hitStopActive = false;
                    return;
                }

                _hitStopRemaining -= unscaledDt;
                if (_hitStopRemaining <= 0f)
                {
                    Time.timeScale = _hitStopRestore;
                    _hitStopActive = false;
                }
            }
        }

        /// <summary>
        /// Emphasis multiplier for a match: bigger groups and deeper cascades read
        /// slightly stronger, but always inside a hard ceiling so a long chain never
        /// floods the board.
        /// </summary>
        public static float ComputeIntensity(int matchSize, int cascadeStep)
        {
            var cfg = JuiceConfig.Instance;
            float intensity = 1f;

            if (matchSize >= 5) intensity += cfg.FiveMatchBonus;
            else if (matchSize == 4) intensity += cfg.FourMatchBonus;

            if (cascadeStep > 1)
                intensity += cfg.CascadeIntensityMultiplier * (cascadeStep - 1);

            return Mathf.Clamp(intensity, 0.5f, cfg.CascadeIntensityMax);
        }
    }
}
