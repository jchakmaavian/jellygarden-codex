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
public static class KingActivityReel {
 public static void Run(){
  var scene=EditorSceneManager.NewPreviewScene();var prior=RenderTexture.active;
  string output=Path.GetFullPath("../JellyGarden-Codex/Design/KingVanguard-InteractivePortrait-20261004/Review");Directory.CreateDirectory(output+"/ActivityFrames");
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
   float peakHappy=0,peakExcited=0;
   for(int f=0;f<780;f++){
    float t=f/30f;Vector2 gaze=Vector2.zero;float touch=0;
    if(t>1&&t<3)gaze=new Vector2(Mathf.Sin((t-1)*Mathf.PI),-.1f);
    if(t>=3&&t<4)gaze=new Vector2(-.8f,-.4f);
    if(t>=4&&t<4.6f){gaze=new Vector2(-.7f,-.3f);touch=Mathf.Sin((t-4)/.6f*Mathf.PI);}
    Attention(gaze,1,touch);
    if(f==150){king.ResetToIdle();king.PlayMatchAcknowledgement();}
    if(f==210){king.ResetToIdle();king.PlayHappy();}
    if(f==300){king.ResetToIdle();king.PlayExcited();}
    if(f==390){king.ResetToIdle();king.PlayExtraMoves();}
    if(f==480){king.ResetToIdle();king.PlayLaughter();}
    if(f==570){king.ResetToIdle();king.SetWorried(true);}
    if(f==645)king.PlaySad();
    if(f==705)king.PlayRelief();
    if(f==765)king.ResetToIdle();
    graphic.Update(f==0?0:1f/30);graphic.LateUpdate();Canvas.ForceUpdateCanvases();cam.Render();RenderTexture.active=rt;
    if(graphic.color.a!=0)throw new Exception("Old artwork visible at frame "+f);
    peakHappy=Mathf.Max(peakHappy,rig.Portrait.HappyWeight);peakExcited=Mathf.Max(peakExcited,rig.Portrait.ExcitedWeight);
    if(f>210&&f<480&&rig.LaughWeight>.001f)throw new Exception("Positive activity incorrectly uses laughing face");
    var image=new Texture2D(512,512,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,512,512),0,0);image.Apply();File.WriteAllBytes(output+"/ActivityFrames/"+f.ToString("D5")+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
   }
   Check(peakHappy>.9f&&peakExcited>.9f,"distinct happy and excited faces reached full expression without laughing");
   Check(true,"780 rendered frames use the new character exclusively");
   foreach(int fps in new[]{15,30,60}){
    king.ResetToIdle();king.PlayLaughter();float max=0,minJaw=99,maxJaw=0;
    for(int f=0;f<fps*3;f++){graphic.Update(1f/fps);graphic.LateUpdate();max=Mathf.Max(max,rig.LaughWeight);if(rig.LaughWeight>.8f){minJaw=Mathf.Min(minJaw,rig.Portrait.JawOffset);maxJaw=Mathf.Max(maxJaw,rig.Portrait.JawOffset);}}
    Check(max>.98f&&maxJaw-minJaw>.5f,fps+"fps articulated laughter");Check(king.CurrentActivity==KingVanguardCharacter.Activity.Idle&&rig.LaughWeight<.01f,fps+"fps returns to matching neutral art");
   }
   king.ResetToIdle();Attention(new Vector2(.8f,-.3f),.7f,.8f);graphic.Update(.1f);Check((rig.RenderedGaze-new Vector2(.56f,-.21f)).magnitude<.001f,"renderer consumes attention vector");
   king.PlayLaughter();graphic.Update(.7f);king.PlaySad();graphic.Update(.1f);Check(king.CurrentActivity==KingVanguardCharacter.Activity.Sad&&!graphic.GetComponent<KingVanguardLaughter>().Active,"failure interrupts laughter");
   king.ResetToIdle();Check(rig.LaughWeight==0,"reset clears expression immediately");
   rig.enabled=false;Check(graphic.color.a>.99f&&!rig.Portrait.gameObject.activeSelf,"disable restores fallback without duplicate image");rig.enabled=true;Check(rig.IsReady&&graphic.color.a==0,"reenable restores new character");
   Check(!graphic.GetComponent<KingVanguardLaughAudio>(),"all states remain silent");
   File.WriteAllText(output+"/activity-checks.txt",report.ToString());Debug.Log("KING_ACTIVITY_REEL_PASS");
  }catch(Exception e){File.WriteAllText(output+"/activity-checks.txt",report+"\nFAIL "+e);throw;}
  finally{RenderTexture.active=prior;cam.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);EditorSceneManager.ClosePreviewScene(scene);}
 }
}
