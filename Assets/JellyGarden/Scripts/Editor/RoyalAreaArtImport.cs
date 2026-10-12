using System.IO;
using System.Linq;
using UnityEditor;

namespace RoyalThrone.Editor
{
    // Area art (Resources/RoyalAreas, WebP from Design/Areas/tools/build_areas.py) is saved with ASTC on phones, where a full
    // 940 x 1672 scene would otherwise decompress to RGBA32; task objects also bleed colour under their soft transparent edges.
    // The settings live in each file's .meta; run this after the pipeline adds new area art.
    public static class RoyalAreaArtImport
    {
        public const string Folder = "Assets/JellyGarden/Resources/RoyalAreas";

        [MenuItem("Tools/Royal Throne/Areas/Apply area art import settings")]
        public static int Apply()
        {
            int changed = 0;
            foreach (string path in Directory.GetFiles(Folder, "*.webp", SearchOption.AllDirectories).Select(p => p.Replace('\\', '/')))
            {
                if (!(AssetImporter.GetAtPath(path) is RoyalWebPImporter importer)) continue;
                bool bleed = Path.GetFileName(path).StartsWith("task-");
                if (importer.astcOnMobile && importer.bleedTransparentEdges == bleed) continue;
                importer.astcOnMobile = true; importer.bleedTransparentEdges = bleed;
                // Field changes from code do not mark the importer dirty, and SaveAndReimport then writes nothing.
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport(); changed++;
            }
            return changed;
        }
    }
}
