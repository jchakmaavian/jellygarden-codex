using UnityEngine;
using JellyGarden.Scripts;
using RoyalThrone.ReferenceGameplay;
using UnityEngine.Rendering;
using RoyalThrone.Boosters.ColorBall.V5;
namespace RoyalThrone.Boosters.ColorBall.V4
{
    // Visuals expose a contact signal. The director alone decides which cells change.
    [DefaultExecutionOrder(1200)]
    public sealed class ColorBallVfxV4 : MonoBehaviour
    {
        public Mesh quad;public Material glow,star,beam;
        public bool referenceV5;public Material beamCore;
        public sealed class Flight { internal Renderer outer,inner,head,sourceFlare;internal ReferenceVfxPool.Handle native;internal NativeRay[] nativeRays;internal bool visible;internal float opacity;internal Vector3 from,to,drawnSource;internal float age,duration;internal Color tint;internal ColorBallVisualV4 source;internal int sourceLease;internal ColorBallPresentationSettings settings;public bool Active {get;internal set;}public bool Arrived {get;internal set;}public int Lease {get;internal set;}public System.Action<int> ArrivalNotice {get;set;}public float Progress=>Mathf.Clamp01(age/duration); }
        internal sealed class NativeRay
        {
            public LineRenderer view; public float width; public GradientColorKey[] colors;
            public GradientAlphaKey[] alpha, faded; public Gradient gradient;
        }
        struct NativeContact { public Vector3 position; public float until; }
        NativeContact[] nativeContacts=new NativeContact[BeamCapacity];int nativeContactCursor;
        static LineRenderer[] nativeRayTemplates;
        sealed class Mote {public Renderer view;public bool active,contact;public float age,life,size,angle;public Vector3 position,velocity;public Color tint;public float detail;}
        const int BeamCapacity=32,MoteCapacity=192;Flight[] flights;Mote[] motes;int cursor;
        static ColorBallVfxV4 instance;MaterialPropertyBlock props;
        void Awake(){props=new MaterialPropertyBlock();}
        readonly System.Random random=new System.Random(72894);
        public static int Peak {get;private set;}
        public static int Active{get{int n=0;if(instance){foreach(var f in instance.flights)if(f.visible)n++;foreach(var m in instance.motes)if(m.active)n++;}return n;}}
        public static int ActiveBeams{get{int n=0;if(instance)foreach(var f in instance.flights)if(f.Active)n++;return n;}}
        // Logical ActiveBeams retains its original .14s retirement contract. Residual light
        // has a separate pooled visual lifetime and cannot delay the director/Rocket handoff.
        const float LinkResidue=.46f,BeamSupportCells=.68f;
        public static int VisibleLinks{get{int n=0;if(instance)foreach(var f in instance.flights)if(f.visible&&f.opacity>.12f&&f.Progress>.2f)n++;return n;}}
        public static bool RootsBehindBody{get{if(instance&&instance.referenceV5)foreach(var f in instance.flights){if(f.native!=null&&f.native.Valid){var group=f.native.GameObject.GetComponent<SortingGroup>();if(!group||group.sortingOrder>=35)return false;}else if(f.outer.sortingOrder>=35||f.inner.sortingOrder>=35||f.head.sortingOrder>=35||f.sourceFlare.sortingOrder>=35)return false;}return true;}}
        public static int NativeLinks{get{int count=0;if(instance)foreach(var f in instance.flights)if(f.visible&&f.native!=null&&f.native.Valid)count++;return count;}}
        public static int PoolSize=>BeamCapacity+MoteCapacity;
        public static float AttachmentError{get{float error=0;if(instance)foreach(var f in instance.flights)if(f.visible&&f.source&&f.source.Lease==f.sourceLease){var d=f.drawnSource-f.source.VisibleOrigin;d.z=0;error=Mathf.Max(error,d.magnitude);}return error;}}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset(){instance=null;Peak=0;nativeRayTemplates=null;}
        public static void Prewarm(){if(instance)return;var prefab=!ColorBallVisualV4.UseV4Baseline?Resources.Load<GameObject>("ColorBall-V5-VFX"):null;instance=Instantiate(prefab?prefab:Resources.Load<GameObject>("ColorBall-V4-VFX")).GetComponent<ColorBallVfxV4>();instance.Build();}
        Renderer Part(string name,Material mat,int order){var g=new GameObject(name);g.transform.SetParent(transform,false);g.AddComponent<MeshFilter>().sharedMesh=quad;var r=g.AddComponent<MeshRenderer>();r.sharedMaterial=mat;r.sortingOrder=order;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;r.enabled=false;return r;}
        void Build(){flights=new Flight[BeamCapacity];for(int i=0;i<BeamCapacity;i++)flights[i]=new Flight{outer=Part("Colored moving tail",beam,referenceV5?30:41),inner=Part("White moving core",beamCore?beamCore:beam,referenceV5?31:42),head=Part("Traveling four point head",star,referenceV5?32:43),sourceFlare=referenceV5?Part("Aura discharge",beam,34):null};motes=new Mote[MoteCapacity];for(int i=0;i<MoteCapacity;i++)motes[i]=new Mote{view=Part("Pooled Color Ball accent",star,44)};}
        float Range(float a,float b)=>Mathf.Lerp(a,b,(float)random.NextDouble());
        static Color Tint(int c){switch(c%6){case 0:return new Color(1,.39f,.38f);case 1:return new Color(.38f,.78f,1);case 2:return new Color(1,.82f,.23f);case 3:return new Color(.43f,1,.47f);case 4:return new Color(1,.44f,.92f);default:return new Color(.61f,.51f,1);}}
        public static Flight Launch(Vector3 from,Vector3 to,int color){Prewarm();foreach(var f in instance.flights)if(!f.visible){f.Lease++;f.Active=f.visible=true;f.Arrived=false;f.ArrivalNotice=null;f.age=0;f.from=from+Vector3.back*.9f;f.to=to+Vector3.back*.9f;f.tint=Tint(color);f.source=instance.referenceV5?ColorBallVisualV4.SourceAt(from):null;f.sourceLease=f.source?f.source.Lease:0;f.settings=f.source?f.source.GetComponent<ColorBallV5Presentation>()?.Settings:null;f.duration=Mathf.Clamp(Vector3.Distance(from,to)/ColorBallVisualV4.CellSize/17f,.14f,.37f);instance.NativeBeam(f);instance.Draw(f);return f;}return null;}
        void NativeBeam(Flight flight)
        {
            flight.native=null;flight.nativeRays=null;
            if(!ReferenceVfxPool.Has("LightballRayPrefab"))return;
            if(nativeRayTemplates==null){var prefab=Resources.Load<GameObject>("ReferenceGameplay/VFX/LightballRayPrefab");nativeRayTemplates=prefab.GetComponentsInChildren<LineRenderer>(true);}
            if(nativeRayTemplates.Length==0)return;
            var handle=ReferenceVfxPool.Play("LightballRayPrefab",flight.from,-1,ColorBallVisualV4.CellSize,true);
            if(handle==null||!handle.Valid)return;
            flight.native=handle;
            var root=handle.GameObject;
            if(LevelManager.THIS&&LevelManager.THIS.GameField)foreach(var node in root.GetComponentsInChildren<Transform>(true))node.gameObject.layer=LevelManager.THIS.GameField.gameObject.layer;
            var sorting=root.GetComponent<SortingGroup>();if(sorting)sorting.sortingOrder=30;
            var lines=root.GetComponentsInChildren<LineRenderer>(true);
            var rays=new System.Collections.Generic.List<NativeRay>();
            foreach(var line in lines)
            {
                LineRenderer template=null;foreach(var original in nativeRayTemplates)if(original.name==line.name){template=original;break;}
                if(!template){line.enabled=false;continue;}
                // Read unmodified prefab properties each lease, never last frame's faded values.
                var originalGradient=template.colorGradient;
                var ray=new NativeRay{view=line,width=template.widthMultiplier,colors=originalGradient.colorKeys,alpha=originalGradient.alphaKeys,
                    gradient=new Gradient{mode=originalGradient.mode}};
                ray.faded=new GradientAlphaKey[ray.alpha.Length];ray.gradient.SetKeys(ray.colors,ray.alpha);
                line.colorGradient=ray.gradient;line.widthCurve=template.widthCurve;line.widthMultiplier=ray.width*ColorBallVisualV4.CellSize;
                line.positionCount=2;line.enabled=true;line.gameObject.SetActive(true);rays.Add(ray);
            }
            flight.nativeRays=rays.ToArray();
            flight.outer.enabled=flight.inner.enabled=flight.head.enabled=false;if(flight.sourceFlare)flight.sourceFlare.enabled=false;
        }
        void DrawNative(Flight flight,Vector3 end,float cell,float fade)
        {
            if(flight.native==null||!flight.native.Valid||flight.nativeRays==null)return;
            foreach(var ray in flight.nativeRays)
            {
                if(!ray.view)continue;
                ray.view.widthMultiplier=ray.width*cell;
                ray.view.SetPosition(0,ray.view.useWorldSpace?flight.from:ray.view.transform.InverseTransformPoint(flight.from));
                ray.view.SetPosition(1,ray.view.useWorldSpace?end:ray.view.transform.InverseTransformPoint(end));
                for(int i=0;i<ray.alpha.Length;i++)ray.faded[i]=new GradientAlphaKey(ray.alpha[i].alpha*fade,ray.alpha[i].time);
                ray.gradient.SetKeys(ray.colors,ray.faded);ray.view.colorGradient=ray.gradient;
            }
        }
        bool NativeContactAt(Vector3 position)
        {
            foreach(var contact in nativeContacts)if(contact.until>0&&contact.until>=Time.time&&(contact.position-position).sqrMagnitude<.0001f)return true;
            return false;
        }
        void Paint(Renderer r,Vector3 p,Vector3 size,float angle,Color color){r.gameObject.layer=LevelManager.THIS.GameField.gameObject.layer;r.enabled=true;r.transform.position=p;r.transform.localScale=size;r.transform.rotation=Quaternion.Euler(0,0,angle);props.Clear();props.SetColor("_Color",color);r.SetPropertyBlock(props);}
        void Draw(Flight f){
            // Follow only the animated source endpoint. Arrival duration and target never change.
            if(f.source&&f.source.Lease==f.sourceLease&&f.source.Current!=ColorBallVisualV4.Phase.Free){var origin=f.source.VisibleOrigin;f.from.x=origin.x;f.from.y=origin.y;}
            f.drawnSource=f.from;
            float cell=ColorBallVisualV4.CellSize;var delta=f.to-f.from;float distance=delta.magnitude;
            float progress=Mathf.Clamp01(f.age/f.duration);float end=progress*distance;
            float tail=Mathf.Min(distance,cell*1.75f);float start=Mathf.Max(0,end-tail);
            float after=Mathf.Max(0,f.age-f.duration);start=referenceV5?0:Mathf.Lerp(start,distance,Mathf.Clamp01(after/.12f));
            float length=Mathf.Max(.001f,end-start);
            // Recent links stay warm while subsequent real targets launch. New travel is strongest.
            float fade=referenceV5?(after<=0?1:Mathf.Lerp(.76f,0,Mathf.SmoothStep(0,1,after/LinkResidue))):1-Mathf.SmoothStep(0,1,after/.14f);
            if(referenceV5&&f.source&&f.source.Current==ColorBallVisualV4.Phase.Finish)fade*=1-Mathf.SmoothStep(0,1,f.source.Age/.10f);
            f.opacity=fade;
            if(f.native!=null){DrawNative(f,Vector3.Lerp(f.from,f.to,progress),cell,fade);return;}
            Vector3 direction=delta.normalized,p=f.from+direction*((start+end)*.5f);float angle=Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg;
            var tint=referenceV5?new Color(1,.85f,.44f):f.tint;tint.a=(referenceV5?.94f:.82f)*fade;
            Paint(f.outer,p,new Vector3(length,(referenceV5?BeamSupportCells:.225f)*cell,1),angle,tint);
            Paint(f.inner,p+Vector3.back*.01f,new Vector3(length,(referenceV5?BeamSupportCells:.095f)*cell,1),angle,new Color(1,.99f,.95f,fade));
            Paint(f.head,f.from+direction*end+Vector3.back*.02f,referenceV5?new Vector3(.42f,.25f,1)*cell:Vector3.one*(cell*.30f),referenceV5?angle:angle+45,new Color(1,.98f,.82f,fade));
            if(referenceV5){
                f.head.GetPropertyBlock(props);props.SetFloat("_Leading",1);props.SetColor("_Color",new Color(1,.98f,.82f,fade*(1-Mathf.SmoothStep(0,1,after/.065f))));f.head.SetPropertyBlock(props);
                // Position stays relative to the animated source, at its rim in the shot direction.
                // The solid body conceals the beam root; this warm shoulder blends into its halo.
                float pulse=Mathf.SmoothStep(0,1,f.age/.022f)*(1-Mathf.SmoothStep(0,1,f.age/.16f));
                Paint(f.sourceFlare,f.from+direction*cell*.46f,new Vector3(.70f,.39f,1)*cell,angle,new Color(1,.91f,.61f,pulse*.72f));
                f.sourceFlare.GetPropertyBlock(props);props.SetFloat("_SourceFlare",1);f.sourceFlare.SetPropertyBlock(props);
            }
            if(referenceV5)for(int layer=0;layer<2;layer++){var r=layer==0?f.outer:f.inner;r.GetPropertyBlock(props);props.SetFloat("_Phase",f.age);props.SetFloat("_Travel",progress);props.SetFloat("_After",after);props.SetFloat("_LengthCells",distance/cell);props.SetFloat("_Detail",f.settings?f.settings.detail:0);r.SetPropertyBlock(props);}
        }
        public static void Pulse(Vector3 p,float size,float life,Color tint){Prewarm();instance.Emit(p,Vector3.zero,size,life,tint,false);}
        public static void Spark(Vector3 p,float size,float life,Color tint){Prewarm();instance.Emit(p,Vector3.zero,size,life,tint,true);}
        void Emit(Vector3 p,Vector3 velocity,float size,float life,Color tint,bool isStar,bool contact=false){
            for(int i=0;i<MoteCapacity;i++){var m=motes[(cursor+i)%MoteCapacity];if(m.active)continue;cursor=(cursor+i+1)%MoteCapacity;m.active=true;m.contact=contact;m.age=0;m.life=life;m.size=size;m.position=p+Vector3.back*.94f;m.velocity=velocity;m.tint=tint;m.angle=Range(-45,45);m.detail=0;if(contact){foreach(var f in flights)if(f.Active&&(f.to-p-Vector3.back*.9f).sqrMagnitude<.01f){m.angle=Mathf.Atan2(f.to.y-f.from.y,f.to.x-f.from.x)*Mathf.Rad2Deg;m.detail=f.settings?f.settings.detail:0;}}m.view.sharedMaterial=contact?beam:isStar?star:glow;Draw(m);return;}
        }
        void Draw(Mote m){float t=Mathf.Clamp01(m.age/m.life),fade=1-Mathf.SmoothStep(0,1,m.contact?(m.age-.055f)/(m.life-.055f):t);var c=m.tint;c.a*=fade;Paint(m.view,m.position+m.velocity*m.age,Vector3.one*(ColorBallVisualV4.CellSize*m.size*(m.contact?1:Mathf.Lerp(1,.45f,t))),m.angle+t*18,c);if(m.contact){m.view.GetPropertyBlock(props);props.SetFloat("_Contact",1);props.SetFloat("_ContactAge",m.age);props.SetFloat("_Detail",m.detail);m.view.SetPropertyBlock(props);}}
        public static void Stars(Vector3 p,int count,float radius){Prewarm();float cell=ColorBallVisualV4.CellSize;for(int i=0;i<count;i++){float a=instance.Range(0,Mathf.PI*2);Vector3 dir=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);instance.Emit(p+dir*cell*radius,dir*cell*.22f,instance.Range(.10f,.18f),instance.Range(.18f,.32f),i%3==0?new Color(1,.84f,.34f):new Color(1,1,.90f),true);}}
        // shaped: an ordinary gem that ColorBallChargedTargetV4 will hold. It flashes and glows in its own shape, so the
        // imported contact's round white burst and ring are left out (2026-10-04, the user: not a solid white blob).
        public static void Contact(Vector3 p,int color,bool shaped=false){
            Prewarm();
            var native=ReferenceVfxPool.Play("LightballEffectedItemExplodeParticles",p,color,ColorBallVisualV4.CellSize);
            if(native!=null&&native.Valid){
                if(shaped)foreach(var ps in native.GameObject.GetComponentsInChildren<ParticleSystem>())if(ps.name=="bang_blast"||ps.name=="ring")ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
                instance.nativeContacts[instance.nativeContactCursor++%BeamCapacity]=new NativeContact{position=p,until=Time.time+.5f};return;}
            if(!instance.referenceV5){Spark(p,.19f,.085f,Color.white);return;}
            Pulse(p,.38f,.085f,new Color(1,1,.94f,.85f));Spark(p,.21f,.12f,new Color(1,1,.91f));
            // Color remains a local visual parameter; authoritative target conversion stays in the director.
            instance.Emit(p,Vector3.zero,.85f,.22f,new Color(1,.86f,.44f,.95f),false,true);
        }
        public static void TargetFlash(Vector3 p){if(instance&&instance.NativeContactAt(p))return;Pulse(p,.48f,.12f,new Color(1,.95f,.77f,.7f));Spark(p,instance&&instance.referenceV5?.24f:.43f,.13f,Color.white);}
        public static void Converted(Vector3 p){if(instance&&instance.NativeContactAt(p))return;Spark(p+Vector3.up*ColorBallVisualV4.CellSize*.25f,.26f,.19f,new Color(1,.92f,.56f));}
        void Retire(Flight f){f.ArrivalNotice=null;if(f.native!=null&&f.native.Valid&&f.nativeRays!=null)foreach(var ray in f.nativeRays)if(ray.view){ray.view.enabled=false;ray.view.widthMultiplier=ray.width;ray.gradient.SetKeys(ray.colors,ray.alpha);ray.view.colorGradient=ray.gradient;}f.native?.Stop();f.native=null;f.nativeRays=null;f.visible=f.Active=false;f.source=null;f.opacity=0;f.outer.enabled=f.inner.enabled=f.head.enabled=false;if(f.sourceFlare)f.sourceFlare.enabled=false;}
        void Update(){if(ColorBallVisualV4.Paused||flights==null)return;foreach(var f in flights)if(f.visible){
            f.age+=Time.deltaTime;if(!f.Arrived&&f.age>=f.duration){
                f.Arrived=true;var notice=f.ArrivalNotice;f.ArrivalNotice=null;
                // Latch contact before this visual can retire/reuse after a long frame.
                // The gameplay coroutine still owns all hits and conversions.
                notice?.Invoke(f.Lease);
            }
            if(f.age>=f.duration+.14f)f.Active=false;
            bool sourceGone=referenceV5&&f.source&&(f.source.Lease!=f.sourceLease||f.source.Current==ColorBallVisualV4.Phase.Free);
            if(sourceGone||f.age>=f.duration+(referenceV5?LinkResidue:.14f))Retire(f);else if(!referenceV5)Draw(f);
        }foreach(var m in motes)if(m.active){m.age+=Time.deltaTime;if(m.age>=m.life){m.active=false;m.view.enabled=false;}else Draw(m);}Peak=Mathf.Max(Peak,Active);}
        // Draw after the source's local pose; prevents a one-frame detached origin at 30 FPS.
        void LateUpdate(){if(referenceV5&&flights!=null&&!ColorBallVisualV4.Paused)foreach(var f in flights)if(f.visible)Draw(f);}
        public static void Clear(){if(!instance||instance.flights==null)return;foreach(var f in instance.flights){instance.Retire(f);f.Arrived=false;}foreach(var m in instance.motes){m.active=false;m.view.enabled=false;}System.Array.Clear(instance.nativeContacts,0,instance.nativeContacts.Length);}
        void OnDisable(){Clear();}
    }
}
