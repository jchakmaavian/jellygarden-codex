// The original TNT piece riding a King Leo TNT item. See StoryOriginalTnt.
//
// Presentation only, after royal-throne's ReferencePieceVisual: the item keeps
// its type, colour, movement and hits; this hides the item's sprite and draws
// the imported prefab in its place, one board cell wide, at the item's own
// sorting slot. It lets go the moment the item stops being a TNT or starts
// being destroyed, which is when the blast takes over.

using RoyalThrone.ReferenceGameplay.Preview;
using UnityEngine;

namespace JellyGarden.Scripts.Story
{
    [DisallowMultipleComponent]
    public sealed class StoryOriginalTntPiece : MonoBehaviour
    {
        private Item _item;
        private GameObject _view;
        private Animator _animator;
        private float _revealLeft;

        public bool Showing => _view != null && _view.activeSelf;
        public bool Revealing => Showing && _revealLeft > 0f;
        /// <summary>The last reveal was a Color Ball's (TntLightballCreationAnimation).</summary>
        public bool ColorBallReveal { get; private set; }

        internal bool Show(Item item, bool reveal)
        {
            _item = item;
            if (_view == null)
            {
                _view = ReferenceTnt.SpawnVisual(ReferenceTnt.PieceKey, transform, transform.position, 1f, 0, 0);
                if (_view == null) return false;
                _view.name = "Original TNT piece";
                _animator = _view.GetComponentInChildren<Animator>(true);
            }
            _view.transform.localPosition = Vector3.zero;
            _view.SetActive(true);
            enabled = true;
            ColorBallReveal = reveal && StoryOriginalBoosters.TakeColorBallReveal(item);
            if (reveal) _revealLeft = ReferenceTnt.Play(_animator, ColorBallReveal
                ? ReferenceTnt.ColorBallCreationState : ReferenceTnt.CreationState);
            else { _revealLeft = 0f; ReferenceTnt.Idle(_animator); }
            Pose();
            return true;
        }

        private void LateUpdate()
        {
            if (_item == null || _item.destroying || _item.currentType != ItemsTypes.PACKAGE)
            {
                Hide();
                return;
            }

            bool paused = LevelManager.THIS != null && LevelManager.THIS.gameStatus == GameState.Pause;
            if (_revealLeft > 0f && !paused)
            {
                _revealLeft -= Time.deltaTime;
                if (_revealLeft <= 0f) ReferenceTnt.Idle(_animator);
            }
            if (_animator != null) _animator.speed = paused || _revealLeft <= 0f ? 0f : 1f;
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
            // A TNT turned back into a gem gets its sprite back. One being
            // destroyed does not: DestroyCor already decides what it shows.
            if (_item != null && !_item.destroying && _item.sprRenderer != null) _item.sprRenderer.enabled = true;
        }
    }
}
