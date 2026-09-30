using PlayerPrefs = RoyalThrone.Testing.RoyalSave;
using System.Collections.Generic;
using System.Text;
using RoyalThrone.Presentation;
using UnityEngine;

namespace RoyalThrone
{
    // The player's profile (2026-09-30), kept on this device only through RoyalSave, so a QA session works on its own copy.
    // - What the player chooses: a name, an avatar, a frame and a name style (the Edit Profile card, RoyalProfilePage).
    // - What the save already knows: levels completed, areas restored, scores.
    // - Two counts the save did not keep before, counted from this update on: levels won on their first attempt
    //   (LevelManager.PrepareGame / CommitWin) and power-ups made by matches (Item's DestroyCor, while the level is played).
    // - The month the profile was first seen, for "playing since".
    public static class RoyalProfile
    {
        const string Prefix = "RoyalThrone.Profile.";
        public const string NameKey = Prefix + "Name", AvatarKey = Prefix + "Avatar", FrameKey = Prefix + "Frame", StyleKey = Prefix + "NameStyle";
        public const string SinceKey = Prefix + "Since", FirstTryKey = Prefix + "FirstTryWins", PowerupsKey = Prefix + "PowerupsMade";
        public const int NameMin = 3, NameMax = 14;
        public const string DefaultAvatar = "king";

        /// <summary>Raised after the profile is saved (the page and Home's portrait redraw).</summary>
        public static event System.Action Changed;

        // ---- Choices ---------------------------------------------------------------------------------------------------

        /// <summary>The player's name; a new profile starts as "Player" and four digits, as in the reference.</summary>
        public static string Name
        {
            get
            {
                string name = PlayerPrefs.GetString(NameKey, "");
                if (!string.IsNullOrEmpty(name)) return name;
                name = "Player " + Random.Range(1000, 10000);
                PlayerPrefs.SetString(NameKey, name); PlayerPrefs.Save();
                return name;
            }
        }

        public static string Avatar { get { string id = PlayerPrefs.GetString(AvatarKey, DefaultAvatar); return RoyalProfileArt.Find(id) != null ? id : DefaultAvatar; } }
        public static int Frame => Mathf.Clamp(PlayerPrefs.GetInt(FrameKey, 0), 0, RoyalProfileArt.Frames.Length - 1);
        public static int NameStyle => Mathf.Clamp(PlayerPrefs.GetInt(StyleKey, 0), 0, RoyalProfileArt.NameStyles.Length - 1);

        /// <summary>"MM/yyyy" of the month this profile was first seen (the save kept no install date before).</summary>
        public static string Since
        {
            get
            {
                string since = PlayerPrefs.GetString(SinceKey, "");
                if (!string.IsNullOrEmpty(since)) return since;
                since = System.DateTime.Now.ToString("MM/yyyy", System.Globalization.CultureInfo.InvariantCulture);
                PlayerPrefs.SetString(SinceKey, since); PlayerPrefs.Save();
                return since;
            }
        }

        /// <summary>A name as it would be kept: letters, digits, spaces, _ - . only, single spaces, trimmed, at most NameMax.</summary>
        public static string Clean(string name)
        {
            if (name == null) return "";
            var sb = new StringBuilder();
            foreach (char ch in name.Trim())
            {
                bool ok = char.IsLetterOrDigit(ch) || ch == '_' || ch == '-' || ch == '.' || ch == ' ';
                if (!ok) continue;
                if (ch == ' ' && (sb.Length == 0 || sb[sb.Length - 1] == ' ')) continue;
                sb.Append(ch);
                if (sb.Length >= NameMax) break;
            }
            return sb.ToString().Trim();
        }

        public static bool Valid(string name) { string clean = Clean(name); return clean.Length >= NameMin && clean == name; }

        /// <summary>Keeps the Edit Profile card's choices; an invalid name keeps the old one.</summary>
        public static void Save(string name, string avatar, int frame, int style)
        {
            string clean = Clean(name);
            if (clean.Length >= NameMin) PlayerPrefs.SetString(NameKey, clean);
            if (RoyalProfileArt.Find(avatar) != null) PlayerPrefs.SetString(AvatarKey, avatar);
            PlayerPrefs.SetInt(FrameKey, Mathf.Clamp(frame, 0, RoyalProfileArt.Frames.Length - 1));
            PlayerPrefs.SetInt(StyleKey, Mathf.Clamp(style, 0, RoyalProfileArt.NameStyles.Length - 1));
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        // ---- Counts kept from this update on ---------------------------------------------------------------------------

        public static int FirstTryWins => Mathf.Max(0, PlayerPrefs.GetInt(FirstTryKey, 0));
        public static int PowerupsMade => Mathf.Max(0, PlayerPrefs.GetInt(PowerupsKey, 0));
        static bool firstAttempt;

        /// <summary>A level attempt begins (LevelManager.PrepareGame, once per attempt; a Continue is the same attempt). It is a
        /// first attempt when the level was never won and never tried since this count began.</summary>
        public static void AttemptStarted(int level, bool alreadyWon)
        {
            string key = Prefix + "Tried." + level.ToString("000");
            bool tried = PlayerPrefs.GetInt(key, 0) != 0;
            firstAttempt = !alreadyWon && !tried;
            if (!tried) PlayerPrefs.SetInt(key, 1);
        }

        /// <summary>The win is committed (LevelManager.CommitWin, once per attempt). `firstWin`: the level had not been won before.</summary>
        public static void Won(bool firstWin)
        {
            if (firstWin && firstAttempt) PlayerPrefs.SetInt(FirstTryKey, FirstTryWins + 1);
            firstAttempt = false;
        }

        /// <summary>A match made a power-up (a Rocket, TNT, Propeller or Color Ball) while the level is played.</summary>
        public static void PowerupMade() => PlayerPrefs.SetInt(PowerupsKey, PowerupsMade + 1);

        // ---- What the save already knows -------------------------------------------------------------------------------

        /// <summary>Areas restored: every area before the current one, and the current one once all its tasks are done.</summary>
        public static int AreasCompleted
        {
            get
            {
                int total = RoyalAreas.All.Length;
                int done = RoyalAreas.Current - 1 + (RoyalAreas.Complete ? 1 : 0);
                return Mathf.Clamp(done, 0, total);
            }
        }

        /// <summary>The best score kept for each level in `levels` (LevelManager.CommitWin keeps a level's best), summed, and the best of them.</summary>
        public static void Scores(IEnumerable<int> levels, out long total, out int best)
        {
            total = 0; best = 0;
            if (levels == null) return;
            foreach (int level in levels)
            {
                int score = Mathf.Max(0, PlayerPrefs.GetInt("Score" + level, 0));
                total += score;
                if (score > best) best = score;
            }
        }
    }
}
