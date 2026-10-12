// The original Royal Throne gameplay backdrop behind the King Leo board.
//
// After royal-throne's RoyalGameplayHud.Backdrop: not a picture but a royal-blue
// gradient, brightest down the middle and towards the top, deep navy at the
// sides. A small bilinear texture stretched over the whole screen, at full
// brightness. StoryBoardTheme shows it in place of the storybook courtyard; with
// the switch off the courtyard comes back exactly as before.

using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    public static class StoryOriginalBackdrop
    {
        public static bool Enabled
        {
            get
            {
                if (_enabled.HasValue) return _enabled.Value;
                _enabled = StoryVfxFlags.On("-originalbackdrop", "StoryVfx.OriginalBackdrop");
                return _enabled.Value;
            }
            set { _enabled = value; }
        }
        private static bool? _enabled;

        /// <summary>The colour royal-throne clears its camera to under the backdrop.</summary>
        public static readonly Color Navy = new Color(.047f, .208f, .49f);

        static Sprite _sprite;

        /// <summary>The gradient, built once (royal-throne's exact colours and falloff).</summary>
        public static Sprite Sprite
        {
            get
            {
                if (_sprite != null) return _sprite;
                const int w = 96, h = 192;
                var texture = new Texture2D(w, h, TextureFormat.RGBA32, false)
                {
                    name = "Royal Throne backdrop", wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave
                };
                Color centreTop = Hex(0x2b79da), centreBottom = Hex(0x1964c5), edgeTop = Hex(0x11418e), edgeBottom = Hex(0x0c357d);
                var pixels = new Color[w * h];
                for (int y = 0; y < h; y++)
                {
                    float v = y / (h - 1f);
                    Color centre = Color.Lerp(centreBottom, centreTop, v), edge = Color.Lerp(edgeBottom, edgeTop, v);
                    for (int x = 0; x < w; x++)
                    {
                        float d = (x + .5f) / w - .5f, f = Mathf.Exp(-(d * d) / (2 * .235f * .235f));
                        pixels[y * w + x] = Color.Lerp(edge, centre, f);
                    }
                }
                texture.SetPixels(pixels);
                texture.Apply(false, true);
                _sprite = Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(.5f, .5f), w, 0, SpriteMeshType.FullRect);
                _sprite.name = "Royal Throne backdrop";
                _sprite.hideFlags = HideFlags.DontSave;
                return _sprite;
            }
        }

        /// <summary>
        /// Puts the gradient on the kit's Background image, stretched edge to edge
        /// over its parent the way royal-throne stretches it over the camera view.
        /// </summary>
        public static void Show(Image image)
        {
            image.sprite = Sprite;
            image.raycastTarget = false;
            image.preserveAspect = false;
            image.color = Color.white;
            StorybookBoardLayout.DressBackdrop(image);

            var fitter = image.GetComponent<AspectRatioFitter>();
            if (fitter != null) fitter.enabled = false;
            var rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(.5f, .5f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            var cam = Camera.main;
            if (cam != null) cam.backgroundColor = Navy;
        }

        static Color Hex(int rgb) => new Color((rgb >> 16 & 255) / 255f, (rgb >> 8 & 255) / 255f, (rgb & 255) / 255f, 1f);
    }
}
