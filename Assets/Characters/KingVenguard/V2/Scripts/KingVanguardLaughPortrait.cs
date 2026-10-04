using System.Collections.Generic;
using Spine;
using Spine.Unity;
using UnityEngine;
using UnityEngine.UI;
namespace RoyalThrone.Characters {
 // The reference face is registered to the existing head. Crown and body remain native.
 public sealed class KingVanguardLaughPortrait : MaskableGraphic {
  Texture2D source;float at,opacity;
  SkeletonGraphic rig;KingVanguardLaughter laugh;Bone head,restHead,chest;
  Vector2[] local,closed;
  static List<Vector2> points;static List<int> triangles;
  public float JawOffset {get;private set;}
  public override Texture mainTexture {get{if(!source)source=Resources.Load<Texture2D>("KingVanguard/LaughArt/ReferenceLaugh");return source?source:Texture2D.whiteTexture;}}
  static float Jaw(float t)=>12*(1-KingVanguardLaughTiming.PosePulse(t))+60*(1-Mathf.SmoothStep(0,1,(t-.16f)/.22f))+60*Mathf.SmoothStep(0,1,(t-1.96f)/.26f);
  public void Configure(SkeletonGraphic graphic,KingVanguardLaughter performance){
   rig=graphic;laugh=performance;head=rig.Skeleton.FindBone("yuz");chest=rig.Skeleton.FindBone("pelerin");
   var rest=new Skeleton(rig.Skeleton.Data);rest.SetToSetupPose();rest.Data.FindAnimation(KingVanguardCharacter.IdleAnimation).Apply(rest,0,0,false,null,1,MixBlend.Replace,MixDirection.In);rest.UpdateWorldTransform();restHead=rest.FindBone("yuz");
   EnsureMesh();local=new Vector2[points.Count];closed=new Vector2[points.Count];
   for(int i=0;i<points.Count;i++){
    var uv=points[i];local[i]=Bind(uv);
    float jaw=Mathf.SmoothStep(0,1,Mathf.InverseLerp(580,705,uv.y))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(910,1020,uv.y)));
    float center=1-Mathf.SmoothStep(0,1,Mathf.Abs(uv.x-627)/380);
    closed[i]=Bind(uv-new Vector2(0,72*jaw*center))-local[i];
   }
  }
  Vector2 Bind(Vector2 uv){
   var p=Fit(uv);float x,y;
   // Registered against the existing 336x350 gameplay crop at idle time zero.
   restHead.WorldToLocal((p.x-256)/90.2061856f,(465.278351f-p.y)/90.2061856f,out x,out y);return new Vector2(x,y);
  }
  public void SetPose(float time,float alpha){at=time;opacity=alpha;JawOffset=Jaw(time);SetVerticesDirty();}
  protected override void OnPopulateMesh(VertexHelper vh){
   vh.Clear();if(!rig||local==null)return;
   float scale=rig.canvas?rig.canvas.referencePixelsPerUnit:100;
   for(int i=0;i<points.Count;i++){
    var p=local[i]+closed[i]*(JawOffset/72);float x,y;head.LocalToWorld(p.x,p.y,out x,out y);
    var w=laugh.MoveBody(new Vector2(x,y),chest);
    var position=rectTransform.InverseTransformPoint(rig.rectTransform.TransformPoint(new Vector3(w.x*scale,w.y*scale,0)));
    var v=UIVertex.simpleVert;v.position=position;v.uv0=new Vector2(points[i].x/1254f,1-points[i].y/1254f);v.color=new Color(1,1,1,opacity);vh.AddVert(v);
   }
   for(int i=0;i<triangles.Count;i+=3)vh.AddTriangle(triangles[i],triangles[i+1],triangles[i+2]);
  }
  // Thin-plate registration: forehead, eyes, nose, lip and beard landmarks from the normal rig.
  static readonly Vector4[] Landmarks={
   new Vector4(0.251196172f,0.367623604f,713.270406958f,-766.719885655f),
   new Vector4(0.307017544f,0.303030303f,-332.028708902f,341.956021280f),
   new Vector4(0.390749601f,0.244816587f,-1147.392401662f,-253.586267438f),
   new Vector4(0.470494418f,0.216905901f,451.082395042f,-43.321576207f),
   new Vector4(0.558213716f,0.215311005f,-110.613997071f,-37.973927189f),
   new Vector4(0.629984051f,0.247208931f,363.708754936f,228.903675510f),
   new Vector4(0.693779904f,0.310207337f,-340.804782140f,-227.089084925f),
   new Vector4(0.234449761f,0.446570973f,-417.821469569f,448.077269354f),
   new Vector4(0.725677831f,0.446570973f,-319.488896308f,-180.694406527f),
   new Vector4(0.220893142f,0.582137161f,-56.056271256f,-183.861272618f),
   new Vector4(0.732057416f,0.574162679f,76.858828248f,-165.879985089f),
   new Vector4(0.515948963f,0.752791069f,-5.207483459f,32.530789117f),
   new Vector4(0.325358852f,0.560606061f,761.403034688f,184.956472609f),
   new Vector4(0.626794258f,0.550239234f,217.510512444f,167.094610018f),
   new Vector4(0.342902711f,0.653907496f,-190.194319356f,19.941041246f),
   new Vector4(0.629984051f,0.653907496f,112.341086282f,170.833127788f),
   new Vector4(0.397129187f,0.343700159f,1003.559809101f,-984.422308139f),
   new Vector4(0.561403509f,0.326156300f,-970.514251943f,-1697.812920807f),
   new Vector4(0.397129187f,0.275917065f,792.199009212f,541.871560391f),
   new Vector4(0.551036683f,0.255980861f,-10.404926296f,446.314563775f),
   new Vector4(0.494417863f,0.389952153f,299.165237767f,-839.407721493f),
   new Vector4(0.337320574f,0.446570973f,-665.143540918f,198.324000932f),
   new Vector4(0.630781499f,0.407496013f,1184.210410037f,411.983144445f),
   new Vector4(0.432216906f,0.459330144f,102.192016537f,-31.042921305f),
   new Vector4(0.565390750f,0.441786284f,-1197.892378949f,20.586544760f),
   new Vector4(0.503987241f,0.555821372f,8.147668671f,-81.013908238f),
   new Vector4(0.498405104f,0.443381180f,305.013972931f,-223.353749078f),
   new Vector4(0.510366826f,0.659489633f,-250.761937438f,-228.810551819f),
   new Vector4(0.437001595f,0.378787879f,-934.999038800f,1126.190723787f),
   new Vector4(0.550239234f,0.362838915f,558.661261212f,1605.426941517f),
  };
  static Vector2 Fit(Vector2 uv){var p=uv/1254f;var result=new Vector2(25.608242127f,104.800949445f)+p.x*new Vector2(469.273804324f,21.344212225f)+p.y*new Vector2(0.670832938f,439.341651776f);
   foreach(var k in Landmarks){float dx=p.x-k.x,dy=p.y-k.y,r=dx*dx+dy*dy;if(r>1e-12f)result+=new Vector2(k.z,k.w)*(r*Mathf.Log(r));}return result;}
  static void EnsureMesh(){
   if(points!=null)return;
   // Face-only outline in original painting coordinates; no crown or costume pixels.
   points=new List<Vector2>{
    new Vector2(315,461),
    new Vector2(345,416),
    new Vector2(385,380),
    new Vector2(430,342),
    new Vector2(490,307),
    new Vector2(540,286),
    new Vector2(590,272),
    new Vector2(645,265),
    new Vector2(700,270),
    new Vector2(750,287),
    new Vector2(790,310),
    new Vector2(826,344),
    new Vector2(870,389),
    new Vector2(887,396),
    new Vector2(888,415),
    new Vector2(899,424),
    new Vector2(904,432),
    new Vector2(906,459),
    new Vector2(902,470),
    new Vector2(892,481),
    new Vector2(899,512),
    new Vector2(897,537),
    new Vector2(903,542),
    new Vector2(903,551),
    new Vector2(914,562),
    new Vector2(920,563),
    new Vector2(920,571),
    new Vector2(926,578),
    new Vector2(930,587),
    new Vector2(930,610),
    new Vector2(935,619),
    new Vector2(936,651),
    new Vector2(934,653),
    new Vector2(933,661),
    new Vector2(926,673),
    new Vector2(942,688),
    new Vector2(942,698),
    new Vector2(936,709),
    new Vector2(909,735),
    new Vector2(898,772),
    new Vector2(877,809),
    new Vector2(847,841),
    new Vector2(812,873),
    new Vector2(768,897),
    new Vector2(714,912),
    new Vector2(677,927),
    new Vector2(647,944),
    new Vector2(614,925),
    new Vector2(570,915),
    new Vector2(525,914),
    new Vector2(478,907),
    new Vector2(430,896),
    new Vector2(384,878),
    new Vector2(344,852),
    new Vector2(308,819),
    new Vector2(285,783),
    new Vector2(277,750),
    new Vector2(271,731),
    new Vector2(277,722),
    new Vector2(277,712),
    new Vector2(280,705),
    new Vector2(283,704),
    new Vector2(300,687),
    new Vector2(308,661),
    new Vector2(307,644),
    new Vector2(309,633),
    new Vector2(307,631),
    new Vector2(309,629),
    new Vector2(309,618),
    new Vector2(301,607),
    new Vector2(296,594),
    new Vector2(294,569),
    new Vector2(298,549),
    new Vector2(304,536),
    new Vector2(314,524),
    new Vector2(314,517),
    new Vector2(292,507),
    new Vector2(282,496),
    new Vector2(279,487),
    new Vector2(281,478),
    new Vector2(288,471),
    new Vector2(289,467),
    new Vector2(296,472),
    new Vector2(299,469),
   };
   triangles=new List<int>();var remaining=new List<int>();for(int i=0;i<points.Count;i++)remaining.Add(i);
   float signed=0;for(int i=0;i<points.Count;i++){var a=points[i];var b=points[(i+1)%points.Count];signed+=a.x*b.y-b.x*a.y;}
   float direction=Mathf.Sign(signed);int guard=points.Count*points.Count;
   while(remaining.Count>3&&guard-->0){bool cut=false;for(int i=0;i<remaining.Count;i++){
    int a=remaining[(i+remaining.Count-1)%remaining.Count],b=remaining[i],c=remaining[(i+1)%remaining.Count];
    if(Cross(points[a],points[b],points[c])*direction<=0)continue;
    bool inside=false;foreach(int p in remaining){if(p==a||p==b||p==c)continue;if(Inside(points[p],points[a],points[b],points[c],direction)){inside=true;break;}}
    if(inside)continue;triangles.AddRange(new[]{a,b,c});remaining.RemoveAt(i);cut=true;break;
   }if(!cut)throw new System.InvalidOperationException("Reference silhouette is not a simple polygon");}
   if(remaining.Count==3)triangles.AddRange(remaining);
   for(int step=0;step<4;step++){
    var next=new List<int>();var edges=new Dictionary<ulong,int>();
    int Mid(int a,int b){ulong key=((ulong)Mathf.Min(a,b)<<32)|(uint)Mathf.Max(a,b);if(edges.TryGetValue(key,out int found))return found;int n=points.Count;points.Add((points[a]+points[b])*.5f);edges[key]=n;return n;}
    for(int i=0;i<triangles.Count;i+=3){int a=triangles[i],b=triangles[i+1],c=triangles[i+2],ab=Mid(a,b),bc=Mid(b,c),ca=Mid(c,a);next.AddRange(new[]{a,ab,ca,ab,b,bc,ca,bc,c,ab,bc,ca});}triangles=next;
   }
  }
  static float Cross(Vector2 a,Vector2 b,Vector2 c)=>(b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x);
  static bool Inside(Vector2 p,Vector2 a,Vector2 b,Vector2 c,float d)=>Cross(a,b,p)*d>=0&&Cross(b,c,p)*d>=0&&Cross(c,a,p)*d>=0;
 }
}
