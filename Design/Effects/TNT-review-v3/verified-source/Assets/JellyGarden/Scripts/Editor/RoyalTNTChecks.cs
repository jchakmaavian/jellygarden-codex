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
        const string Folder="Design/Effects/TNT-review-v3/";
        static void Capture(string name){var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Folder+name+".png",t.EncodeToPNG());Object.Destroy(t);}
        static IEnumerator Until(TNT3DAnimator actor,float age){while(actor&&actor.gameObject.activeSelf&&actor.VisualAge<age)yield return null;yield return new WaitForEndOfFrame();}
        public static IEnumerator Run(LevelManager lm,Action seed,Action colours,Action<bool,string> check)
        {
            Directory.CreateDirectory(Folder);float originalSpeed=Time.timeScale;
#if !ROYAL_DISABLE_QA
            var qa=RoyalThrone.Testing.RoyalQaService.Instance;bool enabled=qa&&qa.enabled;if(qa)qa.enabled=false;
#endif
            try
            {
                foreach(float speed in new[]{.25f,1f})
                {
                    string tag=speed<1?"quarter":"normal";Time.timeScale=speed;
                    RoyalPowerupDirector.Get.Cancel();CandyShatterService.Instance.Clear();
                    seed();yield return null;colours();yield return null;
                    var cell=lm.GetSquare(4,4);Object.DestroyImmediate(cell.item.gameObject);cell.item=null;
                    var item=cell.GenItem(false);item.NextType=ItemsTypes.PACKAGE;item.ChangeType();
                    var reveal=TNT3DAnimator.Find(item);
                    check(reveal&&!reveal.model.gameObject.activeSelf&&item.sprRenderer.enabled,tag+": original artwork appears immediately, mesh stays disabled");
                    yield return Until(reveal,.16f);
                    float peak=item.sprRenderer.bounds.size.x;
                    check(reveal.FlameIntensity>.7f&&!item.falling,tag+": strong rim glow during elastic reveal, no replacement fall delay");Capture(tag+"-reveal-peak");
                    yield return new WaitForSeconds(.30f);yield return new WaitForEndOfFrame();
                    check(!TNT3DAnimator.Find(item)&&item.sprRenderer.enabled&&item.sprRenderer.sprite==RoyalPieceSkin.For(ItemsTypes.PACKAGE,0)&&item.sprRenderer.bounds.size.x<peak,tag+": reveal rebounds into original idle sprite");Capture(tag+"-idle");
                    item.DestroyItem(true);yield return null;
                    var actor=Object.FindObjectsByType<TNT3DAnimator>(FindObjectsSortMode.None).First(a=>a.gameObject.activeSelf);
                    yield return Until(actor,.09f);Capture(tag+"-ignition");
                    while(actor.State!=TNT3DAnimator.VisualState.Explosion&&actor.State!=TNT3DAnimator.VisualState.Debris)yield return null;
                    yield return Until(actor,.12f);
                    var props=new MaterialPropertyBlock();actor.blast[0].GetPropertyBlock(props);var core=props.GetColor("_Color");actor.blast[1].GetPropertyBlock(props);var edge=props.GetColor("_Color");
                    check(core.b>.75f&&edge.b<.05f&&actor.blast[0].enabled&&actor.blast[3].enabled,tag+": white-hot ball, gold edge and expanding shockwave");Capture(tag+"-blast-peak");
                    yield return Until(actor,.23f);Capture(tag+"-rays");
                    yield return Until(actor,.40f);
                    check(CandyShatterService.Instance.ActiveFragments>0&&!actor.blast[0].enabled,tag+": colored debris remains visible after the center fades");Capture(tag+"-falling-debris");
                    yield return new WaitForSeconds(1.2f);
                    check(TNT3DAnimator.ActiveCount==0&&!RoyalPowerupDirector.Busy&&CandyShatterService.Instance.ActiveFragments==0,tag+": all pooled effects and debris finish");
                }
                check(TNT3DAnimator.Reuses>0,"Pooled TNT actors reset and replay without leftover reveal transforms");
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
