// The effect that plays while a repair is happening.
//
// Configured per kind of work: dust drifting up as rubble is hauled out, sparks
// off new timber, warm embers as a wick catches, a gold shimmer over something
// polished. Nothing here touches the kit's own particle prefabs — the board's
// effects are untouched.
//
// These are UI quads, not a ParticleSystem, and that is deliberate. A
// ParticleSystem does not render inside a ScreenSpaceOverlay canvas at all: it
// is a mesh renderer and the overlay canvas draws after everything in the
// scene, so the effect is simply never visible. The cutscene is entirely UI, so
// the particles are too — a small pool of Images, moved by hand each frame.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    public sealed class RepairVfx : MonoBehaviour
    {
        // Big enough for the Royal sparkle at full rate: 260 a second living
        // two thirds of one is about 170 alive at the peak, and a pool that runs
        // dry does not look sparser, it looks like the effect stuttering. They
        // are Images on one sprite, so they batch into a single draw call.
        private const int PoolSize = 220;

        private sealed class Mote
        {
            public RectTransform Rect;
            public Image Image;
            public Vector2 Velocity;
            public Vector2 Gravity;
            public float Life;
            public float Age;
            public float Size;
            public float Spin;
            public Color Colour;
            public bool Alive;
        }

        private readonly List<Mote> _pool = new List<Mote>(PoolSize);
        private RectTransform _area;

        private bool _emitting;
        private float _emitAccumulator;
        private float _rate;
        private RepairWorkType _work;
        private float _width;
        private float _height;

        private static Sprite _dot;
        private static Sprite _spark;

        // The Royal mode: one gold sparkle effect, thrown over the part of the
        // room a repair actually changed rather than over the whole picture.
        //
        // It exists beside the eight per-work effects rather than replacing
        // them because the two are answering different questions. Dust, sparks
        // and embers say what KIND of work this is, which is what a cutscene on
        // its own screen has to carry — there is nothing else on it. On the room
        // the player is already looking at, the room says that; the effect only
        // has to say WHERE, and Royal Match answers it the same way every time.
        private bool _royal;
        private Rect _region;

        public static RepairVfx Attach(RectTransform parent)
        {
            var go = new GameObject("RepairVfx", typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            StoryUi.Stretch(rect);

            var vfx = go.AddComponent<RepairVfx>();
            vfx._area = rect;
            vfx.BuildPool();
            return vfx;
        }

        private void BuildPool()
        {
            for (int i = 0; i < PoolSize; i++)
            {
                var go = new GameObject("Mote", typeof(RectTransform));
                var rect = go.GetComponent<RectTransform>();
                rect.SetParent(_area, false);
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);

                var image = go.AddComponent<Image>();
                image.sprite = Dot();
                image.raycastTarget = false;
                go.SetActive(false);

                _pool.Add(new Mote { Rect = rect, Image = image });
            }
        }

        /// <summary>Starts emitting for a kind of work, sized to the room on screen.</summary>
        public void Play(RepairWorkType work, float width, float height)
        {
            _work = work;
            _royal = false;
            _width = Mathf.Max(1f, width);
            _height = Mathf.Max(1f, height);
            _emitting = true;
            _emitAccumulator = 0f;
            _rate = RateFor(work);
        }

        /// <summary>
        /// Starts the gold sparkle, confined to <paramref name="region"/> — the
        /// part of the room this repair changed, in the room's own space
        /// (origin bottom-left, 0..1 on both axes). Pass
        /// <see cref="StoryRepairRegions.Unknown"/> for a repair nobody has
        /// measured and it falls over the whole room, which is honest rather
        /// than pretty: guessing a corner would put the sparkles somewhere
        /// nothing happened.
        /// </summary>
        public void PlayRoyal(Rect region, float width, float height)
        {
            _royal = true;
            _region = region;
            _width = Mathf.Max(1f, width);
            _height = Mathf.Max(1f, height);
            _emitting = true;
            _emitAccumulator = 0f;

            // Denser than any of the work effects, and denser still on a small
            // region: what reads as a shower over a doorway reads as a drizzle
            // over a whole courtyard. Clamped so a hairline region does not try
            // to outrun the pool.
            float area = Mathf.Clamp(region.width * region.height, 0.02f, 1f);
            _rate = Mathf.Lerp(260f, 150f, area);
        }

        public void Stop()
        {
            _emitting = false;
        }

        /// <summary>Stops and clears immediately, for a skip.</summary>
        public void Clear()
        {
            _emitting = false;
            for (int i = 0; i < _pool.Count; i++)
            {
                _pool[i].Alive = false;
                _pool[i].Rect.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            if (_emitting)
            {
                _emitAccumulator += _rate * dt;
                while (_emitAccumulator >= 1f)
                {
                    _emitAccumulator -= 1f;
                    Emit();
                }
            }

            for (int i = 0; i < _pool.Count; i++)
            {
                Mote mote = _pool[i];
                if (!mote.Alive) continue;

                mote.Age += dt;
                if (mote.Age >= mote.Life)
                {
                    mote.Alive = false;
                    mote.Rect.gameObject.SetActive(false);
                    continue;
                }

                mote.Velocity += mote.Gravity * dt;
                mote.Rect.anchoredPosition += mote.Velocity * dt;
                mote.Rect.Rotate(0f, 0f, mote.Spin * dt);

                float t = mote.Age / mote.Life;

                // In fast, out slow, so nothing pops on or off.
                float alpha = t < 0.15f
                    ? t / 0.15f
                    : 1f - (t - 0.15f) / 0.85f;

                float scale = Mathf.Lerp(0.6f, 1.15f, Mathf.Min(1f, t * 3f))
                              * Mathf.Lerp(1f, 0.5f, t);

                mote.Image.color = new Color(mote.Colour.r, mote.Colour.g, mote.Colour.b,
                    Mathf.Clamp01(alpha) * mote.Colour.a);
                mote.Rect.sizeDelta = Vector2.one * (mote.Size * scale);
            }
        }

        private void Emit()
        {
            Mote mote = null;
            for (int i = 0; i < _pool.Count; i++)
            {
                if (!_pool[i].Alive) { mote = _pool[i]; break; }
            }
            if (mote == null) return;

            float w = _width, h = _height;

            if (_royal)
            {
                EmitRoyal(mote, w, h);
                return;
            }

            Vector2 spawn;
            Vector2 velocity;
            Vector2 gravity = Vector2.zero;
            Color colour;
            float life;
            float size;
            float spin = Random.Range(-90f, 90f);

            switch (_work)
            {
                case RepairWorkType.Clear:
                    // Lifts off the floor and drifts away.
                    spawn = new Vector2(Random.Range(-w * 0.45f, w * 0.45f),
                        Random.Range(-h * 0.45f, -h * 0.05f));
                    velocity = new Vector2(Random.Range(-w * 0.06f, w * 0.06f),
                        Random.Range(h * 0.10f, h * 0.26f));
                    colour = Lerp(new Color(0.78f, 0.70f, 0.54f), new Color(0.52f, 0.45f, 0.34f));
                    life = Random.Range(1.1f, 1.8f);
                    size = Random.Range(w * 0.030f, w * 0.075f);
                    break;

                case RepairWorkType.Water:
                    spawn = new Vector2(Random.Range(-w * 0.45f, w * 0.45f),
                        Random.Range(0f, h * 0.45f));
                    velocity = new Vector2(Random.Range(-w * 0.03f, w * 0.03f), -h * 0.12f);
                    gravity = new Vector2(0f, -h * 0.55f);
                    colour = Lerp(new Color(0.60f, 0.80f, 0.92f), new Color(0.34f, 0.56f, 0.75f));
                    life = Random.Range(0.7f, 1.2f);
                    size = Random.Range(w * 0.012f, w * 0.032f);
                    break;

                case RepairWorkType.Rebuild:
                    // Sparks thrown off the work, falling back.
                    spawn = new Vector2(Random.Range(-w * 0.35f, w * 0.35f),
                        Random.Range(-h * 0.25f, h * 0.15f));
                    velocity = new Vector2(Random.Range(-w * 0.28f, w * 0.28f),
                        Random.Range(h * 0.10f, h * 0.32f));
                    gravity = new Vector2(0f, -h * 0.75f);
                    colour = Lerp(new Color(1f, 0.84f, 0.42f), new Color(0.92f, 0.48f, 0.14f));
                    life = Random.Range(0.6f, 1.0f);
                    size = Random.Range(w * 0.010f, w * 0.028f);
                    break;

                case RepairWorkType.Light:
                    // Embers, slow and warm, rising.
                    spawn = new Vector2(Random.Range(-w * 0.40f, w * 0.40f),
                        Random.Range(-h * 0.40f, 0f));
                    velocity = new Vector2(Random.Range(-w * 0.04f, w * 0.04f),
                        Random.Range(h * 0.08f, h * 0.20f));
                    colour = Lerp(new Color(1f, 0.88f, 0.55f), new Color(1f, 0.56f, 0.20f));
                    life = Random.Range(1.4f, 2.2f);
                    size = Random.Range(w * 0.014f, w * 0.040f);
                    break;

                case RepairWorkType.Hang:
                    // Dust shaken loose from above by something going up.
                    spawn = new Vector2(Random.Range(-w * 0.45f, w * 0.45f),
                        Random.Range(h * 0.10f, h * 0.45f));
                    velocity = new Vector2(Random.Range(-w * 0.05f, w * 0.05f), -h * 0.06f);
                    gravity = new Vector2(0f, -h * 0.18f);
                    colour = Lerp(new Color(0.95f, 0.90f, 0.76f), new Color(0.72f, 0.64f, 0.52f));
                    life = Random.Range(1.0f, 1.7f);
                    size = Random.Range(w * 0.016f, w * 0.042f);
                    break;

                case RepairWorkType.Polish:
                    // A shimmer sweeping left to right.
                    spawn = new Vector2(Random.Range(-w * 0.50f, -w * 0.10f),
                        Random.Range(-h * 0.40f, h * 0.40f));
                    velocity = new Vector2(Random.Range(w * 0.20f, w * 0.45f),
                        Random.Range(-h * 0.03f, h * 0.08f));
                    colour = Lerp(new Color(1f, 0.97f, 0.80f), new Color(0.97f, 0.84f, 0.46f));
                    life = Random.Range(0.8f, 1.3f);
                    size = Random.Range(w * 0.012f, w * 0.034f);
                    break;

                case RepairWorkType.Restock:
                    // Things dropping in from above.
                    spawn = new Vector2(Random.Range(-w * 0.40f, w * 0.40f), h * 0.45f);
                    velocity = new Vector2(Random.Range(-w * 0.03f, w * 0.03f), -h * 0.10f);
                    gravity = new Vector2(0f, -h * 0.90f);
                    colour = Lerp(new Color(0.88f, 0.92f, 0.62f), new Color(0.56f, 0.72f, 0.40f));
                    life = Random.Range(0.7f, 1.1f);
                    size = Random.Range(w * 0.018f, w * 0.044f);
                    break;

                case RepairWorkType.Force:
                    // Shards, thrown hard from the middle.
                    spawn = new Vector2(Random.Range(-w * 0.12f, w * 0.12f),
                        Random.Range(-h * 0.10f, h * 0.10f));
                    velocity = new Vector2(Random.Range(-w * 0.45f, w * 0.45f),
                        Random.Range(h * 0.08f, h * 0.40f));
                    gravity = new Vector2(0f, -h * 1.1f);
                    colour = Lerp(new Color(0.84f, 0.84f, 0.86f), new Color(0.44f, 0.44f, 0.48f));
                    life = Random.Range(0.5f, 0.9f);
                    size = Random.Range(w * 0.014f, w * 0.046f);
                    spin = Random.Range(-360f, 360f);
                    break;

                default:
                    spawn = new Vector2(Random.Range(-w * 0.45f, w * 0.45f),
                        Random.Range(-h * 0.35f, h * 0.20f));
                    velocity = new Vector2(Random.Range(-w * 0.05f, w * 0.05f),
                        Random.Range(h * 0.05f, h * 0.16f));
                    colour = Lerp(new Color(0.97f, 0.94f, 0.84f), new Color(0.76f, 0.71f, 0.60f));
                    life = Random.Range(1.0f, 1.6f);
                    size = Random.Range(w * 0.014f, w * 0.036f);
                    break;
            }

            mote.Image.sprite = Dot();
            mote.Rect.anchoredPosition = spawn;
            mote.Rect.localRotation = Quaternion.identity;
            mote.Rect.sizeDelta = Vector2.one * size;
            mote.Velocity = velocity;
            mote.Gravity = gravity;
            mote.Life = life;
            mote.Age = 0f;
            mote.Size = size;
            mote.Spin = spin;
            mote.Colour = colour;
            mote.Image.color = new Color(colour.r, colour.g, colour.b, 0f);
            mote.Alive = true;
            mote.Rect.gameObject.SetActive(true);
        }

        /// <summary>
        /// One gold sparkle, somewhere in the changed region.
        ///
        /// Tuned off docs/royal-match/03-repair-in-place.jpg: four-point glints
        /// in gold and white, most of them small with a few large ones through
        /// them, drifting up and out from where the work is rather than falling.
        /// They rise because the repair is something arriving; dust and debris
        /// are the effects that fall, and those belong to the work types.
        /// </summary>
        private void EmitRoyal(Mote mote, float w, float h)
        {
            // The region is in the room's space, origin bottom-left. Anchored
            // positions are measured from the middle of the room, so a spawn is
            // the point inside the region, less half the room.
            float x = (_region.x + Random.value * _region.width - 0.5f) * w;
            float y = (_region.y + Random.value * _region.height - 0.5f) * h;

            // Big glints are rare, and they are what makes the shower read as
            // gold rather than as a haze. A flat size range gives an even fog.
            // Large glints are what make this read as gold rather than as
            // dust caught in the light. The first pass ran them at 18% and a
            // twentieth of the room across, and on a bright room — which most
            // of ours are once repaired — the whole effect disappeared into the
            // wallpaper. They are nearly a third of the shower now, and the big
            // ones are a seventh of the room across.
            bool big = Random.value < 0.30f;
            float size = big
                ? Random.Range(w * 0.085f, w * 0.150f)
                : Random.Range(w * 0.028f, w * 0.060f);

            // Warm gold at the small end, near-white at the large end: the
            // brightest glints in the reference have burnt out to white.
            Color colour = big
                ? Color.Lerp(new Color(1f, 0.94f, 0.76f), Color.white, Random.value)
                : Color.Lerp(new Color(1f, 0.80f, 0.30f), new Color(1f, 0.93f, 0.62f),
                    Random.value);

            mote.Image.sprite = Spark();
            mote.Rect.anchoredPosition = new Vector2(x, y);
            mote.Rect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 90f));
            mote.Rect.sizeDelta = Vector2.one * size;
            mote.Velocity = new Vector2(Random.Range(-w * 0.05f, w * 0.05f),
                Random.Range(h * 0.02f, h * 0.14f));
            mote.Gravity = Vector2.zero;
            mote.Life = Random.Range(0.45f, 0.95f);
            mote.Age = 0f;
            mote.Size = size;
            mote.Spin = Random.Range(-40f, 40f);
            mote.Colour = colour;
            mote.Image.color = new Color(colour.r, colour.g, colour.b, 0f);
            mote.Alive = true;
            mote.Rect.gameObject.SetActive(true);
        }

        private static float RateFor(RepairWorkType work)
        {
            switch (work)
            {
                case RepairWorkType.Force: return 70f;
                case RepairWorkType.Water: return 60f;
                case RepairWorkType.Rebuild: return 55f;
                case RepairWorkType.Clear: return 45f;
                case RepairWorkType.Polish: return 50f;
                case RepairWorkType.Light: return 28f;
                case RepairWorkType.Restock: return 24f;
                default: return 35f;
            }
        }

        private static Color Lerp(Color a, Color b)
        {
            return Color.Lerp(a, b, Random.value);
        }

        /// <summary>
        /// One soft round dot, generated once and shared. A texture rather than an
        /// imported sprite so the effect has no asset dependency and cannot break
        /// if art moves.
        /// </summary>
        private static Sprite Dot()
        {
            if (_dot != null) return _dot;

            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color32[size * size];
            float centre = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - centre) / centre;
                    float dy = (y - centre) / centre;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);

                    // Soft falloff, squared so the core stays bright.
                    float a = Mathf.Clamp01(1f - d);
                    a *= a;
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();

            _dot = Sprite.Create(texture, new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f), 100f);
            _dot.hideFlags = HideFlags.HideAndDontSave;
            return _dot;
        }

        /// <summary>
        /// A four-point glint: a bright core with a thin cross of light out of
        /// it. Drawn rather than imported so it costs no asset and no atlas.
        ///
        /// The shape is what separates a sparkle from a haze. The same effect
        /// with round dots reads as smoke lit gold; the cross is the whole
        /// reason it reads as something catching the light.
        /// </summary>
        private static Sprite Spark()
        {
            if (_spark != null) return _spark;

            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color32[size * size];
            float centre = (size - 1) * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - centre) / centre;
                    float dy = (y - centre) / centre;
                    float ax = Mathf.Abs(dx);
                    float ay = Mathf.Abs(dy);

                    // The core: a small, hard-falling dot.
                    float core = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy) * 3.4f);
                    core *= core;

                    // The arms: bright along an axis, pinching to nothing at the
                    // tips. Each arm is the length along it times how close the
                    // pixel is to that axis, raised so the edge is not a band.
                    float horizontal = Mathf.Clamp01(1f - ax) * Mathf.Clamp01(1f - ay * 9f);
                    float vertical = Mathf.Clamp01(1f - ay) * Mathf.Clamp01(1f - ax * 9f);
                    float arms = Mathf.Max(horizontal, vertical);
                    arms *= arms * arms;

                    float a = Mathf.Clamp01(core + arms * 0.85f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            _spark = Sprite.Create(texture, new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f), 100f);
            _spark.hideFlags = HideFlags.HideAndDontSave;
            return _spark;
        }
    }
}
