using System.Collections.Generic;
using Spine;
using Spine.Unity;
using UnityEngine;
namespace RoyalThrone.Characters {
 [ExecuteAlways,DisallowMultipleComponent]
 public sealed class KingVanguardMouthRig:MonoBehaviour {
  const int Columns=21,Rows=5,PatchVertices=Columns*Rows,TotalVertices=PatchVertices*3;
  ISkeletonAnimation animator;Skeleton skeleton;MeshAttachment mouth;float[] sourceWorld;
  public float Opening {get;private set;} public float Width {get;private set;}
  public float MaximumToothEscape {get;private set;}
  public static void Ensure(GameObject go){if(!go.GetComponent<KingVanguardMouthRig>())go.AddComponent<KingVanguardMouthRig>();}
  void OnEnable(){animator=GetComponent<SkeletonGraphic>() as ISkeletonAnimation ?? GetComponent<SkeletonAnimation>();if(animator!=null){animator.UpdateComplete+=UpdateMouth;UpdateMouth(animator);}}
  void OnDisable(){if(animator!=null)animator.UpdateComplete-=UpdateMouth;animator=null;skeleton=null;mouth=null;}
  public static MeshAttachment Create(string name,AtlasPage page){
   var regions=new[]{KingVanguardFaceSkin.Region(page,3),KingVanguardFaceSkin.Region(page,4),KingVanguardFaceSkin.Region(page,5)};
   var uv=new float[TotalVertices*2];var triangles=new List<int>();
   for(int part=0;part<3;part++)for(int y=0;y<Rows;y++)for(int x=0;x<Columns;x++){
    int n=part*PatchVertices+y*Columns+x;float u=(float)x/(Columns-1),v=(float)y/(Rows-1);var r=regions[part];
    uv[n*2]=Mathf.Lerp(r.u,r.u2,u);uv[n*2+1]=Mathf.Lerp(r.v,r.v2,v);
    if(x<Columns-1&&y<Rows-1)triangles.AddRange(new[]{n,n+1,n+Columns,n+1,n+Columns+1,n+Columns});
   }
   return new MeshAttachment(name){Path=KingVanguardFaceSkin.MouthPath,WorldVerticesLength=TotalVertices*2,Vertices=new float[TotalVertices*2],RegionUVs=(float[])uv.Clone(),UVs=uv,Triangles=triangles.ToArray(),RendererObject=regions[0],R=1,G=1,B=1,A=1};
  }
  void UpdateMouth(ISkeletonAnimation animated){
   var current=animated.Skeleton;if(current==null)return;
   var driverSlot=current.FindSlot("Ağız");var target=current.FindSlot("Ust Diş");var driver=driverSlot?.Attachment as MeshAttachment;
   var template=current.Data.DefaultSkin?.GetAttachment(target==null?-1:target.Data.Index,"Ust Diş") as MeshAttachment;
   if(target==null||template?.Path!=KingVanguardFaceSkin.MouthPath)return;
   if(driver==null||driver.WorldVerticesLength<80){target.Attachment=null;return;}
   if(skeleton!=current||mouth==null){skeleton=current;mouth=Create("Ust Diş",((AtlasRegion)template.RendererObject).page);sourceWorld=new float[driver.WorldVerticesLength];}
   driver.ComputeWorldVertices(driverSlot,sourceWorld);
   // Recovered mouth hole: upper contour vertices 21..31, lower contour 32..39.
   // Sample the original animated geometry, including native FFD and crossfades.
   Vector2 left=Point(21),right=Point(31),axis=(right-left).normalized;
   if(axis.x<0){var swap=left;left=right;right=swap;axis=-axis;}
   var up=new Vector2(-axis.y,axis.x);
   Width=Mathf.Clamp(Vector2.Distance(left,right)*1.08f,.25f,1.8f);
   var upper=(Point(24)+Point(25)+Point(26))/3f;var lower=(Point(34)+Point(35)+Point(36))/3f;
   float rawOpening=Mathf.Max(0,Vector2.Dot(upper-lower,up));
   Opening=Mathf.Clamp(rawOpening,.008f,Width*.65f);
   float rim=Width*.045f,height=Opening+rim*2;
   var center=(left+right)*.5f;
   // Center in the live lip gap rather than averaging all vertices of a skin patch.
   center+=up*(Vector2.Dot((upper+lower)*.5f-center,up));
   float cornerLift=Mathf.Clamp(Vector2.Dot((left+right)*.5f-upper,up),-height*.5f,height*.5f);
   MaximumToothEscape=0;
   for(int part=0;part<3;part++)for(int y=0;y<Rows;y++)for(int x=0;x<Columns;x++){
    float u=(float)x/(Columns-1),v=(float)y/(Rows-1);float px,py;
    if(part==0){
     px=(u-.5f)*Width;py=(.5f-v)*height;
     // Preserve native smile/frown corner motion while keeping the central bite coherent.
     py+=(cornerLift-height*.08f)*Mathf.Pow(Mathf.Abs(u*2-1),2);
    }else{
     px=(u-.5f)*Width*(part==1?.68f:.56f);
     float mouthU=.5f+px/Width,vt,vb;KingVanguardMouthContour.Bounds(mouthU,out vt,out vb);
     float bend=(cornerLift-height*.08f)*Mathf.Pow(Mathf.Abs(mouthU*2-1),2);
     float cavityTop=(.5f-vt)*height+bend,cavityBottom=(.5f-vb)*height+bend;
     float cavityMiddle=(cavityTop+cavityBottom)*.5f;
     float fullToothHeight=Width*(part==1?.12f:.085f);
     float top=part==1?cavityTop:cavityBottom+fullToothHeight;
     float bottom=top-fullToothHeight;
     float rawY=Mathf.Lerp(top,bottom,v);
     float limitTop=part==1?cavityTop:Mathf.Lerp(cavityMiddle,cavityBottom,.15f);
     float limitBottom=part==1?Mathf.Lerp(cavityMiddle,cavityTop,.15f):cavityBottom;
     // Crop by collapsing exterior geometry at the cavity boundary. Visible teeth
     // keep their own height; no independent translation, rotation or scale fit.
     py=Mathf.Clamp(rawY,limitBottom,limitTop);
     if(part==2&&Opening<Width*.24f)py=limitBottom;
     MaximumToothEscape=Mathf.Max(MaximumToothEscape,Mathf.Max(0,Mathf.Max(py-cavityTop,cavityBottom-py)));
    }
    var world=center+axis*px+up*py;float lx,ly;target.Bone.WorldToLocal(world.x,world.y,out lx,out ly);
    int n=(part*PatchVertices+y*Columns+x)*2;mouth.Vertices[n]=lx;mouth.Vertices[n+1]=ly;
   }
   target.Attachment=mouth;
   target.R=driverSlot.R;target.G=driverSlot.G;target.B=driverSlot.B;target.A=driverSlot.A;
  }
  Vector2 Point(int i){return new Vector2(sourceWorld[i*2],sourceWorld[i*2+1]);}
 }
}
