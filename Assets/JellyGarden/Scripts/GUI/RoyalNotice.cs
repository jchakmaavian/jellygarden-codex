using UnityEngine;
using UnityEngine.UI;

namespace RoyalThrone.Presentation
{
    // A short message near the control that caused it (UI polish, 2026-09-30), such as "Not enough coins". A navy
    // pill with a thin gold edge and white text. It fades in over 0.18 s, rising 6 units, holds, and fades out over 0.25 s,
    // on the unscaled clock, and never takes a tap. One notice at a time: a new one replaces the one showing. When the control it
    // points at goes away (its card closed), it leaves with it in 0.12 s, so it never lingers over the next card.
    public sealed class RoyalNotice : MonoBehaviour
    {
        const float In = .18f, Out = .25f, Leave = .12f, Rise = 6, Hold = 1.8f;
        static RoyalNotice current;
        CanvasGroup group;
        RectTransform rect;
        Transform source;
        Vector2 rest;
        float age, life;
        bool leaving;

        /// <summary>Not enough coins for a price: the player's live balance, and how coins are earned.</summary>
        public static void NotEnoughCoins(Transform near, int coins) =>
            Show(near, "Not enough coins (you have " + coins.ToString("N0") + ")\nWin levels to earn more");

        /// <summary>Shows `message` just above `near` (a control), scaled like its canvas.</summary>
        public static void Show(Transform near, string message, float hold = Hold)
        {
            var canvas = near ? near.GetComponentInParent<Canvas>() : null;
            if (!canvas) return;
            if (current) Destroy(current.gameObject);
            // Its own overlay canvas, not a child of the popups' canvas: every active child there counts as an open popup, which
            // would block the board and hold Back while the notice shows. Scaled like the control's canvas.
            var holder = new GameObject("Royal notice", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var own = holder.GetComponent<Canvas>(); own.renderMode = RenderMode.ScreenSpaceOverlay; own.sortingOrder = 900;
            var source = canvas.rootCanvas.GetComponent<CanvasScaler>(); var scaler = holder.GetComponent<CanvasScaler>();
            if (source) { scaler.uiScaleMode = source.uiScaleMode; scaler.referenceResolution = source.referenceResolution; scaler.screenMatchMode = source.screenMatchMode; scaler.matchWidthOrHeight = source.matchWidthOrHeight; scaler.scaleFactor = source.scaleFactor; }
            var root = (RectTransform)holder.transform;
            var go = new GameObject("Royal notice card", typeof(RectTransform), typeof(CanvasGroup));
            var notice = holder.AddComponent<RoyalNotice>();
            current = notice;
            notice.rect = (RectTransform)go.transform;
            notice.source = near;
            notice.rect.SetParent(root, false);
            notice.group = go.GetComponent<CanvasGroup>(); notice.group.blocksRaycasts = false; notice.group.interactable = false;
            Canvas.ForceUpdateCanvases();
            // Above the control (its top edge on screen), kept on the screen.
            var corners = new Vector3[4]; ((RectTransform)near).GetWorldCorners(corners);
            var eye = canvas.rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.rootCanvas.worldCamera;
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(eye, (corners[1] + corners[2]) * .5f);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, null, out var top);
            const float w = 560, h = 104;
            var r = root.rect;
            float x = Mathf.Clamp(top.x, r.xMin + w * .5f + 12, r.xMax - w * .5f - 12), y = Mathf.Min(top.y + h * .5f + 16, r.yMax - h * .5f - 12);
            RoyalMenuSkin.Place(notice.rect, x, y, w, h);
            notice.rest = notice.rect.anchoredPosition;
            var shadow = Shape(notice.rect, "Shadow", 0, -4, w + 10, h + 10, h * .5f + 5, new Color(0, .02f, .1f, .45f), new Color(0, .02f, .1f, .45f)); shadow.softness = 12;
            Shape(notice.rect, "Edge", 0, 0, w, h, h * .5f, Hex("ffe27a"), Hex("d99a22"));
            Shape(notice.rect, "Face", 0, 0, w - 5, h - 5, (h - 5) * .5f, Hex("163f97"), Hex("0b2a6e"));
            var text = RoyalMenuSkin.Label(notice.rect, "Message", message, 0, 0, w - 40, h - 14, 28);
            text.resizeTextMinSize = 18;
            foreach (var s in text.GetComponents<Shadow>()) s.enabled = false;
            var style = text.gameObject.AddComponent<RoyalTextStyle>();
            style.Set(Color.white, Hex("eef3ff"), Color.clear, 0, Color.clear, 0, new Color(0, .04f, .2f, .6f), new Vector2(0, -1.5f));
            notice.life = In + hold + Out;
            notice.Pose();
        }

        void Update()
        {
            age += Time.unscaledDeltaTime;
            // The control is gone (its card closed): leave with it, quickly, from the current alpha.
            if (!leaving && (!source || !source.gameObject.activeInHierarchy)) leaving = true;
            if (leaving) { group.alpha = Mathf.MoveTowards(group.alpha, 0, Time.unscaledDeltaTime / Leave); if (group.alpha <= 0) Destroy(gameObject); return; }
            Pose();
            if (age >= life) Destroy(gameObject);
        }

        void OnDestroy() { if (current == this) current = null; }

        void Pose()
        {
            float a = age < In ? EaseOut(age / In) : age > life - Out ? 1 - EaseOut((age - (life - Out)) / Out) : 1;
            group.alpha = a;
            rect.anchoredPosition = rest + new Vector2(0, Rise * (age < In ? EaseOut(age / In) - 1 : 0));
        }

        static float EaseOut(float x) { x = Mathf.Clamp01(x); float u = 1 - x; return 1 - u * u * u; }

        static RoyalCardShape Shape(Transform parent, string name, float x, float y, float w, float h, float radius, Color from, Color to)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var shape = go.AddComponent<RoyalCardShape>();
            shape.raycastTarget = false; shape.from = from; shape.to = to; shape.radius = radius;
            RoyalMenuSkin.Place(go.transform, x, y, w, h);
            return shape;
        }

        static Color Hex(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out var c); return c; }
    }
}
