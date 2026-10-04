using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;
using RoyalThrone.Characters;
public static class KingBowChecks {
 public static string Run(){
  var report=new StringBuilder();
  void Check(bool ok,string text){report.AppendLine((ok?"PASS ":"FAIL ")+text);if(!ok)throw new Exception(text);}
  foreach(int fps in new[]{15,30,60,120}){
   var p=new KingVanguardPerformance();p.Reset();float dt=1f/fps,peak=0,maxStep=0;bool lifted=false;
   for(int f=0;f<fps*17;f++){
    float before=p.Bow;p.Step(KingVanguardCharacter.Activity.Idle,dt,0,Vector2.zero,0,0,0,0);
    peak=Mathf.Max(peak,p.Bow);maxStep=Mathf.Max(maxStep,Mathf.Abs(p.Bow-before));
    if(p.LookStage>=5&&p.Bow<.01f)lifted=true;
   }
   Check(peak>.99f,fps+"fps lower-board scan reaches approved full head bow");
   Check(lifted,fps+"fps head lifts before looking to frame corners");
   Check(maxStep<3.2f/fps,fps+"fps eased entry and recovery have bounded speed");
   p.Reset();for(int f=0;f<fps*4;f++)p.Step(KingVanguardCharacter.Activity.Idle,dt,0,Vector2.zero,0,0,0,0);
   float paused=p.Bow;p.Step(KingVanguardCharacter.Activity.Happy,0,.2f,Vector2.one,0,1,0,1);
   Check(p.Bow==paused,fps+"fps zero-time callback preserves bow pose");
   for(int f=0;f<fps;f++)p.Step(KingVanguardCharacter.Activity.Idle,dt,0,Vector2.one*.5f,0,0,0,2);
   Check(p.Bow<.01f&&p.LookStage==-1,fps+"fps fresh player input smoothly retires head bow");
   for(int f=0;f<fps*2;f++)p.Step(KingVanguardCharacter.Activity.Idle,dt,0,new Vector2(-.5f,-.6f),0,0,0,3);
   Check(p.Bow>.84f,fps+"fps downward hint prompts attentive head bend");
   for(int f=0;f<fps;f++)p.Step(KingVanguardCharacter.Activity.Laughing,dt,.3f,Vector2.zero,0,0,.8f,0);
   Check(p.Bow<.01f&&p.Laugh>.7f,fps+"fps impact reaction interrupts bow without stale pose");
   p.Reset();Check(p.Bow==0,fps+"fps board reset clears bow state");
  }
  Check(KingVanguardBowMesh.Points.Length==KingVanguardFaceMesh.Neutral.Length,"bow and expression vertices correspond one to one");
  var shader=Resources.Load<Shader>("KingVanguard/Interactive/KingVanguardPainted");
  Check(shader&&!ShaderUtil.ShaderHasError(shader),"seven-pose shader compiles");
  Check(Resources.Load<Texture2D>("KingVanguard/Interactive/King-Curious-Bow"),"approved bowed-head artwork imports");
  string root=Path.GetFullPath(Application.dataPath+"/..");
  if(root.EndsWith("KingVanguard-RigPreview-20261004"))root=Path.GetFullPath(root+"/../JellyGarden-Codex");
  File.WriteAllText(root+"/Design/KingVanguard-Bow-20261004/Review/bow-checks.txt",report.ToString());
  Debug.Log("KING_BOW_CHECKS_PASS");return report.ToString();
 }
 public static void Batch(){Run();}
}
