// King Leo story layer — the player's place in the story.
//
// One JSON file at Application.persistentDataPath/story_save.json, written to a
// temp file and atomically renamed so a kill mid-write cannot corrupt a save.
// Deliberately separate from the kit's PlayerPrefs: the story owns area, task
// and star state, and PlayerPrefs keeps owning audio settings and the kit's own
// bookkeeping.

using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace JellyGarden.Scripts.Story
{
    public static class StoryProgress
    {
        public const int SaveVersion = 1;
        private const string FileName = "story_save.json";

        private static bool _loaded;
        private static bool _dirty;

        private static int _version = SaveVersion;
        private static int _currentArea = 1;
        private static bool _openingSeen;

        /// <summary>Unspent stars, per area. Stars do not travel between areas.</summary>
        private static readonly Dictionary<int, int> _areaStars = new Dictionary<int, int>();

        private static readonly Dictionary<int, HashSet<int>> _tasksCompleted =
            new Dictionary<int, HashSet<int>>();

        private static readonly HashSet<int> _areasCompleted = new HashSet<int>();
        private static readonly HashSet<int> _cluesSeen = new HashSet<int>();
        private static readonly HashSet<int> _cutscenesSeen = new HashSet<int>();
        private static readonly HashSet<int> _levelsBeaten = new HashSet<int>();
        private static readonly HashSet<string> _scenesSeen = new HashSet<string>();

        // Tests use a separate temporary directory and never reset a player's save.
        public static string SaveDirectoryOverride;

        public static string SavePath
        {
            get { return Path.Combine(SaveDirectoryOverride ?? StoryIdentity.SaveDirectory, StoryCampaign.SaveFileName); }
        }

        public static int CurrentArea
        {
            get { Load(); return _currentArea; }
            set
            {
                Load();
                if (!StoryCampaign.IsReady(value)) return;
                if (_currentArea == value) return;
                _currentArea = value;
                MarkDirty();
            }
        }

        public static bool OpeningSeen
        {
            get { Load(); return _openingSeen; }
        }

        #region Queries

        public static int StarsIn(int areaId)
        {
            Load();
            int stars;
            return _areaStars.TryGetValue(areaId, out stars) ? stars : 0;
        }

        public static bool IsTaskComplete(int areaId, int taskIndex)
        {
            Load();
            HashSet<int> done;
            return _tasksCompleted.TryGetValue(areaId, out done) && done.Contains(taskIndex);
        }

        public static int TasksCompletedIn(int areaId)
        {
            Load();
            HashSet<int> done;
            return _tasksCompleted.TryGetValue(areaId, out done) ? done.Count : 0;
        }

        public static bool IsAreaComplete(int areaId)
        {
            Load();
            return _areasCompleted.Contains(areaId);
        }

        /// <summary>Area 1 is always open. Every other area waits on the one before it.</summary>
        public static bool IsAreaUnlocked(int areaId)
        {
            Load();
            if (!StoryCampaign.IsReady(areaId)) return false;
            if (areaId <= 1) return true;
            return _areasCompleted.Contains(areaId - 1);
        }

        public static bool IsClueSeen(int areaId)
        {
            Load();
            return _cluesSeen.Contains(areaId);
        }

        public static bool IsCutsceneSeen(int areaId)
        {
            Load();
            return _cutscenesSeen.Contains(areaId);
        }

        public static bool IsLevelBeaten(int levelNumber)
        {
            Load();
            return _levelsBeaten.Contains(levelNumber);
        }

        public static int LevelsBeatenCount
        {
            get { Load(); return _levelsBeaten.Count; }
        }

        /// <summary>The furthest unlocked area — where the map should place the player.</summary>
        public static int FurthestUnlockedArea
        {
            get
            {
                Load();
                int furthest = 1;
                for (int i = 1; i <= StoryDatabase.AreaCount; i++)
                    if (IsAreaUnlocked(i)) furthest = i;
                return furthest;
            }
        }

        /// <summary>Can this task be paid for right now?</summary>
        public static bool CanCompleteTask(int areaId, int taskIndex)
        {
            Load();
            if (IsTaskComplete(areaId, taskIndex)) return false;
            if (StoryCampaign.IsTreasureHunt)
            {
                if (!IsAreaUnlocked(areaId)) return false;
                StoryTask next = NextTask(areaId);
                if (next == null || next.Index != taskIndex) return false;
            }

            StoryArea area = StoryDatabase.GetArea(areaId);
            if (area == null) return false;

            StoryTask task = FindTask(area, taskIndex);
            if (task == null) return false;

            return StarsIn(areaId) >= task.Stars;
        }

        public static StoryTask FindTask(StoryArea area, int taskIndex)
        {
            if (area == null) return null;
            for (int i = 0; i < area.Tasks.Count; i++)
                if (area.Tasks[i].Index == taskIndex) return area.Tasks[i];
            return null;
        }

        /// <summary>The next task the player should be looking at, or null when the area is done.</summary>
        public static StoryTask NextTask(int areaId)
        {
            StoryArea area = StoryDatabase.GetArea(areaId);
            if (area == null) return null;
            for (int i = 0; i < area.Tasks.Count; i++)
                if (!IsTaskComplete(areaId, area.Tasks[i].Index)) return area.Tasks[i];
            return null;
        }

        #endregion

        #region Mutations

        public static void AddStar(int areaId, int count = 1)
        {
            Load();
            if (count <= 0) return;
            _areaStars[areaId] = StarsIn(areaId) + count;
            MarkDirty();
        }

        public static void MarkLevelBeaten(int levelNumber)
        {
            Load();
            if (_levelsBeaten.Add(levelNumber)) MarkDirty();
        }

        /// <summary>Record the first win and its star as one persistence mutation.</summary>
        public static bool RecordLevelWin(int levelNumber, int areaId)
        {
            Load();
            if (StoryLevelPlan.AreaOf(levelNumber) != areaId || !IsAreaUnlocked(areaId)) return false;
            if (!_levelsBeaten.Add(levelNumber)) return false;
            _areaStars[areaId] = StarsIn(areaId) + 1;
            MarkDirty();
            return true;
        }

        public static bool IsSceneSeen(string id)
        {
            Load();
            return _scenesSeen.Contains(id);
        }

        public static void MarkSceneSeen(string id)
        {
            Load();
            if (string.IsNullOrEmpty(id)) return;
            bool changed = _scenesSeen.Add(id);
            if (id == StoryCampaign.OpeningScene && !_openingSeen)
            {
                _openingSeen = true;
                changed = true;
            }
            if (changed) MarkDirty();
        }

        /// <summary>
        /// Spends the task's stars and records it. Returns false when the player
        /// cannot afford it or the task is already done — callers can use the
        /// return value instead of pre-checking.
        /// </summary>
        public static bool CompleteTask(int areaId, int taskIndex)
        {
            Load();
            if (!CanCompleteTask(areaId, taskIndex)) return false;

            StoryArea area = StoryDatabase.GetArea(areaId);
            StoryTask task = FindTask(area, taskIndex);

            _areaStars[areaId] = StarsIn(areaId) - task.Stars;

            HashSet<int> done;
            if (!_tasksCompleted.TryGetValue(areaId, out done))
            {
                done = new HashSet<int>();
                _tasksCompleted[areaId] = done;
            }
            done.Add(taskIndex);

            if (done.Count >= area.Tasks.Count)
                _areasCompleted.Add(areaId);

            MarkDirty();
            return true;
        }

        public static void MarkClueSeen(int areaId)
        {
            Load();
            if (_cluesSeen.Add(areaId)) MarkDirty();
        }

        public static void MarkCutsceneSeen(int areaId)
        {
            Load();
            if (_cutscenesSeen.Add(areaId)) MarkDirty();
        }

        public static void MarkOpeningSeen()
        {
            Load();
            if (_openingSeen) return;
            _openingSeen = true;
            MarkDirty();
        }

        public static void ResetAll()
        {
            ClearInMemory();
            _loaded = true;
            _dirty = true;
            Flush();
        }

        /// <summary>Drops every in-memory field back to a fresh-save state.</summary>
        private static void ClearInMemory()
        {
            _version = SaveVersion;
            _currentArea = 1;
            _openingSeen = false;
            _areaStars.Clear();
            _tasksCompleted.Clear();
            _areasCompleted.Clear();
            _cluesSeen.Clear();
            _cutscenesSeen.Clear();
            _levelsBeaten.Clear();
            _scenesSeen.Clear();
        }

        /// <summary>
        /// Forgets everything held in memory and re-reads the file. Used by the
        /// save tests to prove state actually round-trips through disk rather
        /// than surviving because it never left RAM.
        /// </summary>
        public static void ReloadFromDisk()
        {
            ClearInMemory();
            _loaded = false;
            _dirty = false;
            Load();
        }

        #endregion

        #region Persistence

        public static void Load()
        {
            if (_loaded) return;
            _loaded = true;

            string path = SavePath;
            if (!File.Exists(path)) return;

            string text;
            try
            {
                text = File.ReadAllText(path, Encoding.UTF8);
            }
            catch (IOException e)
            {
                Debug.LogWarning("[Story] Could not read save, starting fresh: " + e.Message);
                return;
            }

            JsonValue root = JsonValue.Parse(text);
            if (root.IsNull)
            {
                Debug.LogWarning("[Story] Save file was unreadable, starting fresh.");
                return;
            }

            _version = root["version"].AsInt(SaveVersion);
            _currentArea = Mathf.Max(1, root["currentArea"].AsInt(1));
            if (!StoryCampaign.IsReady(_currentArea)) _currentArea = 1;
            _openingSeen = root["openingSeen"].AsBool();

            foreach (JsonValue entry in root["areaStars"].Items)
                _areaStars[entry["area"].AsInt()] = entry["stars"].AsInt();

            foreach (JsonValue entry in root["tasksCompleted"].Items)
            {
                int areaId = entry["area"].AsInt();
                var done = new HashSet<int>();
                foreach (JsonValue task in entry["tasks"].Items) done.Add(task.AsInt());
                _tasksCompleted[areaId] = done;
            }

            ReadIntSet(root["areasCompleted"], _areasCompleted);
            ReadIntSet(root["cluesSeen"], _cluesSeen);
            ReadIntSet(root["cutscenesSeen"], _cutscenesSeen);
            ReadIntSet(root["levelsBeaten"], _levelsBeaten);
            foreach (JsonValue scene in root["scenesSeen"].Items) _scenesSeen.Add(scene.AsString());
        }

        private static void ReadIntSet(JsonValue array, HashSet<int> into)
        {
            foreach (JsonValue item in array.Items) into.Add(item.AsInt());
        }

        /// <summary>
        /// Debounced: mutations happen in bursts (a repair spends stars, marks a
        /// task and may complete an area), so the write is deferred to end of
        /// frame and coalesced into one atomic rename.
        /// </summary>
        private static void MarkDirty()
        {
            if (_dirty) return;
            _dirty = true;
            StoryProgressWriter.Schedule();
        }

        /// <summary>Writes immediately if anything is pending. Safe to call at any time.</summary>
        public static void Flush()
        {
            if (!_dirty) return;
            _dirty = false;
            Write();
        }

        private static void Write()
        {
            var sb = new StringBuilder(1024);
            sb.Append("{\n");
            sb.Append("  \"version\": ").Append(_version).Append(",\n");
            sb.Append("  \"currentArea\": ").Append(_currentArea).Append(",\n");
            sb.Append("  \"openingSeen\": ").Append(_openingSeen ? "true" : "false").Append(",\n");

            sb.Append("  \"areaStars\": [");
            bool first = true;
            foreach (KeyValuePair<int, int> pair in _areaStars)
            {
                if (pair.Value == 0) continue;
                if (!first) sb.Append(", ");
                first = false;
                sb.Append("{\"area\": ").Append(pair.Key)
                  .Append(", \"stars\": ").Append(pair.Value).Append('}');
            }
            sb.Append("],\n");

            sb.Append("  \"tasksCompleted\": [");
            first = true;
            foreach (KeyValuePair<int, HashSet<int>> pair in _tasksCompleted)
            {
                if (pair.Value.Count == 0) continue;
                if (!first) sb.Append(", ");
                first = false;
                sb.Append("{\"area\": ").Append(pair.Key).Append(", \"tasks\": [");
                bool innerFirst = true;
                foreach (int task in pair.Value)
                {
                    if (!innerFirst) sb.Append(", ");
                    innerFirst = false;
                    sb.Append(task);
                }
                sb.Append("]}");
            }
            sb.Append("],\n");

            AppendIntSet(sb, "areasCompleted", _areasCompleted, true);
            AppendIntSet(sb, "cluesSeen", _cluesSeen, true);
            AppendIntSet(sb, "cutscenesSeen", _cutscenesSeen, true);
            AppendIntSet(sb, "levelsBeaten", _levelsBeaten, true);
            sb.Append("  \"scenesSeen\": [");
            first = true;
            foreach (string scene in _scenesSeen)
            {
                if (!first) sb.Append(", ");
                first = false;
                sb.Append('"').Append(scene.Replace("\\", "\\\\").Replace("\"", "\\\"")).Append('"');
            }
            sb.Append("]\n");
            sb.Append("}\n");

            string path = SavePath;
            string temp = path + ".tmp";
            try
            {
                File.WriteAllText(temp, sb.ToString(), Encoding.UTF8);
                if (File.Exists(path))
                    File.Replace(temp, path, null);
                else
                    File.Move(temp, path);
            }
            catch (IOException e)
            {
                Debug.LogWarning("[Story] Save failed: " + e.Message);
            }
        }

        private static void AppendIntSet(StringBuilder sb, string key, HashSet<int> set, bool comma)
        {
            sb.Append("  \"").Append(key).Append("\": [");
            bool first = true;
            foreach (int value in set)
            {
                if (!first) sb.Append(", ");
                first = false;
                sb.Append(value);
            }
            sb.Append(']').Append(comma ? ",\n" : "\n");
        }

        #endregion
    }

    /// <summary>
    /// Hidden driver that turns StoryProgress's dirty flag into one write per
    /// frame, and guarantees a final write when the app loses focus or quits.
    /// </summary>
    [DefaultExecutionOrder(10000)]
    internal sealed class StoryProgressWriter : MonoBehaviour
    {
        private static StoryProgressWriter _instance;

        internal static void Schedule()
        {
            if (!Application.isPlaying)
            {
                // Edit mode and batchmode have no frame loop to debounce onto,
                // so there is nothing to defer to — write it now.
                StoryProgress.Flush();
                return;
            }

            if (_instance != null) return;

            var host = new GameObject("~StoryProgressWriter");
            host.hideFlags = HideFlags.HideAndDontSave;
            StoryObjects.Persist(host);
            _instance = host.AddComponent<StoryProgressWriter>();
        }

        private void LateUpdate()
        {
            StoryProgress.Flush();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) StoryProgress.Flush();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused) StoryProgress.Flush();
        }

        private void OnApplicationQuit()
        {
            StoryProgress.Flush();
        }
    }
}
