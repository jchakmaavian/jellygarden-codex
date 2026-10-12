using UnityEngine;
using UnityEngine.UI;

namespace RoyalThrone.ReferenceGameplay
{
    /// <summary>
    /// Recovered CoinFlipAnimation sprite and FlipX keys from rfv387, using the already
    /// approved club artwork. This owns the coin pose only, never its flight or reward.
    /// See Design/Coin-Reference/DecodedCoinAnimations.json for the source bindings.
    /// </summary>
    public static class ReferenceCoinAnimation
    {
        public const float Duration = .2f;
        const string Root = "ReferenceGameplay/Sprites/";
        static readonly string[] Names =
        {
            "Items-CoinItem-coin__16660", "Items-CoinItem-coin.02__21355",
            "Items-CoinItem-coin.03__22083", "Items-CoinItem-coin.04__17107",
            "Items-CoinItem-coin.05__17096", "Items-CoinItem-coin.10__21779",
            "Items-CoinItem-coin.11__22228", "Items-CoinItem-coin.10_g__16629",
            "Items-CoinItem-coin.11_g__17115"
        };
        static readonly int[] LoopFrames = { 0, 1, 2, 3, 4, 3, 2, 1, 0, 6, 5, 6 };
        static readonly int[] OnceFrames = { 0, 1, 2, 3, 4, 3, 2, 1, 0, 8, 7, 8, 0 };
        static readonly Sprite[] sprites = new Sprite[Names.Length];
        static bool loaded;

        static Sprite Frame(float age, bool looping, out bool flipX)
        {
            if (!loaded)
            {
                for (int i = 0; i < Names.Length; i++) sprites[i] = Resources.Load<Sprite>(Root + Names[i]);
                loaded = true;
            }
            float time = looping ? Mathf.Repeat(Mathf.Max(0, age), Duration) : Mathf.Clamp(age, 0, Duration);
            int key = Mathf.Clamp(Mathf.FloorToInt(time * 60 + .00001f), 0, looping ? 11 : 12);
            // Native FlipX is on at 5/60 and off again at 10/60 seconds.
            flipX = key >= 5 && key < 10;
            return sprites[(looping ? LoopFrames : OnceFrames)[key]];
        }

        public static void Apply(Image image, float age, float size) => ApplyImage(image, age, size, true);
        public static void Apply(SpriteRenderer renderer, float age) => ApplyRenderer(renderer, age, true);
        public static void ApplyOnce(Image image, float age, float size) => ApplyImage(image, age, size, false);
        public static void ApplyOnce(SpriteRenderer renderer, float age) => ApplyRenderer(renderer, age, false);

        static void ApplyImage(Image image, float age, float size, bool looping)
        {
            if (!image) return;
            var sprite = Frame(age, looping, out var flipX);
            if (!sprite) return;
            image.sprite = sprite;
            image.preserveAspect = true;
            // Keep a constant height. Edge-on slices retain their authored narrow width.
            var rect = image.rectTransform;
            float height = Mathf.Max(0, size);
            rect.sizeDelta = new Vector2(height * sprite.rect.width / Mathf.Max(1, sprite.rect.height), height);
            var scale = rect.localScale;
            scale.x = Mathf.Abs(scale.x) * (flipX ? -1 : 1);
            rect.localScale = scale;
        }

        static void ApplyRenderer(SpriteRenderer renderer, float age, bool looping)
        {
            if (!renderer) return;
            var sprite = Frame(age, looping, out var flipX);
            if (!sprite) return;
            renderer.sprite = sprite;
            renderer.flipX = flipX;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { loaded = false; System.Array.Clear(sprites, 0, sprites.Length); }
    }
}
