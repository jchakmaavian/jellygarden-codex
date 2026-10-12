using System.Collections.Generic;
using JellyGarden.Scripts;
using UnityEngine;

namespace RoyalThrone.Boosters.Propeller.V5
{
    [DefaultExecutionOrder(500)]
    public sealed class PropellerSeatV5 : MonoBehaviour
    {
        static readonly Dictionary<Square,PropellerSeatV5> seats=new Dictionary<Square,PropellerSeatV5>();
        Item item;Square registered;PropellerVisualV5 view;int lease;
        public PropellerVisualV5 View=>view;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void ResetStatics(){seats.Clear();}
        public static PropellerSeatV5 Attach(Item item,bool reveal=false)
        {
            if(!Application.isPlaying||!item||item.destroying||item.currentType!=ItemsTypes.HELICOPTER)return null;
            var s=item.GetComponent<PropellerSeatV5>();if(!s)s=item.gameObject.AddComponent<PropellerSeatV5>();s.item=item;
            if(!s.view||s.view.Current==PropellerVisualV5.Phase.Free||s.view.Lease!=s.lease){
                var lm=LevelManager.THIS;float cell=lm.squareWidth*Mathf.Abs(lm.GameField.lossyScale.x);
                s.view=PropellerVisualV5.Take(item.transform.position,cell,reveal);s.lease=s.view.Lease;
            }else if(reveal)s.view.Reveal();
            s.Register();s.Hide();return s;
        }
        void Register(){if(registered!=item.square){if(registered&&seats.TryGetValue(registered,out var old)&&old==this)seats.Remove(registered);registered=item.square;}if(registered)seats[registered]=this;}
        void Hide(){if(item&&item.sprRenderer)item.sprRenderer.enabled=false;}
        public static PropellerVisualV5 Claim(Square square)
        {
            if(!square||!seats.TryGetValue(square,out var s)||!s||!s.view||s.view.Lease!=s.lease)return null;
            var v=s.view;s.view=null;seats.Remove(square);s.registered=null;s.Hide();return v;
        }
        void LateUpdate()
        {
            if(!item||item.destroying||item.currentType!=ItemsTypes.HELICOPTER){Stop();return;}
            if(!view)return;Register();Hide();view.transform.position=item.transform.position+Vector3.back*.8f;
        }
        void Stop(){if(view&&view.Lease==lease)view.Release();view=null;if(registered&&seats.TryGetValue(registered,out var s)&&s==this)seats.Remove(registered);registered=null;}
        void OnDisable(){Stop();}void OnDestroy(){Stop();}
    }
}
