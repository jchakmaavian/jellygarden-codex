using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JellyGarden.Scripts.Story
{
    // The treasure hunt starts with the first puzzle; returning players reach
    // their saved gates through the shell in the same game scene.
    public sealed class TreasureHuntEntry : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (!StoryCampaign.IsTreasureHunt || SceneManager.GetActiveScene().name != "main") return;
            var host = new GameObject("~TreasureHuntEntry");
            DontDestroyOnLoad(host);
            host.AddComponent<TreasureHuntEntry>();
        }

        private IEnumerator Start()
        {
            yield return null;
            SceneManager.LoadScene("game");
            Destroy(gameObject);
        }
    }
}
