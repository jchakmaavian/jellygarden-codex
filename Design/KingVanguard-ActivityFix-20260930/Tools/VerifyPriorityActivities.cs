var qa=RoyalThrone.Testing.RoyalQaService.Instance;
if(!RoyalThrone.Testing.RoyalSave.Testing)throw new System.Exception("QA required");
string report="Design/KingVanguard-ActivityFix-20260930/Validation/live-activity-checks.txt";
System.IO.File.WriteAllText(report,"RUNNING: assisted fixtures exercise real gameplay event paths; no natural input claim.\n");
void Check(bool ok,string message){System.IO.File.AppendAllText(report,(ok?"PASS ":"FAIL ")+message+"\n");if(!ok)throw new System.Exception(message);}
RoyalThrone.Characters.KingVanguardCharacter Hud(){
 foreach(var k in UnityEngine.Object.FindObjectsByType<RoyalThrone.Characters.KingVanguardCharacter>(UnityEngine.FindObjectsSortMode.None)){
  var t=k.transform;while(t){if(t.name=="Royal gameplay HUD")return k;t=t.parent;}
 }return null;
}
System.Collections.IEnumerator Run(){
 bool complete=false;
 try{
  qa.Level=12;yield return qa.OpenLevel();qa.Visible=false;
  var lm=JellyGarden.Scripts.LevelManager.THIS;var king=Hud();
  Check(king&&king.CurrentActivity==RoyalThrone.Characters.KingVanguardCharacter.Activity.Idle,"live HUD mounts fresh APK portrait");
  UnityEngine.ScreenCapture.CaptureScreenshot("Design/KingVanguard-ActivityFix-20260930/Validation/Gameplay-idle.png");
  yield return new UnityEngine.WaitForSecondsRealtime(.2f);
  lm.Limit=3;yield return new UnityEngine.WaitForSecondsRealtime(.4f);
  Check(king.CurrentAnimation=="k-1_kaygi","3 remaining moves enter source worry");
  yield return new UnityEngine.WaitForSecondsRealtime(6.1f);
  Check(king.CurrentAnimation=="k-1_kaygi_loop","worry entrance reaches source loop");
  UnityEngine.ScreenCapture.CaptureScreenshot("Design/KingVanguard-ActivityFix-20260930/Validation/Gameplay-worry.png");
  yield return new UnityEngine.WaitForSecondsRealtime(.2f);
  lm.Limit=0;lm.CheckWinLose();yield return new UnityEngine.WaitForSecondsRealtime(1);
  Check(lm.gameStatus==JellyGarden.Scripts.GameState.PreFailed,"real zero-move check opens continuation offer");
  JellyGarden.Scripts.GUI.AnimationManager offer=null;
  foreach(var a in UnityEngine.Object.FindObjectsByType<JellyGarden.Scripts.GUI.AnimationManager>(UnityEngine.FindObjectsSortMode.None)){
   var animation=a.GetComponent<UnityEngine.Animation>();if(animation&&animation["bannerFailed"]!=null){offer=a;break;}
  }
  Check(offer!=null,"continuation offer found");
  offer.GoOnFailed();
  float until=UnityEngine.Time.realtimeSinceStartup+5;bool extra=false;
  while(UnityEngine.Time.realtimeSinceStartup<until){if(king.CurrentAnimation=="k-1_sevinme_NEW"){extra=true;break;}yield return null;}
  Check(extra&&lm.Limit>0,"actual extra-move grant triggers source extra-move celebration");
  UnityEngine.ScreenCapture.CaptureScreenshot("Design/KingVanguard-ActivityFix-20260930/Validation/Extra-moves.png");
  yield return new UnityEngine.WaitForSecondsRealtime(.2f);
  yield return new UnityEngine.WaitForSecondsRealtime(3.2f);
  Check(!king.IsWorried,"extra moves clear low-move context");
  foreach(var target in lm.targetObject)target.countCollected=target.targetCount;
  lm.Limit=1;lm.CheckWinLose();yield return null;yield return null;
  Check(king.CurrentAnimation=="Relax_1","real goal completion triggers source relief before finale");
  UnityEngine.ScreenCapture.CaptureScreenshot("Design/KingVanguard-ActivityFix-20260930/Validation/Goals-complete.png");
  yield return new UnityEngine.WaitForSecondsRealtime(.2f);
  until=UnityEngine.Time.realtimeSinceStartup+35;
  while(lm.gameStatus!=JellyGarden.Scripts.GameState.Win&&UnityEngine.Time.realtimeSinceStartup<until)yield return null;
  Check(lm.gameStatus==JellyGarden.Scripts.GameState.Win,"win sequence reaches result");
  yield return new UnityEngine.WaitForSecondsRealtime(.7f);
  bool happy=false;foreach(var k in UnityEngine.Object.FindObjectsByType<RoyalThrone.Characters.KingVanguardCharacter>(UnityEngine.FindObjectsSortMode.None))if(k.CurrentAnimation=="k-1sevinme_6")happy=true;
  Check(happy,"win result plays original serialized happy clip");
  UnityEngine.ScreenCapture.CaptureScreenshot("Design/KingVanguard-ActivityFix-20260930/Validation/Win-result.png");
  yield return new UnityEngine.WaitForSecondsRealtime(.2f);
  complete=true;
 }finally{
  System.IO.File.AppendAllText(report,complete?"PASS live activity flow finished\n":"INCOMPLETE - inspect preceding assertion\n");
  qa.StartCoroutine(qa.EndSession());
 }
}
qa.StartCoroutine(Run());return "Started live activity verification.";
