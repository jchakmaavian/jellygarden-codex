using System;
using System.IO;
using Spine.Unity;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using RoyalThrone.Characters;
public static class KingLaughterReview {
 public static void Run(){
  // Preserve the exact reference texture at its native resolution.
  KingImpactLaughChecks.Run();
  var scene=EditorSceneManager.NewPreviewScene();var prior=RenderTexture.active;
  string output=Path.GetFullPath("../JellyGarden-Codex/Design/KingVanguard-ProportionFix-20261004/Review");Directory.CreateDirectory(output);
  var cg=new GameObject("UI review camera",typeof(Camera));SceneManager.MoveGameObjectToScene(cg,scene);var cam=cg.GetComponent<Camera>();cam.scene=scene;cam.enabled=false;cam.orthographic=true;cam.orthographicSize=256;cam.transform.position=new Vector3(0,0,-1000);cam.nearClipPlane=.1f;cam.farClipPlane=2000;cam.backgroundColor=new Color(.08f,.18f,.34f,1);cam.clearFlags=CameraClearFlags.SolidColor;
  var cv=new GameObject("UI canvas",typeof(RectTransform),typeof(Canvas));SceneManager.MoveGameObjectToScene(cv,scene);var canvas=cv.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=cam;canvas.referencePixelsPerUnit=100;((RectTransform)cv.transform).sizeDelta=new Vector2(512,512);
  var rt=new RenderTexture(512,512,24);rt.Create();cam.targetTexture=rt;
  try{
   var container=new GameObject("Gameplay portrait",typeof(RectTransform),typeof(RectMask2D));container.transform.SetParent(cv.transform,false);var rect=(RectTransform)container.transform;rect.sizeDelta=new Vector2(336,350);
   var data=AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>("Assets/Characters/KingVenguard/V2/Spine/King_Vanguard_SkeletonData.asset");var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/Characters/KingVenguard/V2/Materials/KingVanguard_SkeletonGraphic_StraightAlpha.mat");
   var graphic=SkeletonGraphic.NewSkeletonGraphicGameObject(data,container.transform,mat);graphic.Initialize(true);
   // Exercise the serialized-component path, not only FaceRig.Ensure callers.
   graphic.gameObject.AddComponent<KingVanguardFaceRig>();
   if(!graphic.GetComponent<KingVanguardMouthRig>())throw new Exception("Serialized FaceRig did not bind mouth");
   var root=container.AddComponent<KingVanguardCharacter>();var so=new SerializedObject(root);so.FindProperty("skeletonGraphic").objectReferenceValue=graphic;so.ApplyModifiedPropertiesWithoutUndo();root.ResetToIdle();root.Frame(rect,KingVanguardCharacter.Framing.GameplayPortrait);
   Directory.CreateDirectory(output+"/Frames");
   int frameCount=135;
   for(int frame=0;frame<frameCount;frame++){
    if(frame==12&&!root.PlayLaughter())throw new Exception("Laugh rejected");
    graphic.Update(frame==0?0:1f/30);graphic.LateUpdate();Canvas.ForceUpdateCanvases();cam.Render();RenderTexture.active=rt;
    var im=new Texture2D(512,512,TextureFormat.RGBA32,false);im.ReadPixels(new Rect(0,0,512,512),0,0);im.Apply();
    File.WriteAllBytes(output+"/Frames/"+frame.ToString("D5")+".png",im.EncodeToPNG());UnityEngine.Object.DestroyImmediate(im);
   }
   if(root.CurrentActivity!=KingVanguardCharacter.Activity.Idle)throw new Exception("Laugh did not return to idle");
   if(!root.PlayLaughter()||root.PlayLaughter())throw new Exception("Laughter stacks");
   root.SetWorried(true);for(int i=0;i<180;i++)graphic.Update(1f/60);
   if(root.CurrentActivity!=KingVanguardCharacter.Activity.Worried)throw new Exception("Worry context lost");
   if(!root.PlayLaughter()||!root.PlaySad()||graphic.GetComponent<KingVanguardLaughter>().Active)throw new Exception("Failure priority lost");
   root.ResetToIdle();if(!root.PlayLaughter()||!root.PlayRelief()||graphic.GetComponent<KingVanguardLaughter>().Active)throw new Exception("Committed relief priority lost");
   root.ResetToIdle();if(!root.PlayLaughter())throw new Exception("Reset failed");root.ResetToIdle();
   if(graphic.GetComponent<KingVanguardLaughter>().Active)throw new Exception("Reset retained laugh");
   if(!root.PlayPowerupCelebration()||root.CurrentActivity!=KingVanguardCharacter.Activity.Happy)throw new Exception("Powerup cooldown ignored");
   string result="PASS 135 rendered laughter frames; no stacking; return to idle and worry; failure and relief interruption; reset; four-second impact laugh cooldown.";File.WriteAllText(output+"/canvas-check.txt",result);Debug.Log("FACE_CANVAS_PASS "+result);
  }catch(Exception e){File.WriteAllText(output+"/canvas-check.txt","FAIL "+e);throw;}finally{RenderTexture.active=prior;cam.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);EditorSceneManager.ClosePreviewScene(scene);}
 }
}
