using System.Collections.Generic;
using JellyGarden.Scripts;
using JellyGarden.Scripts.Juice;
using UnityEngine;

namespace RoyalThrone.Boosters.ColorBall.V4
{
    // Reconstructed target hold. The source metadata exposes ShakeItem/CreateItemBg and AllRaysComplete, not editable
    // code. No targets, hits or conversion callbacks originate in this presentation helper.
    // 2026-10-04 (the user: the layer under a target must not be solid white; give it the gem's shape, a sparkling glow
    // and a vibration): the old contour dilated the gem's sprite inside its own rectangle, so it read as a white box.
    // A held gem now has a glow cut to its silhouette behind it (ColorBallGemGlow mode 0: a bright band on the outline
    // fading into the gem's colour, breathing), a gem-shaped flash and a passing shine on it (mode 1), four sparkles
    // twinkling on its outline, and a tremble with a slight rock. Decoration only; it uses its own random sequence.
    [DefaultExecutionOrder(1250)]
    public sealed class ColorBallChargedTargetV4 : MonoBehaviour
    {
        static readonly HashSet<ColorBallChargedTargetV4> active = new HashSet<ColorBallChargedTargetV4>();
        static Material glowMaterial;
        static Mesh quad;
        static Sprite sparkleSprite;
        const int Sparkles = 4;
        const float Pad = .22f;   // the glow quad reaches this far past the sprite, as a fraction of the gem's width
        sealed class Sparkle { public SpriteRenderer view; public float age, life, angle, size, spin; }
        Item item; Transform body; MeshRenderer glow, light; MaterialPropertyBlock props;
        readonly Sparkle[] sparkles = new Sparkle[Sparkles];
        global::System.Random random;
        Vector3 rest, applied; Quaternion restTurn; Bounds shape; Color tint; float age, phase; bool held;
        public int Lease { get; private set; }
        public static int Active => active.Count;
        public static ColorBallChargedTargetV4 Hold(Item owner)
        {
            if (!owner || owner.destroying || owner.currentType != ItemsTypes.NONE) return null;
            var pose = owner.GetComponent<ColorBallChargedTargetV4>();
            if (!pose) pose = owner.gameObject.AddComponent<ColorBallChargedTargetV4>();
            pose.Release(); pose.item = owner; pose.rest = pose.applied = owner.transform.position;
            pose.age = 0; pose.phase = owner.square ? (owner.square.col * .43f + owner.square.row * .71f) : 0;
            pose.held = true; pose.Lease++; pose.enabled = true; active.Add(pose);
            var gem = owner.sprRenderer;
            if (gem && gem.sprite && pose.Build(gem))
            {
                pose.body = gem.transform; pose.restTurn = pose.body.localRotation;
                pose.Fit(gem);
                pose.glow.enabled = pose.light.enabled = true;
                pose.random = new global::System.Random(pose.Lease * 7919 + (owner.square ? owner.square.col * 131 + owner.square.row : 0));
                for (int i = 0; i < Sparkles; i++) { var s = pose.sparkles[i]; s.life = 0; s.age = -.06f - i * .11f; s.view.enabled = false; }
                pose.Draw();
            }
            return pose;
        }

        static Mesh Quad()
        {
            if (quad) return quad;
            quad = new Mesh { name = "Color Ball target glow quad" };
            quad.vertices = new[] { new Vector3(-.5f, -.5f), new Vector3(.5f, -.5f), new Vector3(-.5f, .5f), new Vector3(.5f, .5f) };
            quad.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
            quad.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            quad.RecalculateBounds();
            return quad;
        }

