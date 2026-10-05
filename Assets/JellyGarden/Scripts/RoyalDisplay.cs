using System.Collections;
using UnityEngine;

namespace RoyalThrone
{
    /// <summary>
    /// Android: the game owns the whole screen, with no status bar (the user's request, 2026-10-05).
    /// The player settings (start in full screen, Fullscreen Window, render outside the safe area) were not enough on their
    /// own: since Unity 6.1 the GameActivity applies full screen through WindowInsetsController, and the status bar stayed on
    /// the user's phone. So this asks Android directly: hide the status and navigation bars (a swipe from the edge shows them
    /// for a moment, as in other games), draw into the camera cutout, and keep the layout behind the bars. It runs at launch,
    /// twice more as the first scenes come up (Unity applies its own settings then), and whenever the app returns to the front.
    /// The UI keeps to Screen.safeArea, so nothing that matters sits under a notch.
    /// </summary>
    public static class RoyalDisplay
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Enforce()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            Screen.fullScreen = true;
            HideSystemBars();
            Application.focusChanged += focused => { if (focused) { Screen.fullScreen = true; HideSystemBars(); } };
            var host = new GameObject("Royal display (full screen)") { hideFlags = HideFlags.HideAndDontSave };
            Object.DontDestroyOnLoad(host);
            host.AddComponent<Reapply>();
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        sealed class Reapply : MonoBehaviour
        {
            IEnumerator Start()
            {
                yield return new WaitForSecondsRealtime(.5f); HideSystemBars();
                yield return new WaitForSecondsRealtime(2f); HideSystemBars();
            }
        }

        static void HideSystemBars()
        {
            try
            {
                var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                if (activity == null) return;
                activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                {
                    try
                    {
                        var window = activity.Call<AndroidJavaObject>("getWindow");
                        int sdk = new AndroidJavaClass("android.os.Build$VERSION").GetStatic<int>("SDK_INT");
                        if (sdk >= 28)
                        {
                            // LAYOUT_IN_DISPLAY_CUTOUT_MODE_SHORT_EDGES: draw into the cutout beside the camera.
                            var attributes = window.Call<AndroidJavaObject>("getAttributes");
                            attributes.Set("layoutInDisplayCutoutMode", 1);
                            window.Call("setAttributes", attributes);
                        }
                        if (sdk >= 30)
                        {
                            window.Call("setDecorFitsSystemWindows", false);
                            var controller = window.Call<AndroidJavaObject>("getInsetsController");
                            if (controller != null)
                            {
                                var types = new AndroidJavaClass("android.view.WindowInsets$Type");
                                controller.Call("hide", types.CallStatic<int>("statusBars") | types.CallStatic<int>("navigationBars"));
                                controller.Call("setSystemBarsBehavior", 2);   // BEHAVIOR_SHOW_TRANSIENT_BARS_BY_SWIPE
                            }
                        }
                        else
                        {
                            // IMMERSIVE_STICKY | FULLSCREEN | HIDE_NAVIGATION | LAYOUT_FULLSCREEN | LAYOUT_HIDE_NAVIGATION | LAYOUT_STABLE
                            window.Call<AndroidJavaObject>("getDecorView").Call("setSystemUiVisibility", 0x1000 | 0x4 | 0x2 | 0x400 | 0x200 | 0x100);
                        }
                    }
                    catch (System.Exception e) { Debug.LogWarning("[Royal display] could not hide the system bars: " + e.Message); }
                }));
            }
            catch (System.Exception e) { Debug.LogWarning("[Royal display] could not hide the system bars: " + e.Message); }
        }
#endif
    }
}
