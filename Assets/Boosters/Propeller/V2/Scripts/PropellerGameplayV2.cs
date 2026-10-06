using System.Collections;
using RoyalThrone.Boosters.Propeller.V2;
using UnityEngine;

namespace JellyGarden.Scripts.Juice
{
    public sealed partial class RoyalPowerupDirector
    {
        // Selection, travel path and target damage remain in the existing gameplay director.
        // This adapter owns no blast radius, matching rules or target-scoring logic.
        IEnumerator PropellerSequenceV2(Square origin,ItemsTypes payload,int count)
        {
            var seated=PropellerSeatV2.Claim(origin);
            for(int i=0;i<count;i++){
                var target=Target(origin);
                if(target){StartOperation(PropellerFlightV2(origin,target,payload,i==0?seated:null,i==0));seated=null;}
                yield return Wait(.08f);
            }
            if(seated)seated.Release();
        }
        IEnumerator PropellerFlightV2(Square origin,Square target,ItemsTypes payload,PropellerVisualV2 view,bool neighbors)
        {
            if(!view)view=PropellerVisualV2.Take(origin.transform.position,Cell,false);
            int lease=view.Lease;
            try{
                RoyalAudioDirector.Get?.Play(RoyalCue.Helicopter);view.Activate(target.transform.position);
                bool hitNeighbors=false;
                while(view&&view.Current==PropellerVisualV2.Phase.Activation){
                    if(neighbors&&!hitNeighbors&&view.Age>=PropellerVisualV2.TakeoffAt){
                        hitNeighbors=true;foreach(var n in origin.GetAllNeghbors()){if(!n)continue;view.NeighborHit(n.transform.position);Hit(n);}
                    }
                    yield return null;
                }
                if(!view||!view.FlightReady)yield break;
                var start=view.BeginFlightTranslation();var end=target.transform.position+Vector3.back*.8f;
                var control=(start+end)*.5f+Vector3.up*Cell*2;
                float duration=RoyalEffectsProfile.Get.helicopterFlight,t=0;
                while(t<duration){
                    if(!Paused)t+=Time.deltaTime;float k=Mathf.SmoothStep(0,1,Mathf.Clamp01(t/duration));
                    var p=(1-k)*(1-k)*start+2*(1-k)*k*control+k*k*end;
                    view.Place(p,2*(1-k)*(control-start)+2*k*(end-control));yield return null;
                }
                view.Contact(target.transform.position);reserved.Remove(target);
                // Payloads are handed to their approved systems unchanged, after the compact contact.
                if(payload==ItemsTypes.PACKAGE)yield return Explosion(target,2);
                else if(payload==ItemsTypes.HORIZONTAL_STRIPPED||payload==ItemsTypes.VERTICAL_STRIPPED)yield return Rocket(target,payload==ItemsTypes.HORIZONTAL_STRIPPED,1);
                else Hit(target);
                yield return Wait(.40f); // only V2's tiny stars finish; there is no smoke/debris tail.
            }
            finally {if(target)reserved.Remove(target);if(view&&view.Lease==lease&&view.Current!=PropellerVisualV2.Phase.Free)view.Release();}
        }
    }
}
