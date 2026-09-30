using System;
using UnityEngine;

namespace RoyalThrone.Characters.Activities
{
    /// <summary>Activity API over the native rfv387 KingDrill Animator rig. Does not depend on the V1 character.</summary>
    [DisallowMultipleComponent]
    public sealed class NativeKingDrillActivity : MonoBehaviour
    {
        public const string ResourcePath = "KingVanguard/KingVanguardDrill";
        public const string IdleClip = "KingLoopAnim";
        public const string AlternateIdleClip = "KingLoop2Anim";
        public const string WalkClip = "KingWalkAnim";
        public const string WinClip = "KingWinAnim";
        public const string FailClip = "KingFailAnim";
        public const string FailWinClip = "KingFailWin";
        [SerializeField] Animator animator;
        [SerializeField] AnimationClip[] clips = Array.Empty<AnimationClip>();
        [SerializeField] NativeKingDrillPart[] parts = Array.Empty<NativeKingDrillPart>();

        public Animator Animator => animator;
        public AnimationClip[] Clips => clips;
        public NativeKingDrillPart[] Parts => parts;

        public void Configure(Animator value, AnimationClip[] sourceClips, NativeKingDrillPart[] renderParts)
        { animator = value; clips = sourceClips; parts = renderParts; }

        public void Idle(bool alternate = false) { Play(alternate ? AlternateIdleClip : IdleClip); }
        public void Walk() { Play(WalkClip); } // Native controller goes to KingLoop2Anim at the source exit time.
        public void Win() { Play(WinClip); }
        public void Fail() { Play(FailClip); }
        public void RecoverFromFail() { Play(FailWinClip); }

        public void Play(string clipName)
        {
            if (!animator || !FindClip(clipName)) return;
            animator.enabled = true;
            animator.Play(clipName, 0, 0f);
            animator.Update(0f);
            RefreshParts();
        }

        public AnimationClip FindClip(string clipName)
        {
            foreach (var clip in clips) if (clip && clip.name == clipName) return clip;
            return null;
        }

        /// <summary>Deterministic editor/QA sample, including the recovered mesh and sprite-swap result.</summary>
        public bool Sample(string clipName, float time)
        {
            var clip = FindClip(clipName);
            if (!animator || !clip) return false;
            animator.enabled = false;
            clip.SampleAnimation(animator.gameObject, Mathf.Clamp(time, 0f, clip.length));
            RefreshParts();
            return true;
        }

        public void RefreshParts()
        { foreach (var part in parts) if (part && part.gameObject.activeInHierarchy) part.RefreshNow(); }

        /// <summary>Restore native scene overlays only when presenting the complete source room; standalone defaults hide them.</summary>
        public void SetSceneOverlaysVisible(bool visible)
        { foreach (var part in parts) if (part && part.IsSceneOverlay) part.SetSceneOverlayVisible(visible); }
    }
}
