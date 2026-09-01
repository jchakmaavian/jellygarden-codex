// A short line of text at the bottom of the screen, used for the things that
// need saying but not a screen: "restore the gates first", "one more star
// needed", and the line a repair task prints when it lands.

using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    public sealed class StoryToast : MonoBehaviour
    {
        private const float DefaultDuration = 2.6f;
        private const float FadeTime = 0.35f;

        private static StoryToast _instance;

        private CanvasGroup _group;
        private Text _text;
        private float _hideAt;

        public static void Show(string message, float duration = DefaultDuration)
        {
            if (string.IsNullOrEmpty(message)) return;
            Ensure();
            _instance.Display(message, duration);
        }

        private static void Ensure()
        {
            if (_instance != null) return;

            var go = new GameObject("~StoryToast");
            StoryObjects.Persist(go);
            _instance = go.AddComponent<StoryToast>();
            _instance.Build();
        }

        private void Build()
        {
            Canvas canvas = StoryUi.CreateOverlayCanvas("ToastCanvas", 900);
            canvas.transform.SetParent(transform, false);

            RectTransform holder = StoryUi.CreateRect("Toast", canvas.transform);
            StoryUi.BottomAnchored(holder, 220f, 130f, 70f);

            _group = holder.gameObject.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _group.interactable = false;
            _group.alpha = 0f;

            Image backdrop = StoryUi.CreateImage("Backdrop", holder, StoryUi.CloudSprite,
                StoryUi.CloudSprite != null ? Color.white : new Color(0f, 0f, 0f, 0.75f));
            StoryUi.Stretch(backdrop.rectTransform);

            _text = StoryUi.CreateText("Text", holder, "", 38, StoryUi.Ink,
                TextAnchor.MiddleCenter);
            StoryUi.Stretch(_text.rectTransform, 24f);
        }

        private void Display(string message, float duration)
        {
            _text.text = message;
            _group.alpha = 1f;
            _hideAt = StoryClock.Now + duration;
        }

        private void Update()
        {
            if (_group == null || _group.alpha <= 0f) return;

            float remaining = _hideAt - StoryClock.Now;
            if (remaining <= 0f)
                _group.alpha = 0f;
            else if (remaining < FadeTime)
                _group.alpha = remaining / FadeTime;
        }
    }
}
