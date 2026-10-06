using System;
using System.Collections.Generic;
using UnityEngine;
using JellyGarden.Scripts;
using JellyGarden.Scripts.Juice;
namespace RoyalThrone.Boosters.ColorBall.V3
{
    public enum ColorBallEvent { ColorBallRevealStarted,ColorBallRevealFinished,ColorBallActivationStarted,ColorBallChargeComplete,ColorBallBeamPrepared,ColorBallBeamFired,ColorBallBeamHit,ColorBallTargetFlash,ColorBallTargetConverted,ColorBallSequenceFinished,ColorBallFinalPulse,ColorBallResolved,ColorBallCompleted }
    [DefaultExecutionOrder(1100)]
    public sealed class ColorBallVisualV3 : MonoBehaviour
    {
        public enum Phase { Free,Reveal,Idle,Charge,Charged,Finish,PayoffHold }
        public const float RevealDuration=.76f,ChargeDuration=.62f,FinishDuration=.30f,HandoffPause=.16f;
        public Transform bodyLayer,colorSurfaceLayer,vfxRoot,beamOrigin;
        public Renderer body,energy,halo,core,orbitBack,orbitFront,orbitGlint,originFlare;
        public Phase Current {get;private set;}
        public float Age {get;private set;} public float Angle {get;private set;}
        public float Speed {get;private set;} public float Charge {get;private set;}
        public float OrbitAngle {get;private set;} public float OrbitSpeed {get;private set;}
        public float Cell {get;private set;} public int Lease {get;private set;}
        public Vector3 Anchor {get;private set;}
        public bool AwaitingClaim {get;private set;} float claimAge;
        public float SourcePulse=>Mathf.Sin(Mathf.Clamp01(sourceAge/.14f)*Mathf.PI);
        public void HoldForClaim(){AwaitingClaim=true;claimAge=0;}
        public static ColorBallVisualV3 ClaimPending(Vector3 position){ColorBallVisualV3 best=null;float distance=float.MaxValue;foreach(var v in pool)if(v&&v.AwaitingClaim){float d=(v.Anchor-position).sqrMagnitude;if(d<distance){best=v;distance=d;}}if(best)best.AwaitingClaim=false;return best;}
        public static event Action<ColorBallEvent,ColorBallVisualV3,Vector3> Event;
        static readonly List<ColorBallVisualV3> pool=new List<ColorBallVisualV3>();static int serial;
        MaterialPropertyBlock props;float sourceAge=1,lifeAge,idleClock,beamAngle;bool finalFlash,revealSpark;
        void Awake(){props=new MaterialPropertyBlock();}
        public static int PoolSize=>pool.Count;
        public static int Active{get{int n=0;foreach(var v in pool)if(v&&v.Current!=Phase.Free)n++;return n;}}
        public static bool Paused=>LevelManager.THIS&&LevelManager.THIS.gameStatus==GameState.Pause;
        public static float CellSize=>ColorBallFrameAnimator.Cell;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset(){pool.Clear();serial=0;Event=null;}
        public static void Prewarm(){if(pool.Count>0)return;var prefab=Resources.Load<GameObject>("ColorBall-V3");if(!prefab)throw new InvalidOperationException("ColorBall V3 prefab is missing.");for(int i=0;i<12;i++){var v=Instantiate(prefab).GetComponent<ColorBallVisualV3>();v.gameObject.SetActive(false);pool.Add(v);}ColorBallVfxV3.Prewarm();ColorBallAudioV3.Prewarm();}
        public static ColorBallVisualV3 Take(Vector3 position,bool reveal=false){
            Prewarm();ColorBallVisualV3 v=null;foreach(var c in pool)if(c&&c.Current==Phase.Free){v=c;break;}
            if(!v){v=Instantiate(Resources.Load<GameObject>("ColorBall-V3")).GetComponent<ColorBallVisualV3>();pool.Add(v);}
            v.Lease=++serial;v.Cell=CellSize;v.Angle=v.OrbitAngle=0;v.Speed=.065f;v.OrbitSpeed=.12f;v.Charge=0;v.sourceAge=1;v.lifeAge=v.idleClock=0;v.finalFlash=false;v.AwaitingClaim=false;
            v.SetAnchor(position);v.gameObject.SetActive(true);
            int layer=LevelManager.THIS.GameField.gameObject.layer;
            foreach(var r in v.GetComponentsInChildren<Renderer>()){r.gameObject.layer=layer;r.sortingLayerID=SortingLayer.NameToID("Default");}
            v.Current=Phase.Idle;v.Age=0;if(reveal)v.Reveal();else v.Draw();return v;
        }
        public void SetAnchor(Vector3 position){Anchor=position;transform.position=position+Vector3.back*.7f;transform.rotation=Quaternion.identity;transform.localScale=Vector3.one*Cell;}
        public void Reveal(){Current=Phase.Reveal;Age=0;Speed=.065f;OrbitSpeed=.12f;Charge=0;revealSpark=false;Send(ColorBallEvent.ColorBallRevealStarted,Anchor);Draw();}
        public void Activate(){Current=Phase.Charge;Age=0;sourceAge=1;Send(ColorBallEvent.ColorBallActivationStarted,Anchor);Draw();}
        public void Finish(){if(Current==Phase.Finish||Current==Phase.PayoffHold||Current==Phase.Free)return;Current=Phase.Finish;Age=0;finalFlash=false;Send(ColorBallEvent.ColorBallSequenceFinished,Anchor);}
        public void Send(ColorBallEvent e,Vector3 p){ColorBallAudioV3.OnEvent(e,this);Event?.Invoke(e,this,p);}
        public void PrepareBeam(Vector3 target){sourceAge=0;var d=target-Anchor;beamAngle=Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg;Send(ColorBallEvent.ColorBallBeamPrepared,target);}
        public void BeamFired(Vector3 target){Send(ColorBallEvent.ColorBallBeamFired,target);}
        void Update(){
            if(Current==Phase.Free||Paused)return;
            float dt=Time.deltaTime;if(AwaitingClaim){claimAge+=dt;if(claimAge>.30f){Release();return;}}
            Age+=dt;lifeAge+=dt;sourceAge+=dt;
            float target=Current==Phase.Charge?Mathf.Lerp(.065f,7.8f,Smooth(Age/ChargeDuration)):Current==Phase.Charged?7.8f:Current==Phase.Finish?Mathf.Lerp(7.8f,10.2f,Smooth(Age/FinishDuration)):Current==Phase.PayoffHold?10.2f:.065f;
            float orbitTarget=Current==Phase.Charge?Mathf.Lerp(.12f,8.6f,Smooth(Age/ChargeDuration)):Current==Phase.Charged?8.6f:Current==Phase.Finish?Mathf.Lerp(8.6f,13,Smooth(Age/FinishDuration)):Current==Phase.PayoffHold?13:.12f;
            Angle=Mathf.Repeat(Angle+(Speed+target)*.5f*dt,Mathf.PI*2);Speed=target;
            OrbitAngle=Mathf.Repeat(OrbitAngle+(OrbitSpeed+orbitTarget)*.5f*dt,Mathf.PI*2);OrbitSpeed=orbitTarget;
            if(Current==Phase.Reveal&&!revealSpark&&Age>=.21f){revealSpark=true;ColorBallVfxV3.Stars(Anchor,3,.30f);}
            if(Current==Phase.Reveal&&Age>=RevealDuration){Current=Phase.Idle;Age-=RevealDuration;Send(ColorBallEvent.ColorBallRevealFinished,Anchor);}
            if(Current==Phase.Charge){Charge=Smooth(Age/ChargeDuration);if(Age>=ChargeDuration){Current=Phase.Charged;Age-=ChargeDuration;Charge=1;Send(ColorBallEvent.ColorBallChargeComplete,Anchor);}}
            if(Current==Phase.Idle){Charge=0;idleClock+=dt;if(idleClock>6){idleClock=0;ColorBallVfxV3.Spark(Anchor+new Vector3(-.20f,.24f,0)*Cell,.075f,.42f,new Color(.7f,.86f,1,.3f));}}
            if(Current==Phase.Finish){Charge=1;if(!finalFlash&&Age>=.21f){finalFlash=true;ColorBallVfxV3.Pulse(Anchor,.92f,.19f,new Color(.8f,.92f,1));ColorBallVfxV3.Spark(Anchor,.75f,.18f,Color.white);ColorBallVfxV3.Stars(Anchor,3,.20f);Send(ColorBallEvent.ColorBallFinalPulse,Anchor);}if(Age>=FinishDuration){Current=Phase.PayoffHold;Age=0;Send(ColorBallEvent.ColorBallResolved,Anchor);}}
            if(Current==Phase.PayoffHold&&Age>=HandoffPause){Send(ColorBallEvent.ColorBallCompleted,Anchor);Release();return;}
            Draw();
        }
        static float Smooth(float t){t=Mathf.Clamp01(t);return t*t*(3-2*t);}
        static float RevealScale(float t){if(t<.11f)return Mathf.Lerp(.14f,.20f,Smooth(t/.11f));if(t<.60f)return Mathf.Lerp(.20f,1.11f,Smooth((t-.11f)/.49f));if(t<.82f)return Mathf.Lerp(1.11f,.985f,Smooth((t-.60f)/.22f));return Mathf.Lerp(.985f,1,Smooth((t-.82f)/.18f));}
        void Draw(){
            float x=1,y=1,dots=1,opacity=1,flash=0,orbitScale=1;
            if(Current==Phase.Reveal){float t=Age/RevealDuration;x=RevealScale(t);y=x*Mathf.Lerp(.58f,1,Smooth(t/.55f));dots=Smooth((t-.18f)/.30f);flash=Mathf.Sin(Mathf.Clamp01((t-.20f)/.40f)*Mathf.PI)*.42f;}
            else if(Current==Phase.Charge){float t=Mathf.Clamp01(Age/ChargeDuration),compress=Mathf.Sin(Mathf.Clamp01(t/.35f)*Mathf.PI);x=1+.03f*compress+.035f*Charge;y=1-.075f*compress;}
            else if(Current==Phase.Charged){x=1.035f+.03f*SourcePulse;y=1+.025f*SourcePulse;}
            else if(Current==Phase.Finish){float t=Age/FinishDuration,shrink=Smooth((t-.26f)/.74f);x=Mathf.Lerp(1.035f,.035f,shrink);y=Mathf.Lerp(1,.035f,shrink);orbitScale=Mathf.Lerp(1,.025f,Smooth(t));opacity=1-Smooth((t-.70f)/.30f);flash=Mathf.Sin(Mathf.Clamp01(t)*Mathf.PI)*.6f;}
            else if(Current==Phase.PayoffHold)opacity=0;
            var scale=new Vector3(.84f*x,.84f*y,1);bodyLayer.localScale=colorSurfaceLayer.localScale=scale;bodyLayer.localPosition=colorSurfaceLayer.localPosition=Vector3.zero;
            props.Clear();props.SetFloat("_Opacity",opacity);props.SetFloat("_Charge",Charge);body.SetPropertyBlock(props);
            props.SetFloat("_Opacity",opacity*dots);props.SetFloat("_Angle",Angle);props.SetFloat("_Exposure",Mathf.Max(0,Speed-.3f)*.019f);energy.SetPropertyBlock(props);
            float breath=.5f+.5f*Mathf.Sin(lifeAge*2.1f);
            Light(halo,new Color(.34f,.63f,1,(Charge*.17f+flash*.45f)*opacity),new Vector3(1.5f,1.4f,1));
            Light(core,new Color(1,.97f,.90f,(.045f+.012f*breath+Charge*(.47f+.06f*breath)+flash)*opacity),Vector3.one*(.43f+.20f*Charge));
            float orbitAlpha=(.042f+.84f*Charge)*opacity;Orbit(orbitBack,orbitAlpha*.72f,orbitScale);Orbit(orbitFront,orbitAlpha,orbitScale);
            float theta=OrbitAngle;bool behind=Mathf.Sin(theta)>0;var tilt=orbitFront.transform.localRotation;
            orbitGlint.transform.localPosition=tilt*new Vector3(.66f*Mathf.Cos(theta),.22f*Mathf.Sin(theta),.25f*Mathf.Sin(theta))*orbitScale;
            orbitGlint.sortingOrder=behind?34:39;orbitGlint.transform.localRotation=Quaternion.Euler(0,0,theta*Mathf.Rad2Deg*.35f);
            Light(orbitGlint,new Color(.91f,.97f,1,Charge*.8f*opacity),Vector3.one*(.13f*orbitScale));
            float seed=Current==Phase.Reveal?1-Smooth(Age/.17f):0;float source=Current==Phase.Charged?SourcePulse:0;
            Light(originFlare,new Color(1,1,.93f,Mathf.Max(seed,source*.88f)*opacity),new Vector3(.34f+.14f*source,.22f+.08f*source,1));beamOrigin.localRotation=Quaternion.Euler(0,0,beamAngle);
        }
        void Orbit(Renderer r,float alpha,float scale){props.Clear();props.SetColor("_Color",new Color(.55f,.80f,1,alpha));props.SetFloat("_OrbitPhase",OrbitAngle);r.SetPropertyBlock(props);r.transform.localScale=Vector3.one*scale;r.transform.localRotation=Quaternion.Euler(0,0,12+5*Mathf.Sin(lifeAge*.8f));r.enabled=alpha>.001f;}
        void Light(Renderer r,Color c,Vector3 s){props.Clear();props.SetColor("_Color",c);r.SetPropertyBlock(props);r.transform.localScale=s;r.enabled=c.a>.001f;}
        public void Release(){AwaitingClaim=false;ColorBallAudioV3.StopCharge(Lease);Current=Phase.Free;Age=0;Charge=0;Speed=OrbitSpeed=Angle=OrbitAngle=0;sourceAge=1;gameObject.SetActive(false);}
        public static void Clear(){foreach(var v in pool)if(v)v.Release();ColorBallVfxV3.Clear();ColorBallAudioV3.Clear();}
        void OnDisable(){ColorBallAudioV3.StopCharge(Lease);Current=Phase.Free;}
    }
}
