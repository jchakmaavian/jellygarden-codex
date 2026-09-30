// Unity eval_file script. Authoring only: root invokes in Play Mode. Uses isolated QA saves.
var qa=RoyalThrone.Testing.RoyalQaService.Instance;
if(!qa)throw new System.Exception("Enter Play Mode and wait for RoyalQaService before invoking this script.");
var output="Design/KingVanguard-ActivityFix-20260930/Validation";
System.IO.Directory.CreateDirectory(output);
var report=output+"/live-feedback-checks.txt";
var trace=output+"/live-feedback-transitions.csv";
System.IO.File.WriteAllText(report,"RUNNING: QA command fixtures exercise actual swap/match resolution; direct adapter probes are explicitly labelled.\n");
System.IO.File.WriteAllText(trace,"stage,realtime,activity,clip,trackTime,matchedGroups,powerupsCreated\n");
string stage="startup",lastClip="";int matchedGroups=0,powerupsCreated=0;bool ownsSession=false,complete=false;
bool juiceCaptured=false,originalJuice=true;
var seen=new System.Collections.Generic.HashSet<string>();
RoyalThrone.Characters.KingVanguardCharacter Hud()
{
 foreach(var k in UnityEngine.Object.FindObjectsByType<RoyalThrone.Characters.KingVanguardCharacter>(UnityEngine.FindObjectsInactive.Include))
 {
  var t=k.transform;while(t){if(t.name=="Royal gameplay HUD")return k;t=t.parent;}
 }
 return null;
}
void Check(bool ok,string message)
{
 System.IO.File.AppendAllText(report,(ok?"PASS ":"FAIL ")+message+"\n");
 if(!ok)throw new System.Exception(message);
}
void Begin(string name){stage=name;seen.Clear();lastClip="";System.IO.File.AppendAllText(report,"STAGE "+name+"\n");}
void Sample()
{
 var k=Hud();if(!k||!k.isActiveAndEnabled)return;
 var entry=k.Graphic.AnimationState.GetCurrent(0);if(entry==null)return;
 string clip=entry.Animation.Name;seen.Add(clip);
 if(clip!=lastClip)
 {
  lastClip=clip;
  System.IO.File.AppendAllText(trace,stage+","+UnityEngine.Time.realtimeSinceStartup.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+","+
   k.CurrentActivity+","+clip+","+entry.TrackTime.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+","+matchedGroups+","+powerupsCreated+"\n");
 }
}
System.Collections.IEnumerator Observe(float seconds)
{
 float end=UnityEngine.Time.realtimeSinceStartup+seconds;
 while(UnityEngine.Time.realtimeSinceStartup<end){Sample();yield return null;}
 Sample();
}
System.Collections.IEnumerator Request(string command,float observeAfter=1.2f)
{
 Check(!qa.Busy,"QA ready for "+command);
 int completedBefore=qa.Completed,failedBefore=qa.Failed;
 qa.Request(command);qa.Visible=false;
 float deadline=UnityEngine.Time.realtimeSinceStartup+65;
 while(qa.Busy&&UnityEngine.Time.realtimeSinceStartup<deadline){Sample();yield return null;}
 Sample();
 Check(!qa.Busy&&qa.Failed==failedBefore&&qa.Completed==completedBefore+1,
  command+" completed through RoyalQaService.Request: "+qa.Status);
 yield return Observe(observeAfter);
}
System.Collections.IEnumerator HideAndReopen()
{
 var lm=JellyGarden.Scripts.LevelManager.THIS;
 lm.gameStatus=JellyGarden.Scripts.GameState.Pause;
 yield return Observe(.12f);
 Check(Hud()&&!Hud().isActiveAndEnabled,"pause closes the HUD character slot");
 lm.gameStatus=JellyGarden.Scripts.GameState.Playing;
 yield return Observe(.12f);
 Check(Hud()&&Hud().isActiveAndEnabled,"resume reopens the HUD character slot");
}
System.Action<JellyGarden.Scripts.LevelManager,int> onMatch=(level,count)=>{if(level==JellyGarden.Scripts.LevelManager.THIS&&count>=3)matchedGroups++;};
System.Action<JellyGarden.Scripts.LevelManager,JellyGarden.Scripts.Item> onPowerup=(level,item)=>{if(level==JellyGarden.Scripts.LevelManager.THIS&&item)powerupsCreated++;};
System.Collections.IEnumerator Cases()
{
 float deadline=UnityEngine.Time.realtimeSinceStartup+45;
 while((!qa.Ready||qa.Busy)&&UnityEngine.Time.realtimeSinceStartup<deadline)yield return null;
 Check(qa.Ready&&!qa.Busy,"game services are ready");
 // Restore any prior root inspection session first so this run owns a fresh snapshot.
 if(RoyalThrone.Testing.RoyalSave.Testing)yield return qa.EndSession();
 qa.StartSession();ownsSession=RoyalThrone.Testing.RoyalSave.Testing;
 Check(ownsSession,"fresh isolated QA profile started");
 qa.Loop=false;qa.Speed=1;qa.Visible=false;qa.Level=12;
 RoyalThrone.Characters.KingVanguardActivityEvents.MatchResolved+=onMatch;
 RoyalThrone.Characters.KingVanguardActivityEvents.PowerupCreated+=onPowerup;
 Begin("open-practice");yield return Request("Open level");yield return Request("Practice board");
 Check(qa.Practising&&Hud()&&Hud().isActiveAndEnabled,"practice board and live HUD are visible");

 Begin("invalid-swap");int groupsBefore=matchedGroups,powerupsBefore=powerupsCreated;
 yield return Request("Invalid swap",1.2f);
 Check(matchedGroups==groupsBefore,"rejected swap emits no committed match event");
 Check(powerupsCreated==powerupsBefore,"rejected swap emits no committed power-up creation");
 Check(!seen.Contains(RoyalThrone.Characters.KingVanguardCharacter.MatchAcknowledgementAnimation),"rejected swap does not acknowledge a match");

 Begin("successful-swap");groupsBefore=matchedGroups;
 yield return Request("Swap",1.2f);
 Check(matchedGroups>groupsBefore,"valid swap resolves a real matched group");
 Check(seen.Contains(RoyalThrone.Characters.KingVanguardCharacter.MatchAcknowledgementAnimation),"valid swap visibly enters the 0.7 second source acknowledgment");

 Begin("match-with-vfx-disabled");yield return HideAndReopen();seen.Clear();
 originalJuice=JellyGarden.Scripts.Juice.JuiceConfig.Instance.EnableJuice;juiceCaptured=true;
 JellyGarden.Scripts.Juice.JuiceConfig.Instance.EnableJuice=false;groupsBefore=matchedGroups;
 yield return Request("Match 3",1.2f);
 JellyGarden.Scripts.Juice.JuiceConfig.Instance.EnableJuice=originalJuice;juiceCaptured=false;
 Check(matchedGroups>groupsBefore&&seen.Contains(RoyalThrone.Characters.KingVanguardCharacter.MatchAcknowledgementAnimation),
  "actual match still acknowledges when juice/VFX is disabled");

 Begin("actual-match-four-powerup");yield return HideAndReopen();seen.Clear();groupsBefore=matchedGroups;
 powerupsBefore=powerupsCreated;
 yield return Request("Match 4",5f);
 Check(matchedGroups>groupsBefore,"match-four fixture resolves through the game");
 Check(powerupsCreated>powerupsBefore,"match-four commits a real board power-up and emits its semantic creation event");
 Check(seen.Contains(RoyalThrone.Characters.KingVanguardCharacter.HappyAnimation),"real power-up creation produces the original stronger happy reaction");

 Begin("adapter-overlap");yield return Request("Practice board",.2f);yield return HideAndReopen();seen.Clear();
 var lm=JellyGarden.Scripts.LevelManager.THIS;var king=Hud();
 Check(king.PlayMatchAcknowledgement(),"controlled overlap probe starts short activity");
 // Adapter probe: emit the existing creation VFX signal while a short reaction is busy, then a weaker match signal.
 JellyGarden.Scripts.Juice.RoyalPowerupDirector.Get.Creation(lm.GameField.position);
 RoyalThrone.Characters.KingVanguardActivityEvents.NotifyMatchResolved(lm,3);
 Check(king.CurrentAnimation==RoyalThrone.Characters.KingVanguardCharacter.MatchAcknowledgementAnimation,"strong feedback waits instead of interrupting short activity");
 yield return Observe(1.2f);
 Check(seen.Contains(RoyalThrone.Characters.KingVanguardCharacter.HappyAnimation),"one queued stronger response survives rejection while busy and plays afterward");
 yield return Observe(4f);

 Begin("hidden-pending-cleared");yield return HideAndReopen();seen.Clear();king=Hud();
 Check(king.PlayHappy(),"controlled hide probe starts a busy reaction");
 RoyalThrone.Characters.KingVanguardActivityEvents.NotifyMatchResolved(lm,3);
 yield return HideAndReopen();seen.Clear();yield return Observe(2.5f);
 Check(!seen.Contains(RoyalThrone.Characters.KingVanguardCharacter.MatchAcknowledgementAnimation)&&king.CurrentActivity==RoyalThrone.Characters.KingVanguardCharacter.Activity.Idle,
  "hidden HUD clears pending feedback and resumes idle without a stale acknowledgment");

 Begin("new-attempt-pending-cleared");var oldSquares=lm.squaresArray;king=Hud();
 Check(king.PlayHappy(),"controlled restart probe starts a busy reaction");
 RoyalThrone.Characters.KingVanguardActivityEvents.NotifyMatchResolved(lm,3);
 yield return Request("Restart level",2.5f);lm=JellyGarden.Scripts.LevelManager.THIS;
 Check(!object.ReferenceEquals(oldSquares,lm.squaresArray),"restart creates a new board");
 Check(!seen.Contains(RoyalThrone.Characters.KingVanguardCharacter.MatchAcknowledgementAnimation),"prior-attempt pending feedback does not play on restarted board");

 Begin("real-match-returns-to-worry");yield return Request("Practice board",.2f);yield return HideAndReopen();
 lm=JellyGarden.Scripts.LevelManager.THIS;lm.Limit=3;seen.Clear();yield return Observe(.2f);
 Check(Hud().IsWorried,"three moves establishes the worry context");
 groupsBefore=matchedGroups;
 // Request("Match 3") resets moves to 99 through Practice. Prepare the same three-gem fixture here to retain three moves.
 for(int x=1;x<=3;x++)lm.GetSquare(x,3).item.SetColor(0);
 lm.FindMatches();yield return Observe(12f);
 Check(matchedGroups>groupsBefore&&seen.Contains(RoyalThrone.Characters.KingVanguardCharacter.MatchAcknowledgementAnimation),"real low-move matched group acknowledges success");
 Check(Hud().IsWorried&&Hud().CurrentAnimation==RoyalThrone.Characters.KingVanguardCharacter.WorryLoopAnimation,
  "short reaction finishes and returns to established worry loop");
 complete=true;
}
System.Collections.IEnumerator Restore()
{
 yield return qa.EndSession();
 Check(!RoyalThrone.Testing.RoyalSave.Testing,"normal player profile restored after live fixture");
 System.IO.File.AppendAllText(report,complete?"ALL LIVE FEEDBACK CHECKS PASSED\n":"RUN FAILED; profile restoration completed\n");
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
   if(item.Current is System.Collections.IEnumerator child&&!(child is UnityEngine.CustomYieldInstruction))stack.Push(child);
   else yield return item.Current;
  }
 }
 finally
 {
  while(stack.Count>0)(stack.Pop() as System.IDisposable)?.Dispose();
  RoyalThrone.Characters.KingVanguardActivityEvents.MatchResolved-=onMatch;
  RoyalThrone.Characters.KingVanguardActivityEvents.PowerupCreated-=onPowerup;
  if(juiceCaptured)JellyGarden.Scripts.Juice.JuiceConfig.Instance.EnableJuice=originalJuice;
  qa.Loop=false;
  System.IO.File.AppendAllText(report,complete?"CHECKS COMPLETE; restoring profile\n":"INCOMPLETE; restoring profile\n");
  if(ownsSession)qa.StartCoroutine(Restore());
 }
}
qa.StartCoroutine(Guarded());
return "Started isolated live feedback QA. Poll Validation/live-feedback-checks.txt for ALL LIVE FEEDBACK CHECKS PASSED after profile restoration. No screenshots are taken by this script.";
