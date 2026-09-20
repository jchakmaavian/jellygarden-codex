using PlayerPrefs = RoyalThrone.Testing.RoyalSave;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using JellyGarden.Scripts;
using JellyGarden.Scripts.Juice;
using JellyGarden.Scripts.Targets;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace RoyalThrone.Editor
{
    public static class RoyalEffectsChecks
    {
        [MenuItem("Tools/Royal Throne/Effects/Open current level for testing without life cost #F12")]
        public static void OpenBoard()
        {
            var level=LevelManager.THIS;if(!EditorApplication.isPlaying||!level||level.gameStatus!=GameState.Map)return;
            if(!JellyGarden.Scripts.GUI.MenuManager.Instance.menuPlay.activeSelf)InitScript.Instance.TryOpenLevel(Mathf.Max(1,PlayerPrefs.GetInt("OpenLevel",1)));
            JellyGarden.Scripts.GUI.MenuManager.Instance.menuPlay.SetActive(false);
            level.gameStatus=GameState.PrepareGame;
        }
        [MenuItem("Tools/Royal Throne/Effects/Inspect current board #F11")]
        public static void Inspect()
        {
            var l=LevelManager.THIS;if(!l)return;Directory.CreateDirectory(Folder);
            string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
            var lines=new List<string>{"LevelManager: "+PathOf(l.transform),"Field: "+PathOf(l.GameField),"Status: "+l.gameStatus+" blocked: "+l.DragBlocked};
            foreach(var c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None))lines.Add("CANVAS "+PathOf(c.transform)+" mode="+c.renderMode+" sort="+c.sortingOrder);
            foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(t.name=="CanvasBack"||t.name=="BottomPanel"||t.name=="BoostBomb"||t.name=="Panel"&&t.GetComponent<UnityEngine.UI.Image>())lines.Add("NODE "+PathOf(t)+" active="+t.gameObject.activeInHierarchy);
            File.WriteAllLines(Folder+"hierarchy.txt",lines);Capture("current-gameplay");Debug.Log("[Royal Effects] Board inspection saved.");
        }
        static readonly List<string> checks=new List<string>(),errors=new List<string>();
        static bool cellBreakOnly,rocketOnly;
        static string Folder=>rocketOnly?"Design/Effects/Rocket-review-v2/":cellBreakOnly?"Design/Effects/Unity-review-v6/":"Design/Effects/Unity-review-v4/";
        [MenuItem("Tools/Royal Throne/Effects/Verify restored smooth match breaks %#F10")]
        public static void RunCellBreaks(){cellBreakOnly=true;Run();}
        [MenuItem("Tools/Royal Throne/Effects/Verify rocket travel at half and full speed %#F11")]
        public static void RunRockets(){rocketOnly=true;Run();}
        static LevelManager lm;static Transform originalField,testField;static Square[] originalSquares;
        static TargetObject[] originalTargets;static int rows,cols,limit,score,move;static bool drag,showScores;
        static int money,lives;static UnityEngine.Random.State random;
        static int originalMotion;static bool hadMotion;
        static readonly List<float> frames=new List<float>();static bool recording;
        static RoyalReviewAudioCapture capture;
        [MenuItem("Tools/Royal Throne/Effects/Run board regression and capture #F10")]
        public static void Run()
        {
            RoyalHomeScreenBuilder.RequireCopy();lm=LevelManager.THIS;
            if(!EditorApplication.isPlaying||!lm||lm.gameStatus!=GameState.Playing||lm.DragBlocked)throw new InvalidOperationException("Open a settled gameplay board in Play Mode first.");
            checks.Clear();errors.Clear();frames.Clear();Directory.CreateDirectory(Folder);Application.logMessageReceived+=Log;
            originalField=lm.GameField;originalSquares=lm.squaresArray;originalTargets=lm.targetObject;rows=lm.maxRows;cols=lm.maxCols;limit=lm.Limit;score=LevelManager.Score;move=lm.moveID;drag=lm.DragBlocked;showScores=lm.showPopupScores;
            money=InitScript.Gems;lives=InitScript.lifes;random=UnityEngine.Random.state;
            hadMotion=PlayerPrefs.HasKey("RoyalThrone.HomeMotion");originalMotion=PlayerPrefs.GetInt("RoyalThrone.HomeMotion",1);
            lm.StopAllCoroutines();AI.THIS.StopAllCoroutines();AI.THIS.allowShowTip=false;lm.DragBlocked=true;lm.showPopupScores=false;
            originalField.gameObject.SetActive(false);testField=new GameObject("Royal workshop test board — no save rewards").transform;
            testField.position=new Vector3(-3.5f*lm.squareWidth,3.5f*lm.squareHeight,0);lm.GameField=testField;lm.maxRows=lm.maxCols=8;lm.squaresArray=new Square[64];
            lm.targetObject=new[]{new TargetObject{type=Target.SCORE,targetCount=1000000}};lm.Limit=99;lm.destroyAnyway.Clear();
            for(int y=0;y<8;y++)for(int x=0;x<8;x++)
            {var go=new GameObject("Test cell "+x+","+y);go.transform.SetParent(testField,false);go.transform.localPosition=new Vector3(x*lm.squareWidth,-y*lm.squareHeight,0);var sr=go.AddComponent<SpriteRenderer>();sr.sprite=lm.squareSprite;sr.color=new Color(.22f,.57f,.9f);var sq=go.AddComponent<Square>();sq.row=y;sq.col=x;sq.type=SquareTypes.EMPTY;lm.squaresArray[y*8+x]=sq;}
            RoyalRegressionRunner.Run(Suite());
        }
        static void Check(bool ok,string text){if(!ok){errors.Add("FAIL: "+text);throw new Exception(text);}checks.Add(text);}
        static void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message);}
        static void Seed()
        {
            foreach(var item in testField.GetComponentsInChildren<Item>())Object.DestroyImmediate(item.gameObject);
            foreach(var sq in lm.squaresArray){foreach(var b in sq.block)if(b)Object.DestroyImmediate(b);sq.block.Clear();sq.type=SquareTypes.EMPTY;sq.item=null;sq.GenItem(false);}
        }
        static void SetColours()
        {foreach(var sq in lm.squaresArray){var i=sq.item;i.SetColor((sq.col+sq.row*2)%5);i.currentType=ItemsTypes.NONE;i.NextType=ItemsTypes.NONE;i.falling=false;i.justCreatedItem=false;i.transform.position=sq.transform.position+Vector3.back*.2f;}}
        static Item At(int x,int y)=>lm.GetSquare(x,y).item;
        static void Paint(int color,params Vector2Int[] points){foreach(var p in points)At(p.x,p.y).SetColor(color);}
        static List<List<Item>> Groups()=>new JellyGarden.Scripts.System.CombineManager().GetCombine();
        static IEnumerator Settled(float max=10)
        {float until=Time.realtimeSinceStartup+max;do{yield return null;}while(RoyalPowerupDirector.Busy&&Time.realtimeSinceStartup<until);yield return new WaitForSeconds(.35f);Check(!RoyalPowerupDirector.Busy,"Impact queue settles within "+max+" seconds");}
        static IEnumerator Suite()
        {
            bool completed=false;
            try
            {
                if(rocketOnly)
                {
                    yield return RoyalRocketChecks.Run(lm,Seed,SetColours,Check);
                    Check(InitScript.Gems==money&&InitScript.lifes==lives,"Rocket tests preserve coins and lives");
                    Check(errors.Count==0,"No runtime errors during rocket tests");completed=true;yield break;
                }
                if(cellBreakOnly)
                {
                    yield return RoyalCellBreakChecks.RunRestored(lm,Seed,SetColours,Check);
                    Check(InitScript.Gems==money&&InitScript.lifes==lives,"Cell-break regression preserves coins and lives");
                    Check(errors.Count==0,"No runtime errors during cell-break regression");
                    completed=true;yield break;
                }
                Check(Resources.Load<GameObject>("RoyalGameplay/EffectsRig"),"Authored particle rig loads");
                foreach(var name in RoyalPieceSkin.Colours.Concat(new[]{"rocket","tnt","helicopter","orb","smoke","gem","heart","star","ice","key","chest","chains","stone"}))Check(RoyalPieceSkin.Art(name),"WebP sprite loads: "+name);
                foreach(var name in new[]{"match-a","match-b","rocket","tnt","helicopter","create","castle-theme","land","click","firework","celebrate"})Check(Resources.Load<AudioClip>("RoyalGameplay/Audio/"+name),"Audio imports: "+name);
                foreach(var blockName in new[]{"Block","SolidBlock","ThrivingBlock","UndestrBlock","WireBlock"}){
                    var prefab=Resources.Load<GameObject>("Prefabs/"+blockName);var original=prefab.GetComponent<SpriteRenderer>().sprite;
                    var styled=RoyalThrone.Presentation.RoyalVisualCatalog.Replace(original,true);
                    Check(styled&&styled.texture!=original.texture&&styled.name==original.name,blockName+" has new art and preserves target identifier");
                    Check(Mathf.Abs(Mathf.Max(styled.bounds.size.x,styled.bounds.size.y)-Mathf.Max(original.bounds.size.x,original.bounds.size.y))<.01f,blockName+" preserves board footprint");
                }
                Seed();Check(lm.GetItems().Where(i=>i).All(i=>i.sprRenderer.sprite==RoyalPieceSkin.For(i.currentType,i.color)),"New pieces show royal art before their first rendered frame");yield return null;SetColours();var blocked=lm.GetSquare(3,3);blocked.type=SquareTypes.WIREBLOCK;
                Check(!blocked.CanGoOut(),"Chain artwork retains trapped-piece rule");blocked.type=SquareTypes.BLOCK;
                for(int k=0;k<2;k++){var block=Object.Instantiate(Resources.Load<GameObject>("Prefabs/Block"),blocked.transform.position,Quaternion.identity,testField);blocked.block.Add(block);}
                blocked.DestroyBlock();Check(blocked.block.Count==1&&blocked.type==SquareTypes.BLOCK,"Layered obstacle loses only one layer per direct hit");
                blocked.DestroyBlock();Check(blocked.block.Count==0&&blocked.type==SquareTypes.EMPTY,"Cleared obstacle releases its square");
                var targetIcon=Object.Instantiate(Resources.Load<TargetIcon>("Targets/TargetBlocks"));targetIcon.SetTarget(lm.targetObject[0]);
                Check(ReferenceEquals(targetIcon.target,lm.targetObject[0]),"Goal artwork retains live target data reference");Object.DestroyImmediate(targetIcon.gameObject);
                Check(!JuiceConfig.Instance.EnableHitStop,"Ordinary matches do not change global timescale");
                Seed();yield return null;SetColours();yield return RoyalBoardSurfaceChecks.Run(lm,Check);
                yield return PresentationChecks();
                yield return RoyalToolkitChecks.Run(lm,Check);
                Seed();yield return null;SetColours();yield return RoyalInputRegression.Run(lm,Check);
                capture=RoyalReviewAudioCapture.Begin();
                for(int n=3;n<=5;n++)
                {
                    Seed();yield return null;SetColours();for(int x=1;x<=n;x++)At(x,3).SetColor(0);
                    var groups=Groups();Check(groups.Any(g=>g.Count>=n),n+"-line detected");
                    Check(n==3||lm.GetItems().Any(i=>i&&i.NextType==(n==5?ItemsTypes.BOMB:ItemsTypes.VERTICAL_STRIPPED)||i&&n==4&&i.NextType==ItemsTypes.HORIZONTAL_STRIPPED),n+"-line power-up rule");
                    yield return RoyalToolkitChecks.ResolveProductionMatch(lm,Check,n);
                }
                Seed();yield return null;SetColours();Paint(2,new Vector2Int(2,2),new Vector2Int(3,2),new Vector2Int(2,3),new Vector2Int(3,3));
                At(1,3).SetColor(4); // Avoid an overlapping horizontal three in the deterministic base pattern.
                Check(RoyalPatterns.SquareAt(lm.GetSquare(2,2)).Count==4,"2×2 square detected");var squareGroups=Groups();Check(lm.GetItems().Any(i=>i&&i.NextType==ItemsTypes.HELICOPTER),"2×2 creates helicopter");
                foreach(var g in squareGroups)foreach(var item in g)item.DestroyItem(true);yield return Settled();Check(lm.GetItems().Any(i=>i&&i.currentType==ItemsTypes.HELICOPTER),"Helicopter survives match destruction as a playable piece");
                Seed();yield return null;SetColours();Paint(0,new Vector2Int(2,2),new Vector2Int(3,2),new Vector2Int(4,2),new Vector2Int(3,3),new Vector2Int(3,4));Groups();Check(lm.GetItems().Any(i=>i&&i.NextType==ItemsTypes.PACKAGE),"T match creates TNT");
                ItemsTypes[] powers={ItemsTypes.HORIZONTAL_STRIPPED,ItemsTypes.PACKAGE,ItemsTypes.HELICOPTER,ItemsTypes.BOMB};
                foreach(var type in powers)
                {
                    Seed();yield return null;SetColours();At(3,3).currentType=type;At(3,3).DestroyItem(true);yield return Settled();
                    Check(lm.GetItems().Count(i=>i)<63,type+" removes additional targets on impact");Capture(type.ToString());
                }
                for(int a=0;a<4;a++)for(int b=a;b<4;b++)
                {
                    Seed();yield return null;SetColours();At(3,3).currentType=powers[a];At(4,3).currentType=powers[b];
                    Check(RoyalPowerupDirector.TrySwap(At(3,3),At(4,3)),"Combo accepted: "+powers[a]+" + "+powers[b]);yield return Settled(14);
                    Check(lm.GetItems().Count(i=>i)<62,"Combo changes board: "+powers[a]+" + "+powers[b]);
                }
                Seed();yield return null;SetColours();At(3,3).currentType=ItemsTypes.PACKAGE;At(5,3).currentType=ItemsTypes.HORIZONTAL_STRIPPED;At(3,3).DestroyItem(true);yield return Settled();Check(!lm.GetSquare(7,3).item,"TNT chains into rocket and reaches board edge");
                Seed();yield return null;SetColours();At(3,3).currentType=ItemsTypes.PACKAGE;At(3,3).DestroyItem(true);Time.timeScale=0;
                int before=lm.GetItems().Count(i=>i);yield return new WaitForSecondsRealtime(.3f);Check(lm.GetItems().Count(i=>i)==before,"Pause holds TNT impact");Time.timeScale=1;yield return Settled();
                Seed();yield return null;SetColours();yield return RoyalBoardPresentationChecks.Run(lm,Check);
                Seed();yield return null;SetColours();yield return RoyalHintPresentationChecks.Run(lm,Check);
                Seed();yield return null;SetColours();lm.lastDraggedItem=null;lm.lastSwitchedItem=null;
                var aItem=At(1,1);var bItem=At(2,1);int oldLimit=lm.Limit;lm.DragBlocked=false;aItem.SwitchDirection(Vector3.left);
                yield return new WaitForSeconds(.5f);Check(At(1,1)==aItem&&At(2,1)==bItem&&lm.Limit==oldLimit,"Invalid swap returns pieces and spends no move");
                At(3,3).currentType=ItemsTypes.HORIZONTAL_STRIPPED;lm.DragBlocked=false;oldLimit=lm.Limit;
                Check(RoyalPowerupDirector.Tap(At(3,3)),"Tap activates power-up");Check(lm.Limit==oldLimit-1,"Power-up tap spends exactly one move");
                float timeout=Time.realtimeSinceStartup+15;while(lm.DragBlocked&&Time.realtimeSinceStartup<timeout)yield return null;
                Check(!lm.DragBlocked&&!RoyalPowerupDirector.Busy,"Live gravity/refill/cascade finishes and input returns");
                Check(lm.GetItems().Where(i=>i).All(i=>!i.destroying&&!i.falling&&i.square.item==i),"Refill has valid square ownership and no falling leftovers");
                if(capture)capture.End();capture=null;
                recording=true;previous=0;EditorApplication.update+=Frame;for(int i=0;i<12;i++){RoyalPowerupDirector.Get.Preview(i%2==0?"TNT":"Rocket",Vector3.zero);yield return new WaitForSeconds(.12f);}yield return new WaitForSeconds(.8f);recording=false;EditorApplication.update-=Frame;
                Check(RoyalAudioDirector.Get.PeakVoices<=RoyalEffectsProfile.Get.soundVoices,"Simultaneous audio stays within voice cap");
                Check(RoyalPowerupDirector.Get.PeakProjectiles<=RoyalEffectsProfile.Get.projectilePool,"Projectiles stay within pool cap");
                Check(CandyShatterService.Instance.PeakFragments<=JuiceConfig.Instance.ShatterPoolSize,"Falling fragments stay within their fixed pool");
                Check(InitScript.Gems==money&&InitScript.lifes==lives,"Regression run preserves coins and lives");
                Check(errors.Count==0,"No runtime errors during regression run");
                completed=true;
            }
            finally {if(!completed)errors.Add("Regression aborted before completion; inspect the Unity Console for the originating exception.");Finish();}
        }
        static IEnumerator PresentationChecks()
        {
            Seed();yield return null;SetColours();
            var decoration=new GameObject("Ingredient counter regression - decorative renderer");
            var renderer=decoration.AddComponent<SpriteRenderer>();
            try
            {
                Check(lm.GetIngredientsCount("missing ingredient")==0,"Empty decorative sprite does not throw in ingredient counting");
                string name=At(3,1).sprRenderer.sprite.name;renderer.sprite=At(3,1).sprRenderer.sprite;
                Check(lm.GetIngredientsCount(name)==0,"Decorative sprite with a matching name is not counted as an ingredient");
                At(3,1).currentType=ItemsTypes.INGREDIENT;
                Check(lm.GetIngredientsCount(name)==1,"Only the owned board ingredient contributes to the count");
                At(3,1).currentType=ItemsTypes.NONE;
            }
            finally { Object.DestroyImmediate(decoration); }

            PlayerPrefs.SetInt("RoyalThrone.HomeMotion",1);
            var shatter=CandyShatterService.Instance;shatter.Clear();
            var source=At(3,1).sprRenderer;float startY=source.bounds.center.y,unit=Mathf.Max(source.bounds.size.x,source.bounds.size.y);
            shatter.Shatter(source,1);Check(shatter.ActiveFragments>=4,"Ordinary gem break emits recognisable supplied fracture pieces");
            yield return new WaitForSeconds(.42f);
            var fragments=shatter.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled).ToArray();
            Check(fragments.Length>0&&fragments.Average(r=>r.transform.position.y)<startY-unit*.6f,"Chunks visibly fall more than half a gem height instead of disappearing on impact");
            var property=new MaterialPropertyBlock();bool opaque=true;
            foreach(var fragment in fragments){fragment.GetPropertyBlock(property);opaque&=property.GetColor("_Color").a>.99f;}
            Check(opaque,"Falling chunks remain opaque after the former fade-out point");
            var pausedPositions=fragments.Select(r=>r.transform.position).ToArray();Time.timeScale=0;
            yield return new WaitForSecondsRealtime(.18f);
            Check(fragments.Select((r,index)=>Vector3.Distance(r.transform.position,pausedPositions[index])<.001f).All(v=>v),"Pause freezes falling debris");
            Time.timeScale=1;yield return new WaitForSeconds(1.6f);
            Check(shatter.ActiveFragments==0,"Debris leaves below the board and returns to the pool");
            PlayerPrefs.SetInt("RoyalThrone.HomeMotion",0);shatter.Shatter(source,1);
            Check(shatter.ActiveFragments<=2,"Reduced motion uses a restrained two-chunk breakup");shatter.Clear();

            // Exercise real pool pressure without yielding a frame or changing board
            // items. Last-slot degradation must retain a falling chunk, not allocate.
            int capacity=JuiceConfig.Instance.ShatterPoolSize;
            Check(capacity==240,"Fragment pressure scenarios use the authored 240-slot pool");
            for(int remaining=3;remaining>=1;remaining--)
            {
                shatter.Clear();PlayerPrefs.SetInt("RoyalThrone.HomeMotion",remaining==2?0:1);
                if(remaining==2)for(int i=0;i<(capacity-2)/2;i++)shatter.Shatter(source,1,true);
                else for(int i=0;i<(capacity-3)/3;i++)shatter.Shatter(source,1,true);
                if(remaining==1){PlayerPrefs.SetInt("RoyalThrone.HomeMotion",0);shatter.Shatter(source,1,true);}
                Check(shatter.ActiveFragments==capacity-remaining,"Debris pressure fixture leaves "+remaining+" slots");
                PlayerPrefs.SetInt("RoyalThrone.HomeMotion",1);shatter.Shatter(source,1);
                Check(shatter.ActiveFragments==capacity,"Low-capacity breakup uses the remaining "+remaining+" fragment slots");
                shatter.Shatter(source,1);
                Check(shatter.ActiveFragments==capacity&&shatter.GetComponentsInChildren<MeshRenderer>().Length==capacity,"Exhausted debris pool stays inside its renderer and activity cap");
            }
            shatter.Clear();

            // Set only the fixture's backing state, avoiding Map reward/menu events.
            // The actual effect Update observes the transition on the next frame.
            var stateField=typeof(LevelManager).GetField("GameStatus",BindingFlags.Instance|BindingFlags.NonPublic);
            var director=RoyalPowerupDirector.Get;var beforeState=lm.gameStatus;
            try
            {
                director.Cancel();yield return null;director.Burst(source.transform.position);
                Check(director.ActiveOperations==0,"Decorative TNT tail does not hold board input");
                stateField.SetValue(lm,GameState.Map);yield return null;yield return new WaitForSeconds(.14f);
                Check(VisibleImpactParticles(director)==0,"Leaving gameplay cancels finished TNT particles and delayed corona");
                director.Preview("TNT",source.transform.position);yield return null;
                Check(VisibleImpactParticles(director)>0,"Effects Workshop preview still plays while already on Map");
            }
            finally {stateField.SetValue(lm,beforeState);director.Cancel();}

            // The celebration is decorative only: no Button/GraphicRaycaster and no
            // move, score or economy callback is attached to its reusable canvas.
            PlayerPrefs.SetInt("RoyalThrone.HomeMotion",1);
            var celebration=lm.GetComponent<RoyalCelebration>();if(celebration){celebration.Cancel();yield return new WaitForSeconds(2.5f);}
            Check(RoyalCelebration.Play(4),"Cascade achievement opens the native celebration");
            celebration=lm.GetComponent<RoyalCelebration>();
            Check(!RoyalCelebration.Play(5),"Consecutive celebrations coalesce through the cooldown");
            Check(celebration.PoolSize==32,"Celebration sparkles use a fixed 32-element pool");
            var banner=lm.transform.Find("Royal celebration - no input blocking");
            Check(banner&&!banner.GetComponent<CanvasGroup>().blocksRaycasts&&!banner.GetComponent<UnityEngine.UI.GraphicRaycaster>(),"Celebration never intercepts a swap or booster tap");
            yield return new WaitForSeconds(.25f);Capture("celebration-bravo");
            yield return new WaitForSeconds(1.15f);Check(!celebration.IsShowing,"Celebration clears promptly without delaying input");
            PlayerPrefs.SetInt("RoyalThrone.HomeMotion",originalMotion);
        }
        static int VisibleImpactParticles(RoyalPowerupDirector director)
            =>director.GetComponentsInChildren<ParticleSystem>().Sum(ps=>ps.particleCount)+
              (RoyalToolkitImpact.Existing?RoyalToolkitImpact.Existing.GetComponentsInChildren<ParticleSystem>(true).Sum(ps=>ps.particleCount):0);
        static double previous;
        static void Frame(){double now=EditorApplication.timeSinceStartup;if(recording&&previous>0)frames.Add((float)((now-previous)*1000));previous=now;}
        [MenuItem("Tools/Royal Throne/Effects/Capture current presentation #F4")]
        public static void CapturePresentation()
        {
            if(!EditorApplication.isPlaying)return;
            Capture(LevelManager.THIS?LevelManager.THIS.gameStatus==GameState.Pause?"pause":"current-gameplay":"loading");
        }
        [MenuItem("Tools/Royal Throne/Effects/Capture celebration %#F4")]
        public static void CaptureCelebration()
        {
            if(!EditorApplication.isPlaying||!LevelManager.THIS||LevelManager.THIS.gameStatus!=GameState.Playing)return;
            RoyalRegressionRunner.Run(CelebrationFrame());
        }
        static IEnumerator CelebrationFrame()
        {
            yield return new WaitForSeconds(2.5f);
            RoyalCelebration.Play(4);yield return new WaitForSeconds(.25f);
            yield return CaptureFrame("celebration-bravo");
        }
        static void Capture(string name) => RoyalRegressionRunner.Run(CaptureFrame(name));
        static IEnumerator CaptureFrame(string name)
        {
            // Read the completed Game view, not an intermediate Editor render target.
            yield return new WaitForEndOfFrame();Directory.CreateDirectory(Folder);
            var tex=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Folder+name+".png",tex.EncodeToPNG());Object.Destroy(tex);
        }
        static void Finish()
        {
            if(capture)capture.End();capture=null;
            recording=false;EditorApplication.update-=Frame;Application.logMessageReceived-=Log;Time.timeScale=1;
            if(hadMotion)PlayerPrefs.SetInt("RoyalThrone.HomeMotion",originalMotion);else PlayerPrefs.DeleteKey("RoyalThrone.HomeMotion");
            CandyShatterService.Instance.Clear();MatchVFXService.Instance.Clear();lm.GetComponent<RoyalCelebration>()?.Cancel();
            RoyalMatchBreakService.Existing?.Clear();
            lm.StopAllCoroutines();RoyalPowerupDirector.Get.Cancel();lm.GameField=originalField;lm.squaresArray=originalSquares;lm.maxRows=rows;lm.maxCols=cols;lm.targetObject=originalTargets;lm.Limit=limit;LevelManager.Score=score;lm.moveID=move;lm.showPopupScores=showScores;lm.destroyAnyway.Clear();
            typeof(LevelManager).GetField("royalResolving",BindingFlags.Instance|BindingFlags.NonPublic)?.SetValue(lm,false);
            lm.lastDraggedItem=lm.lastSwitchedItem=null;lm.DragBlocked=drag;UnityEngine.Random.state=random;Object.Destroy(testField.gameObject);originalField.gameObject.SetActive(true);
            frames.Sort();string result="Checks completed: "+checks.Count+"\n"+string.Join("\n",checks)+"\nErrors: "+string.Join("\n",errors)+"\nEditor update p95 ms (not device GPU benchmark): "+(frames.Count>0?frames[(int)(frames.Count*.95f)]:0);
            File.WriteAllText(Folder+"checks.txt",result);Debug.Log("[Royal Effects Checks] "+result);AI.THIS.StartCoroutine(AI.THIS.CheckPossibleCombines());
            cellBreakOnly=false;rocketOnly=false;
        }
    }
}
