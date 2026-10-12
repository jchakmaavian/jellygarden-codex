using System.Collections;
using RoyalThrone.Boosters.Propeller.V3;
using UnityEngine;

namespace JellyGarden.Scripts.Juice
{
    public sealed partial class RoyalPowerupDirector
    {
        void PropellerHitV3(Square cell)
        {
            if(cell&&cell.item&&!cell.item.destroying&&cell.item.currentType==ItemsTypes.NONE)cell.item.royalPropellerV3Clear=true;
            Hit(cell);
        }
        IEnumerator PropellerSequenceV3(Square origin,ItemsTypes payload,int count)
        {
            var seated=PropellerSeatV3.Claim(origin);
            for(int i=0;i<count;i++){
                var target=Target(origin);
                if(target){StartOperation(PropellerFlightV3(origin,target,payload,i==0?seated:null,i==0));seated=null;}
                yield return Wait(.06f);
            }
            if(seated)seated.Release();
        }
        IEnumerator PropellerFlightV3(Square origin,Square target,ItemsTypes payload,PropellerVisualV3 view,bool neighbors)
        {
            if(!view)view=PropellerVisualV3.Take(origin.transform.position,Cell,false);int lease=view.Lease;
            try{
                float delay=PropellerVisualV3.ReserveLaunchDelay();if(delay>0)yield return Wait(delay);
                RoyalAudioDirector.Get?.Play(RoyalCue.Helicopter);view.Activate(target.transform.position);
                bool hitNeighbors=false;
                while(view&&view.Current==PropellerVisualV3.Phase.Activation){
                    // Follow the visual release event, not an independent timer one frame ahead.
                    if(neighbors&&!hitNeighbors&&view.HasTakenOff){hitNeighbors=true;foreach(var n in origin.GetAllNeghbors()){if(!n)continue;view.NeighborHit(n.transform.position);PropellerHitV3(n);}}
                    yield return null;
                }
                if(!view||!view.FlightReady)yield break;
                var start=view.BeginFlightTranslation();var end=target.transform.position+Vector3.back*.8f;
                var control=(start+end)*.5f+Vector3.up*Cell*2;float duration=RoyalEffectsProfile.Get.helicopterFlight,t=0;
                while(t<duration){
                    if(!Paused)t+=Time.deltaTime;float progress=Mathf.Clamp01(t/duration),k=Mathf.SmoothStep(0,1,progress);
                    var p=(1-k)*(1-k)*start+2*(1-k)*k*control+k*k*end;
                    view.Place(p,2*(1-k)*(control-start)+2*k*(end-control),progress);yield return null;
                }
                view.Contact(target.transform.position);
                while(view&&view.Current==PropellerVisualV3.Phase.Impact&&!view.ImpactStarted)yield return null;
                reserved.Remove(target);
                if(payload==ItemsTypes.PACKAGE)yield return Explosion(target,2);
                else if(payload==ItemsTypes.HORIZONTAL_STRIPPED||payload==ItemsTypes.VERTICAL_STRIPPED)yield return Rocket(target,payload==ItemsTypes.HORIZONTAL_STRIPPED,1);
                else PropellerHitV3(target);
                yield return Wait(.40f);
            }
            finally{if(target)reserved.Remove(target);if(view&&view.Lease==lease&&view.Current!=PropellerVisualV3.Phase.Free)view.Release();}
        }
    }
}
