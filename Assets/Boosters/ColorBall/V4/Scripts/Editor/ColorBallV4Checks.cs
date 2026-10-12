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
namespace RoyalThrone.Boosters.ColorBall.V4.Editor
{
    public static class ColorBallV4Checks
    {
        const string Root=ColorBallV4Builder.Root+"Animation/Review/";
        static RoyalQaService qa;static bool running;static int frame,fps=30,phaseSamples;
        static string label;static float begin,activation=-1,chargeAt,firstBeam,lastContact,lastConversion,sequenceEnd,completed,resolved;
        static int fired,hit,converted,launches,expected,activationLease;static ItemsTypes payload;
        static readonly List<string> checks=new List<string>(),events=new List<string>(),sounds=new List<string>(),errors=new List<string>();
        static readonly Dictionary<Vector3,float> contacts=new Dictionary<Vector3,float>(),flashes=new Dictionary<Vector3,float>(),prepared=new Dictionary<Vector3,float>();
        static readonly Dictionary<int,float> angles=new Dictionary<int,float>();
        static readonly HashSet<ColorBallAudioV4.Cue> cues=new HashSet<ColorBallAudioV4.Cue>();
        static readonly HashSet<string> captured=new HashSet<string>();
        [MenuItem("Tools/Royal Throne/Color Ball V4/Run focused checks with audio %&#F10")]
        static void Run(){qa=RoyalQaService.Instance;if(running||!Application.isPlaying||!qa||!qa.Ready)throw new Exception("Enter the game first.");running=true;checks.Clear();events.Clear();sounds.Clear();errors.Clear();cues.Clear();captured.Clear();phaseSamples=0;fps=30;RoyalThrone.Editor.RoyalRegressionRunner.Run(Suite());}
        static void Check(bool ok,string text){if(!ok)throw new Exception("Color Ball V4: "+text);checks.Add("PASS "+text);}
        static void Log(string m,string s,LogType t){if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)errors.Add(m);}
        static void Sound(ColorBallAudioV4.Cue cue,int lease){cues.Add(cue);sounds.Add(Time.time.ToString("F4")+","+label+","+lease+","+cue);}
        static void Event(ColorBallEvent e,ColorBallVisualV4 v,Vector3 p){
            events.Add(Time.time.ToString("F4")+","+label+","+v.Lease+","+e+","+p);
            switch(e){
                case ColorBallEvent.ColorBallRevealFinished:Check(v.Age<.04f,"Reveal settles without extra hold "+label);break;
                case ColorBallEvent.ColorBallActivationStarted:activation=Time.time;Check(v.Lease==activationLease,"Idle visual retained through activation "+label);break;
                case ColorBallEvent.ColorBallChargeComplete:chargeAt=Time.time;Check(chargeAt-activation>=ColorBallVisualV4.ChargeDuration-.02f&&chargeAt-activation<ColorBallVisualV4.ChargeDuration+.06f,"Charge respects configured duration "+label);break;
                case ColorBallEvent.ColorBallBeamPrepared:prepared[p]=Time.time;break;
                case ColorBallEvent.ColorBallBeamFired:Check(prepared.ContainsKey(p)&&Time.time-prepared[p]>=1f/60f-.001f,"Source reacts before launch "+label+" "+p);if(fired++==0){firstBeam=Time.time;Check(firstBeam>=chargeAt&&firstBeam-activation>=ColorBallVisualV4.ChargeDuration-1e-5f,"First beam follows charge "+label);}break;
                case ColorBallEvent.ColorBallBeamHit:hit++;lastContact=Time.time;Check(!contacts.ContainsKey(p),"One contact per target "+label+" "+p);contacts[p]=Time.time;break;
                case ColorBallEvent.ColorBallTargetFlash:Check(contacts.ContainsKey(p)&&Time.time-contacts[p]>=1f/30f-.001f,"Flash staged after contact "+label+" "+p);flashes[p]=Time.time;break;
                case ColorBallEvent.ColorBallTargetConverted:
                    converted++;lastConversion=Time.time;Check(flashes.ContainsKey(p)&&Time.time-flashes[p]>=1f/30f-.001f,"Conversion follows separated flash "+label+" "+p);
                    var square=LevelManager.THIS.squaresArray.First(s=>s&&Vector3.Distance(s.transform.position,p)<.001f);Check(square.item&&square.item.currentType==payload,"Correct gameplay booster/orientation "+label+" "+p);break;
                case ColorBallEvent.ColorBallSequenceFinished:sequenceEnd=Time.time;Check(ColorBallVfxV4.ActiveBeams==0,"Last moving beam fully finishes "+label);break;
                case ColorBallEvent.ColorBallResolved:resolved=Time.time;break;
                case ColorBallEvent.ColorBallCompleted:completed=Time.time;Check(completed-resolved>=.12f&&completed-resolved<=.20f,"Micro-pause between ball resolution and Rocket handoff "+label);Check(completed-sequenceEnd>=.45f&&completed-sequenceEnd<=.52f,"Contraction and held handoff "+label);break;
            }
        }
        static IEnumerator Command(string command){qa.Request(command);while(qa.Busy)yield return null;qa.Visible=false;}
        static void Begin(string name){ColorBallV4Capture.End();label=name;frame=0;begin=Time.time;activation=-1;fired=hit=converted=0;contacts.Clear();flashes.Clear();prepared.Clear();angles.Clear();ColorBallV4Capture.Begin(Root+".Capture/"+name);File.WriteAllText(Root+".Capture/"+name+"/motion.csv","frame,time,phase,age,angle,speed,charge,x,y,scaleX,scaleY,beams,orbitAngle,orbitSpeed,sourcePulse\n");File.WriteAllText(Root+"progress.txt",name);}
        static void Record(bool capture=true){
            foreach(var v in Object.FindObjectsByType<ColorBallVisualV4>()){
                if(v.Current==ColorBallVisualV4.Phase.Free)continue;
                CheckAlignment(v);
                if(angles.TryGetValue(v.Lease,out float previous)){float step=Mathf.Repeat(v.Angle-previous,Mathf.PI*2);
                    // V3 guarded strobing by keeping the per-frame rotation under .36 rad, because it
                    // had almost no shutter. V4 turns 90 degrees a frame like the reference, so the
                    // invariant is instead that the smear is the designed fraction of the step: enough
                    // to elongate each bead and hide the aliasing, short enough that neighbouring beads
                    // do not average to grey.
                    float wanted=step*ColorBallVisualV4.Shutter;
                    if(step>.05f&&v.ShutterArc<wanted*.90f)throw new Exception("Shutter arc "+v.ShutterArc+" rad is short of the "+wanted+" rad the "+step+" rad step needs");
                    if(step>ColorBallVisualV4.MaxStepRad+.02f)throw new Exception("Rotation step "+step+" rad exceeds the per-frame cap");
                    phaseSamples++;}angles[v.Lease]=v.Angle;
                if(activation>=0&&(RoyalPowerupDirector.Get.ColorBallState==RoyalPowerupDirector.ColorBallPhase.SpinUp||RoyalPowerupDirector.Get.ColorBallState==RoyalPowerupDirector.ColorBallPhase.Converting||RoyalPowerupDirector.Get.ColorBallState==RoyalPowerupDirector.ColorBallPhase.Finish)&&RoyalPowerupDirector.Get.RocketLaunches!=launches)throw new Exception("Rocket fired before Color Ball payoff");
                if(capture){File.AppendAllText(Root+".Capture/"+label+"/motion.csv",frame+","+(Time.time-begin).ToString("F4")+","+v.Current+","+v.Age.ToString("F4")+","+v.Angle.ToString("F4")+","+v.Speed.ToString("F4")+","+v.Charge.ToString("F4")+","+v.Anchor.x.ToString("F4")+","+v.Anchor.y.ToString("F4")+","+v.bodyLayer.localScale.x.ToString("F4")+","+v.bodyLayer.localScale.y.ToString("F4")+","+ColorBallVfxV4.ActiveBeams+","+v.OrbitAngle.ToString("F4")+","+v.OrbitSpeed.ToString("F4")+","+v.SourcePulse.ToString("F4")+"\n");}
                bool moment=v.Current==ColorBallVisualV4.Phase.Reveal?v.Age>.32f:v.Current==ColorBallVisualV4.Phase.Charge?v.Age>.4f:v.Current==ColorBallVisualV4.Phase.Finish?v.Age>.07f:v.Age>.15f;
                if(capture&&moment&&captured.Add(label+"-"+v.Current)){var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Root+label+"-"+v.Current+".png",t.EncodeToPNG());Object.Destroy(t);}
            }
            if(capture){ColorBallV4Capture.Frame();frame++;}
        }
        static void CheckAlignment(ColorBallVisualV4 v){if(v.bodyLayer.localPosition!=v.colorSurfaceLayer.localPosition||v.bodyLayer.localScale!=v.colorSurfaceLayer.localScale||v.bodyLayer.rotation!=v.colorSurfaceLayer.rotation)throw new Exception("Body/energy center or volume mismatch");if(v.orbitBack.sortingOrder>=v.body.sortingOrder||v.orbitFront.sortingOrder<=v.energy.sortingOrder||v.orbitBack.sharedMaterial.renderQueue!=v.body.sharedMaterial.renderQueue)throw new Exception("Orbit depth ordering mismatch");if(!v.body.sharedMaterial.shader.isSupported||!v.energy.sharedMaterial.shader.isSupported)throw new Exception("Sphere shader unsupported");}
        static IEnumerator Observe(float seconds,bool capture=true){float until=Time.time+seconds;while(Time.time<until){yield return new WaitForEndOfFrame();Record(capture);}}
        static Item Create(int x,int y,ItemsTypes type){var i=LevelManager.THIS.GetSquare(x,y).item;i.NextType=type;i.ChangeType();return i;}
        static IEnumerator Combo(string name,int x,int y,ItemsTypes type,bool high=false){
            yield return Command("Practice board");
            if(high)foreach(var sq in LevelManager.THIS.squaresArray)if(sq&&sq.item)sq.item.SetColor((sq.col+sq.row)%2==0?0:(sq.col+sq.row*2)%4+1);
            Begin(name);payload=type;var ball=Create(x,y,ItemsTypes.BOMB);var partner=Create(x+1,y,type);yield return Observe(.95f);
            var v=ball.GetComponent<ColorBallSeatV4>().View;Check(v.Current==ColorBallVisualV4.Phase.Idle,"Reveal ends on Idle "+name);Check(Mathf.Abs(v.bodyLayer.localScale.x-.84f)<.001f,"Cell-sized stable Idle "+name);
            int[] counts=new int[6];foreach(var i in LevelManager.THIS.GetItems())if(i&&i!=ball&&i!=partner&&i.color>=0&&i.color<6)counts[i.color]++;expected=counts.Max();launches=RoyalPowerupDirector.Get.RocketLaunches;
            activationLease=v.Lease;Check(RoyalPowerupDirector.TrySwap(ball,partner),"Existing live combo accepted "+name);LevelManager.THIS.FindMatches();
            float deadline=Time.time+25;do{yield return new WaitForEndOfFrame();Record();}while((RoyalPowerupDirector.Busy||LevelManager.THIS.IsResolvingMatches)&&Time.time<deadline);
            yield return Observe(.65f);Check(!RoyalPowerupDirector.Busy&&!LevelManager.THIS.IsResolvingMatches,"Gameplay finishes "+name);
            float baseline=high?3.4333f:fps==60?1.8333f:1.7333f;float slowdown=(lastConversion-firstBeam)/baseline;Check(slowdown>=1.14f&&slowdown<=1.27f,"Conversion volley 15-25% slower than measured V2 (frame tolerance): "+slowdown.ToString("F3"));
            Check(fired==hit&&hit==converted&&converted==expected,"All selected targets contacted/converted exactly once "+name+": "+converted);
            Check(RoyalPowerupDirector.Get.RocketLaunches>launches,"Existing Rockets launch after payoff "+name);
            var owners=Object.FindObjectsByType<ColorBallSeatV4>().Where(s=>s.View&&s.GetComponent<Item>()&&!s.GetComponent<Item>().destroying&&s.GetComponent<Item>().currentType==ItemsTypes.BOMB).Select(s=>s.View).ToArray();
            var remaining=Object.FindObjectsByType<ColorBallVisualV4>().Where(s=>s.Current!=ColorBallVisualV4.Phase.Free).ToArray();
            Check(remaining.All(s=>owners.Count(o=>o==s)==1&&(s.Current==ColorBallVisualV4.Phase.Idle||s.Current==ColorBallVisualV4.Phase.Reveal))&&remaining.Length==owners.Length,"Only legitimate cascade-created seated Color Balls remain "+name);
            Check(ColorBallVfxV4.ActiveBeams==0&&ColorBallAudioV4.Active==0,"No beam/audio leftovers "+name);
            Check(ColorBallV4Capture.Clipped==0,"Audio mix has zero clipped samples "+name);Check(!LevelManager.THIS.DragBlocked,"Board input restored "+name);
            checks.Add("MEASURE "+name+": targets="+converted+", charge="+(firstBeam-activation).ToString("F3")+", volley="+(lastConversion-firstBeam).ToString("F3")+", final="+(completed-sequenceEnd).ToString("F3")+", audio peak="+ColorBallV4Capture.Peak.ToString("F4"));ColorBallV4Capture.End();
        }
        static IEnumerator Suite(){bool success=false;float capture=Time.captureDeltaTime;int coins=InitScript.Gems,lives=InitScript.lifes;
            Application.logMessageReceived+=Log;ColorBallVisualV4.Event+=Event;ColorBallAudioV4.Played+=Sound;
            try{
                if(RoyalSave.Testing)yield return qa.EndSession();qa.StartSession();qa.Visible=false;qa.Counters=false;qa.Loop=false;qa.Level=1;RoyalSave.SetInt("Sound",1);Check(Mathf.Approximately(Time.timeScale,1),"Normal 1x speed");Time.captureDeltaTime=1f/30;
                yield return Command("Practice board");ColorBallVisualV4.Prewarm();Begin("reveal-idle-30fps");var item=Create(3,3,ItemsTypes.BOMB);yield return Observe(.9f);var visual=item.GetComponent<ColorBallSeatV4>().View;float angle=visual.Angle;var scale=visual.bodyLayer.localScale;yield return Observe(2);ColorBallV4Capture.End();yield return Observe(13,false);Check(visual.Current==ColorBallVisualV4.Phase.Idle&&visual.bodyLayer.localScale==scale,"Idle stable for 15 seconds");Check(Mathf.Abs(Mathf.Repeat(visual.Angle-angle,Mathf.PI*2)-.975f)<.03f,"Very slow independent spherical rotation");Check(ColorBallAudioV4.Active==0,"Idle is silent");
                yield return Combo("ColorBall-V4-Gameplay",3,3,ItemsTypes.HORIZONTAL_STRIPPED);
                yield return Combo("high-target-count",0,7,ItemsTypes.VERTICAL_STRIPPED,true);
                int pool=ColorBallVisualV4.PoolSize;fps=60;Time.captureDeltaTime=1f/fps;
                yield return Combo("combo-60fps-reuse",0,0,ItemsTypes.VERTICAL_STRIPPED);
                Check(ColorBallVisualV4.PoolSize==pool,"Repeated use reuses pooled rig");Check(cues.Count==7,"Seven original audio event cues played; Idle silent");Check(phaseSamples>600,"30/60 FPS continuous phase samples: "+phaseSamples);Check(errors.Count==0,"No new console errors");
                Check(Mathf.Approximately(Time.timeScale,1),"TimeScale stayed at 1");checks.Add("PEAK VFX "+ColorBallVfxV4.Peak+"/224; voices "+ColorBallAudioV4.Peak+"/24");
                yield return qa.EndSession();Check(ColorBallVisualV4.Active==0&&ColorBallVfxV4.Active==0&&ColorBallAudioV4.Active==0,"All pools clean after test session");Check(!RoyalSave.Testing&&InitScript.Gems==coins&&InitScript.lifes==lives,"Normal player data restored");success=true;
            }finally{
                ColorBallV4Capture.End();ColorBallVisualV4.Clear();Time.captureDeltaTime=capture;Application.logMessageReceived-=Log;ColorBallVisualV4.Event-=Event;ColorBallAudioV4.Played-=Sound;
                if(RoyalSave.Testing)qa.StartCoroutine(qa.EndSession());running=false;checks.AddRange(errors);checks.Add(success?"ALL COLOR BALL V4 CHECKS PASSED":"FAILED - inspect Console");File.WriteAllLines(Root+"checks.txt",checks);File.WriteAllLines(Root+"events.csv",events);File.WriteAllLines(Root+"sfx-events.csv",sounds);Debug.Log("[Color Ball V4 checks] "+(success?"PASS":"FAIL"));
            }
        }
    }
}
#endif
