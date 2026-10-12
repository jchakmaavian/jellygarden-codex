using UnityEngine;

namespace RoyalThrone.Testing
{
    // One forwarding boundary for the inherited PlayerPrefs API. Release builds
    // always use the original keys. QA writes never touch those keys.
    public static class RoyalSave
    {
        public static bool Testing { get; internal set; }
        public static bool BlockGameplayInput
        {
            get
            {
#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !ROYAL_DISABLE_QA
                var qa=RoyalQaService.Instance;
                return qa&&(qa.Visible||qa.Busy);
#else
                return false;
#endif
            }
        }
        // Royal Throne's reference-campaign profile is not ported (plan D8): one QA profile.
        const string Prefix = "RoyalThrone.QA.v1/";
        static string Index => Prefix + "keys";
        static string WriteKey(string key)
        {
            if (!Testing) return key;
            string entry = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(key));
            string index = UnityEngine.PlayerPrefs.GetString(Index, "|");
            if (!index.Contains("|" + entry + "|")) UnityEngine.PlayerPrefs.SetString(Index, index + entry + "|");
            UnityEngine.PlayerPrefs.SetInt(Prefix + "seen/" + key, 1);
            return Prefix + "data/" + key;
        }
        static string ReadKey(string key) => Testing && UnityEngine.PlayerPrefs.HasKey(Prefix + "seen/" + key) ? Prefix + "data/" + key : key;
        public static int GetInt(string key, int value = 0) => UnityEngine.PlayerPrefs.GetInt(ReadKey(key), value);
        public static float GetFloat(string key, float value = 0) => UnityEngine.PlayerPrefs.GetFloat(ReadKey(key), value);
        public static string GetString(string key, string value = "") => UnityEngine.PlayerPrefs.GetString(ReadKey(key), value);
        public static bool HasKey(string key) => UnityEngine.PlayerPrefs.HasKey(ReadKey(key));
        public static void SetInt(string key, int value) => UnityEngine.PlayerPrefs.SetInt(WriteKey(key), value);
        public static void SetFloat(string key, float value) => UnityEngine.PlayerPrefs.SetFloat(WriteKey(key), value);
        public static void SetString(string key, string value) => UnityEngine.PlayerPrefs.SetString(WriteKey(key), value);
        public static void DeleteKey(string key) => UnityEngine.PlayerPrefs.DeleteKey(WriteKey(key));
        public static void Save() => UnityEngine.PlayerPrefs.Save();
        public static void DeleteAll() { if (Testing) ResetTesting(); else UnityEngine.PlayerPrefs.DeleteAll(); }
        internal static void ResetTesting()
        {
            foreach (string entry in UnityEngine.PlayerPrefs.GetString(Index, "").Split('|'))
            {
                if (entry.Length == 0) continue;
                string key = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(entry));
                UnityEngine.PlayerPrefs.DeleteKey(Prefix + "seen/" + key);
                UnityEngine.PlayerPrefs.DeleteKey(Prefix + "data/" + key);
            }
            UnityEngine.PlayerPrefs.DeleteKey(Index); Save();
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession() { Testing = false; }
    }
}
