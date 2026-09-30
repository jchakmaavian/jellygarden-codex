using System.Collections.Generic;
using Spine;
using Spine.Unity;
using UnityEngine;

namespace RoyalThrone.Characters
{
    /// <summary>Presentation adapter for the two unchanged Spine binaries extracted from rfv387.
    /// Animation curves, attachment swaps and deformations are played by the official Spine runtime.</summary>
    [DisallowMultipleComponent]
    public sealed class KingVanguardCharacter : MonoBehaviour
    {
        public enum Framing { Bust, Avatar, Celebration, GameplayPortrait }
        public enum Activity { Idle, Worried, Happy, Sad, Excited, Relief, ExtraMoves, Celebration, MatchAcknowledgement }
        public const int WorryMoveCount = 3;
        public const string IdleAnimation = "idle_loop_1";
        public const string HappyAnimation = "k-1sevinme_6";
        public const string MatchAcknowledgementAnimation = "k-1_sevinme_3";
        public const string WorryStartAnimation = "k-1_kaygi";
        public const string WorryLoopAnimation = "k-1_kaygi_loop";
        public const string SadAnimation = "k-1_uzulme";
        public const string AreaIdleAnimation = "king_drill_loop";
        static readonly string[] IdleCycle = { "idle_loop_2", "idle_loop_2", "idle_loop_3" };
        static readonly HashSet<string> PortraitDecorations = new HashSet<string> { "Frame Background3", "sol frame", "sag frame" };
        static readonly HashSet<string> AreaDecorations = new HashSet<string> {
            "ipad_background", "background", "sol", "borazan_mask", "borazan", "borazan2", "borazan3", "borazan4",
            "new_borazan_golge", "new_borazan_golge2", "new_borazan_golge3", "new_borazan_golge4", "flama_mask_1",
            "new_flama_golge", "new_flama_kose", "new_flama_kose2", "new_flama_kivrik", "new_flama_kivrik2",
            "new_flama_curve", "new_flama_rulo", "Group 1 copy"
        };

        [SerializeField] SkeletonGraphic skeletonGraphic;
        [SerializeField] bool areaRig;
        [SerializeField] Rect portraitFocus = new Rect(-214, 0, 428, 415);
        // The gameplay arch replaces the source outer frame. Crop into its inner window,
        // so the cape's straight source edges extend behind the UI ring instead of floating inside it.
        static readonly Rect GameplayFocus = new Rect(-184, 38, 368, 388);
        [SerializeField] Rect avatarFocus = new Rect(-178, 105, 356, 310);
        [SerializeField] Rect celebrationFocus = new Rect(-565, -55, 1130, 1240);
        [SerializeField, Range(0f, .2f)] float topMargin = .02f;
        readonly List<CanvasGroup> parentGroups = new List<CanvasGroup>();
        RectTransform frameArea;
        Vector2 framedSize = new Vector2(-1, -1);
        Framing framing;
        bool attached, worried, worryEntered;
        int sequence, idleIndex;
        Activity activity;

        public SkeletonGraphic Graphic => skeletonGraphic;
        public bool IsAreaRig => areaRig;
        public bool IsWorried => worried;
        public Activity CurrentActivity => activity;
        public bool IsReacting => activity != Activity.Idle && activity != Activity.Worried;
        public string CurrentAnimation => Ready ? skeletonGraphic.AnimationState.GetCurrent(0)?.Animation.Name : null;

        bool Ready
        {
            get
            {
                if (!skeletonGraphic) return false;
                if (!skeletonGraphic.IsValid) skeletonGraphic.Initialize(false);
                if (!skeletonGraphic.IsValid || skeletonGraphic.AnimationState == null) return false;
                if (!attached) { skeletonGraphic.UpdateComplete += RemoveDecoration; attached = true; }
                return true;
            }
        }

        void Awake() { ResetToIdle(); }
        void OnDestroy() { if (attached && skeletonGraphic) skeletonGraphic.UpdateComplete -= RemoveDecoration; }

        /// <summary>Suppress only the APK's surrounding UI, after attachment timelines have applied.</summary>
        void RemoveDecoration(ISkeletonAnimation animated)
        {
            var hidden = areaRig ? AreaDecorations : PortraitDecorations;
            foreach (var slot in skeletonGraphic.Skeleton.Slots) if (hidden.Contains(slot.Data.Name)) slot.Attachment = null;
        }

        public void ResetToIdle()
        {
            if (!Ready) return;
            sequence++; worried = false; worryEntered = false; idleIndex = 0;
            skeletonGraphic.AnimationState.ClearTracks();
            skeletonGraphic.Skeleton.SetToSetupPose();
            StartIdle(true);
            RemoveDecoration(skeletonGraphic);
        }

        public void PlayIdle()
        {
            if (!Ready || (activity == Activity.Idle && skeletonGraphic.AnimationState.GetCurrent(0) != null)) return;
            sequence++; worried = false; worryEntered = false; StartIdle(false);
        }

        void StartIdle(bool first)
        {
            activity = Activity.Idle;
            var state = skeletonGraphic.AnimationState;
            state.SetAnimation(0, areaRig ? AreaIdleAnimation : first ? IdleAnimation : NextIdle(), areaRig);
            QueueIdle();
        }

        string NextIdle() { return IdleCycle[idleIndex++ % IdleCycle.Length]; }

