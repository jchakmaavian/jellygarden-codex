using System.Collections;
using System.Collections.Generic;
using RoyalThrone.Campaign;
using RoyalThrone.Boosters.Propeller.V5;
using UnityEngine;

namespace JellyGarden.Scripts.Juice
{
    public sealed partial class RoyalPowerupDirector
    {
        public int PropellerReservedTargets => reserved.Count;
        public int PropellerPayloadHandoffs {get;private set;}
        static bool PropellerLandingCell(Square square) => square && !square.IsNone() && square.type!=SquareTypes.UNDESTROYABLE;
        // Reconstructed objective-first selection. The APK exposes separate combo pilots
        // and target scoring, not editable selection code; equal-score tie breaks retain
        // the existing distance/random preference. Never target an already reserved cell.
        bool PropellerEligible(Square square, Square origin) => square && square != origin &&
            !square.IsNone() && square.type != SquareTypes.UNDESTROYABLE && !reserved.Contains(square) &&
            (!square.item || square.item.currentType != ItemsTypes.INGREDIENT) &&
            (square.item && !square.item.destroying || square.block.Count > 0);

        static bool PropellerGoalRemaining(string name)
        {
            var level=LevelManager.THIS;
            if(string.IsNullOrEmpty(name)||!level||level.targetObject==null)return false;
            foreach(var goal in level.targetObject)
                if(goal!=null&&goal.referenceGoal==name&&!goal.Done())return true;
            return false;
        }

        public static int PropellerObjectiveScore(Square target, ItemsTypes payload)
        {
            var level=LevelManager.THIS;if(!level||!target)return 0;
            int result=0;var obstacles=new List<Square>();
            foreach(var cell in level.squaresArray)
            {
                if(!cell||cell.IsNone()||cell.type==SquareTypes.UNDESTROYABLE)continue;
                bool reached=cell==target;
                if(payload==ItemsTypes.PACKAGE)reached=Mathf.Abs(cell.col-target.col)<=2&&Mathf.Abs(cell.row-target.row)<=2;
                else if(payload==ItemsTypes.HORIZONTAL_STRIPPED)reached=cell.row==target.row;
                else if(payload==ItemsTypes.VERTICAL_STRIPPED)reached=cell.col==target.col;
                if(!reached)continue;
                if(RoyalReferenceCampaign.Active)
                {
                    if(RoyalCampaignBlockers.GrassAt(cell)>0&&PropellerGoalRemaining("Grass"))result++;
                    string goal=RoyalCampaignBlockers.GoalAt(cell);
                    if(PropellerGoalRemaining(goal)&&!obstacles.Exists(previous=>RoyalCampaignBlockers.SharesObstacle(previous,cell)))
                    {result++;obstacles.Add(cell);}
                    var special=cell.item?cell.item.GetComponent<RoyalCampaignItem>():null;
                    if(special&&!cell.item.destroying&&PropellerGoalRemaining(special.GoalName))result++;
                }
                else if(level.targetObject!=null)
                {
                    foreach(var goal in level.targetObject)
                    {
                        if(goal==null||goal.Done())continue;
                        if(goal.type==global::JellyGarden.Scripts.Target.COLLECT&&cell.item&&!cell.item.destroying&&cell.item.currentType==ItemsTypes.NONE&&cell.item.color==goal.color)result++;
                        else if(goal.type==global::JellyGarden.Scripts.Target.BLOCKS&&cell.block.Count>0)result++;
                    }
                }
            }
            return result;
        }

        Square PropellerTarget(Square origin,ItemsTypes payload)
        {
            var level=LevelManager.THIS;Square best=null;int bestGoals=-1;float bestTie=float.NegativeInfinity;
            foreach(var cell in level.squaresArray)
            {
                if(!PropellerEligible(cell,origin))continue;
                int goals=PropellerObjectiveScore(cell,payload);
                float tie=(cell.block.Count>0?100:0)+Vector3.Distance(origin.transform.position,cell.transform.position)+Random.value*3;
                if(goals>bestGoals||goals==bestGoals&&tie>bestTie){best=cell;bestGoals=goals;bestTie=tie;}
            }
            if(best)reserved.Add(best);return best;
        }

