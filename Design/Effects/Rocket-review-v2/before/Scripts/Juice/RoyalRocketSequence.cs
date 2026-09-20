using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JellyGarden.Scripts.Juice
{
    public sealed partial class RoyalPowerupDirector
    {
        Material rocketCoreMaterial;
        ParticleSystem rocketImpact, rocketGlow, rocketStars;
        ParticleSystem[] rocketSystems;
        public int RocketCellImpacts { get; private set; }
        public int RocketLaunches { get; private set; }
        public int RocketBreaks { get; private set; }
        public float LastRocketSweepDuration { get; private set; }
        public float LastRocketAnticipation { get; private set; }
        // Fixed diagnostics: permit a regression to compare travel and mutation order.
        public readonly Vector3[] RocketImpactPositions = new Vector3[512];
        public readonly float[] RocketImpactTimes = new float[512];
        public readonly float[] RocketBreakTimes = new float[512];
        public int RocketParticleCount { get { int n=0;if(rocketSystems!=null)foreach(var ps in rocketSystems)n+=ps.particleCount;return n; } }
        void BuildRocketVfx()
        {
            rocketCoreMaterial=new Material(Resources.Load<Material>("RoyalGameplay/Trail")){name="Rocket white-hot core",mainTexture=Texture2D.whiteTexture};
            rocketCoreMaterial.SetFloat("_Boost",1.7f);
            var library=Resources.Load<RoyalToolkitLibrary>("RoyalGameplay/Toolkit/Library");
            if(!library || !library.ice)return;
            var rig=Instantiate(library.ice,transform);rig.name="Rocket AllIn1 impact and stars - shared emitters";
            rig.transform.localScale=Vector3.one;rig.SetActive(true);
            rocketSystems=rig.GetComponentsInChildren<ParticleSystem>(true);
            foreach(var ps in rocketSystems)
            {
                ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.transform.localScale=Vector3.one;
                var main=ps.main;main.loop=false;main.playOnAwake=false;main.stopAction=ParticleSystemStopAction.None;
                main.simulationSpace=ParticleSystemSimulationSpace.World;main.scalingMode=ParticleSystemScalingMode.Shape;
                main.useUnscaledTime=false;main.startDelay=0;main.gravityModifier=0;main.maxParticles=ps.name.Contains("Sparks")?768:64;
                var emission=ps.emission;emission.enabled=false;
                var shape=ps.shape;shape.enabled=false;
                var force=ps.forceOverLifetime;force.enabled=false;
                var velocity=ps.velocityOverLifetime;velocity.enabled=false;
                var noise=ps.noise;noise.enabled=false;
                var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sortingOrder=39;
                if(ps.name=="ImpactRing")rocketImpact=ps;
                else if(ps.name=="ImpactGlow")rocketGlow=ps;
                else if(ps.name=="ImpactSparks")rocketStars=ps;
                else renderer.enabled=false;
                var colour=ps.colorOverLifetime;colour.enabled=true;
                var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},
                    new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(.85f,.35f),new GradientAlphaKey(0,1)});colour.color=gradient;
                var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.EaseInOut(0,.9f,1,.15f));
                ps.useAutoRandomSeed=false;ps.randomSeed=37183;
            }
        }
        void OnDestroy(){if(rocketCoreMaterial)Destroy(rocketCoreMaterial);}
        void UpdateRocketVfx()
        {
            if(rocketSystems==null)return;
            foreach(var ps in rocketSystems){if(Paused&&!ps.isPaused)ps.Pause(false);else if(!Paused&&ps.isPaused)ps.Play(false);}
        }
        void ClearRocketVfx(){if(rocketSystems!=null)foreach(var ps in rocketSystems)ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);}
        void RocketImpact(Vector3 position,bool end=false)
        {
            Emit(rocketImpact,position,1,end?.85f:.62f,0,.23f,Color.white);
            Emit(rocketGlow,position,1,end?1.1f:.78f,0,.18f,new Color(1,.87f,.48f,.72f));
            Emit(rocketStars,position,end?12:7,.16f,1.6f,.34f,new Color(1,.96f,.76f,1));
        }
        IEnumerator RocketCell(Square sq)
        {
            if(!sq || sq.IsNone())yield break;
            int serial=RocketCellImpacts++;int slot=serial%RocketImpactTimes.Length;
            RocketImpactPositions[slot]=sq.transform.position;RocketImpactTimes[slot]=Time.time;RocketBreakTimes[slot]=-1;
            RocketImpact(sq.transform.position);
            // The actual moving head reaches the square first. Keep the intact asset
            // visible for the white contact flash before invoking existing destruction.
            float contact=0;
            while(contact<.055f){yield return null;if(!Paused)contact+=Time.deltaTime;}
            if(!sq || sq.type==SquareTypes.UNDESTROYABLE)yield break;
            RocketBreakTimes[slot]=Time.time;RocketBreaks++;
            if(sq.item && sq.item.currentType!=ItemsTypes.INGREDIENT)
            {
                sq.item.royalRocketClear=true;sq.item.royalBlastClear=true;
                sq.item.juiceStagger=0;sq.item.DestroyItem(true);
            }
            else if(sq.block.Count>0)sq.DestroyBlock(true);
        }
        IEnumerator Rocket(Square origin,bool horizontal,int lanes)
        {
            if(!origin)yield break;
            var lm=LevelManager.THIS;Vector3 p=origin.transform.position;
            Vector3 axis=horizontal?Vector3.right:Vector3.up;
            Vector3 across=horizontal?Vector3.up:Vector3.right;
            float pitch=Mathf.Abs(horizontal?lm.squareWidth*lm.GameField.lossyScale.x:lm.squareHeight*lm.GameField.lossyScale.y);
            float crossPitch=Mathf.Abs(horizontal?lm.squareHeight*lm.GameField.lossyScale.y:lm.squareWidth*lm.GameField.lossyScale.x);
            int negative=horizontal?origin.col:lm.maxRows-origin.row-1;
            int positive=horizontal?lm.maxCols-origin.col-1:origin.row;
            float reach=Mathf.Max(negative,positive)+.8f;
            float duration=Mathf.Clamp(reach/Mathf.Max(1,RoyalEffectsProfile.Get.rocketCellsPerSecond),.8f,1.2f);
            LastRocketSweepDuration=duration;LastRocketAnticipation=Mathf.Clamp(RoyalEffectsProfile.Get.rocketAnticipation,.15f,.3f);
            RocketLaunches++;
            var active=new List<Flyer>(lanes*2);var hit=new HashSet<Square>();
            for(int lane=-lanes/2;lane<=lanes/2;lane++)for(int d=-1;d<=1;d+=2)
            {
                var f=Take("rocket",p+across*lane*crossPitch,.72f);
                if(f!=null)
                {
                    f.rocket=true;f.go.transform.rotation=Quaternion.Euler(0,0,horizontal?(d<0?180:0):(d<0?-90:90));
                    f.trail.emitting=false;f.trail.time=.32f;f.trail.widthMultiplier=.38f*pitch;
                    f.trail.startColor=new Color(1,.67f,.15f,.7f);f.trail.endColor=new Color(1,.3f,.025f,0);
                    f.core.time=.28f;f.core.widthMultiplier=.09f*pitch;
                }
                active.Add(f);
            }
            float anticipation=0;
            while(anticipation<LastRocketAnticipation)
            {
                if(!Paused)anticipation+=Time.deltaTime;
                float compression=1f-.07f*Mathf.Sin(Mathf.Clamp01(anticipation/LastRocketAnticipation)*Mathf.PI);
                foreach(var f in active)if(f!=null)f.go.transform.localScale=Vector3.one*pitch*.72f*compression;
                yield return null;
            }
            RocketImpact(p,true);RoyalAudioDirector.Get?.Play(RoyalCue.Rocket);
            foreach(var f in active)if(f!=null){f.trail.Clear();f.core.Clear();f.trail.emitting=true;f.core.emitting=true;}
            float t=0,nextSpark=0;
            int stepDone=-1;
            while(t<duration)
            {
                if(Paused){yield return null;continue;}
                // Do not jump over a cell during a render hitch; the head must visibly lead.
                t=Mathf.Min(duration,t+Mathf.Min(Time.deltaTime,duration/reach*.4f));float distance=reach*t/duration;int index=0;
                for(int lane=-lanes/2;lane<=lanes/2;lane++)for(int d=-1;d<=1;d+=2)
                {
                    int edge=d<0?negative:positive;var f=active[index++];
                    Vector3 head=p+across*lane*crossPitch+axis*d*Mathf.Min(distance,edge+.8f)*pitch;
                    if(f!=null && f.sprite.enabled)f.go.transform.position=head+Vector3.back*.6f;
                    if(distance<=edge+.8f && t>=nextSpark)
                        Emit(rocketStars,head-axis*d*pitch*.25f,3,.11f,.65f,.34f,new Color(1,.95f,.7f,.9f));
                    if(distance>=edge+.8f && f!=null && f.sprite.enabled)
                    {RocketImpact(head,true);StartCoroutine(Retire(f));active[index-1]=null;}
                    for(int step=stepDone+1;step<=Mathf.Min(edge,Mathf.FloorToInt(distance));step++)
                    {
                        int col=origin.col+(horizontal?step*d:lane),row=origin.row+(horizontal?-lane:-step*d);
                        var sq=lm.GetSquare(col,row);if(sq&&hit.Add(sq))StartOperation(RocketCell(sq));
                    }
                }
                stepDone=Mathf.FloorToInt(distance);
                if(t>=nextSpark)nextSpark=t+.02f;
                yield return null;
            }
            foreach(var f in active)if(f!=null&&f.sprite.enabled){RocketImpact(f.go.transform.position,true);StartCoroutine(Retire(f));}
            Reaction?.Invoke(.8f);
        }
    }
}
