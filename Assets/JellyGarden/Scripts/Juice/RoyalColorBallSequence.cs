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

        sealed class ColorBallFlight
        {
            public Square square;
            public Item item;
            public float launched;
            public bool arrived;
        }

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
                var flights=new List<ColorBallFlight>(4);
                float clock=0,nextBeam=0;int nextTarget=0;
                // A short overlap makes a flowing volley; gameplay changes still occur on arrival.
                while(nextTarget<targets.Count||flights.Count>0)
                {
                    if(Paused){yield return null;continue;}
                    if(nextTarget<targets.Count&&clock>=nextBeam)
                    {
                        var sq=targets[nextTarget++];nextBeam=clock+.105f;
                        if(sq&&sq.item&&!sq.item.destroying)
                        {
                            flights.Add(new ColorBallFlight{square=sq,item=sq.item,launched=clock});
                            ColorBallTargetVfx.Play(ColorBallTargetVfx.Kind.Beam,centre,sq.transform.position);
                        }
                    }
                    for(int i=0;i<flights.Count;)
                    {
                        var flight=flights[i];var sq=flight.square;var selected=flight.item;
                        if(!sq||!selected||selected.destroying||sq.item!=selected){flights.RemoveAt(i);continue;}
                        float elapsed=clock-flight.launched;
                        if(!flight.arrived&&elapsed>=ColorBallTargetVfx.ArrivalTime)
                        {
                            flight.arrived=true;ColorBallBeamArrivals++;
                            ColorBallTargetVfx.Play(ColorBallTargetVfx.Kind.TargetGlow,sq.transform.position,sq.transform.position,selected.sprRenderer);
                            ColorBallTargetVfx.Play(ColorBallTargetVfx.Kind.TargetImpact,sq.transform.position,sq.transform.position);
                        }
                        if(elapsed>=ColorBallTargetVfx.ArrivalTime+.045f)
                        {
                            if(convert&&selected.currentType==ItemsTypes.NONE)
                            {selected.NextType=payload;selected.ChangeType();ColorBallConversions++;}
                            if(!convert&&selected.currentType==ItemsTypes.NONE)Hit(sq);else deferred.Add(sq);
                            flights.RemoveAt(i);continue;
                        }
                        i++;
                    }
                    yield return null;
                    if(!Paused)clock+=Time.deltaTime;
                }
                // Keep the resolver busy through the final impact, authored finish and handoff.
                // This also prevents gravity/refill from running underneath an unfinished ball.
                // The final impact can fade over the authored finish; do not hold a static loop for its tail.
                yield return Wait(.08f);
                ColorBallState=ColorBallPhase.Finish;
                if(boosting)boosting.Finish();
                while(boosting && boosting.Current!=ColorBallBoostView.Phase.Idle)yield return null;
                while(ColorBallTargetVfx.Active>0)yield return null;
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
