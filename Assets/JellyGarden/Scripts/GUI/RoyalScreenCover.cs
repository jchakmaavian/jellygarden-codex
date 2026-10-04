using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalThrone.Presentation
{
    // The level start's cover (2026-10-04, the user: the game start transition must be smooth). PLAY on the Level Start card
    // used to cut in one frame from the card over Home to the goals banner. Now a full-screen cover of the gameplay's
    // royal-blue backdrop fades in over the card and Home, the level is switched in underneath it (`midpoint`), and the
    // cover then lifts off the goals banner, which sits on the same backdrop, so only the banner is seen arriving.
    // The cover takes every tap while it is up.
    public sealed class RoyalScreenCover : MonoBehaviour
    {
        public const float FadeIn = .24f, Hold = .05f, FadeOut = .28f;
        static RoyalScreenCover instance;
        CanvasGroup group;
        bool running;
        /// <summary>A cover is fading in, holding or lifting.</summary>
        public static bool Busy => instance && instance.running;
        /// <summary>The cover's opacity (0 when it is not shown).</summary>
        public static float Opacity => instance && instance.group ? instance.group.alpha : 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { instance = null; }

        public static bool Play(Action midpoint)
        {
            if (Busy) return false;
            if (!instance) Build();
            instance.gameObject.SetActive(true);   // the coroutine needs an active object; it is switched off again when done
            instance.StartCoroutine(instance.Run(midpoint));
            return true;
        }

        static void Build()
        {
            var go = new GameObject("Royal level start cover", typeof(RectTransform));
            DontDestroyOnLoad(go);
            var canvas = go.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
            go.AddComponent<GraphicRaycaster>();
            instance = go.AddComponent<RoyalScreenCover>();
            instance.group = go.AddComponent<CanvasGroup>(); instance.group.alpha = 0; instance.group.blocksRaycasts = false;
            var art = new GameObject("Royal-blue backdrop", typeof(RectTransform)).GetComponent<RectTransform>();
            art.SetParent(go.transform, false); art.anchorMin = Vector2.zero; art.anchorMax = Vector2.one; art.offsetMin = art.offsetMax = Vector2.zero;
            var image = art.gameObject.AddComponent<Image>(); image.sprite = RoyalGameplayHud.Backdrop(); image.raycastTarget = true;
            go.SetActive(false);
        }

        IEnumerator Run(Action midpoint)
        {
            running = true; group.blocksRaycasts = true;
            try
            {
                // Fades over the card and Home: smoothstep, so it neither pops nor lingers.
                for (float t = 0; t < FadeIn; t += Mathf.Min(Time.unscaledDeltaTime, .05f)) { group.alpha = Mathf.SmoothStep(0, 1, t / FadeIn); yield return null; }
                group.alpha = 1;
                try { midpoint?.Invoke(); } catch (Exception e) { Debug.LogException(e); }
                // The switch's heavier frames (the level's start) stay under the cover.
                yield return null; yield return null;
                for (float t = 0; t < Hold; t += Time.unscaledDeltaTime) yield return null;
                for (float t = 0; t < FadeOut; t += Mathf.Min(Time.unscaledDeltaTime, .05f)) { group.alpha = 1 - Mathf.SmoothStep(0, 1, t / FadeOut); yield return null; }
            }
            finally { group.alpha = 0; group.blocksRaycasts = false; running = false; gameObject.SetActive(false); }
        }
    }
}
