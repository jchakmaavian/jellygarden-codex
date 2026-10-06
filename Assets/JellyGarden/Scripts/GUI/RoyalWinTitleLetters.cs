using UnityEngine;
using UnityEngine.UI;

namespace RoyalThrone.Presentation
{
    // Each generated glyph owns its complete bevel and extrusion, including hidden side edges.
    public sealed class RoyalWinTitleLetters : MonoBehaviour
    {
        [System.Serializable] public sealed class Crop { public float x, y, width, height; }
        [System.Serializable] public sealed class Glyph { public string name; public Crop uv; }
        [System.Serializable] public sealed class Atlas { public Glyph[] glyphs; }
        static readonly string[] Names = { "W", "e", "l", "l", "D", "o", "n", "e", "bang" };
        static readonly int[] Beats = { 0, 1, 2, 3, 5, 6, 7, 8, 9 };
        // Match the reference's fuller lowercase and closely joined gold word silhouettes.
        static readonly float[] Heights = { .98f, .82f, 1, 1, 1, .82f, .84f, .82f, 1 };
        static readonly float[] Spacing = { -12, -14, -12, 10, -13, -12, -13, -13 };
        readonly RawImage[] letters = new RawImage[9];
        public RectTransform RectTransform => (RectTransform)transform;
        public RawImage[] Letters => letters;
        public bool Ready { get; private set; }
        float progress = float.NaN;

        public bool Build()
        {
            if (Ready) return true;
            var data = Resources.Load<TextAsset>("RoyalGameplay/Win/Letters/glyphs");
            if (!data) return false;
            var atlas = JsonUtility.FromJson<Atlas>(data.text);
            var textures = new Texture2D[9]; var uvs = new Rect[9]; var widths = new float[9];
            float total = 0;
            foreach (float space in Spacing) total += space;
            for (int i = 0; i < Names.Length; i++)
            {
                var glyph = System.Array.Find(atlas.glyphs, g => g.name == Names[i]);
                textures[i] = Resources.Load<Texture2D>("RoyalGameplay/Win/Letters/" + Names[i]);
                if (glyph == null || !textures[i] || glyph.uv == null || glyph.uv.width <= 0 || glyph.uv.height <= 0) return false;
                uvs[i] = new Rect(glyph.uv.x, glyph.uv.y, glyph.uv.width, glyph.uv.height);
                widths[i] = Heights[i] * 100 * textures[i].width * glyph.uv.width / (textures[i].height * glyph.uv.height);
                total += widths[i];
            }
            float fit = RectTransform.rect.width / total, cursor = -RectTransform.rect.width * .5f;
            for (int i = 0; i < Names.Length; i++)
            {
                var go = new GameObject("Letter " + i + " " + Names[i], typeof(RectTransform), typeof(RawImage));
                go.transform.SetParent(transform, false);
                var image = go.GetComponent<RawImage>(); letters[i] = image;
                image.texture = textures[i]; image.uvRect = uvs[i]; image.raycastTarget = false;
                var rect = image.rectTransform; rect.pivot = new Vector2(.5f, 0);
                rect.sizeDelta = new Vector2(widths[i], Heights[i] * 100) * fit;
                rect.anchoredPosition = new Vector2(cursor + rect.sizeDelta.x * .5f, -45);
                cursor += rect.sizeDelta.x + (i < Spacing.Length ? Spacing[i] * fit : 0);
            }
            Ready = true; Write(0); return true;
        }

        public void Write(float value)
        {
            if (!Ready || progress == value) return;
            progress = value;
            for (int i = 0; i < letters.Length; i++)
            {
                float u = Mathf.Clamp01((value - Beats[i]) / 10f);
                var image = letters[i]; image.enabled = u > 0;
                if (!image.enabled) continue;
                float scale = u < .25f ? Mathf.Lerp(.28f, 1.62f, Smooth(u / .25f))
                    : u < .75f ? Mathf.Lerp(1.62f, .94f, Smooth((u - .25f) / .50f))
                    : Mathf.Lerp(.94f, 1, Smooth((u - .75f) / .25f));
                image.rectTransform.localScale = new Vector3(scale, scale, 1);
                image.rectTransform.localRotation = Quaternion.Euler(0, 0, (i % 2 == 0 ? -5 : 5) * Mathf.Sin(Mathf.PI * u) * (1 - u));
                image.color = Color.white;
            }
        }

        static float Smooth(float u) => u * u * (3 - 2 * u);
    }
}
