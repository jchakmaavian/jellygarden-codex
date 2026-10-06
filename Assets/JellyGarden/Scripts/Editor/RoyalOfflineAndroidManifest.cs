#if UNITY_ANDROID
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor.Android;
using UnityEditor.Build;

namespace RoyalThrone.Editor
{
    /// <summary>Prevent dormant packages from adding network access to the offline Android player.</summary>
    public sealed class RoyalOfflineAndroidManifest : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 10000;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            // Unity passes the unityLibrary module; launcher is the higher-priority merge input.
            string launcher = Path.Combine(Path.GetDirectoryName(path), "launcher", "src", "main", "AndroidManifest.xml");
            if (!File.Exists(launcher))
                throw new BuildFailedException("Royal Throne offline permission check could not find the launcher manifest.");
            RemoveNetworkPermissions(launcher);
        }

        public static void RemoveNetworkPermissions(string path)
        {
            var document = XDocument.Load(path);
            XNamespace android = "http://schemas.android.com/apk/res/android";
            XNamespace tools = "http://schemas.android.com/tools";
            document.Root.SetAttributeValue(XNamespace.Xmlns + "tools", tools.NamespaceName);
            foreach (string permission in new[] { "android.permission.INTERNET", "android.permission.ACCESS_NETWORK_STATE",
                "android.permission.ACCESS_WIFI_STATE", "com.google.android.gms.permission.AD_ID" })
            {
                document.Root.Elements("uses-permission").Where(element => (string)element.Attribute(android + "name") == permission).Remove();
                document.Root.AddFirst(new XElement("uses-permission", new XAttribute(android + "name", permission),
                    new XAttribute(tools + "node", "remove")));
            }
            document.Save(path);
        }
    }
}
#endif
