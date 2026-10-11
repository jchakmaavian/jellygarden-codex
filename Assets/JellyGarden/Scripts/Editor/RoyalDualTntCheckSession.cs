using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JellyGarden.Scripts;
using JellyGarden.Scripts.Juice;
using RoyalThrone.ReferenceGameplay;
using RoyalThrone.Testing;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace RoyalThrone.Editor
{
    public static class RoyalDualTntCheckSession
    {
        const string Folder = "Design/Effects/Dual-TNT-test/";
        static int checks; static bool complete;
        static readonly List<string> errors = new List<string>();
        static void Check(bool ok, string message)
        {
            File.AppendAllText(Folder + "checks.txt", (ok ? "PASS " : "FAIL ") + message + "\n");
            if (!ok) throw new Exception(message); checks++;
        }
        static void Log(string message, string stack, LogType kind)
        { if (kind == LogType.Error || kind == LogType.Exception || kind == LogType.Assert) errors.Add(message); }
        public static void Run()
        {
            if (!Application.isPlaying || RoyalSave.Testing) throw new Exception("Normal Play Mode profile required");
            Directory.CreateDirectory(Folder); Directory.CreateDirectory(Folder + "Frames");
            checks=0; complete=false; errors.Clear(); File.WriteAllText(Folder + "checks.txt", "RUNNING\n");
            RoyalRegressionRunner.Run(Suite());
        }
        static bool Blast() => Object.FindObjectsByType<SortingGroup>().Any(g => g.name.StartsWith("TntTntComboExplodeParticles"));
        static void Capture(int frame)
        {
            var image=ScreenCapture.CaptureScreenshotAsTexture(); var old=RenderTexture.active;
            var rt=RenderTexture.GetTemporary(360,640,0); var small=new Texture2D(360,640,TextureFormat.RGB24,false);
            try { Graphics.Blit(image,rt); RenderTexture.active=rt; small.ReadPixels(new Rect(0,0,360,640),0,0); small.Apply(); File.WriteAllBytes(Folder+"Frames/"+frame.ToString("D4")+".png",small.EncodeToPNG()); }
            finally { RenderTexture.active=old; RenderTexture.ReleaseTemporary(rt); Object.Destroy(image); Object.Destroy(small); }
        }
        static IEnumerator Suite()
        {
            var qa=RoyalQaService.Instance; bool created=!qa;
            if (!qa) qa=new GameObject("Dual TNT QA").AddComponent<RoyalQaService>();
            float capture=Time.captureDeltaTime, clock=Time.timeScale, speed=qa.Speed;
            int rate=Application.targetFrameRate, vsync=QualitySettings.vSyncCount, coins=InitScript.Gems, lives=InitScript.lifes;
            bool visible=qa.Visible, enabled=qa.enabled;
            Application.logMessageReceived+=Log;
            try
            {
                qa.StartSession(); Check(RoyalSave.Testing,"Player profile isolated"); qa.Visible=false; qa.Speed=1; Time.timeScale=1;
                yield return qa.Practice(); yield return qa.Settled();
                yield return RoyalSuperBombFuseChecks.Run(LevelManager.THIS,Check);
                foreach(int fps in new[]{15,30,60}) foreach(bool vertical in new[]{false,true})
                {
                    qa.enabled=true; yield return qa.Practice(); yield return qa.Settled(); qa.enabled=false;
                    Time.captureDeltaTime=1f/fps; Application.targetFrameRate=fps; QualitySettings.vSyncCount=0;
                    var lm=LevelManager.THIS; var fx=RoyalPowerupDirector.Get;
                    var a=lm.GetSquare(3,3).item; var b=lm.GetSquare(vertical?3:4,vertical?4:3).item;
                    a.NextType=b.NextType=ItemsTypes.PACKAGE; a.ChangeType(); b.ChangeType();
                    yield return new WaitForSeconds(1.6f);
                    lm.DragBlocked=false;
                    string name=fps+" FPS "+(vertical?"vertical":"horizontal");
                    Check(a.CanAcceptInput && b.CanAcceptInput,name+": both TNTs accept the initial input");
                    int plays=fx.NativeTntComboPlays, moves=lm.Limit; Vector3 delta=a.transform.position-b.transform.position;
                    a.SwitchDirection(delta); a.SwitchDirection(delta);
                    Check(!RoyalPowerupDirector.Tap(a) && !RoyalPowerupDirector.Tap(b),name+": duplicate input blocked during merge");
                    float deadline=Time.realtimeSinceStartup+20, preparedAt=-1, blastAt=-1; int frames=0; bool actor=false, unsupported=false, sunShown=false, wrongBand=false;
                    var sun=UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ReferenceGameplay/Art/Resources/ReferenceGameplay/Sprites/Items-SpecialItems-TntItem-Combo-kemer__21603.png");
                    while((RoyalPowerupDirector.Busy || lm.InputSwaps>0 || frames==0) && Time.realtimeSinceStartup<deadline)
                    {
                        yield return new WaitForEndOfFrame(); frames++;
                        if(ReferenceActivationVisual.ActiveCount>0) { actor=true; if(preparedAt<0) preparedAt=Time.time; }
                        foreach(var view in Object.FindObjectsByType<ReferenceActivationVisual>()) if(view.Active)
                        foreach(var renderer in view.GetComponentsInChildren<SpriteRenderer>(true)) if(renderer.name=="TntKemer" && renderer.enabled)
                        { sunShown=true; wrongBand |= renderer.sprite!=sun; }
                        if(Blast() && blastAt<0) blastAt=Time.time;
                        foreach(var ps in Object.FindObjectsByType<ParticleSystem>()) if(ps.particleCount>0)
                        { var renderer=ps.GetComponent<ParticleSystemRenderer>(); unsupported |= renderer && (!renderer.sharedMaterial || !renderer.sharedMaterial.shader.isSupported); }
                        if(fps==60 && !vertical && frames%3==0) Capture(frames);
                    }
                    Check(actor && fx.NativeTntComboPlays==plays+1,name+": one dedicated native dual animation");
                    Check(sun && sunShown && !wrongBand,name+": approved sun band used throughout animation, without TNT lettering");
                    Check(Mathf.Abs(fx.LastTntComboPreparation-1.583333f)<.002f,name+": authored 1.583-second preparation retained");
                    Check(blastAt>=0 && preparedAt>=0 && blastAt-preparedAt>=1.583333f-2f/fps,name+": blast follows preparation");
                    Check(lm.Limit==moves-1,name+": rapid repeated input costs exactly one move");
                    Check(!unsupported,name+": live particle shaders supported");
                    qa.enabled=true; yield return qa.Settled();
                    Check(lm.BoardInputAllowed && !RoyalPowerupDirector.Busy,name+": board refills and input recovers");
                    yield return new WaitForSeconds(3);
                    Check(ReferenceActivationVisual.ActiveCount==0 && ReferenceVfxPool.Active==0,name+": actors and particle leases retire");
                }
                Check(errors.Count==0,"No new Unity runtime errors"); complete=true;
            }
            finally
            {
                RoyalPowerupDirector.Get?.Cancel(); qa.enabled=true;
                Application.logMessageReceived-=Log;
                RoyalRegressionRunner.Run(Restore(qa,created,enabled,visible,speed,capture,clock,rate,vsync,coins,lives));
            }
        }
        static IEnumerator Restore(RoyalQaService qa,bool created,bool enabled,bool visible,float speed,float capture,float clock,int rate,int vsync,int coins,int lives)
        {
            yield return qa.EndSession(); qa.enabled=enabled; qa.Visible=visible; qa.Speed=speed;
            Time.captureDeltaTime=capture; Time.timeScale=clock; Application.targetFrameRate=rate; QualitySettings.vSyncCount=vsync;
            Check(!RoyalSave.Testing && InitScript.Gems==coins && InitScript.lifes==lives,"Real player balance and profile restored");
            if(created) Object.Destroy(qa.gameObject);
            File.AppendAllText(Folder+"checks.txt",(complete?"ALL DUAL TNT CHECKS PASSED ":"INCOMPLETE ")+checks+" checks\n");
        }
    }
}
