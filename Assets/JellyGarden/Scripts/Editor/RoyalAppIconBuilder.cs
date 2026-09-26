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
        static readonly Color Background=new Color(.025f,.16f,.49f);
        [MenuItem("Tools/Royal Throne/Branding/Apply and verify app icon #F6")]
        public static void Apply()
        {
            RoyalHomeScreenBuilder.RequireCopy();
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before setting the app icon.");
            var texture=new Texture2D(8,8,TextureFormat.RGB24,false);
            var pixels=Enumerable.Repeat(Background,64).ToArray();texture.SetPixels(pixels);texture.Apply();
            File.WriteAllBytes(Folder+"royal-throne-icon-background.png",texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
            WriteRoundIcon(Folder+"royal-throne-icon-foreground.png",Folder+"royal-throne-icon-round.png");
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
            var round=Load("royal-throne-icon-round");
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown,new[]{icon},IconKind.Any);
            int slots=0;
            // Legacy and Round are deprecated in 6000.5 but still filled: dropping them changes the APK's icon
            // resources, which only a device build can verify. Revisit when Unity removes them (minSdk is 26).
#pragma warning disable CS0618
            foreach(var kind in new[]{AndroidPlatformIconKind.Legacy,AndroidPlatformIconKind.Round,AndroidPlatformIconKind.Adaptive})
            {
                var icons=PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android,kind);
                foreach(var slot in icons)slot.SetTextures(kind==AndroidPlatformIconKind.Adaptive?new[]{background,foreground}:kind==AndroidPlatformIconKind.Round?new[]{round}:new[]{icon});
                PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android,kind,icons);
                var saved=PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android,kind);
                if(saved.Length==0||saved.Any(slot=>slot.GetTexture(0)==null))throw new InvalidOperationException("Missing app icon slots: "+kind);
                if(kind==AndroidPlatformIconKind.Adaptive&&saved.Any(slot=>slot.GetTexture(1)!=foreground))throw new InvalidOperationException("Missing adaptive foreground.");
                if(kind==AndroidPlatformIconKind.Round&&saved.Any(slot=>slot.GetTexture(0)!=round))throw new InvalidOperationException("Missing round icon.");
                slots+=saved.Length;
            }
#pragma warning restore CS0618
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Design/Branding/AppIcon-v1");
            File.WriteAllText("Design/Branding/AppIcon-v1/checks.txt","Default icon assigned. Android Legacy/Round/Adaptive slots verified: "+slots+". "+SafeZone(Folder+"royal-throne-icon-foreground.png")+" Round slots use the adaptive layers in a circle.\n");
            Debug.Log("[Royal Throne Icon] Default and "+slots+" Android icon slots verified.");
        }

        // Adaptive layers are 108 dp; launchers show the central 72 dp through their mask, and only what lies inside the 66 dp
        // safe circle shows under every mask. Measure the foreground's visible pixels (alpha above 16) against both, and fail if
        // any reaches past 36 dp, where a circular mask would cut it.
        static string SafeZone(string foregroundPath)
        {
            var fg=new Texture2D(2,2,TextureFormat.RGBA32,false);fg.LoadImage(File.ReadAllBytes(foregroundPath));
            int n=fg.width;float dp=n/108f,c=(n-1)*.5f,far=0,ink=0,outside=0;var px=fg.GetPixels32();
            for(int y=0;y<n;y++)for(int x=0;x<n;x++)
            {
                float a=px[y*n+x].a/255f;if(a<=0)continue;
                float r=Mathf.Sqrt((x-c)*(x-c)+(y-c)*(y-c))/dp;ink+=a;if(r>33)outside+=a;if(px[y*n+x].a>16)far=Mathf.Max(far,r);
            }
            UnityEngine.Object.DestroyImmediate(fg);
            if(far>36)throw new InvalidOperationException("Adaptive foreground reaches "+far.ToString("F2")+" dp from the centre; a circular launcher mask shows 36 dp.");
            return "Adaptive foreground: farthest visible pixel "+far.ToString("F2")+" dp from the centre (a circular mask shows 36 dp); "+(100*outside/ink).ToString("F1")+"% of its ink lies outside the 66 dp safe circle.";
        }

        // Launchers that ask for the Round icon (Android 7.1) show it as it is, so a square icon stays square there. Build it from
        // the adaptive layers the way Android 8+ launchers show them: the foreground over the background, the central 72 of the
        // 108 dp, in an anti-aliased circle.
        static void WriteRoundIcon(string foregroundPath,string outputPath)
        {
            var fg=new Texture2D(2,2,TextureFormat.RGBA32,false);fg.LoadImage(File.ReadAllBytes(foregroundPath));
            const int size=1024;const float margin=18f/108f,span=72f/108f,radius=size*.5f;
            var output=new Texture2D(size,size,TextureFormat.RGBA32,false);var pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                var c=fg.GetPixelBilinear(margin+span*(x+.5f)/size,margin+span*(y+.5f)/size);
                var px=Color.Lerp(Background,new Color(c.r,c.g,c.b,1),c.a);
                px.a=Mathf.Clamp01(radius-Vector2.Distance(new Vector2(x+.5f,y+.5f),new Vector2(radius,radius))+.5f);
                pixels[y*size+x]=px;
            }
            output.SetPixels(pixels);output.Apply();File.WriteAllBytes(outputPath,output.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(fg);UnityEngine.Object.DestroyImmediate(output);
        }
    }
}
