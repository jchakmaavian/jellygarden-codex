using System.Collections.Generic;
using UnityEngine;

namespace JellyGarden.Scripts.Juice
{
    /// <summary>
    /// Board Rocket presentation: an energetic Reveal when Match-4 creates it, then a calm Idle loop.
    /// It poses only the Item's sprite child after RoyalPieceSkin fits it, so the Item transform,
    /// collider and gameplay state are untouched. On activation RoyalPowerupDirector copies the
    /// last pose into a launch stand-in (see RoyalRocketLaunch) before the directional flyers take over.
    /// </summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(1000)]
    public sealed class RoyalRocketPieceAnimator : MonoBehaviour
    {
        public enum Phase { Reveal, Idle }

        public const float RevealDuration = .48f;
        public const float IdlePeriod = 2.2f;
        // Idle choreography inside each loop, in seconds: a sheen crosses the body, then the nose it leaves glints.
        const float SheenStart = .62f, SheenLife = .48f, SparkleStart = 1.02f, SparkleLife = .36f;

        // Reveal, in seconds: pops in small and stretched, flicks over, overshoots, rebounds, settles.
        static readonly AnimationCurve revealScale = new AnimationCurve(
            new Keyframe(0, .30f, 0, 11f), new Keyframe(.13f, 1.15f), new Keyframe(.25f, .93f), new Keyframe(.36f, 1.025f), new Keyframe(RevealDuration, 1));
        static readonly AnimationCurve revealAxis = new AnimationCurve(
            new Keyframe(0, 1), new Keyframe(.05f, 1.20f), new Keyframe(.12f, .88f), new Keyframe(.21f, 1.06f), new Keyframe(.32f, .985f), new Keyframe(RevealDuration, 1));
        static readonly AnimationCurve revealCross = new AnimationCurve(
            new Keyframe(0, 1), new Keyframe(.05f, .82f), new Keyframe(.12f, 1.12f), new Keyframe(.21f, .96f), new Keyframe(.32f, 1.012f), new Keyframe(RevealDuration, 1));
        static readonly AnimationCurve revealTurn = new AnimationCurve(
            new Keyframe(0, -16), new Keyframe(.11f, 8), new Keyframe(.21f, -3.5f), new Keyframe(.32f, 1.2f), new Keyframe(RevealDuration, 0));

        static readonly List<RoyalRocketPieceAnimator> live = new List<RoyalRocketPieceAnimator>();
        static Material overlayMaterial;
        static Sprite sparkleSprite, sheenSprite;

        public Phase Current { get; private set; }
        public float Age => age;
        /// <summary>Additive flash over the whole Rocket (0-1): bright during the Reveal pop, copied by the launch stand-in.</summary>
        public float Highlight { get; private set; }
        /// <summary>Multipliers applied on top of the fitted sprite this frame, for tests and the launch hand-off.</summary>
        public float AxisScale { get; private set; } = 1;
        public float CrossScale { get; private set; } = 1;
        public float Turn { get; private set; }
        public float SheenStrength { get; private set; }
        public float SparkleStrength { get; private set; }
        public SpriteRenderer HighlightRenderer => highlight;

        Item item;
        SpriteRenderer highlight, sheen, sparkle;
        SpriteMask sheenMask;
        float age;
        int startFrame = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { live.Clear(); }

        static bool IsRocket(Item item) => item.currentType == ItemsTypes.HORIZONTAL_STRIPPED || item.currentType == ItemsTypes.VERTICAL_STRIPPED;

        /// <summary>Adds the animator to a board Rocket. A Rocket without a reveal starts in Idle at rest.</summary>
        public static RoyalRocketPieceAnimator Attach(Item item)
        {
            if (!Application.isPlaying || !item || !item.sprRenderer || item.destroying || !IsRocket(item) || !RoyalEffectsProfile.Get) return null;
            var animator = item.GetComponent<RoyalRocketPieceAnimator>();
            if (!animator) animator = item.gameObject.AddComponent<RoyalRocketPieceAnimator>();
            return animator;
        }

        /// <summary>Starts the Reveal for a Rocket that Match-4 just created, with its light and spark pop.</summary>
        public static RoyalRocketPieceAnimator PlayReveal(Item item)
        {
            var animator = Attach(item);
            if (!animator) return null;
            animator.Begin(Phase.Reveal);
            RoyalPowerupDirector.Get?.RocketRevealPop(item.transform.position);
            return animator;
        }

        /// <summary>The live board Rocket at a square, including one that is being activated this frame.</summary>
        public static RoyalRocketPieceAnimator At(Square square)
        {
            if (!square) return null;
            foreach (var animator in live)
                if (animator && animator.item && animator.item.square == square) return animator;
            return null;
        }

        public bool Horizontal => item && item.currentType == ItemsTypes.HORIZONTAL_STRIPPED;
        public bool Destroying => item && item.destroying;
        public SpriteRenderer Body => item ? item.sprRenderer : null;
        // The vertical board art is authored upright; without it the horizontal art is rotated 90 degrees.
        public bool ArtAxisIsX => Horizontal || !RoyalGameplayArt.Art("rocket-vertical");

