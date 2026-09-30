UnityEditor.AssetDatabase.Refresh(UnityEditor.ImportAssetOptions.ForceSynchronousImport);
foreach(string rig in new[]{"King_Vanguard","AreaCompleted_Vanguard"}){
 var asset=UnityEditor.AssetDatabase.LoadAssetAtPath<Spine.Unity.SkeletonDataAsset>("Assets/Characters/KingVenguard/V2/Spine/"+rig+"_SkeletonData.asset");
 asset.Clear();if(asset.GetSkeletonData(false)==null)throw new System.Exception("Face data failed to reload: "+rig);
}
return "Reloaded original facial geometry and current face configuration.";
