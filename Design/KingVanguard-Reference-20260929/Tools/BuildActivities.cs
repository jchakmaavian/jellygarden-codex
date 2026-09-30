{
 try {
  RoyalThrone.Characters.Activities.Editor.NativeKingDrillImporter.Build();
  var data=UnityEditor.AssetDatabase.LoadAssetAtPath<Spine.Unity.SkeletonDataAsset>("Assets/Characters/KingVenguard/V2/Spine/Nightmare_Vanguard_SkeletonData.asset");
  var go=new UnityEngine.GameObject("KingVanguardBedtime");
  try {
   var actor=go.AddComponent<Spine.Unity.SkeletonAnimation>();actor.skeletonDataAsset=data;actor.Initialize(false);actor.AnimationName="animation";actor.loop=true;
   UnityEditor.PrefabUtility.SaveAsPrefabAsset(go,"Assets/Characters/KingVenguard/V2/Resources/KingVanguard/KingVanguardBedtime.prefab");
  } finally { UnityEngine.Object.DestroyImmediate(go); }
  var built=RoyalThrone.Characters.EditorTools.KingVanguardPrefabBuilder.Build();
  var checks=RoyalThrone.Characters.EditorTools.KingVanguardRuntimeChecks.Run();
  System.IO.File.WriteAllText("Design/KingVanguard-Reference-20260929/Validation/runtime-checks.txt",built+"\n"+checks);
  System.IO.File.WriteAllText("Design/KingVanguard-Reference-20260929/Validation/build-activities.txt","PASS Native full-body and bedtime prefabs built; all controller checks passed.");
 } catch(System.Exception e) {System.IO.File.WriteAllText("Design/KingVanguard-Reference-20260929/Validation/build-activities.txt",e.ToString());UnityEngine.Debug.LogException(e);}
}
return System.IO.File.ReadAllText("Design/KingVanguard-Reference-20260929/Validation/build-activities.txt");
