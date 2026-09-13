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
                _item.sprRenderer.transform.localRotation = Quaternion.Euler(0, 0, vertical ? 45 : -45);
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
