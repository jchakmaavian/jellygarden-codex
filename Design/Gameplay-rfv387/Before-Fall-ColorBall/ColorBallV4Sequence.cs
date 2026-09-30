using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RoyalThrone.Boosters.ColorBall.V4;
namespace JellyGarden.Scripts.Juice
{
    public sealed partial class RoyalPowerupDirector
    {
        sealed class V4Target {public Square square;public Item item;public ColorBallVfxV4.Flight beam;public int lease;public bool hit,flashed;public float contactAge;public Vector3 rest;}
        IEnumerator AnimateColorBallV4Targets(Square origin,List<Square> targets,ItemsTypes payload)
        {
            ColorBallConversions=0;ColorBallBeamArrivals=0;ColorBallSelectedTargets=targets.Count;
            var ball=ColorBallSeatV4.Claim(origin)??ColorBallVisualV4.Take(origin.transform.position);
            var centre=origin.transform.position;ball.SetAnchor(centre);int visualLease=ball.Lease;ball.Activate();
            bool convert=payload!=ItemsTypes.NONE;var deferred=new List<Square>();var flights=new List<V4Target>(12);
            ColorBallState=ColorBallPhase.SpinUp;
            try{
                while(ball.Current==ColorBallVisualV4.Phase.Charge)yield return null;
                ColorBallState=ColorBallPhase.Converting;int next=0;float clock=0,nextBeam=0;bool prepared=false;
                while(next<targets.Count||flights.Count>0){
                    if(Paused){yield return null;continue;}
                    if(next<targets.Count&&clock>=nextBeam){
                        var sq=targets[next];
                        if(!sq||!sq.item||sq.item.destroying){next++;prepared=false;continue;}
                        if(!prepared){ball.PrepareBeam(sq.transform.position);prepared=true;nextBeam+=1f/30f;}
                        else {var beam=ColorBallVfxV4.Launch(centre,sq.transform.position,sq.item.color);
                        if(beam!=null){
                            var item=sq.item;flights.Add(new V4Target{square=sq,item=item,beam=beam,lease=beam.Lease,rest=item.transform.localScale});
                            ball.BeamFired(sq.transform.position);next++;prepared=false;
                            float fraction=next/(float)Mathf.Max(1,targets.Count);
                            // Same selected cells/order. Only the visual launch rhythm varies with count.
                            nextBeam+=(targets.Count>28?Mathf.Lerp(.132f,.121f,Mathf.Sin(fraction*Mathf.PI)):.124f)-1f/30f;
                        }
                    }}
                    for(int i=0;i<flights.Count;){
                        var f=flights[i];if(!f.square||!f.item||f.item.destroying||f.square.item!=f.item){if(f.item)f.item.transform.localScale=f.rest;flights.RemoveAt(i);continue;}
                        if(!f.hit&&f.beam.Lease==f.lease&&f.beam.Arrived){f.hit=true;f.contactAge=0;ColorBallBeamArrivals++;ColorBallVfxV4.Contact(f.square.transform.position,f.item.color);ball.Send(ColorBallEvent.ColorBallBeamHit,f.square.transform.position);i++;continue;}
                        if(f.hit){
                            f.contactAge+=Time.deltaTime;float t=Mathf.Clamp01(f.contactAge/(2f/30f));
                            if(!f.flashed&&f.contactAge+1e-5f>=1f/30f){f.flashed=true;ColorBallVfxV4.TargetFlash(f.square.transform.position);ball.Send(ColorBallEvent.ColorBallTargetFlash,f.square.transform.position);}
                            f.item.transform.localScale=Vector3.Scale(f.rest,new Vector3(1+.055f*Mathf.Sin(t*Mathf.PI),1-.11f*Mathf.Sin(t*Mathf.PI),1));
                            if(f.contactAge+1e-5f>=2f/30f){
                                f.item.transform.localScale=f.rest;
                                if(convert&&f.item.currentType==ItemsTypes.NONE){f.item.NextType=payload;f.item.ChangeType();ColorBallConversions++;ColorBallVfxV4.Converted(f.square.transform.position);ball.Send(ColorBallEvent.ColorBallTargetConverted,f.square.transform.position);}
                                if(!convert&&f.item.currentType==ItemsTypes.NONE)Hit(f.square);else deferred.Add(f.square);
                                flights.RemoveAt(i);continue;
                            }
                        }
                        i++;
                    }
                    yield return null;if(!Paused)clock+=Time.deltaTime;
                }
                // Last tail must actually finish; no arbitrary delay before contact/conversion.
                while(ColorBallVfxV4.ActiveBeams>0)yield return null;
                ColorBallState=ColorBallPhase.Finish;ball.Finish();
                while(ball.Lease==visualLease&&ball.Current!=ColorBallVisualV4.Phase.Free)yield return null;
                ColorBallState=ColorBallPhase.Handoff;ColorBallHandoffs++;
                foreach(var sq in deferred)Hit(sq); // Unchanged Rocket queue/launch/destruction owner.
                Reaction?.Invoke(1.4f);
            }finally{
                foreach(var f in flights)if(f.item&&!f.item.destroying)f.item.transform.localScale=f.rest;
                if(ball&&ball.Lease==visualLease)ball.Release();ColorBallState=ColorBallPhase.Complete;
            }
        }
    }
}