        bool Build(SpriteRenderer gem)
        {
            if (glow) return true;
            if (!glowMaterial)
            {
                var shader = Resources.Load<Shader>("RoyalColorBall/ColorBallGemGlow");
                if (!shader) return false;
                glowMaterial = new Material(shader) { name = "Color Ball held target glow" };
            }
            if (!sparkleSprite)
            {
                var tex = SparkleTextureFactory.FourPointSparkle;
                sparkleSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f), tex.width);
            }
            props = new MaterialPropertyBlock();
            MeshRenderer Layer(string name)
            {
                var go = new GameObject(name); go.transform.SetParent(gem.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = Quad();
                var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = glowMaterial;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false; r.enabled = false;
                return r;
            }
            glow = Layer("Held target glow (gem silhouette)");
            light = Layer("Held target light (rim, shine, flash)");
            for (int i = 0; i < Sparkles; i++)
            {
                var s = new GameObject("Held target sparkle " + i).AddComponent<SpriteRenderer>();
                s.transform.SetParent(gem.transform, false); s.sprite = sparkleSprite; s.maskInteraction = SpriteMaskInteraction.None; s.enabled = false;
                sparkles[i] = new Sparkle { view = s };
            }
            return true;
        }

        // Lays the two quads over the gem's sprite (and past it), and points the shader at the sprite's part of its texture.
        void Fit(SpriteRenderer gem)
        {
            var sprite = gem.sprite; shape = sprite.bounds;
            Vector2 min = sprite.uv[0], max = sprite.uv[0];
            foreach (var uv in sprite.uv) { min = Vector2.Min(min, uv); max = Vector2.Max(max, uv); }
            Vector2 size = shape.size; float pad = size.x * Pad;
            Vector2 span = size + Vector2.one * pad * 2;
            int layer = gem.gameObject.layer;
            foreach (var r in new[] { glow, light })
            {
                r.gameObject.layer = layer; r.sortingLayerID = gem.sortingLayerID;
                r.transform.localPosition = new Vector3(shape.center.x, shape.center.y, r == glow ? -.02f : -.03f);
                r.transform.localRotation = Quaternion.identity; r.transform.localScale = new Vector3(span.x, span.y, 1);
            }
            // Behind the gem but in front of its contact shadow (same order, nearer the camera); the light goes on top.
            glow.sortingOrder = gem.sortingOrder - 1; light.sortingOrder = gem.sortingOrder + 1;
            foreach (var s in sparkles) { s.view.gameObject.layer = layer; s.view.sortingLayerID = gem.sortingLayerID; s.view.sortingOrder = gem.sortingOrder + 2; }
            props.Clear();
            props.SetTexture("_MainTex", sprite.texture);
            props.SetVector("_UVRect", new Vector4(min.x, min.y, max.x - min.x, max.y - min.y));
            props.SetVector("_Map", new Vector4(span.x / size.x, span.y / size.y, -pad / size.x, -pad / size.y));
            props.SetVector("_Unit", new Vector4(1, size.x / size.y, 0, 0));
            tint = Tint(sprite.name);
        }

        // The glow takes the gem's own colour (the art's names; anything else gets warm gold).
        static Color Tint(string name)
        {
            name = name.ToLowerInvariant();
            if (name.Contains("book")) return new Color(1f, .36f, .30f);
            if (name.Contains("crown")) return new Color(1f, .80f, .22f);
            if (name.Contains("leaf")) return new Color(.42f, .95f, .38f);
            if (name.Contains("shield")) return new Color(.36f, .70f, 1f);
            if (name.Contains("gem")) return new Color(1f, .42f, .92f);
            return new Color(1f, .84f, .40f);
        }

        void LateUpdate()
        {
            if (!held) return;
            if (!item || item.destroying || item.currentType != ItemsTypes.NONE || item.falling || item.inputSwapLocked ||
                !item.square || item.square.item != item) { Release(); return; }
            if (LevelManager.THIS && LevelManager.THIS.gameStatus == GameState.Pause) return;
            age += Time.deltaTime;
            float cell = ColorBallVisualV4.CellSize;
            // Tremble: two mixed frequencies per axis so it reads as a buzz rather than a sway, growing as the charge builds.
            float k = Mathf.SmoothStep(0, 1, age / .06f);
            float amount = cell * Mathf.Lerp(.018f, .03f, Mathf.SmoothStep(0, 1, age / .6f)) * k;
            var shake = new Vector3(Mathf.Sin(age * 41f + phase) + .35f * Mathf.Sin(age * 67f + phase * 2), .6f * Mathf.Sin(age * 37f + phase * 1.3f) + .3f * Mathf.Sin(age * 59f + phase), 0);
            applied = rest + shake * amount;
            item.transform.position = applied;
            if (body) body.localRotation = restTurn * Quaternion.Euler(0, 0, Mathf.Sin(age * 29f + phase) * 2.4f * k);
            Draw();
        }

