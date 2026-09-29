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
        public const float Entrance=.32f, Aim=.28f, FireFrame=.20f, Flight=.42f;
        public const float RecoilEnd=Entrance+Aim+.9f, BarReturn=.24f;
        public float Elapsed {get;private set;}
        public bool Fired {get;private set;}
        public bool Running {get;private set;}
        SkeletonAnimation cannon;
        GameObject ball,flash,fuse;
        AudioClip moveSound,useSound,fireSound;
        RoyalAudioDirector audioOwner;
        Action<float> present;
        readonly List<GameObject> owned=new List<GameObject>();
        readonly List<ParticleSystem> paused=new List<ParticleSystem>();
        LevelManager LM=>LevelManager.THIS;
        public static bool Available=>Resources.Load<SkeletonDataAsset>("ReferenceGameplay/Cannon/CannonSkeleton")&&Resources.Load<GameObject>("ReferenceGameplay/VFX/CannonBallRfv387");

        GameObject Effect(string key,Vector3 position,float cell)
        {
            var template=Resources.Load<GameObject>("ReferenceGameplay/VFX/"+key);
            if(!template)return null;
            var go=Instantiate(template);owned.Add(go);go.transform.SetPositionAndRotation(position,Quaternion.identity);
            go.transform.localScale=template.transform.localScale*cell;
            var sort=go.GetComponent<SortingGroup>();if(!sort)sort=go.AddComponent<SortingGroup>();sort.sortingOrder=38;
            foreach(var ps in go.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main=ps.main;main.scalingMode=ParticleSystemScalingMode.Hierarchy;
                ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
                if(ps.gameObject.activeInHierarchy)ps.Play(false);
            }
            return go;
        }
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
            float cell=LM.squareWidth*Mathf.Abs(LM.GameField.lossyScale.x);
            float bottom=LM.squaresArray.Where(s=>s&&!s.IsNone()).Min(s=>s.transform.position.y);
            float top=LM.squaresArray.Where(s=>s&&!s.IsNone()).Max(s=>s.transform.position.y);
            Vector3 seat=new Vector3(cells[0].transform.position.x,bottom-cell*.55f,-.5f);
            // Native skeleton has a 252px setup width, authored for a 140px cell and scaled .8 in its prefab.
            cannon=SkeletonAnimation.NewSkeletonAnimationGameObject(Resources.Load<SkeletonDataAsset>("ReferenceGameplay/Cannon/CannonSkeleton"));
            cannon.name="Cannon - imported source motion";owned.Add(cannon.gameObject);cannon.enabled=false;
            cannon.transform.localScale=Vector3.one*(cell*.8f);cannon.GetComponent<MeshRenderer>().sortingOrder=37;
            cannon.AnimationState.SetAnimation(0,"gercek_gelis_loop",true);
            moveSound=Resources.Load<AudioClip>("ReferenceGameplay/Cannon/cannon_move");
            useSound=Resources.Load<AudioClip>("ReferenceGameplay/Cannon/cannon_use");
            fireSound=Resources.Load<AudioClip>("ReferenceGameplay/Cannon/cannon_explode");Sound(moveSound);
            fuse=Effect("CannonFuseRfv387",seat,cell);
            bool firing=false;int next=0;float shotTime=Entrance+Aim+FireFrame;
            Vector3 muzzle=seat+Vector3.up*(.34f*cell),exit=new Vector3(seat.x,top+cell*1.8f,seat.z);
            try
            {
                while(Elapsed<RecoilEnd+BarReturn)
                {
                    bool pausedNow=LM&&LM.gameStatus==GameState.Pause;Pause(pausedNow);
                    if(pausedNow){yield return null;continue;}
                    float dt=Time.deltaTime;Elapsed+=dt;
                    present?.Invoke(Elapsed);
                    float u=Mathf.Clamp01(Elapsed/Entrance);float ease=1-Mathf.Pow(1-u,3);
                    cannon.transform.position=seat+Vector3.down*(1-ease)*cell*2.5f;
                    if(!firing&&Elapsed>=Entrance+Aim)
                    {firing=true;cannon.AnimationState.SetAnimation(0,"gercek_patlama",false);Sound(useSound);}
                    cannon.Update(dt);cannon.LateUpdate();
                    // Return the controls only after the source firing/recoil clip finishes.
                    if(Elapsed>=RecoilEnd)cannon.gameObject.SetActive(false);
                    if(fuse){var bone=cannon.Skeleton.FindBone("fitil_bone");fuse.transform.position=cannon.transform.TransformPoint(new Vector3(bone.WorldX,bone.WorldY,0));}
                    if(!Fired&&Elapsed>=shotTime)
                    {
                        Fired=true;StopEmission(fuse);flash=Effect("CannonFireRfv387",muzzle,cell);
                        ball=Effect("CannonBallRfv387",muzzle,cell);Sound(fireSound);
                    }
                    if(Fired)
                    {
                        float progress=Mathf.Clamp01((Elapsed-shotTime)/Flight);
                        var position=Vector3.Lerp(muzzle,exit,progress);
                        if(ball)ball.transform.position=position;
                        // Sweep all crossed cells even on a slow frame; a cell is never damaged twice.
                        while(next<cells.Length&&position.y>=cells[next].transform.position.y){hit(cells[next]);next++;}
                        if(progress>=1&&ball){StopEmission(ball);foreach(var r in ball.GetComponentsInChildren<SpriteRenderer>())r.enabled=false;}
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
            foreach(var go in owned)if(go)Destroy(go);owned.Clear();paused.Clear();cannon=null;ball=flash=fuse=null;Running=false;
            if(audioOwner){audioOwner.StopClip(moveSound);audioOwner.StopClip(useSound);audioOwner.StopClip(fireSound);}
            audioOwner=null;
        }
        void OnDisable()=>Cancel();
    }
}
