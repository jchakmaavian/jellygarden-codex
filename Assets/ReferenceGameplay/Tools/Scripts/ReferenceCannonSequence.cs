using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JellyGarden.Scripts;
using JellyGarden.Scripts.Juice;
using Spine.Unity;
using UnityEngine;
using UnityEngine.Rendering;

namespace RoyalThrone.ReferenceGameplay
{
    // Native Spine 3.8 motion and native particles, driven by one board-owned clock.
    // No source gameplay scripts/events are imported; the caller owns each cell hit.
    public sealed class ReferenceCannonSequence : MonoBehaviour
    {
        // Source pose review: compression at .4, extension at .55, muzzle discharge
        // at .6. Anticipation overlaps the end of the rise, as in the reference.
        public const float Entrance=.50f, AnticipationStart=.40f, FireFrame=.60f, Flight=.38f;
        public const float RecoilEnd=AnticipationStart+.9f, BarReturn=.24f;
        public float Elapsed {get;private set;}
        public bool Fired {get;private set;}
        public bool Running {get;private set;}
        SkeletonAnimation cannon;
        GameObject ball,flash,fuse;
        AudioClip moveSound,useSound,fireSound;
        RoyalAudioDirector audioOwner;
        Action<float> present;
        Spine.Bone fuseBone;
        Spine.Slot muzzleSlot;
        readonly float[] muzzleVertices=new float[8];
        Vector3 seat;float cell;
        bool firing;
        readonly Dictionary<GameObject,ParticleSystem[]> particles=new Dictionary<GameObject,ParticleSystem[]>();
        readonly List<GameObject> owned=new List<GameObject>();
        readonly List<ParticleSystem> paused=new List<ParticleSystem>();
        LevelManager LM=>LevelManager.THIS;
        public static bool Available=>Resources.Load<SkeletonDataAsset>("ReferenceGameplay/Cannon/CannonSkeleton")&&Resources.Load<GameObject>("ReferenceGameplay/VFX/CannonBallRfv387");

