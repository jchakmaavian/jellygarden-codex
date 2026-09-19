using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    // Small nine-sliced enamel surfaces. Text, counts and icons stay live UI.
    public static class StoryRoyalChrome
    {
        public enum Finish { Blue, Ivory, Green }
        static readonly Dictionary<int, Sprite> Sprites = new Dictionary<int, Sprite>();
        public static readonly Color Ink = new Color32(14, 44, 84, 255);
        public static readonly Color Paper = new Color32(255, 244, 202, 255);

        public static Image Plate(string name, Transform parent, Finish finish = Finish.Blue, bool circle = false)
        {
            var image = StoryUi.CreateImage(name, parent, Surface(finish, circle), Color.white);
            image.type = circle ? Image.Type.Simple : Image.Type.Sliced;
            image.raycastTarget = false;
            return image;
        }

        static Sprite Surface(Finish finish, bool circle)
        {
            int key = (int)finish * 2 + (circle ? 1 : 0);
            if (Sprites.TryGetValue(key, out var sprite) && sprite != null) return sprite;
            const int n = 128;
            var texture = new Texture2D(n, n, TextureFormat.RGBA32, false)
            { name = "Royal enamel " + finish, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    Vector2 p = new Vector2(x - 63.5f, y - 63.5f);
                    Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - Vector2.one * 43f;
                    float d = circle ? p.magnitude - 62f :
                        new Vector2(Mathf.Max(q.x, 0), Mathf.Max(q.y, 0)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0) - 19f;
                    float inside = -d;
                    float t = y / 127f;
                    Color face = finish == Finish.Ivory
                        ? Color.Lerp(new Color32(248, 220, 161, 255), new Color32(255, 254, 230, 255), t)
                        : finish == Finish.Green
                            ? Color.Lerp(new Color32(24, 118, 15, 255), new Color32(131, 233, 48, 255), t)
                            : Color.Lerp(new Color32(5, 43, 133, 255), new Color32(12, 132, 242, 255), t);
                    Color c;
                    if (inside < 1.8f) c = new Color32(119, 64, 21, 255);
                    else if (inside < 4.1f) c = Color.Lerp(new Color32(235, 153, 30, 255), new Color32(255, 242, 150, 255), t);
                    else if (inside < 5.7f) c = new Color32(255, 249, 186, 255);
                    else if (inside < 8.5f) c = Color.Lerp(new Color32(211, 122, 19, 255), new Color32(255, 207, 70, 255), t);
                    else if (inside < 10.4f) c = finish == Finish.Ivory ? new Color32(183, 131, 54, 255) : Ink;
                    else
                    {
                        float bevel = Mathf.Clamp01((14f - inside) / 3f) * .25f;
                        c = Color.Lerp(face, t > .5f ? Color.white : Ink, bevel);
                    }
                    c.a = Mathf.Clamp01(.5f - d);
                    pixels[y * n + x] = c;
                }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            sprite = Sprite.Create(texture, new Rect(0, 0, n, n), Vector2.one * .5f, 100, 0,
                SpriteMeshType.FullRect, circle ? Vector4.zero : Vector4.one * 26);
            sprite.name = "Royal " + finish + (circle ? " badge" : " panel");
            Sprites[key] = sprite;
            return sprite;
        }
    }
}
