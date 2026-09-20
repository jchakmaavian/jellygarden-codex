using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JellyGarden.Scripts.Juice
{
    public sealed partial class RoyalPowerupDirector
    {
        public enum ColorBallPhase { Complete, SpinUp, Converting, Finish, Handoff }
        public ColorBallPhase ColorBallState { get; private set; }
        public int ColorBallConversions { get; private set; }
        public int ColorBallSelectedTargets { get; private set; }
        public int ColorBallHandoffs { get; private set; }
        public int ColorBallBeamArrivals { get; private set; }

        IEnumerator AnimateColorBallTargets(Square origin,List<Square> targets,ItemsTypes payload)
        {
            ColorBallTargetVfx.Prewarm();
            ColorBallConversions=0;ColorBallBeamArrivals=0;ColorBallSelectedTargets=targets.Count;
            bool convert=payload!=ItemsTypes.NONE;
            var deferred=new List<Square>();
            var centre=origin.transform.position;
            var boosting=ColorBallBoostView.Begin(centre);
            ColorBallState=ColorBallPhase.SpinUp;
            RoyalAudioDirector.Get?.Play(RoyalCue.Create);
            try
            {
                while(boosting && boosting.Current==ColorBallBoostView.Phase.Intro)yield return null;
                ColorBallState=ColorBallPhase.Converting;
                foreach(var sq in targets)
                {
                    if(!sq || !sq.item || sq.item.destroying)continue;
                    var selected=sq.item;
                    ColorBallTargetVfx.Play(ColorBallTargetVfx.Kind.Beam,centre,sq.transform.position);
                    yield return Wait(ColorBallTargetVfx.ArrivalTime);
                    if(!sq || !selected || selected.destroying || sq.item!=selected)continue;
                    ColorBallTargetVfx.Play(ColorBallTargetVfx.Kind.TargetGlow,sq.transform.position,sq.transform.position);
                    ColorBallTargetVfx.Play(ColorBallTargetVfx.Kind.TargetImpact,sq.transform.position,sq.transform.position);
                    ColorBallBeamArrivals++;
                    yield return Wait(.055f);
                    if(!selected || selected.destroying || sq.item!=selected)continue;
                    if(convert && selected.currentType==ItemsTypes.NONE)
                    {
                        // Preserve the existing payload orientation and the Item's creation path.
                        selected.NextType=payload;
                        selected.ChangeType();
                        ColorBallConversions++;
                    }
                    // Standalone Color Ball clears each ordinary gem on its beam impact.
                    // Existing/generated powerups wait until finish before they can chain.
                    if(!convert && selected.currentType==ItemsTypes.NONE)Hit(sq);
                    else deferred.Add(sq);
                }
                // Keep the resolver busy through the final impact, authored finish and handoff.
                // This also prevents gravity/refill from running underneath an unfinished ball.
                while(ColorBallTargetVfx.Active>0)yield return null;
                ColorBallState=ColorBallPhase.Finish;
                if(boosting)boosting.Finish();
                while(boosting && boosting.Current!=ColorBallBoostView.Phase.Idle)yield return null;
                ColorBallState=ColorBallPhase.Handoff;
                ColorBallHandoffs++;
                // Hit -> Item.DestroyItem -> Queue -> existing Rocket. It alone owns the clear/VFX.
                foreach(var sq in deferred)Hit(sq);
                Reaction?.Invoke(1.4f);
            }
            finally
            {
                if(boosting)boosting.Stop();
                ColorBallState=ColorBallPhase.Complete;
            }
        }
    }
}
