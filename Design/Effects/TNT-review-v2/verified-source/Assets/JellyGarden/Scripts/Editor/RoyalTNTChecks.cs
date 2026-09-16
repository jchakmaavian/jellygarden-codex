using System;
using System.Collections;
using System.IO;
using System.Linq;
using JellyGarden.Scripts;
using JellyGarden.Scripts.Juice;
using UnityEngine;
using Object=UnityEngine.Object;
namespace RoyalThrone.Editor
{
    public static class RoyalTNTChecks
    {
        const string Folder="Design/Effects/TNT-review-v2/";
        static void Capture(string name){var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Folder+name+".png",t.EncodeToPNG());Object.Destroy(t);}
        public static IEnumerator Run(LevelManager lm,Action seed,Action colours,Action<bool,string> check)
        {
            Directory.CreateDirectory(Folder);float originalSpeed=Time.timeScale;
#if !ROYAL_DISABLE_QA
            var qa=RoyalThrone.Testing.RoyalQaService.Instance;bool enabled=qa&&qa.enabled;if(qa)qa.enabled=false;
#endif
            try
            {
                Time.timeScale=.5f;var fx=RoyalPowerupDirector.Get;fx.Cancel();CandyShatterService.Instance.Clear();
                seed();yield return null;colours();yield return null;
                var cell=lm.GetSquare(4,4);var item=cell.item;
                item.NextType=ItemsTypes.PACKAGE;item.ChangeType();yield return new WaitForSeconds(.15f);yield return new WaitForEndOfFrame();
                check(item.currentType==ItemsTypes.PACKAGE&&item.sprRenderer.enabled&&item.sprRenderer.sprite==RoyalPieceSkin.For(ItemsTypes.PACKAGE,0),"Original TNT sprite is visible after creation");
                check(!TNT3DAnimator.Find(item),"No mesh TNT is attached to the board sprite");Capture("sprite-restored");
                item.DestroyItem(true);yield return new WaitForSeconds(.10f);yield return new WaitForEndOfFrame();
                var actor=Object.FindObjectsByType<TNT3DAnimator>(FindObjectsSortMode.None).First(a=>a.gameObject.activeSelf);
                check(!actor.model.gameObject.activeSelf,"Activation does not show the rejected mesh model");
                yield return new WaitForSeconds(.155f);yield return new WaitForEndOfFrame();
                var props=new MaterialPropertyBlock();actor.blast[0].GetPropertyBlock(props);var core=props.GetColor("_Color");
                actor.blast[1].GetPropertyBlock(props);var edge=props.GetColor("_Color");
                check(core.r>core.b+.5f&&edge.r>edge.b+.7f,"Expanded blast has yellow-white core and gold outer color");Capture("gold-yellow-white-peak");
                yield return new WaitForSeconds(1.6f);
                check(!actor.gameObject.activeSelf&&!RoyalPowerupDirector.Busy&&CandyShatterService.Instance.ActiveFragments==0,"Preserved explosion and debris clean up");
            }
            finally
            {
                Time.timeScale=originalSpeed;
#if !ROYAL_DISABLE_QA
                if(qa)qa.enabled=enabled;
#endif
            }
        }
    }
}
