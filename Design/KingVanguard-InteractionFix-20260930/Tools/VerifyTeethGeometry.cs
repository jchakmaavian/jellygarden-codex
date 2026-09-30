// Unity eval_file: isolated preview actors, no saved project/game state writes.
// Exercise the ACTUAL post-skin renderer, not a second implementation of its fitting.
string output="Design/KingVanguard-InteractionFix-20260930/Validation";
System.IO.Directory.CreateDirectory(output);
var report=new System.Text.StringBuilder();
var trace=new System.Text.StringBuilder("rig,stage,time,row,centroidErrorSourceUnits,uniformScaleSpread,maximumCorrectionSourceUnits\n");
var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
int frames=0,rowChecks=0,mixingFrames=0,clipCount=0;
float worstCentroid=0,worstScaleSpread=0;
string stage="setup";float sampleTime=0;
void Require(bool condition,string message){if(!condition)throw new System.Exception(stage+": "+message);}
var actors=new System.Collections.Generic.List<Spine.Unity.SkeletonAnimation>();
var hashes=new System.Collections.Generic.Dictionary<Spine.SkeletonData,string>();
var rows=new[]{"Alt Diş","Ust Diş"};
UnityEngine.Vector2[] Local(Spine.Skeleton skeleton,Spine.MeshAttachment attachment,Spine.Slot slot)
{
 var world=new float[attachment.WorldVerticesLength];attachment.ComputeWorldVertices(slot,world);
 var result=new UnityEngine.Vector2[world.Length/2];var face=skeleton.FindBone("on_yuz");
 for(int i=0;i<result.Length;i++){float x,y;face.WorldToLocal(world[i*2],world[i*2+1],out x,out y);result[i]=new UnityEngine.Vector2(x,y);}return result;
}
UnityEngine.Vector2 Center(UnityEngine.Vector2[] points){var center=UnityEngine.Vector2.zero;foreach(var p in points)center+=p;return center/points.Length;}
float Area(UnityEngine.Vector2 a,UnityEngine.Vector2 b,UnityEngine.Vector2 c){return (b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x);}
string Fingerprint(Spine.SkeletonData data)
{
 using(var memory=new System.IO.MemoryStream())using(var writer=new System.IO.BinaryWriter(memory))
 {
  void ArrayData(System.Array array)
  {
   if(array==null){writer.Write(-1);return;}writer.Write(array.Length);
   foreach(var value in array)
   {
    if(value is System.Array nested)ArrayData(nested);
    else if(value is float f)writer.Write(f);
    else if(value is int i)writer.Write(i);
    else if(value is string s)writer.Write(s??"");
   }
  }
  foreach(var bone in data.Bones){writer.Write(bone.Name);writer.Write(bone.X);writer.Write(bone.Y);writer.Write(bone.Rotation);writer.Write(bone.ScaleX);writer.Write(bone.ScaleY);writer.Write(bone.ShearX);writer.Write(bone.ShearY);}
  foreach(var skin in data.Skins)foreach(var entry in skin.GetAttachments())
  {
   writer.Write(entry.SlotIndex);writer.Write(entry.Name);
   var vertex=entry.Attachment as Spine.VertexAttachment;
   if(vertex!=null){ArrayData(vertex.Vertices);ArrayData(vertex.Bones);}
   var mesh=entry.Attachment as Spine.MeshAttachment;
   if(mesh!=null){ArrayData(mesh.UVs);ArrayData(mesh.RegionUVs);ArrayData(mesh.Triangles);}
   var region=entry.Attachment as Spine.RegionAttachment;
   if(region!=null){ArrayData(region.UVs);ArrayData(region.Offset);writer.Write(region.X);writer.Write(region.Y);writer.Write(region.Rotation);writer.Write(region.ScaleX);writer.Write(region.ScaleY);}
  }
  // Include private backing arrays in each timeline/base class: FFD keys, Bezier data,
  // numeric timelines, attachment names and draw-order arrays, without touching them.
  foreach(var clip in data.Animations)
  {
   writer.Write(clip.Name);writer.Write(clip.Duration);
   foreach(var timeline in clip.Timelines)
    for(var type=timeline.GetType();type!=null&&type!=typeof(object);type=type.BaseType)
     foreach(var field in type.GetFields(System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.DeclaredOnly))
      if(field.FieldType.IsArray){writer.Write(type.FullName+"."+field.Name);ArrayData(field.GetValue(timeline) as System.Array);}
  }
  writer.Flush();using(var sha=System.Security.Cryptography.SHA256.Create())return System.BitConverter.ToString(sha.ComputeHash(memory.ToArray())).Replace("-","");
 }
}

