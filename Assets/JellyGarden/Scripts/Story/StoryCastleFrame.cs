using UnityEngine;

namespace JellyGarden.Scripts.Story
{
    /// <summary>
    /// A 360-point-wide layout inside the device safe area. Height stays flexible;
    /// short screens scale the controls together instead of stacking them up.
    /// The room itself stays outside this frame and covers the entire screen.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StoryCastleFrame : MonoBehaviour
    {
        private RectTransform _rect;
        private RectTransform _parent;
        private Vector2 _lastSize;
        private Rect _lastSafe;
        private Vector2 _lastScreen;

        public static RectTransform Create(Transform parent)
        {
            RectTransform rect = StoryUi.CreateRect("CastleSafeFrame", parent);
            rect.gameObject.AddComponent<StoryCastleFrame>().Fit();
            return rect;
        }

        private void LateUpdate() { Fit(); }

        public void Fit()
        {
            Fit(Screen.safeArea, new Vector2(Screen.width, Screen.height));
        }

        // Also used by layout checks with a simulated notch and home indicator.
        public void Fit(Rect safe, Vector2 screen)
        {
            if (_rect == null) _rect = (RectTransform)transform;
            if (_parent == null) _parent = transform.parent as RectTransform;
            if (_parent == null || screen.x <= 0 || screen.y <= 0) return;
            Vector2 size = _parent.rect.size;
            if (size.x <= 0 || size.y <= 0) return;
            if (safe.width <= 0 || safe.height <= 0) safe = new Rect(Vector2.zero, screen);
            if (_lastSize == size && _lastSafe == safe && _lastScreen == screen) return;

            Vector2 available = Vector2.Scale(size,
                new Vector2(safe.width / screen.x, safe.height / screen.y));
            float scale = Mathf.Min(available.x / 360f, available.y / 640f);
            Vector2 center = new Vector2(safe.center.x / screen.x, safe.center.y / screen.y);
            _rect.anchorMin = _rect.anchorMax = center;
            _rect.pivot = new Vector2(0.5f, 0.5f);
            _rect.anchoredPosition = Vector2.zero;
            _rect.sizeDelta = new Vector2(360f, available.y / scale);
            _rect.localScale = Vector3.one * scale;
            _lastSize = size;
            _lastSafe = safe;
            _lastScreen = screen;
        }
    }
}
