using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JellyGarden.Scripts;
using JellyGarden.Scripts.Juice;
using UnityEngine;
using UnityEngine.Rendering;

namespace RoyalThrone.ReferenceGameplay
{
    // Native APK arrow/target poses and particle modules. Board travel is adapted
    // to the selected row; only the caller changes cells, inventory or moves.
    public sealed class ReferenceArcherSequence : MonoBehaviour
    {
        public const float LaunchTime=.16f, FlightDuration=.42f, ImpactTime=LaunchTime+FlightDuration;
        public const float SettleDuration=.5f, FadeDuration=.16f, Duration=ImpactTime+SettleDuration+FadeDuration;
        const string Resource="ReferenceGameplay/Archer/";
        public float Elapsed {get;private set;}
        public float TipX {get;private set;}
        public int HitCount {get;private set;}
        public bool Running {get;private set;}
        public bool Launched {get;private set;}
        public bool Impacted {get;private set;}
        public static bool Available=>Resources.Load<GameObject>(Resource+"ArcherAnimation")&&Resources.Load<AnimationClip>(Resource+"ArrowAnimation")&&
            Resources.Load<AudioClip>(Resource+"arrow_use")&&Resources.Load<GameObject>("ReferenceGameplay/VFX/ArcherTrailRfv387")&&Resources.Load<GameObject>("ReferenceGameplay/VFX/ArcherSmokeRfv387");
        GameObject rig,trail,smoke;
        Transform arrow,target;
        SpriteRenderer head;
        SpriteRenderer[] renderers;
        Color[] colors;
        AnimationClip clip;
        AudioClip sound;
        RoyalAudioDirector audioOwner;
        readonly Dictionary<AudioSource,AudioClip> pausedVoices=new Dictionary<AudioSource,AudioClip>();
        readonly Dictionary<GameObject,ParticleSystem[]> effects=new Dictionary<GameObject,ParticleSystem[]>();
        Vector3[] cells;
        Action<int> damage;
        float cell,startX,endX;

