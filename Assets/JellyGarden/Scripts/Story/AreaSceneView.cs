// The picture of the room being repaired, drawn as a stack of layers.
//
// Layer 0 is the ruin. Each completed repair reveals the next layer on top of
// it, so the scene changes visibly with every star spent — decay, then
// structure, then identity, then life, which is the order Area 001 spells out
// and the shape every later area follows.
//
// Layers are resolved by convention (area_001_state0, state1, ...). An area with
// no art of its own falls back to its arc's backdrop, so 150 areas can ship
// before 150 sets of paintings do.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    public sealed class AreaSceneView : MonoBehaviour
    {
        private const float RevealSeconds = 0.55f;

        private static AreaSceneView _instance;

        private readonly List<Image> _layers = new List<Image>();
        private readonly List<float> _revealStartedAt = new List<float>();

        private RectTransform _root;
        private Image _damage;
        private int _areaId;

        public int AreaId
        {
            get { return _areaId; }
        }

        /// <summary>
        /// Attaches the scene view under a parent rect — the area screen puts it
        /// behind its panel, the cutscene runner puts it full screen.
        /// </summary>
        public static AreaSceneView Attach(RectTransform parent, int areaId)
        {
            var go = new GameObject("AreaSceneView", typeof(RectTransform));
            var view = go.AddComponent<AreaSceneView>();
            view._root = go.GetComponent<RectTransform>();
            view._root.SetParent(parent, false);
            StoryUi.Stretch(view._root);
            view.Show(areaId);
            _instance = view;
            return view;
        }

        /// <summary>Rebuilds the stack for an area at its current repair count.</summary>
        public void Show(int areaId)
        {
            _areaId = areaId;
            Rebuild();
        }

        private void Rebuild()
        {
            foreach (Image layer in _layers)
                if (layer != null) Destroy(layer.gameObject);
            _layers.Clear();
            _revealStartedAt.Clear();
            if (_damage != null) Destroy(_damage.gameObject);

            StoryArea area = StoryDatabase.GetArea(_areaId);
            if (area == null) return;

            int done = StoryProgress.TasksCompletedIn(_areaId);

            // Areas 001 and 002 ship a picture per repair; the rest ship one
            // restored room. Both are handled the same way — draw whatever states
            // exist, then lay the neglect over them so an area with a single
            // picture still shows its progress.
            for (int state = 0; state <= area.Tasks.Count; state++)
            {
                Sprite sprite = StoryArt.AreaState(_areaId, state);
                if (sprite == null && state == 0) sprite = StoryArt.AreaRestored(_areaId);
                if (sprite == null)
                {
                    _layers.Add(null);
                    _revealStartedAt.Add(0f);
                    continue;
                }

                Image image = StoryUi.CreateImage("State" + state, _root, sprite, Color.white);
                StoryUi.Stretch(image.rectTransform);
                image.preserveAspect = true;
                image.gameObject.SetActive(state <= done);

                _layers.Add(image);
                _revealStartedAt.Add(0f);
            }

            // How ruined the room still looks. Full at the start, gone once every
            // repair is paid for — the same wash the repair cutscene lifts.
            float progress = area.Tasks.Count == 0
                ? 1f
                : Mathf.Clamp01((float)done / area.Tasks.Count);

            _damage = StoryUi.CreateImage("Damage", _root, null,
                new Color(0.05f, 0.07f, 0.05f, Mathf.Lerp(0.55f, 0f, progress)));
            StoryUi.Stretch(_damage.rectTransform);
        }

        /// <summary>
        /// Fades and scales in the layer a repair just unlocked, and fires the
        /// kit's own repair particle at the same moment. Nothing appears without
        /// an arrival — a layer that pops in reads as a bug.
        /// </summary>
        public void Reveal(int taskIndex)
        {
            int state = Mathf.Clamp(taskIndex, 0, _layers.Count - 1);
            if (state < 0 || state >= _layers.Count) return;

            Image layer = _layers[state];
            if (layer == null) return;

            layer.gameObject.SetActive(true);
            _revealStartedAt[state] = StoryClock.Now;
        }

        private void Update()
        {
            for (int i = 0; i < _layers.Count; i++)
            {
                if (_layers[i] == null || _revealStartedAt[i] <= 0f) continue;

                float t = (StoryClock.Now - _revealStartedAt[i]) / RevealSeconds;
                if (t >= 1f)
                {
                    _layers[i].color = Color.white;
                    _layers[i].rectTransform.localScale = Vector3.one;
                    _revealStartedAt[i] = 0f;
                    continue;
                }

                float eased = 1f - (1f - t) * (1f - t);
                _layers[i].color = new Color(1f, 1f, 1f, eased);
                _layers[i].rectTransform.localScale = Vector3.one * (1.04f - 0.04f * eased);
            }
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        /// <summary>
        /// Called by StoryRepair. Routed through a static so the repair path does
        /// not have to know whether a scene view happens to be on screen.
        /// </summary>
        public static void NotifyTaskCompleted(int areaId, int taskIndex)
        {
            if (_instance == null || _instance._areaId != areaId) return;
            _instance.Reveal(taskIndex);
        }
    }
}
