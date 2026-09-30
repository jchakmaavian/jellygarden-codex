using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RoyalThrone.Boosters.ColorBall.V4;
namespace JellyGarden.Scripts.Juice
{
    public sealed partial class RoyalPowerupDirector
    {
        public int ColorBallOrdinaryClears { get; private set; }
        public int ColorBallLastClearFrame { get; private set; }
        sealed class V4Target {public Square square;public Item item;public ColorBallVfxV4.Flight beam;public int lease,chargeLease;public ColorBallChargedTargetV4 charge;public bool hit,flashed,arrivalNotified,scaleHeld;public float contactAge;public Vector3 rest;}
        // Explicit ownership survives Unity cancelling nested enumerators without disposal.
        readonly HashSet<V4Target> colorBallContactPoses = new HashSet<V4Target>();
        void BeginColorBallContactPose(V4Target target)
        {
            if (target.scaleHeld || !target.item) return;
            target.rest = target.item.transform.localScale;
            foreach (var owner in colorBallContactPoses)
                if (owner.item == target.item) { target.rest = owner.rest; break; }
            target.scaleHeld = true; colorBallContactPoses.Add(target);
        }
        void EndColorBallContactPose(V4Target target)
        {
            if (!target.scaleHeld) return;
            target.scaleHeld = false; colorBallContactPoses.Remove(target);
            if (target.item && !target.item.destroying) target.item.transform.localScale = target.rest;
        }
        void ReleaseColorBallContactPoses()
        {
            foreach (var owner in colorBallContactPoses)
            {
                owner.scaleHeld = false;
                if (owner.item && !owner.item.destroying) owner.item.transform.localScale = owner.rest;
            }
            colorBallContactPoses.Clear();
        }
        IEnumerator AnimateColorBallV4Targets(Square origin,List<Square> targets,ItemsTypes payload)
        {
            ColorBallConversions=0;ColorBallBeamArrivals=0;ColorBallSelectedTargets=targets.Count;
            ColorBallOrdinaryClears=0;ColorBallLastClearFrame=-1;
            var ball=ColorBallSeatV4.Claim(origin)??ColorBallVisualV4.Take(origin.transform.position);
            var centre=origin.transform.position;ball.SetAnchor(centre);int visualLease=ball.Lease;ball.Activate();
            bool convert=payload!=ItemsTypes.NONE;var deferred=new List<V4Target>();var flights=new List<V4Target>(12);
            var charged=new List<V4Target>(targets.Count);var collected=new HashSet<Item>();
            var selectedItems=new Dictionary<Square,Item>();
            foreach(var square in targets)if(square)selectedItems[square]=square.item;
            var convertedTntViews=new List<RoyalThrone.ReferenceGameplay.ReferencePieceVisual>();
            ColorBallState=ColorBallPhase.SpinUp;
            try{
                while(ball.Current==ColorBallVisualV4.Phase.Charge)yield return null;
                ColorBallState=ColorBallPhase.Converting;int next=0;float clock=0,nextBeam=0;bool prepared=false;
                while(next<targets.Count||flights.Count>0){
                    if(Paused){yield return null;continue;}
                    if(next<targets.Count&&clock>=nextBeam){
                        var sq=targets[next];
                        if(!sq||!selectedItems.TryGetValue(sq,out var selected)||!selected||sq.item!=selected||selected.destroying||selected.currentType!=ItemsTypes.NONE){next++;prepared=false;continue;}
                        if(!prepared){ball.PrepareBeam(sq.transform.position);prepared=true;nextBeam+=1f/30f;}
                        else {var beam=ColorBallVfxV4.Launch(centre,sq.transform.position,sq.item.color);
                        if(beam!=null){
                            var item=sq.item;var target=new V4Target{square=sq,item=item,beam=beam,lease=beam.Lease,rest=item.transform.localScale};
                            beam.ArrivalNotice=completedLease=>{if(target.lease==completedLease)target.arrivalNotified=true;};
                            flights.Add(target);
                            ball.BeamFired(sq.transform.position);next++;prepared=false;
                            float fraction=next/(float)Mathf.Max(1,targets.Count);
                            // Same selected cells/order. Only the visual launch rhythm varies with count.
                            nextBeam+=(targets.Count>28?Mathf.Lerp(.132f,.121f,Mathf.Sin(fraction*Mathf.PI)):.124f)-1f/30f;
                        }
                    }}
                    for(int i=0;i<flights.Count;){
                        var f=flights[i];if(!f.square||!f.item||f.item.destroying||f.square.item!=f.item||f.item.currentType!=ItemsTypes.NONE){EndColorBallContactPose(f);if(f.charge)f.charge.Release(f.chargeLease);if(f.beam.Lease==f.lease)f.beam.ArrivalNotice=null;flights.RemoveAt(i);continue;}
                        if(!f.hit&&f.arrivalNotified){f.hit=true;f.contactAge=0;ColorBallBeamArrivals++;ColorBallVfxV4.Contact(f.square.transform.position,f.item.color);ball.Send(ColorBallEvent.ColorBallBeamHit,f.square.transform.position);
                            if(convert)BeginColorBallContactPose(f);
                            if(!convert&&f.item.currentType==ItemsTypes.NONE){f.charge=ColorBallChargedTargetV4.Hold(f.item);if(f.charge)f.chargeLease=f.charge.Lease;}
                            i++;continue;}
                        if(f.hit){
                            f.contactAge+=Time.deltaTime;float t=Mathf.Clamp01(f.contactAge/(2f/30f));
                            if(!f.flashed&&f.contactAge+1e-5f>=1f/30f){f.flashed=true;ColorBallVfxV4.TargetFlash(f.square.transform.position);ball.Send(ColorBallEvent.ColorBallTargetFlash,f.square.transform.position);}
                            if(convert)f.item.transform.localScale=Vector3.Scale(f.rest,new Vector3(1+.055f*Mathf.Sin(t*Mathf.PI),1-.11f*Mathf.Sin(t*Mathf.PI),1));
                            if(f.contactAge+1e-5f>=2f/30f){
                                EndColorBallContactPose(f);
                                if(convert&&f.item.currentType==ItemsTypes.NONE){f.item.NextType=payload;f.item.ChangeType();
                                    if(payload==ItemsTypes.PACKAGE){var native=f.item.GetComponent<RoyalThrone.ReferenceGameplay.ReferencePieceVisual>();if(native&&native.RevealFromColorBall()>0)convertedTntViews.Add(native);}
                                    if(f.item.currentType==payload){deferred.Add(f);ColorBallConversions++;ColorBallVfxV4.Converted(f.square.transform.position);ball.Send(ColorBallEvent.ColorBallTargetConverted,f.square.transform.position);}}
                                // Keep ordinary gems charged in their original cells until
                                // all selected beam contacts finish. This staging is rebuilt
                                // from the requested sequence and recovered callback roles.
                                if(!convert&&f.item.currentType==ItemsTypes.NONE)charged.Add(f);
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
                foreach(var f in charged){
                    if(f.charge)f.charge.Release(f.chargeLease);
                    if(f.square&&f.item&&!f.item.destroying&&f.item.currentType==ItemsTypes.NONE&&f.square.item==f.item&&collected.Add(f.item))
                    {Hit(f.square);ColorBallOrdinaryClears++;ColorBallLastClearFrame=Time.frameCount;}
                }
                while(ball.Lease==visualLease&&ball.Current!=ColorBallVisualV4.Phase.Free)yield return null;
                // Let the final dedicated TNT conversion clip settle only if it
                // outlasts the existing Color Ball finish. No extra fixed pause.
                while(convertedTntViews.Exists(view=>view&&view.Revealing))yield return null;
                ColorBallState=ColorBallPhase.Handoff;ColorBallHandoffs++;
                // Only this sequence's successful conversions belong to its handoff.
                foreach(var f in deferred)if(f.square&&f.item&&!f.item.destroying&&f.square.item==f.item&&f.item.currentType==payload&&collected.Add(f.item))Hit(f.square);
                Reaction?.Invoke(1.4f);
            }finally{
                foreach(var f in flights)if(f.beam.Lease==f.lease)f.beam.ArrivalNotice=null;
                foreach(var f in flights)EndColorBallContactPose(f);
                foreach(var f in flights)if(f.charge)f.charge.Release(f.chargeLease);
                foreach(var f in charged)if(f.charge)f.charge.Release(f.chargeLease);
                if(ball&&ball.Lease==visualLease)ball.Release();ColorBallState=ColorBallPhase.Complete;
            }
        }
    }
}
