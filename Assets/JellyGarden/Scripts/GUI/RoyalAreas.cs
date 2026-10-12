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
        public string name, patch, icon;
        public int cost;
        public float x0, y0, x1, y1;
    }

    [Serializable]
    public sealed class RoyalAreaInfo
    {
        public int number, rewardCoins;
        public string name, subtitle, rewardBoost, before, after, thumbBefore, thumbAfter;
        public bool outdoor, placeholder, nativeMotion;
        public RoyalAreaTask[] tasks;
        // An area needs a scene to be restored; one without its generated scene borrows a placeholder (RoyalAreas.All).
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
        public const string CurrentKey = "RoyalThrone.Area.Current", DoneKey = "RoyalThrone.Area.Done", SpentKey = "RoyalThrone.Area.Spent",
            EnteredKey = "RoyalThrone.Area.Entered", BoughtKey = "RoyalThrone.Area.BoughtStars";
        static RoyalAreaInfo[] all;

        public static RoyalAreaInfo[] All
        {
            get
            {
                if (all != null) return all;
                var file = Resources.Load<TextAsset>(Folder + "areas");
                all = file ? JsonUtility.FromJson<CatalogFile>(file.text).areas ?? new RoyalAreaInfo[0] : new RoyalAreaInfo[0];
                BorrowPlaceholderArt(all);
                return all;
            }
        }
        public static Sprite Art(string key) => string.IsNullOrEmpty(key) ? null : Resources.Load<Sprite>(Folder + key);

        // An area whose scene is not generated yet borrows the first ready area's art as a placeholder scene (the user's
        // decision, 2026-09-29), so the player goes on to New Area, Unlock and its tasks, as in the reference, instead of
        // "Coming Soon". It keeps its own name, tasks and star costs; task i takes the art's patch i, repeating past its last.
        static void BorrowPlaceholderArt(RoyalAreaInfo[] areas)
        {
            var art = Array.Find(areas, a => a.Ready);
            if (art == null) return;
            foreach (var area in areas)
            {
                if (area.Ready || area.tasks == null || area.tasks.Length == 0) continue;
                area.placeholder = true;
                if (string.IsNullOrEmpty(area.before)) area.before = art.before;
                if (string.IsNullOrEmpty(area.after)) area.after = art.after;
                if (string.IsNullOrEmpty(area.thumbBefore)) area.thumbBefore = art.thumbBefore;
                if (string.IsNullOrEmpty(area.thumbAfter)) area.thumbAfter = art.thumbAfter;
                for (int i = 0; i < area.tasks.Length; i++)
                {
                    var task = area.tasks[i]; if (!string.IsNullOrEmpty(task.patch)) continue;
                    var from = art.tasks[i % art.tasks.Length];
                    task.patch = from.patch; task.x0 = from.x0; task.y0 = from.y0; task.x1 = from.x1; task.y1 = from.y1;
                }
            }
        }

        /// <summary>1-based; one past the last area once every area is restored.</summary>
        public const string LayoutVersionKey = "RoyalThrone.Area.LayoutVersion";
        public const string MigrationCreditKey = "RoyalThrone.Area.MigrationCredit";
        public static int Current
        {
            get { var catalog = All; MigrateNativeLayout(); return Mathf.Clamp(PlayerPrefs.GetInt(CurrentKey, 1), 1, catalog.Length + 1); }
        }
        // Preserve paid task count and already-complete areas when replacing the old 6/6/7 placeholders with 6/7/9 tasks.
        // No stars, coins, chest rewards or entered-area state are changed. RoyalSave keeps QA migration isolated.
        public static int MigratedDoneMask(int area, int mask)
        {
            return AreaMotion.AreaProgressMigration.Map(area, mask);
        }
        static void MigrateNativeLayout()
        {
            if (PlayerPrefs.GetInt(LayoutVersionKey, 0) >= 1) return;
            int current = PlayerPrefs.GetInt(CurrentKey, 1), mask = PlayerPrefs.GetInt(DoneKey, 0);
            PlayerPrefs.SetInt(MigrationCreditKey, AreaMotion.AreaProgressMigration.Credit(current, mask));
            if (current == 2 || current == 3) PlayerPrefs.SetInt(DoneKey, MigratedDoneMask(current, mask));
            PlayerPrefs.SetInt(LayoutVersionKey, 1); PlayerPrefs.Save();
        }
        public static RoyalAreaInfo CurrentArea => Current <= All.Length ? All[Current - 1] : null;
        public static bool AllRestored => Current > All.Length;
        public static int Spent => Mathf.Max(0, PlayerPrefs.GetInt(SpentKey, 0));
        public static int TaskCost(int task)
        {
            var area = CurrentArea;
            if (area == null || task < 0 || task >= area.tasks.Length) return 0;
            return Mathf.Max(0, area.tasks[task].cost - (task == NextTask ? PlayerPrefs.GetInt(MigrationCreditKey, 0) : 0));
        }
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
        public static int Available(int earned) => Mathf.Max(0, earned + Bought - Spent);
        /// <summary>Stars bought in the shop (RoyalShopPage: with coins or through Google Play), spent like earned ones.</summary>
        public static int Bought => Mathf.Max(0, PlayerPrefs.GetInt(BoughtKey, 0));
        public static void AddBoughtStars(int stars)
        {
            if (stars <= 0) return;
            PlayerPrefs.SetInt(BoughtKey, Bought + stars); PlayerPrefs.Save();
        }
        /// <summary>The next task to build: tasks are offered one at a time, in order, as in the reference. -1 when none is left.</summary>
        public static int NextTask
        {
            get
            {
                var area = CurrentArea; if (area == null) return -1;
                for (int i = 0; i < area.tasks.Length; i++) if (!IsDone(i)) return i;
                return -1;
            }
        }
        /// <summary>The last area the player has gone into. After a chest the next area waits behind its "New Area" card until
        /// Unlock (Enter); saves from before this key count as entered.</summary>
        public static int Entered => Mathf.Clamp(PlayerPrefs.GetInt(EnteredKey, Current), 0, All.Length + 1);
        public static bool AwaitingEntry => Entered < Current && !AllRestored;
        /// <summary>Unlock on the New Area card: the next area becomes the one shown and built.</summary>
        public static bool Enter()
        {
            if (!AwaitingEntry) return false;
            PlayerPrefs.SetInt(EnteredKey, Current); PlayerPrefs.Save();
            return true;
        }

        /// <summary>Raised after a task is paid for: area number, task index. Build animations can hook in here later.</summary>
        public static event Action<int, int> TaskBuilt;
        public static event Action<int> AreaCollected;

        public enum BuildResult { Built, AlreadyDone, NotEnoughStars, Unavailable }

        public static BuildResult Build(int task, int earned)
        {
            var area = CurrentArea;
            if (area == null || !area.Ready || task < 0 || task >= area.tasks.Length) return BuildResult.Unavailable;
            if (IsDone(task)) return BuildResult.AlreadyDone;
            // One task at a time, in order: a later task is not on offer yet.
            if (task != NextTask || AwaitingEntry) return BuildResult.Unavailable;
            int cost = TaskCost(task);
            if (Available(earned) < cost) return BuildResult.NotEnoughStars;
            PlayerPrefs.SetInt(DoneKey, PlayerPrefs.GetInt(DoneKey, 0) | (1 << task));
            PlayerPrefs.SetInt(SpentKey, Spent + cost);
            PlayerPrefs.SetInt(MigrationCreditKey, Mathf.Max(0, PlayerPrefs.GetInt(MigrationCreditKey, 0) - area.tasks[task].cost));
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
            PlayerPrefs.SetInt(MigrationCreditKey, 0);
            // The next area waits behind the Area card's "New Area" until the player unlocks it (Enter).
            PlayerPrefs.SetInt(EnteredKey, area.number);
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
