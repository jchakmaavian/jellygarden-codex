// Opens one board in every arc and says whether it reached the screen.
//
// Levels wear Royal Throne's board backdrop now (port decision D6), so an arc's
// theme no longer dresses the board and nothing here asserts a backdrop or a
// cell ground. What is left is the check that caught a board drawn under a
// black scrim: launch the first level of each arc into its own area, read the
// frame back, and report a frame too dark to be a board.
//
//     tools/run_player.sh -storythemetest
//
// The report is Application.persistentDataPath/themetest.txt and the six PNGs
// are next to it in themetest/. A FINDING line is an arc whose board did not
// reach the screen; the pictures are for everything a check cannot see.

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using JellyGarden.Scripts.GUI;
using JellyGarden.Scripts.GUI.TutorialStuff;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    [DisallowMultipleComponent]
    public sealed class StoryThemeTest : MonoBehaviour
    {
        private const string Flag = "-storythemetest";

        /// <summary>Set by an Editor runner, which cannot pass a command line.</summary>
        private const string Pref = "StoryThemeTest.Requested";

        /// <summary>Written when the run is over, so a host can stop waiting.</summary>
        public const string ReportName = "themetest.txt";

        /// <summary>The arcs the story has. StoryTheme is asked about each one.</summary>
        private const int ArcCount = 6;

        private static bool _installed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void Install()
        {
            if (_installed) return;
            if (!Requested()) return;

            _installed = true;
            var host = new GameObject("~StoryThemeTest");
            DontDestroyOnLoad(host);
            host.AddComponent<StoryThemeTest>();
        }

        private static bool Requested()
        {
            foreach (string arg in global::System.Environment.GetCommandLineArgs())
                if (arg == Flag) return true;

            if (PlayerPrefs.GetInt(Pref, 0) != 1) return false;

            // One shot, for the same reason StoryPlaytest clears its own: a pref
            // left up turns the tester's next press of Play into another run.
            PlayerPrefs.SetInt(Pref, 0);
            PlayerPrefs.Save();
            return true;
        }

        #region The report

        private readonly List<string> _lines = new List<string>();
        private readonly List<string> _findings = new List<string>();

        private int _arc;
        private string _shotDir = string.Empty;

        private void Say(string line)
        {
            _lines.Add(line);
            Debug.Log("[ThemeTest] " + line);
        }

        private void Finding(string what)
        {
            string named = (_arc > 0 ? "arc " + _arc + ": " : string.Empty) + what;
            _findings.Add(named);
            _lines.Add("FINDING: " + named);
            Debug.LogWarning("[ThemeTest] FINDING: " + named);
        }

        #endregion

        private void Start()
        {
            StartCoroutine(Drive());
        }

        private IEnumerator Drive()
        {
            OpenShotDir();

            Say(string.Format("window {0}x{1}{2}; shots at {3}x.",
                Screen.width, Screen.height,
                Screen.fullScreen ? " FULL SCREEN" : " windowed",
                StoryWindow.ShotSuperSize));
            Say(_shotDir.Length > 0
                ? "screenshots in " + _shotDir
                : "screenshots: none (headless, or the directory could not be made).");
            Say(StoryTheme.Count + " arc theme(s) loaded from Resources/Story/themes.json.");

            yield return new WaitForSecondsRealtime(2f);

            for (int arc = 1; arc <= ArcCount; arc++)
            {
                _arc = arc;

                // The theme only names the shot now; the board does not wear it.
                ArcTheme theme = StoryTheme.ForArc(arc);
                string themeKey = theme == null ? "arc" + arc : theme.ThemeKey;

                int level = FirstLevelOf(arc);
                if (level <= 0)
                {
                    Finding("no level in the plan belongs to it, so no board of it is " +
                            "ever opened by anything");
                    continue;
                }

                int areaId = StoryLevelPlan.AreaOf(level);
                Say(string.Format("---- arc {0} ({1}) — level {2}, area {3} ----",
                    arc, themeKey, level, areaId));

                // Leaving the last level drops the player on home, and home is
                // where the story's beats fire. Launch from a calm shell.
                yield return WaitForCalm(BeatTimeout);

                yield return Reach(level, areaId);

                if (LevelManager.THIS == null || LevelManager.THIS.gameStatus != GameState.Playing)
                {
                    Finding(string.Format(
                        "level {0} never became playable — state {1}. Nothing was drawn, " +
                        "so the board could not be looked at.", level,
                        LevelManager.THIS == null
                            ? "no LevelManager"
                            : LevelManager.THIS.gameStatus.ToString()));
                    yield return Shot(arc, themeKey);
                    continue;
                }

                HideTutorial();

                // The board can be live and themed with a beat still drawn over
                // it — that is exactly the fault this test read past on its
                // first run, reporting six correct themes and photographing one
                // board and five copies of the opening cutscene.
                yield return WaitForCalm(BeatTimeout);
                if (StoryShell.StoryIsPlaying)
                {
                    Finding(string.Format(
                        "a story beat was still on screen {0}s after the board opened, so " +
                        "the picture of this arc is a picture of the beat", BeatTimeout));
                }

                yield return new WaitForSecondsRealtime(1f);

                yield return MeasureFrame();
                yield return Shot(arc, themeKey);
                yield return LeaveLevel();
            }

            _arc = 0;
            Finish();
        }

        /// <summary>The lowest level number the plan gives this arc.</summary>
        private static int FirstLevelOf(int arc)
        {
            StoryLevelPlan.Load();

            int best = 0;
            for (int level = 1; level <= StoryLevelPlan.LevelCount; level++)
            {
                StoryLevelEntry entry = StoryLevelPlan.Get(level);
                if (entry == null || entry.Arc != arc) continue;
                if (StoryDatabase.GetArea(entry.AreaId) == null) continue;
                best = level;
                break;
            }
            return best;
        }

        /// <summary>Gets to a live board without anyone clicking a map node.</summary>
        private IEnumerator Reach(int level, int areaId)
        {
            if (LevelManager.THIS == null)
            {
                SceneManager.LoadScene("game");
                float wait = 0f;
                while (wait < 25f && LevelManager.THIS == null)
                {
                    yield return new WaitForSecondsRealtime(0.25f);
                    wait += 0.25f;
                }
                yield return new WaitForSecondsRealtime(1.5f);
            }

            if (LevelManager.THIS == null) yield break;

            StoryFlow.LaunchLevel(level, areaId);

            float waited = 0f;
            while (waited < 30f && LevelManager.THIS.gameStatus != GameState.Playing)
            {
                yield return new WaitForSecondsRealtime(0.25f);
                waited += 0.25f;
            }

            // The board finishes falling in after it starts playing. This is a
            // picture of a board, so it waits for the board to be still.
            yield return new WaitForSecondsRealtime(2.5f);
        }

        /// <summary>
        /// Takes the kit's level-1 tutorial off the screen before the shot.
        ///
        /// TutorialManager dims the whole screen and puts a pointing hand over
        /// the board on level 1, which is arc 1's first level and therefore
        /// exactly the frame this test is here to photograph. A shot of the
        /// overlay says nothing about arc 1's board. Nothing is destroyed — the
        /// object is switched off in a run that exists to take pictures.
        /// </summary>
        private void HideTutorial()
        {
            int hidden = 0;

            foreach (TutorialManager manager in
                     FindObjectsByType<TutorialManager>(FindObjectsInactive.Include))
            {
                if (manager == null || manager.tutorial == null) continue;
                if (!manager.tutorial.activeSelf) continue;
                manager.tutorial.SetActive(false);
                hidden++;
            }

            if (hidden > 0) Say("kit tutorial overlay hidden for the shot.");
        }

        /// <summary>A colour as it reads in a report: 0..255 per channel plus alpha.</summary>
        private static string Describe(Color colour)
        {
            return string.Format("{0},{1},{2} @ {3:0.00}",
                Mathf.RoundToInt(colour.r * 255f), Mathf.RoundToInt(colour.g * 255f),
                Mathf.RoundToInt(colour.b * 255f), colour.a);
        }

        /// <summary>Hands the level back the way the game does.</summary>
        private IEnumerator LeaveLevel()
        {
            StoryFlow.HandleLevelFailed();
            yield return new WaitForSecondsRealtime(3f);
        }

        /// <summary>How long a beat gets to finish before it is the finding.</summary>
        private const float BeatTimeout = 25f;

        private const float Poll = 0.4f;

        /// <summary>
        /// Waits until nothing the story is saying is on screen, tapping through
        /// dialogue the way a player would.
        ///
        /// Every screen in this game can have an opening sequence, a clue card
        /// or an arc beat drawn over it, and a beat waits for a tap forever. A
        /// screenshot taken under one is a screenshot of the beat — which is
        /// what the first run of this test produced for five of the six arcs
        /// while reporting that all six themes were correct.
        /// </summary>
        private IEnumerator WaitForCalm(float timeout)
        {
            float waited = 0f;
            while (waited < timeout)
            {
                if (!StoryShell.StoryIsPlaying) yield break;
                PressOnThroughDialogue();
                yield return new WaitForSecondsRealtime(Poll);
                waited += Poll;
            }
        }

        /// <summary>
        /// Dialogue never advances itself: one tap finishes the line, the next
        /// moves on. This presses the screen's own full-screen Advance button —
        /// the handler a tap would reach — rather than synthesising a click.
        /// Same shape as StoryShellTest.PressOnThroughDialogue.
        /// </summary>
        private static void PressOnThroughDialogue()
        {
            if (!DialogueSystem.IsPlaying) return;

            GameObject host = GameObject.Find("~StoryDialogue");
            if (host == null) return;

            Button[] buttons = host.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
                if (buttons[i].name == "Advance") buttons[i].onClick.Invoke();
        }

        #region What actually reached the screen

        /// <summary>
        /// How bright the frame is allowed to come out, as a fraction of the
        /// board backdrop's own luminance.
        ///
        /// The whole frame is that backdrop with lighter pieces, cells and HUD
        /// over it, so a correct frame reads at or above it. The kit's black
        /// scrim at 0.87 takes any frame to about a seventh of itself, well
        /// under this floor.
        /// </summary>
        private const float MinFrameBrightnessFraction = 0.35f;

        /// <summary>
        /// The camera clear and backdrop navy GUI/RoyalGameplayPresentation
        /// draws behind every board.
        /// </summary>
        private static readonly Color BoardBackdrop = new Color(0.047f, 0.208f, 0.49f);

        /// <summary>
        /// Reads the frame the player would see and reports one too dark to be
        /// a board.
        ///
        /// This is the one check here that cannot be fooled by the scene graph
        /// being correct. Everything else asks an object what it is set to; this
        /// asks the screen what it looks like.
        /// </summary>
        private IEnumerator MeasureFrame()
        {
            // WaitForEndOfFrame never returns under -nographics, and a batchmode
            // Editor hung on it in the baseline run.
            if (Application.isBatchMode ||
                SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                yield break;

            yield return new WaitForEndOfFrame();

            var shot = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            shot.ReadPixels(new Rect(0f, 0f, Screen.width, Screen.height), 0, 0);
            shot.Apply();

            Color[] pixels = shot.GetPixels();
            Destroy(shot);

            if (pixels.Length == 0) yield break;

            float sum = 0f;
            float brightest = 0f;
            for (int i = 0; i < pixels.Length; i++)
            {
                float luminance = 0.2126f * pixels[i].r + 0.7152f * pixels[i].g +
                                  0.0722f * pixels[i].b;
                sum += luminance;
                if (luminance > brightest) brightest = luminance;
            }

            float mean = sum / pixels.Length;
            float ground = 0.2126f * BoardBackdrop.r + 0.7152f * BoardBackdrop.g +
                           0.0722f * BoardBackdrop.b;

            Say(string.Format("frame: mean luminance {0:0}, brightest {1:0}, " +
                              "against a board backdrop of {2:0}.",
                mean * 255f, brightest * 255f, ground * 255f));

            if (mean < ground * MinFrameBrightnessFraction)
            {
                // The mean and not the brightest pixel: one stray highlight is
                // enough to make a screen that is black to a player look fine to
                // a max. Arc 6 measured a brightest of 255 while its mean sat at
                // 40, and the picture was unreadable.
                Finding(string.Format(
                    "the frame is dark: mean luminance {0:0} against a board backdrop of " +
                    "{1:0} — something is drawn over the board. {2}",
                    mean * 255f, ground * 255f, WhatCoversTheScreen()));
            }
        }

        /// <summary>
        /// Names the full-screen graphics drawn over the board, brightest-first
        /// by how much they hide.
        ///
        /// A finding that says "the frame is dark" and stops costs the reader the
        /// whole hierarchy. This walks every enabled Graphic, keeps the ones that
        /// cover most of the screen and are not near-transparent, and names them
        /// by their path — which is what a person would have gone looking for.
        /// </summary>
        private static string WhatCoversTheScreen()
        {
            var covers = new List<string>();

            foreach (Graphic graphic in
                     FindObjectsByType<Graphic>(FindObjectsInactive.Exclude))
            {
                if (graphic == null || !graphic.isActiveAndEnabled) continue;
                if (graphic.color.a < 0.5f) continue;

                // Screen coverage from the rect's own world corners, which is
                // the only measurement that works across a Screen Space Camera
                // canvas, an Overlay one and a scaled one alike. The first
                // version of this divided a lossy-scaled size by the canvas
                // scale factor and found nothing at all over a screen that was
                // 87% black.
                Vector3[] corners = new Vector3[4];
                graphic.rectTransform.GetWorldCorners(corners);

                Canvas canvas = graphic.canvas;
                Camera camera = canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay
                    ? null
                    : canvas.worldCamera;

                Vector2 low = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
                Vector2 high = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
                float width = Mathf.Abs(high.x - low.x);
                float height = Mathf.Abs(high.y - low.y);
                float screen = Screen.width * Screen.height;
                if (screen <= 0f || width * height < screen * 0.5f) continue;

                covers.Add(string.Format("{0} [{1} {2:0}x{3:0} {4} order {5}]",
                    PathOf(graphic.transform), graphic.GetType().Name, width, height,
                    Describe(graphic.color),
                    canvas == null ? 0 : canvas.sortingOrder));
            }

            if (covers.Count == 0)
                return "No full-screen UI layer is drawn over it. " + WhatElseIsDark();

            return "Drawn over half the screen or more: " +
                   string.Join("; ", covers.ToArray()) + ". " + WhatElseIsDark();
        }

        /// <summary>
        /// The other three things that can darken every pixel at once when no UI
        /// layer is doing it: a camera clearing to a dark colour or dimmed by its
        /// own settings, the scene's ambient light (the board's sprites are lit),
        /// and the backdrop Image's own tint. Reported together because a
        /// finding that only rules things out costs the reader the search.
        /// </summary>
        private static string WhatElseIsDark()
        {
            var notes = new List<string>();

            foreach (Camera camera in FindObjectsByType<Camera>(FindObjectsInactive.Exclude))
            {
                if (camera == null || !camera.isActiveAndEnabled) continue;
                notes.Add(string.Format("camera '{0}' clears {1} to {2}", camera.name,
                    camera.clearFlags, Describe(camera.backgroundColor)));
            }

            notes.Add("ambient " + Describe(RenderSettings.ambientLight) +
                      " intensity " + RenderSettings.ambientIntensity.ToString("0.00"));

            var background = FindFirstObjectByType<Background>(FindObjectsInactive.Include);
            var image = background == null ? null : background.GetComponent<Image>();
            if (image != null)
            {
                notes.Add(string.Format("backdrop Image tint {0}, canvas '{1}'",
                    Describe(image.color),
                    image.canvas == null ? "none" : image.canvas.name));
            }

            // One cell, read back off the renderer rather than off the theme:
            // the theme can be right and the renderer still be dark.
            if (LevelManager.THIS != null && LevelManager.THIS.squaresArray != null)
            {
                foreach (Square square in LevelManager.THIS.squaresArray)
                {
                    if (square == null) continue;
                    var cell = square.GetComponent<SpriteRenderer>();
                    if (cell == null || !cell.enabled || cell.sprite == null) continue;
                    notes.Add(string.Format("a cell renders {0} on '{1}' ({2})",
                        Describe(cell.color), cell.sprite.name,
                        cell.sharedMaterial == null ? "no material" : cell.sharedMaterial.name));
                    break;
                }
            }

            return string.Join("; ", notes.ToArray()) + ".";
        }

        private static string PathOf(Transform transform)
        {
            string path = transform.name;
            Transform parent = transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }

        #endregion

        #region Pictures and the report

        private void OpenShotDir()
        {
            // WaitForEndOfFrame never returns under -nographics or in a batchmode
            // Editor, and there is nothing to photograph there either.
            if (Application.isBatchMode ||
                SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                _shotDir = string.Empty;
                return;
            }

            string dir = Path.Combine(Application.persistentDataPath, "themetest");
            try
            {
                Directory.CreateDirectory(dir);
                _shotDir = dir;
            }
            catch (global::System.Exception e)
            {
                _shotDir = string.Empty;
                Debug.LogWarning("[ThemeTest] could not make " + dir + ": " + e.Message);
            }
        }

        /// <summary>
        /// One PNG per arc, named so the six sort into arc order and a reader can
        /// put them side by side. This is the half of the story no check covers:
        /// whether the arc looks like the place it is meant to be.
        /// </summary>
        private IEnumerator Shot(int arc, string themeKey)
        {
            if (_shotDir.Length == 0) yield break;

            yield return new WaitForEndOfFrame();

            string name = string.Format("arc_{0}_{1}.png", arc, themeKey);
            StoryWindow.Capture(Path.Combine(_shotDir, name));

            yield return new WaitForSecondsRealtime(0.6f);
            Say("shot -> " + name);
        }

        private void Finish()
        {
            var report = new StringBuilder();
            report.AppendLine("King Leo — one board per arc");
            report.AppendLine(global::System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            report.AppendLine();

            foreach (string line in _lines) report.AppendLine(line);

            report.AppendLine();
            report.AppendLine(_findings.Count + " finding(s).");
            foreach (string finding in _findings) report.AppendLine("  - " + finding);

            string path = Path.Combine(Application.persistentDataPath, ReportName);
            try
            {
                File.WriteAllText(path, report.ToString());
                Debug.Log("[ThemeTest] report -> " + path);
            }
            catch (global::System.Exception e)
            {
                Debug.LogWarning("[ThemeTest] could not write " + path + ": " + e.Message);
            }

            Debug.Log("[ThemeTest] done: " + _findings.Count + " finding(s).");
            Application.Quit(_findings.Count == 0 ? 0 : 1);
        }

        #endregion
    }
}
