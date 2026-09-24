#if UNITY_EDITOR && !ROYAL_DISABLE_QA
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using JellyGarden.Scripts;
using JellyGarden.Scripts.GUI;
using JellyGarden.Scripts.Juice;
using RoyalThrone.Testing;
using Unity.Collections;
using UnityEngine;

namespace RoyalThrone.Editor
{
    /// <summary>
    /// Records solo preview footage of the live runtime effects for the developer handoff.
    /// Editor-only. It plays the same public entry points the game uses (Item.ChangeType,
    /// RoyalPowerupDirector.Tap/TrySwap, the QA service fixtures) on the isolated QA practice
    /// board, so what it records is the shipping effect, not a reconstruction.
    ///
    /// Frames advance on Time.captureDeltaTime, so every recorded frame is a distinct 1/60 s
    /// simulation step at 1.0x speed - no duplicated frames and no slow motion. Unity's real
    /// audio mix is rendered on the same clock through AudioRenderer.
    /// </summary>
    public static class RoyalHandoffCapture
    {
        public const int Fps = 60;
        public static string Root = "";
        public static string Filter = "";
        public static bool Running { get; private set; }
        public static bool Finished { get; private set; }
        public static string Status = "idle";
        public static string LastError = "";

        static BinaryWriter audio;
        static string folder;
        static int frame, channels;
        static long samples;
        static readonly List<string> marks = new List<string>();
        static readonly List<string> results = new List<string>();
        static bool pumping, pumpAlive;

        static LevelManager LM => LevelManager.THIS;
        static RoyalQaService Qa => RoyalQaService.Instance;
        static Square Cell(int x, int y) => LM.GetSquare(x, y);
        static Item Piece(int x, int y) => Cell(x, y).item;
        static Vector3 Centre => LM.GameField.position + new Vector3((LM.maxCols - 1) * LM.squareWidth * .5f, -(LM.maxRows - 1) * LM.squareHeight * .5f, -.2f);

        public static void Run(string root, string filter)
        {
            if (Running) throw new InvalidOperationException("A handoff capture is already running.");
            if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
            Root = root; Filter = filter ?? ""; Finished = false; LastError = ""; results.Clear();
            RoyalRegressionRunner.Run(Suite());
        }

        // ---------------------------------------------------------------- capture plumbing

        static void Begin(string name)
        {
            folder = Path.Combine(Root, name);
            Directory.CreateDirectory(folder);
            foreach (var old in Directory.GetFiles(folder)) File.Delete(old);
            frame = 0; samples = 0; marks.Clear();
            channels = AudioSettings.speakerMode == AudioSpeakerMode.Mono ? 1 : 2;
            if (!AudioRenderer.Start()) throw new Exception("Unity audio recording could not start.");
            audio = new BinaryWriter(File.Create(Path.Combine(folder, "audio.wav")));
            audio.Write(new byte[44]);
        }

        static void Shot()
        {
            var texture = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(folder, frame.ToString("D5") + ".jpg"), texture.EncodeToJPG(86));
            UnityEngine.Object.Destroy(texture);
            frame++;
            int n = AudioRenderer.GetSampleCountForCaptureFrame() * channels;
            using (var buffer = new NativeArray<float>(n, Allocator.Temp))
            {
                if (!AudioRenderer.Render(buffer)) throw new Exception("Unity audio render failed.");
                foreach (float value in buffer) { audio.Write((short)Mathf.RoundToInt(Mathf.Clamp(value, -1, 1) * 32767)); samples++; }
            }
        }

        static void Mark(string stage) { marks.Add(frame + "\t" + stage); Status = folder != null ? Path.GetFileName(folder) + " / " + stage : stage; }

