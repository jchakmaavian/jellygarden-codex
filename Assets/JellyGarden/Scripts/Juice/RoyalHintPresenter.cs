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
        static RoyalHintPresenter attentionSource;
        sealed class Outline
        {
            public LineRenderer Halo,Core,Glint;
            public Vector3[] Points;
            public float[] Distance;
            public float Length;
        }
        readonly List<Outline> pool=new List<Outline>();
        readonly Vector3[] glintPoints=new Vector3[9];
        RoyalHintPlan plan;RoyalPieceSkin firstSkin,secondSkin;
        InputAction press,cancel;Material material;GameObject visualRoot;
        float shownAt,cell;int activeContours;
        public bool IsVisible=>plan!=null;
        public int VisibleSwapCount=>plan==null?0:plan.Second?2:1;
        public int OutlineCellCount=>plan==null?0:plan.MatchCells.Count;
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
        {return lm&&candidate!=null&&RoyalHintSettings.Enabled&&lm.gameStatus==GameState.Playing&&!lm.DragBlocked&&!InputManager.Instance.IsTouching&&candidate.IsCurrent(lm);}
        public bool Show(RoyalHintPlan candidate)
        {
            Hide();var lm=LevelManager.THIS;
            if(!CanShow(lm,candidate)||candidate.MatchCells.Count>MaxCells)return false;
            firstSkin=candidate.First.GetComponent<RoyalPieceSkin>();
            secondSkin=candidate.Second?candidate.Second.GetComponent<RoyalPieceSkin>():null;
            if(!firstSkin||(candidate.Second&&!secondSkin)){firstSkin=secondSkin=null;return false;}
            var grid=new List<Vector2Int>();foreach(var sq in candidate.MatchCells)grid.Add(new Vector2Int(sq.col,-sq.row));
            var shape=RoyalHintContour.Build(grid);
            if(shape.Loops.Count==0||shape.Loops.Count>MaxContours){firstSkin=secondSkin=null;return false;}
            EnsureMaterial();cell=Mathf.Min(lm.squareWidth,lm.squareHeight);
            var anchor=candidate.MatchCells[0];var anchorPoint=anchor.transform.position;
            Vector3 World(Vector2 point)=>new Vector3(anchorPoint.x+(point.x-anchor.col-.5f)*lm.squareWidth,
                anchorPoint.y+(point.y+anchor.row-.5f)*lm.squareHeight,candidate.First.transform.position.z-.02f);
            for(int i=0;i<shape.Loops.Count;i++){
                if(pool.Count<=i)pool.Add(CreateOutline(i));var outline=pool[i];
                var vertices=shape.Loops[i];var points=new List<Vector3>();
                for(int j=0;j<vertices.Count;j++){
                    var previous=vertices[(j+vertices.Count-1)%vertices.Count];var here=vertices[j];var next=vertices[(j+1)%vertices.Count];
                    if(here-previous==next-here)continue;
                    Vector3 corner=World(here),entry=corner+(World(previous)-corner).normalized*cell*.085f,
                        exit=corner+(World(next)-corner).normalized*cell*.085f;
                    for(int s=0;s<=5;s++){float t=s/5f;points.Add((1-t)*(1-t)*entry+2*(1-t)*t*corner+t*t*exit);}
                }
                outline.Points=points.ToArray();outline.Distance=new float[points.Count+1];outline.Length=0;
                for(int j=0;j<points.Count;j++){outline.Length+=Vector3.Distance(points[j],points[(j+1)%points.Count]);outline.Distance[j+1]=outline.Length;}
                foreach(var line in new[]{outline.Halo,outline.Core,outline.Glint}){
                    line.sortingLayerID=candidate.First.sprRenderer.sortingLayerID;line.sortingOrder=candidate.First.sprRenderer.sortingOrder+4;
                    line.enabled=true;
                }
                outline.Halo.widthMultiplier=cell*.075f;outline.Core.widthMultiplier=cell*.025f;outline.Glint.widthMultiplier=cell*.046f;
                outline.Halo.positionCount=outline.Core.positionCount=points.Count;
                outline.Halo.SetPositions(outline.Points);outline.Core.SetPositions(outline.Points);
            }
            plan=candidate;attentionSource=this;activeContours=shape.Loops.Count;BoundaryEdgeCount=shape.BoundaryEdges;shownAt=Time.unscaledTime;
            Animate();return true;
        }
        void EnsureMaterial()
        {
            if(material)return;
            material=new Material(Shader.Find("Sprites/Default")){name="Royal hint contour material"};
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
            var outline=new Outline{Halo=Create("soft gold",true),Core=Create("ivory",true),Glint=Create("travelling light",false)};
            outline.Glint.positionCount=glintPoints.Length;
            outline.Glint.widthCurve=new AnimationCurve(new Keyframe(0,0),new Keyframe(.35f,1),new Keyframe(.65f,1),new Keyframe(1,0));
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
                var direction=(plan.Second.transform.position-plan.First.transform.position).normalized;
                Pose(firstSkin,plan.First,direction*cell*.045f*pulse,1+.008f*pulse);
                Pose(secondSkin,plan.Second,-direction*cell*.045f*pulse,1+.008f*pulse);
            }else Pose(firstSkin,plan.First,Vector3.zero,1+.018f*pulse);
            for(int i=0;i<activeContours;i++){
                var outline=pool[i];var gold=new Color(1,.74f,.18f,.26f+.12f*pulse);var ivory=new Color(1,.97f,.71f,.76f+.2f*pulse);
                outline.Halo.startColor=outline.Halo.endColor=gold;outline.Core.startColor=outline.Core.endColor=ivory;
                outline.Glint.enabled=motion;
                if(!motion)continue;
                float head=Mathf.Repeat(elapsed*cell*2.2f+i*cell,outline.Length),length=Mathf.Min(cell*.75f,outline.Length*.24f);
                for(int j=0;j<glintPoints.Length;j++)glintPoints[j]=PointAlong(outline,Mathf.Repeat(head-length*j/(glintPoints.Length-1),outline.Length));
                outline.Glint.SetPositions(glintPoints);outline.Glint.startColor=outline.Glint.endColor=new Color(1,1,.91f,.96f);
            }
        }
        static void Pose(RoyalPieceSkin skin,Item item,Vector3 worldOffset,float scale)
        {if(skin&&item&&item.sprRenderer)skin.SetHintPose(item.sprRenderer.transform.parent.InverseTransformVector(worldOffset),scale);}
        static Vector3 PointAlong(Outline outline,float distance)
        {
            for(int i=0;i<outline.Points.Length;i++)if(distance<=outline.Distance[i+1])
                return Vector3.Lerp(outline.Points[i],outline.Points[(i+1)%outline.Points.Length],Mathf.InverseLerp(outline.Distance[i],outline.Distance[i+1],distance));
            return outline.Points[0];
        }
        public void Hide()
        {
            if(attentionSource==this)attentionSource=null;
            if(firstSkin)firstSkin.ClearHintPose();if(secondSkin)secondSkin.ClearHintPose();
            firstSkin=secondSkin=null;plan=null;activeContours=0;BoundaryEdgeCount=0;
            foreach(var outline in pool){outline.Halo.enabled=false;outline.Core.enabled=false;outline.Glint.enabled=false;}
        }
        void OnDestroy(){Hide();if(visualRoot)Destroy(visualRoot);if(material)Destroy(material);}
    }
}
