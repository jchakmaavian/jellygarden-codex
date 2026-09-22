using System;
using UnityEngine;
using RoyalThrone.Testing;
using JellyGarden.Scripts.Juice;
namespace RoyalThrone.Boosters.ColorBall.V4
{
    public sealed class ColorBallAudioV4 : MonoBehaviour
    {
        public enum Cue { Reveal,Idle,Charge,Beam,TargetHit,Convert,FinalPulse,Orbit }
        sealed class Voice {public AudioSource source;public ColorBallVisualV4 owner;public int lease;public float gain,age,tail;public Cue cue;public bool paused,refined;}
        static ColorBallAudioV4 instance;Voice[] voices;AudioClip[] clips;int variation;
        public static event Action<Cue,int> Played;
        public static int Active{get{int n=0;if(instance&&instance.voices!=null)foreach(var v in instance.voices)if(v.source.isPlaying||v.paused)n++;return n;}}
        public static int Peak {get;private set;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset(){instance=null;Peak=0;Played=null;}
        public static void Prewarm(){if(instance)return;instance=new GameObject("Color Ball V4 original audio pool").AddComponent<ColorBallAudioV4>();instance.Build();}
        void Build(){clips=new AudioClip[8];foreach(Cue c in Enum.GetValues(typeof(Cue)))if(c!=Cue.Idle){clips[(int)c]=Resources.Load<AudioClip>("ColorBallV4/ColorBall_"+c);if(!clips[(int)c])Debug.LogWarning("Color Ball V4 source audio required: "+c);}voices=new Voice[24];for(int i=0;i<voices.Length;i++){var s=gameObject.AddComponent<AudioSource>();s.playOnAwake=false;s.spatialBlend=0;s.priority=150;voices[i]=new Voice{source=s};}}
        public static void OnEvent(ColorBallEvent e,ColorBallVisualV4 owner){Prewarm();switch(e){
            case ColorBallEvent.ColorBallRevealStarted:instance.Play(Cue.Reveal,owner,.20f);break;
            case ColorBallEvent.ColorBallActivationStarted:instance.Play(Cue.Charge,owner,owner.referenceV5?.024f:.17f,true);if(!owner.referenceV5)instance.Play(Cue.Orbit,owner,.038f,true);break;
            case ColorBallEvent.ColorBallChargeComplete:if(owner.referenceV5)instance.Play(Cue.Convert,owner,.10f);break;
            case ColorBallEvent.ColorBallBeamFired:instance.Play(Cue.Beam,owner,owner.referenceV5?.18f:.14f);break;
            case ColorBallEvent.ColorBallBeamHit:instance.Play(Cue.TargetHit,owner,owner.referenceV5?.10f:.11f);break;
            case ColorBallEvent.ColorBallTargetConverted:instance.Play(Cue.Convert,owner,owner.referenceV5?.065f:.12f);break;
            case ColorBallEvent.ColorBallFinalPulse:StopCharge(owner.Lease);instance.Play(Cue.FinalPulse,owner,owner.referenceV5?.36f:.30f);break;
            case ColorBallEvent.ColorBallCompleted:StopCharge(owner.Lease);break;
        }}
        void Play(Cue cue,ColorBallVisualV4 owner,float gain,bool loop=false){
            if(RoyalSave.GetInt("Sound",1)==0||!clips[(int)cue])return;
            // Bound overlapping accents locally; existing cue assets and the global mix stay intact.
            int count=0;foreach(var voice in voices)if(voice.source.isPlaying&&voice.cue==cue)count++;
            if(owner.referenceV5&&count>=(loop?2:3))return;
            foreach(var v in voices)if(!v.source.isPlaying&&!v.paused){v.owner=owner;v.lease=owner.Lease;v.cue=cue;v.refined=owner.referenceV5;v.age=0;v.tail=cue==Cue.Beam?.24f:cue==Cue.TargetHit?.16f:cue==Cue.Convert?.18f:cue==Cue.FinalPulse?.48f:0;v.gain=gain*(loop?1:1+(variation%5-2)*.04f)*RoyalEffectsProfile.Get.effectsVolume;v.source.clip=clips[(int)cue];v.source.loop=loop;v.source.pitch=loop?.76f:1+(variation++%5-2)*.025f;v.source.panStereo=0;v.source.volume=0;v.source.Play();Balance();Played?.Invoke(cue,owner.Lease);return;}
        }
        // The loops follow the measured spin, not the charge ramp, so the sound rises and holds with
        // what is actually on screen. V4 spins about five times faster than V3, so the pitch range is
        // widened to match rather than topping out while the ball is still accelerating.
        float Gain(Voice v){if(!v.source.loop)return v.gain*(v.refined&&v.tail>0?1-Mathf.SmoothStep(0,1,(v.age-v.tail*.55f)/(v.tail*.45f)):1);if(!v.owner||v.owner.Lease!=v.lease||v.owner.Current==ColorBallVisualV4.Phase.Free)return 0;float envelope=1;if(v.owner.referenceV5){envelope=v.owner.Current==ColorBallVisualV4.Phase.Charge?Mathf.SmoothStep(0,1,v.owner.Age/.10f):v.owner.Current==ColorBallVisualV4.Phase.Finish?1-Mathf.SmoothStep(0,1,v.owner.Age/.17f):Mathf.Lerp(1,.32f,Mathf.Clamp01(v.owner.Age/.16f));}return v.gain*Mathf.Lerp(.15f,1,v.owner.SpinEnergy)*envelope;}
        void Balance(){float total=0;foreach(var v in voices)if(v.source.isPlaying)total+=Gain(v);float balance=Mathf.Min(1,.44f/Mathf.Max(.001f,total));foreach(var v in voices)if(v.source.isPlaying)v.source.volume=Gain(v)*balance;Peak=Mathf.Max(Peak,Active);}
        void Update(){foreach(var v in voices){if(RoyalSave.GetInt("Sound",1)==0){v.source.Stop();v.paused=false;continue;}if(ColorBallVisualV4.Paused&&v.source.isPlaying){v.source.Pause();v.paused=true;}else if(!ColorBallVisualV4.Paused&&v.paused){v.source.UnPause();v.paused=false;}if(!ColorBallVisualV4.Paused&&v.source.isPlaying){v.age+=Time.deltaTime;if(v.refined&&!v.source.loop&&v.tail>0&&v.age>=v.tail)v.source.Stop();}if(v.source.loop&&v.owner){if(v.owner.Lease!=v.lease||v.owner.Current==ColorBallVisualV4.Phase.Free){v.source.Stop();v.paused=false;continue;}v.source.pitch=v.refined?Mathf.Lerp(.92f,1.04f,v.owner.SpinEnergy):v.cue==Cue.Orbit?Mathf.Lerp(.90f,1.20f,Mathf.Clamp01(v.owner.OrbitSpeed/ColorBallVisualV4.FinishOrbit)):Mathf.Lerp(.72f,1.34f,v.owner.SpinEnergy);v.source.panStereo=v.cue==Cue.Orbit?Mathf.Sin(v.owner.OrbitAngle)*.14f:0;}}Balance();}
        public static void StopCharge(int lease){if(!instance||instance.voices==null)return;foreach(var v in instance.voices)if(v.lease==lease&&v.source.loop){v.source.Stop();v.source.loop=false;v.paused=false;}}
        public static void Clear(){if(instance&&instance.voices!=null)foreach(var v in instance.voices){v.source.Stop();v.source.loop=false;v.paused=false;}}
        void OnDisable(){Clear();}
    }
}
