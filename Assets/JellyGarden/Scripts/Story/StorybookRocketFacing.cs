using UnityEngine;

namespace JellyGarden.Scripts.Story
{
    // The sprite is a child of Item. Its facing is independent of the root's
    // swap/fall/destruction animation, and communicates the special's clear axis.
    public sealed class StorybookRocketFacing : MonoBehaviour
    {
        Item _item;
        bool _turned;
        void Awake() { _item = GetComponent<Item>(); }
        void LateUpdate()
        {
            if (_item == null || _item.sprRenderer == null || _item.sprRenderer.transform == transform) return;
            bool vertical = _item.currentType == ItemsTypes.VERTICAL_STRIPPED;
            bool horizontal = _item.currentType == ItemsTypes.HORIZONTAL_STRIPPED;
            if (vertical || horizontal)
            {
                // The sprite is drawn TILTED: its nose points up and to the
                // right, about 45 degrees off vertical. Both turns cancel that
                // baked angle first, then aim the axis -- so vertical is -45
                // (nose straight up) and horizontal another quarter turn past
                // it (nose right). Rotating by 0/-90 leaves the baked tilt in
                // and is what made the rocket sit at 45 degrees on the board.
                _item.sprRenderer.transform.localRotation = Quaternion.Euler(0, 0, vertical ? -45 : -135);
                _turned = true;
            }
            else if (_turned)
            {
                _item.sprRenderer.transform.localRotation = Quaternion.identity;
                _turned = false;
            }
        }
    }
}
