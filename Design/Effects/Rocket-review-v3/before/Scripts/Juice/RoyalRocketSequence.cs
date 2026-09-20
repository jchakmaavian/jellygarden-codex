using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JellyGarden.Scripts.Juice
{
    public sealed partial class RoyalPowerupDirector
    {
        Material rocketCoreMaterial, rocketTrailMaterial, rocketFireMaterial;
        ParticleSystem rocketImpact, rocketGlow, rocketStars, rocketEmbers;
        ParticleSystem[] rocketSystems;
        Sprite[] rocketHeads;
        Mesh rocketFlameMesh;
        float rocketClock;
        public int RocketCellImpacts { get; private set; }
        public int RocketLaunches { get; private set; }
        public int RocketBreaks { get; private set; }
        public int RocketSparkParticlesEmitted { get; private set; }
        public float LastRocketSweepDuration { get; private set; }
        public float LastRocketAnticipation { get; private set; }
        public readonly Vector3[] RocketImpactPositions = new Vector3[512];
        public readonly float[] RocketImpactTimes = new float[512];
        public readonly float[] RocketBreakTimes = new float[512];
        public int RocketParticleCount { get { int n=0;if(rocketSystems!=null)foreach(var ps in rocketSystems)n+=ps.particleCount;return n; } }
        public int RocketExhaustCount { get { int n=0;if(flyers!=null)foreach(var f in flyers)if(f.used&&f.exhaust.enabled)n++;return n; } }
        public bool RocketUsesCustomVfx => rocketSystems!=null && rocketSystems.Length==4 && rocketFireMaterial && rocketFireMaterial.shader.name=="RoyalThrone/RocketFire";

        void BuildRocketVfx()
        {
            rocketCoreMaterial=Resources.Load<Material>("RoyalRocketFX/CoreTrail");
            rocketTrailMaterial=Resources.Load<Material>("RoyalRocketFX/GlowTrail");
            // Only the time uniform is mutable; authored materials and source art stay unchanged.
            rocketFireMaterial=new Material(Resources.Load<Material>("RoyalRocketFX/FireExhaust")){name="Rocket fire exhaust - scaled clock"};
            rocketImpact=CreateRocketParticles("Rocket contact rings","Ring",128,true);
            rocketGlow=CreateRocketParticles("Rocket launch and contact light","Flash",128,false);
            rocketStars=CreateRocketParticles("Rocket white gold stars","Star",2048,false);
            rocketEmbers=CreateRocketParticles("Rocket fire embers","Flash",512,false);
            rocketSystems=new[]{rocketImpact,rocketGlow,rocketStars,rocketEmbers};
            // Cached views split the supplied two-headed sprite at its centre. Pixels are untouched.
            rocketHeads=new[]{RocketHalf(false,false),RocketHalf(false,true),RocketHalf(true,false),RocketHalf(true,true)};
            rocketFlameMesh=new Mesh{name="Rocket exhaust quad"};
            rocketFlameMesh.vertices=new[]{new Vector3(-1.65f,-.48f,0),new Vector3(.035f,-.48f,0),new Vector3(.035f,.48f,0),new Vector3(-1.65f,.48f,0)};
            rocketFlameMesh.uv=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(0,1)};
            rocketFlameMesh.colors=new[]{Color.white,Color.white,Color.white,Color.white};
            rocketFlameMesh.triangles=new[]{0,1,2,0,2,3};rocketFlameMesh.RecalculateBounds();
        }
        ParticleSystem CreateRocketParticles(string name,string material,int capacity,bool expanding)
        {
            var go=new GameObject(name);go.transform.SetParent(transform,false);
            var ps=go.AddComponent<ParticleSystem>();ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;main.loop=true;main.playOnAwake=false;main.stopAction=ParticleSystemStopAction.None;
            main.simulationSpace=ParticleSystemSimulationSpace.World;main.scalingMode=ParticleSystemScalingMode.Shape;
            main.useUnscaledTime=false;main.startSpeed=0;main.startSize=1;main.gravityModifier=0;main.maxParticles=capacity;
            var emission=ps.emission;emission.enabled=false;var shape=ps.shape;shape.enabled=false;
            var renderer=go.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=Resources.Load<Material>("RoyalRocketFX/"+material);
            renderer.renderMode=ParticleSystemRenderMode.Billboard;renderer.alignment=ParticleSystemRenderSpace.View;renderer.sortingOrder=38;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            var colour=ps.colorOverLifetime;colour.enabled=true;
            var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},
                new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(.85f,.28f),new GradientAlphaKey(0,1)});colour.color=gradient;
            var size=ps.sizeOverLifetime;size.enabled=true;
            size.size=new ParticleSystem.MinMaxCurve(1,expanding?AnimationCurve.EaseInOut(0,.45f,1,1.25f):new AnimationCurve(new Keyframe(0,.9f),new Keyframe(.2f,1),new Keyframe(1,.12f)));
            var rotation=ps.rotationOverLifetime;rotation.enabled=material=="Star";rotation.z=new ParticleSystem.MinMaxCurve(-2.2f,2.2f);
            ps.useAutoRandomSeed=false;ps.randomSeed=(uint)(37183+capacity);return ps;
        }
        Sprite RocketHalf(bool vertical,bool positive)
        {
            var source=RoyalGameplayArt.Art(vertical?"rocket-vertical":"rocket");
            if(!source)return null;
            Rect rect=source.rect;Vector2 pivot=new Vector2(source.pivot.x/rect.width,source.pivot.y/rect.height);
            if(vertical){rect.height*=.5f;if(positive)rect.y+=rect.height;pivot.y=positive?0:1;}
            else {rect.width*=.5f;if(positive)rect.x+=rect.width;pivot.x=positive?0:1;}
            var half=Sprite.Create(source.texture,rect,pivot,source.pixelsPerUnit,0,SpriteMeshType.FullRect);
            half.name="RoyalRocket_"+(vertical?(positive?"Up":"Down"):(positive?"Right":"Left"));return half;
        }
        MeshRenderer CreateRocketExhaust(Transform parent,int index)
        {
            var go=new GameObject("Fire exhaust");go.transform.SetParent(parent,false);
            go.AddComponent<MeshFilter>().sharedMesh=rocketFlameMesh;
            var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=rocketFireMaterial;renderer.sortingOrder=34;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            var props=new MaterialPropertyBlock();props.SetFloat("_Phase",index*.37f);renderer.SetPropertyBlock(props);renderer.enabled=false;return renderer;
        }
        void OnDestroy()
        {
            if(rocketFireMaterial)Destroy(rocketFireMaterial);if(rocketFlameMesh)Destroy(rocketFlameMesh);
            if(rocketHeads!=null)foreach(var sprite in rocketHeads)if(sprite)Destroy(sprite);
        }
        void UpdateRocketVfx()
        {
            if(!Paused){rocketClock+=Time.deltaTime;if(rocketFireMaterial)rocketFireMaterial.SetFloat("_FxTime",rocketClock);}
            if(rocketSystems==null)return;
            foreach(var ps in rocketSystems){if(Paused&&!ps.isPaused)ps.Pause(false);else if(!Paused&&ps.isPaused)ps.Play(false);}
        }
        void ClearRocketVfx()
        {
            if(rocketSystems!=null)foreach(var ps in rocketSystems)ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
            if(flyers!=null)foreach(var f in flyers)f.exhaust.enabled=false;
        }
        void RocketImpact(Vector3 position,bool end=false)
        {
            Emit(rocketGlow,position,1,end?1.15f:.70f,0,end?.20f:.13f,new Color(1,.96f,.70f,end?.85f:.72f));
            Emit(rocketImpact,position,1,end?1.32f:.83f,0,.28f,new Color(1,.70f,.19f,.85f));
            Emit(rocketStars,position,end?22:10,end?.22f:.17f,end?2.5f:1.6f,.43f,new Color(1,.96f,.75f,1));
        }
        void RocketExhaustSparks(Vector3 nozzle,Vector3 direction)
        {
            Vector3 across=new Vector3(-direction.y,direction.x,0);
            int count=RoyalThrone.Testing.RoyalSave.GetInt("RoyalThrone.HomeMotion",1)==0?1:3;
            for(int i=0;i<count;i++)
            {
                var e=new ParticleSystem.EmitParams{
                    position=nozzle-direction*Cell*Range(.10f,.85f)+across*Cell*Range(-.18f,.18f)+Vector3.back*.5f,
                    velocity=(-direction*Range(.3f,1.3f)+across*Range(-.6f,.6f))*Cell,
                    startSize=Cell*Range(.105f,.22f),startLifetime=Range(.30f,.52f),rotation=Range(0,90),
                    startColor=i==0?new Color(1,1,.92f,1):new Color(1,.82f,.35f,.95f),applyShapeToPosition=false};
                rocketStars.Emit(e,1);RocketSparkParticlesEmitted++;
            }
            if(!rocketStars.isPlaying)rocketStars.Play(false);
            Emit(rocketEmbers,nozzle-direction*Cell*.24f,1,.085f,1.4f,.32f,new Color(1,.42f,.055f,.72f));
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
        // QA replays share the live flight/exhaust, with board mutation disabled.
        IEnumerator PreviewRocket(Vector3 position,bool horizontal)
        {
            var lm=LevelManager.THIS;if(!lm||lm.squaresArray==null)yield break;
            Square nearest=null;float distance=float.PositiveInfinity;
            foreach(var sq in lm.squaresArray)
                if(sq&&!sq.IsNone())
                {float d=(sq.transform.position-position).sqrMagnitude;if(d<distance){nearest=sq;distance=d;}}
            if(nearest)yield return Rocket(nearest,horizontal,1,false);
        }
        IEnumerator Rocket(Square origin,bool horizontal,int lanes,bool clearCells=true)
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
            var source=RoyalGameplayArt.Art(horizontal?"rocket":"rocket-vertical");
            float fit=pitch*.90f/Mathf.Max(source.bounds.size.x,source.bounds.size.y);
            for(int lane=-lanes/2;lane<=lanes/2;lane++)for(int d=-1;d<=1;d+=2)
            {
                var f=Take("rocket",p+across*lane*crossPitch,.72f);
                if(f!=null)
                {
                    f.rocket=true;f.sprite.sprite=rocketHeads[(horizontal?0:2)+(d>0?1:0)];
                    f.go.transform.rotation=Quaternion.identity;f.go.transform.localScale=Vector3.one*fit;
                    f.exhaust.transform.localRotation=Quaternion.Euler(0,0,horizontal?(d<0?180:0):(d<0?-90:90));
                    f.exhaust.transform.localScale=Vector3.one*(pitch/fit);
                    f.trail.sharedMaterial=rocketTrailMaterial;f.trail.emitting=false;f.trail.time=.42f;f.trail.widthMultiplier=.72f*pitch;
                    f.trail.startColor=new Color(1,.61f,.08f,.80f);f.trail.endColor=new Color(1,.22f,.015f,0);
                    f.core.time=.36f;f.core.widthMultiplier=.18f*pitch;
                }
                active.Add(f);
            }
            float anticipation=0;
            while(anticipation<LastRocketAnticipation)
            {
                if(!Paused)anticipation+=Time.deltaTime;
                float compression=1f-.055f*Mathf.Sin(Mathf.Clamp01(anticipation/LastRocketAnticipation)*Mathf.PI);
                foreach(var f in active)if(f!=null)f.go.transform.localScale=Vector3.one*fit*compression;
                yield return null;
            }
            RocketImpact(p,true);RoyalAudioDirector.Get?.Play(RoyalCue.Rocket);
            foreach(var f in active)if(f!=null){f.go.transform.localScale=Vector3.one*fit;f.trail.Clear();f.core.Clear();f.trail.emitting=true;f.core.emitting=true;f.exhaust.enabled=true;}
            float t=0;int stepDone=-1,sparkStepDone=-1;
            while(t<duration)
            {
                if(Paused){yield return null;continue;}
                // Ease ignition into travel; cap a hitch so a visible head cannot skip a cell.
                t=Mathf.Min(duration,t+Mathf.Min(Time.deltaTime,duration/reach*.30f));
                float u=t/duration;float distance=reach*(u*u*(2-u));int index=0;
                // Each cut sprite extends 0.45 cell in front of the moving nozzle.
                float noseDistance=distance+.45f;
                for(int lane=-lanes/2;lane<=lanes/2;lane++)for(int d=-1;d<=1;d+=2)
                {
                    int edge=d<0?negative:positive;var f=active[index++];
                    Vector3 head=p+across*lane*crossPitch+axis*d*Mathf.Min(distance,edge+.8f)*pitch;
                    if(f!=null&&f.sprite.enabled)f.go.transform.position=head+Vector3.back*.6f;
                    for(int sample=sparkStepDone+1;sample<=Mathf.FloorToInt(Mathf.Min(distance,edge+.8f)/.10f);sample++)
                        RocketExhaustSparks(p+across*lane*crossPitch+axis*d*(sample*.10f)*pitch,axis*d);
                    if(distance>=edge+.8f&&f!=null&&f.sprite.enabled)
                    {RocketImpact(head,true);StartCoroutine(Retire(f));active[index-1]=null;}
                    for(int step=stepDone+1;step<=Mathf.Min(edge,Mathf.FloorToInt(noseDistance));step++)
                    {
                        int col=origin.col+(horizontal?step*d:lane),row=origin.row+(horizontal?-lane:-step*d);
                        var sq=lm.GetSquare(col,row);
                        if(sq&&!sq.IsNone()&&hit.Add(sq))
                        {if(clearCells)StartOperation(RocketCell(sq));else RocketImpact(sq.transform.position);}
                    }
                }
                stepDone=Mathf.FloorToInt(noseDistance);sparkStepDone=Mathf.FloorToInt(distance/.10f);
                yield return null;
            }
            foreach(var f in active)if(f!=null&&f.sprite.enabled){RocketImpact(f.go.transform.position,true);StartCoroutine(Retire(f));}
            Reaction?.Invoke(.8f);
        }
    }
}
