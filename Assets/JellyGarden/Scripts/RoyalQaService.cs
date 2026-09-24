#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !ROYAL_DISABLE_QA
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JellyGarden.Scripts;
using JellyGarden.Scripts.GUI;
using JellyGarden.Scripts.Juice;
using JellyGarden.Scripts.MapScripts;
using JellyGarden.Scripts.Targets;
using RoyalThrone.Presentation;
using UnityEngine;

namespace RoyalThrone.Testing
{
    // Commands prepare fixtures; all matching, gravity, power-ups and rewards are
    // executed by the game. Nothing in this class is present in a release player.
    public sealed class RoyalQaService : MonoBehaviour
    {
        public static RoyalQaService Instance { get; private set; }
        public static readonly string[] Categories = { "Gems", "Stars", "Lives", "Coins", "Boosters", "VFX TEST LAB", "ANIMATION TEST", "LEVEL TESTING", "DEBUG" };
        public readonly Dictionary<string,List<string>> Commands = new Dictionary<string,List<string>>();
        public int Amount=10, Level=1, Column=3, Row=3, Gem=0, PowerA=0, PowerB=1;
        public bool Busy { get; private set; }
        public bool Loop, Grid, Ids, ValidMoves, Matches, Candidate, Counters=true;
        public float Speed=1;
        public string Status="Start an isolated test session.", LastReplay="";
        public int Completed { get; private set; }
        public int Failed { get; private set; }
        public bool Visible=true;
        public event Action Changed;
        int normalMoney,normalLives,normalLevel; float normalTimer,normalMusic,normalSound;
        string normalDate; UnityEngine.Random.State normalRandom;
        bool practice; float replayAt;
        readonly List<JellyGarden.Scripts.GUI.TutorialStuff.TutorialManager> suspendedTutorials=new List<JellyGarden.Scripts.GUI.TutorialStuff.TutorialManager>();
        static readonly ItemsTypes[] Powers={ItemsTypes.PACKAGE,ItemsTypes.HORIZONTAL_STRIPPED,ItemsTypes.VERTICAL_STRIPPED,ItemsTypes.HELICOPTER,ItemsTypes.BOMB};
        public static readonly string[] PowerNames={"TNT","Rocket H","Rocket V","Propeller","Color ball"};
        LevelManager LM => LevelManager.THIS;
        public bool Ready => LM && InitScript.Instance && MenuManager.Instance;
        void Awake()
        {
            Instance=this;
            Add("Gems","Add gems","Remove gems","Set gems","Spawn selected gem","Match 3","Match 4","Match 5","Cascade","Create special");
            Add("Stars","Add stars","Remove stars","Set stars","Star reward","Star animation");
            Add("Lives","Set lives","Refill lives","Empty lives","Test refill timer","Reset refill timer");
            Add("Coins","Add coins","Remove coins","Set coins","Refill coins","Coin reward");
            Add("Boosters","Spawn TNT","Spawn rocket H","Spawn rocket V","Spawn propeller","Spawn color ball","Activate selected","Combine selected","Set booster stock");
            Add("VFX TEST LAB","VFX Match 3","VFX Match 4","VFX Match 5","Ice Impact","Fire Impact","TNT explosion","Break particles","Falling fragments","Rocket trails","Propeller effect","Color-ball effect","Hint aura","Win effect","Reward effect");
            Add("ANIMATION TEST","Gem idle","Swap","Match animation","Hint animation","Invalid swap","Fall","Cascade animation","Booster creation","Booster activation","TNT animation","Character / reward");
            Add("LEVEL TESTING","Open level","Restart level","Practice board","Win instantly","Lose instantly","Set moves","Add moves","Remove moves","Complete targets","Spawn blocker","Clear board","Shuffle board");
            Commands["LEVEL TESTING"].InsertRange(0,new[]{"Reference campaign 01–50","Original levels"});
            Add("DEBUG","Grid / cells","Gem IDs / types","Valid moves","Detected matches","Hint candidate","FPS / particles","Reset test data");
        }
        void Add(string category,params string[] labels)=>Commands[category]=new List<string>(labels);
        public void StartSession()
        {
            if(!Ready||RoyalSave.Testing||Busy)return;
#if PLAYFAB || GAMESPARKS || FACEBOOK || UNITY_INAPPS || UNITY_ADS || GOOGLE_MOBILE_ADS
            Status="QA requires the project's offline build configuration (network, purchases and ads disabled).";Changed?.Invoke();return;
#endif
            normalMoney=InitScript.Gems;normalLives=InitScript.lifes;normalTimer=InitScript.RestLifeTimer;
            normalDate=InitScript.DateOfExit;normalLevel=LM.currentLevel;normalRandom=UnityEngine.Random.state;
            normalMusic=MusicBase.Instance.GetVolume();normalSound=SoundBase.Instance.GetVolume();
            // Switch before leaving a board: any transition callback also writes QA keys.
            RoyalSave.Testing=true;RoyalSave.SetInt("OpenLevelTest",0);
            ToMap();Hydrate();Level=Mathf.Max(1,normalLevel);Visible=true;
            Status="TEST PROFILE ACTIVE. Coins and Gems share one balance. Stars are per selected level (0–3).";Changed?.Invoke();
        }
        void Hydrate()
        {
            InitScript.Gems=RoyalSave.GetInt("Gems",normalMoney);InitScript.lifes=RoyalSave.GetInt("Lifes",normalLives);
            SetTimer(RoyalSave.GetFloat("RestLifeTimer",normalTimer));Refresh();
        }
        public IEnumerator EndSession()
        {
            if(!RoyalSave.Testing)yield break;
            Loop=false;Status="Finishing the current test before restoring the player profile.";
            while(Busy)yield return null;
            ToMap();yield return null; // retire old UI animation callbacks before switching profile
            RoyalSave.Save();RoyalSave.Testing=false;
            RoyalThrone.Campaign.RoyalReferenceCampaign.Select(false);
            InitScript.Gems=normalMoney;InitScript.lifes=normalLives;InitScript.DateOfExit=normalDate;
            // Restore the remaining seconds captured at StartSession, not a stale exit stamp.
            InitScript.Instance.SetRefillSeconds(normalTimer);
            LM.currentLevel=normalLevel;UnityEngine.Random.state=normalRandom;Speed=1;Time.timeScale=1;practice=false;
            MusicBase.Instance.SetVolume(normalMusic);SoundBase.Instance.SetVolume(normalSound);
            Refresh();Status="Test session ended. Normal player progress restored.";Visible=false;Changed?.Invoke();
        }
        void ToMap()
        {
            if(!LM)return;Loop=false;LM.StopAllCoroutines();if(AI.THIS){AI.THIS.StopAllCoroutines();AI.THIS.GetComponent<RoyalHintPresenter>()?.Hide();}
            RetireTutorial();
            foreach(var tutorial in suspendedTutorials)if(tutorial)tutorial.enabled=true;
            suspendedTutorials.Clear();
            RoyalPowerupDirector.Get.Cancel();RoyalToolkitImpact.Existing?.Clear();MatchVFXService.Instance.Clear();CandyShatterService.Instance.Clear();
            foreach(var animation in MenuManager.Instance.GetComponentsInChildren<AnimationManager>(true))animation.StopAllCoroutines();
            MenuManager.Instance.HideAllCanvasElements();Time.timeScale=1;LM.gameStatus=GameState.Map;LM.DragBlocked=false;practice=false;
        }
        static void RetireTutorial()
        {
            // The authored tutorial holds live Item references. Retire its hand
            // before replacing a board; normal level loading can bind it again.
            foreach(var tutorial in FindObjectsByType<JellyGarden.Scripts.GUI.TutorialStuff.TutorialManager>(FindObjectsInactive.Include))
            {
                tutorial.StopAllCoroutines();
                if(tutorial.tutorial)tutorial.tutorial.SetActive(false);
                tutorial.items.Clear();
            }
            foreach(var hand in FindObjectsByType<JellyGarden.Scripts.GUI.TutorialStuff.HandTutorial>(FindObjectsInactive.Include))hand.StopAllCoroutines();
        }
        void Refresh()
        {
            foreach(var home in FindObjectsByType<RoyalHomeScreen>(FindObjectsInactive.Include))home.RefreshProgress();
        }
        void SetTimer(float seconds)
        {
            InitScript.DateOfExit=DateTime.UtcNow.ToString("o",System.Globalization.CultureInfo.InvariantCulture);
            InitScript.Instance.SetRefillSeconds(seconds);
            RoyalSave.SetString("DateOfExit",InitScript.DateOfExit);RoyalSave.Save();
            foreach(var timer in FindObjectsByType<LIFESAddCounter>(FindObjectsInactive.Include)){bool on=timer.enabled;timer.enabled=false;timer.enabled=on;}
        }
        float FullTimer=>InitScript.Instance.TotalTimeForRestLifeHours*3600+InitScript.Instance.TotalTimeForRestLifeMin*60+InitScript.Instance.TotalTimeForRestLifeSec;
        public void Request(string command)
        {
            if(Busy){Status="Wait for the current test to finish.";return;}
            StartCoroutine(RunSafely(command));
        }
        IEnumerator RunSafely(string command)
        {
            Busy=true;var stack=new Stack<IEnumerator>();stack.Push(Execute(command));
            try
            {
                while(stack.Count>0)
                {
                    var step=stack.Peek();bool next=false;Exception error=null;
                    try{next=step.MoveNext();}catch(Exception ex){error=ex;}
                    if(error!=null){Loop=false;Failed++;Status=command+": "+error.Message;Debug.LogWarning("[Royal QA] "+Status);yield break;}
                    if(!next){stack.Pop();(step as IDisposable)?.Dispose();continue;}
                    if(step.Current is IEnumerator child && !(child is CustomYieldInstruction))stack.Push(child);else yield return step.Current;
                }
                Completed++;Status="Completed: "+command;
            }
            finally{while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();Busy=false;replayAt=Time.unscaledTime+1.5f;Changed?.Invoke();}
        }
        public IEnumerator Execute(string command)
        {
            if(!RoyalSave.Testing||!Ready)throw new InvalidOperationException("Start the isolated test profile first.");
            if(!Commands.Values.Any(list=>list.Contains(command)))throw new InvalidOperationException("Unknown QA control.");
            Status="Running: "+command;Changed?.Invoke();
            if(command=="Reference campaign 01–50"||command=="Original levels")
            {
                ToMap();yield return null;RoyalThrone.Campaign.RoyalReferenceCampaign.Select(command=="Reference campaign 01–50");
                Level=1;Hydrate();Status=RoyalThrone.Campaign.RoyalReferenceCampaign.Enabled?"Reference campaign selected in its own QA save. Set level 1–50 and Open level. Bonus outcome, refill weighting and blocker behavior are inspection adaptations.":"Original levels selected in the existing QA profile.";
                Changed?.Invoke();yield break;
            }
            if(command=="Reset test data"){ToMap();yield return null;RoyalSave.ResetTesting();Hydrate();yield break;}
            if(command=="Grid / cells"){Grid=!Grid;yield break;}if(command=="Gem IDs / types"){Ids=!Ids;yield break;}
            if(command=="Valid moves"){ValidMoves=!ValidMoves;yield break;}if(command=="Detected matches"){Matches=!Matches;yield break;}
            if(command=="Hint candidate"){Candidate=!Candidate;yield break;}if(command=="FPS / particles"){Counters=!Counters;yield break;}
            if(command.EndsWith("gems")||command.EndsWith("coins"))
            {
                int value=command.StartsWith("Add")?InitScript.Gems+Mathf.Clamp(Amount,0,1000000):command.StartsWith("Remove")?InitScript.Gems-Mathf.Clamp(Amount,0,1000000):command=="Refill coins"?10000:Amount;
                InitScript.Instance.SetGems(Mathf.Clamp(value,0,1000000));Refresh();yield break;
            }
            if(command.EndsWith("stars"))
            {
                ValidateLevel();int old=RoyalSave.GetInt(StarKey);int value=command.StartsWith("Add")?old+Mathf.Clamp(Amount,0,3):command.StartsWith("Remove")?old-Mathf.Clamp(Amount,0,3):Amount;
                InitScript.Instance.SaveLevelStarsCount(Level,Mathf.Clamp(value,0,3));RoyalSave.Save();Refresh();yield break;
            }
            if(command.EndsWith("lives"))
            {
                int value=command=="Refill lives"?InitScript.Instance.CapOfLife:command=="Empty lives"?0:Mathf.Clamp(Amount,0,InitScript.Instance.CapOfLife);
                InitScript.Instance.AddLife(value-InitScript.lifes);SetTimer(value>=InitScript.Instance.CapOfLife?0:FullTimer);Refresh();yield break;
            }
            if(command=="Test refill timer"||command=="Reset refill timer")
            {
                if(command=="Test refill timer"&&InitScript.lifes>=InitScript.Instance.CapOfLife)InitScript.Instance.SpendLife(1);
                SetTimer(command=="Test refill timer"?3:FullTimer);yield break;
            }
            if(command=="Set booster stock"){foreach(BoostType type in Enum.GetValues(typeof(BoostType)))if(type!=BoostType.None)InitScript.Instance.BuyBoost(type,0,Mathf.Clamp(Amount,0,999));yield break;}
            if(command=="Open level"||command=="Restart level"){if(command=="Restart level")Level=LM.currentLevel;yield return OpenLevel();yield break;}
            if(command=="Practice board"){yield return Practice();yield break;}
            yield return Board();
            bool visual=Commands["VFX TEST LAB"].Contains(command)||Commands["ANIMATION TEST"].Contains(command)||command.EndsWith("animation")||command.StartsWith("Match ")||command=="Cascade"||command=="Create special"||command=="Star reward"||command=="Coin reward";
            if(visual){LastReplay=command;Visible=false;Changed?.Invoke();}
            if(command=="Set moves"||command=="Add moves"||command=="Remove moves")
            {LM.Limit=Mathf.Clamp(command=="Set moves"?Amount:LM.Limit+(command=="Add moves"?Mathf.Clamp(Amount,0,999):-Mathf.Clamp(Amount,0,999)),0,999);yield break;}
            if(command=="Complete targets"||command=="Win instantly")
            {
                if(LM.gameStatus!=GameState.Playing)throw new InvalidOperationException("Restart the level before another outcome.");
                foreach(var target in LM.targetObject)target.countCollected=target.targetCount;
                LevelManager.Score=Mathf.Max(LevelManager.Score,LM.star3);LM.stars=3;
                if(command=="Win instantly"){LM.gameStatus=GameState.Win;RoyalSave.Save();} // the real event/save flow, exactly once
                yield break;
            }
            if(command=="Lose instantly"){if(LM.gameStatus!=GameState.Playing)throw new InvalidOperationException("Restart before another outcome.");LM.Limit=0;LM.gameStatus=GameState.GameOver;yield break;}
            if(command=="Spawn blocker")
            {
                var square=Selected();if(square.block.Count>0)throw new InvalidOperationException("Selected cell already has a blocker.");
                var blocker=Instantiate(LM.wireBlockPrefab,square.transform.position,Quaternion.identity,LM.GameField);square.block.Add(blocker);square.type=SquareTypes.WIREBLOCK;yield break;
            }
            if(command=="Shuffle board"){LM.NoMatches();yield return Settled();yield break;}
            if(command=="Clear board"){foreach(var item in LM.GetItems().Where(i=>i).ToArray())item.DestroyItem();LM.FindMatches();yield return Settled();yield break;}
            if(command=="Spawn selected gem"){var square=Selected();if(!square.item)square.GenItem(false);SetPiece(square.item,ItemsTypes.NONE,Mathf.Clamp(Gem,0,5));yield break;}
            int spawn=Array.IndexOf(new[]{"Spawn TNT","Spawn rocket H","Spawn rocket V","Spawn propeller","Spawn color ball"},command);
            if(spawn>=0){SetPiece(Selected().item,Powers[spawn],Gem);yield break;}
            if(command=="Combine selected")
            {
                var a=Selected().item;var b=Selected().GetNeighborRight()?.item;if(!a||!b)throw new InvalidOperationException("Choose a cell with an occupied right neighbor.");
                SetPiece(a,Powers[Mathf.Clamp(PowerA,0,4)],Gem);SetPiece(b,Powers[Mathf.Clamp(PowerB,0,4)],Gem);
                yield return ColorBallReady(a);yield return ColorBallReady(b);
                if(!RoyalPowerupDirector.TrySwap(a,b))throw new InvalidOperationException("Combination rejected by the game.");LM.FindMatches();yield return Settled();yield break;
            }
            if(command=="Activate selected"){var selected=Selected().item;yield return ColorBallReady(selected);if(!RoyalPowerupDirector.Tap(selected))throw new InvalidOperationException("Select a power-up.");yield return Settled();yield break;}
            if(command=="Star reward"){ValidateLevel();InitScript.Instance.SaveLevelStarsCount(Level,Mathf.Min(3,RoyalSave.GetInt(StarKey)+1));RoyalSave.Save();Refresh();yield return RewardStars();yield break;}
            if(command=="Star animation"||command=="Character / reward"||command=="Win effect"){yield return RewardStars();yield break;}
            if(command=="Coin reward"){InitScript.Instance.AddGems(Mathf.Clamp(Amount,0,1000000));Refresh();RoyalPowerupDirector.Get.Firework(Center,1);RoyalAudioDirector.Get.Play(RoyalCue.Create);yield return GetComponent<RoyalQaMenu>().CoinReward();yield break;}
            if(command=="Reward effect"){RoyalPowerupDirector.Get.Preview("Celebration",Center);yield return new WaitForSeconds(1.4f);yield break;}
            if(command=="Ice Impact"||command=="Fire Impact")
            {if(command=="Ice Impact")RoyalToolkitImpact.TryPlayIce(Center,LM.squareWidth);else RoyalToolkitImpact.TryPlayFire(Center,LM.squareWidth);yield return new WaitForSeconds(1.6f);yield break;}
            if(command=="Break particles"||command=="Falling fragments"){CandyShatterService.Instance.Shatter(Selected().item.sprRenderer,1);yield return new WaitForSeconds(1.8f);yield break;}
            if(command.StartsWith("VFX Match")||command=="TNT explosion"||command=="Rocket trails"||command=="Propeller effect")
            {string effect=command.StartsWith("VFX ")?command.Substring(4):command=="TNT explosion"?"TNT":command=="Rocket trails"?(PowerA==2?"Rocket V":"Rocket"):"Helicopter";RoyalPowerupDirector.Get.Preview(effect,Center);if(command=="Rocket trails"){float until=Time.time+10;while(RoyalPowerupDirector.Get.RocketVisualsActive&&Time.time<until)yield return null;}else yield return new WaitForSeconds(1.8f);yield break;}
            if(command=="Gem idle"){Selected().item.StartIdleAnim();yield return new WaitForSeconds(1.2f);Selected().item.StopIdleAnim();yield break;}
            // Repeatable animation fixtures reuse the existing board; they do not reload a level.
            yield return Practice();
            if(command=="Hint aura"||command=="Hint animation"||command=="Swap")
            {
                SetHintFixture();var plan=RoyalHintSolver.Find(LM);if(plan==null)throw new InvalidOperationException("No hint in the fixture.");
                if(command=="Swap"){Swap(plan.First,plan.Second);yield return Settled();}
                else{RoyalHintSettings.Enabled=true;yield return null;AI.THIS.GetComponent<RoyalHintPresenter>().Show(plan);yield return new WaitForSeconds(2.2f);}
                yield break;
            }
            if(command=="Invalid swap"){Swap(LM.GetSquare(0,0).item,LM.GetSquare(1,0).item);yield return new WaitForSeconds(.6f);yield break;}
            if(command=="Fall"){var i=LM.GetSquare(3,3).item;i.transform.position+=Vector3.up*LM.squareHeight*3;i.justCreatedItem=true;i.StartFalling();yield return Settled();yield break;}
            if(command=="Booster activation"||command=="TNT animation"||command=="Color-ball effect")
            {var item=LM.GetSquare(3,3).item;SetPiece(item,command=="Color-ball effect"?ItemsTypes.BOMB:command=="TNT animation"?ItemsTypes.PACKAGE:Powers[Mathf.Clamp(PowerA,0,4)],0);yield return ColorBallReady(item);RoyalPowerupDirector.Tap(item);yield return Settled();yield break;}
            if(command=="Create special"||command=="Booster creation")
            {foreach(var p in new[]{new Vector2Int(2,2),new Vector2Int(3,2),new Vector2Int(4,2),new Vector2Int(3,3),new Vector2Int(3,4)})LM.GetSquare(p.x,p.y).item.SetColor(0);}
            else
            {
                int n=command=="Match 5"?5:command=="Match 4"?4:3;
                for(int x=1;x<=n;x++)LM.GetSquare(x,3).item.SetColor(0);
                if(command=="Cascade"||command=="Cascade animation")for(int x=1;x<=3;x++)LM.GetSquare(x,x==2?1:2).item.SetColor(4);
            }
            LM.FindMatches();yield return Settled();
        }
        string StarKey=>string.Format("Level.{0:000}.StarsCount",Level);
        public HashSet<Square> ReadDetectedMatches()
        {
            var result=new HashSet<Square>();if(!LM||LM.squaresArray==null)return result;
            var previous=LM.countedSquares;LM.countedSquares=new Hashtable();
            try{foreach(var sq in LM.squaresArray)if(sq&&sq.item)foreach(var item in sq.FindMatchesAround())if(item)result.Add(item.square);}
            finally{LM.countedSquares=previous;}
            return result;
        }
        void ValidateLevel(){if(Level<1||!Resources.Load<TextAsset>("Levels/"+Level))throw new InvalidOperationException("No authored level "+Level+".");}
        public IEnumerator OpenLevel()
        {
            RoyalThrone.Campaign.RoyalReferenceCampaign.RequirePlayable(Level);
            ValidateLevel();ToMap();yield return null;
            if(RoyalThrone.Campaign.RoyalReferenceCampaign.Enabled)
                foreach(var tutorial in FindObjectsByType<JellyGarden.Scripts.GUI.TutorialStuff.TutorialManager>(FindObjectsInactive.Include))
                    if(tutorial.enabled){suspendedTutorials.Add(tutorial);tutorial.enabled=false;}
            RoyalSave.SetInt("OpenLevel",Level);RoyalSave.Save();LM.LoadLevel();LM.gameStatus=GameState.PrepareGame;
            float end=Time.realtimeSinceStartup+20;while(LM.gameStatus!=GameState.Playing&&Time.realtimeSinceStartup<end)yield return null;
            if(LM.gameStatus!=GameState.Playing)throw new InvalidOperationException("Level opening timed out.");yield return Settled();
        }
        IEnumerator Board()
        {
            if(LM.gameStatus==GameState.Map)yield return OpenLevel();
            if(LM.gameStatus==GameState.Pause){MenuManager.Instance.HideAllCanvasElements();LM.gameStatus=GameState.Playing;}
            if(LM.gameStatus==GameState.RegenLevel)yield return Settled();
            if(LM.gameStatus!=GameState.Playing)throw new InvalidOperationException("Open or restart a level first ("+LM.gameStatus+").");yield return Settled();
        }
        bool BoardMoving()=>LM.IsResolvingMatches||RoyalPowerupDirector.Busy||LM.DragBlocked||LM.gameStatus==GameState.RegenLevel||LM.GetItems().Any(i=>i&&(i.falling||i.destroying));
        public IEnumerator Settled()
        {
            yield return null;float end=Time.realtimeSinceStartup+30;int quietFrames=0;
            while(quietFrames<3&&Time.realtimeSinceStartup<end){quietFrames=BoardMoving()?0:quietFrames+1;yield return null;}
            if(BoardMoving())throw new InvalidOperationException("Board did not settle within 30 seconds.");
        }
        public IEnumerator Practice()
        {
            yield return Board();LM.StopAllCoroutines();AI.THIS.StopAllCoroutines();AI.THIS.GetComponent<RoyalHintPresenter>()?.Hide();RoyalPowerupDirector.Get.Cancel();
            RetireTutorial();
            foreach(var tutorial in FindObjectsByType<JellyGarden.Scripts.GUI.TutorialStuff.TutorialManager>(FindObjectsInactive.Include))
                if(tutorial.enabled){suspendedTutorials.Add(tutorial);tutorial.enabled=false;}
            if(!practice)
            {
                foreach(Transform child in LM.GameField)Destroy(child.gameObject);yield return null;
                LM.maxCols=LM.maxRows=8;LM.squaresArray=new Square[64];LM.GameField.position=new Vector3(-3.5f*LM.squareWidth,3.5f*LM.squareHeight,0);
                for(int y=0;y<8;y++)for(int x=0;x<8;x++)
                {var go=new GameObject("QA cell "+x+","+y,typeof(SpriteRenderer),typeof(Square));go.transform.SetParent(LM.GameField,false);go.transform.localPosition=new Vector3(x*LM.squareWidth,-y*LM.squareHeight,0);go.GetComponent<SpriteRenderer>().sprite=LM.squareSprite;var sq=go.GetComponent<Square>();sq.col=x;sq.row=y;sq.type=SquareTypes.EMPTY;LM.squaresArray[y*8+x]=sq;}
                practice=true;
            }
            foreach(var i in LM.GameField.GetComponentsInChildren<Item>())Destroy(i.gameObject);yield return null;
            foreach(var sq in LM.squaresArray){foreach(var b in sq.block)if(b)Destroy(b);sq.block.Clear();sq.type=SquareTypes.EMPTY;sq.item=null;sq.GenItem(false);}
            yield return null;
            foreach(var sq in LM.squaresArray){SetPiece(sq.item,ItemsTypes.NONE,(sq.col+sq.row*2)%5);sq.item.falling=false;sq.item.justCreatedItem=false;sq.item.transform.position=sq.transform.position+Vector3.back*.2f;}
            // Deep copies, never mutate TargetLevel ScriptableObjects.
            LM.targetObject=new[]{new TargetObject{type=Target.SCORE,targetCount=1000000}};LM.Limit=99;LevelManager.Score=0;LM.DragBlocked=false;LM.destroyAnyway.Clear();
            foreach(var icon in FindObjectsByType<TargetIcon>())if(icon.transform.IsChildOf(LM.Level.transform))icon.SetTarget(LM.targetObject[0]);
            SetHintFixture();AI.THIS.allowShowTip=true;LM.StartCoroutine(AI.THIS.CheckPossibleCombines());yield return null;
        }
        Square Selected(){var sq=LM.GetSquare(Column,Row,true);if(!sq||sq.type==SquareTypes.NONE||!sq.item)throw new InvalidOperationException("Select an occupied playable cell (column / row are zero-based).");return sq;}
        Vector3 Center=>LM.GameField.position+new Vector3((LM.maxCols-1)*LM.squareWidth*.5f,-(LM.maxRows-1)*LM.squareHeight*.5f,-.2f);
        static void SetPiece(Item item,ItemsTypes type,int color)
        {
            if(!item)throw new InvalidOperationException("Cell has no gem.");
            if(type==ItemsTypes.BOMB)
            {
                // Real creation sets Color Ball's sentinel color (555), positions it, and plays Reveal.
                // Giving it the selected gem color let QA balls match and chain as ordinary gems.
                item.NextType=ItemsTypes.BOMB;item.ChangeType();return;
            }
            item.currentType=type;item.NextType=ItemsTypes.NONE;item.SetColor(Mathf.Clamp(color,0,5));item.GetComponent<RoyalPieceSkin>()?.ApplyNow();
        }
        static IEnumerator ColorBallReady(Item item)
        {
            if(!item||item.currentType!=ItemsTypes.BOMB)yield break;
            var visual=item.GetComponent<RoyalThrone.Boosters.ColorBall.V4.ColorBallSeatV4>()?.View;
            bool revealing=visual&&visual.Current==RoyalThrone.Boosters.ColorBall.V4.ColorBallVisualV4.Phase.Reveal;
            while(item&&visual&&visual.Current==RoyalThrone.Boosters.ColorBall.V4.ColorBallVisualV4.Phase.Reveal)yield return null;
            if(revealing)yield return new WaitForSeconds(.2f);
        }
        static void Swap(Item a,Item b){a.SwitchDirection(a.transform.position-b.transform.position);}
        void SetHintFixture(){LM.GetSquare(1,3).item.SetColor(0);LM.GetSquare(2,3).item.SetColor(0);LM.GetSquare(3,3).item.SetColor(2);LM.GetSquare(3,2).item.SetColor(0);}
        IEnumerator RewardStars()
        {
            int stars=LM.stars;LM.stars=3;MenuManager.Instance.ShowMenuComplete();yield return new WaitForSeconds(2.2f);
            // Presentation replay never enters GameState.Win, so no reward/save events.
            foreach(var a in MenuManager.Instance.GetComponentsInChildren<AnimationManager>(true))a.StopAllCoroutines();
            MenuManager.Instance.HideAllCanvasElements();LM.stars=stars;
        }
        void Update()
        {
            if(!RoyalSave.Testing||!Ready)return;
            if(LM.gameStatus!=GameState.Pause)Time.timeScale=Speed;
            if(Loop&&!Busy&&!Visible&&LastReplay.Length>0&&Time.unscaledTime>=replayAt)Request(LastReplay);
        }
        void OnDestroy(){if(Instance==this)Instance=null;Time.timeScale=1;}
    }
}
#endif
