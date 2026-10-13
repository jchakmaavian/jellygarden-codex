using JellyGarden.Scripts.Juice;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalThrone.Presentation
{
    /// <summary>Unscaled logo entrance and quiet jewel gleams; respects the existing home-motion setting.</summary>
    [DisallowMultipleComponent]
    public sealed class RoyalLogoMotion : MonoBehaviour
    {
        RectTransform logo;
        Image[] gleams;
        Sprite sparkle;
        float started;
        static readonly Vector2[] Places = { new Vector2(.49f, .87f), new Vector2(.24f, .53f), new Vector2(.78f, .28f) };
        void OnEnable()
        {
            logo = (RectTransform)transform;
            started = Time.unscaledTime;
            if (gleams != null) return;
            var texture = SparkleTextureFactory.FourPointSparkle;
            sparkle = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * .5f, texture.width);
            gleams = new Image[Places.Length];
            for (int i = 0; i < gleams.Length; i++)
            {
                var go = new GameObject("Royal Tales jewel gleam " + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(transform, false);
                var image = go.GetComponent<Image>(); image.sprite = sparkle; image.raycastTarget = false; image.color = Color.clear;
                var rt = image.rectTransform; rt.anchorMin = rt.anchorMax = Places[i]; rt.anchoredPosition = Vector2.zero;
                gleams[i] = image;
            }
        }
        void LateUpdate()
        {
            bool motion = RoyalThrone.Testing.RoyalSave.GetInt("RoyalThrone.HomeMotion", 1) != 0;
            float age = Time.unscaledTime - started;
            // A small overshoot into the final size, followed by an almost imperceptible breathing motion.
            float t = Mathf.Clamp01(age / .65f), u = t - 1;
            float settle = 1 + 2.3f * u * u * u + 1.3f * u * u;
            float scale = age < .65f ? Mathf.Lerp(.82f, 1, settle) : 1 + Mathf.Sin((age - .65f) * Mathf.PI * .5f) * .008f;
            logo.localScale = Vector3.one * (motion ? scale : 1);
            for (int i = 0; i < gleams.Length; i++)
            {
                float phase = Mathf.Repeat(age - .8f - i * .8f, 4.8f);
                float pulse = motion && age >= .8f + i * .8f && phase < .7f ? Mathf.Sin(phase / .7f * Mathf.PI) : 0;
                gleams[i].color = new Color(1, .96f, .76f, pulse * .85f);
                gleams[i].rectTransform.sizeDelta = Vector2.one * Mathf.Min(logo.rect.width, logo.rect.height) * .085f;
                gleams[i].rectTransform.localScale = Vector3.one * Mathf.Lerp(.55f, 1.1f, pulse);
                gleams[i].rectTransform.localRotation = Quaternion.Euler(0, 0, phase * 35);
            }
        }
        void OnDisable() { if (logo) logo.localScale = Vector3.one; }
        void OnDestroy() { if (sparkle) Destroy(sparkle); }
    }
}
