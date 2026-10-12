using System;
using System.Collections;
using System.Collections.Generic;
using JellyGarden.Scripts;
using JellyGarden.Scripts.Juice;
using UnityEngine;
using UnityEngine.Rendering;

namespace RoyalThrone.ReferenceGameplay
{
    // The APK supplies every pose and particle module. This clock adds the
    // button-to-target travel and schedules the caller's single gameplay hit.
    public sealed class ReferenceHammerSequence : MonoBehaviour
    {
        public const float PlaybackSpeed=1.5f, SourceImpact=.6f;
        public const float ImpactTime=SourceImpact/PlaybackSpeed, Entrance=.30f;
        public const float ReturnStart=.90f, ReturnDuration=.30f, VisualEnd=ReturnStart+ReturnDuration, Duration=1.36f;
        const string Resource="ReferenceGameplay/Hammer/";
        public float Elapsed {get;private set;}
        public bool Impacted {get;private set;}
        public bool Running {get;private set;}
        public Vector3 ImpactPosition {get;private set;}
        public static bool Available=>Resources.Load<GameObject>(Resource+"HammerAnimation")&&
            Resources.Load<AnimationClip>(Resource+"HammerExplodeAnimation2")&&
            Resources.Load<GameObject>("ReferenceGameplay/VFX/HammerImpactRfv387")&&
            Resources.Load<AudioClip>(Resource+"hammer_use_appear")&&Resources.Load<AudioClip>(Resource+"hammer_use_hit")&&Resources.Load<AudioClip>(Resource+"hammer_use_disappear");
        GameObject placement,rig,impact;
        AnimationClip clip;
        ParticleSystem[] particles;
        SpriteRenderer[] sprites;
        Color[] colors;
        AudioClip appear,hitSound,disappear;
        RoyalAudioDirector audioOwner;
        readonly Dictionary<AudioSource,AudioClip> pausedVoices=new Dictionary<AudioSource,AudioClip>();
        Vector3 origin,seat,initialCenter,endCenter;
        float cell;
        bool returning;
        Action damage;
        LevelManager LM=>LevelManager.THIS;

