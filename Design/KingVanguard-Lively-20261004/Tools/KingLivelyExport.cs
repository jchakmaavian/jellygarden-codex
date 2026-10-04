using UnityEditor;
public static class KingLivelyExport {
 public static void Run(){
  AssetDatabase.ExportPackage(new[]{
   "Assets/Characters/KingVenguard/V2/Scripts/KingVanguardPaintedRig.cs",
   "Assets/Characters/KingVenguard/V2/Scripts/KingVanguardPaintedGraphic.cs",
   "Assets/Characters/KingVenguard/V2/Scripts/KingVanguardPerformance.cs",
   "Assets/Characters/KingVenguard/V2/Scripts/KingVanguardFaceMesh.cs",
   "Assets/Characters/KingVenguard/V2/Resources/KingVanguard/Interactive/KingVanguardPainted.shader"
  },"../JellyGarden-Codex/Design/KingVanguard-Lively-20261004/King-Lively-Upgrade.unitypackage",ExportPackageOptions.Recurse);
 }
}
