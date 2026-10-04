using UnityEditor;
public static class KingPaintedExport {
 public static void Run(){
  AssetDatabase.ExportPackage(new[]{
   "Assets/Characters/KingVenguard/V2/Scripts/KingVanguardCharacter.cs",
   "Assets/Characters/KingVenguard/V2/Scripts/KingVanguardLaughter.cs",
   "Assets/Characters/KingVenguard/V2/Scripts/KingVanguardPaintedRig.cs",
   "Assets/Characters/KingVenguard/V2/Scripts/KingVanguardPaintedGraphic.cs",
   "Assets/Characters/KingVenguard/V2/Resources/KingVanguard/Interactive",
   "Assets/Characters/KingVenguard/V2/Resources/KingVanguard/LaughArt/ReferenceLaugh.png"
  },"../JellyGarden-Codex/Design/KingVanguard-InteractivePortrait-20261004/King-Interactive-Portrait.unitypackage",ExportPackageOptions.Recurse);
 }
}