        void QueueIdle()
        {
            if (areaRig || activity != Activity.Idle || !Ready) return;
            var current = skeletonGraphic.AnimationState.GetCurrent(0);
            if (current != null && current.Next == null)
                skeletonGraphic.AnimationState.AddAnimation(0, NextIdle(), false, 0);
        }

        public void SetWorried(bool value)
        {
            if (areaRig || worried == value || !Ready) return;
            worried = value; worryEntered = false;
            // A finite positive reaction finishes normally, then reads this latest context.
            if (IsReacting) return;
            sequence++;
            if (worried) StartWorry(true); else StartIdle(false);
        }

        void StartWorry(bool entrance)
        {
            worryEntered = true;
            activity = Activity.Worried;
            var state = skeletonGraphic.AnimationState;
            state.SetAnimation(0, entrance ? WorryStartAnimation : WorryLoopAnimation, !entrance);
            if (entrance) state.AddAnimation(0, WorryLoopAnimation, true, 0);
        }

        void ReturnToContext(int stamp)
        {
            if (stamp != sequence || !Ready) return;
            if (worried && !areaRig) StartWorry(!worryEntered); else StartIdle(false);
        }

        bool React(string animation, Activity next, bool hold = false, bool priority = false)
        {
            if (!Ready || (IsReacting && !priority)) return false;
            if (skeletonGraphic.Skeleton.Data.FindAnimation(animation) == null) return false;
            int stamp = ++sequence; activity = next;
            var entry = skeletonGraphic.AnimationState.SetAnimation(0, animation, false);
            if (!hold) entry.Complete += completed => ReturnToContext(stamp);
            return true;
        }

        public bool PlayHappy() { return areaRig ? PlayCelebration() : React(HappyAnimation, Activity.Happy); }
        public bool PlayMatchAcknowledgement() { return !areaRig && React(MatchAcknowledgementAnimation, Activity.MatchAcknowledgement); }
        public bool PlayThumbsUp() { return !areaRig && React("Happy_2", Activity.Happy); }
        public bool PlayExcited() { return !areaRig && React("Heyecan_1", Activity.Excited); }
        // Committed gameplay outcomes take priority over a generic power-up cheer.
        public bool PlayRelief() { return !areaRig && React("Relax_1", Activity.Relief, false, true); }
        public bool PlayExtraMoves() { return !areaRig && React("k-1_sevinme_NEW", Activity.ExtraMoves, false, true); }
        public bool PlaySad() { return !areaRig && React(SadAnimation, Activity.Sad, true, true); }

        public bool PlayCelebration()
        {
            if (!areaRig || !Ready || IsReacting) return false;
            int stamp = ++sequence; activity = Activity.Celebration;
            var state = skeletonGraphic.AnimationState;
            state.SetAnimation(0, "king_drill_win_clap_start", false);
            state.AddAnimation(0, "king_drill_win_clap_loop", false, 0);
            state.AddAnimation(0, "king_drill_win_clap_loop", false, 0);
            var finish = state.AddAnimation(0, "king_drill_win_clap_finish", false, 0);
            finish.Complete += completed => ReturnToContext(stamp);
            return true;
        }

        public void Frame(RectTransform area, Framing how)
        {
            frameArea = area; framing = how; framedSize = new Vector2(-1, -1); ApplyFraming();
        }

        void ApplyFraming()
        {
            if (!skeletonGraphic || !frameArea) return;
            var size = frameArea.rect.size;
            if (size.x <= 0 || size.y <= 0 || size == framedSize) return;
            framedSize = size;
            var box = areaRig ? celebrationFocus : framing == Framing.Avatar ? avatarFocus :
                framing == Framing.GameplayPortrait ? GameplayFocus : portraitFocus;
            var canvas = skeletonGraphic.canvas;
            float units = skeletonGraphic.SkeletonDataAsset.scale * (canvas ? canvas.referencePixelsPerUnit : 100);
            bool gameplayPortrait = !areaRig && framing == Framing.GameplayPortrait;
            float margin = gameplayPortrait ? 0 : topMargin;
            float scale = Mathf.Min(size.x / box.width, size.y / (box.height * (1 + margin)));
            var rt = skeletonGraphic.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
            rt.pivot = new Vector2(-box.xMin / box.width, -box.yMin / box.height);
            rt.sizeDelta = box.size * units;
            rt.localScale = new Vector3(scale / units, scale / units, 1);
            rt.localRotation = Quaternion.identity;
            float originY = gameplayPortrait ? -size.y * .5f - box.yMin * scale :
                size.y * .5f - size.y * margin - box.yMax * scale;
            rt.anchoredPosition = new Vector2(-box.center.x * scale, originY);
        }

        void LateUpdate()
        {
            ApplyFraming(); QueueIdle();
            if (!skeletonGraphic) return;
            bool hidden = false;
            GetComponentsInParent(true, parentGroups);
            foreach (var group in parentGroups)
            {
                if (!group.enabled) continue;
                if (group.alpha <= 0) { hidden = true; break; }
                if (group.ignoreParentGroups) break;
            }
            if (hidden == skeletonGraphic.freeze) return;
            skeletonGraphic.freeze = hidden;
            if (!hidden && Ready) { skeletonGraphic.Update(0); skeletonGraphic.LateUpdate(); }
        }
    }
}
