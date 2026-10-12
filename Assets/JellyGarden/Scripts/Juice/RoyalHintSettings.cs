using PlayerPrefs = RoyalThrone.Testing.RoyalSave;
using System;
using UnityEngine;

namespace JellyGarden.Scripts.Juice
{
    /// <summary>Visual assistance only. The board solver still detects dead boards when hints are off.</summary>
    public static class RoyalHintSettings
    {
        public const string PreferenceKey = "RoyalThrone.ShowHints";
        public static event Action<bool> Changed;
        public static bool Enabled
        {
            get => PlayerPrefs.GetInt(PreferenceKey, 1) != 0;
            set
            {
                bool previous = Enabled;
                PlayerPrefs.SetInt(PreferenceKey, value ? 1 : 0);
                PlayerPrefs.Save();
                if (previous != value) Changed?.Invoke(value);
            }
        }
    }
}
