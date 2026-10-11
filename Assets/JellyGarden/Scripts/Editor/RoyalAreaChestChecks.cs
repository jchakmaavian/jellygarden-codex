using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using JellyGarden.Scripts;
using RoyalThrone.Presentation;
using RoyalThrone.Testing;
using UnityEngine;

namespace RoyalThrone.Editor
{
    public static class RoyalAreaChestChecks
    {
        const string Folder = "Design/Effects/Area-chest-opening/";
        static int passes;
        static void Check(bool ok, string text)
        {
            File.AppendAllText(Folder + "checks.txt", (ok ? "PASS " : "FAIL ") + text + "\n");
            if (!ok) throw new Exception(text); passes++;
        }
        public static void Run() { Directory.CreateDirectory(Folder); File.WriteAllText(Folder + "checks.txt", "RUNNING\n"); passes = 0; RoyalRegressionRunner.Run(Drive()); }
        static IEnumerator Wait(float s) { float end=Time.unscaledTime+s; while(Time.unscaledTime<end) yield return null; }
        static IEnumerator Shot(string name) { yield return new WaitForEndOfFrame(); var texture=ScreenCapture.CaptureScreenshotAsTexture(); File.WriteAllBytes(Folder+name+".png",texture.EncodeToPNG()); UnityEngine.Object.Destroy(texture); }
        static IEnumerator Record(float duration, int first)
        {
            float end=Time.unscaledTime+duration, next=0; int frame=first;
            while(Time.unscaledTime<end)
            {
                yield return new WaitForEndOfFrame();
                if(Time.unscaledTime<next) continue;
                next=Time.unscaledTime+.06f;
                var image=ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(Folder+"frame-"+(frame++).ToString("D2")+".png",image.EncodeToPNG()); UnityEngine.Object.Destroy(image);
            }
        }
        static IEnumerator Drive()
        {
            var home=UnityEngine.Object.FindAnyObjectByType<RoyalHomeScreen>();
            var qa=RoyalQaService.Instance; bool created=!qa; if(!qa) qa=new GameObject("Chest QA").AddComponent<RoyalQaService>();
            bool visible=qa.Visible; float clock=Time.timeScale; int rate=Application.targetFrameRate;
            int coins=InitScript.Gems, lives=InitScript.lifes;
            string[] keys={RoyalAreas.CurrentKey,RoyalAreas.DoneKey,RoyalAreas.SpentKey,RoyalAreas.EnteredKey,RoyalAreas.LayoutVersionKey,RoyalAreas.MigrationCreditKey,"RoyalThrone.HomeMotion","Gems","Stripes"};
            var values=new Dictionary<string,int>(); var had=new HashSet<string>();
            if(RoyalSave.Testing) throw new Exception("Another QA session is active");
            try
            {
                Check(home && home.IsHomeVisible,"Home visible"); qa.StartSession(); qa.Visible=false;
                foreach(var k in keys) { values[k]=RoyalSave.GetInt(k); if(RoyalSave.HasKey(k)) had.Add(k); }
                foreach(int fps in new[]{15,30,60})
                {
                    foreach(bool motion in new[]{true,false})
                    {
                        home.ClosePanel(); yield return Wait(3);
                        RoyalSave.SetInt(RoyalAreas.CurrentKey,1); RoyalSave.SetInt(RoyalAreas.EnteredKey,1);
                        RoyalSave.SetInt(RoyalAreas.DoneKey,(1<<RoyalAreas.All[0].tasks.Length)-1);
                        RoyalSave.SetInt("RoyalThrone.HomeMotion",motion?1:0);
                        home.RefreshProgress(); home.OpenPanel(RoyalAreaView.Chest);
                        Time.timeScale=0;
                        yield return Wait(.75f);
                        var view=home.AreaView;
                        var animation=view.GetComponentInChildren<RoyalAreaChestAnimation>(true);
                        Check(view.StageStep==1 && animation && !animation.Opened,"Closed chest waits for a tap at timeScale zero");
                        if(fps==30 && motion) yield return Shot("01-closed");
                        int before=InitScript.Gems, rockets=RoyalSave.GetInt("Stripes");
                        Application.targetFrameRate=fps;
                        view.StageButton.onClick.Invoke();
                        if(fps==30 && motion) yield return Record(.45f, 0); else yield return Wait(.45f);
                        Check(view.StageStep==2 && animation.Opened,"Lid opens after the tap");
                        if(fps==30 && motion) yield return Shot("02-opening-burst");
                        for(int i=0;i<8;i++) view.StageButton.onClick.Invoke();
                        if(fps==30 && motion) yield return Record(1.15f, 8); else yield return Wait(1.15f);
                        Check(view.StageStep==2,"Opening taps cannot skip the reveal");
                        Check(InitScript.Gems==before+RoyalAreas.All[0].rewardCoins && RoyalSave.GetInt("Stripes")==rockets+1,"Correct coins and rocket paid once");
                        Check(!RoyalAreas.CollectChest(out _),"Second collection refused");
                        if(fps==30 && motion) yield return Shot("03-open-rewards");
                        view.StageButton.onClick.Invoke(); yield return Wait(.15f);
                        Check(view.StageStep==3,"Continue accepted after reveal");
                        home.ClosePanel(); yield return Wait(3); yield return null;
                    }
                }
            }
            finally
            {
                Time.timeScale=clock; Application.targetFrameRate=rate;
                home.ClosePanel(); if(RoyalSave.Testing) foreach(var k in keys) { if(had.Contains(k)) RoyalSave.SetInt(k,values[k]); else RoyalSave.DeleteKey(k); }
                RoyalRegressionRunner.Run(Restore(qa,home,created,visible,coins,lives));
            }
        }
        static IEnumerator Restore(RoyalQaService qa,RoyalHomeScreen home,bool created,bool visible,int coins,int lives)
        {
            yield return qa.EndSession(); qa.Visible=visible; if(created) UnityEngine.Object.Destroy(qa.gameObject);
            home.RefreshProgress(); home.AreaView.Scene.ShowCurrent();
            Check(!RoyalSave.Testing && InitScript.Gems==coins && InitScript.lifes==lives,"Real player balance and save profile restored");
            File.AppendAllText(Folder+"checks.txt","FINISHED "+passes+" checks\n");
        }
    }
}
