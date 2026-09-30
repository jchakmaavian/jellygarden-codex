var built=RoyalThrone.Characters.EditorTools.KingVanguardPrefabBuilder.Build();
var checks=RoyalThrone.Characters.EditorTools.KingVanguardRuntimeChecks.Run();
System.IO.Directory.CreateDirectory("Design/KingVanguard-Reference-20260929/Validation");
System.IO.File.WriteAllText("Design/KingVanguard-Reference-20260929/Validation/runtime-checks.txt",built+"\n"+checks);
return built+"\n"+checks;
