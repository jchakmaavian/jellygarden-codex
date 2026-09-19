using UnityEngine;

namespace RoyalRocketVFX
{
    /// <summary>
    /// Optional shared profile. Assign one to a RocketVFXPlayer to drive several players from a
    /// single asset; leave it empty to use the player's own inline Inspector values.
    /// Create via Assets > Create > Royal Rocket VFX > Settings Profile.
    /// </summary>
    [CreateAssetMenu(menuName = "Royal Rocket VFX/Settings Profile", fileName = "RocketVFXProfile")]
    public sealed class RocketVFXSettings : ScriptableObject
    {
        public RocketVFXTuning tuning = new RocketVFXTuning();
    }
}
