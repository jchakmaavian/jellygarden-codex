UnityEditor.AssetDatabase.Refresh(UnityEditor.ImportAssetOptions.ForceSynchronousImport);
var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
var report=new System.Text.StringBuilder();
try {
 foreach(var name in new[]{"King_Vanguard","AreaCompleted_Vanguard"}) {
  var data=UnityEditor.AssetDatabase.LoadAssetAtPath<Spine.Unity.SkeletonDataAsset>("Assets/Characters/KingVenguard/V2/Spine/"+name+"_SkeletonData.asset");
  var go=new UnityEngine.GameObject(name);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,scene);
  var rig=go.AddComponent<Spine.Unity.SkeletonAnimation>();rig.skeletonDataAsset=data;rig.Initialize(false);
  string animation=name.StartsWith("King")?"Happy_2":"king_drill_win_clap_loop";
  rig.AnimationState.SetAnimation(0,animation,false);rig.Update(name.StartsWith("King")?2.2f:.5f);rig.LateUpdate();
  float x,y,w,h;float[] verts=null;rig.Skeleton.GetBounds(out x,out y,out w,out h,ref verts);
  var camGo=new UnityEngine.GameObject("Inspection camera",typeof(UnityEngine.Camera));UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(camGo,scene);
  var cam=camGo.GetComponent<UnityEngine.Camera>();cam.scene=scene;cam.orthographic=true;cam.orthographicSize=UnityEngine.Mathf.Max(h,w)*.58f;
  cam.transform.position=new UnityEngine.Vector3(x+w*.5f,y+h*.5f,-20);cam.clearFlags=UnityEngine.CameraClearFlags.SolidColor;cam.backgroundColor=new UnityEngine.Color(.18f,.22f,.32f,1);cam.nearClipPlane=.1f;cam.farClipPlane=100;
  var rt=new UnityEngine.RenderTexture(800,800,24,UnityEngine.RenderTextureFormat.ARGB32);cam.targetTexture=rt;cam.Render();
  var previous=UnityEngine.RenderTexture.active;UnityEngine.RenderTexture.active=rt;var tex=new UnityEngine.Texture2D(800,800,UnityEngine.TextureFormat.RGBA32,false);tex.ReadPixels(new UnityEngine.Rect(0,0,800,800),0,0);tex.Apply();UnityEngine.RenderTexture.active=previous;
  var path="Design/KingVanguard-Reference-20260929/"+name+"-first-render.png";System.IO.File.WriteAllBytes(path,tex.EncodeToPNG());report.AppendLine(path+" bounds "+x+","+y+","+w+","+h);
  UnityEngine.Object.DestroyImmediate(tex);cam.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(camGo);UnityEngine.Object.DestroyImmediate(go);
 }
} finally {UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
return report.ToString();
