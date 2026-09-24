#if UNITY_EDITOR && !ROYAL_DISABLE_QA
using System;
using System.Collections;
using System.IO;
using System.Linq;
using JellyGarden.Scripts;
using RoyalThrone.Presentation;
using RoyalThrone.Testing;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
namespace RoyalThrone.ReferenceGameplay.Editor
{
    public static class ReferenceHomeSkyChecks
    {
        const string RestoreKey="Royal.HomeSky.RestorePlayOptions";
        [InitializeOnLoadMethod] static void RegisterRestore(){EditorApplication.playModeStateChanged+=RestoreOptions;}
        static void RestoreOptions(PlayModeStateChange state){if(state!=PlayModeStateChange.EnteredEditMode||!SessionState.GetBool(RestoreKey,false))return;EditorSettings.enterPlayModeOptionsEnabled=SessionState.GetBool(RestoreKey+"Enabled",false);EditorSettings.enterPlayModeOptions=(EnterPlayModeOptions)SessionState.GetInt(RestoreKey+"Options",0);SessionState.SetBool(RestoreKey,false);}
        public static void EnterFreshPlay(){if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");SessionState.SetBool(RestoreKey+"Enabled",EditorSettings.enterPlayModeOptionsEnabled);SessionState.SetInt(RestoreKey+"Options",(int)EditorSettings.enterPlayModeOptions);SessionState.SetBool(RestoreKey,true);EditorSettings.enterPlayModeOptionsEnabled=false;EditorApplication.isPlaying=true;}
        const string Folder="Design/HomeSky";
        static void Check(bool ok,string text){File.AppendAllText(Folder+"/checks.txt",(ok?"PASS ":"FAIL ")+text+"\n");if(!ok)throw new InvalidOperationException(text);}
        static IEnumerator Shot(string name){yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Folder+"/"+name+".png",t.EncodeToPNG());UnityEngine.Object.Destroy(t);}
        public static IEnumerator Run(){Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"/checks.txt","Homepage sky checks\n");var qa=RoyalQaService.Instance;var lm=LevelManager.THIS;int coins=InitScript.Gems;
            EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
            try{qa.StartSession();qa.Visible=false;RoyalSave.SetInt("RoyalThrone.HomeMotion",1);yield return new WaitForSecondsRealtime(.6f);
                var home=UnityEngine.Object.FindAnyObjectByType<RoyalHomeScreen>();var sky=home.GetComponent<ReferenceHomeSky>();
                var bg=home.transform.Find("Castle framing/Royal blue courtyard").GetComponent<Image>();
                Check(home.IsHomeVisible&&bg.sprite==Resources.Load<Sprite>("ReferenceGameplay/Home/Sky/Homepage_bg"),"Supplied background appears on live homepage");
                Check(sky&&sky.CloudCount==3,"Exactly three cloud sprites created");Check(bg.GetComponentsInChildren<Image>().Where(i=>i.name.StartsWith("Sky cloud ")).All(i=>!i.raycastTarget&&i.transform.parent==bg.transform),"Clouds render behind controls and never intercept taps");
                var first=Enumerable.Range(0,3).Select(sky.CloudPosition).ToArray();yield return Shot("Homepage-clouds");yield return new WaitForSecondsRealtime(12);yield return Shot("Homepage-clouds-12s");
                Check(Enumerable.Range(0,3).All(i=>Vector2.Distance(first[i],sky.CloudPosition(i))>.1f),"All three clouds drift continuously at normal speed");
                RoyalSave.SetInt("RoyalThrone.HomeMotion",0);yield return null;float elapsed=sky.MotionSeconds;var still=sky.CloudPosition(0);yield return new WaitForSecondsRealtime(.4f);Check(Mathf.Approximately(elapsed,sky.MotionSeconds)&&Vector2.Distance(still,sky.CloudPosition(0))<.01f,"Reduced-motion setting holds clouds without resetting positions");
                RoyalSave.SetInt("RoyalThrone.HomeMotion",1);lm.gameStatus=GameState.Pause;yield return null;elapsed=sky.MotionSeconds;yield return new WaitForSecondsRealtime(.4f);Check(Mathf.Approximately(elapsed,sky.MotionSeconds),"Cloud animation sleeps while homepage is hidden");lm.gameStatus=GameState.Map;yield return null;
                home.resourceButtons[4].onClick.Invoke();yield return null;Check(home.dialog.activeSelf,"Settings still opens above cloud layer");home.ClosePanel();
                yield return qa.EndSession();Check(!RoyalSave.Testing&&InitScript.Gems==coins,"Normal player data restored");File.AppendAllText(Folder+"/checks.txt","ALL CHECKS PASSED\n");
            }finally{if(RoyalSave.Testing)RoyalThrone.Editor.RoyalRegressionRunner.Run(qa.EndSession());}
        }
    }
}
#endif