        void PropellerHitV5(Square cell)
        {
            if(cell&&cell.item&&!cell.item.destroying&&cell.item.currentType==ItemsTypes.NONE)cell.item.royalPropellerV5Clear=true;
            Hit(cell);
        }
        IEnumerator PropellerSequenceV5(Square origin,ItemsTypes payload,int count)
        {
            var seated=PropellerSeatV5.Claim(origin);
            for(int i=0;i<count;i++){
                var target=PropellerTarget(origin,payload);
                // On an almost-empty board, still perform the first takeoff/nearby
                // clear and deliver its payload locally. Do not create duplicate
                // fallback contacts for extra Propellers when every cell is claimed.
                if(!target&&i==0&&PropellerLandingCell(origin)&&reserved.Add(origin))target=origin;
                if(target){StartOperation(PropellerFlightV5(origin,target,payload,i==0?seated:null,i==0));seated=null;}
                yield return Wait(.07f);
            }
            if(seated)seated.Release();
        }
        IEnumerator PropellerFlightV5(Square origin,Square target,ItemsTypes payload,PropellerVisualV5 view,bool neighbors)
        {
            if(!view)view=PropellerVisualV5.Take(origin.transform.position,Cell,false);int lease=view.Lease;
            try{
                float delay=PropellerVisualV5.ReserveLaunchDelay();if(delay>0)yield return Wait(delay);
                view.Activate(target.transform.position,payload);
                bool hitNeighbors=false;
                while(view&&view.Current==PropellerVisualV5.Phase.Activation){
                    // Follow the visual release event, not an independent timer one frame ahead.
                    if(neighbors&&!hitNeighbors&&view.HasTakenOff){hitNeighbors=true;foreach(var n in origin.GetAllNeghbors()){if(!n)continue;view.NeighborHit(n.transform.position);PropellerHitV5(n);}}
                    yield return null;
                }
                if(!view||!view.FlightReady)yield break;
                // A takeoff neighbour hit or another active booster can consume the
                // destination. Release its claim before choosing another valid cell.
                var lastDestination=target;
                reserved.Remove(target);
                if(!PropellerEligible(target,origin))target=PropellerTarget(origin,payload);
                else reserved.Add(target);
                // No replacement must not silently discard a consumed Rocket/TNT.
                // Its last valid board cell can still own the area/line payoff even
                // when the centre gem has already been cleared by another effect.
                if(!target&&PropellerLandingCell(lastDestination)&&reserved.Add(lastDestination))target=lastDestination;
                if(!target)yield break;
                var start=view.BeginFlightTranslation();var end=target.transform.position+Vector3.back*.8f;
                float distance=Vector2.Distance(origin.transform.position,target.transform.position)/Cell;
                float duration=PropellerMotionV5.Duration(distance),t=0;
                // Cosmetic path: board targeting and destruction remain in the existing director.
                var liftControl=start+Vector3.up*Cell*.30f;
                var approach=Vector3.Lerp(start,end,.68f)+Vector3.up*Cell*PropellerMotionV5.Arc(distance)*.65f;
                view.FlightDuration=duration;
                while(t<duration){
                    if(!Paused)t+=Time.deltaTime;float progress=Mathf.Clamp01(t/duration),k=PropellerMotionV5.Ease(progress,duration);
                    float a=1-k;var p=a*a*a*start+3*a*a*k*liftControl+3*a*k*k*approach+k*k*k*end;
                    var tangent=3*a*a*(liftControl-start)+6*a*k*(approach-liftControl)+3*k*k*(end-approach);view.Place(p,tangent,progress);yield return null;
                }
                view.Contact(target.transform.position);
                while(view&&view.Current==PropellerVisualV5.Phase.Impact&&!view.ImpactStarted)yield return null;
                reserved.Remove(target);
                if(payload==ItemsTypes.PACKAGE){PropellerPayloadHandoffs++;yield return Explosion(target,2);}
                else if(payload==ItemsTypes.HORIZONTAL_STRIPPED||payload==ItemsTypes.VERTICAL_STRIPPED){yield return Wait(.07f);PropellerPayloadHandoffs++;yield return Rocket(target,payload==ItemsTypes.HORIZONTAL_STRIPPED,1);}
                else PropellerHitV5(target);
                yield return Wait(.40f);
            }
            finally{if(target)reserved.Remove(target);if(view&&view.Lease==lease&&view.Current!=PropellerVisualV5.Phase.Free)view.Release();}
        }
    }
}
