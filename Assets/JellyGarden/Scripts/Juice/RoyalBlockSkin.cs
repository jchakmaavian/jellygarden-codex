using UnityEngine;
using RoyalThrone.Presentation;

namespace JellyGarden.Scripts.Juice
{
    public sealed class RoyalBlockSkin:MonoBehaviour
    {
        SpriteRenderer visual;
        void Awake(){visual=GetComponent<SpriteRenderer>();}
        void LateUpdate(){if(visual&&visual.sprite)visual.sprite=RoyalVisualCatalog.Replace(visual.sprite,true);}
    }
}
