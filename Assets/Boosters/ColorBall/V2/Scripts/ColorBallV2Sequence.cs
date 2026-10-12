using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RoyalThrone.Boosters.ColorBall.V2;
namespace JellyGarden.Scripts.Juice
{
    public sealed partial class RoyalPowerupDirector
    {
        sealed class V2Target {public Square square;public Item item;public ColorBallVfxV2.Flight beam;public int lease;public bool hit;public float contactAge;public Vector3 rest;}
        IEnumerator AnimateColorBallV2Targets(Square origin,List<Square> targets,ItemsTypes payload)
        {
            ColorBallConversions=0;ColorBallBeamArrivals=0;ColorBallSelectedTargets=targets.Count;
            var ball=ColorBallSeatV2.Claim(origin)??ColorBallVisualV2.Take(origin.transform.position);
            var centre=origin.transform.position;ball.SetAnchor(centre);ball.Activate();
            bool convert=payload!=ItemsTypes.NONE;var deferred=new List<Square>();var flights=new List<V2Target>(12);
            ColorBallState=ColorBallPhase.SpinUp;
            try{
                while(ball.Current==ColorBallVisualV2.Phase.Charge)yield return null;
                ColorBallState=ColorBallPhase.Converting;int next=0;float clock=0,nextBeam=0;
                while(next<targets.Count||flights.Count>0){
                    if(Paused){yield return null;continue;}
                    if(next<targets.Count&&clock>=nextBeam){
                        var sq=targets[next];
                        if(!sq||!sq.item||sq.item.destroying){next++;continue;}
                        var beam=ColorBallVfxV2.Launch(centre,sq.transform.position,sq.item.color);
                        if(beam!=null){
                            var item=sq.item;flights.Add(new V2Target{square=sq,item=item,beam=beam,lease=beam.Lease,rest=item.transform.localScale});
                            ball.BeamFired(sq.transform.position);next++;
                            float fraction=next/(float)Mathf.Max(1,targets.Count);
                            // Same selected cells/order. Only the visual launch rhythm varies with count.
                            nextBeam=clock+(targets.Count>28?Mathf.Lerp(.095f,.068f,Mathf.Sin(fraction*Mathf.PI)):.095f);
                        }
                    }
                    for(int i=0;i<flights.Count;){
                        var f=flights[i];if(!f.square||!f.item||f.item.destroying||f.square.item!=f.item){if(f.item)f.item.transform.localScale=f.rest;flights.RemoveAt(i);continue;}
                        if(!f.hit&&f.beam.Lease==f.lease&&f.beam.Arrived){f.hit=true;f.contactAge=0;ColorBallBeamArrivals++;ColorBallVfxV2.Contact(f.square.transform.position,f.item.color);ball.Send(ColorBallEvent.ColorBallBeamHit,f.square.transform.position);}
                        if(f.hit){
                            f.contactAge+=Time.deltaTime;float t=Mathf.Clamp01(f.contactAge/.065f);
                            f.item.transform.localScale=Vector3.Scale(f.rest,new Vector3(1+.055f*Mathf.Sin(t*Mathf.PI),1-.11f*Mathf.Sin(t*Mathf.PI),1));
                            if(f.contactAge>=.065f){
                                f.item.transform.localScale=f.rest;
                                if(convert&&f.item.currentType==ItemsTypes.NONE){f.item.NextType=payload;f.item.ChangeType();ColorBallConversions++;ColorBallVfxV2.Converted(f.square.transform.position);ball.Send(ColorBallEvent.ColorBallTargetConverted,f.square.transform.position);}
                                if(!convert&&f.item.currentType==ItemsTypes.NONE)Hit(f.square);else deferred.Add(f.square);
                                flights.RemoveAt(i);continue;
                            }
                        }
                        i++;
                    }
                    yield return null;if(!Paused)clock+=Time.deltaTime;
                }
                // Last tail must actually finish; no arbitrary delay before contact/conversion.
                while(ColorBallVfxV2.ActiveBeams>0)yield return null;
                ColorBallState=ColorBallPhase.Finish;ball.Finish();
                while(ball.Current!=ColorBallVisualV2.Phase.Free)yield return null;
                ColorBallState=ColorBallPhase.Handoff;ColorBallHandoffs++;
                foreach(var sq in deferred)Hit(sq); // Unchanged Rocket queue/launch/destruction owner.
                Reaction?.Invoke(1.4f);
            }finally{
                foreach(var f in flights)if(f.item&&!f.item.destroying)f.item.transform.localScale=f.rest;
                if(ball)ball.Release();ColorBallState=ColorBallPhase.Complete;
            }
        }
    }
}
