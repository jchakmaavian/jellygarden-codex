using System.Collections;
using RoyalThrone.ReferenceGameplay;
using UnityEngine;

namespace JellyGarden.Scripts.Juice
{
    public sealed partial class RoyalPowerupDirector
    {
        public int NativeTntComboPlays { get; private set; }
        public float LastTntComboPreparation { get; private set; }

        // The APK supplies a dedicated body/roof/fuse animation for the double TNT.
        // Its compiled owner signals animation completion; this adapter keeps that
        // boundary without changing the existing centre, damage or reward code.
        IEnumerator NativeTntComboAnticipation(Vector3 position)
        {
            var actor = TakeTntSprite(position);
            ReferenceActivationVisual view = null;
            ReferenceVfxPool.Handle fuse = null;
            try
            {
                if (actor != null)
                    view = ReferenceActivationVisual.Attach(actor.go, "TNTTNTComboRfv387", 36);
                float length = view ? view.Play("TntTntComboExplodeAnimation") : 0;
                if (length <= 0)
                {
                    Debug.LogWarning("[Reference Gameplay] Dedicated TNT+TNT body animation unavailable.");
                    yield return TntAnticipation(actor);
                    yield break;
                }
                NativeTntComboPlays++;
                LastTntComboPreparation = length;
                ReferenceGameplayAudio.Play("tnt_tnt_combo__rfv387_17741", .45f);
                var socket = view.Socket("FitilPartikul");
                fuse = ReferenceVfxPool.Play("TntTntComboFuseParticles", socket.position, worldCell: Cell, sustained: true);
                // The body renders at 36; the pool default (35) hides the burning tip.
                fuse?.Follow(socket, 37);
                float elapsed = 0;
                while (elapsed < length)
                {
                    yield return null;
                    if (!Paused) elapsed += Time.deltaTime;
                }
            }
            finally
            {
                fuse?.Stop();
                if (view) view.Release();
                if (actor != null) { actor.go.SetActive(false); actor.used = false; }
            }
        }
    }
}
