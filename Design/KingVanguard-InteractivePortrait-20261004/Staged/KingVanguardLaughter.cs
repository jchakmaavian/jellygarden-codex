using System.Collections.Generic;
using Spine;
using Spine.Unity;
using UnityEngine;

namespace RoyalThrone.Characters
{
    // An actor-owned overlay: measured voice envelope, native eyelid deformation,
    // and a small whole-body chuckle. Shared skeleton/animation assets are never edited.
    [DisallowMultipleComponent]
    public sealed class KingVanguardLaughter : MonoBehaviour
    {
        ISkeletonAnimation animator;
        IAnimationStateComponent state;
        TrackEntry entry;
        SkeletonGraphic clockGraphic;
        bool ownsClock, previousUnscaledTime;
        readonly List<Timeline> eyelids = new List<Timeline>();
        readonly List<CanvasGroup> groups = new List<CanvasGroup>();
        public bool Active => entry != null && state != null && ReferenceEquals(state.AnimationState.GetCurrent(0), entry);
        public float Time => Active ? entry.TrackTime / entry.TimeScale : 0;
        public float Pulse => Active ? KingVanguardLaughTiming.PosePulse(Time) : 0;
        public float Intensity => Active ? KingVanguardLaughTiming.Intensity(Time) : 0;

        void OnEnable() { Bind(); }
        void Bind()
        {
            if (animator != null) return;
            animator = GetComponent<SkeletonGraphic>() as ISkeletonAnimation ?? GetComponent<SkeletonAnimation>();
            state = animator as IAnimationStateComponent;
            if (animator != null) animator.UpdateLocal += PoseEyes;
        }
        void OnDisable() { if (animator != null) animator.UpdateLocal -= PoseEyes; animator = null; state = null; Cancel(); }
        public void Begin(TrackEntry track)
        {
            Bind();
            clockGraphic = GetComponent<SkeletonGraphic>();
            if (clockGraphic && !ownsClock)
            {
                previousUnscaledTime = clockGraphic.unscaledTime;
                clockGraphic.unscaledTime = false;
                ownsClock = true;
            }
            entry = track; eyelids.Clear();
            if (!GetComponent<KingVanguardPaintedRig>()) KingVanguardLaughArt.Ensure(gameObject);
            var skeleton = animator.Skeleton;
            var blink = skeleton.Data.FindAnimation(KingVanguardCharacter.IdleAnimation);
            foreach (var timeline in blink.Timelines)
            {
                var slotTimeline = timeline as ISlotTimeline;
                if (slotTimeline == null) continue;
                string name = skeleton.Slots.Items[slotTimeline.SlotIndex].Data.Name;
                if (name == "Sol Göz kapağı" || name == "Sağ Göz Kapağı" || name == "Sol Kirpik" || name == "Sağ Kirpik")
                    eyelids.Add(timeline);
            }
        }
        public void Cancel()
        {
            entry = null;
            if (ownsClock && clockGraphic) clockGraphic.unscaledTime = previousUnscaledTime;
            ownsClock = false;
            GetComponent<KingVanguardLaughArt>()?.Cancel();
        }
        void PoseEyes(ISkeletonAnimation unused)
        {
            if (!Active) return;
            float closure = Intensity * (.94f + .06f * Pulse);
            float blinkTime = Mathf.Lerp(3.55f, 3.74f, closure);
            // Native attachment and FFD timelines keep the eyelids and lashes together.
            foreach (var timeline in eyelids)
                timeline.Apply(animator.Skeleton, 0, blinkTime, null, 1, MixBlend.Replace, MixDirection.In);
        }
        public Vector2 MoveBody(Vector2 point, Bone chest)
        {
            float energy = Intensity, pulse = Pulse;
            float angle = (Mathf.Sin(Time * 3.5f) * .45f + pulse * .65f) * energy * Mathf.Deg2Rad;
            float x = (point.x - chest.WorldX) * (1 + pulse * .024f);
            float y = (point.y - chest.WorldY) * (1 - pulse * .024f);
            return new Vector2(chest.WorldX + Mathf.Cos(angle) * x - Mathf.Sin(angle) * y,
                chest.WorldY + Mathf.Sin(angle) * x + Mathf.Cos(angle) * y + pulse * .07f);
        }
        public bool IsVisible()
        {
            if (!gameObject.activeInHierarchy) return false;
            GetComponentsInParent(true, groups);
            foreach (var group in groups)
            {
                if (!group.enabled) continue;
                if (group.alpha <= .01f) return false;
                if (group.ignoreParentGroups) break;
            }
            return true;
        }
        void Update()
        {
            if (Active && !IsVisible())
            {
                var owner = GetComponentInParent<KingVanguardCharacter>();
                if (owner) owner.ResetToIdle(); else Cancel();
            }
        }
    }
}
