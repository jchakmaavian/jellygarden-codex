using UnityEngine;
using UnityEngine.EventSystems;

namespace RoyalThrone.Presentation
{
    // Reports whether a control is held down with the pointer still over it, for press feedback drawn elsewhere
    // (RoyalLevelCardMotion). The control's Button keeps receiving the same events and still does the clicking.
    public sealed class RoyalPressTracker : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
    {
        bool down, inside;
        // Unscaled time of the latest press, or -1.
        public float PressedAt { get; private set; } = -1;
        public bool Held => down && inside;

        public void OnPointerDown(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left) return;
            down = inside = true;
            PressedAt = Time.unscaledTime;
        }
        public void OnPointerUp(PointerEventData e) => down = false;
        public void OnPointerEnter(PointerEventData e) => inside = true;
        public void OnPointerExit(PointerEventData e) => inside = false;
        void OnDisable() { down = inside = false; PressedAt = -1; }
    }
}
