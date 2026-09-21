using System;
using System.Collections.Generic;
using UnityEngine;
using JellyGarden.Scripts;
using JellyGarden.Scripts.Juice;
namespace RoyalThrone.Boosters.ColorBall.V2
{
    public enum ColorBallEvent { ColorBallRevealStarted,ColorBallRevealFinished,ColorBallActivationStarted,ColorBallChargeComplete,ColorBallBeamFired,ColorBallBeamHit,ColorBallTargetConverted,ColorBallSequenceFinished,ColorBallFinalPulse,ColorBallCompleted }
    [DefaultExecutionOrder(1100)]
    public sealed class ColorBallVisualV2 : MonoBehaviour
    {
        public enum Phase { Free,Reveal,Idle,Charge,Charged,Finish }
        public const float RevealDuration=.66f,ChargeDuration=.60f,FinishDuration=.18f;
        public Transform bodyLayer,colorEnergyLayer,vfxRoot;
        public Renderer body,energy,halo,core,orbit;
        public Phase Current {get;private set;}
        public float Age {get;private set;} public float Angle {get;private set;}
        public float Speed {get;private set;} public float Charge {get;private set;}
        public float Cell {get;private set;} public int Lease {get;private set;}
        public Vector3 Anchor {get;private set;}
        public bool AwaitingClaim {get;private set;}float claimAge;
        public void HoldForClaim(){AwaitingClaim=true;claimAge=0;}
        public void AcceptClaim(){AwaitingClaim=false;}
        public static event Action<ColorBallEvent,ColorBallVisualV2,Vector3> Event;
        static readonly List<ColorBallVisualV2> pool=new List<ColorBallVisualV2>();static int serial;
        MaterialPropertyBlock props;
        void Awake(){props=new MaterialPropertyBlock();}
        float pulse,sparkClock,idleClock;bool finalFlash,revealSpark;
        public static int PoolSize=>pool.Count;
        public static int Active{get{int n=0;foreach(var v in pool)if(v&&v.Current!=Phase.Free)n++;return n;}}
        public static bool Paused=>LevelManager.THIS&&LevelManager.THIS.gameStatus==GameState.Pause;
        public static float CellSize=>ColorBallFrameAnimator.Cell;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset(){pool.Clear();serial=0;Event=null;}
        public static void Prewarm(){if(pool.Count>0)return;var prefab=Resources.Load<GameObject>("ColorBall-V2");if(!prefab)throw new InvalidOperationException("ColorBall V2 prefab is missing.");for(int i=0;i<12;i++){var v=Instantiate(prefab).GetComponent<ColorBallVisualV2>();v.gameObject.SetActive(false);pool.Add(v);}ColorBallVfxV2.Prewarm();ColorBallAudioV2.Prewarm();}
        public static ColorBallVisualV2 Take(Vector3 position,bool reveal=false)
        {
            Prewarm();ColorBallVisualV2 v=null;foreach(var c in pool)if(c&&c.Current==Phase.Free){v=c;break;}
            // Board limit bounds the pool; additional seated balls are cached once, never per beam.
            if(!v){v=Instantiate(Resources.Load<GameObject>("ColorBall-V2")).GetComponent<ColorBallVisualV2>();pool.Add(v);}
            v.Lease=++serial;v.Cell=CellSize;v.Angle=0;v.Speed=.025f;v.Charge=0;v.pulse=0;v.sparkClock=0;v.idleClock=0;v.finalFlash=false;
            v.SetAnchor(position);v.gameObject.SetActive(true);
            int layer=LevelManager.THIS.GameField.gameObject.layer;
            foreach(var r in v.GetComponentsInChildren<Renderer>()){r.gameObject.layer=layer;r.sortingLayerID=SortingLayer.NameToID("Default");}
            v.Current=Phase.Idle;v.Age=0;if(reveal)v.Reveal();else v.Draw();return v;
        }
        public void SetAnchor(Vector3 position){Anchor=position;transform.position=position+Vector3.back*.7f;transform.rotation=Quaternion.identity;transform.localScale=Vector3.one*Cell;}
        public void Reveal(){Current=Phase.Reveal;Age=0;Speed=.025f;Charge=0;revealSpark=false;Send(ColorBallEvent.ColorBallRevealStarted,Anchor);Draw();}
        public void Activate(){Current=Phase.Charge;Age=0;pulse=0;sparkClock=0;Send(ColorBallEvent.ColorBallActivationStarted,Anchor);Draw();}
        public void Finish(){if(Current==Phase.Finish||Current==Phase.Free)return;Current=Phase.Finish;Age=0;finalFlash=false;Send(ColorBallEvent.ColorBallSequenceFinished,Anchor);}
        public void Send(ColorBallEvent e,Vector3 p){ColorBallAudioV2.OnEvent(e,this);Event?.Invoke(e,this,p);}
        public void BeamFired(Vector3 target){pulse=.06f;ColorBallVfxV2.Pulse(Anchor,.18f,.16f,new Color(.8f,.94f,1));Send(ColorBallEvent.ColorBallBeamFired,target);}
        void Update()
        {
            if(Current==Phase.Free||Paused)return;
            float dt=Time.deltaTime;
            if(AwaitingClaim){claimAge+=dt;if(claimAge>.25f){Release();return;}}
            Age+=dt;pulse=Mathf.MoveTowards(pulse,0,dt*.7f);
            float target=Current==Phase.Charge?Mathf.Lerp(.025f,7.2f,Smooth(Age/ChargeDuration)):Current==Phase.Charged?7.2f:Current==Phase.Finish?Mathf.Lerp(7.2f,9,Smooth(Age/FinishDuration)):.025f;
            // The charge curve has zero endpoint derivatives; no angular velocity snap at the loop.
            float prior=Speed;Speed=target;Angle=Mathf.Repeat(Angle+(prior+Speed)*.5f*dt,Mathf.PI*2);
            if(Current==Phase.Reveal&&!revealSpark&&Age>=.14f){revealSpark=true;ColorBallVfxV2.Stars(Anchor,5,.32f);}
            if(Current==Phase.Reveal&&Age>=RevealDuration){Current=Phase.Idle;Age-=RevealDuration;Send(ColorBallEvent.ColorBallRevealFinished,Anchor);}
            if(Current==Phase.Charge){Charge=Smooth(Age/ChargeDuration);if(Age>=ChargeDuration){Current=Phase.Charged;Age-=ChargeDuration;Charge=1;Send(ColorBallEvent.ColorBallChargeComplete,Anchor);}}
            if(Current==Phase.Idle){Charge=0;idleClock+=dt;if(idleClock>4.8f){idleClock=0;ColorBallVfxV2.Spark(Anchor+new Vector3(-.20f,.24f,0)*Cell,.08f,.42f,new Color(.7f,.86f,1,.4f));}}
            if(Current==Phase.Charge||Current==Phase.Charged){sparkClock+=dt;if(sparkClock>.18f&&Charge>.25f){sparkClock=0;float a=Angle*1.7f;ColorBallVfxV2.Spark(Anchor+new Vector3(Mathf.Cos(a)*.48f,Mathf.Sin(a)*.30f,0)*Cell,.12f,.23f,new Color(1,.94f,.68f,.75f));}}
            if(Current==Phase.Finish){Charge=1-Smooth(Age/FinishDuration);if(!finalFlash&&Age>=.09f){finalFlash=true;ColorBallVfxV2.Pulse(Anchor,.7f,.13f,Color.white);ColorBallVfxV2.Stars(Anchor,5,.28f);Send(ColorBallEvent.ColorBallFinalPulse,Anchor);}if(Age>=FinishDuration){Send(ColorBallEvent.ColorBallCompleted,Anchor);Release();return;}}
            Draw();
        }
        static float Smooth(float t){t=Mathf.Clamp01(t);return t*t*(3-2*t);}
        void Draw()
        {
            float x=1,y=1,dots=1,opacity=1,flash=0;
            if(Current==Phase.Reveal){float t=Age/RevealDuration;float grow=Smooth(t/.55f);float rebound=Mathf.Sin(Mathf.Clamp01((t-.34f)/.66f)*Mathf.PI*2)*.095f*(1-Smooth((t-.58f)/.42f));x=Mathf.Lerp(.24f,1,grow)+rebound;y=Mathf.Lerp(.16f,1,Smooth(t/.65f))+rebound*.60f;dots=Smooth((t-.10f)/.35f);flash=Mathf.Sin(Mathf.Clamp01((t-.17f)/.38f)*Mathf.PI)*.27f;}
            else if(Current==Phase.Charge){float t=Mathf.Clamp01(Age/ChargeDuration);float compress=Mathf.Sin(Mathf.Clamp01(t/.35f)*Mathf.PI);x=1+.04f*compress+.08f*Charge;y=1-.10f*compress-.04f*Charge;}
            else if(Current==Phase.Charged){x=1.08f+pulse;y=.96f+pulse*.5f;}
            else if(Current==Phase.Finish){float t=Smooth(Age/FinishDuration);x=Mathf.Lerp(1.08f,.12f,t);y=Mathf.Lerp(.96f,.12f,t);opacity=1-Smooth((Age/FinishDuration-.50f)/.50f);}
            var scale=new Vector3(.84f*x,.84f*y,1);
            bodyLayer.localScale=colorEnergyLayer.localScale=scale;
            bodyLayer.localPosition=colorEnergyLayer.localPosition=Vector3.zero;
            props.Clear();props.SetFloat("_Opacity",opacity);props.SetFloat("_Charge",Charge);body.SetPropertyBlock(props);
            props.SetFloat("_Opacity",opacity*dots);props.SetFloat("_Angle",Angle);props.SetFloat("_Exposure",Mathf.Max(0,Speed-.3f)*.016f);energy.SetPropertyBlock(props);
            Light(halo,new Color(.38f,.65f,1,(Charge*.20f+flash)*opacity),new Vector3(1.55f,1.45f,1));
            Light(core,new Color(1,.97f,.89f,(Charge*.60f+flash)*opacity),Vector3.one*(.56f+.11f*Charge));
            Light(orbit,new Color(.60f,.83f,1,Charge*.40f*opacity),new Vector3(1.26f,.42f,1));orbit.transform.localRotation=Quaternion.Euler(0,0,9*Mathf.Sin(Angle*.7f));
        }
        void Light(Renderer r,Color c,Vector3 s){props.Clear();props.SetColor("_Color",c);r.SetPropertyBlock(props);r.transform.localScale=s;r.enabled=c.a>.001f;}
        public void Release(){AwaitingClaim=false;ColorBallAudioV2.StopCharge(Lease);Current=Phase.Free;Age=0;Charge=0;Speed=0;Angle=0;gameObject.SetActive(false);}
        public static void Clear(){ColorBallSeatV2.ClearPending();foreach(var v in pool)if(v)v.Release();ColorBallVfxV2.Clear();ColorBallAudioV2.Clear();}
        void OnDisable(){ColorBallAudioV2.StopCharge(Lease);Current=Phase.Free;}
    }
}
