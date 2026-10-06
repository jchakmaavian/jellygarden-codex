#if ROYAL_REFERENCE_CAMPAIGN && (UNITY_EDITOR || DEVELOPMENT_BUILD) && !ROYAL_DISABLE_QA
using System.Collections;
using RoyalThrone.Testing;
using UnityEngine;

namespace RoyalThrone.Campaign
{
    // Included only in the separate levels01-50 test package by extraScriptingDefines.
    public sealed class RoyalReferenceCampaignBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if(FindFirstObjectByType<RoyalReferenceCampaignBootstrap>())return;
            var host=new GameObject("Reference campaign test package");DontDestroyOnLoad(host);host.AddComponent<RoyalReferenceCampaignBootstrap>();
        }
        IEnumerator Start()
        {
            // Splash remains available; after Play enters the game scene, select the
            // campaign before the first interactable gameplay frame.
            while(!RoyalQaService.Instance||!RoyalQaService.Instance.Ready)yield return null;
            var qa=RoyalQaService.Instance;qa.StartSession();RoyalReferenceCampaign.Select(true);
            qa.Visible=false;qa.Level=1;
            qa.Status="LEVELS 01–50 TEST BUILD. Separate campaign save. Runtime adaptations are documented in the inspection notes.";
            Destroy(gameObject);
        }
    }
}
#endif
