// The one place that knows how big the game's window is.
//
// The player must never open full screen. A sweep of 1208 levels that takes
// over the display cannot be watched, cannot be interrupted, and — worse —
// screenshots the wrong shape: a full-screen capture on this machine is a
// 16:10 desktop, and the game ships on a 9:19.5 phone. A visual fault found in
// the wrong aspect is not a fault the player would ever see, and a real one
// hides in the letterboxing.
//
// So one size, everywhere: 393x852, which is the point size of an iPhone
// 15/16 in portrait. It is spelled out once here and once in
// `tools/run_player.sh`, and `tools/check_launch_args.py` fails if those two
// ever disagree or if any other file spells the flags out for itself.
//
// Two halves:
//   * `Apply()` is the belt to the launcher's braces. `tools/run_player.sh`
//     passes `-screen-fullscreen 0 -screen-width 393 -screen-height 852`, but
//     an app opened by double-clicking it takes whatever Unity last saved in
//     the player prefs — which is full screen once anyone has ever made it so.
//     Apply puts a desktop player back, and does nothing at all on a phone,
//     where full screen is the only thing there is.
//   * `Capture()` is the one screenshot call. superSize 2 renders the shot at
//     twice the window — 786x1704 — because a 393-point-wide PNG is too small
//     to read a target chip or a tile's sprite in, and reading them is the
//     whole point of taking it.

using UnityEngine;

namespace JellyGarden.Scripts.Story
{
    public static class StoryWindow
    {
        /// <summary>iPhone 15/16 portrait, in points.</summary>
        public const int Width = 393;

        public const int Height = 852;

        /// <summary>
        /// Screenshots render at this multiple of the window. 1 is unreadable at
        /// 393 points wide; 2 is 786x1704 and shows a tile's sprite.
        /// </summary>
        public const int ShotSuperSize = 2;

        /// <summary>
        /// The launch flags, for anything that has to print them. Scripts run
        /// `tools/run_player.sh` instead of pasting these.
        /// </summary>
        public static string LaunchArguments
        {
            get
            {
                return string.Format(
                    "-screen-fullscreen 0 -screen-width {0} -screen-height {1}",
                    Width, Height);
            }
        }

        /// <summary>True on a platform where a window is a thing that exists.</summary>
        private static bool IsDesktopPlayer
        {
            get
            {
                if (Application.isEditor) return false;

                return Application.platform == RuntimePlatform.OSXPlayer ||
                       Application.platform == RuntimePlatform.WindowsPlayer ||
                       Application.platform == RuntimePlatform.LinuxPlayer;
            }
        }

        /// <summary>
        /// Puts a desktop player into a 393x852 window if it is not in one
        /// already. A launch through `tools/run_player.sh` is already right and
        /// this is a no-op; a double-clicked app that Unity remembers as full
        /// screen is not, and this is the only thing that catches it.
        /// </summary>
        public static void Apply()
        {
            if (!IsDesktopPlayer) return;

            if (!Screen.fullScreen && Screen.width == Width && Screen.height == Height) return;

            Debug.Log(string.Format(
                "[Story] window {0}x{1}{2} -> {3}x{4} windowed.",
                Screen.width, Screen.height, Screen.fullScreen ? " full screen" : string.Empty,
                Width, Height));

            Screen.SetResolution(Width, Height, FullScreenMode.Windowed);
        }

        /// <summary>
        /// The screenshot call. Every capture in the story layer comes through
        /// here so none of them can be taken at the wrong size.
        /// </summary>
        public static void Capture(string path)
        {
            ScreenCapture.CaptureScreenshot(path, ShotSuperSize);
        }
    }
}
