using System;
using System.Collections.Generic;
using UnityEngine;
using JellyGarden.Scripts;
using JellyGarden.Scripts.Juice;
using RoyalThrone.ReferenceGameplay;
namespace RoyalThrone.Boosters.ColorBall.V4
{
    public enum ColorBallEvent { ColorBallRevealStarted,ColorBallRevealFinished,ColorBallActivationStarted,ColorBallChargeComplete,ColorBallBeamPrepared,ColorBallBeamFired,ColorBallBeamHit,ColorBallTargetFlash,ColorBallTargetConverted,ColorBallSequenceFinished,ColorBallFinalPulse,ColorBallResolved,ColorBallCompleted }
    [DefaultExecutionOrder(1100)]
    public sealed class ColorBallVisualV4 : MonoBehaviour
    {
        public enum Phase { Free,Reveal,Idle,Charge,Charged,Finish,PayoffHold }
        public const float RevealDuration=.76f,ChargeDuration=.62f,FinishDuration=.30f,HandoffPause=.16f;
        // Artistic spin rate. Blurred footage does not establish a physical RPM.
        public const float IdleSpin=.065f,ChargedSpin=94f,FinishSpin=126f;
        public const float IdleOrbit=.12f,ChargedOrbit=11f,FinishOrbit=16f;
        // The smear covers the rotation of the frame being drawn; capped so it never averages to mush.
        public const float Shutter=.60f,MaxShutterArc=2.6f;
        // Never turn more than this in one drawn frame. At 60fps the charged rate lands at 90 deg a
        // frame, matching the reference; at 30fps the same rate would be 180 deg, which aliases into
        // mush however wide the shutter is, so the ball simply spins fewer revolutions per second
        // instead. Motion stays readable at either refresh rate.
        public const float MaxStepRad=1.75f;
        public const float ChargedWiden=1.80f;
        // Optional V5 prefab presentation. V4 prefab defaults remain unchanged.
        public bool referenceV5;
        public static bool UseV4Baseline;
        public float FinishSeconds=>referenceV5?FinishDuration+HandoffPause:FinishDuration;
        static GameObject Prefab=>!UseV4Baseline?Resources.Load<GameObject>("ColorBall-V5")??Resources.Load<GameObject>("ColorBall-V4"):Resources.Load<GameObject>("ColorBall-V4");
        public Transform bodyLayer,colorSurfaceLayer,vfxRoot,beamOrigin;
        public Renderer body,energy,halo,core,orbitBack,orbitFront,orbitGlint,originFlare;
        public Phase Current {get;private set;}
        public float Age {get;private set;} public float Angle {get;private set;}
        public float Speed {get;private set;} public float Charge {get;private set;}
        public float OrbitAngle {get;private set;} public float OrbitSpeed {get;private set;}
        public float Cell {get;private set;} public int Lease {get;private set;}
        public Vector3 Anchor {get;private set;}
        // A read-only presentation anchor. Targeting continues to use Anchor.
        public Vector3 VisibleOrigin=>UsesNative?imported.SourceOrigin:beamOrigin?beamOrigin.position:Anchor;
        ReferenceActivationVisual imported; ReferenceVfxPool.Handle nativeUse; int nativeRay;
        static readonly string[] nativeRays={"lightball_ray1__15747","lightball_ray2__15961","lightball_ray3__15912"};
        public bool UsesNative=>imported&&imported.Active;
        public static ColorBallVisualV4 SourceAt(Vector3 anchor){foreach(var v in pool)if(v&&v.referenceV5&&(v.Current==Phase.Charge||v.Current==Phase.Charged)&&(v.Anchor-anchor).sqrMagnitude<.001f)return v;return null;}
        public bool AwaitingClaim {get;private set;} float claimAge;
        public float SourcePulse=>Mathf.Sin(Mathf.Clamp01(sourceAge/.14f)*Mathf.PI);
        public void HoldForClaim(){AwaitingClaim=true;claimAge=0;}
        public static ColorBallVisualV4 ClaimPending(Vector3 position){ColorBallVisualV4 best=null;float distance=float.MaxValue;foreach(var v in pool)if(v&&v.AwaitingClaim){float d=(v.Anchor-position).sqrMagnitude;if(d<distance){best=v;distance=d;}}if(best)best.AwaitingClaim=false;return best;}
        public static event Action<ColorBallEvent,ColorBallVisualV4,Vector3> Event;
        static readonly List<ColorBallVisualV4> pool=new List<ColorBallVisualV4>();static int serial;
        MaterialPropertyBlock props;float sourceAge=1,lifeAge,idleClock,beamAngle,lastDt=1f/60f,lastStep;bool finalFlash,revealSpark;
        /// <summary>The arc the surface shader is smearing this frame, in radians. Drives the spin SFX.</summary>
        public float ShutterArc {get;private set;}
        /// <summary>0 at rest, 1 once the charged high-speed state is established.</summary>
        public float SpinEnergy=>Mathf.Clamp01(Speed/ChargedSpin);
        void Awake(){props=new MaterialPropertyBlock();}
        public static int PoolSize=>pool.Count;
        public static int Active{get{int n=0;foreach(var v in pool)if(v&&v.Current!=Phase.Free)n++;return n;}}
        public static bool Paused=>LevelManager.THIS&&LevelManager.THIS.gameStatus==GameState.Pause;
        public static float CellSize=>ColorBallFrameAnimator.Cell;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset(){pool.Clear();serial=0;Event=null;UseV4Baseline=false;}
        // count lets the level-start warm-up build the pool one visual per frame (RoyalGameplayWarmup); first use still fills all 12.
        public static void Prewarm(int count=12){if(pool.Count>=count)return;var prefab=Prefab;if(!prefab)throw new InvalidOperationException("ColorBall V4 prefab is missing.");while(pool.Count<count){var v=Instantiate(prefab).GetComponent<ColorBallVisualV4>();v.gameObject.SetActive(false);pool.Add(v);}ColorBallVfxV4.Prewarm();ColorBallAudioV4.Prewarm();}
        public static ColorBallVisualV4 Take(Vector3 position,bool reveal=false){
            Prewarm();ColorBallVisualV4 v=null;foreach(var c in pool)if(c&&c.Current==Phase.Free){v=c;break;}
            if(!v){v=Instantiate(Prefab).GetComponent<ColorBallVisualV4>();pool.Add(v);}
            v.Lease=++serial;v.Cell=CellSize;v.Angle=v.OrbitAngle=0;v.Speed=.065f;v.OrbitSpeed=.12f;v.Charge=0;v.sourceAge=1;v.lifeAge=v.idleClock=0;v.finalFlash=false;v.AwaitingClaim=false;
            v.SetAnchor(position);v.gameObject.SetActive(true);
            int layer=LevelManager.THIS.GameField.gameObject.layer;
            foreach(var r in v.GetComponentsInChildren<Renderer>()){r.gameObject.layer=layer;r.sortingLayerID=SortingLayer.NameToID("Default");}
            v.Current=Phase.Idle;v.Age=0;v.nativeRay=0;
            v.imported=ReferenceActivationVisual.Attach(v.gameObject,"ColorBall",38,v.GetComponent<RoyalThrone.Boosters.ColorBall.V5.ColorBallV5Presentation>());
            if(v.UsesNative)v.imported.Play("LightballDefaultAnimation");
            if(reveal)v.Reveal();else v.Draw();return v;
        }
        public void SetAnchor(Vector3 position){Anchor=position;transform.position=position+Vector3.back*.7f;transform.rotation=Quaternion.identity;transform.localScale=Vector3.one*Cell;}
        public void Reveal(){Current=Phase.Reveal;Age=0;Speed=.065f;OrbitSpeed=.12f;Charge=0;revealSpark=false;if(UsesNative)imported.Play("LightballCreationAnimation");Send(ColorBallEvent.ColorBallRevealStarted,Anchor);Draw();}
        public void Activate(){Current=Phase.Charge;Age=0;sourceAge=1;if(UsesNative){imported.Play("LightballUseAnimation");nativeUse=ReferenceVfxPool.Play("LightballUseParticles",VisibleOrigin,-1,Cell,true);}Send(ColorBallEvent.ColorBallActivationStarted,Anchor);Draw();}
        public void Finish(){if(Current==Phase.Finish||Current==Phase.PayoffHold||Current==Phase.Free)return;Current=Phase.Finish;Age=0;finalFlash=false;Send(ColorBallEvent.ColorBallSequenceFinished,Anchor);}
        public void Send(ColorBallEvent e,Vector3 p){
            if(UsesNative){
                switch(e){
                    case ColorBallEvent.ColorBallRevealStarted:ReferenceGameplayAudio.Play("lightball_creation__15586");break;
                    case ColorBallEvent.ColorBallActivationStarted:imported.PlayOwned("lightball_start__15692");break;
                    case ColorBallEvent.ColorBallBeamFired:ReferenceGameplayAudio.Play(nativeRays[nativeRay++%nativeRays.Length]);break;
                    case ColorBallEvent.ColorBallFinalPulse:imported.StopOwned();nativeUse?.Stop();ReferenceVfxPool.Play("LightballItemExplodeParticles",VisibleOrigin,-1,Cell);ReferenceGameplayAudio.Play("lightball_explode__15567");break;
                    case ColorBallEvent.ColorBallCompleted:imported.StopOwned();break;
                }
            }else ColorBallAudioV4.OnEvent(e,this);
            Event?.Invoke(e,this,p);
        }
        public void PrepareBeam(Vector3 target){sourceAge=0;var d=target-Anchor;beamAngle=Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg;Send(ColorBallEvent.ColorBallBeamPrepared,target);}
        public void BeamFired(Vector3 target){Send(ColorBallEvent.ColorBallBeamFired,target);}
        void Update(){
            if(Current==Phase.Free||Paused)return;
            float dt=Time.deltaTime;lastDt=Mathf.Clamp(dt,1f/240f,1f/20f);if(AwaitingClaim){claimAge+=dt;if(claimAge>.30f){Release();return;}}
            Age+=dt;lifeAge+=dt;sourceAge+=dt;
            float target=Current==Phase.Charge?Mathf.Lerp(IdleSpin,ChargedSpin,Smooth(Age/ChargeDuration)):Current==Phase.Charged?ChargedSpin:Current==Phase.Finish?referenceV5?ChargedSpin:Mathf.Lerp(ChargedSpin,FinishSpin,Smooth(Age/FinishDuration)):Current==Phase.PayoffHold?FinishSpin:IdleSpin;
            if(referenceV5){if(Current==Phase.Reveal)target=Mathf.Lerp(ChargedSpin,IdleSpin,Smooth((Age-.24f)/.30f));else if(Current==Phase.Charge)target=Mathf.Lerp(IdleSpin,ChargedSpin,Smooth(Age/.15f));}
            float orbitTarget=Current==Phase.Charge?Mathf.Lerp(IdleOrbit,ChargedOrbit,Smooth(Age/ChargeDuration)):Current==Phase.Charged?ChargedOrbit:Current==Phase.Finish?referenceV5?ChargedOrbit:Mathf.Lerp(ChargedOrbit,FinishOrbit,Smooth(Age/FinishDuration)):Current==Phase.PayoffHold?FinishOrbit:IdleOrbit;
            float step=(Speed+target)*.5f*dt;if(!referenceV5)step=Mathf.Min(step,MaxStepRad);lastStep=step;
            Angle=Mathf.Repeat(Angle+step,Mathf.PI*2);Speed=target;
            OrbitAngle=Mathf.Repeat(OrbitAngle+(OrbitSpeed+orbitTarget)*.5f*dt,Mathf.PI*2);OrbitSpeed=orbitTarget;
            if(!UsesNative&&Current==Phase.Reveal&&!revealSpark&&Age>=.21f){revealSpark=true;ColorBallVfxV4.Stars(Anchor,3,.30f);}
            if(Current==Phase.Reveal&&Age>=RevealDuration){Current=Phase.Idle;Age-=RevealDuration;if(UsesNative)imported.Play("LightballDefaultAnimation");Send(ColorBallEvent.ColorBallRevealFinished,Anchor);}
            if(Current==Phase.Charge){Charge=Smooth(Age/ChargeDuration);if(Age>=ChargeDuration){Current=Phase.Charged;Age-=ChargeDuration;Charge=1;Send(ColorBallEvent.ColorBallChargeComplete,Anchor);}}
            if(Current==Phase.Idle){Charge=0;idleClock+=dt;if(!UsesNative&&idleClock>6){idleClock=0;ColorBallVfxV4.Spark(Anchor+new Vector3(-.20f,.24f,0)*Cell,.075f,.42f,new Color(.7f,.86f,1,.3f));}}
            if(Current==Phase.Finish){
                Charge=1;
                if(!finalFlash&&Age>=(referenceV5?.18f:.21f)){
                    finalFlash=true;
                    // V5's flash/rim is drawn by its presentation component, on the same pool lease.
                    if(!UsesNative&&!referenceV5){ColorBallVfxV4.Pulse(Anchor,.92f,.19f,new Color(.8f,.92f,1));ColorBallVfxV4.Spark(Anchor,.75f,.18f,Color.white);ColorBallVfxV4.Stars(Anchor,3,.20f);}
                    Send(ColorBallEvent.ColorBallFinalPulse,Anchor);
                }
                if(Age>=FinishSeconds){Send(ColorBallEvent.ColorBallResolved,Anchor);if(referenceV5){Send(ColorBallEvent.ColorBallCompleted,Anchor);Release();return;}Current=Phase.PayoffHold;Age=0;}
            }
            if(Current==Phase.PayoffHold&&Age>=HandoffPause){Send(ColorBallEvent.ColorBallCompleted,Anchor);Release();return;}
            Draw();
        }
        static float Smooth(float t){t=Mathf.Clamp01(t);return t*t*(3-2*t);}
        static float RevealScale(float t){if(t<.11f)return Mathf.Lerp(.14f,.20f,Smooth(t/.11f));if(t<.60f)return Mathf.Lerp(.20f,1.11f,Smooth((t-.11f)/.49f));if(t<.82f)return Mathf.Lerp(1.11f,.985f,Smooth((t-.60f)/.22f));return Mathf.Lerp(.985f,1,Smooth((t-.82f)/.18f));}
        void Draw(){
            if(UsesNative){if(nativeUse?.GameObject)nativeUse.GameObject.transform.position=VisibleOrigin;return;}
            float x=1,y=1,dots=1,opacity=1,flash=0,orbitScale=1;
            if(Current==Phase.Reveal){float t=Age/RevealDuration;x=RevealScale(t);y=x*Mathf.Lerp(.58f,1,Smooth(t/.55f));dots=Smooth((t-.18f)/.30f);flash=Mathf.Sin(Mathf.Clamp01((t-.20f)/.40f)*Mathf.PI)*.42f;}
            else if(Current==Phase.Charge){float t=Mathf.Clamp01(Age/ChargeDuration),compress=Mathf.Sin(Mathf.Clamp01(t/.35f)*Mathf.PI);x=1+.03f*compress+.035f*Charge;y=1-.075f*compress;}
            else if(Current==Phase.Charged){x=1.035f+.03f*SourcePulse;y=1+.025f*SourcePulse;}
            else if(Current==Phase.Finish){float t=Age/FinishDuration,shrink=Smooth((t-.26f)/.74f);x=Mathf.Lerp(1.035f,.035f,shrink);y=Mathf.Lerp(1,.035f,shrink);orbitScale=Mathf.Lerp(1,.025f,Smooth(t));opacity=1-Smooth((t-.70f)/.30f);flash=Mathf.Sin(Mathf.Clamp01(t)*Mathf.PI)*.6f;}
            else if(Current==Phase.PayoffHold)opacity=0;
            // Widen stretches the quad itself, so body and surface share one widened silhouette and
            // one pivot; the charged form spreads sideways the way the reference does.
            // The reference blurs first and only then flattens: it is still round about eight frames
            // after full blur and reaches its widest around thirteen. So the widen trails the charge
            // rather than arriving with it.
            float widenT=Smooth(Mathf.Clamp01((Charge-.35f)/.65f));
            float widen=Mathf.Lerp(1,ChargedWiden,widenT)*(Current==Phase.Finish?Mathf.Lerp(1,.72f,Smooth(Age/FinishDuration)):1);
            var scale=referenceV5?new Vector3(.84f,.84f,1):new Vector3(.84f*x*widen,.84f*y,1);bodyLayer.localScale=colorSurfaceLayer.localScale=scale;bodyLayer.localPosition=colorSurfaceLayer.localPosition=Vector3.zero;
            // The shutter is the arc actually turned this frame, so the smear tiles frame to frame at
            // any refresh rate instead of being a fixed fraction that strobes when the speed rises.
            ShutterArc=Mathf.Min(lastStep*Shutter,MaxShutterArc);
            float samples=Mathf.Clamp(Mathf.Ceil(ShutterArc*Mathf.Rad2Deg/7f),6,24);
            props.Clear();props.SetFloat("_Opacity",opacity);props.SetFloat("_Charge",Charge);props.SetFloat("_Widen",widen);body.SetPropertyBlock(props);
            props.SetFloat("_Opacity",opacity*dots);props.SetFloat("_Angle",Angle);props.SetFloat("_Exposure",ShutterArc);
            props.SetFloat("_Samples",samples);props.SetFloat("_Energy",Charge);props.SetFloat("_Core",Mathf.Clamp01(Charge*.62f+SourcePulse*.38f+flash*.8f));energy.SetPropertyBlock(props);
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
        public void Release(){AwaitingClaim=false;nativeUse?.Stop();nativeUse=null;if(imported)imported.Release();ColorBallAudioV4.StopCharge(Lease);Current=Phase.Free;Age=0;Charge=0;Speed=OrbitSpeed=Angle=OrbitAngle=0;sourceAge=1;gameObject.SetActive(false);}
        public static void Clear(){foreach(var v in pool)if(v)v.Release();ColorBallVfxV4.Clear();ColorBallAudioV4.Clear();}
        void OnDisable(){nativeUse?.Stop();nativeUse=null;if(imported)imported.Release();ColorBallAudioV4.StopCharge(Lease);Current=Phase.Free;}
    }
}
