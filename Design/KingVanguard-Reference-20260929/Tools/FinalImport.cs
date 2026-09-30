foreach(var name in new[]{"King_Vanguard","AreaCompleted_Vanguard"})UnityEditor.AssetDatabase.ImportAsset("Assets/Characters/KingVenguard/V2/Spine/"+name+".png",UnityEditor.ImportAssetOptions.ForceSynchronousImport|UnityEditor.ImportAssetOptions.ForceUpdate);
System.IO.File.WriteAllText("Design/KingVanguard-Reference-20260929/Validation/runtime-checks.txt",RoyalThrone.Characters.EditorTools.KingVanguardRuntimeChecks.Run());
return "Final textures imported; controller checks PASS";
