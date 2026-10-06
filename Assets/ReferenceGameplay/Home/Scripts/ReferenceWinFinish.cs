using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JellyGarden.Scripts;
using JellyGarden.Scripts.Juice;
using UnityEngine;
using UnityEngine.UI;
namespace RoyalThrone.ReferenceGameplay
{
    // Reconstructed orchestration from packaged RemainingMovesItemReplacer/Exploder.
    // Native particle modules are imported; board damage and all booster sequences stay existing.
    public sealed class ReferenceWinFinish : MonoBehaviour
    {
        public int Sent {get;private set;} public int Converted {get;private set;} public int Fired {get;private set;}
        public bool Running {get;private set;} int travelling; LevelManager lm; Text skip; readonly List<ReferenceVfxPool.Handle> leases=new List<ReferenceVfxPool.Handle>();
        public static bool Power(Item i)=>i&&!i.destroying&&(i.currentType==ItemsTypes.HORIZONTAL_STRIPPED||i.currentType==ItemsTypes.VERTICAL_STRIPPED||i.currentType==ItemsTypes.PACKAGE||i.currentType==ItemsTypes.BOMB||i.currentType==ItemsTypes.HELICOPTER);
        List<Item> Regular()=>lm.GetItems().Where(i=>i&&!i.destroying&&!i.falling&&i.currentType==ItemsTypes.NONE&&i.NextType==ItemsTypes.NONE&&i.square&&i.square.CanGoOut()&&!RoyalThrone.Campaign.RoyalCampaignBlockers.IsSpecialItem(i)).ToList();
        bool Active=>lm&&lm.gameStatus==GameState.PreWinAnimations;
        public static IEnumerator Play(LevelManager owner){var view=owner.GetComponent<ReferenceWinFinish>();if(!view)view=owner.gameObject.AddComponent<ReferenceWinFinish>();yield return view.Run(owner);}
        IEnumerator Delay(float normal,float fast){float t=0;while(Active&&t<(lm.SkipWinCelebration?fast:normal)){t+=Time.deltaTime;yield return null;}}
        // The board resolves after each volley: what the boosters cleared falls and refills, cascades included (at most 6 s).
        IEnumerator Settle(){lm.FindMatches();float until=Time.unscaledTime+6;do{yield return null;}while(Active&&Time.unscaledTime<until&&(lm.IsResolvingMatches||RoyalPowerupDirector.Busy||lm.GetItems().Any(i=>i&&(i.destroying||i.falling))));}
        IEnumerator Run(LevelManager owner){lm=owner;Sent=Converted=Fired=travelling=0;Running=true;
            try{
                // The booster session after the user's Royal Match Level 13 clip (Design/RoyalMatchReference/OBSERVATIONS.md; the
                // user, 2026-10-06: "I want this game experience", with the refill): a beat after the celebration's cut, each move
                // left leaves the Moves panel as a comet, 0.1 s apart, and becomes a rocket where it lands; the new rockets sit a
                // moment, then every power piece on the board goes off one after another, the board falls and refills, and the
                // round repeats while moves or power pieces are left. The card follows the Reward counter's last coin and a short
                // rest (RoyalWinSequence.EndFinale).
                BuildLabel();skip.gameObject.SetActive(true);
                yield return Delay(.25f,.05f);
                int left=lm.limitType==LIMIT.MOVES?Mathf.Max(0,lm.Limit):8;
                for(int round=0;round<12&&Active;round++){
                    var candidates=Regular();int count=Mathf.Min(left,candidates.Count);
                    for(int n=0;n<count&&Active;n++){
                        int pick=Random.Range(0,candidates.Count);var item=candidates[pick];candidates.RemoveAt(pick);
                        left--;Sent++;if(lm.limitType==LIMIT.MOVES)lm.Limit=Mathf.Max(0,lm.Limit-1);
                        travelling++;StartCoroutine(Send(item));yield return Delay(.1f,.015f);
                    }
                    float flying=Time.unscaledTime+3;while(Active&&travelling>0&&Time.unscaledTime<flying)yield return null;
                    var powers=lm.GetItems().Where(Power).ToList();
                    // No fabricated target: a fully blocked board keeps its remaining counter (the win already paid for it).
                    if(powers.Count==0)break;
                    // The new rockets sit for a moment before they go (0.65 s in the clip, after a 0.2 s landing flash).
                    if(count>0)yield return Delay(.75f,.05f);
                    foreach(var item in powers){if(!Active)yield break;if(!Power(item))continue;item.DestroyItem();Fired++;yield return Delay(.18f,.025f);}
                    yield return Settle();
                    if(left==0&&!lm.GetItems().Any(Power))break;
                }
                // Moves that found no piece to become (a small board) are spent with the rest; the win already paid for them.
                if(Active&&lm.limitType==LIMIT.MOVES)lm.Limit=0;
            }finally{Running=false;if(skip)skip.gameObject.SetActive(false);foreach(var h in leases)h?.Stop();leases.Clear();}
        }
        IEnumerator Send(Item item){ReferenceVfxPool.Handle flight=null;
            try{
                if(!item)yield break;Vector3 end=item.transform.position,start=end+Vector3.up*3;
                var label=GameObject.Find("Moves remaining");var camera=Camera.main;
                if(label&&camera){var canvas=label.GetComponentInParent<Canvas>();Vector2 screen=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,label.transform.position);start=camera.ScreenToWorldPoint(new Vector3(screen.x,screen.y,camera.WorldToScreenPoint(end).z));}
                flight=ReferenceVfxPool.Play("RemainingMovesParticles",start,sustained:true);if(flight!=null)leases.Add(flight);
                float progress=0;while(Active&&item&&progress<1){progress+=Time.deltaTime/(lm.SkipWinCelebration?.10f:.45f);float u=Mathf.Clamp01(progress);Vector3 p=Vector3.Lerp(start,end,u)+Vector3.right*Mathf.Sin(u*Mathf.PI)*.5f;if(flight!=null&&flight.Valid)flight.GameObject.transform.position=p;yield return null;}
                if(Active&&item&&!item.destroying&&item.currentType==ItemsTypes.NONE){ReferenceVfxPool.Play("RemainingMovesHitParticles",end);item.NextType=Random.value<.5f?ItemsTypes.HORIZONTAL_STRIPPED:ItemsTypes.VERTICAL_STRIPPED;item.ChangeType();Converted++;}
            }finally{flight?.Stop();travelling=Mathf.Max(0,travelling-1);}
        }
        void BuildLabel(){if(skip)return;var r=new GameObject("Finishing stage overlay",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));r.transform.SetParent(transform,false);var c=r.GetComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceOverlay;c.sortingOrder=23;var scale=r.GetComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(720,1280);
            var rt=new GameObject("Tap to skip",typeof(RectTransform)).GetComponent<RectTransform>();rt.SetParent(r.transform,false);rt.anchorMin=new Vector2(.25f,.12f);rt.anchorMax=new Vector2(.75f,.16f);rt.offsetMin=rt.offsetMax=Vector2.zero;skip=rt.gameObject.AddComponent<Text>();skip.font=Resources.Load<Font>("RoyalGameplay/Display");skip.text="Tap to Skip";skip.fontSize=28;skip.alignment=TextAnchor.MiddleCenter;skip.color=new Color(1,.97f,.8f);skip.raycastTarget=false;var shadow=rt.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(.02f,.10f,.22f);shadow.effectDistance=new Vector2(1,-2);}
        void Update(){if(Running&&!Active){StopAllCoroutines();travelling=0;Running=false;foreach(var h in leases)h?.Stop();leases.Clear();if(skip)skip.gameObject.SetActive(false);}}
        void OnDisable(){StopAllCoroutines();travelling=0;Running=false;foreach(var h in leases)h?.Stop();leases.Clear();if(skip)skip.gameObject.SetActive(false);}
    }
}