        static void End(string name, string note)
        {
            if (audio == null) return;
            AudioRenderer.Stop();
            int rate = AudioSettings.outputSampleRate;
            audio.Seek(0, SeekOrigin.Begin);
            audio.Write(Encoding.ASCII.GetBytes("RIFF")); audio.Write((int)samples * 2 + 36);
            audio.Write(Encoding.ASCII.GetBytes("WAVEfmt ")); audio.Write(16); audio.Write((short)1);
            audio.Write((short)channels); audio.Write(rate); audio.Write(rate * channels * 2);
            audio.Write((short)(channels * 2)); audio.Write((short)16);
            audio.Write(Encoding.ASCII.GetBytes("data")); audio.Write((int)samples * 2);
            audio.Dispose(); audio = null;
            File.WriteAllLines(Path.Combine(folder, "stages.tsv"), new[] { "frame\tstage" }.Concat(marks));
            File.WriteAllText(Path.Combine(folder, "capture.json"),
                "{\"clip\":\"" + name + "\",\"frames\":" + frame + ",\"fps\":" + Fps + ",\"channels\":" + channels +
                ",\"sampleRate\":" + rate + ",\"samples\":" + samples + ",\"note\":\"" + note + "\"}");
            results.Add(name + "\t" + frame + "\tok");
        }

        static IEnumerator Pump()
        {
            pumpAlive = true;
            while (pumpAlive)
            {
                yield return new WaitForEndOfFrame();
                if (pumping && audio != null) Shot();
            }
        }

        /// <summary>Advances game time by <paramref name="seconds"/>. The pump records each frame.</summary>
        static IEnumerator Hold(float seconds)
        {
            int n = Mathf.Max(1, Mathf.RoundToInt(seconds * Fps));
            for (int i = 0; i < n; i++) yield return null;
        }

        /// <summary>
        /// Waits for the board to stop moving, budgeting rendered frames rather than real
        /// seconds: while recording, one frame costs ~50 ms of wall clock, so a real-time
        /// budget would expire long before the board has actually finished.
        /// </summary>
        static IEnumerator Quiet(float timeout = 8f)
        {
            int budget = Mathf.RoundToInt(timeout * Fps), spent = 0, calm = 0;
            while (calm < 6 && spent++ < budget)
            {
                bool moving = LM.IsResolvingMatches || RoyalPowerupDirector.Busy || LM.DragBlocked
                              || LM.GetItems().Any(i => i && (i.falling || i.destroying));
                calm = moving ? 0 : calm + 1;
                yield return null;
            }
        }

        // ---------------------------------------------------------------- board fixtures

        static void Make(int x, int y, ItemsTypes type)
        {
            var item = Piece(x, y);
            item.NextType = type;
            item.ChangeType();
        }

        static void Plain(int x, int y, int colour)
        {
            var item = Piece(x, y);
            item.currentType = ItemsTypes.NONE; item.NextType = ItemsTypes.NONE;
            item.SetColor(Mathf.Clamp(colour, 0, 5));
            item.GetComponent<RoyalPieceSkin>()?.ApplyNow();
        }

        /// <summary>
        /// Rebuilds the isolated practice board. Runs with the capture pump OFF: recording a
        /// frame costs ~50 ms, and RoyalQaService.Settled() budgets 30 *real* seconds, so
        /// rebuilding while recording makes a perfectly healthy board look like it never settles.
        /// </summary>
        static IEnumerator Fresh()
        {
            bool wasPumping = pumping;
            pumping = false;
            try
            {
                RoyalPowerupDirector.Get.Cancel();
                MatchVFXService.Instance.Clear();
                CandyShatterService.Instance.Clear();
                yield return Quiet(20f);
                yield return Qa.Practice();
                LM.Limit = 99; LM.DragBlocked = false;
                yield return null;
            }
            finally { pumping = wasPumping; }
        }

        // ---------------------------------------------------------------- clips

        struct Clip { public string Name; public Func<IEnumerator> Body; public string Note; }

