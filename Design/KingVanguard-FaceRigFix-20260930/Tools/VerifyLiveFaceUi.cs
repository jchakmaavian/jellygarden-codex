var qa=RoyalThrone.Testing.RoyalQaService.Instance;
if(!RoyalThrone.Testing.RoyalSave.Testing||qa.Busy)throw new System.Exception("Open isolated settled HUD first");
RoyalThrone.Characters.KingVanguardCharacter king=null;
foreach(var k in UnityEngine.Object.FindObjectsByType<RoyalThrone.Characters.KingVanguardCharacter>(UnityEngine.FindObjectsSortMode.None)){
 var parent=k.transform;while(parent){if(parent.name=="Royal gameplay HUD"){king=k;break;}parent=parent.parent;}
}
if(!king)throw new System.Exception("Gameplay King missing");
string report="Design/KingVanguard-FaceRigFix-20260930/Validation/live-hud.txt";
System.IO.File.WriteAllText(report,"RUNNING actual in-game HUD face rendering; presentation probes in isolated QA.\n");
void Check(bool ok,string message){System.IO.File.AppendAllText(report,(ok?"PASS ":"FAIL ")+message+"\n");if(!ok)throw new System.Exception(message);}
System.Collections.IEnumerator Run(){
 try{
  king.ResetToIdle();yield return new UnityEngine.WaitForSecondsRealtime(1.2f);
  var mapper=king.Graphic.GetComponent<RoyalThrone.Characters.KingVanguardFaceRig>();
  Check(mapper&&mapper.IsRegistered&&mapper.LastMappedVertices>0,"live SkeletonGraphic uses animated face mapping");
  Check(mapper.LastAmbiguousVertices==0,"live face ownership has no ambiguity");
  Check(king.Graphic.canvas.referencePixelsPerUnit==100,"HUD canvas scale path exercised at 100 pixels per unit");
  UnityEngine.ScreenCapture.CaptureScreenshot("Design/KingVanguard-FaceRigFix-20260930/After/Gameplay-HUD.png");
  yield return new UnityEngine.WaitForSecondsRealtime(.3f);
  Check(king.PlayHappy(),"happy activity accepted on actual HUD");
  float end=UnityEngine.Time.realtimeSinceStartup+4f;int frames=0;
  while(UnityEngine.Time.realtimeSinceStartup<end){
   yield return null;frames++;
   if(mapper.LastMappedVertices<=0||mapper.LastAmbiguousVertices!=0)throw new System.Exception("Live happy face mapping lost registration");
  }
  Check(frames>20,"happy transition retains facial mapping throughout "+frames+" live frames");
  Check(king.CurrentActivity==RoyalThrone.Characters.KingVanguardCharacter.Activity.Idle,"happy returns to idle in game");
  System.IO.File.AppendAllText(report,"ALL LIVE HUD FACE CHECKS PASSED\n");
 }finally{qa.StartCoroutine(qa.EndSession());}
}
qa.StartCoroutine(Run());return "Checking live HUD and restoring the normal player profile when finished.";
