#if UNITY_EDITOR && !ROYAL_DISABLE_QA
// Runs Royal Throne's Play-mode check suites (PortChecks) from a batchmode
// Unity and exits with the verdict.
//
//   Unity -batchmode -projectPath . -logFile - \
//     -executeMethod JellyGarden.Scripts.Story.Editor.PortChecksRunner.RunHeadless \
//     [-portChecks=Overlap,QaChecks] [-portChecksTimeout=300] [-portChecksBudget=3600]
//
// No -quit and no -nographics: entering Play mode reloads the domain, so the
// run is armed in SessionState and resumed on the other side of the reload,
// and several suites check that shaders are supported, which needs a real
// graphics device. Unity's project lock means the project must not be open in
// another Editor; for an open Editor use King Leo > Port checks, or the file
// bridge described in PortChecks.cs.
//
// -portChecks         suites to run (default: every suite that does not read
//                     Game-view pixels; 'all' means the same)
// -portChecksTimeout  per-suite timeout in seconds for every suite (default:
//                     each suite's own, 240 s unless the suite is known long)
// -portChecksBudget   whole-run budget in seconds (default: 300 + each
//                     suite's timeout + 90)
//
// Writes Logs/port-checks/report.txt, ending 'SUMMARY: N pass, M fail,
// K timeout, E error'. Exit code 0 only when every suite passed. A watchdog
// thread kills the Editor if the main thread stops answering for five minutes
// or the budget runs two minutes over (exit code is then the kill signal's,
// never 0).

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace JellyGarden.Scripts.Story.Editor
{
    public static class PortChecksRunner
    {
        /// <summary>Batchmode entry point.</summary>
        public static void RunHeadless()
        {
            string list = Argument("-portChecks=");
            double timeout = Number(Argument("-portChecksTimeout="));
            double budget = Number(Argument("-portChecksBudget="));

            IList<string> names;
            if (string.IsNullOrEmpty(list) || string.Equals(list.Trim(), "all", StringComparison.OrdinalIgnoreCase))
                names = PortChecks.HeadlessSuiteNames;
            else
                names = list.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();

            Debug.Log("[Port checks] headless run: " + string.Join(", ", names.ToArray()));
            try
            {
                PortChecks.Start(names, true, timeout, budget, null);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                try
                {
                    File.AppendAllText(PortChecks.ReportPath, "ERROR PortChecks could not start: " + e.Message + "\n");
                    int ignored;
                    File.AppendAllText(PortChecks.ReportPath, PortChecks.SummaryOf(PortChecks.ReportPath, out ignored) + "\n");
                }
                catch (Exception)
                {
                    // The exit code below is the verdict either way.
                }
                EditorApplication.Exit(1);
            }
        }

        private static string Argument(string prefix)
        {
            foreach (string arg in global::System.Environment.GetCommandLineArgs())
                if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return arg.Substring(prefix.Length);
            return null;
        }

        private static double Number(string text)
        {
            double value;
            return double.TryParse(text, global::System.Globalization.NumberStyles.Float, global::System.Globalization.CultureInfo.InvariantCulture, out value) && value > 0
                ? value
                : 0;
        }

        #region Watchdog

        // EditorApplication.update keeps ticking while a coroutine hangs, so the
        // per-suite timeouts and the run budget are enforced from there. This
        // thread is for the case nothing on the main thread can handle: a suite
        // or the game stuck in a loop that never yields.

        /// <summary>How long the main thread may go without a heartbeat.</summary>
        private const double StallSeconds = 300;

        private static global::System.Threading.Thread _thread;
        private static readonly global::System.Threading.ManualResetEvent Stop = new global::System.Threading.ManualResetEvent(false);
        private static long _heartbeat;
        private static long _deadline;
        private static string _report;

        [InitializeOnLoadMethod]
        private static void Install()
        {
            // A sleeping thread must not hold up the domain unload.
            AssemblyReloadEvents.beforeAssemblyReload -= StopWatchdog;
            AssemblyReloadEvents.beforeAssemblyReload += StopWatchdog;
            EditorApplication.quitting -= StopWatchdog;
            EditorApplication.quitting += StopWatchdog;
        }

        /// <summary>Called from the main thread's poll; the watchdog reads it.</summary>
        internal static void Heartbeat()
        {
            global::System.Threading.Interlocked.Exchange(ref _heartbeat, DateTime.UtcNow.Ticks);
        }

        internal static void StartWatchdog(DateTime hardDeadlineUtc, string report)
        {
            global::System.Threading.Interlocked.Exchange(ref _deadline, hardDeadlineUtc.Ticks);
            _report = report;
            Heartbeat();
            if (_thread != null && _thread.IsAlive) return;
            Stop.Reset();
            _thread = new global::System.Threading.Thread(Watch) { IsBackground = true, Name = "Port checks watchdog" };
            _thread.Start();
        }

        /// <summary>The run is over and the Editor is exiting: give it 90 s, then kill it.</summary>
        internal static void ExpectExit()
        {
            global::System.Threading.Interlocked.Exchange(ref _deadline, DateTime.UtcNow.AddSeconds(90).Ticks);
        }

        internal static void StopWatchdog()
        {
            Stop.Set();
            var thread = _thread;
            _thread = null;
            if (thread != null && thread.IsAlive && thread != global::System.Threading.Thread.CurrentThread) thread.Join(2000);
        }

        private static void Watch()
        {
            while (!Stop.WaitOne(2000))
            {
                long now = DateTime.UtcNow.Ticks;
                long beat = global::System.Threading.Interlocked.Read(ref _heartbeat);
                long deadline = global::System.Threading.Interlocked.Read(ref _deadline);
                string why = null;
                if (now > deadline)
                    why = PortChecks.Finished
                        ? "the run finished but the Editor did not exit"
                        : "the run's budget is over by two minutes and the Editor has not exited";
                else if (beat > 0 && TimeSpan.FromTicks(now - beat).TotalSeconds > StallSeconds)
                    why = "the Editor's main thread has not ticked for " + StallSeconds.ToString("0") + " s";
                if (why == null) continue;

                try
                {
                    if (!PortChecks.Finished && _report != null)
                    {
                        File.AppendAllText(_report, "TIMEOUT PortChecks watchdog: " + why + "; killing the Editor\n");
                        int ignored;
                        File.AppendAllText(_report, PortChecks.SummaryOf(_report, out ignored) + "\n");
                    }
                }
                catch (Exception)
                {
                    // Killing the process matters more than the last line of the report.
                }
                global::System.Diagnostics.Process.GetCurrentProcess().Kill();
                return;
            }
        }

        #endregion
    }
}
#endif
