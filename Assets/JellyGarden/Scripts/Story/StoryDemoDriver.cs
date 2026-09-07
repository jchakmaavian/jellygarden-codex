// Drives the built player through the story loop and screenshots each step.
//
// Launch the app with -storydemo and it walks itself: menu -> map -> area 1 ->
// spend stars on every repair -> clue card -> next area unlocked, capturing a
// PNG at each beat and logging what it saw.
//
// This exists because verifying by synthetic mouse clicks does not work here:
// the player window cascades to a new position on every launch and Unity moves
// it again after start, so screen coordinates captured a moment ago are already
// stale. Driving the game through its own API is both reliable and closer to
// what is actually being checked.
//
//   tools/run_player.sh -storydemo
//
// Screenshots land next to the log, in the player's persistent data path.

using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JellyGarden.Scripts.Story
{
    public sealed class StoryDemoDriver : MonoBehaviour
    {
        private const string Flag = "-storydemo";
        private const string GameScene = "game";

        private static bool _running;

        private string _shotDir;
        private int _shot;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void Install()
        {
            if (_running) return;

            bool asked = false;
            foreach (string arg in global::System.Environment.GetCommandLineArgs())
                if (arg == Flag) asked = true;
            if (!asked) return;

            _running = true;
            var go = new GameObject("~StoryDemoDriver");
            DontDestroyOnLoad(go);
            go.AddComponent<StoryDemoDriver>();
        }

        private void Start()
        {
            _shotDir = Path.Combine(Application.persistentDataPath, "demo");
            Directory.CreateDirectory(_shotDir);
            StartCoroutine(Drive());
        }

        private IEnumerator Drive()
        {
            Log("demo starting; screenshots in " + _shotDir);
            yield return new WaitForSecondsRealtime(2f);
            yield return Shot("01_menu");

            // Straight into the map. The menu's Play button just loads this scene.
            if (SceneManager.GetActiveScene().name != GameScene)
            {
                Log("loading the game scene");
                SceneManager.LoadScene(GameScene);
                yield return new WaitForSecondsRealtime(4f);
            }
            yield return Shot("02_map");

            StoryProgress.ResetAll();
            yield return new WaitForSecondsRealtime(0.5f);

            int areaId = 1;
            StoryArea area = StoryDatabase.GetArea(areaId);
            Log(string.Format("area {0} is '{1}': {2} tasks, {3} stars to restore",
                areaId, area.Name, area.Tasks.Count, area.StarCost));

            // Open it the same way a tap on the map node would.
            StoryFlow.HandleMapNodeClicked(areaId);
            yield return new WaitForSecondsRealtime(1.5f);
            yield return Shot("03_area_empty");
            Log("area screen open with no stars: the first repair should be unaffordable");

            // Earn the area's stars. The real path is winning levels; this pays
            // them in directly so the meta layer can be seen without playing four
            // boards by hand.
            StoryProgress.AddStar(areaId, area.StarCost);
            if (AreaScreen.Instance != null) AreaScreen.Instance.Refresh();
            yield return new WaitForSecondsRealtime(1f);
            yield return Shot("04_area_funded");
            Log(string.Format("{0} stars earned; every repair should now be payable",
                StoryProgress.StarsIn(areaId)));

            // Spend them, one repair at a time, watching the cutscene each time.
            for (int i = 0; i < area.Tasks.Count; i++)
            {
                StoryTask task = area.Tasks[i];
                RepairWorkType work = RepairWork.Classify(task.Label);

                bool paid = StoryRepair.Perform(areaId, task.Index,
                    () => { if (AreaScreen.Instance != null) AreaScreen.Instance.Refresh(); });

                Log(string.Format("repair {0}/{1} '{2}' [{3}]: {4}",
                    i + 1, area.Tasks.Count, task.Label, work, paid ? "done" : "REFUSED"));

                // Catch the cutscene at its two beats: the work happening, and
                // the repaired state wiping in.
                yield return new WaitForSecondsRealtime(1.5f);
                yield return Shot(string.Format("05_repair{0}_working", i + 1));

                yield return new WaitForSecondsRealtime(1.3f);
                yield return Shot(string.Format("06_repair{0}_reveal", i + 1));

                // And wait for it to finish before touching anything else.
                float guard = 0f;
                while (RepairCutscene.IsPlaying && guard < 8f)
                {
                    guard += Time.unscaledDeltaTime;
                    yield return null;
                }
                Log(string.Format("repair {0} cutscene finished", i + 1));
            }

            // The last repair fires the clue and closes the area.
            yield return new WaitForSecondsRealtime(1.2f);
            yield return Shot("07_clue");
            Log("clue seen: " + StoryProgress.IsClueSeen(areaId));

            yield return new WaitForSecondsRealtime(4f);
            yield return Shot("08_after_clue");

            Log(string.Format("area {0} complete={1}; area {2} unlocked={3}",
                areaId, StoryProgress.IsAreaComplete(areaId),
                areaId + 1, StoryProgress.IsAreaUnlocked(areaId + 1)));

            // And the next area, to show the story moving on.
            StoryFlow.HandleMapNodeClicked(areaId + 1);
            yield return new WaitForSecondsRealtime(1.5f);
            yield return Shot("09_area_two");

            StoryArea next = StoryDatabase.GetArea(areaId + 1);
            Log(string.Format("area {0} is '{1}': {2}", next.Id, next.Name, next.Goal));

            // Areas 001-002 ship a picture per repair. Everything else ships one
            // restored room, which is a different path through the cutscene, so
            // one of those gets demonstrated too.
            const int SingleImageArea = 5;
            StoryProgress.ResetAll();
            for (int i = 1; i < SingleImageArea; i++)
            {
                StoryArea done = StoryDatabase.GetArea(i);
                StoryProgress.AddStar(i, done.StarCost);
                foreach (StoryTask t in done.Tasks) StoryProgress.CompleteTask(i, t.Index);
            }

            StoryArea hall = StoryDatabase.GetArea(SingleImageArea);
            StoryProgress.AddStar(SingleImageArea, hall.StarCost);
            StoryFlow.HandleMapNodeClicked(SingleImageArea);
            yield return new WaitForSecondsRealtime(1.2f);
            yield return Shot("10_single_image_area");

            StoryTask first = hall.Tasks[0];
            Log(string.Format("area {0} '{1}': repairing '{2}' [{3}]",
                SingleImageArea, hall.Name, first.Label, RepairWork.Classify(first.Label)));
            StoryRepair.Perform(SingleImageArea, first.Index,
                () => { if (AreaScreen.Instance != null) AreaScreen.Instance.Refresh(); });

            yield return new WaitForSecondsRealtime(1.5f);
            yield return Shot("11_single_working");
            yield return new WaitForSecondsRealtime(1.3f);
            yield return Shot("12_single_reveal");

            float guard2 = 0f;
            while (RepairCutscene.IsPlaying && guard2 < 8f)
            {
                guard2 += Time.unscaledDeltaTime;
                yield return null;
            }

            Log("demo complete");
        }

        private IEnumerator Shot(string name)
        {
            // End of frame, or the capture catches the UI mid-build.
            yield return new WaitForEndOfFrame();

            string path = Path.Combine(_shotDir, string.Format("{0:00}_{1}.png", ++_shot, name));
            StoryWindow.Capture(path);

            // CaptureScreenshot is asynchronous; give it a frame to land.
            yield return new WaitForSecondsRealtime(0.6f);
            Log("shot -> " + Path.GetFileName(path));
        }

        private static void Log(string message)
        {
            Debug.Log("[StoryDemo] " + message);
        }
    }
}
