using UnityEngine;

namespace RoyalThrone.Editor
{
 public static class RoyalSquarePlusOneChecks
 {
public static System.Collections.IEnumerator Run(System.Action<bool,string> Check){
 const string folder="Logs/port-checks/QaFeedback/11-SquarePlusOne/four-gem-merge/";System.IO.Directory.CreateDirectory(folder);
 var qa=RoyalThrone.Testing.RoyalQaService.Instance;
 if(!RoyalThrone.Testing.RoyalSave.Testing)throw new System.InvalidOperationException("Requires isolated QA session");
 qa.Level=1;qa.Visible=false;qa.Loop=false;qa.Speed=1;
 float original=UnityEngine.Time.captureDeltaTime;
 int[,] cases={{2,3,3,3,3,2},{2,4,3,4,3,5},{5,3,4,3,4,2},{5,4,4,4,4,5},{3,2,3,3,2,3},{4,2,4,3,5,3},{3,5,3,4,2,4},{4,5,4,4,5,4}};
 try{foreach(int rate in new[]{60,30,15,0})for(int k=0;k<8;k++){
 int fps=rate==0?60:rate;bool cascade=rate==0;UnityEngine.Time.captureDeltaTime=1f/fps;yield return qa.Practice();qa.Visible=false;
 var lm=JellyGarden.Scripts.LevelManager.THIS;lm.limitType=JellyGarden.Scripts.LIMIT.MOVES;lm.Limit=99;
 var ai=JellyGarden.Scripts.AI.THIS;ai.StopAllCoroutines();ai.CancelVisibleHint();ai.allowShowTip=false;
 foreach(var sq in lm.squaresArray){sq.item.SetColor(1+(sq.col+sq.row*2)%4);sq.item.currentType=sq.item.NextType=JellyGarden.Scripts.ItemsTypes.NONE;}
 var dest=lm.GetSquare(cases[k,2],cases[k,3]);var source=lm.GetSquare(cases[k,4],cases[k,5]).item;
 var group=new System.Collections.Generic.List<JellyGarden.Scripts.Item>();
 for(int x=3;x<=4;x++)for(int y=3;y<=4;y++){var sq=lm.GetSquare(x,y);if(sq!=dest)group.Add(sq.item);}
 var extra=lm.GetSquare(cases[k,0],cases[k,1]).item;group.Add(extra);group.Add(cascade?dest.item:source);
 foreach(var item in group){item.SetColor(0);item.GetComponent<JellyGarden.Scripts.Juice.RoyalPieceSkin>()?.ApplyNow();}
 string label=(cascade?"Cascade":fps+" FPS")+" extra("+cases[k,0]+","+cases[k,1]+")";
 if(fps==60&&!cascade){yield return new UnityEngine.WaitForEndOfFrame();var tex=UnityEngine.ScreenCapture.CaptureScreenshotAsTexture();System.IO.File.WriteAllBytes(folder+"case-"+k+"-before.jpg",tex.EncodeToJPG(92));UnityEngine.Object.Destroy(tex);}
 int repairs=JellyGarden.Scripts.Juice.RoyalBoardIntegrity.Repairs;
 if(cascade)lm.FindMatches();else source.SwitchDirection(source.transform.position-dest.transform.position);
 Check(cascade?lm.IsResolvingMatches:lm.InputSwaps==1,label+": requested resolver or player swap started");
 bool ownership=true;int calm=0,peakPropellers=0,peakTnt=0;bool sawGather=false,onlySquareGathered=true,extraClearedNormally=false;int peakGather=0;int frame=0;
 for(;frame<fps*12;frame++){
 yield return new UnityEngine.WaitForEndOfFrame();
 if(fps==60&&!cascade&&k==0&&frame<120){System.IO.Directory.CreateDirectory(folder+"frames/");var capture=UnityEngine.ScreenCapture.CaptureScreenshotAsTexture();System.IO.File.WriteAllBytes(folder+"frames/"+frame.ToString("D4")+".jpg",capture.EncodeToJPG(92));UnityEngine.Object.Destroy(capture);}
 bool busy=lm.IsResolvingMatches||lm.InputSwaps>0||JellyGarden.Scripts.Juice.RoyalPowerupDirector.Busy;int propellers=0,tnt=0;
 var owners=new System.Collections.Generic.HashSet<JellyGarden.Scripts.Square>();
 foreach(var item in lm.GameField.GetComponentsInChildren<JellyGarden.Scripts.Item>())if(item){
 busy|=item.falling||item.destroying||item.inputSwapLocked;
 if(!item.destroying){ownership&=item.square&&item.square.item==item&&owners.Add(item.square);if(item.currentType==JellyGarden.Scripts.ItemsTypes.HELICOPTER)propellers++;if(item.currentType==JellyGarden.Scripts.ItemsTypes.PACKAGE)tnt++;}
 }
 peakPropellers=System.Math.Max(peakPropellers,propellers);peakTnt=System.Math.Max(peakTnt,tnt);
 int gathering=0;
 foreach(var sr in UnityEngine.Object.FindObjectsByType<UnityEngine.SpriteRenderer>())if(sr.name=="Special gathering gem"){
 sawGather=true;gathering++;onlySquareGathered&=sr.GetComponentInParent<JellyGarden.Scripts.Item>()!=extra;
 }
 peakGather=System.Math.Max(peakGather,gathering);
 if(extra){onlySquareGathered&=!extra.royalSpecialGather;if(extra.destroying&&!extra.royalSpecialGather)extraClearedNormally=true;}
 calm=busy?0:calm+1;if(calm>=fps/2)break;
 }
 int finalPropellers=0;foreach(var item in lm.GetItems())if(item&&item.currentType==JellyGarden.Scripts.ItemsTypes.HELICOPTER)finalPropellers++;
 Check(peakPropellers==1&&finalPropellers==1,label+": exactly one Propeller created and remains playable (actual peak="+peakPropellers+", final="+finalPropellers+", TNT="+peakTnt+")");
 bool retired=true;foreach(var item in group)retired&=!item;
 Check(retired,label+": all five source gems retired, including extra gem");
 Check(sawGather&&peakGather==4&&onlySquareGathered,label+": exactly four square gems merge; extra never joins formation");
 Check(extraClearedNormally,label+": extra gem runs ordinary destruction separately");
 Check(ownership&&JellyGarden.Scripts.Juice.RoyalBoardIntegrity.Repairs==repairs,label+": unique reciprocal ownership without integrity repair");
 Check(frame<fps*12&&!lm.IsResolvingMatches&&!lm.DragBlocked,label+": board settles and input unlocks");
 if(fps==60&&!cascade){var tex=UnityEngine.ScreenCapture.CaptureScreenshotAsTexture();System.IO.File.WriteAllBytes(folder+"case-"+k+"-after.jpg",tex.EncodeToJPG(92));UnityEngine.Object.Destroy(tex);}
 }}finally{UnityEngine.Time.captureDeltaTime=original;}
}

 }
}
