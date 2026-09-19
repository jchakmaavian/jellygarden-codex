using RoyalThrone.Match3Vfx;
using UnityEngine;

namespace JellyGarden.Scripts.Story
{
    /// <summary>
    /// The shipping rocket: a short squeeze, two accelerating heads, tapered gold exhaust,
    /// and small contact flashes. Distances are in board cells. ReachesAt is the inverse of
    /// DistanceAt, shared with the clear coroutine so visuals and impacts cannot drift.
    /// </summary>
    public sealed class StoryRocketFlight : MonoBehaviour
    {
        public const float Anticipation = 0.09f;
        public const float Acceleration = 0.045f;
        public const float CellsPerSecond = 24f;
        const float TrailLife = 0.09f;
        const int HeadCapacity = 32;
        const int MoteCapacity = 192;

        static StoryRocketFlight instance;
        readonly Head[] heads = new Head[HeadCapacity];
        readonly Mote[] motes = new Mote[MoteCapacity];
        [SerializeField, HideInInspector] Material trailMaterial;
        int nextMote;

        sealed class Head
        {
            public Transform root;
            public SpriteRenderer body, glow, flame;
            public TrailRenderer trail, core;
            public Vector3 origin, direction;
            public float pitch, reach, age, fit, nextSmoke;
            public bool active, launched, retired, showAnticipation;
        }

        sealed class Mote
        {
            public SpriteRenderer renderer;
            public Vector3 position, velocity;
            public Color color;
            public float age, life, startSize, endSize, spin;
            public bool active;
        }

        public static float DistanceAt(float seconds)
        {
            float t = Mathf.Max(0f, seconds - Anticipation);
            return t < Acceleration
                ? CellsPerSecond * t * t / (2f * Acceleration)
                : CellsPerSecond * (t - Acceleration * 0.5f);
        }

        public static float ReachesAt(float cells)
        {
            float distance = Mathf.Abs(cells);
            float rampDistance = CellsPerSecond * Acceleration * 0.5f;
            return Anticipation + (distance < rampDistance
                ? Mathf.Sqrt(2f * Acceleration * distance / CellsPerSecond)
                : distance / CellsPerSecond + Acceleration * 0.5f);
        }

        public static void Prewarm()
        {
            if (!Application.isPlaying || instance != null) return;
            var go = new GameObject("~StoryRocketFlight");
            // Scene-owned: changing scenes removes every trail and pending particle.
            instance = go.AddComponent<StoryRocketFlight>();
        }

        void OnEnable()
        {
            instance = this;
            if (heads[0] != null) return;
            // Unity can reload scripts while the board is playing. The managed pool resets,
            // but its native children survive that reload, so rebuild them together.
            foreach (Transform child in transform) Destroy(child.gameObject);
            if (trailMaterial != null) Destroy(trailMaterial);
            BuildPool();
        }

        public static void Fire(Vector3 origin, bool horizontal, int negative, int positive,
                                float pitch, Sprite sprite)
        {
            Prewarm();
            if (instance == null) return;
            if (sprite == null) sprite = Resources.Load<Sprite>("Story/TreasureHunt/UI/Layers/rocket");
            Vector3 direction = horizontal ? Vector3.right : Vector3.up;
            instance.Launch(origin, direction, positive, pitch, sprite, true);
            instance.Launch(origin, -direction, negative, pitch, sprite, false);
        }

        public static void Contact(Vector3 position, float pitch)
        {
            if (!StoryRocketVfx.Enabled) return;
            Prewarm();
            if (instance != null) instance.Impact(position, pitch, false);
        }

