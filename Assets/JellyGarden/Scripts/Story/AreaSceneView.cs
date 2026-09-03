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
//
// The view fits its parent one of two ways. Letterboxed is the default: the
// whole painting is visible and the parent shows through the bars, which is what
// the area screen and the cutscene stage want. Covered is the home screen's:
// the painting is scaled until it fills the parent in both directions and the
// overflow is cropped, so the room bleeds to every edge of the phone with no
// bars — the shape of docs/royal-match/01-home-area.jpg. The parent has to mask
// its overflow for the second one; a covered view is otherwise happy to draw
// outside it.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    public sealed class AreaSceneView : MonoBehaviour
    {
        private const float RevealSeconds = 0.55f;

        // The neglect wash: full on an untouched room, gone once every repair in
        // the area is paid for. One constant, because the wash is both rebuilt
        // (Rebuild) and animated (LiftDamage) and the two must agree.
        // Royal Match shows a room it has barely started on — Area 2 at 1/7 —
        // and it is still bright: the ruin is carried by what is broken and
        // missing in the picture, never by dimming it. At 0.55 this wash was
        // laying a 41% black sheet over an already dark painting at one repair
        // in four, and the home screen measured 57/255 against the reference's
        // 149. The wash still says "unrestored", it just no longer does it by
        // putting the lights out.
        private const float MaxDamage = 0.22f;
        private static readonly Color DamageTint = new Color(0.05f, 0.07f, 0.05f, 1f);

        // Every scene view alive right now, not just the last one attached. The
        // home screen and the area screen can both be showing the same room, and
        // a repair has to land on both of them — a single static instance meant
        // whichever was built second silently swallowed the reveal.
        private static readonly List<AreaSceneView> _live = new List<AreaSceneView>();

        private readonly List<Image> _layers = new List<Image>();
        private readonly List<float> _revealStartedAt = new List<float>();

        private RectTransform _root;
        private Image _damage;
        private int _areaId;
        private bool _cover;
        private AspectRatioFitter _fitter;

        // The wash animates rather than jumping: a repair lifting the grime is
        // half of what a repair looks like in the 148 areas that ship one
        // painting instead of one per state.
        private float _damageFrom;
        private float _damageTo;
        private float _damageStartedAt;

        public int AreaId
        {
            get { return _areaId; }
        }

        /// <summary>
        /// The rect the painting is drawn in. A cutscene that plays over the room
        /// rather than over a black screen stages itself on this.
        /// </summary>
        public RectTransform Root
        {
            get { return _root; }
        }

        /// <summary>How heavy the neglect wash is being drawn right now, 0..0.55.</summary>
        public float DamageAlpha
        {
            get { return _damage == null ? 0f : _damage.color.a; }
        }

        /// <summary>
        /// What the wash is heading for. Where <see cref="DamageAlpha"/> is
        /// mid-animation and needs a frame loop to arrive, this lands the moment a
        /// repair does — which is what an editor test can read.
        /// </summary>
        public float DamageTarget
        {
            get { return _damageTo; }
        }

        /// <summary>Whether this view fills its parent and crops, or letterboxes.</summary>
        public bool IsCovering
        {
            get { return _cover; }
        }

        /// <summary>The painting's width over its height, or 0 when there is none.</summary>
        public float Aspect
        {
            get { return _fitter == null ? 0f : _fitter.aspectRatio; }
        }

        /// <summary>How many state layers are drawn, painted or not.</summary>
        public int LayerCount
        {
            get { return _layers.Count; }
        }

        /// <summary>How many of them the player can actually see.</summary>
        public int VisibleLayerCount
        {
            get
            {
                int visible = 0;
                for (int i = 0; i < _layers.Count; i++)
                    if (_layers[i] != null && _layers[i].gameObject.activeSelf) visible++;
                return visible;
            }
        }

        /// <summary>
        /// Attaches the scene view under a parent rect — the area screen puts it
        /// behind its panel, the cutscene runner puts it full screen.
        /// </summary>
        public static AreaSceneView Attach(RectTransform parent, int areaId)
        {
            return Attach(parent, areaId, false);
        }

        /// <summary>
        /// As above, with <paramref name="cover"/> filling the parent in both
        /// directions and cropping the overflow instead of letterboxing. Only a
        /// parent that masks — a RectMask2D — should ask for it.
        /// </summary>
        public static AreaSceneView Attach(RectTransform parent, int areaId, bool cover)
        {
            var go = new GameObject("AreaSceneView", typeof(RectTransform));
            var view = go.AddComponent<AreaSceneView>();
            view._root = go.GetComponent<RectTransform>();
            view._root.SetParent(parent, false);
            view._cover = cover;
            StoryUi.Stretch(view._root);
            view.Show(areaId);
            _live.Add(view);
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

            _damageTo = Mathf.Lerp(MaxDamage, 0f, progress);
            _damageFrom = _damageTo;
            _damageStartedAt = 0f;

            _damage = StoryUi.CreateImage("Damage", _root, null, Wash(_damageTo));
            StoryUi.Stretch(_damage.rectTransform);

            ApplyCover();
        }

        /// <summary>
        /// Turns enveloping on or off after the view has been built.
        ///
        /// The area page draws its room letterboxed, because the task list needs
        /// the bottom of the screen and a room cropped to a third of its height
        /// is a room nobody can read. A repair wants the opposite: the list is
        /// gone, the room has the whole screen, and letterbox bars would be the
        /// black cutscene backdrop this replaces, arrived at by accident.
        /// </summary>
        public void SetCovering(bool cover)
        {
            if (_cover == cover) return;
            _cover = cover;

            if (!cover)
            {
                if (_fitter != null) _fitter.enabled = false;
                StoryUi.Stretch(_root);
                return;
            }

            ApplyCover();
        }

        /// <summary>
        /// Sizes the whole stack to envelope its parent at the painting's own
        /// aspect. Done with an AspectRatioFitter rather than arithmetic so it
        /// re-solves on a rotation or a resized Game view, neither of which this
        /// class would otherwise hear about. The layers stay stretched to the
        /// root, so once the root carries the painting's aspect their
        /// preserveAspect has nothing left to letterbox.
        /// </summary>
        private void ApplyCover()
        {
            if (!_cover) return;

            float aspect = 0f;
            for (int i = 0; i < _layers.Count && aspect <= 0f; i++)
            {
                if (_layers[i] == null || _layers[i].sprite == null) continue;
                Rect rect = _layers[i].sprite.rect;
                if (rect.height > 0f) aspect = rect.width / rect.height;
            }

            // No painting at all: there is nothing to shape the root by, and a
            // fitter with a zero ratio collapses it. Leave it stretched.
            if (aspect <= 0f)
            {
                if (_fitter != null) _fitter.enabled = false;
                return;
            }

            if (_fitter == null)
                _fitter = _root.gameObject.AddComponent<AspectRatioFitter>();

            _fitter.enabled = true;
            _fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            _fitter.aspectRatio = aspect;
        }

        /// <summary>
        /// Fades and scales in the layer a repair just unlocked, and fires the
        /// kit's own repair particle at the same moment. Nothing appears without
        /// an arrival — a layer that pops in reads as a bug.
        /// </summary>
        public void Reveal(int taskIndex)
        {
            // The wash lifts on every repair, before anything is said about
            // layers: most areas have no painting for the state just reached, and
            // the grime leaving is the whole of what those rooms can show. It was
            // previously only recomputed by a full Rebuild, so a repair watched on
            // this screen left the room exactly as dirty as it found it.
            LiftDamage();

            int state = Mathf.Clamp(taskIndex, 0, _layers.Count - 1);
            if (state < 0 || state >= _layers.Count) return;

            Image layer = _layers[state];
            if (layer == null) return;

            layer.gameObject.SetActive(true);
            _revealStartedAt[state] = StoryClock.Now;
        }

        /// <summary>
        /// Starts the wash moving to whatever the area's current repair count
        /// deserves. Read from progress rather than counted here, so it is right
        /// however the repair was bought.
        /// </summary>
        private void LiftDamage()
        {
            if (_damage == null) return;

            StoryArea area = StoryDatabase.GetArea(_areaId);
            if (area == null || area.Tasks.Count == 0) return;

            float progress = Mathf.Clamp01(
                (float)StoryProgress.TasksCompletedIn(_areaId) / area.Tasks.Count);

            _damageFrom = _damage.color.a;
            _damageTo = Mathf.Lerp(MaxDamage, 0f, progress);
            _damageStartedAt = StoryClock.Now;
        }

        /// <summary>
        /// Brings the wash into line with the area's repair count without
        /// animating it. A screen refreshing after a change it did not watch — a
        /// repair bought on another screen, progress reset under it — would
        /// otherwise keep drawing the grime it was built with, because the wash is
        /// only otherwise recomputed when the view changes area. A lift already in
        /// flight is retargeted rather than interrupted.
        /// </summary>
        public void SyncDamage()
        {
            if (_damage == null) return;

            StoryArea area = StoryDatabase.GetArea(_areaId);
            if (area == null || area.Tasks.Count == 0) return;

            float progress = Mathf.Clamp01(
                (float)StoryProgress.TasksCompletedIn(_areaId) / area.Tasks.Count);
            _damageTo = Mathf.Lerp(MaxDamage, 0f, progress);

            if (_damageStartedAt > 0f) return;

            _damageFrom = _damageTo;
            _damage.color = Wash(_damageTo);
        }

        private static Color Wash(float alpha)
        {
            return new Color(DamageTint.r, DamageTint.g, DamageTint.b, alpha);
        }

        private void Update()
        {
            if (_damageStartedAt > 0f && _damage != null)
            {
                float t = (StoryClock.Now - _damageStartedAt) / RevealSeconds;
                if (t >= 1f)
                {
                    _damage.color = Wash(_damageTo);
                    _damageStartedAt = 0f;
                }
                else
                {
                    float eased = 1f - (1f - t) * (1f - t);
                    _damage.color = Wash(Mathf.Lerp(_damageFrom, _damageTo, eased));
                }
            }

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
            _live.Remove(this);
        }

        /// <summary>
        /// Called by StoryRepair. Routed through a static so the repair path does
        /// not have to know whether a scene view happens to be on screen. Every
        /// view of that area reveals, because more than one can be alive.
        /// </summary>
        public static void NotifyTaskCompleted(int areaId, int taskIndex)
        {
            for (int i = _live.Count - 1; i >= 0; i--)
            {
                AreaSceneView view = _live[i];
                if (view == null)
                {
                    _live.RemoveAt(i);
                    continue;
                }
                if (view._areaId == areaId) view.Reveal(taskIndex);
            }
        }
    }
}