        void Sound(AudioClip sound){audioOwner=RoyalAudioDirector.Get;if(audioOwner)audioOwner.PlayClip(sound,.85f,60,1);}
        void PauseAudio(bool pause)
        {
            if(pause&&pausedVoices.Count==0&&audioOwner)
                foreach(var source in audioOwner.GetComponents<AudioSource>())
                    if(source.isPlaying&&(source.clip==appear||source.clip==hitSound||source.clip==disappear))
                    {pausedVoices[source]=source.clip;source.Pause();}
            if(!pause&&pausedVoices.Count>0)
            {
                foreach(var entry in pausedVoices)if(entry.Key&&entry.Key.clip==entry.Value)entry.Key.UnPause();
                pausedVoices.Clear();
            }
        }
        Vector3 Center(string child)
        {
            var renderer=rig.transform.Find(child).GetComponent<SpriteRenderer>();
            return placement.transform.InverseTransformPoint(renderer.bounds.center);
        }
        void Sample(float elapsed)
        {
            clip.SampleAnimation(rig,Mathf.Min(clip.length,elapsed*PlaybackSpeed));
            float scale=1;Vector3 position=seat;
            if(elapsed<Entrance)
            {
                float u=Mathf.Clamp01(elapsed/Entrance),ease=1-Mathf.Pow(1-u,3);
                scale=Mathf.Lerp(.24f,1,ease);
                position=Vector3.Lerp(origin-initialCenter*cell*.24f,seat,ease);
            }
            else if(elapsed>=ReturnStart)
            {
                float u=Mathf.Clamp01((elapsed-ReturnStart)/ReturnDuration),ease=u*u;
                scale=Mathf.Lerp(1,.18f,ease);
                position=Vector3.Lerp(seat,origin-endCenter*cell*.18f,ease);
            }
            placement.transform.SetPositionAndRotation(position,Quaternion.identity);
            placement.transform.localScale=Vector3.one*(cell*scale);
            float alpha=elapsed<VisualEnd-.06f?1:Mathf.Clamp01((VisualEnd-elapsed)/.06f);
            // Preserve the native animated shadow alpha; fade only the final return.
            for(int i=0;i<sprites.Length;i++){var color=sprites[i].color;color.a*=alpha;sprites[i].color=color;}
            if(elapsed>=VisualEnd)placement.SetActive(false);
        }
        public void Begin(Vector3 target,Vector3 button,float worldCell,Action onImpact)
        {
            Cancel();
            if(!Available)throw new InvalidOperationException("Hammer native animation, VFX, or audio is missing.");
            Elapsed=0;Impacted=false;returning=false;damage=onImpact;cell=worldCell;origin=button;origin.z=-.5f;
            ImpactPosition=new Vector3(target.x,target.y,-.5f);
            clip=Resources.Load<AnimationClip>(Resource+"HammerExplodeAnimation2");
            placement=new GameObject("Hammer - imported source motion");
            rig=Instantiate(Resources.Load<GameObject>(Resource+"HammerAnimation"),placement.transform);
            var animator=rig.GetComponent<Animator>();if(animator)animator.enabled=false;
            var sorting=placement.AddComponent<SortingGroup>();sorting.sortingOrder=37;
            sprites=rig.GetComponentsInChildren<SpriteRenderer>(true);colors=new Color[sprites.Length];for(int i=0;i<sprites.Length;i++)colors[i]=sprites[i].color;
            clip.SampleAnimation(rig,SourceImpact);var contact=Center("01_kafa");
            seat=ImpactPosition-contact*cell;
            clip.SampleAnimation(rig,0);initialCenter=Center("Base");
            clip.SampleAnimation(rig,clip.length);endCenter=Center("Base");
            impact=Instantiate(Resources.Load<GameObject>("ReferenceGameplay/VFX/HammerImpactRfv387"));
            impact.name="Hammer - native impact VFX";impact.transform.position=ImpactPosition;impact.transform.localScale*=cell;
            var fxSort=impact.GetComponent<SortingGroup>();if(!fxSort)fxSort=impact.AddComponent<SortingGroup>();fxSort.sortingOrder=36;
            particles=impact.GetComponentsInChildren<ParticleSystem>(true);
            foreach(var ps in particles){var main=ps.main;main.scalingMode=ParticleSystemScalingMode.Hierarchy;ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);}
            impact.SetActive(false);
            appear=Resources.Load<AudioClip>(Resource+"hammer_use_appear");hitSound=Resources.Load<AudioClip>(Resource+"hammer_use_hit");disappear=Resources.Load<AudioClip>(Resource+"hammer_use_disappear");
            Running=true;Sample(0);Sound(appear);
        }
        // Public stepping supports deterministic low-frame-rate and pause verification.
        public void Advance(float delta,bool paused)
        {
            if(!Running)return;
            PauseAudio(paused);if(paused)return;
            float before=Elapsed;Elapsed=Mathf.Min(Duration,Elapsed+Mathf.Max(0,delta));
            if(!Impacted&&Elapsed>=ImpactTime)
            {
                Sample(ImpactTime);Impacted=true;impact.SetActive(true);
                foreach(var ps in particles)if(ps.gameObject.activeInHierarchy){ps.Play(false);ps.Pause(false);}
                Sound(hitSound);damage?.Invoke();
            }
            if(Impacted)
            {
                float dt=Elapsed-Mathf.Max(before,ImpactTime);
                if(dt>0)foreach(var ps in particles)if(ps.gameObject.activeInHierarchy){ps.Simulate(dt,false,false,false);ps.Pause(false);}
            }
            if(!returning&&Elapsed>=ReturnStart){returning=true;Sound(disappear);}
            // Restore base colors before sampling so the final fade cannot compound.
            for(int i=0;i<sprites.Length;i++)sprites[i].color=colors[i];
            Sample(Elapsed);
            if(Elapsed>=Duration)Cancel();
        }
        public IEnumerator Play(Vector3 target,Vector3 button,float worldCell,Action onImpact)
        {
            Begin(target,button,worldCell,onImpact);
            try{while(Running){yield return null;Advance(Time.deltaTime,LM&&LM.gameStatus==GameState.Pause);}}
            finally{Cancel();}
        }
        public void Cancel()
        {
            Running=false;damage=null;
            if(placement){placement.SetActive(false);Destroy(placement);}if(impact){impact.SetActive(false);Destroy(impact);}
            placement=rig=impact=null;particles=null;sprites=null;colors=null;pausedVoices.Clear();
            if(audioOwner){audioOwner.StopClip(appear);audioOwner.StopClip(hitSound);audioOwner.StopClip(disappear);}audioOwner=null;
        }
        void OnDisable()=>Cancel();
    }
}
