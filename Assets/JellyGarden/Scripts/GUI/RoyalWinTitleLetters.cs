using UnityEngine;
using UnityEngine.UI;

namespace RoyalThrone.Presentation
{
    // The completion title: the whole-word "Well Done!" art (one continuous gold outline, navy keyline, ivory face) bent
    // into the reference's gentle arch by RoyalTitleArch, revealed with a single pop.
    public sealed class RoyalWinTitleLetters : MonoBehaviour
    {
        // Visible art inside well-done-art.png (2170 x 725), as a UV crop with a few pixels of clear margin.
        static readonly Rect Crop = new Rect(24 / 2170f, 98 / 725f, 2123 / 2170f, 544 / 725f);
        const float Aspect = 2123f / 544f, Arch = .10f;
        // Reveal in RoyalWinCard's letter units (0.04 s each): 0.48 s, well inside the card's title span.
        const float Span = 12;
        // The art's top edge sits where the earlier lettering's did, keeping the crown clearance.
        const float Top = 44;
        RawImage art;
        public RectTransform RectTransform => (RectTransform)transform;
        public RawImage Art => art;
        public bool Ready { get; private set; }
        float progress = float.NaN;

        public bool Build()
        {
            if (Ready) return true;
            var texture = Resources.Load<Texture2D>("RoyalGameplay/Win/well-done-art");
            if (!texture) return false;
            var go = new GameObject("Well Done art", typeof(RectTransform), typeof(RawImage), typeof(RoyalTitleArch));
            go.transform.SetParent(transform, false);
            art = go.GetComponent<RawImage>();
            art.texture = texture; art.uvRect = Crop; art.raycastTarget = false;
            go.GetComponent<RoyalTitleArch>().angle = Arch;
            var rect = art.rectTransform; float width = RectTransform.rect.width, height = width / Aspect;
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(0, Top - height * .5f);
            Ready = true; Write(0); return true;
        }

        public void Write(float value)
        {
            if (!Ready || progress == value) return;
            progress = value;
            float u = Mathf.Clamp01(value / Span);
            art.enabled = u > 0;
            if (!art.enabled) return;
            // Grow past full size, dip under it, then settle: one confident stamp rather than a letter ripple.
            float scale = u < .40f ? Mathf.Lerp(.35f, 1.12f, Smooth(u / .40f))
                : u < .75f ? Mathf.Lerp(1.12f, .97f, Smooth((u - .40f) / .35f))
                : Mathf.Lerp(.97f, 1, Smooth((u - .75f) / .25f));
            art.rectTransform.localScale = new Vector3(scale, scale, 1);
            art.color = new Color(1, 1, 1, Smooth(Mathf.Clamp01(u / .20f)));
        }

        static float Smooth(float u) => u * u * (3 - 2 * u);
    }
}
