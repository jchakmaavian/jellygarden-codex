#if UNITY_EDITOR && !ROYAL_DISABLE_QA
using System;
using System.Collections;
using System.IO;
using System.Linq;
using JellyGarden.Scripts;
using JellyGarden.Scripts.GUI;
using JellyGarden.Scripts.Juice;
using RoyalThrone.Testing;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace RoyalThrone.ReferenceGameplay.Editor
{
    public static class ReferenceBoosterChecks
    {
        const string Folder="Design/ReferenceBoosterBar";
        static void Check(bool ok,string text){File.AppendAllText(Folder+"/validation.txt",(ok?"PASS ":"FAIL ")+text+"\n");if(!ok)throw new InvalidOperationException(text);}
        static IEnumerator Shot(string name){yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Folder+"/"+name+".png",t.EncodeToPNG());UnityEngine.Object.Destroy(t);}
        static IEnumerator Wait(ReferenceBoosterBar bar){float end=Time.realtimeSinceStartup+35;while(bar.Busy&&Time.realtimeSinceStartup<end)yield return null;Check(!bar.Busy,"Tool operation completes");yield return RoyalQaService.Instance.Settled();}
        public static IEnumerator Run()
        {
            File.WriteAllText(Folder+"/validation.txt","RUNNING imported booster tools\n");var qa=RoyalQaService.Instance;var lm=LevelManager.THIS;Mouse mouse=null;
            int normalCoins=InitScript.Gems;var normalStock=ReferenceBoosterBar.Types.Select(t=>RoyalSave.GetInt(t.ToString())).ToArray();var actions=InputManager.Instance.inputActionAsset;var oldDevices=actions.devices;
            EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
            try
            {
                qa.StartSession();yield return qa.Practice();qa.Visible=false;var bar=ReferenceBoosterBar.Instance;
                Check(bar&&RoyalSave.Testing,"Reference tools active in isolated save");
                foreach(var t in ReferenceBoosterBar.Types)RoyalSave.SetInt(t.ToString(),3);
                yield return new WaitForSecondsRealtime(.6f);yield return Shot("Bottom-bar");
                var controls=lm.GetComponentsInChildren<BoostIcon>(true).Where(b=>ReferenceBoosterBar.IsTool(b.type)).ToArray();Check(controls.Length==4,"All four original buttons bind to Hammer, Arrow, Cannon, Jester");
                Check(controls.All(b=>b.boostCount.enabled&&b.boostCount.gameObject.activeInHierarchy&&b.boostCount.transform.IsChildOf(b.transform)&&b.boostCount.text=="3"),"Live tool counts are visible inside their bottom-bar badges");
                controls.First(b=>b.type==BoostType.RoyalHammer).GetComponent<Button>().onClick.Invoke();Check(bar.Selected==0&&!lm.BoardInputAllowed,"Selection owns board input without triggering a normal swipe");
                yield return Shot("Hammer-selected");bar.CancelSelection();Check(bar.Stock(0)==3&&lm.BoardInputAllowed,"Cancel spends nothing and releases board input");
                RoyalSave.SetInt(BoostType.RoyalHammer.ToString(),0);InitScript.Instance.SetGems(0);bar.Choose(BoostType.RoyalHammer);Check(bar.ShopOpen,"Empty stock opens imported purchase popup");
                yield return Shot("Hammer-shop");Check(!bar.Buy()&&InitScript.Gems==0&&bar.Stock(0)==0,"Insufficient balance cannot spend or grant tools");
                bar.Offer(0,out int count,out int cost);InitScript.Instance.SetGems(cost+20);Check(bar.Buy(),"Existing local coin economy purchases a tool pack");
                Check(InitScript.Gems==20&&bar.Stock(0)==count&&!bar.ShopOpen&&bar.Selected==0,"Purchase updates balance, stock and selection once");
                Check(!bar.Buy()&&InitScript.Gems==20&&bar.Stock(0)==count,"Duplicate purchase callback cannot grant or charge again");bar.CancelSelection();
                for(int tool=0;tool<3;tool++){
                    yield return qa.Practice();qa.Visible=false;int moves=lm.Limit;RoyalSave.SetInt(ReferenceBoosterBar.Types[tool].ToString(),3);
                    var sq=lm.GetSquare(3,3);if(tool==0){qa.Column=3;qa.Row=3;yield return qa.Execute("Spawn blocker");qa.Visible=false;}bar.Choose(ReferenceBoosterBar.Types[tool]);Check(!bar.TryUse(null)&&bar.Stock(tool)==3,"Invalid target does not consume "+ReferenceBoosterBar.Types[tool]);
                    if(tool==0){
                        mouse=InputSystem.AddDevice<Mouse>();actions.devices=new InputDevice[]{mouse};
                        Vector2 p=Camera.main.WorldToScreenPoint(sq.transform.position);InputSystem.QueueStateEvent(mouse,new MouseState{position=p,buttons=1});
                        float until=Time.realtimeSinceStartup+2;while(!bar.Busy&&Time.realtimeSinceStartup<until)yield return null;
                        InputSystem.QueueStateEvent(mouse,new MouseState{position=p,buttons=0});yield return null;
                        Check(bar.Busy||bar.Stock(0)==2,"Real board pointer selects Hammer target");actions.devices=oldDevices;InputSystem.RemoveDevice(mouse);mouse=null;
                    }else Check(bar.TryUse(sq),"Selected row/column action starts");
                    yield return Wait(bar);Check(bar.Stock(tool)==2&&lm.Limit==moves,"One "+ReferenceBoosterBar.Types[tool]+" spent, no move used");
                    Check(bar.LastHitCount==(tool==0?1:tool==1?lm.maxCols:lm.maxRows),"Correct cell/row/column receives hits");
                    if(tool==0)Check(sq.type!=SquareTypes.WIREBLOCK&&sq.block.Count==0,"Hammer can target and remove the wire blocker through the existing damage system");
                    Check(lm.GetItems().Where(i=>i).All(i=>i.square&&i.square.item==i),"Refilled pieces retain unique cell ownership");
                }
                yield return qa.Practice();qa.Visible=false;var rocket=lm.GetSquare(1,1).item;rocket.currentType=ItemsTypes.HORIZONTAL_STRIPPED;rocket.GetComponent<RoyalPieceSkin>()?.ApplyNow();
                var pieces=lm.GetItems().Where(i=>i).ToArray();int before=lm.Limit;RoyalSave.SetInt(BoostType.RoyalJester.ToString(),3);bar.Choose(BoostType.RoyalJester);Check(bar.TryUse(null),"Jester shuffle starts on confirmation");yield return Wait(bar);
                Check(bar.Stock(3)==2&&lm.Limit==before,"Shuffle spends one hat and no move");Check(pieces.All(i=>i&&i.square&&i.square.item==i)&&rocket.currentType==ItemsTypes.HORIZONTAL_STRIPPED,"Shuffle preserves original pieces and Rocket");
                Check(lm.GetMatches().Count==0&&RoyalHintSolver.Find(lm)!=null,"Shuffled board has no automatic match and has a valid move");
                yield return Shot("After-shuffle");bar.Choose(BoostType.RoyalCannon);yield return qa.EndSession();yield return null;
                Check(!ReferenceBoosterBar.OwnsInput&&!RoyalSave.Testing,"Leaving level clears tool selection and restores normal profile");
                Check(InitScript.Gems==normalCoins&&normalStock.SequenceEqual(ReferenceBoosterBar.Types.Select(t=>RoyalSave.GetInt(t.ToString()))),"Normal coins and tool inventory unchanged by QA");
                File.AppendAllText(Folder+"/validation.txt","ALL TOOL CHECKS PASSED\n");
            }
            finally{actions.devices=oldDevices;if(mouse!=null)InputSystem.RemoveDevice(mouse);if(RoyalSave.Testing)RoyalThrone.Editor.RoyalRegressionRunner.Run(qa.EndSession());}
        }
    }
}
#endif

