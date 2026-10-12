using System;
using UnityEngine;

namespace JellyGarden.Scripts.Juice
{
    public sealed class RoyalBurstLibrary : ScriptableObject
    {
        [Serializable] public sealed class Piece { public Sprite sprite; public Vector2 offset; }
        [Serializable] public sealed class Burst { public string color; public Piece[] pieces; }
        public Burst[] bursts;
    }
}
