using System.Collections.Generic;
using UnityEngine;
using PlayerPrefs = RoyalThrone.Testing.RoyalSave;

namespace JellyGarden.Scripts.Juice
{
    /// <summary>Cell-local presentation for confirmed matches. No matching, rewards or board ownership.</summary>
    public sealed class RoyalMatchBreakService : MonoBehaviour
    {
        public const int CellCapacity = 256;
        public const float GemDuration = .20f, Duration = .80f;
        sealed class Cell
        {
            public SpriteRenderer ghost;
            public bool active, impact;
            public float age, delay, unit, scale;
            public int color, count, emitted, seed, strength;
            public Vector3 center;
            public Color tint;
        }
        static RoyalMatchBreakService instance;
        static bool attempted;
        readonly Cell[] cells = new Cell[CellCapacity];
        readonly List<ParticleSystem> all = new List<ParticleSystem>(40);
        readonly List<Mesh> meshes = new List<Mesh>(32);
        readonly List<Material> materials = new List<Material>(6);
        ParticleSystem[] ice;
        ParticleSystem[][] chips;
        Transform field;
        int levelNumber, cursor, serial;
        uint randomSeed = 72431;
        public int PlayedCells { get; private set; }
        public int EmittedIceCells { get; private set; }
        public int EmittedFragments { get; private set; }
        public int DroppedCells { get; private set; }
        public int ActiveCells { get; private set; }
        public int PeakCells { get; private set; }
        public int VisibleGems { get; private set; }
        public int ParticleCount { get { int n=0; foreach(var ps in all) if(ps) n+=ps.particleCount; return n; } }
        public static RoyalMatchBreakService Existing => instance;
        // Bounded diagnostics also let the Play Mode checks compare real resolver output to cell coordinates.
        public readonly Vector3[] RecentCenters = new Vector3[CellCapacity];
        public readonly int[] RecentColors = new int[CellCapacity];
        public readonly int[] RecentSizes = new int[CellCapacity];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { instance=null; attempted=false; }
        // Archived cell-emitter experiment. No automatic pool allocation or gameplay hook.
        static RoyalMatchBreakService Get()
        {
            if (attempted || !Application.isPlaying) return instance;
            attempted=true;
            var library=Resources.Load<RoyalToolkitLibrary>("RoyalGameplay/Toolkit/Library");
            var bursts=Resources.Load<RoyalBurstLibrary>("RoyalGameplay/Toolkit/BurstPieces");
            var material=Resources.Load<Material>("RoyalMatchFragment");
            if (!library || !library.ice || !bursts || !material) return null;
            instance=new GameObject("Royal match breaks - pooled cell presentation").AddComponent<RoyalMatchBreakService>();
            DontDestroyOnLoad(instance.gameObject);
            instance.Build(library,bursts,material);
            return instance;
        }

