using PlayerPrefs = RoyalThrone.Testing.RoyalSave;
using System;
using JellyGarden.Scripts;
using JellyGarden.Scripts.GUI;
using UnityEngine;

namespace RoyalThrone.Presentation
{
    // One restoration task: its name, star cost, and the restored object's patch with its place in the scene
    // (fractions of the scene, y down from the top), as written by Design/Areas/tools/build_areas.py.
    [Serializable]
    public sealed class RoyalAreaTask
    {
        public string name, patch;
        public int cost;
        public float x0, y0, x1, y1;
    }

    [Serializable]
    public sealed class RoyalAreaInfo
    {
        public int number, rewardCoins;
        public string name, subtitle, rewardBoost, before, after, thumbBefore, thumbAfter;
        public bool outdoor, placeholder;
        public RoyalAreaTask[] tasks;
        // An area without its generated scene is listed but cannot be restored yet ("coming soon").
        public bool Ready => !string.IsNullOrEmpty(before) && !string.IsNullOrEmpty(after) && tasks != null && tasks.Length > 0 &&
                             Array.TrueForAll(tasks, t => !string.IsNullOrEmpty(t.patch));
        public int TotalCost { get { int sum = 0; foreach (var t in tasks) sum += t.cost; return sum; } }
    }

    // Areas are restored in order, one task at a time, with the stars won in levels (as in Royal Match). Levels still award
    // their best 1-3 stars; what a task costs is recorded as spent, so the stars left to spend are earned minus spent.
    // Every value goes through RoyalSave, so the QA profile keeps its own areas.
    public static class RoyalAreas
    {
        [Serializable] sealed class CatalogFile { public RoyalAreaInfo[] areas; }
        const string Folder = "RoyalAreas/";
        public const string CurrentKey = "RoyalThrone.Area.Current", DoneKey = "RoyalThrone.Area.Done", SpentKey = "RoyalThrone.Area.Spent";
        static RoyalAreaInfo[] all;

        public static RoyalAreaInfo[] All
        {
            get
            {
                if (all != null) return all;
                var file = Resources.Load<TextAsset>(Folder + "areas");
                all = file ? JsonUtility.FromJson<CatalogFile>(file.text).areas ?? new RoyalAreaInfo[0] : new RoyalAreaInfo[0];
                return all;
            }
        }
        public static Sprite Art(string key) => string.IsNullOrEmpty(key) ? null : Resources.Load<Sprite>(Folder + key);

        /// <summary>1-based; one past the last area once every area is restored.</summary>
        public static int Current => Mathf.Clamp(PlayerPrefs.GetInt(CurrentKey, 1), 1, All.Length + 1);
        public static RoyalAreaInfo CurrentArea => Current <= All.Length ? All[Current - 1] : null;
        public static bool AllRestored => Current > All.Length;
        public static int Spent => Mathf.Max(0, PlayerPrefs.GetInt(SpentKey, 0));
        public static bool IsDone(int task) => task >= 0 && task < 31 && (PlayerPrefs.GetInt(DoneKey, 0) & (1 << task)) != 0;
        public static int DoneCount
        {
            get
            {
                var area = CurrentArea; if (area == null) return 0;
                int count = 0; for (int i = 0; i < area.tasks.Length; i++) if (IsDone(i)) count++;
                return count;
            }
        }
        public static bool Complete { get { var area = CurrentArea; return area != null && area.tasks.Length > 0 && DoneCount == area.tasks.Length; } }
        public static int Available(int earned) => Mathf.Max(0, earned - Spent);

        /// <summary>Raised after a task is paid for: area number, task index. Build animations can hook in here later.</summary>
        public static event Action<int, int> TaskBuilt;
        public static event Action<int> AreaCollected;

        public enum BuildResult { Built, AlreadyDone, NotEnoughStars, Unavailable }

        public static BuildResult Build(int task, int earned)
        {
            var area = CurrentArea;
            if (area == null || !area.Ready || task < 0 || task >= area.tasks.Length) return BuildResult.Unavailable;
            if (IsDone(task)) return BuildResult.AlreadyDone;
            int cost = area.tasks[task].cost;
            if (Available(earned) < cost) return BuildResult.NotEnoughStars;
            PlayerPrefs.SetInt(DoneKey, PlayerPrefs.GetInt(DoneKey, 0) | (1 << task));
            PlayerPrefs.SetInt(SpentKey, Spent + cost);
            PlayerPrefs.Save();
            TaskBuilt?.Invoke(area.number, task);
            return BuildResult.Built;
        }

        public struct Reward { public int area, coins; public BoostType boost; }

        /// <summary>Opens a restored area's chest (coins and one pre-level booster) and moves on to the next area.</summary>
        public static bool CollectChest(out Reward reward)
        {
            reward = default;
            var area = CurrentArea;
            if (area == null || !Complete) return false;
            reward.area = area.number; reward.coins = Mathf.Max(0, area.rewardCoins);
            reward.boost = Enum.TryParse(area.rewardBoost, out BoostType boost) ? boost : BoostType.None;
            if (reward.coins > 0)
            {
                if (InitScript.Instance) InitScript.Instance.AddGems(reward.coins);
                else { InitScript.Gems += reward.coins; PlayerPrefs.SetInt("Gems", InitScript.Gems); }
            }
            if (reward.boost != BoostType.None) PlayerPrefs.SetInt("" + reward.boost, PlayerPrefs.GetInt("" + reward.boost) + 1);
            PlayerPrefs.SetInt(CurrentKey, area.number + 1);
            PlayerPrefs.SetInt(DoneKey, 0);
            PlayerPrefs.Save();
            AreaCollected?.Invoke(area.number);
            return true;
        }

        /// <summary>The Home card and HUD read areas through the progress snapshot; levels no longer decide the area.</summary>
        public static void Fill(ref RoyalHomeProgress progress)
        {
            progress.available = Available(progress.stars);
            var area = CurrentArea;
            progress.area = Mathf.Min(Current, Mathf.Max(1, All.Length));
            progress.areaTotal = area != null ? area.tasks.Length : All.Length > 0 ? All[All.Length - 1].tasks.Length : 0;
            progress.areaCompleted = area != null ? DoneCount : progress.areaTotal;
        }

        /// <summary>The restored area the Home falls back to while the current one has no art yet.</summary>
        public static RoyalAreaInfo LastReadyBefore(int number)
        {
            for (int n = Mathf.Min(number, All.Length); n >= 1; n--) if (All[n - 1].Ready) return All[n - 1];
            return null;
        }
    }
}
