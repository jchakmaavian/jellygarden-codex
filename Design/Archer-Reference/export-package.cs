UnityEditor.EditorApplication.delayCall += () => {
 try {
 UnityEditor.AssetDatabase.ExportPackage(new[]{"Assets/ReferenceGameplay/Tools/Archer","Assets/ReferenceGameplay/VFX/Resources/ReferenceGameplay/VFX/ArcherTrailRfv387.prefab","Assets/ReferenceGameplay/VFX/Resources/ReferenceGameplay/VFX/ArcherSmokeRfv387.prefab"},"../Archer-Animation-VFX-SFX.unitypackage",UnityEditor.ExportPackageOptions.Recurse|UnityEditor.ExportPackageOptions.IncludeDependencies);
 System.IO.File.WriteAllText("Design/Archer-Reference/Review/package-export.txt","Exported editable archer clips, controller, prefab, sprites, native VFX, materials and WAV. Game-specific runtime scripts are installed in the main project separately.");
 } catch(System.Exception e) {System.IO.File.WriteAllText("Design/Archer-Reference/Review/package-error.txt",e.ToString());}
};return true;
