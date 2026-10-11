using UnityEngine;
using UnityEngine.UI;
using JellyGarden.Scripts.Juice;

namespace RoyalThrone.Presentation
{
    // Presentation only: the area view owns collection and navigation.
    public sealed class RoyalAreaChestAnimation : MonoBehaviour
    {
        Image chest, rays;
        Sprite[] frames; bool idle;
        public bool Opened { get; private set; }
        public float Elapsed { get; private set; }
        public void Prepare(Image target)
        {
            chest = target;
            var texture = Resources.Load<Texture2D>("RoyalGameplay/area-chest-opening");
            if (texture && frames == null)
            {
                frames = new Sprite[4];
                int w = texture.width / 2, h = texture.height / 2;
                for (int i = 0; i < 4; i++)
                    frames[i] = Sprite.Create(texture, new Rect((i % 2) * w, (1 - i / 2) * h, w, h), new Vector2(.5f, .5f), 100);
            }
            var parent = (RectTransform)chest.transform.parent;
            if (!rays)
            {
                rays = RoyalAreaKit.Picture("Opening gold rays", parent, RoyalPieceSkin.Art("reward-rays"), 369, 938, 550, 550);
                rays.transform.SetSiblingIndex(chest.transform.GetSiblingIndex());
            }
            chest.rectTransform.sizeDelta = new Vector2(320, 320);
            ResetPose();
        }
        public void ResetPose()
        {
            Opened = false; Elapsed = 0; idle = false;
            if (frames != null) chest.sprite = frames[0];
            if (rays) rays.enabled = false;
            if (chest) { chest.color = Color.white; chest.rectTransform.localRotation = Quaternion.identity; }
        }
        public void Pose(float elapsed, bool motion)
        {
            Elapsed = elapsed; idle = elapsed >= 1.4f; Opened = elapsed >= .34f;
            float p = motion ? Mathf.Clamp01((elapsed - .3f) / .36f) * 3 : 3;
            // Use opaque authored poses to keep one clear lid silhouette.
            if (frames != null) chest.sprite = frames[Mathf.RoundToInt(p)];
            float shake = motion && elapsed < .3f ? Mathf.Sin(elapsed * 85) * 3.5f * Mathf.Sin(elapsed / .3f * Mathf.PI) : 0;
            chest.rectTransform.localRotation = Quaternion.Euler(0, 0, shake);
            chest.rectTransform.localScale = Vector3.one * (1 + (motion ? .065f * Mathf.Sin(Mathf.Clamp01((elapsed - .3f) / .36f) * Mathf.PI) : 0));
            rays.enabled = Opened || !motion;
            rays.color = new Color(1, .86f, .45f, .35f * Mathf.Clamp01((elapsed - .34f) / .25f));
            rays.rectTransform.localRotation = Quaternion.Euler(0, 0, motion ? elapsed * 12 : 0);
            rays.rectTransform.localScale = Vector3.one * (.82f + .18f * Mathf.Clamp01((elapsed - .34f) / .35f));
        }
        public void Finish() { idle = false; if (rays) rays.enabled = false; }
        void Update()
        {
            if (idle) Pose(Elapsed + Time.unscaledDeltaTime, RoyalThrone.Testing.RoyalSave.GetInt("RoyalThrone.HomeMotion", 1) != 0);
        }
        void OnDestroy()
        {
            if (frames != null) foreach (var sprite in frames) if (sprite) Destroy(sprite);
        }
    }
}
