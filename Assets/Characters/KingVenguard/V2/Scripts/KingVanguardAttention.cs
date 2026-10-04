using System.Collections.Generic;
using JellyGarden.Scripts;
using JellyGarden.Scripts.GUI;
using JellyGarden.Scripts.Juice;
using Spine.Unity;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RoyalThrone.Characters
{
    /// <summary>
    /// Read-only attention for the live gameplay portrait. It observes the existing
    /// input route and visible hint, never consumes input or writes Spine animation data.
    /// The face renderer adds these small offsets after the original pose is skinned.
    /// </summary>
    [DefaultExecutionOrder(50), DisallowMultipleComponent]
    public sealed class KingVanguardAttention : MonoBehaviour
    {
        public enum TargetSource { None, Touch, Mouse, Hint }
        const float MouseMemory = 1.25f, TapDuration = .32f;
        readonly List<CanvasGroup> groups = new List<CanvasGroup>();
        KingVanguardCharacter character;
        LevelManager scopedLevel;
        Transform scopedField;
        Square[] scopedBoard;
        int scopedAttempt, mouseDeviceId;
        bool mouseSeeded, pressedLastFrame, waitForRelease = true, applicationPaused, retireOnResume;
        Vector2 lastMousePosition, recentMousePosition, gazeVelocity;
        float mouseMovedAt = float.NegativeInfinity, tapAt = float.NegativeInfinity, leanVelocity, influenceVelocity;

        public Vector2 Gaze { get; private set; }
        /// <summary>Blend from the authored pupil pose to the bounded attention target.</summary>
        public float Influence { get; private set; }
        public float BodyLean { get; private set; }
        public float TapResponse { get; private set; }
        public TargetSource Source { get; private set; }
        // Keep rendering the brief return to the source pose after a target expires.
        public bool HasAttention { get { return Source != TargetSource.None || Influence > 1e-5f || Mathf.Abs(BodyLean) > 1e-5f || TapResponse > 1e-5f; } }

        public static KingVanguardAttention Ensure(SkeletonGraphic graphic)
        {
            var owner = graphic ? graphic.GetComponentInParent<KingVanguardCharacter>() : null;
            if (!IsGameplayPortrait(owner)) return null;
            var attention = owner.GetComponent<KingVanguardAttention>();
            if (!attention) attention = owner.gameObject.AddComponent<KingVanguardAttention>();
            attention.character = owner;
            return attention;
        }

        static bool IsGameplayPortrait(KingVanguardCharacter owner)
        {
            if (!owner || owner.IsAreaRig) return false;
            for (var parent = owner.transform; parent; parent = parent.parent)
                if (parent.name == "Royal gameplay HUD") return true;
            return false;
        }

        void Awake() { character = GetComponent<KingVanguardCharacter>(); }
        void OnEnable() { applicationPaused = false; ResetAttention(); }
        void OnDisable() { ResetAttention(); }
        void OnApplicationFocus(bool focused) { if (!focused) BeginApplicationRetirement(); }
        void OnApplicationPause(bool paused) { applicationPaused = paused; if (paused) BeginApplicationRetirement(); }

        void BeginApplicationRetirement()
        {
            // The OS may stop Update immediately. Keep the pending return across
            // suspension instead of reacquiring a hint with the old influence.
            retireOnResume = HasAttention;
            ClearPointerHistory();
        }

        /// <summary>Clear pointer history and require a release before accepting a held press.</summary>
        public void ResetAttention()
        {
            Gaze = Vector2.zero; Influence = BodyLean = TapResponse = 0; Source = TargetSource.None;
            gazeVelocity = Vector2.zero; leanVelocity = influenceVelocity = 0;
            scopedLevel = null; scopedField = null; scopedBoard = null; scopedAttempt = 0;
            retireOnResume = false;
            ClearPointerHistory();
        }

        void ClearPointerHistory()
        {
            Source = TargetSource.None; gazeVelocity = Vector2.zero;
            mouseSeeded = pressedLastFrame = false; waitForRelease = true; mouseDeviceId = 0;
            mouseMovedAt = tapAt = float.NegativeInfinity;
        }

        bool IsVisiblePortrait()
        {
            if (!IsGameplayPortrait(character) || !character.isActiveAndEnabled || !character.Graphic ||
                !character.Graphic.isActiveAndEnabled) return false;
            GetComponentsInParent(true, groups);
            foreach (var group in groups)
            {
                if (!group.enabled) continue;
                if (group.alpha <= .001f) return false;
                if (group.ignoreParentGroups) break;
            }
            return true;
        }

        void RetireAttention(float delta)
        {
            // The HUD remains visible during its exit fade and transient board locks.
            // Blend back to the authored pose; do not snap the pupils/body to neutral.
            // Discard the old press/target so it cannot replay when input returns.
            ClearPointerHistory();
            // A spring that was still accelerating toward the pointer must not lean
            // farther toward that lost target during its first retirement frames.
            if (influenceVelocity > 0) influenceVelocity = 0;
            if (BodyLean * leanVelocity > 0) leanVelocity = 0;
            Influence = Mathf.Clamp01(Mathf.SmoothDamp(Influence, 0, ref influenceVelocity, .08f, 12, delta));
            BodyLean = Mathf.SmoothDamp(BodyLean, 0, ref leanVelocity, .1f, 8, delta);
            TapResponse = Mathf.MoveTowards(TapResponse, 0, delta / .08f);
            if (Influence < .001f && Mathf.Abs(BodyLean) < .001f && TapResponse == 0) retireOnResume = false;
        }

        static bool DeviceAllowed(InputManager input, InputDevice device)
        {
            if (device == null || !device.added || !device.enabled) return false;
            var asset = input.inputActionAsset;
            if (!asset) return false;
            var devices = asset.devices;
            if (!devices.HasValue) return true;
            foreach (var allowed in devices.Value) if (allowed == device) return true;
            return false;
        }

        static T AllowedDevice<T>(InputManager input, T current) where T : InputDevice
        {
            if (DeviceAllowed(input, current)) return current;
            // Mouse.current/Touchscreen.current are global. A physical editor device
            // can become current while the gameplay actions deliberately target a
            // different device (QA, accessibility or an explicit player device set).
            // Follow that same action-asset filter instead of losing the valid pointer.
            var asset = input.inputActionAsset;
            if (!asset || !asset.devices.HasValue) return null;
            foreach (var device in asset.devices.Value)
                if (device is T selected && DeviceAllowed(input, selected)) return selected;
            return null;
        }

        static bool OnScreen(Vector2 point)
        {
            return !float.IsNaN(point.x) && !float.IsNaN(point.y) &&
                point.x >= 0 && point.y >= 0 && point.x <= Screen.width && point.y <= Screen.height;
        }

        Vector2 DirectionTo(Vector2 screenPoint)
        {
            var graphic = character.Graphic;
            var canvas = graphic.canvas;
            var camera = canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            var head = graphic.Skeleton != null ? graphic.Skeleton.FindBone("yuz") : null;
            float units = canvas ? canvas.referencePixelsPerUnit : 1;
            Vector3 world = head == null ? graphic.transform.position :
                graphic.transform.TransformPoint(new Vector3(head.WorldX * units, head.WorldY * units, 0));
            Vector2 origin = RectTransformUtility.WorldToScreenPoint(camera, world);
            Vector2 direction = screenPoint - origin;
            direction.x /= Mathf.Max(1, Screen.width * .42f);
            direction.y /= Mathf.Max(1, Screen.height * .45f);
            return Vector2.ClampMagnitude(direction, 1);
        }

        void Update()
        {
            var level = LevelManager.THIS;
            // A hidden actor or a missing/replaced board has no visible continuity to
            // preserve. Input restrictions on a still-visible actor are handled below.
            if (!IsVisiblePortrait() || !level || !level.GameField) { ResetAttention(); return; }
            if (scopedLevel != level || scopedField != level.GameField || scopedBoard != level.squaresArray || scopedAttempt != level.passLevelCounter)
            {
                ResetAttention(); scopedLevel = level; scopedField = level.GameField;
                scopedBoard = level.squaresArray; scopedAttempt = level.passLevelCounter;
            }
            var input = InputManager.Instance;
            float now = Time.unscaledTime, delta = Mathf.Min(Time.unscaledDeltaTime, 1f / 15);
            if (applicationPaused || !Application.isFocused || !level.GameField.gameObject.activeInHierarchy ||
                !level.BoardInputAllowed || RoyalThrone.Testing.RoyalSave.BlockGameplayInput ||
                !input.isActiveAndEnabled || input.Gameplay?.PrimaryTouch == null || !input.Gameplay.PrimaryTouch.enabled ||
                character.IsReacting)
            {
                RetireAttention(delta);
                return;
            }
            var touch = AllowedDevice(input, Touchscreen.current);
            bool touching = touch != null && touch.primaryTouch.press.isPressed;
            bool pressed = touching || input.IsTouching;
            // A genuinely new press on the first frame after a reaction/lock is valid.
            // A press already held across that boundary still needs a release first.
            bool newPress = touching ? touch.primaryTouch.press.wasPressedThisFrame : input.WasTouchPressed;
            if (retireOnResume)
            {
                // A fresh gesture may take over immediately. An old held gesture or
                // passive hint waits for the suspended attention to retire first.
                if (newPress) retireOnResume = false;
                else { RetireAttention(delta); return; }
            }
            if (!pressed || newPress) waitForRelease = false;
            var mouse = AllowedDevice(input, Mouse.current);
            if (mouse != null)
            {
                var point = mouse.position.ReadValue();
                if (!mouseSeeded || mouseDeviceId != mouse.deviceId)
                {
                    mouseSeeded = true; mouseDeviceId = mouse.deviceId; lastMousePosition = point;
                    mouseMovedAt = float.NegativeInfinity;
                }
                else if ((point - lastMousePosition).sqrMagnitude >= 4)
                {
                    lastMousePosition = point;
                    if (OnScreen(point)) { recentMousePosition = point; mouseMovedAt = now; }
                    else mouseMovedAt = float.NegativeInfinity;
                }
            }
            else { mouseSeeded = false; mouseMovedAt = float.NegativeInfinity; }

            Vector2 target = Vector2.zero; Source = TargetSource.None;
            if (pressed && !waitForRelease)
            {
                Vector2 point = touching ? touch.primaryTouch.position.ReadValue() : input.TouchPosition;
                if (OnScreen(point))
                {
                    Source = TargetSource.Touch; target = DirectionTo(point);
                    if (!pressedLastFrame) tapAt = now;
                }
                // A released finger must not revive an old desktop pointer target.
                if (touching) mouseMovedAt = float.NegativeInfinity;
            }
            else if (!pressed && now - mouseMovedAt < MouseMemory)
            {
                Source = TargetSource.Mouse; target = DirectionTo(recentMousePosition);
            }
            else if (!pressed && RoyalHintPresenter.TryGetCurrentFocus(level, out var hintPoint))
            {
                var camera = Camera.main;
                if (camera)
                {
                    Vector3 point = camera.WorldToScreenPoint(hintPoint);
                    if (point.z > 0 && OnScreen(point)) { Source = TargetSource.Hint; target = DirectionTo(point) * .68f; }
                }
            }
            pressedLastFrame = pressed;
            float strength = character.CurrentActivity == KingVanguardCharacter.Activity.Worried ? .3f : 1;
            float smooth = Source == TargetSource.Touch ? .11f : Source == TargetSource.Mouse ? .18f : Source == TargetSource.Hint ? .36f : .3f;
            // Preserve the last target direction while its blend fades back to the
            // source eye animation; pulling direction to zero too would move twice.
            if (Source != TargetSource.None) Gaze = Vector2.ClampMagnitude(Vector2.SmoothDamp(Gaze, target, ref gazeVelocity, smooth, 8, delta), 1);
            else
            {
                gazeVelocity = Vector2.zero;
                // A hint disappearing or pointer expiring must start returning
                // immediately, even while its previous target was still easing in.
                if (influenceVelocity > 0) influenceVelocity = 0;
                if (BodyLean * leanVelocity > 0) leanVelocity = 0;
            }
            Influence = Mathf.Clamp01(Mathf.SmoothDamp(Influence, Source == TargetSource.None ? 0 : strength,
                ref influenceVelocity, smooth, 8, delta));
            BodyLean = Mathf.SmoothDamp(BodyLean, target.x * .65f * strength, ref leanVelocity, .24f, 4, delta);
            float age = (now - tapAt) / TapDuration;
            TapResponse = age >= 0 && age < 1 ? Mathf.Sin(age * Mathf.PI) * strength : 0;
        }
    }
}
