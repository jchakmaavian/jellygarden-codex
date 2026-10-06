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
        // Only the boosters (and what they set off) are waited for, never gravity: nothing refills and the sweep takes what is
        // left wherever it is, so letting the pieces slide down first only added seconds of nothing (measured 2026-10-06: 2.9 s).
        IEnumerator BoostersDone(){float until=Time.unscaledTime+4;do{yield return null;}while(Active&&Time.unscaledTime<until&&RoyalPowerupDirector.Busy);}
        // The last pops play out (at most 0.35 s) so the board is seen empty for a moment before the card.
        IEnumerator PopsDone(){float until=Time.unscaledTime+.35f;do{yield return null;}while(Active&&Time.unscaledTime<until&&lm.GetItems().Any(i=>i&&i.destroying));}
        IEnumerator Run(LevelManager owner){lm=owner;Sent=Converted=Fired=travelling=0;Running=true;
            try{
                // The booster session (the user, 2026-10-06: "do the booster to empty the box area, no more fill up, do not bore
                // the player"): the moves left become rockets in one quick volley, every power-up on the board goes off in one
                // ripple, nothing refills behind them (LevelManager.GenerateNewItems), and whatever is left is swept away so the
                // board ends empty. No pause before, between or after the steps.
                BuildLabel();skip.gameObject.SetActive(true);
                int left=lm.limitType==LIMIT.MOVES?Mathf.Max(0,lm.Limit):8;
                var candidates=Regular();int count=Mathf.Min(left,candidates.Count);
                for(int n=0;n<count&&Active;n++){
                    int pick=Random.Range(0,candidates.Count);var item=candidates[pick];candidates.RemoveAt(pick);
                    Sent++;if(lm.limitType==LIMIT.MOVES)lm.Limit=Mathf.Max(0,lm.Limit-1);
                    travelling++;StartCoroutine(Send(item));yield return Delay(.05f,.01f);
                }
                float flying=Time.unscaledTime+2;while(Active&&travelling>0&&Time.unscaledTime<flying)yield return null;
                // Rockets crossing other power-ups set them off too; a few rounds at most.
                for(int round=0;round<4&&Active;round++){
                    var powers=lm.GetItems().Where(Power).ToList();if(powers.Count==0)break;
                    foreach(var item in powers){if(!Active)yield break;if(!Power(item))continue;item.DestroyItem();Fired++;yield return Delay(.05f,.01f);}
                    yield return BoostersDone();
                }
                if(Active)yield return Sweep();
                // Moves that found no piece to become (a small board) are spent with the rest; the win already paid for them.
                if(Active&&lm.limitType==LIMIT.MOVES)lm.Limit=0;
            }finally{Running=false;if(skip)skip.gameObject.SetActive(false);foreach(var h in leases)h?.Stop();leases.Clear();}
        }
        // Whatever the boosters left, pieces and blockers alike, goes in one wave from the centre out (about 0.3 s).
        IEnumerator Sweep(){
            var cells=new List<Square>();
            for(int r=0;r<lm.maxRows;r++)for(int c=0;c<lm.maxCols;c++){var sq=lm.GetSquare(c,r);if(sq&&!sq.IsNone()&&sq.type!=SquareTypes.UNDESTROYABLE&&((sq.item&&!sq.item.destroying)||sq.block.Count>0))cells.Add(sq);}
            if(cells.Count==0)yield break;
            float cx=(lm.maxCols-1)*.5f,cy=(lm.maxRows-1)*.5f;
            foreach(var ring in cells.GroupBy(sq=>Mathf.RoundToInt(Mathf.Max(Mathf.Abs(sq.col-cx),Mathf.Abs(sq.row-cy)))).OrderBy(g=>g.Key)){
                if(!Active)yield break;
                foreach(var sq in ring){if(sq.item&&!sq.item.destroying){sq.item.royalBlastClear=true;sq.item.DestroyItem();}else if(sq.block.Count>0)sq.DestroyBlock();}
                yield return Delay(.05f,.02f);
            }
            // Blockers with more than one layer take another pass once their cell is clear.
            for(int pass=0;pass<3&&Active;pass++){bool any=false;foreach(var sq in cells)if(sq&&sq.block.Count>0&&!sq.item){sq.DestroyBlock();any=true;}if(!any)break;yield return Delay(.05f,.02f);}
            yield return PopsDone();
        }
        IEnumerator Send(Item item){ReferenceVfxPool.Handle flight=null;
            try{
                if(!item)yield break;Vector3 end=item.transform.position,start=end+Vector3.up*3;
                var label=GameObject.Find("Moves remaining");var camera=Camera.main;
                if(label&&camera){var canvas=label.GetComponentInParent<Canvas>();Vector2 screen=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,label.transform.position);start=camera.ScreenToWorldPoint(new Vector3(screen.x,screen.y,camera.WorldToScreenPoint(end).z));}
                flight=ReferenceVfxPool.Play("RemainingMovesParticles",start,sustained:true);if(flight!=null)leases.Add(flight);
                float progress=0;while(Active&&item&&progress<1){progress+=Time.deltaTime/(lm.SkipWinCelebration?.10f:.24f);float u=Mathf.Clamp01(progress);Vector3 p=Vector3.Lerp(start,end,u)+Vector3.right*Mathf.Sin(u*Mathf.PI)*.5f;if(flight!=null&&flight.Valid)flight.GameObject.transform.position=p;yield return null;}
                if(Active&&item&&!item.destroying&&item.currentType==ItemsTypes.NONE){ReferenceVfxPool.Play("RemainingMovesHitParticles",end);item.NextType=Random.value<.5f?ItemsTypes.HORIZONTAL_STRIPPED:ItemsTypes.VERTICAL_STRIPPED;item.ChangeType();Converted++;}
            }finally{flight?.Stop();travelling=Mathf.Max(0,travelling-1);}
        }
        void BuildLabel(){if(skip)return;var r=new GameObject("Finishing stage overlay",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));r.transform.SetParent(transform,false);var c=r.GetComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceOverlay;c.sortingOrder=23;var scale=r.GetComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(720,1280);
            var rt=new GameObject("Tap to skip",typeof(RectTransform)).GetComponent<RectTransform>();rt.SetParent(r.transform,false);rt.anchorMin=new Vector2(.25f,.12f);rt.anchorMax=new Vector2(.75f,.16f);rt.offsetMin=rt.offsetMax=Vector2.zero;skip=rt.gameObject.AddComponent<Text>();skip.font=Resources.Load<Font>("RoyalGameplay/Display");skip.text="Tap to Skip";skip.fontSize=28;skip.alignment=TextAnchor.MiddleCenter;skip.color=new Color(1,.97f,.8f);skip.raycastTarget=false;var shadow=rt.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(.02f,.10f,.22f);shadow.effectDistance=new Vector2(1,-2);}
        void Update(){if(Running&&!Active){StopAllCoroutines();travelling=0;Running=false;foreach(var h in leases)h?.Stop();leases.Clear();if(skip)skip.gameObject.SetActive(false);}}
        void OnDisable(){StopAllCoroutines();travelling=0;Running=false;foreach(var h in leases)h?.Stop();leases.Clear();if(skip)skip.gameObject.SetActive(false);}
    }
}
