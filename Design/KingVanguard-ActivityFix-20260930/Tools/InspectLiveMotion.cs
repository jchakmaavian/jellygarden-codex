var output="Design/KingVanguard-ActivityFix-20260930/Before";
System.IO.Directory.CreateDirectory(output);
var qa=RoyalThrone.Testing.RoyalQaService.Instance;
var report=new System.Text.StringBuilder();
RoyalThrone.Characters.KingVanguardCharacter king=null;
foreach(var candidate in UnityEngine.Object.FindObjectsByType<RoyalThrone.Characters.KingVanguardCharacter>(UnityEngine.FindObjectsSortMode.None)){
 var parent=candidate.transform;
 while(parent){if(parent.name=="Royal gameplay HUD"){king=candidate;break;}parent=parent.parent;}
}
if(!king)throw new System.Exception("No active gameplay King");
System.Collections.IEnumerator Sample(){
 for(int frame=0;frame<6;frame++){
  var entry=king.Graphic.AnimationState.GetCurrent(0);
  report.AppendLine("frame="+frame+" clip="+entry.Animation.Name+" time="+entry.TrackTime+" duration="+entry.Animation.Duration+" activity="+king.CurrentActivity+" freeze="+king.Graphic.freeze+" timeScale="+king.Graphic.timeScale+" mix="+entry.MixTime+"/"+entry.MixDuration);
  UnityEngine.ScreenCapture.CaptureScreenshot(output+"/Live-"+frame.ToString("D2")+".png");
  yield return new UnityEngine.WaitForSecondsRealtime(.5f);
 }
 System.IO.File.WriteAllText(output+"/live-motion.txt",report.ToString());
}
qa.StartCoroutine(Sample());return "Started read-only six-frame live King sampling.";
