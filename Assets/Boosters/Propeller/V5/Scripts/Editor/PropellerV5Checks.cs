#if UNITY_EDITOR && !ROYAL_DISABLE_QA
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using JellyGarden.Scripts;
using JellyGarden.Scripts.Juice;
using RoyalThrone.Testing;
using Object=UnityEngine.Object;
namespace RoyalThrone.Boosters.Propeller.V5.Editor
{
    public static class PropellerV5Checks
    {
        const string Root=PropellerV5Builder.Root+"Animation/Review/";
        static int fps=30;static readonly Dictionary<int,float> lastPhase=new Dictionary<int,float>();static int phaseSamples;
        static RoyalQaService qa;static string label;static int frame,peakRigs;static bool running;
        static readonly List<string> results=new List<string>(),events=new List<string>(),sounds=new List<string>(),errors=new List<string>();
        static readonly Dictionary<int,float> flightStart=new Dictionary<int,float>();static readonly List<float> launches=new List<float>();
        static readonly HashSet<string> captures=new HashSet<string>();static readonly HashSet<PropellerAudioV5.Cue> heard=new HashSet<PropellerAudioV5.Cue>();
        [MenuItem("Tools/Royal Throne/Propeller V5/Run focused checks with audio %&#F12")]
        static void Run(){qa=RoyalQaService.Instance;if(running||!Application.isPlaying||!qa||!qa.Ready)throw new Exception("Enter gameplay first.");running=true;results.Clear();events.Clear();sounds.Clear();errors.Clear();captures.Clear();heard.Clear();phaseSamples=0;fps=30;flightStart.Clear();peakRigs=0;RoyalThrone.Editor.RoyalRegressionRunner.Run(Suite());}
        static void Check(bool ok,string text){if(!ok)throw new Exception("Propeller V5: "+text);results.Add("PASS "+text);}
        static void Log(string m,string s,LogType t){if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)errors.Add(m);}
        static void Sound(PropellerAudioV5.Cue c,int lease){heard.Add(c);sounds.Add(Time.time.ToString("F4")+","+label+","+lease+","+c);}
        static void Event(PropellerEvent e,PropellerVisualV5 v,Vector3 p)
        {
            events.Add(Time.time.ToString("F4")+","+label+","+v.Lease+","+e+","+p);
            if(e==PropellerEvent.PropellerTakeoff)launches.Add(Time.time);
            if(e==PropellerEvent.PropellerFlightReady){flightStart[v.Lease]=Time.time;Check(v.pose.localPosition.y/v.Cell>.54f,"Vertical lift before translation "+label);Check(v.Blur>.98f,"High speed sweep ready "+label);}
            if(e==PropellerEvent.PropellerTargetReached){Check(PropellerAudioV5.Loops==0||PropellerVisualV5.Active>1,"Flight audio stops at contact "+label);float actual=Time.time-flightStart[v.Lease];Check(Mathf.Abs(actual-v.FlightDuration)<.075f,"Distance flight duration "+label+": "+actual.ToString("F3")+" / "+v.FlightDuration.ToString("F3"));}
        }
        static IEnumerator Command(string c){qa.Request(c);while(qa.Busy)yield return null;qa.Visible=false;}
        static void Begin(string name){lastPhase.Clear();PropellerCapture.End();label=name;frame=0;PropellerCapture.Begin(Root+".Capture/"+name);File.WriteAllText(Root+".Capture/"+name+"/motion.csv","frame,time,lease,phase,age,rotorSpeed,x,y,size,bank,flightDuration,rotorAngle,visibleSpeed,blur\n");}
        static void Record()
        {
            var views=Object.FindObjectsByType<PropellerVisualV5>();peakRigs=Mathf.Max(peakRigs,views.Count(v=>v.Current==PropellerVisualV5.Phase.Activation||v.Current==PropellerVisualV5.Phase.Flight));
            foreach(var v in views){
                File.AppendAllText(Root+".Capture/"+label+"/motion.csv",frame+","+Time.time.ToString("F4")+","+v.Lease+","+v.Current+","+v.Age.ToString("F4")+","+v.RotorSpeed.ToString("F2")+","+v.FlightPosition.x.ToString("F4")+","+v.FlightPosition.y.ToString("F4")+","+(v.pose.localScale.x/v.Cell).ToString("F4")+","+Mathf.DeltaAngle(0,v.pose.localEulerAngles.z).ToString("F2")+","+v.FlightDuration.ToString("F3")+","+v.RotorAngle.ToString("F4")+","+v.PresentationSpeed.ToString("F3")+","+v.Blur.ToString("F4")+"\n");
                bool moment=v.Current==PropellerVisualV5.Phase.Impact?v.Age>.04f:v.Current==PropellerVisualV5.Phase.Activation?v.Age>.30f:v.Age>.13f;
                if(moment&&captures.Add(label+"-"+v.Current)){var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Root+label+"-"+v.Current+".png",t.EncodeToPNG());Object.Destroy(t);}
            }
            foreach(var v in views){
                if(v.Current==PropellerVisualV5.Phase.Activation||v.Current==PropellerVisualV5.Phase.Flight){
                    if(lastPhase.TryGetValue(v.Lease,out float prior)&&v.RotorSpeed>100){float step=Mathf.Repeat(v.RotorAngle-prior,360);if(step<=0||step>=59)throw new Exception("Aliased/reversing rotor step: "+step);phaseSamples++;}
                    lastPhase[v.Lease]=v.RotorAngle;
                }
            }
            if(PropellerAudioV5.Loops>5)throw new Exception("Rotor voice cap exceeded");
            PropellerCapture.Frame();frame++;
        }
        static IEnumerator Observe(float seconds){float end=Time.time+seconds;while(Time.time<end){yield return new WaitForEndOfFrame();Record();}}
        static IEnumerator Finish(){float timeout=Time.realtimeSinceStartup+40;do{yield return new WaitForEndOfFrame();Record();}while((RoyalPowerupDirector.Busy||LevelManager.THIS.IsResolvingMatches)&&Time.realtimeSinceStartup<timeout);yield return Observe(.55f);Check(!RoyalPowerupDirector.Busy,"Gameplay finishes "+label);Check(PropellerVfxV5.Active==0&&PropellerAudioV5.Active==0,"No VFX/audio leftovers "+label);Check(PropellerCapture.Clipped==0,"Audio mix has no clipped samples "+label);PropellerCapture.End();}
        static Item Create(int x,int y){var item=LevelManager.THIS.GetSquare(x,y).item;item.NextType=ItemsTypes.HELICOPTER;item.ChangeType();return item;}
        static IEnumerator TargetCase(string name,Vector2Int origin,Vector2Int target,float idle=0)
        {
            yield return Command("Practice board");qa.Column=target.x;qa.Row=target.y;yield return Command("Spawn blocker");Begin(name);
            var item=Create(origin.x,origin.y);yield return Observe(.8f+idle);var v=item.GetComponent<PropellerSeatV5>().View;
            Check(v.Current==PropellerVisualV5.Phase.Idle,"Reveal settles to Idle "+name);var anchor=v.Anchor;yield return Observe(.25f);Check(v.Anchor==anchor,"Stable Idle anchor "+name);
            var expect=LevelManager.THIS.GetSquare(target.x,target.y).transform.position;Vector3 actual=Vector3.positiveInfinity;int lease=v.Lease;
            Action<PropellerEvent,PropellerVisualV5,Vector3> listener=(e,a,p)=>{if(e==PropellerEvent.PropellerTargetReached&&a.Lease==lease)actual=p;};PropellerVisualV5.Event+=listener;
            try{Check(RoyalPowerupDirector.Tap(item),"Live tap accepted "+name);yield return Finish();Check(Vector2.Distance(actual,expect)<.001f,"Existing target priority preserved "+name);}
            finally{PropellerVisualV5.Event-=listener;}
        }
        static IEnumerator Suite()
        {
            bool success=false;int coins=InitScript.Gems,lives=InitScript.lifes;float capture=Time.captureDeltaTime;
            Application.logMessageReceived+=Log;PropellerVisualV5.Event+=Event;PropellerAudioV5.Played+=Sound;
            try{
                if(RoyalSave.Testing)yield return qa.EndSession();qa.StartSession();qa.Visible=false;qa.Counters=false;qa.Loop=false;qa.Level=1;
                Check(Mathf.Approximately(Time.timeScale,1),"Normal 1x speed; no global slowdown");RoyalSave.SetInt("Sound",1);Time.captureDeltaTime=1f/30;
                yield return Command("Practice board");PropellerVisualV5.Prewarm();Begin("square-match-create");
                for(int y=2;y<=3;y++)for(int x=2;x<=3;x++)LevelManager.THIS.GetSquare(x,y).item.SetColor(5);
                var groups=new List<List<Item>>();RoyalPatterns.AddSquares(groups);Check(groups.Count==1&&groups[0].Count==4,"Existing 2x2 rule creates Propeller");foreach(var item in groups[0])item.DestroyItem(true);LevelManager.THIS.FindMatches();yield return Observe(1.8f);yield return Finish();
                Check(LevelManager.THIS.GetItems().Any(i=>i&&i.currentType==ItemsTypes.HELICOPTER&&i.GetComponent<PropellerSeatV5>()),"Created Propeller uses independent V5");
                yield return TargetCase("short-flight",new Vector2Int(1,3),new Vector2Int(3,3));
                yield return TargetCase("medium-flight",new Vector2Int(1,3),new Vector2Int(5,3));
                yield return TargetCase("long-flight",new Vector2Int(0,0),new Vector2Int(7,7),2f);
                yield return TargetCase("edge-target",new Vector2Int(1,3),new Vector2Int(7,3));
                yield return TargetCase("corner-target",new Vector2Int(2,3),new Vector2Int(7,7));
                foreach(int count in new[]{3,5}){
                    yield return Command("Practice board");Begin(count+"-propellers");launches.Clear();var points=new[]{new Vector2Int(0,0),new Vector2Int(7,0),new Vector2Int(0,7),new Vector2Int(7,7),new Vector2Int(3,3)};
                    var items=points.Take(count).Select(p=>Create(p.x,p.y)).ToArray();yield return Observe(.8f);int pool=PropellerVisualV5.PoolSize;
                    foreach(var i in items)i.DestroyItem(true);LevelManager.THIS.FindMatches();yield return Finish();
                    Check(launches.Count>=count,"All "+count+" Propellers launch");Check(launches.Zip(launches.Skip(1),(a,b)=>b-a).All(d=>d>=.033f&&d<=.105f),"Staggered takeoffs "+count);Check(PropellerVisualV5.PoolSize==pool,"Rig pool reused "+count);
                }
                yield return Command("Practice board");Begin("propeller-plus-tnt");var prop=Create(0,0);qa.Column=7;qa.Row=7;yield return Command("Spawn TNT");var tnt=LevelManager.THIS.GetSquare(7,7).item;yield return Observe(.8f);prop.DestroyItem(true);tnt.DestroyItem(true);LevelManager.THIS.FindMatches();yield return Finish();
                yield return Command("Practice board");Begin("cancel-reuse");var interrupted=Create(3,3);yield return Observe(.7f);RoyalPowerupDirector.Tap(interrupted);yield return Observe(.15f);RoyalPowerupDirector.Get.Cancel();Check(PropellerAudioV5.Active==0&&PropellerVisualV5.Active==0,"Cancellation stops rotor audio and releases views");yield return Observe(.15f);PropellerCapture.End();
                yield return TargetCase("pooled-reuse",new Vector2Int(1,3),new Vector2Int(5,3));
                PropellerVisualV5.Clear();Check(PropellerAudioV5.Active==0&&PropellerVfxV5.Active==0&&PropellerVisualV5.Active==0,"All pools clean");
                fps=60;Time.captureDeltaTime=1f/fps;
                yield return TargetCase("rotor-60fps",new Vector2Int(2,3),new Vector2Int(7,7));
                Check(phaseSamples>200,"30/60 FPS phase progression checked: "+phaseSamples);
                Check(heard.Count==6,"All six mechanical sound event hooks played");Check(peakRigs>=5,"Five concurrent live rigs");Check(errors.Count==0,"No new console errors");Check(Mathf.Approximately(Time.timeScale,1),"TimeScale remains 1");
                results.Add("PEAK VFX "+PropellerVfxV5.Peak+"/240; voices "+PropellerAudioV5.Peak+"/20; rigs "+peakRigs);
                yield return qa.EndSession();Check(!RoyalSave.Testing&&InitScript.Gems==coins&&InitScript.lifes==lives,"Player profile restored");success=true;
            }finally{
                PropellerCapture.End();PropellerVisualV5.Clear();Time.captureDeltaTime=capture;Application.logMessageReceived-=Log;PropellerVisualV5.Event-=Event;PropellerAudioV5.Played-=Sound;
                if(RoyalSave.Testing)qa.StartCoroutine(qa.EndSession());running=false;results.AddRange(errors);results.Add(success?"ALL PROPELLER V5 CHECKS PASSED":"FAILED - inspect Console");
                File.WriteAllLines(Root+"checks.txt",results);File.WriteAllLines(Root+"events.csv",events);File.WriteAllLines(Root+"sfx-events.csv",sounds);Debug.Log("[Propeller V5 checks] "+(success?"PASS":"FAIL"));
            }
        }
    }
}
#endif