        GameObject Effect(string key,Vector3 position,float cell,bool play=true)
        {
            var template=Resources.Load<GameObject>("ReferenceGameplay/VFX/"+key);
            if(!template)return null;
            var go=Instantiate(template);owned.Add(go);go.transform.SetPositionAndRotation(position,Quaternion.identity);
            go.transform.localScale=template.transform.localScale*cell;
            var sort=go.GetComponent<SortingGroup>();if(!sort)sort=go.AddComponent<SortingGroup>();sort.sortingOrder=38;
            var systems=go.GetComponentsInChildren<ParticleSystem>(true);particles[go]=systems;
            foreach(var ps in systems)
            {
                var main=ps.main;main.scalingMode=ParticleSystemScalingMode.Hierarchy;
                ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
                if(play&&ps.gameObject.activeInHierarchy)ps.Play(false);
            }
            if(!play)go.SetActive(false);
            return go;
        }
        void StartEffect(GameObject go,Vector3 position)
        {
            if(!go)return;go.transform.position=position;go.SetActive(true);
            foreach(var ps in particles[go])if(ps.gameObject.activeInHierarchy)ps.Play(false);
        }
        void AdvanceMotion(float from,float to)
        {
            float start=AnticipationStart;
            if(!firing&&to>=start){
                if(from<start)cannon.Update(start-from);
                var track=cannon.AnimationState.SetAnimation(0,"gercek_patlama",false);track.MixDuration=.05f;
                firing=true;Sound(useSound);cannon.Update(to-Mathf.Max(from,start));
            }else cannon.Update(to-from);
            float ease=Mathf.SmoothStep(0,1,to/Entrance);
            cannon.transform.position=seat+Vector3.down*(1-ease)*cell*2.5f;
        }
        Vector3 Muzzle()
        {
            // Use the aperture in the animated gold rim, including its squash/stretch.
            if(muzzleSlot?.Attachment is Spine.RegionAttachment region){
                region.ComputeWorldVertices(muzzleSlot.Bone,muzzleVertices,0,2);
                float x0=float.MaxValue,x1=float.MinValue,y0=float.MaxValue,y1=float.MinValue;
                for(int i=0;i<8;i+=2){x0=Mathf.Min(x0,muzzleVertices[i]);x1=Mathf.Max(x1,muzzleVertices[i]);y0=Mathf.Min(y0,muzzleVertices[i+1]);y1=Mathf.Max(y1,muzzleVertices[i+1]);}
                return cannon.transform.TransformPoint(new Vector3((x0+x1)*.5f,Mathf.Lerp(y0,y1,.72f),0));
            }
            var bone=muzzleSlot?.Bone??cannon.Skeleton.FindBone("kafa_bone");
            return cannon.transform.TransformPoint(new Vector3(bone.WorldX,bone.WorldY,0));
        }
        public static float FlightProgress(float time){float u=Mathf.Clamp01(time/Flight);return .3f*u+.7f*u*u;}
        static void StopEmission(GameObject go)
        {if(go)foreach(var ps in go.GetComponentsInChildren<ParticleSystem>())ps.Stop(false,ParticleSystemStopBehavior.StopEmitting);}
        void Sound(AudioClip clip){audioOwner=RoyalAudioDirector.Get;if(audioOwner)audioOwner.PlayClip(clip,.8f,60,1);}
        void Pause(bool value)
        {
            if(value&&paused.Count==0)
                foreach(var go in owned)if(go)foreach(var ps in go.GetComponentsInChildren<ParticleSystem>())if(ps.isPlaying){ps.Pause(false);paused.Add(ps);}
            if(!value&&paused.Count>0){foreach(var ps in paused)if(ps&&ps.isPaused)ps.Play(false);paused.Clear();}
        }
        public IEnumerator Play(IReadOnlyList<Square> column,Action<Square> hit,Action<float> presentation=null)
        {
            Cancel();Elapsed=0;Fired=false;Running=true;present=presentation;
            var cells=column.Where(s=>s).OrderBy(s=>s.transform.position.y).ToArray();
            if(cells.Length==0){Cancel();yield break;}
            cell=LM.squareWidth*Mathf.Abs(LM.GameField.lossyScale.x);
            float bottom=LM.squaresArray.Where(s=>s&&!s.IsNone()).Min(s=>s.transform.position.y);
            float top=LM.squaresArray.Where(s=>s&&!s.IsNone()).Max(s=>s.transform.position.y);
            seat=new Vector3(cells[0].transform.position.x,bottom-cell*.55f,-.5f);
            // Native skeleton has a 252px setup width, authored for a 140px cell and scaled .8 in its prefab.
            cannon=SkeletonAnimation.NewSkeletonAnimationGameObject(Resources.Load<SkeletonDataAsset>("ReferenceGameplay/Cannon/CannonSkeleton"));
            cannon.name="Cannon - imported source motion";owned.Add(cannon.gameObject);cannon.enabled=false;
            cannon.transform.localScale=Vector3.one*(cell*.8f);cannon.GetComponent<MeshRenderer>().sortingOrder=37;
            cannon.AnimationState.SetAnimation(0,"gercek_gelis_loop",true);
            fuseBone=cannon.Skeleton.FindBone("fitil_bone");muzzleSlot=cannon.Skeleton.Slots.FirstOrDefault(s=>s.Bone.Data.Name=="kafa_bone");
            moveSound=Resources.Load<AudioClip>("ReferenceGameplay/Cannon/cannon_move");
            useSound=Resources.Load<AudioClip>("ReferenceGameplay/Cannon/cannon_use");
            fireSound=Resources.Load<AudioClip>("ReferenceGameplay/Cannon/cannon_explode");Sound(moveSound);
            fuse=Effect("CannonFuseRfv387",seat,cell);
            // Allocate the source hierarchies before the timed motion, avoiding a launch-frame Instantiate spike.
            flash=Effect("CannonFireRfv387",seat,cell,false);ball=Effect("CannonBallRfv387",seat,cell,false);
            firing=false;bool escaped=false;int next=0;float shotTime=AnticipationStart+FireFrame;
            Vector3 muzzle=seat,exit=new Vector3(seat.x,top+cell*1.8f,seat.z);
            try
            {
                while(Elapsed<RecoilEnd+BarReturn)
                {
                    bool pausedNow=LM&&LM.gameStatus==GameState.Pause;Pause(pausedNow);
                    if(pausedNow){yield return null;continue;}
                    float previous=Elapsed;Elapsed+=Time.deltaTime;
                    present?.Invoke(Elapsed);
                    if(!Fired&&Elapsed>=shotTime)
                    {
                        // Sample exactly at launch, even when a slow frame crosses the cue.
                        AdvanceMotion(previous,shotTime);muzzle=Muzzle();exit.x=muzzle.x;
                        Fired=true;StopEmission(fuse);StartEffect(flash,muzzle);StartEffect(ball,muzzle);Sound(fireSound);
                        AdvanceMotion(shotTime,Elapsed);
                    }else AdvanceMotion(previous,Elapsed);
                    cannon.LateUpdate();
                    if(Elapsed>=RecoilEnd)cannon.gameObject.SetActive(false);
                    if(fuse)fuse.transform.position=cannon.transform.TransformPoint(new Vector3(fuseBone.WorldX,fuseBone.WorldY,0));
                    if(Fired)
                    {
                        float progress=FlightProgress(Elapsed-shotTime);
                        var position=Vector3.Lerp(muzzle,exit,progress);
                        if(ball)ball.transform.position=position;
                        // Sweep all crossed cells even on a slow frame; a cell is never damaged twice.
                        while(next<cells.Length&&position.y>=cells[next].transform.position.y){hit(cells[next]);next++;}
                        if(progress>=1&&!escaped&&ball){escaped=true;StopEmission(ball);foreach(var r in ball.GetComponentsInChildren<SpriteRenderer>())r.enabled=false;}
                    }
                    yield return null;
                }
                while(next<cells.Length){hit(cells[next]);next++;}
            }
            finally{Cancel();}
        }
        public void Cancel()
        {
            var restore=present;present=null;restore?.Invoke(-1);
            foreach(var go in owned)if(go)Destroy(go);owned.Clear();paused.Clear();particles.Clear();cannon=null;ball=flash=fuse=null;fuseBone=null;muzzleSlot=null;Running=false;
            if(audioOwner){audioOwner.StopClip(moveSound);audioOwner.StopClip(useSound);audioOwner.StopClip(fireSound);}
            audioOwner=null;
        }
        void OnDisable()=>Cancel();
    }
}
