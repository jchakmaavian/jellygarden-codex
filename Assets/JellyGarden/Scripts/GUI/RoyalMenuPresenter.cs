using PlayerPrefs = RoyalThrone.Testing.RoyalSave;
using UnityEngine;
using UnityEngine.UI;
using JellyGarden.Scripts.Targets;
using JellyGarden.Scripts.Juice;

namespace RoyalThrone.Presentation
{
    // Target instances are created after the menu enables; keep their live fields intact.
    public sealed class RoyalMenuPresenter : MonoBehaviour
    {
        float next;
        AnimationClip retryClip,prePlayClip;
        public void ConfigurePrePlay()
        {
            var root=transform as RectTransform;var banner=transform.Find("Banner");if(!root||!banner)return;
            root.anchorMin=Vector2.zero;root.anchorMax=Vector2.one;root.offsetMin=root.offsetMax=Vector2.zero;
            // Keep the castle visible throughout the transition. The inherited scene
            // had no fullscreen background here, leaving a black frame around its banner.
            var castle=RoyalMenuSkin.Art(root,"Royal castle journey","home-castle",0,0,720,1280);
            var cr=castle.rectTransform;cr.anchorMin=Vector2.zero;cr.anchorMax=Vector2.one;cr.offsetMin=cr.offsetMax=Vector2.zero;castle.preserveAspect=false;
            var fitter=castle.gameObject.AddComponent<AspectRatioFitter>();fitter.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;
            if(castle.sprite)fitter.aspectRatio=castle.sprite.rect.width/castle.sprite.rect.height;
            castle.transform.SetAsFirstSibling();
            var veil=new GameObject("Royal journey shade",typeof(RectTransform),typeof(Image));veil.transform.SetParent(root,false);veil.transform.SetSiblingIndex(1);
            var vr=(RectTransform)veil.transform;vr.anchorMin=Vector2.zero;vr.anchorMax=Vector2.one;vr.offsetMin=vr.offsetMax=Vector2.zero;veil.GetComponent<Image>().color=new Color(.015f,.06f,.17f,.5f);veil.GetComponent<Image>().raycastTarget=false;
            foreach(var group in GetComponentsInChildren<TargetsGroup>(true)){
                group.transform.SetParent(banner,false);RoyalMenuSkin.Place(group.transform,0,-23,520,110);
            }
            foreach(Transform child in root)if(child!=banner&&child!=castle.transform&&child!=veil.transform)Hide(child);
            foreach(Transform child in banner)if(!child.GetComponent<TargetsGroup>())Hide(child);
            RoyalMenuSkin.Place(banner,0,0,620,350);RoyalMenuSkin.Surface(banner);
            RoyalMenuSkin.Label(banner,"Royal heading","YOUR GOALS",0,108,540,55,35);
            RoyalMenuSkin.Label(banner,"Royal journey caption","Make every move count",0,-126,530,40,22);
            var animation=GetComponent<Animation>();if(!animation||!animation.clip)return;
            prePlayClip=Instantiate(animation.clip);prePlayClip.name="Royal goals journey";prePlayClip.ClearCurves();
            prePlayClip.SetCurve("Banner",typeof(RectTransform),"m_AnchoredPosition.y",new AnimationCurve(new Keyframe(0,-720),new Keyframe(.28f,10),new Keyframe(.38f,0),new Keyframe(1.70f,0),new Keyframe(2.05f,760)));
            prePlayClip.events=new[]{new AnimationEvent{time=.18f,functionName="SwishSound"},new AnimationEvent{time=1.8f,functionName="SwishSound"},new AnimationEvent{time=2.08f,functionName="OnFinished"}};
            animation.AddClip(prePlayClip,prePlayClip.name);animation.clip=prePlayClip;
        }
        static void Hide(Transform tr){var cg=tr.GetComponent<CanvasGroup>();if(!cg)cg=tr.gameObject.AddComponent<CanvasGroup>();cg.alpha=0;cg.blocksRaycasts=false;}
        public void ConfigureRetryMotion()
        {
            var animation=GetComponent<Animation>();if(!animation)return;
            var original=animation.GetClip("bannerFailed");if(!original)return;
            // Retain the wait/continue/finish events while shortening the visual entrance.
            retryClip=Instantiate(original);retryClip.name="bannerFailed";
            retryClip.SetCurve("Banner",typeof(RectTransform),"m_AnchoredPosition.y",new AnimationCurve(new Keyframe(0,850),new Keyframe(.28f,0),new Keyframe(2.46f,0),new Keyframe(3,1000)));
            foreach(string name in new[]{"Buy","GiveUp"}){
                float direction=name=="Buy"?1:-1;
                retryClip.SetCurve(name,typeof(RectTransform),"m_AnchoredPosition.x",new AnimationCurve(new Keyframe(0,direction*850),new Keyframe(.38f,0),new Keyframe(2.46f,0),new Keyframe(3,direction*1000)));
            }
            animation.AddClip(retryClip,"bannerFailed");animation.clip=retryClip;
        }
        void OnDestroy(){if(retryClip)Destroy(retryClip);if(prePlayClip)Destroy(prePlayClip);}
        void OnEnable()
        {
            next=0;
            var level=JellyGarden.Scripts.LevelManager.THIS;
            if(name=="PreFailed"&&level){
                var animation=GetComponent<Animation>();
                if(animation&&animation["bannerFailed"]!=null)animation["bannerFailed"].speed=1;
                var message=transform.Find("Banner/Royal retry text");
                if(message)message.GetComponent<Text>().text=level.limitType==JellyGarden.Scripts.LIMIT.MOVES
                    ?level.ExtraFailedMoves+" extra moves to finish your goals"
                    :level.ExtraFailedSecs+" extra seconds to finish your goals";
            }
        }
        void LateUpdate()
        {
            if(Time.unscaledTime<next)return;next=Time.unscaledTime+.15f;
            if(name=="MenuPause"||name=="Settings"){
                SetCaption("Image/Music/Royal caption","MUSIC "+(PlayerPrefs.GetInt("Music",1)!=0?"ON":"OFF"));
                SetCaption("Image/Sound/Royal caption","SFX "+(PlayerPrefs.GetInt("Sound",1)!=0?"ON":"OFF"));
                SetCaption("Image/Hints/Royal caption","MATCHING HINTS "+(RoyalHintSettings.Enabled?"ON":"OFF"));
            }
            foreach(var group in GetComponentsInChildren<TargetsGroup>(true)){
                foreach(var layout in group.GetComponents<LayoutGroup>())layout.enabled=false;
                var targets=group.GetComponentsInChildren<TargetIcon>();
                for(int i=0;i<targets.Length;i++){
                    var target=targets[i];RoyalMenuSkin.Place(target.transform,(i-(targets.Length-1)*.5f)*150,8,128,90);
                    RoyalMenuSkin.Typography(target.textObj,29);RoyalMenuSkin.Place(target.textObj.transform,0,-33,132,40);
                    if(target.imageRenderer){RoyalMenuSkin.Place(target.imageRenderer.transform,0,16,58,58);target.imageRenderer.preserveAspect=true;}
                    if(target.checkObject)RoyalMenuSkin.Place(target.checkObject.transform,0,-32,32,32);
                }
            }
        }
        void SetCaption(string path,string value){var tr=transform.Find(path);if(tr){var text=tr.GetComponent<Text>();if(text)text.text=value;}}
    }
}
