using PlayerPrefs = RoyalThrone.Testing.RoyalSave;
using System;
using System.Collections;
using System.Linq;
using JellyGarden.Scripts;
using JellyGarden.Scripts.Juice;
using RoyalThrone.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalThrone.Editor
{
    /// <summary>Runs only inside the disposable board fixture; never changes player progression.</summary>
    public static class RoyalBoardPresentationChecks
    {
        public static IEnumerator Run(LevelManager lm,Action<bool,string> check)
        {
            var ai=AI.THIS;
            bool hadPreference=PlayerPrefs.HasKey(RoyalHintSettings.PreferenceKey);
            int rawPreference=PlayerPrefs.GetInt(RoyalHintSettings.PreferenceKey,1);
            int moves=lm.Limit,score=LevelManager.Score,gems=InitScript.Gems,lives=InitScript.lifes;
            GameState state=lm.gameStatus;bool blocked=lm.DragBlocked;
            try
            {
                check(ai,"Hint controller exists");
                // A real legal vertical swap completes a horizontal three, with no match at rest.
                lm.GetSquare(1,1).item.SetColor(0);lm.GetSquare(2,1).item.SetColor(0);
                lm.GetSquare(3,1).item.SetColor(1);lm.GetSquare(3,2).item.SetColor(0);
                lm.DragBlocked=false;lm.gameStatus=GameState.Playing;
                RoyalHintSettings.Enabled=true;ai.StopAllCoroutines();ai.CancelVisibleHint();ai.allowShowTip=true;
                ai.StartCoroutine(ai.CheckPossibleCombines());
                float until=Time.realtimeSinceStartup+8;
                while(ai.VisibleHintCount==0&&Time.realtimeSinceStartup<until)yield return null;
                check(ai.gotTip&&ai.VisibleHintCount>0,"Hints ON reveals a legal candidate after the idle delay");
                RoyalHintSettings.Enabled=false;
                check(ai.VisibleHintCount==0,"Hints OFF cancels the active visual immediately");
                yield return new WaitForSeconds(.1f);
                check(lm.GetItems().Where(i=>i&&i.anim).All(i=>!i.anim.GetCurrentAnimatorStateInfo(0).IsName("tip")),"Hints OFF exits any current tip animation");
                ai.StopAllCoroutines();ai.allowShowTip=true;ai.StartCoroutine(ai.CheckPossibleCombines());
                yield return new WaitForSeconds(6.2f);
                check(ai.gotTip&&ai.VisibleHintCount==0,"Hints OFF keeps move discovery active while suppressing automatic visuals");
                check(PlayerPrefs.GetInt(RoyalHintSettings.PreferenceKey,-1)==0,"Hints OFF is stored for scene and app reloads");
                RoyalHintSettings.Enabled=true;
                ai.StopAllCoroutines();ai.allowShowTip=true;ai.StartCoroutine(ai.CheckPossibleCombines());
                until=Time.realtimeSinceStartup+8;
                while(ai.VisibleHintCount==0&&Time.realtimeSinceStartup<until)yield return null;
                check(ai.VisibleHintCount>0,"Hints ON resumes automatic assistance");
                lm.gameStatus=GameState.Pause;yield return null;
                check(ai.VisibleHintCount==0,"Opening pause cancels a visible board hint");
                lm.gameStatus=GameState.Playing;ai.StopAllCoroutines();ai.CancelVisibleHint();ai.allowShowTip=false;
                foreach(var item in lm.GetItems().Where(i=>i&&!i.destroying&&!i.falling))
                {
                    item.GetComponent<RoyalPieceSkin>().ApplyNow();
                    var size=item.sprRenderer.bounds.size;
                    check(Mathf.Max(size.x/lm.squareWidth,size.y/lm.squareHeight)>.88f&&size.x<=lm.squareWidth*1.01f&&size.y<=lm.squareHeight*1.01f,
                        "Piece fills its cell without crossing adjacent hit areas: "+item.square.col+","+item.square.row);
                }
                var canvas=JellyGarden.Scripts.GUI.MenuManager.Instance.GetCanvasGlobal();
                foreach(string menu in new[]{"MenuPause","Settings"})
                {
                    var hint=canvas.Find(menu+"/Image/Hints");
                    check(hint&&hint.GetComponent<Button>()&&hint.Find("Royal caption"),menu+" includes an interactive matching-hints setting");
                }
                foreach(var resolution in new[]{new Vector2(360,640),new Vector2(390,844),new Vector2(412,915),new Vector2(768,1024)})
                foreach(var board in new[]{new Vector2Int(8,10),new Vector2Int(9,11),new Vector2Int(5,11),new Vector2Int(9,5)})
                {
                    var safe=new Rect(0,24/resolution.y,1,1-64/resolution.y);
                    var viewport=RoyalGameplayPresentation.PlayableViewport(safe);
                    var bounds=new Bounds(Vector3.zero,new Vector3(lm.squareWidth*(board.x+.06f),lm.squareHeight*(board.y+.51f),0));
                    float aspect=resolution.x/resolution.y,size=RoyalGameplayPresentation.FitSize(bounds,aspect,viewport);
                    float cellPixels=lm.squareWidth/(size*2*aspect)*resolution.x;
                    check(size>0&&bounds.size.x<=size*2*aspect*viewport.width+.001f&&bounds.size.y<=size*2*viewport.height+.001f&&cellPixels>=32,
                        board.x+"x"+board.y+" board fits safe area above boosters with readable cells at "+resolution.x+"x"+resolution.y+" ("+cellPixels.ToString("F1")+" px/cell)");
                }
                check(lm.Limit==moves&&LevelManager.Score==score&&InitScript.Gems==gems&&InitScript.lifes==lives,"Hint and layout checks preserve moves, score, gems and lives");
            }
            finally
            {
                if(ai){ai.StopAllCoroutines();ai.CancelVisibleHint();ai.allowShowTip=false;}
                RoyalHintSettings.Enabled=rawPreference!=0;
                if(!hadPreference)PlayerPrefs.DeleteKey(RoyalHintSettings.PreferenceKey);
                else PlayerPrefs.SetInt(RoyalHintSettings.PreferenceKey,rawPreference);
                PlayerPrefs.Save();lm.DragBlocked=blocked;lm.gameStatus=state;
            }
        }
    }
}
