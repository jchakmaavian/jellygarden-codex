using PlayerPrefs = RoyalThrone.Testing.RoyalSave;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace JellyGarden.Scripts.Juice
{
    // Runs after the legacy Animator and fitted skin. Only child art receives hint motion.
    [DefaultExecutionOrder(1000),DisallowMultipleComponent]
    public sealed class RoyalHintPresenter : MonoBehaviour
    {
        const int MaxContours=16,MaxCells=128;
        public const float PairStartDelay=.4f,PairTravelSeconds=1.2f,PairRestSeconds=.35f;
        const float PairNudge=.08f,MatchGrowth=.06f,ArtClearance=.015f;
        sealed class HintGem { public Item Item;public RoyalPieceSkin Skin;public Vector3 Offset;public float Growth; }
        readonly List<HintGem> hintGems=new List<HintGem>();
        static RoyalHintPresenter attentionSource;
        sealed class Outline
        {
            public LineRenderer Glow,Edge,Halo,Core,Glint,Chaser;
            public Vector3[] Points;
            public float[] Distance;
            public float Length;
            public readonly List<float> Corners=new List<float>();   // distance along the loop of each corner's middle
            public SpriteRenderer HeadA,HeadB;                        // the stars riding the two travelling lights
        }
        // The look (2026-10-04, third pass at the user's request): a white brush stroke, no dark line. A painted texture
        // (bristle streaks, dry-brush edges) tiled along the contour, a width that swells and thins like brush pressure and a
        // slight hand-drawn wobble, over a soft white glow. Two white lights chase each other round it, each carrying a
        // turning star; sparkles twinkle along the stroke, pop at its corners, trail the lights and drift off it. All white.
        // The sparkles use their own random sequence, so a hint never changes the game's. Sorting, from the pieces up: glow
        // +3, halo (the stroke's soft body) +5, core (its bristles) +6, lights +7, sparkles and stars +8.
        const float GlowWidth=.2f,HaloWidth=.11f,CoreWidth=.062f,LightWidth=.065f,HeadSize=.74f,Wobble=.014f;
        const int SparkleCount=34;
        enum Kind { Twinkle, Corner, Trail, Drift }
        sealed class Sparkle { public SpriteRenderer Renderer; public float Born,Life,Spin,Size; public int Contour; public float At; public Kind Kind; public Vector2 Drift; }
        static Texture2D brushTexture,glowTexture;Material brushMaterial,glowMaterial;
        readonly float[] heads=new float[MaxContours];
        readonly List<Sparkle> sparkles=new List<Sparkle>();
        static Sprite sparkleSprite;
        global::System.Random sparkleRandom=new global::System.Random(7);
        readonly List<Outline> pool=new List<Outline>();
        readonly Vector3[] glintPoints=new Vector3[9];
        RoyalHintPlan plan;RoyalPieceSkin firstSkin,secondSkin;
        InputAction press,cancel;Material material;GameObject visualRoot;
        Material pairGlowMaterial;readonly List<SpriteRenderer> pairGlows=new List<SpriteRenderer>();
        float shownAt,cell;int activeContours;
        public bool IsVisible=>plan!=null;
        public int VisibleSwapCount=>plan==null?0:plan.Second?2:1;
        public int OutlineCellCount=>plan==null?0:plan.PreviewCells.Count;
        public int ContourCount=>activeContours;
        public int BoundaryEdgeCount {get;private set;}
        public LineRenderer ContourRenderer(int index)=>index>=0&&index<activeContours?pool[index].Core:null;
        public RoyalHintPlan CurrentPlan=>plan;

        // Read the same selected pair/tap piece that is currently being presented.
        // Revalidate here as well as LateUpdate: consumers may run before the
        // presenter has cleared a hint invalidated by this frame's board change.
        public static bool TryGetCurrentFocus(LevelManager level,out Vector3 worldPoint)
        {
            worldPoint=default;
            var source=attentionSource;
            if(!source||!source.isActiveAndEnabled||!level||level!=LevelManager.THIS||
                !source.CanShow(level,source.plan))return false;
            var selected=source.plan;
            worldPoint=selected.Second?(selected.First.transform.position+selected.Second.transform.position)*.5f:
                selected.First.transform.position;
            return true;
        }

        void OnEnable()
        {
            var input=InputManager.Instance;
            press=input.Gameplay?.PrimaryTouch;cancel=input.Gameplay?.Cancel;
            if(press!=null)press.performed+=PointerPressed;
            if(cancel!=null)cancel.performed+=PointerPressed;
            RoyalHintSettings.Changed+=PreferenceChanged;
        }
        void OnDisable()
        {
            if(press!=null)press.performed-=PointerPressed;
            if(cancel!=null)cancel.performed-=PointerPressed;
            press=cancel=null;RoyalHintSettings.Changed-=PreferenceChanged;Hide();
        }
        void PointerPressed(InputAction.CallbackContext context){Hide();}
        void PreferenceChanged(bool enabled){if(!enabled)Hide();}
        bool CanShow(LevelManager lm,RoyalHintPlan candidate)
        {return lm&&candidate!=null&&RoyalHintSettings.Enabled&&lm.gameStatus==GameState.Playing&&!lm.DragBlocked&&!InputManager.Instance.IsTouching&&!RoyalThrone.ReferenceGameplay.ReferenceBoosterBar.OwnsInput&&candidate.IsCurrent(lm);}
        public bool Show(RoyalHintPlan candidate)
        {
            Hide();var lm=LevelManager.THIS;
            if(!CanShow(lm,candidate)||candidate.PreviewCells.Count>MaxCells)return false;
            firstSkin=candidate.First.GetComponent<RoyalPieceSkin>();
            secondSkin=candidate.Second?candidate.Second.GetComponent<RoyalPieceSkin>():null;
            if(!firstSkin||(candidate.Second&&!secondSkin)){firstSkin=secondSkin=null;return false;}
            var grid=new List<Vector2Int>();foreach(var sq in candidate.PreviewCells)grid.Add(new Vector2Int(sq.col,-sq.row));
            var shape=RoyalHintContour.Build(grid);
            if(shape.Loops.Count==0||shape.Loops.Count>MaxContours){firstSkin=secondSkin=null;return false;}
            if(attentionSource&&attentionSource!=this)attentionSource.Hide();
            EnsureMaterial();cell=Mathf.Min(lm.squareWidth,lm.squareHeight);
            var anchor=candidate.PreviewCells[0];var anchorPoint=anchor.transform.position;
            Vector3 World(Vector2 point)=>new Vector3(anchorPoint.x+(point.x-anchor.col-.5f)*lm.squareWidth,
                anchorPoint.y+(point.y+anchor.row-.5f)*lm.squareHeight,candidate.First.transform.position.z-.02f);
            for(int i=0;i<shape.Loops.Count;i++){
                if(pool.Count<=i)pool.Add(CreateOutline(i));var outline=pool[i];
                var vertices=shape.Loops[i];var rounded=new List<Vector3>();var cornerIndex=new List<int>();
                for(int j=0;j<vertices.Count;j++){
                    var previous=vertices[(j+vertices.Count-1)%vertices.Count];var here=vertices[j];var next=vertices[(j+1)%vertices.Count];
                    if(here-previous==next-here)continue;
                    Vector3 corner=World(here),entry=corner+(World(previous)-corner).normalized*cell*.085f,
                        exit=corner+(World(next)-corner).normalized*cell*.085f;
                    for(int s=0;s<=5;s++){float t=s/5f;if(s==3)cornerIndex.Add(rounded.Count);rounded.Add((1-t)*(1-t)*entry+2*(1-t)*t*corner+t*t*exit);}
                }
                var points=new List<Vector3>();var cornerAt=new HashSet<int>();
                for(int j=0;j<rounded.Count;j++){
                    if(cornerIndex.Contains(j))cornerAt.Add(points.Count);
                    var a=rounded[j];var b=rounded[(j+1)%rounded.Count];int steps=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(a,b)/(cell*.22f)));
                    for(int k=0;k<steps;k++)points.Add(Vector3.Lerp(a,b,(float)k/steps));
                }
                // The hand-drawn wobble: each point eased sideways by a slow wave along the loop, a fraction of a cell.
                float phase=(float)sparkleRandom.NextDouble()*10;var wobbled=new Vector3[points.Count];
                for(int j=0;j<points.Count;j++){
                    var tangent=(points[(j+1)%points.Count]-points[(j+points.Count-1)%points.Count]).normalized;var normal=new Vector3(-tangent.y,tangent.x,0);
                    float w=Mathf.Sin(j*.55f+phase)*.6f+Mathf.Sin(j*1.37f+phase*2.3f)*.4f;
                    wobbled[j]=points[j]+normal*w*cell*Wobble;
                }
                outline.Points=wobbled;outline.Distance=new float[points.Count+1];outline.Length=0;outline.Corners.Clear();
                for(int j=0;j<points.Count;j++){if(cornerAt.Contains(j))outline.Corners.Add(outline.Length);outline.Length+=Vector3.Distance(wobbled[j],wobbled[(j+1)%points.Count]);outline.Distance[j+1]=outline.Length;}
                int baseOrder=candidate.First.sprRenderer.sortingOrder;
                var layers=new[]{outline.Glow,outline.Edge,outline.Halo,outline.Core,outline.Glint,outline.Chaser};
                for(int k=0;k<layers.Length;k++){
                    var line=layers[k];line.sortingLayerID=candidate.First.sprRenderer.sortingLayerID;line.sortingOrder=baseOrder+3+Mathf.Min(k,4);
                    line.enabled=line!=outline.Edge;   // no dark line
                }
                outline.Glow.widthMultiplier=cell*GlowWidth;outline.Halo.widthMultiplier=cell*HaloWidth;outline.Core.widthMultiplier=cell*CoreWidth;
                outline.Glint.widthMultiplier=outline.Chaser.widthMultiplier=cell*LightWidth;
                // Brush pressure: the stroke swells and thins along its length (different on every hint).
                var pressure=new AnimationCurve();int keys=Mathf.Clamp(Mathf.RoundToInt(outline.Length/(cell*.6f)),6,24);
                for(int k=0;k<=keys;k++)pressure.AddKey(new Keyframe((float)k/keys,k==keys?pressure.keys[0].value:.42f+(float)sparkleRandom.NextDouble()*.95f));
                for(int k=0;k<pressure.length;k++)pressure.SmoothTangents(k,0);
                outline.Core.widthCurve=pressure;outline.Halo.widthCurve=pressure;
                outline.Core.textureScale=new Vector2(1/(cell*1.1f),1);outline.Halo.textureScale=new Vector2(1/(cell*1.6f),1);
                foreach(var line in new[]{outline.Glow,outline.Halo,outline.Core}){line.positionCount=points.Count;line.SetPositions(outline.Points);}
                foreach(var head in new[]{outline.HeadA,outline.HeadB}){head.sortingLayerID=candidate.First.sprRenderer.sortingLayerID;head.sortingOrder=baseOrder+8;head.enabled=false;}
            }
            plan=candidate;attentionSource=this;activeContours=shape.Loops.Count;BoundaryEdgeCount=shape.BoundaryEdges;shownAt=Time.unscaledTime;
            PrepareGemInteraction(lm);
            sparkleRandom=new global::System.Random(candidate.PreviewCells.Count*7919+lm.moveID);
            EnsureSparkles(candidate.First.sprRenderer.sortingLayerID,candidate.First.sprRenderer.sortingOrder+8);
            foreach(var sparkle in sparkles)Respawn(sparkle,shownAt-(float)sparkleRandom.NextDouble()*.7f);
            Animate();return true;
        }
        // The brush: 256 along the stroke (tiling) by 64 across. Bristle streaks run along it, its edges are ragged and dry
        // (alpha broken up by streaky noise), and its density drifts along its length. All periodic along u, so it tiles.
        static float Wave(float u,float v,int seed)
        {
            float sum=0,norm=0;var r=new global::System.Random(seed);
            for(int k=1;k<=5;k++){float a=1f/k;float ph=(float)r.NextDouble()*6.283f,pv=(float)r.NextDouble()*6.283f;sum+=a*Mathf.Sin(6.2832f*k*u+ph+v*(k*1.7f+pv));norm+=a;}
            return .5f+.5f*sum/norm;
        }
        static Texture2D BrushTexture()
        {
            if(brushTexture)return brushTexture;
            const int w=256,h=64;brushTexture=new Texture2D(w,h,TextureFormat.RGBA32,false){name="Royal hint brush",wrapModeU=TextureWrapMode.Repeat,wrapModeV=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear,hideFlags=HideFlags.DontSave};
            var bristle=new float[h];var r=new global::System.Random(11);
            for(int y=0;y<h;y++)bristle[y]=.5f+.5f*(float)r.NextDouble();
            var px=new Color[w*h];
            for(int y=0;y<h;y++)for(int x=0;x<w;x++){
                float u=(float)x/w,v=(y+.5f)/h;
                float lo=.08f+.16f*Wave(u,0,3),hi=.92f-.16f*Wave(u,0,5);
                float edge=Mathf.SmoothStep(0,1,(v-lo)/.12f)*Mathf.SmoothStep(0,1,(hi-v)/.12f);
                float streak=Mathf.Lerp(bristle[y],1,.2f)*(.62f+.38f*Wave(u*2,v*9,7));
                float centre=Mathf.Abs(v-.5f)*2,dry=centre>.35f?Mathf.SmoothStep(0,1,(Wave(u*3,v*14,9)-.3f-.25f*(centre-.35f))/.28f):1;
                px[y*w+x]=new Color(1,1,1,Mathf.Clamp01(edge*streak*dry*1.3f));
            }
            brushTexture.SetPixels(px);brushTexture.Apply(false,true);return brushTexture;
        }
        static Texture2D GlowTexture()
        {
            if(glowTexture)return glowTexture;
            const int h=64;glowTexture=new Texture2D(4,h,TextureFormat.RGBA32,false){name="Royal hint glow",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear,hideFlags=HideFlags.DontSave};
            var px=new Color[4*h];
            for(int y=0;y<h;y++){float v=(y+.5f)/h*2-1;var c=new Color(1,1,1,Mathf.Exp(-v*v*4.5f));for(int x=0;x<4;x++)px[y*4+x]=c;}
            glowTexture.SetPixels(px);glowTexture.Apply(false,true);return glowTexture;
        }
        // A soft four-point star, white, drawn once.
        static Sprite SparkleSprite()
        {
            if(sparkleSprite)return sparkleSprite;
            const int n=64;var texture=new Texture2D(n,n,TextureFormat.RGBA32,false){name="Royal hint sparkle",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear,hideFlags=HideFlags.DontSave};
            var pixels=new Color[n*n];
            for(int y=0;y<n;y++)for(int x=0;x<n;x++){
                float u=(x+.5f)/n*2-1,v=(y+.5f)/n*2-1,ax=Mathf.Abs(u),ay=Mathf.Abs(v);
                float arms=Mathf.Max(Mathf.Exp(-ay*ay/.0035f)*Mathf.Clamp01(1-ax),Mathf.Exp(-ax*ax/.0035f)*Mathf.Clamp01(1-ay));
                float core=Mathf.Exp(-(u*u+v*v)/.03f);
                pixels[y*n+x]=new Color(1,1,1,Mathf.Clamp01(arms*.95f+core));
            }
            texture.SetPixels(pixels);texture.Apply(false,true);
            sparkleSprite=Sprite.Create(texture,new Rect(0,0,n,n),new Vector2(.5f,.5f),n);sparkleSprite.name="Royal hint sparkle";sparkleSprite.hideFlags=HideFlags.DontSave;
            return sparkleSprite;
        }
        void EnsureSparkles(int layer,int order)
        {
            while(sparkles.Count<SparkleCount){
                var go=new GameObject("Hint sparkle "+sparkles.Count);go.transform.SetParent(visualRoot.transform,false);
                var renderer=go.AddComponent<SpriteRenderer>();renderer.sprite=SparkleSprite();renderer.sharedMaterial=material;renderer.enabled=false;
                sparkles.Add(new Sparkle{Renderer=renderer});
            }
            foreach(var sparkle in sparkles){sparkle.Renderer.sortingLayerID=layer;sparkle.Renderer.sortingOrder=order;}
        }
        void Respawn(Sparkle sparkle,float now)
        {
            double roll=sparkleRandom.NextDouble();
            sparkle.Kind=roll<.38?Kind.Trail:roll<.58?Kind.Corner:roll<.78?Kind.Drift:Kind.Twinkle;
            sparkle.Born=now;sparkle.Life=.3f+(float)sparkleRandom.NextDouble()*.45f;sparkle.Spin=((float)sparkleRandom.NextDouble()-.5f)*200;
            sparkle.Size=sparkle.Kind==Kind.Corner?.36f+(float)sparkleRandom.NextDouble()*.22f:.16f+(float)sparkleRandom.NextDouble()*.22f;
            sparkle.Contour=activeContours>0?sparkleRandom.Next(activeContours):0;sparkle.At=(float)sparkleRandom.NextDouble();sparkle.Drift=Vector2.zero;
            if(activeContours==0)return;
            var o=pool[sparkle.Contour];float length=Mathf.Max(.0001f,o.Length);
            // Trail: born just behind one of the two lights, where it stays. Corner: on a corner of the stroke, bigger.
            // Drift: floats off the stroke, outwards and up, while it twinkles.
            if(sparkle.Kind==Kind.Trail){float behind=cell*(.06f+(float)sparkleRandom.NextDouble()*.55f);
                float head=heads[sparkle.Contour]+(sparkleRandom.NextDouble()<.5?0:o.Length*.5f);sparkle.At=Mathf.Repeat(head-behind,o.Length)/length;}
            else if(sparkle.Kind==Kind.Corner&&o.Corners.Count>0)sparkle.At=o.Corners[sparkleRandom.Next(o.Corners.Count)]/length;
            else if(sparkle.Kind==Kind.Drift){float dx=(float)sparkleRandom.NextDouble()*2-1,dy=(float)sparkleRandom.NextDouble();sparkle.Drift=new Vector2(dx*.3f,.25f+dy*.3f)*cell;sparkle.Life+=.25f;}
        }
        void EnsureMaterial()
        {
            if(material)return;
            material=new Material(Shader.Find("Sprites/Default")){name="Royal hint contour material"};
            brushMaterial=new Material(material){name="Royal hint brush",mainTexture=BrushTexture()};
            glowMaterial=new Material(material){name="Royal hint glow",mainTexture=GlowTexture()};
            visualRoot=new GameObject("Royal flowing match hint");visualRoot.transform.SetParent(transform,false);
        }
        Outline CreateOutline(int index)
        {
            LineRenderer Create(string name,bool loop){
                var go=new GameObject("Contour "+index+" "+name);go.transform.SetParent(visualRoot.transform,false);
                var line=go.AddComponent<LineRenderer>();line.sharedMaterial=material;line.useWorldSpace=true;line.loop=loop;
                line.numCornerVertices=3;line.numCapVertices=3;line.alignment=LineAlignment.View;
                line.textureMode=LineTextureMode.Stretch;line.enabled=false;return line;
            }
            var outline=new Outline{Glow=Create("soft white glow",true),Edge=Create("unused edge",true),Halo=Create("white brush body",true),Core=Create("white brush bristles",true),
                Glint=Create("travelling light",false),Chaser=Create("travelling light chaser",false)};
            outline.Glow.sharedMaterial=glowMaterial;outline.Halo.sharedMaterial=outline.Core.sharedMaterial=brushMaterial;
            outline.Halo.textureMode=outline.Core.textureMode=LineTextureMode.Tile;
            SpriteRenderer Head(string name){var go=new GameObject("Contour "+index+" "+name);go.transform.SetParent(visualRoot.transform,false);
                var r=go.AddComponent<SpriteRenderer>();r.sprite=SparkleSprite();r.sharedMaterial=material;r.enabled=false;return r;}
            outline.HeadA=Head("light star");outline.HeadB=Head("chaser star");
            outline.Glint.positionCount=outline.Chaser.positionCount=glintPoints.Length;
            outline.Glint.widthCurve=outline.Chaser.widthCurve=new AnimationCurve(new Keyframe(0,0),new Keyframe(.35f,1),new Keyframe(.65f,1),new Keyframe(1,0));
            return outline;
        }
        void LateUpdate()
        {
            if(plan==null)return;
            if(!CanShow(LevelManager.THIS,plan)){Hide();return;}
            Animate();
        }
        void Animate()
        {
            bool motion=PlayerPrefs.GetInt("RoyalThrone.HomeMotion",1)!=0;
            float elapsed=Time.unscaledTime-shownAt,pulse=motion?Mathf.Pow(Mathf.Sin(elapsed*Mathf.PI/1.1f),2):0;
            if(plan.Second){
                float phase=Mathf.Repeat(Mathf.Max(0,elapsed-PairStartDelay),PairTravelSeconds+PairRestSeconds);
                float interaction=motion&&elapsed>=PairStartDelay&&phase<PairTravelSeconds
                    ?Mathf.Pow(Mathf.Sin(phase*Mathf.PI/PairTravelSeconds),2):0;
                for(int i=0;i<hintGems.Count;i++){
                    var gem=hintGems[i];
                    Pose(gem.Skin,gem.Item,gem.Offset*interaction,1+gem.Growth*interaction);
                    GlowGem(i,gem.Item.sprRenderer,interaction);
                }
            }else Pose(firstSkin,plan.First,Vector3.zero,1+.018f*pulse);
            // White only: the bristles at full white, the brush body and the glow breathing with the pulse.
            float neon=motion?.5f+.5f*Mathf.Sin(elapsed*Mathf.PI*2/1.1f):1;
            for(int i=0;i<activeContours;i++){
                var outline=pool[i];
                outline.Glow.startColor=outline.Glow.endColor=new Color(1,1,1,.32f+.22f*neon);
                outline.Halo.startColor=outline.Halo.endColor=new Color(1,1,1,.72f+.18f*neon);
                outline.Core.startColor=outline.Core.endColor=new Color(1,1,1,1);
                outline.Glint.enabled=outline.Chaser.enabled=outline.HeadA.enabled=outline.HeadB.enabled=motion;
                if(!motion)continue;
                float length=Mathf.Min(cell*.75f,outline.Length*.24f);
                float head=Mathf.Repeat(elapsed*cell*2.2f+i*cell,outline.Length);heads[i]=head;
                for(int j=0;j<glintPoints.Length;j++)glintPoints[j]=PointAlong(outline,Mathf.Repeat(head-length*j/(glintPoints.Length-1),outline.Length));
                outline.Glint.SetPositions(glintPoints);outline.Glint.startColor=outline.Glint.endColor=new Color(1,1,1,1);
                float chase=Mathf.Repeat(head+outline.Length*.5f,outline.Length);   // the second light, half a lap behind
                for(int j=0;j<glintPoints.Length;j++)glintPoints[j]=PointAlong(outline,Mathf.Repeat(chase-length*j/(glintPoints.Length-1),outline.Length));
                outline.Chaser.SetPositions(glintPoints);outline.Chaser.startColor=outline.Chaser.endColor=new Color(1,1,1,.92f);
                // A turning, pulsing star rides each light.
                float twinkle=.82f+.18f*Mathf.Sin(elapsed*13f+i);
                foreach(var (star,at,sign) in new[]{(outline.HeadA,head,1f),(outline.HeadB,chase,-1f)}){
                    var t=star.transform;t.position=PointAlong(outline,at)+Vector3.back*.012f;
                    t.localScale=Vector3.one*cell*HeadSize*twinkle;t.rotation=Quaternion.Euler(0,0,sign*elapsed*170f);star.color=Color.white;
                }
            }
            // Sparkles: each twinkles once (grows, turns, fades) at a point on a contour, then appears somewhere else.
            float now=Time.unscaledTime;
            foreach(var sparkle in sparkles){
                if(!motion||activeContours==0){sparkle.Renderer.enabled=false;continue;}
                float age=now-sparkle.Born;
                if(age>sparkle.Life||sparkle.Contour>=activeContours){Respawn(sparkle,now);age=0;}
                if(age<0){sparkle.Renderer.enabled=false;continue;}
                var outline=pool[sparkle.Contour];float p=age/sparkle.Life,bloom=Mathf.Sin(p*Mathf.PI);
                var t=sparkle.Renderer.transform;t.position=PointAlong(outline,sparkle.At*outline.Length)+(Vector3)(sparkle.Drift*p)+Vector3.back*.01f;
                t.localScale=Vector3.one*cell*sparkle.Size*bloom;t.rotation=Quaternion.Euler(0,0,sparkle.Spin*age);
                sparkle.Renderer.color=new Color(1,1,1,bloom);sparkle.Renderer.enabled=true;
            }
        }
        static void Pose(RoyalPieceSkin skin,Item item,Vector3 worldOffset,float scale)
        {if(skin&&item&&item.sprRenderer)skin.SetHintPose(item.sprRenderer.transform.parent.InverseTransformVector(worldOffset),scale);}
        void PrepareGemInteraction(LevelManager lm)
        {
            // The match contour contains destination cells. Include their current
            // occupants and both exchange partners, including the gem outside it.
            void Add(Item item){
                if(!item||!item.sprRenderer)return;
                foreach(var existing in hintGems)if(existing.Item==item)return;
                var skin=item.GetComponent<RoyalPieceSkin>();if(!skin)return;
                skin.ApplyNow();hintGems.Add(new HintGem{Item=item,Skin=skin});
            }
            foreach(var sq in plan.PreviewCells)Add(sq.item);
            Add(plan.First);Add(plan.Second);
            Vector3 direction=plan.Second?(plan.Second.transform.position-plan.First.transform.position).normalized:Vector3.zero;
            var affected=new HashSet<Item>();foreach(var gem in hintGems)affected.Add(gem.Item);
            Bounds Peak(Item item){
                var art=item.sprRenderer;var b=art.bounds;if(!affected.Contains(item))return b;
                var pivot=art.transform.position;
                return new Bounds(pivot+(b.center-pivot)*(1+MatchGrowth),b.size*(1+MatchGrowth));
            }
            Vector3 Motion(Item item)=>item==plan.First?direction:item==plan.Second?-direction:Vector3.zero;
            bool horizontal=Mathf.Abs(direction.x)>.5f;int axis=horizontal?0:1,cross=horizontal?1:0;
            float travel=cell*PairNudge;
            // Share the free space between the two moving partners. Artwork can
            // cross an imaginary cell edge, but cannot touch another gem. Testing
            // the expanded peak bounds also keeps every intermediate pose clear.
            foreach(var item in new[]{plan.First,plan.Second}){
                if(!item)continue;var a=Peak(item);
                foreach(var sq in lm.squaresArray){
                    var other=sq?sq.item:null;if(!other||other==item||!other.sprRenderer)continue;
                    var b=Peak(other);
                    if(a.max[cross]<=b.min[cross]||b.max[cross]<=a.min[cross])continue;
                    float sign=b.center[axis]>a.center[axis]?1:-1;
                    float closing=(Motion(item)[axis]-Motion(other)[axis])*sign;if(closing<=0)continue;
                    float gap=sign>0?b.min[axis]-a.max[axis]:a.min[axis]-b.max[axis];
                    travel=Mathf.Min(travel,(gap-cell*ArtClearance)/closing);
                }
            }
            foreach(var gem in hintGems){gem.Growth=MatchGrowth;gem.Offset=Motion(gem.Item)*Mathf.Max(0,travel);}
        }
        void GlowGem(int index,SpriteRenderer source,float pulse)
        {
            // A pooled additive copy brightens the matching-area artwork. No material,
            // transform or collider on the gameplay root is changed.
            while(pairGlows.Count<=index)pairGlows.Add(null);
            var glow=pairGlows[index];
            if(pulse<=0||!source.enabled){if(glow)glow.enabled=false;return;}
            if(!pairGlowMaterial){
                pairGlowMaterial=new Material(Resources.Load<Material>("JuiceAdditive")){name="Royal hint gem glow"};
                pairGlowMaterial.EnableKeyword("SPRITE_RENDERER_COLOR");
                pairGlowMaterial.SetFloat("_UseRendererColor",1);pairGlowMaterial.SetFloat("_Boost",1);
                pairGlowMaterial.SetColor("_Color",Color.white);pairGlowMaterial.SetColor("_Tint",Color.white);
            }
            if(!glow){
                glow=new GameObject("Hint match gem glow "+index).AddComponent<SpriteRenderer>();
                glow.sharedMaterial=pairGlowMaterial;pairGlows[index]=glow;
            }
            var tr=glow.transform;var art=source.transform;
            tr.SetParent(art,false);tr.localPosition=Vector3.zero;tr.localRotation=Quaternion.identity;tr.localScale=Vector3.one;
            glow.sprite=source.sprite;glow.flipX=source.flipX;glow.flipY=source.flipY;
            glow.sortingLayerID=source.sortingLayerID;glow.sortingOrder=source.sortingOrder+1;
            // JuiceAdditive premultiplies then blends by alpha: square-root keeps
            // brightness in step with the movement (about 23 percent at peak).
            glow.color=new Color(1,1,1,.48f*Mathf.Sqrt(pulse)*source.color.a);glow.enabled=true;
        }
        static Vector3 PointAlong(Outline outline,float distance)
        {
            for(int i=0;i<outline.Points.Length;i++)if(distance<=outline.Distance[i+1])
                return Vector3.Lerp(outline.Points[i],outline.Points[(i+1)%outline.Points.Length],Mathf.InverseLerp(outline.Distance[i],outline.Distance[i+1],distance));
            return outline.Points[0];
        }
        public void Hide()
        {
            if(attentionSource==this)attentionSource=null;
            foreach(var glow in pairGlows)if(glow){glow.enabled=false;if(visualRoot)glow.transform.SetParent(visualRoot.transform,false);}
            foreach(var gem in hintGems)if(gem.Skin)gem.Skin.ClearHintPose();hintGems.Clear();
            if(firstSkin)firstSkin.ClearHintPose();if(secondSkin)secondSkin.ClearHintPose();
            firstSkin=secondSkin=null;plan=null;activeContours=0;BoundaryEdgeCount=0;
            foreach(var outline in pool){outline.Glow.enabled=outline.Edge.enabled=outline.Halo.enabled=outline.Core.enabled=outline.Glint.enabled=outline.Chaser.enabled=false;
                outline.HeadA.enabled=outline.HeadB.enabled=false;}
            foreach(var sparkle in sparkles)sparkle.Renderer.enabled=false;
        }
        void OnDestroy(){Hide();if(visualRoot)Destroy(visualRoot);if(material)Destroy(material);if(pairGlowMaterial)Destroy(pairGlowMaterial);}
    }
}
