// Opens one board in every arc and says whether it is wearing that arc.
//
// tools/check_themes.py reads themes.json and the art beside it and can prove
// what the theme SAYS. It cannot prove what reaches a board: whether
// StoryArt.Load actually found the picture in a built player's Resources,
// whether ApplyBackdrop fell back to the kit's own, whether the ground reached
// the cells or the level opened before the theme did. Those only show on a live
// board, and the fault they produce — arc 6 wearing arc 1's tiles, or the kit's
// untinted default — raises no error and ends no level.
//
// So this is the runtime half: six levels, one per arc, each launched into the
// area it belongs to, each verified and each photographed.
//
//     tools/run_player.sh -storythemetest
//
// The report is Application.persistentDataPath/themetest.txt and the six PNGs
// are next to it in themetest/. A FINDING line is an arc that is not wearing
// itself; the pictures are for everything a check cannot see.

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

        /// <summary>
        /// What each arc turned out to be wearing, so the last arc can be told
        /// apart from the first. Two arcs that resolve to the same picture and
        /// the same ground are one arc as far as a player is concerned, and that
        /// is invisible in either arc's own screenshot.
        /// </summary>
        private readonly Dictionary<string, int> _backdropSeen = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _groundSeen = new Dictionary<string, int>();

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

                ArcTheme theme = StoryTheme.ForArc(arc);
                if (theme == null)
                {
                    // Not a cosmetic miss: StoryBoardTheme.Apply returns early on
                    // a null theme, so every level in the arc keeps the kit's own
                    // look and nothing says so.
                    Finding("has no theme in Resources/Story/themes.json, so every " +
                            "board in it keeps the kit's own backdrop and black checker");
                    continue;
                }

                int level = FirstLevelOf(arc);
                if (level <= 0)
                {
                    Finding("no level in the plan belongs to it, so its theme is " +
                            "never exercised by anything");
                    continue;
                }

                int areaId = StoryLevelPlan.AreaOf(level);
                Say(string.Format("---- arc {0} ({1}) — level {2}, area {3} ----",
                    arc, theme.ThemeKey, level, areaId));

                // Leaving the last level drops the player on home, and home is
                // where the story's beats fire. Launch from a calm shell.
                yield return WaitForCalm(BeatTimeout);

                yield return Reach(level, areaId);

                if (LevelManager.THIS == null || LevelManager.THIS.gameStatus != GameState.Playing)
                {
                    Finding(string.Format(
                        "level {0} never became playable — state {1}. Nothing was drawn, " +
                        "so the theme could not be looked at.", level,
                        LevelManager.THIS == null
                            ? "no LevelManager"
                            : LevelManager.THIS.gameStatus.ToString()));
                    yield return Shot(arc, theme.ThemeKey);
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

                Verify(arc, theme, level);
                yield return MeasureFrame(theme);
                yield return Shot(arc, theme.ThemeKey);
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

            // The theme is applied on OnLevelLoaded and again on OnStartPlay, and
            // the board finishes falling in after both. This is a picture of a
            // board, so it waits for the board to be still.
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

        /// <summary>Everything about this arc's board that a check can settle.</summary>
        private void Verify(int arc, ArcTheme theme, int level)
        {
            // 1. The theme the board actually resolved, through the level rather
            //    than through the arc — that is the path the game uses.
            ArcTheme viaLevel = StoryTheme.ForLevel(level);
            if (viaLevel == null)
            {
                Finding(string.Format(
                    "level {0} resolves to no theme, so StoryBoardTheme.Apply returns " +
                    "before it dresses anything", level));
            }
            else if (viaLevel.Arc != arc)
            {
                Finding(string.Format(
                    "level {0} resolves to arc {1}'s theme ({2}), not its own",
                    level, viaLevel.Arc, viaLevel.ThemeKey));
            }

            // 2. What went behind the board, read off the Image rather than off
            //    this layer's own bookkeeping.
            string backdrop = "none";
            var background = FindFirstObjectByType<Background>(FindObjectsInactive.Include);
            Image image = background == null ? null : background.GetComponent<Image>();
            if (image == null || image.sprite == null)
            {
                Finding("nothing is drawn behind the board — no Background image");
            }
            else
            {
                backdrop = image.sprite.name;

                // The board wears the KIT's background — StoryBoardTheme.
                // UseStoryBackdrop is false. What is checked is therefore that
                // the kit's own rule was honoured, not that the story's room
                // art arrived; the story art is still checked everywhere it is
                // actually used, on the home screen and the area page.
                if (!StoryBoardTheme.UseStoryBackdrop)
                {
                    Sprite[] pictures = background.pictures;
                    if (pictures == null || pictures.Length == 0)
                    {
                        Finding("the kit's Background has no pictures, so the board " +
                                "is wearing whatever the scene last left on it");
                    }
                    else
                    {
                        int every = Mathf.Max(1, background.changeBackgoundEveryLevels);
                        int index = Mathf.Clamp(
                            (int)((float)level / every - 0.01f), 0, pictures.Length - 1);
                        if (image.sprite != pictures[index])
                        {
                            Finding(string.Format(
                                "the board is wearing '{0}' and not the kit's picture {1} " +
                                "for level {2}", backdrop, index, level));
                        }
                    }
                }
                else
                {
                    int areaId = StoryLevelPlan.AreaOf(level);
                    Sprite expected = StoryArt.AreaAtProgress(areaId) ?? StoryArt.Load(theme.BoardBackdrop);
                    if (expected == null)
                    {
                        Finding(string.Format(
                            "its room art and fallback are missing: Resources/Story/Art/{0}. The board " +
                            "is wearing the kit's own '{1}' instead.",
                            theme.BoardBackdrop, backdrop));
                    }
                    else if (image.sprite != expected)
                    {
                        Finding(string.Format(
                            "the board is wearing '{0}' and not area {1}'s current room '{2}'",
                            backdrop, areaId, expected.name));
                    }
                    else
                    {
                        ReportBackdropSharpness(image);
                    }
                }
            }



            if (StoryBoardTheme.LastBackdropWasKitDefault)
            {
                Finding("StoryBoardTheme fell back to the kit default: " +
                        StoryBoardTheme.LastBackdrop);
            }

            // 3. The ground under the cells. A theme that names a fill and a
            //    board that never got one look identical in the log and nothing
            //    like each other on screen.
            //
            //    Unless the story is not drawing a ground at all: with
            //    StoryBoardTheme.UseStoryCellFill off the cells are meant to be
            //    the kit's black checker, and a board with no story ground on
            //    it is the board working correctly.
            if (!StoryBoardTheme.UseStoryCellFill)
            {
                Say("cells keep the kit's own checker; the arc's ground is off.");
            }
            else if (!theme.HasBoardFill)
            {
                Finding("names no board ground, so its cells keep the kit's flat " +
                        "black checker whatever the arc is");
            }
            else if (StoryBoardTheme.LastFilledCells <= 0)
            {
                Finding("names a board ground but no cell carries it — the board was " +
                        "built after the theme was applied, or every cell is a hole");
            }
            else
            {
                Say(string.Format("ground {0} on {1} cell(s); backdrop '{2}'.",
                    Describe(StoryBoardTheme.LastFill), StoryBoardTheme.LastFilledCells,
                    backdrop));
            }

            // 4. Told apart from the arcs already seen. Two arcs wearing the same
            //    picture AND the same ground are one arc, and neither arc's own
            //    screenshot can show that.
            // With the cells on the kit's checker every arc's ground is the
            // same, so the backdrop is the only thing telling two arcs apart —
            // and it is the room being restored, which no two arcs share.
            int firstArc;
            if (_backdropSeen.TryGetValue(backdrop, out firstArc) && backdrop != "none")
            {
                string ground = StoryBoardTheme.UseStoryCellFill
                    ? Describe(StoryBoardTheme.LastFill)
                    : "the kit's checker";
                int groundArc;
                if (_groundSeen.TryGetValue(ground, out groundArc) && groundArc == firstArc)
                {
                    Finding(string.Format(
                        "is the same board as arc {0}: the same backdrop '{1}' and the " +
                        "same ground {2}", firstArc, backdrop, ground));
                }
                else
                {
                    Say(string.Format("shares backdrop '{0}' with arc {1}, separated by " +
                                      "its ground.", backdrop, firstArc));
                }
            }
            else if (backdrop != "none")
            {
                _backdropSeen[backdrop] = arc;
            }

            string key = Describe(StoryBoardTheme.LastFill);
            if (!_groundSeen.ContainsKey(key)) _groundSeen[key] = arc;
        }

        /// <summary>
        /// How many of the backdrop's own pixels reach each pixel it is drawn
        /// into. Below 1 the picture is being magnified, and a magnified
        /// painting is the blur behind the board that no check had ever
        /// measured — it looks like an out-of-focus photograph of the place
        /// rather than the place.
        ///
        /// One is the honest floor: at exactly one source pixel per screen pixel
        /// the picture is as sharp as it was painted, and anything above is a
        /// downscale, which is always fine.
        /// </summary>
        private const float MinBackdropPixelsPerScreenPixel = 1f;

        private void ReportBackdropSharpness(Image image)
        {
            Sprite sprite = image.sprite;
            if (sprite == null || sprite.texture == null) return;

            var rect = image.rectTransform;
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);

            Canvas canvas = image.canvas;
            Camera camera = canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.worldCamera;

            Vector2 low = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            Vector2 high = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
            float drawnWidth = Mathf.Abs(high.x - low.x);
            float drawnHeight = Mathf.Abs(high.y - low.y);
            if (drawnWidth <= 0f || drawnHeight <= 0f) return;

            // Against the window's own pixels, because that is what
            // StoryWindow.Capture now writes.
            float perPixel = Mathf.Min(sprite.rect.width / drawnWidth,
                                       sprite.rect.height / drawnHeight);

            Say(string.Format(
                "backdrop {0} is {1:0}x{2:0} in a {3}x{4} texture, drawn into {5:0}x{6:0} " +
                "shot pixels — {7:0.00} source pixel(s) each.",
                sprite.name, sprite.rect.width, sprite.rect.height,
                sprite.texture.width, sprite.texture.height,
                drawnWidth, drawnHeight, perPixel));

            if (perPixel < MinBackdropPixelsPerScreenPixel)
            {
                Finding(string.Format(
                    "the room behind the board is magnified {0:0.0}x: {1} carries only " +
                    "{2:0.00} of its own pixels per pixel it is drawn into, which is why it " +
                    "reads as an out-of-focus photograph rather than a place. Its texture is " +
                    "{3}x{4}.",
                    1f / Mathf.Max(perPixel, 0.0001f), sprite.name, perPixel,
                    sprite.texture.width, sprite.texture.height));
            }
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
        /// ground the theme asked for.
        ///
        /// The frame is the board's ground over part of the screen and the arc's
        /// backdrop over the rest, so it is always darker than the ground alone.
        /// Take the worst honest case — a board covering only a third of the
        /// screen, on the darkest backdrop the game ships (luminance 50) under
        /// the darkest ground (170): 0.33*170 + 0.67*50 is 90, which is 0.53 of
        /// that ground. So anything correct lands well above a half, and a third
        /// is a floor no correctly drawn arc can reach while still catching a
        /// scrim laid over the whole screen.
        /// </summary>
        private const float MinFrameBrightnessFraction = 0.35f;

        /// <summary>
        /// Reads the frame the player would see and compares it with the ground
        /// the theme asked for.
        ///
        /// This is the one check here that cannot be fooled by the scene graph
        /// being correct. Everything else asks an object what it is set to; this
        /// asks the screen what it looks like.
        /// </summary>
        private IEnumerator MeasureFrame(ArcTheme theme)
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
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
            float ground = 0.2126f * theme.BoardFill.r + 0.7152f * theme.BoardFill.g +
                           0.0722f * theme.BoardFill.b;

            Say(string.Format("frame: mean luminance {0:0}, brightest {1:0}, " +
                              "against a ground of {2:0}.",
                mean * 255f, brightest * 255f, ground * 255f));

            if (ground <= 0f) yield break;

            if (mean < ground * MinFrameBrightnessFraction)
            {
                // The mean and not the brightest pixel: one stray highlight is
                // enough to make a screen that is black to a player look fine to
                // a max. Arc 6 measured a brightest of 255 while its mean sat at
                // 40 against a ground of 170, and the picture is unreadable.
                Finding(string.Format(
                    "the frame is dark: mean luminance {0:0} against a board ground of " +
                    "{1:0} — the theme reached the board and something is drawn over it. {2}",
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
            // WaitForEndOfFrame never returns under -nographics, and there is
            // nothing to photograph there either.
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
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
            report.AppendLine("King Leo — board theme, one level per arc");
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