        static List<Clip> Clips()
        {
            var list = new List<Clip>();
            void A(string name, Func<IEnumerator> body, string note = "") => list.Add(new Clip { Name = name, Body = body, Note = note });

            // --- boosters: one continuous 1.0x take, stage-marked ---
            A("Rocket", () => Booster(ItemsTypes.HORIZONTAL_STRIPPED, true), "horizontal rocket, full stage chain");
            A("Rocket-Vertical", () => Booster(ItemsTypes.VERTICAL_STRIPPED, true), "vertical rocket, full stage chain");
            A("TNT", () => Booster(ItemsTypes.PACKAGE, true), "TNT reveal frames, idle loop, ignition, blast");
            A("Propeller", () => Booster(ItemsTypes.HELICOPTER, true), "propeller reveal, rotor idle, flight, impact");
            A("ColorBall", ColorBallClip, "color ball reveal, idle, charge, beams, conversion");

            // --- combinations ---
            A("Combo-Rocket-Rocket", () => Combo(ItemsTypes.HORIZONTAL_STRIPPED, ItemsTypes.VERTICAL_STRIPPED));
            A("Combo-Rocket-TNT", () => Combo(ItemsTypes.HORIZONTAL_STRIPPED, ItemsTypes.PACKAGE));
            A("Combo-TNT-TNT", () => Combo(ItemsTypes.PACKAGE, ItemsTypes.PACKAGE));
            A("Combo-Propeller-Propeller", () => Combo(ItemsTypes.HELICOPTER, ItemsTypes.HELICOPTER));
            A("Combo-Propeller-TNT", () => Combo(ItemsTypes.HELICOPTER, ItemsTypes.PACKAGE));
            A("Combo-Propeller-Rocket", () => Combo(ItemsTypes.HELICOPTER, ItemsTypes.HORIZONTAL_STRIPPED));
            A("Combo-ColorBall-Rocket", () => Combo(ItemsTypes.BOMB, ItemsTypes.HORIZONTAL_STRIPPED));
            A("Combo-ColorBall-TNT", () => Combo(ItemsTypes.BOMB, ItemsTypes.PACKAGE));
            A("Combo-ColorBall-Propeller", () => Combo(ItemsTypes.BOMB, ItemsTypes.HELICOPTER));
            A("Combo-ColorBall-ColorBall", () => Combo(ItemsTypes.BOMB, ItemsTypes.BOMB));

            // --- board effects ---
            A("Match-Match3", () => MatchRun(3));
            A("Match-Match4", () => MatchRun(4), "match 4 creates a rocket");
            A("Match-Match5", () => MatchRun(5), "match 5 creates a color ball");
            A("Match-Cascade", Cascade);
            A("Destruction-Fragments", Fragments);
            A("Blockers-Break", BlockerBreak);
            A("Blockers-FireImpact", () => Impact(true));
            A("Blockers-IceImpact", () => Impact(false));
            A("GravityRefill-Fall", Fall);
            A("Hint-Aura", HintAura);
            A("Celebration-Combo", CelebrationClip);
            A("Rewards-StarReward", StarReward);
            A("Rewards-CoinReward", CoinReward);
            A("WinFail-Win", () => Outcome(true));
            A("WinFail-Fail", () => Outcome(false));

            return list;
        }

        static IEnumerator Booster(ItemsTypes type, bool stages)
        {
            yield return Fresh();
            Mark("Reveal");
            Make(3, 3, type);
            yield return Hold(type == ItemsTypes.PACKAGE ? 2.1f : 1.0f);
            Mark("Idle");
            yield return Hold(1.4f);
            var item = Piece(3, 3);
            Mark(type == ItemsTypes.PACKAGE ? "Charge" : "Activation");
            RoyalPowerupDirector.Tap(item);
            if (type == ItemsTypes.PACKAGE)
            {
                yield return Hold(TNT3DAnimator.IgnitionDuration);
                Mark("Explosion");
                yield return Hold(1.1f);
            }
            else
            {
                yield return Hold(.22f);
                Mark("Travel");
                yield return Hold(.85f);
                Mark("Impact");
                yield return Hold(.75f);
            }
            Mark("Cleanup");
            yield return Quiet();
            yield return Hold(.5f);
        }

