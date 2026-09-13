using UnityEngine;
using UnityEngine.UI;
using JellyGarden.Scripts.Targets;

namespace RoyalThrone.Presentation
{
    // Target instances are created after the menu enables; keep their live fields intact.
    public sealed class RoyalMenuPresenter : MonoBehaviour
    {
        float next;
        AnimationClip retryClip;
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
        void OnDestroy(){if(retryClip)Destroy(retryClip);}
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
    }
}
