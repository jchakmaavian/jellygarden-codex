#if UNITY_EDITOR && !ROYAL_DISABLE_QA
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JellyGarden.Scripts;
using JellyGarden.Scripts.GUI;
using JellyGarden.Scripts.Juice;
using RoyalThrone.Testing;
using UnityEditor;
using UnityEngine;

namespace RoyalThrone.Editor
{
    public static class RoyalQaChecks
    {
        const string Folder="Logs/port-checks/QA-Mode/PlayMode/";
        static readonly List<string> lines=new List<string>();static readonly List<string> errors=new List<string>();
        static RoyalQaService qa;static bool running;
        [MenuItem("Tools/Royal Throne/QA/Verify every testing control %#F6")]
        public static void Run()
        {
            if(running)throw new InvalidOperationException("QA verification already running.");
            qa=RoyalQaService.Instance;if(!EditorApplication.isPlaying||!qa||!qa.Ready)throw new InvalidOperationException("Enter the game scene in Play Mode first.");
            if(RoyalSave.Testing)throw new InvalidOperationException("End the manual test profile before the automated isolation audit.");
            running=true;lines.Clear();errors.Clear();Directory.CreateDirectory(Folder);RoyalRegressionRunner.Run(Suite());
        }
        static void Check(bool condition,string text){if(!condition)throw new Exception(text);lines.Add("PASS "+text);File.WriteAllLines(Folder+"checks.txt",lines);}
        static void Log(string message,string stack,LogType type){if(type==LogType.Exception||type==LogType.Error||type==LogType.Assert)errors.Add(message);}
        static Dictionary<string,string> Snapshot()
        {
            var keys=new HashSet<string>{"Gems","Lifes","RestLifeTimer","DateOfExit","OpenLevel","OpenLevelTest","Music","Sound","RoyalThrone.ShowHints","RoyalThrone.HomeMotion","Lauched"};
            foreach(var level in Resources.LoadAll<TextAsset>("Levels"))if(int.TryParse(level.name,out int number)){keys.Add(string.Format("Level.{0:000}.StarsCount",number));keys.Add("Score"+number);}
            foreach(BoostType boost in Enum.GetValues(typeof(BoostType)))keys.Add(boost.ToString());
            return keys.ToDictionary(k=>k,k=>UnityEngine.PlayerPrefs.HasKey(k)+"/"+UnityEngine.PlayerPrefs.GetInt(k)+"/"+UnityEngine.PlayerPrefs.GetFloat(k)+"/"+UnityEngine.PlayerPrefs.GetString(k));
        }
        static IEnumerator Command(string name)
        {
            int complete=qa.Completed,failed=qa.Failed;qa.Request(name);float until=Time.realtimeSinceStartup+45;
            while(qa.Busy&&Time.realtimeSinceStartup<until)yield return null;
            Check(!qa.Busy&&qa.Failed==failed&&qa.Completed==complete+1,"Control executes exactly once: "+name+" ("+qa.Status+")");
            Check(errors.Count==0,"No runtime exception after "+name);
        }
        static IEnumerator Suite()
        {
            var normal=Snapshot();int money=InitScript.Gems,lives=InitScript.lifes;bool success=false;
            Application.logMessageReceived+=Log;
            try
            {
                // Reproduce an external harness enabling routing without capturing player state.
                var routing=typeof(RoyalSave).GetProperty("Testing");
                routing.SetValue(null,true);
                try
                {
                    bool startRejected=false,endRejected=false;
                    try{qa.StartSession();}catch(InvalidOperationException){startRejected=true;}
                    try{qa.EndSession().MoveNext();}catch(InvalidOperationException){endRejected=true;}
                    Check(startRejected&&endRejected&&RoyalSave.Testing,"Unowned test profile cannot switch routing back to the player save");
                    var untouched=Snapshot();Check(normal.All(k=>untouched[k.Key]==k.Value),"Rejected unowned teardown leaves every player key unchanged");
                }
                finally {routing.SetValue(null,false);}
                qa.StartSession();Check(RoyalSave.Testing,"QA session enters isolated save routing");qa.Level=1;qa.Amount=10;qa.Speed=1;qa.Loop=false;
                // King Leo has no QA menu: the service is installed hidden and must not hold the board's input.
                Check(!qa.Visible&&!RoyalSave.BlockGameplayInput,"QA session leaves board input to the game");
                // Save semantics: missing/delete tombstones must not fall back to the player's value.
                int raw=UnityEngine.PlayerPrefs.GetInt("Gems");RoyalSave.SetInt("Gems",4321);Check(RoyalSave.GetInt("Gems")==4321&&UnityEngine.PlayerPrefs.GetInt("Gems")==raw,"Writes land only in QA keys");
                RoyalSave.DeleteKey("Gems");Check(!RoyalSave.HasKey("Gems")&&RoyalSave.GetInt("Gems",-7)==-7,"Deleted QA key does not expose the player value");RoyalSave.SetInt("Gems",money);
                foreach(string command in new[]{"Set gems","Add gems","Remove gems","Set coins","Add coins","Remove coins","Refill coins"})
                {int old=InitScript.Gems;yield return Command(command);int expected=command=="Refill coins"?10000:command.StartsWith("Set")?10:command.StartsWith("Add")?old+10:Mathf.Max(0,old-10);Check(InitScript.Gems==expected&&RoyalSave.GetInt("Gems")==expected,"Currency runtime/save/UI source agrees for "+command);}
                qa.Amount=2;yield return Command("Set stars");Check(RoyalSave.GetInt("Level.001.StarsCount")==2,"Selected-level stars set to 2");qa.Amount=1;
                yield return Command("Add stars");Check(RoyalSave.GetInt("Level.001.StarsCount")==3,"Stars add clamps to three");yield return Command("Remove stars");Check(RoyalSave.GetInt("Level.001.StarsCount")==2,"Stars remove updates the same level");
                qa.Amount=2;yield return Command("Set lives");Check(InitScript.lifes==2&&RoyalSave.GetInt("Lifes")==2,"Lives runtime and save agree");
                qa.StartSession();Check(RoyalSave.Testing&&InitScript.lifes==2,"Repeated StartSession keeps the active test and original player snapshot");
                yield return Command("Empty lives");Check(InitScript.lifes==0,"Empty lives reaches zero");yield return Command("Refill lives");Check(InitScript.lifes==InitScript.Instance.CapOfLife,"Refill reaches the authored cap");
                yield return Command("Test refill timer");int before=InitScript.lifes;float realStart=Time.realtimeSinceStartup;
                // The life schedule runs on the real (UTC) clock. A scaled WaitForSeconds counts from the start of a frame that
                // can be long here (every check rewrites the report), so it ended before the 3 s deadline: wait on real time.
                while(InitScript.lifes==before&&Time.realtimeSinceStartup-realStart<6f)yield return null;
                float arrived=Time.realtimeSinceStartup-realStart;yield return new WaitForSecondsRealtime(.5f);
                Check(InitScript.lifes==before+1&&arrived>=2.8f&&arrived<=4f,"Real life counter awards exactly one life at timer expiry (lives "+before+" -> "+InitScript.lifes+" after "+arrived.ToString("0.00")+" s)");
                // Full lives have no next life to time (the UTC schedule clears the timer), so reset from a short timer below the cap.
                qa.Amount=InitScript.Instance.CapOfLife-1;yield return Command("Set lives");yield return Command("Test refill timer");
                yield return Command("Reset refill timer");Check(InitScript.RestLifeTimer>3,"Reset uses the authored refill duration");
                qa.Amount=8;yield return Command("Set booster stock");foreach(BoostType boost in Enum.GetValues(typeof(BoostType)))if(boost!=BoostType.None)Check(RoyalSave.GetInt(boost.ToString())==8,"Booster stock uses existing key: "+boost);
                yield return Command("Open level");yield return Command("Restart level");yield return Command("Practice board");
                Check(LevelManager.THIS.colorLimit==RoyalQaService.PracticeColors,"Practice board refills from the full palette");
                // No-move fixture with one piece still falling: the idle hint scan must
                // wait, not regenerate the board underneath an active resolution.
                foreach(var cell in LevelManager.THIS.squaresArray)cell.item.SetColor((cell.col+cell.row*2)%5);
                Check(RoyalHintSolver.Find(LevelManager.THIS)==null,"Shuffle regression fixture has no available move");
                var fallingPiece=LevelManager.THIS.GetSquare(0,0).item;fallingPiece.falling=true;
                AI.THIS.StartCoroutine(AI.THIS.CheckPossibleCombines());
                yield return new WaitForSeconds(1.5f);
                bool waited=LevelManager.THIS.gameStatus==GameState.Playing&&fallingPiece&&fallingPiece.square.item==fallingPiece;
                fallingPiece.falling=false;
                Check(waited,"Hint scan does not shuffle while a piece is still falling");
                float shuffleUntil=Time.realtimeSinceStartup+4f;
                while(LevelManager.THIS.gameStatus==GameState.Playing&&Time.realtimeSinceStartup<shuffleUntil)yield return null;
                Check(LevelManager.THIS.gameStatus==GameState.RegenLevel,"No-move shuffle resumes when the board becomes idle");
                yield return qa.Settled();yield return Command("Practice board");
                for(int color=0;color<RoyalQaService.PracticeColors;color++){qa.Gem=color;yield return Command("Spawn selected gem");Check(LevelManager.THIS.GetSquare(3,3).item.color==color,"Spawned selected gem "+color);}
                string[] spawns={"Spawn TNT","Spawn rocket H","Spawn rocket V","Spawn propeller","Spawn color ball"};
                ItemsTypes[] types={ItemsTypes.PACKAGE,ItemsTypes.HORIZONTAL_STRIPPED,ItemsTypes.VERTICAL_STRIPPED,ItemsTypes.HELICOPTER,ItemsTypes.BOMB};
                for(int n=0;n<spawns.Length;n++){yield return Command(spawns[n]);Check(LevelManager.THIS.GetSquare(3,3).item.currentType==types[n],"Native power type: "+types[n]);}
                yield return Command("Activate selected");
                for(int a=0;a<5;a++)for(int b=a;b<5;b++){yield return Command("Practice board");qa.PowerA=a;qa.PowerB=b;yield return Command("Combine selected");Check(LevelManager.THIS.GetItems().Where(i=>i).All(i=>i.square&&i.square.item==i&&!i.destroying&&!i.falling),"Settled ownership after combination "+a+" + "+b);}
                foreach(string command in new[]{"Match 3","Match 4","Match 5","Cascade","Create special"}){yield return Command(command);Check(LevelManager.Score>0,"Real match scoring runs for "+command);}
                foreach(string category in new[]{"VFX TEST LAB","ANIMATION TEST"})foreach(string command in qa.Commands[category]){yield return Command(command);}
                yield return Command("Star reward");int earned=RoyalSave.GetInt("Level.001.StarsCount");yield return Command("Star animation");Check(RoyalSave.GetInt("Level.001.StarsCount")==earned,"Replaying star animation does not duplicate the reward");
                int coins=InitScript.Gems;qa.Amount=10;yield return Command("Coin reward");Check(InitScript.Gems==coins+10&&RoyalSave.GetInt("Gems")==coins+10,"Coin reward credits exactly once");
                foreach(float speed in new[]{.25f,.5f,1,1.5f}){qa.Speed=speed;yield return null;Check(Mathf.Approximately(Time.timeScale,speed),"Game speed "+speed+" applies");yield return Command("Fire Impact");}qa.Speed=1;
                qa.LastReplay="Ice Impact";qa.Visible=false;qa.Loop=true;int completed=qa.Completed;float timeout=Time.realtimeSinceStartup+9;
                while(qa.Completed<completed+2&&Time.realtimeSinceStartup<timeout)yield return null;qa.Loop=false;while(qa.Busy)yield return null;Check(qa.Completed>=completed+2,"Loop replays and stops without overlapping operations");
                yield return Command("Practice board");qa.Amount=25;yield return Command("Set moves");Check(LevelManager.THIS.Limit==25,"Moves set");qa.Amount=3;yield return Command("Add moves");Check(LevelManager.THIS.Limit==28,"Moves add");yield return Command("Remove moves");Check(LevelManager.THIS.Limit==25,"Moves remove");
                yield return Command("Spawn blocker");Check(LevelManager.THIS.GetSquare(3,3).type==SquareTypes.WIREBLOCK&&!LevelManager.THIS.GetSquare(3,3).CanGoOut(),"Blocker prefab retains trap behavior");
                yield return Command("Practice board");yield return Command("Clear board");yield return Command("Shuffle board");
                foreach(string command in qa.Commands["DEBUG"].Where(c=>c!="Reset test data")){yield return Command(command);yield return Command(command);}
                yield return Command("Complete targets");Check(LevelManager.THIS.targetObject.All(t=>t.Done()),"All targets complete in runtime copies");
                int coinsBeforeWin=InitScript.Gems;yield return Command("Win instantly");Check(LevelManager.THIS.gameStatus==GameState.Win,"Instant win follows the real win state");int rewardLives=InitScript.lifes,rewardCoins=InitScript.Gems;
                Check(rewardCoins==coinsBeforeWin,"A win changes no coins (King Leo's free game, plan D4/D5)");
                int fail=qa.Failed;qa.Request("Win instantly");while(qa.Busy)yield return null;Check(qa.Failed==fail+1&&InitScript.lifes==rewardLives&&InitScript.Gems==rewardCoins,"Repeated win rejected without a duplicated life or coin reward");
                yield return Command("Restart level");yield return Command("Lose instantly");Check(LevelManager.THIS.gameStatus==GameState.GameOver,"Instant lose follows the real lose state");
                yield return Command("Reset test data");Check(InitScript.Gems==money,"Reset test data restores initial player-derived QA balance");
                var after=Snapshot();Check(normal.All(k=>after[k.Key]==k.Value),"Every normal economy, level, score, booster and preference key remains unchanged");
                var ending=qa.EndSession();Check(ending.MoveNext()&&RoyalSave.Testing,"Teardown keeps QA routing while retiring callbacks");
                Check(!qa.EndSession().MoveNext(),"Concurrent EndSession cannot restore twice");
                int completedAtEnd=qa.Completed;qa.Request("Empty lives");
                Check(!qa.Busy&&qa.Completed==completedAtEnd,"Teardown rejects new QA commands");
                yield return null;yield return ending;
                Check(!RoyalSave.Testing&&InitScript.Gems==money&&InitScript.lifes==lives,"Ending QA restores normal runtime resources");
                yield return qa.EndSession();
                var restored=Snapshot();Check(normal.All(k=>restored[k.Key]==k.Value),"Ending and repeated teardown preserve every player key including the life deadline");
                Check(Mathf.Approximately(Time.timeScale,1),"Ending QA restores game speed");success=true;
            }
            finally
            {
                qa.Loop=false;if(RoyalSave.Testing)qa.StartCoroutine(qa.EndSession());Application.logMessageReceived-=Log;running=false;
                lines.Add(success?"ALL QA CONTROLS PASSED":"FAILED — see the Console; QA profile is being restored.");lines.AddRange(errors);File.WriteAllLines(Folder+"checks.txt",lines);Debug.Log("[Royal QA Checks] "+(success?"PASS":"FAIL")+" "+lines.Count+" checks recorded.");
            }
        }
    }
}
#endif