        static IEnumerator ColorBallClip()
        {
            yield return Fresh();
            Mark("Reveal");
            Make(3, 3, ItemsTypes.BOMB);
            yield return Hold(1.0f);
            Mark("Idle");
            yield return Hold(1.4f);
            Mark("Charge");
            RoyalPowerupDirector.Tap(Piece(3, 3));
            yield return Hold(RoyalThrone.Boosters.ColorBall.V4.ColorBallVisualV4.ChargeDuration);
            Mark("Activation");
            yield return Hold(.55f);
            Mark("Impact");
            yield return Hold(.9f);
            Mark("Cleanup");
            yield return Quiet();
            yield return Hold(.5f);
        }

        static IEnumerator Combo(ItemsTypes a, ItemsTypes b)
        {
            yield return Fresh();
            Mark("Setup");
            Make(3, 3, a);
            Make(4, 3, b);
            yield return Hold(1.3f);
            Mark("Activation");
            if (!RoyalPowerupDirector.TrySwap(Piece(3, 3), Piece(4, 3)))
                throw new InvalidOperationException("Combination rejected by the game.");
            LM.FindMatches();
            yield return Hold(2.2f);
            Mark("Cleanup");
            yield return Quiet(12f);
            yield return Hold(.5f);
        }

        static IEnumerator MatchRun(int n)
        {
            yield return Fresh();
            Mark("Board");
            yield return Hold(.5f);
            for (int x = 1; x <= n; x++) Plain(x, 3, 0);
            yield return Hold(.25f);
            Mark("Match");
            LM.FindMatches();
            yield return Hold(1.4f);
            Mark("Cleanup");
            yield return Quiet();
            yield return Hold(.5f);
        }

        static IEnumerator Cascade()
        {
            yield return Fresh();
            Mark("Board");
            yield return Hold(.4f);
            for (int x = 1; x <= 3; x++) Plain(x, 3, 0);
            for (int x = 1; x <= 3; x++) Plain(x, x == 2 ? 1 : 2, 4);
            yield return Hold(.25f);
            Mark("Cascade");
            LM.FindMatches();
            yield return Hold(2.2f);
            Mark("Cleanup");
            yield return Quiet();
            yield return Hold(.5f);
        }

        static IEnumerator Fragments()
        {
            yield return Fresh();
            Mark("Shatter");
            CandyShatterService.Instance.Shatter(Piece(3, 3).sprRenderer, 1);
            CandyShatterService.Instance.Shatter(Piece(4, 3).sprRenderer, 1);
            yield return Hold(2.2f);
            Mark("Cleanup");
            yield return Hold(.6f);
        }

        static IEnumerator BlockerBreak()
        {
            yield return Fresh();
            Mark("Board");
            var squares = new[] { Cell(2, 4), Cell(3, 4), Cell(4, 4) };
            foreach (var sq in squares)
            {
                var block = UnityEngine.Object.Instantiate(LM.wireBlockPrefab, sq.transform.position, Quaternion.identity, LM.GameField);
                sq.block.Add(block); sq.type = SquareTypes.WIREBLOCK;
            }
            yield return Hold(.8f);
            Mark("Break");
            foreach (var sq in squares) sq.DestroyBlock();
            yield return Hold(1.6f);
            Mark("Cleanup");
            yield return Quiet();
            yield return Hold(.5f);
        }

        static IEnumerator Impact(bool fire)
        {
            yield return Fresh();
            Mark("Board");
            yield return Hold(.35f);
            Mark(fire ? "Fire" : "Ice");
            if (fire) RoyalToolkitImpact.TryPlayFire(Centre, LM.squareWidth);
            else RoyalToolkitImpact.TryPlayIce(Centre, LM.squareWidth);
            yield return Hold(1.9f);
            Mark("Cleanup");
            yield return Hold(.5f);
        }

