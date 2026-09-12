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
//   * `Capture()` is the one screenshot call, and it reads the back buffer at
//     the window's own size — 393x852. It used to render the frame again at
//     twice that, for the good reason that 393 points is small to read a target
//     chip in. It cost more than it bought: the re-render drops the Screen
//     Space - Camera canvas, so every shot the project had ever taken was
//     missing the whole level HUD and showed the room behind the board as a
//     blur. A small honest picture beats a large one with the HUD cut out of
//     it. `CaptureSuperSized` is still there for anything that wants the 2x
//     render knowing what it loses.

using UnityEngine;

namespace JellyGarden.Scripts.Story
{
    public static class StoryWindow
    {
        /// <summary>iPhone 15/16 portrait, in points.</summary>
        public const int Width = 393;

        public const int Height = 852;

        /// <summary>
        /// The multiple <see cref="CaptureSuperSized"/> renders at. The default
        /// capture no longer uses it — see the note on that method — but the
        /// number is still the one thing that decides how big a 2x shot is, and
        /// tools/check_launch_args.py reads it.
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
            CaptureNative(path);
        }

        /// <summary>
        /// A GPU-only copy of the current back buffer for a modal's blurred
        /// backdrop. Like CaptureNative, call after WaitForEndOfFrame; it keeps
        /// Screen Space - Camera UI and avoids a second camera render.
        /// </summary>
        public static void CaptureInto(RenderTexture target)
        {
            ScreenCapture.CaptureScreenshotIntoRenderTexture(target);
        }

        /// <summary>
        /// The old capture: Unity renders the frame AGAIN at
        /// <see cref="ShotSuperSize"/> times the window and writes that.
        ///
        /// Kept, and not used, because it does not photograph this game. The
        /// re-render loses the Screen Space - Camera canvas the level's HUD and
        /// the arc's backdrop both live on: the board comes back sharp, the
        /// Target plate and the Moves counter are simply absent, and the room
        /// behind the board comes back as a smear. Two entries in
        /// ralph/findings.md — "no HUD is drawn over the board" and "the room
        /// behind the board is a dark blur" — were this, not the game. The two
        /// shots were taken of the same frame, a millisecond apart, and only one
        /// of them has a HUD in it.
        ///
        /// Anything that genuinely wants a 2x render can call this; nothing
        /// that is looking for a visual fault should.
        /// </summary>
        public static void CaptureSuperSized(string path)
        {
            ScreenCapture.CaptureScreenshot(path, ShotSuperSize);
        }

        /// <summary>
        /// The same frame, read straight off the back buffer at the window's own
        /// size — no superSize, no re-render.
        ///
        /// It exists because the two do not agree. `Capture` at superSize 2 asks
        /// Unity to render the frame again at twice the size, and a Screen Space
        /// Camera canvas does not survive that intact: the board comes back
        /// sharp and the backdrop behind it comes back as a smear, which is what
        /// "the room behind the board is a dark blur" turned out to be. The
        /// picture itself is a 1152x2048 texture drawn into 967x1548 — a
        /// downscale, and sharp on screen.
        ///
        /// So a shot that a layout is judged from can be taken at either size,
        /// but a shot that ARTWORK is judged from has to be this one. Must be
        /// called after WaitForEndOfFrame, like any back-buffer read.
        /// </summary>
        public static void CaptureNative(string path)
        {
            var frame = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            frame.ReadPixels(new Rect(0f, 0f, Screen.width, Screen.height), 0, 0);
            frame.Apply();

            byte[] png = frame.EncodeToPNG();
            Object.Destroy(frame);

            try
            {
                global::System.IO.File.WriteAllBytes(path, png);
            }
            catch (global::System.Exception e)
            {
                Debug.LogWarning("[Story] could not write " + path + ": " + e.Message);
            }
        }
    }
}
