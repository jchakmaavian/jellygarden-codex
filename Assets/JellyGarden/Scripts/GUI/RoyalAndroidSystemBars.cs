#if UNITY_ANDROID
using System.Collections;
using UnityEngine;
using UnityEngine.Android;

namespace RoyalThrone.Presentation
{
    // Player Settings hide the navigation bar and reveal it only transiently by swipe, but on Android 11 the player
    // does not apply that behaviour: the first touch reveals the bar (swallowing the touch) and it stays over the
    // booster and home tab bars. Apply the behaviour and hide the bar at startup and whenever the game regains focus.
    // Deliberately not on every insets change: the transient swipe must still let players reach Back/Home.
    public sealed class RoyalAndroidSystemBars : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (Application.isEditor) return;
            var host = new GameObject("Royal Android system bars");
            DontDestroyOnLoad(host);
            host.AddComponent<RoyalAndroidSystemBars>();
        }

        void Start()
        {
            StartCoroutine(ApplyImmersiveNavigation());
        }

        void OnApplicationFocus(bool focused)
        {
            if (focused) StartCoroutine(ApplyImmersiveNavigation());
        }

        static IEnumerator ApplyImmersiveNavigation()
        {
            // Let the player finish its own resume handling first; it can run after the focus callback.
            yield return new WaitForSecondsRealtime(0.5f);
            var insets = AndroidApplication.currentWindowInsets;
            if (insets == null) yield break;
            insets.SetSystemBarsBehavior(AndroidWindowInsets.SystemBarsBehavior.ShowTransientBarsBySwipe);
            if (insets.IsVisible(AndroidWindowInsets.Type.NavigationBars))
                insets.Hide(AndroidWindowInsets.Type.NavigationBars);
        }
    }
}
#endif
