using System.Collections;
using System.Collections.Generic;
using RoyalThrone.ReferenceGameplay;
using UnityEngine;

namespace JellyGarden.Scripts.Juice
{
    public sealed partial class RoyalPowerupDirector
    {
        public int NativeDualColorBallPlays { get; private set; }
        public int DualColorBallHits { get; private set; }
        public int DualColorBallClearFrame { get; private set; }
        public float DualColorBallPreparation { get; private set; }

        IEnumerator DualColorBall(Square origin, List<Square> targets)
        {
            DualColorBallHits = 0; DualColorBallClearFrame = -1;
            var actor = ReferenceDualColorBallVisual.Begin(origin.transform.position);
            if (!actor)
            {
                Debug.LogError("[Reference Gameplay] Native dual Colorball animation unavailable.");
                foreach (var square in targets) Hit(square);
                yield break;
            }
            NativeDualColorBallPlays++;
            DualColorBallPreparation = actor.Duration;
            bool completed = false;
            try
            {
                while (actor.Active && !actor.Ready) yield return null;
                if (!actor.Active) yield break;
                actor.Detonate();
                // The dedicated source blast is a board-wide flash. Apply the existing
                // one-hit-per-cell rule on this cue, instead of scanning rows visibly.
                foreach (var square in targets) { Hit(square); DualColorBallHits++; }
                DualColorBallClearFrame = Time.frameCount;
                Reaction?.Invoke(1.4f);
                completed = true;
                // Audio and native particle tails retire independently of gameplay.
            }
            finally { if (!completed && actor) actor.Release(); }
        }
    }
}
