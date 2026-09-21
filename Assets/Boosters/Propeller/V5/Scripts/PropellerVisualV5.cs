using System;
using System.Collections.Generic;
using JellyGarden.Scripts;
using JellyGarden.Scripts.Juice;
using UnityEngine;

namespace RoyalThrone.Boosters.Propeller.V5
{
    public enum PropellerEvent { PropellerRevealStarted,PropellerRevealFinished,PropellerActivationStarted,PropellerRotorAccelerating,PropellerNeighborHit,PropellerTakeoff,PropellerFlightReady,PropellerTargetReached,PropellerImpactStarted,PropellerImpactFinished,PropellerCompleted }
    public sealed class PropellerVisualV5 : MonoBehaviour
    {
        public enum Phase { Free,Reveal,Idle,Activation,Flight,Impact }
        public const float RevealSeconds=.52f,ActivationSeconds=.46f,TakeoffAt=.355f,ImpactSeconds=.18f,MotorAt=.055f;
        public const float FlightDegreesPerSecond=3300;
        public Transform pose,body,rotorPlane,rotor;
        public Renderer sweptRotor;
        public Renderer[] bladeViews;
        public Renderer[] solidViews;
        public float PresentationSpeed {get;private set;}
        public float Blur {get;private set;}
        public Phase Current {get;private set;}
        public float Age {get;private set;}
        public float RotorAngle {get;private set;}
        public float RotorSpeed {get;private set;}
        public float Cell {get;private set;}
        public int Lease {get;private set;}
        public bool FlightReady=>Current==Phase.Flight;
        public bool HasTakenOff=>lifted;
        public bool ImpactStarted=>impactStarted;
        public Vector3 Anchor=>transform.position;
        public Vector3 FlightPosition=>transform.position+pose.localPosition;
        public float FlightProgress {get;private set;}
        public float FlightDuration {get;set;}
        public static event Action<PropellerEvent,PropellerVisualV5,Vector3> Event;
        static readonly List<PropellerVisualV5> pool=new List<PropellerVisualV5>();
        static int leases;static float nextLaunch;
        MaterialPropertyBlock block;
        Vector3 startScale,startOffset;Quaternion startTurn;
        float alpha=1,lean,pitch,desiredLean,desiredPitch,startSpeed,impactAngle;
        bool lifted,motorStarted,impactStarted,revealFlash;int flightAccents;
        public static int Active {get{int n=0;foreach(var v in pool)if(v&&v.Current!=Phase.Free)n++;return n;}}
        public static int PoolSize=>pool.Count;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics(){pool.Clear();leases=0;nextLaunch=0;Event=null;}
        public static float ReserveLaunchDelay(){float slot=Mathf.Max(Time.time,nextLaunch);nextLaunch=slot+.07f;return slot-Time.time;}
        public static void Prewarm(int count=12)
        {
            pool.RemoveAll(v=>!v);var prefab=Resources.Load<PropellerVisualV5>("Propeller-V5");
            if(!prefab)throw new InvalidOperationException("Build the independent Propeller-V5 prefab first.");
            while(pool.Count<count){var v=Instantiate(prefab,RoyalPowerupDirector.Get.transform);v.block=new MaterialPropertyBlock();v.gameObject.SetActive(false);pool.Add(v);}
            PropellerVfxV5.Prewarm();PropellerAudioV5.Prewarm();
        }
        public static PropellerVisualV5 Take(Vector3 p,float cell,bool reveal)
        {
            if(pool.Count==0)Prewarm();PropellerVisualV5 v=null;
            foreach(var candidate in pool)if(candidate&&candidate.Current==Phase.Free){v=candidate;break;}
            if(!v){Prewarm(pool.Count+4);foreach(var candidate in pool)if(candidate&&candidate.Current==Phase.Free){v=candidate;break;}}
            v.Lease=++leases;v.Cell=cell;v.transform.SetPositionAndRotation(p+Vector3.back*.8f,Quaternion.identity);v.transform.localScale=Vector3.one;
            v.PresentationSpeed=v.Blur=0;v.RotorAngle=v.RotorSpeed=v.lean=v.pitch=v.desiredLean=v.desiredPitch=0;v.alpha=1;v.lifted=v.motorStarted=v.impactStarted=v.revealFlash=false;
            v.flightAccents=0;v.FlightDuration=0;v.FlightProgress=0;v.Current=Phase.Idle;v.Age=0;v.gameObject.SetActive(true);v.Pose(1,0,0,0,0);v.ApplyRotor();
            if(reveal)v.Reveal();return v;
        }
        public void Reveal(){Age=0;Current=Phase.Reveal;RotorAngle=RotorSpeed=0;revealFlash=false;Pose(0,.28f,0,0,0);Emit(PropellerEvent.PropellerRevealStarted,Anchor);}
        public void SetIdle(){Current=Phase.Idle;Age=0;RotorAngle=RotorSpeed=lean=pitch=0;Pose(1,0,0,0,0);ApplyRotor();}
        public void Activate(Vector3 target)
        {
            startScale=pose.localScale;startOffset=pose.localPosition;startTurn=pose.localRotation;
            desiredLean=Mathf.Clamp((target.x-Anchor.x)/Cell,-1,1)*-10;desiredPitch=4;
            startSpeed=RotorSpeed;Current=Phase.Activation;Age=0;lifted=motorStarted=false;
            Emit(PropellerEvent.PropellerActivationStarted,Anchor);
        }
        public Vector3 BeginFlightTranslation(){var p=FlightPosition;transform.position=p;pose.localPosition=Vector3.zero;return p;}
        public void Place(Vector3 p,Vector3 tangent,float progress)
        {
            transform.position=p;FlightProgress=Mathf.Clamp01(progress);
            var direction=((Vector2)tangent).normalized;
            desiredLean=-direction.x*10;desiredPitch=Mathf.Lerp(4,-4,Mathf.Clamp01((progress-.45f)/.55f));
        }
        public void NeighborHit(Vector3 p){PropellerVfxV5.Pressure(Anchor,p,Cell);Emit(PropellerEvent.PropellerNeighborHit,p);}
        public void Contact(Vector3 p)
        {
            transform.position=p+Vector3.back*.8f;pose.localPosition=Vector3.zero;
            startScale=pose.localScale;startTurn=pose.localRotation;Current=Phase.Impact;Age=0;impactStarted=false;
            impactAngle=lean;Emit(PropellerEvent.PropellerTargetReached,p);
        }
        public void Release(){PropellerAudioV5.StopFlight(Lease);Current=Phase.Free;Age=0;RotorSpeed=RotorAngle=0;alpha=1;gameObject.SetActive(false);}
        public static void Clear(){foreach(var v in pool)if(v)v.Release();nextLaunch=0;PropellerVfxV5.Clear();PropellerAudioV5.Clear();}
        void OnDisable(){PropellerAudioV5.StopFlight(Lease);Current=Phase.Free;RotorSpeed=0;}
        void Emit(PropellerEvent e,Vector3 p){PropellerAudioV5.OnEvent(e,this);Event?.Invoke(e,this,p);}
        void Update()
        {
            if(Current==Phase.Free||LevelManager.THIS&&LevelManager.THIS.gameStatus==GameState.Pause)return;
            Age+=Time.deltaTime;
            switch(Current){
                case Phase.Reveal:
                    float r=Mathf.Clamp01(Age/RevealSeconds);
                    RotorAngle=RotorReveal.Evaluate(r);RotorSpeed=0;
                    lean=4.5f*Mathf.Sin(r*Mathf.PI*3)*(1-r);pitch=0;
                    Pose(RevealScale.Evaluate(r),RevealSquash.Evaluate(r),.055f*Mathf.Sin(r*Mathf.PI),lean,0);
                    rotorPlane.localRotation=Quaternion.Euler(53+Mathf.Sin(r*Mathf.PI*3)*9*(1-r),0,0);
                    if(!revealFlash&&Age>=.085f){revealFlash=true;PropellerVfxV5.Play(PropellerVfxV5.Kind.Reveal,Anchor,Cell);}
                    if(r>=1){SetIdle();Emit(PropellerEvent.PropellerRevealFinished,Anchor);}break;
                case Phase.Idle:
                    float wave=Mathf.Sin(Age*1.55f);RotorAngle=2.6f*Mathf.Sin(Age*1.13f);
                    Pose(1+.0035f*wave,0,.005f*wave,-.55f*wave,0);break;
                case Phase.Activation:
                    float k=Mathf.Clamp01(Age/ActivationSeconds);
                    if(!motorStarted&&Age>=MotorAt){motorStarted=true;Emit(PropellerEvent.PropellerRotorAccelerating,Anchor);}
                    RotorSpeed=Mathf.Max(startSpeed,MotorSpeed.Evaluate(Age));AdvanceRotor();
                    float release=Mathf.SmoothStep(0,1,Mathf.InverseLerp(TakeoffAt,ActivationSeconds,Age));
                    lean=Mathf.Lerp(4.5f*Mathf.Sin(Mathf.PI*Mathf.Clamp01(Age/TakeoffAt)),desiredLean,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.375f,ActivationSeconds,Age)));pitch=4*release;
                    Pose(Mathf.Lerp(1,1.065f,release),ActivationSquash.Evaluate(Age),ActivationLift.Evaluate(Age),lean,pitch);
                    rotorPlane.localRotation=Quaternion.Euler(53-9*Mathf.Sin(k*Mathf.PI),0,0);
                    body.localRotation=Quaternion.Euler(-pitch*.5f,0,-lean*.33f-2.5f*Mathf.Sin(k*Mathf.PI));
                    float blend=Mathf.SmoothStep(0,1,Mathf.Clamp01(Age/.065f));
                    pose.localScale=Vector3.Lerp(startScale,pose.localScale,blend);pose.localPosition=Vector3.Lerp(startOffset,pose.localPosition,blend);pose.localRotation=Quaternion.Slerp(startTurn,pose.localRotation,blend);
                    if(!lifted&&Age>=TakeoffAt){lifted=true;PropellerVfxV5.Play(PropellerVfxV5.Kind.Takeoff,Anchor+Vector3.down*Cell*.20f,Cell);Emit(PropellerEvent.PropellerTakeoff,Anchor);}
                    if(k>=1){Current=Phase.Flight;Age=0;FlightProgress=0;Emit(PropellerEvent.PropellerFlightReady,FlightPosition);}break;
                case Phase.Flight:
                    RotorSpeed=FlightDegreesPerSecond;AdvanceRotor();
                    float response=1-Mathf.Exp(-Time.deltaTime*17);
                    lean=Mathf.Lerp(lean,desiredLean,response);pitch=Mathf.Lerp(pitch,desiredPitch,response);
                    float size=FlightProgress<.72f?Mathf.Lerp(1.065f,1.02f,FlightProgress/.72f):Mathf.Lerp(1.02f,.97f,Mathf.SmoothStep(0,1,(FlightProgress-.72f)/.28f));
                    pose.localScale=Vector3.one*Cell*size;pose.localRotation=Quaternion.Euler(pitch,0,lean+Mathf.Sin(Age*36)*.45f*Mathf.Min(1,Age*12));
                    body.localRotation=Quaternion.Euler(-pitch*.5f,0,-lean*.18f);rotorPlane.localRotation=Quaternion.Euler(53,0,0);break;
                case Phase.Impact:
                    float q=Mathf.Clamp01(Age/ImpactSeconds);AdvanceRotor();
                    float compression=Mathf.Sin(Mathf.Clamp01(Age/.055f)*Mathf.PI)*.24f;
                    float disappear=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.045f,ImpactSeconds,Age));
                    pose.localScale=Vector3.Scale(startScale,new Vector3(1+compression*.55f,1-compression,1))*(1-.78f*disappear);
                    pose.localRotation=startTurn;body.localRotation=Quaternion.Euler(0,0,-impactAngle*.18f);
                    if(!impactStarted&&Age>=.022f){impactStarted=true;PropellerVfxV5.Play(PropellerVfxV5.Kind.Impact,Anchor,Cell);Emit(PropellerEvent.PropellerImpactStarted,Anchor);}
                    alpha=1-disappear;
                    if(q>=1){Emit(PropellerEvent.PropellerImpactFinished,Anchor);Emit(PropellerEvent.PropellerCompleted,Anchor);Release();return;}break;
            }
            RotorAngle=Mathf.Repeat(RotorAngle,360);ApplyRotor();
        }
        void Pose(float size,float squash,float lift,float z,float x)
        {
            pose.localScale=Vector3.one*Cell*size;pose.localPosition=Vector3.up*Cell*lift;pose.localRotation=Quaternion.Euler(x,0,z);
            body.localScale=new Vector3(1+squash*.45f,1-squash,1);body.localRotation=Quaternion.Euler(-x*.5f,0,-z*.25f);
            rotorPlane.localPosition=new Vector3(0,.17f-squash*.12f,-.20f);rotorPlane.localRotation=Quaternion.Euler(53,0,0);
        }
        // Motor speed drives blur and audio. The visible phase is band-limited below
        // the three-blade Nyquist limit at 30 Hz; never jumps backwards or per-frame.
        public static float VisibleRate(float motor)=>motor<=600?motor:600+180*(1-Mathf.Exp(-(motor-600)/600));
        void AdvanceRotor(){PresentationSpeed=VisibleRate(RotorSpeed);RotorAngle+=PresentationSpeed*Time.deltaTime;}
        void ApplyRotor()
        {
            rotor.localRotation=Quaternion.Euler(0,0,RotorAngle);
            Blur=Mathf.SmoothStep(0,1,Mathf.InverseLerp(450,2300,RotorSpeed));
            foreach(var view in solidViews)SetAlpha(view,alpha);
            foreach(var view in bladeViews)SetAlpha(view,alpha*(1-Blur));
            sweptRotor.enabled=Blur>.001f&&alpha>.001f;
            if(sweptRotor.enabled){block.Clear();block.SetFloat("_Phase",RotorAngle*Mathf.Deg2Rad);block.SetFloat("_Sweep",Mathf.Lerp(15,68,Blur)*Mathf.Deg2Rad);block.SetFloat("_Opacity",alpha*Blur*.95f);sweptRotor.SetPropertyBlock(block);}
        }
        void SetAlpha(Renderer r,float a){r.enabled=a>.001f;if(!r.enabled)return;var c=r.sharedMaterial.color;c.a=a;block.Clear();block.SetColor("_Color",c);r.SetPropertyBlock(block);}
        static AnimationCurve Curve(params float[] p){var keys=new Keyframe[p.Length/2];for(int i=0;i<keys.Length;i++)keys[i]=new Keyframe(p[i*2],p[i*2+1]);return new AnimationCurve(keys);}
        static readonly AnimationCurve RevealScale=Curve(0,0,.08f,.10f,.20f,.70f,.37f,1.17f,.65f,.96f,.85f,1.015f,1,1);
        static readonly AnimationCurve RevealSquash=Curve(0,.30f,.20f,.20f,.37f,-.18f,.65f,.09f,.85f,-.025f,1,0);
        static readonly AnimationCurve RotorReveal=Curve(0,-16,.22f,-10,.42f,33,.70f,-7,.86f,2,1,0);
        static readonly AnimationCurve MotorSpeed=Curve(0,0,.04f,65,.10f,230,.17f,700,.24f,1900,.32f,3300,.46f,3300);
        static readonly AnimationCurve ActivationSquash=Curve(0,0,.14f,0,.25f,.12f,.35f,-.065f,.46f,0);
        static readonly AnimationCurve ActivationLift=new AnimationCurve(new Keyframe(0,0,0,0),new Keyframe(.14f,0,0,0),new Keyframe(.25f,-.065f,0,0),new Keyframe(.46f,.58f,1.2f,1.2f));
    }
}
