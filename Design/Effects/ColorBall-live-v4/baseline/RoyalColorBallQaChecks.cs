#if UNITY_EDITOR && !ROYAL_DISABLE_QA
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JellyGarden.Scripts;
using JellyGarden.Scripts.Juice;
using RoyalThrone.Testing;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace RoyalThrone.Editor
{
    // Tests the actual QA command entry points, with the real resolver/refill running.
    public static class RoyalColorBallQaChecks
    {
        const string Folder="Design/Effects/ColorBall-live-v3/";
        static readonly List<string> checks=new List<string>(),errors=new List<string>();
        static RoyalQaService qa;
        static bool running;
        [MenuItem("Tools/Royal Throne/Color Ball/Verify QA activation and Rocket combo %#F5")]
        public static void Run()
        {
            qa=RoyalQaService.Instance;
            if(running||!EditorApplication.isPlaying||!qa||!qa.Ready)throw new InvalidOperationException("Open the game in Play Mode first.");
            running=true;checks.Clear();errors.Clear();Directory.CreateDirectory(Folder);
            RoyalRegressionRunner.Run(Suite());
        }
        static void Check(bool ok,string message)
        { if(!ok)throw new Exception(message);checks.Add("PASS "+message); }
        static void Log(string message,string stack,LogType type)
        { if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message); }
        static IEnumerator Command(string name)
        {
            int done=qa.Completed,failed=qa.Failed;qa.Request(name);float end=Time.realtimeSinceStartup+35;
            while(qa.Busy&&Time.realtimeSinceStartup<end)yield return null;
            Check(!qa.Busy&&qa.Completed==done+1&&qa.Failed==failed,"QA control completes: "+name+" / "+qa.Status);
        }
        static void Capture(string name)
        {
            var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Folder+name+".png",t.EncodeToPNG());Object.Destroy(t);
        }
        static IEnumerator Observe(string command,string label,bool convert,Item[] untouched=null,bool record=false)
        {
            var director=RoyalPowerupDirector.Get;int done=qa.Completed,failed=qa.Failed,launches=director.RocketLaunches;
            qa.Visible=false;qa.Request(command);
            float end=Time.realtimeSinceStartup+35,start=Time.realtimeSinceStartup,lastCapture=-1;
            bool sawSpin=false,sawBeams=false,sawFinish=false,sawReveal=false,sawIdle=false;
            bool noEarlyClear=true,noEarlyLaunch=true,noRefill=true,otherBallsStable=true;
            bool settledCapture=false;int arrivals=0,peak=0,index=0,beamPeak=0;
            float boostStart=-1,boostEnd=-1;
            var timestamps=new List<string>();
            string frames="Temp/ColorBall-live-v3-"+label+"/";if(record)Directory.CreateDirectory(frames);
            while(qa.Busy&&Time.realtimeSinceStartup<end)
            {
                yield return new WaitForEndOfFrame();
                var balls=Object.FindObjectsByType<ColorBallFrameAnimator>(FindObjectsSortMode.None);
                if(!sawReveal&&balls.Any(b=>b.Current==ColorBallFrameAnimator.Phase.Reveal&&b.Frame>=18)){Capture(label+"-reveal");sawReveal=true;}
                if(!sawIdle&&balls.Any(b=>b.Current==ColorBallFrameAnimator.Phase.Idle)){Capture(label+"-idle");sawIdle=true;}
                if(ColorBallBoostView.Active>0)
                {
                    if(boostStart<0)boostStart=Time.time;
                    boostEnd=Time.time;
                    beamPeak=Mathf.Max(beamPeak,ColorBallTargetVfx.ActiveBeams);
                    peak=Mathf.Max(peak,ColorBallBoostView.Active);
                    var boost=Object.FindObjectsByType<ColorBallBoostView>(FindObjectsSortMode.None).First(b=>b.Current!=ColorBallBoostView.Phase.Idle);
                    bool intro=boost.Current==ColorBallBoostView.Phase.Intro;
                    if(intro)noEarlyClear&=director.ColorBallBeamArrivals==0;
                    if(boost.Current==ColorBallBoostView.Phase.Loop)sawSpin=true;
                    noEarlyLaunch&=director.RocketLaunches==launches;
                    noRefill&=!LevelManager.THIS.GetItems().Any(i=>i&&(i.falling||i.justCreatedItem));
                    if(untouched!=null)otherBallsStable&=untouched.All(i=>i&&!i.destroying&&i.currentType==ItemsTypes.BOMB&&i.color==555);
                    if(director.ColorBallBeamArrivals>=3&&!sawBeams){Capture(label+"-beams");sawBeams=true;}
                    arrivals=Mathf.Max(arrivals,director.ColorBallBeamArrivals);
                    if(boost.Current==ColorBallBoostView.Phase.Finish&&!sawFinish){Capture(label+"-finish");sawFinish=true;}
                }
                else if(sawFinish&&!settledCapture){Capture(label+"-handoff-refill");settledCapture=true;}
                if(record&&Time.realtimeSinceStartup-lastCapture>=1f/15f)
                {
                    lastCapture=Time.realtimeSinceStartup;
                    var t=ScreenCapture.CaptureScreenshotAsTexture();
                    File.WriteAllBytes(frames+index.ToString("D4")+".jpg",t.EncodeToJPG(90));Object.Destroy(t);
                    timestamps.Add((lastCapture-start).ToString("F5",System.Globalization.CultureInfo.InvariantCulture));index++;
                }
            }
            if(record)File.WriteAllLines(frames+"timestamps.txt",timestamps);
            Check(!qa.Busy&&qa.Completed==done+1&&qa.Failed==failed,label+": command finishes through real board resolver");
            Check(sawSpin&&sawBeams&&sawFinish&&arrivals>=3,label+": charged loop, multiple beams, authored finish observed");
            Check(beamPeak>=2,label+": overlapping beam volley observed");
            Check(arrivals==director.ColorBallSelectedTargets,label+": every selected target received exactly one impact");
            Check(ColorBallTargetVfx.PoolSize==208,label+": fixed VFX pool reused without growth");
            checks.Add("MEASURE "+label+": boost "+(boostEnd-boostStart).ToString("F2")+" game seconds; "+arrivals+" targets; peak "+beamPeak+" beams");
            Check(noEarlyClear&&noEarlyLaunch,label+": no clear before beam arrival and no Rocket launch before finish");
            Check(noRefill,label+": no falling/refill under the active Color Ball");
            Check(peak==1&&otherBallsStable,label+": one activation visual; other spawned Color Balls are not color-matched/chained");
            Check(ColorBallBoostView.Active==0&&ColorBallTargetVfx.Active==0&&!RoyalPowerupDirector.Busy,label+": all visual and gameplay work released");
            Check(!LevelManager.THIS.DragBlocked&&!LevelManager.THIS.IsResolvingMatches,label+": board accepts input after refill");
            Check(convert?director.ColorBallConversions>0&&director.RocketLaunches>launches:director.ColorBallConversions==0,label+": correct standalone / Rocket conversion behavior");
            if(command=="Color-ball effect"||command=="Combine selected")Check(sawReveal&&sawIdle,label+": QA control includes Reveal then Idle before activation");
            Capture(label+"-settled");
        }
        static IEnumerator Suite()
        {
            bool success=false;Application.logMessageReceived+=Log;
            try
            {
                qa.Loop=false;if(RoyalSave.Testing)yield return qa.EndSession();
                int coins=InitScript.Gems,lives=InitScript.lifes;
                qa.StartSession();qa.Level=1;qa.Speed=.5f;
                yield return Observe("Color-ball effect","effect-half",false);
                qa.Speed=1;yield return Command("Practice board");
                var others=new List<Item>();
                foreach(var p in new[]{new Vector2Int(1,3),new Vector2Int(2,4),new Vector2Int(3,3)})
                {
                    qa.Column=p.x;qa.Row=p.y;qa.Gem=0;yield return Command("Spawn color ball");
                    var item=LevelManager.THIS.GetSquare(p.x,p.y).item;
                    Check(item.color==555&&item.currentType==ItemsTypes.BOMB,"QA spawn uses real Color Ball sentinel color");
                    if(p.x!=3)others.Add(item);
                }
                yield return new WaitForSeconds(1.2f);
                yield return Observe("Activate selected","spawn-activate-full",false,others.ToArray(),true);
                yield return Command("Practice board");qa.PowerA=4;qa.PowerB=1;qa.Column=3;qa.Row=3;
                yield return Observe("Combine selected","rocket-combo-full",true,null,true);
                Check(errors.Count==0,"No new runtime errors through QA Color Ball controls");
                yield return qa.EndSession();
                Check(InitScript.Gems==coins&&InitScript.lifes==lives&&!RoyalSave.Testing,"Normal player profile restored");success=true;
            }
            finally
            {
                qa.Loop=false;if(RoyalSave.Testing)qa.StartCoroutine(qa.EndSession());
                Application.logMessageReceived-=Log;running=false;
                checks.Add(success?"ALL COLOR BALL QA PATHS PASSED":"FAILED - inspect Console");checks.AddRange(errors);
                File.WriteAllLines(Folder+"checks.txt",checks);Debug.Log("[Color Ball QA paths] "+(success?"PASS":"FAIL"));
            }
        }
    }
}
#endif
