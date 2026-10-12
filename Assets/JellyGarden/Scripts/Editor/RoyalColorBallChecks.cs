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
    public static class RoyalColorBallChecks
    {
        const string Folder="Logs/port-checks/Effects/ColorBall-live-v1/";
        static void Capture(string name)
        {
            if(UnityEngine.Application.isBatchMode)return;var t=ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Folder+name+".png",t.EncodeToPNG());Object.Destroy(t);
        }
        public static IEnumerator Run(LevelManager lm,Action seed,Action colours,Action<bool,string> check)
        {
            Directory.CreateDirectory(Folder);
            ColorBallFrameAnimator.PrewarmBoost();ColorBallTargetVfx.Prewarm();
            check(ColorBallFrameAnimator.BoostReady&&ColorBallTargetVfx.Ready,"Approved Color Ball frames and separate beam/glow/impact clips load");
            check(ColorBallFrameAnimator.RevealFrameCount==60&&ColorBallFrameAnimator.BoostFrameCount==108,"Approved 60/120/108 frame contracts");
            var director=RoyalPowerupDirector.Get;
            ColorBallBoostView firstView=null;
            int vfxPool=ColorBallTargetVfx.PoolSize;
            foreach(var orientation in new[]{ItemsTypes.HORIZONTAL_STRIPPED,ItemsTypes.VERTICAL_STRIPPED})
            {
                string label=orientation==ItemsTypes.HORIZONTAL_STRIPPED?"horizontal-half":"vertical-full";
                Time.timeScale=orientation==ItemsTypes.HORIZONTAL_STRIPPED?.5f:1f;
                seed();yield return null;colours();
                // Non-unit field scale exercises actual cell calibration, not hardcoded pixels.
                lm.GameField.localScale=Vector3.one*.85f;
                colours();
                var ball=lm.GetSquare(3,3).item;
                ball.NextType=ItemsTypes.BOMB;ball.ChangeType();
                var anim=ball.GetComponent<ColorBallFrameAnimator>();
                check(anim&&anim.Current==ColorBallFrameAnimator.Phase.Reveal,"Creation begins Reveal immediately: "+label);
                float start=Time.time;bool capturedReveal=false;int samples=0;
                while(anim.Current==ColorBallFrameAnimator.Phase.Reveal)
                {
                    yield return (UnityEngine.Application.isBatchMode?null:new WaitForEndOfFrame());
                    check(anim.View&&anim.View.enabled&&!ball.sprRenderer.enabled,"Reveal has exactly one visible renderer sample "+samples+++": "+label);
                    check(Mathf.Abs(anim.View.bounds.size.x/ColorBallFrameAnimator.Cell-512f/112f)<.005f,"Fixed padded canvas preserves authored 3x/1x size: "+label);
                    check(anim.View.maskInteraction==SpriteMaskInteraction.None,"Reveal ignores board/cell masks: "+label);
                    if(!capturedReveal&&Time.time-start>.32f){Capture(label+"-01-reveal");capturedReveal=true;}
                    if(Time.time-start>2)throw new Exception("Reveal stuck");
                }
                yield return new WaitForSeconds(.2f);yield return (UnityEngine.Application.isBatchMode?null:new WaitForEndOfFrame());
                check(anim.Current==ColorBallFrameAnimator.Phase.Idle&&anim.View.sprite==ColorBallFrameAnimator.IdleFrame(0),"Reveal enters approved 1-cell Idle: "+label);
                var idlePosition=anim.View.transform.position;var idleScale=anim.View.transform.lossyScale;
                Capture(label+"-02-idle");
                yield return new WaitForSeconds(2.05f);yield return (UnityEngine.Application.isBatchMode?null:new WaitForEndOfFrame());
                check(anim.Current==ColorBallFrameAnimator.Phase.Idle&&Vector3.Distance(idlePosition,anim.View.transform.position)<.001f&&Vector3.Distance(idleScale,anim.View.transform.lossyScale)<.001f,"Idle loops without scale/pivot drift: "+label);
                var partner=lm.GetSquare(4,3).item;partner.NextType=orientation;partner.ChangeType();
                yield return new WaitForSeconds(.45f);
                // These are the unchanged Orb rules: most frequent eligible color, row-major targets.
                var counts=new int[6];foreach(var item in lm.GetItems())if(item&&item!=ball&&item!=partner&&item.color>=0&&item.color<6)counts[item.color]++;
                int color=0;for(int k=1;k<6;k++)if(counts[k]>counts[color])color=k;
                var expected=lm.squaresArray.Where(s=>s.item&&s.item!=ball&&s.item!=partner&&s.item.color==color).ToArray();
                var expectedItems=expected.Select(s=>s.item).ToArray();
                int launches=director.RocketLaunches,handoffs=director.ColorBallHandoffs;
                check(RoyalPowerupDirector.TrySwap(ball,partner),"Live Color Ball + Rocket combo accepted: "+label);
                yield return (UnityEngine.Application.isBatchMode?null:new WaitForEndOfFrame());
                var boost=Object.FindObjectsByType<ColorBallBoostView>().First(v=>v.Current!=ColorBallBoostView.Phase.Idle);
                if(firstView)check(boost==firstView,"Boost animation object reused from pool");else firstView=boost;
                check(!anim||!anim.View.enabled,"Idle stops immediately on activation: "+label);
                check(boost.View.maskInteraction==SpriteMaskInteraction.None&&boost.View.sortingOrder==40,"Boosting is unmasked above pieces and below overlay HUD: "+label);
                var centre=lm.GetSquare(3,3).transform.position;
                bool beamCapture=false,finishCapture=false,launchCapture=false;int lastConversions=0;float deadline=Time.realtimeSinceStartup+35;
                while(RoyalPowerupDirector.Busy||ColorBallBoostView.Active>0||director.RocketVisualsActive)
                {
                    yield return (UnityEngine.Application.isBatchMode?null:new WaitForEndOfFrame());
                    if(Time.realtimeSinceStartup>deadline)throw new Exception("Color Ball/Rocket handoff failed to settle");
                    if(boost.Current!=ColorBallBoostView.Phase.Idle)
                    {
                        check(boost.View.enabled&&boost.View.sprite,"Boost never displays a missing frame: "+label);
                        check(Vector2.Distance(boost.transform.position,centre)<.001f,"Boost stays on activation cell: "+label);
                        check(director.RocketLaunches==launches,"No Rocket fires before Boosting finish: "+label);
                    }
                    if(director.ColorBallConversions>lastConversions)
                    {
                        check(boost.Current==ColorBallBoostView.Phase.Loop,"Conversions occur during Charged Spin: "+label);
                        lastConversions=director.ColorBallConversions;
                        if(!beamCapture&&lastConversions>=2){Capture(label+"-03-converting");beamCapture=true;}
                    }
                    if(director.ColorBallState==RoyalPowerupDirector.ColorBallPhase.Finish&&!finishCapture)
                    {
                        check(director.ColorBallConversions==expected.Length,"Every selected gem converted exactly once: "+label);
                        for(int i=0;i<expected.Length;i++)check(expected[i].item==expectedItems[i]&&expectedItems[i].currentType==orientation,"Correct cell, original Item, Rocket orientation, no premature destruction "+i+": "+label);
                        check(ColorBallTargetVfx.Active==0,"Last beam/glow/impact finishes before handoff: "+label);
                        Capture(label+"-04-charged-complete");finishCapture=true;
                    }
                    if(boost.Current==ColorBallBoostView.Phase.Finish&&boost.Frame>=90)CaptureOnce(ref finishFrame,label+"-05-finish");
                    if(director.RocketLaunches>launches&&!launchCapture){Capture(label+"-06-rockets");launchCapture=true;}
                }
                check(beamCapture&&finishCapture&&launchCapture,"Observed conversion, finish and existing Rocket launch: "+label);
                check(director.ColorBallHandoffs==handoffs+1,"Exactly one handoff: "+label);
                check(director.RocketLaunches-launches==expected.Length,"Every generated Rocket activates once: "+label);
                check(ColorBallBoostView.Active==0&&ColorBallTargetVfx.Active==0&&!boost.View.enabled&&boost.transform.localScale==Vector3.one,"No stale ball/beam/glow or pooled scale: "+label);
                check(ColorBallTargetVfx.PoolSize==vfxPool&&ColorBallBoostView.PoolSize==1,"Pools remain bounded across live reuse: "+label);
                Capture(label+"-07-clean");finishFrame=false;
            }
            // Cancellation/level-exit cleanup uses the existing Director.Cancel path.
            var cancelView=ColorBallBoostView.Begin(Vector3.zero);
            ColorBallTargetVfx.Play(ColorBallTargetVfx.Kind.Beam,Vector3.zero,Vector3.one);
            director.Cancel();yield return null;
            check(ColorBallBoostView.Active==0&&ColorBallTargetVfx.Active==0,"Cancellation returns all Color Ball visuals to pools");
        }
        static bool finishFrame;
        static void CaptureOnce(ref bool done,string name){if(!done){Capture(name);done=true;}}
    }
}
