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
namespace RoyalThrone.Boosters.Propeller.V4.Editor
{
    public static class PropellerTntRegression
    {
        const string Root="Assets/Boosters/Propeller/V4/Animation/Review/";
        [MenuItem("Tools/Royal Throne/Propeller V4/Record TNT before integration %&F9")]
        static void Before(){RoyalThrone.Editor.RoyalRegressionRunner.Run(Session("tnt-before"));}
        static IEnumerator Session(string label){var qa=RoyalQaService.Instance;if(!Application.isPlaying||!qa||!qa.Ready)throw new Exception("Enter gameplay first.");qa.StartSession();try{yield return Run(label);}finally{qa.StartCoroutine(qa.EndSession());}}
        public static IEnumerator Run(string label)
        {
            var qa=RoyalQaService.Instance;float capture=Time.captureDeltaTime;var random=UnityEngine.Random.state;var errors=new List<string>();
            Application.LogCallback log=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception)errors.Add(m);};Application.logMessageReceived+=log;
            try{
                UnityEngine.Random.InitState(8943);Time.captureDeltaTime=1f/30;qa.Request("Practice board");while(qa.Busy)yield return null;qa.Visible=false;
                qa.Column=3;qa.Row=3;qa.Request("Spawn TNT");while(qa.Busy)yield return null;
                var lm=LevelManager.THIS;var item=lm.GetSquare(3,3).item;var originals=lm.GetItems().ToArray();
                PropellerCapture.Begin(Root+".Capture/"+label);var trace=new List<string>();
                for(int i=0;i<24;i++){yield return new WaitForEndOfFrame();PropellerCapture.Frame();}
                if(!RoyalPowerupDirector.Tap(item))throw new Exception("TNT tap rejected.");
                for(int i=0;i<85;i++){
                    yield return new WaitForEndOfFrame();PropellerCapture.Frame();
                    var actors=UnityEngine.Object.FindObjectsByType<TNT3DAnimator>();
                    trace.Add(i+","+string.Join("|",actors.Select(a=>a.State.ToString()).OrderBy(s=>s))+","+originals.Count(x=>!x||x.destroying));
                }
                if(RoyalPowerupDirector.Busy||errors.Count>0)throw new Exception("TNT regression did not finish cleanly: "+string.Join(";",errors));
                File.WriteAllLines(Root+label+".csv",trace);File.WriteAllText(Root+label+".txt","PASS TNT creation, tap, explosion, per-cell destruction and cleanup; no errors.");
                Debug.Log("[Propeller V4] "+label+" PASS");
            }finally{PropellerCapture.End();Time.captureDeltaTime=capture;UnityEngine.Random.state=random;Application.logMessageReceived-=log;}
        }
    }
}
#endif
