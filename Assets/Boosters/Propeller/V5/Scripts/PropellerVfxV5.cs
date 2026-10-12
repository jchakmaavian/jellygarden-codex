using UnityEngine;
using JellyGarden.Scripts;
using JellyGarden.Scripts.Juice;

namespace RoyalThrone.Boosters.Propeller.V5
{
    public sealed class PropellerVfxV5 : MonoBehaviour
    {
        public enum Kind { Reveal,Takeoff,Impact }
        public Mesh quad;public Material glow,star,air,pressure;
        public Mesh[] chipMeshes;public Material[] chipMaterials;
        sealed class Particle {public Transform tr;public MeshRenderer view;public MeshFilter filter;public float age,life,spin,growth,gravity;public Vector3 p,v,scale;public Color color;public bool used;}
        const int Capacity=240;Particle[] pool;MaterialPropertyBlock block;static PropellerVfxV5 instance;
        uint seed=61307;public static int Peak {get;private set;}
        public static int Active {get{int n=0;if(instance&&instance.pool!=null)foreach(var p in instance.pool)if(p.used)n++;return n;}}
        public static int PoolSize=>instance?Capacity:0;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void ResetStatics(){instance=null;Peak=0;}
        public static void Prewarm(){if(instance)return;instance=Instantiate(Resources.Load<PropellerVfxV5>("Propeller-V5-VFX"),RoyalPowerupDirector.Get.transform);instance.Build();}
        void Build(){block=new MaterialPropertyBlock();pool=new Particle[Capacity];for(int i=0;i<Capacity;i++){
            var go=new GameObject("Propeller V5 air / star");go.transform.SetParent(transform,false);go.AddComponent<MeshFilter>().sharedMesh=quad;
            var r=go.AddComponent<MeshRenderer>();r.sortingOrder=40;r.sharedMaterial=star;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;
            pool[i]=new Particle{tr=go.transform,view=r,filter=go.GetComponent<MeshFilter>()};r.enabled=false;
        }}
        float R(){seed^=seed<<13;seed^=seed>>17;seed^=seed<<5;return (seed&0xffffff)/(float)0x1000000;}
        static Vector3 Size(float x,float y)=>new Vector3(x,y,1);
        public static void Play(Kind kind,Vector3 p,float cell)
        {
            Prewarm();var s=instance;p.z-=.65f;bool impact=kind==Kind.Impact;
            s.Emit(p,Vector3.zero,Size(cell*(impact?.64f:.48f),cell*(impact?.64f:.40f)),.09f,new Color(1,.96f,.77f,.9f),s.glow,0,.25f);
            s.Emit(p,Vector3.zero,Size(cell*.38f,cell*.38f),impact?.17f:.10f,new Color(1,1,.96f,1),s.star,0,.3f);
            if(impact)s.Emit(p,Vector3.zero,Size(cell*.56f,cell*.56f),.15f,new Color(1,.79f,.12f,.8f),s.star,45,.25f);
            if(impact)s.Emit(p,Vector3.zero,Size(cell*.38f,cell*.27f),.18f,new Color(1,.94f,.68f,.38f),s.air,0,.75f);
            if(kind==Kind.Takeoff)s.Emit(p,Vector3.zero,Size(cell*.65f,cell*.29f),.20f,new Color(1,.98f,.82f,.6f),s.air,0,.9f);
            int stars=kind==Kind.Reveal?4:kind==Kind.Takeoff?2:3;
            for(int i=0;i<stars;i++){
                float angle=(i+s.R()*.35f)*Mathf.PI*2/stars;var d=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle));
                float size=cell*Mathf.Lerp(.07f,.12f,s.R());
                s.Emit(p+d*cell*.14f,d*cell*Mathf.Lerp(.8f,1.6f,s.R()),Size(size,size),Mathf.Lerp(.15f,.24f,s.R()),i%2==0?new Color(1,1,.96f,1):new Color(1,.82f,.22f,1),s.star,s.R()*90,0);
            }
            for(int i=0;i<2;i++){
                float a=s.R()*Mathf.PI*2;var d=new Vector3(Mathf.Cos(a),Mathf.Sin(a));
                s.Emit(p+d*cell*.11f,d*cell*1.9f,Size(cell*.072f,cell*.022f),.16f,new Color(1,.75f,.12f,1),s.pressure,a*Mathf.Rad2Deg,0);
            }
            Peak=Mathf.Max(Peak,Active);
        }
        // Cosmetic endpoints come from gameplay; there is no cell lookup, radius or damage here.
        public static void Pressure(Vector3 origin,Vector3 target,float cell)
        {
            Prewarm();var s=instance;var dir=target-origin;dir.z=0;float distance=dir.magnitude;dir.Normalize();
            var p=origin+dir*distance*.36f;p.z=target.z-1.1f;
            s.Emit(p,dir*cell*1.25f,Size(distance*.52f,cell*.075f),.19f,new Color(1,.95f,.72f,.72f),s.pressure,Mathf.Atan2(dir.y,dir.x)*Mathf.Rad2Deg,0);
            target.z-=1.1f;s.Emit(target,Vector3.zero,Size(cell*.26f,cell*.26f),.12f,new Color(1,1,.96f,1),s.star,0,.1f);
            for(int i=0;i<2;i++){
                var v=new Vector3(-dir.y,dir.x)*(i==0?1:-1)*cell*.5f+dir*cell*.55f;
                s.Emit(target,v,Size(cell*.065f,cell*.065f),.20f,new Color(1,.84f,.28f,.9f),s.star,20+i*30,0);
            }
            Peak=Mathf.Max(Peak,Active);
        }
        public static void FlightAir(Vector3 p,Vector3 tangent,float cell)
        {
            Prewarm();var s=instance;var d=((Vector2)tangent).normalized;p-=new Vector3(d.x,d.y)*cell*.25f;p.z-=.5f;
            s.Emit(p,new Vector3(-d.x,-d.y)*cell*.3f,Size(cell*.22f,cell*.022f),.12f,new Color(1,.97f,.85f,.3f),s.pressure,Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg,0);
            s.Emit(p,Vector3.zero,Size(cell*.055f,cell*.055f),.11f,new Color(1,.9f,.58f,.45f),s.star,20,0);
        }
        public static void Break(SpriteRenderer source,int color)
        {
            if(!source)return;Prewarm();var s=instance;if(color<0||color>=s.chipMaterials.Length)return;
            var lm=LevelManager.THIS;float cell=lm.squareWidth*Mathf.Abs(lm.GameField.lossyScale.x);
            var center=source.bounds.center;center.z-=.3f;
            for(int i=0;i<3;i++){
                float a=(i/3f+s.R()*.14f)*Mathf.PI*2;var d=new Vector3(Mathf.Cos(a),Mathf.Sin(a));
                float size=cell*Mathf.Lerp(.09f,.15f,s.R());
                var particle=s.Emit(center+d*cell*.07f,(d*.8f+Vector3.up*.75f)*cell,Size(size,size),.30f+s.R()*.08f,Color.white,s.chipMaterials[color],s.R()*180,0);
                if(particle==null)break;particle.filter.sharedMesh=s.chipMeshes[color*3+i];particle.gravity=cell*7;particle.spin=(s.R()-.5f)*400;
            }
            Peak=Mathf.Max(Peak,Active);
        }
        Particle Emit(Vector3 p,Vector3 v,Vector3 scale,float life,Color color,Material mat,float rotation,float growth)
        {
            foreach(var part in pool)if(!part.used){part.used=true;part.age=0;part.life=life;part.p=p;part.v=v;part.scale=scale;part.color=color;part.growth=growth;part.spin=(R()-.5f)*70;part.gravity=0;part.filter.sharedMesh=quad;
                part.tr.position=p;part.tr.localScale=scale;part.tr.rotation=Quaternion.Euler(0,0,rotation);part.view.sharedMaterial=mat;part.view.enabled=true;
                block.Clear();block.SetColor("_Color",color);part.view.SetPropertyBlock(block);return part;}return null;
        }
        void Update()
        {
            if(LevelManager.THIS&&LevelManager.THIS.gameStatus==GameState.Pause)return;
            foreach(var p in pool)if(p.used){p.age+=Time.deltaTime;float t=p.age/p.life;
                if(t>=1){p.used=false;p.view.enabled=false;continue;}
                p.tr.position=p.gravity>0?p.p+p.v*p.age-Vector3.up*(p.gravity*.5f*p.age*p.age):p.p+p.v*p.life*(1-Mathf.Pow(1-t,2))*.5f;p.tr.Rotate(0,0,p.spin*Time.deltaTime);
                p.tr.localScale=p.scale*(p.growth>0?1+p.growth*t:1-.65f*t*t);
                var c=p.color;c.a*=Mathf.Pow(1-t,1.2f);block.Clear();block.SetColor("_Color",c);p.view.SetPropertyBlock(block);
            }
        }
        public static void Clear(){if(instance&&instance.pool!=null)foreach(var p in instance.pool){p.used=false;p.view.enabled=false;}}
        void OnDisable(){Clear();}
    }
}
