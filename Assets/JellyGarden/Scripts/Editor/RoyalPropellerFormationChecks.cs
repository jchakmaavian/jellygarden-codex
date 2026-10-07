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
    public static class RoyalPropellerFormationChecks
    {
        public static IEnumerator Run(LevelManager lm, Action<bool,string> check)
        {
            const string folder="Design/QaFeedback/06-PropellerFormation/raw/";
            Directory.CreateDirectory(folder);
            float oldCapture=Time.captureDeltaTime;
            try
            {
                foreach(int fps in new[]{30,60})
                {
                    if(fps==60)yield return RoyalThrone.Testing.RoyalQaService.Instance.Practice();
                    AI.THIS.StopAllCoroutines();AI.THIS.CancelVisibleHint();AI.THIS.allowShowTip=false;
                    foreach(var sq in lm.squaresArray){sq.item.SetColor(1+(sq.col+sq.row*2)%4);sq.item.currentType=sq.item.NextType=ItemsTypes.NONE;}
                    var cells=new[]{lm.GetSquare(3,3),lm.GetSquare(4,3),lm.GetSquare(3,4),lm.GetSquare(4,4)};
                    var items=cells.Select(s=>s.item).ToArray();
                    foreach(var item in items){item.SetColor(0);item.GetComponent<RoyalPieceSkin>().ApplyNow();}
                    var roots=items.Select(i=>i.transform.position).ToArray();
                    Time.captureDeltaTime=1f/fps;
                    lm.FindMatches();
                    bool sawFour=false,rootsStable=true,hidden=true,paused=false,fullSize=true,fadeAfterMerge=true,sawBlend=false,noPop=true;
                    var sizes=items.Select(i=>i.sprRenderer.bounds.size).ToArray();
                    int frame=0,gatherFrames=0;float firstDistance=-1,lastDistance=-1;
                    while(frame<fps*2)
                    {
                        yield return new WaitForEndOfFrame();
                        var gems=Object.FindObjectsByType<SpriteRenderer>().Where(s=>s.name=="Propeller gathering gem").ToArray();
                        if(gems.Length>0){
                            gatherFrames++;sawFour|=gems.Length==4;
                            for(int i=0;i<4;i++)if(items[i]){rootsStable&=Vector3.Distance(items[i].transform.position,roots[i])<.001f;hidden&=items[i].sprRenderer.forceRenderingOff;}
                            foreach(var gem in gems){
                                int index=Array.IndexOf(items,gem.GetComponentInParent<Item>());
                                fullSize &= index>=0 && Vector3.Distance(gem.bounds.size,sizes[index])<.001f;
                                if(gem.color.a<.99f)fadeAfterMerge &= gems.All(other=>Vector3.Distance(gem.bounds.center,other.bounds.center)<.001f);
                            }
                            sawBlend |= Object.FindObjectsByType<Transform>().Any(t=>t.name=="Propeller merge result") && gems.Any(g=>g.color.a>0 && g.color.a<1);
                            float distance=0;foreach(var gem in gems)foreach(var other in gems)distance+=Vector3.Distance(gem.bounds.center,other.bounds.center);
                            if(firstDistance<0)firstDistance=distance;lastDistance=distance;
                            if(!paused&&gatherFrames==3){
                                paused=true;lm.gameStatus=GameState.Pause;
                                var positions=gems.Select(g=>g.transform.position).ToArray();
                                yield return null;yield return new WaitForEndOfFrame();
                                check(gems.Select((g,i)=>Vector3.Distance(g.transform.position,positions[i])<.0001f).All(v=>v),fps+" FPS: gathering pauses without movement");
                                lm.gameStatus=GameState.Playing;
                            }
                        }
                        foreach(var propeller in lm.GetItems().Where(i=>i&&i.currentType==ItemsTypes.HELICOPTER)){
                            var visual=propeller.GetComponent<RoyalThrone.ReferenceGameplay.ReferencePieceVisual>();
                            var nativeAnimator=propeller.GetComponentsInChildren<Animator>().FirstOrDefault(a=>a!=propeller.anim);
                            noPop &= visual && nativeAnimator && !(nativeAnimator.GetCurrentAnimatorStateInfo(0).IsName("PropellerCreationAnimation") && nativeAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime<.99f);
                        }
                        if(fps==60&&frame<85){var tex=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(folder+frame.ToString("D3")+".png",tex.EncodeToPNG());Object.Destroy(tex);}
                        frame++;
                    }
                    check(sawFour,fps+" FPS: all four matched gems gather together");
                    check(fullSize,fps+" FPS: all four gems retain their full original size throughout movement and merge");
                    check(fadeAfterMerge,fps+" FPS: transformation begins only after all four gems reach the same location");
                    check(sawBlend&&noPop,fps+" FPS: merged gems blend into the full-size Propeller without replaying its pop reveal");
                    check(lastDistance<firstDistance*.2f,fps+" FPS: gems converge into one location");
                    check(rootsStable&&hidden,fps+" FPS: visual-only movement; original roots stay fixed and originals are hidden");
                    check(gatherFrames>=fps*.20f,fps+" FPS: continuous gathering frames precede reveal");
                    check(items.All(i=>!i),fps+" FPS: all four matched source items retire");
                    check(lm.GetItems().Count(i=>i&&i.currentType==ItemsTypes.HELICOPTER)==1,fps+" FPS: exactly one playable Propeller is created");
                    check(!Object.FindObjectsByType<SpriteRenderer>().Any(s=>s.name=="Propeller gathering gem"),fps+" FPS: no gathering visuals remain");
                    check(lm.squaresArray.Where(s=>s&&s.item).All(s=>s.item.square==s),fps+" FPS: refill retains cell ownership");
                }
            }
            finally{Time.captureDeltaTime=oldCapture;}
        }
    }
}
