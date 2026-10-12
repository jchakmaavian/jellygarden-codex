using System.Collections.Generic;
using UnityEngine;
using JellyGarden.Scripts;
namespace RoyalThrone.Boosters.ColorBall.V2
{
    [DefaultExecutionOrder(1050)]
    public sealed class ColorBallSeatV2 : MonoBehaviour
    {
        static readonly Dictionary<Square,ColorBallSeatV2> seats=new Dictionary<Square,ColorBallSeatV2>();
        static readonly Dictionary<Square,ColorBallVisualV2> pending=new Dictionary<Square,ColorBallVisualV2>();
        Item item;Square registered;ColorBallVisualV2 view;int lease;bool claimed;
        public ColorBallVisualV2 View=>view;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset(){seats.Clear();pending.Clear();}
        public static ColorBallSeatV2 Attach(Item item,bool reveal=false){
            if(!Application.isPlaying||!item||item.destroying||item.currentType!=ItemsTypes.BOMB)return null;
            var s=item.GetComponent<ColorBallSeatV2>();if(!s)s=item.gameObject.AddComponent<ColorBallSeatV2>();s.item=item;s.claimed=false;
            if(!s.view||s.view.Current==ColorBallVisualV2.Phase.Free||s.view.Lease!=s.lease){s.view=ColorBallVisualV2.Take(item.transform.position,reveal);s.lease=s.view.Lease;}
            else if(reveal)s.view.Reveal();
            s.Register();s.Hide();return s;
        }
        void Register(){if(registered!=item.square){Unregister();registered=item.square;}if(registered)seats[registered]=this;}
        void Unregister(){if(registered&&seats.TryGetValue(registered,out var old)&&old==this)seats.Remove(registered);registered=null;}
        void Hide(){if(item&&item.sprRenderer)item.sprRenderer.enabled=false;}
        public static void HideForActivation(Item i){
            if(!i||i.currentType!=ItemsTypes.BOMB)return;
            var s=i.GetComponent<ColorBallSeatV2>();if(!s)return;s.Hide();
            if(!s.view||s.view.Lease!=s.lease||!i.square)return;
            // Item destruction precedes the nested combo coroutine. Keep its exact surface pose alive.
            pending[i.square]=s.view;s.view.HoldForClaim();s.view=null;s.claimed=true;s.Unregister();
        }
        public static ColorBallVisualV2 Claim(Square square){
            if(!square)return null;
            if(!pending.TryGetValue(square,out var v)||!v||!v.AwaitingClaim){
                v=null;float distance=float.MaxValue;
                foreach(var p in pending)if(p.Value&&p.Value.AwaitingClaim){float d=(p.Value.Anchor-square.transform.position).sqrMagnitude;if(d<distance){distance=d;v=p.Value;}}
            }
            if(v&&v.AwaitingClaim){v.AcceptClaim();return v;}
            if(!seats.TryGetValue(square,out var s)||!s||!s.view||s.lease!=s.view.Lease)return null;
            v=s.view;s.view=null;s.claimed=true;s.Unregister();s.Hide();return v;
        }
        public static void ClearPending(){pending.Clear();}
        void LateUpdate(){if(!item||item.destroying||item.currentType!=ItemsTypes.BOMB){Stop();return;}if(claimed||!view)return;Register();Hide();view.SetAnchor(item.transform.position);}
        void Stop(){if(view&&view.Lease==lease)view.Release();view=null;Unregister();}
        void OnDisable(){Stop();}void OnDestroy(){Stop();}
    }
}
