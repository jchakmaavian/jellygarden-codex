if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Edit mode required");
UnityEditor.EditorApplication.delayCall+=()=>{try{var result=KingImpactLaughChecks.Run();result+="\n"+RoyalThrone.Characters.EditorTools.KingVanguardRuntimeChecks.Run();System.IO.File.WriteAllText("Design/KingVanguard-ImpactLaugh-20261004/Validation/main-rig-checks.txt",result);}catch(System.Exception e){System.IO.File.WriteAllText("Design/KingVanguard-ImpactLaugh-20261004/Validation/main-rig-checks.txt","FAIL "+e);UnityEngine.Debug.LogException(e);}};
return "Scheduled main regression checks";
