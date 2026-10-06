using System;
using System.Collections.Generic;
using JellyGarden.Scripts;
using JellyGarden.Scripts.Juice;
using UnityEngine;

namespace RoyalThrone.Boosters.Propeller.V2
{
    public enum PropellerEvent { PropellerCreated,PropellerRevealComplete,PropellerActivationStarted,PropellerNeighborHit,PropellerTakeoff,PropellerFlightReady,PropellerTargetReached,PropellerImpact,PropellerCompleted }
    public sealed class PropellerVisualV2 : MonoBehaviour
    {
        public enum Phase { Free,Reveal,Idle,Activation,Flight,Impact }
        public const float RevealSeconds=.54f,ActivationSeconds=.34f,TakeoffAt=.14f,ImpactSeconds=.12f;
        public const float FlightRpm=400; // 2400 degrees/sec; actual swept blades, not a replacement disc.
        public Transform pose,body,rotorPlane,rotor;
        public Transform[] echoes;
        public Renderer[] solidViews,echoViews;
        public Phase Current {get;private set;}
        public float Age {get;private set;}
        public float RotorAngle {get;private set;}
        public float RotorSpeed {get;private set;}
        public int Lease {get;private set;}
        public float Cell {get;private set;}
        public bool FlightReady=>Current==Phase.Flight;
        public Vector3 Anchor=>transform.position;
        public Vector3 FlightPosition=>transform.position+pose.localPosition;
        public static event Action<PropellerEvent,PropellerVisualV2,Vector3> Event;
        static readonly List<PropellerVisualV2> pool=new List<PropellerVisualV2>();
        static int leases;
        public static int Active {get{int n=0;foreach(var v in pool)if(v&&v.Current!=Phase.Free)n++;return n;}}
        public static int PoolSize=>pool.Count;
        MaterialPropertyBlock block;
        float startAngle,startSpeed,heading,alpha=1;Vector3 startScale,startOffset;Quaternion startTurn;
        bool lifted;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics(){pool.Clear();leases=0;Event=null;}
        public static void Prewarm(int count=12)
        {
            pool.RemoveAll(v=>!v);var prefab=Resources.Load<PropellerVisualV2>("Propeller-V2");
            if(!prefab)throw new InvalidOperationException("Build the independent Propeller-V2 prefab first.");
            while(pool.Count<count){var v=Instantiate(prefab,RoyalPowerupDirector.Get.transform);v.name="Propeller-V2 pooled";v.block=new MaterialPropertyBlock();v.gameObject.SetActive(false);pool.Add(v);}
            PropellerVfxV2.Prewarm();
        }
        public static PropellerVisualV2 Take(Vector3 p,float cell,bool reveal)
        {
            if(pool.Count==0)Prewarm();PropellerVisualV2 v=null;
            foreach(var candidate in pool)if(candidate&&candidate.Current==Phase.Free){v=candidate;break;}
            if(!v){Prewarm(pool.Count+4);foreach(var candidate in pool)if(candidate.Current==Phase.Free){v=candidate;break;}}
            v.Lease=++leases;v.Cell=cell;v.transform.position=p+Vector3.back*.8f;
            v.transform.rotation=Quaternion.identity;v.transform.localScale=Vector3.one;
            v.Age=0;v.RotorAngle=v.RotorSpeed=0;v.heading=0;v.alpha=1;v.lifted=false;
            v.pose.localPosition=Vector3.zero;v.pose.localRotation=Quaternion.identity;
            v.pose.localScale=Vector3.one*cell;v.Current=reveal?Phase.Reveal:Phase.Idle;
            v.gameObject.SetActive(true);v.ApplyPose(1,0,0,0);v.ApplyRotor();
            if(reveal){v.ApplyPose(.20f,.22f,0,0);v.Emit(PropellerEvent.PropellerCreated,p);}
            return v;
        }
        public void Reveal(){Age=0;Current=Phase.Reveal;RotorAngle=RotorSpeed=0;ApplyPose(.20f,.22f,0,0);Emit(PropellerEvent.PropellerCreated,Anchor);}
        public void SetIdle(){Current=Phase.Idle;Age=0;RotorSpeed=RotorAngle=0;ApplyPose(1,0,0,0);ApplyRotor();}
        public void Activate(Vector3 destination)
        {
            startScale=pose.localScale;startOffset=pose.localPosition;startTurn=pose.localRotation;
            startAngle=RotorAngle;startSpeed=RotorSpeed;heading=Mathf.Clamp((destination.x-Anchor.x)/Cell,-1,1)*-12;
            Current=Phase.Activation;Age=0;lifted=false;Emit(PropellerEvent.PropellerActivationStarted,Anchor);
        }
        // Gameplay owns world translation. Capture the lifted pose before its local offset is absorbed.
        public Vector3 BeginFlightTranslation(){var p=FlightPosition;transform.position=p;pose.localPosition=Vector3.zero;return p;}
        public void Place(Vector3 p,Vector3 tangent){transform.position=p;heading=Mathf.Clamp(tangent.x/Cell,-1,1)*-12;}
        public void NeighborHit(Vector3 p){PropellerVfxV2.Play(PropellerVfxV2.Kind.Neighbor,p,Cell);Emit(PropellerEvent.PropellerNeighborHit,p);}
        public void Contact(Vector3 p)
        {
            transform.position=p+Vector3.back*.8f;pose.localPosition=Vector3.zero;
            startScale=pose.localScale;startTurn=pose.localRotation;Current=Phase.Impact;Age=0;
            Emit(PropellerEvent.PropellerTargetReached,p);PropellerVfxV2.Play(PropellerVfxV2.Kind.Impact,p,Cell);Emit(PropellerEvent.PropellerImpact,p);
        }
        public void Release(){Current=Phase.Free;Age=0;RotorSpeed=RotorAngle=0;alpha=1;gameObject.SetActive(false);}
        public static void Clear(){foreach(var v in pool)if(v)v.Release();PropellerVfxV2.Clear();}
        void OnDisable(){Current=Phase.Free;RotorSpeed=0;}
        void Emit(PropellerEvent e,Vector3 p){Event?.Invoke(e,this,p);}
        void Update()
        {
            if(Current==Phase.Free)return;
            if(LevelManager.THIS&&LevelManager.THIS.gameStatus==GameState.Pause)return;
            Age+=Time.deltaTime;
            switch(Current){
                case Phase.Reveal:
                    float t=Mathf.Clamp01(Age/RevealSeconds);
                    float scale=RevealScale.Evaluate(t),squash=RevealSquash.Evaluate(t);
                    RotorAngle=28*Mathf.Sin(t*Mathf.PI)*Mathf.Exp(-t*3);RotorSpeed=0;
                    ApplyPose(scale,squash,Mathf.Sin(t*Mathf.PI*4)*5*(1-t),0);
                    if(!lifted&&Age>=.075f){lifted=true;PropellerVfxV2.Play(PropellerVfxV2.Kind.Reveal,Anchor,Cell);}
                    if(t>=1){SetIdle();Emit(PropellerEvent.PropellerRevealComplete,Anchor);}break;
                case Phase.Idle:
                    float wave=Mathf.Sin(Age*1.8f);
                    RotorAngle=1.8f*Mathf.Sin(Age*1.2f);RotorSpeed=0;
                    ApplyPose(1+.0025f*wave,0,-wave*.25f,wave*.003f);break;
                case Phase.Activation:
                    float k=Mathf.Clamp01(Age/ActivationSeconds),ease=Mathf.SmoothStep(0,1,k);
                    RotorSpeed=Mathf.Lerp(startSpeed,FlightRpm*6,Mathf.SmoothStep(0,1,Mathf.Clamp01((k-.08f)/.76f)));
                    RotorAngle+=RotorSpeed*Time.deltaTime;
                    float squeeze=TakeoffSquash.Evaluate(k),rise=TakeoffLift.Evaluate(k);
                    ApplyPose(Mathf.Lerp(1,1.10f,ease),squeeze,Mathf.Lerp(0,heading,ease)+Mathf.Sin(k*Mathf.PI*2)*3,rise);
                    // Preserve an interrupted reveal/idle pose without a one-frame reset.
                    float blend=Mathf.SmoothStep(0,1,Mathf.Clamp01(k/.24f));
                    pose.localScale=Vector3.Lerp(startScale,pose.localScale,blend);
                    pose.localPosition=Vector3.Lerp(startOffset,pose.localPosition,blend);
                    pose.localRotation=Quaternion.Slerp(startTurn,pose.localRotation,blend);
                    if(!lifted&&Age>=TakeoffAt){lifted=true;PropellerVfxV2.Play(PropellerVfxV2.Kind.Takeoff,Anchor+Vector3.down*Cell*.22f,Cell);Emit(PropellerEvent.PropellerTakeoff,Anchor);}
                    if(k>=1){Current=Phase.Flight;Age=0;Emit(PropellerEvent.PropellerFlightReady,FlightPosition);}break;
                case Phase.Flight:
                    RotorSpeed=FlightRpm*6;RotorAngle+=RotorSpeed*Time.deltaTime;
                    // No translation here: even the landing is completely owned by gameplay.
                    pose.localRotation=Quaternion.Euler(0,0,heading+Mathf.Sin(Age*24)*.35f);
                    pose.localScale=Vector3.one*Cell*1.10f;break;
                case Phase.Impact:
                    float q=Mathf.Clamp01(Age/ImpactSeconds);RotorAngle+=RotorSpeed*Time.deltaTime;
                    pose.localScale=startScale*(1+.06f*Mathf.Sin(q*Mathf.PI)-.82f*q*q);
                    alpha=1-Mathf.SmoothStep(0,1,q);
                    if(q>=1){Emit(PropellerEvent.PropellerCompleted,Anchor);Release();return;}break;
            }
            RotorAngle=Mathf.Repeat(RotorAngle,360);ApplyRotor();
        }
        void ApplyPose(float size,float squash,float tilt,float lift)
        {
            pose.localScale=new Vector3(1+squash,1-squash,1)*Cell*size;
            pose.localPosition=Vector3.up*(lift*Cell);pose.localRotation=Quaternion.Euler(0,0,tilt);
            body.localRotation=Quaternion.Euler(0,0,-tilt*.22f);
        }
        void ApplyRotor()
        {
            rotor.localRotation=Quaternion.Euler(0,0,RotorAngle);
            float blur=Mathf.SmoothStep(0,1,Mathf.InverseLerp(850,2300,RotorSpeed));
            for(int i=0;i<echoes.Length;i++)echoes[i].localRotation=Quaternion.Euler(0,0,RotorAngle-(i+1)*RotorSpeed*.006f);
            SetAlpha(solidViews,alpha);SetAlpha(echoViews,alpha*blur*.115f);
        }
        void SetAlpha(Renderer[] views,float a)
        {foreach(var r in views){var c=r.sharedMaterial.color;c.a=a;block.Clear();block.SetColor("_Color",c);r.SetPropertyBlock(block);}}
        static AnimationCurve Make(params float[] p){var keys=new Keyframe[p.Length/2];for(int i=0;i<keys.Length;i++)keys[i]=new Keyframe(p[i*2],p[i*2+1]);return new AnimationCurve(keys);}
        static readonly AnimationCurve RevealScale=Make(0,.20f,.12f,.60f,.32f,1.16f,.54f,.95f,.74f,1.025f,1,1);
        static readonly AnimationCurve RevealSquash=Make(0,.22f,.22f,-.13f,.40f,.065f,.7f,-.012f,1,0);
        static readonly AnimationCurve TakeoffSquash=Make(0,0,.22f,.12f,.48f,-.09f,.8f,-.02f,1,0);
        static readonly AnimationCurve TakeoffLift=Make(0,0,.24f,-.085f,.5f,.17f,.78f,.48f,1,.62f);
    }
}
