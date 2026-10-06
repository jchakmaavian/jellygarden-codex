using System.Collections.Generic;
using UnityEngine;

namespace JellyGarden.Scripts.Juice
{
    /// <summary>
    /// Drives the V2 rig for a propeller sitting on the board: the create/reveal pop and then the
    /// near-static idle. The Item keeps its static sprite, hidden, so cell fit, target matching and
    /// hint geometry are unchanged. Activation releases the rig and the flight takes over.
    /// </summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(1000)]
    public sealed class RoyalPropellerPieceV2 : MonoBehaviour
    {
        public static event global::System.Action<Vector3> RevealStarted;
        public static event global::System.Action<Vector3> RevealFinished;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { RevealStarted = null; RevealFinished = null; seatedSpin.Clear(); }

        /// <summary>
        /// The rotor phase each seated propeller is showing, by cell. Activation reads it so the
        /// flight rig carries the blades on from exactly where the idle left them.
        /// </summary>
        static readonly Dictionary<Square, float> seatedSpin = new Dictionary<Square, float>();

        /// <summary>Last published rotor angle for <paramref name="square"/>, or zero if none.</summary>
        public static float SpinAt(Square square)
        {
            return square && seatedSpin.TryGetValue(square, out float spin) ? spin : 0f;
        }

        /// <summary>Drops the published phases when the board goes away.</summary>
        public static void ClearSeated() { seatedSpin.Clear(); }

        public RoyalPropellerV2 Rig { get; private set; }
        public bool Revealing => Rig && Rig.Current == RoyalPropellerV2.Phase.Reveal;

        Item item;
        bool announcedFinish;
        int rigSerial;

        /// <summary>Starts the reveal on a propeller created by a match.</summary>
        public static RoyalPropellerPieceV2 PlayReveal(Item item) => Attach(item, true);

        /// <summary>
        /// Gives a propeller its V2 rig. One that appears without a reveal (a pre-level boost, or a
        /// piece restored by a shuffle) joins the idle pose directly, so its static art never shows.
        /// </summary>
        public static RoyalPropellerPieceV2 Attach(Item item, bool reveal = false)
        {
            if (!Application.isPlaying || !item || !item.sprRenderer || item.destroying ||
                item.currentType != ItemsTypes.HELICOPTER || !RoyalEffectsProfile.Get || !RoyalPropellerV2.Ready) return null;
            var piece = item.GetComponent<RoyalPropellerPieceV2>();
            if (!piece) piece = item.gameObject.AddComponent<RoyalPropellerPieceV2>();
            if (!piece.Rig) piece.Begin(reveal);
            return piece;
        }

        void Awake() { item = GetComponent<Item>(); }

        void Begin(bool reveal)
        {
            var source = item.sprRenderer;
            float cell = LevelManager.THIS ? LevelManager.THIS.squareWidth * Mathf.Abs(LevelManager.THIS.GameField.lossyScale.x) : 1f;
            Rig = RoyalPropellerV2.Take(Contact(), cell, source.sortingLayerID, source.sortingOrder + 1);
            if (!Rig) return;
            rigSerial = Rig.Serial;
            announcedFinish = false;
            if (!reveal) Rig.Seat();
            else RevealStarted?.Invoke(transform.position);
        }

        /// <summary>The rig is seated on the cell's contact point, not on the sprite's centre.</summary>
        Vector3 Contact()
        {
            float cell = LevelManager.THIS ? LevelManager.THIS.squareWidth * Mathf.Abs(LevelManager.THIS.GameField.lossyScale.x) : 1f;
            return transform.position + Vector3.down * cell * .5f + Vector3.back * .25f;
        }

        void LateUpdate()
        {
            if (!item || !item.sprRenderer || item.destroying || item.currentType != ItemsTypes.HELICOPTER) { Stop(); return; }
            // A rig released underneath us (a board teardown) may already serve someone else.
            if (Rig && (Rig.Serial != rigSerial || Rig.Current == RoyalPropellerV2.Phase.Free)) Rig = null;
            if (!Rig) { Begin(false); if (!Rig) return; }
            // The rig follows the piece, so a propeller that falls or is swapped keeps its pose.
            Rig.Place(Contact());
            item.sprRenderer.enabled = false;
            if (item.square) seatedSpin[item.square] = Rig.Spin;
            if (!announcedFinish && Rig.Current == RoyalPropellerV2.Phase.Seated)
            {
                announcedFinish = true;
                RevealFinished?.Invoke(transform.position);
            }
        }

        /// <summary>Releases the rig without a retire fade: the flight rig takes over on activation.</summary>
        public void Stop()
        {
            if (Rig && Rig.Serial == rigSerial) Rig.Release();
            Rig = null;
            if (item && item.sprRenderer && !item.destroying) item.sprRenderer.enabled = true;
        }

        void OnDisable() { Stop(); }
        void OnDestroy() { Stop(); }
    }
}
