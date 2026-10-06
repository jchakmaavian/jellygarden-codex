using UnityEngine;

namespace JellyGarden.Scripts.Juice
{
    // Editor-built derivatives retain the user's toolkit assets as nested prefab sources.
    public sealed class RoyalToolkitLibrary : ScriptableObject
    {
        public GameObject ice, fire;
        public float iceScale = 1.1f, fireScale = 3.2f;
        public float iceLifetime = .9f, fireLifetime = 1.5f;
        public int icePool = 4, firePool = 2;
    }
}
