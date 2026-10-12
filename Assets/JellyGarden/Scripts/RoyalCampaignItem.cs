using JellyGarden.Scripts;
using UnityEngine;

namespace RoyalThrone.Campaign
{
    /// <summary>A falling nonmatch egg/bonus coin using the existing Item lifecycle.</summary>
    [DisallowMultipleComponent]
    public sealed class RoyalCampaignItem : MonoBehaviour
    {
        Item item;
        int code;
        int lastWave = -1;
        bool collected;
        public Sprite Visual => RoyalCampaignBlockers.Art(code == 26 ? "egg" : "coin");
        public string GoalName => code == 26 ? "Egg" : "Coin";
        public void Configure(int tiledCode)
        {
            item = GetComponent<Item>();
            code = tiledCode;
            // A valid palette index keeps inherited animation and score lookups safe.
            // Square guards exclude this item from matching and dragging.
            item.color = 5;
            item.currentType = ItemsTypes.NONE;
            if (item.sprRenderer) item.sprRenderer.sprite = Visual;
        }
        public void Hit(int wave)
        {
            if (!item || item.destroying || lastWave == wave) return;
            lastWave = wave;
            item.DestroyItem(true);
        }
        public void Collect()
        {
            if (collected) return;
            collected = true;
            RoyalReferenceCampaign.CollectGoal(code == 26 ? "Egg" : "Coin");
        }
    }
}