        void Draw()
        {
            if (!glow || !glow.enabled) return;
            float k = Mathf.SmoothStep(0, 1, age / .08f);
            // Contact: the silhouette glow pops a little larger and settles; the gem flashes in its own shape.
            float pop = 1 + .16f * (1 - Mathf.SmoothStep(0, 1, age / .24f));
            Vector2 span = (Vector2)shape.size + Vector2.one * shape.size.x * Pad * 2;
            glow.transform.localScale = new Vector3(span.x * pop, span.y * pop, 1);
            float breath = .5f + .5f * Mathf.Sin(age * 7.5f + phase);
            props.SetColor("_Tint", tint);
            props.SetFloat("_Mode", 0); props.SetFloat("_Grow", breath); props.SetFloat("_Flash", 0); props.SetFloat("_Shine", -1);
            props.SetColor("_Color", new Color(1, 1, 1, k * (.80f + .18f * breath)));
            glow.SetPropertyBlock(props);
            float flash = .7f * (1 - Mathf.SmoothStep(0, 1, age / .22f));
            float shine = Mathf.Repeat(age * 1.5f + phase * .37f, 1.6f) - .3f;   // a glint crosses the gem about every second
            props.SetFloat("_Mode", 1); props.SetFloat("_Flash", flash); props.SetFloat("_Shine", shine);
            props.SetColor("_Color", new Color(1, 1, 1, .35f + .65f * k));
            light.SetPropertyBlock(props);
            // Sparkles on the outline: each pops, turns and fades, then reappears somewhere else on the edge.
            for (int i = 0; i < Sparkles; i++)
            {
                var s = sparkles[i];
                s.age += Time.deltaTime;
                if (s.age < 0) { s.view.enabled = false; continue; }
                if (s.life <= 0 || s.age >= s.life)
                {
                    s.age = 0; s.life = .42f + (float)random.NextDouble() * .3f;
                    s.angle = (i * 90 + (float)random.NextDouble() * 70 - 35 + 45) * Mathf.Deg2Rad;
                    s.size = shape.size.x * (.36f + (float)random.NextDouble() * .16f);
                    s.spin = (random.NextDouble() < .5 ? -1 : 1) * (50 + (float)random.NextDouble() * 60);
                }
                float t = s.age / s.life, bloom = Mathf.Pow(Mathf.Sin(t * Mathf.PI), 1.4f);
                var edge = new Vector3(Mathf.Cos(s.angle) * shape.extents.x, Mathf.Sin(s.angle) * shape.extents.y, 0) * (1.0f + .14f * t);
                s.view.transform.localPosition = shape.center + edge + Vector3.back * .04f;
                s.view.transform.localRotation = Quaternion.Euler(0, 0, s.spin * s.age);
                s.view.transform.localScale = Vector3.one * s.size * (.35f + .65f * bloom);
                s.view.color = new Color(1, 1, .96f, Mathf.Min(1, bloom * 1.25f));
                s.view.enabled = true;
            }
        }

        public void Release(int expectedLease) { if (expectedLease == Lease) Release(); }
        void Release()
        {
            if (held && item && !item.destroying && !item.falling && !item.inputSwapLocked &&
                (item.transform.position - applied).sqrMagnitude < .000001f) item.transform.position = rest;
            if (held && body) body.localRotation = restTurn;
            held = false; active.Remove(this);
            if (glow) glow.enabled = false; if (light) light.enabled = false;
            foreach (var s in sparkles) if (s != null && s.view) s.view.enabled = false;
        }
        public static void ClearAll()
        {
            foreach (var pose in new List<ColorBallChargedTargetV4>(active)) if (pose) pose.Release();
            active.Clear();
        }
        void OnDisable() { Release(); }
        void OnDestroy() { active.Remove(this); }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { active.Clear(); glowMaterial = null; quad = null; sparkleSprite = null; }
    }
}
