using System;
using UnityEngine;

namespace RoyalThrone.Presentation
{
    public struct RoyalHomeProgress
    {
        public int nextLevel, completed, stars, area, areaCompleted, areaTotal;

        public static RoyalHomeProgress Read(int[] levels, Func<int, int> starsForLevel)
        {
            var result = new RoyalHomeProgress { nextLevel = 1, area = 1 };
            if (levels == null || levels.Length == 0) return result;
            result.nextLevel = levels[0];
            foreach (int level in levels)
            {
                int stars = Mathf.Clamp(starsForLevel(level), 0, 3);
                result.stars += stars;
                if (stars > 0) result.completed++;
                // Match the existing map's unlock rule, including restored non-contiguous saves.
                if (level == 1 || starsForLevel(level - 1) > 0) result.nextLevel = level;
            }
            result.area = (result.nextLevel - 1) / 6 + 1;
            foreach (int level in levels)
                if ((level - 1) / 6 + 1 == result.area)
                {
                    result.areaTotal++;
                    if (starsForLevel(level) > 0) result.areaCompleted++;
                }
            return result;
        }
    }
}
