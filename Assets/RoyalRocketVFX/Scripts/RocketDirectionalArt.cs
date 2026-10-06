using System;
using UnityEngine;

namespace RoyalRocketVFX
{
    /// <summary>
    /// One directional rocket texture plus the trimmed pixel rect actually occupied by the art
    /// and the pivot that sits on the nozzle. The pivot is what keeps the flame welded to the
    /// rocket with no gap, and the trim rect is what makes "rocket scale" mean the visible art
    /// rather than the transparent padding around it.
    /// </summary>
    [Serializable]
    public struct RocketDirectionalArt
    {
        [Tooltip("Source texture for this direction.")]
        public Texture2D texture;

        [Tooltip("Pixel rect of the visible artwork inside the texture (x, y, width, height).")]
        public Rect trimRect;

        [Tooltip("Normalised pivot inside the trim rect. Put it on the nozzle: the flame spawns here.")]
        public Vector2 nozzlePivot;

        public Sprite CreateSprite(string name)
        {
            if (texture == null) return null;
            var rect = trimRect.width > 0f && trimRect.height > 0f
                ? trimRect
                : new Rect(0f, 0f, texture.width, texture.height);
            var sprite = Sprite.Create(texture, rect, nozzlePivot, 100f, 0, SpriteMeshType.FullRect);
            sprite.name = name;
            return sprite;
        }
    }
}
