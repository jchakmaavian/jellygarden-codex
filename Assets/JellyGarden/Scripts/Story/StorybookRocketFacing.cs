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
                // The sprite is drawn UPRIGHT, nose pointing right, exactly like
                // every other piece on the board. So a horizontal rocket needs
                // no rotation at all and a vertical one is a plain quarter turn.
                //
                // It used to be drawn tilted at 45 degrees, and this method
                // cancelled that tilt with -45 / -135. Every bug the rocket had
                // came out of that one decision: a residual angle when the bake
                // was not exactly 45, art that overflowed its cell because a
                // turned sprite sweeps its diagonal rather than its width, and
                // a pair of angles attached to the wrong cases. Drawing it
                // straight, like its neighbours, removed all three at once.
                _item.sprRenderer.transform.localRotation = Quaternion.Euler(0, 0, vertical ? -90 : 0);
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
