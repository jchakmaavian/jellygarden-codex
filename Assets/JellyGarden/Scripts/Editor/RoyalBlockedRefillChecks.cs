using System;
using System.Collections;
using System.Linq;
using JellyGarden.Scripts;
using JellyGarden.Scripts.Juice;
using RoyalThrone.Testing;
using UnityEngine;
using Object=UnityEngine.Object;
namespace RoyalThrone.Editor
{
 public static class RoyalBlockedRefillChecks
 {
  public static IEnumerator Run(Action<bool,string> check)
  {
   if(!RoyalSave.Testing)throw new InvalidOperationException("Requires isolated QA profile");
   var qa=RoyalQaService.Instance;float old=Time.captureDeltaTime;
   try{
    foreach(int fps in new[]{15,30,60})foreach(string pattern in new[]{"left-roof","right-roof","middle-roof","stacked-holes","diagonal","top","void"}){
     yield return qa.Practice();qa.Visible=false;Time.captureDeltaTime=1f/fps;
     var lm=LevelManager.THIS;AI.THIS.StopAllCoroutines();AI.THIS.CancelVisibleHint();AI.THIS.allowShowTip=false;
     foreach(var sq in lm.squaresArray){sq.item.SetColor(1+(sq.col+sq.row*2)%4);sq.item.GetComponent<RoyalPieceSkin>()?.ApplyNow();}
     int col=pattern=="right-roof"?7:pattern=="middle-roof"?3:0;
     var blockers=new System.Collections.Generic.List<Square>();
     void Empty(Square sq){if(sq.item)Object.Destroy(sq.item.gameObject);sq.item=null;}
     if(pattern!="top"&&pattern!="void"){
      for(int x=Math.Max(0,col-1);x<=Math.Min(7,col+1);x++){
       if(pattern=="diagonal"&&x!=col)continue;
       var sq=lm.GetSquare(x,3);Empty(sq);sq.type=SquareTypes.SOLIDBLOCK;blockers.Add(sq);
      }
     }
     var hole=lm.GetSquare(col,pattern=="top"?0:4);Empty(hole);
     if(pattern=="stacked-holes"){Empty(lm.GetSquare(col,5));Empty(lm.GetSquare(col,6));}
     if(pattern=="void"){var sq=lm.GetSquare(2,2);Empty(sq);sq.type=SquareTypes.NONE;blockers.Add(sq);}
     yield return null;int repairs=RoyalBoardIntegrity.Repairs;lm.FindMatches();
     int calm=0,f=0;for(;f<fps*15;f++){
      yield return new WaitForEndOfFrame();
      bool busy=lm.IsResolvingMatches||lm.InputSwaps>0||RoyalPowerupDirector.Busy||lm.GetItems().Any(i=>i&&(i.falling||i.destroying||i.justCreatedItem));
      calm=busy?0:calm+1;if(calm>=fps/2)break;
     }
     string label=fps+" FPS "+pattern;
     check(f<fps*15&&!lm.DragBlocked,label+": resolver settles and input unlocks");
     check(hole.item&&lm.squaresArray.Where(s=>s.CanGoInto()).All(s=>s.item&&!s.item.destroying),label+": every playable cell is filled");
     check(blockers.All(s=>!s.item),label+": blockers and board voids are not filled");
     var pieces=lm.GameField.GetComponentsInChildren<Item>().Where(i=>i).ToArray();
     check(pieces.All(i=>i.square&&i.square.item==i)&&pieces.Select(i=>i.square).Distinct().Count()==pieces.Length,label+": no orphan or duplicate cell ownership");
     check(RoyalBoardIntegrity.Repairs==repairs,label+": no watchdog repair needed");
    }
    yield return qa.Practice();var level=LevelManager.THIS;
    var reserve=level.GetSquare(3,3);var host=reserve.item;reserve.item=null;host.destroying=true;reserve.Reserve(host);
    try{
     typeof(LevelManager).GetMethod("GenerateNewItems",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(level,new object[]{false,true});
     check(!reserve.item&&reserve.Reserved,"Fallback preserves the pending special's reserved cell");
    }finally{host.destroying=false;reserve.Release(host);reserve.item=host;}
   }finally{Time.captureDeltaTime=old;}
  }
 }
}
