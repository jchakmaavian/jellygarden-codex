using System;
using System.IO;
using System.Collections.Generic;
using Spine;
using Spine.Unity;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using RoyalThrone.Characters;
public static class KingFaceRepairChecks {
 public static void Run(){
  string output=Path.GetFullPath("../JellyGarden-Codex/Design/KingVanguard-FaceRepair-20261004/Validation");Directory.CreateDirectory(output);
  int frames=0,toothSamples=0;float minIris=99,maxEscape=0;var summary=new System.Text.StringBuilder();
  var scene=EditorSceneManager.NewPreviewScene();
  try{foreach(string rigName in new[]{"King","AreaCompleted"}){
   var go=new GameObject("Face regression "+rigName);SceneManager.MoveGameObjectToScene(go,scene);var actor=go.AddComponent<SkeletonAnimation>();
   actor.skeletonDataAsset=AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>("Assets/Characters/KingVenguard/V2/Spine/"+rigName+"_Vanguard_SkeletonData.asset");actor.Initialize(false);actor.enabled=false;
   var mapper=KingVanguardFaceRig.Ensure(actor);var mouthRig=go.GetComponent<KingVanguardMouthRig>();
   var tex=new Texture2D(2,2,TextureFormat.RGBA32,false);ImageConversion.LoadImage(tex,File.ReadAllBytes("Assets/Characters/KingVenguard/V2/Spine/"+rigName+"_Vanguard.png"));
   var shadow=actor.Skeleton.FindSlot("Taç Alt Gölge").Attachment as MeshAttachment;
   Require(shadow!=null&&shadow.Path=="Vanguard Crown Contact"&&shadow.Triangles.Length>0,"new crown contact shadow missing");
   foreach(var clip in actor.Skeleton.Data.Animations){
    actor.AnimationState.ClearTracks();actor.Skeleton.SetToSetupPose();actor.AnimationState.SetAnimation(0,clip.Name,false);
    for(int f=0;f<16;f++){
     actor.Update(f==0?0:clip.Duration/15);Inspect(actor,mapper,mouthRig,tex,clip.Name+"/"+f,ref frames,ref toothSamples,ref minIris,ref maxEscape);
    }
   }
   string idle=rigName=="King"?"idle_loop_1":"king_drill_loop",reaction=rigName=="King"?"k-1sevinme_6":"king_drill_win_clap_loop";
   actor.AnimationState.ClearTracks();actor.Skeleton.SetToSetupPose();actor.AnimationState.SetAnimation(0,idle,true);
   for(int f=0;f<300;f++){if(f==100)actor.AnimationState.SetAnimation(0,reaction,true);if(f==200)actor.AnimationState.SetAnimation(0,idle,true);actor.Update(1/60f);Inspect(actor,mapper,mouthRig,tex,"crossfade/"+f,ref frames,ref toothSamples,ref minIris,ref maxEscape);}
   var one=actor.Skeleton.FindSlot("Ust Diş").Attachment as MeshAttachment;var saved=(float[])one.Vertices.Clone();
   var go2=new GameObject("Independent mouth instance");SceneManager.MoveGameObjectToScene(go2,scene);var other=go2.AddComponent<SkeletonAnimation>();other.skeletonDataAsset=actor.skeletonDataAsset;other.Initialize(false);other.enabled=false;KingVanguardFaceRig.Ensure(other);other.AnimationState.SetAnimation(0,reaction,true);other.Update(.4f);other.LateUpdate();
   Require(!ReferenceEquals(one,other.Skeleton.FindSlot("Ust Diş").Attachment),"actors share mutable mouth geometry");for(int i=0;i<saved.Length;i++)Require(saved[i]==one.Vertices[i],"other actor modified this mouth");
   other.AnimationState.ClearTracks();other.Skeleton.FindSlot("Ağız").Attachment=null;other.Update(0);Require(other.Skeleton.FindSlot("Ust Diş").Attachment==null,"hidden native mouth left a ghost assembly");
   other.Skeleton.SetToSetupPose();other.Skeleton.FindSlot("Ağız").A=.35f;other.Update(0);Require(Mathf.Abs(other.Skeleton.FindSlot("Ust Diş").A-.35f)<.00001f,"mouth did not follow native slot fade");
   other.gameObject.SetActive(false);other.gameObject.SetActive(true);Require((other.Skeleton.FindSlot("Ust Diş").Attachment as MeshAttachment)?.Path==KingVanguardFaceSkin.MouthPath,"mouth not initialized on reactivation");
   summary.AppendLine("PASS "+rigName+": all "+actor.Skeleton.Data.Animations.Count+" clips, real 60 Hz crossfades, independent actor geometry.");UnityEngine.Object.DestroyImmediate(tex);
  }
  summary.AppendLine("PASS "+frames+" rendered frames; "+toothSamples+" opaque tooth samples inside mouth artwork; minimum iris scale "+minIris+"; maximum normalized tooth escape "+maxEscape);
  File.WriteAllText(output+"/face-repair-checks.txt",summary.ToString());Debug.Log("FACE_REPAIR_CHECKS_PASS "+summary);
  }catch(Exception e){File.WriteAllText(output+"/face-repair-checks-FAIL.txt",e.ToString());throw;}finally{EditorSceneManager.ClosePreviewScene(scene);}
 }
 static void Inspect(SkeletonAnimation actor,KingVanguardFaceRig mapper,KingVanguardMouthRig rig,Texture2D tex,string label,ref int frames,ref int samples,ref float minIris,ref float maxEscape){
  actor.LateUpdate();frames++;Require(mapper.IsRegistered&&mapper.ActivePaintedEyeContours==4,label+": eye contour registration");Require(mapper.LastAmbiguousVertices==0,label+": ambiguous face vertex");minIris=Mathf.Min(minIris,mapper.MinimumIrisScale);Require(mapper.MinimumIrisScale>1.2f,label+": iris collapsed");
  var slot=actor.Skeleton.FindSlot("Ust Diş");var mouth=slot.Attachment as MeshAttachment;Require(mouth!=null&&mouth.Path==KingVanguardFaceSkin.MouthPath,label+": mouth not driven this frame");Require(rig.Opening>0&&rig.Width>0,label+": invalid lip opening");
  foreach(var n in new[]{"Ağız","Alt Diş","Dil","Sol Yanak","Sağ Yanak"}){var a=actor.Skeleton.FindSlot(n)?.Attachment;if(a is MeshAttachment m)Require(m.A==0,label+": old patch visible "+n);else if(a is RegionAttachment r)Require(r.A==0,label+": old patch visible "+n);}
  var world=new float[mouth.WorldVerticesLength];mouth.ComputeWorldVertices(slot,world);foreach(float v in world)Require(!float.IsNaN(v)&&!float.IsInfinity(v),label+": nonfinite mouth");
  var uvs=mouth.UVs;var page=((AtlasRegion)mouth.RendererObject).page;var region=KingVanguardFaceSkin.Region(page,3);
  const int count=105;const int perPatchTriangles=480;
  for(int part=1;part<3;part++)for(int v=1;v<4;v++)for(int u=2;u<19;u+=2){
   int i=part*count+v*21+u;var painted=tex.GetPixelBilinear(uvs[i*2],uvs[i*2+1]);if(painted.a<.8f)continue;
   // Ignore fully collapsed clipped triangles (invisible vertices along an occlusion edge).
   var p=P(world,i);var prev=P(world,i-21);var next=P(world,i+21);if((next-prev).sqrMagnitude<1e-10f)continue;
   bool contained=false;float alpha=0;
   for(int t=0;t<perPatchTriangles;t+=3){int a=mouth.Triangles[t],b=mouth.Triangles[t+1],c=mouth.Triangles[t+2];Vector3 bary;if(!Bary(p,P(world,a),P(world,b),P(world,c),out bary))continue;
    var uv=P(uvs,a)*bary.x+P(uvs,b)*bary.y+P(uvs,c)*bary.z;var color=tex.GetPixelBilinear(uv.x,uv.y);alpha=Mathf.Max(alpha,color.a);if(color.a>.7f&&color.g<.38f)contained=true;
   }
   samples++;if(!contained){maxEscape=Mathf.Max(maxEscape,1-alpha);throw new Exception(label+": tooth outside dark mouth, part="+part+" u="+u+" row="+v+" opening="+rig.Opening+" alpha="+alpha);}
  }
 }
 static Vector2 P(float[] a,int i){return new Vector2(a[i*2],a[i*2+1]);}
 static bool Bary(Vector2 p,Vector2 a,Vector2 b,Vector2 c,out Vector3 r){float d=(b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x);r=Vector3.zero;if(Mathf.Abs(d)<1e-10f)return false;float v=((p.x-a.x)*(c.y-a.y)-(p.y-a.y)*(c.x-a.x))/d,w=((b.x-a.x)*(p.y-a.y)-(b.y-a.y)*(p.x-a.x))/d;r=new Vector3(1-v-w,v,w);return r.x>=-1e-4f&&r.y>=-1e-4f&&r.z>=-1e-4f;}
 static void Require(bool value,string message){if(!value)throw new Exception(message);}
}