        void Build(RoyalToolkitLibrary library,RoyalBurstLibrary bursts,Material template)
        {
            for(int i=0;i<cells.Length;i++)
            {
                var go=new GameObject("Matched gem "+i);go.transform.SetParent(transform,false);
                var sr=go.AddComponent<SpriteRenderer>();sr.enabled=false;
                cells[i]=new Cell { ghost=sr };
            }
            // One copy of the actual AllIn1 Ice Impact layers. Explicit world-space emission
            // gives every cell independent particles while keeping the system/draw-call count fixed.
            var rig=Instantiate(library.ice,transform);rig.name="AllIn1 Ice Impact - shared cell emitters";
            rig.SetActive(true);rig.transform.localPosition=Vector3.zero;rig.transform.localScale=Vector3.one;
            ice=rig.GetComponentsInChildren<ParticleSystem>(true);
            foreach(var ps in ice)
            {
                ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.transform.localScale=Vector3.one;
                Configure(ps,2048);
                var shape=ps.shape;shape.enabled=false;
                var velocity=ps.velocityOverLifetime;velocity.enabled=false;
                var force=ps.forceOverLifetime;force.enabled=false;
                var noise=ps.noise;noise.enabled=false;
                var main=ps.main;main.gravityModifier=0;
                var size=ps.sizeOverLifetime;size.enabled=true;size.separateAxes=false;
                size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,.55f,1,1));
                var color=ps.colorOverLifetime;color.enabled=true;
                var iceFade=new Gradient();iceFade.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},
                    new[]{new GradientAlphaKey(.8f,0),new GradientAlphaKey(1,.15f),new GradientAlphaKey(.9f,.5f),new GradientAlphaKey(0,1)});
                color.color=iceFade;
                all.Add(ps);
            }
            chips=new ParticleSystem[6][];
            for(int color=0;color<chips.Length;color++)
            {
                var pieces=color<bursts.bursts.Length?bursts.bursts[color].pieces:null;
                int count=pieces==null?4:pieces.Length;
                chips[color]=new ParticleSystem[count];
                var fallback=RoyalPieceSkin.For(ItemsTypes.NONE,color);
                var texture=pieces==null?fallback.texture:pieces[0].sprite.texture;
                var material=new Material(template){name="Match chips "+color,mainTexture=texture};materials.Add(material);
                for(int p=0;p<count;p++)
                {
                    var sprite=pieces==null?fallback:pieces[p].sprite;
                    Rect rect=sprite.rect;
                    // The sixth gem is porcelain: cut its own painted surface into four shards.
                    if(pieces==null)rect=new Rect(rect.x+(p%2)*rect.width*.5f,rect.y+(p/2)*rect.height*.5f,rect.width*.5f,rect.height*.5f);
                    var mesh=ChipMesh(rect,texture);meshes.Add(mesh);
                    var go=new GameObject("Match chip "+color+"/"+p);go.transform.SetParent(transform,false);
                    var ps=go.AddComponent<ParticleSystem>();Configure(ps,1024);
                    var shape=ps.shape;shape.enabled=false;
                    var main=ps.main;main.startSpeed=0;
                    var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.renderMode=ParticleSystemRenderMode.Mesh;
                    renderer.mesh=mesh;renderer.sharedMaterial=material;renderer.alignment=ParticleSystemRenderSpace.World;
                    renderer.sortingLayerName="Default";renderer.sortingOrder=40;
                    renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
                    var fade=ps.colorOverLifetime;fade.enabled=true;
                    var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},
                        new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(1,.64f),new GradientAlphaKey(0,1)});
                    fade.color=gradient;
                    var shrink=ps.sizeOverLifetime;shrink.enabled=true;
                    shrink.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,1),new Keyframe(.64f,.95f),new Keyframe(1,.12f)));
                    var spin=ps.rotationOverLifetime;spin.enabled=true;
                    spin.z=new ParticleSystem.MinMaxCurve(-5.5f,5.5f);
                    chips[color][p]=ps;all.Add(ps);
                }
            }
        }
        static void Configure(ParticleSystem ps,int capacity)
        {
            ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;main.loop=false;main.playOnAwake=false;main.stopAction=ParticleSystemStopAction.None;
            main.simulationSpace=ParticleSystemSimulationSpace.World;main.scalingMode=ParticleSystemScalingMode.Shape;
            main.useUnscaledTime=false;main.maxParticles=capacity;main.startDelay=0;main.cullingMode=ParticleSystemCullingMode.AlwaysSimulate;
            var emission=ps.emission;emission.enabled=false;
            ps.useAutoRandomSeed=false;ps.randomSeed=23717;
        }
        static Mesh ChipMesh(Rect r,Texture tex)
        {
            float diagonal=Mathf.Sqrt(r.width*r.width+r.height*r.height);
            float x=r.width/diagonal*.5f,y=r.height/diagonal*.5f;
            var mesh=new Mesh {name="Normalized painted fracture",hideFlags=HideFlags.HideAndDontSave};
            mesh.vertices=new[]{new Vector3(-x,-y,0),new Vector3(x,-y,0),new Vector3(x,y,0),new Vector3(-x,y,0)};
            mesh.uv=new[]{new Vector2(r.xMin/tex.width,r.yMin/tex.height),new Vector2(r.xMax/tex.width,r.yMin/tex.height),new Vector2(r.xMax/tex.width,r.yMax/tex.height),new Vector2(r.xMin/tex.width,r.yMax/tex.height)};
            mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateBounds();return mesh;
        }
        public static bool Play(Item item,int matchSize,float stagger)
        {
            if(!item || !item.square || !item.sprRenderer || !item.sprRenderer.sprite)return false;
            var service=Get();return service && service.Begin(item,matchSize,stagger);
        }
        bool Begin(Item item,int matchSize,float stagger)
        {
            var lm=LevelManager.THIS;if(!lm || !lm.GameField)return false;
            if(field!=lm.GameField || levelNumber!=lm.currentLevel){Clear();field=lm.GameField;levelNumber=lm.currentLevel;}
            Cell c=null;
            for(int i=0;i<cells.Length;i++) {int n=(cursor+i)%cells.Length;if(!cells[n].active){c=cells[n];cursor=(n+1)%cells.Length;break;}}
            if(c==null){DroppedCells++;return false;}
            var source=item.sprRenderer;
            c.center=item.square.transform.position;
            c.unit=Mathf.Max(.01f,Mathf.Min(Mathf.Abs(lm.squareWidth*field.lossyScale.x),Mathf.Abs(lm.squareHeight*field.lossyScale.y)));
            c.color=Mathf.Clamp(item.color,0,5);c.strength=Mathf.Clamp(matchSize-3,0,2);
            c.count=Mathf.Min(7,chips[c.color].Length)*2+c.strength*2;
            if(PlayerPrefs.GetInt("RoyalThrone.HomeMotion",1)==0)c.count=4;
            c.delay=Mathf.Clamp(stagger,0,.04f);c.age=0;c.emitted=0;c.impact=false;c.seed=++serial*397;
            c.tint=source.color;c.tint.a=1;
            c.ghost.sprite=source.sprite;c.ghost.color=c.tint;c.ghost.flipX=source.flipX;c.ghost.flipY=source.flipY;
            c.ghost.sortingLayerID=source.sortingLayerID;c.ghost.sortingOrder=Mathf.Max(source.sortingOrder,35);
            c.scale=c.unit*.94f/Mathf.Max(source.sprite.bounds.size.x,source.sprite.bounds.size.y);
            c.ghost.transform.SetPositionAndRotation(c.center+Vector3.back*.3f-source.sprite.bounds.center*c.scale,Quaternion.identity);
            c.ghost.transform.localScale=Vector3.one*c.scale;c.ghost.enabled=true;c.active=true;
            int record=PlayedCells%CellCapacity;RecentCenters[record]=c.center;RecentColors[record]=c.color;RecentSizes[record]=matchSize;
            PlayedCells++;ActiveCells++;PeakCells=Mathf.Max(PeakCells,ActiveCells);
            return true;
        }
        void Impact(Cell c)
        {
            c.impact=true;EmittedIceCells++;
            foreach(var ps in ice)
            {
                var renderer=ps.GetComponent<ParticleSystemRenderer>();if(!renderer || !renderer.enabled)continue;
                bool spark=ps.name.Contains("Sparks") || ps.name.Contains("Flakes");
                bool ring=ps.name.Contains("Ring");
                // Omit the large glow/flare layers; the toolkit's rings and crystal sparks
                // retain their original shader/texture/streams in a strictly local footprint.
                if(!spark && !ring)continue;
                int count=spark?3+c.strength:1;
                for(int i=0;i<count;i++)
                {
                    float angle=Hash(c.seed+i,17)*Mathf.PI*2;
                    var emit=new ParticleSystem.EmitParams {
                        position=c.center+Vector3.back*.65f,
                        velocity=spark?new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0)*c.unit*.45f:Vector3.zero,
                        startSize=c.unit*(spark?Mathf.Lerp(.09f,.15f,Hash(c.seed+i,29)):.88f+c.strength*.03f),
                        startLifetime=.32f,rotation=spark?angle*Mathf.Rad2Deg:0,
                        startColor=Color.white,randomSeed=++randomSeed
                    };
                    ps.Emit(emit,1);
                }
                if(!ps.isPlaying)ps.Play(false);
            }
        }
        void Fragment(Cell c,int n)
        {
            var ps=chips[c.color][n%chips[c.color].Length];
            var main=ps.main;main.gravityModifier=c.unit*7.5f/Mathf.Max(.01f,Mathf.Abs(Physics.gravity.y));
            float a=n*Mathf.PI*2/c.count+(Hash(c.seed+n,13)-.5f)*.55f;
            float speed=c.unit*Mathf.Lerp(.55f,1.0f,Hash(c.seed+n,23));
            var emit=new ParticleSystem.EmitParams {
                position=c.center+new Vector3(Mathf.Cos(a)*.07f*c.unit,Mathf.Sin(a)*.07f*c.unit,-.5f),
                velocity=new Vector3(Mathf.Cos(a)*speed,Mathf.Sin(a)*speed*.7f+.25f*c.unit,0),
                startSize=c.unit*Mathf.Lerp(n%3==0?.21f:.12f,n%3==0?.28f:.18f,Hash(c.seed+n,31)),
                startLifetime=Mathf.Max(.1f,Duration-c.age),rotation=Hash(c.seed+n,41)*360,
                startColor=c.tint,randomSeed=++randomSeed
            };
            ps.Emit(emit,1);if(!ps.isPlaying)ps.Play(false);EmittedFragments++;
        }
        static float Hash(int a,int b) { unchecked {int h=a*73856093^b*19349663;h=(h^(h>>13))*1274126177;return ((h^(h>>16))&0x7fffff)/(float)0x7fffff;} }
        void Update()
        {
            var lm=LevelManager.THIS;
            if(field && (!lm || field!=lm.GameField || levelNumber!=lm.currentLevel || !field.gameObject.activeInHierarchy || lm.gameStatus==GameState.Map || lm.gameStatus==GameState.PrepareGame)){Clear();field=null;return;}
            bool paused=Time.timeScale<=0 || (lm && lm.gameStatus==GameState.Pause);
            foreach(var ps in all){if(paused && ps.isPlaying)ps.Pause(false);else if(!paused && ps.isPaused)ps.Play(false);}
            if(paused)return;
            VisibleGems=0;
            foreach(var c in cells)
            {
                if(!c.active)continue;c.age+=Time.deltaTime;
                float t=c.age-c.delay;
                if(t>=.025f && !c.impact)Impact(c);
                if(t>=.09f)
                {
                    int wanted=Mathf.Min(c.count,1+Mathf.FloorToInt((t-.09f)/.13f*c.count));
                    while(c.emitted<wanted)Fragment(c,c.emitted++);
                }
                if(t<GemDuration)
                {
                    float squash=Mathf.Sin(Mathf.Clamp01(t/.13f)*Mathf.PI)*.06f;
                    c.ghost.transform.localScale=new Vector3(1+squash,1-squash,1)*c.scale;
                    float alpha=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.105f,GemDuration,t));
                    c.ghost.color=new Color(c.tint.r,c.tint.g,c.tint.b,alpha);VisibleGems++;
                }
                else c.ghost.enabled=false;
                if(c.age>=Duration){c.active=false;c.ghost.enabled=false;c.ghost.sprite=null;ActiveCells--;}
            }
        }
        public void Clear()
        {
            foreach(var c in cells)if(c!=null){c.active=false;c.ghost.enabled=false;c.ghost.sprite=null;}
            foreach(var ps in all)if(ps)ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
            ActiveCells=VisibleGems=0;
        }
        void OnDisable(){Clear();}
        void OnDestroy(){foreach(var mesh in meshes)if(mesh)Destroy(mesh);foreach(var mat in materials)if(mat)Destroy(mat);if(instance==this)instance=null;}
    }
}

