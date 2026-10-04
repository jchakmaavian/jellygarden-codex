#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !ROYAL_DISABLE_QA
using System;
using System.Collections;
using System.Collections.Generic;
using JellyGarden.Scripts;
using RoyalThrone.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RoyalThrone.Testing
{
    /// <summary>
    /// The Mod Menu. It owns no game logic: every action routes to the existing
    /// <see cref="RoyalQaService"/> commands, so the tools stay in one place and this file is only
    /// navigation, layout and a few adapters (speed, pause, frame step) the service did not expose.
    ///
    /// Progressive disclosure: Home -> Category -> Controls. Nothing is built until a screen is
    /// opened, so the old wall of seventy buttons never appears at once.
    /// </summary>
    public sealed class RoyalModMenu : MonoBehaviour
    {
        public const string EnabledKey = "RoyalThrone.ModMenu";
        public static bool ModsEnabled
        {
            get => RoyalSave.GetInt(EnabledKey, 0) == 1;
            set { RoyalSave.SetInt(EnabledKey, value ? 1 : 0); }
        }

        enum Screen { Home, Game, Board, Boosters, Lab, Progress, Debug, TestMode }

        static readonly Color Ink = new Color(.035f, .05f, .085f, 1f);
        static readonly Color Card = new Color(.075f, .10f, .16f, 1f);
        static readonly Color CardHi = new Color(.10f, .15f, .24f, 1f);
        static readonly Color Accent = new Color(.24f, .62f, 1f);
        static readonly Color Line = new Color(.22f, .30f, .44f, .75f);
        static readonly Color Dim = new Color(.58f, .66f, .78f);

        RoyalQaService service;
        Font font;
        RectTransform root, panel, body;
        Text title, crumb, modified, statusLine;
        Screen screen = Screen.Home;
        string detail;                 // selected booster / lab object / test subject
        string stage = "Reveal";
        int changes;
        bool shown, stepping;
        float restoreSpeed = 1f, nextAttach;
        readonly HashSet<int> attached = new HashSet<int>();

        /// <summary>Stages the lab offers per object. Only what each booster actually plays.</summary>
        static readonly Dictionary<string, string[]> Stages = new Dictionary<string, string[]>
        {
            { "TNT",        new[] { "Reveal", "Idle", "Activation", "Explosion", "Full Sequence" } },
            { "Rocket H",   new[] { "Reveal", "Idle", "Activation", "Full Sequence" } },
            { "Rocket V",   new[] { "Reveal", "Idle", "Activation", "Full Sequence" } },
            { "Propeller",  new[] { "Reveal", "Idle", "Activation", "Impact", "Full Sequence" } },
            { "Color ball", new[] { "Reveal", "Idle", "Activation", "Impact", "Full Sequence" } },
            { "Gems",       new[] { "Idle", "Swap", "Match", "Cascade", "Fall" } },
        };

        /// <summary>Spawn command per booster, so the lab and the booster page share one path.</summary>
        static readonly Dictionary<string, string> SpawnOf = new Dictionary<string, string>
        {
            { "TNT", "Spawn TNT" }, { "Rocket H", "Spawn rocket H" }, { "Rocket V", "Spawn rocket V" },
            { "Propeller", "Spawn propeller" }, { "Color ball", "Spawn color ball" },
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            SceneManager.sceneLoaded -= OnScene;
            SceneManager.sceneLoaded += OnScene;
            OnScene(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        static void OnScene(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "game" || FindAnyObjectByType<RoyalModMenu>()) return;
            // Set before the components exist: RoyalQaMenu.Awake runs first and would otherwise build
            // its own screen before the Mod Menu could suppress it.
            RoyalQaMenu.SuppressUi = true;
            new GameObject("Mod Menu", typeof(RoyalQaService), typeof(RoyalQaMenu), typeof(RoyalModMenu));
        }

        void Awake()
        {
            service = GetComponent<RoyalQaService>();
            // The legacy screen stays on the object only because the service calls its coin reward
            // coroutine; its own UI is suppressed so there is one menu, not two.
            RoyalQaMenu.SuppressUi = true;
            // The menu is opened from the MOD entry, never on its own when the scene loads.
            service.Visible = false;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            root = Rect("Mod menu canvas", transform);
            var canvas = root.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1001;
            var scaler = root.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720, 1280);
            scaler.matchWidthOrHeight = .5f;
            root.gameObject.AddComponent<GraphicRaycaster>();

            var shade = Rect("Shade", root);
            Bounds(shade, 0, 0, 1, 1);
            var shadeImage = shade.gameObject.AddComponent<Image>();
            shadeImage.color = new Color(0, 0, 0, .55f);

            panel = Rect("Panel", root);
            // Fullscreen page, not a dialog inside Settings.
            Bounds(panel, 0f, 0f, 1f, 1f);
            panel.gameObject.AddComponent<Image>().color = Ink;

            // Header:  <   MOD MENU   x
            var back = Chip(panel, "‹", () => GoBack());
            Bounds((RectTransform)back.transform, .045f, .922f, .145f, .966f);
            title = Label(panel, "MOD MENU", 30, TextAnchor.MiddleCenter);
            Bounds(title.rectTransform, .16f, .922f, .84f, .966f);
            var close = Chip(panel, "×", () => Close());
            Bounds((RectTransform)close.transform, .855f, .922f, .955f, .966f);

            crumb = Label(panel, "Game Tools & Testing", 17, TextAnchor.MiddleLeft);
            crumb.color = Dim;
            Bounds(crumb.rectTransform, .055f, .885f, .60f, .917f);
            modified = Label(panel, "", 16, TextAnchor.MiddleRight);
            modified.color = Accent;
            Bounds(modified.rectTransform, .60f, .885f, .945f, .917f);

            body = Rect("Body", panel);
            Bounds(body, .055f, .042f, .945f, .872f);

            statusLine = Label(panel, "", 15, TextAnchor.MiddleLeft);
            statusLine.color = Dim;
            Bounds(statusLine.rectTransform, .055f, .012f, .945f, .036f);

            panel.gameObject.SetActive(false);
            shade.gameObject.SetActive(false);
            Build();
        }

        // ---------------------------------------------------------------- navigation

        void Open(Screen next, string which = null)
        {
            screen = next;
            if (which != null) detail = which;
            Build();
        }

        void GoBack()
        {
            if (screen == Screen.Home) { Close(); return; }
            if (screen != Screen.Home && detail != null &&
                (screen == Screen.Boosters || screen == Screen.Lab || screen == Screen.TestMode))
            { detail = null; Build(); return; }
            Open(Screen.Home);
        }

        void Close()
        {
            service.Visible = false;
            // Leaving the menu must not leave the game running at a test speed.
            service.Speed = 1f;
            Time.timeScale = 1f;
            restoreSpeed = 1f;
            stepping = false;
        }

        void Note(string text) { service.Status = text; }

        /// <summary>
        /// Every service command refuses to run outside the isolated test profile, so the menu opens
        /// one on demand. Normal save data is restored by End test profile / Reset Changes.
        /// </summary>
        void Run(string command)
        {
            if (!RoyalSave.Testing) service.StartSession();
            changes++;
            service.Request(command);
            Note(command);
        }

        // ---------------------------------------------------------------- build

        void Build()
        {
            for (int i = body.childCount - 1; i >= 0; i--) Destroy(body.GetChild(i).gameObject);
            switch (screen)
            {
                case Screen.Home: BuildHome(); break;
                case Screen.Game: BuildGame(); break;
                case Screen.Board: BuildBoard(); break;
                case Screen.Boosters: BuildBoosters(); break;
                case Screen.Lab: BuildLab(); break;
                case Screen.Progress: BuildProgress(); break;
                case Screen.Debug: BuildDebug(); break;
                case Screen.TestMode: BuildTestMode(); break;
            }
            title.text = screen == Screen.Home ? "MOD MENU" : Title(screen);
            crumb.text = screen == Screen.Home ? "Game Tools & Testing"
                : detail != null ? Pretty(screen) + " › " + detail : Pretty(screen);
        }

        static string Title(Screen s) => Pretty(s).ToUpperInvariant();
        static string Pretty(Screen s) => s == Screen.Lab ? "Animation & VFX" : s == Screen.TestMode ? "Test Mode" : s.ToString();

        void BuildHome()
        {
            // Quick Test: the fastest path to one animation.
            var quick = Panel(body, "Quick", .78f, .995f);
            Label(quick, "QUICK TEST", 16, TextAnchor.UpperLeft, .04f, .70f, .6f, .96f).color = Accent;
            Picker(quick, "Object", .04f, .34f, .48f, .66f, BoosterNames(), () => detail ?? "TNT", v => { detail = v; Build(); });
            Picker(quick, "Stage", .52f, .34f, .96f, .66f, StagesFor(detail ?? "TNT"), () => stage, v => { stage = v; Build(); });
            Label(quick, "Cell  C" + service.Column + " R" + service.Row + "     Speed " + service.Speed + "×", 15,
                TextAnchor.MiddleLeft, .04f, .06f, .60f, .30f).color = Dim;
            var run = Solid(quick, "RUN", () => PlayStage(detail ?? "TNT", stage));
            Bounds((RectTransform)run.transform, .62f, .06f, .96f, .30f);

            var test = Solid(body, "Test Mode", () => Open(Screen.TestMode, null));
            Bounds((RectTransform)test.transform, 0f, .695f, .485f, .765f);
            var reset = Chip(body, "Reset Changes", () => { changes = 0; Run("Reset test data"); });
            Bounds((RectTransform)reset.transform, .515f, .695f, 1f, .765f);

            // The test profile is disposable: everything the menu changes lives inside it.
            var session = Toggle(body, RoyalSave.Testing ? "Test profile: ON" : "Test profile: OFF", RoyalSave.Testing, () =>
            {
                if (RoyalSave.Testing) StartCoroutine(EndProfile());
                else { service.StartSession(); Note("Isolated test profile started."); Build(); }
            });
            Bounds((RectTransform)session.transform, 0f, .612f, 1f, .682f);

            string[] names = { "Game", "Board", "Boosters", "Animation & VFX", "Progress", "Debug" };
            string[] subs = { "Level, moves, speed", "Cells, matches, refill", "Spawn, activate, combo",
                              "Lab, stages, VFX, SFX", "Coins, lives, stars", "Overlays, state" };
            Screen[] to = { Screen.Game, Screen.Board, Screen.Boosters, Screen.Lab, Screen.Progress, Screen.Debug };
            for (int i = 0; i < 6; i++)
            {
                int k = i;
                float y1 = .60f - i * .098f, y0 = y1 - .085f;
                var card = Chip(body, "", () => Open(to[k], null));
                Bounds((RectTransform)card.transform, 0f, y0, 1f, y1);
                Label(card.transform, names[i], 21, TextAnchor.MiddleLeft, .04f, .42f, .8f, .95f);
                Label(card.transform, subs[i], 14, TextAnchor.MiddleLeft, .04f, .08f, .8f, .45f).color = Dim;
                Label(card.transform, "›", 24, TextAnchor.MiddleRight, .8f, .1f, .96f, .9f).color = Dim;
            }
        }

        void BuildGame()
        {
            var speed = Panel(body, "Speed", .84f, .995f);
            Label(speed, "GAME SPEED", 15, TextAnchor.UpperLeft, .04f, .62f, .6f, .95f).color = Accent;
            float[] speeds = { .25f, .5f, 1f, 1.5f, 2f };
            for (int i = 0; i < speeds.Length; i++)
            {
                float s = speeds[i];
                var b = Toggle(speed, s + "×", Mathf.Approximately(service.Speed, s), () =>
                { service.Speed = s; restoreSpeed = s; Time.timeScale = s; changes++; Note("Speed " + s + "×"); Build(); });
                Bounds((RectTransform)b.transform, .04f + i * .186f, .10f, .21f + i * .186f, .55f);
            }

            var step = Panel(body, "Step", .70f, .825f);
            Label(step, "PAUSE & FRAME STEP", 15, TextAnchor.UpperLeft, .04f, .60f, .7f, .95f).color = Accent;
            var pause = Toggle(step, Paused ? "Resume" : "Pause", Paused, () => { SetPaused(!Paused); Build(); });
            Bounds((RectTransform)pause.transform, .04f, .08f, .34f, .55f);
            var s1 = Chip(step, "Step ‹", () => StartCoroutine(StepFrame()));
            Bounds((RectTransform)s1.transform, .37f, .08f, .65f, .55f);
            var s2 = Chip(step, "Step ›", () => StartCoroutine(StepFrame()));
            Bounds((RectTransform)s2.transform, .68f, .08f, .96f, .55f);

            string[] cmds = { "Open level", "Restart level", "Win instantly", "Lose instantly",
                              "Set moves", "Add moves", "Remove moves", "Complete targets",
                              "Set lives", "Refill lives", "Empty lives", "Practice board" };
            List(cmds, .09f, .675f);
            Stepper(body, "Level", .0f, .015f, .48f, .075f, () => service.Level, v => service.Level = Mathf.Max(1, v));
            Stepper(body, "Amount", .52f, .015f, 1f, .075f, () => service.Amount, v => service.Amount = Mathf.Max(0, v));
        }

        void BuildBoard()
        {
            var sel = Panel(body, "Cell", .80f, .995f);
            Label(sel, "SELECTED CELL", 15, TextAnchor.UpperLeft, .04f, .64f, .6f, .95f).color = Accent;
            Label(sel, "Column " + service.Column + "     Row " + service.Row, 22, TextAnchor.MiddleLeft, .04f, .30f, .62f, .62f);
            var prev = Chip(sel, "‹ Prev", () => { MoveCell(-1); Build(); });
            Bounds((RectTransform)prev.transform, .64f, .34f, .79f, .62f);
            var next = Chip(sel, "Next ›", () => { MoveCell(1); Build(); });
            Bounds((RectTransform)next.transform, .81f, .34f, .96f, .62f);
            Stepper(sel, "Column", .04f, .05f, .48f, .28f, () => service.Column, v => { service.Column = Mathf.Max(0, v); Build(); });
            Stepper(sel, "Row", .52f, .05f, .96f, .28f, () => service.Row, v => { service.Row = Mathf.Max(0, v); Build(); });

            string[] cmds = { "Spawn selected gem", "Create special", "Clear board", "Shuffle board",
                              "Spawn blocker", "Match 3", "Match 4", "Match 5", "Cascade" };
            List(cmds, .09f, .78f);
            Stepper(body, "Gem 0–5", .0f, .015f, 1f, .075f, () => service.Gem, v => service.Gem = Mathf.Clamp(v, 0, 5));
        }

        void BuildBoosters()
        {
            if (detail == null)
            {
                Label(body, "Select a booster", 16, TextAnchor.UpperLeft, 0f, .93f, .7f, .99f).color = Dim;
                var names = BoosterNames();
                for (int i = 0; i < names.Length; i++)
                {
                    string n = names[i];
                    var b = Chip(body, n, () => Open(Screen.Boosters, n));
                    Bounds((RectTransform)b.transform, 0f, .845f - i * .085f, 1f, .915f - i * .085f);
                }
                var combo = Solid(body, "Combo Test", () => Open(Screen.Boosters, "Combo"));
                Bounds((RectTransform)combo.transform, 0f, .33f, 1f, .40f);
                return;
            }

            if (detail == "Combo")
            {
                Label(body, "Pick two powers, then combine", 15, TextAnchor.UpperLeft, 0f, .93f, 1f, .99f).color = Dim;
                var a = Chip(body, "A: " + RoyalQaService.PowerNames[service.PowerA],
                    () => { service.PowerA = (service.PowerA + 1) % 5; Build(); });
                Bounds((RectTransform)a.transform, 0f, .845f, .48f, .915f);
                var b2 = Chip(body, "B: " + RoyalQaService.PowerNames[service.PowerB],
                    () => { service.PowerB = (service.PowerB + 1) % 5; Build(); });
                Bounds((RectTransform)b2.transform, .52f, .845f, 1f, .915f);
                var go = Solid(body, "Combine selected", () => Run("Combine selected"));
                Bounds((RectTransform)go.transform, 0f, .755f, 1f, .825f);
                Label(body, "Combinations come from the live combo table, so only\nwhat the director implements will resolve.", 14,
                    TextAnchor.UpperLeft, 0f, .66f, 1f, .74f).color = Dim;
                return;
            }

            string[] actions = { "Spawn", "Reveal", "Idle", "Activate", "Full Sequence" };
            for (int i = 0; i < actions.Length; i++)
            {
                string act = actions[i];
                var b = Chip(body, act, () => PlayStage(detail, act == "Spawn" ? "Spawn" : act == "Activate" ? "Activation" : act));
                Bounds((RectTransform)b.transform, 0f, .845f - i * .085f, 1f, .915f - i * .085f);
            }
            Label(body, "Acts on the selected cell  (C" + service.Column + " R" + service.Row + ")", 14,
                TextAnchor.UpperLeft, 0f, .38f, 1f, .44f).color = Dim;
            var stock = Chip(body, "Set booster stock", () => Run("Set booster stock"));
            Bounds((RectTransform)stock.transform, 0f, .29f, 1f, .36f);
        }

        void BuildLab()
        {
            string obj = detail ?? "TNT";
            var head = Panel(body, "Pick", .80f, .995f);
            Label(head, "ANIMATION & VFX LAB", 15, TextAnchor.UpperLeft, .04f, .66f, .7f, .96f).color = Accent;
            Picker(head, "Object", .04f, .34f, .48f, .64f, LabObjects(), () => obj, v => { detail = v; Build(); });
            Picker(head, "Stage", .52f, .34f, .96f, .64f, StagesFor(obj), () => stage, v => { stage = v; Build(); });
            Label(head, "State " + stage + "     Speed " + service.Speed + "×     Loop " + (service.Loop ? "ON" : "OFF"), 14,
                TextAnchor.MiddleLeft, .04f, .06f, .96f, .30f).color = Dim;

            var play = Solid(body, "Play", () => PlayStage(obj, stage));
            Bounds((RectTransform)play.transform, 0f, .705f, .32f, .775f);
            var pause = Chip(body, Paused ? "Resume" : "Pause", () => { SetPaused(!Paused); Build(); });
            Bounds((RectTransform)pause.transform, .34f, .705f, .66f, .775f);
            var restart = Chip(body, "Restart", () => PlayStage(obj, stage));
            Bounds((RectTransform)restart.transform, .68f, .705f, 1f, .775f);

            var prev = Chip(body, "‹ Stage", () => { CycleStage(obj, -1); Build(); });
            Bounds((RectTransform)prev.transform, 0f, .625f, .32f, .695f);
            var st1 = Chip(body, "Step ‹", () => StartCoroutine(StepFrame()));
            Bounds((RectTransform)st1.transform, .34f, .625f, .66f, .695f);
            var nxt = Chip(body, "Stage ›", () => { CycleStage(obj, 1); Build(); });
            Bounds((RectTransform)nxt.transform, .68f, .625f, 1f, .695f);

            float[] speeds = { .25f, .5f, 1f, 1.5f };
            for (int i = 0; i < speeds.Length; i++)
            {
                float s = speeds[i];
                var b = Toggle(body, s + "×", Mathf.Approximately(service.Speed, s), () =>
                { service.Speed = s; restoreSpeed = s; Time.timeScale = s; Build(); });
                Bounds((RectTransform)b.transform, i * .255f, .545f, .23f + i * .255f, .61f);
            }
            var loop = Toggle(body, "Loop " + (service.Loop ? "ON" : "OFF"), service.Loop,
                () => { service.Loop = !service.Loop; Build(); });
            Bounds((RectTransform)loop.transform, 0f, .465f, 1f, .53f);

            // Channel isolation: animation / VFX / SFX can each be run alone.
            Label(body, "CHANNELS", 15, TextAnchor.UpperLeft, 0f, .415f, .6f, .46f).color = Accent;
            var anim = Toggle(body, "Animation", RoyalModChannels.Animation, () => { RoyalModChannels.Animation = !RoyalModChannels.Animation; Build(); });
            Bounds((RectTransform)anim.transform, 0f, .34f, .32f, .405f);
            var vfx = Toggle(body, "VFX", RoyalModChannels.Vfx, () => { RoyalModChannels.Vfx = !RoyalModChannels.Vfx; Build(); });
            Bounds((RectTransform)vfx.transform, .34f, .34f, .66f, .405f);
            var sfx = Toggle(body, "SFX", RoyalModChannels.Sfx, () => { RoyalModChannels.Sfx = !RoyalModChannels.Sfx; RoyalSave.SetInt("Sound", RoyalModChannels.Sfx ? 1 : 0); Build(); });
            Bounds((RectTransform)sfx.transform, .68f, .34f, 1f, .405f);

            string[] vfxCmds = VfxFor(obj);
            for (int i = 0; i < vfxCmds.Length && i < 4; i++)
            {
                string c = vfxCmds[i];
                var b = Chip(body, c, () => Run(c));
                Bounds((RectTransform)b.transform, 0f, .255f - i * .075f, 1f, .32f - i * .075f);
            }
        }

        void BuildProgress()
        {
            Stepper(body, "Amount applied by the buttons below", 0f, .90f, 1f, .97f, () => service.Amount, v => service.Amount = Mathf.Max(0, v));
            string[] cmds = { "Add coins", "Remove coins", "Set coins", "Add gems", "Remove gems", "Set gems",
                              "Add stars", "Remove stars", "Set stars", "Set lives", "Refill lives", "Refill coins" };
            List(cmds, .09f, .875f);
            Stepper(body, "Level", 0f, .015f, 1f, .075f, () => service.Level, v => service.Level = Mathf.Max(1, v));
        }

        void BuildDebug()
        {
            Label(body, "State", 15, TextAnchor.UpperLeft, 0f, .93f, 1f, .99f).color = Accent;
            var lm = LevelManager.THIS;
            string info = "FPS " + Mathf.RoundToInt(1f / Mathf.Max(.0001f, Time.smoothDeltaTime))
                + "     Status " + (lm ? lm.gameStatus.ToString() : "—")
                + "\nCell C" + service.Column + " R" + service.Row
                + "     Booster " + RoyalQaService.PowerNames[service.PowerA]
                + "\nStage " + stage + "     Speed " + service.Speed + "×"
                + "\nPass " + service.Completed + "     Fail " + service.Failed;
            Label(body, info, 15, TextAnchor.UpperLeft, 0f, .76f, 1f, .925f).color = Dim;

            string[] cmds = { "Grid / cells", "Gem IDs / types", "Valid moves", "Detected matches",
                              "Hint candidate", "FPS / particles" };
            List(cmds, .075f, .74f);

            Label(body, "ADVANCED", 15, TextAnchor.UpperLeft, 0f, .25f, 1f, .30f).color = new Color(1, .72f, .35f);
            var wipe = Chip(body, "Reset test data", () =>
            {
                if (detail == "confirm") { detail = null; changes = 0; Run("Reset test data"); }
                else { detail = "confirm"; Note("Tap again to confirm reset."); }
                Build();
            });
            Bounds((RectTransform)wipe.transform, 0f, .17f, 1f, .24f);
            if (detail == "confirm")
                Label(body, "Tap again to confirm — clears the test profile.", 14, TextAnchor.UpperLeft, 0f, .12f, 1f, .165f)
                    .color = new Color(1, .72f, .35f);
        }

        void BuildTestMode()
        {
            if (detail == null)
            {
                Label(body, "Pick a system to run end to end", 15, TextAnchor.UpperLeft, 0f, .93f, 1f, .99f).color = Dim;
                string[] subjects = { "TNT", "Rocket H", "Rocket V", "Propeller", "Color ball", "Gems" };
                for (int i = 0; i < subjects.Length; i++)
                {
                    string s = subjects[i];
                    var b = Chip(body, s, () => Open(Screen.TestMode, s));
                    Bounds((RectTransform)b.transform, 0f, .845f - i * .085f, 1f, .915f - i * .085f);
                }
                Label(body, "Test Mode runs on the disposable practice board, so\nnormal progress is never touched.", 14,
                    TextAnchor.UpperLeft, 0f, .26f, 1f, .34f).color = Dim;
                var practice = Solid(body, "Start practice board", () => Run("Practice board"));
                Bounds((RectTransform)practice.transform, 0f, .17f, 1f, .24f);
                return;
            }

            var stages = StagesFor(detail);
            Label(body, detail, 22, TextAnchor.UpperLeft, 0f, .92f, 1f, .99f);
            for (int i = 0; i < stages.Length; i++)
            {
                string s = stages[i];
                var b = Toggle(body, s, stage == s, () => { stage = s; Build(); });
                Bounds((RectTransform)b.transform, 0f, .83f - i * .075f, 1f, .895f - i * .075f);
            }
            var play = Solid(body, "PLAY TEST", () => PlayStage(detail, stage));
            Bounds((RectTransform)play.transform, 0f, .40f, 1f, .48f);

            float[] speeds = { .25f, .5f, 1f };
            for (int i = 0; i < speeds.Length; i++)
            {
                float s = speeds[i];
                var b = Toggle(body, s + "×", Mathf.Approximately(service.Speed, s), () =>
                { service.Speed = s; restoreSpeed = s; Time.timeScale = s; Build(); });
                Bounds((RectTransform)b.transform, i * .345f, .315f, .31f + i * .345f, .38f);
            }
            var loop = Toggle(body, "Loop " + (service.Loop ? "ON" : "OFF"), service.Loop, () => { service.Loop = !service.Loop; Build(); });
            Bounds((RectTransform)loop.transform, 0f, .235f, .48f, .30f);
            var stepb = Chip(body, "Frame Step", () => StartCoroutine(StepFrame()));
            Bounds((RectTransform)stepb.transform, .52f, .235f, 1f, .30f);

            var anim = Toggle(body, "Animation", RoyalModChannels.Animation, () => { RoyalModChannels.Animation = !RoyalModChannels.Animation; Build(); });
            Bounds((RectTransform)anim.transform, 0f, .155f, .32f, .22f);
            var vfx = Toggle(body, "VFX", RoyalModChannels.Vfx, () => { RoyalModChannels.Vfx = !RoyalModChannels.Vfx; Build(); });
            Bounds((RectTransform)vfx.transform, .34f, .155f, .66f, .22f);
            var sfx = Toggle(body, "SFX", RoyalModChannels.Sfx, () => { RoyalModChannels.Sfx = !RoyalModChannels.Sfx; RoyalSave.SetInt("Sound", RoyalModChannels.Sfx ? 1 : 0); Build(); });
            Bounds((RectTransform)sfx.transform, .68f, .155f, 1f, .22f);
        }

        // ---------------------------------------------------------------- actions

        /// <summary>Maps an object and a stage onto the service commands that already do the work.</summary>
        void PlayStage(string obj, string stageName)
        {
            if (obj == "Gems")
            {
                switch (stageName)
                {
                    case "Swap": Run("Swap"); return;
                    case "Match": Run("Match animation"); return;
                    case "Cascade": Run("Cascade animation"); return;
                    case "Fall": Run("Fall"); return;
                    default: Run("Gem idle"); return;
                }
            }
            if (stageName == "Spawn" || stageName == "Reveal")
            {
                if (SpawnOf.TryGetValue(obj, out var spawn)) Run(spawn);
                return;
            }
            if (stageName == "Idle") { if (SpawnOf.TryGetValue(obj, out var s2)) Run(s2); return; }
            if (stageName == "Activation" || stageName == "Explosion" || stageName == "Impact")
            {
                if (obj == "TNT" && stageName == "Explosion") { Run("TNT explosion"); return; }
                Run("Activate selected");
                return;
            }
            if (stageName == "Full Sequence")
            {
                if (SpawnOf.TryGetValue(obj, out var s3)) { Run(s3); StartCoroutine(Then("Activate selected")); }
                return;
            }
            Run("Booster activation");
        }

        IEnumerator EndProfile()
        {
            yield return service.EndSession();
            changes = 0;
            Note("Normal player data restored.");
            Build();
        }

        IEnumerator Then(string command)
        {
            while (service.Busy) yield return null;
            yield return new WaitForSecondsRealtime(.35f);
            Run(command);
        }

        /// <summary>
        /// Pause and step run through the service speed, not Time.timeScale directly: the service
        /// reapplies Time.timeScale = Speed every frame and would otherwise undo them immediately.
        /// </summary>
        bool Paused => service.Speed <= 0f;
        void SetPaused(bool on)
        {
            float v = on ? 0f : (restoreSpeed <= 0f ? 1f : restoreSpeed);
            // Both, so pause holds whether or not a test profile is active: the service only
            // reapplies Time.timeScale = Speed while RoyalSave.Testing is on.
            service.Speed = v;
            Time.timeScale = v;
        }

        /// <summary>One rendered frame while paused, for frame-by-frame inspection.</summary>
        IEnumerator StepFrame()
        {
            if (stepping) yield break;
            stepping = true;
            float keep = restoreSpeed <= 0f ? 1f : restoreSpeed;
            service.Speed = keep; Time.timeScale = keep;
            yield return new WaitForEndOfFrame();
            yield return null;
            service.Speed = 0f; Time.timeScale = 0f;
            stepping = false;
            Note("Frame step");
            Build();
        }

        void MoveCell(int direction)
        {
            var lm = LevelManager.THIS;
            int cols = lm ? Mathf.Max(1, lm.maxCols) : 9;
            int index = service.Row * cols + service.Column + direction;
            index = Mathf.Max(0, index);
            service.Column = index % cols;
            service.Row = index / cols;
        }

        void CycleStage(string obj, int direction)
        {
            var list = StagesFor(obj);
            int i = Array.IndexOf(list, stage);
            if (i < 0) i = 0;
            stage = list[((i + direction) % list.Length + list.Length) % list.Length];
        }

        static string[] BoosterNames() => RoyalQaService.PowerNames;
        static string[] LabObjects()
        {
            var list = new List<string>(RoyalQaService.PowerNames) { "Gems" };
            return list.ToArray();
        }
        static string[] StagesFor(string obj) => Stages.TryGetValue(obj, out var s) ? s : Stages["TNT"];

        /// <summary>Only the VFX commands the service actually implements for this object.</summary>
        static string[] VfxFor(string obj)
        {
            switch (obj)
            {
                case "TNT": return new[] { "TNT explosion", "Break particles", "Falling fragments" };
                case "Rocket H":
                case "Rocket V": return new[] { "Rocket trails", "Break particles" };
                case "Propeller": return new[] { "Propeller effect", "Break particles" };
                case "Color ball": return new[] { "Color-ball effect", "Break particles" };
                default: return new[] { "VFX Match 3", "VFX Match 4", "VFX Match 5", "Hint aura" };
            }
        }

        void List(string[] commands, float rowHeight, float top)
        {
            for (int i = 0; i < commands.Length; i++)
            {
                string c = commands[i];
                var b = Chip(body, c, () => Run(c));
                float y1 = top - i * rowHeight;
                Bounds((RectTransform)b.transform, 0f, y1 - rowHeight + .012f, 1f, y1);
            }
        }

        // ---------------------------------------------------------------- widgets

        RectTransform Panel(Transform parent, string name, float y0, float y1)
        {
            var r = Rect(name, parent);
            Bounds(r, 0f, y0, 1f, y1);
            var img = r.gameObject.AddComponent<Image>();
            img.color = Card;
            return r;
        }

        void Picker(Transform parent, string label, float x0, float y0, float x1, float y1,
                    string[] options, Func<string> get, Action<string> set)
        {
            var b = Chip(parent, "", () =>
            {
                int i = Array.IndexOf(options, get());
                set(options[((i < 0 ? 0 : i) + 1) % options.Length]);
            });
            Bounds((RectTransform)b.transform, x0, y0, x1, y1);
            Label(b.transform, label, 13, TextAnchor.UpperLeft, .06f, .52f, .96f, .95f).color = Dim;
            Label(b.transform, get(), 18, TextAnchor.LowerLeft, .06f, .06f, .96f, .55f);
        }

        void Stepper(Transform parent, string label, float x0, float y0, float x1, float y1, Func<int> get, Action<int> set)
        {
            var r = Rect(label, parent);
            Bounds(r, x0, y0, x1, y1);
            r.gameObject.AddComponent<Image>().color = Card;
            Label(r, label, 13, TextAnchor.UpperLeft, .04f, .5f, .7f, .95f).color = Dim;
            var value = Label(r, get().ToString(), 19, TextAnchor.MiddleCenter, .30f, .05f, .70f, .52f);
            var minus = Chip(r, "–", () => { set(get() - 1); value.text = get().ToString(); changes++; });
            Bounds((RectTransform)minus.transform, .04f, .08f, .26f, .50f);
            var plus = Chip(r, "+", () => { set(get() + 1); value.text = get().ToString(); changes++; });
            Bounds((RectTransform)plus.transform, .74f, .08f, .96f, .50f);
        }

        RectTransform Rect(string name, Transform parent)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(parent, false);
            return r;
        }

        Text Label(Transform parent, string text, int size, TextAnchor anchor)
        {
            var r = Rect("L", parent);
            var t = r.gameObject.AddComponent<Text>();
            t.font = font; t.text = text; t.fontSize = size; t.color = Color.white;
            t.alignment = anchor; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        Text Label(Transform parent, string text, int size, TextAnchor anchor, float x0, float y0, float x1, float y1)
        {
            var t = Label(parent, text, size, anchor);
            Bounds(t.rectTransform, x0, y0, x1, y1);
            return t;
        }

        Button Chip(Transform parent, string text, UnityEngine.Events.UnityAction action) => Make(parent, text, action, CardHi, false);
        Button Solid(Transform parent, string text, UnityEngine.Events.UnityAction action) => Make(parent, text, action, Accent, true);
        Button Toggle(Transform parent, string text, bool on, UnityEngine.Events.UnityAction action)
            => Make(parent, text, action, on ? Accent : CardHi, on);

        Button Make(Transform parent, string text, UnityEngine.Events.UnityAction action, Color colour, bool strong)
        {
            var r = Rect(string.IsNullOrEmpty(text) ? "Card" : text, parent);
            var image = r.gameObject.AddComponent<Image>();
            image.color = colour;
            var b = r.gameObject.AddComponent<Button>();
            b.targetGraphic = image;
            var colours = b.colors;
            colours.highlightedColor = new Color(1, 1, 1, .92f);
            colours.pressedColor = new Color(.78f, .86f, 1f);
            colours.fadeDuration = .06f;
            b.colors = colours;
            if (action != null) b.onClick.AddListener(action);
            if (!string.IsNullOrEmpty(text))
            {
                var t = Label(r, text, 18, TextAnchor.MiddleCenter);
                Bounds(t.rectTransform, .04f, 0f, .96f, 1f);
                t.color = strong ? Color.white : new Color(.92f, .95f, 1f);
            }
            return b;
        }

        static void Bounds(RectTransform r, float x, float y, float xx, float yy)
        {
            r.anchorMin = new Vector2(x, y);
            r.anchorMax = new Vector2(xx, yy);
            r.offsetMin = r.offsetMax = Vector2.zero;
        }

        // ---------------------------------------------------------------- lifecycle

        void Update()
        {
            bool visible = service.Visible;
            if (shown != visible)
            {
                shown = visible;
                panel.gameObject.SetActive(visible);
                root.GetChild(0).gameObject.SetActive(visible);
                if (visible) Build();
                else if (!Mathf.Approximately(Time.timeScale, 1f) && Mathf.Approximately(restoreSpeed, 1f)) Time.timeScale = 1f;
            }
            if (!visible) { AttachEntries(); return; }
            modified.text = changes > 0 ? "Modified: " + changes : "";
            statusLine.text = service.Status;
            AttachEntries();
        }

        /// <summary>Adds the MOD entry beside the existing Settings controls, only while mods are on.</summary>
        void AttachEntries()
        {
            // Two scene-wide searches: twice a second is plenty for a button that is added once per screen. Every frame
            // they cost about 1.3 ms in the editor, a fifth of a level frame, and more on a phone's Development build.
            if (Time.unscaledTime < nextAttach) return;
            nextAttach = Time.unscaledTime + .5f;
            foreach (var home in FindObjectsByType<RoyalHomeScreen>(FindObjectsInactive.Include))
            {
                if (!home.settingsGroup || home.settingButtons == null || home.settingButtons.Length < 4) continue;
                if (!attached.Add(home.GetHashCode())) continue;

                // Clone a shipped settings button so the style is identical by construction rather
                // than by eye: same sprite, gradient, border, radius, font and pressed colours.
                var source = home.settingButtons[home.settingButtons.Length - 1];
                var entry = Instantiate(source, home.settingsGroup.transform);
                entry.name = "Setting 4 Mod Menu";
                var text = entry.GetComponentInChildren<Text>();
                if (text) text.text = "Mod Menu";
                entry.onClick.RemoveAllListeners();
                entry.onClick.AddListener(() => { home.ClosePanel(); service.Visible = true; });

                // Five rows now share the group, so every button is re-anchored to a fifth each.
                var all = new List<RectTransform>();
                foreach (var b in home.settingButtons) all.Add((RectTransform)b.transform);
                all.Add((RectTransform)entry.transform);
                float slice = 1f / all.Count;
                for (int i = 0; i < all.Count; i++)
                {
                    all[i].anchorMin = new Vector2(0, 1 - (i + 1) * slice + .012f);
                    all[i].anchorMax = new Vector2(1, 1 - i * slice - .012f);
                    all[i].offsetMin = all[i].offsetMax = Vector2.zero;
                }
            }
            if (!JellyGarden.Scripts.GUI.MenuManager.Instance) return;
            foreach (var menu in JellyGarden.Scripts.GUI.MenuManager.Instance.GetComponentsInChildren<JellyGarden.Scripts.GUI.AnimationManager>(true))
            {
                if (menu.name != "Settings" && menu.name != "MenuPause" || !attached.Add(menu.GetHashCode())) continue;
                var face = menu.transform.Find("Image");
                if (!face) continue;
                var b = Chip(face, "Mod Menu", () => { service.Visible = true; });
                var rect = (RectTransform)b.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0);
                rect.sizeDelta = new Vector2(300, 52);
                rect.anchoredPosition = new Vector2(0, -39);
            }
        }
    }

    /// <summary>Which channels a test should exercise. Read by the menu; SFX maps to the Sound save.</summary>
    public static class RoyalModChannels
    {
        public static bool Animation = true, Vfx = true, Sfx = true;
    }
}
#endif
