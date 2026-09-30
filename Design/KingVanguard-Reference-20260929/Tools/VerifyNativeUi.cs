var qa=RoyalThrone.Testing.RoyalQaService.Instance;
if(!qa||!qa.Ready)throw new System.Exception("Game not ready");
qa.StartSession();qa.Visible=false;qa.Counters=false;
if(!RoyalThrone.Testing.RoyalSave.Testing)throw new System.Exception("QA required");
var home=UnityEngine.Object.FindAnyObjectByType<RoyalThrone.Presentation.RoyalHomeScreen>();
string report="Design/KingVanguard-Reference-20260929/Validation/live-native-ui-checks.txt";
System.IO.File.WriteAllText(report,"RUNNING live leaderboard native rig\n");
void Check(bool ok,string message){System.IO.File.AppendAllText(report,(ok?"PASS ":"FAIL ")+message+"\n");if(!ok)throw new System.Exception(message);}
System.Collections.IEnumerator Run(){
 bool complete=false;
 try{
  home.ClosePanel();yield return new UnityEngine.WaitForSecondsRealtime(.5f);home.OpenPanel("leagues");
  yield return new UnityEngine.WaitForSecondsRealtime(.65f);
  var view=UnityEngine.Object.FindAnyObjectByType<RoyalThrone.Characters.Activities.NativeKingDrillUiSlot>();
  Check(view&&view.IsShowing,"leaderboard shows original full-body rig");
  Check(view.Target&&view.Target.IsCreated()&&view.Target.width==448&&view.Target.height==512,"bounded transparent render target");
  Check(!view.RenderCamera.enabled&&view.Actor.gameObject.activeInHierarchy,"camera renders only explicitly");
  Check(view.Actor.Animator.GetCurrentAnimatorStateInfo(0).IsName("KingWinAnim"),"source Win clip on page opening");
  UnityEngine.ScreenCapture.CaptureScreenshot("Design/KingVanguard-Reference-20260929/Validation/Leaderboard-fullbody.png");
  var savedActor=view.Actor;var savedCamera=view.RenderCamera;
  float idleDeadline=UnityEngine.Time.realtimeSinceStartup+8;
  while(!view.Actor.Animator.GetCurrentAnimatorStateInfo(0).IsName("KingLoopAnim")&&UnityEngine.Time.realtimeSinceStartup<idleDeadline)yield return null;
  Check(view.Actor.Animator.GetCurrentAnimatorStateInfo(0).IsName("KingLoopAnim"),"source Win returns to Idle");
  home.ClosePanel();yield return null;yield return null;
  Check(!view.IsShowing&&!view.Target.IsCreated()&&!view.Actor.gameObject.activeInHierarchy,"hidden page suspends animation and frees GPU target");
  home.OpenPanel("leagues");yield return new UnityEngine.WaitForSecondsRealtime(.5f);
  Check(view.IsShowing&&view.Actor==savedActor&&view.RenderCamera==savedCamera,"reopen reuses actor/camera");
  home.ClosePanel();yield return new UnityEngine.WaitForSecondsRealtime(.2f);
  UnityEngine.ScreenCapture.CaptureScreenshot("Design/KingVanguard-Reference-20260929/Validation/Home-avatar.png");
  complete=true;
 }finally{
  System.IO.File.AppendAllText(report,complete?"PASS live full-body UI checks finished\n":"INCOMPLETE - inspect preceding assertion\n");
  qa.StartCoroutine(qa.EndSession());
 }
}
qa.StartCoroutine(Run());return "Started native leaderboard QA";
