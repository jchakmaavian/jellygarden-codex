using System.Collections.Generic;
using UnityEngine;

namespace RoyalThrone.Presentation.AreaMotion
{
    // Real Unity particle systems using the recovered modules, atlas frames and transforms.
    public sealed class RoyalAreaParticles
    {
        readonly EffectLibrary library;
        readonly Dictionary<string,Sprite> sprites=new Dictionary<string,Sprite>();
        readonly Dictionary<string,EffectData> effects=new Dictionary<string,EffectData>();
        readonly List<Material> materials=new List<Material>();
        readonly List<GameObject> transients=new List<GameObject>();
        readonly List<float> expiry=new List<float>();
        readonly List<ParticleSystem> systems=new List<ParticleSystem>();
        readonly Transform root;
        readonly Material alpha,additive;
        public GameObject Fountain {get;private set;}
        public int ActiveParticleCount {get {int n=0;foreach(var p in systems)if(p&&p.gameObject.activeInHierarchy)n+=p.particleCount;return n;}}
        public RoyalAreaParticles(Transform root)
        {
            this.root=root;
            library=JsonUtility.FromJson<EffectLibrary>(Resources.Load<TextAsset>(RoyalAreaMotionPlayer.Folder+"effects").text);
            var atlas=Resources.Load<Texture2D>(RoyalAreaMotionPlayer.Folder+"VFX/particles");
            foreach(var s in library.sprites)sprites[s.id]=Sprite.Create(atlas,new Rect(s.x,s.y,s.w,s.h),new Vector2(.5f,.5f),s.ppu);
            foreach(var e in library.effects)effects[e.name]=e;
            var shader=Resources.Load<Shader>(RoyalAreaMotionPlayer.Folder+"Shaders/RoyalAreaLayer");
            alpha=new Material(shader){name="Area particles alpha"};alpha.mainTexture=atlas;
            additive=new Material(alpha){name="Area particles additive"};additive.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.One);
            materials.Add(alpha);materials.Add(additive);
        }
        public ParticleSystem Attach(Transform parent,ParticleData d,bool strongFountain=false)
        {
            var go=new GameObject("Native area particles");go.layer=31;go.transform.SetParent(parent,false);
            var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;main.playOnAwake=false;main.duration=Mathf.Max(.01f,d.duration);main.loop=d.loop;
            main.useUnscaledTime=true;main.maxParticles=strongFountain?256:160;main.cullingMode=ParticleSystemCullingMode.AlwaysSimulate;
            main.simulationSpace=ParticleSystemSimulationSpace.World;main.scalingMode=ParticleSystemScalingMode.Hierarchy;
            main.startDelay=MotionCurves.Particle(d.delay);main.startLifetime=MotionCurves.Particle(d.lifetime);
            // Emitters were authored at 0.37 scene scale. World simulation needs that scale on jet speed too.
            main.startSpeed=MotionCurves.Particle(d.speed,strongFountain?1.22f*Mathf.Abs(parent.lossyScale.x):1);
            main.startSize=MotionCurves.Particle(d.size,strongFountain?1.08f:1);
            main.startRotation=MotionCurves.Particle(d.rotation);main.gravityModifier=MotionCurves.Particle(d.gravity);
            Color high=d.startColor,low=d.startColorMin;
            if(strongFountain){string name=parent.name.ToLowerInvariant();float opacity=name.Contains("drop")||name.Contains("rain")?.65f:name.Contains("water")?.45f:.22f;high.a*=opacity;low.a*=opacity;}
            main.startColor=d.startColorMode==2?new ParticleSystem.MinMaxGradient(low,high):new ParticleSystem.MinMaxGradient(high);
            var emission=ps.emission;emission.enabled=true;emission.rateOverTime=MotionCurves.Particle(d.rate,strongFountain?1.75f:1);
            if(d.bursts!=null){var bursts=new ParticleSystem.Burst[d.bursts.Length];for(int i=0;i<bursts.Length;i++)bursts[i]=new ParticleSystem.Burst(d.bursts[i].time,MotionCurves.Particle(d.bursts[i].count));emission.SetBursts(bursts);}
            var shape=ps.shape;shape.enabled=d.shapeEnabled;shape.shapeType=(ParticleSystemShapeType)Mathf.Clamp(d.shape,0,21);
            // The APK's mesh-based shapes need source meshes not used by this sprite preview. Use the native radius as a disk.
            if(d.shape==6||d.shape==13||d.shape==14||d.shape==19||d.shape==20)shape.shapeType=ParticleSystemShapeType.Circle;
            shape.radius=Mathf.Max(0,d.radius);shape.angle=d.angle;shape.radiusThickness=Mathf.Clamp01(d.radiusThickness);
            shape.position=d.shapePosition;shape.rotation=d.shapeRotation;shape.scale=d.shapeScale==Vector3.zero?Vector3.one:d.shapeScale;
            var size=ps.sizeOverLifetime;size.enabled=d.sizeEnabled;if(d.sizeEnabled)size.size=MotionCurves.Particle(d.sizeCurve);
            var rotation=ps.rotationOverLifetime;rotation.enabled=d.rotationEnabled;if(d.rotationEnabled)rotation.z=MotionCurves.Particle(d.rotationCurve);
            var velocity=ps.velocityOverLifetime;velocity.enabled=d.velocityEnabled;
            if(d.velocityEnabled){velocity.space=d.velocityWorld?ParticleSystemSimulationSpace.World:ParticleSystemSimulationSpace.Local;velocity.x=MotionCurves.Particle(d.vx);velocity.y=MotionCurves.Particle(d.vy);velocity.z=MotionCurves.Particle(d.vz);}
            var color=ps.colorOverLifetime;color.enabled=d.colorEnabled;
            if(d.colorEnabled&&d.color!=null){var g=new Gradient();var ck=new GradientColorKey[d.color.colors.Length];var ak=new GradientAlphaKey[d.color.alphas.Length];for(int i=0;i<ck.Length;i++)ck[i]=new GradientColorKey(d.color.colors[i].c,d.color.colors[i].t);for(int i=0;i<ak.Length;i++)ak[i]=new GradientAlphaKey(d.color.alphas[i].a,d.color.alphas[i].t);if(ck.Length>0&&ak.Length>0){g.SetKeys(ck,ak);color.color=g;}}
            var sheet=ps.textureSheetAnimation;int count=0;
            if(d.sprites!=null)foreach(string id in d.sprites)if(sprites.ContainsKey(id))count++;
            if(count>0){sheet.enabled=true;sheet.mode=ParticleSystemAnimationMode.Sprites;foreach(string id in d.sprites)if(sprites.TryGetValue(id,out var s))sheet.AddSprite(s);sheet.frameOverTime=new ParticleSystem.MinMaxCurve(0);sheet.startFrame=new ParticleSystem.MinMaxCurve(0,Mathf.Max(0,count-.01f));}
            var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=d.additive?additive:alpha;
            renderer.renderMode=d.renderMode==1?ParticleSystemRenderMode.Stretch:ParticleSystemRenderMode.Billboard;
            // The source's foreground FX layer sits above area artwork even when its local order is zero.
            renderer.lengthScale=d.lengthScale;renderer.velocityScale=d.velocityScale;renderer.sortingOrder=d.sortingLayer>0?Mathf.Clamp(28000+d.order/4,-32700,32700):RoyalAreaMotionPlayer.SortingOrder(d.order);
            renderer.alignment=ParticleSystemRenderSpace.View;renderer.maxParticleSize=.2f;
            if(count==0)renderer.enabled=false;
            systems.Add(ps);
            return ps;
        }
        GameObject InstantiateEffect(string name,bool enhanced)
        {
            if(!effects.TryGetValue(name,out var e))return null;
            var holder=new GameObject(name);holder.layer=31;holder.transform.SetParent(root,false);
            var transforms=new Transform[e.nodes.Length];
            for(int i=0;i<e.nodes.Length;i++){
                var d=e.nodes[i];var go=new GameObject(d.name);go.layer=31;transforms[i]=go.transform;
                go.transform.SetParent(d.parent>=0?transforms[d.parent]:holder.transform,false);go.transform.localPosition=d.position;go.transform.localRotation=d.rotation;go.transform.localScale=d.scale;
                if(d.data!=null){var ps=Attach(go.transform,d.data,enhanced);if(!enhanced){var r=ps.GetComponent<ParticleSystemRenderer>();r.sortingOrder=30000;}}
                go.SetActive(d.active||i==0);
            }
            return holder;
        }
        public void SetFountain(bool visible)
        {
            if(visible&&!Fountain)Fountain=InstantiateEffect("FountainParticles",true);
            if(!Fountain)return;
            bool was=Fountain.activeSelf;Fountain.SetActive(visible);
            if(visible&&(!was||Fountain.GetComponentsInChildren<ParticleSystem>().Length>0))foreach(var p in Fountain.GetComponentsInChildren<ParticleSystem>())if(!p.isPlaying)p.Play(false);
        }
        public void Play(string name,float clock)
        {
            var go=InstantiateEffect(name,false);if(!go)return;
            foreach(var p in go.GetComponentsInChildren<ParticleSystem>()){var m=p.main;m.loop=false;p.Play(false);}
            transients.Add(go);expiry.Add(clock+7);
        }
        public void Tick(float clock,bool motion)
        {
            for(int i=transients.Count-1;i>=0;i--)if(!transients[i]||clock>expiry[i]){if(transients[i])Object.Destroy(transients[i]);transients.RemoveAt(i);expiry.RemoveAt(i);}
            for(int i=systems.Count-1;i>=0;i--){var p=systems[i];if(!p){systems.RemoveAt(i);continue;}if(!p.gameObject.activeInHierarchy)continue;
                if(!motion&&p.isPlaying)p.Pause(false);
                else if(motion&&p.isPaused)p.Play(false);
                else if(motion&&p.main.loop&&!p.isPlaying)p.Play(false);
            }
        }
        public void Dispose(){foreach(var m in materials)if(m)Object.Destroy(m);foreach(var s in sprites.Values)if(s)Object.Destroy(s);}
    }
}
