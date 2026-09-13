using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    /// <summary>Production RGBA layers, packaged from the approved concept and its cleared surfaces.</summary>
    public static class StorybookLayers
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        public static Sprite Get(string name)
        {
            if (Cache.TryGetValue(name, out Sprite sprite) && sprite != null) return sprite;
            sprite = Resources.Load<Sprite>("Story/TreasureHunt/UI/Layers/" + name);
            if (sprite != null) Cache[name] = sprite;
            return sprite;
        }

        public static Image Image(string name, Transform parent, string layer)
        {
            var sprite = Get(layer);
            if (sprite == null) Debug.LogError("[Storybook] Missing production layer: " + layer);
            var image = StoryUi.CreateImage(name, parent, sprite, Color.white);
            image.raycastTarget = false;
            return image;
        }

        public static Image Sliced(string name, Transform parent, string layer)
        {
            var image = Image(name, parent, layer);
            image.type = UnityEngine.UI.Image.Type.Sliced;
            image.gameObject.AddComponent<StorybookLayerScale>();
            return image;
        }
    }

    /// <summary>Keep cap proportions at every canvas scale, without a second gradient or rim.</summary>
    public sealed class StorybookLayerScale : MonoBehaviour
    {
        private Image _image;
        private void LateUpdate()
        {
            if (_image == null) _image = GetComponent<Image>();
            if (_image.sprite == null || _image.rectTransform.rect.height <= 0) return;
            _image.pixelsPerUnitMultiplier = Mathf.Max(.01f,
                _image.sprite.rect.height / _image.rectTransform.rect.height);
        }
    }
}
