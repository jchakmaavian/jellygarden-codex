using System.Collections.Generic;
using Spine.Unity;
using UnityEngine;

namespace RoyalThrone.Characters
{
    /// <summary>
    /// King Venguard V1: the Spine 3.8 rig rendered by the official spine-unity SkeletonGraphic, driven only by its authored
    /// animations. "idle" (4.0 s) loops on track 0. <see cref="PlayHappy"/> plays "happy_thumbs_up" (3.8 s, thumbs-up swapped
    /// in at 1.90 s) once on the same track and queues idle behind it, so reactions never overlap or stack. The rig is framed
    /// into its UI slot with one uniform scale: the character is never stretched to fit.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class KingVenguardCharacter : MonoBehaviour
    {
        public const string IdleAnimation = "idle";
        public const string HappyAnimation = "happy_thumbs_up";

        /// <summary>Which part of the full-body front rig a slot shows. Boxes are in Spine units (root at ground centre, y up).</summary>
        public enum Framing { Bust, Avatar, FullBody }

        [SerializeField] SkeletonGraphic skeletonGraphic;
        [SerializeField] Framing framing = Framing.Bust;
        [SerializeField, Range(0f, 0.2f)] float topMargin = 0.04f;

        RectTransform frameArea;
        Vector2 framedSize = new Vector2(-1, -1);
        Framing framedAs;
        readonly List<CanvasGroup> parentGroups = new List<CanvasGroup>();

        public SkeletonGraphic Graphic => skeletonGraphic;
        public bool IsReacting
        {
            get
            {
                var state = Ready ? skeletonGraphic.AnimationState : null;
                var current = state?.GetCurrent(0);
                return current != null && current.Animation.Name == HappyAnimation && !current.IsComplete;
            }
        }

        bool Ready
        {
            get
            {
                if (!skeletonGraphic) return false;
                if (!skeletonGraphic.IsValid) skeletonGraphic.Initialize(false);
                return skeletonGraphic.IsValid && skeletonGraphic.AnimationState != null;
            }
        }

        /// <summary>Crown-to-chest bust (portrait windows), a tighter head crop (small avatars) or the whole figure.</summary>
        static Rect Focus(Framing f)
        {
            switch (f)
            {
                case Framing.Avatar: return Rect.MinMaxRect(-430, 1300, 430, 2175);
                case Framing.FullBody: return Rect.MinMaxRect(-830, -20, 830, 2175);
                default: return Rect.MinMaxRect(-600, 1140, 600, 2175);   // caped shoulders (±587) and crown tip (2135) in view
            }
        }

        public void PlayIdle()
        {
            if (!Ready) return;
            var current = skeletonGraphic.AnimationState.GetCurrent(0);
            if (current != null && current.Animation.Name == IdleAnimation && current.Loop) return;
            skeletonGraphic.AnimationState.SetAnimation(0, IdleAnimation, true);
        }

        /// <summary>Drop any reaction in progress and restart the idle loop from its first pose, so a screen that closed
        /// mid-reaction reopens on a clean idle instead of resuming the old reaction.</summary>
        public void ResetToIdle()
        {
            if (!Ready) return;
            skeletonGraphic.AnimationState.ClearTracks();
            skeletonGraphic.Skeleton.SetToSetupPose();
            skeletonGraphic.AnimationState.SetAnimation(0, IdleAnimation, true);
        }

        /// <summary>Play the happy reaction once and return to idle. Ignored while a reaction is already playing.</summary>
        public bool PlayHappy()
        {
            if (!Ready || IsReacting) return false;
            var state = skeletonGraphic.AnimationState;
            state.SetAnimation(0, HappyAnimation, false);   // mixes in from the current idle pose (SkeletonDataAsset mix 0.2 s)
            state.AddAnimation(0, IdleAnimation, true, 0f);  // happy ends on idle's first pose, so the return is seamless
            return true;
        }

        /// <summary>Frame the rig inside <paramref name="area"/> with a uniform scale: the focus box fits the area, top-aligned.</summary>
        public void Frame(RectTransform area, Framing how)
        {
            frameArea = area; framing = how; framedSize = new Vector2(-1, -1);
            ApplyFraming();
        }

        void ApplyFraming()
        {
            if (!skeletonGraphic || !frameArea) return;
            var size = frameArea.rect.size;
            if (size.x <= 0 || size.y <= 0 || (size == framedSize && framedAs == framing)) return;
            framedSize = size; framedAs = framing;
            var canvas = skeletonGraphic.canvas;
            float unitsPerSpine = skeletonGraphic.SkeletonDataAsset.scale * (canvas ? canvas.referencePixelsPerUnit : 100f);
            var box = Focus(framing);
            float scale = Mathf.Min(size.x / box.width, size.y / (box.height * (1 + topMargin)));   // uniform: never distorts
            var rt = skeletonGraphic.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
            // The mesh is drawn around the skeleton root, which sits at the pivot. Size the rect to the focus box and put the
            // pivot at the root's place inside it (spine-unity's own "match RectTransform with bounds" convention), so the rect
            // covers what is drawn: RectMask2D culls a graphic by its rect, and a rect centred on the feet culls the bust.
            rt.pivot = new Vector2(-box.xMin / box.width, -box.yMin / box.height);
            rt.sizeDelta = new Vector2(box.width, box.height) * unitsPerSpine;
            rt.localScale = new Vector3(scale / unitsPerSpine, scale / unitsPerSpine, 1);
            rt.localRotation = Quaternion.identity;
            // skeleton root sits at the pivot: put the focus box's top just under the area's top, centred horizontally
            rt.anchoredPosition = new Vector2(-box.center.x * scale, size.y * .5f - size.y * topMargin - box.yMax * scale);
        }

        void LateUpdate() { ApplyFraming(); FreezeWhileHidden(); }

        /// <summary>Home and the gameplay HUD hide by setting a CanvasGroup's alpha to 0 and stay active, so the King on the
        /// hidden screen kept animating and rebuilding his mesh every frame. He is frozen while a group above him is fully
        /// transparent, and refreshed once on the frame he shows again, so that frame never draws a stale pose.</summary>
        void FreezeWhileHidden()
        {
            if (!skeletonGraphic) return;
            bool hidden = false;
            GetComponentsInParent(true, parentGroups);
            foreach (var group in parentGroups)
            {
                if (!group.enabled) continue;   // Unity ignores a disabled group's alpha
                if (group.alpha <= 0f) { hidden = true; break; }
                if (group.ignoreParentGroups) break;
            }
            if (hidden == skeletonGraphic.freeze) return;
            skeletonGraphic.freeze = hidden;
            if (!hidden && Ready) { skeletonGraphic.Update(0f); skeletonGraphic.LateUpdate(); }
        }
    }
}
