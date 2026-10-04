using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalThrone.Characters {
 // Original reference pixels, clipped by a silhouette mesh. The texture is never redrawn.
 public sealed class KingVanguardLaughPortrait : MaskableGraphic {
  Texture2D source;
  float at, opacity;
  static List<Vector2> points;
  static List<int> triangles;
  public float JawOffset {get;private set;}
  static float Jaw(float time)=>12*(1-KingVanguardLaughTiming.PosePulse(time))+60*(1-Mathf.SmoothStep(0,1,(time-.16f)/.22f))+60*Mathf.SmoothStep(0,1,(time-1.96f)/.26f);
  public override Texture mainTexture {get{if(!source)source=Resources.Load<Texture2D>("KingVanguard/LaughArt/ReferenceLaugh");return source?source:Texture2D.whiteTexture;}}
  public void SetPose(float time,float alpha){at=time;opacity=alpha;JawOffset=Jaw(time);SetVerticesDirty();}
  protected override void OnPopulateMesh(VertexHelper vh){
   vh.Clear();EnsureMesh();var rect=rectTransform.rect;
   float size=rect.width*1.18f;
   float pulse=KingVanguardLaughTiming.PosePulse(at);
   JawOffset=Jaw(at);
   float lift=3*(pulse-1),tilt=(pulse-1)*.65f*Mathf.Deg2Rad;
   foreach(var uv in points){
    float x=uv.x,y=uv.y;
    // Only the lower jaw/chin moves; upper teeth, nose and moustache remain one painted unit.
    float jaw=Mathf.SmoothStep(0,1,Mathf.InverseLerp(580,705,y))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(910,1020,y)));
    float center=1-Mathf.SmoothStep(0,1,Mathf.Abs(x-627)/380);
    y-=JawOffset*jaw*center;
    float head=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(900,1120,y));
    float dx=x-627,dy=y-870;
    x+=(-dy*Mathf.Sin(tilt)+dx*(Mathf.Cos(tilt)-1))*head;
    y+=(dx*Mathf.Sin(tilt)+dy*(Mathf.Cos(tilt)-1)+lift)*head;
    var v=UIVertex.simpleVert;v.position=new Vector3(rect.center.x+(x/1254f-.5f)*size,rect.yMax-y/1254f*size,0);
    v.uv0=new Vector2(uv.x/1254f,1-uv.y/1254f);v.color=new Color(1,1,1,opacity);vh.AddVert(v);
   }
   for(int i=0;i<triangles.Count;i+=3)vh.AddTriangle(triangles[i],triangles[i+1],triangles[i+2]);
  }
  static void EnsureMesh(){
   if(points!=null)return;
   // Clockwise outline in original 1254px painting coordinates, including crown valleys and hair.
   points=new List<Vector2>{
    new Vector2(283,167),
    new Vector2(274,177),
    new Vector2(268,191),
    new Vector2(268,208),
    new Vector2(275,224),
    new Vector2(284,233),
    new Vector2(299,239),
    new Vector2(301,285),
    new Vector2(284,315),
    new Vector2(277,334),
    new Vector2(276,319),
    new Vector2(271,308),
    new Vector2(257,298),
    new Vector2(240,297),
    new Vector2(228,301),
    new Vector2(218,311),
    new Vector2(213,323),
    new Vector2(213,337),
    new Vector2(217,348),
    new Vector2(226,357),
    new Vector2(234,361),
    new Vector2(245,362),
    new Vector2(247,364),
    new Vector2(269,415),
    new Vector2(290,456),
    new Vector2(299,469),
    new Vector2(296,472),
    new Vector2(289,467),
    new Vector2(288,471),
    new Vector2(281,478),
    new Vector2(279,487),
    new Vector2(282,496),
    new Vector2(292,507),
    new Vector2(314,517),
    new Vector2(314,524),
    new Vector2(304,536),
    new Vector2(298,549),
    new Vector2(294,569),
    new Vector2(296,594),
    new Vector2(301,607),
    new Vector2(309,618),
    new Vector2(309,629),
    new Vector2(307,631),
    new Vector2(309,633),
    new Vector2(307,644),
    new Vector2(308,661),
    new Vector2(300,687),
    new Vector2(283,704),
    new Vector2(280,705),
    new Vector2(277,712),
    new Vector2(277,722),
    new Vector2(271,731),
    new Vector2(258,736),
    new Vector2(252,736),
    new Vector2(245,741),
    new Vector2(240,741),
    new Vector2(237,744),
    new Vector2(228,746),
    new Vector2(220,751),
    new Vector2(162,768),
    new Vector2(158,771),
    new Vector2(152,771),
    new Vector2(137,780),
    new Vector2(131,781),
    new Vector2(109,795),
    new Vector2(93,815),
    new Vector2(96,850),
    new Vector2(78,864),
    new Vector2(69,868),
    new Vector2(57,880),
    new Vector2(53,887),
    new Vector2(49,904),
    new Vector2(0,950),
    new Vector2(0,1253),
    new Vector2(1253,1253),
    new Vector2(1253,958),
    new Vector2(1225,926),
    new Vector2(1193,894),
    new Vector2(1194,873),
    new Vector2(1189,859),
    new Vector2(1177,847),
    new Vector2(1172,844),
    new Vector2(1165,843),
    new Vector2(1152,833),
    new Vector2(1132,825),
    new Vector2(1121,823),
    new Vector2(1118,820),
    new Vector2(1119,802),
    new Vector2(1107,790),
    new Vector2(1072,766),
    new Vector2(1030,746),
    new Vector2(1013,743),
    new Vector2(994,734),
    new Vector2(959,723),
    new Vector2(941,720),
    new Vector2(936,715),
    new Vector2(936,709),
    new Vector2(942,698),
    new Vector2(942,688),
    new Vector2(926,673),
    new Vector2(933,661),
    new Vector2(934,653),
    new Vector2(936,651),
    new Vector2(935,619),
    new Vector2(930,610),
    new Vector2(930,587),
    new Vector2(926,578),
    new Vector2(920,571),
    new Vector2(920,563),
    new Vector2(914,562),
    new Vector2(903,551),
    new Vector2(903,542),
    new Vector2(897,537),
    new Vector2(899,512),
    new Vector2(892,481),
    new Vector2(902,470),
    new Vector2(906,459),
    new Vector2(904,432),
    new Vector2(899,424),
    new Vector2(888,415),
    new Vector2(887,396),
    new Vector2(875,383),
    new Vector2(875,378),
    new Vector2(885,350),
    new Vector2(897,306),
    new Vector2(899,304),
    new Vector2(905,304),
    new Vector2(912,301),
    new Vector2(922,291),
    new Vector2(927,280),
    new Vector2(927,263),
    new Vector2(923,254),
    new Vector2(906,241),
    new Vector2(890,241),
    new Vector2(877,246),
    new Vector2(870,253),
    new Vector2(864,265),
    new Vector2(850,243),
    new Vector2(843,176),
    new Vector2(852,172),
    new Vector2(863,161),
    new Vector2(867,153),
    new Vector2(870,142),
    new Vector2(870,133),
    new Vector2(864,116),
    new Vector2(849,102),
    new Vector2(841,99),
    new Vector2(821,99),
    new Vector2(804,106),
    new Vector2(794,116),
    new Vector2(788,129),
    new Vector2(788,148),
    new Vector2(793,160),
    new Vector2(798,166),
    new Vector2(788,178),
    new Vector2(771,161),
    new Vector2(777,168),
    new Vector2(770,172),
    new Vector2(756,169),
    new Vector2(752,165),
    new Vector2(744,165),
    new Vector2(738,160),
    new Vector2(728,157),
    new Vector2(719,149),
    new Vector2(706,147),
    new Vector2(702,141),
    new Vector2(692,141),
    new Vector2(668,126),
    new Vector2(652,125),
    new Vector2(639,131),
    new Vector2(634,130),
    new Vector2(628,134),
    new Vector2(584,88),
    new Vector2(593,67),
    new Vector2(593,49),
    new Vector2(583,28),
    new Vector2(574,20),
    new Vector2(556,12),
    new Vector2(540,12),
    new Vector2(527,15),
    new Vector2(511,23),
    new Vector2(498,38),
    new Vector2(492,53),
    new Vector2(492,75),
    new Vector2(500,92),
    new Vector2(517,107),
    new Vector2(468,125),
    new Vector2(421,152),
    new Vector2(399,169),
    new Vector2(389,179),
    new Vector2(389,182),
    new Vector2(377,197),
    new Vector2(376,202),
    new Vector2(370,207),
    new Vector2(366,203),
    new Vector2(349,224),
    new Vector2(342,218),
    new Vector2(348,204),
    new Vector2(348,186),
    new Vector2(342,173),
    new Vector2(334,165),
    new Vector2(322,159),
    new Vector2(306,158),
    new Vector2(295,161),
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
   for(int step=0;step<3;step++){
    var next=new List<int>();var edges=new Dictionary<ulong,int>();
    int Mid(int a,int b){ulong key=((ulong)Mathf.Min(a,b)<<32)|(uint)Mathf.Max(a,b);if(edges.TryGetValue(key,out int found))return found;int n=points.Count;points.Add((points[a]+points[b])*.5f);edges[key]=n;return n;}
    for(int i=0;i<triangles.Count;i+=3){int a=triangles[i],b=triangles[i+1],c=triangles[i+2],ab=Mid(a,b),bc=Mid(b,c),ca=Mid(c,a);next.AddRange(new[]{a,ab,ca,ab,b,bc,ca,bc,c,ab,bc,ca});}triangles=next;
   }
  }
  static float Cross(Vector2 a,Vector2 b,Vector2 c)=>(b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x);
  static bool Inside(Vector2 p,Vector2 a,Vector2 b,Vector2 c,float d)=>Cross(a,b,p)*d>=0&&Cross(b,c,p)*d>=0&&Cross(c,a,p)*d>=0;
 }
}