        static IEnumerator Fall()
        {
            yield return Fresh();
            Mark("Board");
            yield return Hold(.35f);
            Mark("Fall");
            for (int x = 2; x <= 5; x++)
            {
                var i = Piece(x, 3);
                i.transform.position += Vector3.up * LM.squareHeight * (3 + (x % 2));
                i.justCreatedItem = true;
                i.StartFalling();
            }
            yield return Hold(1.8f);
            Mark("Cleanup");
            yield return Quiet();
            yield return Hold(.4f);
        }

        static IEnumerator HintAura()
        {
            yield return Fresh();
            Mark("Board");
            Cell(1, 3).item.SetColor(0); Cell(2, 3).item.SetColor(0);
            Cell(3, 3).item.SetColor(2); Cell(3, 2).item.SetColor(0);
            yield return Hold(.4f);
            var plan = RoyalHintSolver.Find(LM);
            if (plan == null) throw new InvalidOperationException("No hint in the fixture.");
            Mark("Hint");
            RoyalHintSettings.Enabled = true;
            yield return null;
            AI.THIS.GetComponent<RoyalHintPresenter>().Show(plan);
            yield return Hold(2.6f);
            Mark("Cleanup");
            AI.THIS.GetComponent<RoyalHintPresenter>().Hide();
            yield return Hold(.4f);
        }

        static IEnumerator CelebrationClip()
        {
            yield return Fresh();
            Mark("Board");
            yield return Hold(.3f);
            Mark("Celebration");
            RoyalCelebration.Play(4);
            yield return Hold(2.2f);
            Mark("Cleanup");
            yield return Hold(.5f);
        }

        static IEnumerator StarReward()
        {
            yield return Fresh();
            Mark("Board");
            yield return Hold(.3f);
            Mark("Reward");
            int stars = LM.stars; LM.stars = 3;
            MenuManager.Instance.ShowMenuComplete();
            yield return Hold(3.4f);
            Mark("Cleanup");
            foreach (var a in MenuManager.Instance.GetComponentsInChildren<JellyGarden.Scripts.GUI.AnimationManager>(true)) a.StopAllCoroutines();
            MenuManager.Instance.HideAllCanvasElements();
            LM.stars = stars;
            yield return Hold(.5f);
        }

        static IEnumerator CoinReward()
        {
            yield return Fresh();
            Mark("Board");
            yield return Hold(.3f);
            Mark("Coins");
            RoyalPowerupDirector.Get.Firework(Centre, 1);
            RoyalAudioDirector.Get.Play(RoyalCue.Create);
            yield return Qa.GetComponent<RoyalQaMenu>().CoinReward();
            Mark("Cleanup");
            yield return Hold(.5f);
        }

        static IEnumerator Outcome(bool win)
        {
            yield return Fresh();
            Mark("Board");
            yield return Hold(.3f);
            Mark(win ? "Win" : "Fail");
            if (win)
            {
                int stars = LM.stars; LM.stars = 3;
                MenuManager.Instance.ShowMenuComplete();
                yield return Hold(3.2f);
                foreach (var a in MenuManager.Instance.GetComponentsInChildren<JellyGarden.Scripts.GUI.AnimationManager>(true)) a.StopAllCoroutines();
                MenuManager.Instance.HideAllCanvasElements();
                LM.stars = stars;
            }
            else
            {
                MenuManager.Instance.ShowMenuFailed();
                yield return Hold(3.2f);
                foreach (var a in MenuManager.Instance.GetComponentsInChildren<JellyGarden.Scripts.GUI.AnimationManager>(true)) a.StopAllCoroutines();
                MenuManager.Instance.HideAllCanvasElements();
            }
            Mark("Cleanup");
            yield return Hold(.5f);
        }

        // ---------------------------------------------------------------- suite

