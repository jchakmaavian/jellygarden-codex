var paths = UnityEditor.AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/JellyGarden/Resources/RoyalGameplay/Win/Letters"});
var report=new global::System.Collections.Generic.List<string>();
foreach(var id in paths) {
    var path=UnityEditor.AssetDatabase.GUIDToAssetPath(id);
    var ti=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
    ti.textureType=UnityEditor.TextureImporterType.Default; ti.maxTextureSize=1024;
    ti.npotScale=UnityEditor.TextureImporterNPOTScale.None; ti.mipmapEnabled=false;
    ti.alphaIsTransparency=true; ti.sRGBTexture=true;
    ti.textureCompression=UnityEditor.TextureImporterCompression.Uncompressed;
    ti.wrapMode=UnityEngine.TextureWrapMode.Clamp; ti.filterMode=UnityEngine.FilterMode.Bilinear;
    ti.SaveAndReimport();
    var tex=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(path);
    report.Add(path+" "+tex.width+"x"+tex.height+" "+tex.format);
}
return report;