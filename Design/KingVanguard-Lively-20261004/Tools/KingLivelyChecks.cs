using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;
using RoyalThrone.Characters;

public static class KingLivelyChecks
{
    public static string Run()
    {
        var report = new StringBuilder();
        void Check(bool valid, string text) { if (!valid) throw new Exception(text); report.AppendLine("PASS " + text); }
        foreach (int fps in new[] {15, 30, 60, 120})
        {
            var p = new KingVanguardPerformance(); p.Reset(); float dt = 1f / fps;
            void Step(KingVanguardCharacter.Activity activity, float phase, Vector2 gaze, float tap = 0, float laughTime = 0, int source = 0)
                => p.Step(activity, dt, phase, gaze, 0, tap, laughTime, source);
            for (int i = 0; i < fps * 22; i++) Step(KingVanguardCharacter.Activity.Idle, 0, Vector2.zero);
            Check(p.GestureCount >= 3, fps + "fps idle curiosity, pleased nod and greeting variations");
            float priorHappy = p.Happy;
            Step(KingVanguardCharacter.Activity.Idle, 0, new Vector2(.8f, -.3f), .2f, 0, 1);
            for (int i = 0; i < fps / 2; i++) Step(KingVanguardCharacter.Activity.Idle, 0, new Vector2(.8f, -.3f), .8f, 0, 1);
            Check(p.Happy > .35f, fps + "fps touch warms the expression");
            Check((p.Eyes-new Vector2(.8f,-.3f)).magnitude < .02f, fps + "fps fresh pointer wins over autonomous glances");
            int priorGestures = p.GestureCount;
            for (int i = 0; i < fps; i++) Step(KingVanguardCharacter.Activity.Idle, 0, new Vector2(-.4f, -.4f), 0, 0, 3);
            Check(p.GestureCount == priorGestures + 1, fps + "fps hint acquisition nod occurs once");
            float maxHeadStep = 0, maxWeightStep = 0;
            for (int i = 0; i < fps * 6; i++)
            {
                float t = i * dt;
                var activity = t < 1 ? KingVanguardCharacter.Activity.Happy : t < 2 ? KingVanguardCharacter.Activity.Laughing : t < 3 ? KingVanguardCharacter.Activity.Sad : t < 4 ? KingVanguardCharacter.Activity.Relief : t < 5 ? KingVanguardCharacter.Activity.ExtraMoves : KingVanguardCharacter.Activity.Worried;
                var before = p.Head; float weight = p.Laugh;
                Step(activity, Mathf.Repeat(t, 1), Vector2.zero, 0, Mathf.Max(0,t-1));
                maxHeadStep = Mathf.Max(maxHeadStep,Vector4.Distance(before,p.Head));
                maxWeightStep = Mathf.Max(maxWeightStep,Mathf.Abs(weight-p.Laugh));
                if(float.IsNaN(p.Head.x)||float.IsNaN(p.Head.y)||float.IsInfinity(p.Head.y))throw new Exception("Non-finite pose");
                float sum = p.Happy+p.Excited+p.Laugh+p.Worry+p.Sad;
                if (sum > 1.001f || sum < -.001f) throw new Exception("Expression weights exceed one face");
            }
            Check(maxHeadStep < 130f / fps, fps + "fps interrupted motion has bounded velocity: " + maxHeadStep);
            Check(maxWeightStep < 6.5f / fps, fps + "fps interrupted face has bounded blend: " + maxWeightStep);
            Check(p.Worry > .99f && p.Happy < .001f && p.Laugh < .001f, fps + "fps worry settles without stale smiling/laughter");
            var head = p.Head; var eyes = p.Eyes; float time = p.Clock; var lids = p.Lids;
            p.Step(KingVanguardCharacter.Activity.Excited,0,.8f,Vector2.one,1,1,1,2);
            Check(head == p.Head && eyes == p.Eyes && time == p.Clock && lids == p.Lids, fps + "fps zero-time activity switch does not jump");
            for (int i = 0; i < fps; i++) Step(KingVanguardCharacter.Activity.Sad,1,Vector2.zero);
            Check(p.Sad > .99f && p.Head.y < -9, fps + "fps disappointment settles into gentle slump");
            p.Reset(); Check(p.Laugh == 0 && p.Head == Vector4.zero && p.Eyes == Vector2.zero, fps + "fps board reset clears all motion");
        }
        var shader = Resources.Load<Shader>("KingVanguard/Interactive/KingVanguardPainted");
        Check(shader && !ShaderUtil.ShaderHasError(shader), "expression morph shader compiles");
        report.AppendLine(KingImpactLaughChecks.Run());
        string root=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
        if(root.EndsWith("KingVanguard-RigPreview-20261004"))root=Path.GetFullPath(root+"/../JellyGarden-Codex");
        File.WriteAllText(root+"/Design/KingVanguard-Lively-20261004/Review/motion-checks.txt", report.ToString());
        Debug.Log("KING_LIVELY_CHECKS_PASS"); return report.ToString();
    }
    public static void Batch() { Run(); }
}
