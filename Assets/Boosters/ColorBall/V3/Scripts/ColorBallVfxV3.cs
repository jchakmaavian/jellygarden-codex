using UnityEngine;
using JellyGarden.Scripts;
namespace RoyalThrone.Boosters.ColorBall.V3
{
    // Visuals expose a contact signal. The director alone decides which cells change.
    public sealed class ColorBallVfxV3 : MonoBehaviour
    {
        public Mesh quad;public Material glow,star,beam;
        public sealed class Flight { internal Renderer outer,inner,head;internal Vector3 from,to;internal float age,duration;internal Color tint;public bool Active {get;internal set;}public bool Arrived {get;internal set;}public int Lease {get;internal set;}public float Progress=>Mathf.Clamp01(age/duration); }
        sealed class Mote {public Renderer view;public bool active;public float age,life,size,angle;public Vector3 position,velocity;public Color tint;}
        const int BeamCapacity=32,MoteCapacity=192;Flight[] flights;Mote[] motes;int cursor;
        static ColorBallVfxV3 instance;MaterialPropertyBlock props;
        void Awake(){props=new MaterialPropertyBlock();}
        readonly System.Random random=new System.Random(72894);
        public static int Peak {get;private set;}
        public static int Active{get{int n=0;if(instance){foreach(var f in instance.flights)if(f.Active)n++;foreach(var m in instance.motes)if(m.active)n++;}return n;}}
        public static int ActiveBeams{get{int n=0;if(instance)foreach(var f in instance.flights)if(f.Active)n++;return n;}}
        public static int PoolSize=>BeamCapacity+MoteCapacity;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset(){instance=null;Peak=0;}
        public static void Prewarm(){if(instance)return;instance=Instantiate(Resources.Load<GameObject>("ColorBall-V3-VFX")).GetComponent<ColorBallVfxV3>();instance.Build();}
        Renderer Part(string name,Material mat,int order){var g=new GameObject(name);g.transform.SetParent(transform,false);g.AddComponent<MeshFilter>().sharedMesh=quad;var r=g.AddComponent<MeshRenderer>();r.sharedMaterial=mat;r.sortingOrder=order;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;r.enabled=false;return r;}
        void Build(){flights=new Flight[BeamCapacity];for(int i=0;i<BeamCapacity;i++)flights[i]=new Flight{outer=Part("Colored moving tail",beam,41),inner=Part("White moving core",beam,42),head=Part("Traveling four point head",star,43)};motes=new Mote[MoteCapacity];for(int i=0;i<MoteCapacity;i++)motes[i]=new Mote{view=Part("Pooled Color Ball accent",star,44)};}
        float Range(float a,float b)=>Mathf.Lerp(a,b,(float)random.NextDouble());
        static Color Tint(int c){switch(c%6){case 0:return new Color(1,.39f,.38f);case 1:return new Color(.38f,.78f,1);case 2:return new Color(1,.82f,.23f);case 3:return new Color(.43f,1,.47f);case 4:return new Color(1,.44f,.92f);default:return new Color(.61f,.51f,1);}}
        public static Flight Launch(Vector3 from,Vector3 to,int color){Prewarm();foreach(var f in instance.flights)if(!f.Active){f.Lease++;f.Active=true;f.Arrived=false;f.age=0;f.from=from+Vector3.back*.9f;f.to=to+Vector3.back*.9f;f.tint=Tint(color);f.duration=Mathf.Clamp(Vector3.Distance(from,to)/ColorBallVisualV3.CellSize/17f,.14f,.37f);instance.Draw(f);return f;}return null;}
        void Paint(Renderer r,Vector3 p,Vector3 size,float angle,Color color){r.gameObject.layer=LevelManager.THIS.GameField.gameObject.layer;r.enabled=true;r.transform.position=p;r.transform.localScale=size;r.transform.rotation=Quaternion.Euler(0,0,angle);props.Clear();props.SetColor("_Color",color);r.SetPropertyBlock(props);}
        void Draw(Flight f){
            float cell=ColorBallVisualV3.CellSize;var delta=f.to-f.from;float distance=delta.magnitude;
            float progress=Mathf.Clamp01(f.age/f.duration);float end=progress*distance;
            float tail=Mathf.Min(distance,cell*1.75f);float start=Mathf.Max(0,end-tail);
            float after=Mathf.Max(0,f.age-f.duration);start=Mathf.Lerp(start,distance,Mathf.Clamp01(after/.12f));
            float length=Mathf.Max(.001f,end-start),fade=1-Mathf.SmoothStep(0,1,after/.14f);
            Vector3 direction=delta.normalized,p=f.from+direction*((start+end)*.5f);float angle=Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg;
            var tint=f.tint;tint.a=.64f*fade;
            Paint(f.outer,p,new Vector3(length,.11f*cell,1),angle,tint);
            Paint(f.inner,p+Vector3.back*.01f,new Vector3(length,.027f*cell,1),angle,new Color(1,.99f,.95f,fade));
            Paint(f.head,f.from+direction*end+Vector3.back*.02f,Vector3.one*(cell*.23f),angle+45,new Color(1,1,.94f,fade));
        }
        public static void Pulse(Vector3 p,float size,float life,Color tint){Prewarm();instance.Emit(p,Vector3.zero,size,life,tint,false);}
        public static void Spark(Vector3 p,float size,float life,Color tint){Prewarm();instance.Emit(p,Vector3.zero,size,life,tint,true);}
        void Emit(Vector3 p,Vector3 velocity,float size,float life,Color tint,bool isStar){
            for(int i=0;i<MoteCapacity;i++){var m=motes[(cursor+i)%MoteCapacity];if(m.active)continue;cursor=(cursor+i+1)%MoteCapacity;m.active=true;m.age=0;m.life=life;m.size=size;m.position=p+Vector3.back*.94f;m.velocity=velocity;m.tint=tint;m.angle=Range(-45,45);m.view.sharedMaterial=isStar?star:glow;Draw(m);return;}
        }
        void Draw(Mote m){float t=Mathf.Clamp01(m.age/m.life),fade=1-Mathf.SmoothStep(0,1,t);var c=m.tint;c.a*=fade;Paint(m.view,m.position+m.velocity*m.age,Vector3.one*(ColorBallVisualV3.CellSize*m.size*Mathf.Lerp(1,.45f,t)),m.angle+t*18,c);}
        public static void Stars(Vector3 p,int count,float radius){Prewarm();float cell=ColorBallVisualV3.CellSize;for(int i=0;i<count;i++){float a=instance.Range(0,Mathf.PI*2);Vector3 dir=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);instance.Emit(p+dir*cell*radius,dir*cell*.22f,instance.Range(.10f,.18f),instance.Range(.18f,.32f),i%3==0?new Color(1,.84f,.34f):new Color(1,1,.90f),true);}}
        public static void Contact(Vector3 p,int color){Spark(p,.19f,.085f,Color.white);}
        public static void TargetFlash(Vector3 p){Pulse(p,.48f,.12f,new Color(1,.95f,.77f,.7f));Spark(p,.43f,.13f,Color.white);}
        public static void Converted(Vector3 p){Spark(p+Vector3.up*ColorBallVisualV3.CellSize*.25f,.26f,.19f,new Color(1,.92f,.56f));}
        void Update(){if(ColorBallVisualV3.Paused)return;foreach(var f in flights)if(f.Active){f.age+=Time.deltaTime;if(f.age>=f.duration)f.Arrived=true;if(f.age>=f.duration+.14f){f.Active=false;f.outer.enabled=f.inner.enabled=f.head.enabled=false;}else Draw(f);}foreach(var m in motes)if(m.active){m.age+=Time.deltaTime;if(m.age>=m.life){m.active=false;m.view.enabled=false;}else Draw(m);}Peak=Mathf.Max(Peak,Active);}
        public static void Clear(){if(!instance||instance.flights==null)return;foreach(var f in instance.flights){f.Active=false;f.Arrived=false;f.outer.enabled=f.inner.enabled=f.head.enabled=false;}foreach(var m in instance.motes){m.active=false;m.view.enabled=false;}}
        void OnDisable(){Clear();}
    }
}
