#if UNITY_EDITOR && !ROYAL_DISABLE_QA
using System;
using System.Collections;
using System.IO;
using System.Linq;
using JellyGarden.Scripts;
using JellyGarden.Scripts.GUI;
using JellyGarden.Scripts.Juice;
using RoyalThrone.Testing;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace RoyalThrone.ReferenceGameplay.Editor
{
    [InitializeOnLoad]
    public static class ReferenceHammerChecks
    {
        const string Folder="Design/Hammer-Reference/Review";
        static bool running;
        static ReferenceHammerChecks(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            if(running||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists("Temp/HammerChecks.request"))return;
            if(!EditorApplication.isPlaying){EditorApplication.isPlaying=true;return;}
            var title=UnityEngine.Object.FindAnyObjectByType<RoyalThrone.Presentation.RoyalTitleScreen>();
            if(title){title.BeginPlay();return;}
            if(!LevelManager.THIS||!RoyalQaService.Instance||!InitScript.Instance)return;
            File.Delete("Temp/HammerChecks.request");running=true;RoyalThrone.Editor.RoyalRegressionRunner.Run(Run());
        }
        static void Check(bool ok,string message)
        {File.AppendAllText(Folder+"/gameplay-checks.txt",(ok?"PASS ":"FAIL ")+message+"\n");if(!ok)throw new Exception(message);}
        static void Capture(int frame)
        {var im=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Folder+"/GameplayFrames/"+frame.ToString("D4")+".png",im.EncodeToPNG());UnityEngine.Object.Destroy(im);}
        public static IEnumerator Run()
        {
            Directory.CreateDirectory(Folder+"/GameplayFrames");File.WriteAllText(Folder+"/gameplay-checks.txt","Hammer animation / native VFX / SFX / gameplay checks\n");
            var lm=LevelManager.THIS;var qa=RoyalQaService.Instance;var actions=InputManager.Instance.inputActionAsset;var oldDevices=actions.devices;Mouse mouse=null;
            EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
            int oldRate=Time.captureFramerate,stock=RoyalSave.GetInt(BoostType.RoyalHammer.ToString()),coins=InitScript.Gems,sound=PlayerPrefs.GetInt("Sound",1);
            bool background=Application.runInBackground;
            try
            {
                Application.runInBackground=true;qa.StartSession();qa.Level=1;qa.Speed=1;qa.Loop=false;Time.timeScale=1;
                RoyalThrone.Campaign.RoyalReferenceCampaign.Select(false);yield return qa.Practice();qa.Visible=false;
                Check(RoyalSave.Testing&&ReferenceHammerSequence.Available,"Isolated profile and all native hammer resources available");
                var bar=ReferenceBoosterBar.Instance;
                // Direct cue-crossing checks sample the real native split-head strike pose inside the callback.
                var probe=new GameObject("Hammer timing probe").AddComponent<ReferenceHammerSequence>();
                foreach(float step in new[]{1f/60,1f/30,1f/15,.53f})
                {
                    int hits=0;bool contactPose=false;
                    probe.Begin(lm.GetSquare(3,3).transform.position,lm.GetSquare(0,7).transform.position,lm.squareWidth*Mathf.Abs(lm.GameField.lossyScale.x),()=>{
                        hits++;var go=GameObject.Find("Hammer - imported source motion");contactPose=go.GetComponentsInChildren<SpriteRenderer>().Any(r=>r.name=="01_kafa"&&r.enabled);
                    });
                    while(probe.Running)probe.Advance(step,false);
                    Check(hits==1&&contactPose,"Exactly one callback at the native impact pose for step "+step.ToString("F4"));yield return null;
                }
                probe.Begin(lm.GetSquare(3,3).transform.position,Vector3.zero,1,()=>{throw new Exception("Canceled hammer delivered a hit");});
                probe.Advance(.1f,false);float held=probe.Elapsed;probe.Advance(1,true);
                Check(probe.Elapsed==held&&!probe.Impacted,"Pause prevents motion, VFX, and premature damage");probe.Cancel();probe.Advance(2,false);UnityEngine.Object.Destroy(probe.gameObject);yield return null;
                foreach(int col in new[]{3,0,7})
                {
                    Time.captureFramerate=col==3?60:col==0?30:15;
                    RoyalThrone.Campaign.RoyalReferenceCampaign.Select(false);yield return qa.Practice();qa.Visible=false;lm.colorLimit=5;
                    if(col==3){qa.Column=3;qa.Row=3;yield return qa.Execute("Spawn blocker");qa.Visible=false;}
                    var target=lm.GetSquare(col,3);int moves=lm.Limit;RoyalSave.SetInt(BoostType.RoyalHammer.ToString(),3);
                    var rail=GameObject.Find("Imported booster rail").GetComponent<RectTransform>();var rest=rail.anchoredPosition;
                    bar.Choose(BoostType.RoyalHammer);Check(!bar.TryUse(null)&&bar.Stock(0)==3,"Invalid target does not spend stock");
                    if(col==3)
                    {
                        mouse=InputSystem.AddDevice<Mouse>();actions.devices=new InputDevice[]{mouse};
                        Vector2 point=Camera.main.WorldToScreenPoint(target.transform.position);InputSystem.QueueStateEvent(mouse,new MouseState{position=point,buttons=1});
                        float timeout=Time.realtimeSinceStartup+4;while(!bar.Busy&&Time.realtimeSinceStartup<timeout)yield return null;
                        InputSystem.QueueStateEvent(mouse,new MouseState{position=point,buttons=0});
                        Check(bar.Busy,"Real board pointer starts hammer on the selected blocker");actions.devices=oldDevices;InputSystem.RemoveDevice(mouse);mouse=null;
                    }else Check(bar.TryUse(target),"Hammer starts in column "+col);
                    Check(!bar.TryUse(target),"Duplicate input rejected while hammer runs");
                    var sequence=bar.GetComponent<ReferenceHammerSequence>();Check(sequence&&sequence.Running&&bar.LastHitCount==0,"Flight begins before damage");
                    int frame=0;bool impactSeen=false;float deadline=Time.realtimeSinceStartup+90;
                    while(bar.Busy&&Time.realtimeSinceStartup<deadline)
                    {
                        if(sequence.Running&&!sequence.Impacted)CheckNoEarlyHit(bar);
                        if(sequence.Impacted)impactSeen=true;
                        CheckRail(rail,rest);
                        if(col==3&&frame<150){yield return new WaitForEndOfFrame();Capture(frame);}
                        frame++;yield return null;
                    }
                    Check(!bar.Busy&&impactSeen,"Hammer action and board resolution finish in column "+col);
                    Check(bar.LastHitCount==1&&bar.Stock(0)==2&&lm.Limit==moves,"Exactly one cell hit and one hammer spent; no move consumed");
                    if(col==3)Check(target.block.Count==0&&target.type!=SquareTypes.WIREBLOCK,"Hammer clears the targeted wire blocker");
                    Check(lm.GetItems().Where(i=>i).All(i=>i.square&&i.square.item==i),"Refill preserves piece-to-cell ownership");
                    Check(!sequence.Running&&!GameObject.Find("Hammer - imported source motion")&&!GameObject.Find("Hammer - native impact VFX"),"Animation and particle objects cleaned up");
                    if(col==3)File.WriteAllText(Folder+"/gameplay-capture.json","{\"fps\":60,\"frames\":"+Mathf.Min(frame,150)+",\"source\":\"Unity Game view; synthetic QA board\"}");
                }
                yield return qa.Practice();qa.Visible=false;RoyalSave.SetInt(BoostType.RoyalHammer.ToString(),3);bar.Choose(BoostType.RoyalHammer);bar.TryUse(lm.GetSquare(3,3));
                var active=bar.GetComponent<ReferenceHammerSequence>();yield return null;float elapsed=active.Elapsed;lm.gameStatus=GameState.Pause;
                for(int i=0;i<8;i++)yield return null;
                Check(active.Elapsed==elapsed&&bar.LastHitCount==0,"Game pause freezes an in-flight hammer");lm.gameStatus=GameState.Playing;
                bar.enabled=false;yield return null;Check(!bar.Busy&&!active.Running&&!ReferenceBoosterBar.OwnsInput,"Cancellation releases tool input without an impact");bar.enabled=true;
                yield return qa.EndSession();Check(!RoyalSave.Testing&&RoyalSave.GetInt(BoostType.RoyalHammer.ToString())==stock&&InitScript.Gems==coins,"Normal inventory and coins restored after QA");
                Check(PlayerPrefs.GetInt("Sound",1)==sound,"Sound preference preserved");File.AppendAllText(Folder+"/gameplay-checks.txt","ALL HAMMER CHECKS PASSED\n");
            }
            finally
            {
                actions.devices=oldDevices;if(mouse!=null)InputSystem.RemoveDevice(mouse);Time.captureFramerate=oldRate;Application.runInBackground=background;running=false;
                if(lm&&lm.gameStatus==GameState.Pause)lm.gameStatus=GameState.Playing;
                if(RoyalSave.Testing)RoyalThrone.Editor.RoyalRegressionRunner.Run(qa.EndSession());
            }
        }
        static void CheckNoEarlyHit(ReferenceBoosterBar bar){if(bar.LastHitCount!=0)Check(false,"No early hit during approach");}
        static void CheckRail(RectTransform rail,Vector2 rest){if((rail.anchoredPosition-rest).sqrMagnitude>.01f)Check(false,"Hammer keeps the bottom bar visible and stationary");}
    }
}
#endif
