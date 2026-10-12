using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JellyGarden.Scripts;
using JellyGarden.Scripts.Juice;
using RoyalThrone.ReferenceGameplay;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RoyalThrone.Editor
{
    // Exercise actual swaps: FindMatches alone does not cover the dragged-host delay.
    public static class RoyalFormationReferenceChecks
    {
        public static IEnumerator Run(LevelManager lm, Action<bool,string> report, bool referenceTiming)
        {
            string folder = "Design/QaFeedback/07-SpecialFormation/video-reference/" + (referenceTiming ? "after/" : "before/");
            Directory.CreateDirectory(folder);
            float capture = Time.captureDeltaTime;
            try
            {
                foreach (int fps in new[] {30,60}) foreach (bool vertical in new[] {true,false})
                {
                    yield return RoyalThrone.Testing.RoyalQaService.Instance.Practice();
                    AI.THIS.StopAllCoroutines(); AI.THIS.CancelVisibleHint(); AI.THIS.allowShowTip = false;
                    int[] backgroundColors={0,2,3,4};
                    foreach (var sq in lm.squaresArray) { sq.item.SetColor(backgroundColors[(sq.col+sq.row*2)%4]); sq.item.currentType=sq.item.NextType=ItemsTypes.NONE; }
                    var destination = lm.GetSquare(3,4);
                    var source = lm.GetSquare(vertical ? 4 : 3, vertical ? 4 : 5).item;
                    var group = vertical ? new[]{lm.GetSquare(3,2).item,lm.GetSquare(3,3).item,source,lm.GetSquare(3,5).item} :
                        new[]{lm.GetSquare(1,4).item,lm.GetSquare(2,4).item,source,lm.GetSquare(4,4).item};
                    foreach(var item in group) { item.SetColor(1); item.GetComponent<RoyalPieceSkin>().ApplyNow(); }
                    var sizes = group.Select(i=>i.sprRenderer.bounds.size).ToArray();
                    Time.captureDeltaTime=1f/fps;
                    int moves=lm.Limit, score=LevelManager.Score;
                    int commit=-1, gather=-1, lastGather=-1, reveal=-1, fall=-1;
                    bool fullSize=true, opaque=true, safe=true, owns=true, credit=false, revealMoving=false, revealStart=false, flashVisible=false, flashFollows=true;
                    Item created=null; Vector3 creationPosition=Vector3.zero;
                    var trace=new List<string>{"frame,commit,gatherCount,reveal,falling,x,y,creationNormalizedTime"};
                    string label=fps+" FPS "+(vertical?"vertical":"horizontal");
                    void Check(bool ok,string message)=>report(ok,label+": "+message);
                    source.SwitchDirection(source.transform.position-destination.transform.position);
                    Check(lm.InputSwaps==1,"actual player swap started");
                    for(int frame=0;frame<fps*2;frame++)
                    {
                        yield return new WaitForEndOfFrame();
                        if(lm.InputSwaps>0) safe &= group.All(i=>i&&!i.destroying);
                        else if(commit<0)commit=frame;
                        var gems=Object.FindObjectsByType<SpriteRenderer>().Where(s=>s.name=="Special gathering gem").ToArray();
                        if(gems.Length>0) {
                            if(gather<0)gather=frame;lastGather=frame;
                            foreach(var gem in gems) {
                                int index=Array.IndexOf(group,gem.GetComponentInParent<Item>());
                                fullSize &= index>=0 && Vector3.Distance(gem.bounds.size,sizes[index])<.001f;
                                opaque &= gem.color.a>=.99f;
                            }
                        }
                        if(!created) created=lm.GetItems().FirstOrDefault(i=>i&&RoyalSpecialFormation.Supported(i.currentType));
                        float normalized=-1;
                        var visual=created?created.GetComponent<ReferencePieceVisual>():null;
                        if(created) {
                            var flash=created.GetComponentInChildren<RoyalSpecialCreationFlash>();
                            if(flash) {
                                flashVisible |= flash.GetComponentsInChildren<SpriteRenderer>().Any(s=>s.sprite&&s.color.a>.8f);
                                flashFollows &= Vector2.Distance(flash.transform.position,created.transform.position)<.001f;
                            }
                            var animator=created.GetComponentsInChildren<Animator>().FirstOrDefault(a=>a!=created.anim);
                            if(animator) normalized=animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
                            if(reveal<0){reveal=frame;creationPosition=created.transform.position;credit=LevelManager.Score==score+4*lm.scoreForItem;revealStart=visual&&visual.Revealing&&normalized<.15f;}
                            if(Vector2.Distance(created.transform.position,creationPosition)>.01f) {
                                if(fall<0)fall=frame;
                                revealMoving |= visual&&visual.Revealing&&normalized>.03f&&normalized<1;
                            }
                        }
                        owns &= lm.squaresArray.Where(s=>s&&s.item).All(s=>s.item.square==s);
                        trace.Add($"{frame},{commit},{gems.Length},{(visual&&visual.Revealing)},{(created&&created.falling)},{(created?created.transform.position.x:0):F4},{(created?created.transform.position.y:0):F4},{normalized:F4}");
                        if(fps==60&&vertical&&frame<85) {var tex=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(folder+frame.ToString("D3")+".png",tex.EncodeToPNG());Object.Destroy(tex);}
                    }
                    File.WriteAllLines(folder+fps+"-"+(vertical?"V":"H")+".csv",trace);
                    Check(gather>=commit&&reveal>gather,"commit="+commit+", gather="+gather+", reveal="+reveal+", first fall="+fall);
                    Check(fullSize&&opaque,"gems retain full size and opacity throughout convergence");
                    Check(reveal-lastGather==1&&revealStart,"original Reveal starts immediately after the last merge frame");
                    Check(safe&&owns,"swap and refill cell ownership remain consistent");
                    Check(credit&&lm.Limit==moves-1,"four gems credited and exactly one move consumed");
                    Check(group.All(i=>!i)&&created,"matched sources retired and playable rocket survives");
                    Check(!Object.FindObjectsByType<SpriteRenderer>().Any(s=>s.name=="Special gathering gem"),"merge proxies cleaned up");
                    if(referenceTiming) {
                        Check(created&&created.currentType==(vertical?ItemsTypes.HORIZONTAL_STRIPPED:ItemsTypes.VERTICAL_STRIPPED),"rocket orientation follows the creating swap, as in the video");
                        Check(flashVisible&&flashFollows,"creation flash is visible and follows the rocket through gravity");
                        Check(!Object.FindAnyObjectByType<RoyalSpecialCreationFlash>(),"creation flash cleans up after settling");
                        Check(Mathf.Abs((reveal-gather)/(float)fps-1f/6f)<=1.1f/fps,"convergence matches the approximately 0.17-second video interval");
                        if(vertical) {Check(fall-reveal>=0&&(fall-reveal)/(float)fps<=.085f,"gravity begins within the video's brief Reveal lead-in");Check(revealMoving,"native Reveal advances while the rocket falls");}
                    }
                    float deadline=Time.realtimeSinceStartup+8;
                    while(lm.IsResolvingMatches&&Time.realtimeSinceStartup<deadline)yield return null;
                    Check(!lm.IsResolvingMatches,"resolver completes");
                }
            }
            finally { Time.captureDeltaTime=capture; }
        }
    }
}
