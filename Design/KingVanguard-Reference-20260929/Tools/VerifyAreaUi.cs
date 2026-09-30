var qa=RoyalThrone.Testing.RoyalQaService.Instance;
qa.StartSession();qa.Visible=false;qa.Counters=false;
if(!RoyalThrone.Testing.RoyalSave.Testing)throw new System.Exception("QA required");
var home=UnityEngine.Object.FindAnyObjectByType<RoyalThrone.Presentation.RoyalHomeScreen>();
string report="Design/KingVanguard-Reference-20260929/Validation/live-area-checks.txt";
System.IO.File.WriteAllText(report,"RUNNING area-completion presentation on isolated fixture\n");
void Check(bool ok,string message){System.IO.File.AppendAllText(report,(ok?"PASS ":"FAIL ")+message+"\n");if(!ok)throw new System.Exception(message);}
System.Collections.IEnumerator Run(){
 bool complete=false;
 try{
  home.ClosePanel();
  RoyalThrone.Testing.RoyalSave.SetInt(RoyalThrone.Presentation.RoyalAreas.CurrentKey,1);
  RoyalThrone.Testing.RoyalSave.SetInt(RoyalThrone.Presentation.RoyalAreas.EnteredKey,1);
  RoyalThrone.Testing.RoyalSave.SetInt(RoyalThrone.Presentation.RoyalAreas.DoneKey,63);
  home.RefreshProgress();yield return null;
  var view=home.AreaView;home.OpenPanel(RoyalThrone.Presentation.RoyalAreaView.Chest);
  yield return new UnityEngine.WaitForSecondsRealtime(1.5f);
  Check(view.StageStep==1,"completed area opens original chest ceremony");
  view.StageButton.onClick.Invoke();
  float deadline=UnityEngine.Time.realtimeSinceStartup+12;
  while(view.StageStep!=2&&UnityEngine.Time.realtimeSinceStartup<deadline)yield return null;
  Check(view.StageStep==2,"chest reward stage opens");
  view.StageButton.onClick.Invoke();
  while(view.StageStep!=4&&UnityEngine.Time.realtimeSinceStartup<deadline)yield return null;
  Check(view.StageStep==4,"ceremony reaches king celebration");
  yield return new UnityEngine.WaitForSecondsRealtime(1.6f);
  RoyalThrone.Characters.KingVanguardCharacter king=null;
  foreach(var k in UnityEngine.Object.FindObjectsByType<RoyalThrone.Characters.KingVanguardCharacter>(UnityEngine.FindObjectsSortMode.None))if(k.IsAreaRig)king=k;
  Check(king&&king.CurrentAnimation.StartsWith("king_drill_win_clap"),"live ceremony plays imported clap family");
  UnityEngine.ScreenCapture.CaptureScreenshot("Design/KingVanguard-Reference-20260929/Validation/Area-celebration.png");
  yield return new UnityEngine.WaitForSecondsRealtime(.15f);home.ClosePanel();complete=true;
 }finally{
  System.IO.File.AppendAllText(report,complete?"PASS live area character check finished\n":"INCOMPLETE - inspect preceding assertion\n");
  qa.StartCoroutine(qa.EndSession());
 }
}
qa.StartCoroutine(Run());return "Started area celebration QA";
