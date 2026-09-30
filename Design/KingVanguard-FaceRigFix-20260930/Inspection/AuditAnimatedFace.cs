using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using Spine;
using RoyalThrone.Characters;
public class AuditAnimatedFace {
 class Loader:TextureLoader { public void Load(AtlasPage p,string n){} public void Unload(object o){} }
 public class Result { public string rig,animation,attachment; public int samples; public double maximumVertexErrorSourceUnits,maximumCentroidErrorSourceUnits; public float vertexPeakTime,centroidPeakTime; public double centroidDxAtPeak,centroidDyAtPeak; }
 static SkeletonData Read(string path){return new SkeletonBinary(new Atlas(path+".atlas",new Loader())){Scale=.01f}.ReadSkeletonData(path+".skel");}
 static float[] World(Attachment a,Slot s){var m=a as VertexAttachment;if(m!=null){var v=new float[m.WorldVerticesLength];m.ComputeWorldVertices(s,v);return v;}var r=a as RegionAttachment;if(r!=null){var v=new float[8];r.ComputeWorldVertices(s.Bone,v,0);return v;}return null;}
 static float[] Center(Skeleton s,string name){int si=s.Data.FindSlotIndex(name);var a=s.Data.DefaultSkin.GetAttachment(si,name);var v=World(a,s.Slots.Items[si]);return new[]{Enumerable.Range(0,v.Length/2).Average(i=>v[2*i]),Enumerable.Range(0,v.Length/2).Average(i=>v[2*i+1])};}
 static void Map(Skeleton s,float[] v,string name){var f=s.FindBone("on_yuz");var h=s.FindBone("yuz");float hx,hy;f.WorldToLocal(h.WorldX,h.WorldY,out hx,out hy);for(int i=0;i<v.Length;i+=2){float x,y;f.WorldToLocal(v[i],v[i+1],out x,out y);f.LocalToWorld(hx+(x-hx)*1.15f,hy+(y-hy)*.92f,out v[i],out v[i+1]);}
 float sx=1,sy=1;string pivot=name;if(name=="Ağız"||name=="Alt Diş"||name=="Ust Diş"||name=="Dil"){sx=sy=.82f;pivot="Ağız";}else if(name=="Burun"){sx=1.05f;sy=.85f;}else if(name=="Sağ Göz Bebeği"||name=="Sol  Göz Bebeği")sx=sy=1.1f;
 if(sx!=1||sy!=1){var center=Center(s,pivot);Map(s,center,"");float cx,cy;f.WorldToLocal(center[0],center[1],out cx,out cy);for(int i=0;i<v.Length;i+=2){float x,y;f.WorldToLocal(v[i],v[i+1],out x,out y);f.LocalToWorld(cx+(x-cx)*sx,cy+(y-cy)*sy,out v[i],out v[i+1]);}}}
 public static int Main(string[] args){var records=new List<Result>();string[] names={"Sakal Surat","Bıyık","Bıyık Gölge","Ağız","Alt Diş","Ust Diş","Dil","Sağ Kaş","Sol Kaş","Sağ Kaş Gölge","Sol Kaş Gölge","Sağ Göz","Sol Göz","Sağ Göz Bebeği","Sol  Göz Bebeği","Sağ Göz Kapağı","Sol Göz kapağı","Burun","Taç"};
 foreach(string rig in new[]{"King","AreaCompleted"}){var original=Read(Path.Combine(args[0],rig));var modified=Read(Path.Combine(args[0],rig));new KingVanguardFaceShapeModifier().Apply(modified);var s=new Skeleton(original);var m=new Skeleton(modified);
 foreach(var anim in original.Animations){var modifiedAnim=modified.FindAnimation(anim.Name);var sums=names.ToDictionary(n=>n,n=>new Result{rig=rig,animation=anim.Name,attachment=n});int steps=(int)Math.Ceiling(anim.Duration*60);
 for(int i=0;i<=steps;i++){float time=Math.Min(anim.Duration,i/60f);s.SetToSetupPose();m.SetToSetupPose();anim.Apply(s,0,time,false,null,1,MixBlend.Replace,MixDirection.In);modifiedAnim.Apply(m,0,time,false,null,1,MixBlend.Replace,MixDirection.In);s.UpdateWorldTransform();m.UpdateWorldTransform();
 foreach(string name in names){var ss=s.FindSlot(name);var ms=m.FindSlot(name);if(ss==null||ss.Attachment==null||ms.Attachment==null||ss.A<.01f)continue;var expected=World(ss.Attachment,ss);var actual=World(ms.Attachment,ms);if(expected==null||actual==null)continue;Map(s,expected,name);var r=sums[name];r.samples++;double cx=0,cy=0;for(int j=0;j<actual.Length;j+=2){double dx=actual[j]-expected[j],dy=actual[j+1]-expected[j+1];cx+=dx;cy+=dy;double error=100*Math.Sqrt(dx*dx+dy*dy);if(error>r.maximumVertexErrorSourceUnits){r.maximumVertexErrorSourceUnits=error;r.vertexPeakTime=time;}}cx*=200.0/actual.Length;cy*=200.0/actual.Length;double centroid=Math.Sqrt(cx*cx+cy*cy);if(centroid>r.maximumCentroidErrorSourceUnits){r.maximumCentroidErrorSourceUnits=centroid;r.centroidPeakTime=time;r.centroidDxAtPeak=cx;r.centroidDyAtPeak=cy;}}
 }
 records.AddRange(sums.Values);
 }
 }
 var report=new {method="60 Hz comparison of current bind-space modifier versus coherent affine applied AFTER original animation skinning in current on_yuz/yuz space; mouth group pivot is current original mouth centroid. Values measure transform coherence, not aesthetic ground truth or pixel alpha.",sourceScale=.01,pupilScale=1.1,records=records};
 File.WriteAllText(args[1],new JavaScriptSerializer{MaxJsonLength=int.MaxValue}.Serialize(report),new System.Text.UTF8Encoding(false));
 foreach(var g in records.GroupBy(r=>r.rig+" / "+r.attachment)){var max=g.OrderByDescending(r=>r.maximumVertexErrorSourceUnits).First();Console.WriteLine(g.Key+": vertex drift "+max.maximumVertexErrorSourceUnits.ToString("F3")+" @ "+max.animation+" "+max.vertexPeakTime.ToString("F3")+"s; centroid "+max.maximumCentroidErrorSourceUnits.ToString("F3"));}return 0;
 }
}