try
{
 foreach(string name in new[]{"King","AreaCompleted"})
 {
  var actor=new UnityEngine.GameObject("Tooth geometry QA "+name);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(actor,scene);
  var rig=actor.AddComponent<Spine.Unity.SkeletonAnimation>();actors.Add(rig);
  rig.skeletonDataAsset=UnityEditor.AssetDatabase.LoadAssetAtPath<Spine.Unity.SkeletonDataAsset>("Assets/Characters/KingVenguard/V2/Spine/"+name+"_Vanguard_SkeletonData.asset");
  rig.Initialize(false);rig.enabled=false;var data=rig.Skeleton.Data;hashes[data]=Fingerprint(data);
  float units=rig.skeletonDataAsset.scale;
  var setup=new Spine.Skeleton(data);setup.SetToSetupPose();setup.UpdateWorldTransform();
  var rest=new System.Collections.Generic.Dictionary<string,UnityEngine.Vector2[]>();
  var raw=new System.Collections.Generic.Dictionary<string,UnityEngine.Vector2[]>();
  var indices=new System.Collections.Generic.Dictionary<string,int[]>();
  foreach(string row in rows){var slot=setup.FindSlot(row);rest[row]=Local(setup,(Spine.MeshAttachment)slot.Attachment,slot);}
  // Record independently attributable source vertices BEFORE the presentation callback.
  rig.OnPostProcessVertices+=buffers=>{
   foreach(string row in rows)
   {
    var slot=rig.Skeleton.FindSlot(row);var mesh=slot.Attachment as Spine.MeshAttachment;
    if(mesh==null){raw.Remove(row);indices.Remove(row);continue;}
    raw[row]=Local(rig.Skeleton,mesh,slot);
    var world=new float[mesh.WorldVerticesLength];mesh.ComputeWorldVertices(slot,world);
    var found=new int[world.Length/2];
    for(int p=0;p<found.Length;p++)
    {
     found[p]=-1;
     for(int v=0;v<buffers.vertexCount;v++)
      if(buffers.uvBuffer[v].x==mesh.UVs[p*2]&&buffers.uvBuffer[v].y==mesh.UVs[p*2+1]&&
       UnityEngine.Mathf.Abs(buffers.vertexBuffer[v].x-world[p*2])<1e-5f&&UnityEngine.Mathf.Abs(buffers.vertexBuffer[v].y-world[p*2+1])<1e-5f){found[p]=v;break;}
     Require(found[p]>=0,name+" missing emitted original tooth vertex "+row+"/"+p);
    }
    indices[row]=found;
   }
  };
  var mapper=RoyalThrone.Characters.KingVanguardFaceRig.Ensure(rig);mapper.StabilizeTeeth=true;
  rig.OnPostProcessVertices+=buffers=>{
   Require(RoyalThrone.Characters.KingVanguardFaceShapeModifier.TryGet(data,out var shape),"missing face configuration");
   Require(UnityEngine.Mathf.Abs(shape.MouthScale-1)<1e-6f,"fixture expects the approved mouthScale=1");
   var face=rig.Skeleton.FindBone("on_yuz");var head=rig.Skeleton.FindBone("yuz");float hx,hy;face.WorldToLocal(head.WorldX,head.WorldY,out hx,out hy);
   foreach(string row in rows)
   {
    if(!indices.TryGetValue(row,out var ids))continue;
    var current=new UnityEngine.Vector2[ids.Length];
    for(int i=0;i<ids.Length;i++)
    {
     var point=buffers.vertexBuffer[ids[i]];float x,y;face.WorldToLocal(point.x,point.y,out x,out y);
     current[i]=new UnityEngine.Vector2(hx+(x-hx)/shape.FaceScale.x,hy+(y-hy)/shape.FaceScale.y);
    }
    float centroid=(Center(current)-Center(raw[row])).magnitude/units;
    float min=float.MaxValue,max=0;
    for(int i=0;i<current.Length;i++)for(int j=i+1;j<current.Length;j++)
    {
     float original=(rest[row][j]-rest[row][i]).magnitude;if(original<units*.001f)continue;
     float ratio=(current[j]-current[i]).magnitude/original;min=UnityEngine.Mathf.Min(min,ratio);max=UnityEngine.Mathf.Max(max,ratio);
    }
    float spread=max-min;
    Require(centroid<.025f,name+" "+row+" changed source animated row centroid by "+centroid+" source units");
    Require(spread<.002f,name+" "+row+" bent/sheared instead of one uniform similarity: "+spread);
    var mesh=(Spine.MeshAttachment)rig.Skeleton.FindSlot(row).Attachment;
    for(int i=0;i<mesh.Triangles.Length;i+=3)
    {
     int a=mesh.Triangles[i],b=mesh.Triangles[i+1],c=mesh.Triangles[i+2];float original=Area(rest[row][a],rest[row][b],rest[row][c]);
     if(UnityEngine.Mathf.Abs(original)>units*units*.001f)Require(Area(current[a],current[b],current[c])*original>0,name+" tooth triangle inverted");
    }
    worstCentroid=UnityEngine.Mathf.Max(worstCentroid,centroid);worstScaleSpread=UnityEngine.Mathf.Max(worstScaleSpread,spread);rowChecks++;
    trace.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,"{0},{1},{2:F6},{3},{4:F8},{5:F8},{6:F8}",name,stage,sampleTime,row,centroid,spread,mapper.MaximumToothCorrection/units));
   }
  };
  foreach(var clip in data.Animations)
  {
   stage="clip "+clip.Name;clipCount++;int steps=UnityEngine.Mathf.CeilToInt(clip.Duration*60);
   for(int i=0;i<=steps;i++)
   {
    sampleTime=UnityEngine.Mathf.Min(clip.Duration,i/60f);rig.Skeleton.SetToSetupPose();
    clip.Apply(rig.Skeleton,0,sampleTime,false,null,1,Spine.MixBlend.Replace,Spine.MixDirection.In);
    rig.Skeleton.UpdateWorldTransform();rig.LateUpdate();frames++;
    Require(mapper.LastMappedVertices>0&&mapper.LastAmbiguousVertices==0,"missing or ambiguous face attribution");
   }
  }
  // Isolated AnimationStateData: never alter a shared asset's mix configuration.
  var state=new Spine.AnimationState(new Spine.AnimationStateData(data){DefaultMix=.25f});
  string[] sequence=name=="King"?new[]{"idle_loop_1","k-1_kaygi","k-1_kaygi_loop","k-1sevinme_6","k-1_uzulme","idle_loop_2"}:
   new[]{"king_drill_loop","king_drill_win_clap_start","king_drill_win_clap_loop","king_drill_win_clap_finish","king_drill_loop"};
  rig.Skeleton.SetToSetupPose();
  foreach(string clip in sequence)
  {
   stage="mix to "+clip;state.SetAnimation(0,clip,true);
   for(int i=0;i<60;i++)
   {
    sampleTime=i/60f;state.Update(1f/60);state.Apply(rig.Skeleton);rig.Skeleton.UpdateWorldTransform();rig.LateUpdate();frames++;
    if(state.GetCurrent(0).MixingFrom!=null)mixingFrames++;
   }
  }
  Require(Fingerprint(data)==hashes[data],name+" source geometry/weights/deforms/timelines changed");
  report.AppendLine("PASS "+name+": all "+data.Animations.Count+" tracks at 60Hz, row centroid preserved, every pairwise edge uses one scale, no triangle inversion, mixed transitions coherent, source fingerprint unchanged.");
 }
 Require(clipCount==42,"expected all 42 source tracks");Require(mixingFrames>80,"insufficient real AnimationState crossfade coverage");
 report.AppendLine("PASS "+frames+" rendered poses, "+rowChecks+" tooth-row checks, "+mixingFrames+" mixed frames; max centroid error "+worstCentroid+" source units; max scale spread "+worstScaleSpread+".");
 report.AppendLine("Lip-mask occlusion remains a separate visual/pixel test; this fixture proves actual emitted geometry and source preservation.");
}
catch(System.Exception error){report.AppendLine("FAIL "+error);throw;}
finally
{
 UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
 System.IO.File.WriteAllText(output+"/teeth-geometry-checks.txt",report.ToString());System.IO.File.WriteAllText(output+"/teeth-geometry-60hz.csv",trace.ToString());
}
return report.ToString();
