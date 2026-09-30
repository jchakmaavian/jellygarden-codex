// Standalone verification with the project's official spine-csharp runtime.
// Unity stubs instantiate the modifier only; no Unity/editor process is launched.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Spine;
using RoyalThrone.Characters;
namespace UnityEngine {
    public class ScriptableObject { }
    public class SerializeField : Attribute { }
    public class CreateAssetMenuAttribute : Attribute { public string menuName; }
    public struct Vector2 { public float x,y; public Vector2(float x,float y){this.x=x;this.y=y;} }
}
namespace Spine.Unity { public abstract class SkeletonDataModifierAsset : UnityEngine.ScriptableObject { public abstract void Apply(SkeletonData data); } }
class VerifyFaceShapeOffline {
    class Loader : TextureLoader { public void Load(AtlasPage page,string path){} public void Unload(object o){} }
    static float error;
    static int points,frames;
    static Dictionary<string,float[]> centers;
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static SkeletonData Read(string stem){return new SkeletonBinary(new Atlas(stem+".atlas",new Loader())){Scale=.01f}.ReadSkeletonData(stem+".skel");}
    static Skeleton Setup(SkeletonData data){var s=new Skeleton(data);s.SetToSetupPose();s.UpdateWorldTransform();return s;}
    static float[] World(Attachment a,Slot slot){var v=a as VertexAttachment;if(v!=null){var w=new float[v.WorldVerticesLength];v.ComputeWorldVertices(slot,w);return w;}var r=a as RegionAttachment;if(r!=null){var w=new float[8];r.ComputeWorldVertices(slot.Bone,w,0);return w;}return null;}
    static void Map(Skeleton setup,float[] vertices,string feature){
        var head=setup.FindBone("yuz");var face=setup.FindBone("on_yuz");float hx,hy;
        face.WorldToLocal(head.WorldX,head.WorldY,out hx,out hy);
        for(int i=0;i<vertices.Length;i+=2){float x,y;face.WorldToLocal(vertices[i],vertices[i+1],out x,out y);x=hx+(x-hx)*1.15f;y=hy+(y-hy)*.92f;face.LocalToWorld(x,y,out vertices[i],out vertices[i+1]);}
        float sx=1,sy=1;string pivot=feature;
        if(feature=="Burun"){sx=1.05f;sy=.85f;}
        else if(feature=="Sağ Göz Bebeği"||feature=="Sol  Göz Bebeği")sx=sy=1.55f;
        else if(feature=="Ağız"||feature=="Alt Diş"||feature=="Ust Diş"||feature=="Dil"){sx=sy=.82f;pivot="Ağız";}
        if(sx!=1||sy!=1){var center=(float[])centers[pivot].Clone();Map(setup,center,null);float cx,cy;face.WorldToLocal(center[0],center[1],out cx,out cy);for(int i=0;i<vertices.Length;i+=2){float x,y;face.WorldToLocal(vertices[i],vertices[i+1],out x,out y);face.LocalToWorld(cx+(x-cx)*sx,cy+(y-cy)*sy,out vertices[i],out vertices[i+1]);}}
    }
    static void Compare(float[] a,float[] b,string label){Check(a.Length==b.Length,label+" count");for(int i=0;i<a.Length;i++){float e=Math.Abs(a[i]-b[i]);error=Math.Max(error,e);Check(!float.IsNaN(b[i])&&!float.IsInfinity(b[i])&&e<.00003f,label+" at "+i+": "+a[i]+" / "+b[i]);}points+=a.Length/2;}
    static void AssignDeform(Slot slot,VertexAttachment attachment,float[] vertices){slot.Attachment=attachment;slot.Deform.Clear();foreach(float f in vertices)slot.Deform.Add(f);}
    public static int Main(string[] args){try{
        foreach(string name in new[]{"King","AreaCompleted"}){
            error=0;points=0;frames=0;
            string stem=Path.Combine(args[0],name);var original=Read(stem);var changed=Read(stem);var before=Setup(original);
            centers=new Dictionary<string,float[]>();foreach(var entry in original.DefaultSkin.GetAttachments()){var w=World(entry.Attachment,before.Slots.Items[entry.SlotIndex]);if(w!=null){float x=0,y=0;for(int i=0;i<w.Length;i+=2){x+=w[i];y+=w[i+1];}centers[entry.Name]=new[]{x*2/w.Length,y*2/w.Length};}}
            var modifier=new KingVanguardFaceShapeModifier();modifier.Apply(changed);var after=Setup(changed);
            var affected=new HashSet<string>();int setupAttachments=0;
            foreach(var e in original.DefaultSkin.GetAttachments()){
                var a=e.Attachment;var b=changed.DefaultSkin.GetAttachment(e.SlotIndex,e.Name);var wa=World(a,before.Slots.Items[e.SlotIndex]);var wb=World(b,after.Slots.Items[e.SlotIndex]);if(wa==null)continue;
                bool diff=wa.Where((v,i)=>Math.Abs(v-wb[i])>.000001f).Any();
                if(diff){affected.Add(e.Name);Map(before,wa,e.Name);setupAttachments++;}
                Compare(wa,wb,e.Name+" setup");
                var va=a as VertexAttachment;var vb=b as VertexAttachment;
                if(va!=null&&va.Bones!=null){Check(va.Bones.SequenceEqual(vb.Bones),e.Name+" bone indices");for(int i=2;i<va.Vertices.Length;i+=3)Check(va.Vertices[i]==vb.Vertices[i],e.Name+" weights");}
                var ma=a as MeshAttachment;var mb=b as MeshAttachment;if(ma!=null){Check(ma.Triangles.SequenceEqual(mb.Triangles),e.Name+" topology");Check(ma.RegionUVs.SequenceEqual(mb.RegionUVs),e.Name+" UVs");}
            }
            int timelineCount=0;
            for(int ai=0;ai<original.Animations.Count;ai++){
                var oa=original.Animations.Items[ai];var ca=changed.Animations.Items[ai];Check(oa.Name==ca.Name&&oa.Duration==ca.Duration&&oa.Timelines.Count==ca.Timelines.Count,"Animation identity");
                for(int ti=0;ti<oa.Timelines.Count;ti++){
                    var ot=oa.Timelines.Items[ti];var ct=ca.Timelines.Items[ti];Check(ot.GetType()==ct.GetType(),"Timeline type");
                    var timing=ot.GetType().GetProperty("Frames");if(timing!=null){var ov=timing.GetValue(ot,null) as float[];var cv=timing.GetValue(ct,null) as float[];if(ov!=null)Check(ov.SequenceEqual(cv),"Nondeform/time key data changed: "+oa.Name);}
                    var od=ot as DeformTimeline;var cd=ct as DeformTimeline;if(od==null)continue;
                    bool isAffected=affected.Contains(od.Attachment.Name);if(isAffected)timelineCount++;
                    for(int fi=0;fi<od.Vertices.Length;fi++){
                        var os=before.Slots.Items[od.SlotIndex];var cs=after.Slots.Items[cd.SlotIndex];AssignDeform(os,od.Attachment,od.Vertices[fi]);AssignDeform(cs,cd.Attachment,cd.Vertices[fi]);
                        var w=World(od.Attachment,os);var got=World(cd.Attachment,cs);if(isAffected){Map(before,w,od.Attachment.Name);frames++;}Compare(w,got,oa.Name+"/"+od.Attachment.Name+" deform "+fi);os.Deform.Clear();cs.Deform.Clear();
                    }
                }
                // Every track also executes under the modified skeleton's complete
                // original bone animation. Check finite geometry across its duration.
                for(int sample=0;sample<=10;sample++){
                    after.SetToSetupPose();ca.Apply(after,0,ca.Duration*sample/10f,false,null,1,MixBlend.Replace,MixDirection.In);after.UpdateWorldTransform();
                    foreach(var s in after.Slots){if(s.Attachment==null)continue;var w=World(s.Attachment,s);if(w!=null)Check(w.All(v=>!float.IsNaN(v)&&!float.IsInfinity(v)),"Nonfinite animated geometry: "+ca.Name);if(s.Attachment.Name=="Taç Gölge Ust")Check(((RegionAttachment)s.Attachment).A==0,"Upper crown shadow became visible: "+ca.Name);}
                }
                // Bind-pose matrices are needed by the next independent key check.
                after.SetToSetupPose();after.UpdateWorldTransform();
            }
            var checkBefore=Setup(changed);var snapshots=new Dictionary<string,float[]>();foreach(var e in changed.DefaultSkin.GetAttachments()){var w=World(e.Attachment,checkBefore.Slots.Items[e.SlotIndex]);if(w!=null)snapshots[e.SlotIndex+"/"+e.Name]=w;}
            modifier.Apply(changed);var checkAfter=Setup(changed);foreach(var e in changed.DefaultSkin.GetAttachments()){float[] w;if(snapshots.TryGetValue(e.SlotIndex+"/"+e.Name,out w))Compare(w,World(e.Attachment,checkAfter.Slots.Items[e.SlotIndex]),"Idempotence "+e.Name);}
            Console.WriteLine(name+": PASS; "+setupAttachments+" reshaped attachments, "+timelineCount+" deform timelines, "+frames+" deform keys, "+points+" compared vertex points, max error "+error+" Unity units; "+original.Animations.Count+" animations sampled at 11 times. Weights/topology/UVs/key times unchanged; repeated apply stable.");
            foreach(var entry in changed.DefaultSkin.GetAttachments())if(entry.Name.StartsWith("Taç")){var w=World(entry.Attachment,checkAfter.Slots.Items[entry.SlotIndex]);float xmin=float.MaxValue,xmax=float.MinValue,ymin=float.MaxValue,ymax=float.MinValue;for(int i=0;i<w.Length;i+=2){xmin=Math.Min(xmin,w[i]);xmax=Math.Max(xmax,w[i]);ymin=Math.Min(ymin,w[i+1]);ymax=Math.Max(ymax,w[i+1]);}Console.WriteLine("  Crown bounds "+entry.Name+": x="+xmin+".."+xmax+", y="+ymin+".."+ymax);}
        }
        return 0;
    }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}}
}
