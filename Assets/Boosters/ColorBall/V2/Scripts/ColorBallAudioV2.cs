using System;
using UnityEngine;
using RoyalThrone.Testing;
using JellyGarden.Scripts.Juice;
namespace RoyalThrone.Boosters.ColorBall.V2
{
    public sealed class ColorBallAudioV2 : MonoBehaviour
    {
        public enum Cue { Reveal,Idle,Charge,Beam,TargetHit,Convert,FinalPulse }
        sealed class Voice {public AudioSource source;public ColorBallVisualV2 owner;public int lease;public float gain;public bool paused;}
        static ColorBallAudioV2 instance;Voice[] voices;AudioClip[] clips;int variation;
        public static event Action<Cue,int> Played;
        public static int Active{get{int n=0;if(instance&&instance.voices!=null)foreach(var v in instance.voices)if(v.source.isPlaying||v.paused)n++;return n;}}
        public static int Peak {get;private set;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset(){instance=null;Peak=0;Played=null;}
        public static void Prewarm(){if(instance)return;instance=new GameObject("Color Ball V2 original audio pool").AddComponent<ColorBallAudioV2>();instance.Build();}
        void Build(){clips=new AudioClip[7];foreach(Cue c in Enum.GetValues(typeof(Cue)))if(c!=Cue.Idle){clips[(int)c]=Resources.Load<AudioClip>("ColorBallV2/ColorBall_"+c);if(!clips[(int)c])Debug.LogWarning("Color Ball V2 source audio required: "+c);}voices=new Voice[24];for(int i=0;i<voices.Length;i++){var s=gameObject.AddComponent<AudioSource>();s.playOnAwake=false;s.spatialBlend=0;s.priority=150;voices[i]=new Voice{source=s};}}
        public static void OnEvent(ColorBallEvent e,ColorBallVisualV2 owner){Prewarm();switch(e){
            case ColorBallEvent.ColorBallRevealStarted:instance.Play(Cue.Reveal,owner,.20f);break;
            case ColorBallEvent.ColorBallActivationStarted:instance.Play(Cue.Charge,owner,.19f,true);break;
            case ColorBallEvent.ColorBallBeamFired:instance.Play(Cue.Beam,owner,.14f);break;
            case ColorBallEvent.ColorBallBeamHit:instance.Play(Cue.TargetHit,owner,.11f);break;
            case ColorBallEvent.ColorBallTargetConverted:instance.Play(Cue.Convert,owner,.12f);break;
            case ColorBallEvent.ColorBallFinalPulse:StopCharge(owner.Lease);instance.Play(Cue.FinalPulse,owner,.30f);break;
            case ColorBallEvent.ColorBallCompleted:StopCharge(owner.Lease);break;
        }}
        void Play(Cue cue,ColorBallVisualV2 owner,float gain,bool loop=false){if(RoyalSave.GetInt("Sound",1)==0||!clips[(int)cue])return;foreach(var v in voices)if(!v.source.isPlaying&&!v.paused){v.owner=owner;v.lease=owner.Lease;v.gain=gain*RoyalEffectsProfile.Get.effectsVolume;v.source.clip=clips[(int)cue];v.source.loop=loop;v.source.pitch=loop?.76f:1+(variation++%5-2)*.025f;v.source.volume=0;v.source.Play();Balance();Played?.Invoke(cue,owner.Lease);return;}}
        float Gain(Voice v){if(!v.source.loop)return v.gain;if(!v.owner||v.owner.Lease!=v.lease||v.owner.Current==ColorBallVisualV2.Phase.Free)return 0;return v.gain*Mathf.Lerp(.15f,1,v.owner.Charge);}
        void Balance(){float total=0;foreach(var v in voices)if(v.source.isPlaying)total+=Gain(v);float balance=Mathf.Min(1,.44f/Mathf.Max(.001f,total));foreach(var v in voices)if(v.source.isPlaying)v.source.volume=Gain(v)*balance;Peak=Mathf.Max(Peak,Active);}
        void Update(){foreach(var v in voices){if(RoyalSave.GetInt("Sound",1)==0){v.source.Stop();v.paused=false;continue;}if(ColorBallVisualV2.Paused&&v.source.isPlaying){v.source.Pause();v.paused=true;}else if(!ColorBallVisualV2.Paused&&v.paused){v.source.UnPause();v.paused=false;}if(v.source.loop&&v.owner)v.source.pitch=Mathf.Lerp(.76f,1.15f,v.owner.Charge);}Balance();}
        public static void StopCharge(int lease){if(!instance||instance.voices==null)return;foreach(var v in instance.voices)if(v.lease==lease&&v.source.loop){v.source.Stop();v.source.loop=false;v.paused=false;}}
        public static void Clear(){if(instance&&instance.voices!=null)foreach(var v in instance.voices){v.source.Stop();v.source.loop=false;v.paused=false;}}
        void OnDisable(){Clear();}
    }
}
