using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JellyGarden.Scripts.Juice
{
    public sealed partial class RoyalPowerupDirector
    {
        Material rocketCoreMaterial, rocketTrailMaterial, rocketFireMaterial, rocketRibbonMaterial;
        Transform rocketVfxRoot;
        ParticleSystem rocketImpact, rocketGlow, rocketStars, rocketEmbers, rocketSmoke, rocketExhaustStars;
        [SerializeField,Range(6f,8f)] float rocketExhaustCells=7f;
        public float RocketExhaustLengthCells { get=>rocketExhaustCells; set=>rocketExhaustCells=Mathf.Clamp(value,6f,8f); }
        ParticleSystem[] rocketSystems;
        Sprite[] rocketHeads;
        Mesh rocketFlameMesh;
        float rocketClock;
        const float RocketExhaustSampleCells=.075f;
        public int RocketCellImpacts { get; private set; }
        public int RocketLaunches { get; private set; }
        public int RocketBreaks { get; private set; }
        public int RocketSparkParticlesEmitted { get; private set; }
        public float LastRocketSweepDuration { get; private set; }
        public float LastRocketAnticipation { get; private set; }
        public int RocketScreenExits { get; private set; }
        public readonly Vector3[] RocketExitViewports = new Vector3[128];
        public bool RocketVisualsActive { get { if(RocketParticleCount>0)return true;if(flyers!=null)foreach(var f in flyers)if(f.used&&f.rocket)return true;return false; } }
        public readonly Vector3[] RocketImpactPositions = new Vector3[512];
        public readonly float[] RocketImpactTimes = new float[512];
        public readonly float[] RocketBreakTimes = new float[512];
        public int RocketParticleCount { get { int n=0;if(rocketSystems!=null)foreach(var ps in rocketSystems)n+=ps.particleCount;return n; } }
        public int RocketExhaustCount { get { int n=0;if(flyers!=null)foreach(var f in flyers)if(f.used&&f.exhaust.enabled)n++;return n; } }
        public bool RocketUsesCustomVfx => rocketSystems!=null && rocketSystems.Length==6 && rocketFireMaterial && rocketFireMaterial.shader.name=="RoyalThrone/RocketFire";

        void BuildRocketVfx()
        {
            rocketVfxRoot=new GameObject("GameplayVFX - unmasked rocket line clear").transform;
            rocketVfxRoot.SetParent(transform,false);
            rocketCoreMaterial=Resources.Load<Material>("RoyalRocketFX/CoreTrail");
            rocketTrailMaterial=Resources.Load<Material>("RoyalRocketFX/GlowTrail");
            // Only the time uniform is mutable; authored materials and source art stay unchanged.
            rocketFireMaterial=new Material(Resources.Load<Material>("RoyalRocketFX/FireExhaust")){name="Rocket fire exhaust - scaled clock"};
            rocketRibbonMaterial=new Material(Resources.Load<Material>("RoyalRocketFX/FlameTrail")){name="Rocket flame ribbon - scaled clock"};
            rocketImpact=CreateRocketParticles("Rocket contact rings","Ring",128,true);
            rocketGlow=CreateRocketParticles("Rocket launch and contact light","Flash",128,false);
            rocketStars=CreateRocketParticles("Rocket white gold stars","Star",2048,false);
            rocketEmbers=CreateRocketParticles("Rocket fire embers","Flash",1024,false);
            rocketExhaustStars=CreateRocketParticles("StarSparkles - rocket exhaust","Star",2048,false);
            rocketSmoke=CreateRocketParticles("WhiteSmokeTail - rocket exhaust","WhiteSmoke",512,false);
            // Smoke is born with every spatial sample but stays transparent through the
            // flame body. It appears only in the final two cells, grows, then dissolves.
            var smokeColour=rocketSmoke.colorOverLifetime;var smokeFade=new Gradient();
            smokeFade.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},
                new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(0,.72f),new GradientAlphaKey(.14f,.82f),new GradientAlphaKey(.09f,.92f),new GradientAlphaKey(0,1)});smokeColour.color=smokeFade;
            var smokeSize=rocketSmoke.sizeOverLifetime;
            smokeSize.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.42f),new Keyframe(.72f,.62f),new Keyframe(1,1.85f)));
            // Gold at birth, cooling to white, and held at readable size through mid-plume so
            // individual four-point stars stay countable before they thin out.
            var starColour=rocketExhaustStars.colorOverLifetime;var starFade=new Gradient();
            starFade.SetKeys(new[]{new GradientColorKey(new Color(1,.84f,.40f),0),new GradientColorKey(new Color(1,.95f,.80f),.34f),new GradientColorKey(Color.white,.62f),new GradientColorKey(Color.white,1)},
                new[]{new GradientAlphaKey(.78f,0),new GradientAlphaKey(1,.18f),new GradientAlphaKey(.86f,.50f),new GradientAlphaKey(.28f,.84f),new GradientAlphaKey(0,1)});starColour.color=starFade;
            var starSize=rocketExhaustStars.sizeOverLifetime;
            starSize.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.80f),new Keyframe(.22f,1),new Keyframe(.62f,.78f),new Keyframe(1,.12f)));
            rocketSystems=new[]{rocketImpact,rocketGlow,rocketStars,rocketEmbers,rocketSmoke,rocketExhaustStars};
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
            var go=new GameObject(name);go.transform.SetParent(rocketVfxRoot,false);
            var ps=go.AddComponent<ParticleSystem>();ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;main.loop=true;main.playOnAwake=false;main.stopAction=ParticleSystemStopAction.None;
            main.simulationSpace=ParticleSystemSimulationSpace.World;main.scalingMode=ParticleSystemScalingMode.Shape;
            main.useUnscaledTime=false;main.startSpeed=0;main.startSize=1;main.gravityModifier=0;main.maxParticles=capacity;
            var emission=ps.emission;emission.enabled=false;var shape=ps.shape;shape.enabled=false;
            var renderer=go.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=Resources.Load<Material>("RoyalRocketFX/"+material);
            renderer.maskInteraction=SpriteMaskInteraction.None;renderer.sortingLayerName="Default";
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
            if(rocketFireMaterial)Destroy(rocketFireMaterial);if(rocketRibbonMaterial)Destroy(rocketRibbonMaterial);if(rocketFlameMesh)Destroy(rocketFlameMesh);
            if(rocketHeads!=null)foreach(var sprite in rocketHeads)if(sprite)Destroy(sprite);
        }
        void UpdateRocketVfx()
        {
            if(!Paused){rocketClock+=Time.deltaTime;if(rocketFireMaterial)rocketFireMaterial.SetFloat("_FxTime",rocketClock);if(rocketRibbonMaterial)rocketRibbonMaterial.SetFloat("_FxTime",rocketClock);}
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
        // Spatial samples make density independent of frame rate and the Lab time scale.
        // Star and ember lifetimes are derived from the live speed, so the plume keeps its
        // cell length at any rocket speed; only the tail wisps outlive the ribbon slightly.
        void RocketExhaustSparks(Vector3 nozzle,Vector3 direction,float pitch,float speed,float length,float travelled)
        {
            Vector3 across=new Vector3(-direction.y,direction.x,0);
            int count=RoyalThrone.Testing.RoyalSave.GetInt("RoyalThrone.HomeMotion",1)==0?2:4;
            for(int i=0;i<count;i++)
            {
                float behind=Mathf.Min(travelled,Range(.05f,.30f)),drift=Range(.08f,.28f);
                // Each star draws the fraction of the plume it is allowed to reach. A uniform
                // draw keeps every star alive near the nozzle and lets progressively fewer
                // survive toward the tail, so the trail thins out instead of ending as a block.
                float reach=Range(.43f,1f);
                float lifetime=Mathf.Max(.05f,(length*reach-behind-.10f)/(speed+drift));
                var e=new ParticleSystem.EmitParams{
                    position=nozzle-direction*pitch*behind+across*pitch*Range(-.20f,.20f)+Vector3.back*.5f,
                    velocity=(-direction*drift+across*Range(-.08f,.08f))*pitch,
                    startSize=pitch*(i==0&&Range(0,1)<.12f?Range(.32f,.43f):Range(.12f,.25f)),
                    startLifetime=lifetime,rotation=Range(0,360),
                    startColor=new Color(1,1,1,Range(.75f,1f)),applyShapeToPosition=false};
                rocketExhaustStars.Emit(e,1);RocketSparkParticlesEmitted++;
            }
            for(int i=0;i<2;i++)
            {
                // Gold embers carry the yellow far past the first cell, thinning out by mid-plume.
                float emberReach=Range(.30f,.66f);
                var e=new ParticleSystem.EmitParams{position=nozzle-direction*pitch*Mathf.Min(travelled,.18f)+across*pitch*Range(-.20f,.20f)+Vector3.back*.5f,
                    velocity=(-direction*.18f+across*Range(-.15f,.15f))*pitch,startSize=pitch*Range(.07f,.17f),
                    startLifetime=length*emberReach/(speed+.18f),startColor=new Color(1,.82f,.28f,.85f),applyShapeToPosition=false};
                rocketEmbers.Emit(e,1);
            }
            // One low-opacity wisp follows each spatial sample. Its lifetime matches the
            // seven-cell distance and its alpha curve hides it until the energy tail, so
            // smoke cannot form a detached bulb or a hard cap behind the ribbon.
            float smokeDrift=Range(.05f,.12f);
            var smokeParticle=new ParticleSystem.EmitParams{
                position=nozzle+across*pitch*Range(-.10f,.10f)+Vector3.back*.5f,
                velocity=(-direction*smokeDrift+across*Range(-.06f,.06f))*pitch,
                startSize=pitch*Range(.30f,.47f),startLifetime=length/(speed+smokeDrift)*Range(.96f,1.04f),
                rotation=Range(0,360),startColor=new Color(1,1,.96f,Range(.55f,.78f)),applyShapeToPosition=false};
            rocketSmoke.Emit(smokeParticle,1);
            if(!rocketExhaustStars.isPlaying)rocketExhaustStars.Play(false);
            if(!rocketEmbers.isPlaying)rocketEmbers.Play(false);
            if(!rocketSmoke.isPlaying)rocketSmoke.Play(false);
        }
        static Gradient RocketExhaustGradient(bool core)
        {
            var g=new Gradient();g.SetKeys(new[]{new GradientColorKey(core?new Color(1,.99f,.88f):new Color(1,.68f,.13f),0),
                new GradientColorKey(core?Color.white:new Color(1,.88f,.42f),.56f),new GradientColorKey(Color.white,.82f),new GradientColorKey(Color.white,1)},
                new[]{new GradientAlphaKey(core?.98f:.62f,0),new GradientAlphaKey(core?.90f:.48f,.56f),new GradientAlphaKey(core?.35f:.11f,.84f),new GradientAlphaKey(0,1)});return g;
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
        float RocketExitDistance(Vector3 origin,Vector3 direction,float pitch,float boardEdge)
        {
            var cam=Camera.main;if(!cam)return boardEdge+3f;
            float depth=Vector3.Dot(origin-cam.transform.position,cam.transform.forward);
            var min=cam.ViewportToWorldPoint(new Vector3(0,0,depth));
            var max=cam.ViewportToWorldPoint(new Vector3(1,1,depth));
            float boundary=direction.x!=0?(direction.x>0?max.x-origin.x:origin.x-min.x):(direction.y>0?max.y-origin.y:origin.y-min.y);
            // Continue until the whole nozzle flame is outside the camera, then let trails age out.
            return Mathf.Max(boardEdge+.8f,boundary/pitch+2.2f);
        }
        static Gradient RocketTrailGradient(Color tint,float hold)
        {
            var g=new Gradient();g.SetKeys(new[]{new GradientColorKey(tint,0),new GradientColorKey(tint,1)},
                new[]{new GradientAlphaKey(tint.a,0),new GradientAlphaKey(tint.a,hold),new GradientAlphaKey(0,1)});return g;
        }
        void RecordRocketExit(Vector3 position)
        {
            RocketExitViewports[RocketScreenExits++%RocketExitViewports.Length]=Camera.main?Camera.main.WorldToViewportPoint(position):position;
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
            // One constant cells-per-second speed. Origin, axis and direction never change it,
            // so horizontal and vertical sweeps read at the same pace and the same trail length.
            float speed=Mathf.Max(1,RoyalEffectsProfile.Get.rocketCellsPerSecond);
            float duration=reach/speed;
            LastRocketSweepDuration=duration;LastRocketAnticipation=Mathf.Clamp(RoyalEffectsProfile.Get.rocketAnticipation,.15f,.3f);
            RocketLaunches++;
            var active=new List<Flyer>(lanes*2);var exits=new List<float>(lanes*2);var hit=new HashSet<Square>();
            float maxExit=0;
            float exhaustLength=RocketExhaustLengthCells,exhaustLife=exhaustLength/speed;
            var source=RoyalGameplayArt.Art(horizontal?"rocket":"rocket-vertical");
            float fit=pitch*.90f/Mathf.Max(source.bounds.size.x,source.bounds.size.y);
            for(int lane=-lanes/2;lane<=lanes/2;lane++)for(int d=-1;d<=1;d+=2)
            {
                var laneOrigin=p+across*lane*crossPitch;
                float exit=RocketExitDistance(laneOrigin,axis*d,pitch,d<0?negative:positive);exits.Add(exit);maxExit=Mathf.Max(maxExit,exit);
                var f=Take("rocket",laneOrigin,.72f);
                if(f!=null)
                {
                    f.rocket=true;f.go.transform.SetParent(rocketVfxRoot,true);f.sprite.maskInteraction=SpriteMaskInteraction.None;f.sprite.sprite=rocketHeads[(horizontal?0:2)+(d>0?1:0)];
                    f.go.transform.rotation=Quaternion.identity;f.go.transform.localScale=Vector3.one*fit;
                    f.exhaust.transform.localRotation=Quaternion.Euler(0,0,horizontal?(d<0?180:0):(d<0?-90:90));
                    f.exhaust.transform.localScale=Vector3.one*(pitch/fit);
                    f.preRocketGradient=f.trail.colorGradient;
                    f.trail.sharedMaterial=rocketTrailMaterial;f.trail.emitting=false;f.trail.time=exhaustLife;f.trail.minVertexDistance=.035f*pitch;f.trail.widthMultiplier=.76f*pitch;
                    f.trail.widthCurve=new AnimationCurve(new Keyframe(0,1),new Keyframe(.50f,.80f),new Keyframe(.84f,.32f),new Keyframe(1,.015f));
                    f.trail.colorGradient=RocketExhaustGradient(false);
                    f.flame.widthMultiplier=.90f*pitch;f.flame.enabled=false;
                    f.flameProperties.SetFloat("_TravelFraction",0);f.flameProperties.SetFloat("_Phase",RocketLaunches*.73f+d);f.flame.SetPropertyBlock(f.flameProperties);
                    f.flame.startColor=f.flame.endColor=new Color(1,1,1,.88f);
                    f.flame.positionCount=2;f.flame.SetPosition(0,laneOrigin+Vector3.back*.6f);f.flame.SetPosition(1,laneOrigin+Vector3.back*.6f);
                    f.core.time=exhaustLife;f.core.minVertexDistance=.02f*pitch;f.core.widthMultiplier=.17f*pitch;
                    f.core.widthCurve=new AnimationCurve(new Keyframe(0,1),new Keyframe(.60f,.90f),new Keyframe(.88f,.22f),new Keyframe(1,0));
                    f.core.colorGradient=RocketExhaustGradient(true);
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
            foreach(var f in active)if(f!=null){f.go.transform.localScale=Vector3.one*fit;f.trail.Clear();f.core.Clear();f.trail.emitting=true;f.core.emitting=true;f.flame.enabled=true;f.exhaust.enabled=true;}
            float distance=0;int stepDone=-1,sparkStepDone=-1;
            while(distance<maxExit)
            {
                if(Paused){yield return null;continue;}
                // Anticipation owns ignition. Flight then keeps one constant velocity through board and screen.
                distance=Mathf.Min(maxExit,distance+speed*Time.deltaTime);int index=0;
                // Each cut sprite extends 0.45 cell in front of the moving nozzle.
                float noseDistance=distance+.45f;
                for(int lane=-lanes/2;lane<=lanes/2;lane++)for(int d=-1;d<=1;d+=2)
                {
                    int edge=d<0?negative:positive;float exit=exits[index];var f=active[index++];
                    Vector3 head=p+across*lane*crossPitch+axis*d*Mathf.Min(distance,exit)*pitch;
                    if(f!=null&&f.sprite.enabled)
                    {
                        f.go.transform.position=head+Vector3.back*.6f;
                        float visibleCells=Mathf.Min(Mathf.Min(distance,exit),exhaustLength);
                        f.flame.SetPosition(0,head-axis*d*visibleCells*pitch+Vector3.back*.6f);
                        f.flame.SetPosition(1,head+Vector3.back*.6f);
                        f.flameProperties.SetFloat("_TravelFraction",visibleCells/exhaustLength);f.flame.SetPropertyBlock(f.flameProperties);
                    }
                    for(int sample=sparkStepDone+1;sample<=Mathf.FloorToInt(Mathf.Min(distance,exit)/RocketExhaustSampleCells);sample++)
                        RocketExhaustSparks(p+across*lane*crossPitch+axis*d*(sample*RocketExhaustSampleCells)*pitch,axis*d,pitch,speed,exhaustLength,sample*RocketExhaustSampleCells);
                    if(distance>=exit)
                    {if(f!=null&&f.sprite.enabled){RecordRocketExit(head);RocketImpact(head,true);StartCoroutine(Retire(f));}active[index-1]=null;}
                    for(int step=stepDone+1;step<=Mathf.Min(edge,Mathf.FloorToInt(noseDistance));step++)
                    {
                        int col=origin.col+(horizontal?step*d:lane),row=origin.row+(horizontal?-lane:-step*d);
                        var sq=lm.GetSquare(col,row);
                        if(sq&&!sq.IsNone()&&hit.Add(sq))
                        {if(clearCells)StartOperation(RocketCell(sq));else RocketImpact(sq.transform.position);}
                    }
                }
                stepDone=Mathf.FloorToInt(noseDistance);sparkStepDone=Mathf.FloorToInt(distance/RocketExhaustSampleCells);
                yield return null;
            }
            foreach(var f in active)if(f!=null&&f.sprite.enabled){RecordRocketExit(f.go.transform.position);RocketImpact(f.go.transform.position,true);StartCoroutine(Retire(f));}
            Reaction?.Invoke(.8f);
        }
    }
}
