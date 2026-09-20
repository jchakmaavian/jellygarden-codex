using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JellyGarden.Scripts;
using JellyGarden.Scripts.Juice;
using UnityEngine;
using Object=UnityEngine.Object;
namespace RoyalThrone.Editor
{
    public static class RoyalRocketChecks
    {
        const string Folder="Design/Effects/Rocket-review-v2/";
        public static IEnumerator Run(LevelManager lm,Action seed,Action colours,Action<bool,string> check)
        {
            yield return RoyalVfxLab.ReviewRocketSpeeds(speed=>AtSpeed(lm,seed,colours,check,speed));
        }
        static IEnumerator AtSpeed(LevelManager lm,Action seed,Action colours,Action<bool,string> check,float speed)
        {
            Directory.CreateDirectory(Folder);
            check(Mathf.Approximately(Time.timeScale,speed),"VFX Lab uses actual game speed "+speed);
            var fx=RoyalPowerupDirector.Get;
            for(int scenario=0;scenario<5;scenario++)
            {
                fx.Cancel();lm.StopAllCoroutines();AI.THIS.StopAllCoroutines();AI.THIS.allowShowTip=false;lm.DragBlocked=true;CandyShatterService.Instance.Clear();seed();yield return null;colours();yield return null;
                bool vertical=scenario==1;string name=(vertical?"vertical":scenario==2?"blockers":scenario==3?"chain":scenario==4?"cascade":"horizontal")+"-"+speed;
                Item At(int x,int y)=>lm.GetSquare(x,y).item;
                var origin=At(3,3);origin.currentType=vertical?ItemsTypes.VERTICAL_STRIPPED:ItemsTypes.HORIZONTAL_STRIPPED;
                Square blocked=null;
                if(scenario==2)
                {
                    blocked=lm.GetSquare(6,3);Object.DestroyImmediate(blocked.item.gameObject);blocked.item=null;
                    var go=Object.Instantiate(Resources.Load<GameObject>("Prefabs/WireBlock"),blocked.transform);
                    go.transform.localPosition=Vector3.zero;blocked.block.Add(go);blocked.type=SquareTypes.WIREBLOCK;
                }
                if(scenario==3)At(5,3).currentType=ItemsTypes.VERTICAL_STRIPPED;
                yield return null;
                var beforeItems=lm.squaresArray.Where(s=>s&&s.item).ToDictionary(s=>s,s=>s.item);
                int sparkStart=fx.RocketSparkParticlesEmitted;
                int before=fx.RocketCellImpacts,launches=fx.RocketLaunches,objects=fx.GetComponentsInChildren<Transform>(true).Length;
                float started=Time.time,until=Time.realtimeSinceStartup+20,nextCapture=.10f;int frame=0,inspected=before,peakParticles=0,peakExhaust=0;bool leads=true,untouched=true,correctArt=true;
                origin.DestroyItem(true);
                if(scenario==4)lm.FindMatches();
                do
                {
                    yield return new WaitForEndOfFrame();
                    peakParticles=Mathf.Max(peakParticles,fx.RocketParticleCount);
                    peakExhaust=Mathf.Max(peakExhaust,fx.RocketExhaustCount);
                    foreach(var sr in fx.GetComponentsInChildren<SpriteRenderer>())
                        if(sr.enabled&&sr.gameObject.name.StartsWith("Projectile"))
                            correctArt &= sr.sprite&&sr.sprite.name.StartsWith("RoyalRocket_") &&
                                (sr.sprite.texture==RoyalGameplayArt.Art("rocket").texture || sr.sprite.texture==RoyalGameplayArt.Art("rocket-vertical").texture);
                    if(scenario<3)
                    {
                        var impacted=new HashSet<Vector3>();for(int n=before;n<fx.RocketCellImpacts;n++)impacted.Add(fx.RocketImpactPositions[n%512]);
                        foreach(var entry in beforeItems)
                            if(entry.Value!=origin&&!impacted.Contains(entry.Key.transform.position)&&entry.Key!=blocked)
                                untouched &= entry.Key.item==entry.Value && entry.Value && !entry.Value.destroying;
                    }
                    for(int n=inspected;n<fx.RocketCellImpacts;n++)
                    {
                        var pos=fx.RocketImpactPositions[n%512];float cell=Mathf.Abs(lm.squareWidth*lm.GameField.lossyScale.x);
                        if(scenario<3)leads &= fx.GetComponentsInChildren<SpriteRenderer>().Any(sr=>sr.enabled&&sr.gameObject.name.StartsWith("Projectile")&&Vector2.Distance(sr.transform.position,pos)<cell*.8f);
                    }
                    inspected=fx.RocketCellImpacts;
                    if(Time.time-started>=nextCapture && frame<18)
                    {var tex=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Folder+name+"-"+frame.ToString("D2")+".png",tex.EncodeToPNG());Object.Destroy(tex);frame++;nextCapture+=.09f;}
                }while((RoyalPowerupDirector.Busy||lm.IsResolvingMatches)&&Time.realtimeSinceStartup<until);
                // The resolver restarts its idle checker. Hold this disposable board
                // while particle tails expire, so it cannot start a shuffle/banner.
                if(!RoyalPowerupDirector.Busy&&!lm.IsResolvingMatches)
                {lm.DragBlocked=true;lm.StopAllCoroutines();AI.THIS.StopAllCoroutines();}
                yield return new WaitForSeconds(1f);
                check(!RoyalPowerupDirector.Busy&&!lm.IsResolvingMatches,name+": rocket/chains/cascades settle");
                check(fx.LastRocketSweepDuration>=.8f&&fx.LastRocketSweepDuration<=1.2f,name+": sweep lasts0.8-1.2 game seconds");
                check(fx.RocketImpactTimes[before%512]-started>=.15f,name+": visible launch before first contact");
                check(fx.RocketCellImpacts-before>=8,name+": travel reaches every row/column cell");
                for(int n=before;n<fx.RocketCellImpacts;n++)
                    check(fx.RocketBreakTimes[n%512]>=fx.RocketImpactTimes[n%512]+.05f,name+": cell"+(n-before)+" white impact precedes mutation");
                if(scenario<3){check(leads,name+": physical rocket head leads every crossed cell");check(untouched,name+": cells remain intact until reached");}
                if(scenario==0||scenario==1)
                {
                    for(int n=0;n<8;n++)check(!(vertical?lm.GetSquare(3,n).item:lm.GetSquare(n,3).item),name+": intended cell cleared"+n);
                    check(lm.GetItems().Count(i=>i)==56,name+": unrelated cells stay intact");
                }
                if(blocked)check(blocked.block.Count==0,name+": crossed blocker removed by existing service");
                if(scenario==3)check(fx.RocketLaunches-launches==2,name+": chained rocket activates once");
                if(scenario==4)check(lm.GetItems().Where(i=>i).All(i=>!i.destroying&&!i.falling&&i.square.item==i),name+": cascade restores valid ownership");
                check(fx.RocketUsesCustomVfx,name+": custom rocket shader and four fixed emitters replace toolkit effects");
                check(correctArt,name+": moving heads preserve supplied rocket textures");
                check(peakExhaust>=2,name+": both heads show animated fire exhaust");
                check(fx.RocketExhaustCount==0,name+": fire exhaust releases with projectile pool");
                check(fx.RocketSparkParticlesEmitted-sparkStart>=160,name+": dense spatial star trail emits along travel");
                check(peakParticles>=70,name+": layered custom effects are visible during sweep");
                check(fx.GetComponentsInChildren<Transform>(true).Length==objects,name+": no additional projectile or VFX objects allocated");
                check(CandyShatterService.Instance.ActiveFragments==0,name+": debris pool cleans up");
                check(fx.RocketParticleCount==0,name+": trail sparks fade after exit");
            }
            // Exercise the exact visual entry point used by QA and the Effects Workshop.
            for(int direction=0;direction<2;direction++)
            {
                fx.Cancel();lm.StopAllCoroutines();AI.THIS.StopAllCoroutines();lm.DragBlocked=true;
                CandyShatterService.Instance.Clear();seed();yield return null;colours();yield return null;
                var items=lm.squaresArray.ToDictionary(sq=>sq,sq=>sq.item);
                int moves=lm.Limit,score=LevelManager.Score,coins=InitScript.Gems,lives=InitScript.lifes;
                int objects=fx.GetComponentsInChildren<Transform>(true).Length;
                string effect=direction==0?"Rocket":"Rocket V",name="preview-"+direction+"-"+speed;
                for(int replay=0;replay<2;replay++)
                {
                    int launch=fx.RocketLaunches;float start=Time.time;int exhaust=0;bool captured=false;
                    fx.Preview(effect,lm.GetSquare(3,3).transform.position);
                    while(Time.time-start<2.1f)
                    {
                        yield return new WaitForEndOfFrame();exhaust=Mathf.Max(exhaust,fx.RocketExhaustCount);
                        if(!captured&&Time.time-start>.68f)
                        {var tex=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Folder+name+"-"+replay+".png",tex.EncodeToPNG());Object.Destroy(tex);captured=true;}
                    }
                    check(fx.RocketLaunches==launch+1&&exhaust==2,name+": replay "+replay+" uses the two-headed fire sequence");
                    check(items.All(pair=>pair.Value&&pair.Key.item==pair.Value&&!pair.Value.destroying),name+": preview preserves every gem");
                    check(lm.Limit==moves&&LevelManager.Score==score&&InitScript.Gems==coins&&InitScript.lifes==lives,name+": preview grants no rewards or moves");
                    check(!RoyalPowerupDirector.Busy&&fx.RocketExhaustCount==0&&fx.RocketParticleCount==0,name+": replay returns all effects to pool");
                    check(fx.GetComponentsInChildren<Transform>(true).Length==objects,name+": replay does not allocate new effect objects");
                }
            }
        }
    }
}
