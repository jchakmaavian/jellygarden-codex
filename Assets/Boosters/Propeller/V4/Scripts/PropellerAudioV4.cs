using System;
using UnityEngine;
using JellyGarden.Scripts;
using JellyGarden.Scripts.Juice;
using RoyalThrone.Testing;
namespace RoyalThrone.Boosters.Propeller.V4
{
    public sealed class PropellerAudioV4 : MonoBehaviour
    {
        public enum Cue { Create,RotorStart,Takeoff,Flight,NeighborHit,Impact }
        sealed class Voice {public AudioSource source;public int lease;public Cue cue;public float gain;public PropellerVisualV4 owner;public bool paused;}
        const int Capacity=24;Voice[] voices;readonly AudioClip[] clips=new AudioClip[6];
        static PropellerAudioV4 instance;float lastNeighbor=-1;
        public static event Action<Cue,int> Played;
        public static int Peak {get;private set;}
        public static int Active {get{int n=0;if(instance&&instance.voices!=null)foreach(var v in instance.voices)if(v.source.isPlaying||v.paused)n++;return n;}}
        public static int Loops {get{int n=0;if(instance&&instance.voices!=null)foreach(var v in instance.voices)if(v.source.loop&&(v.source.isPlaying||v.paused))n++;return n;}}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset(){instance=null;Played=null;Peak=0;}
        public static void Prewarm()
        {
            if(instance)return;var go=new GameObject("Propeller V4 original audio pool");go.transform.SetParent(RoyalPowerupDirector.Get.transform,false);instance=go.AddComponent<PropellerAudioV4>();instance.Build();
        }
        void Build()
        {
            for(int i=0;i<clips.Length;i++){clips[i]=Resources.Load<AudioClip>("PropellerV4/Propeller_"+(Cue)i);if(!clips[i])throw new InvalidOperationException("Missing original Propeller SFX: "+(Cue)i);}
            voices=new Voice[Capacity];for(int i=0;i<Capacity;i++){var s=gameObject.AddComponent<AudioSource>();s.playOnAwake=false;s.spatialBlend=0;s.priority=140;voices[i]=new Voice{source=s};}
        }
        public static void OnEvent(PropellerEvent e,PropellerVisualV4 owner)
        {
            Prewarm();var s=instance;
            switch(e){
                case PropellerEvent.PropellerRevealStarted:s.Play(Cue.Create,owner,.38f);break;
                case PropellerEvent.PropellerRotorAccelerating:s.Play(Cue.RotorStart,owner,.12f);break;
                case PropellerEvent.PropellerTakeoff:s.Play(Cue.Takeoff,owner,.42f);break;
                case PropellerEvent.PropellerNeighborHit:
                    if(Time.time-s.lastNeighbor>.035f){s.lastNeighbor=Time.time;s.Play(Cue.NeighborHit,owner,.18f);}break;
                case PropellerEvent.PropellerFlightReady:StopFlight(owner.Lease);s.Play(Cue.Flight,owner,.075f,true);break;
                case PropellerEvent.PropellerTargetReached:StopFlight(owner.Lease);break;
                case PropellerEvent.PropellerImpactStarted:s.Play(Cue.Impact,owner,.46f);break;
            }
        }
        void Play(Cue cue,PropellerVisualV4 owner,float gain,bool loop=false)
        {
            if(RoyalSave.GetInt("Sound",1)==0)return;
            foreach(var v in voices)if(!v.source.isPlaying&&!v.paused){
                v.source.Stop();v.source.clip=clips[(int)cue];v.source.loop=loop;v.source.pitch=1;v.source.volume=0;v.source.panStereo=Mathf.Clamp(owner.Anchor.x*.04f,-.15f,.15f);
                v.cue=cue;v.lease=owner.Lease;v.owner=owner;v.gain=gain*RoyalEffectsProfile.Get.effectsVolume;v.source.Play();Balance();Played?.Invoke(cue,owner.Lease);return;
            }
        }
        public static void StopFlight(int lease)
        {
            if(!instance||instance.voices==null)return;
            foreach(var v in instance.voices)if(v.lease==lease&&(v.cue==Cue.Flight||v.cue==Cue.RotorStart)){v.source.Stop();v.source.loop=false;v.paused=false;}
        }
        float Gain(Voice v)
        {
            float f=1;if(v.cue==Cue.Flight){if(!v.owner||v.owner.Lease!=v.lease||v.owner.Current!=PropellerVisualV4.Phase.Flight)return 0;f=Mathf.Clamp01(v.owner.Age/.05f)*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.78f,1,v.owner.FlightProgress)));}
            return v.gain*f;
        }
        void Balance()
        {
            float total=0;foreach(var v in voices)if(v.source.isPlaying)total+=Gain(v);
            // Independent headroom; never change TNT or the shared mixer/voice gains.
            float factor=Mathf.Min(1,.22f/Mathf.Max(.001f,total));foreach(var v in voices)if(v.source.isPlaying)v.source.volume=Gain(v)*factor;
            Peak=Mathf.Max(Peak,Active);
        }
        void Update()
        {
            bool pause=LevelManager.THIS&&LevelManager.THIS.gameStatus==GameState.Pause;
            foreach(var v in voices){
                if(RoyalSave.GetInt("Sound",1)==0){v.source.Stop();v.paused=false;continue;}
                if(pause&&v.source.isPlaying){v.source.Pause();v.paused=true;}
                else if(!pause&&v.paused){v.source.UnPause();v.paused=false;}
            }
            Balance();
        }
        public static void Clear(){if(instance&&instance.voices!=null){foreach(var v in instance.voices){v.source.Stop();v.source.loop=false;v.paused=false;}instance.lastNeighbor=-1;}}
        void OnDisable(){Clear();}
    }
}
