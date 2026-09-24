using PlayerPrefs = RoyalThrone.Testing.RoyalSave;
// // ©2015 - 2026 Candy Smith
// // All rights reserved
// // Redistribution of this software is strictly not allowed.
// // Copy of this software can be obtained from unity asset store only.
// // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// // IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// // FITNESS FOR A PARTICULAR PURPOSE AND NON-INFRINGEMENT. IN NO EVENT SHALL THE
// // AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// // LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// // OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// // THE SOFTWARE.

using System;
using UnityEngine;

namespace JellyGarden.Scripts.System
{
    public static class DateTimeManager
    {
        // Dates are stored in the invariant round-trip format. Older saves used the device culture's format, which
        // DateTime.Parse rejects after a language/region change (it threw in InitScript.Awake on every launch).
        // A value that cannot be read in either form counts as "no date" instead of throwing.
        static bool TryRead(string key, out DateTime date)
        {
            var s = PlayerPrefs.GetString(key);
            date = DateTime.MinValue;
            if (string.IsNullOrEmpty(s)) return false;
            return DateTime.TryParse(s, global::System.Globalization.CultureInfo.InvariantCulture, global::System.Globalization.DateTimeStyles.RoundtripKind, out date)
                || DateTime.TryParse(s, global::System.Globalization.CultureInfo.CurrentCulture, global::System.Globalization.DateTimeStyles.None, out date);
        }

        public static bool IsDayPassed(string dateTime)
        {
            if (TryRead(dateTime, out var d))
            {
                if (DateTime.Now.Subtract(d).Days >= 1)
                {
                    PlayerPrefs.SetInt(dateTime, 0);
                    return true;
                }
                return false;
            }

            return true;
        }
        public static bool IsMinutesPassed(string dateTime)
        {
            if (TryRead(dateTime, out var d))
            {
                if (DateTime.Now.Subtract(d).Minutes >= 1)
                {
                    PlayerPrefs.SetInt(dateTime, 0);
                    return true;
                }
                return false;
            }

            return true;
        }

        public static bool IsHourPassed(string dateTime)
        {
            if (TryRead(dateTime, out var d))
            {
                if (DateTime.Now.Subtract(d).Hours >= 1)
                {
                    PlayerPrefs.SetInt(dateTime, 0);
                    return true;
                }
                return false;
            }

            return true;
        }

        public static bool IsPeriodPassed(string dateTime)
        {
            switch (InitScript.Instance.dailyRewardedFrequencyTime)
            {
                case RewardedAdsTime.Day:
                    return IsDayPassed(dateTime);
                case RewardedAdsTime.Hour:
                    return IsHourPassed(dateTime);
                case RewardedAdsTime.Minute:
                    return IsMinutesPassed(dateTime);
            }
            return IsMinutesPassed(dateTime);
        }

        public static void SetDateTimeNow(string dateTime)
        {
            PlayerPrefs.SetString(dateTime, DateTime.Now.ToString("o", global::System.Globalization.CultureInfo.InvariantCulture));
        }

        public static DateTime GetLastDateTime(string dateTime)
        {
            return TryRead(dateTime, out var d) ? d : DateTime.Now;
        }

    }

    public enum RewardedAdsTime
    {
        Day,
        Hour,
        Minute
    }
}