        public void Begin(IReadOnlyList<Vector3> row,float worldCell,Action<int> onHit)
        {
            Cancel();
            if(!Available)throw new InvalidOperationException("Archer animation, VFX or audio is missing.");
            if(row==null||row.Count==0||worldCell<=0)throw new ArgumentException("Archer needs a nonempty left-to-right row and a positive cell size.");
            cells=row.ToArray();
            for(int i=1;i<cells.Length;i++)if(cells[i].x<=cells[i-1].x)throw new ArgumentException("Archer row must be ordered left to right.");
            cell=worldCell;damage=onHit;Elapsed=0;HitCount=0;Launched=Impacted=false;
            try
            {
                rig=Instantiate(Resources.Load<GameObject>(Resource+"ArcherAnimation"));rig.name="Archer - imported source motion";
                rig.GetComponent<Animator>().enabled=false;
                rig.transform.position=new Vector3(cells[cells.Length-1].x-cell*.45f,cells[0].y,-.5f);rig.transform.localScale=Vector3.one*cell;
                var sort=rig.AddComponent<SortingGroup>();sort.sortingOrder=37;
                clip=Resources.Load<AnimationClip>(Resource+"ArrowAnimation");
                arrow=rig.transform.Find("Arrow");target=rig.transform.Find("Target");head=arrow.Find("Arrow_uc").GetComponent<SpriteRenderer>();
                renderers=rig.GetComponentsInChildren<SpriteRenderer>(true);colors=renderers.Select(r=>r.color).ToArray();
                clip.SampleAnimation(rig,1);endX=head.bounds.max.x;startX=cells[0].x-cell*1.3f;
                trail=Effect("ArcherTrailRfv387");smoke=Effect("ArcherSmokeRfv387");
                sound=Resources.Load<AudioClip>(Resource+"arrow_use");Running=true;Sample(0);
            }
            catch{Cancel();throw;}
        }
        GameObject Effect(string key)
        {
            var go=Instantiate(Resources.Load<GameObject>("ReferenceGameplay/VFX/"+key));go.name="Archer - "+key;
            go.transform.localScale*=cell;
            var sort=go.GetComponent<SortingGroup>();if(!sort)sort=go.AddComponent<SortingGroup>();sort.sortingOrder=38;
            var systems=go.GetComponentsInChildren<ParticleSystem>(true);effects[go]=systems;
            foreach(var ps in systems){var main=ps.main;main.scalingMode=ParticleSystemScalingMode.Hierarchy;ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);}
            go.SetActive(false);return go;
        }
        void StartEffect(GameObject go)
        {
            go.SetActive(true);
            foreach(var ps in effects[go])if(ps.gameObject.activeInHierarchy){ps.Play(false);ps.Pause(false);}
        }
        void Sample(float t)
        {
            for(int i=0;i<renderers.Length;i++)renderers[i].color=colors[i];
            float u=Mathf.Clamp01((t-LaunchTime)/FlightDuration);
            float source=t<ImpactTime?u:Mathf.Min(9,1+(t-ImpactTime)*16);
            clip.SampleAnimation(rig,source);
            target.localScale=Vector3.one*Mathf.SmoothStep(0,1,t/LaunchTime);
            arrow.localPosition=new Vector3(-1,0,0);
            float progress=.2f*u+.8f*u*u;
            if(t<ImpactTime)
            {
                float desired=Mathf.Lerp(startX,endX,progress);
                arrow.position+=Vector3.right*(desired-head.bounds.max.x);
            }
            TipX=head.bounds.max.x;
            float opacity=Mathf.Clamp01(t/.07f)*(1-Mathf.Clamp01((t-ImpactTime-SettleDuration)/FadeDuration));
            for(int i=0;i<renderers.Length;i++){var color=renderers[i].color;color.a*=opacity;renderers[i].color=color;}
            trail.transform.position=new Vector3(TipX-cell*.65f,cells[0].y,-.55f);
            smoke.transform.position=new Vector3(endX,cells[0].y,-.55f);
        }
        public void Advance(float delta,bool paused)
        {
            if(!Running)return;
            PauseAudio(paused);if(paused)return;
            // Fixed substeps keep native trails continuous, including a frame that
            // crosses the entire row. The monotonic cursor hits each cell once.
            float until=Mathf.Min(Duration,Elapsed+Mathf.Max(0,delta));
            while(Running&&Elapsed<until)
            {
                float previous=Elapsed;Elapsed=Mathf.Min(until,Elapsed+1f/60);
                Sample(Elapsed);
                if(!Launched&&Elapsed>=LaunchTime)
                {
                    Launched=true;StartEffect(trail);audioOwner=RoyalAudioDirector.Get;
                    if(audioOwner)audioOwner.PlayClip(sound,.85f,60,1);
                }
                if(Launched)
                    while(Running&&HitCount<cells.Length&&TipX>=cells[HitCount].x)
                    {int index=HitCount++;damage?.Invoke(index);}
                if(!Running)return;
                if(!Impacted&&Elapsed>=ImpactTime)
                {
                    Impacted=true;StartEffect(smoke);
                    foreach(var ps in effects[trail])ps.Stop(false,ParticleSystemStopBehavior.StopEmitting);
                }
                foreach(var entry in effects)if(entry.Key.activeSelf)
                    foreach(var ps in entry.Value)if(ps.gameObject.activeInHierarchy){ps.Simulate(Elapsed-previous,false,false,false);ps.Pause(false);}
            }
            if(Elapsed>=Duration)Cancel();
        }
        void PauseAudio(bool pause)
        {
            if(pause&&pausedVoices.Count==0&&audioOwner)
                foreach(var voice in audioOwner.GetComponents<AudioSource>())if(voice.isPlaying&&voice.clip==sound){pausedVoices[voice]=voice.clip;voice.Pause();}
            if(!pause&&pausedVoices.Count>0){foreach(var pair in pausedVoices)if(pair.Key&&pair.Key.clip==pair.Value)pair.Key.UnPause();pausedVoices.Clear();}
        }
        public IEnumerator Play(IReadOnlyList<Square> row,Action<Square> onHit)
        {
            var ordered=row.Where(s=>s).OrderBy(s=>s.transform.position.x).ToArray();
            var lm=LevelManager.THIS;
            Begin(ordered.Select(s=>s.transform.position).ToArray(),lm.squareWidth*Mathf.Abs(lm.GameField.lossyScale.x),i=>onHit(ordered[i]));
            try{while(Running){yield return null;Advance(Time.deltaTime,lm&&lm.gameStatus==GameState.Pause);}}
            finally{Cancel();}
        }
        public void Cancel()
        {
            Running=false;damage=null;
            if(rig){rig.SetActive(false);Destroy(rig);}rig=null;
            foreach(var go in effects.Keys)if(go){go.SetActive(false);Destroy(go);}effects.Clear();trail=smoke=null;
            if(audioOwner)audioOwner.StopClip(sound);audioOwner=null;pausedVoices.Clear();
        }
        void OnDisable()=>Cancel();
    }
}
