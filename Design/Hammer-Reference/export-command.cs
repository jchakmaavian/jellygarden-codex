UnityEditor.EditorApplication.delayCall += () => {
    UnityEditor.AssetDatabase.ExportPackage(new[]{"Assets/ReferenceGameplay/Tools/Hammer","Assets/ReferenceGameplay/VFX/Resources/ReferenceGameplay/VFX/HammerImpactRfv387.prefab","Assets/ReferenceGameplay/VFX/Resources/ReferenceGameplay/VFX/HammerRewardRfv387.prefab"},"../Hammer-Animation-VFX-SFX.unitypackage",UnityEditor.ExportPackageOptions.Recurse|UnityEditor.ExportPackageOptions.IncludeDependencies);
    System.IO.File.WriteAllText("Design/Hammer-Reference/Review/package-export.txt","Exported Hammer-Animation-VFX-SFX.unitypackage with native animation, sprite, particle, material, and audio dependencies.");
};
return true;
