using UnityEngine;
using JellyGarden.Scripts;
using JellyGarden.Scripts.Juice;

namespace RoyalThrone.Boosters.Propeller.V2
{
    // Fixed, independent pool. Quads are analytic four-point stars / soft lights / thin air rings.
    // No textures, materials, timing or particles from TNT are used here.
    public sealed class PropellerVfxV2 : MonoBehaviour
    {
        public enum Kind { Reveal,Takeoff,Neighbor,Impact }
        public Mesh quad;public Material glow,star,air;
        sealed class Particle {public Transform tr;public MeshRenderer view;public float age,life,size,spin,growth;public Vector3 p,v;public Color color;public bool used;}
        const int Capacity=320;Particle[] pool;MaterialPropertyBlock block;static PropellerVfxV2 instance;
        uint seed=482019;public static int Peak {get;private set;}
        public static int Active {get{int n=0;if(instance&&instance.pool!=null)foreach(var p in instance.pool)if(p.used)n++;return n;}}
        public static int PoolSize=>instance?Capacity:0;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void ResetStatics(){instance=null;Peak=0;}
        public static void Prewarm(){if(instance)return;instance=Instantiate(Resources.Load<PropellerVfxV2>("Propeller-V2-VFX"),RoyalPowerupDirector.Get.transform);instance.name="Propeller-V2 air and stars pool";instance.Build();}
        void Build(){block=new MaterialPropertyBlock();pool=new Particle[Capacity];for(int i=0;i<Capacity;i++){
            var go=new GameObject("Air / star");go.transform.SetParent(transform,false);go.AddComponent<MeshFilter>().sharedMesh=quad;
            var r=go.AddComponent<MeshRenderer>();r.sortingOrder=40;r.sharedMaterial=star;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;
            pool[i]=new Particle{tr=go.transform,view=r};r.enabled=false;
        }}
        float R(){seed^=seed<<13;seed^=seed>>17;seed^=seed<<5;return (seed&0xffffff)/(float)0x1000000;}
        public static void Play(Kind kind,Vector3 pos,float cell)
        {
            Prewarm();var s=instance;pos.z-=1.15f;
            bool small=kind==Kind.Neighbor;
            s.Emit(pos,Vector3.zero,cell*(small?.38f:kind==Kind.Takeoff?.62f:.80f),small?.10f:.16f,new Color(1,.91f,.50f,.9f),s.glow,0,.6f);
            s.Emit(pos,Vector3.zero,cell*(small?.18f:.34f),.10f,new Color(1,1,.94f,1),s.star,45,.4f);
            if(kind==Kind.Takeoff)s.Emit(pos,Vector3.zero,cell*.58f,.23f,new Color(1,.96f,.75f,.55f),s.air,0,1.3f);
            int count=small?4:kind==Kind.Impact?11:10;
            for(int i=0;i<count;i++){
                float a=(i+s.R()*.6f)*Mathf.PI*2/count;
                var d=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);
                float size=cell*Mathf.Lerp(.055f,.14f,s.R())*(small?.65f:1);
                s.Emit(pos+d*cell*.10f,d*cell*Mathf.Lerp(.55f,1.8f,s.R())*(small?.55f:1),size,Mathf.Lerp(.20f,.38f,s.R()),i%3==0?new Color(1,.72f,.12f,.95f):new Color(1,1,.91f,1),s.star,s.R()*180,0);
            }
            Peak=Mathf.Max(Peak,Active);
        }
        void Emit(Vector3 p,Vector3 v,float size,float life,Color color,Material mat,float rotation,float growth)
        {
            foreach(var part in pool)if(!part.used){part.used=true;part.age=0;part.life=life;part.p=p;part.v=v;part.size=size;part.color=color;part.growth=growth;part.spin=(R()-.5f)*100;
                part.tr.position=p;part.tr.localScale=Vector3.one*size;part.tr.rotation=Quaternion.Euler(0,0,rotation);part.view.sharedMaterial=mat;part.view.enabled=true;
                block.Clear();block.SetColor("_Color",color);part.view.SetPropertyBlock(block);return;}
        }
        void Update()
        {
            if(LevelManager.THIS&&LevelManager.THIS.gameStatus==GameState.Pause)return;
            foreach(var p in pool)if(p.used){p.age+=Time.deltaTime;float t=p.age/p.life;
                if(t>=1){p.used=false;p.view.enabled=false;continue;}
                p.tr.position=p.p+p.v*(p.life*(1-Mathf.Pow(1-t,2))*.5f);
                p.tr.Rotate(0,0,p.spin*Time.deltaTime);
                float scale=p.growth>0?1+p.growth*t:1-.75f*t*t;
                p.tr.localScale=Vector3.one*(p.size*scale);
                var c=p.color;c.a*=Mathf.Pow(1-t,1.45f);block.Clear();block.SetColor("_Color",c);p.view.SetPropertyBlock(block);
            }
        }
        public static void Clear(){if(instance&&instance.pool!=null)foreach(var p in instance.pool){p.used=false;p.view.enabled=false;}}
        void OnDisable(){if(pool!=null)foreach(var p in pool){p.used=false;p.view.enabled=false;}}
    }
}