        void BuildPool()
        {
            var source = Resources.Load<Material>("Match3Vfx/Additive");
            trailMaterial = source != null ? new Material(source) : new Material(Shader.Find("Sprites/Default"));
            trailMaterial.name = "Rocket exhaust";
            trailMaterial.mainTexture = Match3Shapes.SoftGlow;
            trailMaterial.DisableKeyword("SPRITE_RENDERER_COLOR");
            if (trailMaterial.HasProperty("_UseRendererColor")) trailMaterial.SetFloat("_UseRendererColor", 0f);
            if (trailMaterial.HasProperty("_Boost")) trailMaterial.SetFloat("_Boost", 1.25f);

            for (int i = 0; i < heads.Length; i++)
            {
                var root = new GameObject("Rocket head " + i).transform;
                root.SetParent(transform, false);
                heads[i] = new Head
                {
                    root = root,
                    body = Sprite(root, "Rocket", null, 30),
                    glow = Sprite(root, "Nose glow", Match3Shapes.GlowSprite, 29),
                    flame = Sprite(root, "Exhaust flame", Match3Shapes.GlowSprite, 28),
                    trail = Trail(root, "Gold exhaust", 26, 0.30f,
                        new Color(1f, 0.63f, 0.12f, 0.85f), new Color(1f, 0.27f, 0.02f, 0f)),
                    core = Trail(root, "Hot exhaust core", 27, 0.085f,
                        new Color(1f, 1f, 0.91f, 1f), new Color(1f, 0.87f, 0.4f, 0f))
                };
                root.gameObject.SetActive(false);
            }
            for (int i = 0; i < motes.Length; i++)
            {
                var renderer = Sprite(transform, "Rocket sparkle " + i, Match3Shapes.SparkleSprite, 31);
                renderer.gameObject.SetActive(false);
                motes[i] = new Mote { renderer = renderer };
            }
        }

        static SpriteRenderer Sprite(Transform parent, string name, Sprite sprite, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return renderer;
        }

        TrailRenderer Trail(Transform parent, string name, int order, float width, Color start, Color end)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var trail = go.AddComponent<TrailRenderer>();
            trail.sharedMaterial = trailMaterial;
            trail.sortingOrder = order;
            trail.time = TrailLife;
            trail.minVertexDistance = 0.015f;
            trail.numCapVertices = 3;
            trail.numCornerVertices = 2;
            trail.widthCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
            trail.widthMultiplier = width;
            trail.startColor = start;
            trail.endColor = end;
            trail.emitting = false;
            return trail;
        }

