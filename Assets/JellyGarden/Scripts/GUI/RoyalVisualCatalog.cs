using System.Collections.Generic;
using JellyGarden.Scripts;
using JellyGarden.Scripts.Juice;
using JellyGarden.Scripts.Targets;
using UnityEngine;

namespace RoyalThrone.Presentation
{
    public static class RoyalVisualCatalog
    {
        static readonly Dictionary<Sprite,Sprite> replacements=new Dictionary<Sprite,Sprite>();
        static readonly HashSet<Sprite> generated=new HashSet<Sprite>();
        public static Sprite Goal(TargetObject target)
        {
            if(!RoyalEffectsProfile.Get)return target.icon;
            if(target.type==Target.SCORE)return RoyalPieceSkin.Art("star");
            if(target.type==Target.COLLECT)return RoyalPieceSkin.For(ItemsTypes.NONE,target.color);
            return Replace(target.icon);
        }
        public static Sprite Replace(Sprite original)
        {
            if(!original||generated.Contains(original))return original;
            if(replacements.TryGetValue(original,out var cached)&&cached)return cached;
            string n=original.name.ToLowerInvariant(),art=null;
            if(n=="block"||n=="block_02")art="ice";
            else if(n.Contains("solidblock"))art="crate";
            else if(n.Contains("undestroyable"))art="stone";
            else if(n.Contains("wireblock"))art="chains";
            else if(n.Contains("thriving"))art="leaf";
            else if(n.Contains("ingredient_01"))art="key";
            else if(n.Contains("ingredient_02"))art="chest";
            if(art==null)return original;
            var source=RoyalPieceSkin.Art(art);if(!source)return original;
            float extent=Mathf.Max(original.bounds.size.x,original.bounds.size.y);
            var sprite=Sprite.Create(source.texture,source.rect,new Vector2(.5f,.5f),Mathf.Max(source.rect.width,source.rect.height)/Mathf.Max(.01f,extent),0,SpriteMeshType.FullRect);
            // Legacy target matching compares sprite names. Retain its data identifier.
            sprite.name=original.name;sprite.hideFlags=HideFlags.DontSave;replacements[original]=sprite;generated.Add(sprite);return sprite;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics(){foreach(var s in generated)if(s)Object.Destroy(s);generated.Clear();replacements.Clear();}
    }
}
