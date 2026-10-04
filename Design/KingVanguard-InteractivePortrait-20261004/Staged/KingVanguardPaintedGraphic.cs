using UnityEngine;
using UnityEngine.UI;

namespace RoyalThrone.Characters {
 public sealed class KingVanguardPaintedGraphic : MaskableGraphic {
  Texture2D neutral,worried,sad,laugh; Material ownedMaterial;
  float clock,lean,tap,cheer,worryWeight,sadWeight,blink,jaw,pulse;
  public bool Ready => neutral&&worried&&sad&&laugh&&ownedMaterial;
  public Vector2 Gaze {get;private set;}
  public float LaughWeight {get;private set;}
  public float Blink => blink;
  public float JawOffset => jaw;
  public override Texture mainTexture => neutral?neutral:Texture2D.whiteTexture;
  public bool InitializeArtwork(){
   if(Ready)return true;
   neutral=Resources.Load<Texture2D>("KingVanguard/Interactive/King-Neutral");
   worried=Resources.Load<Texture2D>("KingVanguard/Interactive/King-Worried");
   sad=Resources.Load<Texture2D>("KingVanguard/Interactive/King-Sad");
   laugh=Resources.Load<Texture2D>("KingVanguard/LaughArt/ReferenceLaugh");
   var shader=Resources.Load<Shader>("KingVanguard/Interactive/KingVanguardPainted");
   if(!neutral||!worried||!sad||!laugh||!shader)return false;
   if(!ownedMaterial)ownedMaterial=new Material(shader){name="King interactive portrait (instance)"};
   ownedMaterial.SetTexture("_WorriedTex",worried);ownedMaterial.SetTexture("_SadTex",sad);ownedMaterial.SetTexture("_LaughTex",laugh);material=ownedMaterial;
   return true;
  }
  public void ApplyPose(KingVanguardCharacter.Activity activity,float time,float delta,float phase,Vector2 gaze,float bodyLean,float touch,float laughTime,bool immediate){
   clock=time;Gaze=Vector2.ClampMagnitude(gaze,1);lean=bodyLean;tap=Mathf.Clamp01(touch);
   float targetWorry=activity==KingVanguardCharacter.Activity.Worried?1:0;
   float targetSad=activity==KingVanguardCharacter.Activity.Sad?1:0;
   float targetLaugh=activity==KingVanguardCharacter.Activity.Laughing?KingVanguardLaughTiming.Intensity(laughTime):0;
   bool positive=activity==KingVanguardCharacter.Activity.Happy||activity==KingVanguardCharacter.Activity.Excited||activity==KingVanguardCharacter.Activity.ExtraMoves;
   if(positive)targetLaugh=Mathf.SmoothStep(0,1,phase/.16f)*(1-Mathf.SmoothStep(0,1,(phase-.70f)/.3f));
   float blend=immediate?1:1-Mathf.Exp(-delta*18);
   worryWeight=Mathf.Lerp(worryWeight,targetWorry,blend);sadWeight=Mathf.Lerp(sadWeight,targetSad,blend);
   LaughWeight=activity==KingVanguardCharacter.Activity.Laughing?targetLaugh:Mathf.Lerp(LaughWeight,targetLaugh,blend);
   pulse=activity==KingVanguardCharacter.Activity.Laughing?KingVanguardLaughTiming.PosePulse(laughTime):Mathf.Sin(phase*Mathf.PI);
   cheer=(activity==KingVanguardCharacter.Activity.MatchAcknowledgement||activity==KingVanguardCharacter.Activity.Relief)?Mathf.Sin(phase*Mathf.PI):0;
   // A quick close, short hold and gentler opening. All timing follows the actor's scaled clock.
   float cycle=Mathf.Repeat(clock,4.7f);
   blink=Mathf.SmoothStep(0,1,(cycle-3.55f)/.085f)*(1-Mathf.SmoothStep(0,1,(cycle-3.67f)/.13f));
   blink*=1-Mathf.Max(LaughWeight,sadWeight);
   jaw=LaughWeight*(1-pulse)*6;
   if(ownedMaterial){
    ownedMaterial.SetVector("_Expression",new Vector4(worryWeight,sadWeight,LaughWeight,blink));
    ownedMaterial.SetVector("_Gaze",new Vector4(Gaze.x,Gaze.y,0,0));
    // Stencil materials are cached copies. Update the rendered instance as well.
    var render=materialForRendering;if(render!=ownedMaterial){render.SetVector("_Expression",new Vector4(worryWeight,sadWeight,LaughWeight,blink));render.SetVector("_Gaze",new Vector4(Gaze.x,Gaze.y,0,0));}
   }
   SetVerticesDirty();
  }
  protected override void OnPopulateMesh(VertexHelper vh){
   vh.Clear();if(!Ready)return;
   const int columns=44,rows=44;
   Rect rect=rectTransform.rect;float size=rect.width*1.28f,unit=size/1254;
   float breathe=Mathf.Sin(clock*1.65f)*1.7f;
   float angle=(Gaze.x*1.25f+lean*.65f+Mathf.Sin(clock*1.2f)*.18f+LaughWeight*Mathf.Sin(clock*5)*.5f)*Mathf.Deg2Rad;
   for(int y=0;y<=rows;y++)for(int x=0;x<=columns;x++){
    Vector2 source=new Vector2(x*1254f/columns,y*1254f/rows),p=source;
    float head=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(760,1030,p.y));
    Vector2 offset=p-new Vector2(627,880);float a=angle*head;
    p=new Vector2(627+Mathf.Cos(a)*offset.x-Mathf.Sin(a)*offset.y,880+Mathf.Sin(a)*offset.x+Mathf.Cos(a)*offset.y);
    p.x+=Gaze.x*6*head;
    p.y-=Gaze.y*3*head+tap*7*head+cheer*5*head+breathe*(.3f+.7f*head)+LaughWeight*pulse*5;
    float mouth=Mathf.Exp(-Mathf.Pow((source.x-627)/210,2))*Mathf.SmoothStep(0,1,(source.y-580)/120)*(1-Mathf.SmoothStep(0,1,(source.y-850)/150));
    p.y-=jaw*mouth;
    var v=UIVertex.simpleVert;v.position=new Vector3(rect.center.x+(p.x-627)*unit,rect.yMax-12-p.y*unit,0);v.uv0=new Vector2(source.x/1254,1-source.y/1254);v.color=color;vh.AddVert(v);
   }
   for(int y=0;y<rows;y++)for(int x=0;x<columns;x++){int a=y*(columns+1)+x,b=a+1,c=a+columns+1,d=c+1;vh.AddTriangle(a,c,b);vh.AddTriangle(b,c,d);}
  }
  protected override void OnDestroy(){base.OnDestroy();if(ownedMaterial){if(Application.isPlaying)Destroy(ownedMaterial);else DestroyImmediate(ownedMaterial);}}
 }
}
