using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalThrone.Characters.Activities
{
    /// <summary>Renders the original full-body native rig into an existing uGUI illustration slot.</summary>
    [DefaultExecutionOrder(10000), DisallowMultipleComponent]
    public sealed class NativeKingDrillUiSlot : MonoBehaviour
    {
        public const int TextureWidth = 448, TextureHeight = 512;
        public const float Aspect = (float)TextureWidth / TextureHeight;
        public const float GroundFraction = .069f; // ground within the padded, fixed Idle/Win camera rectangle
        public const string ShaderResource = "KingVanguard/NativeKingDrillUiPMA";
        const int RenderLayer = 31;
        // Fixed union of 13 samples of original Idle and Win; includes the native NewKing root offset.
        static readonly Vector3 FocusCenter = new Vector3(2.330120f, 3.056841f, 0);
        static readonly Vector2 FocusSize = new Vector2(4.787283f, 5.283624f);
        static int nextInstance;
        readonly List<CanvasGroup> groups = new List<CanvasGroup>();
        GameObject renderRoot;
        Camera renderCamera;
        NativeKingDrillActivity actor;
        RawImage picture;
        RenderTexture target;
        Material material;
        bool showing, failed, waitingForIdle;
        float idleAt;

        public NativeKingDrillActivity Actor => actor;
        public Camera RenderCamera => renderCamera;
        public RenderTexture Target => target;
        public bool IsShowing => showing;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { nextInstance = 0; }

        public static NativeKingDrillUiSlot Mount(Image slot)
        {
            if (!slot) return null;
            slot.enabled = false;
            var existing = slot.GetComponent<NativeKingDrillUiSlot>();
            if (existing) return existing;
            var view = slot.gameObject.AddComponent<NativeKingDrillUiSlot>();
            view.Prepare();
            return view;
        }

        void Prepare()
        {
            if (renderRoot || failed) return;
            var prefab = Resources.Load<GameObject>(NativeKingDrillActivity.ResourcePath);
            var shader = Resources.Load<Shader>(ShaderResource);
            if (!prefab || !shader)
            {
                failed = true; Debug.LogError("King Vanguard native UI import is missing its prefab or PMA shader."); return;
            }
            renderRoot = new GameObject("King Vanguard native UI render") { hideFlags = HideFlags.DontSave };
            renderRoot.SetActive(false);
            renderRoot.transform.position = new Vector3(10000 + (++nextInstance) * 100, 10000, 0);
            var instance = Instantiate(prefab, renderRoot.transform, false);
            actor = instance.GetComponent<NativeKingDrillActivity>();
            if (!actor)
            {
                failed = true; Destroy(renderRoot); renderRoot = null;
                Debug.LogError("King Vanguard native prefab has no activity component."); return;
            }
            foreach (var child in instance.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = RenderLayer;
            actor.SetSceneOverlaysVisible(false);
            actor.Animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            actor.Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var cameraObject = new GameObject("King Vanguard isolated camera", typeof(Camera));
            cameraObject.transform.SetParent(renderRoot.transform, false);
            renderCamera = cameraObject.GetComponent<Camera>();
            renderCamera.enabled = false; renderCamera.orthographic = true;
            renderCamera.clearFlags = CameraClearFlags.SolidColor; renderCamera.backgroundColor = Color.clear;
            renderCamera.cullingMask = 1 << RenderLayer; renderCamera.nearClipPlane = .1f; renderCamera.farClipPlane = 50;
            renderCamera.allowHDR = false; renderCamera.allowMSAA = false;
            renderCamera.aspect = Aspect;
            renderCamera.orthographicSize = Mathf.Max(FocusSize.y * .5f, FocusSize.x * .5f / Aspect) * 1.12f;
            cameraObject.transform.localPosition = FocusCenter + Vector3.back * 10;
            target = new RenderTexture(TextureWidth, TextureHeight, 24, RenderTextureFormat.ARGB32)
            { name = "King Vanguard UI 448x512", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            renderCamera.targetTexture = target;
            material = new Material(shader) { name = "King Vanguard UI premultiplied", hideFlags = HideFlags.DontSave };
            var imageObject = new GameObject("Native King Vanguard", typeof(RectTransform), typeof(RawImage));
            imageObject.layer = gameObject.layer;
            imageObject.transform.SetParent(transform, false);
            picture = imageObject.GetComponent<RawImage>(); picture.texture = target; picture.material = material;
            picture.raycastTarget = false; picture.maskable = true; picture.enabled = false;
            var rt = picture.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        bool Visible()
        {
            if (!isActiveAndEnabled || !gameObject.activeInHierarchy) return false;
            GetComponentsInParent(true, groups);
            foreach (var group in groups)
            {
                if (!group.enabled) continue;
                if (group.alpha <= 0) return false;
                if (group.ignoreParentGroups) break;
            }
            return true;
        }

        void Show()
        {
            if (!target.IsCreated()) target.Create();
            renderRoot.SetActive(true);
            actor.SetSceneOverlaysVisible(false); actor.Win();
            var win = actor.FindClip(NativeKingDrillActivity.WinClip);
            idleAt = Time.unscaledTime + (win ? win.length : 0); waitingForIdle = true;
            showing = true;
            Render(); picture.enabled = true;
        }

        void Suspend()
        {
            showing = false; waitingForIdle = false;
            if (picture) picture.enabled = false;
            if (renderRoot) renderRoot.SetActive(false);
            // Keep the camera/actor objects for the next opening, but free the hidden view's GPU allocation.
            if (target && target.IsCreated()) target.Release();
        }

        void LateUpdate()
        {
            if (failed) return;
            if (!renderRoot) Prepare();
            if (!renderRoot) return;
            if (!Visible()) { if (showing) Suspend(); return; }
            if (!showing) { Show(); return; }
            if (waitingForIdle && Time.unscaledTime >= idleAt) { waitingForIdle = false; actor.Idle(); }
            Render();
        }

        void Render()
        {
            // Run after animation and NativeKingDrillPart.LateUpdate; never render the property-target sprites.
            actor.RefreshParts(); renderCamera.Render();
        }

        void OnDisable() { Suspend(); }
        void OnDestroy()
        {
            Suspend();
            if (picture) { picture.texture = null; picture.material = null; Destroy(picture.gameObject); }
            if (renderCamera) renderCamera.targetTexture = null;
            if (target) Destroy(target);
            if (material) Destroy(material);
            if (renderRoot) Destroy(renderRoot);
        }
    }
}
