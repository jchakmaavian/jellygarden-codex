using System;
using System.IO;
using System.Text;
using Spine;
using Spine.Unity;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using RoyalThrone.Characters;

public static class KingImpactLaughChecks {
 public static string Run(){
  string reportRoot=Path.GetFullPath(Path.Combine(Application.dataPath,".."));if(reportRoot.EndsWith("KingVanguard-RigPreview-20261004"))reportRoot=Path.GetFullPath(Path.Combine(reportRoot,"../JellyGarden-Codex"));
  string initialReport=Path.Combine(reportRoot,"Design/KingVanguard-InteractivePortrait-20261004/Review/rig-checks.txt");Directory.CreateDirectory(Path.GetDirectoryName(initialReport));File.WriteAllText(initialReport,"RUNNING "+DateTime.UtcNow.ToString("o")+"\n");
  var report=new StringBuilder();int frames=0,posed=0;
  void Check(bool ok,string text){if(!ok)throw new Exception(text);report.AppendLine("PASS "+text);}
  Check(KingVanguardFeedbackQueue.FromImpactStrength(.45f)==KingVanguardFeedbackQueue.Feedback.None,"ordinary match is not a laugh");
  Check(KingVanguardFeedbackQueue.FromImpactStrength(.6f)==KingVanguardFeedbackQueue.Feedback.Powerup,"creation keeps normal cheer");
  foreach(float strength in new[]{.8f,1f,1.4f})Check(KingVanguardFeedbackQueue.FromImpactStrength(strength)==KingVanguardFeedbackQueue.Feedback.Impact,"rocket/TNT/huge impact "+strength+" selects laugh");
  var queue=new KingVanguardFeedbackQueue();queue.Offer(KingVanguardFeedbackQueue.Feedback.Match,0);queue.Accepted(0);
  queue.Offer(KingVanguardFeedbackQueue.Feedback.Impact,.1f);queue.Offer(KingVanguardFeedbackQueue.Feedback.Match,.2f);
  Check(queue.TryGet(.2f,out var feedback)&&feedback==KingVanguardFeedbackQueue.Feedback.Impact,"impact outranks match and bypasses normal cheer spacing");
  queue.Accepted(.2f);Check(!queue.TryGet(.3f,out feedback),"accepted chain has no backlog");
  queue.Offer(KingVanguardFeedbackQueue.Feedback.Impact,1);Check(!queue.TryGet(2.3f,out feedback),"stale impact expires");
  var scene=EditorSceneManager.NewPreviewScene();
  try {
   foreach(int fps in new[]{15,30,60}){
    var go=new GameObject("Impact test "+fps,typeof(RectTransform),typeof(Canvas));SceneManager.MoveGameObjectToScene(go,scene);var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.referencePixelsPerUnit=100;
    var data=AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>("Assets/Characters/KingVenguard/V2/Spine/King_Vanguard_SkeletonData.asset");var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Characters/KingVenguard/V2/Materials/KingVanguard_SkeletonGraphic_StraightAlpha.mat");
    var graphic=SkeletonGraphic.NewSkeletonGraphicGameObject(data,go.transform,material);graphic.Initialize(true);KingVanguardFaceRig.Ensure(graphic);
    var king=go.AddComponent<KingVanguardCharacter>();var so=new SerializedObject(king);so.FindProperty("skeletonGraphic").objectReferenceValue=graphic;so.ApplyModifiedPropertiesWithoutUndo();king.ResetToIdle();
    Check(king.PlayMatchAcknowledgement()&&king.PlayImpactReaction(),fps+"fps impact interrupts small acknowledgement");
    var art=graphic.GetComponent<KingVanguardPaintedRig>();Check(art!=null,"new interactive art rig is installed");
    float jawMin=99,jawMax=-99;int active=0;
    for(int i=0;i<fps*3;i++){
     graphic.Update(i==0?0:1f/fps);graphic.LateUpdate();frames++;
     if(art.LaughWeight>.02f){
      posed++;active++;
      if(!art.IsReady||art.Portrait.mainTexture.name!="King-Neutral")throw new Exception("New character artwork unavailable");
      if(graphic.color.a>.001f)throw new Exception("Old character style is visible");
      if(graphic.GetComponent<KingVanguardLaughAudio>())throw new Exception("Laughter audio was reintroduced");
      jawMin=Mathf.Min(jawMin,art.Portrait.JawOffset);jawMax=Mathf.Max(jawMax,art.Portrait.JawOffset);
      if(king.PlayImpactReaction())throw new Exception("Chain restarted active laugh");
     }
    }
    Check(active>fps&&jawMax-jawMin>.5f,fps+"fps silent reference-pose articulation");
    Check(king.CurrentActivity==KingVanguardCharacter.Activity.Idle&&art.LaughWeight<.02f,fps+"fps returns to clean idle");
    Check(!king.PlayImpactReaction(),fps+"fps impact cooldown blocks immediate replay");
    Check(king.PlayLaughter(),"explicit QA laugh starts");graphic.Update(.4f);graphic.LateUpdate();
    Check(king.PlaySad()&&!graphic.GetComponent<KingVanguardLaughter>().Active&&art.IsReady,"failure interrupts laughter while retaining new artwork");king.ResetToIdle();
    king.SetWorried(true);king.PlayLaughter();for(int i=0;i<fps*3;i++)graphic.Update(1f/fps);
    Check(king.CurrentActivity==KingVanguardCharacter.Activity.Worried&&art.LaughWeight<.02f,"laugh returns to low-move worry");
    king.ResetToIdle();king.PlayLaughter();graphic.Update(.4f);graphic.LateUpdate();Check(king.PlayRelief()&&!graphic.GetComponent<KingVanguardLaughter>().Active&&art.IsReady,"goal completion interrupts laugh");
    UnityEngine.Object.DestroyImmediate(go);
   }

   report.AppendLine("PASS "+frames+" frames; "+posed+" new-art frames; 15/30/60fps; all impact laugh checks passed.");
  }finally{EditorSceneManager.ClosePreviewScene(scene);}
  string project=Path.GetFullPath(Path.Combine(Application.dataPath,".."));if(project.EndsWith("KingVanguard-RigPreview-20261004"))project=Path.GetFullPath(Path.Combine(project,"../JellyGarden-Codex"));
  string output=Path.Combine(project,"Design/KingVanguard-InteractivePortrait-20261004/Review/rig-checks.txt");Directory.CreateDirectory(Path.GetDirectoryName(output));File.WriteAllText(output,report.ToString());Debug.Log(report.ToString());return report.ToString();
 }
}
