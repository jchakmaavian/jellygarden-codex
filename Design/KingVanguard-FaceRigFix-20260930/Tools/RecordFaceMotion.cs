// Deterministic 60 fps playback of the actual Spine AnimationState, including mixes.
// Runs in a private preview scene; no player profile or game scene is changed.
string output="Design/KingVanguard-FaceRigFix-20260930/After";
string frames=output+"/MotionFrames";System.IO.Directory.CreateDirectory(frames);
var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
var actor=new UnityEngine.GameObject("Face motion review");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(actor,scene);
var rig=actor.AddComponent<Spine.Unity.SkeletonAnimation>();
rig.skeletonDataAsset=UnityEditor.AssetDatabase.LoadAssetAtPath<Spine.Unity.SkeletonDataAsset>("Assets/Characters/KingVenguard/V2/Spine/King_Vanguard_SkeletonData.asset");rig.Initialize(false);rig.enabled=false;
actor.AddComponent<RoyalThrone.Characters.KingVanguardFaceRig>();
var cameraGo=new UnityEngine.GameObject("Face motion camera",typeof(UnityEngine.Camera));UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraGo,scene);
var camera=cameraGo.GetComponent<UnityEngine.Camera>();camera.scene=scene;camera.enabled=false;camera.orthographic=true;camera.orthographicSize=1.85f;
camera.transform.position=new UnityEngine.Vector3(0,2.4f,-20);camera.clearFlags=UnityEngine.CameraClearFlags.SolidColor;camera.backgroundColor=new UnityEngine.Color(.08f,.18f,.34f,1);
var target=new UnityEngine.RenderTexture(640,640,24,UnityEngine.RenderTextureFormat.ARGB32);camera.targetTexture=target;
var image=new UnityEngine.Texture2D(640,640,UnityEngine.TextureFormat.RGB24,false);
var trace=new System.Text.StringBuilder("frame,time,clip,trackTime,mixTime\n");
int frame=0;const int count=1440;
System.Action Dispose=()=>{camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(image);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);};
UnityEditor.EditorApplication.CallbackFunction tick=null;
tick=()=>{
 var previous=UnityEngine.RenderTexture.active;
 try{
  for(int batch=0;batch<20&&frame<count;batch++,frame++){
   switch(frame){
    case 0:rig.AnimationState.SetAnimation(0,"idle_loop_1",true);break;
    case 219:rig.AnimationState.SetAnimation(0,"k-1_sevinme_3",false);break;
    case 264:rig.AnimationState.SetAnimation(0,"idle_loop_3",true);break;
    case 360:rig.AnimationState.SetAnimation(0,"k-1_kaygi",false);rig.AnimationState.AddAnimation(0,"k-1_kaygi_loop",true,0);break;
    case 762:rig.AnimationState.SetAnimation(0,"k-1sevinme_6",false);break;
    case 987:rig.AnimationState.SetAnimation(0,"k-1_kaygi_loop",true);break;
    case 1092:rig.AnimationState.SetAnimation(0,"k-1_sevinme_NEW",false);break;
    case 1278:rig.AnimationState.SetAnimation(0,"idle_loop_2",true);break;
   }
   rig.Update(frame==0?0:1f/60f);
   foreach(string hidden in new[]{"Frame Background3","sol frame","sag frame"})rig.Skeleton.FindSlot(hidden).Attachment=null;
   rig.LateUpdate();camera.Render();UnityEngine.RenderTexture.active=target;
   image.ReadPixels(new UnityEngine.Rect(0,0,640,640),0,0);image.Apply();
   System.IO.File.WriteAllBytes(frames+"/frame-"+frame.ToString("D4")+".png",image.EncodeToPNG());
   var entry=rig.AnimationState.GetCurrent(0);
   trace.AppendLine(frame+","+(frame/60f).ToString("F4",System.Globalization.CultureInfo.InvariantCulture)+","+entry.Animation.Name+","+entry.TrackTime.ToString("F4",System.Globalization.CultureInfo.InvariantCulture)+","+entry.MixTime.ToString("F4",System.Globalization.CultureInfo.InvariantCulture));
  }
  System.IO.File.WriteAllText(output+"/record-status.txt",frame>=count?"PASS 1440 frames at 60 fps, 24 seconds, actual AnimationState crossfades; isolated presentation review.":"RUNNING "+frame+" / "+count);
  if(frame>=count){System.IO.File.WriteAllText(output+"/motion-trace.csv",trace.ToString());UnityEditor.EditorApplication.update-=tick;Dispose();}
 }catch(System.Exception e){System.IO.File.WriteAllText(output+"/record-status.txt","FAIL "+e);UnityEditor.EditorApplication.update-=tick;Dispose();}
 finally{UnityEngine.RenderTexture.active=previous;}
};
UnityEditor.EditorApplication.update+=tick;
return "Recording 60 fps facial animation and transitions in isolated preview scene.";
