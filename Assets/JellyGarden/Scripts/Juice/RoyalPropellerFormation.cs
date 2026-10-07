using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RoyalThrone.ReferenceGameplay;

namespace JellyGarden.Scripts.Juice
{
    // Only the accepted 2x2 Propeller group uses this presentation. Match detection,
    // host selection, collection credit and the reserved output cell remain with the resolver.
    public static class RoyalPropellerFormation
    {
        public const float TravelDuration = .24f;
        public const float BlendDuration = .14f;
        public const float Duration = TravelDuration + BlendDuration;
        public static bool Prepare(List<Item> group)
        {
            if (!RoyalEffectsProfile.Get || !ReferencePieceVisual.Has(ItemsTypes.HELICOPTER) || group.Count != 4) return false;
            Item host = null;
            foreach (var item in group)
            {
                if (!item || item.destroying || item.currentType != ItemsTypes.NONE || !item.sprRenderer) return false;
                if (item.NextType == ItemsTypes.HELICOPTER) host = item;
            }
            if (!host) return false;
            foreach (var item in group)
            {
                item.royalPropellerGather = true;
                item.royalPropellerGatherTarget = host.sprRenderer.bounds.center;
            }
            return true;
        }

        public static IEnumerator Gather(Item item)
        {
            var source = item.sprRenderer;
            var go = new GameObject("Propeller gathering gem");
            go.transform.SetParent(item.transform, false);
            var sprite = go.AddComponent<SpriteRenderer>();
            sprite.sprite = source.sprite; sprite.sharedMaterial = source.sharedMaterial;
            sprite.sortingLayerID = source.sortingLayerID; sprite.sortingOrder = 34;
            sprite.color = source.color;
            go.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
            var inherited = item.transform.lossyScale;
            go.transform.localScale = new Vector3(source.transform.lossyScale.x / inherited.x, source.transform.lossyScale.y / inherited.y, 1);
            Vector3 start = go.transform.position;
            Vector3 target = item.royalPropellerGatherTarget - (sprite.bounds.center - go.transform.position);
            GameObject result = null;
            ReferenceActivationVisual resultView = null;
            SpriteRenderer[] resultSprites = null;
            Color[] resultColors = null;
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
                    // Keep every gem at its original size. Only after they occupy
                    // the same location does the merged silhouette blend into the Propeller.
                    go.transform.position = Vector3.Lerp(start,target,travel);
                    float blend = Mathf.SmoothStep(0,1,Mathf.Clamp01((elapsed-TravelDuration)/BlendDuration));
                    sprite.color = new Color(1,1,1,1-blend);
                    if (elapsed >= TravelDuration && item.NextType == ItemsTypes.HELICOPTER)
                    {
                        if (!result)
                        {
                            result = new GameObject("Propeller merge result");
                            result.transform.SetParent(item.transform,false);
                            result.transform.position = item.square.transform.position + Vector3.back * .2f;
                            resultView = ReferenceActivationVisual.Attach(result,"Propeller",35);
                            resultView.Play("PropellerCreationAnimation", normalizedTime:1);
                            resultView.Hold();
                            resultSprites = result.GetComponentsInChildren<SpriteRenderer>(true);
                            resultColors = new Color[resultSprites.Length];
                            for (int i=0;i<resultSprites.Length;i++) resultColors[i]=resultSprites[i].color;
                        }
                        for (int i=0;i<resultSprites.Length;i++)
                        { var color=resultColors[i];color.a*=blend;resultSprites[i].color=color; }
                    }
                    yield return null;
                }
                item.SetAnimationDestroyingFinished();
            }
            finally
            {
                if (resultView) resultView.Release();
                if (result) Object.Destroy(result);
                if (go) Object.Destroy(go);
            }
        }
    }
}
