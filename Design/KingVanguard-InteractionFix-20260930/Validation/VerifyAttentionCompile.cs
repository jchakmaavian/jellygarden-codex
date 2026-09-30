public static class VerifyAttentionCompile { public static object Run(){
// Unity eval_file. Run in Play Mode after compilation, with the focused Game view.
// Isolated QA profile; real virtual-device events follow the production InputManager.
var qa=RoyalThrone.Testing.RoyalQaService.Instance;
if(!qa||!qa.Ready||qa.Busy||RoyalThrone.Testing.RoyalSave.Testing)throw new System.Exception("Requires a ready, inactive QA service and normal profile.");
string output="Design/KingVanguard-InteractionFix-20260930/Validation";System.IO.Directory.CreateDirectory(output);
string report=output+"/attention-checks.txt",trace=output+"/attention-trace.csv";
System.IO.File.WriteAllText(report,"RUNNING: real virtual pointer events, actual visible hint and live gameplay portrait.\n");
System.IO.File.WriteAllText(trace,"stage,time,source,gazeX,gazeY,influence,lean,tap,activity\n");
string stage="startup";bool ownsSession=false,complete=false;int checkedFrames=0;
UnityEngine.InputSystem.Mouse mouse=null,oldMouse=UnityEngine.InputSystem.Mouse.current;
UnityEngine.InputSystem.Touchscreen touch=null,oldTouch=UnityEngine.InputSystem.Touchscreen.current;
var input=InputManager.Instance;var oldDevices=input.inputActionAsset.devices;
var culture=System.Globalization.CultureInfo.InvariantCulture;
RoyalThrone.Characters.KingVanguardCharacter king=null;RoyalThrone.Characters.KingVanguardAttention attention=null;
JellyGarden.Scripts.Juice.RoyalHintPresenter presenter=null;JellyGarden.Scripts.Square[] restoreBoard=null;
void Check(bool ok,string text){System.IO.File.AppendAllText(report,(ok?"PASS ":"FAIL ")+stage+": "+text+"\n");if(!ok)throw new System.Exception(text);}
void Begin(string name){stage=name;System.IO.File.AppendAllText(report,"STAGE "+name+"\n");}
RoyalThrone.Characters.KingVanguardCharacter Hud()
{
 foreach(var candidate in UnityEngine.Object.FindObjectsByType<RoyalThrone.Characters.KingVanguardCharacter>(UnityEngine.FindObjectsInactive.Include))
  for(var p=candidate.transform;p;p=p.parent)if(p.name=="Royal gameplay HUD"&&!candidate.IsAreaRig)return candidate;
 return null;
}
void Sample()
{
 if(!attention)return;
 var g=attention.Gaze;
 if(float.IsNaN(g.x)||float.IsNaN(g.y)||g.sqrMagnitude>1.001f||!(attention.Influence>=0&&attention.Influence<=1.001f)||!(UnityEngine.Mathf.Abs(attention.BodyLean)<=.66f)||!(attention.TapResponse>=0&&attention.TapResponse<=1.001f))Check(false,"bounded finite attention");
 checkedFrames++;
 System.IO.File.AppendAllText(trace,stage+","+UnityEngine.Time.realtimeSinceStartup.ToString("F3",culture)+","+attention.Source+","+g.x.ToString("F4",culture)+","+g.y.ToString("F4",culture)+","+attention.Influence.ToString("F4",culture)+","+attention.BodyLean.ToString("F4",culture)+","+attention.TapResponse.ToString("F4",culture)+","+king.CurrentActivity+"\n");
}
System.Collections.IEnumerator Observe(float seconds)
{
 float end=UnityEngine.Time.realtimeSinceStartup+seconds;
 while(UnityEngine.Time.realtimeSinceStartup<end){Sample();yield return null;}
 Sample();
}
System.Collections.IEnumerator Capture(string name)
{
 Check(!qa.Visible,"QA overlay is hidden for "+name+" screenshot");
 yield return new UnityEngine.WaitForEndOfFrame();
 var directory="Design/KingVanguard-InteractionFix-20260930/After";System.IO.Directory.CreateDirectory(directory);
 var texture=UnityEngine.ScreenCapture.CaptureScreenshotAsTexture();
 try{System.IO.File.WriteAllBytes(directory+"/Attention-"+name+".png",UnityEngine.ImageConversion.EncodeToPNG(texture));}
 finally{if(texture)UnityEngine.Object.Destroy(texture);}
}
System.Collections.IEnumerator Request(string command)
{
 int failed=qa.Failed,completed=qa.Completed;qa.Request(command);qa.Visible=false;
 float end=UnityEngine.Time.realtimeSinceStartup+60;
 while(qa.Busy&&UnityEngine.Time.realtimeSinceStartup<end)yield return null;
 Check(!qa.Busy&&qa.Failed==failed&&qa.Completed==completed+1,command+" completed");
 yield return null;
}
void MouseAt(float x,float y)
{
 UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse,new UnityEngine.InputSystem.LowLevel.MouseState{position=new UnityEngine.Vector2(UnityEngine.Screen.width*x,UnityEngine.Screen.height*y)});
}
void TouchAt(UnityEngine.InputSystem.TouchPhase phase,float x,float y)
{
 UnityEngine.InputSystem.InputSystem.QueueStateEvent(touch,new UnityEngine.InputSystem.LowLevel.TouchState{touchId=73,phase=phase,position=new UnityEngine.Vector2(UnityEngine.Screen.width*x,UnityEngine.Screen.height*y)});
}
System.Collections.IEnumerator Cases()
{
 Check(UnityEngine.Application.isFocused,"Game view/application focused for input observation");
 qa.StartSession();ownsSession=RoyalThrone.Testing.RoyalSave.Testing;Check(ownsSession,"isolated QA profile started");qa.Visible=false;
 yield return Request("Practice board");
 var lm=JellyGarden.Scripts.LevelManager.THIS;lm.StopAllCoroutines();JellyGarden.Scripts.AI.THIS.StopAllCoroutines();JellyGarden.Scripts.AI.THIS.allowShowTip=false;
 presenter=JellyGarden.Scripts.AI.THIS.GetComponent<JellyGarden.Scripts.Juice.RoyalHintPresenter>();presenter?.Hide();
 king=Hud();Check(king&&king.isActiveAndEnabled,"live main-HUD portrait found");
 attention=king.GetComponent<RoyalThrone.Characters.KingVanguardAttention>();Check(attention&&attention.enabled,"FaceRig mounted the HUD attention component without fixture assistance");
 king.ResetToIdle();lm.Limit=99;yield return Observe(.2f);
 mouse=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Mouse>("Vanguard attention QA mouse");
 touch=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Touchscreen>("Vanguard attention QA touch");
 input.inputActionAsset.devices=new UnityEngine.InputSystem.InputDevice[]{mouse,touch};
 MouseAt(.5f,.84f);yield return Observe(.15f);
 Begin("moving mouse");MouseAt(.85f,.84f);yield return Observe(.45f);
 Check(attention.Source==RoyalThrone.Characters.KingVanguardAttention.TargetSource.Mouse&&attention.Gaze.x>.2f&&attention.Influence>.8f&&attention.BodyLean>.06f,"real mouse movement supplies smoothed gaze and subtle body lean");
 yield return Capture("Mouse");
 Begin("mouse to actual hint");
 var plan=JellyGarden.Scripts.Juice.RoyalHintSolver.Find(lm);Check(plan!=null,"practice board has a real legal hint");
 JellyGarden.Scripts.Juice.RoyalHintSettings.Enabled=true;
 if(!presenter)presenter=JellyGarden.Scripts.AI.THIS.gameObject.AddComponent<JellyGarden.Scripts.Juice.RoyalHintPresenter>();
 Check(presenter.Show(plan),"the actual hint presenter shows its selected pieces");
 Check(JellyGarden.Scripts.Juice.RoyalHintPresenter.TryGetCurrentFocus(lm,out var focus),"visible valid hint exposes focus");
 var expected=plan.Second?(plan.First.transform.position+plan.Second.transform.position)*.5f:plan.First.transform.position;
 Check((focus-expected).sqrMagnitude<1e-8f,"hint focus is exactly the actual swap-pair midpoint/tap piece");
 yield return Observe(.2f);Check(attention.Source==RoyalThrone.Characters.KingVanguardAttention.TargetSource.Mouse,"recent mouse wins over a visible hint");
 yield return Observe(1.3f);Check(attention.Source==RoyalThrone.Characters.KingVanguardAttention.TargetSource.Hint&&attention.Gaze.y<-.1f,"stationary mouse expires and the King looks toward the hint on the board");
 yield return Capture("Hint");
 Begin("touch priority");int moves=lm.Limit;
 TouchAt(UnityEngine.InputSystem.TouchPhase.Began,.15f,.84f);yield return Observe(.12f);
 Check(input.IsTouching&&attention.Source==RoyalThrone.Characters.KingVanguardAttention.TargetSource.Touch&&attention.TapResponse>.2f,"real touchscreen press wins and produces a bounded tap response");
 MouseAt(.9f,.84f);yield return Observe(.3f);
 Check(attention.Source==RoyalThrone.Characters.KingVanguardAttention.TargetSource.Touch&&attention.Gaze.x<-.2f,"held touchscreen stays dominant while the mouse moves elsewhere");
 yield return Capture("Touch");
 TouchAt(UnityEngine.InputSystem.TouchPhase.Ended,.15f,.84f);yield return Observe(.12f);
 Check(!input.IsTouching&&lm.Limit==moves,"input is released normally and portrait-area touch spent no game move");
 Check(presenter.Show(plan),"valid hint can resume after the player releases");yield return Observe(.6f);
 Check(attention.Source==RoyalThrone.Characters.KingVanguardAttention.TargetSource.Hint,"touch release does not revive the old desktop target");
 Begin("invalid hint and source idle");int colour=plan.First.color;plan.First.SetColor((colour+1)%5);
 Check(!JellyGarden.Scripts.Juice.RoyalHintPresenter.TryGetCurrentFocus(lm,out focus),"changed selected-piece colour immediately invalidates focus before presenter LateUpdate");
 plan.First.SetColor(colour);presenter.Hide();yield return Observe(1f);
 Check(attention.Source==RoyalThrone.Characters.KingVanguardAttention.TargetSource.None&&attention.Influence<.03f,"absent target smoothly restores source eye animation");
 Begin("authored reaction priority");MouseAt(.8f,.8f);yield return Observe(.4f);Check(attention.Influence>.7f,"external gaze established before reaction");
 Check(king.PlayHappy(),"original happy activity starts");yield return Observe(.4f);
 Check(attention.Source==RoyalThrone.Characters.KingVanguardAttention.TargetSource.None&&attention.Influence<.03f&&UnityEngine.Mathf.Abs(attention.BodyLean)<.02f,"authored reaction retires external gaze/body smoothly");
 yield return Observe(3.7f);Check(!king.IsReacting&&attention.Source==RoyalThrone.Characters.KingVanguardAttention.TargetSource.None,"reaction completion does not replay stale pointer attention");
 Begin("worry strength");lm.Limit=3;yield return Observe(.3f);MouseAt(.2f,.8f);yield return Observe(.55f);
 Check(king.IsWorried&&attention.Source==RoyalThrone.Characters.KingVanguardAttention.TargetSource.Mouse&&attention.Influence>.2f&&attention.Influence<.34f&&UnityEngine.Mathf.Abs(attention.BodyLean)<.21f,"worry keeps source expression priority with reduced attention influence");
 Begin("pause visibility reset");lm.gameStatus=JellyGarden.Scripts.GameState.Pause;yield return Observe(.2f);
 Check(attention.Influence==0&&attention.BodyLean==0&&attention.TapResponse==0,"pause/hidden HUD clears all external response");
 lm.gameStatus=JellyGarden.Scripts.GameState.Playing;lm.Limit=99;yield return Observe(.2f);
 Check(attention.Source==RoyalThrone.Characters.KingVanguardAttention.TargetSource.None,"resume requires fresh pointer motion");
 Begin("board identity reset");MouseAt(.75f,.8f);yield return Observe(.4f);Check(attention.Influence>.6f,"attention established before board-scope probe");
 restoreBoard=lm.squaresArray;lm.squaresArray=(JellyGarden.Scripts.Square[])restoreBoard.Clone();yield return Observe(.1f);
 Check(attention.Influence==0&&attention.Source==RoyalThrone.Characters.KingVanguardAttention.TargetSource.None,"new board-array identity clears attention independently of QA/pause gates");
 lm.squaresArray=restoreBoard;restoreBoard=null;yield return Observe(.1f);
 Begin("QA gate");MouseAt(.25f,.8f);yield return Observe(.4f);qa.Visible=true;yield return Observe(.1f);
 Check(attention.Influence==0&&attention.Source==RoyalThrone.Characters.KingVanguardAttention.TargetSource.None,"QA overlay blocks player attention input");qa.Visible=false;
 Check(checkedFrames>30,"bounded finite gaze/influence/lean/tap across "+checkedFrames+" observed frames");complete=true;
}
System.Collections.IEnumerator Restore()
{
 yield return qa.EndSession();Check(!RoyalThrone.Testing.RoyalSave.Testing,"normal player profile restored");
 System.IO.File.AppendAllText(report,complete?"ALL ATTENTION CHECKS PASSED\n":"RUN FAILED; profile restored\n");
}
System.Collections.IEnumerator Guarded()
{
 var stack=new System.Collections.Generic.Stack<System.Collections.IEnumerator>();stack.Push(Cases());
 try
 {
  while(stack.Count>0)
  {
   var item=stack.Peek();bool next=false;System.Exception error=null;
   try{next=item.MoveNext();}catch(System.Exception ex){error=ex;}
   if(error!=null){System.IO.File.AppendAllText(report,"ERROR "+error+"\n");UnityEngine.Debug.LogException(error);yield break;}
   if(!next){stack.Pop();(item as System.IDisposable)?.Dispose();continue;}
   if(item.Current is System.Collections.IEnumerator child&&!(child is UnityEngine.CustomYieldInstruction))stack.Push(child);else yield return item.Current;
  }
 }
 finally
 {
  while(stack.Count>0)(stack.Pop() as System.IDisposable)?.Dispose();
  if(restoreBoard!=null&&JellyGarden.Scripts.LevelManager.THIS)JellyGarden.Scripts.LevelManager.THIS.squaresArray=restoreBoard;
  presenter?.Hide();input.inputActionAsset.devices=oldDevices;
  if(mouse!=null&&mouse.added)UnityEngine.InputSystem.InputSystem.RemoveDevice(mouse);
  if(touch!=null&&touch.added)UnityEngine.InputSystem.InputSystem.RemoveDevice(touch);
  if(oldMouse!=null&&oldMouse.added)oldMouse.MakeCurrent();if(oldTouch!=null&&oldTouch.added)oldTouch.MakeCurrent();
  qa.Loop=false;System.IO.File.AppendAllText(report,complete?"CHECKS COMPLETE; restoring profile\n":"INCOMPLETE; restoring profile\n");
  if(ownsSession)qa.StartCoroutine(Restore());
 }
}
qa.StartCoroutine(Guarded());return "Started attention QA. Poll Validation/attention-checks.txt; profile and virtual devices restore in finally.";

}}