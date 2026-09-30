using UnityEditor;
using UnityEngine;

namespace RoyalThrone.Editor
{
    // These PNGs are mesh textures, not flattened UI patches. Their original UV geometry must remain intact.
    public sealed class RoyalAreaMotionImport : AssetPostprocessor
    {
        const string Root="Assets/JellyGarden/Resources/RoyalAreas/NativeMotion/";
        void OnPreprocessTexture()
        {
            if(!assetPath.StartsWith(Root))return;
            var t=(TextureImporter)assetImporter;bool icon=assetPath.Contains("/UI/");
            t.textureType=icon?TextureImporterType.Sprite:TextureImporterType.Default;t.spriteImportMode=SpriteImportMode.Single;
            t.alphaSource=TextureImporterAlphaSource.FromInput;t.alphaIsTransparency=true;t.mipmapEnabled=false;t.isReadable=false;t.sRGBTexture=true;t.wrapMode=TextureWrapMode.Clamp;t.filterMode=FilterMode.Bilinear;t.maxTextureSize=4096;t.textureCompression=TextureImporterCompression.Uncompressed;
            var android=t.GetPlatformTextureSettings("Android");android.overridden=true;android.maxTextureSize=4096;android.format=TextureImporterFormat.ASTC_6x6;t.SetPlatformTextureSettings(android);
            var ios=t.GetPlatformTextureSettings("iPhone");ios.overridden=true;ios.maxTextureSize=4096;ios.format=TextureImporterFormat.ASTC_6x6;t.SetPlatformTextureSettings(ios);
        }
        void OnPreprocessAudio()
        {
            if(!assetPath.StartsWith(Root))return;
            var a=(AudioImporter)assetImporter;var s=a.defaultSampleSettings;s.compressionFormat=AudioCompressionFormat.Vorbis;s.quality=.82f;s.loadType=AudioClipLoadType.DecompressOnLoad;s.preloadAudioData=true;a.defaultSampleSettings=s;a.forceToMono=false;
        }
    }
}