        void Awake()
        {
            item = GetComponent<Item>();
            if (!item || !item.sprRenderer) { enabled = false; return; }
            var parent = item.sprRenderer.transform;
            highlight = CreateOverlay(parent, "Rocket highlight");
            // The sheen is a soft white band clipped to the Rocket's silhouette. It keeps the default
            // alpha-blended sprite material: the additive overlay does not draw through a SpriteMask here.
            sheen = CreateOverlay(parent, "Rocket idle sheen", false);
            sheen.sprite = SheenSprite();
            sheen.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            var maskObject = new GameObject("Rocket sheen mask");
            maskObject.layer = parent.gameObject.layer;
            maskObject.transform.SetParent(parent, false);
            sheenMask = maskObject.AddComponent<SpriteMask>();
            sheenMask.alphaCutoff = .4f;
            sheenMask.isCustomRangeActive = true;
            sheenMask.enabled = false;
            sparkle = CreateOverlay(parent, "Rocket idle sparkle");
            sparkle.sprite = SparkleSprite();
            Begin(Phase.Idle);
        }

        void OnEnable() { if (!live.Contains(this)) live.Add(this); }
        void OnDisable() { live.Remove(this); HideOverlays(); }
        void OnDestroy()
        {
            live.Remove(this);
            if (highlight) Destroy(highlight.gameObject);
            if (sheen) Destroy(sheen.gameObject);
            if (sheenMask) Destroy(sheenMask.gameObject);
            if (sparkle) Destroy(sparkle.gameObject);
        }

        SpriteRenderer CreateOverlay(Transform parent, string name, bool additive = true)
        {
            var go = new GameObject(name);
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            if (additive) renderer.sharedMaterial = OverlayMaterial();
            renderer.enabled = false;
            return renderer;
        }

        void Begin(Phase phase)
        {
            Current = phase;
            age = 0;
            // The first rendered frame shows the phase's first pose; the clock starts on the next frame.
            startFrame = Time.frameCount;
        }

        void Update()
        {
            var level = LevelManager.THIS;
            if (Time.frameCount != startFrame && !(level && level.gameStatus == GameState.Pause)) age += Time.deltaTime;
            if (Current == Phase.Reveal && age >= RevealDuration)
            {
                // The Reveal ends exactly at rest; Idle starts from rest with zero velocity.
                Current = Phase.Idle;
                age = 0;
            }
        }

        /// <summary>Called by RoyalPieceSkin after the sprite child is fitted to its cell.</summary>
        public void Pose(Transform tr)
        {
            float axis = 1, cross = 1, turn = 0, glow = 0;
            if (Current == Phase.Reveal)
            {
                float t = Mathf.Min(age, RevealDuration), s = revealScale.Evaluate(t);
                axis = s * revealAxis.Evaluate(t);
                cross = s * revealCross.Evaluate(t);
                // Horizontal pieces flick the opposite way to vertical ones, like the previous reveal.
                turn = revealTurn.Evaluate(t) * (Horizontal ? -1 : 1);
                glow = .55f * Mathf.Clamp01(1 - t / .22f);
            }
            else
            {
                float phase = age % IdlePeriod / IdlePeriod, wave = .5f - .5f * Mathf.Cos(phase * Mathf.PI * 2);
                // Breathing starts and ends at rest; rocking eases in over the first idle second.
                axis = 1 + .022f * wave;
                cross = 1 + .012f * wave;
                turn = 1.6f * Mathf.Sin(phase * Mathf.PI * 2) * Mathf.SmoothStep(0, 1, age / .9f);
            }
            AxisScale = axis; CrossScale = cross; Turn = turn; Highlight = glow;
            bool axisX = ArtAxisIsX;
            var scale = tr.localScale;
            tr.localScale = new Vector3(scale.x * (axisX ? axis : cross), scale.y * (axisX ? cross : axis), scale.z);
            tr.localRotation *= Quaternion.Euler(0, 0, turn);
        }

