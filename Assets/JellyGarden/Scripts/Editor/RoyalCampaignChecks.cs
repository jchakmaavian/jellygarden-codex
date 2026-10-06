#if UNITY_EDITOR && !ROYAL_DISABLE_QA
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JellyGarden.Scripts;
using JellyGarden.Scripts.GUI;
using JellyGarden.Scripts.Juice;
using JellyGarden.Scripts.MapScripts;
using RoyalThrone.Campaign;
using RoyalThrone.Testing;
using UnityEditor;
using UnityEngine;

namespace RoyalThrone.Editor
{
    public static class RoyalCampaignChecks
    {
        const string Folder="Design/Inspection-Levels01-50/Validation/";
        static readonly List<string> lines=new List<string>();
        static bool running;
        static bool allRunning;
        public static string Status {get;private set;}="Not started";
        static void SetStatus(string value){Status=value;File.WriteAllText(Folder+"status.txt",value+"\n"+DateTime.UtcNow.ToString("u"));}
        static void Check(bool condition,string message)
        {
            if(!condition)throw new InvalidOperationException(message);
            lines.Add("PASS "+message);File.WriteAllLines(Folder+"campaign-checks.txt",lines);
        }
        [MenuItem("Tools/Royal Throne/Campaign/Verify data for all 50 levels")]
        public static void Data()
        {
            if(running||allRunning)throw new InvalidOperationException("A campaign check is already running.");
            Directory.CreateDirectory(Folder);lines.Clear();
            var levels=RoyalReferenceCampaign.Levels;
            Check(levels.Length==50&&levels.Select(l=>l.number).SequenceEqual(Enumerable.Range(1,50)),"Exactly levels 01–50, no gaps or duplicates");
            foreach(var l in levels)
            {
                RoyalReferenceCampaign.Validate(l);
                Check(l.cells.Length==l.width*l.height&&l.moves>=20&&l.moves<=35,"Level "+l.number+": dimensions and authored move budget");
                Check(l.colors.All(c=>c>=1&&c<=6)&&l.colors.Distinct().Count()==l.colors.Length,"Level "+l.number+": valid distinct color palette");
                for(int i=0;i<l.cells.Length;i++)
                {
                    int c=l.cells[i].code;
                    if(c!=41&&c!=43)continue;
                    Check(i%l.width+1<l.width&&i+l.width+1<l.cells.Length&&l.cells[i+1].code==c+1&&l.cells[i+l.width].code==c+10&&l.cells[i+l.width+1].code==c+11,
                        "Level "+l.number+": 2x2 footprint at "+i);
                }
                foreach(var goal in l.goals)
                {
                    if(goal.fromSettings||goal.name=="Coin")continue;
                    int expected=goal.name=="Box"?l.cells.Count(c=>c.code>=21&&c.code<=23):goal.name=="Grass"?l.cells.Count(c=>c.grass>0):
                        goal.name=="Cupboard"?10*l.cells.Count(c=>c.code==41):goal.name=="Potion"?4*l.cells.Count(c=>c.code==43):l.cells.Count(c=>c.code==26);
                    Check(goal.count==expected,"Level "+l.number+": "+goal.name+" goal matches authored layout");
                }
            }
            File.WriteAllLines(Folder+"data-checks.txt",lines);Debug.Log("[Royal Campaign] Data PASS: "+lines.Count+" checks.");
        }
        [MenuItem("Tools/Royal Throne/Campaign/Open-check levels 01–10")]
        public static void Batch01()=>Run(1,10);
        [MenuItem("Tools/Royal Throne/Campaign/Open-check levels 11–20")]
        public static void Batch11()=>Run(11,20);
        [MenuItem("Tools/Royal Throne/Campaign/Open-check levels 21–30")]
        public static void Batch21()=>Run(21,30);
        [MenuItem("Tools/Royal Throne/Campaign/Open-check levels 31–40")]
        public static void Batch31()=>Run(31,40);
        [MenuItem("Tools/Royal Throne/Campaign/Open-check levels 41–50")]
        public static void Batch41()=>Run(41,50);
        [MenuItem("Tools/Royal Throne/Campaign/Run all 50 checks %&#F6")]
        public static void RunAll()
        {
            RequireReady();Data();File.WriteAllText(Folder+"all-batches.txt","");allRunning=true;RoyalRegressionRunner.Run(AllBatches());
        }
        static void RequireReady()
        {
            if(running||allRunning)throw new InvalidOperationException("Campaign checks are already running.");
            if(!EditorApplication.isPlaying||!RoyalQaService.Instance||!RoyalQaService.Instance.Ready)throw new InvalidOperationException("Enter the game scene in Play Mode first.");
            if(RoyalSave.Testing)throw new InvalidOperationException("End the existing QA session before running the isolated batch.");
        }
        static IEnumerator AllBatches()
        {
            int completed=0;
            try
            {
                for(int first=1;first<=41;first+=10)
                {
                    lines.Clear();running=true;SetStatus("RUNNING batch "+first.ToString("00")+"–"+(first+9));
                    yield return Suite(first,first+9);completed++;
                    File.AppendAllText(Folder+"all-batches.txt","PASS "+first.ToString("00")+"–"+(first+9)+"\n");
                    yield return null;
                }
            }
            finally{running=false;allRunning=false;SetStatus(completed==5?"ALL FIVE BATCHES PASSED":"STOPPED after "+completed+" batches; inspect the failed batch and Console");}
        }
        public static void Run(int first,int last)
        {
            RequireReady();
            if(first<1||last>50||first>last)throw new ArgumentOutOfRangeException();
            Directory.CreateDirectory(Folder);lines.Clear();running=true;RoyalRegressionRunner.Run(Suite(first,last));
        }
        // Focused replay of the observed Level 40 completion failure, without changing
        // its production celebration, target selection, simulation speed or time bound.
        public static void RunLevel40Outcome()
        {
            RequireReady();Directory.CreateDirectory(Folder);lines.Clear();running=true;
            RoyalRegressionRunner.Run(Level40Outcome());
        }
        static IEnumerator Level40Outcome()
        {
            var normal=Snapshot();var qa=RoyalQaService.Instance;bool passed=false;
            try
            {
                qa.StartSession();RoyalReferenceCampaign.Select(true);SetStatus("RUNNING Level 40 outcome diagnostic");
                Check(RoyalSave.Testing&&RoyalReferenceCampaign.Enabled,"Separate reference QA profile selected");
                yield return Outcomes(qa,40,true);
                var after=Snapshot();Check(normal.All(p=>after[p.Key]==p.Value),"Normal player save unchanged across Level 40 outcome");
                yield return qa.EndSession();Check(!RoyalSave.Testing&&!RoyalReferenceCampaign.Enabled,"Player profile restored");passed=true;
            }
            finally
            {
                if(RoyalSave.Testing)qa.StartCoroutine(qa.EndSession());running=false;
                lines.Add(passed?"OUTCOME PASSED":"OUTCOME FAILED - inspect win diagnostics; profile cleanup requested.");
                File.WriteAllLines(Folder+"level-40-outcome.txt",lines);SetStatus("LEVEL 40 OUTCOME "+(passed?"PASSED":"FAILED"));
            }
        }
        static Dictionary<string,string> Snapshot()
        {
            var keys=new List<string>{"Gems","Lifes","OpenLevel","OpenLevelTest","RestLifeTimer","DateOfExit",InitScript.NextLifeKey};
            keys.AddRange(new[]{"Music","Sound","RoyalThrone.ShowHints","RoyalThrone.HomeMotion"});
            keys.AddRange(Enum.GetNames(typeof(BoostType)));
            for(int i=1;i<=100;i++){keys.Add("Level."+i.ToString("000")+".StarsCount");keys.Add("Score"+i);}
            return keys.ToDictionary(k=>k,k=>PlayerPrefs.HasKey(k)+"/"+PlayerPrefs.GetInt(k)+"/"+PlayerPrefs.GetFloat(k)+"/"+PlayerPrefs.GetString(k));
        }
        static IEnumerator Suite(int first,int last)
        {
            var normal=Snapshot();var qa=RoyalQaService.Instance;bool passed=false;
            try
            {
                qa.StartSession();RoyalReferenceCampaign.Select(true);
                Check(RoyalSave.Testing&&RoyalReferenceCampaign.Enabled,"Separate reference QA profile selected");
                for(int n=first;n<=last;n++)
                {
                    SetStatus("RUNNING level "+n.ToString("00")+" (batch "+first.ToString("00")+"–"+last+")");
                    qa.Level=n;yield return qa.OpenLevel();qa.Visible=false;
                    var lm=LevelManager.THIS;var l=RoyalReferenceCampaign.Current;
                    Check(lm.gameStatus==GameState.Playing&&lm.currentLevel==n,"Level "+n+": opens and settles");
                    Check(lm.maxCols==l.width&&lm.maxRows==l.height&&lm.Limit==l.moves,"Level "+n+": dimensions and moves reach runtime");
                    Check(lm.squaresArray.Count(s=>s.IsNone())==l.cells.Count(c=>c.code==0),"Level "+n+": exact board holes");
                    Check(lm.targetObject.Length==l.goals.Length,"Level "+n+": all goals reach runtime");
                    for(int i=0;i<l.cells.Length;i++)
                    {
                        var cell=l.cells[i];var sq=lm.squaresArray[i];
                        if(cell.code>=1&&cell.code<=6)Check(sq.item&&sq.item.color==RoyalReferenceCampaign.ColorIndex(cell.code),"Level "+n+": authored gem "+i);
                        if(cell.code>=21&&cell.code<=23)Check(RoyalCampaignBlockers.HealthAt(sq)==cell.code-20,"Level "+n+": box health "+i);
                        if(cell.grass>0)Check(RoyalCampaignBlockers.GrassAt(sq)==cell.grass-30,"Level "+n+": grass layers "+i);
                        if(cell.code==41||cell.code==43)
                            Check(RoyalCampaignBlockers.HealthAt(sq)==(cell.code==41?10:4)&&RoyalCampaignBlockers.SharesObstacle(sq,lm.squaresArray[i+l.width+1]),"Level "+n+": shared 2x2 obstacle "+i);
                    }
                    Check(lm.GetItems().Where(i=>i).All(i=>i.square&&i.square.item==i&&!i.falling&&!i.destroying),"Level "+n+": settled item ownership");
                    yield return RealMove(qa,lm,n);
                    RoyalCampaignBlockerChecks.VerifyBehavior(lm,Check);
                    yield return null;
                    if(n==20||n==40)yield return Outcomes(qa,n,true);
                }
                yield return Outcomes(qa,first,false);
                var after=Snapshot();Check(normal.All(p=>after[p.Key]==p.Value),"Normal player save unchanged across campaign batch");
                yield return qa.EndSession();Check(!RoyalSave.Testing&&!RoyalReferenceCampaign.Enabled,"Player profile restored");passed=true;
            }
            finally
            {
                if(RoyalSave.Testing)qa.StartCoroutine(qa.EndSession());running=false;
                lines.Add(passed?"BATCH PASSED":"BATCH FAILED - inspect Console; profile cleanup requested.");
                File.WriteAllLines(Folder+"batch-"+first.ToString("00")+"-"+last.ToString("00")+".txt",lines);
                SetStatus("BATCH "+first.ToString("00")+"–"+last+" "+(passed?"PASSED":"FAILED"));
                Debug.Log("[Royal Campaign] Batch "+first+"–"+last+": "+(passed?"PASS":"FAIL"));
            }
        }
        static IEnumerator RealMove(RoyalQaService qa,LevelManager lm,int n)
        {
            var plan=RoyalHintSolver.Find(lm);
            if(plan==null){lm.NoMatches();yield return qa.Settled();plan=RoyalHintSolver.Find(lm);}
            Check(plan!=null,"Level "+n+": has a legal swap or existing power action");
            int moves=lm.Limit,score=LevelManager.Score;
            if(plan.IsTap)Check(RoyalPowerupDirector.Tap(plan.First),"Level "+n+": native power activation accepted");
            else plan.First.SwitchDirection(plan.First.transform.position-plan.Second.transform.position);
            yield return qa.Settled();
            Check(lm.Limit==moves-1,"Level "+n+": real action spends exactly one move");
            Check(LevelManager.Score>score,"Level "+n+": real action scores through match/blast resolution");
            Check(lm.GetItems().Where(i=>i).All(i=>i.square&&i.square.item==i&&!i.falling&&!i.destroying),"Level "+n+": real clear, gravity and refill settle with valid ownership");
        }
        static IEnumerator Outcomes(RoyalQaService qa,int n,bool bonus)
        {
            qa.Level=n;yield return qa.OpenLevel();qa.Visible=false;var lm=LevelManager.THIS;
            if(!bonus)
            {
                lm.Limit=0;lm.CheckWinLose();
                Check(lm.gameStatus==GameState.PreFailed,"Level "+n+": unfinished goals at zero moves enter failure offer");
                var offer=UnityEngine.Object.FindObjectsByType<AnimationManager>(FindObjectsInactive.Include).FirstOrDefault(a=>a.name=="PreFailed");
                Check(offer,"Level "+n+": actual failure offer exists");
                float failEnd=Time.realtimeSinceStartup+10;
                while(lm.gameStatus!=GameState.GameOver&&Time.realtimeSinceStartup<failEnd){offer.GiveUp();yield return null;}
            }
            else
            {
                lm.CheckWinLose();Check(lm.gameStatus==GameState.Playing,"Bonus "+n+": zero authored Coin goal does not cause an instant win");
            }
            if(!bonus)
            {
                Check(lm.gameStatus==GameState.GameOver,"Level "+n+": failure dialog/state is reachable");
                yield return qa.OpenLevel();qa.Visible=false;
                Check(lm.Limit==RoyalReferenceCampaign.Current.moves&&lm.targetObject.All(t=>t.countCollected==0),"Level "+n+": retry restores original moves and fresh targets");
                yield return RealMove(qa,lm,n);
                foreach(var goal in lm.targetObject)RoyalReferenceCampaign.CollectGoal(goal.referenceGoal,goal.targetCount);
            }
            int lives=InitScript.lifes,starsBefore=RoyalSave.GetInt("Level."+n.ToString("000")+".StarsCount");
            lm.Limit=0;lm.CheckWinLose();
            Check(lm.gameStatus==GameState.PreWinAnimations,"Level "+n+": prepared final-move condition enters production win celebration");
            float realStart=Time.realtimeSinceStartup,gameStart=Time.time;int frameStart=Time.frameCount;
            float end=realStart+45,nextSnapshot=realStart+5;
            if(n==40)
            {
                File.WriteAllText(Folder+"level-40-win-diagnostics.tsv","utc\trealSeconds\tsimulationSeconds\tframes\tstate\tdragBlocked\tresolving\toperations\tcolorBallPhase\tselected\tarrivals\tconversions\tactiveBeams\tnativeLinks\tactiveBalls\tboardBoosters\tboardItems\tfalling\tdestroying\tnativeVfx\n");
                WinSnapshot(lm,realStart,gameStart,frameStart);
            }
            while(lm.gameStatus!=GameState.Win&&Time.realtimeSinceStartup<end)
            {
                lm.RequestSkipWinCelebration();
                if(n==40&&Time.realtimeSinceStartup>=nextSnapshot){WinSnapshot(lm,realStart,gameStart,frameStart);nextSnapshot+=5;}
                yield return null;
            }
            if(n==40)WinSnapshot(lm,realStart,gameStart,frameStart);
            Check(lm.gameStatus==GameState.Win,"Level "+n+": production celebration reaches win screen");
            int stars=RoyalSave.GetInt("Level."+n.ToString("000")+".StarsCount");
            Check(stars>=1&&stars>=starsBefore&&RoyalSave.GetInt("Score"+n)>0,"Level "+n+": completion and score save to reference profile");
            if(n<50)Check(!LevelsMap.IsLevelLocked(n+1),"Level "+n+": saved completion unlocks next level");
            int expected=InitScript.Instance.losingLifeEveryGame?lives:Mathf.Min(InitScript.Instance.CapOfLife,lives+1);
            Check(InitScript.lifes==expected,"Level "+n+": native win life reward applies once");
            yield return null;Check(InitScript.lifes==expected,"Level "+n+": idle win frame does not repeat reward");
            yield return qa.OpenLevel();
            Check(RoyalSave.GetInt("Level."+n.ToString("000")+".StarsCount")==stars,"Level "+n+": completion survives a fresh board load");
        }
        static void WinSnapshot(LevelManager lm,float realStart,float gameStart,int frameStart)
        {
            var director=RoyalPowerupDirector.Get;var items=lm.GetItems().Where(i=>i).ToArray();
            var culture=System.Globalization.CultureInfo.InvariantCulture;
            File.AppendAllText(Folder+"level-40-win-diagnostics.tsv",string.Join("\t",new object[]{
                DateTime.UtcNow.ToString("o"),(Time.realtimeSinceStartup-realStart).ToString("F3",culture),(Time.time-gameStart).ToString("F3",culture),Time.frameCount-frameStart,
                lm.gameStatus,lm.DragBlocked,lm.IsResolvingMatches,director?director.ActiveOperations:0,director?director.ColorBallState.ToString():"none",
                director?director.ColorBallSelectedTargets:0,director?director.ColorBallBeamArrivals:0,director?director.ColorBallConversions:0,
                RoyalThrone.Boosters.ColorBall.V4.ColorBallVfxV4.ActiveBeams,RoyalThrone.Boosters.ColorBall.V4.ColorBallVfxV4.NativeLinks,
                RoyalThrone.Boosters.ColorBall.V4.ColorBallVisualV4.Active,items.Count(i=>RoyalPowerupDirector.IsPower(i.currentType)),items.Length,
                items.Count(i=>i.falling),items.Count(i=>i.destroying),RoyalThrone.ReferenceGameplay.ReferenceVfxPool.Active})+"\n");
        }
    }
}
#endif