        static IEnumerator Suite()
        {
            Running = true;
            float capture = Time.captureDeltaTime;
            float scale = Time.timeScale;
            bool visible = Qa != null && Qa.Visible;
            bool counters = Qa != null && Qa.Counters;
            float speed = Qa != null ? Qa.Speed : 1f;
            var canvases = Qa != null ? Qa.GetComponentsInChildren<Canvas>(true).ToArray() : new Canvas[0];
            var canvasState = canvases.Select(c => c.enabled).ToArray();
            var host = new GameObject("Royal handoff capture pump") { hideFlags = HideFlags.HideAndDontSave };
            var pumpHost = host.AddComponent<RoyalRegressionRunner>();
            Directory.CreateDirectory(Root);
            try
            {
                if (!RoyalSave.Testing) Qa.StartSession();
                Qa.Loop = false; Qa.Visible = false; Qa.Counters = false; Qa.Speed = 1f;
                Time.timeScale = 1f;
                foreach (var c in canvases) c.enabled = false;
                RoyalSave.SetInt("Sound", 1);
                Time.captureDeltaTime = 1f / Fps;
                pumpHost.StartCoroutine(Pump());
                pumping = true;

                foreach (var clip in Clips())
                {
                    // Filter is a comma-separated list of exact clip names; empty means all.
                    if (Filter.Length > 0 && !Filter.Split(',').Any(f => f.Trim() == clip.Name)) continue;
                    Status = clip.Name;
                    Exception failure = null;
                    Begin(clip.Name);
                    var body = clip.Body();
                    var stack = new Stack<IEnumerator>();
                    stack.Push(body);
                    while (stack.Count > 0)
                    {
                        var step = stack.Peek(); bool next = false;
                        try { next = step.MoveNext(); } catch (Exception ex) { failure = ex; }
                        if (failure != null) break;
                        if (!next) { stack.Pop(); continue; }
                        if (step.Current is IEnumerator child && !(child is CustomYieldInstruction)) stack.Push(child);
                        else yield return step.Current;
                    }
                    if (failure != null)
                    {
                        if (audio != null) { AudioRenderer.Stop(); audio.Dispose(); audio = null; }
                        results.Add(clip.Name + "\t" + frame + "\tFAILED: " + failure.Message.Replace("\t", " ").Replace("\n", " "));
                        Debug.LogWarning("[Royal handoff] " + clip.Name + " failed: " + failure.Message);
                        // recover the board before the next clip
                        RoyalPowerupDirector.Get.Cancel();
                        yield return null;
                    }
                    else End(clip.Name, clip.Note);
                    yield return Hold(.2f);
                }
            }
            finally
            {
                pumping = false; pumpAlive = false;
                if (audio != null) { try { AudioRenderer.Stop(); } catch { } audio.Dispose(); audio = null; }
                // This project runs with domain reload disabled (EnterPlayModeOptions 3), so a
                // static left set here survives Play Mode exit and silently downgrades the live
                // board on the next run. Force the Color Ball back to its shipping prefab even if
                // a caller flipped the baseline switch before starting a comparison capture.
                if (RoyalThrone.Boosters.ColorBall.V4.ColorBallVisualV4.UseV4Baseline)
                {
                    RoyalThrone.Boosters.ColorBall.V4.ColorBallVisualV4.Clear();
                    RoyalThrone.Boosters.ColorBall.V4.ColorBallVisualV4.UseV4Baseline = false;
                    Debug.Log("[Royal handoff] restored ColorBallVisualV4.UseV4Baseline = false");
                }
                Time.captureDeltaTime = capture;
                Time.timeScale = scale;
                if (Qa != null) { Qa.Visible = visible; Qa.Counters = counters; Qa.Speed = speed; }
                for (int i = 0; i < canvases.Length; i++) if (canvases[i]) canvases[i].enabled = canvasState[i];
                if (host) UnityEngine.Object.DestroyImmediate(host);
                File.WriteAllLines(Path.Combine(Root, "capture-report.tsv"), new[] { "clip\tframes\tstatus" }.Concat(results));
                Running = false; Finished = true; Status = "finished: " + results.Count(r => r.EndsWith("ok")) + " ok / " + results.Count + " total";
                Debug.Log("[Royal handoff] " + Status);
            }
        }
    }
}
#endif
