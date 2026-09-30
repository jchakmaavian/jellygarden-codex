// Unity eval_file fixture. Author only; execute after the runtime has recompiled.
// Uses the current imported King/Area data and atlas materials, in a private preview scene.
// No scene/save/prefab/source mutation. This verifies mesh invariants, not visual appeal.
string output="Design/KingVanguard-FaceRigFix-20260930/Validation";
System.IO.Directory.CreateDirectory(output);
var report=new System.Text.StringBuilder("RUNNING: original AnimationState at 60 Hz, current post-skin face rig.\n");
var trace=new System.Text.StringBuilder("actor,stage,frame,clip,trackTime,mixingFrom,mixTime,mapped,interpolated,ambiguous,unattributed,visibleFaceNames,vertices,boundsWidth,boundsHeight\n");
var culture=System.Globalization.CultureInfo.InvariantCulture;
var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
var actors=new System.Collections.Generic.List<Spine.Unity.SkeletonAnimation>();
var mappers=new System.Collections.Generic.Dictionary<Spine.Unity.SkeletonAnimation,RoyalThrone.Characters.KingVanguardFaceRig>();
var captures=new System.Collections.Generic.Dictionary<Spine.Unity.SkeletonAnimation,(UnityEngine.Vector3[] raw,UnityEngine.Vector2[] uv,bool[] face,System.Collections.Generic.HashSet<string> names,float[] pose)>();
var coverage=new System.Collections.Generic.Dictionary<Spine.Unity.SkeletonAnimation,System.Collections.Generic.HashSet<string>>();
var faceNames=new System.Collections.Generic.HashSet<string>(new[]{
 "Alt Diş","Ağız","Burun","Bıyık","Bıyık Gölge","Dil","Sakal Gölge","Sakal Surat",
 "Sağ Göz","Sağ Göz Bebeği","Sağ Göz Kapağı","Sağ Kaş","Sağ Kaş Gölge","Sağ Kirpik","Sağ Kulak","Sağ Yanak",
 "Sol  Göz Bebeği","Sol Göz","Sol Göz kapağı","Sol Kaş","Sol Kaş Gölge","Sol Kirpik","Sol Kulak","Sol Yanak","Taç","Taç Alt Gölge","Taç Gölge Ust","Ust Diş"},System.StringComparer.Ordinal);
