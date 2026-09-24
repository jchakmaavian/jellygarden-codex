#if UNITY_EDITOR && !ROYAL_DISABLE_QA
using System;
using System.Collections;
using System.IO;
using System.Linq;
using JellyGarden.Scripts;
using JellyGarden.Scripts.GUI;
using RoyalThrone.Testing;
using RoyalThrone.Presentation;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
namespace RoyalThrone.ReferenceGameplay.Editor
{
    public static class ReferenceLayoutChecks
    {
        const string Folder="Design/ReferenceGameplayLayout";
        static void Check(bool value,string message)
        {
            File.AppendAllText(Folder+"/validation.txt",(value?"PASS ":"FAIL ")+message+"\n");
            if(!value)throw new InvalidOperationException(message);
        }
        static IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            var texture=ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Folder+"/"+name+".png",texture.EncodeToPNG());UnityEngine.Object.Destroy(texture);
        }
        public static IEnumerator Run()
        {
            File.WriteAllText(Folder+"/validation.txt","RUNNING gameplay layout checks\n");
            var qa=RoyalQaService.Instance;var lm=LevelManager.THIS;
            var view=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));view.Show();view.Focus();
            try
            {
                qa.StartSession();Check(RoyalSave.Testing,"Separate QA save active");
                yield return qa.Execute("Original levels");qa.Level=14;yield return qa.OpenLevel();qa.Visible=false;
                yield return new WaitForSecondsRealtime(1);
                var hud=lm.transform.Find("Royal gameplay HUD/Safe area");Check(hud,"Live gameplay HUD is active");
                // The imported tool bar adds its own "Native left/right" art from Tools/ (checked by ReferenceBoosterChecks), not layout art.
                var imported=hud.GetComponentsInChildren<Image>(true).Where(i=>(i.name.Contains(" left")||i.name.Contains(" right")||i.name.Contains("Packaged header"))&&!i.name.StartsWith("Native ")).ToArray();
                Check(imported.Length>=30&&imported.All(i=>i.sprite&&AssetDatabase.GetAssetPath(i.sprite).StartsWith("Assets/ReferenceGameplay/Layout/")),"Header, portrait frame and five rail slots use imported APK sprites");
                Check(hud.GetComponentsInChildren<BoostIcon>(true).Length==4,"Four original boost controllers and inventory bindings retained");
                yield return Capture("Gameplay-Level14-first");
                Check(hud.GetComponentsInChildren<BoostIcon>(true).All(b=>b.boostCount&&b.transform.Find("Indicator/Plus")&&b.transform.Find("Lock")),"In-game booster count, plus and lock connections intact");
                var hand=hud.GetComponentsInChildren<BoostIcon>(true).Single(b=>b.name=="BoostHand");
                RoyalSave.SetInt(hand.type.ToString(),3);yield return null;
                // The four rail tools belong to the imported tool bar (BoostIcon routes them there since the tool bar was connected),
                // so a tap selects the tool in ReferenceBoosterBar rather than LevelManager.ActivatedBoost.
                var bar=ReferenceBoosterBar.Instance;int handIndex=System.Array.IndexOf(ReferenceBoosterBar.Types,hand.type);
                hand.GetComponent<Button>().onClick.Invoke();yield return null;
                Check(bar&&handIndex>=0&&bar.Selected==handIndex&&lm.activatedBoost==null,"Imported hand button selects its original action");
                hand.GetComponent<Button>().onClick.Invoke();yield return null;
                Check(bar.Selected<0&&lm.activatedBoost==null,"Second button tap cancels selection");
                Check(lm.GetItems().Where(i=>i).All(i=>i.square&&i.square.item==i),"Level 14 piece ownership intact");
                Canvas.ForceUpdateCanvases();
                var safe=Screen.safeArea;var screen=new Vector2(Screen.width,Screen.height);
                var vp=RoyalGameplayPresentation.PlayableViewport(new Rect(safe.position/screen,safe.size/screen));
                Check(lm.squaresArray.Where(s=>s&&s.type!=SquareTypes.NONE).All(s=>vp.Contains((Vector2)Camera.main.WorldToViewportPoint(s.transform.position))),"Irregular board stays within space between HUD and booster rail");
                yield return Capture("Gameplay-Level14");
                var button=hud.Find("Imported booster rail/Pause game").GetComponent<Button>();button.onClick.Invoke();
                yield return new WaitForSecondsRealtime(.6f);
                Check(GameObject.Find("CanvasGlobal").transform.Find("MenuPause").gameObject.activeInHierarchy,"Imported settings button opens existing pause menu");
                yield return Capture("Gameplay-Pause");MenuManager.Instance.HideAllCanvasElements();lm.gameStatus=GameState.Playing;
                yield return qa.Practice();qa.Visible=false;yield return qa.Execute("Match 3");yield return qa.Settled();
                Check(lm.gameStatus==GameState.Playing&&!lm.DragBlocked,"Match and refill remain playable with imported HUD");
                yield return Capture("Gameplay-SquareBoard");
                yield return qa.EndSession();Check(!RoyalSave.Testing,"Normal save restored");
                File.AppendAllText(Folder+"/validation.txt","ALL LAYOUT CHECKS PASSED\n");
            }
            finally {if(RoyalSave.Testing)RoyalThrone.Editor.RoyalRegressionRunner.Run(qa.EndSession());}
        }
    }
}
#endif

