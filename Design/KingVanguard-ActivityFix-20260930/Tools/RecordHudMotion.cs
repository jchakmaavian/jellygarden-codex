// Visual playback review on the actual HUD; direct presentation actions are labelled in the trace.
var qa=RoyalThrone.Testing.RoyalQaService.Instance;
var level=JellyGarden.Scripts.LevelManager.THIS;int originalMoves=level.Limit;
if(!RoyalThrone.Testing.RoyalSave.Testing||qa.Busy)throw new System.Exception("Open an isolated settled level first");
RoyalThrone.Characters.KingVanguardCharacter king=null;
foreach(var k in UnityEngine.Object.FindObjectsByType<RoyalThrone.Characters.KingVanguardCharacter>(UnityEngine.FindObjectsSortMode.None)){
 var p=k.transform;while(p){if(p.name=="Royal gameplay HUD"){king=k;break;}p=p.parent;}
}
if(!king)throw new System.Exception("Gameplay portrait missing");
var output="Design/KingVanguard-ActivityFix-20260930/After";
var frames=output+"/MotionFrames";System.IO.Directory.CreateDirectory(frames);
var trace=new System.Text.StringBuilder("frame,realtime,stage,clip,trackTime\n");
var concat=new System.Text.StringBuilder();
var invariant=System.Globalization.CultureInfo.InvariantCulture;
int index=0;double lastTime=-1;string lastFile=null;
System.Collections.IEnumerator Capture(string stage,float duration){
 float end=UnityEngine.Time.realtimeSinceStartup+duration;
 while(UnityEngine.Time.realtimeSinceStartup<end){
  yield return new UnityEngine.WaitForEndOfFrame();
  double now=UnityEngine.Time.realtimeSinceStartupAsDouble;
  var screen=UnityEngine.ScreenCapture.CaptureScreenshotAsTexture();
  int height=(UnityEngine.Mathf.RoundToInt(screen.height*.22f)/2)*2;
  var crop=new UnityEngine.Texture2D(screen.width,height,UnityEngine.TextureFormat.RGB24,false);
  crop.SetPixels(screen.GetPixels(0,screen.height-height,screen.width,height));crop.Apply();
  string file="frame-"+index.ToString("D4")+".png";
  System.IO.File.WriteAllBytes(frames+"/"+file,crop.EncodeToPNG());
  if(lastFile!=null)concat.AppendLine("file 'MotionFrames/"+lastFile+"'\nduration "+(now-lastTime).ToString("F6",invariant));
  lastTime=now;lastFile=file;
  var entry=king.Graphic.AnimationState.GetCurrent(0);
  trace.AppendLine(index+","+now.ToString("F6",invariant)+","+stage+","+entry.Animation.Name+","+entry.TrackTime.ToString("F4",invariant));
  UnityEngine.Object.Destroy(screen);UnityEngine.Object.Destroy(crop);index++;
  yield return new UnityEngine.WaitForSecondsRealtime(.075f);
 }
}
System.Collections.IEnumerator Run(){
 try{
  king.ResetToIdle();yield return Capture("idle",2f);
  king.PlayMatchAcknowledgement();yield return Capture("match-acknowledgment",1.1f);
  king.PlayHappy();yield return Capture("powerup-happy",4f);
  level.Limit=3;king.SetWorried(true);yield return Capture("worry-entrance",6.2f);
  yield return Capture("worry-loop",1.8f);
  level.Limit=originalMoves;king.SetWorried(false);king.PlayExtraMoves();yield return Capture("extra-moves",3.2f);
  if(lastFile!=null)concat.AppendLine("file 'MotionFrames/"+lastFile+"'\nduration 0.083333\nfile 'MotionFrames/"+lastFile+"'");
  System.IO.File.WriteAllText(output+"/hud-motion-concat.txt",concat.ToString());
  System.IO.File.WriteAllText(output+"/hud-motion-trace.csv",trace.ToString());
  System.IO.File.WriteAllText(output+"/record-status.txt","PASS captured "+index+" timestamped frames from live HUD. Direct presentation playback; gameplay event tests are separate.\n");
 }finally{level.Limit=originalMoves;king.ResetToIdle();}
}
qa.StartCoroutine(Run());return "Recording timestamped HUD expression and framing review.";
