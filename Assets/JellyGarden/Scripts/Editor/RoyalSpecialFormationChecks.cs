using System;
using System.Collections;
using System.IO;
using System.Linq;
using JellyGarden.Scripts;
using JellyGarden.Scripts.Juice;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RoyalThrone.Editor
{
    public static class RoyalSpecialFormationChecks
    {
        public static IEnumerator Run(LevelManager lm, Action<bool,string> report, ItemsTypes type, string shape)
        {
            Action<bool,string> check = (ok, message) => report(ok, shape + ": " + message);
            string folder="Logs/port-checks/QaFeedback/07-SpecialFormation/reveals-restored/"+shape+"/raw/";
            Directory.CreateDirectory(folder);
            float oldCapture=Time.captureDeltaTime;
            try
            {
                foreach(int fps in new[]{30,60})
                {
                    if(fps==60)yield return RoyalThrone.Testing.RoyalQaService.Instance.Practice();
                    AI.THIS.StopAllCoroutines();AI.THIS.CancelVisibleHint();AI.THIS.allowShowTip=false;
                    foreach(var sq in lm.squaresArray){sq.item.SetColor(1+(sq.col+sq.row*2)%4);sq.item.currentType=sq.item.NextType=ItemsTypes.NONE;}
                    var cells = shape=="TNT-L" ? new[]{lm.GetSquare(2,2),lm.GetSquare(2,3),lm.GetSquare(2,4),lm.GetSquare(3,4),lm.GetSquare(4,4)} :
                        shape=="TNT-T" ? new[]{lm.GetSquare(2,2),lm.GetSquare(3,2),lm.GetSquare(4,2),lm.GetSquare(3,3),lm.GetSquare(3,4)} :
                        type==ItemsTypes.VERTICAL_STRIPPED ? new[]{lm.GetSquare(3,2),lm.GetSquare(3,3),lm.GetSquare(3,4),lm.GetSquare(3,5)} :
                        type==ItemsTypes.BOMB ? new[]{lm.GetSquare(1,3),lm.GetSquare(2,3),lm.GetSquare(3,3),lm.GetSquare(4,3),lm.GetSquare(5,3)} :
                        new[]{lm.GetSquare(2,3),lm.GetSquare(3,3),lm.GetSquare(4,3),lm.GetSquare(5,3)};
                    var items=cells.Select(s=>s.item).ToArray();
                    foreach(var item in items){item.SetColor(0);item.GetComponent<RoyalPieceSkin>().ApplyNow();}
                    var roots=items.Select(i=>i.transform.position).ToArray();
                    Time.captureDeltaTime=1f/fps;
                    lm.FindMatches();
                    bool sawFour=false,rootsStable=true,hidden=true,paused=false,fullSize=true,opaqueMerge=true,sawRevealStart=false,sawRevealAdvance=false;
                    var sizes=items.Select(i=>i.sprRenderer.bounds.size).ToArray();
                    int frame=0,gatherFrames=0,createdAtHandoff=-1;float firstDistance=-1,lastDistance=-1;
                    while(frame<fps*2)
                    {
                        yield return (UnityEngine.Application.isBatchMode?null:new WaitForEndOfFrame());
                        var gems=Object.FindObjectsByType<SpriteRenderer>().Where(s=>s.name=="Special gathering gem").ToArray();
                        if(gems.Length>0){
                            gatherFrames++;sawFour|=gems.Length==items.Length;
                            for(int i=0;i<items.Length;i++)if(items[i]){rootsStable&=Vector3.Distance(items[i].transform.position,roots[i])<.001f;hidden&=items[i].sprRenderer.forceRenderingOff;}
                            foreach(var gem in gems){
                                int index=Array.IndexOf(items,gem.GetComponentInParent<Item>());
                                fullSize &= index>=0 && Vector3.Distance(gem.bounds.size,sizes[index])<.001f;
                                opaqueMerge &= gem.color.a >= .99f;
                            }
                            float distance=0;foreach(var gem in gems)foreach(var other in gems)distance+=Vector3.Distance(gem.bounds.center,other.bounds.center);
                            if(firstDistance<0)firstDistance=distance;lastDistance=distance;
                            if(!paused&&gatherFrames==3){
                                paused=true;lm.gameStatus=GameState.Pause;
                                var positions=gems.Select(g=>g.transform.position).ToArray();
                                yield return null;yield return (UnityEngine.Application.isBatchMode?null:new WaitForEndOfFrame());
                                check(gems.Select((g,i)=>Vector3.Distance(g.transform.position,positions[i])<.0001f).All(v=>v),fps+" FPS: gathering pauses without movement");
                                lm.gameStatus=GameState.Playing;
                            }
                        }
                        foreach(var propeller in lm.GetItems().Where(i=>i&&i.currentType==type)){
                            var visual=propeller.GetComponent<RoyalThrone.ReferenceGameplay.ReferencePieceVisual>();
                            var nativeAnimator=propeller.GetComponentsInChildren<Animator>().FirstOrDefault(a=>a!=propeller.anim);
                            if(visual && nativeAnimator) {
                                var state=nativeAnimator.GetCurrentAnimatorStateInfo(0);
                                if(state.IsName(RoyalSpecialFormation.CreationState(type)) && visual.Revealing){
                                    if(createdAtHandoff<0)createdAtHandoff=lm.GetItems().Count(i=>i&&i.currentType==type);
                                    sawRevealStart |= state.normalizedTime < .15f;
                                    sawRevealAdvance |= state.normalizedTime > .15f && state.normalizedTime < 1;
                                }
                            }
                        }
                        if(fps==60&&frame<85&&!UnityEngine.Application.isBatchMode){var tex=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(folder+frame.ToString("D3")+".png",tex.EncodeToPNG());Object.Destroy(tex);}
                        frame++;
                    }
                    check(sawFour,fps+" FPS: all matched gems gather together");
                    check(fullSize,fps+" FPS: all matched gems retain their full original size throughout movement and merge");
                    check(opaqueMerge,fps+" FPS: merging gems remain fully visible until the original Reveal takes over");
                    check(sawRevealStart&&sawRevealAdvance,fps+" FPS: original creation Reveal starts at its beginning and advances normally");
                    check(lastDistance<firstDistance*.3f,fps+" FPS: gems converge into the compact reference stack before the creation flash");
                    check(rootsStable&&hidden,fps+" FPS: visual-only movement; original roots stay fixed and originals are hidden");
                    check(Mathf.Abs(gatherFrames/(float)fps-RoyalSpecialFormation.Duration)<=2f/fps,fps+" FPS: continuous gathering follows the reference duration before reveal");
                    check(items.All(i=>!i),fps+" FPS: all matched source items retire");
                    check(createdAtHandoff==1,fps+" FPS: the initial match creates exactly one playable special piece before later cascades");
                    check(!Object.FindObjectsByType<SpriteRenderer>().Any(s=>s.name=="Special gathering gem"),fps+" FPS: no gathering visuals remain");
                    check(lm.squaresArray.Where(s=>s&&s.item).All(s=>s.item.square==s),fps+" FPS: refill retains cell ownership");
                }
            }
            finally{Time.captureDeltaTime=oldCapture;}
        }
    }
}
