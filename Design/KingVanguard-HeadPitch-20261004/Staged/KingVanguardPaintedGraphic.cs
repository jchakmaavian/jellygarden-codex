using UnityEngine;
using UnityEngine.UI;

namespace RoyalThrone.Characters {
 public sealed class KingVanguardPaintedGraphic : MaskableGraphic {
  Texture2D neutral,worried,sad,laugh,happy,excited; Material ownedMaterial;
  readonly KingVanguardPerformance performance = new KingVanguardPerformance();
  public KingVanguardPerformance Performance => performance;
  public bool Ready => neutral&&worried&&sad&&laugh&&happy&&excited&&ownedMaterial;
  public Vector2 Gaze => performance.Eyes;
  public float LaughWeight => performance.Laugh;
  public float Blink => performance.Lids.x;
  public float HappyWeight => performance.Happy;
  public float ExcitedWeight => performance.Excited;
  public float JawOffset => performance.Jaw;
  public override Texture mainTexture => neutral?neutral:Texture2D.whiteTexture;
  public bool InitializeArtwork(){
   if(canvas)canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.TexCoord2;
   if(Ready)return true;
   neutral=Resources.Load<Texture2D>("KingVanguard/Interactive/King-Neutral");
   worried=Resources.Load<Texture2D>("KingVanguard/Interactive/King-Worried");
   sad=Resources.Load<Texture2D>("KingVanguard/Interactive/King-Sad");
   laugh=Resources.Load<Texture2D>("KingVanguard/LaughArt/ReferenceLaugh");
   happy=Resources.Load<Texture2D>("KingVanguard/Interactive/King-Happy");
   excited=Resources.Load<Texture2D>("KingVanguard/Interactive/King-Excited");
   var shader=Resources.Load<Shader>("KingVanguard/Interactive/KingVanguardPainted");
   if(!neutral||!worried||!sad||!laugh||!happy||!excited||!shader)return false;
   if(!ownedMaterial)ownedMaterial=new Material(shader){name="King interactive portrait (instance)"};
   ownedMaterial.SetTexture("_WorriedTex",worried);ownedMaterial.SetTexture("_SadTex",sad);ownedMaterial.SetTexture("_LaughTex",laugh);ownedMaterial.SetTexture("_HappyTex",happy);ownedMaterial.SetTexture("_ExcitedTex",excited);material=ownedMaterial;
   return true;
  }
  public void ApplyPose(KingVanguardCharacter.Activity activity,float time,float delta,float phase,Vector2 gaze,float bodyLean,float touch,float laughTime,bool immediate,int attentionSource=0){
   if(immediate)performance.Reset();
   else performance.Step(activity,delta,phase,gaze,bodyLean,touch,laughTime,attentionSource);
   if(ownedMaterial){
    ApplyMaterial(ownedMaterial);
    var render=materialForRendering;if(render!=ownedMaterial)ApplyMaterial(render);
   }
   SetVerticesDirty();
  }
  void ApplyMaterial(Material target){
   target.SetVector("_Expression",new Vector4(performance.Worry,performance.Sad,performance.Laugh,0));
   target.SetVector("_Positive",new Vector4(performance.Happy,performance.Excited,0,0));
   target.SetVector("_Gaze",new Vector4(Gaze.x,Gaze.y,0,0));
   target.SetVector("_Lids",new Vector4(performance.Lids.x,performance.Lids.y,performance.Head.w,0));
   target.SetFloat("_HeadPitch",performance.Bow);
  }
  protected override void OnPopulateMesh(VertexHelper vh){
   vh.Clear();if(!Ready)return;
   Rect rect=rectTransform.rect;float size=rect.width*1.28f,unit=size/1254;
   Vector4 pose=performance.Head;Vector2 body=performance.Body;
   float angle=pose.z*Mathf.Deg2Rad;
   float w=performance.Worry,s=performance.Sad,h=performance.Happy,e=performance.Excited,l=performance.Laugh;
   float n=Mathf.Max(0,1-w-s-h-e-l);
   for(int i=0;i<KingVanguardFaceMesh.Neutral.Length;i++){
    Vector2 source=KingVanguardFaceMesh.Neutral[i];
    Vector2 p=source*n+KingVanguardFaceMesh.Worried[i]*w+KingVanguardFaceMesh.Sad[i]*s+KingVanguardFaceMesh.Happy[i]*h+KingVanguardFaceMesh.Excited[i]*e+KingVanguardFaceMesh.Laugh[i]*l;
    float left=EyeMask(source,new Vector2(516,443)),right=EyeMask(source,new Vector2(690,415));
    p=Vector2.Lerp(p,KingVanguardFaceMesh.Laugh[i],Mathf.Max(left*performance.Lids.x,right*performance.Lids.y)*(1-l));
    p=KingVanguardHeadPitch.Project(p,source,performance.Bow);
    float head=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(760,1030,source.y));
    Vector2 offset=p-new Vector2(627,880);float a=angle*head+body.y*Mathf.Deg2Rad*(1-head);
    p=new Vector2(627+Mathf.Cos(a)*offset.x-Mathf.Sin(a)*offset.y,880+Mathf.Sin(a)*offset.x+Mathf.Cos(a)*offset.y);
    p.x+=pose.x*head;p.y-=pose.y*head+body.x*(1-head);
    float mouth=Mathf.Exp(-Mathf.Pow((source.x-627)/210,2))*Mathf.SmoothStep(0,1,(source.y-580)/120)*(1-Mathf.SmoothStep(0,1,(source.y-850)/150));
    p.y-=performance.Jaw*mouth;
    var v=UIVertex.simpleVert;v.position=new Vector3(rect.center.x+(p.x-627)*unit,rect.yMax-12-p.y*unit,0);
    v.uv0=Pair(source,KingVanguardFaceMesh.Worried[i]);
    v.uv1=Pair(KingVanguardFaceMesh.Sad[i],KingVanguardFaceMesh.Happy[i]);
    v.uv2=Pair(KingVanguardFaceMesh.Excited[i],KingVanguardFaceMesh.Laugh[i]);
    v.color=color;vh.AddVert(v);
   }
   var triangles=KingVanguardFaceMesh.Triangles;
   for(int i=0;i<triangles.Length;i+=3)vh.AddTriangle(triangles[i],triangles[i+2],triangles[i+1]);
  }
  static Vector4 Pair(Vector2 first,Vector2 second)=>new Vector4(first.x/1254,1-first.y/1254,second.x/1254,1-second.y/1254);
  static float EyeMask(Vector2 p,Vector2 center){float d=new Vector2((p.x-center.x)/72,(p.y-center.y)/48).magnitude;return 1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.6f,1.2f,d));}
  protected override void OnDestroy(){base.OnDestroy();if(ownedMaterial){if(Application.isPlaying)Destroy(ownedMaterial);else DestroyImmediate(ownedMaterial);}}
 }
}
