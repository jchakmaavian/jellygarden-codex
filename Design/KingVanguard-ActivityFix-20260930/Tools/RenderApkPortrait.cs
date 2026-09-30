// Render the untouched APK skeleton, original texture and original frame for comparison.
var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
var allocated=new System.Collections.Generic.List<UnityEngine.Object>();
var savedActive=UnityEngine.RenderTexture.active;
try{
 var texture=new UnityEngine.Texture2D(2,2,UnityEngine.TextureFormat.RGBA32,false);allocated.Add(texture);
 UnityEngine.ImageConversion.LoadImage(texture,System.IO.File.ReadAllBytes("Design/KingVanguard-Reference-20260929/Original-rfv387/King.png"));
 texture.name="King";texture.wrapMode=UnityEngine.TextureWrapMode.Clamp;texture.filterMode=UnityEngine.FilterMode.Bilinear;
 var material=new UnityEngine.Material(UnityEngine.Shader.Find("Spine/Skeleton"));allocated.Add(material);material.name="King";material.mainTexture=texture;
 var atlasText=new UnityEngine.TextAsset(System.IO.File.ReadAllText("Design/KingVanguard-Reference-20260929/Original-rfv387/King.atlas"));allocated.Add(atlasText);
 var atlas=UnityEngine.ScriptableObject.CreateInstance<Spine.Unity.SpineAtlasAsset>();allocated.Add(atlas);atlas.atlasFile=atlasText;atlas.materials=new[]{material};
 var data=UnityEngine.ScriptableObject.CreateInstance<Spine.Unity.SkeletonDataAsset>();allocated.Add(data);
 data.skeletonJSON=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.TextAsset>("Assets/Characters/KingVenguard/V2/Spine/King_Vanguard.skel.bytes");
 data.atlasAssets=new Spine.Unity.AtlasAssetBase[]{atlas};data.scale=.01f;
 var go=new UnityEngine.GameObject("APK King untouched");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,scene);
 var rig=go.AddComponent<Spine.Unity.SkeletonAnimation>();rig.skeletonDataAsset=data;rig.Initialize(false);
 rig.AnimationState.SetAnimation(0,"idle_loop_1",false);rig.Update(1.5f);rig.LateUpdate();
 var cg=new UnityEngine.GameObject("Source Camera",typeof(UnityEngine.Camera));UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cg,scene);
 var camera=cg.GetComponent<UnityEngine.Camera>();camera.scene=scene;camera.orthographic=true;camera.orthographicSize=2.3f;
 camera.transform.position=new UnityEngine.Vector3(0,2.05f,-20);camera.clearFlags=UnityEngine.CameraClearFlags.SolidColor;camera.backgroundColor=new UnityEngine.Color(.04f,.1f,.22f,1);
 var target=new UnityEngine.RenderTexture(512,512,24);allocated.Add(target);camera.targetTexture=target;camera.Render();
 UnityEngine.RenderTexture.active=target;var capture=new UnityEngine.Texture2D(512,512,UnityEngine.TextureFormat.RGB24,false);allocated.Add(capture);
 capture.ReadPixels(new UnityEngine.Rect(0,0,512,512),0,0);capture.Apply();
 System.IO.File.WriteAllBytes("Design/KingVanguard-ActivityFix-20260930/Before/APK-original-portrait.png",capture.EncodeToPNG());
 camera.targetTexture=null;target.Release();
}finally{UnityEngine.RenderTexture.active=savedActive;UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);foreach(var item in allocated)UnityEngine.Object.DestroyImmediate(item);}
return "Rendered original APK portrait and frame without Vanguard modifiers.";
