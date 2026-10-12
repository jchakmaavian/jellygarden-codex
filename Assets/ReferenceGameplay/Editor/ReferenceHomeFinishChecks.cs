#if UNITY_EDITOR && !ROYAL_DISABLE_QA
using System;
using System.Collections;
using System.IO;
using System.Linq;
using JellyGarden.Scripts;
using JellyGarden.Scripts.Juice;
using RoyalThrone.Presentation;
using RoyalThrone.Testing;
using UnityEditor;
using UnityEngine;
namespace RoyalThrone.ReferenceGameplay.Editor
{
    public static class ReferenceHomeFinishChecks
    {
        const string Folder="Design/ReferenceHomeFinish";
        static void Check(bool ok,string text){File.AppendAllText(Folder+"/validation.txt",(ok?"PASS ":"FAIL ")+text+"\n");if(!ok)throw new InvalidOperationException(text);}
        static IEnumerator Shot(string name){yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Folder+"/"+name+".png",t.EncodeToPNG());UnityEngine.Object.Destroy(t);}
        public static IEnumerator Run(bool homeOnly=false){File.WriteAllText(Folder+"/validation.txt","RUNNING homepage and finishing stage\n");var qa=RoyalQaService.Instance;var lm=LevelManager.THIS;int coins=InitScript.Gems;bool music=SoundBase.Instance.GetComponent<AudioSource>()?SoundBase.Instance.GetComponent<AudioSource>().isPlaying:false;
            EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
            try{
                qa.StartSession();yield return qa.Practice();qa.Visible=false;var home=UnityEngine.Object.FindAnyObjectByType<RoyalHomeScreen>();
                Check(home&&home.GetComponent<ReferenceHomeLayout>(),"Imported homepage adapter active on existing home");
                var bg=home.transform.Find("Castle framing/Royal blue courtyard").GetComponent<UnityEngine.UI.Image>().sprite;
                lm.gameStatus=GameState.Map;yield return new WaitForSecondsRealtime(.6f);yield return Shot("Homepage");
                Check(home.IsHomeVisible&&home.levelButton.interactable,"Homepage is visible and original level button remains connected");
                Check(home.resourceButtons.All(b=>b&&b.onClick.GetPersistentEventCount()>=0)&&home.tabButtons.Length==5,"Resource controls and five navigation buttons retained");
                Check(home.transform.Find("Castle framing/Royal blue courtyard").GetComponent<UnityEngine.UI.Image>().sprite==bg,"Background sprite unchanged");
                home.resourceButtons[4].onClick.Invoke();yield return null;Check(home.dialog.activeSelf,"Existing Settings action still opens");home.ClosePanel();
                Check(ReferenceVfxPool.Has("RemainingMovesParticles")&&ReferenceVfxPool.Has("RemainingMovesHitParticles"),"Both recovered native finishing particle prefabs available");
                for(int test=0;test<(homeOnly?0:3);test++){
                    if(test>0){JellyGarden.Scripts.GUI.MenuManager.Instance.HideAllCanvasElements();lm.gameStatus=GameState.Playing;lm.DragBlocked=false;}
                    yield return qa.Practice();qa.Visible=false;lm.limitType=LIMIT.MOVES;lm.Limit=test==0?20:test==1?0:5;
                    int expected=lm.Limit;var rocket=lm.GetSquare(1,1).item;rocket.NextType=ItemsTypes.HORIZONTAL_STRIPPED;rocket.ChangeType();
                    var tnt=lm.GetSquare(5,5).item;tnt.NextType=ItemsTypes.PACKAGE;tnt.ChangeType();yield return new WaitForSeconds(.6f);
                    lm.gameStatus=GameState.PreWinAnimations;float deadline=Time.realtimeSinceStartup+65;bool shot=false,blastShot=false,skipSent=false;
                    while(lm.gameStatus==GameState.PreWinAnimations&&Time.realtimeSinceStartup<deadline){
                        var finish=lm.GetComponent<ReferenceWinFinish>();
                        if(test==0&&finish&&finish.Converted>=5&&!shot){shot=true;yield return Shot("Remaining-moves");}
                        if(test==2&&finish&&finish.Sent>=1&&!skipSent){lm.RequestSkipWinCelebration();skipSent=true;}
                        if(test==0&&finish&&finish.Fired>0&&!blastShot){blastShot=true;yield return Shot("Booster-finale");}
                        yield return null;
                    }
                    var result=lm.GetComponent<ReferenceWinFinish>();Check(lm.gameStatus==GameState.Win,"Finishing case "+test+" reaches Win without stall");
                    Check(result.Sent==expected&&result.Converted==expected&&lm.Limit==0,"Every remaining move converted exactly once ("+expected+")");
                    Check(!result.Running&&!RoyalPowerupDirector.Busy&&!lm.GetItems().Any(ReferenceWinFinish.Power),"Existing boosters complete and no finishing operation remains");
                }
                yield return qa.EndSession();yield return null;Check(!RoyalSave.Testing&&InitScript.Gems==coins,"Normal save and currency restored");File.AppendAllText(Folder+"/validation.txt","ALL CHECKS PASSED\n");
            }finally{if(RoyalSave.Testing)RoyalThrone.Editor.RoyalRegressionRunner.Run(qa.EndSession());}
        }
    }
}
#endif
