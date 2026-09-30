using PlayerPrefs = RoyalThrone.Testing.RoyalSave;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JellyGarden.Scripts;
using JellyGarden.Scripts.GUI;
using JellyGarden.Scripts.Targets;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalThrone.Editor
{
    public static class RoyalPresentationChecks
    {
        const string Folder="Design/Effects/Unity-review-v2/";
        [MenuItem("Tools/Royal Throne/Effects/Review dialogs without purchases %#F8")]
        static void Review(){if(EditorApplication.isPlaying&&LevelManager.THIS&&LevelManager.THIS.gameStatus==GameState.Map)LevelManager.THIS.StartCoroutine(Run());}
        static IEnumerator Run()
        {
            Directory.CreateDirectory(Folder);int gems=InitScript.Gems,lives=InitScript.lifes;var result=new List<string>();
            // Opening the card writes OpenLevel; the player's value (or its absence) is put back at the end.
            bool hadLevel=PlayerPrefs.HasKey("OpenLevel");int openLevel=PlayerPrefs.GetInt("OpenLevel",1);
            var manager=MenuManager.Instance;var root=manager.GetCanvasGlobal();
            InitScript.Instance.TryOpenLevel(Mathf.Max(1,PlayerPrefs.GetInt("OpenLevel",1)));manager.menuPlay.SetActive(false);
            foreach(string name in new[]{"MenuPlay","GemsShop","LiveShop","BoostShop","BoostInfo","Settings","Tutorial","MenuComplete","MenuFailed","PreFailed"}){
                var menu=root.Find(name);if(!menu){result.Add("FAIL missing "+name);continue;}
                if(name=="BoostShop")menu.GetComponent<BoostShop>().SetBoost(BoostType.Stripes);else menu.gameObject.SetActive(true);
                yield return new WaitForSecondsRealtime(name=="PreFailed"?2.2f:.85f);yield return new WaitForEndOfFrame();
                var tex=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Folder+name+".png",tex.EncodeToPNG());Object.Destroy(tex);
                var close=menu.Find("Image/Close");var title=menu.Find(name=="PreFailed"?"Banner/Royal heading":"Image/Royal heading");
                // The ribbon cards (Level Failed, How to Play) carry their title on the ribbon.
                if(!title)title=menu.Find("Image/Royal ribbon/Royal heading");
                bool headerSafe=title&&(!close||!Overlap((RectTransform)close,(RectTransform)title));
                result.Add((headerSafe?"PASS ":"FAIL ")+name+(close?" heading clears close control":" heading visible; dismissal uses primary action"));
                var icons=menu.GetComponentsInChildren<TargetIcon>();
                foreach(var icon in icons)if(title&&Overlap((RectTransform)title,(RectTransform)icon.transform))result.Add("FAIL "+name+" goal overlaps heading");
                for(int i=1;i<icons.Length;i++)result.Add((!Overlap(icons[i-1].textObj.rectTransform,icons[i].textObj.rectTransform)?"PASS ":"FAIL ")+name+" target counts do not overlap");
                foreach(var button in menu.GetComponentsInChildren<Button>()){
                    if(!button.interactable)continue;var r=(RectTransform)button.transform;var corners=new Vector3[4];r.GetWorldCorners(corners);
                    if(corners[0].x<0||corners[2].x>Screen.width||corners[0].y<0||corners[2].y>Screen.height)result.Add("FAIL "+name+" control offscreen "+button.name);
                }
                // Each lesson (RoyalLessonDemo) is a small board playing its lesson on a loop: it must be playing, and its board
                // must keep clear of the lesson's title and rule.
                if(name=="Tutorial")for(int page=1;page<RoyalThrone.Presentation.RoyalLessonDemo.Count;page++){
                    menu.GetComponent<Tutorial>().Next();yield return null;yield return new WaitForEndOfFrame();
                    var lesson=menu.Find("Image/Royal lesson "+page);var stage=lesson?lesson.Find("Stage"):null;
                    var demo=stage?stage.GetComponent<RoyalThrone.Presentation.RoyalLessonDemo>():null;var board=stage?stage.Find("Board") as RectTransform:null;
                    var ruleText=lesson?lesson.Find("Rule") as RectTransform:null;var titleText=lesson?lesson.Find("Title") as RectTransform:null;
                    bool clear=demo&&demo.Playing&&board&&ruleText&&titleText&&!Overlap(board,ruleText)&&!Overlap(board,titleText);
                    result.Add((clear?"PASS ":"FAIL ")+"Tutorial page "+page+" plays its lesson clear of its title and rule");
                    var shot=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Folder+"Tutorial-"+page+".png",shot.EncodeToPNG());Object.Destroy(shot);
                }
                // End-screen dismissal normally advances progress; visual review must not invoke it.
                if(name=="MenuComplete"||name=="MenuFailed"||name=="PreFailed")menu.gameObject.SetActive(false);
                else menu.GetComponent<AnimationManager>().CloseMenu();yield return null;
            }
            result.Add((InitScript.Gems==gems&&InitScript.lifes==lives?"PASS ":"FAIL ")+"Dialog review preserves gems and lives; no purchase callbacks invoked");
            if(hadLevel)PlayerPrefs.SetInt("OpenLevel",openLevel);else PlayerPrefs.DeleteKey("OpenLevel");PlayerPrefs.Save();
            File.WriteAllLines(Folder+"dialog-checks.txt",result);Debug.Log("[Royal Presentation Checks] "+string.Join("; ",result));
        }
        static bool Overlap(RectTransform a,RectTransform b){var ac=new Vector3[4];var bc=new Vector3[4];a.GetWorldCorners(ac);b.GetWorldCorners(bc);return new Rect(ac[0],ac[2]-ac[0]).Overlaps(new Rect(bc[0],bc[2]-bc[0]));}
    }
}
