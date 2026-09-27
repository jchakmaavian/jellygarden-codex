using RoyalThrone.Presentation;
using UnityEngine;
using UnityEngine.UI;
namespace RoyalThrone.ReferenceGameplay
{
    // Three reusable UI sprites. Motion is local to the home sky and never changes game time.
    [DefaultExecutionOrder(10010)]
    public sealed class ReferenceHomeSky : MonoBehaviour
    {
        RoyalHomeScreen home; RectTransform sky; readonly RectTransform[] clouds=new RectTransform[3];
        readonly float[] widths={.43f,.33f,.24f};
        // Heights sit .018 lower than before 2026-09-27: ReferenceHomeLayout now lifts the background to meet the card row,
        // and this keeps the clouds where the Home reference shows them.
        readonly Vector2[] seats={new Vector2(.19f,.802f),new Vector2(.80f,.722f),new Vector2(.52f,.672f)};
        readonly float[] periods={170,205,240},amplitudes={.10f,.08f,.11f},phases={0,1.6f,3.1f};
        // Drift runs 1.3x the periods above (the user's request of 2026-09-27); the path and its bounds are unchanged.
        const float DriftSpeed=1.3f;
        float elapsed;bool initialized;public int CloudCount=>initialized?3:0;
        public float MotionSeconds=>elapsed;
        public Vector2 CloudPosition(int index)=>clouds[index].anchoredPosition;
        public static void Attach(RoyalHomeScreen home){var sky=home.GetComponent<ReferenceHomeSky>();if(!sky)sky=home.gameObject.AddComponent<ReferenceHomeSky>();sky.Initialize(home);}
        void Initialize(RoyalHomeScreen owner){if(initialized)return;home=owner;
            var background=home.transform.Find("Castle framing/Royal blue courtyard");
            var art=Resources.Load<Sprite>("ReferenceGameplay/Home/Sky/Homepage_bg");if(!background||!art)return;
            var image=background.GetComponent<Image>();image.sprite=art;image.color=Color.white;
            var fitter=background.GetComponent<AspectRatioFitter>();if(fitter)fitter.aspectRatio=art.rect.width/art.rect.height;
            if(home.waterGlow)home.waterGlow.enabled=false;
            sky=(RectTransform)background;
            string[] names={"cloud_large","cloud_medium","cloud_small"};
            for(int i=0;i<3;i++){
                var r=new GameObject("Sky cloud "+(i+1),typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(sky,false);r.anchorMin=r.anchorMax=Vector2.zero;r.pivot=new Vector2(.5f,.5f);
                var cloud=r.gameObject.AddComponent<Image>();cloud.sprite=Resources.Load<Sprite>("ReferenceGameplay/Home/Sky/"+names[i]);cloud.preserveAspect=true;cloud.raycastTarget=false;cloud.color=Color.white;clouds[i]=r;
            }
            initialized=true;Render();
        }
        void LateUpdate(){if(!initialized)return;if(home.IsHomeVisible&&home.MotionEnabled)elapsed+=Time.unscaledDeltaTime;Render();}
        void Render(){float w=sky.rect.width,h=sky.rect.height;
            for(int i=0;i<3;i++){var r=clouds[i];var art=r.GetComponent<Image>().sprite;float phase=elapsed*DriftSpeed*Mathf.PI*2/periods[i]+phases[i];
                // Bounded continuous drift has no wrap, teleport, opacity pop or random jitter.
                float x=seats[i].x+amplitudes[i]*(Mathf.Sin(phase)-Mathf.Sin(phases[i]));
                float y=seats[i].y+.0025f*(Mathf.Sin(phase*.73f)-Mathf.Sin(phases[i]*.73f));
                r.anchoredPosition=new Vector2(x*w,y*h);float width=w*widths[i];r.sizeDelta=new Vector2(width,width*art.rect.height/art.rect.width);
            }
        }
    }
}
