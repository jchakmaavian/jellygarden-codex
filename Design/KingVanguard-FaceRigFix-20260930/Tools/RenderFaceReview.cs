string root="Design/KingVanguard-FaceRigFix-20260930";
string phase=System.IO.File.ReadAllText(root+"/render-phase.txt").Trim();
string output=root+"/"+phase;System.IO.Directory.CreateDirectory(output);
var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
var previous=UnityEngine.RenderTexture.active;
var report=new System.Text.StringBuilder("Deterministic face samples, current game assets. Columns 0%, 20%, 40%; second row 60%, 80%, 95%.\n");
try{
 var cameraGo=new UnityEngine.GameObject("Face review camera",typeof(UnityEngine.Camera));UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraGo,scene);
 var camera=cameraGo.GetComponent<UnityEngine.Camera>();camera.scene=scene;camera.orthographic=true;camera.orthographicSize=1.8f;
 camera.transform.position=new UnityEngine.Vector3(0,2.45f,-20);camera.clearFlags=UnityEngine.CameraClearFlags.SolidColor;camera.backgroundColor=new UnityEngine.Color(.08f,.18f,.34f,1);
 var target=new UnityEngine.RenderTexture(512,512,24,UnityEngine.RenderTextureFormat.ARGB32);camera.targetTexture=target;
 string[] names={"idle_loop_2","k-1_sevinme_3","k-1sevinme_6","k-1_kaygi","k-1_kaygi_loop","k-1_uzulme","k-1_sevinme_NEW"};
 float[] phases={0,.2f,.4f,.6f,.8f,.95f};
 foreach(string name in names){
  var actor=new UnityEngine.GameObject("Face review actor");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(actor,scene);
  var rig=actor.AddComponent<Spine.Unity.SkeletonAnimation>();
  rig.skeletonDataAsset=UnityEditor.AssetDatabase.LoadAssetAtPath<Spine.Unity.SkeletonDataAsset>("Assets/Characters/KingVenguard/V2/Spine/King_Vanguard_SkeletonData.asset");rig.Initialize(false);
  var mapperType=System.Type.GetType("RoyalThrone.Characters.KingVanguardFaceRig, Assembly-CSharp");if(mapperType!=null)actor.AddComponent(mapperType);
  var animation=rig.Skeleton.Data.FindAnimation(name);
  var board=new UnityEngine.Texture2D(1536,1024,UnityEngine.TextureFormat.RGBA32,false);
  for(int i=0;i<phases.Length;i++){
   rig.AnimationState.ClearTracks();rig.Skeleton.SetToSetupPose();rig.AnimationState.SetAnimation(0,name,false);rig.Update(animation.Duration*phases[i]);
   foreach(string hidden in new[]{"Frame Background3","sol frame","sag frame"})rig.Skeleton.FindSlot(hidden).Attachment=null;
   rig.LateUpdate();camera.Render();UnityEngine.RenderTexture.active=target;
   var image=new UnityEngine.Texture2D(512,512,UnityEngine.TextureFormat.RGBA32,false);image.ReadPixels(new UnityEngine.Rect(0,0,512,512),0,0);image.Apply();
   board.SetPixels((i%3)*512,(1-i/3)*512,512,512,image.GetPixels());
   if(i==2)System.IO.File.WriteAllBytes(output+"/"+name+"-close.png",image.EncodeToPNG());
   UnityEngine.Object.DestroyImmediate(image);
  }
  board.Apply();System.IO.File.WriteAllBytes(output+"/"+name+".png",board.EncodeToPNG());UnityEngine.Object.DestroyImmediate(board);
  report.AppendLine(name+" duration "+animation.Duration);
  UnityEngine.Object.DestroyImmediate(actor);
 }
 camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);
}finally{UnityEngine.RenderTexture.active=previous;UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
System.IO.File.WriteAllText(output+"/face-review.txt",report.ToString());return report.ToString();
