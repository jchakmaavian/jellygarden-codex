#if UNITY_EDITOR && !ROYAL_DISABLE_QA
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JellyGarden.Scripts;
using JellyGarden.Scripts.Juice;
using JellyGarden.Scripts.Targets;
using RoyalThrone.ReferenceGameplay;
using RoyalThrone.Testing;
using UnityEditor;
using UnityEngine;

namespace RoyalThrone.Boosters.Propeller.V5.Editor
{
    public static class PropellerReferenceChecks
    {
        const string Output="Design/Inspection-rfv387/Propeller/Runtime-checks.txt";
        static readonly List<string> lines=new List<string>();
        static readonly List<Vector3> contacts=new List<Vector3>();
        static int carriedPeak;static bool running,errors;
        static RoyalQaService qa;
        static LevelManager LM=>LevelManager.THIS;
        [MenuItem("Tools/Royal Throne/Reference Gameplay/Check Propeller Reference Flow")]
        public static void Run()
        {
            qa=RoyalQaService.Instance;
            if(running||!Application.isPlaying||!qa||!qa.Ready)throw new InvalidOperationException("Ready gameplay in Play Mode is required.");
            running=true;lines.Clear();errors=false;
            Directory.CreateDirectory(Path.GetDirectoryName(Output));
            RoyalThrone.Editor.RoyalRegressionRunner.Run(Suite());
        }
        static void Check(bool ok,string label){lines.Add((ok?"PASS ":"FAIL ")+label);if(!ok)throw new InvalidOperationException(label);}
        static void Log(string text,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert){errors=true;lines.Add("ERROR "+text);}}
        static void Event(PropellerEvent e,PropellerVisualV5 view,Vector3 p)
        {
            if(e==PropellerEvent.PropellerTargetReached)contacts.Add(p);
            carriedPeak=Mathf.Max(carriedPeak,PropellerVisualV5.ActiveCarriedBoosters);
        }
        static IEnumerator Fresh()
        {
            qa.Request("Practice board");while(qa.Busy)yield return null;qa.Visible=false;
            LM.targetObject=new[]{new TargetObject{type=Target.COLLECT,color=4,targetCount=10000,countCollected=0}};
            foreach(var cell in LM.squaresArray)if(cell&&cell.item)cell.item.SetColor((cell.col+2*cell.row)%4);
            contacts.Clear();carriedPeak=0;
        }
        static Item Make(int x,int y,ItemsTypes type)
        {var item=LM.GetSquare(x,y).item;item.NextType=type;item.ChangeType();return item;}
        static IEnumerator FinishRegen(IEnumerator routine,Action completed)
        {yield return routine;completed();}
        static IEnumerator DuplicateSquareRegen()
        {
            yield return Fresh();
            var quad=new HashSet<Item>();
            for(int y=2;y<4;y++)for(int x=2;x<4;x++)
            {var item=LM.GetSquare(x,y).item;item.SetColor(4);quad.Add(item);}
            Check(RoyalPatterns.SquareAt(LM.GetSquare(2,2)).Count==4,"Equal-colour 2x2 is a Propeller formation");
            var groups=LM.GetMatches();
            Check(groups.Sum(g=>g.Count(quad.Contains))>quad.Count,"Regression fixture includes duplicate 2x2 membership");
            var method=typeof(LevelManager).GetMethod("RegenMatches",global::System.Reflection.BindingFlags.Instance|global::System.Reflection.BindingFlags.NonPublic);
            Check(method!=null,"Existing regeneration routine found");
            bool complete=false;
            LM.DragBlocked=true;
            var operation=LM.StartCoroutine(FinishRegen((IEnumerator)method.Invoke(LM,new object[]{false}),()=>complete=true));
            try
            {
                float until=Time.realtimeSinceStartup+3;
                while(!complete&&Time.realtimeSinceStartup<until)yield return null;
                Check(complete&&!LM.DragBlocked&&LM.GetMatches().Count==0,"Duplicate 2x2 regeneration finishes without a stuck input gate or manual recolouring");
            }
            finally{if(operation!=null)LM.StopCoroutine(operation);}
        }
        static IEnumerator Settled()
        {
            float until=Time.realtimeSinceStartup+20;
            do {carriedPeak=Mathf.Max(carriedPeak,PropellerVisualV5.ActiveCarriedBoosters);yield return null;}
            while((RoyalPowerupDirector.Busy||LM.IsResolvingMatches)&&Time.realtimeSinceStartup<until);
            Check(!RoyalPowerupDirector.Busy&&!LM.IsResolvingMatches,"Board resolves and input recovers");
            yield return new WaitForSeconds(1.2f);
            Check(PropellerVisualV5.ActiveCarriedBoosters==0&&RoyalPowerupDirector.Get.PropellerReservedTargets==0,"No carried booster or reserved target remains");
            Check(ReferenceActivationVisual.PlayingVoices==0,"No native flight audio remains");
            Check(LM.GetItems().Any(i=>i&&i.CanAcceptInput),"Live pieces accept input again");
        }
        static IEnumerator Suite()
        {
            bool own=!RoyalSave.Testing,success=false;
            Application.logMessageReceived+=Log;PropellerVisualV5.Event+=Event;
            try
            {
                if(own)qa.StartSession();Check(RoyalSave.Testing,"Isolated profile active");
                yield return DuplicateSquareRegen();
                yield return Fresh();
                // Standalone visual check of the exact takeoff-to-translation frame.
                var actor=PropellerVisualV5.Take(LM.GetSquare(3,3).transform.position,LM.squareWidth*Mathf.Abs(LM.GameField.lossyScale.x),false);
                actor.Activate(LM.GetSquare(6,6).transform.position,ItemsTypes.PACKAGE);
                while(!actor.FlightReady)yield return null;
                var native=actor.transform.Find("Imported Propeller activation");
                var carried=actor.transform.Find("Propeller paired booster");
                Check(native&&carried,"Native pair bodies exist");
                Vector3 nativeBefore=native.position,carriedBefore=carried.position;
                actor.BeginFlightTranslation();
                Check(Vector3.Distance(native.position,nativeBefore)<.0001f&&Vector3.Distance(carried.position,carriedBefore)<.0001f,"Both pair bodies preserve their position on translation handoff");
                actor.Release();RoyalPowerupDirector.Get.Cancel();contacts.Clear();carriedPeak=0;
                var wanted=LM.GetSquare(LM.maxCols-1,LM.maxRows-1);wanted.item.SetColor(4);
                Check(RoyalPowerupDirector.PropellerObjectiveScore(wanted,ItemsTypes.NONE)==1,"Remaining colour objective receives priority");
                LM.targetObject[0].countCollected=10000;
                Check(RoyalPowerupDirector.PropellerObjectiveScore(wanted,ItemsTypes.NONE)==0,"Completed objective no longer receives priority");
                LM.targetObject[0].countCollected=0;
                var origin=Make(1,1,ItemsTypes.HELICOPTER);yield return new WaitForSeconds(1.4f);
                var destination=wanted.transform.position;
                Check(RoyalPowerupDirector.Tap(origin),"Single Propeller tap accepted");yield return Settled();
                Check(contacts.Count==1&&Vector2.Distance(contacts[0],destination)<.01f,"Single Propeller reaches the remaining colour objective exactly once");
                Check(LM.targetObject[0].countCollected>=1,"Contact updates its actual objective");

                foreach(var payload in new[]{ItemsTypes.PACKAGE,ItemsTypes.HORIZONTAL_STRIPPED,ItemsTypes.VERTICAL_STRIPPED})
                {
                    yield return Fresh();
                    var a=Make(3,3,ItemsTypes.HELICOPTER);var b=Make(4,3,payload);
                    yield return new WaitForSeconds(1.4f);
                    Check(RoyalPowerupDirector.TrySwap(a,b),"Live Propeller + "+payload+" swap accepted");LM.FindMatches();
                    yield return Settled();
                    Check(carriedPeak==1&&contacts.Count>=1,"Visible paired payload flies and reaches target: "+payload);
                }

                foreach(var payload in new[]{ItemsTypes.PACKAGE,ItemsTypes.HORIZONTAL_STRIPPED})
                {
                    yield return Fresh();
                    var a=Make(3,3,ItemsTypes.HELICOPTER);var b=Make(4,3,payload);
                    foreach(var cell in LM.squaresArray)if(cell&&cell.item!=a&&cell.item!=b)
                    {if(cell.item)UnityEngine.Object.Destroy(cell.item.gameObject);cell.item=null;cell.type=SquareTypes.NONE;}
                    yield return new WaitForSeconds(1.4f);
                    int before=RoyalPowerupDirector.Get.PropellerPayloadHandoffs;
                    Check(RoyalPowerupDirector.TrySwap(a,b),"No-target pair accepted: "+payload);
                    float timeout=Time.realtimeSinceStartup+12;
                    while(RoyalPowerupDirector.Busy&&Time.realtimeSinceStartup<timeout)yield return null;
                    Check(!RoyalPowerupDirector.Busy&&RoyalPowerupDirector.Get.PropellerPayloadHandoffs==before+1,"No-target payload hands off exactly once: "+payload);
                    Check(contacts.Count==1&&RoyalPowerupDirector.Get.PropellerReservedTargets==0,"No-target fallback completes one contact and releases reservation");
                }

                yield return Fresh();
                var active=new List<Item>();
                for(int n=0;n<5;n++){LM.GetSquare(n+1,LM.maxRows-1).item.SetColor(4);active.Add(Make(n+1,0,ItemsTypes.HELICOPTER));}
                yield return new WaitForSeconds(1.4f);
                foreach(var item in active)Check(RoyalPowerupDirector.Tap(item),"Concurrent Propeller accepted");
                yield return Settled();
                Check(contacts.Count==5,"Five Propellers produce five destination contacts");
                Check(contacts.Select(p=>new Vector2(p.x,p.y)).Distinct().Count()==5,"Concurrent destination reservations stay distinct");

                yield return Fresh();
                var cancelled=Make(3,3,ItemsTypes.HELICOPTER);var cancelledPayload=Make(4,3,ItemsTypes.PACKAGE);
                yield return new WaitForSeconds(1.4f);
                Check(RoyalPowerupDirector.TrySwap(cancelled,cancelledPayload),"Repeated pooled pair accepted");
                yield return new WaitForSeconds(.2f);RoyalPowerupDirector.Get.Cancel();
                Check(PropellerVisualV5.ActiveCarriedBoosters==0&&RoyalPowerupDirector.Get.PropellerReservedTargets==0,"Cancellation releases pair visuals and reservations");
                Check(ReferenceVfxPool.Active==0&&ReferenceActivationVisual.PlayingVoices==0,"Cancellation clears native VFX and audio");
                Check(!errors,"No new error/exception logs");success=true;
                if(own)yield return qa.EndSession();
            }
            finally
            {
                RoyalPowerupDirector.Get?.Cancel();
                PropellerVisualV5.Event-=Event;Application.logMessageReceived-=Log;
                if(own&&RoyalSave.Testing)qa.StartCoroutine(qa.EndSession());
                lines.Insert(0,(success?"PASS":"INCOMPLETE")+" "+DateTime.UtcNow.ToString("o"));
                File.WriteAllLines(Output,lines);running=false;
            }
        }
    }
}
#endif
