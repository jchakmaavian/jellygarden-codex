using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RoyalThrone.Presentation
{
    [RequireComponent(typeof(Button))]
    public sealed class RoyalButtonMotion : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
        IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        public float hoverScale = 1.025f;
        public float pressScale = .955f;
        // Rate of the exponential settle toward the target scale: 20 reaches 95% of the way in about 0.15 s.
        public float response = 22;
        Button button;
        bool pressed;
        bool hovered;
        bool selected;

        void Awake() => button = GetComponent<Button>();
        void OnDisable() { pressed = hovered = selected = false; transform.localScale = Vector3.one; }
        void Update()
        {
            bool usable = button != null && button.IsInteractable();
            float target = !usable ? 1 : pressed ? pressScale : hovered || selected ? hoverScale : 1;
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * target,
                1 - Mathf.Exp(-response * Time.unscaledDeltaTime));
        }
        public void OnPointerDown(PointerEventData e) { if (e.button == PointerEventData.InputButton.Left) pressed = true; }
        public void OnPointerUp(PointerEventData e) => pressed = false;
        public void OnPointerEnter(PointerEventData e) => hovered = true;
        public void OnPointerExit(PointerEventData e) { hovered = false; pressed = false; }
        public void OnSelect(BaseEventData e) => selected = true;
        public void OnDeselect(BaseEventData e) => selected = false;
    }
}
