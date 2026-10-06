using System;
using System.Collections.Generic;
using Spine;
using Spine.Unity;
using Spine.Unity.AttachmentTools;
using UnityEngine;
namespace RoyalThrone.Characters {
 /// <summary>Registers independent redesigned face silhouettes; retains native animation drivers.</summary>
 public static class KingVanguardFaceSkin {
  public const string MouthPath="Vanguard Articulated Mouth";
  static readonly Dictionary<string,int> Parts=new Dictionary<string,int>{
   {"Sakal Surat",0},{"Bıyık",1},{"Burun",2},{"Sol Göz",6},{"Sağ Göz",7},
   {"Sol Kulak",10},{"Sağ Kulak",11},{"Sol  Göz Bebeği",12},{"Sağ Göz Bebeği",13}};
  public static AtlasRegion Region(AtlasPage page,int index){
   int start=page.name.StartsWith("King_")?2192:2034;
   int size=index>=12?240:496,x=index>=12?(index-12)*256+8:(index%4)*512+8,y=index>=12?start+1536+8:start+(index/4)*512+8;
   return new AtlasRegion{name="Vanguard Face "+index,page=page,x=x,y=y,width=size,height=size,originalWidth=size,originalHeight=size,index=-1,u=(float)x/page.width,u2=(float)(x+size)/page.width,v=1f-(float)y/page.height,v2=1f-(float)(y+size)/page.height};
  }
  public static void Apply(SkeletonData data){
   var setup=new Skeleton(data);setup.SetToSetupPose();setup.UpdateWorldTransform();
   foreach(var skin in data.Skins){
    var entries=new List<Skin.SkinEntry>(skin.GetAttachments());
    foreach(var e in entries){
     var render=e.Attachment as IHasRendererObject;var region=render?.RendererObject as AtlasRegion;
     if(region==null||region.page.width!=2048||region.page.height<3500)continue;
     if(region.name.StartsWith("Vanguard Face"))continue;
     var mesh=e.Attachment as MeshAttachment;var ra=e.Attachment as RegionAttachment;
     if(e.Name=="Taç Alt Gölge"){
      int crownIndex=data.FindSlot("Taç").Index;var crown=skin.GetAttachment(crownIndex,"Taç") as MeshAttachment;
      if(crown!=null){var shadow=Shadow(crown,e.Name,setup);skin.RemoveAttachment(e.SlotIndex,e.Name);skin.SetAttachment(e.SlotIndex,e.Name,shadow);}continue;
     }
     if(e.Name=="Ağız"||e.Name=="Alt Diş"||e.Name=="Dil"||e.Name=="Sol Yanak"||e.Name=="Sağ Yanak"||e.Name.Contains("Gölge")){
      // Baked cheeks and continuous beard replace independent skin patches.
      // Original mouth remains invisible as a live deformation driver.
      if(mesh!=null)mesh.A=0;if(ra!=null)ra.A=0;continue;
     }
     if(e.Name=="Ust Diş"){
      var replacement=KingVanguardMouthRig.Create(e.Name,region.page);
      skin.RemoveAttachment(e.SlotIndex,e.Name);skin.SetAttachment(e.SlotIndex,e.Name,replacement);continue;
     }
     int index;if(!Parts.TryGetValue(e.Name,out index))continue;
     var newRegion=Region(region.page,index);
     if(mesh!=null){
      if(index==0||index==1||index==6||index==7){
       Rect fit=index==0?new Rect(.035f,-.015f,.93f,1.03f):index==1?new Rect(.04f,.15f,.92f,.8f):new Rect(0,0,1,1);
       var replacement=KingVanguardCrownSkin.RebindArt(mesh,setup.Slots.Items[e.SlotIndex],setup,newRegion,"Vanguard Face "+index,fit,index==0?17:index==1?21:3,index==0?21:index==1?9:3);
       skin.RemoveAttachment(e.SlotIndex,e.Name);skin.SetAttachment(e.SlotIndex,e.Name,replacement);
      }else mesh.SetRegion(newRegion);
     }else if(ra!=null){
      // Preserve the calibrated native quad dimensions when changing texture pixels.
      ra.SetRegion(newRegion);ra.UpdateOffset();
     }
    }
    int moustacheIndex=data.FindSlotIndex("Bıyık"),shadowIndex=data.FindSlotIndex("Bıyık Gölge");
    if(moustacheIndex>=0&&shadowIndex>=0){var hair=skin.GetAttachment(moustacheIndex,"Bıyık") as MeshAttachment;
     if(hair!=null&&hair.Path=="Vanguard Face 1"){
      var contact=Shadow(hair,"Bıyık Gölge",setup,false);skin.RemoveAttachment(shadowIndex,"Bıyık Gölge");skin.SetAttachment(shadowIndex,"Bıyık Gölge",contact);
     }}
   }
  }
  static MeshAttachment Shadow(MeshAttachment crown,string name,Skeleton setup,bool bandOnly=true){
   var triangles=new List<int>();
   for(int t=0;t<crown.Triangles.Length;t+=3){int a=crown.Triangles[t],b=crown.Triangles[t+1],c=crown.Triangles[t+2];
    if(!bandOnly||(crown.RegionUVs[a*2+1]>=.66f&&crown.RegionUVs[b*2+1]>=.66f&&crown.RegionUVs[c*2+1]>=.66f))triangles.AddRange(new[]{a,b,c});}
   var vertices=(float[])crown.Vertices.Clone();int bi=0,vi=0;
   for(int n=0;n<crown.WorldVerticesLength/2;n++){int count=crown.Bones[bi++];for(int k=0;k<count;k++){
    var bone=setup.Bones.Items[crown.Bones[bi++]];float x0,y0,x1,y1;bone.WorldToLocal(0,0,out x0,out y0);bone.WorldToLocal(0,bandOnly?-.022f:-.013f,out x1,out y1);vertices[vi]+=x1-x0;vertices[vi+1]+=y1-y0;vi+=3;}}
   return new MeshAttachment(name){Path=bandOnly?"Vanguard Crown Contact":"Vanguard Moustache Contact",WorldVerticesLength=crown.WorldVerticesLength,Bones=(int[])crown.Bones.Clone(),Vertices=vertices,RegionUVs=(float[])crown.RegionUVs.Clone(),UVs=(float[])crown.UVs.Clone(),Triangles=triangles.ToArray(),RendererObject=crown.RendererObject,R=.22f,G=.12f,B=.075f,A=bandOnly?.22f:.15f};
  }
 }
}