Spine.Unity.SkeletonAnimation injectActor=null;int injectedIndex=-1,totalFrames=0,mixingFrames=0;string stage="setup";
bool Finite(float x){return !float.IsNaN(x)&&!float.IsInfinity(x);}
void Require(bool value,string text){if(!value)throw new System.Exception(stage+": "+text);}
void Pass(string text){report.AppendLine("PASS "+text);}
float[] Pose(Spine.Skeleton skeleton)
{
 var list=new System.Collections.Generic.List<float>();
 foreach(var b in skeleton.Bones){list.AddRange(new[]{b.X,b.Y,b.Rotation,b.ScaleX,b.ScaleY,b.ShearX,b.ShearY,b.A,b.B,b.C,b.D,b.WorldX,b.WorldY});}
 foreach(var s in skeleton.Slots){list.Add(s.Deform.Count);for(int i=0;i<s.Deform.Count;i++)list.Add(s.Deform.Items[i]);}
 return list.ToArray();
}
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
void Capture(Spine.Unity.SkeletonAnimation rig,Spine.Unity.MeshGeneratorBuffers buffers)
{
 // Independent attribution uses each active attachment's actual atlas UV + its
 // original skinned position. Packed rectangle overlap cannot mark body as face.
 var owners=new System.Collections.Generic.Dictionary<UnityEngine.Vector2,System.Collections.Generic.List<(UnityEngine.Vector2 position,string name,bool face)>>();
 var sourceTriangles=new System.Collections.Generic.List<(float[] world,float[] uv,int[] triangles,string name)>();
 foreach(var slot in rig.Skeleton.Slots)
 {
  var attachment=slot.Attachment;var region=attachment as Spine.RegionAttachment;var mesh=attachment as Spine.MeshAttachment;
  if(!slot.Bone.Active||(region==null&&mesh==null))continue;
  var world=new float[region!=null?8:mesh.WorldVerticesLength];var uv=region!=null?region.UVs:mesh.UVs;
  if(region!=null)region.ComputeWorldVertices(slot.Bone,world,0);else mesh.ComputeWorldVertices(slot,world);
  bool facial=faceNames.Contains(attachment.Name);
  for(int i=0;i<uv.Length;i+=2)
  {
   var key=new UnityEngine.Vector2(uv[i],uv[i+1]);
   if(!owners.TryGetValue(key,out var list)){list=new System.Collections.Generic.List<(UnityEngine.Vector2,string,bool)>();owners.Add(key,list);}
   list.Add((new UnityEngine.Vector2(world[i],world[i+1]),attachment.Name,facial));
  }
  if(facial&&attachment.Name=="Sakal Surat")sourceTriangles.Add((world,uv,region!=null?new[]{0,1,2,2,3,0}:mesh.Triangles,attachment.Name));
 }
 var raw=new UnityEngine.Vector3[buffers.vertexCount];var uvs=new UnityEngine.Vector2[buffers.vertexCount];
 var isFace=new bool[buffers.vertexCount];var names=new System.Collections.Generic.HashSet<string>();
 for(int i=0;i<buffers.vertexCount;i++)
 {
  raw[i]=buffers.vertexBuffer[i];uvs[i]=buffers.uvBuffer[i];
  if(owners.TryGetValue(uvs[i],out var list))foreach(var owner in list)
   if(owner.face&&UnityEngine.Vector2.SqrMagnitude(owner.position-new UnityEngine.Vector2(raw[i].x,raw[i].y))<1e-9f)
   {isFace[i]=true;names.Add(owner.name);coverage[rig].Add(owner.name);}
 }
 if(rig==injectActor)
 {
  // Simulate a clipped triangle's interpolated emitted vertex, WITHOUT modifying
  // any source attachment or creating fake clipping/bone data. XY and UV both
  // use the same interior barycentric coordinates; the normal mesh is rebuilt next frame.
  foreach(var part in sourceTriangles)
  {
   for(int t=0;t<part.triangles.Length&&injectedIndex<0;t+=3)
   {
    int a=part.triangles[t]*2,b=part.triangles[t+1]*2,c=part.triangles[t+2]*2;
    float determinant=(part.uv[b]-part.uv[a])*(part.uv[c+1]-part.uv[a+1])-(part.uv[b+1]-part.uv[a+1])*(part.uv[c]-part.uv[a]);
    if(UnityEngine.Mathf.Abs(determinant)<1e-10f)continue;
    for(int i=0;i<raw.Length;i++)if(isFace[i]&&uvs[i]==new UnityEngine.Vector2(part.uv[a],part.uv[a+1])&&UnityEngine.Vector2.SqrMagnitude(new UnityEngine.Vector2(raw[i].x-part.world[a],raw[i].y-part.world[a+1]))<1e-9f)
    {
     raw[i].x=.2f*part.world[a]+.3f*part.world[b]+.5f*part.world[c];raw[i].y=.2f*part.world[a+1]+.3f*part.world[b+1]+.5f*part.world[c+1];
     uvs[i]=new UnityEngine.Vector2(.2f*part.uv[a]+.3f*part.uv[b]+.5f*part.uv[c],.2f*part.uv[a+1]+.3f*part.uv[b+1]+.5f*part.uv[c+1]);
     buffers.vertexBuffer[i]=raw[i];buffers.uvBuffer[i]=uvs[i];injectedIndex=i;break;
    }
   }
   if(injectedIndex>=0)break;
  }
  Require(injectedIndex>=0,"could not create the interior face-UV probe");
 }
 captures[rig]=(raw,uvs,isFace,names,Pose(rig.Skeleton));
}
Spine.Unity.SkeletonAnimation Actor(string label,string path)
{
 var go=new UnityEngine.GameObject("Face motion QA "+label);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,scene);
 var rig=go.AddComponent<Spine.Unity.SkeletonAnimation>();actors.Add(rig);
 rig.skeletonDataAsset=UnityEditor.AssetDatabase.LoadAssetAtPath<Spine.Unity.SkeletonDataAsset>(path);Require(rig.skeletonDataAsset!=null,"missing "+path);
 rig.Initialize(false);rig.enabled=false;coverage[rig]=new System.Collections.Generic.HashSet<string>();
 rig.OnPostProcessVertices+=buffers=>Capture(rig,buffers);
 mappers[rig]=RoyalThrone.Characters.KingVanguardFaceRig.Ensure(rig);
 Require(rig.GetComponent<UnityEngine.MeshRenderer>().sharedMaterials.Length>0,"no current atlas material "+label);
 return rig;
}
void Inspect(Spine.Unity.SkeletonAnimation rig,int frame)
{
 var mapper=mappers[rig];var capture=captures[rig];var mesh=rig.GetComponent<UnityEngine.MeshFilter>().sharedMesh;
 Require(mesh!=null,"no generated mesh");var vertices=mesh.vertices;var uvs=mesh.uv;var bounds=mesh.bounds;
 Require(vertices.Length>=capture.raw.Length&&uvs.Length>=capture.raw.Length,"mesh buffer lost vertices");
 var pose=Pose(rig.Skeleton);Require(pose.Length==capture.pose.Length,"mapper changed deform lengths");
 for(int i=0;i<pose.Length;i++)Require(pose[i]==capture.pose[i],"mapper changed a bone or live Slot.Deform value");
 int expected=0,changed=0;
 for(int i=0;i<capture.raw.Length;i++)
 {
  var v=vertices[i];Require(Finite(v.x)&&Finite(v.y)&&Finite(v.z),"non-finite output vertex "+i);
  Require(uvs[i]==capture.uv[i],"mapper changed atlas UV "+i);
  var min=bounds.min;var max=bounds.max;
  Require(v.x>=min.x-1e-4f&&v.x<=max.x+1e-4f&&v.y>=min.y-1e-4f&&v.y<=max.y+1e-4f&&v.z>=min.z-1e-4f&&v.z<=max.z+1e-4f,"mapped vertex outside published mesh bounds "+i);
  float delta=(v-capture.raw[i]).sqrMagnitude;
  if(capture.face[i]){expected++;if(delta>1e-10f)changed++;}
  else Require(delta<1e-12f,"non-facial/clipped body vertex changed "+i);
  if(!mapper.enabled)Require(delta<1e-12f,"disabled mapper still modifies mesh "+i);
 }
 Require(Finite(bounds.size.x)&&Finite(bounds.size.y)&&Finite(bounds.size.z),"non-finite mesh bounds");
 if(mapper.enabled)
 {
  Require(mapper.IsRegistered,"mapper did not register current SkeletonData");
  Require(expected>20&&changed>10,"no visible mapped face (expected "+expected+", changed "+changed+")");
  Require(mapper.LastMappedVertices==expected,"facial coverage count mismatch: independently expected "+expected+", mapped "+mapper.LastMappedVertices);
  Require(mapper.LastAmbiguousVertices==0,"ambiguous atlas/position ownership");
  Require(mapper.ActiveFaceAttachments>=capture.names.Count,"active face diagnostics exclude emitted attachments");
 }
 var entry=rig.AnimationState.GetCurrent(0);if(entry!=null&&entry.MixingFrom!=null)mixingFrames++;
 trace.Append(rig.name).Append(',').Append(stage).Append(',').Append(frame).Append(',').Append(entry==null?"":entry.Animation.Name).Append(',').Append(entry==null?"":entry.TrackTime.ToString("F4",culture)).Append(',')
  .Append(entry==null||entry.MixingFrom==null?"":entry.MixingFrom.Animation.Name).Append(',').Append(entry==null?"":entry.MixTime.ToString("F4",culture)).Append(',')
  .Append(mapper.LastMappedVertices).Append(',').Append(mapper.LastInterpolatedVertices).Append(',').Append(mapper.LastAmbiguousVertices).Append(',').Append(mapper.LastUnattributedVertices).Append(',')
  .Append(capture.names.Count).Append(',').Append(capture.raw.Length).Append(',').Append(bounds.size.x.ToString("F5",culture)).Append(',').Append(bounds.size.y.ToString("F5",culture)).AppendLine();
 totalFrames++;
}
void Render(Spine.Unity.SkeletonAnimation rig,int frame){rig.LateUpdate();Inspect(rig,frame);}
void Run(Spine.Unity.SkeletonAnimation rig,string label,float seconds)
{
 stage=label;int count=UnityEngine.Mathf.CeilToInt(seconds*60);int mixesBefore=mixingFrames;
 for(int i=0;i<count;i++){rig.Update(1f/60);Render(rig,i);}
 Pass(label+": "+count+" frames; "+(mixingFrames-mixesBefore)+" with actual mixingFrom; face coverage, body vertices, live deform and bounds valid");
}
void SetPose(Spine.Unity.SkeletonAnimation rig,string clip,float time)
{
 Require(rig.Skeleton.Data.FindAnimation(clip)!=null,"missing source clip "+clip);
 rig.AnimationState.ClearTracks();rig.Skeleton.SetToSetupPose();rig.AnimationState.SetAnimation(0,clip,true);rig.Update(time);Render(rig,0);
}
void SameMesh(UnityEngine.Vector3[] first,UnityEngine.Vector3[] second,string reason)
{
 Require(first.Length==second.Length,reason+" count changed");for(int i=0;i<first.Length;i++)Require((first[i]-second[i]).sqrMagnitude<1e-12f,reason+" vertex "+i);
}
try
{
 string basePath="Assets/Characters/KingVenguard/V2/Spine/";
 var king=Actor("King A",basePath+"King_Vanguard_SkeletonData.asset");var kingB=Actor("King B",basePath+"King_Vanguard_SkeletonData.asset");
 var area=Actor("Area A",basePath+"AreaCompleted_Vanguard_SkeletonData.asset");var areaB=Actor("Area B",basePath+"AreaCompleted_Vanguard_SkeletonData.asset");
 var kingHash=Fingerprint(king.Skeleton.Data);var areaHash=Fingerprint(area.Skeleton.Data);
 Require(object.ReferenceEquals(king.Skeleton.Data,kingB.Skeleton.Data)&&object.ReferenceEquals(area.Skeleton.Data,areaB.Skeleton.Data),"isolation actors do not share source SkeletonData");
 stage="idle blink setup";SetPose(king,"idle_loop_1",0);Run(king,"idle to blink",3.65f);
 Require(king.Skeleton.FindSlot("Sağ Göz Kapağı")?.Attachment!=null&&king.Skeleton.FindSlot("Sol Göz kapağı")?.Attachment!=null,"acknowledgement must interrupt the source blink with both eyelids attached");
 Pass("acknowledgement starts at idle_loop_1 time "+king.AnimationState.GetCurrent(0).TrackTime.ToString("F4",culture)+", inside the source 3.5667-3.9 second blink");
 var acknowledgement=king.AnimationState.SetAnimation(0,"k-1_sevinme_3",false);
 acknowledgement.Complete+=entry=>{king.AnimationState.SetAnimation(0,"idle_loop_2",true);};
 Run(king,"idle blink to acknowledgement to idle",1.4f);
 Require(king.AnimationState.GetCurrent(0).Animation.Name=="idle_loop_2","acknowledgement did not return to idle");
 king.AnimationState.SetAnimation(0,"k-1_kaygi",false);king.AnimationState.AddAnimation(0,"k-1_kaygi_loop",true,0);
 Run(king,"worry entrance to worry loop",6.5f);
 Require(king.AnimationState.GetCurrent(0).Animation.Name=="k-1_kaygi_loop","worry start did not enter source loop");
 var happy=king.AnimationState.SetAnimation(0,"k-1sevinme_6",false);
 happy.Complete+=entry=>{king.AnimationState.SetAnimation(0,"k-1_kaygi_loop",true);};
 Run(king,"worry to happy to worry",4.3f);
 Require(king.AnimationState.GetCurrent(0).Animation.Name=="k-1_kaygi_loop","happy did not restore worry context");
 // Area has its own source clips; do not invent portrait clip names on this rig.
 stage="Area source setup";SetPose(area,"king_drill_loop",.4f);
 var areaStart=area.Skeleton.Data.FindAnimation("king_drill_win_clap_start");
 Require(areaStart!=null&&area.Skeleton.Data.FindAnimation("king_drill_win_clap_loop")!=null&&area.Skeleton.Data.FindAnimation("king_drill_win_clap_finish")!=null,"source Area clap clips missing");
 area.AnimationState.SetAnimation(0,"king_drill_win_clap_start",false);area.AnimationState.AddAnimation(0,"king_drill_win_clap_loop",true,0);
 Run(area,"Area drill to clap start to loop",areaStart.Duration+.8f);
 var finish=area.AnimationState.SetAnimation(0,"king_drill_win_clap_finish",false);finish.Complete+=entry=>{area.AnimationState.SetAnimation(0,"king_drill_loop",true);};
 Run(area,"Area clap finish to drill",finish.Animation.Duration+.6f);
 foreach(var rig in new[]{king,area})
 {
  string idle=rig==king?"idle_loop_2":"king_drill_loop";stage=rig.name+" reproducibility";SetPose(rig,idle,.43f);
  var mesh=rig.GetComponent<UnityEngine.MeshFilter>().sharedMesh;var baseline=mesh.vertices;
  for(int i=0;i<12;i++){Render(rig,i);SameMesh(baseline,rig.GetComponent<UnityEngine.MeshFilter>().sharedMesh.vertices,"same pose accumulated mapping");}
  Pass(rig.name+": 12 repeated mesh builds at one pose do not accumulate deformation");
  mappers[rig].enabled=false;Render(rig,0);Require(!mappers[rig].IsRegistered,"disabled mapper registration retained");
  mappers[rig].enabled=true;Render(rig,1);SameMesh(baseline,rig.GetComponent<UnityEngine.MeshFilter>().sharedMesh.vertices,"enable/disable changed mapped pose");
  rig.gameObject.SetActive(false);Require(!mappers[rig].IsRegistered,"hidden actor retained mapper binding");rig.gameObject.SetActive(true);Render(rig,2);
  SameMesh(baseline,rig.GetComponent<UnityEngine.MeshFilter>().sharedMesh.vertices,"hide/show changed mapped pose");Pass(rig.name+": disable and hide clear mapping; re-enable rebuilds the identical pose");
  stage=rig.name+" barycentric face probe";injectActor=rig;injectedIndex=-1;Render(rig,0);
  Require(mappers[rig].LastInterpolatedVertices>=1,"interior face UV was not attributed using triangle interpolation");
  Require((rig.GetComponent<UnityEngine.MeshFilter>().sharedMesh.vertices[injectedIndex]-captures[rig].raw[injectedIndex]).sqrMagnitude>1e-10f,"interpolated face vertex was left unmapped");
  injectActor=null;Render(rig,1);SameMesh(baseline,rig.GetComponent<UnityEngine.MeshFilter>().sharedMesh.vertices,"probe contaminated next generated mesh");Pass(rig.name+": interior barycentric face vertex mapped and next clean render restored");
 }
 foreach(var pair in new[]{(a:king,b:kingB,idle:"idle_loop_2",action:"k-1sevinme_6"),(a:area,b:areaB,idle:"king_drill_loop",action:"king_drill_win_clap_loop")})
 {
  stage=pair.a.name+" independent instances";SetPose(pair.b,pair.idle,.61f);var baseline=pair.b.GetComponent<UnityEngine.MeshFilter>().sharedMesh.vertices;var pose=Pose(pair.b.Skeleton);
  pair.a.AnimationState.SetAnimation(0,pair.action,false);Run(pair.a,pair.a.name+" independent action",.5f);Render(pair.b,0);
  SameMesh(baseline,pair.b.GetComponent<UnityEngine.MeshFilter>().sharedMesh.vertices,"another instance changed this mesh");
  var after=Pose(pair.b.Skeleton);Require(after.Length==pose.Length,"other instance changed deform size");for(int i=0;i<pose.Length;i++)Require(pose[i]==after[i],"other instance changed a bone/deform");
  Require(pair.a.GetComponent<UnityEngine.MeshFilter>().sharedMesh!=pair.b.GetComponent<UnityEngine.MeshFilter>().sharedMesh,"instances unexpectedly share generated meshes");
  Pass(pair.a.name+": shared source data with independent live bones, deforms and generated meshes");
 }
 Require(mixingFrames>=30,"fixture failed to exercise enough real crossfade frames");
 Require(Fingerprint(king.Skeleton.Data)==kingHash,"King shared attachment geometry/UV/weights/FFD/timeline data changed");
 Require(Fingerprint(area.Skeleton.Data)==areaHash,"Area shared attachment geometry/UV/weights/FFD/timeline data changed");
 Pass("both source fingerprints unchanged, including attachment weights/UV/geometry, all timeline arrays and setup bones");
 foreach(var rig in new[]{king,area}){report.AppendLine("COVERAGE "+rig.name+": "+string.Join(" | ",coverage[rig]));Require(coverage[rig].Contains("Burun")&&coverage[rig].Contains("Bıyık")&&coverage[rig].Contains("Sakal Surat")&&coverage[rig].Contains("Sağ Göz Bebeği")&&coverage[rig].Contains("Sol  Göz Bebeği"),"core visible face feature coverage incomplete");}
 report.AppendLine("COMPLETE "+totalFrames+" checked mesh frames; "+mixingFrames+" real AnimationState mixing frames. Visual transition quality still requires the companion motion recording.");
}
catch(System.Exception error){report.AppendLine("FAIL "+error);throw;}
finally
{
 injectActor=null;UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
 System.IO.File.WriteAllText(output+"/face-motion-checks.txt",report.ToString());System.IO.File.WriteAllText(output+"/face-motion-60hz.csv",trace.ToString());
}
return report.ToString();
