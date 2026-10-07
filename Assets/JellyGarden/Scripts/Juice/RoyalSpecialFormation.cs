using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RoyalThrone.ReferenceGameplay;

namespace JellyGarden.Scripts.Juice
{
    // Accepted special-creation groups use this presentation. Match detection,
    // host selection, collection credit and the reserved output cell remain with the resolver.
    public static class RoyalSpecialFormation
    {
        public const float TravelDuration = .24f;
        public const float Duration = TravelDuration;
        public static bool Prepare(List<Item> group)
        {
            if (!RoyalEffectsProfile.Get || group.Count < 4) return false;
            Item host = null;
            foreach (var item in group)
            {
                if (!item || item.destroying || item.currentType != ItemsTypes.NONE || !item.sprRenderer) return false;
                if (Supported(item.NextType))
                {
                    if (host && host != item) return false;
                    host = item;
                }
                else if (item.NextType != ItemsTypes.NONE) return false;
            }
            if (!host || !ReferencePieceVisual.Has(host.NextType)) return false;
            foreach (var item in group)
            {
                item.royalSpecialGather = true;
                item.royalSpecialGatherTarget = host.sprRenderer.bounds.center;
            }
            return true;
        }
        public static bool Supported(ItemsTypes type) => type == ItemsTypes.HELICOPTER || type == ItemsTypes.PACKAGE ||
            type == ItemsTypes.HORIZONTAL_STRIPPED || type == ItemsTypes.VERTICAL_STRIPPED || type == ItemsTypes.BOMB;
        public static string CreationState(ItemsTypes type) => type == ItemsTypes.PACKAGE ? "TntCreationAnimation" :
            type == ItemsTypes.HORIZONTAL_STRIPPED ? "HorizontalRocketCreationAnimation" :
            type == ItemsTypes.VERTICAL_STRIPPED ? "VerticalRocketCreationAnimation" :
            type == ItemsTypes.BOMB ? "LightballCreationAnimation" : "PropellerCreationAnimation";

        public static IEnumerator Gather(Item item)
        {
            var source = item.sprRenderer;
            var go = new GameObject("Special gathering gem");
            go.transform.SetParent(item.transform, false);
            var sprite = go.AddComponent<SpriteRenderer>();
            sprite.sprite = source.sprite; sprite.sharedMaterial = source.sharedMaterial;
            sprite.sortingLayerID = source.sortingLayerID; sprite.sortingOrder = 34;
            sprite.color = source.color;
            go.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
            var inherited = item.transform.lossyScale;
            go.transform.localScale = new Vector3(source.transform.lossyScale.x / inherited.x, source.transform.lossyScale.y / inherited.y, 1);
            Vector3 start = go.transform.position;
            Vector3 target = item.royalSpecialGatherTarget - (sprite.bounds.center - go.transform.position);
            source.forceRenderingOff = true;
            if (item.anim) item.anim.enabled = false;
            var shadow = item.transform.Find("Royal contact shadow");
            if (shadow) shadow.gameObject.SetActive(false);
            try
            {
                float elapsed = 0;
                while (elapsed < Duration)
                {
                    if (LevelManager.THIS.gameStatus != GameState.Pause) elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / TravelDuration);
                    float travel = t * t * (3 - 2 * t);
                    // Keep the full-size gem merge, then hand creation to the original
                    // piece Reveal. Do not substitute a settled preview or skip its clip.
                    go.transform.position = Vector3.Lerp(start,target,travel);
                    yield return null;
                }
                item.SetAnimationDestroyingFinished();
            }
            finally
            {
                if (go) Object.Destroy(go);
            }
        }
    }
}
