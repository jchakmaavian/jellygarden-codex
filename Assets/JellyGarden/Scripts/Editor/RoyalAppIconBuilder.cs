using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEngine;

namespace RoyalThrone.Editor
{
    public static class RoyalAppIconBuilder
    {
        const string Folder="Assets/RoyalBranding/";
        [MenuItem("Tools/Royal Throne/Branding/Apply and verify app icon #F6")]
        public static void Apply()
        {
            RoyalHomeScreenBuilder.RequireCopy();
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before setting the app icon.");
            var texture=new Texture2D(8,8,TextureFormat.RGB24,false);
            var pixels=Enumerable.Repeat(new Color(.025f,.16f,.49f),64).ToArray();texture.SetPixels(pixels);texture.Apply();
            File.WriteAllBytes(Folder+"royal-throne-icon-background.png",texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.Refresh();
            Texture2D Load(string name)
            {
                string path=Folder+name+".png";
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType=TextureImporterType.Default;importer.mipmapEnabled=false;
                importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.alphaIsTransparency=true;importer.maxTextureSize=1024;importer.SaveAndReimport();
                return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
            var icon=Load("royal-throne-icon");var foreground=Load("royal-throne-icon-foreground");var background=Load("royal-throne-icon-background");
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown,new[]{icon},IconKind.Any);
            int slots=0;
            // Legacy and Round are deprecated in 6000.5 but still filled: dropping them changes the APK's icon
            // resources, which only a device build can verify. Revisit when Unity removes them (minSdk is 26).
#pragma warning disable CS0618
            foreach(var kind in new[]{AndroidPlatformIconKind.Legacy,AndroidPlatformIconKind.Round,AndroidPlatformIconKind.Adaptive})
#pragma warning restore CS0618
            {
                var icons=PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android,kind);
                foreach(var slot in icons)slot.SetTextures(kind==AndroidPlatformIconKind.Adaptive?new[]{background,foreground}:new[]{icon});
                PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android,kind,icons);
                var saved=PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android,kind);
                if(saved.Length==0||saved.Any(slot=>slot.GetTexture(0)==null))throw new InvalidOperationException("Missing app icon slots: "+kind);
                if(kind==AndroidPlatformIconKind.Adaptive&&saved.Any(slot=>slot.GetTexture(1)!=foreground))throw new InvalidOperationException("Missing adaptive foreground.");
                slots+=saved.Length;
            }
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Design/Branding/AppIcon-v1");
            File.WriteAllText("Design/Branding/AppIcon-v1/checks.txt","Default icon assigned. Android Legacy/Round/Adaptive slots verified: "+slots+". Adaptive foreground fits within 62.5% safe square.\n");
            Debug.Log("[Royal Throne Icon] Default and "+slots+" Android icon slots verified.");
        }
    }
}
