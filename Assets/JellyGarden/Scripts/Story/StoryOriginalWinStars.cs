// The original Royal Throne win stars on King Leo's win card.
//
// After royal-throne's RoyalWinConfetti: the imported StarHitParticles and
// StarIdleParticles play on a hidden stage far from the board, a camera renders
// them into a texture, and this graphic adds that texture over the card behind
// the star (Hidden/RoyalThrone/UIWinGlow). The card is an overlay canvas, which
// world-space particles cannot reach any other way. It stands in for the card's
// own StoryWinStarVfx while it plays; with any imported asset missing it is
// never added and the card keeps its own effect.

using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class StoryOriginalWinStars : MaskableGraphic
    {
        const int StageLayer = 31;
        // The imported prefabs under Resources/ReferenceGameplay/VFX (ReferenceBoosters' names).
        const string StarHitVfx = "StarHitParticles", StarIdleVfx = "StarIdleParticles";

        GameObject _stage;
        Camera _camera;
        RenderTexture _target;
        Material _composite;
        ParticleSystem[] _systems;
        float _started = -1f, _previous = -1f;

        public int VisibleSparkles { get; private set; }
        public bool Playing => _started >= 0f;
        public override Texture mainTexture => _target != null ? _target : Texture2D.blackTexture;

        /// <summary>
        /// Adds the stars over <paramref name="effects"/>, in its place among the card's
        /// children, and hides the card's own star effect. Null when an asset is missing.
        /// </summary>
        public static StoryOriginalWinStars Attach(RectTransform effects, Graphic replaced)
        {
            if (!Application.isPlaying || effects == null) return null;
            var go = new GameObject("OriginalWinStars", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(effects.parent, false);
            rect.anchorMin = effects.anchorMin; rect.anchorMax = effects.anchorMax;
            rect.pivot = effects.pivot;
            rect.anchoredPosition = effects.anchoredPosition;
            rect.sizeDelta = effects.sizeDelta;
            rect.SetSiblingIndex(effects.GetSiblingIndex() + 1);
            var stars = go.AddComponent<StoryOriginalWinStars>();
            if (!stars.Build()) { Destroy(go); return null; }
            if (replaced != null) replaced.enabled = false;
            return stars;
        }

        bool Build()
        {
            if (_stage != null) return true;
            var hit = Resources.Load<GameObject>("ReferenceGameplay/VFX/" + StarHitVfx);
            var idle = Resources.Load<GameObject>("ReferenceGameplay/VFX/" + StarIdleVfx);
            var shader = Resources.Load<Shader>("RoyalGameplay/UIWinGlow");
            if (hit == null || idle == null || shader == null) return false;
            _stage = new GameObject("Original win stars stage") { hideFlags = HideFlags.HideAndDontSave };
            _stage.transform.position = new Vector3(20000, 20000, 0);
            foreach (var prefab in new[] { hit, idle })
            {
                var instance = Instantiate(prefab, _stage.transform, false);
                instance.transform.localPosition = Vector3.zero;
                foreach (var t in instance.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = StageLayer;
            }
            _systems = _stage.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < _systems.Length; i++)
            {
                var ps = _systems[i];
                ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.useAutoRandomSeed = false;
                ps.randomSeed = (uint)(17 + i);
            }
            var cameraObject = new GameObject("Original win stars camera");
            cameraObject.transform.SetParent(_stage.transform, false);
            cameraObject.transform.localPosition = new Vector3(0, 0, -10);
            _camera = cameraObject.AddComponent<Camera>();
            _camera.enabled = false;
            _camera.orthographic = true;
            _camera.orthographicSize = 4;
            _camera.nearClipPlane = .1f;
            _camera.farClipPlane = 20;
            _camera.cullingMask = 1 << StageLayer;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = Color.black;
            _camera.allowHDR = false;
            _camera.allowMSAA = false;
            _target = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32) { name = "Original win stars" };
            _target.Create();
            _camera.targetTexture = _target;
            _composite = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            material = _composite;
            raycastTarget = false;
            color = new Color(1, 1, 1, 0);
            SetAllDirty();
            return true;
        }

        /// <summary>Starts the burst; the idle sparkle then loops while the card is up.</summary>
        public void Play()
        {
            if (!Build()) return;
            _started = Time.unscaledTime;
            _previous = -1f;
        }

        void Update()
        {
            if (_started < 0f || _systems == null) return;
            float seconds = Time.unscaledTime - _started;
            // RoyalWinConfetti.Pose: fade in over the card's entrance, then run the native simulation.
            color = new Color(1, 1, 1, Mathf.SmoothStep(0, 1, Mathf.Clamp01((seconds - .20f) / .64f)));
            float time = Mathf.Max(0, seconds - .32f);
            bool restart = _previous < 0;
            VisibleSparkles = 0;
            foreach (var ps in _systems)
            {
                if (!ps.gameObject.activeInHierarchy) continue;
                ps.Simulate(restart ? time : time - _previous, false, restart, false);
                ps.Pause(false);
                VisibleSparkles += ps.particleCount;
            }
            _previous = time;
            _camera.Render();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            // A square view of the stage, centred on the star, as wide as this rect.
            float half = Mathf.Max(r.width, r.height) * .5f;
            Vector2 c = r.center;
            vh.AddVert(c + new Vector2(-half, -half), color, new Vector2(0, 0));
            vh.AddVert(c + new Vector2(-half, half), color, new Vector2(0, 1));
            vh.AddVert(c + new Vector2(half, half), color, new Vector2(1, 1));
            vh.AddVert(c + new Vector2(half, -half), color, new Vector2(1, 0));
            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(0, 2, 3);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _previous = -1f;
            if (_stage != null) _stage.SetActive(false);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (_stage != null) _stage.SetActive(true);
            _previous = -1f;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_stage != null) Destroy(_stage);
            if (_target != null) { _target.Release(); Destroy(_target); }
            if (_composite != null) Destroy(_composite);
        }
    }
}
