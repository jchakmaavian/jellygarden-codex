using System.Collections.Generic;
using UnityEngine;
using JellyGarden.Scripts;
namespace RoyalThrone.Boosters.ColorBall.V3
{
    [DefaultExecutionOrder(1050)]
    public sealed class ColorBallSeatV3 : MonoBehaviour
    {
        static readonly Dictionary<Square,ColorBallSeatV3> seats=new Dictionary<Square,ColorBallSeatV3>();
        Item item;Square registered;ColorBallVisualV3 view;int lease;bool claimed;
        public ColorBallVisualV3 View=>view;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset(){seats.Clear();}
        public static ColorBallSeatV3 Attach(Item item,bool reveal=false){
            if(!Application.isPlaying||!item||item.destroying||item.currentType!=ItemsTypes.BOMB)return null;
            var s=item.GetComponent<ColorBallSeatV3>();if(!s)s=item.gameObject.AddComponent<ColorBallSeatV3>();s.item=item;s.claimed=false;
            if(!s.view||s.view.Current==ColorBallVisualV3.Phase.Free||s.view.Lease!=s.lease){s.view=ColorBallVisualV3.Take(item.transform.position,reveal);s.lease=s.view.Lease;}
            else if(reveal)s.view.Reveal();
            s.Register();s.Hide();return s;
        }
        void Register(){if(registered!=item.square){Unregister();registered=item.square;}if(registered)seats[registered]=this;}
        void Unregister(){if(registered&&seats.TryGetValue(registered,out var old)&&old==this)seats.Remove(registered);registered=null;}
        void Hide(){if(item&&item.sprRenderer)item.sprRenderer.enabled=false;}
        public static void HideForActivation(Item i){
            if(!i||i.currentType!=ItemsTypes.BOMB)return;
            var s=i.GetComponent<ColorBallSeatV3>();if(!s)return;s.Hide();
            if(!s.view||s.view.Lease!=s.lease||!i.square)return;
            // Item destruction precedes the nested combo coroutine. Keep its exact surface pose alive.
            s.view.HoldForClaim();s.view=null;s.claimed=true;s.Unregister();
        }
        public static ColorBallVisualV3 Claim(Square square){
            if(!square)return null;
            var v=ColorBallVisualV3.ClaimPending(square.transform.position);if(v)return v;
            if(!seats.TryGetValue(square,out var s)||!s||!s.view||s.lease!=s.view.Lease)return null;
            v=s.view;s.view=null;s.claimed=true;s.Unregister();s.Hide();return v;
        }
        void LateUpdate(){if(!item||item.destroying||item.currentType!=ItemsTypes.BOMB){Stop();return;}if(claimed||!view)return;Register();Hide();view.SetAnchor(item.transform.position);}
        void Stop(){if(view&&view.Lease==lease)view.Release();view=null;Unregister();}
        void OnDisable(){Stop();}void OnDestroy(){Stop();}
    }
}
