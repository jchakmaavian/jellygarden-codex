using System;
using System.Collections;
using System.IO;
using System.Linq;
using JellyGarden.Scripts;
using JellyGarden.Scripts.Juice;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace RoyalThrone.Editor
{
    public static class RoyalTNTChecks
    {
        const string Folder="Design/Effects/TNT-review-v1/";
        static void Capture(string name){var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Folder+name+".png",t.EncodeToPNG());Object.Destroy(t);}
        public static IEnumerator Run(LevelManager lm,Action seed,Action colours,Action<bool,string> check)
        {
            Directory.CreateDirectory(Folder);float originalSpeed=Time.timeScale;
            var tutorial=Object.FindFirstObjectByType<JellyGarden.Scripts.GUI.TutorialStuff.TutorialManager>();
            var overlay=tutorial&&tutorial.tutorial?tutorial.tutorial.GetComponentsInChildren<Graphic>(true).Where(g=>g.enabled).ToArray():Array.Empty<Graphic>();
            foreach(var graphic in overlay)graphic.enabled=false;
#if !ROYAL_DISABLE_QA
            var qa=RoyalThrone.Testing.RoyalQaService.Instance;bool enabled=qa&&qa.enabled;if(qa)qa.enabled=false;
#endif
            try
            {
                foreach(float speed in new[]{.5f,1f})
                {
                    Time.timeScale=speed;var fx=RoyalPowerupDirector.Get;fx.Cancel();CandyShatterService.Instance.Clear();
                    seed();yield return null;colours();yield return null;
                    // Reserve blue for this T shape; avoid the legacy sixth-color atlas.
                    foreach(var sq in lm.squaresArray){int c=(sq.col+sq.row*2)%4;sq.item.SetColor(c==0?0:c+1);}
                    string label="tnt-"+speed.ToString("F1",System.Globalization.CultureInfo.InvariantCulture);
                    var cell=lm.GetSquare(4,4);var old=cell.item;
                    foreach(var p in new[]{new Vector2Int(3,4),new Vector2Int(4,4),new Vector2Int(5,4),new Vector2Int(4,3),new Vector2Int(4,2)})lm.GetSquare(p.x,p.y).item.SetColor(1);
                    var previousDragged=lm.lastDraggedItem;var previousSwitched=lm.lastSwitchedItem;
                    lm.lastDraggedItem=old;lm.lastSwitchedItem=null;
                    var group=new JellyGarden.Scripts.System.CombineManager().GetCombine().Single(g=>g.Contains(old)).Distinct().ToList();
                    lm.lastDraggedItem=previousDragged;lm.lastSwitchedItem=previousSwitched;
                    check(group.Count==5&&old.NextType==ItemsTypes.PACKAGE,label+": real T-match creates TNT");
                    float start=Time.time;foreach(var matched in group){matched.royalMatchSize=5;matched.DestroyItem(true);}
                    TNT3DAnimator actor=null;float first=-1;bool peak=false;float maxLift=0,maxFuse=0;TNT3DAnimator originalActor=null;
                    while(Time.time-start<1.5f)
                    {
                        yield return new WaitForEndOfFrame();if(!cell.item||cell.item==old)continue;actor=TNT3DAnimator.Find(cell.item);if(!actor)continue;
                        if(first<0){first=Time.time;originalActor=actor;}
                        maxLift=Mathf.Max(maxLift,actor.HeadLift);maxFuse=Mathf.Max(maxFuse,Quaternion.Angle(actor.fuse.localRotation,Quaternion.identity));
                        if(!peak&&actor.FlameIntensity>.8f){Capture(label+"-reveal-peak");peak=true;}
                        if(actor.State==TNT3DAnimator.VisualState.Idle&&Time.time-first>.2f)break;
                    }
                    check(actor&&peak&&maxLift>.12f&&maxFuse>3,label+": independent head lift, delayed fuse motion and head flame");
                    check(first-start<.4f&&Time.time-first<=.56f,label+": immediate reveal settles within 0.55 seconds");
                    check(actor.State==TNT3DAnimator.VisualState.Idle&&actor.FlameIntensity==0&&!cell.item.sprRenderer.enabled,label+": mesh Idle replaces sprite and clears reveal effects");
                    check(actor.model.localScale==Vector3.one&&Mathf.Abs(actor.HeadLift)<.001f,label+": idle body/head transforms reset");
                    Capture(label+"-idle");
                    int reuse=TNT3DAnimator.Reuses;cell.item.DestroyItem(true);float launch=Time.time;bool ignition=false,blast=false,debris=false;int fragments=0;
                    while(Time.time-launch<1.25f)
                    {
                        yield return new WaitForEndOfFrame();float t=Time.time-launch;
                        if(!ignition&&actor.State==TNT3DAnimator.VisualState.Ignition){Capture(label+"-ignition");ignition=true;}
                        if(!blast&&t>.255f&&t<.38f){Capture(label+"-explosion-peak");blast=actor.blast.Any(r=>r.enabled);}
                        fragments=Mathf.Max(fragments,CandyShatterService.Instance.ActiveFragments);
                        if(!debris&&t>.57f){Capture(label+"-falling-debris");debris=true;}
                    }
                    check(ignition&&blast&&fragments>=100,label+": ignition, white/gold blast and dense pooled cell fragments (peak "+fragments+")");
                    check(!actor.gameObject.activeSelf&&!RoyalPowerupDirector.Busy&&CandyShatterService.Instance.ActiveFragments==0,label+": explosion and fragments clean up");
                    var item=cell.GenItem(false);yield return null;item.currentType=ItemsTypes.PACKAGE;item.falling=false;item.transform.position=cell.transform.position;
                    var reused=TNT3DAnimator.Attach(item,true);yield return new WaitForSeconds(.55f);
                    check(reused&&reused==originalActor&&TNT3DAnimator.Reuses>reuse&&reused.State==TNT3DAnimator.VisualState.Idle&&reused.FlameIntensity==0,label+": same pooled TNT reuses cleanly");
                    reused.Cleanup();Object.Destroy(item.gameObject);yield return null;
                    var cancelled=TNT3DAnimator.PreviewAt(cell.transform.position);cancelled.Activate();fx.Cancel();
                    check(TNT3DAnimator.ActiveCount==0,label+": cancellation releases an igniting TNT without stale effects");
                }
            }
            finally
            {
                Time.timeScale=originalSpeed;foreach(var g in overlay)if(g)g.enabled=true;
#if !ROYAL_DISABLE_QA
                if(qa)qa.enabled=enabled;
#endif
            }
        }
    }
}
