if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Edit mode required");
var result=KingImpactLaughChecks.Run();
result+="\n"+RoyalThrone.Characters.EditorTools.KingVanguardRuntimeChecks.Run();
System.IO.File.WriteAllText("Design/KingVanguard-ReferenceLaugh-20261004/Review/main-rig-checks.txt",result);
return result;
