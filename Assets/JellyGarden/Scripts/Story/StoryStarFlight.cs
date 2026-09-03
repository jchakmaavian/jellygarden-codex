// The star leaving the pile — the one moment the price of a repair is visible
// rather than read.
//
// It lives on a canvas of its own rather than on whichever screen spent the
// star, because that screen is usually about to go: a repair takes the area
// page down, counter included, in the same frame the star is spent. A star
// parented to it would disappear on the first frame of its own flight.
//
// The two ends are screen positions, taken by the caller before anything is
// hidden. That is the whole seam: this class never looks at a rect belonging to
// a screen that may not survive the flight.

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    public sealed class StoryStarFlight : MonoBehaviour
    {
        // Above the repair animation (780), because the star is spent on the
        // beat the repair starts and would otherwise fly behind it.
        private const int SortingOrder = 800;

        private const float StarSize = 96f;

        private static StoryStarFlight _instance;

        private Canvas _canvas;

        /// <summary>
        /// Throws a star from <paramref name="from"/> to <paramref name="to"/>,
        /// both in screen points. Silently does nothing outside play mode, where
        /// there is no frame loop to fly it on.
        /// </summary>
        public static void Play(Vector2 from, Vector2 to, float seconds, float arc)
        {
            if (!Application.isPlaying) return;

            Ensure();
            _instance.StartCoroutine(_instance.Fly(from, to, seconds, arc));
        }

        private static void Ensure()
        {
            if (_instance != null) return;

            var go = new GameObject("~StoryStarFlight");
            StoryObjects.Persist(go);
            _instance = go.AddComponent<StoryStarFlight>();
            _instance.Build();
        }

        private void Build()
        {
            _canvas = StoryUi.CreateOverlayCanvas("StarFlightCanvas", SortingOrder);
            _canvas.transform.SetParent(transform, false);
        }

        private IEnumerator Fly(Vector2 from, Vector2 to, float seconds, float arc)
        {
            var space = _canvas.transform as RectTransform;

            Vector2 start = LocalPoint(space, from);
            Vector2 end = LocalPoint(space, to);

            Sprite art = StoryArt.Load("icon_star");
            Image star = StoryUi.CreateImage("SpentStar", space,
                art != null ? art : StoryUi.StarSprite,
                art != null ? Color.white : StoryUi.Gold);
            star.preserveAspect = true;
            star.raycastTarget = false;

            RectTransform rect = star.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(StarSize, StarSize);

            float lift = (end - start).magnitude * arc;
            float began = StoryClock.Now;
            float t = 0f;

            while (t < 1f)
            {
                t = Mathf.Clamp01((StoryClock.Now - began) / Mathf.Max(0.01f, seconds));

                // Ease out, so it leaves the button fast and arrives softly.
                float e = 1f - (1f - t) * (1f - t);

                Vector2 at = Vector2.Lerp(start, end, e);
                at.y += Mathf.Sin(t * Mathf.PI) * lift;

                rect.anchoredPosition = at;
                rect.localScale = Vector3.one * Mathf.Lerp(1f, 0.55f, e);
                rect.localRotation = Quaternion.Euler(0f, 0f, -180f * e);

                // It only fades at the very end. Fading across the whole flight
                // reads as the star being taken away rather than banked.
                float alpha = t < 0.85f ? 1f : 1f - (t - 0.85f) / 0.15f;
                star.color = new Color(star.color.r, star.color.g, star.color.b, alpha);

                yield return null;
            }

            Destroy(star.gameObject);
        }

        private static Vector2 LocalPoint(RectTransform space, Vector2 screen)
        {
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                space, screen, null, out local);
            return local;
        }
    }
}
