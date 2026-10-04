using System;
using UnityEngine;
using JellyGarden.Scripts;
using JellyGarden.Scripts.Juice;
using RoyalThrone.Testing;
namespace RoyalThrone.Boosters.Propeller.V5
{
    // Continuous physical rotor source spans startup and flight without restarting.
    public sealed class PropellerAudioV5 : MonoBehaviour
    {
        public enum Cue { Create,RotorStart,Takeoff,Flight,NeighborHit,Impact }
        sealed class Voice {public AudioSource source;public int lease;public Cue cue;public float gain;public PropellerVisualV5 owner;public bool paused;}
        const int Capacity=20,MaxRotors=5;Voice[] voices;
        AudioClip rotor,air,hit;static PropellerAudioV5 instance;float lastNeighbor=-1;
        public static event Action<Cue,int> Played;
        public static int Peak {get;private set;}
        public static int Active {get{int n=0;if(instance&&instance.voices!=null)foreach(var v in instance.voices)if(v.source.isPlaying||v.paused)n++;return n;}}
        public static int Loops {get{int n=0;if(instance&&instance.voices!=null)foreach(var v in instance.voices)if(v.source.loop&&(v.source.isPlaying||v.paused))n++;return n;}}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset(){instance=null;Played=null;Peak=0;}
        public static void Prewarm()
        {
            if(instance)return;var go=new GameObject("Propeller V5 mechanical audio pool");go.transform.SetParent(RoyalPowerupDirector.Get.transform,false);instance=go.AddComponent<PropellerAudioV5>();instance.Build();
        }
        void Build()
        {
            rotor=Resources.Load<AudioClip>("PropellerV5/RotorLoop");air=Resources.Load<AudioClip>("PropellerV5/AirRelease");hit=Resources.Load<AudioClip>("PropellerV5/PhysicalHit");
            if(!rotor||!air||!hit)Debug.LogWarning("Propeller V5: final mechanical SFX required; no synthetic fallback is played.");
            voices=new Voice[Capacity];for(int i=0;i<Capacity;i++){var s=gameObject.AddComponent<AudioSource>();s.playOnAwake=false;s.spatialBlend=0;s.priority=140;voices[i]=new Voice{source=s};}
        }
        public static void OnEvent(PropellerEvent e,PropellerVisualV5 owner)
        {
            Prewarm();var s=instance;
            switch(e){
                case PropellerEvent.PropellerRevealStarted:s.Play(Cue.Create,owner,.06f,s.hit,false,1.15f);break;
                case PropellerEvent.PropellerRotorAccelerating:if(Loops<MaxRotors)s.Play(Cue.RotorStart,owner,.13f,s.rotor,true,.42f);break;
                case PropellerEvent.PropellerTakeoff:s.Play(Cue.Takeoff,owner,.25f,s.air);break;
                case PropellerEvent.PropellerNeighborHit:if(Time.time-s.lastNeighbor>.045f){s.lastNeighbor=Time.time;s.Play(Cue.NeighborHit,owner,.075f,s.hit,false,.92f);}break;
                case PropellerEvent.PropellerFlightReady:
                    foreach(var v in s.voices)if(v.lease==owner.Lease&&v.source.loop){v.cue=Cue.Flight;Played?.Invoke(Cue.Flight,owner.Lease);}break;
                case PropellerEvent.PropellerTargetReached:StopFlight(owner.Lease);break;
                case PropellerEvent.PropellerImpactStarted:s.Play(Cue.Impact,owner,.32f,s.hit);break;
            }
        }
        void Play(Cue cue,PropellerVisualV5 owner,float gain,AudioClip clip,bool loop=false,float pitch=1)
        {
            if(!clip||RoyalSave.GetInt("Sound",1)==0)return;
            foreach(var v in voices)if(!v.source.isPlaying&&!v.paused){
                v.source.Stop();v.source.clip=clip;v.source.loop=loop;v.source.pitch=pitch;v.source.volume=0;v.source.panStereo=Mathf.Clamp(owner.Anchor.x*.04f,-.15f,.15f);
                v.cue=cue;v.lease=owner.Lease;v.owner=owner;v.gain=gain*RoyalEffectsProfile.Get.effectsVolume;v.source.Play();Balance();Played?.Invoke(cue,owner.Lease);return;
            }
        }
        public static void StopFlight(int lease)
        {
            if(!instance||instance.voices==null)return;
            foreach(var v in instance.voices)if(v.lease==lease&&v.source.loop){v.source.Stop();v.source.loop=false;v.paused=false;}
        }
        float Gain(Voice v)
        {
            float f=1;
            if(v.source.loop){
                if(!v.owner||v.owner.Lease!=v.lease)return 0;
                float power=Mathf.Clamp01(v.owner.RotorSpeed/PropellerVisualV5.FlightDegreesPerSecond);
                f=Mathf.Sqrt(power)*Mathf.Clamp01(v.owner.Age/.045f);
                if(v.owner.Current==PropellerVisualV5.Phase.Flight)f=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.89f,1,v.owner.FlightProgress));
            }
            return v.gain*f;
        }
        void Balance()
        {
            float total=0;foreach(var v in voices)if(v.source.isPlaying)total+=Gain(v);
            float factor=Mathf.Min(1,.32f/Mathf.Max(.001f,total));foreach(var v in voices)if(v.source.isPlaying)v.source.volume=Gain(v)*factor;
            Peak=Mathf.Max(Peak,Active);
        }
        void Update()
        {
            bool pause=LevelManager.THIS&&LevelManager.THIS.gameStatus==GameState.Pause;
            bool soundOff=RoyalSave.GetInt("Sound",1)==0;   // one read per frame, not one per voice
            foreach(var v in voices){
                if(soundOff){v.source.Stop();v.paused=false;continue;}
                if(pause&&v.source.isPlaying){v.source.Pause();v.paused=true;}
                else if(!pause&&v.paused){v.source.UnPause();v.paused=false;}
                if(v.source.loop&&v.owner){float power=Mathf.Clamp01(v.owner.RotorSpeed/PropellerVisualV5.FlightDegreesPerSecond);v.source.pitch=Mathf.Lerp(v.source.pitch,.42f+.58f*power,1-Mathf.Exp(-Time.deltaTime*32));}
            }
            Balance();
        }
        public static void Clear(){if(instance&&instance.voices!=null){foreach(var v in instance.voices){v.source.Stop();v.source.loop=false;v.paused=false;}instance.lastNeighbor=-1;}}
        void OnDisable(){Clear();}
    }
}
