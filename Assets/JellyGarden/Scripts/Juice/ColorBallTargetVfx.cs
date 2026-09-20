using UnityEngine;

namespace JellyGarden.Scripts.Juice
{
    // Decorative only: target selection, arrival and conversion stay in the director.
    // Fixed pools; no per-shot objects/materials or changes to gameplay's random state.
    public sealed class ColorBallTargetVfx : MonoBehaviour
    {
        public enum Kind { Beam, TargetGlow, TargetImpact }
        public const float ArrivalTime=.12f;
        const int BeamCapacity=16, ParticleCapacity=192, Samples=18;
        static ColorBallTargetVfx instance;
        static readonly Color Gold=new Color(1,.72f,.16f), Warm=new Color(1,.96f,.76f);
        sealed class Beam
        {
            public LineRenderer glow,core;
            public SpriteRenderer tip;
            public Vector3 from,to;
            public float age,bend;
            public bool active;
        }
        sealed class Mote
        {
            public SpriteRenderer view;
            public Vector3 start,velocity;
            public Color tint;
            public float age,life,size,growth,spin;
            public bool active;
            public bool outline;
            public Vector3 fixedScale;
        }
        readonly Beam[] beams=new Beam[BeamCapacity];
        readonly Mote[] motes=new Mote[ParticleCapacity];
        readonly global::System.Random random=new global::System.Random(38147);
        Sprite star,soft,ring;
        Material material,haloMaterial,outlineMaterial;
        int nextMote;
        public static bool Ready=>instance;
        public static int PoolSize=>instance?BeamCapacity+ParticleCapacity:0;
        public static int ActiveBeams {get {int n=0;if(instance)foreach(var b in instance.beams)if(b.active)n++;return n;}}
        public static int Active
        {
            get {int n=0;if(instance){foreach(var b in instance.beams)if(b.active)n++;foreach(var m in instance.motes)if(m.active)n++;}return n;}
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics(){instance=null;}
        float Range(float a,float b)=>a+(float)random.NextDouble()*(b-a);
        public static void Prewarm()
        {
            if(instance)return;
            instance=new GameObject("GameplayVFX - Color Ball energy pool (unmasked)").AddComponent<ColorBallTargetVfx>();
            instance.Build();
        }
        void Build()
        {
            material=new Material(Shader.Find("Sprites/Default")){name="Color Ball pooled light"};
            star=Texture(0);soft=Texture(1);ring=Texture(2);
            haloMaterial=new Material(material){name="Color Ball soft beam halo",mainTexture=soft.texture};
            outlineMaterial=new Material(Resources.Load<Shader>("RoyalColorBall/ColorBallOutline")){name="Color Ball gem contour"};
            for(int i=0;i<BeamCapacity;i++)
            {
                var tip=new GameObject("Moving beam star").AddComponent<SpriteRenderer>();
                tip.transform.SetParent(transform,false);tip.sharedMaterial=material;tip.sprite=star;
                tip.sortingOrder=43;tip.maskInteraction=SpriteMaskInteraction.None;tip.enabled=false;
                beams[i]=new Beam{glow=Line("Gold beam halo",39,.14f),core=Line("White beam core",41,.026f),tip=tip};
            }
            for(int i=0;i<ParticleCapacity;i++)
            {
                var r=new GameObject("Reusable Color Ball light").AddComponent<SpriteRenderer>();
                r.transform.SetParent(transform,false);r.sharedMaterial=material;
                r.maskInteraction=SpriteMaskInteraction.None;r.enabled=false;
                motes[i]=new Mote{view=r};
            }
        }
        // Analytic masks generated once; approved animation frames remain untouched.
        Sprite Texture(int type)
        {
            const int size=64;
            var tex=new Texture2D(size,size,TextureFormat.RGBA32,false){name="Color Ball light mask "+type,wrapMode=TextureWrapMode.Clamp};
            var pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float u=(x+.5f)/size*2-1,v=(y+.5f)/size*2-1,r=Mathf.Sqrt(u*u+v*v),a;
                if(type==0)
                {
                    float cross=Mathf.Pow(Mathf.Clamp01(1-Mathf.Pow(Mathf.Abs(u),.65f)-Mathf.Pow(Mathf.Abs(v),.65f)),.5f);
                    a=Mathf.Max(cross,Mathf.Exp(-r*r*70)*.75f);
                }
                else if(type==1)a=Mathf.Exp(-r*r*5)*Mathf.Clamp01((1-r)*5);
                else a=Mathf.Exp(-Mathf.Pow((r-.72f)/.045f,2))*Mathf.Clamp01((1-r)*5);
                pixels[y*size+x]=new Color(1,1,1,a);
            }
            tex.SetPixels(pixels);tex.Apply(false,true);
            return Sprite.Create(tex,new Rect(0,0,size,size),new Vector2(.5f,.5f),size);
        }
        LineRenderer Line(string name,int order,float width)
        {
            var r=new GameObject(name).AddComponent<LineRenderer>();r.transform.SetParent(transform,false);
            r.sharedMaterial=order==39?haloMaterial:material;r.useWorldSpace=true;r.positionCount=Samples;r.sortingOrder=order;
            r.numCapVertices=4;r.numCornerVertices=2;r.widthMultiplier=width;r.enabled=false;
            r.widthCurve=new AnimationCurve(new Keyframe(0,.1f),new Keyframe(.12f,1),new Keyframe(.85f,.8f),new Keyframe(1,.1f));
            return r;
        }
        static int BoardLayer=>LevelManager.THIS&&LevelManager.THIS.GameField?LevelManager.THIS.GameField.gameObject.layer:0;
        public static void Play(Kind kind,Vector3 position,Vector3 target,SpriteRenderer targetSprite=null)
        {
            Prewarm();float cell=ColorBallFrameAnimator.Cell;
            if(kind==Kind.Beam)
            {
                foreach(var b in instance.beams)if(!b.active)
                {
                    b.active=true;b.age=0;b.from=position+Vector3.back*.32f;b.to=target+Vector3.back*.32f;
                    b.bend=0;
                    b.glow.gameObject.layer=b.core.gameObject.layer=BoardLayer;
                    b.glow.sortingLayerID=b.core.sortingLayerID=ColorBallFrameAnimator.SeatedSortingLayer;
                    b.glow.enabled=b.core.enabled=true;instance.Draw(b);break;
                }
                instance.Emit(true,position,Vector3.zero,.19f,.35f,0,0,Warm,42);
            }
            else if(kind==Kind.TargetGlow)
            {
                if(targetSprite&&targetSprite.sprite)
                {
                    var outline=instance.Emit(false,targetSprite.transform.position,Vector3.zero,.20f,1,0,0,new Color(1,.88f,.42f,1),42);
                    if(outline!=null)
                    {
                        outline.outline=true;outline.fixedScale=targetSprite.transform.lossyScale;
                        outline.view.sprite=targetSprite.sprite;outline.view.sharedMaterial=instance.outlineMaterial;
                        outline.view.transform.rotation=targetSprite.transform.rotation;
                        outline.view.transform.localScale=outline.fixedScale;
                    }
                }
            }
            else
            {
                instance.Emit(false,position,Vector3.zero,.10f,.46f,-.12f,0,new Color(1,1,.94f,1),42);
                instance.Emit(true,position,Vector3.zero,.18f,.76f,-.36f,0,Color.white,43);
                for(int i=0;i<6;i++)
                {
                    float a=instance.Range(0,Mathf.PI*2);var dir=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);
                    instance.Emit(true,position+dir*cell*.08f,dir*cell*instance.Range(.55f,1.3f),instance.Range(.16f,.28f),instance.Range(.13f,.24f),-.05f,instance.Range(-90,90),i%3==0?Gold:Warm,42);
                }
            }
        }
        Mote Emit(bool isStar,Vector3 position,Vector3 velocity,float life,float size,float growth,float spin,Color tint,int order)
        {
            Mote m=null;
            for(int i=0;i<ParticleCapacity;i++){int k=(nextMote+i)%ParticleCapacity;if(!motes[k].active){m=motes[k];nextMote=(k+1)%ParticleCapacity;break;}}
            if(m==null)return null;
            m.active=true;m.outline=false;m.age=0;m.life=life;m.start=position+Vector3.back*.34f;m.velocity=velocity;
            m.size=size;m.growth=growth;m.spin=spin;m.tint=tint;
            var r=m.view;r.gameObject.layer=BoardLayer;r.sortingLayerID=ColorBallFrameAnimator.SeatedSortingLayer;r.sortingOrder=order;
            r.sharedMaterial=material;
            r.sprite=isStar?star:soft;r.transform.position=m.start;r.transform.rotation=Quaternion.Euler(0,0,Range(-25,25));
            r.transform.localScale=Vector3.one*(size*ColorBallFrameAnimator.Cell);r.color=tint;r.enabled=true;return m;
        }
        public static void RevealSparkles(Vector3 centre)
        {
            Prewarm();float cell=ColorBallFrameAnimator.Cell;
            for(int i=0;i<5;i++)
            {
                float a=i*Mathf.PI*2/5;var offset=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0)*cell*.32f;
                instance.Emit(true,centre+offset,offset*.5f,.20f,.23f,-.1f,0,Color.white,42);
            }
        }
        // Sparse sparks around the spinning disc; no orbiting dots or idle motion.
        public static void Charge(Vector3 centre,float progress,bool finishing)
        {
            Prewarm();float cell=ColorBallFrameAnimator.Cell;
            for(int i=0;i<2;i++)
            {
                var offset=new Vector3(instance.Range(-.85f,.85f),instance.Range(-.26f,.26f),0)*cell*progress;
                instance.Emit(true,centre+offset,finishing?-offset:offset*.2f,.16f,instance.Range(.10f,.20f),-.05f,0,Warm,42);
            }
        }
        void Draw(Beam b)
        {
            float reach=Mathf.Clamp01(b.age/ArrivalTime),fade=1-Mathf.Clamp01((b.age-ArrivalTime)/.13f),cell=ColorBallFrameAnimator.Cell;
            var delta=b.to-b.from;var side=new Vector3(-delta.y,delta.x,0).normalized;
            for(int i=0;i<Samples;i++)
            {
                float t=i/(float)(Samples-1)*reach;
                var p=Vector3.Lerp(b.from,b.to,t)+side*(Mathf.Sin(t*Mathf.PI)*b.bend);
                b.glow.SetPosition(i,p);b.core.SetPosition(i,p);
            }
            b.glow.widthMultiplier=cell*.105f*fade;b.core.widthMultiplier=cell*.027f*fade;
            b.glow.startColor=b.glow.endColor=new Color(1,.90f,.52f,.85f*fade);
            b.core.startColor=b.core.endColor=new Color(1,1,.90f,fade);
            b.tip.enabled=true;b.tip.gameObject.layer=BoardLayer;b.tip.sortingLayerID=ColorBallFrameAnimator.SeatedSortingLayer;
            b.tip.transform.position=b.core.GetPosition(Samples-1)+Vector3.back*.02f;
            b.tip.transform.localScale=Vector3.one*(cell*.25f*fade);
            b.tip.transform.rotation=Quaternion.Euler(0,0,b.age*180);b.tip.color=new Color(1,1,.85f,fade);
        }
        void LateUpdate()
        {
            if(LevelManager.THIS&&LevelManager.THIS.gameStatus==GameState.Pause)return;
            foreach(var b in beams)
            {
                if(!b.active)continue;b.age+=Time.deltaTime;
                if(b.age>=ArrivalTime+.13f){b.active=false;b.glow.enabled=b.core.enabled=b.tip.enabled=false;continue;}
                Draw(b);
            }
            foreach(var m in motes)
            {
                if(!m.active)continue;m.age+=Time.deltaTime;float t=m.age/m.life;
                if(t>=1){m.active=false;m.view.enabled=false;continue;}
                m.view.transform.position=m.start+m.velocity*m.age;
                if(!m.outline)m.view.transform.Rotate(0,0,m.spin*Time.deltaTime);
                m.view.transform.localScale=m.outline?m.fixedScale:Vector3.one*(Mathf.Max(.01f,m.size+m.growth*t)*ColorBallFrameAnimator.Cell);
                var c=m.tint;c.a*=Mathf.Pow(1-t,1.5f);m.view.color=c;
            }
        }
        public static void ClearAll()
        {
            if(!instance)return;
            foreach(var b in instance.beams){b.active=false;b.glow.enabled=b.core.enabled=b.tip.enabled=false;}
            foreach(var m in instance.motes){m.active=false;m.view.enabled=false;}
        }
        void OnDisable(){if(instance==this)ClearAll();}
        void OnDestroy()
        {
            foreach(var sprite in new[]{star,soft,ring})if(sprite){Destroy(sprite.texture);Destroy(sprite);}
            if(material)Destroy(material);if(haloMaterial)Destroy(haloMaterial);if(outlineMaterial)Destroy(outlineMaterial);if(instance==this)instance=null;
        }
    }
}
