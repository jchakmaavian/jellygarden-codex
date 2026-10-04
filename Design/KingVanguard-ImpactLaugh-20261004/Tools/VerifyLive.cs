var qa=RoyalThrone.Testing.RoyalQaService.Instance;
if(!qa||!qa.Ready||qa.Busy||RoyalThrone.Testing.RoyalSave.Testing)throw new System.Exception("Ready normal profile required");
string output="Design/KingVanguard-ImpactLaugh-20261004/Validation",review="Design/KingVanguard-ImpactLaugh-20261004/Review";
System.IO.Directory.CreateDirectory(output);System.IO.Directory.CreateDirectory(review);
string report=output+"/live-checks.txt";System.IO.File.WriteAllText(report,"RUNNING\n");
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
System.Collections.IEnumerator ImpactCase(string label,string command,int a=0,int b=0){
 yield return Command("Practice board");qa.Visible=false;JellyGarden.Scripts.AI.THIS.allowShowTip=false;
 king.ResetToIdle();yield return Wait(4.1f);king.ResetToIdle();
 lastImpactAt=-100;lastStrength=0;qa.PowerA=a;qa.PowerB=b;qa.Column=3;qa.Row=3;
 int failed=qa.Failed;qa.Request(command);float end=UnityEngine.Time.realtimeSinceStartup+45;
 bool reacted=false,voiced=false,captured=false;float sync=0;
 while((qa.Busy||!reacted||!voiced||!captured)&&UnityEngine.Time.realtimeSinceStartup<end){
  var laugh=king.Graphic.GetComponent<RoyalThrone.Characters.KingVanguardLaughter>();
  var art=king.Graphic.GetComponent<RoyalThrone.Characters.KingVanguardLaughArt>();
  var voice=king.Graphic.GetComponent<RoyalThrone.Characters.KingVanguardLaughAudio>();
  if(lastImpactAt>0&&king.CurrentActivity==RoyalThrone.Characters.KingVanguardCharacter.Activity.Laughing&&art&&art.IsShowing){
   reacted=true;
   if(voice&&voice.IsPlaying){voiced=true;sync=UnityEngine.Mathf.Max(sync,UnityEngine.Mathf.Abs(voice.PlaybackTime-(laugh.Time-RoyalThrone.Characters.KingVanguardLaughTiming.LeadIn)));}
   if(!captured&&laugh.Time>.65f){UnityEngine.ScreenCapture.CaptureScreenshot(review+"/HUD-"+label+".png");captured=true;}
  }
  yield return null;
 }
 Check(!qa.Busy&&qa.Failed==failed,label+" actual board action completed");
 Check(lastStrength>=.75f&&reacted,label+" impact triggered new-art laugh");
 Check(voiced&&sync<.16f,label+" voice follows mouth within 160ms; max "+sync);
 yield return Wait(2.8f);
}
System.Collections.IEnumerator Cases(){
 Check(UnityEngine.Application.isFocused,"Game view focused");qa.StartSession();owns=RoyalThrone.Testing.RoyalSave.Testing;qa.Visible=false;
 yield return Command("Practice board");
 foreach(var candidate in UnityEngine.Object.FindObjectsByType<RoyalThrone.Characters.KingVanguardCharacter>(UnityEngine.FindObjectsInactive.Exclude))
  for(var parent=candidate.transform;parent;parent=parent.parent)if(parent.name=="Royal gameplay HUD"&&!candidate.IsAreaRig){king=candidate;break;}
 Check(king,"HUD King found");RoyalThrone.Testing.RoyalSave.SetInt("Sound",1);
 JellyGarden.Scripts.Juice.RoyalPowerupDirector.Reaction+=impact;
 yield return ImpactCase("TNT","TNT animation");
 yield return ImpactCase("Rocket-H","Booster activation",1);
 yield return ImpactCase("Rocket-V","Booster activation",2);
 yield return ImpactCase("TNT-Combo","Combine selected",0,0);
 yield return ImpactCase("Color-Ball","Color-ball effect",4);
 yield return Command("Practice board");qa.Visible=false;JellyGarden.Scripts.AI.THIS.allowShowTip=false;
 king.ResetToIdle();Check(king.PlayLaughter(),"audio lifecycle test starts");yield return Wait(.60f);
 var laugh=king.Graphic.GetComponent<RoyalThrone.Characters.KingVanguardLaughter>();var voice=king.Graphic.GetComponent<RoyalThrone.Characters.KingVanguardLaughAudio>();var art=king.Graphic.GetComponent<RoyalThrone.Characters.KingVanguardLaughArt>();
 Check(voice.IsPlaying&&art.IsShowing,"live voice and new artwork active");
 RoyalThrone.Testing.RoyalSave.SetInt("Sound",0);yield return Wait(.10f);Check(!voice.IsPlaying,"mute stops voice");
 RoyalThrone.Testing.RoyalSave.SetInt("Sound",1);yield return Wait(.10f);Check(!voice.IsPlaying,"unmute does not replay stale voice");
 king.ResetToIdle();king.PlayLaughter();yield return Wait(.55f);float pausedAt=laugh.Time;qa.Speed=0;UnityEngine.Time.timeScale=0;yield return Wait(.2f);
 Check(!voice.IsPlaying&&UnityEngine.Mathf.Abs(laugh.Time-pausedAt)<.05f,"pause freezes laugh and voice; drift "+UnityEngine.Mathf.Abs(laugh.Time-pausedAt));qa.Speed=1;UnityEngine.Time.timeScale=savedScale;yield return Wait(.12f);Check(voice.IsPlaying,"resume continues current voice");
 Check(king.PlaySad()&&!voice.IsPlaying&&!laugh.Active&&!art.IsShowing,"failure cancels artwork and audio");Check(king.Graphic.unscaledTime,"normal portrait clock restored after interruption");
 king.ResetToIdle();king.PlayLaughter();yield return Wait(.4f);king.gameObject.SetActive(false);yield return null;Check(!voice.IsPlaying&&!art.IsShowing,"hidden King retires laugh");king.gameObject.SetActive(true);king.ResetToIdle();
 king.PlayLaughter();yield return Wait(.4f);voice.SendMessage("OnApplicationFocus",false);yield return Wait(.1f);Check(!voice.IsPlaying,"focus loss stops voice");voice.SendMessage("OnApplicationFocus",true);yield return Wait(.1f);Check(!voice.IsPlaying,"focus return does not replay voice");
 king.ResetToIdle();king.PlayLaughter();Check(king.PlayRelief()&&!laugh.Active&&!voice.IsPlaying&&!art.IsShowing,"goal completion takes priority");
 king.ResetToIdle();king.PlayLaughter();yield return Wait(2.7f);Check(!laugh.Active&&!voice.IsPlaying&&!art.IsShowing,"complete performance leaves no stale layers or sound");
 Check(king.Graphic.unscaledTime,"normal portrait clock restored after completion");complete=true;
}
System.Collections.IEnumerator Restore(){yield return qa.EndSession();Check(!RoyalThrone.Testing.RoyalSave.Testing,"normal player profile restored");System.IO.File.AppendAllText(report,complete?"ALL LIVE IMPACT LAUGHTER CHECKS PASSED\n":"FAILED; normal profile restored\n");}
System.Collections.IEnumerator Guarded(){
 var stack=new System.Collections.Generic.Stack<System.Collections.IEnumerator>();stack.Push(Cases());
 try{while(stack.Count>0){var item=stack.Peek();bool next=false;System.Exception error=null;try{next=item.MoveNext();}catch(System.Exception e){error=e;}if(error!=null){System.IO.File.AppendAllText(report,"ERROR "+error+"\n");UnityEngine.Debug.LogException(error);yield break;}if(!next){stack.Pop();(item as System.IDisposable)?.Dispose();continue;}if(item.Current is System.Collections.IEnumerator child&&!(child is UnityEngine.CustomYieldInstruction))stack.Push(child);else yield return item.Current;}}
 finally{while(stack.Count>0)(stack.Pop() as System.IDisposable)?.Dispose();JellyGarden.Scripts.Juice.RoyalPowerupDirector.Reaction-=impact;qa.Speed=1;UnityEngine.Time.timeScale=savedScale;RoyalThrone.Testing.RoyalSave.SetInt("Sound",savedSound);if(king){king.gameObject.SetActive(true);king.ResetToIdle();}if(owns)qa.StartCoroutine(Restore());}
}
qa.StartCoroutine(Guarded());return "Started TNT, H/V rocket, TNT combo, Color Ball and voice lifecycle QA; normal profile will be restored.";
