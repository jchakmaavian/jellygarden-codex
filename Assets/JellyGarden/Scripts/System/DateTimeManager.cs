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
        public static bool IsDayPassed(string dateTime)
        {
            var s = PlayerPrefs.GetString(dateTime);
            if (s != "")
            {
                var d = DateTime.Parse(s);
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
            var s = PlayerPrefs.GetString(dateTime);
            if (s != "")
            {
                var d = DateTime.Parse(s);
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
            var s = PlayerPrefs.GetString(dateTime);
            if (s != "")
            {
                var d = DateTime.Parse(s);
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
            PlayerPrefs.SetString(dateTime, DateTime.Now.ToString());
        }

        public static DateTime GetLastDateTime(string dateTime)
        {
            return DateTime.Parse(PlayerPrefs.GetString(dateTime, DateTime.Now.ToString()));
        }

    }

    public enum RewardedAdsTime
    {
        Day,
        Hour,
        Minute
    }
}