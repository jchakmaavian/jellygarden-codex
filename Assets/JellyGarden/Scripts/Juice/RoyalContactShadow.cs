using UnityEngine;

namespace JellyGarden.Scripts.Juice
{
    /// <summary>
    /// The soft ellipse every board piece sits on. Pieces were drawn straight onto the flat cells
    /// with nothing tying them to the board, so they read as stickers rather than objects. One
    /// generated texture is shared by every piece, so the shadows batch into a single draw call.
    /// </summary>
    public static class RoyalContactShadow
    {
        const int Size = 64;

        static Sprite blob;
        static Material material;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { blob = null; material = null; }

        /// <summary>A radial falloff with no hard edge, built once and kept for the session.</summary>
        public static Sprite Blob
        {
            get
            {
                if (blob) return blob;
                var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
                {
                    name = "Royal contact shadow",
                    hideFlags = HideFlags.DontSave,
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear
                };
                var pixels = new Color32[Size * Size];
                float centre = (Size - 1) * .5f;
                for (int y = 0; y < Size; y++)
                    for (int x = 0; x < Size; x++)
                    {
                        float dx = (x - centre) / centre, dy = (y - centre) / centre;
                        float r = Mathf.Sqrt(dx * dx + dy * dy);
                        // Squared falloff: dense in the middle, gone well before the edge.
                        float a = Mathf.Clamp01(1f - r);
                        a *= a;
                        pixels[y * Size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                    }
                texture.SetPixels32(pixels);
                texture.Apply(false, false);
                blob = Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(.5f, .5f), Size,
                    0, SpriteMeshType.FullRect);
                blob.name = "Royal contact shadow";
                blob.hideFlags = HideFlags.DontSave;
                return blob;
            }
        }

        /// <summary>One shared material, so every piece's shadow batches together.</summary>
        public static Material Material
        {
            get
            {
                if (material) return material;
                material = new Material(Shader.Find("Sprites/Default"))
                {
                    name = "Royal contact shadow",
                    hideFlags = HideFlags.DontSave
                };
                return material;
            }
        }
    }
}
