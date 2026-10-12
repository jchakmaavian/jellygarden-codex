#if UNITY_EDITOR && !ROYAL_DISABLE_QA
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JellyGarden.Scripts;
using JellyGarden.Scripts.GUI;
using RoyalThrone.Testing;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace RoyalThrone.ReferenceGameplay.Editor
{
    [InitializeOnLoad]
    public static class ReferenceArcherChecks
    {
        const string Folder="Design/Archer-Reference/Review";
        static bool running;
        static ReferenceArcherChecks(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            if(running||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists("Temp/ArcherChecks.request"))return;
            if(!EditorApplication.isPlaying){EditorApplication.isPlaying=true;return;}
            var title=UnityEngine.Object.FindAnyObjectByType<RoyalThrone.Presentation.RoyalTitleScreen>();
            if(title){title.BeginPlay();return;}
            if(!LevelManager.THIS||!RoyalQaService.Instance||!InitScript.Instance)return;
            File.Delete("Temp/ArcherChecks.request");running=true;RoyalThrone.Editor.RoyalRegressionRunner.Run(Run());
        }
        static void Check(bool ok,string message)
        {File.AppendAllText(Folder+"/checks.txt",(ok?"PASS ":"FAIL ")+message+"\n");if(!ok)throw new Exception(message);}
        public static IEnumerator Run()
        {
            Directory.CreateDirectory(Folder+"/Frames");File.WriteAllText(Folder+"/checks.txt","Archer native motion, row sweep, VFX and SFX checks\n");
            var lm=LevelManager.THIS;var qa=RoyalQaService.Instance;var actions=InputManager.Instance.inputActionAsset;var oldDevices=actions.devices;Mouse mouse=null;
            int oldRate=Time.captureFramerate,stock=RoyalSave.GetInt(BoostType.RoyalArrow.ToString()),coins=InitScript.Gems,sound=PlayerPrefs.GetInt("Sound",1);
            float oldScale=Time.timeScale;bool background=Application.runInBackground;ReferenceArcherSequence probe=null;
            var inputSettings=InputSystem.settings;
            var oldInputBehavior=inputSettings.editorInputBehaviorInPlayMode;var oldBackground=inputSettings.backgroundBehavior;
            try
            {
                // The automated editor can be behind another app. Route only the
                // synthetic device to Play Mode, and restore the original settings.
                inputSettings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                inputSettings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
                Application.runInBackground=true;EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
                qa.StartSession();qa.Level=1;qa.Speed=1;qa.Loop=false;Time.timeScale=1;
                RoyalThrone.Campaign.RoyalReferenceCampaign.Select(false);yield return qa.Practice();qa.Visible=false;
                Check(RoyalSave.Testing&&ReferenceArcherSequence.Available&&ReferenceHammerSequence.Available,"Isolated QA profile; archer and existing hammer resources resolve");
                probe=new GameObject("Archer timing probe").AddComponent<ReferenceArcherSequence>();
                foreach(float step in new[]{1f/60,1f/30,1f/15,.53f,2f})
                {
                    var positions=new[]{new Vector3(-3,0),new Vector3(-1,0),new Vector3(0,0),new Vector3(3,0)};var hits=new List<int>();bool early=false;
                    probe.Begin(positions,1,i=>{hits.Add(i);if(probe.TipX<positions[i].x||probe.Elapsed<ReferenceArcherSequence.LaunchTime)early=true;});
                    while(probe.Running)probe.Advance(step,false);
                    Check(!early&&hits.SequenceEqual(new[]{0,1,2,3}),"Sparse row hit once in left-to-right order at arrow arrival; delta="+step.ToString("F4"));yield return null;
                }
                int canceledHits=0;probe.Begin(new[]{new Vector3(0,0),new Vector3(1,0)},1,i=>canceledHits++);
                probe.Advance(.17f,false);float elapsed=probe.Elapsed;var trail=GameObject.Find("Archer - ArcherTrailRfv387");
                var systems=trail.GetComponentsInChildren<ParticleSystem>();var times=systems.Select(p=>p.time).ToArray();
                probe.Advance(1,true);Check(probe.Elapsed==elapsed&&systems.Select(p=>p.time).SequenceEqual(times),"Pause freezes motion and all native particle clocks");
                probe.Cancel();probe.Advance(5,false);yield return null;
                Check(canceledHits==0&&!GameObject.Find("Archer - imported source motion")&&!GameObject.Find("Archer - ArcherTrailRfv387"),"Cancel before contact suppresses damage and cleans up objects");
                UnityEngine.Object.Destroy(probe.gameObject);probe=null;
                var bar=ReferenceBoosterBar.Instance;
                foreach(int row in new[]{3,0,7})
                {
                    Time.captureFramerate=row==3?60:row==0?30:15;
                    RoyalThrone.Campaign.RoyalReferenceCampaign.Select(false);yield return qa.Practice();qa.Visible=false;lm.colorLimit=5;
                    if(row==3){qa.Column=3;qa.Row=3;yield return qa.Execute("Spawn blocker");qa.Visible=false;}
                    var target=lm.GetSquare(3,row);int moves=lm.Limit;RoyalSave.SetInt(BoostType.RoyalArrow.ToString(),3);
                    int expected=lm.squaresArray.Count(s=>s&&!s.IsNone()&&s.row==row&&s.type!=SquareTypes.UNDESTROYABLE&&(s.item||s.block.Count>0));
                    bar.Choose(BoostType.RoyalArrow);Check(!bar.TryUse(null)&&bar.Stock(1)==3,"Invalid target keeps archer stock");
                    if(row==3)
                    {
                        mouse=InputSystem.AddDevice<Mouse>();actions.devices=new InputDevice[]{mouse};Vector2 point=Camera.main.WorldToScreenPoint(target.transform.position);
                        InputSystem.QueueStateEvent(mouse,new MouseState{position=point,buttons=1});
                        float wait=Time.realtimeSinceStartup+4;while(!bar.Busy&&Time.realtimeSinceStartup<wait)yield return null;
                        InputSystem.QueueStateEvent(mouse,new MouseState{position=point,buttons=0});
                        Check(bar.Busy,"Board-pointer targeting starts archer on the chosen row");actions.devices=oldDevices;InputSystem.RemoveDevice(mouse);mouse=null;
                    }
                    else Check(bar.TryUse(target),"Archer starts on edge row "+row);
                    Check(!bar.TryUse(target)&&bar.LastHitCount==0,"Duplicate input rejected; no damage during anticipation");
                    var active=bar.GetComponent<ReferenceArcherSequence>();int frame=0;bool sawParticles=false;float deadline=Time.realtimeSinceStartup+90;
                    while(bar.Busy&&Time.realtimeSinceStartup<deadline)
                    {
                        if(active.Running&&active.Launched&&UnityEngine.Object.FindObjectsByType<ParticleSystem>().Any(p=>p.transform.root.name.StartsWith("Archer -")&&p.particleCount>0))sawParticles=true;
                        if(row==3&&frame<120)
                        {
                            yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();
                            File.WriteAllBytes(Folder+"/Frames/"+frame.ToString("D4")+".png",image.EncodeToPNG());UnityEngine.Object.Destroy(image);
                        }
                        frame++;yield return null;
                    }
                    Check(!bar.Busy&&active.Impacted,"Animation and board resolution complete on row "+row);
                    Check(bar.LastHitCount==expected&&bar.Stock(1)==2&&lm.Limit==moves,"Selected row hit once; one archer spent; no move consumed");
                    Check(sawParticles,"Native archer particles emit during flight");
                    if(row==3){Check(target.block.Count==0&&target.type!=SquareTypes.WIREBLOCK,"Archer clears the selected row's wire blocker");File.WriteAllText(Folder+"/capture.json","{\"fps\":60,\"frames\":"+Math.Min(frame,120)+",\"source\":\"Unity Game view; synthetic QA board\"}");}
                    Check(lm.GetItems().Where(i=>i).All(i=>i.square&&i.square.item==i),"Refill preserves piece-to-cell ownership");
                    Check(!active.Running&&!GameObject.Find("Archer - imported source motion"),"Archer releases animation objects after resolution");
                }
                yield return qa.Practice();qa.Visible=false;RoyalSave.SetInt(BoostType.RoyalArrow.ToString(),3);bar.Choose(BoostType.RoyalArrow);Check(bar.TryUse(lm.GetSquare(3,3)),"Start archer for game pause/cancel check");
                var sequence=bar.GetComponent<ReferenceArcherSequence>();lm.gameStatus=GameState.Pause;float held=sequence.Elapsed;
                for(int i=0;i<8;i++)yield return null;
                Check(sequence.Elapsed==held&&bar.LastHitCount==0,"Game pause freezes archer before any cell damage");lm.gameStatus=GameState.Playing;
                bar.enabled=false;yield return null;Check(!bar.Busy&&!sequence.Running&&!ReferenceBoosterBar.OwnsInput,"Disabling bar cancels archer and releases input");bar.enabled=true;
                yield return qa.EndSession();Check(!RoyalSave.Testing&&RoyalSave.GetInt(BoostType.RoyalArrow.ToString())==stock&&InitScript.Gems==coins,"Normal inventory and coins restored");
                Check(PlayerPrefs.GetInt("Sound",1)==sound,"Sound preference preserved");
            }
            finally
            {
                if(probe)UnityEngine.Object.Destroy(probe.gameObject);actions.devices=oldDevices;if(mouse!=null)InputSystem.RemoveDevice(mouse);
                InputSystem.settings.editorInputBehaviorInPlayMode=oldInputBehavior;InputSystem.settings.backgroundBehavior=oldBackground;
                Time.captureFramerate=oldRate;Time.timeScale=oldScale;Application.runInBackground=background;running=false;
                if(lm&&lm.gameStatus==GameState.Pause)lm.gameStatus=GameState.Playing;
                if(RoyalSave.Testing)RoyalThrone.Editor.RoyalRegressionRunner.Run(qa.EndSession());
            }
            Check(InputSystem.settings.editorInputBehaviorInPlayMode==oldInputBehavior&&InputSystem.settings.backgroundBehavior==oldBackground&&Time.captureFramerate==oldRate,"QA input settings and capture clock restored");
            File.AppendAllText(Folder+"/checks.txt","ALL ARCHER CHECKS PASSED\n");
        }
    }
}
#endif
