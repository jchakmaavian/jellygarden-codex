using System.Collections.Generic;
using Spine;
using Spine.Unity;
using UnityEngine;

namespace RoyalThrone.Characters {
 /// <summary>Actor-owned laugh attachments. Upper teeth stay on the skull; jaw and beard share a driver.</summary>
 [DisallowMultipleComponent]
 public sealed class KingVanguardLaughArt : MonoBehaviour {
  const int Columns=17, Rows=17;
  ISkeletonAnimation animator; SkeletonGraphic graphic; KingVanguardLaughter laugh;
  Skeleton skeleton, setup; Bone face, restFace;
  readonly List<Part> parts=new List<Part>();
  readonly List<Hidden> hidden=new List<Hidden>();
  readonly List<Material> materials=new List<Material>();
  bool bound, shown;
  public bool IsShowing => shown;
  public int RenderedPartCount => shown ? parts.Count : 0;
  public float JawDrop {get;private set;}
  public static KingVanguardLaughArt Ensure(GameObject go){
   var art=go.GetComponent<KingVanguardLaughArt>();if(!art)art=go.AddComponent<KingVanguardLaughArt>();art.Bind();return art;
  }
  void OnEnable(){Bind();}
  void Bind(){
   if(bound)return;graphic=GetComponent<SkeletonGraphic>();
   animator=graphic as ISkeletonAnimation ?? GetComponent<SkeletonAnimation>();
   laugh=GetComponent<KingVanguardLaughter>();if(animator==null||!laugh)return;
   animator.UpdateLocal+=BeforePose;animator.UpdateComplete+=AfterPose;bound=true;
  }
  void OnDisable(){Cancel();if(bound){animator.UpdateLocal-=BeforePose;animator.UpdateComplete-=AfterPose;}bound=false;}
  void OnDestroy(){foreach(var m in materials)if(m){
   if(graphic&&graphic.CustomMaterialOverride.TryGetValue(m.mainTexture,out var current)&&current==m)graphic.CustomMaterialOverride.Remove(m.mainTexture);
   if(Application.isPlaying)Destroy(m);else DestroyImmediate(m);
  }}
  public void Cancel(){Restore();JawDrop=0;}
  void BeforePose(ISkeletonAnimation unused){Restore();}
  void Restore(){
   if(!shown)return;
   foreach(var p in parts)if(ReferenceEquals(p.slot.Attachment,p.mesh))p.slot.Attachment=p.original;
   foreach(var h in hidden)if(h.slot.Attachment==null)h.slot.Attachment=h.original;
   hidden.Clear();shown=false;
  }
  bool Prepare(){
   if(skeleton==animator.Skeleton&&parts.Count>0)return parts.Count==6;
   skeleton=animator.Skeleton;if(skeleton==null)return false;
   setup=new Skeleton(skeleton.Data);setup.SetToSetupPose();setup.UpdateWorldTransform();
   face=skeleton.FindBone("on_yuz");restFace=setup.FindBone("on_yuz");if(face==null)return false;
   parts.Clear();
   // Visible alpha bounds, sampled from the reviewed PNGs; UV cropping does not alter the artwork.
   Add("Sakal Gölge","Jaw-Beard",new Rect(45,169,1163,952),new Rect(-1.15f,.15f,2.36f,1.94f),Kind.Beard);
   Add("Sol Göz","Eyes-Joy",new Rect(138,209,593,481),new Rect(-.55f,1.82f,.62f,.50f),Kind.Eye);
   Add("Sağ Göz","Eyes-Joy",new Rect(1044,209,593,481),new Rect(.04f,1.87f,.62f,.50f),Kind.Eye);
   Add("Ust Diş","Mouth-Peak",new Rect(125,115,1262,839),new Rect(-.52f,1.08f,1.12f,.56f),Kind.Mouth);
   Add("Bıyık","Moustache-Lifted",new Rect(114,148,1945,443),new Rect(-.89f,1.48f,1.86f,.50f),Kind.Moustache);
   var slot=skeleton.FindSlot("Sakal Surat");var original=slot?.Attachment as MeshAttachment;
   if(original!=null){
    var clipped=(MeshAttachment)original.Copy();var triangles=new List<int>();
    for(int t=0;t<original.Triangles.Length;t+=3){int a=original.Triangles[t],b=original.Triangles[t+1],c=original.Triangles[t+2];
     if(original.RegionUVs[a*2+1]<=.60f&&original.RegionUVs[b*2+1]<=.60f&&original.RegionUVs[c*2+1]<=.60f)triangles.AddRange(new[]{a,b,c});}
    clipped.Triangles=triangles.ToArray();parts.Add(new Part{slot=slot,mesh=clipped,kind=Kind.Head});
   }
   if(graphic&&!graphic.allowMultipleCanvasRenderers){graphic.canvasRenderer.Clear();graphic.allowMultipleCanvasRenderers=true;}
   return parts.Count==6;
  }
  void Add(string slotName,string assetName,Rect crop,Rect bind,Kind kind){
   var texture=Resources.Load<Texture2D>("KingVanguard/LaughArt/"+assetName);var slot=skeleton.FindSlot(slotName);
   if(!texture||slot==null)return;
   Material material=null;
   foreach(var p in parts)if(((AtlasRegion)p.mesh.RendererObject).page.name==assetName){material=(Material)((AtlasRegion)p.mesh.RendererObject).page.rendererObject;break;}
   if(!material){
    var existing=(slot.Attachment as IHasRendererObject)?.RendererObject as AtlasRegion;
    material=graphic?new Material(graphic.material):new Material((Material)existing.page.rendererObject);
    material.mainTexture=texture;material.name="Vanguard laugh "+assetName;material.EnableKeyword("_STRAIGHT_ALPHA_INPUT");
    if(graphic&&(kind==Kind.Eye||kind==Kind.Mouth)){
     var shader=Resources.Load<Shader>("KingVanguard/LaughArt/LaughSkinBlend");
     if(shader){
      material.shader=shader;
      if(kind==Kind.Eye){
       material.SetVector("_ArtRect",new Vector4(138f/1774,1-690f/887,593f/1774,481f/887));
       material.SetVector("_ArtRectRight",new Vector4(1044f/1774,1-690f/887,593f/1774,481f/887));
       material.SetFloat("_Paired",1);material.SetFloat("_FeatherInner",.55f);material.SetFloat("_FeatherOuter",1f);
      }else{
       material.SetVector("_ArtRect",new Vector4(125f/1510,1-954f/1041,1262f/1510,839f/1041));
       material.SetFloat("_FeatherInner",.91f);material.SetFloat("_FeatherOuter",1.08f);
      }
      graphic.CustomMaterialOverride[texture]=material;
     }
    }
    materials.Add(material);
   }
   var page=new AtlasPage{name=assetName,width=texture.width,height=texture.height,rendererObject=material};
   var sourceSize=assetName=="Moustache-Lifted"?new Vector2(2172,724):assetName=="Eyes-Joy"?new Vector2(1774,887):assetName=="Mouth-Peak"?new Vector2(1510,1041):new Vector2(1254,1254);
   var region=new AtlasRegion{name="Laugh "+assetName,page=page,width=(int)crop.width,height=(int)crop.height,
    originalWidth=(int)crop.width,originalHeight=(int)crop.height,u=crop.x/sourceSize.x,u2=crop.xMax/sourceSize.x,
    v=1-crop.y/sourceSize.y,v2=1-crop.yMax/sourceSize.y};
   var uv=new float[Columns*Rows*2];var triangles=new List<int>();
   for(int y=0;y<Rows;y++)for(int x=0;x<Columns;x++){
    int n=y*Columns+x;uv[n*2]=Mathf.Lerp(region.u,region.u2,x/(float)(Columns-1));uv[n*2+1]=Mathf.Lerp(region.v,region.v2,y/(float)(Rows-1));
    if(x<Columns-1&&y<Rows-1)triangles.AddRange(new[]{n,n+1,n+Columns,n+1,n+Columns+1,n+Columns});
   }
   var mesh=new MeshAttachment(slotName){Path="Vanguard Laugh "+assetName,WorldVerticesLength=uv.Length,Vertices=new float[uv.Length],RegionUVs=(float[])uv.Clone(),UVs=uv,Triangles=triangles.ToArray(),RendererObject=region,R=1,G=1,B=1,A=1};
   parts.Add(new Part{slot=slot,mesh=mesh,rect=bind,kind=kind});
  }
  void AfterPose(ISkeletonAnimation unused){
   if(!laugh||!laugh.Active||laugh.Intensity<.72f){Restore();return;}
   if(!Prepare())return;
   // No independent teeth translation: all upper mouth rows use the same fixed skull frame.
   JawDrop=-.30f+laugh.Intensity*(.16f+.40f*laugh.Pulse);
   foreach(var p in parts){
    p.original=p.slot.Attachment;
    if(p.kind!=Kind.Head)for(int y=0;y<Rows;y++)for(int x=0;x<Columns;x++){
     float u=x/(float)(Columns-1),v=y/(float)(Rows-1);
     var point=new Vector2(p.rect.x+u*p.rect.width,p.rect.yMax-v*p.rect.height);
     if(p.kind==Kind.Mouth)point.y-=JawDrop*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.40f,1,v));
     else if(p.kind==Kind.Beard){
      point.y-=JawDrop*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.12f,.40f,v));
      point.y-=.025f*(KingVanguardLaughTiming.PosePulse(laugh.Time-.04f)-laugh.Pulse)*laugh.Intensity*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.7f,1,v));
     }
     else if(p.kind==Kind.Moustache)point.y+=.045f*laugh.Pulse*Mathf.Abs(u*2-1);
     else if(p.kind==Kind.Eye)point.y+=.025f*laugh.Pulse;
     float lx,ly,wx,wy;restFace.WorldToLocal(point.x,point.y,out lx,out ly);face.LocalToWorld(lx,ly,out wx,out wy);
     p.slot.Bone.WorldToLocal(wx,wy,out lx,out ly);int n=(y*Columns+x)*2;p.mesh.Vertices[n]=lx;p.mesh.Vertices[n+1]=ly;
    }
    p.slot.Attachment=p.mesh;
   }
   foreach(string name in HiddenSlots){var slot=skeleton.FindSlot(name);if(slot!=null&&slot.Attachment!=null){hidden.Add(new Hidden{slot=slot,original=slot.Attachment});slot.Attachment=null;}}
   shown=true;
  }
  static readonly string[] HiddenSlots={"Sol  Göz Bebeği","Sağ Göz Bebeği","Sol Göz kapağı","Sağ Göz Kapağı","Sol Kirpik","Sağ Kirpik","Bıyık Gölge","Alt Diş","Dil"};
  enum Kind {Head,Beard,Eye,Mouth,Moustache}
  sealed class Part{public Slot slot;public Attachment original;public MeshAttachment mesh;public Rect rect;public Kind kind;}
  sealed class Hidden{public Slot slot;public Attachment original;}
 }
}
