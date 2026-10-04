var qa=RoyalThrone.Testing.RoyalQaService.Instance;
if(!qa||!qa.Ready||qa.Busy||RoyalThrone.Testing.RoyalSave.Testing)throw new System.Exception("Ready normal profile required");
string output="Design/KingVanguard-InteractivePortrait-20261004/Review",review="Design/KingVanguard-InteractivePortrait-20261004/Review";
System.IO.Directory.CreateDirectory(output);System.IO.Directory.CreateDirectory(review);
string report=output+"/positive-checks.txt";System.IO.File.WriteAllText(report,"RUNNING\n");
bool owns=false,complete=false;int savedSound=RoyalThrone.Testing.RoyalSave.GetInt("Sound",1);float savedScale=UnityEngine.Time.timeScale;
RoyalThrone.Characters.KingVanguardCharacter king=null;
float lastImpactAt=-100,lastStrength=0;
System.Action<float> impact=(strength)=>{if(strength>=.75f){lastImpactAt=UnityEngine.Time.unscaledTime;lastStrength=strength;}};
void Check(bool ok,string message){System.IO.File.AppendAllText(report,(ok?"PASS ":"FAIL ")+message+"\n");if(!ok)throw new System.Exception(message);}
System.Collections.IEnumerator Wait(float seconds){yield return new UnityEngine.WaitForSecondsRealtime(seconds);}
System.Collections.IEnumerator Command(string name){
 int failed=qa.Failed;qa.Request(name);float end=UnityEngine.Time.realtimeSinceStartup+45;
 while(qa.Busy&&UnityEngine.Time.realtimeSinceStartup<end)yield return null;
 Check(!qa.Busy&&qa.Failed==failed,"QA "+name+" completed");qa.Visible=false;
}
System.Collections.IEnumerator Cases(){
 UnityEditor.EditorWindow.GetWindow(System.Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();yield return Wait(.25f);
 qa.StartSession();owns=RoyalThrone.Testing.RoyalSave.Testing;qa.Visible=false;yield return Command("Practice board");JellyGarden.Scripts.AI.THIS.allowShowTip=false;
 foreach(var candidate in UnityEngine.Object.FindObjectsByType<RoyalThrone.Characters.KingVanguardCharacter>(UnityEngine.FindObjectsInactive.Exclude))
  for(var parent=candidate.transform;parent;parent=parent.parent)if(parent.name=="Royal gameplay HUD"&&!candidate.IsAreaRig){king=candidate;break;}
 Check(king,"HUD King found");
 foreach(string pose in new[]{"Happy","Excited","ExtraMoves","Match","Relief","Worried","Sad"}){
  JellyGarden.Scripts.LevelManager.THIS.Limit=pose=="Worried"?3:99;king.ResetToIdle();bool accepted=true;
  switch(pose){case "Happy":accepted=king.PlayHappy();break;case "Excited":accepted=king.PlayExcited();break;case "ExtraMoves":accepted=king.PlayExtraMoves();break;case "Match":accepted=king.PlayMatchAcknowledgement();break;case "Relief":accepted=king.PlayRelief();break;case "Worried":king.SetWorried(true);break;case "Sad":accepted=king.PlaySad();break;}
  Check(accepted,pose+" accepted");yield return Wait(pose=="Match"?.25f:.8f);
  var art=king.Graphic.GetComponent<RoyalThrone.Characters.KingVanguardPaintedRig>();
  Check(art&&art.IsReady&&king.Graphic.color.a==0,pose+" renders matching replacement art");
  Check(king.CurrentActivity.ToString()==(pose=="Match"?"MatchAcknowledgement":pose),pose+" retains activity through pose");
  System.IO.File.AppendAllText(report,"INFO "+pose+" current="+king.CurrentActivity+" happy="+art.Portrait.HappyWeight+" excited="+art.Portrait.ExcitedWeight+"\n");
  Check(art.LaughWeight<.001f,pose+" does not use laughing face");
  if(pose=="Happy"||pose=="Match"||pose=="Relief")Check(art.Portrait.HappyWeight>.5f,pose+" uses happy face");
  if(pose=="Excited"||pose=="ExtraMoves")Check(art.Portrait.ExcitedWeight>.5f,pose+" uses excited face");
  UnityEngine.ScreenCapture.CaptureScreenshot(review+"/HUD-"+pose+".png");yield return Wait(.3f);
 }
 complete=true;
}
System.Collections.IEnumerator Restore(){yield return qa.EndSession();Check(!RoyalThrone.Testing.RoyalSave.Testing,"normal player profile restored");System.IO.File.AppendAllText(report,complete?"ALL LIVE EXPRESSION CHECKS PASSED\n":"FAILED; normal profile restored\n");}
System.Collections.IEnumerator Guarded(){
 var stack=new System.Collections.Generic.Stack<System.Collections.IEnumerator>();stack.Push(Cases());
 try{while(stack.Count>0){var item=stack.Peek();bool next=false;System.Exception error=null;try{next=item.MoveNext();}catch(System.Exception e){error=e;}if(error!=null){System.IO.File.AppendAllText(report,"ERROR "+error+"\n");UnityEngine.Debug.LogException(error);yield break;}if(!next){stack.Pop();(item as System.IDisposable)?.Dispose();continue;}if(item.Current is System.Collections.IEnumerator child&&!(child is UnityEngine.CustomYieldInstruction))stack.Push(child);else yield return item.Current;}}
 finally{while(stack.Count>0)(stack.Pop() as System.IDisposable)?.Dispose();JellyGarden.Scripts.Juice.RoyalPowerupDirector.Reaction-=impact;qa.Speed=1;UnityEngine.Time.timeScale=savedScale;RoyalThrone.Testing.RoyalSave.SetInt("Sound",savedSound);if(king){king.gameObject.SetActive(true);king.ResetToIdle();}if(owns)qa.StartCoroutine(Restore());}
}
qa.StartCoroutine(Guarded());return "Started matching expression QA; normal profile will be restored.";
