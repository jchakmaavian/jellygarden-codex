using System;
using System.Collections.Generic;
using Spine.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RoyalThrone.Characters.EditorTools
{
    /// <summary>Exercises the imported assets and real Spine state machine in an isolated preview scene.</summary>
    public static class KingVanguardRuntimeChecks
    {
        public static string Run()
        {
            var results = new List<string>();
            var scene = EditorSceneManager.NewPreviewScene();
            Action<bool, string> check = (ok, name) => { if (!ok) throw new InvalidOperationException("FAIL " + name); results.Add("PASS " + name); };
            try
            {
                var portrait = AssetDatabase.LoadAssetAtPath<GameObject>(KingVanguardPrefabBuilder.PortraitPath);
                var area = AssetDatabase.LoadAssetAtPath<GameObject>(KingVanguardPrefabBuilder.CelebrationPath);
                check(portrait && area, "both new prefabs exist");
                var go = (GameObject)PrefabUtility.InstantiatePrefab(portrait, scene);
                var king = go.GetComponent<KingVanguardCharacter>();
                king.ResetToIdle();
                check(king.Graphic.Skeleton.Data.Animations.Count == 32, "all 32 portrait tracks load");
                check(king.CurrentAnimation == KingVanguardCharacter.IdleAnimation, "reopen starts first idle");
                check(king.Graphic.AnimationState.GetCurrent(0).Next != null, "next idle queued at animation boundary");
                check(king.PlayHappy(), "positive reaction starts");
                check(king.CurrentAnimation == KingVanguardCharacter.HappyAnimation, "exact serialized KingUi happy binding");
                check(!king.PlayHappy(), "cascade cannot restart active reaction");
                king.SetWorried(true);
                check(king.CurrentAnimation == KingVanguardCharacter.HappyAnimation, "worry context does not interrupt positive action");
                Advance(king, 4);
                check(king.CurrentAnimation == KingVanguardCharacter.WorryStartAnimation, "new low-move context starts worry after reaction");
                Advance(king, 6.3f);
                check(king.CurrentAnimation == KingVanguardCharacter.WorryLoopAnimation, "deferred worry entrance reaches loop");
                check(king.PlayHappy(), "positive action can interrupt established worry");
                Advance(king, 4);
                check(king.CurrentAnimation == KingVanguardCharacter.WorryLoopAnimation, "established worry resumes without repeated entrance");
                king.SetWorried(false);
                check(king.CurrentAnimation.StartsWith("idle_loop_", StringComparison.Ordinal), "extra moves recover idle");
                king.SetWorried(true);
                check(king.CurrentAnimation == KingVanguardCharacter.WorryStartAnimation, "worry entrance plays");
                Advance(king, 6.3f);
                check(king.CurrentAnimation == KingVanguardCharacter.WorryLoopAnimation, "worry entrance reaches loop");
                float time = king.Graphic.AnimationState.GetCurrent(0).TrackTime;
                king.SetWorried(true);
                check(king.Graphic.AnimationState.GetCurrent(0).TrackTime == time, "persistent low moves do not restart worry");
                check(king.PlaySad(), "failure overrides worry");
                Advance(king, 4);
                check(king.CurrentAnimation == KingVanguardCharacter.SadAnimation && king.CurrentActivity == KingVanguardCharacter.Activity.Sad,
                    "failure holds final sad expression");
                king.ResetToIdle();
                check(!king.IsWorried && !king.IsReacting && king.CurrentAnimation == KingVanguardCharacter.IdleAnimation, "reopen clears stale state");
                check(king.PlayHappy(), "generic cheer before committed gameplay event");
                check(king.PlayExtraMoves() && king.CurrentAnimation == "k-1_sevinme_NEW", "committed extra moves override generic cheer");
                check(king.PlayRelief() && king.CurrentAnimation == "Relax_1", "committed goals override pending positive reaction");
                Advance(king, 2.3f);
                check(king.CurrentActivity == KingVanguardCharacter.Activity.Idle, "relief finishes and returns to neutral context");
                var rect = (RectTransform)go.transform; rect.sizeDelta = new Vector2(180, 230);
                king.Frame(rect, KingVanguardCharacter.Framing.Bust);
                check(Mathf.Approximately(king.Graphic.rectTransform.localScale.x, king.Graphic.rectTransform.localScale.y), "portrait uses uniform scale");
                Advance(king, .1f);
                foreach (string name in new[] { "Frame Background3", "sol frame", "sag frame" })
                    check(king.Graphic.Skeleton.FindSlot(name)?.Attachment == null, "external UI owns decoration " + name);
                var areaGo = (GameObject)PrefabUtility.InstantiatePrefab(area, scene);
                var celebration = areaGo.GetComponent<KingVanguardCharacter>();
                celebration.ResetToIdle();
                check(celebration.IsAreaRig && celebration.Graphic.Skeleton.Data.Animations.Count == 10, "all 10 area tracks load in independent area rig");
                check(celebration.PlayCelebration(), "area clap begins");
                check(!celebration.PlayCelebration(), "area clap cannot stack");
                Advance(celebration, 6);
                check(celebration.CurrentAnimation == KingVanguardCharacter.AreaIdleAnimation, "area clap finishes and returns to area idle");
                foreach (string name in new[] { "background", "ipad_background", "borazan", "new_flama_rulo", "Group 1 copy" })
                    check(celebration.Graphic.Skeleton.FindSlot(name)?.Attachment == null, "area decoration removed " + name);
                check(!king.Graphic.raycastTarget && !celebration.Graphic.raycastTarget, "neither rig intercepts input");
                return string.Join("\n", results);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        static void Advance(KingVanguardCharacter king, float seconds)
        {
            for (float t = 0; t < seconds; t += 1f / 60) king.Graphic.Update(1f / 60);
        }
    }
}
