public static class VerifyAttentionPixelsCompile { public static object Run(){
// Unity eval_file, synchronous private preview. Root executes; no live board/input is changed.
// The companion live fixture proves InputManager/HUD routing. This bounded fixture
// injects ONLY attention values to measure the actual post-processed mesh + installed atlas alpha.
string output="Design/KingVanguard-InteractionFix-20260930/Validation";System.IO.Directory.CreateDirectory(output);
var report=new System.Text.StringBuilder("RUNNING: actual rendered pupil triangles, current installed atlas, source versus cardinal attention.\n");
var rows=new System.Text.StringBuilder("clip,time,target,eye,opaquePixels,uncoveredPixels,lidCoveredPixels,pupilCenterFaceX,pupilCenterFaceY\n");
var culture=System.Globalization.CultureInfo.InvariantCulture;
var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
UnityEngine.Texture2D atlas=null;string stage="setup";int checks=0;
string leftPupil="Sol  Göz Bebeği",rightPupil="Sağ Göz Bebeği";
var wanted=new System.Collections.Generic.HashSet<string>(new[]{"Sol Göz","Sağ Göz",leftPupil,rightPupil,"Sol Göz kapağı","Sağ Göz Kapağı","Sol Kirpik","Sağ Kirpik"});
var actor=new UnityEngine.GameObject("Attention pixel proof");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(actor,scene);
var animation=actor.AddComponent<Spine.Unity.SkeletonAnimation>();
animation.skeletonDataAsset=UnityEditor.AssetDatabase.LoadAssetAtPath<Spine.Unity.SkeletonDataAsset>("Assets/Characters/KingVenguard/V2/Spine/King_Vanguard_SkeletonData.asset");
animation.Initialize(false);animation.enabled=false;
var inputObject=new UnityEngine.GameObject("Attention values only - fixture");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(inputObject,scene);
var attention=inputObject.AddComponent<RoyalThrone.Characters.KingVanguardAttention>();
string[] ownerByVertex=null;var drawRank=new System.Collections.Generic.Dictionary<string,int>();
var triangles=new System.Collections.Generic.Dictionary<string,System.Collections.Generic.List<(UnityEngine.Vector2 a,UnityEngine.Vector2 b,UnityEngine.Vector2 c,UnityEngine.Vector2 ua,UnityEngine.Vector2 ub,UnityEngine.Vector2 uc,float aa,float ab,float ac)>>();
void Require(bool ok,string message){if(!ok)throw new System.Exception(stage+": "+message);checks++;}
void Pass(string message){report.AppendLine("PASS "+message);}
void Value(string property,object value)
{
 var field=typeof(RoyalThrone.Characters.KingVanguardAttention).GetField("<"+property+">k__BackingField",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
 Require(field!=null,"attention property missing "+property);field.SetValue(attention,value);
}
bool Barycentric(UnityEngine.Vector2 point,UnityEngine.Vector2 a,UnityEngine.Vector2 b,UnityEngine.Vector2 c,out UnityEngine.Vector3 weights)
{
 var ab=b-a;var ac=c-a;var ap=point-a;float determinant=ab.x*ac.y-ab.y*ac.x;
 weights=default;if(UnityEngine.Mathf.Abs(determinant)<1e-13f)return false;
 float wb=(ap.x*ac.y-ap.y*ac.x)/determinant,wc=(ab.x*ap.y-ab.y*ap.x)/determinant;
 weights=new UnityEngine.Vector3(1-wb-wc,wb,wc);return weights.x>=-1e-5f&&weights.y>=-1e-5f&&weights.z>=-1e-5f;
}
animation.OnPostProcessVertices+=buffers=>
{
 // Establish attribution BEFORE the mapper with both original skinning and UVs.
 // This avoids broad atlas rectangles and prevents overlapping packed islands
 // or an unrelated body's UV from being counted as an eye.
 var owners=new System.Collections.Generic.Dictionary<UnityEngine.Vector2,System.Collections.Generic.List<(UnityEngine.Vector2 point,string name)>>();
 drawRank.Clear();int rank=0;
 foreach(var slot in animation.Skeleton.DrawOrder)
 {
  rank++;var attachment=slot.Attachment;if(attachment==null||!slot.Bone.Active||!wanted.Contains(attachment.Name))continue;
  var region=attachment as Spine.RegionAttachment;var mesh=attachment as Spine.MeshAttachment;if(region==null&&mesh==null)continue;
  drawRank[attachment.Name]=rank;float[] world=new float[region!=null?8:mesh.WorldVerticesLength];float[] uv=region!=null?region.UVs:mesh.UVs;
  if(region!=null)region.ComputeWorldVertices(slot.Bone,world,0);else mesh.ComputeWorldVertices(slot,world);
  for(int i=0;i<world.Length;i+=2)
  {
   var key=new UnityEngine.Vector2(uv[i],uv[i+1]);if(!owners.TryGetValue(key,out var list)){list=new System.Collections.Generic.List<(UnityEngine.Vector2,string)>();owners.Add(key,list);}
   list.Add((new UnityEngine.Vector2(world[i],world[i+1]),attachment.Name));
  }
 }
 ownerByVertex=new string[buffers.vertexCount];
 for(int i=0;i<buffers.vertexCount;i++)if(owners.TryGetValue(buffers.uvBuffer[i],out var list))
  foreach(var owner in list)if((owner.point-new UnityEngine.Vector2(buffers.vertexBuffer[i].x,buffers.vertexBuffer[i].y)).sqrMagnitude<1e-9f)
  {Require(ownerByVertex[i]==null||ownerByVertex[i]==owner.name,"ambiguous original UV + vertex ownership");ownerByVertex[i]=owner.name;}
};
var mapper=RoyalThrone.Characters.KingVanguardFaceRig.Ensure(animation);
var attentionField=typeof(RoyalThrone.Characters.KingVanguardFaceRig).GetField("attention",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
Require(attentionField!=null,"FaceRig attention reference missing");
// SkeletonAnimation and SkeletonGraphic share the same mapper. Inject the dependency
// on this private actor; normal runtime HUD registration and input are tested separately.
attentionField.SetValue(mapper,attention);
void Render(UnityEngine.Vector2 gaze,float influence)
{
 Value("Gaze",gaze);Value("Influence",influence);Value("BodyLean",0f);Value("TapResponse",0f);
 animation.LateUpdate();Require(mapper.LastAmbiguousVertices==0,"mapper reported ambiguous ownership");
 Require(UnityEngine.Mathf.Abs(mapper.GazeInfluence-influence)<1e-6f,"attention was not consumed by rendered mesh");
 var mesh=actor.GetComponent<UnityEngine.MeshFilter>().sharedMesh;Require(mesh!=null,"no actual generated mesh");
 var vertices=mesh.vertices;var uvs=mesh.uv;var colors=mesh.colors32;var indices=mesh.triangles;triangles.Clear();
 for(int i=0;i<indices.Length;i+=3)
 {
  int a=indices[i],b=indices[i+1],c=indices[i+2];if(a>=ownerByVertex.Length||b>=ownerByVertex.Length||c>=ownerByVertex.Length)continue;
  string name=ownerByVertex[a];if(name==null||name!=ownerByVertex[b]||name!=ownerByVertex[c])continue;
  if(!triangles.TryGetValue(name,out var list))
  {list=new System.Collections.Generic.List<(UnityEngine.Vector2,UnityEngine.Vector2,UnityEngine.Vector2,UnityEngine.Vector2,UnityEngine.Vector2,UnityEngine.Vector2,float,float,float)>();triangles.Add(name,list);}
  list.Add((vertices[a],vertices[b],vertices[c],uvs[a],uvs[b],uvs[c],colors.Length>a?colors[a].a/255f:1,colors.Length>b?colors[b].a/255f:1,colors.Length>c?colors[c].a/255f:1));
 }
}
float AlphaAt(string name,UnityEngine.Vector2 world)
{
 if(!triangles.TryGetValue(name,out var list))return 0;
 float alpha=0;
 foreach(var t in list)if(Barycentric(world,t.a,t.b,t.c,out var w))
 {
  var uv=t.ua*w.x+t.ub*w.y+t.uc*w.z;
  alpha=UnityEngine.Mathf.Max(alpha,atlas.GetPixelBilinear(uv.x,uv.y).a*(t.aa*w.x+t.ab*w.y+t.ac*w.z));
 }
 return alpha;
}
(int opaque,int outside,int coveredByLid,UnityEngine.Vector2 center) Measure(string pupil,string white,string lid,string lash)
{
 Require(triangles.TryGetValue(pupil,out var list)&&list.Count>0,"pupil has no rendered triangles: "+pupil);
 Require(triangles.ContainsKey(white),"eye white has no rendered triangles: "+white);
 var min=new UnityEngine.Vector2(float.MaxValue,float.MaxValue);var max=new UnityEngine.Vector2(float.MinValue,float.MinValue);
 foreach(var t in list){min=UnityEngine.Vector2.Min(min,UnityEngine.Vector2.Min(t.ua,UnityEngine.Vector2.Min(t.ub,t.uc)));max=UnityEngine.Vector2.Max(max,UnityEngine.Vector2.Max(t.ua,UnityEngine.Vector2.Max(t.ub,t.uc)));}
 int opaque=0,outside=0,coveredByLid=0;var center=UnityEngine.Vector2.zero;
 for(int y=UnityEngine.Mathf.Max(0,UnityEngine.Mathf.FloorToInt(min.y*atlas.height));y<UnityEngine.Mathf.Min(atlas.height,UnityEngine.Mathf.CeilToInt(max.y*atlas.height));y++)
  for(int x=UnityEngine.Mathf.Max(0,UnityEngine.Mathf.FloorToInt(min.x*atlas.width));x<UnityEngine.Mathf.Min(atlas.width,UnityEngine.Mathf.CeilToInt(max.x*atlas.width));x++)
  {
   var uv=new UnityEngine.Vector2((x+.5f)/atlas.width,(y+.5f)/atlas.height);
   foreach(var t in list)if(Barycentric(uv,t.ua,t.ub,t.uc,out var w))
   {
    float alpha=atlas.GetPixelBilinear(uv.x,uv.y).a*(t.aa*w.x+t.ab*w.y+t.ac*w.z);if(alpha<128f/255)break;
    var world=t.a*w.x+t.b*w.y+t.c*w.z;opaque++;center+=world;
    float lidAlpha=drawRank.TryGetValue(lid,out var lidRank)&&lidRank>drawRank[pupil]?AlphaAt(lid,world):0;
    float lashAlpha=drawRank.TryGetValue(lash,out var lashRank)&&lashRank>drawRank[pupil]?AlphaAt(lash,world):0;
    bool occluded=UnityEngine.Mathf.Max(lidAlpha,lashAlpha)>=32f/255;
    if(occluded)coveredByLid++;
    if(AlphaAt(white,world)<32f/255&&!occluded)outside++;
    break;
   }
  }
 Require(opaque>10,"current pupil art has too few opaque pixels: "+pupil);
 center/=opaque;var face=animation.Skeleton.FindBone("on_yuz");float cx,cy;face.WorldToLocal(center.x,center.y,out cx,out cy);
 return(opaque,outside,coveredByLid,new UnityEngine.Vector2(cx,cy));
}
void Row(string clip,float time,string target,string eye,(int opaque,int outside,int coveredByLid,UnityEngine.Vector2 center) data)
{
 rows.Append(clip).Append(',').Append(time.ToString("F3",culture)).Append(',').Append(target).Append(',').Append(eye).Append(',').Append(data.opaque).Append(',').Append(data.outside).Append(',').Append(data.coveredByLid).Append(',').Append(data.center.x.ToString("F6",culture)).Append(',').Append(data.center.y.ToString("F6",culture)).AppendLine();
}
try
{
 atlas=new UnityEngine.Texture2D(2,2,UnityEngine.TextureFormat.RGBA32,false);
 var bytes=System.IO.File.ReadAllBytes("Assets/Characters/KingVenguard/V2/Spine/King_Vanguard.png");
 Require(UnityEngine.ImageConversion.LoadImage(atlas,bytes,false),"cannot decode current atlas PNG");
 report.AppendLine("Atlas "+atlas.width+"x"+atlas.height+"; opaque pupil threshold 128/255; white/lid coverage 32/255; compare against the same current-art source pose.\nNo requirement that transparent pupil-quad margins fit the painted white.");
 foreach(var pose in new[]{(clip:"idle_loop_1",time:0f),(clip:"idle_loop_1",time:3.65f),(clip:"k-1_kaygi_loop",time:.5f)})
 {
  stage=pose.clip+" @ "+pose.time.ToString("F3",culture);
  animation.AnimationState.ClearTracks();animation.Skeleton.SetToSetupPose();animation.AnimationState.SetAnimation(0,pose.clip,true);animation.Update(pose.time);
  foreach(var name in new[]{"Frame Background3","sol frame","sag frame"}){var slot=animation.Skeleton.FindSlot(name);if(slot!=null)slot.Attachment=null;}
  if(pose.time==3.65f)Require(animation.Skeleton.FindSlot("Sağ Göz Kapağı").Attachment!=null&&animation.Skeleton.FindSlot("Sol Göz kapağı").Attachment!=null,"blink probe must retain both native eyelids");
  Render(UnityEngine.Vector2.zero,0);
  var sourceLeft=Measure(leftPupil,"Sol Göz","Sol Göz kapağı","Sol Kirpik");var sourceRight=Measure(rightPupil,"Sağ Göz","Sağ Göz Kapağı","Sağ Kirpik");
  Row(pose.clip,pose.time,"source","left",sourceLeft);Row(pose.clip,pose.time,"source","right",sourceRight);
  Render(UnityEngine.Vector2.zero,1);
  var neutralLeft=Measure(leftPupil,"Sol Göz","Sol Göz kapağı","Sol Kirpik");var neutralRight=Measure(rightPupil,"Sağ Göz","Sağ Göz Kapağı","Sağ Kirpik");
  Row(pose.clip,pose.time,"attention-center","left",neutralLeft);Row(pose.clip,pose.time,"attention-center","right",neutralRight);
  foreach(var target in new[]{(name:"left",gaze:UnityEngine.Vector2.left),(name:"right",gaze:UnityEngine.Vector2.right),(name:"up",gaze:UnityEngine.Vector2.up),(name:"down",gaze:UnityEngine.Vector2.down)})
  {
   Render(target.gaze,1);
   var left=Measure(leftPupil,"Sol Göz","Sol Göz kapağı","Sol Kirpik");var right=Measure(rightPupil,"Sağ Göz","Sağ Göz Kapağı","Sağ Kirpik");
   Row(pose.clip,pose.time,target.name,"left",left);Row(pose.clip,pose.time,target.name,"right",right);
   foreach(var eye in new[]{(name:"left",result:left,source:sourceLeft,neutral:neutralLeft),(name:"right",result:right,source:sourceRight,neutral:neutralRight)})
   {
    // Two texels or 1% allow only the bilinear boundary sampling difference.
    // Existing exposed pixels in the source expression are reported, not mislabelled as new attention spill.
    int tolerance=UnityEngine.Mathf.Max(2,UnityEngine.Mathf.CeilToInt(eye.source.opaque*.01f));
    Require(eye.result.outside<=eye.source.outside+tolerance,eye.name+" "+target.name+" introduces opaque iris spill: source "+eye.source.outside+", attention "+eye.result.outside+", tolerance "+tolerance);
    float movement=UnityEngine.Vector2.Dot(eye.result.center-eye.neutral.center,target.gaze);
    Require(movement>.002f,eye.name+" "+target.name+" rendered pupil did not move meaningfully in the requested direction ("+movement.ToString("F6",culture)+")");
   }
  }
  Pass(stage+": both pupils move toward all four cardinal targets; opaque spill stays at source baseline or below (AA tolerance stated); original eyelid geometry/order included");
 }
 report.AppendLine("ALL PUPIL PIXEL CHECKS PASSED (3 source poses, source + centered + 4 cardinal targets; "+checks+" invariant checks). Actual HUD input/screenshots are verified separately.");
}
catch(System.Exception error){report.AppendLine("FAIL "+error);throw;}
finally
{
 if(atlas)UnityEngine.Object.DestroyImmediate(atlas);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
 System.IO.File.WriteAllText(output+"/pupil-pixel-checks.txt",report.ToString());System.IO.File.WriteAllText(output+"/pupil-pixel-coverage.csv",rows.ToString());
}
return report.ToString();

}}