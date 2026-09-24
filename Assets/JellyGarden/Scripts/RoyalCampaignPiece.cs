using UnityEngine;
namespace RoyalThrone.Campaign
{
    // Runtime-only source identity for palette assignment; never stored on a game prefab.
    public sealed class RoyalCampaignPiece : MonoBehaviour
    {
        public int code;
        public bool fixedOpening,assigned;
    }
}