        void Launch(Vector3 origin, Vector3 direction, int cells, float pitch, Sprite sprite, bool visible)
        {
            foreach (Head head in heads)
            {
                if (head.active) continue;
                head.active = true;
                head.launched = head.retired = false;
                head.showAnticipation = visible;
                head.origin = origin + Vector3.forward * 0.12f;
                head.direction = direction;
                head.pitch = Mathf.Max(0.001f, pitch);
                head.reach = Mathf.Max(0, cells) + 0.7f;
                head.age = head.nextSmoke = 0f;
                head.body.sprite = sprite != null ? sprite : Match3Shapes.GemSprite;
                head.fit = head.pitch / Mathf.Max(0.001f, head.body.sprite.bounds.size.x);
                head.root.SetPositionAndRotation(head.origin,
                    Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg));
                head.body.transform.localScale = Vector3.one * head.fit * 0.84f;
                head.body.enabled = visible;
                head.body.color = Color.white;
                head.glow.enabled = head.flame.enabled = false;
                head.trail.transform.localPosition = head.core.transform.localPosition = Vector3.left * pitch * 0.25f;
                head.trail.widthMultiplier = pitch * 0.30f;
                head.core.widthMultiplier = pitch * 0.085f;
                head.root.gameObject.SetActive(true);
                head.trail.Clear();
                head.core.Clear();
                head.trail.emitting = head.core.emitting = false;
                return;
            }
        }

        void Update()
        {
            bool paused = LevelManager.THIS != null && LevelManager.THIS.gameStatus == GameState.Pause;
            if (paused || Time.deltaTime <= 0f)
            {
                // TrailRenderer ages its vertices independently of our clock.
                foreach (Head head in heads)
                    if (head != null && head.active)
                    {
                        head.trail.time += Time.deltaTime;
                        head.core.time += Time.deltaTime;
                    }
                return;
            }
            float dt = Time.deltaTime;
            foreach (Head head in heads)
            {
                if (!head.active) continue;
                head.trail.time = head.core.time = TrailLife;
                head.age += dt;
                if (head.age < Anticipation)
                {
                    float squeeze = Mathf.Sin(head.age / Anticipation * Mathf.PI);
                    head.body.transform.localScale = new Vector3(1f - squeeze * 0.16f,
                        1f + squeeze * 0.12f, 1f) * head.fit * 0.84f;
                    continue;
                }

                float distance = Mathf.Min(head.reach, DistanceAt(head.age));
                if (!head.launched)
                {
                    head.launched = true;
                    head.body.enabled = head.glow.enabled = head.flame.enabled = true;
                    head.body.transform.localScale = new Vector3(1.08f, 0.90f, 1f) * head.fit * 0.62f;
                    head.trail.emitting = head.core.emitting = true;
                    if (head.showAnticipation) Impact(head.origin, head.pitch, true);
                }
                head.root.position = head.origin + head.direction * (distance * head.pitch);
                head.glow.transform.localPosition = Vector3.right * head.pitch * 0.18f;
                head.glow.transform.localScale = new Vector3(0.42f, 0.35f, 1f) * head.pitch;
                head.glow.color = new Color(1f, 0.91f, 0.58f, 0.8f);
                head.flame.transform.localPosition = Vector3.left * head.pitch * 0.40f;
                head.flame.transform.localScale = new Vector3(0.85f, 0.23f, 1f) * head.pitch;
                head.flame.color = new Color(1f, 0.72f, 0.24f, 0.95f);

                // Emit at travelled positions, including skipped positions after a slow frame.
                while (head.nextSmoke <= distance)
                {
                    Vector3 p = head.origin + head.direction * ((head.nextSmoke - 0.3f) * head.pitch);
                    Emit(p, Vector3.zero, Match3Shapes.GlowSprite,
                        new Color(1f, 0.90f, 0.70f, 0.24f), head.pitch * 0.15f, head.pitch * 0.36f, 0.16f, 0f);
                    head.nextSmoke += 0.40f;
                }
                if (distance >= head.reach && !head.retired)
                {
                    head.retired = true;
                    head.body.enabled = head.glow.enabled = head.flame.enabled = false;
                    head.trail.emitting = head.core.emitting = false;
                }
                if (head.age >= ReachesAt(head.reach) + TrailLife)
                {
                    head.active = false;
                    head.trail.Clear();
                    head.core.Clear();
                    head.root.gameObject.SetActive(false);
                }
            }

            foreach (Mote mote in motes)
            {
                if (!mote.active) continue;
                mote.age += dt;
                float t = Mathf.Clamp01(mote.age / mote.life);
                if (t >= 1f)
                {
                    mote.active = false;
                    mote.renderer.gameObject.SetActive(false);
                    continue;
                }
                float ease = 1f - (1f - t) * (1f - t);
                mote.renderer.transform.position = mote.position + mote.velocity * (mote.life * ease);
                mote.renderer.transform.localScale = Vector3.one * Mathf.Lerp(mote.startSize, mote.endSize, ease);
                mote.renderer.transform.Rotate(0f, 0f, mote.spin * dt);
                Color color = mote.color;
                color.a *= (1f - t) * (1f - t);
                mote.renderer.color = color;
            }
        }

        void Impact(Vector3 position, float pitch, bool launch)
        {
            Emit(position, Vector3.zero, Match3Shapes.GlowSprite, new Color(1f, 0.93f, 0.66f, 0.95f),
                pitch * (launch ? 0.9f : 0.65f), pitch * 0.15f, 0.10f, 0f);
            Emit(position, Vector3.zero, Match3Shapes.RingSprite, new Color(1f, 0.83f, 0.40f, 0.65f),
                pitch * 0.18f, pitch * (launch ? 1.0f : 0.65f), 0.14f, 0f);
            for (int i = 0; i < 4; i++)
            {
                float angle = (i * 90f + 35f) * Mathf.Deg2Rad;
                Emit(position, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * pitch * 1.4f,
                    Match3Shapes.SparkleSprite, new Color(1f, 0.92f, 0.61f, 1f),
                    pitch * 0.13f, 0f, 0.20f, i % 2 == 0 ? 140f : -140f);
            }
        }

        void Emit(Vector3 position, Vector3 velocity, Sprite sprite, Color color,
                  float startSize, float endSize, float life, float spin)
        {
            for (int i = 0; i < motes.Length; i++)
            {
                Mote mote = motes[nextMote++ % motes.Length];
                if (mote.active) continue;
                mote.active = true;
                mote.age = 0f;
                mote.life = life;
                mote.position = position + Vector3.forward * 0.10f;
                mote.velocity = velocity;
                mote.color = color;
                mote.startSize = startSize;
                mote.endSize = endSize;
                mote.spin = spin;
                mote.renderer.sprite = sprite;
                mote.renderer.color = color;
                mote.renderer.transform.SetPositionAndRotation(mote.position, Quaternion.identity);
                mote.renderer.transform.localScale = Vector3.one * startSize;
                mote.renderer.gameObject.SetActive(true);
                return;
            }
        }

        void OnDestroy()
        {
            if (trailMaterial != null) Destroy(trailMaterial);
            if (instance == this) instance = null;
        }
    }
}
