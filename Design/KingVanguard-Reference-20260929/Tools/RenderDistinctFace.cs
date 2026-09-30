var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
var output="Design/KingVanguard-Reference-20260929/FaceRedesign/Review";System.IO.Directory.CreateDirectory(output);
var board=new UnityEngine.Texture2D(1024,1024,UnityEngine.TextureFormat.RGBA32,false);
var savedActive=UnityEngine.RenderTexture.active;
try {
 string[] clips={"idle_loop_1","k-1sevinme_6","k-1_kaygi_loop","k-1_uzulme"};
 string[] labels={"Idle","Happy","Worried","Sad"};
 float[] times={1.5f,2.4f,2f,2.8f};
 for(int i=0;i<clips.Length;i++){
  var go=new UnityEngine.GameObject("Vanguard "+labels[i]);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,scene);
  var rig=go.AddComponent<Spine.Unity.SkeletonAnimation>();
  rig.skeletonDataAsset=UnityEditor.AssetDatabase.LoadAssetAtPath<Spine.Unity.SkeletonDataAsset>("Assets/Characters/KingVenguard/V2/Spine/King_Vanguard_SkeletonData.asset");
  rig.Initialize(false);rig.AnimationState.SetAnimation(0,clips[i],false);rig.Update(times[i]);
  foreach(var name in new[]{"Frame Background3","sol frame","sag frame"}){var slot=rig.Skeleton.FindSlot(name);if(slot!=null)slot.Attachment=null;}
  rig.LateUpdate();
  var cameraGo=new UnityEngine.GameObject("Preview",typeof(UnityEngine.Camera));UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraGo,scene);
  var camera=cameraGo.GetComponent<UnityEngine.Camera>();camera.scene=scene;camera.orthographic=true;camera.orthographicSize=2.3f;
  camera.transform.position=new UnityEngine.Vector3(0,2.05f,-20);camera.clearFlags=UnityEngine.CameraClearFlags.SolidColor;
  camera.backgroundColor=new UnityEngine.Color(.065f,.11f,.19f,1);camera.nearClipPlane=.1f;camera.farClipPlane=50;
  var target=new UnityEngine.RenderTexture(512,512,24,UnityEngine.RenderTextureFormat.ARGB32);camera.targetTexture=target;camera.Render();
  UnityEngine.RenderTexture.active=target;var texture=new UnityEngine.Texture2D(512,512,UnityEngine.TextureFormat.RGBA32,false);
  texture.ReadPixels(new UnityEngine.Rect(0,0,512,512),0,0);texture.Apply();
  System.IO.File.WriteAllBytes(output+"/"+labels[i]+".png",texture.EncodeToPNG());
  board.SetPixels((i%2)*512,(1-i/2)*512,512,512,texture.GetPixels());
  UnityEngine.RenderTexture.active=savedActive;camera.targetTexture=null;target.Release();
  UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(cameraGo);UnityEngine.Object.DestroyImmediate(go);
 }
 board.Apply();System.IO.File.WriteAllBytes(output+"/New-face-expressions.png",board.EncodeToPNG());
} finally {
 UnityEngine.RenderTexture.active=savedActive;UnityEngine.Object.DestroyImmediate(board);
 UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
}
return output+"/New-face-expressions.png";
