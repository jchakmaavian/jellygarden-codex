using System.IO;
using UnityEngine;

namespace JellyGarden.Scripts.Story
{
    public static class StoryIdentity
    {
        public const string Title = "Adventure of King Leo";
        // Desktop Unity derives the save folder from productName. Keep an existing
        // player's campaign where it was; mobile retains its stable bundle ID.
        public static string SaveDirectory
        {
            get
            {
                string current = Application.persistentDataPath;
                if (Application.isEditor || Application.platform == RuntimePlatform.OSXPlayer ||
                    Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.LinuxPlayer)
                {
                    string parent = Path.GetDirectoryName(current);
                    if (!string.IsNullOrEmpty(parent))
                    {
                        string legacy = Path.Combine(parent, "King Leo");
                        if (Directory.Exists(legacy)) return legacy;
                    }
                }
                return current;
            }
        }
    }
}
