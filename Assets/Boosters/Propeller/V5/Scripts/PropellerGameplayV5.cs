using System.Collections;
using RoyalThrone.Boosters.Propeller.V5;
using UnityEngine;

namespace JellyGarden.Scripts.Juice
{
    public sealed partial class RoyalPowerupDirector
    {
        void PropellerHitV5(Square cell)
        {
            if(cell&&cell.item&&!cell.item.destroying&&cell.item.currentType==ItemsTypes.NONE)cell.item.royalPropellerV5Clear=true;
            Hit(cell);
        }
        IEnumerator PropellerSequenceV5(Square origin,ItemsTypes payload,int count)
        {
            var seated=PropellerSeatV5.Claim(origin);
            for(int i=0;i<count;i++){
                var target=Target(origin);
                if(target){StartOperation(PropellerFlightV5(origin,target,payload,i==0?seated:null,i==0));seated=null;}
                yield return Wait(.07f);
            }
            if(seated)seated.Release();
        }
        IEnumerator PropellerFlightV5(Square origin,Square target,ItemsTypes payload,PropellerVisualV5 view,bool neighbors)
        {
            if(!view)view=PropellerVisualV5.Take(origin.transform.position,Cell,false);int lease=view.Lease;
            try{
                float delay=PropellerVisualV5.ReserveLaunchDelay();if(delay>0)yield return Wait(delay);
                view.Activate(target.transform.position);
                bool hitNeighbors=false;
                while(view&&view.Current==PropellerVisualV5.Phase.Activation){
                    // Follow the visual release event, not an independent timer one frame ahead.
                    if(neighbors&&!hitNeighbors&&view.HasTakenOff){hitNeighbors=true;foreach(var n in origin.GetAllNeghbors()){if(!n)continue;view.NeighborHit(n.transform.position);PropellerHitV5(n);}}
                    yield return null;
                }
                if(!view||!view.FlightReady)yield break;
                var start=view.BeginFlightTranslation();var end=target.transform.position+Vector3.back*.8f;
                float distance=Vector2.Distance(origin.transform.position,target.transform.position)/Cell;
                float duration=PropellerMotionV5.Duration(distance),t=0;
                // Cosmetic path: board targeting and destruction remain in the existing director.
                var liftControl=start+Vector3.up*Cell*.30f;
                var approach=Vector3.Lerp(start,end,.68f)+Vector3.up*Cell*PropellerMotionV5.Arc(distance)*.65f;
                view.FlightDuration=duration;
                while(t<duration){
                    if(!Paused)t+=Time.deltaTime;float progress=Mathf.Clamp01(t/duration),k=PropellerMotionV5.Ease(progress,duration);
                    float a=1-k;var p=a*a*a*start+3*a*a*k*liftControl+3*a*k*k*approach+k*k*k*end;
                    var tangent=3*a*a*(liftControl-start)+6*a*k*(approach-liftControl)+3*k*k*(end-approach);view.Place(p,tangent,progress);yield return null;
                }
                view.Contact(target.transform.position);
                while(view&&view.Current==PropellerVisualV5.Phase.Impact&&!view.ImpactStarted)yield return null;
                reserved.Remove(target);
                if(payload==ItemsTypes.PACKAGE)yield return Explosion(target,2);
                else if(payload==ItemsTypes.HORIZONTAL_STRIPPED||payload==ItemsTypes.VERTICAL_STRIPPED)yield return Rocket(target,payload==ItemsTypes.HORIZONTAL_STRIPPED,1);
                else PropellerHitV5(target);
                yield return Wait(.40f);
            }
            finally{if(target)reserved.Remove(target);if(view&&view.Lease==lease&&view.Current!=PropellerVisualV5.Phase.Free)view.Release();}
        }
    }
}
