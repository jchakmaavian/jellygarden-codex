using UnityEngine;

namespace RoyalTNTVFX
{
    /// <summary>
    /// Static entry point for the TNT explosion.
    ///
    ///     TNTExplosionVFX.Play(position, radius: 2, cellSize: 1f);
    ///
    /// Requires one TNTExplosionPlayer in the scene - drag in Prefabs/TNTExplosionVFX.
    /// The player registers itself on Awake.
    /// </summary>
    public static class TNTExplosionVFX
    {
        static TNTExplosionPlayer active;

        /// <summary>The player the static calls route to. Null when no prefab is in the scene.</summary>
        public static TNTExplosionPlayer Player => active;

        /// <summary>True while any explosion is still visible.</summary>
        public static bool IsBusy => active && active.IsBusy;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => active = null;

        /// <summary>Called automatically by TNTExplosionPlayer.Awake.</summary>
        public static void Register(TNTExplosionPlayer player)
        {
            if (player) active = player;
        }

        /// <summary>Called automatically when a player is destroyed.</summary>
        public static void Unregister(TNTExplosionPlayer player)
        {
            if (active == player) active = null;
        }

        /// <summary>
        /// Explode at <paramref name="position"/>. <paramref name="radius"/> is in cells
        /// (2 = the 5x5 TNT, 4 = the 9x9 TNT+TNT combo). <paramref name="cellSize"/> is world
        /// units per cell; 0 uses the player's value. <paramref name="intensity"/> 0 applies the
        /// game's rule: 1.45 when radius is above 2, otherwise 1.
        /// </summary>
        public static void Play(Vector3 position, int radius, float cellSize = 0f, float intensity = 0f)
        {
            if (!Ready()) return;
            active.Play(position, radius, cellSize, intensity);
        }

        /// <summary>Stop every explosion and hide everything immediately.</summary>
        public static void Clear()
        {
            if (active) active.Clear();
        }

        static bool Ready()
        {
            if (active) return true;
            Debug.LogWarning("[RoyalTNTVFX] No TNTExplosionPlayer in the scene. Drag " +
                             "Assets/RoyalTNTVFX/Prefabs/TNTExplosionVFX.prefab into your scene, " +
                             "or call TNTExplosionVFX.Register(player) yourself.");
            return false;
        }
    }
}
