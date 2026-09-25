using UnityEngine;

namespace RoyalThrone.Presentation
{
    /// <summary>
    /// One Back / Escape gesture (press, then release) is handled once. Every handler that closes or cancels
    /// something because of Back calls <see cref="Consume"/>, and the home screen only quits the app when nothing
    /// consumed the gesture. Without this, Back that dismissed a home dialog (closed on press) or a popup (closed on
    /// release, earlier in the same frame) also reached the quit branch and exited the game.
    /// </summary>
    public static class RoyalBackButton
    {
        static int pressFrame = -1, consumedFrame = -1;

        /// <summary>Quit requests made through <see cref="Quit"/> (Application.Quit itself is ignored in the Editor).</summary>
        public static int QuitRequests { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { pressFrame = consumedFrame = -1; QuitRequests = 0; }

        /// <summary>Record the frame a Back press begins (any order relative to the handlers of that frame).</summary>
        public static void NotePress() { pressFrame = Time.frameCount; }

        /// <summary>The current Back gesture closed or cancelled something; it must not do anything else.</summary>
        public static void Consume() { consumedFrame = Time.frameCount; }

        /// <summary>True when something consumed the gesture that began with the latest press.</summary>
        public static bool Consumed => pressFrame >= 0 && consumedFrame >= pressFrame;

        /// <summary>True when a handler consumed a Back gesture in this very frame, even if its press was not noted.</summary>
        public static bool ConsumedThisFrame => consumedFrame == Time.frameCount;

        public static void Quit()
        {
            QuitRequests++;
            Application.Quit();
        }
    }
}
