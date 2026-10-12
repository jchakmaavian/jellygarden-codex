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
    public static class RoyalMatchResponseChecks
    {
        public static IEnumerator Run(LevelManager lm, Action<bool,string> check, bool immediate)
        {
            string folder="Design/QaFeedback/08-MatchResponse/"+(immediate?"after/":"before/");
            Directory.CreateDirectory(folder);
            float capture=Time.captureDeltaTime;
            try
            {
                foreach(int fps in new[]{30,60})foreach(bool vertical in new[]{false,true})
                {
                    yield return RoyalThrone.Testing.RoyalQaService.Instance.Practice();
                    AI.THIS.StopAllCoroutines();AI.THIS.CancelVisibleHint();AI.THIS.allowShowTip=false;
                    foreach(var sq in lm.squaresArray){sq.item.SetColor(1+(sq.col+2*sq.row)%4);sq.item.currentType=sq.item.NextType=ItemsTypes.NONE;}
                    var source=lm.GetSquare(4,4).item;
                    var destination=lm.GetSquare(vertical?3:4,vertical?4:3);
                    var group=vertical?new[]{lm.GetSquare(3,2).item,lm.GetSquare(3,3).item,source}:
                        new[]{lm.GetSquare(2,3).item,lm.GetSquare(3,3).item,source};
                    foreach(var item in group){item.SetColor(0);item.GetComponent<RoyalPieceSkin>().ApplyNow();}
                    CandyShatterService.Instance.Clear();
                    Time.captureDeltaTime=1f/fps;
                    int moves=lm.Limit,score=LevelManager.Score;
                    string label=fps+" FPS "+(vertical?"vertical":"horizontal");
                    int commit=-1,first=-1,last=-1;
                    bool fragments=false,safe=true,credit=false;
                    check(source.CanAcceptInput && destination.item.CanAcceptInput,label+": valid input ready");
                    source.SwitchDirection(source.transform.position-destination.transform.position);
                    check(lm.InputSwaps==1,label+": actual player swap started");
                    for(int frame=0;frame<fps*2;frame++)
                    {
                        yield return new WaitForEndOfFrame();
                        if(lm.InputSwaps>0)safe&=group.All(i=>i&&!i.destroying&&i.sprRenderer.enabled);
                        else if(commit<0)commit=frame;
                        int hidden=group.Count(i=>!i||!i.sprRenderer.enabled);
                        if(hidden>0&&first<0)first=frame;
                        if(hidden==3&&last<0){last=frame;credit=LevelManager.Score==score+3*lm.scoreForItem;}
                        fragments|=CandyShatterService.Instance.ActiveFragments>0 || Object.FindObjectsByType<ParticleSystem>().Any(p=>p.particleCount>0 && p.GetComponentInParent<UnityEngine.Rendering.SortingGroup>() is var g && g && g.name.StartsWith("MatchItemExplodeParticles"));
                        if(fps==60&&!vertical&&frame<55){var tex=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(folder+frame.ToString("D3")+".png",tex.EncodeToPNG());Object.Destroy(tex);}
                    }
                    int delay=first-commit,spread=last-first;
                    check(first>=0&&commit>=0&&last>=0,label+": commit-to-first-break "+delay+" rendered frames; first-to-last "+spread+" frames");
                    if(immediate){check(delay>=0&&delay<=1,label+": break starts within one rendered frame of swap completion");check(spread<=1,label+": all three break together");}
                    check(safe,label+": no destruction while swap owns the pieces");
                    check(fragments,label+": painted break fragments remain visible");
                    check(credit,label+": exactly three gems credited at the initial clear");
                    check(lm.Limit==moves-1,label+": exactly one move consumed");
                    check(group.All(i=>!i),label+": cleared source items retire");
                    check(lm.squaresArray.Where(s=>s&&s.item).All(s=>s.item.square==s),label+": refill cell ownership remains consistent");
                    float deadline=Time.realtimeSinceStartup+8;
                    while(lm.IsResolvingMatches&&Time.realtimeSinceStartup<deadline)yield return null;
                    check(!lm.IsResolvingMatches,label+": resolver completes after the clear and refill");
                }
            }
            finally{Time.captureDeltaTime=capture;}
        }
    }
}
