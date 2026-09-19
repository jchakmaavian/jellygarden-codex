using UnityEngine;

namespace RoyalRocketVFX
{
    /// <summary>
    /// Static entry point for the rocket effect.
    ///
    ///     RocketVFX.Play(position, RocketDirection.Right, distanceInCells: 8, cellSize: 1f);
    ///
    /// Requires one RocketVFXPlayer in the scene - drag in Prefabs/RocketVFX_Horizontal or
    /// RocketVFX_Vertical. The player registers itself on Awake.
    /// </summary>
    public static class RocketVFX
    {
        static RocketVFXPlayer active;

        /// <summary>The player the static calls route to. Null when no prefab is in the scene.</summary>
        public static RocketVFXPlayer Player => active;

        /// <summary>True while any rocket or trailing particle is still on screen.</summary>
        public static bool IsBusy => active && active.IsBusy;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => active = null;

        /// <summary>Called automatically by RocketVFXPlayer.Awake.</summary>
        public static void Register(RocketVFXPlayer player)
        {
            if (player) active = player;
        }

        /// <summary>Called automatically when a player is destroyed.</summary>
        public static void Unregister(RocketVFXPlayer player)
        {
            if (active == player) active = null;
        }

        /// <summary>
        /// Fire one rocket from <paramref name="position"/>.
        /// <paramref name="distanceInCells"/> is how many cells it sweeps (one CellReached event
        /// each); it then continues off screen at the same speed and retires.
        /// <paramref name="cellSize"/> is world units per cell; pass 0 to use the player's default.
        /// </summary>
        public static void Play(Vector3 position, RocketDirection direction, float distanceInCells, float cellSize = 0f)
        {
            if (!Ready()) return;
            active.Play(position, direction, distanceInCells, cellSize);
        }

        /// <summary>Fire the two rockets of a match-3 line clear: Left+Right, or Up+Down.</summary>
        public static void PlayPair(Vector3 position, RocketAxis axis, float distanceInCells, float cellSize = 0f)
        {
            if (!Ready()) return;
            active.PlayPair(position, axis, distanceInCells, cellSize);
        }

        /// <summary>Stop every rocket and clear all particles immediately.</summary>
        public static void Clear()
        {
            if (active) active.Clear();
        }

        static bool Ready()
        {
            if (active) return true;
            Debug.LogWarning("[RoyalRocketVFX] No RocketVFXPlayer in the scene. Drag " +
                             "Assets/RoyalRocketVFX/Prefabs/RocketVFX_Horizontal.prefab (or _Vertical) " +
                             "into your scene, or call RocketVFX.Register(player) yourself.");
            return false;
        }
    }
}
