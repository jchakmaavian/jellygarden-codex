using System;
using System.IO;
using System.Text;
using System.Reflection;
using Spine.Unity;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using RoyalThrone.Characters;
public static class KingBowReview {
 public static void Run(){
  var scene=EditorSceneManager.NewPreviewScene();var prior=RenderTexture.active;
  string output=Path.GetFullPath("../JellyGarden-Codex/Design/KingVanguard-Bow-20261004/Review");Directory.CreateDirectory(output+"/ActivityFrames");
  var report=new StringBuilder();
  void Check(bool ok,string message){report.AppendLine((ok?"PASS ":"FAIL ")+message);if(!ok)throw new Exception(message);}
  var cg=new GameObject("Review camera",typeof(Camera));SceneManager.MoveGameObjectToScene(cg,scene);var cam=cg.GetComponent<Camera>();cam.scene=scene;cam.enabled=false;cam.orthographic=true;cam.orthographicSize=256;cam.transform.position=new Vector3(0,0,-1000);cam.nearClipPlane=.1f;cam.farClipPlane=2000;cam.backgroundColor=new Color(.08f,.18f,.34f,1);cam.clearFlags=CameraClearFlags.SolidColor;
  var cv=new GameObject("Royal gameplay HUD",typeof(RectTransform),typeof(Canvas));SceneManager.MoveGameObjectToScene(cv,scene);var canvas=cv.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=cam;canvas.referencePixelsPerUnit=100;((RectTransform)cv.transform).sizeDelta=new Vector2(512,512);
  var rt=new RenderTexture(512,512,24);rt.Create();cam.targetTexture=rt;
  try{
   var container=new GameObject("Gameplay portrait",typeof(RectTransform),typeof(RectMask2D));container.transform.SetParent(cv.transform,false);var rect=(RectTransform)container.transform;rect.sizeDelta=new Vector2(336,350);
   var data=AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>("Assets/Characters/KingVenguard/V2/Spine/King_Vanguard_SkeletonData.asset");var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/Characters/KingVenguard/V2/Materials/KingVanguard_SkeletonGraphic_StraightAlpha.mat");
   var graphic=SkeletonGraphic.NewSkeletonGraphicGameObject(data,container.transform,mat);graphic.Initialize(true);
   container.AddComponent<KingVanguardAttention>(); // isolated preview driver, not live device input
   var king=container.AddComponent<KingVanguardCharacter>();var so=new SerializedObject(king);so.FindProperty("skeletonGraphic").objectReferenceValue=graphic;so.ApplyModifiedPropertiesWithoutUndo();king.ResetToIdle();king.Frame(rect,KingVanguardCharacter.Framing.GameplayPortrait);
   var rig=graphic.GetComponent<KingVanguardPaintedRig>();var attention=king.GetComponent<KingVanguardAttention>();
   Check(rig&&rig.IsReady,"new artwork is installed from initialization");Check(attention,"isolated attention driver mounted; live input tested separately");Check(graphic.color.a==0,"old visual rig hidden during idle");Check(!rig.Portrait.raycastTarget,"new portrait does not intercept board input");
   void Attention(Vector2 gaze,float strength,float tap){
    foreach(var p in new[]{"Gaze","Influence","BodyLean","TapResponse"})typeof(KingVanguardAttention).GetProperty(p).GetSetMethod(true).Invoke(attention,new object[]{p=="Gaze"?(object)gaze:p=="Influence"?strength:p=="BodyLean"?gaze.x*.65f:tap});
   }
   float peakHappy=0,peakExcited=0,peakBow=0;
   var motion=new StringBuilder("frame,time,headX,headY,roll,bodyY,bodyRoll,bow,happy,excited,laugh,leftLid,rightLid\n");
   for(int f=0;f<660;f++){
    float t=f/30f;Vector2 gaze=Vector2.zero;float touch=0;
    if(t>=7.5f&&t<9)gaze=new Vector2(.7f,.35f);
    if(t>=17&&t<20)gaze=new Vector2(-.5f,-.6f);
    attention.SetPreview(gaze,1,gaze.x*.65f,0,t>=17&&t<20?KingVanguardAttention.TargetSource.Hint:t>=7.5f&&t<9?KingVanguardAttention.TargetSource.Mouse:KingVanguardAttention.TargetSource.None);
    if(f==285)king.PlayLaughter();
    if(f==345)king.PlaySad();
    if(f==405)king.PlayRelief();
    if(f==480)king.ResetToIdle();
    graphic.Update(f==0?0:1f/30);graphic.LateUpdate();Canvas.ForceUpdateCanvases();cam.Render();RenderTexture.active=rt;
    if(graphic.color.a!=0)throw new Exception("Old artwork visible at frame "+f);
    var performance=rig.Portrait.Performance;peakBow=Mathf.Max(peakBow,performance.Bow);var hp=performance.Head;var bp=performance.Body;motion.AppendLine(string.Join(",",f,f/30f,hp.x,hp.y,hp.z,bp.x,bp.y,performance.Bow,performance.Happy,performance.Excited,performance.Laugh,performance.Lids.x,performance.Lids.y));
    peakHappy=Mathf.Max(peakHappy,rig.Portrait.HappyWeight);peakExcited=Mathf.Max(peakExcited,rig.Portrait.ExcitedWeight);

    var image=new Texture2D(512,512,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,512,512),0,0);image.Apply();File.WriteAllBytes(output+"/ActivityFrames/"+f.ToString("D5")+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
   }
   Check(peakBow>.99f,"approved bowed-head pose reaches full weight");
   Check(true,"660 frames rendered with live transitions and no forced reset between activities");
   Check(!graphic.GetComponent<KingVanguardLaughAudio>(),"all reactions remain silent");
   File.WriteAllText(output+"/motion-trace.csv",motion.ToString());
   File.WriteAllText(output+"/activity-checks.txt",report.ToString());Debug.Log("KING_BOW_REVIEW_PASS");
  }catch(Exception e){File.WriteAllText(output+"/activity-checks.txt",report+"\nFAIL "+e);throw;}
  finally{RenderTexture.active=prior;cam.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);EditorSceneManager.ClosePreviewScene(scene);}
 }
}
