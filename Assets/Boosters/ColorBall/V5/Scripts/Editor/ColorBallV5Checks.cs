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
using RoyalThrone.Boosters.ColorBall.V4;
using RoyalThrone.Boosters.ColorBall.V4.Editor;
using Object=UnityEngine.Object;
namespace RoyalThrone.Boosters.ColorBall.V5.Editor
{
    public static class ColorBallV5Checks
    {
        static string Root;
        static ColorBallPresentationProfile testProfile;
        static bool running,record;static RoyalQaService qa;
        static int fired,hits,converted,expected,lease,launches,frames;
        static float begin,activation,chargeAt,firstContact,lastContact,finishAt,completed;
        static string label;static ItemsTypes payload;
        static readonly List<string> notes=new List<string>(),events=new List<string>(),errors=new List<string>();
        static readonly Dictionary<Vector3,float> contacts=new Dictionary<Vector3,float>();
        static readonly List<float> frameTimes=new List<float>();
        static int previousLease;static float previousTime,previousAngle;static ColorBallVisualV4.Phase previousPhase;
        static int rigidSamples,spinSamples;static readonly List<int> linkCounts=new List<int>();
        [MenuItem("Tools/Royal Throne/Color Ball V5/Presentation/ReferenceMatch")]
        static void ReferenceProfile(){ColorBallV5Presentation.ActiveProfile=ColorBallPresentationProfile.ReferenceMatch;}
        [MenuItem("Tools/Royal Throne/Color Ball V5/Presentation/ReferenceMatch",true)]
        static bool ReferenceSelected(){Menu.SetChecked("Tools/Royal Throne/Color Ball V5/Presentation/ReferenceMatch",ColorBallV5Presentation.ActiveProfile==ColorBallPresentationProfile.ReferenceMatch);return !running;}
        [MenuItem("Tools/Royal Throne/Color Ball V5/Presentation/Enhanced")]
        static void EnhancedProfile(){ColorBallV5Presentation.ActiveProfile=ColorBallPresentationProfile.Enhanced;}
        [MenuItem("Tools/Royal Throne/Color Ball V5/Presentation/Enhanced",true)]
        static bool EnhancedSelected(){Menu.SetChecked("Tools/Royal Throne/Color Ball V5/Presentation/Enhanced",ColorBallV5Presentation.ActiveProfile==ColorBallPresentationProfile.Enhanced);return !running;}
        [MenuItem("Tools/Royal Throne/Color Ball V5/Short live proof %&#F11")]
        static void Proof(){Run(true);}
        [MenuItem("Tools/Royal Throne/Color Ball V5/Single live sequence")]
        static void SingleSequence(){Run(false,true);}
        [MenuItem("Tools/Royal Throne/Color Ball V5/Combo and actual frame rate checks")]
        static void Full(){Run(false);}
        static void Run(bool proof,bool singleSequence=false){qa=RoyalQaService.Instance;if(running||!Application.isPlaying||!qa||!qa.Ready){Debug.LogError("Color Ball: enter the game before the focused test.");return;}testProfile=ColorBallV5Presentation.ActiveProfile;Root="Logs/port-checks/ColorBallV5/Links-"+testProfile+"/";Directory.CreateDirectory(Root);running=true;notes.Clear();events.Clear();errors.Clear();notes.Add("Presentation profile: "+testProfile+"; same live gameplay, target selection and Rocket handoff.");RoyalThrone.Editor.RoyalRegressionRunner.Run(Suite(proof,singleSequence));}
        static void Check(bool condition,string message){if(!condition)throw new Exception("Color Ball V5: "+message);notes.Add("PASS "+message);}
        static void Error(string m,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(m+"\n"+stack);}
        static void Event(ColorBallEvent e,ColorBallVisualV4 v,Vector3 p){
            events.Add(label+","+(Time.time-begin).ToString("F4")+","+e+","+v.Lease+","+p);
            if(e==ColorBallEvent.ColorBallActivationStarted){lease=v.Lease;activation=Time.time;}
            if(e==ColorBallEvent.ColorBallChargeComplete)chargeAt=Time.time;
            if(e==ColorBallEvent.ColorBallBeamFired)fired++;
            if(e==ColorBallEvent.ColorBallBeamHit){if(contacts.ContainsKey(p))throw new Exception("Duplicate contact");contacts[p]=Time.time;lastContact=Time.time;if(hits++==0)firstContact=Time.time;}
            if(e==ColorBallEvent.ColorBallTargetConverted){if(!contacts.ContainsKey(p)||Time.time-contacts[p]<.065f)throw new Exception("Conversion before contact reaction");var sq=LevelManager.THIS.squaresArray.First(s=>s&&Vector3.Distance(s.transform.position,p)<.001f);if(!sq.item||sq.item.currentType!=payload)throw new Exception("Conversion result/orientation changed");converted++;}
            if(e==ColorBallEvent.ColorBallSequenceFinished)finishAt=Time.time;
            if(e==ColorBallEvent.ColorBallCompleted)completed=Time.time;
        }
        static Item Create(int x,int y,ItemsTypes type){var item=LevelManager.THIS.GetSquare(x,y).item;item.NextType=type;item.ChangeType();return item;}
        static IEnumerator Practice(){qa.Request("Practice board");while(qa.Busy)yield return null;qa.Visible=false;Check(qa.Status.StartsWith("Completed"),"Disposable practice fixture opens");yield return new WaitForSeconds(.5f);}
        static void Sample(){
            frames++;frameTimes.Add(Time.unscaledDeltaTime);
            if(hits>=3&&finishAt==0)linkCounts.Add(ColorBallVfxV4.VisibleLinks);
            if(!ColorBallVfxV4.RootsBehindBody)throw new Exception("Beam renderer overlays solid body");
            if(ColorBallVfxV4.AttachmentError>ColorBallVisualV4.CellSize*.001f)throw new Exception("Beam detached from animated source");
            foreach(var v in Object.FindObjectsByType<ColorBallVisualV4>())if(v.Current!=ColorBallVisualV4.Phase.Free){
                if(v.bodyLayer.localScale!=v.colorSurfaceLayer.localScale||v.bodyLayer.position!=v.colorSurfaceLayer.position)throw new Exception("Body/surface pivot mismatch");
                if(v.bodyLayer.localRotation!=v.colorSurfaceLayer.localRotation)throw new Exception("Body/surface tilt mismatch");
                if(v.Current!=ColorBallVisualV4.Phase.Reveal&&v.bodyLayer.localPosition.magnitude>.05f)throw new Exception("Visual source drifts beyond bounded tremble");
                if(Vector3.Distance(v.transform.position,v.Anchor+Vector3.back*.7f)>.001f)throw new Exception("Logical cell anchor moved");
                if(!v.body.sharedMaterial.shader.isSupported||!v.energy.sharedMaterial.shader.isSupported)throw new Exception("Unsupported shader");
                if(v.Current==ColorBallVisualV4.Phase.Charge||v.Current==ColorBallVisualV4.Phase.Charged||v.Current==ColorBallVisualV4.Phase.Finish){
                    if(RoyalPowerupDirector.Get.RocketLaunches!=launches)throw new Exception("Premature Rocket launch");
                    var scale=v.bodyLayer.localScale;
                    if(Mathf.Abs(scale.x-.84f)>.0001f||Mathf.Abs(scale.y-.84f)>.0001f||Mathf.Abs(scale.z-1)>.0001f)throw new Exception("Rigid metal shell was scaled/deformed during energized motion");
                    if(!v.TryGetComponent<ColorBallV5Presentation>(out var presentation)||presentation.Profile!=testProfile)throw new Exception("Runtime presentation profile differs from test profile");
                    rigidSamples++;
                }
                // A steady-state angular step must be seconds-based at both frame rates, not
                // capped per rendered frame. This checks implementation consistency, not reference RPM.
                if(v.Lease==lease){
                    if(previousLease==v.Lease&&previousPhase==ColorBallVisualV4.Phase.Charged&&v.Current==ColorBallVisualV4.Phase.Charged){
                        float expected=previousAngle+v.Speed*(Time.time-previousTime);
                        float error=Mathf.Abs(Mathf.DeltaAngle(expected*Mathf.Rad2Deg,v.Angle*Mathf.Rad2Deg))*Mathf.Deg2Rad;
                        if(error>.03f)throw new Exception("Charged rotation is not integrated in seconds: "+error.ToString("F4")+" rad");
                        spinSamples++;
                    }
                    previousLease=v.Lease;previousPhase=v.Current;previousAngle=v.Angle;previousTime=Time.time;
                }
            }
            if(record&&!UnityEngine.Application.isBatchMode)ColorBallV4Capture.Frame();
        }
        static IEnumerator Observe(float duration){float until=Time.time+duration;while(Time.time<until){yield return (UnityEngine.Application.isBatchMode?null:new WaitForEndOfFrame());Sample();}}
        static IEnumerator Combo(string name,bool proof,bool capture,bool high,int targetFps){
            yield return Practice();label=name;frames=fired=hits=converted=0;rigidSamples=spinSamples=previousLease=0;contacts.Clear();frameTimes.Clear();linkCounts.Clear();activation=chargeAt=finishAt=completed=0;record=capture;
            if(high)foreach(var s in LevelManager.THIS.squaresArray)if(s&&s.item)s.item.SetColor((s.col+s.row)%2==0?0:(s.col+s.row*2)%4+1);
            payload=high?ItemsTypes.VERTICAL_STRIPPED:ItemsTypes.HORIZONTAL_STRIPPED;
            begin=Time.time;launches=RoyalPowerupDirector.Get.RocketLaunches;
            if(record&&!UnityEngine.Application.isBatchMode)ColorBallV4Capture.Begin(Root+".Capture/"+name);
            var ball=Create(3,3,ItemsTypes.BOMB);var partner=Create(4,3,payload);
            yield return Observe(.95f);var view=ball.GetComponent<ColorBallSeatV4>().View;
            Check(view&&view.referenceV5&&view.Current==ColorBallVisualV4.Phase.Idle,"V5 reveal settles to pooled Idle");
            Check(view.GetComponent<ColorBallV5Presentation>()&&view.body.sharedMaterial.shader.name=="RoyalThrone/ColorBallV5/Sphere"&&view.halo.sharedMaterial.shader.name=="RoyalThrone/ColorBallV5/Light","Runtime V5 presentation and local materials connected");
            Check(view.GetComponent<ColorBallV5Presentation>().Profile==testProfile,"Runtime pooled actor captured "+testProfile+" profile");
            notes.Add("Runtime body="+AssetDatabase.GetAssetPath(view.body.sharedMaterial)+"; halo="+AssetDatabase.GetAssetPath(view.halo.sharedMaterial));
            var fx=Object.FindAnyObjectByType<ColorBallVfxV4>();Check(fx&&fx.referenceV5&&fx.beam.shader.name=="RoyalThrone/ColorBallV5/Light","Runtime V5 beam pool uses the connected local shader");
            Check(Mathf.Abs(view.bodyLayer.localScale.x-.84f)<.001f,"Idle scale unchanged");
            var camera=Camera.main;var center=camera.WorldToScreenPoint(view.Anchor);var edge=camera.WorldToScreenPoint(view.Anchor+Vector3.right*view.Cell);
            File.WriteAllText(Root+name+"-framing.txt","screen="+Screen.width+","+Screen.height+"; center="+center+"; cell="+Vector3.Distance(center,edge));
            int[] counts=new int[6];foreach(var i in LevelManager.THIS.GetItems())if(i&&i!=ball&&i!=partner&&i.color>=0&&i.color<6)counts[i.color]++;expected=counts.Max();
            Check(RoyalPowerupDirector.TrySwap(ball,partner),"Live Color Ball + Rocket combo starts");LevelManager.THIS.FindMatches();
            float deadline=Time.time+25;
            do{yield return (UnityEngine.Application.isBatchMode?null:new WaitForEndOfFrame());Sample();if(proof&&converted>0&&Time.time-firstContact>.72f)break;}while((RoyalPowerupDirector.Busy||LevelManager.THIS.IsResolvingMatches)&&Time.time<deadline);
            if(proof){Check(converted>=4&&hits>=converted&&contacts.Count==hits,"Short proof reaches multiple unique hit/conversion events");Check(RoyalPowerupDirector.Get.RocketLaunches==launches,"Proof ends before Rocket handoff");}
            if(!proof){
                yield return Observe(.8f);
                Check(!RoyalPowerupDirector.Busy&&!LevelManager.THIS.IsResolvingMatches,"Live combo and Rocket payoff finish");
                Check(fired==expected&&hits==expected&&converted==expected,"Every selected target converted once: "+converted);
                Check(RoyalPowerupDirector.Get.RocketLaunches>launches,"Existing Rockets launch after Color Ball finish");
                Check(ColorBallVfxV4.ActiveBeams==0&&ColorBallAudioV4.Active==0,"No beam/voice leftovers");
                Check(completed-finishAt>=.45f&&completed-finishAt<.53f,"Final .46s envelope preserves handoff time");
                Check(!LevelManager.THIS.DragBlocked,"Board input restored");
            }
            Check(chargeAt-activation>=.60f&&chargeAt-activation<.71f,"V4 .62s charge preserved");
            Check(rigidSamples>10&&spinSamples>5,"Rigid shared shell transforms and seconds-based charged rotation sampled throughout the proof");
            if(linkCounts.Count>0){notes.Add(name+": visible links mean="+linkCounts.Average().ToString("F2")+"; range="+linkCounts.Min()+".."+linkCounts.Max());Check(linkCounts.Average()>=3&&linkCounts.Max()>=4,"Recent real links overlap without changing hits");}
            if(!proof)Check(Mathf.Abs(finishAt-lastContact-.1667f)<.04f,"Original final-hit to finish interval retained");
            if(record){Check(ColorBallV4Capture.Clipped==0,"Actual game audio capture has zero clipped samples");notes.Add(name+" audio peak="+ColorBallV4Capture.Peak);ColorBallV4Capture.End();record=false;}
            var sorted=frameTimes.OrderBy(t=>t).ToArray();notes.Add(name+": targets="+converted+"; "+(capture?"30 FPS fixed-step recording (not a performance result)":"uncaptured actual "+targetFps+" FPS check")+"; wall-frame mean FPS="+(1f/frameTimes.Average()).ToString("F2")+"; median FPS="+(1f/sorted[sorted.Length/2]).ToString("F2")+"; p95 ms="+(1000*sorted[(int)(sorted.Length*.95f)]).ToString("F2")+"; rigid samples="+rigidSamples+"; steady spin samples="+spinSamples);
            if(!capture)Check(1f/sorted[sorted.Length/2]>=targetFps*.90f,"Actual "+targetFps+" FPS median achieved");
            File.WriteAllText(Root+"progress.txt",name+" complete");
        }
        static IEnumerator Suite(bool proof,bool singleSequence){bool pass=false;float capture=Time.captureDeltaTime;int fps=Application.targetFrameRate,vsync=QualitySettings.vSyncCount,coins=InitScript.Gems,lives=InitScript.lifes;
            Application.logMessageReceived+=Error;ColorBallVisualV4.Event+=Event;
            try{
                Check(Mathf.Approximately(Time.timeScale,1),"Normal 1x TimeScale");if(RoyalSave.Testing)yield return qa.EndSession();qa.StartSession();qa.Visible=false;qa.Counters=false;qa.Loop=false;qa.Level=1;RoyalSave.SetInt("Sound",1);
                Time.captureDeltaTime=1f/30;yield return Combo("ColorBall-"+testProfile+(proof?"-Proof":"-Gameplay"),proof,true,false,30);
                if(proof){
                    int pool=ColorBallVisualV4.PoolSize;Time.captureDeltaTime=0;QualitySettings.vSyncCount=0;
                    foreach(int rate in new[]{30,60}){yield return qa.OpenLevel();ColorBallVisualV4.Clear();Application.targetFrameRate=rate;yield return Combo("rigid-actual-"+rate+"fps",true,false,false,rate);}
                    // This proof stops during conversion. Retire its authoritative operation first;
                    // clearing only presentation cannot prevent the next scheduled beam/contact.
                    yield return qa.OpenLevel();ColorBallVisualV4.Clear();yield return null;Check(ColorBallVisualV4.PoolSize==pool,"Repeated short proof does not grow actor pool");Check(!RoyalPowerupDirector.Busy&&ColorBallVfxV4.Active==0&&ColorBallAudioV4.Active==0,"Cancellation clears the operation and all local effects/audio");
                }
                if(!proof&&!singleSequence){int pool=ColorBallVisualV4.PoolSize;Time.captureDeltaTime=0;QualitySettings.vSyncCount=0;
                    foreach(int rate in new[]{30,60}){Application.targetFrameRate=rate;yield return Combo("actual-"+rate+"fps-reuse",false,false,false,rate);yield return Combo("actual-"+rate+"fps-high-target",false,false,true,rate);}
                    Check(ColorBallVisualV4.PoolSize==pool,"Repeated use does not grow visual pool");
                    var cancelled=ColorBallVisualV4.Take(LevelManager.THIS.GetSquare(3,3).transform.position);cancelled.Activate();yield return new WaitForSeconds(.7f);ColorBallVisualV4.Clear();yield return null;
                    Check(ColorBallVfxV4.Active==0&&ColorBallAudioV4.Active==0,"Cancellation stops all local effects/audio");
                    var reused=ColorBallVisualV4.Take(cancelled.Anchor,true);yield return new WaitForSeconds(.8f);
                    Check(reused.bodyLayer.localPosition==Vector3.zero&&reused.bodyLayer.localRotation==Quaternion.identity&&reused.beamOrigin.localPosition==Vector3.zero,"Pooled reveal/Idle restores shared pivot and beam origin");reused.Release();
                }
                ColorBallVisualV4.Clear();yield return qa.EndSession();yield return null;
                Check(!RoyalSave.Testing&&InitScript.Gems==coins&&InitScript.lifes==lives,"Normal player data restored");Check(ColorBallVisualV4.Active==0&&ColorBallVfxV4.Active==0&&ColorBallAudioV4.Active==0,"Pools clean on session exit");Check(errors.Count==0,"No new console errors");pass=true;
            }finally{
                ColorBallV4Capture.End();ColorBallVisualV4.Clear();Time.captureDeltaTime=capture;Application.targetFrameRate=fps;QualitySettings.vSyncCount=vsync;
                ColorBallVisualV4.Event-=Event;Application.logMessageReceived-=Error;if(RoyalSave.Testing)qa.StartCoroutine(qa.EndSession());running=false;notes.AddRange(errors);notes.Add(pass?"PASS":"FAILED");File.WriteAllLines(Root+(singleSequence?"sequence-checks.txt":proof?"proof-checks.txt":"checks.txt"),notes);File.WriteAllLines(Root+(singleSequence?"sequence-events.csv":proof?"proof-events.csv":"events.csv"),events);Debug.Log("[Color Ball V5] "+(singleSequence?"single sequence ":proof?"proof ":"combo checks ")+(pass?"PASS":"FAIL"));
                // Focused captures end here, after restoring the normal profile.
                if((proof||singleSequence)&&pass)EditorApplication.delayCall+=EditorApplication.ExitPlaymode;
            }
        }
    }
}
#endif
