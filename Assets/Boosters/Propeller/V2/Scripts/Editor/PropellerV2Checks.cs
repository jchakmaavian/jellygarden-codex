#if UNITY_EDITOR && !ROYAL_DISABLE_QA
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JellyGarden.Scripts;
using JellyGarden.Scripts.Juice;
using RoyalThrone.Testing;
using RoyalThrone.Editor;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace RoyalThrone.Boosters.Propeller.V2.Editor
{
    public static class PropellerV2Checks
    {
        const string Root=PropellerV2Builder.Root+"Animation/Review/";
        static RoyalQaService qa;static bool running;
        static readonly List<string> results=new List<string>(),events=new List<string>(),errors=new List<string>();
        static readonly Dictionary<int,List<PropellerEvent>> perLease=new Dictionary<int,List<PropellerEvent>>();
        static readonly HashSet<string> captures=new HashSet<string>();
        static string label;static int frame;static float lastCapture,startTime;static int flightPeak;
        [MenuItem("Tools/Royal Throne/Propeller V2/Run focused live checks %&F12")]
        public static void Run(){
            qa=RoyalQaService.Instance;if(running||!EditorApplication.isPlaying||!qa||!qa.Ready)throw new InvalidOperationException("Enter gameplay Play Mode first.");
            running=true;results.Clear();events.Clear();errors.Clear();perLease.Clear();captures.Clear();flightPeak=0;
            Directory.CreateDirectory(Root);RoyalRegressionRunner.Run(Suite());
        }
        static void Check(bool value,string message){if(!value)throw new Exception("Propeller V2: "+message);results.Add("PASS "+message);}
        static void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message);}
        static void Event(PropellerEvent e,PropellerVisualV2 v,Vector3 p){
            events.Add(Time.time.ToString("F4")+","+label+","+v.Lease+","+e+","+p);
            if(!perLease.TryGetValue(v.Lease,out var list)){list=new List<PropellerEvent>();perLease[v.Lease]=list;}list.Add(e);
        }
        static IEnumerator Command(string c){int done=qa.Completed;qa.Request(c);float end=Time.realtimeSinceStartup+30;while(qa.Busy&&Time.realtimeSinceStartup<end)yield return null;Check(!qa.Busy&&qa.Completed==done+1,"QA fixture: "+c);}
        static void Begin(string name){label=name;frame=0;lastCapture=-1;startTime=Time.unscaledTime;Directory.CreateDirectory(Root+".Capture/"+name);File.WriteAllText(Root+".Capture/"+name+"/timestamps.txt","");}
        static void Capture(string file){var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Root+file+".png",t.EncodeToPNG());Object.Destroy(t);}
        static void Record(){
            foreach(var v in Object.FindObjectsByType<PropellerVisualV2>()){
                string phase=v.Current.ToString();bool moment=v.Current==PropellerVisualV2.Phase.Reveal?v.Age>.13f:v.Current==PropellerVisualV2.Phase.Activation?v.Age>.21f:v.Current==PropellerVisualV2.Phase.Flight?v.Age>.10f:v.Current==PropellerVisualV2.Phase.Idle?v.Age>.12f:v.Current==PropellerVisualV2.Phase.Impact;
                string key=label+"-"+phase;if(moment&&captures.Add(key))Capture(key);
            }
            flightPeak=Mathf.Max(flightPeak,Object.FindObjectsByType<PropellerVisualV2>().Count(v=>v.Current==PropellerVisualV2.Phase.Activation||v.Current==PropellerVisualV2.Phase.Flight));
            lastCapture=Time.unscaledTime;
            var t=ScreenCapture.CaptureScreenshotAsTexture();var path=Root+".Capture/"+label+"/";
            File.WriteAllBytes(path+frame.ToString("D4")+".jpg",t.EncodeToJPG(90));Object.Destroy(t);
            File.AppendAllText(path+"timestamps.txt",(lastCapture-startTime).ToString("F5",System.Globalization.CultureInfo.InvariantCulture)+"\n");frame++;
        }
        static IEnumerator Observe(float seconds){float end=Time.time+seconds;while(Time.time<end){yield return new WaitForEndOfFrame();Record();}}
        static IEnumerator Finish(){float end=Time.realtimeSinceStartup+30;do{yield return new WaitForEndOfFrame();Record();}while((RoyalPowerupDirector.Busy||LevelManager.THIS.IsResolvingMatches)&&Time.realtimeSinceStartup<end);yield return Observe(.45f);Check(!RoyalPowerupDirector.Busy,"Gameplay handoff completes: "+label);Check(PropellerVfxV2.Active==0,"V2 particles clean up: "+label);}
        static Item Create(int c,int r){var item=LevelManager.THIS.GetSquare(c,r).item;item.NextType=ItemsTypes.HELICOPTER;item.ChangeType();return item;}
        static IEnumerator Suite(){
            bool success=false;Application.logMessageReceived+=Log;PropellerVisualV2.Event+=Event;
            int coins=InitScript.Gems,lives=InitScript.lifes;float previousCapture=Time.captureDeltaTime;
            try{
                qa.Loop=false;if(RoyalSave.Testing)yield return qa.EndSession();qa.StartSession();qa.Visible=false;qa.Counters=false;qa.Level=1;Time.captureDeltaTime=1f/30;
                qa.Speed=.5f;yield return Command("Practice board");qa.Visible=false;PropellerVisualV2.Prewarm();Begin("square-match-create");
                for(int y=2;y<=3;y++)for(int x=2;x<=3;x++)LevelManager.THIS.GetSquare(x,y).item.SetColor(5);
                var groups=new List<List<Item>>();RoyalPatterns.AddSquares(groups);
                Check(groups.Count==1&&groups[0].Count==4,"Actual 2x2 gameplay rule selects Propeller creation");
                foreach(var gem in groups[0])gem.DestroyItem(true);LevelManager.THIS.FindMatches();yield return Observe(1.8f);yield return Finish();
                var created=LevelManager.THIS.GetItems().FirstOrDefault(i=>i&&i.currentType==ItemsTypes.HELICOPTER);
                Check(created&&created.currentType==ItemsTypes.HELICOPTER&&created.GetComponent<PropellerSeatV2>().View.Current==PropellerVisualV2.Phase.Idle,"2x2-created Propeller survives match resolution in Idle");
                foreach(float speed in new[]{.5f,1f}){
                    qa.Speed=speed;yield return Command("Practice board");qa.Visible=false;
                    PropellerVisualV2.Prewarm();Begin(speed<1?"half-speed":"normal-speed");
                    var item=Create(2,3);yield return Observe(.78f);
                    var seat=item.GetComponent<PropellerSeatV2>();Check(seat&&seat.View&&seat.View.Current==PropellerVisualV2.Phase.Idle,"Reveal settles into independent Idle");
                    int lease=seat.View.Lease;var pos=seat.View.Anchor;yield return Observe(.6f);
                    var bounds=seat.View.solidViews[0].bounds;foreach(var r in seat.View.solidViews)bounds.Encapsulate(r.bounds);
                    Check(bounds.size.x<seat.View.Cell&&bounds.size.y<seat.View.Cell,"Idle mesh fits within one actual cell");
                    Check(Vector3.Distance(pos,seat.View.Anchor)<.0001f,"Idle has no position drift");
                    Check(RoyalPowerupDirector.Tap(item),"Activation accepted through live gameplay");yield return Finish();
                    var ev=perLease[lease];foreach(var e in new[]{PropellerEvent.PropellerCreated,PropellerEvent.PropellerRevealComplete,PropellerEvent.PropellerActivationStarted,PropellerEvent.PropellerNeighborHit,PropellerEvent.PropellerTakeoff,PropellerEvent.PropellerFlightReady,PropellerEvent.PropellerTargetReached,PropellerEvent.PropellerImpact,PropellerEvent.PropellerCompleted})Check(ev.Contains(e),speed+"x "+e);
                    Check(ev.Count(e=>e==PropellerEvent.PropellerImpact)==1,"One precise target impact per activation");
                }
                qa.Speed=1;yield return Command("Practice board");qa.Visible=false;Begin("five-simultaneous");
                var lm=LevelManager.THIS;int right=lm.maxCols-1,bottom=lm.maxRows-1;
                var five=new[]{Create(0,0),Create(right,0),Create(0,bottom),Create(right,bottom),Create(right/2,bottom/2)};
                yield return Observe(.8f);int poolCount=PropellerVisualV2.PoolSize;
                foreach(var item in five)if(item&&!item.destroying)item.DestroyItem(true);lm.FindMatches();yield return Finish();
                Check(flightPeak>=5,"Five simultaneous live Propellers; corner and edge origins");
                Check(PropellerVisualV2.PoolSize==poolCount,"Prewarmed rig pool reused without growth");
                yield return Command("Practice board");qa.Visible=false;Begin("propeller-plus-tnt");
                var prop=Create(0,0);qa.Column=right;qa.Row=bottom;yield return Command("Spawn TNT");var tnt=lm.GetSquare(right,bottom).item;
                yield return Observe(.75f);prop.DestroyItem(true);tnt.DestroyItem(true);lm.FindMatches();yield return Finish();
                Check(perLease.Values.Count(l=>l.Contains(PropellerEvent.PropellerCompleted))>=8,"Repeated reuse and concurrent TNT finish");
                yield return Command("Practice board");qa.Visible=false;Begin("cancel-and-reuse");
                var interrupted=Create(3,3);yield return Observe(.75f);Check(RoyalPowerupDirector.Tap(interrupted),"Cancellation fixture activates");yield return Observe(.19f);
                RoyalPowerupDirector.Get.Cancel();yield return null;
                Check(PropellerVisualV2.Active==0&&PropellerVfxV2.Active==0,"Cancellation releases active rigs and air/star pool immediately");
                yield return Command("Practice board");qa.Visible=false;
                var reused=Create(3,3);yield return Observe(.75f);Check(RoyalPowerupDirector.Tap(reused),"Cancelled pool can activate again");yield return Finish();
                Check(PropellerVfxV2.PoolSize==320,"Fixed VFX capacity");Check(errors.Count==0,"No new console exceptions/errors");
                results.Add("VFX peak "+PropellerVfxV2.Peak+" / 320; live rigs peak "+flightPeak);
                PropellerVisualV2.Clear();Check(PropellerVisualV2.Active==0&&PropellerVfxV2.Active==0,"No pooled V2 objects remain active after cleanup");yield return qa.EndSession();
                Check(!RoyalSave.Testing&&InitScript.Gems==coins&&InitScript.lifes==lives,"Normal player profile restored");success=true;
            }
            finally{
                PropellerVisualV2.Event-=Event;Application.logMessageReceived-=Log;qa.Speed=1;qa.Loop=false;PropellerVisualV2.Clear();
                if(RoyalSave.Testing)qa.StartCoroutine(qa.EndSession());Time.captureDeltaTime=previousCapture;running=false;
                results.AddRange(errors);results.Add(success?"ALL PROPELLER V2 CHECKS PASSED":"FAILED - inspect Console");
                File.WriteAllLines(Root+"checks.txt",results);File.WriteAllLines(Root+"events.csv",events);Debug.Log("[Propeller V2 checks] "+(success?"PASS":"FAIL"));
            }
        }
    }
}
#endif
