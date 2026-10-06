using Spine;
using Spine.Unity;
using UnityEngine;

namespace RoyalThrone.Characters {
 // One artwork family for every portrait state; gameplay still owns activity and attention.
 [ExecuteAlways, DisallowMultipleComponent]
 public sealed class KingVanguardPaintedRig : MonoBehaviour {
  SkeletonGraphic graphic; KingVanguardCharacter owner; KingVanguardAttention attention;
  KingVanguardPaintedGraphic portrait;
  float previousSkeletonTime, clock; Color originalColor; bool bound, originalUnscaledTime;
  float attentionCheckedAt; UpdateMode originalUpdateMode=UpdateMode.FullUpdate;
  public KingVanguardPaintedGraphic Portrait => portrait;
  public bool IsReady => portrait && portrait.Ready;
  public Vector2 RenderedGaze => portrait ? portrait.Gaze : Vector2.zero;
  public float LaughWeight => portrait ? portrait.LaughWeight : 0;
  public static KingVanguardPaintedRig Ensure(SkeletonGraphic target, KingVanguardCharacter actor) {
   var rig=target.GetComponent<KingVanguardPaintedRig>();
   if(!rig)rig=target.gameObject.AddComponent<KingVanguardPaintedRig>();
   rig.owner=actor;rig.Bind();return rig;
  }
  void OnEnable(){Bind();}
  void Bind(){
   if(bound)return;graphic=GetComponent<SkeletonGraphic>();
   if(!owner)owner=GetComponentInParent<KingVanguardCharacter>();
   if(!graphic||!owner||owner.IsAreaRig)return;
   if(!portrait){
    var go=new GameObject("King Vanguard interactive artwork",typeof(RectTransform),typeof(CanvasRenderer),typeof(KingVanguardPaintedGraphic));
    go.transform.SetParent(graphic.transform.parent,false);
    var rect=(RectTransform)go.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
    portrait=go.GetComponent<KingVanguardPaintedGraphic>();portrait.raycastTarget=false;
   }
   if(!portrait.InitializeArtwork())return; // Keep the existing rig visible if an import is incomplete.
   var old=graphic.GetComponent<KingVanguardLaughArt>();if(old){old.Cancel();old.enabled=false;}
   originalUnscaledTime=graphic.unscaledTime;graphic.unscaledTime=false;
   originalColor=graphic.color;graphic.color=new Color(originalColor.r,originalColor.g,originalColor.b,0);
   attention=KingVanguardAttention.Ensure(graphic);attentionCheckedAt=Time.unscaledTime;
   // The rig is invisible under the painted portrait, which reads only its clock, track phase and bones: it keeps animating
   // but builds no mesh (its mesh, face and mouth rigs ran every frame for nothing; bug review 2026-10-05).
   originalUpdateMode=graphic.UpdateMode;graphic.UpdateMode=UpdateMode.EverythingExceptMesh;
   graphic.UpdateComplete+=Pose;bound=true;portrait.gameObject.SetActive(true);ResetPresentation();
  }
  public void ResetPresentation(){clock=0;previousSkeletonTime=graphic&&graphic.Skeleton!=null?graphic.Skeleton.Time:0;if(portrait)portrait.ApplyPose(owner.CurrentActivity,0,0,0,Vector2.zero,0,0,0,true);}
  void Pose(ISkeletonAnimation unused){
   if(!bound||!portrait)return;
   var track=graphic.AnimationState.GetCurrent(0);
   // Skeleton time is continuous across clip swaps and worry loops; track phase is not.
   float now=graphic.Skeleton.Time;
   float delta=Mathf.Clamp(now-previousSkeletonTime,0,.1f);previousSkeletonTime=now;clock+=delta;
   // Becoming visible resets a SkeletonGraphic to a full update; the hidden rig goes back to no mesh.
   if(graphic.UpdateMode==UpdateMode.FullUpdate)graphic.UpdateMode=UpdateMode.EverythingExceptMesh;
   // Only the gameplay HUD's king has attention: look again at most once a second, not every frame (each look walks the
   // parents' names, which allocates).
   if(!attention&&Time.unscaledTime-attentionCheckedAt>=1){attention=KingVanguardAttention.Ensure(graphic);attentionCheckedAt=Time.unscaledTime;}
   var gaze=attention?attention.Gaze*attention.Influence:Vector2.zero;
   var laugh=graphic.GetComponent<KingVanguardLaughter>();
   float laughTime=laugh&&laugh.Active?laugh.Time:0;
   float phase=track!=null?Mathf.Clamp01(track.AnimationTime/Mathf.Max(.01f,track.Animation.Duration)):0;
   portrait.ApplyPose(owner.CurrentActivity,clock,delta,phase,gaze,attention?attention.BodyLean:0,attention?attention.TapResponse:0,laughTime,false,attention?(int)attention.Source:0);
  }
  void OnDisable(){
   if(bound&&graphic){graphic.GetComponent<KingVanguardLaughter>()?.Cancel();graphic.UpdateComplete-=Pose;graphic.color=originalColor;graphic.unscaledTime=originalUnscaledTime;graphic.UpdateMode=originalUpdateMode;}
   bound=false;if(portrait)portrait.gameObject.SetActive(false);
  }
  void OnDestroy(){if(portrait){if(Application.isPlaying)Destroy(portrait.gameObject);else DestroyImmediate(portrait.gameObject);}}
 }
}