        void LateUpdate()
        {
            if (!item || !item.sprRenderer || item.destroying || !IsRocket(item) || !item.sprRenderer.enabled) { HideOverlays(); return; }
            var source = item.sprRenderer;
            int order = source.sortingOrder;
            highlight.sprite = source.sprite;
            highlight.sortingLayerID = sheen.sortingLayerID = sparkle.sortingLayerID = source.sortingLayerID;
            highlight.sortingOrder = sheen.sortingOrder = order + 1;
            sparkle.sortingOrder = order + 2;
            highlight.color = new Color(1, .96f, .82f, Highlight * source.color.a);
            highlight.enabled = Highlight > .001f;

            var size = source.sprite ? source.sprite.bounds.size : Vector3.one;
            bool axisX = ArtAxisIsX;
            float length = axisX ? size.x : size.y, width = axisX ? size.y : size.x;
            float loopTime = age % IdlePeriod;
            // Alternate the sweep direction every loop; the sparkle marks the nose the sheen leaves from.
            float direction = Mathf.FloorToInt(age / IdlePeriod) % 2 == 0 ? 1 : -1;
            // Counter the fitted parent's non-uniform scale so round shapes stay round.
            var parentScale = source.transform.localScale;
            float aspect = Mathf.Abs(parentScale.x / Mathf.Max(.0001f, parentScale.y));

            float sweep = (loopTime - SheenStart) / SheenLife;
            SheenStrength = Current == Phase.Idle && sweep >= 0 && sweep < 1 ? Mathf.Sin(sweep * Mathf.PI) : 0;
            bool lit = SheenStrength > .001f;
            sheen.enabled = sheenMask.enabled = lit;
            if (lit)
            {
                sheenMask.sprite = source.sprite;
                sheenMask.frontSortingLayerID = sheenMask.backSortingLayerID = source.sortingLayerID;
                sheenMask.backSortingOrder = order;
                sheenMask.frontSortingOrder = order + 1;
                float along = Mathf.Lerp(-.62f, .62f, Mathf.SmoothStep(0, 1, sweep)) * length * direction;
                sheen.transform.localPosition = axisX ? new Vector3(along, 0, 0) : new Vector3(0, along, 0);
                // A soft diagonal band a fifth of the Rocket wide, tall enough to cross it at an angle.
                sheen.transform.localRotation = Quaternion.Euler(0, 0, axisX ? -28 : 62);
                // The band sprite is 1 x 0.125 units.
                float span = Mathf.Max(length, width) * 1.6f;
                sheen.transform.localScale = new Vector3(.2f * length, span / .125f, 1);
                sheen.color = new Color(1, .99f, .94f, .42f * SheenStrength * source.color.a);
            }

            float glint = (loopTime - SparkleStart) / SparkleLife;
            SparkleStrength = Current == Phase.Idle && glint >= 0 && glint < 1 ? Mathf.Sin(glint * Mathf.PI) : 0;
            sparkle.enabled = SparkleStrength > .001f;
            if (!sparkle.enabled) return;
            float tip = .31f * length * direction, beside = .15f * width;
            sparkle.transform.localPosition = axisX ? new Vector3(tip, beside, 0) : new Vector3(beside, tip, 0);
            float star = .24f * Mathf.Max(length, width) * Mathf.Sqrt(SparkleStrength);
            sparkle.transform.localScale = new Vector3(star, star * aspect, 1);
            sparkle.transform.localRotation = Quaternion.Euler(0, 0, 45 * glint);
            sparkle.color = new Color(1, .95f, .78f, SparkleStrength * source.color.a);
        }

        void HideOverlays()
        {
            if (highlight) highlight.enabled = false;
            if (sheen) sheen.enabled = false;
            if (sheenMask) sheenMask.enabled = false;
            if (sparkle) sparkle.enabled = false;
            SheenStrength = SparkleStrength = 0;
        }

        internal static Material OverlayMaterial()
        {
            if (overlayMaterial) return overlayMaterial;
            // Resources keeps the shader in player builds; SpriteRenderer.color drives the alpha.
            var source = Resources.Load<Material>("JuiceAdditive");
            overlayMaterial = source ? new Material(source) : new Material(Shader.Find("Sprites/Default"));
            overlayMaterial.name = "Rocket overlay (additive)";
            overlayMaterial.hideFlags = HideFlags.HideAndDontSave;
            overlayMaterial.EnableKeyword("SPRITE_RENDERER_COLOR");
            overlayMaterial.SetFloat("_UseRendererColor", 1);
            return overlayMaterial;
        }

        /// <summary>A soft four-point star drawn once at runtime, unit size at 64 PPU.</summary>
        static Sprite SparkleSprite()
        {
            if (sparkleSprite) return sparkleSprite;
            const int n = 64;
            var pixels = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float px = (x + .5f) / n * 2 - 1, py = (y + .5f) / n * 2 - 1;
                    float diamond = Mathf.Pow(Mathf.Abs(px), .45f) + Mathf.Pow(Mathf.Abs(py), .45f);
                    float a = Mathf.Clamp01(Mathf.InverseLerp(1.15f, .72f, diamond)) + Mathf.Exp(-(px * px + py * py) * 26) * .42f;
                    pixels[y * n + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255));
                }
            return sparkleSprite = RuntimeSprite("Rocket sparkle", n, n, pixels);
        }

        /// <summary>A soft vertical band, unit size at 32 PPU, brightest in its centre column.</summary>
        static Sprite SheenSprite()
        {
            if (sheenSprite) return sheenSprite;
            const int w = 32, h = 4;
            var pixels = new Color32[w * h];
            for (int x = 0; x < w; x++)
            {
                float u = (x + .5f) / w * 2 - 1;
                byte a = (byte)(Mathf.Exp(-u * u * 5.5f) * 255);
                for (int y = 0; y < h; y++) pixels[y * w + x] = new Color32(255, 255, 255, a);
            }
            return sheenSprite = RuntimeSprite("Rocket sheen", w, h, pixels, w);
        }

        static Sprite RuntimeSprite(string name, int w, int h, Color32[] pixels, float ppu = 0)
        {
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.HideAndDontSave };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(.5f, .5f), ppu > 0 ? ppu : w, 0, SpriteMeshType.FullRect);
            sprite.name = name;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
