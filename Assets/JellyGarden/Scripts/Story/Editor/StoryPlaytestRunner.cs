// Runs StoryPlaytest from a batchmode Unity and exits with its verdict.
//
//   Unity -batchmode -projectPath . -logFile - \
//     -executeMethod JellyGarden.Scripts.Story.Editor.StoryPlaytestRunner.RunHeadless
//
// Note there is no -quit: entering play mode reloads the domain, so the call
// that started the run is gone by the time the run begins. The flag that says a
// run is in progress is therefore kept in EditorPrefs and picked up again on the
// other side of the reload, and this exits the Editor itself when the playtest
// writes its report.
//
// Pass the level as StoryPlaytest.Level in PlayerPrefs, or use RunLevel.

using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace JellyGarden.Scripts.Story.Editor
{
    public static class StoryPlaytestRunner
    {
        private const string Running = "StoryPlaytest.Running";
        private const string Deadline = "StoryPlaytest.Deadline";

        private const string RequestPref = "StoryPlaytest.Requested";
        private const string LevelPref = "StoryPlaytest.Level";
        private const string LevelsPref = "StoryPlaytest.Levels";

        /// <summary>How long the whole run may take before it is called hung.</summary>
        private const double Budget = 420.0;

        private static string ReportPath
        {
            get { return Path.Combine(Application.persistentDataPath, StoryPlaytest.ReportName); }
        }

        /// <summary>
        /// The band a Preview run plays. A menu item cannot be given an
        /// argument and Unity has no text-entry dialog, so the band is read
        /// from a file the caller can write from a shell.
        /// </summary>
        private const string BandFile = "ralph/playtest_band.txt";

        [MenuItem("King Leo/Preview/Playtest level 1")]
        public static void RunLevel1() { Start("1,2", false); }

        /// <summary>
        /// Plays whatever is in the band file, in the Editor already open, and
        /// leaves it open afterwards. This is the way to watch a run: the
        /// batchmode entry point exits the Editor when the report lands, which
        /// on a human's Editor is a closed project rather than a verdict.
        /// </summary>
        // A "/" in a MenuItem path opens a submenu, so the file cannot be named here.
        [MenuItem("King Leo/Preview/Playtest the band file")]
        public static void RunBand() { Start(Band(), false); }

        private static string Band()
        {
            try
            {
                string path = Path.Combine(Directory.GetCurrentDirectory(), BandFile);
                if (File.Exists(path))
                {
                    string band = File.ReadAllText(path).Trim();
                    if (band.Length > 0) return band;
                }
            }
            catch (global::System.Exception e)
            {
                Debug.LogWarning("[Playtest] could not read " + BandFile + ": " + e.Message);
            }
            return "1";
        }

        /// <summary>Batchmode entry point. Plays the level in StoryPlaytest.Level, or 1.</summary>
        public static void RunHeadless()
        {
            string levels = "1";
            foreach (string arg in global::System.Environment.GetCommandLineArgs())
            {
                if (!arg.StartsWith("-playtestLevels=")) continue;
                levels = arg.Substring("-playtestLevels=".Length);
            }
            Start(levels, true);
        }

        /// <summary>
        /// Arms the run. <paramref name="exitWhenDone"/> is for batchmode only:
        /// it quits the Editor as soon as the report is written, which is how a
        /// headless host learns the verdict and is never what a human wants.
        /// </summary>
        private static void Start(string levels, bool exitWhenDone)
        {
            PlayerPrefs.SetInt(RequestPref, 1);
            PlayerPrefs.SetString(LevelsPref, levels);
            PlayerPrefs.Save();

            if (File.Exists(ReportPath)) File.Delete(ReportPath);

            EditorPrefs.SetBool(Running, exitWhenDone);
            if (exitWhenDone)
                EditorPrefs.SetString(Deadline,
                    (EditorApplication.timeSinceStartup + Budget).ToString());

            Debug.Log("[Playtest] runner: opening the menu scene and entering play mode for levels " + levels + ".");
            EditorSceneManager.OpenScene("Assets/JellyGarden/Scenes/main.unity");
            EditorApplication.EnterPlaymode();
        }

        /// <summary>Picked up again after the domain reload that entering play mode causes.</summary>
        [InitializeOnLoadMethod]
        private static void Resume()
        {
            if (!EditorPrefs.GetBool(Running, false)) return;
            EditorApplication.update += Poll;
        }

        private static void Poll()
        {
            double deadline;
            double.TryParse(EditorPrefs.GetString(Deadline, "0"), out deadline);

            if (File.Exists(ReportPath))
            {
                Done(File.ReadAllText(ReportPath), 0);
                return;
            }

            if (EditorApplication.timeSinceStartup > deadline)
            {
                Done("==== playtest ====\nFINDING: the run never produced a report inside " +
                     Budget + "s. The game hung before the playtest could finish.\n==== end ====", 3);
            }
        }

        private static void Done(string report, int code)
        {
            EditorApplication.update -= Poll;
            EditorPrefs.SetBool(Running, false);
            PlayerPrefs.SetInt(RequestPref, 0);
            PlayerPrefs.Save();

            Debug.Log("[Playtest] runner: report follows.\n" + report);
            EditorApplication.Exit(code);
        }
    }
}
