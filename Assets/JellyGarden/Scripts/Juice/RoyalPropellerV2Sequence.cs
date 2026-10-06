using System;
using System.Collections;
using UnityEngine;

namespace JellyGarden.Scripts.Juice
{
    /// <summary>
    /// Propeller V2 flight and impact. The board work (neighbour hits, the payload at the target)
    /// is unchanged and still owned by the director; this file adds the staging, the named events
    /// and the target-cell impact, and swaps the old flat flyer sprite for the V2 rig.
    /// </summary>
    public sealed partial class RoyalPowerupDirector
    {
        public enum PropellerPhase { Complete, Takeoff, Flight, Impact }
        public PropellerPhase PropellerState { get; private set; }
        public int PropellerLaunches { get; private set; }
        public int PropellerImpacts { get; private set; }
        public int PropellerNeighborBlasts { get; private set; }

        /// <summary>Cosmetic timeline hooks. Board destruction never runs from these.</summary>
        public static event Action<Vector3> PropellerActivated;
        public static event Action<Vector3> NeighborBlast;
        public static event Action<Vector3> FlightReady;
        public static event Action<Vector3> TargetReached;
        public static event Action<Vector3> ImpactStarted;
        public static event Action<Vector3> ImpactFinished;

        static void ResetPropellerEvents()
        {
            PropellerActivated = null; NeighborBlast = null; FlightReady = null;
            TargetReached = null; ImpactStarted = null; ImpactFinished = null;
        }

        /// <summary>
        /// One propeller: takeoff on its own cell, a gameplay-driven curved flight, then the impact
        /// at the target. The rig only ever draws; every board mutation stays where it was.
        /// </summary>
        IEnumerator FlightV2(Square origin, Square target, ItemsTypes payload)
        {
            // One board contact pivot for the whole booster: the seated rig sits on the cell's
            // contact point, so the takeoff has to start from the same place or the propeller
            // pops half a cell upward the moment it is activated.
            Vector3 contact = Vector3.down * Cell * .5f + Vector3.back * .8f;
            var rig = RoyalPropellerV2.Take(origin.transform.position + contact, Cell,
                SortingLayer.NameToID("Default"), 34, RoyalPropellerPieceV2.SpinAt(origin));
            if (rig == null) { yield return Flight(origin, target, payload); yield break; }

            PropellerState = PropellerPhase.Takeoff;
            PropellerLaunches++;
            RoyalAudioDirector.Get?.Play(RoyalCue.Helicopter);
            PropellerActivated?.Invoke(origin.transform.position);

            // Skip the reveal: this rig is created already flying.
            rig.Launch();
            bool blasted = false;
            while (!rig.FlightReady)
            {
                if (!blasted && rig.Age >= RoyalPropellerV2.TakeoffBlastAt)
                {
                    blasted = true;
                    PropellerNeighborBlasts++;
                    TakeoffBurst(origin.transform.position);
                    NeighborBlast?.Invoke(origin.transform.position);
                }
                yield return null;
            }
            FlightReady?.Invoke(rig.transform.position);

            PropellerState = PropellerPhase.Flight;
            // The rig flies in contact space and lifts itself off the board through its own rise
            // curve; the impact reads from the target cell centre instead.
            Vector3 start = origin.transform.position + contact;
            Vector3 end = target.transform.position + contact;
            Vector3 impact = target.transform.position + Vector3.back * .8f;
            Vector3 control = (start + end) * .5f + Vector3.up * Cell * 2;
            float duration = RoyalEffectsProfile.Get.helicopterFlight, t = 0;
            while (t < duration)
            {
                if (!Paused) t += Time.deltaTime;
                float k = Mathf.SmoothStep(0, 1, t / duration);
                rig.Place((1 - k) * (1 - k) * start + 2 * (1 - k) * k * control + k * k * end);
                yield return null;
            }
            TargetReached?.Invoke(impact);
            reserved.Remove(target);
            rig.Retire();

            PropellerState = PropellerPhase.Impact;
            PropellerImpacts++;
            ImpactStarted?.Invoke(impact);
            TargetImpact(impact);
            if (payload == ItemsTypes.PACKAGE) yield return Explosion(target, 2);
            else if (payload == ItemsTypes.HORIZONTAL_STRIPPED || payload == ItemsTypes.VERTICAL_STRIPPED)
                yield return Rocket(target, payload == ItemsTypes.HORIZONTAL_STRIPPED, 1);
            else Hit(target);
            ImpactFinished?.Invoke(impact);
            PropellerState = PropellerPhase.Complete;
        }

        /// <summary>
        /// White and gold takeoff flash with a shock pulse over the neighbouring cells. Short lived
        /// on purpose: it must not sit on top of the board information.
        /// </summary>
        void TakeoffBurst(Vector3 p)
        {
            // The white core used to be nearly as wide as the ring, which bleached the gold out
            // of both bursts. Keep it small and hot, and let the gold ring carry the silhouette.
            Emit(flash, p, 1, .62f, 0, .10f, new Color(1, .97f, .86f, 1));
            Emit(ring, p, 1, 1.55f, 0, .26f, new Color(1, .82f, .32f, 1));
            Emit(sparks, p, 10, .30f, 3.4f, .26f, new Color(1, .93f, .62f, 1));
            Emit(embers, p, 5, .22f, 2.2f, .28f, new Color(1, .78f, .30f, 1));
            shakeEnergy = Mathf.Max(shakeEnergy, .55f);
            Reaction?.Invoke(.9f);
        }

        /// <summary>
        /// The target-cell impact: a white-hot core, a short gold ring, four-point sparks and a few
        /// chips. It plays on the cell that was hit, so none of it is baked into the flying rig.
        /// </summary>
        void TargetImpact(Vector3 p)
        {
            Emit(flash, p, 1, .54f, 0, .09f, new Color(1, .98f, .88f, 1));
            Emit(ring, p, 1, 1.42f, 0, .24f, new Color(1, .80f, .28f, 1));
            Emit(sparks, p, 12, .26f, 3.1f, .24f, new Color(1, .95f, .70f, 1));
            Emit(embers, p, 6, .20f, 2.0f, .24f, new Color(1, .74f, .26f, 1));
            Chip(p);
            shakeEnergy = Mathf.Max(shakeEnergy, .7f);
            Reaction?.Invoke(1.1f);
        }
    }
}
