using Spine;
using Spine.Unity;
using UnityEngine;

namespace RoyalThrone.Characters {
 // One artwork family for every portrait state; gameplay still owns activity and attention.
 [ExecuteAlways, DisallowMultipleComponent]
 public sealed class KingVanguardPaintedRig : MonoBehaviour {
  SkeletonGraphic graphic; KingVanguardCharacter owner; KingVanguardAttention attention;
  KingVanguardPaintedGraphic portrait; TrackEntry previousTrack;
  float previousTime, clock; Color originalColor; bool bound, originalUnscaledTime;
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
   attention=KingVanguardAttention.Ensure(graphic);
   graphic.UpdateComplete+=Pose;bound=true;portrait.gameObject.SetActive(true);ResetPresentation();
  }
  public void ResetPresentation(){clock=0;previousTrack=null;previousTime=0;if(portrait)portrait.ApplyPose(owner.CurrentActivity,0,0,0,Vector2.zero,0,0,0,true);}
  void Pose(ISkeletonAnimation unused){
   if(!bound||!portrait)return;
   var track=graphic.AnimationState.GetCurrent(0);float delta=0;
   if(track!=null){if(ReferenceEquals(previousTrack,track))delta=Mathf.Clamp((track.TrackTime-previousTime)/Mathf.Max(.001f,track.TimeScale),0,.1f);previousTime=track.TrackTime;}
   previousTrack=track;clock+=delta;
   if(!attention)attention=KingVanguardAttention.Ensure(graphic);
   var gaze=attention?attention.Gaze*attention.Influence:Vector2.zero;
   var laugh=graphic.GetComponent<KingVanguardLaughter>();
   float laughTime=laugh&&laugh.Active?laugh.Time:0;
   float phase=track!=null?Mathf.Clamp01(track.AnimationTime/Mathf.Max(.01f,track.Animation.Duration)):0;
   portrait.ApplyPose(owner.CurrentActivity,clock,delta,phase,gaze,attention?attention.BodyLean:0,attention?attention.TapResponse:0,laughTime,false);
  }
  void OnDisable(){
   if(bound&&graphic){graphic.GetComponent<KingVanguardLaughter>()?.Cancel();graphic.UpdateComplete-=Pose;graphic.color=originalColor;graphic.unscaledTime=originalUnscaledTime;}
   bound=false;previousTrack=null;if(portrait)portrait.gameObject.SetActive(false);
  }
  void OnDestroy(){if(portrait){if(Application.isPlaying)Destroy(portrait.gameObject);else DestroyImmediate(portrait.gameObject);}}
 }
}
