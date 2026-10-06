using System.Collections.Generic;
using Spine;
using Spine.Unity;
using UnityEngine;

namespace RoyalThrone.Characters {
 // Replace only facial attachments. The existing crown, cape and shoulders keep their rig and scale.
 [DisallowMultipleComponent]
 public sealed class KingVanguardLaughArt : MonoBehaviour {
  SkeletonGraphic graphic;
  KingVanguardLaughter laugh;
  KingVanguardLaughPortrait portrait;
  readonly List<Slot> hidden=new List<Slot>();
  readonly List<Attachment> originals=new List<Attachment>();
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
   if(bound)return;graphic=GetComponent<SkeletonGraphic>();laugh=GetComponent<KingVanguardLaughter>();
   if(!graphic||!laugh)return;
   graphic.UpdateLocal+=BeforePose;graphic.UpdateComplete+=Pose;bound=true;
  }
  void OnDisable(){Cancel();if(bound&&graphic){graphic.UpdateLocal-=BeforePose;graphic.UpdateComplete-=Pose;}bound=false;}
  void OnDestroy(){if(portrait){if(Application.isPlaying)Destroy(portrait.gameObject);else DestroyImmediate(portrait.gameObject);}}
  void BeforePose(ISkeletonAnimation unused){RestoreAttachments();}
  void RestoreAttachments(){
   for(int i=0;i<hidden.Count;i++)if(hidden[i].Attachment==null)hidden[i].Attachment=originals[i];
   hidden.Clear();originals.Clear();
  }
  public void Cancel(){RestoreAttachments();showing=false;if(portrait)portrait.gameObject.SetActive(false);}
  void Pose(ISkeletonAnimation unused){
   if(!laugh.Active){Cancel();return;}
   if(!portrait){
    var go=new GameObject("King fitted laugh face",typeof(RectTransform),typeof(CanvasRenderer),typeof(KingVanguardLaughPortrait));
    go.transform.SetParent(graphic.transform.parent,false);
    var rt=(RectTransform)go.transform;rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=rt.offsetMax=Vector2.zero;
    portrait=go.GetComponent<KingVanguardLaughPortrait>();portrait.raycastTarget=false;portrait.Configure(graphic,laugh);
   }
   showing=KingVanguardLaughTiming.PortraitOpacity(laugh.Time)>0;
   portrait.gameObject.SetActive(showing);
   if(!showing){RestoreAttachments();return;}
   // Fixed source skeleton: 4..24 are ears/face/beard/eyes/mouth; 28..31 are brows.
   // 1 is the original body; 25..27 are its fitted crown and contact shadows.
   foreach(var slot in graphic.Skeleton.Slots){int n=slot.Data.Index;if((n>=4&&n<=24)||(n>=28&&n<=31)){
    if(slot.Attachment!=null){hidden.Add(slot);originals.Add(slot.Attachment);slot.Attachment=null;}
   }}
   portrait.SetPose(laugh.Time,1);
  }
 }
}
