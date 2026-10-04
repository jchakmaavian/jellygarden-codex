using Spine.Unity;
using UnityEngine;

namespace RoyalThrone.Characters {
 // The approved painting is the expression: no separately redrawn eyes, teeth or beard.
 [DisallowMultipleComponent]
 public sealed class KingVanguardLaughArt : MonoBehaviour {
  SkeletonGraphic graphic;
  KingVanguardLaughter laugh;
  KingVanguardLaughPortrait portrait;
  Color normalColor;
  bool bound, showing;
  public bool IsShowing => showing;
  public int RenderedPartCount => showing ? 1 : 0;
  public float JawDrop => portrait ? portrait.JawOffset : 0;
  public KingVanguardLaughPortrait Portrait => portrait;
  public static KingVanguardLaughArt Ensure(GameObject go) {
   var art=go.GetComponent<KingVanguardLaughArt>();if(!art)art=go.AddComponent<KingVanguardLaughArt>();art.Bind();return art;
  }
  void OnEnable(){Bind();}
  void Bind(){
   if(bound)return;
   graphic=GetComponent<SkeletonGraphic>();laugh=GetComponent<KingVanguardLaughter>();
   if(!graphic||!laugh)return;
   normalColor=graphic.color;graphic.UpdateComplete+=Pose;bound=true;
  }
  void OnDisable(){Cancel();if(bound&&graphic)graphic.UpdateComplete-=Pose;bound=false;}
  void OnDestroy(){if(portrait){if(Application.isPlaying)Destroy(portrait.gameObject);else DestroyImmediate(portrait.gameObject);}}
  public void Cancel(){
   if(showing&&graphic)graphic.color=normalColor;
   showing=false;if(portrait)portrait.gameObject.SetActive(false);
  }
  void Pose(ISkeletonAnimation unused){
   if(!laugh.Active){Cancel();return;}
   if(!portrait){
    var go=new GameObject("King reference laugh",typeof(RectTransform),typeof(CanvasRenderer),typeof(KingVanguardLaughPortrait));
    go.transform.SetParent(graphic.transform.parent,false);
    var rt=(RectTransform)go.transform;rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=rt.offsetMax=Vector2.zero;
    portrait=go.GetComponent<KingVanguardLaughPortrait>();portrait.raycastTarget=false;
   }
   if(!showing)normalColor=graphic.color;
   float alpha=KingVanguardLaughTiming.PortraitOpacity(laugh.Time);
   showing=alpha>0;
   portrait.gameObject.SetActive(showing);
   if(showing)portrait.SetPose(laugh.Time,alpha);
   graphic.color=new Color(normalColor.r,normalColor.g,normalColor.b,normalColor.a*(1-alpha));
  }
 }
}
