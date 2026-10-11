// The original Rocket or Color Ball piece riding a King Leo item. See
// StoryOriginalBoosters; the TNT has its own, StoryOriginalTntPiece.
//
// Presentation only, after royal-throne's ReferencePieceVisual: the item keeps
// its type, colour, movement and hits; this hides the item's sprite and draws
// the imported prefab in its place, one board cell wide, at the item's own
// sorting slot. A Rocket plays its creation clip and holds the last frame, on
// the branch for its orientation; a Color Ball plays its creation clip and then
// loops its idle. It follows the item's type every frame, because some combos
// write currentType directly, and lets go when the item is destroyed.

using RoyalThrone.ReferenceGameplay.Preview;
using UnityEngine;
using static RoyalThrone.ReferenceGameplay.Preview.ReferenceBoosters;

namespace JellyGarden.Scripts.Story
{
    [DisallowMultipleComponent]
    public sealed class StoryOriginalPiece : MonoBehaviour
    {
        private Item _item;
        private GameObject _view;
        private Animator _animator;
        private string _key;
        private ItemsTypes _type;
        private float _revealLeft;

        public bool Showing => _view != null && _view.activeSelf;
        public bool Revealing => Showing && _revealLeft > 0f;
        public string Key => _key;

        static bool IsRocket(ItemsTypes t) => t == ItemsTypes.HORIZONTAL_STRIPPED || t == ItemsTypes.VERTICAL_STRIPPED;
        static string KeyFor(ItemsTypes t) => t == ItemsTypes.BOMB ? ColorBallKey : IsRocket(t) ? RocketKey : null;
        static string RocketState(ItemsTypes t) => t == ItemsTypes.HORIZONTAL_STRIPPED ? RocketHorizontalState : RocketVerticalState;

        internal bool Show(Item item, bool reveal)
        {
            _item = item;
            string key = KeyFor(item.currentType);
            if (key == null) return false;
            if (_view != null && _key != key) { Destroy(_view); _view = null; }
            if (_view == null)
            {
                _view = ReferenceTnt.SpawnVisual(key, transform, transform.position, 1f, 0, 0);
                if (_view == null) return false;
                _view.name = "Original " + key + " piece";
                _animator = _view.GetComponentInChildren<Animator>(true);
                _key = key;
            }
            _view.transform.localPosition = Vector3.zero;
            _view.SetActive(true);
            enabled = true;
            _type = item.currentType;
            if (reveal) Reveal(); else Idle();
            Pose();
            return true;
        }

        private void Reveal()
        {
            if (_type == ItemsTypes.BOMB)
            {
                ReferenceTnt.Play(_animator, ColorBallCreationState);
                _revealLeft = ColorBallRevealSeconds;
            }
            else
            {
                _revealLeft = ReferenceTnt.Play(_animator, RocketState(_type));
                Branch();
            }
        }

        // ReferencePieceVisual.SetIdle: a Rocket holds its creation clip's last frame; a Color Ball loops its idle.
        private void Idle()
        {
            _revealLeft = 0f;
            if (_type == ItemsTypes.BOMB) ReferenceTnt.Play(_animator, ColorBallDefaultState);
            else
            {
                ReferenceTnt.Play(_animator, RocketState(_type), 1f);
                if (_animator != null) _animator.speed = 0f;
                Branch();
            }
        }

        // One Rocket prefab: the orientation picks its branch.
        private void Branch()
        {
            if (_view == null) return;
            var h = ReferenceTnt.Find(_view.transform, RocketHorizontalBranch);
            var v = ReferenceTnt.Find(_view.transform, RocketVerticalBranch);
            if (h != null) h.gameObject.SetActive(_type == ItemsTypes.HORIZONTAL_STRIPPED);
            if (v != null) v.gameObject.SetActive(_type == ItemsTypes.VERTICAL_STRIPPED);
        }

        private void LateUpdate()
        {
            if (_item == null || _item.destroying || KeyFor(_item.currentType) == null)
            {
                Hide();
                return;
            }
            if (_item.currentType != _type)
            {
                // A Rocket turned the other way, or into the other piece: show it finished.
                if (KeyFor(_item.currentType) != _key) { Show(_item, false); return; }
                _type = _item.currentType;
                Idle();
            }

            bool paused = LevelManager.THIS != null && LevelManager.THIS.gameStatus == GameState.Pause;
            if (_revealLeft > 0f && !paused)
            {
                _revealLeft -= Time.deltaTime;
                if (_revealLeft <= 0f) Idle();
            }
            if (_animator != null)
                _animator.speed = paused ? 0f : _revealLeft > 0f || _type == ItemsTypes.BOMB ? 1f : 0f;
            Pose();
        }

        // One board cell wide whatever the item's own (breathing) scale, and drawn
        // where the item's sprite would be, which this then hides.
        private void Pose()
        {
            Vector3 inherited = transform.lossyScale;
            float cell = StoryBoardScale.Pitch;
            _view.transform.localScale = new Vector3(
                cell / Mathf.Max(.0001f, Mathf.Abs(inherited.x)),
                cell / Mathf.Max(.0001f, Mathf.Abs(inherited.y)),
                1f);

            var sprite = _item.sprRenderer;
            if (sprite == null) return;
            var sorting = _view.GetComponent<UnityEngine.Rendering.SortingGroup>();
            sorting.sortingLayerID = sprite.sortingLayerID;
            sorting.sortingOrder = sprite.sortingOrder;
            sprite.enabled = false;
        }

        private void Hide()
        {
            _revealLeft = 0f;
            if (_view != null) _view.SetActive(false);
            enabled = false;
            // A piece turned back into a gem gets its sprite back. One being
            // destroyed does not: DestroyCor already decides what it shows.
            if (_item != null && !_item.destroying && _item.sprRenderer != null) _item.sprRenderer.enabled = true;
        }
    }
}
